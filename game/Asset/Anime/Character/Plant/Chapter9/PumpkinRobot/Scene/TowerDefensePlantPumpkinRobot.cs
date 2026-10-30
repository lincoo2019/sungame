using Godot;

[Tool]
public partial class TowerDefensePlantPumpkinRobot : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	private const double MaxHp = 8000.0;

	private const double HpPerDeathrattle = 2000.0;

	private bool _destroySetOver;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_explodeComponent = componentManager.GetRuntime<ExplodeComponent>("character.explode");
		if (!inGame && !editorMapPreviewMode)
		{
			PumpkinSprite pumpkinSprite = base.sprite as PumpkinSprite;
			if (GodotObject.IsInstanceValid(pumpkinSprite) && GodotObject.IsInstanceValid(pumpkinSprite.back))
			{
				pumpkinSprite.back.ZIndex = 0;
			}
		}
	}

	public override void DestroySet()
	{
		if (_destroySetOver)
		{
			return;
		}
		_destroySetOver = true;
		if (!isShovel)
		{
			if (_explodeComponent != null && !_explodeComponent.IsReleased)
			{
				_explodeComponent.Explode();
			}
			StrengthenOtherPumpkinRobots();
		}
		base.DestroySet();
	}

	private void StrengthenOtherPumpkinRobots()
	{
		if (base.config == null)
		{
			return;
		}
		foreach (Node item in GetTree().GetNodesInGroup(base.config.name))
		{
			if (item is TowerDefensePlantPumpkinRobot towerDefensePlantPumpkinRobot && towerDefensePlantPumpkinRobot != this && GodotObject.IsInstanceValid(towerDefensePlantPumpkinRobot))
			{
				towerDefensePlantPumpkinRobot.instance.hitpointsSave = Mathf.Min(8000.0, towerDefensePlantPumpkinRobot.instance.hitpointsSave + 2000.0);
				towerDefensePlantPumpkinRobot.Health(2000.0);
				towerDefensePlantPumpkinRobot.instance.hitpoints = Mathf.Min(towerDefensePlantPumpkinRobot.instance.hitpoints, towerDefensePlantPumpkinRobot.instance.hitpointsSave);
			}
		}
	}
}
