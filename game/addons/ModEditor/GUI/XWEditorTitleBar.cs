using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.RunBar;

namespace PVZHE.ModEditor.GUI;

public partial class XWEditorTitleBar : VBoxContainer
{
	[Signal]
	public delegate void MainScreenChangedEventHandler(string screenKey);

	private const float CompactWidth = 1020f;

	private const string ReducedMotionSetting = "interface/reduced_motion";

	private const string LowPerformanceSetting = "interface/low_performance_mode";

	private const string ResourceHubIconPath = "res://addons/ModEditor/Icons/ResourceMap.svg";

	private const string ResourcePaletteScenePath = "res://addons/ModEditor/GUI/XWResourceWorkspacePalette.tscn";

	private static PackedScene _resourcePaletteScene;

	private MenuBar _menuBar;

	private Label _brandLabel;

	private PanelContainer _brandPanel;

	private PanelContainer _projectStatusPanel;

	private TextureRect _projectStatusIcon;

	private Label _projectTitle;

	private Label _workspaceLabel;

	private HBoxContainer _mainScreenButtons;

	private Button _blueprintButton;

	private Button _twoDButton;

	private Button _scriptButton;

	private Button _resourceHubButton;

	private Button _motionModeButton;

	private XWResourceWorkspacePalette _resourcePalette;

	private HBoxContainer _runBarContainer;

	private PanelContainer _runStatusPanel;

	private TextureRect _runStatusIcon;

	private Label _runStatusLabel;

	private XWEditorRunBar _boundRunBar;

	private readonly Dictionary<Button, string> _mainScreenLabels = new Dictionary<Button, string>();

	private readonly Dictionary<Button, XWUiMotion> _navigationMotions = new Dictionary<Button, XWUiMotion>();

	private XWUiMotion _topHudMotion;

	private XWUiMotion _workspaceHudMotion;

	private string _activeResourceKey = "";

	private bool _compactMode;

	public MenuBar MenuBar => _menuBar;

	public HBoxContainer RunBarContainer => _runBarContainer;

	public bool IsCompactMode => _compactMode;

	public Button MotionModeButton => _motionModeButton;

	public bool IsReducedMotion => XWUiMotion.ReducedMotion;

	public bool IsLowPerformanceMode => XWUiMotion.LowPerformanceMode;

	public override void _Ready()
	{
		_menuBar = GetNode<MenuBar>("%MenuBar");
		_brandLabel = GetNodeOrNull<Label>("%BrandLabel");
		_brandPanel = GetNodeOrNull<PanelContainer>("%BrandPanel");
		_projectStatusPanel = GetNodeOrNull<PanelContainer>("%ProjectStatusPanel");
		_projectStatusIcon = GetNodeOrNull<TextureRect>("%ProjectStatusIcon");
		_projectTitle = GetNodeOrNull<Label>("%ProjectTitle");
		_workspaceLabel = GetNodeOrNull<Label>("%WorkspaceLabel");
		_mainScreenButtons = GetNode<HBoxContainer>("%MainScreenButtons");
		_blueprintButton = GetNode<Button>("%BlueprintButton");
		_twoDButton = GetNodeOrNull<Button>("%2DButton");
		_scriptButton = GetNode<Button>("%ScriptButton");
		_resourceHubButton = GetNodeOrNull<Button>("%ResourceHubButton");
		_motionModeButton = GetNodeOrNull<Button>("%MotionModeButton");
		_runBarContainer = GetNode<HBoxContainer>("%RunBarContainer");
		_runStatusPanel = GetNodeOrNull<PanelContainer>("%RunStatusPanel");
		_runStatusIcon = GetNodeOrNull<TextureRect>("%RunStatusIcon");
		_runStatusLabel = GetNodeOrNull<Label>("%RunStatusLabel");
		SetupResourceWorkspacePalette();
		PrepareMainScreenButton(_blueprintButton, "blueprint", _blueprintButton.Text);
		PrepareMainScreenButton(_twoDButton, "2d", _twoDButton?.Text ?? "2D");
		PrepareMainScreenButton(_scriptButton, "script", _scriptButton.Text);
		SetupMotionControls();
		_runBarContainer.ChildEnteredTree += OnRunBarChildEnteredTree;
		base.Resized += UpdateResponsiveLayout;
		SetProjectTitle("");
		UpdateRunStatus(running: false, paused: false, 0);
		UpdateResponsiveLayout();
		CallDeferred("BindRunBarIfAvailable");
		CallDeferred("PlayInitialHudReveal");
	}

