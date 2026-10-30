using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ShovelEventSealMagicConfig : ShovelEventConfig
{
	[Export(PropertyHint.None, "")]
	public double costPercentage = 0.03;

	public override void Execute(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && !(character.cost <= 0.0) && (!Global.Instance.isMultiplayerMode || MultiPlayerManager.Instance.isHost))
		{
			TowerDefenseCharacter towerDefenseCharacter = FindZombieNearestHome();
			if (towerDefenseCharacter != null)
			{
				TowerDefenseCharacterBuffSealMagic buffConfig = new TowerDefenseCharacterBuffSealMagic
				{
					time = character.cost * costPercentage
				};
				towerDefenseCharacter.buff.AddBuff(buffConfig);
			}
		}
	}

	private static TowerDefenseCharacter FindZombieNearestHome()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return null;
		}
		TowerDefenseCharacter result = null;
		float num = 1f / 0f;
		List<TowerDefenseCharacter> cleanCharactersList = instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (towerDefenseCharacter is TowerDefenseZombie && towerDefenseCharacter.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.instance.hypnoses && towerDefenseCharacter.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && (towerDefenseCharacter.buff == null || !towerDefenseCharacter.buff.BuffHas("SealMagic")) && TowerDefenseCharacterBuffSealMagic.CanCarrySeal(towerDefenseCharacter))
			{
				float x = towerDefenseCharacter.WorldHitRect.Position.X;
				if (x < num)
				{
					num = x;
					result = towerDefenseCharacter;
				}
			}
		}
		return result;
	}
}
