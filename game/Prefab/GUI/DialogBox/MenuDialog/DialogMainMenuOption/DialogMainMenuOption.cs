using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Tools.GUI;

[GlobalClass]
public partial class DialogMainMenuOption : MenuDialogBase
{
	private Control _options;

	private MainButton _managementButton;

	private BaseButton _backButton;

	private Shortcut _backShortcut;

	private Control _managementOverlay;

	private XWModToolsPanel _managementPanel;

	public bool AllowModNavigation { get; set; } = true;

	public override void _Ready()
	{
		base._Ready();
		_options = GetNode<Control>("Layer/Control/DialogMenu");
		_backButton = GetNode<BaseButton>("%BackButton");
		_managementButton = _options.GetNode<MainButton>("ModManagementButton");
		_managementButton.Pressed += OpenManagement;
	}

	public void OpenManagement()
	{
		if (GodotObject.IsInstanceValid(_managementOverlay))
		{
			return;
		}
		dragStart = false;
		isInDrag = false;
		_backShortcut = _backButton.Shortcut;
		_backButton.Shortcut = null;
		_backButton.Disabled = true;
		_options.Hide();
		_managementOverlay = new Control
		{
			Name = "ModManagementOverlay",
			MouseFilter = MouseFilterEnum.Stop,
			Theme = XWModToolsPanel.CreatePlayerManagementTheme()
		};
		GetNode<Control>("Layer/Control").AddChild(_managementOverlay, forceReadableName: false, InternalMode.Disabled);
		_managementOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		TextureRect textureRect = new TextureRect
		{
			Name = "HandbookBackground",
			MouseFilter = MouseFilterEnum.Ignore,
			Texture = GD.Load<Texture2D>("res://Asset/Texture/GUI/Almanac/AlmanacPlantBackground.jpg"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale
		};
		_managementOverlay.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
		textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		Label label = new Label
		{
			Name = "ManagementTitle",
			Text = "Mod 管理",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		label.AddThemeFontOverride("font", GD.Load<Font>("res://Asset/Font/fzjz.ttf"));
		label.AddThemeFontSizeOverride("font_size", 32);
		label.AddThemeColorOverride("font_color", new Color("f8df9a"));
		label.AddThemeColorOverride("font_outline_color", new Color("482508"));
		label.AddThemeConstantOverride("outline_size", 2);
		_managementOverlay.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		label.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide, LayoutPresetMode.Minsize);
		label.OffsetTop = 16f;
		label.OffsetBottom = 66f;
		MarginContainer marginContainer = new MarginContainer
		{
			Name = "HandbookContent"
		};
		marginContainer.AddThemeConstantOverride("margin_left", 36);
		marginContainer.AddThemeConstantOverride("margin_right", 36);
		marginContainer.AddThemeConstantOverride("margin_top", 90);
		marginContainer.AddThemeConstantOverride("margin_bottom", 36);
		_managementOverlay.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		marginContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_managementPanel = GD.Load<PackedScene>("res://addons/ModEditor/Tools/GUI/XWModToolsPanel.tscn").Instantiate<XWModToolsPanel>(PackedScene.GenEditState.Disabled);
		_managementPanel.Name = "PlayerModManager";
		_managementPanel.ManagementOnly = true;
		_managementPanel.ManagementConfirmationHost = _managementOverlay;
		_managementPanel.ManagementReturnRequested = ReturnFromManagement;
		if (AllowModNavigation)
		{
			_managementPanel.ManagementPlayRequested = delegate
			{
				_managementPanel.CloseManagementSession();
				CloseDialog();
				XWModLevelSession.Clear();
				Global.Instance.isEditor = false;
				Global.Instance.currentLevelChoose = "__ModLevels";
				Global.Instance.currentChapterId = -1;
				Global.Instance.currentLevelId = -1;
				SceneManager.Instance.ChangeScene("LevelChoose");
			};
			_managementPanel.ManagementCreateRequested = delegate
			{
				ReturnFromManagement();
				CloseDialog();
				ModEditorManager.Instance?.OpenEditor();
			};
		}
		marginContainer.AddChild(_managementPanel, forceReadableName: false, InternalMode.Disabled);
	}

	private void ReturnFromManagement()
	{
		if (GodotObject.IsInstanceValid(_managementPanel) && !_managementPanel.TryCancelManagementConfirmation() && !_managementPanel.TryClosePlayerUtility())
		{
			_managementPanel.CloseManagementSession();
			_managementOverlay.QueueFree();
			_managementPanel = null;
			_managementOverlay = null;
			_backButton.Shortcut = _backShortcut;
			_backButton.Disabled = false;
			_options.Show();
			_managementButton.GrabFocus();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (GodotObject.IsInstanceValid(_managementOverlay) && @event.IsPressed() && !@event.IsEcho())
		{
			Shortcut backShortcut = _backShortcut;
			if (backShortcut != null && backShortcut.MatchesEvent(@event))
			{
				GetViewport().SetInputAsHandled();
				ReturnFromManagement();
			}
		}
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_managementPanel))
		{
			_managementPanel.CloseManagementSession();
		}
		base._ExitTree();
	}
}
