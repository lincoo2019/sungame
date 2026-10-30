using Godot;

public partial class DialogBoxLevelEditorTips : DialogPopup
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		SceneManager.Instance.ChangeScene("MainMenu");
		CloseDialog();
	}
}
