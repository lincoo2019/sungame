using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

public partial class SurvivalProgressPerformanceRuntimeTest : Node
{
	private readonly record struct FileSnapshot(string Hash, long Length, DateTime LastWriteTimeUtc);

	private readonly record struct NodeSchedulingSnapshot(Dictionary NodeTypeCounts, Dictionary ProcessingNodeTypeCounts, Dictionary PhysicsProcessingNodeTypeCounts, Dictionary InternalProcessingNodeTypeCounts, Dictionary InternalPhysicsProcessingNodeTypeCounts, int Nodes, int ProcessingNodes, int PhysicsProcessingNodes, int InternalProcessingNodes, int InternalPhysicsProcessingNodes);

	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private const string DefaultLevelPath = "res://Asset/Config/Level/TowerDefense/Survival/Classic/Survival_Level1_3.tres";

	private const string ProbeUser = "SurvivalProgressPerfProbe";

	private const string ResultPrefix = "SURVIVAL_PROGRESS_PERF_RESULT ";

	private const double DefaultWarmupSeconds = 5.0;

	private const double DefaultSampleSeconds = 12.0;

	private const double ResourceLoadTimeoutSeconds = 180.0;

	private const double ProgressEntryTimeoutSeconds = 180.0;

	private const double FixtureDurableHitpoints = 1E+30;

	private const string FixtureModeAttack = "attack";

	private const string FixtureModeIdleDetect = "idle-detect";

	private const string FixtureModePlantTarget = "plant-target";

	private const string FixtureModeBattle = "battle";

	private const string FixtureTargetPlantPacket = "PlantPeaShooterSingle";

	private const int DefaultFixtureAttackDivisor = 10;

	private readonly List<TowerDefenseZombie> _fixtureZombies = new List<TowerDefenseZombie>();

	private readonly List<string> _fixtureZombiePacketNames = new List<string>();

	private readonly List<bool> _fixtureZombieAttackRoles = new List<bool>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2> _fixtureAttackAnchors = new System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2I> _fixtureAttackGrids = new System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2I>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2> _fixtureWalkAnchors = new System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2I> _fixtureWalkGrids = new System.Collections.Generic.Dictionary<TowerDefenseZombie, Vector2I>();

	private readonly System.Collections.Generic.Dictionary<string, int> _fixturePacketCounts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

	private readonly HashSet<AttackComponent> _fixtureEngagedAttackComponents = new HashSet<AttackComponent>();

	private readonly HashSet<FireComponent> _fixtureEngagedFireComponents = new HashSet<FireComponent>();

	private readonly HashSet<TowerDefenseZombie> _fixtureAttackStateSources = new HashSet<TowerDefenseZombie>();

	private int _fixtureAttackComponentCount;

	private int _fixtureFireComponentCount;

	private int _fixtureAttackCandidatesReady;

	private int _fixtureReplacementSpawns;

	private long _fixtureAttackEvents;

	private long _fixtureFireVolleys;

	private long _fixtureAttackStateEntries;

	private long _fixturePlantBodyHurtEvents;

	private long _fixturePlantArmorHurtEvents;

	private long _plantTargetZombieBodyHurtEvents;

	private long _plantTargetZombieArmorHurtEvents;

	private readonly HashSet<TowerDefenseZombie> _plantTargetZombieHitTargets = new HashSet<TowerDefenseZombie>();

	private readonly List<TowerDefensePlant> _fixtureTargetPlants = new List<TowerDefensePlant>();

	private int _clearedBattlefieldCharacters;

	private int _clearedBattlefieldProjectiles;

	private int _clearedBattlefieldEffects;

	private int _clearedBattlefieldOtherObjects;

	private bool _fixtureTallTargetBlocker;

	private readonly List<TowerDefensePlant> _plantFixturePlants = new List<TowerDefensePlant>();

	private readonly HashSet<AttackComponent> _plantFixtureEngagedAttackComponents = new HashSet<AttackComponent>();

	private readonly HashSet<FireComponent> _plantFixtureEngagedFireComponents = new HashSet<FireComponent>();

	private string _plantFixturePacket = "";

	private bool _plantFixtureForceAwake;

	private int _plantFixtureRequestedCells;

	private int _plantFixtureRequestedCount;

	private int _plantFixtureSpawnPerFrame;

	private int _plantFixtureCreated;

	private int _plantFixtureRemovedRestoredPlants;

	private int _plantFixtureAttackComponentCount;

	private int _plantFixtureFireComponentCount;

	private long _plantFixtureAttackEvents;

	private long _plantFixtureFireVolleys;

	private readonly List<string> _productionWavePackets = new List<string>();

	private readonly HashSet<string> _productionWavePacketSet = new HashSet<string>(StringComparer.Ordinal);

	private int _productionWaveLoadMultiplier;

	private int _productionWaveOriginalPoint;

	private int _productionWaveScaledPoint;

	private int _productionWaveStartWave;

	private int _productionWaveRemovedRestoredZombies;

	private ulong _productionWaveRandomSeed;

	private int _productionWaveMinimumZombies;

	private bool _productionWaveKeepBodiesDurable;

	private bool _productionWavePreserveRestoredBattlefield;

	private bool _productionWaveResetCompletedRound;

	private int _productionWaveNextSpamCount = 1;

	private bool _productionWaveNextSpamWhilePaused;

	private int _productionWaveNextSpamFrameInterval;

	private int _productionWaveNextSpamIssuedCount;

	private Task _productionWaveNextSpamTask = Task.CompletedTask;

	private int _productionWavePauseCycleFrames;

	private int _productionWavePauseDurationFrames;

	private int _productionWavePauseTransitions;

	private readonly Godot.Collections.Dictionary<int, int> _productionWaveBeginCounts = new Godot.Collections.Dictionary<int, int>();

	private int _productionWaveBeginEvents;

	private int _productionWaveDuplicateBeginEvents;

	private int _productionWaveFinalEvents;

	private double _productionWaveMassDeathAfterSeconds = -1.0;

	private int _productionWaveMassDeathRequestedCount;

	private int _productionWaveMassDeathIssuedCount;

	private bool _productionWaveMassDeathIssued;

	private double _productionWavePopulationWaitSeconds;

	private int _productionWavePopulationObservationFrames;

	private int _productionWavePopulationGrowthFrames;

	private int _productionWaveMaxGrowthPerFrame;

	private double _productionWaveDurabilityRefreshSeconds;

	private readonly HashSet<TowerDefenseZombie> _productionWaveDurableZombies = new HashSet<TowerDefenseZombie>();

	private bool _visibleWindow;

	private Label _visibleStatusLabel;

	private string _levelPath = "res://Asset/Config/Level/TowerDefense/Survival/Classic/Survival_Level1_3.tres";

	private string _levelSaveKey = "Survival_Level1_3";

