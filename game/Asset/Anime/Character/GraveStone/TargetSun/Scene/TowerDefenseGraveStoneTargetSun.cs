using Godot;

[Tool]
public partial class TowerDefenseGraveStoneTargetSun : TowerDefenseGravestone
{
	public override async void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if ((double)GD.Randf() > 0.5)
		{
			SunCreate(logicalGlobalPosition, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
		else
		{
			SunCreate(logicalGlobalPosition, 15L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
