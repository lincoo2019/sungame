using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Godot;

internal static class AdobeAnimateGpuGraphStateWriter
{
	internal const int HeaderTexels = 4;

	internal const int OwnerStateTexels = 10;

	internal const int OwnerMetadataTexels = 1;

	internal const int ManagedVisualStateTexels = 7;

	private const int LayerMaskBitsPerTexel = 64;

	private const int LayerMaskBitsPerComponent = 16;

	public static bool TryMeasure(in AdobeAnimateGpuRenderGraphAllocation allocation, IReadOnlyList<AdobeAnimateGpuGraphOwnerState> ownerStates, int ownerStart, int ownerCount, int managedVisualCount, out AdobeAnimateGpuGraphStateLayout layout)
	{
		layout = default(AdobeAnimateGpuGraphStateLayout);
		if (allocation.Signature == 0L || allocation.RenderSlotCount <= 0 || ownerStates == null || ownerStart < 0 || ownerCount <= 0 || managedVisualCount < 0 || ownerStart > ownerStates.Count - ownerCount || ownerCount != allocation.OwnerCount)
		{
			return false;
		}
		ReadOnlySpan<AdobeAnimateGpuGraphOwnerState> readOnlySpan = ((ownerStates is List<AdobeAnimateGpuGraphOwnerState> list) ? CollectionsMarshal.AsSpan(list) : default(Span<AdobeAnimateGpuGraphOwnerState>));
		bool flag = !readOnlySpan.IsEmpty;
		long num = 0L;
		for (int i = 0; i < ownerCount; i++)
		{
			AdobeAnimateGpuGraphOwnerState ownerState = (flag ? readOnlySpan[ownerStart + i] : ownerStates[ownerStart + i]);
			num += GetLayerMaskTexelCount(ownerState);
		}
		long num2 = 4 + (long)ownerCount * 10L + ownerCount + num + (long)managedVisualCount * 7L;
		if (num2 <= 0 || num2 > 2147483647)
		{
			return false;
		}
		layout = new AdobeAnimateGpuGraphStateLayout((int)num2, allocation.RenderSlotCount);
		return true;
	}

