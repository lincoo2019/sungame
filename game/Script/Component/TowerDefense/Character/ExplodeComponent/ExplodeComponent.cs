using System;
using Godot;
using Godot.Collections;

public sealed class ExplodeComponent : CharacterComponentRuntime
{
	public delegate void ExplodeEventHandler();

	public delegate void ExplodeStartEventHandler();

	private const string DeathExplodeOperationName = "death_explode";

	public AdobeAnimateSprite sprite;

	public string explodeAnimeClips = "Explode";

	public float explodeAnimeTimeScale = 1f;

	public bool hostAuthoritative = true;

	public bool skipGameRunning;

	public bool checkIZM = true;

	public string explodeMethod = "Range";

	public bool explodeUse = true;

	public bool reload = true;

	public bool explodeOnce = true;

	public string reverseAudio = "ReverseExplosion";

	public string explodeAudio = "ExplodeCherrybomb";

	public Array<TowerDefenseCharacterEventBase> explodeEvent = new Array<TowerDefenseCharacterEventBase>();

	public PackedScene explodeEffect;

	public AabbShape2DResource explodeShape;

	public bool scaleExplodeShapeToMapGrid = true;

	public Vector2 explodeRange = new Vector2(1.5f, 1.5f);

	public StringName explodeStateEvent = "ToExplode";

	public StringName idleStateEvent = "ToIdle";

	public string explodeJalaFireType = "Fire";

	public float explodeJalaNum = 1800f;

	public Array<int> explodeJalaOffset = new Array<int>();

	public bool cameraShakeUse = true;

	public Vector2 cameraShakeOffset = Vector2.One;

	public float cameraShakeForce = 5f;

	public float cameraShakeInterval = 0.05f;

	public int cameraShakeTime = 4;

	public bool screenColorBlinkUse;

	public Color screenColorBlinkColor = Colors.DarkSlateBlue;

	public float screenColorBlinkDuration = 0.5f;

	public bool screenColorBlinkRise;

	public bool craterCreateUse;

	public string craterCreatePacketName = "CraterDayGround";

	public TowerDefenseCharacter parent;

	public bool izmMode;

	public bool isHurt;

	private bool _restoreInvincibleOnExit;

	private bool _invincibleBeforeExplode;

	private bool _reapplyInvincibilityOnBind;

	private StateHandle _idleState;

	private StateHandle _explodeState;

	private bool _signalsConnected;

	private bool _hurtSignalsConnected;

	private string _pendingSyncedState;

	private NodePath _spritePath = new NodePath();

	private bool _configured;

	private bool _spriteSignalConnected;

	private long _nextDeathExplodeOperationSequence;

	private long _lastAppliedDeathExplodeOperationSequence = -1L;

