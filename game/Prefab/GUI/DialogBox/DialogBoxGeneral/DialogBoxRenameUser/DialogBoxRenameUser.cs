using Godot;

public partial class DialogBoxRenameUser : DialogPopup
{
	public LineEdit nameLineEdit;

	public MainButton readyButton;

	public MainButton cancelButton;

	private string _changeUser = "";

	public string changeUser
	{
		get
		{
			return _changeUser;
		}
		set
		{
			_changeUser = value;
			if (nameLineEdit != null)
			{
				nameLineEdit.Text = value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		nameLineEdit = GetNode<LineEdit>("%NameLineEdit");
		readyButton = GetNode<MainButton>("%ReadyButton");
		cancelButton = GetNode<MainButton>("%CancelButton");
		GetNode<BaseButton>("%ReadyButton").Pressed += ReadyButtonPressed;
		GetNode<BaseButton>("%CancelButton").Pressed += CancelButtonPressed;
		nameLineEdit.Text = changeUser;
	}

	public void ReadyButtonPressed()
	{
		string text = nameLineEdit.Text;
		if (text.Length > 0 && !GameSaveManager.Instance.HasUser(text))
		{
			GameSaveManager.Instance.RenameUser(changeUser, text);
			GameSaveManager.Instance.SetUserCurrent(text);
			CloseDialog();
		}
	}

	public void CancelButtonPressed()
	{
		CloseDialog();
	}
}
