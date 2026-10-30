using Godot;

[GlobalClass]
public partial class RemoteCursor : Node2D
{
	private static PackedScene _packetShowScene;

	private static Texture2D _gloveTexture;

	private static readonly Color[] PlayerColors = new Color[4]
	{
		new Color(0.3f, 0.7f, 1f),
		new Color(1f, 0.5f, 0.3f),
		new Color(0.5f, 1f, 0.5f),
		new Color(1f, 1f, 0.3f)
	};

	private Sprite2D _cursorSprite;

	private RichTextLabel _label;

	private CanvasItem _pickNode;

	private string _currentPickType = "";

	private string _currentPickName = "";

	private Vector2 _targetPosition = new Vector2(-100f, -100f);

	private Vector2 _currentPosition = new Vector2(-100f, -100f);

	private static readonly Vector2 PickOffset = new Vector2(0f, -30f);

	public object PacketConfig => null;

	public override void _Ready()
	{
		_cursorSprite = GetNode<Sprite2D>("%CursorSprite");
		_label = GetNode<RichTextLabel>("%Label");
		_label.BbcodeEnabled = true;
		_label.AddThemeFontSizeOverride("normal_font_size", 14);
		_label.AddThemeColorOverride("font_outline_color", Colors.Black);
		_label.AddThemeConstantOverride("outline_size", 3);
	}

	public void SetPlayerInfo(int playerIndex, string playerName = "")
	{
		Color modulate = PlayerColors[playerIndex % PlayerColors.Length];
		_cursorSprite.Modulate = modulate;
		string text = ((playerName != "") ? playerName : $"P{playerIndex + 1}");
		string value = modulate.ToHtml(includeAlpha: false);
		_label.Text = $"[color=#{value}]{EscapeBbcode(text)}[/color]";
		_label.Visible = true;
	}

	public void UpdatePosition(float posX, float posY)
	{
		_targetPosition = new Vector2(posX, posY);
		_cursorSprite.Visible = true;
	}

	public void UpdatePick(string pickType, string pickName)
	{
		if (!(pickType == _currentPickType) || !(pickName == _currentPickName))
		{
			_currentPickType = pickType;
			_currentPickName = pickName;
			_UpdatePickSprite();
		}
	}

	private void _UpdatePickSprite()
	{
		if (GodotObject.IsInstanceValid(_pickNode))
		{
			_pickNode.QueueFree();
			_pickNode = null;
		}
		if (_currentPickType == "" || _currentPickName == "")
		{
			return;
		}
		switch (_currentPickType)
		{
		case "plant":
		{
			TowerDefensePacketConfig towerDefensePacketConfig = TowerDefenseManager.GetPacketConfigReadOnly(_currentPickName);
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				towerDefensePacketConfig = TowerDefenseManager.GetPacketConfigReadOnlyByCharacterName(_currentPickName);
			}
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				return;
			}
			if (_packetShowScene == null)
			{
				_packetShowScene = GD.Load<PackedScene>("uid://bhqecss20rwpb");
			}
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = (TowerDefenseInGamePacketShow)_packetShowScene.Instantiate(PackedScene.GenEditState.Disabled);
			towerDefenseInGamePacketShow.Modulate = new Color(towerDefenseInGamePacketShow.Modulate, 0.7f);
			towerDefenseInGamePacketShow.ZIndex = 1900;
			towerDefenseInGamePacketShow.ZAsRelative = false;
			towerDefenseInGamePacketShow.MouseFilter = Control.MouseFilterEnum.Ignore;
			_pickNode = towerDefenseInGamePacketShow;
			AddChild(_pickNode, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(towerDefensePacketConfig);
			towerDefenseInGamePacketShow.button.MouseFilter = Control.MouseFilterEnum.Ignore;
			break;
		}
		case "shovel":
		{
			ShovelConfig shovel = TowerDefenseManager.GetShovel(_currentPickName);
			if (GodotObject.IsInstanceValid(shovel) && GodotObject.IsInstanceValid(shovel.texture))
			{
				Sprite2D sprite2D2 = new Sprite2D();
				sprite2D2.Texture = shovel.texture;
				sprite2D2.Modulate = new Color(sprite2D2.Modulate, 0.5f);
				sprite2D2.ZIndex = 1900;
				sprite2D2.ZAsRelative = false;
				sprite2D2.Scale = Vector2.One * 80f / shovel.texture.GetWidth();
				_pickNode = sprite2D2;
				AddChild(_pickNode, forceReadableName: false, InternalMode.Disabled);
			}
			break;
		}
		case "glove":
			if (_gloveTexture == null)
			{
				_gloveTexture = GD.Load<Texture2D>("res://Asset/Texture/GUI/General/Glove/Glove.png");
			}
			if (GodotObject.IsInstanceValid(_gloveTexture))
			{
				Sprite2D sprite2D = new Sprite2D();
				sprite2D.Texture = _gloveTexture;
				sprite2D.Modulate = new Color(sprite2D.Modulate, 0.5f);
				sprite2D.ZIndex = 1900;
				sprite2D.ZAsRelative = false;
				sprite2D.Scale = Vector2.One * 80f / _gloveTexture.GetWidth();
				_pickNode = sprite2D;
				AddChild(_pickNode, forceReadableName: false, InternalMode.Disabled);
			}
			break;
		}
		UpdatePickNodePosition();
	}

	private void UpdatePickNodePosition()
	{
		if (GodotObject.IsInstanceValid(_pickNode))
		{
			Vector2 position = _currentPosition + PickOffset;
			if (_pickNode is Control control)
			{
				control.Position = position;
			}
			else if (_pickNode is Node2D node2D)
			{
				node2D.Position = position;
			}
		}
	}

	private static string EscapeBbcode(string text)
	{
		return (text ?? "").Replace("[", "[lb]");
	}

	public override void _Process(double delta)
	{
		if (_cursorSprite.Visible)
		{
			_currentPosition = _currentPosition.Lerp(_targetPosition, Mathf.Min(1f, (float)delta * 20f));
			_cursorSprite.Position = _currentPosition;
			_label.Position = _currentPosition + new Vector2(16f, -8f);
			UpdatePickNodePosition();
		}
	}

	public void _Hide()
	{
		_cursorSprite.Visible = false;
		_label.Visible = false;
		if (GodotObject.IsInstanceValid(_pickNode))
		{
			_pickNode.Visible = false;
		}
	}
}
