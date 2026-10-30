using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoBlover : TowerDefensePlant
{
	private BloverComponent _bloverComponent;

	public bool run;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bloverComponent = componentManager.GetRuntime<BloverComponent>();
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.OnBlowOver += BlowOver;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BloverComponent bloverComponent = _bloverComponent;
		if (bloverComponent != null && !bloverComponent.IsReleased)
		{
			_bloverComponent.OnBlowOver -= BlowOver;
		}
	}

	public override void IdleEntered()
	{
		if (inGame && TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			Run();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
		if (inGame)
		{
			_ = TowerDefenseManager.Instance.currentControl.isGameRunning;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			_bloverComponent.Execult();
		}
	}

	public void Run()
	{
		base.sprite.SetAnimation("Blow", loop: false, 0.2);
		base.sprite.AddAnimation("Loop", 0.0);
		instance.invincible = true;
		foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(base.camp))
		{
			TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
			if (towerDefenseCharacter is TowerDefensePlant)
			{
				towerDefenseCharacter.Hypnoses();
			}
		}
		run = true;
	}

	public void BlowOver()
	{
		Destroy();
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
