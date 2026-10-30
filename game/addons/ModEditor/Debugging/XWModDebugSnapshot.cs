using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugSnapshot : EventArgs
{
	public long Sequence { get; }

	public XWModDebugState State { get; }

	public XWModDebugPauseReason PauseReason { get; }

	public string StatusText { get; }

	public XWModDebugCheckpoint CurrentCheckpoint { get; }

	public IReadOnlyList<XWModDebugStackFrame> CallStack { get; }

	public IReadOnlyDictionary<string, XWModDebugVariable> Variables { get; }

	public DateTimeOffset Timestamp { get; }

	public bool CanContinue => State == XWModDebugState.Paused;

	public bool CanStep => State == XWModDebugState.Paused;

	public bool CanStop => State != XWModDebugState.Stopped;

	internal XWModDebugSnapshot(long sequence, XWModDebugState state, XWModDebugPauseReason pauseReason, string statusText, XWModDebugCheckpoint currentCheckpoint)
	{
		Sequence = sequence;
		State = state;
		PauseReason = pauseReason;
		StatusText = statusText ?? string.Empty;
		CurrentCheckpoint = currentCheckpoint?.Copy();
		CallStack = CurrentCheckpoint?.CallStack ?? Array.Empty<XWModDebugStackFrame>();
		Variables = CurrentCheckpoint?.Variables ?? new ReadOnlyDictionary<string, XWModDebugVariable>(new Dictionary<string, XWModDebugVariable>(StringComparer.Ordinal));
		Timestamp = DateTimeOffset.UtcNow;
	}
}
