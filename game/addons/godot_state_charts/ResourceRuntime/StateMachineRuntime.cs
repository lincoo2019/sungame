using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Godot;

public sealed class StateMachineRuntime : IDisposable
{
	private const int TransactionBudget = 1024;

	private const string AutomaticBudgetDiagnostic = "SMR001";

	private const string SnapshotMismatchDiagnostic = "SMS001";

	private static long _nextHostGeneration;

	private StateMachineProgram _program;

	private StateMachineCallbackBinding _callbackBinding;

	private bool[] _active;

	private int[] _activeLeafIndices;

	private int _activeLeafCount;

	private int _activeProcessStateCount;

	private int _singleActiveProcessStateIndex = -1;

	private int _singleActiveProcessStateOrder = -1;

	private int[] _activeProcessStateIndices;

	private int[] _activeProcessStatePositions;

	private int[] _processStateOrders;

	private int _activePhysicsStateCount;

	private int _singleActivePhysicsStateIndex = -1;

	private int _singleActivePhysicsStateOrder = -1;

	private int[] _activePhysicsStateIndices;

	private int[] _activePhysicsStatePositions;

	private int[] _physicsStateOrders;

	private long _activeConfigurationVersion;

	private int[] _pendingTransitionBySource;

	private int _pendingTransitionCount;

	private double[] _pendingDelayRemaining;

	private double[] _pendingDelayInitial;

	private bool[] _automaticDelayConsumed;

	private int[] _historySlots;

	private int[] _pathScratch;

	private StateHandle[] _handles;

	private StringName[] _eventQueue;

	private int _eventHead;

	private int _eventCount;

	private bool _transactionActive;

	private bool _initialized;

	private bool _disposed;

	private bool _disposeRequested;

	private int _dispatchDepth;

	private int _transactionAcceptedEvents;

	private bool _budgetDiagnosticEmitted;

	private ExceptionDispatchInfo _pendingCallbackException;

	private string[] _processMetricNames;

	private string[] _physicsMetricNames;

	private Dictionary<StringName, Variant> _expressionProperties;

	private long _hostGeneration;

	private StateMachineDelayClock _delayClock;

	public StateMachineProgram Program => _program;

	public long Revision { get; private set; }

	public long SnapshotRevision { get; private set; }

	public bool HasPendingTransition
	{
		get
		{
			if (_initialized && !_disposed)
			{
				return _pendingTransitionCount > 0;
			}
			return false;
		}
	}

	public string LastDiagnosticCode { get; private set; } = string.Empty;

	public double PendingDelayRemaining
	{
		get
		{
			double num = 1.0 / 0.0;
			if (_pendingTransitionBySource == null)
			{
				return 0.0;
			}
			for (int i = 0; i < _pendingTransitionBySource.Length; i++)
			{
				if (_pendingTransitionBySource[i] >= 0 && _pendingDelayRemaining[i] < num)
				{
					num = _pendingDelayRemaining[i];
				}
			}
			if (!double.IsPositiveInfinity(num))
			{
				return Math.Max(0.0, num);
			}
			return 0.0;
		}
	}

	public event Action<StringName> EventReceived;

	public event Action<CompiledStateMachineTransition> TransitionTaken;

	public event Action<CompiledStateMachineTransition> TransitionCompleted;

	public event Action<string> Diagnostic;

	public void Initialize(StateMachineProgram program, StateMachineDelayClock delayClock = StateMachineDelayClock.Process, StateMachineCallbackBinding callbackBinding = null)
	{
		if (_dispatchDepth > 0)
		{
			throw new InvalidOperationException("Cannot initialize StateMachineRuntime from inside a runtime callback.");
		}
		if (program == null)
		{
			throw new ArgumentNullException("program");
		}
		DisposeStorage();
		_program = program;
		_callbackBinding = callbackBinding;
		_delayClock = delayClock;
		_hostGeneration = Interlocked.Increment(ref _nextHostGeneration);
		_active = new bool[program.StateCount];
		_activeLeafIndices = new int[Math.Max(1, program.StateCount)];
		_activeProcessStateCount = 0;
		_singleActiveProcessStateIndex = -1;
		_singleActiveProcessStateOrder = -1;
		int length = program.ProcessStateIndices.Length;
		_activeProcessStateIndices = ((length > 0) ? new int[length] : Array.Empty<int>());
		_activeProcessStatePositions = ((length > 0) ? new int[program.StateCount] : Array.Empty<int>());
		if (length > 0)
		{
			Array.Fill(_activeProcessStatePositions, -1);
		}
		_processStateOrders = program.ProcessStateOrdersStorage;
		_activePhysicsStateCount = 0;
		_singleActivePhysicsStateIndex = -1;
		_singleActivePhysicsStateOrder = -1;
		int length2 = program.PhysicsStateIndices.Length;
		_activePhysicsStateIndices = ((length2 > 0) ? new int[length2] : Array.Empty<int>());
		_activePhysicsStatePositions = ((length2 > 0) ? new int[program.StateCount] : Array.Empty<int>());
		if (length2 > 0)
		{
			Array.Fill(_activePhysicsStatePositions, -1);
		}
		_physicsStateOrders = program.PhysicsStateOrdersStorage;
		_activeConfigurationVersion = 0L;
		_pendingTransitionBySource = (program.HasDelayedTransitions ? new int[program.StateCount] : Array.Empty<int>());
		if (_pendingTransitionBySource.Length != 0)
		{
			Array.Fill(_pendingTransitionBySource, -1);
		}
		_pendingTransitionCount = 0;
		_pendingDelayRemaining = (program.HasDelayedTransitions ? new double[program.StateCount] : Array.Empty<double>());
		_pendingDelayInitial = (program.HasDelayedTransitions ? new double[program.StateCount] : Array.Empty<double>());
		_automaticDelayConsumed = (program.HasDelayedTransitions ? new bool[program.StateCount] : Array.Empty<bool>());
		_historySlots = (program.HasHistoryStates ? new int[program.StateCount] : Array.Empty<int>());
		if (_historySlots.Length != 0)
		{
			Array.Fill(_historySlots, -1);
		}
		_pathScratch = new int[Math.Max(1, program.StateCount)];
		_handles = null;
		_eventQueue = Array.Empty<StringName>();
		_expressionProperties = null;
		Revision = 0L;
		SnapshotRevision = 0L;
		LastDiagnosticCode = string.Empty;
		_initialized = true;
		_disposed = false;
		_disposeRequested = false;
		_pendingCallbackException = null;
	}

	public bool EnterInitialState()
	{
		if (!_initialized || _disposed || _active[_program.RootStateIndex])
		{
			return false;
		}
		BeginDispatch();
		BeginTransaction();
		try
		{
			int budget = 0;
			EnterStateAndInitialDescendants(_program.RootStateIndex);
			if (!_disposeRequested && RunAutomaticTransitions(ref budget))
			{
				DrainQueuedEvents(ref budget);
			}
		}
		finally
		{
			_transactionActive = false;
			EndDispatch();
		}
		ThrowPendingCallbackException();
		return true;
	}

	public StateHandle GetStateHandle(string stableId)
	{
		if (!_initialized || _disposed || _disposeRequested || !_program.TryGetStateIndex(stableId, out var stateIndex))
		{
			return null;
		}
		return GetOrCreateStateHandle(stateIndex);
	}

	private StateHandle GetOrCreateStateHandle(int stateIndex)
	{
		if (_handles == null)
		{
			_handles = new StateHandle[_program.StateCount];
		}
		StateHandle stateHandle = _handles[stateIndex];
		if (stateHandle != null)
		{
			return stateHandle;
		}
		stateHandle = new StateHandle(this, _hostGeneration, _program.ProgramGeneration, stateIndex);
		_handles[stateIndex] = stateHandle;
		return stateHandle;
	}

