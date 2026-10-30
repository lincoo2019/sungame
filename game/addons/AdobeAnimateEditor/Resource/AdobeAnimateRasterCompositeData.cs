using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AdobeAnimateRasterCompositeData : Resource
{
	[Export(PropertyHint.None, "")]
	public Texture2D atlas { get; set; }

	[Export(PropertyHint.None, "")]
	public Vector2I tileSize { get; set; }

	[Export(PropertyHint.None, "")]
	public Vector2 origin { get; set; }

	[Export(PropertyHint.None, "")]
	public int columns { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public int variantFrameStride { get; set; }

	[Export(PropertyHint.None, "")]
	public long[] layerVisibilitySignatures { get; set; } = System.Array.Empty<long>();

	[Export(PropertyHint.None, "")]
	public long ignoredMediaReplaceMask { get; set; }

	[Export(PropertyHint.None, "")]
	public Dictionary<string, Vector3I> clipTiles { get; set; } = new Dictionary<string, Vector3I>();

	public bool AllowsMediaReplaceMask(ulong activeMask, bool hasOverflow)
	{
		if (hasOverflow)
		{
			return false;
		}
		ulong num = (ulong)ignoredMediaReplaceMask;
		return (activeMask & ~num) == 0;
	}

	public bool TryResolveTile(string clip, Vector2I clipRange, float frameFloat, ulong layerVisibilitySignature, out int tileIndex)
	{
		tileIndex = 0;
		if (!TryResolveSequence(clip, clipRange, layerVisibilitySignature, out var tileBase, out var clipStart, out var frameCount))
		{
			return false;
		}
		int num = Math.Clamp(Mathf.FloorToInt(frameFloat) - clipStart, 0, frameCount - 1);
		tileIndex = tileBase + num;
		return tileIndex >= 0;
	}

	public bool TryResolveSequence(string clip, Vector2I clipRange, ulong layerVisibilitySignature, out int tileBase, out int clipStart, out int frameCount)
	{
		tileBase = 0;
		clipStart = 0;
		frameCount = 0;
		if (!GodotObject.IsInstanceValid(atlas) || tileSize.X <= 0 || tileSize.Y <= 0 || columns <= 0 || variantFrameStride <= 0 || string.IsNullOrEmpty(clip) || clipTiles == null || !clipTiles.TryGetValue(clip, out var value))
		{
			return false;
		}
		clipStart = value.X;
		frameCount = value.Y;
		int z = value.Z;
		if (frameCount <= 0 || clipRange.X != clipStart || clipRange.Y != clipStart + frameCount)
		{
			return false;
		}
		int num = ResolveVariant(layerVisibilitySignature);
		if (num < 0)
		{
			return false;
		}
		tileBase = num * variantFrameStride + z;
		return tileBase >= 0;
	}

	private int ResolveVariant(ulong signature)
	{
		long[] array = layerVisibilitySignatures;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == (long)signature)
			{
				return i;
			}
		}
		return -1;
	}
}
