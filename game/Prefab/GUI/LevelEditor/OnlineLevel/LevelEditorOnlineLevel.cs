using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorOnlineLevel : Control
{
	private sealed record BrowseState(int Page, string Suffix, string Suffix2, string Search, string Tags, bool MyCollection, int TypeIndex, string SearchText, string AuthorUid, string AuthorName, BrowseState Parent);

	private static PackedScene _levelEditorOnlineLevelItem;

	private static readonly Regex LevelIdRegex = new Regex("^[0-9a-zA-Z_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static BrowseState _returnBrowseState;

	private BrowseState _previousBrowseState;

	private string _authorUid = "";

	private string _authorName = "";

	private int _requestedPage = 1;

	private NativeHttpRequest _levelGetHTTPRequest;

	private NativeHttpRequest _levelInformationGetHTTPRequest;

	private HFlowContainer _onlineLevelContainer;

	private MarginContainer _levelContainer;

	private Label _pageLabel;

	private Label _statusLabel;

	private LineEdit _levelIdLineEdit;

	private MainButton _enterLevelButton;

	private SpinBox _jumpPageToSpinBox;

	private MainButton _jumpPageToButton;

	private OptionButton _typeOptionButton;

	private OptionButton _tagsOptionButton;

	private bool _pageLoading;

	public int levelNum = 1;

	public int maxPage = 1;

	public int currentPage = -1;

	public Array currentPageList = new Array();

	public string suffix = "";

	public string suffix2 = "";

	public string search = "";

	public string tags = "";

	public string currentLevelId = "";

	public bool myCollection;

	private static PackedScene LEVEL_EDITOR_ONLINE_LEVEL_ITEM => _levelEditorOnlineLevelItem ?? (_levelEditorOnlineLevelItem = GD.Load<PackedScene>("uid://7tkumqfm20gv"));

	public bool IsAuthorPage => _authorUid != "";

	public override void _Ready()
	{
		_levelGetHTTPRequest = new NativeHttpRequest();
		_levelInformationGetHTTPRequest = new NativeHttpRequest();
		AddChild(_levelGetHTTPRequest, forceReadableName: false, InternalMode.Disabled);
		AddChild(_levelInformationGetHTTPRequest, forceReadableName: false, InternalMode.Disabled);
		_onlineLevelContainer = GetNode<HFlowContainer>("%OnlineLevelContainer");
		_levelContainer = GetNode<MarginContainer>("%LevelContainer");
		_pageLabel = GetNode<Label>("%PageLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_levelIdLineEdit = GetNode<LineEdit>("%LevelIdLineEdit");
		_enterLevelButton = GetNode<MainButton>("%EnterLevelButton");
		_jumpPageToSpinBox = GetNode<SpinBox>("%JumpPageToSpinBox");
		_jumpPageToButton = GetNode<MainButton>("%JumpPageToButton");
		_typeOptionButton = GetNode<OptionButton>("%TypeOptionButton");
		_tagsOptionButton = GetNode<OptionButton>("%TagsOptionButton");
		LevelEditorDropdown.Configure(_typeOptionButton);
		LevelEditorDropdown.Configure(_tagsOptionButton);
		RestoreBrowseState();
		_levelGetHTTPRequest.RequestCompleted += LevelGetHTTPRequestCompleted;
		_levelInformationGetHTTPRequest.RequestCompleted += LevelInformationGetHTTPRequestCompleted;
		GetNode<SpriteBrightButton>("%PreCustomButton").OnPressed += PreButtonPressed;
		GetNode<SpriteBrightButton>("%NextCustomButton").OnPressed += NextButtonpressed;
		_enterLevelButton.Pressed += EnterLevelButtonPressed;
		GetNode<MainButton>("%SearchLevelButton").Pressed += SearchLevelButtonPressed;
		_typeOptionButton.ItemSelected += delegate(long index)
		{
			TypeOptionButtonItemSelected((int)index);
		};
		_tagsOptionButton.ItemSelected += delegate(long index)
		{
			TagsOptionButtonItemSelected((int)index);
		};
		_jumpPageToButton.Pressed += JumpPageToButtonPressed;
		GetNode<NinePatchButtonBase>("HBoxContainer3/MyCollectionButton").OnPressed += MyCollectionButtonPressed;
		GetNode<NinePatchButtonBase>("HBoxContainer3/SmartRecommendationButton").OnPressed += SmartRecommendationButtonPressed;
		GetNode<NinePatchButtonBase>("HBoxContainer3/OfficialRecommendationButton").OnPressed += OfficialRecommendationButtonPressed;
		GetNode<NinePatchButtonBase>("HBoxContainer3/SortByTimeButton").OnPressed += SortByTimeButtonPressed;
		GetNode<NinePatchButtonBase>("HBoxContainer3/SortByNumberOfPlayedButton").OnPressed += SortByNumberOfPlayedButtonPressed;
		GetNode<TextureButton>("ExchangeButton").Pressed += ExchangeButtonPressed;
		InternetServerManager.Instance.OnOnlineLevelGet += LoadPageData;
		InternetServerManager.Instance.OnWorkshopTagsGet += LoadTagsData;
		InternetServerManager.Instance.GetWorkshopTags();
	}

	private void RestoreBrowseState()
	{
		BrowseState returnBrowseState = _returnBrowseState;
		_returnBrowseState = null;
		if (!(returnBrowseState == null))
		{
			ApplyBrowseState(returnBrowseState);
		}
	}

	private BrowseState CaptureBrowseState()
	{
		return new BrowseState(Mathf.Max(1, currentPage), suffix, suffix2, search, tags, myCollection, _typeOptionButton.Selected, _levelIdLineEdit.Text, _authorUid, _authorName, _previousBrowseState);
	}

	private void ApplyBrowseState(BrowseState state)
	{
		currentPage = state.Page;
		suffix = state.Suffix;
		suffix2 = state.Suffix2;
		search = state.Search;
		tags = state.Tags;
		myCollection = state.MyCollection;
		_typeOptionButton.Select(state.TypeIndex);
		_levelIdLineEdit.Text = state.SearchText;
		_authorUid = state.AuthorUid;
		_authorName = state.AuthorName;
		_previousBrowseState = state.Parent;
		for (int i = 0; i < _tagsOptionButton.ItemCount; i++)
		{
			if (i == 0 || _tagsOptionButton.GetItemText(i) == tags)
			{
				_tagsOptionButton.Select(i);
			}
		}
		RefreshBrowseMode();
	}

	public void OpenAuthorLevels(string uid, string name)
	{
		if (!string.IsNullOrEmpty(uid))
		{
			if (!IsAuthorPage)
			{
				_previousBrowseState = CaptureBrowseState();
			}
			_authorUid = uid;
			_authorName = name;
			myCollection = false;
			suffix = (suffix2 = (search = (tags = "")));
			_typeOptionButton.Select(0);
			_levelIdLineEdit.Text = "";
			ResetTags();
			RefreshBrowseMode();
			maxPage = 1;
			GetPage();
		}
	}

	public bool TryReturnToLevelList()
	{
		if (!IsAuthorPage || _previousBrowseState == null)
		{
			return false;
		}
		ApplyBrowseState(_previousBrowseState);
		GetPage(currentPage);
		return true;
	}

	private void RefreshBrowseMode()
	{
		GetNode<Control>("HBoxContainer").Visible = !IsAuthorPage;
		GetNode<Control>("HBoxContainer3").Visible = !IsAuthorPage;
		GetNode<Control>("ExchangeButton").Visible = !IsAuthorPage;
		GetNode<Label>("Label").Text = (IsAuthorPage ? "作者关卡" : "在线关卡");
		Label node = GetNode<Label>("%AuthorNameLabel");
		node.Visible = IsAuthorPage;
		node.Text = _authorName;
		node.TooltipText = _authorName;
	}

	public override void _ExitTree()
	{
		InternetServerManager.Instance.OnOnlineLevelGet -= LoadPageData;
		InternetServerManager.Instance.OnWorkshopTagsGet -= LoadTagsData;
		base._ExitTree();
	}

	public void GetPage(int pageIndex = 1)
	{
		_requestedPage = Mathf.Clamp(pageIndex, 1, 100000);
		currentPage = _requestedPage;
		maxPage = Mathf.Max(currentPage, maxPage);
		PageLabelFresh();
		_levelContainer.Visible = false;
		_pageLoading = true;
		_statusLabel.Visible = false;
		ShowLoadingLabelAfterDelay(1f);
		ClearLevelItems();
		if (IsAuthorPage)
		{
			InternetServerManager.Instance.GetOnlineLevelPage(_requestedPage, "", "", "", "", _authorUid);
			return;
		}
		if (!myCollection)
		{
			InternetServerManager.Instance.GetOnlineLevelPage(_requestedPage, suffix, suffix2, search, tags);
			return;
		}
		InternetServerManager.Instance.onlineLevelHTTPRequest.CancelRequest();
		LoadMyCollectionPage(_requestedPage);
	}

	private async void ShowLoadingLabelAfterDelay(float delay)
	{
		await ToSignal(GetTree().CreateTimer(delay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (_pageLoading && GodotObject.IsInstanceValid(_statusLabel))
		{
			_statusLabel.Text = "关卡加载中...";
			_statusLabel.Visible = true;
		}
	}

	private void ClearLevelItems()
	{
		foreach (Node child in _onlineLevelContainer.GetChildren())
		{
			_onlineLevelContainer.RemoveChild(child);
			child.QueueFree();
		}
		GetNode<ScrollContainer>("LevelContainer/ScrollContainer").ScrollVertical = 0;
	}

	public void LoadTagsData(Array data)
	{
		if (!GodotObject.IsInstanceValid(_tagsOptionButton))
		{
			return;
		}
		_tagsOptionButton.Clear();
		_tagsOptionButton.AddItem("全部标签", 0);
		int idx = 0;
		for (int i = 0; i < data.Count; i++)
		{
			string text = data[i].AsGodotDictionary().GetValueOrDefault("name", "").AsString();
			if (text != "")
			{
				_tagsOptionButton.AddItem(text, i + 1);
				if (text == tags)
				{
					idx = _tagsOptionButton.ItemCount - 1;
				}
			}
		}
		_tagsOptionButton.Select(idx);
	}

	public void TagsOptionButtonItemSelected(int index)
	{
		myCollection = false;
		if (index <= 0)
		{
			tags = "";
		}
		else
		{
			tags = _tagsOptionButton.GetItemText(index);
		}
		GetPage();
	}

	public void LoadMyCollectionPage(int pageIndex = 1)
	{
		_pageLoading = false;
		_levelContainer.Visible = true;
		Array array = GameSaveManager.Instance.GetKeyValue("OnlineMyCollection").AsGodotDictionary().GetValueOrDefault("Level", new Array())
			.AsGodotArray();
		levelNum = array.Count;
		maxPage = Mathf.Max(1, (int)Mathf.Ceil((double)array.Count / 10.0));
		currentPage = Mathf.Clamp(pageIndex, 1, maxPage);
		_statusLabel.Visible = levelNum == 0;
		_statusLabel.Text = "暂无收藏关卡";
		PageLabelFresh();
		for (int i = (currentPage - 1) * 10; i < Mathf.Min(currentPage * 10, levelNum); i++)
		{
			Dictionary dictionary = array[i].AsGodotDictionary();
			if (dictionary.GetValueOrDefault("id", "-1").AsString() != "-1")
			{
				LevelEditorOnlineLevelItem levelEditorOnlineLevelItem = LEVEL_EDITOR_ONLINE_LEVEL_ITEM.Instantiate<LevelEditorOnlineLevelItem>(PackedScene.GenEditState.Disabled);
				_onlineLevelContainer.AddChild(levelEditorOnlineLevelItem, forceReadableName: false, InternalMode.Disabled);
				levelEditorOnlineLevelItem.Init(dictionary);
				levelEditorOnlineLevelItem.OnSelect += EnterLevel;
				levelEditorOnlineLevelItem.OnAuthorSelected += OpenAuthorLevels;
			}
		}
	}

	public void LoadPageData(Dictionary data)
	{
		if (myCollection || !GodotObject.IsInstanceValid(_levelContainer))
		{
			return;
		}
		_pageLoading = false;
		_levelContainer.Visible = true;
		ClearLevelItems();
		if (!data.TryGetValue("list", out var value) || value.VariantType != Variant.Type.Array)
		{
			currentPage = _requestedPage;
			maxPage = Mathf.Max(currentPage, maxPage);
			currentPageList = new Array();
			levelNum = 0;
			_statusLabel.Text = "关卡加载失败，请点击跳转页数重试";
			_statusLabel.Visible = true;
			PageLabelFresh();
			return;
		}
		levelNum = data.GetValueOrDefault("total", 0).AsInt32();
		maxPage = Mathf.Max(1, data.GetValueOrDefault("maxPage", data.GetValueOrDefault("max_page", 1)).AsInt32());
		currentPage = Mathf.Clamp(data.GetValueOrDefault("page", _requestedPage).AsInt32(), 1, maxPage);
		currentPageList = value.AsGodotArray();
		_statusLabel.Visible = currentPageList.Count == 0;
		_statusLabel.Text = (IsAuthorPage ? "该作者暂无公开关卡" : "暂无关卡");
		PageLabelFresh();
		foreach (Variant currentPage in currentPageList)
		{
			Dictionary dictionary = currentPage.AsGodotDictionary();
			if (dictionary.GetValueOrDefault("id", "-1").AsString() != "-1")
			{
				LevelEditorOnlineLevelItem levelEditorOnlineLevelItem = LEVEL_EDITOR_ONLINE_LEVEL_ITEM.Instantiate<LevelEditorOnlineLevelItem>(PackedScene.GenEditState.Disabled);
				_onlineLevelContainer.AddChild(levelEditorOnlineLevelItem, forceReadableName: false, InternalMode.Disabled);
				levelEditorOnlineLevelItem.Init(dictionary);
				levelEditorOnlineLevelItem.OnSelect += EnterLevel;
				levelEditorOnlineLevelItem.OnAuthorSelected += OpenAuthorLevels;
			}
		}
	}

	public void PageLabelFresh()
	{
		_jumpPageToSpinBox.MaxValue = maxPage;
		_jumpPageToSpinBox.Value = currentPage;
		_pageLabel.Text = $"第{currentPage}页，共{maxPage}页";
	}

	public void PreButtonPressed()
	{
		if (currentPage != -1)
		{
			int num = currentPage - 1;
			if (num < 1)
			{
				num = maxPage;
			}
			GetPage(num);
		}
	}

	public void NextButtonpressed()
	{
		if (currentPage != -1)
		{
			int num = currentPage + 1;
			if (num > maxPage)
			{
				num = 1;
			}
			GetPage(num);
		}
	}

	public void EnterLevelButtonPressed()
	{
		if (LevelIdRegex.IsMatch(_levelIdLineEdit.Text))
		{
			string url = $"https://api.pvzhe.com/workshop/levels/{_levelIdLineEdit.Text}";
			_levelInformationGetHTTPRequest.Request(url, Global.Instance.header);
			_enterLevelButton.Disabled = true;
		}
	}

	public void SearchLevelButtonPressed()
	{
		myCollection = false;
		search = _levelIdLineEdit.Text.Trim();
		GetPage();
	}

	public void JumpPageToButtonPressed()
	{
		GetPage((int)_jumpPageToSpinBox.Value);
	}

	public async void ToBattle(Json json)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
		towerDefenseLevelConfig.data = json;
		towerDefenseLevelConfig.Init();
		_returnBrowseState = CaptureBrowseState();
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		Global.Instance.enterLevelMode = "OnlineLevel";
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.selectedLevelId = "";
			await MultiPlayerManager.Instance.StartCompatibleGameAsync();
		}
		else
		{
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}

	public void EnterLevel(string url, string id)
	{
		currentLevelId = id;
		if (url != "")
		{
			string url2 = $"https://api.pvzhe.com{url}";
			_levelGetHTTPRequest.Request(url2, Global.Instance.header);
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

	public void LevelInformationGetHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			BroadCastManager.Instance.BroadCastFloatCreate(Global.Instance.GetHTTPRequestErrorMessage(result), Colors.Red);
			_enterLevelButton.Disabled = false;
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType != Variant.Type.Nil)
		{
			Dictionary dictionary = json.Data.AsGodotDictionary();
			string _id = dictionary.GetValueOrDefault("id", "-1").AsString();
			if (_id != "-1")
			{
				OnlineLevelPreview obj = (OnlineLevelPreview)DialogManager.Instance.DialogCreate("OnlineLevelPreview");
				obj.OnAuthorSelected += OpenAuthorLevels;
				obj.InitDialogData(dictionary);
				obj.OnSelect += delegate(string url)
				{
					EnterLevel(url, _id);
				};
			}
			else
			{
				BroadCastManager.Instance.BroadCastFloatCreate("关卡Id无效", Colors.Red);
			}
		}
		else
		{
			BroadCastManager.Instance.BroadCastFloatCreate("关卡Id无效", Colors.Red);
		}
		_enterLevelButton.Disabled = false;
	}

	public void MyCollectionButtonPressed()
	{
		myCollection = true;
		suffix = "";
		_typeOptionButton.Selected = 0;
		TypeOptionButtonItemSelected(0);
		ResetTags();
		GetPage();
	}

	public void SmartRecommendationButtonPressed()
	{
		myCollection = false;
		suffix = "&recp";
		ResetTags();
		GetPage();
	}

	public void OfficialRecommendationButtonPressed()
	{
		myCollection = false;
		suffix = "&feature=rec";
		ResetTags();
		GetPage();
	}

	public void SortByTimeButtonPressed()
	{
		myCollection = false;
		suffix = "";
		ResetTags();
		GetPage();
	}

	public void SortByNumberOfPlayedButtonPressed()
	{
		myCollection = false;
		suffix = "&sort=plays";
		ResetTags();
		GetPage();
	}

	private void ResetTags()
	{
		tags = "";
		if (_tagsOptionButton != null && _tagsOptionButton.ItemCount > 0)
		{
			_tagsOptionButton.Select(0);
		}
	}

	public void ExchangeButtonPressed()
	{
		DialogManager.Instance.DialogCreate("OnlineLevelExchange");
	}

	public void TypeOptionButtonItemSelected(int index)
	{
		switch (index)
		{
		case 0:
			suffix2 = "";
			break;
		case 1:
			suffix2 = "&finish_method=wave&round_mode=default&is_lucky=false";
			break;
		case 2:
			suffix2 = "&finish_method=vase";
			break;
		case 3:
			suffix2 = "&finish_method=izm";
			break;
		case 4:
			suffix2 = "&finish_method=izm2";
			break;
		case 5:
			suffix2 = "&round_mode=survival";
			break;
		case 6:
			suffix2 = "&round_mode=endless";
			break;
		case 7:
			suffix2 = "&is_lucky=true";
			break;
		}
		GetPage();
	}
}
