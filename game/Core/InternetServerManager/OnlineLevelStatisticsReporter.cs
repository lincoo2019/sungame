using System;
using System.Collections.Generic;
using System.Net.Http;
using Godot;
using Microsoft.Extensions.Logging;
using ZLogger;

public sealed partial class OnlineLevelStatisticsReporter : Node
{
	internal enum Outcome
	{
		Success,
		Exhausted,
		Rejected,
		Uncertain
	}

	internal readonly record struct ReportResult(long EventId, string LevelId, string Statistic, int Attempts, Outcome Outcome, long TransportResult, long StatusCode, Error StartError);

	private sealed record PendingReport(long EventId, string LevelId, string Statistic, string Url, string[] Headers);

	private enum State
	{
		Idle,
		Sending,
		WaitingToRetry,
		Stopped
	}

	internal const int MaxAttempts = 3;

	internal const ulong RetryDelayMilliseconds = 10000uL;

	private const string LevelsUrl = "https://api.pvzhe.com/workshop/levels";

	private static readonly ILogger Logger = Log.CreateLogger<OnlineLevelStatisticsReporter>();

	private readonly Queue<PendingReport> _pending = new Queue<PendingReport>();

	private readonly string _levelsUrl;

	private readonly Func<ulong> _milliseconds;

	private readonly Func<string, string[], Action<long, long>, Error> _sendOverride;

	private readonly Action _cancelOverride;

	private NativeHttpRequest _request;

	private Action<long, long, string[], byte[]> _requestCompleted;

	private PendingReport _current;

	private State _state;

	private int _attempts;

	private long _eventSequence;

	private long _attemptSequence;

	private ulong _retryAt;

	private bool _ready;

	internal event Action<ReportResult> ReportCompleted;

	public OnlineLevelStatisticsReporter()
		: this("https://api.pvzhe.com/workshop/levels")
	{
	}

	internal OnlineLevelStatisticsReporter(string levelsUrl, Func<ulong> milliseconds = null, Func<string, string[], Action<long, long>, Error> send = null, Action cancel = null)
	{
		_levelsUrl = levelsUrl.TrimEnd('/');
		_milliseconds = milliseconds ?? ((Func<ulong>)(() => Time.GetTicksMsec()));
		_sendOverride = send;
		_cancelOverride = cancel;
		base.ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		if (_sendOverride == null)
		{
			_request = new NativeHttpRequest
			{
				FollowRedirects = false
			};
			AddChild(_request, forceReadableName: false, InternalMode.Disabled);
		}
		_ready = true;
		Pump();
	}

