using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionScriptMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		if ((data?.GetExtension() ?? "") == "cs")
		{
			return XWClassRegistry.Instance.GetUIIcon("Script");
		}
		return XWClassRegistry.Instance.GetUIIcon("File");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (data == null || string.IsNullOrEmpty(data.Path))
		{
			return;
		}
		if (!XWScriptEditor.IsSupportedScriptPath(data.Path))
		{
			if (!XWResourceEditorRegistry.TryOpenPath(data.Path))
			{
				XWEditorInterface.Instance?.ShowToast("脚本编辑器仅支持 C#；该文件没有可用的可视化编辑入口。", 2);
			}
			return;
		}
		XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
		if (xWScriptEditor == null || !GodotObject.IsInstanceValid(xWScriptEditor))
		{
			XWEditorInterface.Instance?.ShowToast("脚本编辑器未就绪");
			return;
		}
		xWScriptEditor.OpenFile(data.Path);
		XWEditorInterface.Instance?.FocusPanel("script_editor");
	}
}
