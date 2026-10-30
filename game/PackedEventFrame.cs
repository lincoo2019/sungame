using Godot.Collections;

public readonly struct PackedEventFrame(int frame, Array events)
{
	public int Frame { get; } = frame;

	public Array Events { get; } = events;
}
