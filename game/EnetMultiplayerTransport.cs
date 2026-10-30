using System;
using Godot.Collections;

public sealed class EnetMultiplayerTransport : IMultiplayerTransport, IDisposable
{
	private readonly MultiPlayerManager _manager;

	public bool IsHost
	{
		get
		{
			if (_manager != null)
			{
				return _manager.isHost;
			}
			return false;
		}
	}

	public string PeerId => _manager?.peerId ?? "";

	public Godot.Collections.Array MatchMembers => _manager?.matchMembers ?? new Godot.Collections.Array();

	public event Action<NetworkTransportMessage> MessageReceived;

	public event Action<string> PeerConnected;

	public event Action<string> PeerDisconnected;

	public EnetMultiplayerTransport(MultiPlayerManager manager)
	{
		_manager = manager;
		if (_manager != null)
		{
			_manager.OnNetworkMessageReceived += HandleNetworkMessageReceived;
			_manager.OnPeerLeft += HandlePeerLeft;
		}
	}

	public bool CreateHost(int port, int maxPlayers)
	{
		if (_manager != null)
		{
			return _manager.CreateMatch();
		}
		return false;
	}

	public bool Join(string address, int defaultPort)
	{
		if (_manager != null)
		{
			return _manager.JoinMatch(address);
		}
		return false;
	}

	public void Leave()
	{
		_manager?.LeaveMatch();
	}

	public void Send(NetMessageEnvelope envelope, NetDeliveryMode deliveryMode, string targetPeerId = "")
	{
		_manager?.SendEnvelope(envelope, deliveryMode, targetPeerId);
	}

	public void Dispose()
	{
		if (_manager != null)
		{
			_manager.OnNetworkMessageReceived -= HandleNetworkMessageReceived;
			_manager.OnPeerLeft -= HandlePeerLeft;
		}
	}

	private void HandleNetworkMessageReceived(NetMessageContext context)
	{
		if (context != null)
		{
			EmitMessageReceived(new NetworkTransportMessage(context.Envelope, context.SenderPeerId, context.DeliveryMode));
		}
	}

	private void HandlePeerLeft(string username, string peerId)
	{
		EmitPeerDisconnected(peerId);
	}

	internal void EmitMessageReceived(NetworkTransportMessage message)
	{
		this.MessageReceived?.Invoke(message);
	}

	internal void EmitPeerConnected(string peerId)
	{
		this.PeerConnected?.Invoke(peerId);
	}

	internal void EmitPeerDisconnected(string peerId)
	{
		this.PeerDisconnected?.Invoke(peerId);
	}
}
