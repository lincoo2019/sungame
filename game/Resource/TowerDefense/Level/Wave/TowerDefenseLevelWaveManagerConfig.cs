using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelWaveManagerConfig : Resource
{
	public const double MaximumSpawnFrameBudgetMilliseconds = 16.0;

	[Export(PropertyHint.None, "")]
	public bool zombieInvisible;

	[Export(PropertyHint.None, "")]
	public bool flagZombieUse = true;

	[Export(PropertyHint.None, "")]
	public string flagZombie = "ZombieFlag";

	[Export(PropertyHint.None, "")]
	public int flagWaveInterval = 10;

	[Export(PropertyHint.None, "")]
	public double maxNextWaveHealthPercentage = 0.15;

	[Export(PropertyHint.None, "")]
	public double minNextWaveHealthPercentage = 0.2;

	[Export(PropertyHint.None, "")]
	public double beginCol = 20.0;

	[Export(PropertyHint.None, "")]
	public double spawnColEnd = 20.0;

	[Export(PropertyHint.None, "")]
	public double spawnColStart = 5.0;

	[Export(PropertyHint.Range, "0.25,16,0.25")]
	public double spawnFrameBudgetMilliseconds = 6.0;

	[Export(PropertyHint.Range, "1,64,1,or_greater")]
	public int spawnMaxCharactersPerFrame = 8;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride spawnOverride;

	private Array<TowerDefenseLevelDynamicConfig> _dynamic = new Array<TowerDefenseLevelDynamicConfig> { null, null, null, null, null, null, null };

	private Array<TowerDefenseLevelWaveConfig> _wave = new Array<TowerDefenseLevelWaveConfig>();

	[ExportCategory("Survival")]
	[Export(PropertyHint.None, "")]
	public bool isCustomSurvival;

	[Export(PropertyHint.None, "")]
	public Variant survival = "";

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelSurvivalConfig customSurvival;

	[ExportCategory("Dynamic")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelDynamicConfig> dynamic
	{
		get
		{
			return _dynamic;
		}
		set
		{
			_dynamic = value;
			WaveDynamicPlantfoodFill();
		}
	}

	[ExportCategory("Wave")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelWaveConfig> wave
	{
		get
		{
			return _wave;
		}
		set
		{
			_wave = value;
			WaveDynamicPlantfoodFill();
		}
	}

	public void Init(Dictionary waveManagerData)
	{
		zombieInvisible = waveManagerData.GetValueOrDefault("ZombieInvisible", false).AsBool();
		flagZombieUse = waveManagerData.GetValueOrDefault("FlagZombieUse", true).AsBool();
		flagZombie = waveManagerData.GetValueOrDefault("FlagZombie", "ZombieFlag").AsString();
		flagWaveInterval = waveManagerData.GetValueOrDefault("FlagWaveInterval", 5.0).AsInt32();
		maxNextWaveHealthPercentage = waveManagerData.GetValueOrDefault("MaxNextWaveHealthPercentage", 0.15).AsDouble();
		minNextWaveHealthPercentage = waveManagerData.GetValueOrDefault("MinNextWaveHealthPercentage", 0.2).AsDouble();
		beginCol = waveManagerData.GetValueOrDefault("BeginCol", 20.0).AsDouble();
		spawnColEnd = waveManagerData.GetValueOrDefault("SpawnColEnd", 20.0).AsDouble();
		spawnColStart = waveManagerData.GetValueOrDefault("SpawnColStart", 5.0).AsDouble();
		spawnFrameBudgetMilliseconds = Mathf.Clamp(waveManagerData.GetValueOrDefault("SpawnFrameBudgetMilliseconds", 6.0).AsDouble(), 0.25, 16.0);
		spawnMaxCharactersPerFrame = Mathf.Max(1, waveManagerData.GetValueOrDefault("SpawnMaxCharactersPerFrame", 8).AsInt32());
		if (waveManagerData.ContainsKey("SpawnOverride"))
		{
			spawnOverride = new TowerDefenseCharacterOverride();
			spawnOverride.Init(waveManagerData.GetValueOrDefault("SpawnOverride", new Dictionary()).AsGodotDictionary());
		}
		Array obj = (waveManagerData.ContainsKey("Dynamic") ? ((Array)waveManagerData["Dynamic"]) : new Array());
		dynamic.Clear();
		foreach (Variant item in obj)
		{
			TowerDefenseLevelDynamicConfig towerDefenseLevelDynamicConfig = new TowerDefenseLevelDynamicConfig();
			Dictionary dictionary = item.AsGodotDictionary();
			if (dictionary.Count > 0)
			{
				towerDefenseLevelDynamicConfig.Init(dictionary);
			}
			dynamic.Add(towerDefenseLevelDynamicConfig);
		}
		Array obj2 = (waveManagerData.ContainsKey("Wave") ? ((Array)waveManagerData["Wave"]) : new Array());
		wave.Clear();
		foreach (Variant item2 in obj2)
		{
			TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = new TowerDefenseLevelWaveConfig();
			towerDefenseLevelWaveConfig.Init(item2.AsGodotDictionary());
			wave.Add(towerDefenseLevelWaveConfig);
		}
		Variant valueOrDefault = waveManagerData.GetValueOrDefault("Survival", "");
		if (valueOrDefault.VariantType == Variant.Type.Dictionary)
		{
			isCustomSurvival = true;
			customSurvival = new TowerDefenseLevelSurvivalConfig();
			customSurvival.Load(valueOrDefault.AsGodotDictionary());
		}
		else
		{
			isCustomSurvival = false;
			survival = valueOrDefault;
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["ZombieInvisible"] = zombieInvisible,
			["FlagZombieUse"] = flagZombieUse,
			["FlagZombie"] = flagZombie,
			["FlagWaveInterval"] = flagWaveInterval,
			["MaxNextWaveHealthPercentage"] = maxNextWaveHealthPercentage,
			["MinNextWaveHealthPercentage"] = minNextWaveHealthPercentage,
			["BeginCol"] = beginCol,
			["SpawnColEnd"] = spawnColEnd,
			["SpawnColStart"] = spawnColStart,
			["SpawnFrameBudgetMilliseconds"] = spawnFrameBudgetMilliseconds,
			["SpawnMaxCharactersPerFrame"] = spawnMaxCharactersPerFrame,
			["Dynamic"] = new Array(),
			["Wave"] = new Array(),
			["Survival"] = survival
		};
		if (GodotObject.IsInstanceValid(spawnOverride))
		{
			dictionary["SpawnOverride"] = spawnOverride.Export();
		}
		foreach (TowerDefenseLevelDynamicConfig item in dynamic)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				((Array)dictionary["Dynamic"]).Add(item.Export());
			}
			else
			{
				((Array)dictionary["Dynamic"]).Add(new Dictionary());
			}
		}
		foreach (TowerDefenseLevelWaveConfig item2 in wave)
		{
			((Array)dictionary["Wave"]).Add(item2.Export());
		}
		if (isCustomSurvival && GodotObject.IsInstanceValid(customSurvival))
		{
			dictionary["Survival"] = customSurvival.Export();
		}
		return dictionary;
	}

	public void WaveDynamicPlantfoodFill()
	{
		if (wave == null)
		{
			return;
		}
		foreach (Variant item in (Array?)wave)
		{
			if (item.VariantType != Variant.Type.Nil && item.AsGodotObject() is TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig)
			{
				towerDefenseLevelWaveConfig.dynamicPlantfood.Resize(dynamic.Count);
			}
		}
	}
}
