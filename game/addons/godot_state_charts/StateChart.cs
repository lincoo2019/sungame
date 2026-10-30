using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
[Icon("res://addons/godot_state_charts/state_chart.svg")]
[Tool]
public partial class StateChart : Node
{
	[Flags]
	public enum TriggerType
	{
		None = 0,
		Event = 1,
		StateEnter = 2,
		PropertyChange = 4,
		StateChange = 8
	}

	public delegate void EventReceivedEventHandler(StringName eventName);

	private struct PendingTransition
	{
		public Transition transition;

		public StateChartState source;
	}

	private bool _trackInEditor;

	private bool _warnOnSendingUnknownEvents = true;

	private Dictionary _initialExpressionProperties = new Dictionary();

	private StateChartState _state;

	public Dictionary _expressionProperties = new Dictionary();

	private List<StringName> _queuedEvents = new List<StringName>();

	private bool _propertyChangePending;

	private bool _stateChangePending;

	private bool _lockedDown;

	private bool _transitionsProcessingActive;

	private readonly List<PendingTransition> _pendingTransitions = new List<PendingTransition>();

	private HashSet<StringName> _validEventNames = new HashSet<StringName>();

	private StateMachineController _resourceController;

	private bool _resourceBatchRegistered;

	[Export(PropertyHint.None, "")]
	public bool trackInEditor
	{
		get
		{
			return _trackInEditor;
		}
		set
		{
			_trackInEditor = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public bool warnOnSendingUnknownEvents
	{
		get
		{
			return _warnOnSendingUnknownEvents;
		}
		set
		{
			_warnOnSendingUnknownEvents = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public Dictionary initialExpressionProperties
	{
		get
		{
			return _initialExpressionProperties;
		}
		set
		{
			_initialExpressionProperties = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public StateMachineRuntimeMode RuntimeMode { get; set; }

	[Export(PropertyHint.None, "")]
	public StateMachineDefinition Definition { get; set; }

	public bool IsResourceMode { get; private set; }

	public bool ResourceInitializationSucceeded
	{
		get
		{
			if (IsResourceMode)
			{
				return _resourceController?.IsInitialized ?? false;
			}
			return false;
		}
	}

	public string InitializationError { get; private set; } = string.Empty;

	public StateHandle CurrentStateHandle
	{
		get
		{
			if (!IsResourceMode)
			{
				return null;
			}
			return _resourceController?.CurrentStateHandle;
		}
	}

	public long StateRevision
	{
		get
		{
			if (!IsResourceMode)
			{
				return 0L;
			}
			return _resourceController?.Revision ?? 0;
		}
	}

	internal StateMachineRuntime ResourceRuntime => _resourceController?.Runtime;

	public bool InitializationDisabled { get; set; }

	public StateChartState CurrentState
	{
		get
		{
			if (IsResourceMode)
			{
				return null;
			}
			if (_state == null || !GodotObject.IsInstanceValid(_state))
			{
				return null;
			}
			return _state;
		}
	}

	public event EventReceivedEventHandler OnEventReceived;

	public void EmitEventReceived(StringName eventName)
	{
		this.OnEventReceived?.Invoke(eventName);
	}

	public override void _EnterTree()
	{
		if (_resourceController?.Runtime != null && !_resourceBatchRegistered && !RegisterResourceRuntimeBatch())
		{
			ReportInitializationError("Unable to resume compact StateChart runtime batching.");
		}
	}

	public override void _Ready()
	{
		if (Engine.IsEditorHint() || InitializationDisabled)
		{
			return;
		}
		InitializeExpressionProperties();
		if (RuntimeMode == StateMachineRuntimeMode.Legacy)
		{
			InitializeLegacy();
		}
		else if (RuntimeMode == StateMachineRuntimeMode.Resource)
		{
			if (Definition == null)
			{
				IsResourceMode = true;
				DisableLegacyStateNodes(this);
				ReportInitializationError("Resource StateChart requires a StateMachineDefinition.");
			}
			else
			{
				InitializeResourceRuntime();
			}
		}
		else if (Definition != null)
		{
			InitializeResourceRuntime();
		}
		else
		{
			InitializeLegacy();
		}
	}

	private void InitializeLegacy()
	{
		IsResourceMode = false;
		InitializationError = string.Empty;
		if (GetChildCount() != 1)
		{
			GD.PushError("StateChart must have exactly one child");
			return;
		}
		Node child = GetChild(0);
		if (!(child is StateChartState))
		{
			GD.PushError("StateMachine's child must be a State");
			return;
		}
		if (OS.IsDebugBuild())
		{
			_validEventNames = new HashSet<StringName>(StateChartUtil.EventsOf(this));
		}
		_state = child as StateChartState;
		_state._StateInit();
		Callable.From(_EnterInitialState).CallDeferred();
	}

	private void InitializeExpressionProperties()
	{
		if (_initialExpressionProperties == null)
		{
			return;
		}
		foreach (Variant key in _initialExpressionProperties.Keys)
		{
			if (key.VariantType != Variant.Type.String && key.VariantType != Variant.Type.StringName)
			{
				GD.PushError("Expression property names must be strings. Ignoring initial expression property with key ", key);
			}
			else
			{
				_expressionProperties[key] = _initialExpressionProperties[key];
			}
		}
	}

	private void InitializeResourceRuntime()
	{
		IsResourceMode = true;
		InitializationError = string.Empty;
		DisableLegacyStateNodes(this);
		try
		{
			_resourceController = new StateMachineController();
			_resourceController.EventReceived += EmitEventReceived;
			_resourceController.Diagnostic += OnResourceDiagnostic;
			if (!_resourceController.Initialize(Definition, this))
			{
				throw new InvalidOperationException(_resourceController.InitializationError);
			}
			CopyExpressionPropertiesToResourceRuntime();
			BuildResourceEventNames(_resourceController.Runtime.Program);
			if (!RegisterResourceRuntimeBatch())
			{
				throw new InvalidOperationException("Unable to register compact StateChart runtime batching.");
			}
			Callable.From(_EnterInitialState).CallDeferred();
		}
		catch (Exception ex)
		{
			ReleaseResourceRuntime();
			ReportInitializationError(ex.Message);
		}
	}

	private void CopyExpressionPropertiesToResourceRuntime()
	{
		foreach (Variant key in _expressionProperties.Keys)
		{
			_resourceController.SetExpressionProperty(key.AsStringName(), _expressionProperties[key]);
		}
	}

	private void DisableLegacyStateNodes(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is StateChartState stateChartState)
			{
				stateChartState.ProcessMode = ProcessModeEnum.Disabled;
				stateChartState.SetProcess(enable: false);
				stateChartState.SetPhysicsProcess(enable: false);
				stateChartState.SetProcessInput(enable: false);
				stateChartState.SetProcessUnhandledInput(enable: false);
			}
			DisableLegacyStateNodes(child);
		}
	}

	private void BuildResourceEventNames(StateMachineProgram program)
	{
		_validEventNames.Clear();
		ReadOnlySpan<CompiledStateMachineTransition> transitions = program.Transitions;
		for (int i = 0; i < transitions.Length; i++)
		{
			StringName eventName = transitions[i].EventName;
			if (eventName != null && eventName != (StringName)"")
			{
				_validEventNames.Add(eventName);
			}
		}
	}

	private void OnResourceDiagnostic(string code)
	{
		GD.PushError("StateChart compact runtime diagnostic: ", code);
	}

	private void ReportInitializationError(string message)
	{
		InitializationError = message ?? "StateChart Resource initialization failed.";
		GD.PushError(InitializationError);
	}

	private bool RegisterResourceRuntimeBatch()
	{
		if (_resourceController?.Runtime == null)
		{
			return false;
		}
		if (_resourceBatchRegistered)
		{
			return true;
		}
		_resourceBatchRegistered = StateMachineRuntimeBatch.Register(this, _resourceController.Runtime, _resourceController.HasProcessWork, _resourceController.HasPhysicsWork);
		return _resourceBatchRegistered;
	}

	private void SuspendResourceRuntimeBatch()
	{
		if (_resourceBatchRegistered)
		{
			StateMachineRuntimeBatch.Unregister(this);
			_resourceBatchRegistered = false;
		}
	}

	private void _EnterInitialState()
	{
		if (IsResourceMode)
		{
			_resourceController?.EnterInitialState();
			return;
		}
		_transitionsProcessingActive = true;
		_lockedDown = true;
		_state._StateEnter(null);
		_RunQueuedTransitions();
		_RunChanges();
	}

	public void SendEvent(StringName eventName)
	{
		if (!IsNodeReady())
		{
			GD.PushError("State chart is not yet ready. If you call `SendEvent` in _Ready, please call it deferred.");
			return;
		}
		if (IsResourceMode)
		{
			if (_resourceController?.Runtime == null)
			{
				GD.PushError("State chart has no compact runtime. Ignoring call to `SendEvent`.");
				return;
			}
			WarnUnknownEvent(eventName);
			_resourceController.SendEvent(eventName);
			return;
		}
		if (!GodotObject.IsInstanceValid(_state))
		{
			GD.PushError("State chart has no root state. Ignoring call to `SendEvent`.");
			return;
		}
		WarnUnknownEvent(eventName);
		_queuedEvents.Add(eventName);
		if (!_lockedDown)
		{
			_RunChanges();
		}
	}

	private void WarnUnknownEvent(StringName eventName)
	{
		if (_warnOnSendingUnknownEvents && eventName != null && eventName != (StringName)"" && OS.IsDebugBuild() && !_validEventNames.Contains(eventName))
		{
			GD.PushWarning("State chart does not have an event '", eventName, "' defined. Sending this event will do nothing.");
		}
	}

	public void SetExpressionProperty(StringName name, Variant value)
	{
		if (!IsNodeReady())
		{
			GD.PushError("State chart is not yet ready. If you call `SetExpressionProperty` in `_Ready`, please call it deferred.");
			return;
		}
		if (IsResourceMode)
		{
			if (_resourceController?.Runtime == null)
			{
				GD.PushError("State chart has no compact runtime. Ignoring call to `SetExpressionProperty`.");
			}
			else
			{
				_resourceController.SetExpressionProperty(name, value);
			}
			return;
		}
		if (!GodotObject.IsInstanceValid(_state))
		{
			GD.PushError("State chart has no root state. Ignoring call to `SetExpressionProperty`.");
			return;
		}
		_expressionProperties[name] = value;
		_propertyChangePending = true;
		if (!_lockedDown)
		{
			_RunChanges();
		}
	}

	public Variant GetExpressionProperty(StringName name, Variant defaultValue = default(Variant))
	{
		if (IsResourceMode)
		{
			return _resourceController?.GetExpressionProperty(name, defaultValue) ?? defaultValue;
		}
		if (_expressionProperties.ContainsKey(name))
		{
			return _expressionProperties[name];
		}
		return defaultValue;
	}

	public StateHandle GetStateById(string stableId)
	{
		if (!IsResourceMode)
		{
			return null;
		}
		return _resourceController?.GetStateById(stableId);
	}

	public StateHandle GetState(string stableId)
	{
		return GetStateById(stableId);
	}

	public StateMachineSnapshot CaptureResourceSnapshot()
	{
		if (!IsResourceMode)
		{
			return null;
		}
		return _resourceController?.CaptureSnapshot();
	}

	public bool RestoreResourceSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = false)
	{
		if (IsResourceMode && _resourceController?.Runtime != null)
		{
			return _resourceController.RestoreSnapshot(snapshot, suppressEntryEffects);
		}
		return false;
	}

	public bool ApplyRemoteSnapshot(StateMachineSnapshot snapshot, bool suppressEntryEffects = true)
	{
		if (IsResourceMode && _resourceController?.Runtime != null)
		{
			return _resourceController.ApplyRemoteSnapshot(snapshot, suppressEntryEffects);
		}
		return false;
	}

	internal void _RunChanges()
	{
		_lockedDown = true;
		while (_queuedEvents.Count > 0 || _propertyChangePending || _stateChangePending)
		{
			if (_stateChangePending)
			{
				_stateChangePending = false;
				_state._ProcessTransitions(TriggerType.StateChange);
			}
			if (_propertyChangePending)
			{
				_propertyChangePending = false;
				_state._ProcessTransitions(TriggerType.PropertyChange);
			}
			if (_queuedEvents.Count > 0)
			{
				StringName stringName = _queuedEvents[0];
				_queuedEvents.RemoveAt(0);
				this.OnEventReceived?.Invoke(stringName);
				_state._ProcessTransitions(TriggerType.Event, stringName);
			}
		}
		_lockedDown = false;
	}

	public void _RunTransition(Transition transition, StateChartState source)
	{
		_pendingTransitions.Add(new PendingTransition
		{
			transition = transition,
			source = source
		});
		if (!_transitionsProcessingActive)
		{
			_RunQueuedTransitions();
		}
	}

	private void _RunQueuedTransitions()
	{
		_transitionsProcessingActive = true;
		int num = 1;
		while (_pendingTransitions.Count > 0)
		{
			PendingTransition pendingTransition = _pendingTransitions[0];
			_pendingTransitions.RemoveAt(0);
			_DoRunTransition(pendingTransition.transition, pendingTransition.source);
			num++;
			if (num > 100)
			{
				GD.PushError("Infinite loop detected in transitions. Aborting. The state chart is now in an invalid state and no longer usable.");
				break;
			}
		}
		_transitionsProcessingActive = false;
		if (!_lockedDown)
		{
			_RunChanges();
		}
	}

	private void _DoRunTransition(Transition transition, StateChartState source)
	{
		if (source.active)
		{
			transition.EmitTaken();
			source._HandleTransition(transition, source);
			_stateChangePending = true;
		}
		else
		{
			_WarnNotActive(transition, source);
		}
	}

	private void _WarnNotActive(Transition transition, StateChartState source)
	{
		GD.PushWarning("Ignoring request for transitioning from ", source.Name, " to ", transition.to, " as the source state is no longer active. Check whether your trigger multiple state changes within a single frame.");
	}

	public void Step()
	{
		if (!IsNodeReady())
		{
			GD.PushError("State chart is not yet ready. If you call `Step` in `_Ready`, please call it deferred.");
		}
		else if (IsResourceMode)
		{
			GD.PushWarning("Compact StateChart does not expose Legacy Step callbacks.");
		}
		else if (!GodotObject.IsInstanceValid(_state))
		{
			GD.PushError("State chart has no root state. Ignoring call to `Step`.");
		}
		else
		{
			_state._StateStep();
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>();
		if (RuntimeMode == StateMachineRuntimeMode.Resource || (RuntimeMode == StateMachineRuntimeMode.Auto && Definition != null))
		{
			if (Definition == null)
			{
				list.Add("Resource StateChart requires a StateMachineDefinition.");
			}
			else
			{
				StateMachineValidationResult stateMachineValidationResult = StateMachineValidator.Validate(Definition);
				for (int i = 0; i < stateMachineValidationResult.Diagnostics.Count; i++)
				{
					StateMachineDiagnostic stateMachineDiagnostic = stateMachineValidationResult.Diagnostics[i];
					list.Add(stateMachineDiagnostic.Code + ": " + stateMachineDiagnostic.Message);
				}
			}
			return list.ToArray();
		}
		if (GetChildCount() != 1)
		{
			list.Add("StateChart must have exactly one child");
		}
		else if (!(GetChild(0) is StateChartState))
		{
			list.Add("StateChart's child must be a State");
		}
		return list.ToArray();
	}

	public override void _ExitTree()
	{
		SuspendResourceRuntimeBatch();
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1)
		{
			ReleaseResourceRuntime();
		}
	}

	private void ReleaseResourceRuntime()
	{
		SuspendResourceRuntimeBatch();
		if (_resourceController != null)
		{
			_resourceController.EventReceived -= EmitEventReceived;
			_resourceController.Diagnostic -= OnResourceDiagnostic;
			_resourceController.Dispose();
			_resourceController = null;
		}
	}
}
