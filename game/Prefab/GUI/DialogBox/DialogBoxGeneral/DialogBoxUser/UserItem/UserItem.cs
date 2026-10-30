using Godot;

[GlobalClass]
public partial class UserItem : Button
{
	public delegate void ChooseEventHandler(string user);

	public event ChooseEventHandler OnChoose;

	public override void _Ready()
	{
		base.Pressed += OnPressed;
	}

	public void OnPressed()
	{
		this.OnChoose?.Invoke(base.Text);
	}
}
