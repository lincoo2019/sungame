using Godot;

[Tool]
public partial class TowerDefenseRuneStonesLow : TowerDefenseGravestone
{
	private const string RUNE_STONES_WATER_1 = "uid://cjd6af321g5xm";

	private const string RUNE_STONES_WATER_2 = "uid://bwaqfa2ivxpg1";

	private const string RUNE_STONES_WATER_3 = "uid://d026tplpd0elk";

	private const string RUNE_STONES_WATER_4 = "uid://cfoggnl858a8o";

	private const string RUNE_STONES_WATER_5 = "uid://cbby5m82o0uy1";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				shadowSprite.Visible = false;
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://cjd6af321g5xm");
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (GodotObject.IsInstanceValid(cell) && cell.isWater)
		{
			switch (damagePointName)
			{
			case "Damage0":
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://cjd6af321g5xm");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://bwaqfa2ivxpg1");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://d026tplpd0elk");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://cfoggnl858a8o");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("RuneStones1_1.png", "uid://cbby5m82o0uy1");
				break;
			}
		}
	}
}
