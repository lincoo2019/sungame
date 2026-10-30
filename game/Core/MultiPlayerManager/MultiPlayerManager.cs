using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class MultiPlayerManager : Node
{
	public delegate void AllClientsReadyEventHandler();

	public delegate void AllLevelConfigAckedEventHandler();

	public delegate void AllGameEntryAckedEventHandler();

	public delegate void MatchCreatedEventHandler(string matchId);

	public delegate void MatchJoinedEventHandler(string matchId);

	public delegate void MatchLeftEventHandler();

	public delegate void PeerJoinedEventHandler(string username);

	public delegate void PeerLeftEventHandler(string username, string peerId);

	public delegate void MatchStateReceivedEventHandler(string opCode, string data, string senderId);

	public delegate void ConnectionChangedEventHandler(bool connected);

	public delegate void PingUpdatedEventHandler(string peerId, int latencyMs);

	public const int DEFAULT_PORT = 7777;

	public const int MAX_PLAYERS = 4;

	private ENetMultiplayerPeer _peer;

	private string _playerName = "";

	private string _receivedLevelConfigJson = "";

	private string _receivedLevelConfigType = "";

	private string _receivedLevelEnterMode = "";

	private bool _receivedLevelIsBattle;

	private string _activeBattleLevelConfigData = "";

	private string _activeBattleStartData = "";

	private bool _gameEntrySent;

	private double _pingTimer;

	private readonly NetworkMessageRouter _networkMessageRouter = new NetworkMessageRouter();

	private long _reliableSequence;

	private long _unreliableSequence;

	private long _fallingObjectSpawnSequence;

	private long _damagePartSpawnSequence;

	private readonly System.Collections.Generic.Dictionary<string, long> _lastUnreliableSequenceByStream = new System.Collections.Generic.Dictionary<string, long>();

	private long _sessionGeneration;

	private bool _startGameTransitionPending;

	private bool _startGameTransitionCompleted;

	public const double PING_INTERVAL = 2.0;

	private int _currentPort = 7777;

	private XWModLevelIdentity _acceptedLevelIdentity;

	private readonly System.Collections.Generic.Dictionary<string, ModCompatibilityBatch> _modBatches = new System.Collections.Generic.Dictionary<string, ModCompatibilityBatch>(StringComparer.Ordinal);

	private readonly HashSet<string> _battleAdmitted = new HashSet<string>(StringComparer.Ordinal);

	private ModCompatibilityBatch _hostModBatch;

	private XWModEnvironmentService.EnvironmentLease _modEnvironmentLease;

	private bool _preparingModStart;

	private bool _modBattleActive;

	private string _modBattleId = "";

	private string _acceptedModBatch = "";

	private string _pendingRemoteModBatch = "";

	private long _remoteModTicket;

	private long _remoteModDeadline;

	private long _battleSceneDeadline;

	private TaskCompletionSource<bool> _modSceneAccepted;

	public static MultiPlayerManager Instance { get; private set; }

	public string currentMatchId { get; set; } = "";

	public bool isHost { get; set; }

	public static bool IsHost
	{
		get
		{
			if (Instance != null)
			{
				return Instance.isHost;
			}
			return false;
		}
	}

	public string peerId { get; set; } = "";

	public Godot.Collections.Array matchMembers { get; set; } = new Godot.Collections.Array();

	public string selectedLevelId { get; set; } = "";

	private System.Collections.Generic.Dictionary<int, string> _peerNames { get; set; } = new System.Collections.Generic.Dictionary<int, string>();

	private Godot.Collections.Array _clientsReady { get; set; } = new Godot.Collections.Array();

	public Godot.Collections.Array ClientsReady => _clientsReady;

	private List<string> _levelConfigAcked { get; set; } = new List<string>();

	private Godot.Collections.Array _gameEntryAcked { get; set; } = new Godot.Collections.Array();

	public Godot.Collections.Array GameEntryAcked => _gameEntryAcked;

	public bool GameEntrySent => _gameEntrySent;

	private System.Collections.Generic.Dictionary<string, double> _pingSendTimes { get; set; } = new System.Collections.Generic.Dictionary<string, double>();

	private System.Collections.Generic.Dictionary<string, int> _peerLatencies { get; set; } = new System.Collections.Generic.Dictionary<string, int>();

	public int CurrentPort => _currentPort;

	public string LastModCompatibilityFailure { get; private set; } = "";

	public Godot.Collections.Array BattleAdmittedPeers => new Godot.Collections.Array(((IEnumerable<string>)_battleAdmitted.OrderBy<string, string>((string id) => id, StringComparer.Ordinal)).Select((Func<string, Variant>)((string id) => id)));

	public event AllClientsReadyEventHandler OnAllClientsReady;

	public event AllLevelConfigAckedEventHandler OnAllLevelConfigAcked;

	public event AllGameEntryAckedEventHandler OnAllGameEntryAcked;

	public event MatchCreatedEventHandler OnMatchCreated;

	public event MatchJoinedEventHandler OnMatchJoined;

	public event MatchLeftEventHandler OnMatchLeft;

	public event PeerJoinedEventHandler OnPeerJoined;

	public event PeerLeftEventHandler OnPeerLeft;

	public event MatchStateReceivedEventHandler OnMatchStateReceived;

	public event Action<NetMessageContext> OnNetworkMessageReceived;

	public event ConnectionChangedEventHandler OnConnectionChanged;

	public event PingUpdatedEventHandler OnPingUpdated;

	public event Action<string> OnModCompatibilityFailure;

	public void EmitAllClientsReady()
	{
		this.OnAllClientsReady?.Invoke();
	}

	public void EmitAllGameEntryAcked()
	{
		this.OnAllGameEntryAcked?.Invoke();
	}

	public override void _Process(double delta)
	{
		ProcessModCompatibility();
		if (!(currentMatchId == "") && _peer != null)
		{
			_pingTimer += delta;
			if (_pingTimer >= 2.0)
			{
				_pingTimer = 0.0;
				_SendPing();
			}
		}
	}

	public int GetPeerLatency(string peerIdStr)
	{
		return _peerLatencies.GetValueOrDefault(peerIdStr, 0);
	}

	public int GetSignalLevel(string peerIdStr)
	{
		int valueOrDefault = _peerLatencies.GetValueOrDefault(peerIdStr, 999);
		if (valueOrDefault <= 50)
		{
			return 5;
		}
		if (valueOrDefault <= 100)
		{
			return 4;
		}
		if (valueOrDefault <= 200)
		{
			return 3;
		}
		if (valueOrDefault <= 400)
		{
			return 2;
		}
		if (valueOrDefault <= 800)
		{
			return 1;
		}
		return 0;
	}

	private void _SendPing()
	{
		double value = Time.GetTicksMsec();
		string text = peerId + "_" + value;
		_pingSendTimes[text] = value;
		double num = Time.GetTicksMsec();
		List<string> list = new List<string>();
		foreach (string key in _pingSendTimes.Keys)
		{
			if (num - _pingSendTimes[key] > 10000.0)
			{
				list.Add(key);
			}
		}
		foreach (string item in list)
		{
			_pingSendTimes.Remove(item);
		}
		if (isHost)
		{
			_peerLatencies[peerId] = 0;
			this.OnPingUpdated?.Invoke(peerId, 0);
			{
				foreach (Variant matchMember in matchMembers)
				{
					string s = (string)matchMember;
					if (int.Parse(s) != 1)
					{
						RpcId(int.Parse(s), "_RpcPing", text);
					}
				}
				return;
			}
		}
		RpcId(1L, "_RpcPing", text);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void _RpcPing(string pingId)
	{
		string s = base.Multiplayer.GetRemoteSenderId().ToString();
		RpcId(int.Parse(s), "_RpcPong", pingId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void _RpcPong(string pingId)
	{
		string key = base.Multiplayer.GetRemoteSenderId().ToString();
		if (!_pingSendTimes.ContainsKey(pingId))
		{
			return;
		}
		double num = _pingSendTimes[pingId];
		int num2 = (int)((double)Time.GetTicksMsec() - num);
		if (!isHost)
		{
			_pingSendTimes.Remove(pingId);
		}
		_peerLatencies[key] = num2;
		this.OnPingUpdated?.Invoke(key, num2);
		if (!isHost)
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		dictionary[peerId] = 0;
		foreach (Variant matchMember in matchMembers)
		{
			string text = (string)matchMember;
			dictionary[text] = _peerLatencies.GetValueOrDefault(text, 0);
		}
		Rpc("_RpcPingResult", Json.Stringify(dictionary));
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	public void _RpcPingResult(string data)
	{
		Variant variant = Json.ParseString(data);
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = (Dictionary)variant;
		foreach (Variant key in dictionary.Keys)
		{
			string text = (string)key;
			_peerLatencies[text] = (int)(long)dictionary[text];
			this.OnPingUpdated?.Invoke(text, (int)(long)dictionary[text]);
		}
	}

	public override void _Ready()
	{
		Instance = this;
		_networkMessageRouter.SetFallback(_HandleNetworkMessageFallback);
		base.Multiplayer.PeerConnected += _OnPeerConnected;
		base.Multiplayer.PeerDisconnected += _OnPeerDisconnected;
		base.Multiplayer.ConnectedToServer += _OnConnectedToServer;
		base.Multiplayer.ConnectionFailed += _OnConnectionFailed;
		base.Multiplayer.ServerDisconnected += _OnServerDisconnected;
	}

	public bool LogOut()
	{
		if (currentMatchId != "" || _peer != null)
		{
			LeaveMatch();
		}
		_playerName = "";
		peerId = "";
		matchMembers.Clear();
		_peerNames.Clear();
		_pingSendTimes.Clear();
		_peerLatencies.Clear();
		_lastUnreliableSequenceByStream.Clear();
		_ResetSessionState();
		return true;
	}

	public bool IsConnect()
	{
		if (_playerName != "")
		{
			return CanSendMatchRpc();
		}
		return false;
	}

	private bool CanSendMatchRpc()
	{
		if (currentMatchId == "")
		{
			return false;
		}
		MultiplayerPeer multiplayerPeer = base.Multiplayer?.MultiplayerPeer;
		if (GodotObject.IsInstanceValid(multiplayerPeer) && !(multiplayerPeer is OfflineMultiplayerPeer))
		{
			return multiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
		}
		return false;
	}

	public string GetUserName()
	{
		return _playerName;
	}

	public string GetUserDisplayName()
	{
		return _playerName;
	}

	public string GetPeerName(string peerIdStr)
	{
		if (!int.TryParse(peerIdStr, out var result))
		{
			return "P?";
		}
		if (_peerNames.ContainsKey(result))
		{
			return _peerNames[result];
		}
		return "P" + peerIdStr;
	}

	public bool CreateMatch()
	{
		if (currentMatchId != "" || _peer != null)
		{
			return false;
		}
		_currentPort = 7777;
		_peer = new ENetMultiplayerPeer();
		Error error = _peer.CreateServer(_currentPort, 4);
		if (error != Error.Ok)
		{
			_currentPort = 7778;
			_peer = new ENetMultiplayerPeer();
			error = _peer.CreateServer(_currentPort, 4);
		}
		if (error != Error.Ok)
		{
			BroadCastManager.Instance.BroadCastFloatCreate("CREATE_MATCH_FAILED", Colors.Red);
			_peer = null;
			return false;
		}
		base.Multiplayer.MultiplayerPeer = _peer;
		isHost = true;
		currentMatchId = "host";
		peerId = base.Multiplayer.GetUniqueId().ToString();
		_playerName = ((GameSaveManager.Instance.GetUserCurrent() != "") ? GameSaveManager.Instance.GetUserCurrent() : "Host");
		Global.Instance.isMultiplayerMode = true;
		Global.Instance.isMultiplayerHost = true;
		matchMembers.Clear();
		matchMembers.Add(peerId);
		_peerNames.Clear();
		_sessionGeneration++;
		_startGameTransitionPending = false;
		_startGameTransitionCompleted = false;
		_ResetSessionState();
		this.OnMatchCreated?.Invoke(currentMatchId);
		BroadCastManager.Instance.BroadCastFloatCreate("ROOM_CREATED", Colors.Green);
		return true;
	}

	public bool JoinMatch(string address)
	{
		if (currentMatchId != "" || _peer != null || !TryParseMatchEndpoint(address, out var host, out var port))
		{
			BroadCastManager.Instance.BroadCastFloatCreate("JOIN_MATCH_FAILED", Colors.Red);
			return false;
		}
		_peer = new ENetMultiplayerPeer();
		if (_peer.CreateClient(host, port) != Error.Ok)
		{
			BroadCastManager.Instance.BroadCastFloatCreate("JOIN_MATCH_FAILED", Colors.Red);
			_peer = null;
			return false;
		}
		base.Multiplayer.MultiplayerPeer = _peer;
		isHost = false;
		currentMatchId = address;
		peerId = base.Multiplayer.GetUniqueId().ToString();
		_playerName = ((GameSaveManager.Instance.GetUserCurrent() != "") ? GameSaveManager.Instance.GetUserCurrent() : "Client");
		Global.Instance.isMultiplayerMode = true;
		Global.Instance.isMultiplayerHost = false;
		matchMembers.Clear();
		matchMembers.Add(peerId);
		_peerNames.Clear();
		_sessionGeneration++;
		_startGameTransitionPending = false;
		_startGameTransitionCompleted = false;
		_ResetSessionState();
		return true;
	}

	internal static bool TryParseMatchEndpoint(string address, out string host, out int port)
	{
		host = "";
		port = 7777;
		if (string.IsNullOrWhiteSpace(address) || address.Length > 512)
		{
			return false;
		}
		string text = address.Trim();
		if (text.StartsWith("[", StringComparison.Ordinal))
		{
			int num = text.IndexOf(']');
			if (num <= 1)
			{
				return false;
			}
			host = text.Substring(1, num - 1);
			string text2 = text;
			int num2 = num + 1;
			string text3 = text2.Substring(num2, text2.Length - num2);
			if (text3.Length == 0)
			{
				return true;
			}
			if (text3.StartsWith(":", StringComparison.Ordinal))
			{
				text2 = text3;
				return TryParsePort(text2.Substring(1, text2.Length - 1), out port);
			}
			return false;
		}
		int num3 = text.IndexOf(':');
		int num4 = text.LastIndexOf(':');
		if (num3 < 0)
		{
			host = text;
			return host.Length > 0;
		}
		if (num3 != num4)
		{
			host = text;
			return true;
		}
		host = text.Substring(0, num4);
		if (host.Length > 0)
		{
			string text2 = text;
			int num2 = num4 + 1;
			return TryParsePort(text2.Substring(num2, text2.Length - num2), out port);
		}
		return false;
	}

	private static bool TryParsePort(string value, out int port)
	{
		if (int.TryParse(value, out port))
		{
			int num = port;
			if (num >= 1)
			{
				return num <= 65535;
			}
			return false;
		}
		return false;
	}

	public void LeaveMatch()
	{
		_CleanupConnection();
	}

	public void SendMatchState(string opCode, string data)
	{
		if (!CanSendMatchRpc())
		{
			return;
		}
		string text = _CreateEnvelopeData(opCode, data, NetDeliveryMode.Reliable, peerId);
		if (!(text == ""))
		{
			if (isHost)
			{
				Rpc("_RpcReceiveMatchState", "__net_v2", text);
				_ProcessMatchState(opCode, data, peerId);
			}
			else
			{
				RpcId(1L, "_RpcReceiveMatchState", "__net_v2", text);
			}
		}
	}

	public void SendMatchStateUnreliable(string opCode, string data)
	{
		if (!CanSendMatchRpc())
		{
			return;
		}
		string text = _CreateEnvelopeData(opCode, data, NetDeliveryMode.Unreliable, peerId);
		if (!(text == ""))
		{
			if (isHost)
			{
				Rpc("_RpcReceiveMatchStateUnreliable", "__net_v2", text);
				_ProcessMatchState(opCode, data, peerId);
			}
			else
			{
				RpcId(1L, "_RpcReceiveMatchStateUnreliable", "__net_v2", text);
			}
		}
	}

	public void SendMatchStateToPeer(string opCode, string data, string targetPeerId)
	{
		if (isHost && !string.IsNullOrWhiteSpace(targetPeerId) && !(targetPeerId == peerId))
		{
			NetMessageType netMessageType = NetProtocol.FromLegacyOpCode(opCode);
			if (netMessageType == NetMessageType.Unknown)
			{
				GD.PushWarning($"Cannot send unknown network opcode '{opCode}' to peer {targetPeerId}.");
			}
			else
			{
				SendEnvelope(new NetMessageEnvelope
				{
					message_type = netMessageType,
					payload = (data ?? ""),
					sender_peer_id = peerId
				}, NetDeliveryMode.Reliable, targetPeerId);
			}
		}
	}

	public void SendEnvelope(NetMessageEnvelope envelope, NetDeliveryMode deliveryMode, string targetPeerId = "")
	{
		if (!CanSendMatchRpc() || envelope == null)
		{
			return;
		}
		_PrepareOutgoingEnvelope(envelope, deliveryMode);
		if (!NetProtocolSerializer.TrySerialize(envelope, out var data))
		{
			return;
		}
		string text = ((deliveryMode == NetDeliveryMode.Unreliable) ? "_RpcReceiveMatchStateUnreliable" : "_RpcReceiveMatchState");
		int result;
		bool flag = int.TryParse(targetPeerId, out result);
		if (isHost)
		{
			if (flag && targetPeerId != peerId)
			{
				RpcId(result, text, "__net_v2", data);
			}
			else if (!flag)
			{
				Rpc(text, "__net_v2", data);
			}
			if (!flag || targetPeerId == peerId)
			{
				_DispatchLocalEnvelope(envelope, deliveryMode);
			}
		}
		else
		{
			RpcId(1L, text, "__net_v2", data);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void _RpcReceiveMatchState(string opCode, string data)
	{
		string text = base.Multiplayer.GetRemoteSenderId().ToString();
		if (!_TryDispatchEnvelope(opCode, data, text, NetDeliveryMode.Reliable))
		{
			_RejectLegacyProtocol(text);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void _RpcReceiveMatchStateUnreliable(string opCode, string data)
	{
		string text = base.Multiplayer.GetRemoteSenderId().ToString();
		if (!_TryHandleUnreliableEnvelope(opCode, data, text))
		{
			_RejectLegacyProtocol(text);
		}
	}

	private string _CreateEnvelopeData(string opCode, string data, NetDeliveryMode deliveryMode, string senderPeerId, bool forwarded = false)
	{
		if (!NetProtocolSerializer.TrySerialize(new NetMessageEnvelope
		{
			protocol_version = 4,
			message_type = NetProtocol.FromLegacyOpCode(opCode),
			sender_peer_id = senderPeerId,
			sequence = ((deliveryMode == NetDeliveryMode.Unreliable) ? (++_unreliableSequence) : (++_reliableSequence)),
			game_tick = ((TowerDefenseManager.Instance != null) ? TowerDefenseManager.Instance.runGameTime : ((double)Time.GetTicksMsec() / 1000.0)),
			payload_format = "json",
			payload = (data ?? ""),
			forwarded = forwarded
		}, out var data2))
		{
			return "";
		}
		return data2;
	}

	private void _PrepareOutgoingEnvelope(NetMessageEnvelope envelope, NetDeliveryMode deliveryMode)
	{
		envelope.protocol_version = 4;
		if (string.IsNullOrEmpty(envelope.sender_peer_id))
		{
			envelope.sender_peer_id = peerId;
		}
		if (envelope.sequence <= 0)
		{
			envelope.sequence = ((deliveryMode == NetDeliveryMode.Unreliable) ? (++_unreliableSequence) : (++_reliableSequence));
		}
		else if (deliveryMode == NetDeliveryMode.Unreliable)
		{
			_unreliableSequence = Math.Max(_unreliableSequence, envelope.sequence);
		}
		else
		{
			_reliableSequence = Math.Max(_reliableSequence, envelope.sequence);
		}
		if (envelope.game_tick <= 0.0)
		{
			envelope.game_tick = ((TowerDefenseManager.Instance != null) ? TowerDefenseManager.Instance.runGameTime : ((double)Time.GetTicksMsec() / 1000.0));
		}
		if (string.IsNullOrEmpty(envelope.payload_format))
		{
			envelope.payload_format = "json";
		}
	}

	private void _DispatchLocalEnvelope(NetMessageEnvelope envelope, NetDeliveryMode deliveryMode)
	{
		NetMessageContext netMessageContext = new NetMessageContext(envelope, peerId, deliveryMode);
		this.OnNetworkMessageReceived?.Invoke(netMessageContext);
		_networkMessageRouter.Dispatch(netMessageContext);
	}

	private bool _TryDispatchEnvelope(string opCode, string data, string rpcSenderId, NetDeliveryMode deliveryMode)
	{
		if (opCode != "__net_v2")
		{
			return false;
		}
		if (!NetProtocolSerializer.TryDeserialize(data, out var envelope))
		{
			return true;
		}
		if (envelope.protocol_version != 4)
		{
			_RejectProtocolVersion(rpcSenderId, envelope.protocol_version);
			return true;
		}
		if (!IsEnvelopeSenderAuthorized(envelope.message_type, rpcSenderId) || !AllowsModMessage(envelope.message_type, rpcSenderId))
		{
			return true;
		}
		NetMessageContext netMessageContext = new NetMessageContext(envelope, rpcSenderId, deliveryMode);
		this.OnNetworkMessageReceived?.Invoke(netMessageContext);
		_networkMessageRouter.Dispatch(netMessageContext);
		return true;
	}

	private bool _TryHandleUnreliableEnvelope(string opCode, string data, string rpcSenderId)
	{
		if (opCode != "__net_v2")
		{
			return false;
		}
		if (!NetProtocolSerializer.TryDeserialize(data, out var envelope))
		{
			return true;
		}
		if (envelope.protocol_version != 4)
		{
			_RejectProtocolVersion(rpcSenderId, envelope.protocol_version);
			return true;
		}
		if (!IsEnvelopeSenderAuthorized(envelope.message_type, rpcSenderId) || !AllowsModMessage(envelope.message_type, rpcSenderId))
		{
			return true;
		}
		if (isHost && envelope.message_type == NetMessageType.CursorState)
		{
			envelope.payload = NormalizeAuthenticatedUserId(envelope.payload, rpcSenderId);
		}
		NetMessageContext netMessageContext = new NetMessageContext(envelope, rpcSenderId, NetDeliveryMode.Unreliable);
		string key = netMessageContext.AuthenticatedSenderPeerId + ":" + envelope.message_type;
		if (_lastUnreliableSequenceByStream.TryGetValue(key, out var value) && envelope.sequence <= value)
		{
			return true;
		}
		_lastUnreliableSequenceByStream[key] = envelope.sequence;
		this.OnNetworkMessageReceived?.Invoke(netMessageContext);
		string text = NetProtocol.ToLegacyOpCode(envelope.message_type);
		if (text == "")
		{
			return true;
		}
		if (isHost)
		{
			envelope.forwarded = true;
			if (!NetProtocolSerializer.TrySerialize(envelope, out var data2))
			{
				return true;
			}
			Rpc("_RpcReceiveMatchStateUnreliable", "__net_v2", data2);
		}
		this.OnMatchStateReceived?.Invoke(text, envelope.payload, netMessageContext.AuthenticatedSenderPeerId);
		return true;
	}

	private static bool IsEnvelopeSenderAuthorized(NetMessageType messageType, string rpcSenderId)
	{
		if (!RequiresHostAuthority(messageType))
		{
			return true;
		}
		return rpcSenderId == "1";
	}

	private static bool RequiresHostAuthority(NetMessageType messageType)
	{
		switch (messageType)
		{
		case NetMessageType.StartGame:
		case NetMessageType.CommandRejected:
		case NetMessageType.GameStateSync:
		case NetMessageType.GameResult:
		case NetMessageType.SpawnZombie:
		case NetMessageType.SpawnGrid:
		case NetMessageType.CharacterDestroy:
		case NetMessageType.CharacterInit:
		case NetMessageType.CharacterStateDelta:
		case NetMessageType.CharacterPositionSync:
		case NetMessageType.Pause:
		case NetMessageType.Resume:
		case NetMessageType.SelectLevel:
		case NetMessageType.LevelConfig:
		case NetMessageType.ChooseOver:
		case NetMessageType.VaseBreak:
		case NetMessageType.PacketSpawn:
		case NetMessageType.SpawnCoin:
		case NetMessageType.SpawnFallingObject:
		case NetMessageType.ZombieStateDelta:
		case NetMessageType.SpawnCharacterAt:
		case NetMessageType.ConveyorSpawn:
		case NetMessageType.GameEntry:
		case NetMessageType.TipsPlay:
		case NetMessageType.DamagePart:
		case NetMessageType.DamagePointReach:
		case NetMessageType.ArmorDamagePointReach:
		case NetMessageType.ArmorHitpointsEmpty:
		case NetMessageType.CraterCreate:
		case NetMessageType.PlantGridSnapshot:
		case NetMessageType.EventExecute:
		case NetMessageType.WaveEventExecute:
		case NetMessageType.BattleCharacterRoster:
		case NetMessageType.BattlePacketSnapshot:
		case NetMessageType.BattleSnapshotEntitiesReady:
		case NetMessageType.BattleSnapshotComplete:
		case NetMessageType.ProjectileEffectSpawn:
		case NetMessageType.CharacterComponentOperation:
			return true;
		default:
			return false;
		}
	}

	private static string NormalizeAuthenticatedUserId(string data, string authenticatedSenderId)
	{
		Variant variant = Json.ParseString(data);
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return data;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		dictionary["user_id"] = authenticatedSenderId;
		return Json.Stringify(dictionary);
	}

	private void _HandleNetworkMessageFallback(NetMessageContext context)
	{
		if (context != null)
		{
			string legacyOpCode = context.LegacyOpCode;
			if (!(legacyOpCode == ""))
			{
				_ProcessMatchState(legacyOpCode, context.Payload, context.AuthenticatedSenderPeerId);
			}
		}
	}

	private void _BroadcastReliableEnvelope(string opCode, string data, string senderPeerId, bool forwarded = true)
	{
		string text = _CreateEnvelopeData(opCode, data, NetDeliveryMode.Reliable, senderPeerId, forwarded);
		if (!(text == ""))
		{
			Rpc("_RpcReceiveMatchState", "__net_v2", text);
		}
	}

	private void _RejectLegacyProtocol(string senderId)
	{
		if (isHost && int.TryParse(senderId, out var result) && result != 1)
		{
			_peer?.DisconnectPeer(result);
		}
	}

	private void _RejectProtocolVersion(string senderId, int remoteProtocolVersion)
	{
		BroadCastManager.Instance.BroadCastFloatCreate($"Protocol mismatch: local v{4}, remote v{remoteProtocolVersion}", Colors.Red);
		if (isHost && int.TryParse(senderId, out var result) && result != 1)
		{
			_peer?.DisconnectPeer(result);
		}
	}

	public void _ProcessMatchState(string opCode, string data, string senderId)
	{
		if (!IsEnvelopeSenderAuthorized(NetProtocol.FromLegacyOpCode(opCode), senderId) || !AllowsModMessage(NetProtocol.FromLegacyOpCode(opCode), senderId))
		{
			return;
		}
		if (isHost && senderId != peerId && (opCode == "choose_ready" || opCode == "cursor_pick_sync"))
		{
			data = NormalizeAuthenticatedUserId(data, senderId);
		}
		switch (opCode)
		{
		case "start_game":
			if (senderId == "1")
			{
				_HandleStartGame(data);
			}
			return;
		case "select_level":
			if (!isHost && senderId == "1")
			{
				_HandleSelectLevel(data);
			}
			return;
		case "level_config":
			if (!isHost && senderId == "1")
			{
				_HandleLevelConfig(data);
			}
			return;
		case "level_config_ack":
			ReceiveModAck(senderId, data);
			return;
		case "choose_ready":
			if (isHost && senderId != peerId)
			{
				_BroadcastReliableEnvelope(opCode, data, senderId);
			}
			this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			return;
		case "choose_over":
			this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			return;
		case "use_shovel":
		case "remove_plant":
		case "packet_pick":
		case "place_plant":
		case "move_character":
		case "vase_break_request":
		case "gem_match_command":
			this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			return;
		case "command_rejected":
			this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			return;
		case "client_ready":
		case "game_entry_ack":
			if (isHost && senderId != peerId)
			{
				this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			}
			return;
		case "wave_event_execute":
		case "projectile_effect_spawn":
		case "event_execute":
		case "character_component_operation":
			if (!isHost)
			{
				this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			}
			return;
		case "battle_snapshot_request":
			if (isHost && senderId != peerId)
			{
				this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
			}
			return;
		}
		if (senderId != peerId)
		{
			if (isHost)
			{
				_BroadcastReliableEnvelope(opCode, data, senderId);
			}
			this.OnMatchStateReceived?.Invoke(opCode, data, senderId);
		}
	}

	public void SendSelectLevel(string levelId)
	{
		if (isHost)
		{
			_hostModBatch?.Cancel("关卡选择变化，请重新确认。");
			selectedLevelId = levelId;
			XWModLevelIdentity selectedLevelIdentity = GetSelectedLevelIdentity();
			string difficulty = selectedLevelIdentity.Difficulty;
			string text = (selectedLevelIdentity.IsMod ? "" : _FindLevelUid(levelId, difficulty));
			Dictionary dictionary = new Dictionary
			{
				{ "level_id", levelId },
				{ "user_id", peerId },
				{
					"level_identity",
					selectedLevelIdentity.ToDictionary()
				},
				{ "difficult", difficulty },
				{ "level_uid", text }
			};
			SendMatchState("select_level", Json.Stringify(dictionary));
		}
	}

	public void SendLevelConfig()
	{
		StartCompatibleGameAsync();
	}

	private string BuildLevelConfigData()
	{
		TowerDefenseLevelBaseConfig currentLevelConfig = TowerDefenseManager.Instance.currentLevelConfig;
		if (!GodotObject.IsInstanceValid(currentLevelConfig))
		{
			return "";
		}
		string text = "";
		string text2 = "";
		if (currentLevelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			text = "TowerDefenseLevelConfig";
			if (towerDefenseLevelConfig.data != null && towerDefenseLevelConfig.data.Data.VariantType == Variant.Type.Dictionary)
			{
				text2 = Json.Stringify((Dictionary)towerDefenseLevelConfig.data.Data);
			}
			else
			{
				towerDefenseLevelConfig.ExportToFeatureProcess();
				text2 = Json.Stringify(new Dictionary
				{
					{
						"Feature",
						_SerializeFeatures(_ConvertFeatureData(towerDefenseLevelConfig.featureData))
					},
					{
						"Process",
						new Dictionary
						{
							{ "Name", towerDefenseLevelConfig.processName },
							{ "Data", towerDefenseLevelConfig.processData }
						}
					},
					{ "Name", towerDefenseLevelConfig.name },
					{ "LevelName", towerDefenseLevelConfig.levelName },
					{ "Description", towerDefenseLevelConfig.description },
					{ "LevelNumber", towerDefenseLevelConfig.levelNumber },
					{ "baseTimeScale", towerDefenseLevelConfig.baseTimeScale }
				});
			}
		}
		else
		{
			if (!(currentLevelConfig is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig))
			{
				return "";
			}
			text = "TowerDefenseLevelNewConfig";
			text2 = ((towerDefenseLevelNewConfig.data == null || towerDefenseLevelNewConfig.data.Data.VariantType != Variant.Type.Dictionary) ? Json.Stringify(new Dictionary
			{
				{
					"Feature",
					_SerializeFeatures(_ConvertFeatureData(towerDefenseLevelNewConfig.featureData))
				},
				{
					"Process",
					new Dictionary
					{
						{ "Name", towerDefenseLevelNewConfig.processName },
						{ "Data", towerDefenseLevelNewConfig.processData }
					}
				},
				{ "Name", towerDefenseLevelNewConfig.name },
				{ "LevelName", towerDefenseLevelNewConfig.levelName },
				{ "Description", towerDefenseLevelNewConfig.description },
				{ "LevelNumber", towerDefenseLevelNewConfig.levelNumber },
				{ "Version", towerDefenseLevelNewConfig.version }
			}) : Json.Stringify((Dictionary)towerDefenseLevelNewConfig.data.Data));
		}
		return Json.Stringify(new Dictionary
		{
			{ "config_type", text },
			{
				"level_identity",
				GetSelectedLevelIdentity().ToDictionary()
			},
			{ "config_json", text2 },
			{
				"enter_level_mode",
				Global.Instance.enterLevelMode
			},
			{
				"enter_level_is_battle",
				Global.Instance.enterLevelIsBattle
			}
		});
	}

	private Dictionary _ConvertFeatureData(Godot.Collections.Dictionary<StringName, Dictionary> featureData)
	{
		Dictionary dictionary = new Dictionary();
		foreach (KeyValuePair<StringName, Dictionary> featureDatum in featureData)
		{
			dictionary[featureDatum.Key] = featureDatum.Value;
		}
		return dictionary;
	}

	private Godot.Collections.Array _SerializeFeatures(Dictionary featureData)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant key in featureData.Keys)
		{
			string text = (string)key;
			array.Add(new Dictionary
			{
				{ "Name", text },
				{
					"Data",
					featureData[text]
				}
			});
		}
		return array;
	}

	private string _FindLevelUid(string levelId, string difficult)
	{
		foreach (string key in ResourceManager.Instance.LEVELS.Keys)
		{
			Dictionary dictionary = ResourceManager.Instance.LEVELS[key];
			if (dictionary == null || !dictionary.ContainsKey("Chapter"))
			{
				continue;
			}
			foreach (Variant item in (Godot.Collections.Array)dictionary["Chapter"])
			{
				Dictionary dictionary2 = (Dictionary)item;
				if (!dictionary2.ContainsKey("Level"))
				{
					continue;
				}
				foreach (Variant item2 in (Godot.Collections.Array)dictionary2["Level"])
				{
					Dictionary dictionary3 = (Dictionary)item2;
					if ((string)dictionary3.GetValueOrDefault("SaveKey", "") != levelId)
					{
						continue;
					}
					Dictionary dictionary4 = (Dictionary)dictionary3.GetValueOrDefault("Level", new Dictionary());
					if (dictionary4 != null && dictionary4.Count > 0)
					{
						string text = (string)dictionary4.GetValueOrDefault(difficult, "");
						if (text != "" && ResourceLoader.Exists(text))
						{
							return text;
						}
						text = (string)dictionary4.GetValueOrDefault("Normal", "");
						if (text != "" && ResourceLoader.Exists(text))
						{
							return text;
						}
					}
				}
			}
		}
		return "";
	}

	public void SendStartGame()
	{
		if (!isHost)
		{
			return;
		}
		ModCompatibilityBatch hostModBatch = _hostModBatch;
		if (hostModBatch == null || !hostModBatch.Accepted)
		{
			return;
		}
		XWModEnvironmentService.EnvironmentLease modEnvironmentLease = _modEnvironmentLease;
		if (modEnvironmentLease != null && modEnvironmentLease.IsCurrent && !(_acceptedModBatch != _hostModBatch.Id))
		{
			XWModLevelIdentity acceptedLevelIdentity = _acceptedLevelIdentity;
			if (!(acceptedLevelIdentity == null))
			{
				string difficulty = acceptedLevelIdentity.Difficulty;
				string text = "";
				Dictionary dictionary = new Dictionary
				{
					{ "config_batch_id", _acceptedModBatch },
					{
						"level_identity",
						acceptedLevelIdentity.ToDictionary()
					},
					{
						"snapshot_sha256",
						_modEnvironmentLease.Status.Snapshot.Sha256
					},
					{ "battle_id", _modBattleId },
					{ "admitted_peers", BattleAdmittedPeers },
					{ "level_id", acceptedLevelIdentity.LevelSaveKey },
					{ "difficult", difficulty },
					{ "level_uid", text }
				};
				_activeBattleStartData = Json.Stringify(dictionary);
				SendMatchState("start_game", _activeBattleStartData);
			}
		}
	}

	private void SendLateJoinBootstrap(string targetPeerId)
	{
		if (isHost && targetPeerId != "")
		{
			SendCompatibleLateJoinAsync(targetPeerId);
		}
	}

	public void SendPlacePlant(string plantName, int gridX, int gridY, int syncId = -1, string overrideData = "", string requestId = "", string ownerPeerId = "", string placementKind = "grid", int targetSyncId = -1, bool hypnoses = false)
	{
		string text = (string.IsNullOrWhiteSpace(ownerPeerId) ? peerId : ownerPeerId);
		Dictionary dictionary = new Dictionary
		{
			{ "plant_name", plantName },
			{ "grid_x", gridX },
			{ "grid_y", gridY },
			{ "sync_id", syncId },
			{ "user_id", peerId },
			{ "owner_peer_id", text },
			{ "override_data", overrideData },
			{ "placement_kind", placementKind }
		};
		if (targetSyncId >= 0)
		{
			dictionary["target_sync_id"] = targetSyncId;
			dictionary["hypnoses"] = hypnoses;
		}
		if (requestId != "")
		{
			dictionary["request_id"] = requestId;
		}
		if (isHost && placementKind == "jala_vase" && TowerDefenseManager.Instance.currentControl._syncCharacters.TryGetValue(targetSyncId, out var value) && value is TowerDefensePlantJalaVase towerDefensePlantJalaVase && GodotObject.IsInstanceValid(towerDefensePlantJalaVase))
		{
			dictionary["target_state"] = towerDefensePlantJalaVase.ExportVariantSave();
		}
		SendMatchState("place_plant", Json.Stringify(dictionary));
	}

	public void SendCommandRejected(string targetPeerId, string rejectedOpCode, string requestId, string reason)
	{
		if (isHost && !(targetPeerId == ""))
		{
			Dictionary dictionary = new Dictionary
			{
				{ "op_code", rejectedOpCode },
				{ "request_id", requestId },
				{ "reason", reason },
				{ "target_user_id", targetPeerId },
				{ "user_id", peerId }
			};
			SendEnvelope(new NetMessageEnvelope
			{
				message_type = NetMessageType.CommandRejected,
				payload = Json.Stringify(dictionary)
			}, NetDeliveryMode.Reliable, targetPeerId);
		}
	}

	public void SendRemovePlant(int gridX, int gridY)
	{
		Dictionary dictionary = new Dictionary
		{
			{ "grid_x", gridX },
			{ "grid_y", gridY },
			{ "user_id", peerId }
		};
		SendMatchState("remove_plant", Json.Stringify(dictionary));
	}

	public void SendUseShovel(int gridX, int gridY)
	{
		Dictionary dictionary = new Dictionary
		{
			{ "grid_x", gridX },
			{ "grid_y", gridY },
			{ "user_id", peerId }
		};
		SendMatchState("use_shovel", Json.Stringify(dictionary));
	}

	public void SendMoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5)
	{
		if (syncId >= 0 && !(fromGridPos == toGridPos))
		{
			moveKind = (string.IsNullOrWhiteSpace(moveKind) ? "drag" : moveKind);
			duration = ((moveKind == "magnet") ? 2.0 : 0.5);
			Dictionary dictionary = new Dictionary
			{
				{ "sync_id", syncId },
				{ "from_grid_x", fromGridPos.X },
				{ "from_grid_y", fromGridPos.Y },
				{ "to_grid_x", toGridPos.X },
				{ "to_grid_y", toGridPos.Y },
				{ "move_kind", moveKind },
				{ "duration", duration },
				{ "user_id", peerId }
			};
			SendMatchState("move_character", Json.Stringify(dictionary));
		}
	}

	public void SendGameStateSync(Dictionary state)
	{
		if (isHost)
		{
			SendMatchState("game_state_sync", Json.Stringify(state));
		}
	}

	public void SendSpawnZombie(string zombieName, int line, double offsetX, int syncId, string spawnOverrideData = "", string spawnConfigOverrideData = "")
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "zombie_name", zombieName },
				{ "line", line },
				{ "offset_x", offsetX },
				{ "sync_id", syncId },
				{ "user_id", peerId },
				{ "spawn_override", spawnOverrideData },
				{ "spawn_config_override", spawnConfigOverrideData }
			};
			SendMatchState("spawn_zombie", Json.Stringify(dictionary));
		}
	}

	public void SendSpawnGrid(string packetName, int gridX, int gridY, int syncId)
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "packet_name", packetName },
				{ "grid_x", gridX },
				{ "grid_y", gridY },
				{ "sync_id", syncId },
				{ "user_id", peerId }
			};
			SendMatchState("spawn_grid", Json.Stringify(dictionary));
		}
	}

	public void SendGameResult(bool victory)
	{
		if (isHost)
		{
			SendMatchState("game_result", MatchStateSerializer.Serialize(new GameResultDto
			{
				victory = victory
			}));
		}
	}

	public void SendCharacterDestroy(int syncId, bool isExplode = false, bool isSmash = false)
	{
		if (isHost)
		{
			SendMatchState("character_destroy", MatchStateSerializer.Serialize(new CharacterDestroyDto
			{
				sync_id = syncId,
				is_explode = isExplode,
				is_smash = isSmash
			}));
		}
	}

	public void SendCharacterInit(int syncId, double posX, double posY, double hp, bool die, string clipName = "", bool loopAnim = true, double blendTimeVal = 0.0, int frameIndexVal = 0, double timeScaleVal = 1.0, double walkSpeedScaleVal = 1.0)
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "sync_id", syncId },
				{ "x", posX },
				{ "y", posY },
				{ "hp", hp },
				{ "die", die },
				{ "clip", clipName },
				{ "loop", loopAnim },
				{ "blendTime", blendTimeVal },
				{ "frame", frameIndexVal },
				{ "timeScale", timeScaleVal },
				{ "walkSpeedScale", walkSpeedScaleVal }
			};
			SendMatchState("character_init", Json.Stringify(dictionary));
		}
	}

	public void SendCharacterStateSync(string charactersData)
	{
		if (isHost)
		{
			SendMatchState("character_state_sync", charactersData);
		}
	}

	public void SendCharacterPositionSync(int syncId, double posX, double posY)
	{
		if (isHost)
		{
			SendMatchStateUnreliable("character_position_sync", Json.Stringify(new Dictionary
			{
				{ "sync_id", syncId },
				{ "x", posX },
				{ "y", posY }
			}));
		}
	}

	public void SendZombieFullSync(string zombiesData)
	{
		if (isHost)
		{
			SendMatchStateUnreliable("zombie_full_sync", zombiesData);
		}
	}

	public void SendCursorSync(double posX, double posY)
	{
		SendMatchStateUnreliable("cursor_sync", Json.Stringify(new Dictionary
		{
			{ "x", posX },
			{ "y", posY },
			{ "user_id", peerId }
		}));
	}

	public void SendCursorPickSync(string pickType, string pickName)
	{
		SendMatchState("cursor_pick_sync", Json.Stringify(new Dictionary
		{
			{ "pickType", pickType },
			{ "pickName", pickName },
			{ "user_id", peerId }
		}));
	}

	public void SendChooseReady()
	{
		SendMatchState("choose_ready", Json.Stringify(new Dictionary { { "user_id", peerId } }));
	}

	public void SendChooseOver()
	{
		if (isHost)
		{
			SendMatchState("choose_over", Json.Stringify(new Dictionary()));
		}
	}

	public void SendVaseBreakRequest(int gridX, int gridY)
	{
		Dictionary dictionary = new Dictionary
		{
			{ "grid_x", gridX },
			{ "grid_y", gridY },
			{ "user_id", peerId }
		};
		SendMatchState("vase_break_request", Json.Stringify(dictionary));
	}

	public void SendGemMatchCommand(string action, Vector2I from = default(Vector2I), Vector2I to = default(Vector2I), string upgradeKey = "")
	{
		if (!string.IsNullOrWhiteSpace(action))
		{
			Dictionary dictionary = new Dictionary
			{
				["action"] = action,
				["from_x"] = from.X,
				["from_y"] = from.Y,
				["to_x"] = to.X,
				["to_y"] = to.Y,
				["upgrade_key"] = upgradeKey ?? "",
				["user_id"] = peerId
			};
			SendMatchState("gem_match_command", Json.Stringify(dictionary));
		}
	}

	public void SendVaseBreakResult(Dictionary breakData)
	{
		if (isHost)
		{
			SendMatchState("vase_break", Json.Stringify(breakData));
		}
	}

	public void SendPacketSpawn(int syncId, string packetName, double posX, double posY, double aliveTime, bool isFall, bool useCost, double velocityX, double velocityY, int zIndex, double fallHeight = 0.0, TowerDefensePacketConfig packetConfig = null, EconomyAccountId sunAccountId = default(EconomyAccountId))
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "sync_id", syncId },
				{ "packet_name", packetName },
				{ "pos_x", posX },
				{ "pos_y", posY },
				{ "alive_time", aliveTime },
				{ "is_fall", isFall },
				{ "use_cost", useCost },
				{ "velocity_x", velocityX },
				{ "velocity_y", velocityY },
				{ "z_index", zIndex },
				{ "fall_height", fallHeight },
				{
					"sun_account",
					sunAccountId.ToString()
				}
			};
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				TowerDefensePacketRuntimeState.Write(packetConfig, dictionary, "packet_override", "can_change_cost", "change_cost_list");
			}
			SendMatchState("packet_spawn", Json.Stringify(dictionary));
		}
	}

	public void SendPacketPick(int syncId, string pickType = "remove")
	{
		Dictionary dictionary = new Dictionary
		{
			{ "sync_id", syncId },
			{ "pick_type", pickType },
			{ "user_id", peerId }
		};
		SendMatchState("packet_pick", Json.Stringify(dictionary));
	}

	public void SendPause()
	{
		SendMatchState("pause", Json.Stringify(new Dictionary { { "user_id", peerId } }));
	}

	public void SendResume()
	{
		SendMatchState("resume", Json.Stringify(new Dictionary { { "user_id", peerId } }));
	}

	public void SendSpawnCoin(double posX, double posY, int num, double velocityX, double velocityY, double gravity, double height, bool collect)
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "pos_x", posX },
				{ "pos_y", posY },
				{ "num", num },
				{ "velocity_x", velocityX },
				{ "velocity_y", velocityY },
				{ "gravity", gravity },
				{ "height", height },
				{ "collect", collect }
			};
			SendMatchState("spawn_coin", Json.Stringify(dictionary));
		}
	}

	public void SendSpawnFallingObject(ObjectManagerConfig.OBJECT objectId, double posX, double posY, double velocityX, double velocityY, double gravity, double height, int gridX, int gridY)
	{
		if (isHost && objectId != ObjectManagerConfig.OBJECT.NOONE)
		{
			long num = ++_fallingObjectSpawnSequence;
			Dictionary dictionary = new Dictionary
			{
				{
					"object_id",
					(int)objectId
				},
				{ "spawn_sequence", num },
				{ "pos_x", posX },
				{ "pos_y", posY },
				{ "velocity_x", velocityX },
				{ "velocity_y", velocityY },
				{ "gravity", gravity },
				{ "height", height },
				{ "grid_x", gridX },
				{ "grid_y", gridY }
			};
			SendMatchState("spawn_falling_object", Json.Stringify(dictionary));
		}
	}

	public void SendSpawnCharacterAt(string packetName, int gridX, int gridY, int syncId, double hitpointScale = 1.0, double scaleVal = 1.0, bool hypnoses = false, double riseDuration = 0.0, bool useCreate = false, double posX = 0.0, double posY = 0.0, bool walkAfterSpawn = false, double groundHeight = 0.0, string sizeVal = "", Dictionary spawnState = null, string economyOwner = "")
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "packet_name", packetName },
				{ "grid_x", gridX },
				{ "grid_y", gridY },
				{ "sync_id", syncId },
				{ "hitpoint_scale", hitpointScale },
				{ "scale", scaleVal },
				{ "hypnoses", hypnoses },
				{ "rise_duration", riseDuration },
				{ "use_create", useCreate },
				{ "pos_x", posX },
				{ "pos_y", posY },
				{ "walk_after_spawn", walkAfterSpawn },
				{ "ground_height", groundHeight },
				{ "size", sizeVal },
				{ "economy_owner", economyOwner },
				{ "user_id", peerId }
			};
			if (spawnState != null && spawnState.Count > 0)
			{
				dictionary["spawn_state"] = spawnState;
			}
			SendMatchState("spawn_character_at", Json.Stringify(dictionary));
		}
	}

	public void SendConveyorSpawn(string packetName, string packetType)
	{
		if (isHost)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "packet_name", packetName },
				{ "packet_type", packetType },
				{ "user_id", peerId }
			};
			SendMatchState("conveyor_spawn", Json.Stringify(dictionary));
		}
	}

	public void SendClientReady()
	{
		if (!isHost)
		{
			SendMatchState("client_ready", MatchStateSerializer.Serialize(new UserIdDto
			{
				user_id = peerId
			}));
		}
	}

	public void SendGameEntry(int roundNum)
	{
		if (isHost)
		{
			_gameEntrySent = true;
			SendMatchState("game_entry", MatchStateSerializer.Serialize(new GameEntryDto
			{
				round_num = roundNum
			}));
		}
	}

	public void SendTipsPlay(string text, double duration)
	{
		if (isHost)
		{
			SendMatchState("tips_play", MatchStateSerializer.Serialize(new TipsPlayDto
			{
				text = text,
				duration = duration
			}));
		}
	}

	public long SendDamagePart(int syncId, string partName, double posX, double posY, double velocityX, double velocityY, long sequence = 0L)
	{
		if (!isHost)
		{
			return 0L;
		}
		sequence = ((sequence > 0) ? sequence : (++_damagePartSpawnSequence));
		SendMatchState("damage_part", MatchStateSerializer.Serialize(new DamagePartDto
		{
			sync_id = syncId,
			part_name = partName,
			px = posX,
			py = posY,
			vx = velocityX,
			vy = velocityY,
			seq = sequence
		}));
		return sequence;
	}

	public void SendDamagePointReach(int syncId, string damagePointName)
	{
		if (isHost)
		{
			SendMatchState("damage_point_reach", MatchStateSerializer.Serialize(new DamagePointReachDto
			{
				sync_id = syncId,
				damage_point_name = damagePointName
			}));
		}
	}

	public void SendArmorDamagePointReach(int syncId, string armorName, int stage)
	{
		if (isHost)
		{
			SendMatchState("armor_damage_point_reach", MatchStateSerializer.Serialize(new ArmorDamagePointReachDto
			{
				sync_id = syncId,
				armor_name = armorName,
				stage = stage
			}));
		}
	}

	public void SendArmorHitpointsEmpty(int syncId, string armorName)
	{
		if (isHost)
		{
			SendMatchState("armor_hitpoints_empty", MatchStateSerializer.Serialize(new ArmorHitpointsEmptyDto
			{
				sync_id = syncId,
				armor_name = armorName
			}));
		}
	}

	public void SendCraterCreate(int gridX, int gridY, string craterName)
	{
		if (isHost)
		{
			SendMatchState("crater_create", MatchStateSerializer.Serialize(new CraterCreateDto
			{
				grid_x = gridX,
				grid_y = gridY,
				crater_name = craterName
			}));
		}
	}

	public void SendProjectileEffectSpawn(ProjectileEffectSpawnDto dto)
	{
		if (isHost && dto != null)
		{
			SendMatchState("projectile_effect_spawn", MatchStateSerializer.Serialize(dto));
		}
	}

	public void SendCharacterComponentOperation(int ownerSyncId, string componentInstanceId, string componentTypeId, long sequence, string operationName, Dictionary operationData)
	{
		if (isHost && ownerSyncId >= 0 && !string.IsNullOrWhiteSpace(componentInstanceId) && !string.IsNullOrWhiteSpace(componentTypeId) && !string.IsNullOrWhiteSpace(operationName) && sequence >= 0)
		{
			Dictionary dictionary = new Dictionary
			{
				{ "sync_id", ownerSyncId },
				{ "component_instance_id", componentInstanceId },
				{ "component_type_id", componentTypeId },
				{ "sequence", sequence },
				{ "operation_name", operationName },
				{
					"data",
					operationData?.Duplicate(deep: true) ?? new Dictionary()
				}
			};
			SendMatchState("character_component_operation", Json.Stringify(dictionary));
		}
	}

	public void SendPlantFullSync(string plantsData)
	{
		if (isHost)
		{
			SendMatchState("plant_full_sync", plantsData);
		}
	}

	public void SendEventExecute(string phase, string eventsData)
	{
		if (isHost)
		{
			SendMatchState("event_execute", Json.Stringify(new Dictionary
			{
				{ "phase", phase },
				{ "events", eventsData }
			}));
		}
	}

	public void SendWaveEventExecute(int waveId, int eventId, string eventsData)
	{
		if (isHost)
		{
			SendMatchState("wave_event_execute", Json.Stringify(new Dictionary
			{
				{ "wave_id", waveId },
				{ "event_id", eventId },
				{ "events", eventsData }
			}));
		}
	}

	public void ResetClientsReady()
	{
		_clientsReady.Clear();
	}

	private void _ResetSessionState()
	{
		ReleaseBattleCompatibility();
		_preparingModStart = false;
		_hostModBatch = null;
		_clientsReady.Clear();
		_levelConfigAcked.Clear();
		_gameEntryAcked.Clear();
		_gameEntrySent = false;
		selectedLevelId = "";
		_receivedLevelConfigJson = "";
		_receivedLevelConfigType = "";
		_receivedLevelEnterMode = "";
		_receivedLevelIsBattle = false;
		_activeBattleLevelConfigData = "";
		_activeBattleStartData = "";
		_fallingObjectSpawnSequence = 0L;
		_damagePartSpawnSequence = 0L;
		_pingTimer = 0.0;
	}

	public bool CheckAllClientsReady()
	{
		Godot.Collections.Array battleAdmittedPeers = BattleAdmittedPeers;
		battleAdmittedPeers.Remove("1");
		if (battleAdmittedPeers.Count == 0)
		{
			return true;
		}
		foreach (Variant item in battleAdmittedPeers)
		{
			string text = (string)item;
			if (!_clientsReady.Contains(text))
			{
				return false;
			}
		}
		return true;
	}

	public void SendLevelConfigAck()
	{
	}

	public void ResetLevelConfigAck()
	{
		_hostModBatch?.Cancel("配置确认已重置，请重新开始。");
		_levelConfigAcked.Clear();
	}

	public bool CheckAllLevelConfigAcked()
	{
		ModCompatibilityBatch hostModBatch = _hostModBatch;
		if (hostModBatch != null && hostModBatch.Accepted)
		{
			return _modEnvironmentLease?.IsCurrent ?? false;
		}
		return false;
	}

	public void SendGameEntryAck()
	{
		if (!isHost)
		{
			SendMatchState("game_entry_ack", MatchStateSerializer.Serialize(new UserIdDto
			{
				user_id = peerId
			}));
		}
	}

	public void ResetGameEntryAck()
	{
		_gameEntryAcked.Clear();
		_gameEntrySent = false;
	}

	public bool CheckAllGameEntryAcked()
	{
		Godot.Collections.Array battleAdmittedPeers = BattleAdmittedPeers;
		battleAdmittedPeers.Remove("1");
		if (battleAdmittedPeers.Count == 0)
		{
			return true;
		}
		foreach (Variant item in battleAdmittedPeers)
		{
			string text = (string)item;
			if (!_gameEntryAcked.Contains(text))
			{
				return false;
			}
		}
		return true;
	}

	public string GetMatchIdShort()
	{
		if (currentMatchId == "")
		{
			return "";
		}
		if (isHost)
		{
			return _GetLanIp() + ":" + _currentPort;
		}
		return currentMatchId;
	}

	private string _GetLanIp()
	{
		string[] localAddresses = IP.GetLocalAddresses();
		List<string> list = new List<string>();
		string[] array = localAddresses;
		foreach (string text in array)
		{
			if (text == "127.0.0.1" || text.Contains(":") || text.StartsWith("169.254"))
			{
				continue;
			}
			if (text.StartsWith("172."))
			{
				int num = int.Parse(text.Split(".")[1]);
				if (num >= 16 && num <= 31)
				{
					continue;
				}
			}
			list.Add(text);
		}
		foreach (string item in list)
		{
			if (item.StartsWith("192.168."))
			{
				return item;
			}
		}
		foreach (string item2 in list)
		{
			if (item2.StartsWith("10."))
			{
				return item2;
			}
		}
		if (list.Count > 0)
		{
			return list[0];
		}
		return "127.0.0.1";
	}

	public string[] GetAllLanIps()
	{
		string[] localAddresses = IP.GetLocalAddresses();
		List<string> list = new List<string>();
		string[] array = localAddresses;
		foreach (string text in array)
		{
			if (text == "127.0.0.1" || text.Contains(":") || text.StartsWith("169.254"))
			{
				continue;
			}
			if (text.StartsWith("172."))
			{
				int num = int.Parse(text.Split(".")[1]);
				if (num >= 16 && num <= 31)
				{
					continue;
				}
			}
			list.Add(text);
		}
		return list.ToArray();
	}

	private void _OnPeerConnected(long id)
	{
		ModPeerChanged(id.ToString(), disconnected: false);
		if (!isHost)
		{
			return;
		}
		if (matchMembers.Count >= 4)
		{
			_peer.DisconnectPeer((int)id);
			return;
		}
		if (!matchMembers.Contains(id.ToString()))
		{
			matchMembers.Add(id.ToString());
		}
		this.OnPeerJoined?.Invoke("Player");
		Dictionary dictionary = new Dictionary();
		dictionary["__protocol_version"] = 4;
		foreach (int key in _peerNames.Keys)
		{
			dictionary[key.ToString()] = _peerNames[key];
		}
		RpcId((int)id, "_RpcSendHostInfo", _playerName, matchMembers, Global.Instance.version, dictionary);
		foreach (Variant matchMember in matchMembers)
		{
			int num = int.Parse((string)matchMember);
			if (num != 1 && num != (int)id)
			{
				RpcId(num, "_RpcSyncMembers", matchMembers);
			}
		}
		SendLateJoinBootstrap(id.ToString());
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	public void _RpcSendHostInfo(string hostName, Godot.Collections.Array members, string hostVersion, Dictionary namesData)
	{
		_peerNames.Clear();
		_peerNames[1] = hostName;
		int num = namesData.GetValueOrDefault("__protocol_version", 1).AsInt32();
		if (num != 4)
		{
			BroadCastManager.Instance.BroadCastFloatCreate($"Protocol mismatch: local v{4}, host v{num}", Colors.Red);
			RpcId(1L, "_RpcVersionMismatch", $"protocol:{4}");
			_CleanupConnection();
			return;
		}
		foreach (Variant key in namesData.Keys)
		{
			string text = (string)key;
			if (!(text == "__protocol_version"))
			{
				_peerNames[int.Parse(text)] = (string)namesData[text];
			}
		}
		matchMembers = members;
		if (hostVersion != Global.Instance.version)
		{
			BroadCastManager.Instance.BroadCastFloatCreate("版本不一致，无法连接", Colors.Red);
			RpcId(1L, "_RpcVersionMismatch", Global.Instance.version);
			_CleanupConnection();
		}
		else
		{
			this.OnPeerJoined?.Invoke(hostName);
			RpcId(1L, "_RpcSendClientName", _playerName);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void _RpcVersionMismatch(string clientVersion)
	{
		long num = base.Multiplayer.GetRemoteSenderId();
		string valueOrDefault = _peerNames.GetValueOrDefault((int)num, "Client");
		BroadCastManager.Instance.BroadCastFloatCreate(valueOrDefault + " 版本不一致(" + clientVersion + ")，已断开", Colors.Red);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	public void _RpcSyncMembers(Godot.Collections.Array members)
	{
		matchMembers = members;
		this.OnPeerJoined?.Invoke("Player");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void _RpcSendClientName(string playerName)
	{
		long num = base.Multiplayer.GetRemoteSenderId();
		_peerNames[(int)num] = playerName;
		this.OnPeerJoined?.Invoke(playerName);
		_broadcastPeerNames();
	}

	private void _broadcastPeerNames()
	{
		if (!isHost)
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		foreach (int key in _peerNames.Keys)
		{
			dictionary[key.ToString()] = _peerNames[key];
		}
		dictionary["1"] = _playerName;
		Rpc("_RpcSyncPeerNames", dictionary);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	public void _RpcSyncPeerNames(Dictionary namesData)
	{
		_peerNames.Clear();
		foreach (Variant key in namesData.Keys)
		{
			string text = (string)key;
			_peerNames[int.Parse(text)] = (string)namesData[text];
		}
	}

	private void _OnPeerDisconnected(long id)
	{
		string valueOrDefault = _peerNames.GetValueOrDefault((int)id, "Player");
		string text = id.ToString();
		ModPeerChanged(text, disconnected: true);
		matchMembers.Remove(text);
		_peerNames.Remove((int)id);
		_clientsReady.Remove(text);
		_levelConfigAcked.Remove(text);
		_gameEntryAcked.Remove(text);
		_peerLatencies.Remove(text);
		this.OnPeerLeft?.Invoke(valueOrDefault, text);
		ReevaluateHostBarriersAfterPeerLeft();
	}

	private void ReevaluateHostBarriersAfterPeerLeft()
	{
		if (isHost)
		{
			bool num = CheckAllClientsReady();
			bool flag = CheckAllLevelConfigAcked();
			bool flag2 = _gameEntrySent && CheckAllGameEntryAcked();
			if (num)
			{
				EmitAllClientsReady();
			}
			if (flag)
			{
				this.OnAllLevelConfigAcked?.Invoke();
			}
			if (flag2)
			{
				EmitAllGameEntryAcked();
			}
		}
	}

	private void _OnConnectedToServer()
	{
		this.OnMatchJoined?.Invoke(currentMatchId);
		BroadCastManager.Instance.BroadCastFloatCreate("JOINED_ROOM", Colors.Green);
	}

	private void _OnConnectionFailed()
	{
		BroadCastManager.Instance.BroadCastFloatCreate("JOIN_MATCH_FAILED", Colors.Red);
		_CleanupConnection();
	}

	private void _OnServerDisconnected()
	{
		BroadCastManager.Instance.BroadCastFloatCreate("HOST_DISCONNECTED", Colors.Red);
		_CleanupConnection();
	}

	private void _CleanupConnection()
	{
		if (currentMatchId != "" || _peer != null || isHost || (Global.Instance != null && Global.Instance.isMultiplayerMode))
		{
			_sessionGeneration++;
			_startGameTransitionPending = false;
			_startGameTransitionCompleted = false;
			if (_peer != null)
			{
				_peer.Close();
			}
			base.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
			_peer = null;
			currentMatchId = "";
			isHost = false;
			_currentPort = 7777;
			_playerName = "";
			matchMembers.Clear();
			_peerNames.Clear();
			_pingSendTimes.Clear();
			_peerLatencies.Clear();
			_lastUnreliableSequenceByStream.Clear();
			_ResetSessionState();
			if (Global.Instance != null)
			{
				Global.Instance.isMultiplayerMode = false;
				Global.Instance.isMultiplayerHost = false;
			}
			this.OnConnectionChanged?.Invoke(connected: false);
			this.OnMatchLeft?.Invoke();
		}
	}

	public void _HandleStartGame(string data)
	{
		if (!_startGameTransitionPending && !_startGameTransitionCompleted && !(currentMatchId == "") && _peer != null && AcceptCompatibleStart(data))
		{
			_modSceneAccepted = new TaskCompletionSource<bool>();
			_startGameTransitionPending = true;
			long sessionGeneration = _sessionGeneration;
			HandleStartGameAsync(data, sessionGeneration);
		}
	}

	private async Task HandleStartGameAsync(string data, long sessionGeneration)
	{
		TaskCompletionSource<bool> completion = _modSceneAccepted;
		try
		{
			Variant variant = Json.ParseString(data);
			if (variant.VariantType != Variant.Type.Dictionary)
			{
				return;
			}
			Dictionary dictionary = (Dictionary)variant;
			XWModLevelIdentity identity = _acceptedLevelIdentity;
			selectedLevelId = identity.LevelSaveKey;
			string difficulty = identity.Difficulty;
			string text = (string)dictionary.GetValueOrDefault("level_uid", "");
			Global.Instance.enterLevelMode = ((_receivedLevelEnterMode != "") ? _receivedLevelEnterMode : "Multiplayer");
			Global.Instance.enterLevelId = selectedLevelId;
			Global.Instance.enterLevelIsBattle = _receivedLevelIsBattle;
			Global.Instance.isMultiplayerMode = true;
			Global.Instance.isMultiplayerHost = isHost;
			XWModLevelSession.Select(identity);
			if (identity.IsMod)
			{
				if (!XWModContentCatalog.TryResolve(identity, out var config, out var error))
				{
					throw new InvalidOperationException(error);
				}
				TowerDefenseManager.Instance.currentLevelConfig = config;
				Global.Instance.currentLevelChoose = "__ModLevels";
				Global.Instance.enterLevelMode = "ModLevel";
			}
			else if (!isHost)
			{
				TowerDefenseManager.Instance.currentLevelConfig = null;
				if (_receivedLevelConfigJson != "")
				{
					_CreateLevelConfigFromReceived();
				}
				else if (text != "" && ResourceLoader.Exists(text))
				{
					TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(text);
				}
				else
				{
					_LoadLevelById(selectedLevelId, difficulty);
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (sessionGeneration != _sessionGeneration || currentMatchId == "" || _peer == null || !IsInsideTree())
			{
				return;
			}
			XWModEnvironmentService.EnvironmentLease modEnvironmentLease = _modEnvironmentLease;
			bool accepted;
			if (modEnvironmentLease != null && modEnvironmentLease.IsCurrent && _modBattleActive)
			{
				TowerDefenseLevelBaseConfig currentLevelConfig = TowerDefenseManager.Instance.currentLevelConfig;
				if (!GodotObject.IsInstanceValid(currentLevelConfig) || (currentLevelConfig.name != identity.LevelSaveKey && (!(currentLevelConfig.name == "") || !(identity.LevelSaveKey == "__session"))))
				{
					throw new InvalidOperationException("开战配置与已确认的关卡身份不一致。");
				}
				_startGameTransitionCompleted = true;
				accepted = false;
				SceneManager.Instance.OnSceneChange += SceneAccepted;
				try
				{
					SceneManager.Instance.ChangeScene("TowerDefense");
				}
				finally
				{
					SceneManager.Instance.OnSceneChange -= SceneAccepted;
				}
				if (!accepted)
				{
					throw new InvalidOperationException("场景切换未被受理，请退出后重新加入。");
				}
				completion?.TrySetResult(result: true);
			}
			void SceneAccepted(string scene)
			{
				if (scene == "TowerDefense")
				{
					accepted = true;
				}
			}
		}
		catch (Exception ex)
		{
			if (sessionGeneration == _sessionGeneration)
			{
				_CleanupConnection();
				ShowModCompatibilityFailure("多人场景切换失败: " + ex.Message);
			}
		}
		finally
		{
			completion?.TrySetResult(result: false);
			if (sessionGeneration == _sessionGeneration)
			{
				_startGameTransitionPending = false;
			}
		}
	}

	private void _LoadLevelById(string levelId, string difficult = "Normal")
	{
		foreach (string key in ResourceManager.Instance.LEVELS.Keys)
		{
			Dictionary dictionary = ResourceManager.Instance.LEVELS[key];
			if (dictionary == null || !dictionary.ContainsKey("Chapter"))
			{
				continue;
			}
			foreach (Variant item in (Godot.Collections.Array)dictionary["Chapter"])
			{
				Dictionary dictionary2 = (Dictionary)item;
				if (!dictionary2.ContainsKey("Level"))
				{
					continue;
				}
				foreach (Variant item2 in (Godot.Collections.Array)dictionary2["Level"])
				{
					Dictionary dictionary3 = (Dictionary)item2;
					if (!dictionary3.ContainsKey("SaveKey") || !((string)dictionary3["SaveKey"] == levelId))
					{
						continue;
					}
					Dictionary dictionary4 = (Dictionary)dictionary3.GetValueOrDefault("Level", new Dictionary());
					if (dictionary4 != null && dictionary4.Count > 0)
					{
						string text = (string)dictionary4.GetValueOrDefault(difficult, "");
						if (text == "" || !ResourceLoader.Exists(text))
						{
							text = (string)dictionary4.GetValueOrDefault("Normal", "");
						}
						if (text != "" && ResourceLoader.Exists(text))
						{
							TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(text);
						}
					}
					return;
				}
			}
		}
	}

	private void _HandleSelectLevel(string data)
	{
		Variant variant = Json.ParseString(data);
		if (variant.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = (Dictionary)variant;
			if (ReadLevelIdentity(dictionary, out var identity) && !(dictionary.GetValueOrDefault("level_id", "").AsString() != identity.LevelSaveKey) && !(dictionary.GetValueOrDefault("difficult", "").AsString() != identity.Difficulty))
			{
				selectedLevelId = identity.LevelSaveKey;
			}
		}
	}

	private void _HandleLevelConfig(string data)
	{
		ReceiveCompatibleLevelConfig(data);
	}

	private void _CreateLevelConfigFromReceived()
	{
		if (_receivedLevelConfigJson == "")
		{
			return;
		}
		string receivedLevelConfigType = _receivedLevelConfigType;
		if (!(receivedLevelConfigType == "TowerDefenseLevelConfig"))
		{
			if (receivedLevelConfigType == "TowerDefenseLevelNewConfig")
			{
				Json json = new Json();
				if (json.Parse(_receivedLevelConfigJson) == Error.Ok)
				{
					TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = new TowerDefenseLevelNewConfig();
					towerDefenseLevelNewConfig.data = json;
					TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelNewConfig;
				}
			}
		}
		else
		{
			Json json2 = new Json();
			if (json2.Parse(_receivedLevelConfigJson) == Error.Ok)
			{
				TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
				towerDefenseLevelConfig.data = json2;
				TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
			}
		}
		_receivedLevelConfigJson = "";
		_receivedLevelConfigType = "";
		_receivedLevelEnterMode = "";
		_receivedLevelIsBattle = false;
	}

	private XWModLevelIdentity GetSelectedLevelIdentity()
	{
		TowerDefenseLevelBaseConfig currentLevelConfig = TowerDefenseManager.Instance.currentLevelConfig;
		if (Global.Instance.enterLevelMode == "ModLevel")
		{
			XWModLevelIdentity current = XWModLevelSession.Current;
			if (current == null || current.LevelSaveKey != currentLevelConfig.name)
			{
				throw new InvalidOperationException("Mod 选关身份与配置不一致。");
			}
			return current;
		}
		string text = Global.Instance.currentLevelChoose;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Global.Instance.enterLevelMode;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Multiplayer";
		}
		string text2 = currentLevelConfig?.name;
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = (string.IsNullOrWhiteSpace(selectedLevelId) ? "__session" : selectedLevelId);
		}
		string text3 = GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString();
		bool flag;
		switch (text3)
		{
		case "Normal":
		case "Difficult":
		case "Ultimate":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			text3 = "Normal";
		}
		return new XWModLevelIdentity("", text, text2, text3);
	}

	private static bool ReadLevelIdentity(Dictionary data, out XWModLevelIdentity identity)
	{
		return XWModLevelIdentity.TryParse(data.GetValueOrDefault("level_identity"), out identity);
	}

	private bool MatchesAcceptedLevel(Dictionary data)
	{
		if (ReadLevelIdentity(data, out var identity))
		{
			return identity == _acceptedLevelIdentity;
		}
		return false;
	}

	internal static bool ConfigMatchesIdentity(Dictionary data, XWModLevelIdentity identity)
	{
		Variant variant = Json.ParseString(data.GetValueOrDefault("config_json", "").AsString());
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		if (!variant.AsGodotDictionary().TryGetValue("Name", out var value) || value.VariantType != Variant.Type.String)
		{
			return false;
		}
		string text = value.AsString();
		if (!(text == identity.LevelSaveKey))
		{
			if (!identity.IsMod && text.Length == 0)
			{
				return identity.LevelSaveKey == "__session";
			}
			return false;
		}
		return true;
	}

	public bool IsBattleParticipant(string id)
	{
		if (_modBattleActive)
		{
			return _battleAdmitted.Contains(id);
		}
		return false;
	}

	private void ShowModCompatibilityFailure(string reason)
	{
		LastModCompatibilityFailure = ((reason.Length > 4096) ? (reason.Substring(0, 4096) + "…") : reason);
		GD.PushWarning("[ModCompatibility] " + LastModCompatibilityFailure);
		this.OnModCompatibilityFailure?.Invoke(LastModCompatibilityFailure);
		if (_modBattleActive)
		{
			BroadCastManager.Instance?.BroadCastFloatCreate(LastModCompatibilityFailure, Colors.Red);
		}
		else if (GodotObject.IsInstanceValid(DialogManager.Instance))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", LastModCompatibilityFailure.Replace("[", "[lb]"));
		}
	}

	public async Task<bool> StartCompatibleGameAsync()
	{
		if (!isHost || !CanSendMatchRpc() || _modBattleActive)
		{
			return false;
		}
		if (_preparingModStart)
		{
			_hostModBatch?.Cancel("本次开战已取消，请重新开始确认。");
			return false;
		}
		_preparingModStart = true;
		long session = _sessionGeneration;
		ModCompatibilityBatch batch = null;
		bool started = false;
		try
		{
			using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15L));
			XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.EnsureReadyAsync(timeout.Token);
			if (session != _sessionGeneration || !isHost || !CanSendMatchRpc())
			{
				return false;
			}
			_modEnvironmentLease = XWModEnvironmentService.TryAcquireEnvironment(xWModEnvironmentStatus.Generation);
			if (_modEnvironmentLease == null)
			{
				throw new InvalidOperationException("Mod 环境尚未就绪：" + xWModEnvironmentStatus.Reason);
			}
			batch = (_hostModBatch = new ModCompatibilityBatch(from id in matchMembers
				select id.AsString() into id
				where id != peerId
				select id, xWModEnvironmentStatus.Snapshot.Sha256, System.Environment.TickCount64, 15000, GetSelectedLevelIdentity().Canonical));
			_modBatches.Add(batch.Id, batch);
			_levelConfigAcked.Clear();
			string raw = BuildLevelConfigData();
			if (raw == "")
			{
				throw new InvalidOperationException("关卡配置不可用。");
			}
			Dictionary dictionary = Json.ParseString(raw).AsGodotDictionary();
			dictionary["config_batch_id"] = batch.Id;
			dictionary["mod_snapshot"] = XWModEnvironmentService.WriteSnapshot(xWModEnvironmentStatus.Snapshot);
			_activeBattleLevelConfigData = Json.Stringify(dictionary);
			CacheCompatibleLevelConfig(dictionary);
			foreach (string candidate in batch.Candidates)
			{
				SendMatchStateToPeer("level_config", _activeBattleLevelConfigData, candidate);
			}
			if (!(await batch.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15L))) || !batch.Accepted)
			{
				throw new InvalidOperationException(batch.Failure);
			}
			if (session != _sessionGeneration || !_modEnvironmentLease.IsCurrent || System.Environment.TickCount64 >= batch.Deadline || !batch.Candidates.SetEquals(from id in matchMembers
				select id.AsString() into id
				where id != peerId
				select id) || BuildLevelConfigData() != raw)
			{
				throw new InvalidOperationException("成员、关卡配置或 Mod 环境已变化，请重新确认。");
			}
			_acceptedModBatch = batch.Id;
			_modBattleId = batch.Id;
			_battleAdmitted.UnionWith(batch.Candidates);
			_battleAdmitted.Add(peerId);
			_modBattleActive = true;
			SendStartGame();
			TaskCompletionSource<bool> modSceneAccepted = _modSceneAccepted;
			bool flag = modSceneAccepted != null;
			if (flag)
			{
				flag = await modSceneAccepted.Task;
			}
			started = flag && session == _sessionGeneration && (_modEnvironmentLease?.IsCurrent ?? false);
			return started;
		}
		catch (Exception ex) when (((ex is TimeoutException || ex is OperationCanceledException || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			if (session == _sessionGeneration)
			{
				bool flag2 = ((ex is TimeoutException || ex is OperationCanceledException) ? true : false);
				ShowModCompatibilityFailure(flag2 ? "Mod 环境确认超时，未开始战斗。" : ex.Message);
			}
			return false;
		}
		finally
		{
			if (batch != null)
			{
				_modBatches.Remove(batch.Id);
				if (!started)
				{
					CancelRemoteModBatch(batch, (batch.Failure == "") ? "本次开战已取消。" : batch.Failure);
				}
			}
			if (session == _sessionGeneration)
			{
				_hostModBatch = null;
				_preparingModStart = false;
				if (!started)
				{
					ReleaseBattleCompatibility();
				}
			}
		}
	}

	private void CancelRemoteModBatch(ModCompatibilityBatch batch, string reason)
	{
		batch.Cancel(reason);
		Dictionary dictionary = new Dictionary
		{
			["config_batch_id"] = batch.Id,
			["cancelled"] = true,
			["reason"] = reason
		};
		foreach (string candidate in batch.Candidates)
		{
			if (matchMembers.Contains(candidate))
			{
				SendMatchStateToPeer("level_config", Json.Stringify(dictionary), candidate);
			}
		}
	}

	private void ReceiveModAck(string sender, string raw)
	{
		if (!isHost || !matchMembers.Contains(sender))
		{
			return;
		}
		Variant variant = Json.ParseString(raw);
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		string text = dictionary.GetValueOrDefault("config_batch_id", "").AsString();
		if (!_modBatches.TryGetValue(text, out var value))
		{
			return;
		}
		Variant value2;
		Variant value3;
		bool accepted = dictionary.TryGetValue("accepted", out value2) && value2.VariantType == Variant.Type.Bool && value2.AsBool() && dictionary.TryGetValue("differences", out value3) && value3.VariantType == Variant.Type.Array && value3.AsGodotArray().Count == 0;
		string reason = dictionary.GetValueOrDefault("reason", "快照或确认字段无效").AsString();
		if (ReadLevelIdentity(dictionary, out var identity) && value.Confirm(sender, text, accepted, dictionary.GetValueOrDefault("snapshot_sha256", "").AsString(), reason, System.Environment.TickCount64, identity.Canonical))
		{
			if (!_levelConfigAcked.Contains(sender))
			{
				_levelConfigAcked.Add(sender);
			}
			if (value.Accepted)
			{
				this.OnAllLevelConfigAcked?.Invoke();
			}
		}
	}

	private void ReceiveCompatibleLevelConfig(string raw)
	{
		if (isHost)
		{
			return;
		}
		Variant variant = Json.ParseString(raw);
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		if (dictionary.GetValueOrDefault("roster_only", false).AsBool())
		{
			if (_modBattleActive && dictionary.GetValueOrDefault("battle_id", "").AsString() == _modBattleId && MatchesAcceptedLevel(dictionary))
			{
				ReadAdmittedRoster(dictionary);
			}
			return;
		}
		string text = dictionary.GetValueOrDefault("config_batch_id", "").AsString();
		if (dictionary.GetValueOrDefault("cancelled", false).AsBool())
		{
			if (!_modBattleActive && text != "" && text == _pendingRemoteModBatch)
			{
				ReleaseBattleCompatibility();
				ShowModCompatibilityFailure(dictionary.GetValueOrDefault("reason", "本次开战已取消。").AsString());
			}
		}
		else if (!_modBattleActive)
		{
			ReleaseBattleCompatibility();
			_pendingRemoteModBatch = text;
			ValidateRemoteModConfigAsync(dictionary, ++_remoteModTicket, _sessionGeneration);
		}
	}

	private async Task ValidateRemoteModConfigAsync(Dictionary data, long ticket, long session)
	{
		string batch = data.GetValueOrDefault("config_batch_id", "").AsString();
		string reason = "Mod 配置字段缺失或格式无效。";
		Godot.Collections.Array differences = new Godot.Collections.Array();
		bool accepted = false;
		string digest = "";
		try
		{
			using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15L));
			XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.EnsureReadyAsync(timeout.Token);
			if (ticket != _remoteModTicket || session != _sessionGeneration)
			{
				return;
			}
			digest = xWModEnvironmentStatus.Snapshot?.Sha256 ?? "";
			string text = data.GetValueOrDefault("config_type", "").AsString();
			Guid result;
			bool flag = Guid.TryParseExact(batch, "N", out result);
			if (flag)
			{
				bool flag2 = ((text == "TowerDefenseLevelConfig" || text == "TowerDefenseLevelNewConfig") ? true : false);
				flag = flag2;
			}
			if (flag && ReadLevelIdentity(data, out var identity) && ConfigMatchesIdentity(data, identity) && data.TryGetValue("enter_level_mode", out var value) && value.VariantType == Variant.Type.String && identity.IsMod == (value.AsString() == "ModLevel") && data.TryGetValue("enter_level_is_battle", out var value2) && value2.VariantType == Variant.Type.Bool && Json.ParseString(data.GetValueOrDefault("config_json", "").AsString()).VariantType == Variant.Type.Dictionary && data.TryGetValue("mod_snapshot", out var value3) && value3.VariantType == Variant.Type.Dictionary && XWModEnvironmentService.TryReadSnapshot(value3.AsGodotDictionary(), out var snapshot, out reason))
			{
				if (identity.IsMod && (!XWModContentCatalog.TryResolve(identity, out var config, out reason) || config.GetType().Name != text))
				{
					reason = "Mod 关卡身份或配置类型不一致：" + reason;
				}
				else if (!xWModEnvironmentStatus.Ready || xWModEnvironmentStatus.Snapshot == null)
				{
					reason = "Mod 环境尚未就绪：" + xWModEnvironmentStatus.Reason;
				}
				else
				{
					IReadOnlyList<XWModSnapshotDifference> readOnlyList = XWModEnvironmentService.CompareSnapshots(snapshot, xWModEnvironmentStatus.Snapshot);
					foreach (XWModSnapshotDifference item in readOnlyList)
					{
						differences.Add(new Dictionary
						{
							["id"] = item.Id,
							["field"] = item.Field,
							["expected"] = item.Expected,
							["actual"] = item.Actual
						});
					}
					reason = XWModEnvironmentService.DescribeDifferences(readOnlyList);
					if (readOnlyList.Count == 0)
					{
						_modEnvironmentLease = XWModEnvironmentService.TryAcquireEnvironment(xWModEnvironmentStatus.Generation);
						accepted = _modEnvironmentLease != null;
						if (!accepted)
						{
							reason = "Mod 环境正在改变，请重试。";
						}
					}
				}
			}
			if (accepted)
			{
				_acceptedModBatch = batch;
				_remoteModDeadline = System.Environment.TickCount64 + 30000;
				CacheCompatibleLevelConfig(data);
			}
		}
		catch (Exception ex)
		{
			reason = "Mod 环境检查失败：" + ex.Message;
		}
		if (ticket == _remoteModTicket && session == _sessionGeneration)
		{
			Dictionary dictionary = new Dictionary
			{
				["config_batch_id"] = batch,
				["accepted"] = accepted,
				["snapshot_sha256"] = digest,
				["level_identity"] = data.GetValueOrDefault("level_identity"),
				["differences"] = differences,
				["reason"] = reason
			};
			SendMatchState("level_config_ack", Json.Stringify(dictionary));
			if (!accepted)
			{
				ShowModCompatibilityFailure(reason);
			}
		}
	}

	private void CacheCompatibleLevelConfig(Dictionary data)
	{
		if (!ReadLevelIdentity(data, out _acceptedLevelIdentity))
		{
			throw new InvalidOperationException("关卡身份缺失或格式无效。");
		}
		if (!ConfigMatchesIdentity(data, _acceptedLevelIdentity))
		{
			throw new InvalidOperationException("关卡身份与实际配置名称不一致。");
		}
		_receivedLevelConfigType = data.GetValueOrDefault("config_type", "").AsString();
		_receivedLevelConfigJson = data.GetValueOrDefault("config_json", "").AsString();
		_receivedLevelEnterMode = data.GetValueOrDefault("enter_level_mode", "").AsString();
		_receivedLevelIsBattle = data.GetValueOrDefault("enter_level_is_battle", false).AsBool();
	}

	private bool AcceptCompatibleStart(string raw)
	{
		Variant variant = Json.ParseString(raw);
		if (variant.VariantType == Variant.Type.Dictionary)
		{
			XWModEnvironmentService.EnvironmentLease modEnvironmentLease = _modEnvironmentLease;
			if (modEnvironmentLease != null && modEnvironmentLease.IsCurrent)
			{
				Dictionary dictionary = variant.AsGodotDictionary();
				if (_acceptedModBatch == "" || dictionary.GetValueOrDefault("config_batch_id", "").AsString() != _acceptedModBatch || !MatchesAcceptedLevel(dictionary) || dictionary.GetValueOrDefault("level_id", "").AsString() != _acceptedLevelIdentity.LevelSaveKey || dictionary.GetValueOrDefault("difficult", "").AsString() != _acceptedLevelIdentity.Difficulty || dictionary.GetValueOrDefault("snapshot_sha256", "").AsString() != _modEnvironmentLease.Status.Snapshot.Sha256 || (!isHost && System.Environment.TickCount64 >= _remoteModDeadline))
				{
					return false;
				}
				if (!ReadAdmittedRoster(dictionary) || !_battleAdmitted.Contains(peerId))
				{
					return false;
				}
				_modBattleId = dictionary.GetValueOrDefault("battle_id", "").AsString();
				if (_modBattleId == "")
				{
					return false;
				}
				_modBattleActive = true;
				_battleSceneDeadline = System.Environment.TickCount64 + 60000;
				return true;
			}
		}
		return false;
	}

	private bool ReadAdmittedRoster(Dictionary data)
	{
		if (!data.TryGetValue("admitted_peers", out var value) || value.VariantType != Variant.Type.Array)
		{
			return false;
		}
		string[] array = (from variant in value.AsGodotArray()
			select variant.AsString()).ToArray();
		if (array.Length > 4 || !array.Contains("1") || array.Distinct().Count() != array.Length || array.Any((string id) => !matchMembers.Contains(id)))
		{
			return false;
		}
		_battleAdmitted.Clear();
		_battleAdmitted.UnionWith(array);
		return true;
	}

	private async Task SendCompatibleLateJoinAsync(string peer)
	{
		if (!_modBattleActive)
		{
			return;
		}
		XWModEnvironmentService.EnvironmentLease modEnvironmentLease = _modEnvironmentLease;
		if (modEnvironmentLease == null || !modEnvironmentLease.IsCurrent || _activeBattleLevelConfigData == "")
		{
			return;
		}
		long session = _sessionGeneration;
		ModCompatibilityBatch batch = new ModCompatibilityBatch(new string[1] { peer }, _modEnvironmentLease.Status.Snapshot.Sha256, System.Environment.TickCount64, 15000, _acceptedLevelIdentity.Canonical);
		_modBatches.Add(batch.Id, batch);
		bool joined = false;
		try
		{
			Dictionary dictionary = Json.ParseString(_activeBattleLevelConfigData).AsGodotDictionary();
			dictionary["config_batch_id"] = batch.Id;
			SendMatchStateToPeer("level_config", Json.Stringify(dictionary), peer);
			if (!(await batch.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15L))) || !batch.Accepted)
			{
				throw new InvalidOperationException(batch.Failure);
			}
			if (session != _sessionGeneration || !_modBattleActive)
			{
				return;
			}
			XWModEnvironmentService.EnvironmentLease modEnvironmentLease2 = _modEnvironmentLease;
			if (modEnvironmentLease2 == null || !modEnvironmentLease2.IsCurrent || !matchMembers.Contains(peer) || System.Environment.TickCount64 >= batch.Deadline)
			{
				return;
			}
			_battleAdmitted.Add(peer);
			Dictionary dictionary2 = Json.ParseString(_activeBattleStartData).AsGodotDictionary();
			dictionary2["config_batch_id"] = batch.Id;
			dictionary2["admitted_peers"] = BattleAdmittedPeers;
			Dictionary dictionary3 = new Dictionary
			{
				["roster_only"] = true,
				["battle_id"] = _modBattleId,
				["level_identity"] = _acceptedLevelIdentity.ToDictionary(),
				["admitted_peers"] = BattleAdmittedPeers
			};
			foreach (string item in _battleAdmitted.Where((string id) => id != peerId && id != peer))
			{
				SendMatchStateToPeer("level_config", Json.Stringify(dictionary3), item);
			}
			SendMatchStateToPeer("start_game", Json.Stringify(dictionary2), peer);
			joined = true;
		}
		catch (Exception ex) when (((ex is TimeoutException || ex is InvalidOperationException) ? 1 : 0) != 0)
		{
			if (session == _sessionGeneration)
			{
				ShowModCompatibilityFailure("成员 " + peer + " 未进入战斗：" + ex.Message);
			}
		}
		finally
		{
			_modBatches.Remove(batch.Id);
			if (!joined && session == _sessionGeneration)
			{
				CancelRemoteModBatch(batch, (batch.Failure == "") ? "晚加入确认已取消或超时。" : batch.Failure);
			}
		}
	}

	private void ProcessModCompatibility()
	{
		if (_modBattleActive && _battleSceneDeadline != 0L)
		{
			TowerDefenseControlNew towerDefenseControlNew = TowerDefenseManager.Instance?.currentControl;
			if (GodotObject.IsInstanceValid(towerDefenseControlNew) && towerDefenseControlNew.IsInsideTree())
			{
				_battleSceneDeadline = 0L;
			}
			else if (System.Environment.TickCount64 >= _battleSceneDeadline)
			{
				_CleanupConnection();
				SceneManager.Instance?.ChangeScene("MainMenu");
				ShowModCompatibilityFailure("联机场景加载超时，会话已结束。");
				return;
			}
		}
		if (_modEnvironmentLease != null && !_modEnvironmentLease.IsCurrent)
		{
			if (_modBattleActive)
			{
				_CleanupConnection();
				SceneManager.Instance?.ChangeScene("MainMenu");
				ShowModCompatibilityFailure("联机 Mod 环境意外变化，已终止本次会话。");
			}
			else
			{
				_hostModBatch?.Cancel("Mod 环境变化，请重新确认。");
				if (!isHost)
				{
					ReleaseBattleCompatibility();
				}
			}
		}
		if (!isHost && !_modBattleActive && _acceptedModBatch != "" && System.Environment.TickCount64 >= _remoteModDeadline)
		{
			ReleaseBattleCompatibility();
			ShowModCompatibilityFailure("等待开战超时，请由主机重新发起确认。");
		}
	}

	private void ModPeerChanged(string peer, bool disconnected)
	{
		_hostModBatch?.Cancel("房间成员变化，请重新确认。");
		if (!disconnected)
		{
			return;
		}
		_battleAdmitted.Remove(peer);
		ModCompatibilityBatch[] array = _modBatches.Values.ToArray();
		foreach (ModCompatibilityBatch modCompatibilityBatch in array)
		{
			if (modCompatibilityBatch.Candidates.Contains(peer))
			{
				modCompatibilityBatch.Cancel("成员已断线。");
			}
		}
	}

	public void ReleaseBattleCompatibility()
	{
		_modSceneAccepted?.TrySetResult(result: false);
		_modSceneAccepted = null;
		_remoteModTicket++;
		ModCompatibilityBatch[] array = _modBatches.Values.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Cancel("会话或战斗已结束。");
		}
		_modBatches.Clear();
		_modEnvironmentLease?.Dispose();
		_modEnvironmentLease = null;
		_battleAdmitted.Clear();
		_acceptedModBatch = (_pendingRemoteModBatch = (_modBattleId = ""));
		_acceptedLevelIdentity = null;
		_modBattleActive = false;
		_battleSceneDeadline = 0L;
		_activeBattleStartData = (_activeBattleLevelConfigData = "");
		_startGameTransitionPending = (_startGameTransitionCompleted = false);
	}

	private bool AllowsModMessage(NetMessageType type, string authenticatedSender)
	{
		if ((type == NetMessageType.StartGame || (uint)(type - 25) <= 1u || type == NetMessageType.LevelConfigAck) ? true : false)
		{
			return true;
		}
		if (!isHost)
		{
			return IsBattleParticipant(peerId);
		}
		if (!(authenticatedSender == peerId))
		{
			return IsBattleParticipant(authenticatedSender);
		}
		return true;
	}

	public MultiPlayerManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/MultiPlayerManager");
	}
}
