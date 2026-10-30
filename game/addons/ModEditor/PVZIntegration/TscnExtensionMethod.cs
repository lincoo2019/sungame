using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.SceneEditor;

namespace PVZHE.ModEditor.PVZIntegration;

public partial class TscnExtensionMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/PackedScene.svg");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (!FileAccess.FileExists(data.Path))
		{
			XWEditorInterface.Instance?.ShowToast("场景不存在: " + data.Path);
			return;
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(data.Path, null, ResourceLoader.CacheMode.Reuse);
		if (packedScene != null)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (xW2DSceneEditor == null)
			{
				XWEditorInterface.Instance?.ShowToast("2D 场景编辑器未就绪");
				return;
			}
			xW2DSceneEditor.LoadPackedScene(packedScene, data.Path);
			XWEditorInterface.Instance?.FocusPanel("2d_editor");
		}
	}
}
