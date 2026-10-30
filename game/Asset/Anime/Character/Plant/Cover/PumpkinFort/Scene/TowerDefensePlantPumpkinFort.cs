using Godot;

[Tool]
public partial class TowerDefensePlantPumpkinFort : TowerDefensePlant
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !inGame && !editorMapPreviewMode)
		{
			PumpkinFortSprite pumpkinFortSprite = base.sprite as PumpkinFortSprite;
			if (GodotObject.IsInstanceValid(pumpkinFortSprite) && GodotObject.IsInstanceValid(pumpkinFortSprite.back))
			{
				pumpkinFortSprite.back.ZIndex = 0;
			}
		}
	}
}
