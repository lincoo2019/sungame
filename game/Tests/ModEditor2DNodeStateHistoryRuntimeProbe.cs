using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DNodeStateHistoryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private string _scenePath = "";

	private string _scriptPath = "";

	public override async void _Ready()
	{
		_ = 14;
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
			string text = Guid.NewGuid().ToString("N");
			_scenePath = "res://Tests/.mod_editor_2d_node_state_" + text + ".tscn";
			_scriptPath = "res://Tests/.mod_editor_2d_node_state_" + text + ".gd";
			Require(SaveSourceScene(_scenePath), "Could not save the node-state history source scene.");
			editor.LoadPackedSceneFromPath(_scenePath);
			await WaitFrames(10);
			Node currentSceneInstance = editor.CurrentSceneInstance;
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWSceneNodeTree tree = FindNodeOfType<XWSceneNodeTree>(dock);
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Require(GodotObject.IsInstanceValid(currentSceneInstance), "The editable source root is missing.");
			Require(GodotObject.IsInstanceValid(dock), "The real scene-tree dock is missing.");
			Require(GodotObject.IsInstanceValid(tree), "The real scene-node tree is missing.");
			Require(GodotObject.IsInstanceValid(history), "The Scene undo manager is missing.");
			if (!GodotObject.IsInstanceValid(currentSceneInstance) || !GodotObject.IsInstanceValid(dock) || !GodotObject.IsInstanceValid(tree) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			history.ClearHistory();
			tree.PrepareForSceneHistoryBranchChange();
			ColorRect target = currentSceneInstance.FindChild("StateTarget", recursive: false, owned: false) as ColorRect;
			Node scriptNode = currentSceneInstance.FindChild("ScriptNode", recursive: false, owned: false);
			GodotObject inspectorBeforeHistory = inspector?.CurrentObject;
			SelectForHistory(dock, target);
			bool visibilityApplied = tree.ToggleNodeVisibilityWithHistory(target) && !target.Visible && history.GetCurrentActionName() == "隐藏 2D 节点";
			bool visibilityUndoCalled = history.Undo();
			await WaitFrames(2);
			bool visibilityUndo = visibilityUndoCalled && target.Visible;
			bool visibilityRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag = visibilityRedoCalled && !target.Visible && dock.GetSelectedNode() == target;
			bool visibility = visibilityApplied && visibilityUndo && flag;
			Require(visibility, "Visibility did not apply, undo, and redo through Scene history.");
			bool lockApplied = tree.ToggleNodeLockWithHistory(target) && tree.IsNodeLocked(target) && target.HasMeta("_edit_lock_") && history.GetCurrentActionName() == "锁定 2D 节点";
			bool lockUndoCalled = history.Undo();
			await WaitFrames(2);
			bool lockUndo = lockUndoCalled && !tree.IsNodeLocked(target) && !target.HasMeta("_edit_lock_");
			bool lockRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag2 = lockRedoCalled && tree.IsNodeLocked(target) && target.HasMeta("_edit_lock_") && dock.GetSelectedNode() == target;
			bool lockHistory = lockApplied && lockUndo && flag2;
			Require(lockHistory, "Lock did not apply, undo, and redo through Scene history.");
			SelectForHistory(dock, scriptNode);
			Script attachedScript = dock.CreateAndAttachScriptWithHistory(_scriptPath);
			await WaitFrames(4);
			bool attachApplied = GodotObject.IsInstanceValid(attachedScript) && FileAccess.FileExists(_scriptPath) && scriptNode.GetScript().AsGodotObject() == attachedScript && dock.SelectedNodeHasScript() && history.GetCurrentActionName() == "挂载 2D 节点脚本";
			bool attachUndoCalled = history.Undo();
			await WaitFrames(2);
			bool attachUndo = attachUndoCalled && scriptNode.GetScript().VariantType == Variant.Type.Nil && !dock.SelectedNodeHasScript();
			bool attachRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag3 = attachRedoCalled && scriptNode.GetScript().AsGodotObject() == attachedScript && dock.SelectedNodeHasScript();
			bool attach = attachApplied && attachUndo && flag3;
			Require(attach, "Script attach did not preserve the script and toolbar state through undo/redo.");
			bool detachApplied = dock.DetachSelectedScriptWithHistory() && scriptNode.GetScript().VariantType == Variant.Type.Nil && !dock.SelectedNodeHasScript() && history.GetCurrentActionName() == "分离 2D 节点脚本";
			bool detachedSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node = ReloadScene(_scenePath);
			Node node2 = node?.FindChild("ScriptNode", recursive: false, owned: false);
			bool detachedReloaded = GodotObject.IsInstanceValid(node2) && node2.GetScript().VariantType == Variant.Type.Nil;
			node?.Free();
			bool detachUndoCalled = history.Undo();
			await WaitFrames(3);
			bool detachUndo = detachUndoCalled && scriptNode.GetScript().AsGodotObject() == attachedScript && dock.SelectedNodeHasScript();
			bool attachedSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node3 = ReloadScene(_scenePath);
			Script script = (node3?.FindChild("ScriptNode", recursive: false, owned: false))?.GetScript().AsGodotObject() as Script;
			bool attachedReloaded = GodotObject.IsInstanceValid(script) && script.ResourcePath == _scriptPath;
			node3?.Free();
			bool detach = detachApplied && detachUndo;
			Require(detach, "Script detach did not apply and restore the exact script through Scene history.");
			Require(detachedSaved && detachedReloaded, "Detached script state did not survive save/reload.");
			Require(attachedSaved && attachedReloaded, "Restored attached script state did not survive a second save/reload.");
			bool redoBeforeBranch = history.HasRedo();
			SelectForHistory(dock, target);
			bool branchVisibility = tree.SetNodeVisibilityWithHistory(target, visible: true);
			await WaitFrames(2);
			bool redoBranch = redoBeforeBranch && branchVisibility && !history.HasRedo() && scriptNode.GetScript().AsGodotObject() == attachedScript && target.Visible;
			Require(redoBranch, "A new node-state action did not discard the script-detach redo branch safely.");
			bool finalVisibility = tree.SetNodeVisibilityWithHistory(target, visible: false);
			bool finalSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node4 = ReloadScene(_scenePath);
			ColorRect colorRect = node4?.FindChild("StateTarget", recursive: false, owned: false) as ColorRect;
			Node node5 = node4?.FindChild("ScriptNode", recursive: false, owned: false);
			bool flag4 = GodotObject.IsInstanceValid(colorRect) && !colorRect.Visible && colorRect.HasMeta("_edit_lock_") && node5?.GetScript().AsGodotObject() is Script script2 && script2.ResourcePath == _scriptPath;
			node4?.Free();
			Require(finalVisibility && finalSaved && flag4, "Visibility, lock, and script state did not survive the final save/reload.");
			bool flag5 = dock.GetSelectedNode() == target && GodotObject.IsInstanceValid(tree.FindItemForNode(target));
			bool flag6 = XWEditorInterface.Instance.GetEditorSelection().IsSelected(target);
			bool flag7 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorBeforeHistory && inspector.CurrentObject != target && inspector.CurrentObject != scriptNode;
			Require(flag5, "Node-state history did not keep the scene-tree highlight synchronized.");
			Require(flag6, "Node-state history did not keep the canvas selection synchronized.");
			Require(flag7, "Node-state history redirected the raw Inspector.");
			GD.Print($"[MOD_EDITOR_2D_NODE_STATE_HISTORY_PROBE] window={window} visibility={visibility} lock={lockHistory} attach={attach} detach={detach} detachedSaved={detachedSaved} detachedReloaded={detachedReloaded} attachedSaved={attachedSaved} attachedReloaded={attachedReloaded} redoBranch={redoBranch} finalSaved={finalSaved} finalReloaded={flag4} treeSynced={flag5} canvasSelection={flag6} inspectorUntouched={flag7} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static bool SaveSourceScene(string path)
	{
		Node2D node2D = new Node2D
		{
			Name = "NodeStateHistoryRoot"
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "StateTarget",
			Position = new Vector2(-24f, -24f),
			Size = new Vector2(48f, 48f),
			Color = new Color(0.3f, 0.72f, 1f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		Node2D node2D2 = new Node2D
		{
			Name = "ScriptNode",
			Position = new Vector2(72f, 0f)
		};
		node2D.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		colorRect.Owner = node2D;
		node2D2.Owner = node2D;
		PackedScene packedScene = new PackedScene();
		Error num = packedScene.Pack(node2D);
		node2D.Free();
		if (num == Error.Ok)
		{
			return ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		return false;
	}

	private static Node ReloadScene(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
	}

	private static void SelectForHistory(XWSceneTreeDock dock, Node node)
	{
		dock.SelectNode(node, emitSignal: false);
		XWEditorInterface.Instance.SetSelectedNode(node);
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
			GD.PrintErr("[MOD_EDITOR_2D_NODE_STATE_HISTORY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		RemoveProbeFile(_scenePath);
		RemoveProbeFile(_scriptPath);
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_NODE_STATE_HISTORY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static void RemoveProbeFile(string path)
	{
		if (!string.IsNullOrWhiteSpace(path) && FileAccess.FileExists(path))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		}
	}
}
