using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseGravestoneConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public bool isChests;

	[Export(PropertyHint.None, "")]
	public bool blocksPlanting;
}
