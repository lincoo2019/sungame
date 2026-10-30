using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class TowerDefenseRuneMagicUtil
{
	public static readonly Color PurpleColor = new Color(0.66f, 0.2f, 0.94f);

	public static readonly Color BlueColor = new Color(0.18f, 0.36f, 1f);

	public static readonly Color RedColor = new Color(1f, 0.23f, 0.19f);

	public static readonly Color OrangeColor = new Color(1f, 0.58f, 0f);

	public static readonly Color GreenColor = new Color(0.2f, 0.78f, 0.35f);

	public const double HasteTimeScaleValue = 2.0;

	public const double StormDamagePercentPerSecond = 0.05;

	public const double DefaultMagicHealPerSecond = 50.0;

	public const int StormDamageFlags = 67;

	public const double BuffDurationPadding = 0.5;

	private const string DizzinessBuffKey = "Dizziness";

	private static readonly TowerDefenseRuneMagicColor[] StormColorPool = new TowerDefenseRuneMagicColor[5]
	{
		TowerDefenseRuneMagicColor.Purple,
		TowerDefenseRuneMagicColor.Blue,
		TowerDefenseRuneMagicColor.Red,
		TowerDefenseRuneMagicColor.Orange,
		TowerDefenseRuneMagicColor.Green
	};

	private static readonly TowerDefenseRuneMagicColor[] FogColorPool = new TowerDefenseRuneMagicColor[4]
	{
		TowerDefenseRuneMagicColor.Purple,
		TowerDefenseRuneMagicColor.Blue,
		TowerDefenseRuneMagicColor.Red,
		TowerDefenseRuneMagicColor.Green
	};

	public const float FogTintWhiten = 0.45f;

	public const float StormTintWhiten = 0.45f;

	public const int GuardianShieldCellCount = 5;

	public const int GuardianShieldZombieCount = 5;

	public const double GuardianShieldHp = 500.0;

	public static Color ToColor(TowerDefenseRuneMagicColor color)
	{
		return color switch
		{
			TowerDefenseRuneMagicColor.Purple => PurpleColor, 
			TowerDefenseRuneMagicColor.Blue => BlueColor, 
			TowerDefenseRuneMagicColor.Red => RedColor, 
			TowerDefenseRuneMagicColor.Orange => OrangeColor, 
			TowerDefenseRuneMagicColor.Green => GreenColor, 
			_ => Colors.White, 
		};
	}

	public static Color ToFogTint(TowerDefenseRuneMagicColor color)
	{
		return TintTowardsWhite(ToColor(color), 0.45f);
	}

	public static Color ToStormTint(TowerDefenseRuneMagicColor color)
	{
		return TintTowardsWhite(ToColor(color), 0.45f);
	}

	public static Color ToFogTint(Color rawColor)
	{
		return TintTowardsWhite(rawColor, 0.45f);
	}

	public static Color UntintFogColor(Color tintedColor)
	{
		return UntintFromWhite(tintedColor, 0.45f);
	}

	private static Color TintTowardsWhite(Color color, float whiten)
	{
		return Colors.White.Lerp(color, 1f - Mathf.Clamp(whiten, 0f, 1f));
	}

	private static Color UntintFromWhite(Color color, float whiten)
	{
		float num = Mathf.Clamp(whiten, 0f, 0.999f);
		float num2 = 1f - num;
		return new Color(Mathf.Clamp((color.R - num) / num2, 0f, 1f), Mathf.Clamp((color.G - num) / num2, 0f, 1f), Mathf.Clamp((color.B - num) / num2, 0f, 1f), color.A);
	}

	public static string ToKey(TowerDefenseRuneMagicColor color)
	{
		return color switch
		{
			TowerDefenseRuneMagicColor.Purple => "Purple", 
			TowerDefenseRuneMagicColor.Blue => "Blue", 
			TowerDefenseRuneMagicColor.Red => "Red", 
			TowerDefenseRuneMagicColor.Orange => "Orange", 
			TowerDefenseRuneMagicColor.Green => "Green", 
			_ => "", 
		};
	}

	public static bool TryParse(string text, out TowerDefenseRuneMagicColor color)
	{
		color = TowerDefenseRuneMagicColor.Purple;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		switch (text.Trim().ToLowerInvariant())
		{
		case "purple":
			color = TowerDefenseRuneMagicColor.Purple;
			return true;
		case "blue":
			color = TowerDefenseRuneMagicColor.Blue;
			return true;
		case "red":
			color = TowerDefenseRuneMagicColor.Red;
			return true;
		case "orange":
			color = TowerDefenseRuneMagicColor.Orange;
			return true;
		case "green":
			color = TowerDefenseRuneMagicColor.Green;
			return true;
		default:
			return false;
		}
	}

	public static TowerDefenseRuneMagicColor RandomStormColor()
	{
		return StormColorPool[GD.RandRange(0, StormColorPool.Length - 1)];
	}

	public static TowerDefenseRuneMagicColor RandomFogColor()
	{
		return FogColorPool[GD.RandRange(0, FogColorPool.Length - 1)];
	}

	public static TowerDefenseRuneMagicColor RandomFogColorExcept(TowerDefenseRuneMagicColor current)
	{
		int val = System.Array.IndexOf(FogColorPool, current);
		return FogColorPool[(Math.Max(0, val) + 1 + GD.RandRange(0, FogColorPool.Length - 2)) % FogColorPool.Length];
	}

	public static bool IsValidTarget(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(character.instance))
		{
			return false;
		}
		if (!character.die && !character.nearDie)
		{
			return !character.isDestroy;
		}
		return false;
	}

	public static bool IsBoss(TowerDefenseCharacter character)
	{
		if (character is TowerDefenseZombie && GodotObject.IsInstanceValid(character.instance))
		{
			return character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		}
		return false;
	}

	public static void ForEachPlant(Action<TowerDefenseCharacter> action)
	{
		ForEachInGroup(action, zombie: false);
	}

	public static void ForEachZombie(Action<TowerDefenseCharacter> action)
	{
		ForEachInGroup(action, zombie: true);
	}

	private static void ForEachInGroup(Action<TowerDefenseCharacter> action, bool zombie)
	{
		if (action == null)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		Godot.Collections.Array array = (zombie ? instance.GetZombie() : instance.GetPlant());
		for (int i = 0; i < array.Count; i++)
		{
			if (array[i].AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && IsValidTarget(towerDefenseCharacter))
			{
				action(towerDefenseCharacter);
			}
		}
	}

	public static void ApplySleep(TowerDefenseCharacter character, double time)
	{
		if (IsValidTarget(character) && !(time <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			if (character.buff.BuffGet("Sleep") is TowerDefenseCharacterBuffSleep towerDefenseCharacterBuffSleep)
			{
				towerDefenseCharacterBuffSleep.time = Math.Max(towerDefenseCharacterBuffSleep.time, towerDefenseCharacterBuffSleep.currentTime + time);
				return;
			}
			TowerDefenseCharacterBuffSleep towerDefenseCharacterBuffSleep2 = new TowerDefenseCharacterBuffSleep();
			towerDefenseCharacterBuffSleep2.time = time;
			character.buff.AddBuff(towerDefenseCharacterBuffSleep2);
		}
	}

	public static void ApplyDizzy(TowerDefenseCharacter character, double time)
	{
		if (IsValidTarget(character) && !(time <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			if (character.buff.BuffGet("Dizziness") is TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness)
			{
				towerDefenseCharacterBuffDizziness.time = Math.Max(towerDefenseCharacterBuffDizziness.time, towerDefenseCharacterBuffDizziness.currentTime + time);
				return;
			}
			TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness2 = new TowerDefenseCharacterBuffDizziness();
			towerDefenseCharacterBuffDizziness2.time = time;
			character.buff.AddBuff(towerDefenseCharacterBuffDizziness2);
		}
	}

	public static void ApplySlowHalf(TowerDefenseCharacter character, double time)
	{
		ApplyRuneBuff(character, "RuneStormSlow", time, 0.5);
	}

	public static void ApplyHasteDouble(TowerDefenseCharacter character, double time)
	{
		ApplyRuneBuff(character, "RuneFogHaste", time, 2.0);
	}

	public static void ApplyIceSlow(TowerDefenseCharacter character, double time)
	{
		if (IsValidTarget(character) && !(time <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			if (character.buff.BuffGet("IceSpeedDown") is TowerDefenseCharacterBuffIceSpeedDown towerDefenseCharacterBuffIceSpeedDown)
			{
				towerDefenseCharacterBuffIceSpeedDown.time = Math.Max(towerDefenseCharacterBuffIceSpeedDown.time, towerDefenseCharacterBuffIceSpeedDown.currentTime + time);
				return;
			}
			character.buff.AddBuff(new TowerDefenseCharacterBuffIceSpeedDown
			{
				time = time
			});
		}
	}

	private static void ApplyRuneBuff(TowerDefenseCharacter character, string key, double time, double multiplier)
	{
		if (IsValidTarget(character) && !(time <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			if (character.buff.BuffGet(key) is TowerDefenseCharacterBuffRuneMagic towerDefenseCharacterBuffRuneMagic)
			{
				towerDefenseCharacterBuffRuneMagic.time = Math.Max(towerDefenseCharacterBuffRuneMagic.time, towerDefenseCharacterBuffRuneMagic.currentTime + time);
				return;
			}
			character.buff.AddBuff(new TowerDefenseCharacterBuffRuneMagic
			{
				key = key,
				time = time,
				timeScaleValue = multiplier
			});
		}
	}

	public static void ApplyMagicDamage(TowerDefenseCharacter character, double num)
	{
		if (IsValidTarget(character) && !(num <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			character.FlagHurt(num, 67);
		}
	}

	public static double GetRuneMagicDamage(TowerDefenseCharacter character, double percentPerSecond, double delta)
	{
		if (percentPerSecond <= 0.0 || delta <= 0.0 || !IsValidTarget(character))
		{
			return 0.0;
		}
		TowerDefenseCharacterInstance instance = character.instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return 0.0;
		}
		double projectileDamageableDurability = instance.GetProjectileDamageableDurability(67);
		return Math.Max(0.0, projectileDamageableDurability) * percentPerSecond * delta;
	}

	public static void ApplyHeal(TowerDefenseCharacter character, double num)
	{
		if (IsValidTarget(character) && !(num <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			character.Health(num);
			TowerDefenseCharacterInstance instance = character.instance;
			if (GodotObject.IsInstanceValid(instance) && instance.hitpoints > instance.hitpointsSave)
			{
				instance.hitpoints = instance.hitpointsSave;
			}
		}
	}

	public static void ApplyCellShield(TowerDefenseCellInstance cell, double hp)
	{
		if (GodotObject.IsInstanceValid(cell) && !(hp <= 0.0) && TowerDefenseManager.HasGameplayAuthority)
		{
			TowerDefenseItemSheild.CreateOnCellWithHP(cell, new StringName("Default"), hp);
		}
	}

	public static void ApplyDizzyImmune(TowerDefenseCharacter character, double time)
	{
		if (IsValidTarget(character) && TowerDefenseManager.HasGameplayAuthority)
		{
			ApplyRuneBuff(character, "RuneFogDizzyImmune", time, 1.0);
			character.buff.DeleteBuff("Dizziness");
		}
	}

	public static void ApplyStormInstant(TowerDefenseRuneMagicColor color, double duration)
	{
		if (color == TowerDefenseRuneMagicColor.Orange)
		{
			ApplyGuardianStorm();
		}
		else
		{
			ApplyStormContinuous(color, duration);
		}
	}

	public static void ApplyStormContinuous(TowerDefenseRuneMagicColor color, double remaining)
	{
		switch (color)
		{
		case TowerDefenseRuneMagicColor.Purple:
			ForEachPlant(delegate(TowerDefenseCharacter plant)
			{
				ApplySleep(plant, remaining);
			});
			ForEachZombie(delegate(TowerDefenseCharacter zombie)
			{
				if (!IsBoss(zombie))
				{
					ApplyDizzy(zombie, remaining);
				}
			});
			break;
		case TowerDefenseRuneMagicColor.Blue:
			ForEachPlant(delegate(TowerDefenseCharacter plant)
			{
				ApplySlowHalf(plant, remaining);
			});
			ForEachZombie(delegate(TowerDefenseCharacter zombie)
			{
				if (!IsBoss(zombie))
				{
					ApplySlowHalf(zombie, remaining);
				}
			});
			break;
		}
	}

	public static void ApplyStormPerSecond(TowerDefenseRuneMagicColor color, double damagePercentPerSecond, double healPerSecond)
	{
		switch (color)
		{
		case TowerDefenseRuneMagicColor.Red:
			if (!(damagePercentPerSecond <= 0.0))
			{
				ForEachPlant(delegate(TowerDefenseCharacter plant)
				{
					ApplyMagicDamage(plant, GetRuneMagicDamage(plant, damagePercentPerSecond, 1.0));
				});
				ForEachZombie(delegate(TowerDefenseCharacter zombie)
				{
					ApplyMagicDamage(zombie, GetRuneMagicDamage(zombie, damagePercentPerSecond, 1.0));
				});
			}
			break;
		case TowerDefenseRuneMagicColor.Green:
			if (!(healPerSecond <= 0.0))
			{
				ForEachPlant(delegate(TowerDefenseCharacter plant)
				{
					ApplyHeal(plant, healPerSecond);
				});
				ForEachZombie(delegate(TowerDefenseCharacter zombie)
				{
					ApplyHeal(zombie, healPerSecond);
				});
			}
			break;
		}
	}

	public static void ApplyGuardianStorm()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			ApplyRandomCellShields(5, 500.0);
			ApplyRandomZombieShields(5, 500.0);
		}
	}

	private static void ApplyRandomCellShields(int count, double hp)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || !GodotObject.IsInstanceValid(mapFeature.mapConfig))
		{
			return;
		}
		Vector2I gridNum = mapFeature.mapConfig.gridNum;
		if (gridNum.X <= 0 || gridNum.Y <= 0)
		{
			return;
		}
		List<TowerDefenseCellInstance> list = new List<TowerDefenseCellInstance>();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, j));
				if (GodotObject.IsInstanceValid(mapCell))
				{
					list.Add(mapCell);
				}
			}
		}
		for (int k = 0; k < Math.Min(count, list.Count); k++)
		{
			int num = GD.RandRange(k, list.Count - 1);
			List<TowerDefenseCellInstance> list2 = list;
			int index = k;
			int index2 = num;
			TowerDefenseCellInstance value = list[num];
			TowerDefenseCellInstance value2 = list[k];
			list2[index] = value;
			list[index2] = value2;
			ApplyCellShield(list[k], hp);
		}
	}

	private static void ApplyRandomZombieShields(int count, double hp)
	{
		List<TowerDefenseCharacter> candidates = new List<TowerDefenseCharacter>();
		ForEachZombie(delegate(TowerDefenseCharacter zombie)
		{
			if (!IsBoss(zombie))
			{
				candidates.Add(zombie);
			}
		});
		if (candidates.Count != 0)
		{
			int num = Mathf.Min(count, candidates.Count);
			for (int num2 = 0; num2 < num; num2++)
			{
				int num3 = GD.RandRange(num2, candidates.Count - 1);
				List<TowerDefenseCharacter> list = candidates;
				int index = num2;
				List<TowerDefenseCharacter> list2 = candidates;
				int index2 = num3;
				TowerDefenseCharacter value = candidates[num3];
				TowerDefenseCharacter value2 = candidates[num2];
				list[index] = value;
				list2[index2] = value2;
				candidates[num2].instance.hitpointsSave += hp;
				candidates[num2].Health(hp);
			}
		}
	}

	public static void ApplyFogEffect(TowerDefenseRuneMagicColor color, TowerDefenseCharacter character, double refreshTime, double damagePercentPerSecond, double healPerSecond, double tickDelta)
	{
		if (!IsValidTarget(character) || character.instance.hologram)
		{
			return;
		}
		switch (color)
		{
		case TowerDefenseRuneMagicColor.Purple:
			if (character is TowerDefensePlant)
			{
				ApplySleep(character, refreshTime);
			}
			else if (character is TowerDefenseZombie)
			{
				ApplyDizzyImmune(character, refreshTime);
			}
			break;
		case TowerDefenseRuneMagicColor.Blue:
			if (character is TowerDefensePlant)
			{
				ApplyIceSlow(character, refreshTime);
			}
			else
			{
				ApplyHasteDouble(character, refreshTime);
			}
			break;
		case TowerDefenseRuneMagicColor.Red:
			ApplyMagicDamage(character, GetRuneMagicDamage(character, damagePercentPerSecond, tickDelta));
			break;
		case TowerDefenseRuneMagicColor.Green:
			if (healPerSecond > 0.0 && tickDelta > 0.0)
			{
				ApplyHeal(character, healPerSecond * tickDelta);
			}
			break;
		case TowerDefenseRuneMagicColor.Orange:
			break;
		}
	}
}
