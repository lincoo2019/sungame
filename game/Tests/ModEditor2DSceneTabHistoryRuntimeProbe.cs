using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DSceneTabHistoryRuntimeProbe : Node
{
	private const string SceneAPath = "user://mod_editor_2d_history_a.tscn";

	private const string SceneBPath = "user://mod_editor_2d_history_b.tscn";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		_ = 19;
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
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			bool window = FindAncestorWindow(editor) != null;
			Require(window, "2D scene editor is not mounted under the F3 ModEditor window.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			Require(CreateFixture("user://mod_editor_2d_history_a.tscn", "HistorySceneA", "SceneANode", new Vector2(12f, 20f)), "Could not create scene A fixture.");
			Require(CreateFixture("user://mod_editor_2d_history_b.tscn", "HistorySceneB", "SceneBNode", new Vector2(36f, 44f)), "Could not create scene B fixture.");
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_history_a.tscn");
			await WaitFrames(6);
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_history_b.tscn");
			await WaitFrames(8);
			TabBar tabs = editor.FindChild("SceneTabBar", recursive: true, owned: false) as TabBar;
			XWSceneNodeTree sceneTree = FindNodeOfType<XWSceneNodeTree>(XWEditorInterface.Instance?.GetSceneTreeDock());
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			Require(GodotObject.IsInstanceValid(tabs) && tabs.TabCount == 2, "The real 2D editor did not open both scene tabs.");
			Require(GodotObject.IsInstanceValid(sceneTree), "The real 2D scene tree is missing.");
			Require(GodotObject.IsInstanceValid(history), "The shared undo manager is missing.");
			if (!GodotObject.IsInstanceValid(tabs) || !GodotObject.IsInstanceValid(sceneTree) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			int historyA = editor.GetSceneHistoryIdAt(0L);
			int historyB = editor.GetSceneHistoryIdAt(1L);
			bool independent = historyA >= 1000 && historyB >= 1000 && historyA != historyB && history.HasHistory(historyA) && history.HasHistory(historyB);
			bool baseline = !editor.IsSceneTabDirty(0L) && !editor.IsSceneTabDirty(1L);
			Require(independent, "The two scene tabs did not receive independent dynamic history scopes.");
			Require(baseline, "A newly opened scene tab was dirty before editing.");
			tabs.CurrentTab = 0;
			await WaitFrames(4);
			CanvasItem nodeA = editor.CurrentSceneInstance?.FindChild("SceneANode", recursive: true, owned: false) as CanvasItem;
			Require(GodotObject.IsInstanceValid(nodeA), "Scene A editable node is missing after tab switch.");
			bool editA = GodotObject.IsInstanceValid(nodeA) && sceneTree.SetNodeVisibilityWithHistory(nodeA, visible: false);
			await WaitFrames(3);
			bool aDirtyOnly = editA && editor.IsSceneTabDirty(0L) && !editor.IsSceneTabDirty(1L) && tabs.GetTabTitle(0).StartsWith("(*)", StringComparison.Ordinal) && !tabs.GetTabTitle(1).StartsWith("(*)", StringComparison.Ordinal);
			tabs.CurrentTab = 1;
			await WaitFrames(4);
			CanvasItem nodeB = editor.CurrentSceneInstance?.FindChild("SceneBNode", recursive: true, owned: false) as CanvasItem;
			Require(GodotObject.IsInstanceValid(nodeB), "Scene B editable node is missing after tab switch.");
			bool editB = GodotObject.IsInstanceValid(nodeB) && sceneTree.SetNodeVisibilityWithHistory(nodeB, visible: false);
			await WaitFrames(3);
			bool interleaved = aDirtyOnly && editB && editor.IsSceneTabDirty(0L) && editor.IsSceneTabDirty(1L) && !nodeA.Visible && !nodeB.Visible;
			Require(interleaved, "Interleaved A/B edits did not preserve both tab-local dirty states.");
			tabs.CurrentTab = 0;
			await WaitFrames(4);
			bool undoCalled = history.Undo();
			await WaitFrames(3);
			bool isolatedUndo = undoCalled && nodeA.Visible && !nodeB.Visible && !editor.IsSceneTabDirty(0L) && editor.IsSceneTabDirty(1L) && history.GetCurrentHistoryType() == historyA;
			Require(isolatedUndo, "Undo on scene A changed scene B or used the wrong history scope.");
			Require(history.Redo(), "Redo on scene A was unavailable after isolated undo.");
			await WaitFrames(3);
			bool savedA = editor.SaveCurrentScene();
			await WaitFrames(3);
			bool afterSaveA = savedA && !editor.IsSceneTabDirty(0L) && editor.IsSceneTabDirty(1L);
			tabs.CurrentTab = 1;
			await WaitFrames(4);
			bool savedB = editor.SaveCurrentScene();
			await WaitFrames(3);
			bool separateSave = afterSaveA && savedB && !editor.IsSceneTabDirty(0L) && !editor.IsSceneTabDirty(1L);
			Require(separateSave, "Saving one scene tab incorrectly cleared the other tab's dirty state.");
			bool savedReload = ReloadedVisibility("user://mod_editor_2d_history_a.tscn", "SceneANode", expected: false) && ReloadedVisibility("user://mod_editor_2d_history_b.tscn", "SceneBNode", expected: false);
			Require(savedReload, "Separately saved A/B scene values did not survive cache-ignored reload.");
			bool postSaveEdit = sceneTree.SetNodeVisibilityWithHistory(nodeB, visible: true);
			await WaitFrames(2);
			bool postSaveDirty = postSaveEdit && editor.IsSceneTabDirty(1L);
			bool postSaveUndo = history.Undo();
			await WaitFrames(2);
			bool savedUndoClean = postSaveDirty && postSaveUndo && !nodeB.Visible && !editor.IsSceneTabDirty(1L) && history.HasRedo(historyB);
			Require(savedUndoClean, "Save -> edit -> undo did not return scene B to its saved token.");
			bool branchEdit = sceneTree.SetNodeLockWithHistory(nodeB, locked: true);
			await WaitFrames(3);
			bool branchDirty = branchEdit && editor.IsSceneTabDirty(1L) && !history.HasRedo(historyB) && sceneTree.IsNodeLocked(nodeB);
			Require(branchDirty, "Undo -> new branch did not remain dirty or discard scene B redo.");
			tabs.EmitSignal(TabBar.SignalName.TabClosePressed, Variant.From<long>(0L));
			await WaitFrames(4);
			bool closeClean = tabs.TabCount == 1 && !history.HasHistory(historyA) && history.HasHistory(historyB) && editor.GetSceneHistoryIdAt(0L) == historyB;
			Require(closeClean, "Closing clean scene A did not release only its history scope.");
			tabs.EmitSignal(TabBar.SignalName.TabClosePressed, Variant.From<long>(0L));
			await WaitFrames(3);
			ConfirmationDialog confirmationDialog = editor.FindChild("UnsavedSceneCloseDialog", recursive: true, owned: false) as ConfirmationDialog;
			bool closePrompt = GodotObject.IsInstanceValid(confirmationDialog) && confirmationDialog.Visible && tabs.TabCount == 1 && history.HasHistory(historyB);
			Require(closePrompt, "Closing dirty scene B did not show the unsaved-scene confirmation.");
			Button discard = editor.FindChild("DiscardUnsavedSceneButton", recursive: true, owned: false) as Button;
			discard?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(5);
			bool flag = GodotObject.IsInstanceValid(discard) && tabs.TabCount == 0 && !history.HasHistory(historyB) && editor.CurrentSceneInstance == null;
			bool flag2 = flag && history.GetCurrentHistoryType() == 0;
			Require(flag, "Discard-close did not close the dirty scene B tab.");
			Require(flag2, "Closing the final tab did not release history and return to Global scope.");
			GD.Print($"[MOD_EDITOR_2D_SCENE_TAB_HISTORY_PROBE] window={window} independent={independent} baseline={baseline} interleaved={interleaved} isolatedUndo={isolatedUndo} separateSave={separateSave} savedReload={savedReload} savedUndoClean={savedUndoClean} branchDirty={branchDirty} closeClean={closeClean} closePrompt={closePrompt} closeDiscard={flag} cleanup={flag2} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static bool CreateFixture(string path, string rootName, string childName, Vector2 position)
	{
		Node2D node2D = new Node2D
		{
			Name = rootName
		};
		ColorRect colorRect = new ColorRect
		{
			Name = childName,
			Position = position,
			Size = new Vector2(48f, 48f),
			Color = new Color(0.22f, 0.72f, 0.92f)
		};
		node2D.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		colorRect.Owner = node2D;
		PackedScene packedScene = new PackedScene
		{
			ResourceName = rootName
		};
		bool result = packedScene.Pack(node2D) == Error.Ok && ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) == Error.Ok;
		node2D.Free();
		return result;
	}

	private static bool ReloadedVisibility(string path, string childName, bool expected)
	{
		Node node = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
		CanvasItem canvasItem = node?.FindChild(childName, recursive: true, owned: false) as CanvasItem;
		bool result = GodotObject.IsInstanceValid(canvasItem) && canvasItem.Visible == expected;
		node?.Free();
		return result;
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await WaitFrames(1);
		}
		Require(condition: false, $"F3 did not initialize the 2D scene editor within {maxFrames} frames.");
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
			GD.PrintErr("[MOD_EDITOR_2D_SCENE_TAB_HISTORY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		string[] array = new string[2] { "user://mod_editor_2d_history_a.tscn", "user://mod_editor_2d_history_b.tscn" };
		foreach (string path in array)
		{
			if (FileAccess.FileExists(path))
			{
				DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
			}
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_SCENE_TAB_HISTORY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
