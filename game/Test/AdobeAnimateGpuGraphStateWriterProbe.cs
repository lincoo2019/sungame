using System;
using System.Collections.Generic;
using Godot;

public partial class AdobeAnimateGpuGraphStateWriterProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_GPU_GRAPH_STATE_WRITER_RESULT";

	private int _checks;

	private readonly List<string> _failures = new List<string>();

	public override void _Ready()
	{
		try
		{
			RunChecks();
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		bool flag = _failures.Count == 0 && _checks == 21;
		GD.Print($"{"ADOBE_ANIMATE_GPU_GRAPH_STATE_WRITER_RESULT"} passed={flag} checks={_checks} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("ADOBE_ANIMATE_GPU_GRAPH_STATE_WRITER_RESULT failure=" + failure);
		}
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void RunChecks()
	{
		AdobeAnimateGpuRenderGraphAllocation allocation = new AdobeAnimateGpuRenderGraphAllocation(42uL, 1, 7, 9, 1, 1);
		List<AdobeAnimateGpuGraphOwnerState> list = new List<AdobeAnimateGpuGraphOwnerState>
		{
			new AdobeAnimateGpuGraphOwnerState(null, null, Transform2D.Identity, Colors.White, new Vector2(2f, 3f), VerticalClipState.Disabled, 4, 0.25f, allLayersVisible: false, canUseLayerMask: true, 18446744073709551615uL, null, 1, hasMediaReplace: false, null, null, null, Vector2.Zero, 0, 0uL, visible: true, default(AdobeAnimateClipBlendState))
		};
		AdobeAnimateGpuGraphStateLayout layout;
		bool condition = AdobeAnimateGpuGraphStateWriter.TryMeasure(in allocation, list, 0, 1, 1, out layout);
		Check(condition, "The graph fixture must be measurable.");
		int num = 23;
		Check(layout.StateTexelCount == num, $"Expected {num} state texels, got {layout.StateTexelCount}.");
		AdobeAnimateGpuRenderGraphDefinition graph = new AdobeAnimateGpuRenderGraphDefinition(42uL, new AdobeAnimateGpuRenderOwner[1], new AdobeAnimateGpuRenderSlot[1], Array.Empty<AdobeAnimateGpuFrameSlotEntry>(), Array.Empty<int>(), Array.Empty<AdobeAnimateGpuManagedAttachmentPose>(), Array.Empty<AdobeAnimateGpuManagedVisualBinding>(), 0uL, 0);
		List<AdobeAnimateGpuDynamicOverrideAllocation> overrideAllocations = new List<AdobeAnimateGpuDynamicOverrideAllocation> { default(AdobeAnimateGpuDynamicOverrideAllocation) };
		List<AdobeAnimateGpuManagedVisualState> managedVisualStates = new List<AdobeAnimateGpuManagedVisualState>
		{
			new AdobeAnimateGpuManagedVisualState(new Vector2(16f, 24f), new Rect2(0.1f, 0.2f, 0.3f, 0.4f), 2, new Vector2(5f, 6f), Transform2D.Identity, Colors.White, Vector2.Zero, flipH: false, flipV: false, useRotate: true, useScale: true, useSkew: true, visible: true, blink: false)
		};
		int num2 = 3;
		float[] array = new float[(num2 + layout.StateTexelCount) * 4];
		AdobeAnimateGpuGraphStateWriter.Write(array, num2, in allocation, graph, list, 0, 1, overrideAllocations, 0, managedVisualStates, 0, 1, useAbsoluteTransform: false, new AdobeAnimateRootMotionState(new Transform2D(0.5f, new Vector2(8f, 9f)), enabled: true), 1234f);
		Check(Mathf.IsEqualApprox(array[(num2 + 3) * 4 + 3], 1234f), "Transaction stamp must occupy root-motion flags w.");
		Check(layout.QuadCount == allocation.RenderSlotCount, "Quad count must equal graph render slot count.");
		Check(Mathf.IsEqualApprox(array[num2 * 4], allocation.BaseTexel), "Header must retain the graph base texel.");
		int num3 = (num2 + 4) * 4;
		Check(Mathf.IsZeroApprox(array[num3 + 28]) && Mathf.IsZeroApprox(array[num3 + 29]) && Mathf.IsZeroApprox(array[num3 + 30]) && Mathf.IsZeroApprox(array[num3 + 31]), "Disabled clip blending must serialize four zero values without frame math.");
		AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = list[0];
		list[0] = new AdobeAnimateGpuGraphOwnerState(adobeAnimateGpuGraphOwnerState.StaticState, adobeAnimateGpuGraphOwnerState.GlobalTransform, adobeAnimateGpuGraphOwnerState.Modulate, adobeAnimateGpuGraphOwnerState.FrameIndex, adobeAnimateGpuGraphOwnerState.InterpolationT, adobeAnimateGpuGraphOwnerState.Visible, new AdobeAnimateClipBlendState(enabled: true, 7.75f, 0.4f));
		AdobeAnimateGpuGraphStateWriter.Write(array, num2, in allocation, graph, list, 0, 1, overrideAllocations, 0, managedVisualStates, 0, 1, useAbsoluteTransform: false, default(AdobeAnimateRootMotionState), 1235f);
		Check(Mathf.IsEqualApprox(array[num3 + 28], 7f) && Mathf.IsEqualApprox(array[num3 + 29], 0.75f) && Mathf.IsEqualApprox(array[num3 + 30], 0.4f) && Mathf.IsEqualApprox(array[num3 + 31], 1f), "Enabled clip blending must retain floor, fraction, weight and enabled values.");
		float[] array2 = new float[layout.StateTexelCount * 4];
		AdobeAnimateGpuGraphStateWriter.Write(array2, 0, in allocation, graph, list, 0, 1, overrideAllocations, 0, managedVisualStates, 0, 1, useAbsoluteTransform: false, default(AdobeAnimateRootMotionState), 0f);
		int num4 = 19;
		float[] array3 = new float[(num4 + layout.StateTexelCount) * 4];
		bool condition2 = AdobeAnimateGpuGraphStateWriter.TryCopyRelocated(array2, layout.StateTexelCount, 1, array3, num4);
		Check(condition2, "A valid zero-based state block must relocate into the shared arena.");
		int num5 = num4 * 4;
		Check(Mathf.IsEqualApprox(array3[num5 + 5], array2[5] + (float)num4) && Mathf.IsEqualApprox(array3[num5 + 6], array2[6] + (float)num4), "Header metadata and managed-visual pointers must relocate.");
		int num6 = 16;
		int num7 = 56;
		Check(Mathf.IsEqualApprox(array3[num5 + num6 + 14], array2[num6 + 14] + (float)num4) && Mathf.IsEqualApprox(array3[num5 + num7], array2[num7] + (float)num4), "Owner and metadata layer-mask pointers must relocate together.");
		Check(Mathf.IsEqualApprox(array3[num5], allocation.BaseTexel), "Graph-atlas pointers must not be relocated with crowd-state pointers.");
		Check(Mathf.IsEqualApprox(array3[num5 + num7 + 2], -1f), "Missing dynamic-override pointers must remain disabled.");
		Check(Mathf.IsEqualApprox(array2[5], 14f), "Relocation must not mutate the retained zero-based source block.");
		bool condition3 = AdobeAnimateGpuGraphStateWriter.TryPatchRootMotion(array3, num4, new AdobeAnimateRootMotionState(new Transform2D(0.25f, new Vector2(12f, 13f)), enabled: true), 4321f);
		Check(condition3, "A relocated state block must accept a transaction-local root-motion patch.");
		Check(Mathf.IsEqualApprox(array3[num5 + 12], 12f) && Mathf.IsEqualApprox(array3[num5 + 13], 13f) && Mathf.IsEqualApprox(array3[num5 + 14], 1f) && Mathf.IsEqualApprox(array3[num5 + 15], 4321f), "Root-motion origin, enabled flag and transaction stamp must patch without rebuilding owner state.");
		AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState2 = new AdobeAnimateGpuGraphOwnerState(adobeAnimateGpuGraphOwnerState.StaticState, new Transform2D(0.5f, new Vector2(21f, 22f)), new Color(0.1f, 0.2f, 0.3f, 0.4f), 9, 0.75f, visible: false, new AdobeAnimateClipBlendState(enabled: true, 6.5f, 0.6f), new AdobeAnimateGpuClockState(enabled: true, 10f, 11f, 12f, 13f, 14f, loop: true));
		int num8 = num5 + 16;
		array3[num8 + 8] = adobeAnimateGpuGraphOwnerState2.Offset.X + 100f;
		array3[num8 + 9] = adobeAnimateGpuGraphOwnerState2.Offset.Y + 100f;
		bool condition4 = AdobeAnimateGpuGraphStateWriter.TryPatchRootOwnerDynamicState(array3, num4, adobeAnimateGpuGraphOwnerState2, useAbsoluteTransform: true);
		Check(condition4, "A relocated state block must accept root-owner dynamic patching.");
		bool condition5 = Mathf.IsEqualApprox(array3[num8 + 4], 21f) && Mathf.IsEqualApprox(array3[num8 + 5], 22f) && Mathf.IsEqualApprox(array3[num8 + 6], 9f) && Mathf.IsEqualApprox(array3[num8 + 7], 0.75f) && Mathf.IsEqualApprox(array3[num8 + 8], adobeAnimateGpuGraphOwnerState2.Offset.X) && Mathf.IsEqualApprox(array3[num8 + 9], adobeAnimateGpuGraphOwnerState2.Offset.Y) && Mathf.IsZeroApprox(array3[num8 + 13]) && Mathf.IsEqualApprox(array3[num8 + 19], 0.4f) && Mathf.IsEqualApprox(array3[num8 + 30], 0.6f) && Mathf.IsEqualApprox(array3[num8 + 31], 1f);
		Check(condition5, "Root transform, visual offset, frame, visibility, modulate and blend state must patch in place.");
		AdobeAnimateGpuDynamicOverrideAllocation allocation2 = new AdobeAnimateGpuDynamicOverrideAllocation(99uL, 314, 6);
		bool condition6 = AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicOverride(array3, num4, 1, 0, in allocation2);
		Check(condition6, "A relocated state block must accept dynamic-override pointer patching.");
		Check(Mathf.IsEqualApprox(array3[num5 + num7 + 2], allocation2.BaseTexel) && Mathf.IsEqualApprox(array3[num5 + num7 + 3], allocation2.MediaCount), "Dynamic-override atlas pointer and media count must patch in place.");
		bool condition7 = AdobeAnimateGpuGraphStateWriter.TryPatchManagedVisualState(array3, num4, 0, new AdobeAnimateGpuManagedVisualState(new Vector2(32f, 48f), new Rect2(0.25f, 0.5f, 0.125f, 0.375f), 7, new Vector2(9f, 10f), new Transform2D(0.75f, new Vector2(11f, 12f)), new Color(0.2f, 0.4f, 0.6f, 0.8f), new Vector2(13f, 14f), flipH: true, flipV: true, useRotate: false, useScale: false, useSkew: false, visible: false, blink: true));
		Check(condition7, "A relocated state block must accept managed-visual state patching.");
		int num9 = Mathf.RoundToInt(array3[num5 + 6]) * 4;
		Check(Mathf.IsEqualApprox(array3[num9], 32f) && Mathf.IsEqualApprox(array3[num9 + 2], 7f) && Mathf.IsZeroApprox(array3[num9 + 3]) && Mathf.IsEqualApprox(array3[num9 + 20], 0.2f) && Mathf.IsEqualApprox(array3[num9 + 23], 0.8f) && Mathf.IsEqualApprox(array3[num9 + 27], 1f), "Managed visual size, atlas layer, visibility, modulate and blink must patch in place.");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
