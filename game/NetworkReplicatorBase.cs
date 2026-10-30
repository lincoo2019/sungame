public abstract class NetworkReplicatorBase : INetworkReplicator
{
	protected BattleNetworkSession Session { get; private set; }

	protected IBattleNetworkContext Context => Session?.Context;

	public virtual void Initialize(BattleNetworkSession session)
	{
		Session = session;
	}

	public virtual void Process(double delta)
	{
	}

	public virtual void Dispose()
	{
		Session = null;
	}
}
