using System;
using System.Collections.Generic;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.OutPutPanel;

public partial class XWOutputPanel : PanelContainer
{
	public enum MessageType
	{
		Std,
		Error,
		Warning,
		Editor
	}

	private class LogMessage
	{
		public MessageType Type;

		public string Text = "";

		public int Count = 1;
	}

	private const int LineLimit = 10000;

	private const int IncrementalRefreshSlack = 256;

	private RichTextLabel _log;

	private LineEdit _searchBox;

	private Button _clearButton;

	private Button _collapseButton;

	private Button _stdFilter;

	private Button _errorFilter;

	private Button _warningFilter;

	private Button _editorFilter;

	private readonly List<LogMessage> _messages = new List<LogMessage>();

	private readonly Dictionary<MessageType, int> _messageCounts = new Dictionary<MessageType, int>
	{
		{
			MessageType.Std,
			0
		},
		{
			MessageType.Error,
			0
		},
		{
			MessageType.Warning,
			0
		},
		{
			MessageType.Editor,
			0
		}
	};

	private bool _filterStd = true;

	private bool _filterError = true;

	private bool _filterWarning = true;

	private bool _filterEditor = true;

	private bool _collapse;

	private bool _refreshScheduled;

	private bool _refreshWhenVisible;

	private int _renderedLineCount;

	public int FullRefreshCount { get; private set; }

	public int IncrementalAppendCount { get; private set; }

	public int RenderedLineCount => _renderedLineCount;

	public int StoredLineCount => GetStoredLineCount();

	public override void _Ready()
	{
		_log = GetNode<RichTextLabel>("%Log");
		_searchBox = GetNode<LineEdit>("%SearchBox");
		_clearButton = GetNode<Button>("%ClearButton");
		_collapseButton = GetNode<Button>("%CollapseButton");
		_stdFilter = GetNode<Button>("%StdFilter");
		_errorFilter = GetNode<Button>("%ErrorFilter");
		_warningFilter = GetNode<Button>("%WarningFilter");
		_editorFilter = GetNode<Button>("%EditorFilter");
		_clearButton.Pressed += OnClearPressed;
		_collapseButton.Toggled += OnCollapseToggled;
		_stdFilter.Toggled += delegate
		{
			_filterStd = _stdFilter.ButtonPressed;
			RequestRefresh();
		};
		_errorFilter.Toggled += delegate
		{
			_filterError = _errorFilter.ButtonPressed;
			RequestRefresh();
		};
		_warningFilter.Toggled += delegate
		{
			_filterWarning = _warningFilter.ButtonPressed;
			RequestRefresh();
		};
		_editorFilter.Toggled += delegate
		{
			_filterEditor = _editorFilter.ButtonPressed;
			RequestRefresh();
		};
		_searchBox.TextChanged += delegate
		{
			RequestRefresh();
		};
		base.VisibilityChanged += OnOutputVisibilityChanged;
		UpdateFilterCounts();
		Refresh();
	}

