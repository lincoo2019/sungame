using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class Test : Node2D
{
	private sealed class ExternalVisualStressFixture
	{
		public TowerDefenseCharacter Character;

		public AdobeAnimateSprite Owner;

		public Sprite2D Visual;

		public AdobeAnimateExternalVisualHandle Handle = AdobeAnimateExternalVisualHandle.Invalid;

		public bool OwnsVisual;

		public int StableIndex;
	}

	private struct RenderManagerStats
	{
		public int RenderManagers;

		public int NormalBuckets;

		public int VisibleInstances;

		public int AllocatedInstances;

		public int MaxBucketVisible;

		public int CompactCrowdRoots;

		public int CompositeCrowdRoots;

		public int GpuGraphRoots;

		public int GpuGraphSlots;

		public int GpuGraphStateTexels;

		public int CrowdStateTexels;
	}

	private const string DefaultZombieAnimationSceneUid = "uid://d1jj5qhp7tex4";

	private const string DefaultZombieCharacterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string DefaultPlantCombatTargetScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const string HypnotistCharacterScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn";

	private const string ExternalVisualStressTexturePath = "res://Asset/Texture/Character/Effect/ButterSplat.png";

	private const double CombatStressDurableHitpoints = 1000000000000.0;

	private const double CombatStressTargetDurableHitpoints = 1000000000000.0;

	private AdobeAnimateRenderBackend _testRenderBackend;

	private readonly List<Node2D> _spawnedNodes = new List<Node2D>();

	private readonly List<TowerDefenseCharacter> _spawnedCharacters = new List<TowerDefenseCharacter>();

	private readonly List<string> _spawnedCharacterScenePaths = new List<string>();

	private readonly List<ProcessModeEnum> _spawnedCharacterProcessModes = new List<ProcessModeEnum>();

	private readonly List<int> _spawnedCharacterSleepBuffFlags = new List<int>();

	private readonly List<AdobeAnimateSpriteBase> _spawnedAnimationSprites = new List<AdobeAnimateSpriteBase>();

	private readonly HashSet<AttackComponent> _engagedAttackComponents = new HashSet<AttackComponent>();

	private readonly HashSet<FireComponent> _engagedFireComponents = new HashSet<FireComponent>();

	private readonly HashSet<AttackComponent> _exercisedAttackUtilityComponents = new HashSet<AttackComponent>();

	private readonly List<(TowerDefenseZombieSoccer Role, AttackComponent Component)> _pendingSoccerAttackUtilityComponents = new List<(TowerDefenseZombieSoccer, AttackComponent)>();

	private readonly List<TowerDefenseCharacter> _manyEnemyIdleTargets = new List<TowerDefenseCharacter>();

	private readonly List<AttackComponent> _manyEnemyIdleAttackComponents = new List<AttackComponent>();

	private readonly List<FireComponent> _manyEnemyIdleFireComponents = new List<FireComponent>();

	private readonly Dictionary<AttackComponent, AttackComponent.AttackEventHandler> _attackEngagementHandlers = new Dictionary<AttackComponent, AttackComponent.AttackEventHandler>();

	private readonly Dictionary<StateHandle, Action> _legacyAttackEngagementHandlers = new Dictionary<StateHandle, Action>();

	private readonly Dictionary<FireComponent, FireComponent.FireVolleyEventHandler> _fireEngagementHandlers = new Dictionary<FireComponent, FireComponent.FireVolleyEventHandler>();

	private readonly List<PackedScene> _scenePool = new List<PackedScene>();

	private readonly List<ExternalVisualStressFixture> _externalVisualStressFixtures = new List<ExternalVisualStressFixture>();

	private readonly List<string> _scenePoolPaths = new List<string>();

	private readonly List<double> _measurementFrameSeconds = new List<double>(131072);

	private readonly List<double> _measurementSimulationDeltaSeconds = new List<double>(131072);

	private readonly List<double> _measurementProcessMilliseconds = new List<double>(131072);

	private readonly List<double> _measurementPhysicsMilliseconds = new List<double>(131072);

	private readonly List<double> _spawnInstantiateMilliseconds = new List<double>(10000);

	private readonly List<double> _spawnAddChildReadyMilliseconds = new List<double>(10000);

	private readonly List<double> _spawnBatchFrameMilliseconds = new List<double>(2048);

	private readonly List<double> _viewportEntryFrameSeconds = new List<double>(128);

	private readonly List<double> _animationSwitchWindowFrameSeconds = new List<double>(4096);

	private readonly List<double> _animationSwitchSteadyFrameSeconds = new List<double>(4096);

	private readonly List<double> _bowlingImpactWindowFrameSeconds = new List<double>(4096);

	private readonly List<double> _bowlingImpactSteadyFrameSeconds = new List<double>(4096);

	private readonly Stopwatch _uptime = new Stopwatch();

	private int[] _sceneUseCounts = Array.Empty<int>();

	private double _printTimer;

	private double _windowDeltaSeconds;

	private double _sampleStartSeconds;

	private int _windowFrames;

	private bool _runtimeCrowdFilterDebugPrinted;

	private bool _crowdModeReportPrinted;

	private int _spawnFailures;

	private int _loadedScenePoolCount;

	private int _skippedScenePoolCount;

	private int _excludedScenePoolCount;

	private int _nextSpawnIndex;

	private int _nextSpawnProgressPrint;

	private long _spawnStartTicks;

	private double _loadMs;

	private bool _spawnNoGcRegionActive;

	private PackedScene _pendingSpawnFallbackScene;

	private bool _spawnPending;

	private bool _spawnFrameBudgetPassed = true;

	private bool _autoQuitPrinted;

	private bool _measurementStarted;

	private int _measurementFrames;

	private TowerDefenseCharacter _singleEnemyTarget;

	private int _singleEnemyAttackComponentCount;

	private int _singleEnemyAttackUtilityComponentCount;

	private int _singleEnemyFireComponentCount;

	private double _singleEnemyTargetInitialHitpoints;

	private bool _singleEnemyFixturePrepared;

	private bool _singleEnemyRobotModeAdvanceRequested;

	private bool _singleEnemyRoleUtilityPhaseStarted;

	private bool _singleEnemyRoleUtilityPhaseCompleted;

	private bool _manyEnemyIdleFixturePrepared;

	private double _nextSingleEnemyRoleUtilityAuditSeconds;

	private int _sleepCyclePhase;

	private double _sleepCyclePhaseDeadlineSeconds;

	private bool _sleepCycleCompleted;

	private bool _sleepCyclePassed;

	private int _sleepCycleEligibleCount;

	private int _sleepCycleAppliedCount;

	private int _sleepCycleEnteredCount;

	private int _sleepCycleWokenCount;

	private int _sleepCycleNormalizedImmunityCount;

	private bool _sleepCycleImmunityRestored;

	private TowerDefenseZombieHypnotist _sleepCycleHypnotistEventProvider;

	private double _measurementStartSeconds;

	private double _measurementEndSeconds = 0.0 / 0.0;

	private long _measurementPreviousFrameTicks;

	private bool _screenshotRequested;

	private bool _screenshotSaved;

	private bool _resultPrinted;

	private bool _performanceResultPassed;

	private bool _animeFrameRateSwitchCompleted;

	private bool _animeFrameRateSwitchPassed = true;

	private bool _runtimeVisualMutationCompleted;

	private bool _runtimeVisualMutationPassed = true;

	private bool _animationSwitchUseSecondClip;

	private double _nextAnimationSwitchSeconds;

	private double _animationSwitchWindowEndSeconds;

	private int _animationSwitchCount;

	private int _animationSwitchAppliedCount;

	private int _animationSwitchMissingClipCount;

	private double _animationSwitchSynchronousTotalMilliseconds;

	private double _animationSwitchSynchronousMaxMilliseconds;

	private double _nextBowlingImpactSeconds;

	private double _bowlingImpactWindowEndSeconds;

	private int _bowlingImpactBurstCount;

	private int _bowlingImpactAppliedCount;

	private int _bowlingImpactMissingShieldCount;

	private int _bowlingImpactMaxProcessedTweens;

	private int _bowlingImpactMaxBatchedMotions;

	private int _bowlingImpactMaxBatchedShieldVisuals;

	private double _bowlingImpactSynchronousTotalMilliseconds;

	private double _bowlingImpactSynchronousMaxMilliseconds;

	private long _bowlingImpactThreadAllocatedBytes;

	private int _viewportEntryCaptureFramesRemaining;

	private bool _viewportEntryCaptureSkipCurrentFrame;

	private bool _viewportEntryCapturePrinted;

	private long _measurementThreadAllocatedBefore;

	private long _measurementTotalAllocatedBefore;

	private int _measurementGen0Before;

	private int _measurementGen1Before;

	private int _measurementGen2Before;

	private AdobeAnimateSpriteBase _cadenceSprite;

	private double _cadencePreviousPhase;

	private int _cadenceSamples;

	private int _cadenceAdvances;

	private AdobeAnimateSlot _cadenceSlot;

	private int _cadencePreviousSlotVersion;

	private int _cadenceSlotAdvances;

	private Polygon2D _zInterleaveMarker;

	private CpuParticles2D _zInterleaveParticles;

	private ImageTexture _zInterleaveParticleTexture;

	private int _externalVisualExpectedActive;

	private int _externalVisualConfigurationFailures;

	private long _externalVisualGraphBuildCountAtMeasurementStart;

	private long _gpuGraphInitialBuildCountAtMeasurementStart;

	private long _gpuGraphRebuildCountAtMeasurementStart;

	private long _gpuGraphOwnerExitInvalidationCountAtMeasurementStart;

	private long _gpuGraphManagedSlotInvalidationCountAtMeasurementStart;

	private long _gpuGraphExternalVisualInvalidationCountAtMeasurementStart;

	private Texture2D _externalVisualStressTexture;

	private int _originalMaxFps;

	private int _originalPhysicsTicksPerSecond;

	private bool _invalidTestRenderBackend;

	private double _spawnInstantiateMaxMilliseconds;

	private string _spawnInstantiateMaxScene = "";

	private double _spawnAddChildReadyMaxMilliseconds;

	private string _spawnAddChildReadyMaxScene = "";

	private int _adobeFixedAnimationFrame = -1;

	private bool _invalidAdobeFixedAnimationFrame;

	private OptionButton _renderBackendOption;

	private Label _renderBackendStatus;

	private double _renderBackendUiRefreshTimer;

	private CharacterStressControlStub _combatStressControl;

	private TowerDefenseBattleFeatureMap _combatStressMapFeature;

	private TowerDefenseMapControl _combatStressMapControl;

	private TowerDefenseControlNew _previousControl;

	private Vector2 _previousGridBegin;

	private Vector2 _previousGridSize;

	private Vector2I _previousGridNum;

	private bool _previousBackZombie;

	private bool _combatStressConfigured;

	private bool _resourceManagerLoadGatePending;

	private bool _originalLowProcessorUsageMode;

	private bool _unfocusedRunConfigured;

	[Export(PropertyHint.None, "")]
	public PackedScene TestScene { get; set; }

	[Export(PropertyHint.None, "")]
	public string ExactScenePath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public string ExactAnimationClip { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public int SpawnCount { get; set; } = 10000;

	[Export(PropertyHint.None, "")]
	public Vector2 SpawnOrigin { get; set; } = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public Vector2 SpawnArea { get; set; } = new Vector2(1080f, 600f);

	[Export(PropertyHint.None, "")]
	public bool UseDifferentZombieScenePool { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool UseZombieCharacterScenes { get; set; }

	[Export(PropertyHint.None, "")]
	public string ZombieSceneRoot { get; set; } = "res://Asset/Anime/Character/Zombie";

	[Export(PropertyHint.None, "")]
	public string CharacterSceneFilePrefix { get; set; } = "TowerDefenseZombie";

	[Export(PropertyHint.None, "")]
	public string ExcludedZombieScenePathSubstring { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public int MaxDifferentZombieScenes { get; set; }

	[Export(PropertyHint.None, "")]
	public int SpawnBatchSize { get; set; }

	[Export(PropertyHint.None, "")]
	public double SpawnTimeBudgetMilliseconds { get; set; }

	[Export(PropertyHint.None, "")]
	public bool UseGridSpawnLayout { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int SpawnGridColumns { get; set; }

	[Export(PropertyHint.None, "")]
	public int RenderZBucketCount { get; set; }

	[Export(PropertyHint.None, "")]
	public float SpawnScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public ulong SpawnRandomSeed { get; set; } = 10000uL;

	[Export(PropertyHint.None, "")]
	public double AutoQuitAfterSeconds { get; set; }

	[Export(PropertyHint.None, "")]
	public double WarmupSeconds { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public double MinimumAverageFps { get; set; }

	[Export(PropertyHint.None, "")]
	public double MaximumFrameP99Milliseconds { get; set; }

	[Export(PropertyHint.None, "")]
	public double MaximumFrameMilliseconds { get; set; }

	[Export(PropertyHint.None, "")]
	public double MinimumOnePercentLowFps { get; set; }

	[Export(PropertyHint.None, "")]
	public int BenchmarkMaxFps { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public int BenchmarkPhysicsTicksPerSecond { get; set; }

	[Export(PropertyHint.None, "")]
	public string ScreenshotPath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public bool WaitForDamagePartSettledBeforeScreenshot { get; set; }

	[Export(PropertyHint.None, "")]
	public float PrintIntervalSeconds { get; set; } = 60f;

	[Export(PropertyHint.None, "")]
	public bool EnableAdobeAnimateProfiler { get; set; }

	[Export(PropertyHint.None, "")]
	public bool EnableDetailedAdobeAnimateProfiler { get; set; }

	[Export(PropertyHint.None, "")]
	public bool EnableAllocationTelemetry { get; set; }

	[Export(PropertyHint.None, "")]
	public int ProfilerDumpIntervalFrames { get; set; } = 120;

	[Export(PropertyHint.None, "")]
	public int ProfilerMaxMetricsPerDump { get; set; } = 96;

	[Export(PropertyHint.None, "")]
	public bool ClearAdobeAnimateRuntimeCachesBeforeRun { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool EnableGpuRenderGraph { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool ForceCpuPoseFallbackForTest { get; set; }

	[Export(PropertyHint.None, "")]
	public int FixedAnimationFrame { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public double FixedAnimationSubframe { get; set; }

	[Export(PropertyHint.None, "")]
	public bool PrintCrowdFilterDebugData { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int CrowdFilterDebugSpriteCount { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public int CrowdFilterDebugMaxFrameSlices { get; set; } = 256;

	[Export(PropertyHint.None, "")]
	public bool PrintCrowdFilterDebugAfterFirstRuntimeFrame { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool PrintCrowdFilterDebugEveryRuntimeSnapshot { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool EnableZInterleaveFixture { get; set; }

	[Export(PropertyHint.None, "")]
	public bool PrintCrowdModeReport { get; set; }

	[Export(PropertyHint.None, "")]
	public double InitialAnimeFrameRate { get; set; }

	[Export(PropertyHint.None, "")]
	public double SwitchAnimeFrameRateAfterSeconds { get; set; } = -1.0;

	[Export(PropertyHint.None, "")]
	public double SwitchAnimeFrameRateTo { get; set; }

	[Export(PropertyHint.None, "")]
	public string RuntimeVisualMutation { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public double RuntimeVisualMutationAfterSeconds { get; set; } = -1.0;

	[Export(PropertyHint.None, "")]
	public bool EnableAnimationSwitchStress { get; set; }

	[Export(PropertyHint.None, "")]
	public string AnimationSwitchClipA { get; set; } = "Walk1";

	[Export(PropertyHint.None, "")]
	public string AnimationSwitchClipB { get; set; } = "Eat";

	[Export(PropertyHint.None, "")]
	public double AnimationSwitchIntervalSeconds { get; set; } = 0.75;

	[Export(PropertyHint.None, "")]
	public double AnimationSwitchBlendSeconds { get; set; } = 0.2;

	[Export(PropertyHint.None, "")]
	public double AnimationSwitchStartAfterSeconds { get; set; } = 5.0;

	[Export(PropertyHint.None, "")]
	public bool EnableBowlingImpactTweenStress { get; set; }

	[Export(PropertyHint.None, "")]
	public bool BowlingImpactEnsureShield { get; set; }

	[Export(PropertyHint.None, "")]
	public bool BowlingImpactUseProductionEvent { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int BowlingImpactProcessedTweenLimit { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public int BowlingImpactTargetsPerBurst { get; set; }

	[Export(PropertyHint.None, "")]
	public double BowlingImpactDamage { get; set; } = 1.0;

	[Export(PropertyHint.None, "")]
	public double BowlingImpactBurstIntervalSeconds { get; set; } = 0.75;

	[Export(PropertyHint.None, "")]
	public double BowlingImpactActiveWindowSeconds { get; set; } = 0.4;

	[Export(PropertyHint.None, "")]
	public double BowlingImpactStartAfterSeconds { get; set; } = 5.0;

	[Export(PropertyHint.None, "")]
	public bool RunComponentAliveSchedulingRegression { get; set; }

	[Export(PropertyHint.None, "")]
	public bool RunStatePhysicsBatchRateRegression { get; set; }

	[Export(PropertyHint.None, "")]
	public bool RunAnimationCadenceRegression { get; set; }

	[Export(PropertyHint.None, "")]
	public bool EnableExternalVisualStress { get; set; }

	[Export(PropertyHint.None, "")]
	public bool ExternalVisualIceOnly { get; set; }

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double ExternalVisualRatio { get; set; } = 1.0;

	[Export(PropertyHint.None, "")]
	public bool EnableCombatStateStress { get; set; }

	[Export(PropertyHint.None, "")]
	public bool PreserveCombatCharactersInStress { get; set; }

	[Export(PropertyHint.None, "")]
	public bool CombatStressIsNight { get; set; }

	[Export(PropertyHint.None, "")]
	public bool ShowCharacterHealthInStress { get; set; }

	[Export(PropertyHint.None, "")]
	public bool EnableSingleEnemyTargetFixture { get; set; }

	[Export(PropertyHint.None, "")]
	public string SingleEnemyTargetScenePath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public float SingleEnemyTargetScale { get; set; } = 4f;

	[Export(PropertyHint.None, "")]
	public bool EnableSleepCycleStress { get; set; }

	[Export(PropertyHint.None, "")]
	public double SleepCycleStartAfterSeconds { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public bool EnableManyEnemyIdleFixture { get; set; }

	[Export(PropertyHint.None, "")]
	public int ManyEnemyIdleTargetCount { get; set; } = 1000;

	[Export(PropertyHint.None, "")]
	public bool ManyEnemyIdleTargetsOutsideAttackRange { get; set; }

	[Export(PropertyHint.None, "")]
	public bool WaitForResourceManagerLoad { get; set; }

	[Export(PropertyHint.None, "")]
	public bool KeepRunningWhenUnfocused { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string RuntimeProfile { get; set; } = "unknown";

	public override void _Ready()
	{
		_originalMaxFps = Engine.MaxFps;
		_originalPhysicsTicksPerSecond = Engine.PhysicsTicksPerSecond;
		_uptime.Restart();
		_spawnedNodes.Clear();
		_spawnedCharacters.Clear();
		_spawnedCharacterScenePaths.Clear();
		_spawnedCharacterProcessModes.Clear();
		_spawnedCharacterSleepBuffFlags.Clear();
		_spawnedAnimationSprites.Clear();
		_manyEnemyIdleTargets.Clear();
		_manyEnemyIdleAttackComponents.Clear();
		_manyEnemyIdleFireComponents.Clear();
		_scenePool.Clear();
		_scenePoolPaths.Clear();
		_externalVisualStressFixtures.Clear();
		_externalVisualStressTexture = null;
		_measurementFrameSeconds.Clear();
		_measurementSimulationDeltaSeconds.Clear();
		_measurementProcessMilliseconds.Clear();
		_measurementPhysicsMilliseconds.Clear();
		_spawnInstantiateMilliseconds.Clear();
		_spawnAddChildReadyMilliseconds.Clear();
		_viewportEntryFrameSeconds.Clear();
		_animationSwitchWindowFrameSeconds.Clear();
		_animationSwitchSteadyFrameSeconds.Clear();
		_bowlingImpactWindowFrameSeconds.Clear();
		_bowlingImpactSteadyFrameSeconds.Clear();
		_sceneUseCounts = Array.Empty<int>();
		_sampleStartSeconds = 0.0;
		_spawnFailures = 0;
		_loadedScenePoolCount = 0;
		_skippedScenePoolCount = 0;
		_excludedScenePoolCount = 0;
		_nextSpawnIndex = 0;
		_nextSpawnProgressPrint = 0;
		_spawnStartTicks = 0L;
		_loadMs = 0.0;
		_pendingSpawnFallbackScene = null;
		_spawnPending = false;
		_spawnFrameBudgetPassed = true;
		_runtimeCrowdFilterDebugPrinted = false;
		_crowdModeReportPrinted = false;
		_autoQuitPrinted = false;
		_measurementStarted = false;
		_measurementFrames = 0;
		_measurementStartSeconds = 0.0;
		_measurementEndSeconds = 0.0 / 0.0;
		_measurementPreviousFrameTicks = 0L;
		_singleEnemyRobotModeAdvanceRequested = false;
		_singleEnemyRoleUtilityPhaseStarted = false;
		_singleEnemyRoleUtilityPhaseCompleted = false;
		_manyEnemyIdleFixturePrepared = false;
		_nextSingleEnemyRoleUtilityAuditSeconds = 0.0;
		_exercisedAttackUtilityComponents.Clear();
		_pendingSoccerAttackUtilityComponents.Clear();
		_screenshotRequested = false;
		_screenshotSaved = false;
		_resultPrinted = false;
		_performanceResultPassed = false;
		_animeFrameRateSwitchCompleted = false;
		_animeFrameRateSwitchPassed = true;
		_runtimeVisualMutationCompleted = false;
		_runtimeVisualMutationPassed = true;
		_animationSwitchUseSecondClip = true;
		_nextAnimationSwitchSeconds = Math.Max(0.0, AnimationSwitchStartAfterSeconds);
		_animationSwitchWindowEndSeconds = -1.0 / 0.0;
		_animationSwitchCount = 0;
		_animationSwitchAppliedCount = 0;
		_animationSwitchMissingClipCount = 0;
		_animationSwitchSynchronousTotalMilliseconds = 0.0;
		_animationSwitchSynchronousMaxMilliseconds = 0.0;
		_nextBowlingImpactSeconds = Math.Max(0.0, BowlingImpactStartAfterSeconds);
		_bowlingImpactWindowEndSeconds = -1.0 / 0.0;
		_bowlingImpactBurstCount = 0;
		_bowlingImpactAppliedCount = 0;
		_bowlingImpactMissingShieldCount = 0;
		_bowlingImpactMaxProcessedTweens = 0;
		_bowlingImpactMaxBatchedMotions = 0;
		_bowlingImpactMaxBatchedShieldVisuals = 0;
		_bowlingImpactSynchronousTotalMilliseconds = 0.0;
		_bowlingImpactSynchronousMaxMilliseconds = 0.0;
		_bowlingImpactThreadAllocatedBytes = 0L;
		_measurementThreadAllocatedBefore = 0L;
		_measurementTotalAllocatedBefore = 0L;
		_measurementGen0Before = 0;
		_measurementGen1Before = 0;
		_measurementGen2Before = 0;
		_cadenceSprite = null;
		_cadencePreviousPhase = 0.0;
		_cadenceSamples = 0;
		_cadenceAdvances = 0;
		_cadenceSlot = null;
		_cadencePreviousSlotVersion = 0;
		_cadenceSlotAdvances = 0;
		_externalVisualExpectedActive = 0;
		_externalVisualConfigurationFailures = 0;
		_externalVisualGraphBuildCountAtMeasurementStart = 0L;
		_gpuGraphInitialBuildCountAtMeasurementStart = 0L;
		_gpuGraphRebuildCountAtMeasurementStart = 0L;
		_gpuGraphOwnerExitInvalidationCountAtMeasurementStart = 0L;
		_gpuGraphManagedSlotInvalidationCountAtMeasurementStart = 0L;
		_gpuGraphExternalVisualInvalidationCountAtMeasurementStart = 0L;
		ApplyCommandLineOverrides();
		ConfigureUnfocusedBenchmarkRun();
		_nextAnimationSwitchSeconds = Math.Max(0.0, AnimationSwitchStartAfterSeconds);
		_nextBowlingImpactSeconds = Math.Max(0.0, BowlingImpactStartAfterSeconds);
		if (_invalidTestRenderBackend || _invalidAdobeFixedAnimationFrame)
		{
			GetTree().Quit(1);
			return;
		}
		Global.Instance.adobeAnimateRenderBackend = _testRenderBackend;
		InitializeRenderBackendUi();
		Viewport viewport = GetViewport();
		if (GodotObject.IsInstanceValid(viewport))
		{
			RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), enable: true);
		}
		TowerDefenseAllocationTelemetry.Enabled = EnableAllocationTelemetry;
		TowerDefenseAllocationTelemetry.Reset();
		if (BenchmarkMaxFps >= 0)
		{
			base.ProcessPriority = 1000;
			Engine.MaxFps = BenchmarkMaxFps;
		}
		if (BenchmarkPhysicsTicksPerSecond > 0)
		{
			Engine.PhysicsTicksPerSecond = BenchmarkPhysicsTicksPerSecond;
		}
		if (RunComponentAliveSchedulingRegression)
		{
			RunComponentAliveSchedulingRegressionAsync();
		}
		else if (RunStatePhysicsBatchRateRegression)
		{
			RunStatePhysicsBatchRateRegressionAsync();
		}
		else if ((WaitForResourceManagerLoad || EnableSingleEnemyTargetFixture || EnableManyEnemyIdleFixture) && UseZombieCharacterScenes)
		{
			ResourceManager instance = ResourceManager.Instance;
			if (!GodotObject.IsInstanceValid(instance))
			{
				GD.PrintErr("[TestPerf] ResourceManager is unavailable; cannot run a fully initialized character-scene workload.");
				GetTree().Quit(1);
				return;
			}
			_resourceManagerLoadGatePending = true;
			instance.OnLoadOver += OnStressResourceManagerLoadOver;
			GD.Print("[TestPerf] waiting for ResourceManager before loading character scenes.");
			instance.BeginLoad();
		}
		else
		{
			StartStressWorkload();
		}
	}

	private void OnStressResourceManagerLoadOver()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.OnLoadOver -= OnStressResourceManagerLoadOver;
		}
		if (_resourceManagerLoadGatePending)
		{
			_resourceManagerLoadGatePending = false;
			if (!GodotObject.IsInstanceValid(instance))
			{
				GD.PrintErr("[TestPerf] ResourceManager became unavailable before the character workload started.");
				GetTree().Quit(1);
				return;
			}
			instance.RequireFullGameplayResourcesReady("OnStressResourceManagerLoadOver");
			GD.Print($"[TestPerf] full gameplay resources ready in {instance.FullGameplayResourceLoadMetrics.FullWallMilliseconds:F3} ms.");
			StartStressWorkload();
		}
	}

	private void StartStressWorkload()
	{
		ConfigureCombatStateStress();
		if (InitialAnimeFrameRate > 0.0 && Global.Instance != null)
		{
			Global.Instance.animeFrameRate = InitialAnimeFrameRate;
		}
		if (EnableZInterleaveFixture)
		{
			SpawnCount = 2;
			SpawnBatchSize = 0;
		}
		AdobeAnimateRenderManager.GpuRenderGraphEnabled = EnableGpuRenderGraph;
		if (ClearAdobeAnimateRuntimeCachesBeforeRun)
		{
			AdobeAnimateDefinitionCache.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			GD.Print("[TestPerf] AdobeAnimate runtime caches cleared before load.");
		}
		if (SpawnRandomSeed != 0L)
		{
			GD.Seed(SpawnRandomSeed);
		}
		if (EnableAdobeAnimateProfiler)
		{
			TowerDefensePerfProfiler.Enabled = true;
			TowerDefensePerfProfiler.DetailedHotPathMetrics = EnableDetailedAdobeAnimateProfiler;
			TowerDefensePerfProfiler.DumpIntervalFrames = Math.Max(1, ProfilerDumpIntervalFrames);
			TowerDefensePerfProfiler.MaxMetricsPerDump = Math.Max(1, ProfilerMaxMetricsPerDump);
			TowerDefensePerfProfiler.Reset();
			GD.Print($"[TestPerf] AdobeAnimate profiler enabled detailed={TowerDefensePerfProfiler.DetailedHotPathMetrics} dumpFrames={TowerDefensePerfProfiler.DumpIntervalFrames} maxMetrics={TowerDefensePerfProfiler.MaxMetricsPerDump}");
		}
		long timestamp = Stopwatch.GetTimestamp();
		PackedScene packedScene = null;
		bool flag = !string.IsNullOrWhiteSpace(ExactScenePath);
		if (flag)
		{
			UseDifferentZombieScenePool = false;
			packedScene = GD.Load<PackedScene>(ExactScenePath);
		}
		else if (UseDifferentZombieScenePool)
		{
			LoadDifferentZombieScenePool();
		}
		else
		{
			packedScene = TestScene ?? GD.Load<PackedScene>(UseZombieCharacterScenes ? "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn" : "uid://d1jj5qhp7tex4");
		}
		_loadMs = ElapsedMs(timestamp);
		if (!UseDifferentZombieScenePool && packedScene == null)
		{
			string text = (flag ? ExactScenePath : (UseZombieCharacterScenes ? "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn" : "uid://d1jj5qhp7tex4"));
			GD.PrintErr("[TestPerf] Failed to load test scene: " + text);
			return;
		}
		if (UseDifferentZombieScenePool && _scenePool.Count == 0)
		{
			GD.PrintErr("[TestPerf] Failed to load any zombie " + (UseZombieCharacterScenes ? "character" : "animation") + " scenes under " + ZombieSceneRoot);
			return;
		}
		_spawnedNodes.EnsureCapacity(SpawnCount);
		_spawnedCharacters.EnsureCapacity(SpawnCount);
		_spawnedAnimationSprites.EnsureCapacity(SpawnCount);
		_spawnNoGcRegionActive = TowerDefenseCharacterSpawnBudget.TryBeginSpawnNoGcRegion(SpawnCount);
		_spawnStartTicks = Stopwatch.GetTimestamp();
		if (SpawnCount > 1 && (SpawnTimeBudgetMilliseconds > 0.0 || (SpawnBatchSize > 0 && SpawnCount > SpawnBatchSize)))
		{
			_pendingSpawnFallbackScene = packedScene;
			_spawnPending = true;
			_nextSpawnProgressPrint = Math.Max(Math.Max(1, SpawnBatchSize), Math.Max(1, SpawnCount / 10));
			GD.Print($"[TestPerf] spawn-batch-start count={SpawnCount} batch={SpawnBatchSize} budgetMs={SpawnTimeBudgetMilliseconds:F3} differentScenes={UseDifferentZombieScenePool} scenePool={_scenePool.Count} loadMs={_loadMs:F3}");
		}
		else
		{
			SpawnRange(0, SpawnCount, packedScene);
			CompleteSpawn();
		}
	}

	private void InitializeRenderBackendUi()
	{
		CanvasLayer nodeOrNull = GetNodeOrNull<CanvasLayer>("RenderBackendUi");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.Visible = AutoQuitAfterSeconds <= 0.0;
			if (nodeOrNull.Visible)
			{
				_renderBackendOption = GetNode<OptionButton>("%RenderBackendOption");
				_renderBackendStatus = GetNode<Label>("%RenderBackendStatus");
				_renderBackendOption.Clear();
				_renderBackendOption.AddItem(AdobeAnimateRenderBackendPolicy.GetDisplayName(AdobeAnimateRenderBackend.GpuCrowd), 0);
				_renderBackendOption.AddItem(AdobeAnimateRenderBackendPolicy.GetDisplayName(AdobeAnimateRenderBackend.CpuPose), 1);
				_renderBackendOption.Select((int)_testRenderBackend);
				_renderBackendOption.ItemSelected += HandleRenderBackendSelected;
				RefreshRenderBackendUi();
			}
		}
	}

	private void HandleRenderBackendSelected(long selectedIndex)
	{
		if (!GodotObject.IsInstanceValid(_renderBackendOption))
		{
			return;
		}
		int num = (int)selectedIndex;
		if (num < 0 || num >= _renderBackendOption.ItemCount)
		{
			return;
		}
		AdobeAnimateRenderBackend adobeAnimateRenderBackend = AdobeAnimateRenderBackendPolicy.Normalize(_renderBackendOption.GetItemId(num));
		if (_testRenderBackend == adobeAnimateRenderBackend)
		{
			RefreshRenderBackendUi();
			return;
		}
		_testRenderBackend = adobeAnimateRenderBackend;
		if (Global.Instance != null)
		{
			Global.Instance.adobeAnimateRenderBackend = adobeAnimateRenderBackend;
		}
		RestartMeasurementAfterBackendSwitch();
		RefreshRenderBackendUi();
		GD.Print("[TestPerf] render backend switched interactively to " + GetTestRenderBackendLabel() + "; measurement restarted.");
	}

	private void RestartMeasurementAfterBackendSwitch()
	{
		_measurementFrameSeconds.Clear();
		_measurementSimulationDeltaSeconds.Clear();
		_measurementProcessMilliseconds.Clear();
		_measurementPhysicsMilliseconds.Clear();
		_measurementStarted = false;
		_measurementFrames = 0;
		_measurementStartSeconds = 0.0;
		_measurementEndSeconds = 0.0 / 0.0;
		_measurementPreviousFrameTicks = 0L;
		_resultPrinted = false;
		_performanceResultPassed = false;
		_sampleStartSeconds = _uptime.Elapsed.TotalSeconds;
		_windowFrames = 0;
		_windowDeltaSeconds = 0.0;
		_printTimer = 0.0;
		TowerDefenseAllocationTelemetry.Reset();
	}

	private void UpdateRenderBackendUi(double delta)
	{
		if (GodotObject.IsInstanceValid(_renderBackendStatus))
		{
			_renderBackendUiRefreshTimer += delta;
			if (!(_renderBackendUiRefreshTimer < 0.25))
			{
				_renderBackendUiRefreshTimer = 0.0;
				RefreshRenderBackendUi();
			}
		}
	}

	private void RefreshRenderBackendUi()
	{
		if (GodotObject.IsInstanceValid(_renderBackendStatus))
		{
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			double monitor = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
			int value = ((_testRenderBackend == AdobeAnimateRenderBackend.CpuPose) ? aggregateRenderStats.CpuRoots : (aggregateRenderStats.CrowdRoots + aggregateRenderStats.FallbackRoots));
			_renderBackendStatus.Text = $"当前：{AdobeAnimateRenderBackendPolicy.GetDisplayName(_testRenderBackend)}\nFPS {Engine.GetFramesPerSecond():F1}  Draw Call {monitor:F0}  可见根 {value}";
		}
	}

	private async void RunComponentAliveSchedulingRegressionAsync()
	{
		ComponentAliveSchedulingProbe probe = new ComponentAliveSchedulingProbe();
		AddChild(probe, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		int enabledCount = probe.PhysicsProcessCount;
		bool enabledPassed = enabledCount > 0 && probe.IsPhysicsProcessing();
		probe.alive = false;
		int disabledCount = probe.PhysicsProcessCount;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		bool disabledPassed = probe.PhysicsProcessCount == disabledCount && !probe.IsPhysicsProcessing();
		probe.alive = true;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		int physicsProcessCount = probe.PhysicsProcessCount;
		bool flag = physicsProcessCount > disabledCount && probe.IsPhysicsProcessing();
		probe.alive = true;
		bool flag2 = probe.IsPhysicsProcessing();
		probe.alive = false;
		probe.alive = false;
		bool flag3 = !probe.IsPhysicsProcessing();
		bool flag4 = enabledPassed && disabledPassed && flag && flag2 && flag3;
		GD.Print($"[TestComponentAliveScheduling] enabledCount={enabledCount} disabledCount={disabledCount} restoredCount={physicsProcessCount} enabled={enabledPassed} disabled={disabledPassed} restored={flag} repeatEnable={flag2} repeatDisable={flag3} passed={flag4}");
		probe.QueueFree();
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	private async void RunStatePhysicsBatchRateRegressionAsync()
	{
		StateChartState first = new StateChartState
		{
			Name = "BatchRateFirst"
		};
		StateChartState second = new StateChartState
		{
			Name = "BatchRateSecond"
		};
		int firstCount = 0;
		int secondCount = 0;
		double firstDelta = 0.0;
		double secondDelta = 0.0;
		first.OnStatePhysicsProcessing += delegate(double delta)
		{
			firstCount++;
			firstDelta += delta;
		};
		second.OnStatePhysicsProcessing += delegate(double delta)
		{
			secondCount++;
			secondDelta += delta;
		};
		AddChild(first, forceReadableName: false, InternalMode.Disabled);
		AddChild(second, forceReadableName: false, InternalMode.Disabled);
		first._StateEnter(null);
		second._StateEnter(null);
		for (int i = 0; i < 3; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		firstCount = 0;
		secondCount = 0;
		firstDelta = 0.0;
		secondDelta = 0.0;
		ulong startFrame = Engine.GetPhysicsFrames();
		for (int i = 0; i < 8; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		int num = (int)(Engine.GetPhysicsFrames() - startFrame);
		double num2 = 1.0 / (double)Math.Max(1, Engine.PhysicsTicksPerSecond);
		double num3 = Math.Max(0.0, num - 2) * num2;
		double num4 = (double)(num + 1) * num2;
		bool flag = Math.Abs(firstCount - secondCount) <= 1;
		bool flag2 = firstCount >= num - 1 && firstCount <= num + 1 && secondCount >= num - 1 && secondCount <= num + 1;
		bool flag3 = firstDelta >= num3 && firstDelta <= num4 && secondDelta >= num3 && secondDelta <= num4;
		bool flag4 = flag && flag2 && flag3;
		GD.Print($"[TestStatePhysicsBatchRate] frames={num} counts={firstCount},{secondCount} delta={firstDelta:F6},{secondDelta:F6} balanced={flag} fullRate={flag2} deltaPreserved={flag3} passed={flag4}");
		first._StateExit();
		second._StateExit();
		first.QueueFree();
		second.QueueFree();
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	public override void _ExitTree()
	{
		if (_resourceManagerLoadGatePending && GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			ResourceManager.Instance.OnLoadOver -= OnStressResourceManagerLoadOver;
		}
		_resourceManagerLoadGatePending = false;
		DetachCombatComponentEngagementHandlers();
		RestoreCombatStateStress();
		Viewport viewport = GetViewport();
		if (GodotObject.IsInstanceValid(viewport))
		{
			RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), enable: false);
		}
		TowerDefenseAllocationTelemetry.Enabled = false;
		if (BenchmarkMaxFps >= 0)
		{
			Engine.MaxFps = _originalMaxFps;
		}
		if (BenchmarkPhysicsTicksPerSecond > 0)
		{
			Engine.PhysicsTicksPerSecond = _originalPhysicsTicksPerSecond;
		}
		if (_unfocusedRunConfigured)
		{
			OS.LowProcessorUsageMode = _originalLowProcessorUsageMode;
		}
		for (int i = 0; i < _externalVisualStressFixtures.Count; i++)
		{
			ExternalVisualStressFixture externalVisualStressFixture = _externalVisualStressFixtures[i];
			if (externalVisualStressFixture.Handle.IsValid && GodotObject.IsInstanceValid(externalVisualStressFixture.Owner))
			{
				externalVisualStressFixture.Owner.UnregisterExternalVisual(externalVisualStressFixture.Handle);
			}
			if (externalVisualStressFixture.OwnsVisual && GodotObject.IsInstanceValid(externalVisualStressFixture.Visual) && !externalVisualStressFixture.Visual.IsQueuedForDeletion())
			{
				externalVisualStressFixture.Visual.QueueFree();
			}
		}
		_externalVisualStressFixtures.Clear();
		_externalVisualStressTexture = null;
		if (GodotObject.IsInstanceValid(_zInterleaveParticles))
		{
			_zInterleaveParticles.Emitting = false;
			_zInterleaveParticles.Texture = null;
			if (!_zInterleaveParticles.IsQueuedForDeletion())
			{
				_zInterleaveParticles.QueueFree();
			}
		}
		if (GodotObject.IsInstanceValid(_zInterleaveMarker) && !_zInterleaveMarker.IsQueuedForDeletion())
		{
			_zInterleaveMarker.QueueFree();
		}
		_zInterleaveParticles = null;
		_zInterleaveMarker = null;
		_zInterleaveParticleTexture?.Dispose();
		_zInterleaveParticleTexture = null;
		if (GodotObject.IsInstanceValid(_sleepCycleHypnotistEventProvider))
		{
			_sleepCycleHypnotistEventProvider.Free();
		}
		_sleepCycleHypnotistEventProvider = null;
	}

	private void CompleteSpawn()
	{
		TowerDefenseCharacterSpawnBudget.EndSpawnNoGcRegion();
		EnsureZInterleaveFixture();
		if (ForceCpuPoseFallbackForTest)
		{
			ForceCpuPoseFallback();
		}
		PrepareAnimationCadenceRegression();
		PrepareSingleEnemyTargetFixture();
		PrepareManyEnemyIdleTargetFixture();
		PrintExactRoleComponentInventory();
		double value = ElapsedMs(_spawnStartTicks);
		GD.Print($"[TestPerf] ready spawn={_spawnedNodes.Count}/{SpawnCount} characters={_spawnedCharacters.Count} animationSprites={_spawnedAnimationSprites.Count} failures={_spawnFailures} differentScenes={UseDifferentZombieScenePool} characterScenes={UseZombieCharacterScenes} scenePool={_scenePool.Count} loaded={_loadedScenePoolCount} skipped={_skippedScenePoolCount} excluded={_excludedScenePoolCount} loadMs={_loadMs:F3} spawnAddChildMs={value:F3} noGc={_spawnNoGcRegionActive} totalMs={_uptime.Elapsed.TotalMilliseconds:F3}");
		PrintScenePoolSnapshot("ready");
		PrintRuntimeSnapshot("ready");
		PrintCrowdFilterDebugSnapshot("ready");
		if (EnableAdobeAnimateProfiler)
		{
			Callable.From(PrintScheduledNodeTypeSnapshot).CallDeferred();
		}
		_sampleStartSeconds = _uptime.Elapsed.TotalSeconds;
	}

	private void PrepareAnimationCadenceRegression()
	{
		if (!RunAnimationCadenceRegression)
		{
			return;
		}
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _spawnedAnimationSprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				_cadenceSprite = adobeAnimateSpriteBase;
				_cadenceSlot = FindCadenceSlot(adobeAnimateSpriteBase);
				if (GodotObject.IsInstanceValid(_cadenceSlot) && !_cadenceSlot.RequiresRuntimeUpdate)
				{
					_cadenceSlot.AddChild(new Node2D
					{
						Name = "CadenceRuntimeDependency"
					}, forceReadableName: false, InternalMode.Disabled);
					_cadenceSlot.RefreshRuntimeUpdateRequirement(refreshVisibilityWatchers: true);
				}
				if (!GodotObject.IsInstanceValid(_cadenceSlot))
				{
					_cadenceSprite.refreshEveryFrame = true;
				}
				_cadencePreviousPhase = (double)_cadenceSprite.frameIndex + _cadenceSprite.elapsedTimer;
				_cadencePreviousSlotVersion = (GodotObject.IsInstanceValid(_cadenceSlot) ? _cadenceSlot.RuntimePositionVersion : 0);
				GD.Print($"[TestAnimationCadence] prepared sprite={_cadenceSprite.Name} slot={_cadenceSlot?.Name.ToString() ?? "<forced-refresh>"} trueFrameRate={_cadenceSprite.trueFrameRate:F2}");
				break;
			}
		}
	}

	private static AdobeAnimateSlot FindCadenceSlot(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is AdobeAnimateSlot result)
			{
				return result;
			}
			AdobeAnimateSlot adobeAnimateSlot = FindCadenceSlot(child);
			if (GodotObject.IsInstanceValid(adobeAnimateSlot))
			{
				return adobeAnimateSlot;
			}
		}
		return null;
	}

	private void SampleAnimationCadence()
	{
		if (!RunAnimationCadenceRegression || !GodotObject.IsInstanceValid(_cadenceSprite))
		{
			return;
		}
		double num = (double)_cadenceSprite.frameIndex + _cadenceSprite.elapsedTimer;
		_cadenceSamples++;
		if (Math.Abs(num - _cadencePreviousPhase) > 1E-06)
		{
			_cadenceAdvances++;
		}
		_cadencePreviousPhase = num;
		if (GodotObject.IsInstanceValid(_cadenceSlot))
		{
			int runtimePositionVersion = _cadenceSlot.RuntimePositionVersion;
			if (runtimePositionVersion != _cadencePreviousSlotVersion)
			{
				_cadenceSlotAdvances++;
			}
			_cadencePreviousSlotVersion = runtimePositionVersion;
		}
	}

	private bool IsAnimationCadencePassed()
	{
		if (!RunAnimationCadenceRegression)
		{
			return true;
		}
		bool num = _cadenceSamples >= 30 && (double)_cadenceAdvances >= (double)_cadenceSamples * 0.8;
		bool flag = !GodotObject.IsInstanceValid(_cadenceSlot) || (double)_cadenceSlotAdvances >= (double)_cadenceSamples * 0.8;
		return num && flag;
	}

	private void ForceCpuPoseFallback()
	{
		HashSet<AdobeAnimateRuntimeDefinition> hashSet = new HashSet<AdobeAnimateRuntimeDefinition>();
		int num = 0;
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _spawnedAnimationSprites[i];
			if (!GodotObject.IsInstanceValid(adobeAnimateSpriteBase) || adobeAnimateSpriteBase.flashAnimeData == null)
			{
				continue;
			}
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateSpriteBase.flashAnimeData);
			if (orBuild != null && hashSet.Add(orBuild))
			{
				orBuild.GpuPoseTexture = null;
				orBuild.GpuPoseTextureArray = null;
				orBuild.GpuPoseTextureRid = default(Rid);
				orBuild.GpuPoseTextureSize = Vector2I.Zero;
				orBuild.UsesGpuPoseTextureArray = false;
				if (AdobeAnimateDefinitionCache.EnsureCpuPoseData(orBuild))
				{
					num += orBuild.Slices.Length;
				}
			}
		}
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		GD.Print($"[TestCpuPoseFallback] definitions={hashSet.Count} poses={num} enabled={hashSet.Count > 0 && num > 0}");
	}

	private void PrintScheduledNodeTypeSnapshot()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>(StringComparer.Ordinal);
		Dictionary<string, int> dictionary3 = new Dictionary<string, int>(StringComparer.Ordinal);
		Stack<Node> stack = new Stack<Node>();
		stack.Push(this);
		while (stack.Count > 0)
		{
			Node node = stack.Pop();
			string name = node.GetType().Name;
			if (node.IsProcessing())
			{
				dictionary[name] = dictionary.GetValueOrDefault(name) + 1;
			}
			if (node.IsPhysicsProcessing())
			{
				dictionary2[name] = dictionary2.GetValueOrDefault(name) + 1;
			}
			if (node is ComponentBase { IsSharedBatchDispatchActive: not false })
			{
				dictionary3[name] = dictionary3.GetValueOrDefault(name) + 1;
			}
			foreach (Node child in node.GetChildren())
			{
				stack.Push(child);
			}
		}
		PrintScheduledNodeTypeCounts("process", dictionary);
		PrintScheduledNodeTypeCounts("physics", dictionary2);
		PrintScheduledNodeTypeCounts("component-batch", dictionary3);
	}

	private static void PrintScheduledNodeTypeCounts(string kind, Dictionary<string, int> counts)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(counts);
		list.Sort((KeyValuePair<string, int> left, KeyValuePair<string, int> right) => right.Value.CompareTo(left.Value));
		int num = 0;
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			num += list[num2].Value;
		}
		int num3 = Math.Min(24, list.Count);
		string[] array = new string[num3];
		for (int num4 = 0; num4 < num3; num4++)
		{
			array[num4] = $"{list[num4].Key}:{list[num4].Value}";
		}
		GD.Print($"[TestScheduledNodes] kind={kind} total={num} top={string.Join(',', array)}");
	}

	public override void _Process(double delta)
	{
		if (KeepRunningWhenUnfocused)
		{
			if (OS.LowProcessorUsageMode)
			{
				OS.LowProcessorUsageMode = false;
			}
			SceneTree tree = GetTree();
			if (GodotObject.IsInstanceValid(tree) && tree.Paused)
			{
				tree.Paused = false;
			}
		}
		if (BenchmarkMaxFps >= 0 && Engine.MaxFps != BenchmarkMaxFps)
		{
			Engine.MaxFps = BenchmarkMaxFps;
		}
		if (BenchmarkPhysicsTicksPerSecond > 0 && Engine.PhysicsTicksPerSecond != BenchmarkPhysicsTicksPerSecond)
		{
			Engine.PhysicsTicksPerSecond = BenchmarkPhysicsTicksPerSecond;
		}
		UpdateRenderBackendUi(delta);
		EnforceFixedAnimationFrame();
		if (_resourceManagerLoadGatePending)
		{
			return;
		}
		if (_spawnPending)
		{
			SpawnPendingBatch();
			return;
		}
		_windowFrames++;
		_windowDeltaSeconds += delta;
		_printTimer += delta;
		double num = _uptime.Elapsed.TotalSeconds - _sampleStartSeconds;
		TrySwitchAnimeFrameRate(num);
		TryApplyRuntimeVisualMutation(num);
		TryRunAnimationSwitchStress(num);
		TryRunBowlingImpactTweenStress(num);
		TryAdvanceSingleEnemyRoleAttackModes();
		TryDriveSingleEnemyRoleAttackUtilities();
		TryRunSleepCycleStress(num);
		CaptureViewportEntryFrame(delta);
		if (PrintCrowdModeReport && !_crowdModeReportPrinted && num >= 0.5)
		{
			PrintNonGpuGraphCrowdModes();
			_crowdModeReportPrinted = true;
		}
		if (num >= WarmupSeconds)
		{
			bool flag = false;
			if (!_measurementStarted)
			{
				StartMeasurementTelemetry();
				flag = true;
			}
			if (!flag)
			{
				long timestamp = Stopwatch.GetTimestamp();
				double num2 = (double)(timestamp - _measurementPreviousFrameTicks) / (double)Stopwatch.Frequency;
				_measurementPreviousFrameTicks = timestamp;
				EnforceAdobeFixedAnimationFrame();
				_measurementFrames++;
				_measurementFrameSeconds.Add(Math.Max(0.0, num2));
				_measurementSimulationDeltaSeconds.Add(delta);
				if (EnableAnimationSwitchStress)
				{
					if (num <= _animationSwitchWindowEndSeconds)
					{
						_animationSwitchWindowFrameSeconds.Add(num2);
					}
					else
					{
						_animationSwitchSteadyFrameSeconds.Add(num2);
					}
				}
				if (EnableBowlingImpactTweenStress)
				{
					if (num <= _bowlingImpactWindowEndSeconds)
					{
						_bowlingImpactWindowFrameSeconds.Add(num2);
					}
					else
					{
						_bowlingImpactSteadyFrameSeconds.Add(num2);
					}
				}
				_measurementProcessMilliseconds.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0);
				_measurementPhysicsMilliseconds.Add(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0);
				SampleAnimationCadence();
				TryCapturePerformanceScreenshot();
			}
		}
		if (AutoQuitAfterSeconds > 0.0 && num >= AutoQuitAfterSeconds)
		{
			if (!string.IsNullOrWhiteSpace(ScreenshotPath) && !_screenshotSaved)
			{
				TryCapturePerformanceScreenshot();
				if (!_screenshotSaved)
				{
					return;
				}
			}
			if (_measurementStarted && double.IsNaN(_measurementEndSeconds))
			{
				_measurementEndSeconds = _uptime.Elapsed.TotalSeconds;
			}
			if (!_autoQuitPrinted)
			{
				PrintRuntimeSnapshot("final");
				PrintScenePoolSnapshot("final");
				PrintPerformanceResult();
				_autoQuitPrinted = true;
			}
			bool flag2 = SwitchAnimeFrameRateAfterSeconds >= 0.0 && SwitchAnimeFrameRateTo > 0.0;
			bool flag3 = _performanceResultPassed && _animeFrameRateSwitchPassed && _runtimeVisualMutationPassed && (string.IsNullOrWhiteSpace(RuntimeVisualMutation) || _runtimeVisualMutationCompleted) && (!flag2 || _animeFrameRateSwitchCompleted);
			GetTree().Quit((!flag3) ? 2 : 0);
			return;
		}
		if (PrintCrowdFilterDebugAfterFirstRuntimeFrame && !_runtimeCrowdFilterDebugPrinted)
		{
			PrintCrowdFilterDebugSnapshot("runtime-first-frame");
			_runtimeCrowdFilterDebugPrinted = true;
		}
		if (!(_printTimer < (double)PrintIntervalSeconds))
		{
			PrintRuntimeSnapshot("runtime");
			if (PrintCrowdFilterDebugEveryRuntimeSnapshot)
			{
				PrintCrowdFilterDebugSnapshot("runtime");
			}
			_printTimer = 0.0;
			_windowFrames = 0;
			_windowDeltaSeconds = 0.0;
		}
	}

	private void StartMeasurementTelemetry()
	{
		_measurementStarted = true;
		_measurementStartSeconds = _uptime.Elapsed.TotalSeconds;
		_measurementPreviousFrameTicks = Stopwatch.GetTimestamp();
		_measurementThreadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		_measurementTotalAllocatedBefore = GC.GetTotalAllocatedBytes();
		_measurementGen0Before = GC.CollectionCount(0);
		_measurementGen1Before = GC.CollectionCount(1);
		_measurementGen2Before = GC.CollectionCount(2);
		_externalVisualGraphBuildCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphBuildCount;
		_gpuGraphInitialBuildCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphInitialBuildCount;
		_gpuGraphRebuildCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphRebuildCount;
		_gpuGraphOwnerExitInvalidationCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphOwnerExitInvalidationCount;
		_gpuGraphManagedSlotInvalidationCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphManagedSlotInvalidationCount;
		_gpuGraphExternalVisualInvalidationCountAtMeasurementStart = AdobeAnimateRenderManager.GpuRenderGraphExternalVisualInvalidationCount;
		TowerDefenseAllocationTelemetry.Reset();
	}

	private bool HasValidMeasurementTelemetry()
	{
		if (_measurementStarted && _measurementFrames > 0 && _measurementFrameSeconds.Count == _measurementFrames && _measurementSimulationDeltaSeconds.Count == _measurementFrames && _measurementProcessMilliseconds.Count == _measurementFrames)
		{
			return _measurementPhysicsMilliseconds.Count == _measurementFrames;
		}
		return false;
	}

	private void TrySwitchAnimeFrameRate(double sampleElapsed)
	{
		if (_animeFrameRateSwitchCompleted || SwitchAnimeFrameRateAfterSeconds < 0.0 || SwitchAnimeFrameRateTo <= 0.0 || sampleElapsed < SwitchAnimeFrameRateAfterSeconds)
		{
			return;
		}
		_animeFrameRateSwitchCompleted = true;
		if (Global.Instance == null)
		{
			_animeFrameRateSwitchPassed = false;
			GD.PrintErr("[TestTrueFrameRate] failed reason=Global.Instance-null");
			return;
		}
		double animeFrameRate = Global.Instance.animeFrameRate;
		double[] array = new double[_spawnedAnimationSprites.Count];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _spawnedAnimationSprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				array[i] = (double)adobeAnimateSpriteBase.frameIndex + adobeAnimateSpriteBase.elapsedTimer;
				if (adobeAnimateSpriteBase.UsesRuntimeGpuClockInterpolation)
				{
					num++;
				}
				if (adobeAnimateSpriteBase.NeedsRuntimeRenderSubmission)
				{
					num2++;
				}
			}
		}
		Global.Instance.animeFrameRate = SwitchAnimeFrameRateTo;
		double trueAnimeFrameRate = Global.Instance.trueAnimeFrameRate;
		double num3 = 0.0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int j = 0; j < _spawnedAnimationSprites.Count; j++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase2 = _spawnedAnimationSprites[j];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase2))
			{
				double num7 = (double)adobeAnimateSpriteBase2.frameIndex + adobeAnimateSpriteBase2.elapsedTimer;
				num3 = Math.Max(num3, Math.Abs(num7 - array[j]));
				if (!Mathf.IsEqualApprox((float)adobeAnimateSpriteBase2.trueFrameRate, (float)trueAnimeFrameRate))
				{
					num4++;
				}
				if (adobeAnimateSpriteBase2.UsesRuntimeGpuClockInterpolation)
				{
					num5++;
				}
				if (adobeAnimateSpriteBase2.NeedsRuntimeRenderSubmission)
				{
					num6++;
				}
			}
		}
		_animeFrameRateSwitchPassed = num3 <= 1E-06 && num4 == 0 && num == num5 && num2 == num6 && Engine.MaxFps == Global.Instance.effectiveAnimeFrameRate;
		GD.Print($"[TestTrueFrameRate] old={animeFrameRate:F2} new={Global.Instance.animeFrameRate:F2} true={trueAnimeFrameRate:F2} phaseDelta={num3:F6} wrongTrueRate={num4} gpuClock={num}->{num5} dirty={num2}->{num6} maxFps={Engine.MaxFps} passed={_animeFrameRateSwitchPassed}");
	}

	private void TryRunAnimationSwitchStress(double sampleElapsed)
	{
		if (!EnableAnimationSwitchStress || AnimationSwitchIntervalSeconds <= 0.0 || _sampleStartSeconds <= 0.0 || sampleElapsed < AnimationSwitchStartAfterSeconds || sampleElapsed + 1E-06 < _nextAnimationSwitchSeconds)
		{
			return;
		}
		_nextAnimationSwitchSeconds = sampleElapsed + AnimationSwitchIntervalSeconds;
		string text = (_animationSwitchUseSecondClip ? AnimationSwitchClipB : AnimationSwitchClipA);
		_animationSwitchUseSecondClip = !_animationSwitchUseSecondClip;
		long timestamp = Stopwatch.GetTimestamp();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.sprite))
			{
				num++;
				if (!towerDefenseCharacter.sprite.HasClip(text))
				{
					num3++;
					continue;
				}
				towerDefenseCharacter.sprite.SetAnimation(text, loop: true, Math.Max(0.0, AnimationSwitchBlendSeconds));
				num2++;
			}
		}
		double num4 = ElapsedMs(timestamp);
		_animationSwitchCount++;
		_animationSwitchAppliedCount += num2;
		_animationSwitchMissingClipCount += num3;
		_animationSwitchSynchronousTotalMilliseconds += num4;
		_animationSwitchSynchronousMaxMilliseconds = Math.Max(_animationSwitchSynchronousMaxMilliseconds, num4);
		_animationSwitchWindowEndSeconds = Math.Max(_animationSwitchWindowEndSeconds, sampleElapsed + Math.Max(0.05, AnimationSwitchBlendSeconds + 0.05));
		GD.Print($"[TestAnimationSwitch] sequence={_animationSwitchCount} target={text} blend={AnimationSwitchBlendSeconds:F3} candidates={num} applied={num2} missingClip={num3} syncMs={num4:F3}");
	}

	private void TryRunBowlingImpactTweenStress(double sampleElapsed)
	{
		if (!EnableBowlingImpactTweenStress || BowlingImpactBurstIntervalSeconds <= 0.0 || _sampleStartSeconds <= 0.0 || sampleElapsed < BowlingImpactStartAfterSeconds || sampleElapsed + 1E-06 < _nextBowlingImpactSeconds)
		{
			return;
		}
		_nextBowlingImpactSeconds = sampleElapsed + BowlingImpactBurstIntervalSeconds;
		int num = ((BowlingImpactTargetsPerBurst <= 0) ? _spawnedCharacters.Count : Math.Min(BowlingImpactTargetsPerBurst, _spawnedCharacters.Count));
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long timestamp = Stopwatch.GetTimestamp();
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
			{
				continue;
			}
			if (BowlingImpactEnsureShield && !towerDefenseCharacter.HasShield())
			{
				num3++;
			}
			Vector2 pos = towerDefenseCharacter.GlobalPosition + new Vector2(-1f, 0f);
			if (BowlingImpactUseProductionEvent)
			{
				TowerDefenseCharacterEventBowlingHurt.Run(pos, towerDefenseCharacter, Math.Max(0.0, BowlingImpactDamage));
			}
			else
			{
				double num4 = Math.Max(0.0, BowlingImpactDamage);
				bool flag = towerDefenseCharacter.HasShield();
				if (flag)
				{
					num4 *= 2.0 / 9.0;
				}
				Vector2 velocity = new Vector2((float)((double)(towerDefenseCharacter.GlobalPosition.X - pos.X) * GD.RandRange(3.0, 6.0)), -200f);
				towerDefenseCharacter.BowlingHurt(num4, playSplatAudio: true, velocity, flag);
			}
			num2++;
		}
		double num5 = ElapsedMs(timestamp);
		long num6 = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread);
		int num7 = CountProcessedTweens();
		int num8 = CountBowlingImpactMotions();
		int activeCount = TowerDefenseShieldImpactBatch.ActiveCount;
		_bowlingImpactBurstCount++;
		_bowlingImpactAppliedCount += num2;
		_bowlingImpactMissingShieldCount += num3;
		_bowlingImpactMaxProcessedTweens = Math.Max(_bowlingImpactMaxProcessedTweens, num7);
		_bowlingImpactMaxBatchedMotions = Math.Max(_bowlingImpactMaxBatchedMotions, num8);
		_bowlingImpactMaxBatchedShieldVisuals = Math.Max(_bowlingImpactMaxBatchedShieldVisuals, activeCount);
		_bowlingImpactSynchronousTotalMilliseconds += num5;
		_bowlingImpactSynchronousMaxMilliseconds = Math.Max(_bowlingImpactSynchronousMaxMilliseconds, num5);
		_bowlingImpactThreadAllocatedBytes += num6;
		_bowlingImpactWindowEndSeconds = Math.Max(_bowlingImpactWindowEndSeconds, sampleElapsed + Math.Max(0.05, BowlingImpactActiveWindowSeconds));
		GD.Print($"[TestBowlingImpactTween] burst={_bowlingImpactBurstCount} targets={num} applied={num2} missingShield={num3} productionEvent={BowlingImpactUseProductionEvent} damage={BowlingImpactDamage:F3} syncMs={num5:F3} threadAllocatedBytes={num6} processedTweens={num7} batchedMotions={num8} batchedShieldVisuals={activeCount}");
	}

	private int CountProcessedTweens()
	{
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return 0;
		}
		return tree.GetProcessedTweens().Count;
	}

	private int CountBowlingImpactMotions()
	{
		int num = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.IsBowlingImpactDisplacementActive)
			{
				num++;
			}
		}
		return num;
	}

	private void TryApplyRuntimeVisualMutation(double sampleElapsed)
	{
		if (_runtimeVisualMutationCompleted || string.IsNullOrWhiteSpace(RuntimeVisualMutation) || RuntimeVisualMutationAfterSeconds < 0.0 || sampleElapsed < RuntimeVisualMutationAfterSeconds || TryApplyDamagePartMutation() || TryApplyViewportEntryMutation())
		{
			return;
		}
		ulong num = 0uL;
		ulong num2 = 0uL;
		int num3 = 0;
		for (int i = 0; i < _spawnedNodes.Count; i++)
		{
			if (_spawnedNodes[i] is ZombieBoss zombieBoss && GodotObject.IsInstanceValid(zombieBoss))
			{
				if (AdobeAnimateRenderManager.TryResolveGpuRenderGraph(zombieBoss, out var graph, out var graphOwners, out var allocation, out var textureArray, out var textureSize))
				{
					num = graph?.Signature ?? 0;
				}
				switch (RuntimeVisualMutation.Trim().ToLowerInvariant())
				{
				case "boss-stage1":
					zombieBoss.DamagePointSet("Stage1");
					break;
				case "boss-stage2":
					zombieBoss.DamagePointSet("Stage2");
					break;
				case "boss-dynamic":
					zombieBoss.DamagePointSet("Stage1");
					zombieBoss.SetHeadAttackBall(isFire: true);
					zombieBoss.SetRVVisible(visible: false);
					break;
				default:
					_runtimeVisualMutationPassed = false;
					_runtimeVisualMutationCompleted = true;
					GD.PrintErr("[TestRuntimeVisualMutation] unknown=" + RuntimeVisualMutation);
					return;
				}
				num3++;
				if (AdobeAnimateRenderManager.TryResolveGpuRenderGraph(zombieBoss, out var graph2, out graphOwners, out allocation, out textureArray, out textureSize))
				{
					num2 = graph2?.Signature ?? 0;
				}
			}
		}
		_runtimeVisualMutationCompleted = true;
		bool flag = num != 0L && num == num2;
		_runtimeVisualMutationPassed = num3 > 0 && (!EnableGpuRenderGraph || flag);
		GD.Print($"[TestRuntimeVisualMutation] mutation={RuntimeVisualMutation} applied={num3} graph=0x{num:X16}->0x{num2:X16} sharedGraphPreserved={flag} passed={_runtimeVisualMutationPassed}");
		if (PrintCrowdModeReport)
		{
			PrintNonGpuGraphCrowdModes();
		}
	}

	private bool TryApplyViewportEntryMutation()
	{
		if (!string.Equals(RuntimeVisualMutation.Trim(), "viewport-entry", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		Vector2 size = GetViewportRect().Size;
		Vector2 vector = new Vector2(Mathf.Max(40f, size.X * 0.08f), Mathf.Max(40f, size.Y * 0.18f));
		Vector2 vector2 = vector - SpawnOrigin;
		int num = 0;
		for (int i = 0; i < _spawnedNodes.Count; i++)
		{
			Node2D node2D = _spawnedNodes[i];
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.GlobalPosition += vector2;
				num++;
			}
		}
		_runtimeVisualMutationCompleted = true;
		_runtimeVisualMutationPassed = num > 0;
		_viewportEntryFrameSeconds.Clear();
		_viewportEntryCaptureFramesRemaining = 60;
		_viewportEntryCaptureSkipCurrentFrame = true;
		_viewportEntryCapturePrinted = false;
		GD.Print($"[TestViewportEntry] applied={num} translation={vector2} targetOrigin={vector} passed={_runtimeVisualMutationPassed}");
		return true;
	}

	private void CaptureViewportEntryFrame(double delta)
	{
		if (_viewportEntryCaptureFramesRemaining <= 0)
		{
			return;
		}
		if (_viewportEntryCaptureSkipCurrentFrame)
		{
			_viewportEntryCaptureSkipCurrentFrame = false;
			return;
		}
		_viewportEntryFrameSeconds.Add(delta);
		_viewportEntryCaptureFramesRemaining--;
		if (_viewportEntryCaptureFramesRemaining == 0)
		{
			PrintViewportEntryPerformance();
		}
	}

	private void PrintViewportEntryPerformance()
	{
		if (!_viewportEntryCapturePrinted && _viewportEntryFrameSeconds.Count != 0)
		{
			_viewportEntryCapturePrinted = true;
			double[] array = _viewportEntryFrameSeconds.ToArray();
			double value = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
			double value2 = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
			GD.Print($"[TestViewportEntryPerf] frames={array.Length} frameP95Ms={value:F3} frameMaxMs={value2:F3}");
		}
	}

	private bool TryApplyDamagePartMutation()
	{
		string text = RuntimeVisualMutation.Trim();
		bool flag = text.StartsWith("armor-drop:", StringComparison.OrdinalIgnoreCase);
		if (!flag && !text.StartsWith("damage-part:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text2 = text;
		int length = (flag ? "armor-drop:" : "damage-part:").Length;
		string text3 = text2.Substring(length, text2.Length - length).Trim();
		int num = 0;
		if (!string.IsNullOrEmpty(text3))
		{
			for (int i = 0; i < _spawnedCharacters.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					continue;
				}
				int previousCount = CountActiveDamagePartNodes();
				if (flag)
				{
					bool flag2 = GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && towerDefenseCharacter.instance.ArmorHas(text3);
					GD.Print($"[TestArmorDropInitial] armor={text3} present={flag2} currentArmor={towerDefenseCharacter.currentArmor.Contains(text3)}");
					if (!flag2)
					{
						GD.PushError("[TestArmorDropInitial] 角色生成后没有创建逻辑防具：" + text3);
						continue;
					}
					towerDefenseCharacter.instance.ArmorDelete(text3);
					if (towerDefenseCharacter.instance.ArmorHas(text3))
					{
						GD.PushError("[TestArmorDropRemoval] 防具删除后仍存在逻辑实例：" + text3);
						continue;
					}
				}
				else
				{
					towerDefenseCharacter.DamagePartCreate(new StringName(text3), null, new Vector2(-220f, -250f), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
				}
				num += ValidateNewDamagePartNode(towerDefenseCharacter, previousCount);
			}
		}
		_runtimeVisualMutationCompleted = true;
		_runtimeVisualMutationPassed = num > 0;
		GD.Print($"[TestDamagePartMutation] mutation={RuntimeVisualMutation} applied={num} passed={_runtimeVisualMutationPassed}");
		return true;
	}

	private static int ValidateNewDamagePartNode(TowerDefenseCharacter character, int previousCount)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return 0;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		int num = CountActiveDamagePartNodes();
		if (!GodotObject.IsInstanceValid(characterNode) || num <= previousCount)
		{
			return 0;
		}
		int value = (GodotObject.IsInstanceValid(character.sprite) ? character.sprite.GetEffectOnceBatchEffectiveZIndex() : character.ZIndex);
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is DamagePartDrop { over: false } damagePartDrop && GodotObject.IsInstanceValid(damagePartDrop.sprite))
			{
				GD.Print($"[TestDamagePartVisual] characterZ={value} damagePartZ={damagePartDrop.ZIndex} characterGrid={character.gridPos} node={damagePartDrop.GetPath()}");
			}
		}
		return num - previousCount;
	}

	private static int CountActiveDamagePartNodes()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is DamagePartDrop { over: false } damagePartDrop && GodotObject.IsInstanceValid(damagePartDrop.sprite))
			{
				num++;
			}
		}
		return num;
	}

	private void PrintNonGpuGraphCrowdModes()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spawnedAnimationSprites[i];
			if (adobeAnimateSprite == null || !GodotObject.IsInstanceValid(adobeAnimateSprite) || adobeAnimateSprite.IsRenderedByParentSpriteForRender())
			{
				continue;
			}
			num++;
			if (adobeAnimateSprite.TryBuildCrowdRenderState(out var state) != AdobeAnimateCrowdRenderStateResult.Submitted)
			{
				num5++;
				continue;
			}
			if (state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph)
			{
				num2++;
				continue;
			}
			if (state.Mode == AdobeAnimateCrowdRenderMode.Composite)
			{
				num3++;
			}
			else
			{
				num4++;
			}
			string text = adobeAnimateSprite.SceneFilePath;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = state.Definition?.Source?.ResourcePath ?? adobeAnimateSprite.Name.ToString();
			}
			if (hashSet.Add(text))
			{
				GD.Print($"[TestCrowdMode] mode={state.Mode} scene={text}");
				GD.Print(adobeAnimateSprite.BuildCrowdFilterDebugReport($"mode-report:{state.Mode}", 32));
			}
		}
		GD.Print($"[TestCrowdModeSummary] roots={num} gpuGraph={num2} composite={num3} compact={num4} skipped={num5} uniqueNonGraph={hashSet.Count}");
	}

	private void EnforceFixedAnimationFrame()
	{
		ApplyFixedAnimationFrame(FixedAnimationFrame);
	}

	private void EnforceAdobeFixedAnimationFrame()
	{
		ApplyFixedAnimationFrame(_adobeFixedAnimationFrame);
	}

	private void ApplyFixedAnimationFrame(int fixedAnimationFrame)
	{
		if (fixedAnimationFrame < 0)
		{
			return;
		}
		double elapsedTimer = Math.Clamp(FixedAnimationSubframe, 0.0, 0.999999);
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _spawnedAnimationSprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				adobeAnimateSpriteBase.frameIndex = fixedAnimationFrame;
				adobeAnimateSpriteBase.elapsedTimer = elapsedTimer;
				adobeAnimateSpriteBase.pause = true;
			}
		}
		for (int j = 0; j < _spawnedAnimationSprites.Count; j++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase2 = _spawnedAnimationSprites[j];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase2))
			{
				adobeAnimateSpriteBase2.UpdateChild();
			}
		}
	}

	private void SpawnPendingBatch()
	{
		long timestamp = Stopwatch.GetTimestamp();
		int num = ((SpawnBatchSize > 0) ? SpawnBatchSize : 2147483647);
		int num2 = 0;
		while (_nextSpawnIndex < SpawnCount && num2 < num && (num2 <= 0 || !(SpawnTimeBudgetMilliseconds > 0.0) || !TowerDefenseCharacterSpawnBudget.IsFrameBudgetExhausted(SpawnTimeBudgetMilliseconds)))
		{
			ulong ticksUsec = Time.GetTicksUsec();
			SpawnRange(_nextSpawnIndex, _nextSpawnIndex + 1, _pendingSpawnFallbackScene);
			TowerDefenseCharacterSpawnBudget.RecordFrameWork(ticksUsec);
			_nextSpawnIndex++;
			num2++;
			if (SpawnTimeBudgetMilliseconds > 0.0 && (TowerDefenseCharacterSpawnBudget.IsFrameBudgetExhausted(SpawnTimeBudgetMilliseconds) || ElapsedMs(timestamp) >= TowerDefenseCharacterSpawnBudget.ClampFrameBudget(SpawnTimeBudgetMilliseconds)))
			{
				break;
			}
		}
		double num3 = ElapsedMs(timestamp);
		_spawnBatchFrameMilliseconds.Add(num3);
		if (SpawnTimeBudgetMilliseconds > 0.0 && num3 > TowerDefenseCharacterSpawnBudget.ClampFrameBudget(SpawnTimeBudgetMilliseconds))
		{
			_spawnFrameBudgetPassed = false;
		}
		if (_nextSpawnIndex >= _nextSpawnProgressPrint && _nextSpawnIndex < SpawnCount)
		{
			GD.Print($"[TestPerf] spawn-batch-progress spawned={_nextSpawnIndex}/{SpawnCount} elapsedMs={ElapsedMs(_spawnStartTicks):F3}");
			_nextSpawnProgressPrint += Math.Max(1, SpawnCount / 10);
		}
		if (_nextSpawnIndex >= SpawnCount)
		{
			_spawnPending = false;
			_pendingSpawnFallbackScene = null;
			CompleteSpawn();
		}
	}

	private void SpawnRange(int start, int end, PackedScene fallbackScene)
	{
		for (int i = start; i < end; i++)
		{
			if (!TryInstantiateSpawnNode(i, fallbackScene, out var spawnNode))
			{
				_spawnFailures++;
				continue;
			}
			if (EnableZInterleaveFixture)
			{
				spawnNode.ZAsRelative = false;
				spawnNode.ZIndex = (((i & 1) != 0) ? 2 : 0);
			}
			else if (RenderZBucketCount > 0)
			{
				spawnNode.ZIndex = i % RenderZBucketCount;
			}
			if (SpawnScale > 0f && Math.Abs(SpawnScale - 1f) > 0.0001f)
			{
				spawnNode.Scale = new Vector2(SpawnScale, SpawnScale);
			}
			long timestamp = Stopwatch.GetTimestamp();
			spawnNode.GlobalPosition = ResolveSpawnPosition(i);
			if (EnableCombatStateStress && spawnNode is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.gridPos = ((EnableSingleEnemyTargetFixture || EnableManyEnemyIdleFixture) ? ResolveSingleTargetSourceGridPosition() : new Vector2I(i % Math.Max(1, TowerDefenseManager.Instance.gridNum.X) + 1, i / Math.Max(1, TowerDefenseManager.Instance.gridNum.X) % Math.Max(1, TowerDefenseManager.Instance.gridNum.Y) + 1));
			}
			AddChild(spawnNode, forceReadableName: false, InternalMode.Disabled);
			double num = ElapsedMs(timestamp);
			_spawnAddChildReadyMilliseconds.Add(num);
			if (num > _spawnAddChildReadyMaxMilliseconds)
			{
				_spawnAddChildReadyMaxMilliseconds = num;
				_spawnAddChildReadyMaxScene = spawnNode.SceneFilePath;
			}
			RegisterSpawnedNode(spawnNode);
			if (EnableExternalVisualStress)
			{
				if (spawnNode is TowerDefenseCharacter character)
				{
					ConfigureExternalVisualStress(character, i);
				}
				else if (spawnNode is AdobeAnimateSprite owner)
				{
					ConfigureExternalVisualStress(owner, i);
				}
			}
		}
	}

	private void ConfigureExternalVisualStress(TowerDefenseCharacter character, int stableIndex)
	{
		if (!GodotObject.IsInstanceValid(character) || !ShouldEnableExternalVisual(stableIndex))
		{
			return;
		}
		_externalVisualExpectedActive++;
		if (ExternalVisualIceOnly || (stableIndex & 1) == 0)
		{
			TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = new TowerDefenseCharacterBuffFrozen();
			towerDefenseCharacterBuffFrozen.character = character;
			towerDefenseCharacterBuffFrozen.Enter();
			RegisterExternalVisualStressFixture(character, character.sprite, character.icetrapSprite, AdobeAnimateExternalVisualHandle.Invalid, ownsVisual: false, stableIndex);
			return;
		}
		if (_externalVisualStressTexture == null)
		{
			_externalVisualStressTexture = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/ButterSplat.png");
		}
		Sprite2D sprite2D = new Sprite2D
		{
			Name = $"ExternalVisualStress_{stableIndex}",
			Texture = _externalVisualStressTexture,
			Rotation = -0.4f
		};
		character.spriteGroup.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		AdobeAnimateSlot adobeAnimateSlot = (GodotObject.IsInstanceValid(character.headSlot) ? character.headSlot : null);
		if (GodotObject.IsInstanceValid(adobeAnimateSlot))
		{
			adobeAnimateSlot.Update();
			sprite2D.GlobalPosition = adobeAnimateSlot.GlobalPosition + new Vector2(-5f, -10f);
		}
		else
		{
			sprite2D.Position = new Vector2(0f, -30f);
		}
		AdobeAnimateExternalVisualHandle handle = character.RegisterCharacterExternalVisual(sprite2D, GodotObject.IsInstanceValid(adobeAnimateSlot) ? new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Slot, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation, adobeAnimateSlot) : new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
		RegisterExternalVisualStressFixture(character, character.sprite, sprite2D, handle, ownsVisual: true, stableIndex);
	}

	private void ConfigureExternalVisualStress(AdobeAnimateSprite owner, int stableIndex)
	{
		if (GodotObject.IsInstanceValid(owner) && ShouldEnableExternalVisual(stableIndex))
		{
			_externalVisualExpectedActive++;
			if (_externalVisualStressTexture == null)
			{
				_externalVisualStressTexture = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/ButterSplat.png");
			}
			Sprite2D sprite2D = new Sprite2D
			{
				Name = $"ExternalVisualStress_{stableIndex}",
				Texture = _externalVisualStressTexture,
				Position = new Vector2(0f, -30f),
				Rotation = -0.4f
			};
			owner.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateExternalVisualHandle handle = owner.RegisterExternalVisual(sprite2D, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
			RegisterExternalVisualStressFixture(null, owner, sprite2D, handle, ownsVisual: true, stableIndex);
		}
	}

	private void RegisterExternalVisualStressFixture(TowerDefenseCharacter character, AdobeAnimateSprite owner, Sprite2D visual, AdobeAnimateExternalVisualHandle handle, bool ownsVisual, int stableIndex)
	{
		_externalVisualStressFixtures.Add(new ExternalVisualStressFixture
		{
			Character = character,
			Owner = owner,
			Visual = visual,
			Handle = handle,
			OwnsVisual = ownsVisual,
			StableIndex = stableIndex
		});
		if (!GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(visual) || (ownsVisual && !handle.IsValid))
		{
			_externalVisualConfigurationFailures++;
			GD.PrintErr($"[TestExternalVisual] configure-failed index={stableIndex} reason=invalid-fixture scene={character?.SceneFilePath ?? owner?.SceneFilePath} sprite={owner?.SceneFilePath}");
		}
	}

	private bool ShouldEnableExternalVisual(int stableIndex)
	{
		double num = Math.Clamp(ExternalVisualRatio, 0.0, 1.0);
		if (num <= 0.0)
		{
			return false;
		}
		if (num >= 1.0)
		{
			return true;
		}
		return (double)(uint)(stableIndex * -1640531535 + -2048144777) / 4294967296.0 < num;
	}

	private void LoadDifferentZombieScenePool()
	{
		List<string> list = new List<string>(256);
		CollectZombieScenePaths(NormalizeResDirectory(ZombieSceneRoot), list);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			if (MaxDifferentZombieScenes > 0 && _scenePool.Count >= MaxDifferentZombieScenes)
			{
				break;
			}
			if (!IsZombieSceneFile(item))
			{
				_skippedScenePoolCount++;
				continue;
			}
			if (ShouldExcludeZombieScenePath(item))
			{
				_excludedScenePoolCount++;
				continue;
			}
			PackedScene packedScene = GD.Load<PackedScene>(item);
			if (packedScene == null)
			{
				_skippedScenePoolCount++;
				continue;
			}
			_scenePool.Add(packedScene);
			_scenePoolPaths.Add(item);
			_loadedScenePoolCount++;
		}
		_sceneUseCounts = new int[_scenePool.Count];
	}

	private bool ShouldExcludeZombieScenePath(string resPath)
	{
		if (string.IsNullOrWhiteSpace(ExcludedZombieScenePathSubstring))
		{
			return false;
		}
		string text = resPath.Replace('\\', '/');
		string text2 = ExcludedZombieScenePathSubstring.Replace('\\', '/').Trim();
		if (text2.Length > 0)
		{
			return text.Contains(text2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private bool IsZombieSceneFile(string resPath)
	{
		if (!UseZombieCharacterScenes)
		{
			return IsZombieAnimationSceneFile(resPath);
		}
		return IsZombieCharacterSceneFile(resPath);
	}

	private static void CollectZombieScenePaths(string directoryPath, List<string> paths)
	{
		using DirAccess dirAccess = DirAccess.Open(directoryPath);
		if (dirAccess == null)
		{
			return;
		}
		dirAccess.ListDirBegin();
		while (true)
		{
			string next = dirAccess.GetNext();
			if (string.IsNullOrEmpty(next))
			{
				break;
			}
			if (!(next == ".") && !(next == ".."))
			{
				string text = directoryPath + "/" + next;
				if (dirAccess.CurrentIsDir())
				{
					CollectZombieScenePaths(text, paths);
				}
				else if (next.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
				{
					paths.Add(text);
				}
			}
		}
		dirAccess.ListDirEnd();
	}

	private static bool IsZombieAnimationSceneFile(string resPath)
	{
		string text = resPath.Replace('\\', '/');
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("/scene/") || text2.Contains("/config/") || text2.Contains("/packet/") || text2.Contains("/damagepoint/") || text2.Contains("/effect/") || text2.Contains("/armor/"))
		{
			return false;
		}
		if (!GetFileNameWithoutExtension(text).StartsWith("Zombie", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		using FileAccess fileAccess = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return false;
		}
		string asText = fileAccess.GetAsText();
		return asText.Contains("flashAnimeData", StringComparison.Ordinal) && asText.Contains("AdobeAnimateSpriteBase.cs", StringComparison.Ordinal);
	}

	private bool IsZombieCharacterSceneFile(string resPath)
	{
		string text = resPath.Replace('\\', '/');
		string text2 = text.ToLowerInvariant();
		if (text2.EndsWith("/towerdefensezombiebungispawn.tscn", StringComparison.Ordinal))
		{
			return false;
		}
		if (!text2.Contains("/scene/"))
		{
			return false;
		}
		string fileNameWithoutExtension = GetFileNameWithoutExtension(text);
		string value = (string.IsNullOrWhiteSpace(CharacterSceneFilePrefix) ? "TowerDefenseZombie" : CharacterSceneFilePrefix.Trim());
		if (!fileNameWithoutExtension.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		using FileAccess fileAccess = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return false;
		}
		string asText = fileAccess.GetAsText();
		return asText.Contains("Prefab/TowerDefense/Character/TowerDefenseZombie.tscn", StringComparison.Ordinal) || asText.Contains("TowerDefenseZombie : TowerDefenseCharacter", StringComparison.Ordinal) || asText.Contains("script = ExtResource", StringComparison.Ordinal);
	}

	private bool TryInstantiateSpawnNode(int index, PackedScene fallbackScene, out Node2D spawnNode)
	{
		spawnNode = null;
		PackedScene packedScene = fallbackScene;
		int num = -1;
		if (UseDifferentZombieScenePool)
		{
			if (_scenePool.Count == 0)
			{
				return false;
			}
			num = index % _scenePool.Count;
			packedScene = _scenePool[num];
		}
		if (packedScene == null)
		{
			return false;
		}
		long timestamp = Stopwatch.GetTimestamp();
		Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		double num2 = ElapsedMs(timestamp);
		_spawnInstantiateMilliseconds.Add(num2);
		if (num2 > _spawnInstantiateMaxMilliseconds)
		{
			_spawnInstantiateMaxMilliseconds = num2;
			_spawnInstantiateMaxScene = packedScene.ResourcePath;
		}
		if (!(node is Node2D node2D))
		{
			node?.QueueFree();
			return false;
		}
		if (UseZombieCharacterScenes && !(node2D is TowerDefenseCharacter))
		{
			node2D.QueueFree();
			return false;
		}
		if (!UseZombieCharacterScenes && !(node2D is AdobeAnimateSpriteBase))
		{
			node2D.QueueFree();
			return false;
		}
		if (num >= 0 && num < _sceneUseCounts.Length)
		{
			_sceneUseCounts[num]++;
		}
		spawnNode = node2D;
		return true;
	}

	private void RegisterSpawnedNode(Node2D node)
	{
		_spawnedNodes.Add(node);
		if (node is TowerDefenseCharacter towerDefenseCharacter)
		{
			_spawnedCharacters.Add(towerDefenseCharacter);
			_spawnedCharacterScenePaths.Add(towerDefenseCharacter.SceneFilePath);
			_spawnedCharacterProcessModes.Add(towerDefenseCharacter.ProcessMode);
			if (BowlingImpactEnsureShield && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.HasShield())
			{
				towerDefenseCharacter.instance.ArmorAdd("Shield");
			}
			ConfigureCombatCharacterPreservation(towerDefenseCharacter);
			towerDefenseCharacter.showHealthComponent?.SetAlive(ShowCharacterHealthInStress);
			if (ShouldFreezeCombatSourcesForInitialSleep())
			{
				towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
			}
		}
		if (!string.IsNullOrWhiteSpace(ExactAnimationClip))
		{
			if (node is AdobeAnimateSpriteBase adobeAnimateSpriteBase)
			{
				adobeAnimateSpriteBase.SetClip(ExactAnimationClip);
			}
			else if (node is TowerDefenseCharacter towerDefenseCharacter2 && GodotObject.IsInstanceValid(towerDefenseCharacter2.sprite))
			{
				towerDefenseCharacter2.sprite.SetClip(ExactAnimationClip);
			}
		}
		int count = _spawnedAnimationSprites.Count;
		CollectAnimationSprites(node, _spawnedAnimationSprites);
		if (FixedAnimationFrame < 0)
		{
			return;
		}
		for (int i = count; i < _spawnedAnimationSprites.Count; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase2 = _spawnedAnimationSprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase2))
			{
				adobeAnimateSpriteBase2.frameIndex = FixedAnimationFrame;
				adobeAnimateSpriteBase2.elapsedTimer = Math.Clamp(FixedAnimationSubframe, 0.0, 0.999999);
				adobeAnimateSpriteBase2.pause = true;
			}
		}
	}

	private void ConfigureCombatCharacterPreservation(TowerDefenseCharacter character)
	{
		if (!EnableCombatStateStress || !PreserveCombatCharactersInStress || !GodotObject.IsInstanceValid(character?.instance))
		{
			return;
		}
		TowerDefenseCharacterInstance instance = character.instance;
		instance.keepAlive = true;
		instance.hitpointsNearDeath = 0.0;
		instance.hitpointsSave = 1000000000000.0;
		instance.hitpoints = 1000000000000.0;
		for (int i = 0; i < instance.armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = instance.armorList[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
			{
				towerDefenseArmorInstance.damagePointBase = 1000000000000.0;
				towerDefenseArmorInstance.hitpointsSave = 1000000000000.0;
				towerDefenseArmorInstance.hitPoints = 1000000000000.0;
			}
		}
	}

	private bool ShouldFreezeCombatSourcesForInitialSleep()
	{
		if (EnableCombatStateStress && EnableSingleEnemyTargetFixture)
		{
			return EnableSleepCycleStress;
		}
		return false;
	}

	private void RestoreCombatSourceProcessModes()
	{
		int num = Math.Min(_spawnedCharacters.Count, _spawnedCharacterProcessModes.Count);
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.ProcessMode = _spawnedCharacterProcessModes[i];
			}
		}
	}

	private bool TryNormalizeCombatStressSourceCamp(out TowerDefenseEnum.CHARACTER_CAMP sourceCamp)
	{
		sourceCamp = _spawnedCharacters[0].camp;
		TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = sourceCamp;
		if (cHARACTER_CAMP != TowerDefenseEnum.CHARACTER_CAMP.PLANT && cHARACTER_CAMP != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			GD.PushError($"[TestCombatFixture] Unsupported source camp: {sourceCamp}.");
			return false;
		}
		int num = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				GD.PushError($"[TestCombatFixture] Source character is invalid at index {i}.");
				return false;
			}
			if (towerDefenseCharacter.camp != sourceCamp)
			{
				towerDefenseCharacter.camp = sourceCamp;
				towerDefenseCharacter.targetRegistrationComponent?.NotifyTargetStateChanged();
				num++;
			}
		}
		if (num > 0)
		{
			GD.Print($"[TestCombatFixture] normalizedSourceCamp={sourceCamp} normalized={num}/{_spawnedCharacters.Count}");
		}
		return true;
	}

	private void PrepareSingleEnemyTargetFixture()
	{
		if (!EnableSingleEnemyTargetFixture)
		{
			return;
		}
		if (!EnableCombatStateStress || !UseZombieCharacterScenes)
		{
			GD.PushError("[TestCombatFixture] The single-enemy fixture requires character scenes and combat-state stress.");
		}
		else if (_spawnedCharacters.Count == 0 || !GodotObject.IsInstanceValid(_spawnedCharacters[0]))
		{
			GD.PushError("[TestCombatFixture] No source character is available for the single enemy.");
		}
		else
		{
			if (!TryNormalizeCombatStressSourceCamp(out var sourceCamp))
			{
				return;
			}
			string text = ((!string.IsNullOrWhiteSpace(SingleEnemyTargetScenePath)) ? SingleEnemyTargetScenePath.Trim() : ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn" : "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn"));
			PackedScene packedScene = GD.Load<PackedScene>(text);
			if (packedScene == null)
			{
				GD.PushError("[TestCombatFixture] Failed to load enemy scene: " + text);
				return;
			}
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is TowerDefenseCharacter towerDefenseCharacter))
			{
				node?.QueueFree();
				GD.PushError("[TestCombatFixture] Enemy scene is not a TowerDefenseCharacter: " + text);
				return;
			}
			Vector2I gridPos = ResolveSingleTargetSourceGridPosition();
			Vector2 vector = ResolveSingleTargetSourcePosition();
			ulong physicsFrames = Engine.GetPhysicsFrames();
			for (int i = 0; i < _spawnedCharacters.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = _spawnedCharacters[i];
				towerDefenseCharacter2.gridPos = gridPos;
				towerDefenseCharacter2.SetGlobalPositionForPhysicsFrame(vector, physicsFrames);
			}
			int num = ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? 1 : (-1));
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			float num2 = (GodotObject.IsInstanceValid(instance) ? Mathf.Clamp(instance.gridSize.X * 0.4f, 24f, 64f) : 48f);
			towerDefenseCharacter.Name = "SingleEnemyComponentStressTarget";
			towerDefenseCharacter.camp = ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
			towerDefenseCharacter.gridPos = gridPos;
			towerDefenseCharacter.GlobalPosition = vector + new Vector2((float)num * num2, 0f);
			float num3 = Math.Max(0.1f, SingleEnemyTargetScale);
			towerDefenseCharacter.Scale *= num3;
			AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
			{
				towerDefenseCharacter.QueueFree();
				GD.PushError("[TestCombatFixture] Enemy target has no runtime instance after Ready.");
				return;
			}
			towerDefenseCharacter.instance.keepAlive = true;
			towerDefenseCharacter.instance.invincible = false;
			towerDefenseCharacter.instance.invincibleHurt = false;
			towerDefenseCharacter.instance.invincibleSmash = false;
			towerDefenseCharacter.instance.canBeCollection = true;
			towerDefenseCharacter.instance.die = false;
			towerDefenseCharacter.instance.nearDie = false;
			towerDefenseCharacter.instance.height = TowerDefenseEnum.CHARACTER_HEIGHT.TALL;
			towerDefenseCharacter.instance.maskFlags = -1;
			towerDefenseCharacter.instance.collisionFlags = -1;
			towerDefenseCharacter.instance.hitpointsNearDeath = 0.0;
			towerDefenseCharacter.instance.hitpointsSave = 1000000000000.0;
			towerDefenseCharacter.instance.hitpoints = 1000000000000.0;
			for (int j = 0; j < towerDefenseCharacter.instance.armorList.Count; j++)
			{
				TowerDefenseArmorInstance towerDefenseArmorInstance = towerDefenseCharacter.instance.armorList[j];
				if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
				{
					towerDefenseArmorInstance.damagePointBase = 1000000000000.0;
					towerDefenseArmorInstance.hitpointsSave = 1000000000000.0;
					towerDefenseArmorInstance.hitPoints = 1000000000000.0;
				}
			}
			towerDefenseCharacter.targetRegistrationComponent?.SetAlive(alive: true);
			if (towerDefenseCharacter.targetRegistrationComponent != null)
			{
				towerDefenseCharacter.targetRegistrationComponent.allLineCheck = true;
				towerDefenseCharacter.targetRegistrationComponent.canProjectileCheck = true;
				towerDefenseCharacter.targetRegistrationComponent.NotifyTargetStateChanged();
			}
			towerDefenseCharacter.showHealthComponent?.SetAlive(alive: false);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.componentManager))
			{
				IReadOnlyList<CharacterComponentRuntime> resourceComponents = towerDefenseCharacter.componentManager.ResourceComponents;
				for (int k = 0; k < resourceComponents.Count; k++)
				{
					CharacterComponentRuntime characterComponentRuntime = resourceComponents[k];
					if ((!(characterComponentRuntime is TargetRegistrationComponent) && !(characterComponentRuntime is HurtComponent)) || 1 == 0)
					{
						characterComponentRuntime.SetAlive(alive: false);
					}
				}
			}
			_singleEnemyTarget = towerDefenseCharacter;
			_singleEnemyTargetInitialHitpoints = GetCombatTargetDurability(towerDefenseCharacter);
			_singleEnemyAttackComponentCount = 0;
			_singleEnemyAttackUtilityComponentCount = 0;
			_singleEnemyFireComponentCount = 0;
			_engagedAttackComponents.Clear();
			_engagedFireComponents.Clear();
			_exercisedAttackUtilityComponents.Clear();
			_pendingSoccerAttackUtilityComponents.Clear();
			for (int l = 0; l < _spawnedCharacters.Count; l++)
			{
				TowerDefenseCharacter towerDefenseCharacter3 = _spawnedCharacters[l];
				ComponentManager componentManager = towerDefenseCharacter3?.componentManager;
				if (!GodotObject.IsInstanceValid(componentManager))
				{
					continue;
				}
				IReadOnlyList<CharacterComponentRuntime> resourceComponents2 = componentManager.ResourceComponents;
				for (int m = 0; m < resourceComponents2.Count; m++)
				{
					CharacterComponentRuntime characterComponentRuntime2 = resourceComponents2[m];
					AttackComponent attack = characterComponentRuntime2 as AttackComponent;
					if (attack != null)
					{
						if (IsRoleUtilityAttackComponent(towerDefenseCharacter3, attack))
						{
							_singleEnemyAttackUtilityComponentCount++;
							_pendingSoccerAttackUtilityComponents.Add(((TowerDefenseZombieSoccer)towerDefenseCharacter3, attack));
							continue;
						}
						_singleEnemyAttackComponentCount++;
						if (!TryAttachLegacyAttackEngagementHandler(towerDefenseCharacter3, attack))
						{
							AttackComponent.AttackEventHandler handler = null;
							handler = delegate
							{
								_engagedAttackComponents.Add(attack);
								attack.OnAttack -= handler;
								_attackEngagementHandlers.Remove(attack);
							};
							_attackEngagementHandlers[attack] = handler;
							attack.OnAttack += handler;
						}
						continue;
					}
					characterComponentRuntime2 = resourceComponents2[m];
					FireComponent fire = characterComponentRuntime2 as FireComponent;
					if (fire != null)
					{
						_singleEnemyFireComponentCount++;
						FireComponent.FireVolleyEventHandler handler2 = null;
						handler2 = delegate
						{
							_engagedFireComponents.Add(fire);
							fire.OnFireVolley -= handler2;
							_fireEngagementHandlers.Remove(fire);
						};
						_fireEngagementHandlers[fire] = handler2;
						fire.OnFireVolley += handler2;
					}
				}
			}
			_singleEnemyFixturePrepared = true;
			if (ShouldFreezeCombatSourcesForInitialSleep())
			{
				BeginSleepCycleStress(0.0);
				RestoreCombatSourceProcessModes();
			}
			GD.Print($"[TestCombatFixture] ready sources={_spawnedCharacters.Count} targets=1 sourceCamp={sourceCamp} targetCamp={towerDefenseCharacter.camp} targetScene={text} attackComponents={_singleEnemyAttackComponentCount} attackUtilityComponents={_singleEnemyAttackUtilityComponentCount} fireComponents={_singleEnemyFireComponentCount} targetScale={num3:F2}");
		}
	}

	private void PrepareManyEnemyIdleTargetFixture()
	{
		if (!EnableManyEnemyIdleFixture)
		{
			return;
		}
		if (EnableSingleEnemyTargetFixture)
		{
			GD.PushError("[TestCombatFixture] Single-enemy attack and many-enemy idle fixtures are mutually exclusive.");
		}
		else if (!EnableCombatStateStress || !UseZombieCharacterScenes)
		{
			GD.PushError("[TestCombatFixture] The many-enemy idle fixture requires character scenes and combat-state stress.");
		}
		else if (_spawnedCharacters.Count == 0 || !GodotObject.IsInstanceValid(_spawnedCharacters[0]))
		{
			GD.PushError("[TestCombatFixture] No source character is available for idle targets.");
		}
		else
		{
			if (!TryNormalizeCombatStressSourceCamp(out var sourceCamp))
			{
				return;
			}
			string text = ((!string.IsNullOrWhiteSpace(SingleEnemyTargetScenePath)) ? SingleEnemyTargetScenePath.Trim() : ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn" : "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn"));
			PackedScene packedScene = GD.Load<PackedScene>(text);
			if (packedScene == null)
			{
				GD.PushError("[TestCombatFixture] Failed to load idle enemy scene: " + text);
				return;
			}
			Vector2I vector2I = ResolveSingleTargetSourceGridPosition();
			Vector2 vector = ResolveSingleTargetSourcePosition();
			ulong physicsFrames = Engine.GetPhysicsFrames();
			for (int i = 0; i < _spawnedCharacters.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
				towerDefenseCharacter.gridPos = vector2I;
				towerDefenseCharacter.SetGlobalPositionForPhysicsFrame(vector, physicsFrames);
			}
			int num = ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? 1 : (-1));
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			float num2 = (GodotObject.IsInstanceValid(instance) ? Mathf.Clamp(instance.gridSize.X * 0.4f, 24f, 64f) : 48f);
			Vector2I gridPos = vector2I;
			Vector2 globalPosition = vector + new Vector2((float)num * num2, 0f);
			if (ManyEnemyIdleTargetsOutsideAttackRange)
			{
				int x = ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? (vector2I.X + 8) : (vector2I.X - 8));
				gridPos = new Vector2I(x, vector2I.Y);
				float num3 = (GodotObject.IsInstanceValid(instance) ? (instance.gridSize.X * 8f) : 800f);
				globalPosition = vector + new Vector2((float)num * num3, 0f);
			}
			float num4 = Math.Max(0.1f, SingleEnemyTargetScale);
			int num5 = Math.Max(1, ManyEnemyIdleTargetCount);
			for (int j = 0; j < num5; j++)
			{
				Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				if (!(node is TowerDefenseCharacter towerDefenseCharacter2))
				{
					node?.QueueFree();
					GD.PushError("[TestCombatFixture] Idle enemy scene is not a TowerDefenseCharacter: " + text);
					break;
				}
				towerDefenseCharacter2.Name = $"ManyEnemyIdleTarget_{j}";
				towerDefenseCharacter2.camp = ((sourceCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
				towerDefenseCharacter2.gridPos = gridPos;
				towerDefenseCharacter2.GlobalPosition = globalPosition;
				towerDefenseCharacter2.Scale *= num4;
				towerDefenseCharacter2.Visible = false;
				towerDefenseCharacter2.ProcessMode = ProcessModeEnum.Disabled;
				AddChild(towerDefenseCharacter2, forceReadableName: false, InternalMode.Disabled);
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter2.instance))
				{
					towerDefenseCharacter2.QueueFree();
					GD.PushError($"[TestCombatFixture] Idle enemy target {j} has no runtime instance after Ready.");
					break;
				}
				towerDefenseCharacter2.instance.keepAlive = true;
				towerDefenseCharacter2.instance.invincible = false;
				towerDefenseCharacter2.instance.invincibleHurt = false;
				towerDefenseCharacter2.instance.invincibleSmash = false;
				towerDefenseCharacter2.instance.canBeCollection = true;
				towerDefenseCharacter2.instance.die = false;
				towerDefenseCharacter2.instance.nearDie = false;
				towerDefenseCharacter2.instance.height = TowerDefenseEnum.CHARACTER_HEIGHT.TALL;
				int num6 = (ManyEnemyIdleTargetsOutsideAttackRange ? (-1) : 0);
				towerDefenseCharacter2.instance.maskFlags = num6;
				towerDefenseCharacter2.instance.collisionFlags = num6;
				towerDefenseCharacter2.instance.hitpointsNearDeath = 0.0;
				towerDefenseCharacter2.instance.hitpointsSave = 1000000000000.0;
				towerDefenseCharacter2.instance.hitpoints = 1000000000000.0;
				for (int k = 0; k < towerDefenseCharacter2.instance.armorList.Count; k++)
				{
					TowerDefenseArmorInstance towerDefenseArmorInstance = towerDefenseCharacter2.instance.armorList[k];
					if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
					{
						towerDefenseArmorInstance.damagePointBase = 1000000000000.0;
						towerDefenseArmorInstance.hitpointsSave = 1000000000000.0;
						towerDefenseArmorInstance.hitPoints = 1000000000000.0;
					}
				}
				towerDefenseCharacter2.targetRegistrationComponent?.SetAlive(alive: true);
				if (towerDefenseCharacter2.targetRegistrationComponent != null)
				{
					towerDefenseCharacter2.targetRegistrationComponent.allLineCheck = !ManyEnemyIdleTargetsOutsideAttackRange;
					towerDefenseCharacter2.targetRegistrationComponent.canProjectileCheck = true;
					towerDefenseCharacter2.targetRegistrationComponent.NotifyTargetStateChanged();
				}
				towerDefenseCharacter2.showHealthComponent?.SetAlive(alive: false);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2.componentManager))
				{
					IReadOnlyList<CharacterComponentRuntime> resourceComponents = towerDefenseCharacter2.componentManager.ResourceComponents;
					for (int l = 0; l < resourceComponents.Count; l++)
					{
						CharacterComponentRuntime characterComponentRuntime = resourceComponents[l];
						if ((!(characterComponentRuntime is TargetRegistrationComponent) && !(characterComponentRuntime is HurtComponent)) || 1 == 0)
						{
							characterComponentRuntime.SetAlive(alive: false);
						}
					}
				}
				_manyEnemyIdleTargets.Add(towerDefenseCharacter2);
			}
			_engagedAttackComponents.Clear();
			_engagedFireComponents.Clear();
			_manyEnemyIdleAttackComponents.Clear();
			_manyEnemyIdleFireComponents.Clear();
			for (int m = 0; m < _spawnedCharacters.Count; m++)
			{
				TowerDefenseCharacter towerDefenseCharacter3 = _spawnedCharacters[m];
				ComponentManager componentManager = towerDefenseCharacter3?.componentManager;
				if (!GodotObject.IsInstanceValid(componentManager))
				{
					continue;
				}
				IReadOnlyList<CharacterComponentRuntime> resourceComponents2 = componentManager.ResourceComponents;
				for (int n = 0; n < resourceComponents2.Count; n++)
				{
					CharacterComponentRuntime characterComponentRuntime2 = resourceComponents2[n];
					AttackComponent attack = characterComponentRuntime2 as AttackComponent;
					if (attack != null)
					{
						_manyEnemyIdleAttackComponents.Add(attack);
						if (!TryAttachLegacyAttackEngagementHandler(towerDefenseCharacter3, attack))
						{
							AttackComponent.AttackEventHandler handler = null;
							handler = delegate
							{
								_engagedAttackComponents.Add(attack);
								attack.OnAttack -= handler;
								_attackEngagementHandlers.Remove(attack);
							};
							_attackEngagementHandlers[attack] = handler;
							attack.OnAttack += handler;
						}
						continue;
					}
					characterComponentRuntime2 = resourceComponents2[n];
					FireComponent fire = characterComponentRuntime2 as FireComponent;
					if (fire != null)
					{
						_manyEnemyIdleFireComponents.Add(fire);
						FireComponent.FireVolleyEventHandler handler2 = null;
						handler2 = delegate
						{
							_engagedFireComponents.Add(fire);
							fire.OnFireVolley -= handler2;
							_fireEngagementHandlers.Remove(fire);
						};
						_fireEngagementHandlers[fire] = handler2;
						fire.OnFireVolley += handler2;
					}
				}
			}
			_manyEnemyIdleFixturePrepared = _manyEnemyIdleTargets.Count == num5;
			GD.Print($"[TestCombatIdleFixture] ready sources={_spawnedCharacters.Count} targets={_manyEnemyIdleTargets.Count}/{num5} sourceCamp={sourceCamp} targetScene={text} attackComponents={_manyEnemyIdleAttackComponents.Count} fireComponents={_manyEnemyIdleFireComponents.Count} targetScale={num4:F2} outsideAttackRange={ManyEnemyIdleTargetsOutsideAttackRange}");
		}
	}

	private static bool IsRoleUtilityAttackComponent(TowerDefenseCharacter source, AttackComponent attack)
	{
		if (source is TowerDefenseZombieSoccer)
		{
			return string.Equals(attack.ComponentDefinition?.InstanceId, "character.attack.1", StringComparison.Ordinal);
		}
		return false;
	}

	private void TryDriveSingleEnemyRoleAttackUtilities()
	{
		if (!_singleEnemyFixturePrepared || _singleEnemyAttackUtilityComponentCount == 0 || _singleEnemyRoleUtilityPhaseCompleted || !GodotObject.IsInstanceValid(_singleEnemyTarget))
		{
			return;
		}
		if (!_singleEnemyRoleUtilityPhaseStarted)
		{
			if (_engagedAttackComponents.Count != _singleEnemyAttackComponentCount)
			{
				return;
			}
			_singleEnemyRoleUtilityPhaseStarted = true;
			_singleEnemyTarget.targetRegistrationComponent?.SetAlive(alive: false);
			_singleEnemyTarget.gridPos = Vector2I.One;
			_singleEnemyTarget.SetGlobalPositionForPhysicsFrame(new Vector2(-10000f, -10000f), Engine.GetPhysicsFrames());
			GD.Print($"[TestCombatFixture] role-attack-utility-phase-start role={"TowerDefenseZombieSoccer"} components={_singleEnemyAttackUtilityComponentCount} " + "trigger=primary-attacks-engaged");
		}
		double totalSeconds = _uptime.Elapsed.TotalSeconds;
		if (totalSeconds < _nextSingleEnemyRoleUtilityAuditSeconds)
		{
			return;
		}
		_nextSingleEnemyRoleUtilityAuditSeconds = totalSeconds + 0.1;
		for (int num = _pendingSoccerAttackUtilityComponents.Count - 1; num >= 0; num--)
		{
			var (towerDefenseZombieSoccer, item) = _pendingSoccerAttackUtilityComponents[num];
			if (GodotObject.IsInstanceValid(towerDefenseZombieSoccer) && !towerDefenseZombieSoccer.hasBall)
			{
				_exercisedAttackUtilityComponents.Add(item);
				_pendingSoccerAttackUtilityComponents.RemoveAt(num);
			}
		}
		if (_pendingSoccerAttackUtilityComponents.Count <= 0)
		{
			Vector2I gridPos = ResolveSingleTargetSourceGridPosition();
			Vector2 vector = ResolveSingleTargetSourcePosition();
			ulong physicsFrames = Engine.GetPhysicsFrames();
			for (int i = 0; i < _spawnedCharacters.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
				towerDefenseCharacter.gridPos = gridPos;
				towerDefenseCharacter.SetGlobalPositionForPhysicsFrame(vector, physicsFrames);
			}
			_singleEnemyTarget.gridPos = gridPos;
			_singleEnemyTarget.SetGlobalPositionForPhysicsFrame(vector + new Vector2((_singleEnemyTarget.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) ? 48f : (-48f), 0f), physicsFrames);
			_singleEnemyTarget.targetRegistrationComponent?.SetAlive(alive: true);
			_singleEnemyTarget.targetRegistrationComponent?.NotifyTargetStateChanged();
			_singleEnemyRoleUtilityPhaseCompleted = true;
			GD.Print($"[TestCombatFixture] role-attack-utility-phase-complete role={"TowerDefenseZombieSoccer"} components={_exercisedAttackUtilityComponents.Count}/{_singleEnemyAttackUtilityComponentCount} trigger=Put");
		}
	}

	private void TryRunSleepCycleStress(double sampleElapsed)
	{
		if (!EnableSleepCycleStress || _sleepCycleCompleted)
		{
			return;
		}
		if (_sleepCyclePhase == 0)
		{
			if (_singleEnemyFixturePrepared && !(sampleElapsed < Math.Max(0.0, SleepCycleStartAfterSeconds)))
			{
				BeginSleepCycleStress(sampleElapsed);
			}
		}
		else if (_sleepCyclePhase == 1)
		{
			_sleepCycleEnteredCount = CountSleepingStressCharacters();
			if (_sleepCycleEnteredCount < _sleepCycleEligibleCount && sampleElapsed < _sleepCyclePhaseDeadlineSeconds)
			{
				return;
			}
			if (_sleepCycleEligibleCount != SpawnCount || _sleepCycleAppliedCount != _sleepCycleEligibleCount || _sleepCycleEnteredCount != _sleepCycleEligibleCount)
			{
				CompleteSleepCycleStress(passed: false, $"sleep entry mismatch eligible={_sleepCycleEligibleCount} applied={_sleepCycleAppliedCount} entered={_sleepCycleEnteredCount} missingBuffScenes={FormatSleepCycleMissingBuffScenes()} missingEntryScenes={FormatSleepCycleMissingEntryScenes()}");
				return;
			}
			for (int i = 0; i < _spawnedCharacters.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.buff?.DeleteBuff("Sleep");
					towerDefenseCharacter.componentManager?.GetRuntime<SleepComponent>("character.sleep")?.SleepProcessing(0f);
				}
			}
			_sleepCyclePhase = 2;
			_sleepCyclePhaseDeadlineSeconds = sampleElapsed + 3.0;
			GD.Print($"[TestSleepCycle] phase=wake-request entered={_sleepCycleEnteredCount} expected={_sleepCycleEligibleCount}");
		}
		else
		{
			_sleepCycleWokenCount = CountWokenStressCharacters();
			if (_sleepCycleWokenCount >= _sleepCycleEligibleCount || !(sampleElapsed < _sleepCyclePhaseDeadlineSeconds))
			{
				CompleteSleepCycleStress(_sleepCycleWokenCount == _sleepCycleEligibleCount, $"wake result={_sleepCycleWokenCount}/{_sleepCycleEligibleCount}");
			}
		}
	}

	private void BeginSleepCycleStress(double sampleElapsed)
	{
		if (_sleepCyclePhase != 0 || _sleepCycleCompleted)
		{
			return;
		}
		TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = FindHypnotistSleepEventForStress();
		if (!GodotObject.IsInstanceValid(towerDefenseCharacterEventAddBuff))
		{
			CompleteSleepCycleStress(passed: false, "real Hypnotist Sleep event was not found");
			return;
		}
		_sleepCycleEligibleCount = 0;
		_sleepCycleAppliedCount = 0;
		_sleepCycleNormalizedImmunityCount = 0;
		_sleepCycleImmunityRestored = false;
		_spawnedCharacterSleepBuffFlags.Clear();
		int num = 256;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			int num2 = (GodotObject.IsInstanceValid(towerDefenseCharacter?.instance) ? towerDefenseCharacter.instance.unUseBuffFlags : 0);
			_spawnedCharacterSleepBuffFlags.Add(num2);
			SleepComponent sleepComponent = towerDefenseCharacter?.componentManager?.GetRuntime<SleepComponent>("character.sleep");
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && sleepComponent != null && !sleepComponent.IsReleased && sleepComponent.Alive)
			{
				_sleepCycleEligibleCount++;
				if ((num2 & num) != 0)
				{
					towerDefenseCharacter.instance.unUseBuffFlags = num2 & ~num;
					_sleepCycleNormalizedImmunityCount++;
				}
				towerDefenseCharacterEventAddBuff.Execute(towerDefenseCharacter.GlobalPosition, towerDefenseCharacter);
				BuffComponent buff = towerDefenseCharacter.buff;
				if (buff != null && buff.BuffHas("Sleep"))
				{
					_sleepCycleAppliedCount++;
				}
			}
		}
		_sleepCyclePhase = 1;
		_sleepCyclePhaseDeadlineSeconds = sampleElapsed + 3.0;
		GD.Print($"[TestSleepCycle] phase=apply eligible={_sleepCycleEligibleCount} applied={_sleepCycleAppliedCount} expected={SpawnCount} normalizedSleepImmunity={_sleepCycleNormalizedImmunityCount} " + "source=Hypnotist.rangeEvent");
	}

	private TowerDefenseCharacterEventAddBuff FindHypnotistSleepEventForStress()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			if (!(_spawnedCharacters[i] is TowerDefenseZombieHypnotist towerDefenseZombieHypnotist))
			{
				continue;
			}
			num++;
			num2 += towerDefenseZombieHypnotist.rangeEvent.Count;
			for (int j = 0; j < towerDefenseZombieHypnotist.rangeEvent.Count; j++)
			{
				if (!(towerDefenseZombieHypnotist.rangeEvent[j] is TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff))
				{
					continue;
				}
				num3++;
				for (int k = 0; k < towerDefenseCharacterEventAddBuff.buffList.Count; k++)
				{
					if (towerDefenseCharacterEventAddBuff.buffList[k] is TowerDefenseCharacterBuffSleep)
					{
						num4++;
						return towerDefenseCharacterEventAddBuff;
					}
				}
			}
		}
		_sleepCycleHypnotistEventProvider = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieHypnotist>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(_sleepCycleHypnotistEventProvider))
		{
			num++;
			num2 += _sleepCycleHypnotistEventProvider.rangeEvent.Count;
			for (int l = 0; l < _sleepCycleHypnotistEventProvider.rangeEvent.Count; l++)
			{
				if (!(_sleepCycleHypnotistEventProvider.rangeEvent[l] is TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff2))
				{
					continue;
				}
				num3++;
				for (int m = 0; m < towerDefenseCharacterEventAddBuff2.buffList.Count; m++)
				{
					if (towerDefenseCharacterEventAddBuff2.buffList[m] is TowerDefenseCharacterBuffSleep)
					{
						num4++;
						GD.Print("[TestSleepCycle] source=authored-hypnotist-provider scene=res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn");
						return towerDefenseCharacterEventAddBuff2;
					}
				}
			}
		}
		GD.PrintErr($"[TestSleepCycle] Hypnotist sleep source missing roles={num} rangeEvents={num2} addBuffEvents={num3} sleepBuffs={num4}");
		return null;
	}

	private int CountSleepingStressCharacters()
	{
		int num = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.IsSleep() && !towerDefenseCharacter.componentAlive)
			{
				BuffComponent buff = towerDefenseCharacter.buff;
				if (buff != null && buff.BuffHas("Sleep"))
				{
					num++;
				}
			}
		}
		return num;
	}

	private int CountWokenStressCharacters()
	{
		int num = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.IsSleep() && towerDefenseCharacter.componentAlive)
			{
				BuffComponent buff = towerDefenseCharacter.buff;
				if (buff == null || !buff.BuffHas("Sleep"))
				{
					num++;
				}
			}
		}
		return num;
	}

	private string FormatSleepCycleMissingBuffScenes()
	{
		return FormatSleepCycleSceneBreakdown(delegate(TowerDefenseCharacter character)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				BuffComponent buff = character.buff;
				if (buff == null)
				{
					return true;
				}
				return !buff.BuffHas("Sleep");
			}
			return true;
		});
	}

	private string FormatSleepCycleMissingEntryScenes()
	{
		return FormatSleepCycleSceneBreakdown((TowerDefenseCharacter character) => !GodotObject.IsInstanceValid(character) || !character.IsSleep() || character.componentAlive);
	}

	private string FormatSleepCycleSceneBreakdown(Func<TowerDefenseCharacter, bool> isMissing)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		int num = Math.Min(_spawnedCharacters.Count, _spawnedCharacterScenePaths.Count);
		for (int i = 0; i < num; i++)
		{
			if (isMissing(_spawnedCharacters[i]))
			{
				string text = _spawnedCharacterScenePaths[i];
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "<unknown>";
				}
				dictionary[text] = dictionary.GetValueOrDefault(text) + 1;
			}
		}
		if (dictionary.Count == 0)
		{
			return "none";
		}
		List<string> list = new List<string>(dictionary.Keys);
		list.Sort(StringComparer.Ordinal);
		string[] array = new string[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			string text2 = list[j];
			array[j] = $"{text2}:{dictionary[text2]}";
		}
		return string.Join('|', array);
	}

	private void CompleteSleepCycleStress(bool passed, string reason)
	{
		RestoreSleepCycleImmunity();
		_sleepCycleCompleted = true;
		_sleepCyclePassed = passed;
		GD.Print($"[TestSleepCycle] phase=complete passed={passed} reason={reason} eligible={_sleepCycleEligibleCount} applied={_sleepCycleAppliedCount} entered={_sleepCycleEnteredCount} woken={_sleepCycleWokenCount}");
	}

	private void RestoreSleepCycleImmunity()
	{
		if (_sleepCycleImmunityRestored)
		{
			return;
		}
		int num = Math.Min(_spawnedCharacters.Count, _spawnedCharacterSleepBuffFlags.Count);
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _spawnedCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter?.instance))
			{
				towerDefenseCharacter.instance.unUseBuffFlags = _spawnedCharacterSleepBuffFlags[i];
			}
		}
		_sleepCycleImmunityRestored = true;
	}

	private bool IsSleepCycleStressPassed()
	{
		if (!EnableSleepCycleStress)
		{
			return true;
		}
		if (_sleepCycleCompleted && _sleepCyclePassed && _sleepCycleEligibleCount == SpawnCount && _sleepCycleAppliedCount == SpawnCount && _sleepCycleEnteredCount == SpawnCount)
		{
			return _sleepCycleWokenCount == SpawnCount;
		}
		return false;
	}

	private void TryAdvanceSingleEnemyRoleAttackModes()
	{
		if (!EnableSingleEnemyTargetFixture || !_singleEnemyFixturePrepared || _singleEnemyRobotModeAdvanceRequested || _spawnedCharacters.Count == 0)
		{
			return;
		}
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			if (!(_spawnedCharacters[i] is TowerDefensePlantRobot towerDefensePlantRobot))
			{
				return;
			}
			FireComponent fireComponent = towerDefensePlantRobot.componentManager?.GetRuntime<FireComponent>("character.fire");
			if (fireComponent == null || !_engagedFireComponents.Contains(fireComponent))
			{
				return;
			}
		}
		_singleEnemyRobotModeAdvanceRequested = true;
		for (int j = 0; j < _spawnedCharacters.Count; j++)
		{
			((TowerDefensePlantRobot)_spawnedCharacters[j]).DoublePressed(Vector2.Zero);
		}
		GD.Print($"[TestCombatFixture] role-attack-mode-advance role={"TowerDefensePlantRobot"} sources={_spawnedCharacters.Count} " + "trigger=DoublePressed");
	}

	private bool TryAttachLegacyAttackEngagementHandler(TowerDefenseCharacter source, AttackComponent attack)
	{
		if (!(source is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie.attackComponent != attack)
		{
			return false;
		}
		StateHandle attackState = towerDefenseZombie.StateMachine?.GetStateById("zombie.attack");
		StateHandle stateHandle = attackState;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		Action handler = null;
		handler = delegate
		{
			_engagedAttackComponents.Add(attack);
			attackState.Entered -= handler;
			_legacyAttackEngagementHandlers.Remove(attackState);
		};
		_legacyAttackEngagementHandlers[attackState] = handler;
		attackState.Entered += handler;
		return true;
	}

	private void DetachCombatComponentEngagementHandlers()
	{
		if (_attackEngagementHandlers.Count > 0)
		{
			foreach (KeyValuePair<AttackComponent, AttackComponent.AttackEventHandler> attackEngagementHandler in _attackEngagementHandlers)
			{
				attackEngagementHandler.Key.OnAttack -= attackEngagementHandler.Value;
			}
			_attackEngagementHandlers.Clear();
		}
		if (_legacyAttackEngagementHandlers.Count > 0)
		{
			foreach (KeyValuePair<StateHandle, Action> legacyAttackEngagementHandler in _legacyAttackEngagementHandlers)
			{
				legacyAttackEngagementHandler.Key.Entered -= legacyAttackEngagementHandler.Value;
			}
			_legacyAttackEngagementHandlers.Clear();
		}
		if (_fireEngagementHandlers.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<FireComponent, FireComponent.FireVolleyEventHandler> fireEngagementHandler in _fireEngagementHandlers)
		{
			fireEngagementHandler.Key.OnFireVolley -= fireEngagementHandler.Value;
		}
		_fireEngagementHandlers.Clear();
	}

	private bool IsCombatComponentEngagementPassed()
	{
		if (!EnableSingleEnemyTargetFixture)
		{
			return true;
		}
		if (_singleEnemyFixturePrepared && GodotObject.IsInstanceValid(_singleEnemyTarget) && _singleEnemyAttackComponentCount + _singleEnemyAttackUtilityComponentCount + _singleEnemyFireComponentCount > 0 && _engagedAttackComponents.Count == _singleEnemyAttackComponentCount && _exercisedAttackUtilityComponents.Count == _singleEnemyAttackUtilityComponentCount)
		{
			return _engagedFireComponents.Count == _singleEnemyFireComponentCount;
		}
		return false;
	}

	private int CountValidManyEnemyIdleTargets()
	{
		int num = 0;
		for (int i = 0; i < _manyEnemyIdleTargets.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _manyEnemyIdleTargets[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie)
			{
				num++;
			}
		}
		return num;
	}

	private int CountManyEnemyIdleBusyStates()
	{
		int num = 0;
		for (int i = 0; i < _manyEnemyIdleAttackComponents.Count; i++)
		{
			StateHandle stateHandle = _manyEnemyIdleAttackComponents[i]?.StateMachine?.CurrentStateHandle;
			if (stateHandle != null && stateHandle.IsValid && stateHandle.StableId != "attack.idle")
			{
				num++;
			}
		}
		for (int j = 0; j < _manyEnemyIdleFireComponents.Count; j++)
		{
			FireComponent fireComponent = _manyEnemyIdleFireComponents[j];
			if (fireComponent != null && fireComponent.IsFireStateBusy())
			{
				num++;
			}
		}
		return num;
	}

	private bool IsManyEnemyIdleFixturePassed()
	{
		if (!EnableManyEnemyIdleFixture)
		{
			return true;
		}
		int num = Math.Max(1, ManyEnemyIdleTargetCount);
		if (_manyEnemyIdleFixturePrepared && _manyEnemyIdleTargets.Count == num && CountValidManyEnemyIdleTargets() == num && _manyEnemyIdleAttackComponents.Count + _manyEnemyIdleFireComponents.Count > 0 && _engagedAttackComponents.Count == 0 && _engagedFireComponents.Count == 0)
		{
			return CountManyEnemyIdleBusyStates() == 0;
		}
		return false;
	}

	private string FormatAttackComponentEngagementBreakdown(bool utilities)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			ComponentManager componentManager = _spawnedCharacters[i]?.componentManager;
			if (!GodotObject.IsInstanceValid(componentManager))
			{
				continue;
			}
			IReadOnlyList<CharacterComponentRuntime> resourceComponents = componentManager.ResourceComponents;
			for (int j = 0; j < resourceComponents.Count; j++)
			{
				if (resourceComponents[j] is AttackComponent attackComponent && IsRoleUtilityAttackComponent(_spawnedCharacters[i], attackComponent) == utilities)
				{
					string text = attackComponent.ComponentDefinition?.InstanceId;
					if (string.IsNullOrWhiteSpace(text))
					{
						text = "<unknown>";
					}
					string key = text + ":" + attackComponent.attackType;
					dictionary[key] = dictionary.GetValueOrDefault(key) + 1;
					if (utilities ? _exercisedAttackUtilityComponents.Contains(attackComponent) : _engagedAttackComponents.Contains(attackComponent))
					{
						dictionary2[key] = dictionary2.GetValueOrDefault(key) + 1;
					}
				}
			}
		}
		return FormatComponentEngagementBreakdown(dictionary, dictionary2);
	}

	private string FormatFireComponentEngagementBreakdown()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			ComponentManager componentManager = _spawnedCharacters[i]?.componentManager;
			if (!GodotObject.IsInstanceValid(componentManager))
			{
				continue;
			}
			IReadOnlyList<CharacterComponentRuntime> resourceComponents = componentManager.ResourceComponents;
			for (int j = 0; j < resourceComponents.Count; j++)
			{
				if (resourceComponents[j] is FireComponent fireComponent)
				{
					string text = fireComponent.ComponentDefinition?.InstanceId;
					if (string.IsNullOrWhiteSpace(text))
					{
						text = "<unknown>";
					}
					dictionary[text] = dictionary.GetValueOrDefault(text) + 1;
					if (_engagedFireComponents.Contains(fireComponent))
					{
						dictionary2[text] = dictionary2.GetValueOrDefault(text) + 1;
					}
				}
			}
		}
		return FormatComponentEngagementBreakdown(dictionary, dictionary2);
	}

	private static string FormatComponentEngagementBreakdown(Dictionary<string, int> totals, Dictionary<string, int> engaged)
	{
		if (totals.Count == 0)
		{
			return "none";
		}
		List<string> list = new List<string>(totals.Keys);
		list.Sort(StringComparer.Ordinal);
		string[] array = new string[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			array[i] = $"{text}={engaged.GetValueOrDefault(text)}/{totals[text]}";
		}
		return string.Join('|', array);
	}

	private static double GetCombatTargetDurability(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target?.instance))
		{
			return 0.0;
		}
		double num = Math.Max(0.0, target.instance.hitpoints);
		for (int i = 0; i < target.instance.armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = target.instance.armorList[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
			{
				num += Math.Max(0.0, towerDefenseArmorInstance.hitPoints);
			}
		}
		return num;
	}

	private void PrintExactRoleComponentInventory()
	{
		if (!UseZombieCharacterScenes || _spawnedCharacters.Count == 0)
		{
			return;
		}
		ComponentManager componentManager = _spawnedCharacters[0]?.componentManager;
		if (!GodotObject.IsInstanceValid(componentManager))
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		IReadOnlyList<CharacterComponentRuntime> resourceComponents = componentManager.ResourceComponents;
		for (int i = 0; i < resourceComponents.Count; i++)
		{
			string key = resourceComponents[i]?.GetType().Name ?? "<null>";
			dictionary[key] = dictionary.GetValueOrDefault(key) + 1;
		}
		for (int j = 0; j < componentManager.componentList.Count; j++)
		{
			ComponentBase componentBase = componentManager.componentList[j];
			if (GodotObject.IsInstanceValid(componentBase))
			{
				string name = componentBase.GetType().Name;
				dictionary[name] = dictionary.GetValueOrDefault(name) + 1;
			}
		}
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(dictionary);
		list.Sort(delegate(KeyValuePair<string, int> left, KeyValuePair<string, int> right)
		{
			int num3 = right.Value.CompareTo(left.Value);
			return (num3 == 0) ? string.CompareOrdinal(left.Key, right.Key) : num3;
		});
		string[] array = new string[list.Count];
		int num = 0;
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			num += list[num2].Value;
			array[num2] = $"{list[num2].Key}:{list[num2].Value}";
		}
		string value = (string.IsNullOrWhiteSpace(ExactScenePath) ? _spawnedCharacters[0].SceneFilePath : ExactScenePath);
		GD.Print($"[TestComponentInventory] scene={value} runtimeType={_spawnedCharacters[0].GetType().Name} total={num} types={string.Join(',', array)}");
	}

	private static void CollectAnimationSprites(Node node, List<AdobeAnimateSpriteBase> sprites)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSpriteBase item)
		{
			sprites.Add(item);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectAnimationSprites(child, sprites);
		}
	}

	private Vector2 ResolveSpawnPosition(int index)
	{
		if (EnableZInterleaveFixture)
		{
			Vector2 size = GetViewportRect().Size;
			Vector2 vector = size * 0.5f;
			float num = Mathf.Clamp(size.X * 0.22f, 150f, 230f);
			return vector + new Vector2(((index & 1) == 0) ? (0f - num) : num, 70f);
		}
		if (EnableSingleEnemyTargetFixture || EnableManyEnemyIdleFixture)
		{
			return ResolveSingleTargetSourcePosition();
		}
		if (!UseGridSpawnLayout)
		{
			return new Vector2(SpawnArea.X * GD.Randf(), SpawnArea.Y * GD.Randf());
		}
		int num2 = Math.Max(1, SpawnCount);
		float num3 = Math.Max(1f, SpawnArea.X);
		float num4 = Math.Max(1f, SpawnArea.Y);
		int num5 = ((SpawnGridColumns > 0) ? Math.Min(num2, SpawnGridColumns) : Math.Min(num2, Math.Max(1, Mathf.CeilToInt(Mathf.Sqrt((float)num2 * num3 / num4)))));
		int num6 = Math.Max(1, (num2 + num5 - 1) / num5);
		int num7 = index % num5;
		int num8 = index / num5;
		float x = ((num5 <= 1) ? (num3 * 0.5f) : (num3 * (float)num7 / (float)(num5 - 1)));
		float y = ((num6 <= 1) ? (num4 * 0.5f) : (num4 * (float)num8 / (float)(num6 - 1)));
		return SpawnOrigin + new Vector2(x, y);
	}

	private Vector2 ResolveSingleTargetSourcePosition()
	{
		return SpawnOrigin + SpawnArea * 0.5f;
	}

	private static Vector2I ResolveSingleTargetSourceGridPosition()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		Vector2I vector2I = (GodotObject.IsInstanceValid(instance) ? instance.gridNum : new Vector2I(9, 5));
		return new Vector2I(Math.Max(1, (vector2I.X + 1) / 2), Math.Max(1, (vector2I.Y + 1) / 2));
	}

	private void EnsureZInterleaveFixture()
	{
		if (!EnableZInterleaveFixture || GodotObject.IsInstanceValid(_zInterleaveMarker))
		{
			return;
		}
		if (_spawnedNodes.Count != 2)
		{
			GD.PrintErr($"[TestZInterleave] expected two animation roots but spawned={_spawnedNodes.Count}");
			return;
		}
		Vector2 position = GetViewportRect().Size * 0.5f + new Vector2(0f, 40f);
		_zInterleaveMarker = new Polygon2D
		{
			Name = "ZInterleaveMarker",
			ZAsRelative = false,
			ZIndex = 1,
			Position = position,
			Polygon = new Vector2[6]
			{
				new Vector2(-285f, -82f),
				new Vector2(-70f, -138f),
				new Vector2(276f, -72f),
				new Vector2(238f, 112f),
				new Vector2(28f, 84f),
				new Vector2(-246f, 132f)
			},
			VertexColors = new Color[6]
			{
				new Color(1f, 0f, 0.72f, 0.66f),
				new Color(1f, 0f, 0.72f, 0.66f),
				new Color(0f, 1f, 0.35f, 0.66f),
				new Color(0f, 1f, 0.35f, 0.66f),
				new Color(0f, 1f, 0.35f, 0.66f),
				new Color(1f, 0f, 0.72f, 0.66f)
			}
		};
		AddChild(_zInterleaveMarker, forceReadableName: false, InternalMode.Disabled);
		using Image image = Image.CreateEmpty(8, 8, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(1f, 1f, 1f));
		_zInterleaveParticleTexture = ImageTexture.CreateFromImage(image);
		_zInterleaveParticles = new CpuParticles2D
		{
			Name = "ZInterleaveParticles",
			ZAsRelative = false,
			ZIndex = 1,
			Position = position,
			Amount = 72,
			Lifetime = 12.0,
			Preprocess = 12.0,
			OneShot = false,
			Randomness = 0f,
			LifetimeRandomness = 0.0,
			UseFixedSeed = true,
			Seed = 20260710u,
			LocalCoords = true,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(270f, 112f),
			Direction = Vector2.Zero,
			Spread = 0f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = 0f,
			InitialVelocityMax = 0f,
			ScaleAmountMin = 2f,
			ScaleAmountMax = 2f,
			Color = new Color(1f, 0.94f, 0.12f),
			Texture = _zInterleaveParticleTexture,
			Emitting = true
		};
		AddChild(_zInterleaveParticles, forceReadableName: false, InternalMode.Disabled);
		GD.Print("[TestZInterleave] animationZ=0,2 externalZ=1 marker=true particles=true");
	}

	private void PrintRuntimeSnapshot(string label)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		Rect2 worldViewportRect = GetWorldViewportRect();
		foreach (Node2D spawnedNode in _spawnedNodes)
		{
			if (GodotObject.IsInstanceValid(spawnedNode))
			{
				num++;
				if (spawnedNode.ProcessMode == ProcessModeEnum.Disabled)
				{
					num9++;
				}
				if (spawnedNode.IsVisibleInTree())
				{
					num2++;
				}
				if (worldViewportRect.HasPoint(spawnedNode.GlobalPosition))
				{
					num3++;
				}
			}
		}
		foreach (TowerDefenseCharacter spawnedCharacter in _spawnedCharacters)
		{
			if (GodotObject.IsInstanceValid(spawnedCharacter))
			{
				num4++;
				if (spawnedCharacter.IsVisibleInTree())
				{
					num5++;
				}
				if (worldViewportRect.HasPoint(spawnedCharacter.GlobalPosition))
				{
					num6++;
				}
			}
		}
		foreach (AdobeAnimateSpriteBase spawnedAnimationSprite in _spawnedAnimationSprites)
		{
			if (GodotObject.IsInstanceValid(spawnedAnimationSprite))
			{
				if (spawnedAnimationSprite.IsRuntimeTickPaused)
				{
					num8++;
				}
				if (spawnedAnimationSprite.IsRuntimeActive)
				{
					num7++;
				}
			}
		}
		double value = ((_windowFrames > 0) ? (_windowDeltaSeconds * 1000.0 / (double)_windowFrames) : 0.0);
		double value2 = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
		double value3 = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;
		RenderManagerStats renderManagerStats = CollectRenderManagerStats();
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		GetRenderFrameTimes(out var renderCpuMs, out var renderGpuMs);
		double monitor = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
		CollectExternalVisualStats(out var activeVisible, out var nativeFallback, out var expectedVisible);
		long value4 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphBuildCount - _externalVisualGraphBuildCountAtMeasurementStart);
		TowerDefensePerfProfiler.Sample("adobeAnimate.externalVisual.active", activeVisible);
		TowerDefensePerfProfiler.Sample("adobeAnimate.externalVisual.nativeFallback", nativeFallback);
		GD.Print($"[TestPerf] {label} t={_uptime.Elapsed.TotalSeconds:F1}s fps={Engine.GetFramesPerSecond():F1} maxFps={Engine.MaxFps} godotProcessMs={value2:F3} godotPhysicsMs={value3:F3} windowDeltaMs={value:F3} renderCpuMs={renderCpuMs:F3} renderGpuMs={renderGpuMs:F3} drawCalls={monitor:F0} spawned={_spawnedNodes.Count} valid={num} characters={_spawnedCharacters.Count} validCharacters={num4} animationSprites={_spawnedAnimationSprites.Count} runtimeActive={num7} paused={num8} processDisabled={num9} visibleInTree={num2} pointInViewport={num3} visibleCharacters={num5} pointCharacters={num6} worldViewport={FormatRect(worldViewportRect)} renderManagers={renderManagerStats.RenderManagers} normalBuckets={renderManagerStats.NormalBuckets} visibleInstances={renderManagerStats.VisibleInstances} allocatedInstances={renderManagerStats.AllocatedInstances} maxBucketVisible={renderManagerStats.MaxBucketVisible} compactCrowdRoots={renderManagerStats.CompactCrowdRoots} compositeCrowdRoots={renderManagerStats.CompositeCrowdRoots} gpuGraphRoots={renderManagerStats.GpuGraphRoots} gpuGraphSlots={renderManagerStats.GpuGraphSlots} gpuGraphAtlasPages={AdobeAnimateRenderManager.GpuRenderGraphAtlasPageCount} gpuGraphStateTexels={renderManagerStats.GpuGraphStateTexels} crowdStateTexels={renderManagerStats.CrowdStateTexels} activeMounts={aggregateRenderStats.ActiveMounts} activeZBuckets={aggregateRenderStats.ActiveZBuckets} crowdRoots={aggregateRenderStats.CrowdRoots} sharedCrowdStateTexels={aggregateRenderStats.CrowdStateTexels} resourceSignatures={aggregateRenderStats.ResourceSignatures} stateTextureUploads={aggregateRenderStats.StateTextureUploads} multiMeshUploads={aggregateRenderStats.MultiMeshUploads} rdBatches={aggregateRenderStats.RdAppliedBatches}/{aggregateRenderStats.RdSubmittedBatches} rdUploads={aggregateRenderStats.RdAppliedUploads}/{aggregateRenderStats.RdQueuedUploads} rdUploadedBytes={aggregateRenderStats.RdUploadedBytes} rdRidRefreshes={aggregateRenderStats.RdBufferRidRefreshes} rdFailures={aggregateRenderStats.RdFailedBatches}/{aggregateRenderStats.RdBufferUpdateFailures}/{aggregateRenderStats.RdInvalidTargets} rdStaleDrops={aggregateRenderStats.RdStaleDrops} rdQueueDepth={aggregateRenderStats.RdQueueDepth}/{aggregateRenderStats.RdMaximumQueueDepth} rdFrames={aggregateRenderStats.RdLastAppliedFrameVersion}/{aggregateRenderStats.RdLastQueuedFrameVersion} signatureConflictBuckets={aggregateRenderStats.SignatureConflictBuckets} fallbackRuns={aggregateRenderStats.FallbackRuns} fallbackRoots={aggregateRenderStats.FallbackRoots} arenaGrowthCount={aggregateRenderStats.ArenaGrowthCount} crowdMeshQuadCapacity={aggregateRenderStats.CrowdMeshQuadCapacity} crowdMeshVersion={aggregateRenderStats.CrowdMeshVersion} meshRebinds={aggregateRenderStats.MeshRebinds} renderBackend={GetTestRenderBackendLabel()} cpuRoots={aggregateRenderStats.CpuRoots} cpuFallbackRoots={aggregateRenderStats.CpuFallbackRoots} cpuValidationFailures={aggregateRenderStats.CpuValidationFailures} cpuMeshRebuilds={aggregateRenderStats.CpuMeshRebuilds} cpuVertexUploads={aggregateRenderStats.CpuVertexUploads} cpuUploadedVertices={aggregateRenderStats.CpuUploadedVertices} cpuCapacityGrowths={aggregateRenderStats.CpuCapacityGrowths} cpuSkippedUploads={aggregateRenderStats.CpuSkippedUploads}");
		GD.Print($"[TestExternalVisual] sample={label} enabled={EnableExternalVisualStress} ratio={ExternalVisualRatio:F3} requested={_externalVisualExpectedActive} expected={expectedVisible} externalVisualActive={activeVisible} externalVisualFallback={nativeFallback} externalVisualGraphRebuilds={value4}");
		PrintExternalVisualPositionDiagnostics(label);
		PrintCrowdAggregateStats(label, aggregateRenderStats);
	}

	private void PrintExternalVisualPositionDiagnostics(string label)
	{
		if (!EnableExternalVisualStress || _externalVisualStressFixtures.Count > 16)
		{
			return;
		}
		for (int i = 0; i < _externalVisualStressFixtures.Count; i++)
		{
			ExternalVisualStressFixture externalVisualStressFixture = _externalVisualStressFixtures[i];
			if (GodotObject.IsInstanceValid(externalVisualStressFixture.Visual) && GodotObject.IsInstanceValid(externalVisualStressFixture.Owner))
			{
				Vector2 value = (GodotObject.IsInstanceValid(externalVisualStressFixture.Character?.shadowSprite) ? externalVisualStressFixture.Character.shadowSprite.GlobalPosition : new Vector2(0f / 0f, 0f / 0f));
				Vector2 value2 = (GodotObject.IsInstanceValid(externalVisualStressFixture.Character?.headSlot) ? externalVisualStressFixture.Character.headSlot.GlobalPosition : new Vector2(0f / 0f, 0f / 0f));
				Vector2 value3 = new Vector2(0f / 0f, 0f / 0f);
				AdobeAnimateExternalVisualSnapshot visual2;
				if (externalVisualStressFixture.Handle.IsValid && externalVisualStressFixture.Owner.TryGetExternalVisualForRender(externalVisualStressFixture.Handle, out var visual))
				{
					value3 = visual.Transform.Origin;
				}
				else if (externalVisualStressFixture.Character != null && externalVisualStressFixture.Visual == externalVisualStressFixture.Character.icetrapSprite && externalVisualStressFixture.Character.TryGetIceTrapExternalVisualForDiagnostics(out visual2))
				{
					value3 = visual2.Transform.Origin;
				}
				GD.Print($"[TestExternalVisualPosition] sample={label} index={externalVisualStressFixture.StableIndex} owner={FormatVector(externalVisualStressFixture.Owner.GlobalPosition)} visual={FormatVector(externalVisualStressFixture.Visual.GlobalPosition)} shadow={FormatVector(value)} head={FormatVector(value2)} crowdLocal={FormatVector(value3)}");
			}
		}
	}

	private static string FormatVector(Vector2 value)
	{
		return $"({value.X:F2},{value.Y:F2})";
	}

	private void CollectExternalVisualStats(out int activeVisible, out int nativeFallback, out int expectedVisible)
	{
		activeVisible = 0;
		nativeFallback = 0;
		expectedVisible = 0;
		for (int i = 0; i < _externalVisualStressFixtures.Count; i++)
		{
			Sprite2D visual = _externalVisualStressFixtures[i].Visual;
			if (GodotObject.IsInstanceValid(visual) && AdobeAnimateManagedSprite2D.GetLogicalVisible(visual))
			{
				expectedVisible++;
				activeVisible++;
				if (!AdobeAnimateManagedSprite2D.IsCrowdManaged(visual))
				{
					nativeFallback++;
				}
			}
		}
	}

	private void ReportExternalVisualStressFailures()
	{
		for (int i = 0; i < _externalVisualStressFixtures.Count; i++)
		{
			ExternalVisualStressFixture externalVisualStressFixture = _externalVisualStressFixtures[i];
			if (GodotObject.IsInstanceValid(externalVisualStressFixture.Visual) && !AdobeAnimateManagedSprite2D.IsCrowdManaged(externalVisualStressFixture.Visual))
			{
				GD.PrintErr($"[TestExternalVisual] crowd-fallback index={externalVisualStressFixture.StableIndex} scene={externalVisualStressFixture.Character?.SceneFilePath ?? externalVisualStressFixture.Owner?.SceneFilePath} sprite={externalVisualStressFixture.Owner?.SceneFilePath} texture={externalVisualStressFixture.Visual.Texture?.ResourcePath}");
				if (GodotObject.IsInstanceValid(externalVisualStressFixture.Owner))
				{
					GD.Print(externalVisualStressFixture.Owner.BuildCrowdFilterDebugReport($"external-visual-fallback:{externalVisualStressFixture.StableIndex}", 32));
				}
			}
		}
	}

	private static void PrintCrowdAggregateStats(string label, AdobeAnimateCrowdAggregateStats crowdStats)
	{
		GD.Print($"[TestCrowdStats] activeMounts={crowdStats.ActiveMounts} activeZBuckets={crowdStats.ActiveZBuckets} crowdRoots={crowdStats.CrowdRoots} sharedCrowdStateTexels={crowdStats.CrowdStateTexels} resourceSignatures={crowdStats.ResourceSignatures} stateTextureUploads={crowdStats.StateTextureUploads} multiMeshUploads={crowdStats.MultiMeshUploads} signatureConflictBuckets={crowdStats.SignatureConflictBuckets} fallbackRuns={crowdStats.FallbackRuns} fallbackRoots={crowdStats.FallbackRoots} arenaGrowthCount={crowdStats.ArenaGrowthCount} crowdMeshQuadCapacity={crowdStats.CrowdMeshQuadCapacity} crowdMeshVersion={crowdStats.CrowdMeshVersion} meshRebinds={crowdStats.MeshRebinds} sample={label}");
	}

	private double GetAverageMeasuredFps()
	{
		double measuredSeconds = GetMeasuredSeconds();
		if (!(measuredSeconds > 0.0))
		{
			return 0.0;
		}
		return (double)_measurementFrames / measuredSeconds;
	}

	private bool IsSelectedBackendAccepted(in AdobeAnimateCrowdAggregateStats crowdStats)
	{
		if (_testRenderBackend == AdobeAnimateRenderBackend.CpuPose)
		{
			if (crowdStats.CpuFallbackRoots == 0 && crowdStats.CpuValidationFailures == 0 && crowdStats.FallbackRoots == 0)
			{
				return crowdStats.CpuRoots == crowdStats.CrowdRoots;
			}
			return false;
		}
		return true;
	}

	private void GetRenderFrameTimes(out double renderCpuMs, out double renderGpuMs)
	{
		renderCpuMs = RenderingServer.GetFrameSetupTimeCpu();
		renderGpuMs = 0.0;
		Viewport viewport = GetViewport();
		if (GodotObject.IsInstanceValid(viewport))
		{
			Rid viewportRid = viewport.GetViewportRid();
			renderCpuMs += RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewportRid);
			renderGpuMs = RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid);
		}
	}

	private double GetMeasuredSeconds()
	{
		if (!_measurementStarted)
		{
			return 0.0;
		}
		double num = (double.IsNaN(_measurementEndSeconds) ? _uptime.Elapsed.TotalSeconds : _measurementEndSeconds);
		return Math.Max(0.0, num - _measurementStartSeconds);
	}

	private bool PrintPerformanceResult()
	{
		if (_resultPrinted)
		{
			return _performanceResultPassed;
		}
		_resultPrinted = true;
		double measuredSeconds = GetMeasuredSeconds();
		double averageMeasuredFps = GetAverageMeasuredFps();
		long num = (_measurementStarted ? (GC.GetAllocatedBytesForCurrentThread() - _measurementThreadAllocatedBefore) : 0);
		long num2 = (_measurementStarted ? (GC.GetTotalAllocatedBytes() - _measurementTotalAllocatedBefore) : 0);
		int value = (_measurementStarted ? (GC.CollectionCount(0) - _measurementGen0Before) : 0);
		int value2 = (_measurementStarted ? (GC.CollectionCount(1) - _measurementGen1Before) : 0);
		int value3 = (_measurementStarted ? (GC.CollectionCount(2) - _measurementGen2Before) : 0);
		double[] array = _measurementFrameSeconds.ToArray();
		double[] array2 = _measurementSimulationDeltaSeconds.ToArray();
		double[] array3 = _measurementProcessMilliseconds.ToArray();
		double[] array4 = _measurementPhysicsMilliseconds.ToArray();
		double[] array5 = _spawnInstantiateMilliseconds.ToArray();
		double[] array6 = _spawnAddChildReadyMilliseconds.ToArray();
		double[] array7 = _spawnBatchFrameMilliseconds.ToArray();
		double num3 = BenchmarkStatistics.Percentile(array, array.Length, 50.0) * 1000.0;
		double num4 = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
		double num5 = BenchmarkStatistics.Percentile(array, array.Length, 99.0) * 1000.0;
		double num6 = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
		double value4 = BenchmarkStatistics.Percentile(array2, array2.Length, 99.0) * 1000.0;
		double num7 = BenchmarkStatistics.WorstFractionFps(array, array.Length, 0.01);
		double value5 = BenchmarkStatistics.WorstFractionFps(array, array.Length, 0.001);
		double value6 = BenchmarkStatistics.Percentile(array3, array3.Length, 50.0);
		double value7 = BenchmarkStatistics.Percentile(array3, array3.Length, 95.0);
		double value8 = BenchmarkStatistics.Percentile(array3, array3.Length, 99.0);
		double value9 = BenchmarkStatistics.Maximum(array3, array3.Length);
		double value10 = BenchmarkStatistics.Percentile(array4, array4.Length, 50.0);
		double value11 = BenchmarkStatistics.Percentile(array4, array4.Length, 95.0);
		double value12 = BenchmarkStatistics.Percentile(array4, array4.Length, 99.0);
		double value13 = BenchmarkStatistics.Maximum(array4, array4.Length);
		double value14 = BenchmarkStatistics.Percentile(array5, array5.Length, 50.0);
		double value15 = BenchmarkStatistics.Percentile(array5, array5.Length, 95.0);
		double value16 = BenchmarkStatistics.Maximum(array5, array5.Length);
		double value17 = BenchmarkStatistics.Percentile(array6, array6.Length, 50.0);
		double value18 = BenchmarkStatistics.Percentile(array6, array6.Length, 95.0);
		double value19 = BenchmarkStatistics.Maximum(array6, array6.Length);
		double value20 = BenchmarkStatistics.Percentile(array7, array7.Length, 95.0);
		double num8 = BenchmarkStatistics.Maximum(array7, array7.Length);
		double[] array8 = _viewportEntryFrameSeconds.ToArray();
		double value21 = BenchmarkStatistics.Percentile(array8, array8.Length, 95.0) * 1000.0;
		double value22 = BenchmarkStatistics.Maximum(array8, array8.Length) * 1000.0;
		int num9 = 0;
		for (int i = 0; i < _spawnedNodes.Count; i++)
		{
			Node2D node2D = _spawnedNodes[i];
			if (GodotObject.IsInstanceValid(node2D))
			{
				num9 += CountStateChartRelatedNodes(node2D);
			}
		}
		double monitor = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
		double monitor2 = Performance.GetMonitor(Performance.Monitor.MemoryStaticMax);
		double monitor3 = Performance.GetMonitor(Performance.Monitor.MemoryMessageBufferMax);
		double monitor4 = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed);
		double monitor5 = Performance.GetMonitor(Performance.Monitor.RenderTextureMemUsed);
		double monitor6 = Performance.GetMonitor(Performance.Monitor.RenderBufferMemUsed);
		double monitor7 = Performance.GetMonitor(Performance.Monitor.ObjectCount);
		double monitor8 = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
		double monitor9 = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
		double monitor10 = Performance.GetMonitor(Performance.Monitor.ObjectResourceCount);
		double monitor11 = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
		GetRenderFrameTimes(out var renderCpuMs, out var renderGpuMs);
		double monitor12 = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
		double monitor13 = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
		CollectExternalVisualStats(out var activeVisible, out var nativeFallback, out var expectedVisible);
		long value23 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphBuildCount - _externalVisualGraphBuildCountAtMeasurementStart);
		long value24 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphInitialBuildCount - _gpuGraphInitialBuildCountAtMeasurementStart);
		long value25 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphRebuildCount - _gpuGraphRebuildCountAtMeasurementStart);
		long value26 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphOwnerExitInvalidationCount - _gpuGraphOwnerExitInvalidationCountAtMeasurementStart);
		long value27 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphManagedSlotInvalidationCount - _gpuGraphManagedSlotInvalidationCountAtMeasurementStart);
		long value28 = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphExternalVisualInvalidationCount - _gpuGraphExternalVisualInvalidationCountAtMeasurementStart);
		AdobeAnimateCrowdAggregateStats crowdStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		if (EnableExternalVisualStress)
		{
			ReportExternalVisualStressFailures();
		}
		bool flag = IsAnimationCadencePassed();
		bool flag2 = !EnableExternalVisualStress || (activeVisible == expectedVisible && nativeFallback == 0 && _externalVisualConfigurationFailures == 0);
		bool flag3 = _spawnFrameBudgetPassed && (SpawnTimeBudgetMilliseconds <= 0.0 || array7.Length == 0 || num8 <= TowerDefenseCharacterSpawnBudget.ClampFrameBudget(SpawnTimeBudgetMilliseconds));
		int num10 = CountValidSpawnedCharacters();
		int value29 = CountValidManyEnemyIdleTargets();
		int value30 = CountManyEnemyIdleBusyStates();
		bool flag4 = !PreserveCombatCharactersInStress || (_spawnedCharacters.Count == SpawnCount && num10 == SpawnCount);
		bool value31 = GodotObject.IsInstanceValid(ResourceManager.Instance) && ResourceManager.Instance.AreFullGameplayResourcesReady;
		double value32 = (GodotObject.IsInstanceValid(ResourceManager.Instance) ? ResourceManager.Instance.FullGameplayResourceLoadMetrics.FullWallMilliseconds : 0.0);
		bool flag5 = IsAnimationSwitchStressPassed();
		bool flag6 = IsBowlingImpactTweenStressPassed();
		bool flag7 = MaximumFrameP99Milliseconds <= 0.0 || num5 <= MaximumFrameP99Milliseconds;
		bool flag8 = MaximumFrameMilliseconds <= 0.0 || num6 <= MaximumFrameMilliseconds;
		bool flag9 = MinimumOnePercentLowFps <= 0.0 || num7 >= MinimumOnePercentLowFps;
		bool value33 = (_performanceResultPassed = (MinimumAverageFps <= 0.0 || averageMeasuredFps >= MinimumAverageFps) && HasValidMeasurementTelemetry() && double.IsFinite(num3) && double.IsFinite(num4) && double.IsFinite(num5) && double.IsFinite(num6) && flag7 && flag8 && flag9 && num >= 0 && num2 >= 0 && flag && flag2 && flag3 && flag4 && flag5 && flag6 && IsCombatComponentEngagementPassed() && IsSleepCycleStressPassed() && IsManyEnemyIdleFixturePassed() && IsSelectedBackendAccepted(in crowdStats));
		GD.Print($"[TestPerfResult] runtimeProfile={RuntimeProfile} spawn={_spawnedNodes.Count} averageFps={averageMeasuredFps:F2} onePercentLowFps={num7:F2} zeroPointOnePercentLowFps={value5:F2} frameClock=wall physicsHz={Engine.PhysicsTicksPerSecond} renderTargetFps={MinimumAverageFps:F2} frameP50Ms={num3:F3} frameP95Ms={num4:F3} frameP99Ms={num5:F3} frameMaxMs={num6:F3} simulationDeltaP99Ms={value4:F3} targetFrameP99Ms={MaximumFrameP99Milliseconds:F3} targetFrameMaxMs={MaximumFrameMilliseconds:F3} targetOnePercentLowFps={MinimumOnePercentLowFps:F2} frameP99Passed={flag7} frameMaxPassed={flag8} onePercentLowPassed={flag9} processP50Ms={value6:F3} processP95Ms={value7:F3} processP99Ms={value8:F3} processMaxMs={value9:F3} physicsP50Ms={value10:F3} physicsP95Ms={value11:F3} physicsP99Ms={value12:F3} physicsMaxMs={value13:F3} staticMemoryBytes={monitor:F0} staticMemoryPeakBytes={monitor2:F0} messageBufferPeakBytes={monitor3:F0} videoMemoryBytes={monitor4:F0} textureMemoryBytes={monitor5:F0} bufferMemoryBytes={monitor6:F0} objectCount={monitor7:F0} nodeCount={monitor8:F0} orphanNodeCount={monitor9:F0} resourceCount={monitor10:F0} drawCalls={monitor11:F0} renderCpuMs={renderCpuMs:F3} renderGpuMs={renderGpuMs:F3} renderBackend={GetTestRenderBackendLabel()} cpuRoots={crowdStats.CpuRoots} combatStateStress={EnableCombatStateStress} preserveCombatCharacters={PreserveCombatCharactersInStress} singleEnemyTarget={EnableSingleEnemyTargetFixture} singleEnemyTargetValid={GodotObject.IsInstanceValid(_singleEnemyTarget)} singleEnemyTargetDamage={Math.Max(0.0, _singleEnemyTargetInitialHitpoints - GetCombatTargetDurability(_singleEnemyTarget)):F3} attackComponentsEngaged={_engagedAttackComponents.Count}/{_singleEnemyAttackComponentCount} attackUtilityComponentsExercised={_exercisedAttackUtilityComponents.Count}/{_singleEnemyAttackUtilityComponentCount} fireComponentsEngaged={_engagedFireComponents.Count}/{_singleEnemyFireComponentCount} attackComponentEngagementBreakdown={FormatAttackComponentEngagementBreakdown(utilities: false)} attackUtilityComponentBreakdown={FormatAttackComponentEngagementBreakdown(utilities: true)} fireComponentEngagementBreakdown={FormatFireComponentEngagementBreakdown()} combatComponentEngagementPassed={IsCombatComponentEngagementPassed()} sleepCycleStress={EnableSleepCycleStress} sleepCycleEligible={_sleepCycleEligibleCount}/{SpawnCount} sleepCycleApplied={_sleepCycleAppliedCount}/{_sleepCycleEligibleCount} sleepCycleEntered={_sleepCycleEnteredCount}/{_sleepCycleEligibleCount} sleepCycleWoken={_sleepCycleWokenCount}/{_sleepCycleEligibleCount} sleepCycleNormalizedImmunity={_sleepCycleNormalizedImmunityCount} sleepCyclePassed={IsSleepCycleStressPassed()} manyEnemyIdle={EnableManyEnemyIdleFixture} manyEnemyIdleOutsideRange={ManyEnemyIdleTargetsOutsideAttackRange} manyEnemyIdleTargets={value29}/{Math.Max(1, ManyEnemyIdleTargetCount)} manyEnemyIdleUnexpectedAttacks={_engagedAttackComponents.Count + _engagedFireComponents.Count} manyEnemyIdleBusyStates={value30}/{_manyEnemyIdleAttackComponents.Count + _manyEnemyIdleFireComponents.Count} manyEnemyIdlePassed={IsManyEnemyIdleFixturePassed()} validCharactersAtResult={num10} invalidCharacterScenes={FormatInvalidSpawnedCharacterScenes()} preservedCharacterCountPassed={flag4} fullGameplayResourcesReady={value31} fullGameplayLoadMs={value32:F3} showHealthStress={ShowCharacterHealthInStress} healthDrawDisplays={TowerDefenseHealthDisplayBatch.ActiveDisplayCount} healthDrawLayers={TowerDefenseHealthDisplayBatch.ActiveLayerCount} healthDrawRebuilds={TowerDefenseHealthDisplayBatch.DrawRebuildCount} healthTextLayouts={TowerDefenseHealthDisplayBatch.CachedTextLineCount} healthTextLayoutBuilds={TowerDefenseHealthDisplayBatch.TextLayoutBuildCount} cpuFallbackRoots={crowdStats.CpuFallbackRoots} cpuValidationFailures={crowdStats.CpuValidationFailures} cpuMeshRebuilds={crowdStats.CpuMeshRebuilds} cpuVertexUploads={crowdStats.CpuVertexUploads} cpuUploadedVertices={crowdStats.CpuUploadedVertices} cpuCapacityGrowths={crowdStats.CpuCapacityGrowths} cpuSkippedUploads={crowdStats.CpuSkippedUploads} renderObjects={monitor12:F0} renderPrimitives={monitor13:F0} threadAllocatedBytes={num} totalAllocatedBytes={num2} gen0={value} gen1={value2} gen2={value3} stateChartNodeCount={num9} instantiateP50Ms={value14:F3} instantiateP95Ms={value15:F3} instantiateMaxMs={value16:F3} addChildReadyP50Ms={value17:F3} addChildReadyP95Ms={value18:F3} addChildReadyMaxMs={value19:F3} instantiateMaxScene={_spawnInstantiateMaxScene} addChildReadyMaxScene={_spawnAddChildReadyMaxScene} spawnBatchFrames={array7.Length} spawnBatchFrameP95Ms={value20:F3} spawnBatchFrameMaxMs={num8:F3} spawnFrameBudgetPassed={flag3} viewportEntryFrames={array8.Length} viewportEntryFrameP95Ms={value21:F3} viewportEntryFrameMaxMs={value22:F3} externalVisualActive={activeVisible} externalVisualExpected={expectedVisible} externalVisualRequested={_externalVisualExpectedActive} externalVisualFallback={nativeFallback} externalVisualConfigurationFailures={_externalVisualConfigurationFailures} externalVisualGraphRebuilds={value23} gpuGraphInitialBuilds={value24} gpuGraphRebuilds={value25} gpuGraphOwnerExitInvalidations={value26} gpuGraphManagedSlotInvalidations={value27} gpuGraphExternalVisualInvalidations={value28} animationSwitchStress={EnableAnimationSwitchStress} animationSwitchStressPassed={flag5} bowlingImpactTweenStress={EnableBowlingImpactTweenStress} bowlingImpactTweenStressPassed={flag6} targetFps={MinimumAverageFps:F2} passed={value33} warmupSeconds={WarmupSeconds:F2} measuredSeconds={measuredSeconds:F2} frames={_measurementFrames} screenshotSaved={_screenshotSaved}");
		PrintAnimationSwitchStressResult(flag5);
		PrintBowlingImpactTweenStressResult(flag6);
		if (EnableAllocationTelemetry)
		{
			GD.Print($"[TestAllocationBreakdown] runtime={GetAllocationMetric(TowerDefenseAllocationMetric.RuntimeProcess)} animationDisplay={GetAllocationMetric(TowerDefenseAllocationMetric.AnimationDisplay)} animationLogic={GetAllocationMetric(TowerDefenseAllocationMetric.AnimationLogic)} renderDispatch={GetAllocationMetric(TowerDefenseAllocationMetric.RenderDispatch)} collect={GetAllocationMetric(TowerDefenseAllocationMetric.RenderCollect)} encode={GetAllocationMetric(TowerDefenseAllocationMetric.RenderEncode)} freeze={GetAllocationMetric(TowerDefenseAllocationMetric.RenderFreeze)} publish={GetAllocationMetric(TowerDefenseAllocationMetric.RenderPublish)} stateUpload={GetAllocationMetric(TowerDefenseAllocationMetric.StateTextureUpload)} bucketPublish={GetAllocationMetric(TowerDefenseAllocationMetric.BucketPublish)} multiMeshUpload={GetAllocationMetric(TowerDefenseAllocationMetric.MultiMeshUpload)} externalVisualCommit={GetAllocationMetric(TowerDefenseAllocationMetric.ExternalVisualCommit)} componentPhysics={GetAllocationMetric(TowerDefenseAllocationMetric.ComponentPhysics)} characterPhysics={GetAllocationMetric(TowerDefenseAllocationMetric.CharacterPhysics)}");
		}
		if (RunAnimationCadenceRegression)
		{
			double value34 = ((_cadenceSamples > 0) ? ((double)_cadenceAdvances / (double)_cadenceSamples) : 0.0);
			double value35 = ((_cadenceSamples > 0) ? ((double)_cadenceSlotAdvances / (double)_cadenceSamples) : 0.0);
			GD.Print($"[TestAnimationCadenceResult] samples={_cadenceSamples} advances={_cadenceAdvances} ratio={value34:F4} slotAdvances={_cadenceSlotAdvances} slotRatio={value35:F4} trueFrameRate={_cadenceSprite?.trueFrameRate ?? 0.0:F2} passed={flag}");
		}
		return _performanceResultPassed;
	}

	private bool IsAnimationSwitchStressPassed()
	{
		if (EnableAnimationSwitchStress)
		{
			if (_animationSwitchCount > 0 && _animationSwitchAppliedCount > 0)
			{
				return _animationSwitchMissingClipCount == 0;
			}
			return false;
		}
		return true;
	}

	private void PrintAnimationSwitchStressResult(bool passed)
	{
		if (EnableAnimationSwitchStress)
		{
			double[] array = _animationSwitchWindowFrameSeconds.ToArray();
			double[] array2 = _animationSwitchSteadyFrameSeconds.ToArray();
			double value = BenchmarkStatistics.Average(array, array.Length) * 1000.0;
			double value2 = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
			double value3 = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
			double value4 = BenchmarkStatistics.Average(array2, array2.Length) * 1000.0;
			double value5 = BenchmarkStatistics.Percentile(array2, array2.Length, 95.0) * 1000.0;
			double value6 = BenchmarkStatistics.Maximum(array2, array2.Length) * 1000.0;
			double value7 = ((_animationSwitchCount > 0) ? (_animationSwitchSynchronousTotalMilliseconds / (double)_animationSwitchCount) : 0.0);
			GD.Print($"[TestAnimationSwitchResult] switches={_animationSwitchCount} applied={_animationSwitchAppliedCount} missingClip={_animationSwitchMissingClipCount} syncAverageMs={value7:F3} syncMaxMs={_animationSwitchSynchronousMaxMilliseconds:F3} switchFrames={array.Length} switchAverageMs={value:F3} switchP95Ms={value2:F3} switchMaxMs={value3:F3} steadyFrames={array2.Length} steadyAverageMs={value4:F3} steadyP95Ms={value5:F3} steadyMaxMs={value6:F3} blend={AnimationSwitchBlendSeconds:F3} passed={passed}");
		}
	}

	private bool IsBowlingImpactTweenStressPassed()
	{
		if (EnableBowlingImpactTweenStress)
		{
			if (_bowlingImpactBurstCount > 0 && _bowlingImpactAppliedCount > 0 && _bowlingImpactMissingShieldCount == 0 && (!BowlingImpactUseProductionEvent || _bowlingImpactMaxBatchedMotions > 0) && (!BowlingImpactEnsureShield || (_bowlingImpactMaxBatchedShieldVisuals > 0 && TowerDefenseShieldImpactBatch.ActiveCount == 0)))
			{
				if (BowlingImpactProcessedTweenLimit >= 0)
				{
					return _bowlingImpactMaxProcessedTweens <= BowlingImpactProcessedTweenLimit;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private void PrintBowlingImpactTweenStressResult(bool passed)
	{
		if (EnableBowlingImpactTweenStress)
		{
			double[] array = _bowlingImpactWindowFrameSeconds.ToArray();
			double[] array2 = _bowlingImpactSteadyFrameSeconds.ToArray();
			double value = BenchmarkStatistics.Average(array, array.Length) * 1000.0;
			double value2 = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
			double value3 = BenchmarkStatistics.Percentile(array, array.Length, 99.0) * 1000.0;
			double value4 = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
			double value5 = BenchmarkStatistics.Average(array2, array2.Length) * 1000.0;
			double value6 = BenchmarkStatistics.Percentile(array2, array2.Length, 95.0) * 1000.0;
			double value7 = BenchmarkStatistics.Percentile(array2, array2.Length, 99.0) * 1000.0;
			double value8 = BenchmarkStatistics.Maximum(array2, array2.Length) * 1000.0;
			double value9 = ((_bowlingImpactBurstCount > 0) ? (_bowlingImpactSynchronousTotalMilliseconds / (double)_bowlingImpactBurstCount) : 0.0);
			double value10 = ((_bowlingImpactAppliedCount > 0) ? (_bowlingImpactSynchronousTotalMilliseconds * 1000.0 / (double)_bowlingImpactAppliedCount) : 0.0);
			double value11 = ((_bowlingImpactAppliedCount > 0) ? ((double)_bowlingImpactThreadAllocatedBytes / (double)_bowlingImpactAppliedCount) : 0.0);
			GD.Print($"[TestBowlingImpactTweenResult] bursts={_bowlingImpactBurstCount} applied={_bowlingImpactAppliedCount} missingShield={_bowlingImpactMissingShieldCount} syncAverageMs={value9:F3} syncMaxMs={_bowlingImpactSynchronousMaxMilliseconds:F3} syncPerImpactUs={value10:F3} threadAllocatedBytes={_bowlingImpactThreadAllocatedBytes} allocatedBytesPerImpact={value11:F2} maxProcessedTweens={_bowlingImpactMaxProcessedTweens} processedTweenLimit={BowlingImpactProcessedTweenLimit} maxBatchedMotions={_bowlingImpactMaxBatchedMotions} maxBatchedShieldVisuals={_bowlingImpactMaxBatchedShieldVisuals} remainingBatchedShieldVisuals={TowerDefenseShieldImpactBatch.ActiveCount} productionEvent={BowlingImpactUseProductionEvent} impactFrames={array.Length} impactAverageMs={value:F3} impactP95Ms={value2:F3} impactP99Ms={value3:F3} impactMaxMs={value4:F3} steadyFrames={array2.Length} steadyAverageMs={value5:F3} steadyP95Ms={value6:F3} steadyP99Ms={value7:F3} steadyMaxMs={value8:F3} passed={passed}");
		}
	}

	private int CountValidSpawnedCharacters()
	{
		int num = 0;
		for (int i = 0; i < _spawnedCharacters.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_spawnedCharacters[i]))
			{
				num++;
			}
		}
		return num;
	}

	private string FormatInvalidSpawnedCharacterScenes()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		int num = Math.Min(_spawnedCharacters.Count, _spawnedCharacterScenePaths.Count);
		for (int i = 0; i < num; i++)
		{
			if (!GodotObject.IsInstanceValid(_spawnedCharacters[i]))
			{
				string text = _spawnedCharacterScenePaths[i];
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "<unknown>";
				}
				dictionary[text] = dictionary.GetValueOrDefault(text) + 1;
			}
		}
		if (dictionary.Count == 0)
		{
			return "none";
		}
		List<string> list = new List<string>(dictionary.Keys);
		list.Sort(StringComparer.Ordinal);
		string[] array = new string[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			string text2 = list[j];
			array[j] = $"{text2}:{dictionary[text2]}";
		}
		return string.Join('|', array);
	}

	private static int CountStateChartRelatedNodes(Node node)
	{
		bool flag = ((node is StateChart || node is StateChartState || node is Transition) ? true : false);
		int num = (flag ? 1 : 0);
		foreach (Node child in node.GetChildren())
		{
			num += CountStateChartRelatedNodes(child);
		}
		return num;
	}

	private static string GetAllocationMetric(TowerDefenseAllocationMetric metric)
	{
		return $"{TowerDefenseAllocationTelemetry.GetAllocatedBytes(metric)}/{TowerDefenseAllocationTelemetry.GetSampleCount(metric)}";
	}

	private void TryCapturePerformanceScreenshot()
	{
		if (_screenshotRequested || string.IsNullOrWhiteSpace(ScreenshotPath) || (WaitForDamagePartSettledBeforeScreenshot && !HasSettledDamagePartForScreenshot()))
		{
			return;
		}
		_screenshotRequested = true;
		Viewport viewport = GetViewport();
		if (!GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(viewport.GetTexture()))
		{
			_screenshotRequested = false;
			GD.PrintErr("[TestPerf] screenshot failed: viewport texture is unavailable.");
			return;
		}
		string text = ScreenshotPath.Replace('\\', '/');
		if (!text.Contains("://", StringComparison.Ordinal))
		{
			text = "res://" + text.TrimStart('/');
		}
		string text2 = ProjectSettings.GlobalizePath(text);
		Error error = DirAccess.MakeDirRecursiveAbsolute(text2.GetBaseDir());
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			_screenshotRequested = false;
			GD.PrintErr($"[TestPerf] screenshot directory failed path={text2.GetBaseDir()} error={error}");
			return;
		}
		Image image = viewport.GetTexture().GetImage();
		Error error2 = image.SavePng(text2);
		_screenshotSaved = error2 == Error.Ok;
		if (_screenshotSaved)
		{
			GD.Print($"[TestPerf] screenshot saved path={text} size={image.GetWidth()}x{image.GetHeight()}");
		}
		else
		{
			_screenshotRequested = false;
			GD.PrintErr($"[TestPerf] screenshot failed path={text} error={error2}");
		}
	}

	private static bool HasSettledDamagePartForScreenshot()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return false;
		}
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is DamagePartDrop { over: not false } damagePartDrop && GodotObject.IsInstanceValid(damagePartDrop.sprite) && !damagePartDrop.IsQueuedForDeletion())
			{
				return true;
			}
		}
		return false;
	}

	private void PrintScenePoolSnapshot(string label)
	{
		if (!UseDifferentZombieScenePool)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		string value = "";
		for (int i = 0; i < _sceneUseCounts.Length; i++)
		{
			int num3 = _sceneUseCounts[i];
			if (num3 > 0)
			{
				num++;
				if (num3 > num2)
				{
					num2 = num3;
					value = ((i < _scenePoolPaths.Count) ? _scenePoolPaths[i] : "");
				}
			}
		}
		GD.Print($"[TestPerf] {label} scenePool root={ZombieSceneRoot} excludedPath={ExcludedZombieScenePathSubstring} excluded={_excludedScenePoolCount} characterScenes={UseZombieCharacterScenes} pool={_scenePool.Count} uniqueUsed={num} maxSceneUse={num2} maxScene={value}");
	}

	private RenderManagerStats CollectRenderManagerStats()
	{
		RenderManagerStats stats = default(RenderManagerStats);
		CollectRenderManagerStats(GetTree().Root, ref stats);
		return stats;
	}

	private static void CollectRenderManagerStats(Node node, ref RenderManagerStats stats)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateRenderManager)
		{
			stats.RenderManagers++;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child is AdobeAnimateMultiMeshBatcher adobeAnimateMultiMeshBatcher)
			{
				stats.CompactCrowdRoots += adobeAnimateMultiMeshBatcher.GetCompactCrowdRootCountForTest();
				stats.CompositeCrowdRoots += adobeAnimateMultiMeshBatcher.GetCompositeCrowdRootCountForTest();
				stats.GpuGraphRoots += adobeAnimateMultiMeshBatcher.GetGpuGraphRootCountForTest();
				stats.GpuGraphSlots += adobeAnimateMultiMeshBatcher.GetGpuGraphSlotCountForTest();
				stats.GpuGraphStateTexels += adobeAnimateMultiMeshBatcher.GetGpuGraphStateWrittenTexelsForTest();
				stats.CrowdStateTexels += adobeAnimateMultiMeshBatcher.GetCrowdStateWrittenTexelsForTest();
			}
			if (child is MultiMeshInstance2D { Multimesh: not null } multiMeshInstance2D)
			{
				if (child.Name.ToString().StartsWith("AdobeAnimateBucket", StringComparison.Ordinal))
				{
					stats.NormalBuckets++;
				}
				int num = Math.Max(0, multiMeshInstance2D.Multimesh.VisibleInstanceCount);
				stats.VisibleInstances += num;
				stats.AllocatedInstances += Math.Max(0, multiMeshInstance2D.Multimesh.InstanceCount);
				if (num > stats.MaxBucketVisible)
				{
					stats.MaxBucketVisible = num;
				}
			}
			CollectRenderManagerStats(child, ref stats);
		}
	}

	private void PrintCrowdFilterDebugSnapshot(string label)
	{
		if (!PrintCrowdFilterDebugData)
		{
			return;
		}
		int num = 0;
		int num2 = Math.Max(0, CrowdFilterDebugSpriteCount);
		for (int i = 0; i < _spawnedAnimationSprites.Count; i++)
		{
			if (num >= num2)
			{
				break;
			}
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = _spawnedAnimationSprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
			{
				adobeAnimateSpriteBase.PrintCrowdFilterDebugReport($"Test.{label}.animationSpriteIndex={i}", CrowdFilterDebugMaxFrameSlices);
				num++;
			}
		}
		GD.Print($"[TestCrowdFilterDebug] label={label} printed={num}/{num2} spawned={_spawnedNodes.Count} animationSprites={_spawnedAnimationSprites.Count}");
	}

	private Rect2 GetWorldViewportRect()
	{
		Viewport viewport = GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return new Rect2(Vector2.Zero, SpawnArea);
		}
		Rect2 visibleRect = viewport.GetVisibleRect();
		Transform2D transform2D = viewport.GetCanvasTransform().AffineInverse();
		Vector2 position = transform2D * visibleRect.Position;
		Vector2 to = transform2D * (visibleRect.Position + new Vector2(visibleRect.Size.X, 0f));
		Vector2 to2 = transform2D * (visibleRect.Position + visibleRect.Size);
		Vector2 to3 = transform2D * (visibleRect.Position + new Vector2(0f, visibleRect.Size.Y));
		return new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
	}

	private static double ElapsedMs(long startTicks)
	{
		return (double)(Stopwatch.GetTimestamp() - startTicks) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static string FormatRect(Rect2 rect)
	{
		return $"({rect.Position.X:F1},{rect.Position.Y:F1},{rect.Size.X:F1},{rect.Size.Y:F1})";
	}

	private static string NormalizeResDirectory(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "res://";
		}
		string text = path.Replace('\\', '/').TrimEnd('/');
		if (text.Length != 0)
		{
			return text;
		}
		return "res://";
	}

	private static string GetFileNameWithoutExtension(string path)
	{
		int num = path.LastIndexOf('/');
		string text = ((num >= 0 && num + 1 < path.Length) ? path.Substring(num + 1) : path);
		int num2 = text.LastIndexOf('.');
		if (num2 <= 0)
		{
			return text;
		}
		return text.Substring(0, num2);
	}

	private void ConfigureUnfocusedBenchmarkRun()
	{
		if (KeepRunningWhenUnfocused)
		{
			_originalLowProcessorUsageMode = OS.LowProcessorUsageMode;
			_unfocusedRunConfigured = true;
			base.ProcessMode = ProcessModeEnum.Always;
			OS.LowProcessorUsageMode = false;
			SceneTree tree = GetTree();
			if (GodotObject.IsInstanceValid(tree))
			{
				tree.Paused = false;
			}
			GD.Print("[TestPerf] unfocused pause and low-processor throttling disabled.");
		}
	}

	private void ApplyCommandLineOverrides()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value2;
			int value3;
			double value4;
			int value5;
			int value6;
			Vector2 value7;
			Vector2 value8;
			float value9;
			double value10;
			double value11;
			string value12;
			double value13;
			double value14;
			double value15;
			double value16;
			int value17;
			int value18;
			string value19;
			bool value20;
			string value21;
			string value22;
			double value23;
			bool value24;
			bool value25;
			string value26;
			string value27;
			string value28;
			bool value29;
			bool value30;
			bool value31;
			bool value32;
			int value33;
			bool value34;
			bool value35;
			bool value36;
			bool value37;
			int value38;
			double value39;
			bool value40;
			bool value41;
			bool value42;
			double value43;
			double value44;
			double value45;
			string value46;
			double value47;
			bool value48;
			string value49;
			string value50;
			double value51;
			double value52;
			double value53;
			bool value54;
			bool value55;
			bool value56;
			bool value57;
			int value58;
			int value59;
			double value60;
			double value61;
			double value62;
			double value63;
			bool value64;
			bool value65;
			bool value66;
			bool value67;
			bool value68;
			double value69;
			bool value70;
			bool value71;
			bool value72;
			bool value73;
			bool value74;
			bool value75;
			double value76;
			bool value77;
			int value78;
			bool value79;
			string value80;
			float value81;
			string value82;
			if (TryReadIntArg(text, "--test-spawn-count=", out var value))
			{
				SpawnCount = Math.Max(0, value);
			}
			else if (TryReadIntArg(text, "--test-max-scenes=", out value2))
			{
				MaxDifferentZombieScenes = Math.Max(0, value2);
			}
			else if (TryReadIntArg(text, "--test-spawn-batch-size=", out value3))
			{
				SpawnBatchSize = Math.Max(0, value3);
			}
			else if (TryReadDoubleArg(text, "--test-spawn-time-budget-ms=", out value4))
			{
				SpawnTimeBudgetMilliseconds = Math.Max(0.0, value4);
			}
			else if (TryReadIntArg(text, "--test-grid-columns=", out value5))
			{
				SpawnGridColumns = Math.Max(0, value5);
			}
			else if (TryReadIntArg(text, "--test-z-buckets=", out value6))
			{
				RenderZBucketCount = Math.Max(0, value6);
			}
			else if (TryReadVector2Arg(text, "--test-spawn-area=", out value7))
			{
				SpawnArea = new Vector2(Math.Max(1f, value7.X), Math.Max(1f, value7.Y));
			}
			else if (TryReadVector2Arg(text, "--test-spawn-origin=", out value8))
			{
				SpawnOrigin = value8;
			}
			else if (TryReadFloatArg(text, "--test-spawn-scale=", out value9))
			{
				SpawnScale = Math.Max(0f, value9);
			}
			else if (TryReadDoubleArg(text, "--test-auto-quit=", out value10))
			{
				AutoQuitAfterSeconds = Math.Max(0.0, value10);
			}
			else if (TryReadDoubleArg(text, "--test-warmup=", out value11))
			{
				WarmupSeconds = Math.Max(0.0, value11);
			}
			else if (TryReadStringArg(text, "--runtime-profile=", out value12))
			{
				RuntimeProfile = value12;
			}
			else if (TryReadDoubleArg(text, "--test-min-average-fps=", out value13))
			{
				MinimumAverageFps = Math.Max(0.0, value13);
			}
			else if (TryReadDoubleArg(text, "--test-max-frame-p99-ms=", out value14))
			{
				MaximumFrameP99Milliseconds = Math.Max(0.0, value14);
			}
			else if (TryReadDoubleArg(text, "--test-max-frame-ms=", out value15))
			{
				MaximumFrameMilliseconds = Math.Max(0.0, value15);
			}
			else if (TryReadDoubleArg(text, "--test-min-one-percent-low-fps=", out value16))
			{
				MinimumOnePercentLowFps = Math.Max(0.0, value16);
			}
			else if (TryReadIntArg(text, "--test-max-fps=", out value17))
			{
				BenchmarkMaxFps = Math.Max(0, value17);
			}
			else if (TryReadIntArg(text, "--test-physics-ticks=", out value18))
			{
				BenchmarkPhysicsTicksPerSecond = Math.Max(1, value18);
			}
			else if (TryReadStringArg(text, "--test-screenshot=", out value19))
			{
				ScreenshotPath = value19;
			}
			else if (TryReadBoolArg(text, "--test-screenshot-after-damage-part-settled=", out value20))
			{
				WaitForDamagePartSettledBeforeScreenshot = value20;
			}
			else if (TryReadStringArg(text, "--test-scene=", out value21))
			{
				ExactScenePath = value21;
			}
			else if (TryReadStringArg(text, "--test-animation-clip=", out value22))
			{
				ExactAnimationClip = value22;
			}
			else if (TryReadDoubleArg(text, "--test-print-interval=", out value23))
			{
				PrintIntervalSeconds = (float)Math.Max(0.05, value23);
			}
			else if (TryReadBoolArg(text, "--test-different-zombies=", out value24))
			{
				UseDifferentZombieScenePool = value24;
			}
			else if (TryReadBoolArg(text, "--test-character-scenes=", out value25))
			{
				UseZombieCharacterScenes = value25;
			}
			else if (TryReadStringArg(text, "--test-zombie-root=", out value26))
			{
				ZombieSceneRoot = value26;
			}
			else if (TryReadStringArg(text, "--test-character-prefix=", out value27))
			{
				CharacterSceneFilePrefix = (string.IsNullOrWhiteSpace(value27) ? "TowerDefenseZombie" : value27.Trim());
			}
			else if (TryReadStringArg(text, "--test-exclude-zombie-path=", out value28))
			{
				ExcludedZombieScenePathSubstring = value28;
			}
			else if (TryReadBoolArg(text, "--test-wait-resource-load=", out value29))
			{
				WaitForResourceManagerLoad = value29;
			}
			else if (TryReadBoolArg(text, "--test-profiler=", out value30))
			{
				EnableAdobeAnimateProfiler = value30;
			}
			else if (TryReadBoolArg(text, "--test-detailed-profiler=", out value31))
			{
				EnableDetailedAdobeAnimateProfiler = value31;
			}
			else if (TryReadBoolArg(text, "--test-allocation-telemetry=", out value32))
			{
				EnableAllocationTelemetry = value32;
			}
			else if (TryReadIntArg(text, "--test-profiler-dump-frames=", out value33))
			{
				ProfilerDumpIntervalFrames = Math.Max(1, value33);
			}
			else if (TryReadBoolArg(text, "--test-clear-animation-cache=", out value34))
			{
				ClearAdobeAnimateRuntimeCachesBeforeRun = value34;
			}
			else if (TryReadBoolArg(text, "--test-gpu-render-graph=", out value35))
			{
				EnableGpuRenderGraph = value35;
			}
			else if (TryReadBoolArg(text, "--test-crowd-mesh-capacity-classes=", out value36))
			{
				AdobeAnimateRenderManager.CrowdMeshCapacityClassesEnabled = value36;
			}
			else if (TryReadBoolArg(text, "--test-force-cpu-pose-fallback=", out value37))
			{
				ForceCpuPoseFallbackForTest = value37;
			}
			else if (TryReadIntArg(text, "--test-fixed-animation-frame=", out value38))
			{
				FixedAnimationFrame = value38;
			}
			else if (TryReadDoubleArg(text, "--test-fixed-animation-subframe=", out value39))
			{
				FixedAnimationSubframe = Math.Clamp(value39, 0.0, 0.999999);
			}
			else if (text.StartsWith("--test-adobe-fixed-frame=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text.Substring("--test-adobe-fixed-frame=".Length);
				if (!int.TryParse(text2, out var result) || result < 0)
				{
					_invalidAdobeFixedAnimationFrame = true;
					GD.PushError("[TestPerf] Invalid --test-adobe-fixed-frame value '" + text2 + "'. Expected a non-negative integer.");
				}
				else
				{
					_adobeFixedAnimationFrame = result;
				}
			}
			else if (TryReadBoolArg(text, "--test-crowd-filter-debug=", out value40))
			{
				PrintCrowdFilterDebugData = value40;
			}
			else if (TryReadBoolArg(text, "--test-z-interleave-fixture=", out value41))
			{
				EnableZInterleaveFixture = value41;
			}
			else if (TryReadBoolArg(text, "--test-crowd-mode-report=", out value42))
			{
				PrintCrowdModeReport = value42;
			}
			else if (TryReadDoubleArg(text, "--test-initial-anime-frame-rate=", out value43))
			{
				InitialAnimeFrameRate = Math.Max(0.0, value43);
			}
			else if (TryReadDoubleArg(text, "--test-switch-anime-frame-rate-after=", out value44))
			{
				SwitchAnimeFrameRateAfterSeconds = Math.Max(0.0, value44);
			}
			else if (TryReadDoubleArg(text, "--test-switch-anime-frame-rate-to=", out value45))
			{
				SwitchAnimeFrameRateTo = Math.Max(0.0, value45);
			}
			else if (TryReadStringArg(text, "--test-runtime-visual-mutation=", out value46))
			{
				RuntimeVisualMutation = value46;
			}
			else if (TryReadDoubleArg(text, "--test-runtime-visual-mutation-after=", out value47))
			{
				RuntimeVisualMutationAfterSeconds = Math.Max(0.0, value47);
			}
			else if (TryReadBoolArg(text, "--test-animation-switch-stress=", out value48))
			{
				EnableAnimationSwitchStress = value48;
			}
			else if (TryReadStringArg(text, "--test-animation-switch-clip-a=", out value49))
			{
				AnimationSwitchClipA = value49.Trim();
			}
			else if (TryReadStringArg(text, "--test-animation-switch-clip-b=", out value50))
			{
				AnimationSwitchClipB = value50.Trim();
			}
			else if (TryReadDoubleArg(text, "--test-animation-switch-interval=", out value51))
			{
				AnimationSwitchIntervalSeconds = Math.Max(0.01, value51);
			}
			else if (TryReadDoubleArg(text, "--test-animation-switch-blend=", out value52))
			{
				AnimationSwitchBlendSeconds = Math.Max(0.0, value52);
			}
			else if (TryReadDoubleArg(text, "--test-animation-switch-start=", out value53))
			{
				AnimationSwitchStartAfterSeconds = Math.Max(0.0, value53);
			}
			else if (TryReadBoolArg(text, "--test-bowling-impact-tween-stress=", out value54))
			{
				EnableBowlingImpactTweenStress = value54;
			}
			else if (TryReadBoolArg(text, "--test-bowling-impact-ensure-shield=", out value55))
			{
				BowlingImpactEnsureShield = value55;
			}
			else if (TryReadBoolArg(text, "--test-bowling-impact-production-event=", out value56))
			{
				BowlingImpactUseProductionEvent = value56;
			}
			else if (TryReadBoolArg(text, "--test-bowling-impact-create-tweens=", out value57))
			{
				BowlingImpactUseProductionEvent = value57;
			}
			else if (TryReadIntArg(text, "--test-bowling-impact-tween-limit=", out value58))
			{
				BowlingImpactProcessedTweenLimit = Math.Max(-1, value58);
			}
			else if (TryReadIntArg(text, "--test-bowling-impact-targets=", out value59))
			{
				BowlingImpactTargetsPerBurst = Math.Max(0, value59);
			}
			else if (TryReadDoubleArg(text, "--test-bowling-impact-damage=", out value60))
			{
				BowlingImpactDamage = Math.Max(0.0, value60);
			}
			else if (TryReadDoubleArg(text, "--test-bowling-impact-interval=", out value61))
			{
				BowlingImpactBurstIntervalSeconds = Math.Max(0.01, value61);
			}
			else if (TryReadDoubleArg(text, "--test-bowling-impact-window=", out value62))
			{
				BowlingImpactActiveWindowSeconds = Math.Max(0.05, value62);
			}
			else if (TryReadDoubleArg(text, "--test-bowling-impact-start=", out value63))
			{
				BowlingImpactStartAfterSeconds = Math.Max(0.0, value63);
			}
			else if (TryReadBoolArg(text, "--test-component-alive-scheduling=", out value64))
			{
				RunComponentAliveSchedulingRegression = value64;
			}
			else if (TryReadBoolArg(text, "--test-state-physics-batch-rate=", out value65))
			{
				RunStatePhysicsBatchRateRegression = value65;
			}
			else if (TryReadBoolArg(text, "--test-animation-cadence=", out value66))
			{
				RunAnimationCadenceRegression = value66;
			}
			else if (TryReadBoolArg(text, "--test-external-visuals=", out value67))
			{
				EnableExternalVisualStress = value67;
			}
			else if (TryReadBoolArg(text, "--test-external-visual-ice-only=", out value68))
			{
				ExternalVisualIceOnly = value68;
			}
			else if (TryReadDoubleArg(text, "--test-external-visual-ratio=", out value69))
			{
				ExternalVisualRatio = Math.Clamp(value69, 0.0, 1.0);
			}
			else if (TryReadBoolArg(text, "--test-combat-state-stress=", out value70))
			{
				EnableCombatStateStress = value70;
			}
			else if (TryReadBoolArg(text, "--test-preserve-combat-characters=", out value71))
			{
				PreserveCombatCharactersInStress = value71;
			}
			else if (TryReadBoolArg(text, "--test-combat-map-night=", out value72))
			{
				CombatStressIsNight = value72;
			}
			else if (TryReadBoolArg(text, "--test-show-health=", out value73))
			{
				ShowCharacterHealthInStress = value73;
			}
			else if (TryReadBoolArg(text, "--test-single-enemy-target=", out value74))
			{
				EnableSingleEnemyTargetFixture = value74;
			}
			else if (TryReadBoolArg(text, "--test-sleep-cycle-stress=", out value75))
			{
				EnableSleepCycleStress = value75;
			}
			else if (TryReadDoubleArg(text, "--test-sleep-cycle-start=", out value76))
			{
				SleepCycleStartAfterSeconds = Math.Max(0.0, value76);
			}
			else if (TryReadBoolArg(text, "--test-many-enemy-idle=", out value77))
			{
				EnableManyEnemyIdleFixture = value77;
			}
			else if (TryReadIntArg(text, "--test-many-enemy-idle-target-count=", out value78))
			{
				ManyEnemyIdleTargetCount = Math.Max(1, value78);
			}
			else if (TryReadBoolArg(text, "--test-many-enemy-idle-outside-range=", out value79))
			{
				ManyEnemyIdleTargetsOutsideAttackRange = value79;
			}
			else if (TryReadStringArg(text, "--test-enemy-scene=", out value80))
			{
				SingleEnemyTargetScenePath = value80.Trim();
			}
			else if (TryReadFloatArg(text, "--test-enemy-scale=", out value81))
			{
				SingleEnemyTargetScale = Math.Max(0.1f, value81);
			}
			else if (TryReadStringArg(text, "--test-adobe-render-backend=", out value82))
			{
				if (string.Equals(value82, "gpu", StringComparison.OrdinalIgnoreCase))
				{
					_testRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
					continue;
				}
				if (string.Equals(value82, "cpu", StringComparison.OrdinalIgnoreCase))
				{
					_testRenderBackend = AdobeAnimateRenderBackend.CpuPose;
					continue;
				}
				_invalidTestRenderBackend = true;
				GD.PushError("[TestPerf] Invalid --test-adobe-render-backend value '" + value82 + "'. Expected gpu or cpu.");
			}
		}
	}

	private string GetTestRenderBackendLabel()
	{
		if (_testRenderBackend != AdobeAnimateRenderBackend.CpuPose)
		{
			return "gpu";
		}
		return "cpu";
	}

	private void ConfigureCombatStateStress()
	{
		if (EnableCombatStateStress && !_combatStressConfigured)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (!GodotObject.IsInstanceValid(instance))
			{
				GD.PushError("[TestPerf] Combat-state stress requires the TowerDefenseManager autoload.");
				return;
			}
			_previousControl = instance.currentControl;
			_previousGridBegin = instance.gridBeginPos;
			_previousGridSize = instance.gridSize;
			_previousGridNum = instance.gridNum;
			_previousBackZombie = instance.backZombie;
			_combatStressControl = new CharacterStressControlStub
			{
				Name = "CharacterStressControl",
				isGameRunning = true,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(_combatStressControl, forceReadableName: false, InternalMode.Disabled);
			_combatStressControl.characterNode = this;
			_combatStressMapControl = new TowerDefenseMapControl
			{
				ProcessMode = ProcessModeEnum.Disabled,
				mapIceCap = new Node2D()
			};
			AddChild(_combatStressMapControl.mapIceCap, forceReadableName: false, InternalMode.Disabled);
			_combatStressMapFeature = new TowerDefenseBattleFeatureMap
			{
				control = _combatStressControl,
				config = new TowerDefenseMapConfig
				{
					isNight = CombatStressIsNight
				},
				mapControl = _combatStressMapControl
			};
			_combatStressMapFeature.iceCapList.Resize(_combatStressMapFeature.config.gridNum.Y + 1);
			_combatStressMapControl.mapFeature = _combatStressMapFeature;
			_combatStressMapFeature.PlantGridInit();
			_combatStressControl.featureDictionary[new StringName("Map")] = _combatStressMapFeature;
			instance.currentControl = _combatStressControl;
			instance.gridBeginPos = SpawnOrigin;
			instance.gridNum = new Vector2I(9, 5);
			instance.gridSize = new Vector2(Math.Max(1f, SpawnArea.X / (float)instance.gridNum.X), Math.Max(1f, SpawnArea.Y / (float)instance.gridNum.Y));
			instance.backZombie = false;
			_combatStressConfigured = true;
			GD.Print($"[TestPerf] Combat-state stress enabled gridBegin={instance.gridBeginPos} gridSize={instance.gridSize} gridNum={instance.gridNum} isNight={CombatStressIsNight}.");
		}
	}

	private void RestoreCombatStateStress()
	{
		if (_combatStressConfigured)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.currentControl = _previousControl;
				instance.gridBeginPos = _previousGridBegin;
				instance.gridSize = _previousGridSize;
				instance.gridNum = _previousGridNum;
				instance.backZombie = _previousBackZombie;
			}
			if (GodotObject.IsInstanceValid(_combatStressControl) && !_combatStressControl.IsQueuedForDeletion())
			{
				_combatStressControl.QueueFree();
			}
			_combatStressControl = null;
			_combatStressMapFeature = null;
			if (GodotObject.IsInstanceValid(_combatStressMapControl?.mapIceCap) && !_combatStressMapControl.mapIceCap.IsQueuedForDeletion())
			{
				_combatStressMapControl.mapIceCap.QueueFree();
			}
			if (GodotObject.IsInstanceValid(_combatStressMapControl))
			{
				_combatStressMapControl.Free();
			}
			_combatStressMapControl = null;
			_previousControl = null;
			_combatStressConfigured = false;
		}
	}

	private static bool TryReadIntArg(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return int.TryParse(arg.Substring(prefix.Length), out value);
		}
		return false;
	}

	private static bool TryReadFloatArg(string arg, string prefix, out float value)
	{
		value = 0f;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return float.TryParse(arg.Substring(prefix.Length), out value);
		}
		return false;
	}

	private static bool TryReadVector2Arg(string arg, string prefix, out Vector2 value)
	{
		value = Vector2.Zero;
		if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = arg.Substring(prefix.Length).Split(new char[4] { 'x', 'X', ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 2)
		{
			return false;
		}
		if (!float.TryParse(array[0], out var result) || !float.TryParse(array[1], out var result2))
		{
			return false;
		}
		value = new Vector2(result, result2);
		return true;
	}

	private static bool TryReadDoubleArg(string arg, string prefix, out double value)
	{
		value = 0.0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return double.TryParse(arg.Substring(prefix.Length), out value);
		}
		return false;
	}

	private static bool TryReadBoolArg(string arg, string prefix, out bool value)
	{
		value = false;
		if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text = arg.Substring(prefix.Length);
		if (bool.TryParse(text, out value))
		{
			return true;
		}
		if (text == "1")
		{
			value = true;
			return true;
		}
		if (text == "0")
		{
			value = false;
			return true;
		}
		return false;
	}

	private static bool TryReadStringArg(string arg, string prefix, out string value)
	{
		value = "";
		if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		value = arg.Substring(prefix.Length);
		return true;
	}
}
