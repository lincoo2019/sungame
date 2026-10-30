using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWModBuildSingleFlight
{
	public readonly struct BuildFlightMetrics
	{
		public int PhysicalStarts { get; }

		public int JoinedRequests { get; }

		public int ActiveFlights { get; }

		internal BuildFlightMetrics(int physicalStarts, int joinedRequests, int activeFlights)
		{
			PhysicalStarts = physicalStarts;
			JoinedRequests = joinedRequests;
			ActiveFlights = activeFlights;
		}
	}

	private sealed class Flight
	{
		public readonly TaskCompletionSource<XWScriptCompiler.CompileResult> Completion = new TaskCompletionSource<XWScriptCompiler.CompileResult>(TaskCreationOptions.RunContinuationsAsynchronously);

		public readonly SynchronizationContext NotificationContext = SynchronizationContext.Current;

		public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

		public readonly List<CancellationTokenRegistration> CallerCancellations = new List<CancellationTokenRegistration>();

		public Task<XWScriptCompiler.CompileResult> Task => Completion.Task;

		public void LinkCallerCancellation(CancellationToken cancellationToken)
		{
			if (cancellationToken.CanBeCanceled)
			{
				CallerCancellations.Add(cancellationToken.Register(Cancellation.Cancel));
			}
		}

		public void DisposeCancellationLinks()
		{
			foreach (CancellationTokenRegistration callerCancellation in CallerCancellations)
			{
				callerCancellation.Dispose();
			}
			CallerCancellations.Clear();
			Cancellation.Dispose();
		}
	}

	private sealed class RootState
	{
		public Flight ActiveFlight;

		public int PhysicalStarts;

		public int JoinedRequests;
	}

	private static readonly object Gate = new object();

	private static readonly Dictionary<string, RootState> States = new Dictionary<string, RootState>(StringComparer.OrdinalIgnoreCase);

	public static event Action<string, bool> FlightStateChanged;

	public static Task<XWScriptCompiler.CompileResult> RunAsync(string modProjectRoot, Func<string, XWScriptCompiler.CompileResult> build)
	{
		if (build == null)
		{
			throw new ArgumentNullException("build");
		}
		return RunAsync(modProjectRoot, (string root, CancellationToken cancellationToken) => Task.Run(() => build(root), cancellationToken), CancellationToken.None);
	}

	public static Task<XWScriptCompiler.CompileResult> RunAsync(string modProjectRoot, Func<string, CancellationToken, Task<XWScriptCompiler.CompileResult>> build)
	{
		return RunAsync(modProjectRoot, build, CancellationToken.None);
	}

	public static Task<XWScriptCompiler.CompileResult> RunAsync(string modProjectRoot, Func<string, CancellationToken, Task<XWScriptCompiler.CompileResult>> build, CancellationToken cancellationToken)
	{
		if (build == null)
		{
			throw new ArgumentNullException("build");
		}
		string text = NormalizeProjectRoot(modProjectRoot);
		if (string.IsNullOrWhiteSpace(text))
		{
			return Task.FromResult(CreateInvalidRootResult());
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromResult(CreateCancelledResult());
		}
		Flight flight;
		lock (Gate)
		{
			if (!States.TryGetValue(text, out var value))
			{
				value = new RootState();
				States[text] = value;
			}
			if (value.ActiveFlight != null)
			{
				value.JoinedRequests++;
				value.ActiveFlight.LinkCallerCancellation(cancellationToken);
				return value.ActiveFlight.Task;
			}
			flight = new Flight();
			flight.LinkCallerCancellation(cancellationToken);
			value.ActiveFlight = flight;
			value.PhysicalStarts++;
		}
		PublishStateChanged(text, busy: true, flight.NotificationContext);
		ExecuteAsync(text, flight, build);
		return flight.Task;
	}

	public static bool IsBusy(string modProjectRoot)
	{
		string text = NormalizeProjectRoot(modProjectRoot);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (Gate)
		{
			RootState value;
			return States.TryGetValue(text, out value) && value.ActiveFlight != null;
		}
	}

	public static BuildFlightMetrics GetMetrics(string modProjectRoot)
	{
		string text = NormalizeProjectRoot(modProjectRoot);
		if (string.IsNullOrWhiteSpace(text))
		{
			return default(BuildFlightMetrics);
		}
		lock (Gate)
		{
			if (!States.TryGetValue(text, out var value))
			{
				return default(BuildFlightMetrics);
			}
			return new BuildFlightMetrics(value.PhysicalStarts, value.JoinedRequests, (value.ActiveFlight != null) ? 1 : 0);
		}
	}

	public static string NormalizeProjectRoot(string modProjectRoot)
	{
		if (string.IsNullOrWhiteSpace(modProjectRoot))
		{
			return "";
		}
		string text = modProjectRoot;
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			text = ProjectSettings.GlobalizePath(text);
		}
		try
		{
			return Path.GetFullPath(text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return text.Trim().TrimEnd('/', '\\');
		}
	}

	private static async Task ExecuteAsync(string normalizedRoot, Flight flight, Func<string, CancellationToken, Task<XWScriptCompiler.CompileResult>> build)
	{
		XWScriptCompiler.CompileResult result = null;
		Exception failure = null;
		try
		{
			result = await build(normalizedRoot, flight.Cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			result = CreateCancelledResult();
		}
		catch (Exception ex2)
		{
			failure = ex2;
		}
		lock (Gate)
		{
			if (States.TryGetValue(normalizedRoot, out var value) && value.ActiveFlight == flight)
			{
				value.ActiveFlight = null;
			}
		}
		PublishStateChanged(normalizedRoot, busy: false, flight.NotificationContext);
		flight.DisposeCancellationLinks();
		if (failure != null)
		{
			flight.Completion.TrySetException(failure);
		}
		else
		{
			flight.Completion.TrySetResult(result);
		}
	}

	private static XWScriptCompiler.CompileResult CreateInvalidRootResult()
	{
		return new XWScriptCompiler.CompileResult
		{
			Success = false,
			IsModProject = true,
			ExitCode = -1,
			Output = "INVALID_MOD_ROOT: Mod project directory was not specified.",
			Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					Line = 0,
					Column = 0,
					Message = "INVALID_MOD_ROOT: Mod project directory was not specified.",
					SeverityLevel = XWCodeErrorChecker.Severity.Error,
					Code = "INVALID_MOD_ROOT"
				}
			}
		};
	}

	private static XWScriptCompiler.CompileResult CreateCancelledResult()
	{
		return new XWScriptCompiler.CompileResult
		{
			Success = false,
			IsModProject = true,
			Cancelled = true,
			ExitCode = -2,
			Output = "BUILD_CANCELLED: the background build was cancelled.",
			Diagnostics = new List<XWCodeErrorChecker.ErrorData>
			{
				new XWCodeErrorChecker.ErrorData
				{
					Line = 0,
					Column = 0,
					Message = "BUILD_CANCELLED: the background build was cancelled.",
					SeverityLevel = XWCodeErrorChecker.Severity.Warning,
					Code = "BUILD_CANCELLED"
				}
			}
		};
	}

	private static void PublishStateChanged(string normalizedRoot, bool busy, SynchronizationContext notificationContext)
	{
		Action<string, bool> handlers = XWModBuildSingleFlight.FlightStateChanged;
		if (handlers == null)
		{
			return;
		}
		if (notificationContext != null && SynchronizationContext.Current != notificationContext)
		{
			notificationContext.Post(delegate
			{
				InvokeHandlers();
			}, null);
		}
		else
		{
			InvokeHandlers();
		}
		void InvokeHandlers()
		{
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action<string, bool> action = (Action<string, bool>)invocationList[i];
				try
				{
					action(normalizedRoot, busy);
				}
				catch (Exception ex)
				{
					GD.PushWarning("Mod build flight state listener failed: " + ex.GetBaseException().Message);
				}
			}
		}
	}
}
