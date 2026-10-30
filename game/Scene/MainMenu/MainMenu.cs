using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class MainMenu : Control
{
	public Camera2D camera;

	public Marker2D moreGameMarker;

	public Marker2D defaultMarker;

	public TextureButton helpButton;

	public TextureButton optionButton;

	public TextureButton quitButton;

	public TextureButton woodButton;

	public TextureButton adventureButton;

	public TextureButton challengeButton;

	public TextureButton survivalButton;

	public RichTextLabel woodNameLabel;

	public TextureButton woodFileButton;

	public AnimationPlayer selectAnimationPlayer;

	public Label currentVersionLabel;

	public LinkButton newVersionLinkButton;

	public TextureButton shopButton;

	public bool choose;

	public bool wait;

	public override void _Ready()
	{
		ResourceManager.Instance.RequireFullGameplayResourcesReady("MainMenu");
		camera = GetNode<Camera2D>("%Camera");
		moreGameMarker = GetNode<Marker2D>("%MoreGameMarker");
		defaultMarker = GetNode<Marker2D>("%DefaultMarker");
		helpButton = GetNode<TextureButton>("%HelpTextureButton");
		optionButton = GetNode<TextureButton>("%OptionTextureButton");
		quitButton = GetNode<TextureButton>("%QuitTextureButton");
		woodButton = GetNode<TextureButton>("%WoodFileButton");
		adventureButton = GetNode<TextureButton>("%AdventureButton");
		challengeButton = GetNode<TextureButton>("%ChallengeButton");
		survivalButton = GetNode<TextureButton>("%SurvivalButton");
		woodNameLabel = GetNode<RichTextLabel>("%WoodNameLabel");
		woodFileButton = GetNode<TextureButton>("%WoodFileButton");
		selectAnimationPlayer = GetNode<AnimationPlayer>("%SelectAnimationPlayer");
		currentVersionLabel = GetNode<Label>("%CurrentVersionLabel");
		newVersionLinkButton = GetNode<LinkButton>("%NewVersionLinkButton");
		shopButton = GetNode<TextureButton>("%ShopButton");
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged += OnGlobalFeatureChanged;
			GlobalFeatureManager.Instance.FeaturesReloaded += RefreshShopVisibility;
		}
		RefreshShopVisibility();
		if ((!Global.Instance.newVersionSkip && Global.Instance.hasNewVersion) || GameSaveManager.Instance.GetUserCurrent() == "")
		{
			wait = true;
		}
		currentVersionLabel.Text = $"当前版本:{Global.Instance.version}";
		AudioManager.Instance.AudioPlay("MainMenu", AudioManagerEnum.TYPE.MUSIC);
		if (Global.Instance.mainMenuShowMoreModes)
		{
			Global.Instance.mainMenuShowMoreModes = false;
			camera.GlobalPosition = new Vector2(moreGameMarker.GlobalPosition.X, camera.GlobalPosition.Y);
		}
		if (Global.Instance.isMultiplayerMode)
		{
			DialogManager.Instance.DialogCreate("MultiplayerLobby");
			if (MultiPlayerManager.Instance.currentMatchId == "")
			{
				Global.Instance.isMultiplayerMode = false;
				Global.Instance.isMultiplayerHost = false;
			}
		}
		if (Global.Instance.enterLevelMode == "LevelChoose" && Global.Instance.currentLevelChoose == "TryLevel")
		{
			DialogManager.Instance.DialogCreate("TryLevel");
		}
		adventureButton.Pressed += AdventureButtonPressed;
		XWModLevelSession.Clear();
		challengeButton.Pressed += ChallengeButtonPressed;
		survivalButton.Pressed += SurvivalButtonPressed;
		helpButton.Pressed += HelpPressed;
		optionButton.Pressed += OptionButtonPressed;
		quitButton.Pressed += ExitButtonPressed;
		shopButton.Pressed += ShopButtonPressed;
		woodFileButton.Pressed += WoodFileButtonPressed;
		selectAnimationPlayer.AnimationFinished += AnimeFinish;
		GetNode<BaseButton>("Background/MenuTexture/DiyButton").Pressed += DiyButtonPressed;
		GetNode<BaseButton>("Background/MenuTexture/MoreTexture/MoreButton").Pressed += MoreButtonPressed;
		GetNode<BaseButton>("Background/MenuTexture/AlmanacButton").Pressed += AlmanacButtonPressed;
		GetNode<BaseButton>("Background/MoreBackground/MoreBackButton").Pressed += MoreBackButtonPressed;
		GetNode<BaseButton>("Background/MoreBackground/MiniGameButton").Pressed += MiniGameButtonPresed;
		GetNode<BaseButton>("Background/MoreBackground/PuzzleGameButton").Pressed += PuzzleGameButtonPressed;
		GetNode<BaseButton>("Background/MoreBackground/BattleGameButton").Pressed += BattleGameButtonPresed;
		GetNode<BaseButton>("Background/MoreBackground/IZM2GameButton").Pressed += IZM2GameButtonPresed;
		GetNode<BaseButton>("Background/MoreBackground/StarsExchangeButton").Pressed += StarsExchangeButtonPresed;
		GetNode<BaseButton>("Background/MoreBackground/HybridParkGameButton").Pressed += HybridParkGameButtonPressed;
		GetNode<BaseButton>("Wood/DailyChallengeButton").Pressed += DailyChallengeButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameSaveManager.Instance.GetUserCurrent() != "")
		{
			woodNameLabel.Text = GameSaveManager.Instance.GetUserCurrent() + "!";
		}
	}

	public override void _ExitTree()
	{
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged -= OnGlobalFeatureChanged;
			GlobalFeatureManager.Instance.FeaturesReloaded -= RefreshShopVisibility;
		}
		base._ExitTree();
	}

	private void OnGlobalFeatureChanged(string featureId, int _oldValue, int _newValue)
	{
		if (featureId == "Shop")
		{
			RefreshShopVisibility();
		}
	}

	private void RefreshShopVisibility()
	{
		if (GodotObject.IsInstanceValid(shopButton))
		{
			shopButton.Visible = GlobalFeatureManager.Instance?.IsUnlocked("Shop") ?? false;
		}
	}

	public void AdventureButtonPressed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "Adventure";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
			adventureButton.GlobalPosition += Vector2.One * 2f;
		}
	}

	public void ChallengeButtonPressed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "Challenge";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
			challengeButton.GlobalPosition += Vector2.One * 2f;
		}
	}

	public void SurvivalButtonPressed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "Survival";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
			survivalButton.GlobalPosition += Vector2.One * 2f;
		}
	}

	public void PuzzleGameButtonPressed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "Puzzle";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
		}
	}

	public void MiniGameButtonPresed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "MiniGames";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
		}
	}

	public void IZM2GameButtonPresed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "IZM2";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
		}
	}

	public void BattleGameButtonPresed()
	{
		if (!wait)
		{
			DialogManager.Instance.DialogCreate("MultiplayerLobby");
		}
	}

	public void StarsExchangeButtonPresed()
	{
		if (!wait)
		{
			DialogManager.Instance.DialogCreate("StarExchange");
		}
	}

	public void HybridParkGameButtonPressed()
	{
		if (!wait)
		{
			Global.Instance.currentLevelChoose = "HybridPark";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
		}
	}

	public void MoreButtonPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("GraveButtonPress");
			Tween tween = camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quad);
			tween.TweenProperty(camera, "global_position:x", moreGameMarker.GlobalPosition.X, 1.0);
		}
	}

	public void AlmanacButtonPressed()
	{
		if (!wait)
		{
			ResourceManager.Instance.RequireFullGameplayResourcesReady("AlmanacButtonPressed");
			DialogManager.Instance.DialogCreate("Almanac");
		}
	}

	public void ShopButtonPressed()
	{
		if (!wait)
		{
			GlobalFeatureManager instance = GlobalFeatureManager.Instance;
			if (instance != null && instance.IsUnlocked("Shop"))
			{
				DialogManager.Instance.DialogCreate("Shop");
			}
		}
	}

	public void DiyButtonPressed()
	{
		if (!wait)
		{
			if (Global.Instance.hasNewVersion)
			{
				BroadCastManager.Instance.BroadCastFloatCreate("使用此功能需更新至最新版本", Colors.Red);
			}
			else
			{
				SceneManager.Instance.ChangeScene("LevelEditorStage");
			}
		}
	}

	public void MoreBackButtonPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("GraveButtonPress");
			Tween tween = camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quad);
			tween.TweenProperty(camera, "global_position:x", defaultMarker.GlobalPosition.X, 1.0);
		}
	}

	public void HelpPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			DialogManager.Instance.DialogCreate("Help");
		}
	}

	public void OptionButtonPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			DialogManager.Instance.DialogCreate("MainMenuOption");
		}
	}

	public void ExitButtonPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			DialogManager.Instance.DialogCreate("ExitGame");
		}
	}

	public void NewUser()
	{
		DialogManager.Instance.DialogCreate("NewUser");
	}

	public async void WoodFileButtonPressed()
	{
		if (!wait)
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			await DialogManager.Instance.DialogCreate("User").WaitForClose();
			RequestReady();
		}
	}

	public async void DailyChallengeButtonPressed()
	{
		if (Global.Instance.hasNewVersion)
		{
			BroadCastManager.Instance.BroadCastFloatCreate("使用此功能需更新至最新版本", Colors.Red);
			return;
		}
		Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
		int curYear = (int)(long)datetimeDictFromSystem["year"];
		int curMonth = (int)(long)datetimeDictFromSystem["month"];
		if (InternetServerManager.Instance.DailyLevelMonthNeedRefresh(curYear, curMonth))
		{
			bool forceRefresh = InternetServerManager.Instance.dailyLevelLoadedMonths.ContainsKey($"{curYear}-{curMonth:D2}");
			InternetServerManager.Instance.GetDailyLevel(curYear, curMonth, forceRefresh);
			ulong startMs = Time.GetTicksMsec();
			while (InternetServerManager.Instance.IsDailyLevelRequestPending(curYear, curMonth) && Time.GetTicksMsec() - startMs <= 15000)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			if (InternetServerManager.Instance.DailyLevelMonthNeedRefresh(curYear, curMonth))
			{
				BroadCastManager.Instance.BroadCastFloatCreate("正在获取每日挑战列表,请稍后重试", Colors.Red);
				return;
			}
		}
		AudioManager.Instance.AudioPlay("ButtonPress");
		await DialogManager.Instance.DialogCreate("DailyChallenge").WaitForClose();
	}

	public async void AnimeFinish(StringName animName)
	{
		if (Global.Instance.hasNewVersion)
		{
			newVersionLinkButton.Visible = true;
			newVersionLinkButton.Text = $"最新版本:{Global.Instance.newVersion}(点此跳转更新)";
			newVersionLinkButton.Uri = Global.Instance.uri;
			if (!Global.Instance.newVersionSkip)
			{
				DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("NewVersion");
				dialogBoxBase.Set("uri", Global.Instance.uri);
				dialogBoxBase.Set("message", InternetServerManager.Instance.versionMessage);
				await dialogBoxBase.WaitForClose();
				Global.Instance.newVersionSkip = true;
			}
		}
		wait = false;
		if (animName == (StringName)"Enter" && GameSaveManager.Instance.GetUserCurrent() == "")
		{
			NewUser();
		}
	}
}
