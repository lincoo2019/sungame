using System;

[Flags]
public enum StateMachineProcessFlags
{
	None = 0,
	Process = 1,
	PhysicsProcess = 2,
	Input = 4,
	UnhandledInput = 8
}
