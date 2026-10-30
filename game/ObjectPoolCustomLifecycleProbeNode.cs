using Godot;

public partial class ObjectPoolCustomLifecycleProbeNode : Node, IObjectPoolLifecycle
{
	public int TypedRefreshCalls { get; private set; }

	public int TypedRecycleCalls { get; private set; }

	public int CustomRefreshCalls { get; private set; }

	public int CustomRecycleCalls { get; private set; }

	public Node RefreshParent { get; private set; }

	public Node RecycleParent { get; private set; }

	bool IObjectPoolLifecycle.SupportsDirectPoolLifecycleDispatch => true;

	public void ModRefresh()
	{
		CustomRefreshCalls++;
		RefreshParent = GetParent();
	}

	public void ModRecycle()
	{
		CustomRecycleCalls++;
		RecycleParent = GetParent();
	}

	void IObjectPoolLifecycle.RefreshFromPool()
	{
		TypedRefreshCalls++;
	}

	void IObjectPoolLifecycle.RecycleToPool()
	{
		TypedRecycleCalls++;
	}
}
