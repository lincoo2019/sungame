using Godot;

[GlobalClass]
public partial class CandyThrowParticles : GpuParticles2D
{
	public override void _Ready()
	{
		Restart();
		base.Finished += base.QueueFree;
	}
}
