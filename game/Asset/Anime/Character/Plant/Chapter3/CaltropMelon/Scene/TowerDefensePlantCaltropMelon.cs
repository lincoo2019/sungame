using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCaltropMelon : TowerDefensePlant
{
	private CharacterTimerComponent timerComponent;

	private AttackComponent attackComponent;

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.5;

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
			timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			timerComponent.OnTimeout += Timeout;
			attackComponent.OnAttack += Attack;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent characterTimerComponent = timerComponent;
		if (characterTimerComponent != null && !characterTimerComponent.IsReleased)
		{
			timerComponent.OnTimeout -= Timeout;
		}
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= Attack;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !timerComponent.IsRunning("Destroy"))
		{
			timerComponent.Run("Destroy", 10.0);
		}
	}

	public void Attack()
	{
		attackComponent.AttackAllFlag(attack, 2);
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Destroy")
		{
			Destroy();
		}
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
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
