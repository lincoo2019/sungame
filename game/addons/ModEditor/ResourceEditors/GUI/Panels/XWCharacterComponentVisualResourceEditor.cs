using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCharacterComponentVisualResourceEditor : XWGenericVisualResourceEditor
{
	private sealed record DynamicPropertyDescriptor(StringName Name, Variant.Type Type, PropertyHint Hint, string HintString, PropertyUsageFlags Usage, string ClassName);

	private const int PreviewPageIndex = 0;

	private const int ComponentLibraryPageIndex = 2;

	private const int PuzzleConfigPageIndex = 3;

	private const string LayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterComponentEditorLayout.tscn";

	private const string StateSurfaceScenePath = "res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const string NodePathHintContract = "PropertyHint.NodePath";

	private static PackedScene _layoutScene;

	private static PackedScene _stateSurfaceScene;

	private static PackedScene _visualChoiceCardScene;

	private static readonly string[] BaseDefinitionProperties = new string[8] { "ComponentTypeId", "DefinitionId", "InstanceId", "WireIndex", "SchemaVersion", "InitiallyAlive", "StateMachineDefinition", "LegacyNodeNames" };

	private XWVisualPropertyBinding _binding;

	private Resource _editingResource;

	private CharacterComponentSet _editingSet;

	private CharacterComponentDefinition _selectedDefinition;

	private CharacterComponentRuntime _runtimePreview;

	private ComponentManager _runtimePreviewManager;

	private TowerDefenseCharacter _runtimePreviewOwner;

	private Node _characterPreview;

	private XWGameplayLogicPreviewSafety _previewSafety;

	private TowerDefenseCharacterConfig _previewCharacterConfig;

	private Control _layoutRoot;

	private VBoxContainer _propertyHost;

	private ItemList _effectiveList;

	private ItemList _localList;

	private Tree _inheritanceTree;

	private HFlowContainer _componentTypeGrid;

	private LineEdit _componentLibrarySearch;

	private Label _previewStatusLabel;

	private SubViewport _previewViewport;

	private Node2D _previewWorld;

	private VBoxContainer _eventSimulator;

	private LineEdit _eventNameEdit;

	private SpinBox _eventValueEdit;

	private Button _eventTypeVisualButton;

	private Button _previewCharacterVisualButton;

	private PopupPanel _eventTypePickerPopup;

	private HFlowContainer _eventTypeVisualGrid;

	private TabContainer _workbenchPages;

	private Type _selectedComponentType;

	private string _selectedEventName = "damage";

	private XWGameplayResourcePickerWindow _resourcePicker;

	private StateMachineGraphEditorSurface _stateSurface;

	private StateMachineGraphController _stateController;

	private XWStateMachineUndoAdapter _stateUndoAdapter;

	private XWShowHealthComponentPresenter _showHealthPresenter;

	private XWFireVolleyPresenter _fireVolleyPresenter;

	private XWAttackContactPresenter _attackContactPresenter;

	private XWExplosionStagePresenter _explosionStagePresenter;

	private XWProductionDropPresenter _productionDropPresenter;

	private XWSlotPositionPresenter _slotPositionPresenter;

	private readonly StateMachineLayoutStore _stateLayoutStore = new StateMachineLayoutStore();

	private readonly HashSet<StateMachineDefinition> _dirtyExternalStateMachines = new HashSet<StateMachineDefinition>();

	private string _embeddedStateMachineLayoutIdentity = string.Empty;

	private readonly List<Type> _componentTypes = new List<Type>();

	private readonly List<Type> _modComponentDefinitionTypes = new List<Type>();

	private readonly List<XWVisualSegmentedOption> _enumVisualSegments = new List<XWVisualSegmentedOption>();

	private readonly List<XWVisualOptionGallery> _enumVisualGalleries = new List<XWVisualOptionGallery>();

	private Assembly _modComponentAssembly;

	private bool _componentLibraryInitialized;

	private bool _runtimePreviewDirty;

	private bool _componentLibraryCardsDirty;

	private bool _stateMachineSurfaceDirty;

	private bool _refreshing;

	private bool _responsiveSignalsConnected;

	private int _preferredWorkbenchPage;

	private int _resourcePickerGeneration;

	public TabContainer WorkbenchPages => _workbenchPages;

	public bool IsPreviewSurfaceActive { get; private set; }

	public bool IsPreviewViewportRendering
	{
		get
		{
			if (GodotObject.IsInstanceValid(_previewViewport))
			{
				return _previewViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Disabled;
			}
			return false;
		}
	}

	public int VisualEventCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_eventTypeVisualGrid))
			{
				return 0;
			}
			return _eventTypeVisualGrid.GetChildCount();
		}
	}

	public string SelectedVisualEventName => _selectedEventName;

	public bool HasVisualCharacterSelector
	{
		get
		{
			if (GodotObject.IsInstanceValid(_previewCharacterVisualButton))
			{
				return GodotObject.IsInstanceValid(_previewCharacterVisualButton.Icon);
			}
			return false;
		}
	}

	public int RuntimePreviewBuildCount { get; private set; }

	public CharacterComponentRuntime CurrentRuntimePreview => _runtimePreview;

	public int ComponentLibraryRenderCount { get; private set; }

	public int StateMachineSurfaceBindCount { get; private set; }

	public CharacterComponentDefinition SelectedDefinition => _selectedDefinition;

	public string EmbeddedStateMachineLayoutIdentity => _embeddedStateMachineLayoutIdentity;

	public bool EmbeddedStateMachineIsReadOnly => _stateController?.ViewModel?.IsReadOnly == true;

	public bool EmbeddedStateMachineIsVisible
	{
		get
		{
			if (GodotObject.IsInstanceValid(_stateSurface))
			{
				return _stateSurface.Visible;
			}
			return false;
		}
	}

	public bool EmbeddedStateMachineUsesRuntimePreview
	{
		get
		{
			StateMachineGraphEditorSurface stateSurface = _stateSurface;
			if (stateSurface == null)
			{
				return false;
			}
			return stateSurface.SimulationPanel?.UsesBorrowedRuntime == true;
		}
	}

	public IStateMachineController EmbeddedStateMachineBoundController => _stateSurface?.SimulationPanel?.BoundRuntimeController;

	public int FullSurfaceRefreshCount { get; private set; }

	public int LeafPropertyRefreshCount { get; private set; }

	public XWShowHealthComponentPresenter ShowHealthPresenter => _showHealthPresenter;

	public XWFireVolleyPresenter FireVolleyPresenter => _fireVolleyPresenter;

	public XWAttackContactPresenter AttackContactPresenter => _attackContactPresenter;

	public XWExplosionStagePresenter ExplosionStagePresenter => _explosionStagePresenter;

	public XWProductionDropPresenter ProductionDropPresenter => _productionDropPresenter;

	public XWSlotPositionPresenter SlotPositionPresenter => _slotPositionPresenter;

	public override void _ExitTree()
	{
		DisconnectResponsiveSignals();
		DisposeDynamicSurface();
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		if (visible)
		{
			ActivateCurrentWorkbenchPage();
		}
		UpdateResponsivePreviewProcessing();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeDynamicSurface();
		if (base.CurrentResource is CharacterComponentSet || base.CurrentResource is CharacterComponentDefinition)
		{
			_editingResource = base.CurrentResource;
			_editingSet = base.CurrentResource as CharacterComponentSet;
			_selectedDefinition = base.CurrentResource as CharacterComponentDefinition;
			_binding = CreateVisualBinding();
			if (_layoutScene == null)
			{
				_layoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterComponentEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_layoutRoot = _layoutScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_layoutRoot) && base.CanvasGrid != null)
			{
				base.CanvasGrid.Columns = 1;
				_layoutRoot.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				_layoutRoot.SizeFlagsVertical = SizeFlags.ExpandFill;
				base.CanvasGrid.AddChild(_layoutRoot, forceReadableName: false, InternalMode.Disabled);
				BindLayoutNodes();
				BindLayoutActions();
				BuildComponentTypeLibrary();
				RefreshCharacterComponentEditorFromHistory();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (HasCompleteCharacterComponentVisualCoverage(resource))
		{
			return false;
		}
		return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
	}

	protected override bool ShouldBindInlineTextSurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteCharacterComponentVisualCoverage(resource))
		{
			return base.ShouldBindInlineTextSurface(resource, path, descriptor);
		}
		return false;
	}

	protected override bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteCharacterComponentVisualCoverage(resource))
		{
			return base.ShouldBindDirectPropertySurface(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteCharacterComponentVisualCoverage(Resource resource)
	{
		if (!(resource is CharacterComponentSet))
		{
			return SupportsDynamicComponentDefinition(resource);
		}
		return true;
	}

	private static bool SupportsDynamicComponentDefinition(Resource resource)
	{
		return resource is CharacterComponentDefinition;
	}

	private void BindLayoutNodes()
	{
		_propertyHost = _layoutRoot.GetNodeOrNull<VBoxContainer>("%ComponentPropertyHost");
		_effectiveList = _layoutRoot.GetNodeOrNull<ItemList>("%EffectiveComponentList");
		_localList = _layoutRoot.GetNodeOrNull<ItemList>("%LocalComponentList");
		_inheritanceTree = _layoutRoot.GetNodeOrNull<Tree>("%InheritanceTree");
		_componentTypeGrid = _layoutRoot.GetNodeOrNull<HFlowContainer>("%ComponentTypeGrid");
		_componentLibrarySearch = _layoutRoot.GetNodeOrNull<LineEdit>("%ComponentLibrarySearch");
		_previewStatusLabel = _layoutRoot.GetNodeOrNull<Label>("%PreviewStatusLabel");
		_eventSimulator = _layoutRoot.GetNodeOrNull<VBoxContainer>("%EventSimulator");
		_eventNameEdit = _layoutRoot.GetNodeOrNull<LineEdit>("%EventNameEdit");
		_eventValueEdit = _layoutRoot.GetNodeOrNull<SpinBox>("%EventValueEdit");
		_eventTypeVisualButton = _layoutRoot.GetNodeOrNull<Button>("%EventTypeVisualButton");
		_previewCharacterVisualButton = _layoutRoot.GetNodeOrNull<Button>("%PreviewCharacterVisualButton");
		_eventTypePickerPopup = _layoutRoot.GetNodeOrNull<PopupPanel>("%EventTypePickerPopup");
		_eventTypeVisualGrid = _layoutRoot.GetNodeOrNull<HFlowContainer>("%EventTypeVisualGrid");
		_workbenchPages = _layoutRoot.GetNodeOrNull<TabContainer>("%CharacterWorkbenchPages");
		if (GodotObject.IsInstanceValid(_workbenchPages) && _workbenchPages.GetTabCount() > 0)
		{
			_workbenchPages.CurrentTab = Mathf.Clamp(_preferredWorkbenchPage, 0, _workbenchPages.GetTabCount() - 1);
		}
		_previewViewport = _layoutRoot.GetNodeOrNull<SubViewport>("%ComponentPreviewViewport");
		_previewWorld = _layoutRoot.GetNodeOrNull<Node2D>("%PreviewWorld");
		ConnectResponsiveSignals();
		BuildVisualEventPicker();
	}

	private void BindLayoutActions()
	{
		if (_componentLibrarySearch != null)
		{
			_componentLibrarySearch.TextChanged += delegate
			{
				FilterComponentTypeLibrary();
			};
		}
		if (_effectiveList != null)
		{
			_effectiveList.ItemSelected += delegate(long index)
			{
				SelectDefinitionFromList(_effectiveList, index);
			};
		}
		if (_localList != null)
		{
			_localList.ItemSelected += delegate(long index)
			{
				SelectDefinitionFromList(_localList, index);
			};
		}
		ConnectButton("%AddComponentButton", AddSelectedLibraryComponent);
		ConnectButton("%OverrideComponentButton", OverrideInheritedComponent);
		ConnectButton("%RemoveComponentButton", RemoveInheritedComponent);
		ConnectButton("%RestoreComponentButton", RestoreInheritedComponent);
		ConnectButton("%SimulateEventButton", SimulateComponentEvent);
		ConnectButton("%ResetPreviewButton", RebuildCharacterRuntimePreview);
		ConnectButton("%CreateStateMachineButton", CreateStateMachineDefinition);
		ConnectButton("%ChooseStateMachineButton", ChooseStateMachineDefinition);
		ConnectButton("%ClearStateMachineButton", ClearStateMachineDefinition);
		ConnectButton("%PreviewCharacterVisualButton", OpenPreviewCharacterPicker);
		ConnectButton("%EventTypeVisualButton", OpenVisualEventPicker);
	}

	private void ConnectButton(string path, Action action)
	{
		Button nodeOrNull = _layoutRoot.GetNodeOrNull<Button>(path);
		if (nodeOrNull != null)
		{
			nodeOrNull.Pressed += action;
		}
	}

	private void ConnectResponsiveSignals()
	{
		if (!_responsiveSignalsConnected && GodotObject.IsInstanceValid(_workbenchPages))
		{
			_workbenchPages.TabChanged += OnWorkbenchPageChanged;
			_responsiveSignalsConnected = true;
			UpdateResponsivePreviewProcessing();
		}
	}

	private void DisconnectResponsiveSignals()
	{
		if (_responsiveSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(_workbenchPages))
			{
				_workbenchPages.TabChanged -= OnWorkbenchPageChanged;
			}
			_responsiveSignalsConnected = false;
			SetPreviewSurfaceActive(active: false);
		}
	}

	private void OnWorkbenchPageChanged(long page)
	{
		_preferredWorkbenchPage = (int)page;
		ActivateCurrentWorkbenchPage();
		UpdateResponsivePreviewProcessing();
	}

	private void UpdateResponsivePreviewProcessing()
	{
		bool previewSurfaceActive = IsVisibleInTree() && GodotObject.IsInstanceValid(_layoutRoot) && _layoutRoot.IsVisibleInTree() && GodotObject.IsInstanceValid(_workbenchPages) && _workbenchPages.CurrentTab == 0;
		SetPreviewSurfaceActive(previewSurfaceActive);
		if (GodotObject.IsInstanceValid(_stateSurface))
		{
			_stateSurface.SetWorkbenchActive(IsVisibleInTree() && GodotObject.IsInstanceValid(_workbenchPages) && _workbenchPages.CurrentTab == 3);
		}
	}

	private void SetPreviewSurfaceActive(bool active)
	{
		IsPreviewSurfaceActive = active;
		if (GodotObject.IsInstanceValid(_previewViewport))
		{
			_previewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(active ? 4 : 0);
		}
		if (GodotObject.IsInstanceValid(_eventSimulator))
		{
			_eventSimulator.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_characterPreview))
		{
			_characterPreview.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_runtimePreviewOwner) && _runtimePreviewOwner.IsInsideTree())
		{
			_runtimePreviewOwner.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		_fireVolleyPresenter?.SetPreviewActive(active);
		_attackContactPresenter?.SetPreviewActive(active);
		_explosionStagePresenter?.SetPreviewActive(active);
		_productionDropPresenter?.SetPreviewActive(active);
		_slotPositionPresenter?.SetPreviewActive(active);
	}

	public void SetWorkbenchPage(int page)
	{
		if (GodotObject.IsInstanceValid(_workbenchPages) && page >= 0 && page < _workbenchPages.GetTabCount())
		{
			_preferredWorkbenchPage = page;
			_workbenchPages.CurrentTab = page;
			ActivateCurrentWorkbenchPage();
			UpdateResponsivePreviewProcessing();
		}
	}

	public void InvalidateComponentTypeLibrary()
	{
		_selectedComponentType = null;
		_componentTypes.Clear();
		_componentLibraryInitialized = false;
		_componentLibraryCardsDirty = true;
		if (GodotObject.IsInstanceValid(_componentTypeGrid))
		{
			ClearChildren(_componentTypeGrid);
		}
	}

	public void SetModComponentDefinitionTypes(Assembly assembly, IReadOnlyList<Type> definitionTypes)
	{
		_modComponentAssembly = assembly;
		_modComponentDefinitionTypes.Clear();
		if (assembly != null && definitionTypes != null)
		{
			foreach (Type definitionType in definitionTypes)
			{
				if (IsComponentDefinitionLibraryType(definitionType) && definitionType.Assembly == assembly && definitionType != typeof(ModCharacterComponentDefinition))
				{
					_modComponentDefinitionTypes.Add(definitionType);
				}
			}
		}
		InvalidateComponentTypeLibrary();
		if (SynchronizeOpenModDefinitionSchemas())
		{
			NotifyCurrentResourceEdited();
			RefreshCharacterComponentEditorFromHistory();
		}
		if (IsVisibleInTree() && GodotObject.IsInstanceValid(_workbenchPages) && _workbenchPages.CurrentTab == 2)
		{
			EnsureComponentLibraryCards();
		}
	}

	public int ReleaseComponentAssemblyReferences(Assembly assembly)
	{
		if (assembly == null)
		{
			return 0;
		}
		if (_runtimePreview?.GetType().Assembly == assembly)
		{
			ResetRuntimePreview();
			_runtimePreviewDirty = true;
		}
		if (_selectedComponentType?.Assembly == assembly)
		{
			_selectedComponentType = null;
		}
		_modComponentDefinitionTypes.RemoveAll((Type type) => type?.Assembly == assembly);
		if (_modComponentAssembly == assembly)
		{
			_modComponentAssembly = null;
		}
		InvalidateComponentTypeLibrary();
		return CloseResourceTabs((Resource resource) => ResourceReferencesAssembly(resource, assembly));
	}

	private static bool ResourceReferencesAssembly(Resource resource, Assembly assembly)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		if (resource.GetType().Assembly == assembly)
		{
			return true;
		}
		if (!(resource is CharacterComponentSet characterComponentSet))
		{
			return false;
		}
		try
		{
			foreach (CharacterComponentDefinition flattenedDefinition in characterComponentSet.GetFlattenedDefinitions())
			{
				if (GodotObject.IsInstanceValid(flattenedDefinition) && flattenedDefinition.GetType().Assembly == assembly)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private void InvalidateDeferredSurfaces(bool componentLibraryCards = false)
	{
		_resourcePickerGeneration++;
		_resourcePicker?.Dismiss();
		_runtimePreviewDirty = true;
		_stateMachineSurfaceDirty = true;
		if (componentLibraryCards)
		{
			_componentLibraryCardsDirty = true;
		}
		ResetRuntimePreview();
		DisconnectStateMachineEditor();
	}

	private void ActivateCurrentWorkbenchPage()
	{
		if (IsVisibleInTree() && GodotObject.IsInstanceValid(_layoutRoot) && _layoutRoot.IsVisibleInTree() && GodotObject.IsInstanceValid(_workbenchPages))
		{
			switch (_workbenchPages.CurrentTab)
			{
			case 0:
				EnsureRuntimePreview();
				break;
			case 2:
				EnsureComponentLibraryCards();
				break;
			case 3:
				EnsureStateMachineSurface();
				break;
			case 1:
				break;
			}
		}
	}

	private void EnsureRuntimePreview()
	{
		if (_runtimePreviewDirty)
		{
			_runtimePreviewDirty = false;
			RuntimePreviewBuildCount++;
			BuildCharacterRuntimePreview();
		}
	}

	private void EnsureComponentLibraryCards()
	{
		if (_componentLibraryCardsDirty)
		{
			BuildComponentTypeLibrary();
			_componentLibraryCardsDirty = false;
			ComponentLibraryRenderCount++;
			FilterComponentTypeLibrary();
		}
	}

	private void EnsureStateMachineSurface()
	{
		if (_stateMachineSurfaceDirty)
		{
			EnsureRuntimePreview();
			_stateMachineSurfaceDirty = false;
			StateMachineSurfaceBindCount++;
			EmbedStateMachineEditor();
		}
	}

	private void RebuildCharacterRuntimePreview()
	{
		_runtimePreviewDirty = true;
		EnsureRuntimePreview();
		UpdateResponsivePreviewProcessing();
	}

	private void BuildVisualEventPicker()
	{
		if (!GodotObject.IsInstanceValid(_eventTypeVisualGrid))
		{
			return;
		}
		ClearChildren(_eventTypeVisualGrid);
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		(string, string, string, string)[] array = new(string, string, string, string)[7]
		{
			("damage", "伤害", "受击 / 扣血", "res://addons/ModEditor/Icons/StatusError.svg"),
			("attack", "攻击", "主动攻击", "res://addons/ModEditor/Icons/MainPlay.svg"),
			("heal", "治疗", "恢复生命", "res://addons/ModEditor/Icons/Add.svg"),
			("dead", "死亡", "结束生命周期", "res://addons/ModEditor/Icons/Remove.svg"),
			("alive", "复活", "恢复活动状态", "res://addons/ModEditor/Icons/ResourceCharacter.svg"),
			("enter", "进入", "进入状态", "res://addons/ModEditor/Icons/PlayScene.svg"),
			("exit", "离开", "退出状态", "res://addons/ModEditor/Icons/Close.svg")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, string) tuple = array[i];
			string key = tuple.Item1;
			string item = tuple.Item2;
			string item2 = tuple.Item3;
			string item3 = tuple.Item4;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Configure(key, item, item2, ResourceLoader.Load<Texture2D>(item3, null, ResourceLoader.CacheMode.Reuse), key == _selectedEventName);
				xWGameVisualChoiceCard.Pressed += delegate
				{
					SelectVisualEvent(key);
				};
				_eventTypeVisualGrid.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
			}
		}
		UpdateVisualEventButton();
	}

	private void OpenVisualEventPicker()
	{
		if (GodotObject.IsInstanceValid(_eventTypePickerPopup))
		{
			_eventTypePickerPopup.PopupCenteredClamped(new Vector2I(600, 310), 0.9f);
		}
	}

	public void SelectVisualEvent(string eventName)
	{
		if (string.IsNullOrWhiteSpace(eventName))
		{
			return;
		}
		_selectedEventName = eventName.StripEdges();
		if (GodotObject.IsInstanceValid(_eventTypeVisualGrid))
		{
			foreach (Node child in _eventTypeVisualGrid.GetChildren())
			{
				if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard)
				{
					xWGameVisualChoiceCard.SetSelected(xWGameVisualChoiceCard.ChoiceKey == _selectedEventName);
				}
			}
		}
		UpdateVisualEventButton();
		_eventTypePickerPopup?.Hide();
	}

	private void UpdateVisualEventButton()
	{
		if (GodotObject.IsInstanceValid(_eventTypeVisualButton))
		{
			Button eventTypeVisualButton = _eventTypeVisualButton;
			eventTypeVisualButton.Text = _selectedEventName switch
			{
				"damage" => "伤害事件", 
				"attack" => "攻击事件", 
				"heal" => "治疗事件", 
				"dead" => "死亡事件", 
				"alive" => "复活事件", 
				"enter" => "进入事件", 
				"exit" => "离开事件", 
				_ => _selectedEventName, 
			};
		}
	}

	private XWVisualPropertyBinding CreateVisualBinding()
	{
		XWVisualPropertyBinding binding = null;
		binding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager(), delegate(bool committed)
		{
			OnComponentPropertyEdited(binding, committed);
		});
		return binding;
	}

	private void OnComponentPropertyEdited(XWVisualPropertyBinding source, bool committed)
	{
		if (!committed)
		{
			LightweightRuntimeStatus();
			return;
		}
		NotifyCurrentResourceEdited();
		if (source == _binding && !source.LastCommitUsedRefreshTarget)
		{
			RefreshCharacterComponentEditorFromHistory();
		}
	}

	public void RefreshCharacterComponentLeafPropertyFromHistory()
	{
		if (_refreshing || !GodotObject.IsInstanceValid(_layoutRoot))
		{
			return;
		}
		LeafPropertyRefreshCount++;
		_binding?.RefreshBoundControls();
		foreach (XWVisualSegmentedOption enumVisualSegment in _enumVisualSegments)
		{
			enumVisualSegment.RefreshSelection();
		}
		foreach (XWVisualOptionGallery enumVisualGallery in _enumVisualGalleries)
		{
			enumVisualGallery.RefreshSelection();
		}
		_showHealthPresenter?.RefreshPreview();
		_fireVolleyPresenter?.RefreshPreview();
		_attackContactPresenter?.RefreshPreview();
		_explosionStagePresenter?.RefreshPreview();
		_productionDropPresenter?.RefreshPreview();
		_slotPositionPresenter?.RefreshPreview();
		_runtimePreviewDirty = true;
		_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("组件配置已变化；再次启动时会重建真实组件预览。");
		LightweightRuntimeStatus();
		ActivateCurrentWorkbenchPage();
	}

	public void RefreshFireVolleyPresenterFromHistory()
	{
		if (!_refreshing && GodotObject.IsInstanceValid(_layoutRoot))
		{
			LeafPropertyRefreshCount++;
			_binding?.RefreshBoundControls();
			_fireVolleyPresenter?.RefreshPreview();
			_runtimePreviewDirty = true;
			_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("发射配置已变化；再次启动时会重建真实组件运行时。");
			LightweightRuntimeStatus();
		}
	}

	public void RefreshAttackContactPresenterFromHistory()
	{
		if (!_refreshing && GodotObject.IsInstanceValid(_layoutRoot))
		{
			LeafPropertyRefreshCount++;
			_binding?.RefreshBoundControls();
			_attackContactPresenter?.RefreshPreview();
			_runtimePreviewDirty = true;
			_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("攻击接触配置已变化；再次启动时会重建真实组件运行时。");
			LightweightRuntimeStatus();
		}
	}

	public void RefreshExplosionStagePresenterFromHistory()
	{
		if (!_refreshing && GodotObject.IsInstanceValid(_layoutRoot))
		{
			LeafPropertyRefreshCount++;
			_binding?.RefreshBoundControls();
			_explosionStagePresenter?.RefreshPreview();
			_runtimePreviewDirty = true;
			_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("爆炸配置已变化；再次启动时会重建真实组件运行时。");
			LightweightRuntimeStatus();
		}
	}

	public void RefreshProductionDropPresenterFromHistory()
	{
		if (!_refreshing && GodotObject.IsInstanceValid(_layoutRoot))
		{
			LeafPropertyRefreshCount++;
			_binding?.RefreshBoundControls();
			_productionDropPresenter?.RefreshPreview();
			_runtimePreviewDirty = true;
			_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("生产配置已变化；再次启动时会重建真实组件运行时。");
			LightweightRuntimeStatus();
		}
	}

	public void RefreshSlotPositionPresenterFromHistory()
	{
		if (!_refreshing && GodotObject.IsInstanceValid(_layoutRoot))
		{
			LeafPropertyRefreshCount++;
			_binding?.RefreshBoundControls();
			_slotPositionPresenter?.RefreshPreview();
			_runtimePreviewDirty = true;
			_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("槽位配置已变化；再次启动时会重建真实组件运行时。");
			LightweightRuntimeStatus();
		}
	}

	public void SetExplosionGeometryMethod(ExplodeComponentDefinition definition, string method)
	{
		if (!GodotObject.IsInstanceValid(definition) || !XWExplosionStagePresenter.IsSupportedMethod(method))
		{
			return;
		}
		bool flag = !string.Equals(method, "Range", StringComparison.Ordinal) && (definition.explodeJalaOffset?.Count ?? 0) == 0;
		if (string.Equals(definition.explodeMethod, method, StringComparison.Ordinal) && !flag)
		{
			return;
		}
		Array<int> array = ((definition.explodeJalaOffset != null) ? definition.explodeJalaOffset.Duplicate(deep: true) : new Array<int>());
		Array<int> array2 = array.Duplicate(deep: true);
		if (flag)
		{
			array2.Add(0);
		}
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			definition.explodeMethod = method;
			if (flag)
			{
				definition.explodeJalaOffset = array2;
			}
			RefreshExplosionStagePresenterFromHistory();
			NotifyCurrentResourceEdited();
			return;
		}
		xWUndoRedoManager.CreateAction("修改爆炸几何");
		xWUndoRedoManager.AddDoProperty(definition, "explodeMethod", method);
		xWUndoRedoManager.AddUndoProperty(definition, "explodeMethod", definition.explodeMethod);
		if (flag)
		{
			xWUndoRedoManager.AddDoProperty(definition, "explodeJalaOffset", array2);
			xWUndoRedoManager.AddUndoProperty(definition, "explodeJalaOffset", array);
		}
		xWUndoRedoManager.AddDoMethod(this, "RefreshExplosionStagePresenterFromHistory");
		xWUndoRedoManager.AddUndoMethod(this, "RefreshExplosionStagePresenterFromHistory");
		xWUndoRedoManager.CommitAction();
		NotifyCurrentResourceEdited();
	}

	public void SetAttackTargetGeometry(AttackComponentDefinition definition, bool checkLine, bool checkGrid, bool useCheckAreaGridColumn)
	{
		if (GodotObject.IsInstanceValid(definition) && (definition.checkLine != checkLine || definition.checkGrid != checkGrid || definition.useCheckAreaGridColumn != useCheckAreaGridColumn))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
			{
				definition.checkLine = checkLine;
				definition.checkGrid = checkGrid;
				definition.useCheckAreaGridColumn = useCheckAreaGridColumn;
				RefreshAttackContactPresenterFromHistory();
				NotifyCurrentResourceEdited();
				return;
			}
			xWUndoRedoManager.CreateAction("修改攻击目标空间组合");
			xWUndoRedoManager.AddDoProperty(definition, "checkLine", checkLine);
			xWUndoRedoManager.AddDoProperty(definition, "checkGrid", checkGrid);
			xWUndoRedoManager.AddDoProperty(definition, "useCheckAreaGridColumn", useCheckAreaGridColumn);
			xWUndoRedoManager.AddUndoProperty(definition, "checkLine", definition.checkLine);
			xWUndoRedoManager.AddUndoProperty(definition, "checkGrid", definition.checkGrid);
			xWUndoRedoManager.AddUndoProperty(definition, "useCheckAreaGridColumn", definition.useCheckAreaGridColumn);
			xWUndoRedoManager.AddDoMethod(this, "RefreshAttackContactPresenterFromHistory");
			xWUndoRedoManager.AddUndoMethod(this, "RefreshAttackContactPresenterFromHistory");
			xWUndoRedoManager.CommitAction();
			NotifyCurrentResourceEdited();
		}
	}

	public void SetFireTargetPriority(FireComponentDefinition definition, bool farthest, bool random)
	{
		if (GodotObject.IsInstanceValid(definition) && (definition.catapultFirstFar != farthest || definition.randomChoose != random))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
			{
				definition.catapultFirstFar = farthest;
				definition.randomChoose = random;
				RefreshFireVolleyPresenterFromHistory();
				NotifyCurrentResourceEdited();
				return;
			}
			xWUndoRedoManager.CreateAction(random ? "投掷目标改为随机" : (farthest ? "投掷目标改为最远优先" : "投掷目标改为最近优先"));
			xWUndoRedoManager.AddDoProperty(definition, "catapultFirstFar", farthest);
			xWUndoRedoManager.AddDoProperty(definition, "randomChoose", random);
			xWUndoRedoManager.AddUndoProperty(definition, "catapultFirstFar", definition.catapultFirstFar);
			xWUndoRedoManager.AddUndoProperty(definition, "randomChoose", definition.randomChoose);
			xWUndoRedoManager.AddDoMethod(this, "RefreshFireVolleyPresenterFromHistory");
			xWUndoRedoManager.AddUndoMethod(this, "RefreshFireVolleyPresenterFromHistory");
			xWUndoRedoManager.CommitAction();
			NotifyCurrentResourceEdited();
		}
	}

	private StringName ResolveScalarRefreshMethod(Resource owner, StringName property)
	{
		string text = property.ToString();
		bool flag = text == "resource_name";
		if (!flag)
		{
			bool flag2 = owner is CharacterComponentDefinition;
			if (flag2)
			{
				bool flag3;
				switch (text)
				{
				case "ComponentTypeId":
				case "DefinitionId":
				case "InstanceId":
				case "WireIndex":
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag2 = flag3;
			}
			flag = flag2;
		}
		return flag ? "RefreshCharacterComponentEditorFromHistory" : "RefreshCharacterComponentLeafPropertyFromHistory";
	}

	public void RefreshCharacterComponentEditorFromHistory()
	{
		if (_refreshing || !GodotObject.IsInstanceValid(_layoutRoot))
		{
			return;
		}
		FullSurfaceRefreshCount++;
		_refreshing = true;
		try
		{
			_editingSet?.InvalidateFlattenedDefinitions();
			_binding?.Dispose();
			_binding = CreateVisualBinding();
			BindResourceHeaderFields();
			BuildInheritanceTree();
			BuildEffectiveComponentRows();
			RenderSelectedDefinitionProperties();
			InvalidateDeferredSurfaces();
			ActivateCurrentWorkbenchPage();
		}
		finally
		{
			_refreshing = false;
		}
	}

	private void BindResourceHeaderFields()
	{
		if (GodotObject.IsInstanceValid(_editingResource))
		{
			LineEdit nodeOrNull = _layoutRoot.GetNodeOrNull<LineEdit>("%ResourceNameEdit");
			CheckButton nodeOrNull2 = _layoutRoot.GetNodeOrNull<CheckButton>("%LocalToSceneCheck");
			if (nodeOrNull != null)
			{
				_binding.BindText(nodeOrNull, _editingResource, "resource_name", LightweightRuntimeStatus, this, "RefreshCharacterComponentEditorFromHistory");
			}
			if (nodeOrNull2 != null)
			{
				_binding.BindToggle(nodeOrNull2, _editingResource, "resource_local_to_scene", LightweightRuntimeStatus, this, "RefreshCharacterComponentLeafPropertyFromHistory");
			}
		}
	}

	private void RenderSelectedDefinitionProperties()
	{
		if (_propertyHost == null)
		{
			return;
		}
		_showHealthPresenter?.Dispose();
		_showHealthPresenter = null;
		_fireVolleyPresenter?.Dispose();
		_fireVolleyPresenter = null;
		_attackContactPresenter?.Dispose();
		_attackContactPresenter = null;
		_explosionStagePresenter?.Dispose();
		_explosionStagePresenter = null;
		_productionDropPresenter?.Dispose();
		_productionDropPresenter = null;
		_slotPositionPresenter?.Dispose();
		_slotPositionPresenter = null;
		DisposeEnumVisualChoices();
		ClearChildren(_propertyHost);
		if (_editingSet != null)
		{
			AddSection(_propertyHost, "集合继承", "父集合始终可在这里选择或清除；即使已经含有本地组件，也不会被组件属性页隐藏。", new Color("8ddf75"));
			CreateParentSetEditor(_propertyHost, _editingSet);
			DynamicPropertyDescriptor dynamicPropertyDescriptor = BuildDynamicPropertyDescriptors(_editingSet).FirstOrDefault((DynamicPropertyDescriptor item) => item.Name.ToString() == "BehaviorIds");
			if (dynamicPropertyDescriptor != null)
			{
				AddSection(_propertyHost, "Registry 行为", "按注册 ID 让多个角色共享同一份组件定义；解析后仍由 ComponentManager 管理运行时。", new Color("7cc8ff"));
				CreateVisualPropertyEditor(_propertyHost, _editingSet, dynamicPropertyDescriptor);
			}
		}
		Resource resource = _selectedDefinition ?? _editingResource;
		if (!GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		if (_selectedDefinition != null && _editingSet != null && !IsLocalDefinition(_selectedDefinition))
		{
			AddSection(_propertyHost, "继承组件（只读）", "先点击左侧“覆盖”生成本地副本，再编辑属性；父资源不会被子资源意外改写。", new Color("ffcb6b"));
			return;
		}
		if (resource != _editingResource)
		{
			AddSection(_propertyHost, "资源身份", "名称、场景归属与稳定组件身份都在这里直接编辑。", new Color("69b8ff"));
			CreateStringEditor(_propertyHost, resource, Descriptor("resource_name", Variant.Type.String));
			CreateBoolEditor(_propertyHost, resource, Descriptor("resource_local_to_scene", Variant.Type.Bool));
		}
		List<DynamicPropertyDescriptor> list = BuildDynamicPropertyDescriptors(resource);
		string[] baseDefinitionProperties = BaseDefinitionProperties;
		foreach (string baseProperty in baseDefinitionProperties)
		{
			DynamicPropertyDescriptor dynamicPropertyDescriptor2 = list.FirstOrDefault((DynamicPropertyDescriptor item) => item.Name.ToString() == baseProperty);
			if (dynamicPropertyDescriptor2 != null && baseProperty != "StateMachineDefinition")
			{
				CreateVisualPropertyEditor(_propertyHost, resource, dynamicPropertyDescriptor2);
			}
		}
		if (resource is CharacterComponentDefinition)
		{
			CreateDirectStateMachineDefinitionEditor(_propertyHost);
		}
		HashSet<string> hashSet = null;
		if (resource is ShowHealthComponentDefinition definition)
		{
			_showHealthPresenter = new XWShowHealthComponentPresenter(definition, _binding, this, "RefreshCharacterComponentLeafPropertyFromHistory");
			_showHealthPresenter.Mount(_propertyHost);
			hashSet = XWShowHealthComponentPresenter.SpecializedProperties;
		}
		else if (resource is FireComponentDefinition definition2)
		{
			_fireVolleyPresenter = new XWFireVolleyPresenter(definition2, _binding, this, "RefreshFireVolleyPresenterFromHistory", _previewWorld);
			_fireVolleyPresenter.Mount(_propertyHost);
			_fireVolleyPresenter.BindPreviewCharacter(_runtimePreviewOwner);
			_fireVolleyPresenter.SetPreviewActive(IsPreviewSurfaceActive);
			hashSet = XWFireVolleyPresenter.SpecializedProperties;
		}
		else if (resource is AttackComponentDefinition definition3)
		{
			_attackContactPresenter = new XWAttackContactPresenter(definition3, _binding, this, "RefreshAttackContactPresenterFromHistory", _previewWorld);
			_attackContactPresenter.Mount(_propertyHost);
			_attackContactPresenter.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as AttackComponent);
			_attackContactPresenter.SetPreviewActive(IsPreviewSurfaceActive);
			hashSet = XWAttackContactPresenter.SpecializedProperties;
		}
		else
		{
			ExplodeComponentDefinition explode = resource as ExplodeComponentDefinition;
			if (explode != null)
			{
				_explosionStagePresenter = new XWExplosionStagePresenter(explode, _binding, this, "RefreshExplosionStagePresenterFromHistory", _previewWorld, delegate(string method)
				{
					SetExplosionGeometryMethod(explode, method);
				});
				_explosionStagePresenter.Mount(_propertyHost);
				_explosionStagePresenter.BindPreviewCharacter(_runtimePreviewOwner);
				_explosionStagePresenter.SetPreviewActive(IsPreviewSurfaceActive);
				hashSet = XWExplosionStagePresenter.SpecializedProperties;
			}
			else if (resource is ProduceComponentDefinition definition4)
			{
				_productionDropPresenter = new XWProductionDropPresenter(definition4, _binding, this, "RefreshProductionDropPresenterFromHistory", _previewWorld);
				_productionDropPresenter.Mount(_propertyHost);
				_productionDropPresenter.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as ProduceComponent);
				_productionDropPresenter.SetPreviewActive(IsPreviewSurfaceActive);
				hashSet = XWProductionDropPresenter.SpecializedProperties;
			}
			else if (resource is SlotComponentDefinition definition5)
			{
				_slotPositionPresenter = new XWSlotPositionPresenter(definition5, _binding, this, "RefreshSlotPositionPresenterFromHistory", _previewWorld);
				_slotPositionPresenter.Mount(_propertyHost);
				_slotPositionPresenter.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as SlotComponent);
				_slotPositionPresenter.SetPreviewActive(IsPreviewSurfaceActive);
				hashSet = XWSlotPositionPresenter.SpecializedProperties;
			}
		}
		foreach (DynamicPropertyDescriptor item in list)
		{
			string text = item.Name.ToString();
			bool flag;
			switch (text)
			{
			case "resource_name":
			case "resource_local_to_scene":
			case "script":
			case "Configuration":
			case "ConfigurationSchema":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				flag = _editingSet != null && resource == _editingSet;
				if (flag)
				{
					bool flag2 = ((text == "ParentSet" || text == "BehaviorIds") ? true : false);
					flag = flag2;
				}
				if (!flag && !BaseDefinitionProperties.Contains(text) && (hashSet == null || !hashSet.Contains(text)))
				{
					CreateVisualPropertyEditor(_propertyHost, resource, item);
				}
			}
		}
	}

	private static DynamicPropertyDescriptor Descriptor(string name, Variant.Type type)
	{
		return new DynamicPropertyDescriptor(name, type, PropertyHint.None, string.Empty, PropertyUsageFlags.Editor, string.Empty);
	}

	private void CreateDirectStateMachineDefinitionEditor(VBoxContainer host)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, Descriptor("StateMachineDefinition", Variant.Type.Object));
		hBoxContainer.Name = "StateMachineDefinitionDirectEditor";
		StateMachineDefinition stateMachineDefinition = _selectedDefinition?.StateMachineDefinition;
		Button button = new Button();
		button.Name = "StateMachineDefinitionDirectOpen";
		button.Text = ((stateMachineDefinition == null) ? "\ud83e\udde9 尚未配置" : ("\ud83e\udde9 " + FirstNonEmpty(stateMachineDefinition.ResourceName, stateMachineDefinition.DefinitionId, "已连接状态机")));
		button.TooltipText = "进入拼图页直接编辑状态、转移与动作";
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Button button2 = button;
		button2.Pressed += delegate
		{
			SetWorkbenchPage(3);
		};
		hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		Button button3 = new Button
		{
			Name = "StateMachineDefinitionDirectCreate",
			Text = "＋",
			TooltipText = "新建内嵌状态机",
			Disabled = (stateMachineDefinition != null)
		};
		button3.Pressed += CreateStateMachineDefinition;
		hBoxContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		Button button4 = new Button
		{
			Name = "StateMachineDefinitionDirectChoose",
			Text = "选择",
			TooltipText = "选择已有 StateMachineDefinition"
		};
		button4.Pressed += ChooseStateMachineDefinition;
		hBoxContainer.AddChild(button4, forceReadableName: false, InternalMode.Disabled);
		Button button5 = new Button
		{
			Name = "StateMachineDefinitionDirectClear",
			Text = "×",
			TooltipText = "清除状态机连接",
			Disabled = (stateMachineDefinition == null)
		};
		button5.Pressed += ClearStateMachineDefinition;
		hBoxContainer.AddChild(button5, forceReadableName: false, InternalMode.Disabled);
	}

	private static List<DynamicPropertyDescriptor> BuildDynamicPropertyDescriptors(Resource resource)
	{
		List<DynamicPropertyDescriptor> list = new List<DynamicPropertyDescriptor>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (property.ContainsKey("name") && property.ContainsKey("type"))
			{
				PropertyUsageFlags propertyUsageFlags = (PropertyUsageFlags)(property.ContainsKey("usage") ? property["usage"].AsInt64() : 4);
				if ((propertyUsageFlags & PropertyUsageFlags.Editor) != PropertyUsageFlags.None)
				{
					list.Add(new DynamicPropertyDescriptor(property["name"].AsString(), (Variant.Type)property["type"].AsInt64(), (PropertyHint)(property.ContainsKey("hint") ? property["hint"].AsInt64() : 0), property.ContainsKey("hint_string") ? property["hint_string"].AsString() : string.Empty, propertyUsageFlags, property.ContainsKey("class_name") ? property["class_name"].AsString() : string.Empty));
				}
			}
		}
		return list;
	}

	private void CreateVisualPropertyEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		string text = descriptor.Name.ToString();
		if (owner is SquashComponentDefinition && text == "completeStartedSequenceOutsideComponentBattlefield")
		{
			CreateSquashBattlefieldContinuationEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Hint == PropertyHint.Enum)
		{
			CreateEnumEditor(host, owner, descriptor);
			return;
		}
		PropertyHint hint = descriptor.Hint;
		if ((hint == PropertyHint.Flags || hint == PropertyHint.Layers2DPhysics) ? true : false)
		{
			CreateFlagsEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.NodePath)
		{
			CreateNodePathEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Hint == PropertyHint.File)
		{
			CreateStringEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.Array)
		{
			CreateArrayEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.Dictionary)
		{
			CreateDictionaryEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.PackedInt32Array)
		{
			CreatePackedInt32ArrayEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.Transform2D)
		{
			CreateTransform2DEditor(host, owner, descriptor);
			return;
		}
		Variant.Type type = descriptor.Type;
		if (((ulong)(type - 5) <= 1uL) ? true : false)
		{
			CreateVector2Editor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.Color)
		{
			CreateColorEditor(host, owner, descriptor);
			return;
		}
		if (descriptor.Type == Variant.Type.Object || descriptor.Hint == PropertyHint.ResourceType)
		{
			if (LooksLikeAudio(text, descriptor))
			{
				CreateAudioPicker(host, owner, descriptor);
			}
			else if (LooksLikeTexture(text, descriptor))
			{
				CreateTexturePicker(host, owner, descriptor);
			}
			else
			{
				CreateResourceEditor(host, owner, descriptor);
			}
			return;
		}
		type = descriptor.Type;
		Variant.Type num = type - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 0:
				CreateBoolEditor(host, owner, descriptor);
				return;
			case 1:
			case 2:
				CreateNumberEditor(host, owner, descriptor);
				return;
			case 3:
				goto IL_01a3;
			}
		}
		if (type != Variant.Type.StringName)
		{
			CreateFallbackVisualEditor(host, owner, descriptor);
			return;
		}
		goto IL_01a3;
		IL_01a3:
		CreateStringEditor(host, owner, descriptor);
	}

	private void CreateBoolEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		CheckButton checkButton = new CheckButton
		{
			Text = Humanize(descriptor.Name),
			TooltipText = descriptor.Name
		};
		host.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		_binding.BindToggle(checkButton, owner, descriptor.Name, LightweightRuntimeStatus, this, ResolveScalarRefreshMethod(owner, descriptor.Name));
	}

	private void CreateSquashBattlefieldContinuationEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			Name = "SquashBattlefieldContinuationCard",
			TooltipText = "只影响已经进入 Ready / Jump 的动作；不会在角色离场后启动新的压击。"
		};
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 10);
		marginContainer.AddThemeConstantOverride("margin_top", 8);
		marginContainer.AddThemeConstantOverride("margin_right", 10);
		marginContainer.AddThemeConstantOverride("margin_bottom", 8);
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 7);
		panelContainer.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		host.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		AddSection(vBoxContainer, "离场后的已启动压击", "决定角色离开组件作用战场后，已经开始的瞄准与跳跃是否继续到落地。", new Color("f4b860"));
		OptionButton optionButton = new OptionButton
		{
			Name = "SquashContinuationModeSource",
			Visible = false
		};
		optionButton.AddItem("离场即暂停", 0);
		optionButton.SetItemIcon(0, ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Pause.svg", null, ResourceLoader.CacheMode.Reuse));
		optionButton.AddItem("完成已启动动作", 1);
		optionButton.SetItemIcon(1, ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/PlayScene.svg", null, ResourceLoader.CacheMode.Reuse));
		optionButton.Select(owner.Get(descriptor.Name).AsBool() ? 1 : 0);
		optionButton.ItemSelected += delegate(long index)
		{
			bool from = index == 1;
			_binding.SetValue(owner, descriptor.Name, Variant.From(in from), from ? "允许离场后完成倭瓜压击" : "离场时暂停倭瓜压击", this, "RefreshCharacterComponentEditorFromHistory");
		};
		vBoxContainer.AddChild(optionButton, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "SquashContinuationVisualModes",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(optionButton, hFlowContainer);
		xWVisualSegmentedOption.Rebuild();
		_enumVisualSegments.Add(xWVisualSegmentedOption);
		bool flag = owner.Get(descriptor.Name).AsBool();
		Label node = new Label
		{
			Name = "SquashContinuationPreviewStatus",
			Text = (flag ? "连续模式：已开始的 Ready / Jump 会继续到落地，适合保龄击退联动。" : "暂停模式：离开战场后立即冻结组件状态机，适合普通倭瓜。"),
			Modulate = (flag ? new Color("91e5a8") : new Color("9fb1c7")),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateNumberEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		SpinBox spinBox = new SpinBox
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			AllowGreater = true,
			AllowLesser = true
		};
		if (descriptor.Type == Variant.Type.Int)
		{
			spinBox.Step = 1.0;
		}
		ApplyRangeHint(spinBox, descriptor);
		hBoxContainer.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
		_binding.BindNumber(spinBox, owner, descriptor.Name, LightweightRuntimeStatus, this, ResolveScalarRefreshMethod(owner, descriptor.Name));
	}

	private void CreateVector2Editor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		hBoxContainer.Name = $"Vector2Editor_{descriptor.Name}";
		TextureRect node = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/2D.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(24f, 24f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TooltipText = ((descriptor.Type == Variant.Type.Vector2I) ? "二维整数坐标" : "二维坐标"),
			MouseFilter = MouseFilterEnum.Ignore
		};
		hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		bool integer = descriptor.Type == Variant.Type.Vector2I;
		Vector2 vector = (integer ? ((Vector2)owner.Get(descriptor.Name).AsVector2I()) : owner.Get(descriptor.Name).AsVector2());
		SpinBox x = CreateVector2AxisControl("X", vector.X, integer, descriptor);
		SpinBox y = CreateVector2AxisControl("Y", vector.Y, integer, descriptor);
		hBoxContainer.AddChild(CreateVector2AxisLabel("X", new Color("ff7676")), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateVector2AxisLabel("Y", new Color("73d69b")), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
		x.FocusEntered += Begin;
		y.FocusEntered += Begin;
		x.ValueChanged += Preview;
		y.ValueChanged += Preview;
		x.FocusExited += Commit;
		y.FocusExited += Commit;
		_binding.RegisterControlSynchronizer(Synchronize);
		void Begin()
		{
			_binding.BeginEdit(owner, descriptor.Name);
		}
		void Commit()
		{
			_binding.CommitEdit(owner, descriptor.Name, ReadValue(), "修改 " + Humanize(descriptor.Name), this, ResolveScalarRefreshMethod(owner, descriptor.Name));
		}
		void Preview(double _)
		{
			_binding.PreviewValue(owner, descriptor.Name, ReadValue());
		}
		Variant ReadValue()
		{
			if (!integer)
			{
				return Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value));
			}
			return Variant.From<Vector2I>(new Vector2I(Mathf.RoundToInt(x.Value), Mathf.RoundToInt(y.Value)));
		}
		void Synchronize()
		{
			if (GodotObject.IsInstanceValid(owner) && GodotObject.IsInstanceValid(x) && GodotObject.IsInstanceValid(y))
			{
				Vector2 vector2 = (integer ? ((Vector2)owner.Get(descriptor.Name).AsVector2I()) : owner.Get(descriptor.Name).AsVector2());
				x.SetValueNoSignal(vector2.X);
				y.SetValueNoSignal(vector2.Y);
			}
		}
	}

	private static Label CreateVector2AxisLabel(string axis, Color color)
	{
		Label label = new Label();
		label.Text = axis;
		label.CustomMinimumSize = new Vector2(18f, 0f);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.Modulate = color;
		label.MouseFilter = MouseFilterEnum.Ignore;
		label.AddThemeFontSizeOverride("font_size", 12);
		return label;
	}

	private static SpinBox CreateVector2AxisControl(string axis, double value, bool integer, DynamicPropertyDescriptor descriptor)
	{
		SpinBox obj = new SpinBox
		{
			Name = ((axis == "X") ? $"Vector2X_{descriptor.Name}" : $"Vector2Y_{descriptor.Name}"),
			MinValue = -1000000.0,
			MaxValue = 1000000.0,
			Step = (integer ? 1.0 : 0.05),
			Value = value,
			AllowGreater = true,
			AllowLesser = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(86f, 0f)
		};
		ApplyRangeHint(obj, descriptor);
		return obj;
	}

	private void CreateColorEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		hBoxContainer.Name = $"ColorEditor_{descriptor.Name}";
		ColorPickerButton picker = new ColorPickerButton
		{
			Name = $"ColorPicker_{descriptor.Name}",
			Color = owner.Get(descriptor.Name).AsColor(),
			EditAlpha = true,
			CustomMinimumSize = new Vector2(118f, 38f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "点击色块选择颜色；支持透明度"
		};
		picker.Pressed += delegate
		{
			_binding.BeginEdit(owner, descriptor.Name);
		};
		picker.ColorChanged += delegate(Color color)
		{
			_binding.PreviewValue(owner, descriptor.Name, Variant.From(in color));
		};
		picker.PopupClosed += delegate
		{
			_binding.CommitEdit(owner, descriptor.Name, Variant.From<Color>(picker.Color), "修改 " + Humanize(descriptor.Name), this, ResolveScalarRefreshMethod(owner, descriptor.Name));
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(picker) && GodotObject.IsInstanceValid(owner))
			{
				picker.Color = owner.Get(descriptor.Name).AsColor();
			}
		});
		hBoxContainer.AddChild(picker, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateStringEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		LineEdit lineEdit = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		_binding.BindText(lineEdit, owner, descriptor.Name, LightweightRuntimeStatus, this, ResolveScalarRefreshMethod(owner, descriptor.Name));
	}

	private void CreateEnumEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		OptionButton control = new OptionButton
		{
			Name = $"EnumSource_{descriptor.Name}",
			Visible = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string[] array = descriptor.HintString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		bool stringBacked = descriptor.Type == Variant.Type.String || descriptor.Type == Variant.Type.StringName;
		string b = (stringBacked ? owner.Get(descriptor.Name).AsString() : string.Empty);
		long num = (stringBacked ? 0 : owner.Get(descriptor.Name).AsInt64());
		for (int i = 0; i < array.Length; i++)
		{
			var (text, num2) = ParseEnumOption(array[i], i);
			control.AddItem(text, (int)num2);
			if (stringBacked ? string.Equals(text, b, StringComparison.Ordinal) : (num2 == num))
			{
				control.Select(i);
			}
		}
		_binding.RegisterControlSynchronizer(SynchronizeSelection);
		control.ItemSelected += delegate(long index)
		{
			string from = control.GetItemText((int)index);
			Variant value = ((descriptor.Type == Variant.Type.String) ? Variant.From(in from) : ((descriptor.Type == Variant.Type.StringName) ? Variant.From<StringName>(new StringName(from)) : Variant.From<long>((long)control.GetItemId((int)index))));
			_binding.SetValue(owner, descriptor.Name, value, "修改 " + Humanize(descriptor.Name), this, ResolveScalarRefreshMethod(owner, descriptor.Name));
		};
		hBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = $"EnumVisual_{descriptor.Name}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		hBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		if (control.ItemCount <= 6)
		{
			XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(control, hFlowContainer);
			xWVisualSegmentedOption.Rebuild();
			_enumVisualSegments.Add(xWVisualSegmentedOption);
		}
		else
		{
			XWVisualOptionGallery xWVisualOptionGallery = new XWVisualOptionGallery(control, hFlowContainer, $"EnumGallery_{descriptor.Name}");
			xWVisualOptionGallery.Rebuild();
			_enumVisualGalleries.Add(xWVisualOptionGallery);
		}
		void SynchronizeSelection()
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(owner))
			{
				string b2 = (stringBacked ? owner.Get(descriptor.Name).AsString() : string.Empty);
				long num3 = (stringBacked ? 0 : owner.Get(descriptor.Name).AsInt64());
				for (int j = 0; j < control.ItemCount; j++)
				{
					if (stringBacked ? string.Equals(control.GetItemText(j), b2, StringComparison.Ordinal) : (control.GetItemId(j) == num3))
					{
						control.Select(j);
						break;
					}
				}
			}
		}
	}

	private void CreateFlagsEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		host.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		AddSection(vBoxContainer, Humanize(descriptor.Name), "点击拼图开关组合能力位。", new Color("d58cff"));
		GridContainer gridContainer = new GridContainer
		{
			Columns = 3
		};
		vBoxContainer.AddChild(gridContainer, forceReadableName: false, InternalMode.Disabled);
		string[] array = descriptor.HintString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		long num = owner.Get(descriptor.Name).AsInt64();
		for (int i = 0; i < array.Length; i++)
		{
			(string, long) tuple = ParseFlagOption(array[i], i);
			string label = tuple.Item1;
			long bit = tuple.Item2;
			CheckButton toggle = new CheckButton
			{
				Text = label,
				ButtonPressed = ((num & bit) != 0)
			};
			toggle.Toggled += delegate(bool enabled)
			{
				long num2 = owner.Get(descriptor.Name).AsInt64();
				num2 = (enabled ? (num2 | bit) : (num2 & ~bit));
				_binding.SetValue(owner, descriptor.Name, Variant.From(in num2), "切换 " + label, this, ResolveScalarRefreshMethod(owner, descriptor.Name));
			};
			_binding.RegisterControlSynchronizer(delegate
			{
				if (GodotObject.IsInstanceValid(toggle) && GodotObject.IsInstanceValid(owner))
				{
					toggle.SetPressedNoSignal((owner.Get(descriptor.Name).AsInt64() & bit) != 0);
				}
			});
			gridContainer.AddChild(toggle, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void CreateNodePathEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		LineEdit control = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Text = owner.Get(descriptor.Name).AsNodePath().ToString()
		};
		hBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		control.FocusEntered += delegate
		{
			_binding.BeginEdit(owner, descriptor.Name);
		};
		control.TextChanged += delegate(string value)
		{
			_binding.PreviewValue(owner, descriptor.Name, Variant.From<NodePath>(new NodePath(value)));
		};
		control.FocusExited += delegate
		{
			_binding.CommitEdit(owner, descriptor.Name, Variant.From<NodePath>(new NodePath(control.Text)), "修改 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
		};
	}

	private void CreateResourceEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		Resource resource = owner.Get(descriptor.Name).As<Resource>();
		Button button = new Button
		{
			Text = ((resource == null) ? "选择资源…" : DisplayResource(resource)),
			Icon = XWClassRegistry.Instance.GetClassIcon(string.IsNullOrWhiteSpace(descriptor.ClassName) ? "Resource" : descriptor.ClassName),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		button.Pressed += delegate
		{
			OpenResourcePicker(owner, descriptor);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Text = "×",
			TooltipText = "清除资源"
		};
		button2.Pressed += delegate
		{
			_binding.SetValue(owner, descriptor.Name, default(Variant), "清除 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
		};
		hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateParentSetEditor(VBoxContainer host, CharacterComponentSet owner)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, Descriptor("ParentSet", Variant.Type.Object));
		CharacterComponentSet characterComponentSet = owner?.ParentSet;
		Button button = new Button
		{
			Name = "ParentSetChooseButton",
			Text = ((characterComponentSet == null) ? "\ud83c\udf31 选择父组件集…" : ("\ud83c\udf33 " + DisplayResource(characterComponentSet))),
			TooltipText = (characterComponentSet?.ResourcePath ?? "从当前 Mod 的组件集图鉴中选择"),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		button.Pressed += OpenParentSetPicker;
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Name = "ParentSetClearButton",
			Text = "×",
			TooltipText = "清除父组件集",
			Disabled = (characterComponentSet == null)
		};
		button2.Pressed += delegate
		{
			TryAssignParentSet(null);
		};
		hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
	}

	private void OpenParentSetPicker()
	{
		if (_editingSet == null)
		{
			return;
		}
		EnsureResourcePicker();
		if (_resourcePicker != null)
		{
			_resourcePicker.OpenResourceLibrary("CharacterComponents", "父组件集", _editingSet.ParentSet?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { "CharacterComponentSet" }, new string[1] { "Resources/CharacterComponents" }, "res://addons/ModEditor/Icons/ResourceCharacter.svg", delegate(XWGameplayResourceChoice choice)
			{
				CharacterComponentSet candidate = ResourceLoader.Load<CharacterComponentSet>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				TryAssignParentSet(candidate);
			});
		}
	}

	public bool TryAssignParentSet(CharacterComponentSet candidate)
	{
		if (_editingSet == null)
		{
			return false;
		}
		if (_editingSet.ParentSet == candidate)
		{
			return true;
		}
		if (WouldCreateParentSetCycle(_editingSet, candidate))
		{
			SetPreviewStatus("已拒绝父集合：该选择会创建组件集继承环。", warning: true);
			return false;
		}
		Variant value = ((candidate == null) ? default(Variant) : Variant.From(in candidate));
		_binding.SetValue(_editingSet, "ParentSet", value, (candidate == null) ? "清除父组件集" : "选择父组件集", this, "RefreshCharacterComponentEditorFromHistory");
		return true;
	}

	private static bool WouldCreateParentSetCycle(CharacterComponentSet owner, CharacterComponentSet candidate)
	{
		HashSet<CharacterComponentSet> hashSet = new HashSet<CharacterComponentSet>();
		for (CharacterComponentSet characterComponentSet = candidate; characterComponentSet != null; characterComponentSet = characterComponentSet.ParentSet)
		{
			if (SameComponentSetIdentity(characterComponentSet, owner) || !hashSet.Add(characterComponentSet))
			{
				return true;
			}
		}
		return false;
	}

	private static bool SameComponentSetIdentity(CharacterComponentSet left, CharacterComponentSet right)
	{
		if (left == right)
		{
			return true;
		}
		string text = NormalizeResourceIdentityPath(left?.ResourcePath);
		string text2 = NormalizeResourceIdentityPath(right?.ResourcePath);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string NormalizeResourceIdentityPath(string path)
	{
		string text = (path ?? string.Empty).Replace('\\', '/').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			text = ProjectSettings.GlobalizePath(text);
		}
		try
		{
			return Path.GetFullPath(text).Replace('\\', '/').TrimEnd('/');
		}
		catch (Exception ex) when (((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? 1 : 0) != 0)
		{
			return text.TrimEnd('/');
		}
	}

	private void CreateAudioPicker(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		Button button = new Button
		{
			Text = "♫ 打开游戏音频库",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		button.Pressed += delegate
		{
			OpenAudioLibrary(owner, descriptor);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateTexturePicker(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		Texture2D texture = owner.Get(descriptor.Name).As<Texture2D>();
		TextureRect node = new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(72f, 72f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Text = "打开贴图库…",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		button.Pressed += delegate
		{
			OpenTextureLibrary(owner, descriptor);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateArrayEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		host.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = AddPropertyRow(vBoxContainer, descriptor);
		Button button = new Button
		{
			Text = "+ 添加卡片"
		};
		button.Pressed += delegate
		{
			AddArrayElement(owner, descriptor);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Godot.Collections.Array array = owner.Get(descriptor.Name).AsGodotArray();
		for (int num = 0; num < array.Count; num++)
		{
			int captured = num;
			HBoxContainer hBoxContainer2 = new HBoxContainer();
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer2.AddChild(new Label
			{
				Text = $"#{num + 1}",
				CustomMinimumSize = new Vector2(34f, 0f)
			}, forceReadableName: false, InternalMode.Disabled);
			CreateInlineArrayValueEditor(hBoxContainer2, owner, descriptor, captured, array[num]);
			AddMiniButton(hBoxContainer2, "↑", delegate
			{
				MoveArrayElement(owner, descriptor.Name, captured, captured - 1);
			});
			AddMiniButton(hBoxContainer2, "↓", delegate
			{
				MoveArrayElement(owner, descriptor.Name, captured, captured + 1);
			});
			AddMiniButton(hBoxContainer2, "⧉", delegate
			{
				DuplicateArrayElement(owner, descriptor.Name, captured);
			});
			AddMiniButton(hBoxContainer2, "×", delegate
			{
				RemoveArrayElement(owner, descriptor.Name, captured);
			});
		}
	}

	private void CreateDictionaryEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		host.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = AddPropertyRow(vBoxContainer, descriptor);
		Button button = new Button
		{
			Text = "+ 键值卡"
		};
		button.Pressed += delegate
		{
			AddDictionaryEntry(owner, descriptor);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Dictionary values = owner.Get(descriptor.Name).AsGodotDictionary();
		foreach (Variant key in values.Keys)
		{
			Variant capturedKey = key;
			HBoxContainer hBoxContainer2 = new HBoxContainer();
			LineEdit lineEdit = new LineEdit
			{
				Text = CompactVariant(key),
				CustomMinimumSize = new Vector2(110f, 0f)
			};
			LineEdit lineEdit2 = new LineEdit
			{
				Text = CompactVariant(values[key]),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			lineEdit.TooltipText = "键";
			lineEdit2.TooltipText = "值";
			lineEdit.TextSubmitted += delegate(string raw)
			{
				RenameDictionaryKey(owner, descriptor.Name, capturedKey, ParseInlineVariant(raw, capturedKey));
			};
			lineEdit2.TextSubmitted += delegate(string raw)
			{
				SetDictionaryValue(owner, descriptor.Name, capturedKey, ParseInlineVariant(raw, values[capturedKey]));
			};
			hBoxContainer2.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer2.AddChild(lineEdit2, forceReadableName: false, InternalMode.Disabled);
			AddMiniButton(hBoxContainer2, "×", delegate
			{
				RemoveDictionaryEntry(owner, descriptor.Name, capturedKey);
			});
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void CreateFallbackVisualEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = AddPropertyRow(host, descriptor);
		Label label = new Label
		{
			Text = descriptor.Type.ToString(),
			Modulate = new Color("8fa4bd")
		};
		LineEdit lineEdit = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		_binding.BindVariantText(lineEdit, label, owner, descriptor.Name, LightweightRuntimeStatus, this, ResolveScalarRefreshMethod(owner, descriptor.Name));
	}

	private void AddArrayElement(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		Godot.Collections.Array from = CloneArrayForHistory(owner.Get(descriptor.Name).AsGodotArray());
		if (IsTypedResourceArray(descriptor, from))
		{
			OpenArrayResourcePickerForAdd(owner, descriptor);
			return;
		}
		from.Add(CreateDefaultArrayElement(descriptor, from));
		_binding.SetValue(owner, descriptor.Name, Variant.From(in from), "添加 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
	}

	private static bool IsTypedResourceArray(DynamicPropertyDescriptor descriptor, Godot.Collections.Array current)
	{
		if (current != null)
		{
			foreach (Variant item in current)
			{
				if (item.VariantType != Variant.Type.Nil)
				{
					return item.VariantType == Variant.Type.Object;
				}
			}
		}
		return InferArrayElementType(descriptor.HintString) == Variant.Type.Object;
	}

	private void OpenArrayResourcePickerForAdd(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		EnsureResourcePicker();
		if (_resourcePicker != null)
		{
			string text = ExtractArrayResourceClass(descriptor.HintString);
			_resourcePicker.OpenResourceLibrary(text, "添加 " + Humanize(descriptor.Name), string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { text }, new string[1] { "Resources" }, "res://addons/ModEditor/Icons/Resource.svg", delegate(XWGameplayResourceChoice choice)
			{
				Resource selected = ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				AppendArrayResource(owner, descriptor, selected);
			});
		}
	}

	private void AppendArrayResource(Resource owner, DynamicPropertyDescriptor descriptor, Resource selected)
	{
		if (!GodotObject.IsInstanceValid(selected))
		{
			SetPreviewStatus("未能加载 " + Humanize(descriptor.Name) + " 选中的资源。", warning: true);
			return;
		}
		Godot.Collections.Array from = CloneArrayForHistory(owner.Get(descriptor.Name).AsGodotArray());
		from.Add(Variant.From(in selected));
		_binding.SetValue(owner, descriptor.Name, Variant.From(in from), "添加 " + Humanize(descriptor.Name) + " 资源", this, "RefreshCharacterComponentEditorFromHistory");
	}

	private void CreatePackedInt32ArrayEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		host.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = AddPropertyRow(vBoxContainer, descriptor);
		Button button = new Button
		{
			Text = "+ 整数",
			Name = $"PackedInt32_{descriptor.Name}_Add"
		};
		button.Pressed += delegate
		{
			int[] array2 = owner.Get(descriptor.Name).AsInt32Array();
			int[] array3 = new int[array2.Length + 1];
			array2.CopyTo(array3, 0);
			SetPackedInt32Array(owner, descriptor.Name, array3, "添加 " + Humanize(descriptor.Name));
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		int[] array = owner.Get(descriptor.Name).AsInt32Array();
		for (int num = 0; num < array.Length; num++)
		{
			int captured = num;
			HBoxContainer hBoxContainer2 = new HBoxContainer();
			hBoxContainer2.AddChild(new Label
			{
				Text = $"#{num + 1}",
				CustomMinimumSize = new Vector2(34f, 0f)
			}, forceReadableName: false, InternalMode.Disabled);
			SpinBox spinBox = new SpinBox
			{
				Name = $"PackedInt32_{descriptor.Name}_{num}",
				Value = array[num],
				Step = 1.0,
				AllowGreater = true,
				AllowLesser = true,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			spinBox.FocusEntered += delegate
			{
				_binding.BeginEdit(owner, descriptor.Name);
			};
			spinBox.ValueChanged += delegate(double changed)
			{
				PreviewPackedInt32ArrayElement(owner, descriptor.Name, captured, (int)Math.Round(changed));
			};
			spinBox.FocusExited += delegate
			{
				CommitPackedInt32ArrayEdit(owner, descriptor.Name);
			};
			hBoxContainer2.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			AddMiniButton(hBoxContainer2, "×", delegate
			{
				RemovePackedInt32ArrayElement(owner, descriptor.Name, captured);
			});
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void PreviewPackedInt32ArrayElement(Resource owner, StringName property, int index, int value)
	{
		int[] from = owner.Get(property).AsInt32Array();
		if (index >= 0 && index < from.Length && from[index] != value)
		{
			from[index] = value;
			_binding.PreviewValue(owner, property, Variant.From(in from));
		}
	}

	private void CommitPackedInt32ArrayEdit(Resource owner, StringName property)
	{
		_binding.CommitEdit(owner, property, Variant.From<int[]>(owner.Get(property).AsInt32Array()), "修改 " + Humanize(property), this, "RefreshCharacterComponentEditorFromHistory");
	}

	private void RemovePackedInt32ArrayElement(Resource owner, StringName property, int index)
	{
		int[] array = owner.Get(property).AsInt32Array();
		if (index >= 0 && index < array.Length)
		{
			int[] array2 = new int[array.Length - 1];
			System.Array.Copy(array, 0, array2, 0, index);
			System.Array.Copy(array, index + 1, array2, index, array.Length - index - 1);
			SetPackedInt32Array(owner, property, array2, $"移除 {Humanize(property)} 第 {index + 1} 项");
		}
	}

	private void SetPackedInt32Array(Resource owner, StringName property, int[] value, string action)
	{
		_binding.SetValue(owner, property, Variant.From(in value), action, this, "RefreshCharacterComponentEditorFromHistory");
	}

	private void CreateTransform2DEditor(VBoxContainer host, Resource owner, DynamicPropertyDescriptor descriptor)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		host.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = AddPropertyRow(vBoxContainer, descriptor);
		Button button = new Button
		{
			Text = "重置单位矩阵"
		};
		button.Pressed += delegate
		{
			_binding.SetValue(owner, descriptor.Name, Variant.From<Transform2D>(Transform2D.Identity), "重置 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Transform2D transform2D = owner.Get(descriptor.Name).AsTransform2D();
		GridContainer gridContainer = new GridContainer
		{
			Columns = 3
		};
		vBoxContainer.AddChild(gridContainer, forceReadableName: false, InternalMode.Disabled);
		(string, string, double)[] array = new(string, string, double)[6]
		{
			("X.x", "XX", transform2D.X.X),
			("X.y", "XY", transform2D.X.Y),
			("Y.x", "YX", transform2D.Y.X),
			("Y.y", "YY", transform2D.Y.Y),
			("原点 X", "OX", transform2D.Origin.X),
			("原点 Y", "OY", transform2D.Origin.Y)
		};
		SpinBox[] controls = new SpinBox[array.Length];
		for (int num = 0; num < array.Length; num++)
		{
			gridContainer.AddChild(new Label
			{
				Text = array[num].Item1
			}, forceReadableName: false, InternalMode.Disabled);
			SpinBox spinBox = new SpinBox
			{
				Name = $"Transform2D_{descriptor.Name}_{array[num].Item2}",
				Value = array[num].Item3,
				Step = 0.01,
				AllowGreater = true,
				AllowLesser = true,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			controls[num] = spinBox;
			gridContainer.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			gridContainer.AddChild(new Control
			{
				CustomMinimumSize = new Vector2(8f, 0f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		SpinBox[] array2 = controls;
		foreach (SpinBox obj in array2)
		{
			obj.FocusEntered += delegate
			{
				_binding.BeginEdit(owner, descriptor.Name);
			};
			obj.ValueChanged += delegate
			{
				_binding.PreviewValue(owner, descriptor.Name, Variant.From<Transform2D>(ReadTransform2DControls(controls)));
			};
			obj.FocusExited += delegate
			{
				_binding.CommitEdit(owner, descriptor.Name, Variant.From<Transform2D>(ReadTransform2DControls(controls)), "修改 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
			};
		}
	}

	private static Transform2D ReadTransform2DControls(IReadOnlyList<SpinBox> controls)
	{
		return new Transform2D(new Vector2((float)controls[0].Value, (float)controls[1].Value), new Vector2((float)controls[2].Value, (float)controls[3].Value), new Vector2((float)controls[4].Value, (float)controls[5].Value));
	}

	private void CreateInlineArrayValueEditor(HBoxContainer row, Resource owner, DynamicPropertyDescriptor descriptor, int index, Variant value)
	{
		if (value.VariantType == Variant.Type.Bool)
		{
			CheckButton checkButton = new CheckButton
			{
				ButtonPressed = value.AsBool(),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			checkButton.Toggled += delegate(bool enabled)
			{
				SetArrayElement(owner, descriptor.Name, index, Variant.From(in enabled));
			};
			row.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		Variant.Type variantType = value.VariantType;
		if (((ulong)(variantType - 2) <= 1uL) ? true : false)
		{
			SpinBox spinBox = new SpinBox
			{
				Value = ((value.VariantType == Variant.Type.Int) ? ((double)value.AsInt64()) : value.AsDouble()),
				Step = ((value.VariantType == Variant.Type.Int) ? 1.0 : 0.01),
				AllowGreater = true,
				AllowLesser = true,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			spinBox.ValueChanged += delegate(double changed)
			{
				SetArrayElement(owner, descriptor.Name, index, (value.VariantType == Variant.Type.Int) ? Variant.From<long>((long)Math.Round(changed)) : Variant.From(in changed));
			};
			row.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		if (value.VariantType == Variant.Type.Object)
		{
			Resource current = value.As<Resource>();
			Button button = new Button
			{
				Text = ((current == null) ? "选择资源…" : DisplayResource(current)),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			button.Pressed += delegate
			{
				OpenArrayResourcePicker(owner, descriptor, index, current);
			};
			row.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		LineEdit text = new LineEdit
		{
			Text = CompactVariant(value),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		bool submitted = false;
		text.TextSubmitted += delegate(string raw)
		{
			submitted = true;
			Commit(raw);
		};
		text.FocusExited += delegate
		{
			if (!submitted)
			{
				Commit(text.Text);
			}
		};
		row.AddChild(text, forceReadableName: false, InternalMode.Disabled);
		void Commit(string raw)
		{
			Variant value2 = ParseInlineVariant(raw, value);
			SetArrayElement(owner, descriptor.Name, index, value2);
		}
	}

	private void SetArrayElement(Resource owner, StringName property, int index, Variant value)
	{
		Godot.Collections.Array from = CloneArrayForHistory(owner.Get(property).AsGodotArray());
		if (index >= 0 && index < from.Count && !from[index].Equals(value))
		{
			from[index] = value;
			_binding.SetValue(owner, property, Variant.From(in from), $"修改 {Humanize(property)} 第 {index + 1} 项", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void RemoveArrayElement(Resource owner, StringName property, int index)
	{
		Godot.Collections.Array from = CloneArrayForHistory(owner.Get(property).AsGodotArray());
		if (index >= 0 && index < from.Count)
		{
			from.RemoveAt(index);
			_binding.SetValue(owner, property, Variant.From(in from), "移除 " + Humanize(property), this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void MoveArrayElement(Resource owner, StringName property, int from, int to)
	{
		Godot.Collections.Array from2 = CloneArrayForHistory(owner.Get(property).AsGodotArray());
		if (from >= 0 && from < from2.Count && to >= 0 && to < from2.Count)
		{
			Variant item = from2[from];
			from2.RemoveAt(from);
			from2.Insert(to, item);
			_binding.SetValue(owner, property, Variant.From(in from2), "移动 " + Humanize(property), this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void DuplicateArrayElement(Resource owner, StringName property, int index)
	{
		Godot.Collections.Array from = CloneArrayForHistory(owner.Get(property).AsGodotArray());
		if (index >= 0 && index < from.Count)
		{
			from.Insert(index + 1, CloneVariantForHistory(from[index]));
			_binding.SetValue(owner, property, Variant.From(in from), "复制 " + Humanize(property), this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private static Godot.Collections.Array CloneArrayForHistory(Godot.Collections.Array source)
	{
		return source?.Duplicate(deep: true) ?? new Godot.Collections.Array();
	}

	private static Variant CloneVariantForHistory(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 24;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 4:
				return Variant.From<Godot.Collections.Array>(value.AsGodotArray().Duplicate(deep: true));
			case 3:
				return Variant.From<Dictionary>(value.AsGodotDictionary().Duplicate(deep: true));
			case 0:
			{
				Resource resource = value.As<Resource>();
				if (resource != null)
				{
					return Variant.From<Resource>(resource.Duplicate(deep: true));
				}
				break;
			}
			}
		}
		return value;
	}

	private void AddDictionaryEntry(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		Dictionary from = owner.Get(descriptor.Name).AsGodotDictionary().Duplicate(deep: true);
		(Type Key, Type Value) tuple = ResolveDictionaryTypes(owner, descriptor.Name);
		Type item = tuple.Key;
		Type item2 = tuple.Value;
		Variant key = CreateDictionaryDefault(item, key: true);
		Variant value = CreateDictionaryDefault(item2, key: false);
		if (!TryMakeUniqueDictionaryKey(from, item, ref key))
		{
			SetPreviewStatus("无法为 " + Humanize(descriptor.Name) + " 生成唯一键", warning: true);
			return;
		}
		try
		{
			from[key] = value;
		}
		catch (Exception ex)
		{
			SetPreviewStatus(Humanize(descriptor.Name) + " 的键值类型不兼容：" + ex.Message, warning: true);
			return;
		}
		_binding.SetValue(owner, descriptor.Name, Variant.From(in from), "添加 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The Mod editor inspects loaded component resource exports to preserve typed dictionary entries.")]
	private static (Type Key, Type Value) ResolveDictionaryTypes(Resource owner, StringName property)
	{
		Type type = (owner?.GetType().GetProperty(property.ToString(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))?.PropertyType;
		if ((object)type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Godot.Collections.Dictionary<, >))
		{
			Type[] genericArguments = type.GetGenericArguments();
			return (Key: genericArguments[0], Value: genericArguments[1]);
		}
		return (Key: typeof(string), Value: typeof(string));
	}

	private static Variant CreateDictionaryDefault(Type type, bool key)
	{
		if (type == typeof(bool))
		{
			return Variant.From<bool>(false);
		}
		if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || ((object)type != null && type.IsEnum))
		{
			return Variant.From<long>(0L);
		}
		if (type == typeof(float) || type == typeof(double))
		{
			return Variant.From<double>(0.0);
		}
		if (type == typeof(StringName))
		{
			return Variant.From<StringName>(new StringName(key ? "new_key" : string.Empty));
		}
		if (type == typeof(NodePath))
		{
			return Variant.From<NodePath>(new NodePath(key ? "new_key" : string.Empty));
		}
		if (type == typeof(Vector2))
		{
			return Variant.From<Vector2>(Vector2.Zero);
		}
		if (type == typeof(Vector2I))
		{
			return Variant.From<Vector2I>(Vector2I.Zero);
		}
		if (type == typeof(Color))
		{
			return Variant.From<Color>(Colors.White);
		}
		if (type != null && typeof(GodotObject).IsAssignableFrom(type))
		{
			return default(Variant);
		}
		return Variant.From<string>(key ? "new_key" : string.Empty);
	}

	private static bool TryMakeUniqueDictionaryKey(Dictionary dictionary, Type keyType, ref Variant key)
	{
		if (!dictionary.ContainsKey(key))
		{
			return true;
		}
		if (keyType == typeof(bool))
		{
			key = Variant.From<bool>(true);
			return !dictionary.ContainsKey(key);
		}
		if (keyType == typeof(byte) || keyType == typeof(sbyte) || keyType == typeof(short) || keyType == typeof(ushort) || keyType == typeof(int) || keyType == typeof(uint) || keyType == typeof(long) || keyType == typeof(ulong) || ((object)keyType != null && keyType.IsEnum))
		{
			for (long from = 1L; from < 10000; from++)
			{
				key = Variant.From(in from);
				if (!dictionary.ContainsKey(key))
				{
					return true;
				}
			}
			return false;
		}
		for (int i = 1; i < 10000; i++)
		{
			string from2 = $"new_key_{i}";
			key = ((keyType == typeof(StringName)) ? Variant.From<StringName>(new StringName(from2)) : ((keyType == typeof(NodePath)) ? Variant.From<NodePath>(new NodePath(from2)) : Variant.From(in from2)));
			if (!dictionary.ContainsKey(key))
			{
				return true;
			}
		}
		return false;
	}

	private void RemoveDictionaryEntry(Resource owner, StringName property, Variant key)
	{
		Dictionary from = owner.Get(property).AsGodotDictionary().Duplicate(deep: true);
		from.Remove(key);
		_binding.SetValue(owner, property, Variant.From(in from), "移除 " + Humanize(property), this, "RefreshCharacterComponentEditorFromHistory");
	}

	private void SetDictionaryValue(Resource owner, StringName property, Variant key, Variant value)
	{
		Dictionary from = owner.Get(property).AsGodotDictionary().Duplicate(deep: true);
		if (from.ContainsKey(key) && !from[key].Equals(value))
		{
			from[key] = value;
			_binding.SetValue(owner, property, Variant.From(in from), "修改 " + Humanize(property) + " 的值", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void RenameDictionaryKey(Resource owner, StringName property, Variant oldKey, Variant newKey)
	{
		Dictionary from = owner.Get(property).AsGodotDictionary().Duplicate(deep: true);
		if (from.ContainsKey(oldKey) && !oldKey.Equals(newKey) && !from.ContainsKey(newKey))
		{
			Variant value = from[oldKey];
			from.Remove(oldKey);
			from[newKey] = value;
			_binding.SetValue(owner, property, Variant.From(in from), "修改 " + Humanize(property) + " 的键", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ModEditor discovers built-in component definitions and consumes an explicit active Mod type catalog.")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Godot component definitions are editor-instantiable Resource classes.")]
	private void BuildComponentTypeLibrary()
	{
		if (_componentLibraryInitialized)
		{
			return;
		}
		_componentTypes.Clear();
		HashSet<Type> hashSet = new HashSet<Type>();
		foreach (Type item in GetAssemblyTypesSafe(typeof(CharacterComponentDefinition).Assembly))
		{
			if (IsComponentDefinitionLibraryType(item) && item != typeof(ModCharacterComponentDefinition) && hashSet.Add(item))
			{
				_componentTypes.Add(item);
			}
		}
		foreach (Type modComponentDefinitionType in _modComponentDefinitionTypes)
		{
			if (IsComponentDefinitionLibraryType(modComponentDefinitionType) && modComponentDefinitionType.Assembly == _modComponentAssembly && hashSet.Add(modComponentDefinitionType))
			{
				_componentTypes.Add(modComponentDefinitionType);
			}
		}
		_componentTypes.Sort((Type left, Type right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
		_componentLibraryInitialized = true;
		_componentLibraryCardsDirty = true;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The built-in component assembly is explicitly scanned for the editor component library.")]
	private static IEnumerable<Type> GetAssemblyTypesSafe(Assembly assembly)
	{
		try
		{
			return assembly?.GetTypes() ?? System.Array.Empty<Type>();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type type) => type != null);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Editor catalog entries are intentionally filtered to public parameterless component definitions.")]
	private static bool IsComponentDefinitionLibraryType(Type type)
	{
		if (type != null && type != typeof(CharacterComponentDefinition) && typeof(CharacterComponentDefinition).IsAssignableFrom(type) && !type.IsAbstract)
		{
			return type.GetConstructor(Type.EmptyTypes) != null;
		}
		return false;
	}

	private void FilterComponentTypeLibrary()
	{
		if (_componentTypeGrid == null)
		{
			return;
		}
		ClearChildren(_componentTypeGrid);
		string value = _componentLibrarySearch?.Text?.StripEdges() ?? string.Empty;
		int num = 0;
		foreach (Type type in _componentTypes)
		{
			if (!string.IsNullOrEmpty(value) && !type.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (_visualChoiceCardScene == null)
			{
				_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			XWGameVisualChoiceCard card = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(card))
			{
				card.Configure(type.FullName, Humanize(type.Name.Replace("ComponentDefinition", string.Empty)), "组件拼图", XWClassRegistry.Instance.GetClassIcon(type.Name), type == _selectedComponentType);
				card.Pressed += delegate
				{
					SelectComponentTypeCard(type, card);
				};
				_componentTypeGrid.AddChild(card, forceReadableName: false, InternalMode.Disabled);
				num++;
			}
		}
		Label label = _layoutRoot?.GetNodeOrNull<Label>("%LibraryCountLabel");
		if (label != null)
		{
			label.Text = $"{num} 张";
		}
	}

	private void AddSelectedLibraryComponent()
	{
		AddComponentFromLibrary(_selectedComponentType);
	}

	private void SelectComponentTypeCard(Type type, XWGameVisualChoiceCard selectedCard)
	{
		_selectedComponentType = type;
		foreach (Node child in _componentTypeGrid.GetChildren())
		{
			if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard)
			{
				xWGameVisualChoiceCard.SetSelected(xWGameVisualChoiceCard == selectedCard);
			}
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The component library admits only public parameterless Resource definitions.")]
	private void AddComponentFromLibrary(Type type)
	{
		if (_editingSet == null || type == null)
		{
			return;
		}
		CharacterComponentDefinition characterComponentDefinition;
		try
		{
			if (type.Assembly == typeof(CharacterComponentDefinition).Assembly)
			{
				characterComponentDefinition = Activator.CreateInstance(type) as CharacterComponentDefinition;
			}
			else
			{
				CharacterComponentDefinition characterComponentDefinition2 = Activator.CreateInstance(type) as CharacterComponentDefinition;
				CharacterComponentRuntime characterComponentRuntime = characterComponentDefinition2?.CreateRuntime();
				ModCharacterComponentDefinition modCharacterComponentDefinition = new ModCharacterComponentDefinition();
				modCharacterComponentDefinition.DefinitionTypeName = type.FullName ?? type.Name;
				modCharacterComponentDefinition.RuntimeTypeName = characterComponentRuntime?.GetType().FullName ?? type.Name.Replace("Definition", string.Empty);
				modCharacterComponentDefinition.SynchronizeConfiguration(characterComponentDefinition2);
				characterComponentDefinition = modCharacterComponentDefinition;
				characterComponentRuntime?.Release();
				characterComponentDefinition2?.Dispose();
			}
		}
		catch (Exception ex)
		{
			SetPreviewStatus("无法创建组件 " + type.Name + "：" + ex.Message, warning: true);
			return;
		}
		if (characterComponentDefinition != null)
		{
			characterComponentDefinition.ResourceName = Humanize(type.Name);
			characterComponentDefinition.ComponentTypeId = type.Name.Replace("Definition", string.Empty);
			characterComponentDefinition.DefinitionId = Guid.NewGuid().ToString("N");
			characterComponentDefinition.InstanceId = Guid.NewGuid().ToString("N");
			characterComponentDefinition.WireIndex = NextWireIndex(_editingSet);
			Array<CharacterComponentDefinition> from = CloneComponents(_editingSet.Components);
			from.Add(characterComponentDefinition);
			_selectedDefinition = characterComponentDefinition;
			_binding.SetValue(_editingSet, "Components", Variant.From(in from), "添加 " + type.Name, this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Only explicit active Mod definition types admitted by the catalog are instantiated.")]
	private bool SynchronizeOpenModDefinitionSchemas()
	{
		if (_modComponentDefinitionTypes.Count == 0)
		{
			return false;
		}
		System.Collections.Generic.Dictionary<string, Type> dictionary = new System.Collections.Generic.Dictionary<string, Type>(StringComparer.Ordinal);
		foreach (Type modComponentDefinitionType in _modComponentDefinitionTypes)
		{
			if (!string.IsNullOrWhiteSpace(modComponentDefinitionType.FullName))
			{
				dictionary[modComponentDefinitionType.FullName] = modComponentDefinitionType;
			}
			dictionary[modComponentDefinitionType.Name] = modComponentDefinitionType;
		}
		IEnumerable<ModCharacterComponentDefinition> source;
		if (_editingSet == null)
		{
			IEnumerable<ModCharacterComponentDefinition> enumerable = ((!(_editingResource is ModCharacterComponentDefinition modCharacterComponentDefinition)) ? ((IEnumerable<ModCharacterComponentDefinition>)System.Array.Empty<ModCharacterComponentDefinition>()) : ((IEnumerable<ModCharacterComponentDefinition>)new ModCharacterComponentDefinition[1] { modCharacterComponentDefinition }));
			source = enumerable;
		}
		else
		{
			source = _editingSet.Components.OfType<ModCharacterComponentDefinition>();
		}
		bool flag = false;
		foreach (ModCharacterComponentDefinition item in source.Distinct())
		{
			if (!dictionary.TryGetValue(item.DefinitionTypeName ?? string.Empty, out var value))
			{
				continue;
			}
			CharacterComponentDefinition characterComponentDefinition = null;
			try
			{
				characterComponentDefinition = Activator.CreateInstance(value) as CharacterComponentDefinition;
				if (characterComponentDefinition != null)
				{
					flag |= item.SynchronizeConfiguration(characterComponentDefinition);
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning("Mod component schema refresh failed for " + value.FullName + ": " + ex.Message);
			}
			finally
			{
				characterComponentDefinition?.Dispose();
			}
		}
		return flag;
	}

	private void BuildInheritanceTree()
	{
		if (_inheritanceTree == null)
		{
			return;
		}
		_inheritanceTree.Clear();
		TreeItem treeItem = _inheritanceTree.CreateItem();
		treeItem.SetText(0, "组件集合继承");
		if (_editingSet == null)
		{
			_inheritanceTree.CreateItem(treeItem).SetText(0, _editingResource?.ResourceName ?? _editingResource?.GetType().Name ?? "组件");
			return;
		}
		List<CharacterComponentSet> list = new List<CharacterComponentSet>();
		HashSet<CharacterComponentSet> hashSet = new HashSet<CharacterComponentSet>();
		CharacterComponentSet characterComponentSet = _editingSet;
		while (characterComponentSet != null && hashSet.Add(characterComponentSet))
		{
			list.Add(characterComponentSet);
			characterComponentSet = characterComponentSet.ParentSet;
		}
		list.Reverse();
		TreeItem parent = treeItem;
		foreach (CharacterComponentSet item in list)
		{
			TreeItem treeItem2 = _inheritanceTree.CreateItem(parent);
			treeItem2.SetText(0, string.IsNullOrWhiteSpace(item.ResourceName) ? item.ResourcePath.GetFile().GetBaseName() : item.ResourceName);
			treeItem2.SetTooltipText(0, item.ResourcePath);
			parent = treeItem2;
		}
		if (!ValidateComponentInheritanceCycle(_editingSet))
		{
			treeItem.SetText(0, "⚠ 组件集合存在继承环");
		}
	}

	private static bool ValidateComponentInheritanceCycle(CharacterComponentSet set)
	{
		HashSet<CharacterComponentSet> hashSet = new HashSet<CharacterComponentSet>();
		for (CharacterComponentSet characterComponentSet = set; characterComponentSet != null; characterComponentSet = characterComponentSet.ParentSet)
		{
			if (!hashSet.Add(characterComponentSet))
			{
				return false;
			}
		}
		return true;
	}

	private void BuildEffectiveComponentRows()
	{
		_effectiveList?.Clear();
		_localList?.Clear();
		if (_editingSet == null)
		{
			if (_selectedDefinition != null)
			{
				AddDefinitionRow(_effectiveList, _selectedDefinition, "独立");
			}
			SynchronizeDefinitionListSelection(_effectiveList, _selectedDefinition);
			return;
		}
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = _editingSet.GetFlattenedDefinitions();
		foreach (CharacterComponentDefinition item in flattenedDefinitions)
		{
			AddDefinitionRow(_effectiveList, item, IsLocalDefinition(item) ? "本地" : "继承");
		}
		foreach (CharacterComponentDefinition component in _editingSet.Components)
		{
			AddDefinitionRow(_localList, component, "本地");
		}
		if (_selectedDefinition != null && !flattenedDefinitions.Any((CharacterComponentDefinition definition) => definition == _selectedDefinition))
		{
			_selectedDefinition = null;
		}
		if (_selectedDefinition == null)
		{
			_selectedDefinition = flattenedDefinitions.FirstOrDefault();
		}
		SynchronizeDefinitionListSelection(_effectiveList, _selectedDefinition);
		SynchronizeDefinitionListSelection(_localList, IsLocalDefinition(_selectedDefinition) ? _selectedDefinition : null);
	}

	private static void SynchronizeDefinitionListSelection(ItemList list, CharacterComponentDefinition selectedDefinition)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.DeselectAll();
		if (!GodotObject.IsInstanceValid(selectedDefinition))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemMetadata(i).As<CharacterComponentDefinition>() == selectedDefinition)
			{
				list.Select(i);
				break;
			}
		}
	}

	private static void AddDefinitionRow(ItemList list, CharacterComponentDefinition definition, string badge)
	{
		if (list != null && definition != null)
		{
			string value = (string.IsNullOrWhiteSpace(definition.ResourceName) ? definition.GetType().Name : definition.ResourceName);
			int idx = list.AddItem($"[{badge}] {value}  ·  {definition.InstanceId}", XWClassRegistry.Instance.GetClassIcon(definition.GetType().Name));
			list.SetItemMetadata(idx, Variant.From(in definition));
		}
	}

	private void SelectDefinitionFromList(ItemList list, long index)
	{
		_selectedDefinition = list?.GetItemMetadata((int)index).As<CharacterComponentDefinition>();
		RenderSelectedDefinitionProperties();
		InvalidateDeferredSurfaces();
		ActivateCurrentWorkbenchPage();
	}

	private bool IsLocalDefinition(CharacterComponentDefinition definition)
	{
		return _editingSet?.Components.Any((CharacterComponentDefinition local) => local == definition) ?? false;
	}

	private void OverrideInheritedComponent()
	{
		if (_editingSet != null && _selectedDefinition != null && !IsLocalDefinition(_selectedDefinition) && _selectedDefinition.Duplicate(deep: true) is CharacterComponentDefinition characterComponentDefinition)
		{
			characterComponentDefinition.InstanceId = _selectedDefinition.InstanceId;
			characterComponentDefinition.ComponentTypeId = _selectedDefinition.ComponentTypeId;
			characterComponentDefinition.WireIndex = _selectedDefinition.WireIndex;
			Array<CharacterComponentDefinition> from = CloneComponents(_editingSet.Components);
			from.Add(characterComponentDefinition);
			_selectedDefinition = characterComponentDefinition;
			_binding.SetValue(_editingSet, "Components", Variant.From(in from), "覆盖继承组件", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void RemoveInheritedComponent()
	{
		if (_editingSet == null || _selectedDefinition == null)
		{
			return;
		}
		if (IsLocalDefinition(_selectedDefinition))
		{
			Array<CharacterComponentDefinition> from = CloneComponents(_editingSet.Components);
			for (int num = from.Count - 1; num >= 0; num--)
			{
				if (from[num] == _selectedDefinition)
				{
					from.RemoveAt(num);
				}
			}
			_binding.SetValue(_editingSet, "Components", Variant.From(in from), "移除本地组件", this, "RefreshCharacterComponentEditorFromHistory");
		}
		else
		{
			Array<string> from2 = CloneRemovedIds(_editingSet.RemovedInstanceIds);
			if (!from2.Contains(_selectedDefinition.InstanceId))
			{
				from2.Add(_selectedDefinition.InstanceId);
			}
			_binding.SetValue(_editingSet, "RemovedInstanceIds", Variant.From(in from2), "移除继承组件", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void RestoreInheritedComponent()
	{
		if (_editingSet != null)
		{
			string text = _selectedDefinition?.InstanceId;
			if (string.IsNullOrWhiteSpace(text) && _editingSet.RemovedInstanceIds.Count > 0)
			{
				Array<string> removedInstanceIds = _editingSet.RemovedInstanceIds;
				text = removedInstanceIds[removedInstanceIds.Count - 1];
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				Array<string> from = CloneRemovedIds(_editingSet.RemovedInstanceIds);
				from.Remove(text);
				_binding.SetValue(_editingSet, "RemovedInstanceIds", Variant.From(in from), "恢复继承组件", this, "RefreshCharacterComponentEditorFromHistory");
			}
		}
	}

	private void EmbedStateMachineEditor()
	{
		DisconnectStateMachineEditor();
		Control control = _layoutRoot?.GetNodeOrNull<Control>("%StateMachineEditorHost");
		StateMachineDefinition stateMachineDefinition = _selectedDefinition?.StateMachineDefinition;
		if (control == null)
		{
			return;
		}
		bool flag = _selectedDefinition != null && _editingSet != null && !IsLocalDefinition(_selectedDefinition);
		SetButtonDisabled("%CreateStateMachineButton", _selectedDefinition == null || flag || stateMachineDefinition != null);
		SetButtonDisabled("%ChooseStateMachineButton", _selectedDefinition == null || flag);
		SetButtonDisabled("%ClearStateMachineButton", _selectedDefinition == null || flag || stateMachineDefinition == null);
		Label nodeOrNull = _layoutRoot.GetNodeOrNull<Label>("%StateMachineStatus");
		if (nodeOrNull != null)
		{
			nodeOrNull.Text = (flag ? "继承状态机只读；覆盖组件后可编辑" : ((stateMachineDefinition == null) ? "尚未配置状态机" : ("已连接：" + FirstNonEmpty(stateMachineDefinition.ResourceName, stateMachineDefinition.DefinitionId))));
		}
		_stateSurface = control.GetNodeOrNull<StateMachineGraphEditorSurface>("%StateMachineGraphEditorSurface");
		if (_stateSurface == null)
		{
			if (_stateSurfaceScene == null)
			{
				_stateSurfaceScene = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_stateSurface = _stateSurfaceScene?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
			if (_stateSurface != null)
			{
				control.AddChild(_stateSurface, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (_stateSurface != null)
		{
			_stateSurface.Visible = stateMachineDefinition != null;
			if (stateMachineDefinition != null)
			{
				_stateController = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
				_stateUndoAdapter = new XWStateMachineUndoAdapter(XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager());
				_stateSurface.Bind(_stateController, _stateUndoAdapter);
				_embeddedStateMachineLayoutIdentity = ResolveEmbeddedStateMachineLayoutIdentity(stateMachineDefinition);
				StateMachineLayout layout = (string.IsNullOrWhiteSpace(_embeddedStateMachineLayoutIdentity) ? new StateMachineLayout() : (_stateLayoutStore.Load(_embeddedStateMachineLayoutIdentity) ?? new StateMachineLayout()));
				_stateSurface.LoadDefinition(stateMachineDefinition, layout, flag);
				_stateSurface.DefinitionChanged += OnEmbeddedStateMachineChanged;
				_stateSurface.LayoutChanged += OnEmbeddedStateMachineLayoutChanged;
				_stateSurface.ResourcePickerRequested += OnStateMachineResourcePickerRequested;
				_stateSurface.GuardEditRequested += OnEmbeddedGuardEditRequested;
				BindEmbeddedStateMachineRuntimeTarget();
			}
		}
	}

	private void BindEmbeddedStateMachineRuntimeTarget()
	{
		StateMachineSimulationPanel stateMachineSimulationPanel = _stateSurface?.SimulationPanel;
		if (stateMachineSimulationPanel != null)
		{
			string text = FirstNonEmpty(_selectedDefinition?.ResourceName, _selectedDefinition?.DefinitionId, _selectedDefinition?.GetType().Name, "角色组件");
			stateMachineSimulationPanel.BindRuntimeTarget(new StateMachineRuntimeDebugTarget("真实组件 · " + text, EnsureEmbeddedRuntimeCurrent, () => _runtimePreview?.StateMachine, (StringName eventName) => _runtimePreview?.SendStateEvent(eventName, allowWhenInactive: true) ?? false, StepEmbeddedRuntime, ResetEmbeddedRuntime));
		}
	}

	private bool EnsureEmbeddedRuntimeCurrent()
	{
		if (_runtimePreviewDirty)
		{
			EnsureRuntimePreview();
		}
		CharacterComponentRuntime runtimePreview = _runtimePreview;
		if (runtimePreview != null && runtimePreview.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return _runtimePreview.StateMachine?.IsInitialized ?? false;
		}
		return false;
	}

	private bool StepEmbeddedRuntime(double seconds)
	{
		IStateMachineController stateMachineController = _runtimePreview?.StateMachine;
		if (stateMachineController == null || !stateMachineController.IsInitialized)
		{
			return false;
		}
		double delta = Math.Max(0.0, seconds);
		stateMachineController.TickProcess(delta);
		stateMachineController.TickPhysics(delta);
		return true;
	}

	private bool ResetEmbeddedRuntime()
	{
		RebuildCharacterRuntimePreview();
		return EnsureEmbeddedRuntimeCurrent();
	}

	private void CreateStateMachineDefinition()
	{
		if (CanEditSelectedDefinition() && _selectedDefinition.StateMachineDefinition == null)
		{
			string text = "root." + Guid.NewGuid().ToString("N");
			string text2 = "state." + Guid.NewGuid().ToString("N");
			StateMachineDefinition stateMachineDefinition = new StateMachineDefinition();
			stateMachineDefinition.ResourceName = FirstNonEmpty(_selectedDefinition.ResourceName, _selectedDefinition.GetType().Name) + " 状态机";
			stateMachineDefinition.DefinitionId = Guid.NewGuid().ToString("N");
			stateMachineDefinition.RootStateId = text;
			stateMachineDefinition.States = new Array<StateMachineStateDefinition>
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
			};
			StateMachineDefinition from = stateMachineDefinition;
			_binding.SetValue(_selectedDefinition, "StateMachineDefinition", Variant.From(in from), "新建组件状态机", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private void ChooseStateMachineDefinition()
	{
		if (CanEditSelectedDefinition())
		{
			OpenResourcePicker(_selectedDefinition, Descriptor("StateMachineDefinition", Variant.Type.Object)with
			{
				ClassName = "StateMachineDefinition",
				HintString = "StateMachineDefinition"
			});
		}
	}

	private void ClearStateMachineDefinition()
	{
		if (CanEditSelectedDefinition() && _selectedDefinition.StateMachineDefinition != null)
		{
			_binding.SetValue(_selectedDefinition, "StateMachineDefinition", default(Variant), "清除组件状态机", this, "RefreshCharacterComponentEditorFromHistory");
		}
	}

	private bool CanEditSelectedDefinition()
	{
		if (_selectedDefinition != null)
		{
			if (_editingSet != null)
			{
				return IsLocalDefinition(_selectedDefinition);
			}
			return true;
		}
		return false;
	}

	private void OnEmbeddedStateMachineChanged()
	{
		StateMachineDefinition stateMachineDefinition = _selectedDefinition?.StateMachineDefinition;
		if (IsExternalStateMachineResource(stateMachineDefinition))
		{
			_dirtyExternalStateMachines.Add(stateMachineDefinition);
		}
		_runtimePreviewDirty = true;
		_stateSurface?.SimulationPanel?.InvalidateRuntimeTarget("状态机配置已变化；再次启动时会重建真实组件预览。");
		NotifyCurrentResourceEdited();
	}

	private void OnEmbeddedStateMachineLayoutChanged()
	{
		if (_stateSurface?.Layout != null && !string.IsNullOrWhiteSpace(_embeddedStateMachineLayoutIdentity))
		{
			Error error = _stateLayoutStore.Save(_embeddedStateMachineLayoutIdentity, _stateSurface.Layout);
			if (error != Error.Ok)
			{
				GD.PushWarning($"Embedded state-machine layout save failed: {_embeddedStateMachineLayoutIdentity} ({error})");
				XWEditorInterface.Instance?.ShowToast($"状态机画布布局保存失败：{error}");
			}
		}
	}

	protected override void OnCurrentResourceSaved()
	{
		base.OnCurrentResourceSaved();
		if (_dirtyExternalStateMachines.Count == 0)
		{
			return;
		}
		StateMachineDefinition[] array = _dirtyExternalStateMachines.ToArray();
		List<string> list = new List<string>();
		StateMachineDefinition[] array2 = array;
		foreach (StateMachineDefinition stateMachineDefinition in array2)
		{
			if (!GodotObject.IsInstanceValid(stateMachineDefinition) || !IsExternalStateMachineResource(stateMachineDefinition))
			{
				_dirtyExternalStateMachines.Remove(stateMachineDefinition);
				continue;
			}
			Error error = ResourceSaver.Save(stateMachineDefinition, stateMachineDefinition.ResourcePath, ResourceSaver.SaverFlags.None);
			if (error == Error.Ok)
			{
				_dirtyExternalStateMachines.Remove(stateMachineDefinition);
				continue;
			}
			list.Add($"{stateMachineDefinition.ResourcePath} ({error})");
		}
		if (list.Count != 0)
		{
			NotifyCurrentResourceEdited();
			string text = string.Join("；", list);
			GD.PushWarning("External state-machine save failed: " + text);
			XWEditorInterface.Instance?.ShowToast("外部状态机保存失败：" + text);
		}
	}

	private bool IsExternalStateMachineResource(StateMachineDefinition definition)
	{
		string text = definition?.ResourcePath?.Replace('\\', '/');
		if (string.IsNullOrWhiteSpace(text) || text.Contains("::", StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = FirstNonEmpty(base.CurrentResourcePath, _editingResource?.ResourcePath)?.Replace('\\', '/');
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return !string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private string ResolveEmbeddedStateMachineLayoutIdentity(StateMachineDefinition definition)
	{
		if (!string.IsNullOrWhiteSpace(definition?.ResourcePath) && !definition.ResourcePath.Contains("::", StringComparison.Ordinal))
		{
			return definition.ResourcePath;
		}
		string text = FirstNonEmpty(base.CurrentResourcePath, _editingResource?.ResourcePath, _editingSet?.ResourcePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		char[] value = FirstNonEmpty(_selectedDefinition?.InstanceId, _selectedDefinition?.DefinitionId, definition?.DefinitionId, "component").Select(delegate(char character)
		{
			bool flag = char.IsLetterOrDigit(character);
			if (!flag)
			{
				bool flag2 = ((character == '-' || character == '_') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : character;
		}).ToArray();
		string text2 = text.Replace('\\', '/');
		int num = text2.LastIndexOf('.');
		if (num > text2.LastIndexOf('/'))
		{
			text2 = text2.Substring(0, num);
		}
		return text2 + ".component." + new string(value) + ".state_machine.tres";
	}

	private void OnStateMachineResourcePickerRequested(Resource owner, string property)
	{
		if (!GodotObject.IsInstanceValid(owner) || string.IsNullOrWhiteSpace(property))
		{
			return;
		}
		EnsureResourcePicker();
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			return;
		}
		Resource resource = owner.Get(property).As<Resource>();
		bool requiresDefinition = property == "BaseDefinition";
		int generation = _resourcePickerGeneration;
		StateMachineGraphEditorSurface boundSurface = _stateSurface;
		StateMachineGraphController boundController = boundSurface?.GraphController;
		_resourcePicker.OpenResourceLibrary("StateMachine", "状态机资源", resource?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, (!requiresDefinition) ? new string[1] { "Resource" } : new string[1] { "StateMachineDefinition" }, new string[2] { "Resources/StateMachines", "/StateMachines/" }, "res://addons/ModEditor/Icons/GraphEdit.svg", delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _resourcePickerGeneration && IsResourcePickerOwnerActive(owner) && GodotObject.IsInstanceValid(boundSurface) && _stateSurface == boundSurface && boundSurface.GraphController == boundController)
			{
				StateMachineGraphController stateMachineGraphController = boundController;
				if (stateMachineGraphController == null || stateMachineGraphController.ViewModel?.IsReadOnly != true)
				{
					Resource resource2 = LoadPickedStateMachineResource(choice?.ResourcePath);
					if (GodotObject.IsInstanceValid(resource2) && (!requiresDefinition || resource2 is StateMachineDefinition))
					{
						boundSurface.ApplyPickedResource(owner, property, resource2);
					}
				}
			}
		});
	}

	private void OnEmbeddedGuardEditRequested(Resource child, Resource owner, string property, int index)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(owner))
		{
			string text = FirstNonEmpty(base.CurrentResourcePath, _editingResource?.ResourcePath, owner.ResourcePath);
			StateMachineDefinition stateMachineDefinition = _selectedDefinition?.StateMachineDefinition;
			bool num = IsExternalStateMachineResource(stateMachineDefinition);
			Resource persistenceRootResource = (num ? stateMachineDefinition : _editingResource);
			string text2 = (num ? stateMachineDefinition.ResourcePath : text);
			string resourcePath = child.ResourcePath;
			XWResourceEditContext currentEditContext = base.CurrentEditContext;
			XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, resourcePath, text, property, index, "character_component_state_machine", (currentEditContext != null && currentEditContext.IsBuiltInSource) || XWResourceEditContext.IsBuiltInPath(text2), "", persistenceRootResource, text2);
			XWEditorInterface.Instance?.EditResource(child, context);
		}
	}

	private static Resource LoadPickedStateMachineResource(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		string text = path.Replace('\\', '/').Trim();
		string text2 = LocalizeKnownStateMachineResourcePath(text);
		Resource resource = ResourceLoader.Load<Resource>(text2, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(resource) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			resource = ResourceLoader.Load<Resource>(text, "", ResourceLoader.CacheMode.Ignore);
		}
		return resource;
	}

	private static string LocalizeKnownStateMachineResourcePath(string path)
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

	private void BuildCharacterRuntimePreview()
	{
		ResetRuntimePreview();
		if (_selectedDefinition == null)
		{
			SetPreviewStatus("选择组件后会创建真实运行时预览。", warning: false);
			return;
		}
		if (string.IsNullOrWhiteSpace(_selectedDefinition.ComponentTypeId) || string.IsNullOrWhiteSpace(_selectedDefinition.InstanceId))
		{
			SetPreviewStatus("请先配置 ComponentTypeId 与 InstanceId，再启动运行时预览。", warning: true);
			return;
		}
		try
		{
			_runtimePreviewOwner = InstantiatePreviewCharacter();
			if (!GodotObject.IsInstanceValid(_runtimePreviewOwner))
			{
				_runtimePreviewOwner = new TowerDefenseCharacter
				{
					Name = "ComponentPreviewHarness",
					inGame = false,
					editorPreviewMode = true,
					config = _previewCharacterConfig
				};
			}
			_runtimePreviewManager = new ComponentManager
			{
				Name = "ComponentPreviewRuntimeManager"
			};
			_runtimePreviewOwner.componentManager = _runtimePreviewManager;
			_runtimePreviewManager.AttachOwner(_runtimePreviewOwner);
			_runtimePreview = _runtimePreviewManager.AddRuntimeComponent(_selectedDefinition);
			if (_runtimePreview == null)
			{
				SetPreviewStatus("组件没有返回运行时。", warning: true);
				return;
			}
			if (!_runtimePreviewManager.IsInsideTree())
			{
				_runtimePreview.Activate();
				_runtimePreview.RegisterStateRuntime();
			}
			UpdateResponsivePreviewProcessing();
			_fireVolleyPresenter?.BindPreviewCharacter(_runtimePreviewOwner);
			_attackContactPresenter?.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as AttackComponent);
			_explosionStagePresenter?.BindPreviewCharacter(_runtimePreviewOwner);
			_productionDropPresenter?.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as ProduceComponent);
			_slotPositionPresenter?.BindPreviewRuntime(_runtimePreviewOwner, _runtimePreview as SlotComponent);
			SetPreviewStatus($"运行时已创建：{_runtimePreview.GetType().Name} · 生命周期 {_runtimePreview.Lifecycle}", warning: false);
		}
		catch (Exception ex)
		{
			ResetRuntimePreview();
			SetPreviewStatus("运行时预览失败：" + ex.Message, warning: true);
		}
	}

	private TowerDefenseCharacter InstantiatePreviewCharacter()
	{
		if (_previewWorld == null || _previewCharacterConfig == null)
		{
			return null;
		}
		PackedScene packedScene = null;
		try
		{
			packedScene = ResourceManager.Instance?.GetCharacterScene(_previewCharacterConfig.name);
		}
		catch
		{
		}
		if (packedScene == null)
		{
			return null;
		}
		Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(node);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			node.Free();
			return null;
		}
		towerDefenseCharacter.inGame = false;
		towerDefenseCharacter.editorPreviewMode = true;
		towerDefenseCharacter.config = _previewCharacterConfig;
		XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
		_previewSafety = new XWGameplayLogicPreviewSafety();
		_previewSafety.PrepareCharacter(node);
		_previewSafety.TrackPreviewRoot(node);
		_characterPreview = node;
		_previewWorld.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseCharacter;
	}

	private void OpenPreviewCharacterPicker()
	{
		EnsureResourcePicker();
		int generation = _resourcePickerGeneration;
		_resourcePicker?.Open(XWGameplayResourceKind.Character, _previewCharacterConfig?.name ?? string.Empty, delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _resourcePickerGeneration)
			{
				_previewCharacterConfig = ResourceLoader.Load<TowerDefenseCharacterConfig>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				if (GodotObject.IsInstanceValid(_previewCharacterVisualButton))
				{
					_previewCharacterVisualButton.Text = (string.IsNullOrWhiteSpace(_previewCharacterConfig?.name) ? "选择角色场景" : _previewCharacterConfig.name);
				}
				RebuildCharacterRuntimePreview();
			}
		}, null, lockKind: true, "运行时角色预览");
	}

	private void SimulateComponentEvent()
	{
		if (_runtimePreview == null)
		{
			_runtimePreviewDirty = true;
			EnsureRuntimePreview();
			if (_runtimePreview == null)
			{
				return;
			}
		}
		string text = _eventNameEdit?.Text?.StripEdges() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = _selectedEventName;
		}
		bool flag = !string.IsNullOrWhiteSpace(text) && _runtimePreview.SendStateEvent(text, allowWhenInactive: true);
		if (text.Equals("alive", StringComparison.OrdinalIgnoreCase))
		{
			_runtimePreview.SetAlive(alive: true);
			flag = true;
		}
		else if (text.Equals("dead", StringComparison.OrdinalIgnoreCase))
		{
			_runtimePreview.SetAlive(alive: false);
			flag = true;
		}
		double value = _eventValueEdit?.Value ?? 0.0;
		SetPreviewStatus(flag ? $"事件已模拟：{text} ({value:0.##})" : ("事件未被当前运行时接收：" + text), !flag);
	}

	private void ResetRuntimePreview()
	{
		DisposeRuntimePreview();
		_previewSafety?.Dispose();
		_previewSafety = null;
		if (GodotObject.IsInstanceValid(_characterPreview) && !_characterPreview.IsQueuedForDeletion())
		{
			_characterPreview.QueueFree();
		}
		_characterPreview = null;
		if (GodotObject.IsInstanceValid(_runtimePreviewOwner) && _runtimePreviewOwner != _characterPreview && !_runtimePreviewOwner.IsInsideTree())
		{
			_runtimePreviewOwner.Free();
		}
		_runtimePreviewOwner = null;
		_runtimePreviewManager = null;
	}

	private void DisposeRuntimePreview()
	{
		try
		{
			if (_runtimePreview != null && GodotObject.IsInstanceValid(_runtimePreviewManager))
			{
				_runtimePreviewManager.RemoveRuntimeComponent(_runtimePreview);
			}
			else
			{
				_runtimePreview?.Release();
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Component preview cleanup: " + ex.Message);
		}
		_runtimePreview = null;
	}

	private void OpenResourcePicker(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		EnsureResourcePicker();
		if (_resourcePicker == null)
		{
			return;
		}
		Resource resource = owner.Get(descriptor.Name).As<Resource>();
		string text = (string.IsNullOrWhiteSpace(descriptor.ClassName) ? descriptor.HintString : descriptor.ClassName);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Resource";
		}
		int generation = _resourcePickerGeneration;
		_resourcePicker.OpenResourceLibrary(text, Humanize(descriptor.Name), resource?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { text }, new string[1] { "Resources" }, "res://addons/ModEditor/Icons/Resource.svg", delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _resourcePickerGeneration && IsResourcePickerOwnerActive(owner))
			{
				Resource from = ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				_binding.SetValue(owner, descriptor.Name, Variant.From(in from), "选择 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
			}
		});
	}

	private void OpenArrayResourcePicker(Resource owner, DynamicPropertyDescriptor descriptor, int index, Resource current)
	{
		EnsureResourcePicker();
		if (_resourcePicker == null)
		{
			return;
		}
		string text = ExtractArrayResourceClass(descriptor.HintString);
		int generation = _resourcePickerGeneration;
		_resourcePicker.OpenResourceLibrary(text, $"{Humanize(descriptor.Name)} · 第 {index + 1} 项", current?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { text }, new string[1] { "Resources" }, "res://addons/ModEditor/Icons/Resource.svg", delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _resourcePickerGeneration && IsResourcePickerOwnerActive(owner))
			{
				Resource from = ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				SetArrayElement(owner, descriptor.Name, index, Variant.From(in from));
			}
		});
	}

	private void OpenAudioLibrary(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		EnsureResourcePicker();
		if (_resourcePicker == null)
		{
			return;
		}
		Variant variant = owner.Get(descriptor.Name);
		string currentKey = ((variant.VariantType == Variant.Type.String) ? variant.AsString() : (variant.As<Resource>()?.ResourcePath ?? string.Empty));
		int generation = _resourcePickerGeneration;
		_resourcePicker.Open(XWGameplayResourceKind.Audio, currentKey, delegate(XWGameplayResourceChoice choice)
		{
			if (generation == _resourcePickerGeneration && IsResourcePickerOwnerActive(owner))
			{
				Variant variant2;
				if (descriptor.Type != Variant.Type.String)
				{
					variant2 = Variant.From<Resource>(ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse));
				}
				else
				{
					string from = choice?.Key ?? string.Empty;
					variant2 = Variant.From(in from);
				}
				Variant value = variant2;
				_binding.SetValue(owner, descriptor.Name, value, "选择 " + Humanize(descriptor.Name), this, "RefreshCharacterComponentEditorFromHistory");
			}
		}, null, lockKind: true, "游戏音频库");
	}

	private void OpenTextureLibrary(Resource owner, DynamicPropertyDescriptor descriptor)
	{
		OpenResourcePicker(owner, descriptor with
		{
			ClassName = "Texture2D",
			HintString = "Texture2D"
		});
	}

	private bool IsResourcePickerOwnerActive(Resource owner)
	{
		if (!GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(_layoutRoot))
		{
			return false;
		}
		if (owner == _selectedDefinition)
		{
			return true;
		}
		StateMachineDefinition stateMachineDefinition = _selectedDefinition?.StateMachineDefinition;
		if (owner == stateMachineDefinition)
		{
			return true;
		}
		if (stateMachineDefinition != null)
		{
			foreach (StateMachineStateDefinition state in stateMachineDefinition.States)
			{
				if (owner == state)
				{
					return true;
				}
			}
			foreach (StateMachineTransitionDefinition transition in stateMachineDefinition.Transitions)
			{
				if (owner == transition)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void EnsureResourcePicker()
	{
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker = XWGameplayResourcePickerWindow.Create();
			if (_resourcePicker != null)
			{
				AddChild(_resourcePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void DisconnectStateMachineEditor()
	{
		if (_stateSurface != null)
		{
			_stateSurface.FlushViewportState();
			_stateSurface.SimulationPanel?.BindRuntimeTarget(null);
			_stateSurface.SetWorkbenchActive(active: false);
			_stateSurface.DefinitionChanged -= OnEmbeddedStateMachineChanged;
			_stateSurface.LayoutChanged -= OnEmbeddedStateMachineLayoutChanged;
			_stateSurface.ResourcePickerRequested -= OnStateMachineResourcePickerRequested;
			_stateSurface.GuardEditRequested -= OnEmbeddedGuardEditRequested;
		}
		_stateController?.Dispose();
		_stateUndoAdapter?.Release();
		_stateSurface = null;
		_stateController = null;
		_stateUndoAdapter = null;
		_embeddedStateMachineLayoutIdentity = string.Empty;
	}

	private void DisposeDynamicSurface()
	{
		_resourcePickerGeneration++;
		if (GodotObject.IsInstanceValid(_workbenchPages))
		{
			_preferredWorkbenchPage = _workbenchPages.CurrentTab;
		}
		DisconnectResponsiveSignals();
		DisconnectStateMachineEditor();
		ResetRuntimePreview();
		_showHealthPresenter?.Dispose();
		_showHealthPresenter = null;
		_fireVolleyPresenter?.Dispose();
		_fireVolleyPresenter = null;
		_attackContactPresenter?.Dispose();
		_attackContactPresenter = null;
		_explosionStagePresenter?.Dispose();
		_explosionStagePresenter = null;
		_productionDropPresenter?.Dispose();
		_productionDropPresenter = null;
		_slotPositionPresenter?.Dispose();
		_slotPositionPresenter = null;
		DisposeEnumVisualChoices();
		_binding?.Dispose();
		_binding = null;
		InvalidateComponentTypeLibrary();
		if (GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker.Dismiss();
			_resourcePicker.QueueFree();
		}
		_resourcePicker = null;
		_layoutRoot = null;
		_componentTypeGrid = null;
		_componentLibrarySearch = null;
		_editingResource = null;
		_editingSet = null;
		_selectedDefinition = null;
		_workbenchPages = null;
		_previewViewport = null;
		_eventSimulator = null;
		_eventTypeVisualGrid = null;
		_eventTypePickerPopup = null;
		_eventTypeVisualButton = null;
		_previewCharacterVisualButton = null;
		_runtimePreviewDirty = false;
		_componentLibraryCardsDirty = false;
		_stateMachineSurfaceDirty = false;
	}

	private void DisposeEnumVisualChoices()
	{
		foreach (XWVisualSegmentedOption enumVisualSegment in _enumVisualSegments)
		{
			enumVisualSegment.Dispose();
		}
		foreach (XWVisualOptionGallery enumVisualGallery in _enumVisualGalleries)
		{
			enumVisualGallery.Dispose();
		}
		_enumVisualSegments.Clear();
		_enumVisualGalleries.Clear();
	}

	private void LightweightRuntimeStatus()
	{
		if (_previewStatusLabel != null && _selectedDefinition != null)
		{
			_previewStatusLabel.Text = "预览待提交 · " + _selectedDefinition.GetType().Name;
		}
	}

	private void SetPreviewStatus(string text, bool warning)
	{
		if (_previewStatusLabel != null)
		{
			_previewStatusLabel.Text = text;
			_previewStatusLabel.Modulate = (warning ? new Color("ff8d7a") : new Color("92e6a7"));
		}
	}

	private static Array<CharacterComponentDefinition> CloneComponents(Array<CharacterComponentDefinition> source)
	{
		Array<CharacterComponentDefinition> array = new Array<CharacterComponentDefinition>();
		if (source != null)
		{
			foreach (CharacterComponentDefinition item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	private static Variant CreateDefaultArrayElement(DynamicPropertyDescriptor descriptor, Godot.Collections.Array current)
	{
		Variant.Type type = ((current.Count > 0) ? current[0].VariantType : InferArrayElementType(descriptor.HintString));
		Variant.Type num = type - 1;
		if ((ulong)num <= 5uL)
		{
			switch ((int)num)
			{
			case 0:
				return Variant.From<bool>(false);
			case 1:
				return Variant.From<long>(0L);
			case 2:
				return Variant.From<double>(0.0);
			case 3:
				return Variant.From(in string.Empty);
			case 4:
				return Variant.From<Vector2>(Vector2.Zero);
			case 5:
				return Variant.From<Vector2I>(Vector2I.Zero);
			}
		}
		Variant.Type num2 = type - 20;
		if ((ulong)num2 <= 2uL)
		{
			switch ((int)num2)
			{
			case 1:
				return Variant.From<StringName>(new StringName(string.Empty));
			case 2:
				return Variant.From<NodePath>(new NodePath(string.Empty));
			case 0:
				return Variant.From<Color>(Colors.White);
			}
		}
		return default(Variant);
	}

	private static Variant.Type InferArrayElementType(string hintString)
	{
		if (!long.TryParse((hintString ?? string.Empty).Split(':', 2)[0].Split('/', 2)[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || !Enum.IsDefined(typeof(Variant.Type), result))
		{
			return Variant.Type.Nil;
		}
		return (Variant.Type)result;
	}

	private static string ExtractArrayResourceClass(string hintString)
	{
		string text = hintString ?? string.Empty;
		int num = text.LastIndexOf(':');
		string text2;
		int result;
		if (num < 0)
		{
			text2 = text.Trim();
		}
		else
		{
			string text3 = text;
			result = num + 1;
			text2 = text3.Substring(result, text3.Length - result).Trim();
		}
		string text4 = text2;
		if (!string.IsNullOrWhiteSpace(text4) && !int.TryParse(text4, out result))
		{
			return text4;
		}
		return "Resource";
	}

	private static Variant ParseInlineVariant(string text, Variant fallback)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		Variant.Type variantType = fallback.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 3:
				return Variant.From(in text);
			case 1:
				goto IL_0092;
			case 2:
				goto IL_00ac;
			case 0:
				goto IL_00ca;
			}
		}
		Variant.Type num2 = variantType - 20;
		if ((ulong)num2 <= 2uL)
		{
			switch ((int)num2)
			{
			case 1:
				return Variant.From<StringName>(new StringName(text));
			case 2:
				return Variant.From<NodePath>(new NodePath(text));
			case 0:
				if (Color.HtmlIsValid(text))
				{
					return Variant.From<Color>(Color.FromHtml(text));
				}
				break;
			}
		}
		goto IL_0102;
		IL_0092:
		if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return Variant.From(in result);
		}
		goto IL_0102;
		IL_0102:
		return fallback;
		IL_00ac:
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return Variant.From(in result2);
		}
		goto IL_0102;
		IL_00ca:
		if (bool.TryParse(text, out var result3))
		{
			return Variant.From(in result3);
		}
		goto IL_0102;
	}

	private static Array<string> CloneRemovedIds(Array<string> source)
	{
		Array<string> array = new Array<string>();
		if (source != null)
		{
			foreach (string item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	private static int NextWireIndex(CharacterComponentSet set)
	{
		HashSet<int> hashSet = new HashSet<int>(from item in set.GetFlattenedDefinitions()
			where item != null
			select item.WireIndex);
		for (int num = 0; num <= 128; num++)
		{
			if (!hashSet.Contains(num))
			{
				return num;
			}
		}
		return -1;
	}

	private static HBoxContainer AddPropertyRow(VBoxContainer host, DynamicPropertyDescriptor descriptor)
	{
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			TooltipText = $"{descriptor.Name} · {descriptor.Type} · {descriptor.Hint}"
		};
		hBoxContainer.AddChild(new Label
		{
			Text = Humanize(descriptor.Name),
			CustomMinimumSize = new Vector2(190f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		host.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static void AddSection(VBoxContainer host, string title, string subtitle, Color color)
	{
		Label label = new Label
		{
			Text = title,
			Modulate = color
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		host.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			host.AddChild(new Label
			{
				Text = subtitle,
				Modulate = new Color("8fa4bd")
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static void AddMiniButton(HBoxContainer row, string text, Action action)
	{
		Button button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(34f, 30f)
		};
		button.Pressed += action;
		row.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void SetButtonDisabled(string path, bool disabled)
	{
		Button button = _layoutRoot?.GetNodeOrNull<Button>(path);
		if (button != null)
		{
			button.Disabled = disabled;
		}
	}

	private static TowerDefenseCharacter FindRuntimeCharacter(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is TowerDefenseCharacter result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(child);
			if (towerDefenseCharacter != null)
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private static string FirstNonEmpty(params string[] candidates)
	{
		if (candidates == null)
		{
			return string.Empty;
		}
		foreach (string text in candidates)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return string.Empty;
	}

	private static void ClearChildren(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static void ApplyRangeHint(SpinBox control, DynamicPropertyDescriptor descriptor)
	{
		if (descriptor.Hint == PropertyHint.Range)
		{
			string[] array = descriptor.HintString.Split(',', StringSplitOptions.TrimEntries);
			if (array.Length != 0 && double.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				control.MinValue = result;
			}
			if (array.Length > 1 && double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				control.MaxValue = result2;
			}
			if (array.Length > 2 && double.TryParse(array[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
			{
				control.Step = result3;
			}
		}
	}

	private static (string Label, long Id) ParseEnumOption(string text, int index)
	{
		string[] array = text.Split(':', 2, StringSplitOptions.TrimEntries);
		if (array.Length != 2 || !long.TryParse(array[1], out var result))
		{
			return (Label: text, Id: index);
		}
		return (Label: array[0], Id: result);
	}

	private static (string Label, long Bit) ParseFlagOption(string text, int index)
	{
		string[] array = text.Split(':', 2, StringSplitOptions.TrimEntries);
		long item = 1L << index;
		if (array.Length == 2 && long.TryParse(array[1], out var result))
		{
			item = result;
		}
		return (Label: array[0], Bit: item);
	}

	private static bool LooksLikeAudio(string property, DynamicPropertyDescriptor descriptor)
	{
		if (!property.Contains("audio", StringComparison.OrdinalIgnoreCase) && !descriptor.ClassName.Contains("AudioStream", StringComparison.OrdinalIgnoreCase))
		{
			return descriptor.HintString.Contains("AudioStream", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool LooksLikeTexture(string property, DynamicPropertyDescriptor descriptor)
	{
		if (!property.Contains("texture", StringComparison.OrdinalIgnoreCase) && !descriptor.ClassName.Contains("Texture", StringComparison.OrdinalIgnoreCase))
		{
			return descriptor.HintString.Contains("Texture", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string Humanize(StringName value)
	{
		return Humanize(value.ToString());
	}

	private static string Humanize(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "未命名";
		}
		if (value.StartsWith("Configuration/", StringComparison.Ordinal))
		{
			string text = value;
			int length = "Configuration/".Length;
			value = text.Substring(length, text.Length - length);
		}
		string text2 = value.Replace('_', ' ');
		for (int num = text2.Length - 1; num > 0; num--)
		{
			if (char.IsUpper(text2[num]) && !char.IsWhiteSpace(text2[num - 1]))
			{
				text2 = text2.Insert(num, " ");
			}
		}
		return text2.Trim();
	}

	private static string DisplayResource(Resource resource)
	{
		if (!string.IsNullOrWhiteSpace(resource?.ResourceName))
		{
			return resource.ResourceName;
		}
		return resource?.ResourcePath.GetFile() ?? "资源";
	}

	private static string CompactVariant(Variant value)
	{
		if (value.VariantType == Variant.Type.Object)
		{
			Resource resource = value.As<Resource>();
			if (resource != null)
			{
				return DisplayResource(resource);
			}
		}
		string text = value.ToString();
		if (text.Length <= 72)
		{
			return text;
		}
		return text.Substring(0, 69) + "…";
	}
}
