using Godot;

[Tool]
[GlobalClass]
public partial class NinePatchButtonBase : MarginContainer
{
	public delegate void PressedEventHandler();

	public delegate void MouseEnteredEventHandler();

	public delegate void MouseExitedEventHandler();

	public NinePatchRect ninePatchTexture;

	public Label labelText;

	private bool _disable;

	private string _text = "";

	private Texture2D _normalTexture;

	private Texture2D _pressedTexture;

	private Texture2D _hoverTexture;

	[Export(PropertyHint.None, "")]
	public string downSfx = "ButtonClickPress";

	[Export(PropertyHint.None, "")]
	public string upSfx = "ButtonClickRelease";

	[Export(PropertyHint.None, "")]
	public bool disable
	{
		get
		{
			return _disable;
		}
		set
		{
			_disable = value;
			if (_disable)
			{
				ninePatchTexture?.SetDeferred("texture", normalTexture);
			}
		}
	}

	[Export(PropertyHint.MultilineText, "")]
	public string text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
			if (labelText != null)
			{
				labelText.Text = value;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Texture2D normalTexture
	{
		get
		{
			return _normalTexture;
		}
		set
		{
			_normalTexture = value;
			if (ninePatchTexture != null)
			{
				ninePatchTexture.Texture = value;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Texture2D pressedTexture
	{
		get
		{
			return _pressedTexture;
		}
		set
		{
			_pressedTexture = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public Texture2D hoverTexture
	{
		get
		{
			return _hoverTexture;
		}
		set
		{
			_hoverTexture = value;
		}
	}

	public event PressedEventHandler OnPressed;

	public event MouseEnteredEventHandler OnMouseEntered;

	public event MouseExitedEventHandler OnMouseExited;

	public override void _Ready()
	{
		ninePatchTexture = GetNode<NinePatchRect>("%NinePatchTexture");
		labelText = GetNode<Label>("%LabelText");
		labelText.Text = text;
		ninePatchTexture.Texture = normalTexture;
		TextureButton node = GetNode<TextureButton>("%TextureButton");
		node.Pressed += _Pressed;
		node.ButtonDown += ButtonDown;
		node.ButtonUp += ButtonUp;
		node.MouseEntered += _MouseEntered;
		node.MouseExited += _MouseExited;
		OnReady();
	}

	public virtual void OnReady()
	{
	}

	public void _Pressed()
	{
		if (!disable)
		{
			ninePatchTexture.Texture = pressedTexture;
			this.OnPressed?.Invoke();
		}
	}

	public void _MouseEntered()
	{
		if (!disable)
		{
			ninePatchTexture.Texture = hoverTexture;
			this.OnMouseEntered?.Invoke();
		}
	}

	public void _MouseExited()
	{
		if (!disable)
		{
			ninePatchTexture.Texture = normalTexture;
			this.OnMouseExited?.Invoke();
		}
	}

	public void ButtonDown()
	{
		if (!disable && downSfx != "")
		{
			AudioManager.Instance.AudioPlay(downSfx);
		}
	}

	public void ButtonUp()
	{
		if (!disable && upSfx != "")
		{
			AudioManager.Instance.AudioPlay(upSfx);
		}
	}
}
