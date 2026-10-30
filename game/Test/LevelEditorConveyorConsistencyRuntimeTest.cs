using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class LevelEditorConveyorConsistencyRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	private LevelEditorSeedbankEditor _editor;

	private TowerDefenseControlNew _control;

	private TowerDefenseBattleFeatureConveyorBelt _conveyor;

	private PacketPickControl _packetPick;

	public override async void _Ready()
	{
		try
		{
			_ = 4;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData();
				towerDefensePacketBankData.category["White"] = new Array<string> { "PlantSunFlower", "PlantWallnut" };
				ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS["Total"] = towerDefensePacketBankData;
				Global.Instance.isEditor = true;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				GameSaveManager.Instance.SetConfigValue("MobilePreset", false);
				_editor = GD.Load<PackedScene>("res://Prefab/GUI/LevelEditor/SeedbankEditor/LevelEditorSeedbankEditor.tscn").Instantiate<LevelEditorSeedbankEditor>(PackedScene.GenEditState.Disabled);
				AddChild(_editor, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				await VerifyModeSwitchAndActualSpawns();
				await VerifyPriorityRoundTrips();
				await VerifyImmediateSaveAndReload();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PrintErr($"LEVEL_EDITOR_CONVEYOR_EXCEPTION {value}");
			}
		}
		finally
		{
			ReleaseConveyor();
			if (GodotObject.IsInstanceValid(_editor))
			{
				_editor.Clear();
				_editor.QueueFree();
			}
			await WaitFrames(2);
		}
		int num = ((DisplayServer.GetName() != "headless" && !string.IsNullOrWhiteSpace(OS.GetEnvironment("PVZ_CONVEYOR_ARTIFACT_DIR"))) ? 27 : 26);
		bool flag = _failures == 0 && _checks == num;
		GD.Print($"LEVEL_EDITOR_CONVEYOR_CONSISTENCY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyModeSwitchAndActualSpawns()
	{
		TowerDefenseLevelConfig level = CreateLevel("PlantWallnut");
		level.packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET;
		level.packetBankList.Add(new TowerDefenseLevelPacketConfig
		{
			packetName = "PlantSunFlower"
		});
		_editor.Init(level);
		await WaitFrames(2);
		Check(LevelEditorSeedbank.Instance.packetList.Count == 1, "预选卡面板应还原一张向日葵");
		OptionButton node = _editor.GetNode<OptionButton>("%MethodOptionButton");
		node.Selected = 3;
		node.EmitSignal(OptionButton.SignalName.ItemSelected, (long)node.Selected);
		foreach (TowerDefenseInGamePacketShow item in LevelEditorSeedbank.Instance.packetList.Duplicate())
		{
			LevelEditorSeedbank.Instance.DeletePacket(item);
		}
		LevelEditorSeedbank.Instance.AddPacket(TowerDefenseManager.GetPacketConfig("PlantWallnut"));
		_editor.Save();
		level.PrepareForEditorSave();
		Check(level.featureData["ConveyorBelt"]["PacketPrioritySpawnList"].AsGodotArray().Count == 0, "旧预选卡不应变成编辑器中看不见的优先出卡");
		Check(level.conveyorData.packetList.Count == 1 && level.conveyorData.packetList[0].name == "PlantWallnut", "保存的卡池应只包含编辑器留下的坚果");
		CreateConveyor(level);
		for (int i = 0; i < 6; i++)
		{
			Check(_conveyor.Spawn()?.config?.saveKey == "PlantWallnut", $"切换模式后第 {i + 1} 张实际出卡应为坚果");
		}
		Check(_conveyor.conveyorBeltManager.GetPacketCount() == 6, "传送带应实际显示六张卡");
		await CaptureEvidence();
		ReleaseConveyor();
		Check(ResourceSaver.Save(level, "user://conveyor_consistency.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "自制关卡资源应保存成功");
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("user://conveyor_consistency.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
		towerDefenseLevelConfig.PrepareForEditorSave();
		CreateConveyor(towerDefenseLevelConfig);
		Check(_conveyor.Spawn()?.config?.saveKey == "PlantWallnut", "重新加载资源再进入测试仍应只出坚果");
		ReleaseConveyor();
		await WaitFrames(2);
	}

	private async Task VerifyPriorityRoundTrips()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = CreateLevel("PlantWallnut");
		towerDefenseLevelConfig.conveyorData.packetPrioritySpawnList.Add(new TowerDefenseLevelPacketConfig
		{
			packetName = "PlantSunFlower"
		});
		towerDefenseLevelConfig.conveyorData.packetPrioritySpawnList.Add(new TowerDefenseLevelPacketConfig
		{
			packetName = "PlantSunFlower"
		});
		for (int i = 0; i < 3; i++)
		{
			Json json = new Json();
			Check(json.Parse(Json.Stringify(towerDefenseLevelConfig.Export())) == Error.Ok, "传送带配置应可导出并解析");
			towerDefenseLevelConfig = new TowerDefenseLevelConfig
			{
				data = json
			};
			towerDefenseLevelConfig.PrepareForEditorSave();
			Check(towerDefenseLevelConfig.featureData["ConveyorBelt"]["PacketPrioritySpawnList"].AsGodotArray().Count == 2, $"第 {i + 1} 次往返后优先卡应仍为两张");
		}
		CreateConveyor(towerDefenseLevelConfig);
		Check(_conveyor.Spawn()?.config?.saveKey == "PlantSunFlower", "第一张明确配置的优先卡应保留");
		Check(_conveyor.Spawn()?.config?.saveKey == "PlantSunFlower", "第二张同名优先卡应保留");
		Check(_conveyor.Spawn()?.config?.saveKey == "PlantWallnut", "两张优先卡用完后应立即回到坚果卡池");
		ReleaseConveyor();
		await WaitFrames(2);
	}

	private async Task VerifyImmediateSaveAndReload()
	{
		foreach (TowerDefenseInGamePacketShow packet in LevelEditorSeedBankChoose.Instance.packetList)
		{
			packet.alive = false;
		}
		LevelEditorSeedBankChoose.Instance.currentIndex = -1;
		TowerDefenseLevelConfig towerDefenseLevelConfig = CreateLevel("PlantSunFlower", "PlantSunFlower", "PlantWallnut");
		_editor.Init(towerDefenseLevelConfig);
		_editor.Save();
		Check(towerDefenseLevelConfig.conveyorData.packetList.Count == 3, "打开关卡后立即保存应保留全部三张卡及重复项");
		TowerDefenseLevelConfig second = CreateLevel("PlantWallnut");
		_editor.Init(second);
		_editor.Save();
		await WaitFrames(2);
		Check(LevelEditorSeedbank.Instance.packetList.Count == 1 && LevelEditorSeedbank.Instance.packetList[0].config.saveKey == "PlantWallnut", "快速切换关卡后旧关卡卡牌不得异步串入");
		_editor.Save();
		second.PrepareForEditorSave();
		Check(second.conveyorData.packetList.Count == 1 && second.conveyorData.packetList[0].name == "PlantWallnut", "立即进入测试的配置应与最终编辑器卡牌一致");
		string[] array = new string[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ((i % 2 == 0) ? "PlantSunFlower" : "PlantWallnut");
		}
		TowerDefenseLevelConfig towerDefenseLevelConfig2 = CreateLevel(array);
		_editor.Init(towerDefenseLevelConfig2);
		_editor.Save();
		towerDefenseLevelConfig2.PrepareForEditorSave();
		Check(LevelEditorSeedbank.Instance.packetList.Count == 16 && towerDefenseLevelConfig2.conveyorData.packetList.Count == 16, "满额卡池应完整保留十六张卡");
		bool flag = true;
		for (int j = 0; j < towerDefenseLevelConfig2.conveyorData.packetList.Count; j++)
		{
			flag &= towerDefenseLevelConfig2.conveyorData.packetList[j].name == array[j];
		}
		Check(flag, "保存后的卡池顺序和重复次数应与编辑器一致");
	}

	private async Task CaptureEvidence()
	{
		if (DisplayServer.GetName() == "headless")
		{
			return;
		}
		for (int i = 0; i < 1200; i++)
		{
			_conveyor.conveyorBeltManager.UpdateBeltAnimation(1.0 / 60.0);
		}
		await WaitFrames(2);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		string environment = OS.GetEnvironment("PVZ_CONVEYOR_ARTIFACT_DIR");
		if (!string.IsNullOrWhiteSpace(environment))
		{
			Check(image.SavePng(Path.Combine(environment, "editor-and-conveyor.png")) == Error.Ok, "编辑器及传送带画面应成功保存");
		}
	}

	private static TowerDefenseLevelConfig CreateLevel(params string[] names)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
		{
			packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR,
			conveyorData = new TowerDefenseConveyorConfig(),
			rainData = new TowerDefenseRainModeConfig(),
			sunManager = new TowerDefenseLevelSunManagerConfig()
		};
		foreach (string name in names)
		{
			towerDefenseLevelConfig.conveyorData.packetList.Add(new TowerDefenseConveyorPacketConfig
			{
				name = name
			});
		}
		return towerDefenseLevelConfig;
	}

	private void CreateConveyor(TowerDefenseLevelConfig level)
	{
		level.ExportToFeatureProcess();
		_control = new TowerDefenseControlNew();
		_control.uITopBankContainer = new HBoxContainer();
		_control.uITopBankContainer.Position = new Vector2(0f, 520f);
		AddChild(_control.uITopBankContainer, forceReadableName: false, InternalMode.Disabled);
		_packetPick = new PacketPickControl();
		TowerDefenseBattleFeatureMap value = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			packetPickControl = _packetPick
		};
		_control.featureDictionary["Map"] = value;
		TowerDefenseManager.Instance.currentControl = _control;
		_conveyor = new TowerDefenseBattleFeatureConveyorBelt
		{
			control = _control
		};
		_control.featureDictionary["ConveyorBelt"] = _conveyor;
		_conveyor.Init(level.featureData["ConveyorBelt"]);
	}

	private void ReleaseConveyor()
	{
		if (_conveyor != null)
		{
			_conveyor.Destroy();
			_conveyor.Dispose();
			_conveyor = null;
		}
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.uITopBankContainer.QueueFree();
			_control.Free();
			_control = null;
			TowerDefenseManager.Instance.currentControl = null;
		}
		if (GodotObject.IsInstanceValid(_packetPick))
		{
			_packetPick.Free();
			_packetPick = null;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PrintErr("LEVEL_EDITOR_CONVEYOR_CHECK_FAILED " + message);
		}
	}
}
