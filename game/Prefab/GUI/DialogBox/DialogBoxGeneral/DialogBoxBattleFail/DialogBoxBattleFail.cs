using Godot;

[GlobalClass]
public partial class DialogBoxBattleFail : DialogPopup
{
	public MainButton restartButton;

	public MainButton backButton;

	public MainButton backBattleButton;

	public override void _Ready()
	{
		base._Ready();
		restartButton = GetNode<MainButton>("%RestartButton");
		backButton = GetNode<MainButton>("%BackButton");
		backBattleButton = GetNode<MainButton>("%BackBattleButton");
		GetNode<BaseButton>("%RestartButton").Pressed += RestartButtonPressed;
		GetNode<BaseButton>("%BackButton").Pressed += BackButtonPressed;
		GetNode<BaseButton>("%BackBattleButton").Pressed += BackBattleButtonPressed;
		if (Global.Instance.enterLevelMode == "OnlineLevel")
		{
			InternetServerManager.Instance.OnlineLevelPost(Global.Instance.enterLevelId, "failure");
			if (Global.Instance.enterLevelIsBattle)
			{
				Global.Instance.enterLevelIsBattleFinish = false;
				backBattleButton.Visible = true;
				restartButton.Visible = false;
				backButton.Visible = false;
			}
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			restartButton.Visible = false;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			restartButton.Visible = false;
			backButton.Visible = false;
			backBattleButton.Visible = false;
			textLabel.Clear();
			textLabel.AppendText("[center]游戏结束\n等待房主操作...[/center]");
			MultiPlayerManager.Instance.OnMatchLeft += _OnHostLeft;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (MultiPlayerManager.Instance != null)
		{
			MultiPlayerManager.Instance.OnMatchLeft -= _OnHostLeft;
		}
	}

	public void _OnHostLeft()
	{
		MultiPlayerManager.Instance.SendClientReady();
		SceneManager.Instance.ChangeScene("MainMenu");
	}

	public void RestartButtonPressed()
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendMatchState("game_result", MatchStateSerializer.Serialize(new GameResultDto
			{
				leave = true
			}));
		}
		GameSaveManager.Instance.DeleteLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
		SceneManager.Instance.ReloadScene();
		CloseDialog();
	}

	public void BackButtonPressed()
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
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
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		}
		CloseDialog();
	}

	public void BackBattleButtonPressed()
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendMatchState("game_result", MatchStateSerializer.Serialize(new GameResultDto
			{
				leave = true
			}));
		}
		SceneManager.Instance.ChangeScene("LevelEditorStage");
		CloseDialog();
	}
}
