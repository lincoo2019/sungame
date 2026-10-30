internal readonly struct AdobeAnimateGpuFrameSlotEntry(int poseTexel, int mediaId, int layerId, bool visible)
{
	public int PoseTexel { get; } = poseTexel;

	public int MediaId { get; } = mediaId;

	public int LayerId { get; } = layerId;

	public bool Visible { get; } = visible;
}
