using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class LevelEditorNutPreviewFlickerRuntimeTest : Node
{
	private const string ResultMarker = "LEVEL_EDITOR_NUT_PREVIEW_FLICKER_RESULT";

	private const string EditorScenePath = "res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn";

	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private const string SuperBigMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres";

	private const string ExternalFixtureEnvironmentName = "PVZHE_LEVEL_EDITOR_FIXTURE_RESOURCE";

	private const int BaselineFrameCount = 180;

	private const int SampleFrameCount = 180;

	private const int RetainedRepublishFrameCount = 720;

	private const int MinimumVisiblePixels = 80;

	private const float MinimumBaselinePixelRatio = 0.75f;

	private static readonly (string Key, string PacketPath, string ScenePath, Vector2I Grid)[] Fixtures = new(string, string, string, Vector2I)[6]
	{
		("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", new Vector2I(4, 2)),
		("PlantFirenut", "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Packet/PlantFirenut.tres", "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", new Vector2I(5, 2)),
		("PlantMagnetnut", "res://Asset/Anime/Character/Plant/Chapter1/Magnetnut/Packet/PlantMagnetnut.tres", "res://Asset/Anime/Character/Plant/Chapter1/Magnetnut/Scene/TowerDefensePlantMagnetnut.tscn", new Vector2I(6, 2)),
		("PlantIcenut", "res://Asset/Anime/Character/Plant/Chapter2/Icenut/Packet/PlantIcenut.tres", "res://Asset/Anime/Character/Plant/Chapter2/Icenut/Scene/TowerDefensePlantIcenut.tscn", new Vector2I(7, 2)),
		("PlantJalaNut", "res://Asset/Anime/Character/Plant/Chapter3/JalaNut/Packet/PlantJalaNut.tres", "res://Asset/Anime/Character/Plant/Chapter3/JalaNut/Scene/TowerDefensePlantJalaNut.tscn", new Vector2I(8, 2)),
		("PlantMininut", "res://Asset/Anime/Character/Plant/Chapter7/Mininut/Packet/PlantMininut.tres", "res://Asset/Anime/Character/Plant/Chapter7/Mininut/Scene/TowerDefensePlantMininut.tscn", new Vector2I(4, 3))
	};

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private Dictionary _characterRegistry;

	private TowerDefensePacketBankData _previousTotalPacketBank;

	private bool _missingTotalPacketBank;

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseLevelBaseConfig previousLevelConfig = manager?.currentLevelConfig;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousEditor = Global.IsEditor;
		string previousScene = SceneManager.CurrentScene;
		LevelEditorNutPreviewFlickerControlStub control = null;
		CanvasLayer editorCanvas = null;
		LevelEditorMapEditor editor = null;
		TowerDefenseMapConfig mapConfig = null;
		Image emptyMapImage = null;
		int expectedPacketCardCount = 0;
		try
		{
			_ = 12;
			try
			{
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(SceneManager.Instance))
				{
					throw new InvalidOperationException("Required gameplay and editor autoloads are unavailable.");
				}
				_originalBackend = Global.Instance.adobeAnimateRenderBackend;
				_originalMaxFps = Engine.MaxFps;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				Engine.MaxFps = 60;
				(string, string, string, Vector2I)[] fixtures = Fixtures;
				for (int i = 0; i < fixtures.Length; i++)
				{
					(string, string, string, Vector2I) tuple = fixtures[i];
					RegisterFixture(tuple.Item1, tuple.Item2, tuple.Item3);
				}
				Global.Instance.isEditor = true;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				editor = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<LevelEditorMapEditor>(PackedScene.GenEditState.Disabled);
				if (!GodotObject.IsInstanceValid(editor))
				{
					throw new InvalidOperationException("The production LevelEditorMapEditor scene is unavailable.");
				}
				string environmentVariable = System.Environment.GetEnvironmentVariable("PVZHE_LEVEL_EDITOR_FIXTURE_RESOURCE");
				bool useExternalFixture = !string.IsNullOrWhiteSpace(environmentVariable);
				TowerDefenseLevelConfig level = (useExternalFixture ? ResourceLoader.Load<TowerDefenseLevelConfig>(environmentVariable, null, ResourceLoader.CacheMode.IgnoreDeep) : new TowerDefenseLevelConfig());
				if (!GodotObject.IsInstanceValid(level))
				{
					throw new InvalidOperationException("The external level fixture could not be loaded: " + environmentVariable);
				}
				if (useExternalFixture)
				{
					HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
					for (int j = 0; j < level.preSpawnList.Count; j++)
					{
						string packetName = level.preSpawnList[j].packetName;
						if (hashSet.Add(packetName))
						{
							RegisterFixture(packetName);
						}
					}
					expectedPacketCardCount = RegisterPacketBankFixture(2147483647);
				}
				LevelEditorPacketBank packetBank = editor.GetNodeOrNull<LevelEditorPacketBank>("%LevelEditorPacketBank");
				if (GodotObject.IsInstanceValid(packetBank))
				{
					packetBank.data = new TowerDefensePacketBankData();
				}
				editor.levelConfig = level;
				control = new LevelEditorNutPreviewFlickerControlStub
				{
					Name = "LevelEditorNutPreviewFlickerControl",
					isInit = true,
					isGameRunning = true,
					levelConfig = level
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = editor.GetNode<Node2D>("%CharacterNode");
				TowerDefenseMapControl node = editor.GetNode<TowerDefenseMapControl>("%TowerDefenseMapControl");
				TowerDefenseBattleFeatureMap mapFeature = (node.mapFeature = new TowerDefenseBattleFeatureMap
				{
					mapControl = node,
					control = control
				});
				control.featureDictionary[new StringName("Map")] = mapFeature;
				manager.currentControl = control;
				manager.currentLevelConfig = level;
				editorCanvas = new CanvasLayer
				{
					Name = "LevelEditorLayer"
				};
				AddChild(editorCanvas, forceReadableName: false, InternalMode.Disabled);
				editorCanvas.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				string path = (useExternalFixture ? "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres" : "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres");
				mapConfig = ResourceLoader.Load<TowerDefenseMapConfig>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
				if (!GodotObject.IsInstanceValid(mapConfig) || !mapFeature.MapInit(mapConfig) || !GodotObject.IsInstanceValid(mapFeature.config))
				{
					throw new InvalidOperationException("The production Frontlawn map failed to initialize in the editor.");
				}
				manager.gridBeginPos = mapFeature.config.gridBeginPos;
				manager.gridSize = mapFeature.config.gridSize;
				manager.gridNum = mapFeature.config.gridNum;
				editor.ApplyMapPreviewConfig(mapConfig);
				await WaitFrames(5);
				emptyMapImage = await CaptureImage();
				if (!useExternalFixture)
				{
					fixtures = Fixtures;
					foreach ((string, string, string, Vector2I) tuple2 in fixtures)
					{
						Array<TowerDefenseLevelPreSpawnConfig> preSpawnList = level.preSpawnList;
						TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig();
						(towerDefenseLevelPreSpawnConfig.packetName, _, _, towerDefenseLevelPreSpawnConfig.gridPos) = tuple2;
						preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
					}
				}
				editor.Save(isSave: false);
				await WaitFrames(useExternalFixture ? 1 : 8);
				TowerDefenseCharacter[] characters = (useExternalFixture ? new TowerDefenseCharacter[2] : new TowerDefenseCharacter[Fixtures.Length]);
				if (useExternalFixture)
				{
					TowerDefenseCharacter towerDefenseCharacter = FindFirstFixtureCharacter(editor.characterNode, level, "PlantFirenut");
					TowerDefenseCharacter towerDefenseCharacter2 = FindFirstFixtureCharacter(editor.characterNode, level, "PlantWallnut");
					if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
					{
						throw new InvalidOperationException("The production editor did not create the authored fire-nut preview.");
					}
					if (!GodotObject.IsInstanceValid(towerDefenseCharacter2))
					{
						throw new InvalidOperationException("The production editor did not create the authored wall-nut preview.");
					}
					characters = new TowerDefenseCharacter[2] { towerDefenseCharacter, towerDefenseCharacter2 };
					await WaitFrames(3);
				}
				Rect2I[] regions = new Rect2I[characters.Length];
				for (int k = 0; k < characters.Length; k++)
				{
					if (!useExternalFixture)
					{
						characters[k] = FindCharacter(editor.characterNode, Fixtures[k].Key, Fixtures[k].Grid);
					}
					if (!GodotObject.IsInstanceValid(characters[k]) || !GodotObject.IsInstanceValid(characters[k].sprite))
					{
						throw new InvalidOperationException($"Editor preview character at index {k} is unavailable.");
					}
				}
				UpdateCaptureRegions(characters, regions, useExternalFixture);
				List<(AdobeAnimateSprite Sprite, StringName MediaName, string Path)> replacementBindings = CollectActiveAtlasReplacements(characters);
				if (replacementBindings.Count == 0)
				{
					throw new InvalidOperationException("The editor nut fixtures do not contain active atlas replacements.");
				}
				int graphAllocationsBeforeConcurrentLoading = AdobeAnimateRenderManager.GpuRenderGraphAtlasAllocationCount;
				if (useExternalFixture)
				{
					if (!GodotObject.IsInstanceValid(packetBank))
					{
						throw new InvalidOperationException("The production level-editor packet bank is unavailable.");
					}
					AdobeAnimateRenderManager.ClearGpuRenderGraphCaches(notifyRuntime: false);
					packetBank.data = ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS["Total"];
					packetBank.CategoryChoose("White", reFresh: true);
				}
				int[] baselinePixels = new int[characters.Length];
				int[] initialMinimumPixels = new int[characters.Length];
				int[] initialBlankFrames = new int[characters.Length];
				int[] loadingBlankFrames = new int[characters.Length];
				int[] loadingMinimumPixels = new int[characters.Length];
				System.Array.Fill(initialMinimumPixels, 2147483647);
				System.Array.Fill(loadingMinimumPixels, 2147483647);
				int packetLoadingFrameCount = 0;
				int minimumCrowdRootsDuringPacketLoading = 2147483647;
				int baselineFrameCount = (useExternalFixture ? 30 : 180);
				for (int frame = 0; frame < baselineFrameCount; frame++)
				{
					using Image current = await CaptureImage();
					UpdateCaptureRegions(characters, regions, useExternalFixture);
					bool flag = useExternalFixture && packetBank.packetList.Count < expectedPacketCardCount;
					for (int l = 0; l < regions.Length; l++)
					{
						int num = CountChangedPixels(current, emptyMapImage, regions[l]);
						baselinePixels[l] = Math.Max(baselinePixels[l], num);
						initialMinimumPixels[l] = Math.Min(initialMinimumPixels[l], num);
						if (num < 80)
						{
							initialBlankFrames[l]++;
						}
						if (flag)
						{
							loadingMinimumPixels[l] = Math.Min(loadingMinimumPixels[l], num);
							if (num < 80)
							{
								loadingBlankFrames[l]++;
							}
						}
					}
					if (flag)
					{
						minimumCrowdRootsDuringPacketLoading = Math.Min(minimumCrowdRootsDuringPacketLoading, AdobeAnimateRenderManager.GetAggregateRenderStats().CrowdRoots);
						packetLoadingFrameCount++;
					}
				}
				AdobeAnimateCrowdAggregateStats baselineStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int baselineGraphAllocations = AdobeAnimateRenderManager.GpuRenderGraphAtlasAllocationCount;
				if (useExternalFixture)
				{
					for (int frame = 0; frame < 240; frame++)
					{
						if (packetBank.packetList.Count >= expectedPacketCardCount)
						{
							break;
						}
						using Image current2 = await CaptureImage();
						UpdateCaptureRegions(characters, regions, useExternalFixture);
						for (int m = 0; m < regions.Length; m++)
						{
							int num2 = CountChangedPixels(current2, emptyMapImage, regions[m]);
							loadingMinimumPixels[m] = Math.Min(loadingMinimumPixels[m], num2);
							if (num2 < 80)
							{
								loadingBlankFrames[m]++;
							}
						}
						minimumCrowdRootsDuringPacketLoading = Math.Min(minimumCrowdRootsDuringPacketLoading, AdobeAnimateRenderManager.GetAggregateRenderStats().CrowdRoots);
						packetLoadingFrameCount++;
					}
					if (packetBank.packetList.Count < expectedPacketCardCount)
					{
						throw new InvalidOperationException("The production level-editor packet bank did not finish loading its GPU previews.");
					}
				}
				if (minimumCrowdRootsDuringPacketLoading == 2147483647)
				{
					minimumCrowdRootsDuringPacketLoading = baselineStats.CrowdRoots;
				}
				int packetPreviewGpuNodes = (useExternalFixture ? CountPacketPreviewGpuNodes(packetBank) : 0);
				int graphAllocationsAfterPacketLoading = AdobeAnimateRenderManager.GpuRenderGraphAtlasAllocationCount;
				AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
				int[] blankFrames = new int[characters.Length];
				int[] minimumPixels = new int[characters.Length];
				System.Array.Fill(minimumPixels, 2147483647);
				int inactiveDispatchFrames = 0;
				int replacementRefreshes = 0;
				int cacheRefreshes = 1;
				int roundTripCount = (useExternalFixture ? 30 : 180);
				int sampleFrameCount = roundTripCount;
				for (int frame = 0; frame < roundTripCount; frame++)
				{
					if (useExternalFixture)
					{
						Vector2 anchor = editor.GetGlobalRect().Position + new Vector2(100f, 300f);
						editor._Input(new InputEventMouseButton
						{
							ButtonIndex = MouseButton.WheelUp,
							Pressed = true,
							Factor = 10f,
							Position = anchor
						});
						await WaitFrames(1);
						using (await CaptureImage())
						{
							editor._Input(new InputEventMouseButton
							{
								ButtonIndex = MouseButton.WheelDown,
								Pressed = true,
								Factor = 10f,
								Position = anchor
							});
							await WaitFrames(1);
						}
					}
					if (!useExternalFixture && frame > 0 && frame % 30 == 0)
					{
						AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
						cacheRefreshes++;
					}
					if (!useExternalFixture && frame % 5 == 0)
					{
						RefreshReplacementBindings(replacementBindings);
						replacementRefreshes++;
					}
					using Image current3 = await CaptureImage();
					UpdateCaptureRegions(characters, regions, useExternalFixture);
					bool flag2 = true;
					for (int n = 0; n < regions.Length; n++)
					{
						int num3 = CountChangedPixels(current3, emptyMapImage, regions[n]);
						minimumPixels[n] = Math.Min(minimumPixels[n], num3);
						if (!HasContinuousPixels(num3, baselinePixels[n]))
						{
							blankFrames[n]++;
						}
						flag2 &= characters[n].sprite.RuntimeManagerDispatchActive;
					}
					if (!flag2)
					{
						inactiveDispatchFrames++;
					}
				}
				int[] retainedRepublishBlankFrames = new int[characters.Length];
				int minimumRetainedDynamicBindings = 2147483647;
				if (!useExternalFixture)
				{
					RefreshReplacementBindings(replacementBindings);
					await WaitFrames(2);
					for (int frame = 0; frame < 720; frame++)
					{
						AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
						using Image current4 = await CaptureImage();
						minimumRetainedDynamicBindings = Math.Min(minimumRetainedDynamicBindings, AdobeAnimateRenderManager.GetPublishedDynamicOverrideBindingCount());
						UpdateCaptureRegions(characters, regions, useExternalFixture);
						for (int num4 = 0; num4 < regions.Length; num4++)
						{
							if (!HasContinuousPixels(CountChangedPixels(current4, emptyMapImage, regions[num4]), baselinePixels[num4]))
							{
								retainedRepublishBlankFrames[num4]++;
							}
						}
					}
				}
				if (minimumRetainedDynamicBindings == 2147483647)
				{
					minimumRetainedDynamicBindings = AdobeAnimateRenderManager.GetPublishedDynamicOverrideBindingCount();
				}
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				bool flag3 = !useExternalFixture || (System.Array.TrueForAll(loadingBlankFrames, (int frames) => frames == 0) && minimumCrowdRootsDuringPacketLoading == baselineStats.CrowdRoots && packetPreviewGpuNodes == 0 && graphAllocationsAfterPacketLoading == baselineGraphAllocations);
				bool flag4 = !useExternalFixture || aggregateRenderStats.CrowdRoots == baselineStats.CrowdRoots;
				bool flag5 = !useExternalFixture || System.Array.TrueForAll(initialBlankFrames, (int frames) => frames == 0);
				bool flag6 = useExternalFixture || (System.Array.TrueForAll(retainedRepublishBlankFrames, (int frames) => frames == 0) && minimumRetainedDynamicBindings > 0);
				bool flag7 = System.Array.TrueForAll(baselinePixels, (int pixels) => pixels >= 80) && flag5 && System.Array.TrueForAll(blankFrames, (int frames) => frames == 0) && inactiveDispatchFrames == 0 && flag3 && flag4 && flag6 && aggregateRenderStats.FallbackRoots == 0;
				GD.Print($"{"LEVEL_EDITOR_NUT_PREVIEW_FLICKER_RESULT"} passed={flag7} baseline={string.Join(',', baselinePixels)} initialMinimum={string.Join(',', initialMinimumPixels)} initialBlank={string.Join(',', initialBlankFrames)}/{baselineFrameCount} initialStable={flag5} loadingMinimum={string.Join(',', loadingMinimumPixels)} loadingBlank={string.Join(',', loadingBlankFrames)}/{packetLoadingFrameCount} packetLoadingStable={flag3} packetPreviewGpuNodes={packetPreviewGpuNodes} graphAllocations={graphAllocationsBeforeConcurrentLoading}->{baselineGraphAllocations}->{graphAllocationsAfterPacketLoading} minimumLoadingCrowd={minimumCrowdRootsDuringPacketLoading} minimum={string.Join(',', minimumPixels)} blank={string.Join(',', blankFrames)}/{sampleFrameCount} retainedBlank={string.Join(',', retainedRepublishBlankFrames)}/{((!useExternalFixture) ? 720 : 0)} retainedDynamicBindings={minimumRetainedDynamicBindings} retainedStable={flag6} inactiveDispatch={inactiveDispatchFrames}/{sampleFrameCount} targets={characters.Length} replacements={replacementBindings.Count} replacementRefreshes={replacementRefreshes} cacheRefreshes={cacheRefreshes} baselineCrowd={baselineStats.CrowdRoots} crowd={aggregateRenderStats.CrowdRoots} crowdRestored={flag4} stateTexels={aggregateRenderStats.CrowdStateTexels} stateCapacity={aggregateRenderStats.CrowdStateCapacityTexels} fallback={aggregateRenderStats.FallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag7) ? 2 : 0);
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"LEVEL_EDITOR_NUT_PREVIEW_FLICKER_RESULT"} exception={value}");
			}
		}
		finally
		{
			emptyMapImage?.Dispose();
			if (GodotObject.IsInstanceValid(editor))
			{
				foreach (Node child in editor.characterNode.GetChildren())
				{
					if (child is TowerDefenseCharacter && !child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
				editor.QueueFree();
			}
			if (GodotObject.IsInstanceValid(editorCanvas) && !editorCanvas.IsQueuedForDeletion())
			{
				editorCanvas.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.currentLevelConfig = previousLevelConfig;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
				Global.Instance.isEditor = previousEditor;
			}
			Engine.MaxFps = _originalMaxFps;
			SceneManager.Instance.currentScene = previousScene;
			mapConfig?.Dispose();
			await WaitFrames(3);
			RestoreFixtures();
			await WaitFrames(2);
		}
		GetTree().Quit(exitCode);
	}

	private void RegisterFixture(string key, string packetPath, string scenePath)
	{
		if (!_previousPackets.ContainsKey(key) && !_missingPackets.Contains(key))
		{
			ResourceManager instance = ResourceManager.Instance;
			if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
			{
				_previousPackets[key] = value;
			}
			else
			{
				_missingPackets.Add(key);
			}
			if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value2))
			{
				_previousCharacters[key] = value2;
			}
			else
			{
				_missingCharacters.Add(key);
			}
			instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		}
	}

	private void RegisterFixture(string key)
	{
		if (!_previousPackets.ContainsKey(key) && !_missingPackets.Contains(key))
		{
			if (_characterRegistry == null)
			{
				Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
				_characterRegistry = (GodotObject.IsInstanceValid(json) ? ((Dictionary)json.Data) : null);
			}
			if (_characterRegistry == null || !_characterRegistry.ContainsKey(key))
			{
				throw new InvalidOperationException("The production character registry does not contain: " + key);
			}
			Dictionary dictionary = (Dictionary)_characterRegistry[key];
			Dictionary dictionary2 = (Dictionary)dictionary["Packet"];
			string text = dictionary.GetValueOrDefault("Scene").AsString();
			string text2 = dictionary2.GetValueOrDefault(key).AsString();
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				throw new InvalidOperationException("The production fixture paths are incomplete: " + key);
			}
			RegisterFixture(key, text2, text);
		}
	}

	private int RegisterPacketBankFixture(int maximumCards)
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/PacketBank/PacketBankResource.json", null, ResourceLoader.CacheMode.Ignore);
		Dictionary dictionary = (GodotObject.IsInstanceValid(json) ? ((Dictionary)json.Data) : null);
		if (dictionary == null || !dictionary.ContainsKey("GeneralPlant"))
		{
			throw new InvalidOperationException("The production packet-bank registry is unavailable.");
		}
		Godot.Collections.Array array = (Godot.Collections.Array)((Dictionary)((Dictionary)dictionary["GeneralPlant"])["Category"])["White"];
		Array<string> array2 = new Array<string>();
		for (int i = 0; i < array.Count; i++)
		{
			if (array2.Count >= maximumCards)
			{
				break;
			}
			string text = array[i].AsString();
			RegisterFixture(text);
			array2.Add(text);
		}
		TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData();
		towerDefensePacketBankData.category["White"] = array2;
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("Total", out var value))
		{
			_previousTotalPacketBank = value;
		}
		else
		{
			_missingTotalPacketBank = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["Total"] = towerDefensePacketBankData;
		return array2.Count;
	}

	private void RestoreFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		if (_missingTotalPacketBank)
		{
			instance.TOWERDEFENSE_PACKETBANKS.Remove("Total");
		}
		else if (GodotObject.IsInstanceValid(_previousTotalPacketBank))
		{
			instance.TOWERDEFENSE_PACKETBANKS["Total"] = _previousTotalPacketBank;
		}
		_previousTotalPacketBank = null;
		_missingTotalPacketBank = false;
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private static TowerDefenseCharacter FindCharacter(Node parent, string configName, Vector2I grid)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && !towerDefenseCharacter.IsQueuedForDeletion() && towerDefenseCharacter.config?.name == configName && towerDefenseCharacter.gridPos == grid)
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private static TowerDefenseCharacter FindFirstFixtureCharacter(Node parent, TowerDefenseLevelConfig level, string configName)
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(level))
		{
			return null;
		}
		for (int i = 0; i < level.preSpawnList.Count; i++)
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = level.preSpawnList[i];
			if (towerDefenseLevelPreSpawnConfig.packetName == configName)
			{
				return FindCharacter(parent, configName, towerDefenseLevelPreSpawnConfig.gridPos);
			}
		}
		return null;
	}

	private static int CountPacketPreviewGpuNodes(LevelEditorPacketBank packetBank)
	{
		if (!GodotObject.IsInstanceValid(packetBank))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < packetBank.packetList.Count; i++)
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = packetBank.packetList[i];
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow?.sprite))
			{
				num += CountPreviewGpuNodes(towerDefenseInGamePacketShow.sprite);
			}
		}
		return num;
	}

	private static int CountPreviewGpuNodes(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return 0;
		}
		int num = ((node is AdobeAnimateSprite { forceLocalRender: false }) ? 1 : 0);
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			num += CountPreviewGpuNodes(child);
		}
		return num;
	}

	private static Rect2I CreateCaptureRegion(AdobeAnimateSprite sprite, bool useAuthoredBounds)
	{
		if (!useAuthoredBounds)
		{
			Transform2D screenTransform = sprite.GetViewport().GetScreenTransform();
			Vector2 origin = sprite.GetGlobalTransformWithCanvas().Origin;
			Vector2 vector = screenTransform * origin;
			int num = Mathf.RoundToInt(70f * screenTransform.X.Length());
			int height = Mathf.RoundToInt(90f * screenTransform.Y.Length());
			int x = Mathf.RoundToInt(vector.X) - num / 2;
			int y = Mathf.RoundToInt(vector.Y - 60f * screenTransform.Y.Length());
			return new Rect2I(x, y, num, height);
		}
		Transform2D transform2D = sprite.GetViewport().GetScreenTransform() * sprite.GetGlobalTransformWithCanvas();
		Rect2 rect = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData)?.LocalBounds ?? new Rect2(new Vector2(-80f, -100f), new Vector2(160f, 180f));
		rect = new Rect2(rect.Position + sprite.offset, rect.Size).Grow(8f);
		Vector2 position = transform2D * rect.Position;
		Vector2 to = transform2D * new Vector2(rect.End.X, rect.Position.Y);
		Vector2 to2 = transform2D * rect.End;
		Vector2 to3 = transform2D * new Vector2(rect.Position.X, rect.End.Y);
		Rect2 rect2 = new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
		return new Rect2I(Mathf.FloorToInt(rect2.Position.X), Mathf.FloorToInt(rect2.Position.Y), Math.Max(1, Mathf.CeilToInt(rect2.Size.X)), Math.Max(1, Mathf.CeilToInt(rect2.Size.Y)));
	}

	private static void UpdateCaptureRegions(TowerDefenseCharacter[] characters, Rect2I[] regions, bool useAuthoredBounds)
	{
		if (characters == null || regions == null || characters.Length != regions.Length)
		{
			return;
		}
		for (int i = 0; i < characters.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = characters[i]?.sprite;
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				regions[i] = CreateCaptureRegion(adobeAnimateSprite, useAuthoredBounds);
			}
		}
	}

	private static List<(AdobeAnimateSprite Sprite, StringName MediaName, string Path)> CollectActiveAtlasReplacements(TowerDefenseCharacter[] characters)
	{
		List<(AdobeAnimateSprite, StringName, string)> list = new List<(AdobeAnimateSprite, StringName, string)>();
		for (int i = 0; i < characters.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = characters[i]?.sprite;
			AdobeAnimateData adobeAnimateData = adobeAnimateSprite?.flashAnimeData;
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite) || adobeAnimateData?.mediaDictionary == null)
			{
				continue;
			}
			foreach (Variant key in adobeAnimateData.mediaDictionary.Keys)
			{
				StringName stringName = new StringName(key.AsString());
				string atlasReplacePath = adobeAnimateSprite.GetAtlasReplacePath(stringName);
				if (!string.IsNullOrWhiteSpace(atlasReplacePath))
				{
					list.Add((adobeAnimateSprite, stringName, atlasReplacePath));
				}
			}
		}
		return list;
	}

	private static void RefreshReplacementBindings(List<(AdobeAnimateSprite Sprite, StringName MediaName, string Path)> bindings)
	{
		HashSet<AdobeAnimateSprite> hashSet = new HashSet<AdobeAnimateSprite>();
		for (int i = 0; i < bindings.Count; i++)
		{
			var (adobeAnimateSprite, mediaName, textureReference) = bindings[i];
			adobeAnimateSprite.SetAtlasReplace(mediaName, textureReference, queueUpdate: false);
			hashSet.Add(adobeAnimateSprite);
		}
		foreach (AdobeAnimateSprite item in hashSet)
		{
			item.UpdateMediaReplaceData();
		}
	}

	private static bool HasContinuousPixels(int pixels, int baselinePixels)
	{
		int num = Math.Max(80, Mathf.FloorToInt((float)baselinePixels * 0.75f));
		if (baselinePixels >= 80)
		{
			return pixels >= num;
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<Image> CaptureImage()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}

	private static int CountChangedPixels(Image current, Image emptyMap, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, Math.Min(current.GetWidth(), emptyMap.GetWidth()));
		int num3 = Math.Min(region.End.Y, Math.Min(current.GetHeight(), emptyMap.GetHeight()));
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
			{
				Color pixel = current.GetPixel(j, i);
				Color pixel2 = emptyMap.GetPixel(j, i);
				if (Math.Abs(pixel.R - pixel2.R) + Math.Abs(pixel.G - pixel2.G) + Math.Abs(pixel.B - pixel2.B) > 0.12f)
				{
					num++;
				}
			}
		}
		return num;
	}
}
