using Godot;

namespace PVZHE.ModEditor.ModSystem;

public partial class ModProjectCreateDialog : ConfirmationDialog
{
	private LineEdit _nameEdit;

	private LineEdit _versionEdit;

	private LineEdit _authorEdit;

	private TextEdit _descriptionEdit;

	private LineEdit _pathEdit;

	private Button _browseButton;

	private FileDialog _browseDialog;

	public string SelectedParentDir => _pathEdit?.Text ?? "";

	public string ModName => _nameEdit?.Text ?? "";

	public string ModVersion => _versionEdit?.Text ?? "1.0.0";

	public string ModAuthor => _authorEdit?.Text ?? "未知";

	public string ModDescription => _descriptionEdit?.Text ?? "";

	public override void _Ready()
	{
		_nameEdit = GetNode<LineEdit>("%NameEdit");
		_versionEdit = GetNode<LineEdit>("%VersionEdit");
		_authorEdit = GetNode<LineEdit>("%AuthorEdit");
		_descriptionEdit = GetNode<TextEdit>("%DescriptionEdit");
		_pathEdit = GetNode<LineEdit>("%PathEdit");
		_browseButton = GetNode<Button>("%BrowseButton");
		_browseDialog = GetNode<FileDialog>("%BrowseDialog");
		_browseButton.Pressed += OnBrowsePressed;
		_browseDialog.DirSelected += OnDirSelected;
		_pathEdit.Text = ProjectSettings.GlobalizePath("user://Mods/");
	}

	private void OnBrowsePressed()
	{
		string text = _pathEdit.Text;
		if (!string.IsNullOrEmpty(text) && DirAccess.DirExistsAbsolute(text))
		{
			_browseDialog.CurrentDir = text;
		}
		_browseDialog.PopupCentered();
	}

	private void OnDirSelected(string dir)
	{
		_pathEdit.Text = dir;
	}

	public override void _ExitTree()
	{
		if (_browseButton != null)
		{
			_browseButton.Pressed -= OnBrowsePressed;
		}
		if (_browseDialog != null)
		{
			_browseDialog.DirSelected -= OnDirSelected;
		}
		base._ExitTree();
	}
}
