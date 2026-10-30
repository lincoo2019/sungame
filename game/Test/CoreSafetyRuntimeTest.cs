using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public sealed partial class CoreSafetyRuntimeTest : Node
{
	private sealed class StatisticsHarness : IDisposable
	{
		internal readonly record struct SentAttempt(string Url, string[] Headers, Action<long, long> Complete);

		internal readonly List<SentAttempt> Sent = new List<SentAttempt>();

		internal readonly List<OnlineLevelStatisticsReporter.ReportResult> Results = new List<OnlineLevelStatisticsReporter.ReportResult>();

		internal readonly OnlineLevelStatisticsReporter Reporter;

		internal Error NextStartError;

		internal int CancelCount;

		private ulong _now;

		private bool _disposed;

		internal StatisticsHarness(Node parent)
		{
			Reporter = new OnlineLevelStatisticsReporter("http://127.0.0.1/levels", () => _now, delegate(string url, string[] headers, Action<long, long> completed)
			{
				Sent.Add(new SentAttempt(url, (string[])headers.Clone(), completed));
				Error nextStartError = NextStartError;
				NextStartError = Error.Ok;
				return nextStartError;
			}, delegate
			{
				CancelCount++;
			});
			Reporter.ReportCompleted += Results.Add;
			parent.AddChild(Reporter, forceReadableName: false, InternalMode.Disabled);
		}

		internal void Complete(int attempt, NativeHttpRequest.Result result, long status)
		{
			Sent[attempt].Complete((long)result, status);
		}

		internal void Advance(ulong milliseconds)
		{
			_now += milliseconds;
			Reporter.Pump();
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				Reporter.Free();
			}
		}
	}

	private sealed class StatisticsHttpServer : IAsyncDisposable
	{
		internal readonly record struct ObservedRequest(string Method, string Path, string Headers, long Timestamp);

		internal readonly ConcurrentQueue<ObservedRequest> Requests = new ConcurrentQueue<ObservedRequest>();

		private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);

		private readonly CancellationTokenSource _stop = new CancellationTokenSource();

		private readonly List<Task> _handlers = new List<Task>();

		private readonly Task _accept;

		private int _retryRequests;

		private long _firstFailureTimestamp;

		internal string LevelsUrl { get; }

		internal long FirstFailureTimestamp => Interlocked.Read(in _firstFailureTimestamp);

		internal StatisticsHttpServer()
		{
			_listener.Start();
			LevelsUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/levels";
			_accept = Task.Run((Func<Task?>)AcceptAsync);
		}

		private async Task AcceptAsync()
		{
			try
			{
				while (!_stop.IsCancellationRequested)
				{
					TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token);
					_handlers.Add(ServeAsync(client));
				}
			}
			catch (OperationCanceledException) when (_stop.IsCancellationRequested)
			{
			}
			catch (SocketException) when (_stop.IsCancellationRequested)
			{
			}
		}

		private async Task ServeAsync(TcpClient client)
		{
			using (client)
			{
				using NetworkStream stream = client.GetStream();
				_ = 3;
				try
				{
					byte[] buffer = new byte[1024];
					StringBuilder header = new StringBuilder();
					while (!header.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
					{
						int num = await stream.ReadAsync(buffer, _stop.Token);
						if (num == 0)
						{
							throw new IOException("Incomplete local HTTP request.");
						}
						header.Append(Encoding.ASCII.GetString(buffer, 0, num));
						if (header.Length > 16384)
						{
							throw new IOException("Local HTTP request headers exceeded budget.");
						}
					}
					string[] array = header.ToString().Split("\r\n", 2)[0].Split(' ');
					ObservedRequest item = new ObservedRequest(array[0], array[1], header.ToString(), Stopwatch.GetTimestamp());
					Requests.Enqueue(item);
					if (item.Path == "/levels/disconnected/failure")
					{
						return;
					}
					if (item.Path == "/levels/timeout/failure")
					{
						await Task.Delay(-1, _stop.Token);
						return;
					}
					string s;
					if (item.Path == "/levels/partial/failure")
					{
						s = "HTTP/1.1 503 Unavailable\r\nContent-Length: 10\r\nConnection: close\r\n\r\nx";
					}
					else if (!(item.Path == "/levels/retry/completion") || Interlocked.Increment(ref _retryRequests) != 1)
					{
						s = ((!(item.Path == "/levels/redirect/abandon")) ? "HTTP/1.1 204 No Content\r\nConnection: close\r\n\r\n" : "HTTP/1.1 307 Redirect\r\nLocation: /forwarded\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
					}
					else
					{
						Interlocked.Exchange(ref _firstFailureTimestamp, Stopwatch.GetTimestamp());
						s = "HTTP/1.1 500 Failure\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
					}
					await stream.WriteAsync(Encoding.ASCII.GetBytes(s), _stop.Token);
					await stream.FlushAsync(_stop.Token);
				}
				catch (OperationCanceledException) when (_stop.IsCancellationRequested)
				{
				}
			}
		}

		public async ValueTask DisposeAsync()
		{
			_stop.Cancel();
			_listener.Stop();
			try
			{
				await _accept;
				await Task.WhenAll(_handlers);
			}
			finally
			{
				_stop.Dispose();
			}
		}
	}

	private int _statisticsChecks;

	public override async void _Ready()
	{
		bool flag = true;
		try
		{
			flag &= TestNetworkBudgets();
			flag &= TestEndpointParsing();
			flag &= TestAtomicSave();
			bool flag2 = flag;
			flag = flag2 & await TestHttpResponseBudgetAsync();
			flag &= TestStatisticsRetryStateMachine();
			flag2 = flag;
			flag = flag2 & await TestStatisticsHttpAsync();
		}
		catch (Exception value)
		{
			flag = false;
			GD.PrintErr($"CORE_SAFETY_RUNTIME_FAILURE exception={value}");
		}
		GD.Print($"CORE_SAFETY_RUNTIME_RESULT passed={flag}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool TestNetworkBudgets()
	{
		string data;
		NetMessageEnvelope envelope;
		bool flag = NetProtocolSerializer.TrySerialize(new NetMessageEnvelope
		{
			message_type = NetMessageType.GameStateSync,
			payload = "{}"
		}, out data) && NetProtocolSerializer.TryDeserialize(data, out envelope) && envelope.message_type == NetMessageType.GameStateSync;
		string data2;
		bool flag2 = !NetProtocolSerializer.TrySerialize(new NetMessageEnvelope
		{
			message_type = NetMessageType.GameStateSync,
			payload = new string('a', 3145729)
		}, out data2);
		NetMessageEnvelope envelope2;
		bool flag3 = !NetProtocolSerializer.TryDeserialize(new string('a', 4194305), out envelope2);
		bool flag4 = flag && flag2 && flag3;
		GD.Print($"CORE_SAFETY_NETWORK_BUDGET passed={flag4} valid={flag} sendRejected={flag2} receiveRejected={flag3}");
		return flag4;
	}

	private static bool TestEndpointParsing()
	{
		string host;
		int port;
		bool flag = MultiPlayerManager.TryParseMatchEndpoint("example.local:8123", out host, out port) && host == "example.local" && port == 8123;
		string host2;
		int port2;
		bool flag2 = MultiPlayerManager.TryParseMatchEndpoint("127.0.0.1", out host2, out port2) && host2 == "127.0.0.1" && port2 == 7777;
		string host3;
		int port3;
		bool flag3 = MultiPlayerManager.TryParseMatchEndpoint("[::1]:9000", out host3, out port3) && host3 == "::1" && port3 == 9000;
		string host4;
		int port4;
		bool flag4 = !MultiPlayerManager.TryParseMatchEndpoint("127.0.0.1:70000", out host4, out port4);
		bool flag5 = !MultiPlayerManager.TryParseMatchEndpoint("127.0.0.1:not-a-port", out host4, out port4);
		bool flag6 = flag && flag2 && flag3 && flag4 && flag5;
		GD.Print($"CORE_SAFETY_ENDPOINT passed={flag6} hostname={flag} ipv4={flag2} ipv6={flag3} invalidPort={flag4} invalidText={flag5}");
		return flag6;
	}

	private static bool TestAtomicSave()
	{
		DirAccess.MakeDirRecursiveAbsolute("user://Csharp/Test");
		GameSaveManager gameSaveManager = new GameSaveManager();
		GameConfigSaveConfigCSharp gameConfigSaveConfigCSharp = new GameConfigSaveConfigCSharp();
		gameConfigSaveConfigCSharp.saveDictionary["marker"] = "old";
		Error num = ResourceSaver.Save(gameConfigSaveConfigCSharp, "user://Csharp/Test/core_safety_atomic.res", ResourceSaver.SaverFlags.None);
		GameConfigSaveConfigCSharp gameConfigSaveConfigCSharp2 = new GameConfigSaveConfigCSharp
		{
			saveDictionary = { [(Variant)"marker"] = "new" }
		};
		Error error = gameSaveManager.SaveResourceAtomically(gameConfigSaveConfigCSharp2, "user://Csharp/Test/core_safety_atomic.res");
		GameConfigSaveConfigCSharp gameConfigSaveConfigCSharp3 = ResourceLoader.Load<GameConfigSaveConfigCSharp>("user://Csharp/Test/core_safety_atomic.res", "", ResourceLoader.CacheMode.Ignore);
		bool flag = num == Error.Ok && error == Error.Ok && gameConfigSaveConfigCSharp3 != null && gameConfigSaveConfigCSharp3.saveDictionary.GetValueOrDefault("marker", "").AsString() == "new";
		using (Godot.FileAccess fileAccess = Godot.FileAccess.Open("user://Csharp/Test/core_safety_preserved.bad", Godot.FileAccess.ModeFlags.Write))
		{
			fileAccess.StoreString("original");
		}
		Error error2 = gameSaveManager.SaveResourceAtomically(gameConfigSaveConfigCSharp2, "user://Csharp/Test/core_safety_preserved.bad");
		using Godot.FileAccess fileAccess2 = Godot.FileAccess.Open("user://Csharp/Test/core_safety_preserved.bad", Godot.FileAccess.ModeFlags.Read);
		bool flag2 = error2 != Error.Ok && fileAccess2 != null && fileAccess2.GetAsText() == "original";
		DirAccess.RemoveAbsolute("user://Csharp/Test/core_safety_atomic.res");
		DirAccess.RemoveAbsolute("user://Csharp/Test/core_safety_preserved.bad");
		DirAccess.RemoveAbsolute("user://Csharp/Test");
		gameConfigSaveConfigCSharp3?.Dispose();
		gameConfigSaveConfigCSharp.Dispose();
		gameConfigSaveConfigCSharp2.Dispose();
		gameSaveManager.Free();
		bool flag3 = flag && flag2;
		GD.Print($"CORE_SAFETY_ATOMIC_SAVE passed={flag3} replaced={flag} preserved={flag2} rejected={error2}");
		return flag3;
	}

	private async Task<bool> TestHttpResponseBudgetAsync()
	{
		TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		Task serverTask = ServeOversizedHeaderAsync(listener);
		NativeHttpRequest request = new NativeHttpRequest();
		AddChild(request, forceReadableName: false, InternalMode.Disabled);
		TaskCompletionSource<(long Result, int BodyLength)> completion = new TaskCompletionSource<(long, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
		request.RequestCompleted += delegate(long result, long responseCode, string[] headers, byte[] body)
		{
			completion.TrySetResult((result, (body != null) ? body.Length : 0));
		};
		Error requestError = request.Request($"http://127.0.0.1:{port}/", Array.Empty<string>());
		Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(5L)));
		listener.Stop();
		await serverTask;
		bool passed = requestError == Error.Ok && finished == completion.Task && completion.Task.Result.Result == 8 && completion.Task.Result.BodyLength == 0;
		request.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GD.Print($"CORE_SAFETY_HTTP_BUDGET passed={passed} requestError={requestError} completed={finished == completion.Task}");
		return passed;
	}

	private static async Task ServeOversizedHeaderAsync(TcpListener listener)
	{
		using TcpClient client = await listener.AcceptTcpClientAsync();
		using NetworkStream stream = client.GetStream();
		byte[] array = new byte[2048];
		if (await stream.ReadAsync(array) > 0)
		{
			string s = $"HTTP/1.1 200 OK\r\nContent-Length: {33554433}\r\nConnection: close\r\n\r\n";
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			await stream.WriteAsync(bytes);
			await stream.FlushAsync();
		}
	}

	private void CheckStatistics(bool condition, string message)
	{
		_statisticsChecks++;
		if (!condition)
		{
			throw new InvalidOperationException($"Statistics check {_statisticsChecks}: {message}");
		}
	}

	private bool TestStatisticsRetryStateMachine()
	{
		long[] array = new long[3] { 200L, 204L, 299L };
		foreach (long num in array)
		{
			using StatisticsHarness statisticsHarness = new StatisticsHarness(this);
			statisticsHarness.Reporter.Enqueue("7", "completion", Array.Empty<string>());
			statisticsHarness.Complete(0, NativeHttpRequest.Result.Success, num);
			statisticsHarness.Advance(60000uL);
			CheckStatistics(statisticsHarness.Sent.Count == 1 && statisticsHarness.Results.Count == 1 && statisticsHarness.Results[0].Outcome == OnlineLevelStatisticsReporter.Outcome.Success, $"HTTP {num} must finish once without retrying.");
		}
		(NativeHttpRequest.Result, long)[] array2 = new(NativeHttpRequest.Result, long)[8]
		{
			(NativeHttpRequest.Result.Success, 500L),
			(NativeHttpRequest.Result.Success, 502L),
			(NativeHttpRequest.Result.Success, 503L),
			(NativeHttpRequest.Result.Success, 504L),
			(NativeHttpRequest.Result.Success, 599L),
			(NativeHttpRequest.Result.CantConnect, 0L),
			(NativeHttpRequest.Result.CantResolve, 0L),
			(NativeHttpRequest.Result.TlsHandshakeError, 0L)
		};
		for (int i = 0; i < array2.Length; i++)
		{
			(NativeHttpRequest.Result, long) value = array2[i];
			using StatisticsHarness statisticsHarness2 = new StatisticsHarness(this);
			statisticsHarness2.Reporter.Enqueue("8", "failure", Array.Empty<string>());
			statisticsHarness2.Advance(90000uL);
			CheckStatistics(statisticsHarness2.Sent.Count == 1, "A pending attempt must never overlap a new attempt.");
			statisticsHarness2.Complete(0, value.Item1, value.Item2);
			statisticsHarness2.Advance(9999uL);
			CheckStatistics(statisticsHarness2.Sent.Count == 1 && statisticsHarness2.Results.Count == 0, $"{value}: retry must wait ten seconds after completion.");
			statisticsHarness2.Complete(0, value.Item1, value.Item2);
			statisticsHarness2.Advance(1uL);
			CheckStatistics(statisticsHarness2.Sent.Count == 2, $"{value}: duplicate failure must not restart the delay.");
			statisticsHarness2.Complete(0, NativeHttpRequest.Result.Success, 200L);
			CheckStatistics(statisticsHarness2.Results.Count == 0, "The previous attempt's late success must not finish the active retry.");
			statisticsHarness2.Complete(1, NativeHttpRequest.Result.Success, 204L);
			statisticsHarness2.Complete(1, value.Item1, value.Item2);
			statisticsHarness2.Advance(60000uL);
			CheckStatistics(statisticsHarness2.Sent.Count == 2 && statisticsHarness2.Results.Count == 1 && statisticsHarness2.Results[0].Attempts == 2 && statisticsHarness2.Results[0].Outcome == OnlineLevelStatisticsReporter.Outcome.Success, $"{value}: success must finish the event exactly once.");
		}
		using (StatisticsHarness statisticsHarness3 = new StatisticsHarness(this))
		{
			statisticsHarness3.Reporter.Enqueue("9", "abandon", Array.Empty<string>());
			for (int j = 0; j < 3; j++)
			{
				statisticsHarness3.Complete(j, NativeHttpRequest.Result.Success, 503L);
				statisticsHarness3.Advance(10000uL);
			}
			statisticsHarness3.Complete(2, NativeHttpRequest.Result.Success, 503L);
			statisticsHarness3.Advance(60000uL);
			CheckStatistics(statisticsHarness3.Sent.Count == 3 && statisticsHarness3.Results.Count == 1 && statisticsHarness3.Results[0].Attempts == 3 && statisticsHarness3.Results[0].Outcome == OnlineLevelStatisticsReporter.Outcome.Exhausted, "Three failures must exhaust the event; no fourth send.");
		}
		(NativeHttpRequest.Result, long, OnlineLevelStatisticsReporter.Outcome)[] array3 = new(NativeHttpRequest.Result, long, OnlineLevelStatisticsReporter.Outcome)[14]
		{
			(NativeHttpRequest.Result.Success, 301L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 307L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 400L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 401L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 404L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 408L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 429L, OnlineLevelStatisticsReporter.Outcome.Rejected),
			(NativeHttpRequest.Result.Success, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.NoResponse, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.ConnectionError, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.RequestFailed, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.ResponseTooLarge, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.RedirectLimitReached, 0L, OnlineLevelStatisticsReporter.Outcome.Uncertain),
			(NativeHttpRequest.Result.ConnectionError, 503L, OnlineLevelStatisticsReporter.Outcome.Uncertain)
		};
		for (int i = 0; i < array3.Length; i++)
		{
			(NativeHttpRequest.Result, long, OnlineLevelStatisticsReporter.Outcome) value2 = array3[i];
			using StatisticsHarness statisticsHarness4 = new StatisticsHarness(this);
			statisticsHarness4.Reporter.Enqueue("10", "abandon", Array.Empty<string>());
			statisticsHarness4.Complete(0, value2.Item1, value2.Item2);
			statisticsHarness4.Advance(60000uL);
			CheckStatistics(statisticsHarness4.Sent.Count == 1 && statisticsHarness4.Results.Count == 1 && statisticsHarness4.Results[0].Outcome == value2.Item3, $"{value2}: a terminal or uncertain result must not retry, including incomplete 5xx responses.");
		}
		using (StatisticsHarness statisticsHarness5 = new StatisticsHarness(this))
		{
			string[] array4 = new string[1] { "X-Statistics-Probe: original" };
			statisticsHarness5.Reporter.Enqueue("11", "failure", array4);
			statisticsHarness5.Reporter.Enqueue("11", "failure", array4);
			statisticsHarness5.Reporter.Enqueue("12", "abandon", array4);
			array4[0] = "X-Statistics-Probe: changed";
			CheckStatistics(statisticsHarness5.Sent.Count == 1, "Enqueuing more events must not replace the active request.");
			statisticsHarness5.Complete(0, NativeHttpRequest.Result.Success, 500L);
			statisticsHarness5.Advance(10000uL);
			statisticsHarness5.Complete(1, NativeHttpRequest.Result.Success, 200L);
			statisticsHarness5.Reporter.Pump();
			statisticsHarness5.Complete(1, NativeHttpRequest.Result.Success, 200L);
			CheckStatistics(statisticsHarness5.Results.Count == 1, "A previous event's duplicate completion must not finish the next event.");
			statisticsHarness5.Complete(2, NativeHttpRequest.Result.Success, 200L);
			statisticsHarness5.Reporter.Pump();
			statisticsHarness5.Complete(3, NativeHttpRequest.Result.Success, 404L);
			statisticsHarness5.Advance(60000uL);
			CheckStatistics(statisticsHarness5.Sent.Select((StatisticsHarness.SentAttempt s) => s.Url).SequenceEqual(new string[4] { "http://127.0.0.1/levels/11/failure", "http://127.0.0.1/levels/11/failure", "http://127.0.0.1/levels/11/failure", "http://127.0.0.1/levels/12/abandon" }), "FIFO must preserve each event and its target, including repeated plays of the same level.");
			CheckStatistics(statisticsHarness5.Sent.All((StatisticsHarness.SentAttempt s) => s.Headers.Single() == "X-Statistics-Probe: original"), "All attempts must use the captured headers.");
			CheckStatistics(statisticsHarness5.Results.Count == 3 && statisticsHarness5.Results.Select((OnlineLevelStatisticsReporter.ReportResult r) => r.EventId).Distinct().Count() == 3 && statisticsHarness5.Results.Select((OnlineLevelStatisticsReporter.ReportResult r) => r.Attempts).SequenceEqual(new int[3] { 2, 1, 1 }), "Attempt budgets must be independent per event.");
		}
		using (StatisticsHarness statisticsHarness6 = new StatisticsHarness(this))
		{
			statisticsHarness6.NextStartError = Error.InvalidParameter;
			statisticsHarness6.Reporter.Enqueue("13", "abandon", Array.Empty<string>());
			statisticsHarness6.Reporter.Enqueue("14", "completion", Array.Empty<string>());
			statisticsHarness6.Complete(1, NativeHttpRequest.Result.Success, 200L);
			statisticsHarness6.Advance(60000uL);
			CheckStatistics(statisticsHarness6.Sent.Count == 2 && statisticsHarness6.Results.Count == 2 && statisticsHarness6.Results[0].StartError == Error.InvalidParameter && statisticsHarness6.Results[0].Outcome == OnlineLevelStatisticsReporter.Outcome.Rejected && statisticsHarness6.Results[1].Outcome == OnlineLevelStatisticsReporter.Outcome.Success, "Synchronous start errors must finish once and allow the queue to proceed.");
		}
		bool[] array5 = new bool[2] { false, true };
		foreach (bool flag in array5)
		{
			using StatisticsHarness statisticsHarness7 = new StatisticsHarness(this);
			statisticsHarness7.Reporter.Enqueue("15", "failure", Array.Empty<string>());
			statisticsHarness7.Reporter.Enqueue("16", "abandon", Array.Empty<string>());
			if (flag)
			{
				statisticsHarness7.Complete(0, NativeHttpRequest.Result.Success, 500L);
			}
			statisticsHarness7.Dispose();
			statisticsHarness7.Complete(0, NativeHttpRequest.Result.Success, 200L);
			statisticsHarness7.Advance(60000uL);
			CheckStatistics(statisticsHarness7.CancelCount == 1 && statisticsHarness7.Sent.Count == 1 && statisticsHarness7.Results.Count == 0, $"Disposal must clear pending work and ignore late callbacks (waiting={flag}).");
		}
		GD.Print($"CORE_SAFETY_STATISTICS_STATE passed=True checks={_statisticsChecks}");
		return true;
	}

	private async Task<bool> TestStatisticsHttpAsync()
	{
		bool result;
		await using (StatisticsHttpServer server = new StatisticsHttpServer())
		{
			List<OnlineLevelStatisticsReporter.ReportResult> results = new List<OnlineLevelStatisticsReporter.ReportResult>();
			OnlineLevelStatisticsReporter reporter = new OnlineLevelStatisticsReporter(server.LevelsUrl);
			reporter.ReportCompleted += results.Add;
			AddChild(reporter, forceReadableName: false, InternalMode.Disabled);
			bool wasPaused = GetTree().Paused;
			double oldScale = Engine.TimeScale;
			ProcessModeEnum oldMode = base.ProcessMode;
			NativeHttpRequest normalRequest = null;
			OnlineLevelStatisticsReporter uncertainReporter = null;
			try
			{
				string[] array = new string[1] { "X-Statistics-Probe: original" };
				reporter.Enqueue("retry", "completion", array);
				reporter.Enqueue("queued", "abandon", array);
				array[0] = "X-Statistics-Probe: changed";
				base.ProcessMode = ProcessModeEnum.Always;
				GetTree().Paused = true;
				Engine.TimeScale = 0.1;
				await WaitStatisticsAsync(() => results.Count == 2, 16.0, "real retry and FIFO completion while paused");
				GetTree().Paused = wasPaused;
				Engine.TimeScale = oldScale;
				StatisticsHttpServer.ObservedRequest[] array2 = server.Requests.ToArray();
				CheckStatistics(array2.Length == 3 && array2[0].Path == "/levels/retry/completion" && array2[1].Path == array2[0].Path && array2[2].Path == "/levels/queued/abandon", "Real HTTP requests must retain FIFO order.");
				CheckStatistics(array2.All((StatisticsHttpServer.ObservedRequest r) => r.Method == "POST" && r.Headers.Contains("X-Statistics-Probe: original", StringComparison.Ordinal)), "Real retries must retain the POST method and captured headers.");
				double elapsed = Stopwatch.GetElapsedTime(server.FirstFailureTimestamp, array2[1].Timestamp).TotalMilliseconds;
				CheckStatistics(elapsed >= 9999.0 && results[0].Attempts == 2 && results[1].Attempts == 1 && results.All((OnlineLevelStatisticsReporter.ReportResult r) => r.Outcome == OnlineLevelStatisticsReporter.Outcome.Success), "Retry must wait ten real seconds after 500; 204 empty body must succeed.");
				reporter.Enqueue("redirect", "abandon", Array.Empty<string>());
				await WaitStatisticsAsync(() => results.Count == 3, 5.0, "statistics redirect rejection");
				CheckStatistics(results[2].StatusCode == 307 && results[2].Outcome == OnlineLevelStatisticsReporter.Outcome.Rejected && !server.Requests.Any((StatisticsHttpServer.ObservedRequest r) => r.Path == "/forwarded"), "Statistics POST must not follow redirects.");
				normalRequest = new NativeHttpRequest();
				AddChild(normalRequest, forceReadableName: false, InternalMode.Disabled);
				long normalStatus = 0L;
				normalRequest.RequestCompleted += delegate(long num2, long status, string[] responseHeaders, byte[] body)
				{
					normalStatus = status;
				};
				CheckStatistics(normalRequest.Request(server.LevelsUrl + "/redirect/abandon", Array.Empty<string>()) == Error.Ok, "Ordinary request must start.");
				await WaitStatisticsAsync(() => normalStatus != 0, 5.0, "ordinary HTTP redirect");
				CheckStatistics(normalStatus == 204 && server.Requests.Count((StatisticsHttpServer.ObservedRequest r) => r.Path == "/forwarded") == 1, "Existing callers must still follow redirects by default.");
				ulong now = 0uL;
				List<OnlineLevelStatisticsReporter.ReportResult> uncertain = new List<OnlineLevelStatisticsReporter.ReportResult>();
				uncertainReporter = new OnlineLevelStatisticsReporter(server.LevelsUrl, () => now);
				uncertainReporter.ReportCompleted += uncertain.Add;
				AddChild(uncertainReporter, forceReadableName: false, InternalMode.Disabled);
				uncertainReporter.Enqueue("disconnected", "failure", Array.Empty<string>());
				uncertainReporter.Enqueue("partial", "failure", Array.Empty<string>());
				uncertainReporter.Enqueue("timeout", "failure", Array.Empty<string>());
				await WaitStatisticsAsync(() => uncertain.Count == 3, 36.0, "connection loss, incomplete response, and real request timeout");
				now += 60000uL;
				uncertainReporter.Pump();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				CheckStatistics(uncertain.All((OnlineLevelStatisticsReporter.ReportResult r) => r.Outcome == OnlineLevelStatisticsReporter.Outcome.Uncertain && r.Attempts == 1) && uncertain[2].TransportResult == 5, "Uncertain transport outcomes must stop after one attempt.");
				string[] array3 = new string[3] { "disconnected", "partial", "timeout" };
				foreach (string level in array3)
				{
					CheckStatistics(server.Requests.Count((StatisticsHttpServer.ObservedRequest r) => r.Path == "/levels/" + level + "/failure") == 1, "Server already received " + level + "; the client must not resubmit it.");
				}
				GD.Print($"CORE_SAFETY_STATISTICS_HTTP passed=True retryDelayMs={elapsed:F1} requests={server.Requests.Count} checks={_statisticsChecks}");
				result = true;
			}
			finally
			{
				GetTree().Paused = wasPaused;
				Engine.TimeScale = oldScale;
				base.ProcessMode = oldMode;
				normalRequest?.Free();
				uncertainReporter?.Free();
				reporter.Free();
			}
		}
		return result;
	}

	private async Task WaitStatisticsAsync(Func<bool> condition, double timeoutSeconds, string label)
	{
		ulong deadline = Time.GetTicksMsec() + (ulong)(timeoutSeconds * 1000.0);
		while (!condition())
		{
			if (Time.GetTicksMsec() >= deadline)
			{
				throw new TimeoutException("Statistics test timed out: " + label);
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}
