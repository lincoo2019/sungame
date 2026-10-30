using Godot;

public partial class DialogBoxNewUser : DialogPopup
{
	public LineEdit nameLineEdit;

	public MainButton readyButton;

	public MainButton cancelButton;

	public MainButton readyButtonNewUser;

	public override void _Ready()
	{
		base._Ready();
		nameLineEdit = GetNode<LineEdit>("%NameLineEdit");
		readyButton = GetNode<MainButton>("%ReadyButton");
		cancelButton = GetNode<MainButton>("%CancelButton");
		readyButtonNewUser = GetNode<MainButton>("%ReadyButtonNewUser");
		GetNode<BaseButton>("%ReadyButton").Pressed += ReadyButtonPressed;
		GetNode<BaseButton>("%CancelButton").Pressed += CancelButtonPressed;
		GetNode<BaseButton>("%ReadyButtonNewUser").Pressed += ReadyButtonNewUserPressed;
		if (GameSaveManager.Instance.GetUserCurrent() == "")
		{
			readyButton.Visible = false;
			cancelButton.Visible = false;
			readyButtonNewUser.Visible = true;
		}
	}

	public void ReadyButtonPressed()
	{
		string text = nameLineEdit.Text;
		if (text.Length > 0 && !GameSaveManager.Instance.HasUser(text))
		{
			GameSaveManager.Instance.AddUser(text);
			GameSaveManager.Instance.SetUserCurrent(text);
			GameSaveManager.Instance.Save();
			CloseDialog();
		}
	}

	public void CancelButtonPressed()
	{
		CloseDialog();
	}

	public void ReadyButtonNewUserPressed()
	{
		string text = nameLineEdit.Text;
		if (text.Length > 0 && !GameSaveManager.Instance.HasUser(text))
		{
			GameSaveManager.Instance.AddUser(text);
			GameSaveManager.Instance.SetUserCurrent(text);
			GameSaveManager.Instance.Save();
			CloseDialog();
		}
	}
}
