using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemMegaFire : TowerDefenseItem
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	private static readonly Array<TowerDefenseCharacter> _emptyCharacterList = new Array<TowerDefenseCharacter>();

	public bool over;

	private CharacterTimerComponent _timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.invincible = true;
			SetHitBoxMonitorable(monitorable: false);
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
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

	public void Timeout(string timerName)
	{
		if (timerName == "AutoDestroy")
		{
			Destroy();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame)
		{
			base.sprite.timeScale = timeScale;
			if (!_timerComponent.IsRunning("AutoDestroy"))
			{
				_timerComponent.Run("AutoDestroy", 15.0);
			}
			if (Engine.GetPhysicsFrames() % 2 == 0L)
			{
				TowerDefenseExplode.CreateExplode(GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame), new Vector2(0.5f, 0.25f), allEventList, _emptyCharacterList, TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
			}
		}
	}
}
