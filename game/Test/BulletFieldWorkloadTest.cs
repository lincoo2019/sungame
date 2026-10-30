using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Godot;

public partial class BulletFieldWorkloadTest : Node2D
{
	private static readonly string[] CompactProjectileSceneCandidates = new string[7] { "res://Asset/Config/Projectile/Pea/Sprite/FirePea/FirePea.tscn", "res://Asset/Config/Projectile/Pea/Sprite/FirePeaS/FirePeaS.tscn", "res://Asset/Config/Projectile/Pea/Sprite/IceFirePea/IceFirePea.tscn", "res://Asset/Config/Projectile/Pea/Sprite/MegaFirePea/MegaFirePea.tscn", "res://Asset/Config/Projectile/Star/FireStar/FireStar.tscn", "res://Asset/Config/Projectile/Pea/Sprite/WhiteFirePea/WhiteFirePea.tscn", "res://Asset/Config/Projectile/Puff/Sprite/HypnoPuff/HypnoPuff.tscn" };

	private const int WorkloadVariantCount = 6;

	private readonly List<TowerDefenseProjectileConfig> _configs = new List<TowerDefenseProjectileConfig>();

	private readonly List<int> _spawnedIndices = new List<int>(65536);

	private readonly List<Vector2> _initialPositions = new List<Vector2>(65536);

	private readonly HashSet<int> _spawnedAnimationDefinitionIds = new HashSet<int>();

	private readonly int[] _workloadVariantSpawnCounts = new int[6];

	private readonly Dictionary<int, TowerDefenseProjectileConfig> _transitionConfigs = new Dictionary<int, TowerDefenseProjectileConfig>();

	private readonly Dictionary<int, BulletField.PreparedProjectileChange> _transitionPreparedChanges = new Dictionary<int, BulletField.PreparedProjectileChange>();

	private readonly List<double> _renderFrameTimes = new List<double>(131072);

	private readonly List<double> _processTimesMs = new List<double>(131072);

	private readonly List<double> _physicsFrameTimesMs = new List<double>(131072);

	private readonly List<double> _physicsUpdateTimesMs = new List<double>(16384);

	private BulletField _bulletField;

	private TowerDefenseManager _manager;

	private Vector2 _originalGridSize;

	private Vector2 _originalGridBegin;

	private Vector2I _originalGridNum;

	private int _originalMaxFps;

	private int _originalPhysicsTicks;

	private double _originalTimeScale;

	private ProjectileUpdateManager _projectileUpdateManager;

	private ProcessModeEnum _originalProjectileManagerProcessMode;

	private bool _projectileManagerProcessModeCaptured;

	private bool _originalPerfProfilerEnabled;

	private bool _originalDetailedHotPathMetrics;

	private int _originalProfilerDumpIntervalFrames;

	private int _originalProfilerMaxMetricsPerDump;

	private double _elapsed;

	private double _measuredRenderSeconds;

	private int _spawnFailures;

	private double _spawnElapsedMs;

	private bool _ready;

	private bool _screenshotSaved;

	private bool _finished;

	private bool _measurementStarted;

	private long _measurementThreadAllocatedBefore;

	private long _measurementTotalAllocatedBefore;

	private int _measurementGen0Before;

	private int _measurementGen1Before;

	private int _measurementGen2Before;

	private long _lastRenderTimestamp;

	private bool _firstPublicationMeasured;

	private double _firstPublicationMs;

	private int _firstPublicationVisible;

	private bool _burstTransitionExecuted;

	private Thread[] _contentionThreads = Array.Empty<Thread>();

	private long[] _contentionIterations = Array.Empty<long>();

	private int _stopContentionThreads;

	[Export(PropertyHint.None, "")]
	public int SpawnCount { get; set; } = 10000;

	[Export(PropertyHint.None, "")]
	public int ZBucketCount { get; set; } = 200;

	[Export(PropertyHint.None, "")]
	public float BulletScale { get; set; } = 0.12f;

