using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWDotNetToolchainLocator
{
	public sealed class ToolchainInfo
	{
		public string DotnetPath { get; set; } = "";

		public string RootPath { get; set; } = "";

		public string Source { get; set; } = "";

		public string Version { get; set; } = "";

		public bool Exists
		{
			get
			{
				if (!string.IsNullOrEmpty(DotnetPath))
				{
					return File.Exists(DotnetPath);
				}
				return false;
			}
		}

		public override string ToString()
		{
			if (!Exists)
			{
				return "Missing .NET toolchain";
			}
			return $"{Source}: {DotnetPath} {Version}".TrimEnd();
		}
	}

	private const string BundledRelativePath = "BuildTools/DotNet/win-x64";

	private const string DevelopmentGodotDotNetPath = "E:\\Godot\\4.7Csharp";

	public static ToolchainInfo Locate()
	{
		ToolchainInfo toolchainInfo = LocateBundled();
		if (toolchainInfo.Exists)
		{
			return toolchainInfo;
		}
		foreach (var item in EnumerateCandidates())
		{
			if (File.Exists(item.dotnetPath))
			{
				return new ToolchainInfo
				{
					DotnetPath = item.dotnetPath,
					RootPath = item.rootPath,
					Source = item.source,
					Version = ReadVersion(item.dotnetPath)
				};
			}
		}
		return new ToolchainInfo();
	}

	public static ToolchainInfo LocateBundled()
	{
		string path = (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
		string bundledToolchainRoot = GetBundledToolchainRoot();
		string dotnetPath = Path.Combine(bundledToolchainRoot, path);
		ToolchainInfo toolchainInfo = new ToolchainInfo
		{
			DotnetPath = dotnetPath,
			RootPath = bundledToolchainRoot,
			Source = "Bundled"
		};
		if (toolchainInfo.Exists)
		{
			toolchainInfo.Version = ReadVersion(dotnetPath);
		}
		return toolchainInfo;
	}

	public static string GetExecutableDirectory()
	{
		string executablePath = OS.GetExecutablePath();
		if (!string.IsNullOrWhiteSpace(executablePath))
		{
			string directoryName = Path.GetDirectoryName(executablePath);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				return directoryName;
			}
		}
		string baseDirectory = AppContext.BaseDirectory;
		if (!string.IsNullOrWhiteSpace(baseDirectory))
		{
			return baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		return ProjectSettings.GlobalizePath("res://");
	}

	public static string GetBundledToolchainRoot()
	{
		return Path.GetFullPath(Path.Combine(GetExecutableDirectory(), "BuildTools/DotNet/win-x64"));
	}

	private static IEnumerable<(string dotnetPath, string rootPath, string source)> EnumerateCandidates()
	{
		string exeName = (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
		string bundledToolchainRoot = GetBundledToolchainRoot();
		yield return (dotnetPath: Path.Combine(bundledToolchainRoot, exeName), rootPath: bundledToolchainRoot, source: "Bundled");
		foreach (string devRoot in EnumerateDevelopmentRoots())
		{
			yield return (dotnetPath: Path.Combine(devRoot, "dotnet", exeName), rootPath: devRoot, source: "Development");
			yield return (dotnetPath: Path.Combine(devRoot, exeName), rootPath: devRoot, source: "Development");
		}
		string text = System.Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			yield return (dotnetPath: Path.Combine(text, exeName), rootPath: text, source: "DOTNET_ROOT");
		}
		string folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
		if (!string.IsNullOrWhiteSpace(folderPath))
		{
			yield return (dotnetPath: Path.Combine(folderPath, "dotnet", exeName), rootPath: Path.Combine(folderPath, "dotnet"), source: "System");
		}
		string folderPath2 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86);
		if (!string.IsNullOrWhiteSpace(folderPath2))
		{
			yield return (dotnetPath: Path.Combine(folderPath2, "dotnet", exeName), rootPath: Path.Combine(folderPath2, "dotnet"), source: "System");
		}
	}

	private static IEnumerable<string> EnumerateDevelopmentRoots()
	{
		yield return "E:\\Godot\\4.7Csharp";
		string text = ProjectSettings.GetSetting("mod_editor/dotnet/development_path", "").AsString();
		if (!string.IsNullOrWhiteSpace(text) && text != "E:\\Godot\\4.7Csharp")
		{
			yield return text;
		}
	}

	private static string ReadVersion(string dotnetPath)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = dotnetPath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				ArgumentList = { "--version" }
			});
			if (process == null)
			{
				return "";
			}
			string result = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit(3000);
			return result;
		}
		catch
		{
			return "";
		}
	}
}
