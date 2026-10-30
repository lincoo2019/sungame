using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectIceCobCannonExplode : TowerDefenseProjectileEffectBase
{
	private static PackedScene iceCobCannonExplosion;

	private static PackedScene ICE_COB_CANNON_EXPLOSION => iceCobCannonExplosion ?? (iceCobCannonExplosion = GD.Load<PackedScene>("uid://c0jqarake84mi"));

	public override async void _Ready()
	{
		EffectCreate();
		TowerDefenseExplode.CreateExplode(base.GlobalPosition, new Vector2(1.3f, 1.3f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, -1);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		QueueFree();
	}

	public void EffectCreate()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 3.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(ICE_COB_CANNON_EXPLOSION, gridPos);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		towerDefenseEffectParticlesOnce.GlobalPosition = base.GlobalPosition;
		if (GodotObject.IsInstanceValid(mapCell))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition = new Vector2(towerDefenseEffectParticlesOnce.GlobalPosition.X, towerDefenseEffectParticlesOnce.GlobalPosition.Y - (float)(mapCell.GetGroundHeight() - 30.0));
		}
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		AudioManager.Instance.AudioPlay("DoomShroom");
		TowerDefenseCharacter.CreateColdEffect(camp, gridPos);
	}
}
