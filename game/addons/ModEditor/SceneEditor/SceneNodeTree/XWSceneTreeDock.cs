using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XWSceneTreeDock : VBoxContainer
{
	private enum ContextMenuOp
	{
		AddNode = 1,
		InstantiateScene,
		Rename,
		Duplicate,
		MoveUp,
		MoveDown,
		Delete,
		CopyNodePath,
		MakeRoot,
		SaveBranchAsScene,
		ExpandAll,
		CollapseAll,
		AddComponentScene
	}

	private const long MoreNodeId = 9999L;

	private const string ModComponentDirectory = "Battle/Components";

	private Button _addNodeBtn;

	private Button _instantiateBtn;

	private LineEdit _filterLineEdit;

	private Button _createScriptBtn;

	private Button _detachScriptBtn;

	private Button _extendScriptBtn;

	private Button _treeMenuBtn;

	private XWSceneNodeTree _sceneTree;

	private PopupMenu _addNodeMenu;

	private PopupMenu _contextMenu;

	private ConfirmationDialog _renameDialog;

	private LineEdit _renameLineEdit;

	private FileDialog _instantiateDialog;

	private FileDialog _componentSceneDialog;

	private FileDialog _scriptDialog;

	private FileDialog _saveBranchDialog;

	private readonly Dictionary<long, string> _addNodeClasses = new Dictionary<long, string>();

	private long _nextAddNodeId = 1L;

	private Node _pendingSceneRoot;

	private Vector2? _pendingAddNodeCanvasPosition;

	public override void _Ready()
	{
		_addNodeBtn = GetNode<Button>("%AddNode");
		_instantiateBtn = GetNode<Button>("%Instantiate");
		_filterLineEdit = GetNode<LineEdit>("%FilterLineEdit");
		_createScriptBtn = GetNode<Button>("%CreateScript");
		_detachScriptBtn = GetNode<Button>("%DetachScript");
		_extendScriptBtn = GetNode<Button>("%ExtendScript");
		_treeMenuBtn = GetNode<Button>("%TreeMenu");
		_sceneTree = GetNode<XWSceneNodeTree>("%SceneTree");
		_addNodeMenu = GetNode<PopupMenu>("%AddNodeMenu");
		_contextMenu = GetNode<PopupMenu>("%ContextMenu");
		_renameDialog = GetNode<ConfirmationDialog>("%RenameDialog");
		_renameLineEdit = GetNode<LineEdit>("%RenameLineEdit");
		_instantiateDialog = GetNode<FileDialog>("%InstantiateDialog");
		_componentSceneDialog = GetNode<FileDialog>("%ComponentSceneDialog");
		_scriptDialog = GetNode<FileDialog>("%ScriptDialog");
		_saveBranchDialog = GetNode<FileDialog>("%SaveBranchDialog");
		SetupToolbarIcons();
		SetupFileDialogs();
		SetupAddNodeMenu();
		SetupContextMenu();
		ConnectSignals();
		SyncSceneRoot();
		UpdateScriptButtons(_sceneTree.GetSelectedNode());
	}

	private void SetupToolbarIcons()
	{
		XWClassRegistry instance = XWClassRegistry.Instance;
		_addNodeBtn.Icon = instance.GetUIIcon("Add");
		_instantiateBtn.Icon = instance.GetUIIcon("Instance");
		_createScriptBtn.Icon = instance.GetUIIcon("ScriptCreate");
		_detachScriptBtn.Icon = instance.GetUIIcon("ScriptRemove");
		_extendScriptBtn.Icon = instance.GetUIIcon("ScriptExtend");
		_treeMenuBtn.Icon = instance.GetUIIcon("GuiTabMenuHl");
		Texture2D uIIcon = instance.GetUIIcon("Search");
		if (GodotObject.IsInstanceValid(uIIcon))
		{
			_filterLineEdit.RightIcon = uIIcon;
		}
	}

	private void SetupFileDialogs()
	{
		_instantiateDialog.Title = "实例化场景";
		_instantiateDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_instantiateDialog.Access = FileDialog.AccessEnum.Resources;
		_instantiateDialog.ClearFilters();
		_instantiateDialog.AddFilter("*.tscn", "场景文件");
		_componentSceneDialog.Title = "选择 Component 场景";
		_componentSceneDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_componentSceneDialog.Access = FileDialog.AccessEnum.Filesystem;
		_componentSceneDialog.ClearFilters();
		_componentSceneDialog.AddFilter("*.tscn", "Component 场景");
		_scriptDialog.Title = "创建脚本";
		_scriptDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		_scriptDialog.Access = FileDialog.AccessEnum.Resources;
		_scriptDialog.ClearFilters();
		_scriptDialog.AddFilter("*.cs", "C# 脚本");
		_saveBranchDialog.Title = "保存分支为场景";
		_saveBranchDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		_saveBranchDialog.Access = FileDialog.AccessEnum.Resources;
		_saveBranchDialog.ClearFilters();
		_saveBranchDialog.AddFilter("*.tscn", "场景文件");
	}

	private void SetupAddNodeMenu()
	{
		_addNodeMenu.Clear();
		_addNodeClasses.Clear();
		_nextAddNodeId = 1L;
		AddNodeMenuItem("Node");
		AddNodeMenuItem("Node2D");
		AddNodeMenuItem("CanvasLayer");
		_addNodeMenu.AddSeparator();
		AddNodeMenuItem("Sprite2D");
		AddNodeMenuItem("AnimatedSprite2D");
		AddNodeMenuItem("Camera2D");
		AddNodeMenuItem("Marker2D");
		AddNodeMenuItem("Path2D");
		AddNodeMenuItem("Line2D");
		AddNodeMenuItem("Polygon2D");
		_addNodeMenu.AddSeparator();
		AddNodeMenuItem("Area2D");
		AddNodeMenuItem("StaticBody2D");
		AddNodeMenuItem("CharacterBody2D");
		AddNodeMenuItem("CollisionShape2D");
		AddNodeMenuItem("CollisionPolygon2D");
		AddNodeMenuItem("RayCast2D");
		_addNodeMenu.AddSeparator();
		AddNodeMenuItem("Control");
		AddNodeMenuItem("Label");
		AddNodeMenuItem("Button");
		AddNodeMenuItem("TextureRect");
		AddNodeMenuItem("ColorRect");
		_addNodeMenu.AddSeparator();
		AddNodeMenuItem("Timer");
		AddNodeMenuItem("AudioStreamPlayer2D");
		AddNodeMenuItem("AnimationPlayer", "动画播放器");
		AddNodeMenuItem("AnimationTree", "动画树");
		AddNodeMenuItem("PointLight2D");
		AddNodeMenuItem("DirectionalLight2D");
		_addNodeMenu.AddSeparator();
		_addNodeMenu.AddIconItem(XWClassRegistry.Instance.GetUIIcon("Add"), "更多...", 9999, Key.None);
	}

	private void AddNodeMenuItem(string className, string displayName = null)
	{
		long num = _nextAddNodeId++;
		_addNodeClasses[num] = className;
		_addNodeMenu.AddIconItem(XWClassRegistry.Instance.GetClassIcon(className), string.IsNullOrWhiteSpace(displayName) ? className : displayName, (int)num, Key.None);
	}

	private void SetupContextMenu()
	{
		_contextMenu.Clear();
		XWClassRegistry instance = XWClassRegistry.Instance;
		_contextMenu.AddIconItem(instance.GetUIIcon("Add"), "添加子节点", 1, Key.None);
		_contextMenu.AddIconItem(instance.GetUIIcon("Instance"), "实例化场景...", 2, Key.None);
		Texture2D classIcon = instance.GetClassIcon("ComponentBase");
		_contextMenu.AddIconItem(GodotObject.IsInstanceValid(classIcon) ? classIcon : instance.GetUIIcon("Instance"), "添加 Component 节点...", 13, Key.None);
		_contextMenu.AddSeparator();
		_contextMenu.AddIconItem(instance.GetUIIcon("Rename"), "重命名", 3, Key.None);
		_contextMenu.AddIconItem(instance.GetUIIcon("Duplicate"), "复制", 4, Key.None);
		_contextMenu.AddSeparator();
		_contextMenu.AddIconItem(instance.GetUIIcon("MoveUp"), "上移", 5, Key.None);
		_contextMenu.AddIconItem(instance.GetUIIcon("MoveDown"), "下移", 6, Key.None);
		_contextMenu.AddSeparator();
		_contextMenu.AddIconItem(instance.GetUIIcon("Remove"), "删除", 7, Key.None);
		_contextMenu.AddSeparator();
		_contextMenu.AddItem("复制节点路径", 8, Key.None);
		_contextMenu.AddItem("设为场景根节点", 9, Key.None);
		_contextMenu.AddIconItem(instance.GetUIIcon("PackedScene"), "保存分支为场景...", 10, Key.None);
		_contextMenu.AddSeparator();
		_contextMenu.AddIconItem(instance.GetUIIcon("ExpandTree"), "展开全部", 11, Key.None);
		_contextMenu.AddIconItem(instance.GetUIIcon("CollapseTree"), "折叠全部", 12, Key.None);
	}

	private void ConnectSignals()
	{
		_addNodeBtn.Pressed += OnAddNodePressed;
		_instantiateBtn.Pressed += OnInstantiatePressed;
		_filterLineEdit.TextChanged += OnFilterTextChanged;
		_createScriptBtn.Pressed += OnCreateScriptPressed;
		_detachScriptBtn.Pressed += OnDetachScriptPressed;
		_extendScriptBtn.Pressed += OnExtendScriptPressed;
		_treeMenuBtn.Pressed += OnTreeMenuPressed;
		_addNodeMenu.IdPressed += OnAddNodeMenuItem;
		_contextMenu.IdPressed += OnContextMenuItem;
		_renameDialog.Confirmed += OnRenameConfirmed;
		_renameDialog.RegisterTextEnter(_renameLineEdit);
		_instantiateDialog.FileSelected += OnInstantiateFileSelected;
		_componentSceneDialog.FileSelected += OnComponentSceneFileSelected;
		_scriptDialog.FileSelected += OnScriptFileSelected;
		_saveBranchDialog.FileSelected += OnSaveBranchFileSelected;
		_sceneTree.ItemMouseSelected += OnTreeMouseSelected;
		_sceneTree.NodeSelected += OnNodeSelected;
		_sceneTree.HistorySelectionChanged += OnHistorySelectionChanged;
	}

	private void SyncSceneRoot()
	{
		Node node = _pendingSceneRoot;
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			node = XWEditorInterface.Instance?.GetEditedSceneRoot();
		}
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			XWEditorInterface.Instance?.SetEditedSceneRoot(node);
		}
		else
		{
			node = null;
			XWEditorInterface.Instance?.SetEditedSceneRoot(null);
		}
		_sceneTree.SetSceneRoot(node);
		UpdateSceneActionButtons(node);
	}

	private void OnAddNodePressed()
	{
		_pendingAddNodeCanvasPosition = null;
		PopupBelow(_addNodeBtn, _addNodeMenu);
	}

	private void OnAddNodeMenuItem(long id)
	{
		string value;
		if (id == 9999)
		{
			OpenCreateNodeDialog();
		}
		else if (_addNodeClasses.TryGetValue(id, out value))
		{
			AddNodeByClass(value);
		}
	}

	private void OpenCreateNodeDialog()
	{
		XWWindowNodeSelector xWWindowNodeSelector = XWWindowNodeSelector.Create();
		if (!GodotObject.IsInstanceValid(xWWindowNodeSelector))
		{
			XWEditorInterface.Instance?.ShowToast("节点选择器未就绪");
			return;
		}
		xWWindowNodeSelector.NodeTypeSelected += AddNodeByClass;
		AddChild(xWWindowNodeSelector, forceReadableName: false, InternalMode.Disabled);
		xWWindowNodeSelector.PopupCenteredClamped(new Vector2I(900, 700), 0.9f);
	}

	private void AddNodeByClass(string className)
	{
		AddNodeByClassWithHistory(className, _pendingAddNodeCanvasPosition);
	}

	public Node AddNodeByClassWithHistory(string className, Vector2? canvasPosition = null)
	{
		_pendingAddNodeCanvasPosition = null;
		Node node = _sceneTree?.GetEditedSceneRoot();
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开或新建 2D 场景");
			return null;
		}
		Node node2 = CreateNodeByClass(className);
		if (node2 == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法创建节点类型: " + className);
			return null;
		}
		node2.Name = BuildNodeName(className, node2);
		Node node3 = _sceneTree?.AddChildNodeWithHistory(node2);
		if (node3 != null)
		{
			ApplyInitialCanvasPosition(node3, canvasPosition);
			XWEditorInterface.Instance?.ShowToast($"已添加节点: {node3.Name}");
			XWEditorInterface.Instance?.Get2DSceneEditor()?.QueueViewportRedraw();
		}
		else if (GodotObject.IsInstanceValid(node2) && node2.GetParent() == null)
		{
			node2.Free();
		}
		return node3;
	}

	private static void ApplyInitialCanvasPosition(Node node, Vector2? canvasPosition)
	{
		if (canvasPosition.HasValue && node != null && GodotObject.IsInstanceValid(node))
		{
			if (node is Node2D node2D)
			{
				node2D.GlobalPosition = canvasPosition.Value;
			}
			else if (node is Control control)
			{
				control.GlobalPosition = canvasPosition.Value;
			}
		}
	}

	private static Node CreateNodeByClass(string className)
	{
		if (string.IsNullOrWhiteSpace(className))
		{
			return null;
		}
		try
		{
			XWClassRegistry instance = XWClassRegistry.Instance;
			if (instance.HasClass(className) && instance.CanInstantiate(className) && instance.Instantiate(className).AsGodotObject() is Node result)
			{
				return result;
			}
			if (ClassDB.CanInstantiate(className) && ClassDB.Instantiate(className).AsGodotObject() is Node result2)
			{
				return result2;
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("创建节点失败: " + className + "\n" + ex.Message);
		}
		return null;
	}

	private static string BuildNodeName(string className, Node node)
	{
		string text = (string.IsNullOrWhiteSpace(className) ? node.GetClass() : className);
		int num = text.LastIndexOf('.');
		if (num >= 0 && num < text.Length - 1)
		{
			string text2 = text;
			int num2 = num + 1;
			text = text2.Substring(num2, text2.Length - num2);
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return node.GetClass();
	}

	private void OnInstantiatePressed()
	{
		Node node = _sceneTree?.GetEditedSceneRoot();
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开或新建 2D 场景");
		}
		else
		{
			_instantiateDialog.PopupCenteredClamped(new Vector2I(700, 500), 0.9f);
		}
	}

	private void OnInstantiateFileSelected(string path)
	{
		InstantiateSceneWithHistory(path);
	}

	public Node InstantiateSceneWithHistory(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			XWEditorInterface.Instance?.ShowToast("场景不存在: " + path);
			return null;
		}
		Node node = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
		if (node != null)
		{
			Node node2 = node;
			node2.Name = path.GetFile().GetBaseName();
			Node obj = _sceneTree?.AddChildNodeWithHistory(node2, "实例化 2D 场景");
			if (obj != null)
			{
				XWEditorInterface instance = XWEditorInterface.Instance;
				if (instance != null)
				{
					instance.ShowToast("已实例化: " + path.GetFile());
					return obj;
				}
				return obj;
			}
			if (GodotObject.IsInstanceValid(node2) && node2.GetParent() == null)
			{
				node2.Free();
			}
			return obj;
		}
		XWEditorInterface.Instance?.ShowToast("无法实例化该场景");
		return null;
	}

	private void ShowComponentSceneDialog()
	{
		if (!CanAddComponentSceneToSelectedNode())
		{
			XWEditorInterface.Instance?.ShowToast("请选择 ComponentManager 节点后再添加组件");
			return;
		}
		string text = FindPreferredComponentSceneDirectory();
		_componentSceneDialog.Access = (FileDialog.AccessEnum)(text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? 0 : 2);
		_componentSceneDialog.CurrentDir = text;
		_componentSceneDialog.PopupCenteredClamped(new Vector2I(700, 500), 0.9f);
	}

	private void OnComponentSceneFileSelected(string path)
	{
		AddComponentSceneWithHistory(path);
	}

	public Node AddComponentSceneWithHistory(string path)
	{
		Node node = _sceneTree?.GetSelectedNode();
		if (!IsComponentContainerNode(node))
		{
			XWEditorInterface.Instance?.ShowToast("当前节点不能添加 Component 场景");
			return null;
		}
		if (!ResourceLoader.Exists(path) && !Godot.FileAccess.FileExists(path))
		{
			XWEditorInterface.Instance?.ShowToast("Component 场景不存在: " + path);
			return null;
		}
		Node node2 = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
		if (node2 != null)
		{
			Node node3 = node2;
			if (!(node3 is ComponentBase))
			{
				node3.QueueFree();
				XWEditorInterface.Instance?.ShowToast("选择的场景根节点不是 ComponentBase");
				return null;
			}
			if (string.IsNullOrWhiteSpace(node3.Name))
			{
				node3.Name = path.GetFile().GetBaseName();
			}
			Node node4 = _sceneTree?.AddChildNodeToWithHistory(node, node3, "添加 2D Component");
			if (node4 != null)
			{
				XWEditorInterface.Instance?.ShowToast($"已添加 Component 节点: {node4.Name}");
				XWEditorInterface.Instance?.Get2DSceneEditor()?.QueueViewportRedraw();
			}
			else if (GodotObject.IsInstanceValid(node3) && node3.GetParent() == null)
			{
				node3.Free();
			}
			return node4;
		}
		XWEditorInterface.Instance?.ShowToast("无法实例化 Component 场景");
		return null;
	}

	public bool CanAddComponentSceneToSelectedNode()
	{
		return IsComponentContainerNode(_sceneTree?.GetSelectedNode());
	}

	private static bool IsComponentContainerNode(Node node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		if (node is TowerDefenseCharacter || node is LegacyComponentManagerNode)
		{
			return true;
		}
		return node.Name.ToString().Equals("ComponentManager", StringComparison.OrdinalIgnoreCase);
	}

	private static string FindPreferredComponentSceneDirectory()
	{
		string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = Path.Combine(text, "Battle/Components".Replace('/', Path.DirectorySeparatorChar));
			if (Directory.Exists(text2))
			{
				return text2.Replace('\\', '/');
			}
		}
		return "res://Script/Component";
	}

	private void OnFilterTextChanged(string filter)
	{
		_sceneTree.ApplyFilter(filter);
	}

	private void UpdateContextMenuState()
	{
		SetContextMenuDisabled(13, !CanAddComponentSceneToSelectedNode());
	}

	private void SetContextMenuDisabled(int itemId, bool disabled)
	{
		int itemIndex = _contextMenu.GetItemIndex(itemId);
		if (itemIndex >= 0)
		{
			_contextMenu.SetItemDisabled(itemIndex, disabled);
		}
	}

	private void OnTreeMouseSelected(Vector2 position, long mouseButtonIndex)
	{
		if (mouseButtonIndex == 2)
		{
			UpdateContextMenuState();
			PopupAtMouse(_contextMenu);
		}
	}

	private void OnContextMenuItem(long id)
	{
		switch ((ContextMenuOp)id)
		{
		case ContextMenuOp.AddNode:
			PopupAddNodeMenuAtMouse();
			break;
		case ContextMenuOp.InstantiateScene:
			ShowInstantiateDialog();
			break;
		case ContextMenuOp.AddComponentScene:
			ShowComponentSceneDialog();
			break;
		case ContextMenuOp.Rename:
			ShowRenameSelectedDialog();
			break;
		case ContextMenuOp.Duplicate:
			DuplicateSelectedNode();
			break;
		case ContextMenuOp.MoveUp:
			MoveSelectedNode(-1);
			break;
		case ContextMenuOp.MoveDown:
			MoveSelectedNode(1);
			break;
		case ContextMenuOp.Delete:
			RemoveSelectedNode();
			break;
		case ContextMenuOp.CopyNodePath:
			CopySelectedNodePath();
			break;
		case ContextMenuOp.MakeRoot:
			if (MakeSelectedNodeRoot())
			{
				XWEditorInterface.Instance?.ShowToast("已设置场景根节点");
			}
			break;
		case ContextMenuOp.SaveBranchAsScene:
			ShowSaveBranchDialogForSelected();
			break;
		case ContextMenuOp.ExpandAll:
			_sceneTree.GetRoot()?.SetCollapsedRecursive(enable: false);
			break;
		case ContextMenuOp.CollapseAll:
			_sceneTree.GetRoot()?.SetCollapsedRecursive(enable: true);
			break;
		}
	}

	private void ShowRenameDialog()
	{
		Node selectedNode = _sceneTree.GetSelectedNode();
		if (selectedNode != null)
		{
			_renameLineEdit.Text = selectedNode.Name;
			_renameDialog.PopupCentered(new Vector2I(360, 110));
			_renameLineEdit.GrabFocus();
			_renameLineEdit.SelectAll();
		}
	}

	private void OnRenameConfirmed()
	{
		if (!_sceneTree.RenameSelectedNode(_renameLineEdit.Text))
		{
			XWEditorInterface.Instance?.ShowToast("无法重命名该节点");
		}
	}

	public void CopySelectedNodePath()
	{
		Node selectedNode = _sceneTree.GetSelectedNode();
		if (selectedNode != null)
		{
			string text = BuildNodePath(selectedNode);
			DisplayServer.ClipboardSet(text);
			XWEditorInterface.Instance?.ShowToast("已复制节点路径: " + text);
		}
	}

	private static string BuildNodePath(Node node)
	{
		List<string> list = new List<string>();
		for (Node node2 = node; node2 != null; node2 = node2.GetParent())
		{
			list.Add(node2.Name);
		}
		list.Reverse();
		return string.Join("/", list);
	}

	private void ShowSaveBranchDialog()
	{
		Node selectedNode = _sceneTree.GetSelectedNode();
		if (selectedNode != null)
		{
			_saveBranchDialog.CurrentFile = $"{selectedNode.Name}.tscn";
			_saveBranchDialog.PopupCenteredClamped(new Vector2I(700, 500), 0.9f);
		}
	}

	private void OnSaveBranchFileSelected(string path)
	{
		Node selectedNode = _sceneTree.GetSelectedNode();
		if (selectedNode != null)
		{
			PackedScene packedScene;
			Error error = PackBranchPreservingOwners(selectedNode, out packedScene);
			if (error == Error.Ok)
			{
				error = ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None);
			}
			XWEditorInterface.Instance?.ShowToast((error == Error.Ok) ? ("已保存场景: " + path) : $"保存场景失败: {error}");
		}
	}

	public static Error PackBranchPreservingOwners(Node branchRoot, out PackedScene packedScene)
	{
		packedScene = new PackedScene();
		if (branchRoot == null || !GodotObject.IsInstanceValid(branchRoot))
		{
			return Error.InvalidParameter;
		}
		HashSet<Node> hashSet = new HashSet<Node>();
		Dictionary<Node, Node> dictionary = new Dictionary<Node, Node>();
		CollectBranchNodes(branchRoot, hashSet, dictionary);
		try
		{
			branchRoot.Owner = null;
			foreach (Node item in hashSet)
			{
				if (item != branchRoot)
				{
					Node node = dictionary[item];
					if (node == null || !hashSet.Contains(node))
					{
						item.Owner = branchRoot;
					}
				}
			}
			return packedScene.Pack(branchRoot);
		}
		finally
		{
			foreach (var (node4, node5) in dictionary)
			{
				if (GodotObject.IsInstanceValid(node4))
				{
					node4.Owner = (GodotObject.IsInstanceValid(node5) ? node5 : null);
				}
			}
		}
	}

	private static void CollectBranchNodes(Node node, HashSet<Node> subtree, Dictionary<Node, Node> ownerSnapshot)
	{
		subtree.Add(node);
		ownerSnapshot[node] = node.Owner;
		foreach (Node child in node.GetChildren())
		{
			CollectBranchNodes(child, subtree, ownerSnapshot);
		}
	}

	private void OnNodeSelected(Node node)
	{
		UpdateScriptButtons(node);
		XWEditorInterface.Instance?.SetSelectedNode(node);
		XWEditorInterface.Instance?.Get2DSceneEditor()?.ShowDirectNodeProperties(node, focusTab: false);
	}

	private void OnHistorySelectionChanged(Node node)
	{
		UpdateScriptButtons(node);
	}

	private void UpdateScriptButtons(Node node)
	{
		bool flag = node != null && GodotObject.IsInstanceValid(node);
		bool flag2 = flag && HasScript(node);
		_createScriptBtn.Visible = flag && !flag2;
		_detachScriptBtn.Visible = flag2;
		_extendScriptBtn.Visible = flag2;
		_createScriptBtn.Disabled = !flag;
		_detachScriptBtn.Disabled = !flag2;
		_extendScriptBtn.Disabled = !flag2;
	}

	private void UpdateSceneActionButtons(Node sceneRoot)
	{
		bool flag = sceneRoot != null && GodotObject.IsInstanceValid(sceneRoot);
		_addNodeBtn.Disabled = !flag;
		_instantiateBtn.Disabled = !flag;
		_treeMenuBtn.Disabled = !flag;
	}

	private void OnCreateScriptPressed()
	{
		Node selectedNode = _sceneTree.GetSelectedNode();
		if (selectedNode != null)
		{
			_scriptDialog.Title = "创建脚本";
			_scriptDialog.CurrentFile = $"{selectedNode.Name}.cs";
			_scriptDialog.PopupCenteredClamped(new Vector2I(700, 500), 0.9f);
		}
	}

	private void OnDetachScriptPressed()
	{
		DetachSelectedScriptWithHistory();
	}

	public bool DetachSelectedScriptWithHistory()
	{
		Node node = _sceneTree?.GetSelectedNode();
		if (node == null || !HasScript(node))
		{
			return false;
		}
		bool num = _sceneTree.SetNodeScriptWithHistory(node, null, "分离 2D 节点脚本");
		if (num)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance == null)
			{
				return num;
			}
			instance.ShowToast("已分离脚本");
		}
		return num;
	}

	private void OnExtendScriptPressed()
	{
		OnCreateScriptPressed();
	}

	private void OnScriptFileSelected(string path)
	{
		CreateAndAttachScriptWithHistory(path);
	}

	public Script CreateAndAttachScriptWithHistory(string path)
	{
		Node node = _sceneTree?.GetSelectedNode();
		if (node == null)
		{
			return null;
		}
		string text = path.Replace('\\', '/');
		if (string.IsNullOrWhiteSpace(text.GetExtension()))
		{
			text += ".cs";
		}
		if (!XWScriptEditor.IsSupportedScriptPath(text))
		{
			XWEditorInterface.Instance?.ShowToast("2D 节点脚本仅支持 C#。", 2);
			return null;
		}
		string baseDir = text.GetBaseDir();
		if (!string.IsNullOrEmpty(baseDir) && !DirAccess.DirExistsAbsolute(baseDir))
		{
			Error error = DirAccess.MakeDirRecursiveAbsolute(baseDir);
			if (error != Error.Ok)
			{
				XWEditorInterface.Instance?.ShowToast($"无法创建脚本目录: {error}");
				return null;
			}
		}
		string value = SanitizeClassName(text.GetFile().GetBaseName());
		string text2 = $"using Godot;\n\npublic partial class {value} : {node.GetClass()}\n{{\n    public override void _Ready()\n    {{\n    }}\n}}\n";
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(text, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			XWEditorInterface.Instance?.ShowToast($"无法创建脚本: {Godot.FileAccess.GetOpenError()}");
			return null;
		}
		fileAccess.StoreString(text2);
		fileAccess.Close();
		Script script = ResourceLoader.Load<Script>(text, null, ResourceLoader.CacheMode.Reuse);
		if (GodotObject.IsInstanceValid(script))
		{
			if (!_sceneTree.SetNodeScriptWithHistory(node, script, "挂载 2D 节点脚本"))
			{
				return null;
			}
			XWEditorInterface.Instance?.ShowToast("已创建并挂载脚本: " + text);
			return script;
		}
		XWEditorInterface.Instance?.ShowToast("脚本已创建，编译后可挂载: " + text);
		return null;
	}

	private static string SanitizeClassName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "NewNodeScript";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in value)
		{
			if (char.IsLetterOrDigit(c) || c == '_')
			{
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length == 0)
		{
			stringBuilder.Append("NewNodeScript");
		}
		if (!char.IsLetter(stringBuilder[0]) && stringBuilder[0] != '_')
		{
			stringBuilder.Insert(0, '_');
		}
		return stringBuilder.ToString();
	}

	private void OnTreeMenuPressed()
	{
		UpdateContextMenuState();
		PopupBelow(_treeMenuBtn, _contextMenu);
	}

	public Node GetSelectedNode()
	{
		return _sceneTree?.GetSelectedNode();
	}

	public bool SelectedNodeHasScript()
	{
		Node selectedNode = GetSelectedNode();
		if (selectedNode != null)
		{
			return HasScript(selectedNode);
		}
		return false;
	}

	public bool IsSelectedNodeRoot()
	{
		Node selectedNode = GetSelectedNode();
		Node node = _sceneTree?.GetEditedSceneRoot();
		if (selectedNode != null)
		{
			return selectedNode == node;
		}
		return false;
	}

	public void PopupAddNodeMenuAtMouse(Vector2? initialCanvasPosition = null)
	{
		_pendingAddNodeCanvasPosition = initialCanvasPosition;
		PopupAtMouse(_addNodeMenu);
	}

	public void ShowInstantiateDialog()
	{
		OnInstantiatePressed();
	}

	public void ShowCreateScriptDialog()
	{
		OnCreateScriptPressed();
	}

	public void DetachSelectedScript()
	{
		OnDetachScriptPressed();
	}

	public void ExtendSelectedScript()
	{
		OnExtendScriptPressed();
	}

	public void ShowRenameSelectedDialog()
	{
		ShowRenameDialog();
	}

	public Node DuplicateSelectedNode()
	{
		Node obj = _sceneTree?.DuplicateSelectedNode();
		if (obj != null)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance == null)
			{
				return obj;
			}
			XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
			if (xW2DSceneEditor == null)
			{
				return obj;
			}
			xW2DSceneEditor.QueueViewportRedraw();
		}
		return obj;
	}

	public bool MoveSelectedNode(int offset)
	{
		XWSceneNodeTree sceneTree = _sceneTree;
		int num;
		if (sceneTree == null)
		{
			num = 0;
		}
		else
		{
			num = (sceneTree.MoveSelectedNode(offset) ? 1 : 0);
			if (num != 0)
			{
				XWEditorInterface instance = XWEditorInterface.Instance;
				if (instance == null)
				{
					return (byte)num != 0;
				}
				XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
				if (xW2DSceneEditor == null)
				{
					return (byte)num != 0;
				}
				xW2DSceneEditor.QueueViewportRedraw();
			}
		}
		return (byte)num != 0;
	}

	public bool CanReparentNode(Node node, Node newParent, out string reason)
	{
		if (_sceneTree == null)
		{
			reason = "2D 场景树尚未初始化。";
			return false;
		}
		return _sceneTree.CanReparentNode(node, newParent, out reason);
	}

	public bool ReparentNodeWithHistory(Node node, Node newParent, string actionName = "拖拽重挂 2D 节点")
	{
		if (_sceneTree == null)
		{
			return false;
		}
		bool flag = _sceneTree.ReparentNodeWithHistory(node, newParent, actionName);
		XWEditorInterface.Instance?.ShowToast(_sceneTree.LastReparentFeedback, (!flag) ? 2 : 0);
		if (flag)
		{
			XWEditorInterface.Instance?.Get2DSceneEditor()?.QueueViewportRedraw();
		}
		return flag;
	}

	public int GetTrackedReparentNodeRecordCount(int historyId = -1)
	{
		return _sceneTree?.GetTrackedReparentNodeRecordCount(historyId) ?? 0;
	}

	public int GetLastReparentHistoryId()
	{
		return _sceneTree?.LastReparentHistoryId ?? (-1);
	}

	public bool DidLastReparentPreserveGlobalTransform()
	{
		return _sceneTree?.LastReparentPreservedGlobalTransform ?? false;
	}

	public string GetLastReparentFeedback()
	{
		return _sceneTree?.LastReparentFeedback ?? "";
	}

	public bool RemoveSelectedNode()
	{
		XWSceneNodeTree sceneTree = _sceneTree;
		int num;
		if (sceneTree == null)
		{
			num = 0;
		}
		else
		{
			num = (sceneTree.RemoveSelectedNode() ? 1 : 0);
			if (num != 0)
			{
				XWEditorInterface instance = XWEditorInterface.Instance;
				if (instance == null)
				{
					return (byte)num != 0;
				}
				XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
				if (xW2DSceneEditor == null)
				{
					return (byte)num != 0;
				}
				xW2DSceneEditor.QueueViewportRedraw();
			}
		}
		return (byte)num != 0;
	}

	public Node AddNodeToParentWithHistory(Node parent, Node node, string actionName)
	{
		Node obj = _sceneTree?.AddChildNodeToWithHistory(parent, node, actionName);
		if (obj != null)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance == null)
			{
				return obj;
			}
			XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
			if (xW2DSceneEditor == null)
			{
				return obj;
			}
			xW2DSceneEditor.QueueViewportRedraw();
		}
		return obj;
	}

	public bool RemoveNodeWithHistory(Node node, string actionName)
	{
		XWSceneNodeTree sceneTree = _sceneTree;
		int num;
		if (sceneTree == null)
		{
			num = 0;
		}
		else
		{
			num = (sceneTree.RemoveNodeWithHistory(node, actionName) ? 1 : 0);
			if (num != 0)
			{
				XWEditorInterface instance = XWEditorInterface.Instance;
				if (instance == null)
				{
					return (byte)num != 0;
				}
				XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
				if (xW2DSceneEditor == null)
				{
					return (byte)num != 0;
				}
				xW2DSceneEditor.QueueViewportRedraw();
			}
		}
		return (byte)num != 0;
	}

	public bool MakeSelectedNodeRoot()
	{
		XWSceneNodeTree sceneTree = _sceneTree;
		int num;
		if (sceneTree == null)
		{
			num = 0;
		}
		else
		{
			num = (sceneTree.MakeSelectedNodeRoot() ? 1 : 0);
			if (num != 0)
			{
				XWEditorInterface instance = XWEditorInterface.Instance;
				if (instance == null)
				{
					return (byte)num != 0;
				}
				XW2DSceneEditor xW2DSceneEditor = instance.Get2DSceneEditor();
				if (xW2DSceneEditor == null)
				{
					return (byte)num != 0;
				}
				xW2DSceneEditor.QueueViewportRedraw();
			}
		}
		return (byte)num != 0;
	}

	public void ShowSaveBranchDialogForSelected()
	{
		ShowSaveBranchDialog();
	}

	private static bool HasScript(Node node)
	{
		return node.GetScript().VariantType != Variant.Type.Nil;
	}

	private static void PopupAtMouse(PopupMenu popup)
	{
		popup.Position = DisplayServer.MouseGetPosition();
		popup.Popup();
	}

	private static void PopupBelow(Control control, PopupMenu popup)
	{
		Rect2 globalRect = control.GetGlobalRect();
		popup.Position = new Vector2I((int)globalRect.Position.X, (int)(globalRect.Position.Y + globalRect.Size.Y));
		popup.Popup();
	}

	public void SetScene(Node scene)
	{
		_pendingSceneRoot = scene;
		if (_sceneTree != null)
		{
			if (scene != null && GodotObject.IsInstanceValid(scene))
			{
				XWEditorInterface.Instance?.SetEditedSceneRoot(scene);
				_sceneTree.SetSceneRoot(scene);
				UpdateSceneActionButtons(scene);
			}
			else
			{
				XWEditorInterface.Instance?.SetEditedSceneRoot(null);
				_sceneTree.SetSceneRoot(null);
				UpdateSceneActionButtons(null);
				UpdateScriptButtons(null);
			}
		}
	}

	public void ApplySceneRootFromHistory(Node scene)
	{
		_pendingSceneRoot = scene;
		if (_sceneTree != null)
		{
			_sceneTree.SetSceneRootFromHistory(scene);
			UpdateSceneActionButtons(scene);
			UpdateScriptButtons(scene);
		}
	}

	public void RefreshScene()
	{
		_sceneTree?.Refresh();
	}

	public void SelectNode(Node node, bool emitSignal = true)
	{
		_sceneTree?.SelectNode(node, emitSignal);
	}

	public void PrepareForSceneHistoryBranchChange()
	{
		_sceneTree?.PrepareForSceneHistoryBranchChange();
	}

	public void ReleaseHistoryResources(int historyId)
	{
		_sceneTree?.ReleaseHistoryResources(historyId);
	}
}