	public static void Write(float[] buffer, int stateBaseTexel, in AdobeAnimateGpuRenderGraphAllocation allocation, AdobeAnimateGpuRenderGraphDefinition graph, IReadOnlyList<AdobeAnimateGpuGraphOwnerState> ownerStates, int ownerStart, int ownerCount, IReadOnlyList<AdobeAnimateGpuDynamicOverrideAllocation> overrideAllocations, int overrideStart, IReadOnlyList<AdobeAnimateGpuManagedVisualState> managedVisualStates, int managedVisualStart, int managedVisualCount, bool useAbsoluteTransform, in AdobeAnimateRootMotionState rootMotion, float transactionStamp)
	{
		ReadOnlySpan<AdobeAnimateGpuGraphOwnerState> readOnlySpan = ((ownerStates is List<AdobeAnimateGpuGraphOwnerState> list) ? CollectionsMarshal.AsSpan(list) : default(Span<AdobeAnimateGpuGraphOwnerState>));
		ReadOnlySpan<AdobeAnimateGpuDynamicOverrideAllocation> readOnlySpan2 = ((overrideAllocations is List<AdobeAnimateGpuDynamicOverrideAllocation> list2) ? CollectionsMarshal.AsSpan(list2) : default(Span<AdobeAnimateGpuDynamicOverrideAllocation>));
		ReadOnlySpan<AdobeAnimateGpuManagedVisualState> readOnlySpan3 = ((managedVisualStates is List<AdobeAnimateGpuManagedVisualState> list3) ? CollectionsMarshal.AsSpan(list3) : default(Span<AdobeAnimateGpuManagedVisualState>));
		bool flag = !readOnlySpan.IsEmpty;
		bool flag2 = !readOnlySpan2.IsEmpty;
		bool flag3 = managedVisualCount == 0 || !readOnlySpan3.IsEmpty;
		int num = stateBaseTexel * 4;
		buffer[num] = allocation.BaseTexel;
		buffer[num + 1] = allocation.Page;
		buffer[num + 2] = allocation.RenderSlotCount;
		buffer[num + 3] = 7f;
		buffer[num + 4] = allocation.OwnerCount;
		int num2 = stateBaseTexel + 4 + ownerCount * 10;
		buffer[num + 5] = num2;
		int num3 = num2 + ownerCount;
		int num4 = 0;
		for (int i = 0; i < ownerCount; i++)
		{
			AdobeAnimateGpuGraphOwnerState ownerState = (flag ? readOnlySpan[ownerStart + i] : ownerStates[ownerStart + i]);
			num4 += GetLayerMaskTexelCount(ownerState);
		}
		int num5 = num3 + num4;
		buffer[num + 6] = ((managedVisualCount > 0) ? ((float)num5) : (-1f));
		buffer[num + 7] = managedVisualCount;
		WriteRootMotionState(buffer, num + 8, useAbsoluteTransform ? default(AdobeAnimateRootMotionState) : rootMotion, transactionStamp);
		for (int j = 0; j < ownerCount; j++)
		{
			AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = (flag ? readOnlySpan[ownerStart + j] : ownerStates[ownerStart + j]);
			AdobeAnimateGpuDynamicOverrideAllocation adobeAnimateGpuDynamicOverrideAllocation = (flag2 ? readOnlySpan2[overrideStart + j] : overrideAllocations[overrideStart + j]);
			int layerMaskTexelCount = GetLayerMaskTexelCount(adobeAnimateGpuGraphOwnerState);
			int num6 = num + (4 + j * 10) * 4;
			WriteOwnerDynamicState(buffer, num6, adobeAnimateGpuGraphOwnerState, useAbsoluteTransform && j == 0);
			buffer[num6 + 14] = ((layerMaskTexelCount > 0) ? ((float)num3) : (-1f));
			buffer[num6 + 15] = ((layerMaskTexelCount > 0) ? ((float)adobeAnimateGpuGraphOwnerState.LayerCount) : 0f);
			int num7 = (num2 + j) * 4;
			buffer[num7] = ((layerMaskTexelCount > 0) ? ((float)num3) : (-1f));
			buffer[num7 + 1] = ((layerMaskTexelCount > 0) ? ((float)adobeAnimateGpuGraphOwnerState.LayerCount) : 0f);
			buffer[num7 + 2] = ((adobeAnimateGpuDynamicOverrideAllocation.Signature != 0L) ? ((float)adobeAnimateGpuDynamicOverrideAllocation.BaseTexel) : (-1f));
			buffer[num7 + 3] = ((adobeAnimateGpuDynamicOverrideAllocation.Signature != 0L) ? ((float)adobeAnimateGpuDynamicOverrideAllocation.MediaCount) : 0f);
			if (layerMaskTexelCount > 0)
			{
				WriteLayerMask(buffer, num3, adobeAnimateGpuGraphOwnerState, layerMaskTexelCount);
				num3 += layerMaskTexelCount;
			}
		}
		for (int k = 0; k < managedVisualCount; k++)
		{
			WriteManagedVisualState(buffer, num5 + k * 7, flag3 ? readOnlySpan3[managedVisualStart + k] : managedVisualStates[managedVisualStart + k]);
		}
	}

	public static bool TryRefreshGraphAllocation(float[] buffer, int stateBaseTexel, in AdobeAnimateGpuRenderGraphAllocation allocation, out bool changed)
	{
		changed = false;
		if (buffer == null || stateBaseTexel < 0 || allocation.Signature == 0L || allocation.Page < 0 || allocation.BaseTexel < 0 || allocation.RenderSlotCount <= 0 || allocation.OwnerCount <= 0)
		{
			return false;
		}
		long num = (long)stateBaseTexel * 4L;
		if (num < 0 || num > (long)buffer.Length - 8L)
		{
			return false;
		}
		int num2 = (int)num;
		if (buffer[num2 + 3] != 7f || buffer[num2 + 4] != (float)allocation.OwnerCount)
		{
			return false;
		}
		changed = buffer[num2] != (float)allocation.BaseTexel || buffer[num2 + 1] != (float)allocation.Page || buffer[num2 + 2] != (float)allocation.RenderSlotCount;
		if (!changed)
		{
			return true;
		}
		buffer[num2] = allocation.BaseTexel;
		buffer[num2 + 1] = allocation.Page;
		buffer[num2 + 2] = allocation.RenderSlotCount;
		return true;
	}

