using Godot;

[GlobalClass]
public partial class DragMenuSelectItem : Control
{
	public delegate void SelectEventHandler(int index);

	public Control graphics;

	public Sprite2D sprite;

	public Button button;

	public int index = -1;

	public bool @lock;

	[Export(PropertyHint.None, "")]
	public Vector2 textureDisplaySize { get; set; } = Vector2.Zero;

	public Button buttonNode => button;

	public event SelectEventHandler OnSelect;

	public override void _Ready()
	{
		graphics = GetNode<Control>("%Graphics");
		sprite = GetNode<Sprite2D>("%Sprite");
		button = GetNode<Button>("%Button");
		sprite.TextureChanged += OnSpriteTextureChanged;
		button.Pressed += OnButtonPressed;
	}

	public void OnSpriteTextureChanged()
	{
		if (GodotObject.IsInstanceValid(sprite.Texture))
		{
			Vector2 size = sprite.Texture.GetSize();
			Vector2 vector = ((textureDisplaySize.X > 0f && textureDisplaySize.Y > 0f) ? textureDisplaySize : size);
			sprite.Scale = vector / size;
			button.Size = vector;
			button.Position = -button.Size / 2f;
		}
	}

	public void OnButtonPressed()
	{
		this.OnSelect?.Invoke(index);
	}
}
