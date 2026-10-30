using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingWallnutSquash : TowerDefensePlantBowlingBase
{
	private SquashComponent _squashComponent;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_squashComponent = componentManager.GetRuntime<SquashComponent>();
		bowlingComponent.OnBowling += Bowling;
		if (base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantWallnutSquash");
			if (towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString() != "")
			{
				base.currentCustom = new Array<string> { towerDefensePacketValue["Key"].AsGodotDictionary()["Custom"].AsString() };
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
		moveComponent.MoveClear();
		bowlingComponent.isRoll = false;
		bowlingComponent.SetAlive(alive: false);
		base.sprite.loop = false;
		base.sprite.AddAnimation("Idle", 0.0);
		_squashComponent.Execute(character);
	}
}
