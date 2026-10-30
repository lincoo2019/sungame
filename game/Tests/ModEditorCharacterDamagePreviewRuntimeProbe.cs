using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorCharacterDamagePreviewRuntimeProbe : Node
{
	private const string DamageDataPath = "res://Asset/Anime/Character/Zombie/Puzzle/Target/DamagePoint/ZombieTargetDamagePointData.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _total = Stopwatch.StartNew();

	private XWCharacterDataVisualResourceEditor _editor;

	public override async void _Ready()
	{
		bool f3 = false;
		bool registry = false;
		bool mounted = false;
		bool realCharacter = false;
		bool gameUi = false;
		bool playPause = false;
		bool incremental = false;
		bool manualScrub = false;
		bool hiddenStopped = false;
		bool resumed = false;
		bool responsive = false;
		bool inspectorHidden = false;
		double frameP95Ms = 1.0 / 0.0;
		try
		{
			ModEditorManager instance = EnsureModEditorManager();
			Require(GodotObject.IsInstanceValid(instance), "无法实例化 ModEditorManager。");
			if (!GodotObject.IsInstanceValid(instance))
			{
				throw new InvalidOperationException("ModEditorManager is unavailable.");
			}
			await WaitFrames(2);
			PressF3();
			f3 = await WaitForEditor(900);
			Require(f3, "F3 未在 900 帧内初始化角色数据编辑器。");
			if (!f3)
			{
				throw new InvalidOperationException("F3 editor initialization failed.");
			}
			bool flag = await EnterEditorSurface(900);
			Require(flag, "F3 ModEditor 主编辑界面未完成加载。");
			if (!flag)
			{
				throw new InvalidOperationException("F3 editor surface failed to load.");
			}
			CharacterDamagePointData characterDamagePointData = ResourceLoader.Load<CharacterDamagePointData>("res://Asset/Anime/Character/Zombie/Puzzle/Target/DamagePoint/ZombieTargetDamagePointData.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(characterDamagePointData), "真实靶子僵尸受伤点资源加载失败。");
			if (!GodotObject.IsInstanceValid(characterDamagePointData))
			{
				throw new InvalidOperationException("DamagePoint fixture is unavailable.");
			}
			registry = XWResourceEditorRegistry.TryGetEditor(characterDamagePointData, "res://Asset/Anime/Character/Zombie/Puzzle/Target/DamagePoint/ZombieTargetDamagePointData.tres", out var descriptor) && descriptor.Category == "CharacterData" && XWResourceEditorRegistry.TryOpen(characterDamagePointData, "res://Asset/Anime/Character/Zombie/Puzzle/Target/DamagePoint/ZombieTargetDamagePointData.tres", XWResourceEditContext.ForRoot(characterDamagePointData, "res://Asset/Anime/Character/Zombie/Puzzle/Target/DamagePoint/ZombieTargetDamagePointData.tres", descriptor.DockKey));
			Require(registry, "真实受伤点资源没有通过 CharacterData 注册入口打开。");
			if (!registry)
			{
				throw new InvalidOperationException("CharacterData registry route failed.");
			}
			XWEditorInterface.Instance.FocusPanel(descriptor.DockKey);
			mounted = await WaitForRuntimeCharacter(900);
			Require(mounted, "角色数据预览没有在 F3 窗口挂载真实角色场景。");
			if (!mounted)
			{
				throw new InvalidOperationException("Runtime character preview did not mount.");
			}
			Button playButton = Find<Button>("DamagePlayPauseButton");
			Button button = Find<Button>("DamageResetButton");
			HSlider healthSlider = Find<HSlider>("Slider");
			Label label = Find<Label>("PlaybackStateLabel");
			SubViewport viewport = Find<SubViewport>("CharacterViewport");
			Control control = Find<Control>("FallbackRibbon");
			Node2D previewRoot = Find<Node2D>("PreviewRoot");
			PanelContainer panelContainer = Find<PanelContainer>("InspectorPanel");
			VBoxContainer vBoxContainer = Find<VBoxContainer>("EmbeddedInspectorHost");
			Window instance2 = FindAncestorWindow(_editor);
			gameUi = GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(playButton) && playButton.Text.StartsWith("▶", StringComparison.Ordinal) && GodotObject.IsInstanceValid(button) && button.Text.StartsWith("↺", StringComparison.Ordinal) && GodotObject.IsInstanceValid(healthSlider) && healthSlider.Editable && GodotObject.IsInstanceValid(label) && label.Text.Contains("播放", StringComparison.Ordinal) && GodotObject.IsInstanceValid(viewport) && _editor.IsCharacterDataPreviewRendering;
			Require(gameUi, "角色受击预览缺少可操作的中文游戏化控件或活动视口。");
			TowerDefenseCharacter character = _editor.CurrentRuntimeCharacterPreview;
			realCharacter = GodotObject.IsInstanceValid(character) && character.editorPreviewMode && !character.inGame && (!GodotObject.IsInstanceValid(control) || !control.Visible);
			Require(realCharacter, "预览没有使用 editorPreviewMode 的真实 TowerDefenseCharacter 场景。");
			inspectorHidden = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			Require(inspectorHidden, "角色受伤预览错误地打开了嵌入检查器。");
			int buildsBefore = _editor.RuntimeCharacterPreviewBuildCount;
			int appliesBefore = _editor.RuntimeCharacterPreviewApplyCount;
			int ticksBefore = _editor.DamagePreviewProcessTickCount;
			double healthBefore = _editor.PreviewHealthRatio;
			playButton?.EmitSignal(BaseButton.SignalName.Pressed);
			bool started = _editor.IsDamagePreviewPlaying && playButton.Text.Contains("暂停", StringComparison.Ordinal);
			List<double> list = await MeasureFrameTimes(120);
			frameP95Ms = Percentile(list, 0.95);
			responsive = list.Count == 120 && frameP95Ms < 100.0;
			Require(responsive, $"受击演示期间主线程 P95 帧耗时过高：{frameP95Ms:0.###} ms。");
			bool flag2 = _editor.PreviewHealthRatio < healthBefore - 0.01 && _editor.DamagePreviewProcessTickCount > ticksBefore && _editor.RuntimeCharacterPreviewApplyCount > appliesBefore;
			incremental = started && flag2 && _editor.RuntimeCharacterPreviewBuildCount == buildsBefore && character == _editor.CurrentRuntimeCharacterPreview;
			Require(incremental, "播放受击时重建了角色，或没有增量驱动真实角色生命阶段。");
			playButton?.EmitSignal(BaseButton.SignalName.Pressed);
			int pausedTicks = _editor.DamagePreviewProcessTickCount;
			await WaitFrames(10);
			playPause = !_editor.IsDamagePreviewPlaying && _editor.DamagePreviewProcessTickCount == pausedTicks && playButton.Text.Contains("播放", StringComparison.Ordinal);
			Require(playPause, "暂停按钮没有停止受击演示处理。");
			healthSlider?.SetValueNoSignal(40.0);
			healthSlider?.EmitSignal(Godot.Range.SignalName.ValueChanged, 40.0);
			await WaitFrames(3);
			manualScrub = Math.Abs(_editor.PreviewHealthRatio - 0.4) < 0.001 && Math.Abs(character.previewDamagePointPersontage - 0.4) < 0.001 && _editor.RuntimeCharacterPreviewBuildCount == buildsBefore && character == _editor.CurrentRuntimeCharacterPreview;
			Require(manualScrub, "拖动生命值没有在原角色实例上增量刷新受伤阶段。");
			playButton?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			int ticksAtHide = _editor.DamagePreviewProcessTickCount;
			double healthAtHide = _editor.PreviewHealthRatio;
			_editor.Hide();
			await WaitFrames(16);
			hiddenStopped = _editor.ProcessMode == ProcessModeEnum.Disabled && _editor.DamagePreviewProcessTickCount == ticksAtHide && Math.Abs(_editor.PreviewHealthRatio - healthAtHide) < 1E-06 && viewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled && GodotObject.IsInstanceValid(previewRoot) && previewRoot.ProcessMode == ProcessModeEnum.Disabled;
			Require(hiddenStopped, "隐藏角色预览后仍在处理或渲染。");
			_editor.Show();
			await WaitFrames(12);
			resumed = _editor.IsDamagePreviewPlaying && _editor.DamagePreviewProcessTickCount > ticksAtHide && _editor.IsCharacterDataPreviewRendering && previewRoot.ProcessMode == ProcessModeEnum.Inherit;
			Require(resumed, "重新显示角色预览后没有恢复此前的演示状态。");
			playButton?.EmitSignal(BaseButton.SignalName.Pressed);
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_CHARACTER_DAMAGE_PREVIEW_PROBE] f3={f3} registry={registry} mounted={mounted} realCharacter={realCharacter} gameUi={gameUi} playPause={playPause} incremental={incremental} manualScrub={manualScrub} hiddenStopped={hiddenStopped} resumed={resumed} responsive={responsive} inspectorHidden={inspectorHidden} frameP95Ms={frameP95Ms:0.###} failures={_failures.Count} elapsedMs={_total.ElapsedMilliseconds}");
		Finish();
	}

	private ModEditorManager EnsureModEditorManager()
	{
		ModEditorManager instance = ModEditorManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			return instance;
		}
		instance = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(instance))
		{
			AddChild(instance, forceReadableName: false, InternalMode.Disabled);
		}
		return instance;
	}

	private static void PressF3()
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

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("character_data_editor") is XWCharacterDataVisualResourceEditor xWCharacterDataVisualResourceEditor && GodotObject.IsInstanceValid(xWCharacterDataVisualResourceEditor))
			{
				_editor = xWCharacterDataVisualResourceEditor;
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				await WaitFrames(3);
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForRuntimeCharacter(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(_editor) && _editor.IsVisibleInTree() && GodotObject.IsInstanceValid(_editor.CurrentRuntimeCharacterPreview) && Find<Button>("DamagePlayPauseButton") != null)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<List<double>> MeasureFrameTimes(int frameCount)
	{
		List<double> samples = new List<double>(frameCount);
		Stopwatch timer = Stopwatch.StartNew();
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			samples.Add(timer.Elapsed.TotalMilliseconds);
			timer.Restart();
		}
		return samples;
	}

	private static double Percentile(IReadOnlyCollection<double> values, double percentile)
	{
		if (values.Count == 0)
		{
			return 1.0 / 0.0;
		}
		double[] array = values.OrderBy((double value) => value).ToArray();
		int num = Math.Clamp((int)Math.Ceiling(percentile * (double)array.Length) - 1, 0, array.Length - 1);
		return array[num];
	}

	private T Find<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
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
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_CHARACTER_DAMAGE_PREVIEW_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_CHARACTER_DAMAGE_PREVIEW_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
