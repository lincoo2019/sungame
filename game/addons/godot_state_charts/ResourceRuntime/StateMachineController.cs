using System;
using Godot;

public sealed class StateMachineController : IStateMachineController, IDisposable
{
	private readonly StateMachineCallbackRegistry _callbackRegistry;

	private StateMachineProgram _program;

	private StateMachineRuntime _runtime;

	private StateMachineCallbackBinding _callbackBinding;

	internal StateMachineRuntime Runtime => _runtime;

	public bool IsInitialized { get; private set; }

	public string InitializationError { get; private set; } = string.Empty;

	public bool HasProcessWork { get; private set; }

	public bool HasPhysicsWork { get; private set; }

	public bool HasPendingTransition => _runtime?.HasPendingTransition ?? false;

	public bool SupportsFlatDirectStateTransitions => _program?.SupportsFlatDirectStateTransitions ?? false;

	public long Revision => _runtime?.Revision ?? 0;

	public long SnapshotRevision => _runtime?.SnapshotRevision ?? 0;

	public StateHandle CurrentStateHandle => _runtime?.GetCurrentStateHandle();

	public event Action<StringName> EventReceived;

	public event Action<CompiledStateMachineTransition> TransitionTaken;

	public event Action<CompiledStateMachineTransition> TransitionCompleted;

	public event Action<string> Diagnostic;

	public StateMachineController(StateMachineCallbackRegistry callbackRegistry = null)
	{
		_callbackRegistry = callbackRegistry ?? StateMachineCallbackRegistry.Shared;
	}

	public bool Initialize(StateMachineDefinition definition, StateMachineDelayClock delayClock = StateMachineDelayClock.Process)
	{
		return InitializeCore(definition, null, string.Empty, delayClock);
	}

	public bool Initialize(StateMachineDefinition definition, object host, StateMachineDelayClock delayClock = StateMachineDelayClock.Process)
	{
		return InitializeCore(definition, host, string.Empty, delayClock);
	}

	public bool Initialize(StateMachineDefinition definition, object host, string callbackOwnerId, StateMachineDelayClock delayClock = StateMachineDelayClock.Process)
	{
		return InitializeCore(definition, host, callbackOwnerId, delayClock);
	}

	private bool InitializeCore(StateMachineDefinition definition, object host, string callbackOwnerId, StateMachineDelayClock delayClock)
	{
		ReleaseRuntime();
		InitializationError = string.Empty;
		try
		{
			_program = StateMachineProgramCache.Acquire(definition);
			if (_program.UsesExecutableCallbacks)
			{
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = _callbackRegistry.EnsureBuiltinAssembly(typeof(StateMachineController).Assembly);
				if (!stateMachineCallbackRegistrationResult.Success)
				{
					throw new InvalidOperationException("Builtin state-machine callbacks could not register: " + stateMachineCallbackRegistrationResult.Error);
				}
				if (!_callbackRegistry.TryAcquireBinding(callbackOwnerId, _program, host, out _callbackBinding, out var diagnostic))
				{
					throw new InvalidOperationException($"[{diagnostic.Code}] {diagnostic.Message} Definition='{_program.DefinitionId}', State='{diagnostic.StableId}', Callback='{diagnostic.CallbackKey}', Host='{host?.GetType().FullName ?? "<null>"}'.");
				}
			}
			_runtime = new StateMachineRuntime();
			_runtime.Initialize(_program, delayClock, _callbackBinding);
			_runtime.EventReceived += OnRuntimeEventReceived;
			_runtime.TransitionTaken += OnRuntimeTransitionTaken;
			_runtime.TransitionCompleted += OnRuntimeTransitionCompleted;
			_runtime.Diagnostic += OnRuntimeDiagnostic;
			HasProcessWork = _program.ProcessStateIndices.Length > 0 || (_program.HasDelayedTransitions && delayClock == StateMachineDelayClock.Process);
			HasPhysicsWork = _program.PhysicsStateIndices.Length > 0 || (_program.HasDelayedTransitions && delayClock == StateMachineDelayClock.Physics);
			IsInitialized = true;
			return true;
		}
		catch (Exception ex)
		{
			ReleaseRuntime();
			InitializationError = (string.IsNullOrWhiteSpace(ex.Message) ? "State machine controller initialization failed." : ex.Message);
			EmitInitializationDiagnostic(InitializationError);
			return false;
		}
	}