	public override void _ExitTree()
	{
		base.Resized -= UpdateResponsiveLayout;
		if (GodotObject.IsInstanceValid(_runBarContainer))
		{
			_runBarContainer.ChildEnteredTree -= OnRunBarChildEnteredTree;
		}
		UnbindRunBar();
		base._ExitTree();
	}

	public Button AddMainScreenButton(string key, string text, Texture2D icon = null)
	{
		if (_mainScreenButtons == null || string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		foreach (Node child in _mainScreenButtons.GetChildren())
		{
			if (child is Button button && button.HasMeta("_main_screen_key") && button.GetMeta("_main_screen_key").AsString() == key)
			{
				return button;
			}
		}
		Button button2 = new Button
		{
			Icon = icon,
			ToggleMode = true,
			ThemeTypeVariation = "GameNavCard"
		};
		_mainScreenButtons.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		PrepareMainScreenButton(button2, key, text);
		UpdateMainScreenButtonPresentation(button2);
		return button2;
	}

	public void ConfigureMotion(bool reducedMotion, bool lowPerformanceMode)
	{
		XWUiMotion.ConfigurePolicy(reducedMotion, lowPerformanceMode);
		if (GodotObject.IsInstanceValid(_motionModeButton))
		{
			_motionModeButton.SetPressedNoSignal(!reducedMotion && !lowPerformanceMode);
			_motionModeButton.Disabled = lowPerformanceMode;
			UpdateMotionTooltip();
		}
	}

	public void AddResourceEditorOption(string key, string text, Texture2D icon = null)
	{
		if (GodotObject.IsInstanceValid(_resourcePalette) && !string.IsNullOrWhiteSpace(key))
		{
			string label = (string.IsNullOrWhiteSpace(text) ? "资源" : text.Trim());
			_resourcePalette.AddWorkspace(key, label, icon);
			if (GodotObject.IsInstanceValid(_resourceHubButton))
			{
				_resourceHubButton.Disabled = false;
			}
		}
	}

	public void SetProjectTitle(string title)
	{
		string text = title?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = FindAncestorPanel()?.GetCurrentProject()?.Name?.Trim() ?? "";
		}
		bool flag = !string.IsNullOrWhiteSpace(text);
		string text2 = (flag ? ShortenStatusText(text, 20) : "项目待加载");
		if (GodotObject.IsInstanceValid(_projectTitle))
		{
			_projectTitle.Text = text2;
		}
		if (GodotObject.IsInstanceValid(_projectStatusPanel))
		{
			_projectStatusPanel.ThemeTypeVariation = (flag ? "HudStatusReady" : "HudStatusIdle");
			_projectStatusPanel.TooltipText = (flag ? ("当前 Mod 项目：" + text) : "尚未打开 Mod 项目");
			_projectStatusPanel.Visible = !_compactMode;
		}
		if (GodotObject.IsInstanceValid(_projectStatusIcon))
		{
			_projectStatusIcon.Modulate = (flag ? ModEditorTheme.LeafColor.Lightened(0.25f) : ModEditorTheme.DimTextColor);
		}
	}

	private void SetupResourceWorkspacePalette()
	{
		if (GodotObject.IsInstanceValid(_resourceHubButton))
		{
			Texture2D icon = (ResourceLoader.Exists("res://addons/ModEditor/Icons/ResourceMap.svg") ? XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceMap.svg", null, ResourceLoader.CacheMode.Reuse)) : null);
			_resourceHubButton.Icon = icon;
			_resourceHubButton.Disabled = true;
			_resourceHubButton.Pressed += OpenResourceWorkspacePalette;
			_navigationMotions[_resourceHubButton] = XWUiMotion.BindButton(_resourceHubButton);
			if (_resourcePaletteScene == null)
			{
				_resourcePaletteScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWResourceWorkspacePalette.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_resourcePalette = _resourcePaletteScene?.Instantiate<XWResourceWorkspacePalette>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_resourcePalette))
			{
				AddChild(_resourcePalette, forceReadableName: false, InternalMode.Disabled);
				_resourcePalette.WorkspaceSelected += SelectResourceWorkspace;
				_resourcePalette.PaletteDismissed += RestoreResourceHubState;
			}
		}
	}

	private void OpenResourceWorkspacePalette()
	{
		if (GodotObject.IsInstanceValid(_resourcePalette))
		{
			_resourcePalette.OpenPalette();
		}
	}

