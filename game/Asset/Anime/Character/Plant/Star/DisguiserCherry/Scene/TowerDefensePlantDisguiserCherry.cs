using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDisguiserCherry : TowerDefensePlant
{
	private static PackedScene _CHERRY_BOMB_EXPLOSION;

	private ExplodeComponent _explodeComponent;

	public double hpNext;

	public double hpNextInterval;

	public bool over;

	private static PackedScene CHERRY_BOMB_EXPLOSION => _CHERRY_BOMB_EXPLOSION ?? (_CHERRY_BOMB_EXPLOSION = GD.Load<PackedScene>("uid://cibtjjjomdxnh"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			hpNextInterval = instance.hitpoints / 6.0;
			hpNext = instance.hitpoints - hpNextInterval;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			while (instance.hitpoints <= hpNext)
			{
				hpNext -= hpNextInterval;
				_explodeComponent.Explode();
			}
		}
	}

	public override void DestroySet()
	{
		if (!isShovel && !over)
		{
			over = true;
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(CHERRY_BOMB_EXPLOSION, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			while (hpNext >= 0.0)
			{
				hpNext -= hpNextInterval;
				AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
				_explodeComponent.Explode();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hpNext", hpNext },
			{ "hpNextInterval", hpNextInterval },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hpNext = data.GetValueOrDefault("hpNext", Variant.From<double>(0.0)).AsDouble();
		hpNextInterval = data.GetValueOrDefault("hpNextInterval", Variant.From<double>(0.0)).AsDouble();
		over = data.GetValueOrDefault("over", Variant.From<bool>(false)).AsBool();
	}
}
