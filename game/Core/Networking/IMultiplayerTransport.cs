using System;
using Godot.Collections;

public interface IMultiplayerTransport
{
	bool IsHost { get; }

	string PeerId { get; }

	Godot.Collections.Array MatchMembers { get; }

	event Action<NetworkTransportMessage> MessageReceived;

	event Action<string> PeerConnected;

	event Action<string> PeerDisconnected;

	bool CreateHost(int port, int maxPlayers);

	bool Join(string address, int defaultPort);

	void Leave();

	void Send(NetMessageEnvelope envelope, NetDeliveryMode deliveryMode, string targetPeerId = "");
}
