using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectDoomCobCannonExplode : TowerDefenseProjectileEffectBase
{
	private static PackedScene doomShroomExplosion;

	private static PackedScene DOOM_SHROOM_EXPLOSION => doomShroomExplosion ?? (doomShroomExplosion = GD.Load<PackedScene>("uid://0hfxonqijrv0"));

	public override async void _Ready()
	{
		EffectCreate();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		QueueFree();
	}

	public async void EffectCreate()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		ViewManager.Instance.FullScreenColorBlink(Colors.DarkSlateBlue, 0.5, rise: false);
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(DOOM_SHROOM_EXPLOSION, gridPos);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		towerDefenseEffectParticlesOnce.GlobalPosition = base.GlobalPosition;
		if (GodotObject.IsInstanceValid(mapCell))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition -= new Vector2(0f, (float)(mapCell.GetGroundHeight() - 30.0));
		}
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		TowerDefenseExplode.CreateExplode(base.GlobalPosition, new Vector2(3.5f, 3.5f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, -1);
		AudioManager.Instance.AudioPlay("ExplodeDoomShroom");
		TowerDefenseManager.GetPacketConfig("CraterDayGround").Plant(gridPos, playAudio: false, noLimit: true);
	}
}
