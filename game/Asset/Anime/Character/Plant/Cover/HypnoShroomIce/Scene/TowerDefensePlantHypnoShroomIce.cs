using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoShroomIce : TowerDefensePlant
{
	public bool over;

	private PointLight2D light;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			light = GetNode<PointLight2D>("%Light");
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (instance.sleep || over)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(character) && (character.instance.unUseBuffFlags & 8) != 0)
		{
			SkipInvincibleHurt(num);
		}
		else if (type == "Eat")
		{
			over = true;
			if (GodotObject.IsInstanceValid(character))
			{
				TowerDefenseCharacterBuffHypnoses towerDefenseCharacterBuffHypnoses = new TowerDefenseCharacterBuffHypnoses();
				towerDefenseCharacterBuffHypnoses.enableTorchwood = true;
				towerDefenseCharacterBuffHypnoses.torchwoodChangeName = "IceFire";
				towerDefenseCharacterBuffHypnoses.torchwoodAudio = "FireHit";
				towerDefenseCharacterBuffHypnoses.enableDeathFreeze = true;
				towerDefenseCharacterBuffHypnoses.deathFreezeTime = 3.0;
				towerDefenseCharacterBuffHypnoses.deathSlowTime = 15.0;
				towerDefenseCharacterBuffHypnoses.deathDamage = 1800.0;
				character.Hypnoses(-1.0, canFliter: true, towerDefenseCharacterBuffHypnoses);
			}
			Destroy();
		}
	}

	public override void DestroySet()
	{
		_explodeComponent.Explode();
		base.DestroySet();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
