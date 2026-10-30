using Godot;

public partial class DialogBoxChoose : DialogPopup
{
	public delegate void ChooseTrueEventHandler();

	public delegate void ChooseFalseEventHandler();

	public string text = "";

	public event ChooseTrueEventHandler OnChooseTrue;

	public event ChooseFalseEventHandler OnChooseFalse;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (textLabel.Text != text)
		{
			textLabel.Clear();
			textLabel.AppendText(text);
		}
	}

	public void TrueButtonPressed()
	{
		this.OnChooseTrue?.Invoke();
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		this.OnChooseFalse?.Invoke();
		CloseDialog();
	}
}
