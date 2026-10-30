using System;

internal sealed class AdobeAnimateGpuRenderGraphDefinition
{
	public const int LayoutVersion = 7;

	public ulong Signature { get; }

	public AdobeAnimateGpuRenderOwner[] Owners { get; }

	public AdobeAnimateGpuRenderSlot[] RenderSlots { get; }

	public AdobeAnimateGpuFrameSlotEntry[] FrameSlotLookup { get; }

	public int[] AttachmentPoseLookup { get; }

	public AdobeAnimateGpuManagedAttachmentPose[] ManagedAttachmentPoses { get; }

	public AdobeAnimateGpuManagedVisualBinding[] ManagedVisualBindings { get; }

	public ulong ManagedTopologySignature { get; }

	public int MaxOwnerDepth { get; }

	public AdobeAnimateGpuRenderGraphDefinition(ulong signature, AdobeAnimateGpuRenderOwner[] owners, AdobeAnimateGpuRenderSlot[] renderSlots, AdobeAnimateGpuFrameSlotEntry[] frameSlotLookup, int[] attachmentPoseLookup, AdobeAnimateGpuManagedAttachmentPose[] managedAttachmentPoses, AdobeAnimateGpuManagedVisualBinding[] managedVisualBindings, ulong managedTopologySignature, int maxOwnerDepth)
	{
		Signature = signature;
		Owners = owners ?? Array.Empty<AdobeAnimateGpuRenderOwner>();
		RenderSlots = renderSlots ?? Array.Empty<AdobeAnimateGpuRenderSlot>();
		FrameSlotLookup = frameSlotLookup ?? Array.Empty<AdobeAnimateGpuFrameSlotEntry>();
		AttachmentPoseLookup = attachmentPoseLookup ?? Array.Empty<int>();
		ManagedAttachmentPoses = managedAttachmentPoses ?? Array.Empty<AdobeAnimateGpuManagedAttachmentPose>();
		ManagedVisualBindings = managedVisualBindings ?? Array.Empty<AdobeAnimateGpuManagedVisualBinding>();
		ManagedTopologySignature = managedTopologySignature;
		MaxOwnerDepth = Math.Max(0, maxOwnerDepth);
	}
}
