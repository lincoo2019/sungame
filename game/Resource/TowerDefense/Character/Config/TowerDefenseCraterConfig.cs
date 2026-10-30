using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseCraterConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public double dieDownTime = 60.0;

	[Export(PropertyHint.None, "")]
	public Array<string> dieDownFliters = new Array<string>();
}
