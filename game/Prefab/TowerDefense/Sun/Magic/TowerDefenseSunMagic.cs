using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseSunMagic : TowerDefenseSunBase
{
	private const string PurifyEffectScenePath = "res://Asset/Anime/Effect/MagicSunBuff/MagicSunBuff.tscn";

	private const string PurifyEffectClip = "Fire";

	private const long PurifyCreateNum = 25L;

	private const double PurifyHealPercentage = 0.1;

	private static readonly string[] PurifyBuffKeys = new string[16]
	{
		"Frozen", "IceSpeedDown", "EMSpeedDown", "TimeMagic", "AttackSpeedDown", "Butter", "EMP", "Dizziness", "MagicImmobilize", "Sleep",
		"Squid", "Poisoning", "RedHeat", "Cherry", "Burn", "TabooBean"
	};

	private static PackedScene _purifyEffectScene;

	public bool zombieCamp;

	public override void _Ready()
	{
		base._Ready();
		base.dieDownTimer.Timeout += DieDown;
	}

	public override string GetGroupName()
	{
		return "MagicSun";
	}

	public override ObjectManagerConfig.OBJECT GetPoolKey()
	{
		return ObjectManagerConfig.OBJECT.SUN_MAGIC;
	}

	public override void OnCollectStart()
	{
		if (gridPos.X < 0 || gridPos.Y < 0)
		{
			gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.sprite.GlobalPosition);
		}
		Purify();
	}

	public override long GetCollectValue()
	{
		if (!zombieCamp)
		{
			return sunNum;
		}
		return -sunNum;
	}

	public override bool ShouldAutoCollect()
	{
		autoCollect = false;
		return base.ShouldAutoCollect();
	}

	public override bool OnDieDown()
	{
		return false;
	}

	public override void DieDown()
	{
		if (!isCollect)
		{
			if (GameSaveManager.Instance.GetFeatureValue("SunCollect") != 0)
			{
				Collection();
			}
			else
			{
				base.DieDown();
			}
		}
	}

	public override void OnRefresh()
	{
		gridPos = new Vector2I(-1, -1);
		zombieCamp = false;
	}

	public void Purify()
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				PurifyCell(TowerDefenseManager.GetMapCell(gridPos + new Vector2I(i, j)));
			}
		}
	}

	private TowerDefenseEnum.CHARACTER_CAMP GetSunCamp()
	{
		if (!zombieCamp)
		{
			return TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		return TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
	}

	private void PurifyCell(TowerDefenseCellInstance cell)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		List<TowerDefensePlant> list = CollectPurifyTargets(cell);
		if (list.Count == 0)
		{
			return;
		}
		bool flag = RemoveLadder(cell);
		TowerDefenseEnum.CHARACTER_CAMP sunCamp = GetSunCamp();
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefensePlant towerDefensePlant = list[i];
			if (GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.isDestroy && !towerDefensePlant.die)
			{
				bool flag2 = AlignPlantHypnoses(towerDefensePlant, sunCamp);
				if (GodotObject.IsInstanceValid(towerDefensePlant) && towerDefensePlant.camp == sunCamp)
				{
					flag2 |= RemoveAbnormalBuffs(towerDefensePlant);
				}
				if (flag2 || flag)
				{
					HealPurifiedPlant(towerDefensePlant);
					CreatePurifiedSun(towerDefensePlant);
					PlayPurifyEffect(towerDefensePlant);
				}
			}
		}
	}

	private List<TowerDefensePlant> CollectPurifyTargets(TowerDefenseCellInstance cell)
	{
		List<TowerDefensePlant> list = new List<TowerDefensePlant>();
		List<TowerDefenseCharacter> characterList = cell.characterList;
		for (int i = 0; i < characterList.Count; i++)
		{
			if (characterList[i] is TowerDefensePlant towerDefensePlant && !(towerDefensePlant is TowerDefensePlantBowlingBase))
			{
				list.Add(towerDefensePlant);
			}
		}
		return list;
	}

	private static bool AlignPlantHypnoses(TowerDefensePlant plant, TowerDefenseEnum.CHARACTER_CAMP sunCamp)
	{
		bool flag = sunCamp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		bool flag2 = HasHypnosesBuff(plant);
		if (flag2 == flag)
		{
			return false;
		}
		plant.Hypnoses();
		if (!GodotObject.IsInstanceValid(plant))
		{
			return false;
		}
		return HasHypnosesBuff(plant) != flag2;
	}

	private static bool HasHypnosesBuff(TowerDefensePlant plant)
	{
		BuffComponent buff = plant.buff;
		if (buff != null && !buff.IsReleased)
		{
			return buff.BuffHas("Hypnoses");
		}
		return false;
	}

	private static bool RemoveLadder(TowerDefenseCellInstance cell)
	{
		TowerDefenseCharacter characterLadder = cell.characterLadder;
		if (!GodotObject.IsInstanceValid(characterLadder))
		{
			return false;
		}
		characterLadder.Destroy();
		return true;
	}

	private static bool RemoveAbnormalBuffs(TowerDefensePlant plant)
	{
		BuffComponent buff = plant.buff;
		if (buff == null || buff.IsReleased)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < PurifyBuffKeys.Length; i++)
		{
			string key = PurifyBuffKeys[i];
			if (buff.BuffHas(key))
			{
				buff.DeleteBuff(key);
				result = true;
			}
		}
		return result;
	}

	private static void HealPurifiedPlant(TowerDefensePlant plant)
	{
		TowerDefenseCharacterInstance instance = plant.instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		double hitpointsSave = instance.hitpointsSave;
		if (hitpointsSave > 0.0)
		{
			double num = Math.Min(hitpointsSave * 0.1, hitpointsSave - instance.hitpoints);
			if (num > 0.0)
			{
				plant.Health(num);
			}
		}
	}

	private void CreatePurifiedSun(TowerDefensePlant plant)
	{
		TowerDefenseCharacterEventSunCreate.Run(plant, 25.0, _dieCreate: false, _mustForzen: false, _fromPacket: false, 1.0, _byCamp: false, zombieCamp);
	}

	private static void PlayPurifyEffect(TowerDefensePlant plant)
	{
		if (_purifyEffectScene == null)
		{
			_purifyEffectScene = GD.Load<PackedScene>("res://Asset/Anime/Effect/MagicSunBuff/MagicSunBuff.tscn");
		}
		if (!GodotObject.IsInstanceValid(_purifyEffectScene))
		{
			return;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(_purifyEffectScene, plant.gridPos, "Fire");
		if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
		{
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (!GodotObject.IsInstanceValid(characterNode))
			{
				towerDefenseEffectSpriteOnce.QueueFree();
				return;
			}
			characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.gridPos = plant.gridPos;
			towerDefenseEffectSpriteOnce.GlobalPosition = plant.GetLogicalGlobalPosition();
		}
	}
}
