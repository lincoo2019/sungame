using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseCharacterEventTeleport : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public PackedScene teleportEffectScene;

	[Export(PropertyHint.None, "")]
	public PackedScene eraseEffectScene;

	[Export(PropertyHint.None, "")]
	public double bossDamage = 500.0;

	[Export(PropertyHint.None, "")]
	public double hpPercentDamage = 0.05;

	[Export(PropertyHint.None, "")]
	public double eraseHpRatio = 0.1;

	[Export(PropertyHint.None, "")]
	public int teleportLength = 1;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, teleportEffectScene, eraseEffectScene, bossDamage, hpPercentDamage, eraseHpRatio, teleportLength);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, teleportEffectScene, eraseEffectScene, bossDamage * delta, hpPercentDamage * delta, eraseHpRatio, teleportLength);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, teleportEffectScene, eraseEffectScene, bossDamage, hpPercentDamage, eraseHpRatio, teleportLength);
	}

	public override void ExecuteGroundProject(TowerDefenseProjectile projectile, Vector2 position, Vector2I gridPos)
	{
		Run(projectile.GlobalPosition, null, teleportEffectScene, eraseEffectScene, bossDamage, hpPercentDamage, eraseHpRatio, teleportLength);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, PackedScene teleportEffectScene, PackedScene eraseEffectScene, double bossDamage, double hpPercentDamage, double eraseHpRatio, int teleportLength)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(target.instance) || target.isDestroy)
		{
			return;
		}
		Vector2I gridPos = target.gridPos;
		double totalHitPoint = target.GetTotalHitPoint();
		double currentHitPoint = target.GetCurrentHitPoint();
		double num = ((totalHitPoint > 0.0) ? (currentHitPoint / totalHitPoint) : 0.0);
		bool flag = target is TowerDefenseZombie && target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		bool flag2 = GodotObject.IsInstanceValid(target.instance) && target.instance.hitpoints > target.instance.hitpointsSave;
		bool flag3 = !flag && !flag2 && num <= eraseHpRatio;
		SpawnTeleportEffect(gridPos, teleportEffectScene);
		if (flag3)
		{
			SpawnEraseEffect(gridPos, eraseEffectScene);
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		if (flag)
		{
			TowerDefenseManager.Instance.damagePipeline.ApplyHurt(target, bossDamage, playSplatAudio: false);
			return;
		}
		if (flag3)
		{
			EraseTarget(target, eraseEffectScene);
			return;
		}
		double num2 = totalHitPoint * hpPercentDamage;
		TowerDefenseManager.Instance.damagePipeline.ApplyHurt(target, num2, playSplatAudio: false);
		if (target is TowerDefenseZombie && GodotObject.IsInstanceValid(target) && !target.isDestroy)
		{
			bool reverseDirection = GodotObject.IsInstanceValid(target.instance) && target.instance.hypnoses;
			TeleportZombie(target, reverseDirection, teleportLength);
		}
	}

	private static void TeleportZombie(TowerDefenseCharacter target, bool reverseDirection, int teleportLength)
	{
		int num = Mathf.Max(0, teleportLength);
		if (num != 0 && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			if (!reverseDirection)
			{
				TeleportAwayFromHouse(target, num);
			}
			else
			{
				TeleportTowardHouse(target, num);
			}
		}
	}

	private static void TeleportAwayFromHouse(TowerDefenseCharacter target, int length)
	{
		if (target.gridPos.X >= 1)
		{
			Vector2I gridPos = (target.gridPos = new Vector2I(target.gridPos.X + length, target.gridPos.Y));
			target.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(gridPos));
		}
	}

	private static void TeleportTowardHouse(TowerDefenseCharacter target, int length)
	{
		Vector2I gridPos = (target.gridPos = new Vector2I(target.gridPos.X - length, target.gridPos.Y));
		target.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(gridPos));
	}

	private static void EraseTarget(TowerDefenseCharacter target, PackedScene eraseEffectScene)
	{
		if (GodotObject.IsInstanceValid(target))
		{
			target.skipDestroySet = true;
			TowerDefenseCellInstance towerDefenseCellInstance = (GodotObject.IsInstanceValid(target.cell) ? target.cell : TowerDefenseManager.GetMapCell(target.gridPos));
			if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
			{
				towerDefenseCellInstance.RemoveCharacter(target);
			}
			target.Destroy(freeInstance: false);
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.CharacterUnregister(target);
			}
			target.RemoveFromGroup("Character");
			if (!target.IsQueuedForDeletion())
			{
				target.QueueFree();
			}
		}
	}

	private static void SpawnTeleportEffect(Vector2I gridPos, PackedScene teleportEffectScene)
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(characterNode) && GodotObject.IsInstanceValid(teleportEffectScene))
		{
			TowerDefenseEffectBase towerDefenseEffectBase = TowerDefenseManager.CreateEffectSpriteOnce(teleportEffectScene, gridPos, "Idle");
			if (GodotObject.IsInstanceValid(towerDefenseEffectBase))
			{
				characterNode.AddChild(towerDefenseEffectBase, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
			}
		}
	}

	private static void SpawnEraseEffect(Vector2I gridPos, PackedScene eraseEffectScene)
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(characterNode) && GodotObject.IsInstanceValid(eraseEffectScene))
		{
			TowerDefenseEffectBase towerDefenseEffectBase = TowerDefenseManager.CreateEffectParticlesOnce(eraseEffectScene, gridPos);
			if (GodotObject.IsInstanceValid(towerDefenseEffectBase))
			{
				characterNode.AddChild(towerDefenseEffectBase, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
			}
		}
	}
}
