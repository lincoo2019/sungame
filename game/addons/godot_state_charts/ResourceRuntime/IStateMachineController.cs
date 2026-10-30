using System;
using Godot;

public interface IStateMachineController : IDisposable
{
	bool IsInitialized { get; }

	string InitializationError { get; }

	bool HasProcessWork { get; }

	bool HasPhysicsWork { get; }

	bool HasPendingTransition { get; }

	bool SupportsFlatDirectStateTransitions { get; }

	long Revision { get; }

	long SnapshotRevision { get; }

	StateHandle CurrentStateHandle { get; }

	event Action<StringName> EventReceived;

	event Action<CompiledStateMachineTransition> TransitionTaken;

	event Action<CompiledStateMachineTransition> TransitionCompleted;

	event Action<string> Diagnostic;

	bool Initialize(StateMachineDefinition definition, StateMachineDelayClock delayClock = StateMachineDelayClock.Process);

	bool Initialize(StateMachineDefinition definition, object host, StateMachineDelayClock delayClock = StateMachineDelayClock.Process);

	bool Initialize(StateMachineDefinition definition, object host, string callbackOwnerId, StateMachineDelayClock delayClock = StateMachineDelayClock.Process);

	bool EnterInitialState();

	StateHandle GetStateById(string stableId);

	StateHandle ResolveState(string stateIdentity, bool allowDisplayNameFallback = false);

	bool SendEvent(StringName eventName);

	bool TrySetFlatState(StateHandle targetState, StringName sourceEvent);

	void TickProcess(double delta);

	void TickPhysics(double delta);

	void SetExpressionProperty(StringName name, Variant value);

	Variant GetExpressionProperty(StringName name, Variant fallback = default(Variant));

	StateMachineSnapshot CaptureSnapshot();

	bool CanRestoreSnapshot(StateMachineSnapshot snapshot);

	bool RestoreSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = false);

	bool ApplyRemoteSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = true);
}
