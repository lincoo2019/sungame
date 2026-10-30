using Godot;

[Tool]
public partial class TowerDefensePlantTallnutPS : TowerDefensePlant
{
	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			if (type != "Eat")
			{
				character.Hurt(Mathf.Min(1000.0, num), playSplatAudio: false);
			}
			else
			{
				character.Hurt(Mathf.Min(80.0, num / 3.0), playSplatAudio: false);
			}
		}
	}
}
