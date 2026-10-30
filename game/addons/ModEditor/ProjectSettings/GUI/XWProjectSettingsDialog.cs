using System;
using System.IO;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ProjectSettingsGUI;

public partial class XWProjectSettingsDialog : ConfirmationDialog
{
	[Signal]
	public delegate void ProjectSavedEventHandler();

	private LineEdit _nameEdit;

	private LineEdit _versionEdit;

	private LineEdit _authorEdit;

	private TextEdit _descriptionEdit;

	private LineEdit _projectPathEdit;

	private LineEdit _projectFileEdit;

	private LineEdit _exportPathEdit;

	private Button _exportBrowseButton;

	private LineEdit _gamePathEdit;

	private Button _gameBrowseButton;

	private SpinBox _autoSaveSpin;

	private CheckBox _showToastsCheck;

	private OptionButton _languageOption;

	private Button _chineseLanguageButton;

	private Button _englishLanguageButton;

	private Label _statusLabel;

	private FileDialog _exportDirDialog;

	private FileDialog _gameDirDialog;

	private ModProject _project;

	private XWEditorSettings _settings;

	private string _originalProjectFilePath = "";

	public override void _Ready()
	{
		base.Title = "项目设置";
		base.OkButtonText = "保存";
		base.CancelButtonText = "取消";
		base.MinSize = new Vector2I(560, 440);
		_nameEdit = GetNode<LineEdit>("%NameEdit");
		_versionEdit = GetNode<LineEdit>("%VersionEdit");
		_authorEdit = GetNode<LineEdit>("%AuthorEdit");
		_descriptionEdit = GetNode<TextEdit>("%DescriptionEdit");
		_projectPathEdit = GetNode<LineEdit>("%ProjectPathEdit");
		_projectFileEdit = GetNode<LineEdit>("%ProjectFileEdit");
		_exportPathEdit = GetNode<LineEdit>("%ExportPathEdit");
		_exportBrowseButton = GetNode<Button>("%ExportBrowseButton");
		_gamePathEdit = GetNode<LineEdit>("%GamePathEdit");
		_gameBrowseButton = GetNode<Button>("%GameBrowseButton");
		_autoSaveSpin = GetNode<SpinBox>("%AutoSaveSpin");
		_showToastsCheck = GetNode<CheckBox>("%ShowToastsCheck");
		_languageOption = GetNode<OptionButton>("%LanguageOption");
		_chineseLanguageButton = GetNode<Button>("%ChineseLanguageButton");
		_englishLanguageButton = GetNode<Button>("%EnglishLanguageButton");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_exportDirDialog = GetNode<FileDialog>("%ExportDirDialog");
		_gameDirDialog = GetNode<FileDialog>("%GameDirDialog");
		_projectPathEdit.Editable = false;
		_projectFileEdit.Editable = false;
		_languageOption.Clear();
		_languageOption.AddItem("简体中文", 0);
		_languageOption.AddItem("英语", 1);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		_chineseLanguageButton.ButtonGroup = buttonGroup;
		_englishLanguageButton.ButtonGroup = buttonGroup;
		_chineseLanguageButton.Pressed += delegate
		{
			SelectLanguage(0);
		};
		_englishLanguageButton.Pressed += delegate
		{
			SelectLanguage(1);
		};
		_exportBrowseButton.Pressed += OnExportBrowsePressed;
		_gameBrowseButton.Pressed += OnGameBrowsePressed;
		_exportDirDialog.DirSelected += delegate(string dir)
		{
			_exportPathEdit.Text = dir;
		};
		_gameDirDialog.DirSelected += delegate(string dir)
		{
			_gamePathEdit.Text = dir;
		};
		base.Confirmed += OnConfirmed;
		Populate();
	}

	public void EditProject(ModProject project, XWEditorSettings settings)
	{
		_project = project;
		_settings = settings;
		_originalProjectFilePath = project?.ProjectFilePath ?? "";
		if (IsInsideTree())
		{
			Populate();
		}
	}

	private void Populate()
	{
		if (_nameEdit != null)
		{
			bool flag = _project != null;
			_nameEdit.Editable = flag;
			_versionEdit.Editable = flag;
			_authorEdit.Editable = flag;
			_descriptionEdit.Editable = flag;
			_exportPathEdit.Editable = flag;
			_gamePathEdit.Editable = flag;
			_exportBrowseButton.Disabled = !flag;
			_gameBrowseButton.Disabled = !flag;
			if (flag)
			{
				_nameEdit.Text = _project.Name ?? "";
				_versionEdit.Text = (string.IsNullOrWhiteSpace(_project.Version) ? "1.0.0" : _project.Version);
				_authorEdit.Text = _project.Author ?? "";
				_descriptionEdit.Text = _project.Description ?? "";
				_projectPathEdit.Text = _project.ProjectPath ?? "";
				_projectFileEdit.Text = _project.ProjectFilePath ?? "";
				_exportPathEdit.Text = GetProjectExportDirectory();
				_gamePathEdit.Text = _project.GameDirectory ?? "";
				_statusLabel.Text = "正在编辑当前 Mod 工程设置";
			}
			else
			{
				_nameEdit.Text = "";
				_versionEdit.Text = "";
				_authorEdit.Text = "";
				_descriptionEdit.Text = "";
				_projectPathEdit.Text = "";
				_projectFileEdit.Text = "";
				_exportPathEdit.Text = "";
				_gamePathEdit.Text = "";
				_statusLabel.Text = "没有打开的 Mod 工程，仅可保存编辑器偏好";
			}
			XWEditorSettings obj = _settings ?? XWEditorInterface.Instance?.GetEditorSettings();
			int num = obj?.GetSetting("editor/auto_save_interval", 300) ?? 300;
			bool buttonPressed = obj?.GetSetting("editor/show_toasts", defaultValue: true) ?? true;
			string text = obj?.GetSetting("interface/language", "zh_CN") ?? "zh_CN";
			_autoSaveSpin.Value = num;
			_showToastsCheck.ButtonPressed = buttonPressed;
			SelectLanguage((text == "en_US") ? 1 : 0);
		}
	}

