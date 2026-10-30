using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantWildMine : TowerDefensePlant
{
	private const string CannonballProjectileName = "WildMineCannon";

	private const string PotatoMinePacketName = "PlantPotatoMine";

	private PotatoComponent _potatoComponent;

	private ExplodeComponent _explodeComponent;

	private AttackComponent _attackComponent;

	private bool _rampaging;

	private int _step;

	private double _stepTimer;

	private readonly HashSet<Vector2I> _searchedGrids = new HashSet<Vector2I>();

	private System.Collections.Generic.Dictionary<ulong, Rect2> _previousTargetRects = new System.Collections.Generic.Dictionary<ulong, Rect2>();

	private System.Collections.Generic.Dictionary<ulong, Rect2> _currentTargetRects = new System.Collections.Generic.Dictionary<ulong, Rect2>();

	private double _readyTime = 15.0;

	[Export(PropertyHint.None, "")]
	public int explodeCount = 5;

	[Export(PropertyHint.None, "")]
	public double explodeInterval = 0.2;

	[Export(PropertyHint.None, "")]
	public double readyTime
	{
		get
		{
			return _readyTime;
		}
		set
		{
			_readyTime = value;
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>();
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)readyTime;
				_potatoComponent.smashExplodeHandler = SmashExplode;
			}
			RestoreRampageState();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame || base.IsRemoteNetworkReplica || die || isDestroy || !base.componentAlive || !TowerDefenseManager.Instance.IsGameRunning())
		{
			return;
		}
		if (_rampaging)
		{
			_stepTimer -= delta * Math.Max(0.0, timeScale);
			while (_rampaging && _stepTimer <= 0.0)
			{
				RampageStep();
			}
			return;
		}
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased && !_potatoComponent.over && _potatoComponent.isCharge)
		{
			AttackComponent attackComponent = _attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased && TryAcquireContactTarget())
			{
				StartRampage();
			}
		}
	}

	private bool TryAcquireContactTarget()
	{
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && attackComponent.CanAttackOnContact())
		{
			return true;
		}
		return TryAcquireSweptContactTarget();
	}

	private bool TryAcquireSweptContactTarget()
	{
		if (_attackComponent == null || !_attackComponent.TryGetCheckAreaWorldRect(out var worldRect) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return false;
		}
		_currentTargetRects.Clear();
		List<TowerDefenseCharacter> charactersForLineList = TowerDefenseManager.Instance.characterRegistry.GetCharactersForLineList(base.gridPos.Y);
		for (int i = 0; i < charactersForLineList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersForLineList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != this && towerDefenseCharacter.HasHitBox)
			{
				Rect2 worldHitRect = towerDefenseCharacter.WorldHitRect;
				ulong instanceId = towerDefenseCharacter.GetInstanceId();
				_currentTargetRects[instanceId] = worldHitRect;
				bool num = AabbShapeUtil.Intersects(worldRect, worldHitRect);
				Rect2 value;
				bool flag = _previousTargetRects.TryGetValue(instanceId, out value) && value != worldHitRect && AabbShapeUtil.Intersects(worldRect, AabbShapeUtil.Union(value, worldHitRect));
				if ((num || flag) && _attackComponent.TryCommitContactTarget(towerDefenseCharacter))
				{
					_previousTargetRects.Clear();
					_currentTargetRects.Clear();
					return true;
				}
			}
		}
		System.Collections.Generic.Dictionary<ulong, Rect2> currentTargetRects = _currentTargetRects;
		System.Collections.Generic.Dictionary<ulong, Rect2> previousTargetRects = _previousTargetRects;
		_previousTargetRects = currentTargetRects;
		_currentTargetRects = previousTargetRects;
		return false;
	}

	private void StartRampage()
	{
		if (!_rampaging)
		{
			_rampaging = true;
			_step = 0;
			_stepTimer = 0.0;
			_searchedGrids.Clear();
			_previousTargetRects.Clear();
			_currentTargetRects.Clear();
			RestoreRampageState();
			RampageStep();
		}
	}

	private bool SmashExplode()
	{
		StartRampage();
		return true;
	}

	private void RestoreRampageState()
	{
		if (_rampaging)
		{
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.over = true;
			}
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.invincible = true;
			}
		}
	}

	private void RampageStep()
	{
		_stepTimer += Math.Max(0.1, explodeInterval);
		_step++;
		_explodeComponent?.Explode();
		try
		{
			ThrowCannonball();
		}
		catch (Exception ex)
		{
			GD.PushWarning("[TowerDefensePlantWildMine] Cannonball throw skipped: " + ex.Message);
		}
		if (_step >= explodeCount)
		{
			_rampaging = false;
			Destroy();
		}
	}

	private void ThrowCannonball()
	{
		if (TryPickCannonballTarget(out var targetGrid))
		{
			_searchedGrids.Add(targetGrid);
			int num = 2;
			TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData("WildMineCannon")
			{
				baseDamage = 0.0,
				damageFlags = 0,
				fireMethodFlags = num,
				collisionFlags = 0
			};
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(targetGrid);
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = targetGrid.Y,
				fireMethodFlagsOverride = num,
				catapultStartPositionOverride = GetLogicalGlobalPosition(),
				catapultTargetPositionOverride = mapCellPlantPos
			};
			FireComponent.CreateProjectilePositionByData(null, null, 0.0, mapCellPlantPos, Vector2.Zero, projectileData, 0, base.camp, default(Vector2), overrides);
		}
	}

	private bool TryPickCannonballTarget(out Vector2I targetGrid)
	{
		targetGrid = default(Vector2I);
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPotatoMine");
		List<Vector2I> list = new List<Vector2I>();
		List<Vector2I> list2 = new List<Vector2I>();
		List<Vector2I> list3 = new List<Vector2I>();
		for (int i = 1; i <= mapGridNum.X; i++)
		{
			for (int j = 1; j <= mapGridNum.Y; j++)
			{
				Vector2I item = new Vector2I(i, j);
				if (_searchedGrids.Contains(item))
				{
					continue;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(item);
				if (GodotObject.IsInstanceValid(mapCell) && WildMineCannonLandingEvent.IsInsidePlantRegion(mapCell))
				{
					list3.Add(item);
					if (WildMineCannonLandingEvent.CellHasEnemyCharacter(mapCell, base.camp))
					{
						list.Add(item);
					}
					else if (WildMineCannonLandingEvent.CanPlantOriginalMine(mapCell, base.camp, packetConfig))
					{
						list2.Add(item);
					}
				}
			}
		}
		List<Vector2I> list4 = ((list.Count > 0) ? list : ((list2.Count > 0) ? list2 : list3));
		if (list4.Count == 0)
		{
			return false;
		}
		targetGrid = list4[GD.RandRange(0, list4.Count - 1)];
		return true;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["readyTime"] = readyTime,
			["rampaging"] = _rampaging,
			["step"] = _step,
			["stepTimer"] = _stepTimer,
			["searchedGrids"] = new Array<Vector2I>(_searchedGrids)
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		readyTime = data.GetValueOrDefault("readyTime", 15.0).AsDouble();
		_rampaging = data.GetValueOrDefault("rampaging", false).AsBool();
		_step = Math.Clamp(data.GetValueOrDefault("step", 0).AsInt32(), 0, Math.Max(0, explodeCount));
		_stepTimer = data.GetValueOrDefault("stepTimer", 0.0).AsDouble();
		_searchedGrids.Clear();
		foreach (Variant item in data.GetValueOrDefault("searchedGrids", new Godot.Collections.Array()).AsGodotArray())
		{
			_searchedGrids.Add(item.AsVector2I());
		}
		RestoreRampageState();
	}
}
