using Godot;

[Tool]
public partial class TowerDefensePlantCherryBean : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
		}
	}

	public override void DestroySet()
	{
		_explodeComponent.Explode();
		base.DestroySet();
	}
}
