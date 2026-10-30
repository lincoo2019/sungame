using System;
using Godot;
using Godot.Collections;

internal sealed class AdobeAnimateGpuGraphOwnerState
{
	internal AdobeAnimateGpuGraphOwnerStateTemplate StaticState { get; private set; }

	public AdobeAnimateSprite SourceSprite => StaticState?.SourceSprite;

	public AdobeAnimateRuntimeDefinition Definition => StaticState?.Definition;

	public Transform2D GlobalTransform { get; private set; }

	public Color Modulate { get; private set; }

	public Vector2 Offset => StaticState?.Offset ?? default(Vector2);

	public VerticalClipState VerticalClip => StaticState?.VerticalClip ?? default(VerticalClipState);

	public int FrameIndex { get; private set; }

	public float InterpolationT { get; private set; }

	public bool AllLayersVisible => StaticState?.AllLayersVisible ?? false;

	public bool CanUseLayerMask => StaticState?.CanUseLayerMask ?? false;

	public ulong LayerMask => StaticState?.LayerMask ?? 0;

	public Array<bool> LayerVisible => StaticState?.LayerVisible;

	public int LayerCount => StaticState?.LayerCount ?? 0;

	public bool HasMediaReplace => StaticState?.HasMediaReplace ?? false;

	public Array<Rect2> MediaReplaceRect => StaticState?.MediaReplaceRect;

	public Array<bool> MediaReplaceUse => StaticState?.MediaReplaceUse;

	public Array<int> MediaReplaceAtlasPages => StaticState?.MediaReplaceAtlasPages;

	public Vector2 MediaReplaceAtlasSize => StaticState?.MediaReplaceAtlasSize ?? default(Vector2);

	public int MediaCount => StaticState?.MediaCount ?? 0;

	public ulong MediaReplaceSignature => StaticState?.MediaReplaceSignature ?? 0;

	public bool Visible { get; private set; }

	public AdobeAnimateClipBlendState ClipBlend { get; private set; }

	public AdobeAnimateGpuClockState GpuClock { get; private set; }

	public AdobeAnimateGpuHitFlashState GpuBrightFlash { get; private set; }

	public AdobeAnimateGpuHitFlashState GpuWhiteFlash { get; private set; }

	public AdobeAnimateGpuGraphOwnerState(AdobeAnimateSprite sourceSprite, AdobeAnimateRuntimeDefinition definition, Transform2D globalTransform, Color modulate, Vector2 offset, VerticalClipState verticalClip, int frameIndex, float interpolationT, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int layerCount, bool hasMediaReplace, Array<Rect2> mediaReplaceRect, Array<bool> mediaReplaceUse, Array<int> mediaReplaceAtlasPages, Vector2 mediaReplaceAtlasSize, int mediaCount, ulong mediaReplaceSignature, bool visible, AdobeAnimateClipBlendState clipBlend, AdobeAnimateGpuClockState gpuClock = default(AdobeAnimateGpuClockState), AdobeAnimateGpuHitFlashState gpuBrightFlash = default(AdobeAnimateGpuHitFlashState), AdobeAnimateGpuHitFlashState gpuWhiteFlash = default(AdobeAnimateGpuHitFlashState))
		: this(new AdobeAnimateGpuGraphOwnerStateTemplate(sourceSprite, definition, offset, verticalClip, allLayersVisible, canUseLayerMask, layerMask, layerVisible, layerCount, hasMediaReplace, mediaReplaceRect, mediaReplaceUse, mediaReplaceAtlasPages, mediaReplaceAtlasSize, mediaCount, mediaReplaceSignature), globalTransform, modulate, frameIndex, interpolationT, visible, clipBlend, gpuClock, gpuBrightFlash, gpuWhiteFlash)
	{
	}

	internal AdobeAnimateGpuGraphOwnerState(AdobeAnimateGpuGraphOwnerStateTemplate staticState, Transform2D globalTransform, Color modulate, int frameIndex, float interpolationT, bool visible, AdobeAnimateClipBlendState clipBlend, AdobeAnimateGpuClockState gpuClock = default(AdobeAnimateGpuClockState), AdobeAnimateGpuHitFlashState gpuBrightFlash = default(AdobeAnimateGpuHitFlashState), AdobeAnimateGpuHitFlashState gpuWhiteFlash = default(AdobeAnimateGpuHitFlashState))
	{
		StaticState = staticState;
		GlobalTransform = globalTransform;
		Modulate = modulate;
		FrameIndex = Math.Max(0, frameIndex);
		InterpolationT = Mathf.Clamp(interpolationT, 0f, 1f);
		Visible = visible;
		ClipBlend = clipBlend;
		GpuClock = gpuClock;
		GpuBrightFlash = gpuBrightFlash;
		GpuWhiteFlash = gpuWhiteFlash;
	}

	public AdobeAnimateGpuGraphOwnerState WithGlobalTransform(Transform2D globalTransform)
	{
		GlobalTransform = globalTransform;
		return this;
	}

	public AdobeAnimateGpuGraphOwnerState WithModulate(Color modulate)
	{
		Modulate = modulate;
		return this;
	}

	public AdobeAnimateGpuGraphOwnerState WithPresentation(in Transform2D globalTransform, in Color modulate)
	{
		GlobalTransform = globalTransform;
		Modulate = modulate;
		return this;
	}

	internal AdobeAnimateGpuGraphOwnerState ResetDynamicState(AdobeAnimateGpuGraphOwnerStateTemplate staticState, in Transform2D globalTransform, in Color modulate, int frameIndex, float interpolationT, bool visible, in AdobeAnimateClipBlendState clipBlend, in AdobeAnimateGpuClockState gpuClock, in AdobeAnimateGpuHitFlashState gpuBrightFlash, in AdobeAnimateGpuHitFlashState gpuWhiteFlash)
	{
		StaticState = staticState;
		GlobalTransform = globalTransform;
		Modulate = modulate;
		FrameIndex = Math.Max(0, frameIndex);
		InterpolationT = Mathf.Clamp(interpolationT, 0f, 1f);
		Visible = visible;
		ClipBlend = clipBlend;
		GpuClock = gpuClock;
		GpuBrightFlash = gpuBrightFlash;
		GpuWhiteFlash = gpuWhiteFlash;
		return this;
	}
}