	public bool SendEvent(StringName eventName)
	{
		if (!_initialized || _disposed || _disposeRequested)
		{
			return false;
		}
		if (!EnqueueEvent(eventName))
		{
			return false;
		}
		if (_transactionActive)
		{
			return true;
		}
		BeginDispatch();
		BeginTransaction();
		try
		{
			int budget = 0;
			DrainQueuedEvents(ref budget);
		}
		finally
		{
			_transactionActive = false;
			EndDispatch();
		}
		ThrowPendingCallbackException();
		return true;
	}

	public bool TrySetFlatState(StateHandle targetState, StringName sourceEvent)
	{
		if (!_initialized || _disposed || _disposeRequested || _transactionActive || targetState == null || targetState.Runtime != this || !targetState.IsValid)
		{
			return false;
		}
		int stateIndex = targetState.StateIndex;
		if (!_program.TryGetFlatDirectTransition(stateIndex, sourceEvent, out var transitionIndex) || !_active[_program.RootStateIndex] || _activeLeafCount != 1)
		{
			return false;
		}
		int num = _activeLeafIndices[0];
		if (_program.States[num].ParentIndex != _program.RootStateIndex)
		{
			return false;
		}
		BeginDispatch();
		BeginTransaction();
		try
		{
			int budget = 1;
			InvokeCallback(this.EventReceived, sourceEvent);
			if (!_disposeRequested)
			{
				ExecuteFlatTransition(transitionIndex, num, stateIndex);
			}
			if (_pendingCallbackException != null)
			{
				ClearEventQueue();
			}
			else if (!_disposeRequested)
			{
				DrainQueuedEvents(ref budget);
			}
		}
		finally
		{
			_transactionActive = false;
			EndDispatch();
		}
		ThrowPendingCallbackException();
		return true;
	}

	public void TickProcess(double delta)
	{
		TickProcessCore(delta, detailedCallbacks: false);
	}

	internal void TickProcessDetailed(double delta)
	{
		TickProcessCore(delta, detailedCallbacks: true);
	}

	private void TickProcessCore(double delta, bool detailedCallbacks)
	{
		if (!_initialized || _disposed)
		{
			return;
		}
		if (_dispatchDepth > 0)
		{
			throw new InvalidOperationException("TickProcess cannot be re-entered from a runtime callback.");
		}
		bool flag = _delayClock == StateMachineDelayClock.Process && (_pendingTransitionCount > 0 || _eventCount > 0);
		if (!_disposeRequested && _pendingCallbackException == null && _activeProcessStateCount == 0 && !flag)
		{
			return;
		}
		BeginDispatch();
		try
		{
			ReadOnlySpan<int> processStateIndices = _program.ProcessStateIndices;
			if (_activeProcessStateCount > 0 && processStateIndices.Length > 0 && !_disposeRequested)
			{
				if (_activeProcessStateCount == 1)
				{
					int singleActiveProcessStateIndex = _singleActiveProcessStateIndex;
					int singleActiveProcessStateOrder = _singleActiveProcessStateOrder;
					long activeConfigurationVersion = _activeConfigurationVersion;
					if (detailedCallbacks)
					{
						EmitProcessingDetailed(singleActiveProcessStateIndex, delta);
					}
					else
					{
						EmitProcessing(singleActiveProcessStateIndex, delta);
					}
					if (_activeConfigurationVersion != activeConfigurationVersion && !_disposeRequested)
					{
						DispatchProcessCallbacksAfter(singleActiveProcessStateOrder, delta, detailedCallbacks);
					}
				}
				else
				{
					DispatchProcessCallbacksAfter(-1, delta, detailedCallbacks);
				}
			}
			if (!_disposeRequested && _delayClock == StateMachineDelayClock.Process && (_pendingTransitionCount > 0 || _eventCount > 0))
			{
				AdvanceDelays(delta);
			}
		}
		finally
		{
			EndDispatch();
		}
		ThrowPendingCallbackException();
	}

	public void TickPhysics(double delta)
	{
		TickPhysicsCore(delta, detailedCallbacks: false);
	}

	internal void TickPhysicsDetailed(double delta)
	{
		TickPhysicsCore(delta, detailedCallbacks: true);
	}

	private void TickPhysicsCore(double delta, bool detailedCallbacks)
	{
		if (!_initialized || _disposed)
		{
			return;
		}
		if (_dispatchDepth > 0)
		{
			throw new InvalidOperationException("TickPhysics cannot be re-entered from a runtime callback.");
		}
		bool flag = _delayClock == StateMachineDelayClock.Physics && (_pendingTransitionCount > 0 || _eventCount > 0);
		if (!_disposeRequested && _pendingCallbackException == null && _activePhysicsStateCount == 0 && !flag)
		{
			return;
		}
		BeginDispatch();
		try
		{
			ReadOnlySpan<int> physicsStateIndices = _program.PhysicsStateIndices;
			if (_activePhysicsStateCount > 0 && physicsStateIndices.Length > 0 && !_disposeRequested)
			{
				if (_activePhysicsStateCount == 1)
				{
					int singleActivePhysicsStateIndex = _singleActivePhysicsStateIndex;
					int singleActivePhysicsStateOrder = _singleActivePhysicsStateOrder;
					long activeConfigurationVersion = _activeConfigurationVersion;
					if (detailedCallbacks)
					{
						EmitPhysicsProcessingDetailed(singleActivePhysicsStateIndex, delta);
					}
					else
					{
						EmitPhysicsProcessing(singleActivePhysicsStateIndex, delta);
					}
					if (_activeConfigurationVersion != activeConfigurationVersion && !_disposeRequested)
					{
						DispatchPhysicsCallbacksAfter(singleActivePhysicsStateOrder, delta, detailedCallbacks);
					}
				}
				else
				{
					DispatchPhysicsCallbacksAfter(-1, delta, detailedCallbacks);
				}
			}
			if (!_disposeRequested && _delayClock == StateMachineDelayClock.Physics && (_pendingTransitionCount > 0 || _eventCount > 0))
			{
				AdvanceDelays(delta);
			}
		}
		finally
		{
			EndDispatch();
		}
		ThrowPendingCallbackException();
	}

	public bool IsActive(string stableId)
	{
		if (_initialized && !_disposed && !_disposeRequested && _program.TryGetStateIndex(stableId, out var stateIndex))
		{
			return _active[stateIndex];
		}
		return false;
	}

	public bool IsActive(int stateIndex)
	{
		if (_initialized && !_disposed && !_disposeRequested && (uint)stateIndex < (uint)_active.Length)
		{
			return _active[stateIndex];
		}
		return false;
	}

	public int GetActiveLeafIds(Span<string> destination)
	{
		if (!_initialized || _disposed || _disposeRequested)
		{
			return 0;
		}
		int num = Math.Min(destination.Length, _activeLeafCount);
		for (int i = 0; i < num; i++)
		{
			destination[i] = _program.States[_activeLeafIndices[i]].StableId;
		}
		return _activeLeafCount;
	}

	public string[] GetActiveLeafIds()
	{
		string[] array = new string[_activeLeafCount];
		GetActiveLeafIds(array);
		return array;
	}

	public StateHandle GetCurrentStateHandle()
	{
		if (!_initialized || _disposed || _disposeRequested || _activeLeafCount == 0)
		{
			return null;
		}
		int stateIndex = _activeLeafIndices[0];
		return GetOrCreateStateHandle(stateIndex);
	}

