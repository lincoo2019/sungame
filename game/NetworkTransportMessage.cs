public readonly struct NetworkTransportMessage(NetMessageEnvelope envelope, string senderPeerId, NetDeliveryMode deliveryMode)
{
	public NetMessageEnvelope Envelope { get; } = envelope;

	public string SenderPeerId { get; } = senderPeerId;

	public NetDeliveryMode DeliveryMode { get; } = deliveryMode;
}
