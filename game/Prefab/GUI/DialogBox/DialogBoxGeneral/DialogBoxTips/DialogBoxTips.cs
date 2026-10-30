using Godot;

public partial class DialogBoxTips : DialogPopup
{
	public string text = "";

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%ConfirmButton").Pressed += ConfirmButtonPressed;
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

	public void ConfirmButtonPressed()
	{
		CloseDialog();
	}
}
