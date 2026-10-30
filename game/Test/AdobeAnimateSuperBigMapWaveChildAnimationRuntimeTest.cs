using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed partial class AdobeAnimateSuperBigMapWaveChildAnimationRuntimeTest : Node
{
	private sealed class ZombieSceneProfile
	{
		public string ScenePath = string.Empty;

		public PackedScene Scene;

		public int RenderRootCount;

		public int OwnerCount;

		public int RenderSlotCount;

		public int SpriteCount;
	}

	private sealed class SpawnedZombie
	{
		public TowerDefenseZombie Zombie;

		public Node2D Holder;

		public int SpriteCount;

		public int RenderRootCount;

		public AdobeAnimateSprite MainSprite;

		public int BaselinePixels;

		public int ConsecutiveBlankFrames;

		public bool BlankDiagnosticPrinted;

		public bool CullingControl;

		public string Phase = string.Empty;

		public int PhaseIndex;

		public bool ResumePlaybackRequired;

		public bool ResumePlaybackAdvanced;

		public int ResumePlaybackStartFrame;

		public double ResumePlaybackStartElapsed;

		public bool FormalDeathRequested;

		public bool FormalDeathAnimationStarted;

		public bool FormalDeathAnimationProgressed;

		public bool FormalDeathFadeObserved;
	}

	private const string ResultMarker = "ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_CHILD_ANIMATION_RESULT";

	private const string SuperBigMapSurvivalPoolPath = "res://Asset/Config/Survival/Config/FrontlawnSuperBig/FrontlawnSuperBigEndlessNormal.json";

	private const string CharacterResourcePath = "res://Asset/Config/Character/CharacterResource.json";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const int ViewportWidth = 1200;

	private const int ViewportHeight = 700;

	private const int MapColumns = 27;

	private const int MapRows = 15;

	private const int BaselineCharacterCount = 240;

	private const int BigWaveCharacterCount = 400;

	private const int FinalWaveCharacterCount = 560;

	private const int SpawnBatchSize = 16;

	private const int ObservationFrameCount = 90;

	private const int PausedWaveObservationFrameCount = 12;

	private const int FormalDeathCountPerWave = 96;

	private const double FormalDeathAnimationTimeScale = 4.0;

	private const double FormalDeathFadeDurationSeconds = 0.35;

	private const int FormalDeathCompletionFrameLimit = 240;

	private const int MutationCharacterCount = 24;

	private static readonly StringName ReplacementMediaName = "Zombie_body.png";

	private static readonly StringName VisibilityLayerName = "Zombie_body";

	private const string ReplacementTexturePath = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png";

	private const int CameraMutationFrameInterval = 12;

	private const int MinimumComplexSceneCount = 12;

	private const int ContinuitySampleCountPerPhase = 4;

	private const int TotalContinuitySampleCount = 12;

	private const int ContinuityBlankFrameThreshold = 2;

	private const int ContinuityBaselineFrameLimit = 24;

	private const int PixelSampleStep = 4;

	private const float MassiveBlankRatio = 0.25f;

	private const float MapZoom = 0.48f;

	private const float CharacterScale = 0.55f;

	private static readonly Color BackgroundColor = new Color(0.013f, 0.017f, 0.023f);

	private readonly List<Node> _detachedAuditInstances = new List<Node>(200);

	private readonly List<PackedScene> _retainedScenes = new List<PackedScene>(200);

	private readonly List<ZombieSceneProfile> _complexProfiles = new List<ZombieSceneProfile>(64);

	private ZombieSceneProfile _baselineProfile;

	private readonly List<SpawnedZombie> _spawnedZombies = new List<SpawnedZombie>(1200);

	private readonly List<SpawnedZombie> _continuitySamples = new List<SpawnedZombie>(12);

	private readonly List<SpawnedZombie> _formalDeathEntries = new List<SpawnedZombie>(192);

	private readonly List<SpawnedZombie> _formalDeathInventory = new List<SpawnedZombie>(192);

	private readonly List<SpawnedZombie> _mutationEntries = new List<SpawnedZombie>(24);

	private Node2D _world;

	private Camera2D _camera;

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	private Texture2D _replacementTexture;

	private int _mutationFrame;

	private int _massiveBlankFrames;

	private int _crowdRootShortageFrames;

	private int _sampleBlankFrames;

	private int _pausedSpawnFrames;

	private int _pausedMassiveBlankFrames;

	private int _pausedSampleBlankFrames;

	private int _pausedCrowdRootShortageFrames;

	private int _resumePlaybackRequired;

	private int _resumePlaybackAdvanced;

	private int _maximumFallbackRoots;

	private int _minimumSignalPixels = 2147483647;

	private int _maximumStateTexels;

	private int _maximumStateCapacityTexels;

	private int _stateCapacityChanges;

	private int _lastStateCapacityTexels;

	private int _maximumGraphAtlasPages;

	private int _maximumGraphAtlasLayerCapacity;

	private int _graphAtlasCapacityChanges;

	private int _lastGraphAtlasLayerCapacity;

	private int _formalDeathAnimationsStarted;

	private int _formalDeathAnimationsProgressed;

	private int _formalDeathFadesObserved;

	private int _formalDeathChildAnimationEntries;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<string> failures = new List<string>();
		try
		{
			_ = 16;
			try
			{
				PrepareRuntime();
				await BuildComplexZombieCatalog(failures);
				Require(_complexProfiles.Count >= 12, $"complex zombie catalog incomplete profiles={_complexProfiles.Count} minimum={12}", failures);
				Require(_baselineProfile != null && GodotObject.IsInstanceValid(_baselineProfile.Scene), "normal zombie baseline scene is unavailable", failures);
				PrintComplexProfiles();
				await SpawnWave("baseline", 240, failures);
				PrepareFormalDeathInventory(failures);
				await WaitPhysicsFrames(6);
				int baselinePixels = await CaptureSignalPixels();
				Require(baselinePixels > 0, "baseline produced no visible animation pixels", failures);
				await CaptureContinuitySampleBaselines(failures);
				await ObservePhase("baseline", baselinePixels, 90, failures);
				PrepareMutationEntries(failures);
				BeginFormalDeathWave("big", failures);
				await SpawnWave("big", 400, failures, baselinePixels, pauseDuringSpawn: true);
				BeginResumePlaybackAudit("big", failures);
				await WaitPhysicsFrames(4);
				int bigWavePixels = await CaptureSignalPixels();
				Require(bigWavePixels >= Math.Max(1, Mathf.RoundToInt((float)baselinePixels * 0.25f)), $"big wave stable pixels collapsed baseline={baselinePixels} current={bigWavePixels}", failures);
				await CaptureContinuitySampleBaselines(failures);
				await ObservePhase("big", bigWavePixels, 90, failures);
				CompleteResumePlaybackAudit("big", failures);
				BeginFormalDeathWave("final", failures);
				await SpawnWave("final", 560, failures, bigWavePixels, pauseDuringSpawn: true);
				BeginResumePlaybackAudit("final", failures);
				await WaitPhysicsFrames(4);
				int finalWavePixels = await CaptureSignalPixels();
				Require(finalWavePixels >= Math.Max(1, Mathf.RoundToInt((float)bigWavePixels * 0.25f)), $"final wave stable pixels collapsed big={bigWavePixels} current={finalWavePixels}", failures);
				await CaptureContinuitySampleBaselines(failures);
				await ObservePhase("final", finalWavePixels, 90, failures);
				CompleteResumePlaybackAudit("final", failures);
				await WaitForFormalDeathCompletion(finalWavePixels, failures);
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int num = 1200;
				int num2 = CountLiveCharacters();
				int value = CountExpectedRenderRoots();
				int value2 = CountExpectedSprites();
				int num3 = (GodotObject.IsInstanceValid(AdobeAnimateRuntimeManager.Instance) ? AdobeAnimateRuntimeManager.Instance.RegisteredSpriteCount : 0);
				Require(_spawnedZombies.Count == 1200, $"spawned zombie count mismatch actual={_spawnedZombies.Count} expected={1200}", failures);
				Require(num2 == num, $"live zombie count mismatch actual={num2} expected={num}", failures);
				Require(num3 >= num, $"registered sprite count is below spawned character count registered={num3} expectedAtLeast={num}", failures);
				Require(aggregateRenderStats.CrowdRoots >= num, $"final Crowd root count is below formal character count actual={aggregateRenderStats.CrowdRoots} expectedAtLeast={num}", failures);
				Require(aggregateRenderStats.FallbackRoots == 0 && _maximumFallbackRoots == 0, $"fallback roots observed final={aggregateRenderStats.FallbackRoots} maximum={_maximumFallbackRoots}", failures);
				Require(_massiveBlankFrames == 0, $"massive animation blank frames observed count={_massiveBlankFrames}", failures);
				Require(_crowdRootShortageFrames == 0, $"Crowd root shortage frames observed count={_crowdRootShortageFrames}", failures);
				Require(_sampleBlankFrames == 0, $"persistent complex zombie sample blank frames observed count={_sampleBlankFrames}", failures);
				Require(_pausedSpawnFrames == 84, $"paused spawn audit frame count mismatch actual={_pausedSpawnFrames}", failures);
				Require(_pausedMassiveBlankFrames == 0, $"paused massive animation blank frames observed count={_pausedMassiveBlankFrames}", failures);
				Require(_pausedSampleBlankFrames == 0, $"paused complex zombie sample blank frames observed count={_pausedSampleBlankFrames}", failures);
				Require(_pausedCrowdRootShortageFrames == 0, $"paused Crowd root publication shortages observed count={_pausedCrowdRootShortageFrames}", failures);
				Require(_resumePlaybackRequired == 960 && _resumePlaybackAdvanced == _resumePlaybackRequired, $"paused wave animations did not all resume actual={_resumePlaybackAdvanced}/{_resumePlaybackRequired} expected={960}", failures);
				Require(_maximumStateTexels > 16384, $"complex wave did not cross the multi-layer state boundary maxStateTexels={_maximumStateTexels}", failures);
				Require(_formalDeathEntries.Count == 192, $"formal death request count mismatch actual={_formalDeathEntries.Count} expected={192}", failures);
				Require(_formalDeathChildAnimationEntries == _formalDeathEntries.Count, $"formal death child-animation coverage mismatch actual={_formalDeathChildAnimationEntries} expected={_formalDeathEntries.Count}", failures);
				Require(_formalDeathAnimationsStarted == _formalDeathEntries.Count, $"formal death animations did not all start actual={_formalDeathAnimationsStarted} expected={_formalDeathEntries.Count}", failures);
				Require(_formalDeathAnimationsProgressed == _formalDeathEntries.Count, $"formal death animations did not all progress actual={_formalDeathAnimationsProgressed} expected={_formalDeathEntries.Count}", failures);
				Require(_formalDeathFadesObserved == _formalDeathEntries.Count, $"formal death fades were not all observed actual={_formalDeathFadesObserved} expected={_formalDeathEntries.Count}", failures);
				PrintFailures(failures);
				bool flag = failures.Count == 0;
				GD.Print($"{"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_CHILD_ANIMATION_RESULT"} passed={flag} profiles={_complexProfiles.Count} minimumProfiles={12} spawned={_spawnedZombies.Count} liveCharacters={num2}/{num} sprites={value2} registeredSprites={num3} renderRoots={aggregateRenderStats.CrowdRoots}/{value} baselinePixels={baselinePixels} bigWavePixels={bigWavePixels} finalWavePixels={finalWavePixels} minimumPixels={_minimumSignalPixels} massiveBlankFrames={_massiveBlankFrames} sampleBlankFrames={_sampleBlankFrames} crowdRootShortageFrames={_crowdRootShortageFrames} pausedSpawnFrames={_pausedSpawnFrames} pausedMassiveBlankFrames={_pausedMassiveBlankFrames} pausedSampleBlankFrames={_pausedSampleBlankFrames} pausedCrowdRootShortageFrames={_pausedCrowdRootShortageFrames} resumePlayback={_resumePlaybackAdvanced}/{_resumePlaybackRequired} formalDeaths={_formalDeathEntries.Count} deathChildAnimations={_formalDeathChildAnimationEntries} deathStarted={_formalDeathAnimationsStarted} deathProgressed={_formalDeathAnimationsProgressed} deathFades={_formalDeathFadesObserved} mutationFrames={_mutationFrame} stateTexels={aggregateRenderStats.CrowdStateTexels} maximumStateTexels={_maximumStateTexels} stateCapacity={aggregateRenderStats.CrowdStateCapacityTexels} maximumStateCapacity={_maximumStateCapacityTexels} capacityChanges={_stateCapacityChanges} graphAtlasPages={AdobeAnimateRenderManager.GpuRenderGraphAtlasPageCount} maximumGraphAtlasPages={_maximumGraphAtlasPages} graphAtlasLayerCapacity={AdobeAnimateRenderManager.GpuRenderGraphAtlasLayerCapacity} maximumGraphAtlasLayerCapacity={_maximumGraphAtlasLayerCapacity} graphAtlasCapacityChanges={_graphAtlasCapacityChanges} fallbackRoots={aggregateRenderStats.FallbackRoots} maximumFallback={_maximumFallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value3)
			{
				GD.PrintErr($"{"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_CHILD_ANIMATION_RESULT"} passed=False exception={value3}");
			}
		}
		finally
		{
			await RestoreRuntime();
		}
		GetTree().Quit(exitCode);
	}

	private void PrepareRuntime()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		if (!GodotObject.IsInstanceValid(Global.Instance))
		{
			throw new InvalidOperationException("Global autoload is unavailable.");
		}
		_originalBackend = Global.Instance.adobeAnimateRenderBackend;
		_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
		_originalMaxFps = Engine.MaxFps;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		AdobeAnimateRenderManager.RasterCompositeEnabled = false;
		Engine.MaxFps = 60;
		_replacementTexture = ResourceLoader.Load<Texture2D>("res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(_replacementTexture))
		{
			throw new InvalidOperationException("replacement texture is unavailable path=res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png");
		}
		CanvasLayer canvasLayer = new CanvasLayer
		{
			Name = "BackgroundLayer",
			Layer = -100,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
		canvasLayer.AddChild(new ColorRect
		{
			Name = "Background",
			Color = BackgroundColor,
			Position = Vector2.Zero,
			Size = new Vector2(1200f, 700f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, InternalMode.Disabled);
		_world = new Node2D
		{
			Name = "SuperBigMapWaveWorld",
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(_world, forceReadableName: false, InternalMode.Disabled);
		_camera = new Camera2D
		{
			Name = "SuperBigMapWaveCamera",
			Position = new Vector2(1175f, 600f),
			Zoom = Vector2.One * 0.48f,
			Enabled = true,
			ProcessMode = ProcessModeEnum.Always
		};
		_world.AddChild(_camera, forceReadableName: false, InternalMode.Disabled);
	}

	private async Task BuildComplexZombieCatalog(List<string> failures)
	{
		_baselineProfile = new ZombieSceneProfile
		{
			ScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn",
			Scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Reuse)
		};
		if (GodotObject.IsInstanceValid(_baselineProfile.Scene))
		{
			_retainedScenes.Add(_baselineProfile.Scene);
		}
		List<string> zombieNames = ReadSuperBigMapZombiePool(failures);
		System.Collections.Generic.Dictionary<string, string> characterScenesByPacket = ReadCharacterSceneReferences(failures);
		HashSet<string> addedSceneReferences = new HashSet<string>(StringComparer.Ordinal);
		List<ZombieSceneProfile> profiles = new List<ZombieSceneProfile>(zombieNames.Count);
		for (int sceneIndex = 0; sceneIndex < zombieNames.Count; sceneIndex++)
		{
			string text = zombieNames[sceneIndex];
			if (!characterScenesByPacket.TryGetValue(text, out var value))
			{
				failures.Add(text + ": CharacterResource packet mapping is missing");
			}
			else
			{
				if (!addedSceneReferences.Add(value))
				{
					continue;
				}
				PackedScene packedScene = (string.IsNullOrWhiteSpace(value) ? null : ResourceLoader.Load<PackedScene>(value, null, ResourceLoader.CacheMode.Reuse));
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					failures.Add(text + ": production character scene load failed");
					continue;
				}
				string resourcePath = packedScene.ResourcePath;
				_retainedScenes.Add(packedScene);
				Node node;
				try
				{
					node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				}
				catch (Exception ex)
				{
					failures.Add(resourcePath + ": instantiate failed: " + ex.Message);
					continue;
				}
				_detachedAuditInstances.Add(node);
				if (!(node is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie is TowerDefenseZombieBungiSpawn)
				{
					continue;
				}
				List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
				CollectSprites(node, list);
				for (int i = 0; i < list.Count; i++)
				{
					list[i].PrepareDetachedGpuGraphWarmup();
				}
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				bool flag = true;
				for (int j = 0; j < list.Count; j++)
				{
					AdobeAnimateSprite adobeAnimateSprite = list[j];
					if (!adobeAnimateSprite.IsRenderedByParentSpriteForRender() && !adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
					{
						num++;
						if (!AdobeAnimateGpuRenderGraphBuilder.TryBuild(adobeAnimateSprite, out var graph, out var ownerSprites, out var failureReason))
						{
							failures.Add($"{resourcePath}:{adobeAnimateSprite.GetPath()}: GPU Graph rejected: {failureReason}");
							flag = false;
							break;
						}
						num2 += ownerSprites.Length;
						num3 += graph.RenderSlots.Length;
					}
				}
				if (flag && num > 0 && num2 > num)
				{
					profiles.Add(new ZombieSceneProfile
					{
						ScenePath = resourcePath,
						Scene = packedScene,
						RenderRootCount = num,
						OwnerCount = num2,
						RenderSlotCount = num3,
						SpriteCount = list.Count
					});
				}
				if ((sceneIndex + 1) % 8 == 0)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
		}
		profiles.Sort(CompareProfiles);
		for (int k = 0; k < profiles.Count; k++)
		{
			_complexProfiles.Add(profiles[k]);
		}
		ReleaseDetachedAuditInstances();
		await WaitProcessFrames(2);
	}

	private static System.Collections.Generic.Dictionary<string, string> ReadCharacterSceneReferences(List<string> failures)
	{
		System.Collections.Generic.Dictionary<string, string> dictionary = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(json))
		{
			failures.Add("character resource JSON load failed path=res://Asset/Config/Character/CharacterResource.json");
			return dictionary;
		}
		Dictionary dictionary2 = json.Data.AsGodotDictionary();
		foreach (Variant key in dictionary2.Keys)
		{
			Dictionary dictionary3 = dictionary2[key].AsGodotDictionary();
			if (!dictionary3.ContainsKey("Scene") || !dictionary3.ContainsKey("Packet"))
			{
				continue;
			}
			string value = dictionary3["Scene"].AsString();
			if (string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			foreach (Variant key2 in dictionary3["Packet"].AsGodotDictionary().Keys)
			{
				string text = key2.AsString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					dictionary[text] = value;
				}
			}
		}
		return dictionary;
	}

	private static List<string> ReadSuperBigMapZombiePool(List<string> failures)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Survival/Config/FrontlawnSuperBig/FrontlawnSuperBigEndlessNormal.json", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(json))
		{
			failures.Add("super big map survival JSON load failed path=res://Asset/Config/Survival/Config/FrontlawnSuperBig/FrontlawnSuperBigEndlessNormal.json");
			return new List<string>();
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		if (!dictionary.ContainsKey("ZombiePoolSetting"))
		{
			failures.Add("super big map survival JSON has no ZombiePoolSetting");
			return new List<string>();
		}
		Dictionary dictionary2 = dictionary["ZombiePoolSetting"].AsGodotDictionary();
		if (dictionary2.ContainsKey("ZombiePoolBase"))
		{
			AddZombieNames(dictionary2["ZombiePoolBase"].AsGodotArray(), hashSet);
		}
		if (dictionary2.ContainsKey("ZombiePoolRoundAdd"))
		{
			Godot.Collections.Array array = dictionary2["ZombiePoolRoundAdd"].AsGodotArray();
			for (int i = 0; i < array.Count; i++)
			{
				Dictionary dictionary3 = array[i].AsGodotDictionary();
				if (dictionary3.ContainsKey("Zombie"))
				{
					AddZombieNames(dictionary3["Zombie"].AsGodotArray(), hashSet);
				}
			}
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	private static void AddZombieNames(Godot.Collections.Array values, HashSet<string> names)
	{
		for (int i = 0; i < values.Count; i++)
		{
			string text = values[i].AsString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				names.Add(text);
			}
		}
	}

	private static int CompareProfiles(ZombieSceneProfile left, ZombieSceneProfile right)
	{
		int num = right.OwnerCount.CompareTo(left.OwnerCount);
		if (num != 0)
		{
			return num;
		}
		int num2 = right.RenderSlotCount.CompareTo(left.RenderSlotCount);
		if (num2 != 0)
		{
			return num2;
		}
		return string.Compare(left.ScenePath, right.ScenePath, StringComparison.OrdinalIgnoreCase);
	}

	private void PrintComplexProfiles()
	{
		for (int i = 0; i < _complexProfiles.Count; i++)
		{
			ZombieSceneProfile zombieSceneProfile = _complexProfiles[i];
			GD.Print($"ADOBE_ANIMATE_COMPLEX_ZOMBIE_PROFILE rank={i + 1} owners={zombieSceneProfile.OwnerCount} slots={zombieSceneProfile.RenderSlotCount} sprites={zombieSceneProfile.SpriteCount} roots={zombieSceneProfile.RenderRootCount} path={zombieSceneProfile.ScenePath}");
		}
	}

	private async Task SpawnWave(string phase, int count, List<string> failures, int continuityPixels = 0, bool pauseDuringSpawn = false)
	{
		int startCount = _spawnedZombies.Count;
		int targetCount = startCount + count;
		SceneTree tree = GetTree();
		bool originalPaused = tree.Paused;
		int massiveBlankFramesBeforePause = _massiveBlankFrames;
		int sampleBlankFramesBeforePause = _sampleBlankFrames;
		if (pauseDuringSpawn)
		{
			tree.Paused = true;
		}
		try
		{
			while (_spawnedZombies.Count < targetCount)
			{
				int num = Math.Min(targetCount, _spawnedZombies.Count + 16);
				while (_spawnedZombies.Count < num)
				{
					if (!SpawnComplexZombie(_spawnedZombies.Count, _spawnedZombies.Count - startCount, phase, failures))
					{
						throw new InvalidOperationException(phase + " wave stopped because a formal zombie could not be created");
					}
				}
				await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
				if (continuityPixels > 0)
				{
					await AuditCurrentFrame(phase + "-spawn", continuityPixels);
				}
				if (pauseDuringSpawn)
				{
					AuditPausedCrowdPublication(phase);
				}
			}
			if (pauseDuringSpawn)
			{
				for (int frame = 0; frame < 12; frame++)
				{
					await AuditCurrentFrame(phase + "-paused-settle", continuityPixels);
					AuditPausedCrowdPublication(phase);
				}
			}
		}
		finally
		{
			tree.Paused = originalPaused;
		}
		if (pauseDuringSpawn)
		{
			_pausedMassiveBlankFrames += _massiveBlankFrames - massiveBlankFramesBeforePause;
			_pausedSampleBlankFrames += _sampleBlankFrames - sampleBlankFramesBeforePause;
			GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_PAUSED_WAVE phase={phase} spawned={count} auditFrames={_pausedSpawnFrames} pausedMassiveBlankFrames={_pausedMassiveBlankFrames} pausedSampleBlankFrames={_pausedSampleBlankFrames} pausedCrowdRootShortageFrames={_pausedCrowdRootShortageFrames}");
		}
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE phase={phase} spawned={count} total={_spawnedZombies.Count} expectedRoots={CountExpectedRenderRoots()} expectedSprites={CountExpectedSprites()}");
	}

	private void AuditPausedCrowdPublication(string phase)
	{
		_pausedSpawnFrames++;
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		int num = CountLiveCharacters();
		if (aggregateRenderStats.CrowdRoots < num || aggregateRenderStats.FallbackRoots != 0)
		{
			_pausedCrowdRootShortageFrames++;
			GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_PAUSED_CROWD_SHORTAGE phase={phase} crowdRoots={aggregateRenderStats.CrowdRoots} minimumRoots={num} fallbackRoots={aggregateRenderStats.FallbackRoots} characters={CountLiveCharacters()}");
		}
	}

	private void BeginResumePlaybackAudit(string phase, List<string> failures)
	{
		int num = 0;
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			SpawnedZombie spawnedZombie = _spawnedZombies[i];
			if (!string.Equals(spawnedZombie.Phase, phase, StringComparison.Ordinal))
			{
				continue;
			}
			AdobeAnimateSprite mainSprite = spawnedZombie.MainSprite;
			if (!GodotObject.IsInstanceValid(mainSprite))
			{
				failures.Add($"{phase}: resumed playback main sprite is invalid index={spawnedZombie.PhaseIndex}");
				continue;
			}
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(mainSprite.flashAnimeData, mainSprite.atlasProfileOverride);
			spawnedZombie.ResumePlaybackRequired = orBuild != null && orBuild.TryGetClipRange(mainSprite.clip, out var range) && range.Y > range.X && !mainSprite.pause && mainSprite.timeScale > 0.0;
			spawnedZombie.ResumePlaybackAdvanced = false;
			spawnedZombie.ResumePlaybackStartFrame = mainSprite.frameIndex;
			spawnedZombie.ResumePlaybackStartElapsed = mainSprite.elapsedTimer;
			if (spawnedZombie.ResumePlaybackRequired)
			{
				num++;
			}
		}
		_resumePlaybackRequired += num;
		int num2 = (string.Equals(phase, "big", StringComparison.Ordinal) ? 400 : 560);
		if (num != num2)
		{
			failures.Add($"{phase}: not every paused wave zombie has resumable multi-frame playback actual={num} expected={num2}");
		}
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_RESUME_PLAYBACK_BEGIN phase={phase} required={num} expected={num2}");
	}

	private void AuditResumePlaybackProgress()
	{
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			SpawnedZombie spawnedZombie = _spawnedZombies[i];
			if (spawnedZombie.ResumePlaybackRequired && !spawnedZombie.ResumePlaybackAdvanced && GodotObject.IsInstanceValid(spawnedZombie.MainSprite))
			{
				AdobeAnimateSprite mainSprite = spawnedZombie.MainSprite;
				if (mainSprite.frameIndex != spawnedZombie.ResumePlaybackStartFrame || Math.Abs(mainSprite.elapsedTimer - spawnedZombie.ResumePlaybackStartElapsed) > 0.001)
				{
					spawnedZombie.ResumePlaybackAdvanced = true;
					_resumePlaybackAdvanced++;
				}
			}
		}
	}

	private void CompleteResumePlaybackAudit(string phase, List<string> failures)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			SpawnedZombie spawnedZombie = _spawnedZombies[i];
			if (string.Equals(spawnedZombie.Phase, phase, StringComparison.Ordinal) && spawnedZombie.ResumePlaybackRequired)
			{
				num++;
				if (spawnedZombie.ResumePlaybackAdvanced)
				{
					num2++;
				}
			}
		}
		if (num2 != num)
		{
			failures.Add($"{phase}: paused wave main animations did not all resume actual={num2} expected={num}");
		}
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_RESUME_PLAYBACK_RESULT phase={phase} advanced={num2} required={num}");
	}

	private bool SpawnComplexZombie(int index, int phaseIndex, string phase, List<string> failures)
	{
		if (_complexProfiles.Count == 0 || _baselineProfile == null)
		{
			failures.Add("no baseline or complex zombie profile is available");
			return false;
		}
		ZombieSceneProfile zombieSceneProfile = ResolveWaveProfile(phase, phaseIndex);
		Node node;
		try
		{
			node = zombieSceneProfile.Scene.Instantiate(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			failures.Add(zombieSceneProfile.ScenePath + ": wave instantiate failed: " + ex.Message);
			return false;
		}
		if (!(node is TowerDefenseZombie towerDefenseZombie))
		{
			node.Free();
			failures.Add(zombieSceneProfile.ScenePath + ": wave instance is not a formal zombie");
			return false;
		}
		towerDefenseZombie.inGame = false;
		towerDefenseZombie.skipDestroySet = true;
		towerDefenseZombie.editorPreviewMode = true;
		towerDefenseZombie.ProcessMode = ProcessModeEnum.Disabled;
		Node2D node2D = new Node2D
		{
			Name = $"WaveZombieHolder{index}",
			Position = ResolveCharacterPosition(index, phaseIndex, phase),
			Scale = Vector2.One * 0.55f,
			ProcessMode = ProcessModeEnum.Always
		};
		_world.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(towerDefenseZombie, forceReadableName: false, InternalMode.Disabled);
		List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
		CollectSprites(towerDefenseZombie, list);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = list[i];
			adobeAnimateSprite.ProcessMode = ProcessModeEnum.Pausable;
			adobeAnimateSprite.pause = false;
			adobeAnimateSprite.timeScale = 1.0;
			EnsureValidAnimation(adobeAnimateSprite, (adobeAnimateSprite == towerDefenseZombie.sprite) ? GetFirstConfiguredClip(towerDefenseZombie.walkAnimeClip) : string.Empty);
			adobeAnimateSprite.RefreshProcessScheduling();
			if (!adobeAnimateSprite.IsRenderedByParentSpriteForRender() && !adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
			{
				num++;
			}
		}
		SpawnedZombie spawnedZombie = new SpawnedZombie
		{
			Zombie = towerDefenseZombie,
			Holder = node2D,
			SpriteCount = list.Count,
			RenderRootCount = num,
			MainSprite = towerDefenseZombie.sprite,
			Phase = phase,
			PhaseIndex = phaseIndex
		};
		_spawnedZombies.Add(spawnedZombie);
		if (phaseIndex < 4)
		{
			spawnedZombie.CullingControl = phaseIndex == 3;
			if (spawnedZombie.CullingControl)
			{
				spawnedZombie.MainSprite.runtimeViewportCullingEnabled = false;
			}
			_continuitySamples.Add(spawnedZombie);
		}
		return true;
	}

	private void PrepareFormalDeathInventory(List<string> failures)
	{
		if (_complexProfiles.Count == 0)
		{
			failures.Add("formal death inventory has no complex zombie scene");
			return;
		}
		int num = 192;
		for (int i = 0; i < num; i++)
		{
			ZombieSceneProfile zombieSceneProfile = _complexProfiles[i % _complexProfiles.Count];
			TowerDefenseZombie towerDefenseZombie;
			try
			{
				towerDefenseZombie = zombieSceneProfile.Scene.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				failures.Add($"formal death victim {i} instantiate failed path={zombieSceneProfile.ScenePath}: {ex.Message}");
				continue;
			}
			towerDefenseZombie.Name = $"FormalDeathVictim{i}";
			towerDefenseZombie.inGame = false;
			towerDefenseZombie.skipDestroySet = true;
			towerDefenseZombie.ProcessMode = ProcessModeEnum.Always;
			Node2D node2D = new Node2D
			{
				Name = $"FormalDeathVictimHolder{i}",
				Position = ResolveFormalDeathPosition(i),
				Scale = Vector2.One * 0.55f,
				ProcessMode = ProcessModeEnum.Always
			};
			_world.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(towerDefenseZombie, forceReadableName: false, InternalMode.Disabled);
			if (towerDefenseZombie is TowerDefenseZombieShadow towerDefenseZombieShadow)
			{
				towerDefenseZombieShadow.over = true;
			}
			ActivateAnimationTree(towerDefenseZombie);
			List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
			CollectSprites(towerDefenseZombie, list);
			int num2 = 0;
			for (int j = 0; j < list.Count; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite = list[j];
				EnsureValidAnimation(adobeAnimateSprite, (adobeAnimateSprite == towerDefenseZombie.sprite) ? GetFirstConfiguredClip(towerDefenseZombie.walkAnimeClip) : string.Empty);
				if (!adobeAnimateSprite.IsRenderedByParentSpriteForRender() && !adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
				{
					num2++;
				}
			}
			if (list.Count > 1)
			{
				ZombieDeathComponent zombieDeathComponent = towerDefenseZombie.zombieDeathComponent;
				if (zombieDeathComponent != null && !zombieDeathComponent.IsReleased)
				{
					_formalDeathChildAnimationEntries++;
					_formalDeathInventory.Add(new SpawnedZombie
					{
						Zombie = towerDefenseZombie,
						Holder = node2D,
						SpriteCount = list.Count,
						RenderRootCount = num2,
						MainSprite = towerDefenseZombie.sprite,
						Phase = "complex-death-inventory",
						PhaseIndex = i
					});
					continue;
				}
			}
			node2D.Free();
			failures.Add($"formal death victim {i} has no live child animation or death component path={zombieSceneProfile.ScenePath} sprites={list.Count}");
		}
		if (_formalDeathInventory.Count != num)
		{
			failures.Add($"formal death inventory incomplete actual={_formalDeathInventory.Count} expected={num}");
		}
	}

	private static void ActivateAnimationTree(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is AdobeAnimateSprite adobeAnimateSprite)
			{
				adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
				adobeAnimateSprite.pause = false;
				adobeAnimateSprite.Visible = true;
				adobeAnimateSprite.RefreshProcessScheduling();
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				ActivateAnimationTree(node.GetChild(i));
			}
		}
	}

	private ZombieSceneProfile ResolveWaveProfile(string phase, int phaseIndex)
	{
		if (string.Equals(phase, "baseline", StringComparison.Ordinal))
		{
			return _baselineProfile;
		}
		int num = Math.Max(1, _complexProfiles.Count / 2);
		int num2 = (string.Equals(phase, "final", StringComparison.Ordinal) ? num : 0);
		int val = (string.Equals(phase, "final", StringComparison.Ordinal) ? (_complexProfiles.Count - num) : num);
		return _complexProfiles[num2 + phaseIndex % Math.Max(1, val)];
	}

	private void PrepareMutationEntries(List<string> failures)
	{
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			if (_mutationEntries.Count >= 24)
			{
				break;
			}
			SpawnedZombie spawnedZombie = _spawnedZombies[i];
			if (string.Equals(spawnedZombie.Phase, "baseline", StringComparison.Ordinal) && spawnedZombie.PhaseIndex >= 4 && GodotObject.IsInstanceValid(spawnedZombie.Zombie?.sprite))
			{
				_mutationEntries.Add(spawnedZombie);
			}
		}
		if (_mutationEntries.Count != 24)
		{
			failures.Add($"mutation character inventory incomplete actual={_mutationEntries.Count} expected={24}");
		}
	}

	private void BeginFormalDeathWave(string phase, List<string> failures)
	{
		int num = 0;
		for (int i = 0; i < _formalDeathInventory.Count; i++)
		{
			if (num >= 96)
			{
				break;
			}
			SpawnedZombie spawnedZombie = _formalDeathInventory[i];
			if (!spawnedZombie.FormalDeathRequested && GodotObject.IsInstanceValid(spawnedZombie.Zombie) && GodotObject.IsInstanceValid(spawnedZombie.Zombie.sprite))
			{
				ZombieDeathComponent zombieDeathComponent = spawnedZombie.Zombie.zombieDeathComponent;
				if (zombieDeathComponent != null && !zombieDeathComponent.IsReleased)
				{
					spawnedZombie.FormalDeathRequested = true;
					spawnedZombie.Zombie.timeScale = 4.0;
					spawnedZombie.Zombie.sprite.timeScale = 4.0;
					spawnedZombie.Zombie.zombieDeathComponent.fadeDuration = 0.35;
					spawnedZombie.Zombie.Die();
					_formalDeathEntries.Add(spawnedZombie);
					num++;
				}
			}
		}
		if (num != 96)
		{
			failures.Add($"{phase}: formal death inventory incomplete actual={num} expected={96}");
		}
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_FORMAL_DEATH phase={phase} requested={num} totalRequested={_formalDeathEntries.Count}");
	}

	private void ApplyConcurrentMediaLayerMutation()
	{
		int num = _mutationFrame % 4;
		for (int i = 0; i < _mutationEntries.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _mutationEntries[i].Zombie?.sprite;
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				continue;
			}
			adobeAnimateSprite.SetFliter(VisibilityLayerName, num == 0 || num == 2);
			switch (num)
			{
			case 0:
				if (!adobeAnimateSprite.SetAtlasReplace(ReplacementMediaName, "res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png"))
				{
					throw new InvalidOperationException("concurrent SetAtlasReplace rejected the production media key");
				}
				break;
			case 1:
				adobeAnimateSprite.SetReplace(ReplacementMediaName, _replacementTexture);
				break;
			case 2:
				if (!adobeAnimateSprite.SetAtlasReplace(ReplacementMediaName, string.Empty))
				{
					throw new InvalidOperationException("concurrent SetAtlasReplace could not clear the production media key");
				}
				break;
			default:
				adobeAnimateSprite.SetReplace(ReplacementMediaName, (Texture2D)null);
				break;
			}
		}
		ApplyConcurrentCameraMutation();
		_mutationFrame++;
	}

	private void ApplyConcurrentCameraMutation()
	{
		if (_mutationFrame % 12 == 0)
		{
			int num = _mutationFrame / 12 % 4;
			Camera2D camera = _camera;
			camera.Position = num switch
			{
				1 => new Vector2(1050f, 560f), 
				2 => new Vector2(1300f, 640f), 
				_ => new Vector2(1175f, 600f), 
			};
			_camera.Zoom = Vector2.One * num switch
			{
				1 => 0.42f, 
				2 => 0.55f, 
				_ => 0.48f, 
			};
			AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
		}
	}

	private void AuditFormalDeathLifecycle()
	{
		for (int i = 0; i < _formalDeathEntries.Count; i++)
		{
			SpawnedZombie spawnedZombie = _formalDeathEntries[i];
			TowerDefenseZombie zombie = spawnedZombie.Zombie;
			if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(zombie.sprite))
			{
				continue;
			}
			if (!spawnedZombie.FormalDeathAnimationStarted)
			{
				ZombieDeathComponent zombieDeathComponent = zombie.zombieDeathComponent;
				if (zombieDeathComponent != null && zombieDeathComponent.IsDeathAnimationClip(zombie.sprite.clip))
				{
					spawnedZombie.FormalDeathAnimationStarted = true;
					_formalDeathAnimationsStarted++;
				}
			}
			if (!spawnedZombie.FormalDeathAnimationProgressed && spawnedZombie.FormalDeathAnimationStarted && (zombie.sprite.frameIndex != zombie.sprite.clipRange.X || zombie.sprite.elapsedTimer > 0.001))
			{
				spawnedZombie.FormalDeathAnimationProgressed = true;
				_formalDeathAnimationsProgressed++;
			}
			if (!spawnedZombie.FormalDeathFadeObserved && zombie.Modulate.A < 0.999f)
			{
				spawnedZombie.FormalDeathFadeObserved = true;
				_formalDeathFadesObserved++;
			}
		}
	}

	private async Task WaitForFormalDeathCompletion(int referencePixels, List<string> failures)
	{
		int completionFrames = 0;
		while (CountRemainingFormalDeathNodes() > 0 && completionFrames < 240)
		{
			await AuditCurrentFrame("death-completion", referencePixels);
			completionFrames++;
		}
		int num = CountRemainingFormalDeathNodes();
		if (num > 0)
		{
			failures.Add($"formal death nodes did not finish deletion remaining={num} frames={completionFrames}");
		}
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_FORMAL_DEATH_COMPLETION frames={completionFrames} remaining={num} started={_formalDeathAnimationsStarted} progressed={_formalDeathAnimationsProgressed} fades={_formalDeathFadesObserved}");
	}

	private int CountRemainingFormalDeathNodes()
	{
		int num = 0;
		for (int i = 0; i < _formalDeathEntries.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_formalDeathEntries[i].Zombie))
			{
				num++;
			}
		}
		return num;
	}

	private static Vector2 ResolveCharacterPosition(int index, int phaseIndex, string phase)
	{
		if (phaseIndex < 4)
		{
			int num = (string.Equals(phase, "big", StringComparison.Ordinal) ? 1 : (string.Equals(phase, "final", StringComparison.Ordinal) ? 2 : 0));
			return new Vector2(600f + (float)phaseIndex * 500f, 165f + (float)num * 145f);
		}
		int num2 = 405;
		int num3 = index % num2;
		int num4 = index / num2;
		int num5 = num3 % 27;
		int num6 = num3 / 27;
		return new Vector2(244f + (float)num5 * 69f + (float)num4 * 7f, 585f + (float)num6 * 44f + (float)num4 * 2f);
	}

	private static Vector2 ResolveFormalDeathPosition(int index)
	{
		int num = index % 24;
		int num2 = index / 24;
		return new Vector2(280f + (float)num * 78f, 610f + (float)num2 * 50f);
	}

	private async Task CaptureContinuitySampleBaselines(List<string> failures)
	{
		for (int frame = 0; frame < 24; frame++)
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			using Image image = GetViewport().GetTexture().GetImage();
			for (int i = 0; i < _continuitySamples.Count; i++)
			{
				SpawnedZombie spawnedZombie = _continuitySamples[i];
				if (GodotObject.IsInstanceValid(spawnedZombie.MainSprite))
				{
					int val = CountSignalPixels(image, CreateCaptureRegion(spawnedZombie.MainSprite, image.GetSize()), 1);
					spawnedZombie.BaselinePixels = Math.Max(spawnedZombie.BaselinePixels, val);
				}
			}
			if (AllContinuitySamplesHaveBaselines())
			{
				break;
			}
		}
		for (int j = 0; j < _continuitySamples.Count; j++)
		{
			SpawnedZombie spawnedZombie2 = _continuitySamples[j];
			if (!GodotObject.IsInstanceValid(spawnedZombie2.MainSprite))
			{
				failures.Add($"continuity sample {j} phase={spawnedZombie2.Phase} has no valid main animation sprite");
			}
			else if (spawnedZombie2.BaselinePixels <= 0)
			{
				failures.Add($"continuity sample {j} phase={spawnedZombie2.Phase} produced no baseline pixels path={spawnedZombie2.MainSprite.GetPath()}");
			}
		}
	}

	private bool AllContinuitySamplesHaveBaselines()
	{
		for (int i = 0; i < _continuitySamples.Count; i++)
		{
			if (_continuitySamples[i].BaselinePixels <= 0)
			{
				return false;
			}
		}
		return true;
	}

	private static void EnsureValidAnimation(AdobeAnimateSprite sprite, string preferredClip)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData, sprite.atlasProfileOverride);
		if (orBuild == null)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(preferredClip) && orBuild.HasClip(preferredClip))
		{
			sprite.SetAnimation(preferredClip);
			return;
		}
		string clip = sprite.clip;
		if (!string.IsNullOrWhiteSpace(clip) && orBuild.HasClip(clip))
		{
			sprite.SetAnimation(clip);
		}
		else
		{
			if (sprite.flashAnimeData?.clips == null)
			{
				return;
			}
			foreach (Variant key in sprite.flashAnimeData.clips.Keys)
			{
				string text = (string)key;
				if (!string.IsNullOrWhiteSpace(text) && orBuild.HasClip(text))
				{
					sprite.SetAnimation(text);
					break;
				}
			}
		}
	}

	private static string GetFirstConfiguredClip(string configuredClips)
	{
		if (string.IsNullOrWhiteSpace(configuredClips))
		{
			return string.Empty;
		}
		string[] array = configuredClips.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 0)
		{
			return string.Empty;
		}
		return array[0];
	}

	private async Task ObservePhase(string phase, int referencePixels, int frameCount, List<string> failures)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await AuditCurrentFrame(phase, referencePixels);
		}
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_PHASE phase={phase} frames={frameCount} characters={_spawnedZombies.Count} expectedRoots={CountExpectedRenderRoots()} crowdRoots={aggregateRenderStats.CrowdRoots} stateTexels={aggregateRenderStats.CrowdStateTexels} stateCapacity={aggregateRenderStats.CrowdStateCapacityTexels} fallback={aggregateRenderStats.FallbackRoots} massiveBlankFrames={_massiveBlankFrames} sampleBlankFrames={_sampleBlankFrames} shortageFrames={_crowdRootShortageFrames}");
		if (_massiveBlankFrames > 0 || _sampleBlankFrames > 0 || _crowdRootShortageFrames > 0 || _maximumFallbackRoots > 0)
		{
			failures.Add($"{phase}: continuity failed blank={_massiveBlankFrames} sampleBlank={_sampleBlankFrames} shortage={_crowdRootShortageFrames} fallback={_maximumFallbackRoots}");
		}
	}

	private async Task AuditCurrentFrame(string phase, int referencePixels)
	{
		if (!string.Equals(phase, "baseline", StringComparison.Ordinal))
		{
			ApplyConcurrentMediaLayerMutation();
		}
		int signalPixels = await CaptureSignalPixels();
		AuditFormalDeathLifecycle();
		AuditResumePlaybackProgress();
		AdobeAnimateCrowdAggregateStats stats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		int num = Math.Max(1, Mathf.RoundToInt((float)referencePixels * 0.25f));
		if (signalPixels < num)
		{
			_massiveBlankFrames++;
			GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_BLANK phase={phase} pixels={signalPixels} minimum={num} crowdRoots={stats.CrowdRoots} expectedRoots={CountExpectedRenderRoots()} stateTexels={stats.CrowdStateTexels} stateCapacity={stats.CrowdStateCapacityTexels} fallback={stats.FallbackRoots}");
		}
		if (stats.CrowdRoots < CountLiveCharacters())
		{
			_crowdRootShortageFrames++;
		}
		await AuditContinuitySamples(phase);
		_minimumSignalPixels = Math.Min(_minimumSignalPixels, signalPixels);
		_maximumFallbackRoots = Math.Max(_maximumFallbackRoots, stats.FallbackRoots);
		_maximumStateTexels = Math.Max(_maximumStateTexels, stats.CrowdStateTexels);
		_maximumStateCapacityTexels = Math.Max(_maximumStateCapacityTexels, stats.CrowdStateCapacityTexels);
		int gpuRenderGraphAtlasPageCount = AdobeAnimateRenderManager.GpuRenderGraphAtlasPageCount;
		int gpuRenderGraphAtlasLayerCapacity = AdobeAnimateRenderManager.GpuRenderGraphAtlasLayerCapacity;
		_maximumGraphAtlasPages = Math.Max(_maximumGraphAtlasPages, gpuRenderGraphAtlasPageCount);
		_maximumGraphAtlasLayerCapacity = Math.Max(_maximumGraphAtlasLayerCapacity, gpuRenderGraphAtlasLayerCapacity);
		if (_lastStateCapacityTexels > 0 && stats.CrowdStateCapacityTexels != _lastStateCapacityTexels)
		{
			_stateCapacityChanges++;
		}
		if (stats.CrowdStateCapacityTexels > 0)
		{
			_lastStateCapacityTexels = stats.CrowdStateCapacityTexels;
		}
		if (_lastGraphAtlasLayerCapacity > 0 && gpuRenderGraphAtlasLayerCapacity != _lastGraphAtlasLayerCapacity)
		{
			_graphAtlasCapacityChanges++;
		}
		if (gpuRenderGraphAtlasLayerCapacity > 0)
		{
			_lastGraphAtlasLayerCapacity = gpuRenderGraphAtlasLayerCapacity;
		}
	}

	private async Task AuditContinuitySamples(string phase)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		for (int i = 0; i < _continuitySamples.Count; i++)
		{
			SpawnedZombie spawnedZombie = _continuitySamples[i];
			if (!GodotObject.IsInstanceValid(spawnedZombie.MainSprite) || spawnedZombie.BaselinePixels <= 0)
			{
				continue;
			}
			Rect2I rect2I = CreateCaptureRegion(spawnedZombie.MainSprite, image.GetSize());
			if (CountSignalPixels(image, rect2I, 1) > 0)
			{
				spawnedZombie.ConsecutiveBlankFrames = 0;
				spawnedZombie.BlankDiagnosticPrinted = false;
				continue;
			}
			spawnedZombie.ConsecutiveBlankFrames++;
			if (spawnedZombie.ConsecutiveBlankFrames >= 2)
			{
				_sampleBlankFrames++;
				if (!spawnedZombie.BlankDiagnosticPrinted)
				{
					spawnedZombie.BlankDiagnosticPrinted = true;
					spawnedZombie.MainSprite.GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var currentVisible, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressed);
					GD.Print($"ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_SAMPLE_BLANK phase={phase} sample={i} samplePhase={spawnedZombie.Phase} samplePhaseIndex={spawnedZombie.PhaseIndex} consecutive={spawnedZombie.ConsecutiveBlankFrames} baseline={spawnedZombie.BaselinePixels} region={rect2I} cullingControl={spawnedZombie.CullingControl} cached={cached} cachedVisible={cachedVisible} currentVisible={currentVisible} prefetchedVisible={currentVisibleWithPrefetch} nativeSuppressed={nativeCanvasSuppressed} clip={spawnedZombie.MainSprite.clip} frame={spawnedZombie.MainSprite.frameIndex} elapsed={spawnedZombie.MainSprite.elapsedTimer:F4} cameraPosition={_camera.Position} cameraZoom={_camera.Zoom} path={spawnedZombie.MainSprite.GetPath()}");
					GD.Print(spawnedZombie.MainSprite.BuildCrowdFilterDebugReport($"super-big-map-sample-{i}", 8));
				}
			}
		}
	}

	private async Task<int> CaptureSignalPixels()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		return CountSignalPixels(image, new Rect2I(Vector2I.Zero, image.GetSize()), 4);
	}

	private static int CountSignalPixels(Image image, Rect2I region, int step)
	{
		int num = 0;
		int num2 = Math.Min(image.GetWidth(), region.End.X);
		int num3 = Math.Min(image.GetHeight(), region.End.Y);
		for (int i = Math.Max(0, region.Position.Y); i < num3; i += Math.Max(1, step))
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j += Math.Max(1, step))
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - BackgroundColor.R) + Mathf.Abs(pixel.G - BackgroundColor.G) + Mathf.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static Rect2I CreateCaptureRegion(AdobeAnimateSprite sprite, Vector2I imageSize)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData, sprite.atlasProfileOverride);
		Rect2 bounds = GetCurrentFrameBounds(sprite, orBuild);
		if (!IsValidBounds(bounds))
		{
			bounds = orBuild?.LocalBounds ?? new Rect2(new Vector2(-64f, -128f), new Vector2(128f, 160f));
		}
		bounds = new Rect2(bounds.Position + sprite.offset, bounds.Size).Grow(4f);
		Transform2D transform2D = sprite.GetViewport().GetScreenTransform() * sprite.GetGlobalTransformWithCanvas();
		Vector2 position = transform2D * bounds.Position;
		Vector2 to = transform2D * new Vector2(bounds.End.X, bounds.Position.Y);
		Vector2 to2 = transform2D * bounds.End;
		Vector2 to3 = transform2D * new Vector2(bounds.Position.X, bounds.End.Y);
		Rect2 rect = new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
		int num = Math.Clamp(Mathf.FloorToInt(rect.Position.X), 0, imageSize.X);
		int num2 = Math.Clamp(Mathf.FloorToInt(rect.Position.Y), 0, imageSize.Y);
		int num3 = Math.Clamp(Mathf.CeilToInt(rect.End.X), num, imageSize.X);
		int num4 = Math.Clamp(Mathf.CeilToInt(rect.End.Y), num2, imageSize.Y);
		return new Rect2I(num, num2, num3 - num, num4 - num2);
	}

	private static Rect2 GetCurrentFrameBounds(AdobeAnimateSprite sprite, AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return default(Rect2);
		}
		if (sprite.frameIndex >= 0 && sprite.frameIndex < definition.FrameLocalBounds.Length)
		{
			Rect2 rect = definition.FrameLocalBounds[sprite.frameIndex];
			if (IsValidBounds(rect))
			{
				return rect;
			}
		}
		return definition.LocalBounds;
	}

	private static bool IsValidBounds(Rect2 bounds)
	{
		if (bounds.Size.X > 0.01f)
		{
			return bounds.Size.Y > 0.01f;
		}
		return false;
	}

	private static void CollectSprites(Node node, List<AdobeAnimateSprite> output)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is AdobeAnimateSprite item)
			{
				output.Add(item);
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				CollectSprites(node.GetChild(i), output);
			}
		}
	}

	private int CountExpectedRenderRoots()
	{
		int num = 0;
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_spawnedZombies[i].Zombie))
			{
				num += _spawnedZombies[i].RenderRootCount;
			}
		}
		return num;
	}

	private int CountExpectedSprites()
	{
		int num = 0;
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_spawnedZombies[i].Zombie))
			{
				num += _spawnedZombies[i].SpriteCount;
			}
		}
		return num;
	}

	private int CountLiveCharacters()
	{
		int num = 0;
		for (int i = 0; i < _spawnedZombies.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_spawnedZombies[i].Zombie))
			{
				num++;
			}
		}
		return num;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static void Require(bool condition, string failure, List<string> failures)
	{
		if (!condition)
		{
			failures.Add(failure);
		}
	}

	private static void PrintFailures(List<string> failures)
	{
		for (int i = 0; i < failures.Count; i++)
		{
			GD.PrintErr("ADOBE_ANIMATE_SUPER_BIG_MAP_WAVE_CHILD_ANIMATION_FAILURE " + failures[i]);
		}
	}

	private void ReleaseDetachedAuditInstances()
	{
		for (int num = _detachedAuditInstances.Count - 1; num >= 0; num--)
		{
			Node node = _detachedAuditInstances[num];
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
		_detachedAuditInstances.Clear();
	}

	private async Task RestoreRuntime()
	{
		GetTree().Paused = false;
		ReleaseDetachedAuditInstances();
		for (int num = _spawnedZombies.Count - 1; num >= 0; num--)
		{
			SpawnedZombie spawnedZombie = _spawnedZombies[num];
			if (GodotObject.IsInstanceValid(spawnedZombie.Holder) && !spawnedZombie.Holder.IsQueuedForDeletion())
			{
				spawnedZombie.Holder.QueueFree();
			}
		}
		_spawnedZombies.Clear();
		for (int num2 = _formalDeathInventory.Count - 1; num2 >= 0; num2--)
		{
			SpawnedZombie spawnedZombie2 = _formalDeathInventory[num2];
			if (GodotObject.IsInstanceValid(spawnedZombie2.Holder) && !spawnedZombie2.Holder.IsQueuedForDeletion())
			{
				spawnedZombie2.Holder.QueueFree();
			}
		}
		_formalDeathInventory.Clear();
		if (GodotObject.IsInstanceValid(_world) && !_world.IsQueuedForDeletion())
		{
			_world.QueueFree();
		}
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_continuitySamples.Clear();
		_formalDeathEntries.Clear();
		_mutationEntries.Clear();
		_complexProfiles.Clear();
		_retainedScenes.Clear();
		_baselineProfile = null;
		_replacementTexture = null;
		_camera = null;
		_world = null;
		ResourceManager.Instance?.ReleaseTransientResources();
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		AdobeAnimateDefinitionCache.Clear();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		for (int frame = 0; frame < 16; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		for (int frame = 0; frame < 8; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (GodotObject.IsInstanceValid(Global.Instance))
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
		}
		AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
		Engine.MaxFps = _originalMaxFps;
	}
}
