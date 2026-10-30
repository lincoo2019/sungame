using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantCaltropSnow : TowerDefensePlant
{
	public AttackComponent attackComponent;

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.0;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && attackComponent != null)
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.OnAttack += Attack;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= Attack;
		}
	}

	public override void DestroySet()
	{
		if (!(instance.hitpoints > 0.0))
		{
			TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
		}
	}

	public virtual void Attack()
	{
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		attackComponent.AttackAllFlag(attack, 2);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attack", attack },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attack = data.GetValueOrDefault("attack", 20.0).AsDouble();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.0).AsDouble();
	}
}
