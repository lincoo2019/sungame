using Godot;
using Godot.Collections;

public partial class TrioAmbushInspection : Node
{
	[Export(PropertyHint.None, "")]
	public string MapName = "Backyard";

	public override async void _Ready()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		GameSaveManager.Instance.EnsureLoaded();
		GameSaveManager.Instance.EnsureUser();
		Global.Instance.isMultiplayerMode = false;
		Global.Instance.enterLevelMode = "LevelTest";
		Dictionary dictionary = Json.ParseString(FileAccess.GetFileAsString("res://Asset/Config/Level/TowerDefense/Chapter3/Level3_1.json")).AsGodotDictionary();
		dictionary["Name"] = "";
		dictionary["LevelName"] = "三人组出场验收";
		dictionary["Map"] = MapName;
		dictionary["Talk"] = "";
		dictionary["MowerUse"] = false;
		dictionary["Reward"] = new Dictionary();
		dictionary["PacketBank"] = new Dictionary
		{
			["Method"] = "Preset",
			["ColdDownStart"] = false,
			["ColdDownUse"] = false,
			["Value"] = new Array { "PlantIceShroom", "PlantCoffeebean", "PlantUmbrellaleaf", "PlantLilyPad", "PlantPot", "PlantCherryBomb" }
		};
		dictionary["SunManager"] = new Dictionary
		{
			["Open"] = true,
			["Begin"] = 9990
		};
		Array array = new Array();
		for (int i = 0; i < 3; i++)
		{
			array.Add(new Dictionary
			{
				["Spawn"] = new Array(),
				["Event"] = new Array
				{
					new Dictionary { ["EventName"] = ((MapName.StartsWith("Roof") || i % 2 == 1) ? "BungiTrioSpawn" : "CoralTrioSpawn") }
				}
			});
		}
		dictionary["WaveManager"] = new Dictionary
		{
			["BeginCol"] = 5.0,
			["SpawnColStart"] = 12.0,
			["SpawnColEnd"] = 20.0,
			["FlagWaveInterval"] = 99,
			["Dynamic"] = new Array(),
			["Wave"] = array
		};
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
		{
			data = new Json
			{
				Data = dictionary
			}
		};
		towerDefenseLevelConfig.Init();
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		Node node = GD.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn").Instantiate(PackedScene.GenEditState.Disabled);
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public Dictionary _mcp_state()
	{
		TowerDefenseControlNew nodeOrNull = GetNodeOrNull<TowerDefenseControlNew>("TowerDefenseControlNew");
		TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = nodeOrNull?.GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		Dictionary dictionary = new Dictionary
		{
			["running"] = nodeOrNull?.isGameRunning ?? false,
			["ready"] = towerDefenseBattleFeatureWave?.readySetPlantOver ?? false,
			["wave"] = towerDefenseBattleFeatureWave?.SaveFeature() ?? new Dictionary()
		};
		Array array = new Array();
		if (GodotObject.IsInstanceValid(nodeOrNull?.characterNode))
		{
			foreach (Node child in nodeOrNull.characterNode.GetChildren())
			{
				if (child is TowerDefenseZombie towerDefenseZombie)
				{
					array.Add(new Dictionary
					{
						["name"] = towerDefenseZombie.Name,
						["packet"] = towerDefenseZombie.packet?.saveKey ?? "carrier",
						["grid"] = towerDefenseZombie.gridPos,
						["z"] = towerDefenseZombie.z,
						["ground"] = towerDefenseZombie.isGround,
						["frozen"] = towerDefenseZombie.buff?.buffDictionary.ContainsKey("Frozen") ?? false,
						["trio"] = TrioAmbushMember.ExportSpawnState(towerDefenseZombie)
					});
				}
			}
		}
		dictionary["characters"] = array;
		return dictionary;
	}
}
