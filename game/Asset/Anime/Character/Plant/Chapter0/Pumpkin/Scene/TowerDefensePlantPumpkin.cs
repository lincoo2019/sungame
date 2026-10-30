using Godot;

[Tool]
public partial class TowerDefensePlantPumpkin : TowerDefensePlant
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !inGame && !editorMapPreviewMode)
		{
			PumpkinSprite pumpkinSprite = base.sprite as PumpkinSprite;
			if (GodotObject.IsInstanceValid(pumpkinSprite) && GodotObject.IsInstanceValid(pumpkinSprite.back))
			{
				pumpkinSprite.back.ZIndex = 0;
			}
		}
	}
}
