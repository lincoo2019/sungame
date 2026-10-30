using System;
using System.Collections.Generic;
using Godot;

internal sealed class HealthTextLayoutCache
{
	internal const float TextWidth = 112f;

	internal const int FontSize = 14;

	private const int MaxCachedTextLines = 4096;

	private readonly Dictionary<string, CachedTextLine> _lines = new Dictionary<string, CachedTextLine>(StringComparer.Ordinal);

	private readonly Font _font;

	private ulong _useSequence;

	public int Count => _lines.Count;

	public long BuildCount { get; private set; }

	public HealthTextLayoutCache(Font font)
	{
		_font = font;
	}

	public CachedTextLine GetOrCreate(string text)
	{
		if (string.IsNullOrEmpty(text) || !GodotObject.IsInstanceValid(_font))
		{
			return null;
		}
		ulong lastUse = ++_useSequence;
		if (_lines.TryGetValue(text, out var value))
		{
			if (GodotObject.IsInstanceValid(value.Line))
			{
				value.LastUse = lastUse;
				return value;
			}
			_lines.Remove(text);
		}
		if (_lines.Count >= 4096)
		{
			EvictLeastRecentlyUsed();
		}
		TextLine textLine = new TextLine();
		textLine.Width = 112f;
		textLine.Alignment = HorizontalAlignment.Center;
		textLine.AddString(text, _font, 14);
		value = new CachedTextLine(textLine, lastUse);
		_lines.Add(text, value);
		BuildCount++;
		return value;
	}

	public void Dispose()
	{
		foreach (CachedTextLine value in _lines.Values)
		{
			value.Dispose();
		}
		_lines.Clear();
	}

	private void EvictLeastRecentlyUsed()
	{
		string text = null;
		CachedTextLine cachedTextLine = null;
		foreach (KeyValuePair<string, CachedTextLine> line in _lines)
		{
			if (cachedTextLine == null || line.Value.LastUse < cachedTextLine.LastUse)
			{
				text = line.Key;
				cachedTextLine = line.Value;
			}
		}
		if (text != null && _lines.Remove(text))
		{
			cachedTextLine.Dispose();
		}
	}
}
