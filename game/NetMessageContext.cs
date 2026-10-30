public sealed class NetMessageContext
{
	public NetMessageEnvelope Envelope { get; }

	public string FallbackSenderPeerId { get; }

	public NetDeliveryMode DeliveryMode { get; }

	public NetMessageType MessageType => Envelope?.message_type ?? NetMessageType.Unknown;

	public string AuthenticatedSenderPeerId => FallbackSenderPeerId ?? "";

	public string OriginSenderPeerId
	{
		get
		{
			if (string.IsNullOrEmpty(Envelope?.sender_peer_id))
			{
				return AuthenticatedSenderPeerId;
			}
			return Envelope.sender_peer_id;
		}
	}

	public string SenderPeerId => AuthenticatedSenderPeerId;

	public string Payload => Envelope?.payload ?? "";

	public string LegacyOpCode => NetProtocol.ToLegacyOpCode(MessageType);

	public long Sequence => Envelope?.sequence ?? 0;

	public double GameTick => Envelope?.game_tick ?? 0.0;

	public NetMessageContext(NetMessageEnvelope envelope, string fallbackSenderPeerId, NetDeliveryMode deliveryMode)
	{
		Envelope = envelope;
		FallbackSenderPeerId = fallbackSenderPeerId;
		DeliveryMode = deliveryMode;
	}
}
