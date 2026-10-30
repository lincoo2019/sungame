using System;
using Godot;

internal readonly struct AdobeAnimateRasterCompositeRenderState(AdobeAnimateRuntimeDefinition definition, Node renderMountParent, Transform2D globalTransform, Color modulate, VerticalClipState verticalClip, int effectiveZIndex, AdobeAnimateRasterCompositeData rasterCompositeData, int rasterCompositeTileIndex)
{
	public AdobeAnimateRuntimeDefinition Definition { get; } = definition;

	public Node RenderMountParent { get; } = renderMountParent;

	public Transform2D GlobalTransform { get; } = globalTransform;

	public Color Modulate { get; } = modulate;

	public VerticalClipState VerticalClip { get; } = verticalClip;

	public int EffectiveZIndex { get; } = effectiveZIndex;

	public AdobeAnimateRasterCompositeData RasterCompositeData { get; } = rasterCompositeData;

	public int RasterCompositeTileIndex { get; } = Math.Max(0, rasterCompositeTileIndex);

	public AdobeAnimateRasterCompositeRenderState WithDynamicState(in Transform2D globalTransform, in Color modulate, int effectiveZIndex, int rasterCompositeTileIndex)
	{
		return new AdobeAnimateRasterCompositeRenderState(Definition, RenderMountParent, globalTransform, modulate, VerticalClip, effectiveZIndex, RasterCompositeData, rasterCompositeTileIndex);
	}
}