	[Export(PropertyHint.None, "")]
	public double WarmupSeconds { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public double MeasureSeconds { get; set; } = 4.0;

	[Export(PropertyHint.None, "")]
	public bool PrimeSpawnRoutes { get; set; }

	[Export(PropertyHint.None, "")]
	public double MaxPrimedSpawnMilliseconds { get; set; } = 50.0;

	[Export(PropertyHint.None, "")]
	public string ScreenshotPath { get; set; } = "res://TestResults/bulletfield-workload.png";

	[Export(PropertyHint.None, "")]
	public string RuntimeProfile { get; set; } = "unknown";

	[Export(PropertyHint.None, "")]
	public bool EnableDetailedProfiler { get; set; }

	[Export(PropertyHint.None, "")]
	public string BurstTransitionMode { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public double BurstTransitionBudgetMilliseconds { get; set; } = 250.0;

	[Export(PropertyHint.None, "")]
	public int BenchmarkMaxFps { get; set; } = 180;

	[Export(PropertyHint.None, "")]
	public double MinimumAverageFps { get; set; } = -1.0;

	[Export(PropertyHint.None, "")]
	public double MinimumOnePercentLowFps { get; set; } = -1.0;

	[Export(PropertyHint.None, "")]
	public double MinimumZeroPointOnePercentLowFps { get; set; } = -1.0;

	[Export(PropertyHint.None, "")]
	public int BackgroundContentionThreads { get; set; }

	[Export(PropertyHint.None, "")]
	public bool MixedProjectileDefinitions { get; set; }

	[Export(PropertyHint.None, "")]
	public int MinimumAnimationDefinitionCount { get; set; } = 1;

	public override void _Ready()
	{
		ApplyCommandLineArguments();
		_renderFrameTimes.Clear();
		_processTimesMs.Clear();
		_physicsFrameTimesMs.Clear();
		_physicsUpdateTimesMs.Clear();
		_spawnedIndices.Clear();
		_initialPositions.Clear();
		_spawnedAnimationDefinitionIds.Clear();
		Array.Clear(_workloadVariantSpawnCounts);
		_transitionConfigs.Clear();
		_transitionPreparedChanges.Clear();
		_burstTransitionExecuted = false;
		_measurementStarted = false;
		_firstPublicationMeasured = false;
		_firstPublicationMs = 0.0;
		_firstPublicationVisible = 0;
		_lastRenderTimestamp = Stopwatch.GetTimestamp();
		_originalMaxFps = Engine.MaxFps;
		_originalPhysicsTicks = Engine.PhysicsTicksPerSecond;
		_originalTimeScale = Engine.TimeScale;
		_originalPerfProfilerEnabled = TowerDefensePerfProfiler.Enabled;
		_originalDetailedHotPathMetrics = TowerDefensePerfProfiler.DetailedHotPathMetrics;
		_originalProfilerDumpIntervalFrames = TowerDefensePerfProfiler.DumpIntervalFrames;
		_originalProfilerMaxMetricsPerDump = TowerDefensePerfProfiler.MaxMetricsPerDump;
		if (EnableDetailedProfiler)
		{
			TowerDefensePerfProfiler.Enabled = true;
			TowerDefensePerfProfiler.DetailedHotPathMetrics = true;
			TowerDefensePerfProfiler.DumpIntervalFrames = 60;
			TowerDefensePerfProfiler.MaxMetricsPerDump = 48;
			TowerDefensePerfProfiler.Reset();
		}
		Engine.MaxFps = Math.Max(0, BenchmarkMaxFps);
		Engine.PhysicsTicksPerSecond = 60;
		Engine.TimeScale = 1.0;
		_manager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(_manager) || !GodotObject.IsInstanceValid(_manager.characterRegistry) || !GodotObject.IsInstanceValid(_manager.targetSystem))
		{
			GD.PrintErr("[BulletFieldWorkloadTest] TowerDefenseManager autoload is not ready.");
			GetTree().Quit(2);
			return;
		}
		_projectileUpdateManager = ProjectileUpdateManager.Instance;
		if (!GodotObject.IsInstanceValid(_projectileUpdateManager))
		{
			GD.PrintErr("[BulletFieldWorkloadTest] ProjectileUpdateManager autoload is not ready.");
			GetTree().Quit(2);
			return;
		}
		_originalProjectileManagerProcessMode = _projectileUpdateManager.ProcessMode;
		_projectileManagerProcessModeCaptured = true;
		_projectileUpdateManager.ProcessMode = ProcessModeEnum.Disabled;
		_originalGridSize = _manager.gridSize;
		_originalGridBegin = _manager.gridBeginPos;
		_originalGridNum = _manager.gridNum;
		Vector2 size = GetViewportRect().Size;
		float num = Math.Max(2f, (size.Y - 285f) / (float)Math.Max(1, ZBucketCount));
		_manager.gridSize = new Vector2(80f, num);
		_manager.gridBeginPos = new Vector2(0f, 285f);
		_manager.gridNum = new Vector2I(64, Math.Max(1, ZBucketCount));
		CreateBackgroundAndLabels();
		_bulletField = new BulletField
		{
			Name = "BulletFieldWorkload"
		};
		AddChild(_bulletField, forceReadableName: false, InternalMode.Disabled);
		LoadConfigsAndReferences();
		if (PrimeSpawnRoutes)
		{
			PrimeRoutes();
		}
		long timestamp = Stopwatch.GetTimestamp();
		SpawnWorkload(size, 285f, num);
		BuildTransitionConfigs();
		_spawnElapsedMs = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		_screenshotSaved = string.IsNullOrWhiteSpace(ScreenshotPath);
		_ready = _spawnFailures == 0 && _bulletField.ActiveCount == SpawnCount && _configs.Count == 6 && _spawnedAnimationDefinitionIds.Count >= MinimumAnimationDefinitionCount && CountActiveWorkloadVariants() == 6;
		GD.Print($"[BulletFieldWorkloadTest] ready spawn={_bulletField.ActiveCount}/{SpawnCount} failures={_spawnFailures} variants={_configs.Count} requestedZBuckets={ZBucketCount} animationDefinitions={_spawnedAnimationDefinitionIds.Count}/{MinimumAnimationDefinitionCount} mixedDefinitions={MixedProjectileDefinitions} spawnMs={_spawnElapsedMs:F3} workloads=shooter,spin,gravity,fall,catapult,track");
		if (_ready)
		{
			StartContentionThreads();
		}
		else
		{
			GetTree().Quit(2);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_ready || _finished)
		{
			return;
		}
		if (!string.IsNullOrEmpty(BurstTransitionMode) && !_burstTransitionExecuted && _elapsed >= WarmupSeconds)
		{
			RunBurstTransition(delta);
			return;
		}
		bool num = _elapsed >= WarmupSeconds;
		if (num)
		{
			StartMeasurementTelemetry();
		}
		long timestamp = Stopwatch.GetTimestamp();
		_bulletField.Update(delta, Engine.GetPhysicsFrames());
		double num2 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		if (!_firstPublicationMeasured)
		{
			_firstPublicationMeasured = true;
			_firstPublicationMs = num2;
			_firstPublicationVisible = _bulletField.GetAnimatedMeshVisibleInstanceCountForTest();
		}
		if (num)
		{
			_physicsUpdateTimesMs.Add(num2);
		}
	}

	public override void _Process(double delta)
	{
		long timestamp = Stopwatch.GetTimestamp();
		double num = ((_lastRenderTimestamp > 0) ? ((double)(timestamp - _lastRenderTimestamp) / (double)Stopwatch.Frequency) : delta);
		_lastRenderTimestamp = timestamp;
		if (!_ready || _finished)
		{
			return;
		}
		_elapsed += delta;
		if (string.IsNullOrEmpty(BurstTransitionMode))
		{
			double num2 = Math.Max(0.5, WarmupSeconds - 0.35);
			if (!_screenshotSaved && _elapsed >= num2)
			{
				_screenshotSaved = CaptureScreenshot();
			}
			if (_elapsed >= WarmupSeconds)
			{
				StartMeasurementTelemetry();
				_measuredRenderSeconds += num;
				_renderFrameTimes.Add(num);
				_processTimesMs.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0);
				_physicsFrameTimesMs.Add(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0);
			}
			if (_measuredRenderSeconds >= MeasureSeconds)
			{
				FinishTest();
			}
		}
	}

