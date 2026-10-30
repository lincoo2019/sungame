using Godot;

public partial class ObjectPoolTypedLifecycleProbeNode : Node, IObjectPoolLifecycle
{
	public static long RefreshCalls { get; private set; }

	public static long RecycleCalls { get; private set; }

	bool IObjectPoolLifecycle.SupportsDirectPoolLifecycleDispatch => true;

	public static void ResetRefreshCalls()
	{
		RefreshCalls = 0L;
	}

	public static void ResetRecycleCalls()
	{
		RecycleCalls = 0L;
	}

	public void Refresh()
	{
		RefreshCalls++;
	}

	public void Recycle()
	{
		RecycleCalls++;
	}

	void IObjectPoolLifecycle.RefreshFromPool()
	{
		Refresh();
	}

	void IObjectPoolLifecycle.RecycleToPool()
	{
		Recycle();
	}
}
