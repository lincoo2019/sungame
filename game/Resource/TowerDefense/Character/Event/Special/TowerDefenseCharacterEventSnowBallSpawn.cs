using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventSnowBallSpawn : TowerDefenseCharacterEventBase
{
	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void Init(Dictionary valueDictionary)
	{
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "SnowBallSpawn",
			["Value"] = new Dictionary()
		};
	}

	public static void Run(TowerDefenseCharacter target)
	{
		if (target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemSnowBall");
		Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
		TowerDefenseCharacter ball = packetConfig.Create(logicalGlobalPosition, target.gridPos, target.groundHeight);
		characterNode.CallDeferred("add_child", ball);
		string size = "";
		bool isZombie = target is TowerDefenseZombie;
		bool needHypnoses = isZombie && target.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		if (isZombie)
		{
			if (target.instance.zombiePhysique <= TowerDefenseEnum.ZOMBIE_PHYSIQUE.SMALL)
			{
				size = "Small";
			}
			else if (target.instance.zombiePhysique <= TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL)
			{
				size = "Normal";
			}
			else
			{
				size = "Large";
			}
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(ball) && isZombie)
			{
				if (size != "" && ball is TowerDefenseItemSnowBall towerDefenseItemSnowBall)
				{
					towerDefenseItemSnowBall.SetSize(size);
				}
				if (needHypnoses)
				{
					Callable.From(delegate
					{
						if (GodotObject.IsInstanceValid(ball))
						{
							ball.Hypnoses();
						}
					}).CallDeferred();
				}
			}
		}).CallDeferred();
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, ball);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ItemSnowBall", target.gridPos.X, target.gridPos.Y, nextSyncId, 1.0, 1.0, needHypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, target.groundHeight, size);
			}
		}
	}
}
