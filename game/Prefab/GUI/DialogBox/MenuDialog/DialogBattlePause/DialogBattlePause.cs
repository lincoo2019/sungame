using Godot;

[GlobalClass]
public partial class DialogBattlePause : MenuDialogBase
{
	private MainButton handbookButton;

	private MainButton restartButton;

	private MainButton mainMenuButton;

	private MainButton levelEditorButton;

	public override void _Ready()
	{
		handbookButton = GetNode<MainButton>("%HandbookButton");
		restartButton = GetNode<MainButton>("%RestartButton");
		mainMenuButton = GetNode<MainButton>("%MainMenuButton");
		levelEditorButton = GetNode<MainButton>("%LevelEditorButton");
		GetNode<BaseButton>("%HandbookButton").Pressed += HandbookButtonPressed;
		GetNode<BaseButton>("%LevelEditorButton").Pressed += LevelEditorButtonPressed;
		GetNode<BaseButton>("%RestartButton").Pressed += RestartButtonPressed;
		GetNode<BaseButton>("%MainMenuButton").Pressed += MainMenuButtonPressed;
		base._Ready();
		if (Global.Instance.isEditor)
		{
			levelEditorButton.Visible = true;
			handbookButton.Visible = false;
			mainMenuButton.Visible = false;
		}
		if (Global.Instance.enterLevelIsBattle)
		{
			restartButton.Visible = false;
			mainMenuButton.Visible = false;
		}
		if (TowerDefenseManager.GetGameMethod() == TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ && TowerDefenseManager.Instance.IsGameRunning())
		{
			restartButton.Visible = false;
		}
		if (Global.Instance.isMultiplayerMode)
		{
			pasue = false;
			if (GetTree().Paused)
			{
				GetTree().Paused = false;
			}
			restartButton.Visible = false;
			mainMenuButton.Visible = false;
			levelEditorButton.Visible = false;
			if (!MultiPlayerManager.Instance.isHost)
			{
				handbookButton.Visible = false;
			}
		}
	}

	public void HandbookButtonPressed()
	{
		if (!Global.Instance.isMultiplayerMode)
		{
			GameSaveManager.Instance.SaveLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendMatchState("game_result", MatchStateSerializer.Serialize(new GameResultDto
			{
				leave = true
			}));
		}
		switch (Global.Instance.enterLevelMode)
		{
		case "ModLevel":
		case "LevelChoose":
			if (Global.Instance.currentLevelChoose != "TryLevel")
			{
				SceneManager.Instance.ChangeScene("LevelChoose");
			}
			else
			{
				SceneManager.Instance.ChangeScene("MainMenu");
			}
			break;
		case "DailyLevel":
			SceneManager.Instance.ChangeScene("MainMenu");
			break;
		case "DiyLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "LoadLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "OnlineLevel":
			InternetServerManager.Instance.OnlineLevelPost(Global.Instance.enterLevelId);
			if (Global.Instance.enterLevelIsBattle)
			{
				Global.Instance.enterLevelIsBattleFinish = false;
			}
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		}
	}

	public void RestartButtonPressed()
	{
		DialogCreate("ReStart");
	}

	public void MainMenuButtonPressed()
	{
		if (!Global.Instance.isMultiplayerMode)
		{
			GameSaveManager.Instance.SaveLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
		}
		if (Global.Instance.enterLevelMode == "OnlineLevel")
		{
			InternetServerManager.Instance.OnlineLevelPost(Global.Instance.enterLevelId);
		}
		SceneManager.Instance.ChangeScene("MainMenu");
	}

	public void LevelEditorButtonPressed()
	{
		if (!Global.Instance.isMultiplayerMode)
		{
			GameSaveManager.Instance.SaveLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
		}
		if (Global.Instance.enterLevelMode == "OnlineLevel")
		{
			InternetServerManager.Instance.OnlineLevelPost(Global.Instance.enterLevelId);
		}
		SceneManager.Instance.ChangeScene("LevelEditorStage");
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!IsQueuedForDeletion() && inputEvent.IsActionPressed("Pause"))
		{
			GetViewport().SetInputAsHandled();
			CloseDialog();
		}
	}
}
