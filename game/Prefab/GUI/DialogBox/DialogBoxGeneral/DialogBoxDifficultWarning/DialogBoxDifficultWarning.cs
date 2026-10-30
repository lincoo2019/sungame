using Godot;

public partial class DialogBoxDifficultWarning : DialogPopup
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%ConfirmButton").Pressed += ConfirmButtonPressed;
	}

	public void ConfirmButtonPressed()
	{
		CloseDialog();
	}
}
