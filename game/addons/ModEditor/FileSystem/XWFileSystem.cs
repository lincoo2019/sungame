using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystem : RefCounted
{
	[Signal]
	public delegate void FilesystemChangedEventHandler();

	[Signal]
	public delegate void ScanStartedEventHandler();

	[Signal]
	public delegate void ScanCompletedEventHandler();

	[Signal]
	public delegate void ScanProgressEventHandler(int current, int total);

	[Signal]
	public delegate void FileChangedEventHandler(string filePath);

	[Signal]
	public delegate void DirectoryChangedEventHandler(string dirPath);

	private static string[] _showExtensionsArray;

	public static XWFileSystem Instance { get; private set; }

	public XWFileSystemDirectory RootDirectory { get; private set; }

	public string ProjectFolderPath { get; private set; } = "";

	public bool IsScanning { get; private set; }

	public XWFileSystemEnum.SortMode SortMode { get; private set; }

	public List<string> Favorites { get; } = new List<string>();

	public int ScanTotal { get; private set; }

	public int ScanCurrent { get; private set; }

	public XWFileSystem()
	{
		XWFileSystemExtensionRegistry.Init();
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public static XWFileSystem GetSingleton()
	{
		if (Instance == null)
		{
			Instance = new XWFileSystem();
		}
		return Instance;
	}

	public void SetProjectFolderPath(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			path = path.Replace('\\', '/');
			if (!path.EndsWith("/"))
			{
				path += "/";
			}
		}
		ProjectFolderPath = path;
		Scan();
	}

	public void Scan()
	{
		if (!string.IsNullOrEmpty(ProjectFolderPath))
		{
			XWFileSystemExtensionRegistry.Init();
			IsScanning = true;
			XWModManifestSyncService.SyncProject(ProjectFolderPath);
			ScanTotal = 0;
			ScanCurrent = 0;
			EmitSignal(SignalName.ScanStarted);
			RootDirectory = new XWFileSystemDirectory("res://", ProjectFolderPath);
			ScanDirectory(RootDirectory, ProjectFolderPath);
			SortAll();
			IsScanning = false;
			EmitSignal(SignalName.ScanCompleted);
			EmitSignal(SignalName.FilesystemChanged);
		}
	}

	public void ScanChanges()
	{
		if (RootDirectory == null)
		{
			Scan();
			return;
		}
		XWModManifestSyncService.SyncProject(ProjectFolderPath);
		UpdateDirectory(RootDirectory, ProjectFolderPath);
		SortAll();
		EmitSignal(SignalName.FilesystemChanged);
	}

	private StringName GetResourceTypeName(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string text = path.GetExtension().ToLowerInvariant();
		string fallbackTypeNameByExtension = GetFallbackTypeNameByExtension(text);
		if (!string.IsNullOrEmpty(fallbackTypeNameByExtension) && text != "res" && text != "tres")
		{
			return fallbackTypeNameByExtension;
		}
		string typeNameFromTextResourceHeader = GetTypeNameFromTextResourceHeader(path, text);
		if (!string.IsNullOrEmpty(typeNameFromTextResourceHeader))
		{
			return typeNameFromTextResourceHeader;
		}
		return fallbackTypeNameByExtension;
	}

	private static string GetFallbackTypeNameByExtension(string extension)
	{
		switch (extension)
		{
		case "tscn":
		case "scn":
			return "PackedScene";
		case "cs":
			return "CSharpScript";
		case "jpeg":
		case "webp":
		case "png":
		case "jpg":
		case "svg":
			return "Texture2D";
		case "mp3":
		case "wav":
		case "ogg":
			return "AudioStream";
		case "json":
		case "csv":
		case "cfg":
		case "txt":
			return "TextFile";
		default:
			return "";
		}
	}

	private static string GetTypeNameFromTextResourceHeader(string path, string extension)
	{
		if (extension != "tres" && extension != "tscn")
		{
			return "";
		}
		if (!Godot.FileAccess.FileExists(path))
		{
			return "";
		}
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return "";
		}
		for (int i = 0; i < 8; i++)
		{
			if (fileAccess.EofReached())
			{
				break;
			}
			string text = fileAccess.GetLine().StripEdges();
			if (text.StartsWith("[gd_scene", StringComparison.Ordinal))
			{
				return "PackedScene";
			}
			if (text.StartsWith("[gd_resource", StringComparison.Ordinal))
			{
				return GetQuotedHeaderAttribute(text, "type");
			}
		}
		return "";
	}

	private static string GetQuotedHeaderAttribute(string line, string attributeName)
	{
		string text = attributeName + "=\"";
		int num = line.IndexOf(text, StringComparison.Ordinal);
		if (num < 0)
		{
			return "";
		}
		num += text.Length;
		int num2 = line.IndexOf('"', num);
		if (num2 <= num)
		{
			return "";
		}
		return line.Substring(num, num2 - num);
	}

	private void ScanDirectory(XWFileSystemDirectory pDir, string pPath)
	{
		using DirAccess dirAccess = DirAccess.Open(pPath);
		if (dirAccess == null)
		{
			return;
		}
		dirAccess.ListDirBegin();
		string next = dirAccess.GetNext();
		while (next != "")
		{
			if (dirAccess.CurrentIsDir())
			{
				string pPath2 = pPath + next + "/";
				XWFileSystemDirectory xWFileSystemDirectory = new XWFileSystemDirectory(next, pPath2);
				pDir.AddSubdir(xWFileSystemDirectory);
				ScanDirectory(xWFileSystemDirectory, pPath2);
			}
			else if (XWFileSystemExtensionRegistry.GetCanShowExtension(next.GetExtension()))
			{
				string text = pPath + next;
				XWFileInfo file = new XWFileInfo(next, text)
				{
					ModifiedTime = Godot.FileAccess.GetModifiedTime(text),
					Type = GetResourceTypeName(text)
				};
				pDir.AddFile(file);
			}
			next = dirAccess.GetNext();
			ScanCurrent++;
			if (ScanCurrent % 25 == 0)
			{
				EmitSignal(SignalName.ScanProgress, ScanCurrent, ScanTotal);
			}
		}
		dirAccess.ListDirEnd();
	}

	private void UpdateDirectory(XWFileSystemDirectory pDir, string pPath)
	{
		using DirAccess dirAccess = DirAccess.Open(pPath);
		if (dirAccess == null)
		{
			return;
		}
		Dictionary<string, XWFileSystemDirectory> dictionary = new Dictionary<string, XWFileSystemDirectory>();
		foreach (XWFileSystemDirectory subdir in pDir.Subdirs)
		{
			dictionary[subdir.Name] = subdir;
		}
		Dictionary<string, XWFileInfo> dictionary2 = new Dictionary<string, XWFileInfo>();
		foreach (XWFileInfo file2 in pDir.Files)
		{
			dictionary2[file2.Name] = file2;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		dirAccess.ListDirBegin();
		string next = dirAccess.GetNext();
		while (next != "")
		{
			if (dirAccess.CurrentIsDir())
			{
				if (!dictionary.ContainsKey(next))
				{
					string pPath2 = pPath + next + "/";
					XWFileSystemDirectory xWFileSystemDirectory = new XWFileSystemDirectory(next, pPath2);
					pDir.AddSubdir(xWFileSystemDirectory);
					ScanDirectory(xWFileSystemDirectory, pPath2);
				}
				else
				{
					UpdateDirectory(dictionary[next], pPath + next + "/");
				}
				list.Add(next);
			}
			else if (XWFileSystemExtensionRegistry.GetCanShowExtension(next.GetExtension()))
			{
				string text = pPath + next;
				if (!dictionary2.ContainsKey(next))
				{
					XWFileInfo file = new XWFileInfo(next, text)
					{
						ModifiedTime = Godot.FileAccess.GetModifiedTime(text),
						Type = GetResourceTypeName(text)
					};
					pDir.AddFile(file);
				}
				else
				{
					XWFileInfo xWFileInfo = dictionary2[next];
					ulong modifiedTime = Godot.FileAccess.GetModifiedTime(text);
					if (modifiedTime != xWFileInfo.ModifiedTime)
					{
						xWFileInfo.ModifiedTime = modifiedTime;
						xWFileInfo.Type = GetResourceTypeName(text);
						EmitSignal(SignalName.FileChanged, text);
					}
				}
				list2.Add(next);
			}
			next = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		for (int num = pDir.Subdirs.Count - 1; num >= 0; num--)
		{
			if (!list.Contains(pDir.Subdirs[num].Name))
			{
				pDir.RemoveSubdir(num);
			}
		}
		for (int num2 = pDir.Files.Count - 1; num2 >= 0; num2--)
		{
			if (!list2.Contains(pDir.Files[num2].Name))
			{
				pDir.RemoveFile(num2);
			}
		}
	}

	private void SortAll()
	{
		if (RootDirectory != null)
		{
			RootDirectory.SortFiles(SortMode);
			RootDirectory.SortSubdirs(SortMode);
		}
	}

	public void SetSortMode(XWFileSystemEnum.SortMode mode)
	{
		SortMode = mode;
		SortAll();
		EmitSignal(SignalName.FilesystemChanged);
	}

	public XWFileSystemDirectory GetFilesystem()
	{
		return RootDirectory;
	}

	public XWFileSystemDirectory GetFilesystemPath(string path)
	{
		return RootDirectory?.FindDirectoryInTree(path);
	}

	public StringName GetFileType(string filePath)
	{
		if (RootDirectory == null)
		{
			return "";
		}
		return RootDirectory.FindFileInTree(filePath)?.Type ?? ((StringName)"");
	}

	public void UpdateFile(string filePath)
	{
		if (RootDirectory != null)
		{
			XWFileInfo xWFileInfo = RootDirectory.FindFileInTree(filePath);
			if (xWFileInfo != null)
			{
				xWFileInfo.ModifiedTime = Godot.FileAccess.GetModifiedTime(filePath);
				xWFileInfo.Type = GetResourceTypeName(filePath);
				EmitSignal(SignalName.FileChanged, filePath);
			}
		}
	}

	public Error MakeDirRecursive(string path)
	{
		Error error = DirAccess.MakeDirRecursiveAbsolute(path);
		if (error == Error.Ok)
		{
			ScanChanges();
			EmitSignal(SignalName.DirectoryChanged, path);
		}
		return error;
	}

	public Error CopyFile(string from, string to)
	{
		using DirAccess dirAccess = DirAccess.Open(from.GetBaseDir());
		if (dirAccess == null)
		{
			return Error.CantOpen;
		}
		Error error = dirAccess.Copy(from, to);
		if (error == Error.Ok)
		{
			ScanChanges();
			EmitSignal(SignalName.FileChanged, to);
		}
		return error;
	}

	public Error CopyDirectory(string from, string to)
	{
		using DirAccess dirAccess = DirAccess.Open(from);
		if (dirAccess == null)
		{
			return Error.CantOpen;
		}
		DirAccess.MakeDirRecursiveAbsolute(to);
		dirAccess.ListDirBegin();
		string next = dirAccess.GetNext();
		while (next != "")
		{
			if (next == "." || next == "..")
			{
				next = dirAccess.GetNext();
				continue;
			}
			string text = from + "/" + next;
			string text2 = to + "/" + next;
			if (dirAccess.CurrentIsDir())
			{
				CopyDirectory(text + "/", text2 + "/");
			}
			else
			{
				DirAccess.CopyAbsolute(text, text2);
			}
			next = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		ScanChanges();
		return Error.Ok;
	}

	public Error MoveFile(string from, string to)
	{
		if (XWModProjectLayout.IsProtectedDirectory(from, ProjectFolderPath))
		{
			return Error.Unauthorized;
		}
		TryGetCharacterPackageRename(from, to, out var oldCharacterName, out var newCharacterName, out var renamedCharacterPackagePath);
		Error error = DirAccess.RenameAbsolute(from, to);
		if (error == Error.Ok)
		{
			XWModManifestSyncService.MovePath(ProjectFolderPath, from, to);
			if (!string.IsNullOrWhiteSpace(renamedCharacterPackagePath))
			{
				SyncCharacterPackageAfterRename(renamedCharacterPackagePath, oldCharacterName, newCharacterName);
			}
			ScanChanges();
			EmitSignal(SignalName.FileChanged, to);
		}
		return error;
	}

	private bool TryGetCharacterPackageRename(string from, string to, out string oldCharacterName, out string newCharacterName, out string renamedCharacterPackagePath)
	{
		oldCharacterName = "";
		newCharacterName = "";
		renamedCharacterPackagePath = "";
		if (string.IsNullOrWhiteSpace(ProjectFolderPath) || !Directory.Exists(from))
		{
			return false;
		}
		string path = XWModProjectLayout.ToProjectRelativePath(from, ProjectFolderPath);
		string path2 = XWModProjectLayout.ToProjectRelativePath(to, ProjectFolderPath);
		string[] array = NormalizePath(path).Split('/', StringSplitOptions.RemoveEmptyEntries);
		string[] array2 = NormalizePath(path2).Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 4 || array2.Length != 4)
		{
			return false;
		}
		if (!string.Equals(array[0], "Resources", StringComparison.OrdinalIgnoreCase) || !string.Equals(array[1], "Characters", StringComparison.OrdinalIgnoreCase) || !string.Equals(array2[0], "Resources", StringComparison.OrdinalIgnoreCase) || !string.Equals(array2[1], "Characters", StringComparison.OrdinalIgnoreCase) || !string.Equals(array[2], array2[2], StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		oldCharacterName = array[3];
		newCharacterName = array2[3];
		if (string.Equals(oldCharacterName, newCharacterName, StringComparison.Ordinal))
		{
			return false;
		}
		renamedCharacterPackagePath = NormalizePath(to);
		return true;
	}

	private void SyncCharacterPackageAfterRename(string packagePath, string oldCharacterName, string newCharacterName)
	{
		if (!string.IsNullOrWhiteSpace(packagePath) && !string.IsNullOrWhiteSpace(oldCharacterName) && !string.IsNullOrWhiteSpace(newCharacterName) && Directory.Exists(packagePath))
		{
			RenameCharacterPackageFileIfExists(packagePath, "", oldCharacterName + ".tres", newCharacterName + ".tres");
			RenameCharacterPackageFileIfExists(packagePath, "", oldCharacterName + ".dat", newCharacterName + ".dat");
			RenameCharacterPackageFileIfExists(packagePath, "Scene", oldCharacterName + ".tscn", newCharacterName + ".tscn");
			RenameCharacterPackageFileIfExists(packagePath, "Script", oldCharacterName + ".cs", newCharacterName + ".cs");
			RenameCharacterPackageFileIfExists(packagePath, "Script", oldCharacterName + ".cs.uid", newCharacterName + ".cs.uid");
			RenameCharacterPackageFileIfExists(packagePath, "Config", oldCharacterName + "Config.tres", newCharacterName + "Config.tres");
			RenameLegacyCharacterConfigIfExists(packagePath, newCharacterName);
			RenameCharacterPackageFileIfExists(packagePath, "Sprite", oldCharacterName + ".tscn", newCharacterName + ".tscn");
			RenameCharacterPackageFileIfExists(packagePath, "Sprite", oldCharacterName + ".tres", newCharacterName + ".tres");
			RenameCharacterPackageFileIfExists(packagePath, "Sprite", oldCharacterName + ".dat", newCharacterName + ".dat");
			ReplaceTextInCharacterPackage(packagePath, oldCharacterName, newCharacterName);
		}
	}

	private void RenameLegacyCharacterConfigIfExists(string packagePath, string newCharacterName)
	{
		RenameCharacterPackageFileIfExists(packagePath, "Config", "Config.tres", newCharacterName + "Config.tres");
	}

	private void RenameCharacterPackageFileIfExists(string packagePath, string folderName, string oldFileName, string newFileName)
	{
		string path = NormalizePath(Path.Combine(packagePath, folderName));
		string text = NormalizePath(Path.Combine(path, oldFileName));
		string text2 = NormalizePath(Path.Combine(path, newFileName));
		if (File.Exists(text) && !File.Exists(text2) && DirAccess.RenameAbsolute(text, text2) == Error.Ok)
		{
			XWModManifestSyncService.MovePath(ProjectFolderPath, text, text2);
		}
	}

	private static void ReplaceTextInCharacterPackage(string packagePath, string oldCharacterName, string newCharacterName)
	{
		string[] files = Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories);
		foreach (string path in files)
		{
			bool flag;
			switch (Path.GetExtension(path).ToLowerInvariant())
			{
			case ".tscn":
			case ".tres":
			case ".cs":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				string text = File.ReadAllText(path);
				if (text.Contains(oldCharacterName, StringComparison.Ordinal))
				{
					File.WriteAllText(path, text.Replace(oldCharacterName, newCharacterName, StringComparison.Ordinal));
				}
			}
		}
	}

	public void RegisterManifestPath(string path)
	{
		XWModManifestSyncService.RegisterPath(ProjectFolderPath, path);
	}

	public Error RemoveFile(string path)
	{
		if (XWModProjectLayout.IsProtectedDirectory(path, ProjectFolderPath))
		{
			return Error.Unauthorized;
		}
		List<string> companionFilesForDeletion = GetCompanionFilesForDeletion(path);
		Error error = MovePathToTrash(path);
		if (error == Error.Ok)
		{
			XWModManifestSyncService.RemovePath(ProjectFolderPath, path);
			foreach (string item in companionFilesForDeletion)
			{
				Error error2 = MovePathToTrash(item);
				if (error2 != Error.Ok)
				{
					GD.PrintErr($"[XWFileSystem] 删除配套资源失败: {item} ({error2})");
				}
				else
				{
					XWModManifestSyncService.RemovePath(ProjectFolderPath, item);
				}
			}
		}
		ScanChanges();
		return error;
	}

	private Error MovePathToTrash(string path)
	{
		return OS.MoveToTrash(ProjectSettings.GlobalizePath(path));
	}

	private List<string> GetCompanionFilesForDeletion(string path)
	{
		List<string> list = new List<string>();
		path = NormalizePath(path);
		if (!XWFileSystemCompanionResourcePolicy.TryResolveAdobeAnimateDatPath(path, out var datPath) || string.IsNullOrWhiteSpace(datPath) || !Godot.FileAccess.FileExists(datPath) || !IsPathInsideProject(datPath))
		{
			return list;
		}
		string text = NormalizePath(datPath);
		if (string.Equals(NormalizePath(text), NormalizePath(path), StringComparison.OrdinalIgnoreCase))
		{
			return list;
		}
		list.Add(text);
		return list;
	}

	private bool IsPathInsideProject(string path)
	{
		string text = NormalizePath(path);
		string text2 = NormalizePath(ProjectFolderPath);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		return text.StartsWith(text2, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}

	public void AddFavorite(string path)
	{
		if (!Favorites.Contains(path))
		{
			Favorites.Add(path);
			EmitSignal(SignalName.FilesystemChanged);
		}
	}

	public void RemoveFavorite(string path)
	{
		Favorites.Remove(path);
		EmitSignal(SignalName.FilesystemChanged);
	}

	public bool IsFavorite(string path)
	{
		return Favorites.Contains(path);
	}

	public string[] GetValidExtensions()
	{
		return _showExtensionsArray ?? (_showExtensionsArray = ShowExtensionsToArray());
	}

	private static string[] ShowExtensionsToArray()
	{
		return new List<string>(XWFileSystemExtensionRegistry.ShowExtension).ToArray();
	}

	public string GenerateUniqueFolderName(string path, string baseName, string exclude = "")
	{
		List<string> list = new List<string>(DirAccess.GetDirectoriesAt(path));
		if (exclude != "")
		{
			list.Remove(exclude);
		}
		if (!list.Contains(baseName))
		{
			return baseName;
		}
		int num = 1;
		string text = baseName + num;
		while (list.Contains(text))
		{
			num++;
			text = baseName + num;
		}
		return text;
	}

	public string GenerateUniqueFileName(string path, string baseName, string exclude = "")
	{
		List<string> list = new List<string>(DirAccess.GetFilesAt(path));
		if (exclude != "")
		{
			list.Remove(exclude);
		}
		if (!list.Contains(baseName))
		{
			return baseName;
		}
		int num = 1;
		string text = baseName.GetBaseName() + num + "." + baseName.GetExtension();
		while (list.Contains(text))
		{
			num++;
			text = baseName.GetBaseName() + num + "." + baseName.GetExtension();
		}
		return text;
	}
}
