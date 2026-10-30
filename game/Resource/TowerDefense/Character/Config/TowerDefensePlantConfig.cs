using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public bool canUsePlantfood = true;

	[Export(PropertyHint.None, "")]
	public Array<Vector2I> extendGrid = new Array<Vector2I>();

	[Export(PropertyHint.None, "")]
	public Dictionary<Vector2I, string> extendCoverDictionary = new Dictionary<Vector2I, string>();

	[Export(PropertyHint.None, "")]
	public bool izm2Fliter;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> coverEvents = new Array<TowerDefenseCharacterEventBase>();
}
