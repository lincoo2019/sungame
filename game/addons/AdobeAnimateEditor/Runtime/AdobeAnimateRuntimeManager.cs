using System;
using System.Collections.Generic;
using Godot;

public sealed partial class AdobeAnimateRuntimeManager : Node
{
	private readonly struct RenderRootOrderEntry(AdobeAnimateSprite sprite, int[] treeOrderPath, ulong stableOrder)
	{
		public AdobeAnimateSprite Sprite { get; } = sprite;

		public int[] TreeOrderPath { get; } = treeOrderPath;

		public ulong StableOrder { get; } = stableOrder;
	}

	private sealed class RenderRootOrderEntryComparer : IComparer<RenderRootOrderEntry>
	{
		public static readonly RenderRootOrderEntryComparer Instance = new RenderRootOrderEntryComparer();

		public int Compare(RenderRootOrderEntry left, RenderRootOrderEntry right)
		{
			return AdobeAnimateRenderManager.CompareTreeOrderForRender(left.TreeOrderPath, left.StableOrder, right.TreeOrderPath, right.StableOrder);
		}
	}

	private const double StaticCrowdRefreshIntervalSeconds = 0.2;

	private const int PauseDispatchValidationSlices = 60;

	internal const double AnimationLogicTicksPerSecond = 30.0;

	private const double AnimationLogicTickInterval = 1.0 / 30.0;

	internal const int FullVisualRateCharacterLimitExclusive = 300;

	internal const int HalfVisualRateCharacterLimitExclusive = 500;

	internal const int ThirdVisualRateCharacterLimitExclusive = 800;

	internal const int QuarterVisualRateCharacterLimitExclusive = 1000;

	public static bool UseRuntimeManager = true;

	private static bool _mountRequested;

	private readonly List<AdobeAnimateSprite> _active = new List<AdobeAnimateSprite>();

	private readonly List<AdobeAnimateSprite> _renderRoots = new List<AdobeAnimateSprite>();

	private readonly List<RenderRootOrderEntry> _renderRootOrderEntries = new List<RenderRootOrderEntry>();

	private readonly List<AdobeAnimateSprite> _displayVisualCandidates = new List<AdobeAnimateSprite>();

	private readonly Dictionary<AdobeAnimateSprite, int> _activeIndices = new Dictionary<AdobeAnimateSprite, int>();

	private readonly HashSet<AdobeAnimateSprite> _registered = new HashSet<AdobeAnimateSprite>();

	private readonly HashSet<AdobeAnimateSprite> _renderStaticArrayAuditSprites = new HashSet<AdobeAnimateSprite>();

	private readonly HashSet<AdobeAnimateSprite> _pendingRenderStaticArrayAuditSprites = new HashSet<AdobeAnimateSprite>();

	private readonly List<AdobeAnimateSprite> _staleRenderStaticArrayAuditSprites = new List<AdobeAnimateSprite>();

	private readonly HashSet<AdobeAnimateSprite> _toAdd = new HashSet<AdobeAnimateSprite>();

	private readonly HashSet<AdobeAnimateSprite> _toRemove = new HashSet<AdobeAnimateSprite>();

	private readonly HashSet<AdobeAnimateSprite> _pauseDispatchDirty = new HashSet<AdobeAnimateSprite>();

	private readonly Dictionary<Node, ProcessModeEnum> _processModeParentCache = new Dictionary<Node, ProcessModeEnum>();

	private SceneTree _runtimeTree;

	private ulong _displayTickVersion;

	private bool _dispatchTreePaused;

	private bool _dispatchTreePausedInitialized;

	private int _pauseDispatchValidationCursor;

	private bool _renderRootsDirty = true;

	private double _staticCrowdRefreshAccumulator;

	private double _animationClockSeconds;

	private double _animationLogicAccumulator;

	private double _visualCadenceAccumulator;

	private double _displayVisualDeltaAccumulator;

	private double _currentVisualTicksPerSecond;

	private int _currentBattleCharacterCount;

	private bool _displayRenderSubmissionPending;

	private bool _fixedRenderSubmissionPending;

	private bool _processingDisplayVisualCandidates;

	private ulong _lastRenderPhysicsFrame = 18446744073709551615uL;

	private ulong _lastRenderStaticArrayAuditPhysicsFrame = 18446744073709551615uL;

	private bool _processEligibilityInitialized;

	private bool _processEligibilityEnabled;

	private Global _subscribedGlobal;

	public static AdobeAnimateRuntimeManager Instance { get; private set; }