	public static bool TryCopyRelocated(float[] source, int stateTexelCount, int ownerCount, float[] destination, int stateBaseTexel)
	{
		if (source == null || destination == null || stateTexelCount <= 0 || ownerCount <= 0 || stateBaseTexel < 0 || stateBaseTexel > 536870911 || stateTexelCount > 536870911 || 4 + (long)ownerCount * 11L > stateTexelCount)
		{
			return false;
		}
		int num = stateTexelCount * 4;
		int num2 = stateBaseTexel * 4;
		if (source.Length < num || num2 < 0 || num2 > destination.Length - num)
		{
			return false;
		}
		source.AsSpan(0, num).CopyTo(destination.AsSpan(num2, num));
		RelocateArenaPointer(destination, num2 + 5, stateBaseTexel);
		RelocateArenaPointer(destination, num2 + 6, stateBaseTexel);
		int num3 = 4 + ownerCount * 10;
		for (int i = 0; i < ownerCount; i++)
		{
			int num4 = num2 + (4 + i * 10) * 4;
			int num5 = num2 + (num3 + i) * 4;
			if (num4 > destination.Length - 40 || num5 > destination.Length - 4)
			{
				return false;
			}
			RelocateArenaPointer(destination, num4 + 14, stateBaseTexel);
			RelocateArenaPointer(destination, num5, stateBaseTexel);
		}
		return true;
	}

	public static bool TryPatchRootMotion(float[] buffer, int stateBaseTexel, in AdobeAnimateRootMotionState rootMotion, float transactionStamp = 0f)
	{
		if (buffer == null || stateBaseTexel < 0 || stateBaseTexel > 536870911)
		{
			return false;
		}
		int num = stateBaseTexel * 4 + 8;
		if (num < 0 || num > buffer.Length - 8)
		{
			return false;
		}
		WriteRootMotionState(buffer, num, in rootMotion, transactionStamp);
		return true;
	}

	public static bool TryPatchRootOwnerDynamicState(float[] buffer, int stateBaseTexel, AdobeAnimateGpuGraphOwnerState rootOwnerState, bool useAbsoluteTransform)
	{
		return TryPatchOwnerDynamicState(buffer, stateBaseTexel, 0, rootOwnerState, useAbsoluteTransform);
	}

	public static bool TryPatchOwnerDynamicState(float[] buffer, int stateBaseTexel, int ownerIndex, AdobeAnimateGpuGraphOwnerState ownerState, bool useAbsoluteTransform)
	{
		if (buffer == null || ownerState == null || stateBaseTexel < 0 || ownerIndex < 0 || stateBaseTexel > 536870911)
		{
			return false;
		}
		long num = ((long)stateBaseTexel + 4L + (long)ownerIndex * 10L) * 4;
		if (num < 0 || num > buffer.Length - 40)
		{
			return false;
		}
		int ownerOffset = (int)num;
		WriteOwnerDynamicState(buffer, ownerOffset, ownerState, useAbsoluteTransform && ownerIndex == 0);
		return true;
	}

	public static bool TryPatchOwnerDynamicOverride(float[] buffer, int stateBaseTexel, int ownerCount, int ownerIndex, in AdobeAnimateGpuDynamicOverrideAllocation allocation)
	{
		if (buffer == null || stateBaseTexel < 0 || ownerCount <= 0 || (uint)ownerIndex >= (uint)ownerCount)
		{
			return false;
		}
		long num = ((long)stateBaseTexel + 4L + (long)ownerCount * 10L + ownerIndex) * 4;
		if (num < 0 || num > buffer.Length - 4)
		{
			return false;
		}
		int num2 = (int)num;
		buffer[num2 + 2] = ((allocation.Signature != 0L) ? ((float)allocation.BaseTexel) : (-1f));
		buffer[num2 + 3] = ((allocation.Signature != 0L) ? ((float)allocation.MediaCount) : 0f);
		return true;
	}

