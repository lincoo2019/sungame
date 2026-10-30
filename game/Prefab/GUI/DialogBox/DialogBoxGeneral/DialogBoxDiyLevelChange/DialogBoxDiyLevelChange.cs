using Godot;

public partial class DialogBoxDiyLevelChange : DialogPopup
{
	public delegate void PlayEventHandler();

	public event PlayEventHandler OnPlay;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		this.OnPlay?.Invoke();
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
