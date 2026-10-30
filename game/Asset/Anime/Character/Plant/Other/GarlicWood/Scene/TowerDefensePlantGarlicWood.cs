using Godot;

[Tool]
public partial class TowerDefensePlantGarlicWood : TowerDefensePlant
{
	private PointLight2D _light;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		if (character.instance.ArmorHas("SpecialHelmet"))
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
