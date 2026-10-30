using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.RunBar;

public partial class ModEditorHudTitleBarRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private Control _editorPanel;

	private XWEditorTitleBar _titleBar;

	public override async void _Ready()
	{
		_ = 9;
		try
		{
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			bool flag = await WaitForTitleBar(900);
			Require(flag, "F3 did not initialize the real ModEditor HUD title bar.");
			if (!flag)
			{
				Finish();
				return;
			}
			Window instance = FindAncestorWindow(_titleBar);
			bool window = GodotObject.IsInstanceValid(instance);
			Require(window, "HUD title bar is not mounted in the real F3 editor window.");
			PanelContainer nodeOrNull = _titleBar.GetNodeOrNull<PanelContainer>("%TopHudSurface");
			PanelContainer nodeOrNull2 = _titleBar.GetNodeOrNull<PanelContainer>("%WorkspaceHudSurface");
			StyleBox styleBox = nodeOrNull?.GetThemeStylebox("panel");
			StyleBox styleBox2 = nodeOrNull2?.GetThemeStylebox("panel");
			bool layeredHud = GodotObject.IsInstanceValid(nodeOrNull) && GodotObject.IsInstanceValid(nodeOrNull2) && styleBox is StyleBoxFlat && styleBox2 is StyleBoxFlat && nodeOrNull.GetGlobalRect().End.Y <= nodeOrNull2.GetGlobalRect().Position.Y + 1f;
			Require(layeredHud, "Title bar did not mount two distinct styled HUD layers.");
			Button blueprint = _titleBar.GetNodeOrNull<Button>("%BlueprintButton");
			Button twoD = _titleBar.GetNodeOrNull<Button>("%2DButton");
			Button nodeOrNull3 = _titleBar.GetNodeOrNull<Button>("%ScriptButton");
			Button nodeOrNull4 = _titleBar.GetNodeOrNull<Button>("%ResourceHubButton");
			TextureRect nodeOrNull5 = _titleBar.GetNodeOrNull<TextureRect>("%BrandLogo");
			bool iconized = GodotObject.IsInstanceValid(nodeOrNull5) && GodotObject.IsInstanceValid(nodeOrNull5.Texture) && GodotObject.IsInstanceValid(blueprint?.Icon) && GodotObject.IsInstanceValid(twoD?.Icon) && GodotObject.IsInstanceValid(nodeOrNull3?.Icon) && GodotObject.IsInstanceValid(nodeOrNull4?.Icon);
			Require(iconized, "HUD navigation or resource entry is missing its visual icon.");
			bool chineseUi = await VerifyChineseUi();
			Require(chineseUi, "The real ModEditor still exposes English-only labels in its primary creation and runtime controls.");
			_titleBar.SetProjectTitle("向日葵实验室");
			await WaitFrames(2);
			PanelContainer nodeOrNull6 = _titleBar.GetNodeOrNull<PanelContainer>("%ProjectStatusPanel");
			Label nodeOrNull7 = _titleBar.GetNodeOrNull<Label>("%ProjectTitle");
			bool projectBadge = GodotObject.IsInstanceValid(nodeOrNull6) && nodeOrNull6.Visible && nodeOrNull6.ThemeTypeVariation == (StringName)"HudStatusReady" && nodeOrNull7?.Text == "向日葵实验室" && !string.IsNullOrWhiteSpace(nodeOrNull6.TooltipText);
			Require(projectBadge, "Project status did not switch to the ready HUD badge.");
			XWEditorRunBar runBar = FindNodeOfType<XWEditorRunBar>(_titleBar);
			PanelContainer runPanel = _titleBar.GetNodeOrNull<PanelContainer>("%RunStatusPanel");
			Label runLabel = _titleBar.GetNodeOrNull<Label>("%RunStatusLabel");
			bool runBadge = false;
			if (GodotObject.IsInstanceValid(runBar) && GodotObject.IsInstanceValid(runPanel))
			{
				runBar.SetRunMode(XWEditorRunBar.RunMode.RunCurrent);
				await WaitFrames(2);
				bool running = runLabel?.Text == "运行中" && runPanel.ThemeTypeVariation == (StringName)"HudStatusRunning";
				runBar.SetPaused(paused: true);
				await WaitFrames(2);
				bool paused = runLabel?.Text == "已暂停" && runPanel.ThemeTypeVariation == (StringName)"HudStatusPaused";
				runBar.SetRunning(running: false);
				await WaitFrames(2);
				bool flag2 = runLabel?.Text == "待机" && runPanel.ThemeTypeVariation == (StringName)"HudStatusIdle";
				runBadge = running && paused && flag2;
			}
			Require(runBadge, "Run HUD badge did not follow running, paused and stopped states.");
			string selectedScreen = "";
			_titleBar.MainScreenChanged += delegate(string key)
			{
				selectedScreen = key;
			};
			if (GodotObject.IsInstanceValid(twoD))
			{
				twoD.SetPressedNoSignal(pressed: true);
				twoD.EmitSignal(BaseButton.SignalName.Toggled, true);
				await WaitFrames(2);
			}
			int num;
			if (selectedScreen == "2d" && (twoD?.ButtonPressed ?? false))
			{
				num = ((blueprint != null && !blueprint.ButtonPressed) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool screenSwitch = (byte)num != 0;
			Require(screenSwitch, "Iconized 2D workspace button did not switch the active main screen.");
			XWResourceWorkspacePalette xWResourceWorkspacePalette = _titleBar.FindChild("XWResourceWorkspacePalette", recursive: true, owned: false) as XWResourceWorkspacePalette;
			int count = XWResourceEditorRegistry.GetAllEditors().Count;
			bool paletteNavigation = GodotObject.IsInstanceValid(xWResourceWorkspacePalette) && xWResourceWorkspacePalette.EntryCount == count && xWResourceWorkspacePalette.TrySelectWorkspace("animation_editor") && selectedScreen == "animation_editor";
			Require(paletteNavigation, $"Visual resource palette did not expose and route all registered workspaces (entries={xWResourceWorkspacePalette?.EntryCount ?? 0}, expected={count}).");
			bool inspectorBefore = HasVisibleRawInspector(_editorPanel);
			bool compact = false;
			bool compactFit = false;
			XWEditorTitleBar compactTitleBar = null;
			SubViewport compactViewport = null;
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWEditorTitleBar.tscn", null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(packedScene))
			{
				compactViewport = new SubViewport
				{
					Size = new Vector2I(760, 140),
					Disable3D = true,
					TransparentBg = true
				};
				AddChild(compactViewport, forceReadableName: false, InternalMode.Disabled);
				Control control = new Control
				{
					Size = new Vector2(760f, 140f)
				};
				compactViewport.AddChild(control, forceReadableName: false, InternalMode.Disabled);
				compactTitleBar = packedScene.Instantiate<XWEditorTitleBar>(PackedScene.GenEditState.Disabled);
				control.AddChild(compactTitleBar, forceReadableName: false, InternalMode.Disabled);
				compactTitleBar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
				ModEditorTheme.ApplyTo(compactTitleBar);
				await WaitFrames(8);
				Button nodeOrNull8 = compactTitleBar.GetNodeOrNull<Button>("%BlueprintButton");
				Button nodeOrNull9 = compactTitleBar.GetNodeOrNull<Button>("%2DButton");
				Button nodeOrNull10 = compactTitleBar.GetNodeOrNull<Button>("%ScriptButton");
				Button nodeOrNull11 = compactTitleBar.GetNodeOrNull<Button>("%ResourceHubButton");
				PanelContainer nodeOrNull12 = compactTitleBar.GetNodeOrNull<PanelContainer>("%ProjectStatusPanel");
				PanelContainer nodeOrNull13 = compactTitleBar.GetNodeOrNull<PanelContainer>("%RunStatusPanel");
				PanelContainer nodeOrNull14 = compactTitleBar.GetNodeOrNull<PanelContainer>("%TopHudSurface");
				PanelContainer nodeOrNull15 = compactTitleBar.GetNodeOrNull<PanelContainer>("%WorkspaceHudSurface");
				Control nodeOrNull16 = compactTitleBar.GetNodeOrNull<Control>("%TopRow");
				Control nodeOrNull17 = compactTitleBar.GetNodeOrNull<Control>("%WorkspaceRow");
				compact = compactTitleBar.IsCompactMode && nodeOrNull8?.Text == "" && nodeOrNull9?.Text == "" && nodeOrNull10?.Text == "" && !string.IsNullOrWhiteSpace(nodeOrNull9?.TooltipText) && nodeOrNull11?.Text == "" && nodeOrNull12 != null && !nodeOrNull12.Visible && nodeOrNull13 != null && !nodeOrNull13.Visible;
				compactFit = FitsWithinWidth(nodeOrNull16, nodeOrNull14) && FitsWithinWidth(nodeOrNull17, nodeOrNull15) && nodeOrNull14.GetGlobalRect().End.Y <= nodeOrNull15.GetGlobalRect().Position.Y + 1f;
			}
			Require(compact, $"760px HUD did not collapse navigation to icons with accessible tooltips (compact={compactTitleBar?.IsCompactMode}).");
			Require(compactFit, "Narrow HUD rows exceeded their visual surfaces or overlapped each other.");
			compactViewport?.QueueFree();
			await WaitFrames(2);
			bool flag3 = !inspectorBefore && !HasVisibleRawInspector(_editorPanel);
			Require(flag3, "HUD interaction opened a raw embedded Inspector.");
			GD.Print($"[MOD_EDITOR_HUD_TITLE_BAR_PROBE] window={window} layeredHud={layeredHud} iconized={iconized} palette={paletteNavigation} projectBadge={projectBadge} runBadge={runBadge} screenSwitch={screenSwitch} compact={compact} compactFit={compactFit} chineseUi={chineseUi} inspectorUntouched={flag3} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> WaitForTitleBar(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			_editorPanel = XWEditorInterface.Instance?.GetEditorPanel();
			_titleBar = FindNodeOfType<XWEditorTitleBar>(_editorPanel);
			Node instance = _editorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(_titleBar) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(FindNodeOfType<XWEditorRunBar>(_titleBar)))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> VerifyChineseUi()
	{
		Label brand = _titleBar.GetNodeOrNull<Label>("%BrandLabel");
		Button profiler = FindNodeOfType<XWEditorRunBar>(_titleBar)?.GetNodeOrNull<Button>("%ProfilerIndicator");
		Node log = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWEditorLog.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
		Node blueprintDialog = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWBlueprintCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
		Node settingsDialog = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ProjectSettings/GUI/XWProjectSettingsDialog.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(log))
		{
			AddChild(log, forceReadableName: false, InternalMode.Disabled);
		}
		if (GodotObject.IsInstanceValid(blueprintDialog))
		{
			AddChild(blueprintDialog, forceReadableName: false, InternalMode.Disabled);
		}
		if (GodotObject.IsInstanceValid(settingsDialog))
		{
			AddChild(settingsDialog, forceReadableName: false, InternalMode.Disabled);
		}
		await WaitFrames(3);
		bool flag = brand?.Text == "模组工坊";
		bool flag2 = _titleBar.GetNodeOrNull<PanelContainer>("%BrandPanel")?.TooltipText == "植物大战僵尸 Mod 制作中心";
		bool flag3 = profiler?.Text == "性能";
		bool flag4 = log?.GetNodeOrNull<Button>("%InfoFilterButton")?.Text == "信息" && log?.GetNodeOrNull<Button>("%WarningFilterButton")?.Text == "警告" && log?.GetNodeOrNull<Button>("%ErrorFilterButton")?.Text == "错误" && log?.GetNodeOrNull<Button>("%ScriptFilterButton")?.Text == "脚本" && log?.GetNodeOrNull<Button>("%CopyButton")?.Text == "复制" && log?.GetNodeOrNull<Button>("%ClearButton")?.Text == "清空";
		bool flag5 = blueprintDialog?.GetNodeOrNull<LineEdit>("%ParentName")?.PlaceholderText == "例如：Node（节点）";
		bool flag6 = settingsDialog?.GetNodeOrNull<Button>("%EnglishLanguageButton")?.Text == "英语" && settingsDialog.GetNodeOrNull<Button>("%EnglishLanguageButton")?.TooltipText == "英语界面";
		bool valid = flag && flag2 && flag3 && flag4 && flag5 && flag6;
		GD.Print($"[MOD_EDITOR_CHINESE_UI_TRACE] brand={flag} brandTip={flag2} profiler={flag3} log={flag4} blueprint={flag5} language={flag6}");
		log?.QueueFree();
		blueprintDialog?.QueueFree();
		settingsDialog?.QueueFree();
		await WaitFrames(2);
		return valid;
	}

	private static bool FitsWithinWidth(Control child, Control parent)
	{
		if (!GodotObject.IsInstanceValid(child) || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		Rect2 globalRect = child.GetGlobalRect();
		Rect2 globalRect2 = parent.GetGlobalRect();
		if (globalRect.Position.X >= globalRect2.Position.X - 1f)
		{
			return globalRect.End.X <= globalRect2.End.X + 1f;
		}
		return false;
	}

	private static bool HasVisibleRawInspector(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		if (root.Name.ToString().Contains("EmbeddedResourceInspector", StringComparison.OrdinalIgnoreCase) && root is Control control && control.IsVisibleInTree())
		{
			return true;
		}
		foreach (Node child in root.GetChildren())
		{
			if (HasVisibleRawInspector(child))
			{
				return true;
			}
		}
		return false;
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (root is T result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_HUD_TITLE_BAR_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_HUD_TITLE_BAR_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
