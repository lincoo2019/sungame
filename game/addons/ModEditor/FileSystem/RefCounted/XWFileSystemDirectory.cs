using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemDirectory : RefCounted
{
	public string Name { get; set; } = "";

	public string Path { get; set; } = "";

	public ulong ModifiedTime { get; set; }

	public XWFileSystemDirectory Parent { get; set; }

	public List<XWFileSystemDirectory> Subdirs { get; } = new List<XWFileSystemDirectory>();

	public List<XWFileInfo> Files { get; } = new List<XWFileInfo>();

	public XWFileSystemDirectory()
	{
	}

	public XWFileSystemDirectory(string pName, string pPath)
	{
		Name = pName;
		Path = pPath;
	}

	public int GetSubdirCount()
	{
		return Subdirs.Count;
	}

	public XWFileSystemDirectory GetSubdir(int idx)
	{
		if (idx < 0 || idx >= Subdirs.Count)
		{
			return null;
		}
		return Subdirs[idx];
	}

	public int GetFileCount()
	{
		return Files.Count;
	}

	public XWFileInfo GetFile(int idx)
	{
		if (idx < 0 || idx >= Files.Count)
		{
			return null;
		}
		return Files[idx];
	}

	public int FindDirIndex(string dirName)
	{
		for (int i = 0; i < Subdirs.Count; i++)
		{
			if (Subdirs[i].Name == dirName)
			{
				return i;
			}
		}
		return -1;
	}

	public int FindFileIndex(string fileName)
	{
		for (int i = 0; i < Files.Count; i++)
		{
			if (Files[i].Name == fileName)
			{
				return i;
			}
		}
		return -1;
	}

	public XWFileSystemDirectory GetSubdirByName(string name)
	{
		int num = FindDirIndex(name);
		if (num < 0)
		{
			return null;
		}
		return Subdirs[num];
	}

	public XWFileInfo GetFileByName(string name)
	{
		int num = FindFileIndex(name);
		if (num < 0)
		{
			return null;
		}
		return Files[num];
	}

	public void AddSubdir(XWFileSystemDirectory subdir)
	{
		subdir.Parent = this;
		Subdirs.Add(subdir);
	}

	public void RemoveSubdir(int idx)
	{
		if (idx >= 0 && idx < Subdirs.Count)
		{
			Subdirs.RemoveAt(idx);
		}
	}

	public void AddFile(XWFileInfo file)
	{
		Files.Add(file);
	}

	public void RemoveFile(int idx)
	{
		if (idx >= 0 && idx < Files.Count)
		{
			Files.RemoveAt(idx);
		}
	}

	public void SortFiles(XWFileSystemEnum.SortMode sortMode)
	{
		XWFileInfo.SortList(Files, sortMode);
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			subdir.SortFiles(sortMode);
		}
	}

	public void SortSubdirs(XWFileSystemEnum.SortMode sortMode)
	{
		switch (sortMode)
		{
		case XWFileSystemEnum.SortMode.NameAsc:
			Subdirs.Sort((XWFileSystemDirectory a, XWFileSystemDirectory b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			break;
		case XWFileSystemEnum.SortMode.NameDesc:
			Subdirs.Sort((XWFileSystemDirectory a, XWFileSystemDirectory b) => -string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			break;
		default:
			Subdirs.Sort((XWFileSystemDirectory a, XWFileSystemDirectory b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			break;
		}
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			subdir.SortSubdirs(sortMode);
		}
	}

	public XWFileInfo FindFileInTree(string filePath)
	{
		foreach (XWFileInfo file in Files)
		{
			if (file.Path == filePath)
			{
				return file;
			}
		}
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			XWFileInfo xWFileInfo = subdir.FindFileInTree(filePath);
			if (xWFileInfo != null)
			{
				return xWFileInfo;
			}
		}
		return null;
	}

	public XWFileSystemDirectory FindDirectoryInTree(string dirPath)
	{
		if (Path == dirPath)
		{
			return this;
		}
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			XWFileSystemDirectory xWFileSystemDirectory = subdir.FindDirectoryInTree(dirPath);
			if (xWFileSystemDirectory != null)
			{
				return xWFileSystemDirectory;
			}
		}
		return null;
	}

	public List<string> GetAllFilePaths()
	{
		List<string> list = new List<string>();
		foreach (XWFileInfo file in Files)
		{
			list.Add(file.Path);
		}
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			list.AddRange(subdir.GetAllFilePaths());
		}
		return list;
	}

	public List<string> GetAllDirPaths()
	{
		List<string> list = new List<string> { Path };
		foreach (XWFileSystemDirectory subdir in Subdirs)
		{
			list.AddRange(subdir.GetAllDirPaths());
		}
		return list;
	}

	public void Clear()
	{
		Subdirs.Clear();
		Files.Clear();
	}
}
