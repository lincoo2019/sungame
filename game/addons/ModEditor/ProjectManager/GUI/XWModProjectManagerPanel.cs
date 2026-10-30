using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ProjectManager;

public partial class XWModProjectManagerPanel : PanelContainer
{
	[Signal]
	public delegate void ProjectOpenRequestedEventHandler(string projectFilePath);

	[Signal]
	public delegate void NewProjectRequestedEventHandler();

	[Signal]
	public delegate void OpenProjectRequestedEventHandler();

	private sealed class ProjectEntry
	{
		public string ProjectFilePath = "";

		public string Name = "";

		public string Version = "";

		public string Author = "";

		public string Description = "";

		public string ProjectDirectory = "";

		public bool Exists;
	}

	private const string RecentProjectsConfigPath = "user://mod_editor_recent_projects.cfg";

	private const int MaxRecentProjects = 24;

	private Tree _projectList;

	private Label _projectNameLabel;

	private Label _projectPathLabel;

	private Label _projectMetaLabel;

	private RichTextLabel _projectDescriptionLabel;

	private Label _emptyHintLabel;

	private Button _newProjectButton;

	private Button _openProjectButton;

	private Button _refreshProjectsButton;

	private Button _openSelectedProjectButton;

	private Button _removeMissingButton;

	private readonly List<string> _recentProjectFiles = new List<string>();

	private readonly List<ProjectEntry> _projectEntries = new List<ProjectEntry>();

	private string _selectedProjectFilePath = "";

	public override void _Ready()
	{
		_projectList = GetNode<Tree>("%ProjectList");
		_projectNameLabel = GetNode<Label>("%ProjectNameLabel");
		_projectPathLabel = GetNode<Label>("%ProjectPathLabel");
		_projectMetaLabel = GetNode<Label>("%ProjectMetaLabel");
		_projectDescriptionLabel = GetNode<RichTextLabel>("%ProjectDescriptionLabel");
		_emptyHintLabel = GetNode<Label>("%EmptyHintLabel");
		_newProjectButton = GetNode<Button>("%NewProjectButton");
		_openProjectButton = GetNode<Button>("%OpenProjectButton");
		_refreshProjectsButton = GetNode<Button>("%RefreshProjectsButton");
		_openSelectedProjectButton = GetNode<Button>("%OpenSelectedProjectButton");
		_removeMissingButton = GetNode<Button>("%RemoveMissingButton");
		SetupProjectList();
		ConnectSignals();
		LoadRecentProjects();
		RefreshProjectList();
	}

	public void ShowStartScreen()
	{
		base.Visible = true;
		LoadRecentProjects();
		RefreshProjectList();
	}

	public void AddRecentProject(ModProject project)
	{
		if (project != null)
		{
			AddRecentProject(project.ProjectFilePath);
		}
	}

	public void AddRecentProject(string projectFilePath)
	{
		string normalized = NormalizeProjectFilePath(projectFilePath);
		if (!string.IsNullOrWhiteSpace(normalized))
		{
			_recentProjectFiles.RemoveAll((string path) => string.Equals(path, normalized, StringComparison.OrdinalIgnoreCase));
			_recentProjectFiles.Insert(0, normalized);
			if (_recentProjectFiles.Count > 24)
			{
				_recentProjectFiles.RemoveRange(24, _recentProjectFiles.Count - 24);
			}
			SaveRecentProjects();
			RefreshProjectList();
			SelectProject(normalized);
		}
	}

	private void SetupProjectList()
	{
		_projectList.Columns = 3;
		_projectList.ColumnTitlesVisible = true;
		_projectList.HideRoot = true;
		_projectList.SelectMode = Tree.SelectModeEnum.Row;
		_projectList.SetColumnTitle(0, "工程");
		_projectList.SetColumnTitle(1, "版本");
		_projectList.SetColumnTitle(2, "路径");
		_projectList.SetColumnExpand(0, expand: true);
		_projectList.SetColumnExpand(1, expand: false);
		_projectList.SetColumnCustomMinimumWidth(1, 92);
		_projectList.SetColumnExpand(2, expand: true);
	}

