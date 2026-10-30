using Godot;

[Tool]
public partial class TowerDefensePlantMagnetsShroom : TowerDefensePlant
{
	private MagnetComponent _magnetComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			if (_magnetComponent == null)
			{
				GD.PushError("Magnets-shroom is missing its Magnet resource runtime.");
			}
		}
	}
}
