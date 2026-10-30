using System;

internal readonly struct AdobeAnimateGpuManagedVisualBinding
{
	public int OwnerIndex { get; }

	public AdobeAnimateGpuManagedVisualSourceKind SourceKind { get; }

	public int VisualIndex { get; }

	public int ManagedSpriteIndex => VisualIndex;

	public int AttachmentPoseBase { get; }

	public AdobeAnimateGpuManagedVisualBinding(int ownerIndex, int managedSpriteIndex, int attachmentPoseBase)
		: this(ownerIndex, AdobeAnimateGpuManagedVisualSourceKind.SlotSprite2D, managedSpriteIndex, attachmentPoseBase)
	{
	}

	public AdobeAnimateGpuManagedVisualBinding(int ownerIndex, AdobeAnimateGpuManagedVisualSourceKind sourceKind, int visualIndex, int attachmentPoseBase)
	{
		OwnerIndex = Math.Max(0, ownerIndex);
		SourceKind = sourceKind;
		VisualIndex = Math.Max(0, visualIndex);
		AttachmentPoseBase = attachmentPoseBase;
	}
}
