using Godot;

public partial class ComponentAliveSchedulingProbe : ComponentBase
{
	public int PhysicsProcessCount { get; private set; }

	public override void _PhysicsProcess(double delta)
	{
		PhysicsProcessCount++;
	}
}
