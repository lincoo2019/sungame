namespace PVZHE.ModEditor.FileSystem;

public static class XWFileSystemEnum
{
	public enum FileItemType
	{
		File,
		Directory
	}

	public enum SortMode
	{
		NameAsc,
		NameDesc,
		TypeAsc,
		TypeDesc,
		ModifiedTimeAsc,
		ModifiedTimeDesc
	}

	public enum DisplayMode
	{
		TreeOnly,
		VSplit,
		HSplit
	}

	public enum FileListDisplayMode
	{
		Thumbnails,
		List
	}

	public enum FileMenu
	{
		Open,
		Inherit,
		Instance,
		AddFavorite,
		RemoveFavorite,
		Move,
		Rename,
		Remove,
		Duplicate,
		NewFolder,
		NewScript,
		NewScene,
		NewBlueprint,
		CopyPath,
		CopyAbsolutePath,
		CopyUid,
		ShowInExplorer,
		Reimport,
		Copy,
		Paste,
		NewTextfile,
		OpenInTerminal,
		OpenScene,
		InheritScene,
		SetMainScene,
		InstantiateScene,
		SetFolderColor
	}

	public enum Overwrite
	{
		Undecided,
		Replace,
		Rename
	}
}
