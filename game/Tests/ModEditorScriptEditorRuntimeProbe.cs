using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorScriptEditorRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWScriptEditor _editor;

	private XWCodeEdit _codeEdit;

	public override async void _Ready()
	{
		_ = 69;
		try
		{
			XWCSharpCodeModel.ResetIndexForProbe();
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager is unavailable.");
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
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not mount the real script editor.");
			if (!flag)
			{
				Finish();
				return;
			}
			Require(await WaitForModEditorReady(1200), "F3 ModEditor did not finish asynchronous initialization.");
			XWEditorInterface.Instance?.FocusPanel("script_editor");
			await WaitFrames(3);
			Require(_editor.IsVisibleInTree(), "F3 ModEditor did not visibly enter the script workspace.");
			bool window = FindAncestorWindow(_editor) != null;
			Require(window, "Script editor is not mounted in the F3 ModEditor window.");
			TabBar tabs = _editor.GetNode<TabBar>("%ScriptTabBar");
			string projectRoot = ProjectSettings.GlobalizePath("user://mod_editor_script_probe_project");
			DirAccess.MakeDirRecursiveAbsolute(projectRoot);
			Stopwatch stopwatch = Stopwatch.StartNew();
			Task task = XWCSharpCodeModel.RequestBackgroundWarmup(projectRoot);
			Task task2 = XWCSharpCodeModel.RequestBackgroundWarmup(projectRoot);
			Task task3 = XWCSharpCodeModel.RequestBackgroundWarmup(projectRoot);
			stopwatch.Stop();
			bool uiResponsive = stopwatch.ElapsedMilliseconds < 50;
			Require(uiResponsive, $"Completion index requests blocked UI dispatch for {stopwatch.ElapsedMilliseconds}ms.");
			global::_003C_003Ey__InlineArray3<Task> buffer = default(global::_003C_003Ey__InlineArray3<Task>);
			buffer[0] = task;
			buffer[1] = task2;
			buffer[2] = task3;
			await Task.WhenAll(buffer);
			XWCSharpProjectIndex.IndexMetrics indexMetrics = XWCSharpCodeModel.GetIndexMetrics();
			bool condition = indexMetrics.FullIndexRuns == 1;
			Require(condition, $"Repeated completion requests built {indexMetrics.FullIndexRuns} full indexes.");
			string firstPath = projectRoot.PathJoin("mod_editor_script_probe_a.cs");
			string secondPath = projectRoot.PathJoin("mod_editor_script_probe_b.cs");
			WriteFile(firstPath, "public class ProbeA { }\n");
			WriteFile(secondPath, "public class ProbeB { }\n");
			_editor.OpenFile(firstPath);
			await WaitFrames(2);
			_editor.OpenFile(secondPath);
			await WaitFrames(2);
			_editor.OpenFile(firstPath);
			await WaitFrames(2);
			_codeEdit.SetProjectRoot(projectRoot);
			bool tabLoadClean = tabs.CurrentTab >= 0 && !tabs.GetTabTitle(tabs.CurrentTab).StartsWith("(*)", StringComparison.Ordinal);
			Require(tabLoadClean, "Programmatic tab loading marked a clean script dirty.");
			int incrementalBefore = XWCSharpCodeModel.GetIndexMetrics().IncrementalPublishes;
			_codeEdit.SetCaretLine(0);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(0).Length);
			_codeEdit.InsertAtCursor("\npublic class IncrementalProbe { }\n");
			await WaitForIncrementalPublish(incrementalBefore, 300);
			int incrementalAfter = XWCSharpCodeModel.GetIndexMetrics().IncrementalPublishes;
			bool incrementalOnly = incrementalAfter == incrementalBefore + 1 && XWCSharpCodeModel.GetIndexMetrics().FullIndexRuns == 1;
			string absoluteFirstPath = firstPath;
			StringBuilder stringBuilder = new StringBuilder("namespace ProbeIndex;\n");
			for (int i = 0; i < 4000; i++)
			{
				stringBuilder.Append("public class StaleProbe").Append(i).Append(" { }\n");
			}
			Task task4 = XWCSharpCodeModel.RequestSourceUpdate(absoluteFirstPath, stringBuilder.ToString());
			Task task5 = XWCSharpCodeModel.RequestSourceUpdate(absoluteFirstPath, "namespace ProbeIndex;\npublic class LatestProbe { }\n");
			global::_003C_003Ey__InlineArray2<Task> buffer2 = default(global::_003C_003Ey__InlineArray2<Task>);
			buffer2[0] = task4;
			buffer2[1] = task5;
			await Task.WhenAll(buffer2);
			if (!XWCSharpProjectIndex.GetSnapshot().TypeNamespaces.ContainsKey("LatestProbe"))
			{
				await XWCSharpCodeModel.RequestSourceUpdate(absoluteFirstPath, "namespace ProbeIndex;\npublic class LatestProbe { }\n");
			}
			XWCSharpProjectIndex.Snapshot snapshot = XWCSharpProjectIndex.GetSnapshot();
			bool flag2 = snapshot.TypeNamespaces.ContainsKey("LatestProbe");
			bool flag3 = snapshot.TypeNamespaces.ContainsKey("StaleProbe3999");
			int stalePublications = XWCSharpCodeModel.GetIndexMetrics().StalePublications;
			GD.Print($"[MOD_EDITOR_SCRIPT_INDEX_DETAIL] latestVisible={flag2} staleVisible={flag3} stalePublications={stalePublications}");
			bool indexStaleSafe = flag2 && !flag3;
			Require(indexStaleSafe, "A stale incremental completion generation replaced the latest source.");
			await WaitForCompletionIdle(_codeEdit, 600);
			Require(_codeEdit.ResetCompletionMetricsForProbe(), "Completion metrics could not reset from an idle editor.");
			StringBuilder stringBuilder2 = new StringBuilder("public class LargeCompletionProbe\n{\n    public void Run()\n    {\n");
			for (int j = 0; j < 20000; j++)
			{
				stringBuilder2.Append("        int filler").Append(j).Append(" = ")
					.Append(j)
					.Append(";\n");
			}
			string oldCompletionSource = stringBuilder2?.ToString() + "        int oldThing = 1;\n        old\n    }\n}\n";
			string allText = stringBuilder2?.ToString() + "        int latestThing = 2;\n        latest\n    }\n}\n";
			_codeEdit.SetAllText(oldCompletionSource);
			_codeEdit.SetCaretLine(_codeEdit.GetLineCount() - 4);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(_codeEdit.GetCaretLine()).Length);
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			_codeEdit.RequestCompletionNow();
			stopwatch2.Stop();
			long firstDispatchMs = stopwatch2.ElapsedMilliseconds;
			_codeEdit.SetAllText(allText);
			_codeEdit.SetCaretLine(_codeEdit.GetLineCount() - 4);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(_codeEdit.GetCaretLine()).Length);
			stopwatch2.Restart();
			_codeEdit.RequestCompletionNow();
			stopwatch2.Stop();
			long secondDispatchMs = stopwatch2.ElapsedMilliseconds;
			(bool, double) obj = await WaitForCompletionIdle(_codeEdit, 1200);
			bool item = obj.Item1;
			double item2 = obj.Item2;
			XWCodeEdit.CompletionMetrics completionMetrics = _codeEdit.GetCompletionMetrics();
			bool largeCompletionResponsive = item && firstDispatchMs < 33 && secondDispatchMs < 33 && item2 < 120.0 && completionMetrics.MaxCaptureMilliseconds < 33 && completionMetrics.MaxApplyMilliseconds < 33;
			bool flag4 = _codeEdit.LastCompletionContainsOption("latestThing");
			bool flag5 = _codeEdit.LastCompletionContainsOption("oldThing");
			bool completionLatestOnly = completionMetrics.AppliedResults == 1 && completionMetrics.LastRequestedRevision == completionMetrics.LastAppliedRevision && completionMetrics.LastAppliedPrefix == "latest" && flag4 && !flag5 && _codeEdit.PublishedCompletionContainsOption("latestThing") && !_codeEdit.PublishedCompletionContainsOption("oldThing");
			bool completionSingleFlight = completionMetrics.Requests == 2 && completionMetrics.CoalescedRequests >= 1 && completionMetrics.ExecutedAnalyses == 1 && completionMetrics.PeakActiveWorkers == 1 && completionMetrics.ActiveWorkers == 0 && completionMetrics.PendingRequests == 0;
			Require(largeCompletionResponsive, $"Large completion blocked frames (dispatch={firstDispatchMs}/{secondDispatchMs}ms, frame={item2:F1}ms, capture={completionMetrics.MaxCaptureMilliseconds}ms, apply={completionMetrics.MaxApplyMilliseconds}ms).");
			Require(completionLatestOnly, $"Completion published a stale revision (applied={completionMetrics.AppliedResults}, prefix={completionMetrics.LastAppliedPrefix}, requestedRevision={completionMetrics.LastRequestedRevision}, appliedRevision={completionMetrics.LastAppliedRevision}, latestOption={flag4}, staleOption={flag5}).");
			Require(completionSingleFlight, $"Completion was not single-flight (requests={completionMetrics.Requests}, coalesced={completionMetrics.CoalescedRequests}, canceled={completionMetrics.CanceledAnalyses}, peak={completionMetrics.PeakActiveWorkers}).");
			GD.Print($"[MOD_EDITOR_SCRIPT_COMPLETION_DETAIL] dispatch={firstDispatchMs}/{secondDispatchMs}ms maxFrame={item2:F1}ms capture={completionMetrics.MaxCaptureMilliseconds}ms apply={completionMetrics.MaxApplyMilliseconds}ms executed={completionMetrics.ExecutedAnalyses} applied={completionMetrics.AppliedResults} options={completionMetrics.LastOptionCount}");
			await XWCSharpCodeModel.RequestSourceUpdate(firstPath, "public class MemberScopeProbe { private int targetStaleMember; }\n");
			await XWCSharpCodeModel.RequestSourceUpdate(secondPath, "public class BaseScopeProbe\n{\n    public int targetBaseMember;\n    private int targetBasePrivate;\n}\n");
			Require(_codeEdit.ResetCompletionMetricsForProbe(), "Member-scope completion metrics could not reset.");
			_codeEdit.SetAllText("public class MemberScopeProbe : BaseScopeProbe\n{\n    [Export] public int targetAttributed { get; set; }\n    [Signal] public delegate void targetHitEventHandler();\n    public int targetLatestMember;\n    public void Run()\n    {\n        this.target\n    }\n}\npublic class OtherScopeProbe\n{\n    public int targetOtherPublic;\n    protected int targetOtherProtected;\n    private int targetLeakedMember;\n}\n");
			_codeEdit.SetCaretLine(7);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(7).Length);
			_codeEdit.RequestCompletionNow();
			bool item3 = (await WaitForCompletionIdle(_codeEdit, 600)).Item1;
			bool completionMemberScopeClean = item3 && _codeEdit.LastCompletionContainsOption("targetLatestMember") && !_codeEdit.LastCompletionContainsOption("targetStaleMember") && !_codeEdit.LastCompletionContainsOption("targetLeakedMember") && _codeEdit.PublishedCompletionContainsOption("targetLatestMember") && !_codeEdit.PublishedCompletionContainsOption("targetStaleMember") && !_codeEdit.PublishedCompletionContainsOption("targetLeakedMember");
			bool completionInheritanceClean = item3 && _codeEdit.LastCompletionContainsOption("targetBaseMember") && _codeEdit.LastCompletionContainsOption("targetAttributed") && _codeEdit.LastCompletionContainsOption("targetHit") && !_codeEdit.LastCompletionContainsOption("targetBasePrivate") && _codeEdit.PublishedCompletionContainsOption("targetBaseMember") && _codeEdit.PublishedCompletionContainsOption("targetAttributed") && _codeEdit.PublishedCompletionContainsOption("targetHit") && !_codeEdit.PublishedCompletionContainsOption("targetBasePrivate");
			Require(completionMemberScopeClean, "Current-class completion leaked a stale current-document member or a different class member.");
			Require(completionInheritanceClean, "Source completion lost an inherited, attributed, or signal member, or exposed a private base member.");
			Require(_codeEdit.ResetCompletionMetricsForProbe(), "External-owner completion metrics could not reset.");
			string allText2 = "public class MemberScopeProbe : BaseScopeProbe\n{\n    [Export] public int targetAttributed { get; set; }\n    [Signal] public delegate void targetHitEventHandler();\n    public int targetLatestMember;\n    public void Run()\n    {\n        this.target\n    }\n}\npublic class OtherScopeProbe\n{\n    public int targetOtherPublic;\n    protected int targetOtherProtected;\n    private int targetLeakedMember;\n}\n".Replace("        this.target\n", "        OtherScopeProbe other;\n        other.target\n", StringComparison.Ordinal);
			_codeEdit.SetAllText(allText2);
			_codeEdit.SetCaretLine(8);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(8).Length);
			_codeEdit.RequestCompletionNow();
			bool completionAccessSafe = (await WaitForCompletionIdle(_codeEdit, 600)).Item1 && _codeEdit.LastCompletionContainsOption("targetOtherPublic") && !_codeEdit.LastCompletionContainsOption("targetLeakedMember") && !_codeEdit.LastCompletionContainsOption("targetOtherProtected") && _codeEdit.PublishedCompletionContainsOption("targetOtherPublic") && !_codeEdit.PublishedCompletionContainsOption("targetLeakedMember") && !_codeEdit.PublishedCompletionContainsOption("targetOtherProtected");
			Require(completionAccessSafe, "Completion exposed a private member through an unrelated instance owner.");
			Require(_codeEdit.ResetCompletionMetricsForProbe(), "Caret-move completion metrics could not reset.");
			_codeEdit.SetAllText(oldCompletionSource);
			_codeEdit.SetCaretLine(_codeEdit.GetLineCount() - 4);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(_codeEdit.GetCaretLine()).Length);
			_codeEdit.RequestCompletionNow();
			bool caretCompletionStarted = await WaitForCompletionExecutionStarted(_codeEdit, 300);
			_codeEdit.SetCaretLine(0);
			_codeEdit.SetCaretColumn(0);
			bool item4 = (await WaitForCompletionIdle(_codeEdit, 600)).Item1;
			XWCodeEdit.CompletionMetrics completionMetrics2 = _codeEdit.GetCompletionMetrics();
			bool completionCaretSafe = caretCompletionStarted && item4 && completionMetrics2.AppliedResults == 0 && completionMetrics2.CanceledAnalyses >= 1;
			Require(completionCaretSafe, $"Moving the caret retained old-position completion (canceled={completionMetrics2.CanceledAnalyses}, applied={completionMetrics2.AppliedResults}).");
			Require(_codeEdit.ResetCompletionMetricsForProbe(), "Hidden completion metrics could not reset.");
			_codeEdit.SetAllText(oldCompletionSource);
			_codeEdit.SetCaretLine(_codeEdit.GetLineCount() - 4);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(_codeEdit.GetCaretLine()).Length);
			_codeEdit.RequestCompletionNow();
			bool hiddenCompletionStarted = await WaitForCompletionExecutionStarted(_codeEdit, 300);
			_editor.Hide();
			bool hiddenCompletionSettled = (await WaitForCompletionIdle(_codeEdit, 600)).Item1;
			XWCodeEdit.CompletionMetrics hiddenCompletionMetrics = _codeEdit.GetCompletionMetrics();
			_editor.Show();
			await WaitFrames(2);
			bool completionHiddenCanceled = hiddenCompletionStarted && hiddenCompletionSettled && hiddenCompletionMetrics.CanceledAnalyses >= 1 && hiddenCompletionMetrics.AppliedResults == 0 && hiddenCompletionMetrics.ActiveWorkers == 0 && hiddenCompletionMetrics.PendingRequests == 0;
			Require(completionHiddenCanceled, $"Hidden completion work did not cancel (canceled={hiddenCompletionMetrics.CanceledAnalyses}, applied={hiddenCompletionMetrics.AppliedResults}, active={hiddenCompletionMetrics.ActiveWorkers}).");
			PackedScene completionCodeEditScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ScriptEditor/GUI/XWCodeEdit.tscn", null, ResourceLoader.CacheMode.Reuse);
			XWCodeEdit disposableCompletionEdit = completionCodeEditScene?.Instantiate<XWCodeEdit>(PackedScene.GenEditState.Disabled);
			Require(GodotObject.IsInstanceValid(disposableCompletionEdit), "Disposable completion editor could not be instantiated.");
			bool completionDestroyedClean = false;
			if (GodotObject.IsInstanceValid(disposableCompletionEdit))
			{
				AddChild(disposableCompletionEdit, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				disposableCompletionEdit.SetFileExtension("cs");
				disposableCompletionEdit.SetDocumentPath(firstPath);
				disposableCompletionEdit.SetAllText(oldCompletionSource);
				disposableCompletionEdit.SetCaretLine(disposableCompletionEdit.GetLineCount() - 4);
				disposableCompletionEdit.SetCaretColumn(disposableCompletionEdit.GetLine(disposableCompletionEdit.GetCaretLine()).Length);
				disposableCompletionEdit.ResetCompletionMetricsForProbe();
				disposableCompletionEdit.RequestCompletionNow();
				bool destroyCompletionStarted = await WaitForCompletionExecutionStarted(disposableCompletionEdit, 300);
				disposableCompletionEdit.QueueFree();
				bool item5 = (await WaitForCompletionIdle(disposableCompletionEdit, 600)).Item1;
				XWCodeEdit.CompletionMetrics completionMetrics3 = disposableCompletionEdit.GetCompletionMetrics();
				completionDestroyedClean = destroyCompletionStarted && item5 && completionMetrics3.AppliedResults == 0 && completionMetrics3.ActiveWorkers == 0 && completionMetrics3.PendingRequests == 0;
				Require(completionDestroyedClean, $"Destroyed completion editor retained work (canceled={completionMetrics3.CanceledAnalyses}, stale={completionMetrics3.StaleResults}, applied={completionMetrics3.AppliedResults}, active={completionMetrics3.ActiveWorkers}, pending={completionMetrics3.PendingRequests}).");
			}
			_codeEdit.SetAllText("public class Broken {\n");
			_codeEdit.RequestValidationNow();
			bool gotDiagnostics = await WaitForDiagnostics(300);
			int firstCount = _codeEdit.GetAllErrors().Count;
			_codeEdit.RequestValidationNow();
			await WaitFrames(30);
			_codeEdit.RequestValidationNow();
			await WaitFrames(30);
			int count = _codeEdit.GetAllErrors().Count;
			bool diagnosticsReplaced = gotDiagnostics && firstCount > 0 && count == firstCount;
			Require(diagnosticsReplaced, $"Diagnostics accumulated across checks ({firstCount} -> {count}).");
			await WaitForValidationIdle(_codeEdit, 300);
			Require(_codeEdit.ResetValidationMetricsForProbe(), "Validation metrics could not reset from an idle editor.");
			for (int k = 0; k < 50; k++)
			{
				string allText3 = ((k == 49) ? "public class LatestValidationProbe { }\n" : $"public class SupersededValidationProbe{k} {{\n");
				_codeEdit.SetAllText(allText3);
				_codeEdit.RequestValidationNow();
			}
			bool flag6 = await WaitForValidationIdle(_codeEdit, 600);
			XWCodeEdit.ValidationMetrics validationMetrics = _codeEdit.GetValidationMetrics();
			bool validationLatestOnly = flag6 && _codeEdit.GetAllErrors().Count == 0 && validationMetrics.PublishedResults == 1;
			bool validationSingleFlight = validationMetrics.Requests == 50 && validationMetrics.CoalescedRequests >= 49 && validationMetrics.ExecutedChecks == 1 && validationMetrics.PeakActiveWorkers == 1 && validationMetrics.ActiveWorkers == 0 && validationMetrics.PendingRequests == 0;
			Require(validationLatestOnly, $"Validation burst published a superseded snapshot (published={validationMetrics.PublishedResults}, errors={_codeEdit.GetAllErrors().Count}).");
			Require(validationSingleFlight, $"Validation burst was not single-flight (requests={validationMetrics.Requests}, coalesced={validationMetrics.CoalescedRequests}, executed={validationMetrics.ExecutedChecks}, peak={validationMetrics.PeakActiveWorkers}, active={validationMetrics.ActiveWorkers}).");
			Require(_codeEdit.ResetValidationMetricsForProbe(), "Superseded validation metrics could not reset.");
			StringBuilder stringBuilder3 = new StringBuilder(2000000);
			for (int l = 0; l < 40000; l++)
			{
				stringBuilder3.Append("public class SupersededValidation").Append(l).Append(" {\n");
			}
			string largeValidationSource = stringBuilder3.ToString();
			_codeEdit.SetAllText(largeValidationSource);
			Stopwatch validationDispatchWatch = Stopwatch.StartNew();
			_codeEdit.RequestValidationNow();
			validationDispatchWatch.Stop();
			bool supersededValidationStarted = await WaitForValidationExecutionStarted(_codeEdit, 300);
			_codeEdit.SetAllText("public class LatestCanceledValidationProbe { }\n");
			validationDispatchWatch.Start();
			_codeEdit.RequestValidationNow();
			validationDispatchWatch.Stop();
			(bool, double) obj2 = await WaitForValidationIdleWithFrameTime(_codeEdit, 900);
			bool item6 = obj2.Item1;
			double item7 = obj2.Item2;
			XWCodeEdit.ValidationMetrics validationMetrics2 = _codeEdit.GetValidationMetrics();
			bool validationSupersededCanceled = supersededValidationStarted && item6 && validationMetrics2.ExecutedChecks >= 2 && validationMetrics2.CanceledChecks >= 1 && validationMetrics2.PublishedResults == 1 && validationMetrics2.PeakActiveWorkers == 1 && validationMetrics2.ActiveWorkers == 0 && validationMetrics2.PendingRequests == 0 && _codeEdit.GetAllErrors().Count == 0;
			bool validationResponsive = item6 && validationDispatchWatch.ElapsedMilliseconds < 66 && item7 < 120.0 && validationMetrics2.MaxCaptureMilliseconds < 33 && validationMetrics2.MaxApplyMilliseconds < 33;
			Require(validationSupersededCanceled, $"Superseded validation kept consuming work or published stale diagnostics (started={supersededValidationStarted}, executed={validationMetrics2.ExecutedChecks}, canceled={validationMetrics2.CanceledChecks}, published={validationMetrics2.PublishedResults}, peak={validationMetrics2.PeakActiveWorkers}).");
			Require(validationResponsive, $"Large validation blocked frames (dispatch={validationDispatchWatch.ElapsedMilliseconds}ms, frame={item7:F1}ms, capture={validationMetrics2.MaxCaptureMilliseconds}ms, apply={validationMetrics2.MaxApplyMilliseconds}ms).");
			Require(_codeEdit.ResetValidationMetricsForProbe(), "Bounded-diagnostic validation metrics could not reset.");
			StringBuilder stringBuilder4 = new StringBuilder(120000);
			for (int m = 0; m < 1200; m++)
			{
				stringBuilder4.Append("public class BoundedDiagnostic").Append(m).Append(" {\n");
			}
			_codeEdit.SetAllText(stringBuilder4.ToString());
			_codeEdit.RequestValidationNow();
			(bool, double) obj3 = await WaitForValidationIdleWithFrameTime(_codeEdit, 900);
			bool item8 = obj3.Item1;
			double item9 = obj3.Item2;
			XWCodeEdit.ValidationMetrics validationMetrics3 = _codeEdit.GetValidationMetrics();
			bool validationBoundedApply = item8 && validationMetrics3.PublishedResults == 1 && validationMetrics3.LastDiagnosticCount > 0 && validationMetrics3.LastDiagnosticCount <= 256 && _codeEdit.GetAllErrors().Count == validationMetrics3.LastDiagnosticCount && item9 < 120.0 && validationMetrics3.MaxApplyMilliseconds < 33;
			Require(validationBoundedApply, $"Validation published an unbounded diagnostic batch (published={validationMetrics3.PublishedResults}, diagnostics={validationMetrics3.LastDiagnosticCount}, frame={item9:F1}ms, apply={validationMetrics3.MaxApplyMilliseconds}ms).");
			Require(_codeEdit.ResetValidationMetricsForProbe(), "Hidden-lifecycle validation metrics could not reset.");
			_codeEdit.SetAllText(largeValidationSource);
			_codeEdit.RequestValidationNow();
			bool hiddenWorkerStarted = await WaitForValidationExecutionStarted(_codeEdit, 300);
			_editor.Hide();
			bool hiddenValidationSettled = await WaitForValidationIdle(_codeEdit, 300);
			XWCodeEdit.ValidationMetrics hiddenMetrics = _codeEdit.GetValidationMetrics();
			_editor.Show();
			await WaitFrames(2);
			bool hiddenCanceled = hiddenWorkerStarted && hiddenValidationSettled && hiddenMetrics.CanceledChecks >= 1 && hiddenMetrics.PublishedResults == 0 && hiddenMetrics.ActiveWorkers == 0 && hiddenMetrics.PendingRequests == 0;
			Require(_codeEdit.ResetValidationMetricsForProbe(), "Resumed validation metrics could not reset.");
			_codeEdit.SetAllText("public class ResumedValidationProbe { }\n");
			_codeEdit.RequestValidationNow();
			bool flag7 = await WaitForValidationIdle(_codeEdit, 300);
			XWCodeEdit.ValidationMetrics validationMetrics4 = _codeEdit.GetValidationMetrics();
			bool validationHiddenCanceled = hiddenCanceled && flag7 && validationMetrics4.PublishedResults == 1 && validationMetrics4.ActiveWorkers == 0;
			Require(validationHiddenCanceled, $"Hidden validation work did not cancel/recover cleanly (canceled={hiddenMetrics.CanceledChecks}, hiddenPublished={hiddenMetrics.PublishedResults}, resumedPublished={validationMetrics4.PublishedResults}).");
			XWCodeEdit disposableCodeEdit = completionCodeEditScene?.Instantiate<XWCodeEdit>(PackedScene.GenEditState.Disabled);
			Require(GodotObject.IsInstanceValid(disposableCodeEdit), "Disposable validation editor could not be instantiated.");
			bool validationDestroyedClean = false;
			if (GodotObject.IsInstanceValid(disposableCodeEdit))
			{
				AddChild(disposableCodeEdit, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				disposableCodeEdit.SetFileExtension("cs");
				disposableCodeEdit.ResetValidationMetricsForProbe();
				disposableCodeEdit.SetAllText(largeValidationSource);
				disposableCodeEdit.RequestValidationNow();
				bool destroyCompletionStarted = await WaitForValidationExecutionStarted(disposableCodeEdit, 300);
				disposableCodeEdit.QueueFree();
				bool flag8 = await WaitForValidationIdle(disposableCodeEdit, 300);
				XWCodeEdit.ValidationMetrics validationMetrics5 = disposableCodeEdit.GetValidationMetrics();
				validationDestroyedClean = destroyCompletionStarted && flag8 && validationMetrics5.CanceledChecks >= 1 && validationMetrics5.ActiveWorkers == 0 && validationMetrics5.PendingRequests == 0;
				Require(validationDestroyedClean, $"Destroyed validation editor retained work (canceled={validationMetrics5.CanceledChecks}, active={validationMetrics5.ActiveWorkers}, pending={validationMetrics5.PendingRequests}).");
			}
			_codeEdit.SetAllText("public class ProbeA { }\n");
			_codeEdit.SetCaretLine(0);
			_codeEdit.SetCaretColumn(_codeEdit.GetLine(0).Length);
			_editor.SaveFile();
			string saved = ReadFile(firstPath);
			_codeEdit.InsertAtCursor(" // edit");
			await WaitFrames(1);
			_codeEdit.Undo();
			await WaitFrames(1);
			bool saveUndo = saved.Contains("ProbeA", StringComparison.Ordinal) && !_codeEdit.Text.Contains("// edit", StringComparison.Ordinal);
			Require(saveUndo, "Save followed by CodeEdit undo did not round-trip.");
			_codeEdit.SetAllText("Alpha alpha AlphaBeta\nalpha target\nthird line");
			_editor.ShowSearchBar(showReplace: true);
			LineEdit nodeOrNull = _editor.GetNodeOrNull<LineEdit>("%SearchInput");
			LineEdit nodeOrNull2 = _editor.GetNodeOrNull<LineEdit>("%ReplaceInput");
			Label nodeOrNull3 = _editor.GetNodeOrNull<Label>("%SearchMatchLabel");
			CheckButton nodeOrNull4 = _editor.GetNodeOrNull<CheckButton>("%SearchWholeButton");
			Container nodeOrNull5 = _editor.GetNodeOrNull<Container>("%ReplaceRow");
			bool searchVisual = GodotObject.IsInstanceValid(nodeOrNull) && GodotObject.IsInstanceValid(nodeOrNull2) && GodotObject.IsInstanceValid(nodeOrNull3) && GodotObject.IsInstanceValid(nodeOrNull4) && GodotObject.IsInstanceValid(nodeOrNull5) && nodeOrNull5.Visible;
			if (searchVisual)
			{
				nodeOrNull.Text = "alpha";
				nodeOrNull.EmitSignal(LineEdit.SignalName.TextChanged, "alpha");
				nodeOrNull4.ButtonPressed = true;
				nodeOrNull4.EmitSignal(BaseButton.SignalName.Toggled, true);
				nodeOrNull2.Text = "plant";
			}
			await WaitFrames(2);
			int replaced = (searchVisual ? _editor.ReplaceAllMatches() : 0);
			await WaitFrames(2);
			bool replacedAll = replaced == 3 && _codeEdit.Text == "plant plant AlphaBeta\nplant target\nthird line";
			_codeEdit.Undo();
			await WaitFrames(2);
			bool flag9 = _codeEdit.Text == "Alpha alpha AlphaBeta\nalpha target\nthird line";
			bool searchReplace = searchVisual && replacedAll && flag9;
			Require(searchReplace, "Visual whole-word search/replace did not replace three matches as one undo operation.");
			_editor.ShowGoToLineDialog();
			SpinBox goToLine = _editor.GetNodeOrNull<SpinBox>("%GoToLineSpin");
			if (GodotObject.IsInstanceValid(goToLine))
			{
				goToLine.Value = 3.0;
			}
			_editor.CommitGoToLine();
			_editor.GetNodeOrNull<ConfirmationDialog>("%GoToLineDialog")?.Hide();
			await WaitFrames(1);
			bool goToLineWorks = GodotObject.IsInstanceValid(goToLine) && _codeEdit.GetCaretLine() == 2;
			Require(goToLineWorks, "Visual go-to-line dialog did not move the real editor caret.");
			Button nodeOrNull6 = _editor.GetNodeOrNull<Button>("%ValidateButton");
			Button nodeOrNull7 = _editor.GetNodeOrNull<Button>("%PreviousProblemButton");
			Button nodeOrNull8 = _editor.GetNodeOrNull<Button>("%NextProblemButton");
			Button api = _editor.GetNodeOrNull<Button>("%OpenBlueprintButton");
			bool apiEntry = GodotObject.IsInstanceValid(nodeOrNull6) && GodotObject.IsInstanceValid(nodeOrNull7) && GodotObject.IsInstanceValid(nodeOrNull8) && GodotObject.IsInstanceValid(api);
			Require(apiEntry, "Visual validation/navigation/API-puzzle actions are missing.");
			if (GodotObject.IsInstanceValid(api))
			{
				api.EmitSignal(BaseButton.SignalName.Pressed);
			}
			await WaitFrames(2);
			string text = XWScriptResourceDropCodeGenerator.BuildSnippetForPaths(new List<string> { "res://icon.svg" }, "cs", preferFieldDeclaration: true);
			string[] obj4 = new string[2] { "res://Scripts/legacy.gd", "res://Scripts/legacy.lua" };
			bool flag10 = true;
			string[] array = obj4;
			foreach (string path in array)
			{
				flag10 &= string.IsNullOrEmpty(XWScriptResourceDropCodeGenerator.BuildSnippetForPath(path, "cs", preferFieldDeclaration: true));
			}
			bool resourceDrop = !string.IsNullOrWhiteSpace(text) && text.Contains("icon.svg", StringComparison.Ordinal) && flag10 && string.IsNullOrEmpty(XWScriptResourceDropCodeGenerator.BuildSnippetForPath("res://icon.svg", "gd", preferFieldDeclaration: true));
			Require(resourceDrop, "Resource drag/drop did not generate C# safely or accepted a non-C# source file.");
			string unsavedPath = "user://mod_editor_script_unsaved_probe.cs";
			WriteFile(unsavedPath, "public class UnsavedOriginal { }\n");
			_editor.OpenFile(unsavedPath);
			await WaitFrames(2);
			_codeEdit.SetAllText("public class UnsavedCanceledEdit { }\n");
			_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			ConfirmationDialog unsavedDialog = _editor.GetNodeOrNull<ConfirmationDialog>("%UnsavedChangesDialog");
			Button button = FindButtonByName(unsavedDialog, "DiscardUnsavedButton");
			bool unsavedVisual = GodotObject.IsInstanceValid(unsavedDialog) && unsavedDialog.Visible && GodotObject.IsInstanceValid(button) && unsavedDialog.GetOkButton().Text == "保存并关闭" && unsavedDialog.GetCancelButton().Text == "取消";
			GD.Print($"[MOD_EDITOR_SCRIPT_UNSAVED_DETAIL] visible={unsavedDialog?.Visible} ok={unsavedDialog?.GetOkButton()?.Text} cancel={unsavedDialog?.GetCancelButton()?.Text} discard={button?.Text} current={_editor.GetCurrentFilePath()}");
			Require(unsavedVisual, "Unsaved script dialog does not expose save, discard, and cancel choices.");
			unsavedDialog?.GetCancelButton()?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			bool unsavedCancel = _editor.GetCurrentFilePath() == unsavedPath && ReadFile(unsavedPath) == "public class UnsavedOriginal { }\n" && !unsavedDialog.Visible;
			GD.Print($"[MOD_EDITOR_SCRIPT_UNSAVED_CANCEL_DETAIL] visible={unsavedDialog.Visible} current={_editor.GetCurrentFilePath()} disk={ReadFile(unsavedPath).Trim()}");
			Require(unsavedCancel, "Cancel did not keep the dirty script open without writing it.");
			_codeEdit.SetAllText("public class UnsavedDiscardedEdit { }\n");
			_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			FindButtonByName(unsavedDialog, "DiscardUnsavedButton")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			bool unsavedDiscard = _editor.GetCurrentFilePath() != unsavedPath && ReadFile(unsavedPath) == "public class UnsavedOriginal { }\n";
			Require(unsavedDiscard, "Discard did not close the script while preserving the original file.");
			_editor.OpenFile(unsavedPath);
			await WaitFrames(2);
			_codeEdit.SetAllText("public class UnsavedSavedEdit { }\n");
			_codeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			unsavedDialog.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			bool unsavedSave = _editor.GetCurrentFilePath() != unsavedPath && ReadFile(unsavedPath) == "public class UnsavedSavedEdit { }\n";
			GD.Print($"[MOD_EDITOR_SCRIPT_UNSAVED_SAVE_DETAIL] visible={unsavedDialog.Visible} current={_editor.GetCurrentFilePath()} disk={ReadFile(unsavedPath).Trim()}");
			Require(unsavedSave, "Save and close did not persist the dirty script before closing it.");
			FileDialog newDialog = _editor.GetNode<FileDialog>("%NewFileDialog");
			FileDialog node = _editor.GetNode<FileDialog>("%OpenFileDialog");
			FileDialog saveAsDialog = _editor.GetNode<FileDialog>("%SaveAsDialog");
			bool dialogFilters = HasOnlyCSharpFilter(newDialog) && HasOnlyCSharpFilter(node) && HasOnlyCSharpFilter(saveAsDialog);
			string text2 = "user://mod_editor_csharp_only_probe.cs";
			WriteFile(text2, "public class CSharpOnlyProbe { }\n");
			bool cSharpOpened = _editor.TryOpenFile(text2);
			await WaitFrames(2);
			int tabCount = tabs.TabCount;
			string currentFilePath = _editor.GetCurrentFilePath();
			int unsupportedScriptRequestCount = _editor.UnsupportedScriptRequestCount;
			string[] array2 = new string[7] { "user://mod_editor_script_probe.gd", "user://mod_editor_script_probe.json", "user://mod_editor_script_probe.txt", "user://mod_editor_script_probe.lua", "user://mod_editor_script_probe.py", "user://mod_editor_script_probe.js", "user://mod_editor_script_probe.ts" };
			array = array2;
			foreach (string text3 in array)
			{
				WriteFile(text3, "unsupported\n");
				Require(!_editor.TryOpenFile(text3), "Non-C# file entered script editor: " + text3);
			}
			bool unsupportedRejected = _editor.UnsupportedScriptRequestCount == unsupportedScriptRequestCount + array2.Length && tabs.TabCount == tabCount && _editor.GetCurrentFilePath() == currentFilePath;
			bool cSharpOnly = dialogFilters && cSharpOpened && unsupportedRejected;
			Require(cSharpOnly, "C#-only file filters or centralized open rejection failed.");
			string text4 = "user://mod_editor_script_template_probe";
			string templatePath = text4 + ".cs";
			int templateBefore = _editor.CSharpTemplateCreateCount;
			newDialog.EmitSignal(FileDialog.SignalName.FileSelected, text4);
			await WaitFrames(3);
			string text5 = ReadFile(templatePath);
			bool template = _editor.CSharpTemplateCreateCount == templateBefore + 1 && _editor.GetCurrentFilePath() == templatePath && text5.Contains("using Godot;", StringComparison.Ordinal) && text5.Contains("public partial class mod_editor_script_template_probe : Node", StringComparison.Ordinal);
			string[] array3 = new string[9] { "feature-csharp", "process-csharp", "character-component", "character-csharp", "projectile-csharp", "shared-csharp", "character-event-csharp", "shovel-event-csharp", "card-event-csharp" };
			string text6 = ProjectSettings.GlobalizePath("user://mod_editor_script_template_catalog_probe_" + Guid.NewGuid().ToString("N"));
			XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
			bool flag11 = true;
			try
			{
				Directory.CreateDirectory(text6);
				array = array3;
				foreach (string text7 in array)
				{
					XWTemplateLibrary.TemplateInfo templateInfo = xWTemplateLibrary.FindTemplate(text7);
					flag11 &= templateInfo != null && templateInfo.FileExtension == ".cs";
					if (templateInfo == null)
					{
						continue;
					}
					string directoryPath = Path.Combine(text6, text7);
					XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplateInDirectory(text7, directoryPath, "CSharpOnlyTemplateProbe");
					bool flag12 = false;
					bool flag13 = false;
					foreach (string createdPath in templateCreateResult.CreatedPaths)
					{
						string extension = Path.GetExtension(createdPath);
						flag12 |= extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
						flag13 |= extension.Equals(".gd", StringComparison.OrdinalIgnoreCase) || extension.Equals(".lua", StringComparison.OrdinalIgnoreCase) || extension.Equals(".py", StringComparison.OrdinalIgnoreCase) || extension.Equals(".js", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ts", StringComparison.OrdinalIgnoreCase);
					}
					flag11 &= templateCreateResult.Success && flag12 && !flag13;
				}
			}
			finally
			{
				if (Directory.Exists(text6))
				{
					Directory.Delete(text6, recursive: true);
				}
			}
			template = template && flag11;
			Require(template, "Default C# creation did not normalize the extension or create a valid Godot template.");
			string rejectedSavePath = "user://mod_editor_save_as_rejected.json";
			string beforeRejectedSave = _editor.GetCurrentFilePath();
			int saveRejectedBefore = _editor.UnsupportedSaveAsRequestCount;
			saveAsDialog.EmitSignal(FileDialog.SignalName.FileSelected, rejectedSavePath);
			await WaitFrames(2);
			bool saveAsRejected = _editor.UnsupportedSaveAsRequestCount == saveRejectedBefore + 1 && _editor.GetCurrentFilePath() == beforeRejectedSave && !Godot.FileAccess.FileExists(rejectedSavePath);
			Require(saveAsRejected, "Save As accepted an explicit non-C# extension.");
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node inspectorSentinel = new Node
			{
				Name = "CSharpBoundaryInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			XWBPEditor blueprintEditor = XWEditorInterface.Instance?.GetBlueprintEditor() as XWBPEditor;
			string bridgeRoot = projectRoot.PathJoin("blueprint_bridge_" + Guid.NewGuid().ToString("N"));
			XWModProjectLayout.EnsureProjectLayout(bridgeRoot);
			int value = _editor.SaveAllTabs();
			bool flag14 = _editor.TrySwitchProjectRoot(bridgeRoot);
			bool flag15 = GodotObject.IsInstanceValid(blueprintEditor) && blueprintEditor.TrySwitchProjectRoot(bridgeRoot);
			bool bridgeRootsReady = flag14 && flag15;
			GD.Print($"[MOD_EDITOR_SCRIPT_BLUEPRINT_ROOT_DETAIL] saved={value} script={flag14} blueprint={flag15} root={bridgeRoot}");
			Require(bridgeRootsReady, "Script/Blueprint editors could not establish an isolated Mod root.");
			string blueprintPath = bridgeRoot.PathJoin("Blueprints").PathJoin("BridgeProbe.tres");
			string generatedPath = bridgeRoot.PathJoin("Blueprints").PathJoin("BridgeProbe.generated.cs");
			string createdBlueprintPath = "";
			XWBlueprintCreateDialog blueprintDialog = XWBlueprintCreateDialog.Create();
			blueprintDialog.Config("Node2D", blueprintPath);
			blueprintDialog.BlueprintCreated += delegate(string text25)
			{
				createdBlueprintPath = text25;
			};
			AddChild(blueprintDialog, forceReadableName: false, InternalMode.Disabled);
			blueprintDialog.PopupCentered();
			await WaitFrames(3);
			Button nodeOrNull9 = blueprintDialog.GetNodeOrNull<Button>("%NodeParentCard");
			Button nodeOrNull10 = blueprintDialog.GetNodeOrNull<Button>("%Node2DParentCard");
			Button nodeOrNull11 = blueprintDialog.GetNodeOrNull<Button>("%ControlParentCard");
			Button nodeOrNull12 = blueprintDialog.GetNodeOrNull<Button>("%ResourceParentCard");
			Button nodeOrNull13 = blueprintDialog.GetNodeOrNull<Button>("%ParentSearchBtn");
			Button nodeOrNull14 = blueprintDialog.GetNodeOrNull<Button>("%PathBtn");
			bool blueprintIconEntry = GodotObject.IsInstanceValid(api) && api.Icon != null;
			bool createDialogVisual = blueprintDialog.Visible && GodotObject.IsInstanceValid(nodeOrNull9) && nodeOrNull9.Icon != null && GodotObject.IsInstanceValid(nodeOrNull10) && nodeOrNull10.Icon != null && GodotObject.IsInstanceValid(nodeOrNull11) && nodeOrNull11.Icon != null && GodotObject.IsInstanceValid(nodeOrNull12) && nodeOrNull12.Icon != null && GodotObject.IsInstanceValid(nodeOrNull13) && nodeOrNull13.Icon != null && GodotObject.IsInstanceValid(nodeOrNull14) && nodeOrNull14.Icon != null && !blueprintDialog.GetOkButton().Disabled;
			Require(blueprintIconEntry, "Script workspace Blueprint entry is missing its visual icon.");
			Require(createDialogVisual, "Blueprint creation dialog did not expose usable icon cards and actions.");
			blueprintDialog.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(5);
			bool blueprintCreated = Godot.FileAccess.FileExists(blueprintPath) && string.Equals(createdBlueprintPath.Replace('\\', '/'), blueprintPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
			Require(blueprintCreated, "Visual Blueprint creation did not create the requested XWBPScript.");
			XWBPScript xWBPScript = (blueprintCreated ? ResourceLoader.Load<XWBPScript>(blueprintPath, "", ResourceLoader.CacheMode.Replace) : null);
			bool flag16 = GodotObject.IsInstanceValid(xWBPScript) && bridgeRootsReady;
			if (flag16)
			{
				blueprintEditor.Init(xWBPScript);
				blueprintEditor.BpScriptData.ExtendsClass = new StringName("Control");
			}
			bool blueprintSaved = flag16 && blueprintEditor.FlushBlueprintPersistence();
			XWBPScript xWBPScript2 = (blueprintSaved ? ResourceLoader.Load<XWBPScript>(blueprintPath, "", ResourceLoader.CacheMode.Replace) : null);
			bool blueprintReopened = GodotObject.IsInstanceValid(xWBPScript2) && xWBPScript2.ExtendsClass == new StringName("Control") && xWBPScript2.Graphs.Count > 0;
			Require(blueprintSaved, "Blueprint editor could not persist the real visual document.");
			Require(blueprintReopened, "Persisted Blueprint could not be reopened with its edited data.");
			if (blueprintReopened)
			{
				blueprintEditor.Init(xWBPScript2);
				blueprintEditor.GenerateScriptButtonPressed();
			}
			await WaitFrames(6);
			PanelContainer nodeOrNull15 = _editor.GetNodeOrNull<PanelContainer>("%GeneratedBlueprintBar");
			Button returnToBlueprint = _editor.GetNodeOrNull<Button>("%ReturnToBlueprintButton");
			bool generatedOpened = Godot.FileAccess.FileExists(generatedPath) && string.Equals(_editor.GetCurrentFilePath().Replace('\\', '/'), generatedPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
			bool generatedReadOnly = generatedOpened && _editor.IsCurrentDocumentReadOnly && _editor.CurrentGeneratedLinkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Linked && !_codeEdit.Editable && GodotObject.IsInstanceValid(nodeOrNull15) && nodeOrNull15.Visible;
			Require(generatedOpened, "Blueprint generation did not open the deterministic C# artifact.");
			Require(generatedReadOnly, "Generated Blueprint C# did not open as a linked read-only document.");
			string generatedDiskBefore = ReadFile(generatedPath);
			string generatedEditorBefore = _codeEdit.Text;
			_codeEdit.SetAllText("public class ForbiddenGeneratedMutation { }\n");
			_codeEdit.SetCaretLine(0);
			_codeEdit.SetCaretColumn(0);
			_codeEdit.InsertAtCursor("// forbidden\n");
			bool generatedSaveRejected = !_editor.SaveFile();
			await WaitFrames(2);
			bool generatedWriteBlocked = generatedSaveRejected && _codeEdit.Text == generatedEditorBefore && ReadFile(generatedPath) == generatedDiskBefore;
			Require(generatedWriteBlocked, "Generated Blueprint C# changed through editor write paths.");
			_editor.CloseCurrentTab();
			await WaitFrames(3);
			bool generatedReopened = _editor.TryOpenFile(generatedPath);
			await WaitFrames(3);
			bool generatedReopenProtected = generatedReopened && _editor.IsCurrentDocumentReadOnly && !_codeEdit.Editable && ReadFile(generatedPath) == generatedDiskBefore;
			bool returnIcon = GodotObject.IsInstanceValid(returnToBlueprint) && returnToBlueprint.Icon != null && returnToBlueprint.Visible && !returnToBlueprint.Disabled;
			Require(generatedReopenProtected, "Reopened Blueprint C# lost its read-only provenance protection.");
			Require(returnIcon, "Generated C# does not expose an enabled visual return-to-Blueprint action.");
			if (returnIcon)
			{
				returnToBlueprint.EmitSignal(BaseButton.SignalName.Pressed);
			}
			await WaitFrames(4);
			bool returnedToBlueprint = GodotObject.IsInstanceValid(blueprintEditor) && blueprintEditor.IsVisibleInTree() && GodotObject.IsInstanceValid(blueprintEditor.BpScript) && string.Equals(blueprintEditor.BpScript.ResourcePath.Replace('\\', '/'), blueprintPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			string text8 = bridgeRoot.PathJoin("Scripts").PathJoin("Manual.generated.cs");
			WriteFile(text8, "public class ManualGeneratedNameProbe { }\n");
			bool flag17 = _editor.TryOpenFile(text8);
			_codeEdit.SetAllText("public class ManualGeneratedEditableProbe { }\n");
			bool flag18 = _editor.SaveFile();
			bool manualGeneratedEditable = flag17 && !_editor.IsCurrentDocumentReadOnly && _codeEdit.Editable && flag18 && ReadFile(text8) == "public class ManualGeneratedEditableProbe { }\n";
			Require(manualGeneratedEditable, "A normal user file ending in .generated.cs was incorrectly locked.");
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			string text9 = bridgeRoot.PathJoin("Blueprints").PathJoin("Orphan.generated.cs");
			string blueprintSourcePath = bridgeRoot.PathJoin("Blueprints").PathJoin("Orphan.tres");
			string content = XWBlueprintGeneratedCSharpPolicy.AttachMetadata("public class OrphanGeneratedProbe { }\n", blueprintSourcePath, text9);
			WriteFile(text9, content);
			bool flag19 = _editor.TryOpenFile(text9);
			string text10 = ReadFile(text9);
			_codeEdit.SetAllText("public class ForbiddenOrphanEdit { }\n");
			bool orphanProtected = flag19 && _editor.CurrentGeneratedLinkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Orphaned && _editor.IsCurrentDocumentReadOnly && !_codeEdit.Editable && ReadFile(text9) == text10 && returnToBlueprint.Disabled;
			Require(orphanProtected, "An orphaned generated file became writable or exposed an unsafe source link.");
			_editor.CloseCurrentTab();
			await WaitFrames(2);
			string text11 = projectRoot.PathJoin("foreign_blueprint_" + Guid.NewGuid().ToString("N") + ".tres");
			XWBlueprintCreationService.Result result = XWBlueprintCreationService.Create(text11);
			string text12 = bridgeRoot.PathJoin("Blueprints").PathJoin("Foreign.generated.cs");
			string content2 = XWBlueprintGeneratedCSharpPolicy.AttachMetadata("public class ForeignGeneratedProbe { }\n", text11, text12);
			WriteFile(text12, content2);
			bool flag20 = _editor.TryOpenFile(text12);
			bool flag21 = result.Success && flag20 && _editor.CurrentGeneratedLinkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Orphaned && _editor.IsCurrentDocumentReadOnly && !_codeEdit.Editable && returnToBlueprint.Disabled;
			Require(flag21, "A generated file linked outside the active Mod escaped read-only isolation.");
			bool flag22 = !string.Equals(XWBPCodeGenerator.BuildGeneratedClassName(blueprintPath, bridgeRoot), XWBPCodeGenerator.BuildGeneratedClassName(bridgeRoot.PathJoin("Other").PathJoin("BridgeProbe.tres"), bridgeRoot), StringComparison.Ordinal);
			Require(flag22, "Blueprints with the same name in different folders generated a colliding C# class.");
			bool flag23 = inspector == null || inspector.CurrentObject == inspectorSentinel;
			Require(returnedToBlueprint, "Generated C# visual return action did not reopen its source Blueprint.");
			Require(flag23, "C#/Blueprint bridge replaced the main raw Inspector target.");
			bool flag24 = blueprintIconEntry && createDialogVisual && blueprintCreated && blueprintSaved && blueprintReopened && generatedOpened && generatedReadOnly && generatedWriteBlocked && generatedReopenProtected && returnIcon && returnedToBlueprint && manualGeneratedEditable && orphanProtected && flag21 && flag22 && flag23;
			GD.Print($"[MOD_EDITOR_SCRIPT_BLUEPRINT_BRIDGE_PROBE] window={window} blueprintIconEntry={blueprintIconEntry} createDialogVisual={createDialogVisual} blueprintCreated={blueprintCreated} blueprintSaved={blueprintSaved} blueprintReopened={blueprintReopened} generatedOpened={generatedOpened} generatedReadOnly={generatedReadOnly} generatedWriteBlocked={generatedWriteBlocked} generatedReopenProtected={generatedReopenProtected} returnIcon={returnIcon} returnedToBlueprint={returnedToBlueprint} manualGeneratedEditable={manualGeneratedEditable} orphanProtected={orphanProtected} crossModProtected={flag21} generatedClassUnique={flag22} inspectorUntouched={flag23} failures={((!flag24) ? 1 : 0)}");
			bool policyBoundary = XWCSharpOnlyPolicy.IsSupportedCSharpScriptPath("res://Scripts/Feature.cs") && !XWCSharpOnlyPolicy.IsSupportedCSharpScriptPath("res://Scripts/Feature.gd") && XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Scripts/Feature.gd") && XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Scripts/Feature.lua") && XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Scripts/Feature.py") && XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Scripts/Feature.js") && XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Scripts/Feature.ts") && !XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Shaders/card.gdshader") && !XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Blueprints/card.tres") && !XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath("res://Assets/card.png");
			Require(policyBoundary, "Shared C#-only policy rejected a supported Mod resource or accepted a non-C# source.");
			bool levelBoundary = XWLevelVisualResourceEditor.IsBattleOptionFile("Feature.cs") && XWLevelVisualResourceEditor.IsBattleOptionFile("Feature.tres") && XWLevelVisualResourceEditor.IsBattleOptionFile("Feature.res") && !XWLevelVisualResourceEditor.IsBattleOptionFile("Feature.gd") && !XWLevelVisualResourceEditor.IsBattleOptionFile("Feature.lua");
			Require(levelBoundary, "Level battle options are not restricted to C# and Blueprint resources.");
			XWBPNodeLoadResource xWBPNodeLoadResource = XWBPNodeLoadResource.CreateForPath("res://Tests/ModEditorScriptEditorRuntimeProbe.cs");
			XWBPNodeLoadResource xWBPNodeLoadResource2 = XWBPNodeLoadResource.CreateForPath("res://icon.svg");
			XWBPNodeLoadResource xWBPNodeLoadResource3 = XWBPNodeLoadResource.CreateForPath("res://Tests/AttackComponentResourceLoad.Tests.gd");
			XWBPNodeData node2 = new XWBPNodeLoadResource
			{
				ResourcePath = "res://Tests/AttackComponentResourceLoad.Tests.gd",
				ResourceClass = "Script"
			}.CreateNodeData();
			string outputValue = new XWBPNodeLoadResource().GetOutputValue(null, node2, "资源", null);
			bool blueprintBoundary = xWBPNodeLoadResource != null && xWBPNodeLoadResource.ResourceClass == "Script" && xWBPNodeLoadResource2 != null && xWBPNodeLoadResource3 == null && outputValue == "null";
			Require(blueprintBoundary, "Blueprint resource nodes accepted GDScript or rejected a supported C#/visual resource.");
			XWResourcePicker scriptPicker = XWResourcePicker.Create();
			AddChild(scriptPicker, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(2);
			scriptPicker.Setup("Script");
			Variant data = BuildFileDropData("res://Tests/ModEditorScriptEditorRuntimeProbe.cs");
			Variant data2 = BuildFileDropData("res://Tests/AttackComponentResourceLoad.Tests.gd");
			Variant data3 = BuildResourceDropData(ResourceLoader.Load<Script>("res://Tests/AttackComponentResourceLoad.Tests.gd", null, ResourceLoader.CacheMode.Reuse));
			bool flag25 = scriptPicker._CanDropData(Vector2.Zero, data) && !scriptPicker._CanDropData(Vector2.Zero, data2) && !scriptPicker._CanDropData(Vector2.Zero, data3);
			scriptPicker._DropData(Vector2.Zero, data2);
			flag25 &= !GodotObject.IsInstanceValid(scriptPicker.GetEditedResource());
			Require(flag25, "Resource picker accepted GDScript or rejected a C# Script.");
			scriptPicker.QueueFree();
			string text13 = ProjectSettings.GlobalizePath("user://mod_editor_csharp_boundary_project_" + Guid.NewGuid().ToString("N"));
			string text14 = ProjectSettings.GlobalizePath("user://mod_editor_csharp_boundary_external_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(text13);
			Directory.CreateDirectory(text14);
			XWModProjectLayout.EnsureProjectLayout(text13);
			XWFileSystem.GetSingleton().SetProjectFolderPath(text13);
			string text15 = Path.Combine(text14, "AllowedFeature.cs");
			string text16 = Path.Combine(text14, "AllowedIcon.png");
			File.WriteAllText(text15, "public class AllowedFeature { }\n");
			File.WriteAllBytes(text16, new byte[4] { 1, 2, 3, 4 });
			string[] obj5 = new string[5] { ".gd", ".lua", ".py", ".js", ".ts" };
			bool flag26 = true;
			array = obj5;
			foreach (string text17 in array)
			{
				string text18 = Path.Combine(text14, "Rejected" + text17);
				File.WriteAllText(text18, "unsupported\n");
				flag26 &= XWFileSystemDropHelper.ImportExternalFilesToBestDirectory(new string[1] { text18 }, text13).Count == 0;
				flag26 &= Directory.GetFiles(text13, Path.GetFileName(text18), SearchOption.AllDirectories).Length == 0;
			}
			bool flag27 = XWFileSystemDropHelper.ImportExternalFilesToBestDirectory(new string[2] { text15, text16 }, text13).Count == 2 && File.Exists(Path.Combine(text13, "Scripts", "AllowedFeature.cs")) && File.Exists(Path.Combine(text13, "Assets", "Images", "AllowedIcon.png"));
			string text19 = Path.Combine(text14, "RejectedDirectory");
			Directory.CreateDirectory(text19);
			File.WriteAllText(Path.Combine(text19, "Nested.gd"), "extends Node\n");
			bool flag28 = XWFileSystemDropHelper.ImportExternalFilesToDirectory(new string[1] { text19 }, Path.Combine(text13, "Assets")).Count == 0;
			bool flag29 = XWModProjectLayout.IsDropAllowedInDirectory(text15, Path.Combine(text13, "Battle", "Features"), text13) && !XWModProjectLayout.IsDropAllowedInDirectory(Path.Combine(text14, "Rejected.gd"), Path.Combine(text13, "Battle", "Features"), text13);
			string text20 = Path.Combine(text13, "Resources", "CharacterComponents");
			string text21 = Path.Combine(text20, "ProbeComponent");
			Directory.CreateDirectory(text21);
			string text22 = Path.Combine(text21, "Nested");
			Directory.CreateDirectory(text22);
			string text23 = Path.Combine(text14, "AllowedComponent.tres");
			string text24 = Path.Combine(text14, "RejectedAttack.tres");
			File.WriteAllText(text23, "[gd_resource type=\"Resource\" script_class=\"CharacterComponentDefinition\"]\n");
			File.WriteAllText(text24, "[gd_resource type=\"Resource\" script_class=\"AttackConfig\"]\n");
			bool flag30 = !XWModProjectLayout.IsDropAllowedInDirectory(text15, text20, text13) && XWModProjectLayout.IsDropAllowedInDirectory(text15, text21, text13) && XWModProjectLayout.IsDropAllowedInDirectory(text23, text21, text13) && !XWModProjectLayout.IsDropAllowedInDirectory(text24, text21, text13) && !XWModProjectLayout.IsDropAllowedInDirectory(text15, text22, text13) && !XWModProjectLayout.IsDropAllowedInDirectory(Path.Combine(text14, "Rejected.gd"), text21, text13) && !XWModProjectLayout.IsDropAllowedInDirectory(text19, text21, text13);
			bool flag31 = XWFileSystemDropHelper.ImportExternalFilesToDirectory(new string[1] { text15 }, text21).Count == 1 && File.Exists(Path.Combine(text21, "AllowedFeature.cs"));
			bool flag32 = flag26 && flag27 && flag28 && flag29 && flag30 && flag31;
			Require(flag32, "External import bypassed C#-only routing or broke supported C#/image routing.");
			bool flag33 = inspector == null || inspector.CurrentObject == inspectorSentinel;
			Require(flag33, "C# boundary validation replaced the raw Inspector target.");
			try
			{
				if (Directory.Exists(text13))
				{
					Directory.Delete(text13, recursive: true);
				}
				if (Directory.Exists(text14))
				{
					Directory.Delete(text14, recursive: true);
				}
			}
			catch (Exception ex)
			{
				_failures.Add("C# boundary probe cleanup failed: " + ex.Message);
			}
			bool value2 = policyBoundary && levelBoundary && blueprintBoundary && flag25 && flag32 && flag33;
			XWCSharpProjectIndex.IndexMetrics indexMetrics2 = XWCSharpCodeModel.GetIndexMetrics();
			Require(incrementalOnly, $"One edited file did not produce exactly one incremental publication ({incrementalBefore} -> {incrementalAfter}).");
			GD.Print($"[MOD_EDITOR_SCRIPT_PROBE] window={window} cSharpOnly={cSharpOnly} template={template} unsupportedRejected={unsupportedRejected} saveAsRejected={saveAsRejected} globalCSharpBoundary={value2} policyBoundary={policyBoundary} levelBoundary={levelBoundary} blueprintBoundary={blueprintBoundary} pickerBoundary={flag25} externalImportBoundary={flag32} inspectorUntouched={flag33} diagnosticsReplaced={diagnosticsReplaced} largeCompletionResponsive={largeCompletionResponsive} completionLatestOnly={completionLatestOnly} completionSingleFlight={completionSingleFlight} completionMemberScopeClean={completionMemberScopeClean} completionInheritanceClean={completionInheritanceClean} completionAccessSafe={completionAccessSafe} completionCaretSafe={completionCaretSafe} completionHiddenCanceled={completionHiddenCanceled} completionDestroyedClean={completionDestroyedClean} validationLatestOnly={validationLatestOnly} validationSingleFlight={validationSingleFlight} validationSupersededCanceled={validationSupersededCanceled} validationResponsive={validationResponsive} validationBoundedApply={validationBoundedApply} validationHiddenCanceled={validationHiddenCanceled} validationDestroyedClean={validationDestroyedClean} tabLoadClean={tabLoadClean} saveUndo={saveUndo} resourceDrop={resourceDrop} apiEntry={apiEntry} searchVisual={searchVisual} searchReplace={searchReplace} goToLine={goToLineWorks} unsavedVisual={unsavedVisual} unsavedCancel={unsavedCancel} unsavedDiscard={unsavedDiscard} unsavedSave={unsavedSave} fullIndexRuns={indexMetrics2.FullIndexRuns} incrementalPublishes={indexMetrics2.IncrementalPublishes} coalescedRequests={indexMetrics2.CoalescedRequests} uiResponsive={uiResponsive} indexStaleSafe={indexStaleSafe} failures={_failures.Count}");
		}
		catch (Exception ex2)
		{
			_failures.Add(ex2.ToString());
		}
		Finish();
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			_editor = FindNodeOfType<XWScriptEditor>(GetTree().Root);
			if (GodotObject.IsInstanceValid(_editor))
			{
				_codeEdit = _editor.GetNodeOrNull<XWCodeEdit>("%XWCodeEdit");
				if (GodotObject.IsInstanceValid(_codeEdit))
				{
					return true;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForModEditorReady(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Control instance = obj?.GetNodeOrNull<Control>("%LoadingOverlay");
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForDiagnostics(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (_codeEdit.GetAllErrors().Count > 0)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForIncrementalPublish(int previousCount, int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (XWCSharpCodeModel.GetIndexMetrics().IncrementalPublishes > previousCount)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForValidationIdle(XWCodeEdit codeEdit, int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWCodeEdit.ValidationMetrics validationMetrics = codeEdit.GetValidationMetrics();
			if (validationMetrics.ActiveWorkers == 0 && validationMetrics.PendingRequests == 0)
			{
				return true;
			}
			await Task.Delay(1);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForValidationExecutionStarted(XWCodeEdit codeEdit, int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWCodeEdit.ValidationMetrics validationMetrics = codeEdit.GetValidationMetrics();
			if (validationMetrics.ActiveWorkers == 1 && validationMetrics.ExecutedChecks >= 1)
			{
				return true;
			}
			await Task.Delay(1);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<(bool Settled, double MaxFrameMilliseconds)> WaitForValidationIdleWithFrameTime(XWCodeEdit codeEdit, int maxFrames)
	{
		ulong previousTick = Time.GetTicksUsec();
		double maxFrameMilliseconds = 0.0;
		for (int i = 0; i < maxFrames; i++)
		{
			XWCodeEdit.ValidationMetrics validationMetrics = codeEdit.GetValidationMetrics();
			if (validationMetrics.ActiveWorkers == 0 && validationMetrics.PendingRequests == 0)
			{
				return (Settled: true, MaxFrameMilliseconds: maxFrameMilliseconds);
			}
			await Task.Delay(1);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ulong ticksUsec = Time.GetTicksUsec();
			maxFrameMilliseconds = Math.Max(maxFrameMilliseconds, (double)(ticksUsec - previousTick) / 1000.0);
			previousTick = ticksUsec;
		}
		return (Settled: false, MaxFrameMilliseconds: maxFrameMilliseconds);
	}

	private async Task<(bool Settled, double MaxFrameMilliseconds)> WaitForCompletionIdle(XWCodeEdit codeEdit, int maxFrames)
	{
		ulong previousTick = Time.GetTicksUsec();
		double maxFrameMilliseconds = 0.0;
		for (int i = 0; i < maxFrames; i++)
		{
			XWCodeEdit.CompletionMetrics completionMetrics = codeEdit.GetCompletionMetrics();
			if (completionMetrics.ActiveWorkers == 0 && completionMetrics.PendingRequests == 0)
			{
				return (Settled: true, MaxFrameMilliseconds: maxFrameMilliseconds);
			}
			await Task.Delay(1);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ulong ticksUsec = Time.GetTicksUsec();
			maxFrameMilliseconds = Math.Max(maxFrameMilliseconds, (double)(ticksUsec - previousTick) / 1000.0);
			previousTick = ticksUsec;
		}
		return (Settled: false, MaxFrameMilliseconds: maxFrameMilliseconds);
	}

	private async Task<bool> WaitForCompletionExecutionStarted(XWCodeEdit codeEdit, int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWCodeEdit.CompletionMetrics completionMetrics = codeEdit.GetCompletionMetrics();
			if (completionMetrics.ActiveWorkers == 1 && completionMetrics.WorkerEnteredAnalyses >= 1 && completionMetrics.ActiveAnalysisWorkers == 1)
			{
				return true;
			}
			await Task.Delay(1);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
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
			if (val != null)
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

	private static Button FindButtonByName(Node root, string name)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is Button result && root.Name == (StringName)name)
		{
			return result;
		}
		foreach (Node child in root.GetChildren(includeInternal: true))
		{
			Button button = FindButtonByName(child, name);
			if (GodotObject.IsInstanceValid(button))
			{
				return button;
			}
		}
		return null;
	}

	private static bool HasOnlyCSharpFilter(FileDialog dialog)
	{
		if (GodotObject.IsInstanceValid(dialog) && dialog.Filters.Length == 1)
		{
			return dialog.Filters[0] == "*.cs ; C# Script";
		}
		return false;
	}

	private static void WriteFile(string path, string content)
	{
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		fileAccess?.StoreString(content);
	}

	private static string ReadFile(string path)
	{
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		return fileAccess?.GetAsText() ?? "";
	}

	private static Variant BuildFileDropData(string path)
	{
		Dictionary dictionary = new Dictionary();
		Variant key = "files";
		dictionary[key] = new string[1] { path };
		Dictionary from = dictionary;
		return Variant.From(in from);
	}

	private static Variant BuildResourceDropData(Resource resource)
	{
		Dictionary from = new Dictionary { ["resource"] = resource };
		return Variant.From(in from);
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SCRIPT_PROBE_FAILURE] " + failure);
		}
		if (_failures.Count == 0 && string.Equals(OS.GetEnvironment("MOD_EDITOR_SCRIPT_VISIBLE_VALIDATION"), "1", StringComparison.Ordinal))
		{
			ModProject modProject = ModProject.Create(ProjectSettings.GlobalizePath("user://visible_mod_project"), "CSharpVisibleProbe", "1.0.0", "CodexValidation", "C#-only visible validation");
			ModEditorPanel modEditorPanel = FindNodeOfType<ModEditorPanel>(GetTree().Root);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			int num;
			if (modProject != null && GodotObject.IsInstanceValid(modEditorPanel))
			{
				object obj = method?.Invoke(modEditorPanel, new object[1] { modProject });
				num = ((obj is bool && (bool)obj) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool value = (byte)num != 0;
			if (typeof(ModEditorPanel).GetField("_projectManagerPanel", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(modEditorPanel) is Control control)
			{
				control.Visible = false;
			}
			string text = ((modProject == null) ? "" : Path.Combine(modProject.ProjectPath, "Scripts", "VisibleValidation.cs"));
			if (modProject != null)
			{
				File.WriteAllText(text, "using Godot;\n\npublic partial class VisibleValidation : Node\n{\n}\n");
				XWEditorInterface.Instance?.FocusPanel("script_editor");
				_editor?.TryOpenFile(text);
			}
			GD.Print($"[MOD_EDITOR_SCRIPT_VISIBLE_VALIDATION_READY] workspace=CSharp entered={value}");
		}
		else
		{
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}
}
