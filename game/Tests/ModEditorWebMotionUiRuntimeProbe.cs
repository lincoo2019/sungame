using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;

public partial class ModEditorWebMotionUiRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private Control _editorPanel;

	private XWEditorTitleBar _titleBar;

	private double _maxFrameGapMs;

	public override async void _Ready()
	{
		_ = 16;
		try
		{
			ModEditorManager manager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(manager))
			{
				manager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(manager))
				{
					AddChild(manager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(manager), "无法实例化 ModEditorManager。");
			if (!GodotObject.IsInstanceValid(manager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			ToggleEditorWithF3();
			bool f3 = await WaitForTitleBar(900);
			Require(f3, "F3 未打开真实 Mod 编辑器动态主界面。");
			if (!f3)
			{
				Finish();
				return;
			}
			Button blueprint = _titleBar.GetNodeOrNull<Button>("%BlueprintButton");
			Button twoD = _titleBar.GetNodeOrNull<Button>("%2DButton");
			Button script = _titleBar.GetNodeOrNull<Button>("%ScriptButton");
			Button resource = _titleBar.GetNodeOrNull<Button>("%ResourceHubButton");
			Button motionMode = _titleBar.GetNodeOrNull<Button>("%MotionModeButton");
			XWUiMotion blueprintMotion = XWUiMotion.FindBinding(blueprint);
			XWUiMotion twoDMotion = XWUiMotion.FindBinding(twoD);
			XWUiMotion scriptMotion = XWUiMotion.FindBinding(script);
			XWUiMotion resourceMotion = XWUiMotion.FindBinding(resource);
			XWUiMotion instance = XWUiMotion.FindBinding(motionMode);
			bool motionBound = GodotObject.IsInstanceValid(blueprintMotion) && GodotObject.IsInstanceValid(twoDMotion) && GodotObject.IsInstanceValid(scriptMotion) && GodotObject.IsInstanceValid(resourceMotion) && GodotObject.IsInstanceValid(instance);
			Require(motionBound, "标题栏关键导航控件没有全部绑定轻量动效。");
			int hoverBefore = twoDMotion?.AnimationStartCount ?? 0;
			twoD?.EmitSignal(Control.SignalName.MouseEntered);
			await TrackFrames(3);
			bool hover = (twoDMotion?.AnimationStartCount ?? 0) > hoverBefore && twoD.Scale.X > 1f;
			Require(hover, "导航卡片 hover 没有产生抬升/高亮过渡。");
			int pressBefore = twoDMotion?.AnimationStartCount ?? 0;
			twoD?.EmitSignal(BaseButton.SignalName.ButtonDown);
			await TrackFrames(2);
			bool pressedDown = (twoDMotion?.AnimationStartCount ?? 0) > pressBefore && twoD.Scale.X < 1f;
			twoD?.EmitSignal(BaseButton.SignalName.ButtonUp);
			await WaitSeconds(0.22);
			bool pressRebound = pressedDown && twoD.Scale.X >= 1f;
			Require(pressRebound, "导航卡片按下后没有完成回弹。");
			int selectedBefore = scriptMotion?.AnimationStartCount ?? 0;
			script?.SetPressedNoSignal(pressed: true);
			script?.EmitSignal(BaseButton.SignalName.Toggled, true);
			await TrackFrames(4);
			int num;
			if (script?.ButtonPressed ?? false)
			{
				if (blueprint != null && !blueprint.ButtonPressed)
				{
					num = (((scriptMotion?.AnimationStartCount ?? 0) > selectedBefore) ? 1 : 0);
					goto IL_066b;
				}
			}
			num = 0;
			goto IL_066b;
			IL_066b:
			bool selected = (byte)num != 0;
			Require(selected, "工作区选中切换没有触发可视过渡。");
			TabContainer centerDock = _editorPanel.GetNodeOrNull<TabContainer>("VBoxContainer/HSplitContainer/CenterPanel/CenterDock");
			XWUiMotion centerMotion = XWUiMotion.FindBinding(centerDock);
			int panelBefore = centerMotion?.AnimationStartCount ?? 0;
			Vector2 panelRestPosition = centerDock?.Position ?? Vector2.Zero;
			twoD?.SetPressedNoSignal(pressed: true);
			twoD?.EmitSignal(BaseButton.SignalName.Toggled, true);
			await TrackFrames(1);
			centerMotion?.PlayPanelReveal(9f);
			centerMotion?.PlayPanelReveal(9f);
			centerMotion?.PlayPanelReveal(9f);
			await WaitSeconds(0.24);
			bool panelReveal = GodotObject.IsInstanceValid(centerMotion) && centerMotion.AnimationStartCount >= panelBefore + 3 && NearlyEqual(centerDock.Position, panelRestPosition);
			Require(panelReveal, "主工作区切换过渡缺失，或连续切换后面板位置发生累积漂移。");
			XWResourceWorkspacePalette palette = FindNodeOfType<XWResourceWorkspacePalette>(_titleBar);
			palette?.OpenPalette();
			await WaitSeconds(0.24);
			Button target = palette?.FindChild("ResourceWorkspace_animation_editor", recursive: true, owned: false) as Button;
			XWUiMotion cardMotion = XWUiMotion.FindBinding(target);
			bool paletteCards = GodotObject.IsInstanceValid(palette) && palette.EntryCount >= 37 && palette.MotionBindingCount >= palette.EntryCount && GodotObject.IsInstanceValid(palette.SurfaceMotion) && palette.SurfaceMotion.AnimationStartCount > 0 && GodotObject.IsInstanceValid(cardMotion) && cardMotion.Role == XWUiMotion.MotionRole.Card;
			Require(paletteCards, "资源导航窗口没有把真实资源图块与面板过渡接入动效层。");
			palette?.Hide();
			await WaitFrames(2);
			motionMode?.SetPressedNoSignal(pressed: false);
			motionMode?.EmitSignal(BaseButton.SignalName.Toggled, false);
			await WaitFrames(2);
			int reducedBefore = twoDMotion?.AnimationStartCount ?? 0;
			twoD?.EmitSignal(Control.SignalName.MouseExited);
			twoD?.EmitSignal(Control.SignalName.MouseEntered);
			await TrackFrames(3);
			int num2;
			if (_titleBar.IsReducedMotion && (twoDMotion?.AnimationStartCount ?? 0) == reducedBefore && NearlyEqual(twoD.Scale, Vector2.One))
			{
				XWEditorInterface instance2 = XWEditorInterface.Instance;
				num2 = ((instance2 != null && instance2.GetEditorSettings()?.GetSetting("interface/reduced_motion", defaultValue: false) == true) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			bool reduced = (byte)num2 != 0;
			Require(reduced, "低动效开关未立即停用位移动画或没有持久化。");
			_titleBar.ConfigureMotion(reducedMotion: false, lowPerformanceMode: true);
			int lowPerformanceBefore = twoDMotion?.AnimationStartCount ?? 0;
			twoD?.EmitSignal(Control.SignalName.MouseExited);
			twoD?.EmitSignal(Control.SignalName.MouseEntered);
			await TrackFrames(3);
			bool lowPerformance = _titleBar.IsLowPerformanceMode && (motionMode?.Disabled ?? false) && (twoDMotion?.AnimationStartCount ?? 0) == lowPerformanceBefore;
			Require(lowPerformance, "低性能模式仍在创建界面 Tween。");
			_titleBar.ConfigureMotion(reducedMotion: false, lowPerformanceMode: false);
			motionMode?.SetPressedNoSignal(pressed: true);
			motionMode?.EmitSignal(BaseButton.SignalName.Toggled, true);
			await WaitFrames(2);
			bool responsive = await ExerciseNavigationAndMeasure(twoD, script, resource);
			Require(responsive, $"动态导航阻塞主线程，最大帧间隔 {_maxFrameGapMs:F2} ms。");
			bool noIdleProcess = !blueprintMotion.IsProcessing() && !twoDMotion.IsProcessing() && !scriptMotion.IsProcessing() && !resourceMotion.IsProcessing() && !centerMotion.IsProcessing() && !cardMotion.IsProcessing();
			Require(noIdleProcess, "动效绑定启用了常驻 _Process。");
			GodotObject inspectorBefore = XWEditorInterface.Instance?.GetInspector();
			int hiddenCancelBefore = twoDMotion.HiddenCancelCount;
			twoD?.EmitSignal(Control.SignalName.MouseExited);
			twoD?.EmitSignal(Control.SignalName.MouseEntered);
			manager.CloseEditor();
			await WaitFrames(4);
			bool hiddenStopped = twoDMotion.IsSuspended && !twoDMotion.IsAnimating && twoDMotion.HiddenCancelCount > hiddenCancelBefore && centerMotion.IsSuspended && !centerMotion.IsAnimating;
			GD.Print($"[MOD_EDITOR_WEB_MOTION_UI_HIDDEN_TRACE] buttonSuspended={twoDMotion.IsSuspended} buttonAnimating={twoDMotion.IsAnimating} hiddenCancelBefore={hiddenCancelBefore} hiddenCancelAfter={twoDMotion.HiddenCancelCount} centerSuspended={centerMotion.IsSuspended} centerAnimating={centerMotion.IsAnimating}");
			Require(hiddenStopped, "编辑器窗口隐藏后动效仍在运行。");
			manager.OpenEditor();
			await WaitFrames(5);
			bool flag = !twoDMotion.IsSuspended && !centerMotion.IsSuspended;
			Require(flag, "重新显示编辑器后动效绑定没有恢复。");
			GodotObject godotObject = XWEditorInterface.Instance?.GetInspector();
			bool flag2 = GodotObject.IsInstanceValid(inspectorBefore) && inspectorBefore == godotObject && !HasVisibleRawEmbeddedInspector(_editorPanel);
			Require(flag2, "主界面动态交互替换或打开了资源 Inspector。");
			GD.Print($"[MOD_EDITOR_WEB_MOTION_UI_PROBE] f3={f3} motionBound={motionBound} hover={hover} pressRebound={pressRebound} selected={selected} panelReveal={panelReveal} paletteCards={paletteCards} reduced={reduced} lowPerformance={lowPerformance} responsive={responsive} hiddenStopped={hiddenStopped} resumed={flag} noIdleProcess={noIdleProcess} inspectorUntouched={flag2} maxFrameGapMs={_maxFrameGapMs:F3} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> ExerciseNavigationAndMeasure(params Button[] buttons)
	{
		Stopwatch watch = Stopwatch.StartNew();
		double previousMs = watch.Elapsed.TotalMilliseconds;
		_maxFrameGapMs = 0.0;
		for (int i = 0; i < 30; i++)
		{
			Button obj = buttons[i % buttons.Length];
			obj?.EmitSignal(Control.SignalName.MouseEntered);
			obj?.EmitSignal(BaseButton.SignalName.ButtonDown);
			obj?.EmitSignal(BaseButton.SignalName.ButtonUp);
			obj?.EmitSignal(Control.SignalName.MouseExited);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			double totalMilliseconds = watch.Elapsed.TotalMilliseconds;
			_maxFrameGapMs = Math.Max(_maxFrameGapMs, totalMilliseconds - previousMs);
			previousMs = totalMilliseconds;
		}
		return _maxFrameGapMs < 120.0;
	}

	private async Task<bool> WaitForTitleBar(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			_editorPanel = XWEditorInterface.Instance?.GetEditorPanel();
			_titleBar = FindNodeOfType<XWEditorTitleBar>(_editorPanel);
			Node instance = _editorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(_titleBar) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(XWEditorInterface.Instance?.GetInspector()))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task TrackFrames(int count)
	{
		Stopwatch watch = Stopwatch.StartNew();
		double previousMs = watch.Elapsed.TotalMilliseconds;
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			double totalMilliseconds = watch.Elapsed.TotalMilliseconds;
			_maxFrameGapMs = Math.Max(_maxFrameGapMs, totalMilliseconds - previousMs);
			previousMs = totalMilliseconds;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private static void ToggleEditorWithF3()
	{
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
	}

	private static bool NearlyEqual(Vector2 left, Vector2 right)
	{
		return left.DistanceTo(right) < 0.01f;
	}

	private static bool HasVisibleRawEmbeddedInspector(Node root)
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
			if (HasVisibleRawEmbeddedInspector(child))
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

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_WEB_MOTION_UI_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_WEB_MOTION_UI_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
