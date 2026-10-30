using Godot;

[Tool]
[GlobalClass]
public partial class GPUParticles2DOnece : GpuParticles2D
{
	public override void _Ready()
	{
		if (!Engine.IsEditorHint() && !(GetParent() is TowerDefenseEffectParticlesOnce))
		{
			base.Finished += base.QueueFree;
			Init();
		}
	}

	public void Init()
	{
		Restart();
		foreach (Node child in GetChildren())
		{
			if (child is GpuParticles2D gpuParticles2D)
			{
				gpuParticles2D.Restart();
			}
		}
	}
}
