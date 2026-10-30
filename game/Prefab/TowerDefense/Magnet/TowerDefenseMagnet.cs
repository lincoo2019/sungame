using Godot;

[GlobalClass]
public partial class TowerDefenseMagnet : TowerDefenseGroundItemBase
{
	private static PackedScene _towerDefenseMagnetScene;

	public TowerDefenseArmorInstance armorInstance;

	public Node adsorbedObject;

	private static PackedScene TowerDefenseMagnetScene => _towerDefenseMagnetScene ?? (_towerDefenseMagnetScene = GD.Load<PackedScene>("uid://baxlrxaix6r2b"));

	public static TowerDefenseMagnet Create(TowerDefenseArmorInstance _armorInstance)
	{
		TowerDefenseMagnet towerDefenseMagnet = TowerDefenseMagnetScene.Instantiate<TowerDefenseMagnet>(PackedScene.GenEditState.Disabled);
		towerDefenseMagnet.armorInstance = _armorInstance;
		return towerDefenseMagnet;
	}

	public void Init(Node2D node)
	{
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(adsorbedObject))
		{
			QueueFree();
		}
	}
}
