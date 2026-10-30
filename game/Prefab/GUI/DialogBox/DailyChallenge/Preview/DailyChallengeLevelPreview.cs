using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DailyChallengeLevelPreview : Control
{
	public const float DragDeadzone = 20f;

	public const int MaxVisibleTags = 8;

	private static Texture2D _izIcon;

	private static Texture2D _iz2Icon;

	private static Texture2D _vrIcon;

	private static Texture2D _endlessIcon;

	private static Texture2D _survivalIcon;

	private static Texture2D _luckyIcon;

	private Label _nameLabel;

	private Label _typeLabel;

	private Label _authorLabel;

	private Label _difficultyLabel;

	private Control _difficultyBadge;

	private Label _rewardLabel;

	private Label _unavailableLabel;

	private TextureRect _mapTexture;

	private TextureRect _finishTexture;

	private HBoxContainer _iconBox;

	private HFlowContainer _tagFlow;

	private PanelContainer _tagChipTemplate;

	private Control _selectionCorners;

	private bool _interactionEnabled = true;

	private bool _pointerPressed;

	private bool _pointerDragged;

	private int _touchIndex = -1;

	private Vector2 _pressPosition;

	private int _pressScrollOffset;

	private static Texture2D IZ_ICON => _izIcon ?? (_izIcon = GD.Load<Texture2D>("uid://ciyj1718ypbih"));

	private static Texture2D IZ2_ICON => _iz2Icon ?? (_iz2Icon = GD.Load<Texture2D>("uid://b8rsot26eb7od"));

	private static Texture2D VR_ICON => _vrIcon ?? (_vrIcon = GD.Load<Texture2D>("uid://24j6iw1ww08b"));

	private static Texture2D ENDLESS_ICON => _endlessIcon ?? (_endlessIcon = GD.Load<Texture2D>("uid://bcjx34t665il2"));

	private static Texture2D SURVIVAL_ICON => _survivalIcon ?? (_survivalIcon = GD.Load<Texture2D>("uid://crvtrh32bqu0r"));

	private static Texture2D LUCKY_ICON => _luckyIcon ?? (_luckyIcon = GD.Load<Texture2D>("uid://caqbmeu30cbsa"));

	public string LevelId { get; private set; } = "";

	public string ApiPath { get; private set; } = "";

	public string DisplayName { get; private set; } = "";

	public Dictionary Metadata { get; private set; } = new Dictionary();

	public bool IsAvailable { get; private set; }

	public bool IsFinished { get; private set; }

	public bool IsSelected { get; private set; }

	public bool IsInteractionEnabled
	{
		get
		{
			if (_interactionEnabled)
			{
				return IsAvailable;
			}
			return false;
		}
	}

	public event Action<DailyChallengeLevelPreview> OnSelected;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_typeLabel = GetNode<Label>("%TypeLabel");
		_authorLabel = GetNode<Label>("%AuthorLabel");
		_difficultyLabel = GetNode<Label>("%DifficultyLabel");
		_difficultyBadge = GetNode<Control>("%DifficultyBadge");
		_rewardLabel = GetNode<Label>("%RewardLabel");
		_unavailableLabel = GetNode<Label>("%UnavailableLabel");
		_mapTexture = GetNode<TextureRect>("%MapTexture");
		_finishTexture = GetNode<TextureRect>("%FinishTexture");
		_iconBox = GetNode<HBoxContainer>("%IconBox");
		_tagFlow = GetNode<HFlowContainer>("%TagFlow");
		_tagChipTemplate = _tagFlow.GetNode<PanelContainer>("TagChipTemplate");
		_selectionCorners = GetNode<Control>("%SelectionCorners");
		base.GuiInput += HandleCardInput;
		RefreshVisuals();
	}

	public override void _Notification(int what)
	{
		if ((long)what == 47)
		{
			ResetPointerGesture();
		}
	}

	public void Bind(string levelId, Dictionary metadata, bool finished)
	{
		LevelId = levelId ?? "";
		Metadata = metadata?.Duplicate(deep: true) ?? new Dictionary();
		ApiPath = Metadata.GetValueOrDefault("api", "").AsString().Trim();
		DisplayName = Metadata.GetValueOrDefault("name", LevelId).AsString();
		if (string.IsNullOrWhiteSpace(DisplayName))
		{
			DisplayName = LevelId;
		}
		IsFinished = finished;
		IsAvailable = !string.IsNullOrWhiteSpace(LevelId) && IsValidApiPath(ApiPath);
		if (IsNodeReady())
		{
			RefreshVisuals();
		}
	}

	public static bool IsValidApiPath(string apiPath)
	{
		if (string.IsNullOrWhiteSpace(apiPath) || !apiPath.StartsWith("/", StringComparison.Ordinal) || apiPath.StartsWith("//", StringComparison.Ordinal))
		{
			return false;
		}
		for (int i = 0; i < apiPath.Length; i++)
		{
			if (char.IsControl(apiPath[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void SetSelected(bool selected)
	{
		IsSelected = selected;
		if (GodotObject.IsInstanceValid(_selectionCorners))
		{
			_selectionCorners.Visible = selected;
		}
	}

	public void SetInteractionEnabled(bool enabled)
	{
		_interactionEnabled = enabled;
		base.MouseFilter = (MouseFilterEnum)(IsInteractionEnabled ? 1 : 2);
		base.MouseDefaultCursorShape = (CursorShape)(IsInteractionEnabled ? 2 : 0);
		base.Modulate = ((!IsAvailable) ? new Color(0.55f, 0.55f, 0.55f) : (enabled ? Colors.White : new Color(0.78f, 0.78f, 0.78f)));
		if (!IsInteractionEnabled)
		{
			ResetPointerGesture();
		}
	}

	public void HandleCardInput(InputEvent inputEvent)
	{
		if (!IsInteractionEnabled || inputEvent == null)
		{
			return;
		}
		if (!(inputEvent is InputEventMouseButton inputEventMouseButton))
		{
			if (!(inputEvent is InputEventMouseMotion inputEventMouseMotion))
			{
				if (!(inputEvent is InputEventScreenTouch inputEventScreenTouch))
				{
					if (inputEvent is InputEventScreenDrag inputEventScreenDrag && inputEventScreenDrag.Index == _touchIndex)
					{
						UpdatePointerDrag(inputEventScreenDrag.Position);
					}
				}
				else if (inputEventScreenTouch.Pressed)
				{
					if (_touchIndex < 0)
					{
						HandlePointerButton(pressed: true, inputEventScreenTouch.Position, inputEventScreenTouch.Index);
					}
				}
				else if (inputEventScreenTouch.Index == _touchIndex)
				{
					HandlePointerButton(pressed: false, inputEventScreenTouch.Position, inputEventScreenTouch.Index);
				}
			}
			else if (_pointerPressed && _touchIndex < 0)
			{
				UpdatePointerDrag(inputEventMouseMotion.Position);
			}
		}
		else if (inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			HandlePointerButton(inputEventMouseButton.Pressed, inputEventMouseButton.Position, -1);
		}
	}

	private void HandlePointerButton(bool pressed, Vector2 position, int touchIndex)
	{
		if (pressed)
		{
			_pointerPressed = true;
			_pointerDragged = false;
			_touchIndex = touchIndex;
			_pressPosition = position;
			_pressScrollOffset = FindScrollContainer()?.ScrollHorizontal ?? 0;
		}
		else if (_pointerPressed && touchIndex == _touchIndex)
		{
			UpdatePointerDrag(position);
			ScrollContainer scrollContainer = FindScrollContainer();
			bool flag = scrollContainer != null && scrollContainer.ScrollHorizontal != _pressScrollOffset;
			bool num = !_pointerDragged && !flag && new Rect2(Vector2.Zero, base.Size).HasPoint(position);
			ResetPointerGesture();
			if (num)
			{
				this.OnSelected?.Invoke(this);
			}
		}
	}

	private void UpdatePointerDrag(Vector2 position)
	{
		if (_pressPosition.DistanceTo(position) >= 20f)
		{
			_pointerDragged = true;
		}
	}

	private void ResetPointerGesture()
	{
		_pointerPressed = false;
		_pointerDragged = false;
		_touchIndex = -1;
	}

	private ScrollContainer FindScrollContainer()
	{
		for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is ScrollContainer result)
			{
				return result;
			}
		}
		return null;
	}

	private void RefreshVisuals()
	{
		if (GodotObject.IsInstanceValid(_nameLabel))
		{
			_nameLabel.Text = (string.IsNullOrWhiteSpace(DisplayName) ? "关卡数据缺失" : DisplayName);
			_typeLabel.Text = "类型：" + Metadata.GetValueOrDefault("type", "未知").AsString();
			RefreshAuthorAndDifficulty();
			RefreshTags();
			_rewardLabel.Text = BuildRewardText();
			_finishTexture.Visible = IsFinished;
			_unavailableLabel.Visible = !IsAvailable;
			SetMapPreview();
			SetFinishIcons();
			SetSelected(IsSelected);
			SetInteractionEnabled(_interactionEnabled);
		}
	}

	private void RefreshAuthorAndDifficulty()
	{
		string optionalMetadataText = GetOptionalMetadataText("authorName");
		_authorLabel.Text = ((optionalMetadataText.Length > 0) ? ("作者：" + optionalMetadataText) : "");
		_authorLabel.TooltipText = _authorLabel.Text;
		_authorLabel.Visible = optionalMetadataText.Length > 0;
		string optionalMetadataText2 = GetOptionalMetadataText("difficulty");
		string text = optionalMetadataText2 switch
		{
			"easy" => "休闲", 
			"normal" => "普通", 
			"hard" => "挑战", 
			_ => optionalMetadataText2, 
		};
		_difficultyLabel.Text = ((text.Length > 0) ? ("难度：" + text) : "");
		_difficultyBadge.TooltipText = _difficultyLabel.Text;
		_difficultyBadge.Visible = text.Length > 0;
	}

	private string GetOptionalMetadataText(string key)
	{
		Variant valueOrDefault = Metadata.GetValueOrDefault(key, "");
		if (valueOrDefault.VariantType != Variant.Type.String)
		{
			return "";
		}
		return valueOrDefault.AsString().Trim();
	}

	private void SetMapPreview()
	{
		string mapName = Metadata.GetValueOrDefault("map", "Frontlawn").AsString();
		TowerDefenseMapConfig towerDefenseMapConfig = null;
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			towerDefenseMapConfig = TowerDefenseManager.Instance.GetMapConfig(mapName);
		}
		TowerDefenseMapConfig.ApplyMapPreviewTexture(_mapTexture, towerDefenseMapConfig?.GetMapThumbnail());
	}

	private void RefreshTags()
	{
		if (!GodotObject.IsInstanceValid(_tagFlow) || !GodotObject.IsInstanceValid(_tagChipTemplate))
		{
			return;
		}
		foreach (Node child in _tagFlow.GetChildren())
		{
			if (child != _tagChipTemplate)
			{
				_tagFlow.RemoveChild(child);
				child.QueueFree();
			}
		}
		List<string> list = BuildTags();
		if (list.Count == 0)
		{
			list.Add("无");
		}
		foreach (string item in list)
		{
			AddTagChip(item);
		}
	}

	private void AddTagChip(string tag)
	{
		PanelContainer panelContainer = _tagChipTemplate.Duplicate() as PanelContainer;
		if (GodotObject.IsInstanceValid(panelContainer))
		{
			panelContainer.Name = "TagChip";
			panelContainer.Visible = true;
			panelContainer.TooltipText = tag;
			panelContainer.MouseFilter = MouseFilterEnum.Ignore;
			Label node = panelContainer.GetNode<Label>("Text");
			node.Text = tag;
			node.TooltipText = tag;
			node.MouseFilter = MouseFilterEnum.Ignore;
			_tagFlow.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private List<string> BuildTags()
	{
		List<string> list = new List<string>();
		AddTagValue(Metadata.GetValueOrDefault("tag", ""), list);
		AddTagValue(Metadata.GetValueOrDefault("tags", new Godot.Collections.Array()), list);
		return list;
	}

	private static void AddTagValue(Variant value, List<string> tags)
	{
		if (tags.Count >= 8 || value.VariantType == Variant.Type.Nil)
		{
			return;
		}
		if (value.VariantType == Variant.Type.Array)
		{
			foreach (Variant item in value.AsGodotArray())
			{
				AddTagValue(item, tags);
				if (tags.Count >= 8)
				{
					break;
				}
			}
			return;
		}
		string text = value.AsString().Trim();
		if (text.Length > 0 && !tags.Contains(text))
		{
			tags.Add(text);
		}
	}

	private string BuildRewardText()
	{
		if (!Metadata.ContainsKey("reward") || Metadata["reward"].VariantType != Variant.Type.Dictionary)
		{
			return "奖励：未知";
		}
		Dictionary dictionary = Metadata["reward"].AsGodotDictionary();
		string text = dictionary.GetValueOrDefault("type", "").AsString();
		string text2 = dictionary.GetValueOrDefault("value", "").AsString();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "奖励：未知";
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return "奖励：" + text + " " + text2;
		}
		return "奖励：" + text;
	}

	private void SetFinishIcons()
	{
		while (_iconBox.GetChildCount() > 0)
		{
			Node child = _iconBox.GetChild(0);
			_iconBox.RemoveChild(child);
			child.QueueFree();
		}
		switch (Metadata.GetValueOrDefault("finishMethod", "WAVE").AsString().ToUpperInvariant())
		{
		case "VASE":
			AddIcon(VR_ICON);
			break;
		case "IZM":
			AddIcon(IZ_ICON);
			break;
		case "IZM2":
			AddIcon(IZ2_ICON);
			break;
		case "WAVE":
			if (Metadata.ContainsKey("survivalRoundlimit") && Metadata["survivalRoundlimit"].VariantType != Variant.Type.Nil)
			{
				int num = Metadata["survivalRoundlimit"].AsInt32();
				if (num == -1)
				{
					AddIcon(ENDLESS_ICON);
				}
				else if (num >= 0)
				{
					AddIcon(SURVIVAL_ICON);
				}
			}
			break;
		}
		if (Metadata.GetValueOrDefault("lucky", false).AsBool())
		{
			AddIcon(LUCKY_ICON);
		}
	}

	private void AddIcon(Texture2D texture)
	{
		if (GodotObject.IsInstanceValid(texture) && _iconBox.GetChildCount() < 2)
		{
			TextureRect node = new TextureRect
			{
				Texture = texture,
				CustomMinimumSize = new Vector2(50f, 50f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore
			};
			_iconBox.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}
}
