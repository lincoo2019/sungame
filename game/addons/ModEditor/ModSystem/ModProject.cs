using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.ModSystem;

public class ModProject
{
	public sealed class ExportResult
	{
		public bool Success { get; set; }

		public bool CompilationAttempted { get; set; }

		public bool ContainsRuntimeAssembly { get; set; }

		public string OutputPath { get; set; } = "";

		public string ErrorMessage { get; set; } = "";

		public XWScriptCompiler.CompileResult CompileResult { get; set; }

		public List<XWValidationIssue> Issues { get; set; } = new List<XWValidationIssue>();
	}

	public const string ProjectFileExtension = ".pvzmodeproject";

	public static readonly string[] SubDirectories = new string[7] { "Scenes", "Scripts", "Resources", "Assets", "Battle", "Localization", "Blueprints" };

	public string Name { get; set; } = "未命名 Mod";

	public string Version { get; set; } = "1.0.0";

	public string Author { get; set; } = "未知";

	public string Description { get; set; } = "";

	public string ExportDirectory { get; set; } = "";

	public string GameDirectory { get; set; } = "";

	public DateTime CreatedDate { get; set; } = DateTime.Now;

	public DateTime LastModifiedDate { get; set; } = DateTime.Now;

	[JsonIgnore]
	public string ProjectPath { get; set; } = "";

	[JsonIgnore]
	public string ProjectFilePath => Path.Combine(ProjectPath, SanitizeDirectoryName(Name) + ".pvzmodeproject");

