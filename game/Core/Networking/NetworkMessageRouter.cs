using System.Collections.Generic;

public sealed class NetworkMessageRouter
{
	private readonly Dictionary<NetMessageType, List<NetworkMessageHandler>> _handlers = new Dictionary<NetMessageType, List<NetworkMessageHandler>>();

	private NetworkMessageHandler _fallbackHandler;

	public void Register(NetMessageType messageType, NetworkMessageHandler handler)
	{
		if (handler != null)
		{
			if (!_handlers.TryGetValue(messageType, out var value))
			{
				value = new List<NetworkMessageHandler>();
				_handlers[messageType] = value;
			}
			if (!value.Contains(handler))
			{
				value.Add(handler);
			}
		}
	}

	public void Unregister(NetMessageType messageType, NetworkMessageHandler handler)
	{
		if (handler != null && _handlers.TryGetValue(messageType, out var value))
		{
			value.Remove(handler);
			if (value.Count == 0)
			{
				_handlers.Remove(messageType);
			}
		}
	}

	public void SetFallback(NetworkMessageHandler handler)
	{
		_fallbackHandler = handler;
	}

	public void Dispatch(NetMessageContext context)
	{
		if (context == null)
		{
			return;
		}
		if (_handlers.TryGetValue(context.MessageType, out var value))
		{
			NetworkMessageHandler[] array = value.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i](context);
			}
		}
		else
		{
			_fallbackHandler?.Invoke(context);
		}
	}
}
