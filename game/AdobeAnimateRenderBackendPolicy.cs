public static class AdobeAnimateRenderBackendPolicy
{
	public static AdobeAnimateRenderBackend Normalize(int value)
	{
		if (value != 1)
		{
			return AdobeAnimateRenderBackend.GpuCrowd;
		}
		return AdobeAnimateRenderBackend.CpuPose;
	}

	public static string GetDisplayName(AdobeAnimateRenderBackend backend)
	{
		if (backend != AdobeAnimateRenderBackend.CpuPose)
		{
			return "GPU Crowd";
		}
		return "CPU Pose";
	}
}
