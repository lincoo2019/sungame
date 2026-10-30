public interface IObjectPoolLifecycle
{
	bool SupportsDirectPoolLifecycleDispatch { get; }

	void RefreshFromPool();

	void RecycleToPool();
}
