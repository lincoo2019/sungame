using Godot;
using Godot.Collections;

public sealed class ImpFlightComponent : CharacterComponentRuntime
{
	public string flyAudio = "Imp";

	public float flyAnimationSpeedScale = 0.8f;

	public double animationBlend = 0.2;

	public StringName flyEvent = "ToFly";

	public StringName landEvent = "ToLand";

	public StringName idleEvent = "ToIdle";

	public StringName flyStateName = "Fly";

	public bool suppressCollisionInFlight = true;

	public bool restoreCollisionOnDisable = true;

	public TowerDefenseZombieImpBase parent;

	private bool _flightCollisionSuppressed;

	private StateHandle _flyState;

	private StateHandle _landState;

	private bool _stateSignalsConnected;

	private bool _landSignalConnected;

	private bool _parentReadyConnected;

	private Dictionary _pendingSyncData;

	private bool _configured;

	protected override bool AllowStateMachineOutsideComponentBattlefield => true;

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	private ImpFlightComponentDefinition Definition => base.ComponentDefinition as ImpFlightComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseZombieImpBase;
		ApplyDefinition();
		ConnectParentSignals();
	}

	protected override void OnActivated()
	{
		ConnectParentSignals();
		if (base.StateMachine?.CurrentStateHandle?.StableId == "imp_flight.fly" && TryGetParent(out var imp))
		{
			SuppressCollision(imp);
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectParentSignals();
		RestoreSuppressedCollision();
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectParentSignals();
		RestoreSuppressedCollision();
		_pendingSyncData = null;
		parent = null;
	}

	private void ApplyDefinition()
	{
		if (!_configured && Definition != null)
		{
			flyAudio = Definition.flyAudio;
			flyAnimationSpeedScale = Definition.flyAnimationSpeedScale;
			animationBlend = Definition.animationBlend;
			flyEvent = Definition.flyEvent;
			landEvent = Definition.landEvent;
			idleEvent = Definition.idleEvent;
			flyStateName = Definition.flyStateName;
			suppressCollisionInFlight = Definition.suppressCollisionInFlight;
			restoreCollisionOnDisable = Definition.restoreCollisionOnDisable;
			_configured = true;
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_flyState = base.StateMachine?.GetStateById("imp_flight.fly");
		_landState = base.StateMachine?.GetStateById("imp_flight.land");
		ConnectStateSignals();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			ApplyPendingSyncData();
		}
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncData();
		if (base.Alive && base.StateMachine?.CurrentStateHandle?.StableId == "imp_flight.fly" && TryGetParent(out var imp))
		{
			SuppressCollision(imp);
		}
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		if (_pendingSyncData != null)
		{
			_pendingSyncData.Remove("state");
		}
		RestoreSuppressedCollision();
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		ApplyPendingSyncData();
		ApplyAuthoritativeFlightInvariants();
	}

	private void ApplyAuthoritativeFlightInvariants()
	{
		if (!TryGetParent(out var imp))
		{
			return;
		}
		string text = base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
		if (text == "imp_flight.fly")
		{
			SuppressCollision(imp);
			if (!string.IsNullOrEmpty(imp.flyAnimeClip) && imp.sprite.clip != imp.flyAnimeClip)
			{
				imp.sprite.SetAnimation(imp.flyAnimeClip, loop: false);
			}
			imp.sprite.timeScale = imp.timeScale * (double)flyAnimationSpeedScale;
		}
		else
		{
			RestoreSuppressedCollision();
			if (text == "imp_flight.land" && !string.IsNullOrEmpty(imp.landAnimeClip) && imp.sprite.clip != imp.landAnimeClip)
			{
				imp.sprite.SetAnimation(imp.landAnimeClip, loop: false);
			}
			imp.sprite.timeScale = imp.timeScale;
		}
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		RestoreSuppressedCollision();
		_flyState = null;
		_landState = null;
	}

	private void DisconnectParentSignals()
	{
		DisconnectParentReady();
		if (_landSignalConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnLand -= Land;
		}
		_landSignalConnected = false;
	}

	private void ConnectParentSignals()
	{
		if (!_landSignalConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnLand += Land;
			_landSignalConnected = true;
		}
		if (!GodotObject.IsInstanceValid(parent) || parent.IsNodeReady())
		{
			ApplyPendingSyncData();
		}
		else if (!_parentReadyConnected)
		{
			parent.Ready += ParentReady;
			_parentReadyConnected = true;
		}
	}

	private void ParentReady()
	{
		DisconnectParentReady();
		ApplyPendingSyncData();
	}

	private void DisconnectParentReady()
	{
		if (_parentReadyConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= ParentReady;
		}
		_parentReadyConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			StateHandle flyState = _flyState;
			if (flyState != null && flyState.IsValid)
			{
				_flyState.Entered += FlyEntered;
				_flyState.PhysicsProcessing += FlyProcessing;
			}
			StateHandle landState = _landState;
			if (landState != null && landState.IsValid)
			{
				_landState.Entered += LandEntered;
				_landState.PhysicsProcessing += LandProcessing;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_flyState != null)
			{
				_flyState.Entered -= FlyEntered;
				_flyState.PhysicsProcessing -= FlyProcessing;
			}
			if (_landState != null)
			{
				_landState.Entered -= LandEntered;
				_landState.PhysicsProcessing -= LandProcessing;
			}
			_stateSignalsConnected = false;
		}
	}

	public void FlyEntered()
	{
		if (TryGetParent(out var imp))
		{
			if (!string.IsNullOrEmpty(flyAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(flyAudio);
			}
			SuppressCollision(imp);
			if (!string.IsNullOrEmpty(imp.flyAnimeClip))
			{
				imp.sprite.SetAnimation(imp.flyAnimeClip, loop: false, animationBlend);
			}
		}
	}

	public void FlyProcessing(double delta)
	{
		if (TryGetParent(out var imp))
		{
			double num = imp.timeScale * (double)flyAnimationSpeedScale;
			if (!Mathf.IsEqualApprox(imp.sprite.timeScale, num))
			{
				imp.sprite.timeScale = num;
			}
			if (!IsRemoteClient && imp.z <= imp.groundHeight)
			{
				Land();
			}
		}
	}

	public void LandEntered()
	{
		if (TryGetParent(out var imp) && !string.IsNullOrEmpty(imp.landAnimeClip))
		{
			imp.sprite.SetAnimation(imp.landAnimeClip, loop: false, animationBlend);
		}
	}

	public void LandProcessing(double delta)
	{
		if (TryGetParent(out var imp) && !Mathf.IsEqualApprox(imp.sprite.timeScale, imp.timeScale))
		{
			imp.sprite.timeScale = imp.timeScale;
		}
	}

	public void Fly()
	{
		if (TryGetParent(out var imp))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized && !flyEvent.IsEmpty)
			{
				imp.Component();
				SendStateEvent(flyEvent);
			}
		}
	}

	public void Land()
	{
		if (IsRemoteClient || !TryGetParent(out var imp) || imp.landOver)
		{
			return;
		}
		imp.landOver = true;
		RestoreSuppressedCollision();
		if (!string.IsNullOrEmpty(imp.landAnimeClip))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				if (!landEvent.IsEmpty)
				{
					SendStateEvent(landEvent);
				}
				else
				{
					imp.Walk();
				}
				return;
			}
		}
		SendStateEvent(idleEvent);
		imp.Walk();
	}

	public bool AnimeCompleted(string clip)
	{
		if (TryGetParent(out var imp) && clip == imp.landAnimeClip)
		{
			SendStateEvent(idleEvent);
			imp.Walk();
			return true;
		}
		return false;
	}

	private bool TryGetParent(out TowerDefenseZombieImpBase imp)
	{
		imp = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(imp) && GodotObject.IsInstanceValid(imp.instance) && GodotObject.IsInstanceValid(imp.sprite) && !imp.die)
		{
			return !imp.isDestroy;
		}
		return false;
	}

	private static void RestoreCollision(TowerDefenseZombieImpBase imp)
	{
		imp.instance.collisionFlags = imp.collectonFlagsSave;
		imp.instance.maskFlags = imp.maskFlagsSave;
	}

	private void SuppressCollision(TowerDefenseZombieImpBase imp)
	{
		if (suppressCollisionInFlight && GodotObject.IsInstanceValid(imp?.instance))
		{
			imp.instance.collisionFlags = 0;
			imp.instance.maskFlags = 0;
			_flightCollisionSuppressed = true;
		}
	}

	private void RestoreSuppressedCollision()
	{
		if (_flightCollisionSuppressed)
		{
			if (GodotObject.IsInstanceValid(parent?.instance))
			{
				RestoreCollision(parent);
			}
			_flightCollisionSuppressed = false;
		}
	}

	protected override void OnAliveChanged(bool alive)
	{
		TowerDefenseZombieImpBase imp;
		if (!alive && restoreCollisionOnDisable)
		{
			RestoreSuppressedCollision();
		}
		else if (alive && base.StateMachine?.CurrentStateHandle?.StableId == "imp_flight.fly" && TryGetParent(out imp))
		{
			SuppressCollision(imp);
		}
	}

	public override Dictionary ExportComponentSave()
	{
		return SerializeRuntimeState();
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		DeserializeRuntimeState(data);
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeRuntimeState();
	}

	private Dictionary SerializeRuntimeState()
	{
		Dictionary dictionary = new Dictionary
		{
			{
				"throw",
				parent != null && parent._throw
			},
			{
				"landOver",
				parent != null && parent.landOver
			},
			{
				"z",
				(parent != null) ? parent.z : 0.0
			},
			{
				"ySpeed",
				(parent != null) ? parent.ySpeed : 0.0
			},
			{
				"gravity",
				(parent != null) ? parent.gravity : 0.0
			},
			{
				"groundHeight",
				(parent != null) ? parent.groundHeight : 0.0
			}
		};
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		DeserializeRuntimeState(data);
	}

	private void DeserializeRuntimeState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady() && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				ApplySyncData(data);
				return;
			}
		}
		_pendingSyncData = data.Duplicate(deep: true);
		if (!base.ShouldApplyLegacyStateField)
		{
			_pendingSyncData.Remove("state");
		}
	}

	private void ApplyPendingSyncData()
	{
		if (_pendingSyncData != null && GodotObject.IsInstanceValid(parent) && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				Dictionary pendingSyncData = _pendingSyncData;
				_pendingSyncData = null;
				ApplySyncData(pendingSyncData);
			}
		}
	}

	private void ApplySyncData(Dictionary data)
	{
		parent._throw = data.GetValueOrDefault("throw", parent._throw).AsBool();
		parent.landOver = data.GetValueOrDefault("landOver", parent.landOver).AsBool();
		parent.z = data.GetValueOrDefault("z", parent.z).AsDouble();
		parent.ySpeed = data.GetValueOrDefault("ySpeed", parent.ySpeed).AsDouble();
		parent.gravity = data.GetValueOrDefault("gravity", parent.gravity).AsDouble();
		parent.groundHeight = data.GetValueOrDefault("groundHeight", parent.groundHeight).AsDouble();
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			string text = data["state"].AsString();
			if ((base.StateMachine?.ResolveState(text, allowDisplayNameFallback: true))?.StableId == "imp_flight.fly" || text == flyStateName.ToString())
			{
				SuppressCollision(parent);
			}
			else if (parent.landOver)
			{
				RestoreSuppressedCollision();
			}
			SyncForceState(base.StateMachine, text);
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
}
