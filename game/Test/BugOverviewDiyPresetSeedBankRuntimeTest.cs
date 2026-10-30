using System;
using Godot;

public partial class BugOverviewDiyPresetSeedBankRuntimeTest : Node
{
	private const string SourceLevelPath = "res://Asset/Config/Level/TowerDefense/Chapter8/Level8_17.tres";

	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private const string SeedBankEditorScenePath = "res://Prefab/GUI/LevelEditor/SeedbankEditor/LevelEditorSeedbankEditor.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			Check(GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(GameSaveManager.Instance), "Required game autoloads must be available.");
			if (_failures > 0)
			{
				return;
			}
			GameSaveManager.Instance.EnsureLoaded();
			if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
			{
				GameSaveManager.Instance.SetUserCurrent("DiyPresetProbe");
			}
			bool resourcesLoaded = false;
			ResourceManager.Instance.OnLoadOver += OnLoadOver;
			try
			{
				ResourceManager.Instance.BeginLoad();
				for (int frame = 0; frame < 7200; frame++)
				{
					if (resourcesLoaded)
					{
						break;
					}
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
			finally
			{
				ResourceManager.Instance.OnLoadOver -= OnLoadOver;
			}
			Check(resourcesLoaded, "ResourceManager must complete before the level test starts.");
			if (!resourcesLoaded)
			{
				return;
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Chapter8/Level8_17.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig), "The real PRESET source level must load.");
			if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
			{
				return;
			}
			towerDefenseLevelConfig.Init();
			Check(towerDefenseLevelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET && towerDefenseLevelConfig.packetBankList.Count > 0, "The source level must provide real preset cards.");
			TowerDefenseLevelConfig diyLevel = towerDefenseLevelConfig.Duplicate(deep: true) as TowerDefenseLevelConfig;
			Check(GodotObject.IsInstanceValid(diyLevel), "The editor level must duplicate.");
			if (!GodotObject.IsInstanceValid(diyLevel))
			{
				return;
			}
			diyLevel.data = null;
			diyLevel.name = "DiyPresetRuntimeProbe";
			diyLevel.canExport = false;
			int expectedPresetCards = diyLevel.packetBankList.Count;
			Check(diyLevel.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET && expectedPresetCards > 0, "The field-backed DIY level must retain PRESET cards before testing.");
			diyLevel.vaseManager = new TowerDefenseLevelVaseManagerConfig();
			diyLevel.vaseManager.vaseList.Add(new TowerDefenseLevelVaseConfig
			{
				packetName = "PlantMagnetMine",
				type = "Normal",
				gridPos = new Vector2I(9, 1)
			});
			diyLevel.finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
			diyLevel.processName = "Vase";
			diyLevel.processData = diyLevel.vaseManager.Export();
			diyLevel.processData.Remove("PacketBankMethod");
			diyLevel.processData.Remove("PacketBankExitDelay");
			diyLevel.processData.Remove("MowerUse");
			Check(GodotObject.IsInstanceValid(diyLevel.vaseManager) && diyLevel.vaseManager.vaseList.Count == 1, "The compatibility probe must contain a real Vase configuration.");
			Check(diyLevel.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE && diyLevel.processName == (StringName)"Vase", "The compatibility probe must reproduce the legacy WAVE-field/Vase-process mismatch.");
			Check(!diyLevel.processData.ContainsKey("PacketBankMethod") && !diyLevel.processData.ContainsKey("MowerUse"), "The legacy Vase process must start without modern SeedBank orchestration fields.");
			LevelEditorSeedbankEditor seedBankEditor = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/SeedbankEditor/LevelEditorSeedbankEditor.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<LevelEditorSeedbankEditor>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(seedBankEditor), "The real in-game SeedBank editor must instantiate.");
			if (!GodotObject.IsInstanceValid(seedBankEditor))
			{
				return;
			}
			AddChild(seedBankEditor, forceReadableName: false, InternalMode.Disabled);
			seedBankEditor.Init(diyLevel);
			for (int frame = 0; frame < 180; frame++)
			{
				if (LevelEditorSeedbank.Instance.packetList.Count == expectedPresetCards)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(LevelEditorSeedbank.Instance.packetList.Count == expectedPresetCards, $"The editor must materialize {expectedPresetCards} selected cards before testing; got {LevelEditorSeedbank.Instance.packetList.Count}.");
			seedBankEditor.Save();
			Check(diyLevel.packetBankList.Count == expectedPresetCards, "LevelTest Save must retain every selected PRESET card.");
			RemoveChild(seedBankEditor);
			seedBankEditor.Free();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			TowerDefenseManager.Instance.currentLevelConfig = diyLevel;
			Global.Instance.isEditor = true;
			Global.Instance.enterLevelMode = "DiyLevel";
			TowerDefenseControlNew battle = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(battle), "The real battle scene must instantiate.");
			if (!GodotObject.IsInstanceValid(battle))
			{
				return;
			}
			AddChild(battle, forceReadableName: false, InternalMode.Disabled);
			for (int frame = 0; frame < 900; frame++)
			{
				if (battle.isGameRunning)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(battle.isGameRunning, "The DIY test must reach GameRunning.");
			Check(diyLevel.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE, "A field-backed level with a saved built-in Vase process must restore VASE finish semantics.");
			Check(diyLevel.processName == (StringName)"Vase" && diyLevel.processData.ContainsKey("PacketBankMethod") && diyLevel.processData["PacketBankMethod"].AsInt32() == 2 && diyLevel.processData.ContainsKey("MowerUse"), "The regenerated Vase process must receive PRESET SeedBank orchestration fields.");
			TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = battle.GetFeature(new StringName("SeedBank")) as TowerDefenseBattleFeatureSeedBank;
			TowerDefenseBattleFeaturePacketBank instance = battle.GetFeature(new StringName("PacketBank")) as TowerDefenseBattleFeaturePacketBank;
			Check(GodotObject.IsInstanceValid(towerDefenseBattleFeatureSeedBank) && GodotObject.IsInstanceValid(towerDefenseBattleFeatureSeedBank.seedBank), "A PRESET DIY test must create the real SeedBank feature and control.");
			Check(GodotObject.IsInstanceValid(instance), "A PRESET DIY test must keep the PacketBank orchestration feature.");
			if (!GodotObject.IsInstanceValid(towerDefenseBattleFeatureSeedBank?.seedBank))
			{
				return;
			}
			TowerDefenseInGameSeedBank seedBank = towerDefenseBattleFeatureSeedBank.seedBank;
			Check(towerDefenseBattleFeatureSeedBank.config.method == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET, "The runtime SeedBank must still resolve PRESET.");
			Check(seedBank.packetList.Count == expectedPresetCards, $"All preset cards must materialize; expected {expectedPresetCards}, got {seedBank.packetList.Count}.");
			Check(GodotObject.IsInstanceValid(seedBank.packetSlotContainer) && seedBank.packetSlotContainer.Visible, "The active preset-card slot container must be visible.");
			Control uiTopContainer = battle.uITopBankContainer?.GetParent() as Control;
			for (int frame = 0; frame < 60; frame++)
			{
				if (!GodotObject.IsInstanceValid(uiTopContainer))
				{
					break;
				}
				if (!(uiTopContainer.Position.Y <= -1f))
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(GodotObject.IsInstanceValid(uiTopContainer) && uiTopContainer.Visible && uiTopContainer.Position.Y > -1f, $"The parent top UI container must finish its entry animation; position={uiTopContainer?.Position}.");
			void OnLoadOver()
			{
				resourcesLoaded = true;
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewDiyPresetSeedBankRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			GD.Print($"DIY_PRESET_SEEDBANK_RESULT passed={_failures == 0 && _checks == 22} checks={_checks} failures={_failures}");
			GetTree().Quit((_failures != 0 || _checks != 22) ? 2 : 0);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewDiyPresetSeedBankRuntimeTest] " + message);
		}
	}
}
