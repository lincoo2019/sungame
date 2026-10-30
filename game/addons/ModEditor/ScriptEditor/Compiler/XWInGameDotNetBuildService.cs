using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ScriptEditor;

public sealed class XWInGameDotNetBuildService
{
	public sealed class BuildResult
	{
		public bool Success { get; set; }

		public bool IsModBuild { get; set; }

		public bool Cancelled { get; set; }

		public bool TimedOut { get; set; }

		public bool ProcessTreeTerminated { get; set; } = true;

		public int ExitCode { get; set; }

		public double ElapsedSeconds { get; set; }

		public int ScriptCount { get; set; }

		public string TargetName { get; set; } = "";

		public string ProjectPath { get; set; } = "";

		public string Output { get; set; } = "";

		public string BuildDirectory { get; set; } = "";

		public string OutputAssemblyPath { get; set; } = "";

		public XWDotNetToolchainLocator.ToolchainInfo Toolchain { get; set; } = new XWDotNetToolchainLocator.ToolchainInfo();

		public List<XWCodeErrorChecker.ErrorData> Diagnostics { get; set; } = new List<XWCodeErrorChecker.ErrorData>();
	}

	private static readonly Regex DiagnosticRegex = new Regex("^(?<file>.+?)\\((?<line>\\d+),(?<column>\\d+)\\):\\s*(?<severity>error|warning)\\s+(?<code>[A-Z]+\\d+):\\s*(?<message>.*?)(?:\\s+\\[[^\\]]+\\])?$", RegexOptions.Compiled);

	public static readonly TimeSpan DefaultBuildTimeout = TimeSpan.FromMinutes(2L);

	public TimeSpan BuildTimeout { get; }

	public XWInGameDotNetBuildService()
		: this(DefaultBuildTimeout)
	{
	}

	public XWInGameDotNetBuildService(TimeSpan buildTimeout)
	{
		if (buildTimeout != Timeout.InfiniteTimeSpan && buildTimeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("buildTimeout");
		}
		BuildTimeout = buildTimeout;
	}

	public Task<BuildResult> BuildExistingProjectAsync(string projectDir, string csprojPath)
	{
		return BuildExistingProjectAsync(projectDir, csprojPath, CancellationToken.None);
	}

	public Task<BuildResult> BuildExistingProjectAsync(string projectDir, string csprojPath, CancellationToken cancellationToken)
	{
		return Task.Run(() => BuildExistingProjectCoreAsync(projectDir, csprojPath, cancellationToken));
	}

	public BuildResult BuildExistingProject(string projectDir, string csprojPath)
	{
		return BuildExistingProjectAsync(projectDir, csprojPath).GetAwaiter().GetResult();
	}

