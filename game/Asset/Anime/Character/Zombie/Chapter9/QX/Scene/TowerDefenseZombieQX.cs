using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieQX : TowerDefenseZombie
{
	private HashSet<TowerDefenseZombie> _livingTracked = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _deathHandled = new HashSet<TowerDefenseZombie>();

	private readonly List<TowerDefenseCharacter> _candidates = new List<TowerDefenseCharacter>();

	private HashSet<TowerDefenseZombie> _currentAlive = new HashSet<TowerDefenseZombie>();

	private Vector2 _checkSize;

	private bool _projecting;

	private TowerDefenseZombie _projectingSource;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_projecting = false;
			_checkSize = TowerDefenseManager.Instance.GetMapGridSize() * 2.75f;
		}
	}

	public override void _ExitTree()
	{
		_livingTracked.Clear();
		_currentAlive.Clear();
		_deathHandled.Clear();
		_candidates.Clear();
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !TowerDefenseManager.Instance.IsGameRunning() || !inGame || _projecting || nearDie || die)
		{
			return;
		}
		if (!base.IsInsideComponentBattlefield)
		{
			if (_livingTracked.Count > 0)
			{
				_livingTracked.Clear();
			}
			if (_deathHandled.Count > 0)
			{
				_deathHandled.Clear();
			}
		}
		else
		{
			ScanRangeDeaths();
		}
	}

	private void ScanRangeDeaths()
	{
		_candidates.Clear();
		Rect2 checkRect = AabbShapeUtil.RectFromCenter(GetLogicalGlobalPosition(), _checkSize);
		TowerDefenseManager.Instance.characterRegistry.FillCharactersIntersectingRectListForCamp(checkRect, base.camp, _candidates);
		_currentAlive.Clear();
		for (int i = 0; i < _candidates.Count; i++)
		{
			if (!(_candidates[i] is TowerDefenseZombie towerDefenseZombie) || !CanWatchZombie(towerDefenseZombie))
			{
				continue;
			}
			if (towerDefenseZombie.die || towerDefenseZombie.nearDie)
			{
				if (_livingTracked.Contains(towerDefenseZombie) && _deathHandled.Add(towerDefenseZombie))
				{
					TriggerProjection(towerDefenseZombie);
					return;
				}
			}
			else
			{
				_currentAlive.Add(towerDefenseZombie);
			}
		}
		HashSet<TowerDefenseZombie> currentAlive = _currentAlive;
		HashSet<TowerDefenseZombie> livingTracked = _livingTracked;
		_livingTracked = currentAlive;
		_currentAlive = livingTracked;
	}

	private bool CanWatchZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie == this)
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = zombie.instance;
		if (towerDefenseCharacterInstance != null && towerDefenseCharacterInstance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (((zombie.instance?.maskFlags ?? 0) & 2) != 0)
		{
			return false;
		}
		return true;
	}

	private void TriggerProjection(TowerDefenseZombie deadZombie)
	{
		if (!_projecting && GodotObject.IsInstanceValid(deadZombie?.packet))
		{
			_projectingSource = deadZombie;
			_projecting = true;
			SetMainStateMachineDispatchEnabled(enabled: false);
			die = true;
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		}
	}

	private void SpawnHologramProjection()
	{
		TowerDefenseZombie projectingSource = _projectingSource;
		if (!GodotObject.IsInstanceValid(projectingSource) || !GodotObject.IsInstanceValid(projectingSource.packet))
		{
			Destroy();
			return;
		}
		string saveKey = projectingSource.packet.saveKey;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(saveKey);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			Destroy();
			return;
		}
		Vector2I vector2I = base.gridPos;
		Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(vector2I) / TowerDefenseManager.GetMapFeature().mapControl.GlobalScale.Y;
		double hitpointScale = projectingSource.instance?.hitpointScale ?? 1.0;
		Vector2 scale = projectingSource.transformPoint.Scale;
		bool hypnoses = projectingSource.instance?.hypnoses ?? false;
		bool invisible = projectingSource.invisible;
		TowerDefenseZombie projection = packetConfig.Create(pos, vector2I, base.groundHeight) as TowerDefenseZombie;
		if (GodotObject.IsInstanceValid(projection))
		{
			TowerDefenseManager.GetCharacterNode().AddChild(projection, forceReadableName: false, InternalMode.Disabled);
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
					projection.invisible = invisible;
				}
			}).CallDeferred();
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, projection);
					Dictionary spawnState = new Dictionary { ["hologram"] = true };
					MultiPlayerManager.Instance.SendSpawnCharacterAt(saveKey, vector2I.X, vector2I.Y, nextSyncId, hitpointScale, scale.X, hypnoses, 0.0, useCreate: true, pos.X, pos.Y, walkAfterSpawn: true, base.groundHeight, "", spawnState);
				}
			}
		}
		Destroy();
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		if (Engine.IsEditorHint() || !inGame || die || nearDie || _projecting)
		{
			return;
		}
		base.sprite.timeScale = timeScale * (double)(float)base.walkSpeedScale;
		if (!attackComponent.CanAttack())
		{
			return;
		}
		if (GodotObject.IsInstanceValid(attackComponent.target?.cell) && attackComponent.target.cell.HasSpike())
		{
			attackComponent.target = attackComponent.target.cell.GetSpike();
		}
		if (GodotObject.IsInstanceValid(attackComponent.target) && (attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
		else if (GodotObject.IsInstanceValid(attackComponent.target))
		{
			if (attackComponent.target.instance.spikeHurt != -1.0)
			{
				TowerDefenseCharacter target = attackComponent.target;
				double spikeHurt = attackComponent.target.instance.spikeHurt;
				target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
			}
			Die();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (_projecting && clip == "Shooting")
		{
			SpawnHologramProjection();
		}
		else
		{
			base.AnimeCompleted(clip);
		}
	}
}
