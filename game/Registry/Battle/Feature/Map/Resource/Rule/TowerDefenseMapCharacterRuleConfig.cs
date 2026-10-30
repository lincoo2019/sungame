using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseMapCharacterRuleConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string characterName = "";

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double timeScaleMultiplier = 1.0;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double hitpointScaleMultiplier = 1.0;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double visualScaleMultiplier = 1.0;

	public bool Matches(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.config) && !string.IsNullOrWhiteSpace(characterName))
		{
			return string.Equals(character.config.name, characterName, StringComparison.Ordinal);
		}
		return false;
	}

	public void Apply(TowerDefenseCharacter character)
	{
		if (Matches(character))
		{
			if (!IsBossCharacter(character))
			{
				character.timeScaleInit *= timeScaleMultiplier;
			}
			if (GodotObject.IsInstanceValid(character.instance))
			{
				character.instance.hitpointScale *= hitpointScaleMultiplier;
			}
			if (GodotObject.IsInstanceValid(character.transformPoint))
			{
				character.transformPoint.Scale *= (float)visualScaleMultiplier;
			}
		}
	}

	private static bool IsBossCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.instance))
		{
			return character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		}
		if (character?.config is TowerDefenseZombieConfig towerDefenseZombieConfig)
		{
			return towerDefenseZombieConfig.physique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		}
		return false;
	}

	public bool TryValidateRuntime(out string reason)
	{
		if (string.IsNullOrWhiteSpace(characterName))
		{
			reason = "character modifier has no character name";
			return false;
		}
		if (!IsFiniteNonNegative(timeScaleMultiplier) || !IsFiniteNonNegative(hitpointScaleMultiplier) || !IsFiniteNonNegative(visualScaleMultiplier))
		{
			reason = "character modifier '" + characterName + "' contains a non-finite or negative multiplier";
			return false;
		}
		reason = "";
		return true;
	}

	private static bool IsFiniteNonNegative(double value)
	{
		if (double.IsFinite(value))
		{
			return value >= 0.0;
		}
		return false;
	}
}
