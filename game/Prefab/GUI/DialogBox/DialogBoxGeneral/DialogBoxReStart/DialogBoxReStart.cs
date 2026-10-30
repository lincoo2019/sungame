using Godot;

public partial class DialogBoxReStart : DialogPopup
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		if (Global.Instance.enterLevelMode == "OnlineLevel")
		{
			InternetServerManager.Instance.OnlineLevelPost(Global.Instance.enterLevelId, "failure");
		}
		GameSaveManager.Instance.DeleteLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
		SceneManager.Instance.ReloadScene();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
