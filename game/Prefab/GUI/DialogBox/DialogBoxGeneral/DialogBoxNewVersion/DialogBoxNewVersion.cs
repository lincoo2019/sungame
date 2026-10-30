using Godot;

public partial class DialogBoxNewVersion : DialogPopup
{
	public string uri = "";

	private string _message = "";

	private MarginContainer marginContainer2;

	private HBoxContainer buttonContainer;

	private RichTextLabel messageLabel;

	private const float maxDialogHeight = 320f;

	public string message
	{
		get
		{
			return _message;
		}
		set
		{
			_message = value;
			UpdateMessageDisplay();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		marginContainer2 = GetNode<MarginContainer>("Layer/Control/MarginContainer2");
		buttonContainer = GetNode<HBoxContainer>("Layer/Control/ButtonContainer");
		messageLabel = GetNode<RichTextLabel>("%MessageLabel");
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
		UpdateMessageDisplay();
	}

	private void UpdateMessageDisplay()
	{
		if (messageLabel != null)
		{
			if (!string.IsNullOrEmpty(_message))
			{
				messageLabel.Text = "[center][font_size=16]更新内容：" + _message + "[/font_size][/center]";
				messageLabel.Visible = true;
			}
			else
			{
				messageLabel.Text = "";
				messageLabel.Visible = false;
			}
			CallDeferred("AdjustDialogSize");
		}
	}

	private void AdjustDialogSize()
	{
		if (marginContainer2 != null && buttonContainer != null)
		{
			float num = 25f;
			float num2 = ((buttonContainer.Size.Y > 0f) ? buttonContainer.Size.Y : 41f);
			float num3 = ((messageLabel != null && messageLabel.Visible) ? messageLabel.Size.Y : 0f);
			float num4 = 20f;
			float num5 = 80f;
			float num6 = 60f;
			float a = Mathf.Max((30f + num3 + num + num2 + num4 * 2f + num5 + num6) / 2f, 107f);
			a = Mathf.Min(a, 320f);
			float num7 = marginContainer2.Size.X / 2f;
			marginContainer2.OffsetTop = 0f - a;
			marginContainer2.OffsetBottom = a;
			marginContainer2.OffsetLeft = 0f - num7;
			marginContainer2.OffsetRight = num7;
			float num8 = a - num2 / 2f - num6;
			buttonContainer.OffsetTop = num8 - num2 / 2f;
			buttonContainer.OffsetBottom = num8 + num2 / 2f;
		}
	}

	public void TrueButtonPressed()
	{
		OS.ShellOpen(uri);
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
