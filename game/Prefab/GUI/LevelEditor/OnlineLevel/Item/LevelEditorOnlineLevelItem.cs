using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class LevelEditorOnlineLevelItem : Control
{
	public delegate void SelectEventHandler(string url, string id);

	private const string PATH = "user://Csharp/OnlineLevel";

	private static Texture2D _izIcon;

	private static Texture2D _iz2Icon;

	private static Texture2D _vrIcon;

	private static Texture2D _endlessIcon;

	private static Texture2D _survivalIcon;

	private static Texture2D _luckyIcon;

	private Label _nameLabel;

	private TextureRect _mapTexture;

	private TextureRect _finishTexture;

	private HBoxContainer _hBoxContainer;

	private CenterContainer _centerContainer;

	private Label _tagsLabel;

	private Label _recommendedLabel;

	[Export(PropertyHint.None, "")]
	public bool isBattle;

	[Export(PropertyHint.None, "")]
	public int cost = 1000;

	public string id;

	public bool lucky;

	private static Texture2D IZ_ICON => _izIcon ?? (_izIcon = GD.Load<Texture2D>("uid://ciyj1718ypbih"));

	private static Texture2D IZ2_ICON => _iz2Icon ?? (_iz2Icon = GD.Load<Texture2D>("uid://b8rsot26eb7od"));

	private static Texture2D Vr_ICON => _vrIcon ?? (_vrIcon = GD.Load<Texture2D>("uid://24j6iw1ww08b"));

	private static Texture2D ENDLESS_ICON => _endlessIcon ?? (_endlessIcon = GD.Load<Texture2D>("uid://bcjx34t665il2"));

	private static Texture2D SURVIVAL_ICON => _survivalIcon ?? (_survivalIcon = GD.Load<Texture2D>("uid://crvtrh32bqu0r"));

	private static Texture2D LUCKY_ICON => _luckyIcon ?? (_luckyIcon = GD.Load<Texture2D>("uid://caqbmeu30cbsa"));

	public event SelectEventHandler OnSelect;

	public event Action<string, string> OnAuthorSelected;

	public override void _Ready()
	{
		GetNode<TextureButton>("SelectButton").Pressed += SelectButtonPressed;
	}

	public async void Init(Dictionary data)
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_mapTexture = GetNode<TextureRect>("%MapTexture");
		_finishTexture = GetNode<TextureRect>("%FinishTexture");
		_hBoxContainer = GetNode<HBoxContainer>("%HBoxContainer");
		_centerContainer = GetNode<CenterContainer>("%CenterContainer");
		_tagsLabel = GetNode<Label>("%TagsLabel");
		_recommendedLabel = GetNode<Label>("%RecommendedLabel");
		_centerContainer.Visible = false;
		foreach (Node child in _hBoxContainer.GetChildren())
		{
			child.QueueFree();
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion())
		{
			return;
		}
		id = data.GetValueOrDefault("id", "-1").AsString();
		_nameLabel.Text = data.GetValueOrDefault("name", "").AsString();
		lucky = data.GetValueOrDefault("lucky", false).AsBool();
		_recommendedLabel.Visible = data.GetValueOrDefault("recommended", false).AsBool();
		SetTags(data);
		TowerDefenseMapConfig mapConfig = TowerDefenseManager.Instance.GetMapConfig(data.GetValueOrDefault("map", "Frontlawn").AsString());
		TowerDefenseMapConfig.ApplyMapPreviewTexture(_mapTexture, mapConfig?.GetMapThumbnail());
		string key = "OnlineLevel-" + id;
		if (GameSaveManager.Instance.GetLevelValue(key).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
			.GetValueOrDefault("Finish", 0)
			.AsInt32() > 0)
		{
			_finishTexture.Visible = true;
		}
		switch (data.GetValueOrDefault("finishMethod", "WAVE").AsString().ToUpper())
		{
		case "WAVE":
			_centerContainer.Visible = false;
			if (data.ContainsKey("survivalRoundlimit") && data["survivalRoundlimit"].VariantType != Variant.Type.Nil)
			{
				int num = data.GetValueOrDefault("survivalRoundlimit", -1).AsInt32();
				if (num == -1)
				{
					_centerContainer.Visible = true;
					AddIconTexture(ENDLESS_ICON);
				}
				else if (num >= 0)
				{
					_centerContainer.Visible = true;
					AddIconTexture(SURVIVAL_ICON);
				}
			}
			break;
		case "VASE":
			_centerContainer.Visible = true;
			AddIconTexture(Vr_ICON);
			break;
		case "IZM":
			_centerContainer.Visible = true;
			AddIconTexture(IZ_ICON);
			break;
		case "IZM2":
			_centerContainer.Visible = true;
			AddIconTexture(IZ2_ICON);
			break;
		}
		if (lucky)
		{
			_centerContainer.Visible = true;
			AddIconTexture(LUCKY_ICON);
		}
	}

	public void SelectButtonPressed()
	{
		if (isBattle && TowerDefenseManager.Instance.GetCoin() < cost)
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您的金币不足[/font_size][/center]");
			return;
		}
		OnlineLevelPreview onlineLevelPreview = (OnlineLevelPreview)DialogManager.Instance.DialogCreate("OnlineLevelPreview");
		if (this.OnAuthorSelected != null)
		{
			onlineLevelPreview.OnAuthorSelected += delegate(string uid, string name)
			{
				this.OnAuthorSelected?.Invoke(uid, name);
			};
		}
		onlineLevelPreview.InitDialog(id);
		onlineLevelPreview.OnSelect += EmitSelect;
	}

	public void EmitSelect(string url)
	{
		this.OnSelect?.Invoke(url, id);
	}

	private void SetTags(Dictionary data)
	{
		if (_tagsLabel == null)
		{
			return;
		}
		if (!data.ContainsKey("tags") || data["tags"].VariantType != Variant.Type.Array)
		{
			_tagsLabel.Visible = false;
			return;
		}
		Godot.Collections.Array array = data["tags"].AsGodotArray();
		List<string> list = new List<string>();
		for (int i = 0; i < array.Count; i++)
		{
			string text = array[i].AsString();
			if (text != "")
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			_tagsLabel.Visible = false;
			return;
		}
		_tagsLabel.Visible = true;
		_tagsLabel.Text = string.Join("\u3000", list);
	}

	public void AddIconTexture(Texture2D iconTexture)
	{
		if (_hBoxContainer.GetChildCount() < 2)
		{
			TextureRect textureRect = new TextureRect();
			textureRect.Texture = iconTexture;
			textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			textureRect.ExpandMode = TextureRect.ExpandModeEnum.KeepSize;
			textureRect.MouseFilter = MouseFilterEnum.Ignore;
			_hBoxContainer.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
		}
	}
}
