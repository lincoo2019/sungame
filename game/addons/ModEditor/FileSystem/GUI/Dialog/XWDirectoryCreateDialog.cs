using Godot;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWDirectoryCreateDialog : ConfirmationDialog
{
	private Label _basePathLabel;

	private LineEdit _dirPathEdit;

	private Label _validationLabel;

	private string _basePath = "";

	public override void _Ready()
	{
		_basePathLabel = GetNode<Label>("%BasePathLabel");
		_dirPathEdit = GetNode<LineEdit>("%DirPathEdit");
		_validationLabel = GetNode<Label>("%ValidationLabel");
		_dirPathEdit.TextSubmitted += delegate
		{
			DoCreate();
		};
		base.Confirmed += DoCreate;
	}

	public void PopupAt(string basePath)
	{
		_basePath = basePath;
		_basePathLabel.Text = basePath;
		_dirPathEdit.Text = "";
		_validationLabel.Hide();
		PopupCentered();
		_dirPathEdit.GrabFocus();
	}

	private void DoCreate()
	{
		string text = _dirPathEdit.Text.Trim();
		if (string.IsNullOrEmpty(text))
		{
			_validationLabel.Text = "名称不能为空";
			_validationLabel.Show();
			return;
		}
		if (text.Contains("/") || text.Contains("\\"))
		{
			_validationLabel.Text = "名称不能包含路径分隔符";
			_validationLabel.Show();
			return;
		}
		_validationLabel.Hide();
		string path = _basePath + text + "/";
		XWFileSystem.GetSingleton().MakeDirRecursive(path);
		Hide();
	}
}
