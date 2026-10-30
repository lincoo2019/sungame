using Godot;

[Tool]
public partial class TowerDefenseZombieYetiBobsledTeam : TowerDefenseZombieBobsledVehicleBase
{
	public override bool IsHardControlImmune => true;

	protected override bool UsesBreakAnimationCompletion => true;

	protected override string GetBreakClip()
	{
		return "Wheelie";
	}

	protected override string GetRidingClip()
	{
		return "Drive";
	}
}
