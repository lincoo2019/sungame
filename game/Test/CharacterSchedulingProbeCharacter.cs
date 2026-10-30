using System.Collections.Generic;
using Godot;

public partial class CharacterSchedulingProbeCharacter : TowerDefenseCharacter
{
	public int ExistingPhysicsUpdateCount { get; private set; }

	public List<string> SchedulingTrace { get; } = new List<string>();

	public void ArmExistingPhysicsProbe(ulong physicsFrame)
	{
		ulong num = 5uL;
		randFreshIndex = (int)((num - physicsFrame % num) % num);
	}

	public void ProbePhysicsFrame(double delta, ulong physicsFrame)
	{
		PhysicsProcessWithFrame(delta, physicsFrame);
	}

	public override bool ShouldUpdateGridPos()
	{
		ExistingPhysicsUpdateCount++;
		SchedulingTrace.Add("existingPhysics");
		return false;
	}
}
