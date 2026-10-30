using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Layout;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPEditorFunctionTree : XWBPScriptItemTreeBase
{
	private static Texture2D _memberMethodIcon;

	private static Texture2D MemberMethodIcon => _memberMethodIcon ?? (_memberMethodIcon = XWBPScriptItemTreeBase.LoadScriptItemIcon("MemberMethod.svg"));

	public void Init(IEnumerable<XWBPFunctionData> functionDataList)
	{
		Clear();
		_root = CreateItem();
		foreach (XWBPFunctionData functionData in functionDataList)
		{
			TreeItem treeItem = _root.CreateChild();
			treeItem.SetText(0, functionData.Name);
			treeItem.SetIcon(0, MemberMethodIcon);
			treeItem.SetMetadata(0, functionData);
		}
		UpdateMinimumHeight();
	}

	protected override void CreateMenu()
	{
		base.CreateMenu();
		AddScriptItemMenuItem("打开", 1, "Load.svg");
		AddScriptItemMenuItem("复制", 5, "ActionCopy.svg");
		AddScriptItemMenuItem("重命名", 10, "Rename.svg");
		AddScriptItemMenuItem("移除", 20, "Remove.svg");
	}

	protected override void OnItemMouseSelected(Vector2 mousePosition, long mouseButtonIndex)
	{
		TreeItem selected = GetSelected();
		if (selected == null)
		{
			return;
		}
		Variant metadata = selected.GetMetadata(0);
		switch (mouseButtonIndex)
		{
		case 1L:
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData target)
			{
				base.Editor?.EditBlueprintObject(target);
			}
			break;
		case 2L:
			PopMenu(mousePosition);
			break;
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData function)
			{
				base.Editor?.RenameFunctionWithUndo(function, text);
			}
		}
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		return BuildDragDataForSelectedItem(atPosition, XWDragData.Type.BpFunction);
	}

	protected override void OnMenuIdPressed(long id)
	{
		switch (id)
		{
		case 1L:
			Open();
			break;
		case 5L:
			Duplicate();
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
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData graphData)
			{
				base.Editor?.GetGraphEditor()?.Init(graphData);
			}
			DeselectAll();
		}
	}

	private void Duplicate()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData function)
			{
				base.Editor?.DuplicateFunctionWithUndo(function);
			}
		}
	}

	private void Rename()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData)
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPFunctionData function)
			{
				base.Editor?.RemoveFunctionWithUndo(function);
			}
		}
	}

	private IEnumerable<XWBPFunctionData> GetAllFunctions()
	{
		if (base.Editor?.BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPFunctionData> function in base.Editor.BpScriptData.Functions)
		{
			yield return function.Value;
		}
	}
}
