using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBlover : TowerDefensePlant
{
	private BloverComponent bloverComponent;

	public bool run;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			bloverComponent = componentManager.GetRuntime<BloverComponent>();
			BloverComponent obj = bloverComponent;
			if (obj != null && !obj.IsReleased)
			{
				bloverComponent.OnBlowOver += BlowOver;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BloverComponent obj = bloverComponent;
		if (obj != null && !obj.IsReleased)
		{
			bloverComponent.OnBlowOver -= BlowOver;
		}
	}

	public override void IdleEntered()
	{
		if (inGame && TowerDefenseManager.CurrentControl.isGameRunning)
		{
			Run();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
		if (inGame && TowerDefenseManager.CurrentControl.isGameRunning && !run)
		{
			Run();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			bloverComponent.Execult();
		}
	}

	public void Run()
	{
		base.sprite.SetAnimation("Blow", loop: false, 0.2);
		base.sprite.AddAnimation("Loop", 0.0);
		instance.invincible = true;
		run = true;
	}

	public void BlowOver()
	{
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "run", run } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.GetValueOrDefault("run", false).AsBool();
	}
}