	public void Enqueue(string levelId, string statistic, string[] headers)
	{
		if (_state == State.Stopped)
		{
			return;
		}
		bool enabled = string.IsNullOrWhiteSpace(levelId);
		if (!enabled)
		{
			bool flag;
			switch (statistic)
			{
			case "abandon":
			case "completion":
			case "failure":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			enabled = !flag;
		}
		if (enabled)
		{
			ILogger logger = Logger;
			ZLoggerWarningInterpolatedStringHandler message = new ZLoggerWarningInterpolatedStringHandler(30, 2, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("在线关卡统计参数无效: level=");
				message.AppendFormatted(levelId, 0, null, "levelId");
				message.AppendLiteral(", statistic=");
				message.AppendFormatted(statistic, 0, null, "statistic");
			}
			logger.ZLogWarning(ref message);
		}
		else
		{
			string[] headers2 = ((headers == null) ? Array.Empty<string>() : ((string[])headers.Clone()));
			_pending.Enqueue(new PendingReport(++_eventSequence, levelId, statistic, $"{_levelsUrl}/{Uri.EscapeDataString(levelId)}/{statistic}", headers2));
			Pump();
		}
	}

	public override void _Process(double delta)
	{
		Pump();
	}

	internal void Pump()
	{
		if (!_ready || _state == State.Stopped || _state == State.Sending)
		{
			return;
		}
		if (_state == State.WaitingToRetry)
		{
			if (_milliseconds() >= _retryAt)
			{
				StartAttempt();
			}
		}
		else if (_pending.TryDequeue(out _current))
		{
			_attempts = 0;
			StartAttempt();
		}
	}

	private void StartAttempt()
	{
		_state = State.Sending;
		_attempts++;
		long attemptId = ++_attemptSequence;
		Action<long, long> completed = delegate(long result, long status)
		{
			CompleteAttempt(attemptId, result, status);
		};
		Error error;
		if (_sendOverride != null)
		{
			error = _sendOverride(_current.Url, _current.Headers, completed);
		}
		else
		{
			_requestCompleted = delegate(long result, long status, string[] headers, byte[] body)
			{
				completed(result, status);
			};
			_request.RequestCompleted += _requestCompleted;
			error = _request.Request(_current.Url, _current.Headers, HttpMethod.Post);
		}
		if (error != Error.Ok && _state == State.Sending && attemptId == _attemptSequence)
		{
			DetachCompletion();
			Finish(Outcome.Rejected, 6L, 0L, error);
		}
	}

	private void CompleteAttempt(long attemptId, long result, long status)
	{
		if (_state != State.Sending || attemptId != _attemptSequence)
		{
			return;
		}
		DetachCompletion();
		if (result == 0L && status >= 200 && status <= 299)
		{
			Finish(Outcome.Success, result, status, Error.Ok);
			return;
		}
		bool flag = result == 1 || result == 2 || result == 4 || (result == 0L && status >= 500 && status <= 599);
		if (flag && _attempts < 3)
		{
			_retryAt = _milliseconds() + 10000;
			_state = State.WaitingToRetry;
			ILogger logger = Logger;
			bool enabled;
			ZLoggerInformationInterpolatedStringHandler message = new ZLoggerInformationInterpolatedStringHandler(75, 8, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("在线关卡统计等待重试: event=");
				message.AppendFormatted(_current.EventId, 0, null, "_current.EventId");
				message.AppendLiteral(", level=");
				message.AppendFormatted(_current.LevelId, 0, null, "_current.LevelId");
				message.AppendLiteral(", statistic=");
				message.AppendFormatted(_current.Statistic, 0, null, "_current.Statistic");
				message.AppendLiteral(", attempt=");
				message.AppendFormatted(_attempts, 0, null, "_attempts");
				message.AppendLiteral("/");
				message.AppendFormatted(3, 0, null, "MaxAttempts");
				message.AppendLiteral(", result=");
				message.AppendFormatted(result, 0, null, "result");
				message.AppendLiteral(", http=");
				message.AppendFormatted(status, 0, null, "status");
				message.AppendLiteral(", delayMs=");
				message.AppendFormatted(10000uL, 0, null, "RetryDelayMilliseconds");
			}
			logger.ZLogInformation(ref message);
		}
		else
		{
			Outcome outcome = (flag ? Outcome.Exhausted : ((result == 0L && status >= 300 && status <= 499) ? Outcome.Rejected : Outcome.Uncertain));
			Finish(outcome, result, status, Error.Ok);
		}
	}

	private void Finish(Outcome outcome, long result, long status, Error startError = Error.Ok)
	{
		ReportResult obj = new ReportResult(_current.EventId, _current.LevelId, _current.Statistic, _attempts, outcome, result, status, startError);
		_current = null;
		_state = State.Idle;
		ILogger logger = Logger;
		bool enabled;
		ZLoggerInformationInterpolatedStringHandler message = new ZLoggerInformationInterpolatedStringHandler(86, 8, logger, out enabled);
		if (enabled)
		{
			message.AppendLiteral("在线关卡统计结束: event=");
			message.AppendFormatted(obj.EventId, 0, null, "report.EventId");
			message.AppendLiteral(", level=");
			message.AppendFormatted(obj.LevelId, 0, null, "report.LevelId");
			message.AppendLiteral(", statistic=");
			message.AppendFormatted(obj.Statistic, 0, null, "report.Statistic");
			message.AppendLiteral(", attempts=");
			message.AppendFormatted(obj.Attempts, 0, null, "report.Attempts");
			message.AppendLiteral(", outcome=");
			message.AppendFormatted(outcome, 0, null, "outcome");
			message.AppendLiteral(", result=");
			message.AppendFormatted(result, 0, null, "result");
			message.AppendLiteral(", http=");
			message.AppendFormatted(status, 0, null, "status");
			message.AppendLiteral(", startError=");
			message.AppendFormatted(startError, 0, null, "startError");
		}
		logger.ZLogInformation(ref message);
		this.ReportCompleted?.Invoke(obj);
	}

	private void DetachCompletion()
	{
		if (_requestCompleted != null && GodotObject.IsInstanceValid(_request))
		{
			_request.RequestCompleted -= _requestCompleted;
		}
		_requestCompleted = null;
	}

	public override void _ExitTree()
	{
		if (_state != State.Stopped)
		{
			_state = State.Stopped;
			_ready = false;
			_pending.Clear();
			_current = null;
			DetachCompletion();
			if (GodotObject.IsInstanceValid(_request))
			{
				_request.CancelRequest();
			}
			_cancelOverride?.Invoke();
			this.ReportCompleted = null;
		}
	}
}
