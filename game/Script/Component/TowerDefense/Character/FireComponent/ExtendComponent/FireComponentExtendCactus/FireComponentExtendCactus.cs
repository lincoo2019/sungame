using System;
using Godot;
using Godot.Collections;

public sealed class FireComponentExtendCactus : FireComponentExtendBase
{
	public delegate void UpOverEventHandler();

	public delegate void DownOverEventHandler();

	public FireComponent fireComponent;

	public AdobeAnimateSprite sprite;

	public string upAnimeClips = "Up";

	public float upAnimeTimeScale = 2f;

	public string downAnimeClips = "Down";

	public float downAnimeTimeScale = 2f;

	public string upFireAnimeClips = "UpFire";

	public string downFireAnimeClips = "Fire";

	public float transitionAnimationStartPosition = 0.2f;

	public StringName upStateEvent = "ToUp";

	public StringName downStateEvent = "ToDown";

	public StringName idleStateEvent = "ToIdle";

	public bool up;

	public bool canRun;

	private StateHandle _idleState;

	private StateHandle _upState;

	private StateHandle _downState;

	private bool _signalsConnected;

	private bool _configured;

	private string _pendingStateName = "";

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

	private FireComponentExtendCactusDefinition Definition => base.ComponentDefinition as FireComponentExtendCactusDefinition;

	public event UpOverEventHandler OnUpOver;

	public event DownOverEventHandler OnDownOver;

	protected override void OnBound()
	{
		base.OnBound();
		ApplyDefinitionOnce();
		ResolveReferences();
		RefreshConfiguration();
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		ConnectSignals();
		ApplyPendingStateName();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSignals();
		fireComponent = null;
		sprite = null;
		base.OnDetaching(reason);
	}

	protected override void OnReleased()
	{
		DisconnectSignals();
		this.OnUpOver = null;
		this.OnDownOver = null;
		fireComponent = null;
		sprite = null;
		base.OnReleased();
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			upAnimeClips = Definition.upAnimeClips;
			upAnimeTimeScale = Definition.upAnimeTimeScale;
			downAnimeClips = Definition.downAnimeClips;
			downAnimeTimeScale = Definition.downAnimeTimeScale;
			upFireAnimeClips = Definition.upFireAnimeClips;
			downFireAnimeClips = Definition.downFireAnimeClips;
			transitionAnimationStartPosition = Definition.transitionAnimationStartPosition;
			upStateEvent = Definition.upStateEvent;
			downStateEvent = Definition.downStateEvent;
			idleStateEvent = Definition.idleStateEvent;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		fireComponent = null;
		if (base.Manager != null && !string.IsNullOrEmpty(Definition?.fireComponentInstanceId) && base.Manager.TryGetRuntimeByInstanceId(Definition.fireComponentInstanceId, out var runtime))
		{
			fireComponent = runtime as FireComponent;
		}
		sprite = ((Definition == null || Definition.spritePath == null || Definition.spritePath.IsEmpty || !GodotObject.IsInstanceValid(parent)) ? null : parent.GetNodeOrNull<AdobeAnimateSprite>(Definition.spritePath));
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("cactus.idle");
		_upState = base.StateMachine?.GetStateById("cactus.up");
		_downState = base.StateMachine?.GetStateById("cactus.down");
		ConnectSignals();
		Callable.From(ApplyPendingStateName).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingStateName();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectSignals();
		_idleState = null;
		_upState = null;
		_downState = null;
	}

	protected override void OnAliveChanged(bool value)
	{
		canRun = value && canRun;
	}

