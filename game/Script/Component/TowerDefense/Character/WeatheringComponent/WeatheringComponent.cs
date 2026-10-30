using Godot;

public sealed class WeatheringComponent : CharacterComponentRuntime
{
	public TowerDefenseCrater parent;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseCrater;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
	}

	public void Processing(float delta)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent) || parent.stageMax <= 0 || !(parent.config is TowerDefenseCraterConfig towerDefenseCraterConfig))
		{
			return;
		}
		parent.dieDownTimer += delta;
		if (parent.dieDownTimer > towerDefenseCraterConfig.dieDownTime / (double)parent.stageMax * (double)(parent.stage + 1))
		{
			parent.stage++;
			if (parent.stage < parent.stageMax)
			{
				parent.SetFliter(parent.stage);
			}
			else
			{
				parent.DieDown();
			}
		}
	}
}
