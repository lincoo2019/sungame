using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormal : TowerDefenseZombie
{
	public bool halfHp;

	public bool isAttack;

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

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Arm")
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

	public override void UnlimitedFireInit()
	{
		instance.hitpointScale *= 2.0;
		base.walkSpeedScale *= GD.RandRange(1.5, 3.0);
	}
}
