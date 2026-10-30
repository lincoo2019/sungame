public interface IBattleSnapshotNetworkContext
{
	bool IsNetworkPaused { get; }

	void SendBattleSnapshotMessage(string opCode, string data, string targetPeerId);

	void RequestBattleSnapshot(string phase);

	void RequestStateSnapshotAfterEntitiesReady();
}
