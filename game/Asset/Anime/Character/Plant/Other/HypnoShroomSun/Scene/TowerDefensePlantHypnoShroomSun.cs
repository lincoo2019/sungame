using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoShroomSun : TowerDefensePlant
{
	public bool over;

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
				towerDefenseCharacterBuffHypnoses.enableSunProduce = true;
				towerDefenseCharacterBuffHypnoses.sunProduceInterval = 5.0;
				towerDefenseCharacterBuffHypnoses.sunProduceNum = 25;
				character.Hypnoses(-1.0, canFliter: true, towerDefenseCharacterBuffHypnoses);
			}
			Destroy();
		}
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
