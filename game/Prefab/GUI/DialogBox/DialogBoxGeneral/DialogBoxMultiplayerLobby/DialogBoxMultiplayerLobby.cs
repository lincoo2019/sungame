using Godot;

[GlobalClass]
public partial class DialogBoxMultiplayerLobby : DialogPopup
{
	public enum STATE
	{
		SelectMode,
		InputIp,
		InRoom
	}

	public RichTextLabel statusLabel;

	public RichTextLabel roomIdLabel;

	public RichTextLabel peerListLabel;

	public TextureButton createButton;

	public TextureButton closeButton;

	public LineEdit roomIdInput;

	public TextureButton confirmJoinButton;

	public HBoxContainer hbox;

	public HBoxContainer hbox2;

	public HBoxContainer hbox3;

	public STATE _state;

	public override void _Ready()
	{
		base._Ready();
		statusLabel = GetNode<RichTextLabel>("%StatusLabel");
		roomIdLabel = GetNode<RichTextLabel>("%RoomIdLabel");
		peerListLabel = GetNode<RichTextLabel>("%PeerListLabel");
		createButton = GetNode<TextureButton>("%CreateButton");
		closeButton = GetNode<TextureButton>("%CloseButton");
		roomIdInput = GetNode<LineEdit>("%RoomIdInput");
		confirmJoinButton = GetNode<TextureButton>("%ConfirmJoinButton");
		hbox = GetNode<HBoxContainer>("Layer/Control/HBox");
		hbox2 = GetNode<HBoxContainer>("Layer/Control/HBox2");
		hbox3 = GetNode<HBoxContainer>("Layer/Control/HBox3");
		GetNode<BaseButton>("%CreateButton").Pressed += _OnCreateButtonPressed;
		GetNode<BaseButton>("%JoinButton").Pressed += _OnJoinButtonPressed;
		GetNode<BaseButton>("%SelectCloseButton").Pressed += base.CloseDialog;
		GetNode<BaseButton>("%LeaveButton").Pressed += _OnLeaveButtonPressed;
		GetNode<BaseButton>("%CloseButton").Pressed += base.CloseDialog;
		GetNode<BaseButton>("%ConfirmJoinButton").Pressed += _OnConfirmJoinButtonPressed;
		GetNode<BaseButton>("%CancelButton").Pressed += _OnCancelJoinButtonPressed;
		MultiPlayerManager.Instance.OnMatchCreated += _OnMatchCreated;
		MultiPlayerManager.Instance.OnMatchJoined += _OnMatchJoined;
		MultiPlayerManager.Instance.OnMatchLeft += _OnMatchLeft;
		MultiPlayerManager.Instance.OnPeerJoined += _OnPeerJoined;
		MultiPlayerManager.Instance.OnPeerLeft += _OnPeerLeft;
		if (MultiPlayerManager.Instance.currentMatchId != "")
		{
			_state = STATE.InRoom;
		}
		_UpdateUiState();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnMatchCreated -= _OnMatchCreated;
			MultiPlayerManager.Instance.OnMatchJoined -= _OnMatchJoined;
			MultiPlayerManager.Instance.OnMatchLeft -= _OnMatchLeft;
			MultiPlayerManager.Instance.OnPeerJoined -= _OnPeerJoined;
			MultiPlayerManager.Instance.OnPeerLeft -= _OnPeerLeft;
		}
	}

	public void _SetState(STATE newState)
	{
		_state = newState;
		_UpdateUiState();
	}

	public void _UpdateUiState()
	{
		hbox.Visible = _state == STATE.SelectMode;
		hbox3.Visible = _state == STATE.InputIp;
		hbox2.Visible = _state == STATE.InRoom;
		roomIdInput.Visible = _state == STATE.InputIp;
		roomIdLabel.Visible = _state == STATE.InRoom;
		peerListLabel.Visible = _state == STATE.InRoom;
		switch (_state)
		{
		case STATE.SelectMode:
			statusLabel.Clear();
			statusLabel.AppendText("[center]创建或加入房间开始联机[/center]");
			break;
		case STATE.InputIp:
			statusLabel.Clear();
			statusLabel.AppendText("[center]输入房主的IP地址[/center]");
			break;
		case STATE.InRoom:
			if (MultiPlayerManager.Instance.isHost)
			{
				statusLabel.Clear();
				statusLabel.AppendText("[center]你是房主 — 关闭此窗口后自由选关[/center]");
				roomIdLabel.Clear();
				string[] allLanIps = MultiPlayerManager.Instance.GetAllLanIps();
				if (allLanIps.Length == 0)
				{
					roomIdLabel.AppendText("[center]未检测到可用IP[/center]");
				}
				else
				{
					string text = "";
					int currentPort = MultiPlayerManager.Instance.CurrentPort;
					string[] array = allLanIps;
					foreach (string arg in array)
					{
						if (text != "")
						{
							text += "\n";
						}
						text += $"{arg}:{currentPort}";
					}
					roomIdLabel.AppendText($"[center]{text}[/center]");
				}
				closeButton.Visible = true;
			}
			else
			{
				statusLabel.Clear();
				statusLabel.AppendText("[center]已加入房间 — 等待房主选关[/center]");
				roomIdLabel.Clear();
				roomIdLabel.AppendText($"[center]主机: {MultiPlayerManager.Instance.currentMatchId}[/center]");
				closeButton.Visible = false;
			}
			peerListLabel.Clear();
			peerListLabel.AppendText($"[center]在线人数: {MultiPlayerManager.Instance.matchMembers.Count}[/center]");
			break;
		}
	}

	public void _OnCreateButtonPressed()
	{
		createButton.Disabled = true;
		MultiPlayerManager.Instance.CreateMatch();
		createButton.Disabled = false;
		if (MultiPlayerManager.Instance.currentMatchId != "")
		{
			_SetState(STATE.InRoom);
		}
	}

	public void _OnJoinButtonPressed()
	{
		_SetState(STATE.InputIp);
	}

	public void _OnConfirmJoinButtonPressed()
	{
		string text = roomIdInput.Text.StripEdges();
		if (text == "")
		{
			BroadCastManager.Instance.BroadCastFloatCreate("请输入IP地址", Colors.Red);
			return;
		}
		confirmJoinButton.Disabled = true;
		MultiPlayerManager.Instance.JoinMatch(text);
		confirmJoinButton.Disabled = false;
		if (MultiPlayerManager.Instance.currentMatchId != "")
		{
			_SetState(STATE.InRoom);
		}
	}

	public void _OnCancelJoinButtonPressed()
	{
		_SetState(STATE.SelectMode);
	}

	public void _OnLeaveButtonPressed()
	{
		MultiPlayerManager.Instance.LeaveMatch();
		_SetState(STATE.SelectMode);
	}

	public void _OnMatchCreated(string matchId)
	{
		_SetState(STATE.InRoom);
	}

	public void _OnMatchJoined(string matchId)
	{
		_SetState(STATE.InRoom);
	}

	public void _OnMatchLeft()
	{
		_SetState(STATE.SelectMode);
	}

	public void _OnPeerJoined(string username)
	{
		_UpdateUiState();
		BroadCastManager.Instance.BroadCastFloatCreate($"{username} 加入了房间", Colors.Green);
	}

	public void _OnPeerLeft(string username, string peerId)
	{
		_UpdateUiState();
		BroadCastManager.Instance.BroadCastFloatCreate($"{username} 离开了房间", Colors.Red);
	}
}
