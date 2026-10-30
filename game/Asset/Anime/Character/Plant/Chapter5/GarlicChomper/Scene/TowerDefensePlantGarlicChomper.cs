using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGarlicChomper : TowerDefensePlant
{
	public ChomperComponent chomperComponent;

	public AttackComponent attackComponent;

	private double _chewTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double chewTime
	{
		get
		{
			return _chewTime;
		}
		set
		{
			_chewTime = value;
			if (IsNodeReady() && chomperComponent != null && !chomperComponent.IsReleased)
			{
				chomperComponent.chewTime = (float)value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.75f);
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
			if (GodotObject.IsInstanceValid(character))
			{
				character.Garlic();
			}
			break;
		case "Smash":
			Destroy();
			break;
		case "Chomp":
			Destroy();
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "chewTime", chewTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		chewTime = data.GetValueOrDefault("chewTime", 30.0).AsDouble();
	}
}
