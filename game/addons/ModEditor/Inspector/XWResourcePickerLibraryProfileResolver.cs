using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors;

namespace PVZHE.ModEditor.Inspector;

internal static class XWResourcePickerLibraryProfileResolver
{
	public static XWResourcePickerLibraryProfile Resolve(string baseType)
	{
		baseType = (string.IsNullOrWhiteSpace(baseType) ? "Resource" : baseType.Trim());
		if (IsTypeOrDerived(baseType, "Texture2D"))
		{
			return Create("Texture", "图片资源", baseType, new string[4] { "Asset/Texture", "Asset/Icon", "Asset/Theme", "Asset/Anime" }, "res://addons/ModEditor/Icons/ClassIcon/Texture2D.svg");
		}
		if (IsTypeOrDerived(baseType, "PackedScene"))
		{
			return Create("Scene", "场景资源", baseType, new string[2] { "Prefab", "Asset/Anime" }, "res://addons/ModEditor/Icons/ClassIcon/PackedScene.svg");
		}
		if (IsTypeOrDerived(baseType, "AudioStream"))
		{
			return Create("Audio", "音频资源", baseType, new string[1] { "Asset/Audio" }, "res://addons/ModEditor/Icons/ClassIcon/AudioStream.svg");
		}
		if (IsTypeOrDerived(baseType, "Font"))
		{
			return Create("Font", "字体资源", baseType, new string[1] { "Asset/Font" }, "res://addons/ModEditor/Icons/ClassIcon/Font.svg");
		}
		if (IsTypeOrDerived(baseType, "Shader"))
		{
			return Create("Shader", "着色器资源", baseType, new string[1] { "Asset/Shader" }, "res://addons/ModEditor/Icons/ClassIcon/Shader.svg");
		}
		XWVisualEditorDescriptor xWVisualEditorDescriptor = FindDescriptor(baseType);
		if (xWVisualEditorDescriptor != null)
		{
			string text = xWVisualEditorDescriptor.DisplayName?.Replace("编辑器", "").Trim();
			return Create(xWVisualEditorDescriptor.Category, string.IsNullOrWhiteSpace(text) ? (baseType + " 资源") : text, baseType, GetBuiltInRoots(xWVisualEditorDescriptor.PathMarkers), xWVisualEditorDescriptor.IconPath);
		}
		return Create("General", (baseType == "Resource") ? "通用资源" : (baseType + " 资源"), baseType, new string[2] { "Asset/Config", "Resource" }, "res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg");
	}

	private static XWVisualEditorDescriptor FindDescriptor(string baseType)
	{
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			if (!(allEditor.Category == "General") && allEditor.ResourceClassNames.Any((string className) => string.Equals(className, baseType, StringComparison.Ordinal) || IsTypeOrDerived(baseType, className)))
			{
				return allEditor;
			}
		}
		return null;
	}

	private static IReadOnlyList<string> GetBuiltInRoots(IEnumerable<string> markers)
	{
		return (from marker in markers ?? Array.Empty<string>()
			select (marker ?? "").Replace('\\', '/').Trim('/') into marker
			where marker.StartsWith("Asset/", StringComparison.OrdinalIgnoreCase) || marker.StartsWith("Resource/", StringComparison.OrdinalIgnoreCase) || marker.StartsWith("Prefab/", StringComparison.OrdinalIgnoreCase) || marker.StartsWith("Registry/", StringComparison.OrdinalIgnoreCase)
			select marker).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private static XWResourcePickerLibraryProfile Create(string category, string displayName, string className, IReadOnlyList<string> builtInPathMarkers, string iconPath)
	{
		return new XWResourcePickerLibraryProfile(category, displayName, new string[1] { className }, builtInPathMarkers ?? Array.Empty<string>(), iconPath ?? "");
	}

	private static bool IsTypeOrDerived(string className, string baseClassName)
	{
		if (string.Equals(className, baseClassName, StringComparison.Ordinal))
		{
			return true;
		}
		if (ClassDB.ClassExists(className) && ClassDB.ClassExists(baseClassName) && ClassDB.IsParentClass(className, baseClassName))
		{
			return true;
		}
		return XWClassRegistry.Instance.IsClassInstanceOf(className, baseClassName);
	}
}
