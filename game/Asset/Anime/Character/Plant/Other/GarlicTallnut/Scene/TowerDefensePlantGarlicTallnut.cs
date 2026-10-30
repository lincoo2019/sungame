using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGarlicTallnut : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Damage1"))
		{
			if (damagePointName == "Damage2")
			{
				_explodeComponent.Explode();
			}
		}
		else
		{
			_explodeComponent.Explode();
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			_explodeComponent.Explode();
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			if (character.instance.ArmorHas("SpecialHelmet"))
			{
				SkipInvincibleHurt(num);
			}
			else if (type == "Eat")
			{
				SkipInvincibleHurt(Mathf.Max(40.0, num));
				character.Garlic();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = (bool)data.GetValueOrDefault("over", false);
	}
}
