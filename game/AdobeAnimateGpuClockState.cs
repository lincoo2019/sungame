using System;

internal readonly struct AdobeAnimateGpuClockState(bool enabled, float startTime, float startFrame, float framesPerSecond, float clipStart, float clipEndExclusive, bool loop)
{
	public bool Enabled { get; } = enabled;

	public float StartTime { get; } = startTime;

	public float StartFrame { get; } = startFrame;

	public float FramesPerSecond { get; } = framesPerSecond;

	public float ClipStart { get; } = clipStart;

	public float ClipEndExclusive { get; } = Math.Max(clipStart + 1f, clipEndExclusive);

	public bool Loop { get; } = loop;
}
