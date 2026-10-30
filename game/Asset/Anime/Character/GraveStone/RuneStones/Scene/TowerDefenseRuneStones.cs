using Godot;

[Tool]
public partial class TowerDefenseRuneStones : TowerDefenseGravestone
{
	private const string RUNE_STONES_WATER_1 = "uid://drim0rxao2i1e";

	private const string RUNE_STONES_WATER_2 = "uid://bp8idfpt8pjls";

	private const string RUNE_STONES_WATER_3 = "uid://c4fmainbpf3b7";

	private const string RUNE_STONES_WATER_4 = "uid://cnjnq72j50hji";

	private const string RUNE_STONES_WATER_5 = "uid://daebeitjtpwfk";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				shadowSprite.Visible = false;
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://drim0rxao2i1e");
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
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://drim0rxao2i1e");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://bp8idfpt8pjls");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://c4fmainbpf3b7");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://cnjnq72j50hji");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("RuneStones3_1.png", "uid://daebeitjtpwfk");
				break;
			}
		}
	}
}
