using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentVaseChooseSeedBankVisibilityRuntimeTest : Node
{
	private const string SourceLevelPath = "res://Asset/Config/Level/TowerDefense/Vase/VaseLevel6.tres";

	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private const string CharacterManifestPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string MapManifestPath = "res://Asset/Config/Map/MapResource.json";

	private const string ShovelManifestPath = "res://Asset/Config/Shovel/ShovelResource.json";

	private const string FocusedPacketBankName = "GeneralPlant";

	private const string BrainPacketName = "ItemBrain";

	private const string DefaultShovelName = "ShovelDefault";

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousSprites = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingSprites = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousMaps = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingMaps = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousShovels = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingShovels = new HashSet<string>();

	private readonly List<Resource> _fixtureResources = new List<Resource>();

	private TowerDefensePacketBankData _previousPacketBank;

	private bool _missingPacketBank;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew battle = null;
		TowerDefenseLevelConfig source = null;
		TowerDefenseLevelConfig chooseLevel = null;
		PackedScene packedBattle = null;
		HashSet<string> requiredPacketNames = null;
		HashSet<string> requiredCharacterNames = null;
		TowerDefenseBattleFeatureSeedBank seedFeature = null;
		TowerDefenseBattleFeaturePacketBank packetBankFeature = null;
		Control uiTopContainer = null;
		Control packetBankTranslate = null;
		List<TowerDefenseBattleComponentBase> battleComponents = new List<TowerDefenseBattleComponentBase>();
		List<Resource> battleComponentConfigs = new List<Resource>();
		TowerDefenseLevelBaseConfig previousLevel = manager?.currentLevelConfig;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		bool previousDebugPacketSelect = CommandManager.Instance?.debugPacketSelect ?? false;
		Variant previousMobilePreset = GameSaveManager.Instance?.GetConfigValue("MobilePreset") ?? ((Variant)false);
		bool previousEditor = Global.Instance?.isEditor ?? false;
		string previousEntryMode = Global.Instance?.enterLevelMode ?? "";
		bool battleRegistryWasInitialized = TowerDefenseBattleRegistry.IsInit;
		bool dropItemRegistryWasInitialized = DropItemRegistry.IsInit;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(GameSaveManager.Instance) && GodotObject.IsInstanceValid(CommandManager.Instance), "Required game autoloads must be available.");
				if (_failures > 0)
				{
					goto end_IL_01a9;
				}
				GameSaveManager.Instance.EnsureLoaded();
				if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
				{
					GameSaveManager.Instance.SetUserCurrent("VaseChooseSeedBankVisibilityProbe");
				}
				source = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Vase/VaseLevel6.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(source), "The real VaseLevel6 resource must load.");
				if (!GodotObject.IsInstanceValid(source))
				{
					goto end_IL_01a9;
				}
				source.Init();
				Check(source.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE && GodotObject.IsInstanceValid(source.vaseManager) && source.vaseManager.vaseList.Count > 0, "The source fixture must retain its real Vase process and authored vase layout.");
				requiredPacketNames = CollectRequiredPacketNames(source);
				Check(requiredPacketNames.Count >= 6, $"The readiness gate must cover the three real vase shells and every authored content; got {requiredPacketNames.Count} unique packet keys.");
				bool flag = RegisterFocusedProductionResources(source, requiredPacketNames);
				Check(flag, "Every real VaseLevel6 packet, character scene, preview and map resource must register without starting the all-project loader.");
				if (!flag)
				{
					goto end_IL_01a9;
				}
				requiredCharacterNames = CollectRequiredCharacterNames(requiredPacketNames);
				Check(AllRequiredResourcesReady(requiredPacketNames, requiredCharacterNames), "Every real VaseLevel6 packet and mapped production character scene must be ready before the battle starts.");
				chooseLevel = source.Duplicate(deep: true) as TowerDefenseLevelConfig;
				Check(GodotObject.IsInstanceValid(chooseLevel), "The real Vase level must duplicate for the self-select scenario.");
				if (!GodotObject.IsInstanceValid(chooseLevel))
				{
					goto end_IL_01a9;
				}
				chooseLevel.data = null;
				chooseLevel.name = "VaseChooseSeedBankVisibilityProbe";
				chooseLevel.canExport = false;
				chooseLevel.finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE;
				chooseLevel.packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE;
				chooseLevel.vaseManager.packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE;
				chooseLevel.processName = "Vase";
				chooseLevel.processData = chooseLevel.vaseManager.Export();
				ConfigureFocusedProductionGraph(chooseLevel);
				Check(chooseLevel.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE && chooseLevel.processData.GetValueOrDefault("PacketBankMethod", -1).AsInt32() == 1, "The reproduced Vase level must explicitly use self-selected cards.");
				GameSaveManager.Instance.SetConfigValue("MobilePreset", false);
				CommandManager.Instance.debugPacketSelect = true;
				Global.Instance.isEditor = true;
				Global.Instance.enterLevelMode = "DiyLevel";
				manager.currentLevelConfig = chooseLevel;
				packedBattle = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(packedBattle), "The production battle scene must load.");
				if (!GodotObject.IsInstanceValid(packedBattle))
				{
					goto end_IL_01a9;
				}
				battle = packedBattle.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(battle), "The production battle scene must instantiate.");
				if (!GodotObject.IsInstanceValid(battle))
				{
					goto end_IL_01a9;
				}
				AddChild(battle, forceReadableName: false, InternalMode.Disabled);
				await WaitUntil(() => battle.process is TowerDefenseBattleProcessVase && battle.GetFeature(new StringName("SeedBank")) is TowerDefenseBattleFeatureSeedBank && battle.GetFeature(new StringName("PacketBank")) is TowerDefenseBattleFeaturePacketBank, 900);
				TowerDefenseBattleProcessVase vaseProcess = battle.process as TowerDefenseBattleProcessVase;
				seedFeature = battle.GetFeature(new StringName("SeedBank")) as TowerDefenseBattleFeatureSeedBank;
				packetBankFeature = battle.GetFeature(new StringName("PacketBank")) as TowerDefenseBattleFeaturePacketBank;
				Check(GodotObject.IsInstanceValid(vaseProcess) && battle.process == vaseProcess, "The live battle must use the production TowerDefenseBattleProcessVase.");
				Check(GodotObject.IsInstanceValid(seedFeature) && GodotObject.IsInstanceValid(seedFeature.seedBank), "The self-select Vase battle must create the real SeedBank feature and control.");
				Check(GodotObject.IsInstanceValid(packetBankFeature) && GodotObject.IsInstanceValid(packetBankFeature.packetBank), "The self-select Vase battle must create the real PacketBank feature and control.");
				if (!GodotObject.IsInstanceValid(vaseProcess) || !GodotObject.IsInstanceValid(seedFeature?.seedBank) || !GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
				{
					goto end_IL_01a9;
				}
				await WaitUntil(delegate
				{
					if (!packetBankFeature.skipPacketChoose)
					{
						CanvasItem packetSlotContainer = seedFeature.seedBank.packetSlotContainer;
						if (packetSlotContainer != null && packetSlotContainer.Visible)
						{
							return packetBankFeature.packetBank.GetNode<Control>("Translate").Position.Y < 100f;
						}
					}
					return false;
				}, 600);
				Check(vaseProcess.config.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE && seedFeature.config.method == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE && !packetBankFeature.skipPacketChoose, "The live Vase process and both card features must remain in manual self-select mode.");
				Check(seedFeature.seedBank.packetSlotContainer.Visible, "The selected-card slot container must be visible while card selection is waiting.");
				uiTopContainer = battle.uITopBankContainer?.GetParent() as Control;
				packetBankTranslate = packetBankFeature.packetBank.GetNode<Control>("Translate");
				Check(GodotObject.IsInstanceValid(uiTopContainer), "The real SeedBank must remain parented under the production top UI container.");
				await WaitUntil(() => GodotObject.IsInstanceValid(uiTopContainer) && uiTopContainer.Visible && uiTopContainer.Position.IsEqualApprox(Vector2.Zero) && packetBankFeature.packetBank.Visible && Mathf.IsEqualApprox(packetBankTranslate.Position.Y, 80f) && !battle.uiTopAnimationPlayer.IsPlaying() && !packetBankFeature.packetBank.packetBankAnimationPlayer.IsPlaying(), 120);
				Check(battle.uiTopAnimationPlayer.AssignedAnimation == new StringName("Enter"), $"Vase self-selection must drive the real top-UI Enter animation; assigned={battle.uiTopAnimationPlayer.AssignedAnimation}.");
				Check(uiTopContainer.Visible && uiTopContainer.Position.IsEqualApprox(Vector2.Zero), $"The selected-card slots must finish on-screen at (0, 0); visible={uiTopContainer.Visible}, position={uiTopContainer.Position}.");
				Rect2 visibleRect = battle.GetViewport().GetVisibleRect();
				Check(uiTopContainer.GetGlobalRect().Intersects(visibleRect), $"The live selected-card slot parent must intersect the game viewport; ui={uiTopContainer.GetGlobalRect()}, viewport={visibleRect}.");
				Check(!battle.isGameRunning, "The visibility assertion must run while the Vase process is still waiting for card selection.");
				Check(packetBankFeature.packetBank.Visible && Mathf.IsEqualApprox(packetBankTranslate.Position.Y, 80f), $"The real PacketBank and SeedBank must be simultaneously on-screen during selection; packetBankVisible={packetBankFeature.packetBank.Visible}, translate={packetBankTranslate.Position}.");
				packetBankFeature.EmitChooseOver();
				await WaitUntil(() => battle.isGameRunning, 900);
				Check(battle.isGameRunning, "Completing the selection must let the real Vase battle reach GameRunning.");
				await WaitUntil(() => uiTopContainer.Visible && uiTopContainer.Position.IsEqualApprox(Vector2.Zero) && !battle.uiTopAnimationPlayer.IsPlaying(), 120);
				Check(uiTopContainer.Visible && uiTopContainer.Position.IsEqualApprox(Vector2.Zero), "The selected cards must remain on-screen after the Vase battle starts.");
				goto end_IL_018a;
				end_IL_01a9:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentVaseChooseSeedBankVisibilityRuntimeTest] Unexpected exception: {value}");
				goto end_IL_018a;
			}
			return;
			end_IL_018a:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(battle))
			{
				if (battle.process != null)
				{
					battleComponents.Add(battle.process);
				}
				foreach (TowerDefenseBattleFeature value2 in battle.featureDictionary.Values)
				{
					battleComponents.Add(value2);
				}
				CollectBattleComponentConfigs(battleComponents, battleComponentConfigs);
				battle.Free();
				await WaitFrames(8);
				battle = null;
			}
			seedFeature = null;
			packetBankFeature = null;
			uiTopContainer = null;
			packetBankTranslate = null;
			DisposeBattleComponents(battleComponents, battleComponentConfigs);
			if (GodotObject.IsInstanceValid(CommandManager.Instance))
			{
				CommandManager.Instance.debugPacketSelect = previousDebugPacketSelect;
			}
			if (GodotObject.IsInstanceValid(GameSaveManager.Instance))
			{
				GameSaveManager.Instance.SetConfigValue("MobilePreset", previousMobilePreset);
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isEditor = previousEditor;
				Global.Instance.enterLevelMode = previousEntryMode;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentLevelConfig = previousLevel;
				manager.currentControl = previousControl;
			}
			packedBattle?.Dispose();
			chooseLevel?.Dispose();
			source?.Dispose();
			requiredPacketNames?.Clear();
			requiredCharacterNames?.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			ObjectManager.Instance?.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			ReleaseFocusedStaticScenes();
			ReleaseTransientStaticTexturesImmediately();
			RestoreFocusedProductionResources();
			if (!dropItemRegistryWasInitialized)
			{
				ReleaseDropItemRegistry();
			}
			if (!battleRegistryWasInitialized)
			{
				ReleaseBattleRegistry();
			}
			ResourceManager.Instance?.ReleaseTransientResources();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(16);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(8);
		}
		bool flag2 = _failures == 0 && _checks == 23;
		GD.Print($"VASE_CHOOSE_SEEDBANK_VISIBILITY_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private bool RegisterFocusedProductionResources(TowerDefenseLevelConfig level, IEnumerable<string> packetNames)
	{
		ResourceManager instance = ResourceManager.Instance;
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
		Json json2 = ResourceLoader.Load<Json>("res://Asset/Config/Map/MapResource.json", null, ResourceLoader.CacheMode.Ignore);
		Json json3 = ResourceLoader.Load<Json>("res://Asset/Config/Shovel/ShovelResource.json", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(json) || !GodotObject.IsInstanceValid(json2) || !GodotObject.IsInstanceValid(json3) || json.Data.VariantType != Variant.Type.Dictionary || json2.Data.VariantType != Variant.Type.Dictionary || json3.Data.VariantType != Variant.Type.Dictionary)
		{
			json?.Dispose();
			json2?.Dispose();
			json3?.Dispose();
			return false;
		}
		_fixtureResources.Add(json);
		_fixtureResources.Add(json2);
		_fixtureResources.Add(json3);
		Dictionary dictionary = json.Data.AsGodotDictionary();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		bool flag = true;
		foreach (string item in new HashSet<string>(packetNames, StringComparer.Ordinal) { "ItemBrain" })
		{
			if (!TryFindPacketBinding(dictionary, item, out var characterName, out var binding))
			{
				flag = false;
				continue;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(binding.GetValueOrDefault("Packet", new Dictionary()).AsGodotDictionary().GetValueOrDefault(item, "")
				.AsString(), null, ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				flag = false;
				continue;
			}
			RememberAndReplace(instance.TOWERDEFENSE_PACKETS, _previousPackets, _missingPackets, item, towerDefensePacketConfig);
			_fixtureResources.Add(towerDefensePacketConfig);
			string text = towerDefensePacketConfig.characterConfig?.name;
			if (string.IsNullOrEmpty(text))
			{
				text = characterName;
			}
			if (hashSet.Add(text))
			{
				Dictionary dictionary2 = dictionary.GetValueOrDefault(text, binding).AsGodotDictionary();
				PackedScene packedScene = ResourceLoader.Load<PackedScene>(dictionary2.GetValueOrDefault("Scene", "").AsString(), null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>(dictionary2.GetValueOrDefault("Sprite", "").AsString(), null, ResourceLoader.CacheMode.Ignore);
				flag &= GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(packedScene2);
				if (GodotObject.IsInstanceValid(packedScene))
				{
					RememberAndReplace(instance.TOWERDEFENSE_CHARCATERS, _previousCharacters, _missingCharacters, text, packedScene);
					_fixtureResources.Add(packedScene);
				}
				if (GodotObject.IsInstanceValid(packedScene2))
				{
					RememberAndReplace(instance.CHARCTAER_SPRITE, _previousSprites, _missingSprites, text, packedScene2);
					_fixtureResources.Add(packedScene2);
				}
			}
		}
		TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>(json2.Data.AsGodotDictionary().GetValueOrDefault(level.map, "").AsString(), null, ResourceLoader.CacheMode.Ignore);
		flag &= GodotObject.IsInstanceValid(towerDefenseMapConfig);
		if (GodotObject.IsInstanceValid(towerDefenseMapConfig))
		{
			RememberAndReplace(instance.MAPS, _previousMaps, _missingMaps, level.map, towerDefenseMapConfig);
			_fixtureResources.Add(towerDefenseMapConfig);
		}
		ShovelConfig shovelConfig = ResourceLoader.Load<ShovelConfig>(json3.Data.AsGodotDictionary().GetValueOrDefault("ShovelDefault", "").AsString(), null, ResourceLoader.CacheMode.Ignore);
		flag &= GodotObject.IsInstanceValid(shovelConfig);
		if (GodotObject.IsInstanceValid(shovelConfig))
		{
			RememberAndReplace(instance.SHOVELS, _previousShovels, _missingShovels, "ShovelDefault", shovelConfig);
			_fixtureResources.Add(shovelConfig);
		}
		TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData();
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = (TowerDefenseLevelPacketConfig)(GodotObject)packetBank;
			if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig) && !string.IsNullOrEmpty(towerDefenseLevelPacketConfig.packetName))
			{
				array.Add(towerDefenseLevelPacketConfig.packetName);
			}
		}
		towerDefensePacketBankData.category["White"] = array;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("GeneralPlant", out var value))
		{
			_previousPacketBank = value;
		}
		else
		{
			_missingPacketBank = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["GeneralPlant"] = towerDefensePacketBankData;
		_fixtureResources.Add(towerDefensePacketBankData);
		return flag;
	}

	private static bool TryFindPacketBinding(Dictionary manifest, string packetName, out string characterName, out Dictionary binding)
	{
		foreach (Variant key in manifest.Keys)
		{
			string text = (string)key;
			Dictionary dictionary = manifest.GetValueOrDefault(text, new Dictionary()).AsGodotDictionary();
			if (dictionary.GetValueOrDefault("Packet", new Dictionary()).AsGodotDictionary().ContainsKey(packetName))
			{
				characterName = text;
				binding = dictionary;
				return true;
			}
		}
		characterName = "";
		binding = null;
		return false;
	}

	private static void ConfigureFocusedProductionGraph(TowerDefenseLevelConfig level)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = (TowerDefenseLevelPacketConfig)(GodotObject)packetBank;
			if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
			{
				array.Add(towerDefenseLevelPacketConfig.Export());
			}
		}
		level.featureData = new Godot.Collections.Dictionary<StringName, Dictionary>
		{
			["Map"] = new Dictionary { ["MapName"] = level.map },
			["PacketPick"] = new Dictionary(),
			["SeedBank"] = new Dictionary
			{
				["Method"] = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE.ToString(),
				["PlantColumn"] = level.plantColumn,
				["ColdDownStart"] = level.packetColdDownStart,
				["ColdDownUse"] = level.packetColdDownUse,
				["Packet"] = array
			},
			["PacketBank"] = new Dictionary
			{
				["PacketBankName"] = "GeneralPlant",
				["CategoryBatchSize"] = 3,
				["MaxPoolSize"] = 3
			}
		};
	}

	private static void RememberAndReplace(System.Collections.Generic.Dictionary<string, Resource> registry, System.Collections.Generic.Dictionary<string, Resource> previous, HashSet<string> missing, string key, Resource replacement)
	{
		if (registry.TryGetValue(key, out var value))
		{
			previous[key] = value;
		}
		else
		{
			missing.Add(key);
		}
		registry[key] = replacement;
	}

	private void RestoreFocusedProductionResources()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			RestoreRegistry(instance.TOWERDEFENSE_PACKETS, _previousPackets, _missingPackets);
			RestoreRegistry(instance.TOWERDEFENSE_CHARCATERS, _previousCharacters, _missingCharacters);
			RestoreRegistry(instance.CHARCTAER_SPRITE, _previousSprites, _missingSprites);
			RestoreRegistry(instance.MAPS, _previousMaps, _missingMaps);
			RestoreRegistry(instance.SHOVELS, _previousShovels, _missingShovels);
			if (_missingPacketBank)
			{
				instance.TOWERDEFENSE_PACKETBANKS.Remove("GeneralPlant");
			}
			else if (GodotObject.IsInstanceValid(_previousPacketBank))
			{
				instance.TOWERDEFENSE_PACKETBANKS["GeneralPlant"] = _previousPacketBank;
			}
		}
		foreach (Resource fixtureResource in _fixtureResources)
		{
			if (fixtureResource is TowerDefenseMapConfig towerDefenseMapConfig)
			{
				towerDefenseMapConfig.ClearLoadedMapResources(clearThumbnail: true);
			}
			if (GodotObject.IsInstanceValid(fixtureResource))
			{
				fixtureResource.Dispose();
			}
		}
		_fixtureResources.Clear();
		_previousPackets.Clear();
		_missingPackets.Clear();
		_previousCharacters.Clear();
		_missingCharacters.Clear();
		_previousSprites.Clear();
		_missingSprites.Clear();
		_previousMaps.Clear();
		_missingMaps.Clear();
		_previousShovels.Clear();
		_missingShovels.Clear();
		_previousPacketBank = null;
		_missingPacketBank = false;
	}

	private static void RestoreRegistry(System.Collections.Generic.Dictionary<string, Resource> registry, System.Collections.Generic.Dictionary<string, Resource> previous, IEnumerable<string> missing)
	{
		foreach (string item in missing)
		{
			registry.Remove(item);
		}
		foreach (KeyValuePair<string, Resource> previou in previous)
		{
			registry[previou.Key] = previou.Value;
		}
	}

	private static void CollectBattleComponentConfigs(IEnumerable<TowerDefenseBattleComponentBase> components, ICollection<Resource> configs)
	{
		foreach (TowerDefenseBattleComponentBase component in components)
		{
			if (component.GetType().GetField("config", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(component) is Resource resource && GodotObject.IsInstanceValid(resource))
			{
				configs.Add(resource);
			}
		}
	}

	private void DisposeBattleComponents(IEnumerable<TowerDefenseBattleComponentBase> components, IEnumerable<Resource> configs)
	{
		foreach (TowerDefenseBattleComponentBase component in components)
		{
			if (GodotObject.IsInstanceValid(component))
			{
				component.Dispose();
			}
		}
		foreach (Resource config in configs)
		{
			if (GodotObject.IsInstanceValid(config) && !_fixtureResources.Contains(config))
			{
				config.Dispose();
			}
		}
		if (components is ICollection<TowerDefenseBattleComponentBase> collection)
		{
			collection.Clear();
		}
		if (configs is ICollection<Resource> collection2)
		{
			collection2.Clear();
		}
	}

	private static void ReleaseFocusedStaticScenes()
	{
		ReleaseStaticResource(typeof(TowerDefenseBattleFeatureMap), "_towerDefenseMapControl");
		ReleaseStaticResource(typeof(TowerDefenseBattleFeatureSeedBank), "_towerDefenseIngameSeedBank");
		ReleaseStaticResource(typeof(TowerDefenseBattleFeaturePacketBank), "_towerDefenseIngamePacketBank");
		ReleaseStaticResource(typeof(TowerDefenseInGameSeedBank), "_towerDefenseInGamePacketSlot");
		ReleaseStaticResource(typeof(TowerDefenseManager), "_towerDefenseInGamePacketShow");
	}

	private static void ReleaseStaticResource(Type ownerType, string fieldName)
	{
		FieldInfo field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
		if (field?.GetValue(null) is Resource resource)
		{
			field.SetValue(null, null);
			resource.Dispose();
		}
	}

	private static void ReleaseTransientStaticTexturesImmediately()
	{
		Type[] types = typeof(BugDepartmentVaseChooseSeedBankVisibilityRuntimeTest).Assembly.GetTypes();
		foreach (Type type in types)
		{
			if ((type.FullName ?? type.Name).StartsWith("AdobeAnimate", StringComparison.Ordinal))
			{
				continue;
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral && typeof(Texture2D).IsAssignableFrom(fieldInfo.FieldType) && fieldInfo.GetValue(null) is Texture2D texture2D)
				{
					fieldInfo.SetValue(null, null);
					texture2D.Dispose();
				}
			}
		}
	}

	private static void ReleaseBattleRegistry()
	{
		HashSet<TowerDefenseBattleDependenceData> hashSet = new HashSet<TowerDefenseBattleDependenceData>();
		foreach (TowerDefenseBattleFeature value in TowerDefenseBattleRegistry.BattleFeatureDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value?.dependenceData))
			{
				hashSet.Add(value.dependenceData);
			}
			value?.Dispose();
		}
		foreach (TowerDefenseBattleProcess value2 in TowerDefenseBattleRegistry.BattleProcessDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value2?.dependenceData))
			{
				hashSet.Add(value2.dependenceData);
			}
			value2?.Dispose();
		}
		foreach (TowerDefenseBattleDependenceData item in hashSet)
		{
			item.Dispose();
		}
		TowerDefenseBattleRegistry.BattleFeatureDictionary.Clear();
		TowerDefenseBattleRegistry.BattleProcessDictionary.Clear();
		ClearStaticDictionary(typeof(TowerDefenseBattleRegistry), "BattleFeatureFactoryDictionary");
		ClearStaticDictionary(typeof(TowerDefenseBattleRegistry), "BattleProcessFactoryDictionary");
		TowerDefenseBattleRegistry.IsInit = false;
	}

	private static void ClearStaticDictionary(Type ownerType, string fieldName)
	{
		if (ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is IDictionary dictionary)
		{
			dictionary.Clear();
		}
	}

	private static void ReleaseDropItemRegistry()
	{
		HashSet<DropItemConfig> hashSet = new HashSet<DropItemConfig>(DropItemRegistry.DropItemDictionary.Values);
		HashSet<PackedScene> hashSet2 = new HashSet<PackedScene>();
		foreach (DropItemConfig item in hashSet)
		{
			if (GodotObject.IsInstanceValid(item?.Scene))
			{
				hashSet2.Add(item.Scene);
			}
			item?.Dispose();
		}
		foreach (PackedScene item2 in hashSet2)
		{
			item2.Dispose();
		}
		DropItemRegistry.DropItemDictionary.Clear();
		DropItemRegistry.DropItemByIdDictionary.Clear();
		DropItemRegistry.DropItemByCoinObjectIdDictionary.Clear();
		ReleaseDropItemHandler(ref DropItemRegistry._SunHandler);
		ReleaseDropItemHandler(ref DropItemRegistry._CoinHandler);
		ReleaseDropItemHandler(ref DropItemRegistry._LuckyBagHandler);
		ReleaseDropItemHandler(ref DropItemRegistry._JalapenoSunHandler);
		ReleaseDropItemHandler(ref DropItemRegistry._BrainSunHandler);
		ReleaseDropItemHandler(ref DropItemRegistry._GoldShardHandler);
		DropItemRegistry.IsInit = false;
	}

	private static void ReleaseDropItemHandler<T>(ref T handler) where T : DropItemHandler
	{
		handler?.Dispose();
		handler = null;
	}

	private async Task WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				break;
			}
			await WaitFrames(1);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static HashSet<string> CollectRequiredPacketNames(TowerDefenseLevelConfig level)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { "VaseNormal", "VasePlant", "VaseZombie" };
		foreach (TowerDefenseLevelVaseConfig vase in level.vaseManager.vaseList)
		{
			if (!string.IsNullOrEmpty(vase?.packetName))
			{
				hashSet.Add(vase.packetName);
			}
		}
		foreach (TowerDefenseLevelVaseFillConfig vaseFill in level.vaseManager.vaseFillList)
		{
			if (!string.IsNullOrEmpty(vaseFill?.packetName))
			{
				hashSet.Add(vaseFill.packetName);
			}
		}
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = (TowerDefenseLevelPacketConfig)(GodotObject)packetBank;
			if (!string.IsNullOrEmpty(towerDefenseLevelPacketConfig?.packetName))
			{
				hashSet.Add(towerDefenseLevelPacketConfig.packetName);
			}
		}
		return hashSet;
	}

	private static HashSet<string> CollectRequiredCharacterNames(IEnumerable<string> packetNames)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (string packetName in packetNames)
		{
			string text = TowerDefenseManager.GetPacketConfigReadOnly(packetName)?.characterConfig?.name;
			if (!string.IsNullOrEmpty(text))
			{
				hashSet.Add(text);
			}
		}
		return hashSet;
	}

	private static bool AllRequiredResourcesReady(IEnumerable<string> packetNames, IReadOnlySet<string> characterNames)
	{
		foreach (string packetName in packetNames)
		{
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(packetName);
			string text = packetConfigReadOnly?.characterConfig?.name;
			if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || string.IsNullOrEmpty(text) || !characterNames.Contains(text))
			{
				return false;
			}
		}
		foreach (string characterName in characterNames)
		{
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance.GetCharacterScene(characterName)))
			{
				return false;
			}
		}
		return true;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[VaseChooseSeedBankVisibility] " + message);
		}
	}
}
