using Godot;
using Godot.Collections;

[Tool]
public abstract partial class TowerDefenseZombieBobsledVehicleBase : TowerDefenseZombie, INetworkBobsledOwner, INetworkSpawnStateReceiver
{
	private const double BrokenFrameDuration = 1.0 / 12.0;

	private const double NetworkPhaseResyncInterval = 0.5;

	private const float IceFrontEpsilon = 1f;

	private bool _phaseInitialized;

	private bool _iceBoostConsumed;

	private bool _releaseCompleted;

	private double _breakVisualRemaining;

	private double _networkPhaseResyncTimer;

	private int _networkSpecialStateRevision = 1;

	private int _lastImportedNetworkSpecialStateRevision;

	private bool _hasImportedNetworkSpecialState;

	[Export(PropertyHint.None, "")]
	public double slowSpeed { get; set; } = 30.0;

	[Export(PropertyHint.None, "")]
	public double iceSpeed { get; set; } = 60.0;

	[Export(PropertyHint.None, "")]
	public bool requiresIce { get; set; }

	[Export(PropertyHint.None, "")]
	public bool createsIce { get; set; }

	[Export(PropertyHint.None, "")]
	public bool crushesPlants { get; set; }

	[Export(PropertyHint.None, "")]
	public bool usesEntryChoreography { get; set; }

	protected BobsledTeamComponent TeamComponent { get; private set; }

	protected Marker2D IceCapMarker { get; private set; }

	public BobsledVehiclePhase CurrentPhase { get; private set; } = BobsledVehiclePhase.Riding;

	public double CurrentSpeed { get; private set; }

	public bool HasCapturedIceFront { get; private set; }

	public float CapturedIceFrontX { get; private set; }

