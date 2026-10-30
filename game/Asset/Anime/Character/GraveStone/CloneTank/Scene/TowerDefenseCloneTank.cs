using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefenseCloneTank : TowerDefenseGravestone
{
	private readonly HashSet<TowerDefenseZombie> _overlappingZombies = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _overlapScratch = new HashSet<TowerDefenseZombie>();

	private bool _triggered;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && !_triggered && !nearDie && !die)
		{
			ProcessHitBoxOverlaps();
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !inGame || !TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y);
		for (int i = 0; i < charactersIntersectingRectList.Count; i++)
		{
			if (charactersIntersectingRectList[i] is TowerDefenseZombie towerDefenseZombie && CanCloneZombie(towerDefenseZombie))
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

	private bool CanCloneZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if (zombie.gridPos.Y != base.gridPos.Y)
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		int maskFlags = zombie.instance.maskFlags;
		if ((maskFlags & 2) != 0)
		{
			return false;
		}
		if ((maskFlags & 0x10) != 0 && (maskFlags & 1) == 0)
		{
			return false;
		}
		return true;
	}

	private void HandleZombieEntered(TowerDefenseZombie zombie)
	{
		if (!_triggered)
		{
			_triggered = true;
			SpawnClonedZombie(zombie);
			die = true;
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		}
	}

	private void SpawnClonedZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(zombie.packet))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(zombie.packet.saveKey);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombie spawned = packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseZombie;
		if (!GodotObject.IsInstanceValid(spawned))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().AddChild(spawned, forceReadableName: false, InternalMode.Disabled);
		double riseDuration = GD.RandRange(0.4, 0.6);
		double hitpointScale = zombie.instance.hitpointScale;
		Vector2 scale = zombie.transformPoint.Scale;
		bool invisible = zombie.invisible;
		bool hypnoses = zombie.instance.hypnoses;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(spawned))
			{
				spawned.Rise(riseDuration);
				spawned.SetHitpointAndScale(hitpointScale, scale);
				spawned.invisible = invisible;
				if (hypnoses)
				{
					spawned.Hypnoses();
				}
			}
		}).CallDeferred();
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl) && GodotObject.IsInstanceValid(spawned))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, spawned);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetConfig.saveKey, base.gridPos.X, base.gridPos.Y, nextSyncId, hitpointScale, scale.X, hypnoses, riseDuration, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Shooting")
		{
			Destroy();
		}
		else
		{
			base.AnimeCompleted(clip);
		}
	}
}
