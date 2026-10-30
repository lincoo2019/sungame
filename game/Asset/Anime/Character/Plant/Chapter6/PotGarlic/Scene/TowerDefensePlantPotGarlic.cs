using Godot;

[Tool]
public partial class TowerDefensePlantPotGarlic : TowerDefensePlant
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.invincibleHurt = true;
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character) && character.instance.ArmorHas("SpecialHelmet"))
		{
			SkipInvincibleHurt(num);
			return;
		}
		switch (type)
		{
		case "Eat":
			SkipInvincibleHurt(Mathf.Max(10.0, num));
			character.Garlic();
			break;
		case "Smash":
			Destroy();
			break;
		case "Chomp":
			Destroy();
			break;
		}
	}
}
