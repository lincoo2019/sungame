using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPRealTimeValidator : RefCounted
{
	[Signal]
	public delegate void ValidationCompleteEventHandler(Array results);

	private readonly List<XWBPGraphData> _subscribedTargets = new List<XWBPGraphData>();

	private XWBPScriptData _subscribedScriptData;

	public XWBPEditor Editor { get; set; }

	public XWBPValidator Validator { get; set; }

	public Timer DebounceTimer { get; set; }

	public bool IsEnabled { get; set; } = true;

	public float DebounceTime { get; set; } = 1f;

	public XWBPRealTimeValidator()
	{
	}

	public XWBPRealTimeValidator(XWBPEditor editor)
	{
		Editor = editor;
		CreateDebounceTimer();
	}

	private void CreateDebounceTimer()
	{
		DebounceTimer = new Timer();
		DebounceTimer.OneShot = true;
		DebounceTimer.WaitTime = DebounceTime;
		DebounceTimer.Timeout += OnDebounceTimerTimeout;
		Editor?.AddChild(DebounceTimer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	public void Enable()
	{
		IsEnabled = true;
		ConnectSignals();
	}

	public void Disable()
	{
		IsEnabled = false;
		DisconnectSignals();
	}

	public void ConnectSignals()
	{
		DisconnectSignals();
		if (!GodotObject.IsInstanceValid(Editor) || Editor.BpScriptData == null)
		{
			return;
		}
		_subscribedScriptData = Editor.BpScriptData;
		_subscribedScriptData.BlueprintChanged += OnBlueprintChanged;
		foreach (XWBPGraphData value in Editor.BpScriptData.Graphs.Values)
		{
			value.GraphChange += OnGraphChanged;
			_subscribedTargets.Add(value);
		}
		foreach (XWBPFunctionData value2 in Editor.BpScriptData.Functions.Values)
		{
			value2.GraphChange += OnGraphChanged;
			_subscribedTargets.Add(value2);
		}
	}

	public void DisconnectSignals()
	{
		foreach (XWBPGraphData subscribedTarget in _subscribedTargets)
		{
			if (subscribedTarget != null && GodotObject.IsInstanceValid(subscribedTarget))
			{
				subscribedTarget.GraphChange -= OnGraphChanged;
			}
		}
		_subscribedTargets.Clear();
		if (GodotObject.IsInstanceValid(_subscribedScriptData))
		{
			_subscribedScriptData.BlueprintChanged -= OnBlueprintChanged;
		}
		_subscribedScriptData = null;
	}

	private void OnGraphChanged()
	{
		if (IsEnabled)
		{
			DebounceTimer?.Start();
		}
	}

	private void OnBlueprintChanged()
	{
		if (IsEnabled)
		{
			ConnectSignals();
			DebounceTimer?.Start();
		}
	}

	private void OnDebounceTimerTimeout()
	{
		if (!GodotObject.IsInstanceValid(Editor) || Editor.BpScriptData == null)
		{
			return;
		}
		Validator = new XWBPValidator(Editor.BpScriptData);
		List<XWBPValidationResult> list = Validator.Validate();
		Array array = new Array();
		foreach (XWBPValidationResult item in list)
		{
			array.Add(item);
		}
		EmitSignal(SignalName.ValidationComplete, array);
	}

	public List<XWBPValidationResult> GetLastResults()
	{
		if (!GodotObject.IsInstanceValid(Validator))
		{
			return new List<XWBPValidationResult>();
		}
		return Validator.Results;
	}

	public bool HasErrors()
	{
		if (!GodotObject.IsInstanceValid(Validator))
		{
			return false;
		}
		return Validator.HasErrors();
	}
}
