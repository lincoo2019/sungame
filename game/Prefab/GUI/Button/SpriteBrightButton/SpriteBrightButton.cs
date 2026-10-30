using Godot;

[Tool]
[GlobalClass]
public partial class SpriteBrightButton : TextureRect
{
	public delegate void PressedEventHandler();

	public delegate void MouseEnteredEventHandler();

	public delegate void MouseExitedEventHandler();

	public Button button;

	private bool _autoSize;

	[Export(PropertyHint.None, "")]
	public bool disabled;

	[Export(PropertyHint.None, "")]
	public bool autoSize
	{
		get
		{
			return _autoSize;
		}
		set
		{
			if (_autoSize != value)
			{
				_autoSize = value;
				RefreshButtonRect();
			}
		}
	}

	public event PressedEventHandler OnPressed;

	public event MouseEnteredEventHandler OnMouseEntered;

	public event MouseExitedEventHandler OnMouseExited;

	public override void _Ready()
	{
		button = GetNode<Button>("%Button");
		button.MouseEntered += _MouseEntered;
		button.MouseExited += _MouseExited;
		button.Pressed += _Pressed;
		base.Material = (Material)base.Material.Duplicate();
		base.Resized += RefreshButtonRect;
		RefreshButtonRect();
	}

	private void RefreshButtonRect()
	{
		if (_autoSize && GodotObject.IsInstanceValid(button))
		{
			button.Position = Vector2.Zero;
			button.Size = base.Size;
		}
	}

	public void _Pressed()
	{
		if (!disabled)
		{
			this.OnPressed?.Invoke();
			AudioManager.Instance.AudioPlay("ButtonPress");
		}
	}

	public void _MouseEntered()
	{
		this.OnMouseEntered?.Invoke();
		if (!disabled)
		{
			((ShaderMaterial)base.Material).SetShaderParameter("brightStrength", 0.3);
		}
	}

	public void _MouseExited()
	{
		this.OnMouseExited?.Invoke();
		if (!disabled)
		{
			((ShaderMaterial)base.Material).SetShaderParameter("brightStrength", 0.0);
		}
	}
}
