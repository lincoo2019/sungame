using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.OutPutPanel;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWCodeEdit : CodeEdit
{
	[Signal]
	public delegate void ErrorsChangedEventHandler(int errorCount, int warningCount);

	[Signal]
	public delegate void DiagnosticsChangedEventHandler();

	[Signal]
	public delegate void ErrorClickedEventHandler(int line, int column);

	public readonly struct ValidationMetrics(int requests, int coalescedRequests, int executedChecks, int publishedResults, int staleResults, int canceledChecks, int activeWorkers, int peakActiveWorkers, int pendingRequests, int lastDiagnosticCount, long maxCaptureMilliseconds, long maxApplyMilliseconds)
	{
		public readonly int Requests = requests;

		public readonly int CoalescedRequests = coalescedRequests;

		public readonly int ExecutedChecks = executedChecks;

		public readonly int PublishedResults = publishedResults;

		public readonly int StaleResults = staleResults;

		public readonly int CanceledChecks = canceledChecks;

		public readonly int ActiveWorkers = activeWorkers;

		public readonly int PeakActiveWorkers = peakActiveWorkers;

		public readonly int PendingRequests = pendingRequests;

		public readonly int LastDiagnosticCount = lastDiagnosticCount;

		public readonly long MaxCaptureMilliseconds = maxCaptureMilliseconds;

		public readonly long MaxApplyMilliseconds = maxApplyMilliseconds;
	}

	private sealed class ValidationRequest
	{
		public string Code = "";

		public string Extension = "";

		public int Generation;
	}

	public readonly struct CompletionMetrics(int requests, int coalescedRequests, int executedAnalyses, int workerEnteredAnalyses, int activeAnalysisWorkers, int appliedResults, int canceledAnalyses, int staleResults, int activeWorkers, int peakActiveWorkers, int pendingRequests, int lastRequestedRevision, int lastAppliedRevision, int lastOptionCount, long maxCaptureMilliseconds, long maxApplyMilliseconds, string lastAppliedPrefix)
	{
		public readonly int Requests = requests;

		public readonly int CoalescedRequests = coalescedRequests;

		public readonly int ExecutedAnalyses = executedAnalyses;

		public readonly int WorkerEnteredAnalyses = workerEnteredAnalyses;

		public readonly int ActiveAnalysisWorkers = activeAnalysisWorkers;

		public readonly int AppliedResults = appliedResults;

		public readonly int CanceledAnalyses = canceledAnalyses;

		public readonly int StaleResults = staleResults;

		public readonly int ActiveWorkers = activeWorkers;

		public readonly int PeakActiveWorkers = peakActiveWorkers;

		public readonly int PendingRequests = pendingRequests;

		public readonly int LastRequestedRevision = lastRequestedRevision;

		public readonly int LastAppliedRevision = lastAppliedRevision;

		public readonly int LastOptionCount = lastOptionCount;

		public readonly long MaxCaptureMilliseconds = maxCaptureMilliseconds;

		public readonly long MaxApplyMilliseconds = maxApplyMilliseconds;

		public readonly string LastAppliedPrefix = lastAppliedPrefix ?? "";
	}

	private sealed class CompletionRequest
	{
		public string Source = "";

		public string DocumentPath = "";

		public int CaretLine;

		public int CaretColumn;

		public int Revision;
	}

	private Godot.Timer _completionTimer;

	private Godot.Timer _validationTimer;

	private const float CompletionDelay = 0.3f;

	private const int CompletionCoalesceDelayMilliseconds = 10;

	private const float ValidationDelay = 1.5f;

	private const float ValidationDelayWithErrors = 0.5f;

	private const int ValidationCoalesceDelayMilliseconds = 10;

	private const int ValidationDiagnosticLimit = 256;

	private int _validationGeneration;

	private readonly object _validationGate = new object();

	private CancellationTokenSource _validationLifetimeCts;

	private CancellationTokenSource _activeValidationCts;

	private Task _validationWorkerTask = Task.CompletedTask;

	private ValidationRequest _pendingValidationRequest;

	private bool _validationWorkerRunning;

	private bool _validationSchedulerEnabled;

	private int _validationRequests;

	private int _validationCoalescedRequests;

	private int _validationExecutedChecks;

	private int _validationPublishedResults;

	private int _validationStaleResults;

	private int _validationCanceledChecks;

	private int _validationActiveWorkers;

	private int _validationPeakActiveWorkers;

	private int _validationLastDiagnosticCount;

	private long _validationMaxCaptureMilliseconds;

	private long _validationMaxApplyMilliseconds;

	private readonly object _completionGate = new object();

	private CancellationTokenSource _completionLifetimeCts;

	private CancellationTokenSource _activeCompletionCts;

	private CompletionRequest _activeCompletionRequest;

	private Task _completionWorkerTask = Task.CompletedTask;

	private CompletionRequest _pendingCompletionRequest;

	private bool _completionWorkerRunning;

	private bool _completionSchedulerEnabled;

	private int _completionDocumentRevision;

	private int _completionRequests;

	private int _completionCoalescedRequests;

	private int _completionExecutedAnalyses;

	private int _completionWorkerEnteredAnalyses;

	private int _completionActiveAnalysisWorkers;

	private int _completionAppliedResults;

	private int _completionCanceledAnalyses;

	private int _completionStaleResults;

	private int _completionActiveWorkers;

	private int _completionPeakActiveWorkers;

	private int _completionLastRequestedRevision;

	private int _completionLastAppliedRevision;

	private int _completionLastOptionCount;

	private long _completionMaxCaptureMilliseconds;

	private long _completionMaxApplyMilliseconds;

	private string _completionLastAppliedPrefix = "";

	private readonly HashSet<string> _completionLastAppliedInserts = new HashSet<string>(StringComparer.Ordinal);

	private XWCodeCompletionProvider _currentCompletionProvider;

	private XWCSharpCompletionProvider _csharpCompletionProvider;

	private CodeHighlighter _csharpHighlighter;

	private const int LargeDocumentSyntaxHighlightThreshold = 400000;

	private bool _largeDocumentSyntaxHighlightSuppressed;

	private string _currentFileExtension = "";

	private string _currentDocumentPath = "";

	private string _projectRoot = "";

	private string _lastIndexedSource;

	private readonly List<XWCodeErrorChecker.ErrorData> _diagnostics = new List<XWCodeErrorChecker.ErrorData>();

	private readonly System.Collections.Generic.Dictionary<int, XWCodeErrorChecker.ErrorData> _errorLines = new System.Collections.Generic.Dictionary<int, XWCodeErrorChecker.ErrorData>();

	private readonly System.Collections.Generic.Dictionary<int, XWCodeErrorChecker.ErrorData> _warningLines = new System.Collections.Generic.Dictionary<int, XWCodeErrorChecker.ErrorData>();

	private static readonly Color ErrorBackgroundColor = new Color(0.3f, 0.1f, 0.1f, 0.5f);

	private static readonly Color WarningBackgroundColor = new Color(0.3f, 0.3f, 0.1f, 0.5f);

	private int _errorGutterIndex = -1;

	private int _lastErrorCount;

	private int _lastWarningCount;

	private Texture2D _errorIcon;

	private Texture2D _warningIcon;

	public bool HasPendingBackgroundWork
	{
		get
		{
			Godot.Timer completionTimer = _completionTimer;
			if (completionTimer == null || completionTimer.ProcessMode != ProcessModeEnum.Disabled)
			{
				Godot.Timer completionTimer2 = _completionTimer;
				if (completionTimer2 != null && !completionTimer2.IsStopped())
				{
					goto IL_0093;
				}
			}
			Godot.Timer validationTimer = _validationTimer;
			if (validationTimer == null || validationTimer.ProcessMode != ProcessModeEnum.Disabled)
			{
				Godot.Timer validationTimer2 = _validationTimer;
				if (validationTimer2 != null && !validationTimer2.IsStopped())
				{
					goto IL_0093;
				}
			}
			if (Volatile.Read(in _validationActiveWorkers) <= 0 && GetPendingValidationRequestCount() <= 0 && Volatile.Read(in _completionActiveWorkers) <= 0)
			{
				return GetPendingCompletionRequestCount() > 0;
			}
			goto IL_0093;
			IL_0093:
			return true;
		}
	}

	public override void _Ready()
	{
		_errorIcon = LoadIconSafe("res://addons/ModEditor/Icons/StatusError.svg");
		_warningIcon = LoadIconSafe("res://addons/ModEditor/Icons/StatusWarning.svg");
		_csharpCompletionProvider = new XWCSharpCompletionProvider(this);
		_csharpHighlighter = new XWCSharpHighlighter();
		CreateCompletionTimer();
		CreateValidationTimer();
		SetupErrorGutter();
		base.TextChanged += OnTextChanged;
		base.CaretChanged += OnCaretChanged;
		base.CodeCompletionEnabled = true;
		lock (_validationGate)
		{
			_validationSchedulerEnabled = true;
			_validationLifetimeCts = new CancellationTokenSource();
		}
		lock (_completionGate)
		{
			_completionSchedulerEnabled = true;
			_completionLifetimeCts = new CancellationTokenSource();
		}
	}

	private static Texture2D LoadIconSafe(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	private void SetupErrorGutter()
	{
		_errorGutterIndex = GetGutterCount();
		AddGutter(1);
		SetGutterName(_errorGutterIndex, "error");
		SetGutterDraw(_errorGutterIndex, draw: true);
		SetGutterOverwritable(_errorGutterIndex, overwritable: false);
		SetGutterWidth(_errorGutterIndex, 16);
	}

	private void CreateCompletionTimer()
	{
		_completionTimer = new Godot.Timer
		{
			OneShot = true,
			WaitTime = 0.30000001192092896
		};
		_completionTimer.Timeout += OnCompletionTimerTimeout;
		AddChild(_completionTimer, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateValidationTimer()
	{
		_validationTimer = new Godot.Timer
		{
			OneShot = true,
			WaitTime = 1.5
		};
		_validationTimer.Timeout += OnValidationTimerTimeout;
		AddChild(_validationTimer, forceReadableName: false, InternalMode.Disabled);
	}

	public void SetFileExtension(string extension)
	{
		_validationGeneration++;
		InvalidateCompletionWork(disableScheduler: false);
		_completionTimer?.Stop();
		_validationTimer?.Stop();
		ClearPendingValidationRequest();
		CancelActiveValidationCheck();
		_currentFileExtension = extension?.ToLower() ?? "";
		UpdateCompletionProvider();
		UpdateSyntaxHighlighter(_currentFileExtension);
	}

	public void SuspendBackgroundWork()
	{
		_completionTimer?.Stop();
		_validationTimer?.Stop();
		if (_completionTimer != null)
		{
			_completionTimer.ProcessMode = ProcessModeEnum.Disabled;
		}
		if (_validationTimer != null)
		{
			_validationTimer.ProcessMode = ProcessModeEnum.Disabled;
		}
		CancelCodeCompletion();
		InvalidateCompletionWork(disableScheduler: true);
		CancelValidationWork(disableScheduler: true);
	}

	public void ResumeBackgroundWork()
	{
		if (_completionTimer != null)
		{
			_completionTimer.ProcessMode = ProcessModeEnum.Inherit;
		}
		if (_validationTimer != null)
		{
			_validationTimer.ProcessMode = ProcessModeEnum.Inherit;
		}
		lock (_validationGate)
		{
			_validationSchedulerEnabled = true;
			if (_validationLifetimeCts == null || _validationLifetimeCts.IsCancellationRequested)
			{
				_validationLifetimeCts = new CancellationTokenSource();
			}
			StartValidationWorkerLockedIfNeeded();
		}
		lock (_completionGate)
		{
			_completionSchedulerEnabled = true;
			if (_completionLifetimeCts == null || _completionLifetimeCts.IsCancellationRequested)
			{
				_completionLifetimeCts = new CancellationTokenSource();
			}
			StartCompletionWorkerLockedIfNeeded();
		}
	}

	public override void _ExitTree()
	{
		base.TextChanged -= OnTextChanged;
		base.CaretChanged -= OnCaretChanged;
		if (_completionTimer != null)
		{
			_completionTimer.Stop();
			_completionTimer.Timeout -= OnCompletionTimerTimeout;
		}
		if (_validationTimer != null)
		{
			_validationTimer.Stop();
			_validationTimer.Timeout -= OnValidationTimerTimeout;
		}
		InvalidateCompletionWork(disableScheduler: true);
		CancelValidationWork(disableScheduler: true);
	}

	public void SetDocumentPath(string path)
	{
		InvalidateCompletionWork(disableScheduler: false);
		_currentDocumentPath = path ?? "";
		if (_currentDocumentPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || _currentDocumentPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			_currentDocumentPath = ProjectSettings.GlobalizePath(_currentDocumentPath);
		}
		_lastIndexedSource = null;
	}

	public void SetProjectRoot(string path)
	{
		string projectRoot = _projectRoot;
		_projectRoot = path ?? "";
		if (_projectRoot.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || _projectRoot.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			_projectRoot = ProjectSettings.GlobalizePath(_projectRoot);
		}
		if (!string.Equals(projectRoot, _projectRoot, StringComparison.OrdinalIgnoreCase))
		{
			_lastIndexedSource = null;
			InvalidateCompletionWork(disableScheduler: false);
		}
		if (!string.IsNullOrWhiteSpace(_projectRoot))
		{
			XWCSharpCodeModel.RequestBackgroundWarmup(_projectRoot);
		}
	}

	public string GetDocumentPath()
	{
		return _currentDocumentPath;
	}

	public string GetProjectRoot()
	{
		return _projectRoot;
	}

	public int[] GetBreakpointLines()
	{
		return GetBreakpointedLines();
	}

	public void ToggleBreakpoint(int line)
	{
		if (line >= 0 && line < GetLineCount())
		{
			SetLineAsBreakpoint(line, !IsLineBreakpointed(line));
		}
	}

	public void RestoreBreakpointLines(IEnumerable<int> lines)
	{
		ClearBreakpointedLines();
		if (lines == null)
		{
			return;
		}
		foreach (int line in lines)
		{
			if (line >= 0 && line < GetLineCount())
			{
				SetLineAsBreakpoint(line, breakpointed: true);
			}
		}
	}

	private void UpdateCompletionProvider()
	{
		if (_currentFileExtension == "cs")
		{
			_currentCompletionProvider = _csharpCompletionProvider;
			base.CodeCompletionEnabled = true;
		}
		else
		{
			_currentCompletionProvider = null;
			base.CodeCompletionEnabled = false;
		}
	}

	private void UpdateSyntaxHighlighter(string extension)
	{
		if (_largeDocumentSyntaxHighlightSuppressed)
		{
			base.SyntaxHighlighter = null;
		}
		else if (extension == "cs")
		{
			base.SyntaxHighlighter = _csharpHighlighter;
		}
		else
		{
			base.SyntaxHighlighter = null;
		}
	}

	public void CheckSyntaxErrors()
	{
		if (string.IsNullOrEmpty(_currentFileExtension))
		{
			SetExternalDiagnostics(new List<XWCodeErrorChecker.ErrorData>());
		}
		else
		{
			RequestValidationNow();
		}
	}

	public void SetExternalDiagnostics(List<XWCodeErrorChecker.ErrorData> diagnostics)
	{
		_validationGeneration++;
		ClearPendingValidationRequest();
		CancelActiveValidationCheck();
		ReplaceDiagnostics(diagnostics);
	}

	private void ReplaceDiagnostics(List<XWCodeErrorChecker.ErrorData> diagnostics)
	{
		ClearAllErrors();
		ApplyDiagnostics(diagnostics);
	}

	private void ApplyDiagnostics(List<XWCodeErrorChecker.ErrorData> diagnostics)
	{
		if (diagnostics == null)
		{
			diagnostics = new List<XWCodeErrorChecker.ErrorData>();
		}
		foreach (XWCodeErrorChecker.ErrorData diagnostic in diagnostics)
		{
			AddError(diagnostic);
		}
		int errorCount = GetErrorCount();
		int warningCount = GetWarningCount();
		if (errorCount != _lastErrorCount || warningCount != _lastWarningCount)
		{
			_lastErrorCount = errorCount;
			_lastWarningCount = warningCount;
			EmitSignal(SignalName.ErrorsChanged, errorCount, warningCount);
		}
		EmitSignal(SignalName.DiagnosticsChanged);
		UpdateValidationDelay();
	}

	private void UpdateValidationDelay()
	{
		_validationTimer.WaitTime = ((_lastErrorCount > 0) ? 0.5f : 1.5f);
	}

	private void OnCompletionTimerTimeout()
	{
		if (base.CodeCompletionEnabled && _currentCompletionProvider == _csharpCompletionProvider)
		{
			string text = base.Text ?? "";
			if (_currentFileExtension == "cs" && !string.IsNullOrEmpty(_currentDocumentPath) && !string.Equals(text, _lastIndexedSource, StringComparison.Ordinal))
			{
				PublishCurrentSourceToIndexAsync(_currentDocumentPath, text, _completionDocumentRevision);
			}
			QueueCompletionRequest(text);
		}
	}

	private void OnValidationTimerTimeout()
	{
		QueueValidationRequest();
	}

	private void OnTextChanged()
	{
		_validationGeneration++;
		InvalidateCompletionWork(disableScheduler: false);
		CancelCodeCompletion();
		_completionTimer.Stop();
		if (base.CodeCompletionEnabled)
		{
			_completionTimer.Start();
		}
		_validationTimer.Start();
	}

	private void OnCaretChanged()
	{
		int caretLine = GetCaretLine();
		int caretColumn = GetCaretColumn();
		lock (_completionGate)
		{
			CompletionRequest completionRequest = _pendingCompletionRequest ?? _activeCompletionRequest;
			if (completionRequest == null || (completionRequest.CaretLine == caretLine && completionRequest.CaretColumn == caretColumn))
			{
				return;
			}
		}
		CancelCodeCompletion();
		InvalidateCompletionWork(disableScheduler: false);
	}

	public void RequestCompletionNow()
	{
		_completionTimer?.Stop();
		if (base.CodeCompletionEnabled && _currentCompletionProvider == _csharpCompletionProvider)
		{
			QueueCompletionRequest(base.Text ?? "");
		}
	}

	private void QueueCompletionRequest(string source)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		CompletionRequest completionRequest = new CompletionRequest
		{
			Source = (source ?? ""),
			DocumentPath = _currentDocumentPath,
			CaretLine = GetCaretLine(),
			CaretColumn = GetCaretColumn(),
			Revision = _completionDocumentRevision
		};
		stopwatch.Stop();
		UpdateMax(ref _completionMaxCaptureMilliseconds, stopwatch.ElapsedMilliseconds);
		Interlocked.Increment(ref _completionRequests);
		Volatile.Write(ref _completionLastRequestedRevision, completionRequest.Revision);
		lock (_completionGate)
		{
			if (_completionSchedulerEnabled)
			{
				if (_pendingCompletionRequest != null || _completionWorkerRunning)
				{
					Interlocked.Increment(ref _completionCoalescedRequests);
				}
				_pendingCompletionRequest = completionRequest;
				_activeCompletionCts?.Cancel();
				StartCompletionWorkerLockedIfNeeded();
			}
		}
	}

	private void StartCompletionWorkerLockedIfNeeded()
	{
		if (_completionSchedulerEnabled && !_completionWorkerRunning && _pendingCompletionRequest != null)
		{
			if (_completionLifetimeCts == null || _completionLifetimeCts.IsCancellationRequested)
			{
				_completionLifetimeCts = new CancellationTokenSource();
			}
			_completionWorkerRunning = true;
			int value = Interlocked.Increment(ref _completionActiveWorkers);
			UpdatePeak(ref _completionPeakActiveWorkers, value);
			_completionWorkerTask = RunCompletionWorkerAsync(_completionLifetimeCts.Token);
		}
	}

	private async Task RunCompletionWorkerAsync(CancellationToken lifetimeToken)
	{
		_ = 1;
		try
		{
			while (true)
			{
				await Task.Delay(10, lifetimeToken);
				CompletionRequest request;
				CancellationTokenSource requestCts;
				lock (_completionGate)
				{
					if (!_completionSchedulerEnabled || _pendingCompletionRequest == null)
					{
						break;
					}
					request = _pendingCompletionRequest;
					_pendingCompletionRequest = null;
					requestCts = (_activeCompletionCts = CancellationTokenSource.CreateLinkedTokenSource(_completionLifetimeCts.Token));
					_activeCompletionRequest = request;
				}
				XWCSharpCompletionProvider.CompletionAnalysis completionAnalysis;
				try
				{
					Interlocked.Increment(ref _completionExecutedAnalyses);
					completionAnalysis = await _csharpCompletionProvider.AnalyzeCompletionAsync(request.Source, request.DocumentPath, request.CaretLine, request.CaretColumn, OnCompletionAnalysisWorkerEntered, OnCompletionAnalysisWorkerExited, requestCts.Token);
				}
				catch (OperationCanceledException)
				{
					Interlocked.Increment(ref _completionCanceledAnalyses);
					continue;
				}
				catch (Exception ex2)
				{
					Interlocked.Increment(ref _completionStaleResults);
					if (GodotObject.IsInstanceValid(this))
					{
						GD.PushError("C# completion analysis failed: " + ex2.Message);
					}
					continue;
				}
				finally
				{
					lock (_completionGate)
					{
						if (_activeCompletionCts == requestCts)
						{
							_activeCompletionCts = null;
							_activeCompletionRequest = null;
						}
					}
					requestCts.Dispose();
				}
				if (!GodotObject.IsInstanceValid(this) || request.Revision != _completionDocumentRevision || !string.Equals(request.DocumentPath, _currentDocumentPath, StringComparison.Ordinal) || request.CaretLine != GetCaretLine() || request.CaretColumn != GetCaretColumn() || !base.CodeCompletionEnabled || _currentCompletionProvider != _csharpCompletionProvider)
				{
					Interlocked.Increment(ref _completionStaleResults);
					continue;
				}
				Stopwatch stopwatch = Stopwatch.StartNew();
				_csharpCompletionProvider.ApplyCompletion(completionAnalysis);
				stopwatch.Stop();
				UpdateMax(ref _completionMaxApplyMilliseconds, stopwatch.ElapsedMilliseconds);
				Interlocked.Increment(ref _completionAppliedResults);
				Volatile.Write(ref _completionLastAppliedRevision, request.Revision);
				Volatile.Write(ref _completionLastOptionCount, completionAnalysis.Options.Count);
				_completionLastAppliedPrefix = completionAnalysis.Prefix;
				_completionLastAppliedInserts.Clear();
				foreach (XWCSharpCompletionProvider.CompletionOptionData option in completionAnalysis.Options)
				{
					_completionLastAppliedInserts.Add(option.Insert);
				}
				request = null;
				requestCts = null;
			}
		}
		catch (OperationCanceledException)
		{
			Interlocked.Increment(ref _completionCanceledAnalyses);
		}
		finally
		{
			Interlocked.Decrement(ref _completionActiveWorkers);
			lock (_completionGate)
			{
				_completionWorkerRunning = false;
				_completionWorkerTask = Task.CompletedTask;
				StartCompletionWorkerLockedIfNeeded();
			}
		}
	}

	private async Task PublishCurrentSourceToIndexAsync(string path, string source, int revision)
	{
		await XWCSharpCodeModel.RequestSourceUpdate(path, source);
		string key = NormalizeIndexPath(path);
		XWCSharpProjectIndex.Snapshot snapshot = XWCSharpProjectIndex.GetSnapshot();
		if (GodotObject.IsInstanceValid(this) && revision == _completionDocumentRevision && string.Equals(path, _currentDocumentPath, StringComparison.Ordinal) && snapshot.Documents.TryGetValue(key, out var value) && string.Equals(value, source, StringComparison.Ordinal))
		{
			_lastIndexedSource = source;
		}
	}

	private static string NormalizeIndexPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		try
		{
			return Path.GetFullPath(path).Replace('\\', '/');
		}
		catch
		{
			return path.Replace('\\', '/');
		}
	}

	private void InvalidateCompletionWork(bool disableScheduler)
	{
		Interlocked.Increment(ref _completionDocumentRevision);
		CancellationTokenSource cancellation = null;
		Task workerTask = Task.CompletedTask;
		CancellationTokenSource activeCompletionCts;
		lock (_completionGate)
		{
			if (disableScheduler)
			{
				_completionSchedulerEnabled = false;
				cancellation = _completionLifetimeCts;
				_completionLifetimeCts = null;
				workerTask = _completionWorkerTask;
			}
			_pendingCompletionRequest = null;
			activeCompletionCts = _activeCompletionCts;
		}
		activeCompletionCts?.Cancel();
		CancelAndDisposeWhenComplete(cancellation, workerTask);
	}

	private int GetPendingCompletionRequestCount()
	{
		lock (_completionGate)
		{
			return (_pendingCompletionRequest != null) ? 1 : 0;
		}
	}

	private void OnCompletionAnalysisWorkerEntered()
	{
		Interlocked.Increment(ref _completionWorkerEnteredAnalyses);
		Interlocked.Increment(ref _completionActiveAnalysisWorkers);
	}

	private void OnCompletionAnalysisWorkerExited()
	{
		Interlocked.Decrement(ref _completionActiveAnalysisWorkers);
	}

	private static void UpdatePeak(ref int target, int value)
	{
		int num;
		do
		{
			num = Volatile.Read(in target);
		}
		while (value > num && Interlocked.CompareExchange(ref target, value, num) != num);
	}

	private static void UpdateMax(ref long target, long value)
	{
		long num;
		do
		{
			num = Interlocked.Read(in target);
		}
		while (value > num && Interlocked.CompareExchange(ref target, value, num) != num);
	}

	public CompletionMetrics GetCompletionMetrics()
	{
		return new CompletionMetrics(Volatile.Read(in _completionRequests), Volatile.Read(in _completionCoalescedRequests), Volatile.Read(in _completionExecutedAnalyses), Volatile.Read(in _completionWorkerEnteredAnalyses), Volatile.Read(in _completionActiveAnalysisWorkers), Volatile.Read(in _completionAppliedResults), Volatile.Read(in _completionCanceledAnalyses), Volatile.Read(in _completionStaleResults), Volatile.Read(in _completionActiveWorkers), Volatile.Read(in _completionPeakActiveWorkers), GetPendingCompletionRequestCount(), Volatile.Read(in _completionLastRequestedRevision), Volatile.Read(in _completionLastAppliedRevision), Volatile.Read(in _completionLastOptionCount), Interlocked.Read(in _completionMaxCaptureMilliseconds), Interlocked.Read(in _completionMaxApplyMilliseconds), _completionLastAppliedPrefix);
	}

	public bool LastCompletionContainsOption(string insert)
	{
		if (!string.IsNullOrEmpty(insert))
		{
			return _completionLastAppliedInserts.Contains(insert);
		}
		return false;
	}

	public bool PublishedCompletionContainsOption(string insert)
	{
		if (string.IsNullOrEmpty(insert))
		{
			return false;
		}
		foreach (Dictionary codeCompletionOption in GetCodeCompletionOptions())
		{
			if (codeCompletionOption.TryGetValue("insert_text", out var value) && string.Equals(value.AsString(), insert, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	public bool ResetCompletionMetricsForProbe()
	{
		CompletionMetrics completionMetrics = GetCompletionMetrics();
		if (completionMetrics.ActiveWorkers != 0 || completionMetrics.PendingRequests != 0)
		{
			return false;
		}
		Interlocked.Exchange(ref _completionRequests, 0);
		Interlocked.Exchange(ref _completionCoalescedRequests, 0);
		Interlocked.Exchange(ref _completionExecutedAnalyses, 0);
		Interlocked.Exchange(ref _completionWorkerEnteredAnalyses, 0);
		Interlocked.Exchange(ref _completionActiveAnalysisWorkers, 0);
		Interlocked.Exchange(ref _completionAppliedResults, 0);
		Interlocked.Exchange(ref _completionCanceledAnalyses, 0);
		Interlocked.Exchange(ref _completionStaleResults, 0);
		Interlocked.Exchange(ref _completionPeakActiveWorkers, 0);
		Interlocked.Exchange(ref _completionLastOptionCount, 0);
		Interlocked.Exchange(ref _completionMaxCaptureMilliseconds, 0L);
		Interlocked.Exchange(ref _completionMaxApplyMilliseconds, 0L);
		_completionLastAppliedPrefix = "";
		_completionLastAppliedInserts.Clear();
		return true;
	}

	public void RequestValidationNow()
	{
		_validationGeneration++;
		_validationTimer.Stop();
		QueueValidationRequest();
	}

	private void QueueValidationRequest()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		ValidationRequest pendingValidationRequest = new ValidationRequest
		{
			Code = (base.Text ?? ""),
			Extension = _currentFileExtension,
			Generation = _validationGeneration
		};
		stopwatch.Stop();
		UpdateMax(ref _validationMaxCaptureMilliseconds, stopwatch.ElapsedMilliseconds);
		Interlocked.Increment(ref _validationRequests);
		lock (_validationGate)
		{
			if (_validationSchedulerEnabled)
			{
				if (_pendingValidationRequest != null || _validationWorkerRunning)
				{
					Interlocked.Increment(ref _validationCoalescedRequests);
				}
				_pendingValidationRequest = pendingValidationRequest;
				_activeValidationCts?.Cancel();
				StartValidationWorkerLockedIfNeeded();
			}
		}
	}

	private void StartValidationWorkerLockedIfNeeded()
	{
		if (_validationSchedulerEnabled && !_validationWorkerRunning && _pendingValidationRequest != null)
		{
			if (_validationLifetimeCts == null || _validationLifetimeCts.IsCancellationRequested)
			{
				_validationLifetimeCts = new CancellationTokenSource();
			}
			_validationWorkerRunning = true;
			int activeWorkers = Interlocked.Increment(ref _validationActiveWorkers);
			UpdatePeakActiveWorkers(activeWorkers);
			_validationWorkerTask = RunValidationWorkerAsync(_validationLifetimeCts.Token);
		}
	}

	private async Task RunValidationWorkerAsync(CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			while (true)
			{
				await Task.Delay(10, cancellationToken);
				ValidationRequest request;
				lock (_validationGate)
				{
					request = _pendingValidationRequest;
					_pendingValidationRequest = null;
				}
				if (request == null)
				{
					break;
				}
				CancellationTokenSource requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				try
				{
					lock (_validationGate)
					{
						_activeValidationCts = requestCts;
					}
					List<XWCodeErrorChecker.ErrorData> list;
					try
					{
						Interlocked.Increment(ref _validationExecutedChecks);
						XWCodeErrorCheckerRegistry.Init();
						list = await Task.Run(() => RunValidationCheckBelowNormalPriority(request.Code, request.Extension, requestCts.Token), requestCts.Token);
					}
					catch (OperationCanceledException)
					{
						Interlocked.Increment(ref _validationCanceledChecks);
						goto end_IL_00f6;
					}
					catch (Exception ex2)
					{
						list = new List<XWCodeErrorChecker.ErrorData>
						{
							new XWCodeErrorChecker.ErrorData
							{
								Line = 0,
								Column = 0,
								SeverityLevel = XWCodeErrorChecker.Severity.Error,
								Code = "VALIDATION_WORKER_FAILED",
								Message = "C# validation failed: " + ex2.Message
							}
						};
					}
					finally
					{
						lock (_validationGate)
						{
							if (_activeValidationCts == requestCts)
							{
								_activeValidationCts = null;
							}
						}
					}
					cancellationToken.ThrowIfCancellationRequested();
					if (!GodotObject.IsInstanceValid(this))
					{
						break;
					}
					if (request.Generation != _validationGeneration)
					{
						Interlocked.Increment(ref _validationStaleResults);
						continue;
					}
					if (list.Count > 256)
					{
						list = list.GetRange(0, 256);
					}
					Stopwatch stopwatch = Stopwatch.StartNew();
					ReplaceDiagnostics(list);
					stopwatch.Stop();
					UpdateMax(ref _validationMaxApplyMilliseconds, stopwatch.ElapsedMilliseconds);
					Volatile.Write(ref _validationLastDiagnosticCount, list.Count);
					Interlocked.Increment(ref _validationPublishedResults);
					end_IL_00f6:;
				}
				finally
				{
					if (requestCts != null)
					{
						((IDisposable)requestCts).Dispose();
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			Interlocked.Increment(ref _validationCanceledChecks);
		}
		finally
		{
			Interlocked.Decrement(ref _validationActiveWorkers);
			lock (_validationGate)
			{
				_validationWorkerRunning = false;
				_validationWorkerTask = Task.CompletedTask;
				StartValidationWorkerLockedIfNeeded();
			}
		}
	}

	private void CancelValidationWork(bool disableScheduler)
	{
		_validationGeneration++;
		CancellationTokenSource cancellation = null;
		Task workerTask = Task.CompletedTask;
		lock (_validationGate)
		{
			if (disableScheduler)
			{
				_validationSchedulerEnabled = false;
				cancellation = _validationLifetimeCts;
				_validationLifetimeCts = null;
				workerTask = _validationWorkerTask;
			}
			_pendingValidationRequest = null;
		}
		CancelAndDisposeWhenComplete(cancellation, workerTask);
	}

	private static void CancelAndDisposeWhenComplete(CancellationTokenSource cancellation, Task workerTask)
	{
		if (cancellation == null)
		{
			return;
		}
		cancellation.Cancel();
		if (workerTask == null || workerTask.IsCompleted)
		{
			cancellation.Dispose();
			return;
		}
		workerTask.ContinueWith(delegate
		{
			cancellation.Dispose();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	private void ClearPendingValidationRequest()
	{
		lock (_validationGate)
		{
			_pendingValidationRequest = null;
		}
	}

	private void CancelActiveValidationCheck()
	{
		lock (_validationGate)
		{
			_activeValidationCts?.Cancel();
		}
	}

	private static List<XWCodeErrorChecker.ErrorData> RunValidationCheckBelowNormalPriority(string code, string extension, CancellationToken cancellationToken)
	{
		Thread currentThread = Thread.CurrentThread;
		ThreadPriority priority = currentThread.Priority;
		try
		{
			currentThread.Priority = ThreadPriority.BelowNormal;
			return XWCodeErrorCheckerRegistry.CheckCode(code, extension, cancellationToken);
		}
		finally
		{
			currentThread.Priority = priority;
		}
	}

	private int GetPendingValidationRequestCount()
	{
		lock (_validationGate)
		{
			return (_pendingValidationRequest != null) ? 1 : 0;
		}
	}

	private void UpdatePeakActiveWorkers(int activeWorkers)
	{
		int num;
		do
		{
			num = Volatile.Read(in _validationPeakActiveWorkers);
		}
		while (activeWorkers > num && Interlocked.CompareExchange(ref _validationPeakActiveWorkers, activeWorkers, num) != num);
	}

	public ValidationMetrics GetValidationMetrics()
	{
		return new ValidationMetrics(Volatile.Read(in _validationRequests), Volatile.Read(in _validationCoalescedRequests), Volatile.Read(in _validationExecutedChecks), Volatile.Read(in _validationPublishedResults), Volatile.Read(in _validationStaleResults), Volatile.Read(in _validationCanceledChecks), Volatile.Read(in _validationActiveWorkers), Volatile.Read(in _validationPeakActiveWorkers), GetPendingValidationRequestCount(), Volatile.Read(in _validationLastDiagnosticCount), Interlocked.Read(in _validationMaxCaptureMilliseconds), Interlocked.Read(in _validationMaxApplyMilliseconds));
	}

	public bool ResetValidationMetricsForProbe()
	{
		ValidationMetrics validationMetrics = GetValidationMetrics();
		if (validationMetrics.ActiveWorkers != 0 || validationMetrics.PendingRequests != 0)
		{
			return false;
		}
		Interlocked.Exchange(ref _validationRequests, 0);
		Interlocked.Exchange(ref _validationCoalescedRequests, 0);
		Interlocked.Exchange(ref _validationExecutedChecks, 0);
		Interlocked.Exchange(ref _validationPublishedResults, 0);
		Interlocked.Exchange(ref _validationStaleResults, 0);
		Interlocked.Exchange(ref _validationCanceledChecks, 0);
		Interlocked.Exchange(ref _validationPeakActiveWorkers, 0);
		Interlocked.Exchange(ref _validationLastDiagnosticCount, 0);
		Interlocked.Exchange(ref _validationMaxCaptureMilliseconds, 0L);
		Interlocked.Exchange(ref _validationMaxApplyMilliseconds, 0L);
		return true;
	}

	private void AddError(int line, int column, string message, int severity, string code)
	{
		XWCodeErrorChecker.ErrorData data = new XWCodeErrorChecker.ErrorData
		{
			Line = line,
			Column = column,
			Message = message,
			SeverityLevel = (XWCodeErrorChecker.Severity)severity,
			Code = code
		};
		AddError(data);
	}

	private void AddError(XWCodeErrorChecker.ErrorData data)
	{
		_diagnostics.Add(data);
		int line = data.Line;
		Color color;
		if (data.SeverityLevel == XWCodeErrorChecker.Severity.Error)
		{
			_errorLines[line] = data;
			color = ErrorBackgroundColor;
			SetErrorGutterIcon(line, isError: true);
		}
		else
		{
			_warningLines[line] = data;
			color = WarningBackgroundColor;
			SetErrorGutterIcon(line, isError: false);
		}
		if (line >= 0 && line < GetLineCount())
		{
			SetLineBackgroundColor(line, color);
		}
	}

	private void SetErrorGutterIcon(int line, bool isError)
	{
		if (_errorGutterIndex >= 0 && line >= 0 && line < GetLineCount())
		{
			SetLineGutterIcon(line, _errorGutterIndex, isError ? _errorIcon : _warningIcon);
		}
	}

	private void ClearErrorGutterIcon(int line)
	{
		if (_errorGutterIndex >= 0 && line >= 0 && line < GetLineCount())
		{
			SetLineGutterIcon(line, _errorGutterIndex, null);
		}
	}

	public void ClearAllErrors()
	{
		foreach (int key in _errorLines.Keys)
		{
			if (key >= 0 && key < GetLineCount())
			{
				SetLineBackgroundColor(key, new Color(0f, 0f, 0f, 0f));
				ClearErrorGutterIcon(key);
			}
		}
		foreach (int key2 in _warningLines.Keys)
		{
			if (key2 >= 0 && key2 < GetLineCount())
			{
				SetLineBackgroundColor(key2, new Color(0f, 0f, 0f, 0f));
				ClearErrorGutterIcon(key2);
			}
		}
		_errorLines.Clear();
		_warningLines.Clear();
		_diagnostics.Clear();
	}

	public int GetErrorCount()
	{
		int num = 0;
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Error)
			{
				num++;
			}
		}
		return num;
	}

	public int GetWarningCount()
	{
		int num = 0;
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasAnyErrors()
	{
		return _diagnostics.Count > 0;
	}

	public XWCodeErrorChecker.ErrorData GetFirstError()
	{
		XWCodeErrorChecker.ErrorData errorData = null;
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Error && (errorData == null || diagnostic.Line < errorData.Line))
			{
				errorData = diagnostic;
			}
		}
		return errorData;
	}

	public XWCodeErrorChecker.ErrorData GetFirstWarning()
	{
		XWCodeErrorChecker.ErrorData errorData = null;
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning && (errorData == null || diagnostic.Line < errorData.Line))
			{
				errorData = diagnostic;
			}
		}
		return errorData;
	}

	public List<XWCodeErrorChecker.ErrorData> GetAllErrors()
	{
		List<XWCodeErrorChecker.ErrorData> list = new List<XWCodeErrorChecker.ErrorData>(_diagnostics);
		list.Sort(CompareDiagnostics);
		return list;
	}

	public List<XWCodeErrorChecker.ErrorData> GetOnlyErrors()
	{
		List<XWCodeErrorChecker.ErrorData> list = new List<XWCodeErrorChecker.ErrorData>();
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Error)
			{
				list.Add(diagnostic);
			}
		}
		list.Sort(CompareDiagnostics);
		return list;
	}

	public List<XWCodeErrorChecker.ErrorData> GetOnlyWarnings()
	{
		List<XWCodeErrorChecker.ErrorData> list = new List<XWCodeErrorChecker.ErrorData>();
		foreach (XWCodeErrorChecker.ErrorData diagnostic in _diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning)
			{
				list.Add(diagnostic);
			}
		}
		list.Sort(CompareDiagnostics);
		return list;
	}

	private static int CompareDiagnostics(XWCodeErrorChecker.ErrorData a, XWCodeErrorChecker.ErrorData b)
	{
		int num = a.Line.CompareTo(b.Line);
		if (num != 0)
		{
			return num;
		}
		int num2 = a.Column.CompareTo(b.Column);
		if (num2 != 0)
		{
			return num2;
		}
		return string.Compare(a.Code, b.Code, StringComparison.Ordinal);
	}

	public void JumpToError(XWCodeErrorChecker.ErrorData errorData)
	{
		if (errorData != null)
		{
			int line = errorData.Line;
			int column = errorData.Column;
			if (line >= 0 && line < GetLineCount())
			{
				column = Mathf.Clamp(column, 0, GetLine(line).Length);
				SetCaretLine(line);
				SetCaretColumn(column);
				CenterViewportToCaret();
				EmitSignal(SignalName.ErrorClicked, line, column);
			}
		}
	}

	public string GetAllText()
	{
		return base.Text;
	}

	public void SetAllText(string text)
	{
		if (base.Editable)
		{
			LoadDocumentText(text);
		}
	}

	public void LoadDocumentText(string text)
	{
		string text2 = text ?? "";
		bool flag = text2.Length >= 400000;
		if (flag && !_largeDocumentSyntaxHighlightSuppressed)
		{
			base.SyntaxHighlighter = null;
		}
		_largeDocumentSyntaxHighlightSuppressed = flag;
		base.Text = text2;
		if (!flag)
		{
			UpdateSyntaxHighlighter(_currentFileExtension);
		}
	}

	public void InsertAtCursor(string text)
	{
		if (base.Editable)
		{
			InsertTextAtCaret(text);
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!base.Editable)
		{
			return false;
		}
		List<string> list = XWFileSystemDropHelper.ExtractDropPaths(data);
		bool preferFieldDeclaration = IsCSharpTypeBodyDropContext(atPosition);
		if (list.Count > 0)
		{
			return !string.IsNullOrWhiteSpace(XWScriptResourceDropCodeGenerator.BuildSnippetForPaths(list, _currentFileExtension, preferFieldDeclaration));
		}
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (base.Editable)
		{
			List<string> list = XWFileSystemDropHelper.ExtractDropPaths(data);
			bool preferFieldDeclaration = IsCSharpTypeBodyDropContext(atPosition);
			string text = XWScriptResourceDropCodeGenerator.BuildSnippetForPaths(list, _currentFileExtension, preferFieldDeclaration);
			if (!string.IsNullOrWhiteSpace(text))
			{
				SetCaretFromDropPosition(atPosition);
				InsertTextAtCaret(text);
				XWEditorInterface.Instance?.AddOutputMessage($"脚本: 从资源拖拽生成代码 ({list.Count})", XWOutputPanel.MessageType.Editor);
			}
		}
	}

	private void SetCaretFromDropPosition(Vector2 atPosition)
	{
		if (GetLineCount() > 0)
		{
			Vector2I dropLineColumn = GetDropLineColumn(atPosition);
			SetCaretLine(dropLineColumn.X);
			SetCaretColumn(dropLineColumn.Y);
		}
	}

	private bool IsCSharpTypeBodyDropContext(Vector2 atPosition)
	{
		if (_currentFileExtension != "cs" || GetLineCount() <= 0)
		{
			return false;
		}
		int textOffsetFromDropPosition = GetTextOffsetFromDropPosition(atPosition);
		return IsCSharpTypeBodyOffset(base.Text ?? "", textOffsetFromDropPosition);
	}

	private int GetTextOffsetFromDropPosition(Vector2 atPosition)
	{
		Vector2I dropLineColumn = GetDropLineColumn(atPosition);
		int num = 0;
		for (int i = 0; i < dropLineColumn.X; i++)
		{
			num += GetLine(i).Length + 1;
		}
		return num + dropLineColumn.Y;
	}

	private Vector2I GetDropLineColumn(Vector2 atPosition)
	{
		Vector2I lineColumnAtPos = GetLineColumnAtPos(new Vector2I(Mathf.RoundToInt(atPosition.X), Mathf.RoundToInt(atPosition.Y)), allowOutOfBounds: true);
		int num = Mathf.Clamp(lineColumnAtPos.X, 0, GetLineCount() - 1);
		int y = Mathf.Clamp(lineColumnAtPos.Y, 0, GetLine(num).Length);
		return new Vector2I(num, y);
	}

	private static bool IsCSharpTypeBodyOffset(string text, int offset)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		offset = Mathf.Clamp(offset, 0, text.Length);
		int num = 0;
		HashSet<int> hashSet = new HashSet<int>();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		for (int i = 0; i < offset; i++)
		{
			char c = text[i];
			char c2 = ((i + 1 < text.Length) ? text[i + 1] : '\0');
			if (flag)
			{
				if (c == '\n')
				{
					flag = false;
				}
				continue;
			}
			if (flag2)
			{
				if (c == '*' && c2 == '/')
				{
					flag2 = false;
					i++;
				}
				continue;
			}
			if (flag3)
			{
				if (flag4)
				{
					if (c == '"' && c2 == '"')
					{
						i++;
					}
					else if (c == '"')
					{
						flag3 = false;
						flag4 = false;
					}
					continue;
				}
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '"':
					flag3 = false;
					break;
				}
				continue;
			}
			if (flag5)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '\'':
					flag5 = false;
					break;
				}
				continue;
			}
			if (c == '/' && c2 == '/')
			{
				flag = true;
				i++;
				continue;
			}
			if (c == '/' && c2 == '*')
			{
				flag2 = true;
				i++;
				continue;
			}
			if (c == '@' && c2 == '"')
			{
				flag3 = true;
				flag4 = true;
				i++;
				continue;
			}
			switch (c)
			{
			case '"':
				flag3 = true;
				flag4 = false;
				break;
			case '\'':
				flag5 = true;
				break;
			case '{':
				num++;
				if (IsTypeDeclarationBrace(text, i))
				{
					hashSet.Add(num);
				}
				break;
			case '}':
				hashSet.Remove(num);
				num = Math.Max(0, num - 1);
				break;
			}
		}
		return hashSet.Contains(num);
	}

	private static bool IsTypeDeclarationBrace(string text, int braceIndex)
	{
		int num = 0;
		for (int num2 = braceIndex - 1; num2 >= 0; num2--)
		{
			char c = text[num2];
			if (c == '{' || c == '}' || c == ';')
			{
				num = num2 + 1;
				break;
			}
		}
		return Regex.IsMatch(text.Substring(num, braceIndex - num), "\\b(class|struct|interface|record)\\s+[A-Za-z_][A-Za-z0-9_]*");
	}
}
