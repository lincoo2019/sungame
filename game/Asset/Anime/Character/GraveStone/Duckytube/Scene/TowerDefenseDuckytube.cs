using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseDuckytube : TowerDefenseGravestone
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				base.sprite.SetFliters(new Array { "Zombie_whitewater", "Zombie_whitewater_复制" }, open: true);
				shadowSprite.Visible = false;
				Transform2D screenTransform = GetViewport().GetScreenTransform();
				screenTransform.Origin = Vector2.Zero;
				SetSpriteGroupShaderParameter("discardDownPos", (screenTransform * (GetLogicalGlobalPosition(spriteGroup) + new Vector2(0f, 24f))).Y);
				base.groundHeight = 0.0;
			}
			else
			{
				ySpeed = -200.0;
				base.sprite.pause = true;
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		AudioManager.Instance.AudioPlay("BalloonPop");
	}
}
