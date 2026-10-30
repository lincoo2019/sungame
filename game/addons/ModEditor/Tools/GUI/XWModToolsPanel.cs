using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Issues.GUI;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.Overrides;
using PVZHE.ModEditor.ModSystem.References;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Tools.GUI;

public partial class XWModToolsPanel : PanelContainer
{
	private sealed class OverrideDiffRow
	{
		public XWOverrideDiff Diff;

		public XWOverrideDiff.Entry Entry;

		public string TargetPath = "";
	}

	private sealed class ReferenceGraphRow
	{
		public string SourcePath = "";

		public string TargetPath = "";

		public bool Missing;
	}

	private readonly XWTemplateLibrary _templateLibrary = new XWTemplateLibrary();

	private readonly Dictionary<int, string> _templateIds = new Dictionary<int, string>();

	private readonly Dictionary<int, string> _importCategories = new Dictionary<int, string>();

	private readonly Dictionary<int, Button> _templateCards = new Dictionary<int, Button>();

	private readonly Dictionary<int, Button> _importCategoryCards = new Dictionary<int, Button>();

	private readonly List<OverrideDiffRow> _overrideDiffRows = new List<OverrideDiffRow>();

	private readonly List<ReferenceGraphRow> _referenceGraphRows = new List<ReferenceGraphRow>();

	private readonly List<XWImportWizard.ImportResult> _importResults = new List<XWImportWizard.ImportResult>();

	private readonly List<string> _localizationKeys = new List<string>();

	private XWLocalizationTable _localizationTable = new XWLocalizationTable();

	private TabContainer _toolTabs;

	private RichTextLabel _result;

	private Tree _modTree;

	private Tree _referenceGraphTree;

	private Tree _overrideDiffTree;

	private Label _modSummary;

	private Label _referenceGraphSummary;

	private Label _overrideDiffSummary;

	private OptionButton _templateOption;

	private HFlowContainer _templateCardGrid;

	private LineEdit _templateName;

	private TextEdit _templatePreview;

	private Label _templateStatus;

	private OptionButton _importCategoryOption;

	private HFlowContainer _importCategoryGrid;

	private LineEdit _importSourcePath;

	private Tree _importResultTree;

	private Label _importSummary;

	private Tree _localizationTree;

	private Label _localizationSummary;

	private Button _runScriptBuildButton;

	private Button _importModPackageButton;

	private Button _deleteModPackageButton;

	private XWModPackageInstaller.PreparedImport _pendingModImport;

	private Guid _pendingModImportToken;

	private SynchronizationContext _toolsSynchronizationContext;

	private CancellationTokenSource _buildLifetimeCts = new CancellationTokenSource();

	private bool _visibilitySignalBound;

	private bool _managementClosed;

	private Guid _filePickerToken;

	private Node _modConfirmation;

	private Action _cancelModConfirmation;

	private Button _playerPlay;

	private Button _playerCreate;

	private static readonly Color PlayerInk = new Color("513015");

	private static readonly Color PlayerMuted = new Color("80603b");

	private static readonly Color PlayerGreen = new Color("3f641e");

	private static readonly Color PlayerGold = new Color("89500e");

	private static readonly Color PlayerError = new Color("a23620");

	private readonly Dictionary<string, Button> _playerRows = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

	private List<XWModManager.ModEntry> _playerEntries = new List<XWModManager.ModEntry>();

	private XWModManager.ModEntry _playerSelected;

	private HBoxContainer _playerColumns;

	private PanelContainer _playerDirectory;

	private CenterContainer _playerEmpty;

	private VBoxContainer _playerList;

	private ScrollContainer _playerListScroll;

	private ScrollContainer _playerDetailScroll;

	private Label _playerName;

	private Label _playerAuthor;

	private Label _playerDescription;

	private Label _playerEnabled;

	private Label _playerRunning;

	private Label _playerMode;

	private Label _playerVersions;

	private Label _playerFailure;

	private Label _playerNotice;

	private Label _playerCounts;

	private Button _playerToggle;

	private Button _playerRefresh;

	private Button _playerEmptyImport;

	private Button _playerReturn;

	private Button _playerMore;

	private Control _playerUtilityOverlay;

	private RichTextLabel _playerTechnical;

	private string _playerSnapshotDetails = "";

	private string _playerOperationDetails = "";

	private string _playerSnapshotNotice = "";

	private Color _playerSnapshotNoticeColor;

	[Export(PropertyHint.None, "")]
	public bool ManagementOnly { get; set; }

	private bool CanUseManagement
	{
		get
		{
			if (!_managementClosed && GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion())
			{
				return IsInsideTree();
			}
			return false;
		}
	}

	public XWScriptCompiler.CompileResult LastScriptBuildResult { get; private set; }

	public bool IsScriptBuildRunning { get; private set; }

	internal Control ManagementConfirmationHost { get; set; }

	internal Action ManagementReturnRequested { get; set; }

	internal Action ManagementPlayRequested { get; set; }

	internal Action ManagementCreateRequested { get; set; }

	public override void _Ready()
	{
		_toolsSynchronizationContext = SynchronizationContext.Current;
		if (ManagementOnly)
		{
			ConfigureManagementOnly();
			RefreshModList();
			return;
		}
		BindScene();
		ApplyScrollSafeLayout();
		XWModBuildSingleFlight.FlightStateChanged += OnModBuildFlightStateChanged;
		base.VisibilityChanged += UpdateScriptBuildActionState;
		_visibilitySignalBound = true;
		SetupTemplateLibrary();
		SetupImportWizard();
		RefreshModList();
		SetResult("选择一个 Mod 工具开始。");
		UpdateScriptBuildActionState();
	}

	public override void _ExitTree()
	{
		CloseManagementSession();
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
		if (_visibilitySignalBound && IsConnected(CanvasItem.SignalName.VisibilityChanged, Callable.From(UpdateScriptBuildActionState)))
		{
			base.VisibilityChanged -= UpdateScriptBuildActionState;
		}
		_visibilitySignalBound = false;
		if (GodotObject.IsInstanceValid(_deleteModPackageButton) && _deleteModPackageButton.IsConnected(BaseButton.SignalName.Pressed, Callable.From(ConfirmDeleteSelectedMod)))
		{
			_deleteModPackageButton.Pressed -= ConfirmDeleteSelectedMod;
		}
		_toolsSynchronizationContext = null;
	}

	public void CloseManagementSession()
	{
		_managementClosed = true;
		_filePickerToken = Guid.Empty;
		_cancelModConfirmation = null;
		if (GodotObject.IsInstanceValid(_modConfirmation))
		{
			_modConfirmation.QueueFree();
		}
		_modConfirmation = null;
		_pendingModImport?.Dispose();
		_pendingModImport = null;
		_pendingModImportToken = Guid.Empty;
		if (ManagementOnly)
		{
			ClosePlayerUtility(restoreFocus: false);
		}
	}

	public bool TryCancelManagementConfirmation()
	{
		if (_cancelModConfirmation == null)
		{
			return false;
		}
		_cancelModConfirmation();
		return true;
	}

	private void BindScene()
	{
		_toolTabs = GetNodeOrNull<TabContainer>("%ToolTabs");
		_result = GetNodeOrNull<RichTextLabel>("%Result");
		_modTree = GetNodeOrNull<Tree>("%ModTree");
		_referenceGraphTree = GetNodeOrNull<Tree>("%ReferenceGraphTree");
		_overrideDiffTree = GetNodeOrNull<Tree>("%OverrideDiffTree");
		_modSummary = GetNodeOrNull<Label>("%ModSummary");
		_referenceGraphSummary = GetNodeOrNull<Label>("%ReferenceGraphSummary");
		_overrideDiffSummary = GetNodeOrNull<Label>("%OverrideDiffSummary");
		_templateOption = GetNodeOrNull<OptionButton>("%TemplateOption");
		_templateCardGrid = GetNodeOrNull<HFlowContainer>("%TemplateCardGrid");
		_templateName = GetNodeOrNull<LineEdit>("%TemplateName");
		_templatePreview = GetNodeOrNull<TextEdit>("%TemplatePreview");
		_templateStatus = GetNodeOrNull<Label>("%TemplateStatus");
		_importCategoryOption = GetNodeOrNull<OptionButton>("%ImportCategoryOption");
		_importCategoryGrid = GetNodeOrNull<HFlowContainer>("%ImportCategoryGrid");
		_importSourcePath = GetNodeOrNull<LineEdit>("%ImportSourcePath");
		_importResultTree = GetNodeOrNull<Tree>("%ImportResultTree");
		_importSummary = GetNodeOrNull<Label>("%ImportSummary");
		_localizationTree = GetNodeOrNull<Tree>("%LocalizationTree");
		_localizationSummary = GetNodeOrNull<Label>("%LocalizationSummary");
		ConnectButton("%RefreshMods", RefreshModList);
		_importModPackageButton = GetNodeOrNull<Button>("%ImportModPackage");
		if (_importModPackageButton != null)
		{
			_importModPackageButton.Visible = true;
			_importModPackageButton.Pressed += ChooseModPackage;
		}
		ConnectButton("%EnableMod", delegate
		{
			SetSelectedModEnabled(enabled: true);
		});
		ConnectButton("%DisableMod", delegate
		{
			SetSelectedModEnabled(enabled: false);
		});
		_deleteModPackageButton = GetNodeOrNull<Button>("%DeleteMod");
		if (_deleteModPackageButton != null)
		{
			_deleteModPackageButton.Pressed += ConfirmDeleteSelectedMod;
		}
		if (_modTree != null)
		{
			_modTree.Columns = 4;
			_modTree.ColumnTitlesVisible = true;
			_modTree.HideRoot = true;
			_modTree.SetColumnTitle(0, "启用");
			_modTree.SetColumnTitle(1, "Mod");
			_modTree.SetColumnTitle(2, "版本");
			_modTree.SetColumnTitle(3, "状态");
			_modTree.ItemSelected += OnModSelected;
			_modTree.ItemActivated += delegate
			{
				SetSelectedModEnabled(!IsSelectedModEnabled());
			};
		}
		if (ManagementOnly)
		{
			return;
		}
		ConnectButton("%RunReleaseCheck", RunReleaseCheck);
		_runScriptBuildButton = GetNodeOrNull<Button>("%RunScriptBuild");
		if (_runScriptBuildButton != null)
		{
			_runScriptBuildButton.Pressed += RunScriptBuild;
		}
		ConnectButton("%RunReferenceGraph", RunReferenceGraph);
		ConnectButton("%ShowOverrideDiff", ShowOverrideDiff);
		ConnectButton("%StartSandbox", StartSandbox);
		ConnectButton("%ShowImportHint", ShowImportHint);
		ConnectButton("%ShowLocalizationHint", ShowLocalizationHint);
		ConnectButton("%RunImportWizard", RunImportWizard);
		ConnectButton("%RefreshLocalizationTable", RefreshLocalizationTable);
		ConnectButton("%SaveLocalizationTable", SaveLocalizationTable);
		ConnectButton("%CreateTemplate", CreateTemplateFromSelection);
		if (_referenceGraphTree != null)
		{
			_referenceGraphTree.Columns = 4;
			_referenceGraphTree.ColumnTitlesVisible = true;
			_referenceGraphTree.HideRoot = true;
			_referenceGraphTree.SetColumnTitle(0, "状态");
			_referenceGraphTree.SetColumnTitle(1, "源资源");
			_referenceGraphTree.SetColumnTitle(2, "目标资源");
			_referenceGraphTree.SetColumnTitle(3, "反向引用");
			_referenceGraphTree.ItemActivated += OpenReferenceGraphTarget;
		}
		if (_overrideDiffTree != null)
		{
			_overrideDiffTree.Columns = 6;
			_overrideDiffTree.ColumnTitlesVisible = true;
			_overrideDiffTree.HideRoot = true;
			_overrideDiffTree.SetColumnTitle(0, "覆盖");
			_overrideDiffTree.SetColumnTitle(1, "分类");
			_overrideDiffTree.SetColumnTitle(2, "Key");
			_overrideDiffTree.SetColumnTitle(3, "属性");
			_overrideDiffTree.SetColumnTitle(4, "原版值");
			_overrideDiffTree.SetColumnTitle(5, "Mod 值");
			_overrideDiffTree.ItemEdited += ToggleOverrideDiffEntry;
			_overrideDiffTree.ItemActivated += OpenOverrideDiffTarget;
		}
		if (_importResultTree != null)
		{
			_importResultTree.Columns = 4;
			_importResultTree.ColumnTitlesVisible = true;
			_importResultTree.HideRoot = true;
			_importResultTree.SetColumnTitle(0, "状态");
			_importResultTree.SetColumnTitle(1, "Key");
			_importResultTree.SetColumnTitle(2, "源文件");
			_importResultTree.SetColumnTitle(3, "目标文件");
			_importResultTree.ItemActivated += OpenImportedResource;
		}
		if (_localizationTree != null)
		{
			_localizationTree.Columns = 4;
			_localizationTree.ColumnTitlesVisible = true;
			_localizationTree.HideRoot = true;
			_localizationTree.SetColumnTitle(0, "Key");
			_localizationTree.SetColumnTitle(1, "简体中文");
			_localizationTree.SetColumnTitle(2, "English");
			_localizationTree.SetColumnTitle(3, "状态");
			_localizationTree.ItemEdited += EditLocalizationEntry;
		}
		if (_templateOption != null)
		{
			_templateOption.ItemSelected += SelectTemplate;
		}
		if (_importCategoryOption != null)
		{
			_importCategoryOption.ItemSelected += delegate(long index)
			{
				SelectImportCategory((int)index);
			};
		}
	}

