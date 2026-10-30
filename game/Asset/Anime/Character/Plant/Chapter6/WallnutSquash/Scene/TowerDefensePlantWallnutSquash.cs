using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWallnutSquash : TowerDefensePlant
{
	private SquashComponent _squashComponent;

	private AttackComponent _attackComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_squashComponent = componentManager.GetRuntime<SquashComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			instance.hitpointsEmpty -= OnHitpointsEmpty;
			instance.keepAlive = true;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
		if (!TryStartSquashDeathrattle())
		{
			SquashComponent squashComponent = _squashComponent;
			if (squashComponent != null && squashComponent.Alive && instance.hitpoints <= 0.0)
			{
				Destroy();
			}
		}
	}

	public void OnHitpointsEmpty()
	{
		Destroy();
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		TryStartSquashDeathrattle();
	}

	private bool TryStartSquashDeathrattle()
	{
		SquashComponent squashComponent = _squashComponent;
		if (squashComponent == null || squashComponent.IsReleased)
		{
			return false;
		}
		if (!_squashComponent.Alive && instance.hitpoints <= 0.0)
		{
			ToSquash();
			return true;
		}
		return false;
	}

	public void ToSquash()
	{
		instance.biteHurt = 0.0;
		instance.keepAlive = false;
		_squashComponent.SetAlive(alive: true);
		instance.hitpoints = 300.0;
		instance.die = false;
		instance.nearDie = false;
		nearDie = false;
		die = false;
		destroyComponent?.EndDeathSettlement();
	}
}
