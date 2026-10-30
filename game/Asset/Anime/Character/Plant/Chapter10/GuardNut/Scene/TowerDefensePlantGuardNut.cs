using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGuardNut : TowerDefensePlant
{
	private const string ShieldArmorName = "GuardNutShield";

	private AttackComponent _attackComponent;

	private double _attackIntervalDefault;

	private bool _shieldBroken;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame)
		{
			return;
		}
		_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack += Attack;
			_attackIntervalDefault = _attackComponent.attackInterval;
			if (_shieldBroken)
			{
				ApplyShieldBrokenSpeedUp();
			}
		}
	}

	public override void _ExitTree()
	{
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
		base._ExitTree();
	}

	private void Attack()
	{
		AudioManager.Instance?.AudioPlay("Stab");
		_attackComponent.AttackEventExecute();
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (!(armorName != "GuardNutShield"))
		{
			_shieldBroken = true;
			ApplyShieldBrokenSpeedUp();
		}
	}

	private void ApplyShieldBrokenSpeedUp()
	{
		if (_attackIntervalDefault <= 0.0)
		{
			return;
		}
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			double num = _attackIntervalDefault * 0.5;
			if (!(_attackComponent.attackInterval <= num))
			{
				_attackComponent.attackInterval = num;
				_attackComponent.Refresh();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["shieldBroken"] = _shieldBroken };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		if (data.GetValueOrDefault("shieldBroken", false).AsBool())
		{
			_shieldBroken = true;
			ApplyShieldBrokenSpeedUp();
		}
	}
}
