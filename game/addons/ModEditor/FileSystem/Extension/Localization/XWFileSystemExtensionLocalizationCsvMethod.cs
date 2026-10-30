using System.IO;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Localization.GUI;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionLocalizationCsvMethod : XWFileSystemExtensionMethod
{
	private const string LocalizationEditorWindowScenePath = "res://addons/ModEditor/Localization/GUI/XWLocalizationCsvEditorWindow.tscn";

	private static PackedScene _localizationEditorWindowScene;

	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWClassRegistry.Instance.GetUIIcon("Translation") ?? XWClassRegistry.Instance.GetUIIcon("File");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.Path))
		{
			return;
		}
		if (!ResourceLoader.Exists("res://addons/ModEditor/Localization/GUI/XWLocalizationCsvEditorWindow.tscn"))
		{
			XWEditorInterface.Instance?.ShowToast("多语言编辑窗口场景缺失");
			return;
		}
		if (_localizationEditorWindowScene == null)
		{
			_localizationEditorWindowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Localization/GUI/XWLocalizationCsvEditorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Window window = _localizationEditorWindowScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		XWLocalizationEditorPanel xWLocalizationEditorPanel = window?.GetNodeOrNull<XWLocalizationEditorPanel>("LocalizationEditorPanel");
		if (!GodotObject.IsInstanceValid(window) || !GodotObject.IsInstanceValid(xWLocalizationEditorPanel))
		{
			window?.QueueFree();
			XWEditorInterface.Instance?.ShowToast("无法加载多语言编辑窗口");
			return;
		}
		window.Title = "多语言 - " + Path.GetFileName(data.Path);
		window.CloseRequested += window.QueueFree;
		xWLocalizationEditorPanel.SetupCsvFile(data.Path);
		Node node = XWEditorInterface.Instance?.GetEditorPanel();
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			node = (Engine.GetMainLoop() as SceneTree)?.Root;
		}
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			window.QueueFree();
			XWEditorInterface.Instance?.ShowToast("无法打开多语言编辑窗口");
		}
		else
		{
			node.AddChild(window, forceReadableName: false, Node.InternalMode.Disabled);
			window.PopupCenteredClamped(new Vector2I(1100, 700), 0.9f);
			XWEditorInterface.Instance?.ShowToast("已打开多语言表: " + Path.GetFileName(data.Path));
		}
	}
}
