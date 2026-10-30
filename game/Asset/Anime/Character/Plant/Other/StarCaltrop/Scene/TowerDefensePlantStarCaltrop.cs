using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantStarCaltrop : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private FireComponent _fireComponent;

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "StarCaltrop";

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
			if (IsNodeReady() && _fireComponent != null && _attackComponent != null)
			{
				_fireComponent.fireInterval = (float)_fireInterval;
				_attackComponent.attackInterval = _fireInterval;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				_fireComponent.fireNum = _fireNum;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_attackComponent.OnAttack += Attack;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
	}

	public void Attack()
	{
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		_attackComponent.AttackAllFlag(attack, 2);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attack", attack },
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attack = (double)data.GetValueOrDefault("attack", 20.0);
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "StarCaltrop");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 1.5);
	}
}
