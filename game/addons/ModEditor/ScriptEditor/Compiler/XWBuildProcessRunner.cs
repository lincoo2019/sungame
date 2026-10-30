using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PVZHE.ModEditor.ScriptEditor;

internal static class XWBuildProcessRunner
{
	internal sealed class ProcessRunResult
	{
		public int ExitCode = -1;

		public int ProcessId;

		public bool Cancelled;

		public bool TimedOut;

		public bool ProcessTreeTerminated = true;

		public string StandardOutput = "";

		public string StandardError = "";
	}

	private static readonly TimeSpan TerminationGracePeriod = TimeSpan.FromSeconds(10L);

	private static readonly TimeSpan OutputDrainGracePeriod = TimeSpan.FromSeconds(5L);

	internal static async Task<ProcessRunResult> RunAsync(ProcessStartInfo startInfo, TimeSpan timeout, CancellationToken cancellationToken)
	{
		if (startInfo == null)
		{
			throw new ArgumentNullException("startInfo");
		}
		if (timeout != Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return new ProcessRunResult
			{
				Cancelled = true
			};
		}
		startInfo.UseShellExecute = false;
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.CreateNoWindow = true;
		using Process process = new Process
		{
			StartInfo = startInfo
		};
		process.Start();
		ProcessRunResult result = new ProcessRunResult
		{
			ProcessId = process.Id
		};
		Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
		Task<string> stderrTask = process.StandardError.ReadToEndAsync();
		using CancellationTokenSource timeoutCts = ((timeout == Timeout.InfiniteTimeSpan) ? null : new CancellationTokenSource(timeout));
		using CancellationTokenSource waitCts = ((timeoutCts == null) ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token));
		try
		{
			await process.WaitForExitAsync(waitCts.Token).ConfigureAwait(continueOnCapturedContext: false);
			result.ExitCode = process.ExitCode;
		}
		catch (OperationCanceledException)
		{
			result.Cancelled = cancellationToken.IsCancellationRequested;
			result.TimedOut = !result.Cancelled && (timeoutCts?.IsCancellationRequested ?? false);
			ProcessRunResult processRunResult = result;
			processRunResult.ProcessTreeTerminated = await TerminateProcessTreeAsync(process).ConfigureAwait(continueOnCapturedContext: false);
			if (result.ProcessTreeTerminated && process.HasExited)
			{
				result.ExitCode = process.ExitCode;
			}
		}
		finally
		{
			await DrainRedirectedOutputAsync(stdoutTask, stderrTask, result).ConfigureAwait(continueOnCapturedContext: false);
		}
		return result;
	}

	private static async Task<bool> TerminateProcessTreeAsync(Process process)
	{
		try
		{
			if (process.HasExited)
			{
				return true;
			}
			process.Kill(entireProcessTree: true);
		}
		catch (InvalidOperationException)
		{
			return true;
		}
		catch
		{
		}
		if (await WaitForExitBoundedAsync(process, TerminationGracePeriod).ConfigureAwait(continueOnCapturedContext: false))
		{
			return true;
		}
		try
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}
		catch
		{
		}
		return await WaitForExitBoundedAsync(process, TimeSpan.FromSeconds(2L)).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<bool> WaitForExitBoundedAsync(Process process, TimeSpan timeout)
	{
		try
		{
			if (process.HasExited)
			{
				return true;
			}
			Task exitTask = process.WaitForExitAsync();
			return await Task.WhenAny(exitTask, Task.Delay(timeout)).ConfigureAwait(continueOnCapturedContext: false) == exitTask;
		}
		catch (InvalidOperationException)
		{
			return true;
		}
		catch
		{
			try
			{
				return process.HasExited;
			}
			catch
			{
				return false;
			}
		}
	}

	private static async Task DrainRedirectedOutputAsync(Task<string> stdoutTask, Task<string> stderrTask, ProcessRunResult result)
	{
		global::_003C_003Ey__InlineArray2<Task<string>> buffer = default(global::_003C_003Ey__InlineArray2<Task<string>>);
		buffer[0] = stdoutTask;
		buffer[1] = stderrTask;
		Task allOutput = Task.WhenAll<string>(buffer);
		if (await Task.WhenAny(allOutput, Task.Delay(OutputDrainGracePeriod)).ConfigureAwait(continueOnCapturedContext: false) != allOutput)
		{
			result.ProcessTreeTerminated = false;
			return;
		}
		try
		{
			await allOutput.ConfigureAwait(continueOnCapturedContext: false);
			result.StandardOutput = stdoutTask.Result ?? "";
			result.StandardError = stderrTask.Result ?? "";
		}
		catch (Exception ex)
		{
			result.StandardError = result.StandardError + Environment.NewLine + "BUILD_OUTPUT_READ_FAILED: " + ex.GetBaseException().Message;
		}
	}
}
