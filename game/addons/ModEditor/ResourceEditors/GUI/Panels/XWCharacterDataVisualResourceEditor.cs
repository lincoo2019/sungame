using System;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCharacterDataVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterDataEditorLayout.tscn";

	private const string VisualPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterDataVisualPreview.tscn";

	private const double DamagePreviewDurationSeconds = 6.0;

	private static PackedScene _editorLayoutScene;

	private static PackedScene _visualPreviewScene;

	private static readonly TowerDefenseEnum.ARMOR_METHOD_FLAGS[] ArmorMethodFlagValues = Enum.GetValues<TowerDefenseEnum.ARMOR_METHOD_FLAGS>();

	private static readonly string[] DerivedCacheProperties = new string[10] { "armorDictionary", "fliterAllDictionary", "fliterOpenDictionary", "fliterCloseDictionary", "customDictionary", "fliterOpenAll", "fliterCloseAll", "damagePointDictionary", "fliterOpenAll", "fliterCloseAll" };

	private Resource _editingData;

	private int _selectedIndex = -1;

	private ItemList _entryList;

	private VBoxContainer _entryEditorHost;

	private Label _summaryLabel;

	private Label _cacheSummaryLabel;

	private SubViewportContainer _previewViewportContainer;

	private Node2D _previewRoot;

	private Label _previewMissingLabel;

	private Control _configurationPreviewCanvas;

	private Control _fallbackRibbon;

	private Label _fallbackRibbonLabel;

	private Label _previewHealthValueLabel;

	private Label _previewPlaybackStateLabel;

	private TowerDefenseCharacter _runtimeCharacterPreview;

	private TowerDefenseCharacterConfig _previewOwnerConfig;

	private HSlider _previewHealthSlider;

	private Label _previewStatusLabel;

	private Button _previewPlayPauseButton;

	private Button _previewResetButton;

	private SubViewport _previewViewport;

	private double _previewHealthRatio = 1.0;

	private double _damagePreviewElapsed;

	private bool _damagePreviewPlaying;

	private bool _draggingPreview;

	private Vector2 _previewPan;

	private float _previewZoom = 1f;

	private bool _updatingControls;

	private XWVisualPropertyBinding _metadataBinding;

	private XWVisualPropertyBinding _entryBinding;

	private XWGameplayResourcePickerWindow _audioPicker;

	private Resource _audioTargetResource;

	private string _audioTargetProperty = "";

	private int _selectedCustomTextureIndex = -1;

	private int _selectedArmorStageIndex = -1;

	public int RuntimeCharacterPreviewBuildCount { get; private set; }

	public int RuntimeCharacterPreviewApplyCount { get; private set; }

	public int DamagePreviewProcessTickCount { get; private set; }

	public bool IsDamagePreviewPlaying => _damagePreviewPlaying;

	public double PreviewHealthRatio => _previewHealthRatio;

	public TowerDefenseCharacter CurrentRuntimeCharacterPreview => _runtimeCharacterPreview;

	public bool IsCharacterDataPreviewRendering
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

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		if (_damagePreviewPlaying)
		{
			DamagePreviewProcessTickCount++;
			_damagePreviewElapsed = Math.Min(6.0, _damagePreviewElapsed + Math.Max(0.0, delta));
			_previewHealthRatio = Mathf.Clamp(1.0 - _damagePreviewElapsed / 6.0, 0.0, 1.0);
			ApplyPreviewHealthRatio();
			if (_damagePreviewElapsed >= 6.0)
			{
				SetDamagePreviewPlaying(playing: false, "受击演示完成 · 点击重播");
			}
		}
	}

	public override void _ExitTree()
	{
		DisposeCharacterDataBindings();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		if (!IsSupportedCharacterDataResource(base.CurrentResource))
		{
			DisposeCharacterDataBindings();
			return;
		}
		_editingData = base.CurrentResource;
		ResetCharacterDataBindings();
		if (IsAggregateCharacterData(base.CurrentResource))
		{
			RenderCharacterDataEditor(base.CurrentResource);
		}
		else
		{
			RenderCharacterDataItemEditor(base.CurrentResource);
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteCharacterDataVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteCharacterDataVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(CharacterArmorData)) && !(type == typeof(CharacterCustomData)) && !(type == typeof(CharacterDamagePointData)) && !(type == typeof(ArmorSlotConfig)) && !(type == typeof(CharacterCustomConfig)) && !(type == typeof(CharacterDamagePointConfig)))
		{
			return type == typeof(TowerDefenseArmorTypeData);
		}
		return true;
	}

	private void ResetCharacterDataBindings()
	{
		ReleaseCharacterDataPreviewBindings();
		_metadataBinding?.Dispose();
		_entryBinding?.Dispose();
		_metadataBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCharacterDataPropertyEdited);
		_entryBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCharacterDataPropertyEdited);
		_entryList = null;
		_entryEditorHost = null;
	}

	private void DisposeCharacterDataBindings()
	{
		ReleaseCharacterDataPreviewBindings();
		_metadataBinding?.Dispose();
		_entryBinding?.Dispose();
		_metadataBinding = null;
		_entryBinding = null;
		_editingData = null;
		_audioTargetResource = null;
		_audioTargetProperty = "";
		_entryList = null;
		_entryEditorHost = null;
	}

	private void ReleaseCharacterDataPreviewBindings()
	{
		SetDamagePreviewPlaying(playing: false, "");
		if (GodotObject.IsInstanceValid(_previewViewport))
		{
			_previewViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
		}
		_runtimeCharacterPreview = null;
		_previewOwnerConfig = null;
		_previewViewportContainer = null;
		_previewViewport = null;
		_previewRoot = null;
		_previewMissingLabel = null;
		_configurationPreviewCanvas = null;
		_fallbackRibbon = null;
		_fallbackRibbonLabel = null;
		_previewHealthValueLabel = null;
		_previewHealthSlider = null;
		_previewStatusLabel = null;
		_previewPlayPauseButton = null;
		_previewResetButton = null;
		_previewPlaybackStateLabel = null;
	}

	private void BindCharacterDataMetadata(VBoxContainer root, Resource resource)
	{
		if (GodotObject.IsInstanceValid(root) && GodotObject.IsInstanceValid(resource))
		{
			LineEdit nodeOrNull = root.GetNodeOrNull<LineEdit>("%ResourceNameEdit");
			CheckButton nodeOrNull2 = root.GetNodeOrNull<CheckButton>("%LocalToSceneCheck");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				_metadataBinding.BindText(nodeOrNull, resource, "resource_name", LightweightCharacterDataPreview, this, "RefreshCharacterDataEditorFromHistory");
			}
			if (GodotObject.IsInstanceValid(nodeOrNull2))
			{
				_metadataBinding.BindToggle(nodeOrNull2, resource, "resource_local_to_scene", LightweightCharacterDataPreview, this, "RefreshCharacterDataEditorFromHistory");
			}
		}
	}

	private void OnCharacterDataPropertyEdited(bool committed)
	{
		if (committed)
		{
			NormalizeCharacterData(_editingData);
			RefreshEntryList();
			if (IsAggregateCharacterData(_editingData))
			{
				RenderSelectedEntryEditor();
			}
			UpdateSummary();
		}
		else
		{
			LightweightCharacterDataPreview();
		}
		NotifyCurrentResourceEdited();
	}

	private void LightweightCharacterDataPreview()
	{
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = BuildDataSummary(_editingData);
		}
		UpdateVisualPreview();
	}

	private void RenderCharacterDataItemEditor(VBoxContainer host, Resource resource)
	{
		if (!GodotObject.IsInstanceValid(host) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		if (!(resource is ArmorSlotConfig slot))
		{
			if (!(resource is CharacterCustomConfig config))
			{
				if (!(resource is CharacterDamagePointConfig config2))
				{
					if (resource is TowerDefenseArmorTypeData data)
					{
						CreateArmorTypeEditor(host, data);
					}
				}
				else
				{
					CreateDamagePointConfigEditor(host, resource, config2);
				}
			}
			else
			{
				CreateCustomConfigEditor(host, resource, config);
			}
		}
		else
		{
			CreateArmorSlotEditor(host, resource, slot);
		}
	}

	private void RenderCharacterDataItemEditor(Resource resource)
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_editorLayoutScene == null)
		{
			_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterDataEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			return;
		}
		base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_summaryLabel = vBoxContainer.GetNodeOrNull<Label>("%SummaryLabel");
		_cacheSummaryLabel = vBoxContainer.GetNodeOrNull<Label>("%CacheSummaryLabel");
		BindCharacterDataMetadata(vBoxContainer, resource);
		Control nodeOrNull = vBoxContainer.GetNodeOrNull<Control>("%VisualPreview");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			BindCharacterDataVisualPreview(nodeOrNull, resource);
		}
		else
		{
			nodeOrNull = CreateCharacterDataVisualPreview(resource);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				vBoxContainer.AddChild(nodeOrNull, forceReadableName: false, InternalMode.Disabled);
			}
		}
		Control nodeOrNull2 = vBoxContainer.GetNodeOrNull<Control>("%EditorSplit");
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.Visible = false;
		}
		_entryEditorHost = vBoxContainer.GetNodeOrNull<VBoxContainer>("%StandaloneEditorHost") ?? vBoxContainer.GetNodeOrNull<VBoxContainer>("%EntryEditorHost");
		RenderCharacterDataItemEditor(_entryEditorHost, resource);
		base.PreviewList?.AddItem("资源类型: " + resource.GetType().Name);
		base.PreviewList?.AddItem("所有可配置属性都在当前游戏化界面直接编辑。");
		base.GraphList?.AddItem(resource.GetType().Name + " -> 运行状态模拟 -> 角色表现");
		UpdateSummary();
		UpdateVisualPreview();
	}

	private void RenderCharacterDataEditor(Resource resource)
	{
		if (base.CanvasGrid == null || resource == null)
		{
			return;
		}
		NormalizeCharacterData(resource);
		ClampSelectedIndex(resource);
		base.CanvasGrid.Columns = 1;
		if (_editorLayoutScene == null)
		{
			_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterDataEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			return;
		}
		base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_summaryLabel = vBoxContainer.GetNode<Label>("%SummaryLabel");
		_cacheSummaryLabel = vBoxContainer.GetNode<Label>("%CacheSummaryLabel");
		BindCharacterDataMetadata(vBoxContainer, resource);
		BindCharacterDataVisualPreview(vBoxContainer.GetNode<Control>("%VisualPreview"), resource);
		if (!(resource is CharacterArmorData armorData))
		{
			if (!(resource is CharacterCustomData customData))
			{
				if (resource is CharacterDamagePointData damagePointData)
				{
					RenderDamagePointDataEditor(vBoxContainer, damagePointData);
				}
			}
			else
			{
				RenderCustomDataEditor(vBoxContainer, customData);
			}
		}
		else
		{
			RenderArmorDataEditor(vBoxContainer, armorData);
		}
		RefreshEntryList();
		RenderSelectedEntryEditor();
		UpdateSummary();
		UpdateVisualPreview();
	}

	private Control CreateCharacterDataVisualPreview(Resource resource)
	{
		if (_visualPreviewScene == null)
		{
			_visualPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterDataVisualPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_visualPreviewScene))
		{
			return null;
		}
		Control control = _visualPreviewScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		BindCharacterDataVisualPreview(control, resource);
		return control;
	}

	private void BindCharacterDataVisualPreview(Control panel, Resource resource)
	{
		_previewStatusLabel = panel.GetNode<Label>("Layout/Header/Status");
		_previewHealthSlider = panel.GetNode<HSlider>("Layout/HealthRow/Slider");
		_previewViewportContainer = panel.GetNode<SubViewportContainer>("%ViewportContainer");
		_previewViewport = panel.GetNode<SubViewport>("%CharacterViewport");
		_previewRoot = panel.GetNode<Node2D>("%PreviewRoot");
		_previewMissingLabel = panel.GetNode<Label>("%MissingLabel");
		_fallbackRibbon = panel.GetNodeOrNull<Control>("%FallbackRibbon");
		_fallbackRibbonLabel = panel.GetNodeOrNull<Label>("%FallbackRibbonLabel");
		_previewHealthValueLabel = panel.GetNodeOrNull<Label>("%HealthValueLabel");
		_previewPlaybackStateLabel = panel.GetNodeOrNull<Label>("%PlaybackStateLabel");
		_previewPlayPauseButton = panel.GetNodeOrNull<Button>("%DamagePlayPauseButton");
		_previewResetButton = panel.GetNodeOrNull<Button>("%DamageResetButton");
		if (_previewRoot.GetParent() is Control control)
		{
			_configurationPreviewCanvas = new Control
			{
				Name = "CharacterDataConfigurationPreview",
				Position = Vector2.Zero,
				Size = new Vector2(900f, 300f),
				MouseFilter = MouseFilterEnum.Ignore,
				Visible = false
			};
			_configurationPreviewCanvas.Draw += delegate
			{
				DrawCharacterDataPreview(_configurationPreviewCanvas);
			};
			control.AddChild(_configurationPreviewCanvas, forceReadableName: false, InternalMode.Disabled);
		}
		_previewHealthSlider.Value = _previewHealthRatio * 100.0;
		_previewHealthSlider.Editable = !(resource is CharacterCustomData) && !(resource is CharacterCustomConfig);
		_previewHealthSlider.ValueChanged += delegate(double value)
		{
			_previewHealthRatio = Mathf.Clamp(value / 100.0, 0.0, 1.0);
			_damagePreviewElapsed = (1.0 - _previewHealthRatio) * 6.0;
			if (GodotObject.IsInstanceValid(_previewHealthValueLabel))
			{
				_previewHealthValueLabel.Text = $"{_previewHealthRatio:P0}";
			}
			UpdateVisualPreview();
		};
		if (GodotObject.IsInstanceValid(_previewPlayPauseButton))
		{
			_previewPlayPauseButton.Pressed += ToggleDamagePreview;
		}
		if (GodotObject.IsInstanceValid(_previewResetButton))
		{
			_previewResetButton.Pressed += ResetDamagePreview;
		}
		_previewViewportContainer.GuiInput += OnRuntimePreviewGuiInput;
		ApplyCharacterDataPreviewVisibility(IsVisibleInTree());
		UpdateDamagePreviewControls("拖动生命值，或播放一次完整受击过程");
		RebuildRuntimeCharacterPreview(resource);
	}

	private void RenderArmorDataEditor(VBoxContainer root, CharacterArmorData armorData)
	{
		root.TooltipText = $"armorList: {armorData.armorList?.Count ?? 0}";
		BindSplitEditorPanel(root, "护甲槽", "护甲槽决定动画替换方式、挂点、偏移和受伤阶段触发阈值。", delegate
		{
			AddArmorSlot(armorData);
		}, RemoveSelectedArmorSlot);
	}

	private void RenderCustomDataEditor(VBoxContainer root, CharacterCustomData customData)
	{
		root.TooltipText = $"customList: {customData.customList?.Count ?? 0}";
		BindSplitEditorPanel(root, "自定义外观", "自定义外观用于解锁皮肤、切换动画滤镜，以及按受伤阶段替换贴图。", delegate
		{
			AddCustomConfig(customData);
		}, RemoveSelectedCustomConfig);
	}

	private void RenderDamagePointDataEditor(VBoxContainer root, CharacterDamagePointData damagePointData)
	{
		root.TooltipText = $"damagePointList: {damagePointData.damagePointList?.Count ?? 0}";
		BindSplitEditorPanel(root, "受伤点", "受伤点按生命百分比从高到低触发，可切换动画滤镜、替换部件和播放受伤特效。", delegate
		{
			AddDamagePoint(damagePointData);
		}, RemoveSelectedDamagePoint);
	}

	private void BindSplitEditorPanel(VBoxContainer root, string title, string description, Action addPressed, Action removePressed)
	{
		root.GetNode<Label>("%EntryTitle").Text = title;
		root.GetNode<Label>("%EntryDescription").Text = description;
		root.GetNode<Button>("%AddButton").Pressed += delegate
		{
			addPressed?.Invoke();
		};
		root.GetNode<Button>("%RemoveButton").Pressed += delegate
		{
			removePressed?.Invoke();
		};
		if (root.GetNode<Button>("%AddButton").GetParent() is Container container)
		{
			container.AddChild(CreateToolbarButton("↑", delegate
			{
				MoveSelectedAggregateEntry(-1);
			}), forceReadableName: false, InternalMode.Disabled);
			container.AddChild(CreateToolbarButton("↓", delegate
			{
				MoveSelectedAggregateEntry(1);
			}), forceReadableName: false, InternalMode.Disabled);
		}
		_entryList = root.GetNode<ItemList>("%EntryList");
		_entryList.ItemSelected += delegate(long index)
		{
			SelectEntry((int)index);
		};
		_entryEditorHost = root.GetNodeOrNull<VBoxContainer>("%EntryEditorContentHost") ?? root.GetNode<VBoxContainer>("%EntryEditorHost");
	}

	private void RefreshEntryList()
	{
		if (!GodotObject.IsInstanceValid(_entryList))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_entryList.Clear();
			int entryCount = GetEntryCount(_editingData);
			if (entryCount == 0)
			{
				_entryList.AddItem("未配置");
				return;
			}
			for (int i = 0; i < entryCount; i++)
			{
				_entryList.AddItem(BuildEntryTitle(_editingData, i));
			}
			ClampSelectedIndex(_editingData);
			if (_selectedIndex >= 0 && _selectedIndex < _entryList.ItemCount)
			{
				_entryList.Select(_selectedIndex);
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void SelectEntry(int index)
	{
		if (!_updatingControls)
		{
			if (GetEntryCount(_editingData) == 0)
			{
				_selectedIndex = -1;
				RenderSelectedEntryEditor();
				UpdateVisualPreview();
			}
			else
			{
				_selectedIndex = index;
				ClampSelectedIndex(_editingData);
				RenderSelectedEntryEditor();
				UpdateVisualPreview();
			}
		}
	}

	private void RenderSelectedEntryEditor()
	{
		if (!GodotObject.IsInstanceValid(_entryEditorHost))
		{
			return;
		}
		_entryBinding?.Dispose();
		_entryBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCharacterDataPropertyEdited);
		foreach (Node child in _entryEditorHost.GetChildren())
		{
			child.QueueFree();
		}
		if (_editingData is CharacterArmorData characterArmorData)
		{
			ArmorSlotConfig armorSlot = GetArmorSlot(characterArmorData);
			if (armorSlot == null)
			{
				_entryEditorHost.AddChild(CreateEmptyEntryLabel("还没有护甲槽。点击左侧添加后编辑。"), forceReadableName: false, InternalMode.Disabled);
			}
			else
			{
				CreateArmorSlotEditor(_entryEditorHost, characterArmorData, armorSlot);
			}
		}
		else if (_editingData is CharacterCustomData characterCustomData)
		{
			CharacterCustomConfig customConfig = GetCustomConfig(characterCustomData);
			if (customConfig == null)
			{
				_entryEditorHost.AddChild(CreateEmptyEntryLabel("还没有自定义外观。点击左侧添加后编辑。"), forceReadableName: false, InternalMode.Disabled);
			}
			else
			{
				CreateCustomConfigEditor(_entryEditorHost, characterCustomData, customConfig);
			}
		}
		else if (_editingData is CharacterDamagePointData characterDamagePointData)
		{
			CharacterDamagePointConfig damagePointConfig = GetDamagePointConfig(characterDamagePointData);
			if (damagePointConfig == null)
			{
				_entryEditorHost.AddChild(CreateEmptyEntryLabel("还没有受伤点。点击左侧添加后编辑。"), forceReadableName: false, InternalMode.Disabled);
			}
			else
			{
				CreateDamagePointConfigEditor(_entryEditorHost, characterDamagePointData, damagePointConfig);
			}
		}
	}

	private void CreateArmorSlotEditor(VBoxContainer host, Resource owner, ArmorSlotConfig slot)
	{
		host.AddChild(CreateSectionTitle($"护甲槽 {_selectedIndex + 1}"), forceReadableName: false, InternalMode.Disabled);
		AddResourceMetadataFields(host, slot);
		host.AddChild(AddBoundLineEdit("ArmorNameLineEdit", "armorName", slot, "armorName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddOptionField("ArmorReplaceMethodOption", "replaceMethod", slot.replaceMethod, new string[2] { "Media", "Sprite" }, delegate(string value)
		{
			SetEntryProperty(owner, slot, "replaceMethod", Variant.From(in value));
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("ArmorReplaceMediaLineEdit", "replaceMediaName", slot, "replaceMediaName", (string value) => Variant.From<StringName>(new StringName(value))), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("ArmorSlotPathLineEdit", "slotPath", slot, "slotPath", (string value) => Variant.From<NodePath>(new NodePath(value))), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundVector2Field("ArmorOffset", "offset", slot, "offset"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("ArmorRotationSpinBox", "rotation", slot, "rotation", -3600.0, 3600.0, 0.1), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundVector2Field("ArmorScale", "scale", slot, "scale"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("ArmorDamagePointSpinBox", "damagePoint", slot, "damagePoint", -1.0, 1.0, 0.01), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("ArmorOpenFilterText", "openFliter", slot, "openFliter"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("ArmorCloseFilterText", "closeFliter", slot, "closeFliter"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("ArmorDestroyFilterText", "destroyFliter", slot, "destroyFliter"), forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateCustomConfigEditor(VBoxContainer host, Resource owner, CharacterCustomConfig config)
	{
		host.AddChild(CreateSectionTitle($"自定义外观 {_selectedIndex + 1}"), forceReadableName: false, InternalMode.Disabled);
		AddResourceMetadataFields(host, config);
		host.AddChild(AddBoundLineEdit("CustomOpenKeyLineEdit", "openKey", config, "openKey"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("CustomNameLineEdit", "customName", config, "customName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddOptionField("CustomTypeOption", "type", config.type, new string[2] { "White", "Gold" }, delegate(string value)
		{
			SetEntryProperty(owner, config, "type", Variant.From(in value));
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("CustomHandbookNameLineEdit", "customHandbookName", config, "customHandbookName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("CustomHandbookAccessLineEdit", "customHandbookAccess", config, "customHandbookAccess"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("CustomHandbookStoryText", "customHandbookStory", config, "customHandbookStory"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("CustomFilterOpenText", "animeFliterOpen", config, "animeFliterOpen"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("CustomFilterCloseText", "animeFliterClose", config, "animeFliterClose"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("CustomDamageMediaLineEdit", "damagePointChangeMediaName", config, "damagePointChangeMediaName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(CreateCustomTextureArrayEditor(config), forceReadableName: false, InternalMode.Disabled);
	}

	private Control CreateCustomTextureArrayEditor(CharacterCustomConfig config)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "CustomTextureArrayEditor",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = "damagePointChangeMediaTexturePaths · 受伤阶段贴图",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("＋", delegate
		{
			AddCustomTexture(config);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("－", delegate
		{
			RemoveSelectedCustomTexture(config);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("←", delegate
		{
			MoveSelectedCustomTexture(config, -1);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("→", delegate
		{
			MoveSelectedCustomTexture(config, 1);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Array<string> array = config.damagePointChangeMediaTexturePaths ?? new Array<string>();
		if (array.Count == 0)
		{
			hBoxContainer2.AddChild(CreateEmptyEntryLabel("点击＋添加第一张阶段贴图"), forceReadableName: false, InternalMode.Disabled);
		}
		for (int num = 0; num < array.Count; num++)
		{
			int index = num;
			VBoxContainer vBoxContainer2 = new VBoxContainer
			{
				CustomMinimumSize = new Vector2(142f, 96f)
			};
			Button button = new Button
			{
				Text = $"阶段 {num + 1}",
				ToggleMode = true,
				ButtonPressed = (num == _selectedCustomTextureIndex)
			};
			button.Pressed += delegate
			{
				_selectedCustomTextureIndex = index;
				RenderSelectedEntryEditor();
			};
			vBoxContainer2.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
			xWResourcePicker.Name = $"CustomTexturePicker{num}";
			xWResourcePicker.Setup("Texture2D");
			xWResourcePicker.SetEditedResource(LoadTexturePreview(array[num]));
			xWResourcePicker.ResourceChanged += delegate(Resource resource)
			{
				SetCustomTexture(config, index, resource?.ResourcePath ?? string.Empty);
			};
			vBoxContainer2.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer2.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private void AddCustomTexture(CharacterCustomConfig config)
	{
		Array<string> array = CopyTexturePaths(config?.damagePointChangeMediaTexturePaths);
		array.Add(string.Empty);
		_selectedCustomTextureIndex = array.Count - 1;
		_entryBinding.SetValue(config, "damagePointChangeMediaTexturePaths", array, "添加外观阶段贴图", this, "RefreshCharacterDataEditorFromHistory");
	}

	private void RemoveSelectedCustomTexture(CharacterCustomConfig config)
	{
		Array<string> array = CopyTexturePaths(config?.damagePointChangeMediaTexturePaths);
		if (_selectedCustomTextureIndex >= 0 && _selectedCustomTextureIndex < array.Count)
		{
			array.RemoveAt(_selectedCustomTextureIndex);
			_selectedCustomTextureIndex = Mathf.Min(_selectedCustomTextureIndex, array.Count - 1);
			_entryBinding.SetValue(config, "damagePointChangeMediaTexturePaths", array, "删除外观阶段贴图", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void MoveSelectedCustomTexture(CharacterCustomConfig config, int direction)
	{
		Array<string> array = CopyTexturePaths(config?.damagePointChangeMediaTexturePaths);
		int num = Mathf.Clamp(_selectedCustomTextureIndex + direction, 0, array.Count - 1);
		if (_selectedCustomTextureIndex >= 0 && num != _selectedCustomTextureIndex)
		{
			string item = array[_selectedCustomTextureIndex];
			array.RemoveAt(_selectedCustomTextureIndex);
			array.Insert(num, item);
			_selectedCustomTextureIndex = num;
			_entryBinding.SetValue(config, "damagePointChangeMediaTexturePaths", array, "调整外观阶段贴图顺序", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void SetCustomTexture(CharacterCustomConfig config, int index, string texturePath)
	{
		Array<string> array = CopyTexturePaths(config?.damagePointChangeMediaTexturePaths);
		if (index >= 0 && index < array.Count)
		{
			array[index] = texturePath ?? string.Empty;
			_entryBinding.SetValue(config, "damagePointChangeMediaTexturePaths", array, "更换外观阶段贴图", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private static Array<string> CopyTexturePaths(Array<string> source)
	{
		if (source != null)
		{
			return new Array<string>(source);
		}
		return new Array<string>();
	}

	private void CreateDamagePointConfigEditor(VBoxContainer host, Resource owner, CharacterDamagePointConfig config)
	{
		host.AddChild(CreateSectionTitle($"受伤点 {_selectedIndex + 1}"), forceReadableName: false, InternalMode.Disabled);
		AddResourceMetadataFields(host, config);
		host.AddChild(AddBoundLineEdit("DamagePointNameLineEdit", "damagePointName", config, "damagePointName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("DamagePercentageSpinBox", "damagePersontage", config, "damagePersontage", 0.0, 1.0, 0.01), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddResourcePicker("DamageEffectPicker", "animeEffect", "PackedScene", config.animeEffect, delegate(Resource value)
		{
			SetEntryProperty(owner, config, "animeEffect", Variant.From(in value));
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundVector2Field("DamageEffectOffset", "animeEffectOffset", config, "animeEffectOffset"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoolField("DamageDropCheck", "isDrop", config.isDrop, delegate(bool value)
		{
			SetEntryProperty(owner, config, "isDrop", Variant.From(in value));
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("DamageFilterOpenText", "animeFliterOpen", config, "animeFliterOpen"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundTextEdit("DamageFilterCloseText", "animeFliterClose", config, "animeFliterClose"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundLineEdit("DamageReplaceMediaLineEdit", "replaceMediaName", config, "replaceMediaName", (string value) => Variant.From<StringName>(new StringName(value))), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddResourcePicker("DamageReplaceTexturePicker", "replaceMediaTexturePath", "Texture2D", LoadTexturePreview(config.replaceMediaTexturePath), delegate(Resource value)
		{
			XWCharacterDataVisualResourceEditor xWCharacterDataVisualResourceEditor = this;
			Resource owner2 = owner;
			CharacterDamagePointConfig entry = config;
			string from = value?.ResourcePath ?? string.Empty;
			xWCharacterDataVisualResourceEditor.SetEntryProperty(owner2, entry, "replaceMediaTexturePath", Variant.From(in from));
		}), forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(AddBoundLineEdit("DamageAudioLineEdit", "damageAudio", config, "damageAudio"), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("游戏音频图鉴", delegate
		{
			_audioTargetResource = config;
			ShowAudioSelector("damageAudio");
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateArmorTypeEditor(VBoxContainer host, TowerDefenseArmorTypeData data)
	{
		if (!GodotObject.IsInstanceValid(host) || !GodotObject.IsInstanceValid(data))
		{
			return;
		}
		host.AddChild(CreateSectionTitle("护甲类型 · 阶段与受击行为"), forceReadableName: false, InternalMode.Disabled);
		AddResourceMetadataFields(host, data);
		host.AddChild(AddBoundLineEdit("ArmorTypeNameLineEdit", "armorName", data, "armorName"), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("ArmorTypeDamagePoint", "damagePoint", data, "damagePoint", 0.0, 100000000.0, 1.0), forceReadableName: false, InternalMode.Disabled);
		OptionButton optionButton = new OptionButton
		{
			Name = "ArmorHeightOption",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		TowerDefenseEnum.CHARACTER_HEIGHT[] values = Enum.GetValues<TowerDefenseEnum.CHARACTER_HEIGHT>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.CHARACTER_HEIGHT id = values[i];
			optionButton.AddItem(id.ToString(), (int)id);
		}
		optionButton.Select((int)data.height);
		optionButton.ItemSelected += delegate(long index)
		{
			SetEntryProperty(data, data, "height", Variant.From<int>((int)index));
		};
		host.AddChild(WrapField("height", optionButton), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(CreateArmorStageCard(data), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("ArmorLimitMaxHit", "limitMaxHit", data, "limitMaxHit", -1.0, 100000000.0, 1.0), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(AddBoundSpinBox("ArmorExplodePercentage", "explodePersontage", data, "explodePersontage", 0.0, 10.0, 0.01), forceReadableName: false, InternalMode.Disabled);
		FlowContainer flowContainer = new FlowContainer
		{
			Name = "ArmorMethodFlags",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		flowContainer.AddChild(new Label
		{
			Text = "ARMOR_METHOD_FLAGS",
			CustomMinimumSize = new Vector2(168f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseEnum.ARMOR_METHOD_FLAGS[] armorMethodFlagValues = ArmorMethodFlagValues;
		for (int i = 0; i < armorMethodFlagValues.Length; i++)
		{
			TowerDefenseEnum.ARMOR_METHOD_FLAGS flag = armorMethodFlagValues[i];
			CheckButton checkButton = new CheckButton
			{
				Text = flag.ToString(),
				ButtonPressed = (((uint)data.armorMethodFlags & (uint)flag) != 0)
			};
			checkButton.Toggled += delegate(bool enabled)
			{
				ToggleArmorMethodFlag(data, flag, enabled);
			};
			flowContainer.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		}
		host.AddChild(flowContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(CreateToolbarButton("受击音效 · " + EmptyToPlaceholder(data.impactAudio), delegate
		{
			_audioTargetResource = data;
			ShowAudioSelector("impactAudio");
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("破坏音效 · " + EmptyToPlaceholder(data.damageAudio), delegate
		{
			_audioTargetResource = data;
			ShowAudioSelector("damageAudio");
		}), forceReadableName: false, InternalMode.Disabled);
		host.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private Control CreateArmorStageCard(TowerDefenseArmorTypeData data)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "ArmorStageCards",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = "stagePersontage / stageAnimeTexturePaths",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("＋", delegate
		{
			AddArmorStage(data);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("－", delegate
		{
			RemoveArmorStage(data);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("←", delegate
		{
			MoveArmorStage(data, -1);
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("→", delegate
		{
			MoveArmorStage(data, 1);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = "ArmorBaseTexturePicker";
		xWResourcePicker.Setup("Texture2D");
		xWResourcePicker.SetEditedResource((data.stageAnimeTexturePaths != null && data.stageAnimeTexturePaths.Count > 0) ? LoadTexturePreview(data.stageAnimeTexturePaths[0]) : null);
		xWResourcePicker.ResourceChanged += delegate(Resource resource)
		{
			SetArmorStageTexture(data, 0, resource?.ResourcePath ?? string.Empty);
		};
		vBoxContainer.AddChild(WrapField("初始外观", xWResourcePicker), forceReadableName: false, InternalMode.Disabled);
		int num = data.stagePersontage?.Count ?? 0;
		for (int num2 = 0; num2 < num; num2++)
		{
			int index = num2;
			HBoxContainer hBoxContainer2 = new HBoxContainer
			{
				CustomMinimumSize = new Vector2(0f, 42f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			Button button = new Button
			{
				Text = $"阶段 {num2 + 1}",
				ToggleMode = true,
				ButtonPressed = (num2 == _selectedArmorStageIndex)
			};
			button.Pressed += delegate
			{
				_selectedArmorStageIndex = index;
				RefreshCharacterDataEditorFromHistory();
			};
			hBoxContainer2.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			SpinBox threshold = new SpinBox
			{
				MinValue = 0.0,
				MaxValue = 1.0,
				Step = 0.01,
				Value = data.stagePersontage[num2],
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			threshold.FocusEntered += delegate
			{
				_entryBinding.BeginEdit(data, "stagePersontage");
			};
			threshold.ValueChanged += delegate(double value)
			{
				PreviewArmorStageThreshold(data, index, value);
			};
			threshold.FocusExited += delegate
			{
				CommitArmorStageThreshold(data, index, threshold.Value);
			};
			hBoxContainer2.AddChild(threshold, forceReadableName: false, InternalMode.Disabled);
			XWResourcePicker xWResourcePicker2 = XWResourcePicker.Create();
			xWResourcePicker2.Setup("Texture2D");
			int textureIndex = num2 + 1;
			xWResourcePicker2.SetEditedResource((data.stageAnimeTexturePaths != null && textureIndex < data.stageAnimeTexturePaths.Count) ? LoadTexturePreview(data.stageAnimeTexturePaths[textureIndex]) : null);
			xWResourcePicker2.ResourceChanged += delegate(Resource resource)
			{
				SetArmorStageTexture(data, textureIndex, resource?.ResourcePath ?? string.Empty);
			};
			hBoxContainer2.AddChild(xWResourcePicker2, forceReadableName: false, InternalMode.Disabled);
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		}
		return vBoxContainer;
	}

	private void AddArmorStage(TowerDefenseArmorTypeData data)
	{
		Array<double> array = CopyThresholds(data?.stagePersontage);
		Array<string> array2 = CopyTexturePaths(data?.stageAnimeTexturePaths);
		array.Add(0.5);
		array2.Add(string.Empty);
		_selectedArmorStageIndex = array.Count - 1;
		StringName refreshMethod = "RefreshCharacterDataEditorFromHistory";
		SetArmorStageArrays(data, array, array2, "添加护甲阶段", refreshMethod);
	}

	private void RemoveArmorStage(TowerDefenseArmorTypeData data)
	{
		Array<double> array = CopyThresholds(data?.stagePersontage);
		Array<string> array2 = CopyTexturePaths(data?.stageAnimeTexturePaths);
		if (_selectedArmorStageIndex >= 0 && _selectedArmorStageIndex < array.Count)
		{
			array.RemoveAt(_selectedArmorStageIndex);
			int num = _selectedArmorStageIndex + 1;
			if (num < array2.Count)
			{
				array2.RemoveAt(num);
			}
			_selectedArmorStageIndex = Mathf.Min(_selectedArmorStageIndex, array.Count - 1);
			StringName refreshMethod = "RefreshCharacterDataEditorFromHistory";
			SetArmorStageArrays(data, array, array2, "删除护甲阶段", refreshMethod);
		}
	}

	private void MoveArmorStage(TowerDefenseArmorTypeData data, int direction)
	{
		Array<double> array = CopyThresholds(data?.stagePersontage);
		Array<string> array2 = CopyTexturePaths(data?.stageAnimeTexturePaths);
		int num = Mathf.Clamp(_selectedArmorStageIndex + direction, 0, array.Count - 1);
		if (_selectedArmorStageIndex >= 0 && num != _selectedArmorStageIndex)
		{
			double item = array[_selectedArmorStageIndex];
			array.RemoveAt(_selectedArmorStageIndex);
			array.Insert(num, item);
			int num2 = _selectedArmorStageIndex + 1;
			int a = num + 1;
			if (num2 < array2.Count)
			{
				string item2 = array2[num2];
				array2.RemoveAt(num2);
				array2.Insert(Mathf.Min(a, array2.Count), item2);
			}
			_selectedArmorStageIndex = num;
			StringName refreshMethod = "RefreshCharacterDataEditorFromHistory";
			SetArmorStageArrays(data, array, array2, "调整护甲阶段顺序", refreshMethod);
		}
	}

	private void PreviewArmorStageThreshold(TowerDefenseArmorTypeData data, int index, double value)
	{
		Array<double> array = CopyThresholds(data?.stagePersontage);
		if (index >= 0 && index < array.Count)
		{
			array[index] = value;
			_entryBinding.PreviewValue(data, "stagePersontage", array);
			LightweightCharacterDataPreview();
		}
	}

	private void CommitArmorStageThreshold(TowerDefenseArmorTypeData data, int index, double value)
	{
		Array<double> array = CopyThresholds(data?.stagePersontage);
		if (index >= 0 && index < array.Count)
		{
			array[index] = value;
			_entryBinding.CommitEdit(data, "stagePersontage", array, "修改护甲阶段阈值", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void SetArmorStageTexture(TowerDefenseArmorTypeData data, int index, string texturePath)
	{
		Array<string> array = CopyTexturePaths(data?.stageAnimeTexturePaths);
		while (array.Count <= index)
		{
			array.Add(string.Empty);
		}
		array[index] = texturePath ?? string.Empty;
		_entryBinding.SetValue(data, "stageAnimeTexturePaths", array, "更换护甲阶段贴图", this, "RefreshCharacterDataEditorFromHistory");
	}

	private void SetArmorStageArrays(TowerDefenseArmorTypeData data, Array<double> stagePersontage, Array<string> stageAnimeTexturePaths, string actionName, StringName refreshMethod)
	{
		if (GodotObject.IsInstanceValid(data))
		{
			Array<double> array = CopyThresholds(data.stagePersontage);
			Array<string> array2 = CopyTexturePaths(data.stageAnimeTexturePaths);
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				data.stagePersontage = stagePersontage;
				data.stageAnimeTexturePaths = stageAnimeTexturePaths;
				OnCharacterDataPropertyEdited(committed: true);
				return;
			}
			xWUndoRedoManager.CreateAction(actionName);
			xWUndoRedoManager.AddDoProperty(data, "stagePersontage", stagePersontage);
			xWUndoRedoManager.AddDoProperty(data, "stageAnimeTexturePaths", stageAnimeTexturePaths);
			xWUndoRedoManager.AddUndoProperty(data, "stagePersontage", array);
			xWUndoRedoManager.AddUndoProperty(data, "stageAnimeTexturePaths", array2);
			xWUndoRedoManager.AddDoMethod(this, refreshMethod.ToString());
			xWUndoRedoManager.AddUndoMethod(this, refreshMethod.ToString());
			xWUndoRedoManager.CommitAction();
			OnCharacterDataPropertyEdited(committed: true);
		}
	}

	private static Array<double> CopyThresholds(Array<double> source)
	{
		if (source != null)
		{
			return new Array<double>(source);
		}
		return new Array<double>();
	}

	private void ToggleArmorMethodFlag(TowerDefenseArmorTypeData data, TowerDefenseEnum.ARMOR_METHOD_FLAGS flag, bool enabled)
	{
		SetEntryProperty(data, data, "armorMethodFlags", Variant.From<int>(enabled ? (data.armorMethodFlags | (int)flag) : (data.armorMethodFlags & (int)(~flag))));
	}

	private void ShowAudioSelector(string propertyName)
	{
		if (!GodotObject.IsInstanceValid(_audioTargetResource))
		{
			return;
		}
		_audioTargetProperty = ((propertyName == "damageAudio") ? "damageAudio" : "impactAudio");
		if (_audioPicker == null)
		{
			_audioPicker = XWGameplayResourcePickerWindow.Create();
		}
		if (GodotObject.IsInstanceValid(_audioPicker))
		{
			if (_audioPicker.GetParent() == null)
			{
				AddChild(_audioPicker, forceReadableName: false, InternalMode.Disabled);
			}
			string currentKey = _audioTargetResource.Get(_audioTargetProperty).AsString();
			_audioPicker.Open(XWGameplayResourceKind.Audio, currentKey, ApplyArmorAudioChoice, (XWGameplayResourceChoice choice) => choice.Kind == XWGameplayResourceKind.Audio, lockKind: true, "选择游戏音效");
		}
	}

	private void ApplyArmorAudioChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null) && choice.Kind == XWGameplayResourceKind.Audio && GodotObject.IsInstanceValid(_audioTargetResource))
		{
			SetEntryProperty(_audioTargetResource, _audioTargetResource, _audioTargetProperty, Variant.From<string>(choice.Key));
		}
	}

	private void AddResourceMetadataFields(VBoxContainer host, Resource resource)
	{
		host.AddChild(AddBoundLineEdit(resource.GetType().Name + "ResourceName", "resource_name", resource, "resource_name"), forceReadableName: false, InternalMode.Disabled);
		CheckButton checkButton = new CheckButton
		{
			Text = "resource_local_to_scene",
			ButtonPressed = resource.ResourceLocalToScene
		};
		_entryBinding.BindToggle(checkButton, resource, "resource_local_to_scene", LightweightCharacterDataPreview, this, "RefreshCharacterDataEditorFromHistory");
		host.AddChild(WrapField("resource_local_to_scene", checkButton), forceReadableName: false, InternalMode.Disabled);
	}

	private void AddArmorSlot(CharacterArmorData data)
	{
		if (data != null)
		{
			EnsureArmorList(data);
			ArmorSlotConfig item = new ArmorSlotConfig
			{
				ResourceName = $"ArmorSlot{data.armorList.Count + 1}",
				armorName = $"Armor{data.armorList.Count + 1}",
				replaceMethod = "Media",
				scale = Vector2.One,
				damagePoint = -1.0
			};
			Array<ArmorSlotConfig> array = CopyArmorSlots(data.armorList);
			array.Add(item);
			_selectedIndex = array.Count - 1;
			_entryBinding.SetValue(data, "armorList", array, "添加护甲槽", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void RemoveSelectedArmorSlot()
	{
		if (_editingData is CharacterArmorData { armorList: not null } characterArmorData && _selectedIndex >= 0 && _selectedIndex < characterArmorData.armorList.Count)
		{
			Array<ArmorSlotConfig> array = CopyArmorSlots(characterArmorData.armorList);
			array.RemoveAt(_selectedIndex);
			_selectedIndex = Mathf.Min(_selectedIndex, array.Count - 1);
			_entryBinding.SetValue(characterArmorData, "armorList", array, "删除护甲槽", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void AddCustomConfig(CharacterCustomData data)
	{
		if (data != null)
		{
			EnsureCustomList(data);
			CharacterCustomConfig item = new CharacterCustomConfig
			{
				ResourceName = $"CustomConfig{data.customList.Count + 1}",
				openKey = $"Custom{data.customList.Count + 1}",
				customName = $"Custom{data.customList.Count + 1}",
				type = "White"
			};
			Array<CharacterCustomConfig> array = CopyCustomConfigs(data.customList);
			array.Add(item);
			_selectedIndex = array.Count - 1;
			_entryBinding.SetValue(data, "customList", array, "添加自定义外观", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void RemoveSelectedCustomConfig()
	{
		if (_editingData is CharacterCustomData { customList: not null } characterCustomData && _selectedIndex >= 0 && _selectedIndex < characterCustomData.customList.Count)
		{
			Array<CharacterCustomConfig> array = CopyCustomConfigs(characterCustomData.customList);
			array.RemoveAt(_selectedIndex);
			_selectedIndex = Mathf.Min(_selectedIndex, array.Count - 1);
			_entryBinding.SetValue(characterCustomData, "customList", array, "删除自定义外观", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void AddDamagePoint(CharacterDamagePointData data)
	{
		if (data != null)
		{
			EnsureDamagePointList(data);
			CharacterDamagePointConfig item = new CharacterDamagePointConfig
			{
				ResourceName = $"DamagePoint{data.damagePointList.Count + 1}",
				damagePointName = $"DamagePoint{data.damagePointList.Count + 1}",
				damagePersontage = 0.5,
				isDrop = true
			};
			Array<CharacterDamagePointConfig> array = CopyDamagePoints(data.damagePointList);
			array.Add(item);
			_selectedIndex = array.Count - 1;
			_entryBinding.SetValue(data, "damagePointList", array, "添加受伤点", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void RemoveSelectedDamagePoint()
	{
		if (_editingData is CharacterDamagePointData { damagePointList: not null } characterDamagePointData && _selectedIndex >= 0 && _selectedIndex < characterDamagePointData.damagePointList.Count)
		{
			Array<CharacterDamagePointConfig> array = CopyDamagePoints(characterDamagePointData.damagePointList);
			array.RemoveAt(_selectedIndex);
			_selectedIndex = Mathf.Min(_selectedIndex, array.Count - 1);
			_entryBinding.SetValue(characterDamagePointData, "damagePointList", array, "删除受伤点", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private void MoveSelectedAggregateEntry(int direction)
	{
		if (_selectedIndex < 0 || direction == 0)
		{
			return;
		}
		int num = Mathf.Clamp(_selectedIndex + direction, 0, GetEntryCount(_editingData) - 1);
		if (num == _selectedIndex)
		{
			return;
		}
		Resource editingData = _editingData;
		if (!(editingData is CharacterArmorData characterArmorData))
		{
			if (!(editingData is CharacterCustomData characterCustomData))
			{
				if (editingData is CharacterDamagePointData characterDamagePointData)
				{
					Array<CharacterDamagePointConfig> array = CopyDamagePoints(characterDamagePointData.damagePointList);
					CharacterDamagePointConfig item = array[_selectedIndex];
					array.RemoveAt(_selectedIndex);
					array.Insert(num, item);
					_selectedIndex = num;
					_entryBinding.SetValue(characterDamagePointData, "damagePointList", array, "调整受伤点顺序", this, "RefreshCharacterDataEditorFromHistory");
				}
			}
			else
			{
				Array<CharacterCustomConfig> array2 = CopyCustomConfigs(characterCustomData.customList);
				CharacterCustomConfig item2 = array2[_selectedIndex];
				array2.RemoveAt(_selectedIndex);
				array2.Insert(num, item2);
				_selectedIndex = num;
				_entryBinding.SetValue(characterCustomData, "customList", array2, "调整外观顺序", this, "RefreshCharacterDataEditorFromHistory");
			}
		}
		else
		{
			Array<ArmorSlotConfig> array3 = CopyArmorSlots(characterArmorData.armorList);
			ArmorSlotConfig item3 = array3[_selectedIndex];
			array3.RemoveAt(_selectedIndex);
			array3.Insert(num, item3);
			_selectedIndex = num;
			_entryBinding.SetValue(characterArmorData, "armorList", array3, "调整护甲槽顺序", this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private static Array<ArmorSlotConfig> CopyArmorSlots(Array<ArmorSlotConfig> source)
	{
		if (source != null)
		{
			return new Array<ArmorSlotConfig>(source);
		}
		return new Array<ArmorSlotConfig>();
	}

	private static Array<CharacterCustomConfig> CopyCustomConfigs(Array<CharacterCustomConfig> source)
	{
		if (source != null)
		{
			return new Array<CharacterCustomConfig>(source);
		}
		return new Array<CharacterCustomConfig>();
	}

	private static Array<CharacterDamagePointConfig> CopyDamagePoints(Array<CharacterDamagePointConfig> source)
	{
		if (source != null)
		{
			return new Array<CharacterDamagePointConfig>(source);
		}
		return new Array<CharacterDamagePointConfig>();
	}

	private void SetEntryProperty(Resource owner, Resource entry, string propertyName, Variant value)
	{
		if (!_updatingControls && GodotObject.IsInstanceValid(owner) && GodotObject.IsInstanceValid(entry) && !string.IsNullOrWhiteSpace(propertyName))
		{
			_entryBinding.SetValue(entry, propertyName, value, "修改 " + propertyName, this, "RefreshCharacterDataEditorFromHistory");
		}
	}

	private static void NormalizeCharacterData(Resource resource)
	{
		if (!(resource is CharacterArmorData characterArmorData))
		{
			if (!(resource is CharacterCustomData characterCustomData))
			{
				if (resource is CharacterDamagePointData characterDamagePointData)
				{
					EnsureDamagePointList(characterDamagePointData);
					characterDamagePointData.Refresh();
				}
			}
			else
			{
				EnsureCustomList(characterCustomData);
				characterCustomData.Init();
			}
		}
		else
		{
			EnsureArmorList(characterArmorData);
			characterArmorData.Init();
		}
	}

	private static void EnsureArmorList(CharacterArmorData data)
	{
		if (data.armorList == null)
		{
			data.armorList = new Array<ArmorSlotConfig>();
		}
	}

	private static void EnsureCustomList(CharacterCustomData data)
	{
		if (data.customList == null)
		{
			data.customList = new Array<CharacterCustomConfig>();
		}
	}

	private static void EnsureDamagePointList(CharacterDamagePointData data)
	{
		if (data.damagePointList == null)
		{
			data.damagePointList = new Array<CharacterDamagePointConfig>();
		}
	}

	private void ClampSelectedIndex(Resource resource)
	{
		int entryCount = GetEntryCount(resource);
		if (entryCount <= 0)
		{
			_selectedIndex = -1;
			return;
		}
		if (_selectedIndex < 0)
		{
			_selectedIndex = 0;
		}
		_selectedIndex = Mathf.Clamp(_selectedIndex, 0, entryCount - 1);
	}

	private static int GetEntryCount(Resource resource)
	{
		if (!(resource is CharacterArmorData characterArmorData))
		{
			if (!(resource is CharacterCustomData characterCustomData))
			{
				if (resource is CharacterDamagePointData characterDamagePointData)
				{
					return characterDamagePointData.damagePointList?.Count ?? 0;
				}
				return 0;
			}
			return characterCustomData.customList?.Count ?? 0;
		}
		return characterArmorData.armorList?.Count ?? 0;
	}

	private ArmorSlotConfig GetArmorSlot(CharacterArmorData data)
	{
		if (data?.armorList == null || _selectedIndex < 0 || _selectedIndex >= data.armorList.Count)
		{
			return null;
		}
		return data.armorList[_selectedIndex];
	}

	private CharacterCustomConfig GetCustomConfig(CharacterCustomData data)
	{
		if (data?.customList == null || _selectedIndex < 0 || _selectedIndex >= data.customList.Count)
		{
			return null;
		}
		return data.customList[_selectedIndex];
	}

	private CharacterDamagePointConfig GetDamagePointConfig(CharacterDamagePointData data)
	{
		if (data?.damagePointList == null || _selectedIndex < 0 || _selectedIndex >= data.damagePointList.Count)
		{
			return null;
		}
		return data.damagePointList[_selectedIndex];
	}

	private static string BuildEntryTitle(Resource resource, int index)
	{
		if (!(resource is CharacterArmorData characterArmorData))
		{
			if (!(resource is CharacterCustomData characterCustomData))
			{
				if (resource is CharacterDamagePointData characterDamagePointData && index >= 0 && index < characterDamagePointData.damagePointList.Count)
				{
					return $"{index + 1}. {EmptyToPlaceholder(characterDamagePointData.damagePointList[index]?.damagePointName)}  hp={characterDamagePointData.damagePointList[index]?.damagePersontage:0.##}";
				}
			}
			else if (index >= 0 && index < characterCustomData.customList.Count)
			{
				return $"{index + 1}. {EmptyToPlaceholder(characterCustomData.customList[index]?.customName)}  type={EmptyToPlaceholder(characterCustomData.customList[index]?.type)}";
			}
		}
		else if (index >= 0 && index < characterArmorData.armorList.Count)
		{
			return $"{index + 1}. {EmptyToPlaceholder(characterArmorData.armorList[index]?.armorName)}  damage={characterArmorData.armorList[index]?.damagePoint:0.##}";
		}
		return $"{index + 1}";
	}

	private void UpdateVisualPreview()
	{
		if (GodotObject.IsInstanceValid(_previewHealthSlider))
		{
			Resource editingData = _editingData;
			bool flag = ((editingData is CharacterCustomData || editingData is CharacterCustomConfig) ? true : false);
			bool flag2 = flag;
			_previewHealthSlider.Editable = !flag2;
			_previewHealthSlider.TooltipText = (flag2 ? "自定义外观不依赖生命值，选择左侧条目查看槽位效果。" : "拖动生命值，预览护甲或受伤阶段在运行时的触发状态。");
			if (GodotObject.IsInstanceValid(_previewPlayPauseButton))
			{
				_previewPlayPauseButton.Disabled = flag2;
				_previewPlayPauseButton.TooltipText = (flag2 ? "自定义外观不依赖生命值，无需播放受击演示。" : "在真实角色预览中连续播放从满生命到零生命的受击阶段。");
			}
		}
		if (GodotObject.IsInstanceValid(_previewStatusLabel))
		{
			Label previewStatusLabel = _previewStatusLabel;
			Resource editingData = _editingData;
			string text = ((editingData is CharacterArmorData data) ? BuildArmorPreviewStatus(data) : ((editingData is CharacterCustomData data2) ? BuildCustomPreviewStatus(data2) : ((editingData is CharacterDamagePointData data3) ? BuildDamagePreviewStatus(data3) : ((editingData is ArmorSlotConfig config) ? BuildArmorSlotPreviewStatus(config) : ((editingData is CharacterCustomConfig config2) ? BuildCustomConfigPreviewStatus(config2) : ((editingData is CharacterDamagePointConfig config3) ? BuildDamagePointConfigPreviewStatus(config3) : ((!(editingData is TowerDefenseArmorTypeData data4)) ? "" : BuildArmorTypePreviewStatus(data4))))))));
			previewStatusLabel.Text = text;
		}
		ApplyRuntimeCharacterDataPreview();
		_configurationPreviewCanvas?.QueueRedraw();
	}

	private void RebuildRuntimeCharacterPreview(Resource data)
	{
		if (!GodotObject.IsInstanceValid(_previewRoot))
		{
			return;
		}
		RuntimeCharacterPreviewBuildCount++;
		foreach (Node child in _previewRoot.GetChildren())
		{
			child.QueueFree();
		}
		_runtimeCharacterPreview = null;
		_previewOwnerConfig = FindOwningCharacterConfig(data);
		if (!GodotObject.IsInstanceValid(_previewOwnerConfig) || !TryResolveOwningCharacterScene(_previewOwnerConfig, out var scene))
		{
			SetRuntimePreviewMissing(missing: true, "未找到引用该数据的完整角色场景。请把数据放入角色包并在角色配置中引用它。");
			return;
		}
		try
		{
			Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			_runtimeCharacterPreview = FindRuntimeCharacter(node);
			if (!GodotObject.IsInstanceValid(_runtimeCharacterPreview))
			{
				node.Free();
				SetRuntimePreviewMissing(missing: true, "角色场景没有 TowerDefenseCharacter 根节点。");
				return;
			}
			_runtimeCharacterPreview.inGame = false;
			_runtimeCharacterPreview.editorPreviewMode = true;
			_runtimeCharacterPreview.config = _previewOwnerConfig;
			_previewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			SetRuntimePreviewMissing(missing: false, "");
			ApplyRuntimeCharacterDataPreview();
		}
		catch (Exception ex)
		{
			_runtimeCharacterPreview = null;
			SetRuntimePreviewMissing(missing: true, "完整角色场景加载失败：" + ex.Message);
		}
	}

	private void ApplyRuntimeCharacterDataPreview()
	{
		if (GodotObject.IsInstanceValid(_runtimeCharacterPreview))
		{
			RuntimeCharacterPreviewApplyCount++;
			_runtimeCharacterPreview.previewDamagePointPersontage = _previewHealthRatio;
			string previewArmorName = GetPreviewArmorName();
			_runtimeCharacterPreview.currentArmor = (string.IsNullOrWhiteSpace(previewArmorName) ? new Array<string>() : new Array<string> { previewArmorName });
			string previewCustomName = GetPreviewCustomName();
			_runtimeCharacterPreview.currentCustom = (string.IsNullOrWhiteSpace(previewCustomName) ? new Array<string>() : new Array<string> { previewCustomName });
		}
	}

	private void ToggleDamagePreview()
	{
		if (_damagePreviewPlaying)
		{
			SetDamagePreviewPlaying(playing: false, "演示已暂停 · 点击继续");
			return;
		}
		if (_previewHealthRatio <= 0.001)
		{
			ResetDamagePreview();
		}
		_damagePreviewElapsed = (1.0 - _previewHealthRatio) * 6.0;
		SetDamagePreviewPlaying(playing: true, "正在播放真实角色受击阶段");
	}

	private void ResetDamagePreview()
	{
		_damagePreviewElapsed = 0.0;
		_previewHealthRatio = 1.0;
		ApplyPreviewHealthRatio();
		SetDamagePreviewPlaying(playing: false, "已恢复满生命 · 可重新播放");
	}

	private void SetDamagePreviewPlaying(bool playing, string stateText)
	{
		_damagePreviewPlaying = playing && GodotObject.IsInstanceValid(_previewHealthSlider) && _previewHealthSlider.Editable;
		RequestVisibilityGatedProcessing(_damagePreviewPlaying);
		UpdateDamagePreviewControls(stateText);
	}

	private void ApplyPreviewHealthRatio()
	{
		if (GodotObject.IsInstanceValid(_previewHealthSlider))
		{
			_previewHealthSlider.SetValueNoSignal(_previewHealthRatio * 100.0);
		}
		if (GodotObject.IsInstanceValid(_previewHealthValueLabel))
		{
			_previewHealthValueLabel.Text = $"{_previewHealthRatio:P0}";
		}
		UpdateVisualPreview();
	}

	private void UpdateDamagePreviewControls(string stateText)
	{
		if (GodotObject.IsInstanceValid(_previewPlayPauseButton))
		{
			_previewPlayPauseButton.Text = (_damagePreviewPlaying ? "⏸ 暂停演示" : "▶ 播放受击");
		}
		if (GodotObject.IsInstanceValid(_previewPlaybackStateLabel))
		{
			_previewPlaybackStateLabel.Text = stateText ?? "";
			_previewPlaybackStateLabel.Modulate = (_damagePreviewPlaying ? new Color(1f, 0.79f, 0.32f) : new Color(0.64f, 0.78f, 0.71f));
		}
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		ApplyCharacterDataPreviewVisibility(visible);
	}

	private void ApplyCharacterDataPreviewVisibility(bool visible)
	{
		if (GodotObject.IsInstanceValid(_previewViewport))
		{
			_previewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(visible ? 4 : 0);
		}
		if (GodotObject.IsInstanceValid(_previewRoot))
		{
			_previewRoot.ProcessMode = (ProcessModeEnum)(visible ? 0 : 4);
		}
	}

	private string GetPreviewArmorName()
	{
		if (_editingData is ArmorSlotConfig armorSlotConfig)
		{
			return armorSlotConfig.armorName;
		}
		if (_editingData is CharacterArmorData { armorList: not null } characterArmorData && characterArmorData.armorList.Count > 0)
		{
			int index = Mathf.Clamp(_selectedIndex, 0, characterArmorData.armorList.Count - 1);
			return characterArmorData.armorList[index]?.armorName ?? "";
		}
		return "";
	}

	private string GetPreviewCustomName()
	{
		if (_editingData is CharacterCustomConfig characterCustomConfig)
		{
			return characterCustomConfig.customName;
		}
		if (_editingData is CharacterCustomData { customList: not null } characterCustomData && characterCustomData.customList.Count > 0)
		{
			int index = Mathf.Clamp(_selectedIndex, 0, characterCustomData.customList.Count - 1);
			return characterCustomData.customList[index]?.customName ?? "";
		}
		return "";
	}

	private TowerDefenseCharacterConfig FindOwningCharacterConfig(Resource data)
	{
		string text = FindCharacterPackageRoot(base.CurrentResourcePath, data?.ResourcePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string text2 = text.PathJoin("Config");
		string[] filesAt = DirAccess.GetFilesAt(text2);
		foreach (string text3 in filesAt)
		{
			if (string.Equals(text3.GetExtension(), "tres", StringComparison.OrdinalIgnoreCase))
			{
				TowerDefenseCharacterConfig towerDefenseCharacterConfig = ResourceLoader.Load<TowerDefenseCharacterConfig>(text2.PathJoin(text3), null, ResourceLoader.CacheMode.Reuse);
				if (GodotObject.IsInstanceValid(towerDefenseCharacterConfig) && CharacterConfigReferencesData(towerDefenseCharacterConfig, data))
				{
					return towerDefenseCharacterConfig;
				}
			}
		}
		return null;
	}

	private static string FindCharacterPackageRoot(params string[] paths)
	{
		for (int i = 0; i < paths.Length; i++)
		{
			string text = NormalizePreviewPath(paths[i]).GetBaseDir();
			for (int j = 0; j < 6; j++)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					break;
				}
				if (DirAccess.DirExistsAbsolute(text.PathJoin("Config")) && DirAccess.DirExistsAbsolute(text.PathJoin("Scene")))
				{
					return text;
				}
				string baseDir = text.GetBaseDir();
				if (baseDir == text)
				{
					break;
				}
				text = baseDir;
			}
		}
		return "";
	}

	private static bool CharacterConfigReferencesData(TowerDefenseCharacterConfig config, Resource data)
	{
		if (config == null || data == null)
		{
			return false;
		}
		if (SameResource(config.armorData, data) || SameResource(config.customData, data) || SameResource(config.damagePointData, data))
		{
			return true;
		}
		if (data is ArmorSlotConfig right && config.armorData?.armorList != null)
		{
			foreach (ArmorSlotConfig armor in config.armorData.armorList)
			{
				if (SameResource(armor, right))
				{
					return true;
				}
			}
		}
		if (data is CharacterCustomConfig right2 && config.customData?.customList != null)
		{
			foreach (CharacterCustomConfig custom in config.customData.customList)
			{
				if (SameResource(custom, right2))
				{
					return true;
				}
			}
		}
		if (data is CharacterDamagePointConfig right3 && config.damagePointData?.damagePointList != null)
		{
			foreach (CharacterDamagePointConfig damagePoint in config.damagePointData.damagePointList)
			{
				if (SameResource(damagePoint, right3))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool SameResource(Resource left, Resource right)
	{
		if (left == right)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(left) && GodotObject.IsInstanceValid(right) && !string.IsNullOrWhiteSpace(left.ResourcePath))
		{
			return string.Equals(NormalizePreviewPath(left.ResourcePath), NormalizePreviewPath(right.ResourcePath), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool TryResolveOwningCharacterScene(TowerDefenseCharacterConfig config, out PackedScene scene)
	{
		scene = null;
		if (GodotObject.IsInstanceValid(config) && !string.IsNullOrWhiteSpace(config.name) && ResourceManager.Instance != null)
		{
			scene = ResourceManager.Instance.GetCharacterScene(config.name);
			if (GodotObject.IsInstanceValid(scene))
			{
				return true;
			}
		}
		string text = FindCharacterPackageRoot(config?.ResourcePath).PathJoin("Scene");
		string[] filesAt = DirAccess.GetFilesAt(text);
		foreach (string text2 in filesAt)
		{
			if (string.Equals(text2.GetExtension(), "tscn", StringComparison.OrdinalIgnoreCase))
			{
				scene = ResourceLoader.Load<PackedScene>(text.PathJoin(text2), null, ResourceLoader.CacheMode.Reuse);
				if (GodotObject.IsInstanceValid(scene))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static TowerDefenseCharacter FindRuntimeCharacter(Node node)
	{
		if (node is TowerDefenseCharacter result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		foreach (Node child in node.GetChildren())
		{
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(child);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private void SetRuntimePreviewMissing(bool missing, string message)
	{
		if (GodotObject.IsInstanceValid(_configurationPreviewCanvas))
		{
			_configurationPreviewCanvas.Visible = missing;
			_configurationPreviewCanvas.QueueRedraw();
		}
		if (GodotObject.IsInstanceValid(_fallbackRibbon))
		{
			_fallbackRibbon.Visible = missing;
		}
		if (GodotObject.IsInstanceValid(_fallbackRibbonLabel))
		{
			_fallbackRibbonLabel.Text = (missing ? message : "");
		}
		if (GodotObject.IsInstanceValid(_previewMissingLabel))
		{
			_previewMissingLabel.Visible = missing && !GodotObject.IsInstanceValid(_configurationPreviewCanvas);
			_previewMissingLabel.Text = message;
		}
	}

	private void OnRuntimePreviewGuiInput(InputEvent inputEvent)
	{
		InputEventMouseButton inputEventMouseButton = inputEvent as InputEventMouseButton;
		bool flag = inputEventMouseButton?.Pressed ?? false;
		if (flag)
		{
			MouseButton buttonIndex = inputEventMouseButton.ButtonIndex;
			bool flag2 = (((ulong)(buttonIndex - 4) <= 1uL) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			_previewZoom = Mathf.Clamp(_previewZoom * ((inputEventMouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.1f : 0.9f), 0.35f, 3f);
			UpdateRuntimePreviewTransform();
			_previewViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseButton inputEventMouseButton2 && inputEventMouseButton2.ButtonIndex == MouseButton.Left)
		{
			_draggingPreview = inputEventMouseButton2.Pressed;
			_previewViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _draggingPreview)
		{
			_previewPan += inputEventMouseMotion.Relative;
			UpdateRuntimePreviewTransform();
			_previewViewportContainer.AcceptEvent();
		}
	}

	private void UpdateRuntimePreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_previewRoot))
		{
			_previewRoot.Position = new Vector2(450f, 228f) + _previewPan;
			_previewRoot.Scale = Vector2.One * _previewZoom;
		}
	}

	private static string NormalizePreviewPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Replace('\\', '/').Trim();
		}
		return "";
	}

	private void DrawCharacterDataPreview(Control canvas)
	{
		if (!GodotObject.IsInstanceValid(canvas))
		{
			return;
		}
		canvas.DrawRect(new Rect2(Vector2.Zero, canvas.Size), new Color(0.042f, 0.048f, 0.058f));
		Resource editingData = _editingData;
		if (!(editingData is CharacterArmorData data))
		{
			if (!(editingData is CharacterCustomData data2))
			{
				if (!(editingData is CharacterDamagePointData data3))
				{
					if (!(editingData is ArmorSlotConfig config))
					{
						if (!(editingData is CharacterCustomConfig config2))
						{
							if (!(editingData is CharacterDamagePointConfig config3))
							{
								if (editingData is TowerDefenseArmorTypeData data4)
								{
									DrawArmorTypePreview(canvas, data4);
								}
							}
							else
							{
								DrawDamagePointConfigPreview(canvas, config3);
							}
						}
						else
						{
							DrawCustomConfigPreview(canvas, config2);
						}
					}
					else
					{
						DrawArmorSlotConfigPreview(canvas, config);
					}
				}
				else
				{
					DrawDamagePointPreview(canvas, data3);
				}
			}
			else
			{
				DrawCustomPreview(canvas, data2);
			}
		}
		else
		{
			DrawArmorPreview(canvas, data);
		}
	}

	private void DrawDamagePointConfigPreview(Control canvas, CharacterDamagePointConfig config)
	{
		float num = Mathf.Max(180f, canvas.Size.X - 72f);
		DrawHealthBar(canvas, "受伤点", 36f, 18f, num);
		float num2 = Mathf.Clamp((float)config.damagePersontage, 0f, 1f);
		float num3 = 124f + Mathf.Max(30f, num - 88f) * num2;
		bool flag = _previewHealthRatio <= (double)num2;
		canvas.DrawLine(new Vector2(num3, 43f), new Vector2(num3, 86f), new Color(1f, 0.62f, 0.34f), 3f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(num3 - 30f, 101f), $"阈值 {num2:P0}", HorizontalAlignment.Center, 60f, 12, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		Rect2 rect = new Rect2(36f, 112f, num, 50f);
		canvas.DrawRect(rect, flag ? new Color(0.38f, 0.16f, 0.13f) : new Color(0.13f, 0.21f, 0.26f));
		canvas.DrawRect(rect, flag ? new Color(1f, 0.58f, 0.38f) : new Color(0.36f, 0.58f, 0.7f), filled: false, 2f);
		Texture2D texture2D = LoadTexturePreview(config.replaceMediaTexturePath);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			canvas.DrawTextureRect(texture2D, new Rect2(43f, 118f, 38f, 38f), tile: false);
		}
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(89f, 134f), EmptyToPlaceholder(config.damagePointName), HorizontalAlignment.Left, num - 62f, 13, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(89f, 154f), flag ? "运行状态：已触发" : "运行状态：等待触发", HorizontalAlignment.Left, num - 62f, 11, flag ? new Color(1f, 0.69f, 0.52f) : new Color(0.67f, 0.82f, 0.91f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void DrawArmorSlotConfigPreview(Control canvas, ArmorSlotConfig config)
	{
		DrawHealthBar(canvas, "护甲槽", 24f, 14f, canvas.Size.X - 48f);
		bool flag = config.damagePoint < 0.0 || _previewHealthRatio > config.damagePoint;
		Vector2 vector = new Vector2(canvas.Size.X * 0.33f, 116f);
		canvas.DrawCircle(vector - new Vector2(0f, 28f), 22f, new Color(0.35f, 0.39f, 0.45f));
		canvas.DrawRect(new Rect2(vector - new Vector2(26f, 6f), new Vector2(52f, 58f)), new Color(0.27f, 0.31f, 0.37f));
		Vector2 position = vector + config.offset * 0.25f;
		canvas.DrawSetTransform(position, (float)config.rotation, config.scale);
		canvas.DrawRect(new Rect2(new Vector2(-34f, -28f), new Vector2(68f, 56f)), flag ? new Color(0.28f, 0.58f, 0.76f) : new Color(0.3f, 0.19f, 0.18f));
		canvas.DrawRect(new Rect2(new Vector2(-34f, -28f), new Vector2(68f, 56f)), flag ? new Color(0.65f, 0.88f, 1f) : new Color(0.72f, 0.43f, 0.38f), filled: false, 2f);
		canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		float x = canvas.Size.X * 0.55f;
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(x, 82f), EmptyToPlaceholder(config.armorName), HorizontalAlignment.Left, -1f, 16, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(x, 108f), "替换方式: " + EmptyToPlaceholder(config.replaceMethod), HorizontalAlignment.Left, -1f, 12, new Color(0.72f, 0.83f, 0.92f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(x, 132f), $"槽位: {config.slotPath}", HorizontalAlignment.Left, -1f, 12, new Color(0.72f, 0.83f, 0.92f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(x, 156f), flag ? "护甲显示" : "已达到移除阈值", HorizontalAlignment.Left, -1f, 12, flag ? new Color(0.55f, 0.92f, 0.66f) : new Color(1f, 0.62f, 0.52f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void DrawCustomConfigPreview(Control canvas, CharacterCustomConfig config)
	{
		bool flag = string.Equals(config.type, "Gold", StringComparison.OrdinalIgnoreCase);
		Rect2 rect = new Rect2(26f, 24f, canvas.Size.X - 52f, canvas.Size.Y - 42f);
		canvas.DrawRect(rect, flag ? new Color(0.34f, 0.27f, 0.08f) : new Color(0.22f, 0.24f, 0.29f));
		canvas.DrawRect(rect, flag ? new Color(0.96f, 0.76f, 0.24f) : new Color(0.73f, 0.78f, 0.84f), filled: false, 2f);
		Texture2D texture2D = ((config.damagePointChangeMediaTexturePaths != null && config.damagePointChangeMediaTexturePaths.Count > 0) ? LoadTexturePreview(config.damagePointChangeMediaTexturePaths[0]) : null);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			canvas.DrawTextureRect(texture2D, new Rect2(rect.End.X - 122f, rect.Position.Y + 12f, 104f, 104f), tile: false);
		}
		canvas.DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(16f, 32f), EmptyToPlaceholder(config.customName), HorizontalAlignment.Left, rect.Size.X - 150f, 18, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(16f, 60f), "品质: " + EmptyToPlaceholder(config.type), HorizontalAlignment.Left, rect.Size.X - 150f, 13, flag ? new Color(1f, 0.82f, 0.35f) : new Color(0.8f, 0.84f, 0.9f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(16f, 86f), "解锁键: " + EmptyToPlaceholder(config.openKey), HorizontalAlignment.Left, rect.Size.X - 150f, 12, new Color(0.72f, 0.82f, 0.91f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		canvas.DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(16f, 112f), $"伤害阶段贴图: {config.damagePointChangeMediaTexturePaths?.Count ?? 0}", HorizontalAlignment.Left, rect.Size.X - 150f, 12, new Color(0.72f, 0.82f, 0.91f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void DrawArmorTypePreview(Control canvas, TowerDefenseArmorTypeData data)
	{
		float num = Mathf.Max(200f, canvas.Size.X - 60f);
		DrawHealthBar(canvas, "护甲耐久", 30f, 14f, num);
		int num2 = data.stagePersontage?.Count ?? 0;
		for (int i = 0; i < num2; i++)
		{
			float num3 = Mathf.Clamp((float)data.stagePersontage[i], 0f, 1f);
			float num4 = 118f + Mathf.Max(30f, num - 88f) * num3;
			canvas.DrawLine(new Vector2(num4, 42f), new Vector2(num4, 68f), new Color(0.98f, 0.67f, 0.3f), 2f);
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(num4 - 22f, 82f), num3.ToString("P0"), HorizontalAlignment.Center, 44f, 10, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
		int num5 = data.stageAnimeTexturePaths?.Count ?? 0;
		float num6 = Mathf.Clamp((num - 8f * (float)Mathf.Max(0, num5 - 1)) / (float)Mathf.Max(1, num5), 82f, 150f);
		for (int j = 0; j < num5; j++)
		{
			float num7 = 30f + (float)j * (num6 + 8f);
			Rect2 rect = new Rect2(num7, 94f, num6, 68f);
			canvas.DrawRect(rect, new Color(0.13f, 0.19f, 0.25f));
			canvas.DrawRect(rect, new Color(0.36f, 0.52f, 0.64f), filled: false, 1f);
			Texture2D texture2D = LoadTexturePreview(data.stageAnimeTexturePaths[j]);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				canvas.DrawTextureRect(texture2D, new Rect2(num7 + 5f, 99f, 48f, 48f), tile: false);
			}
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(num7 + 58f, 124f), $"阶段 {j + 1}", HorizontalAlignment.Left, num6 - 62f, 12, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	private void DrawArmorPreview(Control canvas, CharacterArmorData data)
	{
		DrawHealthBar(canvas, "角色生命", 18f, 14f, canvas.Size.X - 36f);
		Vector2 vector = new Vector2(76f, 105f);
		canvas.DrawCircle(vector - new Vector2(0f, 31f), 22f, new Color(0.34f, 0.39f, 0.45f));
		canvas.DrawRect(new Rect2(vector - new Vector2(25f, 10f), new Vector2(50f, 65f)), new Color(0.27f, 0.31f, 0.37f));
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(36f, 168f), "角色挂点", HorizontalAlignment.Center, 80f, 12, new Color(0.72f, 0.76f, 0.82f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		int valueOrDefault = (data?.armorList?.Count).GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(150f, 105f), "暂无护甲槽", HorizontalAlignment.Left, -1f, 14, new Color(0.72f, 0.74f, 0.78f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			return;
		}
		float num = 138f;
		float num2 = 8f;
		float num3 = Mathf.Clamp((Mathf.Max(120f, canvas.Size.X - num - 16f) - num2 * (float)(valueOrDefault - 1)) / (float)valueOrDefault, 88f, 170f);
		for (int i = 0; i < valueOrDefault; i++)
		{
			ArmorSlotConfig armorSlotConfig = data.armorList[i];
			if (armorSlotConfig != null)
			{
				float num4 = num + (float)i * (num3 + num2);
				Rect2 rect = new Rect2(num4, 54f, num3, 106f);
				bool flag = i == _selectedIndex;
				bool flag2 = armorSlotConfig.damagePoint >= 0.0 && _previewHealthRatio <= armorSlotConfig.damagePoint;
				Color color = (flag2 ? new Color(0.31f, 0.16f, 0.15f) : new Color(0.12f, 0.22f, 0.29f));
				canvas.DrawRect(rect, color);
				canvas.DrawRect(rect, flag ? new Color(0.49f, 0.79f, 1f) : new Color(0.3f, 0.39f, 0.47f), filled: false, flag ? 3f : 1f);
				Texture2D texture2D = null;
				TowerDefenseArmorTypeData typeData = data.GetTypeData(armorSlotConfig.armorName);
				if (typeData?.stageAnimeTexturePaths != null && typeData.stageAnimeTexturePaths.Count > 0)
				{
					texture2D = LoadTexturePreview(typeData.stageAnimeTexturePaths[0]);
				}
				if (GodotObject.IsInstanceValid(texture2D))
				{
					canvas.DrawTextureRect(texture2D, new Rect2(num4 + 8f, 62f, Mathf.Min(42f, num3 - 16f), 42f), tile: false);
				}
				else
				{
					canvas.DrawCircle(new Vector2(num4 + 29f, 83f), 17f, new Color(0.39f, 0.48f, 0.57f));
				}
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num4 + 8f, 119f), $"{i + 1}. {EmptyToPlaceholder(armorSlotConfig.armorName)}", HorizontalAlignment.Left, num3 - 16f, 12, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				string text = ((armorSlotConfig.damagePoint < 0.0) ? "常驻" : (flag2 ? "已达阈值" : $"阈值 {armorSlotConfig.damagePoint:P0}"));
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num4 + 8f, 143f), text, HorizontalAlignment.Left, num3 - 16f, 12, flag2 ? new Color(1f, 0.61f, 0.52f) : new Color(0.68f, 0.83f, 0.94f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
	}

	private void DrawDamagePointPreview(Control canvas, CharacterDamagePointData data)
	{
		float num = Mathf.Max(120f, canvas.Size.X - 84f);
		DrawHealthBar(canvas, "生命阶段", 42f, 18f, num);
		int valueOrDefault = (data?.damagePointList?.Count).GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(42f, 108f), "暂无受伤点", HorizontalAlignment.Left, -1f, 14, new Color(0.72f, 0.74f, 0.78f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			return;
		}
		float num2 = Mathf.Clamp((num - 8f * (float)(valueOrDefault - 1)) / (float)valueOrDefault, 92f, 180f);
		for (int i = 0; i < valueOrDefault; i++)
		{
			CharacterDamagePointConfig characterDamagePointConfig = data.damagePointList[i];
			if (characterDamagePointConfig != null)
			{
				float num3 = Mathf.Clamp((float)characterDamagePointConfig.damagePersontage, 0f, 1f);
				float num4 = 42f + num * num3;
				bool flag = _previewHealthRatio <= (double)num3;
				bool flag2 = i == _selectedIndex;
				canvas.DrawLine(new Vector2(num4, 48f), new Vector2(num4, 72f), flag2 ? new Color(0.54f, 0.83f, 1f) : new Color(0.84f, 0.57f, 0.35f), flag2 ? 3f : 2f);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num4 - 24f, 86f), $"{num3:P0}", HorizontalAlignment.Center, 48f, 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				float num5 = 42f + (float)i * (num2 + 8f);
				Rect2 rect = new Rect2(num5, 98f, num2, 65f);
				canvas.DrawRect(rect, flag ? new Color(0.38f, 0.17f, 0.13f) : new Color(0.13f, 0.19f, 0.24f));
				canvas.DrawRect(rect, flag2 ? new Color(0.52f, 0.82f, 1f) : new Color(0.34f, 0.38f, 0.43f), filled: false, flag2 ? 3f : 1f);
				Texture2D texture2D = LoadTexturePreview(characterDamagePointConfig.replaceMediaTexturePath);
				if (GodotObject.IsInstanceValid(texture2D))
				{
					canvas.DrawTextureRect(texture2D, new Rect2(num5 + 6f, 104f, 38f, 38f), tile: false);
				}
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num5 + 8f, 124f), $"{i + 1}. {EmptyToPlaceholder(characterDamagePointConfig.damagePointName)}", HorizontalAlignment.Left, num2 - 16f, 12, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num5 + 8f, 148f), flag ? "已触发" : "未触发", HorizontalAlignment.Left, num2 - 16f, 11, flag ? new Color(1f, 0.65f, 0.48f) : new Color(0.63f, 0.8f, 0.91f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
	}

	private void DrawCustomPreview(Control canvas, CharacterCustomData data)
	{
		int valueOrDefault = (data?.customList?.Count).GetValueOrDefault();
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(18f, 28f), "外观与解锁槽位", HorizontalAlignment.Left, -1f, 14, new Color(0.82f, 0.86f, 0.92f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		if (valueOrDefault == 0)
		{
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(18f, 92f), "暂无自定义外观", HorizontalAlignment.Left, -1f, 14, new Color(0.72f, 0.74f, 0.78f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			return;
		}
		float num = 10f;
		float num2 = Mathf.Clamp((Mathf.Max(120f, canvas.Size.X - 36f) - num * (float)(valueOrDefault - 1)) / (float)valueOrDefault, 110f, 210f);
		for (int i = 0; i < valueOrDefault; i++)
		{
			CharacterCustomConfig characterCustomConfig = data.customList[i];
			if (characterCustomConfig != null)
			{
				float num3 = 18f + (float)i * (num2 + num);
				Rect2 rect = new Rect2(num3, 42f, num2, 116f);
				bool flag = i == _selectedIndex;
				bool flag2 = string.Equals(characterCustomConfig.type, "Gold", StringComparison.OrdinalIgnoreCase);
				Color color = (flag2 ? new Color(0.34f, 0.27f, 0.08f) : new Color(0.23f, 0.25f, 0.29f));
				Color color2 = (flag ? new Color(0.51f, 0.84f, 1f) : (flag2 ? new Color(0.95f, 0.75f, 0.24f) : new Color(0.72f, 0.76f, 0.82f)));
				canvas.DrawRect(rect, color);
				canvas.DrawRect(rect, color2, filled: false, flag ? 3f : 1f);
				Texture2D texture2D = ((characterCustomConfig.damagePointChangeMediaTexturePaths != null && characterCustomConfig.damagePointChangeMediaTexturePaths.Count > 0) ? LoadTexturePreview(characterCustomConfig.damagePointChangeMediaTexturePaths[0]) : null);
				if (GodotObject.IsInstanceValid(texture2D))
				{
					canvas.DrawTextureRect(texture2D, new Rect2(num3 + num2 - 50f, 50f, 42f, 42f), tile: false);
				}
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num3 + 9f, 67f), $"{i + 1}. {EmptyToPlaceholder(characterCustomConfig.customName)}", HorizontalAlignment.Left, num2 - 62f, 13, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num3 + 9f, 91f), "类型: " + EmptyToPlaceholder(characterCustomConfig.type), HorizontalAlignment.Left, num2 - 18f, 11, color2, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num3 + 9f, 113f), "解锁键: " + EmptyToPlaceholder(characterCustomConfig.openKey), HorizontalAlignment.Left, num2 - 18f, 11, new Color(0.76f, 0.8f, 0.86f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(num3 + 9f, 137f), $"伤害贴图: {characterCustomConfig.damagePointChangeMediaTexturePaths?.Count ?? 0}", HorizontalAlignment.Left, num2 - 18f, 11, new Color(0.64f, 0.82f, 0.91f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
	}

	private void DrawHealthBar(Control canvas, string title, float x, float y, float width)
	{
		float num = Mathf.Max(80f, width);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(x, y + 14f), title, HorizontalAlignment.Left, 88f, 12, new Color(0.8f, 0.84f, 0.9f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		Rect2 rect = new Rect2(x + 88f, y + 2f, Mathf.Max(30f, num - 88f), 15f);
		canvas.DrawRect(rect, new Color(0.16f, 0.1f, 0.1f));
		canvas.DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X * (float)_previewHealthRatio, rect.Size.Y)), new Color(0.28f, 0.72f, 0.35f));
		canvas.DrawRect(rect, new Color(0.58f, 0.64f, 0.7f), filled: false, 1f);
		canvas.DrawString(GetThemeDefaultFont(), new Vector2(rect.End.X - 50f, y + 15f), $"{_previewHealthRatio:P0}", HorizontalAlignment.Right, 46f, 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private string BuildArmorPreviewStatus(CharacterArmorData data)
	{
		int num = 0;
		if (data?.armorList != null)
		{
			foreach (ArmorSlotConfig armor in data.armorList)
			{
				if (armor != null && armor.damagePoint >= 0.0 && _previewHealthRatio <= armor.damagePoint)
				{
					num++;
				}
			}
		}
		return $"生命 {_previewHealthRatio:P0} · 达到 {num} 个护甲阈值";
	}

	private string BuildDamagePreviewStatus(CharacterDamagePointData data)
	{
		int num = 0;
		if (data?.damagePointList != null)
		{
			foreach (CharacterDamagePointConfig damagePoint in data.damagePointList)
			{
				if (damagePoint != null && _previewHealthRatio <= damagePoint.damagePersontage)
				{
					num++;
				}
			}
		}
		return $"生命 {_previewHealthRatio:P0} · 已触发 {num} 个阶段";
	}

	private string BuildCustomPreviewStatus(CharacterCustomData data)
	{
		CharacterCustomConfig customConfig = GetCustomConfig(data);
		if (customConfig != null)
		{
			return "当前: " + EmptyToPlaceholder(customConfig.customName) + " · " + EmptyToPlaceholder(customConfig.type);
		}
		return $"共 {(data?.customList?.Count).GetValueOrDefault()} 个外观槽";
	}

	private string BuildDamagePointConfigPreviewStatus(CharacterDamagePointConfig config)
	{
		bool flag = _previewHealthRatio <= config.damagePersontage;
		return $"生命 {_previewHealthRatio:P0} · {(flag ? "已触发" : "未触发")} · 音效 {EmptyToPlaceholder(config.damageAudio)}";
	}

	private string BuildArmorSlotPreviewStatus(ArmorSlotConfig config)
	{
		bool flag = config.damagePoint < 0.0 || _previewHealthRatio > config.damagePoint;
		return $"生命 {_previewHealthRatio:P0} · {(flag ? "护甲显示" : "护甲移除")} · {EmptyToPlaceholder(config.replaceMethod)}";
	}

	private static string BuildCustomConfigPreviewStatus(CharacterCustomConfig config)
	{
		return $"{EmptyToPlaceholder(config.customName)} · {EmptyToPlaceholder(config.type)} · {config.damagePointChangeMediaTexturePaths?.Count ?? 0} 张阶段贴图";
	}

	private string BuildArmorTypePreviewStatus(TowerDefenseArmorTypeData data)
	{
		int num = 0;
		if (data.stagePersontage != null)
		{
			foreach (double item in data.stagePersontage)
			{
				if (_previewHealthRatio <= item)
				{
					num++;
				}
			}
		}
		return $"耐久 {data.damagePoint:0.##} · 生命 {_previewHealthRatio:P0} · 阶段 {num}/{data.stagePersontage?.Count ?? 0}";
	}

	private static bool IsAggregateCharacterData(Resource resource)
	{
		if (resource is CharacterArmorData || resource is CharacterCustomData || resource is CharacterDamagePointData)
		{
			return true;
		}
		return false;
	}

	private static bool IsSupportedCharacterDataResource(Resource resource)
	{
		bool flag = IsAggregateCharacterData(resource);
		if (!flag)
		{
			bool flag2 = ((resource is ArmorSlotConfig || resource is CharacterCustomConfig || resource is CharacterDamagePointConfig || resource is TowerDefenseArmorTypeData) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private void UpdateSummary()
	{
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = BuildDataSummary(_editingData);
		}
		if (GodotObject.IsInstanceValid(_cacheSummaryLabel))
		{
			_cacheSummaryLabel.Text = BuildCacheSummary(_editingData);
		}
		UpdateVisualPreview();
	}

	private static string BuildDataSummary(Resource resource)
	{
		if (!(resource is CharacterArmorData characterArmorData))
		{
			if (!(resource is CharacterCustomData characterCustomData))
			{
				if (resource is CharacterDamagePointData characterDamagePointData)
				{
					return $"受伤点数据: {characterDamagePointData.damagePointList?.Count ?? 0} 个受伤阶段";
				}
				return "角色数据";
			}
			return $"自定义外观数据: {characterCustomData.customList?.Count ?? 0} 个外观";
		}
		return $"护甲数据: {characterArmorData.armorList?.Count ?? 0} 个护甲槽";
	}

	private static string BuildCacheSummary(Resource resource)
	{
		if (!(resource is CharacterArmorData characterArmorData))
		{
			if (!(resource is CharacterCustomData characterCustomData))
			{
				if (resource is CharacterDamagePointData characterDamagePointData)
				{
					return $"派生缓存: damagePointDictionary={characterDamagePointData.damagePointDictionary?.Count ?? 0}, open={characterDamagePointData.fliterOpenAll?.Count ?? 0}, close={characterDamagePointData.fliterCloseAll?.Count ?? 0}";
				}
				return "";
			}
			return $"派生缓存: customDictionary={characterCustomData.customDictionary?.Count ?? 0}, open={characterCustomData.fliterOpenAll?.Count ?? 0}, close={characterCustomData.fliterCloseAll?.Count ?? 0}";
		}
		return $"派生缓存: armorDictionary={characterArmorData.armorDictionary?.Count ?? 0}, all/open/close filter={characterArmorData.fliterAllDictionary?.Count ?? 0}/{characterArmorData.fliterOpenDictionary?.Count ?? 0}/{characterArmorData.fliterCloseDictionary?.Count ?? 0}";
	}

	private Control AddBoundLineEdit(string nodeName, string label, Resource resource, StringName property, Func<string, Variant> convert = null)
	{
		LineEdit edit = new LineEdit
		{
			Name = nodeName,
			Text = resource.Get(property).AsString(),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		bool submittedSinceLastPreview = false;
		edit.FocusEntered += delegate
		{
			submittedSinceLastPreview = false;
			_entryBinding.BeginEdit(resource, property);
		};
		edit.TextChanged += delegate(string text)
		{
			submittedSinceLastPreview = false;
			_entryBinding.PreviewValue(resource, property, ReadValue(text));
			LightweightCharacterDataPreview();
		};
		edit.TextSubmitted += delegate(string text)
		{
			_entryBinding.CommitEdit(resource, property, ReadValue(text), $"修改 {property}", this, "RefreshCharacterDataEditorFromHistory");
			submittedSinceLastPreview = true;
		};
		edit.FocusExited += delegate
		{
			if (submittedSinceLastPreview)
			{
				submittedSinceLastPreview = false;
			}
			else
			{
				_entryBinding.CommitEdit(resource, property, ReadValue(edit.Text), $"修改 {property}", this, "RefreshCharacterDataEditorFromHistory");
			}
		};
		return WrapField(label, edit);
		Variant ReadValue(string text)
		{
			Func<string, Variant> func = convert;
			if (func == null)
			{
				string from = text ?? "";
				return Variant.From(in from);
			}
			return func(text);
		}
	}

	private Control AddBoundTextEdit(string nodeName, string label, Resource resource, StringName property)
	{
		TextEdit edit = new TextEdit
		{
			Name = nodeName,
			Text = resource.Get(property).AsString(),
			CustomMinimumSize = new Vector2(0f, 78f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		edit.FocusEntered += delegate
		{
			_entryBinding.BeginEdit(resource, property);
		};
		edit.TextChanged += delegate
		{
			_entryBinding.PreviewValue(resource, property, edit.Text);
			LightweightCharacterDataPreview();
		};
		edit.FocusExited += delegate
		{
			_entryBinding.CommitEdit(resource, property, edit.Text, $"修改 {property}", this, "RefreshCharacterDataEditorFromHistory");
		};
		return WrapField(label, edit);
	}

	private Control AddBoundSpinBox(string nodeName, string label, Resource resource, StringName property, double min, double max, double step)
	{
		SpinBox spinBox = new SpinBox
		{
			Name = nodeName,
			MinValue = min,
			MaxValue = max,
			Step = step,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(120f, 0f)
		};
		spinBox.GetLineEdit().Alignment = HorizontalAlignment.Right;
		_entryBinding.BindNumber(spinBox, resource, property, LightweightCharacterDataPreview, this, "RefreshCharacterDataEditorFromHistory");
		return WrapField(label, spinBox);
	}

	private Control AddBoundVector2Field(string nodeName, string label, Resource resource, StringName property)
	{
		Vector2 vector = resource.Get(property).AsVector2();
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = nodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		SpinBox x = CreateVectorSpinBox("X", vector.X);
		SpinBox y = CreateVectorSpinBox("Y", vector.Y);
		Action value = delegate
		{
			_entryBinding.BeginEdit(resource, property);
		};
		Action preview = delegate
		{
			_entryBinding.PreviewValue(resource, property, ReadValue());
			LightweightCharacterDataPreview();
		};
		Action value2 = delegate
		{
			_entryBinding.CommitEdit(resource, property, ReadValue(), $"修改 {property}", this, "RefreshCharacterDataEditorFromHistory");
		};
		x.FocusEntered += value;
		y.FocusEntered += value;
		x.ValueChanged += delegate
		{
			preview();
		};
		y.ValueChanged += delegate
		{
			preview();
		};
		x.FocusExited += value2;
		y.FocusExited += value2;
		hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
		return WrapField(label, hBoxContainer);
		Variant ReadValue()
		{
			return Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value));
		}
	}

	public void RefreshCharacterDataEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || (!xWUndoRedoManager.IsUndoing() && !xWUndoRedoManager.IsRedoing()) || base.CanvasGrid == null)
		{
			return;
		}
		_metadataBinding?.Dispose();
		_entryBinding?.Dispose();
		NormalizeCharacterData(_editingData);
		ClampSelectedIndex(_editingData);
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderCustomVisualPreset(null);
	}

	private static Control AddLineEdit(string nodeName, string label, string value, Action<string> changed)
	{
		LineEdit lineEdit = new LineEdit
		{
			Name = nodeName,
			Text = (value ?? ""),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		lineEdit.TextChanged += delegate(string text)
		{
			changed?.Invoke(text);
		};
		return WrapField(label, lineEdit);
	}

	private static Control AddTextEdit(string nodeName, string label, string value, Action<string> changed)
	{
		TextEdit edit = new TextEdit
		{
			Name = nodeName,
			Text = (value ?? ""),
			CustomMinimumSize = new Vector2(0f, 78f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		edit.TextChanged += delegate
		{
			changed?.Invoke(edit.Text);
		};
		return WrapField(label, edit);
	}

	private static Control AddSpinBox(string nodeName, string label, double value, double min, double max, double step, Action<double> changed)
	{
		SpinBox spinBox = new SpinBox
		{
			Name = nodeName,
			MinValue = min,
			MaxValue = max,
			Step = step,
			Value = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(120f, 0f)
		};
		spinBox.GetLineEdit().Alignment = HorizontalAlignment.Right;
		spinBox.ValueChanged += delegate(double newValue)
		{
			changed?.Invoke(newValue);
		};
		return WrapField(label, spinBox);
	}

	private static Control AddBoolField(string nodeName, string label, bool value, Action<bool> changed)
	{
		CheckBox checkBox = new CheckBox
		{
			Name = nodeName,
			Text = "启用",
			ButtonPressed = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		checkBox.Toggled += delegate(bool newValue)
		{
			changed?.Invoke(newValue);
		};
		return WrapField(label, checkBox);
	}

	private static Control AddVector2Field(string nodeName, string label, Vector2 value, Action<Vector2> changed)
	{
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = nodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddThemeConstantOverride("separation", 4);
		SpinBox x = CreateVectorSpinBox("X", value.X);
		SpinBox y = CreateVectorSpinBox("Y", value.Y);
		x.ValueChanged += delegate
		{
			changed?.Invoke(new Vector2((float)x.Value, (float)y.Value));
		};
		y.ValueChanged += delegate
		{
			changed?.Invoke(new Vector2((float)x.Value, (float)y.Value));
		};
		hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
		return WrapField(label, hBoxContainer);
	}

	private static SpinBox CreateVectorSpinBox(string name, float value)
	{
		SpinBox spinBox = new SpinBox();
		spinBox.Name = name;
		spinBox.MinValue = -99999.0;
		spinBox.MaxValue = 99999.0;
		spinBox.Step = 0.1;
		spinBox.Value = value;
		spinBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		spinBox.GetLineEdit().Alignment = HorizontalAlignment.Right;
		return spinBox;
	}

	private static Control AddOptionField(string nodeName, string label, string value, string[] options, Action<string> changed)
	{
		OptionButton option = new OptionButton
		{
			Name = nodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		for (int i = 0; i < options.Length; i++)
		{
			option.AddItem(options[i], i);
			if (string.Equals(options[i], value, StringComparison.OrdinalIgnoreCase))
			{
				option.Select(i);
			}
		}
		option.ItemSelected += delegate(long index)
		{
			changed?.Invoke(option.GetItemText((int)index));
		};
		return WrapField(label, option);
	}

	private static Control AddResourcePicker(string nodeName, string label, string baseType, Resource value, Action<Resource> changed)
	{
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = nodeName;
		xWResourcePicker.Setup(baseType);
		xWResourcePicker.SetEditedResource(value);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		xWResourcePicker.ResourceChanged += delegate(Resource resource)
		{
			changed?.Invoke(resource);
		};
		return WrapField(label, xWResourcePicker);
	}

	private static Texture2D LoadTexturePreview(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path, "Texture2D"))
		{
			return null;
		}
		return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	private static Control WrapField(string label, Control editor)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(168f, 0f),
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static Label CreateSectionTitle(string text)
	{
		return new Label
		{
			Text = text,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static Label CreateEmptyEntryLabel(string text)
	{
		return new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static Button CreateToolbarButton(string text, Action pressed)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(54f, 28f);
		button.Pressed += delegate
		{
			pressed?.Invoke();
		};
		return button;
	}

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}
}
