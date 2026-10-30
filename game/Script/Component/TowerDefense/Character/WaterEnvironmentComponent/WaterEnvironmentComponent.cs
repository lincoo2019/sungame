using System;
using Godot;
using Godot.Collections;

public sealed class WaterEnvironmentComponent : CharacterComponentRuntime
{
	public AdobeAnimateSpriteBase waterLineSprite;

	public float waterHeight = 35f;

	public string waterIdleAnime = "";

	public float groundHeightLerpSpeed = 3f;

	public int environmentCheckInterval = 5;

	public float entryDiscardOffset = 45f;

	public float exitDiscardStartOffset = 56f;

	public float exitDiscardEndOffset = 86f;

	public float exitDuration = 1f;

	public float discardResetPosition = 10000f;

	public StringName discardShaderParameter = "discardDownPos";

	public bool createEntrySplash = true;

	public bool restoreIdleOnExit = true;

	public Tween.EaseType exitEase = Tween.EaseType.Out;

	public Tween.TransitionType exitTransition = Tween.TransitionType.Cubic;

	public string saveIdleAnime = "";

	public TowerDefenseCharacter parent;

	private bool _inWater;

	private Vector2 _gridSize;

	private bool _parentReadyConnected;

	private Tween _exitTween;

	private Action _exitFinishedHandler;

	private bool _visualStateApplied;

	private bool _visualInWater;

	private bool _configured;

	private bool _ownerInitialized;

	private NodePath _waterLineSpritePath = new NodePath();

	private string _discardShaderProperty = "discardDownPos";

	private Dictionary _pendingSyncData;

	private readonly Dictionary _syncPayload = new Dictionary();

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private bool _syncPayloadInitialized;

	private bool _syncPayloadHasParent;

	private bool _syncPayloadInWater;

	private float _syncPayloadHeight;

	private WaterEnvironmentComponentDefinition Definition => base.ComponentDefinition as WaterEnvironmentComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	public bool inWater
	{
		get
		{
			return _inWater;
		}
		set
		{
			SetInWater(value);
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveOwnerReferences();
		ArmParentInitialization();
	}

	protected override void OnActivated()
	{
		ResolveOwnerReferences();
		ArmParentInitialization();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			InitializeAfterParentReady();
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CleanupOwnerState(clearReferences: true);
		ClearSyncPayload();
	}

	protected override void OnReleased()
	{
		CleanupOwnerState(clearReferences: true);
		_pendingSyncData = null;
		ClearSyncPayload();
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CleanupOwnerState(clearReferences: false);
			return;
		}
		ResolveOwnerReferences();
		ArmParentInitialization();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			InitializeAfterParentReady();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (TryGetActiveParent(out var _))
		{
			ProcessWater(delta, physicsFrame);
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			WaterEnvironmentComponentDefinition definition = Definition;
			_waterLineSpritePath = definition?.waterLineSpritePath ?? new NodePath();
			waterHeight = definition?.waterHeight ?? 35f;
			waterIdleAnime = definition?.waterIdleAnime ?? "";
			groundHeightLerpSpeed = definition?.groundHeightLerpSpeed ?? 3f;
			environmentCheckInterval = Math.Max(1, definition?.environmentCheckInterval ?? 5);
			entryDiscardOffset = definition?.entryDiscardOffset ?? 45f;
			exitDiscardStartOffset = definition?.exitDiscardStartOffset ?? 56f;
			exitDiscardEndOffset = definition?.exitDiscardEndOffset ?? 86f;
			exitDuration = Math.Max(0f, definition?.exitDuration ?? 1f);
			discardResetPosition = definition?.discardResetPosition ?? 10000f;
			discardShaderParameter = definition?.discardShaderParameter ?? new StringName("discardDownPos");
			_discardShaderProperty = discardShaderParameter.ToString();
			createEntrySplash = definition?.createEntrySplash ?? true;
			restoreIdleOnExit = definition?.restoreIdleOnExit ?? true;
			exitEase = definition?.exitEase ?? Tween.EaseType.Out;
			exitTransition = definition?.exitTransition ?? Tween.TransitionType.Cubic;
			_configured = true;
		}
	}