	private void ApplyScrollSafeLayout()
	{
		base.CustomMinimumSize = Vector2.Zero;
		base.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		base.SizeFlagsVertical = SizeFlags.ExpandFill;
		ConfigureBoundedControl(GetNodeOrNull<Control>("Root"), 0f);
		ConfigureScrollContainer(GetNodeOrNull<ScrollContainer>("Root/ToolTabs/检查"));
		ConfigureScrollContainer(GetNodeOrNull<ScrollContainer>("Root/ToolTabs/模板库"));
		ConfigureScrollContainer(GetNodeOrNull<ScrollContainer>("Root/ToolTabs/Mod 管理"));
		ConfigureScrollContainer(GetNodeOrNull<ScrollContainer>("Root/ToolTabs/其它"));
		ConfigureScrollContainer(GetNodeOrNull<ScrollContainer>("%ResultScroll"));
		ConfigureBoundedControl(_referenceGraphTree, 120f);
		ConfigureBoundedControl(_modTree, 120f);
		ConfigureBoundedControl(_templatePreview, 110f);
		ConfigureBoundedControl(_importResultTree, 100f);
		ConfigureBoundedControl(_localizationTree, 100f);
		ConfigureBoundedControl(_overrideDiffTree, 110f);
		ConfigureBoundedLogControl(_result, 96f);
	}

	private static void ConfigureScrollContainer(ScrollContainer scroll)
	{
		if (scroll != null)
		{
			scroll.CustomMinimumSize = Vector2.Zero;
			scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		}
	}

	private static void ConfigureBoundedControl(Control control, float minimumHeight)
	{
		if (control != null)
		{
			control.CustomMinimumSize = new Vector2(0f, minimumHeight);
			control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			control.SizeFlagsVertical = SizeFlags.ExpandFill;
		}
	}

	private static void ConfigureBoundedLogControl(RichTextLabel label, float minimumHeight)
	{
		if (label != null)
		{
			ConfigureBoundedControl(label, minimumHeight);
			label.FitContent = false;
			label.ScrollActive = true;
		}
	}

	private void ConnectButton(string uniquePath, Action action)
	{
		Button nodeOrNull = GetNodeOrNull<Button>(uniquePath);
		if (nodeOrNull != null)
		{
			nodeOrNull.Pressed += action;
		}
	}

