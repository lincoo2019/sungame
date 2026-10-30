using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorResourcePerformanceRotationProbe : Node
{
	private sealed class PreparedResource
	{
		public Resource Resource;

		public string Path = "";

		public string TypeName = "";

		public string Attempts = "";
	}

	private sealed class CategoryResult
	{
		public string Category = "";

		public string TypeName = "";

		public bool Opened;

		public bool Saved;

		public bool ClosedReopened;

		public string PreviewAction = "refresh";

		public double ColdOpenMs;

		public double ReopenMs;

		public readonly List<double> ActionSamples = new List<double>();

		public readonly List<string> ActionDiagnostics = new List<string>();

		public readonly List<double> FrameSamples = new List<double>();

		public int HiddenProcessing;

		public int RawHiddenProcessFlags;

		public readonly List<string> HiddenProcessPaths = new List<string>();

		public int LeakedNodes;

		public bool ResourceReleased;

		public WeakRef ResourceWeak;

		public long MemoryBefore;

		public double MemoryDeltaMb;

		public int FailureCount;
	}

	private const int DefaultBatchCount = 4;

	private const double MaxF3StartupMs = 15000.0;

	private const double MaxCategoryFirstOpenMs = 2000.0;

	private const double MaxCategoryReopenMs = 750.0;

	private const double MaxActionP95Ms = 200.0;

	private const double MaxFrameP95Ms = 40.0;

	private const double MaxBatchMemoryDeltaMb = 64.0;

	private readonly List<string> _failures = new List<string>();

	private readonly List<CategoryResult> _results = new List<CategoryResult>();

	private List<Type> _resourceTypes = new List<Type>();

	private int _batchIndex;

	private int _batchCount;

	public override async void _Ready()
	{
		_ = 6;
		try
		{
			_batchIndex = ReadEnvironmentInt("MOD_EDITOR_PERF_BATCH_INDEX", 0);
			_batchCount = Math.Max(1, ReadEnvironmentInt("MOD_EDITOR_PERF_BATCH_COUNT", 4));
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe = this;
			bool condition = _batchIndex >= 0 && _batchIndex < _batchCount;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(26, 2);
			handler.AppendLiteral("Invalid batch selection ");
			handler.AppendFormatted(_batchIndex);
			handler.AppendLiteral("/");
			handler.AppendFormatted(_batchCount);
			handler.AppendLiteral(".");
			modEditorResourcePerformanceRotationProbe.Require(condition, handler.ToStringAndClear());
			IReadOnlyList<XWVisualEditorDescriptor> descriptors = XWResourceEditorRegistry.GetAllEditors();
			Require(descriptors.Count > 0, "Runtime registry contains no resource editor categories.");
			Require(descriptors.Select((XWVisualEditorDescriptor item) => item.Category).Distinct<string>(StringComparer.Ordinal).Count() == descriptors.Count, "Registry contains duplicate category names.");
			List<XWVisualEditorDescriptor> selected = (from item in descriptors.Select((XWVisualEditorDescriptor item, int index) => (Descriptor: item, Index: index))
				where item.Index % _batchCount == _batchIndex
				select item.Descriptor).ToList();
			bool fullSession = _batchCount == 1;
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe2 = this;
			bool condition2 = selected.Count > 0;
			handler = new DefaultInterpolatedStringHandler(39, 1);
			handler.AppendLiteral("Batch ");
			handler.AppendFormatted(_batchIndex);
			handler.AppendLiteral(" selected no resource categories.");
			modEditorResourcePerformanceRotationProbe2.Require(condition2, handler.ToStringAndClear());
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
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			Stopwatch f3Timer = Stopwatch.StartNew();
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
			bool flag = await WaitForEditor(descriptors.Count, 1200);
			f3Timer.Stop();
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe3 = this;
			handler = new DefaultInterpolatedStringHandler(84, 1);
			handler.AppendLiteral("F3 did not initialize the ");
			handler.AppendFormatted(descriptors.Count);
			handler.AppendLiteral("-entry resource palette with zero loaded resource editors.");
			modEditorResourcePerformanceRotationProbe3.Require(flag, handler.ToStringAndClear());
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe4 = this;
			bool condition3 = f3Timer.Elapsed.TotalMilliseconds <= 15000.0;
			handler = new DefaultInterpolatedStringHandler(29, 2);
			handler.AppendLiteral("F3 startup exceeded ");
			handler.AppendFormatted(15000.0, "0");
			handler.AppendLiteral(" ms: ");
			handler.AppendFormatted(f3Timer.Elapsed.TotalMilliseconds, "0.###");
			handler.AppendLiteral(" ms.");
			modEditorResourcePerformanceRotationProbe4.Require(condition3, handler.ToStringAndClear());
			if (!flag)
			{
				Finish();
				return;
			}
			ModEditorPanel editorPanel = XWEditorInterface.Instance.GetEditorPanel() as ModEditorPanel;
			int initialLoadedEditors = editorPanel?.LoadedResourceEditorCount ?? (-1);
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe5 = this;
			bool condition4 = initialLoadedEditors == 0;
			handler = new DefaultInterpolatedStringHandler(52, 1);
			handler.AppendLiteral("F3 startup must load zero resource editors, actual ");
			handler.AppendFormatted(initialLoadedEditors);
			handler.AppendLiteral(".");
			modEditorResourcePerformanceRotationProbe5.Require(condition4, handler.ToStringAndClear());
			(XWEditorInterface.Instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			XWEditorInterface.Instance.FocusPanel("bp_editor");
			await WaitFrames(4);
			_resourceTypes = DiscoverResourceTypes();
			await ForceCollection();
			long batchMemoryBefore = GC.GetTotalMemory(forceFullCollection: true);
			IFormatProvider invariantCulture;
			foreach (XWVisualEditorDescriptor descriptor in selected)
			{
				CategoryResult result;
				try
				{
					result = await RunCategory(descriptor);
				}
				catch (Exception value)
				{
					result = new CategoryResult
					{
						Category = descriptor.Category,
						TypeName = "unavailable",
						FailureCount = 1
					};
					List<string> failures = _failures;
					handler = new DefaultInterpolatedStringHandler(2, 2);
					handler.AppendFormatted(descriptor.Category);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(value);
					failures.Add(handler.ToStringAndClear());
				}
				await ForceCollection();
				Variant? variant = result.ResourceWeak?.GetRef();
				GodotObject instance = (variant.HasValue ? variant.Value.AsGodotObject() : null);
				result.ResourceReleased = !GodotObject.IsInstanceValid(instance);
				result.ResourceWeak?.Dispose();
				result.ResourceWeak = null;
				long totalMemory = GC.GetTotalMemory(forceFullCollection: true);
				result.MemoryDeltaMb = Math.Max(0.0, (double)(totalMemory - result.MemoryBefore) / 1048576.0);
				if (!result.ResourceReleased)
				{
					result.FailureCount++;
					_failures.Add(descriptor.Category + ": resource WeakRef remained alive after final tab close and forced collection.");
				}
				if (result.HiddenProcessing != 0)
				{
					result.FailureCount++;
					List<string> failures2 = _failures;
					handler = new DefaultInterpolatedStringHandler(52, 2);
					handler.AppendFormatted(descriptor.Category);
					handler.AppendLiteral(": hidden editor subtree still has ");
					handler.AppendFormatted(result.HiddenProcessing);
					handler.AppendLiteral(" processing nodes.");
					failures2.Add(handler.ToStringAndClear());
				}
				if (result.LeakedNodes != 0)
				{
					result.FailureCount++;
					List<string> failures3 = _failures;
					handler = new DefaultInterpolatedStringHandler(49, 2);
					handler.AppendFormatted(descriptor.Category);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(result.LeakedNodes);
					handler.AppendLiteral(" transient editor roots survived refresh/close.");
					failures3.Add(handler.ToStringAndClear());
				}
				if (!result.Opened || !result.Saved || !result.ClosedReopened)
				{
					result.FailureCount++;
					List<string> failures4 = _failures;
					handler = new DefaultInterpolatedStringHandler(54, 4);
					handler.AppendFormatted(descriptor.Category);
					handler.AppendLiteral(": lifecycle incomplete opened=");
					handler.AppendFormatted(result.Opened);
					handler.AppendLiteral(" saved=");
					handler.AppendFormatted(result.Saved);
					handler.AppendLiteral(" closedReopened=");
					handler.AppendFormatted(result.ClosedReopened);
					handler.AppendLiteral(".");
					failures4.Add(handler.ToStringAndClear());
				}
				double num = Percentile(result.ActionSamples, 0.95);
				double num2 = Percentile(result.FrameSamples, 0.95);
				if (!fullSession)
				{
					if (result.ColdOpenMs > 2000.0)
					{
						result.FailureCount++;
						List<string> failures5 = _failures;
						handler = new DefaultInterpolatedStringHandler(34, 3);
						handler.AppendFormatted(descriptor.Category);
						handler.AppendLiteral(": first lazy open ");
						handler.AppendFormatted(result.ColdOpenMs, "0.###");
						handler.AppendLiteral(" ms exceeds ");
						handler.AppendFormatted(2000.0, "0");
						handler.AppendLiteral(" ms.");
						failures5.Add(handler.ToStringAndClear());
					}
					if (result.ReopenMs > 750.0)
					{
						result.FailureCount++;
						List<string> failures6 = _failures;
						handler = new DefaultInterpolatedStringHandler(25, 3);
						handler.AppendFormatted(descriptor.Category);
						handler.AppendLiteral(": reopen ");
						handler.AppendFormatted(result.ReopenMs, "0.###");
						handler.AppendLiteral(" ms exceeds ");
						handler.AppendFormatted(750.0, "0");
						handler.AppendLiteral(" ms.");
						failures6.Add(handler.ToStringAndClear());
					}
					if (num > 200.0)
					{
						result.FailureCount++;
						List<string> failures7 = _failures;
						handler = new DefaultInterpolatedStringHandler(29, 3);
						handler.AppendFormatted(descriptor.Category);
						handler.AppendLiteral(": action p95 ");
						handler.AppendFormatted(num, "0.###");
						handler.AppendLiteral(" ms exceeds ");
						handler.AppendFormatted(200.0, "0");
						handler.AppendLiteral(" ms.");
						failures7.Add(handler.ToStringAndClear());
					}
					if (num2 > 40.0)
					{
						result.FailureCount++;
						List<string> failures8 = _failures;
						handler = new DefaultInterpolatedStringHandler(28, 3);
						handler.AppendFormatted(descriptor.Category);
						handler.AppendLiteral(": frame p95 ");
						handler.AppendFormatted(num2, "0.###");
						handler.AppendLiteral(" ms exceeds ");
						handler.AppendFormatted(40.0, "0");
						handler.AppendLiteral(" ms.");
						failures8.Add(handler.ToStringAndClear());
					}
				}
				_results.Add(result);
				foreach (string hiddenProcessPath in result.HiddenProcessPaths)
				{
					GD.Print("[MOD_EDITOR_RESOURCE_PERF_HIDDEN] category=" + result.Category + " node=" + hiddenProcessPath);
				}
				foreach (string actionDiagnostic in result.ActionDiagnostics)
				{
					GD.Print("[MOD_EDITOR_RESOURCE_PERF_ACTION] category=" + result.Category + " " + actionDiagnostic);
				}
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(231, 16, invariantCulture);
				handler.AppendLiteral("[MOD_EDITOR_RESOURCE_PERF_CATEGORY] category=");
				handler.AppendFormatted(result.Category);
				handler.AppendLiteral(" type=");
				handler.AppendFormatted(result.TypeName);
				handler.AppendLiteral(" ");
				handler.AppendLiteral("opened=");
				handler.AppendFormatted(result.Opened);
				handler.AppendLiteral(" saved=");
				handler.AppendFormatted(result.Saved);
				handler.AppendLiteral(" closedReopened=");
				handler.AppendFormatted(result.ClosedReopened);
				handler.AppendLiteral(" ");
				handler.AppendLiteral("previewAction=");
				handler.AppendFormatted(result.PreviewAction);
				handler.AppendLiteral(" coldMs=");
				handler.AppendFormatted(result.ColdOpenMs, "F3");
				handler.AppendLiteral(" reopenMs=");
				handler.AppendFormatted(result.ReopenMs, "F3");
				handler.AppendLiteral(" ");
				handler.AppendLiteral("actionP95Ms=");
				handler.AppendFormatted(num, "F3");
				handler.AppendLiteral(" frameP95Ms=");
				handler.AppendFormatted(num2, "F3");
				handler.AppendLiteral(" ");
				handler.AppendLiteral("memoryDeltaMb=");
				handler.AppendFormatted(result.MemoryDeltaMb, "F3");
				handler.AppendLiteral(" hiddenProcessing=");
				handler.AppendFormatted(result.HiddenProcessing);
				handler.AppendLiteral(" ");
				handler.AppendLiteral("rawHiddenFlags=");
				handler.AppendFormatted(result.RawHiddenProcessFlags);
				handler.AppendLiteral(" ");
				handler.AppendLiteral("leakedEditors=");
				handler.AppendFormatted(result.LeakedNodes);
				handler.AppendLiteral(" weakRefReleased=");
				handler.AppendFormatted(result.ResourceReleased);
				handler.AppendLiteral(" failures=");
				handler.AppendFormatted(result.FailureCount);
				GD.Print(string.Create(provider, ref handler));
			}
			await ForceCollection();
			long totalMemory2 = GC.GetTotalMemory(forceFullCollection: true);
			double num3 = Math.Max(0.0, (double)(totalMemory2 - batchMemoryBefore) / 1048576.0);
			ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe6 = this;
			bool condition5 = num3 <= 64.0;
			handler = new DefaultInterpolatedStringHandler(35, 2);
			handler.AppendLiteral("Batch memory delta ");
			handler.AppendFormatted(num3, "0.###");
			handler.AppendLiteral(" MB exceeds ");
			handler.AppendFormatted(64.0, "0");
			handler.AppendLiteral(" MB.");
			modEditorResourcePerformanceRotationProbe6.Require(condition5, handler.ToStringAndClear());
			double num4 = Percentile(_results.Select((CategoryResult item) => item.ColdOpenMs), 0.95);
			double num5 = Percentile(_results.SelectMany((CategoryResult item) => item.ActionSamples), 0.95);
			double num6 = Percentile(_results.SelectMany((CategoryResult item) => item.FrameSamples), 0.95);
			if (fullSession)
			{
				ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe7 = this;
				bool condition6 = num4 <= 2000.0;
				handler = new DefaultInterpolatedStringHandler(44, 2);
				handler.AppendLiteral("Full-session first-open p95 ");
				handler.AppendFormatted(num4, "0.###");
				handler.AppendLiteral(" ms exceeds ");
				handler.AppendFormatted(2000.0, "0");
				handler.AppendLiteral(" ms.");
				modEditorResourcePerformanceRotationProbe7.Require(condition6, handler.ToStringAndClear());
				ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe8 = this;
				bool condition7 = num5 <= 200.0;
				handler = new DefaultInterpolatedStringHandler(40, 2);
				handler.AppendLiteral("Full-session action p95 ");
				handler.AppendFormatted(num5, "0.###");
				handler.AppendLiteral(" ms exceeds ");
				handler.AppendFormatted(200.0, "0");
				handler.AppendLiteral(" ms.");
				modEditorResourcePerformanceRotationProbe8.Require(condition7, handler.ToStringAndClear());
				ModEditorResourcePerformanceRotationProbe modEditorResourcePerformanceRotationProbe9 = this;
				bool condition8 = num6 <= 40.0;
				handler = new DefaultInterpolatedStringHandler(39, 2);
				handler.AppendLiteral("Full-session frame p95 ");
				handler.AppendFormatted(num6, "0.###");
				handler.AppendLiteral(" ms exceeds ");
				handler.AppendFormatted(40.0, "0");
				handler.AppendLiteral(" ms.");
				modEditorResourcePerformanceRotationProbe9.Require(condition8, handler.ToStringAndClear());
			}
			int value2 = _results.Count((CategoryResult item) => item.Opened);
			int value3 = _results.Sum((CategoryResult item) => item.HiddenProcessing);
			int value4 = _results.Sum((CategoryResult item) => item.LeakedNodes);
			int value5 = _results.Count((CategoryResult item) => !item.ResourceReleased);
			string value6 = string.Join(",", descriptors.Select((XWVisualEditorDescriptor item) => item.Category));
			string value7 = string.Join(",", _results.Select((CategoryResult item) => item.Category));
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new DefaultInterpolatedStringHandler(287, 19, invariantCulture);
			handler.AppendLiteral("[MOD_EDITOR_RESOURCE_PERF_BATCH] batch=");
			handler.AppendFormatted(_batchIndex);
			handler.AppendLiteral(" batches=");
			handler.AppendFormatted(_batchCount);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("fullSession=");
			handler.AppendFormatted(fullSession);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("registryCategories=");
			handler.AppendFormatted(descriptors.Count);
			handler.AppendLiteral(" selected=");
			handler.AppendFormatted(selected.Count);
			handler.AppendLiteral(" opened=");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("initialLoaded=");
			handler.AppendFormatted(initialLoadedEditors);
			handler.AppendLiteral(" loadedEditors=");
			handler.AppendFormatted(editorPanel?.LoadedResourceEditorCount ?? (-1));
			handler.AppendLiteral(" ");
			handler.AppendLiteral("f3StartupMs=");
			handler.AppendFormatted(f3Timer.Elapsed.TotalMilliseconds, "F3");
			handler.AppendLiteral(" startupP95Ms=");
			handler.AppendFormatted(num4, "F3");
			handler.AppendLiteral(" ");
			handler.AppendLiteral("actionP95Ms=");
			handler.AppendFormatted(num5, "F3");
			handler.AppendLiteral(" frameP95Ms=");
			handler.AppendFormatted(num6, "F3");
			handler.AppendLiteral(" memoryDeltaMb=");
			handler.AppendFormatted(num3, "F3");
			handler.AppendLiteral(" ");
			handler.AppendLiteral("hiddenProcessing=");
			handler.AppendFormatted(value3);
			handler.AppendLiteral(" leakedEditors=");
			handler.AppendFormatted(value4);
			handler.AppendLiteral(" leakedResources=");
			handler.AppendFormatted(value5);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("errors=0 failures=");
			handler.AppendFormatted(_failures.Count);
			handler.AppendLiteral(" categories=");
			handler.AppendFormatted(value7);
			handler.AppendLiteral(" manifest=");
			handler.AppendFormatted(value6);
			GD.Print(string.Create(provider2, ref handler));
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<CategoryResult> RunCategory(XWVisualEditorDescriptor descriptor)
	{
		PreparedResource prepared = PrepareResource(descriptor);
		if (!GodotObject.IsInstanceValid(prepared.Resource))
		{
			throw new InvalidOperationException("No instantiable author resource for category " + descriptor.Category + ". attempts=" + prepared.Attempts);
		}
		Resource resource = prepared.Resource;
		CategoryResult result = new CategoryResult
		{
			Category = descriptor.Category,
			TypeName = prepared.TypeName,
			ResourceWeak = GodotObject.WeakRef(resource),
			MemoryBefore = GC.GetTotalMemory(forceFullCollection: false)
		};
		if (!XWResourceEditorRegistry.TryGetEditor(resource, prepared.Path, out var descriptor2) || descriptor2.Category != descriptor.Category)
		{
			throw new InvalidOperationException($"Representative {prepared.TypeName} routed to {descriptor2?.Category ?? "none"} instead of {descriptor.Category}.");
		}
		ModEditorPanel panel = XWEditorInterface.Instance.GetEditorPanel() as ModEditorPanel;
		if (!GodotObject.IsInstanceValid(panel))
		{
			throw new InvalidOperationException("ModEditorPanel is unavailable during resource rotation.");
		}
		int loadedBefore = panel.LoadedResourceEditorCount;
		if (GodotObject.IsInstanceValid(XWEditorInterface.Instance.TryGetLoadedResourceEditor(descriptor.DockKey)))
		{
			throw new InvalidOperationException(descriptor.Category + " was instantiated before its first open.");
		}
		XWResourceEditContext context = XWResourceEditContext.ForRoot(resource, prepared.Path, descriptor.DockKey);
		Stopwatch stopwatch = Stopwatch.StartNew();
		bool openedByRegistry = XWResourceEditorRegistry.TryOpen(resource, prepared.Path, context);
		stopwatch.Stop();
		result.ColdOpenMs = stopwatch.Elapsed.TotalMilliseconds;
		await WaitFrames(3);
		if (!openedByRegistry || panel.LoadedResourceEditorCount != loadedBefore + 1)
		{
			throw new InvalidOperationException($"{descriptor.Category} first registry open did not instantiate exactly one editor: opened={openedByRegistry} before={loadedBefore} after={panel.LoadedResourceEditorCount}.");
		}
		XWGenericVisualResourceEditor editor = XWEditorInterface.Instance.TryGetLoadedResourceEditor(descriptor.DockKey) as XWGenericVisualResourceEditor;
		TabBar tabs = editor?.FindChild("ResourceTabBar", recursive: true, owned: false) as TabBar;
		result.Opened = GodotObject.IsInstanceValid(editor) && editor.IsVisibleInTree() && GodotObject.IsInstanceValid(tabs) && tabs.TabCount == 1;
		if (!result.Opened)
		{
			throw new InvalidOperationException("Editor did not visibly open one tab for " + descriptor.Category + ".");
		}
		List<ulong> refreshRoots = CaptureTransientRootIds(editor);
		for (int iteration = 0; iteration < 3; iteration++)
		{
			RecordAction(result, $"refresh{iteration + 1}", Measure(delegate
			{
				editor.RefreshResourceIfOpen(resource);
			}));
			await WaitFrames(2);
		}
		result.LeakedNodes += CountLiveObjects(refreshRoots);
		(string, double) obj = await ExercisePreviewAction(descriptor, editor, resource);
		string item = obj.Item1;
		double item2 = obj.Item2;
		result.PreviewAction = item;
		RecordAction(result, "preview-" + item, item2);
		List<double> frameSamples = result.FrameSamples;
		frameSamples.AddRange(await SampleFrameTimes(32, 4));
		Button saveButton = FindButtonByText(editor.FindChild("Toolbar", recursive: true, owned: false), "保存");
		if (!GodotObject.IsInstanceValid(saveButton) || saveButton.Disabled)
		{
			throw new InvalidOperationException(descriptor.Category + " has no enabled toolbar Save action for a real path.");
		}
		RecordAction(result, "save", Measure(delegate
		{
			saveButton.EmitSignal(BaseButton.SignalName.Pressed);
		}));
		await WaitFrames(3);
		Resource resource2 = ResourceLoader.Load<Resource>(prepared.Path, "", ResourceLoader.CacheMode.Ignore);
		result.Saved = GodotObject.IsInstanceValid(resource2);
		resource2?.Dispose();
		RecordAction(result, "hide", Measure(delegate
		{
			XWEditorInterface.Instance.FocusPanel("bp_editor");
		}));
		await WaitFrames(3);
		if (editor.IsVisibleInTree())
		{
			throw new InvalidOperationException(descriptor.Category + " remained visible after switching to bp_editor.");
		}
		result.HiddenProcessing = CountProcessingNodes(editor, editor, result.HiddenProcessPaths, out var rawProcessFlags);
		result.RawHiddenProcessFlags = rawProcessFlags;
		RecordAction(result, "show", Measure(delegate
		{
			XWEditorInterface.Instance.FocusPanel(descriptor.DockKey);
		}));
		await WaitFrames(2);
		List<ulong> firstCloseRoots = CaptureTransientRootIds(editor, includeDirectRows: true);
		RecordAction(result, "close1", Measure(delegate
		{
			CloseCurrentTab(tabs);
		}));
		await WaitFrames(4);
		bool firstClosed = tabs.TabCount == 0;
		result.LeakedNodes += CountLiveObjects(firstCloseRoots);
		Stopwatch stopwatch2 = Stopwatch.StartNew();
		XWResourceEditorRegistry.TryOpen(resource, prepared.Path, context);
		stopwatch2.Stop();
		result.ReopenMs = stopwatch2.Elapsed.TotalMilliseconds;
		RecordActionDiagnostic(result, "reopen", result.ReopenMs);
		await WaitFrames(3);
		bool reopened = editor.IsVisibleInTree() && tabs.TabCount == 1;
		List<ulong> finalCloseRoots = CaptureTransientRootIds(editor, includeDirectRows: true);
		RecordAction(result, "close2", Measure(delegate
		{
			CloseCurrentTab(tabs);
		}));
		await WaitFrames(5);
		bool flag = tabs.TabCount == 0;
		result.LeakedNodes += CountLiveObjects(finalCloseRoots);
		result.ClosedReopened = firstClosed && reopened && flag;
		XWEditorInterface.Instance.FocusPanel("bp_editor");
		await WaitFrames(2);
		prepared.Resource = null;
		resource = null;
		return result;
	}

	private PreparedResource PrepareResource(XWVisualEditorDescriptor descriptor)
	{
		string text = $"user://ModEditorResourcePerfRotation/Batch{_batchIndex}";
		string path = ((descriptor.Category == "Dialog") ? (text + "/Resources/Dialogs/PerfDialog.tscn") : $"{text}/{descriptor.Category}/Perf{descriptor.Category}.tres");
		string directoryName = Path.GetDirectoryName(ProjectSettings.GlobalizePath(path));
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		List<string> list = new List<string>();
		foreach (Resource item in CreateCandidateSequence(descriptor, list))
		{
			if (!GodotObject.IsInstanceValid(item))
			{
				continue;
			}
			item.ResourceName = "Perf" + descriptor.Category;
			if (!XWResourceEditorRegistry.TryGetEditor(item, path, out var descriptor2) || descriptor2.Category != descriptor.Category)
			{
				list.Add(item.GetType().Name + ":route=" + (descriptor2?.Category ?? "none"));
				item.Dispose();
				continue;
			}
			Error error = ResourceSaver.Save(item, path, ResourceSaver.SaverFlags.None);
			if (error != Error.Ok)
			{
				list.Add($"{item.GetType().Name}:save={error}");
				item.Dispose();
				continue;
			}
			return new PreparedResource
			{
				Resource = item,
				Path = path,
				TypeName = item.GetType().Name,
				Attempts = string.Join("|", list)
			};
		}
		return new PreparedResource
		{
			Path = path,
			Attempts = string.Join("|", list)
		};
	}

	private IEnumerable<Resource> CreateCandidateSequence(XWVisualEditorDescriptor descriptor, List<string> attempts)
	{
		if (descriptor.Category == "General")
		{
			yield return new LabelSettings
			{
				FontSize = 24
			};
			yield break;
		}
		if (descriptor.Category == "Audio")
		{
			yield return new AudioStreamWav
			{
				MixRate = 22050
			};
			yield break;
		}
		if (descriptor.Category == "Dialog")
		{
			PackedScene packedScene = new PackedScene();
			Control control = new Control
			{
				Name = "PerfDialog"
			};
			Error error = packedScene.Pack(control);
			control.Free();
			if (error == Error.Ok)
			{
				yield return packedScene;
				yield break;
			}
			attempts.Add($"PackedScene:pack={error}");
			packedScene.Dispose();
			yield break;
		}
		foreach (Type item in from item in (from type in _resourceTypes
				select (Type: type, Rank: MatchRank(type, descriptor.ResourceClassNames)) into item
				where item.Rank >= 0
				orderby item.Rank
				select item).ThenBy<(Type, int), string>(((Type Type, int Rank) item) => item.Type.FullName, StringComparer.Ordinal)
			select item.Type)
		{
			Resource resource = null;
			try
			{
				resource = Activator.CreateInstance(item) as Resource;
			}
			catch (Exception ex)
			{
				attempts.Add(item.Name + ":ctor=" + ex.GetType().Name);
			}
			if (GodotObject.IsInstanceValid(resource))
			{
				yield return resource;
			}
		}
	}

	private static int MatchRank(Type type, IReadOnlyList<string> registeredNames)
	{
		for (int i = 0; i < registeredNames.Count; i++)
		{
			int num = 0;
			Type type2 = type;
			while (type2 != null)
			{
				if (type2.Name == registeredNames[i])
				{
					return i * 1000 + num;
				}
				type2 = type2.BaseType;
				num++;
			}
		}
		return -1;
	}

	private static List<Type> DiscoverResourceTypes()
	{
		return (from type in typeof(TowerDefenseBackgroundMusicConfig).Assembly.GetTypes()
			where !type.IsAbstract && !type.ContainsGenericParameters && typeof(Resource).IsAssignableFrom(type)
			select type).OrderBy<Type, string>((Type type) => type.FullName, StringComparer.Ordinal).ToList();
	}

	private unsafe async Task<(string Mode, double Milliseconds)> ExercisePreviewAction(XWVisualEditorDescriptor descriptor, XWGenericVisualResourceEditor editor, Resource resource)
	{
		object obj = descriptor.Category switch
		{
			"Audio" => ("PlayButton", "StopButton"), 
			"Animation" => ("PlayButton", "PlayButton"), 
			"BGM" => ("EntryButton", "StopButton"), 
			"Card" => ("PreviewPlantButton", "ResetRuntimeButton"), 
			"Projectile" => ("PlayButton", ""), 
			"ProjectileChange" => ("RunButton", "RunButton"), 
			"PacketEvent" => ("TriggerButton", "ResetButton"), 
			"PacketCostRule" => ("ApplyButton", "ResetButton"), 
			"PacketOverride" => ("ApplyButton", "ResetButton"), 
			"ToolEvent" => ("ApplyButton", ""), 
			"Survival" => ("NextWavePreviewButton", "ResetPreviewButton"), 
			"Tutorial" => ("RunButton", ""), 
			"CharacterCombat" => ("SimulateButton", ""), 
			"Shovel" => ("UseShovelButton", ""), 
			_ => ("", ""), 
		};
		string item = ((ValueTuple<string, string>*)(&obj))->Item1;
		string item2 = ((ValueTuple<string, string>*)(&obj))->Item2;
		Button start = (string.IsNullOrWhiteSpace(item) ? null : (editor.FindChild(item, recursive: true, owned: false) as Button));
		Button stop = (string.IsNullOrWhiteSpace(item2) ? null : (editor.FindChild(item2, recursive: true, owned: false) as Button));
		if (GodotObject.IsInstanceValid(start) && !start.Disabled && (string.IsNullOrWhiteSpace(item2) || (GodotObject.IsInstanceValid(stop) && !stop.Disabled)))
		{
			double elapsed = Measure(delegate
			{
				start.EmitSignal(BaseButton.SignalName.Pressed);
			});
			await WaitFrames(2);
			elapsed = ((!GodotObject.IsInstanceValid(stop)) ? (elapsed + Measure(delegate
			{
				editor.RefreshResourceIfOpen(resource);
			})) : (elapsed + Measure(delegate
			{
				stop.EmitSignal(BaseButton.SignalName.Pressed);
			})));
			await WaitFrames(2);
			return (Mode: GodotObject.IsInstanceValid(stop) ? "startStop" : "startRefresh", Milliseconds: elapsed);
		}
		double refreshMs = Measure(delegate
		{
			editor.RefreshResourceIfOpen(resource);
		});
		await WaitFrames(2);
		return (Mode: "refresh", Milliseconds: refreshMs);
	}

	private static Button FindButtonByText(Node root, string text)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text == text)
			{
				return button;
			}
		}
		return null;
	}

	private static void CloseCurrentTab(TabBar tabs)
	{
		if (GodotObject.IsInstanceValid(tabs) && tabs.TabCount > 0)
		{
			tabs.EmitSignal(TabBar.SignalName.TabClosePressed, (long)Mathf.Clamp(tabs.CurrentTab, 0, tabs.TabCount - 1));
		}
	}

	private static List<ulong> CaptureTransientRootIds(XWGenericVisualResourceEditor editor, bool includeDirectRows = false)
	{
		List<ulong> list = new List<ulong>();
		Node node = editor?.FindChild("CanvasGrid", recursive: true, owned: false);
		if (GodotObject.IsInstanceValid(node))
		{
			foreach (Node child in node.GetChildren())
			{
				if (!child.IsInGroup("mod_editor_static_surface"))
				{
					list.Add(child.GetInstanceId());
				}
			}
		}
		if (includeDirectRows && GodotObject.IsInstanceValid(editor))
		{
			foreach (Node item in editor.FindChildren("Direct_*", "", recursive: true, owned: false))
			{
				list.Add(item.GetInstanceId());
			}
		}
		return list.Distinct().ToList();
	}

	private static int CountLiveObjects(IEnumerable<ulong> ids)
	{
		int num = 0;
		foreach (ulong id in ids)
		{
			if (GodotObject.IsInstanceValid(GodotObject.InstanceFromId(id)))
			{
				num++;
			}
		}
		return num;
	}

	private static int CountProcessingNodes(Node node, Node root, List<string> paths, out int rawProcessFlags)
	{
		rawProcessFlags = 0;
		if (!GodotObject.IsInstanceValid(node))
		{
			return 0;
		}
		bool flag = node.IsProcessing();
		bool flag2 = node.IsPhysicsProcessing();
		bool flag3 = flag || flag2;
		bool flag4 = node.CanProcess();
		int num = ((flag3 && flag4) ? 1 : 0);
		rawProcessFlags = (flag3 ? 1 : 0);
		if (flag3)
		{
			paths.Add($"{root.GetPathTo(node)}:{node.GetType().Name}:process={flag}:physics={flag2}:canProcess={flag4}");
		}
		foreach (Node child in node.GetChildren())
		{
			num += CountProcessingNodes(child, root, paths, out var rawProcessFlags2);
			rawProcessFlags += rawProcessFlags2;
		}
		return num;
	}

	private async Task<List<double>> SampleFrameTimes(int count, int skip)
	{
		List<double> samples = new List<double>();
		Stopwatch timer = Stopwatch.StartNew();
		double previous = timer.Elapsed.TotalMilliseconds;
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			double totalMilliseconds = timer.Elapsed.TotalMilliseconds;
			if (index >= skip)
			{
				samples.Add(totalMilliseconds - previous);
			}
			previous = totalMilliseconds;
		}
		return samples;
	}

	private async Task<bool> WaitForEditor(int expectedPaletteEntries, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			Node instance = modEditorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			XWResourceWorkspacePalette xWResourceWorkspacePalette = FindNodeOfType<XWResourceWorkspacePalette>(modEditorPanel);
			bool flag = GodotObject.IsInstanceValid(xWResourceWorkspacePalette) && xWResourceWorkspacePalette.EntryCount == expectedPaletteEntries && xWResourceWorkspacePalette.VisibleEntryCount == expectedPaletteEntries;
			if (GodotObject.IsInstanceValid(modEditorPanel) && !GodotObject.IsInstanceValid(instance) && flag && modEditorPanel.LoadedResourceEditorCount == 0)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
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

	private async Task ForceCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		await WaitFrames(3);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static double Measure(Action action)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		action();
		stopwatch.Stop();
		return stopwatch.Elapsed.TotalMilliseconds;
	}

	private static void RecordAction(CategoryResult result, string action, double milliseconds)
	{
		result.ActionSamples.Add(milliseconds);
		RecordActionDiagnostic(result, action, milliseconds);
	}

	private static void RecordActionDiagnostic(CategoryResult result, string action, double milliseconds)
	{
		List<string> actionDiagnostics = result.ActionDiagnostics;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(11, 2, invariantCulture);
		handler.AppendLiteral("action=");
		handler.AppendFormatted(action);
		handler.AppendLiteral(" ms=");
		handler.AppendFormatted(milliseconds, "F3");
		actionDiagnostics.Add(string.Create(invariantCulture, ref handler));
	}

	private static double Percentile(IEnumerable<double> values, double percentile)
	{
		double[] array = values.OrderBy((double value) => value).ToArray();
		if (array.Length == 0)
		{
			return 0.0;
		}
		int num = Math.Clamp((int)Math.Ceiling((double)array.Length * percentile) - 1, 0, array.Length - 1);
		return array[num];
	}

	private static int ReadEnvironmentInt(string name, int fallback)
	{
		if (!int.TryParse(OS.GetEnvironment(name), out var result))
		{
			return fallback;
		}
		return result;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.Print("[MOD_EDITOR_RESOURCE_PERF_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.Print("[MOD_EDITOR_RESOURCE_PERF_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
