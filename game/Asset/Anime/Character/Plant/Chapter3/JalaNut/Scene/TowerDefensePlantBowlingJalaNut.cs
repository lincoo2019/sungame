using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingJalaNut : TowerDefensePlantBowlingBase
{
	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		bowlingComponent.OnBowling += Bowling;
		if (base.config.customData != null)
		{
			string text = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantJalaNut").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
				.GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "")
			{
				base.currentCustom = new Array<string> { text };
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
		}
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		Explode();
		Destroy();
	}

	public void Explode()
	{
		TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 1800.0);
	}
}
