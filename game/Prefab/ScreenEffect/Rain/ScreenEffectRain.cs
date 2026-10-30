using Godot;

[GlobalClass]
public partial class ScreenEffectRain : ColorRect
{
	public double timer;

	public override void _Ready()
	{
		timer = 0.0;
		AudioManager.Instance.AudioPlay("Rain");
	}

	public override void _ExitTree()
	{
		AudioManager.Instance.MemberFind("Rain").Stop();
	}

	public override void _PhysicsProcess(double delta)
	{
		timer += delta;
		((ShaderMaterial)base.Material).SetShaderParameter("timer", timer);
	}
}
