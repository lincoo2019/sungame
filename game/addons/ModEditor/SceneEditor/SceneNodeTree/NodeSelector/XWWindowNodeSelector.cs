using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XWWindowNodeSelector : XWWindowSelector
{
	[Signal]
	public delegate void NodeTypeSelectedEventHandler(string className);

	private const string ScenePath = "res://addons/ModEditor/SceneEditor/SceneNodeTree/NodeSelector/XWWindowNodeSelector.tscn";

	private const string MetaTypeId = "type_id";

	private const string MetaClassName = "class_name";

	private const string MetaScriptPath = "script_path";

	private const string MetaIsCustom = "is_custom";

	private static PackedScene _scene;

	private readonly System.Collections.Generic.Dictionary<string, XWClassData> _nodeClassData = new System.Collections.Generic.Dictionary<string, XWClassData>(StringComparer.Ordinal);

	private readonly HashSet<string> _creatableClasses = new HashSet<string>(StringComparer.Ordinal);

	public static XWWindowNodeSelector Create()
	{
		if (_scene == null)
		{
			_scene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/SceneEditor/SceneNodeTree/NodeSelector/XWWindowNodeSelector.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_scene))
		{
			return null;
		}
		return _scene.Instantiate<XWWindowNodeSelector>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		FavoritesFilePath = "user://xweditor_node_selector_favorites.cfg";
		RecentFilePath = "user://xweditor_node_selector_recent.cfg";
		base._Ready();
		base.Title = "创建节点";
		base.OkButtonText = "创建";
		Button okButton = GetOkButton();
		if (GodotObject.IsInstanceValid(okButton))
		{
			okButton.Disabled = true;
		}
		if (GodotObject.IsInstanceValid(_searchLineEdit))
		{
			_searchLineEdit.PlaceholderText = "搜索节点类型";
			_searchLineEdit.GrabFocus();
		}
	}

	protected override void OnCloseRequested()
	{
		QueueFree();
	}

	protected override void OnConfirmed()
	{
		Button okButton = GetOkButton();
		if (!GodotObject.IsInstanceValid(okButton) || !okButton.Disabled)
		{
			string selectedClassName = GetSelectedClassName();
			if (!string.IsNullOrEmpty(selectedClassName))
			{
				AddToRecent(new StringName(selectedClassName));
				EmitSignal(SignalName.NodeTypeSelected, selectedClassName);
				QueueFree();
			}
		}
	}

	protected override void BuildTree()
	{
		_tree.Clear();
		Root = _tree.CreateItem();
		AllCategories.Clear();
		CurrentResult.Clear();
		BuildNodeClassCache();
		BuildInheritanceTree();
	}

	protected override void OnItemSelected()
	{
		string selectedClassName = GetSelectedClassName();
		Button okButton = GetOkButton();
		if (GodotObject.IsInstanceValid(okButton))
		{
			okButton.Disabled = string.IsNullOrEmpty(selectedClassName);
		}
		if (string.IsNullOrEmpty(selectedClassName))
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
		}
		else
		{
			ShowNodeDescription(selectedClassName);
		}
	}

	protected override bool CanFilterTreeItem(string lowerQuery, TreeItem treeItem)
	{
		string text = treeItem.GetText(0);
		if (!string.IsNullOrEmpty(text) && text.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dict = metadata.As<Dictionary>();
		if (!DictionaryStringContains(dict, "class_name", lowerQuery))
		{
			return DictionaryStringContains(dict, "script_path", lowerQuery);
		}
		return true;
	}

	private void BuildNodeClassCache()
	{
		_nodeClassData.Clear();
		_creatableClasses.Clear();
		XWClassRegistry instance = XWClassRegistry.Instance;
		foreach (XWClassData allClassDatum in instance.GetAllClassData())
		{
			if (IsDisplayableNodeClass(instance, allClassDatum))
			{
				_nodeClassData[allClassDatum.ClassName] = allClassDatum;
				if (instance.CanInstantiate(allClassDatum.ClassName))
				{
					_creatableClasses.Add(allClassDatum.ClassName);
				}
			}
		}
	}

	private void BuildInheritanceTree()
	{
		if (!_nodeClassData.ContainsKey("Node"))
		{
			return;
		}
		HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { "Node" };
		TreeItem parentItem = CreateNodeItem(Root, "Node", collapsed: false);
		BuildClassChildren("Node", parentItem, visited);
		string[] array = _nodeClassData.Keys.Where((string item) => !visited.Contains(item)).OrderBy<string, string>((string result) => result, StringComparer.OrdinalIgnoreCase).ToArray();
		if (array.Length != 0)
		{
			TreeItem treeItem = _tree.CreateItem(Root);
			treeItem.Collapsed = true;
			treeItem.SetText(0, "其它");
			string[] array2 = array;
			foreach (string className in array2)
			{
				CreateNodeItem(treeItem, className);
			}
		}
	}

	private void BuildClassChildren(string parentClass, TreeItem parentItem, HashSet<string> visited)
	{
		string[] array = _nodeClassData.Keys.Where((string className) => className != parentClass && XWClassRegistry.Instance.GetParentClass(className) == parentClass).OrderBy<string, string>((string className) => className, StringComparer.OrdinalIgnoreCase).ToArray();
		foreach (string text in array)
		{
			if (visited.Add(text))
			{
				TreeItem parentItem2 = CreateNodeItem(parentItem, text);
				BuildClassChildren(text, parentItem2, visited);
			}
		}
	}

	private TreeItem CreateNodeItem(TreeItem parent, string className, bool collapsed = true)
	{
		if (!_nodeClassData.TryGetValue(className, out var value))
		{
			return null;
		}
		TreeItem treeItem = _tree.CreateItem(parent);
		treeItem.Collapsed = collapsed;
		treeItem.SetText(0, className);
		treeItem.SetTooltipText(0, BuildTooltip(value));
		if (_creatableClasses.Contains(className))
		{
			treeItem.SetMetadata(0, BuildNodeMetadata(value));
		}
		else
		{
			treeItem.SetCustomColor(0, new Color(0.62f, 0.66f, 0.72f));
		}
		Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(className);
		if (GodotObject.IsInstanceValid(classIcon))
		{
			treeItem.SetIcon(0, classIcon);
		}
		return treeItem;
	}

	private Dictionary BuildNodeMetadata(XWClassData data)
	{
		return new Dictionary
		{
			{
				"type_id",
				new StringName(data.ClassName)
			},
			{ "class_name", data.ClassName },
			{
				"script_path",
				data.ScriptPath ?? ""
			},
			{ "is_custom", data.IsGlobalClass }
		};
	}

	private string GetSelectedClassName()
	{
		TreeItem selected = _tree.GetSelected();
		if (!GodotObject.IsInstanceValid(selected))
		{
			return "";
		}
		Variant metadata = selected.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Dictionary)
		{
			return "";
		}
		Dictionary dictionary = metadata.As<Dictionary>();
		if (!dictionary.ContainsKey("class_name"))
		{
			return "";
		}
		return dictionary["class_name"].AsString();
	}

	private void ShowNodeDescription(string className)
	{
		if (_nodeClassData.TryGetValue(className, out var value))
		{
			string parentClass = XWClassRegistry.Instance.GetParentClass(className);
			_nameLabel.Text = className;
			_describeLabel.Text = "类型：" + (value.IsGlobalClass ? "自定义节点" : "内置节点") + "\n基类：" + (string.IsNullOrEmpty(parentClass) ? "Node" : parentClass);
			if (value.IsGlobalClass && !string.IsNullOrEmpty(value.ScriptPath))
			{
				RichTextLabel describeLabel = _describeLabel;
				describeLabel.Text = describeLabel.Text + "\n脚本：" + value.ScriptPath;
			}
		}
	}

	private static bool DictionaryStringContains(Dictionary dict, string key, string lowerQuery)
	{
		if (!dict.ContainsKey(key))
		{
			return false;
		}
		string text = dict[key].AsString();
		if (!string.IsNullOrEmpty(text))
		{
			return text.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsDisplayableNodeClass(XWClassRegistry registry, XWClassData data)
	{
		if (data == null || string.IsNullOrEmpty(data.ClassName))
		{
			return false;
		}
		string className = data.ClassName;
		if (!registry.IsClassInstanceOf(className, "Node"))
		{
			return false;
		}
		if (registry.IsClassInstanceOf(className, "Node3D"))
		{
			return false;
		}
		return !IsEditorOnlyClass(className);
	}

	private static bool IsEditorOnlyClass(string className)
	{
		if (!className.StartsWith("Editor", StringComparison.Ordinal) && !className.StartsWith("ScriptEditor", StringComparison.Ordinal))
		{
			return className.StartsWith("VisualShaderEditor", StringComparison.Ordinal);
		}
		return true;
	}

	private static string BuildTooltip(XWClassData data)
	{
		if (data.IsGlobalClass && !string.IsNullOrEmpty(data.ScriptPath))
		{
			return data.ClassName + "\n" + data.ScriptPath;
		}
		return data.ClassName;
	}
}
