using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionResourceXWBPScriptMethod : XWFileSystemExtensionResourceMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWClassRegistry.Instance.GetUIIcon("GraphEdit");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		XWBPScript xWBPScript = LoadBlueprint(data);
		if (xWBPScript == null || !GodotObject.IsInstanceValid(xWBPScript))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载蓝图: " + data?.Path);
		}
		else if (XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor)
		{
			xWBPEditor.Init(xWBPScript);
			XWEditorInterface.Instance?.FocusPanel("bp_editor");
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast("蓝图编辑器未就绪");
		}
	}

	private static XWBPScript LoadBlueprint(XWFileSystemTreeItemData data)
	{
		if (data == null)
		{
			return null;
		}
		if (data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is XWBPScript result)
		{
			return result;
		}
		if (!string.IsNullOrEmpty(data.Path) && ResourceLoader.Exists(data.Path))
		{
			return ResourceLoader.Load<XWBPScript>(data.Path, null, ResourceLoader.CacheMode.Reuse);
		}
		return null;
	}
}
