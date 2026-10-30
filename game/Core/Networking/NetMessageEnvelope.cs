public sealed class NetMessageEnvelope
{
	public int protocol_version { get; set; } = 4;

	public NetMessageType message_type { get; set; }

	public string sender_peer_id { get; set; } = "";

	public long sequence { get; set; }

	public double game_tick { get; set; }

	public string payload_format { get; set; } = "json";

	public string payload { get; set; } = "";

	public bool forwarded { get; set; }
}
