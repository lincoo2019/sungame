using Godot;

[Tool]
public partial class TowerDefensePlantHurtTallnut : TowerDefensePlant
{
	private CharacterTimerComponent _timerComponent;

	public double startHP = 2000.0;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			if (instance.hitpoints > startHP)
			{
				instance.DealHurt(instance.hitpoints - startHP, playSplatAudio: false);
			}
			if (!_timerComponent.IsRunning("Health"))
			{
				_timerComponent.Run("Health", 1.0);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && !IsDie())
		{
			_ = instance.hitpoints;
			_ = base.config.hitpoints;
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Health")
		{
			Health(0.01 * instance.hitpointsSave);
			if (instance.hitpoints >= instance.hitpointsSave)
			{
				instance.hitpoints = instance.hitpointsSave;
			}
			_timerComponent.Run("Health", 1.0);
		}
	}
}
