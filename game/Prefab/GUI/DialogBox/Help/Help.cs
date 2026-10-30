using Godot;

[GlobalClass]
public partial class Help : DialogBoxBase
{
	private static Texture2D _helpMobile;

	private TextureRect helpTexture;

	private static Texture2D HelpMobile => _helpMobile ?? (_helpMobile = GD.Load<Texture2D>("res://Asset/Texture/GUI/Help/HelpMobile.png"));

	public override void _Ready()
	{
		base._Ready();
		helpTexture = GetNode<TextureRect>("%HelpTexture");
		GetNode<NinePatchButtonBase>("%BackButton").OnPressed += BackButtonPressed;
		if (Global.Instance.isMobile)
		{
			helpTexture.Texture = HelpMobile;
		}
	}

	public void BackButtonPressed()
	{
		CloseDialog();
	}
}
