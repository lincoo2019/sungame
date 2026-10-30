using System;
using Godot;

namespace PVZHE.ModEditor.FileSystem;

public static class XWFileSystemCompanionResourcePolicy
{
	public static bool ShouldDisplayAsOwnerChildOnly(string path)
	{
		return IsAdobeAnimateDatCompanion(path);
	}

	public static bool IsAdobeAnimateDatCompanion(string path)
	{
		path = NormalizePath(path);
		if (string.IsNullOrWhiteSpace(path) || path.GetExtension().ToLowerInvariant() != "dat" || !FileAccess.FileExists(path))
		{
			return false;
		}
		if (IsAdobeAnimateResourceReferencingDat(path.GetBaseDir().PathJoin(path.GetFile().GetBaseName() + ".tres"), path))
		{
			return true;
		}
		using DirAccess dirAccess = DirAccess.Open(path.GetBaseDir());
		if (dirAccess == null)
		{
			return false;
		}
		string[] files = dirAccess.GetFiles();
		foreach (string text in files)
		{
			if (!(text.GetExtension().ToLowerInvariant() != "tres") && IsAdobeAnimateResourceReferencingDat(path.GetBaseDir().PathJoin(text), path))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryResolveAdobeAnimateDatPath(string ownerPath, out string datPath)
	{
		datPath = "";
		ownerPath = NormalizePath(ownerPath);
		if (!IsAdobeAnimateResource(ownerPath))
		{
			return false;
		}
		using FileAccess fileAccess = FileAccess.Open(ownerPath, FileAccess.ModeFlags.Read);
		if (fileAccess != null)
		{
			for (int i = 0; i < 512; i++)
			{
				if (fileAccess.EofReached())
				{
					break;
				}
				string text = ReadQuotedAssignment(fileAccess.GetLine().StripEdges(), "animeFile");
				if (!string.IsNullOrWhiteSpace(text))
				{
					datPath = ResolveCompanionPath(ownerPath, text);
					if (!string.IsNullOrWhiteSpace(datPath) && datPath.GetExtension().ToLowerInvariant() == "dat")
					{
						return true;
					}
				}
			}
		}
		string path = ownerPath.GetBaseDir().PathJoin(ownerPath.GetFile().GetBaseName() + ".dat");
		if (FileAccess.FileExists(path))
		{
			datPath = NormalizePath(path);
			return true;
		}
		return false;
	}

	public static string ResolveCompanionPath(string ownerPath, string rawPath)
	{
		if (string.IsNullOrWhiteSpace(rawPath) || rawPath.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		string text = NormalizePath(rawPath);
		if (FileAccess.FileExists(text))
		{
			return text;
		}
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		string path = NormalizePath(ownerPath).GetBaseDir().PathJoin(text);
		if (!FileAccess.FileExists(path))
		{
			return "";
		}
		return NormalizePath(path);
	}

	public static string ReadQuotedAssignment(string line, string propertyName)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(propertyName))
		{
			return "";
		}
		string text = propertyName + " = \"";
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

	private static bool IsAdobeAnimateResourceReferencingDat(string ownerPath, string datPath)
	{
		if (!TryResolveAdobeAnimateDatPath(ownerPath, out var datPath2))
		{
			return false;
		}
		return string.Equals(NormalizePath(datPath2), NormalizePath(datPath), StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsAdobeAnimateResource(string path)
	{
		path = NormalizePath(path);
		if (path.GetExtension().ToLowerInvariant() != "tres" || !FileAccess.FileExists(path))
		{
			return false;
		}
		using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return false;
		}
		for (int i = 0; i < 32; i++)
		{
			if (fileAccess.EofReached())
			{
				break;
			}
			string line = fileAccess.GetLine();
			if (line.Contains("script_class=\"AdobeAnimateData\"", StringComparison.Ordinal) || line.Contains("AdobeAnimateData.cs", StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}
}
