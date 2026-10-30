using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Debugging;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPEditor : PanelContainer
{
	private sealed class BlueprintDocumentSession
	{
		public XWBPScript Script;

		public XWBPScriptData Data;

		public int HistoryId = -1;
	}

	private XWBPGraphEdit _graphEdit;

	private XWBPEditorGraphTree _graphTree;

	private XWBPEditorFunctionTree _functionTree;

	private XWBPEditorVariableTree _variableTree;

	private XWBPEditorSignalTree _signalTree;

	private XWBPValidationPanel _validationPanel;

	private FileDialog _generatedScriptSaveDialog;

	private string _pendingGeneratedCode = "";

	private Godot.Timer _persistBlueprintTimer;

	private TabContainer _blueprintSidebar;

	private PanelContainer _blueprintDirectPropertyPanel;

	private Label _blueprintDirectPropertyTitle;

	private VBoxContainer _blueprintDirectPropertyHost;

	private GodotObject _editingBlueprintObject;

	private readonly List<XWVisualSegmentedOption> _blueprintVisualOptions = new List<XWVisualSegmentedOption>();

	private bool _blueprintDirectPropertyRefreshQueued;

	private CancellationTokenSource _safePreviewCancellation;

	private int _safePreviewTraceCount;

	private readonly HashSet<string> _safePreviewUniqueNodes = new HashSet<string>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, BlueprintDocumentSession> _documentSessions = new System.Collections.Generic.Dictionary<string, BlueprintDocumentSession>(StringComparer.OrdinalIgnoreCase);

	private BlueprintDocumentSession _activeDocument;

	private bool _persistingBlueprint;

	private string _activeProjectRoot = "";

	private readonly object _blueprintDebugSnapshotLock = new object();

	private readonly XWModDebugController _blueprintDebugController = new XWModDebugController();

	private XWModDebugWorkbench _blueprintDebugWorkbench;

	private XWModDebugSnapshot _pendingBlueprintDebugSnapshot;

	private int _blueprintDebugNavigationQueued;

	public XWBPScript BpScript { get; private set; }

	public XWBPScriptData BpScriptData { get; private set; }

	public XWUndoRedoManager UndoRedoManager { get; private set; }

	public XWBPRealTimeValidator RealTimeValidator { get; private set; }

	public int CurrentBlueprintHistoryId => _activeDocument?.HistoryId ?? (-1);

	public XWBlueprintRuntimeResult LastSafePreviewResult { get; private set; }

	public bool SafePreviewRunning { get; private set; }

	public string ActiveModProjectRoot => GetActiveModProjectRoot();

	public bool IsDebugWorkbenchBound => _blueprintDebugWorkbench?.IsControllerBound ?? false;

	public bool IsDebugWorkbenchHiddenProcessSuspended => _blueprintDebugWorkbench?.IsHiddenProcessSuspended ?? true;

	public int DebugWorkbenchCallStackCount => _blueprintDebugWorkbench?.CallStackItemCount ?? 0;

	public int DebugWorkbenchVariableCount => _blueprintDebugWorkbench?.VariableItemCount ?? 0;

	public XWBPGraphEdit GetGraphEditor()
	{
		return _graphEdit;
	}

	public override void _Ready()
	{
		_graphEdit = GetNode<XWBPGraphEdit>("%XWBPGraphEdit");
		_validationPanel = GetNode<XWBPValidationPanel>("%XWBPValidationPanel");
		_graphTree = GetNode<XWBPEditorGraphTree>("%XWBPEditorGraphTree");
		_functionTree = GetNode<XWBPEditorFunctionTree>("%XWBPEditorFunctionTree");
		_variableTree = GetNode<XWBPEditorVariableTree>("%XWBPEditorVariableTree");
		_signalTree = GetNode<XWBPEditorSignalTree>("%XWBPEditorSignalTree");
		_generatedScriptSaveDialog = GetNode<FileDialog>("%GeneratedScriptSaveDialog");
		_blueprintSidebar = GetNode<TabContainer>("%BlueprintSidebar");
		_blueprintDirectPropertyPanel = GetNode<PanelContainer>("%BlueprintDirectPropertyPanel");
		_blueprintDirectPropertyTitle = GetNode<Label>("%BlueprintDirectPropertyTitle");
		_blueprintDirectPropertyHost = GetNode<VBoxContainer>("%BlueprintDirectPropertyHost");
		_blueprintSidebar.SetTabTitle(0, "属性");
		_blueprintSidebar.SetTabTitle(1, "成员");
		InitializeBlueprintDebugging();
		_generatedScriptSaveDialog.FileSelected += OnGeneratedScriptPathSelected;
		_generatedScriptSaveDialog.Canceled += delegate
		{
			_pendingGeneratedCode = "";
		};
		_persistBlueprintTimer = new Godot.Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_persistBlueprintTimer.Timeout += PersistBlueprintState;
		AddChild(_persistBlueprintTimer, forceReadableName: false, InternalMode.Disabled);
		_graphEdit.Editor = this;
		_graphTree.Editor = this;
		_functionTree.Editor = this;
		_variableTree.Editor = this;
		_signalTree.Editor = this;
		_validationPanel.SetEditor(this);
		GetNode<Button>("%GraphAddButton").Pressed += OnGraphAddPressed;
		GetNode<Button>("%FunctionAddButton").Pressed += OnFunctionAddPressed;
		GetNode<Button>("%VariableAddButton").Pressed += OnVariableAddPressed;
		GetNode<Button>("%SignalAddButton").Pressed += OnSignalAddPressed;
		UndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager();
		UndoRedoManager.HistoryChanged += OnUndoRedoHistoryChanged;
		RealTimeValidator = new XWBPRealTimeValidator(this);
		RealTimeValidator.ValidationComplete += OnRealTimeValidationComplete;
		ClearBlueprintDirectProperties();
	}

	public override void _ExitTree()
	{
		FlushBlueprintPersistence();
		ShutdownBlueprintDebugging();
		_safePreviewCancellation?.Cancel();
		_safePreviewCancellation?.Dispose();
		_safePreviewCancellation = null;
		if (GodotObject.IsInstanceValid(_persistBlueprintTimer))
		{
			_persistBlueprintTimer.Stop();
		}
		RealTimeValidator?.Disable();
		DetachActiveDocumentSignals();
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			UndoRedoManager.HistoryChanged -= OnUndoRedoHistoryChanged;
			foreach (BlueprintDocumentSession value in _documentSessions.Values)
			{
				if (value.HistoryId >= 0)
				{
					UndoRedoManager.ReleaseHistory(value.HistoryId);
				}
			}
		}
		_documentSessions.Clear();
		_activeDocument = null;
		base._ExitTree();
	}

	public void Init(XWBPScript bpScript)
	{
		if (!GodotObject.IsInstanceValid(bpScript))
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(_activeProjectRoot) && !IsPathInsideRoot(bpScript.ResourcePath, _activeProjectRoot))
		{
			XWEditorInterface.Instance?.ShowToast("该蓝图不属于当前 Mod，已拒绝打开。", 2);
			return;
		}
		FlushBlueprintPersistence();
		RealTimeValidator?.Disable();
		DetachActiveDocumentSignals();
		ClearBlueprintDirectProperties();
		string blueprintDocumentKey = GetBlueprintDocumentKey(bpScript);
		if (!_documentSessions.TryGetValue(blueprintDocumentKey, out var value))
		{
			value = new BlueprintDocumentSession
			{
				Script = bpScript,
				Data = bpScript.Deserialize(),
				HistoryId = (UndoRedoManager?.CreateHistoryScope() ?? (-1))
			};
			_documentSessions[blueprintDocumentKey] = value;
		}
		else
		{
			value.Script = bpScript;
		}
		_activeDocument = value;
		BpScript = value.Script;
		BpScriptData = value.Data;
		if (value.HistoryId >= 0)
		{
			UndoRedoManager?.SetCurrentHistoryType(value.HistoryId);
		}
		BpScriptData.ExtendsClassChanged += OnExtendsClassChanged;
		_graphEdit?.UpdateExtendsClassLabel();
		InitGraph();
		InitFunction();
		InitVariable();
		InitSignal();
		if (BpScriptData.Graphs.Count > 0)
		{
			System.Collections.Generic.Dictionary<int, XWBPGraphData>.ValueCollection.Enumerator enumerator = BpScriptData.Graphs.Values.GetEnumerator();
			if (enumerator.MoveNext())
			{
				OpenGraph(enumerator.Current);
			}
		}
		RealTimeValidator?.Enable();
		RealTimeValidator?.DebounceTimer?.Start();
	}

	private static string GetBlueprintDocumentKey(XWBPScript script)
	{
		string text = script?.ResourcePath?.Replace('\\', '/').Trim() ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return $"instance://{script?.GetInstanceId() ?? 0}";
	}

	public bool TrySwitchProjectRoot(string projectRoot)
	{
		string text = NormalizeAbsolutePath(projectRoot);
		if (string.Equals(_activeProjectRoot, text, StringComparison.OrdinalIgnoreCase))
		{
			XWBPCSharpMemberRegistry instance = XWBPCSharpMemberRegistry.Instance;
			instance.ConfigureProjectRoot(text);
			instance.EnsureProjectScannedAsync();
			return true;
		}
		if (_documentSessions.Count > 0 && SaveAllBlueprints(showToast: false) < 0)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(_persistBlueprintTimer))
		{
			_persistBlueprintTimer.Stop();
		}
		RealTimeValidator?.Disable();
		DetachActiveDocumentSignals();
		ClearBlueprintDirectProperties();
		_graphEdit?.Init(null);
		_graphTree?.Clear();
		_functionTree?.Clear();
		_variableTree?.Clear();
		_signalTree?.Clear();
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			foreach (BlueprintDocumentSession value in _documentSessions.Values)
			{
				if (value.HistoryId >= 0)
				{
					UndoRedoManager.ReleaseHistory(value.HistoryId);
				}
			}
		}
		_documentSessions.Clear();
		_activeDocument = null;
		BpScript = null;
		BpScriptData = null;
		_editingBlueprintObject = null;
		_activeProjectRoot = text;
		XWBPCSharpMemberRegistry instance2 = XWBPCSharpMemberRegistry.Instance;
		instance2.ConfigureProjectRoot(text);
		instance2.EnsureProjectScannedAsync();
		return true;
	}

	private void OnExtendsClassChanged(StringName oldClass, StringName newClass)
	{
		_graphEdit?.UpdateExtendsClassLabel();
	}

	private void DetachActiveDocumentSignals()
	{
		if (GodotObject.IsInstanceValid(BpScriptData))
		{
			BpScriptData.ExtendsClassChanged -= OnExtendsClassChanged;
		}
	}

	public override void _Notification(int what)
	{
		if ((long)what != 31)
		{
			return;
		}
		if (IsVisibleInTree())
		{
			if (CurrentBlueprintHistoryId >= 0)
			{
				UndoRedoManager?.SetCurrentHistoryType(CurrentBlueprintHistoryId);
			}
		}
		else if (SafePreviewRunning)
		{
			CancelSafePreview();
		}
	}

	private void InitGraph()
	{
		_graphTree.Init(GetAllGraphs());
	}

	private void InitFunction()
	{
		_functionTree.Init(GetAllFunctions());
	}

	private void InitVariable()
	{
		_variableTree.Init(GetAllVariables());
	}

	private void InitSignal()
	{
		_signalTree.Init(GetAllSignals());
	}

	private IEnumerable<XWBPGraphData> GetAllGraphs()
	{
		if (BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPGraphData> graph in BpScriptData.Graphs)
		{
			yield return graph.Value;
		}
	}

	private IEnumerable<XWBPFunctionData> GetAllFunctions()
	{
		if (BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPFunctionData> function in BpScriptData.Functions)
		{
			yield return function.Value;
		}
	}

	private IEnumerable<XWBPVariableData> GetAllVariables()
	{
		if (BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPVariableData> variable in BpScriptData.Variables)
		{
			yield return variable.Value;
		}
	}

	private IEnumerable<XWBPSignalData> GetAllSignals()
	{
		if (BpScriptData == null)
		{
			yield break;
		}
		foreach (KeyValuePair<int, XWBPSignalData> signalData in BpScriptData.SignalDatas)
		{
			yield return signalData.Value;
		}
	}

	public void OpenGraph(XWBPGraphData graph)
	{
		_graphEdit.Init(graph);
	}

	public void FocusValidationResult(XWBPValidationResult result)
	{
		if (result != null && result.GraphData != null)
		{
			OpenGraph(result.GraphData);
			if (result.NodeId >= 0)
			{
				_graphEdit.FocusNode(result.NodeId);
			}
		}
	}

	public void FocusNode(int nodeId)
	{
		if (nodeId >= 0 && BpScriptData != null)
		{
			XWBPGraphData xWBPGraphData = FindGraphContainingNode(nodeId);
			if (xWBPGraphData != null)
			{
				OpenGraph(xWBPGraphData);
				_graphEdit.FocusNode(nodeId);
			}
		}
	}

	private XWBPGraphData FindGraphContainingNode(int nodeId)
	{
		foreach (XWBPGraphData allGraph in GetAllGraphs())
		{
			if (allGraph.Nodes.ContainsKey(nodeId))
			{
				return allGraph;
			}
		}
		foreach (XWBPFunctionData allFunction in GetAllFunctions())
		{
			if (allFunction.Nodes.ContainsKey(nodeId))
			{
				return allFunction;
			}
		}
		return null;
	}

	public void GenerateScriptButtonPressed()
	{
		if (BpScriptData == null || !GodotObject.IsInstanceValid(BpScript))
		{
			XWEditorInterface.Instance?.ShowToast("没有可生成的蓝图脚本。");
			return;
		}
		if (!SaveBlueprintResourceIfPossible())
		{
			XWEditorInterface.Instance?.ShowToast("请先把蓝图保存到当前 Mod，再生成 C#。", 2);
			return;
		}
		string text;
		try
		{
			text = new XWBPCodeGenerator(BpScriptData)
			{
				ClassName = XWBPCodeGenerator.BuildGeneratedClassName(BpScript.ResourcePath, GetActiveModProjectRoot()),
				BlueprintSourcePath = GetBlueprintCallbackSourcePath(BpScript.ResourcePath, GetActiveModProjectRoot())
			}.Generate();
		}
		catch (Exception ex)
		{
			XWEditorInterface.Instance?.ShowToast("蓝图代码生成失败: " + ex.Message);
			XWEditorInterface.Instance?.AddOutputMessage($"蓝图代码生成失败: {ex}", XWOutputPanel.MessageType.Error);
			return;
		}
		string defaultGeneratedCodePath = GetDefaultGeneratedCodePath();
		if (string.IsNullOrWhiteSpace(defaultGeneratedCodePath))
		{
			_pendingGeneratedCode = text;
			_generatedScriptSaveDialog.CurrentFile = "Blueprint.generated.cs";
			_generatedScriptSaveDialog.PopupCentered();
		}
		else
		{
			SaveGeneratedCode(defaultGeneratedCodePath, text);
		}
	}

	public async Task<XWBlueprintRuntimeResult> RunSafePreviewAsync()
	{
		if (SafePreviewRunning)
		{
			return LastSafePreviewResult;
		}
		XWBPGraphData xWBPGraphData = _graphEdit?.GraphData;
		if (!GodotObject.IsInstanceValid(xWBPGraphData))
		{
			_graphEdit?.SetRuntimePreviewState(running: false, "⚠ 没有图表", new Color("ff7b72"), "请先打开一个蓝图图表");
			return null;
		}
		_safePreviewCancellation?.Cancel();
		_safePreviewCancellation?.Dispose();
		_safePreviewCancellation = new CancellationTokenSource();
		_safePreviewTraceCount = 0;
		_safePreviewUniqueNodes.Clear();
		BeginBlueprintDebugging();
		SafePreviewRunning = true;
		LastSafePreviewResult = null;
		_graphEdit.SetRuntimePreviewState(running: true, "▶ 安全运行中", new Color("ffc857"), "正在逐节点运行纯数据预览");
		XWBlueprintRuntimeResult xWBlueprintRuntimeResult;
		try
		{
			xWBlueprintRuntimeResult = await XWBlueprintSafeRuntime.ExecuteGraphAsync(xWBPGraphData, new XWBlueprintRuntimeContext
			{
				CancellationToken = _safePreviewCancellation.Token,
				NodeVisitedAsync = OnSafePreviewNodeVisitedAsync,
				NodeLocationVisitedAsync = OnSafePreviewNodeLocationVisitedAsync,
				DelayAsync = async delegate
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				},
				Output = delegate(string message)
				{
					XWEditorInterface.Instance?.AddOutputMessage("[蓝图预览] " + message);
				},
				EventTriggered = delegate(string eventName)
				{
					XWEditorInterface.Instance?.AddOutputMessage("[蓝图预览事件] " + eventName);
				},
				ModProjectRoot = GetSafePreviewModProjectRoot(),
				SelfClassName = (BpScriptData?.ExtendsClass.ToString() ?? "BlueprintSelf"),
				SelfProperties = BuildSafePreviewSelfProperties(),
				BlueprintVariables = BuildSafePreviewVariables(),
				ScriptData = BpScriptData,
				DebugController = _blueprintDebugController,
				BlueprintSourcePath = (BpScript?.ResourcePath ?? string.Empty)
			});
		}
		catch (OperationCanceledException)
		{
			xWBlueprintRuntimeResult = new XWBlueprintRuntimeResult
			{
				WasCancelled = true,
				StopReason = "蓝图预览已取消。"
			};
			xWBlueprintRuntimeResult.Diagnostics.Add(xWBlueprintRuntimeResult.StopReason);
		}
		catch (Exception ex2)
		{
			xWBlueprintRuntimeResult = new XWBlueprintRuntimeResult
			{
				StopReason = "蓝图安全预览失败: " + ex2.Message
			};
			xWBlueprintRuntimeResult.Diagnostics.Add(xWBlueprintRuntimeResult.StopReason);
			XWEditorInterface.Instance?.AddOutputMessage($"蓝图安全预览失败: {ex2}", XWOutputPanel.MessageType.Error);
		}
		finally
		{
			SafePreviewRunning = false;
			EndBlueprintDebugging();
		}
		LastSafePreviewResult = xWBlueprintRuntimeResult;
		if (xWBlueprintRuntimeResult.Success)
		{
			string tooltip = ((xWBlueprintRuntimeResult.OutputMessages.Count > 0) ? string.Join(" · ", xWBlueprintRuntimeResult.OutputMessages.Take(2)) : "流程完成");
			_graphEdit.SetRuntimePreviewState(running: false, $"✓ {xWBlueprintRuntimeResult.StepCount} 步", new Color("59d185"), tooltip);
			XWEditorInterface.Instance?.ShowToast($"蓝图安全预览完成：{xWBlueprintRuntimeResult.StepCount} 步");
		}
		else
		{
			string text = (string.IsNullOrWhiteSpace(xWBlueprintRuntimeResult.StopReason) ? "预览未完成" : xWBlueprintRuntimeResult.StopReason);
			_graphEdit.SetRuntimePreviewState(running: false, "⚠ 预览受限", new Color("ff7b72"), text);
			XWEditorInterface.Instance?.AddOutputMessage("[蓝图预览已停止] " + text, XWOutputPanel.MessageType.Error);
		}
		return xWBlueprintRuntimeResult;
	}

	public void CancelSafePreview()
	{
		_blueprintDebugController.Stop();
		_safePreviewCancellation?.Cancel();
	}

	private static string GetSafePreviewModProjectRoot()
	{
		return (XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel)?.GetCurrentProject()?.ProjectPath ?? string.Empty;
	}

	private System.Collections.Generic.Dictionary<string, Variant> BuildSafePreviewSelfProperties()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);
		if (BpScriptData?.Variables == null)
		{
			return dictionary;
		}
		foreach (XWBPVariableData value in BpScriptData.Variables.Values)
		{
			if (value != null && XWBlueprintSafeObject.IsSafeIdentifier(value.Name))
			{
				dictionary[value.Name] = value.DefaultValue;
			}
		}
		return dictionary;
	}

	private System.Collections.Generic.Dictionary<int, Variant> BuildSafePreviewVariables()
	{
		System.Collections.Generic.Dictionary<int, Variant> dictionary = new System.Collections.Generic.Dictionary<int, Variant>();
		if (BpScriptData?.Variables == null)
		{
			return dictionary;
		}
		foreach (var (key, xWBPVariableData2) in BpScriptData.Variables)
		{
			if (xWBPVariableData2 != null)
			{
				dictionary[key] = xWBPVariableData2.DefaultValue;
			}
		}
		return dictionary;
	}

	private async Task OnSafePreviewNodeVisitedAsync(int nodeId)
	{
		_safePreviewTraceCount++;
		if (_safePreviewUniqueNodes.Add($"node:{nodeId}") || _safePreviewTraceCount % 24 == 0)
		{
			_graphEdit?.HighlightRuntimeNode(nodeId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task OnSafePreviewNodeLocationVisitedAsync(XWBlueprintRuntimeTracePoint point)
	{
		if (point == null)
		{
			return;
		}
		_safePreviewTraceCount++;
		if (_safePreviewUniqueNodes.Add(point.Location) || _safePreviewTraceCount % 24 == 0)
		{
			if (GodotObject.IsInstanceValid(point.Graph) && _graphEdit?.GraphData != point.Graph)
			{
				OpenGraph(point.Graph);
			}
			_graphEdit?.HighlightRuntimeNode(point.NodeId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void OnGeneratedScriptPathSelected(string path)
	{
		string pendingGeneratedCode = _pendingGeneratedCode;
		_pendingGeneratedCode = "";
		if (!string.IsNullOrWhiteSpace(pendingGeneratedCode))
		{
			SaveGeneratedCode(path, pendingGeneratedCode);
		}
	}

	private bool SaveBlueprintResourceIfPossible()
	{
		return PersistBlueprintSession(_activeDocument, showFailureToast: false, "保存蓝图资源");
	}

	private string GetDefaultGeneratedCodePath()
	{
		string text = BpScript?.ResourcePath ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string file = text.GetFile().GetBaseName() + ".generated.cs";
		return text.GetBaseDir().PathJoin(file);
	}

	private void SaveGeneratedCode(string path, string code)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		string text = BpScript?.ResourcePath ?? "";
		string defaultGeneratedCodePath = GetDefaultGeneratedCodePath();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(defaultGeneratedCodePath) || !string.Equals(NormalizeAbsolutePath(path), NormalizeAbsolutePath(defaultGeneratedCodePath), StringComparison.OrdinalIgnoreCase))
		{
			XWEditorInterface.Instance?.ShowToast("生成脚本必须与源蓝图同名并保存在同一目录。", 2);
			return;
		}
		string code2 = XWBlueprintGeneratedCSharpPolicy.AttachMetadata(code, text, path);
		if (!TryWriteGeneratedCodeAtomically(path, code2, out var error))
		{
			XWEditorInterface.Instance?.ShowToast("无法保存生成脚本: " + error, 2);
			return;
		}
		XWEditorInterface.Instance?.AddOutputMessage("已生成蓝图 C# 脚本: " + path, XWOutputPanel.MessageType.Editor);
		XWEditorInterface.Instance?.ShowToast("已生成蓝图脚本: " + path);
		XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
		if (GodotObject.IsInstanceValid(xWScriptEditor))
		{
			xWScriptEditor.OpenGeneratedBlueprintFile(path);
			XWEditorInterface.Instance?.FocusPanel("script_editor");
		}
	}

	private static bool TryWriteGeneratedCodeAtomically(string path, string code, out string error)
	{
		error = "";
		string text = NormalizeAbsolutePath(path);
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "路径无效";
			return false;
		}
		string text2 = text + ".generating-" + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			string text3 = Path.GetDirectoryName(text) ?? "";
			if (!string.IsNullOrWhiteSpace(text3))
			{
				Directory.CreateDirectory(text3);
			}
			File.WriteAllText(text2, code ?? "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			File.Move(text2, text, overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.GetBaseException().Message;
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
			return false;
		}
	}

	private static string NormalizeAbsolutePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		try
		{
			string text = path.Replace('\\', '/');
			if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
			{
				text = ProjectSettings.GlobalizePath(text);
			}
			return Path.GetFullPath(text).Replace('\\', '/');
		}
		catch
		{
			return "";
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

	private static string GetBlueprintCallbackSourcePath(string blueprintPath, string projectRoot)
	{
		string text = NormalizeAbsolutePath(blueprintPath);
		string text2 = NormalizeAbsolutePath(projectRoot);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			try
			{
				string text3 = Path.GetRelativePath(text2, text).Replace('\\', '/');
				if (!text3.StartsWith("../", StringComparison.Ordinal) && text3 != "..")
				{
					return text3;
				}
			}
			catch
			{
			}
		}
		return (blueprintPath ?? "").Replace('\\', '/').Trim();
	}

	private static bool IsPathInsideRoot(string path, string root)
	{
		string text = NormalizeAbsolutePath(path);
		string text2 = NormalizeAbsolutePath(root);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		text2 = text2.TrimEnd('/', '\\') + "/";
		return text.StartsWith(text2, StringComparison.OrdinalIgnoreCase);
	}

	private void OnRealTimeValidationComplete(Godot.Collections.Array results)
	{
		List<XWBPValidationResult> list = new List<XWBPValidationResult>();
		foreach (Variant result in results)
		{
			XWBPValidationResult xWBPValidationResult = result.As<XWBPValidationResult>();
			if (xWBPValidationResult != null)
			{
				list.Add(xWBPValidationResult);
			}
		}
		_validationPanel?.SetResults(list);
	}

	private void OnEditorActionCommitted(string actionName)
	{
		ScheduleBlueprintPersistence();
		RealTimeValidator?.DebounceTimer?.Start();
		QueueBlueprintDirectPropertyRefresh();
		XWEditorInterface.Instance?.LogEditorAction(actionName);
	}

	private void OnEditorActionUndone(string actionName)
	{
		ScheduleBlueprintPersistence();
		QueueBlueprintDirectPropertyRefresh();
		XWEditorInterface.Instance?.LogEditorUndo(actionName);
	}

	private void OnEditorActionRedone(string actionName)
	{
		ScheduleBlueprintPersistence();
		QueueBlueprintDirectPropertyRefresh();
		XWEditorInterface.Instance?.LogEditorRedo(actionName);
	}

	private void OnUndoRedoHistoryChanged(int historyId)
	{
		if (!_persistingBlueprint && historyId >= 0 && historyId == CurrentBlueprintHistoryId)
		{
			ScheduleBlueprintPersistence();
			RealTimeValidator?.DebounceTimer?.Start();
			QueueBlueprintDirectPropertyRefresh();
		}
	}

	private void QueueBlueprintDirectPropertyRefresh()
	{
		if (!_blueprintDirectPropertyRefreshQueued && IsInsideTree())
		{
			_blueprintDirectPropertyRefreshQueued = true;
			CallDeferred("FlushBlueprintDirectPropertyRefresh");
		}
	}

	private void FlushBlueprintDirectPropertyRefresh()
	{
		_blueprintDirectPropertyRefreshQueued = false;
		RefreshBlueprintDirectProperties();
	}

	private void ScheduleBlueprintPersistence()
	{
		if (GodotObject.IsInstanceValid(_persistBlueprintTimer))
		{
			_persistBlueprintTimer.Start();
		}
	}

	private void PersistBlueprintState()
	{
		PersistBlueprintSession(_activeDocument, showFailureToast: true, "蓝图自动保存");
	}

	private bool PersistBlueprintSession(BlueprintDocumentSession session, bool showFailureToast, string operationName)
	{
		if (session == null || !GodotObject.IsInstanceValid(session.Script) || !GodotObject.IsInstanceValid(session.Data))
		{
			return false;
		}
		_persistingBlueprint = true;
		try
		{
			session.Script.Serialize(session.Data);
			session.Script.EmitChanged();
		}
		catch (Exception ex)
		{
			ReportBlueprintSaveFailure(operationName, ex.Message, session.Script.ResourcePath, showFailureToast);
			return false;
		}
		finally
		{
			_persistingBlueprint = false;
		}
		string resourcePath = session.Script.ResourcePath;
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			ReportBlueprintSaveFailure(operationName, "资源没有保存路径", resourcePath, showFailureToast);
			return false;
		}
		_persistingBlueprint = true;
		Error error;
		try
		{
			error = ResourceSaver.Save(session.Script, resourcePath, ResourceSaver.SaverFlags.None);
			if (error == Error.Ok && session.HistoryId >= 0)
			{
				UndoRedoManager?.SetHistoryAsSaved(session.HistoryId);
			}
		}
		catch (Exception ex2)
		{
			ReportBlueprintSaveFailure(operationName, ex2.Message, resourcePath, showFailureToast);
			return false;
		}
		finally
		{
			_persistingBlueprint = false;
		}
		if (error == Error.Ok)
		{
			return true;
		}
		ReportBlueprintSaveFailure(operationName, error.ToString(), resourcePath, showFailureToast);
		return false;
	}

	private static void ReportBlueprintSaveFailure(string operationName, string detail, string path, bool showToast)
	{
		string text = (string.IsNullOrWhiteSpace(path) ? "" : (" (" + path + ")"));
		XWEditorInterface.Instance?.AddOutputMessage(operationName + "失败: " + detail + text, XWOutputPanel.MessageType.Error);
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast(operationName + "失败: " + detail);
		}
	}

	public int SaveAllBlueprints(bool showToast = true)
	{
		int num = 0;
		int num2 = 0;
		foreach (BlueprintDocumentSession value in _documentSessions.Values)
		{
			if (value.HistoryId >= 0 && GodotObject.IsInstanceValid(UndoRedoManager) && UndoRedoManager.IsHistoryUnsaved(value.HistoryId))
			{
				if (PersistBlueprintSession(value, showFailureToast: false, "保存蓝图资源"))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast((num2 == 0) ? $"已保存全部蓝图（{num}）" : $"蓝图保存完成：成功 {num}，失败 {num2}");
		}
		if (num2 != 0)
		{
			return -num2;
		}
		return num;
	}

	public bool FlushBlueprintPersistence()
	{
		if (GodotObject.IsInstanceValid(_persistBlueprintTimer))
		{
			_persistBlueprintTimer.Stop();
		}
		return PersistBlueprintSession(_activeDocument, showFailureToast: true, "蓝图自动保存");
	}

	public void CreateBlueprintAction(string name, bool mergeMode = false)
	{
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			int currentBlueprintHistoryId = CurrentBlueprintHistoryId;
			UndoRedoManager.CreateAction(name, mergeMode, (currentBlueprintHistoryId >= 0) ? currentBlueprintHistoryId : 0);
		}
	}

	public bool HasEditScript()
	{
		return GodotObject.IsInstanceValid(BpScriptData);
	}

	private int ReserveGraphId(XWBPGraphData graph)
	{
		if (!HasEditScript() || graph == null)
		{
			return 0;
		}
		if (graph.Id > 0)
		{
			return graph.Id;
		}
		int i;
		for (i = BpScriptData.NextGraphId; BpScriptData.Graphs.ContainsKey(i); i++)
		{
		}
		graph.Id = i;
		BpScriptData.NextGraphId = i + 1;
		return i;
	}

	private int ReserveFunctionId(XWBPFunctionData function)
	{
		if (!HasEditScript() || function == null)
		{
			return 0;
		}
		if (function.Id > 0)
		{
			return function.Id;
		}
		int i;
		for (i = BpScriptData.NextFunctionId; BpScriptData.Functions.ContainsKey(i); i++)
		{
		}
		function.Id = i;
		BpScriptData.NextFunctionId = i + 1;
		return i;
	}

	private int ReserveVariableId(XWBPVariableData variable)
	{
		if (!HasEditScript() || variable == null)
		{
			return 0;
		}
		if (variable.Id > 0)
		{
			return variable.Id;
		}
		int i;
		for (i = BpScriptData.NextVariableId; BpScriptData.Variables.ContainsKey(i); i++)
		{
		}
		variable.Id = i;
		BpScriptData.NextVariableId = i + 1;
		return i;
	}

	private int ReserveSignalId(XWBPSignalData signalData)
	{
		if (!HasEditScript() || signalData == null)
		{
			return 0;
		}
		if (signalData.Id > 0)
		{
			return signalData.Id;
		}
		int i;
		for (i = BpScriptData.NextSignalId; BpScriptData.SignalDatas.ContainsKey(i); i++)
		{
		}
		signalData.Id = i;
		BpScriptData.NextSignalId = i + 1;
		return i;
	}

	public void AddGraphWithUndo(XWBPGraphData graph)
	{
		if (HasEditScript() && graph != null)
		{
			int from = ReserveGraphId(graph);
			CreateBlueprintAction("创建图表");
			UndoRedoManager.AddDoMethod(this, "DoAddGraph", Variant.From(in graph), Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoAddGraph", Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoAddGraph(XWBPGraphData graph, int id)
	{
		if (HasEditScript() && graph != null && id > 0)
		{
			BpScriptData.AddGraph(graph, id);
			InitGraph();
		}
	}

	public void UndoAddGraph(int id)
	{
		DoRemoveGraph(id);
	}

	public void RemoveGraphWithUndo(XWBPGraphData graph)
	{
		if (HasEditScript() && graph != null)
		{
			int from = graph.Id;
			CreateBlueprintAction("删除图表");
			UndoRedoManager.AddDoMethod(this, "DoRemoveGraph", Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoRemoveGraph", Variant.From(in graph), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRemoveGraph(int id)
	{
		if (HasEditScript() && id > 0)
		{
			XWBPGraphData xWBPGraphData = _graphEdit?.GraphData;
			if (xWBPGraphData != null && !(xWBPGraphData is XWBPFunctionData) && xWBPGraphData.Id == id)
			{
				_graphEdit.Init(null);
			}
			BpScriptData.RemoveGraph(id);
			InitGraph();
		}
	}

	public void UndoRemoveGraph(XWBPGraphData graph, int id)
	{
		DoAddGraph(graph, id);
	}

	public void RenameGraphWithUndo(XWBPGraphData graph, string requestedName)
	{
		if (HasEditScript() && graph != null)
		{
			string from = graph.Name;
			string from2 = BpScriptData.GenerateUniqueGraphName(requestedName, from);
			if (from == from2)
			{
				InitGraph();
				return;
			}
			CreateBlueprintAction("重命名图表");
			UndoRedoManager.AddDoMethod(this, "DoRenameGraph", Variant.From(in graph), Variant.From(in from2));
			UndoRedoManager.AddUndoMethod(this, "DoRenameGraph", Variant.From(in graph), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRenameGraph(XWBPGraphData graph, string name)
	{
		if (graph != null)
		{
			graph.RenameSelf(name);
			InitGraph();
		}
	}

	public void AddFunctionWithUndo(XWBPFunctionData function, string actionName = "创建方法")
	{
		if (HasEditScript() && function != null)
		{
			int from = ReserveFunctionId(function);
			CreateBlueprintAction(actionName);
			UndoRedoManager.AddDoMethod(this, "DoAddFunction", Variant.From(in function), Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoAddFunction", Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoAddFunction(XWBPFunctionData function, int id)
	{
		if (HasEditScript() && function != null && id > 0)
		{
			BpScriptData.AddFunction(function, id);
			InitFunction();
		}
	}

	public void UndoAddFunction(int id)
	{
		DoRemoveFunction(id);
	}

	public void DuplicateFunctionWithUndo(XWBPFunctionData function)
	{
		if (HasEditScript() && function != null)
		{
			XWBPFunctionData xWBPFunctionData = function.Duplicate();
			xWBPFunctionData.RenameSelf(BpScriptData.GenerateUniqueFunctionName(xWBPFunctionData.Name));
			AddFunctionWithUndo(xWBPFunctionData, "复制方法");
		}
	}

	public void RemoveFunctionWithUndo(XWBPFunctionData function)
	{
		if (HasEditScript() && function != null)
		{
			int from = function.Id;
			CreateBlueprintAction("删除方法");
			UndoRedoManager.AddDoMethod(this, "DoRemoveFunction", Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoRemoveFunction", Variant.From(in function), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRemoveFunction(int id)
	{
		if (HasEditScript() && id > 0)
		{
			if (_graphEdit?.GraphData is XWBPFunctionData xWBPFunctionData && xWBPFunctionData.Id == id)
			{
				_graphEdit.Init(null);
			}
			BpScriptData.RemoveFunction(id);
			InitFunction();
		}
	}

	public void UndoRemoveFunction(XWBPFunctionData function, int id)
	{
		DoAddFunction(function, id);
	}

	public void RenameFunctionWithUndo(XWBPFunctionData function, string requestedName)
	{
		if (HasEditScript() && function != null)
		{
			string from = function.Name;
			string from2 = BpScriptData.GenerateUniqueFunctionName(requestedName, from);
			if (from == from2)
			{
				InitFunction();
				return;
			}
			CreateBlueprintAction("重命名方法");
			UndoRedoManager.AddDoMethod(this, "DoRenameFunction", Variant.From(in function), Variant.From(in from2));
			UndoRedoManager.AddUndoMethod(this, "DoRenameFunction", Variant.From(in function), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRenameFunction(XWBPFunctionData function, string name)
	{
		if (function != null)
		{
			function.RenameSelf(name);
			InitFunction();
		}
	}

	public void AddVariableWithUndo(XWBPVariableData variable, string actionName = "创建属性")
	{
		if (HasEditScript() && variable != null)
		{
			int from = ReserveVariableId(variable);
			CreateBlueprintAction(actionName);
			UndoRedoManager.AddDoMethod(this, "DoAddVariable", Variant.From(in variable), Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoAddVariable", Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoAddVariable(XWBPVariableData variable, int id)
	{
		if (HasEditScript() && variable != null && id > 0)
		{
			BpScriptData.AddVariable(variable, id);
			InitVariable();
		}
	}

	public void UndoAddVariable(int id)
	{
		DoRemoveVariable(id);
	}

	public void DuplicateVariableWithUndo(XWBPVariableData variable)
	{
		if (HasEditScript() && variable != null)
		{
			XWBPVariableData xWBPVariableData = variable.Duplicate();
			xWBPVariableData.RenameSelf(BpScriptData.GenerateUniqueVariableName(xWBPVariableData.Name));
			AddVariableWithUndo(xWBPVariableData, "复制属性");
		}
	}

	public void RemoveVariableWithUndo(XWBPVariableData variable)
	{
		if (HasEditScript() && variable != null)
		{
			int from = variable.Id;
			CreateBlueprintAction("删除属性");
			UndoRedoManager.AddDoMethod(this, "DoRemoveVariable", Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoRemoveVariable", Variant.From(in variable), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRemoveVariable(int id)
	{
		if (HasEditScript() && id > 0)
		{
			BpScriptData.RemoveVariable(id);
			InitVariable();
		}
	}

	public void UndoRemoveVariable(XWBPVariableData variable, int id)
	{
		DoAddVariable(variable, id);
	}

	public void RenameVariableWithUndo(XWBPVariableData variable, string requestedName)
	{
		if (HasEditScript() && variable != null)
		{
			string from = variable.Name;
			string from2 = BpScriptData.GenerateUniqueVariableName(requestedName, from);
			if (from == from2)
			{
				InitVariable();
				return;
			}
			CreateBlueprintAction("重命名属性");
			UndoRedoManager.AddDoMethod(this, "DoRenameVariable", Variant.From(in variable), Variant.From(in from2));
			UndoRedoManager.AddUndoMethod(this, "DoRenameVariable", Variant.From(in variable), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRenameVariable(XWBPVariableData variable, string name)
	{
		if (variable != null)
		{
			variable.RenameSelf(name);
			InitVariable();
		}
	}

	public void AddSignalWithUndo(XWBPSignalData signalData, string actionName = "创建信号")
	{
		if (HasEditScript() && signalData != null)
		{
			int from = ReserveSignalId(signalData);
			CreateBlueprintAction(actionName);
			UndoRedoManager.AddDoMethod(this, "DoAddSignal", Variant.From(in signalData), Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoAddSignal", Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoAddSignal(XWBPSignalData signalData, int id)
	{
		if (HasEditScript() && signalData != null && id > 0)
		{
			BpScriptData.AddSignal(signalData, id);
			InitSignal();
		}
	}

	public void UndoAddSignal(int id)
	{
		DoRemoveSignal(id);
	}

	public void DuplicateSignalWithUndo(XWBPSignalData signalData)
	{
		if (HasEditScript() && signalData != null)
		{
			XWBPSignalData xWBPSignalData = signalData.Duplicate();
			xWBPSignalData.RenameSelf(BpScriptData.GenerateUniqueSignalName(xWBPSignalData.Name));
			AddSignalWithUndo(xWBPSignalData, "复制信号");
		}
	}

	public void RemoveSignalWithUndo(XWBPSignalData signalData)
	{
		if (HasEditScript() && signalData != null)
		{
			int from = signalData.Id;
			CreateBlueprintAction("删除信号");
			UndoRedoManager.AddDoMethod(this, "DoRemoveSignal", Variant.From(in from));
			UndoRedoManager.AddUndoMethod(this, "UndoRemoveSignal", Variant.From(in signalData), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRemoveSignal(int id)
	{
		if (HasEditScript() && id > 0)
		{
			BpScriptData.RemoveSignal(id);
			InitSignal();
		}
	}

	public void UndoRemoveSignal(XWBPSignalData signalData, int id)
	{
		DoAddSignal(signalData, id);
	}

	public void RenameSignalWithUndo(XWBPSignalData signalData, string requestedName)
	{
		if (HasEditScript() && signalData != null)
		{
			string from = signalData.Name;
			string from2 = BpScriptData.GenerateUniqueSignalName(requestedName, from);
			if (from == from2)
			{
				InitSignal();
				return;
			}
			CreateBlueprintAction("重命名信号");
			UndoRedoManager.AddDoMethod(this, "DoRenameSignal", Variant.From(in signalData), Variant.From(in from2));
			UndoRedoManager.AddUndoMethod(this, "DoRenameSignal", Variant.From(in signalData), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	public void DoRenameSignal(XWBPSignalData signalData, string name)
	{
		if (signalData != null)
		{
			signalData.RenameSelf(name);
			InitSignal();
		}
	}

	public void EditBlueprintObject(GodotObject target)
	{
		_editingBlueprintObject = (GodotObject.IsInstanceValid(target) ? target : null);
		RefreshBlueprintDirectProperties();
	}

	private void RefreshBlueprintDirectProperties()
	{
		ClearBlueprintDirectProperties();
		if (!GodotObject.IsInstanceValid(_editingBlueprintObject) || _blueprintDirectPropertyHost == null)
		{
			return;
		}
		GodotObject editingBlueprintObject = _editingBlueprintObject;
		XWBPVariableData xWBPVariableData = editingBlueprintObject as XWBPVariableData;
		if (xWBPVariableData == null)
		{
			XWBPFunctionData xWBPFunctionData = editingBlueprintObject as XWBPFunctionData;
			if (xWBPFunctionData == null)
			{
				XWBPSignalData xWBPSignalData = editingBlueprintObject as XWBPSignalData;
				if (xWBPSignalData == null)
				{
					XWBPGraphData xWBPGraphData = editingBlueprintObject as XWBPGraphData;
					if (xWBPGraphData == null)
					{
						XWBPNodeData xWBPNodeData = editingBlueprintObject as XWBPNodeData;
						if (xWBPNodeData != null)
						{
							_blueprintDirectPropertyTitle.Text = $"节点 · {xWBPNodeData.TypeId}";
							CreateReadOnlyBlueprintField("节点类型", xWBPNodeData.TypeId.ToString());
							CreateReadOnlyBlueprintField("稳定编号", xWBPNodeData.Id.ToString(CultureInfo.InvariantCulture));
							CreateBlueprintToggle("锁定节点", xWBPNodeData.Lock, delegate(bool value)
							{
								CommitBlueprintValue(xWBPNodeData, "NodeLock", xWBPNodeData.Lock, value, "切换节点锁定");
							});
							CreateBlueprintVector2Field("位置", xWBPNodeData.Position, delegate(Vector2 value)
							{
								CommitBlueprintValue(xWBPNodeData, "NodePosition", xWBPNodeData.Position, value, "修改节点位置");
							});
							CreateBlueprintVector2Field("尺寸", xWBPNodeData.Size, delegate(Vector2 value)
							{
								CommitBlueprintValue(xWBPNodeData, "NodeSize", xWBPNodeData.Size, value, "修改节点尺寸");
							});
							CreateBlueprintPortList("输入端口", xWBPNodeData, xWBPNodeData.InputPorts, 0);
							CreateBlueprintPortList("输出端口", xWBPNodeData, xWBPNodeData.OutputPorts, 1);
						}
					}
					else
					{
						_blueprintDirectPropertyTitle.Text = "图表 · " + xWBPGraphData.Name;
						CreateBlueprintTextField("名称", xWBPGraphData.Name, delegate(string value)
						{
							RenameGraphWithUndo(xWBPGraphData, value);
						});
						CreateBlueprintToggle("锁定图", xWBPGraphData.Lock, delegate(bool value)
						{
							CommitBlueprintValue(xWBPGraphData, "GraphLock", xWBPGraphData.Lock, value, "切换图表锁定");
						});
					}
				}
				else
				{
					_blueprintDirectPropertyTitle.Text = "信号 · " + xWBPSignalData.Name;
					CreateBlueprintTextField("名称", xWBPSignalData.Name, delegate(string value)
					{
						RenameSignalWithUndo(xWBPSignalData, value);
					});
					CreateBlueprintPortList("参数", xWBPSignalData, xWBPSignalData.Inputs, 0);
				}
			}
			else
			{
				_blueprintDirectPropertyTitle.Text = "函数 · " + xWBPFunctionData.Name;
				CreateBlueprintTextField("名称", xWBPFunctionData.Name, delegate(string value)
				{
					RenameFunctionWithUndo(xWBPFunctionData, value);
				});
				CreateStateMachineCallbackWorkbench(xWBPFunctionData);
				CreateBlueprintToggle("锁定图", xWBPFunctionData.Lock, delegate(bool value)
				{
					CommitBlueprintValue(xWBPFunctionData, "GraphLock", xWBPFunctionData.Lock, value, "切换函数锁定");
				});
				CreateBlueprintPortList("输入端口", xWBPFunctionData, xWBPFunctionData.Inputs, 0);
				CreateBlueprintPortList("输出端口", xWBPFunctionData, xWBPFunctionData.Outputs, 1);
			}
			return;
		}
		_blueprintDirectPropertyTitle.Text = "变量 · " + xWBPVariableData.Name;
		CreateBlueprintTextField("名称", xWBPVariableData.Name, delegate(string value)
		{
			RenameVariableWithUndo(xWBPVariableData, value);
		}).Name = "BlueprintVariableName";
		CreateBlueprintTypePicker(xWBPVariableData);
		if (xWBPVariableData.Type == Variant.Type.Object)
		{
			CreateBlueprintTextField("资源类型", xWBPVariableData.ClassName, delegate(string value)
			{
				CommitBlueprintValue(xWBPVariableData, "VariableClass", xWBPVariableData.ClassName, value, "修改变量资源类型");
			}).Name = "BlueprintVariableClass";
		}
		CreateBlueprintVariantField("默认值", xWBPVariableData.DefaultValue, delegate(Variant value)
		{
			CommitBlueprintValue(xWBPVariableData, "VariableDefault", xWBPVariableData.DefaultValue, value, "修改变量默认值");
		}).Name = "BlueprintVariableDefault";
	}

	private void ClearBlueprintDirectProperties()
	{
		foreach (XWVisualSegmentedOption blueprintVisualOption in _blueprintVisualOptions)
		{
			blueprintVisualOption.Dispose();
		}
		_blueprintVisualOptions.Clear();
		if (_blueprintDirectPropertyTitle != null)
		{
			_blueprintDirectPropertyTitle.Text = "蓝图属性";
		}
		if (_blueprintDirectPropertyHost == null)
		{
			return;
		}
		foreach (Node child in _blueprintDirectPropertyHost.GetChildren())
		{
			_blueprintDirectPropertyHost.RemoveChild(child);
			child.QueueFree();
		}
	}

	private LineEdit CreateBlueprintTextField(string label, string value, Action<string> commit)
	{
		HBoxContainer hBoxContainer = CreateBlueprintPropertyRow(label);
		LineEdit lineEdit = new LineEdit
		{
			Text = (value ?? string.Empty),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		WireBlueprintTextCommit(lineEdit, commit);
		hBoxContainer.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		return lineEdit;
	}

	private void CreateBlueprintTypePicker(XWBPVariableData variable)
	{
		HBoxContainer hBoxContainer = CreateBlueprintPropertyRow("类型");
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		OptionButton picker = new OptionButton
		{
			Name = "BlueprintVariableTypeSource",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = false
		};
		Variant.Type[] array = new Variant.Type[10]
		{
			Variant.Type.Nil,
			Variant.Type.Bool,
			Variant.Type.Int,
			Variant.Type.Float,
			Variant.Type.String,
			Variant.Type.Vector2,
			Variant.Type.Color,
			Variant.Type.Object,
			Variant.Type.Array,
			Variant.Type.Dictionary
		};
		for (int i = 0; i < array.Length; i++)
		{
			picker.AddItem(HumanizeVariantType(array[i]), (int)array[i]);
			if (array[i] == variable.Type)
			{
				picker.Select(i);
			}
		}
		picker.ItemSelected += delegate(long index)
		{
			CommitBlueprintValue(variable, "VariableType", (long)variable.Type, (long)picker.GetItemId((int)index), "修改变量类型");
		};
		vBoxContainer.AddChild(picker, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "BlueprintVariableTypeChoices",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(picker, hFlowContainer, (int index) => LoadBlueprintVariantTypeIcon((Variant.Type)picker.GetItemId(index)));
		xWVisualSegmentedOption.Rebuild();
		_blueprintVisualOptions.Add(xWVisualSegmentedOption);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateBlueprintToggle(string label, bool value, Action<bool> commit)
	{
		CheckButton checkButton = new CheckButton
		{
			Text = label,
			ButtonPressed = value
		};
		checkButton.Toggled += delegate(bool next)
		{
			commit?.Invoke(next);
		};
		_blueprintDirectPropertyHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateStateMachineCallbackWorkbench(XWBPFunctionData function)
	{
		if (!GodotObject.IsInstanceValid(function) || !GodotObject.IsInstanceValid(_blueprintDirectPropertyHost))
		{
			return;
		}
		PanelContainer panelContainer = new PanelContainer
		{
			Name = "BlueprintStateMachineCallbackWorkbench",
			ThemeTypeVariation = "Card",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 10);
		marginContainer.AddThemeConstantOverride("margin_top", 10);
		marginContainer.AddThemeConstantOverride("margin_right", 10);
		marginContainer.AddThemeConstantOverride("margin_bottom", 10);
		panelContainer.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		TextureRect node = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GraphEdit.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(26f, 26f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "状态机动作拼图",
			ThemeTypeVariation = "HeaderSmall",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		CheckButton checkButton = new CheckButton
		{
			Name = "BlueprintStateMachineCallbackEnabled",
			Text = (function.StateMachineCallbackEnabled ? "已启用" : "未启用"),
			ButtonPressed = function.StateMachineCallbackEnabled,
			TooltipText = "启用后，生成的 C# 会把这个蓝图函数注册为状态机回调"
		};
		checkButton.Toggled += delegate(bool value)
		{
			CommitBlueprintValue(function, "StateMachineCallbackEnabled", function.StateMachineCallbackEnabled, value, value ? "启用状态机蓝图动作" : "停用状态机蓝图动作");
		};
		hBoxContainer.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "选择执行阶段后，函数会生成对应的 C# 回调桥。上下文由桥接层提供，不需要手工添加 context 端口。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("91a6be")
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "执行阶段",
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "BlueprintStateMachineCallbackPhaseCards",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		StateMachineCallbackPhase[] array = new StateMachineCallbackPhase[5]
		{
			StateMachineCallbackPhase.Enter,
			StateMachineCallbackPhase.Exit,
			StateMachineCallbackPhase.Process,
			StateMachineCallbackPhase.PhysicsProcess,
			StateMachineCallbackPhase.Guard
		};
		for (int num = 0; num < array.Length; num++)
		{
			StateMachineCallbackPhase phase = array[num];
			string title = XWBPCodeGenerator.FormatStateMachineCallbackPhase(phase);
			Button button = new Button
			{
				Name = "BlueprintStateMachineCallbackPhase" + phase,
				Text = title + "\n" + GetStateMachineCallbackSignatureSummary(phase),
				TooltipText = GetStateMachineCallbackPhaseTooltip(phase),
				Icon = LoadStateMachineCallbackPhaseIcon(phase),
				ExpandIcon = false,
				Alignment = HorizontalAlignment.Left,
				ToggleMode = true,
				ButtonGroup = buttonGroup,
				ButtonPressed = (function.StateMachineCallbackPhase == phase),
				CustomMinimumSize = new Vector2(154f, 64f),
				ThemeTypeVariation = "MainScreenButton"
			};
			button.Pressed += delegate
			{
				CommitBlueprintValue(function, "StateMachineCallbackPhase", (long)function.StateMachineCallbackPhase, (long)phase, "切换状态机动作阶段为" + title);
			};
			hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddChild(new Label
		{
			Text = "局部动作键",
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit
		{
			Name = "BlueprintStateMachineCallbackLocalKey",
			Text = (function.StateMachineCallbackLocalKey ?? string.Empty),
			PlaceholderText = "例如：角色.待机.进入",
			TooltipText = "只填写局部键；Mod 加载时会自动补成 mod/<Mod ID>/<局部键>",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		WireBlueprintTextCommit(lineEdit, delegate(string value)
		{
			string text = (value ?? string.Empty).Trim();
			CommitBlueprintValue(function, "StateMachineCallbackLocalKey", function.StateMachineCallbackLocalKey ?? string.Empty, text, "修改状态机蓝图动作键");
		});
		vBoxContainer2.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		Label node2 = CreateStateMachineCallbackKeyStatus(function);
		vBoxContainer2.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		string error;
		bool flag = XWBPCodeGenerator.TryValidateStateMachineCallbackPortSignature(function, function.StateMachineCallbackPhase, out error);
		int num2 = CountBlueprintFunctionCallers(function?.Id ?? 0);
		Label node3 = new Label
		{
			Name = "BlueprintStateMachineCallbackSignatureStatus",
			Text = (flag ? ("✓ 签名已匹配：" + GetStateMachineCallbackSignatureSummary(function.StateMachineCallbackPhase)) : ("⚠ 签名不匹配：" + error + "\n当前为 " + FormatStateMachineCallbackPortCounts(function))),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = (flag ? new Color("83d68a") : new Color("ff8a72"))
		};
		vBoxContainer.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
		if (flag && !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(BpScriptData, function, function.StateMachineCallbackPhase, out var error2))
		{
			vBoxContainer.AddChild(new Label
			{
				Name = "BlueprintStateMachineCallbackSafetyStatus",
				Text = "⛔ 回调调用链不安全：" + error2,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color("ff8a72")
			}, forceReadableName: false, InternalMode.Disabled);
		}
		if (!flag && num2 > 0)
		{
			vBoxContainer.AddChild(new Label
			{
				Name = "BlueprintStateMachineCallbackCallsiteStatus",
				Text = $"⛔ 此函数已有 {num2} 个蓝图调用节点。为避免旧端口连线损坏，一键签名修复会拒绝执行；请先删除或断开这些调用节点。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color("ff8a72")
			}, forceReadableName: false, InternalMode.Disabled);
		}
		Button button2 = new Button
		{
			Name = "BlueprintStateMachineCallbackRepairButton",
			Text = (flag ? "✓ 当前端口无需修正" : ((num2 > 0) ? $"⛔ 无法自动修正：存在 {num2} 个调用节点" : ("✦ 一键修正为" + XWBPCodeGenerator.FormatStateMachineCallbackPhase(function.StateMachineCallbackPhase) + "签名"))),
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/ImportCheck.svg", null, ResourceLoader.CacheMode.Reuse),
			Disabled = flag,
			TooltipText = ((num2 > 0) ? "签名改变会使现有调用节点的端口和连线失效；请先移除所有调用点" : "在一次 Undo/Redo 动作中修正输入、返回端口以及关联的返回节点")
		};
		button2.Pressed += delegate
		{
			RepairStateMachineCallbackSignatureWithUndo(function);
		};
		vBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		_blueprintDirectPropertyHost.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private int CountBlueprintFunctionCallers(int functionId)
	{
		if (functionId <= 0 || BpScriptData == null)
		{
			return 0;
		}
		int num = 0;
		foreach (XWBPGraphData value in BpScriptData.Graphs.Values)
		{
			num += CountBlueprintFunctionCallers(value, functionId);
		}
		foreach (XWBPFunctionData value2 in BpScriptData.Functions.Values)
		{
			num += CountBlueprintFunctionCallers(value2, functionId);
		}
		return num;
	}

	private static int CountBlueprintFunctionCallers(XWBPGraphData graph, int functionId)
	{
		if (graph?.Nodes == null)
		{
			return 0;
		}
		int num = 0;
		foreach (XWBPNodeData value in graph.Nodes.Values)
		{
			if (XWBPCodeGenerator.TryGetBlueprintFunctionCallTarget(value, out var functionId2) && functionId2 == functionId)
			{
				num++;
			}
		}
		return num;
	}

	private Label CreateStateMachineCallbackKeyStatus(XWBPFunctionData function)
	{
		string text = (function?.StateMachineCallbackLocalKey ?? string.Empty).Trim();
		string fullKey;
		string error;
		bool flag = StateMachineCallbackKey.TryBuild("blueprint", text, out fullKey, out error);
		bool flag2 = flag && HasDuplicateStateMachineCallbackSlot(function, text, function.StateMachineCallbackPhase);
		string text2;
		Color modulate;
		if (!function.StateMachineCallbackEnabled)
		{
			text2 = "尚未启用；可以先完成阶段、动作键和端口配置。";
			modulate = new Color("91a6be");
		}
		else if (!flag)
		{
			text2 = "⚠ 局部键无效：不能为空，不能包含斜杠或控制字符；可使用中文，但不要填写 mod/... 完整键。";
			modulate = new Color("ff8a72");
		}
		else if (flag2)
		{
			text2 = "⚠ 当前蓝图中已有相同“局部键 + 阶段”的动作，请更换局部键或阶段。";
			modulate = new Color("ff8a72");
		}
		else
		{
			text2 = "✓ 注册后完整键：mod/<当前 Mod>/" + text;
			modulate = new Color("83d68a");
		}
		return new Label
		{
			Name = "BlueprintStateMachineCallbackKeyStatus",
			Text = text2,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = modulate
		};
	}

	private bool HasDuplicateStateMachineCallbackSlot(XWBPFunctionData current, string localKey, StateMachineCallbackPhase phase)
	{
		if (BpScriptData?.Functions == null)
		{
			return false;
		}
		foreach (XWBPFunctionData value in BpScriptData.Functions.Values)
		{
			if (GodotObject.IsInstanceValid(value) && value != current && value.StateMachineCallbackEnabled && value.StateMachineCallbackPhase == phase && string.Equals((value.StateMachineCallbackLocalKey ?? string.Empty).Trim(), localKey, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static string GetStateMachineCallbackSignatureSummary(StateMachineCallbackPhase phase)
	{
		return phase switch
		{
			StateMachineCallbackPhase.Enter => "无输入 · 无返回", 
			StateMachineCallbackPhase.Exit => "无输入 · 无返回", 
			StateMachineCallbackPhase.Process => "delta 小数 · 无返回", 
			StateMachineCallbackPhase.PhysicsProcess => "delta 小数 · 无返回", 
			StateMachineCallbackPhase.Guard => "无输入 · result 开关", 
			_ => "不支持的签名", 
		};
	}

	private static string GetStateMachineCallbackPhaseTooltip(StateMachineCallbackPhase phase)
	{
		return phase switch
		{
			StateMachineCallbackPhase.Enter => "状态进入时执行一次", 
			StateMachineCallbackPhase.Exit => "状态退出前执行一次", 
			StateMachineCallbackPhase.Process => "状态激活时每个 Process 帧执行，并接收 delta", 
			StateMachineCallbackPhase.PhysicsProcess => "状态激活时每个 PhysicsProcess 帧执行，并接收 delta", 
			StateMachineCallbackPhase.Guard => "状态切换前计算一次，返回 true 才允许转移", 
			_ => "当前阶段不受支持", 
		};
	}

	private static Texture2D LoadStateMachineCallbackPhaseIcon(StateMachineCallbackPhase phase)
	{
		return ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/" + phase switch
		{
			StateMachineCallbackPhase.Enter => "PlayStart", 
			StateMachineCallbackPhase.Exit => "Stop", 
			StateMachineCallbackPhase.Process => "MainPlay", 
			StateMachineCallbackPhase.PhysicsProcess => "PhysicsMaterial", 
			StateMachineCallbackPhase.Guard => "CheckButton", 
			_ => "GraphEdit", 
		} + ".svg", null, ResourceLoader.CacheMode.Reuse);
	}

	private static string FormatStateMachineCallbackPortCounts(XWBPFunctionData function)
	{
		return $"输入 {(function?.Inputs?.Count).GetValueOrDefault()} 个、返回 {(function?.Outputs?.Count).GetValueOrDefault()} 个";
	}

	private void CreateBlueprintVector2Field(string label, Vector2 value, Action<Vector2> commit)
	{
		CreateBlueprintTextField(label, value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture), delegate(string text)
		{
			if (TryParseVector2(text, out var value2))
			{
				commit?.Invoke(value2);
			}
		});
	}

	private void CreateBlueprintPortList(string title, GodotObject owner, List<XWBPNodePortData> ports, int direction)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = title,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Text = "+",
			TooltipText = "添加端口"
		};
		button.Pressed += delegate
		{
			CommitInsertBlueprintPort(owner, direction, ports.Count);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		_blueprintDirectPropertyHost.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		for (int num = 0; num < ports.Count; num++)
		{
			XWBPNodePortData port = ports[num];
			int captured = num;
			VBoxContainer vBoxContainer = new VBoxContainer();
			HBoxContainer hBoxContainer2 = new HBoxContainer();
			LineEdit lineEdit = new LineEdit
			{
				Name = $"BlueprintPortName_{direction}_{captured}",
				Text = port.Name,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			WireBlueprintTextCommit(lineEdit, delegate(string text)
			{
				CommitBlueprintValue(port, "PortName", port.Name, text, "重命名蓝图端口");
			});
			hBoxContainer2.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
			Button button2 = new Button
			{
				Text = "×",
				TooltipText = "移除端口"
			};
			button2.Pressed += delegate
			{
				CommitRemoveBlueprintPort(owner, direction, captured, port);
			};
			hBoxContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
			OptionButton typePicker = new OptionButton
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			XWBPNodePortData.PortType[] values = Enum.GetValues<XWBPNodePortData.PortType>();
			for (int num2 = 0; num2 < values.Length; num2++)
			{
				XWBPNodePortData.PortType portType = values[num2];
				typePicker.AddItem(portType.ToString(), (int)portType);
				if (portType == port.PortTypeValue)
				{
					typePicker.Select(typePicker.ItemCount - 1);
				}
			}
			typePicker.ItemSelected += delegate(long selected)
			{
				CommitBlueprintValue(port, "PortType", (long)port.PortTypeValue, (long)typePicker.GetItemId((int)selected), "修改蓝图端口类型");
			};
			vBoxContainer.AddChild(typePicker, forceReadableName: false, InternalMode.Disabled);
			CreateBlueprintVariantField(vBoxContainer, "默认值", port.DefaultValue, delegate(Variant value)
			{
				CommitBlueprintValue(port, "PortDefault", port.DefaultValue, value, "修改端口默认值");
			}).Name = $"BlueprintPortDefault_{direction}_{captured}";
			_blueprintDirectPropertyHost.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private LineEdit CreateBlueprintVariantField(string label, Variant value, Action<Variant> commit)
	{
		return CreateBlueprintVariantField(_blueprintDirectPropertyHost, label, value, commit);
	}

	private LineEdit CreateBlueprintVariantField(VBoxContainer host, string label, Variant value, Action<Variant> commit)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(82f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit
		{
			Text = FormatBlueprintVariant(value),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		WireBlueprintTextCommit(lineEdit, delegate(string text)
		{
			commit?.Invoke(ParseBlueprintVariant(text, value));
		});
		hBoxContainer.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		host.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return lineEdit;
	}

	private static void WireBlueprintTextCommit(LineEdit edit, Action<string> commit)
	{
		string lastCommittedText;
		bool committing;
		if (GodotObject.IsInstanceValid(edit))
		{
			lastCommittedText = edit.Text ?? string.Empty;
			committing = false;
			edit.TextSubmitted += CommitOnce;
			edit.FocusExited += delegate
			{
				CommitOnce(edit.Text);
			};
		}
		void CommitOnce(string text)
		{
			if (text == null)
			{
				text = string.Empty;
			}
			if (committing || string.Equals(lastCommittedText, text, StringComparison.Ordinal))
			{
				return;
			}
			lastCommittedText = text;
			committing = true;
			try
			{
				commit?.Invoke(text);
			}
			finally
			{
				committing = false;
			}
		}
	}

	private void CreateReadOnlyBlueprintField(string label, string value)
	{
		CreateBlueprintPropertyRow(label).AddChild(new Label
		{
			Text = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Modulate = new Color("8fa4bd")
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private HBoxContainer CreateBlueprintPropertyRow(string label)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(82f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		_blueprintDirectPropertyHost.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private void CommitBlueprintValue(GodotObject target, string property, Variant oldValue, Variant newValue, string action)
	{
		if (GodotObject.IsInstanceValid(target) && !oldValue.Equals(newValue))
		{
			CreateBlueprintAction(action);
			UndoRedoManager.AddDoMethod(this, "ApplyBlueprintValue", Variant.From(in target), Variant.From(in property), newValue);
			UndoRedoManager.AddUndoMethod(this, "ApplyBlueprintValue", Variant.From(in target), Variant.From(in property), oldValue);
			UndoRedoManager.CommitAction();
		}
	}

	public void CommitGraphNodePosition(XWBPNodeData node, Vector2 oldPosition, Vector2 newPosition)
	{
		CommitBlueprintValue(node, "NodePosition", Variant.From(in oldPosition), Variant.From(in newPosition), "移动蓝图节点");
	}

	public void CommitGraphNodeSize(XWBPNodeData node, Vector2 oldSize, Vector2 newSize)
	{
		CommitBlueprintValue(node, "NodeSize", Variant.From(in oldSize), Variant.From(in newSize), "缩放蓝图节点");
	}

	public void CommitGraphPortValue(XWBPNodePortData port, Variant oldValue, Variant newValue)
	{
		if (GodotObject.IsInstanceValid(port) && !oldValue.Equals(newValue))
		{
			CreateBlueprintAction("修改节点参数: " + port.Name, mergeMode: true);
			UndoRedoManager.AddDoMethod(this, "ApplyGraphPortValue", Variant.From(in port), newValue);
			UndoRedoManager.AddUndoMethod(this, "ApplyGraphPortValue", Variant.From(in port), oldValue);
			UndoRedoManager.CommitAction();
		}
	}

	public void ApplyGraphPortValue(XWBPNodePortData port, Variant value)
	{
		if (!GodotObject.IsInstanceValid(port))
		{
			return;
		}
		port.Value = value;
		XWUndoRedoManager undoRedoManager = UndoRedoManager;
		if (undoRedoManager == null || !undoRedoManager.IsUndoing())
		{
			XWUndoRedoManager undoRedoManager2 = UndoRedoManager;
			if (undoRedoManager2 == null || !undoRedoManager2.IsRedoing())
			{
				return;
			}
		}
		_graphEdit?.RefreshPortEditor(port);
	}

	public void ApplyBlueprintValue(GodotObject target, string property, Variant value)
	{
		if (target is XWBPVariableData xWBPVariableData)
		{
			switch (property)
			{
			case "VariableType":
				xWBPVariableData.Type = (Variant.Type)value.AsInt64();
				xWBPVariableData.VariableSet();
				InitVariable();
				QueueBlueprintDirectPropertyRefresh();
				break;
			case "VariableClass":
				xWBPVariableData.ClassName = value.AsString();
				xWBPVariableData.VariableSet();
				break;
			case "VariableDefault":
				xWBPVariableData.DefaultValue = value;
				xWBPVariableData.VariableSet();
				break;
			}
		}
		else if (target is XWBPGraphData xWBPGraphData)
		{
			if (!(property == "GraphLock"))
			{
				if (target is XWBPFunctionData xWBPFunctionData)
				{
					switch (property)
					{
					case "StateMachineCallbackEnabled":
						xWBPFunctionData.StateMachineCallbackEnabled = value.AsBool();
						QueueBlueprintDirectPropertyRefresh();
						break;
					case "StateMachineCallbackLocalKey":
						xWBPFunctionData.StateMachineCallbackLocalKey = value.AsString().Trim();
						QueueBlueprintDirectPropertyRefresh();
						break;
					case "StateMachineCallbackPhase":
						xWBPFunctionData.StateMachineCallbackPhase = (StateMachineCallbackPhase)value.AsInt64();
						QueueBlueprintDirectPropertyRefresh();
						break;
					}
				}
			}
			else
			{
				xWBPGraphData.Lock = value.AsBool();
			}
		}
		else if (target is XWBPNodeData xWBPNodeData)
		{
			switch (property)
			{
			case "NodeLock":
				xWBPNodeData.Lock = value.AsBool();
				break;
			case "NodePosition":
			{
				XWBPNodeData xWBPNodeData3 = xWBPNodeData;
				xWBPNodeData3.Position = value.AsVector2();
				_graphEdit?.RefreshNodeTransform(xWBPNodeData3.Id);
				break;
			}
			case "NodeSize":
			{
				XWBPNodeData xWBPNodeData2 = xWBPNodeData;
				xWBPNodeData2.Size = value.AsVector2();
				_graphEdit?.RefreshNodeTransform(xWBPNodeData2.Id);
				break;
			}
			}
		}
		else if (target is XWBPNodePortData xWBPNodePortData)
		{
			switch (property)
			{
			case "PortName":
				xWBPNodePortData.Name = value.AsString();
				break;
			case "PortType":
				xWBPNodePortData.PortTypeValue = (XWBPNodePortData.PortType)value.AsInt64();
				break;
			case "PortDefault":
				xWBPNodePortData.DefaultValue = value;
				xWBPNodePortData.Value = value;
				break;
			}
		}
	}

	private void RepairStateMachineCallbackSignatureWithUndo(XWBPFunctionData function)
	{
		if (!GodotObject.IsInstanceValid(function) || !GodotObject.IsInstanceValid(UndoRedoManager) || XWBPCodeGenerator.TryValidateStateMachineCallbackPortSignature(function, function.StateMachineCallbackPhase, out var _))
		{
			return;
		}
		int num = CountBlueprintFunctionCallers(function.Id);
		if (num > 0)
		{
			XWEditorInterface.Instance?.ShowToast($"无法自动修正签名：此函数仍有 {num} 个蓝图调用节点。请先删除或断开调用点，避免旧端口连线损坏。", 2);
			QueueBlueprintDirectPropertyRefresh();
			return;
		}
		XWBPFunctionData from = function.Duplicate();
		XWBPFunctionData from2 = function.Duplicate();
		if (GodotObject.IsInstanceValid(from) && GodotObject.IsInstanceValid(from2))
		{
			ConfigureStateMachineCallbackPorts(from2, function.StateMachineCallbackPhase, function.Outputs?.Count ?? 0);
			if (!XWBPCodeGenerator.TryValidateStateMachineCallbackPortSignature(from2, function.StateMachineCallbackPhase, out var error2))
			{
				XWEditorInterface.Instance?.ShowToast("状态机动作签名修正失败：" + error2, 2);
				return;
			}
			string text = XWBPCodeGenerator.FormatStateMachineCallbackPhase(function.StateMachineCallbackPhase);
			CreateBlueprintAction("修正" + text + "状态机动作签名");
			UndoRedoManager.AddDoMethod(this, "ApplyStateMachineCallbackFunctionSnapshot", Variant.From(in function), Variant.From(in from2));
			UndoRedoManager.AddUndoMethod(this, "ApplyStateMachineCallbackFunctionSnapshot", Variant.From(in function), Variant.From(in from));
			UndoRedoManager.CommitAction();
		}
	}

	private static void ConfigureStateMachineCallbackPorts(XWBPFunctionData function, StateMachineCallbackPhase phase, int previousOutputCount)
	{
		if (GodotObject.IsInstanceValid(function))
		{
			function.PreviousOutputCount = Math.Max(0, previousOutputCount);
			function.Inputs = new List<XWBPNodePortData>();
			function.Outputs = new List<XWBPNodePortData>();
			switch (phase)
			{
			case StateMachineCallbackPhase.Process:
			case StateMachineCallbackPhase.PhysicsProcess:
				function.Inputs.Add(new XWBPNodePortData("delta", XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Float, null, Variant.From<double>(0.0)));
				break;
			case StateMachineCallbackPhase.Guard:
				function.Outputs.Add(new XWBPNodePortData("result", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Bool, null, Variant.From<bool>(false)));
				break;
			}
			function.InputsSet();
			function.OutputsSet();
		}
	}

	public void ApplyStateMachineCallbackFunctionSnapshot(XWBPFunctionData target, XWBPFunctionData snapshot)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(snapshot))
		{
			return;
		}
		XWBPFunctionData xWBPFunctionData = snapshot.Duplicate();
		if (GodotObject.IsInstanceValid(xWBPFunctionData))
		{
			XWBPScriptData owner = target.Owner;
			int id = target.Id;
			target.Name = xWBPFunctionData.Name;
			target.Nodes = xWBPFunctionData.Nodes;
			target.Connections = xWBPFunctionData.Connections;
			target.NextNodeId = xWBPFunctionData.NextNodeId;
			target.Lock = xWBPFunctionData.Lock;
			target.FunctionNodeId = xWBPFunctionData.FunctionNodeId;
			target.Inputs = xWBPFunctionData.Inputs;
			target.Outputs = xWBPFunctionData.Outputs;
			target.PreviousOutputCount = xWBPFunctionData.Outputs.Count;
			target.StateMachineCallbackEnabled = xWBPFunctionData.StateMachineCallbackEnabled;
			target.StateMachineCallbackLocalKey = xWBPFunctionData.StateMachineCallbackLocalKey ?? string.Empty;
			target.StateMachineCallbackPhase = xWBPFunctionData.StateMachineCallbackPhase;
			target.Owner = owner;
			target.Id = id;
			target.RebuildConnectionIndex();
			target.PortChangeEmit();
			if (_graphEdit?.GraphData == target)
			{
				_graphEdit.Init(target);
			}
			InitFunction();
			QueueBlueprintDirectPropertyRefresh();
		}
	}

	private void CommitInsertBlueprintPort(GodotObject owner, int direction, int index)
	{
		XWBPNodePortData from = new XWBPNodePortData("新端口", (direction != 0) ? XWBPNodePortData.Direction.Output : XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Any, null, default(Variant));
		CreateBlueprintAction("添加蓝图端口");
		UndoRedoManager.AddDoMethod(this, "DoInsertBlueprintPort", Variant.From(in owner), direction, Variant.From(in from), index);
		UndoRedoManager.AddUndoMethod(this, "DoRemoveBlueprintPort", Variant.From(in owner), direction, index);
		UndoRedoManager.CommitAction();
	}

	private void CommitRemoveBlueprintPort(GodotObject owner, int direction, int index, XWBPNodePortData port)
	{
		CreateBlueprintAction("移除蓝图端口");
		UndoRedoManager.AddDoMethod(this, "DoRemoveBlueprintPort", Variant.From(in owner), direction, index);
		UndoRedoManager.AddUndoMethod(this, "DoInsertBlueprintPort", Variant.From(in owner), direction, Variant.From(in port), index);
		UndoRedoManager.CommitAction();
	}

	public void DoInsertBlueprintPort(GodotObject owner, int direction, XWBPNodePortData port, int index)
	{
		List<XWBPNodePortData> list = ResolveBlueprintPorts(owner, direction);
		if (list != null && port != null)
		{
			list.Insert(Math.Clamp(index, 0, list.Count), port);
			NotifyBlueprintPortsChanged(owner, direction);
		}
	}

	public void DoRemoveBlueprintPort(GodotObject owner, int direction, int index)
	{
		List<XWBPNodePortData> list = ResolveBlueprintPorts(owner, direction);
		if (list != null && index >= 0 && index < list.Count)
		{
			list.RemoveAt(index);
			NotifyBlueprintPortsChanged(owner, direction);
		}
	}

	private static List<XWBPNodePortData> ResolveBlueprintPorts(GodotObject owner, int direction)
	{
		if (!(owner is XWBPFunctionData xWBPFunctionData))
		{
			if (!(owner is XWBPSignalData { Inputs: var inputs }))
			{
				if (owner is XWBPNodeData xWBPNodeData)
				{
					return (direction == 0) ? xWBPNodeData.InputPorts : xWBPNodeData.OutputPorts;
				}
				return null;
			}
			return inputs;
		}
		return (direction == 0) ? xWBPFunctionData.Inputs : xWBPFunctionData.Outputs;
	}

	private void NotifyBlueprintPortsChanged(GodotObject owner, int direction)
	{
		if (!(owner is XWBPFunctionData xWBPFunctionData))
		{
			if (!(owner is XWBPSignalData xWBPSignalData))
			{
				if (owner is XWBPNodeData xWBPNodeData)
				{
					xWBPNodeData.RebuildPortMap();
					_graphEdit?.Init(null);
					_graphEdit?.Init(FindGraphContainingNode(xWBPNodeData.Id));
				}
			}
			else
			{
				xWBPSignalData.InputsSet();
			}
		}
		else if (direction == 0)
		{
			xWBPFunctionData.InputsSet();
		}
		else
		{
			xWBPFunctionData.OutputsSet();
		}
	}

	private static bool TryParseVector2(string text, out Vector2 value)
	{
		value = Vector2.Zero;
		string[] array = (text ?? string.Empty).Split(new char[4] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 2 && float.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out value.X))
		{
			return float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value.Y);
		}
		return false;
	}

	private static Variant ParseBlueprintVariant(string text, Variant fallback)
	{
		Variant.Type variantType = fallback.VariantType;
		if ((ulong)variantType <= 5uL)
		{
			switch ((int)variantType)
			{
			case 1:
				goto IL_0059;
			case 2:
				goto IL_0074;
			case 3:
				goto IL_0095;
			case 5:
				goto IL_00b7;
			case 0:
			case 4:
			{
				string from = text ?? string.Empty;
				return Variant.From(in from);
			}
			}
		}
		Variant.Type num = variantType - 20;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
				if (Color.HtmlIsValid(text))
				{
					return Variant.From<Color>(Color.FromHtml(text));
				}
				break;
			case 1:
				return Variant.From<StringName>(new StringName(text));
			case 2:
				return Variant.From<NodePath>(new NodePath(text));
			}
		}
		goto IL_012e;
		IL_012e:
		return fallback;
		IL_0059:
		if (bool.TryParse(text, out var result))
		{
			return Variant.From(in result);
		}
		goto IL_012e;
		IL_00b7:
		if (TryParseVector2(text, out var value))
		{
			return Variant.From(in value);
		}
		goto IL_012e;
		IL_0074:
		if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
		{
			return Variant.From(in result2);
		}
		goto IL_012e;
		IL_0095:
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
		{
			return Variant.From(in result3);
		}
		goto IL_012e;
	}

	private static string FormatBlueprintVariant(Variant value)
	{
		return value.VariantType switch
		{
			Variant.Type.Vector2 => value.AsVector2().X.ToString(CultureInfo.InvariantCulture) + ", " + value.AsVector2().Y.ToString(CultureInfo.InvariantCulture), 
			Variant.Type.Color => value.AsColor().ToHtml(), 
			Variant.Type.Nil => string.Empty, 
			_ => value.ToString(), 
		};
	}

	private static string HumanizeVariantType(Variant.Type type)
	{
		Variant.Type num = type;
		if ((ulong)num <= 5uL)
		{
			switch ((int)num)
			{
			case 0:
				return "任意";
			case 1:
				return "开关";
			case 2:
				return "整数";
			case 3:
				return "小数";
			case 4:
				return "文本";
			case 5:
				return "二维坐标";
			}
		}
		if (type != Variant.Type.Color)
		{
			Variant.Type num2 = type - 24;
			if ((ulong)num2 <= 4uL)
			{
				switch ((int)num2)
				{
				case 0:
					return "游戏资源";
				case 4:
					return "列表";
				case 3:
					return "字典";
				}
			}
			return type.ToString();
		}
		return "颜色";
	}

	private static Texture2D LoadBlueprintVariantTypeIcon(Variant.Type type)
	{
		if ((ulong)type <= 5uL)
		{
			switch ((int)type)
			{
			case 0:
				goto IL_0056;
			case 1:
				goto IL_005e;
			case 2:
				goto IL_0066;
			case 3:
				goto IL_006e;
			case 4:
				goto IL_0076;
			case 5:
				goto IL_007e;
			}
		}
		string text;
		if (type != Variant.Type.Color)
		{
			Variant.Type num = type - 24;
			if ((ulong)num > 4uL)
			{
				goto IL_00a6;
			}
			switch ((int)num)
			{
			case 0:
				break;
			case 4:
				goto IL_0096;
			case 3:
				goto IL_009e;
			default:
				goto IL_00a6;
			}
			text = "Object";
		}
		else
		{
			text = "Color";
		}
		goto IL_00ac;
		IL_00ac:
		string text2 = text;
		return ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/" + text2 + ".svg", null, ResourceLoader.CacheMode.Reuse);
		IL_0056:
		text = "Nil";
		goto IL_00ac;
		IL_005e:
		text = "bool";
		goto IL_00ac;
		IL_0066:
		text = "int";
		goto IL_00ac;
		IL_006e:
		text = "float";
		goto IL_00ac;
		IL_0076:
		text = "String";
		goto IL_00ac;
		IL_007e:
		text = "Vector2";
		goto IL_00ac;
		IL_00a6:
		text = "Variant";
		goto IL_00ac;
		IL_0096:
		text = "Array";
		goto IL_00ac;
		IL_009e:
		text = "Dictionary";
		goto IL_00ac;
	}

	public void OnGraphNodeSelected(XWBPGraphNode node)
	{
		if (GodotObject.IsInstanceValid(node?.NodeData))
		{
			EditBlueprintObject(node.NodeData);
		}
	}

	public void OnGraphNodeDeselected()
	{
		EditBlueprintObject(_graphEdit?.GraphData);
	}

	private void OnGraphAddPressed()
	{
		if (HasEditScript())
		{
			XWBPGraphData graph = new XWBPGraphData
			{
				Name = BpScriptData.GenerateUniqueGraphName()
			};
			AddGraphWithUndo(graph);
		}
	}

	private void OnFunctionAddPressed()
	{
		if (HasEditScript())
		{
			XWBPFunctionData xWBPFunctionData = XWBPFunctionData.Create();
			xWBPFunctionData.Name = BpScriptData.GenerateUniqueFunctionName();
			AddFunctionWithUndo(xWBPFunctionData);
		}
	}

	private void OnVariableAddPressed()
	{
		if (HasEditScript())
		{
			XWBPVariableData variable = new XWBPVariableData
			{
				Name = BpScriptData.GenerateUniqueVariableName()
			};
			AddVariableWithUndo(variable);
		}
	}

	private void OnSignalAddPressed()
	{
		if (HasEditScript())
		{
			XWBPSignalData signalData = new XWBPSignalData
			{
				Name = BpScriptData.GenerateUniqueSignalName()
			};
			AddSignalWithUndo(signalData);
		}
	}

	public XWModDebugController GetDebugController()
	{
		return _blueprintDebugController;
	}

	private void InitializeBlueprintDebugging()
	{
		_blueprintDebugController.Stop();
		_blueprintDebugWorkbench = GetNode<XWModDebugWorkbench>("%DebugWorkbench");
		_blueprintDebugWorkbench.BindController(_blueprintDebugController);
		_blueprintDebugWorkbench.StopRequested += OnBlueprintDebugStopRequested;
		_blueprintDebugController.SnapshotChanged += OnBlueprintDebugSnapshotChanged;
		_blueprintSidebar.SetTabTitle(2, "调试");
	}

	private void ShutdownBlueprintDebugging()
	{
		_blueprintDebugController.SnapshotChanged -= OnBlueprintDebugSnapshotChanged;
		_blueprintDebugController.Stop();
		if (GodotObject.IsInstanceValid(_blueprintDebugWorkbench))
		{
			_blueprintDebugWorkbench.StopRequested -= OnBlueprintDebugStopRequested;
			_blueprintDebugWorkbench.UnbindController();
		}
		lock (_blueprintDebugSnapshotLock)
		{
			_pendingBlueprintDebugSnapshot = null;
		}
		Interlocked.Exchange(ref _blueprintDebugNavigationQueued, 0);
	}

	private void BeginBlueprintDebugging()
	{
		_blueprintDebugController.ClearBreakpoints();
		_blueprintDebugController.Start();
	}

	private void EndBlueprintDebugging()
	{
		_blueprintDebugController.Stop();
	}

	private void OnBlueprintDebugStopRequested()
	{
		_blueprintDebugController.Stop();
		_safePreviewCancellation?.Cancel();
	}

	private void OnBlueprintDebugSnapshotChanged(object sender, XWModDebugSnapshot snapshot)
	{
		if (snapshot == null || snapshot.State != XWModDebugState.Paused)
		{
			return;
		}
		XWModDebugCheckpoint currentCheckpoint = snapshot.CurrentCheckpoint;
		if (currentCheckpoint != null && currentCheckpoint.Kind == XWModDebugCheckpointKind.BlueprintNode)
		{
			lock (_blueprintDebugSnapshotLock)
			{
				_pendingBlueprintDebugSnapshot = snapshot;
			}
			if (Interlocked.Exchange(ref _blueprintDebugNavigationQueued, 1) == 0)
			{
				CallDeferred("ApplyBlueprintDebugNavigation");
			}
		}
	}

	private void ApplyBlueprintDebugNavigation()
	{
		Interlocked.Exchange(ref _blueprintDebugNavigationQueued, 0);
		XWModDebugSnapshot pendingBlueprintDebugSnapshot;
		lock (_blueprintDebugSnapshotLock)
		{
			pendingBlueprintDebugSnapshot = _pendingBlueprintDebugSnapshot;
			_pendingBlueprintDebugSnapshot = null;
		}
		if (pendingBlueprintDebugSnapshot?.CurrentCheckpoint == null)
		{
			return;
		}
		string blueprintNodeId = pendingBlueprintDebugSnapshot.CurrentCheckpoint.BlueprintNodeId;
		if (string.IsNullOrWhiteSpace(blueprintNodeId))
		{
			return;
		}
		int num = blueprintNodeId.IndexOf(':');
		if (num > 0 && int.TryParse(blueprintNodeId.AsSpan(0, num), out var result) && int.TryParse(blueprintNodeId.AsSpan(num + 1), out var result2))
		{
			XWBPScriptData bpScriptData = BpScriptData;
			if (bpScriptData != null && bpScriptData.Functions.TryGetValue(result, out var value))
			{
				OpenGraph(value);
				goto IL_00c3;
			}
		}
		if (!int.TryParse(blueprintNodeId, out result2))
		{
			return;
		}
		goto IL_00c3;
		IL_00c3:
		_blueprintSidebar.CurrentTab = 2;
		if (num <= 0)
		{
			FocusNode(result2);
		}
		_graphEdit?.HighlightRuntimeNode(result2);
	}
}
