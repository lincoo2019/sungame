public interface INetworkReplicator
{
	void Initialize(BattleNetworkSession session);

	void Process(double delta);

	void Dispose();
}
