using Godot;

[Tool]
public partial class TowerDefenseItemRakeSun : TowerDefenseItemRakeBase
{
	protected override void ApplyHitEffect()
	{
		attackComponent.AttackExecute(10000.0);
		SunCreate(GetLogicalGlobalPosition(), 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
	}
}
