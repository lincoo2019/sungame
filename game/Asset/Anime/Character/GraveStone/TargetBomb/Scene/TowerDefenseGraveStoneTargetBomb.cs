using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseGraveStoneTargetBomb : TowerDefenseGravestone
{
	private static PackedScene _CHERRY_BOMB_EXPLOSION;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool boom;

	private static PackedScene CHERRY_BOMB_EXPLOSION => _CHERRY_BOMB_EXPLOSION ?? (_CHERRY_BOMB_EXPLOSION = GD.Load<PackedScene>("uid://cibtjjjomdxnh"));

	public override async void DestroySet()
	{
		if (!boom)
		{
			boom = true;
			ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
			Vector2 explosionPosition = GetLogicalGlobalPosition();
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(CHERRY_BOMB_EXPLOSION, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = explosionPosition;
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			TowerDefenseExplode.CreateExplode(explosionPosition, new Vector2(1.5f, 1.5f), eventList, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.NOONE, -1);
			AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
		}
	}
}
