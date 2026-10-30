using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseFloorQX : TowerDefenseGravestone
{
	private readonly HashSet<TowerDefenseZombie> _overlappingZombies = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _overlapScratch = new HashSet<TowerDefenseZombie>();

	private bool _triggered;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.canBeCollection = false;
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
			if (charactersIntersectingRectList[i] is TowerDefenseZombie towerDefenseZombie && CanProjectZombie(towerDefenseZombie))
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

	private bool CanProjectZombie(TowerDefenseZombie zombie)
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
		if ((zombie.instance.maskFlags & 2) != 0)
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
			SpawnHologramProjection(zombie, base.gridPos.Y - 1);
			SpawnHologramProjection(zombie, base.gridPos.Y + 1);
			die = true;
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		}
	}

	private void SpawnHologramProjection(TowerDefenseZombie sourceZombie, int row)
	{
		if (!GodotObject.IsInstanceValid(sourceZombie) || !GodotObject.IsInstanceValid(sourceZombie.packet) || row < 1 || row > TowerDefenseManager.Instance.GetMapGridNum().Y)
		{
			return;
		}
		Vector2I vector2I = new Vector2I(base.gridPos.X, row);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
		if (mapCell == null)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(sourceZombie.packet.saveKey);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(vector2I) / TowerDefenseManager.GetMapFeature().mapControl.GlobalScale.Y;
		TowerDefenseZombie projection = packetConfig.Create(pos, vector2I, base.groundHeight) as TowerDefenseZombie;
		if (!GodotObject.IsInstanceValid(projection))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().AddChild(projection, forceReadableName: false, InternalMode.Disabled);
		bool hypnoses = sourceZombie.instance.hypnoses;
		double hitpointScale = sourceZombie.instance.hitpointScale * 0.3;
		Vector2 scale = sourceZombie.transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(projection))
			{
				projection.Walk();
				projection.SetSpriteGroupShaderParameter("hologram", true);
				if (hypnoses)
				{
					projection.Hypnoses();
				}
				projection.instance.hologram = true;
				projection.SetHitpointAndScale(hitpointScale, scale);
				projection.invisible = sourceZombie.invisible;
			}
		}).CallDeferred();
		mapCell.ReleaseHologramGridOccupancy(projection);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl) && GodotObject.IsInstanceValid(projection))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, projection);
				Dictionary spawnState = new Dictionary { ["hologram"] = true };
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetConfig.saveKey, vector2I.X, vector2I.Y, nextSyncId, hitpointScale, scale.X, hypnoses, 0.0, useCreate: true, pos.X, pos.Y, walkAfterSpawn: true, base.groundHeight, "", spawnState);
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
