using Godot;

public partial class ShieldHealthRuntimeOwnerStub : TowerDefensePlant
{
	public bool TreatAsOutsideComponentBattlefield { get; set; }

	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		TowerDefenseProcessModeDispatch.BeginFrame();
		ComponentManager obj = componentManager;
		if (obj != null && obj.HasRuntimePhysicsWork)
		{
			componentManager.TickRuntimePhysics(delta, Engine.GetPhysicsFrames(), !TreatAsOutsideComponentBattlefield && componentManager.CanDispatchOwnerGameplay);
		}
	}
}