	private void StartMeasurementTelemetry()
	{
		if (!_measurementStarted)
		{
			_measurementStarted = true;
			_measurementThreadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			_measurementTotalAllocatedBefore = GC.GetTotalAllocatedBytes();
			_measurementGen0Before = GC.CollectionCount(0);
			_measurementGen1Before = GC.CollectionCount(1);
			_measurementGen2Before = GC.CollectionCount(2);
		}
	}

	private void LoadConfigsAndReferences()
	{
		List<PackedScene> list = ResolveCompactProjectileScenes();
		if (list.Count == 0)
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			PackedScene packedScene = list[i % list.Count];
			TowerDefenseProjectileConfig item = new TowerDefenseProjectileConfig
			{
				name = $"BulletFieldWorkload{i}",
				projectileScene = packedScene,
				scale = Vector2.One * BulletScale,
				size = new Vector2(28f, 28f),
				collisionFlags = 0,
				fireMethodFlags = 1,
				rotateScale = ((i % 2 == 0) ? 4.0 : 0.0),
				trackSearchInterval = 15
			};
			_configs.Add(item);
			if (packedScene.Instantiate(PackedScene.GenEditState.Disabled) is AdobeAnimateSprite adobeAnimateSprite)
			{
				AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
				adobeAnimateSprite.SetClip(adobeAnimateSprite.clip);
				adobeAnimateSprite.pause = true;
				adobeAnimateSprite.frameIndex = adobeAnimateSprite.clipRange.X + Math.Min(2, Math.Max(0, adobeAnimateSprite.clipRange.Y - adobeAnimateSprite.clipRange.X - 1));
				adobeAnimateSprite.elapsedTimer = 0.25;
				adobeAnimateSprite.Position = new Vector2(125f + (float)i * 165f, 130f);
				adobeAnimateSprite.Scale = Vector2.One * 0.75f;
				adobeAnimateSprite.ZAsRelative = false;
				adobeAnimateSprite.ZIndex = 4000;
				if (i < list.Count)
				{
					GD.Print($"[BulletFieldWorkloadTest] compactTemplate index={i} " + AnimateMultiMeshRenderer.DescribeCompactCompatibility(adobeAnimateSprite.flashAnimeData));
				}
			}
		}
	}

	private List<PackedScene> ResolveCompactProjectileScenes()
	{
		List<PackedScene> list = new List<PackedScene>();
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < CompactProjectileSceneCandidates.Length; i++)
		{
			string text = CompactProjectileSceneCandidates[i];
			PackedScene packedScene = GD.Load<PackedScene>(text);
			if (packedScene != null && ProbeCompactScene(packedScene, text, out var definitionId) && hashSet.Add(definitionId))
			{
				list.Add(packedScene);
				GD.Print($"[BulletFieldWorkloadTest] compactTemplate selected={text} definition={definitionId}");
				if (!MixedProjectileDefinitions)
				{
					break;
				}
			}
		}
		if (list.Count < MinimumAnimationDefinitionCount)
		{
			GD.PrintErr($"[BulletFieldWorkloadTest] only {list.Count} unique compact projectile definitions were found; required {MinimumAnimationDefinitionCount}.");
		}
		return list;
	}

	private bool ProbeCompactScene(PackedScene scene, string path, out int definitionId)
	{
		definitionId = -1;
		TowerDefenseProjectileConfig config = new TowerDefenseProjectileConfig
		{
			name = "BulletFieldCompactProbe:" + path,
			projectileScene = scene,
			scale = Vector2.One * BulletScale,
			size = new Vector2(28f, 28f),
			collisionFlags = 0,
			fireMethodFlags = 1
		};
		int num = _bulletField.TrySpawnFromConfig(config, Vector2.Zero, Vector2.Right, 1.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2I.One, 1, new Rect2(-1000f, -1000f, 2000f, 2000f), null, 0.0, 0.0, 0);
		if (num < 0)
		{
			return false;
		}
		ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(num);
		bool flag = bulletDataRef.renderMode == BulletRenderMode.ANIMATED_MESH && bulletDataRef.animDefId >= 0;
		definitionId = (flag ? bulletDataRef.animDefId : (-1));
		_bulletField.Despawn(num);
		GD.Print($"[BulletFieldWorkloadTest] compactProbe path={path} compatible={flag} definition={definitionId}");
		return flag;
	}

	private void SpawnWorkload(Vector2 viewportSize, float stressTop, float rowHeight)
	{
		if (_configs.Count == 0)
		{
			_spawnFailures = SpawnCount;
			return;
		}
		Rect2 rect = new Rect2(new Vector2(-2000000f, -2000000f), new Vector2(4000000f, 4000000f));
		float num = Math.Max(1f, viewportSize.X - 40f);
		for (int i = 0; i < SpawnCount; i++)
		{
			int num2 = 1 + i % Math.Max(1, ZBucketCount);
			float num3 = stressTop + ((float)num2 - 0.5f) * rowHeight;
			float num4 = 20f + (float)(i * 73 % Math.Max(1, (int)num));
			float y = 0f;
			Vector2 vector = new Vector2(24f + (float)(i % 13), y);
			Vector2 pos = new Vector2(num4, num3);
			Vector2I gridPos = new Vector2I(Math.Max(1, Mathf.FloorToInt(num4 / 80f) + 1), num2);
			int num5 = i % 6;
			int index = (MixedProjectileDefinitions ? (i / 6 % _configs.Count) : num5);
			TowerDefenseProjectileConfig config = _configs[index];
			int num6 = _bulletField.TrySpawnFromConfig(config, pos, vector, vector.Length(), null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, gridPos, num2, rect);
			if (num6 < 0)
			{
				_spawnFailures++;
				continue;
			}
			_spawnedIndices.Add(num6);
			ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(num6);
			if (bulletDataRef.renderMode != BulletRenderMode.ANIMATED_MESH || bulletDataRef.animDefId < 0)
			{
				_spawnFailures++;
				continue;
			}
			ConfigureWorkloadBullet(ref bulletDataRef, i, num3, vector, rect);
			_spawnedAnimationDefinitionIds.Add(bulletDataRef.animDefId);
			_workloadVariantSpawnCounts[num5]++;
			_initialPositions.Add(bulletDataRef.pos);
		}
	}

	private int CountActiveWorkloadVariants()
	{
		int num = 0;
		for (int i = 0; i < _workloadVariantSpawnCounts.Length; i++)
		{
			if (_workloadVariantSpawnCounts[i] > 0)
			{
				num++;
			}
		}
		return num;
	}

	private void BuildTransitionConfigs()
	{
		if (_configs.Count == 0)
		{
			return;
		}
		for (int i = 0; i < _spawnedIndices.Count; i++)
		{
			ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(_spawnedIndices[i]);
			if (!_transitionConfigs.ContainsKey(bulletDataRef.fireMethodFlags))
			{
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = (TowerDefenseProjectileConfig)_configs[0].Duplicate(deep: true);
				towerDefenseProjectileConfig.name = $"BulletFieldTransition{bulletDataRef.fireMethodFlags}";
				towerDefenseProjectileConfig.fireMethodFlags = bulletDataRef.fireMethodFlags;
				towerDefenseProjectileConfig.baseDamage += 1.0;
				_transitionConfigs.Add(bulletDataRef.fireMethodFlags, towerDefenseProjectileConfig);
				if (_bulletField.TryPrepareProjectileChange(towerDefenseProjectileConfig, out var prepared))
				{
					_transitionPreparedChanges.Add(bulletDataRef.fireMethodFlags, prepared);
				}
			}
		}
	}

	private void RunBurstTransition(double delta)
	{
		_burstTransitionExecuted = true;
		int num = 0;
		long timestamp = Stopwatch.GetTimestamp();
		if (BurstTransitionMode.Equals("despawn", StringComparison.OrdinalIgnoreCase))
		{
			for (int i = 0; i < _spawnedIndices.Count; i++)
			{
				int index = _spawnedIndices[i];
				if (_bulletField.IsBulletActive(index))
				{
					_bulletField.Despawn(index);
					num++;
				}
			}
		}
		else
		{
			if (!BurstTransitionMode.Equals("change", StringComparison.OrdinalIgnoreCase))
			{
				GD.PrintErr("[BulletFieldBurstTransitionResult] unsupported mode=" + BurstTransitionMode);
				GetTree().Quit(2);
				return;
			}
			for (int j = 0; j < _spawnedIndices.Count; j++)
			{
				int num2 = _spawnedIndices[j];
				if (_bulletField.IsBulletActive(num2))
				{
					ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(num2);
					if (_transitionConfigs.TryGetValue(bulletDataRef.fireMethodFlags, out var value) && _transitionPreparedChanges.TryGetValue(bulletDataRef.fireMethodFlags, out var value2) && _bulletField.ChangeBulletDataPrepared(num2, value, null, in value2, value.baseDamage) == num2)
					{
						num++;
					}
				}
			}
		}
		double value3 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		long timestamp2 = Stopwatch.GetTimestamp();
		_bulletField.Update(delta, Engine.GetPhysicsFrames());
		double value4 = (double)(Stopwatch.GetTimestamp() - timestamp2) * 1000.0 / (double)Stopwatch.Frequency;
		double num3 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		int num4 = ((!BurstTransitionMode.Equals("despawn", StringComparison.OrdinalIgnoreCase)) ? SpawnCount : 0);
		int num5 = num4;
		int animatedMeshVisibleInstanceCountForTest = _bulletField.GetAnimatedMeshVisibleInstanceCountForTest();
		bool flag = num == SpawnCount && _bulletField.ActiveCount == num4 && animatedMeshVisibleInstanceCountForTest == num5 && num3 <= BurstTransitionBudgetMilliseconds;
		GD.Print($"[BulletFieldBurstTransitionResult] mode={BurstTransitionMode} requested={SpawnCount} succeeded={num} active={_bulletField.ActiveCount}/{num4} visible={animatedMeshVisibleInstanceCountForTest}/{num5} transitionMs={value3:F3} updateMs={value4:F3} elapsedMs={num3:F3}/{BurstTransitionBudgetMilliseconds:F3} passed={flag}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void PrimeRoutes()
	{
		Rect2 rect = new Rect2(-1000f, -1000f, 2000f, 2000f);
		for (int i = 0; i < _configs.Count; i++)
		{
			int num = _bulletField.TrySpawnFromConfig(_configs[i], Vector2.Zero, Vector2.Right, 1.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2I.One, 1, rect, null, 0.0, 0.0, 0);
			if (num >= 0)
			{
				_bulletField.Despawn(num);
			}
		}
	}

	private static void ConfigureWorkloadBullet(ref BulletData bullet, int index, float visualY, Vector2 velocity, Rect2 worldRect)
	{
		bullet.randFreshIndex = index % 5;
		bullet.rect = worldRect;
		bullet.fireLength = -1;
		bullet.lockGridY = true;
		bullet.flipX = (index & 1) != 0;
		BulletField.SetSpriteRotation(ref bullet, ((index & 0xF) == 0) ? Mathf.DegToRad((float)(index % 5 - 2) * 15f) : 0f);
		if (bullet.animFrameMax > 0)
		{
			bullet.animElapsedTimer = (float)((double)index * 0.61803398875 % (double)bullet.animFrameMax);
		}
		int fireMethodFlags = 1;
		int fireMethodFlags2 = 32;
		switch (index % 6)
		{
		case 0:
			bullet.fireMethodFlags = fireMethodFlags;
			bullet.rotateScale = 0f;
			break;
		case 1:
			bullet.fireMethodFlags = fireMethodFlags;
			bullet.rotateScale = 5f + (float)(index % 4);
			break;
		case 2:
			bullet.fireMethodFlags = fireMethodFlags;
			bullet.useGravity = true;
			bullet.gravity = 20.0;
			bullet.gravityScale = 1f;
			bullet.z = 1000000.0 + (double)(index % 30);
			bullet.ySpeed = -10.0;
			bullet.pos.Y = visualY + (float)bullet.z - 20f;
			bullet.rotateFollowVelocity = true;
			bullet.collisionEnabled = false;
			break;
		case 3:
			bullet.fireMethodFlags = fireMethodFlags;
			bullet.useFall = true;
			bullet.gravity = 20.0;
			bullet.gravityScale = 1f;
			bullet.z = 1000000.0 + (double)(index % 30);
			bullet.ySpeed = -8.0;
			bullet.pos.Y = visualY + (float)bullet.z - 20f;
			bullet.rotateFollowVelocity = true;
			bullet.collisionEnabled = false;
			break;
		case 4:
			bullet.fireMethodFlags = fireMethodFlags;
			bullet.catapultOpen = true;
			bullet.z = 1000000.0;
			bullet.ySpeed = -10000.0;
			bullet.pos.Y = visualY + (float)bullet.z;
			bullet.rotateFollowVelocity = true;
			bullet.collisionEnabled = false;
			break;
		default:
			bullet.fireMethodFlags = fireMethodFlags2;
			bullet.trackOpen = true;
			bullet.trackSearchInterval = 15;
			bullet.trackNoTargetInterval = 60;
			bullet.speed = velocity.Length();
			bullet.rotation = velocity.Angle();
			bullet.checkAll = true;
			break;
		}
	}

	private void CreateBackgroundAndLabels()
	{
		Vector2 size = GetViewportRect().Size;
		ColorRect node = new ColorRect
		{
			Color = new Color(0.025f, 0.055f, 0.04f),
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZAsRelative = false,
			ZIndex = -4096
		};
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		AddLabel("Full Sprite references", new Vector2(18f, 18f));
		AddLabel($"BulletField real workload: {SpawnCount} bullets / {ZBucketCount} requested Z buckets", new Vector2(18f, 245f));
	}

	private void AddLabel(string text, Vector2 position)
	{
		Label label = new Label
		{
			Text = text,
			Position = position,
			ZAsRelative = false,
			ZIndex = 4096
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		AddChild(label, forceReadableName: false, InternalMode.Disabled);
	}

	private bool CaptureScreenshot()
	{
		Image image = GetViewport().GetTexture().GetImage();
		if (image == null || image.IsEmpty())
		{
			return false;
		}
		string text = ScreenshotPath.Replace('\\', '/');
		string text2 = (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(text) : text);
		Error error = DirAccess.MakeDirRecursiveAbsolute(text2.GetBaseDir());
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			return false;
		}
		Error num = image.SavePng(text2);
		if (num == Error.Ok)
		{
			GD.Print($"[BulletFieldWorkloadTest] screenshot saved path={text} size={image.GetWidth()}x{image.GetHeight()}");
		}
		return num == Error.Ok;
	}

	private void FinishTest()
	{
		_finished = true;
		StopContentionThreads();
		long num = (_measurementStarted ? (GC.GetAllocatedBytesForCurrentThread() - _measurementThreadAllocatedBefore) : 0);
		long num2 = (_measurementStarted ? (GC.GetTotalAllocatedBytes() - _measurementTotalAllocatedBefore) : 0);
		int value = (_measurementStarted ? (GC.CollectionCount(0) - _measurementGen0Before) : 0);
		int value2 = (_measurementStarted ? (GC.CollectionCount(1) - _measurementGen1Before) : 0);
		int value3 = (_measurementStarted ? (GC.CollectionCount(2) - _measurementGen2Before) : 0);
		double[] array = _renderFrameTimes.ToArray();
		double[] array2 = _processTimesMs.ToArray();
		double[] array3 = _physicsFrameTimesMs.ToArray();
		double[] array4 = _physicsUpdateTimesMs.ToArray();
		double num3 = (double)_renderFrameTimes.Count / Math.Max(0.0001, _measuredRenderSeconds);
		double num4 = BenchmarkStatistics.WorstFractionFps(array, array.Length, 0.01);
		double num5 = BenchmarkStatistics.WorstFractionFps(array, array.Length, 0.001);
		double num6 = BenchmarkStatistics.Percentile(array, array.Length, 50.0) * 1000.0;
		double num7 = BenchmarkStatistics.Percentile(array, array.Length, 95.0) * 1000.0;
		double num8 = BenchmarkStatistics.Percentile(array, array.Length, 99.0) * 1000.0;
		double num9 = BenchmarkStatistics.Maximum(array, array.Length) * 1000.0;
		double value4 = BenchmarkStatistics.Percentile(array2, array2.Length, 50.0);
		double value5 = BenchmarkStatistics.Percentile(array2, array2.Length, 95.0);
		double value6 = BenchmarkStatistics.Percentile(array2, array2.Length, 99.0);
		double value7 = BenchmarkStatistics.Maximum(array2, array2.Length);
		double value8 = BenchmarkStatistics.Percentile(array3, array3.Length, 50.0);
		double value9 = BenchmarkStatistics.Percentile(array3, array3.Length, 95.0);
		double value10 = BenchmarkStatistics.Percentile(array3, array3.Length, 99.0);
		double value11 = BenchmarkStatistics.Maximum(array3, array3.Length);
		double value12 = BenchmarkStatistics.Average(array4, array4.Length);
		double value13 = BenchmarkStatistics.Percentile(array4, array4.Length, 50.0);
		double value14 = BenchmarkStatistics.Percentile(array4, array4.Length, 95.0);
		double value15 = BenchmarkStatistics.Percentile(array4, array4.Length, 99.0);
		double value16 = BenchmarkStatistics.Maximum(array4, array4.Length);
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
		double monitor12 = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
		double monitor13 = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot statistics = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
		bool flag = statistics.AppliedUploadCount > 0 && statistics.UploadedByteCount > 0 && statistics.FailedBatchCount == 0L && statistics.BufferUpdateFailureCount == 0L && statistics.InvalidTargetCount == 0L && statistics.RenderingDeviceUnavailableCount == 0;
		StringName value17 = RenderingServer.GetCurrentRenderingDriverName();
		StringName value18 = RenderingServer.GetCurrentRenderingMethod();
		int animatedMeshVisibleInstanceCountForTest = _bulletField.GetAnimatedMeshVisibleInstanceCountForTest();
		int animatedMeshBucketCountForTest = _bulletField.GetAnimatedMeshBucketCountForTest();
		int num10 = CountActiveWorkloadVariants();
		long num11 = 0L;
		for (int i = 0; i < _contentionIterations.Length; i++)
		{
			num11 += _contentionIterations[i];
		}
		int num12 = 0;
		int num13 = Math.Min(_spawnedIndices.Count, _initialPositions.Count);
		for (int j = 0; j < num13; j++)
		{
			int index = _spawnedIndices[j];
			if (_bulletField.IsBulletActive(index) && _bulletField.GetBulletDataRef(index).pos.DistanceSquaredTo(_initialPositions[j]) > 0.0001f)
			{
				num12++;
			}
		}
		double num14 = ((MinimumAverageFps >= 0.0) ? MinimumAverageFps : ((SpawnCount >= 60000) ? 59.0 : ((SpawnCount <= 10000) ? 175.0 : 0.0)));
		double num15 = Math.Max(0.0, MinimumOnePercentLowFps);
		double num16 = Math.Max(0.0, MinimumZeroPointOnePercentLowFps);
		double num17 = MaxPrimedSpawnMilliseconds * Math.Max(1.0, (double)SpawnCount / 10000.0);
		bool flag2 = !PrimeSpawnRoutes || _spawnElapsedMs <= num17;
		bool flag3 = _measurementStarted && array.Length != 0 && array2.Length == array.Length && array3.Length == array.Length && array4.Length != 0 && double.IsFinite(num6) && double.IsFinite(num7) && double.IsFinite(num8) && double.IsFinite(num9) && num >= 0 && num2 >= 0;
		bool flag4 = _screenshotSaved && _spawnFailures == 0 && _bulletField.ActiveCount == SpawnCount && animatedMeshVisibleInstanceCountForTest == SpawnCount && num12 == SpawnCount && _spawnedAnimationDefinitionIds.Count >= MinimumAnimationDefinitionCount && num10 == 6 && _firstPublicationMeasured && _firstPublicationVisible == SpawnCount && animatedMeshBucketCountForTest > 0 && flag && flag2 && flag3 && num3 >= num14 && num4 >= num15 && num5 >= num16;
		GD.Print($"[BulletFieldWorkloadTestResult] runtimeProfile={RuntimeProfile} spawn={SpawnCount} active={_bulletField.ActiveCount} visible={animatedMeshVisibleInstanceCountForTest}/{SpawnCount} moved={num12}/{SpawnCount} buckets={animatedMeshBucketCountForTest} renderFrames={_renderFrameTimes.Count} animationDefinitions={_spawnedAnimationDefinitionIds.Count}/{MinimumAnimationDefinitionCount} mixedDefinitions={MixedProjectileDefinitions} firingForms={num10}/{6} variantCounts={string.Join(',', _workloadVariantSpawnCounts)} workers={_bulletField.GetBackgroundWorkerCountForTest()} contentionThreads={BackgroundContentionThreads} contentionIterations={num11} maxFps={BenchmarkMaxFps} minimumAverageFps={num14:F2} minimumOnePercentLowFps={num15:F2} minimumZeroPointOnePercentLowFps={num16:F2} averageFps={num3:F2} onePercentLowFps={num4:F2} zeroPointOnePercentLowFps={num5:F2} frameP50Ms={num6:F3} frameP95Ms={num7:F3} frameP99Ms={num8:F3} frameMaxMs={num9:F3} processP50Ms={value4:F3} processP95Ms={value5:F3} processP99Ms={value6:F3} processMaxMs={value7:F3} physicsP50Ms={value8:F3} physicsP95Ms={value9:F3} physicsP99Ms={value10:F3} physicsMaxMs={value11:F3} spawnMs={_spawnElapsedMs:F3}/{num17:F3} spawnPassed={flag2} firstPublicationMs={_firstPublicationMs:F3} firstPublicationVisible={_firstPublicationVisible} fieldPhysicsAverageMs={value12:F3} fieldPhysicsP50Ms={value13:F3} fieldPhysicsP95Ms={value14:F3} fieldPhysicsP99Ms={value15:F3} fieldPhysicsMaxMs={value16:F3} staticMemoryBytes={monitor:F0} staticMemoryPeakBytes={monitor2:F0} messageBufferPeakBytes={monitor3:F0} videoMemoryBytes={monitor4:F0} textureMemoryBytes={monitor5:F0} bufferMemoryBytes={monitor6:F0} objectCount={monitor7:F0} nodeCount={monitor8:F0} orphanNodeCount={monitor9:F0} resourceCount={monitor10:F0} drawCalls={monitor11:F0} renderObjects={monitor12:F0} renderPrimitives={monitor13:F0} rdBatches={statistics.AppliedBatchCount}/{statistics.SubmittedBatchCount} rdUploads={statistics.AppliedUploadCount}/{statistics.QueuedUploadCount} rdUploadedBytes={statistics.UploadedByteCount} rdRidRefreshes={statistics.BufferRidRefreshCount} rdFailures={statistics.FailedBatchCount}/{statistics.BufferUpdateFailureCount}/{statistics.InvalidTargetCount}/{statistics.RenderingDeviceUnavailableCount} rdStaleDrops={statistics.StaleGenerationDropCount} rdQueueDepth={statistics.CurrentQueueDepth}/{statistics.MaximumQueueDepth} rdUploadPassed={flag} threadAllocatedBytes={num} totalAllocatedBytes={num2} gen0={value} gen1={value2} gen2={value3} telemetryPassed={flag3} driver={value17} renderer={value18} screenshotSaved={_screenshotSaved} failures={_spawnFailures} passed={flag4}");
		GetTree().Quit((!flag4) ? 1 : 0);
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value2;
			double value3;
			double value4;
			int value5;
			double value6;
			int value7;
			double value8;
			double value9;
			double value10;
			int value11;
			if (TryReadInt(text, "--bullet-workload-spawn=", out var value))
			{
				SpawnCount = Math.Max(0, value);
			}
			else if (TryReadInt(text, "--bullet-workload-z-buckets=", out value2))
			{
				ZBucketCount = Math.Max(1, value2);
			}
			else if (TryReadDouble(text, "--bullet-workload-warmup=", out value3))
			{
				WarmupSeconds = Math.Max(0.0, value3);
			}
			else if (TryReadDouble(text, "--bullet-workload-measure=", out value4))
			{
				MeasureSeconds = Math.Max(0.1, value4);
			}
			else if (text.StartsWith("--runtime-profile=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--runtime-profile=".Length;
				RuntimeProfile = text2.Substring(length, text2.Length - length).Trim();
			}
			else if (text.StartsWith("--bullet-workload-screenshot=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--bullet-workload-screenshot=".Length;
				ScreenshotPath = text2.Substring(length, text2.Length - length);
			}
			else if (text.Equals("--bullet-workload-prime-routes", StringComparison.OrdinalIgnoreCase))
			{
				PrimeSpawnRoutes = true;
			}
			else if (text.Equals("--bullet-workload-detailed-profile", StringComparison.OrdinalIgnoreCase))
			{
				EnableDetailedProfiler = true;
			}
			else if (text.Equals("--bullet-workload-mixed-definitions", StringComparison.OrdinalIgnoreCase))
			{
				MixedProjectileDefinitions = true;
			}
			else if (TryReadInt(text, "--bullet-workload-minimum-animation-definitions=", out value5))
			{
				MinimumAnimationDefinitionCount = Math.Max(1, value5);
			}
			else if (text.StartsWith("--bullet-workload-transition=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--bullet-workload-transition=".Length;
				BurstTransitionMode = text2.Substring(length, text2.Length - length).Trim();
			}
			else if (TryReadDouble(text, "--bullet-workload-transition-budget-ms=", out value6))
			{
				BurstTransitionBudgetMilliseconds = Math.Max(0.1, value6);
			}
			else if (TryReadInt(text, "--bullet-workload-max-fps=", out value7))
			{
				BenchmarkMaxFps = Math.Max(0, value7);
			}
			else if (TryReadDouble(text, "--bullet-workload-minimum-fps=", out value8))
			{
				MinimumAverageFps = Math.Max(0.0, value8);
			}
			else if (TryReadDouble(text, "--bullet-workload-minimum-1pct-low=", out value9))
			{
				MinimumOnePercentLowFps = Math.Max(0.0, value9);
			}
			else if (TryReadDouble(text, "--bullet-workload-minimum-0.1pct-low=", out value10))
			{
				MinimumZeroPointOnePercentLowFps = Math.Max(0.0, value10);
			}
			else if (TryReadInt(text, "--bullet-workload-contention-threads=", out value11))
			{
				BackgroundContentionThreads = Math.Clamp(value11, 0, 32);
			}
		}
		if (MixedProjectileDefinitions)
		{
			MinimumAnimationDefinitionCount = Math.Max(3, MinimumAnimationDefinitionCount);
		}
	}

	private void StartContentionThreads()
	{
		if (BackgroundContentionThreads <= 0)
		{
			return;
		}
		Volatile.Write(ref _stopContentionThreads, 0);
		_contentionThreads = new Thread[BackgroundContentionThreads];
		_contentionIterations = new long[BackgroundContentionThreads];
		for (int i = 0; i < _contentionThreads.Length; i++)
		{
			int workerIndex = i;
			Thread thread = new Thread((ThreadStart)delegate
			{
				RunContentionWorker(workerIndex);
			})
			{
				IsBackground = true,
				Name = $"BulletFieldContention{workerIndex + 1}"
			};
			_contentionThreads[i] = thread;
			thread.Start();
		}
	}

	private void RunContentionWorker(int workerIndex)
	{
		double num = (double)workerIndex + 1.0;
		long num2 = 0L;
		while (Volatile.Read(in _stopContentionThreads) == 0)
		{
			for (int i = 0; i < 256; i++)
			{
				num = num * 1.0000001192092896 + 9.536743164E-07;
			}
			num2++;
			Thread.SpinWait(32);
		}
		_contentionIterations[workerIndex] = num2;
		GC.KeepAlive(num);
	}

	private void StopContentionThreads()
	{
		if (_contentionThreads.Length != 0)
		{
			Volatile.Write(ref _stopContentionThreads, 1);
			for (int i = 0; i < _contentionThreads.Length; i++)
			{
				_contentionThreads[i]?.Join();
			}
			_contentionThreads = Array.Empty<Thread>();
		}
	}

	private static bool TryReadInt(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(arg.Substring(length, arg.Length - length), out value);
		}
		return false;
	}

	private static bool TryReadDouble(string arg, string prefix, out double value)
	{
		value = 0.0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return double.TryParse(arg.Substring(length, arg.Length - length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}

	public override void _ExitTree()
	{
		StopContentionThreads();
		if (GodotObject.IsInstanceValid(_manager))
		{
			_manager.gridSize = _originalGridSize;
			_manager.gridBeginPos = _originalGridBegin;
			_manager.gridNum = _originalGridNum;
		}
		Engine.MaxFps = _originalMaxFps;
		Engine.PhysicsTicksPerSecond = _originalPhysicsTicks;
		Engine.TimeScale = _originalTimeScale;
		if (_projectileManagerProcessModeCaptured && GodotObject.IsInstanceValid(_projectileUpdateManager))
		{
			_projectileUpdateManager.ProcessMode = _originalProjectileManagerProcessMode;
		}
		TowerDefensePerfProfiler.Enabled = _originalPerfProfilerEnabled;
		TowerDefensePerfProfiler.DetailedHotPathMetrics = _originalDetailedHotPathMetrics;
		TowerDefensePerfProfiler.DumpIntervalFrames = _originalProfilerDumpIntervalFrames;
		TowerDefensePerfProfiler.MaxMetricsPerDump = _originalProfilerMaxMetricsPerDump;
		TowerDefensePerfProfiler.Reset();
		base._ExitTree();
	}
}
