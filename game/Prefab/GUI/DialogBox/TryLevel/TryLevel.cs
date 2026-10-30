using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TryLevel : DialogBoxBase
{
	private static Json _tryLevelResource;

	private static Texture2D _debuffFinish;

	private DragMenu dragMenu;

	private TextureButton shopButton;

	private TextureButton starExchangeButton;

	private TextureButton purpleButton;

	private TextureButton starButton;

	private TextureButton colourButton;

	private readonly System.Collections.Generic.Dictionary<string, List<Control>> _groupPreviewCache = new System.Collections.Generic.Dictionary<string, List<Control>>();

	private readonly List<Control> _activeGroupPreviews = new List<Control>();

	public string currentGroup;

	public bool openedFromShop;

	public bool openedFromStarExchange;

	private static Json TRY_LEVEL_RESOURCE => _tryLevelResource ?? (_tryLevelResource = GD.Load<Json>("res://Asset/Config/Level/TryLevelResource.json"));

	private static Texture2D DEBUFF_FINISH => _debuffFinish ?? (_debuffFinish = GD.Load<Texture2D>("uid://dpm2kwh72n3p4"));

	public override void Init(Dictionary data)
	{
		openedFromShop = data.GetValueOrDefault("openedFromShop", false).AsBool();
		openedFromStarExchange = data.GetValueOrDefault("openedFromStarExchange", false).AsBool();
	}

	public override void _Ready()
	{
		base._Ready();
		dragMenu = GetNode<DragMenu>("%DragMenu");
		shopButton = GetNode<TextureButton>("%ShopButton");
		shopButton.Visible = openedFromShop || (GlobalFeatureManager.Instance?.IsUnlocked("Shop") ?? false);
		starExchangeButton = GetNode<TextureButton>("%StarExchangeButton");
		purpleButton = GetNode<TextureButton>("%PurpleButton");
		starButton = GetNode<TextureButton>("%StarButton");
		colourButton = GetNode<TextureButton>("%ColourButton");
		purpleButton.Pressed += SelectPurple;
		starButton.Pressed += SelectStar;
		colourButton.Pressed += SelectColour;
		shopButton.Pressed += ShopButtonPressed;
		starExchangeButton.Pressed += StarExchangeButtonPressed;
		GetNode<TextureButton>("GroupButtonNode/AlmanacButton").Pressed += AlmanacButtonPressed;
		GetNode<TextureButton>("PlayButton").Pressed += PlayButtonPressed;
		GetNode<TextureButton>("%BackButton").Pressed += BackButtonPressed;
		if (Global.Instance.enterTryLevelGroup == "")
		{
			SelectPurple();
			return;
		}
		switch (Global.Instance.enterTryLevelGroup)
		{
		case "Purple":
			SelectPurple();
			break;
		case "Star":
			SelectStar();
			break;
		case "Colour":
			SelectColour();
			break;
		}
		Global.Instance.enterTryLevelGroup = "";
	}

	public void PlayButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		Array array = ((Dictionary)TRY_LEVEL_RESOURCE.Data)[currentGroup].AsGodotArray();
		string text = GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString();
		Dictionary dictionary = array[dragMenu.currentIndex].AsGodotDictionary()["Level"].AsGodotDictionary();
		if (dictionary.ContainsKey(text) && dictionary[text].AsString() != "")
		{
			TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(dictionary[text].AsString());
		}
		else
		{
			TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(dictionary["Normal"].AsString());
		}
		Global.Instance.enterLevelMode = "LevelChoose";
		Global.Instance.currentLevelChoose = "TryLevel";
		Global.Instance.enterTryLevelGroup = currentGroup;
		SceneManager.Instance.ChangeScene("TowerDefense");
	}

	public void SelectGroup(string groupName)
	{
		currentGroup = groupName;
		DetachActiveGroupPreviews();
		foreach (Control orCreateGroupPreview in GetOrCreateGroupPreviews(groupName))
		{
			if (orCreateGroupPreview.GetParent() != dragMenu)
			{
				dragMenu.AddChild(orCreateGroupPreview, forceReadableName: false, InternalMode.Disabled);
			}
			_activeGroupPreviews.Add(orCreateGroupPreview);
		}
		dragMenu.CallDeferred("SetPos", 0);
	}

	private void DetachActiveGroupPreviews()
	{
		foreach (Control activeGroupPreview in _activeGroupPreviews)
		{
			if (GodotObject.IsInstanceValid(activeGroupPreview) && activeGroupPreview.GetParent() == dragMenu)
			{
				dragMenu.RemoveChild(activeGroupPreview);
			}
		}
		_activeGroupPreviews.Clear();
	}

	private List<Control> GetOrCreateGroupPreviews(string groupName)
	{
		if (_groupPreviewCache.TryGetValue(groupName, out var value))
		{
			RefreshGroupPreviewFinishState(groupName, value);
			return value;
		}
		List<Control> list = new List<Control>();
		foreach (Variant item in ((Dictionary)TRY_LEVEL_RESOURCE.Data)[groupName].AsGodotArray())
		{
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item.AsGodotDictionary().GetValueOrDefault("Character", "").AsString());
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.setMobileLayout = true;
			dragMenu.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly))
			{
				towerDefenseInGamePacketShow.Init(packetConfigReadOnly);
			}
			list.Add(towerDefenseInGamePacketShow);
		}
		_groupPreviewCache[groupName] = list;
		RefreshGroupPreviewFinishState(groupName, list);
		return list;
	}

	private void RefreshGroupPreviewFinishState(string groupName, List<Control> previews)
	{
		Array array = ((Dictionary)TRY_LEVEL_RESOURCE.Data)[groupName].AsGodotArray();
		for (int i = 0; i < previews.Count && i < array.Count; i++)
		{
			if (previews[i] is TowerDefenseInGamePacketShow packet)
			{
				Dictionary dictionary = array[i].AsGodotDictionary();
				Dictionary dictionary2 = GameSaveManager.Instance.GetLevelValue(dictionary["SaveKey"].AsString()).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
				bool visible = CommandManager.Instance.debugOpenAllLevel || dictionary2.GetValueOrDefault("Finish", 0).AsInt32() > 0;
				SetTryLevelFinishMarkerVisible(packet, visible);
			}
		}
	}

	private static void SetTryLevelFinishMarkerVisible(TowerDefenseInGamePacketShow packet, bool visible)
	{
		Sprite2D sprite2D = packet.GetNodeOrNull<Sprite2D>("FinishMarker");
		if (sprite2D == null)
		{
			sprite2D = new Sprite2D();
			sprite2D.Name = "FinishMarker";
			sprite2D.Texture = DEBUFF_FINISH;
			sprite2D.Scale = Vector2.One * 0.5f;
			sprite2D.Position = new Vector2(40f, 25f);
			packet.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		}
		sprite2D.Visible = visible;
	}

	public void SelectPurple()
	{
		SelectGroup("Purple");
		purpleButton.ButtonPressed = true;
		shopButton.Visible = true;
		starExchangeButton.Visible = false;
	}

	public void SelectStar()
	{
		SelectGroup("Star");
		starButton.ButtonPressed = true;
		shopButton.Visible = false;
		starExchangeButton.Visible = true;
	}

	public void SelectColour()
	{
		SelectGroup("Colour");
		colourButton.ButtonPressed = true;
		shopButton.Visible = true;
		starExchangeButton.Visible = false;
	}

	public void BackButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		CloseDialog();
	}

	public void ShopButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (openedFromShop)
		{
			CloseDialog();
			return;
		}
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		if (instance != null && instance.IsUnlocked("Shop"))
		{
			DialogManager.Instance.DialogCreate("Shop");
			CloseDialog();
		}
	}

	public async void StarExchangeButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (openedFromStarExchange)
		{
			CloseDialog();
			return;
		}
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("StarExchange");
		base.Visible = false;
		await dialogBoxBase.WaitForClose();
		base.Visible = true;
	}

	public async void AlmanacButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("Almanac");
		base.Visible = false;
		await dialogBoxBase.WaitForClose();
		base.Visible = true;
	}
}
