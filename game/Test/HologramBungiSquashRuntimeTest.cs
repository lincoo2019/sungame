using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class HologramBungiSquashRuntimeTest : Node
{
	private CubeBoxBloverControl _control;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 2;
		try
		{
			typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
			Register("Diamond/CubeBox", "CubeBox");
			Register("Chapter8/BungiSquash", "BungiSquash");
			Register("Chapter0/Squash", "Squash");
			_control = new CubeBoxBloverControl
			{
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(_control, forceReadableName: false, InternalMode.Disabled);
			_control.characterNode = new Node2D();
			_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			instance.currentControl = _control;
			TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig();
			instance.gridNum = towerDefenseMapConfig.gridNum;
			instance.gridSize = towerDefenseMapConfig.gridSize;
			instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
			{
				config = towerDefenseMapConfig,
				mapConfig = towerDefenseMapConfig,
				control = _control
			};
			towerDefenseBattleFeatureMap.mapControl = new CubeBoxBloverMap
			{
				mapFeature = towerDefenseBattleFeatureMap
			};
			_control.AddChild(towerDefenseBattleFeatureMap.mapControl, forceReadableName: false, InternalMode.Disabled);
			_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
			towerDefenseBattleFeatureMap.PlantGridInit();
			for (int i = 1; i <= 9; i++)
			{
				for (int j = 1; j <= 5; j++)
				{
					towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
				}
			}
			await Probe("PlantBungiSquash");
			await Probe("PlantSquash");
			_control.QueueFree();
			await WaitFrames(5);
			GD.Print($"HOLOGRAM_BUNGI_SQUASH_RESULT passed={_failures == 0 && _checks == 10} checks={_checks} failures={_failures}");
			GetTree().Quit((_failures != 0 || _checks != 10) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PushError(ex.ToString());
			GetTree().Quit(2);
		}
	}

	private void Register(string folder, string name)
	{
		string text = "res://Asset/Anime/Character/Plant/" + folder;
		ResourceManager.Instance.TOWERDEFENSE_PACKETS["Plant" + name] = GD.Load<TowerDefensePacketConfig>(text + "/Packet/Plant" + name + ".tres");
		ResourceManager.Instance.TOWERDEFENSE_CHARCATERS["Plant" + name] = GD.Load<PackedScene>(text + "/Scene/TowerDefensePlant" + name + ".tscn");
	}

	private async Task Probe(string key)
	{
		_control.isGameRunning = false;
		TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData();
		towerDefensePacketBankData.category["White"] = new Godot.Collections.Array { key };
		ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS["PlantPresentBox"] = towerDefensePacketBankData;
		ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS["GeneralPlant"] = towerDefensePacketBankData;
		TowerDefenseBattleFeaturePreSpawn preSpawn = new TowerDefenseBattleFeaturePreSpawn
		{
			control = _control,
			config = new TowerDefenseBattleFeaturePreSpawnConfig()
		};
		preSpawn.config.preSpawnList.Add(new TowerDefenseLevelPreSpawnConfig
		{
			packetName = "PlantCubeBox",
			gridPos = new Vector2I(4, 3)
		});
		_control.featureDictionary["PreSpawn"] = preSpawn;
		await preSpawn.GameEntry();
		await WaitFrames(10);
		TowerDefensePlant plant = null;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter)
			{
				if (plant == null && towerDefenseCharacter.config.name == key)
				{
					plant = (TowerDefensePlant)towerDefenseCharacter;
					continue;
				}
				towerDefenseCharacter.suppressDeathrattles = true;
				towerDefenseCharacter.ClearFromMap();
			}
		}
		await WaitFrames(5);
		Check(plant != null && plant.instance.hologram && plant.z == plant.groundHeight, key + "：必须是盒子生成的地面全息植物。");
		TowerDefenseZombie zombie = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn").Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
		zombie.Position = plant.GetLogicalGlobalPosition() + new Vector2(30f, 0f);
		zombie.gridPos = plant.gridPos;
		_control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(5);
		zombie.instance.hitpoints = 5000.0;
		SquashComponent squash = plant.componentManager.GetRuntime<SquashComponent>();
		int impacts = 0;
		squash.OnJumpDownSmash += delegate
		{
			impacts++;
		};
		_control.isGameRunning = true;
		await preSpawn.GameStart();
		await WaitFrames(2);
		squash.Execute(zombie);
		double peak = 0.0;
		for (int frame = 0; frame < 240; frame++)
		{
			await WaitFrames(1);
			if (GodotObject.IsInstanceValid(plant))
			{
				peak = Math.Max(peak, plant.z - plant.groundHeight);
			}
		}
		Check(peak > 100.0, key + "：必须完成真实起跳。");
		Check(impacts == 1, $"{key}：必须下坠并且只砸击一次，实际{impacts}次。");
		Check(zombie.instance.hitpoints < 5000.0, $"{key}：落地必须实际伤害目标，实际血量{zombie.instance.hitpoints}。");
		Check(!GodotObject.IsInstanceValid(plant) || plant.isDestroy, key + "：砸击后必须清理，不能悬在半空。");
		GD.Print($"HOLOGRAM_SQUASH_TRACE key={key} peak={peak} impacts={impacts} hp={zombie.instance.hitpoints}");
		_control.isGameRunning = false;
		foreach (Node child2 in _control.characterNode.GetChildren())
		{
			if (child2 is TowerDefenseCharacter towerDefenseCharacter2)
			{
				towerDefenseCharacter2.suppressDeathrattles = true;
				towerDefenseCharacter2.ClearFromMap();
			}
		}
		await WaitFrames(5);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.Print("HOLOGRAM_SQUASH_FAILURE " + message);
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
}