	private void ConnectSignals()
	{
		if (!_signalsConnected)
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.OnAnimeCompleted += AnimeCompleted;
			}
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_upState, UpEntered, UpExited, UpProcessing);
			ConnectState(_downState, DownEntered, DownExited, DownProcessing);
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
		DisconnectState(_upState, UpEntered, UpExited, UpProcessing);
		DisconnectState(_downState, DownEntered, DownExited, DownProcessing);
		_signalsConnected = false;
	}

	private static void ConnectState(StateHandle handle, Action entered, Action exited, Action<double> processing)
	{
		if (handle != null && handle.IsValid)
		{
			handle.Entered += entered;
			handle.Exited += exited;
			handle.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle handle, Action entered, Action exited, Action<double> processing)
	{
		if (handle != null)
		{
			handle.Entered -= entered;
			handle.Exited -= exited;
			handle.PhysicsProcessing -= processing;
		}
	}

	public void RefreshConfiguration()
	{
		ApplyFireAnimation();
	}

	private void ApplyFireAnimation()
	{
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			fireComponent.fireAnimeClips = (up ? upFireAnimeClips : downFireAnimeClips);
		}
	}

	public bool IsUp()
	{
		return up;
	}

	public void IdleEntered()
	{
		ApplyFireAnimation();
	}

	public void IdleProcessing(double delta)
	{
		if (!base.Alive || IsRemoteClient || !GodotObject.IsInstanceValid(parent) || fireComponent == null || fireComponent.IsReleased)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		int primaryCollectionFlag = 2;
		int secondaryCollectionFlag = 9;
		fireComponent.CanFireCheckPairOnce(primaryCollectionFlag, secondaryCollectionFlag, out var hasPrimaryTarget, out var hasSecondaryTarget);
		if (hasPrimaryTarget)
		{
			canRun = up;
			if (!up && !fireComponent.IsFireStateBusy())
			{
				parent.Component();
				SendStateEvent(upStateEvent);
			}
			return;
		}
		canRun = false;
		if (up && fireComponent.IsFireStateBusy())
		{
			return;
		}
		if (up && fireComponent.timer <= 0f && fireComponent.checkInterval <= 0)
		{
			parent.Component();
			SendStateEvent(downStateEvent);
		}
		else if (hasSecondaryTarget)
		{
			canRun = !up;
			if (up)
			{
				parent.Component();
				SendStateEvent(downStateEvent);
			}
		}
	}

	public void IdleExited()
	{
	}

	public void UpEntered()
	{
		up = true;
		canRun = false;
		ApplyFireAnimation();
		PlayTransition(upAnimeClips);
	}

	public void UpProcessing(double delta)
	{
		ApplyAnimationTimeScale(upAnimeTimeScale);
		RecoverInterruptedTransition(upAnimeClips);
	}

	public void UpExited()
	{
	}

	public void DownEntered()
	{
		up = false;
		canRun = false;
		ApplyFireAnimation();
		PlayTransition(downAnimeClips);
	}

	public void DownProcessing(double delta)
	{
		ApplyAnimationTimeScale(downAnimeTimeScale);
		RecoverInterruptedTransition(downAnimeClips);
	}

	public void DownExited()
	{
	}

	private void PlayTransition(string clip)
	{
		if (!GodotObject.IsInstanceValid(sprite) || string.IsNullOrEmpty(clip) || !sprite.HasClip(clip))
		{
			FinishTransition(clip);
		}
		else
		{
			sprite.SetAnimation(clip, loop: false, Math.Max(0f, transitionAnimationStartPosition));
		}
	}

	private void ApplyAnimationTimeScale(float multiplier)
	{
		if (GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(parent))
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			sprite.timeScale = ((GodotObject.IsInstanceValid(instance) && instance.IsIZMMode()) ? ((double)multiplier) : (parent.timeScale * (double)multiplier));
		}
	}

	private void RecoverInterruptedTransition(string clip)
	{
		if (GodotObject.IsInstanceValid(sprite) && !string.IsNullOrEmpty(clip) && (sprite.clipOver || !(sprite.clip == clip)))
		{
			FinishTransition(clip);
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (clip == upAnimeClips || clip == downAnimeClips)
		{
			FinishTransition(clip);
		}
	}

	private void FinishTransition(string clip)
	{
		string text;
		if (clip == upAnimeClips)
		{
			text = "cactus.up";
		}
		else
		{
			if (!(clip == downAnimeClips))
			{
				return;
			}
			text = "cactus.down";
		}
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid && !(stateHandle.StableId != text))
		{
			if (clip == upAnimeClips)
			{
				this.OnUpOver?.Invoke();
			}
			else
			{
				this.OnDownOver?.Invoke();
			}
			SendStateEvent(idleStateEvent);
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.Idle();
			}
		}
	}

	protected override bool CanRunLocal()
	{
		return canRun;
	}

	public override bool CanRun()
	{
		return base.CanRun();
	}

	public override Dictionary ExportComponentSave()
	{
		return SerializeState();
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		DeserializeState(data);
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeState();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		DeserializeState(data);
	}

	private Dictionary SerializeState()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "up", up },
			{ "canRun", canRun },
			{ "alive", base.alive }
		};
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	private void DeserializeState(Dictionary data)
	{
		up = data.GetValueOrDefault("up", up).AsBool();
		canRun = data.GetValueOrDefault("canRun", canRun).AsBool();
		SetAlive(data.GetValueOrDefault("alive", base.alive).AsBool());
		ApplyFireAnimation();
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
	}

	private void ApplyPendingStateName()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingStateName))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingStateName))
			{
				_pendingStateName = "";
			}
		}
	}

	private void ApplyStateName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				if (!SyncForceState(base.StateMachine, name))
				{
					_pendingStateName = name;
				}
				return;
			}
		}
		_pendingStateName = name;
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId;
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
