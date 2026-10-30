using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantTorchwoodStar : TowerDefensePlant
{
	private PointLight2D _light;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}
}
