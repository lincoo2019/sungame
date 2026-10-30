using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseGraveStoneTargetMagnet : TowerDefenseGravestone
{
	private ChangeProjectileStateComponent changeProjectileStateComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			changeProjectileStateComponent = componentManager.GetRuntime<ChangeProjectileStateComponent>();
			if (changeProjectileStateComponent?.checkShape?.Geometry is RectangleShape2D rectangleShape2D)
			{
				rectangleShape2D.Size = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(3f, 3f);
				changeProjectileStateComponent.checkShape.Enabled = true;
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Damage3":
		case "Damage4":
		case "Damage5":
			changeProjectileStateComponent?.SetAlive(alive: false);
			base.sprite.SetFliters(new Array { "wave1", "wave2", "shock" }, open: false);
			break;
		}
	}
}
