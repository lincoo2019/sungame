using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorCompiledExportRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _probeRoot = "";

	public override async void _Ready()
	{
		bool background = false;
		bool compiledPackage = false;
		bool editorExport = false;
		bool staticPackage = false;
		bool actionableFailure = false;
		bool atomicFailure = false;
		try
		{
			string path = Guid.NewGuid().ToString("N");
			_probeRoot = Path.Combine(ProjectSettings.GlobalizePath("user://ModEditorCompiledExportProbe/"), path);
			Directory.CreateDirectory(_probeRoot);
			ModProject compiled = CreateProject("Compiled");
			File.WriteAllText(Path.Combine(compiled.ProjectPath, "Scripts", "RuntimeProbe.cs"), "public sealed class RuntimeProbe { public int Value => 42; }");
			XWModManifestSyncService.SyncProject(compiled.ProjectPath);
			string compiledOutput = Path.Combine(_probeRoot, "compiled-output");
			Task<ModProject.ExportResult> compiledTask = compiled.ExportAsync(compiledOutput);
			int liveFrames = await WaitForTask(compiledTask, 3600);
			ModProject.ExportResult exportResult = await compiledTask;
			background = liveFrames > 0;
			compiledPackage = exportResult.Success && exportResult.CompilationAttempted && exportResult.ContainsRuntimeAssembly && (exportResult.CompileResult?.Success ?? false) && File.Exists(exportResult.CompileResult.OutputAssemblyPath) && PackageHasRuntimeAssembly(exportResult.OutputPath, expected: true) && !HasTemporaryPackages(compiledOutput);
			Require(background, "ExportAsync 在 C# 编译期间没有让 Godot 主线程继续处理帧。");
			Require(compiledPackage, "有 C# 的 Mod 导出后没有同时包含 Runtime/ModAssembly.dll 和 runtimeAssembly 清单声明。" + $" success={exportResult.Success}, attempted={exportResult.CompilationAttempted}, " + $"containsAssembly={exportResult.ContainsRuntimeAssembly}; {exportResult.ErrorMessage}\n" + exportResult.CompileResult?.Output);
			ModEditorPanel editorPanel = await OpenModEditor();
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			MethodInfo exportCurrentProject = typeof(ModEditorPanel).GetMethod("ExportCurrentProjectAsync", BindingFlags.Instance | BindingFlags.NonPublic);
			int num;
			if (GodotObject.IsInstanceValid(editorPanel))
			{
				object obj = method?.Invoke(editorPanel, new object[1] { compiled });
				num = ((obj is bool && (bool)obj) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool entered = (byte)num != 0;
			await WaitFrames(10);
			string editorOutput = Path.Combine(_probeRoot, "editor-output");
			Task editorExportTask = (entered ? (exportCurrentProject?.Invoke(editorPanel, new object[1] { editorOutput }) as Task) : null);
			int num2 = ((editorExportTask != null) ? (await WaitForTask(editorExportTask, 3600)) : 0);
			int num3 = num2;
			string packagePath = Path.Combine(editorOutput, compiled.Name + ".pmod");
			editorExport = entered && editorExportTask != null && num3 > 0 && PackageHasRuntimeAssembly(packagePath, expected: true);
			Require(editorExport, "真实 F3 Mod 编辑器的导出入口没有后台编译并写入运行程序集。");
			ModProject resourceOnly = CreateProject("Static");
			File.WriteAllText(Path.Combine(resourceOnly.ProjectPath, "Resources", "StaticProbe.txt"), "resource-only");
			XWModManifestSyncService.SyncProject(resourceOnly.ProjectPath);
			string staticOutput = Path.Combine(_probeRoot, "static-output");
			ModProject.ExportResult exportResult2 = await resourceOnly.ExportAsync(staticOutput);
			staticPackage = exportResult2.Success && !exportResult2.CompilationAttempted && !exportResult2.ContainsRuntimeAssembly && exportResult2.CompileResult == null && PackageHasRuntimeAssembly(exportResult2.OutputPath, expected: false) && !HasTemporaryPackages(staticOutput);
			Require(staticPackage, "纯资源 Mod 被错误地要求编译 C# 或声明了运行程序集。");
			await VerifyAuthoringExperience(resourceOnly, staticOutput);
			ModProject modProject = CreateProject("Broken");
			File.WriteAllText(Path.Combine(modProject.ProjectPath, "Scripts", "Broken.cs"), "public sealed class Broken { public int Value => ; }");
			XWModManifestSyncService.SyncProject(modProject.ProjectPath);
			string brokenOutput = Path.Combine(_probeRoot, "broken-output");
			Directory.CreateDirectory(brokenOutput);
			string existingPackage = Path.Combine(brokenOutput, modProject.Name + ".pmod");
			File.WriteAllText(existingPackage, "existing-package-must-survive");
			ModProject.ExportResult exportResult3 = await modProject.ExportAsync(brokenOutput);
			int num4;
			if (!exportResult3.Success && exportResult3.CompilationAttempted)
			{
				XWScriptCompiler.CompileResult compileResult = exportResult3.CompileResult;
				if (compileResult != null && !compileResult.Success && exportResult3.ErrorMessage.Contains("编译失败", StringComparison.Ordinal))
				{
					num4 = (exportResult3.ErrorMessage.Contains("脚本编辑器", StringComparison.Ordinal) ? 1 : 0);
					goto IL_088c;
				}
			}
			num4 = 0;
			goto IL_088c;
			IL_088c:
			actionableFailure = (byte)num4 != 0;
			atomicFailure = File.Exists(existingPackage) && File.ReadAllText(existingPackage) == "existing-package-must-survive" && !HasTemporaryPackages(brokenOutput);
			Require(actionableFailure, "C# 编译失败没有返回中文、可操作的导出错误。");
			Require(atomicFailure, "失败的导出覆盖了已有安装包或遗留了临时包。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		finally
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(_probeRoot) && Directory.Exists(_probeRoot))
				{
					Directory.Delete(_probeRoot, recursive: true);
				}
			}
			catch (Exception ex2)
			{
				_failures.Add("清理测试工程失败：" + ex2.Message);
			}
		}
		GD.Print($"[MOD_EDITOR_COMPILED_EXPORT_PROBE] background={background} compiledPackage={compiledPackage} editorExport={editorExport} staticPackage={staticPackage} actionableFailure={actionableFailure} atomicFailure={atomicFailure} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_COMPILED_EXPORT_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private ModProject CreateProject(string name)
	{
		return ModProject.Create(_probeRoot, name, "1.0.0", "runtime-probe", "compiled export runtime probe") ?? throw new InvalidOperationException("无法创建临时 Mod 工程：" + name);
	}

	private async Task<int> WaitForTask(Task task, int maximumFrames)
	{
		int frames = 0;
		while (!task.IsCompleted && frames < maximumFrames)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			frames++;
		}
		if (!task.IsCompleted)
		{
			throw new TimeoutException("等待后台 Mod 导出超时。");
		}
		return frames;
	}

	private async Task<ModEditorPanel> OpenModEditor()
	{
		ModEditorManager modEditorManager = ModEditorManager.Instance;
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(modEditorManager))
			{
				AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return null;
		}
		await WaitFrames(2);
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = Key.F3,
			PhysicalKeycode = Key.F3,
			Pressed = true
		});
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = Key.F3,
			PhysicalKeycode = Key.F3,
			Pressed = false
		});
		for (int frame = 0; frame < 900; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			Node instance = modEditorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(modEditorPanel) && !GodotObject.IsInstanceValid(instance))
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static bool PackageHasRuntimeAssembly(string packagePath, bool expected)
	{
		if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
		{
			return false;
		}
		using ZipArchive zipArchive = ZipFile.OpenRead(packagePath);
		ZipArchiveEntry? entry = zipArchive.GetEntry("Runtime/ModAssembly.dll");
		bool flag = entry != null && entry.Length > 0;
		ZipArchiveEntry entry2 = zipArchive.GetEntry("mod.json");
		if (entry2 == null)
		{
			return false;
		}
		using Stream utf8Json = entry2.Open();
		using JsonDocument jsonDocument = JsonDocument.Parse(utf8Json);
		JsonElement value;
		string text = (jsonDocument.RootElement.TryGetProperty("runtimeAssembly", out value) ? (value.GetString() ?? "") : "");
		return (!expected) ? (!flag && string.IsNullOrWhiteSpace(text)) : (flag && jsonDocument.RootElement.GetProperty("runtimeApiVersion").GetInt32() == 1 && jsonDocument.RootElement.GetProperty("runtimeAssemblyPolicy").GetString() == "required" && string.Equals(text, "Runtime/ModAssembly.dll", StringComparison.Ordinal));
	}

	private static bool HasTemporaryPackages(string outputDirectory)
	{
		if (Directory.Exists(outputDirectory))
		{
			return Directory.EnumerateFiles(outputDirectory, "*.tmp", SearchOption.TopDirectoryOnly).GetEnumerator().MoveNext();
		}
		return false;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private async Task VerifyAuthoringExperience(ModProject project, string output)
	{
		string path = Path.Combine(project.ProjectPath, "Battle", "Features", "note.txt");
		string path2 = Path.Combine(project.ProjectPath, "Localization", "text.csv");
		File.WriteAllText(path, "battle-resource-content");
		File.WriteAllText(path2, "key,en,zh\nhello,Hello,你好\n");
		string manifestBefore = File.ReadAllText(Path.Combine(project.ProjectPath, "mod.json"));
		string text = Path.Combine(project.ProjectPath, "exports");
		Directory.CreateDirectory(text);
		File.WriteAllText(Path.Combine(text, "old.pmod"), "previous package must not be nested");
		ModProject.ExportResult exportResult = await project.ExportAsync(output);
		Require(exportResult.Success && !exportResult.CompilationAttempted, "新增目录中的纯资源应直接导出：" + exportResult.ErrorMessage);
		Require(File.ReadAllText(Path.Combine(project.ProjectPath, "mod.json")) == manifestBefore, "导出不能写回作者清单。");
		Require(File.Exists(project.Export(output)), "Godot 主线程的同步导出必须完成，不能死锁。");
		if (exportResult.Success)
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(exportResult.OutputPath);
			Require(!zipArchive.Entries.Any((ZipArchiveEntry zipArchiveEntry) => zipArchiveEntry.FullName.EndsWith(".pmod", StringComparison.OrdinalIgnoreCase)), "导出不能递归打包已有安装包。");
			(string, string)[] array = new(string, string)[2]
			{
				("Battle/Features/note.txt", "battle-resource-content"),
				("Localization/text.csv", "key,en,zh\nhello,Hello,你好\n")
			};
			for (int num = 0; num < array.Length; num++)
			{
				(string, string) tuple = array[num];
				ZipArchiveEntry entry = zipArchive.GetEntry(tuple.Item1);
				Require(entry != null, "安装包漏掉标准目录文件：" + tuple.Item1);
				if (entry != null)
				{
					using StreamReader streamReader = new StreamReader(entry.Open());
					Require(streamReader.ReadToEnd() == tuple.Item2, "打包改变了资源内容。");
				}
			}
		}
		XWTemplateLibrary.TemplateCreateResult create = XWResourceCreateRoute.CreateFromAction("new-playable-level", Path.Combine(project.ProjectPath, "Resources", "Levels"), "FirstLevel");
		Require(create.Success && create.CreatedPaths.Count == 2, "快捷创建必须同时交付首关和目录：" + create.Error);
		if (!create.Success)
		{
			return;
		}
		LevelCatalogConfig catalog = ResourceLoader.Load<LevelCatalogConfig>(create.CreatedPath, null, ResourceLoader.CacheMode.Ignore);
		Require(catalog?.chapterList[0].levelList[0].normalLevel?.name == "FirstLevel", "默认目录没有绑定首关。");
		ModEditorCompiledExportRuntimeProbe modEditorCompiledExportRuntimeProbe = this;
		Texture2D unlockImage = catalog.chapterList[0].unlockImage;
		int condition;
		if (unlockImage != null && unlockImage.GetWidth() > 0)
		{
			Texture2D unlockImage2 = catalog.chapterList[0].levelList[0].unlockImage;
			if (unlockImage2 != null && unlockImage2.GetWidth() > 0)
			{
				Texture2D background = catalog.chapterList[0].background;
				if (background != null && background.GetWidth() > 0)
				{
					Texture2D building = catalog.chapterList[0].building;
					condition = ((building != null && building.GetWidth() > 0) ? 1 : 0);
					goto IL_0461;
				}
			}
		}
		condition = 0;
		goto IL_0461;
		IL_0461:
		modEditorCompiledExportRuntimeProbe.Require((byte)condition != 0, "快捷创建必须包含可加载的章节、关卡封面与背景，不能产生透明入口。");
		string existingPackage = Path.Combine(output, project.Name + ".pmod");
		byte[] preserved = File.ReadAllBytes(existingPackage);
		string levelPath = catalog.chapterList[0].levelList[0].normalLevel.ResourcePath;
		ResourceLoader.Load<TowerDefenseLevelNewConfig>(levelPath, null, ResourceLoader.CacheMode.Reuse);
		TowerDefenseLevelNewConfig diskLevel = ResourceLoader.Load<TowerDefenseLevelNewConfig>(levelPath, null, ResourceLoader.CacheMode.IgnoreDeep);
		diskLevel.name = "disk-name-mismatch";
		Require(ResourceSaver.Save(diskLevel, levelPath, ResourceSaver.SaverFlags.None) == Error.Ok, "保存缓存差异夹具。");
		ModProject.ExportResult exportResult2 = await project.ExportAsync(output);
		Require(!exportResult2.Success && exportResult2.Issues.Any((XWValidationIssue issue) => issue.Message.Contains("name 必须")), "检查必须使用磁盘关卡而不是预热缓存。");
		Require(File.ReadAllBytes(existingPackage).SequenceEqual(preserved), "缓存差异检查失败覆盖了旧安装包。");
		diskLevel.name = "FirstLevel";
		Require(ResourceSaver.Save(diskLevel, levelPath, ResourceSaver.SaverFlags.None) == Error.Ok, "恢复有效关卡夹具。");
		catalog.chapterList[0].levelList[0].normalLevel = null;
		Require(ResourceSaver.Save(catalog, create.CreatedPath, ResourceSaver.SaverFlags.None) == Error.Ok, "保存空普通难度夹具。");
		ModProject.ExportResult exportResult3 = await project.ExportAsync(output);
		Require(!exportResult3.Success && !exportResult3.CompilationAttempted && exportResult3.Issues.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error), "空普通难度未阻止导出。");
		Require(File.ReadAllBytes(existingPackage).SequenceEqual(preserved), "发布检查失败覆盖了旧安装包。");
		File.Delete(create.CreatedPath);
		XWModExportSnapshot snapshot = new XWModExportSnapshot(project.ProjectPath);
		List<XWValidationIssue> source = new XWModValidationService().ValidateExport(snapshot);
		Require(source.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Warning && issue.Message.Contains("尚未加入")), "未接入目录的关卡应提示不可见。");
		Require(!source.Any((XWValidationIssue issue) => issue.Code == XWValidationIssue.IssueCode.DotNetToolchainMissing), "纯资源工程不应检查 .NET 工具链。");
		GD.Print("MOD_AUTHORING_EXPERIENCE_CONTRACTS passed=True");
	}
}
