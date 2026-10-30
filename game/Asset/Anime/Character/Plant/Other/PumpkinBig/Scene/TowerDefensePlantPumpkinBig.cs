using Godot;

[Tool]
public partial class TowerDefensePlantPumpkinBig : TowerDefensePlant
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !inGame && !editorMapPreviewMode)
		{
			((PumpkinBigSprite)base.sprite).back.ZIndex = 0;
		}
	}
}