	public bool ProtectsFromBites
	{
		get
		{
			if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && base.StateMachine?.CurrentStateHandle?.StableId == "explode.explode")
			{
				if (izmMode)
				{
					return isHurt;
				}
				return true;
			}
			return false;
		}
	}

	private bool CanDispatchStateProcessing
	{
		get
		{
			if (base.Alive && GodotObject.IsInstanceValid(parent))
			{
				return GodotObject.IsInstanceValid(sprite);
			}
			return false;
		}
	}

	private bool IsRemoteClient
	{
		get
		{
			if (hostAuthoritative && Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	private ExplodeComponentDefinition Definition => base.ComponentDefinition as ExplodeComponentDefinition;

	public event ExplodeEventHandler OnExplode;

	public event ExplodeStartEventHandler OnExplodeStart;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveOwnerReferences();
		ConnectOwnerSignals();
		ReapplyInvincibilityAfterRebind();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_reapplyInvincibilityOnBind = reason == ComponentDetachReason.TemporaryTreeExit && _restoreInvincibleOnExit;
		if (_reapplyInvincibilityOnBind)
		{
			RestoreInvincibility();
		}
		DisconnectOwnerSignals();
		sprite = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectOwnerSignals();
		RestoreInvincibility();
		this.OnExplode = null;
		this.OnExplodeStart = null;
		explodeEvent.Clear();
		explodeJalaOffset.Clear();
		explodeEffect = null;
		explodeShape = null;
		_reapplyInvincibilityOnBind = false;
		sprite = null;
		parent = null;
	}

	protected override void OnOwnerBeforeDestroy()
	{
		if (ShouldExplodeOnOwnerDeath())
		{
			SendNetworkOperation("death_explode", ++_nextDeathExplodeOperationSequence, new Dictionary());
			CommitDeathExplosion();
		}
	}

	private void ConfigureOnce()
	{
		if (_configured || Definition == null)
		{
			return;
		}
		ExplodeComponentDefinition definition = Definition;
		_spritePath = definition.spritePath;
		explodeAnimeClips = definition.explodeAnimeClips;
		explodeAnimeTimeScale = definition.explodeAnimeTimeScale;
		hostAuthoritative = definition.hostAuthoritative;
		skipGameRunning = definition.skipGameRunning;
		checkIZM = definition.checkIZM;
		explodeMethod = definition.explodeMethod;
		explodeUse = definition.explodeUse;
		reload = definition.reload;
		explodeOnce = definition.explodeOnce;
		reverseAudio = definition.reverseAudio;
		explodeAudio = definition.explodeAudio;
		explodeEffect = definition.explodeEffect;
		explodeShape = (GodotObject.IsInstanceValid(definition.explodeShape) ? (definition.explodeShape.Duplicate(deep: true) as AabbShape2DResource) : null);
		scaleExplodeShapeToMapGrid = definition.scaleExplodeShapeToMapGrid;
		explodeRange = definition.explodeRange;
		explodeStateEvent = definition.explodeStateEvent;
		idleStateEvent = definition.idleStateEvent;
		explodeJalaFireType = definition.explodeJalaFireType;
		explodeJalaNum = definition.explodeJalaNum;
		cameraShakeUse = definition.cameraShakeUse;
		cameraShakeOffset = definition.cameraShakeOffset;
		cameraShakeForce = definition.cameraShakeForce;
		cameraShakeInterval = definition.cameraShakeInterval;
		cameraShakeTime = definition.cameraShakeTime;
		screenColorBlinkUse = definition.screenColorBlinkUse;
		screenColorBlinkColor = definition.screenColorBlinkColor;
		screenColorBlinkDuration = definition.screenColorBlinkDuration;
		screenColorBlinkRise = definition.screenColorBlinkRise;
		craterCreateUse = definition.craterCreateUse;
		craterCreatePacketName = definition.craterCreatePacketName;
		explodeEvent.Clear();
		if (definition.explodeEvent != null)
		{
			for (int i = 0; i < definition.explodeEvent.Count; i++)
			{
				if (definition.explodeEvent[i] != null)
				{
					explodeEvent.Add((TowerDefenseCharacterEventBase)definition.explodeEvent[i].Duplicate(deep: true));
				}
				else
				{
					explodeEvent.Add(null);
				}
			}
		}
		explodeJalaOffset.Clear();
		if (definition.explodeJalaOffset != null)
		{
			for (int j = 0; j < definition.explodeJalaOffset.Count; j++)
			{
				explodeJalaOffset.Add(definition.explodeJalaOffset[j]);
			}
		}
		_configured = true;
	}

	private void ResolveOwnerReferences()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			sprite = (_spritePath.IsEmpty ? null : parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath));
			if (checkIZM && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && TowerDefenseManager.Instance.IsIZMMode())
			{
				izmMode = true;
			}
		}
	}

	private void ConnectOwnerSignals()
	{
		if (!_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			_spriteSignalConnected = true;
		}
		if (!_hurtSignalsConnected && izmMode && GodotObject.IsInstanceValid(parent))
		{
			parent.OnBodyHurt += IZMHurt;
			parent.OnArmorHurt += IZMHurt;
			_hurtSignalsConnected = true;
		}
	}

	private void DisconnectOwnerSignals()
	{
		if (_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		_spriteSignalConnected = false;
		if (_hurtSignalsConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnBodyHurt -= IZMHurt;
			parent.OnArmorHurt -= IZMHurt;
		}
		_hurtSignalsConnected = false;
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("explode.idle");
		_explodeState = base.StateMachine?.GetStateById("explode.explode");
		if (GodotObject.IsInstanceValid(parent))
		{
			ConnectStateSignals();
			Callable.From(ApplyPendingSyncedState).CallDeferred();
		}
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		RestoreInvincibility();
		_idleState = null;
		_explodeState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		if (base.StateMachine?.CurrentStateHandle?.StableId != "explode.explode")
		{
			RestoreInvincibility();
		}
		else if (GodotObject.IsInstanceValid(parent?.instance))
		{
			if (izmMode && !isHurt)
			{
				RestoreInvincibility();
			}
			else
			{
				EnableExplodeInvincibility();
			}
			RestoreAuthoritativeAnimation(remote);
		}
	}

	private void RestoreAuthoritativeAnimation(bool remote)
	{
		if (remote && HasPlayableExplodeAnimation() && !(sprite.clip == explodeAnimeClips))
		{
			sprite.SetAnimation(explodeAnimeClips, loop: false);
		}
	}

	private void ConnectStateSignals()
	{
		if (!_signalsConnected)
		{
			StateHandle idleState = _idleState;
			if (idleState != null && idleState.IsValid)
			{
				_idleState.Entered += IdleEntered;
				_idleState.Exited += IdleExited;
				_idleState.PhysicsProcessing += IdleProcessing;
			}
			StateHandle explodeState = _explodeState;
			if (explodeState != null && explodeState.IsValid)
			{
				_explodeState.Entered += ExplodeEntered;
				_explodeState.Exited += ExplodeExited;
				_explodeState.PhysicsProcessing += ExplodeProcessing;
			}
			_signalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_signalsConnected)
		{
			if (_idleState != null)
			{
				_idleState.Entered -= IdleEntered;
				_idleState.Exited -= IdleExited;
				_idleState.PhysicsProcessing -= IdleProcessing;
			}
			if (_explodeState != null)
			{
				_explodeState.Entered -= ExplodeEntered;
				_explodeState.Exited -= ExplodeExited;
				_explodeState.PhysicsProcessing -= ExplodeProcessing;
			}
			_signalsConnected = false;
		}
	}

	public void Reload()
	{
		reload = true;
	}

	public void Explode()
	{
		if (!base.Alive || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (screenColorBlinkUse)
		{
			ViewManager.Instance.FullScreenColorBlink(screenColorBlinkColor, screenColorBlinkDuration, screenColorBlinkRise);
		}
		if (cameraShakeUse)
		{
			ViewManager.Instance.CameraShake(new Vector2((float)GD.RandRange(0f - cameraShakeOffset.X, cameraShakeOffset.X), (float)GD.RandRange(0f - cameraShakeOffset.Y, cameraShakeOffset.Y)), cameraShakeForce, cameraShakeInterval, cameraShakeTime);
		}
		CreateParticlesEffect();
		if (explodeAudio != "")
		{
			AudioManager.Instance.AudioPlay(explodeAudio);
		}
		if (IsRemoteClient)
		{
			return;
		}
		this.OnExplodeStart?.Invoke();
		switch (explodeMethod)
		{
		case "Range":
			TowerDefenseExplode.CreateExplode(parent.GetLogicalGlobalPosition(), GetRuntimeExplodeRange(), explodeEvent, new Array<TowerDefenseCharacter>(), parent.camp, -1, parent is TowerDefenseZombie);
			break;
		case "Line":
			foreach (int item in explodeJalaOffset)
			{
				if (parent.gridPos.Y + item >= 1 && parent.gridPos.Y + item <= TowerDefenseManager.Instance.GetMapGridNum().Y)
				{
					TowerDefenseCharacter.CreateJalapenoFire(parent.camp, parent.gridPos + new Vector2I(0, item), explodeJalaNum, explodeEvent, new Array<TowerDefenseCharacterEventBase>(), explodeJalaFireType, parent is TowerDefenseZombie);
				}
			}
			break;
		case "Row":
			foreach (int item2 in explodeJalaOffset)
			{
				if (parent.gridPos.Y + item2 >= 1 && parent.gridPos.Y + item2 <= TowerDefenseManager.Instance.GetMapGridNum().Y)
				{
					TowerDefenseCharacter.CreateJalapenoFireColumn(parent.camp, parent.gridPos + new Vector2I(0, item2), explodeJalaNum, explodeEvent, new Array<TowerDefenseCharacterEventBase>(), explodeJalaFireType, parent is TowerDefenseZombie);
				}
			}
			break;
		case "Cross":
			foreach (int item3 in explodeJalaOffset)
			{
				if (parent.gridPos.Y + item3 >= 1 && parent.gridPos.Y + item3 <= TowerDefenseManager.Instance.GetMapGridNum().Y)
				{
					TowerDefenseCharacter.CreateJalapenoFireColumn(parent.camp, parent.gridPos + new Vector2I(0, item3), explodeJalaNum, explodeEvent, new Array<TowerDefenseCharacterEventBase>(), explodeJalaFireType, parent is TowerDefenseZombie);
					TowerDefenseCharacter.CreateJalapenoFire(parent.camp, parent.gridPos + new Vector2I(0, item3), explodeJalaNum, explodeEvent, new Array<TowerDefenseCharacterEventBase>(), explodeJalaFireType, parent is TowerDefenseZombie);
				}
			}
			break;
		case "Slash":
			foreach (int item4 in explodeJalaOffset)
			{
				if (parent.gridPos.Y + item4 >= 1 && parent.gridPos.Y + item4 <= TowerDefenseManager.Instance.GetMapGridNum().Y)
				{
					TowerDefenseCharacter.CreateJalapenoFireSlash(parent.camp, parent.gridPos + new Vector2I(0, item4), explodeJalaNum, explodeEvent, new Array<TowerDefenseCharacterEventBase>(), explodeJalaFireType, parent is TowerDefenseZombie);
				}
			}
			break;
		}
		if (craterCreateUse)
		{
			TowerDefenseCellInstance towerDefenseCellInstance = parent.cell;
			bool flag = false;
			if (!GodotObject.IsInstanceValid(towerDefenseCellInstance))
			{
				towerDefenseCellInstance = TowerDefenseManager.GetMapCell(parent.gridPos);
				if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
				{
					parent.cell = towerDefenseCellInstance;
					flag = true;
				}
			}
			if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
			{
				towerDefenseCellInstance.Clear(parent is TowerDefenseZombie);
				parent.CraterCreate(nolimit: true, craterCreatePacketName);
			}
			if (flag && GodotObject.IsInstanceValid(parent) && parent.cell == towerDefenseCellInstance)
			{
				parent.cell = null;
			}
		}
		InvokeExplodeCallbacks();
	}

	private void InvokeExplodeCallbacks()
	{
		using (FallingObjectReplicationPolicy.BeginCapture(parent))
		{
			this.OnExplode?.Invoke();
		}
	}

	private Vector2 GetRuntimeExplodeRange()
	{
		Vector2 result = new Vector2(Mathf.Abs(explodeRange.X), Mathf.Abs(explodeRange.Y));
		if (scaleExplodeShapeToMapGrid || !GodotObject.IsInstanceValid(explodeShape) || !explodeShape.Enabled || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !explodeShape.TryGetWorldRect(parent.GetLogicalGlobalTransform(parent), out var rect))
		{
			return result;
		}
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		if (mapGridSize.X <= 0f || mapGridSize.Y <= 0f)
		{
			return result;
		}
		return new Vector2(Mathf.Abs(rect.Size.X) / (2f * mapGridSize.X), Mathf.Abs(rect.Size.Y) / (2f * mapGridSize.Y));
	}

	public TowerDefenseEffectParticlesOnce CreateParticlesEffect()
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(explodeEffect))
		{
			return null;
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(explodeEffect, parent.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = parent.GetLogicalGlobalPosition(parent.transformPoint) - new Vector2(0f, 30f);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
		return towerDefenseEffectParticlesOnce;
	}

	public void IZMHurt(int num)
	{
		if (base.Alive)
		{
			isHurt = true;
			if (!(base.StateMachine?.CurrentStateHandle?.StableId != "explode.explode"))
			{
				EnableExplodeInvincibility();
			}
		}
	}

	public void IdleEntered()
	{
		if (parent.componentRunning && parent is TowerDefensePlant towerDefensePlant)
		{
			towerDefensePlant.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (CanDispatchStateProcessing && !IsRemoteClient && (TowerDefenseManager.Instance.IsGameRunning() || skipGameRunning) && parent.inGame && parent.componentAlive && (!parent.componentRunning || (parent is TowerDefensePlant && parent.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)) && reload && HasPlayableExplodeAnimation())
		{
			SendStateEvent(explodeStateEvent);
		}
	}

	public void IdleExited()
	{
	}

	public void ExplodeEntered()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			if (parent is TowerDefensePlant)
			{
				parent.Component();
			}
			if (!izmMode || isHurt)
			{
				EnableExplodeInvincibility();
			}
			if (explodeUse && reverseAudio != "")
			{
				AudioManager.Instance.AudioPlay(reverseAudio);
			}
			if (!HasPlayableExplodeAnimation())
			{
				CompleteExplode();
			}
			else
			{
				sprite.SetAnimation(explodeAnimeClips, loop: false, 0.2);
			}
		}
	}

	public void ExplodeProcessing(double delta)
	{
		if (!CanDispatchStateProcessing)
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(parent))
		{
			SendStateEvent(idleStateEvent);
		}
		else if (!parent.componentAlive)
		{
			SendStateEvent(idleStateEvent);
		}
		else
		{
			if (!HasPlayableExplodeAnimation())
			{
				return;
			}
			bool flag = TowerDefenseManager.Instance != null && TowerDefenseManager.Instance.IsIZMMode();
			if (!checkIZM)
			{
				if (flag)
				{
					sprite.timeScale = explodeAnimeTimeScale;
				}
				else
				{
					sprite.timeScale = parent.timeScale * (double)explodeAnimeTimeScale;
				}
			}
			else if (izmMode)
			{
				if (isHurt)
				{
					sprite.timeScale = explodeAnimeTimeScale;
				}
				else
				{
					sprite.timeScale = 0.0;
				}
			}
			else
			{
				sprite.timeScale = parent.timeScale * (double)explodeAnimeTimeScale;
			}
		}
	}

	public void ExplodeExited()
	{
		RestoreInvincibility();
	}

	private void RestoreInvincibility()
	{
		if (_restoreInvincibleOnExit && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance))
		{
			parent.instance.invincible = _invincibleBeforeExplode;
		}
		_restoreInvincibleOnExit = false;
	}

	private void EnableExplodeInvincibility()
	{
		if (!(parent is TowerDefensePlant) && !_restoreInvincibleOnExit && GodotObject.IsInstanceValid(parent?.instance))
		{
			_invincibleBeforeExplode = parent.instance.invincible;
			_restoreInvincibleOnExit = true;
			parent.instance.invincible = true;
		}
	}

	private void ReapplyInvincibilityAfterRebind()
	{
		if (_reapplyInvincibilityOnBind && GodotObject.IsInstanceValid(parent?.instance))
		{
			_reapplyInvincibilityOnBind = false;
			if (!izmMode || isHurt)
			{
				EnableExplodeInvincibility();
			}
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (GodotObject.IsInstanceValid(parent) && !parent.die && !parent.nearDie && clip == explodeAnimeClips)
		{
			CompleteExplode();
		}
	}

	private bool HasPlayableExplodeAnimation()
	{
		if (!GodotObject.IsInstanceValid(sprite) || sprite.flashAnimeData == null || !GodotObject.IsInstanceValid(sprite.flashAnimeData))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(explodeAnimeClips))
		{
			return sprite.HasClip(explodeAnimeClips);
		}
		return false;
	}

	private void CompleteExplode()
	{
		if (!reload)
		{
			return;
		}
		reload = false;
		if (IsRemoteClient)
		{
			if (explodeUse)
			{
				Explode();
			}
			if (!explodeOnce)
			{
				FinishReusableExplode();
			}
		}
		else
		{
			if (explodeUse)
			{
				Explode();
			}
			else
			{
				InvokeExplodeCallbacks();
			}
			if (explodeOnce)
			{
				parent.Destroy();
			}
			else
			{
				FinishReusableExplode();
			}
		}
	}

	private bool ShouldExplodeOnOwnerDeath()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && reload && parent is TowerDefensePlant && GodotObject.IsInstanceValid(parent.instance) && !parent.instance.sleep && !parent.isShovel && !parent.suppressDeathrattles && !parent.skipDestroySet)
		{
			if (!parent.die && !parent.instance.die)
			{
				return parent.instance.hitpoints <= 0.0;
			}
			return true;
		}
		return false;
	}

	private void CommitDeathExplosion()
	{
		reload = false;
		if (explodeUse)
		{
			Explode();
		}
		else
		{
			InvokeExplodeCallbacks();
		}
	}

	private void FinishReusableExplode()
	{
		if (parent is TowerDefensePlant)
		{
			parent.Idle();
		}
		SendStateEvent(idleStateEvent);
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "izmMode", izmMode },
			{ "isHurt", isHurt },
			{ "reload", reload }
		};
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		izmMode = _data.GetValueOrDefault("izmMode", false).AsBool();
		isHurt = _data.GetValueOrDefault("isHurt", false).AsBool();
		reload = _data.GetValueOrDefault("reload", true).AsBool();
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = ExportComponentSave();
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		izmMode = data.GetValueOrDefault("izmMode", izmMode).AsBool();
		isHurt = data.GetValueOrDefault("isHurt", isHurt).AsBool();
		reload = data.GetValueOrDefault("reload", reload).AsBool();
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	public override void ApplyNetworkOperation(string operationName, long sequence, Dictionary data)
	{
		if (string.Equals(operationName, "death_explode", StringComparison.Ordinal) && sequence > _lastAppliedDeathExplodeOperationSequence && IsRemoteClient && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			_lastAppliedDeathExplodeOperationSequence = sequence;
			reload = false;
			if (explodeUse)
			{
				Explode();
			}
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.Lifecycle == ComponentRuntimeLifecycle.Active && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingSyncedState))
			{
				_pendingSyncedState = null;
			}
		}
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}

	public override void SetAlive(bool value)
	{
		base.SetAlive(value);
		if (!value)
		{
			RestoreInvincibility();
			SendStateEvent(idleStateEvent, allowWhenInactive: true);
		}
		else
		{
			ApplyPendingSyncedState();
		}
	}
}
