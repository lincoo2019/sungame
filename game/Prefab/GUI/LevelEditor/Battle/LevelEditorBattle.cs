using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorBattle : Control
{
	private static PackedScene _levelEditorOnlineLevelItemScene;

	private static Json _onlineLevelExchangeResource;

	private static Texture2D _arenaReward001;

	private static Texture2D _arenaReward002;

	private static Texture2D _arenaReward003;

	private static Texture2D _arenaReward004;

	private static Texture2D _arenaReward005;

	private CheckBox[] failCheckBox;

	public Control mainNode;

	public Control tipsNode;

	public Control awardNode;

	public Control overNode;

	public Control historyNode;

	private NativeHttpRequest levelGetHttpRequest;

	private NativeHttpRequest levelDataGetHttpRequest;

	private NativeHttpRequest levelListGetHttpRequest;

	private LevelEditorOnlineLevelItem levelEditorOnlineLevelItem;

	private Label finishLabel;

	private Label ticketLabel;

	private Label awardLabel;

	private SpriteBrightButton rewardButton;

	private Sprite2D costomRewardTexture1;

	private Sprite2D costomRewardTexture2;

	private DragMenu historyDragMenu;

	private Label historyNooneLabel;

	public static Array LevelList = new Array();

	public string currentLevelId = "";

	public Dictionary currentLevelData = new Dictionary();

	public int failNum;

	public int finishNum;

	private const int MaxRefreshRetry = 10;

	private int _refreshRetryCount;

	private bool _isPaidRefresh;

	private static readonly int[] CoinNumList = new int[13]
	{
		500, 1000, 2000, 3000, 5000, 8000, 12000, 16000, 20000, 30000,
		40000, 50000, 80000
	};

	private static readonly string[] AwardString = new string[13]
	{
		"奖励500金币", "奖励1000金币", "奖励2000金币", "奖励3000金币", "奖励5000金币", "奖励8000金币", "奖励12000金币", "奖励16000金币", "奖励20000金币", "奖励30000金币",
		"奖励40000金币+随机皮肤*1", "奖励50000金币+随机皮肤*1", "奖励80000金币+随机皮肤*2"
	};

	private static PackedScene LevelEditorOnlineLevelItemScene => _levelEditorOnlineLevelItemScene ?? (_levelEditorOnlineLevelItemScene = GD.Load<PackedScene>("uid://7tkumqfm20gv"));

	private static Json ONLINE_LEVEL_EXCHANGE_RESOURCE => _onlineLevelExchangeResource ?? (_onlineLevelExchangeResource = GD.Load<Json>("res://Asset/Config/OnlineLevelExchange/OnlineLevelExchangeResource.json"));

	private static Texture2D ArenaReward001 => _arenaReward001 ?? (_arenaReward001 = GD.Load<Texture2D>("uid://c6wk0iut0oxr2"));

	private static Texture2D ArenaReward002 => _arenaReward002 ?? (_arenaReward002 = GD.Load<Texture2D>("uid://camqjpt2tnw07"));

	private static Texture2D ArenaReward003 => _arenaReward003 ?? (_arenaReward003 = GD.Load<Texture2D>("uid://g0nshu8rrw67"));

	private static Texture2D ArenaReward004 => _arenaReward004 ?? (_arenaReward004 = GD.Load<Texture2D>("uid://oe6bi12ymuwh"));

	private static Texture2D ArenaReward005 => _arenaReward005 ?? (_arenaReward005 = GD.Load<Texture2D>("uid://ctlmif5y5srn"));

	public override void _Ready()
	{
		failCheckBox = new CheckBox[3]
		{
			GetNode<CheckBox>("%FailCheckBox"),
			GetNode<CheckBox>("%FailCheckBox2"),
			GetNode<CheckBox>("%FailCheckBox3")
		};
		mainNode = GetNode<Control>("%MainNode");
		tipsNode = GetNode<Control>("%TipsNode");
		awardNode = GetNode<Control>("%AwardNode");
		overNode = GetNode<Control>("%OverNode");
		historyNode = GetNode<Control>("%HistoryNode");
		levelGetHttpRequest = new NativeHttpRequest();
		levelDataGetHttpRequest = new NativeHttpRequest();
		levelListGetHttpRequest = new NativeHttpRequest();
		AddChild(levelGetHttpRequest, forceReadableName: false, InternalMode.Disabled);
		AddChild(levelDataGetHttpRequest, forceReadableName: false, InternalMode.Disabled);
		AddChild(levelListGetHttpRequest, forceReadableName: false, InternalMode.Disabled);
		levelEditorOnlineLevelItem = GetNode<LevelEditorOnlineLevelItem>("%LevelEditorOnlineLevelItem");
		levelEditorOnlineLevelItem.OnSelect += EnterLevel;
		finishLabel = GetNode<Label>("%FinishLabel");
		ticketLabel = GetNode<Label>("%TicketLabel");
		awardLabel = GetNode<Label>("%AwardLabel");
		rewardButton = GetNode<SpriteBrightButton>("%RewardButton");
		costomRewardTexture1 = GetNode<Sprite2D>("%CostomRewardTexture1");
		costomRewardTexture2 = GetNode<Sprite2D>("%CostomRewardTexture2");
		historyDragMenu = GetNode<DragMenu>("%HistoryDragMenu");
		historyNooneLabel = GetNode<Label>("%HistoryNooneLabel");
		levelListGetHttpRequest.RequestCompleted += LevelListGetHTTPRequestCompleted;
		levelDataGetHttpRequest.RequestCompleted += LevelDataGetHTTPRequestCompleted;
		levelGetHttpRequest.RequestCompleted += LevelGetHTTPRequestCompleted;
		GetNode<TextureButton>("MainNode/LevelEditorOnlineLevelItem/RefreshButton").Pressed += RefreshButtonPressed;
		GetNode<TextureButton>("MainNode/TipsButton").Pressed += TipsButtonPressed;
		rewardButton.OnPressed += RewardButtonPressed;
		GetNode<SpriteBrightButton>("MainNode/HistoryButton").OnPressed += HistoryButtonPressed;
	}

	public async void _Show()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		currentLevelId = GameSaveManager.Instance.GetKeyValue("LevelEditorBattleCurrentLevel").AsString();
		failNum = GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFailNum").AsInt32();
		finishNum = GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFinishNum").AsInt32();
		FreshFinish(finishNum);
		FreshFail(failNum);
		if (currentLevelId == "-1")
		{
			RandomLevel();
		}
		else
		{
			levelDataGetHttpRequest.Request("https://api.pvzhe.com/workshop/levels?include_id=" + currentLevelId, Global.Instance.header);
		}
	}

	public void RandomLevel()
	{
		levelDataGetHttpRequest.Request("https://api.pvzhe.com/workshop/levels/ids?feature=comp&random=1", Global.Instance.header);
	}

	private void TryRetryOrFail()
	{
		_refreshRetryCount++;
		if (_refreshRetryCount < 10)
		{
			RandomLevel();
			return;
		}
		if (_isPaidRefresh)
		{
			TowerDefenseManager.Instance.AddCoin(1000L);
			_isPaidRefresh = false;
		}
		_refreshRetryCount = 0;
		DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]网络不好,请重新刷新[/font_size][/center]");
	}

	public void FreshFail(int num)
	{
		switch (num)
		{
		case 0:
			failCheckBox[0].ButtonPressed = false;
			failCheckBox[1].ButtonPressed = false;
			failCheckBox[2].ButtonPressed = false;
			break;
		case 1:
			failCheckBox[0].ButtonPressed = true;
			failCheckBox[1].ButtonPressed = false;
			failCheckBox[2].ButtonPressed = false;
			break;
		case 2:
			failCheckBox[0].ButtonPressed = true;
			failCheckBox[1].ButtonPressed = true;
			failCheckBox[2].ButtonPressed = false;
			break;
		case 3:
			failCheckBox[0].ButtonPressed = true;
			failCheckBox[1].ButtonPressed = true;
			failCheckBox[2].ButtonPressed = true;
			Over(fail: true);
			break;
		}
	}

	public void FreshFinish(int num)
	{
		finishLabel.Text = $"胜场:{num}";
		costomRewardTexture1.Visible = false;
		costomRewardTexture2.Visible = false;
		switch (num)
		{
		case 0:
		case 1:
		case 2:
			rewardButton.Texture = ArenaReward001;
			break;
		case 3:
		case 4:
		case 5:
			rewardButton.Texture = ArenaReward002;
			break;
		case 6:
		case 7:
		case 8:
			rewardButton.Texture = ArenaReward003;
			break;
		case 9:
			rewardButton.Texture = ArenaReward004;
			break;
		case 10:
		case 11:
			rewardButton.Texture = ArenaReward004;
			costomRewardTexture1.Visible = true;
			break;
		case 12:
			rewardButton.Texture = ArenaReward005;
			costomRewardTexture1.Visible = true;
			costomRewardTexture2.Visible = true;
			break;
		default:
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]数据错误[/font_size][/center]");
			break;
		}
		switch (num)
		{
		case 0:
			awardLabel.Text = $"奖池：{500}金币";
			break;
		case 1:
			awardLabel.Text = $"奖池：{1000}金币";
			break;
		case 2:
			awardLabel.Text = $"奖池：{2000}金币";
			break;
		case 3:
			awardLabel.Text = $"奖池：{3000}金币";
			break;
		case 4:
			awardLabel.Text = $"奖池：{5000}金币";
			break;
		case 5:
			awardLabel.Text = $"奖池：{8000}金币";
			break;
		case 6:
			awardLabel.Text = $"奖池：{12000}金币";
			break;
		case 7:
			awardLabel.Text = $"奖池：{16000}金币";
			break;
		case 8:
			awardLabel.Text = $"奖池：{20000}金币";
			break;
		case 9:
			awardLabel.Text = $"奖池：{30000}金币";
			break;
		case 10:
			awardLabel.Text = $"奖池：{40000}金币+随机皮肤*1";
			break;
		case 11:
			awardLabel.Text = $"奖池：{50000}金币+随机皮肤*1";
			break;
		case 12:
			awardLabel.Text = $"奖池：{80000}金币+随机皮肤*2";
			Over(fail: false);
			break;
		default:
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]数据错误[/font_size][/center]");
			break;
		}
	}

	public void EnterLevel(string url, string id)
	{
		currentLevelId = id;
		if (url != "")
		{
			string url2 = "https://api.pvzhe.com" + url;
			levelGetHttpRequest.Request(url2, Global.Instance.header);
		}
	}

	public void LevelListGetHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			BroadCastManager.Instance.BroadCastFloatCreate(Global.Instance.GetHTTPRequestErrorMessage(result), Colors.Red);
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType != Variant.Type.Nil)
		{
			LevelList = json.Data.AsGodotArray();
			if (currentLevelId == "-1" && LevelList.Count > 0)
			{
				RandomLevel();
			}
		}
	}

	public void LevelDataGetHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			BroadCastManager.Instance.BroadCastFloatCreate(Global.Instance.GetHTTPRequestErrorMessage(result), Colors.Red);
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType == Variant.Type.Nil)
		{
			return;
		}
		if (json.Data.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = json.Data.AsGodotDictionary();
			if (dictionary.ContainsKey("list") && dictionary["list"].AsGodotArray().Count > 0)
			{
				currentLevelData = dictionary["list"].AsGodotArray()[0].AsGodotDictionary();
				levelEditorOnlineLevelItem.Call("Init", currentLevelData);
				_refreshRetryCount = 0;
				_isPaidRefresh = false;
			}
			else
			{
				TryRetryOrFail();
			}
		}
		if (json.Data.VariantType == Variant.Type.Array)
		{
			string text = ((int)json.Data.AsGodotArray()[0].AsDouble()).ToString();
			string key = "OnlineLevel-" + text;
			Dictionary levelValue = GameSaveManager.Instance.GetLevelValue(key);
			Dictionary dictionary2 = (levelValue.ContainsKey("Key") ? levelValue["Key"].AsGodotDictionary() : new Dictionary());
			if (!dictionary2.ContainsKey("Finish"))
			{
				dictionary2["Finish"] = 0;
			}
			if (dictionary2["Finish"].AsInt32() <= 0)
			{
				currentLevelId = text;
				GameSaveManager.Instance.SetKeyValue("LevelEditorBattleCurrentLevel", currentLevelId);
				levelDataGetHttpRequest.Request("https://api.pvzhe.com/workshop/levels?include_id=" + currentLevelId, Global.Instance.header);
			}
			else
			{
				TryRetryOrFail();
			}
		}
	}

	public void LevelGetHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			BroadCastManager.Instance.BroadCastFloatCreate(Global.Instance.GetHTTPRequestErrorMessage(result), Colors.Red);
			return;
		}
		string text = "OnlineLevel-" + currentLevelId;
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType != Variant.Type.Nil)
		{
			Dictionary dictionary = json.Data.AsGodotDictionary();
			if (dictionary.ContainsKey("error") && dictionary["error"].AsBool())
			{
				BroadCastManager.Instance.BroadCastFloatCreate("关卡Id无效", Colors.Red);
				return;
			}
			dictionary["Name"] = text;
			dictionary["Reward"] = new Dictionary();
			dictionary["Reward"].AsGodotDictionary()["RewardType"] = "Coin";
			dictionary["Reward"].AsGodotDictionary()["RewardFirst"] = "1000";
			Json json2 = new Json();
			json2.Parse(Json.Stringify(dictionary));
			GameSaveManager.Instance.SaveOnlineLevel(text, json2);
			ToBattle(json2);
		}
	}

	public async void ToBattle(Json json)
	{
		Dictionary dictionary = GameSaveManager.Instance.GetKeyValue("OnlinehBattleHistory").AsGodotDictionary();
		if (!dictionary.ContainsKey("Level"))
		{
			dictionary["Level"] = new Array();
		}
		Array array = dictionary["Level"].AsGodotArray();
		if (array.Count > 20)
		{
			array.RemoveAt(array.Count - 1);
		}
		array.Insert(0, new Dictionary
		{
			["id"] = currentLevelData.GetValueOrDefault("id", ""),
			["name"] = currentLevelData.GetValueOrDefault("name", ""),
			["map"] = currentLevelData.GetValueOrDefault("map", "")
		});
		GameSaveManager.Instance.SetKeyValue("OnlinehBattleHistory", dictionary);
		GameSaveManager.Instance.Save();
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
		towerDefenseLevelConfig.data = json;
		towerDefenseLevelConfig.Init();
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		Global.Instance.enterLevelMode = "OnlineLevel";
		Global.Instance.enterLevelIsBattle = true;
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.selectedLevelId = "";
			if (await MultiPlayerManager.Instance.StartCompatibleGameAsync())
			{
				TowerDefenseManager.Instance.UseCoin(500L);
			}
		}
		else
		{
			TowerDefenseManager.Instance.UseCoin(500L);
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}

	public void RefreshButtonPressed()
	{
		if (TowerDefenseManager.Instance.GetCoin() < 1000)
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您的金币数量不足1000\n无法刷新关卡[/font_size][/center]");
			return;
		}
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxChoose");
		dialogBoxBase.Set("text", "[center][font_size=24]刷新关卡[/font_size][/center]\n[center][font_size=16]是否花费1000金币刷新关卡？[/font_size][/center]");
		((DialogBoxChoose)dialogBoxBase).OnChooseTrue += delegate
		{
			_refreshRetryCount = 0;
			_isPaidRefresh = true;
			RandomLevel();
			TowerDefenseManager.Instance.UseCoin(1000L);
		};
	}

	public void TipsButtonPressed()
	{
		TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
		mainNode.Visible = false;
		tipsNode.Visible = true;
	}

	public void RewardButtonPressed()
	{
		TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
		mainNode.Visible = false;
		awardNode.Visible = true;
	}

	public async void Over(bool fail)
	{
		overNode.Visible = true;
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(rewardButton, "global_position", new Vector2(410f, 204f), 1.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		string text = "";
		int num = CoinNumList[finishNum];
		string value = ((!fail) ? "闯关成功" : "失败到达上限");
		switch (finishNum)
		{
		case 10:
		{
			Dictionary dictionary5 = RandomCustom();
			text = text + "\n" + dictionary5.GetValueOrDefault("String", "").AsString();
			num += dictionary5.GetValueOrDefault("Coin", 0).AsInt32();
			break;
		}
		case 11:
		{
			Dictionary dictionary4 = RandomCustom();
			text = text + "\n" + dictionary4.GetValueOrDefault("String", "").AsString();
			num += dictionary4.GetValueOrDefault("Coin", 0).AsInt32();
			break;
		}
		case 12:
		{
			Dictionary dictionary = RandomCustom();
			text = text + "\n" + dictionary.GetValueOrDefault("String", "").AsString();
			num += dictionary.GetValueOrDefault("Coin", 0).AsInt32();
			Dictionary dictionary2 = RandomCustom();
			text = text + "\n" + dictionary2.GetValueOrDefault("String", "").AsString();
			num += dictionary2.GetValueOrDefault("Coin", 0).AsInt32();
			Dictionary dictionary3 = RandomPlant();
			text = text + "\n" + dictionary3.GetValueOrDefault("String", "").AsString();
			num += dictionary3.GetValueOrDefault("Coin", 0).AsInt32();
			break;
		}
		}
		string text2 = AwardString[finishNum];
		if (finishNum == 12)
		{
			text2 += "+随机植物*1";
		}
		BroadCastConfig broadCastConfig = new BroadCastConfig();
		broadCastConfig.broadCastString = $"{value}，您一共完成{finishNum}个关卡\n{text2}{text}";
		BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
		await CreateCoin(num);
		await ToSignal(GetTree().CreateTimer(2.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		BroadCastManager.Instance.BraodCastClear();
		tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(rewardButton, "global_position", new Vector2(470f, 320f), 1.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		overNode.Visible = false;
		GameSaveManager.Instance.SetKeyValue("LevelEditorBattleFailNum", 0);
		GameSaveManager.Instance.SetKeyValue("LevelEditorBattleFinishNum", 0);
		failNum = GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFailNum").AsInt32();
		finishNum = GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFinishNum").AsInt32();
		FreshFinish(finishNum);
		FreshFail(failNum);
		GameSaveManager.Instance.Save();
	}

	public Dictionary RandomCustom()
	{
		string text = "";
		CharacterCustomConfig characterCustomConfig = null;
		TowerDefensePacketConfig packetConfig;
		TowerDefenseCharacterConfig characterConfig;
		do
		{
			List<string> packetNames = ResourceManager.Instance.GetPacketNames();
			packetConfig = TowerDefenseManager.GetPacketConfig(packetNames[GD.RandRange(0, packetNames.Count - 1)]);
			characterConfig = packetConfig.characterConfig;
		}
		while (!GodotObject.IsInstanceValid(characterConfig.customData) || characterConfig.customData.customList.Count <= 0);
		text = Tr(packetConfig.name);
		Array<CharacterCustomConfig> customList = characterConfig.customData.customList;
		characterCustomConfig = customList[GD.RandRange(0, customList.Count - 1)];
		if (GodotObject.IsInstanceValid(characterCustomConfig))
		{
			if (characterCustomConfig.openKey == "" || GameSaveManager.Instance.GetFeatureValue(characterCustomConfig.openKey) > 0)
			{
				int num = 10000;
				if (characterCustomConfig.type == "Gold")
				{
					num = 20000;
				}
				return new Dictionary
				{
					["String"] = $"你已获得{text}的皮肤:{Tr(characterCustomConfig.customHandbookName)},为你转化为{num}金币",
					["Coin"] = num
				};
			}
			GameSaveManager.Instance.SetFeatureValue(characterCustomConfig.openKey, true);
			return new Dictionary
			{
				["String"] = "恭喜你获得" + text + "的皮肤:" + Tr(characterCustomConfig.customHandbookName),
				["Coin"] = 0
			};
		}
		return new Dictionary();
	}

	public Dictionary RandomPlant()
	{
		if (!GodotObject.IsInstanceValid(ONLINE_LEVEL_EXCHANGE_RESOURCE) || ONLINE_LEVEL_EXCHANGE_RESOURCE.Data.VariantType != Variant.Type.Dictionary)
		{
			return new Dictionary();
		}
		Array array = ((Dictionary)ONLINE_LEVEL_EXCHANGE_RESOURCE.Data)["Exchange"].AsGodotArray();
		Array array2 = new Array();
		foreach (Variant item in array)
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = item.AsGodotDictionary();
				if (!(dictionary.GetValueOrDefault("Type", "Packet").AsString() != "Packet"))
				{
					array2.Add(dictionary);
				}
			}
		}
		if (array2.Count == 0)
		{
			return new Dictionary();
		}
		Dictionary dictionary2 = array2.PickRandom().AsGodotDictionary();
		string text = dictionary2.GetValueOrDefault("Key", "").AsString();
		int num = dictionary2.GetValueOrDefault("CrystalNum", 0).AsInt32();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return new Dictionary();
		}
		string text2 = Tr(packetConfig.name);
		Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(text);
		if (towerDefensePacketValue.GetValueOrDefault("Unlock", false).AsBool())
		{
			GameSaveManager.Instance.SetKeyValue("CrystalNum", GameSaveManager.Instance.GetKeyValue("CrystalNum").AsInt32() + num);
			return new Dictionary
			{
				["String"] = $"你已获得{text2},为你转化为{num}在线水晶",
				["Coin"] = 0
			};
		}
		towerDefensePacketValue["Unlock"] = true;
		GameSaveManager.Instance.SetTowerDefensePacketValue(text, towerDefensePacketValue);
		return new Dictionary
		{
			["String"] = "恭喜你获得" + text2,
			["Coin"] = 0
		};
	}

	public async Task CreateCoin(int num)
	{
		while (num >= 1000)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, rewardButton.GlobalPosition + new Vector2(50f, 40f), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
			towerDefenseGroundItemBase.Reparent(this, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase is TowerDefenseCoinBase towerDefenseCoinBase)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase.Collection;
			}
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 50)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, rewardButton.GlobalPosition + new Vector2(50f, 40f), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase2.gridPos = new Vector2I(towerDefenseGroundItemBase2.gridPos.X, 200);
			towerDefenseGroundItemBase2.Reparent(this, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase2 is TowerDefenseCoinBase towerDefenseCoinBase2)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase2.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase2.Collection;
			}
			num -= 50;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 10)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, rewardButton.GlobalPosition + new Vector2(50f, 40f), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase3.gridPos = new Vector2I(towerDefenseGroundItemBase3.gridPos.X, 200);
			towerDefenseGroundItemBase3.Reparent(this, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase3 is TowerDefenseCoinBase towerDefenseCoinBase3)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase3.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase3.Collection;
			}
			num -= 10;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public void HistoryButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		historyNode.Visible = true;
		historyNooneLabel.Visible = false;
		Dictionary dictionary = GameSaveManager.Instance.GetKeyValue("OnlinehBattleHistory").AsGodotDictionary();
		if (!dictionary.ContainsKey("Level"))
		{
			dictionary["Level"] = new Array();
		}
		Array array = dictionary["Level"].AsGodotArray();
		foreach (Node child in historyDragMenu.GetChildren())
		{
			child.QueueFree();
		}
		foreach (Variant item in array)
		{
			Control control = LevelEditorOnlineLevelItemScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			historyDragMenu.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			control.PivotOffset = control.Size / 2f;
			control.Call("Init", item);
		}
		if (array.Count <= 0)
		{
			historyNooneLabel.Visible = true;
		}
	}
}
