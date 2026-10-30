public sealed class FullGameplayRootLoadMetric
{
	public string Category { get; }

	public string Path { get; }

	public double QueueWaitMilliseconds { get; }

	public double ActualLoadMilliseconds { get; }

	public double PublishMilliseconds { get; }

	public int CacheBindingHitCount { get; }

	public int CacheBindingMissCount { get; }

	public FullGameplayRootLoadMetric(string category, string path, double actualLoadMilliseconds, double publishMilliseconds, int cacheBindingHitCount = 0, int cacheBindingMissCount = 0)
	{
		Category = category;
		Path = path;
		QueueWaitMilliseconds = 0.0;
		ActualLoadMilliseconds = actualLoadMilliseconds;
		PublishMilliseconds = publishMilliseconds;
		CacheBindingHitCount = cacheBindingHitCount;
		CacheBindingMissCount = cacheBindingMissCount;
	}
}
