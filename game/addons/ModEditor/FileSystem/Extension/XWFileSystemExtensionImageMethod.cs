using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionImageMethod : XWFileSystemExtensionMethod
{
	private const int DefaultIconSize = 16;

	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return GetIcon(data, 16);
	}

	public override Texture2D GetIcon(XWFileSystemTreeItemData data, int targetSize)
	{
		Texture2D texture2D = LoadTexture(data);
		if (!XWTextureSafety.CanPreview(texture2D))
		{
			return XWClassRegistry.Instance.GetUIIcon("Filesystem");
		}
		Image image = texture2D.GetImage();
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return XWClassRegistry.Instance.GetUIIcon("Filesystem");
		}
		return CreateThumbnail(image, targetSize);
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		Texture2D texture2D = LoadTexture(data);
		if (XWTextureSafety.CanPreview(texture2D))
		{
			XWEditorInterface.Instance?.EditResource(texture2D, XWResourceEditContext.ForRoot(texture2D, data?.Path ?? texture2D.ResourcePath, "resource_editor"));
		}
	}

	private static Texture2D LoadTexture(XWFileSystemTreeItemData data)
	{
		if (data == null)
		{
			return null;
		}
		if (data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is Texture2D result)
		{
			return result;
		}
		if (!string.IsNullOrEmpty(data.Path) && ResourceLoader.Exists(data.Path))
		{
			return ResourceLoader.Load<Texture2D>(data.Path, null, ResourceLoader.CacheMode.Reuse);
		}
		if (!string.IsNullOrEmpty(data.Path) && FileAccess.FileExists(GetLoadablePath(data.Path)))
		{
			return LoadTextureFromFile(data.Path);
		}
		return null;
	}

	private static Texture2D LoadTextureFromFile(string path)
	{
		string loadablePath = GetLoadablePath(path);
		Image image = new Image();
		if (((path.GetExtension().ToLowerInvariant() == "svg") ? LoadSvgImage(loadablePath, image) : image.Load(loadablePath)) != Error.Ok || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return null;
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static Error LoadSvgImage(string path, Image image)
	{
		byte[] fileAsBytes = FileAccess.GetFileAsBytes(path);
		if (fileAsBytes.Length != 0)
		{
			return image.LoadSvgFromBuffer(fileAsBytes);
		}
		return Error.FileCantRead;
	}

	private static Texture2D CreateThumbnail(Image image, int targetSize)
	{
		targetSize = Mathf.Max(1, targetSize);
		float num = Mathf.Min((float)targetSize / (float)image.GetWidth(), (float)targetSize / (float)image.GetHeight());
		int width = Mathf.Max(1, Mathf.FloorToInt((float)image.GetWidth() * num));
		int height = Mathf.Max(1, Mathf.FloorToInt((float)image.GetHeight() * num));
		image.Resize(width, height, Image.Interpolation.Bilinear);
		return ImageTexture.CreateFromImage(image);
	}

	private static string GetLoadablePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://") || path.StartsWith("user://"))
		{
			return ProjectSettings.GlobalizePath(path);
		}
		return path.Replace('\\', '/');
	}
}