	public static bool TryPatchManagedVisualState(float[] buffer, int stateBaseTexel, int visualIndex, in AdobeAnimateGpuManagedVisualState visualState)
	{
		if (buffer == null || stateBaseTexel < 0 || visualIndex < 0)
		{
			return false;
		}
		long num = (long)stateBaseTexel * 4L;
		if (num < 0 || num > buffer.Length - 8)
		{
			return false;
		}
		int num2 = (int)num;
		int num3 = Mathf.RoundToInt(buffer[num2 + 6]);
		int num4 = Mathf.RoundToInt(buffer[num2 + 7]);
		if (num3 < 0 || (uint)visualIndex >= (uint)num4)
		{
			return false;
		}
		long num5 = (num3 + (long)visualIndex * 7L) * 4;
		if (num5 < 0 || num5 > buffer.Length - 28)
		{
			return false;
		}
		WriteManagedVisualState(buffer, (int)(num5 / 4), in visualState);
		return true;
	}

	private static void WriteOwnerDynamicState(float[] buffer, int ownerOffset, AdobeAnimateGpuGraphOwnerState ownerState, bool writeAbsoluteTransform)
	{
		Span<System.Numerics.Vector4> span = MemoryMarshal.Cast<float, System.Numerics.Vector4>(buffer.AsSpan(ownerOffset, 40));
		Transform2D transform2D = (writeAbsoluteTransform ? ownerState.GlobalTransform : Transform2D.Identity);
		span[0] = new System.Numerics.Vector4(transform2D.X.X, transform2D.X.Y, transform2D.Y.X, transform2D.Y.Y);
		span[1] = new System.Numerics.Vector4(transform2D.Origin.X, transform2D.Origin.Y, ownerState.FrameIndex, ownerState.InterpolationT);
		span[2] = new System.Numerics.Vector4(ownerState.Offset.X, ownerState.Offset.Y, ownerState.VerticalClip.UpY, ownerState.VerticalClip.DownY);
		span[3].X = (ownerState.VerticalClip.Enabled ? 1f : 0f);
		span[3].Y = (ownerState.Visible ? 1f : 0f);
		Color modulate = ownerState.Modulate;
		span[4] = new System.Numerics.Vector4(modulate.R, modulate.G, modulate.B, modulate.A);
		AdobeAnimateGpuClockState gpuClock = ownerState.GpuClock;
		span[5] = new System.Numerics.Vector4(gpuClock.StartTime, gpuClock.StartFrame, gpuClock.FramesPerSecond, gpuClock.Enabled ? 1f : 0f);
		span[6] = new System.Numerics.Vector4(gpuClock.ClipStart, gpuClock.ClipEndExclusive, gpuClock.Loop ? 1f : 0f, 0f);
		if (ownerState.ClipBlend.Enabled)
		{
			float fromFrameFloat = ownerState.ClipBlend.FromFrameFloat;
			float num = MathF.Floor(fromFrameFloat);
			span[7] = new System.Numerics.Vector4(num, Mathf.Clamp(fromFrameFloat - num, 0f, 1f), ownerState.ClipBlend.Weight, 1f);
		}
		else
		{
			span[7] = default(System.Numerics.Vector4);
		}
		span[8] = BuildHitFlashTexel(ownerState.GpuBrightFlash);
		span[9] = BuildHitFlashTexel(ownerState.GpuWhiteFlash);
	}

	private static void RelocateArenaPointer(float[] buffer, int index, int stateBaseTexel)
	{
		if (buffer[index] >= 0f)
		{
			buffer[index] += stateBaseTexel;
		}
	}

	private static System.Numerics.Vector4 BuildHitFlashTexel(in AdobeAnimateGpuHitFlashState state)
	{
		return new System.Numerics.Vector4(state.StartTime, state.Strength, state.Duration, state.Enabled ? 1f : 0f);
	}

	private static void WriteRootMotionState(float[] buffer, int offset, in AdobeAnimateRootMotionState state, float transactionStamp)
	{
		Transform2D transform2D = (state.Enabled ? state.PreviousRelativeTransform : Transform2D.Identity);
		buffer[offset] = transform2D.X.X;
		buffer[offset + 1] = transform2D.X.Y;
		buffer[offset + 2] = transform2D.Y.X;
		buffer[offset + 3] = transform2D.Y.Y;
		buffer[offset + 4] = transform2D.Origin.X;
		buffer[offset + 5] = transform2D.Origin.Y;
		buffer[offset + 6] = (state.Enabled ? 1f : 0f);
		buffer[offset + 7] = transactionStamp;
	}

