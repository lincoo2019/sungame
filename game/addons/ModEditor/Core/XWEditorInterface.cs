using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.Layout;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.SceneEditor;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorInterface : RefCounted
{
	private XWUndoRedoManager _undoRedoManager;

	private XWEditorData _editorData;

	private XWEditorSettings _editorSettings;

	private XWEditorSelection _editorSelection;

	private XWEditorSelectionHistory _selectionHistory;

	private XWLayoutManager _layoutManager;

	private GodotObject _inspector;

	private Control _fileSystemPanel;

	private Control _blueprintEditor;

	private XWScriptEditor _scriptEditor;

	private XWSceneTreeDock _sceneTreeDock;

	private XW2DSceneEditor _scene2DEditor;

	private Control _animationEditor;

	private Control _resourceEditor;

	private readonly System.Collections.Generic.Dictionary<string, Control> _resourceEditorsByDockKey = new System.Collections.Generic.Dictionary<string, Control>();

	private Func<string, Control> _resourceEditorResolver;

	private Control _issuePanel;

	private XWOutputPanel _outputPanel;

	private RefCounted _fileSystem;

	private Control _editorPanel;

	private Node _editedSceneRoot;

	private Action<string> _showToastCallback;

	public static XWEditorInterface Instance { get; private set; }

	public static void Initialize(XWUndoRedoManager undoRedo, XWEditorData data, XWEditorSettings settings, XWEditorSelection selection, XWEditorSelectionHistory history)
	{
		Instance = new XWEditorInterface
		{
			_undoRedoManager = undoRedo,
			_editorData = data,
			_editorSettings = settings,
			_editorSelection = selection,
			_selectionHistory = history
		};
	}

	public void SetLayoutManager(XWLayoutManager layoutManager)
	{
		_layoutManager = layoutManager;
	}

	public void SetInspector(GodotObject inspector)
	{
		_inspector = inspector;
	}

	public void SetFileSystemPanel(Control fileSystemPanel)
	{
		_fileSystemPanel = fileSystemPanel;
	}

	public void SetBlueprintEditor(Control blueprintEditor)
	{
		_blueprintEditor = blueprintEditor;
	}

	public void SetScriptEditor(XWScriptEditor scriptEditor)
	{
		_scriptEditor = scriptEditor;
	}

	public void SetSceneTreeDock(XWSceneTreeDock sceneTreeDock)
	{
		_sceneTreeDock = sceneTreeDock;
	}

	public void Set2DSceneEditor(XW2DSceneEditor scene2DEditor)
	{
		_scene2DEditor = scene2DEditor;
	}

	public void SetAnimationEditor(Control animationEditor)
	{
		_animationEditor = animationEditor;
	}

	public void SetResourceEditor(Control resourceEditor)
	{
		_resourceEditor = resourceEditor;
	}

	public void SetResourceEditor(string dockKey, Control resourceEditor)
	{
		if (string.IsNullOrWhiteSpace(dockKey))
		{
			SetResourceEditor(resourceEditor);
			return;
		}
		_resourceEditorsByDockKey[dockKey] = resourceEditor;
		if (_resourceEditor == null)
		{
			_resourceEditor = resourceEditor;
		}
	}

	public void SetResourceEditorResolver(Func<string, Control> resolver)
	{
		_resourceEditorResolver = resolver;
	}

	public void SetIssuePanel(Control issuePanel)
	{
		_issuePanel = issuePanel;
	}

	public void SetOutputPanel(XWOutputPanel outputPanel)
	{
		_outputPanel = outputPanel;
	}

	public void SetFileSystem(RefCounted fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public void SetEditorPanel(Control panel)
	{
		_editorPanel = panel;
	}

	public void SetEditedSceneRoot(Node root)
	{
		_editedSceneRoot = root;
	}

	public void SetShowToastCallback(Action<string> callback)
	{
		_showToastCallback = callback;
	}

	public XWUndoRedoManager GetUndoRedoManager()
	{
		return _undoRedoManager;
	}

	public XWEditorData GetEditorData()
	{
		return _editorData;
	}

	public XWEditorSettings GetEditorSettings()
	{
		return _editorSettings;
	}

	public XWEditorSelection GetEditorSelection()
	{
		return _editorSelection;
	}

	public XWEditorSelectionHistory GetSelectionHistory()
	{
		return _selectionHistory;
	}

	public XWLayoutManager GetLayoutManager()
	{
		return _layoutManager;
	}

	public GodotObject GetInspector()
	{
		return _inspector;
	}

	public Control GetFileSystemPanel()
	{
		return _fileSystemPanel;
	}

	public Control GetBlueprintEditor()
	{
		return _blueprintEditor;
	}

	public XWScriptEditor GetScriptEditor()
	{
		return _scriptEditor;
	}

	public XWSceneTreeDock GetSceneTreeDock()
	{
		return _sceneTreeDock;
	}

	public XW2DSceneEditor Get2DSceneEditor()
	{
		return _scene2DEditor;
	}

	public int GetCurrentSceneHistoryId()
	{
		if (!GodotObject.IsInstanceValid(_scene2DEditor))
		{
			return 1;
		}
		return _scene2DEditor.GetCurrentSceneHistoryId();
	}

	public Control GetAnimationEditor()
	{
		return _animationEditor;
	}

	public Control GetResourceEditor()
	{
		return _resourceEditor;
	}

	public Control GetResourceEditor(string dockKey)
	{
		if (string.IsNullOrWhiteSpace(dockKey))
		{
			return _resourceEditor;
		}
		Control control = TryGetLoadedResourceEditor(dockKey);
		if (!GodotObject.IsInstanceValid(control))
		{
			return EnsureResourceEditor(dockKey);
		}
		return control;
	}

	public Control TryGetLoadedResourceEditor(string dockKey)
	{
		if (!string.IsNullOrWhiteSpace(dockKey) && _resourceEditorsByDockKey.TryGetValue(dockKey, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		return null;
	}

	public Control EnsureResourceEditor(string dockKey)
	{
		Control control = TryGetLoadedResourceEditor(dockKey);
		if (GodotObject.IsInstanceValid(control))
		{
			return control;
		}
		control = _resourceEditorResolver?.Invoke(dockKey);
		if (GodotObject.IsInstanceValid(control))
		{
			SetResourceEditor(dockKey, control);
		}
		return control;
	}

	public Control GetIssuePanel()
	{
		return _issuePanel;
	}

	public XWOutputPanel GetOutputPanel()
	{
		return _outputPanel;
	}

	public bool HasAnimationEditor()
	{
		if (_animationEditor != null)
		{
			return GodotObject.IsInstanceValid(_animationEditor);
		}
		return false;
	}

	public RefCounted GetFileSystem()
	{
		return _fileSystem;
	}

	public Control GetEditorPanel()
	{
		return _editorPanel;
	}

	public Node GetEditedSceneRoot()
	{
		return _editedSceneRoot;
	}

	public Node GetSceneRoot()
	{
		return _editedSceneRoot;
	}

	public void InspectObject(GodotObject obj)
	{
		if (obj is Resource res)
		{
			EditResource(res);
			return;
		}
		_inspector?.Call("EditObject", obj);
	}

	public void EditResource(Resource res)
	{
		EditResource(res, XWResourceEditContext.ForRoot(res, res?.ResourcePath ?? "", "resource_editor"));
	}

	public void EditResource(Resource res, XWResourceEditContext context)
	{
		if (GodotObject.IsInstanceValid(res))
		{
			if (context == null)
			{
				context = XWResourceEditContext.ForRoot(res, res.ResourcePath, "resource_editor");
			}
			if (!XWResourceEditorRegistry.TryOpen(res, context.ResourcePath, context))
			{
				ShowToast("可视化资源编辑器尚未就绪，资源未打开。", 2);
			}
		}
	}

	public void EditNode(Node node)
	{
		_inspector?.Call("EditObject", node);
	}

	public void FocusPanel(string key)
	{
		_layoutManager?.FocusPanel(key);
		XWEditorDock xWEditorDock = _layoutManager?.GetDock(key);
		if (GodotObject.IsInstanceValid(_scriptEditor) && (string.Equals(key, "script_editor", StringComparison.Ordinal) || (xWEditorDock != null && xWEditorDock.IsMainScreen)))
		{
			_scriptEditor.SetWorkspaceActive(string.Equals(key, "script_editor", StringComparison.Ordinal));
		}
	}

	public void ShowToast(string message, int severity = 0)
	{
		_showToastCallback?.Invoke(message);
	}

	public void AddOutputMessage(string message, XWOutputPanel.MessageType type = XWOutputPanel.MessageType.Std)
	{
		if (GodotObject.IsInstanceValid(_outputPanel))
		{
			_outputPanel.AddMessage(message, type);
		}
	}

	public void LogEditorAction(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			AddOutputMessage("动作: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public void LogEditorUndo(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			AddOutputMessage("撤销: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public void LogEditorRedo(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			AddOutputMessage("重做: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public Array<Node> GetSelectedNodes()
	{
		List<Node> list = _editorSelection?.GetSelectedNodes();
		if (list == null)
		{
			return new Array<Node>();
		}
		Array<Node> array = new Array<Node>();
		foreach (Node item in list)
		{
			array.Add(item);
		}
		return array;
	}

	public void SelectNode(Node node)
	{
		_editorSelection?.SelectNode(node);
	}

	public void SetSelectedNode(Node node)
	{
		_editorSelection?.Clear();
		_editorSelection?.SelectNode(node);
	}

	public void ClearSelection()
	{
		_editorSelection?.Clear();
	}

	public void AddInspectorPlugin(XWInspectorPlugin plugin)
	{
		if (_inspector is XWInspector xWInspector)
		{
			xWInspector.AddPlugin(plugin);
		}
	}

	public void RemoveInspectorPlugin(XWInspectorPlugin plugin)
	{
		if (_inspector is XWInspector xWInspector)
		{
			xWInspector.RemovePlugin(plugin);
		}
	}
}
