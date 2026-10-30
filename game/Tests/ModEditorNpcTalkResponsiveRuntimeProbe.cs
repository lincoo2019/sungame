using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorNpcTalkResponsiveRuntimeProbe : Node
{
	private const string DraftPath = "user://mod_editor_npc_talk_responsive_probe.tres";

	private const string EditorDockKey = "npc_talk_editor";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWTextFlowVisualResourceEditor _editor;

	public override async void _Ready()
	{
		_ = 14;
		try
		{
			NpcTalkConfig draft = CreateDraft();
			Error error = ResourceSaver.Save(draft, "user://mod_editor_npc_talk_responsive_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not create isolated NPC dialogue draft: {error}.");
			if (error != Error.Ok)
			{
				Finish();
				return;
			}
			draft = ResourceLoader.Load<NpcTalkConfig>("user://mod_editor_npc_talk_responsive_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(draft) && draft.talkList.Count == 2, "NPC dialogue draft did not reload with two entries.");
			if (!GodotObject.IsInstanceValid(draft))
			{
				Finish();
				return;
			}
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
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not initialize the text-flow editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 editor did not finish loading its main editing surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			XWEditorInterface.Instance.EditResource(draft, XWResourceEditContext.ForRoot(draft, "user://mod_editor_npc_talk_responsive_probe.tres", "npc_talk_editor"));
			XWEditorInterface.Instance.FocusPanel("npc_talk_editor");
			XWNpcTalkInlineEditor inline = await WaitForInlineEditor(900);
			Require(GodotObject.IsInstanceValid(inline), "NPC inline director did not mount.");
			if (!GodotObject.IsInstanceValid(inline))
			{
				Finish();
				return;
			}
			Button button = inline.FindChild("EntryVisualButton", recursive: true, owned: false) as Button;
			Button button2 = inline.FindChild("AddTypeVisualButton", recursive: true, owned: false) as Button;
			HFlowContainer hFlowContainer = inline.FindChild("EntryPickerGrid", recursive: true, owned: false) as HFlowContainer;
			HFlowContainer hFlowContainer2 = inline.FindChild("AddTypeGrid", recursive: true, owned: false) as HFlowContainer;
			bool visualCards = GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(button.Icon) && GodotObject.IsInstanceValid(button2.Icon) && GodotObject.IsInstanceValid(hFlowContainer) && GodotObject.IsInstanceValid(hFlowContainer2) && inline.EntryVisualCardCount == 2 && inline.EntryTypeCardCount == 4 && hFlowContainer.GetChildCount() == 2 && hFlowContainer2.GetChildCount() == 4 && hFlowContainer.GetChild(0) is XWNpcTalkVisualChoiceCard && hFlowContainer2.GetChild(0) is XWNpcTalkVisualChoiceCard;
			Require(visualCards, "Dialogue and add-type selectors are not scene-authored icon cards.");
			XWAspectScaledPreviewHost responsiveStage = inline.ResponsiveStage;
			responsiveStage.Size = new Vector2(680f, 378f);
			responsiveStage.ApplyResponsiveLayout();
			SubViewportContainer node = responsiveStage.GetNode<SubViewportContainer>("ViewportContainer");
			bool narrow = responsiveStage.PreviewScale < 1f && responsiveStage.PreviewFitsWithinBounds() && node.Size.IsEqualApprox(new Vector2(1080f, 600f)) && node.Scale.IsEqualApprox(Vector2.One * responsiveStage.PreviewScale) && inline.CustomMinimumSize.X <= 540f;
			Require(narrow, "Narrow inline layout did not scale the fixed 1080x600 game canvas inside its bounds.");
			string originalText = draft.talkList[0].text;
			bool edited = inline.SetCurrentText("F3 响应式气泡编辑");
			await WaitFrames(2);
			XWUndoRedoManager undoRedo = XWEditorInterface.Instance.GetUndoRedoManager();
			bool undoText = undoRedo.Undo();
			await WaitFrames(2);
			bool textRestored = draft.talkList[0].text == originalText;
			bool redoText = undoRedo.Redo();
			await WaitFrames(2);
			bool textRedone = draft.talkList[0].text == "F3 响应式气泡编辑";
			inline.SelectAddTypeVisual(2);
			bool added = inline.AddTalkEntry();
			await WaitFrames(2);
			bool tutorialAdded = draft.talkList.Count == 3 && draft.talkList[2] is NpcTalkTutorialConfig && inline.EntryVisualCardCount == 3;
			bool duplicated = inline.DuplicateTalkEntry();
			await WaitFrames(2);
			bool duplicatePassed = duplicated && draft.talkList.Count == 4 && inline.EntryVisualCardCount == 4;
			bool removed = inline.RemoveTalkEntry();
			await WaitFrames(2);
			bool removePassed = removed && draft.talkList.Count == 3 && inline.EntryVisualCardCount == 3;
			bool undoRemove = undoRedo.Undo();
			await WaitFrames(2);
			bool undoRemovePassed = undoRemove && draft.talkList.Count == 4 && inline.EntryVisualCardCount == 4;
			bool redoRemove = undoRedo.Redo();
			await WaitFrames(2);
			bool flag3 = redoRemove && draft.talkList.Count == 3 && inline.EntryVisualCardCount == 3;
			bool undoRedoPassed = edited && undoText && textRestored && redoText && textRedone && added && tutorialAdded && duplicatePassed && removePassed && undoRemovePassed && flag3;
			Require(undoRedoPassed, "Sentence text/add/duplicate/remove operations did not survive global Undo/Redo.");
			XWNpcTalkPreviewWindow previewWindow = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkPreviewWindow.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWNpcTalkPreviewWindow>(PackedScene.GenEditState.Disabled);
			Require(GodotObject.IsInstanceValid(previewWindow), "Responsive NPC runtime preview window could not be instantiated.");
			bool preview = false;
			if (GodotObject.IsInstanceValid(previewWindow))
			{
				_editor.AddChild(previewWindow, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				previewWindow.Preview(draft);
				await WaitFrames(2);
				XWAspectScaledPreviewHost responsivePreviewHost = previewWindow.ResponsivePreviewHost;
				responsivePreviewHost.Size = new Vector2(720f, 400f);
				responsivePreviewHost.ApplyResponsiveLayout();
				preview = previewWindow.MinSize.X <= 640 && previewWindow.MinSize.Y <= 420 && responsivePreviewHost.PreviewScale < 1f && responsivePreviewHost.PreviewFitsWithinBounds() && GodotObject.IsInstanceValid(previewWindow.FindChild("PreviewNpc", recursive: true, owned: false));
				previewWindow.Hide();
			}
			Require(preview, "Responsive runtime preview did not render a real NPC inside the scaled game canvas.");
			VBoxContainer vBoxContainer = _editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
			bool inspectorUntouched = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0;
			Require(inspectorUntouched, "NPC director restored the raw embedded Inspector.");
			Button button3 = FindButtonByText(_editor, "保存");
			Require(GodotObject.IsInstanceValid(button3), "NPC resource toolbar save button is unavailable.");
			if (GodotObject.IsInstanceValid(button3))
			{
				button3.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(6);
			}
			NpcTalkConfig npcTalkConfig = ResourceLoader.Load<NpcTalkConfig>("user://mod_editor_npc_talk_responsive_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			bool flag4 = GodotObject.IsInstanceValid(npcTalkConfig) && npcTalkConfig.talkList.Count == 3 && npcTalkConfig.talkList[0].text == "F3 响应式气泡编辑" && npcTalkConfig.talkList[2] is NpcTalkTutorialConfig;
			Require(flag4, "Saved NPC dialogue mutations did not survive an uncached reload.");
			bool value = FindAncestorWindow(_editor) != null;
			GD.Print($"[MOD_EDITOR_NPC_TALK_RESPONSIVE_PROBE] window={value} visualCards={visualCards} narrow={narrow} preview={preview} mutate={tutorialAdded && duplicatePassed && removePassed} undoRedo={undoRedoPassed} saveReload={flag4} inspectorUntouched={inspectorUntouched} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static NpcTalkConfig CreateDraft()
	{
		NpcTalkBaseConfig item = new NpcTalkBaseConfig
		{
			ResourceLocalToScene = true,
			ResourceName = "Probe_Normal",
			npc = "CrazyDave",
			text = "第一句原始气泡",
			anime = "",
			audio = ""
		};
		NpcTalkHandConfig item2 = new NpcTalkHandConfig
		{
			ResourceLocalToScene = true,
			ResourceName = "Probe_Hand",
			npc = "WeiWeiMi",
			text = "第二句道具表演",
			anime = "",
			audio = ""
		};
		return new NpcTalkConfig
		{
			ResourceName = "ModEditorNpcTalkResponsiveProbe",
			saveKey = "ProbeNpcTalk",
			talkList = 
			{
				item,
				(NpcTalkBaseConfig)item2
			}
		};
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("npc_talk_editor") is XWTextFlowVisualResourceEditor xWTextFlowVisualResourceEditor && GodotObject.IsInstanceValid(xWTextFlowVisualResourceEditor))
			{
				_editor = xWTextFlowVisualResourceEditor;
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("npc_talk_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<XWNpcTalkInlineEditor> WaitForInlineEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWNpcTalkInlineEditor xWNpcTalkInlineEditor = _editor?.FindChild("NpcTalkInlineEditor", recursive: true, owned: false) as XWNpcTalkInlineEditor;
			if (GodotObject.IsInstanceValid(xWNpcTalkInlineEditor) && xWNpcTalkInlineEditor.IsVisibleInTree() && xWNpcTalkInlineEditor.EntryTypeCardCount == 4)
			{
				return xWNpcTalkInlineEditor;
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
			GD.PrintErr("[MOD_EDITOR_NPC_TALK_RESPONSIVE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_NPC_TALK_RESPONSIVE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
