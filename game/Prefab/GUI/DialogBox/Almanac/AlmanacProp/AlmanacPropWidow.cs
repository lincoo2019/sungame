using Godot;

[GlobalClass]
public partial class AlmanacPropWidow : Control
{
	public delegate void PressedShovelEventHandler(ShovelConfig config);

	public delegate void PressedMowerEventHandler(MowerConfig config);

	public Control spriteNode;

	public string type = "Shovel";

	public ShovelConfig shovelConfig;

	public MowerConfig mowerConfig;

	public Sprite2D sprite;

	public event PressedShovelEventHandler OnPressedShovel;

	public event PressedMowerEventHandler OnPressedMower;

	public override void _Ready()
	{
		spriteNode = GetNode<Control>("%SpriteNode");
		GetNode<BaseButton>("SpriteBrightButton/Transform/Button").Pressed += Pressed;
	}

	public void InitShovel(ShovelConfig config)
	{
		type = "Shovel";
		shovelConfig = config;
		sprite = new Sprite2D();
		sprite.Texture = shovelConfig.texture;
		sprite.LightMask = 0;
		sprite.Scale = Vector2.One * 0.8f;
		spriteNode.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
	}

	public void InitMower(MowerConfig config)
	{
		type = "Mower";
		mowerConfig = config;
		sprite = new Sprite2D();
		sprite.Texture = mowerConfig.texture;
		sprite.LightMask = 0;
		sprite.Scale = Vector2.One * 0.5f;
		spriteNode.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
	}

	public void Pressed()
	{
		AudioManager.Instance.AudioPlay("PacketPick");
		string text = type;
		if (!(text == "Shovel"))
		{
			if (text == "Mower")
			{
				this.OnPressedMower?.Invoke(mowerConfig);
			}
		}
		else
		{
			this.OnPressedShovel?.Invoke(shovelConfig);
		}
	}
}
