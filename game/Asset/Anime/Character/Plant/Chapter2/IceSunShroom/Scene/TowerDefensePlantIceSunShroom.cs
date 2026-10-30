using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceSunShroom : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool over;

	public bool run;

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && inGame)
		{
			if (CanSleep())
			{
				Sleep();
			}
			else if (IsIzmOneShotGateOpen())
			{
				Activate();
			}
		}
	}

	private void Activate()
	{
		if (!run)
		{
			instance.invincible = true;
			base.sprite.SetAnimation("Idle", loop: false, 0.2);
			run = true;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!run)
		{
			if (!IsIzmOneShotGateOpen())
			{
				base.sprite.timeScale = 0.0;
				return;
			}
			Activate();
		}
		base.sprite.timeScale = timeScale * 2.0;
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			timeScaleInit = timeScaleSave;
		}
	}

	public override void IdleExited()
	{
		base.IdleExited();
	}

	public override void AnimeCompleted(string clip)
	{
		if (inGame)
		{
			base.AnimeCompleted(clip);
			if (clip == "Idle" && run && !over)
			{
				over = true;
				TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos, eventList);
				Destroy();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "over", over },
			{ "run", run }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
		run = data.GetValueOrDefault("run", false).AsBool();
	}
}
