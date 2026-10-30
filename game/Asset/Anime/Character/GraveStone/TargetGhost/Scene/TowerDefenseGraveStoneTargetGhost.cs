using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseGraveStoneTargetGhost : TowerDefenseGravestone
{
	private readonly HashSet<TowerDefenseZombie> _overlappingZombies = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _overlapScratch = new HashSet<TowerDefenseZombie>();

	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			ProcessHitBoxOverlaps();
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		_overlapScratch.Clear();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !inGame || nearDie || die || !TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y);
		for (int i = 0; i < charactersIntersectingRectList.Count; i++)
		{
			if (charactersIntersectingRectList[i] is TowerDefenseZombie towerDefenseZombie && CanGhostZombie(towerDefenseZombie))
			{
				_overlapScratch.Add(towerDefenseZombie);
				if (!_overlappingZombies.Contains(towerDefenseZombie))
				{
					HandleZombieEntered(towerDefenseZombie);
				}
			}
		}
		_overlappingZombies.RemoveWhere((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || !_overlapScratch.Contains(zombie));
		foreach (TowerDefenseZombie item in _overlapScratch)
		{
			_overlappingZombies.Add(item);
		}
	}

	private bool CanGhostZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if (zombie.hasGhost)
		{
			return false;
		}
		int maskFlags = zombie.instance.maskFlags;
		if ((maskFlags & 0x10) != 0 && (maskFlags & 1) == 0)
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (zombie.camp != base.camp)
		{
			return false;
		}
		if (!zombie.targetRegistrationComponent.canCarry)
		{
			return false;
		}
		if (zombie.gridPos.Y != base.gridPos.Y)
		{
			return false;
		}
		return true;
	}

	private async void HandleZombieEntered(TowerDefenseZombie zombie)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGhost");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombieGhost ghostZombie = packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseZombieGhost;
		if (!GodotObject.IsInstanceValid(ghostZombie))
		{
			return;
		}
		ghostZombie.carryCharacter = zombie;
		TowerDefenseManager.GetCharacterNode().AddChild(ghostZombie, forceReadableName: false, InternalMode.Disabled);
		double riseDuration = GD.RandRange(0.4, 0.6);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(ghostZombie))
			{
				ghostZombie.Rise(riseDuration);
			}
		}).CallDeferred();
		Hurt(200.0);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, ghostZombie);
				Dictionary spawnState = new Dictionary
				{
					["carrierSyncId"] = zombie.syncId,
					["ghostTimeScaleSave"] = ghostZombie.ghostTimeScaleSave
				};
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGhost", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, hypnoses: false, riseDuration, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight, "", spawnState);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