	public void SetExpressionProperty(StringName name, Variant value)
	{
		if (!_initialized || _disposed || _disposeRequested || (_expressionProperties != null && _expressionProperties.TryGetValue(name, out var value2) && value2.Equals(value)))
		{
			return;
		}
		if (_expressionProperties == null)
		{
			_expressionProperties = new Dictionary<StringName, Variant>();
		}
		_expressionProperties[name] = value;
		IncrementSnapshotRevision();
		if (!_transactionActive && _dispatchDepth == 0 && HasAnyActiveState())
		{
			BeginDispatch();
			BeginTransaction();
			try
			{
				int budget = 0;
				RunAutomaticTransitions(ref budget);
			}
			finally
			{
				_transactionActive = false;
				EndDispatch();
			}
			ThrowPendingCallbackException();
		}
	}

	public Variant GetExpressionProperty(StringName name, Variant defaultValue = default(Variant))
	{
		if (!_initialized || _disposed || _disposeRequested)
		{
			return defaultValue;
		}
		if (_expressionProperties == null || !_expressionProperties.TryGetValue(name, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public StateMachineSnapshot CaptureSnapshot()
	{
		if (!_initialized || _disposed || _disposeRequested)
		{
			return null;
		}
		StateMachineSnapshot stateMachineSnapshot = new StateMachineSnapshot
		{
			DefinitionId = _program.DefinitionId,
			SchemaVersion = _program.SchemaVersion,
			ContentHash = _program.ContentHash,
			Revision = Revision
		};
		for (int i = 0; i < _program.StateCount; i++)
		{
			if (_active[i])
			{
				stateMachineSnapshot.ActiveStateIds.Add(_program.States[i].StableId);
			}
			if (_historySlots.Length != 0)
			{
				int num = _historySlots[i];
				if ((uint)num < (uint)_program.StateCount)
				{
					stateMachineSnapshot.HistoryStateIds.Add(_program.States[num].StableId);
				}
			}
		}
		int num2 = 0;
		while (_pendingTransitionBySource.Length != 0 && num2 < _program.TransitionCount)
		{
			CompiledStateMachineTransition compiledStateMachineTransition = _program.Transitions[num2];
			if (_pendingTransitionBySource[compiledStateMachineTransition.SourceIndex] == num2)
			{
				stateMachineSnapshot.PendingTransitionIds.Add(compiledStateMachineTransition.StableId);
				stateMachineSnapshot.PendingDelayRemaining[compiledStateMachineTransition.StableId] = Math.Max(0.0, _pendingDelayRemaining[compiledStateMachineTransition.SourceIndex]);
			}
			num2++;
		}
		if (_expressionProperties != null)
		{
			foreach (KeyValuePair<StringName, Variant> expressionProperty in _expressionProperties)
			{
				if (StateMachineSnapshot.IsSupportedExpressionVariant(expressionProperty.Value.VariantType))
				{
					stateMachineSnapshot.ExpressionProperties[expressionProperty.Key] = expressionProperty.Value;
				}
			}
		}
		return stateMachineSnapshot;
	}

	public bool RestoreSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = false)
	{
		return RestoreSnapshotInternal(snapshot, suppressEntryEffects, remoteRevision: false);
	}

	public bool CanRestoreSnapshot(StateMachineSnapshot snapshot)
	{
		if (_initialized && !_disposed && !_disposeRequested && snapshot != null && string.Equals(snapshot.DefinitionId, _program.DefinitionId, StringComparison.Ordinal) && snapshot.SchemaVersion == _program.SchemaVersion)
		{
			return string.Equals(snapshot.ContentHash, _program.ContentHash, StringComparison.Ordinal);
		}
		return false;
	}

	public bool ApplyRemoteSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = true)
	{
		if (snapshot == null || snapshot.Revision <= Revision)
		{
			return false;
		}
		return RestoreSnapshotInternal(snapshot, suppressEntryEffects, remoteRevision: true);
	}

	private bool RestoreSnapshotInternal(StateMachineSnapshot snapshot, bool suppressEntryEffects, bool remoteRevision)
	{
		if (!_initialized || _disposed || _disposeRequested || snapshot == null)
		{
			return false;
		}
		if (_dispatchDepth > 0)
		{
			throw new InvalidOperationException("StateMachineRuntime snapshots cannot be restored from inside a runtime callback.");
		}
		if (!CanRestoreSnapshot(snapshot))
		{
			EmitSnapshotMismatchDiagnostic();
			return false;
		}
		long revision = Revision;
		bool flag = false;
		BeginDispatch();
		BeginTransaction();
		try
		{
			ClearEventQueue();
			RestoreExpressionProperties(snapshot);
			ExitActiveConfiguration(!suppressEntryEffects);
			if (!_disposeRequested)
			{
				int targetStateIndex = ResolveSnapshotTarget(snapshot);
				EnterPathAndInitialDescendants(targetStateIndex, !suppressEntryEffects);
			}
			if (!_disposeRequested)
			{
				RestorePendingTransitions(snapshot);
				RefreshActiveLeaf();
				if (remoteRevision)
				{
					Revision = snapshot.Revision;
				}
				else
				{
					long num = Math.Max(revision, snapshot.Revision);
					Revision = ((num == 9223372036854775807L) ? 9223372036854775807L : (num + 1));
				}
				LastDiagnosticCode = string.Empty;
				flag = true;
				IncrementSnapshotRevision();
				int budget = 0;
				if (RunAutomaticTransitions(ref budget, !suppressEntryEffects, incrementRevision: false) && !suppressEntryEffects)
				{
					DrainQueuedEvents(ref budget);
				}
			}
		}
		finally
		{
			_transactionActive = false;
			EndDispatch();
		}
		ThrowPendingCallbackException();
		if (flag)
		{
			return !_disposed;
		}
		return false;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			if (_dispatchDepth > 0)
			{
				_disposeRequested = true;
			}
			else
			{
				DisposeNow();
			}
		}
	}

	private void DisposeNow()
	{
		_disposed = true;
		_initialized = false;
		DisposeStorage();
		_program = null;
		this.EventReceived = null;
		this.TransitionTaken = null;
		this.TransitionCompleted = null;
		this.Diagnostic = null;
	}

	internal bool IsHandleValid(long hostGeneration, long programGeneration, int stateIndex)
	{
		if (_initialized && !_disposed && !_disposeRequested && hostGeneration == _hostGeneration && programGeneration == _program.ProgramGeneration)
		{
			return (uint)stateIndex < (uint)_active.Length;
		}
		return false;
	}

	private void AdvanceDelays(double delta)
	{
		if (delta <= 0.0 || _pendingTransitionBySource.Length == 0)
		{
			return;
		}
		for (int i = 0; i < _pendingTransitionBySource.Length; i++)
		{
			if (_pendingTransitionBySource[i] >= 0 && _active[i])
			{
				_pendingDelayRemaining[i] -= delta;
			}
		}
		BeginTransaction();
		try
		{
			int budget = 0;
			for (int j = 0; j < _program.TransitionCount; j++)
			{
				if (_disposeRequested)
				{
					break;
				}
				CompiledStateMachineTransition transition = _program.Transitions[j];
				int sourceIndex = transition.SourceIndex;
				if (!_active[sourceIndex] || _pendingTransitionBySource[sourceIndex] != j || _pendingDelayRemaining[sourceIndex] > 0.0)
				{
					continue;
				}
				ClearPending(sourceIndex);
				bool flag = IsGuardSatisfied(transition);
				if (_pendingCallbackException != null)
				{
					ClearEventQueue();
					return;
				}
				if (flag)
				{
					if (budget >= 1024)
					{
						EmitBudgetDiagnostic();
						break;
					}
					budget++;
					ExecuteTransition(j);
					if (!RunAutomaticTransitions(ref budget))
					{
						break;
					}
				}
			}
			if (!_disposeRequested)
			{
				DrainQueuedEvents(ref budget);
			}
		}
		finally
		{
			_transactionActive = false;
		}
	}