	public bool EnterInitialState()
	{
		return _runtime?.EnterInitialState() ?? false;
	}

	public StateHandle GetStateById(string stableId)
	{
		return _runtime?.GetStateHandle(stableId);
	}

	public StateHandle ResolveState(string stateIdentity, bool allowDisplayNameFallback = false)
	{
		if (_program == null || _runtime == null || string.IsNullOrWhiteSpace(stateIdentity))
		{
			return null;
		}
		if (_program.TryResolveStateIndex(stateIdentity, out var stateIndex))
		{
			return _runtime.GetStateHandle(_program.States[stateIndex].StableId);
		}
		if (!allowDisplayNameFallback)
		{
			return null;
		}
		ReadOnlySpan<CompiledStateMachineState> states = _program.States;
		for (int i = 0; i < states.Length; i++)
		{
			if (string.Equals(states[i].DisplayName.ToString(), stateIdentity, StringComparison.Ordinal))
			{
				return _runtime.GetStateHandle(states[i].StableId);
			}
		}
		return null;
	}

	public bool SendEvent(StringName eventName)
	{
		return _runtime?.SendEvent(eventName) ?? false;
	}

	public bool TrySetFlatState(StateHandle targetState, StringName sourceEvent)
	{
		return _runtime?.TrySetFlatState(targetState, sourceEvent) ?? false;
	}

	public void TickProcess(double delta)
	{
		_runtime?.TickProcess(delta);
	}

	public void TickPhysics(double delta)
	{
		_runtime?.TickPhysics(delta);
	}

	internal void TickPhysicsDetailed(double delta)
	{
		_runtime?.TickPhysicsDetailed(delta);
	}

	public void SetExpressionProperty(StringName name, Variant value)
	{
		_runtime?.SetExpressionProperty(name, value);
	}

	public Variant GetExpressionProperty(StringName name, Variant fallback = default(Variant))
	{
		return _runtime?.GetExpressionProperty(name, fallback) ?? fallback;
	}

	public StateMachineSnapshot CaptureSnapshot()
	{
		return _runtime?.CaptureSnapshot();
	}

	public bool CanRestoreSnapshot(StateMachineSnapshot snapshot)
	{
		return _runtime?.CanRestoreSnapshot(snapshot) ?? false;
	}

	public bool RestoreSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = false)
	{
		return _runtime?.RestoreSnapshot(snapshot, suppressEntryEffects) ?? false;
	}

	public bool ApplyRemoteSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = true)
	{
		return _runtime?.ApplyRemoteSnapshot(snapshot, suppressEntryEffects) ?? false;
	}

	public void Dispose()
	{
		ReleaseRuntime();
		this.EventReceived = null;
		this.TransitionTaken = null;
		this.TransitionCompleted = null;
		this.Diagnostic = null;
	}

	private void ReleaseRuntime()
	{
		if (_runtime != null)
		{
			_runtime.EventReceived -= OnRuntimeEventReceived;
			_runtime.TransitionTaken -= OnRuntimeTransitionTaken;
			_runtime.TransitionCompleted -= OnRuntimeTransitionCompleted;
			_runtime.Diagnostic -= OnRuntimeDiagnostic;
			_runtime.Dispose();
			_runtime = null;
		}
		_callbackBinding?.Dispose();
		_callbackBinding = null;
		if (_program != null)
		{
			StateMachineProgramCache.Release(_program);
			_program = null;
		}
		HasProcessWork = false;
		HasPhysicsWork = false;
		IsInitialized = false;
	}

	private void OnRuntimeEventReceived(StringName eventName)
	{
		this.EventReceived?.Invoke(eventName);
	}

	private void OnRuntimeTransitionTaken(CompiledStateMachineTransition transition)
	{
		this.TransitionTaken?.Invoke(transition);
	}

	private void OnRuntimeTransitionCompleted(CompiledStateMachineTransition transition)
	{
		this.TransitionCompleted?.Invoke(transition);
	}

	private void OnRuntimeDiagnostic(string code)
	{
		this.Diagnostic?.Invoke(code);
	}

	private void EmitInitializationDiagnostic(string error)
	{
		try
		{
			this.Diagnostic?.Invoke(error);
		}
		catch (Exception)
		{
		}
	}
}
