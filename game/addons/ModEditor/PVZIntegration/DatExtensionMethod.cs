using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;

namespace PVZHE.ModEditor.PVZIntegration;

public partial class DatExtensionMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		OpenDatAnimationResource(data);
	}

	private static void OpenDatAnimationResource(XWFileSystemTreeItemData data)
	{
		string text = data?.Path?.Replace('\\', '/') ?? "";
		if (string.IsNullOrWhiteSpace(text) || !FileAccess.FileExists(text))
		{
			XWEditorInterface.Instance?.ShowToast("DAT 文件不存在");
			return;
		}
		string text2 = FindSameBaseAnimationResourcePath(text);
		if (string.IsNullOrWhiteSpace(text2) || !ResourceLoader.Exists(text2) || !TresExtensionMethod.TryOpenAdobeAnimateResourceWithoutLoading(text2))
		{
			AdobeAnimateData adobeAnimateData = new AdobeAnimateData
			{
				ResourceName = text.GetFile().GetBaseName()
			};
			adobeAnimateData.SetAnimeFileForModImport(text.GetFile());
			adobeAnimateData.SetEditorResolvedAnimeFilePath(text);
			XWEditorInterface.Instance?.EditResource(adobeAnimateData, XWResourceEditContext.ForRoot(adobeAnimateData, text, "animation_editor"));
		}
	}

	private static string FindSameBaseAnimationResourcePath(string datPath)
	{
		if (string.IsNullOrWhiteSpace(datPath))
		{
			return "";
		}
		string text = datPath.GetBaseDir().PathJoin(datPath.GetFile().GetBaseName() + ".tres");
		if (!FileAccess.FileExists(text))
		{
			return "";
		}
		return text;
	}
}