	private static void WriteManagedVisualState(float[] buffer, int baseTexel, in AdobeAnimateGpuManagedVisualState state)
	{
		int num = baseTexel * 4;
		buffer[num] = Math.Max(0.0001f, state.SourceSize.X);
		buffer[num + 1] = Math.Max(0.0001f, state.SourceSize.Y);
		buffer[num + 2] = Math.Max(0, state.AtlasLayer);
		buffer[num + 3] = (state.Visible ? 1f : 0f);
		buffer[num + 4] = state.UvRect.Position.X;
		buffer[num + 5] = state.UvRect.Position.Y;
		buffer[num + 6] = state.UvRect.Size.X;
		buffer[num + 7] = state.UvRect.Size.Y;
		buffer[num + 8] = state.DrawOrigin.X;
		buffer[num + 9] = state.DrawOrigin.Y;
		buffer[num + 10] = (state.FlipH ? (-1f) : 1f);
		buffer[num + 11] = (state.FlipV ? (-1f) : 1f);
		buffer[num + 12] = state.LocalTransform.X.X;
		buffer[num + 13] = state.LocalTransform.X.Y;
		buffer[num + 14] = state.LocalTransform.Y.X;
		buffer[num + 15] = state.LocalTransform.Y.Y;
		buffer[num + 16] = state.LocalTransform.Origin.X;
		buffer[num + 17] = state.LocalTransform.Origin.Y;
		buffer[num + 18] = state.SlotOffset.X;
		buffer[num + 19] = state.SlotOffset.Y;
		buffer[num + 20] = state.Modulate.R;
		buffer[num + 21] = state.Modulate.G;
		buffer[num + 22] = state.Modulate.B;
		buffer[num + 23] = state.Modulate.A;
		buffer[num + 24] = (state.UseRotate ? 1f : 0f);
		buffer[num + 25] = (state.UseScale ? 1f : 0f);
		buffer[num + 26] = (state.UseSkew ? 1f : 0f);
		buffer[num + 27] = (state.Blink ? 1f : 0f);
	}

	private static int GetLayerMaskTexelCount(AdobeAnimateGpuGraphOwnerState ownerState)
	{
		if (ownerState.AllLayersVisible || ownerState.LayerCount <= 0)
		{
			return 0;
		}
		return (ownerState.LayerCount + 64 - 1) / 64;
	}

	private static void WriteLayerMask(float[] buffer, int ownerMaskBaseTexel, AdobeAnimateGpuGraphOwnerState ownerState, int maskTexelCount)
	{
		for (int i = 0; i < maskTexelCount; i++)
		{
			Color color = BuildLayerMaskTexel(ownerState, i);
			int num = (ownerMaskBaseTexel + i) * 4;
			buffer[num] = color.R;
			buffer[num + 1] = color.G;
			buffer[num + 2] = color.B;
			buffer[num + 3] = color.A;
		}
	}

	private static Color BuildLayerMaskTexel(AdobeAnimateGpuGraphOwnerState ownerState, int maskTexelIndex)
	{
		int num = maskTexelIndex * 64;
		return new Color(BuildLayerMaskComponent(ownerState, num), BuildLayerMaskComponent(ownerState, num + 16), BuildLayerMaskComponent(ownerState, num + 32), BuildLayerMaskComponent(ownerState, num + 48));
	}

	private static float BuildLayerMaskComponent(AdobeAnimateGpuGraphOwnerState ownerState, int layerBase)
	{
		if (layerBase >= ownerState.LayerCount)
		{
			return 0f;
		}
		if (ownerState.CanUseLayerMask && layerBase < 64)
		{
			return (int)(ushort)((ownerState.LayerMask >> layerBase) & 0xFFFF);
		}
		int num = 0;
		int num2 = Math.Min(ownerState.LayerCount, layerBase + 16);
		for (int i = layerBase; i < num2; i++)
		{
			if (ownerState.LayerVisible == null || i >= ownerState.LayerVisible.Count || ownerState.LayerVisible[i])
			{
				num |= 1 << i - layerBase;
			}
		}
		return num;
	}
}
