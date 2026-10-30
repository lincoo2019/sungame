using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionMethod : RefCounted
{
	public virtual Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return null;
	}

	public virtual Texture2D GetIcon(XWFileSystemTreeItemData data, int targetSize)
	{
		return GetIcon(data);
	}

	public virtual void Execute(XWFileSystemTreeItemData data)
	{
	}

	protected static Texture2D LoadIcon(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}
}