	private void DrainQueuedEvents(ref int budget)
	{
		while (_eventCount > 0 && !_disposeRequested)
		{
			if (budget >= 1024)
			{
				EmitBudgetDiagnostic();
				ClearEventQueue();
				break;
			}
			budget++;
			StringName stringName = DequeueEvent();
			InvokeCallback(this.EventReceived, stringName);
			if (_disposeRequested)
			{
				break;
			}
			ProcessEvent(stringName);
			if (_pendingCallbackException != null)
			{
				ClearEventQueue();
				break;
			}
			if (!RunAutomaticTransitions(ref budget))
			{
				break;
			}
		}
	}

	private void ProcessEvent(StringName eventName)
	{
		if (!_program.TryGetEventTransitionSlice(eventName, out var slice))
		{
			return;
		}
		bool flag = _activeLeafCount > 1;
		ReadOnlySpan<int> eventTransitionIndices = _program.EventTransitionIndices;
		for (int i = 0; i < slice.Count; i++)
		{
			int num = eventTransitionIndices[slice.Offset + i];
			CompiledStateMachineTransition transition = _program.Transitions[num];
			if (!_active[transition.SourceIndex])
			{
				continue;
			}
			bool flag2 = IsGuardSatisfied(transition);
			if (_pendingCallbackException != null)
			{
				break;
			}
			if (flag2)
			{
				if (transition.DelaySeconds > 0.0)
				{
					SchedulePending(num, transition);
				}
				else
				{
					ExecuteTransition(num);
				}
				if (!flag)
				{
					break;
				}
			}
		}
	}

	private bool RunAutomaticTransitions(ref int budget, bool emitCallbacks = true, bool incrementRevision = true)
	{
		while (true)
		{
			if (_disposeRequested)
			{
				return false;
			}
			int num = -1;
			ReadOnlySpan<int> automaticTransitionIndices = _program.AutomaticTransitionIndices;
			for (int i = 0; i < automaticTransitionIndices.Length; i++)
			{
				int num2 = automaticTransitionIndices[i];
				CompiledStateMachineTransition transition = _program.Transitions[num2];
				if (!_active[transition.SourceIndex])
				{
					continue;
				}
				bool flag = IsGuardSatisfied(transition);
				if (_pendingCallbackException != null)
				{
					ClearEventQueue();
					return false;
				}
				if (flag)
				{
					if (!(transition.DelaySeconds > 0.0))
					{
						num = num2;
						break;
					}
					if (_pendingTransitionBySource[transition.SourceIndex] < 0 && !_automaticDelayConsumed[transition.SourceIndex])
					{
						SchedulePending(num2, transition);
						_automaticDelayConsumed[transition.SourceIndex] = true;
					}
				}
			}
			if (num < 0)
			{
				return true;
			}
			if (budget >= 1024)
			{
				break;
			}
			budget++;
			ExecuteTransition(num, emitCallbacks, incrementRevision);
		}
		EmitBudgetDiagnostic();
		ClearEventQueue();
		return false;
	}

	private void ExecuteTransition(int transitionIndex)
	{
		ExecuteTransition(transitionIndex, emitCallbacks: true, incrementRevision: true);
	}

	private bool IsGuardSatisfied(CompiledStateMachineTransition transition)
	{
		if (transition.Guard.IsDefined)
		{
			return IsGuardSatisfied(transition.Guard, transition);
		}
		return true;
	}

	private bool IsGuardSatisfied(CompiledStateMachineGuard guard, CompiledStateMachineTransition transition)
	{
		Variant value;
		bool flag = guard.Kind switch
		{
			StateMachineGuardKind.ExpressionProperty => _expressionProperties.TryGetValue(guard.PropertyName, out value) && CompareGuardValues(value, guard.ExpectedValue, guard.Operator), 
			StateMachineGuardKind.All => EvaluateAll(guard.Children, transition), 
			StateMachineGuardKind.Any => EvaluateAny(guard.Children, transition), 
			StateMachineGuardKind.Not => guard.Children.Length == 1 && !IsGuardSatisfied(guard.Children[0], transition), 
			StateMachineGuardKind.Callback => EvaluateCallbackGuard(guard, transition), 
			_ => false, 
		};
		if (_pendingCallbackException != null)
		{
			return false;
		}
		if (!guard.Negate)
		{
			return flag;
		}
		return !flag;
	}

	private bool EvaluateAll(CompiledStateMachineGuard[] children, CompiledStateMachineTransition transition)
	{
		for (int i = 0; i < children.Length; i++)
		{
			bool flag = IsGuardSatisfied(children[i], transition);
			if (_pendingCallbackException != null || !flag)
			{
				return false;
			}
		}
		return true;
	}

