using System;
using System.Collections.Generic;
using System.IO;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.Tools;

public sealed class XWImportWizard
{
	public sealed class ImportResult
	{
		public string SourcePath { get; set; } = "";

		public string TargetPath { get; set; } = "";

		public string ResourceKey { get; set; } = "";

		public bool Success { get; set; }

		public string Error { get; set; } = "";
	}

	public ImportResult ImportFile(string sourcePath, string modRoot, string category)
	{
		if (!File.Exists(sourcePath))
		{
			return new ImportResult
			{
				SourcePath = sourcePath,
				Success = false,
				Error = "Source file does not exist."
			};
		}
		if (string.IsNullOrWhiteSpace(modRoot))
		{
			return new ImportResult
			{
				SourcePath = sourcePath,
				Success = false,
				Error = "Mod root is empty."
			};
		}
		string fileName = Path.GetFileName(sourcePath);
		string text = XWModProjectLayout.GetAssetFolderForExtension(Path.GetExtension(sourcePath), category);
		if (text == "Assets" && !string.IsNullOrWhiteSpace(category))
		{
			text = Path.Combine("Assets", SanitizeKey(category)).Replace('\\', '/');
		}
		string text2 = Path.Combine(modRoot, text.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, fileName);
		File.Copy(sourcePath, text3, overwrite: true);
		return new ImportResult
		{
			SourcePath = sourcePath,
			TargetPath = text3,
			ResourceKey = SanitizeKey(Path.GetFileNameWithoutExtension(fileName)),
			Success = true
		};
	}

	public List<ImportResult> ImportFiles(IEnumerable<string> sourcePaths, string modRoot, string category)
	{
		List<ImportResult> list = new List<ImportResult>();
		foreach (string sourcePath in sourcePaths)
		{
			list.Add(ImportFile(sourcePath, modRoot, category));
		}
		return list;
	}

	public List<ImportResult> ImportFilesAndUpdateManifest(IEnumerable<string> sourcePaths, string modRoot, string category)
	{
		List<ImportResult> list = ImportFiles(sourcePaths, modRoot, category);
		UpdateManifest(modRoot, list);
		return list;
	}

	private static void UpdateManifest(string modRoot, IEnumerable<ImportResult> results)
	{
		if (!string.IsNullOrWhiteSpace(modRoot))
		{
			XWModManifestSyncService.RegisterPaths(modRoot, SuccessfulImportTargets(results));
		}
	}

	private static IEnumerable<string> SuccessfulImportTargets(IEnumerable<ImportResult> results)
	{
		if (results == null)
		{
			yield break;
		}
		foreach (ImportResult result in results)
		{
			if (result != null && result.Success && !string.IsNullOrWhiteSpace(result.TargetPath))
			{
				yield return result.TargetPath;
			}
		}
	}

	public static string ToModRelativePath(string path, string modRoot)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string result = path.Replace('\\', '/');
		if (string.IsNullOrWhiteSpace(modRoot))
		{
			return result;
		}
		try
		{
			string fullPath = Path.GetFullPath(path);
			string relativePath = Path.GetRelativePath(Path.GetFullPath(modRoot), fullPath);
			if (!relativePath.StartsWith("..", StringComparison.Ordinal))
			{
				return relativePath.Replace('\\', '/');
			}
			return result;
		}
		catch
		{
			return result;
		}
	}

	public static string SanitizeKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		List<char> list = new List<char>(value.Length);
		string text = value.Trim();
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
			{
				list.Add(c);
			}
			else if (char.IsWhiteSpace(c) || c == '.' || c == '/')
			{
				list.Add('_');
			}
		}
		string text2 = new string(list.ToArray()).Trim('_');
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return "resource";
	}
}
