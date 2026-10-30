using System;

[Flags]
public enum StateMachineCallbackPhaseFlags
{
	None = 0,
	Enter = 1,
	Exit = 2,
	Process = 4,
	PhysicsProcess = 8,
	Guard = 0x10
}
