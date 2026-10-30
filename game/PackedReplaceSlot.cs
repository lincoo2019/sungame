using Godot;

public readonly struct PackedReplaceSlot(StringName mediaName, int mediaId, Vector2 size)
{
	public StringName MediaName { get; } = mediaName;

	public int MediaId { get; } = mediaId;

	public Vector2 Size { get; } = size;
}
