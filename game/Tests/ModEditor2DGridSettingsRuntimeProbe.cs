using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DGridSettingsRuntimeProbe : Node
{
	private const string SceneAPath = "user://mod_editor_2d_grid_settings_a.tscn";

	private const string SceneBPath = "user://mod_editor_2d_grid_settings_b.tscn";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		bool f3 = false;
		bool window = false;
		bool popup = false;
		bool visual = false;
		bool preset = false;
		bool direct = false;
		bool viewportSync = false;
		bool algorithms = false;
		bool tabIsolation = false;
		bool newTabDefault = false;
		bool persisted = false;
		bool inspectorUntouched = false;
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法创建 ModEditorManager。");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish(f3: false, window: false, popup: false, visual: false, preset: false, direct: false, viewportSync: false, algorithms: false, tabIsolation: false, newTabDefault: false, persisted: false, inspectorUntouched: false);
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
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			f3 = GodotObject.IsInstanceValid(editor);
			window = FindAncestorWindow(editor) != null;
			Require(f3 && window, "真实 F3 Mod 编辑器窗口中没有 2D 编辑器。");
			Require(CreateFixture("user://mod_editor_2d_grid_settings_a.tscn", "GridSceneA") && CreateFixture("user://mod_editor_2d_grid_settings_b.tscn", "GridSceneB"), "无法创建临时 2D 场景。");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish(f3, window, popup: false, visual: false, preset: false, direct: false, viewportSync: false, algorithms: false, tabIsolation: false, newTabDefault: false, persisted: false, inspectorUntouched: false);
				return;
			}
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_grid_settings_a.tscn");
			await WaitFrames(8);
			XW2DViewport viewport = editor.GetNodeOrNull<XW2DViewport>("%Viewport2D");
			PopupPanel settingsPopup = editor.FindChild("GridSettingsPopup", recursive: true, owned: false) as PopupPanel;
			XW2DGridSettingsPanel panel = editor.FindChild("GridSettingsPanel", recursive: true, owned: false) as XW2DGridSettingsPanel;
			MenuButton snapMenu = editor.FindChild("SnapConfigMenu", recursive: true, owned: false) as MenuButton;
			TabBar tabs = editor.FindChild("SceneTabBar", recursive: true, owned: false) as TabBar;
			XWEditorSettings settings = XWEditorInterface.Instance?.GetEditorSettings();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node inspectorSentinel = new Node
			{
				Name = "GridSettingsInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			Require(GodotObject.IsInstanceValid(viewport), "真实 2D Viewport 缺失。");
			Require(GodotObject.IsInstanceValid(settingsPopup) && GodotObject.IsInstanceValid(panel) && GodotObject.IsInstanceValid(snapMenu), "网格设置弹窗或面板缺失。");
			Require(GodotObject.IsInstanceValid(tabs), "场景页签栏缺失。");
			if (!GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(settingsPopup) || !GodotObject.IsInstanceValid(panel) || !GodotObject.IsInstanceValid(snapMenu) || !GodotObject.IsInstanceValid(tabs))
			{
				Finish(f3, window, popup: false, visual: false, preset: false, direct: false, viewportSync: false, algorithms: false, tabIsolation: false, newTabDefault: false, persisted: false, inspectorUntouched: false);
				return;
			}
			snapMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 6L);
			await WaitFrames(4);
			popup = settingsPopup.Visible;
			Button button = panel.FindChild("Preset0", recursive: true, owned: false) as Button;
			visual = popup && GodotObject.IsInstanceValid(panel.FindChild("GridSettingsSurface", recursive: true, owned: false)) && GodotObject.IsInstanceValid(panel.FindChild("GridPreview", recursive: true, owned: false)) && GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(panel.FindChild("Preset3", recursive: true, owned: false)) && panel.FindChild("Inspector", recursive: true, owned: false) == null;
			Require(popup, "网格设置菜单没有打开 GridSettingsPopup。");
			Require(visual, "网格设置面板没有建立预览、预设图块或中文直编表面。");
			int settingsChanged = 0;
			panel.SettingsChanged += delegate
			{
				settingsChanged++;
			};
			button?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			preset = panel.GridStep.IsEqualApprox(new Vector2(4f, 4f)) && panel.MoveSnapStep.IsEqualApprox(new Vector2(4f, 4f)) && viewport.GridStep.IsEqualApprox(new Vector2(4f, 4f)) && viewport.MoveSnapStep.IsEqualApprox(new Vector2(4f, 4f));
			Require(preset, "预设图块没有通过 SettingsChanged 更新真实 Viewport。");
			Vector2 aOffset = new Vector2(3f, 5f);
			Vector2 aGrid = new Vector2(12f, 20f);
			Vector2I aPrimary = new Vector2I(3, 5);
			Vector2 aMove = new Vector2(8f, 10f);
			panel.GridOffset = aOffset;
			panel.GridStep = aGrid;
			panel.PrimaryGridStep = aPrimary;
			panel.MoveSnapStep = aMove;
			panel.RotateSnapStep = 15f;
			panel.ScaleSnapStep = 0.25f;
			panel.SmartSnapThreshold = 11f;
			await WaitFrames(4);
			Dictionary dictionary = panel.CaptureState();
			direct = settingsChanged >= 7 && dictionary["grid_offset"].AsVector2().IsEqualApprox(aOffset) && dictionary["grid_step"].AsVector2().IsEqualApprox(aGrid) && dictionary["primary_grid_step"].AsVector2I() == aPrimary && dictionary["move_snap_step"].AsVector2().IsEqualApprox(aMove) && Same(dictionary["rotate_snap_step"].AsSingle(), 15f) && Same(dictionary["scale_snap_step"].AsSingle(), 0.25f) && Same(dictionary["smart_snap_threshold"].AsSingle(), 11f);
			viewportSync = viewport.GridOffset.IsEqualApprox(aOffset) && viewport.GridStep.IsEqualApprox(aGrid) && viewport.PrimaryGridStep == aPrimary && viewport.MoveSnapStep.IsEqualApprox(aMove) && Same(viewport.RotationSnapStepDegrees, 15f) && Same(viewport.ScaleSnapStep, 0.25f) && Same(viewport.SmartSnapThreshold, 11f);
			Require(direct, "面板属性直编状态或 SettingsChanged 次数不正确。");
			Require(viewportSync, "面板设置没有完整同步到真实 Viewport。");
			viewport.GridSnapActive = true;
			Vector2 vector = viewport.TestApplyGridSnapToWorld(new Vector2(17f, 27f));
			Vector2 vector2 = viewport.TestApplyMoveSnap(new Vector2(6f, 9f), new Vector2(10f, 13f));
			float left = viewport.TestApplyRotationSnap(Mathf.DegToRad(22f));
			float left2 = viewport.TestApplyScaleSnap(1.38f);
			algorithms = vector.IsEqualApprox(new Vector2(15f, 25f)) && vector2.IsEqualApprox(new Vector2(13f, 16f)) && Same(left, Mathf.DegToRad(15f)) && Same(left2, 1.5f);
			Require(algorithms, "绝对网格、独立移动步长、旋转或缩放生产吸附算法不正确。");
			viewport.SetZoom(2.4f);
			viewport.CenterAt(new Vector2(123f, 77f));
			settingsPopup.Hide();
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_grid_settings_b.tscn");
			await WaitFrames(8);
			newTabDefault = tabs.TabCount == 2 && Same(viewport.Zoom, 1f) && viewport.ViewOffset.IsEqualApprox(Vector2.Zero) && viewport.GridOffset.IsEqualApprox(aOffset) && viewport.MoveSnapStep.IsEqualApprox(aMove);
			Require(newTabDefault, "新场景页签继承了旧页签 zoom/ofs，或没有采用持久化网格默认值。");
			Vector2 bOffset = new Vector2(-4f, 7f);
			Vector2 bGrid = new Vector2(16f, 24f);
			Vector2I bPrimary = new Vector2I(4, 6);
			Vector2 bMove = new Vector2(6f, 9f);
			snapMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 6L);
			await WaitFrames(3);
			panel.GridOffset = bOffset;
			panel.GridStep = bGrid;
			panel.PrimaryGridStep = bPrimary;
			panel.MoveSnapStep = bMove;
			panel.RotateSnapStep = 30f;
			panel.ScaleSnapStep = 0.2f;
			panel.SmartSnapThreshold = 14f;
			viewport.SetZoom(1.7f);
			viewport.CenterAt(new Vector2(-70f, 32f));
			await WaitFrames(3);
			tabs.CurrentTab = 0;
			await WaitFrames(6);
			bool restoredA = viewport.GridOffset.IsEqualApprox(aOffset) && viewport.GridStep.IsEqualApprox(aGrid) && viewport.PrimaryGridStep == aPrimary && viewport.MoveSnapStep.IsEqualApprox(aMove) && Same(viewport.RotationSnapStepDegrees, 15f) && Same(viewport.ScaleSnapStep, 0.25f) && Same(viewport.SmartSnapThreshold, 11f) && Same(viewport.Zoom, 2.4f) && viewport.ViewOffset.IsEqualApprox(new Vector2(123f, 77f));
			tabs.CurrentTab = 1;
			await WaitFrames(6);
			bool flag = viewport.GridOffset.IsEqualApprox(bOffset) && viewport.GridStep.IsEqualApprox(bGrid) && viewport.PrimaryGridStep == bPrimary && viewport.MoveSnapStep.IsEqualApprox(bMove) && Same(viewport.RotationSnapStepDegrees, 30f) && Same(viewport.ScaleSnapStep, 0.2f) && Same(viewport.SmartSnapThreshold, 14f) && Same(viewport.Zoom, 1.7f) && viewport.ViewOffset.IsEqualApprox(new Vector2(-70f, 32f));
			tabIsolation = restoredA && flag;
			Require(tabIsolation, "两个 2D 页签的网格、吸附或视图状态发生串扰。");
			XWEditorSettings xWEditorSettings = new XWEditorSettings();
			xWEditorSettings.Load();
			persisted = GodotObject.IsInstanceValid(settings) && settings.GetSetting("2d_editor_grid/offset", Vector2.Zero).IsEqualApprox(bOffset) && settings.GetSetting("2d_editor_grid/grid_step", Vector2.Zero).IsEqualApprox(bGrid) && settings.GetSetting("2d_editor_grid/primary_step", Vector2I.Zero) == bPrimary && settings.GetSetting("2d_editor_grid/move_step", Vector2.Zero).IsEqualApprox(bMove) && Same(settings.GetSetting("2d_editor_grid/rotation_degrees", 0f), 30f) && Same(settings.GetSetting("2d_editor_grid/scale_step", 0f), 0.2f) && Same(settings.GetSetting("2d_editor_grid/smart_threshold", 0f), 14f) && xWEditorSettings.GetSetting("2d_editor_grid/offset", Vector2.Zero).IsEqualApprox(bOffset) && xWEditorSettings.GetSetting("2d_editor_grid/move_step", Vector2.Zero).IsEqualApprox(bMove) && Same(xWEditorSettings.GetSetting("2d_editor_grid/rotation_degrees", 0f), 30f);
			Require(persisted, "网格与吸附配置没有写入并从 XWEditorSettings 重载。");
			inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "网格设置面板占用了原始 Inspector。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish(f3, window, popup, visual, preset, direct, viewportSync, algorithms, tabIsolation, newTabDefault, persisted, inspectorUntouched);
	}

	private static bool CreateFixture(string path, string rootName)
	{
		Node2D node2D = new Node2D
		{
			Name = rootName
		};
		Node2D node2D2 = new Node2D
		{
			Name = "Marker",
			Position = new Vector2(40f, 30f)
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		PackedScene packedScene = new PackedScene();
		Error error = packedScene.Pack(node2D);
		Error error2 = ((error == Error.Ok) ? ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) : error);
		node2D.Free();
		if (error == Error.Ok)
		{
			return error2 == Error.Ok;
		}
		return false;
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = FindNodeOfType<XW2DSceneEditor>(GetTree().Root);
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return null;
	}

	private async Task WaitFrames(int frames)
	{
		for (int frame = 0; frame < frames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
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

	private static bool Same(float left, float right)
	{
		return Mathf.IsEqualApprox(left, right);
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish(bool f3, bool window, bool popup, bool visual, bool preset, bool direct, bool viewportSync, bool algorithms, bool tabIsolation, bool newTabDefault, bool persisted, bool inspectorUntouched)
	{
		foreach (string failure in _failures)
		{
			GD.Print("[MOD_EDITOR_2D_GRID_SETTINGS_FAILURE] " + failure);
		}
		GD.Print($"[MOD_EDITOR_2D_GRID_SETTINGS_PROBE] f3={f3} window={window} popup={popup} visual={visual} preset={preset} direct={direct} viewportSync={viewportSync} algorithms={algorithms} tabIsolation={tabIsolation} newTabDefault={newTabDefault} persisted={persisted} inspectorUntouched={inspectorUntouched} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
