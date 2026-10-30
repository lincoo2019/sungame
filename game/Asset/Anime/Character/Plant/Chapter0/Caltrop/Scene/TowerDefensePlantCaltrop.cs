using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCaltrop : TowerDefensePlant
{
	private AttackComponent attackComponent;

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
			if (IsNodeReady())
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = value;
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
			attackComponent.OnAttack += ComponentAttack;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= ComponentAttack;
		}
	}

	public void ComponentAttack()
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
