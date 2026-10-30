using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CommandConsole : CanvasLayer
{
	public delegate void ConsoleOpenedEventHandler();

	public delegate void ConsoleClosedEventHandler();

	private Array<string> _history = new Array<string>();

	private int _historyIndex = -1;

	private bool _isOpen;

	private int _autocompleteIndex = -1;

	private Array<string> _autocompleteList = new Array<string>();

	private bool _welcomeShown;

	private PanelContainer _consolePanel;

	private RichTextLabel _outputLog;

	private LineEdit _inputLine;

	public static CommandConsole Instance;

	public event ConsoleOpenedEventHandler OnConsoleOpened;

	public event ConsoleClosedEventHandler OnConsoleClosed;

	public override void _Ready()
	{
		Instance = this;
		base.Layer = 200;
		_consolePanel = GetNode<PanelContainer>("%ConsolePanel");
		_outputLog = GetNode<RichTextLabel>("%OutputLog");
		_inputLine = GetNode<LineEdit>("%InputLine");
		_consolePanel.Visible = false;
		_outputLog.BbcodeEnabled = true;
		_outputLog.ScrollFollowing = true;
		_inputLine.TextSubmitted += _OnInputSubmitted;
		_inputLine.TextChanged += _OnInputChanged;
	}

	public override void _Input(InputEvent _event)
	{
		if (!(_event is InputEventKey { Pressed: not false } inputEventKey) || !_isOpen)
		{
			return;
		}
		switch (inputEventKey.Keycode)
		{
		case Key.Escape:
			Close();
			GetViewport().SetInputAsHandled();
			break;
		case Key.Up:
			if (_inputLine.HasFocus() && _history.Count > 0)
			{
				_historyIndex = Mathf.Min(_historyIndex + 1, _history.Count - 1);
				_inputLine.Text = _history[_historyIndex];
				_inputLine.CaretColumn = _inputLine.Text.Length;
				GetViewport().SetInputAsHandled();
			}
			break;
		case Key.Down:
			if (_inputLine.HasFocus() && _history.Count > 0)
			{
				_historyIndex = Mathf.Max(_historyIndex - 1, -1);
				if (_historyIndex == -1)
				{
					_inputLine.Text = "";
				}
				else
				{
					_inputLine.Text = _history[_historyIndex];
				}
				_inputLine.CaretColumn = _inputLine.Text.Length;
				GetViewport().SetInputAsHandled();
			}
			break;
		case Key.Tab:
			if (_inputLine.HasFocus())
			{
				_HandleAutocomplete();
				GetViewport().SetInputAsHandled();
			}
			break;
		}
	}

	public void Open()
	{
		if (!_isOpen)
		{
			_isOpen = true;
			_consolePanel.Visible = true;
			_inputLine.GrabFocus();
			_autocompleteIndex = -1;
			_autocompleteList.Clear();
			if (!_welcomeShown)
			{
				_ShowWelcome();
				_welcomeShown = true;
			}
			this.OnConsoleOpened?.Invoke();
		}
	}

	public void Close()
	{
		if (_isOpen)
		{
			_isOpen = false;
			_consolePanel.Visible = false;
			_inputLine.Text = "";
			_inputLine.ReleaseFocus();
			_historyIndex = -1;
			_autocompleteIndex = -1;
			_autocompleteList.Clear();
			this.OnConsoleClosed?.Invoke();
		}
	}

	public bool IsOpen()
	{
		return _isOpen;
	}

	public Array<string> GetHistory()
	{
		return _history;
	}

	public void PrintLine(string text)
	{
		_outputLog.AppendText(text + "\n");
	}

	public void PrintInfo(string text)
	{
		_outputLog.AppendText("[color=gray]" + text + "[/color]\n");
	}

	public void PrintSuccess(string text)
	{
		_outputLog.AppendText("[color=green]" + text + "[/color]\n");
	}

	public void PrintWarning(string text)
	{
		_outputLog.AppendText("[color=yellow]" + text + "[/color]\n");
	}

	public void PrintError(string text)
	{
		_outputLog.AppendText("[color=red]" + text + "[/color]\n");
	}

	public void ClearLog()
	{
		_outputLog.Clear();
	}

	private void _ShowWelcome()
	{
		PrintLine("[color=cyan]═══════════════════════════════════════[/color]");
		PrintLine("[color=cyan]  指令控制台 v1.0[/color]");
		PrintLine("[color=gray]  输入 /help 查看所有指令[/color]");
		PrintLine("[color=gray]  按 Esc 关闭控制台[/color]");
		PrintLine("[color=cyan]═══════════════════════════════════════[/color]");
	}

	private void _OnInputSubmitted(string text)
	{
		_inputLine.Text = "";
		if (!(text.StripEdges() == ""))
		{
			_history.Insert(0, text);
			if (_history.Count > 50)
			{
				_history.RemoveAt(_history.Count - 1);
			}
			_historyIndex = -1;
			_autocompleteIndex = -1;
			_autocompleteList.Clear();
			PrintLine("[color=white]> " + text + "[/color]");
			CommandRegistry.ExecuteCommand(text, new Callable(this, MethodName.PrintError));
			_inputLine.GrabFocus();
		}
	}

	private void _OnInputChanged(string newText)
	{
		_autocompleteIndex = -1;
		_autocompleteList.Clear();
	}

	private void _HandleAutocomplete()
	{
		string text = _inputLine.Text.StripEdges();
		if (text == "")
		{
			return;
		}
		if (_autocompleteIndex == -1)
		{
			_autocompleteList.Clear();
			Array<string> array = CommandRegistry._ParseCommand(text.StartsWith("/") ? text.Substring(1) : text);
			if (array.Count <= 1)
			{
				string text2 = text;
				if (text2.StartsWith("/"))
				{
					text2 = text2.Substring(1);
				}
				text2 = text2.ToLower();
				foreach (KeyValuePair<string, CommandConfig> allCommand in CommandRegistry.GetAllCommands())
				{
					string key = allCommand.Key;
					if (key.StartsWith(text2))
					{
						_autocompleteList.Add(key);
					}
				}
			}
			else
			{
				CommandConfig command = CommandRegistry.GetCommand(array[0].ToLower());
				if (command != null && array.Count - 2 < command.ArgsInfo.Count)
				{
					CommandArg commandArg = command.ArgsInfo[array.Count - 2];
					if (commandArg.Suggestions.Target != null || (object)commandArg.Suggestions.Delegate != null)
					{
						string value = array[array.Count - 1].ToLower();
						Variant variant = commandArg.Suggestions.Call();
						if (variant.VariantType == Variant.Type.Array)
						{
							foreach (Variant item in variant.AsGodotArray())
							{
								if (item.AsString().ToLower().StartsWith(value))
								{
									_autocompleteList.Add(item.AsString());
								}
							}
						}
					}
				}
			}
			if (_autocompleteList.Count == 0)
			{
				return;
			}
			_autocompleteIndex = 0;
		}
		else
		{
			_autocompleteIndex = (_autocompleteIndex + 1) % _autocompleteList.Count;
		}
		if (_autocompleteList.Count > 0)
		{
			Array<string> array2 = CommandRegistry._ParseCommand(text.StartsWith("/") ? text.Substring(1) : text);
			string text3 = _autocompleteList[_autocompleteIndex];
			if (array2.Count <= 1)
			{
				_inputLine.Text = "/" + text3 + " ";
			}
			else
			{
				array2[array2.Count - 1] = text3;
				_inputLine.Text = "/" + string.Join(" ", array2) + " ";
			}
			_inputLine.CaretColumn = _inputLine.Text.Length;
		}
	}

	public CommandConsole()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/CommandConsole");
	}
}
