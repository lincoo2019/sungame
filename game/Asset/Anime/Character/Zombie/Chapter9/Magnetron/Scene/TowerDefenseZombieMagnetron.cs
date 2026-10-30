using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieMagnetron : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private sealed class PendingCrush
	{
		public TowerDefensePlant Plant;

		public int SyncId = -1;

		public Vector2I Grid = new Vector2I(-1, -1);

		public string NodeName = "";

		public string SaveKey = "";

		public double Remaining;
	}

	private sealed class MoveParticipant
	{
		public TowerDefensePlant Plant;

		public int SyncId = -1;

		public string NodeName = "";

		public string SaveKey = "";
	}

	private sealed class MoveDestinationState
	{
		public Vector2I Grid = new Vector2I(-1, -1);

		public int LegacyCount;

		public readonly List<MoveParticipant> Participants = new List<MoveParticipant>();
	}

	public const double AttackInterval = 10.0;

	public const double PullDuration = 2.0;

	public const double DriveSpeed = 22.0;

	public const string PullEvent = "pull";

	public const string MoveKind = "magnet";

	private static PackedScene _pullEffectScene;

	public const string PullOriginNodeName = "PullOrigin";

	public const string FrontAnchorNodeName = "FrontAnchor";

	[Export(PropertyHint.None, "")]
	public NodePath pullOriginPath;

	[Export(PropertyHint.None, "")]
	public NodePath frontAnchorPath;

	public double attackTimer;

	public bool anchored;

	public bool passedAnchor;

	public int stopColumn = -1;

	public bool shooting;

	public double pullRemaining;

	public Vector2I targetGrid = new Vector2I(-1, -1);

	public Vector2I pullTargetGrid = new Vector2I(-1, -1);

	private Node2D _pullOrigin;

	private Marker2D _frontAnchor;

	private MagnetronPullEffect _pullEffect;

	private readonly List<PendingCrush> _pendingCrushes = new List<PendingCrush>();

	private readonly List<MoveDestinationState> _activeMoveDestinations = new List<MoveDestinationState>();

	private readonly List<MoveDestinationState> _pendingMoveRestores = new List<MoveDestinationState>();

	private int _pullTargetSyncId = -1;

	private string _pullTargetNodeName = "";

	private string _pullTargetSaveKey = "";

	private bool _moveRestorePending;

	private bool _pullEffectRestorePending;

	private int _moveRestoreSequence;

	private int _restoredMoveSequence;

	private int _actionSequence;

	private int _remoteActionSequence;

	private int _networkSpecialStateRevision = 1;

	private static PackedScene PullEffectScene => _pullEffectScene ?? (_pullEffectScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Effect/MagnetronPullEffect.tscn"));

	private bool CrushOnLeft
	{
		get
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				return instance.hypnoses;
			}
			return false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_pullOrigin = ResolveMarkedNode<Node2D>(pullOriginPath, "PullOrigin", base.sprite);
			_frontAnchor = ResolveMarkedNode<Marker2D>(frontAnchorPath, "FrontAnchor", this);
		}
	}

	private T ResolveMarkedNode<T>(NodePath exportedPath, string nodeName, Node searchRoot) where T : Node
	{
		if (exportedPath != null && !exportedPath.IsEmpty)
		{
			T nodeOrNull = GetNodeOrNull<T>(exportedPath);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				return nodeOrNull;
			}
		}
		if (!GodotObject.IsInstanceValid(searchRoot))
		{
			return null;
		}
		return searchRoot.FindChild(nodeName, recursive: true, owned: false) as T;
	}

	public override void _ExitTree()
	{
		if ((die || nearDie || isDestroy) && TowerDefenseManager.HasGameplayAuthority)
		{
			FinishPendingCrushes();
		}
		if (GodotObject.IsInstanceValid(_pullEffect))
		{
			_pullEffect.QueueFree();
		}
		_pullEffect = null;
		_pendingCrushes.Clear();
		_activeMoveDestinations.Clear();
		_pendingMoveRestores.Clear();
		base._ExitTree();
	}

	public override void WalkProcessing(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (die || isDestroy)
		{
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				FinishPendingCrushes();
			}
			CancelActivePullForRemoval();
			return;
		}
		base.sprite.timeScale = timeScale;
		if (!TryRestoreMoveTweens())
		{
			return;
		}
		TryRestorePullEffect();
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		ProcessPendingCrushes(delta);
		if (TryBurstTireOnSpike() || TryDeflateBeforeCompletedCrushes())
		{
			return;
		}
		DestroyCompletedCrushes();
		CrushEnemyZombiesInBody();
		if (pullRemaining > 0.0)
		{
			pullRemaining = Math.Max(0.0, pullRemaining - delta);
			if (pullRemaining <= 0.0)
			{
				MarkStateDirty();
				_activeMoveDestinations.Clear();
				ClearPullTargetIdentity();
			}
			return;
		}
		if (nearDie)
		{
			if (shooting)
			{
				CancelShooting();
			}
			return;
		}
		int mapLastColumn = GetMapLastColumn();
		float frontLogicalX = GetFrontLogicalX();
		int currentFrontColumn = GetCurrentFrontColumn();
		if (!passedAnchor)
		{
			float columnX = GetColumnX(CrushOnLeft ? 1 : mapLastColumn);
			if (CrushOnLeft ? (frontLogicalX > columnX + 1f) : (frontLogicalX < columnX - 1f))
			{
				passedAnchor = true;
				MarkStateDirty();
			}
		}
		if (!anchored)
		{
			bool num = (CrushOnLeft ? (currentFrontColumn > 1) : (currentFrontColumn < mapLastColumn));
			bool flag = FindNearestEligibleTargetGrid(currentFrontColumn, includeTerminal: true).X >= 0;
			if (!num || !flag)
			{
				ResumeDriving(delta);
				return;
			}
			SetStopColumn(currentFrontColumn);
			AlignFrontToColumn(stopColumn);
			SetAnchored(value: true);
		}
		ProcessAnchored(delta);
	}

	private bool TryBurstTireOnSpike()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager) || towerDefenseManager.characterRegistry == null || !TryGetValidBodyRect(out var rect))
		{
			return false;
		}
		TowerDefenseCharacter spike = null;
		TowerDefenseCharacter[] array = towerDefenseManager.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp, base.gridPos.Y).ToArray();
		foreach (TowerDefenseCharacter towerDefenseCharacter in array)
		{
			if (IsValidSpikeContactTarget(towerDefenseCharacter))
			{
				spike = towerDefenseCharacter;
				break;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter?.cell))
			{
				spike = towerDefenseCharacter.cell.characterList.FirstOrDefault(IsValidSpikeContactTarget);
				if (GodotObject.IsInstanceValid(spike))
				{
					break;
				}
			}
		}
		if (!GodotObject.IsInstanceValid(spike))
		{
			return false;
		}
		return BurstTireOnSpike(spike);
	}

	private bool BurstTireOnSpike(TowerDefenseCharacter spike)
	{
		if (!IsValidSpikeContactTarget(spike))
		{
			return false;
		}
		double spikeHurt = spike.instance.spikeHurt;
		CancelPullAfterDeflation();
		AudioManager.Instance.AudioPlay("ZamboniExplosion");
		if (spikeHurt != -1.0)
		{
			double damageLimit = spikeHurt;
			spike.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, damageLimit);
		}
		Die();
		return true;
	}

	private bool IsValidSpikeContactTarget(TowerDefenseCharacter target)
	{
		if (GodotObject.IsInstanceValid(target) && !target.die && !target.nearDie && !target.isDestroy && GodotObject.IsInstanceValid(target.instance) && !target.instance.invincible && target.HasHitBox && CanTarget(target) && CanCollision(target.instance.maskFlags))
		{
			return (target.instance.physiqueTypeFlags & 0x10) != 0;
		}
		return false;
	}

	private void ProcessAnchored(double delta)
	{
		int terminalColumn = GetTerminalColumn();
		AlignFrontToColumn(terminalColumn);
		if (CrushUnscheduledPlantsAtTerminal())
		{
			return;
		}
		Vector2I selectedGrid = FindNearestEligibleTargetGrid(terminalColumn, includeTerminal: false);
		if (selectedGrid.X < 0)
		{
			ResumeDriving(delta);
		}
		else if (!shooting)
		{
			attackTimer += delta;
			if (attackTimer >= 10.0)
			{
				BeginShooting(selectedGrid);
			}
		}
	}

	private void ResumeDriving(double delta)
	{
		if (shooting)
		{
			CancelShooting();
		}
		SetAnchored(value: false);
		SetStopColumn(-1);
		MoveLeft(delta);
		CrushPlantsWhileDriving();
	}

	private void CrushPlantsWhileDriving()
	{
		if (!TryGetValidBodyRect(out var rect))
		{
			return;
		}
		List<TowerDefensePlant> list = (from plant in TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp, base.gridPos.Y, includeAllLineCheck: true).OfType<TowerDefensePlant>()
			where IsDraggablePlant(plant, base.camp) && CanCollision(plant.instance.maskFlags)
			select plant).ToList();
		if (TryDeflateBeforeCrushes(list.Select((TowerDefensePlant plant) => (Plant: plant, Grid: plant.gridPos)).ToList()))
		{
			return;
		}
		foreach (TowerDefensePlant item in list)
		{
			DestroyPlantAndReplicate(item, smash: true);
		}
	}

	private void MoveLeft(double delta, float clampX = -1f / 0f)
	{
		if (!base.sprite.pause)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X += (float)(22.0 * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)(base.sprite.playBack ? 1f : (-1f)));
			if (!float.IsNegativeInfinity(clampX))
			{
				logicalGlobalPosition.X = Math.Max(logicalGlobalPosition.X, clampX);
			}
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	private void SetAnchored(bool value)
	{
		if (anchored != value)
		{
			anchored = value;
			MarkStateDirty();
		}
	}

	private void SetStopColumn(int value)
	{
		int num = ((value < 1) ? (-1) : Mathf.Clamp(value, 1, GetMapLastColumn()));
		if (stopColumn != num)
		{
			stopColumn = num;
			attackTimer = 0.0;
			MarkStateDirty();
		}
	}

	private int GetMapLastColumn()
	{
		return Math.Max(1, TowerDefenseManager.Instance.GetMapGridNum().X);
	}

	private int GetTerminalColumn()
	{
		if (stopColumn >= 1)
		{
			return Mathf.Clamp(stopColumn, 1, GetMapLastColumn());
		}
		if (CrushOnLeft)
		{
			return 1;
		}
		return GetMapLastColumn();
	}

	private float GetFrontLocalX()
	{
		if (!GodotObject.IsInstanceValid(_frontAnchor))
		{
			_frontAnchor = ResolveMarkedNode<Marker2D>(frontAnchorPath, "FrontAnchor", this);
		}
		if (!GodotObject.IsInstanceValid(_frontAnchor))
		{
			return -97f;
		}
		return _frontAnchor.Position.X;
	}

	private float GetFrontLogicalX()
	{
		return GetLogicalGlobalPosition().X + GetFrontLocalX() * base.Scale.X;
	}

	private float GetRootXForFront(float frontX)
	{
		return frontX - GetFrontLocalX() * base.Scale.X;
	}

	private float GetColumnX(int column)
	{
		return TowerDefenseManager.GetMapCellPlantPos(new Vector2I(column, base.gridPos.Y)).X;
	}

	private int GetCurrentFrontColumn()
	{
		Vector2 pos = new Vector2(GetFrontLogicalX(), TowerDefenseManager.GetMapCellPlantPos(base.gridPos).Y);
		return Mathf.Clamp(TowerDefenseManager.Instance.GetMapGridPos(pos).X, 1, GetMapLastColumn());
	}

	private void AlignFrontToColumn(int column)
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		logicalGlobalPosition.X = GetRootXForFront(GetColumnX(column));
		SetLogicalGlobalPosition(logicalGlobalPosition);
	}

	private Vector2I FindNearestEligibleTargetGrid(int terminalColumn, bool includeTerminal)
	{
		int mapLastColumn = GetMapLastColumn();
		if (CrushOnLeft)
		{
			for (int i = terminalColumn + ((!includeTerminal) ? 1 : 0); i <= mapLastColumn; i++)
			{
				Vector2I candidate = new Vector2I(i, base.gridPos.Y);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(candidate);
				if (GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Any((TowerDefenseCharacter character) => character.gridPos == candidate && IsEligibleTarget(character, base.camp)))
				{
					return candidate;
				}
			}
			return new Vector2I(-1, -1);
		}
		for (int num = Mathf.Clamp(terminalColumn - ((!includeTerminal) ? 1 : 0), 0, mapLastColumn); num >= 1; num--)
		{
			Vector2I candidate2 = new Vector2I(num, base.gridPos.Y);
			TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(candidate2);
			if (GodotObject.IsInstanceValid(mapCell2) && mapCell2.characterList.Any((TowerDefenseCharacter character) => character.gridPos == candidate2 && IsEligibleTarget(character, base.camp)))
			{
				return candidate2;
			}
		}
		return new Vector2I(-1, -1);
	}

	public static bool IsEligibleTarget(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP myCamp)
	{
		if (IsDraggablePlant(character, myCamp))
		{
			return character.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;
		}
		return false;
	}

	public static bool IsDraggablePlant(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP myCamp)
	{
		if (character is TowerDefensePlant && GodotObject.IsInstanceValid(character) && !character.die && !character.nearDie && !character.isDestroy && GodotObject.IsInstanceValid(character.instance) && GodotObject.IsInstanceValid(character.config) && character.camp != myCamp && !character.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR) && !character.instance.invincible && !character.instance.invincibleSmash)
		{
			if (!character.instance.canBeCollection)
			{
				return character.instance.hologram;
			}
			return true;
		}
		return false;
	}

	private void CrushEnemyZombiesInBody()
	{
		double num = ((base.config is TowerDefenseZombieConfig towerDefenseZombieConfig) ? towerDefenseZombieConfig.smashAttack : 0.0);
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager) || towerDefenseManager.characterRegistry == null || !TryGetValidBodyRect(out var rect))
		{
			return;
		}
		TowerDefenseCharacter[] array = towerDefenseManager.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp, base.gridPos.Y).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is TowerDefenseZombie towerDefenseZombie && CanCrushZombie(towerDefenseZombie, base.camp))
			{
				towerDefenseZombie.SmashHurt(num, playSplatAudio: true, Vector2.Zero);
			}
		}
	}

	private bool TryGetValidBodyRect(out Rect2 rect)
	{
		if (!base.HasHitBox || !base.IsHitBoxEnabled)
		{
			rect = default(Rect2);
			return false;
		}
		rect = base.WorldHitRect;
		return true;
	}

	private static bool CanCrushZombie(TowerDefenseZombie zombie, TowerDefenseEnum.CHARACTER_CAMP myCamp)
	{
		if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(zombie.instance))
		{
			return false;
		}
		if (zombie.die || zombie.nearDie || zombie.isDestroy)
		{
			return false;
		}
		if (zombie.camp == myCamp)
		{
			return false;
		}
		if (zombie.instance.invincible || zombie.instance.invincibleSmash)
		{
			return false;
		}
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = zombie.instance;
		if (towerDefenseCharacterInstance != null && towerDefenseCharacterInstance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		int num = zombie.instance?.maskFlags ?? 0;
		if ((num & 1) == 0)
		{
			return false;
		}
		if ((num & 2) != 0)
		{
			return false;
		}
		return true;
	}

	private void BeginShooting(Vector2I selectedGrid)
	{
		attackTimer = 0.0;
		targetGrid = selectedGrid;
		pullTargetGrid = new Vector2I(-1, -1);
		ClearPullTargetIdentity();
		shooting = true;
		MarkStateDirty();
		if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("anim_shooting"))
		{
			base.sprite.SetAnimation("anim_shooting", loop: false);
		}
	}

	private void CancelShooting()
	{
		shooting = false;
		targetGrid = new Vector2I(-1, -1);
		pullTargetGrid = new Vector2I(-1, -1);
		ClearPullTargetIdentity();
		MarkStateDirty();
		PlayDriveAnimation();
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (string.Equals(command, "pull", StringComparison.OrdinalIgnoreCase) && TowerDefenseManager.HasGameplayAuthority && shooting && !die && !isDestroy)
		{
			ExecutePull();
		}
	}

	public override void AnimeCompleted(string clipName)
	{
		base.AnimeCompleted(clipName);
		if (clipName == "anim_shooting" && shooting && TowerDefenseManager.HasGameplayAuthority)
		{
			CancelShooting();
		}
	}

	private void ExecutePull()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(targetGrid);
		TowerDefensePlant towerDefensePlant = (GodotObject.IsInstanceValid(mapCell) ? mapCell.characterList.OfType<TowerDefensePlant>().FirstOrDefault((TowerDefensePlant plant) => plant.gridPos == targetGrid && IsEligibleTarget(plant, base.camp)) : null);
		shooting = false;
		if (!GodotObject.IsInstanceValid(towerDefensePlant))
		{
			targetGrid = new Vector2I(-1, -1);
			ClearPullTargetIdentity();
			MarkStateDirty();
			PlayDriveAnimation();
			return;
		}
		List<(int, List<TowerDefensePlant>)> list = SnapshotPullChain(targetGrid.X);
		if (list.Count == 0)
		{
			CancelShooting();
			return;
		}
		pullRemaining = 2.0;
		_pullEffectRestorePending = false;
		_activeMoveDestinations.Clear();
		pullTargetGrid = new Vector2I(-1, -1);
		SetPullTargetIdentity(towerDefensePlant);
		PlayDriveAnimation();
		foreach (var item3 in list.OrderByDescending<(int, List<TowerDefensePlant>), int>(((int Column, List<TowerDefensePlant> Plants) entry) => entry.Column))
		{
			int item = item3.Item1;
			List<TowerDefensePlant> item2 = item3.Item2;
			Vector2I vector2I = MovePlantGroup(item, item2);
			if (item == targetGrid.X)
			{
				pullTargetGrid = vector2I;
			}
		}
		_actionSequence = NextSequence(_actionSequence);
		MarkStateDirty();
		CreatePullEffect(towerDefensePlant, 2.0);
	}

	private List<(int Column, List<TowerDefensePlant> Plants)> SnapshotPullChain(int startColumn)
	{
		List<(int, List<TowerDefensePlant>)> list = new List<(int, List<TowerDefensePlant>)>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		int terminalColumn = GetTerminalColumn();
		bool crushOnLeft = CrushOnLeft;
		for (int i = startColumn; crushOnLeft ? (i >= terminalColumn) : (i <= terminalColumn); i += ((!crushOnLeft) ? 1 : (-1)))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, base.gridPos.Y));
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				break;
			}
			List<TowerDefensePlant> list2 = new List<TowerDefensePlant>();
			TowerDefenseCharacter[] array = mapCell.characterList.ToArray();
			foreach (TowerDefenseCharacter towerDefenseCharacter in array)
			{
				if (!(towerDefenseCharacter.gridPos != new Vector2I(i, base.gridPos.Y)) && IsDraggablePlant(towerDefenseCharacter, base.camp) && hashSet.Add(towerDefenseCharacter.GetInstanceId()))
				{
					list2.Add((TowerDefensePlant)towerDefenseCharacter);
				}
			}
			if (list2.Count == 0)
			{
				break;
			}
			list.Add((i, list2));
		}
		return list;
	}

	private Vector2I MovePlantGroup(int column, List<TowerDefensePlant> plants)
	{
		int terminalColumn = GetTerminalColumn();
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(column, base.gridPos.Y));
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return new Vector2I(-1, -1);
		}
		List<TowerDefensePlant> list = plants.Where((TowerDefensePlant c) => IsDraggablePlant(c, base.camp)).ToList();
		if (list.Count == 0)
		{
			return new Vector2I(-1, -1);
		}
		if (CrushOnLeft ? (column <= terminalColumn) : (column >= terminalColumn))
		{
			foreach (TowerDefensePlant item in list)
			{
				item.EmitDestroy();
				QueueCrush(item, 2.0);
			}
			return new Vector2I(terminalColumn, base.gridPos.Y);
		}
		Vector2I destinationGrid = FindPullDestination(column, list);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(destinationGrid);
		if (!GodotObject.IsInstanceValid(mapCell2))
		{
			return new Vector2I(-1, -1);
		}
		foreach (TowerDefensePlant item2 in list)
		{
			Vector2I fromGridPos = item2.gridPos;
			item2.EmitDestroy();
			mapCell.MoveCharacterToCell(item2, mapCell2, 2.0);
			if (Global.IsMultiplayerMode)
			{
				MultiPlayerManager multiPlayerManager = MultiPlayerManager.Instance;
				if (multiPlayerManager != null && multiPlayerManager.isHost && item2.syncId >= 0)
				{
					MultiPlayerManager.Instance.SendMoveCharacter(item2.syncId, fromGridPos, destinationGrid, "magnet", 2.0);
				}
			}
			if (destinationGrid.X == terminalColumn)
			{
				QueueCrush(item2, 2.0);
			}
		}
		if (list.Count > 0)
		{
			MoveDestinationState moveDestinationState = _activeMoveDestinations.FirstOrDefault((MoveDestinationState state) => state.Grid == destinationGrid);
			if (moveDestinationState == null)
			{
				moveDestinationState = new MoveDestinationState
				{
					Grid = destinationGrid
				};
				_activeMoveDestinations.Add(moveDestinationState);
			}
			foreach (TowerDefensePlant item3 in list)
			{
				moveDestinationState.Participants.Add(CreateMoveParticipant(item3));
			}
		}
		return destinationGrid;
	}

	private Vector2I FindPullDestination(int sourceColumn, List<TowerDefensePlant> plants)
	{
		int terminalColumn = GetTerminalColumn();
		if (CrushOnLeft)
		{
			for (int num = sourceColumn - 1; num > terminalColumn; num--)
			{
				Vector2I result = new Vector2I(num, base.gridPos.Y);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(result);
				if (GodotObject.IsInstanceValid(mapCell) && mapCell.CanMoveCharactersHere(plants))
				{
					return result;
				}
			}
			return new Vector2I(terminalColumn, base.gridPos.Y);
		}
		for (int i = sourceColumn + 1; i < terminalColumn; i++)
		{
			Vector2I result2 = new Vector2I(i, base.gridPos.Y);
			TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(result2);
			if (GodotObject.IsInstanceValid(mapCell2) && mapCell2.CanMoveCharactersHere(plants))
			{
				return result2;
			}
		}
		return new Vector2I(terminalColumn, base.gridPos.Y);
	}

	private void QueueCrush(TowerDefensePlant plant, double remaining)
	{
		if (GodotObject.IsInstanceValid(plant) && !_pendingCrushes.Any((PendingCrush entry) => entry.Plant == plant))
		{
			_pendingCrushes.Add(new PendingCrush
			{
				Plant = plant,
				SyncId = plant.syncId,
				Grid = plant.gridPos,
				NodeName = GetSavedNodeName(plant),
				SaveKey = (plant.packet?.saveKey ?? ""),
				Remaining = remaining
			});
		}
	}

	private static MoveParticipant CreateMoveParticipant(TowerDefensePlant plant)
	{
		return new MoveParticipant
		{
			Plant = plant,
			SyncId = (plant?.syncId ?? (-1)),
			NodeName = GetSavedNodeName(plant),
			SaveKey = (plant?.packet?.saveKey ?? "")
		};
	}

	private void SetPullTargetIdentity(TowerDefensePlant plant)
	{
		_pullTargetSyncId = plant?.syncId ?? (-1);
		_pullTargetNodeName = GetSavedNodeName(plant);
		_pullTargetSaveKey = plant?.packet?.saveKey ?? "";
	}

	private void ClearPullTargetIdentity()
	{
		_pullTargetSyncId = -1;
		_pullTargetNodeName = "";
		_pullTargetSaveKey = "";
	}

	private void ProcessPendingCrushes(double delta)
	{
		foreach (PendingCrush pendingCrush in _pendingCrushes)
		{
			if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
			{
				pendingCrush.Plant = ResolvePendingCrush(pendingCrush);
			}
			pendingCrush.Remaining = Math.Max(0.0, pendingCrush.Remaining - delta);
		}
	}

	private void DestroyCompletedCrushes()
	{
		for (int num = _pendingCrushes.Count - 1; num >= 0; num--)
		{
			PendingCrush pendingCrush = _pendingCrushes[num];
			if (!(pendingCrush.Remaining > 0.0))
			{
				if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
				{
					pendingCrush.Plant = ResolvePendingCrush(pendingCrush);
				}
				if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
				{
					_pendingCrushes.RemoveAt(num);
				}
				else
				{
					if (GodotObject.IsInstanceValid(pendingCrush.Plant))
					{
						CompletePendingCrush(pendingCrush);
					}
					_pendingCrushes.RemoveAt(num);
				}
			}
		}
	}

	private bool TryDeflateBeforeCompletedCrushes()
	{
		List<(TowerDefensePlant, Vector2I)> list = new List<(TowerDefensePlant, Vector2I)>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		PendingCrush[] array = _pendingCrushes.ToArray();
		foreach (PendingCrush pendingCrush in array)
		{
			if (!(pendingCrush.Remaining > 0.0))
			{
				if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
				{
					pendingCrush.Plant = ResolvePendingCrush(pendingCrush);
				}
				if (GodotObject.IsInstanceValid(pendingCrush.Plant) && hashSet.Add(pendingCrush.Plant.GetInstanceId()))
				{
					list.Add((pendingCrush.Plant, pendingCrush.Grid));
				}
			}
		}
		return TryDeflateBeforeCrushes(list);
	}

	private bool TryDeflateBeforeCrushes(List<(TowerDefensePlant Plant, Vector2I Grid)> candidates)
	{
		if (candidates.Count == 0 || !TowerDefenseManager.HasGameplayAuthority)
		{
			return false;
		}
		foreach (var candidate in candidates)
		{
			TowerDefensePlant item = candidate.Plant;
			if (IsValidSpikeContactTarget(item))
			{
				return BurstTireOnSpike(item);
			}
		}
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		foreach (var candidate2 in candidates)
		{
			Vector2I item2 = candidate2.Grid;
			if (!hashSet.Add(item2) || !TowerDefenseManager.Instance.CheckMapGridPosIn(item2))
			{
				continue;
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(item2);
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				continue;
			}
			TowerDefenseCharacter[] array = mapCell.characterList.ToArray();
			foreach (TowerDefenseCharacter towerDefenseCharacter in array)
			{
				if (IsValidSpikeContactTarget(towerDefenseCharacter))
				{
					return BurstTireOnSpike(towerDefenseCharacter);
				}
			}
		}
		AttackComponent attackComponent = base.attackComponent;
		if (attackComponent == null || attackComponent.IsReleased)
		{
			return false;
		}
		double num = ((base.config is TowerDefenseZombieConfig towerDefenseZombieConfig) ? towerDefenseZombieConfig.smashAttack : 0.0);
		foreach (var candidate3 in candidates)
		{
			TowerDefensePlant item3 = candidate3.Plant;
			if (CanAttemptPlantCrush(item3) && base.attackComponent.TryShieldBlockSmash(item3, num, forceLethal: true))
			{
				CancelPullAfterDeflation();
				return true;
			}
		}
		return false;
	}

	private static bool CanAttemptPlantCrush(TowerDefensePlant plant)
	{
		if (GodotObject.IsInstanceValid(plant) && !plant.die && !plant.nearDie && !plant.isDestroy && GodotObject.IsInstanceValid(plant.instance) && !plant.instance.invincible)
		{
			return !plant.instance.invincibleSmash;
		}
		return false;
	}

	private void CancelPullAfterDeflation()
	{
		_pendingCrushes.Clear();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.target = null;
		}
		shooting = false;
		attackTimer = 0.0;
		targetGrid = new Vector2I(-1, -1);
		CancelActivePullForRemoval();
		MarkStateDirty();
	}

	private void FinishPendingCrushes()
	{
		for (int num = _pendingCrushes.Count - 1; num >= 0; num--)
		{
			PendingCrush pendingCrush = _pendingCrushes[num];
			if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
			{
				pendingCrush.Plant = ResolvePendingCrush(pendingCrush);
			}
			if (!GodotObject.IsInstanceValid(pendingCrush.Plant))
			{
				_pendingCrushes.RemoveAt(num);
			}
			else
			{
				CompletePendingCrush(pendingCrush);
				_pendingCrushes.RemoveAt(num);
			}
		}
	}

	private static void CompletePendingCrush(PendingCrush pending)
	{
		TowerDefensePlant plant = pending.Plant;
		if (GodotObject.IsInstanceValid(plant))
		{
			plant.CancelCellMoveTween();
			if (TowerDefenseManager.Instance.CheckMapGridPosIn(pending.Grid))
			{
				plant.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(pending.Grid));
			}
			DestroyPlantAndReplicate(plant, smash: true);
		}
	}

	private void CancelActivePullForRemoval()
	{
		pullRemaining = 0.0;
		pullTargetGrid = new Vector2I(-1, -1);
		ClearPullTargetIdentity();
		_pullEffectRestorePending = false;
		_moveRestorePending = false;
		_activeMoveDestinations.Clear();
		_pendingMoveRestores.Clear();
		if (GodotObject.IsInstanceValid(_pullEffect))
		{
			_pullEffect.QueueFree();
		}
		_pullEffect = null;
	}

	private bool TryRestoreMoveTweens()
	{
		if (!_moveRestorePending)
		{
			return true;
		}
		if (pullRemaining <= 0.0 || _pendingMoveRestores.Count == 0)
		{
			_moveRestorePending = false;
			_pendingMoveRestores.Clear();
			return true;
		}
		List<(MoveDestinationState, List<TowerDefensePlant>)> list = new List<(MoveDestinationState, List<TowerDefensePlant>)>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (MoveDestinationState pendingMoveRestore in _pendingMoveRestores)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(pendingMoveRestore.Grid);
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				return AbandonMoveTweenRestore();
			}
			List<TowerDefensePlant> list2 = (from c in mapCell.characterList.OfType<TowerDefensePlant>()
				where IsDraggablePlant(c, base.camp)
				select c).ToList();
			List<TowerDefensePlant> list3 = new List<TowerDefensePlant>();
			if (pendingMoveRestore.Participants.Count == 0)
			{
				if (list2.Count != pendingMoveRestore.LegacyCount)
				{
					return AbandonMoveTweenRestore();
				}
				list3.AddRange(list2);
			}
			else
			{
				foreach (MoveParticipant participant in pendingMoveRestore.Participants)
				{
					TowerDefensePlant towerDefensePlant = ResolveMoveParticipant(participant, pendingMoveRestore.Grid, list2, hashSet, base.camp);
					if (!GodotObject.IsInstanceValid(towerDefensePlant))
					{
						return AbandonMoveTweenRestore();
					}
					list3.Add(towerDefensePlant);
					hashSet.Add(towerDefensePlant.GetInstanceId());
				}
			}
			list.Add((pendingMoveRestore, list3));
		}
		_activeMoveDestinations.Clear();
		foreach (var item3 in list)
		{
			MoveDestinationState item = item3.Item1;
			List<TowerDefensePlant> item2 = item3.Item2;
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(item.Grid);
			MoveDestinationState moveDestinationState = new MoveDestinationState
			{
				Grid = item.Grid
			};
			foreach (TowerDefensePlant item4 in item2)
			{
				TowerDefenseCellInstance.CreateCharacterCellMoveTween(item4, mapCellPlantPos, pullRemaining);
				moveDestinationState.Participants.Add(CreateMoveParticipant(item4));
			}
			_activeMoveDestinations.Add(moveDestinationState);
		}
		_moveRestorePending = false;
		_restoredMoveSequence = Math.Max(_restoredMoveSequence, _moveRestoreSequence);
		_pendingMoveRestores.Clear();
		return true;
	}

	private bool AbandonMoveTweenRestore()
	{
		_moveRestorePending = false;
		_restoredMoveSequence = Math.Max(_restoredMoveSequence, _moveRestoreSequence);
		_pendingMoveRestores.Clear();
		_activeMoveDestinations.Clear();
		return true;
	}

	private static TowerDefensePlant ResolveMoveParticipant(MoveParticipant participant, Vector2I expectedGrid, List<TowerDefensePlant> candidates, HashSet<ulong> usedPlantIds, TowerDefenseEnum.CHARACTER_CAMP myCamp)
	{
		TowerDefensePlant towerDefensePlant = ResolvePlant(participant.SyncId);
		if (IsMatchingMoveParticipant(towerDefensePlant, participant, expectedGrid, usedPlantIds, myCamp))
		{
			return towerDefensePlant;
		}
		if (!string.IsNullOrEmpty(participant.NodeName))
		{
			TowerDefensePlant result = candidates.FirstOrDefault((TowerDefensePlant candidate) => HasSavedNodeName(candidate, participant.NodeName) && IsMatchingMoveParticipant(candidate, participant, expectedGrid, usedPlantIds, myCamp));
			if (GodotObject.IsInstanceValid(result))
			{
				return result;
			}
		}
		return null;
	}

	private static bool IsMatchingMoveParticipant(TowerDefensePlant plant, MoveParticipant participant, Vector2I expectedGrid, HashSet<ulong> usedPlantIds, TowerDefenseEnum.CHARACTER_CAMP myCamp)
	{
		if (GodotObject.IsInstanceValid(plant) && IsDraggablePlant(plant, myCamp) && plant.gridPos == expectedGrid && !usedPlantIds.Contains(plant.GetInstanceId()))
		{
			if (!string.IsNullOrEmpty(participant.SaveKey))
			{
				return plant.packet?.saveKey == participant.SaveKey;
			}
			return true;
		}
		return false;
	}

	private void TryRestorePullEffect()
	{
		if (_pullEffectRestorePending)
		{
			if (pullRemaining <= 0.0)
			{
				_pullEffectRestorePending = false;
			}
			else if (IsNodeReady())
			{
				CreatePullEffect(FindRemoteVisualTarget(), pullRemaining);
				_pullEffectRestorePending = false;
			}
		}
	}

	private bool CrushUnscheduledPlantsAtTerminal()
	{
		if (pullRemaining > 0.0)
		{
			return false;
		}
		int terminalColumn = GetTerminalColumn();
		Vector2I terminalGrid = new Vector2I(terminalColumn, base.gridPos.Y);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(terminalGrid);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return false;
		}
		List<TowerDefensePlant> list = (from plant in mapCell.characterList.OfType<TowerDefensePlant>()
			where plant.gridPos == terminalGrid && IsDraggablePlant(plant, base.camp) && !_pendingCrushes.Any((PendingCrush entry) => entry.Plant == plant)
			group plant by plant.GetInstanceId() into @group
			select @group.First()).ToList();
		List<(TowerDefensePlant, Vector2I)> candidates = list.Select((TowerDefensePlant plant) => (Plant: plant, Grid: terminalGrid)).ToList();
		if (TryDeflateBeforeCrushes(candidates))
		{
			return true;
		}
		foreach (TowerDefensePlant item in list)
		{
			if (CanAttemptPlantCrush(item))
			{
				DestroyPlantAndReplicate(item, smash: true);
			}
		}
		return false;
	}

	private static void DestroyPlantAndReplicate(TowerDefensePlant plant, bool smash)
	{
		if (!GodotObject.IsInstanceValid(plant) || plant.isDestroy || (smash && (plant.instance.invincible || plant.instance.invincibleSmash)))
		{
			return;
		}
		if (Global.IsMultiplayerMode)
		{
			MultiPlayerManager multiPlayerManager = MultiPlayerManager.Instance;
			if (multiPlayerManager != null && multiPlayerManager.isHost && plant.syncId >= 0)
			{
				MultiPlayerManager.Instance.SendCharacterDestroy(plant.syncId, isExplode: false, smash);
			}
		}
		if (smash)
		{
			plant.SmashDestroy();
		}
		else
		{
			plant.Destroy();
		}
	}

	private void CreatePullEffect(TowerDefenseCharacter target, double duration)
	{
		if (GodotObject.IsInstanceValid(PullEffectScene))
		{
			if (GodotObject.IsInstanceValid(_pullEffect))
			{
				_pullEffect.QueueFree();
			}
			_pullEffect = PullEffectScene.Instantiate<MagnetronPullEffect>(PackedScene.GenEditState.Disabled);
			TowerDefenseManager.GetCharacterNode().AddChild(_pullEffect, forceReadableName: false, InternalMode.Disabled);
			_pullEffect.Configure(this, target, GetTargetFallbackPosition(), duration);
		}
	}

	private Vector2 GetTargetFallbackPosition()
	{
		Vector2I effectivePullTargetGrid = GetEffectivePullTargetGrid();
		if (effectivePullTargetGrid.X < 0)
		{
			return GetLogicalGlobalPosition();
		}
		return TowerDefenseManager.GetMapCellPlantPos(effectivePullTargetGrid);
	}

	public Vector2 GetPullOriginPosition()
	{
		if (!GodotObject.IsInstanceValid(_pullOrigin))
		{
			_pullOrigin = ResolveMarkedNode<Node2D>(pullOriginPath, "PullOrigin", base.sprite);
		}
		if (!GodotObject.IsInstanceValid(_pullOrigin))
		{
			return GetLogicalGlobalPosition() + new Vector2(-45f, -55f);
		}
		return _pullOrigin.GlobalPosition;
	}

	private void PlayDriveAnimation()
	{
		if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("anim_drive"))
		{
			base.sprite.SetAnimation("anim_drive");
		}
	}

	private void MarkStateDirty()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_networkSpecialStateRevision = NextSequence(_networkSpecialStateRevision);
		}
	}

	private static int NextSequence(int current)
	{
		current++;
		if (current > 0)
		{
			return current;
		}
		return 1;
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		return ExportMagnetronState();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return ExportMagnetronState();
	}

	private Dictionary ExportMagnetronState()
	{
		return new Dictionary
		{
			["rev"] = _networkSpecialStateRevision,
			["anchored"] = anchored,
			["passedAnchor"] = passedAnchor,
			["stopColumn"] = stopColumn,
			["attackTimer"] = attackTimer,
			["shooting"] = shooting,
			["pullRemaining"] = pullRemaining,
			["targetX"] = targetGrid.X,
			["targetY"] = targetGrid.Y,
			["pullTargetX"] = pullTargetGrid.X,
			["pullTargetY"] = pullTargetGrid.Y,
			["pullTargetSyncId"] = _pullTargetSyncId,
			["pullTargetNodeName"] = _pullTargetNodeName,
			["pullTargetSaveKey"] = _pullTargetSaveKey,
			["actionSequence"] = _actionSequence,
			["moveDestinations"] = ExportMoveDestinations()
		};
	}

	private Array<Dictionary> ExportMoveDestinations()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (MoveDestinationState activeMoveDestination in _activeMoveDestinations)
		{
			Array<Dictionary> array2 = new Array<Dictionary>();
			foreach (MoveParticipant participant in activeMoveDestination.Participants)
			{
				array2.Add(new Dictionary
				{
					["syncId"] = participant.SyncId,
					["nodeName"] = participant.NodeName,
					["saveKey"] = participant.SaveKey
				});
			}
			array.Add(new Dictionary
			{
				["x"] = activeMoveDestination.Grid.X,
				["y"] = activeMoveDestination.Grid.Y,
				["count"] = activeMoveDestination.Participants.Count,
				["plants"] = array2
			});
		}
		return array;
	}

	private void PrepareMoveTweenRestore(Dictionary data, int sequence)
	{
		_pendingMoveRestores.Clear();
		_activeMoveDestinations.Clear();
		foreach (Variant item in data.GetValueOrDefault("moveDestinations", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			int num = Math.Max(0, dictionary.GetValueOrDefault("count", 0).AsInt32());
			Vector2I grid = new Vector2I(dictionary.GetValueOrDefault("x", -1).AsInt32(), dictionary.GetValueOrDefault("y", -1).AsInt32());
			if (num <= 0 || grid.X < 1 || grid.Y < 1)
			{
				continue;
			}
			MoveDestinationState moveDestinationState = new MoveDestinationState
			{
				Grid = grid,
				LegacyCount = num
			};
			bool flag = dictionary.ContainsKey("plants");
			foreach (Variant item2 in dictionary.GetValueOrDefault("plants", new Godot.Collections.Array()).AsGodotArray())
			{
				Dictionary dictionary2 = item2.AsGodotDictionary();
				moveDestinationState.Participants.Add(new MoveParticipant
				{
					SyncId = dictionary2.GetValueOrDefault("syncId", -1).AsInt32(),
					NodeName = NormalizeSavedNodeName(dictionary2.GetValueOrDefault("nodeName", "").AsString()),
					SaveKey = dictionary2.GetValueOrDefault("saveKey", "").AsString()
				});
			}
			if (!flag || moveDestinationState.Participants.Count == num)
			{
				_pendingMoveRestores.Add(moveDestinationState);
			}
		}
		_moveRestoreSequence = sequence;
		_moveRestorePending = pullRemaining > 0.0 && _pendingMoveRestores.Count > 0;
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		ApplyNetworkState(data);
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ApplyNetworkState(data);
	}

	private void ApplyNetworkState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("rev", _networkSpecialStateRevision).AsInt32();
		if (!TowerDefenseManager.HasGameplayAuthority && num < _networkSpecialStateRevision)
		{
			return;
		}
		_networkSpecialStateRevision = Math.Max(_networkSpecialStateRevision, num);
		anchored = data.GetValueOrDefault("anchored", anchored).AsBool();
		passedAnchor = data.GetValueOrDefault("passedAnchor", passedAnchor).AsBool();
		attackTimer = data.GetValueOrDefault("attackTimer", attackTimer).AsDouble();
		shooting = data.GetValueOrDefault("shooting", shooting).AsBool();
		pullRemaining = data.GetValueOrDefault("pullRemaining", pullRemaining).AsDouble();
		stopColumn = ImportStopColumn(data, stopColumn);
		targetGrid = new Vector2I(data.GetValueOrDefault("targetX", targetGrid.X).AsInt32(), data.GetValueOrDefault("targetY", targetGrid.Y).AsInt32());
		pullTargetGrid = ImportPullTargetGrid(data);
		ImportPullTargetIdentity(data);
		int num2 = data.GetValueOrDefault("actionSequence", _actionSequence).AsInt32();
		_actionSequence = Math.Max(_actionSequence, num2);
		if (!TowerDefenseManager.HasGameplayAuthority && num2 > _restoredMoveSequence && pullRemaining > 0.0)
		{
			PrepareMoveTweenRestore(data, num2);
		}
		if (!TowerDefenseManager.HasGameplayAuthority && num2 > _remoteActionSequence && pullRemaining > 0.0)
		{
			_remoteActionSequence = num2;
			if (IsNodeReady())
			{
				TowerDefenseCharacter target = FindRemoteVisualTarget();
				CreatePullEffect(target, pullRemaining);
			}
			else
			{
				_pullEffectRestorePending = true;
			}
		}
	}

	private TowerDefenseCharacter FindRemoteVisualTarget()
	{
		Vector2I effectivePullTargetGrid = GetEffectivePullTargetGrid();
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(effectivePullTargetGrid);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return null;
		}
		TowerDefensePlant towerDefensePlant = ResolvePlant(_pullTargetSyncId);
		if (GodotObject.IsInstanceValid(towerDefensePlant) && towerDefensePlant.gridPos == effectivePullTargetGrid && IsDraggablePlant(towerDefensePlant, base.camp))
		{
			return towerDefensePlant;
		}
		if (!string.IsNullOrEmpty(_pullTargetNodeName))
		{
			TowerDefensePlant result = mapCell.characterList.OfType<TowerDefensePlant>().FirstOrDefault((TowerDefensePlant plant) => IsDraggablePlant(plant, base.camp) && HasSavedNodeName(plant, _pullTargetNodeName) && (string.IsNullOrEmpty(_pullTargetSaveKey) || plant.packet?.saveKey == _pullTargetSaveKey));
			if (GodotObject.IsInstanceValid(result))
			{
				return result;
			}
		}
		return mapCell.characterList.FirstOrDefault((TowerDefenseCharacter c) => IsDraggablePlant(c, base.camp));
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return _networkSpecialStateRevision;
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		return false;
	}

	public override Dictionary ExportVariantSave()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (PendingCrush pendingCrush in _pendingCrushes)
		{
			array.Add(new Dictionary
			{
				["syncId"] = pendingCrush.SyncId,
				["gridX"] = pendingCrush.Grid.X,
				["gridY"] = pendingCrush.Grid.Y,
				["nodeName"] = pendingCrush.NodeName,
				["saveKey"] = pendingCrush.SaveKey,
				["remaining"] = pendingCrush.Remaining
			});
		}
		return new Dictionary
		{
			["attackTimer"] = attackTimer,
			["anchored"] = anchored,
			["passedAnchor"] = passedAnchor,
			["stopColumn"] = stopColumn,
			["shooting"] = shooting,
			["pullRemaining"] = pullRemaining,
			["targetX"] = targetGrid.X,
			["targetY"] = targetGrid.Y,
			["pullTargetX"] = pullTargetGrid.X,
			["pullTargetY"] = pullTargetGrid.Y,
			["pullTargetSyncId"] = _pullTargetSyncId,
			["pullTargetNodeName"] = _pullTargetNodeName,
			["pullTargetSaveKey"] = _pullTargetSaveKey,
			["actionSequence"] = _actionSequence,
			["networkSpecialStateRevision"] = _networkSpecialStateRevision,
			["pendingCrushes"] = array,
			["moveDestinations"] = ExportMoveDestinations()
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attackTimer = data.GetValueOrDefault("attackTimer", 0.0).AsDouble();
		anchored = data.GetValueOrDefault("anchored", false).AsBool();
		passedAnchor = data.GetValueOrDefault("passedAnchor", false).AsBool();
		shooting = data.GetValueOrDefault("shooting", false).AsBool();
		pullRemaining = data.GetValueOrDefault("pullRemaining", 0.0).AsDouble();
		stopColumn = ImportStopColumn(data, -1);
		targetGrid = new Vector2I(data.GetValueOrDefault("targetX", -1).AsInt32(), data.GetValueOrDefault("targetY", -1).AsInt32());
		pullTargetGrid = ImportPullTargetGrid(data);
		ImportPullTargetIdentity(data);
		_actionSequence = data.GetValueOrDefault("actionSequence", 0).AsInt32();
		_networkSpecialStateRevision = data.GetValueOrDefault("networkSpecialStateRevision", 1).AsInt32();
		if (GodotObject.IsInstanceValid(_pullEffect))
		{
			_pullEffect.QueueFree();
		}
		_pullEffect = null;
		_pullEffectRestorePending = pullRemaining > 0.0;
		PrepareMoveTweenRestore(data, _actionSequence);
		_pendingCrushes.Clear();
		foreach (Variant item in data.GetValueOrDefault("pendingCrushes", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			int num = dictionary.GetValueOrDefault("syncId", -1).AsInt32();
			Vector2I grid = new Vector2I(dictionary.GetValueOrDefault("gridX", -1).AsInt32(), dictionary.GetValueOrDefault("gridY", -1).AsInt32());
			if (num >= 0 || (grid.X >= 1 && grid.Y >= 1))
			{
				TowerDefensePlant plant = ResolvePlant(num);
				_pendingCrushes.Add(new PendingCrush
				{
					Plant = plant,
					SyncId = num,
					Grid = grid,
					NodeName = NormalizeSavedNodeName(dictionary.GetValueOrDefault("nodeName", "").AsString()),
					SaveKey = dictionary.GetValueOrDefault("saveKey", "").AsString(),
					Remaining = dictionary.GetValueOrDefault("remaining", 0.0).AsDouble()
				});
			}
		}
	}

	private Vector2I ImportPullTargetGrid(Dictionary data)
	{
		Vector2I legacyPullTargetGrid = GetLegacyPullTargetGrid();
		return new Vector2I(data.GetValueOrDefault("pullTargetX", legacyPullTargetGrid.X).AsInt32(), data.GetValueOrDefault("pullTargetY", legacyPullTargetGrid.Y).AsInt32());
	}

	private int ImportStopColumn(Dictionary data, int existing)
	{
		if (data.ContainsKey("stopColumn"))
		{
			int num = data.GetValueOrDefault("stopColumn", -1).AsInt32();
			if (num >= 1)
			{
				return Mathf.Clamp(num, 1, GetMapLastColumn());
			}
			return -1;
		}
		if (existing >= 1)
		{
			return Mathf.Clamp(existing, 1, GetMapLastColumn());
		}
		if (!anchored && !shooting && !(pullRemaining > 0.0))
		{
			return -1;
		}
		if (!CrushOnLeft)
		{
			return GetMapLastColumn();
		}
		return 1;
	}

	private void ImportPullTargetIdentity(Dictionary data)
	{
		_pullTargetSyncId = data.GetValueOrDefault("pullTargetSyncId", -1).AsInt32();
		_pullTargetNodeName = NormalizeSavedNodeName(data.GetValueOrDefault("pullTargetNodeName", "").AsString());
		_pullTargetSaveKey = data.GetValueOrDefault("pullTargetSaveKey", "").AsString();
	}

	private Vector2I GetEffectivePullTargetGrid()
	{
		if (pullTargetGrid.X < 1 || pullTargetGrid.Y < 1)
		{
			return GetLegacyPullTargetGrid();
		}
		return pullTargetGrid;
	}

	private Vector2I GetLegacyPullTargetGrid()
	{
		if (targetGrid.X < 1 || targetGrid.Y < 1)
		{
			return new Vector2I(-1, -1);
		}
		return new Vector2I(Math.Min(targetGrid.X + 1, GetTerminalColumn()), targetGrid.Y);
	}

	private TowerDefensePlant ResolvePendingCrush(PendingCrush pending)
	{
		TowerDefensePlant result = ResolvePlant(pending.SyncId);
		if (GodotObject.IsInstanceValid(result))
		{
			return result;
		}
		if (!TowerDefenseManager.Instance.CheckMapGridPosIn(pending.Grid))
		{
			return null;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(pending.Grid);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return null;
		}
		List<TowerDefensePlant> source = (from plant in mapCell.characterList.OfType<TowerDefensePlant>()
			where IsDraggablePlant(plant, base.camp) && (string.IsNullOrEmpty(pending.SaveKey) || plant.packet?.saveKey == pending.SaveKey) && !_pendingCrushes.Any((PendingCrush other) => other != pending && other.Plant == plant)
			select plant).ToList();
		if (!string.IsNullOrEmpty(pending.NodeName))
		{
			return source.FirstOrDefault((TowerDefensePlant plant) => HasSavedNodeName(plant, pending.NodeName));
		}
		return null;
	}

	private static string GetSavedNodeName(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return "";
		}
		return NormalizeSavedNodeName(node.Name.ToString());
	}

	private static string NormalizeSavedNodeName(string nodeName)
	{
		if (!string.IsNullOrEmpty(nodeName))
		{
			return nodeName.ValidateNodeName();
		}
		return "";
	}

	private static bool HasSavedNodeName(Node node, string savedNodeName)
	{
		if (!string.IsNullOrEmpty(savedNodeName))
		{
			return GetSavedNodeName(node) == NormalizeSavedNodeName(savedNodeName);
		}
		return false;
	}

	private static TowerDefensePlant ResolvePlant(int syncId)
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (syncId < 0 || !GodotObject.IsInstanceValid(currentControl) || !currentControl._syncCharacters.TryGetValue(syncId, out var value))
		{
			return null;
		}
		return value as TowerDefensePlant;
	}
}
