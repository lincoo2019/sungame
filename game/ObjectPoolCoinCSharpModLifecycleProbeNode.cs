using Godot;

public partial class ObjectPoolCoinCSharpModLifecycleProbeNode : TowerDefenseCoinBase
{
	public int ModRefreshCalls { get; private set; }

	public int ModRecycleCalls { get; private set; }

	public Node ModRefreshParent { get; private set; }

	public Node ModRecycleParent { get; private set; }

	public new void Refresh()
	{
		ModRefreshCalls++;
		ModRefreshParent = GetParent();
	}

	public new void Recycle()
	{
		ModRecycleCalls++;
		ModRecycleParent = GetParent();
	}

	public override void _Ready()
	{
	}
}
