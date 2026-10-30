using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieHypno : TowerDefenseZombie
{
	public bool halfHp;

	public bool isAttack;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			if ((double)GD.Randf() > 0.5)
			{
				base.sprite.SetFliter("anim_tongue", open: true);
			}
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Arm")
		{
			halfHp = true;
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		DestroySet();
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("HypnoPuff");
			towerDefenseProjectileCreateData.baseDamage = 0.0;
			towerDefenseProjectileCreateData.fireMethodFlags = 32;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (spriteGroup.Scale.X < 0f),
				trackSearchIntervalOverride = 1
			};
			FireComponent.CreateProjectilePositionByData(null, null, 0.0, GetLogicalGlobalPosition(), new Vector2(300f * base.Scale.X, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides);
		}
	}
}
