using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWScriptCompiler
{
	public sealed class CompileResult
	{
		public bool Success;

		public bool IsModProject;

		public bool Cancelled;

		public bool TimedOut;

		public bool ProcessTreeTerminated = true;

		public int ExitCode;

		public double ElapsedSeconds;

		public int ScriptCount;

		public string TargetName = "";

		public string ProjectPath = "";

		public string BuildDirectory = "";

		public string OutputAssemblyPath = "";

		public string Output = "";

		public List<XWCodeErrorChecker.ErrorData> Diagnostics = new List<XWCodeErrorChecker.ErrorData>();
	}

	public static Task<CompileResult> CompileProjectAsync()
	{
		return CompileProjectAsync(CancellationToken.None);
	}

	public static Task<CompileResult> CompileProjectAsync(CancellationToken cancellationToken)
	{
		string activeModProjectDirectory = GetActiveModProjectDirectory();
		if (!string.IsNullOrWhiteSpace(activeModProjectDirectory))
		{
			return CompileModProjectAsync(activeModProjectDirectory, cancellationToken);
		}
		string projectDirectory = GetProjectDirectory();
		string csprojPath = FindProjectFile(projectDirectory);
		return CompileProjectAsync(projectDirectory, csprojPath, cancellationToken);
	}

	public static Task<CompileResult> CompileModProjectAsync(string modProjectRoot)
	{
		return XWModBuildSingleFlight.RunAsync(modProjectRoot, CompileModProjectCore);
	}

	public static Task<CompileResult> CompileModProjectSnapshotAsync(string root, XWModManifest manifest, CancellationToken cancellationToken)
	{
		return XWModBuildSingleFlight.RunAsync(root, async (string path, CancellationToken token) => FromBuildResult(await new XWInGameDotNetBuildService().BuildModProjectSnapshotAsync(path, manifest, token).ConfigureAwait(continueOnCapturedContext: false)), cancellationToken);
	}

	public static Task<CompileResult> CompileModProjectAsync(string modProjectRoot, CancellationToken cancellationToken)
	{
		return XWModBuildSingleFlight.RunAsync(modProjectRoot, CompileModProjectCore, cancellationToken);
	}

	public static CompileResult CompileProject()
	{
		string activeModProjectDirectory = GetActiveModProjectDirectory();
		if (!string.IsNullOrWhiteSpace(activeModProjectDirectory))
		{
			return CompileModProject(activeModProjectDirectory);
		}
		string projectDirectory = GetProjectDirectory();
		string csprojPath = FindProjectFile(projectDirectory);
		return CompileProject(projectDirectory, csprojPath);
	}

	public static CompileResult CompileModProject(string modProjectRoot)
	{
		return CompileModProjectAsync(modProjectRoot).GetAwaiter().GetResult();
	}

	public static bool IsModBuildBusy(string modProjectRoot)
	{
		return XWModBuildSingleFlight.IsBusy(modProjectRoot);
	}

	public static XWModBuildSingleFlight.BuildFlightMetrics GetModBuildMetrics(string modProjectRoot)
	{
		return XWModBuildSingleFlight.GetMetrics(modProjectRoot);
	}

	private static async Task<CompileResult> CompileModProjectCore(string modProjectRoot, CancellationToken cancellationToken)
	{
		return FromBuildResult(await new XWInGameDotNetBuildService().BuildModProjectAsync(modProjectRoot, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
	}

	private static CompileResult CompileProject(string projectDir, string csprojPath)
	{
		return CompileProjectAsync(projectDir, csprojPath, CancellationToken.None).GetAwaiter().GetResult();
	}

	private static async Task<CompileResult> CompileProjectAsync(string projectDir, string csprojPath, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(csprojPath))
		{
			return new CompileResult
			{
				Success = false,
				ExitCode = -1,
				ProjectPath = "",
				Output = "Missing .csproj file.",
				Diagnostics = new List<XWCodeErrorChecker.ErrorData>
				{
					new XWCodeErrorChecker.ErrorData
					{
						Line = 0,
						Column = 0,
						Message = "NO_CSPROJ: missing C# project file.",
						SeverityLevel = XWCodeErrorChecker.Severity.Error,
						Code = "NO_CSPROJ"
					}
				}
			};
		}
		return FromBuildResult(await new XWInGameDotNetBuildService().BuildExistingProjectAsync(projectDir, csprojPath, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
	}

	private static CompileResult FromBuildResult(XWInGameDotNetBuildService.BuildResult buildResult)
	{
		return new CompileResult
		{
			Success = buildResult.Success,
			IsModProject = buildResult.IsModBuild,
			Cancelled = buildResult.Cancelled,
			TimedOut = buildResult.TimedOut,
			ProcessTreeTerminated = buildResult.ProcessTreeTerminated,
			ExitCode = buildResult.ExitCode,
			ElapsedSeconds = buildResult.ElapsedSeconds,
			ScriptCount = buildResult.ScriptCount,
			TargetName = buildResult.TargetName,
			ProjectPath = buildResult.ProjectPath,
			BuildDirectory = buildResult.BuildDirectory,
			OutputAssemblyPath = buildResult.OutputAssemblyPath,
			Output = buildResult.Output,
			Diagnostics = buildResult.Diagnostics
		};
	}

	public static List<XWCodeErrorChecker.ErrorData> FilterDiagnosticsForFile(IEnumerable<XWCodeErrorChecker.ErrorData> diagnostics, string filePath)
	{
		List<XWCodeErrorChecker.ErrorData> list = new List<XWCodeErrorChecker.ErrorData>();
		string text = NormalizeFilePath(filePath);
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		foreach (XWCodeErrorChecker.ErrorData diagnostic in diagnostics)
		{
			if (NormalizeFilePath(diagnostic.FilePath) == text)
			{
				list.Add(diagnostic);
			}
		}
		return list;
	}

	public static string NormalizeFilePath(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return "";
		}
		string text = filePath;
		if (text.StartsWith("res://"))
		{
			text = ProjectSettings.GlobalizePath(text);
		}
		try
		{
			return Path.GetFullPath(text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
				.ToLowerInvariant();
		}
		catch
		{
			return text.Replace('/', Path.DirectorySeparatorChar).ToLowerInvariant();
		}
	}

	private static string GetProjectDirectory()
	{
		string text = ProjectSettings.GlobalizePath("res://");
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Directory.GetCurrentDirectory();
	}

	private static string GetActiveModProjectDirectory()
	{
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}

	private static string FindProjectFile(string projectDir)
	{
		if (!Directory.Exists(projectDir))
		{
			return "";
		}
		string[] files = Directory.GetFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly);
		if (files.Length == 0)
		{
			return "";
		}
		return files[0];
	}
}
