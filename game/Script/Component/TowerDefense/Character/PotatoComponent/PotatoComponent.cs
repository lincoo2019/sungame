using System;
using Godot;
using Godot.Collections;

public sealed class PotatoComponent : CharacterComponentRuntime
{
	public delegate void ReadyRiseEventHandler();

	public float readyTime = 15f;

	public bool autoRise = true;

	public bool componentStateUse = true;

	public StringName readyTimerName = "Ready";

	public PackedScene riseEffect;

	public string riseAudio = "GravestoneRumble";

	public bool createRiseEffect = true;

	public bool setSmashInvincibleOnCharge = true;

	public bool destroyAfterExplode = true;

	public bool autoExplodeOnCharge = true;

	public StringName readyStateEvent = "ToReady";

	public StringName riseStateEvent = "ToRise";

	public StringName chargeStateEvent = "ToCharge";

	public StringName idleStateEvent = "ToIdle";

	public string readyAnimeClips = "Ready";

	public float readyAnimeTimeScale = 1f;

	public string riseAnimeClips = "Rise";

	public float riseAnimeTimeScale = 1f;

	public string chargeAnimeClips = "Idle";

	public float chargeAnimeTimeScale = 1f;

	public TowerDefenseCharacter parent;

	public bool rise;

	public bool isCharge;

	public bool over;

	public Func<bool> smashExplodeHandler;

	private StringName _attackComponentName = "AttackComponent";

	private StringName _explodeComponentName = "ExplodeComponent";

	private NodePath _spritePath = new NodePath();

	private StateHandle _idleState;

	private StateHandle _readyState;

	private StateHandle _riseState;

	private StateHandle _chargeState;

	private bool _stateSignalsConnected;

	private bool _spriteSignalConnected;

	private bool _riseEffectPlayed;

	private bool _smashInvincibleCaptured;

	private bool _smashInvincibleBeforeCharge;

	private string _pendingSyncedState;

	private bool _readyTimerRunning;

	private double _readyTimerRemaining;

	private bool _configured;

	public AttackComponent attackComponent { get; private set; }

	public ExplodeComponent explodeComponent { get; private set; }

	public AdobeAnimateSprite sprite { get; private set; }

