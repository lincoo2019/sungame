using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseVaseConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.VASE_TYPE type;
}
