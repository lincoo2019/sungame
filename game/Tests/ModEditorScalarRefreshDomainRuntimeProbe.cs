using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorScalarRefreshDomainRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWGenericVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
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
			bool f3 = await WaitForEditor(900);
			Require(f3, "F3 did not initialize the generic resource workbench.");
			if (!f3)
			{
				Finish();
				return;
			}
			Node sentinel = new Node
			{
				Name = "ScalarRefreshInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = await WaitForInspector(120);
			inspector?.EditObject(sentinel);
			await WaitFrames(2);
			Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://ScalarRefreshDomainProbe"));
			string resourcePath = "user://ScalarRefreshDomainProbe/ScalarRefreshProbe.tres";
			XWDirectPropertyProbeResource resource = new XWDirectPropertyProbeResource();
			Require(ResourceSaver.Save(resource, resourcePath, ResourceSaver.SaverFlags.None) == Error.Ok, "Scalar refresh probe resource could not be saved initially.");
			XWDirectPropertyProbeResource resource2 = ResourceLoader.Load<XWDirectPropertyProbeResource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(resource2) && !string.IsNullOrWhiteSpace(resource2.ResourcePath), "Scalar refresh probe resource did not reload with a persistence path.");
			XWEditorInterface.Instance.EditResource(resource2);
			XWEditorInterface.Instance.FocusPanel("resource_editor");
			await WaitFrames(12);
			XWUniversalResourcePreview xWUniversalResourcePreview = _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false) as XWUniversalResourcePreview;
			XWInlineTextSurface xWInlineTextSurface = _editor.FindChild("InlineTextSurface", recursive: true, owned: false) as XWInlineTextSurface;
			XWDirectPropertySurface xWDirectPropertySurface = _editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			Button button = FindButtonByText(_editor, "保存");
			LineEdit instance = _editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit;
			SpinBox spinBox = xWDirectPropertySurface?.FindChild("Direct_Count", recursive: true, owned: false)?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
			Require(GodotObject.IsInstanceValid(xWUniversalResourcePreview), "Universal preview was not mounted.");
			Require(GodotObject.IsInstanceValid(xWInlineTextSurface), "Inline text surface was not mounted.");
			Require(GodotObject.IsInstanceValid(xWDirectPropertySurface), "Direct property surface was not mounted.");
			Require(GodotObject.IsInstanceValid(button), "Toolbar Save button was not mounted.");
			Require(GodotObject.IsInstanceValid(instance), "Inline Caption editor was not mounted.");
			Require(GodotObject.IsInstanceValid(spinBox), "Direct Count editor was not mounted.");
			if (!GodotObject.IsInstanceValid(xWUniversalResourcePreview) || !GodotObject.IsInstanceValid(xWInlineTextSurface) || !GodotObject.IsInstanceValid(xWDirectPropertySurface) || !GodotObject.IsInstanceValid(button) || !GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(spinBox))
			{
				Finish();
				return;
			}
			ulong canvasId = xWUniversalResourcePreview.GetInstanceId();
			ulong inlineId = xWInlineTextSurface.GetInstanceId();
			ulong toolbarButtonId = button.GetInstanceId();
			ulong directId = xWDirectPropertySurface.GetInstanceId();
			ulong countId = spinBox.GetInstanceId();
			_history.ClearHistory();
			ulong started = Time.GetTicksMsec();
			double maxFrameGapMs = await CommitNumberAndMeasureFrames(spinBox, 17.0, 8);
			await WaitFrames(4);
			long firstCountToken = _history.GetCurrentActionStateToken();
			xWUniversalResourcePreview = _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false) as XWUniversalResourcePreview;
			xWInlineTextSurface = _editor.FindChild("InlineTextSurface", recursive: true, owned: false) as XWInlineTextSurface;
			xWDirectPropertySurface = _editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			button = FindButtonByText(_editor, "保存");
			spinBox = xWDirectPropertySurface?.FindChild("Direct_Count", recursive: true, owned: false)?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
			bool canvasIdentity = GodotObject.IsInstanceValid(xWUniversalResourcePreview) && xWUniversalResourcePreview.GetInstanceId() == canvasId;
			bool inlineIdentity = GodotObject.IsInstanceValid(xWInlineTextSurface) && xWInlineTextSurface.GetInstanceId() == inlineId;
			bool toolbarIdentity = GodotObject.IsInstanceValid(button) && button.GetInstanceId() == toolbarButtonId;
			bool scalarCommitNoRebuild = resource2.Count == 17 && GodotObject.IsInstanceValid(xWDirectPropertySurface) && xWDirectPropertySurface.GetInstanceId() == directId && GodotObject.IsInstanceValid(spinBox) && spinBox.GetInstanceId() == countId && canvasIdentity && inlineIdentity && toolbarIdentity;
			bool previewUpdated = PreviewContainsValue(xWUniversalResourcePreview, "Count", "17");
			bool responsive = Time.GetTicksMsec() - started < 1500 && maxFrameGapMs < 250.0;
			Require(scalarCommitNoRebuild, "Scalar direct-property commit rebuilt a mounted workbench surface.");
			Require(previewUpdated, "Incremental scalar commit did not refresh universal preview data.");
			Require(responsive, $"Scalar refresh stalled: elapsed={Time.GetTicksMsec() - started}ms maxFrameGap={maxFrameGapMs:0.###}ms.");
			spinBox = (_editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface)?.FindChild("Direct_Count", recursive: true, owned: false)?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
			await CommitNumberAndMeasureFrames(spinBox, 19.0, 5);
			await WaitFrames(3);
			long secondCountToken = _history.GetCurrentActionStateToken();
			Button button2 = FindButtonByText(_editor, "撤销");
			Button redoButton = FindButtonByText(_editor, "重做");
			button2?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			int num;
			if (resource2.Count == 17)
			{
				SpinBox obj = (_editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface)?.FindChild("Direct_Count", recursive: true, owned: false)?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
				num = ((obj != null && obj.Value == 17.0) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool repeatedUndone = (byte)num != 0;
			redoButton?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			int num2;
			if (resource2.Count == 19)
			{
				SpinBox obj2 = (_editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface)?.FindChild("Direct_Count", recursive: true, owned: false)?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
				num2 = ((obj2 != null && obj2.Value == 19.0) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			bool flag = (byte)num2 != 0;
			int num3;
			if (firstCountToken > 0 && secondCountToken > 0 && firstCountToken != secondCountToken && repeatedUndone && flag)
			{
				Node node = _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false);
				if (node != null && node.GetInstanceId() == canvasId)
				{
					Node node2 = _editor.FindChild("InlineTextSurface", recursive: true, owned: false);
					if (node2 != null && node2.GetInstanceId() == inlineId)
					{
						Button button3 = FindButtonByText(_editor, "保存");
						num3 = ((button3 != null && button3.GetInstanceId() == toolbarButtonId) ? 1 : 0);
						goto IL_0caf;
					}
				}
			}
			num3 = 0;
			goto IL_0caf;
			IL_0caf:
			bool repeatedActionTokenIsolation = (byte)num3 != 0;
			Require(repeatedActionTokenIsolation, "Repeated Set Property: Count actions collided or rebuilt the mounted workbench during Undo/Redo.");
			instance = _editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit;
			instance.EmitSignal(Control.SignalName.FocusEntered);
			instance.Text = "轻量刷新";
			instance.EmitSignal(LineEdit.SignalName.TextChanged, instance.Text);
			instance.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(8);
			bool condition = resource2.Caption == "轻量刷新" && GodotObject.IsInstanceValid(_editor.FindChild("UniversalResourcePreview", recursive: true, owned: false)) && _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false).GetInstanceId() == canvasId && GodotObject.IsInstanceValid(_editor.FindChild("InlineTextSurface", recursive: true, owned: false)) && _editor.FindChild("InlineTextSurface", recursive: true, owned: false).GetInstanceId() == inlineId && GodotObject.IsInstanceValid(FindButtonByText(_editor, "保存")) && FindButtonByText(_editor, "保存").GetInstanceId() == toolbarButtonId;
			Require(condition, "Inline text scalar commit rebuilt the canvas, inline surface, or toolbar.");
			button2 = FindButtonByText(_editor, "撤销");
			redoButton = FindButtonByText(_editor, "重做");
			Require(GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(redoButton), "Toolbar Undo/Redo actions were unavailable.");
			button2?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(5);
			string captionAfterUndo = resource2.Caption;
			string controlAfterUndo = (_editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit)?.Text ?? "<null>";
			bool undone = captionAfterUndo == "Original caption" && controlAfterUndo == "Original caption";
			redoButton?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(5);
			bool flag2 = resource2.Caption == "轻量刷新" && (_editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit)?.Text == "轻量刷新";
			int num4;
			if (undone && flag2)
			{
				Node node3 = _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false);
				if (node3 != null && node3.GetInstanceId() == canvasId)
				{
					Node node4 = _editor.FindChild("InlineTextSurface", recursive: true, owned: false);
					if (node4 != null && node4.GetInstanceId() == inlineId)
					{
						Button button4 = FindButtonByText(_editor, "保存");
						num4 = ((button4 != null && button4.GetInstanceId() == toolbarButtonId) ? 1 : 0);
						goto IL_10dd;
					}
				}
			}
			num4 = 0;
			goto IL_10dd;
			IL_10dd:
			bool undoRedo = (byte)num4 != 0;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(131, 9);
			defaultInterpolatedStringHandler.AppendLiteral("[MOD_EDITOR_SCALAR_REFRESH_UNDO_DIAGNOSTIC] undone=");
			defaultInterpolatedStringHandler.AppendFormatted(undone);
			defaultInterpolatedStringHandler.AppendLiteral(" redone=");
			defaultInterpolatedStringHandler.AppendFormatted(flag2);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendLiteral("afterUndo=");
			defaultInterpolatedStringHandler.AppendFormatted(captionAfterUndo);
			defaultInterpolatedStringHandler.AppendLiteral(" controlAfterUndo=");
			defaultInterpolatedStringHandler.AppendFormatted(controlAfterUndo);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendLiteral("caption=");
			defaultInterpolatedStringHandler.AppendFormatted(resource2.Caption);
			defaultInterpolatedStringHandler.AppendLiteral(" control=");
			defaultInterpolatedStringHandler.AppendFormatted((_editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit)?.Text);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendLiteral("canvas=");
			Node node5 = _editor.FindChild("UniversalResourcePreview", recursive: true, owned: false);
			defaultInterpolatedStringHandler.AppendFormatted(node5 != null && node5.GetInstanceId() == canvasId);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendLiteral("inline=");
			Node node6 = _editor.FindChild("InlineTextSurface", recursive: true, owned: false);
			defaultInterpolatedStringHandler.AppendFormatted(node6 != null && node6.GetInstanceId() == inlineId);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendLiteral("toolbar=");
			Button button5 = FindButtonByText(_editor, "保存");
			defaultInterpolatedStringHandler.AppendFormatted(button5 != null && button5.GetInstanceId() == toolbarButtonId);
			GD.Print(defaultInterpolatedStringHandler.ToStringAndClear());
			Require(undoRedo, "Scalar Undo/Redo did not synchronize mounted controls in place.");
			FindButtonByText(_editor, "保存")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(10);
			XWDirectPropertyProbeResource xWDirectPropertyProbeResource = ResourceLoader.Load<XWDirectPropertyProbeResource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			bool flag3 = GodotObject.IsInstanceValid(xWDirectPropertyProbeResource) && xWDirectPropertyProbeResource.Count == 19 && xWDirectPropertyProbeResource.Caption == "轻量刷新";
			GD.Print($"[MOD_EDITOR_SCALAR_REFRESH_SAVE_DIAGNOSTIC] liveCount={resource2.Count} liveCaption={resource2.Caption} path={resource2.ResourcePath} reloaded={GodotObject.IsInstanceValid(xWDirectPropertyProbeResource)} count={xWDirectPropertyProbeResource?.Count ?? (-1)} caption={xWDirectPropertyProbeResource?.Caption ?? "<null>"}");
			Require(flag3, "Incrementally refreshed scalar values did not survive Save/Reload.");
			bool flag4 = inspector == null || inspector.CurrentObject == sentinel;
			Require(flag4, "Scalar refresh changed the global Inspector selection.");
			GD.Print($"[MOD_EDITOR_SCALAR_REFRESH_DOMAIN_PROBE] f3={f3} scalarCommitNoRebuild={scalarCommitNoRebuild} canvasIdentity={canvasIdentity} inlineIdentity={inlineIdentity} toolbarIdentity={toolbarIdentity} previewUpdated={previewUpdated} repeatedActionTokenIsolation={repeatedActionTokenIsolation} undoRedo={undoRedo} saveReload={flag3} responsive={responsive} inspectorUntouched={flag4} maxFrameGapMs={maxFrameGapMs:0.###} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<double> CommitNumberAndMeasureFrames(SpinBox control, double value, int frameCount)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.Value = value;
		control.EmitSignal(Godot.Range.SignalName.ValueChanged, value);
		control.EmitSignal(Control.SignalName.FocusExited);
		ulong previous = Time.GetTicksUsec();
		double maximum = 0.0;
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ulong ticksUsec = Time.GetTicksUsec();
			maximum = Math.Max(maximum, (double)(ticksUsec - previous) / 1000.0);
			previous = ticksUsec;
		}
		return maximum;
	}

	private static bool PreviewContainsValue(XWUniversalResourcePreview preview, string propertyName, string expected)
	{
		if (!GodotObject.IsInstanceValid(preview))
		{
			return false;
		}
		foreach (Node item in preview.FindChildren("*", "Tree", recursive: true, owned: false))
		{
			if (!(item is Tree tree) || tree.GetRoot() == null)
			{
				continue;
			}
			for (TreeItem treeItem = tree.GetRoot().GetFirstChild(); treeItem != null; treeItem = treeItem.GetNext())
			{
				if (treeItem.GetText(0).ToString() == propertyName && treeItem.GetText(1).ToString() == expected)
				{
					return true;
				}
			}
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetResourceEditor("resource_editor") is XWGenericVisualResourceEditor editor)
			{
				_editor = editor;
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<XWInspector> WaitForInspector(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetInspector() is XWInspector result)
			{
				return result;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static Button FindButtonByText(Node root, string text)
	{
		foreach (Node item in root.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text == text)
			{
				return button;
			}
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
			GD.PrintErr("[MOD_EDITOR_SCALAR_REFRESH_DOMAIN_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCALAR_REFRESH_DOMAIN_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
