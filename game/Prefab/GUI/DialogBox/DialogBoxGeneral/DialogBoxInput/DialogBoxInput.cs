using Godot;

public partial class DialogBoxInput : DialogPopup
{
	public delegate void ConfirmButtonPressedEventHandler();

	public RichTextLabel titleLabel;

	public LineEdit inputLineEdit;

	public MainButton confirmButton;

	public MainButton cancelButton;

	private string _titleText = "";

	private int _inputMaxLength = 10;

	private string _inputDefaultText = "";

	private string _inputPlaceholder = "";

	public string titleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			_titleText = value;
			if (IsNodeReady())
			{
				titleLabel.Text = value;
			}
		}
	}

	public int inputMaxLength
	{
		get
		{
			return _inputMaxLength;
		}
		set
		{
			_inputMaxLength = value;
			if (IsNodeReady())
			{
				inputLineEdit.MaxLength = value;
			}
		}
	}

	public string inputDefaultText
	{
		get
		{
			return _inputDefaultText;
		}
		set
		{
			_inputDefaultText = value;
			if (IsNodeReady())
			{
				inputLineEdit.Text = value;
			}
		}
	}

	public string inputPlaceholder
	{
		get
		{
			return _inputPlaceholder;
		}
		set
		{
			_inputPlaceholder = value;
			if (IsNodeReady())
			{
				inputLineEdit.PlaceholderText = value;
			}
		}
	}

	public string inputText => inputLineEdit.Text;

	public event ConfirmButtonPressedEventHandler OnConfirmButtonPressed;

	public override void _Ready()
	{
		base._Ready();
		titleLabel = GetNode<RichTextLabel>("%TitleLabel");
		inputLineEdit = GetNode<LineEdit>("%InputLineEdit");
		confirmButton = GetNode<MainButton>("%ConfirmButton");
		cancelButton = GetNode<MainButton>("%CancelButton");
		GetNode<BaseButton>("%ConfirmButton").Pressed += ConfirmButtonPressed;
		GetNode<BaseButton>("%CancelButton").Pressed += CancelButtonPressed;
		titleLabel.Text = titleText;
		inputLineEdit.MaxLength = inputMaxLength;
		inputLineEdit.Text = inputDefaultText;
		inputLineEdit.PlaceholderText = inputPlaceholder;
	}

	public void ConfirmButtonPressed()
	{
		this.OnConfirmButtonPressed?.Invoke();
		CloseDialog();
	}

	public void CancelButtonPressed()
	{
		CloseDialog();
	}
}
