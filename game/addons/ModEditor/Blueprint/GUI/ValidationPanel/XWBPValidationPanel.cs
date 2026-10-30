using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPValidationPanel : PanelContainer
{
	[Signal]
	public delegate void ValidationResultClickedEventHandler(XWBPValidationResult result);

	private XWBPEditor _editor;

	private readonly List<XWBPValidationResult> _results = new List<XWBPValidationResult>();

	private RichTextLabel _resultList;

	private Label _errorCountLabel;

	private Label _warningCountLabel;

	private Button _toggleButton;

	private Button _validateButton;

	private Button _cleanButton;

	public override void _Ready()
	{
		_resultList = GetNode<RichTextLabel>("%ResultList");
		_errorCountLabel = GetNode<Label>("%ErrorCountLabel");
		_warningCountLabel = GetNode<Label>("%WarningCountLabel");
		_toggleButton = GetNode<Button>("%ToggleButton");
		_validateButton = GetNode<Button>("%ValidateButton");
		_cleanButton = GetNode<Button>("%CleanButton");
		_validateButton.Pressed += OnValidatePressed;
		_cleanButton.Pressed += OnCleanPressed;
		_toggleButton.Pressed += OnTogglePressed;
		_resultList.MetaClicked += OnResultMetaClicked;
	}

	public void SetEditor(XWBPEditor editor)
	{
		_editor = editor;
	}

	public void SetResults(List<XWBPValidationResult> results)
	{
		_results.Clear();
		_resultList.Clear();
		int num = 0;
		int num2 = 0;
		foreach (XWBPValidationResult result in results)
		{
			_results.Add(result);
			string severityText = result.GetSeverityText();
			string value = ((result.ResultSeverity == XWBPValidationResult.Severity.Error) ? "red" : ((result.ResultSeverity == XWBPValidationResult.Severity.Warning) ? "yellow" : "white"));
			string text = ((result.GraphData != null) ? result.GraphData.Name : "");
			string text2 = $"[url={_results.Count - 1}][color={value}][{severityText}] {EscapeBbcode(result.GetErrorTypeText())}";
			if (!string.IsNullOrEmpty(text))
			{
				text2 = text2 + " / " + EscapeBbcode(text);
			}
			if (result.NodeId >= 0)
			{
				text2 += $" (节点 {result.NodeId})";
			}
			text2 = text2 + ": " + EscapeBbcode(result.Message) + "[/color][/url]\n";
			_resultList.AppendText(text2);
			if (result.ResultSeverity == XWBPValidationResult.Severity.Error)
			{
				num++;
			}
			else if (result.ResultSeverity == XWBPValidationResult.Severity.Warning)
			{
				num2++;
			}
		}
		_errorCountLabel.Text = num.ToString();
		_warningCountLabel.Text = num2.ToString();
	}

	private void OnValidatePressed()
	{
		if (_editor?.RealTimeValidator?.DebounceTimer != null)
		{
			_editor.RealTimeValidator.DebounceTimer.Start();
		}
	}

	private void OnCleanPressed()
	{
		_results.Clear();
		_resultList.Clear();
		_errorCountLabel.Text = "0";
		_warningCountLabel.Text = "0";
	}

	private void OnTogglePressed()
	{
		bool visible = _resultList.Visible;
		_resultList.Visible = !visible;
		_toggleButton.Text = (visible ? "▼" : "▲");
	}

	private void OnResultMetaClicked(Variant meta)
	{
		int num = meta.AsInt32();
		if (num >= 0 && num < _results.Count)
		{
			XWBPValidationResult xWBPValidationResult = _results[num];
			EmitSignal(SignalName.ValidationResultClicked, xWBPValidationResult);
			_editor?.FocusValidationResult(xWBPValidationResult);
		}
	}

	private static string EscapeBbcode(string text)
	{
		return (text ?? "").Replace("[", "\\[").Replace("]", "\\]");
	}
}
