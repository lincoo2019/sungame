using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.SceneEditor;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorUnifiedSaveRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _projectParent = "";

	public override async void _Ready()
	{
		bool window = false;
		bool menuCommands = false;
		bool plainSIdle = false;
		bool hiddenScriptIsolated = false;
		bool scripts = false;
		bool scenes = false;
		bool blueprints = false;
		bool resources = false;
		bool saveAllResult = false;
		bool failureReported = false;
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
			SendKey(Key.F3, ctrl: false);
			ModEditorPanel panel = await WaitForEditorPanel(900);
			Window editorWindow = FindAncestorWindow(panel);
			window = GodotObject.IsInstanceValid(editorWindow);
			Require(window, "Unified save probe is not running inside the real F3 ModEditor window.");
			if (!GodotObject.IsInstanceValid(panel))
			{
				Finish();
				return;
			}
			await WaitFrames(60);
			PopupMenu popupMenu = FindNodeNamed<PopupMenu>(editorWindow, "文件");
			menuCommands = GodotObject.IsInstanceValid(popupMenu) && popupMenu.GetItemIndex(2) >= 0 && popupMenu.GetItemIndex(6) >= 0 && popupMenu.GetItemText(popupMenu.GetItemIndex(2)).Contains("保存当前", StringComparison.Ordinal) && popupMenu.GetItemText(popupMenu.GetItemIndex(6)).Contains("保存全部", StringComparison.Ordinal);
			Require(menuCommands, "Top File menu does not expose independent Save Current and Save All commands.");
			string text = Guid.NewGuid().ToString("N");
			_projectParent = ProjectSettings.GlobalizePath("user://UnifiedSaveProbe");
			ModProject project = ModProject.Create(_projectParent, "Save" + text, "1.0.0", "probe", "unified save probe");
			Require(project != null, "Could not create the temporary Mod project.");
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			bool flag = project != null && (bool)(method?.Invoke(panel, new object[1] { project }) ?? ((object)false));
			Require(flag, "Could not enter the temporary Mod project.");
			if (!flag)
			{
				Finish();
				return;
			}
			await WaitFrames(12);
			XWScriptEditor scriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			XW2DSceneEditor sceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			XWBPEditor blueprintEditor = XWEditorInterface.Instance?.GetBlueprintEditor() as XWBPEditor;
			Require(GodotObject.IsInstanceValid(scriptEditor), "C# script editor is unavailable.");
			Require(GodotObject.IsInstanceValid(sceneEditor), "2D scene editor is unavailable.");
			Require(GodotObject.IsInstanceValid(blueprintEditor), "Blueprint editor is unavailable.");
			string scriptA = Path.Combine(project.ProjectPath, "Scripts", "SaveA.cs");
			string scriptB = Path.Combine(project.ProjectPath, "Scripts", "SaveB.cs");
			File.WriteAllText(scriptA, "public class BeforeA { }\n");
			File.WriteAllText(scriptB, "public class BeforeB { }\n");
			Require(scriptEditor.TryOpenFile(scriptA), "Could not open the first C# script.");
			XWCodeEdit xWCodeEdit = FindNodeOfType<XWCodeEdit>(scriptEditor);
			Require(GodotObject.IsInstanceValid(xWCodeEdit), "C# CodeEdit is unavailable.");
			if (GodotObject.IsInstanceValid(xWCodeEdit))
			{
				xWCodeEdit.Text = "public class AfterA { }\n";
				xWCodeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
			}
			Require(scriptEditor.TryOpenFile(scriptB), "Could not open the second C# script.");
			if (GodotObject.IsInstanceValid(xWCodeEdit))
			{
				xWCodeEdit.Text = "public class AfterB { }\n";
				xWCodeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
			}
			string text2 = File.ReadAllText(scriptB);
			scriptEditor.SetWorkspaceActive(active: true);
			scriptEditor._UnhandledInput(CreateKey(Key.S, ctrl: false));
			plainSIdle = File.ReadAllText(scriptB) == text2;
			Require(plainSIdle, "Plain S triggered a script save.");
			scriptEditor.SetWorkspaceActive(active: false);
			scriptEditor._UnhandledInput(CreateKey(Key.S, ctrl: true));
			hiddenScriptIsolated = File.ReadAllText(scriptB) == text2;
			Require(hiddenScriptIsolated, "Hidden script workspace intercepted Ctrl+S.");
			scriptEditor.SetWorkspaceActive(active: true);
			string sceneA = Path.Combine(project.ProjectPath, "Scenes", "SaveA.tscn");
			string sceneB = Path.Combine(project.ProjectPath, "Scenes", "SaveB.tscn");
			Require(CreateScene(sceneA, "SceneA"), "Could not create first 2D save fixture.");
			Require(CreateScene(sceneB, "SceneB"), "Could not create second 2D save fixture.");
			sceneEditor.LoadPackedSceneFromPath(sceneA);
			await WaitFrames(8);
			CommitScenePosition(sceneEditor, new Vector2(120f, 40f), "Save all scene A");
			sceneEditor.LoadPackedSceneFromPath(sceneB);
			await WaitFrames(8);
			CommitScenePosition(sceneEditor, new Vector2(220f, 80f), "Save all scene B");
			string blueprintAPath = Path.Combine(project.ProjectPath, "Blueprints", "SaveA.tres");
			string blueprintBPath = Path.Combine(project.ProjectPath, "Blueprints", "SaveB.tres");
			Directory.CreateDirectory(Path.GetDirectoryName(blueprintAPath));
			XWBPScript resource = XWBPScript.Create();
			XWBPScript resource2 = XWBPScript.Create();
			Require(ResourceSaver.Save(resource, blueprintAPath, ResourceSaver.SaverFlags.None) == Error.Ok, "Could not create first Blueprint fixture.");
			Require(ResourceSaver.Save(resource2, blueprintBPath, ResourceSaver.SaverFlags.None) == Error.Ok, "Could not create second Blueprint fixture.");
			resource = ResourceLoader.Load<XWBPScript>(blueprintAPath, "", ResourceLoader.CacheMode.Ignore);
			resource2 = ResourceLoader.Load<XWBPScript>(blueprintBPath, "", ResourceLoader.CacheMode.Ignore);
			blueprintEditor.Init(resource);
			blueprintEditor.AddVariableWithUndo(new XWBPVariableData
			{
				Name = "SavedA",
				Type = Variant.Type.Int
			});
			blueprintEditor.Init(resource2);
			blueprintEditor.AddVariableWithUndo(new XWBPVariableData
			{
				Name = "SavedB",
				Type = Variant.Type.String
			});
			XWGenericVisualResourceEditor resourceEditor = await CreateProbeResourceEditor(panel);
			Require(GodotObject.IsInstanceValid(resourceEditor), "Could not mount the generic resource save probe editor.");
			string resourceAPath = Path.Combine(project.ProjectPath, "Resources", "SaveA.tres");
			string resourceBPath = Path.Combine(project.ProjectPath, "Resources", "SaveB.tres");
			XWVisualEditorDescriptor descriptor = new XWVisualEditorDescriptor
			{
				Category = "Resource",
				DisplayName = "统一保存探针",
				DockKey = "unified_save_probe"
			};
			Resource resource3 = new Resource
			{
				ResourceName = "BeforeA"
			};
			Resource resource4 = new Resource
			{
				ResourceName = "BeforeB"
			};
			Require(ResourceSaver.Save(resource3, resourceAPath, ResourceSaver.SaverFlags.None) == Error.Ok, "Could not create first resource fixture.");
			Require(ResourceSaver.Save(resource4, resourceBPath, ResourceSaver.SaverFlags.None) == Error.Ok, "Could not create second resource fixture.");
			if (GodotObject.IsInstanceValid(resourceEditor))
			{
				resourceEditor.LoadResource(resource3, resourceAPath, descriptor);
				resource3.ResourceName = "AfterA";
				MarkResourceDirty(resourceEditor);
				resourceEditor.LoadResource(resource4, resourceBPath, descriptor);
				resource4.ResourceName = "AfterB";
				MarkResourceDirty(resourceEditor);
			}
			int num = scriptEditor.SaveAllTabs();
			int num2 = sceneEditor.SaveAllScenes(showToast: false);
			int num3 = blueprintEditor.SaveAllBlueprints(showToast: false);
			bool flag2 = resourceEditor.SaveAllOpenResources(showToast: false);
			bool flag3 = panel.SaveAllDocuments();
			saveAllResult = num >= 0 && num2 >= 0 && num3 >= 0 && flag2 && flag3;
			GD.Print($"[MOD_EDITOR_UNIFIED_SAVE_DETAIL] scripts={num} scenes={num2} blueprints={num3} resources={flag2} panel={flag3}");
			await WaitFrames(8);
			Require(saveAllResult, "SaveAllDocuments reported failure for valid open documents.");
			scripts = File.ReadAllText(scriptA).Contains("AfterA", StringComparison.Ordinal) && File.ReadAllText(scriptB).Contains("AfterB", StringComparison.Ordinal);
			scenes = ReadScenePosition(sceneA).IsEqualApprox(new Vector2(120f, 40f)) && ReadScenePosition(sceneB).IsEqualApprox(new Vector2(220f, 80f));
			XWBPScript xWBPScript = ResourceLoader.Load<XWBPScript>(blueprintAPath, "", ResourceLoader.CacheMode.Ignore);
			XWBPScript xWBPScript2 = ResourceLoader.Load<XWBPScript>(blueprintBPath, "", ResourceLoader.CacheMode.Ignore);
			blueprints = xWBPScript != null && xWBPScript.Variables.Count == 1 && xWBPScript2 != null && xWBPScript2.Variables.Count == 1;
			Resource resource5 = ResourceLoader.Load<Resource>(resourceAPath, "", ResourceLoader.CacheMode.Ignore);
			Resource resource6 = ResourceLoader.Load<Resource>(resourceBPath, "", ResourceLoader.CacheMode.Ignore);
			resources = resource5?.ResourceName == "AfterA" && resource6?.ResourceName == "AfterB";
			Require(scripts, "Save All did not persist every dirty C# tab.");
			Require(scenes, "Save All did not persist every dirty 2D scene tab.");
			Require(blueprints, "Save All did not persist every Blueprint document session.");
			Require(resources, "Save All did not persist every dirty resource tab.");
			Resource resource7 = new Resource
			{
				ResourceName = "NoPath"
			};
			resourceEditor.LoadResource(resource7, "", descriptor);
			MarkResourceDirty(resourceEditor);
			failureReported = !panel.SaveAllDocuments();
			Require(failureReported, "Save All masked an unsaved resource failure.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_UNIFIED_SAVE_PROBE] window={window} menuCommands={menuCommands} plainSIdle={plainSIdle} hiddenScriptIsolated={hiddenScriptIsolated} scripts={scripts} scenes={scenes} blueprints={blueprints} resources={resources} saveAllResult={saveAllResult} failureReported={failureReported} failures={_failures.Count}");
		Finish();
	}

	private static InputEventKey CreateKey(Key key, bool ctrl)
	{
		return new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			CtrlPressed = ctrl,
			Pressed = true
		};
	}

	private static void SendKey(Key key, bool ctrl)
	{
		Input.ParseInputEvent(CreateKey(key, ctrl));
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			CtrlPressed = ctrl,
			Pressed = false
		});
	}

	private static bool CreateScene(string path, string rootName)
	{
		Node2D node2D = new Node2D
		{
			Name = rootName
		};
		Node2D node2D2 = new Node2D
		{
			Name = "MoveTarget",
			Position = new Vector2(4f, 8f)
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
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

	private static void CommitScenePosition(XW2DSceneEditor editor, Vector2 position, string actionName)
	{
		Node2D node2D = editor.CurrentSceneInstance?.GetNodeOrNull<Node2D>("MoveTarget");
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		int currentSceneHistoryId = editor.GetCurrentSceneHistoryId();
		if (!GodotObject.IsInstanceValid(node2D) || !GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			throw new InvalidOperationException("2D scene history fixture is unavailable.");
		}
		Vector2 from = node2D.Position;
		xWUndoRedoManager.CreateAction(actionName, mergeMode: false, currentSceneHistoryId);
		xWUndoRedoManager.AddDoProperty(node2D, "position", Variant.From(in position));
		xWUndoRedoManager.AddUndoProperty(node2D, "position", Variant.From(in from));
		xWUndoRedoManager.CommitAction();
	}

	private static Vector2 ReadScenePosition(string path)
	{
		Node node = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
		Vector2 result = node?.GetNodeOrNull<Node2D>("MoveTarget")?.Position ?? new Vector2(1f / 0f, 1f / 0f);
		node?.Free();
		return result;
	}

	private async Task<XWGenericVisualResourceEditor> CreateProbeResourceEditor(ModEditorPanel panel)
	{
		XWGenericVisualResourceEditor editor = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWGenericVisualResourceEditor>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(editor))
		{
			return null;
		}
		panel.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(2);
		if (typeof(ModEditorPanel).GetField("_resourceEditors", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(panel) is Dictionary<string, XWGenericVisualResourceEditor> dictionary)
		{
			dictionary["unified_save_probe"] = editor;
		}
		return editor;
	}

	private static void MarkResourceDirty(XWGenericVisualResourceEditor editor)
	{
		typeof(XWGenericVisualResourceEditor).GetMethod("NotifyCurrentResourceEdited", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(editor, new object[1] { false });
	}

	private async Task<ModEditorPanel> WaitForEditorPanel(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			if (GodotObject.IsInstanceValid(modEditorPanel) && modEditorPanel.IsInsideTree())
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		Require(condition: false, $"F3 did not initialize ModEditorPanel within {maxFrames} frames.");
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

	private static T FindNodeNamed<T>(Node root, string name) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result && root.Name == (StringName)name)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeNamed<T>(child, name);
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
			GD.PrintErr("[MOD_EDITOR_UNIFIED_SAVE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_UNIFIED_SAVE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