	public static ModProject Create(string parentDir, string name, string version, string author, string description)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			GD.PrintErr("[ModProject] 工程名称不能为空");
			return null;
		}
		string text = SanitizeDirectoryName(name);
		string text2 = Path.Combine(parentDir, text);
		if (Directory.Exists(text2) && File.Exists(Path.Combine(text2, text + ".pvzmodeproject")))
		{
			GD.PrintErr("[ModProject] 工程已存在: " + text2);
			return null;
		}
		XWModProjectLayout.EnsureProjectLayout(text2);
		XWModManifestSyncService.SyncProject(text2);
		ModProject modProject = new ModProject
		{
			Name = name,
			Version = (string.IsNullOrWhiteSpace(version) ? "1.0.0" : version),
			Author = (string.IsNullOrWhiteSpace(author) ? "未知" : author),
			Description = (description ?? ""),
			ExportDirectory = ProjectSettings.GlobalizePath("user://Mods/"),
			ProjectPath = text2,
			CreatedDate = DateTime.Now,
			LastModifiedDate = DateTime.Now
		};
		modProject.Save();
		GD.Print("[ModProject] 已创建工程: " + modProject.ProjectFilePath);
		return modProject;
	}

	public static ModProject Load(string projectFilePath)
	{
		if (!File.Exists(projectFilePath))
		{
			GD.PrintErr("[ModProject] 工程文件不存在: " + projectFilePath);
			return null;
		}
		try
		{
			ModProject modProject = JsonSerializer.Deserialize(File.ReadAllText(projectFilePath), XWModJsonContext.Default.ModProject);
			if (modProject == null)
			{
				return null;
			}
			modProject.ProjectPath = Path.GetDirectoryName(projectFilePath);
			new XWModMigrationService().MigrateProject(modProject.ProjectPath);
			XWModProjectLayout.EnsureProjectLayout(modProject.ProjectPath);
			XWModManifestSyncService.SyncProject(modProject.ProjectPath);
			return modProject;
		}
		catch (Exception ex)
		{
			GD.PrintErr("[ModProject] 加载工程失败: " + projectFilePath + "\n" + ex.Message);
			return null;
		}
	}

	public void Save()
	{
		if (string.IsNullOrEmpty(ProjectPath))
		{
			GD.PrintErr("[ModProject] 工程路径未设置，无法保存");
			return;
		}
		LastModifiedDate = DateTime.Now;
		XWModProjectLayout.EnsureProjectLayout(ProjectPath);
		XWModManifestSyncService.SyncProject(ProjectPath);
		string contents = JsonSerializer.Serialize(this, XWModJsonContext.Default.ModProject);
		File.WriteAllText(ProjectFilePath, contents);
		GD.Print("[ModProject] 已保存工程: " + ProjectFilePath);
	}

	public List<string> CollectResourceFiles()
	{
		return new XWModExportSnapshot(ProjectPath).Files;
	}

	public string Export(string outputDir = "")
	{
		ExportResult result = ExportCoreAsync(outputDir, default(CancellationToken), backgroundSnapshot: false).GetAwaiter().GetResult();
		if (!result.Success)
		{
			GD.PrintErr("[ModProject] 导出失败：" + result.ErrorMessage);
		}
		return result.OutputPath;
	}

	public Task<ExportResult> ExportAsync(string outputDir = "", CancellationToken cancellationToken = default(CancellationToken))
	{
		return ExportCoreAsync(outputDir, cancellationToken, backgroundSnapshot: true);
	}

	private async Task<ExportResult> ExportCoreAsync(string outputDir, CancellationToken cancellationToken, bool backgroundSnapshot)
	{
		string projectRoot;
		try
		{
			projectRoot = (string.IsNullOrWhiteSpace(ProjectPath) ? "" : Path.GetFullPath(ProjectPath));
		}
		catch (Exception ex)
		{
			return Failure("Mod 工程路径无效，请重新打开工程：" + ex.Message);
		}
		if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
		{
			return Failure("找不到 Mod 工程目录，请重新打开工程后再导出。");
		}
		XWModExportSnapshot snapshot;
		List<XWValidationIssue> issues;
		try
		{
			XWModExportSnapshot xWModExportSnapshot = ((!backgroundSnapshot) ? new XWModExportSnapshot(projectRoot, cancellationToken) : (await Task.Run(() => new XWModExportSnapshot(projectRoot, cancellationToken), cancellationToken)));
			snapshot = xWModExportSnapshot;
			cancellationToken.ThrowIfCancellationRequested();
			issues = new XWModValidationService().ValidateExport(snapshot);
			if (issues.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error))
			{
				ExportResult exportResult = Failure("发布检查未通过：" + string.Join("；", (from issue in issues
					where issue.Level == XWValidationIssue.Severity.Error
					select issue.Message).Take(5)));
				exportResult.Issues = issues;
				return exportResult;
			}
		}
		catch (OperationCanceledException)
		{
			return Failure("Mod 导出已取消，没有生成或覆盖安装包。");
		}
		catch (Exception ex3)
		{
			return Failure("发布检查失败：" + ex3.Message);
		}
		string resolvedOutputDirectory;
		try
		{
			resolvedOutputDirectory = ResolveExportDirectory(outputDir);
		}
		catch (Exception ex4)
		{
			return Failure("导出目录无效，请重新选择可写目录：" + ex4.Message);
		}
		List<string> scriptFiles;
		try
		{
			scriptFiles = await Task.Run(() => XWInGameDotNetBuildService.ResolveModScriptFiles(projectRoot, snapshot.Manifest), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			return Failure("Mod 导出已取消，没有生成或覆盖安装包。");
		}
		catch (Exception ex6)
		{
			return Failure("读取 Mod 脚本清单失败，请检查 mod.json：" + ex6.Message);
		}
		XWScriptCompiler.CompileResult compileResult = null;
		string runtimeAssemblyPath = "";
		if (scriptFiles.Count > 0)
		{
			try
			{
				compileResult = await XWScriptCompiler.CompileModProjectSnapshotAsync(projectRoot, snapshot.Manifest, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				return Failure("Mod C# 编译已取消，没有生成或覆盖安装包。", compilationAttempted: true);
			}
			catch (Exception ex8)
			{
				return Failure("启动 Mod C# 编译失败，请确认已安装可用的 .NET SDK：" + ex8.Message, compilationAttempted: true);
			}
			XWScriptCompiler.CompileResult compileResult2 = compileResult;
			if (compileResult2 == null || !compileResult2.Success)
			{
				return Failure(BuildCompileFailureMessage(compileResult), compilationAttempted: true, compileResult);
			}
			runtimeAssemblyPath = compileResult.OutputAssemblyPath ?? "";
			if (string.IsNullOrWhiteSpace(runtimeAssemblyPath) || !File.Exists(runtimeAssemblyPath) || !Path.GetExtension(runtimeAssemblyPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
			{
				return Failure("Mod C# 显示编译成功，但没有找到输出 DLL。请清理工程的 .build 目录后重新导出。", compilationAttempted: true, compileResult);
			}
		}
		ModExporter.ModInfo info = new ModExporter.ModInfo
		{
			Name = Name,
			Version = Version,
			Author = Author,
			Description = Description
		};
		try
		{
			string text = await Task.Run(delegate
			{
				cancellationToken.ThrowIfCancellationRequested();
				snapshot.VerifyUnchanged();
				List<string> files = snapshot.Files;
				return ModExporter.ExportFromDirectory(Name, info, projectRoot, files, resolvedOutputDirectory, runtimeAssemblyPath, cancellationToken, snapshot.Manifest, snapshot);
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new ExportResult
			{
				Success = (!string.IsNullOrWhiteSpace(text) && File.Exists(text)),
				CompilationAttempted = (scriptFiles.Count > 0),
				ContainsRuntimeAssembly = !string.IsNullOrWhiteSpace(runtimeAssemblyPath),
				OutputPath = (text ?? ""),
				CompileResult = compileResult,
				Issues = issues,
				ErrorMessage = ((string.IsNullOrWhiteSpace(text) || !File.Exists(text)) ? "导出流程结束，但没有找到 .pmod 文件。请检查导出目录权限。" : "")
			};
		}
		catch (OperationCanceledException)
		{
			return Failure("Mod 打包已取消，没有生成或覆盖安装包。", scriptFiles.Count > 0, compileResult);
		}
		catch (Exception ex10)
		{
			return Failure("Mod 打包失败，没有覆盖已有安装包。请确认导出目录可写且文件未被占用：" + ex10.Message, scriptFiles.Count > 0, compileResult);
		}
	}

	private string ResolveExportDirectory(string outputDir)
	{
		string text = outputDir;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (string.IsNullOrWhiteSpace(ExportDirectory) ? ProjectSettings.GlobalizePath("user://Mods/") : ExportDirectory);
		}
		if (text.StartsWith("user://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
		{
			text = ProjectSettings.GlobalizePath(text);
		}
		return Path.GetFullPath(text);
	}

	private static List<string> ResolveCSharpScripts(string projectRoot, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		XWModManifestSyncService.SyncProject(projectRoot);
		cancellationToken.ThrowIfCancellationRequested();
		XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
		if (xWModManifest == null)
		{
			return new List<string>();
		}
		return XWInGameDotNetBuildService.ResolveModScriptFiles(projectRoot, xWModManifest);
	}

	private static ExportResult Failure(string message, bool compilationAttempted = false, XWScriptCompiler.CompileResult compileResult = null)
	{
		return new ExportResult
		{
			Success = false,
			CompilationAttempted = compilationAttempted,
			ContainsRuntimeAssembly = false,
			OutputPath = "",
			ErrorMessage = (message ?? "Mod 导出失败。"),
			CompileResult = compileResult
		};
	}

	private static string BuildCompileFailureMessage(XWScriptCompiler.CompileResult compileResult)
	{
		if (compileResult == null)
		{
			return "Mod C# 编译没有返回结果。请确认 .NET SDK 可用后重试。";
		}
		if (compileResult.Cancelled)
		{
			return "Mod C# 编译已取消，没有生成安装包。";
		}
		if (compileResult.TimedOut)
		{
			return "Mod C# 编译超时。请关闭占用 .build 的进程，清理 .build 目录后重试。";
		}
		if (!compileResult.ProcessTreeTerminated)
		{
			return "Mod C# 编译进程未能正常结束。请结束残留 dotnet 进程后重试。";
		}
		XWCodeErrorChecker.ErrorData errorData = null;
		foreach (XWCodeErrorChecker.ErrorData item in compileResult.Diagnostics ?? new List<XWCodeErrorChecker.ErrorData>())
		{
			if (errorData == null)
			{
				errorData = item;
			}
			if (item != null && item.SeverityLevel == XWCodeErrorChecker.Severity.Error)
			{
				errorData = item;
				break;
			}
		}
		if (errorData != null)
		{
			string value = (string.IsNullOrWhiteSpace(errorData.FilePath) ? "未知脚本" : Path.GetFileName(errorData.FilePath));
			string value2 = ((errorData.Line >= 0) ? $" 第 {errorData.Line + 1} 行" : "");
			return $"Mod C# 编译失败：{value}{value2}：{errorData.Message}" + "。请在脚本编辑器的错误列表中修复后重新导出。";
		}
		return $"Mod C# 编译失败（退出码 {compileResult.ExitCode}）。" + "请打开脚本编辑器查看完整编译输出，修复后重新导出。";
	}

	private static string SanitizeDirectoryName(string name)
	{
		char[] invalidPathChars = Path.GetInvalidPathChars();
		StringBuilder stringBuilder = new StringBuilder(name.Length);
		foreach (char c in name)
		{
			if (Array.IndexOf(invalidPathChars, c) < 0 && c != '/' && c != '\\')
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString().Trim();
	}
}
