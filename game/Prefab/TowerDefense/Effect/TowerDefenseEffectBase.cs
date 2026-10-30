using Godot;

[GlobalClass]
public partial class TowerDefenseEffectBase : TowerDefenseGroundItemBase
{
	public override void _Ready()
	{
		base._Ready();
		AddToGroup("Effect", persistent: true);
	}
}
