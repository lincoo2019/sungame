using Godot;

public partial class DialogBoxExitGame : DialogPopup
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		GameSaveManager.Instance.Save();
		GetTree().Quit();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
