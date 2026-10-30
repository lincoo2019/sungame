using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.SceneEditor;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionTscnMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWClassRegistry.Instance.GetClassIcon("PackedScene");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		PackedScene packedScene = LoadPackedScene(data);
		if (packedScene == null || !GodotObject.IsInstanceValid(packedScene))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载场景: " + data?.Path);
			return;
		}
		XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
		if (xW2DSceneEditor == null || !GodotObject.IsInstanceValid(xW2DSceneEditor))
		{
			XWEditorInterface.Instance?.ShowToast("2D 场景编辑器未就绪");
			return;
		}
		xW2DSceneEditor.LoadPackedScene(packedScene, data.Path);
		XWEditorInterface.Instance?.FocusPanel("2d_editor");
	}

	private static PackedScene LoadPackedScene(XWFileSystemTreeItemData data)
	{
		if (data == null)
		{
			return null;
		}
		if (data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is PackedScene result)
		{
			return result;
		}
		if (!string.IsNullOrEmpty(data.Path) && FileAccess.FileExists(data.Path))
		{
			return ResourceLoader.Load<PackedScene>(data.Path, null, ResourceLoader.CacheMode.Reuse);
		}
		return null;
	}
}
