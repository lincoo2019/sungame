using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectGroom : TowerDefenseProjectileEffectBase
{
	private static PackedScene gloomSplatsScene;

	public int num = 4;

	private static PackedScene GLOOM_SPLATS_SCENE => gloomSplatsScene ?? (gloomSplatsScene = GD.Load<PackedScene>("uid://djchpnx8iwbpe"));

	public override void _Ready()
	{
		GetNode<Timer>("Timer").Timeout += AttackCreate;
		AttackCreate();
	}

	public void AttackCreate()
	{
		num--;
		AudioManager.Instance.AudioPlay("SplatNormal");
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = GLOOM_SPLATS_SCENE.Instantiate<TowerDefenseEffectParticlesOnce>(PackedScene.GenEditState.Disabled);
		towerDefenseEffectParticlesOnce.objectId = ObjectManagerConfig.OBJECT.NOONE;
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		towerDefenseEffectParticlesOnce.Refresh();
		towerDefenseEffectParticlesOnce.gridPos = gridPos;
		if (!GodotObject.IsInstanceValid(target))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition = base.GlobalPosition - new Vector2(0f, (float)height);
			TowerDefenseExplode.CreateExplode(base.GlobalPosition - new Vector2(0f, (float)height), new Vector2(1.25f, 1.25f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, collisionFlag);
		}
		else
		{
			Vector2 pos = (towerDefenseEffectParticlesOnce.GlobalPosition = target.GetLogicalGlobalPosition(target.transformPoint) - new Vector2(0f, 50f));
			TowerDefenseExplode.CreateExplode(pos, new Vector2(1.25f, 1.25f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, collisionFlag);
		}
		if (num <= 0)
		{
			QueueFree();
		}
	}
}
