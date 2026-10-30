using Godot;

[GlobalClass]
public partial class PacketCategoryButton : Control
{
	public delegate void ChooseEventHandler(string _category);

	[Export(PropertyHint.None, "")]
	public string category = "White";

	public event ChooseEventHandler OnChoose;

	public override void _Ready()
	{
		GetNode<SpriteBrightButton>("SpriteBrightButton").OnPressed += ButtonPressed;
	}

	public void ButtonPressed()
	{
		this.OnChoose?.Invoke(category);
	}
}
