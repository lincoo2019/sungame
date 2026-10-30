using Godot;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWRemoveConfirmDialog : ConfirmationDialog
{
	private Label _messageLabel;

	private Tree _fileTree;

	private string _targetPath = "";

	public override void _Ready()
	{
		_messageLabel = GetNode<Label>("%MessageLabel");
		_fileTree = GetNode<Tree>("%FileTree");
		base.Confirmed += OnConfirmed;
	}

	public void PopupFor(string path)
	{
		if (XWModProjectLayout.IsProtectedDirectory(path, XWFileSystem.GetSingleton().ProjectFolderPath))
		{
			_targetPath = "";
			Hide();
			return;
		}
		_targetPath = path;
		_messageLabel.Text = "确认删除：\n" + path + "\n\n该操作会移到回收站。";
		BuildFileTree(path);
		PopupCentered();
	}

	private void BuildFileTree(string path)
	{
		_fileTree.Clear();
		TreeItem treeItem = _fileTree.CreateItem();
		treeItem.SetText(0, path);
		XWFileSystemDirectory filesystemPath = XWFileSystem.GetSingleton().GetFilesystemPath(path);
		if (filesystemPath != null)
		{
			BuildTree(treeItem, filesystemPath);
		}
	}

	private void BuildTree(TreeItem parent, XWFileSystemDirectory dir)
	{
		foreach (XWFileSystemDirectory subdir in dir.Subdirs)
		{
			TreeItem treeItem = _fileTree.CreateItem(parent);
			treeItem.SetText(0, subdir.Name);
			BuildTree(treeItem, subdir);
		}
		foreach (XWFileInfo file in dir.Files)
		{
			_fileTree.CreateItem(parent).SetText(0, file.Name);
		}
	}

	private void OnConfirmed()
	{
		if (!string.IsNullOrEmpty(_targetPath) && !XWModProjectLayout.IsProtectedDirectory(_targetPath, XWFileSystem.GetSingleton().ProjectFolderPath))
		{
			XWFileSystem.GetSingleton().RemoveFile(_targetPath);
			_targetPath = "";
		}
	}
}
