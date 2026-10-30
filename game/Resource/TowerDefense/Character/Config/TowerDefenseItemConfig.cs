using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseItemConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public bool isLadder;

	[Export(PropertyHint.None, "")]
	public bool isShield;
}
