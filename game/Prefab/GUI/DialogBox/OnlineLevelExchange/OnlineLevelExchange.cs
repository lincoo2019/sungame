using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class OnlineLevelExchange : DialogBoxBase
{
	private static Json _onlineLevelExchangeResource;

	private static Texture2D _texturePlant;

	private static Texture2D _textureCustom;

	private DragMenu dragMenu;

	private Label costLabel;

	private TextureButton exchangeButton;

	private Label crystalNumLabel;

	private TextureRect readpaperTexture;

	private TextureButton switchModeButton;

	private TextureButton readButton;

	public string currentGroup;

	public string currentMode = "Plant";

	public List<Dictionary> filteredExchangeList = new List<Dictionary>();

	private static Json ONLINE_LEVEL_EXCHANGE_RESOURCE => _onlineLevelExchangeResource ?? (_onlineLevelExchangeResource = GD.Load<Json>("res://Asset/Config/OnlineLevelExchange/OnlineLevelExchangeResource.json"));

	private static Texture2D TEXTURE_PLANT => _texturePlant ?? (_texturePlant = GD.Load<Texture2D>("res://Asset/Texture/GUI/OnlineLevelExchange/OnlineLevelExchangeSelectPlant.png"));

	private static Texture2D TEXTURE_CUSTOM => _textureCustom ?? (_textureCustom = GD.Load<Texture2D>("res://Asset/Texture/GUI/OnlineLevelExchange/OnlineLevelExchangeSelectCustom.png"));

	public override void _Ready()
	{
		base._Ready();
		dragMenu = GetNode<DragMenu>("%DragMenu");
		costLabel = GetNode<Label>("%CostLabel");
		exchangeButton = GetNode<TextureButton>("%ExchangeButton");
		crystalNumLabel = GetNode<Label>("%CrystalNumLabel");
		readpaperTexture = GetNode<TextureRect>("%ReadpaperTexture");
		switchModeButton = GetNode<TextureButton>("%SwitchModeButton");
		readButton = GetNode<TextureButton>("%ReadButton");
		GetNode<TextureButton>("GroupButtonNode/AlmanacButton").Pressed += AlmanacButtonPressed;
		switchModeButton.Pressed += SwitchModeButtonPressed;
		exchangeButton.Pressed += ExchangeButtonPressed;
		GetNode<TextureButton>("%BackButton").Pressed += BackButtonPressed;
		readButton.Pressed += ReadButtonPressed;
		readpaperTexture.GuiInput += ReadpaperTextureGuiInput;
		dragMenu.CurrentIndexChanged += OnExchangeIndexChanged;
		base.VisibilityChanged += OnDialogVisibilityChanged;
		Setup();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(dragMenu))
		{
			dragMenu.CurrentIndexChanged -= OnExchangeIndexChanged;
		}
		base.VisibilityChanged -= OnDialogVisibilityChanged;
		base._ExitTree();
	}

	private void OnExchangeIndexChanged(int index)
	{
		RefreshExchangeState();
	}

	private void OnDialogVisibilityChanged()
	{
		if (base.Visible)
		{
			RefreshExchangeState();
		}
	}

	private void RefreshExchangeState()
	{
		if (!GodotObject.IsInstanceValid(dragMenu) || filteredExchangeList.Count == 0)
		{
			return;
		}
		int index = Mathf.Clamp(dragMenu.currentIndex, 0, filteredExchangeList.Count - 1);
		Dictionary dictionary = filteredExchangeList[index];
		string text = dictionary.GetValueOrDefault("Type", "Packet").AsString();
		if (!(text == "Packet"))
		{
			if (text == "Feature")
			{
				if (GameSaveManager.Instance.GetFeatureValue(dictionary.GetValueOrDefault("Key", "").AsString()) > 0)
				{
					costLabel.Visible = false;
					exchangeButton.Disabled = true;
				}
				else
				{
					exchangeButton.Disabled = false;
					costLabel.Visible = true;
					costLabel.Text = dictionary.GetValueOrDefault("CrystalNum", 0).AsInt32().ToString();
				}
			}
		}
		else if (GameSaveManager.Instance.GetTowerDefensePacketValue(dictionary.GetValueOrDefault("Key", "").AsString())["Unlock"].AsBool())
		{
			costLabel.Visible = false;
			exchangeButton.Disabled = true;
		}
		else
		{
			exchangeButton.Disabled = false;
			costLabel.Visible = true;
			costLabel.Text = dictionary.GetValueOrDefault("CrystalNum", 0).AsInt32().ToString();
		}
		crystalNumLabel.Text = GameSaveManager.Instance.GetKeyValue("CrystalNum").AsInt32().ToString();
	}

	public void PlayButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
	}

	public void Setup()
	{
		foreach (Node child in dragMenu.GetChildren())
		{
			child.QueueFree();
		}
		Array array = ((Dictionary)ONLINE_LEVEL_EXCHANGE_RESOURCE.Data)["Exchange"].AsGodotArray();
		filteredExchangeList.Clear();
		string text = ((currentMode == "Plant") ? "Packet" : "Feature");
		foreach (Variant item in array)
		{
			Dictionary dictionary = item.AsGodotDictionary();
			if (dictionary.GetValueOrDefault("Type", "Packet").AsString() != text)
			{
				continue;
			}
			filteredExchangeList.Add(dictionary);
			string text2 = dictionary.GetValueOrDefault("Type", "Packet").AsString();
			if (!(text2 == "Packet"))
			{
				if (text2 == "Feature")
				{
					Control control = new Control();
					dragMenu.AddChild(control, forceReadableName: false, InternalMode.Disabled);
					TextureRect textureRect = new TextureRect();
					textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
					textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
					textureRect.Texture = GD.Load<Texture2D>(dictionary.GetValueOrDefault("Texture", "").AsString());
					textureRect.CustomMinimumSize = new Vector2(80f, 80f);
					textureRect.Position = new Vector2(-40f, -40f);
					control.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
				}
			}
			else
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(dictionary.GetValueOrDefault("Key", "").AsString());
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
				towerDefenseInGamePacketShow.onlyDraw = true;
				towerDefenseInGamePacketShow.setMobileLayout = true;
				dragMenu.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
				towerDefenseInGamePacketShow.Init(packetConfig);
			}
		}
		dragMenu.CallDeferred("SetPos", 0);
		Callable.From(RefreshExchangeState).CallDeferred();
	}

	public void BackButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		CloseDialog();
	}

	public async void AlmanacButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("Almanac");
		base.Visible = false;
		await dialogBoxBase.WaitForClose();
		base.Visible = true;
		RefreshExchangeState();
	}

	private Tween Fade(Control node, bool fadeIn)
	{
		Tween tween = CreateTween();
		if (fadeIn)
		{
			node.Modulate = new Color(1f, 1f, 1f, 0f);
			node.Visible = true;
			tween.TweenProperty(node, "modulate:a", 1f, 0.2);
		}
		else
		{
			tween.TweenProperty(node, "modulate:a", 0f, 0.2);
			tween.Finished += delegate
			{
				node.Visible = false;
				node.Modulate = new Color(1f, 1f, 1f);
			};
		}
		return tween;
	}

	public async void SwitchModeButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		await ToSignal(Fade(switchModeButton, fadeIn: false), "finished");
		currentMode = ((currentMode == "Plant") ? "Custom" : "Plant");
		switchModeButton.TextureNormal = ((currentMode == "Custom") ? TEXTURE_CUSTOM : TEXTURE_PLANT);
		Setup();
		Fade(switchModeButton, fadeIn: true);
	}

	public void ExchangeButtonPressed()
	{
		if (GameSaveManager.Instance.GetKeyValue("CrystalNum").AsInt32() >= int.Parse(costLabel.Text))
		{
			Dictionary dictionary = filteredExchangeList[dragMenu.currentIndex];
			string text = dictionary.GetValueOrDefault("Type", "Packet").AsString();
			if (!(text == "Packet"))
			{
				if (text == "Feature")
				{
					GameSaveManager.Instance.SetFeatureValue(dictionary.GetValueOrDefault("Key", "").AsString(), true);
				}
			}
			else
			{
				Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(dictionary.GetValueOrDefault("Key", "").AsString());
				towerDefensePacketValue["Unlock"] = true;
				GameSaveManager.Instance.SetTowerDefensePacketValue(dictionary.GetValueOrDefault("Key", "").AsString(), towerDefensePacketValue);
			}
			GameSaveManager.Instance.SetKeyValue("CrystalNum", GameSaveManager.Instance.GetKeyValue("CrystalNum").AsInt32() - int.Parse(costLabel.Text));
			GameSaveManager.Instance.Save();
			RefreshExchangeState();
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]成功兑换[/font_size][/center]");
		}
		else
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您的水晶不足[/font_size][/center]");
		}
	}

	public void ReadButtonPressed()
	{
		Fade(readpaperTexture, !readpaperTexture.Visible);
	}

	public void ReadpaperTextureGuiInput(InputEvent _event)
	{
		if (Input.IsActionJustPressed("Press") && readpaperTexture.Visible)
		{
			ReadButtonPressed();
		}
	}
}