	private void ResolveOwnerReferences()
	{
		waterLineSprite = ((GodotObject.IsInstanceValid(parent) && !_waterLineSpritePath.IsEmpty) ? parent.GetNodeOrNull<AdobeAnimateSpriteBase>(_waterLineSpritePath) : null);
	}

	private void ArmParentInitialization()
	{
		if (base.Alive && TryGetBoundParent(out var character))
		{
			if (character.IsNodeReady())
			{
				InitializeAfterParentReady();
			}
			else if (!_parentReadyConnected)
			{
				character.Ready += InitializeAfterParentReady;
				_parentReadyConnected = true;
			}
		}
	}

	private void InitializeAfterParentReady()
	{
		DisconnectParentReady();
		if (base.Alive && TryGetBoundParent(out var character))
		{
			ResolveOwnerReferences();
			if (!_ownerInitialized)
			{
				saveIdleAnime = character.idleAnimeClip;
				RefreshGridSize();
				_ownerInitialized = true;
			}
			if (_pendingSyncData != null)
			{
				ApplyPendingSyncData();
			}
			else if (!IsRemoteSyncedClient(character))
			{
				UpdateIsInWater();
			}
			if (base.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				ApplyVisualState(character);
			}
		}
	}

	private void DisconnectParentReady()
	{
		if (_parentReadyConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= InitializeAfterParentReady;
		}
		_parentReadyConnected = false;
	}

	private void CleanupOwnerState(bool clearReferences)
	{
		DisconnectParentReady();
		CancelExitTween(resetDiscard: true);
		if (GodotObject.IsInstanceValid(waterLineSprite))
		{
			waterLineSprite.Visible = false;
		}
		RestoreSavedIdleClip();
		_gridSize = Vector2.Zero;
		_visualStateApplied = false;
		_visualInWater = false;
		_ownerInitialized = false;
		if (clearReferences)
		{
			waterLineSprite = null;
			parent = null;
		}
	}

	private void RestoreSavedIdleClip()
	{
		if (restoreIdleOnExit && GodotObject.IsInstanceValid(parent) && !string.IsNullOrEmpty(saveIdleAnime) && !(parent.idleAnimeClip == saveIdleAnime))
		{
			parent.idleAnimeClip = saveIdleAnime;
		}
	}

	private void ProcessWater(double delta, ulong physicsFrame)
	{
		if (TryGetActiveParent(out var character))
		{
			if (!IsRemoteSyncedClient(character) && ShouldCheckEnvironment(character, physicsFrame))
			{
				UpdateIsInWater();
			}
			ApplyVisualState(character);
			float num = ResolveTargetGroundHeight(character);
			float num2 = (float)character.groundHeight;
			if (!Mathf.IsEqualApprox(num2, num))
			{
				float num3 = Mathf.Max(0f, groundHeightLerpSpeed);
				float weight = ((num3 <= 0f) ? 1f : (1f - Mathf.Exp((0f - num3) * Mathf.Max(0f, (float)delta))));
				character.groundHeight = Mathf.Lerp(num2, num, weight);
			}
		}
	}

	private float ResolveTargetGroundHeight(TowerDefenseCharacter character)
	{
		if (_inWater)
		{
			return 0f - waterHeight;
		}
		if (!GodotObject.IsInstanceValid(character.cell))
		{
			return 0f;
		}
		if (character is TowerDefenseZombie && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			if (Mathf.IsZeroApprox(_gridSize.X))
			{
				RefreshGridSize();
			}
			if (!Mathf.IsZeroApprox(_gridSize.X))
			{
				Vector2 mapCellPos = TowerDefenseManager.Instance.GetMapCellPos(character.gridPos);
				character.cellPercentage = (character.GetLogicalGlobalPosition().X - mapCellPos.X) / _gridSize.X;
			}
		}
		return (float)character.cell.GetGroundHeight(character.cellPercentage);
	}

