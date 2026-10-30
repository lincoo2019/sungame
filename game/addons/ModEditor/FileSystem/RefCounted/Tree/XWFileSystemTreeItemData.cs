using Godot;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemTreeItemData : RefCounted
{
	public Variant Data { get; set; }

	public string Path { get; set; } = "";

	public bool IsFolder { get; set; }

	public bool IsSubResource { get; set; }

	public string SubResourceOwnerPath { get; set; } = "";

	public string SubResourcePropertyPath { get; set; } = "";

	public string SubResourceDisplayName { get; set; } = "";

	public bool IsCompanionResource { get; set; }

	public string CompanionOwnerPath { get; set; } = "";

	public string CompanionKind { get; set; } = "";

	public string CompanionDisplayName { get; set; } = "";

	public long ModifiedTime { get; set; }

	public StringName FileType { get; set; } = "";

	public XWFileSystemTreeItemData()
	{
	}

	public XWFileSystemTreeItemData(string pPath, Variant pData, bool pIsFolder)
	{
		Path = pPath;
		Data = pData;
		IsFolder = pIsFolder;
	}

	public string GetExtension()
	{
		return Path.GetExtension().ToLower();
	}

	public string GetFileName()
	{
		return Path.GetFile();
	}

	public string GetDisplayName()
	{
		if (IsSubResource && !string.IsNullOrWhiteSpace(SubResourceDisplayName))
		{
			return SubResourceDisplayName;
		}
		if (IsCompanionResource && !string.IsNullOrWhiteSpace(CompanionDisplayName))
		{
			return CompanionDisplayName;
		}
		if (IsFolder)
		{
			return Path.TrimSuffix("/").GetFile();
		}
		return GetFileName();
	}

	public string GetBaseDir()
	{
		return Path.GetBaseDir();
	}
}
