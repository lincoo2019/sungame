using Godot;

namespace PVZHE.ModEditor.Core;

internal static class XWTextureSafety
{
	public static bool CanPreview(Texture2D texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return false;
		}
		return texture.GetClass() != "Texture2D";
	}

	public static Texture2D SafeIcon(Texture2D texture, Texture2D fallback = null)
	{
		if (CanPreview(texture))
		{
			return texture;
		}
		if (texture != fallback && CanPreview(fallback))
		{
			return fallback;
		}
		return null;
	}

	public static bool CanCreateResourceType(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return false;
		}
		if (typeName == "Texture2D")
		{
			return false;
		}
		if (typeName.StartsWith("Editor"))
		{
			return false;
		}
		return ClassDB.CanInstantiate(typeName);
	}
}
