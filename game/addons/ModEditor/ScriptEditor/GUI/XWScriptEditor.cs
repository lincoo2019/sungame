using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Microsoft.CodeAnalysis.CSharp;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Debugging;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWScriptEditor : PanelContainer
{
	private enum MenuFile
	{
		NewScript = 0,
		Sep1 = 1,
		Open = 3,
		Sep2 = 4,
		Save = 5,
		SaveAs = 6,
		SaveAll = 7,
		Sep3 = 8,
		Close = 9,
		CloseAll = 10,
		CloseOther = 11
	}

	private enum DiagnosticsContextMenu
	{
		CopySelected,
		CopyAll
	}

	private enum SearchMenu
	{
		Find,
		Replace,
		Next,
		Previous
	}

	private enum GoToMenu
	{
		Line,
		NextProblem,
		PreviousProblem
	}

	private enum ToolbarOverflowAction
	{
		RenameUndo = 0,
		RenameRedo = 1,
		Format = 3,
		Validate = 4,
		PreviousProblem = 6,
		NextProblem = 7,
		OpenBlueprint = 9
	}

	private readonly struct SearchMatch(int line, int column)
	{
		public int Line { get; } = line;

		public int Column { get; } = column;
	}

	private class ScriptTabInfo
	{
		public string FilePath = "";

		public string Text = "";

		public int CursorLine;

		public int CursorColumn;

		public int ScrollVertical;

		public bool HasUnsavedChanges;

		public string FileExtension = "";

		public ulong ModifiedTime;

		public bool DiskExists;

		public string DiskContentHash = "";

		public long DiskByteLength;

		public bool HasExternalConflict;

		public bool ConflictDiskExists;

		public string ConflictDiskText = "";

		public string ConflictDiskContentHash = "";

		public long ConflictDiskByteLength;

		public ulong ConflictModifiedTime;

		public int[] Breakpoints = Array.Empty<int>();

		public XWBlueprintGeneratedCSharpPolicy.LinkState GeneratedLinkState;

		public string BlueprintSourcePath = "";

		public bool IsBlueprintGenerated => GeneratedLinkState != XWBlueprintGeneratedCSharpPolicy.LinkState.NotGenerated;
	}

	private sealed class DiagnosticListEntry
	{
		public XWCodeErrorChecker.ErrorData Diagnostic;

		public string FilePath = "";
	}

	public enum ScriptSaveStatus
	{
		Unchanged,
		Saved,
		Conflict,
		IoError,
		ReadOnly
	}

	private readonly struct ScriptDiskSnapshot(bool exists, string text, string contentHash, long byteLength, ulong modifiedTime)
	{
		public bool Exists { get; } = exists;

		public string Text { get; } = text ?? "";

		public string ContentHash { get; } = contentHash ?? "";

		public long ByteLength { get; } = byteLength;

		public ulong ModifiedTime { get; } = modifiedTime;
	}

	private enum AtomicWriteFailureKind
	{
		None,
		Conflict,
		ReadOnly,
		IoError
	}

	internal enum IdeRequestKind
	{
		Definition,
		References,
		HoverSignature,
		HoverPopup,
		RenameSymbol,
		RenamePreview
	}

	private readonly struct IdeRequestStamp(long contextRevision, string projectRoot, string filePath, string source, int offset)
	{
		public readonly long ContextRevision = contextRevision;

		public readonly string ProjectRoot = projectRoot;

		public readonly string FilePath = filePath;

		public readonly string Source = source;

		public readonly int Offset = offset;
	}

	private readonly struct IdeSymbolQueryResult(bool isCurrent, IdeRequestStamp stamp, XWCSharpIdeService.SymbolInfoResult info)
	{
		public readonly bool IsCurrent = isCurrent;

		public readonly IdeRequestStamp Stamp = stamp;

		public readonly XWCSharpIdeService.SymbolInfoResult Info = info;
	}

	public const string SupportedScriptExtension = "cs";

	private MenuButton _fileMenuBtn;

	private MenuButton _searchMenuBtn;

	private MenuButton _goToMenuBtn;

	private MenuButton _overflowMenuBtn;

	private Label _scriptNameLabel;

	private LineEdit _scriptFilter;

	private ItemList _scriptList;

	private TabBar _scriptTabBar;

	private XWCodeEdit _codeEdit;

	private Label _lineColLabel;

	private Button _compileButton;

	private Button _validateButton;

	private Button _previousProblemButton;

	private Button _nextProblemButton;

	private Button _openBlueprintButton;

	private PanelContainer _generatedBlueprintBar;

	private Label _generatedBlueprintStatusLabel;

	private Label _generatedBlueprintSourceLabel;

	private Button _returnToBlueprintButton;

	private Label _diagnosticsLabel;

	private PanelContainer _diagnosticsPanel;

	private ItemList _diagnosticsList;

	private Button _diagnosticsCopyButton;

	private PopupMenu _diagnosticsContextMenu;

	private PanelContainer _searchBar;

	private LineEdit _searchInput;

	private Label _searchMatchLabel;

	private Button _searchPreviousButton;

	private Button _searchNextButton;

	private CheckButton _searchCaseButton;

	private CheckButton _searchWholeButton;

	private Button _searchCloseButton;

	private Container _replaceRow;

	private LineEdit _replaceInput;

	private Button _replaceNextButton;

	private Button _replaceAllButton;

	private ConfirmationDialog _goToLineDialog;

	private SpinBox _goToLineSpin;

	private FileDialog _newFileDialog;

	private FileDialog _openFileDialog;

	private FileDialog _saveAsDialog;

	private ConfirmationDialog _unsavedChangesDialog;

	private Button _discardUnsavedButton;

	private readonly Dictionary<string, ScriptTabInfo> _openTabs = new Dictionary<string, ScriptTabInfo>();

	private readonly List<string> _tabKeys = new List<string>();

	private readonly Queue<string> _pendingCloseKeys = new Queue<string>();

	private readonly List<DiagnosticListEntry> _diagnosticEntries = new List<DiagnosticListEntry>();

	private readonly List<SearchMatch> _searchMatches = new List<SearchMatch>();

	private string _currentTabKey = "";

	private bool _isSwitchingTab;

	private bool _isLoadingTab;

	private bool _isCompiling;

	private bool _isBulkReplacing;

	private int _activeSearchMatch = -1;

	private string _pendingCloseTabKey = "";

	private bool _hiddenWorkSuspended;

	private bool _workspaceSelected;

	private bool _workspaceActive;

	private string _activeProjectRoot = "";

	private SynchronizationContext _scriptEditorSynchronizationContext;

	private CancellationTokenSource _buildLifetimeCts = new CancellationTokenSource();

	private static readonly XWModAssemblyLoader CallbackPreviewLoader = new XWModAssemblyLoader();

	private static string _callbackPreviewOwnerId = "";

	private static string _callbackPreviewAssemblyPath = "";

	private Texture2D _iconScript;

	private Texture2D _iconSave;

	private Texture2D _iconNew;

	private Texture2D _iconFolder;

	private Texture2D _iconClose;

	private Texture2D _iconBuildCSharp;

	private Texture2D _iconStatusError;

	private Texture2D _iconStatusWarning;

	private readonly object _scriptDebugSnapshotLock = new object();

	private XWModDebugWorkbench _debugWorkbench;

	private Button _debugWorkbenchToggle;

	private XWModDebugSnapshot _pendingScriptDebugSnapshot;

	private int _scriptDebugNavigationQueued;

	private static readonly UTF8Encoding ScriptUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private PanelContainer _externalConflictBar;

	private Label _externalConflictTitleLabel;

	private Label _externalConflictDetailLabel;

	private Button _viewExternalDiffButton;

	private Button _reloadExternalButton;

	private Button _overwriteExternalButton;

	private Button _saveConflictCopyButton;

	private Window _externalConflictDiffWindow;

	private TextEdit _externalConflictEditorText;

	private TextEdit _externalConflictDiskText;

	private Godot.Timer _externalConflictWatcher;

	private bool _externalConflictWatcherScanning;

	private Button _definitionButton;

	private Button _referencesButton;

	private Button _renameButton;

	private Button _renameUndoButton;

	private Button _renameRedoButton;

	private Button _formatButton;

	private Button _quickFixButton;

	private Button _debugStartButton;

	private Button _debugStopButton;

	private Label _debugStatusLabel;

	private Window _referencesWindow;

	private Label _referencesHeader;

	private ItemList _referencesList;

	private ConfirmationDialog _renameDialog;

	private LineEdit _renameInput;

	private RichTextLabel _renamePreviewLabel;

	private PopupPanel _symbolInfoPopup;

	private RichTextLabel _symbolInfoLabel;

	private PopupMenu _quickFixPopup;

	private Godot.Timer _hoverTimer;

	private Godot.Timer _renamePreviewTimer;

	private readonly List<XWCSharpIdeService.LocationResult> _referenceLocations = new List<XWCSharpIdeService.LocationResult>();

	private XWScriptDebugHost _debugHost;

	private Node _debugPreviewRoot;

	private XWScriptDebugSession _debugSession;

	private XWCSharpIdeService.RenamePreview _pendingRename;

	private IdeRequestStamp? _pendingRenameStamp;

	private int _pendingRenameGeneration;

	private XWCodeErrorChecker.ErrorData _pendingQuickFixDiagnostic;

	private Vector2 _hoverLocalPosition;

	private int _hoverLine;

	private int _hoverColumn;

	private int _hoverGeneration;

	private int _renameGeneration;

	private string _pendingRenameText = "";

	private long _ideContextRevision;

	public XWScriptCompiler.CompileResult LastCompileResult { get; private set; }

	public string CallbackPreviewOwnerId => _callbackPreviewOwnerId;

	public int UnsupportedScriptRequestCount { get; private set; }

	public int UnsupportedSaveAsRequestCount { get; private set; }

	public int CSharpTemplateCreateCount { get; private set; }

	public bool IsHiddenWorkQuiescent
	{
		get
		{
			if (!_workspaceActive && _hiddenWorkSuspended)
			{
				return IsExternalConflictWatcherIdle;
			}
			return false;
		}
	}

	public bool IsCurrentDocumentReadOnly
	{
		get
		{
			if (TryGetCurrentTab(out var tab))
			{
				return tab.IsBlueprintGenerated;
			}
			return false;
		}
	}

	public string CurrentGeneratedBlueprintSourcePath
	{
		get
		{
			if (!TryGetCurrentTab(out var tab))
			{
				return "";
			}
			return tab.BlueprintSourcePath;
		}
	}

	public XWBlueprintGeneratedCSharpPolicy.LinkState CurrentGeneratedLinkState
	{
		get
		{
			if (!TryGetCurrentTab(out var tab))
			{
				return XWBlueprintGeneratedCSharpPolicy.LinkState.NotGenerated;
			}
			return tab.GeneratedLinkState;
		}
	}

	public string HiddenWorkDiagnostic => $"visible={IsVisibleInTree()} process={IsProcessing()} physics={IsPhysicsProcessing()} codePending={_codeEdit?.HasPendingBackgroundWork ?? false} idePending={HasPendingIdeBackgroundWork} externalWatcherIdle={IsExternalConflictWatcherIdle}";

	public bool IsDebugWorkbenchBound => _debugWorkbench?.IsControllerBound ?? false;

	public bool IsDebugWorkbenchVisible => _debugWorkbench?.Visible ?? false;

	public bool IsDebugWorkbenchHiddenProcessSuspended => _debugWorkbench?.IsHiddenProcessSuspended ?? true;

	public int DebugWorkbenchCallStackCount => _debugWorkbench?.CallStackItemCount ?? 0;

	public int DebugWorkbenchVariableCount => _debugWorkbench?.VariableItemCount ?? 0;

	public ScriptSaveStatus LastSaveStatus { get; private set; }

	public string LastSaveError { get; private set; } = "";

	public int ExternalConflictDetectedCount { get; private set; }

	public int ExternalConflictReloadCount { get; private set; }

	public int ExternalConflictOverwriteCount { get; private set; }

	public int CleanExternalReloadCount { get; private set; }

	public int AtomicSaveCount { get; private set; }

	public int SaveAsRollbackCount { get; private set; }

	public int CompileSaveGateCount { get; private set; }

	public int DebugSaveGateCount { get; private set; }

	public int CompileInvocationCount { get; private set; }

	public int DebugStartInvocationCount { get; private set; }

	public int ExternalConflictWatcherScanCount { get; private set; }

	public bool IsCurrentDocumentInExternalConflict
	{
		get
		{
			if (TryGetCurrentTab(out var tab))
			{
				return tab.HasExternalConflict;
			}
			return false;
		}
	}

	public bool IsExternalConflictBarVisible
	{
		get
		{
			if (GodotObject.IsInstanceValid(_externalConflictBar))
			{
				return _externalConflictBar.Visible;
			}
			return false;
		}
	}

	public bool IsExternalConflictWatcherIdle
	{
		get
		{
			if (GodotObject.IsInstanceValid(_externalConflictWatcher))
			{
				if (_externalConflictWatcher.IsStopped() && _externalConflictWatcher.ProcessMode == ProcessModeEnum.Disabled)
				{
					return !_externalConflictWatcherScanning;
				}
				return false;
			}
			return true;
		}
	}

	public string CurrentDocumentText => _codeEdit?.Text ?? "";

	internal XWCSharpIdeService.SymbolInfoResult LastSymbolInfo { get; private set; }

	internal XWCSharpIdeService.RenamePreview LastRenamePreview => _pendingRename;

	internal Func<IdeRequestKind, string, Task> IdeResultGateForProbeAsync { get; set; }

	public XWScriptDebugSession.StartResult LastDebugStartResult { get; private set; }

	public bool IsDebugSessionRunning => _debugSession?.IsRunning ?? false;

	public bool HasLiveDebugSession => _debugSession?.HasLiveSession ?? false;

	public bool DebugEntryInvoked => _debugSession?.EntryPointInvoked ?? false;

	public int DebugLoadedAssemblyCount => _debugSession?.LoadedAssemblyCount ?? 0;

	public bool DebugEntryTaskCompleted => _debugSession?.EntryTaskCompleted ?? true;

	public bool DebugLoadContextAlive
	{
		get
		{
			XWScriptDebugSession debugSession = _debugSession;
			if (debugSession == null)
			{
				return false;
			}
			return debugSession.LastLoadContextWeakReference?.IsAlive == true;
		}
	}

	public int DebugPreviewChildCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_debugPreviewRoot))
			{
				return 0;
			}
			return _debugPreviewRoot.GetChildCount();
		}
	}

	public int DebugMainThreadId => _debugHost?.MainThreadId ?? 0;

	private bool HasPendingIdeBackgroundWork
	{
		get
		{
			Godot.Timer hoverTimer = _hoverTimer;
			if (hoverTimer == null || hoverTimer.ProcessMode != ProcessModeEnum.Disabled)
			{
				Godot.Timer hoverTimer2 = _hoverTimer;
				if (hoverTimer2 != null && !hoverTimer2.IsStopped())
				{
					return true;
				}
			}
			Godot.Timer renamePreviewTimer = _renamePreviewTimer;
			if (renamePreviewTimer == null || renamePreviewTimer.ProcessMode != ProcessModeEnum.Disabled)
			{
				Godot.Timer renamePreviewTimer2 = _renamePreviewTimer;
				if (renamePreviewTimer2 == null)
				{
					return false;
				}
				return !renamePreviewTimer2.IsStopped();
			}
			return false;
		}
	}

	public override void _Ready()
	{
		_scriptEditorSynchronizationContext = SynchronizationContext.Current;
		XWModBuildSingleFlight.FlightStateChanged += OnModBuildFlightStateChanged;
		_fileMenuBtn = GetNode<MenuButton>("%FileMenuBtn");
		_searchMenuBtn = GetNode<MenuButton>("%SearchMenuBtn");
		_goToMenuBtn = GetNode<MenuButton>("%GoToMenuBtn");
		_overflowMenuBtn = GetNode<MenuButton>("%OverflowMenuBtn");
		_scriptNameLabel = GetNode<Label>("%ScriptNameLabel");
		_scriptFilter = GetNode<LineEdit>("%ScriptFilter");
		_scriptList = GetNode<ItemList>("%ScriptList");
		_scriptTabBar = GetNode<TabBar>("%ScriptTabBar");
		_codeEdit = GetNode<XWCodeEdit>("%XWCodeEdit");
		_lineColLabel = GetNode<Label>("%LineColLabel");
		_compileButton = GetNode<Button>("%CompileButton");
		_validateButton = GetNode<Button>("%ValidateButton");
		_previousProblemButton = GetNode<Button>("%PreviousProblemButton");
		_nextProblemButton = GetNode<Button>("%NextProblemButton");
		_openBlueprintButton = GetNode<Button>("%OpenBlueprintButton");
		_generatedBlueprintBar = GetNode<PanelContainer>("%GeneratedBlueprintBar");
		_generatedBlueprintStatusLabel = GetNode<Label>("%GeneratedBlueprintStatusLabel");
		_generatedBlueprintSourceLabel = GetNode<Label>("%GeneratedBlueprintSourceLabel");
		_returnToBlueprintButton = GetNode<Button>("%ReturnToBlueprintButton");
		_diagnosticsLabel = GetNode<Label>("%DiagnosticsLabel");
		_diagnosticsPanel = GetNode<PanelContainer>("%DiagnosticsPanel");
		_diagnosticsList = GetNode<ItemList>("%DiagnosticsList");
		_diagnosticsCopyButton = GetNodeOrNull<Button>("%DiagnosticsCopyButton");
		_diagnosticsContextMenu = GetNode<PopupMenu>("%DiagnosticsContextMenu");
		_searchBar = GetNode<PanelContainer>("%SearchBar");
		_searchInput = GetNode<LineEdit>("%SearchInput");
		_searchMatchLabel = GetNode<Label>("%SearchMatchLabel");
		_searchPreviousButton = GetNode<Button>("%SearchPreviousButton");
		_searchNextButton = GetNode<Button>("%SearchNextButton");
		_searchCaseButton = GetNode<CheckButton>("%SearchCaseButton");
		_searchWholeButton = GetNode<CheckButton>("%SearchWholeButton");
		_searchCloseButton = GetNode<Button>("%SearchCloseButton");
		_replaceRow = GetNode<Container>("%ReplaceRow");
		_replaceInput = GetNode<LineEdit>("%ReplaceInput");
		_replaceNextButton = GetNode<Button>("%ReplaceNextButton");
		_replaceAllButton = GetNode<Button>("%ReplaceAllButton");
		_goToLineDialog = GetNode<ConfirmationDialog>("%GoToLineDialog");
		_goToLineSpin = GetNode<SpinBox>("%GoToLineSpin");
		_newFileDialog = GetNode<FileDialog>("%NewFileDialog");
		_openFileDialog = GetNode<FileDialog>("%OpenFileDialog");
		_saveAsDialog = GetNode<FileDialog>("%SaveAsDialog");
		_unsavedChangesDialog = GetNode<ConfirmationDialog>("%UnsavedChangesDialog");
		_discardUnsavedButton = _unsavedChangesDialog.AddButton("不保存", right: true);
		_discardUnsavedButton.Name = "DiscardUnsavedButton";
		_discardUnsavedButton.UniqueNameInOwner = true;
		_discardUnsavedButton.Text = "不保存";
		InitializeExternalConflictUI();
		LoadIcons();
		BuildFileMenu();
		BuildSearchMenu();
		BuildGoToMenu();
		BuildToolbarOverflowMenu();
		BuildDiagnosticsContextMenu();
		ConnectSignals();
		UpdateDiagnosticList(new List<XWCodeErrorChecker.ErrorData>(), "", includeFileName: false);
		_searchBar.Hide();
		_searchMatches.Clear();
		_activeSearchMatch = -1;
		_searchMatchLabel.Text = "0 / 0";
		UpdateCompileControls();
		InitializeIdeFeatures();
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		base.VisibilityChanged += OnEditorVisibilityChanged;
		OnEditorVisibilityChanged();
	}

	private void LoadIcons()
	{
		XWClassRegistry instance = XWClassRegistry.Instance;
		_iconScript = instance.GetUIIcon("Script");
		_iconSave = instance.GetUIIcon("Save");
		_iconNew = instance.GetUIIcon("Add");
		_iconFolder = instance.GetUIIcon("Folder");
		_iconClose = instance.GetUIIcon("Close");
		_iconBuildCSharp = instance.GetUIIcon("BuildCSharp");
		_iconStatusError = instance.GetUIIcon("StatusError");
		_iconStatusWarning = instance.GetUIIcon("StatusWarning");
		_compileButton.Icon = _iconBuildCSharp;
		_validateButton.Icon = instance.GetUIIcon("Search");
		_previousProblemButton.Icon = instance.GetUIIcon("ArrowUp");
		_nextProblemButton.Icon = instance.GetUIIcon("ArrowDown");
		_openBlueprintButton.Icon = instance.GetUIIcon("GraphEdit");
		_overflowMenuBtn.Icon = instance.GetUIIcon("GuiTabMenuHl");
		Button nodeOrNull = GetNodeOrNull<Button>("%HistoryBackBtn");
		if (nodeOrNull != null)
		{
			nodeOrNull.Icon = instance.GetUIIcon("Back");
		}
		Button nodeOrNull2 = GetNodeOrNull<Button>("%HistoryForwardBtn");
		if (nodeOrNull2 != null)
		{
			nodeOrNull2.Icon = instance.GetUIIcon("Forward");
		}
		LineEdit nodeOrNull3 = GetNodeOrNull<LineEdit>("%ScriptFilter");
		if (nodeOrNull3 != null)
		{
			nodeOrNull3.RightIcon = instance.GetUIIcon("Search");
		}
	}

	private void BuildFileMenu()
	{
		PopupMenu popup = _fileMenuBtn.GetPopup();
		popup.Clear();
		popup.AddIconItem(_iconNew, "新建 C# 脚本", 0, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(_iconFolder, "打开...", 3, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(_iconSave, "保存", 5, Key.None);
		popup.AddIconItem(_iconSave, "另存为...", 6, Key.None);
		popup.AddIconItem(_iconSave, "全部保存", 7, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(_iconClose, "关闭", 9, Key.None);
		popup.AddItem("关闭全部", 10, Key.None);
		popup.AddItem("关闭其他", 11, Key.None);
		popup.IdPressed += OnFileMenuItem;
	}

	private void BuildSearchMenu()
	{
		PopupMenu popup = _searchMenuBtn.GetPopup();
		popup.Clear();
		popup.AddItem("查找（Ctrl+F）", 0, Key.None);
		popup.AddItem("查找并替换（Ctrl+H）", 1, Key.None);
		popup.AddSeparator();
		popup.AddItem("下一个匹配（Enter）", 2, Key.None);
		popup.AddItem("上一个匹配（Shift+Enter）", 3, Key.None);
		popup.IdPressed += OnSearchMenuItem;
	}

	private void BuildGoToMenu()
	{
		PopupMenu popup = _goToMenuBtn.GetPopup();
		popup.Clear();
		popup.AddItem("跳转到行（Ctrl+G）", 0, Key.None);
		popup.AddSeparator();
		popup.AddItem("下一个问题（F8）", 1, Key.None);
		popup.AddItem("上一个问题（Shift+F8）", 2, Key.None);
		popup.IdPressed += OnGoToMenuItem;
	}

	private void BuildToolbarOverflowMenu()
	{
		PopupMenu popup = _overflowMenuBtn.GetPopup();
		popup.Clear();
		XWClassRegistry instance = XWClassRegistry.Instance;
		popup.AddIconItem(instance.GetUIIcon("Back"), "撤销项目重命名", 0, Key.None);
		popup.AddIconItem(instance.GetUIIcon("Forward"), "重做项目重命名", 1, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(instance.GetUIIcon("CombineLines"), "格式化文档或选区", 3, Key.None);
		popup.AddIconItem(instance.GetUIIcon("Search"), "检查当前脚本", 4, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(instance.GetUIIcon("ArrowUp"), "上一个问题", 6, Key.None);
		popup.AddIconItem(instance.GetUIIcon("ArrowDown"), "下一个问题", 7, Key.None);
		popup.AddSeparator();
		popup.AddIconItem(instance.GetUIIcon("GraphEdit"), "打开 API 拼图", 9, Key.None);
		popup.IdPressed += OnToolbarOverflowItem;
	}

	private void OnToolbarOverflowItem(long id)
	{
		switch ((ToolbarOverflowAction)id)
		{
		case ToolbarOverflowAction.RenameUndo:
			UndoLastProjectRenameAsync();
			break;
		case ToolbarOverflowAction.RenameRedo:
			RedoLastProjectRenameAsync();
			break;
		case ToolbarOverflowAction.Format:
			FormatDocumentOrSelection();
			break;
		case ToolbarOverflowAction.Validate:
			RequestCurrentValidation();
			break;
		case ToolbarOverflowAction.PreviousProblem:
			JumpToNextDiagnostic(-1);
			break;
		case ToolbarOverflowAction.NextProblem:
			JumpToNextDiagnostic(1);
			break;
		case ToolbarOverflowAction.OpenBlueprint:
			OpenBlueprintWorkspace();
			break;
		case (ToolbarOverflowAction)2:
		case (ToolbarOverflowAction)5:
		case (ToolbarOverflowAction)8:
			break;
		}
	}

	private void OnEditorVisibilityChanged()
	{
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		ApplyWorkspaceActivity();
	}

	public void SetWorkspaceActive(bool active)
	{
		_workspaceSelected = active;
		ApplyWorkspaceActivity();
	}

	private void ApplyWorkspaceActivity()
	{
		if (_workspaceActive = _workspaceSelected && IsVisibleInTree())
		{
			_hiddenWorkSuspended = false;
			_codeEdit?.ResumeBackgroundWork();
			ResumeIdeBackgroundWork();
			SetExternalConflictWatcherActive(active: true);
		}
		else
		{
			SetExternalConflictWatcherActive(active: false);
			_codeEdit?.SuspendBackgroundWork();
			SuspendIdeBackgroundWork();
			_hiddenWorkSuspended = true;
		}
	}

	private void OnSearchMenuItem(long id)
	{
		switch ((SearchMenu)id)
		{
		case SearchMenu.Find:
			ShowSearchBar(showReplace: false);
			break;
		case SearchMenu.Replace:
			ShowSearchBar(showReplace: true);
			break;
		case SearchMenu.Next:
			NavigateSearch(1);
			break;
		case SearchMenu.Previous:
			NavigateSearch(-1);
			break;
		}
	}

	private void OnGoToMenuItem(long id)
	{
		switch ((GoToMenu)id)
		{
		case GoToMenu.Line:
			ShowGoToLineDialog();
			break;
		case GoToMenu.NextProblem:
			JumpToNextDiagnostic(1);
			break;
		case GoToMenu.PreviousProblem:
			JumpToNextDiagnostic(-1);
			break;
		}
	}

	private void BuildDiagnosticsContextMenu()
	{
		_diagnosticsContextMenu.Clear();
		_diagnosticsContextMenu.AddItem("复制所选", 0, Key.None);
		_diagnosticsContextMenu.AddItem("复制全部", 1, Key.None);
		_diagnosticsContextMenu.IdPressed += OnDiagnosticsContextMenuItem;
	}

	private void OnDiagnosticsContextMenuItem(long id)
	{
		switch ((DiagnosticsContextMenu)id)
		{
		case DiagnosticsContextMenu.CopySelected:
			CopySelectedDiagnostics();
			break;
		case DiagnosticsContextMenu.CopyAll:
			CopyAllDiagnostics();
			break;
		}
	}

	private void ConnectSignals()
	{
		_codeEdit.TextChanged += OnCodeChanged;
		_codeEdit.CaretChanged += UpdateLineCol;
		_scriptFilter.TextChanged += OnScriptFilterChanged;
		_scriptList.ItemSelected += OnScriptListItemSelected;
		_scriptTabBar.TabChanged += OnTabChanged;
		_scriptTabBar.TabClosePressed += OnTabClosePressed;
		_compileButton.Pressed += OnCompileButtonPressed;
		_validateButton.Pressed += RequestCurrentValidation;
		_previousProblemButton.Pressed += delegate
		{
			JumpToNextDiagnostic(-1);
		};
		_nextProblemButton.Pressed += delegate
		{
			JumpToNextDiagnostic(1);
		};
		_openBlueprintButton.Pressed += OpenBlueprintWorkspace;
		_returnToBlueprintButton.Pressed += delegate
		{
			OpenCurrentGeneratedBlueprintSource();
		};
		_codeEdit.ErrorsChanged += OnCodeErrorsChanged;
		_codeEdit.DiagnosticsChanged += OnCodeDiagnosticsChanged;
		_diagnosticsList.ItemSelected += OnDiagnosticListItemSelected;
		_diagnosticsList.ItemActivated += OnDiagnosticListItemActivated;
		_diagnosticsList.GuiInput += OnDiagnosticsListGuiInput;
		if (_diagnosticsCopyButton != null)
		{
			_diagnosticsCopyButton.Pressed += CopySelectedDiagnostics;
		}
		_newFileDialog.FileSelected += OnNewFileSelected;
		_openFileDialog.FileSelected += OpenFile;
		_saveAsDialog.FileSelected += OnSaveAsFileSelected;
		_unsavedChangesDialog.Confirmed += OnUnsavedCloseConfirmed;
		_unsavedChangesDialog.Canceled += OnUnsavedCloseCanceled;
		_discardUnsavedButton.Pressed += delegate
		{
			OnUnsavedCloseCustomAction("discard");
		};
		_searchInput.TextChanged += delegate
		{
			RefreshSearchMatches(selectNearest: true);
		};
		_searchInput.TextSubmitted += delegate
		{
			NavigateSearch(1);
		};
		_searchPreviousButton.Pressed += delegate
		{
			NavigateSearch(-1);
		};
		_searchNextButton.Pressed += delegate
		{
			NavigateSearch(1);
		};
		_searchCaseButton.Toggled += delegate
		{
			RefreshSearchMatches(selectNearest: true);
		};
		_searchWholeButton.Toggled += delegate
		{
			RefreshSearchMatches(selectNearest: true);
		};
		_searchCloseButton.Pressed += CloseSearchBar;
		_replaceInput.TextSubmitted += delegate
		{
			ReplaceCurrentMatch();
		};
		_replaceNextButton.Pressed += ReplaceCurrentMatch;
		_replaceAllButton.Pressed += delegate
		{
			ReplaceAllMatches();
		};
		_goToLineDialog.Confirmed += CommitGoToLine;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_workspaceActive || !(@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey))
		{
			return;
		}
		if (inputEventKey.Keycode == Key.B && inputEventKey.CtrlPressed && !inputEventKey.ShiftPressed)
		{
			Button compileButton = _compileButton;
			if (compileButton == null || !compileButton.Disabled)
			{
				OnCompileButtonPressed();
			}
			GetViewport().SetInputAsHandled();
		}
		else if (inputEventKey.Keycode == Key.F5 && inputEventKey.ShiftPressed)
		{
			Button debugStopButton = _debugStopButton;
			if (debugStopButton == null || !debugStopButton.Disabled)
			{
				StopDebugSessionAsync();
			}
			GetViewport().SetInputAsHandled();
		}
		else if (inputEventKey.Keycode == Key.F5)
		{
			Button debugStartButton = _debugStartButton;
			if (debugStartButton == null || !debugStartButton.Disabled)
			{
				StartDebugSessionAsync();
			}
			GetViewport().SetInputAsHandled();
		}
		else if (inputEventKey.Keycode == Key.O && inputEventKey.CtrlPressed && !inputEventKey.ShiftPressed)
		{
			if (!string.IsNullOrWhiteSpace(GetActiveModProjectRoot()))
			{
				OpenFileDialog();
			}
			GetViewport().SetInputAsHandled();
		}
		else if (HasFileOpen())
		{
			if (HandleIdeShortcut(inputEventKey))
			{
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.F && inputEventKey.CtrlPressed)
			{
				ShowSearchBar(showReplace: false);
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.H && inputEventKey.CtrlPressed)
			{
				ShowSearchBar(showReplace: true);
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.G && inputEventKey.CtrlPressed)
			{
				ShowGoToLineDialog();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.Escape && GodotObject.IsInstanceValid(_searchBar) && _searchBar.Visible)
			{
				CloseSearchBar();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.S && inputEventKey.CtrlPressed && !inputEventKey.ShiftPressed)
			{
				SaveFile();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.S && inputEventKey.CtrlPressed && inputEventKey.ShiftPressed)
			{
				SaveFileAs();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.W && inputEventKey.CtrlPressed)
			{
				CloseCurrentTab();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.F8 && inputEventKey.ShiftPressed)
			{
				JumpToNextDiagnostic(-1);
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.F8)
			{
				JumpToNextDiagnostic(1);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void OnFileMenuItem(long id)
	{
		switch ((MenuFile)id)
		{
		case MenuFile.NewScript:
			NewCSharpScript();
			break;
		case MenuFile.Open:
			OpenFileDialog();
			break;
		case MenuFile.Save:
			SaveFile();
			break;
		case MenuFile.SaveAs:
			SaveFileAs();
			break;
		case MenuFile.SaveAll:
			SaveAllTabs(showToast: true);
			break;
		case MenuFile.Close:
			CloseCurrentTab();
			break;
		case MenuFile.CloseAll:
			CloseAllTabs();
			break;
		case MenuFile.CloseOther:
			CloseOtherTabs();
			break;
		case MenuFile.Sep1:
		case (MenuFile)2:
		case MenuFile.Sep2:
		case MenuFile.Sep3:
			break;
		}
	}

	private void NewCSharpScript()
	{
		if (XWEditorInterface.Instance?.GetFileSystemPanel() is XWFileSystemPanel xWFileSystemPanel && GodotObject.IsInstanceValid(xWFileSystemPanel))
		{
			XWEditorInterface.Instance.FocusPanel("filesystem");
			xWFileSystemPanel.ShowNewCSharpScriptDialog();
		}
		else
		{
			_newFileDialog.CurrentFile = "NewModScript.cs";
			_newFileDialog.PopupCentered();
		}
	}

	private void OnNewFileSelected(string path)
	{
		path = NormalizeNewCSharpPath(path);
		if (!IsSupportedScriptPath(path))
		{
			RejectUnsupportedScriptPath(path, "新建");
			return;
		}
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法创建 C# 脚本: " + path, 2);
			return;
		}
		fileAccess.StoreString(BuildDefaultCSharpTemplate(path));
		CSharpTemplateCreateCount++;
		XWBPCSharpMemberRegistry.Instance.NotifyScriptSaved(path);
		OpenFile(path);
	}

	private void OpenFileDialog()
	{
		_openFileDialog.PopupCentered();
	}

	public void OpenFile(string filePath)
	{
		TryOpenFile(filePath);
	}

	public bool TryOpenFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return false;
		}
		filePath = ResolveOpenFilePath(filePath);
		if (!IsSupportedScriptPath(filePath))
		{
			RejectUnsupportedScriptPath(filePath, "打开");
			return false;
		}
		if (_openTabs.TryGetValue(filePath, out var value))
		{
			if (value.IsBlueprintGenerated)
			{
				ReloadTabFromDisk(value);
			}
			else
			{
				RefreshTabExternalState(value, reloadVisible: false);
			}
			int num = _tabKeys.IndexOf(filePath);
			if (num >= 0 && _scriptTabBar.CurrentTab != num)
			{
				_isSwitchingTab = true;
				_scriptTabBar.CurrentTab = num;
				_isSwitchingTab = false;
			}
			LoadTabData(filePath);
			_codeEdit.GrabFocus();
			return true;
		}
		SaveCurrentTabState();
		ScriptTabInfo scriptTabInfo = new ScriptTabInfo
		{
			FilePath = filePath,
			FileExtension = filePath.GetExtension()
		};
		if (TryReadDiskSnapshot(filePath, out var snapshot, out var error))
		{
			scriptTabInfo.Text = snapshot.Text;
			ApplyDiskBaseline(scriptTabInfo, snapshot);
		}
		else
		{
			LastSaveError = error;
		}
		RefreshGeneratedMetadata(scriptTabInfo);
		_openTabs[filePath] = scriptTabInfo;
		_tabKeys.Add(filePath);
		string baseName = filePath.GetFile().GetBaseName();
		_isSwitchingTab = true;
		_scriptTabBar.AddTab(baseName, _iconScript);
		_scriptTabBar.CurrentTab = _tabKeys.Count - 1;
		_isSwitchingTab = false;
		LoadTabData(filePath);
		UpdateScriptList();
		_codeEdit.GrabFocus();
		return true;
	}

	public void OpenFileAt(string filePath, int line, int column)
	{
		TryOpenFileAt(filePath, line, column);
	}

	public bool OpenGeneratedBlueprintFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return false;
		}
		string text = ResolveOpenFilePath(filePath);
		if (_openTabs.TryGetValue(text, out var value))
		{
			ReloadTabFromDisk(value);
			LoadTabData(text);
			UpdateScriptList();
			return value.IsBlueprintGenerated;
		}
		if (!TryOpenFile(text))
		{
			return false;
		}
		return IsCurrentDocumentReadOnly;
	}

	public bool TryOpenFileAt(string filePath, int line, int column)
	{
		if (!TryOpenFile(filePath))
		{
			return false;
		}
		if (_codeEdit == null)
		{
			return false;
		}
		if (line >= 0)
		{
			_codeEdit.SetCaretLine(line);
		}
		if (column >= 0)
		{
			_codeEdit.SetCaretColumn(column);
		}
		_codeEdit.GrabFocus();
		return true;
	}

	private void SaveCurrentTabState()
	{
		if (!string.IsNullOrEmpty(_currentTabKey) && _openTabs.TryGetValue(_currentTabKey, out var value))
		{
			value.Text = _codeEdit.Text;
			value.CursorLine = _codeEdit.GetCaretLine();
			value.CursorColumn = _codeEdit.GetCaretColumn();
			value.ScrollVertical = (int)_codeEdit.ScrollVertical;
			value.Breakpoints = _codeEdit.GetBreakpointLines();
		}
	}

	private void LoadTabData(string tabKey)
	{
		if (_openTabs.TryGetValue(tabKey, out var value))
		{
			_scriptNameLabel.Text = value.FilePath.GetFile();
			_isLoadingTab = true;
			try
			{
				_codeEdit.SetDocumentPath(value.FilePath);
				_codeEdit.SetProjectRoot(GetActiveModProjectRoot());
				_codeEdit.LoadDocumentText(value.Text);
				_codeEdit.SetFileExtension(value.FileExtension);
				_codeEdit.RestoreBreakpointLines(value.Breakpoints);
				_currentTabKey = tabKey;
				_codeEdit.Editable = !value.IsBlueprintGenerated;
				_codeEdit.SetCaretLine(value.CursorLine);
				_codeEdit.SetCaretColumn(value.CursorColumn);
				_codeEdit.ScrollVertical = value.ScrollVertical;
			}
			finally
			{
				_isLoadingTab = false;
			}
			if (_workspaceActive)
			{
				_codeEdit.ResumeBackgroundWork();
			}
			if (GodotObject.IsInstanceValid(_searchBar) && _searchBar.Visible)
			{
				RefreshSearchMatches(selectNearest: true);
			}
			UpdateTabTitle();
			UpdateLineCol();
			UpdateGeneratedBlueprintBar(value);
			UpdateExternalConflictSurface(value);
			_codeEdit.RequestValidationNow();
			UpdateCompileControls();
		}
	}

	private void OnTabChanged(long tabIdx)
	{
		if (_isSwitchingTab || tabIdx < 0 || tabIdx >= _tabKeys.Count)
		{
			return;
		}
		string text = _tabKeys[(int)tabIdx];
		if (!(text == _currentTabKey))
		{
			SaveCurrentTabState();
			if (_openTabs.TryGetValue(text, out var value) && !value.IsBlueprintGenerated)
			{
				RefreshTabExternalState(value, reloadVisible: false);
			}
			LoadTabData(text);
			UpdateScriptListSelection();
		}
	}

	private void OnTabClosePressed(long tabIdx)
	{
		if (tabIdx >= 0 && tabIdx < _tabKeys.Count)
		{
			CloseTab(_tabKeys[(int)tabIdx]);
		}
	}

	public void CloseTab(string tabKey)
	{
		BeginCloseTabs(new string[1] { tabKey });
	}

	private void BeginCloseTabs(IEnumerable<string> tabKeys)
	{
		_pendingCloseKeys.Clear();
		_pendingCloseTabKey = "";
		foreach (string tabKey in tabKeys)
		{
			if (_openTabs.ContainsKey(tabKey))
			{
				_pendingCloseKeys.Enqueue(tabKey);
			}
		}
		ContinueCloseQueue();
	}

	private void ContinueCloseQueue()
	{
		while (_pendingCloseKeys.Count > 0)
		{
			string text = _pendingCloseKeys.Dequeue();
			if (_openTabs.TryGetValue(text, out var value))
			{
				if (value.HasUnsavedChanges || value.HasExternalConflict)
				{
					_pendingCloseTabKey = text;
					_unsavedChangesDialog.Title = "未保存的修改";
					_unsavedChangesDialog.DialogText = (value.HasExternalConflict ? (value.FilePath.GetFile() + " 同时存在编辑器缓冲区和外部磁盘版本。\n保存会保持冲突并停止关闭；请先在编辑器中选择重新载入或覆盖磁盘。") : (value.FilePath.GetFile() + " 仍有未保存的修改。\n请选择保存、不保存或取消关闭。"));
					_unsavedChangesDialog.PopupCentered();
					return;
				}
				CloseTabImmediately(text);
			}
		}
		_pendingCloseTabKey = "";
	}

	private void OnUnsavedCloseConfirmed()
	{
		string pendingCloseTabKey = _pendingCloseTabKey;
		_pendingCloseTabKey = "";
		if (string.IsNullOrEmpty(pendingCloseTabKey) || !_openTabs.ContainsKey(pendingCloseTabKey))
		{
			ContinueCloseQueue();
			return;
		}
		if (!SaveTab(pendingCloseTabKey))
		{
			_pendingCloseKeys.Clear();
			return;
		}
		CloseTabImmediately(pendingCloseTabKey);
		CallDeferred("ContinueCloseQueue");
	}

	private void OnUnsavedCloseCanceled()
	{
		_pendingCloseTabKey = "";
		_pendingCloseKeys.Clear();
	}

	private void OnUnsavedCloseCustomAction(StringName action)
	{
		if (!(action != (StringName)"discard"))
		{
			string pendingCloseTabKey = _pendingCloseTabKey;
			_pendingCloseTabKey = "";
			_unsavedChangesDialog.Hide();
			if (!string.IsNullOrEmpty(pendingCloseTabKey) && _openTabs.TryGetValue(pendingCloseTabKey, out var value))
			{
				value.HasUnsavedChanges = false;
				CloseTabImmediately(pendingCloseTabKey);
			}
			CallDeferred("ContinueCloseQueue");
		}
	}

	private void CloseTabImmediately(string tabKey)
	{
		if (!_openTabs.ContainsKey(tabKey))
		{
			return;
		}
		bool flag = tabKey == _currentTabKey;
		int num = _tabKeys.IndexOf(tabKey);
		_openTabs.Remove(tabKey);
		_tabKeys.RemoveAt(num);
		_scriptTabBar.RemoveTab(num);
		if (_tabKeys.Count == 0)
		{
			ClearEditor();
			return;
		}
		if (flag)
		{
			int num2 = Mathf.Min(num, _tabKeys.Count - 1);
			_isSwitchingTab = true;
			_scriptTabBar.CurrentTab = num2;
			_isSwitchingTab = false;
			LoadTabData(_tabKeys[num2]);
		}
		UpdateScriptList();
	}

	public void CloseCurrentTab()
	{
		if (!string.IsNullOrEmpty(_currentTabKey))
		{
			CloseTab(_currentTabKey);
		}
	}

	public void CloseAllTabs()
	{
		BeginCloseTabs(new List<string>(_tabKeys));
	}

	public bool TrySwitchProjectRoot(string projectRoot)
	{
		string text = NormalizeProjectRoot(projectRoot);
		if (string.Equals(_activeProjectRoot, text, StringComparison.OrdinalIgnoreCase) && string.Equals(NormalizeProjectRoot(_codeEdit?.GetProjectRoot()), text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (XWScriptCompiler.IsModBuildBusy(NormalizeProjectRoot(_activeProjectRoot)))
		{
			RenewBuildLifetime(cancelCurrentBuild: true);
			return false;
		}
		XWScriptDebugSession debugSession = _debugSession;
		if (debugSession != null && debugSession.HasLiveSession)
		{
			return false;
		}
		SaveCurrentTabState();
		foreach (ScriptTabInfo value in _openTabs.Values)
		{
			if (value.HasUnsavedChanges || value.HasExternalConflict)
			{
				return false;
			}
		}
		if (!UnloadCallbackPreview(reportFailure: true))
		{
			return false;
		}
		SetWorkspaceActive(active: false);
		_pendingCloseKeys.Clear();
		_pendingCloseTabKey = "";
		_openTabs.Clear();
		_tabKeys.Clear();
		ClearEditor();
		_codeEdit.SetDocumentPath("");
		_codeEdit.SetProjectRoot(text);
		_activeProjectRoot = text;
		RenewBuildLifetime(cancelCurrentBuild: true);
		UpdateCompileControls();
		UpdateDebugControls();
		return true;
	}

	private void RenewBuildLifetime(bool cancelCurrentBuild)
	{
		CancellationTokenSource buildLifetimeCts = _buildLifetimeCts;
		_buildLifetimeCts = new CancellationTokenSource();
		if (buildLifetimeCts == null)
		{
			return;
		}
		if (cancelCurrentBuild)
		{
			try
			{
				buildLifetimeCts.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
		}
		buildLifetimeCts.Dispose();
	}

	private static string NormalizeProjectRoot(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		try
		{
			return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return path.TrimEnd('/', '\\');
		}
	}

	public void CloseOtherTabs()
	{
		string currentTabKey = _currentTabKey;
		List<string> list = new List<string>();
		foreach (string tabKey in _tabKeys)
		{
			if (tabKey != currentTabKey)
			{
				list.Add(tabKey);
			}
		}
		BeginCloseTabs(list);
	}

	private void ClearEditor()
	{
		_codeEdit.LoadDocumentText("");
		_codeEdit.Editable = true;
		_codeEdit.ClearAllErrors();
		_currentTabKey = "";
		_scriptNameLabel.Text = "未打开文件";
		_scriptTabBar.ClearTabs();
		_scriptList.Clear();
		_diagnosticsLabel.Text = "0 错误，0 警告";
		UpdateDiagnosticList(new List<XWCodeErrorChecker.ErrorData>(), "", includeFileName: false);
		_searchBar.Hide();
		_searchMatches.Clear();
		_activeSearchMatch = -1;
		_searchMatchLabel.Text = "0 / 0";
		if (GodotObject.IsInstanceValid(_generatedBlueprintBar))
		{
			_generatedBlueprintBar.Hide();
		}
		if (GodotObject.IsInstanceValid(_externalConflictBar))
		{
			_externalConflictBar.Hide();
		}
		if (GodotObject.IsInstanceValid(_externalConflictDiffWindow))
		{
			_externalConflictDiffWindow.Hide();
		}
		UpdateCompileControls();
	}

	public bool SaveFile()
	{
		if (string.IsNullOrEmpty(_currentTabKey))
		{
			return false;
		}
		return SaveTab(_currentTabKey);
	}

	private bool SaveTab(string tabKey)
	{
		if (_openTabs.TryGetValue(tabKey, out var value) && value.IsBlueprintGenerated)
		{
			SaveTabWithResult(tabKey, overwriteExternalConflict: false);
			return false;
		}
		ScriptSaveStatus scriptSaveStatus = SaveTabWithResult(tabKey, overwriteExternalConflict: false);
		if ((uint)scriptSaveStatus <= 1u)
		{
			return true;
		}
		return false;
	}

	public void SaveFileAs()
	{
		if (!string.IsNullOrEmpty(_currentTabKey) && CanEditCurrentDocument(showToast: true))
		{
			_saveAsDialog.CurrentPath = _currentTabKey;
			_saveAsDialog.PopupCentered();
		}
	}

	private void OnSaveAsFileSelected(string path)
	{
		path = NormalizeNewCSharpPath(path);
		if (!IsSupportedScriptPath(path))
		{
			UnsupportedSaveAsRequestCount++;
			RejectUnsupportedScriptPath(path, "另存为", countRequest: false);
		}
		else
		{
			SaveCurrentTabAsAtomic(path);
		}
	}

	public int SaveAllTabs(bool showToast = false)
	{
		SaveCurrentTabState();
		int num = 0;
		int num2 = 0;
		foreach (string tabKey in _tabKeys)
		{
			if (_openTabs.TryGetValue(tabKey, out var value) && !value.IsBlueprintGenerated)
			{
				ScriptSaveStatus scriptSaveStatus = RefreshTabExternalState(value, tabKey == _currentTabKey);
				if ((uint)(scriptSaveStatus - 2) <= 2u)
				{
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			if (showToast)
			{
				XWEditorInterface.Instance?.ShowToast($"C# 脚本保存已停止：{num2} 个冲突或读取失败。", 1);
			}
			return -num2;
		}
		foreach (string tabKey2 in _tabKeys)
		{
			if (_openTabs.TryGetValue(tabKey2, out var value2) && !value2.IsBlueprintGenerated)
			{
				bool flag;
				switch (value2.HasUnsavedChanges ? SaveTabWithResult(tabKey2, overwriteExternalConflict: false) : ScriptSaveStatus.Unchanged)
				{
				case ScriptSaveStatus.Saved:
					num++;
					continue;
				case ScriptSaveStatus.Conflict:
				case ScriptSaveStatus.IoError:
				case ScriptSaveStatus.ReadOnly:
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (flag)
				{
					num2++;
				}
			}
		}
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast((num2 == 0) ? $"已保存全部 C# 脚本（{num}）" : $"C# 脚本保存完成：成功 {num}，失败 {num2}");
		}
		if (num2 != 0)
		{
			return -num2;
		}
		return num;
	}

	private void OnCodeChanged()
	{
		if (!_isLoadingTab)
		{
			if (!string.IsNullOrEmpty(_currentTabKey) && _openTabs.TryGetValue(_currentTabKey, out var value) && !value.IsBlueprintGenerated)
			{
				value.HasUnsavedChanges = true;
				value.Text = _codeEdit.Text;
			}
			UpdateTabTitle();
			UpdateLineCol();
			if (!_isBulkReplacing && GodotObject.IsInstanceValid(_searchBar) && _searchBar.Visible)
			{
				RefreshSearchMatches(selectNearest: true);
			}
		}
	}

	public void ShowSearchBar(bool showReplace)
	{
		if (HasFileOpen())
		{
			if (showReplace && !CanEditCurrentDocument(showToast: true))
			{
				showReplace = false;
			}
			_searchBar.Show();
			_replaceRow.Visible = showReplace;
			_searchBar.CustomMinimumSize = new Vector2(0f, showReplace ? 68 : 38);
			RefreshSearchMatches(selectNearest: true);
			_searchInput.GrabFocus();
			_searchInput.SelectAll();
		}
	}

	public void CloseSearchBar()
	{
		_searchBar.Hide();
		_activeSearchMatch = -1;
		_searchMatches.Clear();
		_searchMatchLabel.Text = "0 / 0";
		_codeEdit.GrabFocus();
	}

	private void RefreshSearchMatches(bool selectNearest)
	{
		_searchMatches.Clear();
		_activeSearchMatch = -1;
		string text = _searchInput.Text;
		if (string.IsNullOrEmpty(text) || !HasFileOpen())
		{
			UpdateSearchMatchLabel();
			return;
		}
		StringComparison comparisonType = (_searchCaseButton.ButtonPressed ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
		for (int i = 0; i < _codeEdit.GetLineCount(); i++)
		{
			string line = _codeEdit.GetLine(i);
			int num = 0;
			while (num <= line.Length - text.Length)
			{
				int num2 = line.IndexOf(text, num, comparisonType);
				if (num2 < 0)
				{
					break;
				}
				if (!_searchWholeButton.ButtonPressed || IsWholeWordMatch(line, num2, text.Length))
				{
					_searchMatches.Add(new SearchMatch(i, num2));
				}
				num = num2 + Math.Max(1, text.Length);
			}
		}
		if (_searchMatches.Count > 0 && selectNearest)
		{
			_activeSearchMatch = FindNearestSearchMatch();
			SelectActiveSearchMatch();
		}
		UpdateSearchMatchLabel();
	}

	public void NavigateSearch(int direction)
	{
		if (!_searchBar.Visible)
		{
			ShowSearchBar(showReplace: false);
		}
		if (string.IsNullOrEmpty(_searchInput.Text))
		{
			_searchInput.GrabFocus();
			return;
		}
		if (_searchMatches.Count == 0)
		{
			RefreshSearchMatches(selectNearest: false);
		}
		if (_searchMatches.Count != 0)
		{
			if (_activeSearchMatch < 0)
			{
				_activeSearchMatch = FindNearestSearchMatch();
			}
			else
			{
				_activeSearchMatch = PositiveModulo(_activeSearchMatch + Math.Sign(direction), _searchMatches.Count);
			}
			SelectActiveSearchMatch();
			UpdateSearchMatchLabel();
		}
	}

	public void ReplaceCurrentMatch()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return;
		}
		if (_searchMatches.Count == 0)
		{
			RefreshSearchMatches(selectNearest: true);
		}
		if (_searchMatches.Count != 0)
		{
			if (_activeSearchMatch < 0)
			{
				_activeSearchMatch = FindNearestSearchMatch();
			}
			SearchMatch searchMatch = _searchMatches[_activeSearchMatch];
			_codeEdit.Select(searchMatch.Line, searchMatch.Column, searchMatch.Line, searchMatch.Column + _searchInput.Text.Length);
			_codeEdit.InsertAtCursor(_replaceInput.Text);
			RefreshSearchMatches(selectNearest: true);
		}
	}

	public int ReplaceAllMatches()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return 0;
		}
		RefreshSearchMatches(selectNearest: false);
		if (_searchMatches.Count == 0)
		{
			return 0;
		}
		List<SearchMatch> list = new List<SearchMatch>(_searchMatches);
		_isBulkReplacing = true;
		_codeEdit.BeginComplexOperation();
		try
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				SearchMatch searchMatch = list[num];
				_codeEdit.Select(searchMatch.Line, searchMatch.Column, searchMatch.Line, searchMatch.Column + _searchInput.Text.Length);
				_codeEdit.InsertAtCursor(_replaceInput.Text);
			}
		}
		finally
		{
			_codeEdit.EndComplexOperation();
			_isBulkReplacing = false;
		}
		RefreshSearchMatches(selectNearest: true);
		XWEditorInterface.Instance?.ShowToast($"已替换 {list.Count} 处匹配");
		return list.Count;
	}

	private int FindNearestSearchMatch()
	{
		int caretLine = _codeEdit.GetCaretLine();
		int caretColumn = _codeEdit.GetCaretColumn();
		for (int i = 0; i < _searchMatches.Count; i++)
		{
			SearchMatch searchMatch = _searchMatches[i];
			if (searchMatch.Line > caretLine || (searchMatch.Line == caretLine && searchMatch.Column >= caretColumn))
			{
				return i;
			}
		}
		return 0;
	}

	private void SelectActiveSearchMatch()
	{
		if (_activeSearchMatch >= 0 && _activeSearchMatch < _searchMatches.Count)
		{
			SearchMatch searchMatch = _searchMatches[_activeSearchMatch];
			_codeEdit.Select(searchMatch.Line, searchMatch.Column, searchMatch.Line, searchMatch.Column + _searchInput.Text.Length);
			_codeEdit.SetCaretLine(searchMatch.Line);
			_codeEdit.SetCaretColumn(searchMatch.Column + _searchInput.Text.Length);
			_codeEdit.CenterViewportToCaret();
		}
	}

	private void UpdateSearchMatchLabel()
	{
		int value = ((_activeSearchMatch >= 0) ? (_activeSearchMatch + 1) : 0);
		_searchMatchLabel.Text = $"{value} / {_searchMatches.Count}";
		bool flag = _searchMatches.Count > 0;
		_searchPreviousButton.Disabled = !flag;
		_searchNextButton.Disabled = !flag;
		_replaceNextButton.Disabled = !flag;
		_replaceAllButton.Disabled = !flag;
	}

	private static bool IsWholeWordMatch(string line, int column, int length)
	{
		bool num = column == 0 || !IsIdentifierCharacter(line[column - 1]);
		int num2 = column + length;
		bool flag = num2 >= line.Length || !IsIdentifierCharacter(line[num2]);
		return num && flag;
	}

	private static bool IsIdentifierCharacter(char character)
	{
		if (!char.IsLetterOrDigit(character))
		{
			return character == '_';
		}
		return true;
	}

	private static int PositiveModulo(int value, int divisor)
	{
		int num = value % divisor;
		if (num >= 0)
		{
			return num;
		}
		return num + divisor;
	}

	public void ShowGoToLineDialog()
	{
		if (HasFileOpen())
		{
			_goToLineSpin.MaxValue = Math.Max(1, _codeEdit.GetLineCount());
			_goToLineSpin.Value = Mathf.Clamp(_codeEdit.GetCaretLine() + 1, 1, _codeEdit.GetLineCount());
			_goToLineDialog.PopupCentered();
			_goToLineSpin.GetLineEdit().GrabFocus();
			_goToLineSpin.GetLineEdit().SelectAll();
		}
	}

	public void CommitGoToLine()
	{
		int line = Mathf.Clamp((int)_goToLineSpin.Value - 1, 0, Math.Max(0, _codeEdit.GetLineCount() - 1));
		_codeEdit.SetCaretLine(line);
		_codeEdit.SetCaretColumn(0);
		_codeEdit.CenterViewportToCaret();
		_codeEdit.GrabFocus();
	}

	private void OnCodeErrorsChanged(int errorCount, int warningCount)
	{
		if (!_isCompiling)
		{
			_diagnosticsLabel.Text = $"实时检测：{errorCount} 错误，{warningCount} 警告";
		}
	}

	private void OnCodeDiagnosticsChanged()
	{
		if (!_isCompiling)
		{
			UpdateDiagnosticList(_codeEdit.GetAllErrors(), _currentTabKey, includeFileName: false);
		}
	}

	private async void OnCompileButtonPressed()
	{
		if (!(_compileButton?.Disabled ?? false))
		{
			await CompileScriptsAsync();
		}
	}

	public async Task<XWScriptCompiler.CompileResult> CompileScriptsAsync()
	{
		if (_isCompiling)
		{
			return LastCompileResult;
		}
		string root = GetActiveModProjectRoot();
		if (string.IsNullOrWhiteSpace(root))
		{
			LastCompileResult = null;
			_diagnosticsLabel.Text = "编译未启动 · 请先打开 Mod 工程";
			XWEditorInterface.Instance?.ShowToast("编译未启动：请先打开一个 Mod 工程。", 1);
			UpdateCompileControls();
			return null;
		}
		if ((_debugSession?.IsRunning ?? false) || (_debugSession?.AssemblyLoaded ?? false))
		{
			LastCompileResult = null;
			_diagnosticsLabel.Text = "编译未启动 · 请先停止调试";
			XWEditorInterface.Instance?.ShowToast("编译未启动：请先停止当前调试会话。", 1);
			UpdateCompileControls();
			return null;
		}
		SaveCurrentTabState();
		if (SaveAllTabs() < 0)
		{
			CompileSaveGateCount++;
			LastCompileResult = null;
			_diagnosticsLabel.Text = "编译已停止 · 请先处理外部修改或保存失败";
			XWEditorInterface.Instance?.ShowToast("编译未启动：存在 C# 外部修改冲突或保存失败。", 1);
			return null;
		}
		_isCompiling = true;
		UpdateCompileControls();
		_diagnosticsLabel.Text = "正在后台编译当前 Mod 脚本…";
		try
		{
			CompileInvocationCount++;
			CancellationToken cancellationToken = _buildLifetimeCts?.Token ?? CancellationToken.None;
			XWScriptCompiler.CompileResult result = (LastCompileResult = await XWScriptCompiler.CompileModProjectAsync(root, cancellationToken));
			ApplyCompileResult(result);
			if (result?.Success ?? false)
			{
				await RefreshCallbackPreviewAsync(root, result.OutputAssemblyPath, cancellationToken);
			}
			return result;
		}
		catch (Exception ex)
		{
			_diagnosticsLabel.Text = "编译启动失败";
			XWEditorInterface.Instance?.AddOutputMessage("脚本编译启动失败: " + ex.Message, XWOutputPanel.MessageType.Error);
			return null;
		}
		finally
		{
			_isCompiling = false;
			UpdateCompileControls();
		}
	}

	private async Task<bool> RefreshCallbackPreviewAsync(string projectRoot, string assemblyPath, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!TryResolveCallbackPreviewOwner(projectRoot, out var ownerId, out var error))
		{
			ReportCallbackPreviewError(error);
			return false;
		}
		if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
		{
			ReportCallbackPreviewError("状态机动作目录没有找到本次编译的 Mod 程序集。");
			return false;
		}
		cancellationToken.ThrowIfCancellationRequested();
		if (!string.IsNullOrWhiteSpace(_callbackPreviewOwnerId))
		{
			if (!(await Task.Run(() => CallbackPreviewLoader.UnloadMod(_callbackPreviewOwnerId), cancellationToken)))
			{
				ReportCallbackPreviewError("旧的状态机动作仍被预览或运行时使用。请停止状态机模拟后重新编译。");
				return false;
			}
			_callbackPreviewOwnerId = "";
			_callbackPreviewAssemblyPath = "";
		}
		try
		{
			XWModAssemblyLoader.LoadedModAssembly loadedModAssembly = await Task.Run(() => CallbackPreviewLoader.LoadModAssembly(ownerId, assemblyPath, new string[1] { Path.GetDirectoryName(assemblyPath) ?? string.Empty }, registerCharacterComponentRuntimes: false), cancellationToken);
			_callbackPreviewOwnerId = loadedModAssembly.ModId;
			_callbackPreviewAssemblyPath = loadedModAssembly.AssemblyPath;
			XWEditorInterface.Instance?.AddOutputMessage($"状态机动作目录已刷新：{loadedModAssembly.ModId} · {loadedModAssembly.StateMachineCallbackCount} 个动作");
			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			ReportCallbackPreviewError("状态机动作目录加载失败：" + ex2.GetBaseException().Message);
			return false;
		}
	}

	private bool UnloadCallbackPreview(bool reportFailure)
	{
		if (string.IsNullOrWhiteSpace(_callbackPreviewOwnerId))
		{
			return true;
		}
		if (!CallbackPreviewLoader.UnloadMod(_callbackPreviewOwnerId))
		{
			if (reportFailure)
			{
				ReportCallbackPreviewError("状态机动作仍有活动运行实例，暂时不能卸载。请停止状态机模拟或游戏预览后重试。");
			}
			return false;
		}
		_callbackPreviewOwnerId = "";
		_callbackPreviewAssemblyPath = "";
		return true;
	}

	private static bool TryResolveCallbackPreviewOwner(string projectRoot, out string ownerId, out string error)
	{
		ownerId = "";
		error = "";
		try
		{
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot ?? string.Empty, "mod.json"));
			if (xWModManifest == null)
			{
				error = "找不到 mod.json，无法确定状态机动作的真实 Mod ID。";
				return false;
			}
			if (!StateMachineCallbackKey.TryNormalizeOwnerId(xWModManifest.Id, out ownerId) || string.Equals(ownerId, "builtin", StringComparison.Ordinal))
			{
				error = "mod.json 的 id 无效，无法加载状态机动作目录。";
				ownerId = "";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "读取 Mod ID 失败：" + ex.Message;
			return false;
		}
	}

	private static void ReportCallbackPreviewError(string message)
	{
		XWEditorInterface.Instance?.AddOutputMessage(message, XWOutputPanel.MessageType.Error);
		XWEditorInterface.Instance?.ShowToast(message, 2);
	}

	private void ApplyCompileResult(XWScriptCompiler.CompileResult result)
	{
		if (result == null)
		{
			_diagnosticsLabel.Text = "编译没有返回结果";
			return;
		}
		int num = 0;
		int num2 = 0;
		foreach (XWCodeErrorChecker.ErrorData diagnostic in result.Diagnostics)
		{
			if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Error)
			{
				num++;
			}
			else if (diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning)
			{
				num2++;
			}
		}
		if (!string.IsNullOrEmpty(_currentTabKey) && _currentTabKey.GetExtension().ToLowerInvariant() == "cs")
		{
			List<XWCodeErrorChecker.ErrorData> list = XWScriptCompiler.FilterDiagnosticsForFile(result.Diagnostics, _currentTabKey);
			if (list.Count > 0)
			{
				_codeEdit.SetExternalDiagnostics(list);
			}
			else
			{
				_codeEdit.RequestValidationNow();
			}
		}
		string value = (result.IsModProject ? ("Mod " + FirstNonEmpty(result.TargetName, "脚本")) : "游戏工程");
		if (result.Cancelled)
		{
			_diagnosticsLabel.Text = $"{value} 编译已取消 · {result.ElapsedSeconds:0.0}s";
		}
		else if (result.TimedOut)
		{
			_diagnosticsLabel.Text = $"{value} 编译超时 · {result.ElapsedSeconds:0.0}s";
		}
		else if (result.Success)
		{
			_diagnosticsLabel.Text = $"{value} 编译完成 · {result.ScriptCount} 个脚本 · {num2} 个警告 · {result.ElapsedSeconds:0.0}s";
		}
		else if (num > 0 || num2 > 0)
		{
			_diagnosticsLabel.Text = $"{value} 编译失败 · {num} 个错误 · {num2} 个警告 · {result.ElapsedSeconds:0.0}s";
		}
		else
		{
			_diagnosticsLabel.Text = $"{value} 编译失败 · 退出码 {result.ExitCode} · {result.ElapsedSeconds:0.0}s";
		}
		if (!string.IsNullOrWhiteSpace(result.Output))
		{
			XWEditorInterface.Instance?.AddOutputMessage(result.Output);
		}
		UpdateDiagnosticList(result.Diagnostics, "", includeFileName: true);
	}

	private void UpdateDiagnosticList(IEnumerable<XWCodeErrorChecker.ErrorData> diagnostics, string defaultFilePath, bool includeFileName)
	{
		_diagnosticEntries.Clear();
		_diagnosticsList.Clear();
		if (diagnostics != null)
		{
			foreach (XWCodeErrorChecker.ErrorData diagnostic in diagnostics)
			{
				if (diagnostic != null)
				{
					_diagnosticEntries.Add(new DiagnosticListEntry
					{
						Diagnostic = diagnostic,
						FilePath = ResolveDiagnosticFilePath(diagnostic, defaultFilePath)
					});
				}
			}
		}
		_diagnosticEntries.Sort(CompareDiagnosticEntries);
		for (int i = 0; i < _diagnosticEntries.Count; i++)
		{
			DiagnosticListEntry diagnosticListEntry = _diagnosticEntries[i];
			int idx = _diagnosticsList.AddItem(BuildDiagnosticText(diagnosticListEntry, includeFileName), GetDiagnosticIcon(diagnosticListEntry.Diagnostic));
			_diagnosticsList.SetItemMetadata(idx, i);
		}
		_diagnosticsPanel.Visible = _diagnosticEntries.Count > 0;
		if (GodotObject.IsInstanceValid(_diagnosticsCopyButton))
		{
			_diagnosticsCopyButton.Disabled = _diagnosticEntries.Count == 0;
		}
	}

	private void OnDiagnosticListItemSelected(long index)
	{
		JumpToDiagnosticItem((int)index);
	}

	private void OnDiagnosticListItemActivated(long index)
	{
		JumpToDiagnosticItem((int)index);
	}

	private void OnDiagnosticsListGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right)
		{
			int itemAtPosition = _diagnosticsList.GetItemAtPosition(inputEventMouseButton.Position, exact: true);
			if (itemAtPosition >= 0 && itemAtPosition < _diagnosticsList.ItemCount)
			{
				_diagnosticsList.Select(itemAtPosition);
			}
			if (GodotObject.IsInstanceValid(_diagnosticsContextMenu))
			{
				_diagnosticsContextMenu.Position = (Vector2I)(_diagnosticsList.GetScreenPosition() + inputEventMouseButton.Position);
				_diagnosticsContextMenu.Popup();
			}
			_diagnosticsList.AcceptEvent();
		}
		else if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.C && (inputEventKey.CtrlPressed || inputEventKey.MetaPressed))
		{
			CopySelectedDiagnostics();
			_diagnosticsList.AcceptEvent();
		}
	}

	private void CopySelectedDiagnostics()
	{
		List<DiagnosticListEntry> list = GetSelectedDiagnosticEntries();
		if (list.Count == 0)
		{
			list = new List<DiagnosticListEntry>(_diagnosticEntries);
		}
		CopyDiagnosticEntriesToClipboard(list);
	}

	private void CopyAllDiagnostics()
	{
		CopyDiagnosticEntriesToClipboard(_diagnosticEntries);
	}

	private void CopyDiagnosticEntriesToClipboard(IEnumerable<DiagnosticListEntry> entries)
	{
		List<string> list = new List<string>();
		if (entries != null)
		{
			foreach (DiagnosticListEntry entry in entries)
			{
				string text = BuildDiagnosticClipboardText(entry);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		if (list.Count != 0)
		{
			DisplayServer.ClipboardSet(string.Join("\n", list));
			XWEditorInterface.Instance?.AddOutputMessage($"脚本诊断: 已复制 {list.Count} 条警告/错误", XWOutputPanel.MessageType.Editor);
		}
	}

	private List<DiagnosticListEntry> GetSelectedDiagnosticEntries()
	{
		List<DiagnosticListEntry> list = new List<DiagnosticListEntry>();
		if (!GodotObject.IsInstanceValid(_diagnosticsList))
		{
			return list;
		}
		int[] selectedItems = _diagnosticsList.GetSelectedItems();
		foreach (int itemIndex in selectedItems)
		{
			DiagnosticListEntry diagnosticEntryFromItem = GetDiagnosticEntryFromItem(itemIndex);
			if (diagnosticEntryFromItem != null)
			{
				list.Add(diagnosticEntryFromItem);
			}
		}
		return list;
	}

	private DiagnosticListEntry GetDiagnosticEntryFromItem(int itemIndex)
	{
		if (itemIndex < 0 || itemIndex >= _diagnosticsList.ItemCount)
		{
			return null;
		}
		Variant itemMetadata = _diagnosticsList.GetItemMetadata(itemIndex);
		if (itemMetadata.VariantType == Variant.Type.Nil)
		{
			return null;
		}
		int num = itemMetadata.AsInt32();
		if (num < 0 || num >= _diagnosticEntries.Count)
		{
			return null;
		}
		return _diagnosticEntries[num];
	}

	private string BuildDiagnosticClipboardText(DiagnosticListEntry entry)
	{
		if (entry?.Diagnostic == null)
		{
			return "";
		}
		XWCodeErrorChecker.ErrorData diagnostic = entry.Diagnostic;
		string value = ((diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning) ? "警告" : "错误");
		string text = (string.IsNullOrWhiteSpace(entry.FilePath) ? _currentTabKey : entry.FilePath);
		string value2 = (string.IsNullOrWhiteSpace(text) ? $"{Mathf.Max(0, diagnostic.Line) + 1}:{Mathf.Max(0, diagnostic.Column) + 1}" : $"{text.Replace('\\', '/')}:{Mathf.Max(0, diagnostic.Line) + 1}:{Mathf.Max(0, diagnostic.Column) + 1}");
		string value3 = (string.IsNullOrEmpty(diagnostic.Code) ? "" : (diagnostic.Code + " "));
		return $"{value} {value2} {value3}{diagnostic.Message}";
	}

	private void JumpToDiagnosticItem(int itemIndex)
	{
		if (itemIndex >= 0 && itemIndex < _diagnosticsList.ItemCount)
		{
			JumpToDiagnostic(GetDiagnosticEntryFromItem(itemIndex));
		}
	}

	private void JumpToDiagnostic(DiagnosticListEntry entry)
	{
		if (entry?.Diagnostic == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(entry.FilePath))
		{
			string text = ResolveOpenFilePath(entry.FilePath);
			if (!string.IsNullOrEmpty(text) && text != _currentTabKey && Godot.FileAccess.FileExists(text))
			{
				OpenFile(text);
			}
		}
		_codeEdit.JumpToError(entry.Diagnostic);
		_codeEdit.GrabFocus();
	}

	private void RequestCurrentValidation()
	{
		if (HasFileOpen())
		{
			_diagnosticsLabel.Text = "正在检查当前脚本...";
			_codeEdit.RequestValidationNow();
		}
	}

	private void JumpToNextDiagnostic(int direction)
	{
		if (_diagnosticEntries.Count == 0)
		{
			RequestCurrentValidation();
			return;
		}
		int caretLine = _codeEdit.GetCaretLine();
		int num = -1;
		if (direction >= 0)
		{
			for (int i = 0; i < _diagnosticEntries.Count; i++)
			{
				DiagnosticListEntry diagnosticListEntry = _diagnosticEntries[i];
				if (diagnosticListEntry.FilePath != _currentTabKey || diagnosticListEntry.Diagnostic.Line > caretLine)
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				num = 0;
			}
		}
		else
		{
			for (int num2 = _diagnosticEntries.Count - 1; num2 >= 0; num2--)
			{
				DiagnosticListEntry diagnosticListEntry2 = _diagnosticEntries[num2];
				if (diagnosticListEntry2.FilePath != _currentTabKey || diagnosticListEntry2.Diagnostic.Line < caretLine)
				{
					num = num2;
					break;
				}
			}
			if (num < 0)
			{
				num = _diagnosticEntries.Count - 1;
			}
		}
		_diagnosticsList.Select(num);
		_diagnosticsList.EnsureCurrentIsVisible();
		JumpToDiagnostic(_diagnosticEntries[num]);
	}

	private void OpenBlueprintWorkspace()
	{
		XWEditorInterface.Instance?.FocusPanel("bp_editor");
	}

	private string ResolveOpenFilePath(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return "";
		}
		string text = XWScriptCompiler.NormalizeFilePath(filePath);
		foreach (string tabKey in _tabKeys)
		{
			if (XWScriptCompiler.NormalizeFilePath(tabKey) == text)
			{
				return tabKey;
			}
		}
		return LocalizeProjectPath(filePath);
	}

	private static string ResolveDiagnosticFilePath(XWCodeErrorChecker.ErrorData diagnostic, string defaultFilePath)
	{
		string text = (string.IsNullOrWhiteSpace(diagnostic.FilePath) ? defaultFilePath : diagnostic.FilePath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return LocalizeProjectPath(text);
		}
		return "";
	}

	private static string LocalizeProjectPath(string filePath)
	{
		string text = filePath.Replace('\\', '/');
		if (text.StartsWith("res://"))
		{
			return text;
		}
		string text2 = ProjectSettings.GlobalizePath("res://").Replace('\\', '/');
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		if (!string.IsNullOrEmpty(text2) && text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return "res://" + text.Substring(text2.Length);
		}
		return filePath;
	}

	private static int CompareDiagnosticEntries(DiagnosticListEntry a, DiagnosticListEntry b)
	{
		int num = GetSeverityRank(a.Diagnostic).CompareTo(GetSeverityRank(b.Diagnostic));
		if (num != 0)
		{
			return num;
		}
		int num2 = string.Compare(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = a.Diagnostic.Line.CompareTo(b.Diagnostic.Line);
		if (num3 != 0)
		{
			return num3;
		}
		int num4 = a.Diagnostic.Column.CompareTo(b.Diagnostic.Column);
		if (num4 != 0)
		{
			return num4;
		}
		return string.Compare(a.Diagnostic.Code, b.Diagnostic.Code, StringComparison.Ordinal);
	}

	private static int GetSeverityRank(XWCodeErrorChecker.ErrorData diagnostic)
	{
		return diagnostic.SeverityLevel switch
		{
			XWCodeErrorChecker.Severity.Error => 0, 
			XWCodeErrorChecker.Severity.Warning => 1, 
			_ => 2, 
		};
	}

	private string BuildDiagnosticText(DiagnosticListEntry entry, bool includeFileName)
	{
		XWCodeErrorChecker.ErrorData diagnostic = entry.Diagnostic;
		string value = ((diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning) ? "警告" : "错误");
		string value2 = ((includeFileName && !string.IsNullOrEmpty(entry.FilePath)) ? (entry.FilePath.Replace('\\', '/').GetFile() + " ") : "");
		string value3 = $"{Mathf.Max(0, diagnostic.Line) + 1}:{Mathf.Max(0, diagnostic.Column) + 1}";
		string value4 = (string.IsNullOrEmpty(diagnostic.Code) ? "" : (diagnostic.Code + " "));
		return $"{value} {value2}{value3} {value4}{diagnostic.Message}";
	}

	private Texture2D GetDiagnosticIcon(XWCodeErrorChecker.ErrorData diagnostic)
	{
		if (diagnostic.SeverityLevel != XWCodeErrorChecker.Severity.Warning)
		{
			return _iconStatusError;
		}
		return _iconStatusWarning;
	}

	private void UpdateCompileControls()
	{
		if (_compileButton != null)
		{
			string activeModProjectRoot = GetActiveModProjectRoot();
			bool flag = !string.IsNullOrWhiteSpace(activeModProjectRoot);
			bool flag2 = flag && XWScriptCompiler.IsModBuildBusy(activeModProjectRoot);
			bool flag3 = _debugSession?.HasLiveSession ?? false;
			bool flag4 = flag2 || flag3;
			_compileButton.Disabled = !flag || _isCompiling || flag2 || flag3;
			_compileButton.Text = ((_isCompiling || flag2) ? "编译中…" : (flag3 ? "调试中" : "编译 Mod"));
			_compileButton.TooltipText = "后台编译当前 Mod 工程中的 C# 与蓝图生成脚本，不会执行脚本";
			bool flag5 = !HasFileOpen();
			bool isCurrentDocumentReadOnly = IsCurrentDocumentReadOnly;
			if (GodotObject.IsInstanceValid(_searchMenuBtn))
			{
				_searchMenuBtn.Disabled = flag5;
			}
			if (GodotObject.IsInstanceValid(_goToMenuBtn))
			{
				_goToMenuBtn.Disabled = flag5;
			}
			if (GodotObject.IsInstanceValid(_definitionButton))
			{
				_definitionButton.Disabled = flag5;
			}
			if (GodotObject.IsInstanceValid(_referencesButton))
			{
				_referencesButton.Disabled = flag5;
			}
			if (GodotObject.IsInstanceValid(_renameButton))
			{
				_renameButton.Disabled = flag5 || isCurrentDocumentReadOnly || flag4;
			}
			if (GodotObject.IsInstanceValid(_formatButton))
			{
				_formatButton.Disabled = flag5 || isCurrentDocumentReadOnly;
			}
			if (GodotObject.IsInstanceValid(_quickFixButton))
			{
				_quickFixButton.Disabled = flag5 || isCurrentDocumentReadOnly;
			}
			XWCSharpIdeService.RenameHistoryAvailability renameHistoryAvailability = XWCSharpIdeService.GetRenameHistoryAvailability(activeModProjectRoot);
			if (GodotObject.IsInstanceValid(_renameUndoButton))
			{
				_renameUndoButton.Disabled = flag4 || !renameHistoryAvailability.CanUndo;
			}
			if (GodotObject.IsInstanceValid(_renameRedoButton))
			{
				_renameRedoButton.Disabled = flag4 || !renameHistoryAvailability.CanRedo;
			}
		}
	}

	private void OnModBuildFlightStateChanged(string projectRoot, bool _)
	{
		if (!string.Equals(NormalizeProjectRoot(GetActiveModProjectRoot()), NormalizeProjectRoot(projectRoot), StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		_scriptEditorSynchronizationContext?.Post(delegate
		{
			if (GodotObject.IsInstanceValid(this) && IsInsideTree())
			{
				UpdateCompileControls();
				UpdateDebugControls();
			}
		}, null);
	}

	public bool OpenCurrentGeneratedBlueprintSource()
	{
		if (!TryGetCurrentTab(out var tab) || !tab.IsBlueprintGenerated)
		{
			return false;
		}
		string blueprintSourcePath;
		XWBlueprintGeneratedCSharpPolicy.LinkState linkState = (tab.GeneratedLinkState = XWBlueprintGeneratedCSharpPolicy.GetLinkState(tab.FilePath, tab.Text, GetActiveModProjectRoot(), out blueprintSourcePath));
		tab.BlueprintSourcePath = ((linkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Linked) ? blueprintSourcePath : "");
		UpdateGeneratedBlueprintBar(tab);
		if (linkState != XWBlueprintGeneratedCSharpPolicy.LinkState.Linked)
		{
			XWEditorInterface.Instance?.ShowToast("源蓝图不存在或不属于当前 Mod，生成代码继续保持只读。", 2);
			return false;
		}
		XWBPScript xWBPScript = ResourceLoader.Load<XWBPScript>(blueprintSourcePath, "", ResourceLoader.CacheMode.Replace);
		if (!GodotObject.IsInstanceValid(xWBPScript) || !(XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor))
		{
			XWEditorInterface.Instance?.ShowToast("无法打开源蓝图。", 2);
			return false;
		}
		xWBPEditor.Init(xWBPScript);
		XWEditorInterface.Instance.FocusPanel("bp_editor");
		return true;
	}

	private bool CanEditCurrentDocument(bool showToast)
	{
		ScriptTabInfo tab;
		bool num = TryGetCurrentTab(out tab) && !tab.IsBlueprintGenerated;
		if (!num && showToast && HasFileOpen())
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance == null)
			{
				return num;
			}
			instance.ShowToast("该文件由蓝图生成；请返回源蓝图修改。", 1);
		}
		return num;
	}

	private bool TryGetCurrentTab(out ScriptTabInfo tab)
	{
		if (!string.IsNullOrWhiteSpace(_currentTabKey) && _openTabs.TryGetValue(_currentTabKey, out tab))
		{
			return true;
		}
		tab = null;
		return false;
	}

	private void RefreshGeneratedMetadata(ScriptTabInfo tab)
	{
		if (tab != null)
		{
			tab.GeneratedLinkState = XWBlueprintGeneratedCSharpPolicy.GetLinkState(tab.FilePath, tab.Text, GetActiveModProjectRoot(), out var blueprintSourcePath);
			tab.BlueprintSourcePath = ((tab.GeneratedLinkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Linked) ? blueprintSourcePath : "");
		}
	}

	private bool ReloadTabFromDisk(ScriptTabInfo tab)
	{
		string error = "";
		if (tab == null || !TryReadDiskSnapshot(tab.FilePath, out var snapshot, out error) || !snapshot.Exists)
		{
			LastSaveError = error;
			return false;
		}
		ApplyDiskSnapshotToTab(tab, snapshot);
		return true;
	}

	private void UpdateGeneratedBlueprintBar(ScriptTabInfo tab)
	{
		if (GodotObject.IsInstanceValid(_generatedBlueprintBar))
		{
			bool flag = tab?.IsBlueprintGenerated ?? false;
			_generatedBlueprintBar.Visible = flag;
			if (flag)
			{
				bool flag2 = tab.GeneratedLinkState == XWBlueprintGeneratedCSharpPolicy.LinkState.Linked;
				_generatedBlueprintStatusLabel.Text = (flag2 ? "蓝图生成代码 · 只读" : "孤立的蓝图生成代码 · 只读保护");
				_generatedBlueprintSourceLabel.Text = (flag2 ? ("来源：" + tab.BlueprintSourcePath) : "来源蓝图缺失、已移动或不属于当前 Mod；不会退化成可写脚本。");
				_returnToBlueprintButton.Disabled = !flag2;
				_returnToBlueprintButton.TooltipText = (flag2 ? "打开生成此 C# 文件的源蓝图" : "源蓝图链接未通过当前 Mod 安全校验");
			}
		}
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string text in values)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		return "";
	}

	private void UpdateLineCol()
	{
		int value = _codeEdit.GetCaretLine() + 1;
		int value2 = _codeEdit.GetCaretColumn() + 1;
		_lineColLabel.Text = $"行 {value}, 列 {value2}";
	}

	private void UpdateTabTitle()
	{
		if (string.IsNullOrEmpty(_currentTabKey))
		{
			return;
		}
		int num = _tabKeys.IndexOf(_currentTabKey);
		if (num < 0)
		{
			return;
		}
		string text = _currentTabKey.GetFile().GetBaseName();
		if (_openTabs.TryGetValue(_currentTabKey, out var value))
		{
			if (value.HasUnsavedChanges)
			{
				text = "(*)" + text;
			}
			if (value.HasExternalConflict)
			{
				text = "(⚠)" + text;
			}
		}
		_scriptTabBar.SetTabTitle(num, text);
	}

	private void UpdateScriptList()
	{
		_scriptList.Clear();
		foreach (string tabKey in _tabKeys)
		{
			string text = tabKey.GetFile();
			if (_openTabs.TryGetValue(tabKey, out var value) && value.HasExternalConflict)
			{
				text = "⚠ " + text;
			}
			Texture2D iconScript = _iconScript;
			int idx = _scriptList.AddItem(text, iconScript);
			_scriptList.SetItemMetadata(idx, tabKey);
			if (tabKey == _currentTabKey)
			{
				_scriptList.Select(idx);
				_scriptList.EnsureCurrentIsVisible();
			}
		}
	}

	private void UpdateScriptListSelection()
	{
		for (int i = 0; i < _scriptList.ItemCount; i++)
		{
			Variant itemMetadata = _scriptList.GetItemMetadata(i);
			if (itemMetadata.VariantType != Variant.Type.Nil && itemMetadata.As<string>() == _currentTabKey)
			{
				_scriptList.Select(i);
				_scriptList.EnsureCurrentIsVisible();
				return;
			}
		}
		_scriptList.DeselectAll();
	}

	private void OnScriptFilterChanged(string filterText)
	{
		string value = filterText.ToLowerInvariant();
		_scriptList.Clear();
		foreach (string tabKey in _tabKeys)
		{
			string text = tabKey.GetFile();
			if (string.IsNullOrEmpty(value) || text.ToLowerInvariant().Contains(value) || tabKey.ToLowerInvariant().Contains(value))
			{
				if (_openTabs.TryGetValue(tabKey, out var value2) && value2.HasExternalConflict)
				{
					text = "⚠ " + text;
				}
				Texture2D iconScript = _iconScript;
				int idx = _scriptList.AddItem(text, iconScript);
				_scriptList.SetItemMetadata(idx, tabKey);
				if (tabKey == _currentTabKey)
				{
					_scriptList.Select(idx);
				}
			}
		}
	}

	private void OnScriptListItemSelected(long idx)
	{
		Variant itemMetadata = _scriptList.GetItemMetadata((int)idx);
		if (itemMetadata.VariantType == Variant.Type.Nil)
		{
			return;
		}
		string text = itemMetadata.As<string>();
		if (!(text == _currentTabKey))
		{
			SaveCurrentTabState();
			int num = _tabKeys.IndexOf(text);
			if (num >= 0)
			{
				_isSwitchingTab = true;
				_scriptTabBar.CurrentTab = num;
				_isSwitchingTab = false;
			}
			LoadTabData(text);
		}
	}

	public bool HasFileOpen()
	{
		return !string.IsNullOrEmpty(_currentTabKey);
	}

	public string GetCurrentFilePath()
	{
		return _currentTabKey;
	}

	public static bool IsSupportedScriptPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return string.Equals(path.GetExtension(), "cs", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static string NormalizeNewCSharpPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !string.IsNullOrWhiteSpace(path.GetExtension()))
		{
			return path ?? string.Empty;
		}
		return path + ".cs";
	}

	public static string BuildDefaultCSharpTemplate(string path)
	{
		string baseName = (path ?? string.Empty).GetFile().GetBaseName();
		StringBuilder stringBuilder = new StringBuilder();
		string text = baseName;
		foreach (char c in text)
		{
			stringBuilder.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
		}
		string text2 = ((stringBuilder.Length == 0) ? "NewModScript" : stringBuilder.ToString());
		if (!char.IsLetter(text2[0]) && text2[0] != '_')
		{
			text2 = "Mod_" + text2;
		}
		if (SyntaxFacts.GetKeywordKind(text2) != SyntaxKind.None)
		{
			text2 = "Mod_" + text2;
		}
		return "using Godot;\n\npublic partial class " + text2 + " : Node\n{\n    public override void _Ready()\n    {\n    }\n}\n";
	}

	private void RejectUnsupportedScriptPath(string path, string action, bool countRequest = true)
	{
		if (countRequest)
		{
			UnsupportedScriptRequestCount++;
		}
		string extension = (path ?? string.Empty).GetExtension();
		string text = (string.IsNullOrWhiteSpace(extension) ? "无扩展名文件" : ("." + extension + " 文件"));
		XWEditorInterface.Instance?.ShowToast("脚本编辑器仅支持 C#，不能" + action + text + "。", 2);
	}

	public void LoadScript(string path)
	{
		OpenFile(path);
	}

	public XWModDebugController GetDebugController()
	{
		return _debugSession?.DebugController;
	}

	private void InitializeDebugWorkbench()
	{
		_debugWorkbench = GetNode<XWModDebugWorkbench>("%DebugWorkbench");
		_debugWorkbenchToggle = GetNode<Button>("%DebugWorkbenchToggle");
		_debugWorkbench.BindController(_debugSession?.DebugController);
		_debugWorkbench.StopRequested += OnDebugWorkbenchStopRequested;
		_debugWorkbenchToggle.Toggled += SetDebugWorkbenchVisible;
		if (_debugSession?.DebugController != null)
		{
			_debugSession.DebugController.SnapshotChanged += OnScriptDebugSnapshotChanged;
		}
		SetDebugWorkbenchVisible(visible: false);
	}

	private void ShutdownDebugWorkbench()
	{
		if (_debugSession?.DebugController != null)
		{
			_debugSession.DebugController.SnapshotChanged -= OnScriptDebugSnapshotChanged;
		}
		if (GodotObject.IsInstanceValid(_debugWorkbench))
		{
			_debugWorkbench.StopRequested -= OnDebugWorkbenchStopRequested;
			_debugWorkbench.UnbindController();
		}
		if (GodotObject.IsInstanceValid(_debugWorkbenchToggle))
		{
			_debugWorkbenchToggle.Toggled -= SetDebugWorkbenchVisible;
		}
		lock (_scriptDebugSnapshotLock)
		{
			_pendingScriptDebugSnapshot = null;
		}
		Interlocked.Exchange(ref _scriptDebugNavigationQueued, 0);
	}

	private void SetDebugWorkbenchVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(_debugWorkbench))
		{
			_debugWorkbench.Visible = visible;
		}
		if (GodotObject.IsInstanceValid(_debugWorkbenchToggle) && _debugWorkbenchToggle.ButtonPressed != visible)
		{
			_debugWorkbenchToggle.SetPressedNoSignal(visible);
		}
	}

	private async void OnDebugWorkbenchStopRequested()
	{
		await StopDebugSessionAsync();
	}

	private void OnScriptDebugSnapshotChanged(object sender, XWModDebugSnapshot snapshot)
	{
		if (snapshot == null || snapshot.State != XWModDebugState.Paused)
		{
			return;
		}
		XWModDebugCheckpoint currentCheckpoint = snapshot.CurrentCheckpoint;
		if (currentCheckpoint != null && currentCheckpoint.Kind == XWModDebugCheckpointKind.CSharpLine)
		{
			lock (_scriptDebugSnapshotLock)
			{
				_pendingScriptDebugSnapshot = snapshot;
			}
			if (Interlocked.Exchange(ref _scriptDebugNavigationQueued, 1) == 0)
			{
				CallDeferred("ApplyScriptDebugNavigation");
			}
		}
	}

	private void ApplyScriptDebugNavigation()
	{
		Interlocked.Exchange(ref _scriptDebugNavigationQueued, 0);
		XWModDebugSnapshot pendingScriptDebugSnapshot;
		lock (_scriptDebugSnapshotLock)
		{
			pendingScriptDebugSnapshot = _pendingScriptDebugSnapshot;
			_pendingScriptDebugSnapshot = null;
		}
		XWModDebugCheckpoint xWModDebugCheckpoint = pendingScriptDebugSnapshot?.CurrentCheckpoint;
		if (xWModDebugCheckpoint != null && xWModDebugCheckpoint.Kind == XWModDebugCheckpointKind.CSharpLine && IsPathInsideRoot(xWModDebugCheckpoint.SourcePath, GetActiveModProjectRoot()) && File.Exists(xWModDebugCheckpoint.SourcePath))
		{
			SetDebugWorkbenchVisible(visible: true);
			OpenFile(xWModDebugCheckpoint.SourcePath);
			_codeEdit?.SetCaretLine(Math.Max(0, xWModDebugCheckpoint.Line - 1));
			_codeEdit?.SetCaretColumn(0);
			_codeEdit?.CenterViewportToCaret();
		}
	}

	private void InitializeExternalConflictUI()
	{
		_externalConflictBar = GetNode<PanelContainer>("%ExternalConflictBar");
		_externalConflictTitleLabel = GetNode<Label>("%ExternalConflictTitleLabel");
		_externalConflictDetailLabel = GetNode<Label>("%ExternalConflictDetailLabel");
		_viewExternalDiffButton = GetNode<Button>("%ViewExternalDiffButton");
		_reloadExternalButton = GetNode<Button>("%ReloadExternalButton");
		_overwriteExternalButton = GetNode<Button>("%OverwriteExternalButton");
		_saveConflictCopyButton = GetNode<Button>("%SaveConflictCopyButton");
		_externalConflictDiffWindow = GetNode<Window>("%ExternalConflictDiffWindow");
		_externalConflictEditorText = GetNode<TextEdit>("%ExternalConflictEditorText");
		_externalConflictDiskText = GetNode<TextEdit>("%ExternalConflictDiskText");
		_viewExternalDiffButton.Pressed += ShowCurrentExternalConflictDiff;
		_reloadExternalButton.Pressed += delegate
		{
			ReloadCurrentExternalConflict();
		};
		_overwriteExternalButton.Pressed += delegate
		{
			OverwriteCurrentExternalConflict();
		};
		_saveConflictCopyButton.Pressed += SaveFileAs;
		_externalConflictDiffWindow.CloseRequested += _externalConflictDiffWindow.Hide;
		_externalConflictBar.Hide();
		_externalConflictWatcher = new Godot.Timer
		{
			Name = "ExternalConflictWatcher",
			OneShot = false,
			WaitTime = 0.85,
			Autostart = false,
			ProcessMode = ProcessModeEnum.Disabled
		};
		_externalConflictWatcher.Timeout += ScanExternalChangesFromWatcher;
		AddChild(_externalConflictWatcher, forceReadableName: false, InternalMode.Disabled);
	}

	private static string ComputeScriptContentHash(string text)
	{
		return Convert.ToHexString(SHA256.HashData(ScriptUtf8.GetBytes(text ?? "")));
	}

	private static string ResolveAbsoluteScriptPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		return Path.GetFullPath((path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase)) ? ProjectSettings.GlobalizePath(path) : path);
	}

	private static bool TryReadDiskSnapshot(string path, out ScriptDiskSnapshot snapshot, out string error)
	{
		snapshot = default(ScriptDiskSnapshot);
		error = "";
		try
		{
			string text = ResolveAbsoluteScriptPath(path);
			if (string.IsNullOrWhiteSpace(text))
			{
				error = "脚本路径为空。";
				return false;
			}
			if (!File.Exists(text))
			{
				snapshot = new ScriptDiskSnapshot(exists: false, "", "", 0L, 0uL);
				return true;
			}
			string text2 = File.ReadAllText(text, ScriptUtf8);
			FileInfo fileInfo = new FileInfo(text);
			snapshot = new ScriptDiskSnapshot(exists: true, text2, ComputeScriptContentHash(text2), fileInfo.Length, Godot.FileAccess.GetModifiedTime(path));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static bool DiskSnapshotMatchesBaseline(ScriptTabInfo tab, ScriptDiskSnapshot snapshot)
	{
		if (tab == null || tab.DiskExists != snapshot.Exists)
		{
			return false;
		}
		if (!snapshot.Exists)
		{
			return true;
		}
		return string.Equals(tab.DiskContentHash, snapshot.ContentHash, StringComparison.Ordinal);
	}

	private static ScriptDiskSnapshot BuildSavedSnapshot(string path, string text)
	{
		if (text == null)
		{
			text = "";
		}
		return new ScriptDiskSnapshot(exists: true, text, ComputeScriptContentHash(text), ScriptUtf8.GetByteCount(text), Godot.FileAccess.GetModifiedTime(path));
	}

	private static void ApplyDiskBaseline(ScriptTabInfo tab, ScriptDiskSnapshot snapshot)
	{
		if (tab != null)
		{
			tab.DiskExists = snapshot.Exists;
			tab.DiskContentHash = snapshot.ContentHash;
			tab.DiskByteLength = snapshot.ByteLength;
			tab.ModifiedTime = snapshot.ModifiedTime;
		}
	}

	private void ApplyDiskSnapshotToTab(ScriptTabInfo tab, ScriptDiskSnapshot snapshot)
	{
		tab.Text = snapshot.Text;
		tab.HasUnsavedChanges = false;
		ApplyDiskBaseline(tab, snapshot);
		ClearExternalConflict(tab, updateSurface: false);
		RefreshGeneratedMetadata(tab);
	}

	private void MarkExternalConflict(ScriptTabInfo tab, ScriptDiskSnapshot snapshot)
	{
		if (tab != null)
		{
			bool num = !tab.HasExternalConflict || tab.ConflictDiskExists != snapshot.Exists || !string.Equals(tab.ConflictDiskContentHash, snapshot.ContentHash, StringComparison.Ordinal);
			tab.HasExternalConflict = true;
			tab.ConflictDiskExists = snapshot.Exists;
			tab.ConflictDiskText = snapshot.Text;
			tab.ConflictDiskContentHash = snapshot.ContentHash;
			tab.ConflictDiskByteLength = snapshot.ByteLength;
			tab.ConflictModifiedTime = snapshot.ModifiedTime;
			if (num)
			{
				ExternalConflictDetectedCount++;
			}
			if (tab.FilePath == _currentTabKey)
			{
				UpdateExternalConflictSurface(tab);
			}
			UpdateTabTitle();
			UpdateScriptList();
		}
	}

	private void ClearExternalConflict(ScriptTabInfo tab, bool updateSurface = true)
	{
		if (tab != null)
		{
			tab.HasExternalConflict = false;
			tab.ConflictDiskExists = false;
			tab.ConflictDiskText = "";
			tab.ConflictDiskContentHash = "";
			tab.ConflictDiskByteLength = 0L;
			tab.ConflictModifiedTime = 0uL;
			if (updateSurface && tab.FilePath == _currentTabKey)
			{
				UpdateExternalConflictSurface(tab);
			}
		}
	}

	private void UpdateExternalConflictSurface(ScriptTabInfo tab)
	{
		if (!GodotObject.IsInstanceValid(_externalConflictBar))
		{
			return;
		}
		bool flag = tab != null && tab.HasExternalConflict && !tab.IsBlueprintGenerated;
		_externalConflictBar.Visible = flag;
		if (!flag)
		{
			if (GodotObject.IsInstanceValid(_externalConflictDiffWindow))
			{
				_externalConflictDiffWindow.Hide();
			}
			return;
		}
		_externalConflictTitleLabel.Text = (tab.ConflictDiskExists ? "检测到外部修改 · 尚未写入磁盘" : "磁盘文件已被外部删除 · 尚未重新创建");
		string value = ((tab.ConflictModifiedTime != 0) ? DateTimeOffset.FromUnixTimeSeconds((long)tab.ConflictModifiedTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "未知时间");
		_externalConflictDetailLabel.Text = (tab.ConflictDiskExists ? $"{tab.FilePath.GetFile()} · {tab.ConflictDiskByteLength} 字节 · {value} · 两个版本均已保留" : (tab.FilePath.GetFile() + " · 编辑器缓冲区仍保留；重新载入不可用"));
		_reloadExternalButton.Disabled = !tab.ConflictDiskExists;
	}

	private ScriptSaveStatus RefreshTabExternalState(ScriptTabInfo tab, bool reloadVisible)
	{
		if (tab == null || tab.IsBlueprintGenerated)
		{
			return ScriptSaveStatus.Unchanged;
		}
		if (!TryReadDiskSnapshot(tab.FilePath, out var snapshot, out var error))
		{
			LastSaveError = error;
			return ScriptSaveStatus.IoError;
		}
		if (DiskSnapshotMatchesBaseline(tab, snapshot))
		{
			if (tab.HasExternalConflict)
			{
				ClearExternalConflict(tab);
			}
			return ScriptSaveStatus.Unchanged;
		}
		if (tab.HasUnsavedChanges || !snapshot.Exists)
		{
			MarkExternalConflict(tab, snapshot);
			return ScriptSaveStatus.Conflict;
		}
		ApplyDiskSnapshotToTab(tab, snapshot);
		CleanExternalReloadCount++;
		if (reloadVisible && tab.FilePath == _currentTabKey)
		{
			LoadTabData(tab.FilePath);
		}
		UpdateScriptList();
		XWEditorInterface.Instance?.ShowToast("已载入外部修改：" + tab.FilePath.GetFile());
		return ScriptSaveStatus.Unchanged;
	}

	private ScriptSaveStatus SaveTabWithResult(string tabKey, bool overwriteExternalConflict)
	{
		LastSaveError = "";
		if (!_openTabs.TryGetValue(tabKey, out var value))
		{
			return LastSaveStatus = ScriptSaveStatus.IoError;
		}
		if (value.IsBlueprintGenerated)
		{
			XWEditorInterface.Instance?.ShowToast("蓝图生成代码是只读构建产物；请返回源蓝图修改。", 1);
			return LastSaveStatus = ScriptSaveStatus.ReadOnly;
		}
		string text = ((tabKey == _currentTabKey) ? _codeEdit.Text : value.Text);
		if (!TryReadDiskSnapshot(value.FilePath, out var snapshot, out var error))
		{
			LastSaveError = error;
			XWEditorInterface.Instance?.ShowToast("读取磁盘版本失败：" + value.FilePath.GetFile(), 2);
			return LastSaveStatus = ScriptSaveStatus.IoError;
		}
		if (!overwriteExternalConflict && !DiskSnapshotMatchesBaseline(value, snapshot))
		{
			if (!value.HasUnsavedChanges && snapshot.Exists)
			{
				ApplyDiskSnapshotToTab(value, snapshot);
				CleanExternalReloadCount++;
				if (tabKey == _currentTabKey)
				{
					LoadTabData(tabKey);
				}
				UpdateScriptList();
				return LastSaveStatus = ScriptSaveStatus.Unchanged;
			}
			MarkExternalConflict(value, snapshot);
			return LastSaveStatus = ScriptSaveStatus.Conflict;
		}
		if (!TryAtomicWriteScript(value.FilePath, text, snapshot, out var error2, out var failureKind))
		{
			LastSaveError = error2;
			if (failureKind == AtomicWriteFailureKind.Conflict)
			{
				if (TryReadDiskSnapshot(value.FilePath, out var snapshot2, out var _))
				{
					MarkExternalConflict(value, snapshot2);
				}
				XWEditorInterface.Instance?.ShowToast("保存已停止：" + value.FilePath.GetFile() + " 在写入前再次发生外部修改。", 1);
				return LastSaveStatus = ScriptSaveStatus.Conflict;
			}
			XWEditorInterface.Instance?.ShowToast("保存失败：" + value.FilePath.GetFile() + " · " + error2, 2);
			return LastSaveStatus = ((failureKind == AtomicWriteFailureKind.ReadOnly) ? ScriptSaveStatus.ReadOnly : ScriptSaveStatus.IoError);
		}
		ScriptDiskSnapshot snapshot3 = BuildSavedSnapshot(value.FilePath, text);
		AtomicSaveCount++;
		if (overwriteExternalConflict)
		{
			ExternalConflictOverwriteCount++;
		}
		value.Text = text;
		value.HasUnsavedChanges = false;
		ApplyDiskBaseline(value, snapshot3);
		ClearExternalConflict(value);
		RefreshGeneratedMetadata(value);
		XWCSharpCodeModel.RequestSourceUpdate(value.FilePath, text);
		XWBPCSharpMemberRegistry.Instance.NotifyScriptSaved(value.FilePath);
		if (tabKey == _currentTabKey)
		{
			UpdateTabTitle();
		}
		UpdateScriptList();
		return LastSaveStatus = ScriptSaveStatus.Saved;
	}

	private static bool TryAtomicWriteScript(string path, string text, ScriptDiskSnapshot expectedDisk, out string error, out AtomicWriteFailureKind failureKind)
	{
		error = "";
		failureKind = AtomicWriteFailureKind.None;
		string text2 = "";
		try
		{
			string text3 = ResolveAbsoluteScriptPath(path);
			string text4 = Path.GetDirectoryName(text3) ?? "";
			if (string.IsNullOrWhiteSpace(text4) || !Directory.Exists(text4))
			{
				error = "目标目录不存在";
				failureKind = AtomicWriteFailureKind.IoError;
				return false;
			}
			if (File.Exists(text3) && (File.GetAttributes(text3) & FileAttributes.ReadOnly) != FileAttributes.None)
			{
				error = "目标文件为只读";
				failureKind = AtomicWriteFailureKind.ReadOnly;
				return false;
			}
			text2 = Path.Combine(text4, $".{Path.GetFileName(text3)}.{Guid.NewGuid():N}.saving");
			using (FileStream fileStream = new FileStream(text2, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
			{
				using StreamWriter streamWriter = new StreamWriter(fileStream, ScriptUtf8, 4096, leaveOpen: true);
				streamWriter.Write(text ?? "");
				streamWriter.Flush();
				fileStream.Flush(flushToDisk: true);
			}
			if (expectedDisk.Exists)
			{
				if (!File.Exists(text3))
				{
					error = "目标文件在写入前已被删除";
					failureKind = AtomicWriteFailureKind.Conflict;
					return false;
				}
				using FileStream fileStream2 = new FileStream(text3, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read | FileShare.Delete);
				using StreamReader streamReader = new StreamReader(fileStream2, ScriptUtf8, detectEncodingFromByteOrderMarks: true, 4096, leaveOpen: true);
				string text5 = streamReader.ReadToEnd();
				if (fileStream2.Length != expectedDisk.ByteLength || !string.Equals(ComputeScriptContentHash(text5), expectedDisk.ContentHash, StringComparison.Ordinal))
				{
					error = "目标文件在写入前再次发生外部修改";
					failureKind = AtomicWriteFailureKind.Conflict;
					return false;
				}
				File.Replace(text2, text3, null, ignoreMetadataErrors: true);
			}
			else
			{
				if (File.Exists(text3))
				{
					error = "目标路径在写入前已被其他程序创建";
					failureKind = AtomicWriteFailureKind.Conflict;
					return false;
				}
				File.Move(text2, text3);
			}
			text2 = "";
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			if (failureKind == AtomicWriteFailureKind.None)
			{
				failureKind = ((File.Exists(ResolveAbsoluteScriptPath(path)) != expectedDisk.Exists) ? AtomicWriteFailureKind.Conflict : AtomicWriteFailureKind.IoError);
			}
			return false;
		}
		finally
		{
			if (!string.IsNullOrWhiteSpace(text2))
			{
				try
				{
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
				}
				catch
				{
				}
			}
		}
	}

	public bool ReloadCurrentExternalConflict()
	{
		if (!TryGetCurrentTab(out var tab) || !tab.HasExternalConflict)
		{
			return false;
		}
		if (!TryReadDiskSnapshot(tab.FilePath, out var snapshot, out var error) || !snapshot.Exists)
		{
			LastSaveError = (string.IsNullOrWhiteSpace(error) ? "磁盘文件不存在" : error);
			XWEditorInterface.Instance?.ShowToast("无法重新载入：磁盘文件不存在或不可读。", 2);
			return false;
		}
		ApplyDiskSnapshotToTab(tab, snapshot);
		ExternalConflictReloadCount++;
		LoadTabData(tab.FilePath);
		UpdateScriptList();
		XWEditorInterface.Instance?.ShowToast("已载入磁盘版本；编辑器旧缓冲区未写入磁盘。");
		return true;
	}

	public bool OverwriteCurrentExternalConflict()
	{
		if (!TryGetCurrentTab(out var tab) || !tab.HasExternalConflict)
		{
			return false;
		}
		SaveCurrentTabState();
		return SaveTabWithResult(tab.FilePath, overwriteExternalConflict: true) == ScriptSaveStatus.Saved;
	}

	public void ShowCurrentExternalConflictDiff()
	{
		if (TryGetCurrentTab(out var tab) && tab.HasExternalConflict)
		{
			SaveCurrentTabState();
			_externalConflictEditorText.Text = tab.Text ?? "";
			_externalConflictDiskText.Text = (tab.ConflictDiskExists ? (tab.ConflictDiskText ?? "") : "（磁盘文件已被删除）");
			_externalConflictDiffWindow.Title = "C# 外部修改差异 · " + tab.FilePath.GetFile();
			_externalConflictDiffWindow.PopupCentered();
		}
	}

	public int ScanExternalChangesNowForProbe()
	{
		SaveCurrentTabState();
		int num = 0;
		foreach (ScriptTabInfo value in _openTabs.Values)
		{
			if (!value.IsBlueprintGenerated)
			{
				ExternalConflictWatcherScanCount++;
				RefreshTabExternalState(value, value.FilePath == _currentTabKey);
				if (value.HasExternalConflict)
				{
					num++;
				}
			}
		}
		return num;
	}

	public ScriptSaveStatus SaveCurrentFileAsForProbe(string path)
	{
		path = NormalizeNewCSharpPath(path);
		if (!IsSupportedScriptPath(path))
		{
			return LastSaveStatus = ScriptSaveStatus.IoError;
		}
		return SaveCurrentTabAsAtomic(path);
	}

	private void SetExternalConflictWatcherActive(bool active)
	{
		if (!GodotObject.IsInstanceValid(_externalConflictWatcher))
		{
			return;
		}
		if (active)
		{
			_externalConflictWatcher.ProcessMode = ProcessModeEnum.Inherit;
			if (_externalConflictWatcher.IsStopped())
			{
				_externalConflictWatcher.Start();
			}
		}
		else
		{
			_externalConflictWatcher.Stop();
			_externalConflictWatcher.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	private void ScanExternalChangesFromWatcher()
	{
		if (!_workspaceActive || _externalConflictWatcherScanning)
		{
			return;
		}
		_externalConflictWatcherScanning = true;
		try
		{
			SaveCurrentTabState();
			foreach (ScriptTabInfo value in _openTabs.Values)
			{
				if (!value.IsBlueprintGenerated && DiskMetadataMayHaveChanged(value))
				{
					ExternalConflictWatcherScanCount++;
					RefreshTabExternalState(value, value.FilePath == _currentTabKey);
				}
			}
		}
		finally
		{
			_externalConflictWatcherScanning = false;
		}
	}

	private static bool DiskMetadataMayHaveChanged(ScriptTabInfo tab)
	{
		try
		{
			string text = ResolveAbsoluteScriptPath(tab.FilePath);
			bool flag = File.Exists(text);
			bool flag2 = (tab.HasExternalConflict ? tab.ConflictDiskExists : tab.DiskExists);
			if (flag != flag2)
			{
				return true;
			}
			if (!flag)
			{
				return false;
			}
			FileInfo fileInfo = new FileInfo(text);
			long num = (tab.HasExternalConflict ? tab.ConflictDiskByteLength : tab.DiskByteLength);
			ulong num2 = (tab.HasExternalConflict ? tab.ConflictModifiedTime : tab.ModifiedTime);
			return fileInfo.Length != num || Godot.FileAccess.GetModifiedTime(tab.FilePath) != num2;
		}
		catch
		{
			return true;
		}
	}

	private ScriptSaveStatus SaveCurrentTabAsAtomic(string path)
	{
		LastSaveError = "";
		if (!TryGetCurrentTab(out var tab) || tab.IsBlueprintGenerated)
		{
			return LastSaveStatus = ScriptSaveStatus.ReadOnly;
		}
		if (_openTabs.ContainsKey(path) && !string.Equals(path, _currentTabKey, StringComparison.OrdinalIgnoreCase))
		{
			LastSaveError = "目标文件已在另一个标签中打开";
			SaveAsRollbackCount++;
			return LastSaveStatus = ScriptSaveStatus.IoError;
		}
		SaveCurrentTabState();
		string currentTabKey = _currentTabKey;
		string text = tab.Text;
		if (!TryReadDiskSnapshot(path, out var snapshot, out var error))
		{
			LastSaveError = error;
			SaveAsRollbackCount++;
			return LastSaveStatus = ScriptSaveStatus.IoError;
		}
		if (!TryAtomicWriteScript(path, text, snapshot, out var error2, out var failureKind))
		{
			LastSaveError = error2;
			SaveAsRollbackCount++;
			XWEditorInterface.Instance?.ShowToast("另存为失败；原标签保持不变：" + LastSaveError, 2);
			return LastSaveStatus = failureKind switch
			{
				AtomicWriteFailureKind.Conflict => ScriptSaveStatus.Conflict, 
				AtomicWriteFailureKind.ReadOnly => ScriptSaveStatus.ReadOnly, 
				_ => ScriptSaveStatus.IoError, 
			};
		}
		ScriptDiskSnapshot snapshot2 = BuildSavedSnapshot(path, text);
		int num = _tabKeys.IndexOf(currentTabKey);
		_openTabs.Remove(currentTabKey);
		_openTabs[path] = tab;
		if (num >= 0)
		{
			_tabKeys[num] = path;
		}
		tab.FilePath = path;
		tab.FileExtension = path.GetExtension();
		tab.Text = text;
		tab.HasUnsavedChanges = false;
		ApplyDiskBaseline(tab, snapshot2);
		ClearExternalConflict(tab, updateSurface: false);
		_currentTabKey = path;
		AtomicSaveCount++;
		XWCSharpCodeModel.RequestSourceUpdate(path, text);
		XWBPCSharpMemberRegistry.Instance.NotifyScriptSaved(path);
		LoadTabData(path);
		UpdateScriptList();
		return LastSaveStatus = ScriptSaveStatus.Saved;
	}

	private void InitializeIdeFeatures()
	{
		_debugHost = XWScriptDebugHost.GetOrCreate(this);
		if (!GodotObject.IsInstanceValid(_debugHost))
		{
			throw new InvalidOperationException("Could not create the ModEditor script debug host.");
		}
		_debugPreviewRoot = _debugHost.CreatePreviewRoot();
		_debugSession = new XWScriptDebugSession(_debugHost, _debugPreviewRoot);
		InitializeDebugWorkbench();
		_definitionButton = GetNode<Button>("%DefinitionButton");
		_referencesButton = GetNode<Button>("%ReferencesButton");
		_renameButton = GetNode<Button>("%RenameButton");
		_renameUndoButton = GetNodeOrNull<Button>("%RenameUndoButton");
		_renameRedoButton = GetNodeOrNull<Button>("%RenameRedoButton");
		_formatButton = GetNode<Button>("%FormatButton");
		_quickFixButton = GetNode<Button>("%QuickFixButton");
		_debugStartButton = GetNode<Button>("%DebugStartButton");
		_debugStopButton = GetNode<Button>("%DebugStopButton");
		_debugStatusLabel = GetNode<Label>("%DebugStatusLabel");
		_referencesWindow = GetNode<Window>("%ReferencesWindow");
		_referencesHeader = GetNode<Label>("%ReferencesHeader");
		_referencesList = GetNode<ItemList>("%ReferencesList");
		_renameDialog = GetNode<ConfirmationDialog>("%RenameDialog");
		_renameInput = GetNode<LineEdit>("%RenameInput");
		_renamePreviewLabel = GetNode<RichTextLabel>("%RenamePreviewLabel");
		_symbolInfoPopup = GetNode<PopupPanel>("%SymbolInfoPopup");
		_symbolInfoLabel = GetNode<RichTextLabel>("%SymbolInfoLabel");
		_quickFixPopup = GetNode<PopupMenu>("%QuickFixPopup");
		XWClassRegistry instance = XWClassRegistry.Instance;
		_definitionButton.Icon = instance.GetUIIcon("MemberMethod");
		_referencesButton.Icon = instance.GetUIIcon("Search");
		_renameButton.Icon = instance.GetUIIcon("Rename");
		if (_renameUndoButton != null)
		{
			_renameUndoButton.Icon = instance.GetUIIcon("Back");
		}
		if (_renameRedoButton != null)
		{
			_renameRedoButton.Icon = instance.GetUIIcon("Forward");
		}
		_formatButton.Icon = instance.GetUIIcon("CombineLines");
		_quickFixButton.Icon = instance.GetUIIcon("StatusWarning");
		_debugStartButton.Icon = instance.GetUIIcon("Debug");
		_debugStopButton.Icon = instance.GetUIIcon("Stop");
		_hoverTimer = new Godot.Timer
		{
			OneShot = true,
			WaitTime = 0.45
		};
		_hoverTimer.Timeout += RequestHoverFromTimer;
		AddChild(_hoverTimer, forceReadableName: false, InternalMode.Disabled);
		_renamePreviewTimer = new Godot.Timer
		{
			OneShot = true,
			WaitTime = 0.28
		};
		_renamePreviewTimer.Timeout += RunDebouncedRenamePreview;
		AddChild(_renamePreviewTimer, forceReadableName: false, InternalMode.Disabled);
		_definitionButton.Pressed += delegate
		{
			NavigateToDefinitionAtCaretAsync();
		};
		_referencesButton.Pressed += delegate
		{
			ShowReferencesAtCaretAsync();
		};
		_renameButton.Pressed += delegate
		{
			ShowRenameDialogAtCaretAsync();
		};
		if (_renameUndoButton != null)
		{
			_renameUndoButton.Pressed += delegate
			{
				UndoLastProjectRenameAsync();
			};
		}
		if (_renameRedoButton != null)
		{
			_renameRedoButton.Pressed += delegate
			{
				RedoLastProjectRenameAsync();
			};
		}
		_formatButton.Pressed += delegate
		{
			FormatDocumentOrSelection();
		};
		_quickFixButton.Pressed += ShowQuickFixMenu;
		_debugStartButton.Pressed += delegate
		{
			StartDebugSessionAsync();
		};
		_debugStopButton.Pressed += async delegate
		{
			await StopDebugSessionAsync();
		};
		_referencesList.ItemActivated += JumpToReference;
		_referencesWindow.CloseRequested += _referencesWindow.Hide;
		_renameInput.TextChanged += delegate
		{
			RequestRenamePreview();
		};
		_renameDialog.Confirmed += delegate
		{
			ApplyPendingRenameAsync();
		};
		_quickFixPopup.IdPressed += delegate
		{
			ApplyPendingQuickFix();
		};
		_codeEdit.GuiInput += OnCodeEditIdeGuiInput;
		_codeEdit.TextChanged += InvalidateIdeRequests;
		_scriptTabBar.TabChanged += delegate
		{
			InvalidateIdeRequests();
		};
		UpdateDebugControls();
	}

	private void SuspendIdeBackgroundWork()
	{
		InvalidateIdeRequests();
		_hoverTimer?.Stop();
		_renamePreviewTimer?.Stop();
		if (_hoverTimer != null)
		{
			_hoverTimer.ProcessMode = ProcessModeEnum.Disabled;
		}
		if (_renamePreviewTimer != null)
		{
			_renamePreviewTimer.ProcessMode = ProcessModeEnum.Disabled;
		}
		if (GodotObject.IsInstanceValid(_symbolInfoPopup))
		{
			_symbolInfoPopup.Hide();
		}
	}

	private void ResumeIdeBackgroundWork()
	{
		if (_hoverTimer != null)
		{
			_hoverTimer.ProcessMode = ProcessModeEnum.Inherit;
		}
		if (_renamePreviewTimer != null)
		{
			_renamePreviewTimer.ProcessMode = ProcessModeEnum.Inherit;
		}
	}

	private bool HandleIdeShortcut(InputEventKey key)
	{
		if (key.Keycode == Key.F12 && key.ShiftPressed)
		{
			Button referencesButton = _referencesButton;
			if (referencesButton != null && referencesButton.Disabled)
			{
				return false;
			}
			ShowReferencesAtCaretAsync();
			return true;
		}
		if (key.Keycode == Key.F12)
		{
			Button definitionButton = _definitionButton;
			if (definitionButton != null && definitionButton.Disabled)
			{
				return false;
			}
			NavigateToDefinitionAtCaretAsync();
			return true;
		}
		if (key.Keycode == Key.F2)
		{
			Button renameButton = _renameButton;
			if (renameButton != null && renameButton.Disabled)
			{
				return false;
			}
			ShowRenameDialogAtCaretAsync();
			return true;
		}
		if (key.Keycode == Key.F && key.AltPressed && key.ShiftPressed)
		{
			FormatDocumentOrSelection();
			return true;
		}
		if (key.Keycode == Key.Period && key.CtrlPressed)
		{
			ShowQuickFixMenu();
			return true;
		}
		if (key.Keycode == Key.F9)
		{
			_codeEdit.ToggleBreakpoint(_codeEdit.GetCaretLine());
			return true;
		}
		if (key.Keycode == Key.F5 && key.ShiftPressed)
		{
			Button debugStopButton = _debugStopButton;
			if (debugStopButton != null && debugStopButton.Disabled)
			{
				return false;
			}
			StopDebugSessionAsync();
			return true;
		}
		if (key.Keycode == Key.F5)
		{
			Button debugStartButton = _debugStartButton;
			if (debugStartButton != null && debugStartButton.Disabled)
			{
				return false;
			}
			StartDebugSessionAsync();
			return true;
		}
		if (key.Keycode == Key.Z && key.CtrlPressed && key.AltPressed)
		{
			UndoLastProjectRenameAsync();
			return true;
		}
		if (key.Keycode == Key.Y && key.CtrlPressed && key.AltPressed)
		{
			RedoLastProjectRenameAsync();
			return true;
		}
		return false;
	}

	public async Task<bool> NavigateToDefinitionAtCaretAsync()
	{
		IdeSymbolQueryResult ideSymbolQueryResult = await QuerySymbolAtCaretAsync(includeReferences: false, IdeRequestKind.Definition);
		if (!ideSymbolQueryResult.IsCurrent)
		{
			return false;
		}
		XWCSharpIdeService.SymbolInfoResult info = ideSymbolQueryResult.Info;
		if (info?.Definition == null)
		{
			XWEditorInterface.Instance?.ShowToast("当前符号没有可跳转的 Mod 源码定义");
			return false;
		}
		OpenFileAt(info.Definition.FilePath, info.Definition.Line, info.Definition.Column);
		return true;
	}

	public async Task<int> ShowReferencesAtCaretAsync()
	{
		IdeSymbolQueryResult ideSymbolQueryResult = await QuerySymbolAtCaretAsync(includeReferences: true, IdeRequestKind.References);
		if (!ideSymbolQueryResult.IsCurrent)
		{
			return 0;
		}
		XWCSharpIdeService.SymbolInfoResult info = ideSymbolQueryResult.Info;
		_referenceLocations.Clear();
		_referencesList.Clear();
		if (info == null)
		{
			XWEditorInterface.Instance?.ShowToast("光标处没有可解析的 C# 符号");
			return 0;
		}
		_referenceLocations.AddRange(info.References);
		_referencesHeader.Text = $"{info.Display} · {_referenceLocations.Count} 处（仅当前 Mod）";
		string projectRoot = ideSymbolQueryResult.Stamp.ProjectRoot;
		foreach (XWCSharpIdeService.LocationResult referenceLocation in _referenceLocations)
		{
			string value = SafeRelativePath(projectRoot, referenceLocation.FilePath);
			_referencesList.AddItem($"{value}:{referenceLocation.Line + 1}:{referenceLocation.Column + 1}   {referenceLocation.Preview}");
		}
		_referencesWindow.PopupCentered();
		return _referenceLocations.Count;
	}

	private void JumpToReference(long index)
	{
		if (index >= 0 && index < _referenceLocations.Count)
		{
			XWCSharpIdeService.LocationResult locationResult = _referenceLocations[(int)index];
			OpenFileAt(locationResult.FilePath, locationResult.Line, locationResult.Column);
			_referencesWindow.Hide();
		}
	}

	internal async Task<XWCSharpIdeService.SymbolInfoResult> GetHoverAndSignatureAtCaretAsync()
	{
		IdeSymbolQueryResult ideSymbolQueryResult = await QuerySymbolAtCaretAsync(includeReferences: false, IdeRequestKind.HoverSignature);
		return ideSymbolQueryResult.IsCurrent ? ideSymbolQueryResult.Info : null;
	}

	private async Task<IdeSymbolQueryResult> QuerySymbolAtCaretAsync(bool includeReferences, IdeRequestKind requestKind)
	{
		if (!TryCaptureIdeRequest(out var stamp, out var documents))
		{
			return default(IdeSymbolQueryResult);
		}
		try
		{
			XWCSharpIdeService.SymbolInfoResult symbolInfoResult = ((!includeReferences) ? (await XWCSharpIdeService.GetSymbolInfoAsync(stamp.ProjectRoot, stamp.FilePath, stamp.Source, stamp.Offset, documents)) : (await XWCSharpIdeService.FindReferencesAsync(stamp.ProjectRoot, stamp.FilePath, stamp.Source, stamp.Offset, documents)));
			XWCSharpIdeService.SymbolInfoResult info = symbolInfoResult;
			await WaitForIdeResultGateForProbeAsync(requestKind, stamp.FilePath);
			if (!IsIdeRequestCurrent(stamp, requireCaretOffset: true))
			{
				return default(IdeSymbolQueryResult);
			}
			LastSymbolInfo = info;
			return new IdeSymbolQueryResult(isCurrent: true, stamp, info);
		}
		catch (Exception ex)
		{
			if (IsIdeRequestCurrent(stamp, requireCaretOffset: true))
			{
				XWEditorInterface.Instance?.ShowToast(ex.GetBaseException().Message, 1);
				return new IdeSymbolQueryResult(isCurrent: true, stamp, null);
			}
			return default(IdeSymbolQueryResult);
		}
	}

	private void OnCodeEditIdeGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion inputEventMouseMotion && HasFileOpen())
		{
			Vector2I lineColumnAtPos = _codeEdit.GetLineColumnAtPos(new Vector2I(Mathf.RoundToInt(inputEventMouseMotion.Position.X), Mathf.RoundToInt(inputEventMouseMotion.Position.Y)), allowOutOfBounds: true);
			_hoverLocalPosition = inputEventMouseMotion.Position;
			_hoverLine = Mathf.Clamp(lineColumnAtPos.X, 0, Math.Max(0, _codeEdit.GetLineCount() - 1));
			_hoverColumn = Mathf.Clamp(lineColumnAtPos.Y, 0, _codeEdit.GetLine(_hoverLine).Length);
			_hoverGeneration++;
			_hoverTimer.Stop();
			_hoverTimer.Start();
			if (_symbolInfoPopup.Visible)
			{
				_symbolInfoPopup.Hide();
			}
		}
	}

	private void RequestHoverFromTimer()
	{
		RequestHoverAsync(_hoverGeneration);
	}

	private async Task<bool> RequestHoverAsync(int generation)
	{
		if (!TryGetIdeContext(out var _, out var _, out var source, out var _))
		{
			return false;
		}
		int textOffset = XWCSharpIdeService.GetTextOffset(source, _hoverLine, _hoverColumn);
		if (!TryCaptureIdeRequest(out var stamp, out var documents, textOffset))
		{
			return false;
		}
		Vector2 popupPosition = _hoverLocalPosition;
		XWCSharpIdeService.SymbolInfoResult info;
		try
		{
			info = await XWCSharpIdeService.GetSymbolInfoAsync(stamp.ProjectRoot, stamp.FilePath, stamp.Source, stamp.Offset, documents);
			await WaitForIdeResultGateForProbeAsync(IdeRequestKind.HoverPopup, stamp.FilePath);
		}
		catch
		{
			return false;
		}
		if (generation != _hoverGeneration || info == null || !IsIdeRequestCurrent(stamp, requireCaretOffset: false))
		{
			return false;
		}
		LastSymbolInfo = info;
		_symbolInfoLabel.Text = BuildSymbolInfoText(info);
		Vector2 vector = _codeEdit.GetScreenPosition() + popupPosition + new Vector2(14f, 20f);
		_symbolInfoPopup.Popup(new Rect2I((Vector2I)vector, new Vector2I(520, 160)));
		return true;
	}

	internal Task<bool> RequestHoverForProbeAsync(int line, int column)
	{
		if (_codeEdit == null)
		{
			return Task.FromResult(result: false);
		}
		_hoverLine = Mathf.Clamp(line, 0, Math.Max(0, _codeEdit.GetLineCount() - 1));
		_hoverColumn = Mathf.Clamp(column, 0, _codeEdit.GetLine(_hoverLine).Length);
		_hoverGeneration++;
		return RequestHoverAsync(_hoverGeneration);
	}

	internal async Task<XWCSharpIdeService.RenamePreview> PreviewRenameAtCaretAsync(string newName)
	{
		int generation = BeginRenamePreviewRequest();
		return await PreviewRenameAtCaretAsync(newName, generation);
	}

	private async Task<XWCSharpIdeService.RenamePreview> PreviewRenameAtCaretAsync(string newName, int generation)
	{
		if (!TryCaptureIdeRequest(out var stamp, out var documents))
		{
			return null;
		}
		try
		{
			XWCSharpIdeService.RenamePreview preview = await XWCSharpIdeService.PreviewRenameAsync(stamp.ProjectRoot, stamp.FilePath, stamp.Source, stamp.Offset, newName, documents);
			await WaitForIdeResultGateForProbeAsync(IdeRequestKind.RenamePreview, newName);
			if (generation != _renameGeneration || !IsIdeRequestCurrent(stamp, requireCaretOffset: true))
			{
				return null;
			}
			_pendingRename = preview;
			_pendingRenameStamp = stamp;
			_pendingRenameGeneration = generation;
			return preview;
		}
		catch (Exception ex)
		{
			if (generation == _renameGeneration && IsIdeRequestCurrent(stamp, requireCaretOffset: true))
			{
				XWEditorInterface.Instance?.ShowToast(ex.GetBaseException().Message, 1);
			}
			return null;
		}
	}

	private async Task ShowRenameDialogAtCaretAsync()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return;
		}
		IdeSymbolQueryResult ideSymbolQueryResult = await QuerySymbolAtCaretAsync(includeReferences: false, IdeRequestKind.RenameSymbol);
		if (ideSymbolQueryResult.IsCurrent)
		{
			XWCSharpIdeService.SymbolInfoResult info = ideSymbolQueryResult.Info;
			if (info == null || info.Definition == null)
			{
				XWEditorInterface.Instance?.ShowToast("只能重命名当前 Mod 工程中定义的符号");
				return;
			}
			_pendingRename = null;
			_pendingRenameStamp = null;
			_renameInput.Text = info.Name;
			_renameInput.SelectAll();
			_renamePreviewLabel.Text = "[b]" + EscapeBbcode(info.Display) + "[/b]\n输入新名称后显示受影响文件与引用。";
			_renameDialog.GetOkButton().Disabled = true;
			_renameDialog.PopupCentered();
			_renameInput.GrabFocus();
		}
	}

	private void RequestRenamePreview()
	{
		if (_renameDialog.Visible)
		{
			_pendingRenameText = _renameInput.Text;
			BeginRenamePreviewRequest();
			_renamePreviewTimer.Stop();
			_renamePreviewTimer.Start();
		}
	}

	private void RunDebouncedRenamePreview()
	{
		if (_renameDialog.Visible)
		{
			int renameGeneration = _renameGeneration;
			UpdateRenamePreviewAsync(renameGeneration, _pendingRenameText);
		}
	}

	private async Task UpdateRenamePreviewAsync(int generation, string newName)
	{
		XWCSharpIdeService.RenamePreview preview = await PreviewRenameAtCaretAsync(newName, generation);
		if (!GodotObject.IsInstanceValid(this) || generation != _renameGeneration)
		{
			return;
		}
		XWCSharpIdeService.RenamePreview renamePreview = preview;
		if (renamePreview != null && renamePreview.ReadOnlyGeneratedDocuments.Count > 0)
		{
			_renameDialog.GetOkButton().Disabled = true;
			_renamePreviewLabel.Text = "[color=#ff8f70][b]重命名涉及蓝图生成代码，操作已中止[/b][/color]\n请先返回对应蓝图修改名称并重新生成：\n" + string.Join("\n", preview.ReadOnlyGeneratedDocuments.Select((string path) => "• " + EscapeBbcode(SafeRelativePath(preview.ProjectRoot, path))));
			return;
		}
		bool flag = (preview?.IsValid ?? false) && !string.Equals(preview.OldName, preview.NewName, StringComparison.Ordinal);
		List<string> list = (flag ? GetDirtyAffectedDocuments(preview) : new List<string>());
		if (list.Count > 0)
		{
			flag = false;
		}
		_renameDialog.GetOkButton().Disabled = !flag;
		if (list.Count > 0)
		{
			_renamePreviewLabel.Text = "[color=#ff8f70][b]受影响的标签仍有未保存修改[/b][/color]\n请先保存这些文件，再重新预览重命名：\n" + string.Join("\n", list.Select((string path) => "• " + EscapeBbcode(SafeRelativePath(GetActiveModProjectRoot(), path))));
			return;
		}
		if (!flag)
		{
			_renamePreviewLabel.Text = "请输入有效且不同的 C# 标识符。";
			return;
		}
		_renamePreviewLabel.Text = $"[b]{EscapeBbcode(preview.SymbolDisplay)}[/b]\n[color=#e9c46a]{preview.OldName} → {preview.NewName}[/color]\n{preview.UpdatedDocuments.Count} 个文件，{preview.Occurrences.Count} 处引用\n\n" + string.Join("\n", preview.UpdatedDocuments.Keys.Select((string path) => "• " + EscapeBbcode(SafeRelativePath(preview.ProjectRoot, path))));
	}

	public async Task<bool> ApplyPendingRenameAsync()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return false;
		}
		if (XWScriptCompiler.IsModBuildBusy(GetActiveModProjectRoot()) || (_debugSession?.HasLiveSession ?? false))
		{
			XWEditorInterface.Instance?.ShowToast("构建或调试期间不能应用项目重命名。", 1);
			UpdateCompileControls();
			return false;
		}
		XWCSharpIdeService.RenamePreview preview = _pendingRename;
		IdeRequestStamp? pendingRenameStamp = _pendingRenameStamp;
		if (preview == null || !preview.IsValid || !pendingRenameStamp.HasValue || _pendingRenameGeneration != _renameGeneration || !IsIdeRequestCurrent(pendingRenameStamp.GetValueOrDefault(), requireCaretOffset: true) || (_renameDialog.Visible && !string.Equals(_renameInput.Text, preview.NewName, StringComparison.Ordinal)))
		{
			_pendingRename = null;
			_pendingRenameStamp = null;
			if (GodotObject.IsInstanceValid(_renameDialog))
			{
				_renameDialog.GetOkButton().Disabled = true;
			}
			XWEditorInterface.Instance?.ShowToast("重命名预览已过期，请重新预览。", 1);
			return false;
		}
		if (GetDirtyAffectedDocuments(preview).Count > 0)
		{
			XWEditorInterface.Instance?.ShowToast("受影响脚本仍有未保存修改；请先保存后重新预览。", 1);
			return false;
		}
		bool flag = await XWCSharpIdeService.ApplyRenameAsync(preview);
		if (!GodotObject.IsInstanceValid(this))
		{
			return false;
		}
		if (flag)
		{
			ReloadOpenDocumentsFromDisk();
			UpdateCompileControls();
			XWEditorInterface.Instance?.ShowToast($"已重命名 {preview.OldName} → {preview.NewName}；Ctrl+Alt+Z 可恢复");
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast(XWCSharpIdeService.LastRenameError, 2);
		}
		return flag;
	}

	public async Task<bool> UndoLastProjectRenameAsync()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return false;
		}
		string activeModProjectRoot = GetActiveModProjectRoot();
		if (XWScriptCompiler.IsModBuildBusy(activeModProjectRoot) || (_debugSession?.HasLiveSession ?? false))
		{
			XWEditorInterface.Instance?.ShowToast("构建或调试期间不能撤销项目重命名。", 1);
			UpdateCompileControls();
			return false;
		}
		bool num = await XWCSharpIdeService.UndoLastRenameAsync(activeModProjectRoot);
		if (num && GodotObject.IsInstanceValid(this))
		{
			ReloadOpenDocumentsFromDisk();
			UpdateCompileControls();
			XWEditorInterface.Instance?.ShowToast("已撤销项目重命名；Ctrl+Alt+Y 可重做");
		}
		return num;
	}

	public async Task<bool> RedoLastProjectRenameAsync()
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return false;
		}
		string activeModProjectRoot = GetActiveModProjectRoot();
		if (XWScriptCompiler.IsModBuildBusy(activeModProjectRoot) || (_debugSession?.HasLiveSession ?? false))
		{
			XWEditorInterface.Instance?.ShowToast("构建或调试期间不能重做项目重命名。", 1);
			UpdateCompileControls();
			return false;
		}
		bool num = await XWCSharpIdeService.RedoLastRenameAsync(activeModProjectRoot);
		if (num && GodotObject.IsInstanceValid(this))
		{
			ReloadOpenDocumentsFromDisk();
			UpdateCompileControls();
			XWEditorInterface.Instance?.ShowToast("已重做项目重命名");
		}
		return num;
	}

	public bool FormatDocumentOrSelection()
	{
		if (!HasFileOpen() || !_currentTabKey.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !CanEditCurrentDocument(showToast: true))
		{
			return false;
		}
		string text = (_codeEdit.HasSelection() ? _codeEdit.GetSelectedText() : _codeEdit.Text);
		string text2;
		try
		{
			text2 = XWCSharpIdeService.FormatCSharp(text);
		}
		catch
		{
			return false;
		}
		if (string.Equals(text, text2, StringComparison.Ordinal))
		{
			return true;
		}
		_codeEdit.BeginComplexOperation();
		try
		{
			if (!_codeEdit.HasSelection())
			{
				_codeEdit.SelectAll();
			}
			_codeEdit.InsertAtCursor(text2);
		}
		finally
		{
			_codeEdit.EndComplexOperation();
		}
		XWEditorInterface.Instance?.ShowToast("已格式化；一次撤销可恢复");
		return true;
	}

	public bool ApplyQuickFixAtCaret()
	{
		XWCodeErrorChecker.ErrorData diagnostic = FindDiagnosticAtCaret();
		return ApplyQuickFix(diagnostic);
	}

	private void ShowQuickFixMenu()
	{
		if (CanEditCurrentDocument(showToast: true))
		{
			_pendingQuickFixDiagnostic = FindDiagnosticAtCaret();
			if (XWCSharpIdeService.TryBuildQuickFix(_codeEdit.Text, _pendingQuickFixDiagnostic, out var title) == null)
			{
				XWEditorInterface.Instance?.ShowToast("当前诊断没有安全的自动修复");
				return;
			}
			_quickFixPopup.Clear();
			_quickFixPopup.AddItem("⚡ " + title, 0, Key.None);
			Vector2 vector = _quickFixButton.GetScreenPosition() + new Vector2(0f, _quickFixButton.Size.Y);
			_quickFixPopup.Position = (Vector2I)vector;
			_quickFixPopup.Popup();
		}
	}

	private void ApplyPendingQuickFix()
	{
		ApplyQuickFix(_pendingQuickFixDiagnostic);
	}

	private bool ApplyQuickFix(XWCodeErrorChecker.ErrorData diagnostic)
	{
		if (!CanEditCurrentDocument(showToast: true))
		{
			return false;
		}
		string title;
		string text = XWCSharpIdeService.TryBuildQuickFix(_codeEdit.Text, diagnostic, out title);
		if (text == null)
		{
			return false;
		}
		_codeEdit.BeginComplexOperation();
		try
		{
			_codeEdit.SelectAll();
			_codeEdit.InsertAtCursor(text);
		}
		finally
		{
			_codeEdit.EndComplexOperation();
		}
		XWEditorInterface.Instance?.ShowToast("已应用：" + title + "；一次撤销可恢复");
		return true;
	}

	private XWCodeErrorChecker.ErrorData FindDiagnosticAtCaret()
	{
		List<XWCodeErrorChecker.ErrorData> allErrors = _codeEdit.GetAllErrors();
		int line = _codeEdit.GetCaretLine();
		return allErrors.FirstOrDefault((XWCodeErrorChecker.ErrorData item) => item.Line == line) ?? allErrors.FirstOrDefault();
	}

	public IReadOnlyList<XWScriptDebugSession.Breakpoint> GetProjectBreakpoints()
	{
		SaveCurrentTabState();
		string activeModProjectRoot = GetActiveModProjectRoot();
		List<XWScriptDebugSession.Breakpoint> list = new List<XWScriptDebugSession.Breakpoint>();
		foreach (ScriptTabInfo value in _openTabs.Values)
		{
			if (IsPathInsideRoot(value.FilePath, activeModProjectRoot))
			{
				int[] array = value.Breakpoints ?? Array.Empty<int>();
				foreach (int line in array)
				{
					list.Add(new XWScriptDebugSession.Breakpoint
					{
						FilePath = value.FilePath,
						Line = line
					});
				}
			}
		}
		return list;
	}

	public async Task<XWScriptDebugSession.StartResult> StartDebugSessionAsync()
	{
		string activeModProjectRoot = GetActiveModProjectRoot();
		if (string.IsNullOrWhiteSpace(activeModProjectRoot))
		{
			LastDebugStartResult = new XWScriptDebugSession.StartResult
			{
				Message = "请先打开一个 Mod 工程。"
			};
			XWEditorInterface.Instance?.ShowToast(LastDebugStartResult.Message, 1);
			return LastDebugStartResult;
		}
		if (SaveAllTabs() < 0)
		{
			DebugSaveGateCount++;
			LastDebugStartResult = new XWScriptDebugSession.StartResult
			{
				Message = "调试未启动：请先处理 C# 外部修改冲突或保存失败。"
			};
			_debugStatusLabel.Text = "● 保存冲突";
			XWEditorInterface.Instance?.ShowToast(LastDebugStartResult.Message, 1);
			return LastDebugStartResult;
		}
		_debugStartButton.Disabled = true;
		_debugStatusLabel.Text = "◌ 正在后台编译…";
		if (_debugSession == null)
		{
			LastDebugStartResult = new XWScriptDebugSession.StartResult
			{
				Message = "脚本调试宿主不可用。"
			};
			UpdateDebugControls();
			XWEditorInterface.Instance?.ShowToast(LastDebugStartResult.Message, 2);
			return LastDebugStartResult;
		}
		DebugStartInvocationCount++;
		CancellationToken cancellationToken = _buildLifetimeCts?.Token ?? CancellationToken.None;
		if (!UnloadCallbackPreview(reportFailure: true))
		{
			LastDebugStartResult = new XWScriptDebugSession.StartResult
			{
				Message = "调试未启动：状态机动作仍被预览或运行时使用。"
			};
			UpdateDebugControls();
			return LastDebugStartResult;
		}
		LastDebugStartResult = await _debugSession.StartAsync(activeModProjectRoot, GetProjectBreakpoints(), cancellationToken);
		if (!GodotObject.IsInstanceValid(this))
		{
			return LastDebugStartResult;
		}
		if (LastDebugStartResult.CompileResult != null && !LastDebugStartResult.CompileResult.Success)
		{
			ApplyCompileResult(LastDebugStartResult.CompileResult);
		}
		if (LastDebugStartResult.Success)
		{
			SetDebugWorkbenchVisible(visible: true);
		}
		UpdateDebugControls();
		XWEditorInterface.Instance?.ShowToast(LastDebugStartResult.Message, (!LastDebugStartResult.Success) ? 2 : 0);
		return LastDebugStartResult;
	}

	public async Task<bool> StopDebugSessionAsync(int timeoutMilliseconds = 2500)
	{
		if (_debugSession == null)
		{
			return true;
		}
		_debugStopButton.Disabled = true;
		_debugStatusLabel.Text = "◌ 正在取消入口并卸载…";
		bool stopped = await _debugSession.StopAsync(timeoutMilliseconds);
		if (!GodotObject.IsInstanceValid(this))
		{
			return stopped;
		}
		if (stopped)
		{
			XWScriptDebugSession.StartResult lastDebugStartResult = LastDebugStartResult;
			if (lastDebugStartResult != null && lastDebugStartResult.CompileResult?.Success == true)
			{
				await RefreshCallbackPreviewAsync(GetActiveModProjectRoot(), LastDebugStartResult.CompileResult.OutputAssemblyPath, _buildLifetimeCts?.Token ?? CancellationToken.None);
			}
		}
		UpdateDebugControls();
		if (stopped)
		{
			XWEditorInterface.Instance?.ShowToast("调试程序集已卸载");
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast(_debugSession.StatusText, 2);
		}
		return stopped;
	}

	public override void _ExitTree()
	{
		CancellationTokenSource buildLifetimeCts = _buildLifetimeCts;
		_buildLifetimeCts = null;
		if (buildLifetimeCts != null)
		{
			try
			{
				buildLifetimeCts.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			buildLifetimeCts.Dispose();
		}
		XWModBuildSingleFlight.FlightStateChanged -= OnModBuildFlightStateChanged;
		_scriptEditorSynchronizationContext = null;
		UnloadCallbackPreview(reportFailure: false);
		base.VisibilityChanged -= OnEditorVisibilityChanged;
		SetExternalConflictWatcherActive(active: false);
		SuspendIdeBackgroundWork();
		ShutdownDebugWorkbench();
		if (GodotObject.IsInstanceValid(_debugHost))
		{
			_debugHost.OwnSessionShutdown(_debugSession, _debugPreviewRoot);
		}
		_debugSession = null;
		_debugPreviewRoot = null;
	}

	private void UpdateDebugControls()
	{
		if (GodotObject.IsInstanceValid(_debugStartButton))
		{
			string activeModProjectRoot = GetActiveModProjectRoot();
			bool flag = !string.IsNullOrWhiteSpace(activeModProjectRoot);
			bool flag2 = flag && XWScriptCompiler.IsModBuildBusy(activeModProjectRoot);
			bool flag3 = _debugSession?.HasLiveSession ?? false;
			_debugStartButton.Disabled = !flag || flag3 || flag2;
			_debugStopButton.Disabled = !flag3;
			Label debugStatusLabel = _debugStatusLabel;
			object text;
			if (!flag2 || flag3)
			{
				XWScriptDebugSession debugSession = _debugSession;
				text = ((debugSession != null && debugSession.IsRunning) ? (_debugSession.EntryPointInvoked ? "● 入口运行中" : "● 程序集已加载") : ("● " + (_debugSession?.StatusText ?? "未启动")));
			}
			else
			{
				text = "◌ Mod 编译中";
			}
			debugStatusLabel.Text = (string)text;
			Label debugStatusLabel2 = _debugStatusLabel;
			XWScriptDebugSession debugSession2 = _debugSession;
			debugStatusLabel2.Modulate = ((debugSession2 != null && debugSession2.IsRunning) ? new Color(0.35f, 1f, 0.55f) : Colors.White);
			UpdateCompileControls();
			if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
			{
				modEditorPanel.GetModToolsPanel()?.UpdateScriptBuildActionState();
			}
		}
	}

	public void RefreshBuildActionState()
	{
		UpdateDebugControls();
	}

	private int BeginRenamePreviewRequest()
	{
		_renameGeneration++;
		_pendingRename = null;
		_pendingRenameStamp = null;
		_pendingRenameGeneration = 0;
		if (GodotObject.IsInstanceValid(_renameDialog))
		{
			_renameDialog.GetOkButton().Disabled = true;
		}
		return _renameGeneration;
	}

	private void InvalidateIdeRequests()
	{
		Interlocked.Increment(ref _ideContextRevision);
		LastSymbolInfo = null;
		_hoverGeneration++;
		_hoverTimer?.Stop();
		if (GodotObject.IsInstanceValid(_symbolInfoPopup))
		{
			_symbolInfoPopup.Hide();
		}
		BeginRenamePreviewRequest();
		_renamePreviewTimer?.Stop();
		if (GodotObject.IsInstanceValid(_referencesWindow))
		{
			_referencesWindow.Hide();
		}
		_referenceLocations.Clear();
		_referencesList?.Clear();
	}

	private bool TryCaptureIdeRequest(out IdeRequestStamp stamp, out IReadOnlyDictionary<string, string> documents, int? offsetOverride = null)
	{
		stamp = default(IdeRequestStamp);
		documents = null;
		if (!TryGetIdeContext(out var root, out var path, out var source, out var offset))
		{
			return false;
		}
		documents = CaptureOpenDocuments();
		stamp = new IdeRequestStamp(Volatile.Read(in _ideContextRevision), NormalizeIdePath(root), NormalizeIdePath(path), source, offsetOverride ?? offset);
		return true;
	}

	private bool IsIdeRequestCurrent(IdeRequestStamp stamp, bool requireCaretOffset)
	{
		if (!GodotObject.IsInstanceValid(this) || !_workspaceActive || stamp.ContextRevision != Volatile.Read(in _ideContextRevision) || !string.Equals(stamp.ProjectRoot, NormalizeIdePath(GetActiveModProjectRoot()), StringComparison.OrdinalIgnoreCase) || !string.Equals(stamp.FilePath, NormalizeIdePath(_currentTabKey), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text = _codeEdit?.Text ?? "";
		if (!string.Equals(stamp.Source, text, StringComparison.Ordinal))
		{
			return false;
		}
		if (!requireCaretOffset || _codeEdit == null)
		{
			return true;
		}
		return XWCSharpIdeService.GetTextOffset(text, _codeEdit.GetCaretLine(), _codeEdit.GetCaretColumn()) == stamp.Offset;
	}

	private async Task WaitForIdeResultGateForProbeAsync(IdeRequestKind requestKind, string discriminator)
	{
		Func<IdeRequestKind, string, Task> ideResultGateForProbeAsync = IdeResultGateForProbeAsync;
		if (ideResultGateForProbeAsync != null)
		{
			await ideResultGateForProbeAsync(requestKind, discriminator ?? "");
		}
	}

	private static string NormalizeIdePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		try
		{
			return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return path.TrimEnd('/', '\\');
		}
	}

	private string GetActiveModProjectRoot()
	{
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			string text = modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return _activeProjectRoot;
	}

	private bool TryGetIdeContext(out string root, out string path, out string source, out int offset)
	{
		root = GetActiveModProjectRoot();
		path = _currentTabKey;
		source = _codeEdit?.Text ?? "";
		offset = ((_codeEdit != null) ? XWCSharpIdeService.GetTextOffset(source, _codeEdit.GetCaretLine(), _codeEdit.GetCaretColumn()) : 0);
		if (!string.IsNullOrWhiteSpace(root) && !string.IsNullOrWhiteSpace(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			return IsPathInsideRoot(path, root);
		}
		return false;
	}

	private IReadOnlyDictionary<string, string> CaptureOpenDocuments()
	{
		SaveCurrentTabState();
		string activeModProjectRoot = GetActiveModProjectRoot();
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (ScriptTabInfo value in _openTabs.Values)
		{
			if (value.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && IsPathInsideRoot(value.FilePath, activeModProjectRoot))
			{
				dictionary[value.FilePath] = value.Text ?? "";
			}
		}
		return dictionary;
	}

	private List<string> GetDirtyAffectedDocuments(XWCSharpIdeService.RenamePreview preview)
	{
		List<string> list = new List<string>();
		if (preview == null)
		{
			return list;
		}
		SaveCurrentTabState();
		foreach (string key in preview.UpdatedDocuments.Keys)
		{
			if (_openTabs.TryGetValue(key, out var value) && value.HasUnsavedChanges)
			{
				list.Add(key);
			}
		}
		return list;
	}

	private void ReloadOpenDocumentsFromDisk()
	{
		string currentTabKey = _currentTabKey;
		foreach (ScriptTabInfo value in _openTabs.Values)
		{
			ReloadTabFromDisk(value);
		}
		if (!string.IsNullOrWhiteSpace(currentTabKey) && _openTabs.ContainsKey(currentTabKey))
		{
			LoadTabData(currentTabKey);
		}
		UpdateScriptList();
	}

	private static bool IsPathInsideRoot(string path, string root)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
		{
			return false;
		}
		try
		{
			string value = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return Path.GetFullPath(path).StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static string SafeRelativePath(string root, string path)
	{
		try
		{
			return Path.GetRelativePath(root, path).Replace('\\', '/');
		}
		catch
		{
			return Path.GetFileName(path);
		}
	}

	private static string BuildSymbolInfoText(XWCSharpIdeService.SymbolInfoResult info)
	{
		string text = (string.IsNullOrWhiteSpace(info.Signature) ? info.Display : info.Signature);
		string text2 = (string.IsNullOrWhiteSpace(info.Documentation) ? "没有 XML 文档说明" : info.Documentation);
		return $"[color=#e9c46a][b]{EscapeBbcode(text)}[/b][/color]\n{EscapeBbcode(info.Kind)} · F12 定义 · Shift+F12 引用\n\n{EscapeBbcode(text2)}";
	}

	private static string EscapeBbcode(string text)
	{
		return (text ?? "").Replace("[", "［").Replace("]", "］");
	}
}
