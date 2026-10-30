using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPEditorGraphTree : XWBPScriptItemTreeBase
{
	private const string MethodEntryTypeId = "__XWBPGraphNode_MethodEntry";

	private static Texture2D _filesystemIcon;

	private static Texture2D _memberMethodIcon;

	private static Texture2D FilesystemIcon => _filesystemIcon ?? (_filesystemIcon = XWBPScriptItemTreeBase.LoadScriptItemIcon("Filesystem.svg"));

	private static Texture2D MemberMethodIcon => _memberMethodIcon ?? (_memberMethodIcon = XWBPScriptItemTreeBase.LoadScriptItemIcon("MemberMethod.svg"));

	public void Init(IEnumerable<XWBPGraphData> graphDataList)
	{
		Clear();
		_root = CreateItem();
		foreach (XWBPGraphData graphData in graphDataList)
		{
			TreeItem treeItem = _root.CreateChild();
			treeItem.SetText(0, graphData.Name);
			treeItem.SetIcon(0, FilesystemIcon);
			treeItem.SetMetadata(0, graphData);
			BuildVirtualMethodItems(treeItem, graphData);
		}
		UpdateMinimumHeight();
	}

	private void BuildVirtualMethodItems(TreeItem parentItem, XWBPGraphData graphData)
	{
		foreach (KeyValuePair<int, XWBPNodeData> node in graphData.Nodes)
		{
			XWBPNodeData value = node.Value;
			if (!(value.TypeId.ToString() != "__XWBPGraphNode_MethodEntry") && (int)value.GetMetaData("MethodType").AsInt64() == 2)
			{
				string text = value.GetMetaData("MethodData").AsGodotDictionary().GetValueOrDefault("name", Variant.From<string>(""))
					.AsString();
				TreeItem treeItem = parentItem.CreateChild();
				treeItem.SetText(0, text);
				treeItem.SetIcon(0, MemberMethodIcon);
				treeItem.SetMetadata(0, new Dictionary
				{
					{ "graphData", graphData },
					{ "nodeId", value.Id }
				});
			}
		}
	}

	public void RefreshVirtualMethods(XWBPGraphData graphData)
	{
		if (_root == null)
		{
			return;
		}
		for (int i = 0; i < _root.GetChildCount(); i++)
		{
			TreeItem child = _root.GetChild(i);
			Variant metadata = child.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() == graphData)
			{
				while (child.GetChildCount() > 0)
				{
					child.GetChild(0).Free();
				}
				BuildVirtualMethodItems(child, graphData);
				break;
			}
		}
	}

	protected override void CreateMenu()
	{
		base.CreateMenu();
		AddScriptItemMenuItem("打开", 1, "Load.svg");
		AddScriptItemMenuItem("重命名", 10, "Rename.svg");
		AddScriptItemMenuItem("移除", 20, "Remove.svg");
	}

	protected override void OnItemMouseSelected(Vector2 mousePosition, long mouseButtonIndex)
	{
		TreeItem selected = GetSelected();
		if (mouseButtonIndex == 1 && selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			GodotObject godotObject = null;
			if (metadata.VariantType == Variant.Type.Object)
			{
				godotObject = metadata.As<GodotObject>();
			}
			else if (metadata.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = metadata.AsGodotDictionary();
				XWBPGraphData xWBPGraphData = dictionary.GetValueOrDefault("graphData").As<XWBPGraphData>();
				int nodeId = dictionary.GetValueOrDefault("nodeId", -1).AsInt32();
				godotObject = (GodotObject)(((object)xWBPGraphData?.GetNode(nodeId)) ?? ((object)xWBPGraphData));
			}
			if (GodotObject.IsInstanceValid(godotObject))
			{
				base.Editor?.EditBlueprintObject(godotObject);
			}
		}
		else if (mouseButtonIndex == 2)
		{
			selected = SelectItemAtPosition(mousePosition);
			if (selected != null)
			{
				bool disabled = selected.GetMetadata(0).VariantType == Variant.Type.Dictionary;
				int itemIndex = _menu.GetItemIndex(20);
				_menu.SetItemDisabled(itemIndex, disabled);
				PopupMenuAtPosition(mousePosition);
			}
		}
	}

	protected override void OnItemActivated()
	{
		Open();
	}

	protected override void OnItemEdited()
	{
		TreeItem edited = GetEdited();
		if (edited != null)
		{
			string text = edited.GetText(0);
			Variant metadata = edited.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPGraphData graph)
			{
				base.Editor?.RenameGraphWithUndo(graph, text);
			}
		}
	}

	protected override void OnMenuIdPressed(long id)
	{
		switch (id)
		{
		case 1L:
			Open();
			break;
		case 10L:
			Rename();
			break;
		case 20L:
			Remove();
			break;
		}
	}

	private void Open()
	{
		TreeItem selected = GetSelected();
		if (selected == null)
		{
			return;
		}
		Variant metadata = selected.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPGraphData graphData)
		{
			base.Editor?.GetGraphEditor()?.Init(graphData);
		}
		else if (metadata.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = metadata.AsGodotDictionary();
			XWBPGraphData xWBPGraphData = dictionary["graphData"].As<XWBPGraphData>();
			int num = (int)dictionary["nodeId"].AsInt64();
			if (xWBPGraphData != null && num >= 0)
			{
				XWBPGraphEdit obj = base.Editor?.GetGraphEditor();
				obj?.Init(xWBPGraphData);
				obj?.FocusNode(num);
			}
		}
		DeselectAll();
	}

	private void Rename()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPGraphData)
			{
				EditSelected(forceEdit: true);
			}
		}
	}

	private void Remove()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPGraphData graph)
			{
				base.Editor?.RemoveGraphWithUndo(graph);
			}
		}
	}
}
