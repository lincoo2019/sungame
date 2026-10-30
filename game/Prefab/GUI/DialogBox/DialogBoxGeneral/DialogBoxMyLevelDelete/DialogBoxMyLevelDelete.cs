using Godot;

public partial class DialogBoxMyLevelDelete : DialogPopup
{
	public delegate void PressDeleteEventHandler();

	public event PressDeleteEventHandler OnPressDelete;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		this.OnPressDelete?.Invoke();
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