	public bool ProtectsFromBites
	{
		get
		{
			if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && componentStateUse && rise)
			{
				return isCharge;
			}
			return false;
		}
	}

	private PotatoComponentDefinition Definition => base.ComponentDefinition as PotatoComponentDefinition;

	internal override bool WantsPhysicsProcess => _readyTimerRunning;

	public event ReadyRiseEventHandler OnReadyRise;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveDependencies();
		ConnectSignals();
		ExplodeComponent obj = explodeComponent;
		if (obj != null && !obj.IsReleased)
		{
			explodeComponent.reload = false;
		}
	}

	protected override void OnActivated()
	{
		ResolveDependencies();
		ExplodeComponent obj = explodeComponent;
		if (obj != null && !obj.IsReleased)
		{
			explodeComponent.reload = false;
		}
		ConnectSignals();
		ApplyPendingSyncedState();
		RefreshPhysicsProcessEligibility();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSignals();
		parent = null;
		attackComponent = null;
		explodeComponent = null;
		sprite = null;
	}

	protected override void OnReleased()
	{
		this.OnReadyRise = null;
		smashExplodeHandler = null;
		parent = null;
		attackComponent = null;
		explodeComponent = null;
		sprite = null;
		riseEffect = null;
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			PotatoComponentDefinition definition = Definition;
			readyTime = Mathf.Max(0f, definition?.readyTime ?? 15f);
			readyTimerName = definition?.readyTimerName ?? new StringName("Ready");
			autoRise = definition?.autoRise ?? true;
			componentStateUse = definition?.componentStateUse ?? true;
			riseEffect = definition?.riseEffect;
			riseAudio = definition?.riseAudio ?? "GravestoneRumble";
			createRiseEffect = definition?.createRiseEffect ?? true;
			setSmashInvincibleOnCharge = definition?.setSmashInvincibleOnCharge ?? true;
			destroyAfterExplode = definition?.destroyAfterExplode ?? true;
			autoExplodeOnCharge = definition?.autoExplodeOnCharge ?? true;
			readyStateEvent = definition?.readyStateEvent ?? new StringName("ToReady");
			riseStateEvent = definition?.riseStateEvent ?? new StringName("ToRise");
			chargeStateEvent = definition?.chargeStateEvent ?? new StringName("ToCharge");
			idleStateEvent = definition?.idleStateEvent ?? new StringName("ToIdle");
			readyAnimeClips = definition?.readyAnimeClips ?? "Ready";
			readyAnimeTimeScale = definition?.readyAnimeTimeScale ?? 1f;
			riseAnimeClips = definition?.riseAnimeClips ?? "Rise";
			riseAnimeTimeScale = definition?.riseAnimeTimeScale ?? 1f;
			chargeAnimeClips = definition?.chargeAnimeClips ?? "Idle";
			chargeAnimeTimeScale = definition?.chargeAnimeTimeScale ?? 1f;
			_attackComponentName = definition?.attackComponentName ?? new StringName("AttackComponent");
			_explodeComponentName = definition?.explodeComponentName ?? new StringName("ExplodeComponent");
			_spritePath = definition?.spritePath ?? new NodePath();
			_configured = true;
		}
	}

	private void ResolveDependencies()
	{
		if (base.Manager != null && GodotObject.IsInstanceValid(parent))
		{
			string text = _attackComponentName.ToString();
			attackComponent = ((!string.IsNullOrEmpty(text) && base.Manager.TryGetRuntimeByLegacyNodeName(text, out var runtime)) ? (runtime as AttackComponent) : null);
			explodeComponent = base.Manager.GetRuntime<ExplodeComponent>();
			sprite = ((!_spritePath.IsEmpty) ? parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath) : parent.sprite);
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("potato.idle");
		_readyState = base.StateMachine?.GetStateById("potato.ready");
		_riseState = base.StateMachine?.GetStateById("potato.rise");
		_chargeState = base.StateMachine?.GetStateById("potato.charge");
		ConnectStateSignals();
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeDetaching()
	{
		CaptureCurrentStateForReattach();
		DisconnectStateSignals();
		StopReadyTimer();
		RestoreSmashInvincibility();
		_idleState = null;
		_readyState = null;
		_riseState = null;
		_chargeState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		string text = base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
		bool flag = text == "potato.idle";
		bool num = text == "potato.ready";
		bool flag2 = text == "potato.rise";
		bool flag3 = (isCharge = text == "potato.charge");
		if (!num)
		{
			StopReadyTimer();
		}
		if (flag2 || flag3)
		{
			_riseEffectPlayed = true;
		}
		else if (flag)
		{
			_riseEffectPlayed = false;
		}
		if (!flag3)
		{
			RestoreSmashInvincibility();
		}
		else
		{
			RefreshChargeSmashInvincibility();
		}
		RestoreAuthoritativeAnimation(text, remote);
	}

	private void RestoreAuthoritativeAnimation(string stateId, bool remote)
	{
		if (remote && GodotObject.IsInstanceValid(sprite))
		{
			string text = stateId switch
			{
				"potato.ready" => readyAnimeClips, 
				"potato.rise" => riseAnimeClips, 
				"potato.charge" => chargeAnimeClips, 
				_ => string.Empty, 
			};
			if (!string.IsNullOrEmpty(text) && !(sprite.clip == text) && sprite.HasClip(text))
			{
				bool loop = stateId != "potato.rise";
				sprite.SetAnimation(text, loop);
			}
		}
	}

	private void ConnectSignals()
	{
		if (!_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			_spriteSignalConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		_spriteSignalConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, null, IdleExited, IdleProcessing);
			ConnectState(_readyState, ReadyEntered, ReadyExited, ReadyProcessing);
			ConnectState(_riseState, RiseEntered, RiseExited, RiseProcessing);
			ConnectState(_chargeState, ChargeEntered, ChargeExited, ChargeProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, null, IdleExited, IdleProcessing);
			DisconnectState(_readyState, ReadyEntered, ReadyExited, ReadyProcessing);
			DisconnectState(_riseState, RiseEntered, RiseExited, RiseProcessing);
			DisconnectState(_chargeState, ChargeEntered, ChargeExited, ChargeProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null && state.IsValid)
		{
			if (entered != null)
			{
				state.Entered += entered;
			}
			if (exited != null)
			{
				state.Exited += exited;
			}
			if (processing != null)
			{
				state.PhysicsProcessing += processing;
			}
		}
	}

	private static void DisconnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null)
		{
			if (entered != null)
			{
				state.Entered -= entered;
			}
			if (exited != null)
			{
				state.Exited -= exited;
			}
			if (processing != null)
			{
				state.PhysicsProcessing -= processing;
			}
		}
	}

	public void IdleEntered()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.componentRunning)
		{
			parent.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (IsRemoteSyncedClient() || !TryGetStateRuntime(out var character))
		{
			return;
		}
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			if (!(character is TowerDefenseZombie) && autoRise)
			{
				if (componentStateUse)
				{
					character.Component();
				}
				rise = true;
				SendStateEvent(chargeStateEvent);
				this.OnReadyRise?.Invoke();
			}
			else
			{
				SendStateEvent(readyStateEvent);
			}
		}
		else
		{
			if (!TowerDefenseManager.Instance.IsGameRunning() || !character.inGame || !character.componentAlive || character.componentRunning)
			{
				return;
			}
			if (rise)
			{
				SendStateEvent(riseStateEvent);
				return;
			}
			if (!(character is TowerDefenseZombie) && componentStateUse)
			{
				character.Component();
			}
			SendStateEvent(readyStateEvent);
		}
	}

	public void IdleExited()
	{
	}

	public void ReadyEntered()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(readyAnimeClips);
		}
		if (autoRise && !IsRemoteSyncedClient())
		{
			StartReadyTimer(Math.Max(0f, readyTime));
		}
	}

	public void ReadyProcessing(double delta)
	{
		ApplyAnimationTimeScale(readyAnimeTimeScale);
	}

	public void ReadyExited()
	{
		StopReadyTimer();
	}

	public void RiseEntered()
	{
		if (!string.IsNullOrEmpty(riseAudio))
		{
			AudioManager.Instance?.AudioPlay(riseAudio);
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(riseAnimeClips, loop: false);
		}
		CreateConfiguredRiseEffect();
	}

	public void RiseProcessing(double delta)
	{
		ApplyAnimationTimeScale(riseAnimeTimeScale);
	}

	public void RiseExited()
	{
	}

	public void ChargeEntered()
	{
		isCharge = true;
		RefreshChargeSmashInvincibility();
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(chargeAnimeClips);
		}
		ChargeProcessing(0.0);
	}

	public void ChargeProcessing(double delta)
	{
		ApplyAnimationTimeScale(chargeAnimeTimeScale);
		if (IsRemoteSyncedClient() || !autoExplodeOnCharge || over)
		{
			return;
		}
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased && attackComponent.CanAttackOnContact())
		{
			over = true;
			Explode();
			if (destroyAfterExplode && GodotObject.IsInstanceValid(parent))
			{
				parent.Destroy();
			}
		}
	}

	public void ChargeExited()
	{
		isCharge = false;
		RestoreSmashInvincibility();
	}

	public void ReadyRise()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !IsRemoteSyncedClient() && !rise)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				rise = true;
				SendStateEvent(riseStateEvent);
				StopReadyTimer();
				this.OnReadyRise?.Invoke();
			}
		}
	}

	public void ReadyCharge()
	{
		if (!base.IsReleased && !IsRemoteSyncedClient())
		{
			rise = true;
			StopReadyTimer();
			if (parent is TowerDefensePlant && componentStateUse)
			{
				parent.Component();
			}
			_pendingSyncedState = "potato.charge";
			ApplyPendingSyncedState();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && clip == riseAnimeClips)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SendStateEvent(chargeStateEvent);
			}
		}
	}

	public void Explode()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			ExplodeComponent obj = explodeComponent;
			if (obj != null && !obj.IsReleased)
			{
				explodeComponent.Explode();
			}
		}
	}

	public bool CanExplodeOnSmash()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !IsRemoteSyncedClient() && rise && isCharge && !over)
		{
			ExplodeComponent obj = explodeComponent;
			if (obj != null && !obj.IsReleased)
			{
				return GodotObject.IsInstanceValid(parent);
			}
		}
		return false;
	}

	public bool TryExplodeOnSmash()
	{
		if (!CanExplodeOnSmash())
		{
			return false;
		}
		Func<bool> func = smashExplodeHandler;
		if (func != null && func())
		{
			return true;
		}
		over = true;
		Explode();
		if (destroyAfterExplode && GodotObject.IsInstanceValid(parent))
		{
			parent.Destroy();
		}
		return true;
	}

	private void Timeout(string timerName)
	{
		if (!IsRemoteSyncedClient() && timerName == GetReadyTimerName())
		{
			ReadyRise();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (_readyTimerRunning)
		{
			double num = (GodotObject.IsInstanceValid(parent) ? Math.Max(0.0, parent.timeScale) : 1.0);
			_readyTimerRemaining = Math.Max(0.0, _readyTimerRemaining - delta * num);
			if (!(_readyTimerRemaining > 0.0))
			{
				string timerName = GetReadyTimerName();
				StopReadyTimer();
				Timeout(timerName);
			}
		}
	}

	private void StartReadyTimer(double duration)
	{
		_readyTimerRemaining = Math.Max(0.0, duration);
		_readyTimerRunning = true;
		RefreshPhysicsProcessEligibility();
	}

	private void StopReadyTimer()
	{
		if (_readyTimerRunning || !(_readyTimerRemaining <= 0.0))
		{
			_readyTimerRunning = false;
			_readyTimerRemaining = 0.0;
			RefreshPhysicsProcessEligibility();
		}
	}

	private void CreateConfiguredRiseEffect()
	{
		if (!_riseEffectPlayed && createRiseEffect && GodotObject.IsInstanceValid(riseEffect) && GodotObject.IsInstanceValid(parent))
		{
			Node2D node2D = TowerDefenseManager.CreateEffectParticlesOnce(riseEffect, parent.gridPos);
			Node2D characterNode = TowerDefenseGroundItemBase.characterNode;
			if (!GodotObject.IsInstanceValid(node2D) || !GodotObject.IsInstanceValid(characterNode))
			{
				node2D?.QueueFree();
				return;
			}
			characterNode.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
			node2D.GlobalPosition = parent.GetLogicalGlobalPosition();
			_riseEffectPlayed = true;
		}
	}

	private void ApplyAnimationTimeScale(float configuredScale)
	{
		if (GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(parent))
		{
			sprite.timeScale = parent.timeScale * (double)configuredScale;
		}
	}

	private string GetReadyTimerName()
	{
		if (!readyTimerName.IsEmpty)
		{
			return readyTimerName.ToString();
		}
		return "Ready";
	}

	private bool CanUseChargeSmashInvincibility()
	{
		if (setSmashInvincibleOnCharge && rise)
		{
			return HasSmashExplodePath();
		}
		return false;
	}

	private bool HasSmashExplodePath()
	{
		ExplodeComponent obj = explodeComponent;
		if (obj == null || obj.IsReleased)
		{
			return smashExplodeHandler != null;
		}
		return true;
	}

	private void RefreshChargeSmashInvincibility()
	{
		if (!CanUseChargeSmashInvincibility())
		{
			RestoreSmashInvincibility();
		}
		else if (GodotObject.IsInstanceValid(parent?.instance))
		{
			if (!_smashInvincibleCaptured)
			{
				_smashInvincibleBeforeCharge = parent.instance.invincibleSmash;
				_smashInvincibleCaptured = true;
			}
			parent.instance.invincibleSmash = true;
		}
	}

	private void RestoreSmashInvincibility()
	{
		if (!GodotObject.IsInstanceValid(parent?.instance))
		{
			_smashInvincibleCaptured = false;
			return;
		}
		if (_smashInvincibleCaptured)
		{
			parent.instance.invincibleSmash = _smashInvincibleBeforeCharge;
		}
		else if (setSmashInvincibleOnCharge)
		{
			parent.instance.invincibleSmash = false;
		}
		_smashInvincibleCaptured = false;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			StopReadyTimer();
			RestoreSmashInvincibility();
			SendStateEvent(idleStateEvent, allowWhenInactive: true);
		}
		else
		{
			ApplyPendingSyncedState();
		}
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			["rise"] = rise,
			["isCharge"] = isCharge,
			["over"] = over,
			["timerState"] = ExportReadyTimerState()
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		rise = data.GetValueOrDefault("rise", rise).AsBool();
		isCharge = data.GetValueOrDefault("isCharge", isCharge).AsBool();
		over = data.GetValueOrDefault("over", over).AsBool();
		if (data.ContainsKey("timerState"))
		{
			ImportReadyTimerState(data["timerState"].AsGodotDictionary());
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = ExportComponentSave();
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null)
		{
			rise = data.GetValueOrDefault("rise", rise).AsBool();
			isCharge = data.GetValueOrDefault("isCharge", isCharge).AsBool();
			over = data.GetValueOrDefault("over", over).AsBool();
			if (data.ContainsKey("timerState"))
			{
				ImportReadyTimerState(data["timerState"].AsGodotDictionary());
			}
			if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
			{
				_pendingSyncedState = data["state"].AsString();
				ApplyPendingSyncedState();
			}
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && SyncForceState(base.StateMachine, _pendingSyncedState))
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

	private Dictionary ExportReadyTimerState()
	{
		string text = GetReadyTimerName();
		Dictionary dictionary = new Dictionary { [text] = _readyTimerRunning };
		Dictionary dictionary2 = new Dictionary { [text] = _readyTimerRemaining };
		Dictionary dictionary3 = new Dictionary { [text] = 0.0 };
		return new Dictionary
		{
			["timerRunning"] = dictionary,
			["timerWaitTime"] = dictionary2,
			["timerCurrent"] = dictionary3,
			["timeScale"] = (GodotObject.IsInstanceValid(parent) ? parent.timeScale : 1.0)
		};
	}

	private void ImportReadyTimerState(Dictionary timerState)
	{
		string text = GetReadyTimerName();
		Dictionary dictionary = timerState.GetValueOrDefault("timerRunning", new Dictionary()).AsGodotDictionary();
		Dictionary dictionary2 = timerState.GetValueOrDefault("timerWaitTime", new Dictionary()).AsGodotDictionary();
		Dictionary dictionary3 = timerState.GetValueOrDefault("timerCurrent", new Dictionary()).AsGodotDictionary();
		bool num = dictionary.GetValueOrDefault(text, false).AsBool();
		double num2 = dictionary2.GetValueOrDefault(text, 0.0).AsDouble();
		double num3 = dictionary3.GetValueOrDefault(text, 0.0).AsDouble();
		double duration = Math.Max(0.0, num2 - num3);
		if (num)
		{
			StartReadyTimer(duration);
		}
		else
		{
			StopReadyTimer();
		}
	}

	private void CaptureCurrentStateForReattach()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (string.IsNullOrEmpty(_pendingSyncedState) && stateHandle != null && stateHandle.IsValid)
		{
			_pendingSyncedState = stateHandle.StableId;
		}
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId.ToString();
	}

	private bool IsRemoteSyncedClient()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	private bool TryGetStateRuntime(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				return GodotObject.IsInstanceValid(TowerDefenseManager.Instance);
			}
		}
		return false;
	}
}
