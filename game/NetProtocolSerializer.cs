using System.Text.Json;

public static class NetProtocolSerializer
{
	public static bool TrySerialize(NetMessageEnvelope envelope, out string data)
	{
		data = "";
		if (envelope == null || !NetProtocol.IsWithinUtf8Budget(envelope.payload ?? "", 3145728))
		{
			return false;
		}
		string text = JsonSerializer.Serialize(envelope, NetProtocolJsonContext.Default.NetMessageEnvelope);
		if (!NetProtocol.IsWithinUtf8Budget(text, 4194304))
		{
			return false;
		}
		data = text;
		return true;
	}

	public static bool TryDeserialize(string data, out NetMessageEnvelope envelope)
	{
		envelope = null;
		if (string.IsNullOrEmpty(data) || !NetProtocol.IsWithinUtf8Budget(data, 4194304))
		{
			return false;
		}
		try
		{
			envelope = JsonSerializer.Deserialize(data, NetProtocolJsonContext.Default.NetMessageEnvelope);
			return envelope != null && NetProtocol.IsWithinUtf8Budget(envelope.payload ?? "", 3145728);
		}
		catch (JsonException)
		{
			return false;
		}
	}
}
