using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.FileSystem;

public static class XWNewLevelResourceDefaults
{
	public const string ResourceClassName = "TowerDefenseLevelNewConfig";

	public const string ScriptUid = "uid://c88k3cqu0k3jm";

	public const string ScriptPath = "Resource/TowerDefense/Level/TowerDefenseLevelNewConfig.cs";

	public const string DefaultMapName = "Frontlawn";

	public const string DefaultBgmName = "Frontlawn";

	public const string DefaultPacketBankName = "GeneralPlant";

	public static TowerDefenseLevelNewConfig Create(string resourceName, string displayName)
	{
		string text = (resourceName ?? "").Trim();
		string text2 = (string.IsNullOrWhiteSpace(displayName) ? text : displayName.Trim());
		return new TowerDefenseLevelNewConfig
		{
			ResourceName = text,
			name = text,
			levelName = text2,
			description = text2,
			levelNumber = 1,
			version = new StringName("1.0"),
			featureData = CreateFeatureData(),
			processName = new StringName("Wave"),
			processData = CreateProcessData()
		};
	}

	private static Dictionary<StringName, Dictionary> CreateFeatureData()
	{
		return new Dictionary<StringName, Dictionary>
		{
			[new StringName("Camera")] = new Dictionary(),
			[new StringName("Map")] = new Dictionary { ["MapName"] = "Frontlawn" },
			[new StringName("Shovel")] = new Dictionary(),
			[new StringName("PacketPick")] = new Dictionary(),
			[new StringName("Mower")] = new Dictionary(),
			[new StringName("Brain")] = new Dictionary(),
			[new StringName("Progress")] = new Dictionary(),
			[new StringName("PacketBank")] = new Dictionary { ["PacketBankName"] = "GeneralPlant" },
			[new StringName("SeedBank")] = new Dictionary
			{
				["Method"] = "CHOOSE",
				["PlantColumn"] = false,
				["ColdDownStart"] = false,
				["ColdDownUse"] = true,
				["Packet"] = new Array()
			},
			[new StringName("BGM")] = new Dictionary { ["BackgroundMusic"] = "Frontlawn" },
			[new StringName("ScreenEffect")] = new Dictionary
			{
				["StormOpen"] = false,
				["PacketBankMethod"] = 1
			},
			[new StringName("Sun")] = new Dictionary
			{
				["Open"] = true,
				["Type"] = "Normal",
				["Begin"] = 300,
				["SpawnInterval"] = 12.0,
				["SpawnNum"] = 50,
				["MovingMethod"] = "LAND"
			},
			[new StringName("Wave")] = CreateWaveData()
		};
	}

	private static Dictionary CreateProcessData()
	{
		Dictionary dictionary = CreateWaveData();
		dictionary["MowerUse"] = true;
		dictionary["StormOpen"] = false;
		return dictionary;
	}

	private static Dictionary CreateWaveData()
	{
		return new Dictionary
		{
			["ZombieInvisible"] = false,
			["FlagZombieUse"] = true,
			["FlagZombie"] = "ZombieFlag",
			["FlagWaveInterval"] = 10,
			["MaxNextWaveHealthPercentage"] = 0.15,
			["MinNextWaveHealthPercentage"] = 0.2,
			["BeginCol"] = 20.0,
			["SpawnColEnd"] = 20.0,
			["SpawnColStart"] = 5.0,
			["Dynamic"] = new Array
			{
				new Dictionary(),
				new Dictionary(),
				new Dictionary(),
				new Dictionary(),
				new Dictionary(),
				new Dictionary(),
				new Dictionary()
			},
			["Wave"] = new Array
			{
				new Dictionary
				{
					["DynamicPlantfood"] = new Array { 0, 0, 0, 0, 0, 0, 0 },
					["Event"] = new Array(),
					["GridSpawn"] = new Array(),
					["Spawn"] = new Array
					{
						new Dictionary
						{
							["Zombie"] = "ZombieNormal",
							["Num"] = 1,
							["Line"] = -1
						}
					}
				}
			},
			["Survival"] = ""
		};
	}
}
