using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceShroom : TowerDefensePlant
{
	public bool over;

	public bool run;

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.biteHurt = base.config.biteHurt;
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && inGame)
		{
			if (CanSleep())
			{
				Sleep();
				return;
			}
			instance.biteHurt = 0.0;
			base.sprite.SetAnimation("Idle", loop: false, 0.2);
			run = true;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 1.0;
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
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
				TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
				Destroy();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["run"] = run };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.ContainsKey("run") && data["run"].AsBool();
	}
}
