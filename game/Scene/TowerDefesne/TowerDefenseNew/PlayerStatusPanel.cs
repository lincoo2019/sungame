using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PlayerStatusPanel : Control
{
	private static readonly Color[] PlayerColors = new Color[4]
	{
		new Color(0.3f, 0.7f, 1f),
		new Color(1f, 0.5f, 0.3f),
		new Color(0.5f, 1f, 0.5f),
		new Color(1f, 1f, 0.3f)
	};

	private const string SignalBarFull = "■";

	private const string SignalBarEmpty = "□";

	private VBoxContainer _playerList;

	private readonly System.Collections.Generic.Dictionary<string, HBoxContainer> _playerRows = new System.Collections.Generic.Dictionary<string, HBoxContainer>();

	private double _refreshTimer;

	private const double RefreshInterval = 0.5;

	private readonly StringBuilder _signalTextBuilder = new StringBuilder(16);

	public override void _Ready()
	{
		_playerList = GetNode<VBoxContainer>("%PlayerList");
		base.Visible = false;
		if (MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnPingUpdated += _OnPingUpdated;
			MultiPlayerManager.Instance.OnPeerJoined += _OnPeerChanged;
			MultiPlayerManager.Instance.OnPeerLeft += _OnPeerLeft;
			MultiPlayerManager.Instance.OnMatchLeft += _OnMatchLeft;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnPingUpdated -= _OnPingUpdated;
			MultiPlayerManager.Instance.OnPeerJoined -= _OnPeerChanged;
			MultiPlayerManager.Instance.OnPeerLeft -= _OnPeerLeft;
			MultiPlayerManager.Instance.OnMatchLeft -= _OnMatchLeft;
		}
	}

	public override void _Process(double delta)
	{
		if (base.Visible)
		{
			_refreshTimer += delta;
			if (_refreshTimer >= 0.5)
			{
				_refreshTimer = 0.0;
				_RefreshAllRows();
			}
		}
	}

	public void ShowPanel()
	{
		base.Visible = true;
		_refreshTimer = 0.0;
		_RebuildRows();
	}

	public void HidePanel()
	{
		base.Visible = false;
	}

	private void _RebuildRows()
	{
		foreach (Node child in _playerList.GetChildren())
		{
			child.QueueFree();
		}
		_playerRows.Clear();
		if (MultiPlayerManager.Instance != null && Global.IsMultiplayerMode)
		{
			Array array = MultiPlayerManager.Instance.matchMembers.Duplicate();
			if (!array.Contains(MultiPlayerManager.Instance.peerId))
			{
				array.Insert(0, MultiPlayerManager.Instance.peerId);
			}
			for (int i = 0; i < array.Count; i++)
			{
				string text = (string)array[i];
				HBoxContainer hBoxContainer = _CreatePlayerRow(text, i);
				_playerList.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
				_playerRows[text] = hBoxContainer;
			}
		}
	}

	private HBoxContainer _CreatePlayerRow(string peerIdStr, int index)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.CustomMinimumSize = new Vector2(340f, 28f);
		hBoxContainer.MouseFilter = MouseFilterEnum.Ignore;
		ColorRect colorRect = new ColorRect();
		colorRect.CustomMinimumSize = new Vector2(4f, 20f);
		colorRect.Color = PlayerColors[index % PlayerColors.Length];
		colorRect.MouseFilter = MouseFilterEnum.Ignore;
		hBoxContainer.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		Label label = new Label();
		string text = ((MultiPlayerManager.Instance != null) ? MultiPlayerManager.Instance.GetPeerName(peerIdStr) : "Player");
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 16);
		label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.9f));
		label.CustomMinimumSize = new Vector2(120f, 0f);
		label.MouseFilter = MouseFilterEnum.Ignore;
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Label label2 = new Label();
		label2.AddThemeFontSizeOverride("font_size", 16);
		label2.CustomMinimumSize = new Vector2(110f, 0f);
		label2.MouseFilter = MouseFilterEnum.Ignore;
		hBoxContainer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		Label label3 = new Label();
		label3.AddThemeFontSizeOverride("font_size", 16);
		label3.CustomMinimumSize = new Vector2(80f, 0f);
		label3.HorizontalAlignment = HorizontalAlignment.Right;
		label3.MouseFilter = MouseFilterEnum.Ignore;
		hBoxContainer.AddChild(label3, forceReadableName: false, InternalMode.Disabled);
		_UpdateRowData(hBoxContainer, peerIdStr);
		return hBoxContainer;
	}

	private void _UpdateRowData(HBoxContainer row, string peerIdStr)
	{
		Array<Node> children = row.GetChildren();
		if (children.Count < 4)
		{
			return;
		}
		Label label = (Label)children[2];
		Label label2 = (Label)children[3];
		if (MultiPlayerManager.Instance == null)
		{
			return;
		}
		int peerLatency = MultiPlayerManager.Instance.GetPeerLatency(peerIdStr);
		int signalLevel = MultiPlayerManager.Instance.GetSignalLevel(peerIdStr);
		_signalTextBuilder.Clear();
		for (int i = 0; i < 5; i++)
		{
			_signalTextBuilder.Append((i < signalLevel) ? "■" : "□");
		}
		_signalTextBuilder.Append(' ');
		Color color;
		switch (signalLevel)
		{
		case 5:
			color = new Color(0.3f, 1f, 0.3f);
			_signalTextBuilder.Append("极好");
			break;
		case 4:
			color = new Color(0.5f, 1f, 0.5f);
			_signalTextBuilder.Append("良好");
			break;
		case 3:
			color = new Color(1f, 1f, 0.3f);
			_signalTextBuilder.Append("一般");
			break;
		case 2:
			color = new Color(1f, 0.6f, 0.2f);
			_signalTextBuilder.Append("较差");
			break;
		case 1:
			color = new Color(1f, 0.3f, 0.3f);
			_signalTextBuilder.Append("极差");
			break;
		default:
			color = new Color(0.5f, 0.5f, 0.5f);
			_signalTextBuilder.Append("无信号");
			break;
		}
		label.Text = _signalTextBuilder.ToString();
		label.AddThemeColorOverride("font_color", color);
		if (peerLatency > 0)
		{
			label2.Text = peerLatency + " ms";
			if (peerLatency <= 50)
			{
				label2.AddThemeColorOverride("font_color", new Color(0.3f, 1f, 0.3f));
			}
			else if (peerLatency <= 100)
			{
				label2.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
			}
			else if (peerLatency <= 200)
			{
				label2.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.3f));
			}
			else if (peerLatency <= 400)
			{
				label2.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.2f));
			}
			else
			{
				label2.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
			}
		}
		else
		{
			label2.Text = "--- ms";
			label2.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
		}
	}

	private void _RefreshAllRows()
	{
		foreach (var (peerIdStr, hBoxContainer2) in _playerRows)
		{
			if (GodotObject.IsInstanceValid(hBoxContainer2))
			{
				_UpdateRowData(hBoxContainer2, peerIdStr);
			}
		}
	}

	private void _OnPingUpdated(string peerId, int latencyMs)
	{
		if (base.Visible && _playerRows.TryGetValue(peerId, out var value) && GodotObject.IsInstanceValid(value))
		{
			_UpdateRowData(value, peerId);
		}
	}

	private void _OnPeerChanged(string username)
	{
		if (base.Visible)
		{
			_RebuildRows();
		}
	}

	private void _OnPeerLeft(string username, string peerId)
	{
		if (base.Visible)
		{
			_RebuildRows();
		}
	}

	private void _OnMatchLeft()
	{
		base.Visible = false;
		foreach (Node child in _playerList.GetChildren())
		{
			child.QueueFree();
		}
		_playerRows.Clear();
	}
}
