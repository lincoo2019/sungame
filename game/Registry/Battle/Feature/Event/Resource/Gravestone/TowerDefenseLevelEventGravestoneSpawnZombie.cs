using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventGravestoneSpawnZombie : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public Array zombieNames = new Array { "ZombieNormal", "ZombieNormalCone", "ZombieNormalBucket" };

	[Export(PropertyHint.None, "")]
	public int zombieNum = 5;

	[Export(PropertyHint.None, "")]
	public Vector2 delay = new Vector2(-1f, -1f);

	public TowerDefenseCharacterOverride overrideVal;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride @override
	{
		get
		{
			return overrideVal;
		}
		set
		{
			overrideVal = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "overrideVal")
		{
			overrideVal = value.As<TowerDefenseCharacterOverride>();
			return true;
		}
		return false;
	}

	public override string _GetName()
	{
		return "LEVLE_EVENT_GRAVESTONE_SPAWN_ZOMBIE";
	}

	public override void Execute()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			TowerDefenseBattleFeatureWave.Instance.GravestoneSpawn(zombieNames, zombieNum, delay, overrideVal);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		zombieNames = valueDictionary.GetValueOrDefault("ZombieNames", new Array()).AsGodotArray();
		zombieNum = valueDictionary.GetValueOrDefault("ZombieNum", 1).AsInt32();
		Variant valueOrDefault = valueDictionary.GetValueOrDefault("Delay", new Array { -1, -1 });
		if (valueOrDefault.VariantType == Variant.Type.Array)
		{
			Array array = valueOrDefault.AsGodotArray();
			if (array.Count == 2)
			{
				delay = new Vector2((float)array[0].AsDouble(), (float)array[1].AsDouble());
			}
			else if (array.Count == 1)
			{
				delay = new Vector2((float)array[0].AsDouble(), (float)array[0].AsDouble());
			}
		}
		if (valueOrDefault.VariantType == Variant.Type.Float)
		{
			delay = Vector2.One * (float)valueOrDefault.AsDouble();
		}
		Dictionary dictionary = valueDictionary.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary();
		if (dictionary.Count > 0)
		{
			overrideVal = new TowerDefenseCharacterOverride();
			overrideVal.Init(dictionary);
		}
	}

	public override Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["EventName"] = "GravestoneSpawnZombie",
			["Value"] = new Dictionary
			{
				["ZombieNames"] = zombieNames,
				["ZombieNum"] = zombieNum,
				["Delay"] = new Array { delay.X, delay.Y }
			}
		};
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			((Dictionary)dictionary["Value"])["Override"] = overrideVal.Export();
		}
		return dictionary;
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["随机创建墓碑僵尸"] = new Dictionary
		{
			["数量"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Int",
				["Property"] = "zombieNum",
				["Rest"] = 5
			},
			["延时范围"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Vector2",
				["Property"] = "delay",
				["Rest"] = new Vector2(-1f, -1f)
			}
		};
		return property;
	}
}
