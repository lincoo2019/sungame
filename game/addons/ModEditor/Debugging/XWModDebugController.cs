using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugController
{
	private enum StepMode
	{
		None,
		Into,
		Over,
		Out
	}

	private readonly object _sync = new object();

	private readonly object _notificationSync = new object();

	private readonly HashSet<XWModDebugBreakpoint> _breakpoints = new HashSet<XWModDebugBreakpoint>();

	private readonly Queue<XWModDebugSnapshot> _notificationQueue = new Queue<XWModDebugSnapshot>();

	private TaskCompletionSource<XWModDebugCheckpointResult> _resumeCompletion;

	private XWModDebugCheckpoint _pausedCheckpoint;

	private XWModDebugSnapshot _snapshot;

	private XWModDebugState _state;

	private StepMode _stepMode;

	private int _stepStartDepth;

	private bool _pauseRequested;

	private bool _notificationDrainScheduled;

	private long _sequence;

	public XWModDebugState State
	{
		get
		{
			lock (_sync)
			{
				return _state;
			}
		}
	}

	public XWModDebugSnapshot Snapshot
	{
		get
		{
			lock (_sync)
			{
				return _snapshot;
			}
		}
	}

	public IReadOnlyList<XWModDebugBreakpoint> Breakpoints
	{
		get
		{
			lock (_sync)
			{
				return Array.AsReadOnly(_breakpoints.ToArray());
			}
		}
	}

	public event EventHandler<XWModDebugSnapshot> SnapshotChanged;

	public XWModDebugController()
	{
		lock (_sync)
		{
			_snapshot = CreateSnapshotLocked(XWModDebugState.Running, XWModDebugPauseReason.None, "调试运行中", null);
		}
	}

	public bool Start()
	{
		XWModDebugSnapshot snapshot;
		lock (_sync)
		{
			if (_state == XWModDebugState.Paused)
			{
				return false;
			}
			if (_state != XWModDebugState.Stopped)
			{
				return true;
			}
			_state = XWModDebugState.Running;
			_pauseRequested = false;
			_stepMode = StepMode.None;
			_pausedCheckpoint = null;
			_resumeCompletion = null;
			snapshot = (_snapshot = CreateSnapshotLocked(_state, XWModDebugPauseReason.None, "调试已开始", null));
		}
		PublishSnapshot(snapshot);
		return true;
	}

	public bool AddBreakpoint(XWModDebugBreakpoint breakpoint)
	{
		if (breakpoint == null)
		{
			throw new ArgumentNullException("breakpoint");
		}
		lock (_sync)
		{
			return _breakpoints.Add(breakpoint);
		}
	}

	public bool RemoveBreakpoint(XWModDebugBreakpoint breakpoint)
	{
		if (breakpoint == null)
		{
			return false;
		}
		lock (_sync)
		{
			return _breakpoints.Remove(breakpoint);
		}
	}

	public void ReplaceBreakpoints(IEnumerable<XWModDebugBreakpoint> breakpoints)
	{
		lock (_sync)
		{
			_breakpoints.Clear();
			if (breakpoints == null)
			{
				return;
			}
			foreach (XWModDebugBreakpoint breakpoint in breakpoints)
			{
				if (breakpoint != null)
				{
					_breakpoints.Add(breakpoint);
				}
			}
		}
	}

	public void ClearBreakpoints()
	{
		lock (_sync)
		{
			_breakpoints.Clear();
		}
	}

	public bool RequestPause()
	{
		XWModDebugSnapshot snapshot;
		lock (_sync)
		{
			if (_state != XWModDebugState.Running || _pauseRequested)
			{
				return false;
			}
			_pauseRequested = true;
			_state = XWModDebugState.PauseRequested;
			snapshot = (_snapshot = CreateSnapshotLocked(_state, XWModDebugPauseReason.PauseRequest, "已请求暂停，将在下一个安全检查点暂停", null));
		}
		PublishSnapshot(snapshot);
		return true;
	}

	public bool Continue()
	{
		return Resume(StepMode.None, "继续运行");
	}

	public bool StepInto()
	{
		return Resume(StepMode.Into, "单步进入");
	}

	public bool StepOver()
	{
		return Resume(StepMode.Over, "单步跳过");
	}

	public bool StepOut()
	{
		return Resume(StepMode.Out, "单步跳出");
	}

	public bool Stop()
	{
		TaskCompletionSource<XWModDebugCheckpointResult> resumeCompletion;
		XWModDebugSnapshot snapshot;
		lock (_sync)
		{
			if (_state == XWModDebugState.Stopped)
			{
				return false;
			}
			_state = XWModDebugState.Stopped;
			_pauseRequested = false;
			_stepMode = StepMode.None;
			_pausedCheckpoint = null;
			resumeCompletion = _resumeCompletion;
			_resumeCompletion = null;
			snapshot = (_snapshot = CreateSnapshotLocked(_state, XWModDebugPauseReason.None, "调试已停止", null));
		}
		resumeCompletion?.TrySetResult(XWModDebugCheckpointResult.Stopped);
		PublishSnapshot(snapshot);
		return true;
	}

	public async Task<XWModDebugCheckpointResult> CheckpointAsync(XWModDebugCheckpoint checkpoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (checkpoint == null)
		{
			throw new ArgumentNullException("checkpoint");
		}
		cancellationToken.ThrowIfCancellationRequested();
		XWModDebugSnapshot snapshot = null;
		XWModDebugCheckpoint xWModDebugCheckpoint = checkpoint.Copy();
		TaskCompletionSource<XWModDebugCheckpointResult> taskCompletionSource;
		lock (_sync)
		{
			if (_state == XWModDebugState.Stopped)
			{
				return XWModDebugCheckpointResult.Stopped;
			}
			if (_state == XWModDebugState.Paused && _resumeCompletion != null)
			{
				taskCompletionSource = _resumeCompletion;
			}
			else
			{
				if (!ShouldPauseLocked(xWModDebugCheckpoint, out var reason))
				{
					return XWModDebugCheckpointResult.Continue;
				}
				_pauseRequested = false;
				_stepMode = StepMode.None;
				_state = XWModDebugState.Paused;
				_pausedCheckpoint = xWModDebugCheckpoint;
				taskCompletionSource = (_resumeCompletion = new TaskCompletionSource<XWModDebugCheckpointResult>(TaskCreationOptions.RunContinuationsAsynchronously));
				snapshot = (_snapshot = CreateSnapshotLocked(_state, reason, BuildPausedStatus(reason, xWModDebugCheckpoint), xWModDebugCheckpoint));
			}
		}
		PublishSnapshot(snapshot);
		return await taskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private bool Resume(StepMode stepMode, string actionText)
	{
		TaskCompletionSource<XWModDebugCheckpointResult> resumeCompletion;
		XWModDebugSnapshot snapshot;
		lock (_sync)
		{
			if (_state != XWModDebugState.Paused || _resumeCompletion == null)
			{
				return false;
			}
			_stepMode = stepMode;
			_stepStartDepth = _pausedCheckpoint?.FrameDepth ?? 0;
			_state = XWModDebugState.Running;
			_pausedCheckpoint = null;
			resumeCompletion = _resumeCompletion;
			_resumeCompletion = null;
			snapshot = (_snapshot = CreateSnapshotLocked(_state, XWModDebugPauseReason.None, actionText, null));
		}
		resumeCompletion.TrySetResult(XWModDebugCheckpointResult.Continue);
		PublishSnapshot(snapshot);
		return true;
	}

	private bool ShouldPauseLocked(XWModDebugCheckpoint checkpoint, out XWModDebugPauseReason reason)
	{
		if (checkpoint.ForcePause)
		{
			reason = XWModDebugPauseReason.ForcedBreakpoint;
			return true;
		}
		if (_pauseRequested || _state == XWModDebugState.PauseRequested)
		{
			reason = XWModDebugPauseReason.PauseRequest;
			return true;
		}
		if (_breakpoints.Any((XWModDebugBreakpoint breakpoint) => breakpoint.Matches(checkpoint)))
		{
			reason = XWModDebugPauseReason.Breakpoint;
			return true;
		}
		bool flag = _stepMode switch
		{
			StepMode.Into => true, 
			StepMode.Over => checkpoint.FrameDepth <= _stepStartDepth, 
			StepMode.Out => checkpoint.FrameDepth < _stepStartDepth, 
			_ => false, 
		};
		reason = (flag ? XWModDebugPauseReason.Step : XWModDebugPauseReason.None);
		return flag;
	}

	private XWModDebugSnapshot CreateSnapshotLocked(XWModDebugState state, XWModDebugPauseReason reason, string statusText, XWModDebugCheckpoint checkpoint)
	{
		return new XWModDebugSnapshot(Interlocked.Increment(ref _sequence), state, reason, statusText, checkpoint);
	}

	private void PublishSnapshot(XWModDebugSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return;
		}
		lock (_notificationSync)
		{
			_notificationQueue.Enqueue(snapshot);
			if (_notificationDrainScheduled)
			{
				return;
			}
			_notificationDrainScheduled = true;
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			DrainSnapshotNotifications();
		});
	}

	private void DrainSnapshotNotifications()
	{
		while (true)
		{
			XWModDebugSnapshot e;
			lock (_notificationSync)
			{
				if (_notificationQueue.Count == 0)
				{
					_notificationDrainScheduled = false;
					break;
				}
				e = _notificationQueue.Dequeue();
			}
			EventHandler<XWModDebugSnapshot> eventHandler = this.SnapshotChanged;
			if (eventHandler == null)
			{
				continue;
			}
			Delegate[] invocationList = eventHandler.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				EventHandler<XWModDebugSnapshot> eventHandler2 = (EventHandler<XWModDebugSnapshot>)invocationList[i];
				try
				{
					eventHandler2(this, e);
				}
				catch
				{
				}
			}
		}
	}

	private static string BuildPausedStatus(XWModDebugPauseReason reason, XWModDebugCheckpoint checkpoint)
	{
		string text = ((checkpoint.Kind == XWModDebugCheckpointKind.CSharpLine) ? $"{checkpoint.SourcePath} 第 {checkpoint.Line} 行" : (checkpoint.DisplayName + "（节点 " + checkpoint.BlueprintNodeId + "）"));
		return reason switch
		{
			XWModDebugPauseReason.PauseRequest => "已按暂停请求停下", 
			XWModDebugPauseReason.Breakpoint => "已命中断点", 
			XWModDebugPauseReason.ForcedBreakpoint => "已到达断点节点", 
			XWModDebugPauseReason.Step => "单步执行已暂停", 
			_ => "调试已暂停", 
		} + "：" + text;
	}
}
