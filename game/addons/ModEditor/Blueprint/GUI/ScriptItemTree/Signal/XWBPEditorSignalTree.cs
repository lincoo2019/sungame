using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Layout;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPEditorSignalTree : XWBPScriptItemTreeBase
{
	private static Texture2D _signalIcon;

	private static Texture2D SignalIcon => _signalIcon ?? (_signalIcon = XWBPScriptItemTreeBase.LoadScriptItemIcon("ClassIcon/Signal.svg"));

	public void Init(IEnumerable<XWBPSignalData> signalDataList)
	{
		Clear();
		_root = CreateItem();
		foreach (XWBPSignalData signalData in signalDataList)
		{
			TreeItem treeItem = _root.CreateChild();
			treeItem.SetText(0, signalData.Name);
			treeItem.SetIcon(0, SignalIcon);
			treeItem.SetMetadata(0, signalData);
			signalData.Change += delegate
			{
				OnSignalChanged(treeItem);
			};
		}
		UpdateMinimumHeight();
	}

	private static void OnSignalChanged(TreeItem treeItem)
	{
		GodotObject.IsInstanceValid(treeItem);
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPSignalData target)
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPSignalData signalData)
			{
				base.Editor?.RenameSignalWithUndo(signalData, text);
			}
		}
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		return BuildDragDataForSelectedItem(atPosition, XWDragData.Type.BpSignal);
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPSignalData signalData)
			{
				base.Editor?.DuplicateSignalWithUndo(signalData);
			}
		}
	}

	private void Rename()
	{
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPSignalData)
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
			if (metadata.VariantType == Variant.Type.Object && metadata.As<RefCounted>() is XWBPSignalData signalData)
			{
				base.Editor?.RemoveSignalWithUndo(signalData);
			}
		}
	}

	private IEnumerable<XWBPSignalData> GetAllSignals()
	{
		if (base.Editor?.BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPSignalData> signalData in base.Editor.BpScriptData.SignalDatas)
		{
			yield return signalData.Value;
		}
	}
}
