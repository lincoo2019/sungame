using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Layout;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPEditorVariableTree : XWBPScriptItemTreeBase
{
	private List<XWBPVariableData> _variableDataList = new List<XWBPVariableData>();

	public void Init(IEnumerable<XWBPVariableData> variableDataList)
	{
		Clear();
		_variableDataList = new List<XWBPVariableData>(variableDataList);
		_root = CreateItem();
		foreach (XWBPVariableData variableData in _variableDataList)
		{
			TreeItem treeItem = _root.CreateChild();
			treeItem.SetText(0, variableData.Name);
			ApplyIcon(treeItem, variableData);
			treeItem.SetMetadata(0, variableData);
			variableData.Change += delegate
			{
				OnVariableChanged(treeItem, variableData);
			};
		}
		UpdateMinimumHeight();
	}

	private static void ApplyIcon(TreeItem treeItem, XWBPVariableData variableData)
	{
		if (treeItem != null && variableData != null)
		{
			Texture2D texture2D = null;
			if (variableData.Type == Variant.Type.Object && !string.IsNullOrWhiteSpace(variableData.ClassName))
			{
				texture2D = XWClassRegistry.Instance.GetClassIcon(variableData.ClassName);
			}
			if (texture2D == null && XWTypeRegistry.Instance.HasType(variableData.Type))
			{
				texture2D = XWTypeRegistry.Instance.GetTypeIcon(variableData.Type);
			}
			if (texture2D != null)
			{
				treeItem.SetIcon(0, texture2D);
			}
		}
	}

	private void OnVariableChanged(TreeItem treeItem, XWBPVariableData variableData)
	{
		if (GodotObject.IsInstanceValid(treeItem))
		{
			ApplyIcon(treeItem, variableData);
		}
	}

	protected override void CreateMenu()
	{
		base.CreateMenu();
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPVariableData target)
			{
				base.Editor?.EditBlueprintObject(target);
			}
			break;
		case 2L:
			PopMenu(mousePosition);
			break;
		}
	}

	protected override void OnItemEdited()
	{
		TreeItem edited = GetEdited();
		if (edited != null)
		{
			string text = edited.GetText(0);
			Variant metadata = edited.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPVariableData variable)
			{
				base.Editor?.RenameVariableWithUndo(variable, text);
			}
		}
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		return BuildDragDataForSelectedItem(atPosition, XWDragData.Type.BpVariable);
	}

	protected override void OnMenuIdPressed(long id)
	{
		switch (id)
		{
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

	private void Duplicate()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPVariableData variable)
			{
				base.Editor?.DuplicateVariableWithUndo(variable);
			}
		}
	}

	private void Rename()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPVariableData)
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPVariableData variable)
			{
				base.Editor?.RemoveVariableWithUndo(variable);
			}
		}
	}

	private IEnumerable<XWBPVariableData> GetAllVariables()
	{
		if (base.Editor?.BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPVariableData> variable in base.Editor.BpScriptData.Variables)
		{
			yield return variable.Value;
		}
	}
}
