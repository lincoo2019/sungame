using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWEditorLog : PanelContainer
{
	public enum LogType
	{
		Info,
		Warning,
		Error,
		Script
	}

	private Button _infoFilterButton;

	private Button _warningFilterButton;

	private Button _errorFilterButton;

	private Button _scriptFilterButton;

	private Button _copyButton;

	private Button _clearButton;

	private RichTextLabel _logLabel;

	private readonly List<(LogType type, string text)> _entries = new List<(LogType, string)>();

	private bool _showInfo = true;

	private bool _showWarning = true;

	private bool _showError = true;

	private bool _showScript = true;

	public override void _Ready()
	{
		_infoFilterButton = GetNode<Button>("%InfoFilterButton");
		_warningFilterButton = GetNode<Button>("%WarningFilterButton");
		_errorFilterButton = GetNode<Button>("%ErrorFilterButton");
		_scriptFilterButton = GetNode<Button>("%ScriptFilterButton");
		_copyButton = GetNode<Button>("%CopyButton");
		_clearButton = GetNode<Button>("%ClearButton");
		_logLabel = GetNode<RichTextLabel>("%LogLabel");
		_infoFilterButton.Toggled += delegate(bool v)
		{
			_showInfo = v;
			Refresh();
		};
		_warningFilterButton.Toggled += delegate(bool v)
		{
			_showWarning = v;
			Refresh();
		};
		_errorFilterButton.Toggled += delegate(bool v)
		{
			_showError = v;
			Refresh();
		};
		_scriptFilterButton.Toggled += delegate(bool v)
		{
			_showScript = v;
			Refresh();
		};
		_copyButton.Pressed += OnCopyPressed;
		_clearButton.Pressed += OnClearPressed;
	}

	public void AddEntry(string text, LogType type = LogType.Info)
	{
		_entries.Add((type, text));
		Refresh();
	}

	private void Refresh()
	{
		_logLabel.Clear();
		foreach (var (type, text) in _entries)
		{
			if (PassFilter(type))
			{
				AppendColored(text, type);
			}
		}
	}

	private bool PassFilter(LogType type)
	{
		return type switch
		{
			LogType.Info => _showInfo, 
			LogType.Warning => _showWarning, 
			LogType.Error => _showError, 
			LogType.Script => _showScript, 
			_ => true, 
		};
	}

	private void AppendColored(string text, LogType type)
	{
		_logLabel.PushColor(new Color(type switch
		{
			LogType.Error => "#ff4545", 
			LogType.Warning => "#ffd400", 
			LogType.Script => "#87ceeb", 
			_ => "#cccccc", 
		}));
		_logLabel.AddText(text + "\n");
		_logLabel.Pop();
	}

	private void OnCopyPressed()
	{
		DisplayServer.ClipboardSet(_logLabel.GetSelectedText());
	}

	private void OnClearPressed()
	{
		_entries.Clear();
		Refresh();
	}
}
