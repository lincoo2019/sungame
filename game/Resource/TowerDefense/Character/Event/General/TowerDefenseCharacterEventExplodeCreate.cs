using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventExplodeCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public PackedScene effectScene;

	[Export(PropertyHint.None, "")]
	public string effectAudio = "";

	[Export(PropertyHint.None, "")]
	public Vector2 explodeSize = new Vector2(0.5f, 0.5f);

	[Export(PropertyHint.None, "")]
	public bool onlyLastPenetrateHit = true;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> explodeEvents;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		if (!onlyLastPenetrateHit || projectile.penetrateNum <= 0)
		{
			Run(projectile.GlobalPosition, target, projectile.camp, projectile.collisionFlags);
		}
	}

	public void Run(Vector2 pos, TowerDefenseCharacter target, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, int collisionFlags = 0)
	{
		if (effectScene != null)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(effectScene, target.gridPos, "Idle");
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				Node2D characterNode = TowerDefenseManager.GetCharacterNode();
				if (GodotObject.IsInstanceValid(characterNode))
				{
					characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
					towerDefenseEffectSpriteOnce.gridPos = TowerDefenseManager.Instance.GetMapGridPos(pos);
					towerDefenseEffectSpriteOnce.GlobalPosition = pos;
				}
				else
				{
					towerDefenseEffectSpriteOnce.QueueFree();
				}
			}
		}
		if (!string.IsNullOrEmpty(effectAudio))
		{
			AudioManager.Instance.AudioPlay(effectAudio);
		}
		if (explodeEvents != null && explodeEvents.Count > 0)
		{
			TowerDefenseExplode.CreateExplode(target.GetLogicalGlobalPosition(), explodeSize, explodeEvents, null, camp, collisionFlags);
		}
	}
}
