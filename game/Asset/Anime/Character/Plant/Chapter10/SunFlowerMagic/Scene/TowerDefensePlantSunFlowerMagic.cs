using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSunFlowerMagic : TowerDefensePlant
{
	private const string MagicSunType = "MagicSun";

	private const float MagicSunProduceInterval = 25f;

	private const int MagicSunProduceNum = 25;

	private ProduceComponent _produceComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_produceComponent = componentManager?.GetRuntime<ProduceComponent>();
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.produceType = "MagicSun";
				_produceComponent.produceInterval = 25f;
				_produceComponent.num = 25;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_produceComponent = null;
	}
}
