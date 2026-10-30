using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorIdleProcessAuditProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly List<string> _activeProcesses = new List<string>();

	private readonly List<string> _hiddenProcesses = new List<string>();

	private int _physicsProcesses;

	public override async void _Ready()
	{
		_ = 16;
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
			Control editorPanel = await WaitForEditor(900);
			Require(GodotObject.IsInstanceValid(editorPanel), "F3 did not initialize the real ModEditor panel.");
			if (!GodotObject.IsInstanceValid(editorPanel))
			{
				Finish();
				return;
			}
			await WaitFrames(30);
			Window window = FindAncestorWindow(editorPanel);
			Require(GodotObject.IsInstanceValid(window), "ModEditor panel is not mounted in the F3 window.");
			if (!GodotObject.IsInstanceValid(window))
			{
				Finish();
				return;
			}
			XWInspector inspector = FindRootInspector(window);
			Require(GodotObject.IsInstanceValid(inspector), "The real root Inspector was not mounted in the F3 window.");
			int visibleInspectorTicks = 0;
			int hiddenInspectorTicks = 2147483647;
			int hiddenInspectorPolls = 2147483647;
			int resumedInspectorTicks = 0;
			int hiddenWindowInspectorTicks = 2147483647;
			int reopenedWindowInspectorTicks = 0;
			int invalidObjectHiddenTicks = 2147483647;
			int num = 2147483647;
			bool externalPropertySynchronized = false;
			bool visibleRowsFiltered = false;
			bool invalidObjectCleared = false;
			if (GodotObject.IsInstanceValid(inspector))
			{
				Node2D fixture = new Node2D
				{
					Name = "InspectorLifecycleFixture",
					Position = new Vector2(24f, 48f)
				};
				AddChild(fixture, forceReadableName: false, InternalMode.Disabled);
				XWEditorInterface.Instance.FocusPanel("inspector");
				inspector.Show();
				inspector.SetObject(fixture, forceRefresh: true);
				int visibleTickStart = inspector.UpdateTimerCallbackCount;
				int visiblePollStart = inspector.PropertyPollPassCount;
				await WaitFrames(30);
				visibleInspectorTicks = inspector.UpdateTimerCallbackCount - visibleTickStart;
				bool condition = inspector.IsUpdateTimerRunning && visibleInspectorTicks > 0 && inspector.PropertyPollPassCount > visiblePollStart;
				Require(condition, "Visible Inspector did not run its lifecycle-gated value refresh.");
				inspector.Search("position");
				await WaitFrames(3);
				bool trackedPosition = inspector.IsTrackedPropertySynchronized("position");
				fixture.Position = new Vector2(80f, 96f);
				bool observedExternalChange = !inspector.IsTrackedPropertySynchronized("position");
				int externalPollStart = inspector.PropertyPollPassCount;
				int externalReadStart = inspector.PropertyPollReadCount;
				await WaitFrames(30);
				int num2 = inspector.PropertyPollPassCount - externalPollStart;
				int num3 = inspector.PropertyPollReadCount - externalReadStart;
				externalPropertySynchronized = trackedPosition && observedExternalChange && inspector.IsTrackedPropertySynchronized("position");
				visibleRowsFiltered = num3 > 0 && num2 > 0 && num3 < inspector.PropertyEditorCount * num2;
				Require(externalPropertySynchronized, "Visible Inspector did not refresh a real externally changed Node2D position property.");
				Require(visibleRowsFiltered, $"Inspector did not limit polling to visible rows: reads={num3} editors={inspector.PropertyEditorCount} passes={num2}.");
				inspector.Hide();
				await WaitFrames(3);
				int hiddenTickStart = inspector.UpdateTimerCallbackCount;
				int hiddenPollStart = inspector.PropertyPollPassCount;
				await WaitFrames(30);
				hiddenInspectorTicks = inspector.UpdateTimerCallbackCount - hiddenTickStart;
				hiddenInspectorPolls = inspector.PropertyPollPassCount - hiddenPollStart;
				Require(!inspector.IsUpdateTimerRunning && hiddenInspectorTicks == 0 && hiddenInspectorPolls == 0, $"Hidden Inspector kept polling: timer={inspector.IsUpdateTimerRunning} ticks={hiddenInspectorTicks} polls={hiddenInspectorPolls}.");
				inspector.Show();
				XWEditorInterface.Instance.FocusPanel("inspector");
				int resumedTickStart = inspector.UpdateTimerCallbackCount;
				await WaitFrames(30);
				resumedInspectorTicks = inspector.UpdateTimerCallbackCount - resumedTickStart;
				Require(inspector.IsUpdateTimerRunning && resumedInspectorTicks > 0, "Inspector polling did not resume after the panel became visible again.");
				window.Hide();
				await WaitFrames(3);
				int hiddenWindowTickStart = inspector.UpdateTimerCallbackCount;
				await WaitFrames(30);
				hiddenWindowInspectorTicks = inspector.UpdateTimerCallbackCount - hiddenWindowTickStart;
				Require(!inspector.IsUpdateTimerRunning && hiddenWindowInspectorTicks == 0, $"Inspector kept polling after the F3 window was hidden: ticks={hiddenWindowInspectorTicks}.");
				window.Show();
				await WaitFrames(3);
				XWEditorInterface.Instance.FocusPanel("inspector");
				int reopenedWindowTickStart = inspector.UpdateTimerCallbackCount;
				await WaitFrames(30);
				reopenedWindowInspectorTicks = inspector.UpdateTimerCallbackCount - reopenedWindowTickStart;
				Require(inspector.IsUpdateTimerRunning && reopenedWindowInspectorTicks > 0, "Inspector polling did not resume after the F3 window reopened.");
				window.Hide();
				await WaitFrames(3);
				int invalidObjectHiddenTickStart = inspector.UpdateTimerCallbackCount;
				fixture.QueueFree();
				await WaitFrames(30);
				invalidObjectHiddenTicks = inspector.UpdateTimerCallbackCount - invalidObjectHiddenTickStart;
				Require(invalidObjectHiddenTicks == 0, $"Hidden Inspector received callbacks while its edited object was freed: ticks={invalidObjectHiddenTicks}.");
				window.Show();
				await WaitFrames(3);
				invalidObjectCleared = inspector.CurrentObject == null && !inspector.IsUpdateTimerRunning && inspector.PropertyContainer.GetChildCount() == 0;
				Require(invalidObjectCleared, "Reopening the F3 window did not clear an edited object that was freed while hidden.");
				int noObjectTickStart = inspector.UpdateTimerCallbackCount;
				await WaitFrames(30);
				num = inspector.UpdateTimerCallbackCount - noObjectTickStart;
				Require(!inspector.IsUpdateTimerRunning && num == 0, $"Inspector without an edited object kept polling: ticks={num}.");
			}
			CollectProcessingNodes(window, window);
			foreach (string activeProcess in _activeProcesses)
			{
				GD.Print("[MOD_EDITOR_IDLE_PROCESS_NODE] " + activeProcess);
			}
			Require(_hiddenProcesses.Count == 0, "Hidden ModEditor nodes still process every frame: " + string.Join(" | ", _hiddenProcesses));
			GD.Print($"[MOD_EDITOR_IDLE_PROCESS_AUDIT] f3=True active={_activeProcesses.Count} hidden={_hiddenProcesses.Count} physics={_physicsProcesses} inspectorVisibleTicks={visibleInspectorTicks} inspectorHiddenTicks={hiddenInspectorTicks} inspectorHiddenPolls={hiddenInspectorPolls} inspectorResumedTicks={resumedInspectorTicks} inspectorHiddenWindowTicks={hiddenWindowInspectorTicks} inspectorReopenedWindowTicks={reopenedWindowInspectorTicks} externalPropertySynchronized={externalPropertySynchronized} visibleRowsFiltered={visibleRowsFiltered} invalidObjectHiddenTicks={invalidObjectHiddenTicks} invalidObjectCleared={invalidObjectCleared} inspectorNoObjectTicks={num} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private void CollectProcessingNodes(Node node, Node root)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		bool flag = node.IsProcessing();
		bool flag2 = node.IsPhysicsProcessing();
		if (flag2)
		{
			_physicsProcesses++;
		}
		if (flag || flag2)
		{
			bool flag3 = !(node is CanvasItem canvasItem) || canvasItem.IsVisibleInTree();
			string item = $"{root.GetPathTo(node)}:{node.GetType().Name}:visible={flag3}:process={flag}:physics={flag2}";
			_activeProcesses.Add(item);
			if (!flag3)
			{
				_hiddenProcesses.Add(item);
			}
		}
		foreach (Node child in node.GetChildren())
		{
			CollectProcessingNodes(child, root);
		}
	}

	private async Task<Control> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control control = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = control?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(control) && !GodotObject.IsInstanceValid(instance))
			{
				return control;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static XWInspector FindRootInspector(Node root)
	{
		if (root is XWInspector { IsSubInspector: false } xWInspector)
		{
			return xWInspector;
		}
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node child in root.GetChildren())
		{
			XWInspector xWInspector2 = FindRootInspector(child);
			if (GodotObject.IsInstanceValid(xWInspector2))
			{
				return xWInspector2;
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
			GD.PrintErr("[MOD_EDITOR_IDLE_PROCESS_AUDIT_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_IDLE_PROCESS_AUDIT_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