	private void SelectResourceWorkspace(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			_activeResourceKey = key;
			if (GodotObject.IsInstanceValid(_resourceHubButton))
			{
				_resourceHubButton.SetPressedNoSignal(pressed: true);
				_resourceHubButton.TooltipText = "当前资源工作台：" + key + "\n点击选择其他可视资源面板";
				XWUiMotion.FindBinding(_resourceHubButton)?.SyncButtonState();
			}
			OnScreenSelected(key, _resourceHubButton);
		}
	}

	private void ResetResourceEditorOption()
	{
		_activeResourceKey = "";
		RestoreResourceHubState();
	}

	private void RestoreResourceHubState()
	{
		if (GodotObject.IsInstanceValid(_resourceHubButton))
		{
			_resourceHubButton.SetPressedNoSignal(!string.IsNullOrWhiteSpace(_activeResourceKey));
			XWUiMotion.FindBinding(_resourceHubButton)?.SyncButtonState();
			if (string.IsNullOrWhiteSpace(_activeResourceKey))
			{
				_resourceHubButton.TooltipText = "用搜索和图块选择可视资源工作台";
			}
		}
	}

	private void PrepareMainScreenButton(Button button, string key, string label)
	{
		if (button != null)
		{
			label = (string.IsNullOrWhiteSpace(label) ? key : label.Trim());
			button.SetMeta("_main_screen_key", key);
			button.SetMeta("_hud_label", label);
			button.TooltipText = "切换到" + label + "工作区";
			_mainScreenLabels[button] = label;
			_navigationMotions[button] = XWUiMotion.BindButton(button);
			BindMainScreenButton(button, key);
			UpdateMainScreenButtonPresentation(button);
		}
	}

	private void BindMainScreenButton(Button button, string key)
	{
		if (button == null)
		{
			return;
		}
		button.Toggled += delegate(bool pressed)
		{
			if (pressed)
			{
				ResetResourceEditorOption();
				OnScreenSelected(key, button);
			}
		};
	}

	private void OnScreenSelected(string key, Button active)
	{
		foreach (Node child in _mainScreenButtons.GetChildren())
		{
			if (child is Button button && button != active && button.ToggleMode)
			{
				button.SetPressedNoSignal(pressed: false);
				XWUiMotion.FindBinding(button)?.SyncButtonState();
			}
		}
		XWUiMotion.FindBinding(active)?.SyncButtonState();
		_workspaceHudMotion?.PlayPanelReveal(4f);
		EmitSignal(SignalName.MainScreenChanged, key);
	}

	private void SetupMotionControls()
	{
		_topHudMotion = XWUiMotion.BindPanel(GetNodeOrNull<PanelContainer>("%TopHudSurface"));
		_workspaceHudMotion = XWUiMotion.BindPanel(GetNodeOrNull<PanelContainer>("%WorkspaceHudSurface"));
		if (GodotObject.IsInstanceValid(_motionModeButton))
		{
			_motionModeButton.SetPressedNoSignal(XWUiMotion.MotionAllowed);
			_motionModeButton.Toggled += OnMotionModeToggled;
			_navigationMotions[_motionModeButton] = XWUiMotion.BindButton(_motionModeButton);
			UpdateMotionTooltip();
		}
	}

	private void OnMotionModeToggled(bool enabled)
	{
		if (XWUiMotion.LowPerformanceMode)
		{
			_motionModeButton?.SetPressedNoSignal(pressed: false);
			UpdateMotionTooltip();
			return;
		}
		bool flag = !enabled;
		XWUiMotion.SetReducedMotion(flag);
		XWEditorSettings xWEditorSettings = XWEditorInterface.Instance?.GetEditorSettings();
		if (xWEditorSettings != null)
		{
			xWEditorSettings.SetSetting("interface/reduced_motion", flag);
			xWEditorSettings.Save();
		}
		UpdateMotionTooltip();
	}

	private void UpdateMotionTooltip()
	{
		if (GodotObject.IsInstanceValid(_motionModeButton))
		{
			_motionModeButton.TooltipText = (XWUiMotion.LowPerformanceMode ? "低性能模式已启用：界面动效自动关闭" : (XWUiMotion.ReducedMotion ? "低动效已启用；点击恢复卡片与面板过渡" : "界面动效已启用；点击切换为低动效"));
		}
	}

	private void PlayInitialHudReveal()
	{
		_topHudMotion?.PlayPanelReveal(6f);
		_workspaceHudMotion?.PlayPanelReveal(8f);
	}

	private void UpdateResponsiveLayout()
	{
		bool flag = base.Size.X > 1f && base.Size.X < 1020f;
		if (_compactMode == flag && base.Size.X > 1f)
		{
			return;
		}
		_compactMode = flag;
		if (GodotObject.IsInstanceValid(_brandLabel))
		{
			_brandLabel.Visible = !flag;
		}
		if (GodotObject.IsInstanceValid(_brandPanel))
		{
			_brandPanel.CustomMinimumSize = new Vector2(flag ? 44f : 138f, 38f);
		}
		if (GodotObject.IsInstanceValid(_projectStatusPanel))
		{
			_projectStatusPanel.Visible = !flag;
		}
		if (GodotObject.IsInstanceValid(_workspaceLabel))
		{
			_workspaceLabel.Visible = !flag;
		}
		if (GodotObject.IsInstanceValid(_runStatusPanel))
		{
			_runStatusPanel.Visible = !flag;
		}
		foreach (Button key in _mainScreenLabels.Keys)
		{
			UpdateMainScreenButtonPresentation(key);
		}
		UpdateResourceOptionPresentation();
	}

	private void UpdateMainScreenButtonPresentation(Button button)
	{
		if (GodotObject.IsInstanceValid(button) && _mainScreenLabels.TryGetValue(button, out var value))
		{
			button.Text = (_compactMode ? "" : value);
			button.CustomMinimumSize = new Vector2(_compactMode ? 42f : 92f, 38f);
		}
	}

	private void UpdateResourceOptionPresentation()
	{
		if (GodotObject.IsInstanceValid(_resourceHubButton))
		{
			_resourceHubButton.Text = (_compactMode ? "" : "资源");
			_resourceHubButton.CustomMinimumSize = new Vector2(_compactMode ? 48f : 132f, 38f);
		}
	}

	private void OnRunBarChildEnteredTree(Node child)
	{
		if (child is XWEditorRunBar)
		{
			CallDeferred("BindRunBarIfAvailable");
		}
	}

	private void BindRunBarIfAvailable()
	{
		XWEditorRunBar xWEditorRunBar = null;
		foreach (Node child in _runBarContainer.GetChildren())
		{
			if (child is XWEditorRunBar xWEditorRunBar2)
			{
				xWEditorRunBar = xWEditorRunBar2;
				break;
			}
		}
		if (GodotObject.IsInstanceValid(xWEditorRunBar) && xWEditorRunBar != _boundRunBar)
		{
			UnbindRunBar();
			_boundRunBar = xWEditorRunBar;
			_boundRunBar.RunStatusChanged += UpdateRunStatus;
			UpdateRunStatus(_boundRunBar.IsRunning(), _boundRunBar.IsPaused(), (int)_boundRunBar.GetRunMode());
		}
	}

	private void UnbindRunBar()
	{
		if (GodotObject.IsInstanceValid(_boundRunBar))
		{
			_boundRunBar.RunStatusChanged -= UpdateRunStatus;
		}
		_boundRunBar = null;
	}

	private void UpdateRunStatus(bool running, bool paused, int mode)
	{
		string text = (paused ? "已暂停" : (running ? "运行中" : "待机"));
		string text2 = (paused ? "HudStatusPaused" : (running ? "HudStatusRunning" : "HudStatusIdle"));
		if (GodotObject.IsInstanceValid(_runStatusLabel))
		{
			_runStatusLabel.Text = text;
		}
		if (GodotObject.IsInstanceValid(_runStatusPanel))
		{
			_runStatusPanel.ThemeTypeVariation = text2;
			_runStatusPanel.TooltipText = (running ? $"游戏预览状态：{text}（模式 {(XWEditorRunBar.RunMode)mode}）" : "游戏预览尚未运行");
		}
		if (GodotObject.IsInstanceValid(_runStatusIcon))
		{
			_runStatusIcon.Modulate = (paused ? new Color(1f, 0.63f, 0.25f) : (running ? ModEditorTheme.SunGoldColor : ModEditorTheme.DimTextColor));
		}
	}

	private ModEditorPanel FindAncestorPanel()
	{
		Node parent = GetParent();
		while (GodotObject.IsInstanceValid(parent))
		{
			if (parent is ModEditorPanel result)
			{
				return result;
			}
			parent = parent.GetParent();
		}
		return null;
	}

	private static string ShortenStatusText(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxLength - 1)) + "…";
	}
}
