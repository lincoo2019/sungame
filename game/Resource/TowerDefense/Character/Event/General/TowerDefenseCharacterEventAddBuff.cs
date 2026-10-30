using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventAddBuff : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterBuffConfig> buffList = new Array<TowerDefenseCharacterBuffConfig>();

	private static Texture2D _BUTTER_BLUEBERRY_SPLAT;

	private static Texture2D BUTTER_BLUEBERRY_SPLAT => _BUTTER_BLUEBERRY_SPLAT ?? (_BUTTER_BLUEBERRY_SPLAT = GD.Load<Texture2D>("uid://1xg4dycli5xw"));

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, buffList);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, buffList);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, buffList, projectile);
	}

	public static void Run(TowerDefenseCharacter target, Array<TowerDefenseCharacterBuffConfig> buffList, TowerDefenseProjectile projectile = null)
	{
		if (!GodotObject.IsInstanceValid(target) || (projectile != null && target.ProjectileEffectsBlocked(projectile.damageFlags, projectile.fireMethodFlags)))
		{
			return;
		}
		bool flag = projectile != null && projectile.config?.skinName == new StringName("Blueberry");
		foreach (TowerDefenseCharacterBuffConfig buff in buffList)
		{
			if (buff == null || TryShieldBlockCharmBuff(target, buff))
			{
				continue;
			}
			if (buff is TowerDefenseCharacterBuffHypnoses towerDefenseCharacterBuffHypnoses)
			{
				target.Hypnoses(towerDefenseCharacterBuffHypnoses.time);
				continue;
			}
			if (buff is TowerDefenseCharacterBuffFireHit source)
			{
				target.buff.ApplyFireHit(source);
				continue;
			}
			TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = buff.CreateRuntimeInstance();
			if (towerDefenseCharacterBuffConfig != null)
			{
				if (flag && towerDefenseCharacterBuffConfig is TowerDefenseCharacterBuffButter towerDefenseCharacterBuffButter)
				{
					towerDefenseCharacterBuffButter.splatTexture = BUTTER_BLUEBERRY_SPLAT;
				}
				target.buff.AddBuff(towerDefenseCharacterBuffConfig);
			}
		}
	}

	private static bool TryShieldBlockCharmBuff(TowerDefenseCharacter target, TowerDefenseCharacterBuffConfig buff)
	{
		if (!(buff is TowerDefenseCharacterBuffSleep) && !(buff is TowerDefenseCharacterBuffHypnoses))
		{
			return false;
		}
		if (!(target is TowerDefensePlant))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(target.cell) || !GodotObject.IsInstanceValid(target.cell.itemShield))
		{
			return false;
		}
		TowerDefenseItemSheild itemShield = target.cell.itemShield;
		if (itemShield == target)
		{
			return false;
		}
		if (itemShield.instance.hypnoses != target.instance.hypnoses)
		{
			return false;
		}
		return itemShield.ShieldBlockCharm();
	}
}
