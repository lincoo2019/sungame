using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantBowlingBase : TowerDefensePlant
{
	public CharacterMoveComponent moveComponent;

	public WaterEnvironmentComponent waterEnvironmentComponent;

	public BowlingComponent bowlingComponent;

	public override void _Ready()
	{
		base._Ready();
		if (GodotObject.IsInstanceValid(componentManager))
		{
			moveComponent = componentManager.GetRuntime<CharacterMoveComponent>();
			waterEnvironmentComponent = componentManager.GetRuntime<WaterEnvironmentComponent>();
			bowlingComponent = componentManager.GetRuntime<BowlingComponent>();
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
			shadowComponent.saveShadowPosition = new Vector2(shadowComponent.saveShadowPosition.X, globalPositionForPhysicsFrame.Y + 30f);
			base.BatchUpdate(delta);
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
		}
	}

	public override void InWater()
	{
		base.InWater();
		CreateSplash();
		Destroy();
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.moveScale = (instance.hypnoses ? (-1.0) : 1.0);
		}
	}
}
