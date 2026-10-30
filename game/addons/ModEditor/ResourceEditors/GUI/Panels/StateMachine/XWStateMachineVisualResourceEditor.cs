using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWStateMachineVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string SurfaceScenePath = "res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn";

	private static PackedScene _surfaceScene;

	private StateMachineGraphEditorSurface _surface;

	private StateMachineGraphController _controller;

	private XWStateMachineUndoAdapter _undoAdapter;

	private XWUndoRedoManager _undoRedoManager;

	private StateMachineDefinition _editingDefinition;

	private StateMachineLayout _editingLayout;

	private string _editingResourcePath = string.Empty;

	private TabContainer _workbenchTabs;

	private XWDirectPropertySurface _extensionPropertySurface;

	private readonly StateMachineLayoutStore _layoutStore = new StateMachineLayoutStore();

	private XWGameplayResourcePickerWindow _resourcePicker;

	private int _surfaceBindingGeneration;

	private bool _editorWorkbenchVisible = true;

	private bool _historyRefreshQueued;

	private static readonly IReadOnlyCollection<string> GraphOwnedDefinitionProperties = new string[7] { "SchemaVersion", "DefinitionId", "BaseDefinition", "RootStateId", "States", "Transitions", "Aliases" };

	public StateMachineGraphEditorSurface GraphSurface => _surface;

	public XWDirectPropertySurface ExtensionPropertySurface => _extensionPropertySurface;

	public TabContainer WorkbenchTabs => _workbenchTabs;

	public XWGameplayResourcePickerWindow ActiveResourcePicker => _resourcePicker;

	public bool UsesDedicatedGraphInputHost
	{
		get
		{
			ScrollContainer nodeOrNull = GetNodeOrNull<ScrollContainer>("%MainPanel");
			if (nodeOrNull != null && nodeOrNull.HorizontalScrollMode == ScrollContainer.ScrollMode.Disabled)
			{
				return nodeOrNull.VerticalScrollMode == ScrollContainer.ScrollMode.Disabled;
			}
			return false;
		}
	}

	public override void _ExitTree()
	{
		DisconnectSurface();
		_resourcePicker?.Dismiss();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisconnectSurface();
		if (base.CanvasGrid == null)
		{
			return;
		}
		ConfigureDedicatedGraphInputHost();
		_editingDefinition = base.CurrentResource as StateMachineDefinition;
		_editingResourcePath = ((_editingDefinition == null) ? string.Empty : (string.IsNullOrWhiteSpace(base.CurrentResourcePath) ? _editingDefinition.ResourcePath : base.CurrentResourcePath));
		_editingLayout = ((_editingDefinition == null) ? new StateMachineLayout() : (LoadStateMachineLayout(_editingDefinition) ?? new StateMachineLayout()));
		_controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		_undoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager();
		_undoAdapter = new XWStateMachineUndoAdapter(_undoRedoManager);
		_undoRedoManager.HistoryChanged += OnHistoryChanged;
		_controller.BindUndoAdapter(_undoAdapter);
		if (_surfaceScene == null)
		{
			_surfaceScene = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_surface = _surfaceScene?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_surface))
		{
			GD.PushError("ModEditor state-machine graph surface could not be loaded: res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn");
			return;
		}
		base.CanvasGrid.Columns = 1;
		_surface.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_surface.SizeFlagsVertical = SizeFlags.ExpandFill;
		_surface.CustomMinimumSize = new Vector2(0f, 480f);
		BuildWorkbenchTabs();
		_surface.Bind(_controller, _undoAdapter);
		if (_editingDefinition != null)
		{
			_surface.LoadDefinition(_editingDefinition, _editingLayout);
		}
		UpdateGraphWorkbenchActive();
		_surface.DefinitionChanged += OnStateMachineDefinitionChanged;
		_surface.LayoutChanged += OnStateMachineLayoutChanged;
		_surface.ResourcePickerRequested += OnResourcePickerRequested;
		_surface.GuardEditRequested += OnGuardEditRequested;
		_surface.CallbackSourceOpenRequested += OnCallbackSourceOpenRequested;
		_surface.CreateDefinitionRequested += OnCreateDefinitionRequested;
		_surface.OpenDefinitionRequested += OnOpenDefinitionRequested;
	}

	private void ConfigureDedicatedGraphInputHost()
	{
		ScrollContainer nodeOrNull = GetNodeOrNull<ScrollContainer>("%MainPanel");
		if (nodeOrNull != null)
		{
			nodeOrNull.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
			nodeOrNull.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
		}
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		_editorWorkbenchVisible = visible;
		UpdateGraphWorkbenchActive();
		if (!visible)
		{
			_resourcePicker?.Dismiss();
		}
	}

	protected override void UndoLastChange()
	{
		_surface?.DetailsPanel?.FlushPendingNumericCommit();
		_surface?.FlushViewportState();
		XWStateMachineUndoAdapter undoAdapter = _undoAdapter;
		if (undoAdapter == null || !undoAdapter.CanUndo || _controller == null)
		{
			XWEditorInterface.Instance?.ShowToast("没有可撤销的状态机修改。");
		}
		else
		{
			_controller.Undo();
		}
	}

	protected override void RedoLastChange()
	{
		_surface?.DetailsPanel?.FlushPendingNumericCommit();
		_surface?.FlushViewportState();
		XWStateMachineUndoAdapter undoAdapter = _undoAdapter;
		if (undoAdapter == null || !undoAdapter.CanRedo || _controller == null)
		{
			XWEditorInterface.Instance?.ShowToast("没有可重做的状态机修改。");
		}
		else
		{
			_controller.Redo();
		}
	}

	protected override bool ShouldBindInlineTextSurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	private void BuildWorkbenchTabs()
	{
		_workbenchTabs = new TabContainer
		{
			Name = "StateMachineWorkbenchTabs",
			CustomMinimumSize = new Vector2(0f, 480f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		base.CanvasGrid.AddChild(_workbenchTabs, forceReadableName: false, InternalMode.Disabled);
		MarginContainer marginContainer = new MarginContainer
		{
			Name = "状态图",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		marginContainer.AddChild(_surface, forceReadableName: false, InternalMode.Disabled);
		_workbenchTabs.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			Name = "资源与CSharp扩展",
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_extensionPropertySurface = new XWDirectPropertySurface
		{
			Name = "StateMachineExtensionPropertySurface",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_extensionPropertySurface.PropertyEdited += OnExtensionPropertyEdited;
		scrollContainer.AddChild(_extensionPropertySurface, forceReadableName: false, InternalMode.Disabled);
		_workbenchTabs.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		_workbenchTabs.TabChanged += OnWorkbenchTabChanged;
		_extensionPropertySurface.BindObject(_editingDefinition, _undoRedoManager, GraphOwnedDefinitionProperties, _undoAdapter?.HistoryScope ?? (-1));
	}

	private void OnExtensionPropertyEdited(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (obj == _editingDefinition)
		{
			NotifyCurrentResourceEdited();
		}
	}

	private void OnWorkbenchTabChanged(long tab)
	{
		if (tab != 0L)
		{
			_surface?.DetailsPanel?.FlushPendingNumericCommit();
			_surface?.FlushViewportState();
		}
		UpdateGraphWorkbenchActive();
	}

	private void UpdateGraphWorkbenchActive()
	{
		bool flag = _workbenchTabs == null || _workbenchTabs.CurrentTab == 0;
		_surface?.SetWorkbenchActive(_editorWorkbenchVisible && flag);
	}

	private void OnHistoryChanged(int historyScope)
	{
		if (_undoAdapter != null && historyScope == _undoAdapter.HistoryScope && !_historyRefreshQueued)
		{
			_historyRefreshQueued = true;
			CallDeferred("RefreshAfterHistoryChange", _surfaceBindingGeneration);
		}
	}

	private void RefreshAfterHistoryChange(int bindingGeneration)
	{
		if (bindingGeneration == _surfaceBindingGeneration)
		{
			_historyRefreshQueued = false;
			if (GodotObject.IsInstanceValid(_editingDefinition))
			{
				_extensionPropertySurface?.RefreshValues();
				NotifyCurrentResourceEdited();
			}
		}
	}

	private void OnStateMachineDefinitionChanged()
	{
		NotifyCurrentResourceEdited();
	}

	protected override bool CanPersistResource(Resource resource, out string failureReason)
	{
		if (!base.CanPersistResource(resource, out failureReason))
		{
			return false;
		}
		if (!(resource is StateMachineDefinition stateMachineDefinition))
		{
			return true;
		}
		StateMachineValidationResult validation;
		if (stateMachineDefinition == _editingDefinition && _surface?.GraphController != null)
		{
			if (!_surface.GraphController.HasCompositionError)
			{
				return true;
			}
			validation = _surface.GraphController.Diagnostics;
		}
		else if (StateMachineDefinitionComposer.TryValidateInheritanceChain(stateMachineDefinition, out validation))
		{
			return true;
		}
		StateMachineDiagnostic stateMachineDiagnostic = ((validation != null && validation.Diagnostics?.Count > 0) ? validation.Diagnostics[0] : null);
		failureReason = ((stateMachineDiagnostic == null) ? "继承合成失败，请更换或清除基定义后再保存。" : $"[{stateMachineDiagnostic.Code}] {stateMachineDiagnostic.Message} 请更换或清除基定义后再保存。");
		return false;
	}

	private void OnStateMachineLayoutChanged()
	{
		SaveStateMachineLayout(_editingResourcePath, _editingLayout);
	}

	private void DisconnectSurface()
	{
		_surfaceBindingGeneration++;
		_resourcePicker?.Dismiss();
		if (_surface != null)
		{
			_surface.FlushViewportState();
			_surface.SetWorkbenchActive(active: false);
			_surface.DefinitionChanged -= OnStateMachineDefinitionChanged;
			_surface.LayoutChanged -= OnStateMachineLayoutChanged;
			_surface.ResourcePickerRequested -= OnResourcePickerRequested;
			_surface.GuardEditRequested -= OnGuardEditRequested;
			_surface.CallbackSourceOpenRequested -= OnCallbackSourceOpenRequested;
			_surface.CreateDefinitionRequested -= OnCreateDefinitionRequested;
			_surface.OpenDefinitionRequested -= OnOpenDefinitionRequested;
		}
		if (_extensionPropertySurface != null)
		{
			_extensionPropertySurface.PropertyEdited -= OnExtensionPropertyEdited;
		}
		if (_workbenchTabs != null)
		{
			_workbenchTabs.TabChanged -= OnWorkbenchTabChanged;
		}
		if (_undoRedoManager != null)
		{
			_undoRedoManager.HistoryChanged -= OnHistoryChanged;
		}
		_controller?.Dispose();
		_undoAdapter?.Release();
		_surface = null;
		_extensionPropertySurface = null;
		_workbenchTabs = null;
		_controller = null;
		_undoAdapter = null;
		_undoRedoManager = null;
		_editingDefinition = null;
		_editingLayout = null;
		_editingResourcePath = string.Empty;
		_editorWorkbenchVisible = true;
		_historyRefreshQueued = false;
	}

	protected override void OnCurrentResourceSaved()
	{
		base.OnCurrentResourceSaved();
		if (_editingDefinition != null && base.CurrentResource == _editingDefinition)
		{
			_editingResourcePath = (string.IsNullOrWhiteSpace(base.CurrentResourcePath) ? _editingDefinition.ResourcePath : base.CurrentResourcePath);
			SaveStateMachineLayout(_editingResourcePath, _editingLayout);
		}
	}

	private void OnGuardEditRequested(Resource child, Resource owner, string property, int index)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(owner))
		{
			string text = (string.IsNullOrWhiteSpace(base.CurrentResourcePath) ? owner.ResourcePath : base.CurrentResourcePath);
			XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, child.ResourcePath, text, property, index, "state_machine", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text), "", _editingDefinition, text);
			XWEditorInterface.Instance?.EditResource(child, context);
		}
	}

	private void OnCallbackSourceOpenRequested(StateMachineCallbackCatalogEntry entry, StateMachineCallbackPhase phase)
	{
		OpenCallbackSource(entry, phase);
	}

	internal static void OpenCallbackSource(StateMachineCallbackCatalogEntry entry, StateMachineCallbackPhase phase)
	{
		if (entry.SourceKind != StateMachineCallbackSourceKind.Blueprint)
		{
			XWEditorInterface.Instance?.ShowToast("该动作来自 C#，请在 C# 脚本编辑器中打开源码。");
			return;
		}
		string projectRoot = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty;
		if (!TryResolveBlueprintSourcePath(entry.SourcePath, projectRoot, out var resolvedPath, out var error))
		{
			XWEditorInterface.Instance?.ShowToast("无法打开蓝图动作：" + error, 2);
			return;
		}
		XWBPScript xWBPScript;
		try
		{
			xWBPScript = ResourceLoader.Load<XWBPScript>(resolvedPath, null, ResourceLoader.CacheMode.Reuse);
		}
		catch (Exception ex)
		{
			XWEditorInterface.Instance?.ShowToast("蓝图加载失败：" + ex.GetBaseException().Message, 2);
			return;
		}
		if (!GodotObject.IsInstanceValid(xWBPScript) || !(XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor))
		{
			XWEditorInterface.Instance?.ShowToast("蓝图编辑器尚未就绪。", 2);
			return;
		}
		xWBPEditor.Init(xWBPScript);
		if (StateMachineCallbackKey.TryParse(entry.Key, out var _, out var localKey) && xWBPEditor.BpScriptData != null)
		{
			foreach (XWBPFunctionData value in xWBPEditor.BpScriptData.Functions.Values)
			{
				if (value != null && value.StateMachineCallbackEnabled && value.StateMachineCallbackPhase == phase && string.Equals(value.StateMachineCallbackLocalKey?.Trim(), localKey, StringComparison.Ordinal))
				{
					xWBPEditor.OpenGraph(value);
					break;
				}
			}
		}
		XWEditorInterface.Instance.FocusPanel("bp_editor");
	}

	private static bool TryResolveBlueprintSourcePath(string sourcePath, string projectRoot, out string resolvedPath, out string error)
	{
		resolvedPath = string.Empty;
		error = string.Empty;
		string text = (sourcePath ?? string.Empty).Replace('\\', '/').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "动作没有记录蓝图路径。请重新生成并编译蓝图。";
			return false;
		}
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && ResourceLoader.Exists(text))
		{
			resolvedPath = text;
			return true;
		}
		try
		{
			string text2 = (Path.IsPathRooted(text) ? Path.GetFullPath(text) : Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, text)));
			if (File.Exists(text2))
			{
				resolvedPath = text2.Replace('\\', '/');
				return true;
			}
		}
		catch (Exception ex)
		{
			error = ex.GetBaseException().Message;
			return false;
		}
		error = "找不到“" + text + "”。";
		return false;
	}

	private void OnResourcePickerRequested(Resource owner, string property)
	{
		if (owner == null || string.IsNullOrWhiteSpace(property))
		{
			return;
		}
		EnsureResourcePicker();
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			return;
		}
		Resource resource = owner.Get(property).As<Resource>();
		bool flag = property == "BaseDefinition";
		int generation = _surfaceBindingGeneration;
		_resourcePicker.OpenResourceLibrary("StateMachine", "状态机资源", resource?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, (!flag) ? new string[1] { "Resource" } : new string[1] { "StateMachineDefinition" }, new string[2] { "Resources/StateMachines", "/StateMachines/" }, "res://addons/ModEditor/Icons/GraphEdit.svg", delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _surfaceBindingGeneration && IsOwnerBoundToCurrentDefinition(owner))
			{
				ApplyPickedResource(owner, property, choice?.ResourcePath);
			}
		});
	}

	private void OnCreateDefinitionRequested()
	{
		string text = "root." + Guid.NewGuid().ToString("N");
		string text2 = "state." + Guid.NewGuid().ToString("N");
		XWResourceEditorRegistry.TryOpen(new StateMachineDefinition
		{
			DefinitionId = Guid.NewGuid().ToString("N"),
			ResourceName = "新状态机",
			RootStateId = text,
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = text,
					DisplayName = "状态机入口",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = text2
				},
				new StateMachineStateDefinition
				{
					StableId = text2,
					DisplayName = "初始状态",
					Kind = StateMachineStateKind.Atomic,
					ParentId = text
				}
			}
		}, string.Empty);
	}

	private void OnOpenDefinitionRequested()
	{
		EnsureResourcePicker();
		_resourcePicker?.OpenResourceLibrary("StateMachine", "状态机资源", string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { "StateMachineDefinition" }, new string[2] { "Resources/StateMachines", "/StateMachines/" }, "res://addons/ModEditor/Icons/GraphEdit.svg", delegate(XWGameplayResourceChoice choice)
		{
			OpenDefinitionResource(choice?.ResourcePath);
		});
	}

	private void EnsureResourcePicker()
	{
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_resourcePicker))
			{
				AddChild(_resourcePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private static void OpenDefinitionResource(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			StateMachineDefinition stateMachineDefinition = LoadPickedResource<StateMachineDefinition>(path);
			if (stateMachineDefinition != null)
			{
				XWResourceEditorRegistry.TryOpen(stateMachineDefinition, path);
			}
		}
	}

	private void ApplyPickedResource(Resource owner, string property, string path)
	{
		if (owner != null && !string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(path))
		{
			Resource resource = LoadPickedResource<Resource>(path);
			if (resource != null && (!(property == "BaseDefinition") || resource is StateMachineDefinition))
			{
				_surface?.ApplyPickedResource(owner, property, resource);
			}
		}
	}

	private bool IsOwnerBoundToCurrentDefinition(Resource owner)
	{
		if (!GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(_editingDefinition))
		{
			return false;
		}
		if (owner == _editingDefinition)
		{
			return true;
		}
		if (owner is StateMachineStateDefinition stateMachineStateDefinition && _editingDefinition.States != null)
		{
			foreach (StateMachineStateDefinition state in _editingDefinition.States)
			{
				if (state == stateMachineStateDefinition || (!string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId) && string.Equals(state?.StableId, stateMachineStateDefinition.StableId, StringComparison.Ordinal)))
				{
					return true;
				}
			}
		}
		if (owner is StateMachineTransitionDefinition stateMachineTransitionDefinition && _editingDefinition.Transitions != null)
		{
			foreach (StateMachineTransitionDefinition transition in _editingDefinition.Transitions)
			{
				if (transition == stateMachineTransitionDefinition || (!string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.StableId) && string.Equals(transition?.StableId, stateMachineTransitionDefinition.StableId, StringComparison.Ordinal)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static T LoadPickedResource<T>(string path) where T : Resource
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		string text = path.Replace('\\', '/').Trim();
		string text2 = LocalizeKnownResourcePath(text);
		T val = ResourceLoader.Load<T>(text2, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(val) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			val = ResourceLoader.Load<T>(text, "", ResourceLoader.CacheMode.Ignore);
		}
		return val;
	}

	private static string LocalizeKnownResourcePath(string path)
	{
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		try
		{
			string fullPath = Path.GetFullPath(path);
			string[] array = new string[2] { "user://", "res://" };
			foreach (string text in array)
			{
				string text2 = Path.GetFullPath(ProjectSettings.GlobalizePath(text)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if (fullPath.Equals(text2, StringComparison.OrdinalIgnoreCase) || fullPath.StartsWith(text2 + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || fullPath.StartsWith(text2 + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				{
					string text3 = Path.GetRelativePath(text2, fullPath).Replace('\\', '/');
					return text + text3;
				}
			}
		}
		catch (Exception ex) when (((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? 1 : 0) != 0)
		{
		}
		return path;
	}

	private StateMachineLayout LoadStateMachineLayout(StateMachineDefinition definition)
	{
		string text = ((!string.IsNullOrWhiteSpace(base.CurrentResourcePath)) ? base.CurrentResourcePath : definition?.ResourcePath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return _layoutStore.Load(text);
		}
		return null;
	}

	private void SaveStateMachineLayout(string resourcePath, StateMachineLayout layout)
	{
		if (layout != null && !string.IsNullOrWhiteSpace(resourcePath))
		{
			Error error = _layoutStore.Save(resourcePath, layout);
			if (error != Error.Ok)
			{
				GD.PushWarning($"State-machine layout save failed: {resourcePath} ({error})");
				XWEditorInterface.Instance?.ShowToast($"状态机画布布局保存失败：{error}");
			}
		}
	}
}
