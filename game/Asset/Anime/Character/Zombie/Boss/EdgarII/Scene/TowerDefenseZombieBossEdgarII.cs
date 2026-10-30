using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class TowerDefenseZombieBossEdgarII : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private enum EdgarAction
	{
		Idle,
		FlyToSmash,
		Smash,
		FlyHome,
		Fire,
		Pulse,
		Summon,
		Entering
	}

	private const int SaveVersion = 3;

	private const double IdleDuration = 10.0;

	private const double FlightDuration = 0.75;

	private const double IdleMoveWaitMinimum = 3.0;

	private const double IdleMoveWaitMaximum = 6.0;

	private const double IdleRowMoveDurationPerRow = 0.75;

	private const double PulseResendDuration = 0.75;

	private const double PulseResendInterval = 0.1;

	private const double RemotePulseResolveDuration = 1.0;

	private const string FireballConfigName = "ZombieBossEdgarIIFireball";

	private static readonly string[] SummonGiantPool = new string[16]
	{
		"ZombieGargantuar", "ZombieGargantuarRedEyes", "ZombieGargantuarGloompult", "ZombieGargantuarPresentBox", "ZombieGargantuarWallnutZ", "ZombieSkeletuar", "ZombieGargantuarBucket", "ZombieGargantuarHelmet", "ZombieGargantuarScreendoor", "ZombieGargantuarShield",
		"ZombieGargantuarBlackHelmet", "ZombieGargantuarSpecialHelmet", "ZombieFootballGargantuar", "ZombieGargantuarZamboni", "ZombieGargantuarDiamond", "ZombieDiscoGargantuar"
	};

	private const string SpeedDownEffectPath = "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Effect/EdgarSPDown.tscn";

	private const int SkillWeightTotal = 40;

	private EdgarAction _action;

	private double _idleTimer;

	private double _flightTimer;

	private Vector2 _flightStart;

	private Vector2 _flightEnd;

	private Vector2I _smashGrid = Vector2I.One;

	private double _idleMoveWaitTimer;

	private double _idleMoveWaitDuration;

	private bool _idleRowMoveActive;

	private double _idleRowMoveTimer;

	private double _idleRowMoveDuration;

	private Vector2 _idleRowMoveStart;

	private Vector2 _idleRowMoveEnd;

	private int _idleRowMoveTargetRow;

	private bool _skillPendingAfterRowMove;

	private int _lastSkill;

	private int _skillRepeatCount;

	private int _networkRevision;

	private int _pulseActionSequence;

	private int _remotePulseActionSequence;

	private bool _remotePulseStateInitialized;

	private bool _remoteNetworkStateInitialized;

	private double _pulseResendTimer;

	private double _pulseResendAccumulator;

	private readonly Array<Vector2I> _pulseAffectedCells = new Array<Vector2I>();

	private readonly Array<int> _pulseAffectedCharacterIds = new Array<int>();

	private Array<Vector2I> _pendingRemotePulseCells;

	private Array<int> _pendingRemotePulseCharacterIds;

	private double _pendingRemotePulseResolveTimer;

	private PackedScene _speedDownEffect;

	private bool _pendingPresentation;

	private int _pendingPresentationFrame = -1;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.HasValidRuntimeConfiguration)
		{
			TargetRegistrationComponent obj = targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				targetRegistrationComponent.attackGridLineAliasOffset = -1;
			}
			AttackComponent obj2 = attackComponent;
			if (obj2 != null && !obj2.IsReleased)
			{
				attackComponent.alive = false;
				attackComponent.SetAlive(alive: false);
			}
			_speedDownEffect = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Boss/EdgarII/Effect/EdgarSPDown.tscn", null, ResourceLoader.CacheMode.Reuse);
			TryPlayPendingRemotePulseEffects(0.0);
			if (_pendingPresentation)
			{
				ApplyPresentation();
			}
			else
			{
				EnterIdle(resetTimer: true);
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !inGame || die || TowerDefenseManager.Instance == null || !TowerDefenseManager.Instance.IsGameRunning())
		{
			return;
		}
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			AdvancePulseResend(delta);
		}
		TryPlayPendingRemotePulseEffects(delta);
		if (_action == EdgarAction.Entering)
		{
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				AdvanceEntry();
			}
			return;
		}
		EdgarAction action = _action;
		if ((action == EdgarAction.FlyToSmash || action == EdgarAction.FlyHome) ? true : false)
		{
			AdvanceFlight(delta);
		}
		if (_action == EdgarAction.Idle && _idleRowMoveActive)
		{
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				_idleTimer += delta;
				if (_idleTimer >= 10.0)
				{
					AdvanceIdleRowMove(Math.Max(0.0, _idleRowMoveDuration - _idleRowMoveTimer));
					_skillPendingAfterRowMove = false;
					_idleTimer = 0.0;
					StartNextSkill();
					return;
				}
			}
			AdvanceIdleRowMove(delta);
		}
		else
		{
			if (!TowerDefenseManager.HasGameplayAuthority || _action != EdgarAction.Idle)
			{
				return;
			}
			_idleTimer += delta;
			if (_idleTimer >= 10.0)
			{
				_idleTimer = 0.0;
				StartNextSkill();
				return;
			}
			_idleMoveWaitTimer += delta;
			if (_idleMoveWaitTimer >= _idleMoveWaitDuration)
			{
				StartIdleRowMove();
			}
		}
	}

	public override void Walk()
	{
		if (_action == EdgarAction.Entering)
		{
			base.Walk();
		}
		else if (ShouldBeginEntry())
		{
			BeginEntry();
		}
		else
		{
			Idle();
		}
	}

	public override bool ShouldUpdateGridPos()
	{
		if (inGame && (base.gridPos.X <= 0 || _action == EdgarAction.Entering))
		{
			return false;
		}
		return base.ShouldUpdateGridPos();
	}

	private void StartNextSkill()
	{
		int skill = ((_skillRepeatCount >= 2) ? _lastSkill : 0);
		int num = 40 - GetSkillWeight(skill);
		switch (SelectNextSkillByRoll(GD.RandRange(0, num - 1)))
		{
		case 2:
			StartSmashFlight();
			break;
		case 3:
			SetAction(EdgarAction.Fire, "Fire", loop: false);
			break;
		case 4:
			SetAction(EdgarAction.Pulse, "Electronic", loop: false);
			break;
		default:
			SetAction(EdgarAction.Summon, "Summon", loop: false);
			break;
		}
	}

	private bool ShouldBeginEntry()
	{
		if (editorPreviewMode || !TowerDefenseManager.HasGameplayAuthority || base.gridPos.X > 0 || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return false;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		if (mapGridNum.X > 0)
		{
			return mapGridNum.Y > 0;
		}
		return false;
	}

	private void BeginEntry()
	{
		_action = EdgarAction.Entering;
		_idleTimer = 0.0;
		_idleRowMoveActive = false;
		_skillPendingAfterRowMove = false;
		base.sprite?.SetAnimation("Walk");
		base.Walk();
		MarkStateDirty();
	}

	private void AdvanceEntry()
	{
		Vector2I vector2I = ResolveHomeGridPosition(TowerDefenseManager.Instance.GetMapGridNum(), base.gridPos.Y);
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
		if (!(GetLogicalGlobalPosition().X > mapCellPlantPos.X))
		{
			base.gridPos = vector2I;
			SetLogicalGlobalPosition(mapCellPlantPos);
			EnterIdle(resetTimer: true);
		}
	}

	private void StartIdleRowMove()
	{
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		if (mapGridNum.Y <= 1)
		{
			ScheduleNextIdleMove();
			return;
		}
		int num = Mathf.Clamp(base.gridPos.Y, 1, mapGridNum.Y);
		int num2 = GD.RandRange(1, mapGridNum.Y - 1);
		if (num2 >= num)
		{
			num2++;
		}
		_idleRowMoveTargetRow = num2;
		_idleRowMoveTimer = 0.0;
		_idleRowMoveDuration = (double)Math.Abs(num2 - num) * 0.75;
		if (_idleTimer + _idleRowMoveDuration >= 10.0)
		{
			_idleMoveWaitTimer = 0.0;
			_idleMoveWaitDuration = Math.Max(0.0, 10.0 - _idleTimer);
			return;
		}
		_idleRowMoveStart = GetLogicalGlobalPosition();
		_idleRowMoveEnd = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(GetHomeColumn(), num2));
		_idleRowMoveActive = true;
		MarkStateDirty();
	}

	private void AdvanceIdleRowMove(double delta)
	{
		_idleRowMoveTimer = Math.Min(_idleRowMoveDuration, _idleRowMoveTimer + delta);
		float weight = ((_idleRowMoveDuration <= 0.0) ? 1f : ((float)(_idleRowMoveTimer / _idleRowMoveDuration)));
		SetLogicalGlobalPosition(_idleRowMoveStart.Lerp(_idleRowMoveEnd, weight));
		if (!(_idleRowMoveTimer < _idleRowMoveDuration))
		{
			SetLogicalGlobalPosition(_idleRowMoveEnd);
			base.gridPos = new Vector2I(GetHomeColumn(), _idleRowMoveTargetRow);
			_idleRowMoveActive = false;
			_skillPendingAfterRowMove = false;
			ScheduleNextIdleMove();
			MarkStateDirty();
		}
	}

	private void ScheduleNextIdleMove()
	{
		_idleMoveWaitTimer = 0.0;
		_idleMoveWaitDuration = (TowerDefenseManager.HasGameplayAuthority ? GD.RandRange(3.0, 6.0) : 0.0);
	}

	private void StartSmashFlight()
	{
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		int num = Math.Min(4, mapGridNum.X);
		_smashGrid = new Vector2I(GD.RandRange(num, Math.Max(num, mapGridNum.X)), GD.RandRange(1, Math.Max(1, mapGridNum.Y)));
		StartFlight(EdgarAction.FlyToSmash, _smashGrid, "Walk");
	}

	private void StartFlight(EdgarAction action, Vector2I destination, string clip)
	{
		_action = action;
		_flightTimer = 0.0;
		_flightStart = GetLogicalGlobalPosition();
		_flightEnd = TowerDefenseManager.GetMapCellPlantPos(destination);
		base.gridPos = destination;
		base.sprite?.SetAnimation(clip);
		MarkStateDirty();
	}

	private void AdvanceFlight(double delta)
	{
		_flightTimer = Math.Min(0.75, _flightTimer + delta);
		float weight = (float)(_flightTimer / 0.75);
		SetLogicalGlobalPosition(_flightStart.Lerp(_flightEnd, weight));
		if (!(_flightTimer < 0.75) && TowerDefenseManager.HasGameplayAuthority)
		{
			SetLogicalGlobalPosition(_flightEnd);
			if (_action == EdgarAction.FlyToSmash)
			{
				SetAction(EdgarAction.Smash, "Smash", loop: false);
			}
			else
			{
				EnterIdle(resetTimer: true);
			}
		}
	}

	private void ReturnHome()
	{
		StartFlight(EdgarAction.FlyHome, new Vector2I(GetHomeColumn(), Mathf.Clamp(_smashGrid.Y, 1, TowerDefenseManager.Instance.GetMapGridNum().Y)), "Walk2");
	}

	private int GetHomeColumn()
	{
		return ResolveHomeGridPosition(TowerDefenseManager.Instance.GetMapGridNum(), base.gridPos.Y).X;
	}

	internal static Vector2I ResolveHomeGridPosition(Vector2I mapSize, int requestedRow)
	{
		return new Vector2I(Mathf.Clamp(9, 1, Math.Max(1, mapSize.X)), Mathf.Clamp(requestedRow, 1, Math.Max(1, mapSize.Y)));
	}

	private void SetAction(EdgarAction action, string clip, bool loop)
	{
		_action = action;
		base.sprite?.SetAnimation(clip, loop);
		MarkStateDirty();
	}

	private void EnterIdle(bool resetTimer)
	{
		_action = EdgarAction.Idle;
		Idle();
		if (resetTimer)
		{
			_idleTimer = 0.0;
			_idleRowMoveActive = false;
			_skillPendingAfterRowMove = false;
			ScheduleNextIdleMove();
		}
		base.sprite?.SetAnimation("Idle");
		MarkStateDirty();
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!TowerDefenseManager.HasGameplayAuthority || die)
		{
			return;
		}
		switch (command)
		{
		case "smash":
			if (_action == EdgarAction.Smash)
			{
				ResolveSmash();
			}
			break;
		case "fire":
			if (_action == EdgarAction.Fire)
			{
				ResolveFireball();
			}
			break;
		case "pulse":
			if (_action == EdgarAction.Pulse)
			{
				ResolvePulse();
			}
			break;
		case "summon":
			if (_action == EdgarAction.Summon)
			{
				ResolveSummon();
			}
			break;
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!TowerDefenseManager.HasGameplayAuthority || die)
		{
			return;
		}
		switch (_action)
		{
		case EdgarAction.Smash:
			if (clip == "Smash")
			{
				ReturnHome();
			}
			break;
		case EdgarAction.Fire:
			if (!(clip == "Fire"))
			{
				break;
			}
			goto IL_0078;
		case EdgarAction.Pulse:
			if (!(clip == "Electronic"))
			{
				break;
			}
			goto IL_0078;
		case EdgarAction.Summon:
			if (!(clip == "Summon"))
			{
				break;
			}
			goto IL_0078;
		case EdgarAction.FlyHome:
			break;
			IL_0078:
			EnterIdle(resetTimer: true);
			break;
		}
	}

	private void ResolveSmash()
	{
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (cleanCharacters is TowerDefensePlant { die: false, nearDie: false, inGame: not false } towerDefensePlant)
			{
				Vector2I vector2I = towerDefensePlant.gridPos - _smashGrid;
				if (vector2I.X <= 0 && vector2I.X >= -2 && Math.Abs(vector2I.Y) <= 1)
				{
					towerDefensePlant.SmashHurt(10000000.0, playSplatAudio: true, Vector2.Zero);
				}
			}
		}
	}

	private void ResolveFireball()
	{
		TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig("ZombieBossEdgarIIFireball");
		if (projectileConfig != null)
		{
			Vector2 vector = GetLogicalGlobalPosition() + new Vector2(-55f, 0f);
			double height = GetGroundHeight(vector.Y) - base.groundHeight;
			Vector2 pos = vector;
			Vector2 velocity = new Vector2(-300f, 0f);
			int collisionFlags = projectileConfig.collisionFlags;
			TowerDefenseEnum.CHARACTER_CAMP num = base.camp;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				flipXOverride = true
			};
			FireComponent.TryCreateProjectilePositionByConfig(this, null, height, pos, velocity, projectileConfig, out var _, collisionFlags, num, default(Vector2), overrides);
		}
	}

	private void ResolvePulse()
	{
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		HashSet<int> hashSet2 = new HashSet<int>();
		List<TowerDefensePlant> list = new List<TowerDefensePlant>();
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (cleanCharacters is TowerDefensePlant towerDefensePlant && !(towerDefensePlant is TowerDefensePlantBowlingBase) && !towerDefensePlant.die && !towerDefensePlant.nearDie && towerDefensePlant.inGame && TowerDefenseManager.Instance.CheckMapGridPosIn(towerDefensePlant.gridPos))
			{
				towerDefensePlant.buff.AddBuff(new TowerDefenseCharacterBuffAttackSpeedDown
				{
					timeScaleValue = 0.5,
					time = 15.0
				});
				hashSet.Add(towerDefensePlant.gridPos);
				list.Add(towerDefensePlant);
				if (towerDefensePlant.syncId >= 0)
				{
					hashSet2.Add(towerDefensePlant.syncId);
				}
			}
		}
		List<Vector2I> list2 = new List<Vector2I>(hashSet);
		list2.Sort(delegate(Vector2I left, Vector2I right)
		{
			int num = left.Y.CompareTo(right.Y);
			return (num == 0) ? left.X.CompareTo(right.X) : num;
		});
		_pulseAffectedCells.Clear();
		foreach (Vector2I item in list2)
		{
			_pulseAffectedCells.Add(item);
		}
		List<int> list3 = new List<int>(hashSet2);
		list3.Sort();
		_pulseAffectedCharacterIds.Clear();
		foreach (int item2 in list3)
		{
			_pulseAffectedCharacterIds.Add(item2);
		}
		_pulseActionSequence++;
		_pulseResendTimer = 0.75;
		_pulseResendAccumulator = 0.0;
		PlayPulseEffects(list);
		MarkStateDirty();
	}

	private void PlayPulseEffects(IReadOnlyList<TowerDefensePlant> affectedPlants)
	{
		if (_speedDownEffect == null || affectedPlants == null)
		{
			return;
		}
		foreach (TowerDefensePlant affectedPlant in affectedPlants)
		{
			if (!GodotObject.IsInstanceValid(affectedPlant) || affectedPlant.die || affectedPlant.nearDie || !affectedPlant.inGame)
			{
				continue;
			}
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(_speedDownEffect, affectedPlant.gridPos, "Fire");
			if (!GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				continue;
			}
			Node2D node2D = (GodotObject.IsInstanceValid(affectedPlant.frontEffectNode) ? affectedPlant.frontEffectNode : TowerDefenseManager.GetCharacterNode());
			if (!GodotObject.IsInstanceValid(node2D))
			{
				towerDefenseEffectSpriteOnce.Free();
				continue;
			}
			node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			if (node2D == affectedPlant.frontEffectNode)
			{
				towerDefenseEffectSpriteOnce.Position = Vector2.Zero;
				towerDefenseEffectSpriteOnce.ZIndex = 0;
			}
			else
			{
				towerDefenseEffectSpriteOnce.GlobalPosition = affectedPlant.GetLogicalGlobalPosition();
			}
		}
	}

	private int SelectNextSkillByRoll(int roll)
	{
		int excludedSkill = ((_skillRepeatCount >= 2) ? _lastSkill : 0);
		int num = PickSkillByRoll(roll, excludedSkill);
		if (num == _lastSkill)
		{
			_skillRepeatCount = Math.Min(2, _skillRepeatCount + 1);
		}
		else
		{
			_lastSkill = num;
			_skillRepeatCount = 1;
		}
		return num;
	}

	private static int PickSkillByRoll(int roll, int excludedSkill)
	{
		int num = 40 - GetSkillWeight(excludedSkill);
		if (roll < 0 || roll >= num)
		{
			throw new ArgumentOutOfRangeException("roll");
		}
		for (int i = 2; i <= 5; i++)
		{
			if (i != excludedSkill)
			{
				int skillWeight = GetSkillWeight(i);
				if (roll < skillWeight)
				{
					return i;
				}
				roll -= skillWeight;
			}
		}
		throw new InvalidOperationException("Edgar II skill weight table is invalid.");
	}

	private static int GetSkillWeight(int skill)
	{
		return skill switch
		{
			2 => 5, 
			3 => 12, 
			4 => 8, 
			5 => 15, 
			_ => 0, 
		};
	}

	private void PlayLegacyPulseEffects(Array<Vector2I> affectedCells)
	{
		if (_speedDownEffect == null || affectedCells == null)
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		foreach (Vector2I affectedCell in affectedCells)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(_speedDownEffect, affectedCell, "Fire");
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(affectedCell);
			}
		}
	}

	private void TryPlayPendingRemotePulseEffects(double delta)
	{
		if (!IsNodeReady() || _speedDownEffect == null)
		{
			return;
		}
		Array<int> pendingRemotePulseCharacterIds = _pendingRemotePulseCharacterIds;
		if (pendingRemotePulseCharacterIds != null && pendingRemotePulseCharacterIds.Count > 0)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (!GodotObject.IsInstanceValid(currentControl))
			{
				_pendingRemotePulseResolveTimer = Math.Max(0.0, _pendingRemotePulseResolveTimer - delta);
				if (_pendingRemotePulseResolveTimer <= 0.0)
				{
					_pendingRemotePulseCharacterIds = null;
					_pendingRemotePulseCells = null;
				}
				return;
			}
			List<TowerDefensePlant> list = new List<TowerDefensePlant>(_pendingRemotePulseCharacterIds.Count);
			Array<int> array = new Array<int>();
			foreach (int pendingRemotePulseCharacterId in _pendingRemotePulseCharacterIds)
			{
				if (!currentControl._syncCharacters.TryGetValue(pendingRemotePulseCharacterId, out var value) || !(value is TowerDefensePlant item) || !GodotObject.IsInstanceValid(item))
				{
					array.Add(pendingRemotePulseCharacterId);
				}
				else
				{
					list.Add(item);
				}
			}
			PlayPulseEffects(list);
			_pendingRemotePulseResolveTimer = Math.Max(0.0, _pendingRemotePulseResolveTimer - delta);
			_pendingRemotePulseCharacterIds = ((array.Count > 0 && _pendingRemotePulseResolveTimer > 0.0) ? array : null);
			if (_pendingRemotePulseCharacterIds == null)
			{
				_pendingRemotePulseCells = null;
			}
		}
		else if (_pendingRemotePulseCells != null)
		{
			PlayLegacyPulseEffects(_pendingRemotePulseCells);
			_pendingRemotePulseCells = null;
		}
	}

	private void ResolveSummon()
	{
		if (SummonGiantPool.Length == 0)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(SummonGiantPool[GD.RandRange(0, SummonGiantPool.Length - 1)]);
		if (packetConfig != null)
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			for (int i = 1; i <= mapGridNum.Y; i++)
			{
				packetConfig.Plant(new Vector2I(mapGridNum.X, i), playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
			}
		}
	}

	private void MarkStateDirty()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_networkRevision++;
		}
	}

	private void AdvancePulseResend(double delta)
	{
		if (!(_pulseResendTimer <= 0.0))
		{
			_pulseResendTimer = Math.Max(0.0, _pulseResendTimer - delta);
			_pulseResendAccumulator += delta;
			if (!(_pulseResendAccumulator < 0.1))
			{
				_pulseResendAccumulator %= 0.1;
				MarkStateDirty();
			}
		}
	}

	private Dictionary ExportEdgarState(bool includePulseState)
	{
		Dictionary dictionary = new Dictionary
		{
			["schemaVersion"] = 3,
			["action"] = (int)_action,
			["idleTimer"] = _idleTimer,
			["flightTimer"] = _flightTimer,
			["flightStartX"] = _flightStart.X,
			["flightStartY"] = _flightStart.Y,
			["flightEndX"] = _flightEnd.X,
			["flightEndY"] = _flightEnd.Y,
			["smashGridX"] = _smashGrid.X,
			["smashGridY"] = _smashGrid.Y,
			["idleMoveWaitTimer"] = _idleMoveWaitTimer,
			["idleMoveWaitDuration"] = _idleMoveWaitDuration,
			["idleRowMoveActive"] = _idleRowMoveActive,
			["idleRowMoveTimer"] = _idleRowMoveTimer,
			["idleRowMoveDuration"] = _idleRowMoveDuration,
			["idleRowMoveStartX"] = _idleRowMoveStart.X,
			["idleRowMoveStartY"] = _idleRowMoveStart.Y,
			["idleRowMoveEndX"] = _idleRowMoveEnd.X,
			["idleRowMoveEndY"] = _idleRowMoveEnd.Y,
			["idleRowMoveTargetRow"] = _idleRowMoveTargetRow,
			["skillPendingAfterRowMove"] = _skillPendingAfterRowMove,
			["lastSkill"] = _lastSkill,
			["skillRepeatCount"] = _skillRepeatCount,
			["animationFrame"] = (GodotObject.IsInstanceValid(base.sprite) ? base.sprite.frameIndex : (-1))
		};
		if (includePulseState)
		{
			dictionary["rev"] = _networkRevision;
			dictionary["pulseActionSequence"] = _pulseActionSequence;
			dictionary["pulseAffectedCells"] = SerializeGridPositions(_pulseAffectedCells);
			dictionary["pulseAffectedCharacterIds"] = new Array<int>(_pulseAffectedCharacterIds);
		}
		return dictionary;
	}

	private void ImportEdgarState(Dictionary data, bool includePulseState, bool enforceNetworkRevision)
	{
		if (data == null)
		{
			return;
		}
		bool flag = !IsNodeReady();
		int num = data.GetValueOrDefault("schemaVersion", 1).AsInt32();
		int num2 = (enforceNetworkRevision ? data.GetValueOrDefault("rev", 0).AsInt32() : _networkRevision);
		bool flag2 = !TowerDefenseManager.HasGameplayAuthority;
		if (flag2 && enforceNetworkRevision && _remoteNetworkStateInitialized && num2 <= _networkRevision)
		{
			if (includePulseState)
			{
				ImportPulseNetworkState(data);
			}
			return;
		}
		if (flag2 && enforceNetworkRevision)
		{
			_remoteNetworkStateInitialized = true;
		}
		if (enforceNetworkRevision)
		{
			_networkRevision = Math.Max(_networkRevision, num2);
		}
		EdgarAction edgarAction = (EdgarAction)data.GetValueOrDefault("action", (int)_action).AsInt32();
		bool flag3 = edgarAction != _action;
		_action = edgarAction;
		_idleTimer = data.GetValueOrDefault("idleTimer", _idleTimer).AsDouble();
		_flightTimer = data.GetValueOrDefault("flightTimer", _flightTimer).AsDouble();
		_flightStart = new Vector2(data.GetValueOrDefault("flightStartX", _flightStart.X).AsSingle(), data.GetValueOrDefault("flightStartY", _flightStart.Y).AsSingle());
		_flightEnd = new Vector2(data.GetValueOrDefault("flightEndX", _flightEnd.X).AsSingle(), data.GetValueOrDefault("flightEndY", _flightEnd.Y).AsSingle());
		_smashGrid = new Vector2I(data.GetValueOrDefault("smashGridX", _smashGrid.X).AsInt32(), data.GetValueOrDefault("smashGridY", _smashGrid.Y).AsInt32());
		_idleMoveWaitTimer = data.GetValueOrDefault("idleMoveWaitTimer", _idleMoveWaitTimer).AsDouble();
		_idleMoveWaitDuration = data.GetValueOrDefault("idleMoveWaitDuration", _idleMoveWaitDuration).AsDouble();
		_idleRowMoveActive = data.GetValueOrDefault("idleRowMoveActive", false).AsBool();
		_idleRowMoveTimer = data.GetValueOrDefault("idleRowMoveTimer", 0.0).AsDouble();
		_idleRowMoveDuration = data.GetValueOrDefault("idleRowMoveDuration", 0.0).AsDouble();
		_idleRowMoveStart = new Vector2(data.GetValueOrDefault("idleRowMoveStartX", _idleRowMoveStart.X).AsSingle(), data.GetValueOrDefault("idleRowMoveStartY", _idleRowMoveStart.Y).AsSingle());
		_idleRowMoveEnd = new Vector2(data.GetValueOrDefault("idleRowMoveEndX", _idleRowMoveEnd.X).AsSingle(), data.GetValueOrDefault("idleRowMoveEndY", _idleRowMoveEnd.Y).AsSingle());
		_idleRowMoveTargetRow = data.GetValueOrDefault("idleRowMoveTargetRow", _idleRowMoveTargetRow).AsInt32();
		_skillPendingAfterRowMove = data.GetValueOrDefault("skillPendingAfterRowMove", false).AsBool();
		if (num < 2)
		{
			_idleRowMoveActive = false;
			_skillPendingAfterRowMove = false;
			if (_action == EdgarAction.Idle && TowerDefenseManager.HasGameplayAuthority)
			{
				ScheduleNextIdleMove();
			}
		}
		_lastSkill = data.GetValueOrDefault("lastSkill", _lastSkill).AsInt32();
		int lastSkill = _lastSkill;
		if (lastSkill >= 2 && lastSkill <= 5)
		{
			int num3 = ((num < 3) ? 1 : Math.Max(1, _skillRepeatCount));
			_skillRepeatCount = Math.Clamp(data.GetValueOrDefault("skillRepeatCount", num3).AsInt32(), 1, 2);
		}
		else
		{
			_lastSkill = 0;
			_skillRepeatCount = 0;
		}
		_pendingPresentationFrame = data.GetValueOrDefault("animationFrame", _pendingPresentationFrame).AsInt32();
		if (includePulseState && flag2)
		{
			ImportPulseNetworkState(data);
		}
		_pendingPresentation |= flag3 || flag;
		if (_pendingPresentation && IsNodeReady())
		{
			ApplyPresentation();
		}
	}

	private void ImportPulseNetworkState(Dictionary data)
	{
		int num = data.GetValueOrDefault("pulseActionSequence", _remotePulseActionSequence).AsInt32();
		Array<Vector2I> pendingRemotePulseCells = DeserializeGridPositions(data.GetValueOrDefault("pulseAffectedCells", new Array<int>()));
		Array<int> pendingRemotePulseCharacterIds = DeserializeIntegerArray(data.GetValueOrDefault("pulseAffectedCharacterIds", new Array<int>()));
		if (!_remotePulseStateInitialized)
		{
			_remotePulseStateInitialized = true;
			_remotePulseActionSequence = num;
		}
		else if (num > _remotePulseActionSequence)
		{
			_remotePulseActionSequence = num;
			_pendingRemotePulseCharacterIds = pendingRemotePulseCharacterIds;
			_pendingRemotePulseCells = pendingRemotePulseCells;
			_pendingRemotePulseResolveTimer = 1.0;
			TryPlayPendingRemotePulseEffects(0.0);
		}
	}

	private static Array<int> SerializeGridPositions(Array<Vector2I> positions)
	{
		Array<int> array = new Array<int>();
		foreach (Vector2I position in positions)
		{
			array.Add(position.X);
			array.Add(position.Y);
		}
		return array;
	}

	private static Array<Vector2I> DeserializeGridPositions(Variant value)
	{
		Array<Vector2I> array = new Array<Vector2I>();
		Godot.Collections.Array array2 = value.AsGodotArray();
		for (int i = 0; i + 1 < array2.Count; i += 2)
		{
			array.Add(new Vector2I(array2[i].AsInt32(), array2[i + 1].AsInt32()));
		}
		return array;
	}

	private static Array<int> DeserializeIntegerArray(Variant value)
	{
		Array<int> array = new Array<int>();
		foreach (Variant item in value.AsGodotArray())
		{
			array.Add(item.AsInt32());
		}
		return array;
	}

	private void ApplyPresentation()
	{
		_pendingPresentation = false;
		if (!die)
		{
			TowerDefenseCharacterInstance towerDefenseCharacterInstance = instance;
			if (towerDefenseCharacterInstance == null || !towerDefenseCharacterInstance.die)
			{
				if (_action == EdgarAction.Entering && TowerDefenseManager.HasGameplayAuthority)
				{
					base.Walk();
				}
				string text = _action switch
				{
					EdgarAction.Entering => "Walk", 
					EdgarAction.FlyToSmash => "Walk", 
					EdgarAction.Smash => "Smash", 
					EdgarAction.FlyHome => "Walk2", 
					EdgarAction.Fire => "Fire", 
					EdgarAction.Pulse => "Electronic", 
					EdgarAction.Summon => "Summon", 
					_ => "Idle", 
				};
				AdobeAnimateSprite adobeAnimateSprite = base.sprite;
				if (adobeAnimateSprite != null)
				{
					string clipName = text;
					EdgarAction action = _action;
					bool loop = (((uint)action <= 1u || action == EdgarAction.FlyHome || action == EdgarAction.Entering) ? true : false);
					adobeAnimateSprite.SetAnimation(clipName, loop);
				}
				if (GodotObject.IsInstanceValid(base.sprite) && _pendingPresentationFrame >= 0)
				{
					base.sprite.frameIndex = Mathf.Clamp(_pendingPresentationFrame, base.sprite.clipRange.X, Math.Max(base.sprite.clipRange.X, base.sprite.clipRange.Y - 1));
					base.sprite.elapsedTimer = 0.0;
				}
				_pendingPresentationFrame = -1;
				return;
			}
		}
		_pendingPresentationFrame = -1;
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		ImportEdgarState(data, includePulseState: true, enforceNetworkRevision: true);
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		return ExportEdgarState(includePulseState: true);
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return ExportEdgarState(includePulseState: true);
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ImportEdgarState(data, includePulseState: true, enforceNetworkRevision: true);
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return _networkRevision;
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		EdgarAction action = _action;
		if ((action != EdgarAction.FlyToSmash && action != EdgarAction.FlyHome) || 1 == 0)
		{
			return _idleRowMoveActive;
		}
		return true;
	}

	public override Dictionary ExportVariantSave()
	{
		return ExportEdgarState(includePulseState: false);
	}

	public override void ImportVariantSave(Dictionary data)
	{
		ImportEdgarState(data, includePulseState: false, enforceNetworkRevision: false);
	}
}