	public override async void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		_visibleWindow = ReadBoolArgument("--visible-window=", fallback: false);
		if (_visibleWindow)
		{
			InitializeVisibleStatus();
		}
		SetVisibleStatus("Loading production battle resources...");
		OS.LowProcessorUsageMode = false;
		GetTree().Paused = false;
		Engine.MaxFps = 0;
		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		string sourceSavePath = ReadStringArgument("--save-path=");
		string text = ReadStringArgument("--level-path=");
		if (!string.IsNullOrWhiteSpace(text))
		{
			_levelPath = text;
		}
		_levelSaveKey = Path.GetFileNameWithoutExtension(_levelPath);
		if (string.IsNullOrWhiteSpace(_levelSaveKey))
		{
			throw new ArgumentException("The production level path has no save key: " + _levelPath);
		}
		double warmupSeconds = ReadDoubleArgument("--warmup-seconds=", 5.0);
		double sampleSeconds = ReadDoubleArgument("--sample-seconds=", 12.0);
		string screenshotPath = ReadStringArgument("--screenshot-path=");
		string frameScreenshotDirectory = ReadStringArgument("--frame-screenshot-directory=");
		int frameScreenshotInterval = Math.Max(1, ReadIntArgument("--frame-screenshot-interval=", 300));
		if (!string.IsNullOrWhiteSpace(frameScreenshotDirectory))
		{
			frameScreenshotDirectory = Path.GetFullPath(frameScreenshotDirectory);
			Directory.CreateDirectory(frameScreenshotDirectory);
		}
		double timeScale = Math.Clamp(ReadDoubleArgument("--time-scale=", 1.0), 0.1, 20.0);
		List<string> fixturePackets = ReadCsvArgument("--zombie-packets=");
		int fixtureZombieCount = ReadIntArgument("--zombie-count=", 0);
		int num = ReadIntArgument("--zombie-attack-count=", -1);
		int fixtureSpawnPerFrame = Math.Clamp(ReadIntArgument("--zombie-spawn-per-frame=", 0), 0, 1000);
		string fixtureMode = ReadStringArgument("--zombie-mode=").Trim().ToLowerInvariant();
		bool useZombieFixture = fixturePackets.Count > 0 || fixtureZombieCount > 0;
		string plantFixturePacket = ReadStringArgument("--plant-packet=").Trim();
		bool usePlantFixture = !string.IsNullOrWhiteSpace(plantFixturePacket);
		int plantFixtureCount = Math.Max(0, ReadIntArgument("--plant-count=", 0));
		int plantSpawnPerFrame = Math.Clamp(ReadIntArgument("--plant-spawn-per-frame=", 0), 0, 1000);
		bool plantForceAwake = ReadBoolArgument("--plant-force-awake=", fallback: false);
		bool plantAllowNoTarget = ReadBoolArgument("--plant-allow-no-target=", fallback: false);
		List<string> productionWavePackets = ReadCsvArgument("--wave-zombie-packets=");
		int productionWaveLoadMultiplier = ReadIntArgument("--wave-load-multiplier=", 1);
		ulong productionWaveRandomSeed = ReadULongArgument("--wave-random-seed=", 20260726uL);
		int productionWaveMinimumZombies = ReadIntArgument("--wave-minimum-zombies=", 0);
		bool productionWaveKeepBodiesDurable = ReadBoolArgument("--wave-keep-bodies-durable=", fallback: false);
		bool productionWavePreserveRestoredBattlefield = ReadBoolArgument("--wave-preserve-restored-battlefield=", fallback: false);
		int productionWaveNextSpamCount = Math.Max(1, ReadIntArgument("--wave-next-spam-count=", 1));
		bool productionWaveNextSpamWhilePaused = ReadBoolArgument("--wave-next-spam-while-paused=", fallback: false);
		int productionWaveNextSpamFrameInterval = Math.Max(0, ReadIntArgument("--wave-next-spam-frame-interval=", 0));
		int num2 = Math.Max(0, ReadIntArgument("--wave-pause-cycle-frames=", 0));
		int num3 = Math.Max(0, ReadIntArgument("--wave-pause-duration-frames=", 0));
		_productionWaveMassDeathAfterSeconds = ReadDoubleArgument("--wave-mass-death-after-seconds=", -1.0);
		_productionWaveMassDeathRequestedCount = Math.Max(0, ReadIntArgument("--wave-mass-death-count=", 0));
		if (num2 > 0 && num3 >= num2)
		{
			throw new ArgumentException("Wave pause duration must be shorter than its pause cycle.");
		}
		_productionWavePauseCycleFrames = num2;
		_productionWavePauseDurationFrames = num3;
		bool useProductionWaveFixture = productionWavePackets.Count > 0 || productionWaveLoadMultiplier > 1;
		if (productionWavePreserveRestoredBattlefield && !useProductionWaveFixture)
		{
			throw new ArgumentException("Preserving the restored battlefield requires a production-wave fixture.");
		}
		if (useZombieFixture && useProductionWaveFixture)
		{
			throw new ArgumentException("Direct zombie fixtures and production-wave fixtures cannot be combined.");
		}
		bool flag = usePlantFixture && useZombieFixture;
		string text2;
		if (flag)
		{
			text2 = fixtureMode;
			bool flag2 = ((text2 == "plant-target" || text2 == "battle") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			throw new ArgumentException("植物与直接生成的僵尸需要使用 plant-target 或 battle 模式。");
		}
		flag = usePlantFixture && !useProductionWaveFixture;
		if (flag)
		{
			bool flag2 = useZombieFixture;
			if (flag2)
			{
				text2 = fixtureMode;
				bool flag3 = ((text2 == "plant-target" || text2 == "battle") ? true : false);
				flag2 = flag3;
			}
			flag = !flag2;
		}
		if (flag && !plantAllowNoTarget)
		{
			throw new ArgumentException("植物测试需要正式波次或显式僵尸目标。");
		}
		text2 = fixtureMode;
		flag = ((text2 == "plant-target" || text2 == "battle") ? true : false);
		if (flag && (!usePlantFixture || !useZombieFixture))
		{
			throw new ArgumentException("plant-target 和 battle 模式需要同时配置植物与僵尸。");
		}
		if (string.IsNullOrWhiteSpace(fixtureMode))
		{
			fixtureMode = "attack";
		}
		if (num < -1)
		{
			throw new ArgumentException("Zombie attack count cannot be less than -1.");
		}
		int fixtureAttackCount = ((fixtureMode == "battle") ? fixtureZombieCount : ((fixtureMode == "attack") ? ((num < 0) ? GetDefaultFixtureAttackCount(fixtureZombieCount) : num) : 0));
		int fixtureAttackBudget = ((fixtureMode == "battle") ? fixtureZombieCount : GetDefaultFixtureAttackCount(fixtureZombieCount));
		if (fixtureAttackCount > fixtureZombieCount)
		{
			throw new ArgumentException($"Zombie attack count {fixtureAttackCount} exceeds zombie count {fixtureZombieCount}.");
		}
		if (fixtureAttackCount > fixtureAttackBudget)
		{
			throw new ArgumentException($"Zombie attack count {fixtureAttackCount} exceeds the real-battle 10% budget {fixtureAttackBudget} for {fixtureZombieCount} zombies.");
		}
		string profileMode = ReadStringArgument("--profile-mode=").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(profileMode))
		{
			profileMode = "detailed";
		}
		switch (profileMode)
		{
		case "off":
		case "summary":
		case "detailed":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			throw new ArgumentException("Unsupported profile mode '" + profileMode + "'.");
		}
		double originalTimeScale = Engine.TimeScale;
		int originalMaxPhysicsStepsPerFrame = Engine.MaxPhysicsStepsPerFrame;
		Engine.MaxPhysicsStepsPerFrame = 1;
		if (fixtureMode == "battle")
		{
			Engine.MaxPhysicsStepsPerFrame = originalMaxPhysicsStepsPerFrame;
			GD.Seed(2026090701uL);
		}
		FileSnapshot sourceBefore = default(FileSnapshot);
		bool sourceSnapshotAvailable = false;
		try
		{
			if (string.IsNullOrWhiteSpace(sourceSavePath))
			{
				throw new ArgumentException("A source progress path is required.");
			}
			sourceSavePath = Path.GetFullPath(sourceSavePath);
			if (!File.Exists(sourceSavePath))
			{
				throw new FileNotFoundException("The source progress file does not exist.", sourceSavePath);
			}
			sourceBefore = CaptureFileSnapshot(sourceSavePath);
			sourceSnapshotAvailable = true;
			RequireAutoloads();
			GameSaveManager.Instance.EnsureLoaded();
			GameSaveManager.Instance.SetUserCurrent("SurvivalProgressPerfProbe");
			GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
			TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = ResourceLoader.Load<TowerDefenseLevelSaveConfigCSharp>(CopyProgressIntoSandbox(sourceSavePath), "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(towerDefenseLevelSaveConfigCSharp))
			{
				throw new InvalidOperationException("The isolated progress copy could not be loaded.");
			}
			int expectedCharacters = towerDefenseLevelSaveConfigCSharp.characterList?.Count ?? 0;
			if (expectedCharacters <= 0)
			{
				throw new InvalidOperationException("The isolated progress copy has no character records.");
			}
			SetVisibleStatus("Loading production registries and shared animation resources...");
			await EnsureResourcesLoadedAsync();
			await PrepareEmptyModEnvironmentAsync();
			await PresentVisibleStatusAsync("Opening the production Survival battle...");
			TowerDefenseControlNew battle = await StartBattleAsync();
			await PresentVisibleStatusAsync("Restoring the production Survival progress...");
			await WaitForProgressEntryAsync(battle, expectedCharacters);
			CloseRestorePauseDialogs(GetTree().Root);
			GetTree().Paused = false;
			TowerDefenseManager.Instance.pausePacket = false;
			TowerDefenseManager.Instance.pauseZombie = false;
			if ((usePlantFixture || useZombieFixture || useProductionWaveFixture) && !productionWavePreserveRestoredBattlefield)
			{
				SetVisibleStatus("Clearing restored battlefield objects...");
				await ClearBattlefieldForFixtureAsync(battle);
			}
			string text3;
			bool flag2;
			if (usePlantFixture)
			{
				TowerDefenseControlNew battle2 = battle;
				text2 = plantFixturePacket;
				flag = plantForceAwake;
				int requestedCount = plantFixtureCount;
				int requestedSpawnPerFrame = plantSpawnPerFrame;
				flag2 = useZombieFixture;
				if (flag2)
				{
					text3 = fixtureMode;
					bool flag3 = ((text3 == "plant-target" || text3 == "battle") ? true : false);
					flag2 = flag3;
				}
				await PreparePlantFixtureAsync(battle2, text2, flag, requestedCount, requestedSpawnPerFrame, flag2);
				AttachPlantFixtureEvidenceHandlers();
			}
			Task productionWavePopulationTask = null;
			if (useZombieFixture)
			{
				SetVisibleStatus($"Preparing {fixtureZombieCount} real battle zombies...");
				if (fixturePackets.Count == 0 || fixtureZombieCount <= 0)
				{
					throw new ArgumentException("Zombie fixture requires both --zombie-packets and a positive --zombie-count.");
				}
				if (fixtureMode != "attack" && fixtureMode != "idle-detect" && fixtureMode != "plant-target" && fixtureMode != "battle")
				{
					throw new ArgumentException("Unsupported zombie fixture mode '" + fixtureMode + "'.");
				}
				await PrepareZombieFixtureAsync(battle, fixturePackets, fixtureZombieCount, fixtureAttackCount, fixtureMode, fixtureSpawnPerFrame);
				AttachFixtureEvidenceHandlers();
				text2 = fixtureMode;
				if ((text2 == "attack" || text2 == "battle") ? true : false)
				{
					await StabilizeAttackFixtureAtNormalSpeedAsync();
				}
			}
			else if (useProductionWaveFixture)
			{
				if (productionWavePackets.Count == 0 || productionWaveLoadMultiplier <= 0)
				{
					throw new ArgumentException("Production-wave fixture requires packet names and a positive load multiplier.");
				}
				await PrepareProductionSurvivalWaveAsync(battle, productionWavePackets, productionWaveLoadMultiplier, productionWaveRandomSeed, productionWaveMinimumZombies, productionWaveKeepBodiesDurable, productionWavePreserveRestoredBattlefield, productionWaveNextSpamCount, productionWaveNextSpamWhilePaused, productionWaveNextSpamFrameInterval);
				if (_productionWaveMinimumZombies > 0)
				{
					productionWavePopulationTask = WaitForProductionWavePopulationAsync(battle.GetFeature("Wave") as TowerDefenseBattleFeatureWave);
				}
			}
			Engine.TimeScale = timeScale;
			if (useZombieFixture && fixtureMode == "idle-detect")
			{
				await StabilizeIdleFixtureAsync();
			}
			else if (useZombieFixture && fixtureMode == "plant-target")
			{
				await StabilizePlantTargetFixtureAsync();
			}
			SetVisibleStatus($"Warmup: {CountValidFixtureZombies()} real battle zombies...");
			await WaitSecondsAsync(Math.Max(0.0, warmupSeconds));
			if (productionWavePopulationTask != null)
			{
				await productionWavePopulationTask;
			}
			CloseRestorePauseDialogs(GetTree().Root);
			GetTree().Paused = false;
			if (fixtureMode == "battle")
			{
				Engine.MaxFps = 0;
				Engine.PhysicsTicksPerSecond = 60;
				DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
				RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), enable: true);
			}
			TowerDefensePerfProfiler.Reset();
			TowerDefensePerfProfiler.Enabled = profileMode != "off";
			TowerDefensePerfProfiler.DetailedHotPathMetrics = profileMode == "detailed";
			TowerDefensePerfProfiler.DumpIntervalFrames = 60;
			TowerDefensePerfProfiler.MaxMetricsPerDump = 160;
			TowerDefenseAllocationTelemetry.Reset();
			TowerDefenseAllocationTelemetry.Enabled = true;
			if (fixtureMode == "battle" && profileMode == "off")
			{
				TowerDefenseAllocationTelemetry.Enabled = false;
			}
			SetVisibleStatus($"Sampling: {CountValidFixtureZombies()} real battle zombies...");
			GD.Print($"SURVIVAL_PROGRESS_PERF_MARKER phase=sampling characters={CountValidFixtureZombies()} profile={profileMode}");
			long graphBuildsBefore = AdobeAnimateRenderManager.GpuRenderGraphBuildCount;
			long graphRebuildsBefore = AdobeAnimateRenderManager.GpuRenderGraphRebuildCount;
			long graphInitialBuildsBefore = AdobeAnimateRenderManager.GpuRenderGraphInitialBuildCount;
			long graphOwnerExitInvalidationsBefore = AdobeAnimateRenderManager.GpuRenderGraphOwnerExitInvalidationCount;
			long graphOwnerDefinitionInvalidationsBefore = AdobeAnimateRenderManager.GpuRenderGraphOwnerDefinitionInvalidationCount;
			long graphManagedSlotInvalidationsBefore = AdobeAnimateRenderManager.GpuRenderGraphManagedSlotInvalidationCount;
			long graphExternalVisualInvalidationsBefore = AdobeAnimateRenderManager.GpuRenderGraphExternalVisualInvalidationCount;
			long threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			long totalAllocatedBefore = GC.GetTotalAllocatedBytes();
			int gen0Before = GC.CollectionCount(0);
			int gen1Before = GC.CollectionCount(1);
			int gen2Before = GC.CollectionCount(2);
			long fixtureAttackEventsBefore = _fixtureAttackEvents;
			long fixtureFireVolleysBefore = _fixtureFireVolleys;
			long fixtureAttackStateEntriesBefore = _fixtureAttackStateEntries;
			long fixturePlantBodyHurtEventsBefore = _fixturePlantBodyHurtEvents;
			long fixturePlantArmorHurtEventsBefore = _fixturePlantArmorHurtEvents;
			long plantTargetZombieBodyHurtEventsBefore = _plantTargetZombieBodyHurtEvents;
			long plantTargetZombieArmorHurtEventsBefore = _plantTargetZombieArmorHurtEvents;
			_plantTargetZombieHitTargets.Clear();
			long plantFixtureAttackEventsBefore = _plantFixtureAttackEvents;
			long plantFixtureFireVolleysBefore = _plantFixtureFireVolleys;
			int plantFixtureValidAtSampleStart = (usePlantFixture ? CountValidPlantFixturePlants() : 0);
			int plantFixtureAwakeAtSampleStart = (usePlantFixture ? CountAwakePlantFixturePlants() : 0);
			int plantFixtureInTreeAtSampleStart = (usePlantFixture ? CountValidPlantFixturePlants() : 0);
			int fixtureZombiesInTreeAtSampleStart = (useZombieFixture ? CountFixtureZombiesInsideTree() : 0);
			int productionWaveZombiesAtSampleStart = (useProductionWaveFixture ? CountProductionWaveZombies() : 0);
			int productionWavePeakZombies = productionWaveZombiesAtSampleStart;
			List<double> frameSeconds = new List<double>(Math.Max(256, (int)(sampleSeconds * 240.0)));
			List<double> processMilliseconds = new List<double>(frameSeconds.Capacity);
			List<double> physicsMilliseconds = new List<double>(frameSeconds.Capacity);
			int frameScreenshotsSaved = 0;
			ulong physicsFramesBefore = Engine.GetPhysicsFrames();
			double gameTimeBefore = TowerDefenseManager.Instance.runGameTime;
			Stopwatch sampleTimer = Stopwatch.StartNew();
			while (sampleTimer.Elapsed.TotalSeconds < Math.Max(0.25, sampleSeconds))
			{
				long frameStart = Stopwatch.GetTimestamp();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				frameSeconds.Add(Stopwatch.GetElapsedTime(frameStart).TotalSeconds);
				processMilliseconds.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0);
				physicsMilliseconds.Add(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0);
				if (useProductionWaveFixture)
				{
					productionWavePeakZombies = Math.Max(productionWavePeakZombies, CountProductionWaveZombies());
					if (!_productionWaveMassDeathIssued && _productionWaveMassDeathRequestedCount > 0 && sampleTimer.Elapsed.TotalSeconds >= _productionWaveMassDeathAfterSeconds)
					{
						IssueProductionWaveMassDeath();
					}
					if (_productionWavePauseCycleFrames > 0)
					{
						bool flag4 = frameSeconds.Count % _productionWavePauseCycleFrames < _productionWavePauseDurationFrames;
						if (GetTree().Paused != flag4)
						{
							GetTree().Paused = flag4;
							_productionWavePauseTransitions++;
						}
					}
				}
				if (!string.IsNullOrWhiteSpace(frameScreenshotDirectory) && frameSeconds.Count % frameScreenshotInterval == 0)
				{
					AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
					using Image image = GetViewport().GetTexture().GetImage();
					string path = Path.Combine(frameScreenshotDirectory, $"frame_{frameSeconds.Count:D5}_roots_{aggregateRenderStats.CrowdRoots}_state_{aggregateRenderStats.CrowdStateTexels}_capacity_{aggregateRenderStats.CrowdStateCapacityTexels}.png");
					if (image.SavePng(path) == Error.Ok)
					{
						frameScreenshotsSaved++;
					}
				}
				TowerDefensePerfProfiler.DumpIfNeeded();
			}
			sampleTimer.Stop();
			ulong num4 = Engine.GetPhysicsFrames() - physicsFramesBefore;
			double num5 = TowerDefenseManager.Instance.runGameTime - gameTimeBefore;
			TowerDefensePerfProfiler.DumpIfNeeded();
			GetTree().Paused = false;
			long val = GC.GetAllocatedBytesForCurrentThread() - threadAllocatedBefore;
			long val2 = GC.GetTotalAllocatedBytes() - totalAllocatedBefore;
			CollectCharacterCounts(out var characters, out var plants, out var zombies, out var items, out var otherCharacters);
			TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = battle.GetFeature("Wave") as TowerDefenseBattleFeatureWave;
			int num6 = (GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave?.survivalRunner) ? towerDefenseBattleFeatureWave.survivalRunner.roundNum : (-1));
			int num7 = (GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave) ? towerDefenseBattleFeatureWave.currentWave : (-1));
			int otherZombies;
			Dictionary otherPacketCounts;
			Dictionary dictionary = BuildProductionWavePacketCountDictionary(out otherZombies, out otherPacketCounts);
			int num8 = 0;
			foreach (Variant value2 in dictionary.Values)
			{
				num8 += value2.AsInt32();
			}
			FileSnapshot fileSnapshot = CaptureFileSnapshot(sourceSavePath);
			bool sourceUnchanged = sourceBefore == fileSnapshot;
			double[] array = frameSeconds.ToArray();
			double[] array2 = processMilliseconds.ToArray();
			double[] array3 = physicsMilliseconds.ToArray();
			double totalSeconds = sampleTimer.Elapsed.TotalSeconds;
			Viewport viewport = GetViewport();
			double num9 = 0.0;
			double num10 = 0.0;
			if (GodotObject.IsInstanceValid(viewport))
			{
				Rid viewportRid = viewport.GetViewportRid();
				num9 = RenderingServer.GetFrameSetupTimeCpu() + RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewportRid);
				num10 = RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid);
			}
			NodeSchedulingSnapshot nodeSchedulingSnapshot = CaptureNodeSchedulingSnapshot(GetTree().Root);
			AdobeAnimateCrowdAggregateStats aggregateRenderStats2 = AdobeAnimateRenderManager.GetAggregateRenderStats();
			bool flag5 = false;
			string text4 = "";
			if (!string.IsNullOrWhiteSpace(screenshotPath) && GodotObject.IsInstanceValid(viewport))
			{
				try
				{
					screenshotPath = Path.GetFullPath(screenshotPath);
					Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
					Error error = viewport.GetTexture().GetImage().SavePng(screenshotPath);
					flag5 = error == Error.Ok;
					if (!flag5)
					{
						text4 = error.ToString();
					}
				}
				catch (Exception ex)
				{
					text4 = ex.Message;
				}
			}
			Dictionary dictionary2 = CaptureZombieVisibilityDiagnostics(viewport);
			Dictionary dictionary3 = new Dictionary();
			Variant key = "progress_loaded";
			dictionary3[key] = true;
			Variant key2 = "source_unchanged";
			dictionary3[key2] = sourceUnchanged;
			Variant key3 = "source_sha256";
			dictionary3[key3] = fileSnapshot.Hash;
			Variant key4 = "source_bytes";
			dictionary3[key4] = fileSnapshot.Length;
			Variant key5 = "renderer";
			dictionary3[key5] = RenderingServer.GetCurrentRenderingMethod();
			Variant key6 = "time_scale";
			dictionary3[key6] = timeScale;
			Variant key7 = "physics_ticks_per_second";
			dictionary3[key7] = Engine.PhysicsTicksPerSecond;
			Variant key8 = "physics_steps";
			dictionary3[key8] = num4;
			Variant key9 = "observed_physics_hz";
			dictionary3[key9] = (double)num4 / Math.Max(0.001, totalSeconds);
			Variant key10 = "maximum_fps";
			dictionary3[key10] = Engine.MaxFps;
			Variant key11 = "simulated_seconds";
			dictionary3[key11] = num5;
			Variant key12 = "process_working_set_bytes";
			dictionary3[key12] = Process.GetCurrentProcess().WorkingSet64;
			Variant key13 = "battle_population";
			dictionary3[key13] = ((fixtureMode == "battle") ? CaptureBattlePopulation() : new Dictionary());
			Variant key14 = "max_physics_steps_per_frame";
			dictionary3[key14] = Engine.MaxPhysicsStepsPerFrame;
			Variant key15 = "profile_mode";
			dictionary3[key15] = profileMode;
			Variant key16 = "shadow_registered_sources";
			dictionary3[key16] = TowerDefenseShadowMultiMeshRenderer.ActiveRegisteredSourceCount;
			Variant key17 = "focus_pause_dialogs";
			dictionary3[key17] = CountPauseDialogs(GetTree().Root);
			Variant key18 = "tree_paused";
			dictionary3[key18] = GetTree().Paused;
			Variant key19 = "survival_round";
			dictionary3[key19] = num6;
			Variant key20 = "current_wave";
			dictionary3[key20] = num7;
			Variant key21 = "expected_characters";
			dictionary3[key21] = expectedCharacters;
			Variant key22 = "fixture_enabled";
			dictionary3[key22] = useZombieFixture;
			Variant key23 = "fixture_mode";
			dictionary3[key23] = (useZombieFixture ? fixtureMode : "");
			Variant key24 = "fixture_requested_zombies";
			dictionary3[key24] = (useZombieFixture ? fixtureZombieCount : 0);
			Variant key25 = "fixture_requested_attack_zombies";
			dictionary3[key25] = (useZombieFixture ? fixtureAttackCount : 0);
			Variant key26 = "fixture_attack_budget";
			dictionary3[key26] = (useZombieFixture ? fixtureAttackBudget : 0);
			Variant key27 = "fixture_assigned_attack_zombies";
			dictionary3[key27] = CountAssignedFixtureAttackZombies();
			Variant key28 = "fixture_assigned_walk_zombies";
			dictionary3[key28] = CountAssignedFixtureWalkZombies();
			Variant key29 = "fixture_spawn_per_frame";
			dictionary3[key29] = (useZombieFixture ? ((fixtureSpawnPerFrame > 0) ? fixtureSpawnPerFrame : (_visibleWindow ? 2 : 12)) : 0);
			Variant key30 = "fixture_spawned_zombies";
			dictionary3[key30] = CountValidFixtureZombies();
			Variant key31 = "fixture_zombies_in_tree_sample_start";
			dictionary3[key31] = fixtureZombiesInTreeAtSampleStart;
			Variant key32 = "fixture_zombies_in_tree_sample_end";
			dictionary3[key32] = CountFixtureZombiesInsideTree();
			Variant key33 = "fixture_replacement_spawns";
			dictionary3[key33] = _fixtureReplacementSpawns;
			Variant variant = "fixture_target_plant_packet";
			Dictionary dictionary4 = dictionary3;
			Variant key34 = variant;
			if (useZombieFixture)
			{
				text3 = fixtureMode;
				flag = ((text3 == "plant-target" || text3 == "battle") ? true : false);
				text2 = (flag ? _plantFixturePacket : "PlantPeaShooterSingle");
			}
			else
			{
				text2 = "";
			}
			dictionary4[key34] = text2;
			Variant variant2 = "fixture_target_plants";
			Dictionary dictionary5 = dictionary3;
			Variant key35 = variant2;
			text3 = fixtureMode;
			flag2 = ((text3 == "plant-target" || text3 == "battle") ? true : false);
			dictionary5[key35] = (flag2 ? CountValidPlantFixturePlants() : CountValidFixtureTargetPlants());
			Variant key36 = "fixture_tall_target_blocker";
			dictionary3[key36] = _fixtureTallTargetBlocker;
			Variant key37 = "cleared_battlefield_characters";
			dictionary3[key37] = _clearedBattlefieldCharacters;
			Variant key38 = "cleared_battlefield_projectiles";
			dictionary3[key38] = _clearedBattlefieldProjectiles;
			Variant key39 = "cleared_battlefield_effects";
			dictionary3[key39] = _clearedBattlefieldEffects;
			Variant key40 = "cleared_battlefield_other_objects";
			dictionary3[key40] = _clearedBattlefieldOtherObjects;
			Variant key41 = "fixture_packets";
			dictionary3[key41] = BuildFixturePacketArray();
			Variant key42 = "fixture_packet_counts";
			dictionary3[key42] = BuildFixturePacketCountDictionary();
			Variant key43 = "fixture_attack_components";
			dictionary3[key43] = _fixtureAttackComponentCount;
			Variant key44 = "fixture_attack_candidates_ready";
			dictionary3[key44] = _fixtureAttackCandidatesReady;
			Variant key45 = "fixture_attack_components_engaged";
			dictionary3[key45] = _fixtureEngagedAttackComponents.Count;
			Variant key46 = "fixture_fire_components";
			dictionary3[key46] = _fixtureFireComponentCount;
			Variant key47 = "fixture_fire_components_engaged";
			dictionary3[key47] = _fixtureEngagedFireComponents.Count;
			Variant key48 = "fixture_attack_state_sources";
			dictionary3[key48] = _fixtureAttackStateSources.Count;
			Variant key49 = "fixture_attack_events_sample";
			dictionary3[key49] = Math.Max(0L, _fixtureAttackEvents - fixtureAttackEventsBefore);
			Variant key50 = "fixture_fire_volleys_sample";
			dictionary3[key50] = Math.Max(0L, _fixtureFireVolleys - fixtureFireVolleysBefore);
			Variant key51 = "fixture_attack_state_entries_sample";
			dictionary3[key51] = Math.Max(0L, _fixtureAttackStateEntries - fixtureAttackStateEntriesBefore);
			Variant key52 = "fixture_plant_body_hurt_events_sample";
			dictionary3[key52] = Math.Max(0L, _fixturePlantBodyHurtEvents - fixturePlantBodyHurtEventsBefore);
			Variant key53 = "fixture_plant_armor_hurt_events_sample";
			dictionary3[key53] = Math.Max(0L, _fixturePlantArmorHurtEvents - fixturePlantArmorHurtEventsBefore);
			Variant key54 = "plant_target_zombie_body_hurt_events_sample";
			dictionary3[key54] = Math.Max(0L, _plantTargetZombieBodyHurtEvents - plantTargetZombieBodyHurtEventsBefore);
			Variant key55 = "plant_target_zombie_armor_hurt_events_sample";
			dictionary3[key55] = Math.Max(0L, _plantTargetZombieArmorHurtEvents - plantTargetZombieArmorHurtEventsBefore);
			Variant key56 = "plant_target_zombie_unique_hits_sample";
			dictionary3[key56] = _plantTargetZombieHitTargets.Count;
			Variant key57 = "fixture_state_counts";
			dictionary3[key57] = BuildFixtureStateCountDictionary();
			Variant key58 = "plant_fixture_enabled";
			dictionary3[key58] = usePlantFixture;
			Variant key59 = "plant_fixture_packet";
			dictionary3[key59] = (usePlantFixture ? _plantFixturePacket : "");
			Variant key60 = "plant_fixture_force_awake";
			dictionary3[key60] = usePlantFixture && _plantFixtureForceAwake;
			Variant key61 = "plant_fixture_allow_no_target";
			dictionary3[key61] = usePlantFixture && plantAllowNoTarget;
			Variant key62 = "plant_fixture_requested_cells";
			dictionary3[key62] = _plantFixtureRequestedCells;
			Variant key63 = "plant_fixture_requested_count";
			dictionary3[key63] = _plantFixtureRequestedCount;
			Variant key64 = "plant_fixture_spawn_per_frame";
			dictionary3[key64] = _plantFixtureSpawnPerFrame;
			Variant key65 = "plant_fixture_created";
			dictionary3[key65] = _plantFixtureCreated;
			Variant key66 = "plant_fixture_removed_restored_plants";
			dictionary3[key66] = _plantFixtureRemovedRestoredPlants;
			Variant key67 = "plant_fixture_valid_sample_start";
			dictionary3[key67] = plantFixtureValidAtSampleStart;
			Variant key68 = "plant_fixture_awake_sample_start";
			dictionary3[key68] = plantFixtureAwakeAtSampleStart;
			Variant key69 = "plant_fixture_in_tree_sample_start";
			dictionary3[key69] = plantFixtureInTreeAtSampleStart;
			Variant key70 = "plant_fixture_valid_sample_end";
			dictionary3[key70] = CountValidPlantFixturePlants();
			Variant key71 = "plant_fixture_in_tree_sample_end";
			dictionary3[key71] = CountValidPlantFixturePlants();
			Variant key72 = "plant_fixture_attack_components";
			dictionary3[key72] = _plantFixtureAttackComponentCount;
			Variant key73 = "plant_fixture_attack_components_engaged";
			dictionary3[key73] = _plantFixtureEngagedAttackComponents.Count;
			Variant key74 = "plant_fixture_fire_components";
			dictionary3[key74] = _plantFixtureFireComponentCount;
			Variant key75 = "plant_fixture_fire_components_engaged";
			dictionary3[key75] = _plantFixtureEngagedFireComponents.Count;
			Variant key76 = "plant_fixture_attack_events_sample";
			dictionary3[key76] = Math.Max(0L, _plantFixtureAttackEvents - plantFixtureAttackEventsBefore);
			Variant key77 = "plant_fixture_fire_volleys_sample";
			dictionary3[key77] = Math.Max(0L, _plantFixtureFireVolleys - plantFixtureFireVolleysBefore);
			Variant key78 = "plant_fixture_state_counts";
			dictionary3[key78] = BuildPlantFixtureStateCountDictionary();
			Variant key79 = "wave_fixture_enabled";
			dictionary3[key79] = useProductionWaveFixture;
			Variant key80 = "wave_fixture_packets";
			dictionary3[key80] = BuildProductionWavePacketArray();
			Variant key81 = "wave_load_multiplier";
			dictionary3[key81] = ((!useProductionWaveFixture) ? 1 : _productionWaveLoadMultiplier);
			Variant key82 = "wave_original_point";
			dictionary3[key82] = _productionWaveOriginalPoint;
			Variant key83 = "wave_scaled_point";
			dictionary3[key83] = _productionWaveScaledPoint;
			Variant key84 = "wave_start_wave";
			dictionary3[key84] = _productionWaveStartWave;
			Variant key85 = "wave_random_seed";
			dictionary3[key85] = _productionWaveRandomSeed;
			Variant key86 = "wave_minimum_zombies";
			dictionary3[key86] = _productionWaveMinimumZombies;
			Variant key87 = "wave_keep_bodies_durable";
			dictionary3[key87] = _productionWaveKeepBodiesDurable;
			Variant key88 = "wave_preserve_restored_battlefield";
			dictionary3[key88] = _productionWavePreserveRestoredBattlefield;
			Variant key89 = "wave_reset_completed_round";
			dictionary3[key89] = _productionWaveResetCompletedRound;
			Variant key90 = "wave_next_spam_count";
			dictionary3[key90] = _productionWaveNextSpamCount;
			Variant key91 = "wave_next_spam_while_paused";
			dictionary3[key91] = _productionWaveNextSpamWhilePaused;
			Variant key92 = "wave_next_spam_frame_interval";
			dictionary3[key92] = _productionWaveNextSpamFrameInterval;
			Variant key93 = "wave_next_spam_issued_count";
			dictionary3[key93] = _productionWaveNextSpamIssuedCount;
			Variant key94 = "wave_pause_cycle_frames";
			dictionary3[key94] = _productionWavePauseCycleFrames;
			Variant key95 = "wave_pause_duration_frames";
			dictionary3[key95] = _productionWavePauseDurationFrames;
			Variant key96 = "wave_pause_transitions";
			dictionary3[key96] = _productionWavePauseTransitions;
			Variant key97 = "wave_begin_events";
			dictionary3[key97] = _productionWaveBeginEvents;
			Variant key98 = "wave_duplicate_begin_events";
			dictionary3[key98] = _productionWaveDuplicateBeginEvents;
			Variant key99 = "wave_begin_counts";
			dictionary3[key99] = _productionWaveBeginCounts;
			Variant key100 = "wave_final_events";
			dictionary3[key100] = _productionWaveFinalEvents;
			Variant key101 = "wave_mass_death_after_seconds";
			dictionary3[key101] = _productionWaveMassDeathAfterSeconds;
			Variant key102 = "wave_mass_death_requested_count";
			dictionary3[key102] = _productionWaveMassDeathRequestedCount;
			Variant key103 = "wave_mass_death_issued_count";
			dictionary3[key103] = _productionWaveMassDeathIssuedCount;
			Variant key104 = "wave_durable_bodies";
			dictionary3[key104] = _productionWaveDurableZombies.Count;
			Variant key105 = "wave_population_wait_seconds";
			dictionary3[key105] = _productionWavePopulationWaitSeconds;
			Variant key106 = "wave_population_observation_frames";
			dictionary3[key106] = _productionWavePopulationObservationFrames;
			Variant key107 = "wave_population_growth_frames";
			dictionary3[key107] = _productionWavePopulationGrowthFrames;
			Variant key108 = "wave_max_growth_per_frame";
			dictionary3[key108] = _productionWaveMaxGrowthPerFrame;
			Variant key109 = "wave_removed_restored_zombies";
			dictionary3[key109] = _productionWaveRemovedRestoredZombies;
			Variant key110 = "wave_zombies_sample_start";
			dictionary3[key110] = productionWaveZombiesAtSampleStart;
			Variant key111 = "wave_zombies_peak";
			dictionary3[key111] = productionWavePeakZombies;
			Variant key112 = "wave_zombies_sample_end";
			dictionary3[key112] = num8;
			Variant key113 = "wave_other_zombies_sample_end";
			dictionary3[key113] = otherZombies;
			Variant key114 = "wave_other_packet_counts";
			dictionary3[key114] = otherPacketCounts;
			Variant key115 = "wave_packet_counts";
			dictionary3[key115] = dictionary;
			Variant key116 = "wave_state_counts";
			dictionary3[key116] = BuildProductionWaveStateCountDictionary();
			Variant key117 = "wave_spawn_pipeline_active";
			dictionary3[key117] = GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave) && towerDefenseBattleFeatureWave.IsSpawnPipelineActive;
			Variant key118 = "wave_spawn_over";
			dictionary3[key118] = GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave) && towerDefenseBattleFeatureWave.spawnOver;
			Variant key119 = "characters";
			dictionary3[key119] = characters;
			Variant key120 = "plants";
			dictionary3[key120] = plants;
			Variant key121 = "zombies";
			dictionary3[key121] = zombies;
			Variant key122 = "items";
			dictionary3[key122] = items;
			Variant key123 = "other_characters";
			dictionary3[key123] = otherCharacters;
			Variant key124 = "bullet_field_active";
			dictionary3[key124] = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.ActiveCount : 0);
			Variant key125 = "bullet_collision_candidate_cache_builds_last_update";
			dictionary3[key125] = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.CollisionCandidateCacheBuildCountForTest : 0);
			Variant key126 = "bullet_collision_candidate_cache_hits_last_update";
			dictionary3[key126] = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.CollisionCandidateCacheHitCountForTest : 0);
			Variant key127 = "bullet_collision_overlap_cache_builds_last_update";
			dictionary3[key127] = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.CollisionOverlapCacheBuildCountForTest : 0);
			Variant key128 = "bullet_collision_overlap_cache_hits_last_update";
			dictionary3[key128] = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.CollisionOverlapCacheHitCountForTest : 0);
			Variant key129 = "frames";
			dictionary3[key129] = array.Length;
			Variant key130 = "measured_seconds";
			dictionary3[key130] = totalSeconds;
			Variant key131 = "average_fps";
			dictionary3[key131] = ((totalSeconds > 0.0) ? ((double)array.Length / totalSeconds) : 0.0);
			Variant key132 = "frame_p50_ms";
			dictionary3[key132] = BenchmarkStatistics.Percentile(array, array.Length, 50.0) * 1000.0;
			Variant key133 = "frame_p95_ms";
			dictionary3[key133] = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
			Variant key134 = "frame_p99_ms";
			dictionary3[key134] = BenchmarkStatistics.Percentile(array, array.Length, 99.0) * 1000.0;
			Variant key135 = "frame_max_ms";
			dictionary3[key135] = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
			Variant key136 = "one_percent_low_fps";
			dictionary3[key136] = BenchmarkStatistics.WorstFractionFps(array, array.Length, 0.01);
			Variant key137 = "process_p50_ms";
			dictionary3[key137] = BenchmarkStatistics.Percentile(array2, array2.Length, 50.0);
			Variant key138 = "process_p95_ms";
			dictionary3[key138] = BenchmarkStatistics.Percentile(array2, array2.Length, 95.0);
			Variant key139 = "process_p99_ms";
			dictionary3[key139] = BenchmarkStatistics.Percentile(array2, array2.Length, 99.0);
			Variant key140 = "physics_p50_ms";
			dictionary3[key140] = BenchmarkStatistics.Percentile(array3, array3.Length, 50.0);
			Variant key141 = "physics_p95_ms";
			dictionary3[key141] = BenchmarkStatistics.Percentile(array3, array3.Length, 95.0);
			Variant key142 = "physics_p99_ms";
			dictionary3[key142] = BenchmarkStatistics.Percentile(array3, array3.Length, 99.0);
			Variant key143 = "render_cpu_ms";
			dictionary3[key143] = num9;
			Variant key144 = "render_gpu_ms";
			dictionary3[key144] = num10;
			Variant key145 = "object_count";
			dictionary3[key145] = Performance.GetMonitor(Performance.Monitor.ObjectCount);
			Variant key146 = "node_count";
			dictionary3[key146] = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
			Variant key147 = "draw_calls";
			dictionary3[key147] = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
			Variant key148 = "render_objects";
			dictionary3[key148] = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
			Variant key149 = "render_primitives";
			dictionary3[key149] = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
			Variant key150 = "adobe_crowd_roots";
			dictionary3[key150] = aggregateRenderStats2.CrowdRoots;
			Variant key151 = "adobe_crowd_state_texels";
			dictionary3[key151] = aggregateRenderStats2.CrowdStateTexels;
			Variant key152 = "adobe_crowd_state_capacity_texels";
			dictionary3[key152] = aggregateRenderStats2.CrowdStateCapacityTexels;
			Variant key153 = "adobe_crowd_state_upload_bytes";
			dictionary3[key153] = aggregateRenderStats2.StateTextureUploadedBytes;
			Variant key154 = "adobe_state_texture_uploads";
			dictionary3[key154] = aggregateRenderStats2.StateTextureUploads;
			Variant key155 = "adobe_state_texture_updated_layers";
			dictionary3[key155] = aggregateRenderStats2.StateTextureUpdatedLayers;
			Variant key156 = "adobe_graph_atlas_allocations";
			dictionary3[key156] = AdobeAnimateRenderManager.GpuRenderGraphAtlasAllocationCount;
			Variant key157 = "adobe_graph_atlas_pages";
			dictionary3[key157] = AdobeAnimateRenderManager.GpuRenderGraphAtlasPageCount;
			Variant key158 = "adobe_graph_atlas_layer_capacity";
			dictionary3[key158] = AdobeAnimateRenderManager.GpuRenderGraphAtlasLayerCapacity;
			Variant key159 = "screenshot_saved";
			dictionary3[key159] = flag5;
			Variant key160 = "screenshot_path";
			dictionary3[key160] = (flag5 ? screenshotPath : "");
			Variant key161 = "screenshot_error";
			dictionary3[key161] = text4;
			Variant key162 = "frame_screenshot_directory";
			dictionary3[key162] = frameScreenshotDirectory;
			Variant key163 = "frame_screenshot_interval";
			dictionary3[key163] = frameScreenshotInterval;
			Variant key164 = "frame_screenshots_saved";
			dictionary3[key164] = frameScreenshotsSaved;
			Variant key165 = "zombie_visibility";
			dictionary3[key165] = dictionary2;
			Variant key166 = "snapshot_node_count";
			dictionary3[key166] = nodeSchedulingSnapshot.Nodes;
			Variant key167 = "processing_nodes";
			dictionary3[key167] = nodeSchedulingSnapshot.ProcessingNodes;
			Variant key168 = "physics_processing_nodes";
			dictionary3[key168] = nodeSchedulingSnapshot.PhysicsProcessingNodes;
			Variant key169 = "internal_processing_nodes";
			dictionary3[key169] = nodeSchedulingSnapshot.InternalProcessingNodes;
			Variant key170 = "internal_physics_processing_nodes";
			dictionary3[key170] = nodeSchedulingSnapshot.InternalPhysicsProcessingNodes;
			Variant key171 = "node_type_counts";
			dictionary3[key171] = nodeSchedulingSnapshot.NodeTypeCounts;
			Variant key172 = "processing_node_type_counts";
			dictionary3[key172] = nodeSchedulingSnapshot.ProcessingNodeTypeCounts;
			Variant key173 = "physics_processing_node_type_counts";
			dictionary3[key173] = nodeSchedulingSnapshot.PhysicsProcessingNodeTypeCounts;
			Variant key174 = "internal_processing_node_type_counts";
			dictionary3[key174] = nodeSchedulingSnapshot.InternalProcessingNodeTypeCounts;
			Variant key175 = "internal_physics_processing_node_type_counts";
			dictionary3[key175] = nodeSchedulingSnapshot.InternalPhysicsProcessingNodeTypeCounts;
			Variant key176 = "gpu_graph_builds";
			dictionary3[key176] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphBuildCount - graphBuildsBefore);
			Variant key177 = "gpu_graph_initial_builds";
			dictionary3[key177] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphInitialBuildCount - graphInitialBuildsBefore);
			Variant key178 = "gpu_graph_rebuilds";
			dictionary3[key178] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphRebuildCount - graphRebuildsBefore);
			Variant key179 = "gpu_graph_owner_exit_invalidations";
			dictionary3[key179] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphOwnerExitInvalidationCount - graphOwnerExitInvalidationsBefore);
			Variant key180 = "gpu_graph_owner_definition_invalidations";
			dictionary3[key180] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphOwnerDefinitionInvalidationCount - graphOwnerDefinitionInvalidationsBefore);
			Variant key181 = "gpu_graph_managed_slot_invalidations";
			dictionary3[key181] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphManagedSlotInvalidationCount - graphManagedSlotInvalidationsBefore);
			Variant key182 = "gpu_graph_external_visual_invalidations";
			dictionary3[key182] = Math.Max(0L, AdobeAnimateRenderManager.GpuRenderGraphExternalVisualInvalidationCount - graphExternalVisualInvalidationsBefore);
			Variant key183 = "thread_allocated_bytes";
			dictionary3[key183] = Math.Max(0L, val);
			Variant key184 = "total_allocated_bytes";
			dictionary3[key184] = Math.Max(0L, val2);
			Variant key185 = "allocation_metrics";
			dictionary3[key185] = BuildAllocationTelemetrySnapshot();
			Variant key186 = "gen0";
			dictionary3[key186] = GC.CollectionCount(0) - gen0Before;
			Variant key187 = "gen1";
			dictionary3[key187] = GC.CollectionCount(1) - gen1Before;
			Variant key188 = "gen2";
			dictionary3[key188] = GC.CollectionCount(2) - gen2Before;
			Dictionary dictionary6 = dictionary3;
			GD.Print("SURVIVAL_PROGRESS_PERF_RESULT " + Json.Stringify(dictionary6));
			SetVisibleStatus("Sampling complete. Closing in 2 seconds...");
			if (_visibleWindow)
			{
				await WaitSecondsAsync(2.0);
			}
			GetTree().Quit((!sourceUnchanged) ? 3 : 0);
		}
		catch (Exception ex2)
		{
			bool value = !sourceSnapshotAvailable || CaptureFileSnapshot(sourceSavePath) == sourceBefore;
			GD.PrintErr($"SURVIVAL_PROGRESS_PERF_FAILURE sourceUnchanged={value} error={ex2}");
			SetVisibleStatus("Test failed: " + ex2.Message);
			if (_visibleWindow)
			{
				await WaitSecondsAsync(3.0);
			}
			GetTree().Quit(2);
		}
		finally
		{
			Engine.TimeScale = originalTimeScale;
			Engine.MaxPhysicsStepsPerFrame = originalMaxPhysicsStepsPerFrame;
			TowerDefensePerfProfiler.DetailedHotPathMetrics = false;
			TowerDefensePerfProfiler.Enabled = false;
			TowerDefensePerfProfiler.Reset();
			TowerDefenseAllocationTelemetry.Enabled = false;
			TowerDefenseAllocationTelemetry.Reset();
		}
	}

	private static Dictionary BuildAllocationTelemetrySnapshot()
	{
		Dictionary dictionary = new Dictionary();
		for (int i = 0; i < 77; i++)
		{
			TowerDefenseAllocationMetric metric = (TowerDefenseAllocationMetric)i;
			dictionary[metric.ToString()] = new Dictionary
			{
				["bytes"] = TowerDefenseAllocationTelemetry.GetAllocatedBytes(metric),
				["samples"] = TowerDefenseAllocationTelemetry.GetSampleCount(metric)
			};
		}
		return dictionary;
	}

	private static int GetDefaultFixtureAttackCount(int zombieCount)
	{
		if (zombieCount > 0)
		{
			return Math.Max(1, zombieCount / 10);
		}
		return 0;
	}

	private static NodeSchedulingSnapshot CaptureNodeSchedulingSnapshot(Node root)
	{
		System.Collections.Generic.Dictionary<string, int> counts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> counts2 = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> counts3 = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> counts4 = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> counts5 = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		Stack<Node> stack = new Stack<Node>();
		stack.Push(root);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		while (stack.Count > 0)
		{
			Node node = stack.Pop();
			if (GodotObject.IsInstanceValid(node))
			{
				num++;
				string name = node.GetType().Name;
				IncrementCount(counts, name);
				if (node.IsProcessing())
				{
					num2++;
					IncrementCount(counts2, name);
				}
				if (node.IsPhysicsProcessing())
				{
					num3++;
					IncrementCount(counts3, name);
				}
				if (node.IsProcessingInternal())
				{
					num4++;
					IncrementCount(counts4, name);
				}
				if (node.IsPhysicsProcessingInternal())
				{
					num5++;
					IncrementCount(counts5, name);
				}
				for (int num6 = node.GetChildCount() - 1; num6 >= 0; num6--)
				{
					stack.Push(node.GetChild(num6));
				}
			}
		}
		return new NodeSchedulingSnapshot(BuildCountDictionary(counts), BuildCountDictionary(counts2), BuildCountDictionary(counts3), BuildCountDictionary(counts4), BuildCountDictionary(counts5), num, num2, num3, num4, num5);
	}

	private static void IncrementCount(System.Collections.Generic.Dictionary<string, int> counts, string key)
	{
		counts[key] = counts.GetValueOrDefault(key) + 1;
	}

	private static Dictionary BuildCountDictionary(System.Collections.Generic.Dictionary<string, int> counts)
	{
		Dictionary dictionary = new Dictionary();
		foreach (var (text2, num2) in counts)
		{
			dictionary[text2] = num2;
		}
		return dictionary;
	}

	private async Task PreparePlantFixtureAsync(TowerDefenseControlNew battle, string packetName, bool forceAwake, int requestedCount, int requestedSpawnPerFrame, bool reserveTargetColumn)
	{
		TowerDefenseBattleFeatureMap map = battle.GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (!GodotObject.IsInstanceValid(map?.config))
		{
			throw new InvalidOperationException("The restored battle has no usable Map feature for the plant fixture.");
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("PreparePlantFixtureAsync");
		TowerDefensePacketConfig authoredPacket = TowerDefenseManager.GetPacketConfigReadOnly(packetName);
		if (GodotObject.IsInstanceValid(authoredPacket))
		{
			TowerDefenseCharacterConfig characterConfig = authoredPacket.characterConfig;
			if (characterConfig is TowerDefensePlantConfig plantConfig)
			{
				TowerDefenseManager instance = TowerDefenseManager.Instance;
				List<TowerDefenseCharacter> cleanCharactersList = instance.characterRegistry.GetCleanCharactersList();
				for (int i = 0; i < cleanCharactersList.Count; i++)
				{
					if (!(cleanCharactersList[i] is TowerDefensePlant towerDefensePlant))
					{
						continue;
					}
					for (int j = 1; j <= map.config.gridNum.X; j++)
					{
						for (int k = 1; k <= map.config.gridNum.Y; k++)
						{
							TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(j, k));
							if (GodotObject.IsInstanceValid(mapCell))
							{
								mapCell.RemoveCharacter(towerDefensePlant);
							}
						}
					}
					instance.CharacterUnregister(towerDefensePlant);
					towerDefensePlant.RemoveFromGroup("Character");
					towerDefensePlant.RemoveFromGroup("Plant");
					towerDefensePlant.ProcessMode = ProcessModeEnum.Disabled;
					towerDefensePlant.QueueFree();
					_plantFixtureRemovedRestoredPlants++;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				List<Vector2I> validGridPositions = new List<Vector2I>();
				for (int l = 1; l <= map.config.gridNum.Y; l++)
				{
					if (l >= map.lineUse.Count || !map.lineUse[l])
					{
						continue;
					}
					for (int m = 1; m <= map.config.gridNum.X; m++)
					{
						if (!reserveTargetColumn || m != map.config.gridNum.X)
						{
							Vector2I vector2I = new Vector2I(m, l);
							if (CanPlantFixtureOccupyGrid(map, plantConfig, vector2I))
							{
								validGridPositions.Add(vector2I);
							}
						}
					}
				}
				if (validGridPositions.Count == 0)
				{
					throw new InvalidOperationException("Plant fixture packet '" + packetName + "' has no valid authored grid positions.");
				}
				_plantFixturePacket = packetName;
				_plantFixtureRequestedCells = validGridPositions.Count;
				_plantFixtureRequestedCount = ((requestedCount > 0) ? requestedCount : validGridPositions.Count);
				_plantFixtureSpawnPerFrame = ((requestedSpawnPerFrame > 0) ? requestedSpawnPerFrame : _plantFixtureRequestedCount);
				int num = 0;
				for (int plantIndex = 0; plantIndex < _plantFixtureRequestedCount; plantIndex++)
				{
					Vector2I vector2I2 = validGridPositions[plantIndex % validGridPositions.Count];
					TowerDefensePacketConfig towerDefensePacketConfig = authoredPacket.CreateSpawnRuntimeCopy();
					if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
					{
						throw new InvalidOperationException("Failed to create a runtime packet shell for '" + packetName + "'.");
					}
					if (!(towerDefensePacketConfig.Plant(vector2I2, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) is TowerDefensePlant item))
					{
						throw new InvalidOperationException($"Plant fixture packet '{packetName}' did not create plant {plantIndex + 1}/{_plantFixtureRequestedCount} at {vector2I2}.");
					}
					_plantFixturePlants.Add(item);
					_plantFixtureCreated++;
					num++;
					if (num >= _plantFixtureSpawnPerFrame && plantIndex + 1 < _plantFixtureRequestedCount)
					{
						SetVisibleStatus($"Spawning real battle plants: {plantIndex + 1}/{_plantFixtureRequestedCount}");
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						num = 0;
					}
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				_plantFixtureForceAwake = forceAwake;
				for (int n = 0; n < _plantFixturePlants.Count; n++)
				{
					TowerDefensePlant towerDefensePlant2 = _plantFixturePlants[n];
					if (GodotObject.IsInstanceValid(towerDefensePlant2) && !towerDefensePlant2.IsQueuedForDeletion())
					{
						MakeCharacterAndArmorDurable(towerDefensePlant2);
						if (forceAwake)
						{
							towerDefensePlant2.WakeUp();
						}
					}
				}
				if (forceAwake)
				{
					for (int plantIndex = 0; plantIndex < 120; plantIndex++)
					{
						if (CountAwakePlantFixturePlants() >= _plantFixturePlants.Count)
						{
							break;
						}
						await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					}
					int num2 = CountAwakePlantFixturePlants();
					if (num2 != _plantFixturePlants.Count)
					{
						throw new InvalidOperationException($"Plant fixture '{packetName}' did not wake every plant: {num2}/{_plantFixturePlants.Count}.");
					}
				}
				int num3 = CountValidPlantFixturePlants();
				if (_plantFixtureRequestedCount <= 0 || num3 != _plantFixtureRequestedCount)
				{
					throw new InvalidOperationException($"Plant fixture '{packetName}' did not retain every requested node: {num3}/{_plantFixtureRequestedCount}.");
				}
				GD.Print($"[SurvivalPlantFixture] packet={packetName} validCells={_plantFixtureRequestedCells} requested={_plantFixtureRequestedCount} created={_plantFixtureCreated} spawnPerFrame={_plantFixtureSpawnPerFrame} removedRestored={_plantFixtureRemovedRestoredPlants} forceAwake={forceAwake} awake={CountAwakePlantFixturePlants()}");
				return;
			}
		}
		throw new InvalidOperationException("Plant fixture packet '" + packetName + "' is not a loaded plant packet.");
	}

	private async Task ClearBattlefieldForFixtureAsync(TowerDefenseControlNew battle)
	{
		TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = battle.GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		if (GodotObject.IsInstanceValid(CommandManager.Instance))
		{
			CommandManager.Instance.debug = true;
			CommandManager.Instance.debugNoZombieSpawn = true;
			CommandManager.Instance.debugNoLose = true;
		}
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave))
		{
			towerDefenseBattleFeatureWave.isRunning = false;
			towerDefenseBattleFeatureWave.waveFinal = false;
			towerDefenseBattleFeatureWave.spawnOver = false;
			await WaitForWaveSpawnSettlementAsync(towerDefenseBattleFeatureWave);
		}
		Node2D characterMount = TowerDefenseManager.GetCharacterNode();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(characterMount))
		{
			throw new InvalidOperationException("The battle has no character mount to clear.");
		}
		Array<Node> nodesInGroup = GetTree().GetNodesInGroup("Character");
		_clearedBattlefieldCharacters = nodesInGroup.Count;
		foreach (Node item in nodesInGroup)
		{
			if (item is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				manager.CharacterUnregister(towerDefenseCharacter);
			}
		}
		_clearedBattlefieldProjectiles = GetTree().GetNodeCountInGroup("Projectile") + (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.ActiveCount : 0);
		_clearedBattlefieldEffects = GetTree().GetNodeCountInGroup("Effect");
		ProjectileUpdateManager.Instance?.Clear();
		foreach (Node child in characterMount.GetChildren())
		{
			if (GodotObject.IsInstanceValid(child) && !(child is BulletField))
			{
				if (!(child is TowerDefenseCharacter) && !(child is TowerDefenseProjectile) && !(child is TowerDefenseEffectBase) && !(child is TowerDefenseProjectileEffectBase) && !(child is TowerDefenseEffectSpriteOnceBatcher))
				{
					_clearedBattlefieldOtherObjects++;
				}
				child.ProcessMode = ProcessModeEnum.Disabled;
				child.QueueFree();
			}
		}
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = battle.GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap?.config))
		{
			for (int i = 1; i <= towerDefenseBattleFeatureMap.config.gridNum.X; i++)
			{
				for (int j = 1; j <= towerDefenseBattleFeatureMap.config.gridNum.Y; j++)
				{
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, j));
					if (GodotObject.IsInstanceValid(mapCell))
					{
						mapCell.ClearEmpty();
					}
				}
			}
		}
		for (int frame = 0; frame < 120; frame++)
		{
			if (manager.characterRegistry.ActiveCharacterCount == 0)
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		int nodeCountInGroup = GetTree().GetNodeCountInGroup("Character");
		int nodeCountInGroup2 = GetTree().GetNodeCountInGroup("Projectile");
		int nodeCountInGroup3 = GetTree().GetNodeCountInGroup("Effect");
		int num = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.ActiveCount : 0);
		int num2 = 0;
		foreach (Node child2 in characterMount.GetChildren())
		{
			if (GodotObject.IsInstanceValid(child2) && !(child2 is BulletField))
			{
				num2++;
			}
		}
		if (nodeCountInGroup != 0 || nodeCountInGroup2 != 0 || nodeCountInGroup3 != 0 || num != 0 || num2 != 0 || manager.characterRegistry.ActiveCharacterCount != 0)
		{
			throw new InvalidOperationException($"Battlefield clear did not reach an empty baseline: characterNodes={nodeCountInGroup} projectiles={nodeCountInGroup2} bulletField={num} effects={nodeCountInGroup3} mountChildren={num2} registry={manager.characterRegistry.ActiveCharacterCount}.");
		}
		if (!GodotObject.IsInstanceValid(BulletField.Instance) && !GodotObject.IsInstanceValid(BulletField.EnsureMountedOnCharacterNode()))
		{
			throw new InvalidOperationException("BulletField could not be remounted after battlefield clear.");
		}
		GD.Print($"[SurvivalBattlefieldClear] characters={_clearedBattlefieldCharacters} projectiles={_clearedBattlefieldProjectiles} effects={_clearedBattlefieldEffects} other={_clearedBattlefieldOtherObjects} baseline=empty");
	}

	private static bool CanPlantFixtureOccupyGrid(TowerDefenseBattleFeatureMap map, TowerDefensePlantConfig plantConfig, Vector2I gridPosition)
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(gridPosition)))
		{
			return false;
		}
		foreach (Vector2I item in plantConfig.extendGrid)
		{
			Vector2I gridPos = gridPosition + item;
			if (!GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(gridPos)) || gridPos.Y <= 0 || gridPos.Y >= map.lineUse.Count || !map.lineUse[gridPos.Y])
			{
				return false;
			}
		}
		return true;
	}

	private void AttachPlantFixtureEvidenceHandlers()
	{
		for (int i = 0; i < _plantFixturePlants.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = _plantFixturePlants[i];
			if (!GodotObject.IsInstanceValid(towerDefensePlant?.componentManager))
			{
				continue;
			}
			IReadOnlyList<CharacterComponentRuntime> resourceComponents = towerDefensePlant.componentManager.ResourceComponents;
			for (int j = 0; j < resourceComponents.Count; j++)
			{
				CharacterComponentRuntime characterComponentRuntime = resourceComponents[j];
				AttackComponent attack = characterComponentRuntime as AttackComponent;
				if (attack != null)
				{
					_plantFixtureAttackComponentCount++;
					attack.OnAttack += delegate
					{
						_plantFixtureAttackEvents++;
						_plantFixtureEngagedAttackComponents.Add(attack);
					};
					continue;
				}
				characterComponentRuntime = resourceComponents[j];
				FireComponent fire = characterComponentRuntime as FireComponent;
				if (fire != null)
				{
					_plantFixtureFireComponentCount++;
					fire.OnFireVolley += delegate
					{
						_plantFixtureFireVolleys++;
						_plantFixtureEngagedFireComponents.Add(fire);
					};
				}
			}
		}
		GD.Print($"[SurvivalPlantFixture] components attack={_plantFixtureAttackComponentCount} fire={_plantFixtureFireComponentCount}");
	}

	private async Task PrepareProductionSurvivalWaveAsync(TowerDefenseControlNew battle, IReadOnlyList<string> packetNames, int loadMultiplier, ulong randomSeed, int minimumZombies, bool keepBodiesDurable, bool preserveRestoredBattlefield, int nextSpamCount, bool nextSpamWhilePaused, int nextSpamFrameInterval)
	{
		if (!GodotObject.IsInstanceValid(CommandManager.Instance))
		{
			throw new InvalidOperationException("CommandManager is unavailable for the production-wave fixture.");
		}
		TowerDefenseBattleFeatureWave wave = battle.GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		if (!GodotObject.IsInstanceValid(wave) || !wave.isSurvival || !GodotObject.IsInstanceValid(wave.survivalRunner?.config))
		{
			throw new InvalidOperationException("The restored battle has no usable survival wave pipeline.");
		}
		TowerDefenseBattleFeatureMap map = battle.GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (!GodotObject.IsInstanceValid(map?.config))
		{
			throw new InvalidOperationException("The restored battle has no usable Map feature.");
		}
		CommandManager.Instance.debug = true;
		CommandManager.Instance.debugNoLose = true;
		CommandManager.Instance.debugNoZombieSpawn = true;
		wave.isRunning = false;
		await WaitForWaveSpawnSettlementAsync(wave);
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!preserveRestoredBattlefield)
		{
			List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(instance.characterRegistry.GetCleanCharactersList());
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] is TowerDefenseZombie towerDefenseZombie)
				{
					instance.CharacterUnregister(towerDefenseZombie);
					towerDefenseZombie.RemoveFromGroup("Character");
					towerDefenseZombie.RemoveFromGroup("Zombie");
					towerDefenseZombie.ProcessMode = ProcessModeEnum.Disabled;
					towerDefenseZombie.QueueFree();
					_productionWaveRemovedRestoredZombies++;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if ((await PrepareDurablePlantTargetsAsync(map, forceSpecialMovementBlocker: false)).Count == 0)
		{
			throw new InvalidOperationException("The restored level has no targetable plant for normal wave combat.");
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("PrepareProductionSurvivalWaveAsync");
		for (int j = 0; j < packetNames.Count; j++)
		{
			string text = packetNames[j];
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text);
			if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || !(packetConfigReadOnly.characterConfig is TowerDefenseZombieConfig towerDefenseZombieConfig))
			{
				throw new InvalidOperationException("Production-wave packet '" + text + "' is not a loaded zombie packet.");
			}
			if (towerDefenseZombieConfig.physique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				throw new InvalidOperationException("Production-wave packet '" + text + "' is a boss and was excluded.");
			}
			bool flag = false;
			for (int k = 1; k <= map.config.gridNum.Y; k++)
			{
				if (k < map.lineUse.Count && map.lineUse[k] && packetConfigReadOnly.CanSpawn(k))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				throw new InvalidOperationException("Production-wave packet '" + text + "' cannot spawn on this map.");
			}
			_productionWavePackets.Add(text);
			_productionWavePacketSet.Add(text);
		}
		wave.survivalRunner.currentZombiePool.Clear();
		for (int l = 0; l < packetNames.Count; l++)
		{
			wave.survivalRunner.currentZombiePool.Add(packetNames[l]);
		}
		if (GodotObject.IsInstanceValid(wave.currentDynamic))
		{
			wave.currentDynamic.startingPoints = 0;
			wave.currentDynamic.pointIncrementPerWave = 0;
			wave.currentDynamic.zombiePool.Clear();
		}
		if (wave.currentWave >= wave.config.wave.Count)
		{
			wave.Refresh();
			_productionWaveResetCompletedRound = true;
		}
		if (wave.currentWave < 0 || wave.currentWave >= wave.config.wave.Count)
		{
			throw new InvalidOperationException($"Saved wave {wave.currentWave} cannot continue through the production pipeline.");
		}
		TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = wave.config.wave[wave.currentWave];
		if (GodotObject.IsInstanceValid(towerDefenseLevelWaveConfig?.dynamic))
		{
			towerDefenseLevelWaveConfig.dynamic.points = 0;
			towerDefenseLevelWaveConfig.dynamic.zombiePool.Clear();
		}
		wave.currentSpawnPoint = 0;
		wave.config.flagZombieUse = false;
		_productionWaveLoadMultiplier = loadMultiplier;
		_productionWaveRandomSeed = randomSeed;
		_productionWaveMinimumZombies = Math.Max(0, minimumZombies);
		_productionWaveKeepBodiesDurable = keepBodiesDurable;
		_productionWavePreserveRestoredBattlefield = preserveRestoredBattlefield;
		_productionWaveNextSpamCount = Math.Max(1, nextSpamCount);
		_productionWaveNextSpamWhilePaused = nextSpamWhilePaused;
		_productionWaveNextSpamFrameInterval = Math.Max(0, nextSpamFrameInterval);
		_productionWaveOriginalPoint = Math.Max(1, wave.survivalRunner.point);
		_productionWaveScaledPoint = (int)Math.Min(536870911L, (long)_productionWaveOriginalPoint * (long)loadMultiplier);
		wave.survivalRunner.config.pointMax = Math.Max(wave.survivalRunner.config.pointMax, _productionWaveScaledPoint);
		wave.survivalRunner.point = _productionWaveScaledPoint;
		_productionWaveStartWave = wave.currentWave;
		ResourceManager.Instance.RequireFullGameplayResourcesReady("PrepareProductionSurvivalWaveAsync");
		wave.readySetPlantOver = true;
		wave.waveFinal = false;
		wave.spawnOver = false;
		wave.awaitSpawn = false;
		wave.isRunning = true;
		wave.OnWaveBegin += delegate(int waveId, bool _, bool _)
		{
			_productionWaveBeginEvents++;
			int value;
			int num = ((!_productionWaveBeginCounts.TryGetValue(waveId, out value)) ? 1 : (value + 1));
			_productionWaveBeginCounts[waveId] = num;
			if (num > 1)
			{
				_productionWaveDuplicateBeginEvents++;
			}
		};
		wave.OnFinal += delegate
		{
			_productionWaveFinalEvents++;
		};
		CommandManager.Instance.debugNoZombieSpawn = false;
		GD.Seed(_productionWaveRandomSeed);
		if (_productionWaveNextSpamWhilePaused)
		{
			GetTree().Paused = true;
		}
		if (!_productionWaveNextSpamWhilePaused && _productionWaveNextSpamFrameInterval > 0)
		{
			_productionWaveNextSpamTask = RunProductionWaveNextSpamAsync(wave);
		}
		else
		{
			for (int requestIndex = 0; requestIndex < _productionWaveNextSpamCount; requestIndex++)
			{
				wave.NextWave();
				_productionWaveNextSpamIssuedCount++;
				for (int intervalFrame = 0; intervalFrame < _productionWaveNextSpamFrameInterval; intervalFrame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
		}
		if (_productionWaveNextSpamWhilePaused)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Paused = false;
		}
		Stopwatch startTimeout = Stopwatch.StartNew();
		while (wave.currentWave <= _productionWaveStartWave && !wave.IsSpawnPipelineActive)
		{
			if (startTimeout.Elapsed.TotalSeconds >= 15.0)
			{
				throw new TimeoutException("The production survival wave did not start in time.");
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GD.Print($"[SurvivalProductionWaveFixture] packets={string.Join(",", packetNames)} loadMultiplier={loadMultiplier} point={_productionWaveOriginalPoint}->{_productionWaveScaledPoint} wave={_productionWaveStartWave + 1} randomSeed={_productionWaveRandomSeed} minimumZombies={_productionWaveMinimumZombies} durableBodies={_productionWaveKeepBodiesDurable} preserveRestored={_productionWavePreserveRestoredBattlefield} nextSpam={_productionWaveNextSpamCount} spamWhilePaused={_productionWaveNextSpamWhilePaused} spamFrameInterval={_productionWaveNextSpamFrameInterval} removedRestored={_productionWaveRemovedRestoredZombies}");
	}

	private async Task RunProductionWaveNextSpamAsync(TowerDefenseBattleFeatureWave wave)
	{
		for (int requestIndex = 0; requestIndex < _productionWaveNextSpamCount; requestIndex++)
		{
			if (!GodotObject.IsInstanceValid(wave))
			{
				break;
			}
			if (!IsInsideTree())
			{
				break;
			}
			wave.NextWave();
			_productionWaveNextSpamIssuedCount++;
			for (int intervalFrame = 0; intervalFrame < _productionWaveNextSpamFrameInterval; intervalFrame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
	}

	private async Task WaitForProductionWavePopulationAsync(TowerDefenseBattleFeatureWave wave)
	{
		if (!GodotObject.IsInstanceValid(wave))
		{
			throw new InvalidOperationException("The production wave became unavailable while waiting for its population.");
		}
		Stopwatch timeout = Stopwatch.StartNew();
		int previousZombieCount = CountProductionWaveZombies();
		int num;
		do
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			RefreshProductionWaveDurableBodies();
			num = CountProductionWaveZombies();
			_productionWavePopulationObservationFrames++;
			int num2 = Math.Max(0, num - previousZombieCount);
			if (num2 > 0)
			{
				_productionWavePopulationGrowthFrames++;
				_productionWaveMaxGrowthPerFrame = Math.Max(_productionWaveMaxGrowthPerFrame, num2);
			}
			previousZombieCount = num;
			if (num >= _productionWaveMinimumZombies)
			{
				_productionWavePopulationWaitSeconds = timeout.Elapsed.TotalSeconds;
				GD.Print($"[SurvivalProductionWaveFixture] populationReady={num}/{_productionWaveMinimumZombies} waitSeconds={_productionWavePopulationWaitSeconds:F3} observationFrames={_productionWavePopulationObservationFrames} growthFrames={_productionWavePopulationGrowthFrames} maxGrowthPerFrame={_productionWaveMaxGrowthPerFrame} pipelineActive={wave.IsSpawnPipelineActive}");
				return;
			}
			if (!wave.IsSpawnPipelineActive && _productionWaveNextSpamTask.IsCompleted)
			{
				throw new InvalidOperationException($"The normal survival wave settled with only {num}/{_productionWaveMinimumZombies} requested zombies.");
			}
		}
		while (!(timeout.Elapsed.TotalSeconds >= 180.0));
		throw new TimeoutException($"The normal survival wave did not reach {_productionWaveMinimumZombies} zombies in {180.0:F0} seconds; current={num}.");
	}

	private void RefreshProductionWaveDurableBodies()
	{
		if (!_productionWaveKeepBodiesDurable)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> activeCharacters = instance.characterRegistry.GetActiveCharacters();
		for (int i = 0; i < activeCharacters.Count; i++)
		{
			if (activeCharacters[i] is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie.instance))
			{
				string item = (GodotObject.IsInstanceValid(towerDefenseZombie.packet) ? towerDefenseZombie.packet.saveKey : "");
				if (_productionWavePacketSet.Contains(item) && _productionWaveDurableZombies.Add(towerDefenseZombie))
				{
					MakeCharacterBodyDurable(towerDefenseZombie);
				}
			}
		}
	}

	private async Task PrepareZombieFixtureAsync(TowerDefenseControlNew battle, IReadOnlyList<string> packetNames, int requestedCount, int requestedAttackCount, string fixtureMode, int requestedSpawnPerFrame)
	{
		if (!GodotObject.IsInstanceValid(CommandManager.Instance))
		{
			throw new InvalidOperationException("CommandManager is unavailable for the isolated fixture.");
		}
		TowerDefenseBattleFeatureWave wave = battle.GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		if (!GodotObject.IsInstanceValid(wave))
		{
			throw new InvalidOperationException("The restored battle has no Wave feature.");
		}
		TowerDefenseBattleFeatureMap map = battle.GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (!GodotObject.IsInstanceValid(map?.config))
		{
			throw new InvalidOperationException("The restored battle has no usable Map feature.");
		}
		CommandManager.Instance.debug = true;
		CommandManager.Instance.debugNoZombieSpawn = true;
		CommandManager.Instance.debugNoLose = true;
		wave.isRunning = false;
		wave.waveFinal = false;
		wave.spawnOver = false;
		await WaitForWaveSpawnSettlementAsync(wave);
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(manager.characterRegistry.GetCleanCharactersList());
		int removedZombies = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is TowerDefenseZombie towerDefenseZombie)
			{
				manager.CharacterUnregister(towerDefenseZombie);
				towerDefenseZombie.RemoveFromGroup("Character");
				towerDefenseZombie.RemoveFromGroup("Zombie");
				towerDefenseZombie.ProcessMode = ProcessModeEnum.Disabled;
				towerDefenseZombie.QueueFree();
				removedZombies++;
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		_fixtureTallTargetBlocker = fixtureMode == "attack" && RequiresTallFixtureTarget(packetNames);
		System.Collections.Generic.Dictionary<int, TowerDefensePlant> lineTargets = await PrepareDurablePlantTargetsAsync(map, _fixtureTallTargetBlocker);
		if (lineTargets.Count == 0)
		{
			throw new InvalidOperationException("The restored level has no targetable plant for the zombie fixture.");
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("PrepareZombieFixtureAsync");
		System.Collections.Generic.Dictionary<string, TowerDefensePacketConfig> packetConfigs = new System.Collections.Generic.Dictionary<string, TowerDefensePacketConfig>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, List<int>> validLinesByPacket = new System.Collections.Generic.Dictionary<string, List<int>>(StringComparer.Ordinal);
		for (int j = 0; j < packetNames.Count; j++)
		{
			string text = packetNames[j];
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text);
			if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || !(packetConfigReadOnly.characterConfig is TowerDefenseZombieConfig towerDefenseZombieConfig))
			{
				throw new InvalidOperationException("Fixture packet '" + text + "' is not a loaded zombie packet.");
			}
			if (towerDefenseZombieConfig.physique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				throw new InvalidOperationException("Fixture packet '" + text + "' is a boss and was excluded.");
			}
			List<int> list2 = new List<int>();
			for (int k = 1; k <= map.config.gridNum.Y; k++)
			{
				if (k < map.lineUse.Count && map.lineUse[k] && lineTargets.ContainsKey(k) && packetConfigReadOnly.CanSpawn(k))
				{
					list2.Add(k);
				}
			}
			if (list2.Count == 0)
			{
				throw new InvalidOperationException("Fixture packet '" + text + "' has no valid target lane in this save.");
			}
			packetConfigs.Add(text, packetConfigReadOnly);
			validLinesByPacket.Add(text, list2);
			_fixturePacketCounts.Add(text, 0);
		}
		Stopwatch frameBudget = Stopwatch.StartNew();
		bool useAdaptiveFrameBudget = requestedSpawnPerFrame <= 0;
		int maxSpawnPerFrame = ((!useAdaptiveFrameBudget) ? requestedSpawnPerFrame : (_visibleWindow ? 2 : 12));
		int num = 0;
		for (int zombieIndex = 0; zombieIndex < requestedCount; zombieIndex++)
		{
			int fixtureIndex = zombieIndex;
			string text2 = fixtureMode;
			bool flag = ((text2 == "attack" || text2 == "battle") ? true : false);
			SpawnFixtureZombie(fixtureIndex, flag && zombieIndex < requestedAttackCount);
			num++;
			if (num >= maxSpawnPerFrame || (useAdaptiveFrameBudget && frameBudget.Elapsed.TotalMilliseconds >= 4.0))
			{
				SetVisibleStatus($"Spawning real battle zombies: {zombieIndex + 1}/{requestedCount}");
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				num = 0;
				frameBudget.Restart();
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		for (int zombieIndex = 0; zombieIndex < 4; zombieIndex++)
		{
			if (CountValidFixtureZombies() >= requestedCount)
			{
				break;
			}
			PruneInvalidFixtureZombies();
			int num2 = requestedCount - _fixtureZombies.Count;
			int num3 = CountAssignedFixtureAttackZombies();
			for (int l = 0; l < num2; l++)
			{
				string text2 = fixtureMode;
				bool flag = ((text2 == "attack" || text2 == "battle") ? true : false);
				bool flag2 = flag && num3 < requestedAttackCount;
				SpawnFixtureZombie(requestedCount + _fixtureReplacementSpawns, flag2);
				if (flag2)
				{
					num3++;
				}
				_fixtureReplacementSpawns++;
			}
			SetVisibleStatus($"Repairing real battle zombies: {_fixtureZombies.Count}/{requestedCount}");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		PruneInvalidFixtureZombies();
		if (CountValidFixtureZombies() != requestedCount)
		{
			throw new InvalidOperationException($"Zombie fixture count mismatch: {CountValidFixtureZombies()}/{requestedCount}.");
		}
		for (int m = 0; m < _fixtureZombies.Count; m++)
		{
			TowerDefenseZombie towerDefenseZombie2 = _fixtureZombies[m];
			if (GodotObject.IsInstanceValid(towerDefenseZombie2))
			{
				towerDefenseZombie2.ProcessMode = ProcessModeEnum.Inherit;
			}
		}
		GD.Print($"[SurvivalZombieFixture] ready mode={fixtureMode} packets={string.Join(",", packetNames)} requested={requestedCount} attackRoles={CountAssignedFixtureAttackZombies()} walkRoles={CountAssignedFixtureWalkZombies()} spawned={_fixtureZombies.Count} replacements={_fixtureReplacementSpawns} removedRestored={removedZombies} plantTargetLanes={lineTargets.Count}");
		TowerDefenseZombie SpawnFixtureZombie(int num4, bool useAttackRole)
		{
			string text3 = packetNames[num4 % packetNames.Count];
			TowerDefensePacketConfig towerDefensePacketConfig = packetConfigs[text3].CreateSpawnRuntimeCopy();
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				throw new InvalidOperationException("Failed to create a runtime packet shell for '" + text3 + "'.");
			}
			List<int> list3 = validLinesByPacket[text3];
			int num5 = list3[num4 / packetNames.Count % list3.Count];
			TowerDefenseCharacter towerDefenseCharacter = towerDefensePacketConfig.Spawn(num5);
			TowerDefenseZombie zombie = towerDefenseCharacter as TowerDefenseZombie;
			if (zombie == null)
			{
				throw new InvalidOperationException("Packet '" + text3 + "' did not spawn a zombie.");
			}
			zombie.ProcessMode = ProcessModeEnum.Disabled;
			TowerDefensePlant towerDefensePlant = lineTargets[num5];
			Vector2I vector2I;
			Vector2 value;
			if (fixtureMode == "plant-target")
			{
				vector2I = new Vector2I(map.config.gridNum.X, num5);
				value = TowerDefenseManager.GetMapCellPlantPos(vector2I);
				_fixtureWalkAnchors.Add(zombie, value);
				_fixtureWalkGrids.Add(zombie, vector2I);
			}
			else if (useAttackRole)
			{
				float x = Mathf.Clamp(TowerDefenseManager.Instance.gridSize.X * 0.4f, 24f, 64f);
				value = towerDefensePlant.GlobalPosition + new Vector2(x, 0f);
				vector2I = towerDefensePlant.gridPos;
				_fixtureAttackAnchors.Add(zombie, value);
				_fixtureAttackGrids.Add(zombie, vector2I);
			}
			else
			{
				vector2I = new Vector2I(1, num5);
				value = manager.GetMapCellPos(vector2I);
				_fixtureWalkAnchors.Add(zombie, value);
				_fixtureWalkGrids.Add(zombie, vector2I);
			}
			zombie.gridPos = vector2I;
			zombie.SetGlobalPositionForPhysicsFrame(value, Engine.GetPhysicsFrames());
			MakeCharacterBodyDurable(zombie);
			wave.AddSpawnCharacter(zombie);
			_fixtureZombies.Add(zombie);
			_fixtureZombiePacketNames.Add(text3);
			_fixtureZombieAttackRoles.Add(useAttackRole);
			_fixturePacketCounts[text3]++;
			string text4 = fixtureMode;
			if ((text4 == "idle-detect" || text4 == "plant-target") ? true : false)
			{
				zombie.groundMoveComponent?.SetAlive(false);
			}
			if (fixtureMode == "plant-target" && GodotObject.IsInstanceValid(zombie.componentManager))
			{
				IReadOnlyList<CharacterComponentRuntime> resourceComponents = zombie.componentManager.ResourceComponents;
				for (int n = 0; n < resourceComponents.Count; n++)
				{
					CharacterComponentRuntime characterComponentRuntime = resourceComponents[n];
					if ((characterComponentRuntime is AttackComponent || characterComponentRuntime is FireComponent) ? true : false)
					{
						resourceComponents[n].SetAlive(alive: false);
					}
				}
			}
			text4 = fixtureMode;
			if ((text4 == "plant-target" || text4 == "battle") ? true : false)
			{
				zombie.OnBodyHurt += delegate
				{
					_plantTargetZombieBodyHurtEvents++;
					_plantTargetZombieHitTargets.Add(zombie);
				};
				zombie.OnArmorHurt += delegate
				{
					_plantTargetZombieArmorHurtEvents++;
					_plantTargetZombieHitTargets.Add(zombie);
				};
			}
			return zombie;
		}
	}

	private async Task WaitForWaveSpawnSettlementAsync(TowerDefenseBattleFeatureWave wave)
	{
		Stopwatch timeout = Stopwatch.StartNew();
		while (wave.IsSpawnPipelineActive)
		{
			if (timeout.Elapsed.TotalSeconds >= 15.0)
			{
				throw new TimeoutException($"Wave spawn work did not settle; awaitSpawn={wave.awaitSpawn} pending={wave.HasPendingSpawnOperations} pipeline={wave.IsSpawnPipelineActive}.");
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static bool RequiresTallFixtureTarget(IReadOnlyList<string> packetNames)
	{
		for (int i = 0; i < packetNames.Count; i++)
		{
			string text = packetNames[i];
			if (text.Contains("Pogo", StringComparison.OrdinalIgnoreCase) || text.Equals("ZombieChess", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<System.Collections.Generic.Dictionary<int, TowerDefensePlant>> PrepareDurablePlantTargetsAsync(TowerDefenseBattleFeatureMap map, bool forceSpecialMovementBlocker)
	{
		System.Collections.Generic.Dictionary<int, TowerDefensePlant> lineTargets = new System.Collections.Generic.Dictionary<int, TowerDefensePlant>();
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			if (!(cleanCharactersList[i] is TowerDefensePlant towerDefensePlant) || !GodotObject.IsInstanceValid(towerDefensePlant.instance) || towerDefensePlant.die || towerDefensePlant.nearDie || towerDefensePlant.instance.invincible || !towerDefensePlant.instance.canBeCollection)
			{
				continue;
			}
			int y = towerDefensePlant.gridPos.Y;
			if (y > 0 && y <= map.config.gridNum.Y && y < map.lineUse.Count && map.lineUse[y])
			{
				MakeCharacterAndArmorDurable(towerDefensePlant);
				towerDefensePlant.OnBodyHurt += delegate
				{
					_fixturePlantBodyHurtEvents++;
				};
				towerDefensePlant.OnArmorHurt += delegate
				{
					_fixturePlantArmorHurtEvents++;
				};
				if (!lineTargets.TryGetValue(y, out var value) || towerDefensePlant.GlobalPosition.X > value.GlobalPosition.X)
				{
					lineTargets[y] = towerDefensePlant;
				}
			}
		}
		if (lineTargets.Count == 0)
		{
			ResourceManager.Instance.RequireFullGameplayResourcesReady("PrepareDurablePlantTargetsAsync");
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly("PlantPeaShooterSingle");
			if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || !(packetConfigReadOnly.characterConfig is TowerDefensePlantConfig plantConfig))
			{
				throw new InvalidOperationException("Fixture target 'PlantPeaShooterSingle' is not a loaded plant packet.");
			}
			int num = Math.Clamp(4, 1, map.config.gridNum.X);
			for (int num2 = 1; num2 <= map.config.gridNum.Y; num2++)
			{
				if (num2 >= map.lineUse.Count || !map.lineUse[num2])
				{
					continue;
				}
				Vector2I vector2I = default(Vector2I);
				bool flag = false;
				for (int num3 = 0; num3 < map.config.gridNum.X; num3++)
				{
					if (flag)
					{
						break;
					}
					int num4 = num + num3;
					int num5 = num - num3;
					if (num4 <= map.config.gridNum.X)
					{
						Vector2I vector2I2 = new Vector2I(num4, num2);
						if (CanPlantFixtureOccupyGrid(map, plantConfig, vector2I2))
						{
							vector2I = vector2I2;
							flag = true;
						}
					}
					if (!flag && num5 >= 1 && num5 != num4)
					{
						Vector2I vector2I3 = new Vector2I(num5, num2);
						if (CanPlantFixtureOccupyGrid(map, plantConfig, vector2I3))
						{
							vector2I = vector2I3;
							flag = true;
						}
					}
				}
				if (flag)
				{
					if (!(packetConfigReadOnly.CreateSpawnRuntimeCopy()?.Plant(vector2I, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) is TowerDefensePlant towerDefensePlant2))
					{
						throw new InvalidOperationException($"Fixture target '{"PlantPeaShooterSingle"}' failed at {vector2I}.");
					}
					_fixtureTargetPlants.Add(towerDefensePlant2);
					lineTargets.Add(num2, towerDefensePlant2);
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			foreach (TowerDefensePlant value2 in lineTargets.Values)
			{
				MakeCharacterAndArmorDurable(value2);
				value2.OnBodyHurt += delegate
				{
					_fixturePlantBodyHurtEvents++;
				};
				value2.OnArmorHurt += delegate
				{
					_fixturePlantArmorHurtEvents++;
				};
			}
			GD.Print($"[SurvivalZombieFixture] cleanTargets packet={"PlantPeaShooterSingle"} created={_fixtureTargetPlants.Count} lanes={lineTargets.Count}");
		}
		if (forceSpecialMovementBlocker)
		{
			foreach (TowerDefensePlant value3 in lineTargets.Values)
			{
				value3.Scale *= 4f;
				value3.instance.invincible = false;
				value3.instance.invincibleHurt = false;
				value3.instance.invincibleSmash = false;
				value3.instance.canBeCollection = true;
				value3.instance.die = false;
				value3.instance.nearDie = false;
				value3.instance.height = TowerDefenseEnum.CHARACTER_HEIGHT.TALL;
				value3.instance.maskFlags = -1;
				value3.instance.collisionFlags = -1;
				value3.targetRegistrationComponent?.SetAlive(alive: true);
				if (value3.targetRegistrationComponent != null)
				{
					value3.targetRegistrationComponent.allLineCheck = true;
					value3.targetRegistrationComponent.canProjectileCheck = true;
					value3.targetRegistrationComponent.NotifyTargetStateChanged();
				}
				if (!GodotObject.IsInstanceValid(value3.componentManager))
				{
					continue;
				}
				IReadOnlyList<CharacterComponentRuntime> resourceComponents = value3.componentManager.ResourceComponents;
				for (int num6 = 0; num6 < resourceComponents.Count; num6++)
				{
					CharacterComponentRuntime characterComponentRuntime = resourceComponents[num6];
					if ((!(characterComponentRuntime is TargetRegistrationComponent) && !(characterComponentRuntime is HurtComponent)) || 1 == 0)
					{
						characterComponentRuntime.SetAlive(alive: false);
					}
				}
			}
		}
		return lineTargets;
	}

	private static void MakeCharacterBodyDurable(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.instance))
		{
			character.instance.keepAlive = true;
			character.instance.hitpointsNearDeath = 0.0;
			character.instance.hitpointsSave = 1E+30;
			character.instance.hitpoints = 1E+30;
		}
	}

	private static void MakeCharacterAndArmorDurable(TowerDefenseCharacter character)
	{
		MakeCharacterBodyDurable(character);
		for (int i = 0; i < character.instance.armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = character.instance.armorList[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
			{
				towerDefenseArmorInstance.damagePointBase = 1E+30;
				towerDefenseArmorInstance.hitpointsSave = 1E+30;
				towerDefenseArmorInstance.hitPoints = 1E+30;
			}
		}
	}

	private async Task StabilizeIdleFixtureAsync()
	{
		PinFixtureWalkRoles();
		await WaitSecondsAsync(1.0);
		PinFixtureWalkRoles();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		PinFixtureWalkRoles();
	}

	private async Task StabilizeAttackFixtureAtNormalSpeedAsync()
	{
		Stopwatch timeout = Stopwatch.StartNew();
		while (timeout.Elapsed.TotalSeconds < 6.0)
		{
			bool flag = false;
			for (int i = 0; i < _fixtureZombies.Count; i++)
			{
				TowerDefenseZombie towerDefenseZombie = _fixtureZombies[i];
				if (GodotObject.IsInstanceValid(towerDefenseZombie) && towerDefenseZombie.isRise)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		ulong physicsFrames = Engine.GetPhysicsFrames();
		foreach (KeyValuePair<TowerDefenseZombie, Vector2> fixtureAttackAnchor in _fixtureAttackAnchors)
		{
			TowerDefenseZombie key = fixtureAttackAnchor.Key;
			if (GodotObject.IsInstanceValid(key))
			{
				key.gridPos = _fixtureAttackGrids[key];
				key.SetGlobalPositionForPhysicsFrame(fixtureAttackAnchor.Value, physicsFrames);
			}
		}
		PinFixtureWalkRoles();
		await WaitSecondsAsync(0.2);
		PinFixtureWalkRoles();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		PinFixtureWalkRoles();
		_fixtureAttackCandidatesReady = 0;
		foreach (KeyValuePair<TowerDefenseZombie, Vector2> fixtureAttackAnchor2 in _fixtureAttackAnchors)
		{
			TowerDefenseZombie key2 = fixtureAttackAnchor2.Key;
			AttackComponent attackComponent = key2?.attackComponent;
			if (GodotObject.IsInstanceValid(key2) && !(attackComponent?.IsReleased ?? true) && attackComponent.CanAttack())
			{
				_fixtureAttackCandidatesReady++;
				if (!(key2.CurrentStateHandle?.StableId ?? "").Contains("pogo", StringComparison.OrdinalIgnoreCase))
				{
					key2.Attack();
				}
			}
		}
		GD.Print($"[SurvivalZombieFixture] attack-ready candidates={_fixtureAttackCandidatesReady}/{_fixtureAttackAnchors.Count}");
	}

	private async Task StabilizePlantTargetFixtureAsync()
	{
		await WaitSecondsAsync(1.0);
		PinPlantTargetZombies();
		await WaitSecondsAsync(0.2);
		PinPlantTargetZombies();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		PinPlantTargetZombies();
	}

	private void PinPlantTargetZombies()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		foreach (KeyValuePair<TowerDefenseZombie, Vector2> fixtureWalkAnchor in _fixtureWalkAnchors)
		{
			TowerDefenseZombie key = fixtureWalkAnchor.Key;
			if (!GodotObject.IsInstanceValid(key?.instance))
			{
				continue;
			}
			key.gridPos = _fixtureWalkGrids[key];
			key.SetGlobalPositionForPhysicsFrame(fixtureWalkAnchor.Value, physicsFrames);
			if ((key.CurrentStateHandle?.StableId ?? "") != "zombie.walk")
			{
				key.Walk();
			}
			key.groundMoveComponent?.SetAlive(false);
			MakeCharacterBodyDurable(key);
			key.instance.invincible = false;
			key.instance.invincibleHurt = false;
			key.instance.invincibleSmash = false;
			key.instance.canBeCollection = true;
			key.instance.die = false;
			key.instance.nearDie = false;
			key.targetRegistrationComponent?.SetAlive(alive: true);
			if (key.targetRegistrationComponent != null)
			{
				key.targetRegistrationComponent.canProjectileCheck = true;
				key.targetRegistrationComponent.NotifyTargetStateChanged();
			}
			if (!GodotObject.IsInstanceValid(key.componentManager))
			{
				continue;
			}
			IReadOnlyList<CharacterComponentRuntime> resourceComponents = key.componentManager.ResourceComponents;
			for (int i = 0; i < resourceComponents.Count; i++)
			{
				CharacterComponentRuntime characterComponentRuntime = resourceComponents[i];
				if ((characterComponentRuntime is AttackComponent || characterComponentRuntime is FireComponent) ? true : false)
				{
					resourceComponents[i].SetAlive(alive: false);
				}
			}
		}
	}

	private void PinFixtureWalkRoles()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		foreach (KeyValuePair<TowerDefenseZombie, Vector2> fixtureWalkAnchor in _fixtureWalkAnchors)
		{
			TowerDefenseZombie key = fixtureWalkAnchor.Key;
			if (GodotObject.IsInstanceValid(key))
			{
				key.gridPos = _fixtureWalkGrids[key];
				key.SetGlobalPositionForPhysicsFrame(fixtureWalkAnchor.Value, physicsFrames);
				if ((key.CurrentStateHandle?.StableId ?? "") != "zombie.walk")
				{
					key.Walk();
				}
				key.groundMoveComponent?.SetAlive(false);
			}
		}
	}

	private void AttachFixtureEvidenceHandlers()
	{
		for (int i = 0; i < _fixtureZombies.Count; i++)
		{
			TowerDefenseZombie zombie = _fixtureZombies[i];
			if (!GodotObject.IsInstanceValid(zombie))
			{
				continue;
			}
			StateHandle stateHandle = zombie.StateMachine?.GetStateById("zombie.attack");
			if (stateHandle != null && stateHandle.IsValid)
			{
				stateHandle.Entered += delegate
				{
					_fixtureAttackStateEntries++;
					_fixtureAttackStateSources.Add(zombie);
				};
			}
			ComponentManager componentManager = zombie.componentManager;
			if (!GodotObject.IsInstanceValid(componentManager))
			{
				continue;
			}
			IReadOnlyList<CharacterComponentRuntime> resourceComponents = componentManager.ResourceComponents;
			for (int num = 0; num < resourceComponents.Count; num++)
			{
				CharacterComponentRuntime characterComponentRuntime = resourceComponents[num];
				AttackComponent attack = characterComponentRuntime as AttackComponent;
				if (attack != null)
				{
					_fixtureAttackComponentCount++;
					attack.OnAttack += delegate
					{
						_fixtureAttackEvents++;
						_fixtureEngagedAttackComponents.Add(attack);
					};
					continue;
				}
				characterComponentRuntime = resourceComponents[num];
				FireComponent fire = characterComponentRuntime as FireComponent;
				if (fire != null)
				{
					_fixtureFireComponentCount++;
					fire.OnFireVolley += delegate
					{
						_fixtureFireVolleys++;
						_fixtureEngagedFireComponents.Add(fire);
					};
				}
			}
		}
		GD.Print($"[SurvivalZombieFixture] components attack={_fixtureAttackComponentCount} fire={_fixtureFireComponentCount}");
	}

	private int CountValidFixtureZombies()
	{
		int num = 0;
		for (int i = 0; i < _fixtureZombies.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_fixtureZombies[i]))
			{
				num++;
			}
		}
		return num;
	}

	private int CountFixtureZombiesInsideTree()
	{
		int num = 0;
		for (int i = 0; i < _fixtureZombies.Count; i++)
		{
			TowerDefenseZombie towerDefenseZombie = _fixtureZombies[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombie) && !towerDefenseZombie.IsQueuedForDeletion() && towerDefenseZombie.IsInsideTree())
			{
				num++;
			}
		}
		return num;
	}

	private int PruneInvalidFixtureZombies()
	{
		int num = 0;
		for (int num2 = _fixtureZombies.Count - 1; num2 >= 0; num2--)
		{
			TowerDefenseZombie towerDefenseZombie = _fixtureZombies[num2];
			if (!GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				string text = ((num2 < _fixtureZombiePacketNames.Count) ? _fixtureZombiePacketNames[num2] : "");
				_fixtureZombies.RemoveAt(num2);
				if (num2 < _fixtureZombiePacketNames.Count)
				{
					_fixtureZombiePacketNames.RemoveAt(num2);
				}
				if (num2 < _fixtureZombieAttackRoles.Count)
				{
					_fixtureZombieAttackRoles.RemoveAt(num2);
				}
				_fixtureAttackAnchors.Remove(towerDefenseZombie);
				_fixtureAttackGrids.Remove(towerDefenseZombie);
				_fixtureWalkAnchors.Remove(towerDefenseZombie);
				_fixtureWalkGrids.Remove(towerDefenseZombie);
				if (!string.IsNullOrEmpty(text) && _fixturePacketCounts.TryGetValue(text, out var value))
				{
					_fixturePacketCounts[text] = Math.Max(0, value - 1);
				}
				num++;
			}
		}
		return num;
	}

	private int CountAssignedFixtureAttackZombies()
	{
		int num = 0;
		for (int i = 0; i < _fixtureZombieAttackRoles.Count; i++)
		{
			if (_fixtureZombieAttackRoles[i])
			{
				num++;
			}
		}
		return num;
	}

	private int CountAssignedFixtureWalkZombies()
	{
		return Math.Max(0, _fixtureZombies.Count - CountAssignedFixtureAttackZombies());
	}

	private int CountValidFixtureTargetPlants()
	{
		int num = 0;
		for (int i = 0; i < _fixtureTargetPlants.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = _fixtureTargetPlants[i];
			if (GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.IsQueuedForDeletion())
			{
				num++;
			}
		}
		return num;
	}

	private Array<string> BuildFixturePacketArray()
	{
		Array<string> array = new Array<string>();
		foreach (string key in _fixturePacketCounts.Keys)
		{
			array.Add(key);
		}
		return array;
	}

	private Godot.Collections.Dictionary<string, int> BuildFixturePacketCountDictionary()
	{
		Godot.Collections.Dictionary<string, int> dictionary = new Godot.Collections.Dictionary<string, int>();
		foreach (KeyValuePair<string, int> fixturePacketCount in _fixturePacketCounts)
		{
			dictionary.Add(fixturePacketCount.Key, fixturePacketCount.Value);
		}
		return dictionary;
	}

	private Godot.Collections.Dictionary<string, int> BuildFixtureStateCountDictionary()
	{
		Godot.Collections.Dictionary<string, int> dictionary = new Godot.Collections.Dictionary<string, int>();
		for (int i = 0; i < _fixtureZombies.Count; i++)
		{
			TowerDefenseZombie towerDefenseZombie = _fixtureZombies[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				string key = towerDefenseZombie.CurrentStateHandle?.StableId?.ToString() ?? "<none>";
				dictionary[key] = ((!dictionary.TryGetValue(key, out var value)) ? 1 : (value + 1));
			}
		}
		return dictionary;
	}

	private int CountProductionWaveZombies()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			return 0;
		}
		return instance.characterRegistry.GetZombieCount();
	}

	private void IssueProductionWaveMassDeath()
	{
		_productionWaveMassDeathIssued = true;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> activeCharacters = instance.characterRegistry.GetActiveCharacters();
		for (int i = 0; i < activeCharacters.Count; i++)
		{
			if (_productionWaveMassDeathIssuedCount >= _productionWaveMassDeathRequestedCount)
			{
				break;
			}
			if (activeCharacters[i] is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie) && !towerDefenseZombie.IsQueuedForDeletion() && !towerDefenseZombie.die && GodotObject.IsInstanceValid(towerDefenseZombie.sprite) && towerDefenseZombie.zombieDeathComponent != null)
			{
				towerDefenseZombie.timeScale = 20.0;
				towerDefenseZombie.sprite.timeScale = 20.0;
				towerDefenseZombie.zombieDeathComponent.fadeDuration = 0.03;
				towerDefenseZombie.Die();
				_productionWaveMassDeathIssuedCount++;
			}
		}
		GD.Print($"[SurvivalProductionWaveMassDeath] requested={_productionWaveMassDeathRequestedCount} issued={_productionWaveMassDeathIssuedCount} remaining={CountProductionWaveZombies()}");
	}

	private int CountValidPlantFixturePlants()
	{
		int num = 0;
		for (int i = 0; i < _plantFixturePlants.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = _plantFixturePlants[i];
			if (GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.IsQueuedForDeletion() && towerDefensePlant.IsInsideTree())
			{
				num++;
			}
		}
		return num;
	}

	private int CountAwakePlantFixturePlants()
	{
		int num = 0;
		for (int i = 0; i < _plantFixturePlants.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = _plantFixturePlants[i];
			if (GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.IsQueuedForDeletion() && towerDefensePlant.IsInsideTree() && !towerDefensePlant.IsSleep())
			{
				num++;
			}
		}
		return num;
	}

	private Godot.Collections.Dictionary<string, int> BuildPlantFixtureStateCountDictionary()
	{
		Godot.Collections.Dictionary<string, int> dictionary = new Godot.Collections.Dictionary<string, int>();
		for (int i = 0; i < _plantFixturePlants.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = _plantFixturePlants[i];
			if (GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.IsQueuedForDeletion())
			{
				string key = towerDefensePlant.CurrentStateHandle?.StableId?.ToString() ?? "<none>";
				dictionary[key] = ((!dictionary.TryGetValue(key, out var value)) ? 1 : (value + 1));
			}
		}
		return dictionary;
	}

	private Array<string> BuildProductionWavePacketArray()
	{
		Array<string> array = new Array<string>();
		for (int i = 0; i < _productionWavePackets.Count; i++)
		{
			array.Add(_productionWavePackets[i]);
		}
		return array;
	}

	private Dictionary BuildProductionWavePacketCountDictionary(out int otherZombies, out Dictionary otherPacketCounts)
	{
		Dictionary dictionary = new Dictionary();
		for (int i = 0; i < _productionWavePackets.Count; i++)
		{
			dictionary[_productionWavePackets[i]] = 0;
		}
		otherZombies = 0;
		otherPacketCounts = new Dictionary();
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			return dictionary;
		}
		List<TowerDefenseCharacter> cleanCharactersList = instance.characterRegistry.GetCleanCharactersList();
		for (int j = 0; j < cleanCharactersList.Count; j++)
		{
			if (cleanCharactersList[j] is TowerDefenseZombie towerDefenseZombie)
			{
				string text = (GodotObject.IsInstanceValid(towerDefenseZombie.packet) ? towerDefenseZombie.packet.saveKey : "");
				if (_productionWavePacketSet.Contains(text))
				{
					dictionary[text] = dictionary[text].AsInt32() + 1;
					continue;
				}
				otherZombies++;
				string text2 = (string.IsNullOrWhiteSpace(text) ? "<unknown>" : text);
				otherPacketCounts[text2] = ((!otherPacketCounts.TryGetValue(text2, out var value)) ? 1 : (value.AsInt32() + 1));
			}
		}
		return dictionary;
	}

	private Godot.Collections.Dictionary<string, int> BuildProductionWaveStateCountDictionary()
	{
		Godot.Collections.Dictionary<string, int> dictionary = new Godot.Collections.Dictionary<string, int>();
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			return dictionary;
		}
		List<TowerDefenseCharacter> cleanCharactersList = instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			if (cleanCharactersList[i] is TowerDefenseZombie towerDefenseZombie)
			{
				string item = (GodotObject.IsInstanceValid(towerDefenseZombie.packet) ? towerDefenseZombie.packet.saveKey : "");
				if (_productionWavePacketSet.Contains(item))
				{
					string key = towerDefenseZombie.CurrentStateHandle?.StableId?.ToString() ?? "<none>";
					dictionary[key] = ((!dictionary.TryGetValue(key, out var value)) ? 1 : (value + 1));
				}
			}
		}
		return dictionary;
	}

	public override void _Process(double delta)
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
		if (_productionWaveKeepBodiesDurable)
		{
			_productionWaveDurabilityRefreshSeconds += delta;
			if (_productionWaveDurabilityRefreshSeconds >= 0.1)
			{
				_productionWaveDurabilityRefreshSeconds = 0.0;
				RefreshProductionWaveDurableBodies();
			}
		}
	}

	private void InitializeVisibleStatus()
	{
		CanvasLayer canvasLayer = new CanvasLayer
		{
			Name = "PerformanceStatusLayer",
			Layer = 1024
		};
		AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
		ColorRect colorRect = new ColorRect
		{
			Name = "PerformanceStatusPanel",
			Position = new Vector2(16f, 16f),
			Size = new Vector2(1048f, 72f),
			Color = new Color(0.03f, 0.04f, 0.07f, 0.94f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		canvasLayer.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		_visibleStatusLabel = new Label
		{
			Name = "PerformanceStatusLabel",
			Position = new Vector2(18f, 10f),
			Size = new Vector2(1012f, 52f),
			Text = "Starting visible production battle test...",
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_visibleStatusLabel.AddThemeFontSizeOverride("font_size", 24);
		colorRect.AddChild(_visibleStatusLabel, forceReadableName: false, InternalMode.Disabled);
	}

	private void SetVisibleStatus(string text)
	{
		if (GodotObject.IsInstanceValid(_visibleStatusLabel))
		{
			_visibleStatusLabel.Text = text;
		}
	}

	private async Task PresentVisibleStatusAsync(string text)
	{
		SetVisibleStatus(text);
		if (_visibleWindow)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void RequireAutoloads()
	{
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance))
		{
			throw new InvalidOperationException("Required gameplay autoloads are unavailable.");
		}
	}

	private string CopyProgressIntoSandbox(string sourceSavePath)
	{
		string fullPath = Path.GetFullPath(ProjectSettings.GlobalizePath("user://"));
		if (sourceSavePath.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("The source progress file must remain outside the isolated test user directory.");
		}
		string obj = $"user://Csharp/Progress/{"SurvivalProgressPerfProbe"}/{_levelSaveKey}.tres";
		string fullPath2 = Path.GetFullPath(ProjectSettings.GlobalizePath(obj));
		string? directoryName = Path.GetDirectoryName(fullPath2);
		if (string.IsNullOrWhiteSpace(directoryName))
		{
			throw new InvalidOperationException("The isolated progress directory is invalid.");
		}
		Directory.CreateDirectory(directoryName);
		File.Copy(sourceSavePath, fullPath2, overwrite: true);
		if (CaptureFileSnapshot(sourceSavePath).Hash != CaptureFileSnapshot(fullPath2).Hash)
		{
			throw new IOException("The isolated progress copy does not match its source.");
		}
		return obj;
	}

	private static async Task PrepareEmptyModEnvironmentAsync()
	{
		XWModManager xWModManager = new XWModManager(ProjectSettings.GlobalizePath("user://SurvivalPerformanceMods"));
		xWModManager.SaveEnabledIds(System.Array.Empty<string>());
		xWModManager.LoadEnabledModsWithResult();
		XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.EnsureReadyAsync();
		if (!xWModEnvironmentStatus.Ready || xWModEnvironmentStatus.Snapshot == null || xWModEnvironmentStatus.Snapshot.Mods.Count != 0)
		{
			throw new InvalidOperationException("性能测试无 Mod 环境未就绪：" + xWModEnvironmentStatus.Reason);
		}
	}

	private async Task EnsureResourcesLoadedAsync()
	{
		bool resourcesLoaded = false;
		double lastPercentage = 0.0;
		ResourceManager.Instance.OnLoadOver += OnLoadOver;
		ResourceManager.Instance.OnLoadPercentage += OnLoadPercentage;
		try
		{
			ResourceManager.Instance.BeginLoad();
			Stopwatch timeout = Stopwatch.StartNew();
			double nextFinalizationStatusSeconds = 0.0;
			while (!resourcesLoaded && timeout.Elapsed.TotalSeconds < 180.0)
			{
				if (_visibleWindow && lastPercentage >= 0.999 && timeout.Elapsed.TotalSeconds >= nextFinalizationStatusSeconds)
				{
					string value = ResourceManager.Instance.CurrentGameplayAtlasLoadState switch
					{
						ResourceManager.GameplayAtlasLoadState.LoadingVisual => "finalizing the global visual atlas", 
						ResourceManager.GameplayAtlasLoadState.LoadingPose => "finalizing the global GPU pose atlas", 
						ResourceManager.GameplayAtlasLoadState.Ready => "warming shared character dependencies", 
						ResourceManager.GameplayAtlasLoadState.Failed => "atlas failed: " + ResourceManager.Instance.GameplayAtlasLoadError, 
						_ => "waiting for the global animation atlases", 
					};
					SetVisibleStatus($"Finalizing resources: {value} ({timeout.Elapsed.TotalSeconds:F1}s)...");
					nextFinalizationStatusSeconds = timeout.Elapsed.TotalSeconds + 0.25;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		finally
		{
			ResourceManager.Instance.OnLoadOver -= OnLoadOver;
			ResourceManager.Instance.OnLoadPercentage -= OnLoadPercentage;
		}
		if (!resourcesLoaded)
		{
			throw new TimeoutException("ResourceManager did not finish loading in time.");
		}
		void OnLoadOver()
		{
			resourcesLoaded = true;
		}
		void OnLoadPercentage(double percentage, string stepName, string resourceName, int resourceIndex, int resourceTotal)
		{
			lastPercentage = percentage;
			string value2 = ((resourceTotal > 0) ? $" {resourceIndex}/{resourceTotal}" : "");
			string value3 = (string.IsNullOrWhiteSpace(resourceName) ? "" : (" - " + resourceName));
			SetVisibleStatus($"Loading {percentage * 100.0:F1}%: {stepName}{value2}{value3}");
		}
	}

	private async Task<TowerDefenseControlNew> StartBattleAsync()
	{
		Global.Instance.enterLevelMode = "LevelChoose";
		Global.Instance.enterLevelIsBattle = false;
		Global.Instance.isMultiplayerMode = false;
		await PresentVisibleStatusAsync("Loading the production level config...");
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(_levelPath, "", ResourceLoader.CacheMode.IgnoreDeep);
		if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
		{
			throw new InvalidOperationException("Failed to load level config " + _levelPath + ".");
		}
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		await PresentVisibleStatusAsync("Loading the production battle scene...");
		PackedScene packedBattle = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", "", ResourceLoader.CacheMode.IgnoreDeep);
		if (!GodotObject.IsInstanceValid(packedBattle) || !packedBattle.CanInstantiate())
		{
			throw new InvalidOperationException("Failed to load battle scene res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn.");
		}
		await PresentVisibleStatusAsync("Instantiating the production battle scene...");
		TowerDefenseControlNew battle = packedBattle.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(battle))
		{
			throw new InvalidOperationException("Failed to instantiate the production battle scene.");
		}
		await PresentVisibleStatusAsync("Adding the production battle scene to the tree...");
		AddChild(battle, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		return battle;
	}

	private async Task WaitForProgressEntryAsync(TowerDefenseControlNew battle, int expectedCharacters)
	{
		Stopwatch timeout = Stopwatch.StartNew();
		while (timeout.Elapsed.TotalSeconds < 180.0)
		{
			CloseRestorePauseDialogs(GetTree().Root);
			GetTree().Paused = false;
			int num = TowerDefenseManager.Instance.characterRegistry?.ActiveCharacterCount ?? 0;
			if (battle.isGameRunning && !battle.hasProgress && num > 0 && num <= expectedCharacters)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		throw new TimeoutException($"Progress entry did not finish in time; running={battle.isGameRunning} hasProgress={battle.hasProgress} characters={TowerDefenseManager.Instance.characterRegistry?.ActiveCharacterCount ?? 0}/{expectedCharacters}.");
	}

	private static void CloseRestorePauseDialogs(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			CloseRestorePauseDialogs(child);
			if (child is DialogBoxBase { pasue: not false } dialogBoxBase)
			{
				dialogBoxBase.CloseDialog();
			}
		}
	}

	private static int CountPauseDialogs(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in node.GetChildren())
		{
			num += CountPauseDialogs(child);
			if (child is DialogBoxBase { pasue: not false })
			{
				num++;
			}
		}
		return num;
	}

	private async Task WaitSecondsAsync(double seconds)
	{
		Stopwatch timer = Stopwatch.StartNew();
		while (timer.Elapsed.TotalSeconds < seconds)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void CollectCharacterCounts(out int characters, out int plants, out int zombies, out int items, out int otherCharacters)
	{
		characters = 0;
		plants = 0;
		zombies = 0;
		items = 0;
		otherCharacters = 0;
		foreach (Node item in GetTree().GetNodesInGroup("Character"))
		{
			if (item is TowerDefenseCharacter)
			{
				characters++;
				if (item is TowerDefensePlant)
				{
					plants++;
				}
				else if (item is TowerDefenseZombie)
				{
					zombies++;
				}
				else if (item is TowerDefenseItem)
				{
					items++;
				}
				else
				{
					otherCharacters++;
				}
			}
		}
	}

	private Dictionary CaptureBattlePopulation()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		Viewport viewport = GetViewport();
		Rect2 visibleRect = viewport.GetVisibleRect();
		Transform2D canvasTransform = viewport.GetCanvasTransform();
		foreach (Node item in GetTree().GetNodesInGroup("Character"))
		{
			if (!(item is TowerDefenseCharacter towerDefenseCharacter) || towerDefenseCharacter.IsQueuedForDeletion())
			{
				continue;
			}
			if (towerDefenseCharacter is TowerDefensePlant)
			{
				num++;
			}
			else
			{
				if (!(towerDefenseCharacter is TowerDefenseZombie))
				{
					continue;
				}
				num2++;
			}
			if (towerDefenseCharacter.IsVisibleInTree())
			{
				AdobeAnimateSprite sprite = towerDefenseCharacter.sprite;
				if (sprite != null && sprite.IsVisibleInTree())
				{
					num3++;
				}
			}
			if (visibleRect.HasPoint(canvasTransform * towerDefenseCharacter.GetLogicalGlobalPosition()))
			{
				num4++;
			}
			if (towerDefenseCharacter.CanProcess())
			{
				num5++;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.sprite) && !towerDefenseCharacter.sprite.pause)
			{
				num6++;
			}
		}
		return new Dictionary
		{
			["plants"] = num,
			["zombies"] = num2,
			["visible"] = num3,
			["inside_viewport"] = num4,
			["processing"] = num5,
			["playing"] = num6
		};
	}

	private Dictionary CaptureZombieVisibilityDiagnostics(Viewport viewport)
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
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		int num16 = 0;
		int num17 = 0;
		int num18 = 0;
		int num19 = 0;
		int num20 = 0;
		Godot.Collections.Dictionary<string, int> dictionary = new Godot.Collections.Dictionary<string, int>();
		Godot.Collections.Dictionary<string, Dictionary> dictionary2 = new Godot.Collections.Dictionary<string, Dictionary>();
		Rect2 rect = (GodotObject.IsInstanceValid(viewport) ? new Rect2(Vector2.Zero, viewport.GetVisibleRect().Size).Grow(256f) : default(Rect2));
		Transform2D transform2D = (GodotObject.IsInstanceValid(viewport) ? viewport.GetCanvasTransform() : Transform2D.Identity);
		foreach (Node item in GetTree().GetNodesInGroup("Character"))
		{
			if (!(item is TowerDefenseZombie towerDefenseZombie) || !GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				continue;
			}
			num++;
			if (towerDefenseZombie.Visible)
			{
				num2++;
			}
			if (towerDefenseZombie.IsVisibleInTree())
			{
				num3++;
			}
			if (towerDefenseZombie.invisible)
			{
				num6++;
			}
			if (towerDefenseZombie.ProcessMode == ProcessModeEnum.Disabled)
			{
				num9++;
			}
			Vector2 logicalGlobalPosition = towerDefenseZombie.GetLogicalGlobalPosition();
			Vector2 globalPosition = towerDefenseZombie.GlobalPosition;
			if (logicalGlobalPosition.DistanceSquaredTo(globalPosition) > 1f)
			{
				num12++;
			}
			Vector2 point = transform2D * logicalGlobalPosition;
			Vector2 point2 = transform2D * globalPosition;
			if (GodotObject.IsInstanceValid(viewport) && rect.HasPoint(point))
			{
				num10++;
			}
			if (GodotObject.IsInstanceValid(viewport) && rect.HasPoint(point2))
			{
				num11++;
			}
			AdobeAnimateSprite sprite = towerDefenseZombie.sprite;
			if (!GodotObject.IsInstanceValid(sprite))
			{
				continue;
			}
			if (sprite.Visible)
			{
				num4++;
			}
			if (sprite.IsVisibleInTree())
			{
				num5++;
			}
			if (sprite.invisible)
			{
				num7++;
			}
			if (sprite.Modulate.A <= 0.001f || sprite.SelfModulate.A <= 0.001f)
			{
				num8++;
			}
			sprite.GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var currentVisible, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressed);
			if (cached)
			{
				num15++;
			}
			if (cachedVisible)
			{
				num16++;
			}
			if (currentVisible)
			{
				num17++;
			}
			if (currentVisibleWithPrefetch)
			{
				num18++;
			}
			if (currentVisible && cached && !cachedVisible)
			{
				num19++;
			}
			if (nativeCanvasSuppressed)
			{
				num20++;
			}
			AdobeAnimateRuntimeDefinition definition;
			string failureReason;
			bool flag = sprite.TryGetGpuGraphOwnerDefinitionForRender(out definition, out failureReason);
			string text = ExtractCrowdBoundsDebug(sprite);
			AdobeAnimateCrowdRenderState state;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = sprite.TryBuildCrowdRenderState(out state);
			int valueOrDefault = (state?.GpuGraphOwners?.Length).GetValueOrDefault();
			if (flag)
			{
				num13++;
			}
			else
			{
				failureReason = (string.IsNullOrWhiteSpace(failureReason) ? "<unspecified>" : failureReason);
				dictionary[failureReason] = ((!dictionary.TryGetValue(failureReason, out var value)) ? 1 : (value + 1));
			}
			Array<bool> layerVisible = sprite.layerVisible;
			int num21 = 0;
			int num22 = 0;
			Transform2D transform;
			for (int i = 0; i < layerVisible.Count; i++)
			{
				if (layerVisible[i])
				{
					num21++;
					if (sprite.TryGetFrameLayerPoseForRender(sprite.frameIndex, i, out var mediaId, out transform) && mediaId >= 0)
					{
						num22++;
					}
				}
			}
			if (num22 > 0)
			{
				num14++;
			}
			string key = (GodotObject.IsInstanceValid(towerDefenseZombie.packet) ? towerDefenseZombie.packet.saveKey : towerDefenseZombie.GetType().Name);
			if (!dictionary2.ContainsKey(key))
			{
				Transform2D transform2D2;
				if (state == null)
				{
					transform = default(Transform2D);
					transform2D2 = transform;
				}
				else
				{
					transform2D2 = state.GlobalTransform;
				}
				Transform2D transform2D3 = transform2D2;
				AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = state?.GpuGraphRootOwnerState;
				Transform2D transform2D4;
				if (adobeAnimateGpuGraphOwnerState == null)
				{
					transform = default(Transform2D);
					transform2D4 = transform;
				}
				else
				{
					transform2D4 = adobeAnimateGpuGraphOwnerState.GlobalTransform;
				}
				Transform2D transform2D5 = transform2D4;
				dictionary2[key] = new Dictionary
				{
					["type"] = towerDefenseZombie.GetType().Name,
					["clip"] = sprite.clip,
					["frame"] = sprite.frameIndex,
					["clip_range"] = $"{sprite.clipRange.X}:{sprite.clipRange.Y}",
					["layer_count"] = layerVisible.Count,
					["visible_layers"] = num21,
					["current_frame_poses"] = num22,
					["gpu_graph_eligible"] = flag,
					["gpu_failure"] = failureReason,
					["prebuild_bounds"] = text,
					["definition_frames"] = (definition?.Frames?.Length).GetValueOrDefault(),
					["crowd_state_result"] = adobeAnimateCrowdRenderStateResult.ToString(),
					["crowd_mode"] = state?.Mode.ToString() ?? "<none>",
					["crowd_transform"] = ((state == null) ? "<none>" : $"{state.GlobalTransform.Origin.X:F2},{state.GlobalTransform.Origin.Y:F2}"),
					["crowd_basis"] = ((state == null) ? "<none>" : $"{transform2D3.X.X:F3},{transform2D3.X.Y:F3};{transform2D3.Y.X:F3},{transform2D3.Y.Y:F3}"),
					["crowd_determinant"] = ((state == null) ? 0f : transform2D3.Determinant()),
					["crowd_use_absolute_transform"] = state?.GpuGraphUseAbsoluteTransform ?? false,
					["root_owner_transform"] = ((adobeAnimateGpuGraphOwnerState == null) ? "<none>" : $"{transform2D5.X.X:F3},{transform2D5.X.Y:F3};{transform2D5.Y.X:F3},{transform2D5.Y.Y:F3};{transform2D5.Origin.X:F2},{transform2D5.Origin.Y:F2}"),
					["root_owner_visible"] = adobeAnimateGpuGraphOwnerState?.Visible ?? false,
					["root_owner_alpha"] = adobeAnimateGpuGraphOwnerState?.Modulate.A ?? 0f,
					["crowd_z_index"] = state?.EffectiveZIndex ?? 0,
					["graph_owners"] = valueOrDefault,
					["root_visible"] = towerDefenseZombie.Visible,
					["sprite_visible"] = sprite.Visible,
					["sprite_visible_in_tree"] = sprite.IsVisibleInTree(),
					["crowd_native_canvas_suppressed"] = nativeCanvasSuppressed,
					["character_invisible"] = towerDefenseZombie.invisible,
					["sprite_invisible"] = sprite.invisible,
					["modulate_alpha"] = sprite.Modulate.A,
					["self_modulate_alpha"] = sprite.SelfModulate.A,
					["logical_position"] = $"{logicalGlobalPosition.X:F2},{logicalGlobalPosition.Y:F2}",
					["node_position"] = $"{globalPosition.X:F2},{globalPosition.Y:F2}",
					["sprite_global_position"] = $"{sprite.GlobalPosition.X:F2},{sprite.GlobalPosition.Y:F2}",
					["character_scale"] = $"{towerDefenseZombie.Scale.X:F3},{towerDefenseZombie.Scale.Y:F3}",
					["transform_point_scale"] = (GodotObject.IsInstanceValid(towerDefenseZombie.transformPoint) ? $"{towerDefenseZombie.transformPoint.Scale.X:F3},{towerDefenseZombie.transformPoint.Scale.Y:F3}" : "<none>"),
					["sprite_scale"] = $"{sprite.Scale.X:F3},{sprite.Scale.Y:F3}"
				};
			}
		}
		return new Dictionary
		{
			["zombies"] = num,
			["root_visible"] = num2,
			["root_visible_in_tree"] = num3,
			["sprite_visible"] = num4,
			["sprite_visible_in_tree"] = num5,
			["character_invisible"] = num6,
			["sprite_invisible"] = num7,
			["transparent"] = num8,
			["disabled_process_mode"] = num9,
			["logical_position_in_viewport"] = num10,
			["node_position_in_viewport"] = num11,
			["logical_node_position_mismatch"] = num12,
			["gpu_graph_eligible"] = num13,
			["current_frame_has_pose"] = num14,
			["culling_cached"] = num15,
			["culling_cached_visible"] = num16,
			["culling_current_visible"] = num17,
			["culling_current_visible_with_prefetch"] = num18,
			["culling_current_visible_but_cached_offscreen"] = num19,
			["crowd_native_canvas_suppressed"] = num20,
			["gpu_failure_counts"] = dictionary,
			["packet_samples"] = dictionary2
		};
	}

	private static string ExtractCrowdBoundsDebug(AdobeAnimateSprite sprite)
	{
		string[] array = sprite.BuildCrowdFilterDebugReport("save-zombie", 0).Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.StartsWith("bounds ", StringComparison.Ordinal))
			{
				return text;
			}
		}
		return "<missing>";
	}

	private static FileSnapshot CaptureFileSnapshot(string path)
	{
		FileInfo fileInfo = new FileInfo(path);
		using FileStream source = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		string hash = Convert.ToHexString(SHA256.HashData(source));
		fileInfo.Refresh();
		return new FileSnapshot(hash, fileInfo.Length, fileInfo.LastWriteTimeUtc);
	}

	private static string ReadStringArgument(string prefix)
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = prefix.Length;
				return text2.Substring(length, text2.Length - length).Trim().Trim('"');
			}
		}
		return "";
	}

	private static double ReadDoubleArgument(string prefix, double fallback)
	{
		if (!double.TryParse(ReadStringArgument(prefix), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return Math.Max(0.0, result);
	}

	private static int ReadIntArgument(string prefix, int fallback)
	{
		if (!int.TryParse(ReadStringArgument(prefix), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static ulong ReadULongArgument(string prefix, ulong fallback)
	{
		if (!ulong.TryParse(ReadStringArgument(prefix), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static bool ReadBoolArgument(string prefix, bool fallback)
	{
		if (!bool.TryParse(ReadStringArgument(prefix), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static List<string> ReadCsvArgument(string prefix)
	{
		string text = ReadStringArgument(prefix);
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string[] array = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (string item in array)
		{
			if (hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		return list;
	}
}