	private void ConnectSignals()
	{
		_projectList.ItemSelected += OnProjectItemSelected;
		_projectList.ItemActivated += OpenSelectedProject;
		_newProjectButton.Pressed += delegate
		{
			EmitSignal(SignalName.NewProjectRequested);
		};
		_openProjectButton.Pressed += delegate
		{
			EmitSignal(SignalName.OpenProjectRequested);
		};
		_refreshProjectsButton.Pressed += RefreshProjectList;
		_openSelectedProjectButton.Pressed += OpenSelectedProject;
		_removeMissingButton.Pressed += RemoveMissingProjectRecords;
	}

	private void LoadRecentProjects()
	{
		_recentProjectFiles.Clear();
		ConfigFile configFile = new ConfigFile();
		if (configFile.Load("user://mod_editor_recent_projects.cfg") != Error.Ok)
		{
			return;
		}
		int num = (int)configFile.GetValue("projects", "count", 0);
		for (int i = 0; i < num; i++)
		{
			string path = NormalizeProjectFilePath(configFile.GetValue("projects", $"path_{i}", "").AsString());
			if (!string.IsNullOrWhiteSpace(path) && !_recentProjectFiles.Exists((string existing) => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)))
			{
				_recentProjectFiles.Add(path);
			}
		}
	}

	private void SaveRecentProjects()
	{
		ConfigFile configFile = new ConfigFile();
		configFile.SetValue("projects", "count", _recentProjectFiles.Count);
		for (int i = 0; i < _recentProjectFiles.Count; i++)
		{
			configFile.SetValue("projects", $"path_{i}", _recentProjectFiles[i]);
		}
		configFile.Save("user://mod_editor_recent_projects.cfg");
	}

	private void RefreshProjectList()
	{
		_projectEntries.Clear();
		foreach (string recentProjectFile in _recentProjectFiles)
		{
			_projectEntries.Add(ReadProjectEntry(recentProjectFile));
		}
		_projectList.Clear();
		TreeItem parent = _projectList.CreateItem();
		foreach (ProjectEntry projectEntry in _projectEntries)
		{
			TreeItem treeItem = _projectList.CreateItem(parent);
			treeItem.SetText(0, projectEntry.Exists ? projectEntry.Name : (projectEntry.Name + " (缺失)"));
			treeItem.SetText(1, projectEntry.Version);
			treeItem.SetText(2, projectEntry.ProjectDirectory);
			treeItem.SetTooltipText(0, projectEntry.ProjectFilePath);
			treeItem.SetMetadata(0, projectEntry.ProjectFilePath);
			if (!projectEntry.Exists)
			{
				Color color = new Color(1f, 0.45f, 0.38f, 0.92f);
				treeItem.SetCustomColor(0, color);
				treeItem.SetCustomColor(1, color);
				treeItem.SetCustomColor(2, color);
			}
		}
		bool flag = _projectEntries.Count > 0;
		_emptyHintLabel.Visible = !flag;
		if (!flag)
		{
			SelectProject("");
		}
		else if (!string.IsNullOrWhiteSpace(_selectedProjectFilePath))
		{
			SelectProject(_selectedProjectFilePath);
		}
		else
		{
			SelectFirstProject();
		}
	}

	private void OnProjectItemSelected()
	{
		SelectProject(_projectList.GetSelected()?.GetMetadata(0).AsString() ?? "");
	}

	private void SelectFirstProject()
	{
		TreeItem treeItem = _projectList.GetRoot()?.GetFirstChild();
		if (treeItem == null)
		{
			SelectProject("");
			return;
		}
		treeItem.Select(0);
		SelectProject(treeItem.GetMetadata(0).AsString());
	}

	private void SelectProject(string projectFilePath)
	{
		_selectedProjectFilePath = NormalizeProjectFilePath(projectFilePath);
		ProjectEntry projectEntry = _projectEntries.Find((ProjectEntry item) => string.Equals(item.ProjectFilePath, _selectedProjectFilePath, StringComparison.OrdinalIgnoreCase));
		bool num = projectEntry != null;
		bool flag = num && projectEntry.Exists;
		_openSelectedProjectButton.Disabled = !flag;
		_removeMissingButton.Disabled = !_projectEntries.Exists((ProjectEntry item) => !item.Exists);
		if (!num)
		{
			_projectNameLabel.Text = "未选择工程";
			_projectPathLabel.Text = "";
			_projectMetaLabel.Text = "";
			_projectDescriptionLabel.Text = "从左侧新建或打开一个 Mod 工程。";
		}
		else
		{
			_projectNameLabel.Text = projectEntry.Name;
			_projectPathLabel.Text = projectEntry.ProjectDirectory;
			_projectMetaLabel.Text = (projectEntry.Exists ? ("版本 " + projectEntry.Version + "    作者 " + projectEntry.Author) : "工程文件不存在");
			_projectDescriptionLabel.Text = (string.IsNullOrWhiteSpace(projectEntry.Description) ? "没有描述。" : projectEntry.Description);
		}
	}

	private void OpenSelectedProject()
	{
		ProjectEntry projectEntry = _projectEntries.Find((ProjectEntry item) => string.Equals(item.ProjectFilePath, _selectedProjectFilePath, StringComparison.OrdinalIgnoreCase));
		if (projectEntry != null && projectEntry.Exists)
		{
			EmitSignal(SignalName.ProjectOpenRequested, projectEntry.ProjectFilePath);
		}
	}

	private void RemoveMissingProjectRecords()
	{
		_recentProjectFiles.RemoveAll((string path) => !File.Exists(path));
		SaveRecentProjects();
		RefreshProjectList();
	}

	private static ProjectEntry ReadProjectEntry(string projectFilePath)
	{
		string text = NormalizeProjectFilePath(projectFilePath);
		ProjectEntry projectEntry = new ProjectEntry
		{
			ProjectFilePath = text,
			ProjectDirectory = (string.IsNullOrWhiteSpace(text) ? "" : (Path.GetDirectoryName(text) ?? "")),
			Name = (string.IsNullOrWhiteSpace(text) ? "未知工程" : Path.GetFileNameWithoutExtension(text)),
			Version = "-",
			Author = "-",
			Description = "",
			Exists = File.Exists(text)
		};
		if (!projectEntry.Exists)
		{
			return projectEntry;
		}
		try
		{
			ModProject modProject = JsonSerializer.Deserialize(File.ReadAllText(text), XWModJsonContext.Default.ModProject);
			if (modProject == null)
			{
				return projectEntry;
			}
			projectEntry.Name = (string.IsNullOrWhiteSpace(modProject.Name) ? projectEntry.Name : modProject.Name);
			projectEntry.Version = (string.IsNullOrWhiteSpace(modProject.Version) ? "-" : modProject.Version);
			projectEntry.Author = (string.IsNullOrWhiteSpace(modProject.Author) ? "-" : modProject.Author);
			projectEntry.Description = modProject.Description ?? "";
		}
		catch (Exception ex)
		{
			projectEntry.Description = "读取工程信息失败: " + ex.Message;
		}
		return projectEntry;
	}

	private static string NormalizeProjectFilePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string text = path.Trim();
		if (text.StartsWith("user://") || text.StartsWith("res://"))
		{
			text = ProjectSettings.GlobalizePath(text);
		}
		try
		{
			text = Path.GetFullPath(text);
		}
		catch
		{
			return text.Replace('\\', '/');
		}
		return text.Replace('\\', '/');
	}
}
