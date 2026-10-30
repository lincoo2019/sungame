using System;
using Godot;
using Godot.Collections;

internal readonly struct AdobeAnimateRenderSnapshot(AdobeAnimateSprite sprite, AdobeAnimateData data, AdobeAnimateRuntimeDefinition definition, string clip, Vector2I clipRange, float frameFloat, Transform2D globalTransform, Vector2 offset, Color modulate, Array<bool> layerVisible, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, int layerVisibleCount, bool[] layerVisibleValues, Texture2D mediaReplaceAtlas, Array<Rect2> mediaReplaceRect, Array<bool> mediaReplaceUse, bool hasMediaReplace, int mediaReplaceLimit, ulong mediaReplaceUseMask, bool mediaReplaceUseMaskOverflow, Array<int> mediaReplaceAtlasPages, int mediaReplaceAtlasPageCount, bool[] mediaReplaceUseValues, Rect2[] mediaReplaceRects, int[] mediaReplaceAtlasPageValues, TextureLayered mediaReplaceAtlasArray, Vector2 mediaReplaceAtlasArraySize, bool mediaReplaceAtlasUsesTextureArray, VerticalClipState verticalClip, Node renderMountParent, int canvasLayer, int effectiveZIndex, int[] treeOrderPath, bool needsDrawItemSort, AdobeAnimateClipBlendState clipBlend = default(AdobeAnimateClipBlendState))
{
	public AdobeAnimateSprite Sprite { get; } = sprite;

	public AdobeAnimateData Data { get; } = data;

	public AdobeAnimateRuntimeDefinition Definition { get; } = definition;

	public string Clip { get; } = clip;

	public Vector2I ClipRange { get; } = clipRange;

	public float FrameFloat { get; } = frameFloat;

	public Transform2D GlobalTransform { get; } = globalTransform;

	public Vector2 Offset { get; } = offset;

	public Color Modulate { get; } = modulate;

	public Array<bool> LayerVisible { get; } = layerVisible;

	public bool AllLayersVisible { get; } = allLayersVisible;

	public bool CanUseLayerMask { get; } = canUseLayerMask;

	public ulong LayerMask { get; } = layerMask;

	public int LayerVisibleCount { get; } = Math.Max(0, layerVisibleCount);

	public bool[] LayerVisibleValues { get; } = layerVisibleValues ?? System.Array.Empty<bool>();

	public Texture2D MediaReplaceAtlas { get; } = mediaReplaceAtlas;

	public Array<Rect2> MediaReplaceRect { get; } = mediaReplaceRect;

	public Array<bool> MediaReplaceUse { get; } = mediaReplaceUse;

	public bool HasMediaReplace { get; } = hasMediaReplace;

	public int MediaReplaceLimit { get; } = Math.Max(0, mediaReplaceLimit);

	public ulong MediaReplaceUseMask { get; } = mediaReplaceUseMask;

	public bool MediaReplaceUseMaskOverflow { get; } = mediaReplaceUseMaskOverflow;

	public Array<int> MediaReplaceAtlasPages { get; } = mediaReplaceAtlasPages;

	public int MediaReplaceAtlasPageCount { get; } = Math.Max(0, mediaReplaceAtlasPageCount);

	public bool[] MediaReplaceUseValues { get; } = mediaReplaceUseValues ?? System.Array.Empty<bool>();

	public Rect2[] MediaReplaceRects { get; } = mediaReplaceRects ?? System.Array.Empty<Rect2>();

	public int[] MediaReplaceAtlasPageValues { get; } = mediaReplaceAtlasPageValues ?? System.Array.Empty<int>();

	public TextureLayered MediaReplaceAtlasArray { get; } = mediaReplaceAtlasArray;

	public Vector2 MediaReplaceAtlasArraySize { get; } = mediaReplaceAtlasArraySize;

	public bool MediaReplaceAtlasUsesTextureArray { get; } = mediaReplaceAtlasUsesTextureArray;

	public VerticalClipState VerticalClip { get; } = verticalClip;

	public Node RenderMountParent { get; } = renderMountParent;

	public int CanvasLayer { get; } = canvasLayer;

	public int EffectiveZIndex { get; } = effectiveZIndex;

	public int[] TreeOrderPath { get; } = (treeOrderPath == null || treeOrderPath.Length == 0) ? System.Array.Empty<int>() : treeOrderPath;

	public bool NeedsDrawItemSort { get; } = needsDrawItemSort;

	public AdobeAnimateClipBlendState ClipBlend { get; } = clipBlend;
}
