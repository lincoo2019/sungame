public interface INetworkCommandHandler
{
	NetMessageType MessageType { get; }

	void Handle(NetMessageContext context, BattleNetworkSession session);
}
