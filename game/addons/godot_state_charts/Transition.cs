using System.Collections.Generic;
using Godot;

[GlobalClass]
[Icon("res://addons/godot_state_charts/transition.svg")]
[Tool]
public partial class Transition : Node
{
	public delegate void TakenEventHandler();

	private bool _dirty = true;

	private StateChartState _target;

	private int _supportedTriggerTypes;

	private NodePath _to = new NodePath();

	private StringName _event = new StringName();

	private Guard _guard;

	private string _delayInSeconds = "0.0";

	[Export(PropertyHint.None, "")]
	public NodePath to
	{
		get
		{
			return _to;
		}
		set
		{
			_to = value;
			_dirty = true;
			UpdateConfigurationWarnings();
		}
	}

	[Export(PropertyHint.None, "")]
	public StringName @event
	{
		get
		{
			return _event;
		}
		set
		{
			_event = value;
			_dirty = true;
			UpdateConfigurationWarnings();
		}
	}

	[Export(PropertyHint.None, "")]
	public Guard guard
	{
		get
		{
			return _guard;
		}
		set
		{
			_guard = value;
			_dirty = true;
			UpdateConfigurationWarnings();
		}
	}

	public float delaySeconds
	{
		get
		{
			if (float.TryParse(delayInSeconds, out var result))
			{
				return result;
			}
			return 0f;
		}
		set
		{
			delayInSeconds = value.ToString();
		}
	}

	[Export(PropertyHint.Expression, "")]
	public string delayInSeconds
	{
		get
		{
			return _delayInSeconds;
		}
		set
		{
			_delayInSeconds = value;
			UpdateConfigurationWarnings();
		}
	}

	public bool hasEvent
	{
		get
		{
			if (_event != null)
			{
				return _event != (StringName)"";
			}
			return false;
		}
	}

	public event TakenEventHandler OnTaken;

	public void EmitTaken()
	{
		this.OnTaken?.Invoke();
	}

	public bool IsTriggeredBy(StateChart.TriggerType triggerType)
	{
		if (_dirty)
		{
			_RefreshCaches();
		}
		return ((uint)_supportedTriggerTypes & (uint)triggerType) != 0;
	}

	public void Take(bool immediately = true)
	{
		if (!(GetParent() is StateChartState stateChartState))
		{
			GD.PushError("Transitions must be children of states.");
		}
		else
		{
			stateChartState._RunTransition(this, immediately);
		}
	}

	public bool EvaluateGuard()
	{
		if (_guard == null)
		{
			return true;
		}
		if (!(GetParent() is StateChartState))
		{
			GD.PushError("Transitions must be children of states.");
			return false;
		}
		return _guard.IsSatisfied(this, GetParent() as StateChartState);
	}

	public float EvaluateDelay()
	{
		if (float.TryParse(_delayInSeconds, out var result))
		{
			return result;
		}
		if (!(GetParent() is StateChartState stateChartState))
		{
			GD.PushError("Transitions must be children of states.");
			return 0f;
		}
		Variant variant = ExpressionUtil.EvaluateExpression("delay of " + DebugUtil.PathOf(this), stateChartState._chart, _delayInSeconds, 0.0);
		if (variant.VariantType != Variant.Type.Float)
		{
			GD.PushError("Expression: ", _delayInSeconds, " result: ", variant, " is not a float. Returning 0.0.");
			return 0f;
		}
		return variant.As<float>();
	}

	public StateChartState ResolveTarget()
	{
		if (_dirty)
		{
			_RefreshCaches();
		}
		return _target;
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>();
		if (GetChildCount() > 0)
		{
			list.Add("Transitions should not have children");
		}
		if (_to == null || _to == (NodePath)"")
		{
			list.Add("The target state is not set");
		}
		else if (ResolveTarget() == null)
		{
			list.Add("The target state " + _to.ToString() + " could not be found");
		}
		if (!(GetParent() is StateChartState))
		{
			list.Add("Transitions must be children of states.");
		}
		if (string.IsNullOrWhiteSpace(_delayInSeconds))
		{
			list.Add("Delay must be a valid expression. Use 0.0 if you want no delay.");
		}
		return list.ToArray();
	}

	private void _RefreshCaches()
	{
		_dirty = false;
		bool num = _event == null || _event == (StringName)"";
		if (_to != null && _to != (NodePath)"" && GetNodeOrNull(_to) is StateChartState target)
		{
			_target = target;
		}
		_supportedTriggerTypes = 0;
		if (!num)
		{
			_supportedTriggerTypes |= 1;
			return;
		}
		_supportedTriggerTypes |= 2;
		if (_guard != null)
		{
			_supportedTriggerTypes |= _guard.GetSupportedTriggerTypes();
		}
	}
}
