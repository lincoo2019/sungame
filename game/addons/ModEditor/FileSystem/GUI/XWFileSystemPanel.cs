using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.PVZIntegration;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileSystemPanel : PanelContainer
{
	[Signal]
	public delegate void ImportDockRequestedEventHandler(string resourcePath);

	private static readonly string[] SceneExtensions = new string[2] { "tscn", "scn" };

	private const string NameInputDialogScenePath = "res://addons/ModEditor/FileSystem/GUI/Dialog/XWNameInputDialog.tscn";

	private const string ScriptCreateDialogScenePath = "res://addons/ModEditor/FileSystem/GUI/Dialog/XWScriptCreateDialog.tscn";

	private const string ResourceTypeCreateDialogScenePath = "res://addons/ModEditor/GUI/XWCreateDialog.tscn";

	private static PackedScene _nameInputDialogScene;

	private static PackedScene _scriptCreateDialogScene;

	private static PackedScene _resourceTypeCreateDialogScene;

	private LineEdit _pathEdit;

	private LineEdit _searchEdit;

	private LineEdit _listSearchEdit;

	private Button _histPrevBtn;

	private Button _histNextBtn;

	private Button _reloadBtn;

	private MenuButton _createButton;

	private Button _toggleDisplayModeBtn;

	private Button _fileListDisplayModeBtn;

	private HBoxContainer _toolbar2HBox;

	private MenuButton _treeSortButton;

	private MenuButton _fileListSortButton;

	private SplitContainer _splitBox;

	private VBoxContainer _fileListVBox;

	private XWFileSystemTree _tree;

	private XWFileSystemList _list;

	private Label _scanningLabel;

	private ProgressBar _scanningProgress;

	private VBoxContainer _scanningVBox;

	private TextureRect _previewTextureRect;

	private Label _previewFileNameLabel;

	private PanelContainer _previewTextureBackground;

	private AdobeAnimateInspectorPreview _animationFilePreview;

	private PopupMenu _treePopupMenu;

	private PopupMenu _fileListPopupMenu;

	private XWDirectoryCreateDialog _createDirDialog;

	private XWRemoveConfirmDialog _removeDialog;

	private string _currentDirPath = "";

	private string _rmbSelectedPath = "";

	private XWFileSystemTreeItemData _rmbSelectedData;

	private string _clipboardPath = "";

	private bool _clipboardIsCopy;

	private readonly List<string> _navigationHistory = new List<string>();

	private int _navigationIndex = -1;

	private XWFileSystemEnum.DisplayMode _displayMode;

	private XWFileSystemEnum.FileListDisplayMode _fileListDisplayMode = XWFileSystemEnum.FileListDisplayMode.List;

	private int _splitBoxOffsetV = 120;

	private int _splitBoxOffsetH = 180;

	private bool _splitDraggedConnected;

	private bool _fileSystemSignalsConnected;

	private bool _externalDropConnected;

	private Window _externalDropWindow;

	private bool _animationImportInProgress;

	private CancellationTokenSource _animationImportCancellation;

	private XWProgressDialog _animationImportProgressDialog;

	private FileDialog _animationImportFileDialog;

	private FileDialog _newTextFileDialog;

	private FileDialog _moveToDirectoryDialog;

	private string _moveSourcePath = "";

	private string _animationImportTargetDir = "";

	private bool _splitClampPending;

	private readonly Dictionary<int, string> _resourceCreateActionIds = new Dictionary<int, string>();

	private readonly Dictionary<int, string> _subResourceCreateActionIds = new Dictionary<int, string>();

	private const int ResourceCreateActionBaseId = 10000;

	private const int SubResourceCreateActionBaseId = 11000;

	private const int AdvancedResourceCreateId = 12000;

	private string _toolbarCreateTargetDir = "";

	private readonly XWModComponentAssemblyCatalog _componentAssemblyCatalog = new XWModComponentAssemblyCatalog();

	private readonly CancellationTokenSource _componentAssemblyRefreshCancellation = new CancellationTokenSource();

	private readonly object _componentAssemblyResultGate = new object();

	private readonly Dictionary<long, (XWTemplateLibrary.TemplateCreateResult Create, XWModComponentAssemblyCatalog.RefreshResult Refresh)> _componentAssemblyResults = new Dictionary<long, (XWTemplateLibrary.TemplateCreateResult, XWModComponentAssemblyCatalog.RefreshResult)>();

	private long _nextComponentAssemblyResultId;

	private int _animationInspectRequestVersion;

	public static XWFileSystemPanel Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		_pathEdit = GetNode<LineEdit>("%CurrentPathLineEdit");
		_searchEdit = GetNode<LineEdit>("%TreeSearchBox");
		_listSearchEdit = GetNodeOrNull<LineEdit>("%FileListSearchBox");
		_histPrevBtn = GetNode<Button>("%ButtonHistPrev");
		_histNextBtn = GetNode<Button>("%ButtonHistNext");
		_reloadBtn = GetNode<Button>("%ButtonReload");
		_createButton = GetNode<MenuButton>("%ButtonCreate");
		_toggleDisplayModeBtn = GetNode<Button>("%ButtonToggleDisplayMode");
		_fileListDisplayModeBtn = GetNode<Button>("%ButtonFileListDisplayMode");
		_toolbar2HBox = GetNode<HBoxContainer>("%Toolbar2HBox");
		_treeSortButton = GetNode<MenuButton>("%TreeButtonSort");
		_fileListSortButton = GetNode<MenuButton>("%FileListButtonSort");
		_splitBox = GetNode<SplitContainer>("%SplitBox");
		_fileListVBox = GetNode<VBoxContainer>("%FileListVBox");
		_tree = GetNode<XWFileSystemTree>("%FileTree");
		_list = GetNode<XWFileSystemList>("%FileList");
		_scanningVBox = GetNode<VBoxContainer>("%ScanningVBox");
		_scanningLabel = GetNode<Label>("%ScanningLabel");
		_scanningProgress = GetNode<ProgressBar>("%ScanningProgress");
		_previewTextureRect = GetNode<TextureRect>("%PreviewTextureRect");
		_previewFileNameLabel = GetNode<Label>("%PreviewFileNameLabel");
		_previewTextureBackground = GetNode<PanelContainer>("%PreviewTextureBackground");
		_animationFilePreview = GetNode<AdobeAnimateInspectorPreview>("%AnimationFilePreview");
		_treePopupMenu = GetNode<PopupMenu>("%TreePopupMenu");
		_fileListPopupMenu = GetNode<PopupMenu>("%FileListPopupMenu");
		_animationImportFileDialog = GetNode<FileDialog>("%AnimationImportFileDialog");
		_newTextFileDialog = GetNode<FileDialog>("%NewTextFileDialog");
		_moveToDirectoryDialog = GetNode<FileDialog>("%MoveToDirectoryDialog");
		_reloadBtn.Pressed += OnReload;
		_createButton.GetPopup().AboutToPopup += OnToolbarCreateMenuAboutToPopup;
		_createButton.GetPopup().IdPressed += OnToolbarCreateMenuIdPressed;
		_histPrevBtn.Pressed += NavigateHistoryPrevious;
		_histNextBtn.Pressed += NavigateHistoryNext;
		_toggleDisplayModeBtn.Pressed += ToggleDisplayMode;
		_fileListDisplayModeBtn.Pressed += ToggleFileListDisplayMode;
		_pathEdit.TextSubmitted += OnPathSubmitted;
		_searchEdit.TextChanged += OnSearchChanged;
		if (_listSearchEdit != null)
		{
			_listSearchEdit.TextChanged += OnListSearchChanged;
		}
		_tree.ItemSelected += OnTreeSelectionChanged;
		_tree.MultiSelected += OnTreeMultiSelected;
		_tree.ItemActivatedFile += OnFileActivated;
		_tree.ItemActivatedFolder += OnFolderActivated;
		_tree.FavoriteActivated += OnFavoriteActivated;
		_tree.InlineRenameSubmitted += OnTreeInlineRenameSubmitted;
		_list.ItemSelected += OnListItemSelected;
		_list.ItemActivatedFile += OnFileActivated;
		_list.ItemActivatedFolder += OnFolderActivated;
		_animationImportFileDialog.FileSelected += OnAnimationImportFileSelected;
		_animationImportFileDialog.Canceled += OnAnimationImportFileDialogCanceled;
		_newTextFileDialog.FileSelected += OnNewTextFileSelected;
		_moveToDirectoryDialog.DirSelected += OnMoveTargetDirectorySelected;
		_moveToDirectoryDialog.Canceled += delegate
		{
			_moveSourcePath = "";
		};
		if (!_splitDraggedConnected)
		{
			_splitBox.Dragged += OnSplitDragged;
			_splitDraggedConnected = true;
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/FileSystem/GUI/Dialog/XWDirectoryCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		_createDirDialog = packedScene.Instantiate<XWDirectoryCreateDialog>(PackedScene.GenEditState.Disabled);
		_createDirDialog.Name = "CreateDirDialog";
		AddChild(_createDirDialog, forceReadableName: false, InternalMode.Disabled);
		PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/FileSystem/GUI/Dialog/XWRemoveConfirmDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		_removeDialog = packedScene2.Instantiate<XWRemoveConfirmDialog>(PackedScene.GenEditState.Disabled);
		_removeDialog.Name = "RemoveDialog";
		AddChild(_removeDialog, forceReadableName: false, InternalMode.Disabled);
		SetupToolbarIcons();
		SetupSortMenus();
		UpdateDisplayMode();
		UpdateFileListDisplayButton();
		UpdateNavigationButtons();
		ConnectContextMenuSignals();
		InitFileSystem();
		ConnectExternalFileDropSignal();
	}

	public override void _ExitTree()
	{
		_componentAssemblyRefreshCancellation.Cancel();
		lock (_componentAssemblyResultGate)
		{
			_componentAssemblyResults.Clear();
		}
		Assembly activeAssembly = _componentAssemblyCatalog.ActiveAssembly;
		if ((object)activeAssembly != null && XWEditorInterface.Instance?.TryGetLoadedResourceEditor("character_component_editor") is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor)
		{
			xWCharacterComponentVisualResourceEditor.ReleaseComponentAssemblyReferences(activeAssembly);
		}
		_componentAssemblyCatalog.UnloadAsync();
		DisconnectExternalFileDropSignal();
		if (_fileSystemSignalsConnected && XWFileSystem.Instance != null)
		{
			XWFileSystem.Instance.ScanStarted -= OnScanStarted;
			XWFileSystem.Instance.ScanCompleted -= OnScanCompleted;
			XWFileSystem.Instance.ScanProgress -= OnScanProgress;
			_fileSystemSignalsConnected = false;
		}
		base._ExitTree();
	}

	private void SetupToolbarIcons()
	{
		XWClassRegistry instance = XWClassRegistry.Instance;
		Texture2D uIIcon = instance.GetUIIcon("Search");
		Texture2D uIIcon2 = instance.GetUIIcon("Sort");
		SetButtonIcon("%ButtonHistPrev", instance.GetUIIcon("Back"));
		SetButtonIcon("%ButtonHistNext", instance.GetUIIcon("Forward"));
		SetButtonIcon("%ButtonReload", instance.GetUIIcon("Reload"));
		SetMenuButtonIcon("%ButtonCreate", instance.GetUIIcon("Add"));
		SetButtonIcon("%ButtonFileListDisplayMode", instance.GetUIIcon("FileThumbnail"));
		SetButtonIcon("%PreviewHeader", instance.GetUIIcon("FileThumbnail"));
		SetMenuButtonIcon("%TreeButtonSort", uIIcon2);
		SetMenuButtonIcon("%FileListButtonSort", uIIcon2);
		LineEdit nodeOrNull = GetNodeOrNull<LineEdit>("%TreeSearchBox");
		if (nodeOrNull != null && GodotObject.IsInstanceValid(uIIcon))
		{
			nodeOrNull.RightIcon = uIIcon;
		}
	}

	private void SetupSortMenus()
	{
		SetupSortMenu(_treeSortButton);
		SetupSortMenu(_fileListSortButton);
	}

	private void SetupSortMenu(MenuButton button)
	{
		if (button != null)
		{
			PopupMenu popup = button.GetPopup();
			popup.Clear();
			popup.AddItem("名称升序", 0, Key.None);
			popup.AddItem("名称降序", 1, Key.None);
			popup.AddSeparator();
			popup.AddItem("类型升序", 2, Key.None);
			popup.AddItem("类型降序", 3, Key.None);
			popup.AddSeparator();
			popup.AddItem("修改时间升序", 4, Key.None);
			popup.AddItem("修改时间降序", 5, Key.None);
			popup.IdPressed += OnSortMenuItemPressed;
		}
	}

	private void SetButtonIcon(string path, Texture2D icon)
	{
		Button nodeOrNull = GetNodeOrNull<Button>(path);
		if (nodeOrNull != null && GodotObject.IsInstanceValid(icon))
		{
			nodeOrNull.Icon = icon;
		}
	}

	private void SetMenuButtonIcon(string path, Texture2D icon)
	{
		MenuButton nodeOrNull = GetNodeOrNull<MenuButton>(path);
		if (nodeOrNull != null && GodotObject.IsInstanceValid(icon))
		{
			nodeOrNull.Icon = icon;
		}
	}

	private void InitFileSystem()
	{
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		if (!_fileSystemSignalsConnected)
		{
			singleton.ScanStarted += OnScanStarted;
			singleton.ScanCompleted += OnScanCompleted;
			singleton.ScanProgress += OnScanProgress;
			_fileSystemSignalsConnected = true;
		}
	}

	private void ConnectExternalFileDropSignal()
	{
		Window window = GetWindow();
		if (window == null)
		{
			window = GetTree()?.Root;
		}
		if (window != null && (!_externalDropConnected || _externalDropWindow != window))
		{
			DisconnectExternalFileDropSignal();
			window.FilesDropped += OnExternalFilesDropped;
			_externalDropWindow = window;
			_externalDropConnected = true;
		}
	}

	private void DisconnectExternalFileDropSignal()
	{
		if (_externalDropConnected)
		{
			if (_externalDropWindow != null && GodotObject.IsInstanceValid(_externalDropWindow))
			{
				_externalDropWindow.FilesDropped -= OnExternalFilesDropped;
			}
			_externalDropWindow = null;
			_externalDropConnected = false;
		}
	}

	private void OnExternalFilesDropped(string[] files)
	{
		if (files != null && files.Length != 0 && IsVisibleInTree())
		{
			string text = GetDropTargetDirectoryAtMouse();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = GetSelectedDir();
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = XWFileSystem.GetSingleton().ProjectFolderPath;
			}
			text = NormalizeDirectoryPath(text);
			List<string> list = XWFileSystemDropHelper.ImportExternalFilesToBestDirectory(files, text);
			if (list.Count == 0)
			{
				XWEditorInterface.Instance?.ShowToast("没有可导入到当前目录的外部资源。");
				return;
			}
			XWFileSystem.GetSingleton().ScanChanges();
			string path = NormalizeDirectoryPath(list[0].GetBaseDir());
			NavigateTo(path, addHistory: false);
			_list.SelectPath(list[0]);
			XWEditorInterface.Instance?.ShowToast($"已导入 {list.Count} 个外部资源。");
		}
	}

	public string GetDropTargetDirectoryAtMouse()
	{
		Vector2 globalMousePosition = GetGlobalMousePosition();
		if (_list != null && _list.Visible && _list.GetGlobalRect().HasPoint(globalMousePosition))
		{
			return _list.GetDropTargetDirectoryAtGlobalPosition(globalMousePosition);
		}
		if (_tree != null && _tree.Visible && _tree.GetGlobalRect().HasPoint(globalMousePosition))
		{
			return _tree.GetDropTargetDirectoryAtGlobalPosition(globalMousePosition);
		}
		return NormalizeDirectoryPath(_currentDirPath);
	}

	private void OnScanStarted()
	{
		_scanningVBox.Visible = true;
		_scanningLabel.Text = "正在扫描文件，请稍候...";
	}

	private void OnScanProgress(int cur, int total)
	{
		_scanningLabel.Text = ((total > 0) ? $"扫描中... {cur}/{total}" : $"扫描中... 已发现 {cur} 项");
		if (total > 0)
		{
			_scanningProgress.MaxValue = total;
			_scanningProgress.Value = cur;
		}
	}

	private void OnScanCompleted()
	{
		_scanningVBox.Visible = false;
		NavigateTo(_currentDirPath, addHistory: false);
		if (_navigationHistory.Count == 0 && !string.IsNullOrEmpty(_currentDirPath))
		{
			PushNavigationHistory(_currentDirPath);
		}
	}

	private void OnReload()
	{
		XWFileSystem.GetSingleton().ScanChanges();
	}

	private void OnPathSubmitted(string path)
	{
		NavigateTo(path);
	}

	private void OnSearchChanged(string query)
	{
		_tree.Search(query);
		_list.Search(query);
	}

	private void OnListSearchChanged(string query)
	{
		_list.Search(query);
	}

	private void OnFolderActivated(string path)
	{
		NavigateTo(path);
	}

	private void OnSortMenuItemPressed(long id)
	{
		XWFileSystem.GetSingleton().SetSortMode((XWFileSystemEnum.SortMode)id);
		NavigateTo(_currentDirPath, addHistory: false);
	}

	private void ToggleDisplayMode()
	{
		_displayMode = _displayMode switch
		{
			XWFileSystemEnum.DisplayMode.VSplit => XWFileSystemEnum.DisplayMode.HSplit, 
			XWFileSystemEnum.DisplayMode.HSplit => XWFileSystemEnum.DisplayMode.TreeOnly, 
			_ => XWFileSystemEnum.DisplayMode.VSplit, 
		};
		UpdateDisplayMode();
	}

	private void UpdateDisplayMode()
	{
		if (_splitBox != null && _fileListVBox != null)
		{
			bool flag = _displayMode != XWFileSystemEnum.DisplayMode.TreeOnly;
			_splitBox.Vertical = _displayMode != XWFileSystemEnum.DisplayMode.HSplit;
			_splitBox.Collapsed = false;
			_splitBox.DraggingEnabled = flag;
			_splitBox.DraggerVisibility = (SplitContainer.DraggerVisibilityEnum)(flag ? 0 : 2);
			_tree.Visible = true;
			_tree.SizeFlagsVertical = SizeFlags.ExpandFill;
			if (_toolbar2HBox != null)
			{
				_toolbar2HBox.Visible = !flag;
			}
			_fileListVBox.Visible = flag;
			if (flag)
			{
				_splitBox.SplitOffsets = new int[1] { _splitBox.Vertical ? _splitBoxOffsetV : _splitBoxOffsetH };
				ScheduleSplitClamp();
			}
			Button toggleDisplayModeBtn = _toggleDisplayModeBtn;
			toggleDisplayModeBtn.Icon = _displayMode switch
			{
				XWFileSystemEnum.DisplayMode.TreeOnly => XWClassRegistry.Instance.GetUIIcon("Panels1"), 
				XWFileSystemEnum.DisplayMode.HSplit => XWClassRegistry.Instance.GetUIIcon("Panels2Alt"), 
				_ => XWClassRegistry.Instance.GetUIIcon("Panels2"), 
			};
			toggleDisplayModeBtn = _toggleDisplayModeBtn;
			toggleDisplayModeBtn.TooltipText = _displayMode switch
			{
				XWFileSystemEnum.DisplayMode.VSplit => "当前为上下分割，点击切换为左右分割", 
				XWFileSystemEnum.DisplayMode.HSplit => "当前为左右分割，点击切换为仅目录树", 
				_ => "当前仅显示目录树，点击显示文件列表", 
			};
		}
	}

	private async void ScheduleSplitClamp()
	{
		if (!_splitClampPending && _splitBox != null && GodotObject.IsInstanceValid(_splitBox))
		{
			_splitClampPending = true;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			_splitClampPending = false;
			if (_splitBox != null && GodotObject.IsInstanceValid(_splitBox) && _displayMode != XWFileSystemEnum.DisplayMode.TreeOnly && _splitBox.GetChildCount() >= 2)
			{
				_splitBox.ClampSplitOffset();
			}
		}
	}

	private void OnSplitDragged(long offset)
	{
		if (_splitBox != null)
		{
			if (_splitBox.Vertical)
			{
				_splitBoxOffsetV = (int)offset;
			}
			else
			{
				_splitBoxOffsetH = (int)offset;
			}
		}
	}

	private void ToggleFileListDisplayMode()
	{
		_fileListDisplayMode = ((_fileListDisplayMode != XWFileSystemEnum.FileListDisplayMode.List) ? XWFileSystemEnum.FileListDisplayMode.List : XWFileSystemEnum.FileListDisplayMode.Thumbnails);
		_list.SetDisplayMode(_fileListDisplayMode);
		UpdateFileListDisplayButton();
	}

	private void UpdateFileListDisplayButton()
	{
		_fileListDisplayModeBtn.TooltipText = ((_fileListDisplayMode == XWFileSystemEnum.FileListDisplayMode.List) ? "当前为列表视图，点击切换为缩略图" : "当前为缩略图，点击切换为列表视图");
	}

	private void NavigateHistoryPrevious()
	{
		if (_navigationIndex > 0)
		{
			_navigationIndex--;
			NavigateTo(_navigationHistory[_navigationIndex], addHistory: false);
			UpdateNavigationButtons();
		}
	}

	private void NavigateHistoryNext()
	{
		if (_navigationIndex >= 0 && _navigationIndex < _navigationHistory.Count - 1)
		{
			_navigationIndex++;
			NavigateTo(_navigationHistory[_navigationIndex], addHistory: false);
			UpdateNavigationButtons();
		}
	}

	private void PushNavigationHistory(string path)
	{
		if (_navigationIndex >= 0 && _navigationIndex < _navigationHistory.Count && _navigationHistory[_navigationIndex] == path)
		{
			UpdateNavigationButtons();
			return;
		}
		if (_navigationIndex < _navigationHistory.Count - 1)
		{
			_navigationHistory.RemoveRange(_navigationIndex + 1, _navigationHistory.Count - _navigationIndex - 1);
		}
		_navigationHistory.Add(path);
		_navigationIndex = _navigationHistory.Count - 1;
		UpdateNavigationButtons();
	}

	private void UpdateNavigationButtons()
	{
		if (_histPrevBtn != null)
		{
			_histPrevBtn.Disabled = _navigationIndex <= 0;
		}
		if (_histNextBtn != null)
		{
			_histNextBtn.Disabled = _navigationIndex < 0 || _navigationIndex >= _navigationHistory.Count - 1;
		}
	}

	private static string NormalizeDirectoryPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		path = path.Replace('\\', '/');
		if (!path.EndsWith("/"))
		{
			return path + "/";
		}
		return path;
	}

	private void OnFavoriteActivated(string path)
	{
		if (DirAccess.DirExistsAbsolute(path))
		{
			NavigateTo(path);
		}
		else
		{
			OnFileActivated(new XWFileSystemTreeItemData(path, Variant.From(in path), pIsFolder: false));
		}
	}

	private void OnListItemSelected(long index)
	{
		XWFileSystemTreeItemData data = _list.GetItemMetadata((int)index).As<XWFileSystemTreeItemData>();
		UpdatePreview(data);
		TryPreviewSelectedAnimation(data);
	}

	private void OnTreeSelectionChanged()
	{
		UpdateSelectedTreeItem(_tree.GetSelected());
	}

	private void OnTreeMultiSelected(TreeItem item, long column, bool selected)
	{
		if (selected)
		{
			UpdateSelectedTreeItem(item);
		}
		else if (_tree.GetNextSelected(null) == null)
		{
			UpdatePreview(null);
		}
	}

	private void UpdateSelectedTreeItem(TreeItem item)
	{
		if (item == null)
		{
			UpdatePreview(null);
			return;
		}
		if (_tree.IsFavoritesItem(item))
		{
			string from = _tree.GetFavoritePath(item);
			NavigateToSelectedTreePath(from);
			UpdatePreview(new XWFileSystemTreeItemData(from, Variant.From(in from), DirAccess.DirExistsAbsolute(from)));
			return;
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Nil)
		{
			UpdatePreview(null);
			return;
		}
		XWFileSystemTreeItemData xWFileSystemTreeItemData = metadata.As<XWFileSystemTreeItemData>();
		if (xWFileSystemTreeItemData != null && !xWFileSystemTreeItemData.IsSubResource)
		{
			NavigateToSelectedTreePath(xWFileSystemTreeItemData.Path);
		}
		UpdatePreview(xWFileSystemTreeItemData);
		TryPreviewSelectedAnimation(xWFileSystemTreeItemData);
	}

	private void NavigateToSelectedTreePath(string path)
	{
		string directoryForPath = GetDirectoryForPath(path);
		if (!string.IsNullOrWhiteSpace(directoryForPath) && DirAccess.DirExistsAbsolute(directoryForPath) && !string.Equals(directoryForPath, NormalizeDirectoryPath(_currentDirPath), StringComparison.Ordinal))
		{
			NavigateTo(directoryForPath);
		}
	}

	private void TryPreviewSelectedAnimation(XWFileSystemTreeItemData data)
	{
		int requestVersion = ++_animationInspectRequestVersion;
		if (data == null || data.IsFolder || data.IsSubResource || data.IsCompanionResource || !XWFileSystemCompanionResourcePolicy.IsAdobeAnimateResource(data.Path))
		{
			return;
		}
		string resourcePath = data.Path;
		SceneTreeTimer sceneTreeTimer = GetTree()?.CreateTimer(0.45, processAlways: true, processInPhysics: false, ignoreTimeScale: true);
		if (!GodotObject.IsInstanceValid(sceneTreeTimer))
		{
			return;
		}
		sceneTreeTimer.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid(this) && requestVersion == _animationInspectRequestVersion && GodotObject.IsInstanceValid(_animationFilePreview) && TresExtensionMethod.TryCreateAdobeAnimatePreview(resourcePath, out var animation))
			{
				_previewTextureBackground?.Hide();
				_animationFilePreview.Show();
				_animationFilePreview.EditAnimation(animation);
			}
		};
	}

	private void UpdatePreview(XWFileSystemTreeItemData data)
	{
		if (_previewTextureRect == null || _previewFileNameLabel == null)
		{
			return;
		}
		_animationFilePreview?.Hide();
		_previewTextureBackground?.Show();
		if (data == null || string.IsNullOrEmpty(data.Path))
		{
			_previewTextureRect.Texture = null;
			_previewFileNameLabel.Text = "未选择文件";
			return;
		}
		_previewFileNameLabel.Text = ((data.IsSubResource || data.IsCompanionResource) ? data.GetDisplayName() : (data.IsFolder ? data.Path.TrimSuffix("/").GetFile() : data.Path.GetFile()));
		Texture2D texture2D = null;
		if (!data.IsSubResource && !data.IsFolder && IsImageFile(data.Path))
		{
			texture2D = XWFileSystemExtensionRegistry.GetIcon(data, 128);
		}
		if (!XWTextureSafety.CanPreview(texture2D))
		{
			texture2D = null;
		}
		if (texture2D == null)
		{
			texture2D = (data.IsSubResource ? GetIcon("ResourcePreloader") : (data.IsFolder ? GetIcon("Folder") : XWFileSystemExtensionRegistry.GetIcon(data)));
		}
		_previewTextureRect.Texture = texture2D;
	}

	private static bool IsImageFile(string path)
	{
		switch (path.GetExtension().ToLowerInvariant())
		{
		case "png":
		case "jpg":
		case "jpeg":
		case "svg":
		case "webp":
			return true;
		default:
			return false;
		}
	}

	private void OnFileActivated(XWFileSystemTreeItemData data)
	{
		if (data == null)
		{
			return;
		}
		_animationInspectRequestVersion++;
		UpdatePreview(data);
		if (data.IsSubResource)
		{
			InspectSubResource(data);
			return;
		}
		if (data.IsCompanionResource)
		{
			OpenCompanionResource(data);
			return;
		}
		EmitSignal(SignalName.ImportDockRequested, data.Path);
		if (XWFileSystemExtensionRegistry.GetMethod(data) != null)
		{
			XWFileSystemExtensionRegistry.Execute(data);
		}
		else if (ResourceLoader.Exists(data.Path))
		{
			Resource resource = ResourceLoader.Load<Resource>(data.Path, null, ResourceLoader.CacheMode.Reuse);
			if (resource != null)
			{
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, data.Path, "resource_editor"));
			}
		}
	}

	private void OpenCompanionResource(XWFileSystemTreeItemData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.Path))
		{
			return;
		}
		EmitSignal(SignalName.ImportDockRequested, data.Path);
		if (XWFileSystemExtensionRegistry.GetMethod(data) != null)
		{
			XWFileSystemExtensionRegistry.Execute(data);
		}
		else if (ResourceLoader.Exists(data.Path))
		{
			Resource resource = ResourceLoader.Load<Resource>(data.Path, null, ResourceLoader.CacheMode.Reuse);
			if (resource != null)
			{
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, data.Path, "resource_editor"));
			}
		}
	}

	private void InspectSubResource(XWFileSystemTreeItemData data)
	{
		if (data != null && data.IsSubResource && data.Data.VariantType == Variant.Type.Object && data.Data.As<GodotObject>() is Resource res)
		{
			XWEditorInterface.Instance?.EditResource(res);
		}
	}

	private void NavigateTo(string path, bool addHistory = true)
	{
		path = NormalizeDirectoryPath(path);
		if (!string.IsNullOrEmpty(path))
		{
			_currentDirPath = path;
			_pathEdit.Text = path;
			XWFileSystemDirectory filesystemPath = XWFileSystem.GetSingleton().GetFilesystemPath(path);
			_list.DisplayDirectory(filesystemPath);
			if (addHistory)
			{
				PushNavigationHistory(path);
			}
			UpdatePreview(null);
		}
	}

	public void NavigateToProject(string projectDir)
	{
		if (!string.IsNullOrEmpty(projectDir))
		{
			projectDir = projectDir.Replace('\\', '/');
			if (!projectDir.EndsWith("/"))
			{
				projectDir += "/";
			}
			_currentDirPath = projectDir;
			_navigationHistory.Clear();
			_navigationIndex = -1;
			UpdateNavigationButtons();
			_tree.Init(projectDir);
		}
	}

	public void NavigateToPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		path = path.Replace('\\', '/');
		bool flag = path.EndsWith("/") || DirAccess.DirExistsAbsolute(path);
		string text = (flag ? path : path.GetBaseDir());
		if (!string.IsNullOrWhiteSpace(text))
		{
			NavigateTo(text);
			if (!flag)
			{
				_list.SelectPath(path);
			}
		}
	}

	private void RevealCreatedPath(string path)
	{
		path = path?.Replace('\\', '/') ?? "";
		if (!string.IsNullOrWhiteSpace(path))
		{
			bool flag = path.EndsWith("/") || DirAccess.DirExistsAbsolute(path);
			string text = (flag ? path : path.GetBaseDir());
			if (!string.IsNullOrWhiteSpace(text))
			{
				NavigateTo(text, addHistory: false);
			}
			XWFileSystemTreeItemData data = new XWFileSystemTreeItemData(path, Variant.From(in path), flag);
			if (!flag)
			{
				_list?.SelectPath(path);
			}
			UpdatePreview(data);
		}
	}

	private void OpenCreatedPath(string path)
	{
		path = path?.Replace('\\', '/') ?? "";
		if (!string.IsNullOrWhiteSpace(path))
		{
			RevealCreatedPath(path);
			if (!path.EndsWith("/") && !DirAccess.DirExistsAbsolute(path))
			{
				CallDeferred("OpenCreatedPathDeferred", path);
			}
		}
	}

	public void CompleteTemplateCreationFromTool(XWTemplateLibrary.TemplateCreateResult result)
	{
		if (result != null && result.Success)
		{
			if (string.Equals(result.TemplateId, "character-component", StringComparison.OrdinalIgnoreCase))
			{
				XWEditorInterface.Instance?.ShowToast("角色组件已创建，正在后台编译并刷新图鉴…");
				CompleteModernComponentCreationAsync(result);
			}
			else
			{
				OpenCreatedPath(result.CreatedPath);
			}
		}
	}

	private void OpenCreatedPathDeferred(string path)
	{
		if (IsInsideTree() && !string.IsNullOrWhiteSpace(path))
		{
			OnFileActivated(new XWFileSystemTreeItemData(path, Variant.From(in path), pIsFolder: false));
		}
	}

	public void ShowCreateDirDialog(string basePath)
	{
		_createDirDialog.PopupAt(basePath);
	}

	public void ShowRemoveDialog(string path)
	{
		_removeDialog.PopupFor(path);
	}

	private void ConnectContextMenuSignals()
	{
		_tree.ItemMouseSelected += OnTreeItemRmbSelected;
		_list.ItemClicked += OnListItemClicked;
		_treePopupMenu.IdPressed += OnTreeContextMenuIdPressed;
		_fileListPopupMenu.IdPressed += OnFileListContextMenuIdPressed;
	}

	private void OnTreeItemRmbSelected(Vector2 position, long mouseButtonIndex)
	{
		if (mouseButtonIndex != 2)
		{
			return;
		}
		_list.DeselectAll();
		TreeItem itemAtPosition = _tree.GetItemAtPosition(position);
		if (itemAtPosition == null)
		{
			_rmbSelectedPath = "";
			_rmbSelectedData = null;
			ShowEmptyTreePopupMenu();
			return;
		}
		if (_tree.IsFavoritesRoot(itemAtPosition))
		{
			_rmbSelectedPath = "";
			_rmbSelectedData = null;
			return;
		}
		if (_tree.IsFavoritesItem(itemAtPosition))
		{
			string favPath = (_rmbSelectedPath = _tree.GetFavoritePath(itemAtPosition));
			_rmbSelectedData = null;
			ShowFavoritePopupMenu(favPath);
			return;
		}
		XWFileSystemTreeItemData xWFileSystemTreeItemData = itemAtPosition.GetMetadata(0).As<XWFileSystemTreeItemData>();
		if (xWFileSystemTreeItemData == null)
		{
			_rmbSelectedPath = "";
			_rmbSelectedData = null;
			ShowEmptyTreePopupMenu();
		}
		else
		{
			_rmbSelectedData = xWFileSystemTreeItemData;
			_rmbSelectedPath = (xWFileSystemTreeItemData.IsSubResource ? "" : xWFileSystemTreeItemData.Path);
			ShowItemPopupMenu(xWFileSystemTreeItemData, _treePopupMenu);
		}
	}

	private void OnListItemClicked(long index, Vector2 position, long mouseButtonIndex)
	{
		if (mouseButtonIndex == 2)
		{
			_tree.DeselectAll();
			_list.Select((int)index, single: false);
			XWFileSystemTreeItemData xWFileSystemTreeItemData = _list.GetItemMetadata((int)index).As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null)
			{
				_rmbSelectedPath = xWFileSystemTreeItemData.Path;
				_rmbSelectedData = xWFileSystemTreeItemData;
				UpdatePreview(xWFileSystemTreeItemData);
				ShowItemPopupMenu(xWFileSystemTreeItemData, _fileListPopupMenu);
			}
		}
	}

	private void ShowEmptyTreePopupMenu()
	{
		PopupMenu treePopupMenu = _treePopupMenu;
		treePopupMenu.Clear();
		ClearCreateActionIds();
		treePopupMenu.AddIconItem(GetIcon("Folder"), "新建文件夹...", 9, Key.None);
		AddDirectoryCreateItems(treePopupMenu, GetContextTargetDir());
		treePopupMenu.AddIconItem(GetIcon("TextFile"), "新建文本文件...", 20, Key.None);
		treePopupMenu.AddSeparator();
		treePopupMenu.AddIconItem(GetIcon("Terminal"), "在终端中打开", 21, Key.None);
		treePopupMenu.AddIconItem(GetIcon("Filesystem"), "在文件管理器中显示", 16, Key.None);
		treePopupMenu.AddSeparator();
		treePopupMenu.AddItem("刷新", 17, Key.None);
		PopupMenuAt(treePopupMenu);
	}

	private void ShowFavoritePopupMenu(string favPath)
	{
		PopupMenu treePopupMenu = _treePopupMenu;
		treePopupMenu.Clear();
		treePopupMenu.AddItem("打开", 0, Key.None);
		treePopupMenu.AddIconItem(GetIcon("NonFavorite"), "从收藏中移除", 4, Key.None);
		treePopupMenu.AddSeparator();
		treePopupMenu.AddIconItem(GetIcon("Filesystem"), "在文件管理器中显示", 16, Key.None);
		treePopupMenu.AddItem("复制路径", 13, Key.None);
		PopupMenuAt(treePopupMenu);
	}

	private void ShowItemPopupMenu(XWFileSystemTreeItemData data, PopupMenu popup)
	{
		popup.Clear();
		ClearCreateActionIds();
		if (data.IsSubResource)
		{
			popup.AddIconItem(GetIcon("ResourcePreloader"), "打开子资源", 0, Key.None);
			popup.AddIconItem(GetIcon("ActionCopy"), "复制子资源路径", 15, Key.None);
			PopupMenuAt(popup);
			return;
		}
		List<string> selectedPaths = GetSelectedPaths();
		bool flag = selectedPaths.Count > 0;
		bool flag2 = selectedPaths.Count > 0;
		foreach (string item in selectedPaths)
		{
			if (XWFileSystem.GetSingleton().IsFavorite(item))
			{
				flag2 = false;
			}
			else
			{
				flag = false;
			}
		}
		if (data.IsFolder)
		{
			AddDirectoryCreateItems(popup, data.Path);
			popup.AddIconItem(GetIcon("Folder"), "新建文件夹...", 9, Key.None);
			popup.AddIconItem(GetIcon("TextFile"), "新建文本文件...", 20, Key.None);
			popup.AddSeparator();
			if (!flag)
			{
				popup.AddIconItem(GetIcon("Favorites"), "添加到收藏", 3, Key.None);
			}
			if (!flag2)
			{
				popup.AddIconItem(GetIcon("NonFavorite"), "从收藏中移除", 4, Key.None);
			}
			popup.AddSeparator();
			popup.AddIconItem(GetIcon("Filesystem"), "在文件管理器中显示", 16, Key.None);
			popup.AddIconItem(GetIcon("Terminal"), "在终端中打开", 21, Key.None);
			popup.AddItem("复制路径", 13, Key.None);
			if (!IsProtectedDirectory(data.Path))
			{
				popup.AddSeparator();
				popup.AddIconItem(GetIcon("Rename"), "重命名...", 6, Key.None);
				popup.AddIconItem(GetIcon("Remove"), "删除", 7, Key.None);
			}
		}
		else
		{
			string value = data.Path.GetExtension().ToLower();
			if (Array.IndexOf(SceneExtensions, value) >= 0)
			{
				popup.AddIconItem(GetIcon("Load"), "打开场景", 22, Key.None);
				popup.AddIconItem(GetIcon("Instance"), "实例化", 25, Key.None);
				popup.AddSeparator();
			}
			AddSubResourceCreateItems(popup, data.Path);
			popup.AddIconItem(GetIcon("Load"), "打开", 0, Key.None);
			popup.AddSeparator();
			if (!flag)
			{
				popup.AddIconItem(GetIcon("Favorites"), "添加到收藏", 3, Key.None);
			}
			if (!flag2)
			{
				popup.AddIconItem(GetIcon("NonFavorite"), "从收藏中移除", 4, Key.None);
			}
			popup.AddSeparator();
			popup.AddIconItem(GetIcon("Filesystem"), "在文件管理器中显示", 16, Key.None);
			popup.AddIconItem(GetIcon("ActionCopy"), "复制路径", 13, Key.None);
			popup.AddIconItem(GetIcon("ActionCopy"), "复制绝对路径", 14, Key.None);
			popup.AddIconItem(GetIcon("Instance"), "复制资源路径", 15, Key.None);
			popup.AddSeparator();
			popup.AddIconItem(GetIcon("Rename"), "重命名...", 6, Key.None);
			popup.AddIconItem(GetIcon("MoveUp"), "移动到...", 5, Key.None);
			popup.AddIconItem(GetIcon("Remove"), "删除", 7, Key.None);
			popup.AddSeparator();
			popup.AddIconItem(GetIcon("ActionCopy"), "复制", 18, Key.None);
			popup.AddIconItem(GetIcon("ActionCopy"), "粘贴", 19, Key.None);
			popup.AddIconItem(GetIcon("Duplicate"), "重复", 8, Key.None);
		}
		PopupMenuAt(popup);
	}

	private void AddDirectoryCreateItems(PopupMenu popup, string directoryPath)
	{
		bool flag = false;
		if (CanCreateSceneInDirectory(directoryPath))
		{
			popup.AddIconItem(GetIcon("PackedScene"), "新建场景...", 11, Key.None);
			flag = true;
		}
		if (CanCreateScriptInDirectory(directoryPath))
		{
			popup.AddIconItem(GetIcon("ScriptCreate"), GetLogicCreateMenuTitle(directoryPath), 10, Key.None);
			flag = true;
		}
		foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directoryPath, XWFileSystem.GetSingleton().ProjectFolderPath))
		{
			int num = 10000 + _resourceCreateActionIds.Count;
			_resourceCreateActionIds[num] = item.Id;
			popup.AddIconItem(GetIcon(item.IconName), item.DisplayName + "...", num, Key.None);
			flag = true;
		}
		if (CanCreateAdvancedResourceInDirectory(directoryPath))
		{
			popup.AddIconItem(GetIcon("ResourcePreloader"), "新建其他资源...", 12000, Key.None);
			flag = true;
		}
		if (flag)
		{
			popup.AddSeparator();
		}
	}

	private void OnToolbarCreateMenuAboutToPopup()
	{
		PopupMenu popupMenu = _createButton?.GetPopup();
		if (GodotObject.IsInstanceValid(popupMenu))
		{
			_toolbarCreateTargetDir = NormalizeDirectoryPath(_currentDirPath);
			if (string.IsNullOrWhiteSpace(_toolbarCreateTargetDir))
			{
				_toolbarCreateTargetDir = NormalizeDirectoryPath(GetSelectedDir());
			}
			if (string.IsNullOrWhiteSpace(_toolbarCreateTargetDir))
			{
				_toolbarCreateTargetDir = NormalizeDirectoryPath(XWFileSystem.GetSingleton().ProjectFolderPath);
			}
			popupMenu.Clear();
			ClearCreateActionIds();
			AddDirectoryCreateItems(popupMenu, _toolbarCreateTargetDir);
			popupMenu.AddIconItem(GetIcon("Folder"), "新建文件夹...", 9, Key.None);
			popupMenu.AddIconItem(GetIcon("TextFile"), "新建文本文件...", 20, Key.None);
		}
	}

	private void OnToolbarCreateMenuIdPressed(long id)
	{
		HandlePopupMenuId(id, _toolbarCreateTargetDir);
	}

	private void AddSubResourceCreateItems(PopupMenu popup, string ownerPath)
	{
		IReadOnlyList<XWResourceCreateRoute.CreateAction> subResourceActionsForOwner = XWResourceCreateRoute.GetSubResourceActionsForOwner(ownerPath, XWFileSystem.GetSingleton().ProjectFolderPath);
		if (subResourceActionsForOwner.Count == 0)
		{
			return;
		}
		foreach (XWResourceCreateRoute.CreateAction item in subResourceActionsForOwner)
		{
			int num = 11000 + _subResourceCreateActionIds.Count;
			_subResourceCreateActionIds[num] = item.Id;
			popup.AddIconItem(GetIcon(item.IconName), item.DisplayName + "...", num, Key.None);
		}
		popup.AddSeparator();
	}

	private static bool CanCreateSceneInDirectory(string directoryPath)
	{
		return XWModProjectLayout.IsInsideDirectory(directoryPath, XWFileSystem.GetSingleton().ProjectFolderPath, "Scenes");
	}

	private static bool CanCreateAdvancedResourceInDirectory(string directoryPath)
	{
		string text = XWModProjectLayout.ToProjectRelativePath(directoryPath, XWFileSystem.GetSingleton().ProjectFolderPath);
		if (!(text == "Resources"))
		{
			return text.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool CanCreateBlueprintInDirectory(string directoryPath)
	{
		return CanCreateScriptInDirectory(directoryPath);
	}

	private static bool CanCreateScriptInDirectory(string directoryPath)
	{
		string text = XWModProjectLayout.ToProjectRelativePath(directoryPath, XWFileSystem.GetSingleton().ProjectFolderPath);
		switch (text)
		{
		default:
			return text == "Battle/Components";
		case "Scripts":
		case "Battle/Features":
		case "Battle/Processes":
			return true;
		}
	}

	private static string GetScriptTemplateIdForDirectory(string directoryPath)
	{
		string projectFolderPath = XWFileSystem.GetSingleton().ProjectFolderPath;
		if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Features"))
		{
			return "feature-csharp";
		}
		if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Processes"))
		{
			return "process-csharp";
		}
		if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Components"))
		{
			return "character-component";
		}
		return "shared-csharp";
	}

	private static string GetLogicCreateMenuTitle(string directoryPath)
	{
		string projectFolderPath = XWFileSystem.GetSingleton().ProjectFolderPath;
		if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Components"))
		{
			return "新建组件逻辑...";
		}
		return "新建 C# / 蓝图逻辑...";
	}

	private static List<(string TemplateId, string DisplayName)> BuildScriptTemplateOptionsForDirectory(string directoryPath, XWTemplateLibrary templateLibrary)
	{
		List<(string TemplateId, string DisplayName)> options = new List<(string, string)>();
		string projectFolderPath = XWFileSystem.GetSingleton().ProjectFolderPath;
		if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Features"))
		{
			Add("feature-csharp");
		}
		else if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Processes"))
		{
			Add("process-csharp");
		}
		else if (XWModProjectLayout.IsInsideDirectory(directoryPath, projectFolderPath, "Battle/Components"))
		{
			Add("character-component");
		}
		else
		{
			Add("shared-csharp");
			Add("debug-entry-csharp");
			Add("character-event-csharp");
			Add("shovel-event-csharp");
			Add("card-event-csharp");
			Add("character-csharp");
			Add("projectile-csharp");
		}
		if (options.Count == 0)
		{
			Add(GetScriptTemplateIdForDirectory(directoryPath));
		}
		return options;
		void Add(string templateId)
		{
			XWTemplateLibrary.TemplateInfo templateInfo = templateLibrary.FindTemplate(templateId);
			if (templateInfo != null)
			{
				options.Add((templateId, XWTemplatePresentation.Resolve(templateInfo).DisplayNameLabel));
			}
		}
	}

	private static bool IsProtectedDirectory(string path)
	{
		return XWModProjectLayout.IsProtectedDirectory(path, XWFileSystem.GetSingleton().ProjectFolderPath);
	}

	private void ClearCreateActionIds()
	{
		_resourceCreateActionIds.Clear();
		_subResourceCreateActionIds.Clear();
	}

	private void PopupMenuAt(PopupMenu menu)
	{
		menu.Position = DisplayServer.MouseGetPosition();
		menu.ResetSize();
		menu.Popup();
	}

	private static void RegisterCreatedFileInManifest(string filePath, string section)
	{
		if (!string.IsNullOrWhiteSpace(filePath))
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPath(text, filePath);
			}
		}
	}

	private static void RegisterCreatedFilesInManifest(XWTemplateLibrary.TemplateCreateResult result, string defaultSection)
	{
		if (result == null)
		{
			return;
		}
		if (result.CreatedPaths != null && result.CreatedPaths.Count > 0)
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPaths(text, result.CreatedPaths);
			}
		}
		else
		{
			RegisterCreatedFileInManifest(result.CreatedPath, GetManifestSectionForPath(result.CreatedPath, defaultSection));
		}
	}

	private static string GetManifestSectionForTemplate(string templateId)
	{
		switch (templateId)
		{
		case "blueprint":
			return "Blueprints";
		case "feature-csharp":
		case "process-csharp":
		case "character-component":
		case "shovel-event-csharp":
		case "card-event-csharp":
		case "projectile-csharp":
		case "character-csharp":
		case "shared-csharp":
		case "debug-entry-csharp":
		case "character-event-csharp":
			return "Scripts";
		default:
			return "Resources";
		}
	}

	private static string GetManifestSectionForPath(string path, string defaultSection)
	{
		if (path.GetExtension().ToLowerInvariant() == "cs")
		{
			return "Scripts";
		}
		return defaultSection;
	}

	private Texture2D GetIcon(string iconName)
	{
		return XWClassRegistry.Instance.GetUIIcon(iconName);
	}

	private void OnTreeContextMenuIdPressed(long id)
	{
		HandlePopupMenuId(id);
	}

	private void OnFileListContextMenuIdPressed(long id)
	{
		HandlePopupMenuId(id);
	}

	private void HandlePopupMenuId(long id, string targetDirectoryOverride = "")
	{
		if (id == 12000)
		{
			ShowAdvancedResourceTypeDialog(ResolveTargetDirectory());
			return;
		}
		if (_resourceCreateActionIds.TryGetValue((int)id, out var value))
		{
			string text = ResolveTargetDirectory();
			if (value == "new-animation")
			{
				XWAdobeAnimateTrace.Write("UI context action selected: action=" + value + ", target=" + text);
			}
			ShowResourceCreateDialog(value, text);
			return;
		}
		if (_subResourceCreateActionIds.TryGetValue((int)id, out var value2))
		{
			ShowSubResourceCreateDialog(value2, _rmbSelectedPath);
			return;
		}
		List<string> selectedPaths = GetSelectedPaths();
		string from = ((selectedPaths.Count > 0) ? selectedPaths[0] : "");
		if (string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(_rmbSelectedPath))
		{
			from = _rmbSelectedPath;
		}
		switch ((XWFileSystemEnum.FileMenu)id)
		{
		case XWFileSystemEnum.FileMenu.Open:
			if (_rmbSelectedData != null && _rmbSelectedData.IsSubResource)
			{
				OnFileActivated(_rmbSelectedData);
			}
			else if (!string.IsNullOrEmpty(from))
			{
				if (DirAccess.DirExistsAbsolute(from))
				{
					NavigateTo(from.EndsWith("/") ? from : (from + "/"));
				}
				else
				{
					OnFileActivated(new XWFileSystemTreeItemData(from, Variant.From(in from), pIsFolder: false));
				}
			}
			break;
		case XWFileSystemEnum.FileMenu.NewFolder:
			ShowCreateDirDialog(ResolveTargetDirectory());
			break;
		case XWFileSystemEnum.FileMenu.NewScript:
			ShowNewScriptDialog(ResolveTargetDirectory());
			break;
		case XWFileSystemEnum.FileMenu.NewScene:
			CreateNewScene(ResolveTargetDirectory());
			break;
		case XWFileSystemEnum.FileMenu.NewBlueprint:
			CreateNewBlueprint(ResolveTargetDirectory());
			break;
		case XWFileSystemEnum.FileMenu.NewTextfile:
			CreateNewTextFile(ResolveTargetDirectory());
			break;
		case XWFileSystemEnum.FileMenu.CopyPath:
			if (!string.IsNullOrEmpty(from))
			{
				DisplayServer.ClipboardSet(from);
				XWEditorInterface.Instance?.ShowToast("已复制路径: " + from);
			}
			break;
		case XWFileSystemEnum.FileMenu.CopyAbsolutePath:
			if (!string.IsNullOrEmpty(from))
			{
				DisplayServer.ClipboardSet(ProjectSettings.GlobalizePath(from));
				XWEditorInterface.Instance?.ShowToast("已复制绝对路径");
			}
			break;
		case XWFileSystemEnum.FileMenu.CopyUid:
			if (_rmbSelectedData != null && _rmbSelectedData.IsSubResource)
			{
				DisplayServer.ClipboardSet(_rmbSelectedData.SubResourcePropertyPath);
			}
			else if (!string.IsNullOrEmpty(from))
			{
				string text2 = ReadResourceUid(from);
				DisplayServer.ClipboardSet(string.IsNullOrEmpty(text2) ? from : text2);
				XWEditorInterface.Instance?.ShowToast("已复制资源路径");
			}
			break;
		case XWFileSystemEnum.FileMenu.Rename:
			if (!string.IsNullOrEmpty(from))
			{
				BeginTreeInlineRename(from);
			}
			break;
		case XWFileSystemEnum.FileMenu.Move:
			MoveTo();
			break;
		case XWFileSystemEnum.FileMenu.Duplicate:
			DuplicateSelected();
			break;
		case XWFileSystemEnum.FileMenu.AddFavorite:
			AddFavoriteOp(selectedPaths);
			break;
		case XWFileSystemEnum.FileMenu.RemoveFavorite:
			RemoveFavoriteOp(selectedPaths);
			break;
		case XWFileSystemEnum.FileMenu.Remove:
			DeleteSelected(selectedPaths);
			break;
		case XWFileSystemEnum.FileMenu.ShowInExplorer:
			if (!string.IsNullOrEmpty(from))
			{
				ShellOpenInFileManager(from);
			}
			else if (!string.IsNullOrEmpty(_currentDirPath))
			{
				ShellOpenInFileManager(_currentDirPath);
			}
			break;
		case XWFileSystemEnum.FileMenu.OpenInTerminal:
			OpenInTerminal(GetContextTargetDir());
			break;
		case XWFileSystemEnum.FileMenu.Reimport:
			XWFileSystem.GetSingleton().ScanChanges();
			break;
		case XWFileSystemEnum.FileMenu.Copy:
			CopyResource(from);
			break;
		case XWFileSystemEnum.FileMenu.Paste:
			PasteResource();
			break;
		case XWFileSystemEnum.FileMenu.OpenScene:
			if (!string.IsNullOrEmpty(from))
			{
				OnFileActivated(new XWFileSystemTreeItemData(from, Variant.From(in from), pIsFolder: false));
			}
			break;
		case XWFileSystemEnum.FileMenu.InstantiateScene:
			if (!string.IsNullOrEmpty(from))
			{
				InstantiateScene(from);
			}
			break;
		case XWFileSystemEnum.FileMenu.Inherit:
		case XWFileSystemEnum.FileMenu.Instance:
		case XWFileSystemEnum.FileMenu.InheritScene:
		case XWFileSystemEnum.FileMenu.SetMainScene:
			break;
		}
		string ResolveTargetDirectory()
		{
			if (!string.IsNullOrWhiteSpace(targetDirectoryOverride))
			{
				return NormalizeDirectoryPath(targetDirectoryOverride);
			}
			return GetContextTargetDir();
		}
	}

	private void ShowAdvancedResourceTypeDialog(string baseDir)
	{
		baseDir = NormalizeDirectoryPath(baseDir);
		if (!CanCreateAdvancedResourceInDirectory(baseDir))
		{
			XWEditorInterface.Instance?.ShowToast("高级资源只能创建在 Resources 目录中。");
			return;
		}
		if (_resourceTypeCreateDialogScene == null)
		{
			_resourceTypeCreateDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		XWCreateDialog dialog = _resourceTypeCreateDialogScene?.Instantiate<XWCreateDialog>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(dialog))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载资源类型浏览器。");
			return;
		}
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		dialog.ConfigureForResources();
		dialog.Created += delegate(string typeName)
		{
			dialog.QueueFree();
			ShowAdvancedResourceNameDialog(typeName, baseDir);
		};
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCenteredClamped(new Vector2I(920, 650), 0.9f);
	}

	private void ShowAdvancedResourceNameDialog(string typeName, string baseDir)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return;
		}
		LineEdit nameEdit;
		ConfirmationDialog dialog = CreateNameInputDialog("新建 " + typeName, "输入资源文件名", "New" + typeName, out nameEdit);
		bool creating;
		if (GodotObject.IsInstanceValid(dialog))
		{
			creating = false;
			dialog.Confirmed += Create;
			dialog.Canceled += Cleanup;
			nameEdit.TextSubmitted += delegate
			{
				Create();
			};
			dialog.PopupCentered();
			nameEdit.GrabFocus();
			nameEdit.SelectAll();
		}
		void Cleanup()
		{
			dialog.QueueFree();
		}
		void Create()
		{
			if (!creating)
			{
				creating = true;
				string text = XWTemplateLibrary.SanitizeName(nameEdit.Text);
				string createdPath;
				string error;
				if (string.IsNullOrWhiteSpace(text))
				{
					Cleanup();
				}
				else if (!TryCreateAdvancedResource(typeName, baseDir, text, out createdPath, out error))
				{
					XWEditorInterface.Instance?.ShowToast("创建失败: " + error);
					creating = false;
				}
				else
				{
					RegisterCreatedFileInManifest(createdPath, "Resources");
					XWFileSystem.GetSingleton().ScanChanges();
					XWEditorInterface.Instance?.ShowToast("已创建 " + typeName + ": " + createdPath);
					OpenCreatedPath(createdPath);
					Cleanup();
				}
			}
		}
	}

	private static bool TryCreateAdvancedResource(string typeName, string baseDir, string safeName, out string createdPath, out string error)
	{
		createdPath = "";
		error = "";
		XWClassRegistry instance = XWClassRegistry.Instance;
		if (!instance.HasClass(typeName) || !instance.IsParentClass(typeName, "Resource"))
		{
			error = typeName + " 不是可创建的 Resource 类型";
			return false;
		}
		XWClassData classData = instance.GetClassData(typeName);
		if (classData == null || !classData.IsGlobalClass || !GodotObject.IsInstanceValid(classData.ScriptFile))
		{
			error = typeName + " 没有可写入的全局类脚本";
			return false;
		}
		try
		{
			Variant variant = instance.Instantiate(typeName);
			if (variant.VariantType != Variant.Type.Object || !(variant.AsGodotObject() is Resource resource))
			{
				error = typeName + " 无法实例化，可能是抽象资源类型";
				return false;
			}
			resource.Free();
			string resourcePath = classData.ScriptFile.ResourcePath;
			if (string.IsNullOrWhiteSpace(resourcePath))
			{
				error = typeName + " 的脚本路径为空";
				return false;
			}
			createdPath = Path.Combine(baseDir, safeName + ".tres").Replace('\\', '/');
			if (File.Exists(createdPath))
			{
				error = "同名资源已经存在";
				return false;
			}
			Directory.CreateDirectory(baseDir);
			string value = safeName.Replace("\\", "\\\\").Replace("\"", "\\\"");
			string value2 = resourcePath.Replace("\\", "/").Replace("\"", "\\\"");
			string contents = $"[gd_resource type=\"Resource\" script_class=\"{typeName}\" format=3]\n\n[ext_resource type=\"Script\" path=\"{value2}\" id=\"1_script\"]\n\n" + "[resource]\nscript = ExtResource(\"1_script\")\n" + $"resource_name = \"{value}\"\nmetadata/mod_display_name = \"{value}\"\n";
			File.WriteAllText(createdPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private List<string> GetSelectedPaths()
	{
		List<string> list = new List<string>();
		if (_list != null && _list.Visible)
		{
			int[] selectedItems = _list.GetSelectedItems();
			foreach (int idx in selectedItems)
			{
				XWFileSystemTreeItemData xWFileSystemTreeItemData = _list.GetItemMetadata(idx).As<XWFileSystemTreeItemData>();
				if (xWFileSystemTreeItemData != null && !xWFileSystemTreeItemData.IsSubResource)
				{
					list.Add(xWFileSystemTreeItemData.Path);
				}
			}
			return list;
		}
		for (TreeItem nextSelected = _tree.GetNextSelected(null); nextSelected != null; nextSelected = _tree.GetNextSelected(nextSelected))
		{
			if (_tree.IsFavoritesItem(nextSelected))
			{
				string favoritePath = _tree.GetFavoritePath(nextSelected);
				if (!string.IsNullOrEmpty(favoritePath))
				{
					list.Add(favoritePath);
				}
			}
			else if (!_tree.IsFavoritesRoot(nextSelected))
			{
				XWFileSystemTreeItemData xWFileSystemTreeItemData2 = nextSelected.GetMetadata(0).As<XWFileSystemTreeItemData>();
				if (xWFileSystemTreeItemData2 != null && !xWFileSystemTreeItemData2.IsSubResource)
				{
					list.Add(xWFileSystemTreeItemData2.Path);
				}
			}
		}
		return list;
	}

	private string GetSelectedDir()
	{
		List<string> selectedPaths = GetSelectedPaths();
		if (selectedPaths.Count == 0)
		{
			if (!string.IsNullOrEmpty(_currentDirPath))
			{
				return _currentDirPath;
			}
			return XWFileSystem.GetSingleton().ProjectFolderPath;
		}
		string text = selectedPaths[0];
		if (DirAccess.DirExistsAbsolute(text))
		{
			if (!text.EndsWith("/"))
			{
				return text + "/";
			}
			return text;
		}
		return text.GetBaseDir() + "/";
	}

	private string GetContextTargetDir()
	{
		if (!string.IsNullOrEmpty(_rmbSelectedPath))
		{
			return GetDirectoryForPath(_rmbSelectedPath);
		}
		return NormalizeDirectoryPath(string.IsNullOrEmpty(_currentDirPath) ? XWFileSystem.GetSingleton().ProjectFolderPath : _currentDirPath);
	}

	private static string GetDirectoryForPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		path = path.Replace('\\', '/');
		if (path.EndsWith("/") || DirAccess.DirExistsAbsolute(path))
		{
			return NormalizeDirectoryPath(path);
		}
		return NormalizeDirectoryPath(path.GetBaseDir());
	}

	private static string GetTreeItemPath(TreeItem item)
	{
		if (item == null)
		{
			return "";
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Nil)
		{
			return "";
		}
		XWFileSystemTreeItemData xWFileSystemTreeItemData = metadata.As<XWFileSystemTreeItemData>();
		if (xWFileSystemTreeItemData == null)
		{
			return metadata.AsString();
		}
		return xWFileSystemTreeItemData.Path;
	}

	private static bool IsTreeItemFolder(TreeItem item)
	{
		if (item == null)
		{
			return false;
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Nil)
		{
			return false;
		}
		return metadata.As<XWFileSystemTreeItemData>() == null;
	}

	private void ShellOpenInFileManager(string path)
	{
		string text = ProjectSettings.GlobalizePath(path);
		if (DirAccess.DirExistsAbsolute(path))
		{
			OS.ShellOpen(text);
		}
		else
		{
			OS.ShellOpen(text.GetBaseDir());
		}
	}

	private void OpenInTerminal(string selectedPath = "")
	{
		if (string.IsNullOrEmpty(selectedPath))
		{
			selectedPath = GetSelectedDir();
		}
		if (string.IsNullOrEmpty(selectedPath))
		{
			selectedPath = XWFileSystem.GetSingleton().ProjectFolderPath;
		}
		selectedPath = NormalizeDirectoryPath(selectedPath);
		OS.ShellOpen(ProjectSettings.GlobalizePath(selectedPath));
	}

	private void CreateNewScene(string baseDir = "")
	{
		if (string.IsNullOrEmpty(baseDir))
		{
			baseDir = GetSelectedDir();
		}
		baseDir = NormalizeDirectoryPath(baseDir);
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		string text = singleton.GenerateUniqueFileName(baseDir, "NewScene.tscn");
		string text2 = baseDir + text;
		string text3 = "[gd_scene format=3]\n\n[node name=\"Node2D\" type=\"Node2D\"]\n";
		Godot.FileAccess fileAccess = Godot.FileAccess.Open(text2, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法创建场景文件: " + text2);
			return;
		}
		fileAccess.StoreString(text3);
		fileAccess.Close();
		RegisterCreatedFileInManifest(text2, "Resources");
		singleton.ScanChanges();
		XWEditorInterface.Instance?.ShowToast("已创建: " + text2);
		OpenCreatedPath(text2);
	}

	private void CreateNewBlueprint(string baseDir)
	{
		if (string.IsNullOrEmpty(baseDir))
		{
			baseDir = XWFileSystem.GetSingleton().ProjectFolderPath;
		}
		if (string.IsNullOrEmpty(baseDir))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开或创建 Mod 工程");
			return;
		}
		if (!CanCreateBlueprintInDirectory(baseDir))
		{
			XWEditorInterface.Instance?.ShowToast("蓝图只能创建在 Scripts 子目录中。");
			return;
		}
		baseDir = baseDir.Replace('\\', '/');
		if (!baseDir.EndsWith("/"))
		{
			baseDir += "/";
		}
		string text = XWFileSystem.GetSingleton().GenerateUniqueFileName(baseDir, "new_blueprint.tres");
		string path = baseDir + text;
		XWBlueprintCreateDialog xWBlueprintCreateDialog = XWBlueprintCreateDialog.Create();
		xWBlueprintCreateDialog.Config("Node", path);
		xWBlueprintCreateDialog.BlueprintCreated += OnBlueprintCreated;
		AddChild(xWBlueprintCreateDialog, forceReadableName: false, InternalMode.Disabled);
		xWBlueprintCreateDialog.PopupCentered();
	}

	private void OnBlueprintCreated(string filePath)
	{
		RegisterCreatedFileInManifest(filePath, "Blueprints");
		XWFileSystem.GetSingleton().ScanChanges();
		XWEditorInterface.Instance?.ShowToast("已创建蓝图: " + filePath);
		OpenCreatedPath(filePath);
	}

	private void CreateNewTextFile(string baseDir = "")
	{
		if (string.IsNullOrEmpty(baseDir))
		{
			baseDir = GetSelectedDir();
		}
		if (string.IsNullOrEmpty(baseDir))
		{
			baseDir = XWFileSystem.GetSingleton().ProjectFolderPath;
		}
		baseDir = NormalizeDirectoryPath(baseDir);
		_newTextFileDialog.CurrentDir = baseDir;
		_newTextFileDialog.CurrentFile = "new_file.txt";
		_newTextFileDialog.PopupCentered();
	}

	private void OnNewTextFileSelected(string path)
	{
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法创建文本文件: " + path);
			return;
		}
		fileAccess.StoreString("");
		fileAccess.Close();
		XWFileSystem.GetSingleton().ScanChanges();
		XWEditorInterface.Instance?.ShowToast("已创建文本文件: " + path);
		OpenCreatedPath(path);
	}

	private void ShowResourceCreateDialog(string actionId, string baseDir)
	{
		baseDir = NormalizeDirectoryPath(baseDir);
		IReadOnlyList<XWResourceCreateRoute.CreateAction> actionsForDirectory = XWResourceCreateRoute.GetActionsForDirectory(baseDir, XWFileSystem.GetSingleton().ProjectFolderPath);
		XWResourceCreateRoute.CreateAction createAction = null;
		foreach (XWResourceCreateRoute.CreateAction item in actionsForDirectory)
		{
			if (item.Id == actionId)
			{
				createAction = item;
				break;
			}
		}
		if (createAction == null)
		{
			XWEditorInterface.Instance?.ShowToast("当前目录不能创建该资源。");
			return;
		}
		if (actionId == "new-animation")
		{
			XWAdobeAnimateTrace.Write("UI resource create action: action=" + actionId + ", baseDir=" + baseDir);
			ShowAnimationImportDialog(baseDir);
			return;
		}
		LineEdit nameEdit;
		ConfirmationDialog dialog = CreateNameInputDialog(createAction.DisplayName, "输入资源名称", createAction.DefaultName, out nameEdit);
		bool creating;
		if (GodotObject.IsInstanceValid(dialog))
		{
			creating = false;
			dialog.Confirmed += DoCreate;
			dialog.Canceled += Cleanup;
			nameEdit.TextSubmitted += delegate
			{
				DoCreate();
			};
			dialog.PopupCentered();
			nameEdit.GrabFocus();
			nameEdit.SelectAll();
		}
		void Cleanup()
		{
			dialog.QueueFree();
		}
		void DoCreate()
		{
			if (!creating)
			{
				creating = true;
				string text = nameEdit.Text.Trim();
				if (string.IsNullOrEmpty(text))
				{
					Cleanup();
				}
				else
				{
					XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction(actionId, baseDir, text);
					if (templateCreateResult.Success)
					{
						RegisterCreatedFilesInManifest(templateCreateResult, "Resources");
						XWFileSystem.GetSingleton().ScanChanges();
						if (!string.IsNullOrWhiteSpace(templateCreateResult.Warning))
						{
							XWEditorInterface.Instance?.ShowToast("创建完成，但旧备份需要手动清理：" + FirstDiagnosticLine(templateCreateResult.Warning), 1);
						}
						if (actionId == "new-character-component-definition")
						{
							XWEditorInterface.Instance?.ShowToast("角色组件已创建，正在后台编译并刷新图鉴…");
							CompleteModernComponentCreationAsync(templateCreateResult);
						}
						else
						{
							XWEditorInterface.Instance?.ShowToast("已创建 " + templateCreateResult.CreatedPath);
							OpenCreatedPath(templateCreateResult.CreatedPath);
						}
					}
					else
					{
						XWEditorInterface.Instance?.ShowToast("创建失败: " + templateCreateResult.Error);
					}
					Cleanup();
				}
			}
		}
	}

	private async Task CompleteModernComponentCreationAsync(XWTemplateLibrary.TemplateCreateResult create)
	{
		string projectRoot = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		XWModComponentAssemblyCatalog.RefreshResult item = await _componentAssemblyCatalog.RefreshAsync(projectRoot, _componentAssemblyRefreshCancellation.Token);
		long num = Interlocked.Increment(ref _nextComponentAssemblyResultId);
		lock (_componentAssemblyResultGate)
		{
			_componentAssemblyResults[num] = (create, item);
		}
		CallDeferred("FinalizeModernComponentCreation", num);
	}

	private void FinalizeModernComponentCreation(long resultId)
	{
		(XWTemplateLibrary.TemplateCreateResult, XWModComponentAssemblyCatalog.RefreshResult) value;
		lock (_componentAssemblyResultGate)
		{
			if (!_componentAssemblyResults.Remove(resultId, out value))
			{
				return;
			}
		}
		if (!IsInsideTree())
		{
			return;
		}
		XWModComponentAssemblyCatalog.RefreshResult item = value.Item2;
		if (item == null || !item.Success)
		{
			string message = value.Item2?.Message ?? "未知编译错误";
			XWEditorInterface.Instance?.ShowToast("角色组件编译失败：" + FirstDiagnosticLine(message), 2);
			{
				foreach (string createdPath in value.Item1.CreatedPaths)
				{
					if (createdPath.EndsWith("Definition.cs", StringComparison.OrdinalIgnoreCase))
					{
						OpenCreatedPath(createdPath);
						break;
					}
				}
				return;
			}
		}
		if (XWEditorInterface.Instance?.TryGetLoadedResourceEditor("character_component_editor") is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor)
		{
			if (value.Item2.ReplacedAssembly != null)
			{
				xWCharacterComponentVisualResourceEditor.ReleaseComponentAssemblyReferences(value.Item2.ReplacedAssembly);
			}
			xWCharacterComponentVisualResourceEditor.SetModComponentDefinitionTypes(value.Item2.LoadedAssembly?.Assembly, value.Item2.DefinitionTypes);
		}
		XWFileSystem.GetSingleton()?.ScanChanges();
		ModCharacterComponentDefinition modCharacterComponentDefinition = ResourceLoader.Load<ModCharacterComponentDefinition>(value.Item1.CreatedPath, "", ResourceLoader.CacheMode.Replace);
		if (GodotObject.IsInstanceValid(modCharacterComponentDefinition) && !TrySynchronizeCreatedComponentDefinition(modCharacterComponentDefinition, value.Item1.CreatedPath, value.Item2.DefinitionTypes, out var error))
		{
			XWEditorInterface.Instance?.ShowToast("角色组件配置初始化失败：" + error, 2);
			OpenCreatedDefinitionScript(value.Item1);
		}
		else if (GodotObject.IsInstanceValid(modCharacterComponentDefinition) && XWResourceEditorRegistry.TryOpen(modCharacterComponentDefinition, value.Item1.CreatedPath))
		{
			XWEditorInterface.Instance?.ShowToast("角色组件已编译并进入可视化编辑器");
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast("角色组件已编译，但 Definition 资源无法打开", 2);
			OpenCreatedPath(value.Item1.CreatedPath);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Only catalog-validated public Mod component definitions are instantiated for schema synchronization.")]
	private static bool TrySynchronizeCreatedComponentDefinition(ModCharacterComponentDefinition resource, string resourcePath, IReadOnlyList<Type> definitionTypes, out string error)
	{
		error = "";
		Type type = definitionTypes?.FirstOrDefault((Type type2) => string.Equals(type2.FullName, resource.DefinitionTypeName, StringComparison.Ordinal) || string.Equals(type2.Name, resource.DefinitionTypeName, StringComparison.Ordinal));
		if (type == null)
		{
			error = "未找到 " + resource.DefinitionTypeName + " 的活动 Definition";
			return false;
		}
		CharacterComponentDefinition characterComponentDefinition = null;
		try
		{
			characterComponentDefinition = Activator.CreateInstance(type) as CharacterComponentDefinition;
			if (characterComponentDefinition == null)
			{
				error = "无法实例化 " + type.FullName;
				return false;
			}
			if (!resource.SynchronizeConfiguration(characterComponentDefinition))
			{
				return true;
			}
			Error error2 = ResourceSaver.Save(resource, resourcePath, ResourceSaver.SaverFlags.None);
			if (error2 == Error.Ok)
			{
				return true;
			}
			error = $"保存配置元数据失败：{error2}";
			return false;
		}
		catch (Exception ex)
		{
			error = ex.GetBaseException().Message;
			return false;
		}
		finally
		{
			characterComponentDefinition?.Dispose();
		}
	}

	private void OpenCreatedDefinitionScript(XWTemplateLibrary.TemplateCreateResult create)
	{
		foreach (string createdPath in create.CreatedPaths)
		{
			if (createdPath.EndsWith("Definition.cs", StringComparison.OrdinalIgnoreCase))
			{
				OpenCreatedPath(createdPath);
				break;
			}
		}
	}

	private static string FirstDiagnosticLine(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return "未知错误";
		}
		using StringReader stringReader = new StringReader(message);
		return stringReader.ReadLine()?.Trim() ?? message.Trim();
	}

	private void ShowSubResourceCreateDialog(string actionId, string ownerPath)
	{
		ownerPath = ownerPath?.Replace('\\', '/') ?? "";
		IReadOnlyList<XWResourceCreateRoute.CreateAction> subResourceActionsForOwner = XWResourceCreateRoute.GetSubResourceActionsForOwner(ownerPath, XWFileSystem.GetSingleton().ProjectFolderPath);
		XWResourceCreateRoute.CreateAction createAction = null;
		foreach (XWResourceCreateRoute.CreateAction item in subResourceActionsForOwner)
		{
			if (item.Id == actionId)
			{
				createAction = item;
				break;
			}
		}
		if (createAction == null)
		{
			XWEditorInterface.Instance?.ShowToast("当前资源不能创建该子资源。");
			return;
		}
		LineEdit nameEdit;
		ConfirmationDialog dialog = CreateNameInputDialog(createAction.DisplayName, "输入子资源名称", createAction.DefaultName, out nameEdit);
		bool creating;
		if (GodotObject.IsInstanceValid(dialog))
		{
			creating = false;
			dialog.Confirmed += DoCreate;
			dialog.Canceled += Cleanup;
			nameEdit.TextSubmitted += delegate
			{
				DoCreate();
			};
			dialog.PopupCentered();
			nameEdit.GrabFocus();
			nameEdit.SelectAll();
		}
		void Cleanup()
		{
			dialog.QueueFree();
		}
		void DoCreate()
		{
			if (!creating)
			{
				creating = true;
				string text = nameEdit.Text.Trim();
				if (string.IsNullOrEmpty(text))
				{
					Cleanup();
				}
				else
				{
					XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateSubResourceFromAction(actionId, ownerPath, text);
					if (templateCreateResult.Success)
					{
						RegisterCreatedFilesInManifest(templateCreateResult, GetManifestSectionForTemplate(templateCreateResult.TemplateId));
						XWFileSystem.GetSingleton().ScanChanges();
						XWEditorInterface.Instance?.ShowToast("已创建子资源 " + templateCreateResult.CreatedPath);
						OpenCreatedPath(templateCreateResult.CreatedPath);
					}
					else
					{
						XWEditorInterface.Instance?.ShowToast("创建子资源失败: " + templateCreateResult.Error);
					}
					Cleanup();
				}
			}
		}
	}

	private void ShowAnimationImportDialog(string baseDir)
	{
		baseDir = NormalizeDirectoryPath(baseDir);
		XWAdobeAnimateTrace.Write("UI file dialog open: baseDir=" + baseDir);
		XWEditorInterface.Instance?.AddOutputMessage("Adobe Animate import dialog: " + baseDir, XWOutputPanel.MessageType.Editor);
		if (string.IsNullOrEmpty(baseDir))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开或创建 Mod 工程。");
			return;
		}
		_animationImportTargetDir = baseDir;
		_animationImportFileDialog.PopupCentered();
	}

	private void OnAnimationImportFileSelected(string path)
	{
		XWAdobeAnimateTrace.Write("UI file dialog result: ok=True, count=1, first=" + path);
		XWEditorInterface.Instance?.AddOutputMessage("Adobe Animate file dialog result: ok=True, count=1", XWOutputPanel.MessageType.Editor);
		StartAnimationImportAsync(path, _animationImportTargetDir);
	}

	private void OnAnimationImportFileDialogCanceled()
	{
		XWAdobeAnimateTrace.Write("UI file dialog result: ok=False, count=0, first=");
		XWEditorInterface.Instance?.AddOutputMessage("Adobe Animate file dialog result: ok=False, count=0", XWOutputPanel.MessageType.Editor);
	}

	private async void StartAnimationImportAsync(string sourcePath, string baseDir)
	{
		if (_animationImportInProgress)
		{
			XWEditorInterface.Instance?.ShowToast("已有动画导入任务正在运行");
			return;
		}
		XWAdobeAnimateTrace.ResetLog();
		XWAdobeAnimateTrace.Write("UI Adobe Animate import start: source=" + sourcePath + ", baseDir=" + baseDir);
		XWEditorInterface.Instance?.AddOutputMessage("Adobe Animate import start: " + sourcePath, XWOutputPanel.MessageType.Editor);
		XWEditorInterface.Instance?.AddOutputMessage("Adobe Animate trace log: " + XWAdobeAnimateTrace.LogPath, XWOutputPanel.MessageType.Editor);
		_animationImportInProgress = true;
		_animationImportCancellation?.Dispose();
		_animationImportCancellation = new CancellationTokenSource();
		XWProgressDialog progressDialog = GetAnimationImportProgressDialog();
		progressDialog.Title = "导入 Adobe Animate 动画";
		progressDialog.SetMaxValue(100.0);
		progressDialog.SetProgressValue(5.0);
		progressDialog.SetStatus("正在导入: " + sourcePath.GetFile());
		progressDialog.PopupCentered();
		try
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			progressDialog.SetProgressValue(25.0);
			progressDialog.SetStatus("正在导出 DAT: " + sourcePath.GetFile());
			progressDialog.SetProgressValue(55.0);
			XWAdobeAnimateImportResult xWAdobeAnimateImportResult = await XWAdobeAnimateImportService.ImportAdobeAnimateFileAsync(sourcePath, baseDir, "", _animationImportCancellation.Token);
			XWAdobeAnimateTrace.Write($"UI Adobe Animate import result: success={xWAdobeAnimateImportResult.Success}, resource={xWAdobeAnimateImportResult.ResourcePath}, dat={xWAdobeAnimateImportResult.DatPath}, error={xWAdobeAnimateImportResult.Error}");
			if (!xWAdobeAnimateImportResult.Success)
			{
				XWEditorInterface.Instance?.ShowToast("动画导入失败: " + xWAdobeAnimateImportResult.Error);
				return;
			}
			progressDialog.SetStatus("正在读取动画数据: " + xWAdobeAnimateImportResult.ResourcePath.GetFile());
			progressDialog.SetProgressValue(75.0);
			if (HydrateImportedAnimationResource(xWAdobeAnimateImportResult, progressDialog))
			{
				progressDialog.SetProgressValue(100.0);
				XWEditorInterface.Instance?.ShowToast("已导入动画: " + xWAdobeAnimateImportResult.ResourcePath);
				if (GodotObject.IsInstanceValid(progressDialog))
				{
					progressDialog.Hide();
				}
				XWAdobeAnimateTrace.Write("UI Adobe Animate import resource ready: " + xWAdobeAnimateImportResult.ResourcePath);
				OpenCreatedPath(xWAdobeAnimateImportResult.ResourcePath);
			}
		}
		catch (Exception ex)
		{
			XWEditorInterface.Instance?.ShowToast("动画导入失败: " + ex.Message);
		}
		finally
		{
			_animationImportInProgress = false;
			_animationImportCancellation?.Dispose();
			_animationImportCancellation = null;
			if (GodotObject.IsInstanceValid(progressDialog))
			{
				progressDialog.Hide();
			}
		}
	}

	private bool HydrateImportedAnimationResource(XWAdobeAnimateImportResult result, XWProgressDialog progressDialog)
	{
		if (result == null || string.IsNullOrWhiteSpace(result.ResourcePath) || string.IsNullOrWhiteSpace(result.DatPath))
		{
			XWEditorInterface.Instance?.ShowToast("动画导入失败: 缺少动画资源或 DAT 路径。");
			return false;
		}
		XWAdobeAnimateTrace.Write("UI Adobe Animate import full data load start: resource=" + result.ResourcePath + ", dat=" + result.DatPath);
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(result.ResourcePath, "", ResourceLoader.CacheMode.Ignore);
		if (adobeAnimateData == null)
		{
			adobeAnimateData = new AdobeAnimateData
			{
				ResourceName = result.ResourcePath.GetFile().GetBaseName(),
				ResourcePath = result.ResourcePath
			};
		}
		adobeAnimateData.ResourcePath = result.ResourcePath;
		adobeAnimateData.SetAnimeFileForModImport(result.DatPath.GetFile());
		adobeAnimateData.InitForModImport(result.DatPath);
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(125, 7);
		defaultInterpolatedStringHandler.AppendLiteral("UI Adobe Animate import runtime data state: frames=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.frameMax);
		defaultInterpolatedStringHandler.AppendLiteral(", atlasSource=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.HasEmbeddedAtlasSource());
		defaultInterpolatedStringHandler.AppendLiteral(", media=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.mediaDictionary?.Count ?? 0);
		defaultInterpolatedStringHandler.AppendLiteral(", layers=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.layerDictionary?.Count ?? 0);
		defaultInterpolatedStringHandler.AppendLiteral(", packedFrames=");
		int[] frameOffsets = adobeAnimateData.frameOffsets;
		defaultInterpolatedStringHandler.AppendFormatted((frameOffsets != null) ? frameOffsets.Length : 0);
		defaultInterpolatedStringHandler.AppendLiteral(", packedSlices=");
		int[] sliceKeys = adobeAnimateData.sliceKeys;
		defaultInterpolatedStringHandler.AppendFormatted((sliceKeys != null) ? sliceKeys.Length : 0);
		defaultInterpolatedStringHandler.AppendLiteral(", mediaRects=");
		Vector4[] mediaRects = adobeAnimateData.mediaRects;
		defaultInterpolatedStringHandler.AppendFormatted((mediaRects != null) ? mediaRects.Length : 0);
		XWAdobeAnimateTrace.Write(defaultInterpolatedStringHandler.ToStringAndClear());
		if (!HasImportedAnimationFullData(adobeAnimateData))
		{
			XWAdobeAnimateTrace.WriteError("UI Adobe Animate import full data missing after load: resource=" + result.ResourcePath + ", dat=" + result.DatPath);
			XWEditorInterface.Instance?.ShowToast("动画导入失败: DAT 已生成，但完整动画数据读取失败。");
			return false;
		}
		Error error = ResourceSaver.Save(adobeAnimateData, result.ResourcePath, ResourceSaver.SaverFlags.None);
		defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(110, 6);
		defaultInterpolatedStringHandler.AppendLiteral("UI Adobe Animate import runtime data saved: resource=");
		defaultInterpolatedStringHandler.AppendFormatted(result.ResourcePath);
		defaultInterpolatedStringHandler.AppendLiteral(", result=");
		defaultInterpolatedStringHandler.AppendFormatted(error);
		defaultInterpolatedStringHandler.AppendLiteral(", frames=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.frameMax);
		defaultInterpolatedStringHandler.AppendLiteral(", layers=");
		defaultInterpolatedStringHandler.AppendFormatted(adobeAnimateData.layerDictionary?.Count ?? 0);
		defaultInterpolatedStringHandler.AppendLiteral(", packedFrames=");
		int[] frameOffsets2 = adobeAnimateData.frameOffsets;
		defaultInterpolatedStringHandler.AppendFormatted((frameOffsets2 != null) ? frameOffsets2.Length : 0);
		defaultInterpolatedStringHandler.AppendLiteral(", packedSlices=");
		int[] sliceKeys2 = adobeAnimateData.sliceKeys;
		defaultInterpolatedStringHandler.AppendFormatted((sliceKeys2 != null) ? sliceKeys2.Length : 0);
		XWAdobeAnimateTrace.Write(defaultInterpolatedStringHandler.ToStringAndClear());
		if (error != Error.Ok)
		{
			XWEditorInterface.Instance?.ShowToast($"动画导入失败: 完整动画资源保存失败 {error}");
			return false;
		}
		progressDialog?.SetProgressValue(90.0);
		return true;
	}

	private static bool HasImportedAnimationFullData(AdobeAnimateData animation)
	{
		if (animation != null && animation.frameMax > 0 && animation.HasEmbeddedAtlasSource() && animation.mediaDictionary != null && animation.mediaDictionary.Count > 0 && animation.layerDictionary != null && animation.layerDictionary.Count > 0 && animation.frameOffsets != null && animation.frameOffsets.Length == animation.frameMax && animation.frameCounts != null && animation.frameCounts.Length == animation.frameMax && animation.mediaRects != null && animation.mediaRects.Length != 0 && animation.sliceKeys != null)
		{
			return animation.sliceKeys.Length != 0;
		}
		return false;
	}

	private XWProgressDialog GetAnimationImportProgressDialog()
	{
		if (_animationImportProgressDialog != null && GodotObject.IsInstanceValid(_animationImportProgressDialog))
		{
			return _animationImportProgressDialog;
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWProgressDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		_animationImportProgressDialog = packedScene.Instantiate<XWProgressDialog>(PackedScene.GenEditState.Disabled);
		_animationImportProgressDialog.ProgressCanceled += delegate
		{
			if (_animationImportCancellation != null)
			{
				_animationImportCancellation.Cancel();
				XWEditorInterface.Instance?.ShowToast("正在取消动画导入");
			}
		};
		AddChild(_animationImportProgressDialog, forceReadableName: false, InternalMode.Disabled);
		return _animationImportProgressDialog;
	}

	private void InstantiateAndSaveResource(string typeName, string baseDir)
	{
		string baseName = typeName + ".tres";
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		baseName = singleton.GenerateUniqueFileName(baseDir, baseName);
		string text = baseDir + baseName;
		string text2 = "[gd_resource type=\"" + typeName + "\" format=3]\n\n[resource]\n";
		Godot.FileAccess fileAccess = Godot.FileAccess.Open(text, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法创建资源: " + text);
			return;
		}
		fileAccess.StoreString(text2);
		fileAccess.Close();
		singleton.ScanChanges();
		XWEditorInterface.Instance?.ShowToast("已创建: " + text);
	}

	private void AddFavoriteOp(List<string> paths)
	{
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		foreach (string item in (paths.Count > 0) ? paths : (string.IsNullOrEmpty(_rmbSelectedPath) ? new List<string>() : new List<string> { _rmbSelectedPath }))
		{
			if (!string.IsNullOrEmpty(item))
			{
				singleton.AddFavorite(item);
			}
		}
		if (_tree.FavoritesItem != null)
		{
			_tree.FavoritesItem.SetCollapsed(enable: false);
		}
	}

	private void RemoveFavoriteOp(List<string> paths)
	{
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		foreach (string item in (paths.Count > 0) ? paths : (string.IsNullOrEmpty(_rmbSelectedPath) ? new List<string>() : new List<string> { _rmbSelectedPath }))
		{
			if (!string.IsNullOrEmpty(item))
			{
				singleton.RemoveFavorite(item);
			}
		}
	}

	private void DeleteSelected(List<string> paths)
	{
		if (paths.Count == 0 && !string.IsNullOrEmpty(_rmbSelectedPath))
		{
			paths = new List<string> { _rmbSelectedPath };
		}
		if (paths.Count != 0)
		{
			if (IsProtectedDirectory(paths[0]))
			{
				XWEditorInterface.Instance?.ShowToast("固定目录不能删除。");
			}
			else
			{
				_removeDialog.PopupFor(paths[0]);
			}
		}
	}

	private void DuplicateSelected()
	{
		List<string> list = GetSelectedPaths();
		if (list.Count == 0 && !string.IsNullOrEmpty(_rmbSelectedPath))
		{
			list = new List<string> { _rmbSelectedPath };
		}
		foreach (string item in list)
		{
			if (string.IsNullOrEmpty(item))
			{
				continue;
			}
			string text = item.GetBaseDir() + "/";
			string file = item.GetFile();
			XWFileSystem singleton = XWFileSystem.GetSingleton();
			string text2 = text + singleton.GenerateUniqueFileName(text, file, file);
			if (Godot.FileAccess.FileExists(item))
			{
				if (DirAccess.CopyAbsolute(item, text2) == Error.Ok)
				{
					singleton.ScanChanges();
				}
			}
			else if (DirAccess.DirExistsAbsolute(item))
			{
				singleton.CopyDirectory(item.EndsWith("/") ? item : (item + "/"), text2.EndsWith("/") ? text2 : (text2 + "/"));
			}
		}
	}

	private void CopyResource(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			_clipboardPath = path;
			_clipboardIsCopy = true;
		}
	}

	private void PasteResource()
	{
		if (string.IsNullOrEmpty(_clipboardPath))
		{
			return;
		}
		string selectedDir = GetSelectedDir();
		if (!DirAccess.DirExistsAbsolute(selectedDir))
		{
			return;
		}
		string file = _clipboardPath.GetFile();
		string text = selectedDir + file;
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		if (_clipboardPath == text)
		{
			text = selectedDir + singleton.GenerateUniqueFileName(selectedDir, file);
		}
		if (Godot.FileAccess.FileExists(_clipboardPath))
		{
			if (DirAccess.CopyAbsolute(_clipboardPath, text) == Error.Ok)
			{
				singleton.ScanChanges();
			}
		}
		else if (DirAccess.DirExistsAbsolute(_clipboardPath))
		{
			singleton.CopyDirectory(_clipboardPath, text);
		}
	}

	private void MoveTo()
	{
		List<string> list = GetSelectedPaths();
		if (list.Count == 0 && !string.IsNullOrEmpty(_rmbSelectedPath))
		{
			list = new List<string> { _rmbSelectedPath };
		}
		if (list.Count != 0)
		{
			_moveSourcePath = list[0];
			string selectedDir = GetSelectedDir();
			_moveToDirectoryDialog.Title = "移动 \"" + _moveSourcePath.GetFile() + "\" 到...";
			_moveToDirectoryDialog.CurrentDir = selectedDir;
			_moveToDirectoryDialog.PopupCentered();
		}
	}

	private void OnMoveTargetDirectorySelected(string targetDir)
	{
		string moveSourcePath = _moveSourcePath;
		_moveSourcePath = "";
		if (string.IsNullOrWhiteSpace(moveSourcePath))
		{
			return;
		}
		string text = targetDir.TrimSuffix("/") + "/" + moveSourcePath.GetFile();
		if (!(moveSourcePath == text))
		{
			Error error = XWFileSystem.GetSingleton().MoveFile(moveSourcePath, text);
			if (error == Error.Ok)
			{
				XWFileSystem.GetSingleton().ScanChanges();
				XWEditorInterface.Instance?.ShowToast("已移动到: " + text);
			}
			else
			{
				XWEditorInterface.Instance?.ShowToast($"移动失败: {error}");
			}
		}
	}

	private void InstantiateScene(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return;
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
		if (packedScene == null)
		{
			return;
		}
		Node editedSceneRoot = GetTree().EditedSceneRoot;
		if (editedSceneRoot != null)
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node != null)
			{
				editedSceneRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
				node.Owner = editedSceneRoot;
			}
		}
	}

	private static string ReadResourceUid(string resPath)
	{
		string path = resPath + ".uid";
		if (!Godot.FileAccess.FileExists(path))
		{
			return "";
		}
		Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return "";
		}
		string text = fileAccess.GetAsText().Trim();
		fileAccess.Close();
		if (!text.StartsWith("uid://"))
		{
			return "";
		}
		return text;
	}

	private string GetListSelectedItemPath()
	{
		int[] selectedItems = _list.GetSelectedItems();
		if (selectedItems.Length == 0)
		{
			return "";
		}
		Variant itemMetadata = _list.GetItemMetadata(selectedItems[0]);
		if (itemMetadata.VariantType == Variant.Type.Nil)
		{
			return "";
		}
		return itemMetadata.As<XWFileSystemTreeItemData>()?.Path ?? "";
	}

	private ConfirmationDialog CreateNameInputDialog(string title, string prompt, string defaultName, out LineEdit nameEdit)
	{
		nameEdit = null;
		if (_nameInputDialogScene == null)
		{
			_nameInputDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/FileSystem/GUI/Dialog/XWNameInputDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		ConfirmationDialog confirmationDialog = _nameInputDialogScene?.Instantiate<ConfirmationDialog>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(confirmationDialog))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载名称输入对话框");
			return null;
		}
		Label node = confirmationDialog.GetNode<Label>("%PromptLabel");
		nameEdit = confirmationDialog.GetNode<LineEdit>("%NameEdit");
		confirmationDialog.Title = title ?? "";
		node.Text = prompt ?? "";
		nameEdit.Text = defaultName ?? "";
		AddChild(confirmationDialog, forceReadableName: false, InternalMode.Disabled);
		return confirmationDialog;
	}

	public void ShowNewCSharpScriptDialog()
	{
		ShowNewScriptDialog(GetSelectedDir(), cSharpOnly: true);
	}

	public void ShowNewLogicDialog()
	{
		ShowNewScriptDialog(GetSelectedDir());
	}

	private void ShowNewScriptDialog(string baseDir, bool cSharpOnly = false)
	{
		baseDir = NormalizeDirectoryPath(baseDir);
		XWTemplateLibrary templateLibrary = new XWTemplateLibrary();
		List<(string TemplateId, string DisplayName)> templateOptions = BuildScriptTemplateOptionsForDirectory(baseDir, templateLibrary);
		string templateId = ((templateOptions.Count > 0) ? templateOptions[0].TemplateId : GetScriptTemplateIdForDirectory(baseDir));
		string displayNameLabel = XWTemplatePresentation.Resolve(templateLibrary.FindTemplate(templateId)).DisplayNameLabel;
		if (_scriptCreateDialogScene == null)
		{
			_scriptCreateDialogScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/FileSystem/GUI/Dialog/XWScriptCreateDialog.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		ConfirmationDialog dialog = _scriptCreateDialogScene?.Instantiate<ConfirmationDialog>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(dialog))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载新建脚本对话框");
			return;
		}
		Label node = dialog.GetNode<Label>("%DirectoryLabel");
		VBoxContainer node2 = dialog.GetNode<VBoxContainer>("%AuthoringModeSection");
		Button cSharpModeCard = dialog.GetNode<Button>("%CSharpModeCard");
		Button blueprintModeCard = dialog.GetNode<Button>("%BlueprintModeCard");
		Label authoringModeHint = dialog.GetNode<Label>("%AuthoringModeHint");
		Label templateTitle = dialog.GetNode<Label>("%TemplateTitle");
		HBoxContainer node3 = dialog.GetNode<HBoxContainer>("%TemplateRow");
		OptionButton templateOption = dialog.GetNode<OptionButton>("%TemplateOption");
		HFlowContainer node4 = dialog.GetNode<HFlowContainer>("%TemplateCards");
		ScrollContainer templateCardScroll = dialog.GetNode<ScrollContainer>("%TemplateCardScroll");
		LineEdit nameEdit = dialog.GetNode<LineEdit>("%NameEdit");
		HBoxContainer previewHeader = dialog.GetNode<HBoxContainer>("%PreviewHeader");
		TextEdit templatePreview = dialog.GetNode<TextEdit>("%TemplatePreview");
		Label hint = dialog.GetNode<Label>("%TemplateHint");
		dialog.Title = (cSharpOnly ? ("新建" + displayNameLabel) : "新建 Mod 逻辑");
		node.Text = "目录：" + baseDir;
		hint.Text = "模板：" + displayNameLabel;
		templateTitle.Text = "选择 C# 模板";
		node3.Visible = templateOptions.Count > 1;
		node3.Hide();
		for (int i = 0; i < templateOptions.Count; i++)
		{
			templateOption.AddItem(templateOptions[i].DisplayName, i);
			Texture2D texture2D = ResourceLoader.Load<Texture2D>(ResolveScriptTemplateIconPath(templateLibrary.FindTemplate(templateOptions[i].TemplateId)), null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				templateOption.SetItemIcon(i, texture2D);
			}
		}
		if (templateOptions.Count > 0)
		{
			templateOption.Select(0);
		}
		int selectedTemplateIndex = 0;
		bool blueprintMode = false;
		List<Button> cardButtons = new List<Button>();
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		ButtonGroup buttonGroup2 = new ButtonGroup
		{
			AllowUnpress = false
		};
		cSharpModeCard.ButtonGroup = buttonGroup2;
		blueprintModeCard.ButtonGroup = buttonGroup2;
		for (int j = 0; j < templateOptions.Count; j++)
		{
			int cardIndex = j;
			XWTemplateLibrary.TemplateInfo template = templateLibrary.FindTemplate(templateOptions[j].TemplateId);
			XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(template);
			Button button = new Button
			{
				Name = $"ScriptTemplateCard_{j}",
				Text = xWTemplatePresentation.CategoryLabel + "\n" + xWTemplatePresentation.DisplayNameLabel,
				TooltipText = xWTemplatePresentation.PreviewTextLabel,
				ToggleMode = true,
				ButtonGroup = buttonGroup,
				CustomMinimumSize = new Vector2(160f, 72f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Icon = ResourceLoader.Load<Texture2D>(ResolveScriptTemplateIconPath(template), null, ResourceLoader.CacheMode.Reuse),
				ExpandIcon = false
			};
			button.Pressed += delegate
			{
				SelectTemplateCard(cardIndex);
			};
			node4.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			cardButtons.Add(button);
		}
		templateOption.ItemSelected += delegate(long index)
		{
			SelectTemplateCard((int)index);
		};
		nameEdit.TextChanged += delegate
		{
			SelectTemplateCard(selectedTemplateIndex);
		};
		cSharpModeCard.Pressed += delegate
		{
			SelectAuthoringMode(useBlueprint: false);
		};
		blueprintModeCard.Pressed += delegate
		{
			SelectAuthoringMode(useBlueprint: true);
		};
		SelectTemplateCard(0);
		node2.Visible = !cSharpOnly;
		blueprintModeCard.Visible = !cSharpOnly;
		SelectAuthoringMode(useBlueprint: false);
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		bool creating = false;
		dialog.Confirmed += DoCreate;
		dialog.Canceled += Cleanup;
		nameEdit.TextSubmitted += delegate
		{
			DoCreate();
		};
		dialog.PopupCenteredClamped(new Vector2I(720, 540), 0.9f);
		nameEdit.GrabFocus();
		void Cleanup()
		{
			dialog.QueueFree();
		}
		void DoCreate()
		{
			if (!creating)
			{
				creating = true;
				if (blueprintMode)
				{
					dialog.Hide();
					Cleanup();
					CreateNewBlueprint(baseDir);
				}
				else
				{
					string text = nameEdit.Text.Trim();
					selectedTemplateIndex = Mathf.Clamp(selectedTemplateIndex, 0, Mathf.Max(0, templateOptions.Count - 1));
					string templateId2 = ((templateOptions.Count > 0) ? templateOptions[selectedTemplateIndex].TemplateId : templateId);
					if (string.IsNullOrWhiteSpace(text))
					{
						text = XWTemplatePresentation.Resolve(templateLibrary.FindTemplate(templateId2)).SuggestedName;
					}
					XWTemplateLibrary.TemplateCreateResult templateCreateResult = templateLibrary.CreateFromTemplateInDirectory(templateId2, baseDir, text);
					if (templateCreateResult.Success)
					{
						RegisterCreatedFilesInManifest(templateCreateResult, "Scripts");
						foreach (string createdPath in templateCreateResult.CreatedPaths)
						{
							if (createdPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
							{
								XWBPCSharpMemberRegistry.Instance.NotifyScriptSaved(createdPath);
							}
						}
						XWFileSystem.GetSingleton().ScanChanges();
						XWEditorInterface.Instance?.ShowToast("已创建 " + templateCreateResult.CreatedPath);
						CompleteTemplateCreationFromTool(templateCreateResult);
					}
					else
					{
						XWEditorInterface.Instance?.ShowToast("创建失败: " + templateCreateResult.Error);
					}
					Cleanup();
				}
			}
		}
		void SelectAuthoringMode(bool useBlueprint)
		{
			blueprintMode = useBlueprint && !cSharpOnly;
			cSharpModeCard.SetPressedNoSignal(!blueprintMode);
			blueprintModeCard.SetPressedNoSignal(blueprintMode);
			templateTitle.Visible = !blueprintMode;
			templateCardScroll.Visible = !blueprintMode;
			nameEdit.Visible = !blueprintMode;
			previewHeader.Visible = !blueprintMode;
			templatePreview.Visible = !blueprintMode;
			hint.Visible = !blueprintMode;
			dialog.GetOkButton().Text = (blueprintMode ? "下一步：配置蓝图" : "创建 C#");
			authoringModeHint.Text = (blueprintMode ? "蓝图是唯一可视化脚本系统；下一步选择父类和保存位置。" : "C# 是唯一文本脚本语言；选择模板后会直接进入脚本编辑器。");
			if (!blueprintMode && nameEdit.IsInsideTree())
			{
				nameEdit.GrabFocus();
			}
		}
		void SelectTemplateCard(int index)
		{
			if (templateOptions.Count != 0)
			{
				selectedTemplateIndex = Mathf.Clamp(index, 0, templateOptions.Count - 1);
				templateOption.Select(selectedTemplateIndex);
				(string, string) tuple = templateOptions[selectedTemplateIndex];
				XWTemplateLibrary.TemplateInfo templateInfo = templateLibrary.FindTemplate(tuple.Item1);
				XWTemplatePresentation xWTemplatePresentation2 = XWTemplatePresentation.Resolve(templateInfo);
				hint.Text = ((templateInfo == null) ? ("模板：" + tuple.Item2) : $"{xWTemplatePresentation2.CategoryLabel} · {xWTemplatePresentation2.DisplayNameLabel} · {xWTemplatePresentation2.DefaultFolderLabel}");
				nameEdit.PlaceholderText = "例如：" + xWTemplatePresentation2.SuggestedName;
				templatePreview.Text = BuildScriptTemplatePreview(templateInfo, nameEdit.Text);
				for (int k = 0; k < cardButtons.Count; k++)
				{
					cardButtons[k].SetPressedNoSignal(k == selectedTemplateIndex);
				}
			}
		}
	}

	private static string BuildScriptTemplatePreview(XWTemplateLibrary.TemplateInfo template, string resourceName)
	{
		if (template == null)
		{
			return "// 请选择脚本模板";
		}
		string text = XWTemplateLibrary.SanitizeName(string.IsNullOrWhiteSpace(resourceName) ? "NewScript" : resourceName);
		return ((string.IsNullOrWhiteSpace(template.Content) ? template.PreviewText : template.Content) ?? "").Replace("${Name}", text).Replace("${DisplayName}", string.IsNullOrWhiteSpace(resourceName) ? text : resourceName.Trim());
	}

	private static string ResolveScriptTemplateIconPath(XWTemplateLibrary.TemplateInfo template)
	{
		switch (template?.Id)
		{
		case "feature-csharp":
			return "res://addons/ModEditor/Icons/ClassIcon/AnimationTrackGroup.svg";
		case "process-csharp":
			return "res://addons/ModEditor/Icons/ClassIcon/AnimationPlayer.svg";
		case "character-component":
			return "res://addons/ModEditor/Icons/ClassIcon/Node.svg";
		case "projectile-csharp":
			return "res://addons/ModEditor/Icons/ResourceProjectile.svg";
		case "character-csharp":
		case "character-event-csharp":
			return "res://addons/ModEditor/Icons/ResourceCharacter.svg";
		default:
			return "res://addons/ModEditor/Icons/ScriptCreate.svg";
		}
	}

	private void BeginTreeInlineRename(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			if (IsProtectedDirectory(path))
			{
				XWEditorInterface.Instance?.ShowToast("固定目录不能重命名。");
			}
			else if (!_tree.BeginInlineRenamePath(path))
			{
				XWEditorInterface.Instance?.ShowToast("无法在文件树中重命名该项目。");
			}
		}
	}

	private void OnTreeInlineRenameSubmitted(string oldPath, string newName)
	{
		oldPath = oldPath?.Replace('\\', '/') ?? "";
		newName = newName?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(oldPath))
		{
			return;
		}
		string file = oldPath.TrimEnd('/').GetFile();
		if (string.IsNullOrWhiteSpace(newName) || newName == file)
		{
			return;
		}
		if (newName.Contains('/') || newName.Contains('\\'))
		{
			XWEditorInterface.Instance?.ShowToast("名称不能包含路径分隔符。");
			return;
		}
		if (IsProtectedDirectory(oldPath))
		{
			XWEditorInterface.Instance?.ShowToast("固定目录不能重命名。");
			return;
		}
		bool num = DirAccess.DirExistsAbsolute(oldPath);
		string text = oldPath.TrimEnd('/').GetBaseDir().PathJoin(newName);
		if (num)
		{
			text += "/";
		}
		if (Godot.FileAccess.FileExists(text) || DirAccess.DirExistsAbsolute(text))
		{
			XWEditorInterface.Instance?.ShowToast("目标已存在: " + newName);
			return;
		}
		Error error = XWFileSystem.GetSingleton().MoveFile(oldPath, text);
		if (error == Error.Ok)
		{
			XWEditorInterface.Instance?.ShowToast("已重命名: " + file + " → " + newName);
			return;
		}
		XWEditorInterface.Instance?.ShowToast($"重命名失败: {error}");
		XWFileSystem.GetSingleton().ScanChanges();
	}

	private void ShowRenameDialog(string path)
	{
		string oldName = path.GetFile();
		LineEdit nameEdit;
		ConfirmationDialog dialog = CreateNameInputDialog("重命名", "原名称: " + oldName, oldName, out nameEdit);
		if (GodotObject.IsInstanceValid(dialog))
		{
			dialog.Confirmed += DoRename;
			dialog.Canceled += Cleanup;
			nameEdit.TextSubmitted += delegate
			{
				DoRename();
			};
			dialog.PopupCentered();
			nameEdit.GrabFocus();
			nameEdit.SelectAll();
		}
		void Cleanup()
		{
			dialog.QueueFree();
		}
		void DoRename()
		{
			string text = nameEdit.Text.Trim();
			if (string.IsNullOrEmpty(text) || text == oldName)
			{
				Cleanup();
			}
			else
			{
				string to = path.GetBaseDir().PathJoin(text);
				Error error = XWFileSystem.GetSingleton().MoveFile(path, to);
				if (error == Error.Ok)
				{
					XWFileSystem.GetSingleton().ScanChanges();
					XWEditorInterface.Instance?.ShowToast("已重命名: " + oldName + " → " + text);
				}
				else
				{
					XWEditorInterface.Instance?.ShowToast($"重命名失败: {error}");
				}
				Cleanup();
			}
		}
	}
}
