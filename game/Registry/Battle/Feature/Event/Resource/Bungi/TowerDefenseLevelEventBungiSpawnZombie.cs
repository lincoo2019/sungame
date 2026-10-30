using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventBungiSpawnZombie : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public Array zombieNames = new Array { "ZombieNormal", "ZombieNormalCone", "ZombieNormalBucket" };

	[Export(PropertyHint.None, "")]
	public int zombieNum = 5;

	[Export(PropertyHint.None, "")]
	public Vector2 delay = new Vector2(-1f, -1f);

	public TowerDefenseCharacterOverride overrideVal;

	[Export(PropertyHint.None, "")]
	public Vector4I spawnPos = new Vector4I(3, 1, 9, 5);

	[Export(PropertyHint.None, "")]
	public bool hypnoses;

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
		return "LEVLE_EVENT_BUNGI_SPAWN_ZOMBIE";
	}

	public override void Execute()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			if (GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance))
			{
				TowerDefenseBattleFeatureWave.Instance.BungiSpawnZombie(zombieNames, zombieNum, delay, overrideVal, spawnPos, hypnoses);
			}
			else
			{
				ScheduleWithoutWave();
			}
		}
	}

	private void ScheduleWithoutWave()
	{
		TowerDefenseControlNew battleControl = TowerDefenseManager.CurrentControl;
		if (!GodotObject.IsInstanceValid(battleControl) || zombieNum <= 0)
		{
			return;
		}
		Array<WeightPickItemBase> weightPick = new Array<WeightPickItemBase>();
		foreach (Variant zombieName in zombieNames)
		{
			string text = (string)zombieName;
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
			if (GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig is TowerDefenseZombieConfig towerDefenseZombieConfig)
			{
				int num = ((packetConfig.overrideWeight != -1) ? packetConfig.overrideWeight : towerDefenseZombieConfig.weight);
				if (num > 0)
				{
					weightPick.Add(new WeightPickItemBase(text, num));
				}
			}
		}
		if (weightPick.Count == 0)
		{
			return;
		}
		Array<Vector2I> spawnPositions = new Array<Vector2I>();
		for (int i = spawnPos.X; i <= spawnPos.Z; i++)
		{
			for (int j = spawnPos.Y; j <= spawnPos.W; j++)
			{
				spawnPositions.Add(new Vector2I(i, j));
			}
		}
		int num2 = Mathf.Min(spawnPositions.Count, zombieNum);
		if (num2 <= 0)
		{
			return;
		}
		SceneTree tree = battleControl.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		int parentOperationId = battleControl.BeginPendingBattleOperation();
		int pendingCallbacks = num2;
		double num3 = Mathf.Min(delay.X, delay.Y);
		double to = Mathf.Max(delay.X, delay.Y);
		for (int k = 0; k < num2; k++)
		{
			double timeSec = Mathf.Max(0.0, GD.RandRange(num3, to));
			tree.CreateTimer(timeSec, processAlways: false).Timeout += delegate
			{
				try
				{
					if (GodotObject.IsInstanceValid(battleControl) && battleControl.IsPendingBattleOperationCurrent(parentOperationId) && GodotObject.IsInstanceValid(battleControl.levelControl) && !battleControl.levelControl.awardCreate && spawnPositions.Count != 0)
					{
						WeightPickItemBase weightPickItemBase = WeightPickMathine.Pick(weightPick);
						Vector2I vector2I = spawnPositions.PickRandom();
						spawnPositions.Remove(vector2I);
						TowerDefenseManager.Instance.BungiSpawn(weightPickItemBase.item.AsString(), vector2I, overrideVal, hypnoses);
					}
				}
				finally
				{
					pendingCallbacks--;
					if (pendingCallbacks == 0 && GodotObject.IsInstanceValid(battleControl))
					{
						battleControl.CompletePendingBattleOperation(parentOperationId);
					}
				}
			};
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
		Dictionary dictionary2 = valueDictionary.GetValueOrDefault("SpawnPos", new Dictionary()).AsGodotDictionary();
		spawnPos = new Vector4I(dictionary2.GetValueOrDefault("x", -1).AsInt32(), dictionary2.GetValueOrDefault("y", -1).AsInt32(), dictionary2.GetValueOrDefault("z", -1).AsInt32(), dictionary2.GetValueOrDefault("w", -1).AsInt32());
		hypnoses = valueDictionary.GetValueOrDefault("Hypnoses", false).AsBool();
	}

	public override Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["EventName"] = "BungiSpawnZombie",
			["Value"] = new Dictionary
			{
				["ZombieNames"] = zombieNames,
				["ZombieNum"] = zombieNum,
				["Delay"] = new Array { delay.X, delay.Y },
				["SpawnPos"] = new Dictionary
				{
					["x"] = spawnPos.X,
					["y"] = spawnPos.Y,
					["z"] = spawnPos.Z,
					["w"] = spawnPos.W
				},
				["Hypnoses"] = hypnoses
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
		property["随机蹦极放置墓碑僵尸"] = new Dictionary
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
			},
			["范围"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Vector4I",
				["Property"] = "spawnPos",
				["Rest"] = new Vector4I(3, 1, 9, 5)
			},
			["是否魅惑"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Bool",
				["Property"] = "hypnoses",
				["Rest"] = false
			}
		};
		return property;
	}
}
