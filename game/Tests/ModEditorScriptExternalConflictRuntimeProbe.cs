using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorScriptExternalConflictRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWScriptEditor _editor;

	private XWCodeEdit _codeEdit;

	private string _probeParent = "";

	public override async void _Ready()
	{
		bool window = false;
		bool fingerprint = false;
		bool preserved = false;
		bool visual = false;
		bool diff = false;
		bool saveBlocked = false;
		bool saveAllBlocked = false;
		bool compileBlocked = false;
		bool debugBlocked = false;
		bool reload = false;
		bool overwrite = false;
		bool cleanReload = false;
		bool saveAsRollback = false;
		bool generatedReadOnly = false;
		bool inspectorUntouched = false;
		bool responsive = false;
		bool hiddenWatcherStopped = false;
		try
		{
			_ = 20;
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
				Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be created.");
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
				ModEditorPanel panel = await WaitForPanel(900);
				Require(GodotObject.IsInstanceValid(panel), "F3 did not open the real ModEditor panel.");
				_editor = FindNodeOfType<XWScriptEditor>(GetTree().Root);
				_codeEdit = _editor?.GetNodeOrNull<XWCodeEdit>("%XWCodeEdit");
				Require(GodotObject.IsInstanceValid(_editor) && GodotObject.IsInstanceValid(_codeEdit), "The real F3 window did not mount the C# editor.");
				if (!GodotObject.IsInstanceValid(panel) || !GodotObject.IsInstanceValid(_editor) || !GodotObject.IsInstanceValid(_codeEdit))
				{
					Finish(window, fingerprint, preserved, visual, diff, saveBlocked, saveAllBlocked, compileBlocked, debugBlocked, reload, overwrite, cleanReload, saveAsRollback, generatedReadOnly, inspectorUntouched, responsive, hiddenWatcherStopped);
					return;
				}
				XWEditorInterface.Instance?.FocusPanel("script_editor");
				await WaitFrames(3);
				window = FindAncestorWindow(_editor) != null && _editor.IsVisibleInTree();
				Require(window, "The C# editor is not visible inside the real F3 ModEditor window.");
				_probeParent = ProjectSettings.GlobalizePath("user://ScriptExternalConflictProbe_" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(_probeParent);
				ModProject project = ModProject.Create(_probeParent, "ExternalConflictProbe", "1.0.0", "CodexValidation", "C# external modification conflict validation");
				Require(project != null, "Could not create the external-conflict Mod project.");
				MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
				int num;
				if (project != null)
				{
					object obj = method?.Invoke(panel, new object[1] { project });
					num = ((obj is bool && (bool)obj) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				bool condition = (byte)num != 0;
				Require(condition, "Could not enter the external-conflict Mod through the real editor workflow.");
				await WaitFrames(12);
				XWEditorInterface.Instance?.FocusPanel("script_editor");
				_editor.SetWorkspaceActive(active: true);
				await WaitFrames(3);
				XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
				Node inspectorSentinel = new Node
				{
					Name = "ExternalConflictInspectorSentinel"
				};
				AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
				XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
				await WaitFrames(2);
				string scripts = Path.Combine(project.ProjectPath, "Scripts");
				Directory.CreateDirectory(scripts);
				string primaryPath = Path.Combine(scripts, "FingerprintProbe.cs");
				Require(Utf8Length("public class FingerprintProbe { public int Value = 1; }\n") == Utf8Length("public class FingerprintProbe { public int Value = 2; }\n") && Utf8Length("public class FingerprintProbe { public int Value = 2; }\n") == Utf8Length("public class FingerprintProbe { public int Value = 9; }\n"), "The same-length conflict fixture is invalid.");
				File.WriteAllText(primaryPath, "public class FingerprintProbe { public int Value = 1; }\n");
				Require(_editor.TryOpenFile(primaryPath), "Could not open the primary conflict script.");
				await WaitFrames(3);
				_codeEdit.SetAllText("public class FingerprintProbe { public int Value = 2; }\n");
				_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
				await WaitFrames(1);
				ulong baselineTimestamp = Godot.FileAccess.GetModifiedTime(primaryPath);
				long baselineLength = new FileInfo(primaryPath).Length;
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(primaryPath);
				File.WriteAllText(primaryPath, "public class FingerprintProbe { public int Value = 9; }\n");
				File.SetLastWriteTimeUtc(primaryPath, lastWriteTimeUtc);
				Stopwatch scanWatch = Stopwatch.StartNew();
				_editor.ScanExternalChangesNowForProbe();
				scanWatch.Stop();
				await WaitFrames(2);
				fingerprint = new FileInfo(primaryPath).Length == baselineLength && Godot.FileAccess.GetModifiedTime(primaryPath) == baselineTimestamp && _editor.IsCurrentDocumentInExternalConflict && _editor.ExternalConflictDetectedCount > 0;
				Require(fingerprint, "A same-length, same-timestamp external edit was not detected by content fingerprint.");
				preserved = File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 9; }\n" && _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 2; }\n";
				Require(preserved, "Conflict detection did not preserve both disk and editor versions.");
				PanelContainer conflictBar = _editor.GetNodeOrNull<PanelContainer>("%ExternalConflictBar");
				Label nodeOrNull = _editor.GetNodeOrNull<Label>("%ExternalConflictTitleLabel");
				Button nodeOrNull2 = _editor.GetNodeOrNull<Button>("%ViewExternalDiffButton");
				Button reloadButton = _editor.GetNodeOrNull<Button>("%ReloadExternalButton");
				Button overwriteButton = _editor.GetNodeOrNull<Button>("%OverwriteExternalButton");
				Button nodeOrNull3 = _editor.GetNodeOrNull<Button>("%SaveConflictCopyButton");
				Window diffWindow = _editor.GetNodeOrNull<Window>("%ExternalConflictDiffWindow");
				TextEdit editorVersion = _editor.GetNodeOrNull<TextEdit>("%ExternalConflictEditorText");
				TextEdit diskVersion = _editor.GetNodeOrNull<TextEdit>("%ExternalConflictDiskText");
				visual = GodotObject.IsInstanceValid(conflictBar) && conflictBar.Visible && _editor.IsExternalConflictBarVisible && GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Text.Contains("外部", StringComparison.Ordinal) && GodotObject.IsInstanceValid(nodeOrNull2) && nodeOrNull2.Icon != null && GodotObject.IsInstanceValid(reloadButton) && reloadButton.Icon != null && !reloadButton.Disabled && GodotObject.IsInstanceValid(overwriteButton) && overwriteButton.Icon != null && GodotObject.IsInstanceValid(nodeOrNull3) && nodeOrNull3.Icon != null && !string.IsNullOrWhiteSpace(nodeOrNull3.TooltipText);
				Require(visual, "The visual external-conflict surface or its icon actions are incomplete.");
				nodeOrNull2?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				diff = GodotObject.IsInstanceValid(diffWindow) && diffWindow.Visible && editorVersion?.Text == "public class FingerprintProbe { public int Value = 2; }\n" && diskVersion?.Text == "public class FingerprintProbe { public int Value = 9; }\n" && !editorVersion.Editable && !diskVersion.Editable;
				Require(diff, "The real diff button did not show the preserved editor and disk versions.");
				diffWindow?.Hide();
				int compileInvocationsBefore = _editor.CompileInvocationCount;
				int debugInvocationsBefore = _editor.DebugStartInvocationCount;
				int compileGatesBefore = _editor.CompileSaveGateCount;
				int debugGatesBefore = _editor.DebugSaveGateCount;
				saveBlocked = !_editor.SaveFile() && _editor.LastSaveStatus == XWScriptEditor.ScriptSaveStatus.Conflict && File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 9; }\n" && _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 2; }\n";
				Require(saveBlocked, "SaveFile overwrote or lost one side of an external conflict.");
				int num2 = _editor.SaveAllTabs();
				saveAllBlocked = num2 < 0 && File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 9; }\n" && _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 2; }\n";
				Require(saveAllBlocked, "SaveAllTabs did not report the unresolved conflict.");
				Stopwatch compileWatch = Stopwatch.StartNew();
				XWScriptCompiler.CompileResult compileResult = await _editor.CompileScriptsAsync();
				compileWatch.Stop();
				compileBlocked = compileResult == null && _editor.CompileSaveGateCount == compileGatesBefore + 1 && _editor.CompileInvocationCount == compileInvocationsBefore;
				Require(compileBlocked, "Compile dispatched despite an unresolved external conflict.");
				Stopwatch debugWatch = Stopwatch.StartNew();
				XWScriptDebugSession.StartResult startResult = await _editor.StartDebugSessionAsync();
				debugWatch.Stop();
				debugBlocked = (startResult == null || !startResult.Success) && startResult != null && startResult.Message.Contains("冲突", StringComparison.Ordinal) && _editor.DebugSaveGateCount == debugGatesBefore + 1 && _editor.DebugStartInvocationCount == debugInvocationsBefore && !_editor.IsDebugSessionRunning;
				Require(debugBlocked, "Debug dispatched despite an unresolved external conflict.");
				responsive = scanWatch.ElapsedMilliseconds < 120 && compileWatch.ElapsedMilliseconds < 120 && debugWatch.ElapsedMilliseconds < 120 && System.Environment.CurrentManagedThreadId == _editor.DebugMainThreadId;
				Require(responsive, $"Conflict scan/gates blocked the main thread: scan={scanWatch.ElapsedMilliseconds}ms, compile={compileWatch.ElapsedMilliseconds}ms, debug={debugWatch.ElapsedMilliseconds}ms.");
				int reloadCountBefore = _editor.ExternalConflictReloadCount;
				reloadButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(3);
				reload = _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 9; }\n" && File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 9; }\n" && !_editor.IsCurrentDocumentInExternalConflict && !conflictBar.Visible && _editor.ExternalConflictReloadCount == reloadCountBefore + 1;
				Require(reload, "The real reload button did not adopt the disk version and clear conflict state.");
				DateTime lastWriteTimeUtc2 = File.GetLastWriteTimeUtc(primaryPath);
				int cleanReloadBefore = _editor.CleanExternalReloadCount;
				File.WriteAllText(primaryPath, "public class FingerprintProbe { public int Value = 8; }\n");
				File.SetLastWriteTimeUtc(primaryPath, lastWriteTimeUtc2);
				_editor.ScanExternalChangesNowForProbe();
				await WaitFrames(3);
				cleanReload = _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 8; }\n" && !_editor.IsCurrentDocumentInExternalConflict && _editor.CleanExternalReloadCount == cleanReloadBefore + 1;
				Require(cleanReload, "A clean open script did not automatically reload an external edit.");
				_codeEdit.SetAllText("public class FingerprintProbe { public int Value = 6; }\n");
				_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
				File.WriteAllText(primaryPath, "public class FingerprintProbe { public int Value = 7; }\n");
				_editor.ScanExternalChangesNowForProbe();
				await WaitFrames(2);
				int overwriteCountBefore = _editor.ExternalConflictOverwriteCount;
				overwriteButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(3);
				overwrite = File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 6; }\n" && _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 6; }\n" && !_editor.IsCurrentDocumentInExternalConflict && _editor.ExternalConflictOverwriteCount == overwriteCountBefore + 1;
				Require(overwrite, "The real overwrite button did not atomically persist the editor version.");
				string occupiedPath = Path.Combine(scripts, "Occupied.cs");
				File.WriteAllText(occupiedPath, "public class Occupied { }\n");
				Require(_editor.TryOpenFile(occupiedPath), "Could not open the occupied Save As destination.");
				Require(_editor.TryOpenFile(primaryPath), "Could not return to the primary script.");
				_codeEdit.SetAllText("public class FingerprintProbe { public int Value = 5; }\n");
				_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
				int rollbackCountBefore = _editor.SaveAsRollbackCount;
				XWScriptEditor.ScriptSaveStatus saveAsResult = _editor.SaveCurrentFileAsForProbe(occupiedPath);
				await WaitFrames(2);
				saveAsRollback = saveAsResult == XWScriptEditor.ScriptSaveStatus.IoError && SamePath(_editor.GetCurrentFilePath(), primaryPath) && _editor.CurrentDocumentText == "public class FingerprintProbe { public int Value = 5; }\n" && File.ReadAllText(primaryPath) == "public class FingerprintProbe { public int Value = 6; }\n" && File.ReadAllText(occupiedPath) == "public class Occupied { }\n" && _editor.SaveAsRollbackCount == rollbackCountBefore + 1;
				Require(saveAsRollback, "Failed Save As did not roll back tab ownership and both disk files.");
				string generatedPath = Path.Combine(scripts, "Orphan.generated.cs");
				string contents = "// <auto-generated by PVZHE ModEditor Blueprint>\npublic class OrphanGeneratedConflictProbe { }\n";
				string generatedExternal = "// <auto-generated by PVZHE ModEditor Blueprint>\npublic class OrphanGeneratedConflictProbeExternal { }\n";
				File.WriteAllText(generatedPath, contents);
				Require(_editor.TryOpenFile(generatedPath), "Could not open generated C# for read-only verification.");
				await WaitFrames(2);
				File.WriteAllText(generatedPath, generatedExternal);
				_editor.ScanExternalChangesNowForProbe();
				bool flag = _editor.SaveFile();
				generatedReadOnly = _editor.IsCurrentDocumentReadOnly && !flag && _editor.LastSaveStatus == XWScriptEditor.ScriptSaveStatus.ReadOnly && !_editor.IsCurrentDocumentInExternalConflict && File.ReadAllText(generatedPath) == generatedExternal;
				Require(generatedReadOnly, "Blueprint-generated C# lost read-only protection or entered the writable conflict path.");
				inspectorUntouched = inspector == null || inspector.CurrentObject == inspectorSentinel;
				Require(inspectorUntouched, "C# conflict handling replaced or changed the Inspector target.");
				string hiddenPath = Path.Combine(scripts, "HiddenWatcher.cs");
				File.WriteAllText(hiddenPath, "public class HiddenWatcher { public int Value = 1; }\n");
				Require(_editor.TryOpenFile(hiddenPath), "Could not open hidden-watcher fixture.");
				await WaitFrames(2);
				int hiddenReloadBefore = _editor.CleanExternalReloadCount;
				_editor.SetWorkspaceActive(active: false);
				await WaitFrames(3);
				File.WriteAllText(hiddenPath, "public class HiddenWatcher { public int Value = 2; }\n");
				await WaitFrames(120);
				hiddenWatcherStopped = _editor.IsExternalConflictWatcherIdle && _editor.IsHiddenWorkQuiescent && _editor.CleanExternalReloadCount == hiddenReloadBefore && _editor.CurrentDocumentText == "public class HiddenWatcher { public int Value = 1; }\n";
				Require(hiddenWatcherStopped, "The external-conflict watcher continued scanning while the C# workspace was hidden.");
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
				if (GodotObject.IsInstanceValid(_editor) && _editor.IsDebugSessionRunning)
				{
					await _editor.StopDebugSessionAsync();
				}
			}
			catch (Exception ex2)
			{
				_failures.Add("Debug cleanup failed: " + ex2.Message);
			}
			try
			{
				if (!string.IsNullOrWhiteSpace(_probeParent) && Directory.Exists(_probeParent))
				{
					Directory.Delete(_probeParent, recursive: true);
				}
			}
			catch (Exception ex3)
			{
				_failures.Add("Probe cleanup failed: " + ex3.Message);
			}
		}
		Finish(window, fingerprint, preserved, visual, diff, saveBlocked, saveAllBlocked, compileBlocked, debugBlocked, reload, overwrite, cleanReload, saveAsRollback, generatedReadOnly, inspectorUntouched, responsive, hiddenWatcherStopped);
	}

	private async Task<ModEditorPanel> WaitForPanel(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			Control instance = modEditorPanel?.GetNodeOrNull<Control>("%LoadingOverlay");
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

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
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

	private static bool SamePath(string left, string right)
	{
		try
		{
			return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static int Utf8Length(string text)
	{
		return Encoding.UTF8.GetByteCount(text ?? "");
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish(bool window, bool fingerprint, bool preserved, bool visual, bool diff, bool saveBlocked, bool saveAllBlocked, bool compileBlocked, bool debugBlocked, bool reload, bool overwrite, bool cleanReload, bool saveAsRollback, bool generatedReadOnly, bool inspectorUntouched, bool responsive, bool hiddenWatcherStopped)
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCRIPT_EXTERNAL_CONFLICT_FAILURE] " + failure);
		}
		GD.Print($"[MOD_EDITOR_SCRIPT_EXTERNAL_CONFLICT_PROBE] window={window} fingerprint={fingerprint} preserved={preserved} visual={visual} diff={diff} saveBlocked={saveBlocked} saveAllBlocked={saveAllBlocked} compileBlocked={compileBlocked} debugBlocked={debugBlocked} reload={reload} overwrite={overwrite} cleanReload={cleanReload} saveAsRollback={saveAsRollback} generatedReadOnly={generatedReadOnly} inspectorUntouched={inspectorUntouched} responsive={responsive} hiddenWatcherStopped={hiddenWatcherStopped} failures={_failures.Count}");
		Console.Out.Flush();
		Console.Error.Flush();
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