	protected abstract bool UsesBreakAnimationCompletion { get; }

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			TeamComponent = componentManager?.GetRuntime<BobsledTeamComponent>();
			IceCapMarker = GetNodeOrNull<Marker2D>("%IceCapMarker");
			if (_phaseInitialized)
			{
				ApplyPhasePresentation();
			}
			else
			{
				InitializeVehiclePhase();
			}
		}
	}

	public bool TrySetNetworkBobsledPassenger(int slot, TowerDefenseCharacter passenger)
	{
		if (TeamComponent == null)
		{
			BobsledTeamComponent bobsledTeamComponent = (TeamComponent = componentManager?.GetRuntime<BobsledTeamComponent>());
		}
		if (TeamComponent == null)
		{
			return false;
		}
		if (TeamComponent.IsSlotReleased(slot))
		{
			TeamComponent.ResolveReleasedNetworkPassenger(slot, passenger);
			return true;
		}
		TeamComponent.SetNetworkPassenger(slot, passenger);
		return TeamComponent.GetPassenger(slot) == passenger;
	}

	public override void WalkEntered()
	{
		if (!isShow)
		{
			InitializeVehiclePhase();
			if (TeamComponent == null)
			{
				BobsledTeamComponent bobsledTeamComponent = (TeamComponent = componentManager?.GetRuntime<BobsledTeamComponent>());
			}
			TeamComponent?.EnsureAutoSpawnPassengers();
			if (GodotObject.IsInstanceValid(base.sprite) && CurrentPhase != BobsledVehiclePhase.Breaking)
			{
				base.sprite.SetAnimation(GetRidingClip());
			}
		}
	}

	public override void WalkExited()
	{
	}

	public override void WalkProcessing(double delta)
	{
		if (isShow)
		{
			return;
		}
		InitializeVehiclePhase();
		ProcessNetworkPhaseResync(delta);
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.timeScale = timeScale;
		}
		if (CurrentPhase == BobsledVehiclePhase.Breaking)
		{
			ProcessBreaking(delta);
		}
		else
		{
			if (CurrentPhase == BobsledVehiclePhase.Released || base.IsRemoteNetworkReplica || !TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(instance))
			{
				return;
			}
			TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
			if (!GodotObject.IsInstanceValid(towerDefenseManager))
			{
				return;
			}
			if (requiresIce && !towerDefenseManager.TryGetIceCapFrontX(base.gridPos.Y, out var _))
			{
				BeginBreak(BobsledBreakReason.IceEnded);
				return;
			}
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			CurrentSpeed = ResolveCurrentSpeed(globalPositionForPhysicsFrame.X);
			if (!base.sprite.pause)
			{
				double num = (base.sprite.playBack ? 1.0 : (-1.0));
				double num2 = (((double)globalPositionForPhysicsFrame.X > towerDefenseManager.GetMapGroundRight()) ? 2.0 : 1.0);
				globalPositionForPhysicsFrame.X += (float)(CurrentSpeed * delta * timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * num * num2);
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (usesEntryChoreography && CurrentPhase == BobsledVehiclePhase.EnteringPush && (double)globalPositionForPhysicsFrame.X <= towerDefenseManager.GetMapGroundRight())
			{
				SetPhase(BobsledVehiclePhase.EnteringJump);
			}
			if (requiresIce && !instance.hypnoses && HasCapturedIceFront && globalPositionForPhysicsFrame.X <= CapturedIceFrontX + 1f)
			{
				BeginBreak(BobsledBreakReason.IceEnded);
				return;
			}
			if (createsIce && !instance.hypnoses)
			{
				Vector2 pos = (GodotObject.IsInstanceValid(IceCapMarker) ? GetLogicalGlobalPosition(IceCapMarker) : globalPositionForPhysicsFrame);
				towerDefenseManager.SetIceCapPos(base.gridPos.Y, pos);
			}
			if (crushesPlants)
			{
				ProcessVehicleAttack();
			}
		}
	}

	public override void DieEntered()
	{
		BeginBreak(BobsledBreakReason.Hitpoints);
	}

	public override void DieProcessing(double delta)
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.timeScale = timeScale;
		}
		ProcessBreaking(delta);
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		BeginBreak(BobsledBreakReason.Hitpoints);
	}

	public override void AnimeCompleted(string clip)
	{
		if (CurrentPhase == BobsledVehiclePhase.Breaking && clip == GetBreakClip())
		{
			if (UsesBreakAnimationCompletion)
			{
				FinishBreak();
			}
		}
		else
		{
			base.AnimeCompleted(clip);
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		if (!IsHardControlImmune)
		{
			base.Hypnoses(time, canFliter, hypnosesConfig);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		(TeamComponent ?? componentManager?.GetRuntime<BobsledTeamComponent>())?.SynchronizePassengerHypnosis();
	}

	protected void CompleteEntryJump()
	{
		if (CurrentPhase == BobsledVehiclePhase.EnteringJump)
		{
			SetPhase(BobsledVehiclePhase.Riding);
		}
	}

	protected void BeginBreak(BobsledBreakReason reason)
	{
		BobsledVehiclePhase currentPhase = CurrentPhase;
		if ((uint)(currentPhase - 3) > 1u)
		{
			SetPhase(BobsledVehiclePhase.Breaking);
			HitBoxDestroy();
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased)
			{
				attackComponent.SetAlive(alive: false);
			}
			if (reason == BobsledBreakReason.Spike)
			{
				die = true;
			}
			_breakVisualRemaining = (UsesBreakAnimationCompletion ? (1.0 / 0.0) : (1.0 / 12.0));
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation(GetBreakClip(), loop: false);
			}
		}
	}

	protected virtual string GetRidingClip()
	{
		return base.walkAnimeClip;
	}

	protected abstract string GetBreakClip();

	protected virtual void OnPhaseChanged(BobsledVehiclePhase phase)
	{
	}

	private void InitializeVehiclePhase()
	{
		if (!_phaseInitialized)
		{
			_phaseInitialized = true;
			BobsledVehiclePhase bobsledVehiclePhase = ((!usesEntryChoreography) ? BobsledVehiclePhase.Riding : BobsledVehiclePhase.EnteringPush);
			TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(towerDefenseManager))
			{
				HasCapturedIceFront = towerDefenseManager.TryGetIceCapFrontX(base.gridPos.Y, out var frontX);
				CapturedIceFrontX = frontX;
			}
			CurrentSpeed = ResolveCurrentSpeed(GetLogicalGlobalPosition().X);
			if (CurrentPhase == bobsledVehiclePhase)
			{
				ApplyPhasePresentation();
			}
			else
			{
				SetPhase(bobsledVehiclePhase);
			}
		}
	}

	private double ResolveCurrentSpeed(float currentX)
	{
		if (requiresIce)
		{
			return iceSpeed;
		}
		if (!_iceBoostConsumed && HasCapturedIceFront)
		{
			if (currentX > CapturedIceFrontX + 1f)
			{
				return iceSpeed;
			}
			_iceBoostConsumed = true;
		}
		return slowSpeed;
	}

	private void ProcessVehicleAttack()
	{
		AttackComponent obj = attackComponent;
		if (obj == null || obj.IsReleased || !attackComponent.CanAttack() || !GodotObject.IsInstanceValid(attackComponent.target))
		{
			return;
		}
		if (attackComponent.target is TowerDefensePlant && GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
		{
			attackComponent.target = attackComponent.target.cell.GetSpike();
		}
		TowerDefenseCharacter target = attackComponent.target;
		if (!GodotObject.IsInstanceValid(target?.instance))
		{
			return;
		}
		if ((target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			return;
		}
		if (target.instance.spikeHurt >= 0.0)
		{
			double spikeHurt = target.instance.spikeHurt;
			target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
		}
		BeginBreak(BobsledBreakReason.Spike);
	}

	private void ProcessBreaking(double delta)
	{
		if (CurrentPhase == BobsledVehiclePhase.Breaking && !UsesBreakAnimationCompletion && TowerDefenseManager.HasGameplayAuthority)
		{
			_breakVisualRemaining -= delta;
			if (_breakVisualRemaining <= 0.0)
			{
				FinishBreak();
			}
		}
	}

	private void FinishBreak()
	{
		if (!_releaseCompleted && TowerDefenseManager.HasGameplayAuthority)
		{
			_releaseCompleted = true;
			TeamComponent?.ReleaseAll();
			SetPhase(BobsledVehiclePhase.Released);
			Destroy();
		}
	}

	private void SetPhase(BobsledVehiclePhase phase)
	{
		if (CurrentPhase != phase)
		{
			CurrentPhase = phase;
			_networkPhaseResyncTimer = 0.0;
			MarkNetworkPhaseDirty();
			ApplyPhasePresentation();
		}
	}

	private void ApplyPhasePresentation()
	{
		OnPhaseChanged(CurrentPhase);
	}

	private void MarkNetworkPhaseDirty()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_networkSpecialStateRevision++;
			if (_networkSpecialStateRevision <= 0)
			{
				_networkSpecialStateRevision = 1;
			}
		}
	}

	private void ProcessNetworkPhaseResync(double delta)
	{
		if (usesEntryChoreography && Global.IsMultiplayerMode && TowerDefenseManager.HasGameplayAuthority && CurrentPhase != BobsledVehiclePhase.Released)
		{
			_networkPhaseResyncTimer += delta;
			if (!(_networkPhaseResyncTimer < 0.5))
			{
				_networkPhaseResyncTimer %= 0.5;
				MarkNetworkPhaseDirty();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["bobsledPhase"] = (int)CurrentPhase,
			["bobsledCurrentSpeed"] = CurrentSpeed,
			["bobsledHasIceFront"] = HasCapturedIceFront,
			["bobsledIceFrontX"] = CapturedIceFrontX,
			["bobsledIceBoostConsumed"] = _iceBoostConsumed,
			["bobsledReleaseCompleted"] = _releaseCompleted,
			["bobsledPhaseInitialized"] = _phaseInitialized,
			["bobsledNetworkRevision"] = _networkSpecialStateRevision
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		if (data != null)
		{
			_networkSpecialStateRevision = Mathf.Max(1, data.GetValueOrDefault("bobsledNetworkRevision", 1).AsInt32());
			ImportBobsledState(data);
		}
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		Dictionary dictionary = ExportVariantSave();
		dictionary["rev"] = _networkSpecialStateRevision;
		return dictionary;
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (TryAcceptNetworkPhaseState(data))
		{
			ImportBobsledState(data);
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary
		{
			["rev"] = _networkSpecialStateRevision,
			["bobsledPhase"] = (int)CurrentPhase
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (TryAcceptNetworkPhaseState(data))
		{
			BobsledVehiclePhase bobsledVehiclePhase = (BobsledVehiclePhase)Mathf.Clamp(data.GetValueOrDefault("bobsledPhase", (int)CurrentPhase).AsInt32(), 0, 4);
			bool num = CurrentPhase != bobsledVehiclePhase;
			CurrentPhase = bobsledVehiclePhase;
			_phaseInitialized = true;
			if (num)
			{
				ApplyPhasePresentation();
			}
		}
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return _networkSpecialStateRevision;
	}

	private bool TryAcceptNetworkPhaseState(Dictionary data)
	{
		if (data == null)
		{
			return false;
		}
		int num = Mathf.Max(1, data.GetValueOrDefault("rev", _networkSpecialStateRevision).AsInt32());
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			if (_hasImportedNetworkSpecialState && num <= _lastImportedNetworkSpecialStateRevision)
			{
				return false;
			}
			_hasImportedNetworkSpecialState = true;
			_lastImportedNetworkSpecialStateRevision = num;
		}
		_networkSpecialStateRevision = Mathf.Max(_networkSpecialStateRevision, num);
		return true;
	}

	private void ImportBobsledState(Dictionary data)
	{
		if (data != null)
		{
			CurrentPhase = (BobsledVehiclePhase)Mathf.Clamp(data.GetValueOrDefault("bobsledPhase", (int)CurrentPhase).AsInt32(), 0, 4);
			CurrentSpeed = Mathf.Max(0.0, data.GetValueOrDefault("bobsledCurrentSpeed", CurrentSpeed).AsDouble());
			HasCapturedIceFront = data.GetValueOrDefault("bobsledHasIceFront", HasCapturedIceFront).AsBool();
			CapturedIceFrontX = (float)data.GetValueOrDefault("bobsledIceFrontX", CapturedIceFrontX).AsDouble();
			_iceBoostConsumed = data.GetValueOrDefault("bobsledIceBoostConsumed", _iceBoostConsumed).AsBool();
			_releaseCompleted = data.GetValueOrDefault("bobsledReleaseCompleted", _releaseCompleted).AsBool();
			_phaseInitialized = data.GetValueOrDefault("bobsledPhaseInitialized", true).AsBool();
			ApplyPhasePresentation();
		}
	}
}