	private async Task<BuildResult> BuildExistingProjectCoreAsync(string projectDir, string csprojPath, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return BuildLifecycleResult(csprojPath, "", null, cancelled: true, timedOut: false, processTreeTerminated: true, "");
		}
		XWDotNetToolchainLocator.ToolchainInfo toolchainInfo = XWDotNetToolchainLocator.Locate();
		if (!toolchainInfo.Exists)
		{
			return MissingToolchainResult(csprojPath, toolchainInfo);
		}
		if (string.IsNullOrWhiteSpace(csprojPath) || !File.Exists(csprojPath))
		{
			return MissingProjectResult(csprojPath, toolchainInfo);
		}
		string text = Path.Combine(projectDir, ".build");
		Directory.CreateDirectory(text);
		return await RunDotnetBuildAsync(toolchainInfo, projectDir, csprojPath, text, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<BuildResult> CreateModProjectAsync(string modId, string modRoot, IEnumerable<string> scriptFiles)
	{
		return CreateModProjectAsync(modId, modRoot, scriptFiles, CancellationToken.None);
	}

	public Task<BuildResult> CreateModProjectAsync(string modId, string modRoot, IEnumerable<string> scriptFiles, CancellationToken cancellationToken)
	{
		return Task.Run(() => CreateModProjectCoreAsync(modId, modRoot, scriptFiles, cancellationToken));
	}

	public Task<BuildResult> BuildModProjectAsync(string modRoot)
	{
		return BuildModProjectAsync(modRoot, CancellationToken.None);
	}

	public Task<BuildResult> BuildModProjectAsync(string modRoot, CancellationToken cancellationToken)
	{
		return Task.Run(() => BuildModProjectCoreAsync(modRoot, cancellationToken));
	}

	public BuildResult BuildModProject(string modRoot)
	{
		return BuildModProjectAsync(modRoot).GetAwaiter().GetResult();
	}

	private async Task<BuildResult> BuildModProjectCoreAsync(string modRoot, CancellationToken cancellationToken, XWModManifest snapshot = null)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return BuildLifecycleResult(modRoot, "", null, cancelled: true, timedOut: false, processTreeTerminated: true, "");
		}
		string text;
		try
		{
			text = (string.IsNullOrWhiteSpace(modRoot) ? "" : Path.GetFullPath(modRoot));
		}
		catch (Exception ex)
		{
			return InvalidModProjectResult(modRoot, "INVALID_MOD_ROOT: " + ex.Message);
		}
		if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
		{
			return InvalidModProjectResult(text, "INVALID_MOD_ROOT: Mod project directory was not found.");
		}
		if (snapshot == null)
		{
			XWModManifestSyncService.SyncProject(text);
		}
		string manifestPath = Path.Combine(text, "mod.json");
		XWModManifest xWModManifest;
		try
		{
			xWModManifest = snapshot ?? XWModManifest.Load(manifestPath);
		}
		catch (Exception ex2)
		{
			return InvalidModProjectResult(manifestPath, "INVALID_MOD_MANIFEST: " + ex2.Message);
		}
		if (xWModManifest == null)
		{
			return InvalidModProjectResult(manifestPath, "INVALID_MOD_MANIFEST: mod.json was not found or could not be loaded.");
		}
		List<string> list = ResolveModScriptFiles(text, xWModManifest);
		if (list.Count == 0)
		{
			BuildResult buildResult = new BuildResult();
			buildResult.Success = false;
			buildResult.IsModBuild = true;
			buildResult.ExitCode = -1;
			buildResult.TargetName = FirstNonEmpty(xWModManifest.Id, xWModManifest.Name, Path.GetFileName(text));
			buildResult.ProjectPath = manifestPath;
			buildResult.BuildDirectory = Path.Combine(text, ".build");
			buildResult.Output = "NO_MOD_SCRIPTS: no C# scripts were found in the active Mod project.";
			buildResult.Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					FilePath = manifestPath,
					Line = 0,
					Column = 0,
					Message = "NO_MOD_SCRIPTS: no C# scripts were found in the active Mod project.",
					SeverityLevel = XWCodeErrorChecker.Severity.Warning,
					Code = "NO_MOD_SCRIPTS"
				}
			};
			return buildResult;
		}
		if (xWModManifest.RuntimeApiVersion != 0 && xWModManifest.RuntimeApiVersion != 1)
		{
			return InvalidModProjectResult(manifestPath, $"INCOMPATIBLE_MOD_API: runtimeApiVersion={xWModManifest.RuntimeApiVersion}, Host={1}");
		}
		BuildResult buildResult2 = await CreateModProjectCoreAsync(FirstNonEmpty(xWModManifest.Id, xWModManifest.Name, Path.GetFileName(text)), text, list, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (buildResult2.Success && snapshot == null)
		{
			try
			{
				XWModManifest xWModManifest2 = XWModManifest.Load(manifestPath);
				if (xWModManifest2 == null || (xWModManifest2.RuntimeApiVersion != 0 && xWModManifest2.RuntimeApiVersion != 1))
				{
					return InvalidModProjectResult(manifestPath, "MOD_MANIFEST_CHANGED: 编译期间兼容声明发生变化，请检查后重试。");
				}
				xWModManifest2.SchemaVersion = 2;
				xWModManifest2.RuntimeApiVersion = 1;
				if (string.IsNullOrWhiteSpace(xWModManifest2.RuntimeAssemblyPolicy))
				{
					xWModManifest2.RuntimeAssemblyPolicy = "required";
				}
				xWModManifest2.Save(manifestPath);
			}
			catch (Exception ex3)
			{
				return InvalidModProjectResult(manifestPath, "MOD_METADATA_SAVE_FAILED: " + ex3.Message);
			}
		}
		return buildResult2;
	}

	public Task<BuildResult> BuildModProjectSnapshotAsync(string root, XWModManifest snapshot, CancellationToken cancellationToken)
	{
		return Task.Run(() => BuildModProjectCoreAsync(root, cancellationToken, snapshot), cancellationToken);
	}

	public BuildResult CreateModProject(string modId, string modRoot, IEnumerable<string> scriptFiles)
	{
		return CreateModProjectAsync(modId, modRoot, scriptFiles).GetAwaiter().GetResult();
	}

	private async Task<BuildResult> CreateModProjectCoreAsync(string modId, string modRoot, IEnumerable<string> scriptFiles, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return BuildLifecycleResult(modRoot, "", null, cancelled: true, timedOut: false, processTreeTerminated: true, "");
		}
		XWDotNetToolchainLocator.ToolchainInfo toolchainInfo = XWDotNetToolchainLocator.Locate();
		if (!toolchainInfo.Exists)
		{
			BuildResult buildResult = MissingToolchainResult("", toolchainInfo);
			buildResult.IsModBuild = true;
			buildResult.TargetName = SanitizeProjectName(string.IsNullOrWhiteSpace(modId) ? "ModScript" : modId);
			return buildResult;
		}
		string safeId = SanitizeProjectName(string.IsNullOrWhiteSpace(modId) ? "ModScript" : modId);
		string buildDirectory = Path.Combine(modRoot, ".build");
		Directory.CreateDirectory(buildDirectory);
		List<string> resolvedScripts = NormalizeExplicitScriptFiles(modRoot, scriptFiles);
		if (resolvedScripts.Count == 0)
		{
			return InvalidModProjectResult(modRoot, "NO_MOD_SCRIPTS: no safe C# script paths were supplied.");
		}
		string text = Path.Combine(buildDirectory, "src");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, safeId + ".csproj");
		File.WriteAllText(text2, BuildModCsproj(resolvedScripts), Encoding.UTF8);
		BuildResult obj = await RunDotnetBuildAsync(toolchainInfo, text, text2, buildDirectory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		obj.IsModBuild = true;
		obj.ScriptCount = resolvedScripts.Count;
		obj.TargetName = safeId;
		obj.OutputAssemblyPath = Path.Combine(buildDirectory, "bin", safeId + ".dll");
		return obj;
	}

	public static List<string> ResolveModScriptFiles(string projectRoot, XWModManifest manifest)
	{
		List<string> list = new List<string>();
		if (manifest == null || string.IsNullOrWhiteSpace(projectRoot))
		{
			return list;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(projectRoot);
		}
		catch
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in manifest.Scripts ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(item) && item.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				string fullPath2;
				try
				{
					fullPath2 = Path.GetFullPath(Path.Combine(fullPath, item.Replace('/', Path.DirectorySeparatorChar)));
				}
				catch
				{
					continue;
				}
				if (IsPathInsideRoot(fullPath2, fullPath) && !IsBuildArtifactPath(fullPath2, fullPath) && File.Exists(fullPath2) && hashSet.Add(fullPath2))
				{
					list.Add(fullPath2);
				}
			}
		}
		return list;
	}

	public static List<XWCodeErrorChecker.ErrorData> ParseDiagnostics(string output)
	{
		List<XWCodeErrorChecker.ErrorData> list = new List<XWCodeErrorChecker.ErrorData>();
		string[] array = output.Replace("\r\n", "\n").Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length != 0)
			{
				Match match = DiagnosticRegex.Match(text);
				if (match.Success)
				{
					int result;
					int line = (int.TryParse(match.Groups["line"].Value, out result) ? Math.Max(0, result - 1) : 0);
					int result2;
					int column = (int.TryParse(match.Groups["column"].Value, out result2) ? Math.Max(0, result2 - 1) : 0);
					string value = match.Groups["severity"].Value;
					string value2 = match.Groups["code"].Value;
					string text2 = match.Groups["message"].Value.Trim();
					list.Add(new XWCodeErrorChecker.ErrorData
					{
						FilePath = match.Groups["file"].Value.Trim(),
						Line = line,
						Column = column,
						Message = value2 + ": " + text2,
						SeverityLevel = ((value == "warning") ? XWCodeErrorChecker.Severity.Warning : XWCodeErrorChecker.Severity.Error),
						Code = value2
					});
				}
			}
		}
		return list;
	}

	private async Task<BuildResult> RunDotnetBuildAsync(XWDotNetToolchainLocator.ToolchainInfo toolchain, string projectDir, string csprojPath, string buildDirectory, CancellationToken cancellationToken)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			XWBuildProcessRunner.ProcessRunResult processRunResult = await XWBuildProcessRunner.RunAsync(new ProcessStartInfo
			{
				FileName = toolchain.DotnetPath,
				WorkingDirectory = projectDir,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				ArgumentList = 
				{
					"build",
					csprojPath,
					"--nologo",
					"-v:minimal",
					"-o",
					Path.Combine(buildDirectory, "bin"),
					"/p:UseSharedCompilation=false",
					"/nodeReuse:false",
					"/p:PublishTrimmed=false",
					"/p:PublishAot=false"
				}
			}, BuildTimeout, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			stopwatch.Stop();
			string text = JoinOutput(processRunResult.StandardOutput, processRunResult.StandardError);
			if (processRunResult.Cancelled || processRunResult.TimedOut || !processRunResult.ProcessTreeTerminated)
			{
				return BuildLifecycleResult(csprojPath, buildDirectory, toolchain, processRunResult.Cancelled, processRunResult.TimedOut, processRunResult.ProcessTreeTerminated, text, stopwatch.Elapsed.TotalSeconds);
			}
			List<XWCodeErrorChecker.ErrorData> list = ParseDiagnostics(text);
			if (processRunResult.ExitCode != 0 && list.Count == 0)
			{
				list.Add(new XWCodeErrorChecker.ErrorData
				{
					Line = 0,
					Column = 0,
					Message = $"BUILD_FAILED: dotnet build exited with code {processRunResult.ExitCode}.",
					SeverityLevel = XWCodeErrorChecker.Severity.Error,
					Code = "BUILD_FAILED"
				});
			}
			return new BuildResult
			{
				Success = (processRunResult.ExitCode == 0),
				ExitCode = processRunResult.ExitCode,
				TargetName = Path.GetFileNameWithoutExtension(csprojPath),
				ProjectPath = csprojPath,
				BuildDirectory = buildDirectory,
				Toolchain = toolchain,
				ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
				Output = text,
				Diagnostics = list
			};
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			return new BuildResult
			{
				Success = false,
				ExitCode = -1,
				ProjectPath = csprojPath,
				BuildDirectory = buildDirectory,
				Toolchain = toolchain,
				ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
				Output = ex.ToString(),
				Diagnostics = new List<XWCodeErrorChecker.ErrorData>
				{
					new XWCodeErrorChecker.ErrorData
					{
						Line = 0,
						Column = 0,
						Message = "BUILD_START_FAILED: " + ex.Message,
						SeverityLevel = XWCodeErrorChecker.Severity.Error,
						Code = "BUILD_START_FAILED"
					}
				}
			};
		}
	}

	internal static BuildResult BuildLifecycleResult(string projectPath, string buildDirectory, XWDotNetToolchainLocator.ToolchainInfo toolchain, bool cancelled, bool timedOut, bool processTreeTerminated, string capturedOutput, double elapsedSeconds = 0.0)
	{
		string code;
		string text;
		XWCodeErrorChecker.Severity severityLevel;
		if (!processTreeTerminated)
		{
			code = "BUILD_TERMINATION_FAILED";
			text = "BUILD_TERMINATION_FAILED: the build process tree did not exit within the cleanup deadline.";
			severityLevel = XWCodeErrorChecker.Severity.Error;
		}
		else if (cancelled)
		{
			code = "BUILD_CANCELLED";
			text = "BUILD_CANCELLED: the background build was cancelled.";
			severityLevel = XWCodeErrorChecker.Severity.Warning;
		}
		else
		{
			code = "BUILD_TIMEOUT";
			text = "BUILD_TIMEOUT: the background build exceeded its configured timeout.";
			severityLevel = XWCodeErrorChecker.Severity.Error;
		}
		return new BuildResult
		{
			Success = false,
			Cancelled = cancelled,
			TimedOut = timedOut,
			ProcessTreeTerminated = processTreeTerminated,
			ExitCode = (cancelled ? (-2) : (timedOut ? (-3) : (-4))),
			ProjectPath = (projectPath ?? ""),
			BuildDirectory = (buildDirectory ?? ""),
			Toolchain = (toolchain ?? new XWDotNetToolchainLocator.ToolchainInfo()),
			ElapsedSeconds = elapsedSeconds,
			Output = JoinOutput(capturedOutput, text),
			Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					FilePath = (projectPath ?? ""),
					Line = 0,
					Column = 0,
					Message = text,
					SeverityLevel = severityLevel,
					Code = code
				}
			}
		};
	}

	private static string JoinOutput(string first, string second)
	{
		if (string.IsNullOrWhiteSpace(first))
		{
			return second ?? "";
		}
		if (string.IsNullOrWhiteSpace(second))
		{
			return first ?? "";
		}
		return first.TrimEnd() + System.Environment.NewLine + second.TrimStart();
	}

	private static BuildResult MissingToolchainResult(string projectPath, XWDotNetToolchainLocator.ToolchainInfo toolchain)
	{
		return new BuildResult
		{
			Success = false,
			ExitCode = -1,
			ProjectPath = projectPath,
			Toolchain = toolchain,
			Output = "Missing bundled .NET toolchain.",
			Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					Line = 0,
					Column = 0,
					Message = "DOTNET_TOOLCHAIN_MISSING: missing BuildTools/DotNet/win-x64 dotnet toolchain.",
					SeverityLevel = XWCodeErrorChecker.Severity.Error,
					Code = "DOTNET_TOOLCHAIN_MISSING"
				}
			}
		};
	}

	private static BuildResult InvalidModProjectResult(string projectPath, string message)
	{
		string text = (message.StartsWith("NO_MOD_SCRIPTS", StringComparison.Ordinal) ? "NO_MOD_SCRIPTS" : (message.StartsWith("INVALID_MOD_MANIFEST", StringComparison.Ordinal) ? "INVALID_MOD_MANIFEST" : "INVALID_MOD_ROOT"));
		return new BuildResult
		{
			Success = false,
			IsModBuild = true,
			ExitCode = -1,
			ProjectPath = (projectPath ?? ""),
			Output = message,
			Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					FilePath = (projectPath ?? ""),
					Line = 0,
					Column = 0,
					Message = message,
					SeverityLevel = ((text == "NO_MOD_SCRIPTS") ? XWCodeErrorChecker.Severity.Warning : XWCodeErrorChecker.Severity.Error),
					Code = text
				}
			}
		};
	}

	private static BuildResult MissingProjectResult(string projectPath, XWDotNetToolchainLocator.ToolchainInfo toolchain)
	{
		return new BuildResult
		{
			Success = false,
			ExitCode = -1,
			ProjectPath = projectPath,
			Toolchain = toolchain,
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

	private static string BuildModCsproj(IEnumerable<string> scriptFiles)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
		stringBuilder.AppendLine("  <PropertyGroup>");
		stringBuilder.AppendLine("    <TargetFramework>net9.0</TargetFramework>");
		stringBuilder.AppendLine("    <EnableDynamicLoading>true</EnableDynamicLoading>");
		stringBuilder.AppendLine("    <Nullable>enable</Nullable>");
		stringBuilder.AppendLine("  </PropertyGroup>");
		List<string> list = ResolveReferenceAssemblies();
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("  <ItemGroup>");
			foreach (string item in list)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				stringBuilder.Append("    <Reference Include=\"").Append(EscapeXml(fileNameWithoutExtension)).AppendLine("\">");
				stringBuilder.Append("      <HintPath>").Append(EscapeXml(item)).AppendLine("</HintPath>");
				stringBuilder.AppendLine("      <Private>false</Private>");
				stringBuilder.AppendLine("    </Reference>");
			}
			stringBuilder.AppendLine("  </ItemGroup>");
		}
		stringBuilder.AppendLine("  <ItemGroup>");
		foreach (string scriptFile in scriptFiles)
		{
			if (!string.IsNullOrWhiteSpace(scriptFile))
			{
				stringBuilder.Append("    <Compile Include=\"").Append(EscapeXml(Path.GetFullPath(scriptFile))).AppendLine("\" />");
			}
		}
		stringBuilder.AppendLine("  </ItemGroup>");
		stringBuilder.AppendLine("</Project>");
		return stringBuilder.ToString();
	}

	private static List<string> NormalizeExplicitScriptFiles(string modRoot, IEnumerable<string> scriptFiles)
	{
		List<string> list = new List<string>();
		if (scriptFiles == null || string.IsNullOrWhiteSpace(modRoot))
		{
			return list;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(modRoot);
		}
		catch
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string scriptFile in scriptFiles)
		{
			if (!string.IsNullOrWhiteSpace(scriptFile))
			{
				string fullPath2;
				try
				{
					fullPath2 = Path.GetFullPath(scriptFile);
				}
				catch
				{
					continue;
				}
				if (fullPath2.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && IsPathInsideRoot(fullPath2, fullPath) && !IsBuildArtifactPath(fullPath2, fullPath) && File.Exists(fullPath2) && hashSet.Add(fullPath2))
				{
					list.Add(fullPath2);
				}
			}
		}
		return list;
	}

	private static bool IsPathInsideRoot(string path, string root)
	{
		string value = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return path.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsBuildArtifactPath(string path, string root)
	{
		string value = Path.Combine(root, ".build").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return path.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string text in values)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		return "ModScript";
	}

	public static List<string> ResolveReferenceAssemblies()
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in EnumerateReferenceRoots())
		{
			if (string.IsNullOrWhiteSpace(item) || !Directory.Exists(item))
			{
				continue;
			}
			foreach (string item2 in Directory.EnumerateFiles(item, "*.dll", SearchOption.TopDirectoryOnly))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2);
				if (hashSet.Add(fileNameWithoutExtension))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private static IEnumerable<string> EnumerateReferenceRoots()
	{
		string projectRoot = ProjectSettings.GlobalizePath("res://");
		if (!string.IsNullOrWhiteSpace(projectRoot))
		{
			global::_003C_003Ey__InlineArray6<string> buffer = default(global::_003C_003Ey__InlineArray6<string>);
			buffer[0] = projectRoot;
			buffer[1] = ".godot";
			buffer[2] = "mono";
			buffer[3] = "temp";
			buffer[4] = "bin";
			buffer[5] = "Debug";
			yield return Path.Combine(buffer);
			global::_003C_003Ey__InlineArray6<string> buffer2 = default(global::_003C_003Ey__InlineArray6<string>);
			buffer2[0] = projectRoot;
			buffer2[1] = ".godot";
			buffer2[2] = "mono";
			buffer2[3] = "temp";
			buffer2[4] = "bin";
			buffer2[5] = "Release";
			yield return Path.Combine(buffer2);
		}
		string executableRoot = XWDotNetToolchainLocator.GetExecutableDirectory();
		if (!string.IsNullOrWhiteSpace(executableRoot))
		{
			yield return Path.Combine(executableRoot, "BuildTools", "References");
			yield return Path.Combine(executableRoot, "BuildTools", "References", "Debug");
			yield return Path.Combine(executableRoot, "BuildTools", "References", "Release");
			yield return Path.Combine(executableRoot, "GodotSharp", "Api", "Debug");
			yield return Path.Combine(executableRoot, "GodotSharp", "Api", "Release");
		}
		XWDotNetToolchainLocator.ToolchainInfo toolchain = XWDotNetToolchainLocator.Locate();
		if (!string.IsNullOrWhiteSpace(toolchain.RootPath))
		{
			yield return Path.Combine(toolchain.RootPath, "References");
			yield return Path.Combine(toolchain.RootPath, "GodotSharp", "Api", "Debug");
			yield return Path.Combine(toolchain.RootPath, "GodotSharp", "Api", "Release");
		}
	}

	private static string EscapeXml(string value)
	{
		return SecurityElement.Escape(value) ?? "";
	}

	private static string SanitizeProjectName(string value)
	{
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			stringBuilder.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
		}
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return "ModScript";
	}
}
