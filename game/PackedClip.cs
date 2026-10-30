using Godot;

public readonly struct PackedClip(StringName name, Vector2I range)
{
	public StringName Name { get; } = name;

	public Vector2I Range { get; } = range;
}
