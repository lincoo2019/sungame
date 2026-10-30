using System;
using System.Text.Json;

public static class MatchStateSerializer
{
	public static string Serialize<T>(T dto)
	{
		return JsonSerializer.Serialize(dto, typeof(T), MatchStateSerializerContext.Default);
	}

	public static T Deserialize<T>(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return default(T);
		}
		try
		{
			return (T)JsonSerializer.Deserialize(data, typeof(T), MatchStateSerializerContext.Default);
		}
		catch (JsonException)
		{
			return default(T);
		}
	}

	public static bool TryDeserialize<T>(string data, out T result)
	{
		result = default(T);
		if (string.IsNullOrEmpty(data))
		{
			return false;
		}
		try
		{
			result = (T)JsonSerializer.Deserialize(data, typeof(T), MatchStateSerializerContext.Default);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
