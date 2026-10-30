using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemList : ItemList
{
	[Signal]
	public delegate void ItemActivatedFileEventHandler(XWFileSystemTreeItemData data);

	[Signal]
	public delegate void ItemActivatedFolderEventHandler(string path);

	private XWFileSystemDirectory _currentDir;

	private XWFileSystemEnum.FileListDisplayMode _displayMode = XWFileSystemEnum.FileListDisplayMode.List;

	private string _searchText = "";

	private const int ListIconSize = 16;

	private const int ThumbnailIconSize = 72;

	private static readonly Color ProtectedFolderIconColor = new Color(0.42f, 0.65f, 1f);

	private static readonly Color NormalFolderIconColor = Colors.White;

	public override void _Ready()
	{
		base.ItemActivated += OnItemActivated;
		UpdateIconDisplay();
	}

	public void SetDisplayMode(XWFileSystemEnum.FileListDisplayMode mode)
	{
		_displayMode = mode;
		UpdateIconDisplay();
		Refresh();
	}

	private void UpdateIconDisplay()
	{
		SetIconMode((IconModeEnum)((_displayMode == XWFileSystemEnum.FileListDisplayMode.Thumbnails) ? 0 : 1));
		int currentIconSize = GetCurrentIconSize();
		base.FixedIconSize = new Vector2I(currentIconSize, currentIconSize);
	}

	private int GetCurrentIconSize()
	{
		if (_displayMode != XWFileSystemEnum.FileListDisplayMode.Thumbnails)
		{
			return 16;
		}
		return 72;
	}

	public void DisplayDirectory(XWFileSystemDirectory dir)
	{
		_currentDir = dir;
		Refresh();
	}

	public bool SelectPath(string path)
	{
		path = NormalizePath(path);
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		DeselectAll();
		for (int i = 0; i < base.ItemCount; i++)
		{
			XWFileSystemTreeItemData xWFileSystemTreeItemData = GetItemMetadata(i).As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null && !(NormalizePath(xWFileSystemTreeItemData.Path) != path))
			{
				Select(i);
				EnsureCurrentIsVisible();
				return true;
			}
		}
		return false;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		List<string> dragPaths = GetDragPaths(atPosition);
		if (dragPaths.Count == 0)
		{
			return default(Variant);
		}
		Dictionary from = BuildFileDragData(dragPaths);
		SetDragPreview(CreateDragPreview(dragPaths));
		return Variant.From(in from);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		string dropTargetDirectory = GetDropTargetDirectory(atPosition);
		return CanMoveDropData(data, dropTargetDirectory);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		string dropTargetDirectory = GetDropTargetDirectory(atPosition);
		MoveDraggedFilesToDirectory(data, dropTargetDirectory);
	}

	public void Search(string query)
	{
		_searchText = query ?? "";
		Refresh();
	}

	public void Refresh()
	{
		Clear();
		if (_currentDir == null)
		{
			return;
		}
		foreach (XWFileSystemDirectory subdir in _currentDir.Subdirs)
		{
			XWFileSystemDirectory from = subdir;
			if (MatchesFolderFilters(from))
			{
				string projectFolderPath = XWFileSystem.GetSingleton().ProjectFolderPath;
				string displayName = XWModProjectLayout.GetDisplayName(XWModProjectLayout.ToProjectRelativePath(from.Path, projectFolderPath));
				int num = AddItem(displayName);
				SetItemMetadata(num, Variant.From<XWFileSystemTreeItemData>(new XWFileSystemTreeItemData(from.Path, Variant.From(in from), pIsFolder: true)));
				Texture2D texture2D = XWClassRegistry.Instance?.GetUIIcon("Folder");
				if (texture2D != null)
				{
					SetItemIcon(num, texture2D);
				}
				ApplyFolderIconColor(num, from.Path);
			}
		}
		foreach (XWFileInfo file in _currentDir.Files)
		{
			XWFileInfo from2 = file;
			if (MatchesFilters(from2))
			{
				int idx = AddItem(from2.Name);
				XWFileSystemTreeItemData from3 = new XWFileSystemTreeItemData(from2.Path, Variant.From(in from2), pIsFolder: false)
				{
					FileType = from2.Type,
					ModifiedTime = (long)from2.ModifiedTime
				};
				SetItemMetadata(idx, Variant.From(in from3));
				Texture2D icon = XWFileSystemExtensionRegistry.GetIcon(from3, GetCurrentIconSize());
				if (icon != null)
				{
					SetItemIcon(idx, icon);
				}
			}
		}
	}

	private bool MatchesFilters(XWFileInfo file)
	{
		if (file == null)
		{
			return false;
		}
		if (XWFileSystemCompanionResourcePolicy.ShouldDisplayAsOwnerChildOnly(file.Path))
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(_searchText) && !file.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) && !file.Path.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private bool MatchesFolderFilters(XWFileSystemDirectory dir)
	{
		if (dir == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(_searchText) && !dir.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) && !dir.Path.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private void ApplyFolderIconColor(int itemIndex, string folderPath)
	{
		SetItemIconModulate(itemIndex, IsProtectedDirectory(folderPath) ? ProtectedFolderIconColor : NormalFolderIconColor);
	}

	private static bool IsProtectedDirectory(string folderPath)
	{
		return XWModProjectLayout.IsProtectedDirectory(folderPath, XWFileSystem.GetSingleton().ProjectFolderPath);
	}

	private void OnItemActivated(long index)
	{
		XWFileSystemTreeItemData xWFileSystemTreeItemData = GetItemMetadata((int)index).As<XWFileSystemTreeItemData>();
		if (xWFileSystemTreeItemData != null)
		{
			if (xWFileSystemTreeItemData.IsFolder)
			{
				EmitSignal(SignalName.ItemActivatedFolder, xWFileSystemTreeItemData.Path);
			}
			else
			{
				EmitSignal(SignalName.ItemActivatedFile, xWFileSystemTreeItemData);
			}
		}
	}

	private List<string> GetDragPaths(Vector2 atPosition)
	{
		List<string> list = new List<string>();
		int itemAtPosition = GetItemAtPosition(atPosition, exact: true);
		if (itemAtPosition >= 0 && !IsSelected(itemAtPosition))
		{
			AddItemPath(list, itemAtPosition);
			return list;
		}
		int[] selectedItems = GetSelectedItems();
		foreach (int index in selectedItems)
		{
			AddItemPath(list, index);
		}
		if (list.Count == 0 && itemAtPosition >= 0)
		{
			AddItemPath(list, itemAtPosition);
		}
		return list;
	}

	private void AddItemPath(List<string> paths, int index)
	{
		if (index >= 0 && index < base.ItemCount)
		{
			XWFileSystemTreeItemData xWFileSystemTreeItemData = GetItemMetadata(index).As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null && !string.IsNullOrWhiteSpace(xWFileSystemTreeItemData.Path))
			{
				paths.Add(xWFileSystemTreeItemData.Path);
			}
		}
	}

	private static Dictionary BuildFileDragData(List<string> paths)
	{
		string[] array = paths.ToArray();
		bool flag = false;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (DirAccess.DirExistsAbsolute(array2[i]))
			{
				flag = true;
				break;
			}
		}
		return new Dictionary
		{
			["type"] = "files",
			["files"] = array,
			["paths"] = array,
			["path"] = ((array.Length != 0) ? array[0] : ""),
			["has_dirs"] = flag
		};
	}

	private static Control CreateDragPreview(List<string> paths)
	{
		string text = ((paths.Count == 1) ? paths[0].GetFile() : $"{paths.Count} 个项目");
		return new Label
		{
			Text = text
		};
	}

	private string GetDropTargetDirectory(Vector2 atPosition)
	{
		int itemAtPosition = GetItemAtPosition(atPosition, exact: true);
		if (itemAtPosition >= 0 && itemAtPosition < base.ItemCount)
		{
			XWFileSystemTreeItemData xWFileSystemTreeItemData = GetItemMetadata(itemAtPosition).As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null && xWFileSystemTreeItemData.IsFolder)
			{
				return NormalizeDirectoryPath(xWFileSystemTreeItemData.Path);
			}
		}
		return NormalizeDirectoryPath(_currentDir?.Path ?? "");
	}

	public string GetDropTargetDirectoryAtGlobalPosition(Vector2 globalPosition)
	{
		Vector2 atPosition = GetGlobalTransform().AffineInverse() * globalPosition;
		return GetDropTargetDirectory(atPosition);
	}

	private static bool CanMoveDropData(Variant data, string targetDirectory)
	{
		return XWFileSystemDropHelper.CanDropFilesToDirectory(data, targetDirectory);
	}

	private static void MoveDraggedFilesToDirectory(Variant data, string targetDirectory)
	{
		XWFileSystemDropHelper.DropFilesToDirectory(data, targetDirectory);
	}

	private static List<string> ExtractDropPaths(Variant data)
	{
		List<string> list = new List<string>();
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return list;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (dictionary.ContainsKey("files"))
		{
			string[] array = dictionary["files"].AsStringArray();
			foreach (string path in array)
			{
				AddDropPath(list, path);
			}
		}
		else if (dictionary.ContainsKey("paths"))
		{
			string[] array = dictionary["paths"].AsStringArray();
			foreach (string path2 in array)
			{
				AddDropPath(list, path2);
			}
		}
		else if (dictionary.ContainsKey("path"))
		{
			AddDropPath(list, dictionary["path"].AsString());
		}
		return list;
	}

	private static void AddDropPath(List<string> paths, string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			paths.Add(NormalizePath(path));
		}
	}

	private static bool CanMovePathToDirectory(string path, string targetDirectory)
	{
		string text = NormalizePath(path);
		targetDirectory = NormalizeDirectoryPath(targetDirectory);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetDirectory))
		{
			return false;
		}
		if (!FileAccess.FileExists(text) && !DirAccess.DirExistsAbsolute(text))
		{
			return false;
		}
		if ((DirAccess.DirExistsAbsolute(text) ? NormalizeDirectoryPath(text) : NormalizeDirectoryPath(text.GetBaseDir())) == targetDirectory)
		{
			return false;
		}
		if (DirAccess.DirExistsAbsolute(text) && targetDirectory.StartsWith(NormalizeDirectoryPath(text), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private static string MakeUniqueMoveTarget(XWFileSystem fs, string sourcePath, string targetPath)
	{
		string text = NormalizeDirectoryPath(targetPath.GetBaseDir());
		string file = targetPath.GetFile();
		if (DirAccess.DirExistsAbsolute(sourcePath))
		{
			if (!DirAccess.DirExistsAbsolute(targetPath) && !FileAccess.FileExists(targetPath))
			{
				return targetPath;
			}
			return text.PathJoin(fs.GenerateUniqueFolderName(text, file));
		}
		if (!FileAccess.FileExists(targetPath) && !DirAccess.DirExistsAbsolute(targetPath))
		{
			return targetPath;
		}
		return text.PathJoin(fs.GenerateUniqueFileName(text, file));
	}

	private static string NormalizeDirectoryPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		path = NormalizePath(path);
		if (!path.EndsWith("/"))
		{
			return path + "/";
		}
		return path;
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		return path.Replace('\\', '/').TrimEnd('/');
	}
}
