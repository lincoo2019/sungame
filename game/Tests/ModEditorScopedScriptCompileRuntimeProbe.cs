using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools.GUI;

public partial class ModEditorScopedScriptCompileRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _probeRoot = "";

	public override async void _Ready()
	{
		bool f3 = false;
		bool editor = false;
		bool scoped = false;
		bool asyncBuild = false;
		bool diagnostics = false;
		bool recovered = false;
		bool output = false;
		bool safePaths = false;
		bool compileOnly = false;
		bool directUi = false;
		bool singleFlight = false;
		int physicalStarts = -1;
		int joined = -1;
		bool sharedResult = false;
		bool actionsDisabled = false;
		bool actionsRecovered = false;
		bool noProjectBlocked = false;
		XWScriptEditor scriptEditor = null;
		try
		{
			_ = 16;
			try
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
				Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
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
				ModEditorPanel panel = await WaitForEditor(900);
				f3 = GodotObject.IsInstanceValid(panel) && GodotObject.IsInstanceValid(FindAncestorWindow(panel));
				Require(f3, "F3 did not open the real ModEditor window.");
				if (!f3)
				{
					Finish();
					return;
				}
				scriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
				editor = GodotObject.IsInstanceValid(scriptEditor);
				Require(editor, "The real F3 editor did not mount XWScriptEditor.");
				if (!editor)
				{
					Finish();
					return;
				}
				XWModToolsPanel toolsPanel = await WaitForToolsPanel(panel, 300);
				Button compileButton = scriptEditor.GetNodeOrNull<Button>("%CompileButton");
				Button debugStartButton = scriptEditor.GetNodeOrNull<Button>("%DebugStartButton");
				Button toolBuildButton = toolsPanel?.GetNodeOrNull<Button>("%RunScriptBuild");
				int compileInvocationsBeforeNoProject = scriptEditor.CompileInvocationCount;
				XWScriptCompiler.CompileResult noProjectResult = await scriptEditor.CompileScriptsAsync();
				await WaitFrames(1);
				noProjectBlocked = noProjectResult == null && scriptEditor.CompileInvocationCount == compileInvocationsBeforeNoProject && (compileButton?.Disabled ?? false) && (debugStartButton?.Disabled ?? false) && (toolBuildButton?.Disabled ?? false) && XWScriptCompiler.GetModBuildMetrics("").PhysicalStarts == 0;
				Require(noProjectBlocked, "No-project Compile, Debug, or RunScriptBuild action remained available or started a build.");
				string text = Guid.NewGuid().ToString("N");
				string text2 = ProjectSettings.GlobalizePath("user://ScopedScriptCompileProbe/");
				string text3 = "ScopedCompile" + text;
				_probeRoot = Path.Combine(text2, text3);
				if (Directory.Exists(text2))
				{
					Directory.Delete(text2, recursive: true);
				}
				Directory.CreateDirectory(text2);
				ModProject project = ModProject.Create(text2, text3, "1.0.0", "probe", "scoped compile probe");
				Require(project != null && Directory.Exists(project.ProjectPath), "Could not create the external Mod project fixture.");
				if (project == null)
				{
					Finish();
					return;
				}
				string scriptPath = Path.Combine(project.ProjectPath, "Scripts", "CompileProbe.cs");
				string path = Path.Combine(text2, "outside.cs");
				string path2 = Path.Combine(project.ProjectPath, ".build", "ignored.cs");
				string path3 = Path.Combine(project.ProjectPath, "Scripts", "LegacyProbe.gd");
				string path4 = Path.Combine(project.ProjectPath, "Scripts", "LegacyProbe.lua");
				string path5 = Path.Combine(project.ProjectPath, "Scripts", "VisualProbe.tres");
				string executedMarker = Path.Combine(project.ProjectPath, "script-executed.txt");
				Directory.CreateDirectory(Path.GetDirectoryName(scriptPath));
				Directory.CreateDirectory(Path.GetDirectoryName(path2));
				File.WriteAllText(scriptPath, "public static class CompileProbe { public static int Value => ; }");
				File.WriteAllText(path, "public static class OutsideProbe { }");
				File.WriteAllText(path2, "public static class BuildArtifactProbe { }");
				File.WriteAllText(path3, "extends Node\n");
				File.WriteAllText(path4, "return {}\n");
				File.WriteAllText(path5, "[gd_resource type=\"Resource\" format=3]\n\n[resource]\n");
				XWModManifestSyncService.SyncProject(project.ProjectPath);
				XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(project.ProjectPath, "mod.json"));
				bool flag = xWModManifest != null && xWModManifest.Scripts.Count == 1 && string.Equals(xWModManifest.Scripts[0], "Scripts/CompileProbe.cs", StringComparison.OrdinalIgnoreCase) && xWModManifest.Blueprints.Exists((string a) => string.Equals(a, "Scripts/VisualProbe.tres", StringComparison.OrdinalIgnoreCase)) && xWModManifest.Resources.Exists((string a) => string.Equals(a, "Scripts/LegacyProbe.gd", StringComparison.OrdinalIgnoreCase)) && xWModManifest.Resources.Exists((string a) => string.Equals(a, "Scripts/LegacyProbe.lua", StringComparison.OrdinalIgnoreCase));
				Require(flag, "Manifest sync classified non-C# files under Scripts as executable scripts.");
				XWModManifest manifest = new XWModManifest
				{
					Scripts = new List<string> { "Scripts/CompileProbe.cs", "Scripts/CompileProbe.cs", "../outside.cs", ".build/ignored.cs", "Scripts/not-source.txt" }
				};
				List<string> list = XWInGameDotNetBuildService.ResolveModScriptFiles(project.ProjectPath, manifest);
				safePaths = list.Count == 1 && string.Equals(Path.GetFullPath(list[0]), Path.GetFullPath(scriptPath), StringComparison.OrdinalIgnoreCase) && flag;
				Require(safePaths, "Script resolution accepted traversal, build artifacts, duplicates, or non-C# files.");
				MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
				Require(method != null, "Could not locate the real ModEditor project-entry workflow.");
				method?.Invoke(panel, new object[1] { project });
				await WaitFrames(10);
				scriptEditor.OpenFileAt(scriptPath, 0, 0);
				XWCodeEdit codeEdit = scriptEditor.FindChild("XWCodeEdit", recursive: true, owned: false) as XWCodeEdit;
				Label diagnosticsLabel = scriptEditor.FindChild("DiagnosticsLabel", recursive: true, owned: false) as Label;
				directUi = GodotObject.IsInstanceValid(codeEdit) && GodotObject.IsInstanceValid(compileButton) && compileButton.Text.Contains("Mod", StringComparison.Ordinal) && compileButton.TooltipText.Contains("不会执行脚本", StringComparison.Ordinal);
				Require(directUi, "The visible script surface does not expose the scoped compile-only Mod action.");
				Task<XWScriptCompiler.CompileResult> badTask = scriptEditor.CompileScriptsAsync();
				bool returnedPending = !badTask.IsCompleted;
				int liveFrames = 0;
				while (!badTask.IsCompleted && liveFrames < 1800)
				{
					await WaitFrames(1);
					liveFrames++;
				}
				XWScriptCompiler.CompileResult compileResult = await badTask;
				asyncBuild = returnedPending && liveFrames > 0;
				diagnostics = compileResult != null && compileResult.IsModProject && !compileResult.Success && compileResult.Diagnostics.Exists((XWCodeErrorChecker.ErrorData item) => item.SeverityLevel == XWCodeErrorChecker.Severity.Error && XWScriptCompiler.NormalizeFilePath(item.FilePath) == XWScriptCompiler.NormalizeFilePath(scriptPath));
				Require(asyncBuild, "The Mod compile action blocked the F3 main thread instead of yielding frames.");
				Require(diagnostics, "Compiler diagnostics were not mapped back to the active Mod script.");
				string text4 = executedMarker.Replace("\"", "\"\"");
				codeEdit.Text = "using System.IO; public static class CompileProbe { static CompileProbe() { File.WriteAllText(@\"" + text4 + "\", \"executed\"); } public static int Value => 42; }";
				await WaitFrames(2);
				scriptEditor.SaveFile();
				XWScriptCompiler.CompileResult good = await scriptEditor.CompileScriptsAsync();
				recovered = good != null && good.Success && good.IsModProject && good.ScriptCount == 1;
				scoped = recovered && Path.GetFullPath(good.ProjectPath).StartsWith(Path.GetFullPath(project.ProjectPath), StringComparison.OrdinalIgnoreCase) && !string.Equals(Path.GetFullPath(good.ProjectPath), Path.GetFullPath(ProjectSettings.GlobalizePath("res://PlantsVsZombies.csproj")), StringComparison.OrdinalIgnoreCase);
				output = recovered && !string.IsNullOrWhiteSpace(good.OutputAssemblyPath) && File.Exists(good.OutputAssemblyPath);
				compileOnly = !File.Exists(executedMarker);
				directUi = directUi && scriptEditor.LastCompileResult == good && GodotObject.IsInstanceValid(diagnosticsLabel) && diagnosticsLabel.Text.Contains("Mod", StringComparison.Ordinal) && diagnosticsLabel.Text.Contains("1 个脚本", StringComparison.Ordinal);
				Require(recovered, "The visible script editor did not recover from a compile error after fixing the source.");
				Require(scoped, "The compile action rebuilt the game project instead of the active external Mod.");
				Require(output, "The scoped Mod build did not produce its own assembly output.");
				Require(compileOnly, "Compiling a Mod script executed its static initializer.");
				Require(directUi, "The visible compile status did not report the active Mod target and script count.");
				XWEditorInterface.Instance?.FocusPanel("mod_tools");
				Button debugStopButton = scriptEditor.GetNodeOrNull<Button>("%DebugStopButton");
				Require(GodotObject.IsInstanceValid(toolsPanel) && GodotObject.IsInstanceValid(toolBuildButton), "The real Mod tools script-build action is unavailable.");
				Require(GodotObject.IsInstanceValid(debugStartButton) && GodotObject.IsInstanceValid(debugStopButton), "The real Mod debug actions are unavailable.");
				XWEditorInterface.Instance?.FocusPanel("script_editor");
				await WaitFrames(2);
				XWModBuildSingleFlight.BuildFlightMetrics metricsBefore = XWScriptCompiler.GetModBuildMetrics(project.ProjectPath);
				Task<XWScriptCompiler.CompileResult> sharedCompileTask = scriptEditor.CompileScriptsAsync();
				Task<XWScriptDebugSession.StartResult> sharedDebugTask = scriptEditor.StartDebugSessionAsync();
				toolBuildButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(1);
				bool oneActiveFlight = XWScriptCompiler.GetModBuildMetrics(project.ProjectPath).ActiveFlights == 1 && (toolsPanel?.IsScriptBuildRunning ?? false);
				actionsDisabled = (compileButton?.Disabled ?? false) && (debugStartButton?.Disabled ?? false) && (toolBuildButton?.Disabled ?? false);
				Require(actionsDisabled, "Compile, Debug, and RunScriptBuild actions were not all disabled during the shared build.");
				XWScriptCompiler.CompileResult sharedCompile = await sharedCompileTask;
				XWScriptDebugSession.StartResult sharedDebug = await sharedDebugTask;
				int frame = 0;
				while (GodotObject.IsInstanceValid(toolsPanel) && toolsPanel.IsScriptBuildRunning && frame < 300)
				{
					await WaitFrames(1);
					frame++;
				}
				XWModBuildSingleFlight.BuildFlightMetrics metricsAfter = XWScriptCompiler.GetModBuildMetrics(project.ProjectPath);
				physicalStarts = metricsAfter.PhysicalStarts - metricsBefore.PhysicalStarts;
				joined = metricsAfter.JoinedRequests - metricsBefore.JoinedRequests;
				int num;
				if ((sharedCompile?.Success ?? false) && (sharedDebug?.Success ?? false))
				{
					if (toolsPanel != null && toolsPanel.LastScriptBuildResult?.Success == true && sharedCompile == sharedDebug.CompileResult)
					{
						num = ((sharedCompile == toolsPanel.LastScriptBuildResult) ? 1 : 0);
						goto IL_12c5;
					}
				}
				num = 0;
				goto IL_12c5;
				IL_12c5:
				sharedResult = (byte)num != 0;
				bool debugStopped = await scriptEditor.StopDebugSessionAsync();
				await WaitFrames(2);
				int num2;
				if (debugStopped)
				{
					if (compileButton != null && !compileButton.Disabled)
					{
						if (debugStartButton != null && !debugStartButton.Disabled)
						{
							if (toolBuildButton != null && !toolBuildButton.Disabled)
							{
								num2 = ((debugStopButton?.Disabled ?? false) ? 1 : 0);
								goto IL_1400;
							}
						}
					}
				}
				num2 = 0;
				goto IL_1400;
				IL_1400:
				actionsRecovered = (byte)num2 != 0;
				Require(actionsRecovered, "Compile, Debug, and RunScriptBuild actions did not recover after debug stopped.");
				int num3;
				if (oneActiveFlight && physicalStarts == 1 && joined == 2 && metricsAfter.ActiveFlights == 0 && sharedResult && debugStopped && !scriptEditor.IsDebugSessionRunning)
				{
					num3 = ((toolsPanel != null && !toolsPanel.IsScriptBuildRunning) ? 1 : 0);
				}
				else
				{
					num3 = 0;
				}
				singleFlight = (byte)num3 != 0;
				Require(singleFlight, $"Compile/Debug/RunScriptBuild did not share one physical build (physical={physicalStarts}, joined={joined}, shared={sharedResult}).");
				if (!recovered)
				{
					GD.Print($"[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_RESULT] success={good?.Success} exit={good?.ExitCode} isMod={good?.IsModProject} scripts={good?.ScriptCount} project={good?.ProjectPath} assembly={good?.OutputAssemblyPath}");
					foreach (XWCodeErrorChecker.ErrorData item in good?.Diagnostics ?? new List<XWCodeErrorChecker.ErrorData>())
					{
						GD.Print($"[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_DIAGNOSTIC] {item.FilePath}:{item.Line + 1}:{item.Column + 1} {item.Message}");
					}
					GD.Print("[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_OUTPUT] " + good?.Output?.Replace('\r', ' ').Replace('\n', ' '));
				}
			}
			catch (Exception ex)
			{
				_failures.Add(ex.ToString());
			}
		}
		finally
		{
			try
			{
				if (GodotObject.IsInstanceValid(scriptEditor) && scriptEditor.IsDebugSessionRunning)
				{
					await scriptEditor.StopDebugSessionAsync();
				}
			}
			catch (Exception ex2)
			{
				_failures.Add("Debug cleanup failed: " + ex2.Message);
			}
			try
			{
				string text5 = (string.IsNullOrWhiteSpace(_probeRoot) ? "" : Path.GetDirectoryName(_probeRoot));
				if (!string.IsNullOrWhiteSpace(text5) && Directory.Exists(text5))
				{
					Directory.Delete(text5, recursive: true);
				}
			}
			catch (Exception ex3)
			{
				_failures.Add("Cleanup failed: " + ex3.Message);
			}
		}
		GD.Print($"[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_PROBE] f3={f3} editor={editor} scoped={scoped} asyncBuild={asyncBuild} diagnostics={diagnostics} recovered={recovered} output={output} safePaths={safePaths} compileOnly={compileOnly} directUi={directUi} singleFlight={singleFlight} physicalStarts={physicalStarts} joined={joined} sharedResult={sharedResult} actionsDisabled={actionsDisabled} actionsRecovered={actionsRecovered} noProjectBlocked={noProjectBlocked} failures={_failures.Count}");
		Finish();
	}

	private async Task<ModEditorPanel> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
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

	private async Task<XWModToolsPanel> WaitForToolsPanel(ModEditorPanel panel, int maxFrames)
	{
		FieldInfo toolsField = typeof(ModEditorPanel).GetField("_modToolsPanel", BindingFlags.Instance | BindingFlags.NonPublic);
		for (int i = 0; i < maxFrames; i++)
		{
			XWModToolsPanel xWModToolsPanel = toolsField?.GetValue(panel) as XWModToolsPanel;
			if (GodotObject.IsInstanceValid(xWModToolsPanel) && GodotObject.IsInstanceValid(xWModToolsPanel.GetNodeOrNull<Button>("%RunScriptBuild")))
			{
				return xWModToolsPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
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

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCOPED_SCRIPT_COMPILE_PROBE_FAILURE] " + failure);
		}
		int exitCode = ((_failures.Count != 0) ? 1 : 0);
		Console.Out.Flush();
		Console.Error.Flush();
		GetTree().Quit(exitCode);
	}
}
