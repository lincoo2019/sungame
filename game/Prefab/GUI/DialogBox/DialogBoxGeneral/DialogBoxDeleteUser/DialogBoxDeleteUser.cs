using Godot;

public partial class DialogBoxDeleteUser : DialogPopup
{
	public string deleteUser;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		GameSaveManager.Instance.DeleteUser(deleteUser);
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
