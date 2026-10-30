using Godot;

[GlobalClass]
public partial class AwardRay : Node2D
{
	private GpuParticles2D _awardRayFour;

	private GpuParticles2D _awardRayGlow;

	private GpuParticles2D _awardRay1;

	private GpuParticles2D _awardRay2;

	public override void _Ready()
	{
		_awardRayFour = GetNode<GpuParticles2D>("%AwardRayFour");
		_awardRayGlow = GetNode<GpuParticles2D>("%AwardRayGlow");
		_awardRay1 = GetNode<GpuParticles2D>("%AwardRay1");
		_awardRay2 = GetNode<GpuParticles2D>("%AwardRay2");
	}

	public async void Emit()
	{
		_awardRayFour.Restart();
		_awardRay1.Restart();
		_awardRay2.Restart();
		await ToSignal(GetTree().CreateTimer(5.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		_awardRayGlow.Restart();
	}
}
