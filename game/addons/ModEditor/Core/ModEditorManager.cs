using Godot;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.Core;

public partial class ModEditorManager : Node
{
	[Signal]
	public delegate void EditorOpenedEventHandler();

	[Signal]
	public delegate void EditorClosedEventHandler();

	private const string EditorWindowScenePath = "res://addons/ModEditor/GUI/XWModEditorWindow.tscn";

	private static PackedScene _editorWindowScene;

	[Export(PropertyHint.None, "")]
	private PackedScene _editorPanelScene;

	private Window _editorWindow;

	private Control _editorPanel;

	private bool _isOpen;

	private bool _customCursorBlockedForEditor;

	private string _editorWindowProjectTitle = "";

	private const string EditorWindowBaseTitle = "PVZ Mod 编辑器";

	private static readonly Vector2I PreferredEditorWindowSize = new Vector2I(1600, 900);

	private static readonly Vector2I MinimumEditorWindowSize = new Vector2I(960, 540);

	public static ModEditorManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		base.ProcessMode = ProcessModeEnum.Always;
	}

	public void OpenEditor()
	{
		if (Global.IsMobile)
		{
			return;
		}
		if (_editorWindow == null)
		{
			if (_editorPanelScene == null)
			{
				GD.PrintErr("[ModEditor] _editorPanelScene 未绑定，无法打开编辑器。");
				return;
			}
			Rect2I currentScreenUsableRect = GetCurrentScreenUsableRect();
			Vector2I editorWindowInitialSize = GetEditorWindowInitialSize(currentScreenUsableRect.Size);
			if (_editorWindowScene == null)
			{
				_editorWindowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWModEditorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_editorWindow = _editorWindowScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_editorWindow))
			{
				GD.PrintErr("[ModEditor] 无法加载编辑器宿主窗口场景。");
				_editorWindow = null;
				return;
			}
			_editorWindow.Title = BuildEditorWindowTitle(_editorWindowProjectTitle);
			_editorWindow.Size = editorWindowInitialSize;
			_editorWindow.MinSize = GetEditorWindowMinSize(editorWindowInitialSize);
			_editorWindow.Position = new Vector2I(currentScreenUsableRect.Position.X + Mathf.Max(0, (currentScreenUsableRect.Size.X - editorWindowInitialSize.X) / 2), currentScreenUsableRect.Position.Y + Mathf.Max(0, (currentScreenUsableRect.Size.Y - editorWindowInitialSize.Y) / 2));
			AddChild(_editorWindow, forceReadableName: false, InternalMode.Disabled);
			_editorWindow.CloseRequested += CloseEditor;
			_editorPanel = _editorPanelScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_editorPanel))
			{
				GD.PrintErr("[ModEditor] 无法实例化编辑器主面板。");
				_editorWindow.QueueFree();
				_editorWindow = null;
				return;
			}
			_editorPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_editorPanel.OffsetLeft = 0f;
			_editorPanel.OffsetTop = 0f;
			_editorPanel.OffsetRight = 0f;
			_editorPanel.OffsetBottom = 0f;
			_editorWindow.GetNode<Control>("PanelHost").AddChild(_editorPanel, forceReadableName: false, InternalMode.Disabled);
		}
		_editorWindow.Mode = Window.ModeEnum.Maximized;
		_editorWindow.Show();
		_editorWindow.GrabFocus();
		SetCustomCursorBlockedForEditor(blocked: true);
		_isOpen = true;
		EmitSignal(SignalName.EditorOpened);
	}

	public void SetEditorWindowProjectTitle(string projectName)
	{
		_editorWindowProjectTitle = (projectName ?? "").Trim();
		if (_editorWindow != null)
		{
			_editorWindow.Title = BuildEditorWindowTitle(_editorWindowProjectTitle);
		}
	}

	private static string BuildEditorWindowTitle(string projectName)
	{
		if (!string.IsNullOrWhiteSpace(projectName))
		{
			return "PVZ Mod 编辑器 - " + projectName;
		}
		return "PVZ Mod 编辑器";
	}

	private static Rect2I GetCurrentScreenUsableRect()
	{
		int windowAtScreenPosition = DisplayServer.GetWindowAtScreenPosition(DisplayServer.MouseGetPosition());
		Rect2I result = DisplayServer.ScreenGetUsableRect(windowAtScreenPosition);
		if (result.Size != Vector2I.Zero)
		{
			return result;
		}
		Vector2I vector2I = DisplayServer.ScreenGetSize(windowAtScreenPosition);
		if (vector2I == Vector2I.Zero)
		{
			vector2I = DisplayServer.ScreenGetSize();
		}
		return new Rect2I(Vector2I.Zero, vector2I);
	}

	private static Vector2I GetEditorWindowInitialSize(Vector2I usableSize)
	{
		if (usableSize == Vector2I.Zero)
		{
			return PreferredEditorWindowSize;
		}
		int b = Mathf.Max(640, usableSize.X - 48);
		int b2 = Mathf.Max(360, usableSize.Y - 48);
		return new Vector2I(Mathf.Min(PreferredEditorWindowSize.X, b), Mathf.Min(PreferredEditorWindowSize.Y, b2));
	}

	private static Vector2I GetEditorWindowMinSize(Vector2I initialSize)
	{
		return new Vector2I(Mathf.Min(MinimumEditorWindowSize.X, initialSize.X), Mathf.Min(MinimumEditorWindowSize.Y, initialSize.Y));
	}

	public void CloseEditor()
	{
		if (_editorWindow != null)
		{
			_editorWindow.Hide();
		}
		SetCustomCursorBlockedForEditor(blocked: false);
		_isOpen = false;
		EmitSignal(SignalName.EditorClosed);
	}

	public override void _ExitTree()
	{
		SetCustomCursorBlockedForEditor(blocked: false);
		ModLoader.UnloadAll();
		Instance = null;
		base._ExitTree();
	}

	public void ToggleEditor()
	{
		if (_isOpen)
		{
			CloseEditor();
		}
		else
		{
			OpenEditor();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.F3)
		{
			ToggleEditor();
			GetViewport().SetInputAsHandled();
		}
	}

	private void SetCustomCursorBlockedForEditor(bool blocked)
	{
		if (_customCursorBlockedForEditor != blocked)
		{
			Node nodeOrNull = GetNodeOrNull<Node>("/root/Cursor");
			if (nodeOrNull != null && nodeOrNull.HasMethod("set_mod_editor_cursor_blocked"))
			{
				nodeOrNull.Call("set_mod_editor_cursor_blocked", blocked);
				_customCursorBlockedForEditor = blocked;
			}
		}
	}

	public ModEditorManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/ModEditorManager");
	}
}
