using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DTreeReparentRuntimeProbe : Node
{
	private const string ScenePath = "user://mod_editor_2d_tree_reparent_probe.tscn";

	private const float TransformTolerance = 0.002f;

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

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
			SendF3();
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			bool window = FindAncestorWindow(editor) != null;
			Require(window, "2D scene editor is not mounted under the real F3 ModEditor window.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			await WaitFrames(60);
			Require(CreateFixture(), "Could not create the 2D reparent fixture.");
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_tree_reparent_probe.tscn");
			await WaitFrames(12);
			Node root = editor.CurrentSceneInstance;
			Node2D parentA = root?.GetNodeOrNull<Node2D>("ParentA");
			Node2D parentB = root?.GetNodeOrNull<Node2D>("ParentB");
			Node2D nodeChild = parentA?.GetNodeOrNull<Node2D>("NodeChild");
			Control controlChild = parentA?.GetNodeOrNull<Control>("ControlChild");
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			TabBar tabs = editor.FindChild("SceneTabBar", recursive: true, owned: false) as TabBar;
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			int historyId = editor.GetCurrentSceneHistoryId();
			bool sceneLoaded = GodotObject.IsInstanceValid(root) && GodotObject.IsInstanceValid(parentA) && GodotObject.IsInstanceValid(parentB) && GodotObject.IsInstanceValid(nodeChild) && GodotObject.IsInstanceValid(controlChild) && GodotObject.IsInstanceValid(dock) && GodotObject.IsInstanceValid(history) && GodotObject.IsInstanceValid(tabs) && historyId >= 1000;
			Require(sceneLoaded, "The editable reparent fixture or editor services are incomplete.");
			if (!sceneLoaded)
			{
				Finish();
				return;
			}
			if (GodotObject.IsInstanceValid(inspector))
			{
				inspector.SetObject(this, forceRefresh: true);
				await WaitFrames(2);
			}
			GodotObject inspectorSentinel = inspector?.CurrentObject;
			string reason;
			bool flag = !dock.CanReparentNode(root, parentB, out reason) && reason.Contains("根", StringComparison.Ordinal);
			string reason2;
			bool flag2 = !dock.CanReparentNode(parentA, nodeChild, out reason2) && reason2.Contains("循环", StringComparison.Ordinal);
			string reason3;
			bool flag3 = dock.CanReparentNode(nodeChild, parentB, out reason3) && reason3.Contains("重挂", StringComparison.Ordinal);
			bool validation = flag && flag2 && flag3;
			Require(validation, "Root/cycle rejection or valid reparent acceptance is incorrect.");
			Transform2D nodeGlobalBefore = nodeChild.GlobalTransform;
			Transform2D controlGlobalBefore = controlChild.GetGlobalTransform();
			Vector2 controlSizeBefore = controlChild.Size;
			bool nodeReparent = dock.ReparentNodeWithHistory(nodeChild, parentB, "测试：重挂 Node2D 节点") && nodeChild.GetParent() == parentB && TransformApprox(nodeChild.GlobalTransform, nodeGlobalBefore) && nodeChild.Owner == root;
			Require(nodeReparent, "Node2D reparent did not preserve its parent, global transform, or Owner.");
			bool controlReparent = dock.ReparentNodeWithHistory(controlChild, parentB, "测试：重挂 Control 节点") && controlChild.GetParent() == parentB && TransformApprox(controlChild.GetGlobalTransform(), controlGlobalBefore) && controlChild.Size.IsEqualApprox(controlSizeBefore) && controlChild.Owner == root;
			Require(controlReparent, "Control reparent did not preserve its parent, global transform, size, or Owner.");
			bool transformPreserved = nodeReparent && controlReparent && dock.DidLastReparentPreserveGlobalTransform();
			bool ownerFixed = nodeChild.Owner == root && controlChild.Owner == root;
			bool historyScoped = dock.GetLastReparentHistoryId() == historyId && history.GetCurrentHistoryType() == historyId && dock.GetTrackedReparentNodeRecordCount(historyId) == 2 && history.GetCurrentActionName() == "测试：重挂 Control 节点";
			Require(transformPreserved, "The production API did not report preserved global transforms.");
			Require(ownerFixed, "Reparented nodes are not owned by the edited scene root.");
			Require(historyScoped, "Reparent actions did not use the active scene HistoryId.");
			bool undoControl = history.Undo() && controlChild.GetParent() == parentA && TransformApprox(controlChild.GetGlobalTransform(), controlGlobalBefore) && controlChild.Owner == root;
			await WaitFrames(2);
			bool undoNode = history.Undo() && nodeChild.GetParent() == parentA && TransformApprox(nodeChild.GlobalTransform, nodeGlobalBefore) && nodeChild.Owner == root;
			await WaitFrames(2);
			bool redoNode = history.Redo() && nodeChild.GetParent() == parentB && TransformApprox(nodeChild.GlobalTransform, nodeGlobalBefore) && nodeChild.Owner == root;
			await WaitFrames(2);
			bool redoControl = history.Redo() && controlChild.GetParent() == parentB && TransformApprox(controlChild.GetGlobalTransform(), controlGlobalBefore) && controlChild.Size.IsEqualApprox(controlSizeBefore) && controlChild.Owner == root;
			await WaitFrames(3);
			bool undoRedo = undoControl && undoNode && redoNode && redoControl && dock.GetTrackedReparentNodeRecordCount(historyId) == 2;
			Require(undoRedo, "Reparent undo/redo did not restore both exact node hierarchies.");
			Transform2D nodeSavedLocalTransform = nodeChild.Transform;
			Transform2D controlSavedLocalTransform = GetLocalCanvasTransform(controlChild);
			bool saved = editor.SaveCurrentScene();
			await WaitFrames(4);
			string details = "validation skipped";
			bool savedReload = saved && ValidateReloadedScene(nodeSavedLocalTransform, controlSavedLocalTransform, controlSizeBefore, out details);
			if (!savedReload)
			{
				GD.PrintErr($"[MOD_EDITOR_2D_TREE_REPARENT_PROBE_RELOAD] saved={saved} {details}");
			}
			Require(savedReload, "CacheMode.Ignore reload did not preserve reparent hierarchy, transforms, or Owner.");
			bool recordsBeforeClose = dock.GetTrackedReparentNodeRecordCount(historyId) == 2;
			bool inspectorUntouched = !GodotObject.IsInstanceValid(inspector) || inspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "Reparent editing redirected the raw Inspector.");
			tabs.EmitSignal(TabBar.SignalName.TabClosePressed, Variant.From<long>(0L));
			await WaitFrames(6);
			bool flag4 = recordsBeforeClose && tabs.TabCount == 0 && dock.GetTrackedReparentNodeRecordCount(historyId) == 0 && !history.HasHistory(historyId) && history.GetCurrentHistoryType() == 0;
			Require(flag4, "Closing the clean scene tab did not release its reparent records and history scope.");
			inspectorUntouched = inspectorUntouched && (!GodotObject.IsInstanceValid(inspector) || inspector.CurrentObject == inspectorSentinel);
			GD.Print($"[MOD_EDITOR_2D_TREE_REPARENT_PROBE] window={window} sceneLoaded={sceneLoaded} validation={validation} nodeReparent={nodeReparent} controlReparent={controlReparent} transformPreserved={transformPreserved} ownerFixed={ownerFixed} historyScoped={historyScoped} undoRedo={undoRedo} savedReload={savedReload} closeRelease={flag4} inspectorUntouched={inspectorUntouched} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static bool CreateFixture()
	{
		Node2D node2D = new Node2D
		{
			Name = "ReparentProbeRoot"
		};
		Node2D node2D2 = new Node2D
		{
			Name = "ParentA",
			Position = new Vector2(96f, 42f),
			Rotation = 0.28f,
			Scale = new Vector2(1.2f, 1.2f)
		};
		Node2D node2D3 = new Node2D
		{
			Name = "ParentB",
			Position = new Vector2(-72f, 118f),
			Rotation = -0.47f,
			Scale = new Vector2(0.78f, 0.78f)
		};
		Node2D node2D4 = new Node2D
		{
			Name = "NodeChild",
			Position = new Vector2(34f, -18f),
			Rotation = 0.31f,
			Scale = new Vector2(0.85f, 1.1f)
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "ControlChild",
			Position = new Vector2(-26f, 38f),
			Size = new Vector2(74f, 46f),
			PivotOffset = Vector2.Zero,
			Rotation = -0.22f,
			Scale = new Vector2(1.08f, 0.92f),
			Color = new Color(0.25f, 0.78f, 0.96f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		node2D3.Owner = node2D;
		node2D2.AddChild(node2D4, forceReadableName: false, InternalMode.Disabled);
		node2D2.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		node2D4.Owner = node2D;
		colorRect.Owner = node2D;
		PackedScene packedScene = new PackedScene
		{
			ResourceName = "2DTreeReparentProbe"
		};
		bool result = packedScene.Pack(node2D) == Error.Ok && ResourceSaver.Save(packedScene, "user://mod_editor_2d_tree_reparent_probe.tscn", ResourceSaver.SaverFlags.None) == Error.Ok;
		node2D.Free();
		return result;
	}

	private static bool ValidateReloadedScene(Transform2D expectedNodeLocalTransform, Transform2D expectedControlLocalTransform, Vector2 expectedControlSize, out string details)
	{
		Node node = ResourceLoader.Load<PackedScene>("user://mod_editor_2d_tree_reparent_probe.tscn", "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
		Node2D obj = node?.GetNodeOrNull<Node2D>("ParentB");
		Node2D node2D = obj?.GetNodeOrNull<Node2D>("NodeChild");
		Control control = obj?.GetNodeOrNull<Control>("ControlChild");
		bool flag = GodotObject.IsInstanceValid(node);
		bool flag2 = GodotObject.IsInstanceValid(obj);
		bool flag3 = GodotObject.IsInstanceValid(node2D);
		bool flag4 = GodotObject.IsInstanceValid(control);
		bool flag5 = flag3 && TransformApprox(node2D.Transform, expectedNodeLocalTransform);
		bool flag6 = flag4 && TransformApprox(GetLocalCanvasTransform(control), expectedControlLocalTransform);
		bool flag7 = flag4 && control.Size.IsEqualApprox(expectedControlSize);
		bool flag8 = flag3 && node2D.Owner == node;
		bool flag9 = flag4 && control.Owner == node;
		details = $"root={flag} parent={flag2} node={flag3} control={flag4} nodeTransform={flag5} controlTransform={flag6} controlSize={flag7} nodeOwner={flag8} controlOwner={flag9}";
		bool result = flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9;
		node?.Free();
		return result;
	}

	private static Transform2D GetLocalCanvasTransform(CanvasItem item)
	{
		if (!GodotObject.IsInstanceValid(item))
		{
			return Transform2D.Identity;
		}
		if (!(item.GetParent() is CanvasItem canvasItem))
		{
			return item.GetGlobalTransform();
		}
		return canvasItem.GetGlobalTransform().AffineInverse() * item.GetGlobalTransform();
	}

	private static bool TransformApprox(Transform2D actual, Transform2D expected)
	{
		if (actual.Origin.DistanceTo(expected.Origin) <= 0.002f && actual.X.DistanceTo(expected.X) <= 0.002f)
		{
			return actual.Y.DistanceTo(expected.Y) <= 0.002f;
		}
		return false;
	}

	private static void SendF3()
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
			GD.PrintErr("[MOD_EDITOR_2D_TREE_REPARENT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (FileAccess.FileExists("user://mod_editor_2d_tree_reparent_probe.tscn"))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://mod_editor_2d_tree_reparent_probe.tscn"));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_TREE_REPARENT_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
