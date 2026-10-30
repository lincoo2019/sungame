using Godot;

[Tool]
public partial class TowerDefensePlantSnowwood : TowerDefensePlant
{
	private PointLight2D light;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			light = GetNode<PointLight2D>("%Light");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}
}
