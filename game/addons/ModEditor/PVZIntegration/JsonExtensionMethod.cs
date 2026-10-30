using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.PVZIntegration;

public partial class JsonExtensionMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		if (data?.GetExtension() == "cs")
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Script.svg");
		}
		return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/TextFile.svg");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (data != null)
		{
			if (ShouldOpenInScriptEditorFirst(data))
			{
				OpenInScriptEditor(data);
			}
			else if (!XWResourceEditorRegistry.TryOpenPath(data.Path))
			{
				XWEditorInterface.Instance?.ShowToast("脚本编辑器仅支持 C#；请使用该资源对应的可视化配置面板。", 2);
			}
		}
	}

	private static bool ShouldOpenInScriptEditorFirst(XWFileSystemTreeItemData data)
	{
		return (data?.GetExtension() ?? "") == "cs";
	}

	private static void OpenInScriptEditor(XWFileSystemTreeItemData data)
	{
		XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
		if (xWScriptEditor == null)
		{
			XWEditorInterface.Instance?.ShowToast("脚本编辑器未就绪");
			return;
		}
		xWScriptEditor.LoadScript(data?.Path ?? "");
		XWEditorInterface.Instance?.FocusPanel("script_editor");
	}
}
