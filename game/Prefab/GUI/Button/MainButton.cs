using Godot;

[Tool]
[GlobalClass]
public partial class MainButton : TextureButton
{
	public Label label;

	private string _text = "Test";

	[Export(PropertyHint.None, "")]
	public string text
	{
		get
		{
			return _text;
		}
		set
		{
			if (!(_text == value))
			{
				_text = value ?? string.Empty;
				RefreshLayout();
			}
		}
	}

	public override void _Ready()
	{
		label = GetNode<Label>("%TextLabel");
		label.Text = _text;
		base.Pressed += _ButtonPressed;
		base.Resized += RefreshLayout;
		Label nodeOrNull = GetNodeOrNull<Label>("TextLabel2");
		if (nodeOrNull != null)
		{
			nodeOrNull.Resized += OnTextLabel2Resized;
		}
		RefreshLayout();
	}

	private void RefreshLayout()
	{
		if (GodotObject.IsInstanceValid(label))
		{
			label.Text = _text;
			label.Size = base.Size - new Vector2(20f, 0f);
			label.Position = new Vector2(10f, 0f);
			OnTextLabel2Resized();
		}
	}

	public void _ButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
	}

	public void OnTextLabel2Resized()
	{
		Label nodeOrNull = GetNodeOrNull<Label>("TextLabel2");
		if (nodeOrNull != null)
		{
			nodeOrNull.Size = new Vector2(base.Size.X - 18f, nodeOrNull.Size.Y);
		}
	}
}
