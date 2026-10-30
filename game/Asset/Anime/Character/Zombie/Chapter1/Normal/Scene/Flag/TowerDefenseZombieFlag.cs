using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieFlag : TowerDefenseZombie
{
	private const string ZOMBIE_FLAG_2 = "uid://dxoshvi4ee5up";

	private const string ZOMBIE_FLAG_3 = "uid://dyi8c7kqovn5h";

	private AdobeAnimateSpriteBase _flag;

	public bool halfHp;

	public bool isAttack;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_flag = GetNode<AdobeAnimateSpriteBase>("%ZombieFlagpole");
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
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				_flag.SetAtlasReplace("Zombie_flag1.png", "uid://dyi8c7kqovn5h");
				_flag.Visible = false;
				base.sprite.SetFliters(new Array { "anim_innerarm1", "anim_innerarm2", "anim_innerarm3" }, open: true);
				base.sprite.SetFliters(new Array { "Zombie_flaghand", "Zombie_innerarm_screendoor" }, open: false);
			}
		}
		else
		{
			_flag.SetAtlasReplace("Zombie_flag1.png", "uid://dxoshvi4ee5up");
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			halfHp = true;
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		base.sprite.SetFliters(new Array { "Zombie_innerarm_screendoor" }, open: true);
		base.sprite.SetFliters(new Array { "anim_innerarm1", "anim_innerarm2", "anim_innerarm3" }, open: false);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
			else
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_screendoor" }, open: false);
			}
		}
	}

	public override void UnlimitedFireInit()
	{
		instance.hitpointScale *= 2.0;
		base.walkSpeedScale *= GD.RandRange(1.5, 3.0);
	}
}
