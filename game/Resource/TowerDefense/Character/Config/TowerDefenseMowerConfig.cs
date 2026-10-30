using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseMowerConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public MowerConfig mowerConfig;
}
