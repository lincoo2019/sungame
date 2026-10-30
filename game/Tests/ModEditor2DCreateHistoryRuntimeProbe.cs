using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DCreateHistoryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private string _scenePath = "";

	private string _instancePath = "";

	public override async void _Ready()
	{
		_ = 23;
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
			_scenePath = "res://Tests/.mod_editor_2d_create_history_" + text + ".tscn";
			_instancePath = "res://Tests/.mod_editor_2d_create_instance_" + text + ".tscn";
			Require(SavePackedScene(BuildSourceScene(), _scenePath), "Could not save the create-history source scene.");
			Require(SavePackedScene(BuildInstanceScene(), _instancePath), "Could not save the create-history instance fixture.");
			editor.LoadPackedSceneFromPath(_scenePath);
			await WaitFrames(10);
			Node oldRoot = editor.CurrentSceneInstance;
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWSceneNodeTree tree = FindNodeOfType<XWSceneNodeTree>(dock);
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Require(GodotObject.IsInstanceValid(oldRoot), "The editable source root is missing.");
			Require(GodotObject.IsInstanceValid(dock), "The real scene-tree dock is missing.");
			Require(GodotObject.IsInstanceValid(tree), "The real scene-node tree is missing.");
			Require(GodotObject.IsInstanceValid(history), "The Scene undo manager is missing.");
			if (!GodotObject.IsInstanceValid(oldRoot) || !GodotObject.IsInstanceValid(dock) || !GodotObject.IsInstanceValid(tree) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			history.ClearHistory();
			tree.PrepareForSceneHistoryBranchChange();
			GodotObject inspectorBeforeHistory = inspector?.CurrentObject;
			SelectForStructureEdit(dock, oldRoot);
			Node added = dock.AddNodeByClassWithHistory("Node2D", new Vector2(96f, 48f));
			await WaitFrames(2);
			bool addApplied = added is Node2D node2D && added.GetParent() == oldRoot && added.Owner == oldRoot && node2D.GlobalPosition.IsEqualApprox(new Vector2(96f, 48f)) && history.GetCurrentActionName() == "添加 2D 节点";
			bool addUndoCalled = history.Undo();
			await WaitFrames(2);
			bool addUndo = addUndoCalled && GodotObject.IsInstanceValid(added) && added.GetParent() == null && dock.GetSelectedNode() == oldRoot;
			bool addRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag = addRedoCalled && added.GetParent() == oldRoot && dock.GetSelectedNode() == added && ((Node2D)added).GlobalPosition.IsEqualApprox(new Vector2(96f, 48f));
			bool addNode = addApplied && addUndo && flag;
			Require(addNode, "Add Node did not preserve identity, owner, position, selection, and redo.");
			SelectForStructureEdit(dock, oldRoot);
			Node instantiated = dock.InstantiateSceneWithHistory(_instancePath);
			await WaitFrames(2);
			string instantiatedName = instantiated?.Name.ToString() ?? "";
			Node instantiatedLeaf = instantiated?.FindChild("InstanceLeaf", recursive: false, owned: false);
			bool instantiateApplied = GodotObject.IsInstanceValid(instantiated) && instantiated.GetParent() == oldRoot && instantiated.Owner == oldRoot && !string.IsNullOrEmpty(instantiated.SceneFilePath) && instantiatedLeaf?.Owner == instantiated && history.GetCurrentActionName() == "实例化 2D 场景";
			bool instantiateUndoCalled = history.Undo();
			await WaitFrames(2);
			bool instantiateUndo = instantiateUndoCalled && GodotObject.IsInstanceValid(instantiated) && instantiated.GetParent() == null;
			bool instantiateRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag2 = instantiateRedoCalled && instantiated.GetParent() == oldRoot && instantiatedLeaf?.Owner == instantiated && dock.GetSelectedNode() == instantiated;
			bool instantiate = instantiateApplied && instantiateUndo && flag2;
			Require(instantiate, "Instantiate Scene did not preserve identity, owners, selection, and redo.");
			Node componentManager = oldRoot.FindChild("ComponentManager", recursive: false, owned: false);
			SelectForStructureEdit(dock, componentManager);
			Node component = dock.AddComponentSceneWithHistory("res://Script/Component/TimerComponent/TimerComponent.tscn");
			await WaitFrames(3);
			bool componentApplied = component is ComponentBase && component.GetParent() == componentManager && component.Owner == oldRoot && history.GetCurrentActionName() == "添加 2D Component";
			bool componentUndoCalled = history.Undo();
			await WaitFrames(2);
			bool componentUndo = componentUndoCalled && GodotObject.IsInstanceValid(component) && component.GetParent() == null;
			bool componentRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag3 = componentRedoCalled && component.GetParent() == componentManager && dock.GetSelectedNode() == component;
			bool componentHistory = componentApplied && componentUndo && flag3;
			Require(componentHistory, "Add Component did not preserve identity, owner, selection, and redo.");
			bool directSelection = XWEditorInterface.Instance.GetEditorSelection().IsSelected(component);
			bool inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorBeforeHistory && inspector.CurrentObject != added && inspector.CurrentObject != instantiated && inspector.CurrentObject != component;
			Require(directSelection, "Create history did not synchronize direct canvas selection.");
			Require(inspectorUntouched, "Create history redirected the raw Inspector.");
			bool createSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node = ReloadScene(_scenePath);
			bool createReloaded = GodotObject.IsInstanceValid(node) && GodotObject.IsInstanceValid(node.FindChild(added.Name.ToString(), recursive: false, owned: false)) && GodotObject.IsInstanceValid(node.FindChild(instantiatedName, recursive: false, owned: false)) && GodotObject.IsInstanceValid(node.FindChild("TimerComponent", recursive: true, owned: false));
			node?.Free();
			Require(createSaved && createReloaded, "Created nodes, instance, and Component did not survive save/reload.");
			Node branch = oldRoot.FindChild("Branch", recursive: false, owned: false);
			Node oldParent = branch?.GetParent();
			int oldIndex = branch?.GetIndex() ?? (-1);
			SelectForStructureEdit(dock, branch);
			bool makeRootCalled = dock.MakeSelectedNodeRoot();
			await WaitFrames(3);
			bool makeRootApplied = makeRootCalled && editor.CurrentSceneInstance == branch && XWEditorInterface.Instance.GetEditedSceneRoot() == branch && tree.GetEditedSceneRoot() == branch && dock.GetSelectedNode() == branch && branch.Owner == null && oldRoot.GetParent() == null && history.GetCurrentActionName() == "设置 2D 场景根节点";
			bool rootSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node2 = ReloadScene(_scenePath);
			bool rootReloaded = GodotObject.IsInstanceValid(node2) && node2.Name == (StringName)"Branch" && GodotObject.IsInstanceValid(node2.FindChild("BranchLeaf", recursive: false, owned: false));
			node2?.Free();
			bool makeRootUndoCalled = history.Undo();
			await WaitFrames(3);
			bool makeRootUndo = makeRootUndoCalled && editor.CurrentSceneInstance == oldRoot && tree.GetEditedSceneRoot() == oldRoot && branch.GetParent() == oldParent && branch.GetIndex() == oldIndex && branch.Owner == oldRoot && dock.GetSelectedNode() == oldRoot;
			bool makeRootRedoCalled = history.Redo();
			await WaitFrames(3);
			bool makeRootRedo = makeRootRedoCalled && editor.CurrentSceneInstance == branch && tree.GetEditedSceneRoot() == branch && dock.GetSelectedNode() == branch;
			bool makeRootRestoreCalled = history.Undo();
			await WaitFrames(3);
			bool flag4 = makeRootRestoreCalled && editor.CurrentSceneInstance == oldRoot && branch.GetParent() == oldParent && branch.GetIndex() == oldIndex;
			bool makeRoot = makeRootApplied && rootSaved && rootReloaded && makeRootUndo && makeRootRedo && flag4;
			Require(makeRoot, "Make Root did not synchronize tree/canvas/tab state, save the active root, and undo/redo.");
			bool restoredSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node3 = ReloadScene(_scenePath);
			bool flag5 = GodotObject.IsInstanceValid(node3) && node3.Name == (StringName)"CreateHistoryRoot" && GodotObject.IsInstanceValid(node3.FindChild("Branch", recursive: false, owned: false));
			node3?.Free();
			Require(restoredSaved && flag5, "Undoing Make Root did not restore a saveable original root.");
			bool rootRecordBeforeBranch = tree.GetTrackedSceneRootRecordCount() == 1;
			SelectForStructureEdit(dock, branch);
			bool replacementRename = tree.RenameSelectedNode("BranchRenamed");
			await WaitFrames(2);
			bool rootRedoBranchRelease = rootRecordBeforeBranch && replacementRename && tree.GetTrackedSceneRootRecordCount() == 0 && !history.HasRedo() && GodotObject.IsInstanceValid(oldRoot) && branch.GetParent() == oldParent;
			Require(rootRedoBranchRelease, "A new action did not release the discarded Make Root redo record safely.");
			SelectForStructureEdit(dock, branch);
			Node rootDiscardCandidate = oldRoot;
			bool secondMakeRoot = dock.MakeSelectedNodeRoot();
			await WaitFrames(3);
			bool rootRecordBeforeClear = secondMakeRoot && tree.GetTrackedSceneRootRecordCount() == 1 && editor.CurrentSceneInstance == branch;
			history.ClearHistory(1);
			tree.PrepareForSceneHistoryBranchChange();
			await WaitFrames(2);
			bool rootClearRelease = rootRecordBeforeClear && tree.GetTrackedSceneRootRecordCount() == 0 && !GodotObject.IsInstanceValid(rootDiscardCandidate) && GodotObject.IsInstanceValid(branch) && editor.CurrentSceneInstance == branch;
			Require(rootClearRelease, "Clearing Scene history did not release the discarded old root while preserving the active root.");
			SelectForStructureEdit(dock, branch);
			Node discardedAdd = dock.AddNodeByClassWithHistory("Node2D");
			bool discardedAddUndo = history.Undo();
			await WaitFrames(2);
			Node node4 = branch.FindChild("BranchLeaf", recursive: false, owned: false);
			SelectForStructureEdit(dock, node4);
			bool branchRename = tree.RenameSelectedNode("BranchLeafRenamed");
			await WaitFrames(2);
			bool flag6 = discardedAddUndo && branchRename && !GodotObject.IsInstanceValid(discardedAdd) && !history.HasRedo();
			Require(flag6, "A new Scene action did not free an orphaned Add Node from the discarded redo branch.");
			GD.Print($"[MOD_EDITOR_2D_CREATE_HISTORY_PROBE] window={window} addNode={addNode} instantiate={instantiate} component={componentHistory} directSelection={directSelection} inspectorUntouched={inspectorUntouched} createSaved={createSaved} createReloaded={createReloaded} makeRoot={makeRoot} rootSaved={rootSaved} rootReloaded={rootReloaded} rootRedoBranchRelease={rootRedoBranchRelease} rootClearRelease={rootClearRelease} addRedoBranchRelease={flag6} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static Node2D BuildSourceScene()
	{
		Node2D node2D = new Node2D
		{
			Name = "CreateHistoryRoot"
		};
		Node2D node2D2 = new Node2D
		{
			Name = "ComponentManager"
		};
		Node2D node2D3 = new Node2D
		{
			Name = "Branch",
			Position = new Vector2(24f, 12f)
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "BranchLeaf",
			Position = new Vector2(-16f, -16f),
			Size = new Vector2(32f, 32f),
			Color = new Color(0.26f, 0.72f, 1f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
		node2D3.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		node2D3.Owner = node2D;
		colorRect.Owner = node2D;
		return node2D;
	}

	private static Node2D BuildInstanceScene()
	{
		Node2D node2D = new Node2D
		{
			Name = "InstanceFixture"
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "InstanceLeaf",
			Size = new Vector2(20f, 20f),
			Color = new Color(1f, 0.58f, 0.24f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		node2D.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		colorRect.Owner = node2D;
		return node2D;
	}

	private static bool SavePackedScene(Node root, string path)
	{
		PackedScene packedScene = new PackedScene();
		Error num = packedScene.Pack(root);
		root.Free();
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

	private static void SelectForStructureEdit(XWSceneTreeDock dock, Node node)
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
			GD.PrintErr("[MOD_EDITOR_2D_CREATE_HISTORY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		RemoveProbeFile(_scenePath);
		RemoveProbeFile(_instancePath);
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_CREATE_HISTORY_PROBE_FAILURE] " + failure);
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
