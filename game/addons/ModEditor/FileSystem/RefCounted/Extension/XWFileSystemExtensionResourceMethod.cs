using Godot;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemExtensionResourceMethod : XWFileSystemExtensionMethod
{
	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
	}
}
