using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class DailyChallengeLevelChoose : DialogBoxBase
{
	private const string DailyLevelApiOrigin = "https://api.pvzhe.com";

	private const int CardsPerPage = 2;

	private const double PageTweenDuration = 0.2;

	private static readonly Color PageButtonDisabledColor = new Color(0.48f, 0.48f, 0.48f, 0.7f);

	private static PackedScene _dailyChallengeLevelCardScene;

	private readonly Array<DailyChallengeLevelPreview> _cards = new Array<DailyChallengeLevelPreview>();

	private HBoxContainer _cardsContainer;

	private ScrollContainer _cardsScroll;

	private Label _dateLabel;

	private Label _statusLabel;

	private TextureButton _closeButton;

	private TextureButton _previousPageButton;

	private TextureButton _nextPageButton;

	private ScrollBar _horizontalScrollBar;

	private DailyChallengeLevelPreview _selectedCard;

	private Tween _pageTween;

	private bool _isBusy;

	private bool _isPaging;

	private bool _statusIsError;

	private int _currentPage;

	private int _pageCount;

	private string _statusMessage = "";

	private string _pendingDate = "";

	private string _pendingLevelId = "";

	private Dictionary _pendingMetadata = new Dictionary();

	[Export(PropertyHint.None, "")]
	public string date = "";

	public Godot.Collections.Array levelList = new Godot.Collections.Array();

	public NativeHttpRequest levelHttpRequest;

	public MainButton playButton;

	private static PackedScene DAILY_CHALLENGE_LEVEL_CARD_SCENE => _dailyChallengeLevelCardScene ?? (_dailyChallengeLevelCardScene = GD.Load<PackedScene>("uid://8c5010cwpik"));

	public Array<DailyChallengeLevelPreview> Cards => _cards;

	public DailyChallengeLevelPreview SelectedCard => _selectedCard;

	public ScrollContainer CardsScroll => _cardsScroll;

	public bool IsBusy => _isBusy;

	public string SelectedLevelId => _selectedCard?.LevelId ?? "";

	public string StatusText
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_statusLabel))
			{
				return "";
			}
			return _statusLabel.Text;
		}
	}

	internal Func<string, string[], Error> LevelRequestOverride { get; set; }

	internal Action<Json> BattleStartOverride { get; set; }

	public override void Init(Dictionary data)
	{
		date = data?.GetValueOrDefault("date", "").AsString() ?? "";
	}

	public override void _Ready()
	{
		base._Ready();
		_cardsContainer = GetNode<HBoxContainer>("%CardsContainer");
		_cardsScroll = GetNode<ScrollContainer>("%CardsScroll");
		_dateLabel = GetNode<Label>("%DateLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_closeButton = GetNode<TextureButton>("%CloseButton");
		_previousPageButton = GetNode<TextureButton>("%PreviousPageButton");
		_nextPageButton = GetNode<TextureButton>("%NextPageButton");
		_horizontalScrollBar = _cardsScroll.GetHScrollBar();
		playButton = GetNode<MainButton>("%PlayButton");
		levelHttpRequest = new NativeHttpRequest();
		AddChild(levelHttpRequest, forceReadableName: false, InternalMode.Disabled);
		levelHttpRequest.RequestCompleted += LevelHttpRequestCompleted;
		playButton.Pressed += PlayButtonPressed;
		_closeButton.Pressed += CloseButtonPressed;
		_previousPageButton.Pressed += PreviousPageButtonPressed;
		_nextPageButton.Pressed += NextPageButtonPressed;
		_horizontalScrollBar.ValueChanged += CardsScrollValueChanged;
		PopulateCards();
	}

	public void PopulateCards()
	{
		ClearCards();
		_selectedCard = null;
		_statusMessage = "";
		_statusIsError = false;
		levelList = new Godot.Collections.Array();
		playButton.Disabled = true;
		_dateLabel.Text = date;
		if (string.IsNullOrWhiteSpace(date))
		{
			SetStatus("未指定挑战日期", error: true);
			return;
		}
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			SetStatus("关卡数据尚未加载", error: true);
			return;
		}
		System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
		if (dAILY_LEVEL_DATA == null || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap") || dAILY_LEVEL_DATA["LevelDateMap"].VariantType != Variant.Type.Dictionary || !dAILY_LEVEL_DATA.ContainsKey("LevelMeta") || dAILY_LEVEL_DATA["LevelMeta"].VariantType != Variant.Type.Dictionary)
		{
			SetStatus("关卡数据尚未加载", error: true);
			return;
		}
		Dictionary dictionary = dAILY_LEVEL_DATA["LevelDateMap"].AsGodotDictionary();
		Dictionary dictionary2 = dAILY_LEVEL_DATA["LevelMeta"].AsGodotDictionary();
		if (!dictionary.ContainsKey(date) || dictionary[date].VariantType != Variant.Type.Array)
		{
			SetStatus("当日暂无可用关卡", error: true);
			return;
		}
		levelList = dictionary[date].AsGodotArray().Duplicate(deep: true);
		PackedScene dAILY_CHALLENGE_LEVEL_CARD_SCENE = DAILY_CHALLENGE_LEVEL_CARD_SCENE;
		if (!GodotObject.IsInstanceValid(dAILY_CHALLENGE_LEVEL_CARD_SCENE))
		{
			SetStatus("关卡预览资源加载失败", error: true);
			return;
		}
		foreach (Variant level in levelList)
		{
			string text = level.AsString();
			Dictionary metadata = new Dictionary();
			if (!string.IsNullOrWhiteSpace(text) && dictionary2.ContainsKey(text) && dictionary2[text].VariantType == Variant.Type.Dictionary)
			{
				metadata = dictionary2[text].AsGodotDictionary().Duplicate(deep: true);
			}
			DailyChallengeLevelPreview dailyChallengeLevelPreview = dAILY_CHALLENGE_LEVEL_CARD_SCENE.Instantiate<DailyChallengeLevelPreview>(PackedScene.GenEditState.Disabled);
			dailyChallengeLevelPreview.Bind(text, metadata, IsLevelFinished("DailyLevel-" + date + "-" + text));
			dailyChallengeLevelPreview.OnSelected += CardSelected;
			_cardsContainer.AddChild(dailyChallengeLevelPreview, forceReadableName: false, InternalMode.Disabled);
			_cards.Add(dailyChallengeLevelPreview);
		}
		ResetPagination();
		foreach (DailyChallengeLevelPreview card in _cards)
		{
			if (card.IsAvailable)
			{
				SelectCard(card, playAudio: false);
				return;
			}
		}
		SetStatus((levelList.Count == 0) ? "当日暂无可用关卡" : "当日关卡数据不可用", error: true);
	}

	private void ClearCards()
	{
		StopPageTween();
		_currentPage = 0;
		_pageCount = 0;
		if (GodotObject.IsInstanceValid(_cardsScroll))
		{
			_cardsScroll.ScrollHorizontal = 0;
		}
		foreach (DailyChallengeLevelPreview card in _cards)
		{
			if (GodotObject.IsInstanceValid(card))
			{
				card.OnSelected -= CardSelected;
				if (card.GetParent() == _cardsContainer)
				{
					_cardsContainer.RemoveChild(card);
				}
				card.QueueFree();
			}
		}
		_cards.Clear();
		RefreshPaginationUi();
	}

	private void ResetPagination()
	{
		StopPageTween();
		_currentPage = 0;
		_pageCount = Mathf.CeilToInt((float)_cards.Count / 2f);
		_cardsScroll.ScrollHorizontal = 0;
		RefreshPaginationUi();
	}

	private void PreviousPageButtonPressed()
	{
		NavigateToPage(_currentPage - 1);
	}

	private void NextPageButtonPressed()
	{
		NavigateToPage(_currentPage + 1);
	}

	private void NavigateToPage(int targetPage)
	{
		if (_isBusy || _isPaging || _pageCount <= 1)
		{
			return;
		}
		targetPage = Mathf.Clamp(targetPage, 0, _pageCount - 1);
		if (targetPage == _currentPage)
		{
			return;
		}
		_currentPage = targetPage;
		int pageScrollTarget = GetPageScrollTarget(targetPage);
		if (pageScrollTarget == _cardsScroll.ScrollHorizontal)
		{
			RefreshPaginationUi();
			return;
		}
		_isPaging = true;
		RefreshPaginationUi();
		if (GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
		}
		_pageTween = CreateTween();
		_pageTween.SetTrans(Tween.TransitionType.Quart);
		_pageTween.SetEase(Tween.EaseType.Out);
		_pageTween.TweenProperty(_cardsScroll, "scroll_horizontal", pageScrollTarget, 0.2);
		_pageTween.Finished += PageTweenFinished;
	}

	private void PageTweenFinished()
	{
		_pageTween = null;
		_isPaging = false;
		_cardsScroll.ScrollHorizontal = GetPageScrollTarget(_currentPage);
		RefreshPaginationUi();
	}

	private void CardsScrollValueChanged(double value)
	{
		if (!_isPaging && _pageCount > 1)
		{
			int num = FindNearestPage(value);
			if (num != _currentPage)
			{
				_currentPage = num;
				RefreshPaginationUi();
			}
		}
	}

	private int FindNearestPage(double scrollValue)
	{
		int result = 0;
		double num = 1.7976931348623157E+308;
		for (int i = 0; i < _pageCount; i++)
		{
			double num2 = Math.Abs(scrollValue - (double)GetPageScrollTarget(i));
			if (!(num2 >= num))
			{
				result = i;
				num = num2;
			}
		}
		return result;
	}

	private int GetPageScrollTarget(int page)
	{
		if (!GodotObject.IsInstanceValid(_horizontalScrollBar) || !GodotObject.IsInstanceValid(_cardsScroll))
		{
			return 0;
		}
		double val = Math.Max(0.0, _horizontalScrollBar.MaxValue - _horizontalScrollBar.Page);
		return Mathf.RoundToInt(Math.Min((float)Mathf.Clamp(page, 0, Math.Max(0, _pageCount - 1)) * _cardsScroll.Size.X, val));
	}

	private void StopPageTween()
	{
		if (GodotObject.IsInstanceValid(_pageTween))
		{
			_pageTween.Kill();
		}
		_pageTween = null;
		_isPaging = false;
	}

	private void RefreshPaginationUi()
	{
		bool flag = _pageCount > 1;
		if (GodotObject.IsInstanceValid(_previousPageButton))
		{
			_previousPageButton.Visible = flag;
			SetPageButtonState(_previousPageButton, !flag || _isBusy || _isPaging || _currentPage <= 0);
		}
		if (GodotObject.IsInstanceValid(_nextPageButton))
		{
			_nextPageButton.Visible = flag;
			SetPageButtonState(_nextPageButton, !flag || _isBusy || _isPaging || _currentPage >= _pageCount - 1);
		}
		RefreshStatusLabel();
	}

	private static void SetPageButtonState(TextureButton button, bool disabled)
	{
		button.Disabled = disabled;
		button.Modulate = (disabled ? PageButtonDisabledColor : Colors.White);
		button.MouseDefaultCursorShape = (CursorShape)(disabled ? 0 : 2);
	}

	private void CardSelected(DailyChallengeLevelPreview card)
	{
		if (!_isBusy && GodotObject.IsInstanceValid(card) && card.IsAvailable)
		{
			SelectCard(card, playAudio: true);
		}
	}

	private void SelectCard(DailyChallengeLevelPreview card, bool playAudio)
	{
		if (!GodotObject.IsInstanceValid(card) || !card.IsAvailable)
		{
			return;
		}
		foreach (DailyChallengeLevelPreview card2 in _cards)
		{
			card2.SetSelected(card2 == card);
		}
		_selectedCard = card;
		playButton.Disabled = false;
		SetStatus("已选择：" + card.DisplayName);
		if (playAudio && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
		}
	}

	public void PlayButtonPressed()
	{
		if (_isBusy || !GodotObject.IsInstanceValid(_selectedCard) || !_selectedCard.IsAvailable)
		{
			return;
		}
		_pendingDate = date;
		_pendingLevelId = _selectedCard.LevelId;
		_pendingMetadata = _selectedCard.Metadata.Duplicate(deep: true);
		SetBusy(busy: true);
		if (!TryBuildDailyLevelUrl(_selectedCard.ApiPath, out var levelUrl))
		{
			RestoreAfterLoadError("关卡下载地址无效");
			return;
		}
		string[] array = (GodotObject.IsInstanceValid(Global.Instance) ? Global.Instance.header : System.Array.Empty<string>());
		Error error = ((LevelRequestOverride != null) ? LevelRequestOverride(levelUrl, array) : levelHttpRequest.Request(levelUrl, array));
		if (error != Error.Ok)
		{
			RestoreAfterLoadError($"无法开始下载关卡（{error}）");
		}
	}

	private static bool TryBuildDailyLevelUrl(string apiPath, out string levelUrl)
	{
		levelUrl = "";
		if (!DailyChallengeLevelPreview.IsValidApiPath(apiPath))
		{
			return false;
		}
		if (!Uri.TryCreate("https://api.pvzhe.com", UriKind.Absolute, out Uri result) || !Uri.TryCreate(result, apiPath, out Uri result2) || !string.Equals(result2.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !string.Equals(result2.Host, result.Host, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		levelUrl = result2.AbsoluteUri;
		return true;
	}

	public void LevelHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (!_isBusy)
		{
			return;
		}
		if (result != 0L)
		{
			string message = (GodotObject.IsInstanceValid(Global.Instance) ? Global.Instance.GetHTTPRequestErrorMessage(result) : "获取关卡失败，请重新尝试");
			RestoreAfterLoadError(message);
			return;
		}
		if (responseCode < 200 || responseCode >= 300)
		{
			RestoreAfterLoadError($"获取关卡失败（HTTP {responseCode}）");
			return;
		}
		Json json = new Json();
		if (json.Parse(body?.GetStringFromUtf8() ?? "") != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
		{
			RestoreAfterLoadError("关卡数据格式错误，请重新尝试");
			return;
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		if (HasServerError(dictionary))
		{
			RestoreAfterLoadError("服务器未返回有效关卡，请重新尝试");
			return;
		}
		if (!_pendingMetadata.ContainsKey("reward") || _pendingMetadata["reward"].VariantType != Variant.Type.Dictionary)
		{
			RestoreAfterLoadError("关卡奖励数据无效，请重新尝试");
			return;
		}
		Dictionary dictionary2 = _pendingMetadata["reward"].AsGodotDictionary();
		string text = dictionary2.GetValueOrDefault("type", "").AsString();
		if (string.IsNullOrWhiteSpace(text) || !dictionary2.ContainsKey("value"))
		{
			RestoreAfterLoadError("关卡奖励数据无效，请重新尝试");
			return;
		}
		string text2 = "DailyLevel-" + _pendingDate + "-" + _pendingLevelId;
		dictionary["Name"] = text2;
		dictionary["Reward"] = new Dictionary
		{
			["RewardType"] = text,
			["RewardFirst"] = dictionary2["value"]
		};
		Json json2 = new Json();
		if (json2.Parse(Json.Stringify(dictionary)) != Error.Ok || json2.Data.VariantType != Variant.Type.Dictionary)
		{
			RestoreAfterLoadError("关卡数据处理失败，请重新尝试");
			return;
		}
		GameSaveManager.Instance.SaveDailyLevel(text2, json2);
		_closeButton.Disabled = true;
		SetStatus("关卡加载完成，正在进入…");
		if (BattleStartOverride != null)
		{
			BattleStartOverride(json2);
		}
		else
		{
			ToBattle(json2);
		}
	}

	private static bool HasServerError(Dictionary data)
	{
		if (data == null || !data.ContainsKey("error") || data["error"].VariantType == Variant.Type.Nil)
		{
			return false;
		}
		Variant variant = data["error"];
		Variant.Type variantType = variant.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 0:
				return variant.AsBool();
			case 1:
				return variant.AsInt64() != 0;
			case 2:
				return !Mathf.IsZeroApprox(variant.AsDouble());
			case 3:
				return !string.IsNullOrWhiteSpace(variant.AsString());
			}
		}
		return true;
	}

	private void SetBusy(bool busy)
	{
		_isBusy = busy;
		foreach (DailyChallengeLevelPreview card in _cards)
		{
			card.SetInteractionEnabled(!busy);
		}
		playButton.Disabled = busy || !GodotObject.IsInstanceValid(_selectedCard) || !_selectedCard.IsAvailable;
		if (busy)
		{
			SetStatus("正在加载关卡…");
		}
		RefreshPaginationUi();
	}

	private void RestoreAfterLoadError(string message)
	{
		levelHttpRequest?.CancelRequest();
		_isBusy = false;
		foreach (DailyChallengeLevelPreview card in _cards)
		{
			card.SetInteractionEnabled(enabled: true);
		}
		playButton.Disabled = !GodotObject.IsInstanceValid(_selectedCard) || !_selectedCard.IsAvailable;
		_pendingDate = "";
		_pendingLevelId = "";
		_pendingMetadata = new Dictionary();
		SetStatus(message, error: true);
		RefreshPaginationUi();
		if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BroadCastFloatCreate(message, Colors.Red);
		}
	}

	private void SetStatus(string text, bool error = false)
	{
		_statusMessage = text ?? "";
		_statusIsError = error;
		RefreshStatusLabel();
	}

	private void RefreshStatusLabel()
	{
		if (GodotObject.IsInstanceValid(_statusLabel))
		{
			_statusLabel.Text = ((_pageCount <= 1) ? _statusMessage : (string.IsNullOrWhiteSpace(_statusMessage) ? $"第 {_currentPage + 1}/{_pageCount} 页" : $"第 {_currentPage + 1}/{_pageCount} 页 · {_statusMessage}"));
			_statusLabel.AddThemeColorOverride("font_color", _statusIsError ? new Color(0.78f, 0.05f, 0.02f) : new Color(0.12f, 0.38f, 0.08f));
		}
	}

	private static bool IsLevelFinished(string levelName)
	{
		GameSaveManager instance = GameSaveManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.config == null || string.IsNullOrWhiteSpace(levelName))
		{
			return false;
		}
		string userCurrent = instance.GetUserCurrent();
		if (string.IsNullOrWhiteSpace(userCurrent) || instance.config.saveDictionary == null || !instance.config.saveDictionary.ContainsKey(userCurrent))
		{
			return false;
		}
		Variant variant = instance.config.saveDictionary[userCurrent];
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		if (!dictionary.ContainsKey("Level") || dictionary["Level"].VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary2 = dictionary["Level"].AsGodotDictionary();
		if (!dictionary2.ContainsKey(levelName) || dictionary2[levelName].VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary3 = dictionary2[levelName].AsGodotDictionary();
		if (!dictionary3.ContainsKey("Key") || dictionary3["Key"].VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		return dictionary3["Key"].AsGodotDictionary().GetValueOrDefault("Finish", 0).AsInt32() > 0;
	}

	public void CloseButtonPressed()
	{
		levelHttpRequest?.CancelRequest();
		if (GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
		}
		CloseDialog();
	}

	public async void ToBattle(Json json)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
		{
			data = json
		};
		towerDefenseLevelConfig.Init();
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		Global.Instance.enterLevelMode = "DailyLevel";
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			await MultiPlayerManager.Instance.StartCompatibleGameAsync();
		}
		else
		{
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}
}
