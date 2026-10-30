using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieMagnetic : TowerDefenseZombie
{
	private ChangeProjectileStateComponent _changeProjectileStateComponent;

	public double openTimer;

	public double openTime;

	public bool open;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_changeProjectileStateComponent = componentManager.GetRuntime<ChangeProjectileStateComponent>();
			if (_changeProjectileStateComponent?.checkShape?.Geometry is RectangleShape2D rectangleShape2D)
			{
				rectangleShape2D.Size = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(5f, 5f);
			}
			openTime = GD.RandRange(6.0, 12.0);
			if ((double)GD.Randf() > 0.5)
			{
				base.sprite.SetFliter("anim_tongue", open: true);
			}
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !base.IsInsideComponentBattlefield || open)
		{
			return;
		}
		if (openTimer < openTime)
		{
			openTimer += delta;
			return;
		}
		open = true;
		if (_changeProjectileStateComponent?.checkShape != null)
		{
			_changeProjectileStateComponent.checkShape.Enabled = true;
		}
		base.sprite.SetFliters(new Array { "wave2", "wave1", "Zombie_bodyater", "shock" }, open: true);
		base.sprite.SetFliters(new Array { "Zombie_bodyater2" }, open: false);
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (armorName == "HelmetMagnet" && stage == 1)
		{
			base.sprite.SetFliters(new Array { "shock" }, open: false);
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "HelmetMagnet" && _changeProjectileStateComponent?.checkShape?.Geometry is RectangleShape2D rectangleShape2D)
		{
			rectangleShape2D.Size = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(3f, 3f);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "openTimer", openTimer },
			{ "openTime", openTime },
			{ "open", open }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		openTimer = data.GetValueOrDefault("openTimer", 0.0).AsDouble();
		openTime = data.GetValueOrDefault("openTime", 0.0).AsDouble();
		open = data.GetValueOrDefault("open", false).AsBool();
		if (_changeProjectileStateComponent?.checkShape != null)
		{
			_changeProjectileStateComponent.checkShape.Enabled = open;
		}
	}
}