	public void AddMessage(string text, MessageType type = MessageType.Std)
	{
		if (text == null)
		{
			text = "";
		}
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		List<LogMessage> list = new List<LogMessage>(array.Length);
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			list.Add(ProcessMessageLine(text2, type));
		}
		TrimMessages();
		UpdateFilterCounts();
		if (CanAppendIncrementally())
		{
			foreach (LogMessage item in list)
			{
				if (PassFilter(item.Type))
				{
					AppendLogLine(item);
					_renderedLineCount++;
					IncrementalAppendCount++;
				}
			}
			if (_renderedLineCount >= 10256)
			{
				RequestRefresh();
			}
		}
		else
		{
			RequestRefresh();
		}
	}

	public void Clear()
	{
		OnClearPressed();
	}

	private LogMessage ProcessMessageLine(string text, MessageType type)
	{
		if (_messages.Count > 0)
		{
			List<LogMessage> messages = _messages;
			LogMessage logMessage = messages[messages.Count - 1];
			if (logMessage.Type == type && logMessage.Text == text)
			{
				logMessage.Count++;
				_messageCounts[type]++;
				return logMessage;
			}
		}
		LogMessage logMessage2 = new LogMessage
		{
			Type = type,
			Text = text
		};
		_messages.Add(logMessage2);
		_messageCounts[type]++;
		return logMessage2;
	}

	private void Refresh()
	{
		if (!GodotObject.IsInstanceValid(_log) || !GodotObject.IsInstanceValid(_searchBox))
		{
			return;
		}
		FullRefreshCount++;
		_refreshScheduled = false;
		_refreshWhenVisible = false;
		_log.Clear();
		string text = _searchBox.Text;
		int num = 0;
		int num2 = FindStartMessageIndex(text);
		int num3 = GetInitialSkip(num2, text);
		for (int i = num2; i < _messages.Count; i++)
		{
			LogMessage logMessage = _messages[i];
			if (!PassFilter(logMessage.Type) || !PassSearch(logMessage, text))
			{
				continue;
			}
			int num4 = (_collapse ? 1 : logMessage.Count);
			for (int j = num3; j < num4; j++)
			{
				num3 = 0;
				AppendLogLine(logMessage);
				num++;
				if (num >= 10000)
				{
					_renderedLineCount = num;
					return;
				}
			}
		}
		_renderedLineCount = num;
	}

	private bool CanAppendIncrementally()
	{
		if (IsVisibleInTree() && !_refreshScheduled && !_collapse && GodotObject.IsInstanceValid(_searchBox) && string.IsNullOrEmpty(_searchBox.Text))
		{
			return _renderedLineCount < 10256;
		}
		return false;
	}

	private void RequestRefresh()
	{
		if (!IsVisibleInTree())
		{
			_refreshWhenVisible = true;
		}
		else if (!_refreshScheduled)
		{
			_refreshScheduled = true;
			CallDeferred("FlushPendingRefresh");
		}
	}

	private void FlushPendingRefresh()
	{
		if (_refreshScheduled)
		{
			if (!IsVisibleInTree())
			{
				_refreshScheduled = false;
				_refreshWhenVisible = true;
			}
			else
			{
				Refresh();
			}
		}
	}

	private void OnOutputVisibilityChanged()
	{
		if (IsVisibleInTree() && _refreshWhenVisible)
		{
			RequestRefresh();
		}
	}

	private bool PassFilter(MessageType type)
	{
		return type switch
		{
			MessageType.Std => _filterStd, 
			MessageType.Error => _filterError, 
			MessageType.Warning => _filterWarning, 
			MessageType.Editor => _filterEditor, 
			_ => true, 
		};
	}

	private static bool PassSearch(LogMessage message, string filter)
	{
		if (!string.IsNullOrEmpty(filter) && !message.Text.Contains(filter, StringComparison.OrdinalIgnoreCase))
		{
			return StripBbcode(message.Text).Contains(filter, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private int FindStartMessageIndex(string filter)
	{
		int num = 0;
		for (int num2 = _messages.Count - 1; num2 >= 0; num2--)
		{
			LogMessage logMessage = _messages[num2];
			if (PassFilter(logMessage.Type) && PassSearch(logMessage, filter))
			{
				num += (_collapse ? 1 : logMessage.Count);
				if (num >= 10000)
				{
					return num2;
				}
			}
		}
		return 0;
	}

	private int GetInitialSkip(int startIndex, string filter)
	{
		if (_collapse || startIndex <= 0 || startIndex >= _messages.Count)
		{
			return 0;
		}
		int num = 0;
		for (int i = startIndex; i < _messages.Count; i++)
		{
			LogMessage logMessage = _messages[i];
			if (PassFilter(logMessage.Type) && PassSearch(logMessage, filter))
			{
				num += logMessage.Count;
			}
		}
		return Mathf.Max(0, num - 10000);
	}

	private void AppendLogLine(LogMessage message)
	{
		bool flag = false;
		if (message.Type == MessageType.Error)
		{
			_log.PushColor(new Color("#ff5f5f"));
			_log.PushBold();
			_log.AddText("错误: ");
			_log.Pop();
			flag = true;
		}
		else if (message.Type == MessageType.Warning)
		{
			_log.PushColor(new Color("#ffd166"));
			_log.PushBold();
			_log.AddText("警告: ");
			_log.Pop();
			flag = true;
		}
		else if (message.Type == MessageType.Editor)
		{
			_log.PushColor(new Color(1f, 1f, 1f, 0.62f));
			flag = true;
		}
		if (_collapse && message.Count > 1)
		{
			_log.PushBold();
			_log.AddText($"({message.Count}) ");
			_log.Pop();
		}
		if (message.Type == MessageType.Error || message.Type == MessageType.Warning)
		{
			_log.AppendText(message.Text);
		}
		else
		{
			_log.AddText(message.Text);
		}
		if (flag)
		{
			_log.Pop();
		}
		_log.Newline();
	}

	private void OnClearPressed()
	{
		_messages.Clear();
		_messageCounts[MessageType.Std] = 0;
		_messageCounts[MessageType.Error] = 0;
		_messageCounts[MessageType.Warning] = 0;
		_messageCounts[MessageType.Editor] = 0;
		_refreshScheduled = false;
		_refreshWhenVisible = false;
		UpdateFilterCounts();
		Refresh();
	}

	private void OnCollapseToggled(bool toggled)
	{
		_collapse = toggled;
		RequestRefresh();
	}

	private void UpdateFilterCounts()
	{
		_stdFilter.Text = _messageCounts[MessageType.Std].ToString();
		_errorFilter.Text = _messageCounts[MessageType.Error].ToString();
		_warningFilter.Text = _messageCounts[MessageType.Warning].ToString();
		_editorFilter.Text = _messageCounts[MessageType.Editor].ToString();
	}

	private void TrimMessages()
	{
		int num = GetStoredLineCount();
		while (num > 10000 && _messages.Count > 0)
		{
			LogMessage logMessage = _messages[0];
			int num2 = num - 10000;
			if (logMessage.Count > num2)
			{
				logMessage.Count -= num2;
				_messageCounts[logMessage.Type] -= num2;
				num -= num2;
				break;
			}
			num -= logMessage.Count;
			_messageCounts[logMessage.Type] -= logMessage.Count;
			_messages.RemoveAt(0);
		}
	}

	private int GetStoredLineCount()
	{
		int num = 0;
		foreach (LogMessage message in _messages)
		{
			num += message.Count;
		}
		return num;
	}

	public int GetStoredMessageCount(MessageType type)
	{
		return _messageCounts.GetValueOrDefault(type);
	}

	private static string StripBbcode(string text)
	{
		if (string.IsNullOrEmpty(text) || !text.Contains('['))
		{
			return text ?? "";
		}
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (c == '[')
			{
				flag = true;
			}
			else if (c == ']' && flag)
			{
				flag = false;
			}
			else if (!flag)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
