using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.Issues.GUI;
using PVZHE.ModEditor.Layout;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.ProjectManager;
using PVZHE.ModEditor.ProjectSettingsGUI;
using PVZHE.ModEditor.PVZIntegration;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.RunBar;
using PVZHE.ModEditor.SceneEditor;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools.GUI;

namespace PVZHE.ModEditor;

public partial class ModEditorPanel : Control
{
	private const string CreateProjectDialogScenePath = "res://addons/ModEditor/ModSystem/ModProjectCreateDialog.tscn";

	private const string ExportDirectoryDialogScenePath = "res://addons/ModEditor/GUI/XWExportDirectoryDialog.tscn";

	private const string OpenProjectDialogScenePath = "res://addons/ModEditor/GUI/XWOpenProjectDialog.tscn";

	private const string RunCustomSceneDialogScenePath = "res://addons/ModEditor/GUI/XWRunCustomSceneDialog.tscn";

	private const string RunPreviewWindowScenePath = "res://addons/ModEditor/GUI/XWRunPreviewWindow.tscn";

	private static PackedScene _createProjectDialogScene;

	private static PackedScene _exportDirectoryDialogScene;

	private static PackedScene _openProjectDialogScene;

	private static PackedScene _runCustomSceneDialogScene;

	private static PackedScene _runPreviewWindowScene;

	private XWUndoRedoManager _undoRedoManager;

	private XWEditorData _editorData;

	private XWEditorSettings _editorSettings;

	private XWEditorSelection _editorSelection;

	private XWEditorSelectionHistory _selectionHistory;

	private XWLayoutManager _layoutManager;

	private XWPanelRegistry _panelRegistry;

	private XWEditorTitleBar _titleBar;

	private HSplitContainer _workspaceSplit;

	private TabContainer _leftDock;

	private TabContainer _leftBottomDock;

	private TabContainer _leftTopRightDock;

	private TabContainer _leftBottomRightDock;

	private TabContainer _centerDock;

	private XWUiMotion _centerWorkspaceMotion;

	private XWBottomPanel _bottomPanel;

	private TabContainer _bottomDock;

	private TabContainer _rightDock;

	private TabContainer _rightBottomDock;

	private TabContainer _rightTopRightDock;

	private TabContainer _rightBottomRightDock;

	private TabContainer _rightPanelTabs;

	private Label _statusLabel;

	private XWFileSystemPanel _fileSystemPanel;

	private XWSceneTreeDock _sceneTreeDock;

	private XW2DSceneEditor _scene2DEditor;

	private XWInspector _inspector;

	private XWBPEditor _bpEditor;

	private XWScriptEditor _scriptEditor;

	private readonly Dictionary<string, XWGenericVisualResourceEditor> _resourceEditors = new Dictionary<string, XWGenericVisualResourceEditor>();

	private readonly Dictionary<string, XWVisualEditorDescriptor> _resourceEditorDescriptors = new Dictionary<string, XWVisualEditorDescriptor>();

	private XWIssuePanel _issuePanel;

	private XWModToolsPanel _modToolsPanel;

	private XWModProjectManagerPanel _projectManagerPanel;

	private XWEditorRunBar _runBar;

	private XWOutputPanel _outputPanel;

	private XWEditorToaster _toaster;

	private Window _runPreviewWindow;

	private SubViewport _runPreviewViewport;

	private Node _runPreviewRoot;

	private FileDialog _runCustomSceneDialog;

	private readonly Dictionary<Node, ProcessModeEnum> _runPreviewProcessModes = new Dictionary<Node, ProcessModeEnum>();

	private Control _loadingOverlay;

	private ProgressBar _loadingBar;

	private Label _loadingLabel;

	private XWDockOverlay _dockOverlay;

	private bool _isDockDragging;

	private string _dragPanelKey = "";

	private ModProject _currentProject;

	private ModProjectCreateDialog _createProjectDialog;

	private XWProjectSettingsDialog _projectSettingsDialog;

	private FileDialog _openProjectFileDialog;

	private FileDialog _exportDirDialog;

	private CancellationTokenSource _exportLifetimeCts = new CancellationTokenSource();

	private bool _isExporting;

	public int LoadedResourceEditorCount => _resourceEditors.Count;

	public override void _Ready()
	{
		SetProcess(enable: false);
		ModEditorTheme.ApplyTo(this);
		ColorRect nodeOrNull = GetNodeOrNull<ColorRect>("Background");
		if (nodeOrNull != null)
		{
			nodeOrNull.Color = ModEditorTheme.BackgroundColor;
		}
		_titleBar = GetNode<XWEditorTitleBar>("%TitleBar");
		_workspaceSplit = GetNode<HSplitContainer>("VBoxContainer/HSplitContainer");
		_leftDock = GetNode<TabContainer>("%LeftTopDock");
		_leftBottomDock = GetNodeOrNull<TabContainer>("%LeftBottomDock");
		_leftTopRightDock = GetNodeOrNull<TabContainer>("%LeftTopRightDock");
		_leftBottomRightDock = GetNodeOrNull<TabContainer>("%LeftBottomRightDock");
		_centerDock = GetNode<TabContainer>("%CenterDock");
		_centerWorkspaceMotion = XWUiMotion.BindPanel(_centerDock);
		_bottomPanel = GetNode<XWBottomPanel>("%BottomPanel");
		_bottomDock = _bottomPanel.ContentContainer;
		_rightDock = GetNode<TabContainer>("%RightTopDock");
		_rightBottomDock = GetNodeOrNull<TabContainer>("%RightBottomDock");
		_rightTopRightDock = GetNodeOrNull<TabContainer>("%RightTopRightDock");
		_rightBottomRightDock = GetNodeOrNull<TabContainer>("%RightBottomRightDock");
		_rightPanelTabs = GetNodeOrNull<TabContainer>("%RightPanel");
		if (_rightPanelTabs != null)
		{
			_rightPanelTabs.SetTabTitle(0, "主要");
			_rightPanelTabs.SetTabTitle(1, "扩展");
		}
		_statusLabel = GetNodeOrNull<Label>("%StatusLabel");
		_panelRegistry = new XWPanelRegistry();
		_panelRegistry.RegisterBuiltInPanels();
		_layoutManager = new XWLayoutManager();
		_layoutManager.InitializeExisting(this, _panelRegistry);
		_layoutManager.AdoptContainer(4, _leftDock);
		if (_leftBottomDock != null)
		{
			_layoutManager.AdoptContainer(5, _leftBottomDock);
		}
		if (_leftTopRightDock != null)
		{
			_layoutManager.AdoptContainer(6, _leftTopRightDock);
		}
		if (_leftBottomRightDock != null)
		{
			_layoutManager.AdoptContainer(7, _leftBottomRightDock);
		}
		_layoutManager.AdoptContainer(2, _centerDock);
		_layoutManager.AdoptContainer(8, _rightDock);
		if (_rightBottomDock != null)
		{
			_layoutManager.AdoptContainer(9, _rightBottomDock);
		}
		if (_rightTopRightDock != null)
		{
			_layoutManager.AdoptContainer(10, _rightTopRightDock);
		}
		if (_rightBottomRightDock != null)
		{
			_layoutManager.AdoptContainer(11, _rightBottomRightDock);
		}
		_layoutManager.AdoptContainer(3, _bottomDock);
		base.Resized += ApplyResponsiveWorkspaceLayout;
		ApplyResponsiveWorkspaceLayout();
		_dockOverlay = new XWDockOverlay
		{
			Name = "DockOverlay"
		};
		AddChild(_dockOverlay, forceReadableName: false, InternalMode.Disabled);
		BuildMenus();
		UpdateProjectTitle();
		_titleBar.MainScreenChanged += OnMainScreenChanged;
		ShowLoadingScreen();
		CallDeferred("StartAsyncInit");
	}