	private void RefreshGridSize()
	{
		_gridSize = (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) ? TowerDefenseManager.Instance.GetMapGridSize() : Vector2.Zero);
	}

	public void UpdateIsInWater()
	{
		if (TryGetUsableParent(out var character))
		{
			bool flag = false;
			if (GodotObject.IsInstanceValid(character.cell) && character.cell.isWater)
			{
				character.cell.slot.TryGetValue(TowerDefenseEnum.PLANTGRIDTYPE.WATER, out var value);
				flag = !GodotObject.IsInstanceValid(value) || value == character;
			}
			SetInWater(flag);
		}
	}

	public void SetInWater(bool value)
	{
		if (TryGetUsableParent(out var character))
		{
			bool num = _inWater != value;
			_inWater = value;
			character.inWater = value;
			if (num && base.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				ApplyVisualState(character);
			}
		}
	}

	private void ApplyVisualState(TowerDefenseCharacter character)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && character.inGame && (!_visualStateApplied || _visualInWater != _inWater))
		{
			if (_inWater)
			{
				InWater();
			}
			else if (_visualStateApplied)
			{
				OutWater();
			}
			_visualInWater = _inWater;
			_visualStateApplied = true;
		}
	}

	private bool ShouldCheckEnvironment(TowerDefenseCharacter character, ulong physicsFrame)
	{
		int num = Math.Max(1, environmentCheckInterval);
		if (num == 1)
		{
			return true;
		}
		ulong num2 = (ulong)Math.Max(0, character.randFreshIndex);
		return (physicsFrame + num2) % (ulong)num == 0;
	}

	private static bool IsRemoteSyncedClient(TowerDefenseCharacter character)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return character.syncId >= 0;
		}
		return false;
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = GodotObject.IsInstanceValid(parent) && parent.syncId >= 0;
		float num = (flag ? Mathf.Snapped((float)parent.groundHeight, 0.1f) : 0f);
		if (_syncPayloadInitialized)
		{
			int num2 = (flag ? 2 : 0);
			if (_syncPayload.Count == num2 + 1)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == num2 && _syncPayloadHasParent == flag && (!flag || (_syncPayloadInWater == _inWater && _syncPayloadHeight == num)))
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		if (flag)
		{
			_syncPayload["water"] = _inWater;
			_syncPayload["height"] = num;
		}
		_syncPayloadHasParent = flag;
		_syncPayloadInWater = _inWater;
		_syncPayloadHeight = num;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPayloadInitialized = false;
		_syncPayloadHasParent = false;
		_syncPayloadInWater = false;
		_syncPayloadHeight = 0f;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null)
		{
			if (!TryGetUsableParent(out var character) || !character.IsNodeReady() || base.Lifecycle != ComponentRuntimeLifecycle.Active)
			{
				_pendingSyncData = data.Duplicate(deep: true);
			}
			else
			{
				ApplySyncData(data, character);
			}
		}
	}

	private void ApplyPendingSyncData()
	{
		if (_pendingSyncData != null && TryGetUsableParent(out var character) && character.IsNodeReady())
		{
			Dictionary pendingSyncData = _pendingSyncData;
			_pendingSyncData = null;
			ApplySyncData(pendingSyncData, character);
		}
	}

	private void ApplySyncData(Dictionary data, TowerDefenseCharacter character)
	{
		if (data.ContainsKey("height"))
		{
			character.groundHeight = data["height"].AsDouble();
		}
		SetInWater(data.GetValueOrDefault("water", _inWater).AsBool());
	}

	public void InWater()
	{
		if (TryGetActiveParent(out var character))
		{
			CancelExitTween(resetDiscard: false);
			if (!string.IsNullOrEmpty(waterIdleAnime))
			{
				character.idleAnimeClip = waterIdleAnime;
			}
			SetDiscardDownPos(GetViewportDiscardPosition(entryDiscardOffset));
			if (createEntrySplash)
			{
				character.CreateSplash();
			}
			if (GodotObject.IsInstanceValid(waterLineSprite))
			{
				waterLineSprite.Visible = true;
			}
		}
	}

	public void OutWater()
	{
		if (TryGetActiveParent(out var character))
		{
			CancelExitTween(resetDiscard: false);
			if (restoreIdleOnExit && character.idleAnimeClip != saveIdleAnime)
			{
				character.idleAnimeClip = saveIdleAnime;
				character.Idle();
			}
			float viewportDiscardPosition = GetViewportDiscardPosition(exitDiscardStartOffset);
			float viewportDiscardPosition2 = GetViewportDiscardPosition(exitDiscardEndOffset);
			SetDiscardDownPos(viewportDiscardPosition);
			if (GodotObject.IsInstanceValid(waterLineSprite))
			{
				waterLineSprite.Visible = false;
			}
			if (exitDuration <= 0f)
			{
				SetDiscardDownPos(discardResetPosition);
				return;
			}
			_exitTween = parent.CreateTween();
			_exitTween.SetEase(exitEase);
			_exitTween.SetTrans(exitTransition);
			_exitFinishedHandler = OnExitTweenFinished;
			_exitTween.Finished += _exitFinishedHandler;
			_exitTween.TweenMethod(Callable.From<double>(SetDiscardDownPos), viewportDiscardPosition, viewportDiscardPosition2, exitDuration);
		}
	}

	private void OnExitTweenFinished()
	{
		SetDiscardDownPos(discardResetPosition);
		CleanupExitTween(killTween: false);
	}

	private void CancelExitTween(bool resetDiscard)
	{
		CleanupExitTween(killTween: true);
		if (resetDiscard)
		{
			SetDiscardDownPos(discardResetPosition);
		}
	}

	private void CleanupExitTween(bool killTween)
	{
		if (GodotObject.IsInstanceValid(_exitTween) && _exitFinishedHandler != null)
		{
			_exitTween.Finished -= _exitFinishedHandler;
		}
		if (killTween && GodotObject.IsInstanceValid(_exitTween))
		{
			_exitTween.Kill();
		}
		_exitTween = null;
		_exitFinishedHandler = null;
	}

	private float GetViewportDiscardPosition(float offset)
	{
		if (!TryGetActiveParent(out var character))
		{
			return discardResetPosition;
		}
		Viewport viewport = character.GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return discardResetPosition;
		}
		Transform2D screenTransform = viewport.GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		return (screenTransform * (character.GetLogicalGlobalPosition(character.spriteGroup) + new Vector2(0f, offset))).Y;
	}

	private void SetDiscardDownPos(double value)
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.spriteGroup) && !string.IsNullOrEmpty(_discardShaderProperty))
		{
			parent.SetSpriteGroupShaderParameter(_discardShaderProperty, (float)value);
		}
	}

	private bool TryGetBoundParent(out TowerDefenseCharacter character)
	{
		character = parent;
		ComponentRuntimeLifecycle lifecycle = base.Lifecycle;
		bool flag = (uint)(lifecycle - 1) <= 1u;
		if (flag && GodotObject.IsInstanceValid(character))
		{
			return GodotObject.IsInstanceValid(character.spriteGroup);
		}
		return false;
	}

	private bool TryGetUsableParent(out TowerDefenseCharacter character)
	{
		character = null;
		if (base.Alive)
		{
			return TryGetBoundParent(out character);
		}
		return false;
	}

	private bool TryGetActiveParent(out TowerDefenseCharacter character)
	{
		character = null;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return TryGetBoundParent(out character);
		}
		return false;
	}
}
