using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.FileSystem;

public static class XWFileSystemExtensionRegistry
{
	private static bool _isInit;

	private static readonly Dictionary<string, XWFileSystemExtensionMethod> _extensionMethods = new Dictionary<string, XWFileSystemExtensionMethod>();

	private static readonly Dictionary<string, XWFileSystemExtensionMethod> _extensionResourceMethods = new Dictionary<string, XWFileSystemExtensionMethod>();

	private static readonly List<string> _showExtensions = new List<string> { "tres" };

	public static IReadOnlyList<string> ShowExtension => _showExtensions;

	public static void Init()
	{
		if (!_isInit)
		{
			RegisterInit();
			_isInit = true;
		}
	}

	private static void RegisterInit()
	{
		XWFileSystemExtensionImageMethod method = new XWFileSystemExtensionImageMethod();
		RegisterExtensionMethod("png", method);
		RegisterExtensionMethod("jpg", method);
		RegisterExtensionMethod("jpeg", method);
		RegisterExtensionMethod("svg", method);
		RegisterExtensionMethod("webp", method);
		RegisterExtensionMethod("bmp", method);
		RegisterExtensionMethod("tga", method);
		RegisterExtensionMethod("hdr", method);
		RegisterExtensionMethod("exr", method);
		RegisterExtensionMethod("dds", method);
		RegisterExtensionMethod("ktx", method);
		RegisterExtensionMethod("ktx2", method);
		XWFileSystemExtensionScriptMethod method2 = new XWFileSystemExtensionScriptMethod();
		RegisterExtensionMethod("cs", method2);
		RegisterExtensionMethod("csv", new XWFileSystemExtensionLocalizationCsvMethod());
		XWFileSystemExtensionTscnMethod method3 = new XWFileSystemExtensionTscnMethod();
		RegisterExtensionMethod("tscn", method3);
		RegisterExtensionMethod("scn", method3);
		XWFileSystemExtensionVisualResourceMethod method4 = new XWFileSystemExtensionVisualResourceMethod();
		RegisterExtensionMethod("tres", method4);
		RegisterExtensionMethod("res", method4);
		RegisterExtensionMethod("wav", method4);
		RegisterExtensionMethod("ogg", method4);
		RegisterExtensionMethod("mp3", method4);
		RegisterExtensionMethod("flac", method4);
		RegisterExtensionMethod("ttf", method4);
		RegisterExtensionMethod("otf", method4);
		RegisterExtensionMethod("woff", method4);
		RegisterExtensionMethod("woff2", method4);
		RegisterExtensionMethod("gdshader", method4);
		RegisterExtensionMethod("glb", method4);
		RegisterExtensionMethod("gltf", method4);
		RegisterExtensionMethod("obj", method4);
		RegisterExtensionMethod("fbx", method4);
		RegisterExtensionMethod("dae", method4);
		RegisterExtensionMethod("blend", method4);
		RegisterExtensionMethod("translation", method4);
		RegisterExtensionMethod("po", method4);
		RegisterExtensionMethod("ogv", method4);
		RegisterExtensionResourceMethod("XWBPScript", new XWFileSystemExtensionResourceXWBPScriptMethod());
	}

	public static void RegisterExtensionMethod(string extensionName, XWFileSystemExtensionMethod method)
	{
		if (method != null && !string.IsNullOrWhiteSpace(extensionName))
		{
			extensionName = extensionName.ToLower();
			_extensionMethods[extensionName] = method;
			if (!_showExtensions.Contains(extensionName))
			{
				_showExtensions.Add(extensionName);
			}
		}
	}

	public static void RegisterExtensionResourceMethod(string objectClassName, XWFileSystemExtensionMethod method)
	{
		if (method != null && !string.IsNullOrWhiteSpace(objectClassName))
		{
			_extensionResourceMethods[objectClassName] = method;
		}
	}

	public static XWFileSystemExtensionMethod GetMethod(XWFileSystemTreeItemData data)
	{
		if (data == null)
		{
			return null;
		}
		XWFileSystemExtensionMethod resourceMethod = GetResourceMethod(data);
		if (resourceMethod != null)
		{
			return resourceMethod;
		}
		string extension = data.GetExtension();
		if (!_extensionMethods.TryGetValue(extension, out var value))
		{
			return null;
		}
		return value;
	}

	private static XWFileSystemExtensionMethod GetResourceMethod(XWFileSystemTreeItemData data)
	{
		XWFileSystemExtensionMethod xWFileSystemExtensionMethod = FindResourceMethod(data.FileType.ToString());
		if (xWFileSystemExtensionMethod != null)
		{
			return xWFileSystemExtensionMethod;
		}
		if (data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is Resource resource)
		{
			xWFileSystemExtensionMethod = FindResourceMethod(GetResourceClassName(resource));
			if (xWFileSystemExtensionMethod != null)
			{
				return xWFileSystemExtensionMethod;
			}
		}
		return null;
	}

	private static XWFileSystemExtensionMethod FindResourceMethod(string className)
	{
		if (string.IsNullOrEmpty(className))
		{
			return null;
		}
		if (_extensionResourceMethods.TryGetValue(className, out var value))
		{
			return value;
		}
		foreach (string key in _extensionResourceMethods.Keys)
		{
			if (className == key || ClassDB.IsParentClass(className, key))
			{
				return _extensionResourceMethods[key];
			}
		}
		return null;
	}

	private static string GetResourceClassName(Resource resource)
	{
		if (resource == null)
		{
			return "";
		}
		string text = resource.GetClass();
		string name = resource.GetType().Name;
		if (string.IsNullOrEmpty(name) || !(name != text))
		{
			return text;
		}
		return name;
	}

	public static XWFileSystemExtensionMethod GetMethodByExtension(string extension)
	{
		if (string.IsNullOrWhiteSpace(extension))
		{
			return null;
		}
		extension = extension.ToLower();
		if (!_extensionMethods.TryGetValue(extension, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool GetCanShowExtension(string extension)
	{
		if (!string.IsNullOrWhiteSpace(extension))
		{
			return _showExtensions.Contains(extension.ToLower());
		}
		return false;
	}

	public static Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return GetMethod(data)?.GetIcon(data);
	}

	public static Texture2D GetIcon(XWFileSystemTreeItemData data, int targetSize)
	{
		return GetMethod(data)?.GetIcon(data, targetSize);
	}

	public static Texture2D GetIconByExtension(string extension)
	{
		XWFileSystemExtensionMethod methodByExtension = GetMethodByExtension(extension);
		if (methodByExtension != null)
		{
			XWFileSystemTreeItemData data = new XWFileSystemTreeItemData("dummy." + extension, default(Variant), pIsFolder: false);
			return methodByExtension.GetIcon(data);
		}
		return null;
	}

	public static void Execute(XWFileSystemTreeItemData data)
	{
		GetMethod(data)?.Execute(data);
	}
}
