using Godot;

[Tool]
[GlobalClass]
public partial class PointLight2DFollowMudulate : PointLight2D
{
	[Export(PropertyHint.None, "")]
	public CanvasItem followNode { get; set; }

	[Export(PropertyHint.None, "")]
	public float saveEnergy { get; set; } = 1.5f;

	public override void _Ready()
	{
		saveEnergy = base.Energy;
		base.Energy = 0f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.Visible = true;
			if (!GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool())
			{
				base.Visible = false;
			}
		}
	}
}
