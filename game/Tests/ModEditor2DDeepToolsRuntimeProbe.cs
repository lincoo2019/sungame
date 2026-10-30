using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DDeepToolsRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private string _scenePath = "";

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
			_scenePath = $"res://Tests/.mod_editor_2d_deep_tools_{Guid.NewGuid():N}.tscn";
			Require(SaveSourceScene(_scenePath), "Could not save the deep-tools source scene.");
			editor.LoadPackedSceneFromPath(_scenePath);
			await WaitFrames(10);
			Node editedRoot = editor.CurrentSceneInstance;
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWSceneNodeTree tree = FindNodeOfType<XWSceneNodeTree>(dock);
			XW2DViewport viewport = FindNodeOfType<XW2DViewport>(editor);
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node2D node2D = editedRoot?.FindChild("RigRoot", recursive: false, owned: false) as Node2D;
			Node2D node2D2 = node2D?.FindChild("RigChild", recursive: false, owned: false) as Node2D;
			Node2D node2D3 = node2D2?.FindChild("RigTip", recursive: false, owned: false) as Node2D;
			Path2D path = editedRoot?.FindChild("BezierPath", recursive: false, owned: false) as Path2D;
			Require(GodotObject.IsInstanceValid(editedRoot), "The editable source root is missing.");
			Require(GodotObject.IsInstanceValid(dock), "The real scene-tree dock is missing.");
			Require(GodotObject.IsInstanceValid(tree), "The real scene-node tree is missing.");
			Require(GodotObject.IsInstanceValid(viewport), "The real 2D viewport is missing.");
			Require(GodotObject.IsInstanceValid(history), "The Scene undo manager is missing.");
			Require(GodotObject.IsInstanceValid(node2D) && GodotObject.IsInstanceValid(node2D2) && GodotObject.IsInstanceValid(node2D3), "The source rig nodes are missing.");
			Require(GodotObject.IsInstanceValid(path), "The source Bezier Path2D is missing.");
			if (!GodotObject.IsInstanceValid(editedRoot) || !GodotObject.IsInstanceValid(dock) || !GodotObject.IsInstanceValid(tree) || !GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(history) || !GodotObject.IsInstanceValid(node2D) || !GodotObject.IsInstanceValid(node2D2) || !GodotObject.IsInstanceValid(node2D3) || !GodotObject.IsInstanceValid(path))
			{
				Finish();
				return;
			}
			viewport.SetZoom(1f);
			viewport.CenterAt(Vector2.Zero);
			history.ClearHistory();
			tree.PrepareForSceneHistoryBranchChange();
			GodotObject inspectorBeforeHistory = inspector?.CurrentObject;
			SelectRigSources(dock, node2D, node2D2, node2D3);
			Skeleton2D skeleton = editor.GenerateSkeletonFromSelectionWithHistory();
			await WaitFrames(4);
			Bone2D bone2D = skeleton?.FindChild("RigRootBone", recursive: false, owned: false) as Bone2D;
			Bone2D bone2D2 = bone2D?.FindChild("RigChildBone", recursive: false, owned: false) as Bone2D;
			Bone2D bone2D3 = bone2D2?.FindChild("RigTipBone", recursive: false, owned: false) as Bone2D;
			bool boneGenerate = GodotObject.IsInstanceValid(skeleton) && skeleton.GetParent() == editedRoot && skeleton.Owner == editedRoot && skeleton.HasMeta("_xw_generated_skeleton_2d") && GodotObject.IsInstanceValid(bone2D) && GodotObject.IsInstanceValid(bone2D2) && GodotObject.IsInstanceValid(bone2D3) && bone2D.GetParent() == skeleton && bone2D2.GetParent() == bone2D && bone2D3.GetParent() == bone2D2 && bone2D.Rest.IsEqualApprox(bone2D.Transform) && bone2D.GetLength() >= 16f && bone2D.HasMeta("_xw_bone_source_path") && history.GetCurrentActionName() == "从节点生成 2D 骨骼";
			Require(boneGenerate, "Bone generation did not create a persistent source-mapped Bone2D hierarchy.");
			bool generateUndoCalled = history.Undo();
			await WaitFrames(3);
			bool generateUndo = generateUndoCalled && GodotObject.IsInstanceValid(skeleton) && skeleton.GetParent() == null && dock.GetSelectedNode() == editedRoot;
			bool generateRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag = generateRedoCalled && skeleton.GetParent() == editedRoot && dock.GetSelectedNode() == skeleton;
			bool boneUndoRedo = generateUndo && flag;
			Require(boneUndoRedo, "Generated skeleton did not preserve identity through undo/redo.");
			bool generatedSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node = ReloadScene(_scenePath);
			Skeleton2D skeleton2D = node?.FindChild("GeneratedSkeleton2D", recursive: false, owned: false) as Skeleton2D;
			bool generatedReloaded = GodotObject.IsInstanceValid(skeleton2D) && skeleton2D.HasMeta("_xw_generated_skeleton_2d") && CountBones(skeleton2D) == 3 && skeleton2D.FindChild("RigTipBone", recursive: true, owned: false) is Bone2D;
			node?.Free();
			Require(generatedSaved && generatedReloaded, "Generated Skeleton2D/Bone2D hierarchy did not survive save/reload.");
			XWEditorInterface.Instance.SetSelectedNode(skeleton);
			dock.SelectNode(skeleton, emitSignal: false);
			bool clearApplied = editor.ClearGeneratedSkeletonWithHistory() && GodotObject.IsInstanceValid(skeleton) && skeleton.GetParent() == null && history.GetCurrentActionName() == "清除 2D 骨骼";
			bool clearUndoCalled = history.Undo();
			await WaitFrames(3);
			bool clearUndo = clearUndoCalled && skeleton.GetParent() == editedRoot && dock.GetSelectedNode() == skeleton;
			bool clearRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag2 = clearRedoCalled && skeleton.GetParent() == null;
			bool boneClear = clearApplied && clearUndo && flag2;
			Require(boneClear, "Bone clear did not detach and restore the exact skeleton through undo/redo.");
			bool clearedSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node2 = ReloadScene(_scenePath);
			bool clearedReloaded = GodotObject.IsInstanceValid(node2) && node2.FindChild("GeneratedSkeleton2D", recursive: true, owned: false) == null;
			node2?.Free();
			Require(clearedSaved && clearedReloaded, "Cleared skeleton state did not survive save/reload.");
			bool clearRestoreCalled = history.Undo();
			await WaitFrames(3);
			bool clearRestore = clearRestoreCalled && skeleton.GetParent() == editedRoot && history.HasRedo();
			Require(clearRestore, "Undoing clear did not restore the generated skeleton and redo branch.");
			SelectNodeForCanvas(dock, path);
			viewport.SetToolMode(XW2DViewport.ToolMode.Path);
			Vector2 anchor = path.Curve.GetPointPosition(1);
			ClickLeft(viewport, ViewAt(viewport, anchor));
			await WaitFrames(2);
			Vector2 oldOut = path.Curve.GetPointOut(1);
			Vector2 newOut = new Vector2(54f, -38f);
			DragLeft(viewport, ViewAt(viewport, anchor + oldOut), ViewAt(viewport, anchor + newOut));
			await WaitFrames(3);
			bool tangentOutApplied = path.Curve.GetPointOut(1).IsEqualApprox(newOut) && history.GetCurrentActionName() == "调整 2D 贝塞尔出切线";
			bool redoBranch = tangentOutApplied && !history.HasRedo() && skeleton.GetParent() == editedRoot;
			bool tangentOutUndoCalled = history.Undo();
			await WaitFrames(2);
			bool tangentOutUndo = tangentOutUndoCalled && path.Curve.GetPointOut(1).IsEqualApprox(oldOut);
			bool tangentOutRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag3 = tangentOutRedoCalled && path.Curve.GetPointOut(1).IsEqualApprox(newOut);
			bool tangentOut = tangentOutApplied && tangentOutUndo && flag3;
			Require(tangentOut, "Bezier out handle drag did not apply and undo/redo its tangent.");
			Require(redoBranch, "Bezier handle history did not discard the bone-clear redo branch while retaining the skeleton.");
			Vector2 oldIn = path.Curve.GetPointIn(1);
			Vector2 newIn = new Vector2(-50f, 36f);
			DragLeft(viewport, ViewAt(viewport, anchor + oldIn), ViewAt(viewport, anchor + newIn));
			await WaitFrames(3);
			bool tangentInApplied = path.Curve.GetPointIn(1).IsEqualApprox(newIn) && history.GetCurrentActionName() == "调整 2D 贝塞尔入切线";
			bool tangentInUndoCalled = history.Undo();
			await WaitFrames(2);
			bool tangentInUndo = tangentInUndoCalled && path.Curve.GetPointIn(1).IsEqualApprox(oldIn);
			bool tangentInRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag4 = tangentInRedoCalled && path.Curve.GetPointIn(1).IsEqualApprox(newIn);
			bool tangentIn = tangentInApplied && tangentInUndo && flag4;
			Require(tangentIn, "Bezier in handle drag did not apply and undo/redo its tangent.");
			bool finalSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node3 = ReloadScene(_scenePath);
			Path2D path2D = node3?.FindChild("BezierPath", recursive: false, owned: false) as Path2D;
			Skeleton2D skeleton2D2 = node3?.FindChild("GeneratedSkeleton2D", recursive: false, owned: false) as Skeleton2D;
			bool flag5 = GodotObject.IsInstanceValid(path2D) && path2D.Curve.GetPointOut(1).IsEqualApprox(newOut) && path2D.Curve.GetPointIn(1).IsEqualApprox(newIn) && GodotObject.IsInstanceValid(skeleton2D2) && CountBones(skeleton2D2) == 3;
			node3?.Free();
			Require(finalSaved && flag5, "Generated bones and Bezier tangents did not survive the final save/reload.");
			bool flag6 = dock.GetSelectedNode() == path;
			bool flag7 = XWEditorInterface.Instance.GetEditorSelection().IsSelected(path);
			bool flag8 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorBeforeHistory && inspector.CurrentObject != skeleton && inspector.CurrentObject != path;
			Require(flag6, "Deep-tool history did not keep the scene-tree highlight synchronized.");
			Require(flag7, "Deep-tool history did not keep the canvas selection synchronized.");
			Require(flag8, "Deep-tool history redirected the raw Inspector.");
			history.ClearHistory(1);
			tree.PrepareForSceneHistoryBranchChange();
			bool flag9 = !history.HasUndo() && !history.HasRedo() && tree.GetTrackedStructuralNodeRecordCount() == 0 && GodotObject.IsInstanceValid(skeleton) && skeleton.GetParent() == editedRoot;
			Require(flag9, "Clearing Scene history did not release deep-tool records while preserving applied bones.");
			GD.Print($"[MOD_EDITOR_2D_DEEP_TOOLS_PROBE] window={window} boneGenerate={boneGenerate} boneUndoRedo={boneUndoRedo} generatedSaved={generatedSaved} generatedReloaded={generatedReloaded} boneClear={boneClear} clearedSaved={clearedSaved} clearedReloaded={clearedReloaded} clearRestore={clearRestore} tangentOut={tangentOut} tangentIn={tangentIn} redoBranch={redoBranch} finalSaved={finalSaved} finalReloaded={flag5} treeSynced={flag6} canvasSelection={flag7} inspectorUntouched={flag8} historyRelease={flag9} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
			viewport.ClearSceneInstance();
			editedRoot.Free();
			await WaitFrames(2);
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
			Name = "DeepToolsRoot"
		};
		Node2D node2D2 = new Node2D
		{
			Name = "RigRoot",
			Position = new Vector2(-96f, -48f)
		};
		Node2D node2D3 = new Node2D
		{
			Name = "RigChild",
			Position = new Vector2(48f, 0f)
		};
		Node2D node2D4 = new Node2D
		{
			Name = "RigTip",
			Position = new Vector2(40f, 24f)
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D2.AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
		node2D3.AddChild(node2D4, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		node2D3.Owner = node2D;
		node2D4.Owner = node2D;
		Curve2D curve2D = new Curve2D();
		curve2D.AddPoint(new Vector2(-88f, 64f), Vector2.Zero, new Vector2(30f, -22f));
		curve2D.AddPoint(new Vector2(0f, 64f), new Vector2(-32f, 18f), new Vector2(36f, -20f));
		curve2D.AddPoint(new Vector2(88f, 64f), new Vector2(-30f, -22f), Vector2.Zero);
		Path2D path2D = new Path2D
		{
			Name = "BezierPath",
			Curve = curve2D
		};
		node2D.AddChild(path2D, forceReadableName: false, InternalMode.Disabled);
		path2D.Owner = node2D;
		PackedScene packedScene = new PackedScene();
		Error num = packedScene.Pack(node2D);
		node2D.Free();
		if (num == Error.Ok)
		{
			return ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		return false;
	}

	private static void SelectRigSources(XWSceneTreeDock dock, params Node[] sources)
	{
		XWEditorSelection editorSelection = XWEditorInterface.Instance.GetEditorSelection();
		editorSelection.Clear();
		foreach (Node node in sources)
		{
			editorSelection.SelectNode(node);
		}
		dock.SelectNode(sources[^1], emitSignal: false);
	}

	private static void SelectNodeForCanvas(XWSceneTreeDock dock, Node node)
	{
		dock.SelectNode(node, emitSignal: false);
		XWEditorInterface.Instance.SetSelectedNode(node);
	}

	private static int CountBones(Node node)
	{
		int num = ((node is Bone2D) ? 1 : 0);
		foreach (Node child in node.GetChildren())
		{
			num += CountBones(child);
		}
		return num;
	}

	private static Node ReloadScene(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
	}

	private static Vector2 ViewAt(XW2DViewport viewport, Vector2 world)
	{
		return viewport.Size * 0.5f + world;
	}

	private static void ClickLeft(XW2DViewport viewport, Vector2 position)
	{
		PressLeft(viewport, position);
		EmitCanvasInput(viewport, new InputEventMouseButton
		{
			Position = position,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		});
	}

	private static void PressLeft(XW2DViewport viewport, Vector2 position)
	{
		EmitCanvasInput(viewport, new InputEventMouseButton
		{
			Position = position,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		});
	}

	private static void DragLeft(XW2DViewport viewport, Vector2 from, Vector2 to)
	{
		PressLeft(viewport, from);
		EmitCanvasInput(viewport, new InputEventMouseMotion
		{
			Position = to,
			Relative = to - from,
			ButtonMask = MouseButtonMask.Left
		});
		EmitCanvasInput(viewport, new InputEventMouseButton
		{
			Position = to,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		});
	}

	private static void EmitCanvasInput(XW2DViewport viewport, InputEvent inputEvent)
	{
		viewport.EmitSignal(Control.SignalName.GuiInput, inputEvent);
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
			GD.PrintErr("[MOD_EDITOR_2D_DEEP_TOOLS_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (!string.IsNullOrWhiteSpace(_scenePath) && FileAccess.FileExists(_scenePath))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(_scenePath));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_DEEP_TOOLS_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
