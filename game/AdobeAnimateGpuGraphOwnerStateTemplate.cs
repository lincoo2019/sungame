using System;
using Godot;
using Godot.Collections;

internal sealed class AdobeAnimateGpuGraphOwnerStateTemplate
{
	public AdobeAnimateSprite SourceSprite { get; }

	public AdobeAnimateRuntimeDefinition Definition { get; }

	public Vector2 Offset { get; }

	public VerticalClipState VerticalClip { get; }

	public bool AllLayersVisible { get; }

	public bool CanUseLayerMask { get; }

	public ulong LayerMask { get; }

	public Array<bool> LayerVisible { get; }

	public int LayerCount { get; }

	public bool HasMediaReplace { get; }

	public Array<Rect2> MediaReplaceRect { get; }

	public Array<bool> MediaReplaceUse { get; }

	public Array<int> MediaReplaceAtlasPages { get; }

	public Vector2 MediaReplaceAtlasSize { get; }

	public int MediaCount { get; }

	public ulong MediaReplaceSignature { get; }

	public AdobeAnimateGpuGraphOwnerStateTemplate(AdobeAnimateSprite sourceSprite, AdobeAnimateRuntimeDefinition definition, Vector2 offset, VerticalClipState verticalClip, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int layerCount, bool hasMediaReplace, Array<Rect2> mediaReplaceRect, Array<bool> mediaReplaceUse, Array<int> mediaReplaceAtlasPages, Vector2 mediaReplaceAtlasSize, int mediaCount, ulong mediaReplaceSignature)
	{
		SourceSprite = sourceSprite;
		Definition = definition;
		Offset = offset;
		VerticalClip = verticalClip;
		AllLayersVisible = allLayersVisible;
		CanUseLayerMask = canUseLayerMask;
		LayerMask = layerMask;
		LayerVisible = layerVisible;
		LayerCount = Math.Max(0, layerCount);
		HasMediaReplace = hasMediaReplace;
		MediaReplaceRect = mediaReplaceRect;
		MediaReplaceUse = mediaReplaceUse;
		MediaReplaceAtlasPages = mediaReplaceAtlasPages;
		MediaReplaceAtlasSize = mediaReplaceAtlasSize;
		MediaCount = Math.Max(0, mediaCount);
		MediaReplaceSignature = mediaReplaceSignature;
	}
}
