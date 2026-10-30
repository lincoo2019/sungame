using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.OutPutPanel;

public partial class ModEditorOutputPanelStreamingRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		_ = 6;
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
			XWOutputPanel panel = await WaitForOutputPanel(900);
			bool f3 = GodotObject.IsInstanceValid(panel) && panel.IsInsideTree();
			Require(f3, "F3 did not initialize the real output panel within 900 frames.");
			if (!f3)
			{
				Finish();
				return;
			}
			XWEditorInterface.Instance?.GetLayoutManager()?.FocusPanel("output");
			await WaitFrames(3);
			bool window = FindAncestorWindow(panel) != null;
			bool visible = panel.IsVisibleInTree();
			Require(window && visible, "Output panel is not visible inside the real F3 ModEditor window.");
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node sentinel = new Node
			{
				Name = "OutputStreamingInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(sentinel);
			panel.Clear();
			int refreshBefore = panel.FullRefreshCount;
			int incrementalAppendCount = panel.IncrementalAppendCount;
			Stopwatch burstTimer = Stopwatch.StartNew();
			for (int i = 0; i < 500; i++)
			{
				panel.AddMessage($"stream-{i}");
			}
			burstTimer.Stop();
			bool incremental = panel.FullRefreshCount == refreshBefore && panel.IncrementalAppendCount - incrementalAppendCount == 500 && panel.StoredLineCount == 500 && panel.RenderedLineCount == 500;
			Require(incremental, "Visible output burst did not use incremental RichTextLabel appends.");
			string text = string.Join('\n', Enumerable.Repeat("same-line", 10050));
			panel.AddMessage(text);
			await WaitFrames(3);
			Button button = panel.FindChild("StdFilter", recursive: true, owned: false) as Button;
			bool ringTrim = panel.StoredLineCount == 10000 && panel.GetStoredMessageCount(XWOutputPanel.MessageType.Std) == 10000 && panel.RenderedLineCount == 10000 && GodotObject.IsInstanceValid(button) && button.Text == "10000";
			bool boundedRefresh = panel.FullRefreshCount - refreshBefore <= 2;
			Require(ringTrim, "Output ring trim did not preserve exactly 10,000 lines and matching filter counts.");
			Require(boundedRefresh, "Large output burst caused too many full RichTextLabel rebuilds.");
			panel.Hide();
			await WaitFrames(2);
			int hiddenRefreshBefore = panel.FullRefreshCount;
			for (int j = 0; j < 100; j++)
			{
				panel.AddMessage($"hidden-{j}", XWOutputPanel.MessageType.Editor);
			}
			await WaitFrames(2);
			bool hiddenDeferred = !panel.IsVisibleInTree() && panel.FullRefreshCount == hiddenRefreshBefore;
			panel.Show();
			await WaitFrames(3);
			bool flag = panel.IsVisibleInTree() && panel.FullRefreshCount == hiddenRefreshBefore + 1 && panel.StoredLineCount == 10000;
			bool flag2 = hiddenDeferred && flag;
			Require(flag2, "Hidden output messages were not coalesced into one visibility-triggered refresh.");
			bool flag3 = burstTimer.ElapsedMilliseconds < 4000;
			bool flag4 = inspector == null || inspector.CurrentObject == sentinel;
			Require(flag3, $"500-line incremental burst took too long: {burstTimer.ElapsedMilliseconds} ms.");
			Require(flag4, "Output streaming replaced the raw Inspector object.");
			GD.Print($"[MOD_EDITOR_OUTPUT_STREAMING_PROBE] f3={f3} window={window} visible={visible} incremental={incremental} ringTrim={ringTrim} boundedRefresh={boundedRefresh} hiddenBatch={flag2} fastBurst={flag3} inspectorUntouched={flag4} stored={panel.StoredLineCount} refreshes={panel.FullRefreshCount - refreshBefore} burstMs={burstTimer.ElapsedMilliseconds} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<XWOutputPanel> WaitForOutputPanel(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWOutputPanel xWOutputPanel = XWEditorInterface.Instance?.GetOutputPanel();
			if (GodotObject.IsInstanceValid(xWOutputPanel) && xWOutputPanel.IsInsideTree())
			{
				return xWOutputPanel;
			}
			await WaitFrames(1);
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
			GD.PrintErr("[MOD_EDITOR_OUTPUT_STREAMING_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_OUTPUT_STREAMING_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
