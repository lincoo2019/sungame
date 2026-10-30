using Godot;

public partial class ObjectPoolDynamicLifecycleProbeNode : Node
{
	public static long RefreshCalls { get; private set; }

	public static long RecycleCalls { get; private set; }

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
}
