using Godot;

internal sealed class CachedTextLine
{
	public readonly TextLine Line;

	public ulong LastUse;

	public CachedTextLine(TextLine line, ulong lastUse)
	{
		Line = line;
		LastUse = lastUse;
	}

	public void Dispose()
	{
		if (GodotObject.IsInstanceValid(Line))
		{
			Line.Dispose();
		}
	}
}
