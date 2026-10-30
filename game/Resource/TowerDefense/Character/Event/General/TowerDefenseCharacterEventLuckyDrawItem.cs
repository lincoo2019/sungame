using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventLuckyDrawItem : Resource
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterEventBase _event;

	[Export(PropertyHint.None, "")]
	public double weight = 1.0;
}
