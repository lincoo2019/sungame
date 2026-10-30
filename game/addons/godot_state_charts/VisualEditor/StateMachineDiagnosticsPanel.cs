using System;
using Godot;

[Tool]
public partial class StateMachineDiagnosticsPanel : PanelContainer
{
	private Label _summary;

	private VBoxContainer _items;

	public event Action<string> NavigateRequested;

	public override void _Ready()
	{
		base.CustomMinimumSize = new Vector2(0f, 128f);
		VBoxContainer vBoxContainer = new VBoxContainer();
		AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_summary = new Label
		{
			Text = "诊断：尚未校验"
		};
		vBoxContainer.AddChild(_summary, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		_items = new VBoxContainer
		{
			Name = "DiagnosticItems",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		scrollContainer.AddChild(_items, forceReadableName: false, InternalMode.Disabled);
	}

	public void ValidateDefinition(StateMachineDefinition definition)
	{
		StateMachineValidationResult result = StateMachineValidator.Validate(definition);
		ShowDiagnostics(result);
	}

	public void ShowDiagnostics(StateMachineValidationResult result)
	{
		Clear();
		if (result == null)
		{
			_summary.Text = "诊断：无法取得校验结果";
			return;
		}
		int num = 0;
		int num2 = 0;
		foreach (StateMachineDiagnostic diagnostic in result.Diagnostics)
		{
			if (diagnostic.Severity == StateMachineDiagnosticSeverity.Error)
			{
				num++;
			}
			else
			{
				num2++;
			}
			AddDiagnostic(diagnostic);
		}
		_summary.Text = ((result.Diagnostics.Count == 0) ? "✓ 诊断：可以运行" : $"诊断：{num} 个错误，{num2} 个警告");
	}

	public void NavigateToStableId(string stableId)
	{
		if (!string.IsNullOrWhiteSpace(stableId))
		{
			this.NavigateRequested?.Invoke(stableId);
		}
	}

	private void AddDiagnostic(StateMachineDiagnostic diagnostic)
	{
		string value = ((diagnostic.Severity == StateMachineDiagnosticSeverity.Error) ? "⛔" : "⚠");
		Button button = new Button
		{
			Text = $"{value} [{diagnostic.Code}] {diagnostic.Message}",
			Alignment = HorizontalAlignment.Left,
			TooltipText = diagnostic.Name + "\nStableId: " + diagnostic.StableId,
			Disabled = string.IsNullOrWhiteSpace(diagnostic.StableId)
		};
		button.Pressed += delegate
		{
			NavigateToStableId(diagnostic.StableId);
		};
		_items.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void Clear()
	{
		if (_items == null)
		{
			return;
		}
		foreach (Node child in _items.GetChildren())
		{
			_items.RemoveChild(child);
			child.QueueFree();
		}
	}
}
