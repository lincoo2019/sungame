using System.IO;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionVisualResourceMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		if (data != null && data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is Resource resource)
		{
			return XWClassRegistry.Instance.GetClassIcon(resource.GetClass());
		}
		string text;
		switch (data?.GetExtension() ?? "")
		{
		case "wav":
		case "ogg":
		case "mp3":
		case "flac":
			text = "AudioStream";
			break;
		case "ttf":
		case "otf":
		case "woff":
		case "woff2":
			text = "Font";
			break;
		case "gdshader":
			text = "Shader";
			break;
		case "glb":
		case "fbx":
		case "dae":
		case "gltf":
		case "blend":
			text = "PackedScene";
			break;
		case "obj":
			text = "Mesh";
			break;
		case "translation":
		case "po":
			text = "Translation";
			break;
		case "ogv":
			text = "VideoStream";
			break;
		default:
			text = "Resource";
			break;
		}
		string className = text;
		return XWClassRegistry.Instance.GetClassIcon(className);
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (data != null && !string.IsNullOrWhiteSpace(data.Path))
		{
			Resource resource = ((data.Data.VariantType == Variant.Type.Object) ? (data.Data.As<GodotObject>() as Resource) : null);
			if (!GodotObject.IsInstanceValid(resource) && ResourceLoader.Exists(data.Path))
			{
				resource = ResourceLoader.Load<Resource>(data.Path, null, ResourceLoader.CacheMode.Reuse);
			}
			if (!GodotObject.IsInstanceValid(resource))
			{
				resource = LoadExternalAudio(data.Path);
			}
			if (!GodotObject.IsInstanceValid(resource))
			{
				XWEditorInterface.Instance?.ShowToast("无法加载资源: " + data.Path);
			}
			else
			{
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, data.Path, "resource_editor"));
			}
		}
	}

	private static Resource LoadExternalAudio(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return null;
		}
		if (!XWModExternalMediaLoader.TryLoadAudio(path, Path.GetExtension(path), out var audio, out var _))
		{
			return null;
		}
		return audio;
	}
}