	internal static double AnimationClockSeconds
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0.0;
			}
			return Instance._animationClockSeconds;
		}
	}

	internal int RegisteredSpriteCount => _registered.Count;

	internal double CurrentVisualTicksPerSecond => _currentVisualTicksPerSecond;

	internal int CurrentBattleCharacterCount => _currentBattleCharacterCount;

	internal static bool HasInvalidatedRenderRootOrder
	{
		get
		{
			if (Instance != null)
			{
				return Instance._renderRootsDirty;
			}
			return true;
		}
	}

	internal static void InvalidateRenderRoots()
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			Instance._renderRootsDirty = true;
		}
	}

	internal static void NotifyRenderRootVisibilityChanged(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(Instance) && sprite != null && Instance._registered.Contains(sprite))
		{
			AdobeAnimateRuntimeManager instance = Instance;
			instance._renderRootsDirty = true;
			instance._fixedRenderSubmissionPending = true;
			instance.RefreshProcessEligibility();
		}
	}

	internal static void NotifyGpuRenderCachesCleared()
	{
		AdobeAnimateSprite.InvalidateStaticAtlasPathLayoutCacheForGpuReset();
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return;
		}
		AdobeAnimateRuntimeManager instance = Instance;
		instance._renderRootsDirty = true;
		instance._fixedRenderSubmissionPending = true;
		instance._staticCrowdRefreshAccumulator = 0.0;
		foreach (AdobeAnimateSprite item in instance._registered)
		{
			if (GodotObject.IsInstanceValid(item) && item.IsInsideTree())
			{
				item.InvalidateRuntimeRenderAfterGpuCacheReset();
			}
		}
		instance.SetProcess(enable: true);
	}

	internal static void NotifyViewportTransformChanged()
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return;
		}
		AdobeAnimateRuntimeManager instance = Instance;
		instance._fixedRenderSubmissionPending = true;
		instance._staticCrowdRefreshAccumulator = 0.0;
		foreach (AdobeAnimateSprite item in instance._registered)
		{
			if (GodotObject.IsInstanceValid(item) && item.IsInsideTree())
			{
				item.InvalidateRuntimeCrowdCullingForRefresh();
			}
		}
		instance.SetProcess(enable: true);
	}

	internal static void RequestRenderRootRepublish()
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			AdobeAnimateRuntimeManager instance = Instance;
			instance._fixedRenderSubmissionPending = true;
			instance._staticCrowdRefreshAccumulator = 0.0;
			instance.SetProcess(enable: true);
		}
	}

	internal static void RecoverFromFailedRenderTransaction(long frameVersion)
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return;
		}
		AdobeAnimateRuntimeManager instance = Instance;
		instance._fixedRenderSubmissionPending = true;
		instance._staticCrowdRefreshAccumulator = 0.0;
		for (int i = 0; i < instance._active.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = instance._active[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.IsRuntimeInsideTree)
			{
				try
				{
					adobeAnimateSprite.RestoreRuntimeNativeCanvasLayer();
					adobeAnimateSprite.RestoreExternalVisualNativeFallbacks(frameVersion);
					adobeAnimateSprite.RequestRuntimeRenderSubmissionRetry();
				}
				catch (Exception value)
				{
					GD.PushError($"Adobe Animate registered sprite recovery failed for {adobeAnimateSprite.Name}: {value}");
				}
			}
		}
		instance.RefreshProcessEligibility();
	}

	internal static void NotifyRenderAncestorTransformChanged()
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return;
		}
		AdobeAnimateRuntimeManager instance = Instance;
		instance.FlushPending();
		instance.RefreshRenderRootsIfNeeded();
		instance._fixedRenderSubmissionPending = true;
		instance._staticCrowdRefreshAccumulator = 0.0;
		for (int i = 0; i < instance._renderRoots.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = instance._renderRoots[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.IsInsideTree())
			{
				adobeAnimateSprite.NotifyAncestorTransformChangedForRender();
				adobeAnimateSprite.InvalidateRuntimeCrowdCullingForRefresh();
			}
		}
		instance.SetProcess(enable: true);
	}

	internal static void NotifyRenderSubmission(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(Instance) && GodotObject.IsInstanceValid(sprite) && Instance._registered.Contains(sprite))
		{
			AdobeAnimateRuntimeManager instance = Instance;
			if (instance._processingDisplayVisualCandidates)
			{
				instance._displayRenderSubmissionPending = true;
			}
			else
			{
				instance._fixedRenderSubmissionPending = true;
			}
			instance.RefreshProcessEligibility();
		}
	}

	internal static void NotifyDisplayRenderSubmission(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(Instance) && GodotObject.IsInstanceValid(sprite) && Instance._registered.Contains(sprite))
		{
			AdobeAnimateRuntimeManager instance = Instance;
			instance._displayRenderSubmissionPending = true;
			instance.RefreshProcessEligibility();
		}
	}

	internal static void NotifyPauseDispatchInvalidated(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(Instance) && GodotObject.IsInstanceValid(sprite) && Instance._registered.Contains(sprite))
		{
			Instance._pauseDispatchDirty.Add(sprite);
			Instance.SetProcess(enable: true);
		}
	}

	internal static void SetRenderStaticArrayAuditRegistration(AdobeAnimateSprite sprite, bool enabled)
	{
		if (GodotObject.IsInstanceValid(Instance) && GodotObject.IsInstanceValid(sprite))
		{
			Instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled);
		}
	}

	private void UpdateRenderStaticArrayAuditRegistration(AdobeAnimateSprite sprite, bool enabled)
	{
		if (enabled)
		{
			if (_registered.Contains(sprite) && _renderStaticArrayAuditSprites.Add(sprite))
			{
				_pendingRenderStaticArrayAuditSprites.Add(sprite);
			}
		}
		else
		{
			_renderStaticArrayAuditSprites.Remove(sprite);
			_pendingRenderStaticArrayAuditSprites.Remove(sprite);
		}
	}

	public static bool Register(AdobeAnimateSprite sprite)
	{
		if (!UseRuntimeManager || Engine.IsEditorHint() || !GodotObject.IsInstanceValid(sprite) || !sprite.IsInsideTree())
		{
			return false;
		}
		if (!HasUsableInstance())
		{
			TryMount(sprite);
		}
		if (!HasUsableInstance())
		{
			return false;
		}
		AdobeAnimateRuntimeManager instance = Instance;
		instance._toRemove.Remove(sprite);
		if (instance._registered.Contains(sprite))
		{
			if (sprite.RequiresRenderStaticArrayAuditForRuntimeManager())
			{
				instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled: true);
			}
			else
			{
				instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled: false);
			}
			return true;
		}
		instance._registered.Add(sprite);
		if (sprite.RequiresRenderStaticArrayAuditForRuntimeManager())
		{
			instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled: true);
		}
		instance._toAdd.Add(sprite);
		instance._pauseDispatchDirty.Add(sprite);
		instance.RefreshProcessEligibility();
		return true;
	}

	public static void Unregister(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			AdobeAnimateRuntimeManager instance = Instance;
			if (instance._toAdd.Remove(sprite))
			{
				instance._registered.Remove(sprite);
				instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled: false);
				instance._pauseDispatchDirty.Remove(sprite);
				instance.RefreshProcessEligibility();
			}
			else if (instance._registered.Contains(sprite))
			{
				instance.UpdateRenderStaticArrayAuditRegistration(sprite, enabled: false);
				instance._pauseDispatchDirty.Remove(sprite);
				instance._toRemove.Add(sprite);
				instance.RefreshProcessEligibility();
			}
		}
	}

	public override void _EnterTree()
	{
		_runtimeTree = GetTree();
	}

	public override void _Ready()
	{
		if (GodotObject.IsInstanceValid(Instance) && Instance != this)
		{
			SetProcess(enable: false);
			SetPhysicsProcess(enable: false);
			QueueFree();
			return;
		}
		Instance = this;
		_mountRequested = false;
		_animationClockSeconds = 0.0;
		_animationLogicAccumulator = 0.0;
		_visualCadenceAccumulator = 0.0;
		_displayVisualDeltaAccumulator = 0.0;
		_currentVisualTicksPerSecond = 0.0;
		_currentBattleCharacterCount = 0;
		_lastRenderPhysicsFrame = 18446744073709551615uL;
		_lastRenderStaticArrayAuditPhysicsFrame = 18446744073709551615uL;
		base.ProcessMode = ProcessModeEnum.Always;
		_subscribedGlobal = Global.Instance;
		if (_subscribedGlobal != null)
		{
			_subscribedGlobal.OnAdobeAnimateRenderBackendChanged += HandleAdobeAnimateRenderBackendChanged;
		}
		RefreshProcessEligibility();
	}

	public override void _ExitTree()
	{
		_runtimeTree = null;
		if (_subscribedGlobal != null)
		{
			_subscribedGlobal.OnAdobeAnimateRenderBackendChanged -= HandleAdobeAnimateRenderBackendChanged;
		}
		_subscribedGlobal = null;
		if (Instance == this)
		{
			Instance = null;
			_mountRequested = false;
		}
		_active.Clear();
		_renderRoots.Clear();
		_renderRootOrderEntries.Clear();
		_displayVisualCandidates.Clear();
		_activeIndices.Clear();
		_registered.Clear();
		_renderStaticArrayAuditSprites.Clear();
		_pendingRenderStaticArrayAuditSprites.Clear();
		_staleRenderStaticArrayAuditSprites.Clear();
		_toAdd.Clear();
		_toRemove.Clear();
		_pauseDispatchDirty.Clear();
		_processModeParentCache.Clear();
		_displayTickVersion = 0uL;
		_dispatchTreePaused = false;
		_dispatchTreePausedInitialized = false;
		_pauseDispatchValidationCursor = 0;
		_renderRootsDirty = true;
		_staticCrowdRefreshAccumulator = 0.0;
		_animationClockSeconds = 0.0;
		_animationLogicAccumulator = 0.0;
		_visualCadenceAccumulator = 0.0;
		_displayVisualDeltaAccumulator = 0.0;
		_currentVisualTicksPerSecond = 0.0;
		_currentBattleCharacterCount = 0;
		_displayRenderSubmissionPending = false;
		_fixedRenderSubmissionPending = false;
		_processingDisplayVisualCandidates = false;
		_lastRenderPhysicsFrame = 18446744073709551615uL;
		_lastRenderStaticArrayAuditPhysicsFrame = 18446744073709551615uL;
		_processEligibilityInitialized = false;
		_processEligibilityEnabled = false;
	}

	private void HandleAdobeAnimateRenderBackendChanged(AdobeAnimateRenderBackend _)
	{
		_renderRootsDirty = true;
		foreach (AdobeAnimateSprite item in _registered)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.InvalidateRuntimeCrowdCullingForRefresh();
			}
		}
		if (_registered.Count != 0)
		{
			_fixedRenderSubmissionPending = true;
			SetProcess(enable: true);
		}
	}

	public override void _Process(double delta)
	{
		if (GodotObject.IsInstanceValid(Instance) && Instance != this)
		{
			SetProcess(enable: false);
			SetPhysicsProcess(enable: false);
			QueueFree();
			return;
		}
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startBytes2 = TowerDefenseAllocationTelemetry.Begin();
		_animationClockSeconds += delta;
		_animationLogicAccumulator += delta;
		_displayVisualDeltaAccumulator += delta;
		_currentBattleCharacterCount = ResolveBattleCharacterCount();
		int renderFrameRate = ResolveRenderFrameRate();
		double num = ResolveAdaptiveVisualTicksPerSecond(renderFrameRate, _currentBattleCharacterCount);
		bool flag = AdvanceVisualCadence(delta, num, renderFrameRate);
		AdobeAnimateRenderManager.SetAnimationClock(_animationClockSeconds);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RuntimeClock, startBytes2);
		_displayTickVersion++;
		bool flag2 = false;
		long startBytes3 = TowerDefenseAllocationTelemetry.Begin();
		if (flag)
		{
			double displayVisualDeltaAccumulator = _displayVisualDeltaAccumulator;
			_displayVisualDeltaAccumulator = 0.0;
			_processingDisplayVisualCandidates = true;
			try
			{
				flag2 = ProcessDisplayVisualCandidates(displayVisualDeltaAccumulator);
			}
			finally
			{
				_processingDisplayVisualCandidates = false;
			}
		}
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RuntimeDisplay, startBytes3);
		_displayRenderSubmissionPending |= flag2;
		long startBytes4 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool flag3 = ProcessRenderStaticArrayAudits();
		TowerDefensePerfProfiler.End("adobeAnimate.process.staticArrayAudit", startTicks, _renderStaticArrayAuditSprites.Count);
		if (flag3)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.reason.staticArrayAudit", 1);
		}
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RuntimeStaticAudit, startBytes4);
		bool flag4 = _displayRenderSubmissionPending || _fixedRenderSubmissionPending || flag3;
		if (_animationLogicAccumulator >= 1.0 / 30.0)
		{
			long startBytes5 = TowerDefenseAllocationTelemetry.Begin();
			double num2 = (double)Math.Max(1, (int)Math.Floor(_animationLogicAccumulator / (1.0 / 30.0))) * (1.0 / 30.0);
			_animationLogicAccumulator = Math.Max(0.0, _animationLogicAccumulator - num2);
			_processingDisplayVisualCandidates = true;
			bool flag5;
			try
			{
				flag5 = ProcessActive(num2);
			}
			finally
			{
				_processingDisplayVisualCandidates = false;
			}
			flag4 = flag4 || flag5;
			TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RuntimeLogic, startBytes5);
		}
		_staticCrowdRefreshAccumulator += Math.Max(0.0, delta);
		bool renderRootsDirty = _renderRootsDirty;
		bool num3 = _staticCrowdRefreshAccumulator >= 0.2;
		bool flag6 = false;
		if (num3)
		{
			flag6 = InvalidateRuntimeCrowdCullingIfCachedOffscreen();
			_staticCrowdRefreshAccumulator %= 0.2;
		}
		bool flag7 = renderRootsDirty || flag4 || flag6;
		ulong physicsFrames = Engine.GetPhysicsFrames();
		bool flag8 = _lastRenderPhysicsFrame != physicsFrames;
		bool num4 = (renderRootsDirty || flag6 || _fixedRenderSubmissionPending) && flag8;
		bool flag9 = _displayRenderSubmissionPending && flag;
		bool flag10 = num4 || flag9;
		SceneTree runtimeTree = _runtimeTree;
		if (!flag10 && GodotObject.IsInstanceValid(runtimeTree) && runtimeTree.Paused)
		{
			flag10 = true;
		}
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			if (flag4)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.reason.invalidated", 1);
			}
			if (renderRootsDirty)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.reason.topology", 1);
			}
			if (flag6)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.reason.periodic", 1);
			}
			if (flag9)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.reason.displayCadence", 1);
			}
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.visual.characterCount", _currentBattleCharacterCount);
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.visual.targetFps", (int)Math.Round(num));
		}
		if (flag7 && flag10)
		{
			_displayRenderSubmissionPending = false;
			_fixedRenderSubmissionPending = false;
			RenderActive();
			_fixedRenderSubmissionPending |= MarkRuntimeRenderSubmissionsConsumed();
			_lastRenderPhysicsFrame = physicsFrames;
			_staticCrowdRefreshAccumulator = 0.0;
		}
		else if (!flag7)
		{
			TowerDefensePerfProfiler.DumpIfNeeded();
		}
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RuntimeProcess, startBytes);
		RefreshProcessEligibility();
	}

	private bool ProcessRenderStaticArrayAudits()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		bool flag = _lastRenderStaticArrayAuditPhysicsFrame != physicsFrames;
		if (!flag && _pendingRenderStaticArrayAuditSprites.Count == 0)
		{
			return false;
		}
		if (_renderStaticArrayAuditSprites.Count == 0)
		{
			_pendingRenderStaticArrayAuditSprites.Clear();
			return false;
		}
		bool flag2 = false;
		_staleRenderStaticArrayAuditSprites.Clear();
		if (flag)
		{
			_lastRenderStaticArrayAuditPhysicsFrame = physicsFrames;
			_pendingRenderStaticArrayAuditSprites.Clear();
			foreach (AdobeAnimateSprite renderStaticArrayAuditSprite in _renderStaticArrayAuditSprites)
			{
				flag2 |= AuditRenderStaticArraySprite(renderStaticArrayAuditSprite, physicsFrames);
			}
		}
		else
		{
			foreach (AdobeAnimateSprite pendingRenderStaticArrayAuditSprite in _pendingRenderStaticArrayAuditSprites)
			{
				flag2 |= AuditRenderStaticArraySprite(pendingRenderStaticArrayAuditSprite, physicsFrames);
			}
			_pendingRenderStaticArrayAuditSprites.Clear();
		}
		for (int i = 0; i < _staleRenderStaticArrayAuditSprites.Count; i++)
		{
			UpdateRenderStaticArrayAuditRegistration(_staleRenderStaticArrayAuditSprites[i], enabled: false);
		}
		_staleRenderStaticArrayAuditSprites.Clear();
		return flag2;
	}

	private bool AuditRenderStaticArraySprite(AdobeAnimateSprite sprite, ulong physicsFrame)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.IsInsideTree())
		{
			_staleRenderStaticArrayAuditSprites.Add(sprite);
			return false;
		}
		return sprite.QueueRenderStaticArrayAuditIfDue(physicsFrame);
	}

	private static int ResolveRenderFrameRate()
	{
		Global instance = Global.Instance;
		if (instance != null && instance.effectiveAnimeFrameRate > 0)
		{
			return instance.effectiveAnimeFrameRate;
		}
		int maxFps = Engine.MaxFps;
		if (maxFps <= 0)
		{
			return 60;
		}
		return maxFps;
	}

	private static int ResolveBattleCharacterCount()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return 0;
		}
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
		{
			return 0;
		}
		return Math.Max(0, towerDefenseBattleCharacterRegistry.ActiveCharacterCount);
	}

	internal static double ResolveAdaptiveVisualTicksPerSecond(int renderFrameRate, int characterCount)
	{
		int num = Math.Max(1, renderFrameRate);
		int num2 = Math.Max(0, characterCount);
		double val = ((num2 < 300) ? 180.0 : ((num2 < 500) ? 90.0 : ((num2 < 800) ? 60.0 : ((num2 < 1000) ? 45.0 : 30.0))));
		return Math.Min(num, val);
	}

	private bool AdvanceVisualCadence(double delta, double visualTicksPerSecond, int renderFrameRate)
	{
		bool flag = !Mathf.IsEqualApprox((float)_currentVisualTicksPerSecond, (float)visualTicksPerSecond);
		_currentVisualTicksPerSecond = visualTicksPerSecond;
		if (flag)
		{
			_visualCadenceAccumulator = 0.0;
		}
		if (visualTicksPerSecond >= (double)renderFrameRate - 0.001)
		{
			_visualCadenceAccumulator = 0.0;
			return true;
		}
		double num = 1.0 / Math.Max(1.0, visualTicksPerSecond);
		_visualCadenceAccumulator += Math.Max(0.0, delta);
		if (!flag && _visualCadenceAccumulator + 1E-06 < num)
		{
			return false;
		}
		_visualCadenceAccumulator %= num;
		return true;
	}

	private void RefreshProcessEligibility()
	{
		bool flag = _registered.Count > 0 || _toAdd.Count > 0 || _toRemove.Count > 0;
		if (!_processEligibilityInitialized || _processEligibilityEnabled != flag)
		{
			_processEligibilityInitialized = true;
			_processEligibilityEnabled = flag;
			SetProcess(flag);
		}
	}

	private static bool HasUsableInstance()
	{
		return GodotObject.IsInstanceValid(Instance);
	}

	private static void TryMount(AdobeAnimateSprite sprite)
	{
		if (_mountRequested)
		{
			return;
		}
		SceneTree tree = sprite.GetTree();
		if (GodotObject.IsInstanceValid(tree) && GodotObject.IsInstanceValid(tree.Root))
		{
			AdobeAnimateRuntimeManager instance = FindExistingManager(tree.Root);
			if (GodotObject.IsInstanceValid(instance))
			{
				Instance = instance;
				return;
			}
			AdobeAnimateRuntimeManager adobeAnimateRuntimeManager = (Instance = new AdobeAnimateRuntimeManager
			{
				Name = "AdobeAnimateRuntimeManager"
			});
			_mountRequested = true;
			tree.Root.CallDeferred(Node.MethodName.AddChild, adobeAnimateRuntimeManager);
		}
	}

	private static AdobeAnimateRuntimeManager FindExistingManager(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		int childCount = root.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			if (root.GetChild(i) is AdobeAnimateRuntimeManager adobeAnimateRuntimeManager && GodotObject.IsInstanceValid(adobeAnimateRuntimeManager))
			{
				return adobeAnimateRuntimeManager;
			}
		}
		return null;
	}

	private bool ProcessDisplayVisualCandidates(double delta)
	{
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.Begin();
		int num = 0;
		bool flag = false;
		int num2 = 0;
		int num3 = 0;
		long startTicks2 = TowerDefensePerfProfiler.Begin();
		FlushPending();
		TowerDefensePerfProfiler.End("adobeAnimate.process.flushPending", startTicks2);
		RefreshPauseDispatchState(validatePeriodicSlice: false);
		for (int i = 0; i < _displayVisualCandidates.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _displayVisualCandidates[i];
			if (adobeAnimateSprite == null || !adobeAnimateSprite.IsRuntimeInsideTree)
			{
				_toRemove.Add(adobeAnimateSprite);
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.detachedSprite");
				continue;
			}
			flag |= adobeAnimateSprite.NeedsRuntimeRenderSubmission;
			if (!adobeAnimateSprite.IsRuntimeDisplayTickActive)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.inactive");
				continue;
			}
			if (!adobeAnimateSprite.RuntimeManagerDispatchActive)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.pauseModeSkip");
				continue;
			}
			adobeAnimateSprite.MarkRuntimeDisplayTicked(_displayTickVersion);
			long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
			adobeAnimateSprite.RunDisplayFrameVisualUpdate(delta);
			bool needsRuntimeRenderSubmission = adobeAnimateSprite.NeedsRuntimeRenderSubmission;
			flag = flag || needsRuntimeRenderSubmission;
			if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
			{
				if (adobeAnimateSprite.UsesRuntimeGpuClockInterpolation)
				{
					num2++;
				}
				if (needsRuntimeRenderSubmission)
				{
					num3++;
				}
			}
			TowerDefensePerfProfiler.End("adobeAnimate.process.sprite", startTicks3, 1);
			num++;
		}
		if (_toRemove.Count > 0)
		{
			startTicks2 = TowerDefensePerfProfiler.Begin();
			FlushPending();
			TowerDefensePerfProfiler.End("adobeAnimate.process.flushRemoved", startTicks2);
		}
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.gpuClockActive", num2);
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.dirtyAfter", num3);
		}
		TowerDefensePerfProfiler.End("batch.adobeAnimate.displayVisual", startTicks, num);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.AnimationDisplay, startBytes);
		return flag;
	}

	private bool ProcessActive(double delta)
	{
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.Begin();
		int num = 0;
		bool flag = false;
		int num2 = 0;
		int num3 = 0;
		long startTicks2 = TowerDefensePerfProfiler.Begin();
		FlushPending();
		TowerDefensePerfProfiler.End("adobeAnimate.process.flushPending", startTicks2);
		RefreshPauseDispatchState(validatePeriodicSlice: true);
		_displayVisualCandidates.Clear();
		for (int i = 0; i < _active.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _active[i];
			if (adobeAnimateSprite == null || !adobeAnimateSprite.IsRuntimeInsideTree)
			{
				_toRemove.Add(adobeAnimateSprite);
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.detachedSprite");
				continue;
			}
			flag |= adobeAnimateSprite.NeedsRuntimeRenderSubmission;
			if (!adobeAnimateSprite.IsRuntimeActive)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.inactive");
			}
			else if (adobeAnimateSprite.IsRuntimeTickPaused)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.paused");
			}
			else if (!adobeAnimateSprite.RuntimeManagerDispatchActive)
			{
				TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.pauseModeSkip");
			}
			else if (adobeAnimateSprite.RequiresDisplayFrameVisualTick())
			{
				_displayVisualCandidates.Add(adobeAnimateSprite);
			}
			else
			{
				if (adobeAnimateSprite.WasRuntimeDisplayTicked(_displayTickVersion))
				{
					continue;
				}
				long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
				adobeAnimateSprite.RunDefaultBatchedProcessUpdate(delta);
				bool needsRuntimeRenderSubmission = adobeAnimateSprite.NeedsRuntimeRenderSubmission;
				flag = flag || needsRuntimeRenderSubmission;
				if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
				{
					if (adobeAnimateSprite.UsesRuntimeGpuClockInterpolation)
					{
						num2++;
					}
					if (needsRuntimeRenderSubmission)
					{
						num3++;
					}
				}
				TowerDefensePerfProfiler.End("adobeAnimate.process.sprite", startTicks3, 1);
				num++;
			}
		}
		if (_toRemove.Count > 0)
		{
			startTicks2 = TowerDefensePerfProfiler.Begin();
			FlushPending();
			TowerDefensePerfProfiler.End("adobeAnimate.process.flushRemoved", startTicks2);
		}
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.gpuClockActive", num2);
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.process.dirtyAfter", num3);
		}
		TowerDefensePerfProfiler.End("batch.adobeAnimate.process", startTicks, num);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.AnimationLogic, startBytes);
		return flag;
	}

	private bool MarkRuntimeRenderSubmissionsConsumed()
	{
		bool flag = false;
		long lastCompletedRuntimeTransactionVersion = AdobeAnimateRenderManager.LastCompletedRuntimeTransactionVersion;
		for (int i = 0; i < _active.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _active[i];
			if (adobeAnimateSprite != null && adobeAnimateSprite.IsRuntimeInsideTree)
			{
				if (lastCompletedRuntimeTransactionVersion != -9223372036854775808L)
				{
					flag |= adobeAnimateSprite.RecoverRuntimeNativeCanvasAfterMissedTransaction(lastCompletedRuntimeTransactionVersion);
				}
				flag |= adobeAnimateSprite.MarkRuntimeRenderSubmissionConsumed();
			}
		}
		return flag;
	}

	private bool InvalidateRuntimeCrowdCullingIfCachedOffscreen()
	{
		RefreshRenderRootsIfNeeded();
		bool flag = false;
		for (int i = 0; i < _renderRoots.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _renderRoots[i];
			if (adobeAnimateSprite != null && adobeAnimateSprite.IsRuntimeInsideTree)
			{
				flag |= adobeAnimateSprite.InvalidateRuntimeCrowdCullingIfCachedOffscreen();
			}
		}
		return flag;
	}

	private void RenderActive()
	{
		RefreshRenderRootsIfNeeded();
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.Begin();
		AdobeAnimateRenderManager.RenderRuntimeTreeOrdered(_renderRoots);
		TowerDefensePerfProfiler.End("adobeAnimate.render.dispatch", startTicks, _renderRoots.Count);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderDispatch, startBytes);
		TowerDefensePerfProfiler.DumpIfNeeded();
	}

	private void RefreshRenderRootsIfNeeded()
	{
		if (!_renderRootsDirty)
		{
			return;
		}
		_renderRoots.Clear();
		_renderRootOrderEntries.Clear();
		for (int i = 0; i < _active.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _active[i];
			if (adobeAnimateSprite != null && adobeAnimateSprite.IsRuntimeInsideTree && adobeAnimateSprite.IsRuntimeVisibleInTree && !adobeAnimateSprite.IsRenderedByParentSpriteForRender())
			{
				adobeAnimateSprite.InvalidateRuntimeTreeOrderForTopologyChange();
				_renderRoots.Add(adobeAnimateSprite);
				_renderRootOrderEntries.Add(new RenderRootOrderEntry(adobeAnimateSprite, adobeAnimateSprite.GetEffectiveTreeOrderPathForRender(), adobeAnimateSprite.GetCachedInstanceIdForRender()));
			}
		}
		if (_renderRootOrderEntries.Count > 1)
		{
			_renderRootOrderEntries.Sort(RenderRootOrderEntryComparer.Instance);
		}
		for (int j = 0; j < _renderRootOrderEntries.Count; j++)
		{
			_renderRoots[j] = _renderRootOrderEntries[j].Sprite;
		}
		_renderRootsDirty = false;
	}

	private void RefreshPauseDispatchState(bool validatePeriodicSlice)
	{
		SceneTree runtimeTree = _runtimeTree;
		bool flag = GodotObject.IsInstanceValid(runtimeTree) && runtimeTree.Paused;
		bool num = !_dispatchTreePausedInitialized || flag != _dispatchTreePaused;
		_dispatchTreePaused = flag;
		_dispatchTreePausedInitialized = true;
		_processModeParentCache.Clear();
		if (num)
		{
			for (int i = 0; i < _active.Count; i++)
			{
				ResolvePauseDispatch(_active[i]);
			}
			_pauseDispatchDirty.Clear();
			_pauseDispatchValidationCursor = 0;
			return;
		}
		if (_pauseDispatchDirty.Count > 0)
		{
			foreach (AdobeAnimateSprite item in _pauseDispatchDirty)
			{
				if (_registered.Contains(item))
				{
					ResolvePauseDispatch(item);
				}
			}
			_pauseDispatchDirty.Clear();
		}
		if (!validatePeriodicSlice || _active.Count == 0)
		{
			return;
		}
		int num2 = Math.Max(1, (_active.Count + 60 - 1) / 60);
		for (int j = 0; j < num2; j++)
		{
			if (_pauseDispatchValidationCursor >= _active.Count)
			{
				_pauseDispatchValidationCursor = 0;
			}
			ResolvePauseDispatch(_active[_pauseDispatchValidationCursor]);
			_pauseDispatchValidationCursor++;
		}
	}

	private void ResolvePauseDispatch(AdobeAnimateSprite sprite)
	{
		if (sprite == null || !sprite.IsRuntimeInsideTree)
		{
			_toRemove.Add(sprite);
		}
		else
		{
			sprite.SetRuntimeManagerDispatchActive(ShouldDispatchInCurrentPauseState(sprite));
		}
	}

	private bool ShouldDispatchInCurrentPauseState(Node node)
	{
		bool dispatchTreePaused = _dispatchTreePaused;
		ProcessModeEnum processMode = node.ProcessMode;
		switch (processMode)
		{
		case ProcessModeEnum.Disabled:
			return false;
		default:
			if (!dispatchTreePaused)
			{
				return processMode != ProcessModeEnum.WhenPaused;
			}
			if (processMode != ProcessModeEnum.Always)
			{
				return processMode == ProcessModeEnum.WhenPaused;
			}
			return true;
		case ProcessModeEnum.Inherit:
		{
			ProcessModeEnum processModeEnum = ResolveInheritedProcessMode(node);
			if (processModeEnum == ProcessModeEnum.Disabled)
			{
				return false;
			}
			ProcessModeEnum processModeEnum2 = processModeEnum;
			if (!dispatchTreePaused)
			{
				return processModeEnum2 != ProcessModeEnum.WhenPaused;
			}
			if (processModeEnum2 != ProcessModeEnum.Always)
			{
				return processModeEnum2 == ProcessModeEnum.WhenPaused;
			}
			return true;
		}
		}
	}

	private ProcessModeEnum ResolveInheritedProcessMode(Node node)
	{
		Node parent = node.GetParent();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return ProcessModeEnum.Pausable;
		}
		if (_processModeParentCache.TryGetValue(parent, out var value))
		{
			return value;
		}
		ProcessModeEnum processModeEnum = ResolveEffectiveProcessMode(parent);
		_processModeParentCache[parent] = processModeEnum;
		return processModeEnum;
	}

	private static ProcessModeEnum ResolveEffectiveProcessMode(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			ProcessModeEnum processMode = node2.ProcessMode;
			if (processMode != ProcessModeEnum.Inherit)
			{
				return processMode;
			}
			node2 = node2.GetParent();
		}
		return ProcessModeEnum.Pausable;
	}

	private void FlushPending()
	{
		if (_toRemove.Count > 0)
		{
			foreach (AdobeAnimateSprite item in _toRemove)
			{
				if (_activeIndices.TryGetValue(item, out var value))
				{
					RemoveActiveAt(value);
				}
				_registered.Remove(item);
				_pauseDispatchDirty.Remove(item);
			}
			_toRemove.Clear();
		}
		if (_toAdd.Count <= 0)
		{
			return;
		}
		foreach (AdobeAnimateSprite item2 in _toAdd)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				AddActive(item2);
			}
			else
			{
				_registered.Remove(item2);
			}
		}
		_toAdd.Clear();
	}

	private void AddActive(AdobeAnimateSprite sprite)
	{
		if (!_activeIndices.ContainsKey(sprite))
		{
			_activeIndices[sprite] = _active.Count;
			_active.Add(sprite);
			_pauseDispatchDirty.Add(sprite);
			_renderRootsDirty = true;
		}
	}

	private void RemoveActiveAt(int index)
	{
		int num = _active.Count - 1;
		if ((uint)index <= (uint)num)
		{
			AdobeAnimateSprite adobeAnimateSprite = _active[index];
			if (index != num)
			{
				AdobeAnimateSprite adobeAnimateSprite2 = _active[num];
				_active[index] = adobeAnimateSprite2;
				_activeIndices[adobeAnimateSprite2] = index;
			}
			_active.RemoveAt(num);
			_activeIndices.Remove(adobeAnimateSprite);
			_pauseDispatchDirty.Remove(adobeAnimateSprite);
			if (_pauseDispatchValidationCursor > _active.Count)
			{
				_pauseDispatchValidationCursor = 0;
			}
			_renderRootsDirty = true;
		}
	}

	internal void SetAnimationClockForBareTest(double seconds)
	{
		_animationClockSeconds = seconds;
	}

	internal static bool TryCopyRenderRootOrderForTests(Span<AdobeAnimateSprite> destination, out int totalCount, out bool dirty)
	{
		totalCount = 0;
		dirty = true;
		AdobeAnimateRuntimeManager instance = Instance;
		if (instance == null)
		{
			return false;
		}
		totalCount = instance._renderRoots.Count;
		dirty = instance._renderRootsDirty;
		int num = Math.Min(totalCount, destination.Length);
		for (int i = 0; i < num; i++)
		{
			destination[i] = instance._renderRoots[i];
		}
		return true;
	}
}
