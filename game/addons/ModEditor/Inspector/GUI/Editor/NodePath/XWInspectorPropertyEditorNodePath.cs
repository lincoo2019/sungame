using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorNodePath : XWInspectorPropertyEditorBase
{
	private const string EditorScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/NodePath/XWInspectorPropertyEditorNodePath.tscn";

	private const string NodePathSelectorWindowScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/NodePath/XWNodePathSelectorWindow.tscn";

	private static readonly Texture2D SelectNodeIcon = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/CopyNodePath.svg", null, ResourceLoader.CacheMode.Reuse));

	private static PackedScene _nodePathSelectorWindowScene;

	private static PackedScene _editorScene;

	private LineEdit _lineEdit;

	private Button _selectButton;

	private Window _nodePathSelectorWindow;

	private LineEdit _nodePathFilter;

	private Tree _nodePathTree;

	private Label _nodePathPreviewLabel;

	private Button _confirmButton;

	private Button _clearButton;

	private Button _cancelButton;

	private Node _selectorRoot;

	private readonly System.Collections.Generic.Dictionary<TreeItem, Node> _treeItemToNode = new System.Collections.Generic.Dictionary<TreeItem, Node>();

	public static XWInspectorPropertyEditorNodePath Create()
	{
		if (_editorScene == null)
		{
			_editorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/NodePath/XWInspectorPropertyEditorNodePath.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		return _editorScene?.Instantiate<XWInspectorPropertyEditorNodePath>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		_selectButton = GetNode<Button>("%SelectNodePathButton");
		_lineEdit.TextChanged += OnTextChanged;
		_lineEdit.TextSubmitted += CommitTextNodePath;
		_lineEdit.FocusExited += delegate
		{
			CommitTextNodePath(_lineEdit.Text);
		};
		_selectButton.Pressed += ShowNodeSelectorWindow;
		_selectButton.Icon = SelectNodeIcon;
		_selectButton.TooltipText = "选择节点";
		_selectButton.CustomMinimumSize = new Vector2(30f, 28f);
		_selectButton.Flat = true;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_lineEdit, propertyValue.AsString());
		}
	}

	public override Variant GetValue()
	{
		return new NodePath(_lineEdit.Text);
	}

	private void OnTextChanged(string text)
	{
		ValueChange(Variant.From<NodePath>(new NodePath(text)));
	}

	private void CommitTextNodePath(string text)
	{
		_lineEdit.Text = text ?? "";
		ValueChange(Variant.From<NodePath>(new NodePath(_lineEdit.Text)));
	}

	private void ShowNodeSelectorWindow()
	{
		if (EnsureNodeSelectorWindow())
		{
			_selectorRoot = ResolveNodeSelectorRoot();
			if (!IsNodeValid(_selectorRoot))
			{
				XWEditorInterface.Instance?.ShowToast("当前没有可选择的场景节点");
				return;
			}
			_nodePathFilter.Text = "";
			BuildNodeSelectorTree();
			_nodePathSelectorWindow.PopupCentered();
			_nodePathFilter.GrabFocus();
		}
	}

	private bool EnsureNodeSelectorWindow()
	{
		if (GodotObject.IsInstanceValid(_nodePathSelectorWindow))
		{
			return true;
		}
		if (_nodePathSelectorWindowScene == null)
		{
			_nodePathSelectorWindowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/NodePath/XWNodePathSelectorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_nodePathSelectorWindow = _nodePathSelectorWindowScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_nodePathSelectorWindow))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载节点路径选择窗口");
			return false;
		}
		_nodePathFilter = _nodePathSelectorWindow.GetNode<LineEdit>("%NodePathSelectorFilter");
		_nodePathTree = _nodePathSelectorWindow.GetNode<Tree>("%NodePathSelectorTree");
		_nodePathPreviewLabel = _nodePathSelectorWindow.GetNode<Label>("%NodePathPreviewLabel");
		_clearButton = _nodePathSelectorWindow.GetNode<Button>("%ClearNodePath");
		_cancelButton = _nodePathSelectorWindow.GetNode<Button>("%CancelNodePathSelection");
		_confirmButton = _nodePathSelectorWindow.GetNode<Button>("%ConfirmNodePathSelection");
		_nodePathSelectorWindow.CloseRequested += delegate
		{
			_nodePathSelectorWindow.Hide();
		};
		_nodePathFilter.TextChanged += FilterNodePathTree;
		_nodePathTree.ItemSelected += UpdateSelectedNodePathPreview;
		_nodePathTree.ItemActivated += CommitSelectedNodePath;
		_clearButton.Pressed += ClearNodePath;
		_cancelButton.Pressed += delegate
		{
			_nodePathSelectorWindow.Hide();
		};
		_confirmButton.Pressed += CommitSelectedNodePath;
		AddChild(_nodePathSelectorWindow, forceReadableName: false, InternalMode.Disabled);
		return true;
	}

	private void BuildNodeSelectorTree()
	{
		if (GodotObject.IsInstanceValid(_nodePathTree))
		{
			_nodePathTree.Clear();
			_treeItemToNode.Clear();
			if (IsNodeValid(_selectorRoot))
			{
				TreeItem treeItem = _nodePathTree.CreateItem();
				AppendNodeTreeItem(treeItem, _selectorRoot);
				treeItem.Collapsed = false;
				treeItem.Select(0);
				UpdateSelectedNodePathPreview();
			}
		}
	}

	private void AppendNodeTreeItem(TreeItem item, Node node)
	{
		if (!GodotObject.IsInstanceValid(item) || !IsNodeValid(node))
		{
			return;
		}
		item.SetText(0, node.Name);
		item.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(node.GetClass()));
		item.SetTooltipText(0, node.GetPath().ToString());
		item.SetSelectable(0, selectable: true);
		_treeItemToNode[item] = node;
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				TreeItem item2 = item.CreateChild();
				AppendNodeTreeItem(item2, node2);
			}
		}
	}

	private void FilterNodePathTree(string query)
	{
		if (!GodotObject.IsInstanceValid(_nodePathTree))
		{
			return;
		}
		TreeItem root = _nodePathTree.GetRoot();
		if (GodotObject.IsInstanceValid(root))
		{
			string text = (query ?? "").Trim().ToLowerInvariant();
			SetVisibleRecursive(root, text);
			root.Visible = true;
			if (string.IsNullOrEmpty(text))
			{
				root.Collapsed = false;
			}
		}
	}

	private bool SetVisibleRecursive(TreeItem item, string query)
	{
		if (!GodotObject.IsInstanceValid(item))
		{
			return false;
		}
		bool flag = string.IsNullOrEmpty(query) || MatchesFilter(item, query);
		bool flag2 = false;
		TreeItem treeItem = item.GetFirstChild();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			flag2 |= SetVisibleRecursive(treeItem, query);
			treeItem = treeItem.GetNext();
		}
		bool result = (item.Visible = flag || flag2);
		if (!string.IsNullOrEmpty(query) && flag2)
		{
			item.Collapsed = false;
		}
		return result;
	}

	private bool MatchesFilter(TreeItem item, string query)
	{
		if (!GodotObject.IsInstanceValid(item) || string.IsNullOrEmpty(query))
		{
			return true;
		}
		if ((item.GetText(0) ?? "").ToLowerInvariant().Contains(query))
		{
			return true;
		}
		if (_treeItemToNode.TryGetValue(item, out var value) && IsNodeValid(value))
		{
			return value.GetPath().ToString().ToLowerInvariant()
				.Contains(query);
		}
		return false;
	}

	private void UpdateSelectedNodePathPreview()
	{
		if (GodotObject.IsInstanceValid(_nodePathPreviewLabel) && GodotObject.IsInstanceValid(_nodePathTree))
		{
			TreeItem selected = _nodePathTree.GetSelected();
			if (!GodotObject.IsInstanceValid(selected) || !_treeItemToNode.TryGetValue(selected, out var value) || !IsNodeValid(value))
			{
				_nodePathPreviewLabel.Text = "";
			}
			else
			{
				_nodePathPreviewLabel.Text = BuildRelativeNodePath(value);
			}
		}
	}

	private void CommitSelectedNodePath()
	{
		if (GodotObject.IsInstanceValid(_nodePathTree))
		{
			TreeItem selected = _nodePathTree.GetSelected();
			if (GodotObject.IsInstanceValid(selected) && _treeItemToNode.TryGetValue(selected, out var value) && IsNodeValid(value))
			{
				string text = BuildRelativeNodePath(value);
				_lineEdit.Text = text;
				ValueChange(Variant.From<NodePath>(new NodePath(text)));
				XWEditorInterface.Instance?.SetSelectedNode(value);
				_nodePathSelectorWindow?.Hide();
			}
		}
	}

	private void ClearNodePath()
	{
		_lineEdit.Text = "";
		ValueChange(Variant.From<NodePath>(new NodePath("")));
		_nodePathSelectorWindow?.Hide();
	}

	private string BuildRelativeNodePath(Node target)
	{
		if (!IsNodeValid(target))
		{
			return "";
		}
		Node node = ResolvePathBaseNode();
		if (IsNodeValid(node) && IsNodeInSameTree(node, target))
		{
			if (node == target)
			{
				return ".";
			}
			return node.GetPathTo(target).ToString();
		}
		if (IsNodeValid(_selectorRoot) && IsNodeInSameTree(_selectorRoot, target))
		{
			if (_selectorRoot == target)
			{
				return ".";
			}
			return _selectorRoot.GetPathTo(target).ToString();
		}
		return target.GetPath().ToString();
	}

	private Node ResolvePathBaseNode()
	{
		if (GodotObject.IsInstanceValid(Property) && Property.Object is Node node && IsNodeValid(node))
		{
			return node;
		}
		Array<Node> array = XWEditorInterface.Instance?.GetSelectedNodes();
		if (array != null && array.Count > 0)
		{
			Node node2 = array[0];
			if (IsNodeValid(node2))
			{
				return node2;
			}
		}
		return XWEditorInterface.Instance?.GetEditedSceneRoot();
	}

	private Node ResolveNodeSelectorRoot()
	{
		Node node = XWEditorInterface.Instance?.GetEditedSceneRoot();
		if (IsNodeValid(node))
		{
			return node;
		}
		Node node2 = ResolvePathBaseNode();
		if (!IsNodeValid(node2))
		{
			return null;
		}
		while (IsNodeValid(node2.GetParent()))
		{
			node2 = node2.GetParent();
		}
		return node2;
	}

	private static bool IsNodeInSameTree(Node a, Node b)
	{
		if (!IsNodeValid(a) || !IsNodeValid(b))
		{
			return false;
		}
		return a.GetTree() == b.GetTree();
	}

	private static bool IsNodeValid(Node node)
	{
		return GodotObject.IsInstanceValid(node);
	}
}
