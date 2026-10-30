using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelSurvivalConfig : Resource
{
	private Json _json;

	[Export(PropertyHint.None, "")]
	public int roundLimit = -1;

	[Export(PropertyHint.None, "")]
	public bool roundDayNightChange;

	[Export(PropertyHint.None, "")]
	public int pointIncrementPerWave = 50;

	[Export(PropertyHint.None, "")]
	public int pointIncrementPerBigWave = 100;

	[Export(PropertyHint.None, "")]
	public int pointIncrementPerRound = 200;

	[Export(PropertyHint.None, "")]
	public int pointBegin = 100;

	[Export(PropertyHint.None, "")]
	public int pointMax = 10000000;

	[Export(PropertyHint.None, "")]
	public double pointBigWaveScale = 1.5;

	[Export(PropertyHint.None, "")]
	public Array zombiePoolBase = new Array { "ZombieNormal", "ZombieNormalCone" };

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig> zombiePoolRoundAdd = new Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig>();

	[Export(PropertyHint.None, "")]
	public Json json
	{
		get
		{
			return _json;
		}
		set
		{
			_json = value;
			if (GodotObject.IsInstanceValid(_json))
			{
				Init();
				NotifyPropertyListChanged();
			}
		}
	}

	public void Init()
	{
		if (GodotObject.IsInstanceValid(json) && json.Data.VariantType == Variant.Type.Dictionary)
		{
			Dictionary data = json.Data.AsGodotDictionary();
			Load(data);
		}
	}

	public void Load(Dictionary data)
	{
		roundLimit = data.GetValueOrDefault("RoundLimit", -1).AsInt32();
		roundDayNightChange = data.GetValueOrDefault("RoundDayNightChange", false).AsBool();
		pointIncrementPerWave = data.GetValueOrDefault("PointIncrementPerWave", 50).AsInt32();
		pointIncrementPerBigWave = data.GetValueOrDefault("PointIncrementPerBigWave", 100).AsInt32();
		pointIncrementPerRound = data.GetValueOrDefault("PointIncrementPerRound", 200).AsInt32();
		pointBegin = data.GetValueOrDefault("Pointregin", 100).AsInt32();
		pointMax = data.GetValueOrDefault("PointMax", 10000000).AsInt32();
		pointBigWaveScale = data.GetValueOrDefault("PointBigWaveScale", 1.5).AsDouble();
		Dictionary dictionary = data.GetValueOrDefault("ZombiePoolSetting", new Dictionary()).AsGodotDictionary();
		zombiePoolBase = (dictionary.ContainsKey("ZombiePoolBase") ? ((Array)dictionary["ZombiePoolBase"]) : new Array());
		zombiePoolRoundAdd.Clear();
		foreach (Variant item in dictionary.ContainsKey("ZombiePoolRoundAdd") ? ((Array)dictionary["ZombiePoolRoundAdd"]) : new Array())
		{
			Dictionary data2 = item.AsGodotDictionary();
			TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = new TowerDefenseLevelSurvivalZombiePoolRoundAddConfig();
			towerDefenseLevelSurvivalZombiePoolRoundAddConfig.Init(data2);
			zombiePoolRoundAdd.Add(towerDefenseLevelSurvivalZombiePoolRoundAddConfig);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["PointIncrementPerWave"] = pointIncrementPerWave,
			["PointIncrementPerBigWave"] = pointIncrementPerBigWave,
			["PointIncrementPerRound"] = pointIncrementPerRound,
			["Pointregin"] = pointBegin,
			["PointMax"] = pointMax,
			["PointBigWaveScale"] = pointBigWaveScale,
			["ZombiePoolSetting"] = new Dictionary
			{
				["ZombiePoolBase"] = zombiePoolBase,
				["ZombiePoolRoundAdd"] = new Array()
			}
		};
		foreach (TowerDefenseLevelSurvivalZombiePoolRoundAddConfig item in zombiePoolRoundAdd)
		{
			((Array)((Dictionary)dictionary["ZombiePoolSetting"])["ZombiePoolRoundAdd"]).Add(item.Export());
		}
		return dictionary;
	}
}