	private void SelectLanguage(int index)
	{
		int num = ((index == 1) ? 1 : 0);
		_languageOption.Select(num);
		_chineseLanguageButton.SetPressedNoSignal(num == 0);
		_englishLanguageButton.SetPressedNoSignal(num == 1);
	}

	private void OnConfirmed()
	{
		SaveEditorSettings();
		if (_project == null)
		{
			XWEditorInterface.Instance?.ShowToast("项目设置已保存");
			EmitSignal(SignalName.ProjectSaved);
		}
		else if (!SaveProjectSettings())
		{
			CallDeferred("ReopenAfterValidationError");
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast("项目设置已保存");
			EmitSignal(SignalName.ProjectSaved);
		}
	}

	private bool SaveProjectSettings()
	{
		string text = _nameEdit.Text.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			XWEditorInterface.Instance?.ShowToast("项目名称不能为空");
			return false;
		}
		string text2 = _exportPathEdit.Text.Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !EnsureDirectory(text2, "默认导出目录"))
		{
			return false;
		}
		string text3 = _gamePathEdit.Text.Trim();
		if (!string.IsNullOrWhiteSpace(text3) && !DirAccess.DirExistsAbsolute(text3))
		{
			XWEditorInterface.Instance?.ShowToast("游戏目录不存在: " + text3);
			return false;
		}
		_project.Name = text;
		_project.Version = (string.IsNullOrWhiteSpace(_versionEdit.Text) ? "1.0.0" : _versionEdit.Text.Trim());
		_project.Author = (string.IsNullOrWhiteSpace(_authorEdit.Text) ? "未知" : _authorEdit.Text.Trim());
		_project.Description = _descriptionEdit.Text ?? "";
		_project.ExportDirectory = text2;
		_project.GameDirectory = text3;
		_project.Save();
		RemoveOldProjectFileIfRenamed();
		_originalProjectFilePath = _project.ProjectFilePath;
		_projectFileEdit.Text = _project.ProjectFilePath;
		return true;
	}

	private void SaveEditorSettings()
	{
		XWEditorSettings xWEditorSettings = _settings ?? XWEditorInterface.Instance?.GetEditorSettings();
		if (xWEditorSettings != null)
		{
			xWEditorSettings.SetSetting("editor/auto_save_interval", Mathf.RoundToInt((float)_autoSaveSpin.Value));
			xWEditorSettings.SetSetting("editor/show_toasts", _showToastsCheck.ButtonPressed);
			xWEditorSettings.SetSetting("interface/language", (_languageOption.GetSelectedId() == 1) ? "en_US" : "zh_CN");
			xWEditorSettings.Save();
		}
	}

	private void OnExportBrowsePressed()
	{
		string text = _exportPathEdit.Text.Trim();
		_exportDirDialog.CurrentDir = (DirAccess.DirExistsAbsolute(text) ? text : ProjectSettings.GlobalizePath("user://Mods/"));
		_exportDirDialog.PopupCentered(new Vector2I(700, 500));
	}

	private void OnGameBrowsePressed()
	{
		string text = _gamePathEdit.Text.Trim();
		if (DirAccess.DirExistsAbsolute(text))
		{
			_gameDirDialog.CurrentDir = text;
		}
		_gameDirDialog.PopupCentered(new Vector2I(700, 500));
	}

	private void ReopenAfterValidationError()
	{
		PopupCenteredClamped(new Vector2I(720, 560), 0.9f);
	}

	private string GetProjectExportDirectory()
	{
		if (!string.IsNullOrWhiteSpace(_project.ExportDirectory))
		{
			return _project.ExportDirectory;
		}
		return ProjectSettings.GlobalizePath("user://Mods/");
	}

	private static bool EnsureDirectory(string path, string label)
	{
		if (DirAccess.DirExistsAbsolute(path))
		{
			return true;
		}
		if (DirAccess.MakeDirRecursiveAbsolute(path) == Error.Ok && DirAccess.DirExistsAbsolute(path))
		{
			return true;
		}
		XWEditorInterface.Instance?.ShowToast(label + "无法创建: " + path);
		return false;
	}

	private void RemoveOldProjectFileIfRenamed()
	{
		if (string.IsNullOrWhiteSpace(_originalProjectFilePath) || string.Equals(_originalProjectFilePath, _project.ProjectFilePath, StringComparison.OrdinalIgnoreCase) || !File.Exists(_originalProjectFilePath))
		{
			return;
		}
		try
		{
			File.Delete(_originalProjectFilePath);
		}
		catch (Exception ex)
		{
			GD.PrintErr("[XWProjectSettingsDialog] 无法删除旧项目文件: " + _originalProjectFilePath + "\n" + ex.Message);
		}
	}
}