	private async void RunReleaseCheck()
	{
		SelectTab(0);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		List<XWValidationIssue> list = await new XWModValidationService().ValidateProjectAsync(currentProjectPath);
		if (XWEditorInterface.Instance?.GetIssuePanel() is XWIssuePanel xWIssuePanel)
		{
			xWIssuePanel.LoadIssues(list);
			XWEditorInterface.Instance.FocusPanel("issues");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[b]发布检查[/b]");
		if (list.Count == 0)
		{
			stringBuilder.AppendLine("未发现问题。");
		}
		else
		{
			foreach (XWValidationIssue item in list)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
				handler.AppendLiteral("- ");
				handler.AppendFormatted(item.Code);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.Message);
				stringBuilder2.AppendLine(ref handler);
			}
		}
		SetResult(stringBuilder.ToString());
	}

	private void RunReferenceGraph()
	{
		SelectTab(0);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		XWReferenceGraphService.ReferenceGraph referenceGraph = new XWReferenceGraphService().BuildForProject(currentProjectPath);
		PopulateReferenceGraphTree(referenceGraph, currentProjectPath);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[b]引用图[/b]");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2);
		handler.AppendLiteral("资源: ");
		handler.AppendFormatted(referenceGraph.References.Count);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("反向引用: ");
		handler.AppendFormatted(referenceGraph.ReverseReferences.Count);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2);
		handler.AppendLiteral("断链: ");
		handler.AppendFormatted(referenceGraph.MissingReferences.Count);
		stringBuilder5.AppendLine(ref handler);
		foreach (KeyValuePair<string, List<string>> reference in referenceGraph.References)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder2);
			handler.AppendLiteral("- ");
			handler.AppendFormatted(Path.GetFileName(reference.Key));
			handler.AppendLiteral(" -> ");
			handler.AppendFormatted(reference.Value.Count);
			stringBuilder6.AppendLine(ref handler);
		}
		foreach (KeyValuePair<string, List<string>> missingReference in referenceGraph.MissingReferences)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder2);
			handler.AppendLiteral("! ");
			handler.AppendFormatted(Path.GetFileName(missingReference.Key));
			handler.AppendLiteral(" -> ");
			handler.AppendFormatted(missingReference.Value.Count);
			stringBuilder7.AppendLine(ref handler);
		}
		SetResult(stringBuilder.ToString());
	}

	private void PopulateReferenceGraphTree(XWReferenceGraphService.ReferenceGraph graph, string projectPath)
	{
		_referenceGraphRows.Clear();
		if (_referenceGraphTree == null)
		{
			return;
		}
		_referenceGraphTree.Clear();
		TreeItem root = _referenceGraphTree.CreateItem();
		if (graph == null)
		{
			UpdateReferenceGraphSummary(0, 0, 0);
			return;
		}
		foreach (KeyValuePair<string, List<string>> reference in graph.References)
		{
			string key = reference.Key;
			foreach (string item in reference.Value)
			{
				List<string> value;
				bool missing = graph.MissingReferences.TryGetValue(key, out value) && value.Contains(item);
				AddReferenceGraphRow(root, key, item, missing, graph);
			}
		}
		foreach (KeyValuePair<string, List<string>> missingReference in graph.MissingReferences)
		{
			foreach (string item2 in missingReference.Value)
			{
				if (!graph.References.TryGetValue(missingReference.Key, out var value2) || !value2.Contains(item2))
				{
					AddReferenceGraphRow(root, missingReference.Key, item2, missing: true, graph);
				}
			}
		}
		UpdateReferenceGraphSummary(graph.References.Count, graph.ReverseReferences.Count, graph.MissingReferences.Count);
	}

	private void AddReferenceGraphRow(TreeItem root, string source, string target, bool missing, XWReferenceGraphService.ReferenceGraph graph)
	{
		int count = _referenceGraphRows.Count;
		_referenceGraphRows.Add(new ReferenceGraphRow
		{
			SourcePath = source,
			TargetPath = target,
			Missing = missing
		});
		TreeItem treeItem = _referenceGraphTree.CreateItem(root);
		treeItem.SetText(0, missing ? "断链" : "正常");
		treeItem.SetText(1, FormatGraphPath(source));
		treeItem.SetText(2, FormatGraphPath(target));
		treeItem.SetText(3, graph.ReverseReferences.TryGetValue(target, out var value) ? value.Count.ToString() : "0");
		treeItem.SetMetadata(0, count);
		if (missing)
		{
			treeItem.SetCustomColor(0, new Color(1f, 0.35f, 0.25f));
		}
	}

	private void UpdateReferenceGraphSummary(int references, int reverseReferences, int missingReferences)
	{
		if (_referenceGraphSummary != null)
		{
			_referenceGraphSummary.Text = $"正向资源: {references}    反向资源: {reverseReferences}    断链来源: {missingReferences}";
		}
	}

	private void OpenReferenceGraphTarget()
	{
		TreeItem treeItem = _referenceGraphTree?.GetSelected();
		if (treeItem == null)
		{
			return;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Int)
		{
			return;
		}
		int num = metadata.AsInt32();
		if (num >= 0 && num < _referenceGraphRows.Count)
		{
			ReferenceGraphRow referenceGraphRow = _referenceGraphRows[num];
			string text = (string.IsNullOrWhiteSpace(referenceGraphRow.TargetPath) ? referenceGraphRow.SourcePath : referenceGraphRow.TargetPath);
			if (!OpenToolTarget(text))
			{
				SetResult("[b]引用图[/b]\n无法打开: " + text);
			}
		}
	}

	private async void RunScriptBuild()
	{
		if (IsScriptBuildRunning)
		{
			return;
		}
		SelectTab(0);
		string projectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(projectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		IsScriptBuildRunning = true;
		LastScriptBuildResult = null;
		UpdateScriptBuildActionState();
		SetResult("[b]脚本编译[/b]\n正在编译...");
		try
		{
			XWScriptCompiler.CompileResult compileResult = (LastScriptBuildResult = await XWScriptCompiler.CompileModProjectAsync(projectPath, _buildLifetimeCts?.Token ?? CancellationToken.None));
			if (!GodotObject.IsInstanceValid(this))
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("[b]脚本编译[/b]");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2);
			handler.AppendLiteral("目标: ");
			handler.AppendFormatted(compileResult?.TargetName ?? Path.GetFileName(projectPath));
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2);
			handler.AppendLiteral("输出: ");
			handler.AppendFormatted(Path.Combine(compileResult?.BuildDirectory ?? "", "bin"));
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2);
			handler.AppendLiteral("结果: ");
			handler.AppendFormatted((compileResult != null && compileResult.Success) ? "成功" : "失败");
			handler.AppendLiteral(" (");
			handler.AppendFormatted(compileResult?.ElapsedSeconds ?? 0.0, "0.00");
			handler.AppendLiteral("s)");
			stringBuilder5.AppendLine(ref handler);
			if (compileResult != null && compileResult.Diagnostics?.Count > 0)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("[b]诊断[/b]");
				foreach (XWCodeErrorChecker.ErrorData diagnostic in compileResult.Diagnostics)
				{
					string value = ((diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Warning) ? "警告" : "错误");
					string value2 = (string.IsNullOrWhiteSpace(diagnostic.FilePath) ? "" : diagnostic.FilePath.Replace('\\', '/').GetFile());
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder6 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(6, 5, stringBuilder2);
					handler.AppendLiteral("- ");
					handler.AppendFormatted(value);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(value2);
					handler.AppendLiteral(":");
					handler.AppendFormatted(diagnostic.Line + 1);
					handler.AppendLiteral(":");
					handler.AppendFormatted(diagnostic.Column + 1);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(diagnostic.Message);
					stringBuilder6.AppendLine(ref handler);
				}
			}
			else
			{
				stringBuilder.AppendLine("未产生诊断。");
			}
			SetResult(stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			if (GodotObject.IsInstanceValid(this))
			{
				SetResult("[b]脚本编译[/b]\n启动失败: " + ex.GetBaseException().Message);
			}
		}
		finally
		{
			IsScriptBuildRunning = false;
			UpdateScriptBuildActionState();
		}
	}

	public void UpdateScriptBuildActionState()
	{
		if (GodotObject.IsInstanceValid(_runScriptBuildButton))
		{
			string currentProjectPath = GetCurrentProjectPath();
			bool flag = !string.IsNullOrWhiteSpace(currentProjectPath);
			bool flag2 = flag && XWScriptCompiler.IsModBuildBusy(currentProjectPath);
			XWEditorInterface instance = XWEditorInterface.Instance;
			bool flag3 = instance != null && instance.GetScriptEditor()?.HasLiveDebugSession == true;
			_runScriptBuildButton.Disabled = !flag || IsScriptBuildRunning || flag2 || flag3;
		}
	}

	private void OnModBuildFlightStateChanged(string projectRoot, bool _)
	{
		if (!string.Equals(XWModBuildSingleFlight.NormalizeProjectRoot(GetCurrentProjectPath()), XWModBuildSingleFlight.NormalizeProjectRoot(projectRoot), StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		_toolsSynchronizationContext?.Post(delegate
		{
			if (GodotObject.IsInstanceValid(this))
			{
				UpdateScriptBuildActionState();
			}
		}, null);
	}

	private void ShowOverrideDiff()
	{
		SelectTab(3);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			PopulateOverrideDiffTree(new List<XWOverrideDiff>(), currentProjectPath);
			SetResult("[b]差异覆盖[/b]\n请先打开 Mod 工程。");
			return;
		}
		XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(currentProjectPath, "mod.json"));
		if (xWModManifest == null)
		{
			PopulateOverrideDiffTree(new List<XWOverrideDiff>(), currentProjectPath);
			SetResult("[b]差异覆盖[/b]\n缺少 mod.json，无法读取 overrides。");
			return;
		}
		List<XWOverrideDiff> list = BuildOverrideDiffs(currentProjectPath, xWModManifest);
		PopulateOverrideDiffTree(list, currentProjectPath);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[b]差异覆盖[/b]");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("覆盖资源: ");
		handler.AppendFormatted(list.Count);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2);
		handler.AppendLiteral("覆盖项: ");
		handler.AppendFormatted(_overrideDiffRows.Count);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder.AppendLine("可在表格中逐项勾选是否启用覆盖，双击行可跳到 Mod 覆盖资源。");
		SetResult(stringBuilder.ToString());
	}

	private List<XWOverrideDiff> BuildOverrideDiffs(string projectPath, XWModManifest manifest)
	{
		List<XWOverrideDiff> list = new List<XWOverrideDiff>();
		if (manifest?.Overrides == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, List<string>> @override in manifest.Overrides)
		{
			string text = @override.Key ?? "";
			foreach (string item in @override.Value ?? new List<string>())
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					string text2 = FindOverrideResourcePath(projectPath, manifest, text, item);
					XWOverrideDiff xWOverrideDiff = new XWOverrideDiff
					{
						Category = text,
						BaseKey = item,
						TargetKey = item
					};
					xWOverrideDiff.Add("resource", Variant.From<string>("内置 " + text + "/" + item), Variant.From<string>(string.IsNullOrWhiteSpace(text2) ? "未找到 Mod 覆盖资源" : text2), !string.IsNullOrWhiteSpace(text2));
					list.Add(xWOverrideDiff);
				}
			}
		}
		return list;
	}

	private void PopulateOverrideDiffTree(IReadOnlyList<XWOverrideDiff> diffs, string projectPath)
	{
		_overrideDiffRows.Clear();
		if (_overrideDiffTree == null)
		{
			return;
		}
		_overrideDiffTree.Clear();
		TreeItem parent = _overrideDiffTree.CreateItem();
		foreach (XWOverrideDiff diff in diffs)
		{
			foreach (XWOverrideDiff.Entry entry in diff.Entries)
			{
				int count = _overrideDiffRows.Count;
				string text = entry.ModValue.AsString();
				if (text.StartsWith("未找到"))
				{
					text = "";
				}
				_overrideDiffRows.Add(new OverrideDiffRow
				{
					Diff = diff,
					Entry = entry,
					TargetPath = text
				});
				TreeItem treeItem = _overrideDiffTree.CreateItem(parent);
				treeItem.SetCellMode(0, TreeItem.TreeCellMode.Check);
				treeItem.SetChecked(0, entry.Enabled);
				treeItem.SetEditable(0, enabled: true);
				treeItem.SetText(1, diff.Category);
				treeItem.SetText(2, diff.TargetKey);
				treeItem.SetText(3, entry.PropertyPath);
				treeItem.SetText(4, FormatDiffValue(entry.OriginalValue));
				treeItem.SetText(5, FormatDiffValue(entry.ModValue));
				treeItem.SetMetadata(0, count);
			}
		}
		if (_overrideDiffSummary != null)
		{
			_overrideDiffSummary.Text = ((diffs.Count == 0) ? "当前 Mod 没有声明 overrides。" : $"覆盖资源: {diffs.Count}    覆盖项: {_overrideDiffRows.Count}");
		}
	}

	private void ToggleOverrideDiffEntry()
	{
		TreeItem treeItem = _overrideDiffTree?.GetSelected();
		if (treeItem == null)
		{
			return;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Int)
		{
			int num = metadata.AsInt32();
			if (num >= 0 && num < _overrideDiffRows.Count)
			{
				OverrideDiffRow overrideDiffRow = _overrideDiffRows[num];
				overrideDiffRow.Entry.Enabled = treeItem.IsChecked(0);
				overrideDiffRow.Diff.SetEnabled(overrideDiffRow.Entry.PropertyPath, overrideDiffRow.Entry.Enabled);
				SetResult($"[b]差异覆盖[/b]\n{overrideDiffRow.Diff.Category}/{overrideDiffRow.Diff.TargetKey} {overrideDiffRow.Entry.PropertyPath} 已{(overrideDiffRow.Entry.Enabled ? "启用" : "关闭")}覆盖。");
			}
		}
	}

	private void OpenOverrideDiffTarget()
	{
		TreeItem treeItem = _overrideDiffTree?.GetSelected();
		if (treeItem == null)
		{
			return;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Int)
		{
			return;
		}
		int num = metadata.AsInt32();
		if (num >= 0 && num < _overrideDiffRows.Count)
		{
			string targetPath = _overrideDiffRows[num].TargetPath;
			if (string.IsNullOrWhiteSpace(targetPath))
			{
				SetResult("[b]差异覆盖[/b]\n这个覆盖项还没有对应的 Mod 资源文件。");
			}
			else if (!OpenToolTarget(targetPath))
			{
				SetResult("[b]差异覆盖[/b]\n无法打开: " + targetPath);
			}
		}
	}

	private static bool OpenToolTarget(string targetPath)
	{
		if (string.IsNullOrWhiteSpace(targetPath))
		{
			return false;
		}
		string text = NormalizeProjectPath(targetPath);
		if (Path.GetExtension(text).ToLowerInvariant() == ".cs")
		{
			XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			if (xWScriptEditor == null)
			{
				return false;
			}
			if (!xWScriptEditor.TryOpenFileAt(text, 0, 0))
			{
				return false;
			}
			XWEditorInterface.Instance?.FocusPanel("script_editor");
			return true;
		}
		if (XWResourceEditorRegistry.TryOpenPath(text))
		{
			return true;
		}
		if (ResourceLoader.Exists(text))
		{
			Resource resource = ResourceLoader.Load<Resource>(text, null, ResourceLoader.CacheMode.Reuse);
			XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, text, "resource_editor"));
			return true;
		}
		return false;
	}

	private static string FormatGraphPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string text = NormalizeProjectPath(path).Replace('\\', '/');
		if (text.StartsWith("res://") || text.StartsWith("uid://"))
		{
			return text;
		}
		string fileName = Path.GetFileName(text);
		if (!string.IsNullOrWhiteSpace(fileName))
		{
			return fileName;
		}
		return text;
	}

	private async void StartSandbox()
	{
		SelectTab(3);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		string modId = Path.GetFileName(currentProjectPath);
		XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(currentProjectPath, "mod.json"));
		if (!string.IsNullOrWhiteSpace(xWModManifest?.Id))
		{
			modId = xWModManifest.Id;
		}
		await new XWModSandboxSession().StartAsync(modId);
		SetResult("[b]测试沙盒[/b]\n" + modId + " 已临时启用，不会写入正式启用列表。");
	}

	private void ShowImportHint()
	{
		SelectTab(3);
		SetResult("[b]导入向导[/b]\n输入一个或多个本地文件路径，导入后会按扩展名复制到 Assets/Images、Assets/Audio、Assets/Fonts 等目录，生成 key，并写入 mod.json。");
	}

	private void SetupImportWizard()
	{
		if (_importCategoryOption != null)
		{
			_importCategories.Clear();
			_importCategoryCards.Clear();
			_importCategoryOption.Clear();
			ClearVisualChoiceChildren(_importCategoryGrid);
			AddImportCategory(0, "图片", "Images");
			AddImportCategory(1, "音效", "Sfx");
			AddImportCategory(2, "BGM", "BGM");
			AddImportCategory(3, "字体", "Fonts");
			AddImportCategory(4, "场景", "Scenes");
			AddImportCategory(5, "UI", "UI");
			_importCategoryOption.Select(0);
			SelectImportCategory(0);
		}
	}

	private void AddImportCategory(int index, string label, string folder)
	{
		_importCategoryOption.AddItem(label + " / " + folder, index);
		_importCategories[index] = folder;
		if (_importCategoryGrid != null)
		{
			Button button = new Button
			{
				Name = "ImportCategoryCard_" + folder,
				Text = label,
				TooltipText = "导入到 Assets/" + folder,
				ToggleMode = true,
				CustomMinimumSize = new Vector2(108f, 42f),
				Icon = ResourceLoader.Load<Texture2D>(ResolveImportCategoryIcon(folder), null, ResourceLoader.CacheMode.Reuse)
			};
			button.Pressed += delegate
			{
				SelectImportCategory(index);
			};
			_importCategoryGrid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			_importCategoryCards[index] = button;
		}
	}

	private void SelectImportCategory(int index)
	{
		if (!_importCategories.ContainsKey(index))
		{
			index = 0;
		}
		_importCategoryOption?.Select(index);
		foreach (KeyValuePair<int, Button> importCategoryCard in _importCategoryCards)
		{
			importCategoryCard.Value.SetPressedNoSignal(importCategoryCard.Key == index);
		}
	}

	private void RunImportWizard()
	{
		SelectTab(3);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		List<string> list = ParseImportSources(_importSourcePath?.Text ?? "");
		if (list.Count == 0)
		{
			UpdateImportSummary(0, 0);
			SetResult("[b]导入向导[/b]\n请输入要导入的文件路径；多个路径可以换行或用分号分隔。");
			return;
		}
		List<XWImportWizard.ImportResult> list2 = new XWImportWizard().ImportFilesAndUpdateManifest(list, currentProjectPath, GetImportCategory());
		PopulateImportResults(list2);
		int num = 0;
		foreach (XWImportWizard.ImportResult item in list2)
		{
			if (item.Success)
			{
				num++;
			}
		}
		SetResult($"[b]导入向导[/b]\n已导入: {num}/{list2.Count}\n{FormatImportTargetSummary(list2)}");
	}

	private void PopulateImportResults(IReadOnlyList<XWImportWizard.ImportResult> results)
	{
		_importResults.Clear();
		if (_importResultTree == null)
		{
			return;
		}
		_importResultTree.Clear();
		TreeItem parent = _importResultTree.CreateItem();
		int num = 0;
		foreach (XWImportWizard.ImportResult result in results)
		{
			int count = _importResults.Count;
			_importResults.Add(result);
			if (result.Success)
			{
				num++;
			}
			TreeItem treeItem = _importResultTree.CreateItem(parent);
			treeItem.SetText(0, result.Success ? "成功" : "失败");
			treeItem.SetText(1, result.ResourceKey);
			treeItem.SetText(2, FormatGraphPath(result.SourcePath));
			treeItem.SetText(3, result.Success ? FormatGraphPath(result.TargetPath) : result.Error);
			treeItem.SetMetadata(0, count);
			if (!result.Success)
			{
				treeItem.SetCustomColor(0, new Color(1f, 0.35f, 0.25f));
			}
		}
		UpdateImportSummary(num, results.Count - num, FormatImportTargetFolders(results));
	}

	private void OpenImportedResource()
	{
		TreeItem treeItem = _importResultTree?.GetSelected();
		if (treeItem == null)
		{
			return;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Int)
		{
			return;
		}
		int num = metadata.AsInt32();
		if (num >= 0 && num < _importResults.Count)
		{
			XWImportWizard.ImportResult importResult = _importResults[num];
			if (importResult.Success && !OpenToolTarget(importResult.TargetPath))
			{
				SetResult("[b]导入向导[/b]\n无法打开: " + importResult.TargetPath);
			}
		}
	}

	private string GetImportCategory()
	{
		int key = _importCategoryOption?.Selected ?? 0;
		if (!_importCategories.TryGetValue(key, out var value))
		{
			return "Imported";
		}
		return value;
	}

	private void UpdateImportSummary(int success, int failed, string target = "Assets/*")
	{
		if (_importSummary != null)
		{
			_importSummary.Text = $"成功: {success}    失败: {failed}    目标: {target}";
		}
	}

	private string FormatImportTargetSummary(IReadOnlyList<XWImportWizard.ImportResult> results)
	{
		return "目标目录: " + FormatImportTargetFolders(results);
	}

	private string FormatImportTargetFolders(IReadOnlyList<XWImportWizard.ImportResult> results)
	{
		List<string> list = new List<string>();
		string currentProjectPath = GetCurrentProjectPath();
		foreach (XWImportWizard.ImportResult result in results)
		{
			if (result.Success)
			{
				string relativeFolder = GetRelativeFolder(XWImportWizard.ToModRelativePath(result.TargetPath, currentProjectPath));
				if (!string.IsNullOrWhiteSpace(relativeFolder) && !list.Contains(relativeFolder))
				{
					list.Add(relativeFolder);
				}
			}
		}
		if (list.Count != 0)
		{
			return string.Join(", ", list);
		}
		return "Assets/*";
	}

	private static string GetRelativeFolder(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return "";
		}
		string text = relativePath.Replace('\\', '/').Trim('/');
		int num = text.LastIndexOf('/');
		if (num > 0)
		{
			return text.Substring(0, num);
		}
		return "";
	}

	private void ShowTemplateLibrary()
	{
		SelectTab(1);
		XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[b]模板库[/b]");
		foreach (XWTemplateLibrary.TemplateInfo template in xWTemplateLibrary.Templates)
		{
			XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(template);
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
			handler.AppendLiteral("- ");
			handler.AppendFormatted(xWTemplatePresentation.CategoryLabel);
			handler.AppendLiteral("：");
			handler.AppendFormatted(xWTemplatePresentation.DisplayNameLabel);
			stringBuilder2.AppendLine(ref handler);
		}
		SetResult(stringBuilder.ToString());
	}

	private void ShowLocalizationHint()
	{
		SelectTab(3);
		RefreshLocalizationTable();
	}

	private void RefreshLocalizationTable()
	{
		SelectTab(3);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		_localizationTable = XWLocalizationTable.LoadFromProject(currentProjectPath);
		EnsureLocalizationRows(currentProjectPath, _localizationTable);
		PopulateLocalizationTree();
		List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
		SetResult($"[b]多语言[/b]\n文本 key: {_localizationTable.Entries.Count}\n缺失翻译: {missingTranslations.Count}");
	}

	private void SaveLocalizationTable()
	{
		SelectTab(3);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetResult("请先打开 Mod 工程。");
			return;
		}
		_localizationTable.SaveToProject(currentProjectPath, XWLocalizationTable.DefaultLocales);
		PopulateLocalizationTree();
		List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
		SetResult($"[b]多语言[/b]\n已保存到 Localization/translations.csv。\n缺失翻译: {missingTranslations.Count}");
	}

	private void PopulateLocalizationTree()
	{
		_localizationKeys.Clear();
		if (_localizationTree == null)
		{
			return;
		}
		_localizationTree.Clear();
		TreeItem parent = _localizationTree.CreateItem();
		List<string> list = new List<string>(_localizationTable.Entries.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			XWLocalizationTable.Entry entry = _localizationTable.Entries[item];
			int count = _localizationKeys.Count;
			_localizationKeys.Add(item);
			TreeItem treeItem = _localizationTree.CreateItem(parent);
			treeItem.SetText(0, item);
			treeItem.SetText(1, entry.Values.TryGetValue("zh_CN", out var value) ? value : "");
			treeItem.SetText(2, entry.Values.TryGetValue("en_US", out var value2) ? value2 : "");
			treeItem.SetText(3, IsLocalizationEntryMissing(entry) ? "缺失" : "完整");
			treeItem.SetEditable(1, enabled: true);
			treeItem.SetEditable(2, enabled: true);
			treeItem.SetMetadata(0, count);
			if (IsLocalizationEntryMissing(entry))
			{
				treeItem.SetCustomColor(3, new Color(1f, 0.62f, 0.25f));
			}
		}
		List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
		if (_localizationSummary != null)
		{
			_localizationSummary.Text = $"Key: {_localizationTable.Entries.Count}    缺失: {missingTranslations.Count}    文件: Localization/*.csv";
		}
	}

	private void EditLocalizationEntry()
	{
		TreeItem treeItem = _localizationTree?.GetSelected();
		if (treeItem == null)
		{
			return;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Int)
		{
			return;
		}
		int num = metadata.AsInt32();
		if (num < 0 || num >= _localizationKeys.Count)
		{
			return;
		}
		int selectedColumn = _localizationTree.GetSelectedColumn();
		string text = selectedColumn switch
		{
			1 => "zh_CN", 
			2 => "en_US", 
			_ => "", 
		};
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string key = _localizationKeys[num];
		_localizationTable.Set(key, text, treeItem.GetText(selectedColumn));
		if (_localizationTable.Entries.TryGetValue(key, out var value))
		{
			treeItem.SetText(3, IsLocalizationEntryMissing(value) ? "缺失" : "完整");
			if (IsLocalizationEntryMissing(value))
			{
				treeItem.SetCustomColor(3, new Color(1f, 0.62f, 0.25f));
			}
			else
			{
				treeItem.ClearCustomColor(3);
			}
		}
		List<string> missingTranslations = _localizationTable.GetMissingTranslations(XWLocalizationTable.DefaultLocales);
		if (_localizationSummary != null)
		{
			_localizationSummary.Text = $"Key: {_localizationTable.Entries.Count}    缺失: {missingTranslations.Count}    未保存";
		}
	}

	private static bool IsLocalizationEntryMissing(XWLocalizationTable.Entry entry)
	{
		string[] defaultLocales = XWLocalizationTable.DefaultLocales;
		foreach (string key in defaultLocales)
		{
			if (!entry.Values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
			{
				return true;
			}
		}
		return false;
	}

	private static void EnsureLocalizationRows(string projectPath, XWLocalizationTable table)
	{
		if (table != null && !string.IsNullOrWhiteSpace(projectPath))
		{
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectPath, "mod.json"));
			if (xWModManifest != null)
			{
				AddLocalizationRowsFromKeys(table, xWModManifest.Provides, "name");
				AddLocalizationRowsFromKeys(table, xWModManifest.Provides, "description");
				AddLocalizationRowsFromKeys(table, xWModManifest.Overrides, "name");
				AddLocalizationRowsFromKeys(table, xWModManifest.Overrides, "description");
			}
		}
	}

	private static void AddLocalizationRowsFromKeys(XWLocalizationTable table, Dictionary<string, List<string>> groups, string suffix)
	{
		if (groups == null)
		{
			return;
		}
		foreach (KeyValuePair<string, List<string>> group in groups)
		{
			string value = XWImportWizard.SanitizeKey(group.Key);
			foreach (string item in group.Value ?? new List<string>())
			{
				string value2 = XWImportWizard.SanitizeKey(item);
				if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value2))
				{
					continue;
				}
				string key = $"{value}.{value2}.{suffix}";
				string[] defaultLocales = XWLocalizationTable.DefaultLocales;
				foreach (string text in defaultLocales)
				{
					if (!table.Entries.TryGetValue(key, out var value3) || !value3.Values.ContainsKey(text))
					{
						table.Set(key, text, "");
					}
				}
			}
		}
	}

	private static List<string> ParseImportSources(string text)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		string[] array = text.Split(new char[3] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim().Trim('"');
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
		}
		return list;
	}

	private void ShowModConfirmation(string title, string message, string acceptText, Action confirmed, Action canceled = null)
	{
		if (!CanUseManagement || _modConfirmation != null)
		{
			canceled?.Invoke();
			return;
		}
		Node presentation;
		if (ManagementOnly)
		{
			ClosePlayerUtility(restoreFocus: false);
			presentation = new ColorRect
			{
				Name = "ModConfirmation",
				Color = new Color(0.15f, 0.08f, 0.02f, 0.58f),
				MouseFilter = MouseFilterEnum.Stop
			};
		}
		else
		{
			presentation = new ConfirmationDialog
			{
				Title = title,
				DialogText = message,
				OkButtonText = acceptText
			};
		}
		_modConfirmation = presentation;
		UpdatePlayerActionState();
		_cancelModConfirmation = delegate
		{
			Finish(accept: false);
		};
		if (presentation is ConfirmationDialog confirmationDialog)
		{
			confirmationDialog.Confirmed += delegate
			{
				Finish(accept: true);
			};
			confirmationDialog.Canceled += delegate
			{
				Finish(accept: false);
			};
			AddChild(confirmationDialog, forceReadableName: false, InternalMode.Disabled);
			confirmationDialog.PopupCentered(new Vector2I(620, 360));
			return;
		}
		ColorRect overlay = (ColorRect)presentation;
		(GodotObject.IsInstanceValid(ManagementConfirmationHost) ? ManagementConfirmationHost : this).AddChild(overlay, forceReadableName: false, InternalMode.Disabled);
		overlay.Theme = CreatePlayerManagementTheme();
		overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		VBoxContainer vBoxContainer = CreatePlayerDialogContent(overlay, title);
		ScrollContainer scroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0f, 64f),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		vBoxContainer.AddChild(scroll, forceReadableName: false, InternalMode.Disabled);
		Label messageLabel = PlayerLabel(message, 20, PlayerInk);
		messageLabel.Name = "ConfirmationMessage";
		scroll.AddChild(messageLabel, forceReadableName: false, InternalMode.Disabled);
		messageLabel.MinimumSizeChanged += FitMessage;
		overlay.Resized += FitMessage;
		Callable.From(FitMessage).CallDeferred();
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End
		};
		hBoxContainer.AddThemeConstantOverride("separation", 12);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = "Cancel",
			Text = "取消",
			ThemeTypeVariation = "ModSecondaryButton",
			CustomMinimumSize = new Vector2(112f, 48f)
		};
		Button button2 = new Button
		{
			Name = "Confirm",
			Text = acceptText,
			CustomMinimumSize = new Vector2(112f, 48f)
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		button.Pressed += delegate
		{
			Finish(accept: false);
		};
		button2.Pressed += delegate
		{
			Finish(accept: true);
		};
		button.GrabFocus();
		void Finish(bool accept)
		{
			if (_modConfirmation == presentation)
			{
				_modConfirmation = null;
				_cancelModConfirmation = null;
				if (GodotObject.IsInstanceValid(presentation))
				{
					if (presentation is CanvasItem canvasItem)
					{
						canvasItem.Hide();
					}
					if (presentation is Window window)
					{
						window.Hide();
					}
					presentation.QueueFree();
				}
				if (CanUseManagement)
				{
					UpdatePlayerActionState();
					if (ManagementOnly)
					{
						AudioManager.Instance?.AudioPlay("ButtonPress");
					}
					if (accept)
					{
						confirmed();
					}
					else
					{
						canceled?.Invoke();
					}
					if (ManagementOnly && CanUseManagement && _modConfirmation == null)
					{
						RestorePlayerManagementFocus();
					}
				}
			}
		}
		void FitMessage()
		{
			if (_modConfirmation == presentation && GodotObject.IsInstanceValid(scroll))
			{
				float max = Mathf.Max(64f, overlay.Size.Y - 240f);
				scroll.CustomMinimumSize = new Vector2(0f, Mathf.Clamp(messageLabel.GetMinimumSize().Y, 64f, max));
			}
		}
	}

	private void ShowModManagerStatus()
	{
		SelectTab(2);
		RefreshModList();
	}

	private void RefreshModList()
	{
		if (!CanUseManagement)
		{
			return;
		}
		XWModManager xWModManager = new XWModManager();
		List<XWModManager.ModEntry> managementSnapshot = xWModManager.GetManagementSnapshot();
		IReadOnlyList<string> readOnlyList = xWModManager.LoadEnabledIds();
		List<XWValidationIssue> list = xWModManager.ValidateEnableSet(readOnlyList);
		if (ManagementOnly)
		{
			RefreshPlayerManagement(managementSnapshot, readOnlyList.Count, list, XWModEnvironmentService.GetStatus(), xWModManager.ModsDirectory);
			return;
		}
		PopulateModTree(managementSnapshot);
		if (!ManagementOnly)
		{
			UpdateIssuePanel(list);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[b]Mod 管理[/b]");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler.AppendLiteral("安装目录: ");
		handler.AppendFormatted(xWModManager.ModsDirectory);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2);
		handler.AppendLiteral("已启用: ");
		handler.AppendFormatted(readOnlyList.Count);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2);
		handler.AppendLiteral("已发现: ");
		handler.AppendFormatted(managementSnapshot.Count);
		stringBuilder5.AppendLine(ref handler);
		XWModEnvironmentStatus status = XWModEnvironmentService.GetStatus();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
		handler.AppendLiteral("实际运行环境: ");
		handler.AppendFormatted(status.Ready ? "已应用" : "应用失败或尚未应用");
		stringBuilder6.AppendLine(ref handler);
		foreach (XWModManager.ModEntry item in managementSnapshot)
		{
			string effectiveState = item.EffectiveState;
			string value = (item.HasManifest ? "" : " (缺少 mod.json)");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 4, stringBuilder2);
			handler.AppendLiteral("- [");
			handler.AppendFormatted(effectiveState);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item.DisplayName);
			handler.AppendLiteral(" ");
			handler.AppendFormatted(item.Version);
			handler.AppendFormatted(value);
			stringBuilder7.AppendLine(ref handler);
			if (!string.IsNullOrWhiteSpace(item.LastApplyFailure))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder8 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
				handler.AppendLiteral("  最近失败：");
				handler.AppendFormatted(item.LastApplyFailure);
				stringBuilder8.AppendLine(ref handler);
			}
		}
		if (list.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[b]启用检查[/b]");
			foreach (XWValidationIssue item2 in list)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
				handler.AppendLiteral("- ");
				handler.AppendFormatted(item2.Code);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item2.Message);
				stringBuilder9.AppendLine(ref handler);
			}
		}
		SetResult(stringBuilder.ToString());
		if (_modSummary != null)
		{
			_modSummary.Text = $"目录: {xWModManager.ModsDirectory}\n已发现: {managementSnapshot.Count}    已启用: {readOnlyList.Count}    问题: {list.Count}\n实际运行: {(status.Ready ? "已应用" : "应用失败或尚未应用")}";
			if (ManagementOnly)
			{
				_modSummary.Text = $"已安装/发现：{managementSnapshot.Count}    已启用：{readOnlyList.Count}    问题：{list.Count}    实际运行：{(status.Ready ? "已应用" : "应用失败或尚未应用")}";
			}
		}
	}

	private void ChooseModPackage()
	{
		if (!CanUseManagement || _filePickerToken != Guid.Empty || _modConfirmation != null)
		{
			return;
		}
		Guid request = Guid.NewGuid();
		_filePickerToken = request;
		_importModPackageButton.Disabled = true;
		UpdatePlayerActionState();
		Error error = DisplayServer.FileDialogShow("导入 Mod", "", "", showHidden: false, DisplayServer.FileDialogMode.OpenFile, new string[1] { "*.pmod ; Mod package" }, Callable.From(delegate(bool status, string[] paths, int filterIndex)
		{
			if (CanUseManagement && !(_filePickerToken != request))
			{
				_filePickerToken = Guid.Empty;
				_importModPackageButton.Disabled = false;
				UpdatePlayerActionState();
				PrepareSelectedModPackage(status, paths, filterIndex);
			}
		}));
		if (error != Error.Ok && CanUseManagement && _filePickerToken == request)
		{
			_filePickerToken = Guid.Empty;
			_importModPackageButton.Disabled = false;
			UpdatePlayerActionState();
			ShowManagementResult("无法导入", "系统文件选择器未能打开。", failure: true, error.ToString());
		}
	}

	private void PrepareSelectedModPackage(bool status, string[] paths, int selectedFilterIndex)
	{
		if (!CanUseManagement || !status || paths == null || paths.Length == 0)
		{
			return;
		}
		if (_pendingModImport != null)
		{
			ShowManagementResult("等待确认", "请先完成或取消当前导入。");
			return;
		}
		if (!XWModPackageInstaller.TryPrepare(paths[0], "", "", out var prepared, out var diagnostic))
		{
			ShowManagementResult("导入失败", "安装包未通过检查。", failure: true, diagnostic);
			return;
		}
		if (!CanUseManagement)
		{
			prepared.Dispose();
			return;
		}
		_pendingModImport = prepared;
		Guid importToken = Guid.NewGuid();
		_pendingModImportToken = importToken;
		XWModManifest manifest = prepared.Manifest;
		XWModManager.ModEntry modEntry = new XWModManager().ScanMods().Find((XWModManager.ModEntry entry) => string.Equals(entry.Id, manifest.Id, StringComparison.OrdinalIgnoreCase));
		string value = ((modEntry == null) ? "此前未安装此 Mod。" : $"当前已安装版本：{modEntry.Version}。确认后替换为新版本：{manifest.Version}。");
		ShowModConfirmation("确认导入 Mod", $"名称：{manifest.Name}\n作者：{manifest.Author}\n版本：{manifest.Version}\nID：{manifest.Id}\n{value}\n\n确认安装此 Mod？", "安装", delegate
		{
			InstallPreparedMod(prepared, importToken);
		}, delegate
		{
			CancelPreparedMod(prepared, importToken);
		});
	}

	private void InstallPreparedMod(XWModPackageInstaller.PreparedImport prepared, Guid importToken)
	{
		if (!CanUseManagement || _pendingModImport != prepared || _pendingModImportToken != importToken)
		{
			prepared?.Dispose();
			return;
		}
		_pendingModImport = null;
		_pendingModImportToken = Guid.Empty;
		if (prepared == null)
		{
			return;
		}
		XWModManifest manifest = prepared.Manifest;
		string sha = prepared.Sha256;
		using (prepared)
		{
			XWModPackageInstaller.InstallResult installResult = XWModPackageInstaller.Install(prepared, enableAfterInstall: false);
			if (!installResult.Success)
			{
				ShowManagementResult("导入失败", "安装未完成，请查看详情。", failure: true, installResult.Message);
				return;
			}
			RefreshModList();
			if (ManagementOnly)
			{
				SelectPlayerMod(_playerEntries.Find((XWModManager.ModEntry entry) => string.Equals(entry.Id, manifest.Id, StringComparison.OrdinalIgnoreCase)));
			}
			ShowManagementResult("安装完成", manifest.Name + " 已安装。", failure: false, installResult.Message);
			if (!installResult.Enabled)
			{
				ShowEnableImportedModDialog(manifest, installResult.PackagePath, sha);
			}
		}
	}

	private void CancelPreparedMod(XWModPackageInstaller.PreparedImport prepared, Guid importToken)
	{
		if (_pendingModImport == prepared && _pendingModImportToken == importToken)
		{
			_pendingModImport?.Dispose();
			_pendingModImport = null;
			_pendingModImportToken = Guid.Empty;
		}
	}

	private void ShowEnableImportedModDialog(XWModManifest manifest, string packagePath, string expectedHash)
	{
		if (manifest == null || !CanUseManagement)
		{
			return;
		}
		ShowModConfirmation("启用 Mod", "是否启用 " + manifest.Name + "？\n启用后请重启游戏以应用更改。", "启用", delegate
		{
			XWModManager xWModManager = new XWModManager();
			if (!File.Exists(packagePath) || !XWModInstallTransaction.TryHash(packagePath, out var sha) || !string.Equals(sha, expectedHash, StringComparison.OrdinalIgnoreCase))
			{
				ShowManagementResult("启用失败", "安装包已变化，请重新导入并确认。", failure: true);
			}
			else
			{
				XWModManager.SetEnabledResult setEnabledResult = xWModManager.TrySetEnabled(manifest.Id, enabled: true);
				RefreshModList();
				ShowManagementResult(setEnabledResult.Success ? "已启用" : "启用失败", setEnabledResult.Success ? (manifest.Name + " 将在重启后生效。") : "请先解决依赖或冲突问题。", !setEnabledResult.Success, FormatManagementIssues(setEnabledResult.Issues));
			}
		});
	}

	private static string EscapeBbcode(string value)
	{
		return (value ?? "").Replace("[", "［", StringComparison.Ordinal).Replace("]", "］", StringComparison.Ordinal);
	}

	private void PopulateModTree(IReadOnlyList<XWModManager.ModEntry> entries)
	{
		if (_modTree == null)
		{
			return;
		}
		_modTree.Clear();
		TreeItem parent = _modTree.CreateItem();
		foreach (XWModManager.ModEntry entry in entries)
		{
			TreeItem treeItem = _modTree.CreateItem(parent);
			treeItem.SetCellMode(0, TreeItem.TreeCellMode.Check);
			treeItem.SetChecked(0, entry.Enabled);
			treeItem.SetMetadata(0, Variant.From<string>(entry.Id));
			treeItem.SetMetadata(1, Variant.From<string>(entry.DirectoryPath));
			treeItem.SetText(1, entry.DisplayName);
			treeItem.SetText(2, string.IsNullOrWhiteSpace(entry.Version) ? "-" : entry.Version);
			treeItem.SetText(3, (string.IsNullOrEmpty(entry.PackagePath) || entry.HasManifest) ? entry.EffectiveState : "缺少 mod.json");
			if (!entry.HasManifest)
			{
				treeItem.SetCustomColor(3, new Color(1f, 0.62f, 0.25f));
			}
		}
	}

	private void OnModSelected()
	{
		if (!CanUseManagement)
		{
			return;
		}
		string modId = GetSelectedModId();
		if (!string.IsNullOrWhiteSpace(modId))
		{
			string value = _modTree.GetSelected()?.GetMetadata(1).AsString() ?? "";
			XWModManager.ModEntry modEntry = new XWModManager().GetManagementSnapshot().Find((XWModManager.ModEntry item) => string.Equals(item.Id, modId, StringComparison.OrdinalIgnoreCase));
			SetResult($"[b]Mod 管理[/b]\n已选择: {modId}\n安装包: {value}\n安装版本: {modEntry?.InstalledVersion}\n运行版本: {modEntry?.RunningVersion}\n实际模式: {modEntry?.EffectiveMode}\n状态: {modEntry?.EffectiveState}\n安装 SHA256: {modEntry?.PackageSha256}\n运行 SHA256: {modEntry?.RunningSha256}\n最近失败: {modEntry?.LastApplyFailure}\n{modEntry?.Diagnostic}");
		}
	}

	private string GetSelectedModId()
	{
		if (!ManagementOnly)
		{
			return (_modTree?.GetSelected())?.GetMetadata(0).AsString() ?? "";
		}
		return _playerSelected?.Id ?? "";
	}

	private bool IsSelectedModEnabled()
	{
		if (ManagementOnly)
		{
			return _playerSelected?.Enabled ?? false;
		}
		return (_modTree?.GetSelected())?.IsChecked(0) ?? false;
	}

	private void SetSelectedModEnabled(bool enabled)
	{
		if (!CanUseManagement || _modConfirmation != null || _filePickerToken != Guid.Empty)
		{
			return;
		}
		SelectTab(2);
		string selectedModId = GetSelectedModId();
		if (string.IsNullOrWhiteSpace(selectedModId))
		{
			ShowManagementResult("请选择 Mod", "先在列表中选择一个 Mod。");
			return;
		}
		XWModManager xWModManager = new XWModManager();
		XWModManager.SetEnabledResult setEnabledResult = xWModManager.TrySetEnabled(selectedModId, enabled);
		List<XWValidationIssue> issues = ((setEnabledResult.Issues.Count > 0) ? setEnabledResult.Issues : xWModManager.ValidateEnableSet(xWModManager.LoadEnabledIds()));
		if (!ManagementOnly)
		{
			UpdateIssuePanel(issues);
		}
		RefreshModList();
		if (!setEnabledResult.Success)
		{
			ShowManagementResult("状态未更改", "请查看依赖、冲突或运行状态问题。", failure: true, FormatManagementIssues(issues));
			return;
		}
		string message = ((ManagementOnly || OperatingSystem.IsAndroid()) ? "状态已保存；请重启游戏后应用此状态。" : "状态已保存；请使用“重新应用 Mod”，或重启游戏后生效。");
		ShowManagementResult(enabled ? "已启用" : "已停用", message, failure: false, FormatManagementIssues(issues));
	}

	private void ConfirmDeleteSelectedMod()
	{
		if (!CanUseManagement || _modConfirmation != null || _filePickerToken != Guid.Empty)
		{
			return;
		}
		string modId = GetSelectedModId();
		if (string.IsNullOrWhiteSpace(modId))
		{
			ShowManagementResult("请选择 Mod", "先在列表中选择一个 Mod。");
			return;
		}
		XWModManager.ModEntry entry = new XWModManager().ScanMods(includeHashes: true).Find((XWModManager.ModEntry item) => string.Equals(item.Id, modId, StringComparison.OrdinalIgnoreCase));
		if (entry != null)
		{
			ShowModConfirmation("确认删除 Mod", $"将删除安装包：{entry.DisplayName} {entry.Version}\n\nMod 玩家进度会保留。确认继续？", "删除", delegate
			{
				XWModManager.DeleteResult deleteResult = new XWModManager().TryDeleteMod(entry.Id, entry.PackageSha256);
				RefreshModList();
				ShowManagementResult((!deleteResult.Success) ? "删除失败" : (deleteResult.PendingRestart ? "已安排删除" : "删除完成"), (!deleteResult.Success) ? "安装包未能删除，请查看详情。" : (deleteResult.PendingRestart ? "重启后删除安装包，玩家进度会保留。" : "已删除安装包，玩家进度已保留。"), !deleteResult.Success, deleteResult.Message);
			});
		}
	}

	private void SetupTemplateLibrary()
	{
		if (_templateOption == null)
		{
			return;
		}
		_templateIds.Clear();
		_templateCards.Clear();
		_templateOption.Clear();
		ClearVisualChoiceChildren(_templateCardGrid);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < _templateLibrary.Templates.Count; i++)
		{
			XWTemplateLibrary.TemplateInfo templateInfo = _templateLibrary.Templates[i];
			XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(templateInfo);
			Texture2D texture2D = ResourceLoader.Load<Texture2D>(ResolveTemplateCategoryIcon(templateInfo.Category), null, ResourceLoader.CacheMode.Reuse);
			_templateOption.AddItem(xWTemplatePresentation.CategoryLabel + " / " + xWTemplatePresentation.DisplayNameLabel, i);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				_templateOption.SetItemIcon(i, texture2D);
			}
			_templateIds[i] = templateInfo.Id;
			if (_templateCardGrid != null)
			{
				int cardIndex = i;
				Button button = new Button
				{
					Name = $"TemplateCard_{i}",
					Text = xWTemplatePresentation.CategoryLabel + "\n" + xWTemplatePresentation.DisplayNameLabel,
					TooltipText = xWTemplatePresentation.PreviewTextLabel + "\n默认目录：" + xWTemplatePresentation.DefaultFolderLabel,
					ToggleMode = true,
					ButtonGroup = buttonGroup,
					CustomMinimumSize = new Vector2(176f, 72f),
					Icon = texture2D,
					ExpandIcon = false
				};
				button.Pressed += delegate
				{
					SelectTemplate(cardIndex);
				};
				_templateCardGrid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
				_templateCards[i] = button;
			}
		}
		if (_templateLibrary.Templates.Count > 0)
		{
			SelectTemplate(0L);
		}
	}

	private void SelectTemplate(long index)
	{
		int num = (int)index;
		if (!_templateIds.TryGetValue(num, out var value))
		{
			return;
		}
		XWTemplateLibrary.TemplateInfo templateInfo = FindTemplate(value);
		if (templateInfo == null)
		{
			return;
		}
		XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(templateInfo);
		if (_templateName != null && string.IsNullOrWhiteSpace(_templateName.Text))
		{
			_templateName.PlaceholderText = "例如：" + xWTemplatePresentation.SuggestedName;
		}
		if (_templatePreview != null)
		{
			_templatePreview.Text = $"{xWTemplatePresentation.CategoryLabel} / {xWTemplatePresentation.DisplayNameLabel}\n{xWTemplatePresentation.DescriptionLabel}\n\n用途预览：{xWTemplatePresentation.PreviewTextLabel}\n\n技术结构：\n{templateInfo.Content}";
		}
		if (_templateStatus != null)
		{
			_templateStatus.Text = "默认目录：" + xWTemplatePresentation.DefaultFolderLabel;
		}
		_templateOption?.Select(num);
		foreach (KeyValuePair<int, Button> templateCard in _templateCards)
		{
			templateCard.Value.SetPressedNoSignal(templateCard.Key == num);
		}
	}

	private static void ClearVisualChoiceChildren(Node parent)
	{
		if (parent == null)
		{
			return;
		}
		foreach (Node child in parent.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static string ResolveTemplateCategoryIcon(string category)
	{
		if (category != null)
		{
			int length = category.Length;
			if (length <= 5)
			{
				if (length != 3)
				{
					if (length == 5)
					{
						char c = category[0];
						if (c != 'A')
						{
							if (c == 'L' && category == "Level")
							{
								goto IL_0159;
							}
						}
						else if (category == "Audio")
						{
							goto IL_0161;
						}
					}
				}
				else
				{
					char c = category[0];
					if (c != 'B')
					{
						if (c != 'G')
						{
							if (c == 'M' && category == "Map")
							{
								goto IL_0159;
							}
						}
						else if (category == "GUI")
						{
							return "res://addons/ModEditor/Icons/ResourceGUI.svg";
						}
					}
					else if (category == "BGM")
					{
						goto IL_0161;
					}
				}
			}
			else if (length != 9)
			{
				if (length != 10)
				{
					if (length == 14 && category == "CharacterEvent")
					{
						goto IL_0141;
					}
				}
				else if (category == "Projectile")
				{
					return "res://addons/ModEditor/Icons/ResourceProjectile.svg";
				}
			}
			else
			{
				char c = category[1];
				if (c != 'h')
				{
					if (c != 'l')
					{
						if (c == 'o' && category == "Component")
						{
							goto IL_0141;
						}
					}
					else if (category == "Blueprint")
					{
						return "res://addons/ModEditor/Icons/ClassIcon/VisualShader.svg";
					}
				}
				else if (category == "Character")
				{
					goto IL_0141;
				}
			}
		}
		return "res://addons/ModEditor/Icons/ScriptCreate.svg";
		IL_0159:
		return "res://addons/ModEditor/Icons/ResourceLevel.svg";
		IL_0161:
		return "res://addons/ModEditor/Icons/ResourceAudio.svg";
		IL_0141:
		return "res://addons/ModEditor/Icons/ResourceCharacter.svg";
	}

	private static string ResolveImportCategoryIcon(string folder)
	{
		switch (folder)
		{
		case "Images":
			return "res://addons/ModEditor/Icons/ClassIcon/Image.svg";
		case "Sfx":
		case "BGM":
			return "res://addons/ModEditor/Icons/ClassIcon/AudioStream.svg";
		case "Fonts":
			return "res://addons/ModEditor/Icons/ClassIcon/Font.svg";
		case "Scenes":
			return "res://addons/ModEditor/Icons/PackedScene.svg";
		case "UI":
			return "res://addons/ModEditor/Icons/ResourceGUI.svg";
		default:
			return "res://addons/ModEditor/Icons/File.svg";
		}
	}

	private void CreateTemplateFromSelection()
	{
		SelectTab(1);
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrEmpty(currentProjectPath))
		{
			SetTemplateStatus("请先打开 Mod 工程。");
			return;
		}
		int key = _templateOption?.Selected ?? (-1);
		if (!_templateIds.TryGetValue(key, out var value))
		{
			SetTemplateStatus("请先选择模板。");
			return;
		}
		XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(FindTemplate(value));
		string text = _templateName?.Text?.Trim() ?? "";
		bool num = string.IsNullOrWhiteSpace(text);
		string resourceName = (num ? xWTemplatePresentation.SuggestedName : text);
		string displayName = (num ? xWTemplatePresentation.DisplayNameLabel : text);
		XWTemplateLibrary.TemplateCreateResult templateCreateResult = _templateLibrary.CreateFromTemplate(value, currentProjectPath, resourceName, displayName);
		if (!templateCreateResult.Success)
		{
			SetTemplateStatus(templateCreateResult.Error);
			SetResult("[b]模板库[/b]\n创建失败: " + templateCreateResult.Error);
			return;
		}
		SetTemplateStatus("已创建: " + templateCreateResult.CreatedPath);
		SetResult("[b]模板库[/b]\n已创建: " + templateCreateResult.CreatedPath);
		IEnumerable<string> paths;
		if (templateCreateResult.CreatedPaths.Count <= 0)
		{
			IEnumerable<string> enumerable = new string[1] { templateCreateResult.CreatedPath };
			paths = enumerable;
		}
		else
		{
			IEnumerable<string> enumerable = templateCreateResult.CreatedPaths;
			paths = enumerable;
		}
		XWModManifestSyncService.RegisterPaths(currentProjectPath, paths);
		foreach (string createdPath in templateCreateResult.CreatedPaths)
		{
			if (createdPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				XWBPCSharpMemberRegistry.Instance.NotifyScriptSaved(createdPath);
			}
		}
		XWFileSystem.GetSingleton()?.ScanChanges();
		if (!string.IsNullOrWhiteSpace(templateCreateResult.Warning))
		{
			XWEditorInterface.Instance?.ShowToast("模板已创建，但存在清理提示：" + templateCreateResult.Warning, 1);
		}
		if (XWFileSystemPanel.Instance != null)
		{
			XWFileSystemPanel.Instance.CompleteTemplateCreationFromTool(templateCreateResult);
		}
		else if (templateCreateResult.CreatedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			XWEditorInterface.Instance?.GetScriptEditor()?.OpenFileAt(templateCreateResult.CreatedPath, 0, 0);
		}
	}

	private XWTemplateLibrary.TemplateInfo FindTemplate(string templateId)
	{
		foreach (XWTemplateLibrary.TemplateInfo template in _templateLibrary.Templates)
		{
			if (template.Id == templateId)
			{
				return template;
			}
		}
		return null;
	}

	private void SetTemplateStatus(string text)
	{
		if (_templateStatus != null)
		{
			_templateStatus.Text = text;
		}
		SetResult("[b]模板库[/b]\n" + text);
	}

	private static string FindOverrideResourcePath(string projectPath, XWModManifest manifest, string category, string key)
	{
		if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(key))
		{
			return "";
		}
		foreach (string resource in manifest.Resources)
		{
			if (IsLikelyOverridePath(resource, category, key))
			{
				string text = Path.Combine(projectPath, resource.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		foreach (string overrideSearchFolder in GetOverrideSearchFolders(projectPath, category))
		{
			if (!Directory.Exists(overrideSearchFolder))
			{
				continue;
			}
			foreach (string item in Directory.EnumerateFiles(overrideSearchFolder, "*.*", SearchOption.AllDirectories))
			{
				if (!item.Contains($"{Path.DirectorySeparatorChar}.build{Path.DirectorySeparatorChar}") && IsLikelyOverridePath(item, category, key))
				{
					return item;
				}
			}
		}
		return "";
	}

	private static IEnumerable<string> GetOverrideSearchFolders(string projectPath, string category)
	{
		if (!string.IsNullOrWhiteSpace(category))
		{
			yield return Path.Combine(projectPath, "Resources", category);
		}
		yield return Path.Combine(projectPath, "Resources");
		yield return projectPath;
	}

	private static bool IsLikelyOverridePath(string path, string category, string key)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		string text = path.Replace('\\', '/').ToLowerInvariant();
		string text2 = (category ?? "").ToLowerInvariant();
		string text3 = key.Replace('\\', '/').Trim('/').ToLowerInvariant();
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text3.Replace('/', Path.DirectorySeparatorChar));
		string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(text.Replace('/', Path.DirectorySeparatorChar));
		if (!(fileNameWithoutExtension2 == text3) && !(fileNameWithoutExtension2 == fileNameWithoutExtension) && !text.Contains("/" + text3 + ".") && !text.Contains("/" + fileNameWithoutExtension + "."))
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text.Contains("/" + text2.ToLowerInvariant() + "/");
		}
		return true;
	}

	private static string FormatDiffValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "启用" : "关闭";
			case 4:
				goto IL_005c;
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			}
		}
		switch (variantType)
		{
		case Variant.Type.StringName:
		case Variant.Type.NodePath:
			break;
		case Variant.Type.Object:
			return value.AsGodotObject()?.GetType().Name ?? "空";
		default:
			return value.ToString();
		}
		goto IL_005c;
		IL_005c:
		return value.AsString();
	}

	private static string NormalizeProjectPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.StartsWith("res://") || path.StartsWith("uid://"))
		{
			return path;
		}
		string text = path.Replace('\\', '/');
		string text2 = ProjectSettings.GlobalizePath("res://").Replace('\\', '/');
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return "res://" + text.Substring(text2.Length);
		}
		return path;
	}

	private static void UpdateIssuePanel(List<XWValidationIssue> issues)
	{
		if (XWEditorInterface.Instance?.GetIssuePanel() is XWIssuePanel xWIssuePanel)
		{
			xWIssuePanel.LoadIssues(issues);
		}
	}

	private void SelectTab(int index)
	{
		if (_toolTabs != null && index >= 0 && index < _toolTabs.GetTabCount())
		{
			_toolTabs.CurrentTab = index;
		}
	}

	private string GetCurrentProjectPath()
	{
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}

	private void SetResult(string text)
	{
		if (_result != null)
		{
			_result.Clear();
			_result.AppendText(text);
		}
	}

	private void AddExperienceActions(VBoxContainer root)
	{
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ContentActions"
		};
		root.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		if (ManagementPlayRequested != null)
		{
			_playerPlay = PlayerButton("PlayModLevels", "游玩 Mod 关卡", delegate
			{
				if (CanUseManagement)
				{
					if (XWModContentCatalog.GetCatalogs().Count == 0)
					{
						SetPlayerNotice("没有已加载的 Mod 关卡。请导入含关卡目录的 Mod，启用后重启游戏。", PlayerGold, showDetails: false);
					}
					else
					{
						ManagementPlayRequested();
					}
				}
			}, 200);
			hFlowContainer.AddChild(_playerPlay, forceReadableName: false, InternalMode.Disabled);
		}
		if (!Global.IsMobile && ManagementCreateRequested != null)
		{
			_playerCreate = PlayerButton("CreateMod", "制作 Mod（F3）", delegate
			{
				ManagementCreateRequested();
			}, 200, "ModSecondaryButton");
			hFlowContainer.AddChild(_playerCreate, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddProgressRecoveryActions(VBoxContainer actions)
	{
		string owner = _playerSelected?.Id;
		bool flag = !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(GameSaveManager.Instance?.GetUserCurrent());
		Button button = PlayerButton("RestoreModProgress", "从备份恢复进度", delegate
		{
			ConfirmProgressRepair(owner, reset: false);
		}, 200, "ModSecondaryButton");
		button.Disabled = !flag || XWModPlayerProgressService.GetStatus(owner) != XWModPlayerProgressService.ProgressStatus.Recoverable;
		actions.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = PlayerButton("ResetModProgress", "重置 Mod 进度", delegate
		{
			ConfirmProgressRepair(owner, reset: true);
		}, 200, "ModDangerButton");
		button2.Disabled = !flag;
		actions.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
	}

	private void ConfirmProgressRepair(string owner, bool reset)
	{
		ClosePlayerUtility(restoreFocus: false);
		string account = GameSaveManager.Instance?.GetUserCurrent();
		ShowModConfirmation(reset ? "重置 Mod 进度" : "恢复 Mod 进度", reset ? "将先备份原文件，再重置当前账号在此 Mod 中的通关、解锁和收藏。中途战斗存档、其他 Mod 和原版进度保留。" : "将先保护当前文件，再使用最后有效备份恢复此 Mod 的进度配置。", reset ? "备份并重置" : "恢复", delegate
		{
			if (account != GameSaveManager.Instance?.GetUserCurrent())
			{
				SetPlayerNotice("账号已改变，请重新选择 Mod 后操作。", PlayerError, showDetails: true);
			}
			else
			{
				string reason;
				bool flag = (reset ? XWModPlayerProgressService.TryResetProgress(owner, out reason) : XWModPlayerProgressService.TryRestoreBackup(owner, out reason));
				RefreshModList();
				SetPlayerNotice(reason, flag ? PlayerGreen : PlayerError, !flag);
			}
		});
	}

	private void ConfigureManagementOnly()
	{
		GetNode<Control>("Root").Hide();
		base.CustomMinimumSize = Vector2.Zero;
		base.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		base.SizeFlagsVertical = SizeFlags.ExpandFill;
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "PlayerLayout"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 20);
		AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "ManagementToolbar",
			CustomMinimumSize = new Vector2(0f, 48f)
		};
		hBoxContainer.AddThemeConstantOverride("separation", 12);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		AddExperienceActions(vBoxContainer);
		_playerCounts = PlayerLabel("", 16, PlayerMuted, wrap: false);
		_playerCounts.Name = "InstalledCounts";
		_playerCounts.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		_playerCounts.VerticalAlignment = VerticalAlignment.Center;
		hBoxContainer.AddChild(_playerCounts, forceReadableName: false, InternalMode.Disabled);
		_importModPackageButton = PlayerButton("ImportMod", "导入 Mod", ChooseModPackage, 144);
		hBoxContainer.AddChild(_importModPackageButton, forceReadableName: false, InternalMode.Disabled);
		_playerRefresh = PlayerButton("RefreshMods", "刷新", RefreshModList, 96, "ModSecondaryButton");
		hBoxContainer.AddChild(_playerRefresh, forceReadableName: false, InternalMode.Disabled);
		_playerColumns = new HBoxContainer
		{
			Name = "Columns",
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_playerColumns.AddThemeConstantOverride("separation", 24);
		vBoxContainer.AddChild(_playerColumns, forceReadableName: false, InternalMode.Disabled);
		_playerDirectory = new PanelContainer
		{
			Name = "Directory",
			CustomMinimumSize = new Vector2(300f, 0f)
		};
		StyleBoxFlat styleBoxFlat = PlayerFlat(new Color(0.5f, 0.28f, 0.08f, 0.05f), new Color(0.5f, 0.28f, 0.08f, 0.25f), 0, 0);
		styleBoxFlat.BorderWidthRight = 1;
		styleBoxFlat.ContentMarginRight = 16f;
		styleBoxFlat.ContentMarginTop = 8f;
		styleBoxFlat.ContentMarginBottom = 8f;
		_playerDirectory.AddThemeStyleboxOverride("panel", styleBoxFlat);
		_playerColumns.AddChild(_playerDirectory, forceReadableName: false, InternalMode.Disabled);
		_playerListScroll = PlayerScroll("ModListScroll");
		_playerDirectory.AddChild(_playerListScroll, forceReadableName: false, InternalMode.Disabled);
		_playerList = new VBoxContainer
		{
			Name = "ModList",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_playerList.AddThemeConstantOverride("separation", 8);
		_playerListScroll.AddChild(_playerList, forceReadableName: false, InternalMode.Disabled);
		_playerDetailScroll = PlayerScroll("DetailScroll");
		_playerColumns.AddChild(_playerDetailScroll, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			Name = "SelectedMod",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer2.AddThemeConstantOverride("separation", 12);
		_playerDetailScroll.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_playerName = PlayerLabel("", 26, PlayerInk);
		_playerName.Name = "ModName";
		_playerName.MaxLinesVisible = 2;
		_playerName.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		vBoxContainer2.AddChild(_playerName, forceReadableName: false, InternalMode.Disabled);
		_playerAuthor = PlayerLabel("", 16, PlayerMuted);
		vBoxContainer2.AddChild(_playerAuthor, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(PlayerRule(), forceReadableName: false, InternalMode.Disabled);
		_playerDescription = PlayerLabel("", 20, PlayerInk);
		_playerDescription.Name = "ModDescription";
		vBoxContainer2.AddChild(_playerDescription, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Control
		{
			CustomMinimumSize = new Vector2(0f, 8f),
			MouseFilter = MouseFilterEnum.Ignore
		}, forceReadableName: false, InternalMode.Disabled);
		_playerEnabled = PlayerLabel("", 20, PlayerInk);
		_playerEnabled.Name = "EnabledSetting";
		_playerRunning = PlayerLabel("", 20, PlayerInk);
		_playerRunning.Name = "RunningState";
		_playerVersions = PlayerLabel("", 16, PlayerMuted);
		_playerMode = PlayerLabel("", 16, PlayerMuted);
		_playerFailure = PlayerLabel("", 16, PlayerError);
		Label[] array = new Label[5] { _playerEnabled, _playerRunning, _playerVersions, _playerMode, _playerFailure };
		foreach (Label node in array)
		{
			vBoxContainer2.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
		_playerEmpty = new CenterContainer
		{
			Name = "EmptyState",
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(_playerEmpty, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer3 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(400f, 0f)
		};
		vBoxContainer3.AddThemeConstantOverride("separation", 20);
		_playerEmpty.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		Label label = PlayerLabel("还没有安装 Mod", 26, PlayerInk);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		vBoxContainer3.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Label label2 = PlayerLabel("导入 .pmod 文件，为游戏添加新内容。", 20, PlayerMuted);
		label2.HorizontalAlignment = HorizontalAlignment.Center;
		vBoxContainer3.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		_playerEmptyImport = PlayerButton("EmptyImport", "导入 Mod", ChooseModPackage, 180);
		_playerEmptyImport.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		vBoxContainer3.AddChild(_playerEmptyImport, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer
		{
			Name = "ManagementFooter",
			CustomMinimumSize = new Vector2(0f, 48f)
		};
		hBoxContainer2.AddThemeConstantOverride("separation", 16);
		vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_playerReturn = PlayerButton("BackToOptions", "返回选项", delegate
		{
			ManagementReturnRequested?.Invoke();
		}, 144, "ModSecondaryButton");
		hBoxContainer2.AddChild(_playerReturn, forceReadableName: false, InternalMode.Disabled);
		_playerNotice = PlayerLabel("", 16, PlayerGold, wrap: false);
		_playerNotice.Name = "OperationNotice";
		_playerNotice.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		_playerNotice.VerticalAlignment = VerticalAlignment.Center;
		hBoxContainer2.AddChild(_playerNotice, forceReadableName: false, InternalMode.Disabled);
		_playerToggle = PlayerButton("ToggleEnabled", "启用", delegate
		{
			SetSelectedModEnabled(!IsSelectedModEnabled());
		}, 144);
		hBoxContainer2.AddChild(_playerToggle, forceReadableName: false, InternalMode.Disabled);
		_playerMore = PlayerButton("MoreActions", "更多", OpenPlayerMore, 104, "ModSecondaryButton");
		hBoxContainer2.AddChild(_playerMore, forceReadableName: false, InternalMode.Disabled);
		base.Resized += ResizePlayerDirectory;
		ResizePlayerDirectory();
		_playerReturn.GrabFocus();
	}

	private void ResizePlayerDirectory()
	{
		if (GodotObject.IsInstanceValid(_playerDirectory))
		{
			_playerDirectory.CustomMinimumSize = new Vector2(Mathf.Clamp(base.Size.X * 0.3f, 240f, 300f), 0f);
		}
	}

	private void RefreshPlayerManagement(List<XWModManager.ModEntry> entries, int enabledCount, List<XWValidationIssue> issues, XWModEnvironmentStatus environment, string directory)
	{
		string selectedKey = PlayerEntryKey(_playerSelected);
		bool flag = _playerRows.Values.Any((Button row) => row.HasFocus());
		int scrollVertical = _playerListScroll.ScrollVertical;
		_playerEntries = entries;
		foreach (Node child in _playerList.GetChildren())
		{
			_playerList.RemoveChild(child);
			child.QueueFree();
		}
		_playerRows.Clear();
		_playerColumns.Visible = entries.Count > 0;
		_playerEmpty.Visible = entries.Count == 0;
		_importModPackageButton.Visible = entries.Count > 0;
		_playerToggle.Visible = entries.Count > 0;
		_playerSelected = entries.Find((XWModManager.ModEntry entry2) => PlayerEntryKey(entry2).Equals(selectedKey, StringComparison.OrdinalIgnoreCase)) ?? entries.FirstOrDefault();
		foreach (XWModManager.ModEntry entry in entries)
		{
			string key = PlayerEntryKey(entry);
			Button button = new Button
			{
				Name = "ModRow",
				ThemeTypeVariation = "ModListRow",
				ToggleMode = true,
				CustomMinimumSize = new Vector2(0f, 72f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				TooltipText = entry.DisplayName + "\n" + entry.Id
			};
			ColorRect colorRect = new ColorRect
			{
				Name = "Bookmark",
				Color = new Color("ad6b22"),
				MouseFilter = MouseFilterEnum.Ignore
			};
			button.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
			colorRect.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide, LayoutPresetMode.Minsize);
			colorRect.OffsetRight = 5f;
			colorRect.OffsetTop = 8f;
			colorRect.OffsetBottom = -8f;
			MarginContainer marginContainer = new MarginContainer
			{
				MouseFilter = MouseFilterEnum.Ignore
			};
			marginContainer.AddThemeConstantOverride("margin_left", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 14);
			marginContainer.AddThemeConstantOverride("margin_top", 8);
			marginContainer.AddThemeConstantOverride("margin_bottom", 8);
			button.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
			marginContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				MouseFilter = MouseFilterEnum.Ignore,
				Alignment = BoxContainer.AlignmentMode.Center
			};
			vBoxContainer.AddThemeConstantOverride("separation", 3);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			Label label = PlayerLabel(entry.DisplayName, 20, PlayerInk, wrap: false);
			label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
			Label label2 = PlayerLabel(VersionLabel(entry.Version) + "  ·  " + PlayerState(entry), 16, PlayerStateColor(entry), wrap: false);
			label2.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			vBoxContainer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
			button.Pressed += delegate
			{
				SelectPlayerMod(entry);
			};
			button.FocusEntered += delegate
			{
				SelectPlayerMod(entry);
			};
			_playerList.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			_playerRows[key] = button;
		}
		int value = entries.Count((XWModManager.ModEntry modEntry) => !string.IsNullOrEmpty(modEntry.PackagePath));
		_playerCounts.Text = $"已安装 {value}    已启用 {enabledCount}";
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2);
		handler.AppendLiteral("安装目录：");
		handler.AppendFormatted(directory);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2);
		handler.AppendLiteral("运行环境：");
		handler.AppendFormatted(environment.Ready ? "已应用" : "尚未应用");
		handler.AppendLiteral(" / ");
		handler.AppendFormatted(environment.Reason);
		stringBuilder4.AppendLine(ref handler);
		foreach (XWValidationIssue issue in issues)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
			handler.AppendFormatted(issue.Code);
			handler.AppendLiteral(": ");
			handler.AppendFormatted(issue.Message);
			stringBuilder5.AppendLine(ref handler);
		}
		_playerSnapshotDetails = stringBuilder.ToString();
		bool flag2 = !environment.Ready;
		if (flag2)
		{
			bool flag3;
			switch (environment.Reason)
			{
			case "LifecycleFailed":
			case "ApplyFailed":
			case "StorageRecoveryBlocked":
			case "InvalidEnabledState":
			case "NonFormalRuntime":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		bool flag4 = flag2;
		flag2 = !environment.Ready;
		if (flag2)
		{
			bool flag3;
			switch (environment.Reason)
			{
			case "EnabledSetChanged":
			case "LifecycleRollback":
			case "InstalledPackageChanged":
			case "ManagedRuntimeChanged":
			case "RuntimeUnloaded":
			case "RuntimeApplied":
			case "PackageExtracted":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		bool flag5 = flag2;
		bool flag6 = flag4 || issues.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error) || entries.Any(PlayerHasFailure);
		bool flag7 = entries.Any((XWModManager.ModEntry modEntry) => modEntry.PendingDelete || modEntry.Enabled != modEntry.Loaded || modEntry.EffectiveState == "已加载旧版本") || flag5;
		_playerSnapshotNotice = (flag6 ? "部分 Mod 存在问题，请查看状态和详情。" : (flag7 ? "有更改尚未应用，请重启游戏。" : ((!environment.Ready) ? "Mod 运行环境尚未就绪，请查看详情。" : "")));
		_playerSnapshotNoticeColor = (flag6 ? PlayerError : PlayerGold);
		_playerOperationDetails = "";
		SetPlayerNotice(_playerSnapshotNotice, _playerSnapshotNoticeColor, flag6 || (!environment.Ready && !flag7));
		RenderPlayerSelection();
		_playerListScroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, scrollVertical);
		if (flag && _playerSelected != null && _playerRows.TryGetValue(PlayerEntryKey(_playerSelected), out var value2))
		{
			value2.GrabFocus();
		}
	}

	private void SelectPlayerMod(XWModManager.ModEntry entry)
	{
		if (CanUseManagement && _modConfirmation == null && !(_filePickerToken != Guid.Empty) && _playerUtilityOverlay == null)
		{
			bool num = PlayerEntryKey(entry) != PlayerEntryKey(_playerSelected);
			_playerSelected = entry;
			if (num)
			{
				_playerDetailScroll.ScrollVertical = 0;
			}
			RenderPlayerSelection();
		}
	}

	private void RenderPlayerSelection()
	{
		XWModManager.ModEntry playerSelected = _playerSelected;
		foreach (KeyValuePair<string, Button> playerRow in _playerRows)
		{
			playerRow.Value.SetPressedNoSignal(playerRow.Key.Equals(PlayerEntryKey(playerSelected), StringComparison.OrdinalIgnoreCase));
			playerRow.Value.GetNode<ColorRect>("Bookmark").Visible = playerRow.Value.ButtonPressed;
		}
		if (playerSelected != null)
		{
			_playerName.Text = playerSelected.DisplayName;
			_playerName.TooltipText = playerSelected.DisplayName;
			_playerName.MouseFilter = MouseFilterEnum.Pass;
			_playerAuthor.Text = (string.IsNullOrWhiteSpace(playerSelected.Manifest?.Author) ? VersionLabel(playerSelected.Version) : (VersionLabel(playerSelected.Version) + "  ·  " + playerSelected.Manifest.Author));
			_playerDescription.Text = (string.IsNullOrWhiteSpace(playerSelected.Manifest?.Description) ? "作者尚未填写介绍。" : playerSelected.Manifest.Description);
			_playerEnabled.Text = "启用设置：" + (playerSelected.Enabled ? "已启用" : "已停用");
			_playerRunning.Text = "当前运行：" + PlayerRunningState(playerSelected);
			_playerRunning.AddThemeColorOverride("font_color", PlayerStateColor(playerSelected));
			_playerVersions.Visible = playerSelected.Loaded && (playerSelected.InstalledVersion != playerSelected.RunningVersion || playerSelected.EffectiveState == "已加载旧版本");
			_playerVersions.Text = "安装版本：" + VersionLabel(playerSelected.InstalledVersion) + "    运行版本：" + VersionLabel(playerSelected.RunningVersion);
			_playerMode.Visible = playerSelected.Loaded && !string.IsNullOrWhiteSpace(playerSelected.EffectiveMode);
			_playerMode.Text = "运行内容：" + playerSelected.EffectiveMode;
			string value = (string.IsNullOrWhiteSpace(playerSelected.LastApplyFailure) ? playerSelected.Diagnostic : playerSelected.LastApplyFailure);
			_playerFailure.Visible = !string.IsNullOrWhiteSpace(value);
			_playerFailure.Text = "此 Mod 存在问题，请在“更多”中查看诊断。";
			_playerToggle.Text = (playerSelected.Enabled ? "停用" : "启用");
		}
		UpdatePlayerActionState();
		UpdatePlayerTechnicalText();
	}

	private void UpdatePlayerActionState()
	{
		if (!ManagementOnly || !GodotObject.IsInstanceValid(_playerRefresh))
		{
			return;
		}
		bool flag = _filePickerToken != Guid.Empty || _modConfirmation != null || _playerUtilityOverlay != null || !CanUseManagement;
		_importModPackageButton.Disabled = flag;
		_playerEmptyImport.Disabled = flag;
		_playerRefresh.Disabled = flag;
		_playerReturn.Disabled = flag;
		_playerMore.Disabled = flag;
		if (_playerPlay != null)
		{
			_playerPlay.Disabled = flag;
		}
		if (_playerCreate != null)
		{
			_playerCreate.Disabled = flag;
		}
		foreach (Button value in _playerRows.Values)
		{
			value.Disabled = flag;
		}
		_playerToggle.Disabled = flag || _playerSelected == null || (!_playerSelected.Enabled && (!_playerSelected.HasManifest || _playerSelected.PendingDelete));
	}

	internal bool TryClosePlayerUtility()
	{
		return ClosePlayerUtility();
	}

	private bool ClosePlayerUtility(bool restoreFocus = true)
	{
		if (_playerUtilityOverlay == null)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(_playerUtilityOverlay))
		{
			_playerUtilityOverlay.Hide();
			_playerUtilityOverlay.QueueFree();
		}
		_playerUtilityOverlay = null;
		_playerTechnical = null;
		if (CanUseManagement)
		{
			UpdatePlayerActionState();
			if (restoreFocus)
			{
				_playerMore.GrabFocus();
			}
		}
		return true;
	}

	private ColorRect AddPlayerUtilityOverlay(string name, Color shade)
	{
		ColorRect colorRect = (ColorRect)(_playerUtilityOverlay = new ColorRect
		{
			Name = name,
			Color = shade,
			MouseFilter = MouseFilterEnum.Stop
		});
		(GodotObject.IsInstanceValid(ManagementConfirmationHost) ? ManagementConfirmationHost : this).AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		colorRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		UpdatePlayerActionState();
		return colorRect;
	}

	private void OpenPlayerMore()
	{
		if (!CanUseManagement || _modConfirmation != null || _filePickerToken != Guid.Empty || _playerUtilityOverlay != null)
		{
			return;
		}
		ColorRect overlay = AddPlayerUtilityOverlay("MoreActionsOverlay", Colors.Transparent);
		overlay.GuiInput += delegate(InputEvent input)
		{
			if (_playerUtilityOverlay == overlay && input is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				ClosePlayerUtility();
			}
		};
		PanelContainer panelContainer = new PanelContainer
		{
			Name = "MoreMenu",
			MouseFilter = MouseFilterEnum.Stop
		};
		StyleBoxTexture styleBoxTexture = CreatePlayerPaperFrame();
		float num = (styleBoxTexture.ContentMarginBottom = 12f);
		float num3 = (styleBoxTexture.ContentMarginTop = num);
		float contentMarginLeft = (styleBoxTexture.ContentMarginRight = num3);
		styleBoxTexture.ContentMarginLeft = contentMarginLeft;
		panelContainer.AddThemeStyleboxOverride("panel", styleBoxTexture);
		overlay.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		panelContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.Minsize);
		panelContainer.OffsetLeft = -260f;
		panelContainer.OffsetRight = -36f;
		panelContainer.OffsetTop = -340f;
		panelContainer.OffsetBottom = -96f;
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Button button = PlayerButton("ViewDiagnostics", "查看诊断", delegate
		{
			if (CanUseManagement && _playerUtilityOverlay == overlay)
			{
				ClosePlayerUtility(restoreFocus: false);
				OpenPlayerDiagnostics();
			}
		}, 200, "ModSecondaryButton");
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = PlayerButton("DeletePackage", "删除安装包", delegate
		{
			if (CanUseManagement && _playerUtilityOverlay == overlay)
			{
				ClosePlayerUtility(restoreFocus: false);
				ConfirmDeleteSelectedMod();
			}
		}, 200, "ModDangerButton");
		button2.Disabled = _playerSelected == null || string.IsNullOrEmpty(_playerSelected.PackagePath) || _playerSelected.PendingDelete;
		vBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		AddProgressRecoveryActions(vBoxContainer);
		button.GrabFocus();
	}

	private void OpenPlayerDiagnostics()
	{
		if (!CanUseManagement || _modConfirmation != null || _filePickerToken != Guid.Empty || _playerUtilityOverlay != null)
		{
			return;
		}
		ColorRect overlay = AddPlayerUtilityOverlay("DiagnosticsOverlay", new Color(0.15f, 0.08f, 0.02f, 0.58f));
		VBoxContainer vBoxContainer = CreatePlayerDialogContent(overlay, "Mod 诊断", 760);
		_playerTechnical = new RichTextLabel
		{
			Name = "TechnicalText",
			BbcodeEnabled = false,
			SelectionEnabled = true,
			CustomMinimumSize = new Vector2(0f, Mathf.Min(300f, GetViewportRect().Size.Y - 240f)),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ScrollActive = true,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_playerTechnical.AddThemeFontSizeOverride("normal_font_size", 16);
		vBoxContainer.AddChild(_playerTechnical, forceReadableName: false, InternalMode.Disabled);
		UpdatePlayerTechnicalText();
		Button button = PlayerButton("CloseDiagnostics", "返回管理", delegate
		{
			if (_playerUtilityOverlay == overlay)
			{
				ClosePlayerUtility();
			}
		}, 144, "ModSecondaryButton");
		button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		button.GrabFocus();
	}

	private void RestorePlayerManagementFocus()
	{
		if (CanUseManagement)
		{
			if (_playerToggle.Visible && !_playerToggle.Disabled)
			{
				_playerToggle.GrabFocus();
			}
			else if (_playerEmptyImport.Visible && _playerEmpty.Visible && !_playerEmptyImport.Disabled)
			{
				_playerEmptyImport.GrabFocus();
			}
			else if (!_playerMore.Disabled)
			{
				_playerMore.GrabFocus();
			}
		}
	}

	private void UpdatePlayerTechnicalText()
	{
		if (_playerTechnical != null)
		{
			StringBuilder stringBuilder = new StringBuilder(_playerSnapshotDetails);
			XWModManager.ModEntry playerSelected = _playerSelected;
			if (playerSelected != null)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 2, stringBuilder2);
				handler.AppendLiteral("\nMod ID：");
				handler.AppendFormatted(playerSelected.Id);
				handler.AppendLiteral("\n安装包：");
				handler.AppendFormatted(playerSelected.PackagePath);
				stringBuilder3.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(11, 2, stringBuilder2);
				handler.AppendLiteral("安装版本：");
				handler.AppendFormatted(playerSelected.InstalledVersion);
				handler.AppendLiteral("\n运行版本：");
				handler.AppendFormatted(playerSelected.RunningVersion);
				stringBuilder4.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(21, 2, stringBuilder2);
				handler.AppendLiteral("安装 SHA256：");
				handler.AppendFormatted(playerSelected.PackageSha256);
				handler.AppendLiteral("\n运行 SHA256：");
				handler.AppendFormatted(playerSelected.RunningSha256);
				stringBuilder5.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 3, stringBuilder2);
				handler.AppendLiteral("实际模式：");
				handler.AppendFormatted(playerSelected.EffectiveMode);
				handler.AppendLiteral("\n最近失败：");
				handler.AppendFormatted(playerSelected.LastApplyFailure);
				handler.AppendLiteral("\n");
				handler.AppendFormatted(playerSelected.Diagnostic);
				stringBuilder6.AppendLine(ref handler);
			}
			if (!string.IsNullOrWhiteSpace(_playerOperationDetails))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
				handler.AppendLiteral("\n最近操作：\n");
				handler.AppendFormatted(_playerOperationDetails);
				stringBuilder7.AppendLine(ref handler);
			}
			_playerTechnical.Text = stringBuilder.ToString();
		}
	}

	private void ShowManagementResult(string title, string message, bool failure = false, string details = "")
	{
		if (!ManagementOnly)
		{
			SetResult("[b]" + EscapeBbcode(title) + "[/b]\n" + EscapeBbcode(message) + (string.IsNullOrWhiteSpace(details) ? "" : ("\n" + EscapeBbcode(details))));
		}
		else if (CanUseManagement)
		{
			_playerOperationDetails = $"{title}\n{message}\n{details}";
			string message2 = title + "：" + ShortPlayerMessage(message);
			if (!failure && !string.IsNullOrWhiteSpace(_playerSnapshotNotice))
			{
				message2 = title + "。" + _playerSnapshotNotice;
			}
			SetPlayerNotice(message2, failure ? PlayerError : ((_playerSnapshotNotice.Length > 0) ? _playerSnapshotNoticeColor : PlayerGreen), failure || !string.IsNullOrWhiteSpace(details));
			UpdatePlayerTechnicalText();
		}
	}

	private void SetPlayerNotice(string message, Color color, bool showDetails)
	{
		_playerNotice.Text = message;
		_playerNotice.TooltipText = message + (showDetails ? "\n更多 → 查看诊断" : "");
		_playerNotice.MouseFilter = MouseFilterEnum.Pass;
		_playerNotice.AddThemeColorOverride("font_color", color);
	}

	private static string PlayerEntryKey(XWModManager.ModEntry entry)
	{
		if (entry != null)
		{
			return entry.Id + "\n" + entry.PackagePath;
		}
		return "";
	}

	private static string FormatManagementIssues(IEnumerable<XWValidationIssue> issues)
	{
		return string.Join("\n", issues.Select((XWValidationIssue issue) => $"{issue.Code}: {issue.Message}"));
	}

	private static string VersionLabel(string version)
	{
		if (!string.IsNullOrWhiteSpace(version))
		{
			return "v" + version;
		}
		return "版本未提供";
	}

	private static bool PlayerHasFailure(XWModManager.ModEntry entry)
	{
		bool flag = !entry.HasManifest || (entry.Enabled && !string.IsNullOrWhiteSpace(entry.LastApplyFailure));
		if (!flag)
		{
			string effectiveState = entry.EffectiveState;
			bool flag2 = ((effectiveState == "加载失败" || effectiveState == "回滚受阻") ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static string PlayerState(XWModManager.ModEntry entry)
	{
		if (entry.HasManifest)
		{
			if (!entry.PendingDelete)
			{
				if (entry.Enabled || !(entry.EffectiveState != "回滚受阻"))
				{
					if (!PlayerHasFailure(entry))
					{
						if (!entry.Loaded || entry.Enabled)
						{
							return entry.EffectiveState;
						}
						return "已加载，重启后停用";
					}
					return entry.EffectiveState;
				}
				if (!entry.Loaded)
				{
					return "已安装，未启用";
				}
				return "已停用设置，重启后停用";
			}
			return "待删除，重启后移除";
		}
		if (!string.IsNullOrEmpty(entry.PackagePath))
		{
			return "安装包异常";
		}
		return "缺少安装包";
	}

	private static string PlayerRunningState(XWModManager.ModEntry entry)
	{
		if (entry.HasManifest && !entry.PendingDelete && !PlayerHasFailure(entry))
		{
			if (!entry.Loaded)
			{
				if (!entry.Enabled)
				{
					return "未加载";
				}
				return "未加载，重启后启用";
			}
			return PlayerState(entry);
		}
		return PlayerState(entry);
	}

	private static Color PlayerStateColor(XWModManager.ModEntry entry)
	{
		if (!PlayerHasFailure(entry))
		{
			if (!entry.PendingDelete && entry.Loaded == entry.Enabled && !(entry.EffectiveState == "已加载旧版本"))
			{
				if (!entry.Loaded)
				{
					return PlayerMuted;
				}
				return PlayerGreen;
			}
			return PlayerGold;
		}
		return PlayerError;
	}

	private static string ShortPlayerMessage(string message)
	{
		string text = (message ?? "").Replace("\r", "").Split('\n', 2)[0];
		if (text.Length <= 72)
		{
			return text;
		}
		return text.Substring(0, 72) + "…";
	}

	private static Label PlayerLabel(string text, int size, Color color, bool wrap = true)
	{
		Label label = new Label();
		label.Text = text;
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		label.MouseFilter = MouseFilterEnum.Ignore;
		label.AutowrapMode = (TextServer.AutowrapMode)(wrap ? 3 : 0);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static Button PlayerButton(string name, string text, Action pressed, int width, string variation = "")
	{
		Button button = new Button();
		button.Name = name;
		button.Text = text;
		button.CustomMinimumSize = new Vector2(width, 48f);
		button.ThemeTypeVariation = variation;
		button.Pressed += delegate
		{
			AudioManager.Instance?.AudioPlay("ButtonPress");
			pressed();
		};
		return button;
	}

	private static ScrollContainer PlayerScroll(string name)
	{
		return new ScrollContainer
		{
			Name = name,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			FollowFocus = true,
			ClipContents = true
		};
	}

	private static Control PlayerRule()
	{
		return new ColorRect
		{
			Color = new Color(0.5f, 0.28f, 0.08f, 0.25f),
			CustomMinimumSize = new Vector2(0f, 1f),
			MouseFilter = MouseFilterEnum.Ignore
		};
	}

	private static VBoxContainer CreatePlayerDialogContent(Control overlay, string title, int preferredWidth = 640)
	{
		CenterContainer centerContainer = new CenterContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		overlay.AddChild(centerContainer, forceReadableName: false, InternalMode.Disabled);
		centerContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		PanelContainer frame = new PanelContainer
		{
			Name = "PaperDialogFrame",
			MouseFilter = MouseFilterEnum.Stop
		};
		frame.AddThemeStyleboxOverride("panel", CreatePlayerPaperFrame());
		centerContainer.AddChild(frame, forceReadableName: false, InternalMode.Disabled);
		overlay.Resized += ResizeFrame;
		ResizeFrame();
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 18);
		frame.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Label label = PlayerLabel(title, 26, PlayerInk);
		label.Name = "DialogHeading";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
		void ResizeFrame()
		{
			frame.CustomMinimumSize = new Vector2(Mathf.Min(preferredWidth, overlay.Size.X - 64f), 0f);
		}
	}

	private static StyleBoxFlat PlayerFlat(Color background, Color border, int borderWidth, int padding)
	{
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat();
		styleBoxFlat.BgColor = background;
		styleBoxFlat.BorderColor = border;
		styleBoxFlat.CornerRadiusTopLeft = 4;
		styleBoxFlat.CornerRadiusTopRight = 4;
		styleBoxFlat.CornerRadiusBottomLeft = 4;
		styleBoxFlat.CornerRadiusBottomRight = 4;
		styleBoxFlat.ContentMarginLeft = padding;
		styleBoxFlat.ContentMarginRight = padding;
		styleBoxFlat.ContentMarginTop = padding;
		styleBoxFlat.ContentMarginBottom = padding;
		styleBoxFlat.SetBorderWidthAll(borderWidth);
		return styleBoxFlat;
	}

	internal static StyleBoxTexture CreatePlayerPaperFrame()
	{
		return new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Option/LevelOptionButton.png"),
			TextureMarginLeft = 14f,
			TextureMarginRight = 14f,
			TextureMarginTop = 12f,
			TextureMarginBottom = 18f,
			ContentMarginLeft = 28f,
			ContentMarginRight = 28f,
			ContentMarginTop = 28f,
			ContentMarginBottom = 28f
		};
	}

	internal static Theme CreatePlayerManagementTheme()
	{
		Theme theme = new Theme
		{
			DefaultFont = GD.Load<Font>("res://Asset/Font/fzkt.ttf"),
			DefaultFontSize = 20
		};
		StyleBoxTexture styleBoxTexture = new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>("res://Asset/Texture/GUI/General/General/SeedChooserButton.png"),
			TextureMarginLeft = 8f,
			TextureMarginRight = 8f,
			TextureMarginTop = 6f,
			TextureMarginBottom = 6f,
			ContentMarginLeft = 16f,
			ContentMarginRight = 16f,
			ContentMarginTop = 8f,
			ContentMarginBottom = 8f
		};
		StyleBoxTexture styleBoxTexture2 = (StyleBoxTexture)styleBoxTexture.Duplicate();
		styleBoxTexture2.ModulateColor = new Color(0.82f, 0.82f, 0.82f);
		StyleBoxTexture styleBoxTexture3 = (StyleBoxTexture)styleBoxTexture.Duplicate();
		styleBoxTexture3.ModulateColor = new Color(1.08f, 1.08f, 1.08f);
		StyleBoxTexture styleBoxTexture4 = (StyleBoxTexture)styleBoxTexture.Duplicate();
		styleBoxTexture4.Texture = GD.Load<Texture2D>("res://Asset/Texture/GUI/General/General/SeedChooserButtonDisabled.png");
		theme.SetStylebox("normal", "Button", styleBoxTexture);
		theme.SetStylebox("hover", "Button", styleBoxTexture3);
		theme.SetStylebox("pressed", "Button", styleBoxTexture2);
		theme.SetStylebox("disabled", "Button", styleBoxTexture4);
		theme.SetStylebox("focus", "Button", PlayerFlat(Colors.Transparent, new Color("f5d482"), 2, 0));
		theme.SetColor("font_color", "Button", new Color("a8e665"));
		theme.SetColor("font_hover_color", "Button", new Color("d7ffab"));
		theme.SetColor("font_pressed_color", "Button", new Color("bce879"));
		theme.SetColor("font_focus_color", "Button", new Color("c7f293"));
		theme.SetColor("font_disabled_color", "Button", new Color("b29872"));
		theme.SetColor("font_outline_color", "Button", new Color("47240f"));
		theme.SetFontSize("font_size", "Button", 20);
		theme.SetConstant("outline_size", "Button", 2);
		string[] array = new string[3] { "ModSecondaryButton", "ModDangerButton", "ModListRow" };
		foreach (string text in array)
		{
			theme.SetTypeVariation(text, "Button");
		}
		array = new string[4] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" };
		foreach (string text2 in array)
		{
			theme.SetColor(text2, "ModSecondaryButton", new Color("f8dfad"));
			theme.SetColor(text2, "ModDangerButton", new Color("f0b090"));
		}
		StyleBoxFlat texture = PlayerFlat(new Color(1f, 0.88f, 0.62f, 0.22f), new Color(0.5f, 0.28f, 0.08f, 0.2f), 1, 0);
		theme.SetStylebox("normal", "ModListRow", texture);
		theme.SetStylebox("disabled", "ModListRow", texture);
		theme.SetStylebox("hover", "ModListRow", PlayerFlat(new Color("f2d6a4"), new Color("b1874e"), 1, 0));
		theme.SetStylebox("pressed", "ModListRow", PlayerFlat(new Color("f5d99b"), new Color("ac772f"), 1, 0));
		theme.SetStylebox("hover_pressed", "ModListRow", PlayerFlat(new Color("ffe5ae"), new Color("ac772f"), 1, 0));
		theme.SetStylebox("focus", "ModListRow", PlayerFlat(Colors.Transparent, new Color("956025"), 2, 0));
		theme.SetColor("font_color", "Label", PlayerInk);
		theme.SetColor("default_color", "RichTextLabel", PlayerInk);
		theme.SetStylebox("scroll", "VScrollBar", PlayerFlat(new Color(0.5f, 0.28f, 0.08f, 0.12f), Colors.Transparent, 0, 5));
		theme.SetStylebox("grabber", "VScrollBar", PlayerFlat(new Color("ba884f"), new Color("996637"), 1, 5));
		theme.SetStylebox("grabber_highlight", "VScrollBar", PlayerFlat(new Color("d5a262"), PlayerGold, 1, 5));
		theme.SetStylebox("grabber_pressed", "VScrollBar", PlayerFlat(new Color("996637"), PlayerGold, 1, 5));
		return theme;
	}
}
