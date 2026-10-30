public readonly struct AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode attachmentMode, AdobeAnimateExternalVisualDrawBand drawBand, AdobeAnimateSlot slot = null, int relativeZ = 0)
{
	public AdobeAnimateExternalVisualAttachmentMode AttachmentMode { get; } = attachmentMode;

	public AdobeAnimateExternalVisualDrawBand DrawBand { get; } = drawBand;

	public AdobeAnimateSlot Slot { get; } = slot;

	public int RelativeZ { get; } = relativeZ;
}