	private bool EvaluateAny(CompiledStateMachineGuard[] children, CompiledStateMachineTransition transition)
	{
		for (int i = 0; i < children.Length; i++)
		{
			bool flag = IsGuardSatisfied(children[i], transition);
			if (_pendingCallbackException != null)
			{
				return false;
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private bool EvaluateCallbackGuard(CompiledStateMachineGuard guard, CompiledStateMachineTransition transition)
	{
		StateMachineCallbackBinding callbackBinding = _callbackBinding;
		StateMachineGuardCallback stateMachineGuardCallback = callbackBinding?.GetGuard(guard.CallbackIndex);
		if (stateMachineGuardCallback == null)
		{
			return false;
		}
		try
		{
			StateMachineGuardContext context = new StateMachineGuardContext(callbackBinding.GetHost(), this, _program.States[transition.SourceIndex].StableId, _program.States[transition.TargetIndex].StableId, transition.EventName);
			return stateMachineGuardCallback(in context);
		}
		catch (Exception exception)
		{
			CaptureCallbackException(exception);
			return false;
		}
	}

	private static bool CompareGuardValues(Variant actual, Variant expected, StateMachineComparisonOperator comparisonOperator)
	{
		switch (comparisonOperator)
		{
		case StateMachineComparisonOperator.Equal:
			return actual.Equals(expected);
		case StateMachineComparisonOperator.NotEqual:
			return !actual.Equals(expected);
		default:
		{
			Variant.Type variantType = actual.VariantType;
			bool flag = (((ulong)(variantType - 2) <= 1uL) ? true : false);
			bool flag2 = flag;
			if (flag2)
			{
				Variant.Type variantType2 = expected.VariantType;
				bool flag3 = (((ulong)(variantType2 - 2) <= 1uL) ? true : false);
				flag2 = flag3;
			}
			int num = ((!flag2) ? string.Compare(actual.ToString(), expected.ToString(), StringComparison.Ordinal) : actual.AsDouble().CompareTo(expected.AsDouble()));
			return comparisonOperator switch
			{
				StateMachineComparisonOperator.Less => num < 0, 
				StateMachineComparisonOperator.LessOrEqual => num <= 0, 
				StateMachineComparisonOperator.Greater => num > 0, 
				StateMachineComparisonOperator.GreaterOrEqual => num >= 0, 
				_ => false, 
			};
		}
		}
	}

	private void ExecuteTransition(int transitionIndex, bool emitCallbacks, bool incrementRevision)
	{
		CompiledStateMachineTransition value = _program.Transitions[transitionIndex];
		if (emitCallbacks)
		{
			InvokeCallback(this.TransitionTaken, value);
		}
		if (_disposeRequested)
		{
			return;
		}
		int num = ResolveHistoryTarget(value.TargetIndex);
		int num2 = FindCommonAncestor(value.SourceIndex, num);
		if (num2 == num)
		{
			num2 = _program.States[num2].ParentIndex;
		}
		ExitTransitionConfiguration(value.SourceIndex, num2, emitCallbacks);
		int num3 = 0;
		int num4 = num;
		while (num4 >= 0 && num4 != num2)
		{
			_pathScratch[num3++] = num4;
			num4 = _program.States[num4].ParentIndex;
		}
		int num5 = num3 - 1;
		while (num5 >= 0 && !_disposeRequested)
		{
			EnterState(_pathScratch[num5], emitCallbacks);
			num5--;
		}
		if (!_disposeRequested)
		{
			EnterInitialDescendants(num, emitCallbacks);
			RefreshActiveLeaf();
			if (incrementRevision)
			{
				IncrementRevision();
			}
			if (emitCallbacks && !_disposeRequested)
			{
				InvokeCallback(this.TransitionCompleted, value);
			}
		}
	}

	private void ExecuteFlatTransition(int transitionIndex, int currentLeafIndex, int targetStateIndex)
	{
		CompiledStateMachineTransition value = _program.Transitions[transitionIndex];
		InvokeCallback(this.TransitionTaken, value);
		if (_disposeRequested)
		{
			return;
		}
		ExitState(currentLeafIndex, emitCallbacks: true);
		if (!_disposeRequested)
		{
			EnterState(targetStateIndex, emitCallbacks: true);
			if (!_disposeRequested)
			{
				_activeLeafIndices[0] = targetStateIndex;
				_activeLeafCount = 1;
				RefreshActiveCallbackFastPaths();
				IncrementRevision();
				InvokeCallback(this.TransitionCompleted, value);
			}
		}
	}

	private void ExitTransitionConfiguration(int sourceIndex, int commonAncestor, bool emitCallbacks)
	{
		int num = _program.States[sourceIndex].Depth;
		for (int i = 0; i < _program.StateCount; i++)
		{
			if (_active[i] && IsDescendantOrSelf(i, sourceIndex))
			{
				num = Math.Max(num, _program.States[i].Depth);
			}
		}
		int num2 = num;
		while (num2 >= 0 && !_disposeRequested)
		{
			for (int j = 0; j < _program.StateCount; j++)
			{
				if (_disposeRequested)
				{
					break;
				}
				if (_active[j] && _program.States[j].Depth == num2 && j != commonAncestor && IsDescendantOrSelf(j, sourceIndex))
				{
					ExitState(j, emitCallbacks);
				}
			}
			num2--;
		}
		int parentIndex = _program.States[sourceIndex].ParentIndex;
		while (parentIndex >= 0 && parentIndex != commonAncestor && !_disposeRequested)
		{
			ExitState(parentIndex, emitCallbacks);
			parentIndex = _program.States[parentIndex].ParentIndex;
		}
	}

	private bool IsDescendantOrSelf(int candidate, int ancestor)
	{
		for (int num = candidate; num >= 0; num = _program.States[num].ParentIndex)
		{
			if (num == ancestor)
			{
				return true;
			}
		}
		return false;
	}

	private void IncrementRevision()
	{
		if (Revision < 9223372036854775807L)
		{
			Revision++;
		}
		IncrementSnapshotRevision();
	}

	private void IncrementSnapshotRevision()
	{
		if (SnapshotRevision < 9223372036854775807L)
		{
			SnapshotRevision++;
		}
	}

	private int FindCommonAncestor(int sourceIndex, int targetIndex)
	{
		int num = sourceIndex;
		int num2 = targetIndex;
		int num3 = _program.States[num].Depth;
		int num4 = _program.States[num2].Depth;
		while (num3 > num4)
		{
			num = _program.States[num].ParentIndex;
			num3--;
		}
		while (num4 > num3)
		{
			num2 = _program.States[num2].ParentIndex;
			num4--;
		}
		while (num != num2)
		{
			num = _program.States[num].ParentIndex;
			num2 = _program.States[num2].ParentIndex;
		}
		return num;
	}

	private int FindActiveLeafUnder(int sourceIndex)
	{
		for (int i = 0; i < _activeLeafCount; i++)
		{
			int num = _activeLeafIndices[i];
			for (int num2 = num; num2 >= 0; num2 = _program.States[num2].ParentIndex)
			{
				if (num2 == sourceIndex)
				{
					return num;
				}
			}
		}
		return -1;
	}

	private void EnterStateAndInitialDescendants(int stateIndex)
	{
		EnterState(stateIndex);
		if (!_disposeRequested)
		{
			EnterInitialDescendants(stateIndex);
			RefreshActiveLeaf();
		}
	}

	private void EnterInitialDescendants(int stateIndex)
	{
		EnterInitialDescendants(stateIndex, emitCallbacks: true);
	}

	private void EnterInitialDescendants(int stateIndex, bool emitCallbacks)
	{
		if (_disposeRequested)
		{
			return;
		}
		CompiledStateMachineState compiledStateMachineState = _program.States[stateIndex];
		if (compiledStateMachineState.Kind == StateMachineStateKind.Parallel)
		{
			for (int i = 0; i < _program.StateCount; i++)
			{
				if (_disposeRequested)
				{
					break;
				}
				CompiledStateMachineState compiledStateMachineState2 = _program.States[i];
				if (compiledStateMachineState2.ParentIndex == stateIndex && compiledStateMachineState2.Kind != StateMachineStateKind.History)
				{
					EnterState(i, emitCallbacks);
					EnterInitialDescendants(i, emitCallbacks);
				}
			}
		}
		else
		{
			int num = ResolveHistoryTarget(compiledStateMachineState.InitialChildIndex);
			if (num >= 0 && num != stateIndex)
			{
				EnterState(num, emitCallbacks);
				EnterInitialDescendants(num, emitCallbacks);
			}
		}
	}

	private int ResolveHistoryTarget(int stateIndex)
	{
		if ((uint)stateIndex >= (uint)_program.StateCount || _program.States[stateIndex].Kind != StateMachineStateKind.History)
		{
			return stateIndex;
		}
		int parentIndex = _program.States[stateIndex].ParentIndex;
		if (parentIndex < 0)
		{
			return stateIndex;
		}
		int num = _historySlots[parentIndex];
		if ((uint)num < (uint)_program.StateCount && num != stateIndex)
		{
			return num;
		}
		int initialChildIndex = _program.States[parentIndex].InitialChildIndex;
		if ((uint)initialChildIndex < (uint)_program.StateCount && initialChildIndex != stateIndex)
		{
			return initialChildIndex;
		}
		for (int i = 0; i < _program.StateCount; i++)
		{
			if (_program.States[i].ParentIndex == parentIndex && _program.States[i].Kind != StateMachineStateKind.History)
			{
				return i;
			}
		}
		return parentIndex;
	}

	private void EnterState(int stateIndex)
	{
		EnterState(stateIndex, emitCallbacks: true);
	}

	private void EnterState(int stateIndex, bool emitCallbacks)
	{
		if (!_active[stateIndex])
		{
			_active[stateIndex] = true;
			AddActiveCallbackState(stateIndex);
			_activeConfigurationVersion++;
			if (_automaticDelayConsumed.Length != 0)
			{
				_automaticDelayConsumed[stateIndex] = false;
			}
			if (emitCallbacks)
			{
				EmitEntered(stateIndex);
			}
		}
	}

	private void ExitState(int stateIndex)
	{
		ExitState(stateIndex, emitCallbacks: true);
	}

	private void ExitState(int stateIndex, bool emitCallbacks)
	{
		if (_active[stateIndex])
		{
			int parentIndex = _program.States[stateIndex].ParentIndex;
			if (_historySlots.Length != 0 && parentIndex >= 0 && _program.States[stateIndex].Kind != StateMachineStateKind.History)
			{
				_historySlots[parentIndex] = stateIndex;
			}
			ClearPending(stateIndex);
			_active[stateIndex] = false;
			RemoveActiveCallbackState(stateIndex);
			_activeConfigurationVersion++;
			if (emitCallbacks)
			{
				EmitExited(stateIndex);
			}
		}
	}

	private void ExitActiveConfiguration(bool emitCallbacks)
	{
		int num = 0;
		for (int i = 0; i < _program.StateCount; i++)
		{
			num = Math.Max(num, _program.States[i].Depth);
		}
		int num2 = num;
		while (num2 >= 0 && !_disposeRequested)
		{
			for (int j = 0; j < _program.StateCount; j++)
			{
				if (_disposeRequested)
				{
					break;
				}
				if (_active[j] && _program.States[j].Depth == num2)
				{
					ExitState(j, emitCallbacks);
				}
			}
			num2--;
		}
		Array.Fill(_pendingTransitionBySource, -1);
		Array.Clear(_pendingDelayRemaining);
		Array.Clear(_pendingDelayInitial);
		_pendingTransitionCount = 0;
		_activeLeafCount = 0;
	}

	private int ResolveSnapshotTarget(StateMachineSnapshot snapshot)
	{
		int num = _program.RootStateIndex;
		int num2 = _program.States[num].Depth;
		if (snapshot.ActiveStateIds == null)
		{
			return num;
		}
		for (int i = 0; i < snapshot.ActiveStateIds.Count; i++)
		{
			if (_program.TryResolveStateIndex(snapshot.ActiveStateIds[i], out var stateIndex))
			{
				int depth = _program.States[stateIndex].Depth;
				if (depth > num2)
				{
					num = stateIndex;
					num2 = depth;
				}
			}
		}
		return num;
	}

	private void EnterPathAndInitialDescendants(int targetStateIndex, bool emitCallbacks)
	{
		int num = 0;
		for (int num2 = targetStateIndex; num2 >= 0; num2 = _program.States[num2].ParentIndex)
		{
			_pathScratch[num++] = num2;
		}
		int num3 = num - 1;
		while (num3 >= 0 && !_disposeRequested)
		{
			EnterState(_pathScratch[num3], emitCallbacks);
			num3--;
		}
		if (!_disposeRequested)
		{
			EnterInitialDescendants(targetStateIndex, emitCallbacks);
		}
	}

	private void RestorePendingTransitions(StateMachineSnapshot snapshot)
	{
		if (_pendingTransitionBySource.Length == 0 || snapshot.PendingTransitionIds == null || snapshot.PendingDelayRemaining == null)
		{
			return;
		}
		for (int i = 0; i < snapshot.PendingTransitionIds.Count; i++)
		{
			string text = snapshot.PendingTransitionIds[i];
			int num = FindTransitionIndex(text);
			if (num < 0 || !snapshot.PendingDelayRemaining.TryGetValue(text, out var value) || !double.IsFinite(value) || value < 0.0)
			{
				continue;
			}
			CompiledStateMachineTransition compiledStateMachineTransition = _program.Transitions[num];
			if (!(compiledStateMachineTransition.DelaySeconds <= 0.0) && !(value > compiledStateMachineTransition.DelaySeconds) && _active[compiledStateMachineTransition.SourceIndex] && _pendingTransitionBySource[compiledStateMachineTransition.SourceIndex] < 0)
			{
				_pendingTransitionBySource[compiledStateMachineTransition.SourceIndex] = num;
				_pendingDelayRemaining[compiledStateMachineTransition.SourceIndex] = value;
				_pendingDelayInitial[compiledStateMachineTransition.SourceIndex] = compiledStateMachineTransition.DelaySeconds;
				_pendingTransitionCount++;
				StateMachineTriggerKind triggerKind = compiledStateMachineTransition.TriggerKind;
				if ((uint)(triggerKind - 1) <= 1u)
				{
					_automaticDelayConsumed[compiledStateMachineTransition.SourceIndex] = true;
				}
			}
		}
	}

	private int FindTransitionIndex(string stableId)
	{
		for (int i = 0; i < _program.TransitionCount; i++)
		{
			if (string.Equals(_program.Transitions[i].StableId, stableId, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private void RestoreExpressionProperties(StateMachineSnapshot snapshot)
	{
		_expressionProperties?.Clear();
		if (snapshot.ExpressionProperties == null)
		{
			return;
		}
		foreach (StringName key in snapshot.ExpressionProperties.Keys)
		{
			Variant value = snapshot.ExpressionProperties[key];
			if (StateMachineSnapshot.IsSupportedExpressionVariant(value.VariantType))
			{
				if (_expressionProperties == null)
				{
					_expressionProperties = new Dictionary<StringName, Variant>();
				}
				_expressionProperties[key] = value;
			}
		}
	}

	private void EmitSnapshotMismatchDiagnostic()
	{
		LastDiagnosticCode = "SMS001";
		BeginDispatch();
		BeginTransaction();
		try
		{
			InvokeCallback(this.Diagnostic, "SMS001");
			ClearEventQueue();
		}
		finally
		{
			_transactionActive = false;
			EndDispatch();
		}
		ThrowPendingCallbackException();
	}

	private void RefreshActiveLeaf()
	{
		_activeLeafCount = 0;
		for (int i = 0; i < _active.Length; i++)
		{
			if (!_active[i])
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < _active.Length; j++)
			{
				if (flag)
				{
					break;
				}
				flag = _active[j] && _program.States[j].ParentIndex == i;
			}
			if (!flag)
			{
				_activeLeafIndices[_activeLeafCount++] = i;
			}
		}
		RefreshActiveCallbackFastPaths();
	}

	private void RefreshActiveCallbackFastPaths()
	{
		RefreshProcessSingleStateCache();
		RefreshPhysicsSingleStateCache();
	}

	private void RefreshProcessSingleStateCache()
	{
		if (_activeProcessStateCount == 1)
		{
			int num = (_singleActiveProcessStateIndex = _activeProcessStateIndices[0]);
			_singleActiveProcessStateOrder = _processStateOrders[num];
		}
		else
		{
			_singleActiveProcessStateIndex = -1;
			_singleActiveProcessStateOrder = -1;
		}
	}

	private void RefreshPhysicsSingleStateCache()
	{
		if (_activePhysicsStateCount == 1)
		{
			int num = (_singleActivePhysicsStateIndex = _activePhysicsStateIndices[0]);
			_singleActivePhysicsStateOrder = _physicsStateOrders[num];
		}
		else
		{
			_singleActivePhysicsStateIndex = -1;
			_singleActivePhysicsStateOrder = -1;
		}
	}

	private void AddActiveCallbackState(int stateIndex)
	{
		int num = _processStateOrders[stateIndex];
		if (num >= 0)
		{
			InsertActiveProcessState(stateIndex, num);
		}
		int num2 = _physicsStateOrders[stateIndex];
		if (num2 >= 0)
		{
			InsertActivePhysicsState(stateIndex, num2);
		}
	}

	private void RemoveActiveCallbackState(int stateIndex)
	{
		if (_processStateOrders[stateIndex] >= 0)
		{
			RemoveActiveProcessState(stateIndex);
		}
		if (_physicsStateOrders[stateIndex] >= 0)
		{
			RemoveActivePhysicsState(stateIndex);
		}
	}

	private void InsertActiveProcessState(int stateIndex, int stateOrder)
	{
		if (_activeProcessStatePositions[stateIndex] >= 0)
		{
			return;
		}
		int num;
		for (num = _activeProcessStateCount; num > 0; num--)
		{
			int num2 = _activeProcessStateIndices[num - 1];
			if (_processStateOrders[num2] <= stateOrder)
			{
				break;
			}
			_activeProcessStateIndices[num] = num2;
			_activeProcessStatePositions[num2] = num;
		}
		_activeProcessStateIndices[num] = stateIndex;
		_activeProcessStatePositions[stateIndex] = num;
		_activeProcessStateCount++;
		RefreshProcessSingleStateCache();
	}

	private void RemoveActiveProcessState(int stateIndex)
	{
		int num = _activeProcessStatePositions[stateIndex];
		if (num >= 0 && num < _activeProcessStateCount)
		{
			for (int i = num + 1; i < _activeProcessStateCount; i++)
			{
				int num2 = _activeProcessStateIndices[i];
				_activeProcessStateIndices[i - 1] = num2;
				_activeProcessStatePositions[num2] = i - 1;
			}
			_activeProcessStateCount--;
			_activeProcessStatePositions[stateIndex] = -1;
			RefreshProcessSingleStateCache();
		}
	}

	private void InsertActivePhysicsState(int stateIndex, int stateOrder)
	{
		if (_activePhysicsStatePositions[stateIndex] >= 0)
		{
			return;
		}
		int num;
		for (num = _activePhysicsStateCount; num > 0; num--)
		{
			int num2 = _activePhysicsStateIndices[num - 1];
			if (_physicsStateOrders[num2] <= stateOrder)
			{
				break;
			}
			_activePhysicsStateIndices[num] = num2;
			_activePhysicsStatePositions[num2] = num;
		}
		_activePhysicsStateIndices[num] = stateIndex;
		_activePhysicsStatePositions[stateIndex] = num;
		_activePhysicsStateCount++;
		RefreshPhysicsSingleStateCache();
	}

	private void RemoveActivePhysicsState(int stateIndex)
	{
		int num = _activePhysicsStatePositions[stateIndex];
		if (num >= 0 && num < _activePhysicsStateCount)
		{
			for (int i = num + 1; i < _activePhysicsStateCount; i++)
			{
				int num2 = _activePhysicsStateIndices[i];
				_activePhysicsStateIndices[i - 1] = num2;
				_activePhysicsStatePositions[num2] = i - 1;
			}
			_activePhysicsStateCount--;
			_activePhysicsStatePositions[stateIndex] = -1;
			RefreshPhysicsSingleStateCache();
		}
	}

	private int FindFirstActiveProcessPositionAfter(int stateOrder)
	{
		int num = 0;
		int num2 = _activeProcessStateCount;
		while (num < num2)
		{
			int num3 = num + (num2 - num >> 1);
			int num4 = _activeProcessStateIndices[num3];
			if (_processStateOrders[num4] <= stateOrder)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private int FindFirstActivePhysicsPositionAfter(int stateOrder)
	{
		int num = 0;
		int num2 = _activePhysicsStateCount;
		while (num < num2)
		{
			int num3 = num + (num2 - num >> 1);
			int num4 = _activePhysicsStateIndices[num3];
			if (_physicsStateOrders[num4] <= stateOrder)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private void DispatchProcessCallbacksAfter(int stateOrder, double delta, bool detailedCallbacks)
	{
		int num = FindFirstActiveProcessPositionAfter(stateOrder);
		long activeConfigurationVersion = _activeConfigurationVersion;
		while (num < _activeProcessStateCount && !_disposeRequested)
		{
			int num2 = _activeProcessStateIndices[num];
			int stateOrder2 = _processStateOrders[num2];
			if (_active[num2])
			{
				if (detailedCallbacks)
				{
					EmitProcessingDetailed(num2, delta);
				}
				else
				{
					EmitProcessing(num2, delta);
				}
			}
			if (_activeConfigurationVersion == activeConfigurationVersion)
			{
				num++;
				continue;
			}
			activeConfigurationVersion = _activeConfigurationVersion;
			num = FindFirstActiveProcessPositionAfter(stateOrder2);
		}
	}

	private void DispatchPhysicsCallbacksAfter(int stateOrder, double delta, bool detailedCallbacks)
	{
		int num = FindFirstActivePhysicsPositionAfter(stateOrder);
		long activeConfigurationVersion = _activeConfigurationVersion;
		while (num < _activePhysicsStateCount && !_disposeRequested)
		{
			int num2 = _activePhysicsStateIndices[num];
			int stateOrder2 = _physicsStateOrders[num2];
			if (_active[num2])
			{
				if (detailedCallbacks)
				{
					EmitPhysicsProcessingDetailed(num2, delta);
				}
				else
				{
					EmitPhysicsProcessing(num2, delta);
				}
			}
			if (_activeConfigurationVersion == activeConfigurationVersion)
			{
				num++;
				continue;
			}
			activeConfigurationVersion = _activeConfigurationVersion;
			num = FindFirstActivePhysicsPositionAfter(stateOrder2);
		}
	}

	private bool HasAnyActiveState()
	{
		for (int i = 0; i < _active.Length; i++)
		{
			if (_active[i])
			{
				return true;
			}
		}
		return false;
	}

	private void SchedulePending(int transitionIndex, CompiledStateMachineTransition transition)
	{
		if (_pendingTransitionBySource[transition.SourceIndex] != transitionIndex || !(Math.Abs(_pendingDelayRemaining[transition.SourceIndex] - transition.DelaySeconds) < 1E-07))
		{
			if (_pendingTransitionBySource[transition.SourceIndex] < 0)
			{
				_pendingTransitionCount++;
			}
			_pendingTransitionBySource[transition.SourceIndex] = transitionIndex;
			_pendingDelayRemaining[transition.SourceIndex] = transition.DelaySeconds;
			_pendingDelayInitial[transition.SourceIndex] = transition.DelaySeconds;
			IncrementSnapshotRevision();
		}
	}

	private void ClearPending(int sourceIndex)
	{
		if (_pendingTransitionBySource.Length != 0)
		{
			bool num = _pendingTransitionBySource[sourceIndex] >= 0;
			_pendingTransitionBySource[sourceIndex] = -1;
			_pendingDelayRemaining[sourceIndex] = 0.0;
			_pendingDelayInitial[sourceIndex] = 0.0;
			if (num)
			{
				_pendingTransitionCount = Math.Max(0, _pendingTransitionCount - 1);
				IncrementSnapshotRevision();
			}
		}
	}

	private bool EnqueueEvent(StringName eventName)
	{
		if (_eventCount >= 1024 || (_transactionActive && _transactionAcceptedEvents >= 1024))
		{
			EmitBudgetDiagnostic();
			return false;
		}
		if (_eventQueue.Length == 0)
		{
			_eventQueue = new StringName[8];
			_eventHead = 0;
		}
		else if (_eventCount == _eventQueue.Length)
		{
			StringName[] array = new StringName[_eventQueue.Length * 2];
			for (int i = 0; i < _eventCount; i++)
			{
				array[i] = _eventQueue[(_eventHead + i) % _eventQueue.Length];
			}
			_eventQueue = array;
			_eventHead = 0;
		}
		_eventQueue[(_eventHead + _eventCount) % _eventQueue.Length] = eventName;
		_eventCount++;
		if (_transactionActive)
		{
			_transactionAcceptedEvents++;
		}
		return true;
	}

	private StringName DequeueEvent()
	{
		StringName result = _eventQueue[_eventHead];
		_eventQueue[_eventHead] = null;
		_eventHead = (_eventHead + 1) % _eventQueue.Length;
		_eventCount--;
		return result;
	}

	private void ClearEventQueue()
	{
		while (_eventCount > 0)
		{
			DequeueEvent();
		}
	}

	private void EmitBudgetDiagnostic()
	{
		if (!_budgetDiagnosticEmitted)
		{
			_budgetDiagnosticEmitted = true;
			LastDiagnosticCode = "SMR001";
			InvokeCallback(this.Diagnostic, "SMR001");
		}
	}

	private void DisposeStorage()
	{
		if (_handles != null)
		{
			for (int i = 0; i < _handles.Length; i++)
			{
				_handles[i]?.Invalidate();
			}
		}
		_active = null;
		_activeLeafIndices = null;
		_activeProcessStateIndices = null;
		_activeProcessStatePositions = null;
		_processStateOrders = null;
		_activePhysicsStateIndices = null;
		_activePhysicsStatePositions = null;
		_physicsStateOrders = null;
		_pendingTransitionBySource = null;
		_pendingTransitionCount = 0;
		_pendingDelayRemaining = null;
		_pendingDelayInitial = null;
		_automaticDelayConsumed = null;
		_historySlots = null;
		_pathScratch = null;
		_handles = null;
		_eventQueue = null;
		_expressionProperties?.Clear();
		_expressionProperties = null;
		_processMetricNames = null;
		_physicsMetricNames = null;
		_callbackBinding = null;
		_eventHead = 0;
		_eventCount = 0;
		_activeLeafCount = 0;
		_activeProcessStateCount = 0;
		_singleActiveProcessStateIndex = -1;
		_singleActiveProcessStateOrder = -1;
		_activePhysicsStateCount = 0;
		_singleActivePhysicsStateIndex = -1;
		_singleActivePhysicsStateOrder = -1;
		_activeConfigurationVersion = 0L;
		_transactionActive = false;
	}

	private void BeginTransaction()
	{
		_transactionActive = true;
		_transactionAcceptedEvents = _eventCount;
		_budgetDiagnosticEmitted = false;
	}

	private void BeginDispatch()
	{
		_dispatchDepth++;
	}

	private void EndDispatch()
	{
		_dispatchDepth--;
		if (_dispatchDepth == 0 && _disposeRequested)
		{
			_disposeRequested = false;
			DisposeNow();
		}
	}

	private void EmitEntered(int stateIndex)
	{
		StateMachineCallbackBinding callbackBinding = _callbackBinding;
		StateMachineLifecycleCallback stateMachineLifecycleCallback = callbackBinding?.GetEnter(stateIndex);
		if (stateMachineLifecycleCallback != null)
		{
			try
			{
				stateMachineLifecycleCallback(new StateMachineCallbackContext(callbackBinding.GetHost(), this, GetOrCreateStateHandle(stateIndex)));
			}
			catch (Exception exception)
			{
				CaptureCallbackException(exception);
			}
		}
		try
		{
			StateHandle[] handles = _handles;
			if (handles != null)
			{
				handles[stateIndex]?.EmitEntered();
			}
		}
		catch (Exception exception2)
		{
			CaptureCallbackException(exception2);
		}
	}

	private void EmitExited(int stateIndex)
	{
		StateMachineCallbackBinding callbackBinding = _callbackBinding;
		StateMachineLifecycleCallback stateMachineLifecycleCallback = callbackBinding?.GetExit(stateIndex);
		if (stateMachineLifecycleCallback != null)
		{
			try
			{
				stateMachineLifecycleCallback(new StateMachineCallbackContext(callbackBinding.GetHost(), this, GetOrCreateStateHandle(stateIndex)));
			}
			catch (Exception exception)
			{
				CaptureCallbackException(exception);
			}
		}
		try
		{
			StateHandle[] handles = _handles;
			if (handles != null)
			{
				handles[stateIndex]?.EmitExited();
			}
		}
		catch (Exception exception2)
		{
			CaptureCallbackException(exception2);
		}
	}

	private void EmitProcessing(int stateIndex, double delta)
	{
		StateMachineCallbackBinding callbackBinding = _callbackBinding;
		StateMachineDeltaCallback stateMachineDeltaCallback = callbackBinding?.GetProcess(stateIndex);
		if (stateMachineDeltaCallback != null)
		{
			try
			{
				stateMachineDeltaCallback(new StateMachineCallbackContext(callbackBinding.GetHost(), this, GetOrCreateStateHandle(stateIndex)), delta);
			}
			catch (Exception exception)
			{
				CaptureCallbackException(exception);
			}
		}
		try
		{
			StateHandle[] handles = _handles;
			if (handles != null)
			{
				handles[stateIndex]?.EmitProcessing(delta);
			}
		}
		catch (Exception exception2)
		{
			CaptureCallbackException(exception2);
		}
	}

	private void EmitPhysicsProcessing(int stateIndex, double delta)
	{
		StateMachineCallbackBinding callbackBinding = _callbackBinding;
		StateMachineDeltaCallback stateMachineDeltaCallback = callbackBinding?.GetPhysicsProcess(stateIndex);
		if (stateMachineDeltaCallback != null)
		{
			try
			{
				stateMachineDeltaCallback(new StateMachineCallbackContext(callbackBinding.GetHost(), this, GetOrCreateStateHandle(stateIndex)), delta);
			}
			catch (Exception exception)
			{
				CaptureCallbackException(exception);
			}
		}
		try
		{
			StateHandle[] handles = _handles;
			if (handles != null)
			{
				handles[stateIndex]?.EmitPhysicsProcessing(delta);
			}
		}
		catch (Exception exception2)
		{
			CaptureCallbackException(exception2);
		}
	}

	private void EmitProcessingDetailed(int stateIndex, double delta)
	{
		EnsureDetailedMetricNames();
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		try
		{
			EmitProcessing(stateIndex, delta);
		}
		finally
		{
			TowerDefensePerfProfiler.End(_processMetricNames[stateIndex], startTicks);
		}
	}

	private void EmitPhysicsProcessingDetailed(int stateIndex, double delta)
	{
		EnsureDetailedMetricNames();
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		try
		{
			EmitPhysicsProcessing(stateIndex, delta);
		}
		finally
		{
			TowerDefensePerfProfiler.End(_physicsMetricNames[stateIndex], startTicks);
		}
	}

	private void EnsureDetailedMetricNames()
	{
		if (_processMetricNames == null || _physicsMetricNames == null)
		{
			_processMetricNames = new string[_program.StateCount];
			_physicsMetricNames = new string[_program.StateCount];
			for (int i = 0; i < _program.StateCount; i++)
			{
				CompiledStateMachineState compiledStateMachineState = _program.States[i];
				string text = ((compiledStateMachineState.CallbackKey == null || compiledStateMachineState.CallbackKey == (StringName)"") ? compiledStateMachineState.StableId : compiledStateMachineState.CallbackKey.ToString());
				_processMetricNames[i] = "stateProcess." + text;
				_physicsMetricNames[i] = "statePhysics." + text;
			}
		}
	}

	private void InvokeCallback<T>(Action<T> callback, T value)
	{
		if (callback == null)
		{
			return;
		}
		try
		{
			callback(value);
		}
		catch (Exception exception)
		{
			CaptureCallbackException(exception);
		}
	}

	private void CaptureCallbackException(Exception exception)
	{
		if (_pendingCallbackException == null)
		{
			_pendingCallbackException = ExceptionDispatchInfo.Capture(exception);
		}
	}

	private void ThrowPendingCallbackException()
	{
		if (_dispatchDepth <= 0 && _pendingCallbackException != null)
		{
			ExceptionDispatchInfo pendingCallbackException = _pendingCallbackException;
			_pendingCallbackException = null;
			pendingCallbackException.Throw();
		}
	}
}
