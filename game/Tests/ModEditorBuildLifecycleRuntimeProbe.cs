using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorBuildLifecycleRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly List<int> _processIds = new List<int>();

	private long _heartbeat;

	private string _tempRoot = "";

	public override void _Process(double delta)
	{
		_heartbeat++;
	}

	public override async void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		bool f3 = false;
		bool heartbeat = false;
		bool cancel = false;
		bool timeout = false;
		bool trees = false;
		bool singleFlight = false;
		bool released = false;
		bool rebuilt = false;
		bool diagnostics = false;
		try
		{
			f3 = await OpenRealEditorAsync();
			Require(f3, "F3 did not open the real ModEditor window.");
			_tempRoot = Path.Combine(Path.GetTempPath(), "pvzhe-build-lifecycle-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempRoot);
			string flightRoot = Path.Combine(_tempRoot, "single-flight");
			Directory.CreateDirectory(flightRoot);
			string cancelPidFile = Path.Combine(_tempRoot, "cancel-child.pid");
			int physicalStarts = 0;
			using CancellationTokenSource cancelCts = new CancellationTokenSource();
			long heartbeatBefore = _heartbeat;
			Task<XWScriptCompiler.CompileResult> cancelFlight = XWModBuildSingleFlight.RunAsync(flightRoot, async delegate(string _, CancellationToken token)
			{
				Interlocked.Increment(ref physicalStarts);
				return ToCompileResult(await RunLongChildAsync(cancelPidFile, TimeSpan.FromSeconds(30L), token));
			}, cancelCts.Token);
			Task<XWScriptCompiler.CompileResult> joinedFlight = XWModBuildSingleFlight.RunAsync(flightRoot, (string _, CancellationToken _) => Task.FromResult(new XWScriptCompiler.CompileResult
			{
				Success = true
			}));
			int num = await WaitForPidFileAsync(cancelPidFile, 300);
			Require(num > 0, "Cancellation build did not start its controlled child process.");
			if (num > 0)
			{
				_processIds.Add(num);
			}
			await WaitFrames(6);
			cancelCts.Cancel();
			XWScriptCompiler.CompileResult cancelledResult = await cancelFlight;
			XWScriptCompiler.CompileResult compileResult = await joinedFlight;
			heartbeat = _heartbeat - heartbeatBefore >= 6;
			cancel = (cancelledResult?.Cancelled ?? false) && compileResult != null && compileResult.Cancelled && cancelledResult.ProcessTreeTerminated;
			XWModBuildSingleFlight.BuildFlightMetrics metrics = XWModBuildSingleFlight.GetMetrics(flightRoot);
			singleFlight = physicalStarts == 1 && metrics.JoinedRequests >= 1;
			released = metrics.ActiveFlights == 0 && !XWScriptCompiler.IsModBuildBusy(flightRoot);
			string timeoutPidFile = Path.Combine(_tempRoot, "timeout-child.pid");
			heartbeatBefore = _heartbeat;
			XWBuildProcessRunner.ProcessRunResult timeoutRun = await RunLongChildAsync(timeoutPidFile, TimeSpan.FromSeconds(3L), CancellationToken.None);
			int num2 = ReadPidFile(timeoutPidFile);
			if (num2 > 0)
			{
				_processIds.Add(num2);
			}
			heartbeat = heartbeat && _heartbeat - heartbeatBefore >= 2;
			timeout = timeoutRun.TimedOut && !timeoutRun.Cancelled && timeoutRun.ProcessTreeTerminated;
			XWInGameDotNetBuildService.BuildResult buildResult = XWInGameDotNetBuildService.BuildLifecycleResult("cancel.probe", _tempRoot, null, cancelledResult?.Cancelled ?? false, timedOut: false, cancelledResult?.ProcessTreeTerminated ?? false, cancelledResult?.Output ?? "");
			XWInGameDotNetBuildService.BuildResult buildResult2 = XWInGameDotNetBuildService.BuildLifecycleResult("timeout.probe", _tempRoot, null, cancelled: false, timeoutRun.TimedOut, timeoutRun.ProcessTreeTerminated, timeoutRun.StandardError);
			diagnostics = HasDiagnostic(buildResult, "BUILD_CANCELLED") && HasDiagnostic(buildResult2, "BUILD_TIMEOUT") && buildResult.Cancelled && buildResult2.TimedOut;
			XWScriptCompiler.CompileResult compileResult2 = await XWModBuildSingleFlight.RunAsync(flightRoot, async (string _, CancellationToken token) => ToCompileResult(await RunQuickBuildAsync(token)), CancellationToken.None);
			rebuilt = compileResult2 != null && compileResult2.Success && compileResult2.Output.Contains("rebuild-ok", StringComparison.Ordinal) && !XWScriptCompiler.IsModBuildBusy(flightRoot);
			if (timeoutRun.ProcessId > 0)
			{
				_processIds.Add(timeoutRun.ProcessId);
			}
			trees = await WaitForAllProcessesToExitAsync(180);
			Require(heartbeat, "SceneTree heartbeat stopped while a child build was running.");
			Require(cancel, "Cancellation did not return a distinct cancelled result after tree cleanup.");
			Require(timeout, "Timeout did not return a distinct timeout result after tree cleanup.");
			Require(trees, "A controlled build parent or child process survived cancellation/timeout.");
			Require(singleFlight, "Concurrent callers did not share one physical Mod build.");
			Require(released, "Cancelled single-flight state remained busy.");
			Require(rebuilt, "A clean rebuild did not succeed after cancellation released the flight.");
			Require(diagnostics, "Cancellation and timeout diagnostics were not distinct.");
		}
		catch (Exception ex)
		{
			Require(condition: false, ex.ToString());
		}
		finally
		{
			bool flag = CleanupTempRoot();
			Require(flag, "Probe temporary files were not removed.");
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_BUILD_LIFECYCLE_PROBE_FAILURE] " + failure);
			}
			GD.Print($"[MOD_EDITOR_BUILD_LIFECYCLE_PROBE] f3={f3} heartbeat={heartbeat} cancel={cancel} timeout={timeout} trees={trees} singleFlight={singleFlight} released={released} rebuilt={rebuilt} diagnostics={diagnostics} cleanup={flag} failures={_failures.Count}");
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}

	private async Task<bool> OpenRealEditorAsync()
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
			return false;
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
		for (int frame = 0; frame < 300; frame++)
		{
			Control control = XWEditorInterface.Instance?.GetEditorPanel();
			if (GodotObject.IsInstanceValid(control) && control.IsVisibleInTree())
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<XWBuildProcessRunner.ProcessRunResult> RunLongChildAsync(string pidFile, TimeSpan timeout, CancellationToken cancellationToken)
	{
		string text = pidFile.Replace("'", "''", StringComparison.Ordinal);
		XWBuildProcessRunner.ProcessRunResult processRunResult = await XWBuildProcessRunner.RunAsync(CreatePowerShellStartInfo("$child = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile','-NonInteractive','-Command','Start-Sleep -Seconds 120') -PassThru -WindowStyle Hidden; [IO.File]::WriteAllText('" + text + "', [string]$child.Id); Wait-Process -Id $child.Id"), timeout, cancellationToken);
		if (processRunResult.ProcessId > 0)
		{
			_processIds.Add(processRunResult.ProcessId);
		}
		return processRunResult;
	}

	private async Task<XWBuildProcessRunner.ProcessRunResult> RunQuickBuildAsync(CancellationToken cancellationToken)
	{
		XWBuildProcessRunner.ProcessRunResult processRunResult = await XWBuildProcessRunner.RunAsync(CreatePowerShellStartInfo("Write-Output 'rebuild-ok'; exit 0"), TimeSpan.FromSeconds(10L), cancellationToken);
		if (processRunResult.ProcessId > 0)
		{
			_processIds.Add(processRunResult.ProcessId);
		}
		return processRunResult;
	}

	private static ProcessStartInfo CreatePowerShellStartInfo(string command)
	{
		return new ProcessStartInfo
		{
			FileName = "powershell.exe",
			WorkingDirectory = Path.GetTempPath(),
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
			ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command }
		};
	}

	private static XWScriptCompiler.CompileResult ToCompileResult(XWBuildProcessRunner.ProcessRunResult run)
	{
		return new XWScriptCompiler.CompileResult
		{
			Success = (run.ExitCode == 0 && !run.Cancelled && !run.TimedOut),
			IsModProject = true,
			Cancelled = run.Cancelled,
			TimedOut = run.TimedOut,
			ProcessTreeTerminated = run.ProcessTreeTerminated,
			ExitCode = run.ExitCode,
			Output = run.StandardOutput + run.StandardError
		};
	}

	private static bool HasDiagnostic(XWInGameDotNetBuildService.BuildResult result, string code)
	{
		if (result?.Diagnostics == null)
		{
			return false;
		}
		foreach (XWCodeErrorChecker.ErrorData diagnostic in result.Diagnostics)
		{
			if (string.Equals(diagnostic?.Code, code, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<int> WaitForPidFileAsync(string path, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			int num = ReadPidFile(path);
			if (num > 0)
			{
				return num;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return 0;
	}

	private static int ReadPidFile(string path)
	{
		try
		{
			int result;
			return (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out result)) ? result : 0;
		}
		catch
		{
			return 0;
		}
	}

	private async Task<bool> WaitForAllProcessesToExitAsync(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			bool flag = false;
			foreach (int processId in _processIds)
			{
				flag |= IsProcessAlive(processId);
			}
			if (!flag)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private static bool IsProcessAlive(int processId)
	{
		if (processId <= 0)
		{
			return false;
		}
		try
		{
			using Process process = Process.GetProcessById(processId);
			return !process.HasExited;
		}
		catch
		{
			return false;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private bool CleanupTempRoot()
	{
		if (string.IsNullOrWhiteSpace(_tempRoot))
		{
			return true;
		}
		try
		{
			string fullPath = Path.GetFullPath(_tempRoot);
			string fullPath2 = Path.GetFullPath(Path.GetTempPath());
			if (!fullPath.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (Directory.Exists(fullPath))
			{
				Directory.Delete(fullPath, recursive: true);
			}
			return !Directory.Exists(fullPath);
		}
		catch
		{
			return false;
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