	public override void _ExitTree()
	{
		base.Resized -= ApplyResponsiveWorkspaceLayout;
		CancellationTokenSource exportLifetimeCts = _exportLifetimeCts;
		_exportLifetimeCts = null;
		if (exportLifetimeCts != null)
		{
			try
			{
				exportLifetimeCts.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			exportLifetimeCts.Dispose();
		}
		base._ExitTree();
	}

	private void ApplyResponsiveWorkspaceLayout()
	{
		if (GodotObject.IsInstanceValid(_workspaceSplit))
		{
			float num = Mathf.Max(base.Size.X, GetViewportRect().Size.X);
			if (!(num <= 0f))
			{
				int num2 = Mathf.Clamp(Mathf.FloorToInt((num - 760f) * 0.5f), 180, 300);
				_workspaceSplit.SplitOffsets = new int[2]
				{
					num2,
					-num2
				};
			}
		}
	}

	private void BuildMenus()
	{
		MenuBar menuBar = _titleBar.MenuBar;
		menuBar.GetNode<PopupMenu>("文件").IdPressed += OnFileMenuItem;
		menuBar.GetNode<PopupMenu>("编辑").IdPressed += OnEditMenuItem;
		PopupMenu node = menuBar.GetNode<PopupMenu>("模组");
		int itemIndex = node.GetItemIndex(20);
		if (itemIndex >= 0)
		{
			node.SetItemText(itemIndex, "应用已安装 Mod 到当前游戏");
		}
		int itemIndex2 = node.GetItemIndex(21);
		if (itemIndex2 >= 0)
		{
			node.SetItemText(itemIndex2, "打开 Mod 安装目录");
		}
		node.IdPressed += OnModMenuItem;
	}

	private void ShowLoadingScreen()
	{
		_loadingOverlay = GetNode<Control>("%LoadingOverlay");
		_loadingBar = GetNode<ProgressBar>("%LoadingBar");
		_loadingLabel = GetNode<Label>("%LoadingLabel");
		GetNode<ColorRect>("%LoadingBackground").Color = ModEditorTheme.BackgroundColor;
		GetNode<Label>("%LoadingTitle").AddThemeColorOverride("font_color", ModEditorTheme.TextColor);
		_loadingLabel.AddThemeColorOverride("font_color", ModEditorTheme.DimTextColor);
		_loadingOverlay.Show();
	}

	private async void StartAsyncInit()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		List<(string, Action)> steps = new List<(string, Action)>
		{
			("初始化核心系统...", delegate
			{
				_undoRedoManager = new XWUndoRedoManager();
				_undoRedoManager.ActionCommitted = Callable.From<string>(OnEditorActionCommitted);
				_undoRedoManager.ActionUndone = Callable.From<string>(OnEditorActionUndone);
				_undoRedoManager.ActionRedone = Callable.From<string>(OnEditorActionRedone);
				_editorData = new XWEditorData();
				_editorSettings = new XWEditorSettings();
				_editorSelection = new XWEditorSelection();
				_selectionHistory = new XWEditorSelectionHistory();
				_editorData.Load();
				_editorSettings.Load();
				_editorSettings.InitializeDefaultSettings();
				_titleBar.ConfigureMotion(_editorSettings.GetSetting("interface/reduced_motion", defaultValue: false), _editorSettings.GetSetting("interface/low_performance_mode", defaultValue: false));
			}),
			("初始化编辑器接口...", delegate
			{
				XWEditorInterface.Initialize(_undoRedoManager, _editorData, _editorSettings, _editorSelection, _selectionHistory);
				XWEditorInterface.Instance.SetEditorPanel(this);
				XWEditorInterface.Instance.SetLayoutManager(_layoutManager);
				XWEditorInterface.Instance.SetResourceEditorResolver(EnsureResourceEditor);
				XWEditorInterface.Instance.SetShowToastCallback(ShowToast);
			}),
			("注册属性编辑器...", RegisterBuiltinEditorFactories),
			("加载运行栏与输出面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/RunBar/XWEditorRunBar.tscn", null, ResourceLoader.CacheMode.Reuse);
				_runBar = packedScene.Instantiate<XWEditorRunBar>(PackedScene.GenEditState.Disabled);
				_runBar.Name = "RunBar";
				_titleBar.RunBarContainer.AddChild(_runBar, forceReadableName: false, InternalMode.Disabled);
				ConnectRunBarSignals();
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/OutPutPanel/XWOutputPanel.tscn", null, ResourceLoader.CacheMode.Reuse);
				_outputPanel = packedScene2.Instantiate<XWOutputPanel>(PackedScene.GenEditState.Disabled);
				_outputPanel.Name = "Output";
				_layoutManager.AddPanel(_outputPanel, "output", "输出", 3);
				XWEditorInterface.Instance.SetOutputPanel(_outputPanel);
				PackedScene packedScene3 = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWEditorToaster.tscn", null, ResourceLoader.CacheMode.Reuse);
				_toaster = packedScene3.Instantiate<XWEditorToaster>(PackedScene.GenEditState.Disabled);
				_toaster.Name = "Toaster";
				_toaster.SetAnchorsPreset(LayoutPreset.FullRect);
				_toaster.MouseFilter = MouseFilterEnum.Ignore;
				_toaster.ZIndex = 500;
				AddChild(_toaster, forceReadableName: false, InternalMode.Disabled);
			}),
			("加载场景树面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/SceneEditor/SceneNodeTree/XWSceneTreeDock.tscn", null, ResourceLoader.CacheMode.Reuse);
				_sceneTreeDock = packedScene.Instantiate<XWSceneTreeDock>(PackedScene.GenEditState.Disabled);
				_sceneTreeDock.Name = "SceneTreeDock";
				_layoutManager.AddPanel(_sceneTreeDock, "scene_tree", "场景", 6);
				XWEditorInterface.Instance.SetSceneTreeDock(_sceneTreeDock);
			}),
			("加载文件系统面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/FileSystem/GUI/XWFileSystemPanel.tscn", null, ResourceLoader.CacheMode.Reuse);
				_fileSystemPanel = packedScene.Instantiate<XWFileSystemPanel>(PackedScene.GenEditState.Disabled);
				_fileSystemPanel.Name = "FileSystemPanel";
				_layoutManager.AddPanel(_fileSystemPanel, "file_system", "文件系统", 7);
				XWEditorInterface.Instance.SetFileSystemPanel(_fileSystemPanel);
			}),
			("加载检查器面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/XWInspector.tscn", null, ResourceLoader.CacheMode.Reuse);
				_inspector = packedScene.Instantiate<XWInspector>(PackedScene.GenEditState.Disabled);
				_inspector.Name = "Inspector";
				_inspector.UndoRedoManager = _undoRedoManager;
				_layoutManager.AddPanel(_inspector, "inspector", "检查器", 8);
				XWEditorInterface.Instance.SetInspector(_inspector);
			}),
			("加载二维场景编辑器...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/SceneEditor/2D/GUI/XW2DSceneEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
				_scene2DEditor = packedScene.Instantiate<XW2DSceneEditor>(PackedScene.GenEditState.Disabled);
				_scene2DEditor.Name = "Scene2DEditor";
				_layoutManager.AddPanel(_scene2DEditor, "2d_editor", "二维场景", 2);
				XWEditorInterface.Instance.Set2DSceneEditor(_scene2DEditor);
			}),
			("加载蓝图编辑器...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Blueprint/GUI/XWBPEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
				_bpEditor = packedScene.Instantiate<XWBPEditor>(PackedScene.GenEditState.Disabled);
				_bpEditor.Name = "BPEditor";
				_layoutManager.AddPanel(_bpEditor, "bp_editor", "蓝图", 2);
				XWEditorInterface.Instance.SetBlueprintEditor(_bpEditor);
			}),
			("加载脚本编辑器...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ScriptEditor/GUI/XWScriptEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
				_scriptEditor = packedScene.Instantiate<XWScriptEditor>(PackedScene.GenEditState.Disabled);
				_scriptEditor.Name = "ScriptEditor";
				_layoutManager.AddPanel(_scriptEditor, "script_editor", "脚本", 2);
				XWEditorInterface.Instance.SetScriptEditor(_scriptEditor);
			}),
			("加载资源编辑器...", delegate
			{
				LoadResourceEditorPanels();
			}),
			("加载问题面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Issues/GUI/XWIssuePanel.tscn", null, ResourceLoader.CacheMode.Reuse);
				_issuePanel = packedScene.Instantiate<XWIssuePanel>(PackedScene.GenEditState.Disabled);
				_issuePanel.Name = "IssuePanel";
				_layoutManager.AddPanel(_issuePanel, "issues", "问题", 3);
				XWEditorInterface.Instance.SetIssuePanel(_issuePanel);
			}),
			("加载 Mod 工具面板...", delegate
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Tools/GUI/XWModToolsPanel.tscn", null, ResourceLoader.CacheMode.Reuse);
				_modToolsPanel = packedScene.Instantiate<XWModToolsPanel>(PackedScene.GenEditState.Disabled);
				_modToolsPanel.Name = "ModToolsPanel";
				_layoutManager.AddPanel(_modToolsPanel, "mod_tools", "Mod 工具", 9);
			}),
			("初始化 PVZ 集成...", delegate
			{
				PVZIntegrationModule.Initialize();
			})
		};
		int total = steps.Count;
		for (int i = 0; i < total; i++)
		{
			_loadingLabel.Text = steps[i].Item1;
			_loadingBar.Value = (float)i / (float)total * 100f;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			steps[i].Item2();
			_loadingBar.Value = (float)(i + 1) / (float)total * 100f;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		LoadProjectManagerPanel();
		_layoutManager.ApplySavedLayout();
		_layoutManager.AutoSaveLayout();
		XWEditorInterface.Instance?.FocusPanel("bp_editor");
		_loadingLabel.Text = "加载完成";
		_loadingBar.Value = 100.0;
		await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
		_loadingOverlay.QueueFree();
		_loadingOverlay = null;
		ShowProjectManager();
		ShowToast("Mod 编辑器已加载");
	}

	private void LoadProjectManagerPanel()
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ProjectManager/GUI/XWModProjectManagerPanel.tscn", null, ResourceLoader.CacheMode.Reuse);
		_projectManagerPanel = packedScene.Instantiate<XWModProjectManagerPanel>(PackedScene.GenEditState.Disabled);
		_projectManagerPanel.Name = "ProjectManagerPanel";
		_projectManagerPanel.SetAnchorsPreset(LayoutPreset.FullRect);
		_projectManagerPanel.ZIndex = 900;
		_projectManagerPanel.NewProjectRequested += ShowCreateProjectDialog;
		_projectManagerPanel.OpenProjectRequested += ShowOpenProjectDialog;
		_projectManagerPanel.ProjectOpenRequested += OpenProjectFromManager;
		AddChild(_projectManagerPanel, forceReadableName: false, InternalMode.Disabled);
	}

	private void ShowProjectManager()
	{
		if (_projectManagerPanel != null && GodotObject.IsInstanceValid(_projectManagerPanel))
		{
			_scriptEditor?.SetWorkspaceActive(active: false);
			_projectManagerPanel.ShowStartScreen();
			ModEditorManager.Instance?.SetEditorWindowProjectTitle(_currentProject?.Name);
		}
	}

	private void OpenProjectFromManager(string projectFilePath)
	{
		ModProject modProject = ModProject.Load(projectFilePath);
		if (modProject == null)
		{
			ShowToast("打开工程失败");
		}
		else if (EnterProject(modProject))
		{
			ShowToast($"已打开工程: {modProject.Name} ({modProject.ProjectPath})");
		}
	}

	private bool EnterProject(ModProject project)
	{
		if (project == null)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(_scriptEditor) && !_scriptEditor.TrySwitchProjectRoot(project.ProjectPath))
		{
			ShowToast("脚本页仍有未保存修改、外部冲突、构建或调试任务；请处理完成后再切换 Mod。");
			return false;
		}
		if (GodotObject.IsInstanceValid(_bpEditor) && !_bpEditor.TrySwitchProjectRoot(project.ProjectPath))
		{
			ShowToast("蓝图保存失败；已取消切换 Mod。");
			return false;
		}
		_currentProject = project;
		_scriptEditor?.RefreshBuildActionState();
		_modToolsPanel?.UpdateScriptBuildActionState();
		UpdateProjectTitle();
		_projectManagerPanel?.AddRecentProject(project);
		if (_projectManagerPanel != null && GodotObject.IsInstanceValid(_projectManagerPanel))
		{
			_projectManagerPanel.Visible = false;
		}
		NavigateFileSystemToProject(project.ProjectPath);
		XWEditorInterface.Instance?.FocusPanel("bp_editor");
		return true;
	}

	private void OnMainScreenChanged(string screenKey)
	{
		string key = ((!_resourceEditorDescriptors.ContainsKey(screenKey)) ? (screenKey switch
		{
			"blueprint" => "bp_editor", 
			"script" => "script_editor", 
			"2d" => "2d_editor", 
			_ => "bp_editor", 
		}) : (GodotObject.IsInstanceValid(EnsureResourceEditor(screenKey)) ? screenKey : "bp_editor"));
		XWEditorInterface.Instance?.FocusPanel(key);
		CallDeferred("PlayWorkspaceTransition");
	}

	private void PlayWorkspaceTransition()
	{
		_centerWorkspaceMotion?.PlayPanelReveal(9f);
	}

	private static void RegisterBuiltinEditorFactories()
	{
		_ = XWTypeRegistry.Instance;
	}

	private void LoadResourceEditorPanels()
	{
		_resourceEditors.Clear();
		_resourceEditorDescriptors.Clear();
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			_resourceEditorDescriptors[allEditor.DockKey] = allEditor;
			_titleBar.AddResourceEditorOption(allEditor.DockKey, GetMainScreenButtonText(allEditor), GetResourceEditorIcon(allEditor));
		}
	}

	public Control EnsureResourceEditor(string dockKey)
	{
		if (string.IsNullOrWhiteSpace(dockKey))
		{
			return null;
		}
		if (_resourceEditors.TryGetValue(dockKey, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		if (!TryResolveResourceEditorDescriptor(dockKey, out var descriptor))
		{
			return null;
		}
		PackedScene packedScene = LoadResourceEditorScene(descriptor);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return null;
		}
		XWGenericVisualResourceEditor xWGenericVisualResourceEditor = packedScene.Instantiate<XWGenericVisualResourceEditor>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(xWGenericVisualResourceEditor))
		{
			return null;
		}
		xWGenericVisualResourceEditor.Name = descriptor.Category + "Editor";
		xWGenericVisualResourceEditor.LoadResource(null, "", descriptor);
		_resourceEditors[dockKey] = xWGenericVisualResourceEditor;
		_layoutManager.AddPanel(xWGenericVisualResourceEditor, dockKey, descriptor.DisplayName, 2);
		XWEditorInterface.Instance?.SetResourceEditor(dockKey, xWGenericVisualResourceEditor);
		return xWGenericVisualResourceEditor;
	}

	private bool TryResolveResourceEditorDescriptor(string dockKey, out XWVisualEditorDescriptor descriptor)
	{
		if (_resourceEditorDescriptors.TryGetValue(dockKey, out descriptor))
		{
			return true;
		}
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			if (string.Equals(allEditor.DockKey, dockKey, StringComparison.Ordinal))
			{
				descriptor = allEditor;
				_resourceEditorDescriptors[dockKey] = allEditor;
				return true;
			}
		}
		descriptor = null;
		return false;
	}

	private static string GetMainScreenButtonText(XWVisualEditorDescriptor descriptor)
	{
		string text = descriptor?.DisplayName ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = descriptor?.Category ?? "";
		}
		return text.Replace("编辑器", "").Trim();
	}

	private static Texture2D GetResourceEditorIcon(XWVisualEditorDescriptor descriptor)
	{
		string text = ((descriptor == null) ? "" : descriptor.IconPath);
		if (!string.IsNullOrWhiteSpace(text) && ResourceLoader.Exists(text))
		{
			return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(text, null, ResourceLoader.CacheMode.Reuse));
		}
		if (!ResourceLoader.Exists("res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg"))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg", null, ResourceLoader.CacheMode.Reuse));
	}

	private static PackedScene LoadResourceEditorScene(XWVisualEditorDescriptor descriptor)
	{
		if (descriptor == null)
		{
			return null;
		}
		string text = descriptor.ScenePath;
		if (string.IsNullOrWhiteSpace(text) || !ResourceLoader.Exists(text))
		{
			text = "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn";
		}
		if (!ResourceLoader.Exists(text))
		{
			return null;
		}
		return ResourceLoader.Load<PackedScene>(text, null, ResourceLoader.CacheMode.Reuse);
	}

	private void ConnectRunBarSignals()
	{
		if (_runBar != null)
		{
			_runBar.PlayPressed += OnRunMainPressed;
			_runBar.PlayScenePressed += OnRunCurrentScenePressed;
			_runBar.PlayCustomPressed += OnRunCustomPressed;
			_runBar.StopPressed += OnRunStopPressed;
			_runBar.PauseToggled += OnRunPauseToggled;
		}
	}

	private void OnRunMainPressed()
	{
		LogRunMessage("运行主场景");
		string displayName;
		PackedScene packedScene = TryLoadProjectMainScene(out displayName);
		if (packedScene != null)
		{
			StartRunPreview(packedScene, displayName, XWEditorRunBar.RunMode.RunMain);
			return;
		}
		LogRunMessage("未找到工程主场景，改为运行当前 2D 场景。", XWOutputPanel.MessageType.Warning);
		RunCurrent2DScenePreview(XWEditorRunBar.RunMode.RunMain, "当前 2D 场景");
	}

	private void OnRunCurrentScenePressed()
	{
		RunCurrent2DScenePreview(XWEditorRunBar.RunMode.RunCurrent, "当前 2D 场景");
	}

	private void OnRunCustomPressed()
	{
		LogRunMessage("选择自定义运行场景");
		_runBar?.SetRunning(running: false);
		ShowRunCustomSceneDialog();
	}

	private void OnRunStopPressed()
	{
		StopRunPreview(updateRunBar: true, "停止运行");
	}

	private void OnRunPauseToggled(bool paused)
	{
		if (_runPreviewRoot == null || !GodotObject.IsInstanceValid(_runPreviewRoot))
		{
			_runBar?.SetPaused(paused: false);
			ReportRunError("没有正在运行的场景。");
			return;
		}
		SetRunPreviewPaused(paused);
		_runBar?.SetPaused(paused);
		LogRunMessage(paused ? "暂停运行" : "继续运行");
		ShowToast(paused ? "已暂停运行预览" : "已继续运行预览");
	}

	private void RunCurrent2DScenePreview(XWEditorRunBar.RunMode mode, string label)
	{
		PackedScene packedScene = CreatePackedSceneFromCurrent2D();
		if (packedScene == null)
		{
			_runBar?.SetRunning(running: false);
		}
		else
		{
			StartRunPreview(packedScene, GetCurrent2DSceneDisplayName(label), mode);
		}
	}

	private PackedScene CreatePackedSceneFromCurrent2D()
	{
		Node node = _scene2DEditor?.CurrentSceneInstance;
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			ReportRunError("没有打开的 2D 场景。");
			return null;
		}
		PackedScene packedScene = new PackedScene();
		Error error = packedScene.Pack(node);
		if (error != Error.Ok)
		{
			ReportRunError($"打包当前 2D 场景失败: {error}");
			return null;
		}
		return packedScene;
	}

	private PackedScene TryLoadProjectMainScene(out string displayName)
	{
		displayName = "";
		string text = FindProjectMainScenePath();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string text2 = text.Replace('\\', '/');
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(text2, null, ResourceLoader.CacheMode.Reuse);
		if (packedScene == null)
		{
			LogRunMessage("无法加载工程主场景: " + text2, XWOutputPanel.MessageType.Warning);
			return null;
		}
		displayName = Path.GetFileName(text);
		return packedScene;
	}

	private string FindProjectMainScenePath()
	{
		if (_currentProject == null || string.IsNullOrWhiteSpace(_currentProject.ProjectPath))
		{
			return "";
		}
		string text = Path.Combine(_currentProject.ProjectPath, "Scenes");
		if (!Directory.Exists(text))
		{
			return "";
		}
		string[] array = new string[4] { "Main.tscn", "main.tscn", "Test.tscn", "test.tscn" };
		foreach (string path in array)
		{
			string text2 = Path.Combine(text, path);
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		List<string> list = new List<string>(Directory.EnumerateFiles(text, "*.tscn", SearchOption.AllDirectories));
		list.Sort(StringComparer.OrdinalIgnoreCase);
		if (list.Count <= 0)
		{
			return "";
		}
		return list[0];
	}

	private void ShowRunCustomSceneDialog()
	{
		if (_runCustomSceneDialog == null || !GodotObject.IsInstanceValid(_runCustomSceneDialog))
		{
			if (_runCustomSceneDialogScene == null)
			{
				_runCustomSceneDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWRunCustomSceneDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_runCustomSceneDialog = _runCustomSceneDialogScene?.Instantiate<FileDialog>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_runCustomSceneDialog))
			{
				ReportRunError("无法加载自定义场景选择对话框。");
				return;
			}
			_runCustomSceneDialog.FileSelected += OnRunCustomSceneSelected;
			AddChild(_runCustomSceneDialog, forceReadableName: false, InternalMode.Disabled);
		}
		if (_currentProject != null && !string.IsNullOrWhiteSpace(_currentProject.ProjectPath))
		{
			string text = Path.Combine(_currentProject.ProjectPath, "Scenes");
			if (Directory.Exists(text))
			{
				_runCustomSceneDialog.CurrentDir = text;
			}
		}
		_runCustomSceneDialog.PopupCenteredClamped(new Vector2I(700, 500), 0.9f);
	}

	private void OnRunCustomSceneSelected(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			_runBar?.SetRunning(running: false);
			return;
		}
		string text = path.Replace('\\', '/');
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(text, null, ResourceLoader.CacheMode.Reuse);
		if (packedScene == null)
		{
			_runBar?.SetRunning(running: false);
			ReportRunError("无法加载场景: " + text);
		}
		else
		{
			StartRunPreview(packedScene, Path.GetFileName(path), XWEditorRunBar.RunMode.RunCustom);
		}
	}

	private void StartRunPreview(PackedScene packedScene, string displayName, XWEditorRunBar.RunMode runMode)
	{
		if (packedScene == null)
		{
			_runBar?.SetRunning(running: false);
			return;
		}
		Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		if (node != null)
		{
			Node sceneRoot = node;
			StopRunPreview(updateRunBar: false);
			if (!ShowRunPreviewWindow(sceneRoot, displayName))
			{
				_runBar?.SetRunning(running: false);
				return;
			}
			_runBar?.SetRunMode(runMode);
			_runBar?.SetPaused(paused: false);
			LogRunMessage("开始运行: " + displayName);
			ShowToast("正在运行: " + displayName);
		}
		else
		{
			_runBar?.SetRunning(running: false);
			ReportRunError("场景没有有效根节点。");
		}
	}

	private bool ShowRunPreviewWindow(Node sceneRoot, string displayName)
	{
		Vector2I value = new Vector2I(960, 540);
		if (_runPreviewWindowScene == null)
		{
			_runPreviewWindowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWRunPreviewWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_runPreviewWindow = _runPreviewWindowScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_runPreviewWindow))
		{
			if (GodotObject.IsInstanceValid(sceneRoot))
			{
				sceneRoot.QueueFree();
			}
			ReportRunError("无法加载运行预览窗口场景。");
			return false;
		}
		_runPreviewWindow.Title = "运行预览 - " + displayName;
		_runPreviewWindow.CloseRequested += delegate
		{
			StopRunPreview(updateRunBar: true, "关闭运行预览");
		};
		AddChild(_runPreviewWindow, forceReadableName: false, InternalMode.Disabled);
		_runPreviewViewport = _runPreviewWindow.GetNode<SubViewport>("RunPreviewContainer/RunPreviewViewport");
		_runPreviewRoot = sceneRoot;
		_runPreviewViewport.AddChild(_runPreviewRoot, forceReadableName: false, InternalMode.Disabled);
		if (_runPreviewRoot is Control control)
		{
			control.SetAnchorsPreset(LayoutPreset.FullRect);
		}
		CacheRunPreviewProcessModes(_runPreviewRoot);
		_runPreviewWindow.PopupCenteredClamped(value, 0.9f);
		return true;
	}

	private void StopRunPreview(bool updateRunBar, string message = "")
	{
		if (_runPreviewWindow != null && GodotObject.IsInstanceValid(_runPreviewWindow))
		{
			_runPreviewWindow.QueueFree();
		}
		_runPreviewWindow = null;
		_runPreviewViewport = null;
		_runPreviewRoot = null;
		_runPreviewProcessModes.Clear();
		if (updateRunBar)
		{
			_runBar?.SetRunning(running: false);
			if (!string.IsNullOrWhiteSpace(message))
			{
				LogRunMessage(message);
				ShowToast(message);
			}
		}
	}

	private void CacheRunPreviewProcessModes(Node node)
	{
		_runPreviewProcessModes.Clear();
		CacheRunPreviewProcessModesRecursive(node);
	}

	private void CacheRunPreviewProcessModesRecursive(Node node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		_runPreviewProcessModes[node] = node.ProcessMode;
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				CacheRunPreviewProcessModesRecursive(node2);
			}
		}
	}

	private void SetRunPreviewPaused(bool paused)
	{
		foreach (KeyValuePair<Node, ProcessModeEnum> runPreviewProcessMode in _runPreviewProcessModes)
		{
			if (GodotObject.IsInstanceValid(runPreviewProcessMode.Key))
			{
				runPreviewProcessMode.Key.ProcessMode = (paused ? ProcessModeEnum.Disabled : runPreviewProcessMode.Value);
			}
		}
	}

	private string GetCurrent2DSceneDisplayName(string fallback)
	{
		string text = _scene2DEditor?.CurrentPackedScene?.ResourcePath ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.GetFile();
		}
		string text2 = _scene2DEditor?.CurrentSceneInstance?.Name ?? ((StringName)"");
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return fallback;
	}

	private void LogRunMessage(string message, XWOutputPanel.MessageType type = XWOutputPanel.MessageType.Editor)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			_outputPanel?.AddMessage(message, type);
		}
	}

	private void ReportRunError(string message)
	{
		LogRunMessage(message, XWOutputPanel.MessageType.Error);
		ShowToast(message);
	}

	private void OnFileMenuItem(long id)
	{
		if ((ulong)id <= 6uL)
		{
			switch ((int)id)
			{
			case 0:
				ShowCreateProjectDialog();
				return;
			case 1:
				ShowOpenProjectDialog();
				return;
			case 2:
				SaveActiveDocument();
				return;
			case 6:
				SaveAllDocuments();
				return;
			case 3:
				ShowExportDialog();
				return;
			case 4:
				ModEditorManager.Instance.CloseEditor();
				return;
			case 5:
				ShowProjectManager();
				return;
			}
		}
		ShowToast($"文件菜单项 {id}（待实现）");
	}

	private void OnEditMenuItem(long id)
	{
		_scene2DEditor?.CommitPendingAuthoringInput();
		switch (id)
		{
		case 10L:
			_undoRedoManager.Undo();
			break;
		case 11L:
			_undoRedoManager.Redo();
			break;
		}
	}

	private async void OnModMenuItem(long id)
	{
		long num = id - 20;
		if ((ulong)num > 3uL)
		{
			return;
		}
		switch ((int)num)
		{
		case 0:
		{
			if (OperatingSystem.IsAndroid())
			{
				ShowToast("Android 上的 Mod 状态修改将在重启游戏后生效");
				break;
			}
			int num2 = ModLoader.LoadAll();
			ShowToast((num2 < 0) ? "Mod 重载被仍在使用的运行实例阻止" : ((num2 > 0) ? $"已重新应用 {num2} 个已启用 Mod" : "没有已启用且可安全应用的 Mod；请查看输出诊断"));
			break;
		}
		case 1:
		{
			string text = ProjectSettings.GlobalizePath("user://Mods/");
			Directory.CreateDirectory(text);
			OS.ShellOpen(text);
			ShowToast("已打开 Mod 安装目录");
			break;
		}
		case 2:
			if (_currentProject == null)
			{
				ShowToast("没有打开的工程，请先新建或打开工程");
			}
			else
			{
				await ExportCurrentProjectAsync("");
			}
			break;
		case 3:
			ShowProjectSettingsDialog();
			break;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey)
		{
			if (inputEventKey.Keycode == Key.S && inputEventKey.CtrlPressed && !inputEventKey.ShiftPressed && !inputEventKey.AltPressed)
			{
				SaveActiveDocument();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.Z && inputEventKey.CtrlPressed)
			{
				_scene2DEditor?.CommitPendingAuthoringInput();
				_undoRedoManager.Undo();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.Y && inputEventKey.CtrlPressed)
			{
				_scene2DEditor?.CommitPendingAuthoringInput();
				_undoRedoManager.Redo();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 21)
		{
			if (GetViewport().GuiGetDragData().Obj is XWDragData xWDragData && xWDragData.IsType(XWDragData.Type.DockPanel))
			{
				_isDockDragging = true;
				SetProcess(enable: true);
				_dragPanelKey = xWDragData.GetPanelKey();
				_dockOverlay.CenterZoneEnabled = _layoutManager?.CanPanelMoveToDock(_dragPanelKey, 2) ?? false;
				UpdateDockOverlayZones();
				_dockOverlay.Visible = true;
				_dockOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
				_dockOverlay.CreateTween().TweenProperty(_dockOverlay, "modulate:a", 1f, 0.2);
			}
		}
		else
		{
			if ((long)what != 22 || !_isDockDragging)
			{
				return;
			}
			_isDockDragging = false;
			SetProcess(enable: false);
			if (_dockOverlay != null)
			{
				int highlightZone = _dockOverlay.GetHighlightZone();
				_dockOverlay.CenterZoneEnabled = true;
				_dockOverlay.ClearHighlight();
				Tween tween = _dockOverlay.CreateTween();
				tween.TweenProperty(_dockOverlay, "modulate:a", 0f, 0.15);
				tween.TweenCallback(Callable.From(delegate
				{
					if (_dockOverlay != null)
					{
						_dockOverlay.Visible = false;
					}
				}));
				if (!string.IsNullOrEmpty(_dragPanelKey))
				{
					int panelCurrentDock = _layoutManager.GetPanelCurrentDock(_dragPanelKey);
					if (highlightZone >= 0 && highlightZone != panelCurrentDock)
					{
						if (_layoutManager.CanPanelMoveToDock(_dragPanelKey, highlightZone))
						{
							_layoutManager.MovePanelToDock(_dragPanelKey, highlightZone);
						}
					}
					else if (highlightZone >= 0 && highlightZone == panelCurrentDock)
					{
						_layoutManager.GetDockContainer(highlightZone)?.ReorderTab(_dragPanelKey, GetGlobalMousePosition());
					}
				}
			}
			_dragPanelKey = "";
		}
	}

	public override void _Process(double delta)
	{
		if (_isDockDragging && _dockOverlay != null)
		{
			Vector2 globalMousePosition = GetGlobalMousePosition();
			_dockOverlay.UpdateHighlight(globalMousePosition);
		}
	}

	private void UpdateDockOverlayZones()
	{
		if (_dockOverlay != null)
		{
			Rect2 rect = default(Rect2);
			Rect2 value = default(Rect2);
			Rect2 value2 = default(Rect2);
			Rect2 value3 = default(Rect2);
			Rect2 rect2 = default(Rect2);
			Rect2 value4 = default(Rect2);
			Rect2 value5 = default(Rect2);
			Rect2 value6 = default(Rect2);
			Rect2 rect3 = default(Rect2);
			Rect2 rect4 = default(Rect2);
			if (_leftDock != null && _leftDock.Visible)
			{
				rect = new Rect2(_leftDock.GlobalPosition - base.GlobalPosition, _leftDock.Size);
			}
			if (_leftBottomDock != null && _leftBottomDock.Visible)
			{
				value = new Rect2(_leftBottomDock.GlobalPosition - base.GlobalPosition, _leftBottomDock.Size);
			}
			if (_leftTopRightDock != null && _leftTopRightDock.Visible)
			{
				value2 = new Rect2(_leftTopRightDock.GlobalPosition - base.GlobalPosition, _leftTopRightDock.Size);
			}
			if (_leftBottomRightDock != null && _leftBottomRightDock.Visible)
			{
				value3 = new Rect2(_leftBottomRightDock.GlobalPosition - base.GlobalPosition, _leftBottomRightDock.Size);
			}
			if (_rightDock != null && _rightDock.Visible)
			{
				rect2 = new Rect2(_rightDock.GlobalPosition - base.GlobalPosition, _rightDock.Size);
			}
			if (_rightBottomDock != null && _rightBottomDock.Visible)
			{
				value4 = new Rect2(_rightBottomDock.GlobalPosition - base.GlobalPosition, _rightBottomDock.Size);
			}
			if (_rightTopRightDock != null && _rightTopRightDock.Visible)
			{
				value5 = new Rect2(_rightTopRightDock.GlobalPosition - base.GlobalPosition, _rightTopRightDock.Size);
			}
			if (_rightBottomRightDock != null && _rightBottomRightDock.Visible)
			{
				value6 = new Rect2(_rightBottomRightDock.GlobalPosition - base.GlobalPosition, _rightBottomRightDock.Size);
			}
			if (_centerDock != null && _centerDock.Visible)
			{
				rect3 = new Rect2(_centerDock.GlobalPosition - base.GlobalPosition, _centerDock.Size);
			}
			if (_bottomPanel != null && _bottomPanel.Visible)
			{
				rect4 = new Rect2(_bottomPanel.GlobalPosition - base.GlobalPosition, _bottomPanel.Size);
			}
			Dictionary<int, Rect2> subZones = new Dictionary<int, Rect2>
			{
				{ 4, rect },
				{ 5, value },
				{ 6, value2 },
				{ 7, value3 },
				{ 2, rect3 },
				{ 8, rect2 },
				{ 9, value4 },
				{ 10, value5 },
				{ 11, value6 },
				{ 3, rect4 }
			};
			_dockOverlay.UpdateZones(rect, rect2, rect3, rect4, subZones);
		}
	}

	private void ShowCreateProjectDialog()
	{
		if (_createProjectDialog == null || !GodotObject.IsInstanceValid(_createProjectDialog))
		{
			if (_createProjectDialogScene == null)
			{
				_createProjectDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ModSystem/ModProjectCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_createProjectDialog = _createProjectDialogScene?.Instantiate<ModProjectCreateDialog>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_createProjectDialog))
			{
				ShowToast("无法加载新建 Mod 工程对话框");
				return;
			}
			AddChild(_createProjectDialog, forceReadableName: false, InternalMode.Disabled);
			_createProjectDialog.Confirmed += OnCreateProjectConfirmed;
		}
		_createProjectDialog.PopupCentered();
	}

	private void OnCreateProjectConfirmed()
	{
		string modName = _createProjectDialog.ModName;
		string selectedParentDir = _createProjectDialog.SelectedParentDir;
		if (string.IsNullOrWhiteSpace(modName))
		{
			ShowToast("工程名称不能为空");
			_createProjectDialog.PopupCentered();
			return;
		}
		if (string.IsNullOrWhiteSpace(selectedParentDir))
		{
			ShowToast("请输入或选择工程保存位置");
			_createProjectDialog.PopupCentered();
			return;
		}
		if (!DirAccess.DirExistsAbsolute(selectedParentDir))
		{
			DirAccess.MakeDirRecursiveAbsolute(selectedParentDir);
			if (!DirAccess.DirExistsAbsolute(selectedParentDir))
			{
				ShowToast("目录无效或无法创建: " + selectedParentDir);
				_createProjectDialog.PopupCentered();
				return;
			}
		}
		ModProject modProject = ModProject.Create(selectedParentDir, modName, _createProjectDialog.ModVersion, _createProjectDialog.ModAuthor, _createProjectDialog.ModDescription);
		if (modProject == null)
		{
			ShowToast("创建工程失败，请检查名称和路径");
			_createProjectDialog.PopupCentered();
		}
		else if (EnterProject(modProject))
		{
			ShowToast($"已创建工程: {modProject.Name} ({modProject.ProjectPath})");
		}
	}

	private void ShowOpenProjectDialog()
	{
		if (_openProjectFileDialog == null || !GodotObject.IsInstanceValid(_openProjectFileDialog))
		{
			if (_openProjectDialogScene == null)
			{
				_openProjectDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWOpenProjectDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_openProjectFileDialog = _openProjectDialogScene?.Instantiate<FileDialog>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_openProjectFileDialog))
			{
				ShowToast("无法加载打开工程对话框");
				return;
			}
			_openProjectFileDialog.FileSelected += OnOpenProjectFileSelected;
			AddChild(_openProjectFileDialog, forceReadableName: false, InternalMode.Disabled);
		}
		_openProjectFileDialog.PopupCentered();
	}

	private void OnOpenProjectFileSelected(string path)
	{
		OpenProjectFromManager(path);
	}

	public bool SaveActiveDocument()
	{
		bool flag = false;
		bool result = false;
		string text = _layoutManager?.ActiveMainPanelKey ?? "";
		bool num = text == "script_editor" || (string.IsNullOrEmpty(text) && GodotObject.IsInstanceValid(_scriptEditor) && _scriptEditor.IsVisibleInTree());
		bool flag2 = text == "2d_editor" || (string.IsNullOrEmpty(text) && GodotObject.IsInstanceValid(_scene2DEditor) && _scene2DEditor.IsVisibleInTree());
		bool flag3 = text == "bp_editor" || (string.IsNullOrEmpty(text) && GodotObject.IsInstanceValid(_bpEditor) && _bpEditor.IsVisibleInTree());
		XWGenericVisualResourceEditor value;
		if (num && GodotObject.IsInstanceValid(_scriptEditor) && _scriptEditor.HasFileOpen())
		{
			flag = true;
			result = _scriptEditor.SaveFile();
		}
		else if (flag2 && GodotObject.IsInstanceValid(_scene2DEditor) && GodotObject.IsInstanceValid(_scene2DEditor.CurrentSceneInstance))
		{
			flag = true;
			result = _scene2DEditor.SaveCurrentScene();
		}
		else if (flag3 && GodotObject.IsInstanceValid(_bpEditor) && _bpEditor.HasEditScript())
		{
			flag = true;
			result = _bpEditor.FlushBlueprintPersistence();
		}
		else if (_resourceEditors.TryGetValue(text, out value) && GodotObject.IsInstanceValid(value) && GodotObject.IsInstanceValid(value.ActiveResource))
		{
			flag = true;
			result = value.SaveActiveResource();
		}
		bool result2 = SaveCurrentProject(!flag);
		if (!flag)
		{
			return result2;
		}
		return result;
	}

	public bool SaveAllDocuments()
	{
		bool flag = true;
		if (GodotObject.IsInstanceValid(_scriptEditor))
		{
			flag &= _scriptEditor.SaveAllTabs() >= 0;
		}
		if (GodotObject.IsInstanceValid(_scene2DEditor))
		{
			flag &= _scene2DEditor.SaveAllScenes(showToast: false) >= 0;
		}
		if (GodotObject.IsInstanceValid(_bpEditor))
		{
			flag &= _bpEditor.SaveAllBlueprints(showToast: false) >= 0;
		}
		foreach (XWGenericVisualResourceEditor value in _resourceEditors.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				flag &= value.SaveAllOpenResources(showToast: false);
			}
		}
		flag &= SaveCurrentProject(showToast: false);
		ShowToast(flag ? "已保存全部文档与 Mod 项目。" : "保存全部已完成，但有文档保存失败，请查看输出面板。");
		return flag;
	}

	private bool SaveCurrentProject(bool showToast = true)
	{
		if (_currentProject == null)
		{
			if (showToast)
			{
				ShowToast("没有打开的工程，请先新建或打开工程");
			}
			return false;
		}
		_currentProject.Save();
		UpdateProjectTitle();
		if (showToast)
		{
			ShowToast("已保存工程: " + _currentProject.Name);
		}
		return true;
	}

	private void ShowProjectSettingsDialog()
	{
		if (_projectSettingsDialog == null || !GodotObject.IsInstanceValid(_projectSettingsDialog))
		{
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ProjectSettings/GUI/XWProjectSettingsDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
			_projectSettingsDialog = packedScene.Instantiate<XWProjectSettingsDialog>(PackedScene.GenEditState.Disabled);
			_projectSettingsDialog.ProjectSaved += OnProjectSettingsSaved;
			AddChild(_projectSettingsDialog, forceReadableName: false, InternalMode.Disabled);
		}
		_projectSettingsDialog.EditProject(_currentProject, _editorSettings);
		_projectSettingsDialog.PopupCenteredClamped(new Vector2I(720, 560), 0.9f);
	}

	private void OnProjectSettingsSaved()
	{
		UpdateProjectTitle();
		if (_currentProject != null)
		{
			NavigateFileSystemToProject(_currentProject.ProjectPath);
		}
	}

	private void UpdateProjectTitle()
	{
		string editorWindowProjectTitle = _currentProject?.Name;
		_titleBar?.SetProjectTitle("");
		ModEditorManager.Instance?.SetEditorWindowProjectTitle(editorWindowProjectTitle);
	}

	private void ShowExportDialog()
	{
		if (_currentProject == null)
		{
			ShowToast("没有打开的工程，请先新建或打开工程");
			return;
		}
		if (_isExporting)
		{
			ShowToast("Mod 正在后台编译并导出，请稍候");
			return;
		}
		if (_exportDirDialog == null || !GodotObject.IsInstanceValid(_exportDirDialog))
		{
			if (_exportDirectoryDialogScene == null)
			{
				_exportDirectoryDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWExportDirectoryDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_exportDirDialog = _exportDirectoryDialogScene?.Instantiate<FileDialog>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_exportDirDialog))
			{
				ShowToast("无法加载导出目录对话框");
				return;
			}
			_exportDirDialog.DirSelected += OnExportDirSelected;
			AddChild(_exportDirDialog, forceReadableName: false, InternalMode.Disabled);
		}
		_exportDirDialog.CurrentDir = (string.IsNullOrWhiteSpace(_currentProject.ExportDirectory) ? ProjectSettings.GlobalizePath("user://Mods/") : _currentProject.ExportDirectory);
		_exportDirDialog.PopupCentered();
	}

	private async void OnExportDirSelected(string dir)
	{
		if (_currentProject == null)
		{
			ShowToast("没有打开的工程");
		}
		else
		{
			await ExportCurrentProjectAsync(dir);
		}
	}

	private async Task ExportCurrentProjectAsync(string outputDirectory)
	{
		if (_isExporting)
		{
			ShowToast("Mod 正在后台编译并导出，请稍候");
			return;
		}
		ModProject currentProject = _currentProject;
		if (currentProject == null)
		{
			ShowToast("没有打开的工程");
			return;
		}
		if (!SaveAllDocuments())
		{
			string message = "导出已停止：有文档保存失败，请先修复保存问题。";
			LogRunMessage(message, XWOutputPanel.MessageType.Error);
			ShowToast(message);
			return;
		}
		CancellationTokenSource exportLifetimeCts = _exportLifetimeCts;
		if (exportLifetimeCts == null || exportLifetimeCts.IsCancellationRequested)
		{
			return;
		}
		_isExporting = true;
		ShowToast("正在后台编译并导出 Mod…");
		ModProject.ExportResult exportResult;
		try
		{
			exportResult = await currentProject.ExportAsync(outputDirectory, exportLifetimeCts.Token);
		}
		catch (Exception ex)
		{
			exportResult = new ModProject.ExportResult
			{
				Success = false,
				ErrorMessage = "Mod 导出出现未处理错误：" + ex.Message
			};
		}
		finally
		{
			_isExporting = false;
		}
		if (!GodotObject.IsInstanceValid(this))
		{
			return;
		}
		if (exportResult.Issues.Count > 0)
		{
			if (XWEditorInterface.Instance?.GetIssuePanel() is XWIssuePanel xWIssuePanel)
			{
				xWIssuePanel.LoadIssues(exportResult.Issues);
				XWEditorInterface.Instance.FocusPanel("issues");
			}
			foreach (XWValidationIssue issue in exportResult.Issues)
			{
				LogRunMessage(issue.Message, XWOutputPanel.MessageType.Std);
			}
		}
		if (exportResult.Success)
		{
			string text = (exportResult.ContainsRuntimeAssembly ? "已导出 Mod（包含已编译的 C# 运行程序集）" : "已导出纯资源 Mod（无需 C# 运行程序集）");
			LogRunMessage(text + ": " + exportResult.OutputPath, XWOutputPanel.MessageType.Std);
			ShowToast(text + ": " + exportResult.OutputPath);
		}
		else
		{
			string message2 = (string.IsNullOrWhiteSpace(exportResult.ErrorMessage) ? "Mod 导出失败，请查看输出面板。" : exportResult.ErrorMessage);
			LogRunMessage(message2, XWOutputPanel.MessageType.Error);
			ShowToast(message2);
		}
	}

	public ModProject GetCurrentProject()
	{
		return _currentProject;
	}

	private void NavigateFileSystemToProject(string projectDir)
	{
		if (_fileSystemPanel != null && GodotObject.IsInstanceValid(_fileSystemPanel))
		{
			_fileSystemPanel.NavigateToProject(projectDir);
		}
	}

	public void ShowToast(string message)
	{
		if (_statusLabel != null)
		{
			_statusLabel.Text = message;
		}
		_toaster?.ShowToast(message);
	}

	private void OnEditorActionCommitted(string actionName)
	{
		LogEditorAction(actionName);
	}

	private void OnEditorActionUndone(string actionName)
	{
		LogEditorUndo(actionName);
	}

	private void OnEditorActionRedone(string actionName)
	{
		LogEditorRedo(actionName);
	}

	public void LogEditorAction(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			_outputPanel?.AddMessage("动作: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public void LogEditorUndo(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			_outputPanel?.AddMessage("撤销: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public void LogEditorRedo(string actionName)
	{
		if (!string.IsNullOrWhiteSpace(actionName))
		{
			_outputPanel?.AddMessage("重做: " + actionName, XWOutputPanel.MessageType.Editor);
		}
	}

	public XWOutputPanel GetOutputPanel()
	{
		return _outputPanel;
	}

	public XWEditorRunBar GetRunBar()
	{
		return _runBar;
	}

	public XWModToolsPanel GetModToolsPanel()
	{
		return _modToolsPanel;
	}

	public XWSceneTreeDock GetSceneTreeDock()
	{
		return _sceneTreeDock;
	}

	public XW2DSceneEditor Get2DSceneEditor()
	{
		return _scene2DEditor;
	}
}
