using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCharacterVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterVisualEditorLayout.tscn";

	private const string CharacterBasicFieldsScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterBasicFields.tscn";

	private const string CharacterOverrideEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterOverrideEditorLayout.tscn";

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _characterBasicFieldsScene;

	private static PackedScene _characterOverrideEditorLayoutScene;

	private const string PropertyCategory = "category";

	private const string PropertyCreationMode = "creationMode";

	private const string PropertyBaseCharacterKey = "baseCharacterKey";

	private const string PropertyTargetCharacterKey = "targetCharacterKey";

	private const string PropertySceneOverride = "sceneOverride";

	private const string PropertySpriteOverride = "spriteOverride";

	private const string PropertyPacketPatch = "packetPatch";

	private const string PropertyCharacterConfigPatch = "characterConfigPatch";

	private const string PropertyComponentPatch = "componentPatch";

	private const string PropertyComponentList = "componentList";

	private const string PropertySpawnEvent = "spawnEvent";

	private const string PropertyDieEvent = "dieEvent";

	private const string PropertyPropertyChange = "propertyChange";

	private static readonly string[] CharacterCategories = new string[8] { "植物", "僵尸", "道具", "花瓶", "小推车", "物品", "墓碑", "弹坑" };

	private static readonly string[] CreationModes = new string[3] { "空白创建", "继承内置", "覆盖内置" };

	private static readonly HashSet<string> CharacterInspectorEditableProperties = new HashSet<string>
	{
		"resource_name", "resource_local_to_scene", "name", "hitpointsNearDeath", "hitpoints", "explosionHurt", "smashHurt", "dragHurt", "spikeHurt", "biteHurt",
		"canDragIntoWater", "canImitate", "canCopy", "warnningLineFliter", "sleepTime", "height", "damagePointData", "armorData", "customData", "ashScene",
		"homeWorld", "costRise", "cost", "costNight", "costMultiple", "packetCooldown", "startingCooldown", "plantCoverAll", "plantCoverSelf", "plantCover",
		"plantCoverRecycle", "plantCanHasSurround", "plantSurroundCanPlantWater", "plantSurroundCanHasSlot", "plantGridType", "plantGridOverrideType", "Flag/Collision", "Flag/Mask", "Flag/UnUseBuff", "Flag/PhysiqueType",
		"Flag/Elemet", "canUsePlantfood", "extendGrid", "extendCoverDictionary", "izm2Fliter", "physique", "attack", "smashAttack", "impactAudio", "preview",
		"weight", "wavePointCost", "canSpawnPlantfood", "excludeLineGridType", "spawnLineNeed", "type", "isLadder", "isShield", "mowerConfig", "category",
		"creationMode", "baseCharacterKey", "targetCharacterKey", "sceneOverride", "spriteOverride", "packetPatch", "characterConfigPatch", "componentPatch", "componentList", "spawnEvent",
		"dieEvent", "propertyChange", "invisible", "scale", "hitpointScale", "walkSpeedScale", "animeSpeedScale", "armor", "canMowerMove", "propertyName",
		"value"
	};

	private TowerDefenseCharacterConfig _editingCharacter;

	private SubViewport _characterViewport;

	private SubViewportContainer _characterViewportContainer;

	private TextureRect _characterPreviewBackground;

	private ColorRect _characterGroundShade;

	private Node2D _characterPreviewRoot;

	private TowerDefenseCharacter _runtimeCharacterPreview;

	private bool _characterPreviewUsesExternalScriptFallback;

	private Label _previewStatusLabel;

	private HSlider _previewHealthSlider;

	private Label _previewHealthValueLabel;

	private bool _draggingCharacterPreview;

	private Vector2 _characterPreviewPan;

	private float _characterPreviewZoom = 1f;

	private Label _previewNameLabel;

	private Label _previewCategoryLabel;

	private Label _previewModeLabel;

	private Label _previewHitpointsLabel;

	private Label _previewPacketLabel;

	private Label _previewResourceLabel;

	private ItemList _componentListView;

	private ItemList _componentPatchListView;

	private ItemList _spawnEventListView;

	private ItemList _dieEventListView;

	private ItemList _propertyChangeListView;

	private Tree _overrideDiffTree;

	private bool _updatingControls;

	private Timer _characterSaveTimer;

	private TowerDefenseCharacterOverride _editingCharacterOverride;

	private TowerDefenseCharacterPropertyChangeConfig _editingPropertyChange;

	private Resource _editingOverrideResource;

	private TowerDefenseCharacterConfig _overrideBaseCharacter;

	private TowerDefensePacketConfig _overrideBasePacket;

	private PackedScene _overrideRuntimeScene;

	private PackedScene _overrideSpriteScene;

	private PanelContainer _characterOverrideWorkbench;

	private SubViewport _overrideViewport;

	private SubViewportContainer _overrideViewportContainer;

	private TextureRect _overridePreviewBackground;

	private ColorRect _overrideGroundShade;

	private Node2D _overridePreviewCameraRoot;

	private Node2D _overrideBaseCharacterRoot;

	private Node2D _overriddenCharacterRoot;

	private TowerDefenseCharacter _overrideBaseRuntimeCharacter;

	private TowerDefenseCharacter _overriddenRuntimeCharacter;

	private Label _overrideContextLabel;

	private Label _overrideSourceLabel;

	private Label _overridePreviewStatusLabel;

	private Label _overrideZoomLabel;

	private Label _overrideEffectSummaryLabel;

	private Label _overrideSafetySummaryLabel;

	private Label _overrideSaveStateLabel;

	private ItemList _overridePropertyChangeList;

	private Button _selectOverrideBaseCharacterButton;

	private CheckButton _overrideInvisibleCheck;

	private SpinBox _overrideScaleSpinBox;

	private SpinBox _overrideHitpointScaleSpinBox;

	private SpinBox _overrideWalkSpeedMinSpinBox;

	private SpinBox _overrideWalkSpeedMaxSpinBox;

	private SpinBox _overrideAnimeSpeedMinSpinBox;

	private SpinBox _overrideAnimeSpeedMaxSpinBox;

	private CheckButton _overrideCanMowerMoveCheck;

	private Button _addOverridePropertyChangeButton;

	private Button _duplicateOverridePropertyChangeButton;

	private Button _moveOverridePropertyChangeUpButton;

	private Button _moveOverridePropertyChangeDownButton;

	private Button _removeOverridePropertyChangeButton;

	private LineEdit _overridePropertyNameLineEdit;

	private LineEdit _overridePropertyValueLineEdit;

	private Label _overridePropertyValueTypeLabel;

	private bool _draggingOverridePreview;

	private Vector2 _overridePreviewPan;

	private float _overridePreviewZoom = 1f;

	private string _overrideSelectedPacketName = "";

	private string _overrideResolvedPacketName = "";

	private ulong _overridePreviewResourceId;

	private int _selectedOverridePropertyChangeIndex = -1;

	private XWGameplayResourcePickerWindow _overrideBaseCharacterPicker;

	private XWGameplayResourcePickerWindow _characterCatalogPicker;

	private readonly List<XWVisualSegmentedOption> _characterSegmentedOptions = new List<XWVisualSegmentedOption>();

	private XWVisualPropertyBinding _overridePropertyBinding;

	public override void _Ready()
	{
		base._Ready();
		_characterSaveTimer = new Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_characterSaveTimer.Timeout += SavePendingCharacterResource;
		AddChild(_characterSaveTimer, forceReadableName: false, InternalMode.Disabled);
		EnableVisibilityGatedProcessing();
		RequestVisibilityGatedProcessing(requested: false);
	}

	public override void _ExitTree()
	{
		DisposeCharacterVisualChoices();
		if (GodotObject.IsInstanceValid(_characterSaveTimer))
		{
			_characterSaveTimer.Stop();
		}
		DisposeCharacterOverrideEditor(disposePicker: true);
		_editingCharacter = null;
		_runtimeCharacterPreview = null;
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeCharacterVisualChoices();
		DisposeCharacterOverrideEditor(disposePicker: false);
		_editingCharacter = null;
		_runtimeCharacterPreview = null;
		Resource currentResource = base.CurrentResource;
		if (!(currentResource is TowerDefenseCharacterConfig towerDefenseCharacterConfig))
		{
			if (!(currentResource is TowerDefenseCharacterOverride towerDefenseCharacterOverride))
			{
				if (currentResource is TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig)
				{
					_editingCharacter = null;
					_editingPropertyChange = towerDefenseCharacterPropertyChangeConfig;
					_editingOverrideResource = towerDefenseCharacterPropertyChangeConfig;
					RenderCharacterOverrideEditor(null, towerDefenseCharacterPropertyChangeConfig);
				}
			}
			else
			{
				_editingCharacter = null;
				_editingCharacterOverride = towerDefenseCharacterOverride;
				_editingOverrideResource = towerDefenseCharacterOverride;
				RenderCharacterOverrideEditor(towerDefenseCharacterOverride, null);
			}
		}
		else
		{
			_editingCharacter = towerDefenseCharacterConfig;
			RenderCharacterEditor(towerDefenseCharacterConfig);
		}
	}

	protected override HashSet<string> GetEmbeddedInspectorAllowedProperties(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if ((resource is TowerDefenseCharacterConfig || resource is TowerDefenseCharacterOverride || resource is TowerDefenseCharacterPropertyChangeConfig) ? true : false)
		{
			return CharacterInspectorEditableProperties;
		}
		return base.GetEmbeddedInspectorAllowedProperties(resource, path, descriptor);
	}

	protected override void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (!(obj is TowerDefenseCharacterConfig editingCharacter))
		{
			if (!(obj is TowerDefenseCharacterOverride editingCharacterOverride))
			{
				if (obj is TowerDefenseCharacterPropertyChangeConfig editingPropertyChange)
				{
					_editingPropertyChange = editingPropertyChange;
					RefreshCharacterOverrideWorkbench(rebuildPreview: true);
				}
			}
			else
			{
				_editingCharacterOverride = editingCharacterOverride;
				RefreshCharacterOverrideWorkbench(rebuildPreview: true);
			}
		}
		else
		{
			_editingCharacter = editingCharacter;
			UpdateCharacterPreview();
			RefreshComponentPanel();
			RefreshOverrideDiffPanel();
		}
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		SetPreviewRootProcessing(_characterPreviewRoot, visible);
		SetPreviewRootProcessing(_overrideBaseCharacterRoot, visible);
		SetPreviewRootProcessing(_overriddenCharacterRoot, visible);
	}

	private void RenderCharacterOverrideEditor(TowerDefenseCharacterOverride characterOverride, TowerDefenseCharacterPropertyChangeConfig propertyChange)
	{
		if (base.CanvasGrid != null && GodotObject.IsInstanceValid(_editingOverrideResource))
		{
			PrepareCharacterOverrideSession();
			base.CanvasGrid.Columns = 1;
			if (_characterOverrideEditorLayoutScene == null)
			{
				_characterOverrideEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterOverrideEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_characterOverrideWorkbench = _characterOverrideEditorLayoutScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_characterOverrideWorkbench))
			{
				base.CanvasGrid.AddChild(_characterOverrideWorkbench, forceReadableName: false, InternalMode.Disabled);
				BindCharacterOverrideScene(_characterOverrideWorkbench);
				_overrideBaseCharacter = ResolveOverrideBaseCharacter(out _overrideBasePacket, out var packetName, out var sourceDescription);
				UpdateCharacterOverrideContext(packetName, sourceDescription);
				PopulateCharacterOverrideControls();
				BindCharacterOverrideEditing();
				RebuildCharacterOverridePreview();
				CallDeferred("UpdateCharacterOverrideStageLayout");
			}
		}
	}

	private void PrepareCharacterOverrideSession()
	{
		ulong num = (GodotObject.IsInstanceValid(base.CurrentResource) ? base.CurrentResource.GetInstanceId() : 0);
		if (_overridePreviewResourceId != num)
		{
			_overridePreviewResourceId = num;
			_overrideSelectedPacketName = "";
			_overrideResolvedPacketName = "";
			_overridePreviewPan = Vector2.Zero;
			_overridePreviewZoom = 1f;
		}
	}

	private void BindCharacterOverrideScene(PanelContainer root)
	{
		_overrideViewport = root.GetNode<SubViewport>("%OverrideViewport");
		_overrideViewportContainer = root.GetNode<SubViewportContainer>("%ViewportContainer");
		_overridePreviewBackground = root.GetNode<TextureRect>("%GameBackground");
		_overrideGroundShade = root.GetNode<ColorRect>("%GroundShade");
		_overridePreviewCameraRoot = root.GetNode<Node2D>("%PreviewCameraRoot");
		_overrideBaseCharacterRoot = root.GetNode<Node2D>("%BaseCharacterRoot");
		_overriddenCharacterRoot = root.GetNode<Node2D>("%OverriddenCharacterRoot");
		_overrideContextLabel = root.GetNode<Label>("%OverrideContextLabel");
		_overrideSourceLabel = root.GetNode<Label>("%OverrideSourceLabel");
		_overridePreviewStatusLabel = root.GetNode<Label>("%PreviewStatusLabel");
		_overrideZoomLabel = root.GetNode<Label>("%ZoomLabel");
		_overrideEffectSummaryLabel = root.GetNode<Label>("%EffectSummaryLabel");
		_overrideSafetySummaryLabel = root.GetNode<Label>("%SafetySummaryLabel");
		_overrideSaveStateLabel = root.GetNode<Label>("%SaveStateLabel");
		_overridePropertyChangeList = root.GetNode<ItemList>("%PropertyChangeList");
		_selectOverrideBaseCharacterButton = root.GetNode<Button>("%SelectBaseCharacterButton");
		_overrideInvisibleCheck = root.GetNode<CheckButton>("%InvisibleCheck");
		_overrideScaleSpinBox = root.GetNode<SpinBox>("%ScaleSpinBox");
		_overrideHitpointScaleSpinBox = root.GetNode<SpinBox>("%HitpointScaleSpinBox");
		_overrideWalkSpeedMinSpinBox = root.GetNode<SpinBox>("%WalkSpeedMinSpinBox");
		_overrideWalkSpeedMaxSpinBox = root.GetNode<SpinBox>("%WalkSpeedMaxSpinBox");
		_overrideAnimeSpeedMinSpinBox = root.GetNode<SpinBox>("%AnimeSpeedMinSpinBox");
		_overrideAnimeSpeedMaxSpinBox = root.GetNode<SpinBox>("%AnimeSpeedMaxSpinBox");
		_overrideCanMowerMoveCheck = root.GetNode<CheckButton>("%CanMowerMoveCheck");
		_addOverridePropertyChangeButton = root.GetNode<Button>("%AddPropertyChangeButton");
		_duplicateOverridePropertyChangeButton = root.GetNode<Button>("%DuplicatePropertyChangeButton");
		_moveOverridePropertyChangeUpButton = root.GetNode<Button>("%MovePropertyChangeUpButton");
		_moveOverridePropertyChangeDownButton = root.GetNode<Button>("%MovePropertyChangeDownButton");
		_removeOverridePropertyChangeButton = root.GetNode<Button>("%RemovePropertyChangeButton");
		_overridePropertyNameLineEdit = root.GetNode<LineEdit>("%PropertyNameLineEdit");
		_overridePropertyValueLineEdit = root.GetNode<LineEdit>("%PropertyValueLineEdit");
		_overridePropertyValueTypeLabel = root.GetNode<Label>("%PropertyValueTypeLabel");
		_overrideViewportContainer.GuiInput += OnCharacterOverridePreviewGuiInput;
		_overrideViewportContainer.Resized += UpdateCharacterOverrideStageLayout;
		root.GetNode<Button>("%ResetViewButton").Pressed += ResetCharacterOverridePreviewView;
		_selectOverrideBaseCharacterButton.Pressed += OpenOverrideBaseCharacterPicker;
		_overridePropertyChangeList.ItemActivated += OpenOverridePropertyChange;
		_overridePropertyChangeList.ItemSelected += SelectOverridePropertyChange;
		_addOverridePropertyChangeButton.Pressed += AddCharacterOverridePropertyChange;
		_duplicateOverridePropertyChangeButton.Pressed += DuplicateSelectedCharacterOverridePropertyChange;
		_moveOverridePropertyChangeUpButton.Pressed += delegate
		{
			MoveSelectedCharacterOverridePropertyChange(-1);
		};
		_moveOverridePropertyChangeDownButton.Pressed += delegate
		{
			MoveSelectedCharacterOverridePropertyChange(1);
		};
		_removeOverridePropertyChangeButton.Pressed += RemoveSelectedCharacterOverridePropertyChange;
	}

	private TowerDefenseCharacterConfig ResolveOverrideBaseCharacter(out TowerDefensePacketConfig packet, out string packetName, out string sourceDescription)
	{
		packet = null;
		packetName = "";
		sourceDescription = "未选择基础角色";
		_overrideRuntimeScene = null;
		_overrideSpriteScene = null;
		if (base.CurrentEditContext?.OwnerResource is TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig && !string.IsNullOrWhiteSpace(towerDefenseLevelPreSpawnConfig.packetName))
		{
			packetName = towerDefenseLevelPreSpawnConfig.packetName;
			sourceDescription = $"关卡预生成角色 · 格子 {towerDefenseLevelPreSpawnConfig.gridPos.X + 1},{towerDefenseLevelPreSpawnConfig.gridPos.Y + 1}";
		}
		else if (!string.IsNullOrWhiteSpace(_overrideSelectedPacketName))
		{
			packetName = _overrideSelectedPacketName;
			sourceDescription = "工作台临时预览选择（不会写入覆盖资源）";
		}
		else if (!string.IsNullOrWhiteSpace(base.CurrentEditContext?.PreviewKey))
		{
			packetName = base.CurrentEditContext.PreviewKey;
			sourceDescription = "来自上级角色覆盖工作台的预览上下文";
		}
		if (string.IsNullOrWhiteSpace(packetName))
		{
			_overrideResolvedPacketName = "";
			return null;
		}
		_overrideResolvedPacketName = packetName;
		if (ResourceManager.Instance != null)
		{
			Variant packet2 = ResourceManager.Instance.GetPacket(packetName);
			if (packet2.VariantType == Variant.Type.Object && packet2.AsGodotObject() is TowerDefensePacketConfig towerDefensePacketConfig && GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				packet = towerDefensePacketConfig;
				if (GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig))
				{
					_overrideRuntimeScene = ResourceManager.Instance.GetCharacterScene(towerDefensePacketConfig.characterConfig.name);
				}
			}
		}
		if (!GodotObject.IsInstanceValid(packet) && !TryLoadPacketFromCharacterRegistry(packetName, out packet, out _overrideRuntimeScene, out _overrideSpriteScene))
		{
			sourceDescription += " · 未在角色注册表中找到";
			return null;
		}
		if (!GodotObject.IsInstanceValid(_overrideRuntimeScene) && GodotObject.IsInstanceValid(packet?.characterConfig) && ResourceManager.Instance != null)
		{
			_overrideRuntimeScene = ResourceManager.Instance.GetCharacterScene(packet.characterConfig.name);
		}
		if (!GodotObject.IsInstanceValid(packet?.characterConfig))
		{
			return null;
		}
		return packet.characterConfig;
	}

	private static bool TryLoadPacketFromCharacterRegistry(string packetName, out TowerDefensePacketConfig packet, out PackedScene runtimeScene, out PackedScene spriteScene)
	{
		packet = null;
		runtimeScene = null;
		spriteScene = null;
		if (string.IsNullOrWhiteSpace(packetName))
		{
			return false;
		}
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", "", ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		foreach (Variant key2 in dictionary.Keys)
		{
			if (dictionary[key2].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key2].AsGodotDictionary();
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary3 = valueOrDefault.AsGodotDictionary();
			Variant key = default(Variant);
			bool flag = false;
			foreach (Variant key3 in dictionary3.Keys)
			{
				if (string.Equals(key3.AsString(), packetName, StringComparison.OrdinalIgnoreCase))
				{
					key = key3;
					flag = true;
					break;
				}
			}
			if (flag)
			{
				string path = dictionary3[key].AsString();
				packet = LoadRegistryResource<TowerDefensePacketConfig>(path);
				runtimeScene = LoadRegistryResource<PackedScene>(dictionary2.GetValueOrDefault("Scene", "").AsString());
				spriteScene = LoadRegistryResource<PackedScene>(dictionary2.GetValueOrDefault("Sprite", "").AsString());
				return GodotObject.IsInstanceValid(packet);
			}
		}
		return false;
	}

	private static T LoadRegistryResource<T>(string path) where T : Resource
	{
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Reuse);
	}

	private void OpenOverrideBaseCharacterPicker()
	{
		if (!GodotObject.IsInstanceValid(_overrideBaseCharacterPicker))
		{
			_overrideBaseCharacterPicker = XWGameplayResourcePickerWindow.Create();
			if (!GodotObject.IsInstanceValid(_overrideBaseCharacterPicker))
			{
				return;
			}
			AddChild(_overrideBaseCharacterPicker, forceReadableName: false, InternalMode.Disabled);
		}
		_overrideBaseCharacterPicker.Open(XWGameplayResourceKind.Card, _overrideSelectedPacketName, delegate(XWGameplayResourceChoice choice)
		{
			if (!(choice == null) && !string.IsNullOrWhiteSpace(choice.Key))
			{
				_overrideSelectedPacketName = choice.Key;
				_overrideBaseCharacter = ResolveOverrideBaseCharacter(out _overrideBasePacket, out var packetName, out var sourceDescription);
				UpdateCharacterOverrideContext(packetName, sourceDescription);
				RebuildCharacterOverridePreview();
			}
		});
	}

	private void UpdateCharacterOverrideContext(string packetName, string sourceDescription)
	{
		bool flag = GodotObject.IsInstanceValid(_overrideBaseCharacter);
		if (GodotObject.IsInstanceValid(_overrideContextLabel))
		{
			_overrideContextLabel.Text = (flag ? ("基础角色：" + _overrideBaseCharacter.name + " · 卡片 " + packetName) : "尚未解析基础角色 · 请从关卡进入或手动选择");
		}
		if (GodotObject.IsInstanceValid(_overrideSourceLabel))
		{
			_overrideSourceLabel.Text = "来源：" + sourceDescription;
		}
		if (GodotObject.IsInstanceValid(_selectOverrideBaseCharacterButton))
		{
			_selectOverrideBaseCharacterButton.Text = (flag ? "更换预览角色" : "选择基础角色");
		}
	}

	private void RebuildCharacterOverridePreview()
	{
		ClearPreviewRoot(_overrideBaseCharacterRoot);
		ClearPreviewRoot(_overriddenCharacterRoot);
		_overrideBaseRuntimeCharacter = null;
		_overriddenRuntimeCharacter = null;
		if (!GodotObject.IsInstanceValid(_overrideBaseCharacter) || !GodotObject.IsInstanceValid(_overrideBaseCharacterRoot) || !GodotObject.IsInstanceValid(_overriddenCharacterRoot))
		{
			if (GodotObject.IsInstanceValid(_overridePreviewStatusLabel))
			{
				_overridePreviewStatusLabel.Visible = true;
				_overridePreviewStatusLabel.Text = "从关卡的预生成角色打开可自动带入模型，独立资源请先选择基础角色。";
			}
			RefreshCharacterOverrideSummary();
			return;
		}
		Node node = BuildCharacterOverridePreviewNode();
		Node node2 = BuildCharacterOverridePreviewNode();
		if (node != null)
		{
			node.Name = "OriginalCharacterPreview";
			_overrideBaseCharacterRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_overrideBaseRuntimeCharacter = FindRuntimeCharacter(node);
		}
		if (node2 != null)
		{
			node2.Name = "OverriddenCharacterPreview";
			_overriddenCharacterRoot.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			_overriddenRuntimeCharacter = FindRuntimeCharacter(node2);
			ApplySafeCharacterOverridePreview();
		}
		bool flag = node != null && node2 != null;
		if (GodotObject.IsInstanceValid(_overridePreviewStatusLabel))
		{
			_overridePreviewStatusLabel.Visible = !flag;
			_overridePreviewStatusLabel.Text = (flag ? "" : "完整角色场景不可用，当前使用安全动画降级预览。");
		}
		SetPreviewRootProcessing(_overrideBaseCharacterRoot, IsVisibleInTree());
		SetPreviewRootProcessing(_overriddenCharacterRoot, IsVisibleInTree());
		RefreshCharacterOverrideSummary();
	}

	private Node BuildCharacterOverridePreviewNode()
	{
		Node node = InstantiateCharacterPreviewScene(_overrideRuntimeScene, _overrideBaseCharacter);
		if (node != null)
		{
			return node;
		}
		if (GodotObject.IsInstanceValid(_overrideSpriteScene))
		{
			return InstantiateCharacterSpriteFallback(_overrideSpriteScene);
		}
		return BuildSpriteFallbackPreview(_overrideBaseCharacter);
	}

	private static Node InstantiateCharacterPreviewScene(PackedScene scene, TowerDefenseCharacterConfig character)
	{
		if (!GodotObject.IsInstanceValid(scene) || !GodotObject.IsInstanceValid(character))
		{
			return null;
		}
		try
		{
			Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(node);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				node.Free();
				return null;
			}
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.config = character;
			return node;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Character override runtime preview failed: " + ex.Message);
			return null;
		}
	}

	private void ApplySafeCharacterOverridePreview()
	{
		if (!GodotObject.IsInstanceValid(_overriddenRuntimeCharacter))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_editingCharacterOverride))
		{
			if (_editingCharacterOverride.scale >= 0.0 && GodotObject.IsInstanceValid(_overriddenRuntimeCharacter.transformPoint))
			{
				_overriddenRuntimeCharacter.transformPoint.Scale = Vector2.One * (float)_editingCharacterOverride.scale;
			}
			if (_editingCharacterOverride.animeSpeedScale != new Vector2(-1f, -1f))
			{
				_overriddenRuntimeCharacter.timeScale = StableRangeMidpoint(_editingCharacterOverride.animeSpeedScale);
			}
			_overriddenRuntimeCharacter.Modulate = (_editingCharacterOverride.invisible ? new Color(0.75f, 0.9f, 1f, 0.28f) : Colors.White);
			if (_editingCharacterOverride.propertyChange == null)
			{
				return;
			}
			{
				foreach (TowerDefenseCharacterPropertyChangeConfig item in _editingCharacterOverride.propertyChange)
				{
					ApplySafePropertyChangePreview(item);
				}
				return;
			}
		}
		if (GodotObject.IsInstanceValid(_editingPropertyChange))
		{
			ApplySafePropertyChangePreview(_editingPropertyChange);
		}
	}

	private void ApplySafePropertyChangePreview(TowerDefenseCharacterPropertyChangeConfig change)
	{
		if (!GodotObject.IsInstanceValid(change) || !GodotObject.IsInstanceValid(_overriddenRuntimeCharacter) || string.IsNullOrWhiteSpace(change.propertyName) || !IsSafeCharacterPreviewProperty(change.propertyName))
		{
			return;
		}
		foreach (Dictionary property in _overriddenRuntimeCharacter.GetPropertyList())
		{
			if (string.Equals(property.GetValueOrDefault("name", "").AsString(), change.propertyName, StringComparison.Ordinal))
			{
				if (TryConvertSafeCharacterPreviewValue(property, change.value, out var previewValue))
				{
					_overriddenRuntimeCharacter.Set(change.propertyName, previewValue);
				}
				break;
			}
		}
	}

	private static bool TryConvertSafeCharacterPreviewValue(Dictionary property, Variant value, out Variant previewValue)
	{
		previewValue = default(Variant);
		Variant valueOrDefault = property.GetValueOrDefault("type");
		if (valueOrDefault.VariantType != Variant.Type.Int || value.VariantType == Variant.Type.Nil)
		{
			return false;
		}
		Variant.Type type = (Variant.Type)valueOrDefault.AsInt32();
		if (type == value.VariantType)
		{
			previewValue = value;
			return true;
		}
		if (type == Variant.Type.Float && value.VariantType == Variant.Type.Int)
		{
			previewValue = Variant.From<double>(value.AsDouble());
			return true;
		}
		if (type == Variant.Type.Int && value.VariantType == Variant.Type.Float)
		{
			previewValue = Variant.From<long>((long)Math.Round(value.AsDouble()));
			return true;
		}
		if (type == Variant.Type.String && value.VariantType == Variant.Type.StringName)
		{
			previewValue = Variant.From<string>(value.AsString());
			return true;
		}
		if (type == Variant.Type.StringName && value.VariantType == Variant.Type.String)
		{
			previewValue = Variant.From<StringName>(new StringName(value.AsString()));
			return true;
		}
		return false;
	}

	private static bool IsSafeCharacterPreviewProperty(string propertyName)
	{
		if (propertyName != null)
		{
			int length = propertyName.Length;
			if (length <= 9)
			{
				if (length != 4)
				{
					if (length == 9)
					{
						char c = propertyName[0];
						if (c != 'h')
						{
							if (c != 'i')
							{
								if (c == 't' && propertyName == "timeScale")
								{
									goto IL_00c6;
								}
							}
							else if (propertyName == "invisible")
							{
								goto IL_00c6;
							}
						}
						else if (propertyName == "hitpoints")
						{
							goto IL_00c6;
						}
					}
				}
				else if (propertyName == "cost")
				{
					goto IL_00c6;
				}
			}
			else if (length != 12)
			{
				if (length == 28 && propertyName == "previewDamagePointPersontage")
				{
					goto IL_00c6;
				}
			}
			else
			{
				char c = propertyName[0];
				if (c != 'c')
				{
					if (c == 'h' && propertyName == "hitpointsMax")
					{
						goto IL_00c6;
					}
				}
				else if (propertyName == "canMowerMove")
				{
					goto IL_00c6;
				}
			}
		}
		return false;
		IL_00c6:
		return true;
	}

	private static float StableRangeMidpoint(Vector2 range)
	{
		return (range.X + range.Y) * 0.5f;
	}

	private void PopulateCharacterOverrideControls()
	{
		bool flag = GodotObject.IsInstanceValid(_editingCharacterOverride);
		_updatingControls = true;
		try
		{
			_overrideInvisibleCheck.ButtonPressed = flag && _editingCharacterOverride.invisible;
			_overrideScaleSpinBox.Value = (flag ? _editingCharacterOverride.scale : (-1.0));
			_overrideHitpointScaleSpinBox.Value = (flag ? _editingCharacterOverride.hitpointScale : (-1.0));
			_overrideWalkSpeedMinSpinBox.Value = (flag ? ((double)_editingCharacterOverride.walkSpeedScale.X) : (-1.0));
			_overrideWalkSpeedMaxSpinBox.Value = (flag ? ((double)_editingCharacterOverride.walkSpeedScale.Y) : (-1.0));
			_overrideAnimeSpeedMinSpinBox.Value = (flag ? ((double)_editingCharacterOverride.animeSpeedScale.X) : (-1.0));
			_overrideAnimeSpeedMaxSpinBox.Value = (flag ? ((double)_editingCharacterOverride.animeSpeedScale.Y) : (-1.0));
			_overrideCanMowerMoveCheck.ButtonPressed = flag && _editingCharacterOverride.canMowerMove;
		}
		finally
		{
			_updatingControls = false;
		}
		_overrideInvisibleCheck.Disabled = !flag;
		_overrideScaleSpinBox.Editable = flag;
		_overrideHitpointScaleSpinBox.Editable = flag;
		_overrideWalkSpeedMinSpinBox.Editable = flag;
		_overrideWalkSpeedMaxSpinBox.Editable = flag;
		_overrideAnimeSpeedMinSpinBox.Editable = flag;
		_overrideAnimeSpeedMaxSpinBox.Editable = flag;
		_overrideCanMowerMoveCheck.Disabled = !flag;
		_addOverridePropertyChangeButton.Disabled = !flag;
		_duplicateOverridePropertyChangeButton.Disabled = !flag;
		_removeOverridePropertyChangeButton.Disabled = !flag;
		RefreshCharacterOverridePropertyList();
		RefreshCharacterOverrideSummary();
	}

	private void BindCharacterOverrideEditing()
	{
		_overridePropertyBinding?.Dispose();
		_overridePropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCharacterOverrideVisualPropertyEdited);
		if (GodotObject.IsInstanceValid(_editingCharacterOverride))
		{
			_overridePropertyBinding.BindToggle(_overrideInvisibleCheck, _editingCharacterOverride, "invisible", null);
			_overridePropertyBinding.BindNumber(_overrideScaleSpinBox, _editingCharacterOverride, "scale", null);
			_overridePropertyBinding.BindNumber(_overrideHitpointScaleSpinBox, _editingCharacterOverride, "hitpointScale", null);
			_overridePropertyBinding.BindVector2Range(_overrideWalkSpeedMinSpinBox, _overrideWalkSpeedMaxSpinBox, _editingCharacterOverride, "walkSpeedScale", null);
			_overridePropertyBinding.BindVector2Range(_overrideAnimeSpeedMinSpinBox, _overrideAnimeSpeedMaxSpinBox, _editingCharacterOverride, "animeSpeedScale", null);
			_overridePropertyBinding.BindToggle(_overrideCanMowerMoveCheck, _editingCharacterOverride, "canMowerMove", null);
		}
		TowerDefenseCharacterPropertyChangeConfig selectedCharacterOverridePropertyChange = GetSelectedCharacterOverridePropertyChange();
		bool flag = GodotObject.IsInstanceValid(selectedCharacterOverridePropertyChange);
		_overridePropertyNameLineEdit.Editable = flag;
		_overridePropertyValueLineEdit.Editable = flag;
		if (flag)
		{
			_overridePropertyBinding.BindText(_overridePropertyNameLineEdit, selectedCharacterOverridePropertyChange, "propertyName", null);
			_overridePropertyBinding.BindVariantText(_overridePropertyValueLineEdit, _overridePropertyValueTypeLabel, selectedCharacterOverridePropertyChange, "value", null);
		}
		else
		{
			_overridePropertyNameLineEdit.Text = "";
			_overridePropertyValueLineEdit.Text = "";
			_overridePropertyValueTypeLabel.Text = "类型：—";
		}
		UpdateCharacterOverridePropertyButtons();
	}

	private void OnCharacterOverrideVisualPropertyEdited(bool committed)
	{
		NotifyCurrentResourceEdited();
		RefreshCharacterOverridePropertyList();
		RefreshCharacterOverrideSummary();
		RebuildCharacterOverridePreview();
		if (GodotObject.IsInstanceValid(_overrideSaveStateLabel))
		{
			_overrideSaveStateLabel.Text = (committed ? "未保存 · 已加入撤销记录" : "未保存 · 正在实时预览");
		}
	}

	private void SelectOverridePropertyChange(long index)
	{
		_selectedOverridePropertyChangeIndex = (int)index;
		BindCharacterOverrideEditing();
	}

	private TowerDefenseCharacterPropertyChangeConfig GetSelectedCharacterOverridePropertyChange()
	{
		if (GodotObject.IsInstanceValid(_editingPropertyChange))
		{
			return _editingPropertyChange;
		}
		if (!GodotObject.IsInstanceValid(_editingCharacterOverride) || _editingCharacterOverride.propertyChange == null || _selectedOverridePropertyChangeIndex < 0 || _selectedOverridePropertyChangeIndex >= _editingCharacterOverride.propertyChange.Count)
		{
			return null;
		}
		return _editingCharacterOverride.propertyChange[_selectedOverridePropertyChangeIndex];
	}

	private void AddCharacterOverridePropertyChange()
	{
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _overridePropertyBinding != null)
		{
			TowerDefenseCharacterOverride editingCharacterOverride = _editingCharacterOverride;
			if (editingCharacterOverride.propertyChange == null)
			{
				editingCharacterOverride.propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig>();
			}
			Array<TowerDefenseCharacterPropertyChangeConfig> array = new Array<TowerDefenseCharacterPropertyChangeConfig>(_editingCharacterOverride.propertyChange);
			TowerDefenseCharacterPropertyChangeConfig item = new TowerDefenseCharacterPropertyChangeConfig
			{
				ResourceName = $"PropertyChange_{array.Count + 1}",
				ResourceLocalToScene = true,
				propertyName = "timeScale",
				value = 1.0
			};
			array.Add(item);
			_selectedOverridePropertyChangeIndex = array.Count - 1;
			_overridePropertyBinding.SetValue(_editingCharacterOverride, "propertyChange", array, "新增角色属性变化");
			RefreshCharacterOverridePropertyList();
			BindCharacterOverrideEditing();
		}
	}

	private void DuplicateSelectedCharacterOverridePropertyChange()
	{
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _editingCharacterOverride.propertyChange != null && _overridePropertyBinding != null)
		{
			TowerDefenseCharacterPropertyChangeConfig selectedCharacterOverridePropertyChange = GetSelectedCharacterOverridePropertyChange();
			if (GodotObject.IsInstanceValid(selectedCharacterOverridePropertyChange))
			{
				TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig = (selectedCharacterOverridePropertyChange.Duplicate(deep: true) as TowerDefenseCharacterPropertyChangeConfig) ?? new TowerDefenseCharacterPropertyChangeConfig
				{
					propertyName = selectedCharacterOverridePropertyChange.propertyName,
					value = selectedCharacterOverridePropertyChange.value
				};
				towerDefenseCharacterPropertyChangeConfig.ResourceLocalToScene = true;
				towerDefenseCharacterPropertyChangeConfig.ResourceName = (string.IsNullOrWhiteSpace(selectedCharacterOverridePropertyChange.ResourceName) ? $"PropertyChange_{_editingCharacterOverride.propertyChange.Count + 1}" : (selectedCharacterOverridePropertyChange.ResourceName + "_Copy"));
				Array<TowerDefenseCharacterPropertyChangeConfig> array = new Array<TowerDefenseCharacterPropertyChangeConfig>(_editingCharacterOverride.propertyChange);
				int num = Mathf.Clamp(_selectedOverridePropertyChangeIndex + 1, 0, array.Count);
				array.Insert(num, towerDefenseCharacterPropertyChangeConfig);
				_selectedOverridePropertyChangeIndex = num;
				_overridePropertyBinding.SetValue(_editingCharacterOverride, "propertyChange", array, "复制角色属性变化");
				RefreshCharacterOverridePropertyList();
				BindCharacterOverrideEditing();
			}
		}
	}

	private void RemoveSelectedCharacterOverridePropertyChange()
	{
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _editingCharacterOverride.propertyChange != null && _overridePropertyBinding != null && _selectedOverridePropertyChangeIndex >= 0 && _selectedOverridePropertyChangeIndex < _editingCharacterOverride.propertyChange.Count)
		{
			Array<TowerDefenseCharacterPropertyChangeConfig> array = new Array<TowerDefenseCharacterPropertyChangeConfig>(_editingCharacterOverride.propertyChange);
			array.RemoveAt(_selectedOverridePropertyChangeIndex);
			_selectedOverridePropertyChangeIndex = ((array.Count == 0) ? (-1) : Mathf.Min(_selectedOverridePropertyChangeIndex, array.Count - 1));
			_overridePropertyBinding.SetValue(_editingCharacterOverride, "propertyChange", array, "删除角色属性变化");
			RefreshCharacterOverridePropertyList();
			BindCharacterOverrideEditing();
		}
	}

	private void MoveSelectedCharacterOverridePropertyChange(int direction)
	{
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _editingCharacterOverride.propertyChange != null && _overridePropertyBinding != null && _selectedOverridePropertyChangeIndex >= 0)
		{
			int num = _selectedOverridePropertyChangeIndex + Math.Sign(direction);
			if (num >= 0 && num < _editingCharacterOverride.propertyChange.Count)
			{
				Array<TowerDefenseCharacterPropertyChangeConfig> array = new Array<TowerDefenseCharacterPropertyChangeConfig>(_editingCharacterOverride.propertyChange);
				TowerDefenseCharacterPropertyChangeConfig item = array[_selectedOverridePropertyChangeIndex];
				array.RemoveAt(_selectedOverridePropertyChangeIndex);
				array.Insert(num, item);
				_selectedOverridePropertyChangeIndex = num;
				_overridePropertyBinding.SetValue(_editingCharacterOverride, "propertyChange", array, (direction < 0) ? "上移角色属性变化" : "下移角色属性变化");
				RefreshCharacterOverridePropertyList();
				BindCharacterOverrideEditing();
			}
		}
	}

	private void UpdateCharacterOverridePropertyButtons()
	{
		bool flag = GodotObject.IsInstanceValid(_editingCharacterOverride);
		int valueOrDefault = (_editingCharacterOverride?.propertyChange?.Count).GetValueOrDefault();
		bool flag2 = flag && _selectedOverridePropertyChangeIndex >= 0 && _selectedOverridePropertyChangeIndex < valueOrDefault;
		if (GodotObject.IsInstanceValid(_addOverridePropertyChangeButton))
		{
			_addOverridePropertyChangeButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_duplicateOverridePropertyChangeButton))
		{
			_duplicateOverridePropertyChangeButton.Disabled = !flag2;
		}
		if (GodotObject.IsInstanceValid(_removeOverridePropertyChangeButton))
		{
			_removeOverridePropertyChangeButton.Disabled = !flag2;
		}
		if (GodotObject.IsInstanceValid(_moveOverridePropertyChangeUpButton))
		{
			_moveOverridePropertyChangeUpButton.Disabled = !flag2 || _selectedOverridePropertyChangeIndex == 0;
		}
		if (GodotObject.IsInstanceValid(_moveOverridePropertyChangeDownButton))
		{
			_moveOverridePropertyChangeDownButton.Disabled = !flag2 || _selectedOverridePropertyChangeIndex >= valueOrDefault - 1;
		}
	}

	private void RefreshCharacterOverridePropertyList()
	{
		if (!GodotObject.IsInstanceValid(_overridePropertyChangeList))
		{
			return;
		}
		_overridePropertyChangeList.Clear();
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _editingCharacterOverride.propertyChange != null)
		{
			for (int i = 0; i < _editingCharacterOverride.propertyChange.Count; i++)
			{
				TowerDefenseCharacterPropertyChangeConfig change = _editingCharacterOverride.propertyChange[i];
				_overridePropertyChangeList.AddItem(FormatPropertyChange(change, i));
			}
		}
		else if (GodotObject.IsInstanceValid(_editingPropertyChange))
		{
			_overridePropertyChangeList.AddItem(FormatPropertyChange(_editingPropertyChange, 0));
			_selectedOverridePropertyChangeIndex = 0;
		}
		if (_overridePropertyChangeList.ItemCount > 0)
		{
			_selectedOverridePropertyChangeIndex = Mathf.Clamp((_selectedOverridePropertyChangeIndex >= 0) ? _selectedOverridePropertyChangeIndex : 0, 0, _overridePropertyChangeList.ItemCount - 1);
			_overridePropertyChangeList.Select(_selectedOverridePropertyChangeIndex);
		}
		else
		{
			_selectedOverridePropertyChangeIndex = -1;
		}
		UpdateCharacterOverridePropertyButtons();
	}

	private static string FormatPropertyChange(TowerDefenseCharacterPropertyChangeConfig change, int index)
	{
		if (GodotObject.IsInstanceValid(change))
		{
			string value = (string.IsNullOrWhiteSpace(change.propertyName) ? "<未选择属性>" : change.propertyName);
			return $"{index + 1}. {value}  →  {change.value}";
		}
		return $"{index + 1}. <空属性变化>";
	}

	private void OpenOverridePropertyChange(long index)
	{
		TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig = null;
		if (GodotObject.IsInstanceValid(_editingCharacterOverride) && _editingCharacterOverride.propertyChange != null && index >= 0 && index < _editingCharacterOverride.propertyChange.Count)
		{
			towerDefenseCharacterPropertyChangeConfig = _editingCharacterOverride.propertyChange[(int)index];
		}
		else if (GodotObject.IsInstanceValid(_editingPropertyChange) && index == 0L)
		{
			towerDefenseCharacterPropertyChangeConfig = _editingPropertyChange;
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacterPropertyChangeConfig) && towerDefenseCharacterPropertyChangeConfig != base.CurrentResource)
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(towerDefenseCharacterPropertyChangeConfig, _editingCharacterOverride, towerDefenseCharacterPropertyChangeConfig.ResourcePath, base.CurrentResourcePath, "propertyChange", (int)index, "character_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, _overrideResolvedPacketName);
			XWEditorInterface.Instance?.EditResource(towerDefenseCharacterPropertyChangeConfig, context);
		}
	}

	private void RefreshCharacterOverrideWorkbench(bool rebuildPreview)
	{
		if (GodotObject.IsInstanceValid(_characterOverrideWorkbench))
		{
			_overridePropertyBinding?.Dispose();
			_overridePropertyBinding = null;
			PopulateCharacterOverrideControls();
			BindCharacterOverrideEditing();
			if (rebuildPreview)
			{
				RebuildCharacterOverridePreview();
			}
		}
	}

	private void RefreshCharacterOverrideSummary()
	{
		if (GodotObject.IsInstanceValid(_overrideEffectSummaryLabel))
		{
			_overrideEffectSummaryLabel.Text = (GodotObject.IsInstanceValid(_editingCharacterOverride) ? $"覆盖效果：模型 ×{FormatOverrideNumber(_editingCharacterOverride.scale)} · 生命 ×{FormatOverrideNumber(_editingCharacterOverride.hitpointScale)} · 动画 {FormatRange(_editingCharacterOverride.animeSpeedScale)}" : ("单条属性：" + (_editingPropertyChange?.propertyName ?? "<未设置>") + " → " + (_editingPropertyChange?.value.ToString() ?? "<空>")));
		}
		if (GodotObject.IsInstanceValid(_overrideSafetySummaryLabel))
		{
			int valueOrDefault = (_editingCharacterOverride?.armor?.Count).GetValueOrDefault();
			int valueOrDefault2 = (_editingCharacterOverride?.spawnEvent?.Count).GetValueOrDefault();
			int valueOrDefault3 = (_editingCharacterOverride?.dieEvent?.Count).GetValueOrDefault();
			_overrideSafetySummaryLabel.Text = $"安全预览：护甲 {valueOrDefault} · 生成事件 {valueOrDefault2} · 死亡事件 {valueOrDefault3}（事件不会在编辑器执行）";
		}
		if (GodotObject.IsInstanceValid(_overrideSaveStateLabel))
		{
			_overrideSaveStateLabel.Text = (GodotObject.IsInstanceValid(_overrideBaseCharacter) ? "游戏内对照已就绪" : "等待选择基础角色");
		}
	}

	private static string FormatOverrideNumber(double value)
	{
		if (!(value < 0.0))
		{
			return value.ToString("0.##");
		}
		return "沿用";
	}

	private static string FormatRange(Vector2 value)
	{
		if (!(value == new Vector2(-1f, -1f)))
		{
			return $"{value.X:0.##}–{value.Y:0.##}×";
		}
		return "沿用";
	}

	private void OnCharacterOverridePreviewGuiInput(InputEvent inputEvent)
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
			_overridePreviewZoom = Mathf.Clamp(_overridePreviewZoom * ((inputEventMouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.1f : 0.9f), 0.35f, 3f);
			UpdateCharacterOverridePreviewTransform();
			_overrideViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseButton inputEventMouseButton2 && inputEventMouseButton2.ButtonIndex == MouseButton.Left)
		{
			_draggingOverridePreview = inputEventMouseButton2.Pressed;
			_overrideViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _draggingOverridePreview)
		{
			_overridePreviewPan += inputEventMouseMotion.Relative;
			UpdateCharacterOverridePreviewTransform();
			_overrideViewportContainer.AcceptEvent();
		}
	}

	private void ResetCharacterOverridePreviewView()
	{
		_overridePreviewPan = Vector2.Zero;
		_overridePreviewZoom = 1f;
		UpdateCharacterOverridePreviewTransform();
	}

	private void UpdateCharacterOverridePreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_overridePreviewCameraRoot))
		{
			_overridePreviewCameraRoot.Position = _overridePreviewPan;
			_overridePreviewCameraRoot.Scale = Vector2.One * _overridePreviewZoom;
			if (GodotObject.IsInstanceValid(_overrideZoomLabel))
			{
				_overrideZoomLabel.Text = $"{_overridePreviewZoom:P0}";
			}
		}
	}

	private void UpdateCharacterOverrideStageLayout()
	{
		if (GodotObject.IsInstanceValid(_overrideViewport))
		{
			Vector2 size = new Vector2(_overrideViewport.Size.X, _overrideViewport.Size.Y);
			if (size.X <= 1f || size.Y <= 1f)
			{
				size = new Vector2(900f, 350f);
			}
			if (GodotObject.IsInstanceValid(_overridePreviewBackground))
			{
				_overridePreviewBackground.Size = size;
			}
			if (GodotObject.IsInstanceValid(_overrideGroundShade))
			{
				float num = size.Y * 0.73f;
				_overrideGroundShade.Position = new Vector2(0f, num);
				_overrideGroundShade.Size = new Vector2(size.X, size.Y - num);
			}
			if (GodotObject.IsInstanceValid(_overrideBaseCharacterRoot))
			{
				_overrideBaseCharacterRoot.Position = new Vector2(size.X * 0.25f, size.Y * 0.77f);
			}
			if (GodotObject.IsInstanceValid(_overriddenCharacterRoot))
			{
				_overriddenCharacterRoot.Position = new Vector2(size.X * 0.75f, size.Y * 0.77f);
			}
			UpdateCharacterOverridePreviewTransform();
		}
	}

	private static void SetPreviewRootProcessing(Node root, bool enabled)
	{
		if (GodotObject.IsInstanceValid(root))
		{
			root.ProcessMode = (ProcessModeEnum)(enabled ? 0 : 4);
		}
	}

	private static void ClearPreviewRoot(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		foreach (Node child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.Free();
		}
	}

	private void DisposeCharacterOverrideEditor(bool disposePicker)
	{
		_overridePropertyBinding?.Dispose();
		_overridePropertyBinding = null;
		ClearPreviewRoot(_overrideBaseCharacterRoot);
		ClearPreviewRoot(_overriddenCharacterRoot);
		_overrideBaseRuntimeCharacter = null;
		_overriddenRuntimeCharacter = null;
		_overrideBaseCharacter = null;
		_overrideBasePacket = null;
		_overrideRuntimeScene = null;
		_overrideSpriteScene = null;
		_overrideResolvedPacketName = "";
		_characterOverrideWorkbench = null;
		_overrideViewport = null;
		_overrideViewportContainer = null;
		_overridePreviewBackground = null;
		_overrideGroundShade = null;
		_overridePreviewCameraRoot = null;
		_overrideBaseCharacterRoot = null;
		_overriddenCharacterRoot = null;
		_overrideContextLabel = null;
		_overrideSourceLabel = null;
		_overridePreviewStatusLabel = null;
		_overrideZoomLabel = null;
		_overrideEffectSummaryLabel = null;
		_overrideSafetySummaryLabel = null;
		_overrideSaveStateLabel = null;
		_overridePropertyChangeList = null;
		_selectOverrideBaseCharacterButton = null;
		_overrideInvisibleCheck = null;
		_overrideScaleSpinBox = null;
		_overrideHitpointScaleSpinBox = null;
		_overrideWalkSpeedMinSpinBox = null;
		_overrideWalkSpeedMaxSpinBox = null;
		_overrideAnimeSpeedMinSpinBox = null;
		_overrideAnimeSpeedMaxSpinBox = null;
		_overrideCanMowerMoveCheck = null;
		_addOverridePropertyChangeButton = null;
		_duplicateOverridePropertyChangeButton = null;
		_moveOverridePropertyChangeUpButton = null;
		_moveOverridePropertyChangeDownButton = null;
		_removeOverridePropertyChangeButton = null;
		_overridePropertyNameLineEdit = null;
		_overridePropertyValueLineEdit = null;
		_overridePropertyValueTypeLabel = null;
		_editingCharacterOverride = null;
		_editingPropertyChange = null;
		_editingOverrideResource = null;
		_draggingOverridePreview = false;
		_selectedOverridePropertyChangeIndex = -1;
		if (GodotObject.IsInstanceValid(_overrideBaseCharacterPicker))
		{
			_overrideBaseCharacterPicker.Dismiss();
			if (disposePicker)
			{
				_overrideBaseCharacterPicker.Free();
				_overrideBaseCharacterPicker = null;
			}
		}
	}

	private void RenderCharacterEditor(TowerDefenseCharacterConfig character)
	{
		if (base.CanvasGrid == null)
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_editorLayoutScene == null)
		{
			_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			BindCharacterPreview(vBoxContainer, character);
			if (_characterBasicFieldsScene == null)
			{
				_characterBasicFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCharacterBasicFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer2 = _characterBasicFieldsScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer2))
			{
				vBoxContainer.GetNode<VBoxContainer>("%BasicHost").AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
				BindCharacterBasicFields(vBoxContainer2, character);
			}
			BindComponentPanel(vBoxContainer, character);
			BindOverrideDiffPanel(vBoxContainer, character);
			AddCharacterSummaryRows(character);
		}
	}

	private void BindCharacterPreview(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		_previewNameLabel = root.GetNode<Label>("%PreviewNameLabel");
		_previewCategoryLabel = root.GetNode<Label>("%PreviewCategoryLabel");
		_characterViewport = root.GetNode<SubViewport>("%CharacterViewport");
		_characterViewportContainer = root.GetNode<SubViewportContainer>("%ViewportContainer");
		_characterPreviewBackground = root.GetNode<TextureRect>("%GameBackground");
		_characterGroundShade = root.GetNode<ColorRect>("%GroundShade");
		_characterPreviewRoot = root.GetNode<Node2D>("%PreviewRoot");
		_previewStatusLabel = root.GetNode<Label>("%PreviewStatusLabel");
		_previewHealthSlider = root.GetNode<HSlider>("%HealthSlider");
		_previewHealthValueLabel = root.GetNode<Label>("%HealthValueLabel");
		_previewHitpointsLabel = root.GetNode<Label>("%HitpointsLabel");
		_previewPacketLabel = root.GetNode<Label>("%PacketLabel");
		_previewModeLabel = root.GetNode<Label>("%ModeLabel");
		_previewResourceLabel = root.GetNode<Label>("%ResourceLabel");
		_characterViewportContainer.GuiInput += OnCharacterPreviewGuiInput;
		_characterViewportContainer.Resized += UpdateCharacterPreviewStageLayout;
		_previewHealthSlider.ValueChanged += OnPreviewHealthChanged;
		root.GetNode<Button>("%ResetViewButton").Pressed += ResetCharacterPreviewView;
		RebuildCharacterPreview();
		UpdateCharacterPreview();
		CallDeferred("UpdateCharacterPreviewStageLayout");
	}

	private void BindCharacterBasicFields(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		if (!GodotObject.IsInstanceValid(root) || character == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			BindLineEdit(root.GetNode<LineEdit>("%CharacterNameLineEdit"), character.name, delegate(string text)
			{
				OnCharacterFieldChanged("name", Variant.From(in text));
			});
			OptionButton node = root.GetNode<OptionButton>("%CharacterCategoryOption");
			BindStringOption(node, ReadString(character, "category", GetCharacterCategory(character)), CharacterCategories, delegate(string value)
			{
				OnCharacterDiscreteFieldChanged("category", Variant.From(in value), refreshPreview: false);
			});
			MountSmallSegmentedOption(node, root.GetNode<HFlowContainer>("%CharacterCategorySegments"));
			OptionButton node2 = root.GetNode<OptionButton>("%CreationModeOption");
			BindStringOption(node2, ReadString(character, "creationMode", "空白创建"), CreationModes, delegate(string value)
			{
				OnCharacterDiscreteFieldChanged("creationMode", Variant.From(in value), refreshPreview: false);
			});
			MountSmallSegmentedOption(node2, root.GetNode<HFlowContainer>("%CreationModeSegments"));
			BindLineEdit(root.GetNode<LineEdit>("%BaseCharacterKeyLineEdit"), ReadString(character, "baseCharacterKey"), delegate(string text)
			{
				OnCharacterFieldChanged("baseCharacterKey", Variant.From(in text), refreshPreview: false);
			});
			BindLineEdit(root.GetNode<LineEdit>("%TargetCharacterKeyLineEdit"), ReadString(character, "targetCharacterKey"), delegate(string text)
			{
				OnCharacterFieldChanged("targetCharacterKey", Variant.From(in text), refreshPreview: false);
			});
			BindCharacterKeyCatalog(root, "%BaseCharacterKeyLineEdit", "%BaseCharacterSelectButton", "baseCharacterKey");
			BindCharacterKeyCatalog(root, "%TargetCharacterKeyLineEdit", "%TargetCharacterSelectButton", "targetCharacterKey");
			BindResourcePicker(root.GetNode<XWResourcePicker>("%SceneOverridePicker"), "PackedScene", ReadResource(character, "sceneOverride"), delegate(Resource resource)
			{
				OnCharacterDiscreteFieldChanged("sceneOverride", Variant.From(in resource), refreshPreview: true, rebuildPreview: true);
			});
			BindResourcePicker(root.GetNode<XWResourcePicker>("%SpriteOverridePicker"), "Texture2D", ReadResource(character, "spriteOverride"), delegate(Resource resource)
			{
				OnCharacterDiscreteFieldChanged("spriteOverride", Variant.From(in resource), refreshPreview: true, rebuildPreview: true);
			});
			BindResourcePicker(root.GetNode<XWResourcePicker>("%PacketPatchPicker"), "Resource", ReadResource(character, "packetPatch"), delegate(Resource resource)
			{
				OnCharacterDiscreteFieldChanged("packetPatch", Variant.From(in resource), refreshPreview: false);
			});
			BindResourcePicker(root.GetNode<XWResourcePicker>("%CharacterConfigPatchPicker"), "Resource", ReadResource(character, "characterConfigPatch"), delegate(Resource resource)
			{
				OnCharacterDiscreteFieldChanged("characterConfigPatch", Variant.From(in resource), refreshPreview: false);
			});
			BindCharacterResourceCatalog(root, "%SceneOverrideCatalogButton", "sceneOverride", "PackedScene", "角色场景", "res://addons/ModEditor/Icons/PackedScene.svg", rebuildPreview: true);
			BindCharacterResourceCatalog(root, "%SpriteOverrideCatalogButton", "spriteOverride", "Texture2D", "预览贴图", "res://addons/ModEditor/Icons/ClassIcon/Image.svg", rebuildPreview: true);
			BindCharacterResourceCatalog(root, "%PacketPatchCatalogButton", "packetPatch", "Resource", "卡牌补丁", "res://addons/ModEditor/Icons/ResourceCard.svg", rebuildPreview: false);
			BindCharacterResourceCatalog(root, "%CharacterConfigPatchCatalogButton", "characterConfigPatch", "Resource", "角色配置补丁", "res://addons/ModEditor/Icons/ResourceCharacter.svg", rebuildPreview: false);
			BindSpinBox(root.GetNode<SpinBox>("%HitpointsSpinBox"), character.hitpoints, delegate(double value)
			{
				OnCharacterFieldChanged("hitpoints", Variant.From(in value));
			});
			BindSpinBox(root.GetNode<SpinBox>("%NearDeathSpinBox"), character.hitpointsNearDeath, delegate(double value)
			{
				OnCharacterFieldChanged("hitpointsNearDeath", Variant.From(in value));
			});
			BindSpinBox(root.GetNode<SpinBox>("%CostSpinBox"), character.cost, delegate(double value)
			{
				OnCharacterFieldChanged("cost", Variant.From<int>((int)Math.Round(value)));
			});
			BindSpinBox(root.GetNode<SpinBox>("%CooldownSpinBox"), character.packetCooldown, delegate(double value)
			{
				OnCharacterFieldChanged("packetCooldown", Variant.From(in value));
			});
			BindSpinBox(root.GetNode<SpinBox>("%StartingCooldownSpinBox"), character.startingCooldown, delegate(double value)
			{
				OnCharacterFieldChanged("startingCooldown", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%CanImitateCheck"), character.canImitate, delegate(bool value)
			{
				OnCharacterFieldChanged("canImitate", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%CanCopyCheck"), character.canCopy, delegate(bool value)
			{
				OnCharacterFieldChanged("canCopy", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%CanDragIntoWaterCheck"), character.canDragIntoWater, delegate(bool value)
			{
				OnCharacterFieldChanged("canDragIntoWater", Variant.From(in value));
			});
			BindTypedCharacterFields(root, character);
			SetTypedCharacterFieldVisibility(root, character);
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void BindComponentPanel(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		_componentListView = root.GetNode<ItemList>("%ComponentList");
		_componentPatchListView = root.GetNode<ItemList>("%ComponentPatchList");
		_spawnEventListView = root.GetNode<ItemList>("%SpawnEventList");
		_dieEventListView = root.GetNode<ItemList>("%DieEventList");
		_propertyChangeListView = root.GetNode<ItemList>("%PropertyChangeList");
		RefreshComponentPanel(character);
	}

	private void BindOverrideDiffPanel(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		_overrideDiffTree = root.GetNode<Tree>("%OverrideDiffTree");
		_overrideDiffTree.SetColumnTitle(0, "属性");
		_overrideDiffTree.SetColumnTitle(1, "原版值");
		_overrideDiffTree.SetColumnTitle(2, "Mod 值");
		_overrideDiffTree.SetColumnTitle(3, "覆盖");
		RefreshOverrideDiffPanel(character);
	}

	private void BindTypedCharacterFields(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		if (!(character is TowerDefensePlantConfig towerDefensePlantConfig))
		{
			if (!(character is TowerDefenseZombieConfig towerDefenseZombieConfig))
			{
				if (!(character is TowerDefenseMowerConfig towerDefenseMowerConfig))
				{
					if (!(character is TowerDefenseVaseConfig towerDefenseVaseConfig))
					{
						if (character is TowerDefenseItemConfig towerDefenseItemConfig)
						{
							BindToggle(root.GetNode<BaseButton>("%IsLadderCheck"), towerDefenseItemConfig.isLadder, delegate(bool value)
							{
								OnCharacterFieldChanged("isLadder", Variant.From(in value));
							});
							BindToggle(root.GetNode<BaseButton>("%IsShieldCheck"), towerDefenseItemConfig.isShield, delegate(bool value)
							{
								OnCharacterFieldChanged("isShield", Variant.From(in value));
							});
						}
					}
					else
					{
						OptionButton node = root.GetNode<OptionButton>("%VaseTypeOption");
						BindEnumOption(node, towerDefenseVaseConfig.type, delegate(TowerDefenseEnum.VASE_TYPE value)
						{
							int from = (int)value;
							OnCharacterDiscreteFieldChanged("type", Variant.From(in from));
						});
						MountSmallSegmentedOption(node, root.GetNode<HFlowContainer>("%VaseTypeSegments"));
					}
				}
				else
				{
					BindResourcePicker(root.GetNode<XWResourcePicker>("%MowerConfigPicker"), "Resource", towerDefenseMowerConfig.mowerConfig, delegate(Resource resource)
					{
						OnCharacterDiscreteFieldChanged("mowerConfig", Variant.From(in resource), refreshPreview: false);
					});
				}
			}
			else
			{
				BindSpinBox(root.GetNode<SpinBox>("%AttackSpinBox"), towerDefenseZombieConfig.attack, delegate(double value)
				{
					OnCharacterFieldChanged("attack", Variant.From(in value));
				});
				BindSpinBox(root.GetNode<SpinBox>("%SmashAttackSpinBox"), towerDefenseZombieConfig.smashAttack, delegate(double value)
				{
					OnCharacterFieldChanged("smashAttack", Variant.From(in value));
				});
				BindSpinBox(root.GetNode<SpinBox>("%WeightSpinBox"), towerDefenseZombieConfig.weight, delegate(double value)
				{
					OnCharacterFieldChanged("weight", Variant.From<int>((int)Math.Round(value)));
				});
				BindSpinBox(root.GetNode<SpinBox>("%WavePointCostSpinBox"), towerDefenseZombieConfig.wavePointCost, delegate(double value)
				{
					OnCharacterFieldChanged("wavePointCost", Variant.From<int>((int)Math.Round(value)));
				});
				BindToggle(root.GetNode<BaseButton>("%ZombiePreviewCheck"), towerDefenseZombieConfig.preview, delegate(bool value)
				{
					OnCharacterFieldChanged("preview", Variant.From(in value));
				});
			}
		}
		else
		{
			BindToggle(root.GetNode<BaseButton>("%CanUsePlantfoodCheck"), towerDefensePlantConfig.canUsePlantfood, delegate(bool value)
			{
				OnCharacterFieldChanged("canUsePlantfood", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%Izn2FilterCheck"), towerDefensePlantConfig.izm2Fliter, delegate(bool value)
			{
				OnCharacterFieldChanged("izm2Fliter", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%PlantCoverAllCheck"), towerDefensePlantConfig.plantCoverAll, delegate(bool value)
			{
				OnCharacterFieldChanged("plantCoverAll", Variant.From(in value));
			});
			BindToggle(root.GetNode<BaseButton>("%PlantCoverSelfCheck"), towerDefensePlantConfig.plantCoverSelf, delegate(bool value)
			{
				OnCharacterFieldChanged("plantCoverSelf", Variant.From(in value));
			});
		}
	}

	private static void SetTypedCharacterFieldVisibility(VBoxContainer root, TowerDefenseCharacterConfig character)
	{
		root.GetNode<PanelContainer>("%PlantFields").Visible = character is TowerDefensePlantConfig;
		root.GetNode<PanelContainer>("%ZombieFields").Visible = character is TowerDefenseZombieConfig;
		root.GetNode<PanelContainer>("%MowerFields").Visible = character is TowerDefenseMowerConfig;
		root.GetNode<PanelContainer>("%VaseFields").Visible = character is TowerDefenseVaseConfig;
		root.GetNode<PanelContainer>("%ItemFields").Visible = character is TowerDefenseItemConfig;
	}

	private void RebuildCharacterPreview()
	{
		if (!GodotObject.IsInstanceValid(_characterPreviewRoot) || _editingCharacter == null)
		{
			return;
		}
		foreach (Node child in _characterPreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		_runtimeCharacterPreview = null;
		_characterPreviewUsesExternalScriptFallback = false;
		Node node = BuildRuntimeCharacterPreviewScene(_editingCharacter);
		if (node != null)
		{
			node.Name = "ConfiguredCharacterGamePreview";
			_characterPreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_runtimeCharacterPreview = FindRuntimeCharacter(node);
			ApplyCharacterPreviewState();
			if (GodotObject.IsInstanceValid(_previewStatusLabel))
			{
				_previewStatusLabel.Text = "完整游戏角色场景 · 拖拽移动 · 滚轮缩放";
			}
			return;
		}
		Node node2 = BuildSpriteFallbackPreview(_editingCharacter);
		if (node2 != null)
		{
			node2.Name = "CharacterSpriteFallbackPreview";
			_characterPreviewRoot.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewStatusLabel))
			{
				_previewStatusLabel.Text = (_characterPreviewUsesExternalScriptFallback ? "外部 C# 尚未注册到 Godot，当前使用同包游戏动画安全预览" : "未找到完整角色场景，当前为动画资源降级预览");
			}
			return;
		}
		ColorRect node3 = new ColorRect
		{
			Name = "CharacterFallbackPreview",
			Color = GetCategoryColor(GetCharacterCategory(_editingCharacter)),
			CustomMinimumSize = new Vector2(96f, 132f),
			Position = new Vector2(-48f, -106f)
		};
		_characterPreviewRoot.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
		_characterPreviewRoot.AddChild(new Label
		{
			Name = "CharacterFallbackLabel",
			Text = EmptyToPlaceholder(_editingCharacter.name),
			Position = new Vector2(-80f, 34f),
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(160f, 24f)
		}, forceReadableName: false, InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(_previewStatusLabel))
		{
			_previewStatusLabel.Text = "未找到角色场景或动画资源";
		}
	}

	private Node BuildRuntimeCharacterPreviewScene(TowerDefenseCharacterConfig character)
	{
		if (character == null)
		{
			return null;
		}
		if (!TryResolveCharacterRuntimeScene(character, out var characterScene))
		{
			return null;
		}
		if (HasExternalCSharpSceneScript(characterScene.ResourcePath))
		{
			_characterPreviewUsesExternalScriptFallback = true;
			return null;
		}
		try
		{
			Node node = characterScene.Instantiate(PackedScene.GenEditState.Disabled);
			XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(node);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				node.QueueFree();
				return null;
			}
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.config = character;
			return node;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Character editor runtime scene preview failed: " + ex.Message);
			return null;
		}
	}

	private static bool HasExternalCSharpSceneScript(string scenePath)
	{
		string text = ToAbsoluteCharacterPreviewFilePath(scenePath);
		if (string.IsNullOrWhiteSpace(text) || !File.Exists(text) || !string.Equals(Path.GetExtension(text), ".tscn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		foreach (string item in File.ReadLines(text))
		{
			string text2 = item.Trim();
			if (!text2.StartsWith("[ext_resource", StringComparison.OrdinalIgnoreCase) || !text2.Contains("type=\"Script\"", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int num = text2.IndexOf("path=\"", StringComparison.OrdinalIgnoreCase);
			if (num < 0)
			{
				continue;
			}
			num += "path=\"".Length;
			int num2 = text2.IndexOf('"', num);
			if (num2 > num)
			{
				string text3 = text2.Substring(num, num2 - num).Trim();
				if (text3.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !text3.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private Node BuildSpriteFallbackPreview(TowerDefenseCharacterConfig character)
	{
		if (TryResolveCharacterPreviewSpriteScene(character, out var spriteScene))
		{
			return InstantiateCharacterSpriteFallback(spriteScene);
		}
		if (ReadResource(character, "spriteOverride") is Texture2D texture)
		{
			return new Sprite2D
			{
				Texture = texture,
				Centered = true
			};
		}
		return null;
	}

	private bool TryResolveCharacterRuntimeScene(TowerDefenseCharacterConfig character, out PackedScene characterScene)
	{
		characterScene = ReadResource(character, "sceneOverride") as PackedScene;
		if (GodotObject.IsInstanceValid(characterScene))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(character?.name) && ResourceManager.Instance != null)
		{
			characterScene = ResourceManager.Instance.GetCharacterScene(character.name);
			if (GodotObject.IsInstanceValid(characterScene))
			{
				return true;
			}
		}
		string[] array = new string[2]
		{
			base.CurrentResourcePath,
			character?.ResourcePath
		};
		for (int i = 0; i < array.Length; i++)
		{
			string characterPackageDirectoryForPreview = GetCharacterPackageDirectoryForPreview(array[i]);
			if (string.IsNullOrWhiteSpace(characterPackageDirectoryForPreview))
			{
				continue;
			}
			foreach (string characterRuntimeSceneCandidate in GetCharacterRuntimeSceneCandidates(NormalizeCharacterPreviewPath(characterPackageDirectoryForPreview.PathJoin("Scene")), character?.name))
			{
				if (ResourceLoader.Exists(characterRuntimeSceneCandidate))
				{
					characterScene = ResourceLoader.Load<PackedScene>(characterRuntimeSceneCandidate, null, ResourceLoader.CacheMode.Reuse);
					if (GodotObject.IsInstanceValid(characterScene))
					{
						return true;
					}
				}
			}
		}
		characterScene = null;
		return false;
	}

	private static IEnumerable<string> GetCharacterRuntimeSceneCandidates(string sceneDir, string characterName)
	{
		if (string.IsNullOrWhiteSpace(sceneDir) || !DirAccess.DirExistsAbsolute(sceneDir))
		{
			yield break;
		}
		if (!string.IsNullOrWhiteSpace(characterName))
		{
			yield return NormalizeCharacterPreviewPath(sceneDir.PathJoin(characterName + ".tscn"));
			yield return NormalizeCharacterPreviewPath(sceneDir.PathJoin("TowerDefense" + characterName + ".tscn"));
		}
		string[] filesAt = DirAccess.GetFilesAt(sceneDir);
		foreach (string text in filesAt)
		{
			if (string.Equals(text.GetExtension(), "tscn", StringComparison.OrdinalIgnoreCase))
			{
				yield return NormalizeCharacterPreviewPath(sceneDir.PathJoin(text));
			}
		}
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

	private bool TryResolveCharacterPreviewSpriteScene(TowerDefenseCharacterConfig character, out PackedScene spriteScene)
	{
		spriteScene = null;
		if (TryResolveExplicitCharacterSpritePreviewScene(character, out spriteScene))
		{
			return true;
		}
		if (ReadResource(character, "sceneOverride") is PackedScene packedScene && TryResolveCharacterSceneMetadataSpriteScene(packedScene.ResourcePath, out spriteScene))
		{
			return true;
		}
		string text = FindCharacterPackagePreviewScene(character);
		if (!string.IsNullOrWhiteSpace(text) && ResourceLoader.Exists(text))
		{
			spriteScene = ResourceLoader.Load<PackedScene>(text, null, ResourceLoader.CacheMode.Reuse);
			return GodotObject.IsInstanceValid(spriteScene);
		}
		return false;
	}

	private bool TryResolveExplicitCharacterSpritePreviewScene(TowerDefenseCharacterConfig character, out PackedScene spriteScene)
	{
		spriteScene = null;
		if (!TryGetPackedScenePath(ReadResource(character, "spriteOverride"), out var path))
		{
			return false;
		}
		if (!IsCharacterPreviewSpriteScenePath(path))
		{
			GD.PushWarning("Character editor ignored non-sprite preview scene: " + path);
			return false;
		}
		if (!ResourceLoader.Exists(path))
		{
			return false;
		}
		spriteScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
		return GodotObject.IsInstanceValid(spriteScene);
	}

	private Node InstantiateCharacterSpriteFallback(PackedScene spriteScene)
	{
		if (!GodotObject.IsInstanceValid(spriteScene))
		{
			return null;
		}
		try
		{
			Node node = spriteScene.Instantiate(PackedScene.GenEditState.Disabled);
			DisableCharacterFallbackGameplayProcessing(node);
			return node;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Character editor safe sprite preview failed: " + ex.Message);
			return null;
		}
	}

	private static void DisableCharacterFallbackGameplayProcessing(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		node.ProcessMode = ProcessModeEnum.Disabled;
		node.SetProcess(enable: false);
		node.SetPhysicsProcess(enable: false);
		node.SetProcessInput(enable: false);
		node.SetProcessUnhandledInput(enable: false);
		node.SetProcessUnhandledKeyInput(enable: false);
		foreach (Node child in node.GetChildren())
		{
			DisableCharacterFallbackGameplayProcessing(child);
		}
	}

	private void OnPreviewHealthChanged(double value)
	{
		ApplyCharacterPreviewState();
	}

	private void ApplyCharacterPreviewState()
	{
		double num = (GodotObject.IsInstanceValid(_previewHealthSlider) ? Mathf.Clamp(_previewHealthSlider.Value / 100.0, 0.0, 1.0) : 1.0);
		if (GodotObject.IsInstanceValid(_previewHealthValueLabel))
		{
			_previewHealthValueLabel.Text = $"{num:P0}";
		}
		if (GodotObject.IsInstanceValid(_runtimeCharacterPreview))
		{
			_runtimeCharacterPreview.previewDamagePointPersontage = num;
		}
	}

	private void OnCharacterPreviewGuiInput(InputEvent inputEvent)
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
			_characterPreviewZoom = Mathf.Clamp(_characterPreviewZoom * ((inputEventMouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.1f : 0.9f), 0.35f, 3f);
			UpdateCharacterPreviewTransform();
			_characterViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseButton inputEventMouseButton2 && inputEventMouseButton2.ButtonIndex == MouseButton.Left)
		{
			_draggingCharacterPreview = inputEventMouseButton2.Pressed;
			_characterViewportContainer.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _draggingCharacterPreview)
		{
			_characterPreviewPan += inputEventMouseMotion.Relative;
			UpdateCharacterPreviewTransform();
			_characterViewportContainer.AcceptEvent();
		}
	}

	private void ResetCharacterPreviewView()
	{
		_characterPreviewPan = Vector2.Zero;
		_characterPreviewZoom = 1f;
		UpdateCharacterPreviewTransform();
	}

	private void UpdateCharacterPreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_characterPreviewRoot))
		{
			Vector2 vector = (GodotObject.IsInstanceValid(_characterViewport) ? new Vector2(_characterViewport.Size.X, _characterViewport.Size.Y) : new Vector2(720f, 340f));
			_characterPreviewRoot.Position = new Vector2(vector.X * 0.5f, vector.Y * 0.62f) + _characterPreviewPan;
			_characterPreviewRoot.Scale = Vector2.One * _characterPreviewZoom;
		}
	}

	private void UpdateCharacterPreviewStageLayout()
	{
		if (GodotObject.IsInstanceValid(_characterViewport))
		{
			Vector2 size = new Vector2(_characterViewport.Size.X, _characterViewport.Size.Y);
			if (size.X <= 1f || size.Y <= 1f)
			{
				size = new Vector2(720f, 340f);
			}
			if (GodotObject.IsInstanceValid(_characterPreviewBackground))
			{
				_characterPreviewBackground.Size = size;
			}
			if (GodotObject.IsInstanceValid(_characterGroundShade))
			{
				float num = size.Y * 0.72f;
				_characterGroundShade.Position = new Vector2(0f, num);
				_characterGroundShade.Size = new Vector2(size.X, size.Y - num);
			}
			UpdateCharacterPreviewTransform();
		}
	}

	private bool TryResolveCharacterSceneMetadataSpriteScene(string scenePath, out PackedScene spriteScene)
	{
		spriteScene = null;
		scenePath = NormalizeCharacterPreviewPath(scenePath);
		if (string.IsNullOrWhiteSpace(scenePath))
		{
			return false;
		}
		if (!TryReadCharacterSceneMetadataSpritePath(scenePath, out var spritePath))
		{
			return false;
		}
		spritePath = ResolveRelativeCharacterPath(scenePath, spritePath);
		if (string.IsNullOrWhiteSpace(spritePath) || !IsCharacterPreviewSpriteScenePath(spritePath) || !ResourceLoader.Exists(spritePath))
		{
			return false;
		}
		spriteScene = ResourceLoader.Load<PackedScene>(spritePath, null, ResourceLoader.CacheMode.Reuse);
		return GodotObject.IsInstanceValid(spriteScene);
	}

	private static bool TryReadCharacterSceneMetadataSpritePath(string scenePath, out string spritePath)
	{
		spritePath = "";
		string text = ToAbsoluteCharacterPreviewFilePath(scenePath);
		if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
		{
			return false;
		}
		foreach (string item in File.ReadLines(text))
		{
			int num = item.IndexOf("metadata/mod_character_sprite_scene", StringComparison.OrdinalIgnoreCase);
			if (num >= 0)
			{
				int num2 = item.IndexOf('"', num + "metadata/mod_character_sprite_scene".Length);
				int num3 = ((num2 < 0) ? (-1) : item.IndexOf('"', num2 + 1));
				if (num2 >= 0 && num3 > num2)
				{
					spritePath = item.Substring(num2 + 1, num3 - num2 - 1);
					return !string.IsNullOrWhiteSpace(spritePath);
				}
			}
		}
		return false;
	}

	private string FindCharacterPackagePreviewScene(TowerDefenseCharacterConfig character)
	{
		string[] array = new string[3]
		{
			base.CurrentResourcePath,
			character?.ResourcePath,
			(ReadResource(character, "sceneOverride") as PackedScene)?.ResourcePath
		};
		for (int i = 0; i < array.Length; i++)
		{
			string characterPackageDirectoryForPreview = GetCharacterPackageDirectoryForPreview(array[i]);
			if (string.IsNullOrWhiteSpace(characterPackageDirectoryForPreview))
			{
				continue;
			}
			string file = characterPackageDirectoryForPreview.GetFile();
			string[] array2 = new string[2]
			{
				character?.name,
				file
			};
			foreach (string text in array2)
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					string text2 = NormalizeCharacterPreviewPath(characterPackageDirectoryForPreview.PathJoin("Sprite").PathJoin(text + ".tscn"));
					if (ResourceLoader.Exists(text2))
					{
						return text2;
					}
				}
			}
		}
		return "";
	}

	private static bool TryGetPackedScenePath(Resource resource, out string path)
	{
		path = "";
		if (!(resource is PackedScene packedScene) || !GodotObject.IsInstanceValid(packedScene))
		{
			return false;
		}
		path = NormalizeCharacterPreviewPath(packedScene.ResourcePath);
		return !string.IsNullOrWhiteSpace(path);
	}

	private static bool IsCharacterPreviewSpriteScenePath(string path)
	{
		path = NormalizeCharacterPreviewPath(path);
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		if (path.Contains("/Resources/Characters/", StringComparison.OrdinalIgnoreCase) && path.Contains("/Sprite/", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(path.GetExtension(), "tscn", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string GetCharacterPackageDirectoryForPreview(string ownerPath)
	{
		ownerPath = NormalizeCharacterPreviewPath(ownerPath);
		if (string.IsNullOrWhiteSpace(ownerPath))
		{
			return "";
		}
		string baseDir = ownerPath.GetBaseDir();
		string file = baseDir.GetFile();
		if (string.Equals(file, "Config", StringComparison.OrdinalIgnoreCase) || string.Equals(file, "Scene", StringComparison.OrdinalIgnoreCase) || string.Equals(file, "Script", StringComparison.OrdinalIgnoreCase) || string.Equals(file, "Sprite", StringComparison.OrdinalIgnoreCase))
		{
			return baseDir.GetBaseDir();
		}
		if (ownerPath.EndsWith("/Config", StringComparison.OrdinalIgnoreCase) || ownerPath.EndsWith("/Scene", StringComparison.OrdinalIgnoreCase) || ownerPath.EndsWith("/Script", StringComparison.OrdinalIgnoreCase) || ownerPath.EndsWith("/Sprite", StringComparison.OrdinalIgnoreCase))
		{
			return ownerPath.GetBaseDir();
		}
		return baseDir;
	}

	private static string ResolveRelativeCharacterPath(string ownerPath, string path)
	{
		path = NormalizeCharacterPreviewPath(path);
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("uid://", StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(path))
		{
			return path;
		}
		ownerPath = NormalizeCharacterPreviewPath(ownerPath);
		if (string.IsNullOrWhiteSpace(ownerPath))
		{
			return path;
		}
		if (Path.IsPathRooted(ownerPath))
		{
			try
			{
				return NormalizeCharacterPreviewPath(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ownerPath) ?? "", path)));
			}
			catch
			{
				return NormalizeCharacterPreviewPath(ownerPath.GetBaseDir().PathJoin(path));
			}
		}
		return NormalizeCharacterPreviewPath(ownerPath.GetBaseDir().PathJoin(path));
	}

	private static string ToAbsoluteCharacterPreviewFilePath(string path)
	{
		path = NormalizeCharacterPreviewPath(path);
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return ProjectSettings.GlobalizePath(path);
		}
		return path;
	}

	private static string NormalizeCharacterPreviewPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Trim().Replace('\\', '/');
		}
		return "";
	}

	private void OnCharacterFieldChanged(string propertyName, Variant value, bool refreshPreview = true, bool rebuildPreview = false)
	{
		if (_updatingControls || !(base.CurrentResource is TowerDefenseCharacterConfig resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		Variant variant = ReadVariant(resource, propertyName);
		if (!variant.Equals(value))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				ApplyCharacterDiscreteFieldFromHistory(propertyName, value, refreshPreview, rebuildPreview);
				return;
			}
			xWUndoRedoManager.CreateAction("修改角色 " + propertyName, mergeMode: true);
			xWUndoRedoManager.AddDoMethod(this, "ApplyCharacterDiscreteFieldFromHistory", propertyName, value, refreshPreview, rebuildPreview);
			xWUndoRedoManager.AddDoMethod(this, "RefreshCharacterDiscreteSurfaceFromHistory");
			xWUndoRedoManager.AddUndoMethod(this, "ApplyCharacterDiscreteFieldFromHistory", propertyName, variant, refreshPreview, rebuildPreview);
			xWUndoRedoManager.AddUndoMethod(this, "RefreshCharacterDiscreteSurfaceFromHistory");
			xWUndoRedoManager.CommitAction();
		}
	}

	private void OnCharacterDiscreteFieldChanged(string propertyName, Variant value, bool refreshPreview = true, bool rebuildPreview = false)
	{
		if (_updatingControls || !(base.CurrentResource is TowerDefenseCharacterConfig resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		Variant variant = ReadVariant(resource, propertyName);
		if (!variant.Equals(value))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				ApplyCharacterDiscreteFieldFromHistory(propertyName, value, refreshPreview, rebuildPreview);
				return;
			}
			xWUndoRedoManager.CreateAction("修改角色 " + propertyName);
			xWUndoRedoManager.AddDoMethod(this, "ApplyCharacterDiscreteFieldFromHistory", propertyName, value, refreshPreview, rebuildPreview);
			xWUndoRedoManager.AddDoMethod(this, "RefreshCharacterDiscreteSurfaceFromHistory");
			xWUndoRedoManager.AddUndoMethod(this, "ApplyCharacterDiscreteFieldFromHistory", propertyName, variant, refreshPreview, rebuildPreview);
			xWUndoRedoManager.AddUndoMethod(this, "RefreshCharacterDiscreteSurfaceFromHistory");
			xWUndoRedoManager.CommitAction();
		}
	}

	public void ApplyCharacterDiscreteFieldFromHistory(string propertyName, Variant value, bool refreshPreview, bool rebuildPreview)
	{
		SetResourceProperty(propertyName, value);
		UpdateCharacterPreview();
		RefreshComponentPanel();
		RefreshOverrideDiffPanel();
		if (rebuildPreview)
		{
			RebuildCharacterPreview();
			return;
		}
		bool flag = refreshPreview;
		if (flag)
		{
			bool flag2 = ((propertyName == "category" || propertyName == "name") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			RebuildCharacterPreview();
		}
	}

	public void RefreshCharacterDiscreteSurfaceFromHistory()
	{
		if (GodotObject.IsInstanceValid(base.CurrentResource) && base.CurrentDescriptor != null)
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
			{
				LoadResource(base.CurrentResource, base.CurrentResourcePath, base.CurrentDescriptor, base.CurrentEditContext);
			}
		}
	}

	private void SetResourceProperty(string propertyName, Variant value)
	{
		if (!(base.CurrentResource is TowerDefenseCharacterConfig towerDefenseCharacterConfig) || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		if (!(propertyName == "mowerConfig"))
		{
			if (!(propertyName == "type") || !(towerDefenseCharacterConfig is TowerDefenseVaseConfig towerDefenseVaseConfig))
			{
				goto IL_006d;
			}
			towerDefenseVaseConfig.type = (TowerDefenseEnum.VASE_TYPE)value.AsInt32();
		}
		else
		{
			if (!(towerDefenseCharacterConfig is TowerDefenseMowerConfig towerDefenseMowerConfig))
			{
				goto IL_006d;
			}
			towerDefenseMowerConfig.mowerConfig = value.AsGodotObject() as MowerConfig;
		}
		goto IL_0075;
		IL_006d:
		SetFlexibleProperty(towerDefenseCharacterConfig, propertyName, value);
		goto IL_0075;
		IL_0075:
		towerDefenseCharacterConfig.EmitChanged();
		towerDefenseCharacterConfig.NotifyPropertyListChanged();
		if (GodotObject.IsInstanceValid(_characterSaveTimer))
		{
			_characterSaveTimer.Start();
		}
	}

	private void SavePendingCharacterResource()
	{
		if (GodotObject.IsInstanceValid(_editingCharacter))
		{
			SaveCharacterResource(_editingCharacter);
		}
	}

	private void SaveCharacterResource(TowerDefenseCharacterConfig character)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		Error error = ResourceSaver.Save(character, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Character editor save failed: {error} {currentResourcePath}");
		}
		else
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModProjectLayout.MakeSavedTextResourceReferencesPortable(currentResourcePath, text);
				XWModManifestSyncService.RegisterPath(text, currentResourcePath);
			}
		}
	}

	private void UpdateCharacterPreview()
	{
		if (_editingCharacter == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_previewNameLabel))
			{
				_previewNameLabel.Text = BuildNameText(_editingCharacter);
			}
			if (GodotObject.IsInstanceValid(_previewCategoryLabel))
			{
				_previewCategoryLabel.Text = BuildCategoryText(_editingCharacter);
			}
			if (GodotObject.IsInstanceValid(_previewHitpointsLabel))
			{
				_previewHitpointsLabel.Text = $"生命: {_editingCharacter.hitpoints:0.##}";
			}
			if (GodotObject.IsInstanceValid(_previewPacketLabel))
			{
				_previewPacketLabel.Text = "卡片: " + BuildPacketText(_editingCharacter);
			}
			if (GodotObject.IsInstanceValid(_previewModeLabel))
			{
				_previewModeLabel.Text = "创建: " + ReadString(_editingCharacter, "creationMode", "空白创建");
			}
			if (GodotObject.IsInstanceValid(_previewResourceLabel))
			{
				_previewResourceLabel.Text = "资源: " + FormatPath(base.CurrentResourcePath);
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshComponentPanel()
	{
		if (_editingCharacter != null)
		{
			RefreshComponentPanel(_editingCharacter);
		}
	}

	private void RefreshComponentPanel(TowerDefenseCharacterConfig character)
	{
		PopulateVariantList(_componentListView, ReadArray(character, "componentList"));
		PopulateVariantList(_componentPatchListView, ReadArray(character, "componentPatch"));
		PopulateVariantList(_spawnEventListView, ReadArray(character, "spawnEvent"));
		PopulateVariantList(_dieEventListView, ReadArray(character, "dieEvent"));
		PopulateVariantList(_propertyChangeListView, ReadArray(character, "propertyChange"));
	}

	private void RefreshOverrideDiffPanel()
	{
		if (_editingCharacter != null)
		{
			RefreshOverrideDiffPanel(_editingCharacter);
		}
	}

	private void RefreshOverrideDiffPanel(TowerDefenseCharacterConfig character)
	{
		if (GodotObject.IsInstanceValid(_overrideDiffTree))
		{
			_overrideDiffTree.Clear();
			TreeItem root = _overrideDiffTree.CreateItem();
			AppendDiffRow(root, "baseCharacterKey", ReadString(character, "baseCharacterKey"), originalFromKey: true);
			AppendDiffRow(root, "targetCharacterKey", ReadString(character, "targetCharacterKey"), originalFromKey: true);
			AppendDiffRow(root, "sceneOverride", FormatResource(ReadResource(character, "sceneOverride")), originalFromKey: false);
			AppendDiffRow(root, "spriteOverride", FormatResource(ReadResource(character, "spriteOverride")), originalFromKey: false);
			AppendDiffRow(root, "packetPatch", FormatResource(ReadResource(character, "packetPatch")), originalFromKey: false);
			AppendDiffRow(root, "characterConfigPatch", FormatResource(ReadResource(character, "characterConfigPatch")), originalFromKey: false);
			AppendDiffRow(root, "componentPatch", $"Array({ReadArray(character, "componentPatch").Count})", originalFromKey: false);
			AppendDiffRow(root, "propertyChange", $"Array({ReadArray(character, "propertyChange").Count})", originalFromKey: false);
		}
	}

	private void AppendDiffRow(TreeItem root, string propertyName, string modValue, bool originalFromKey)
	{
		TreeItem treeItem = _overrideDiffTree.CreateItem(root);
		string text = (originalFromKey ? EmptyToPlaceholder(ReadString(_editingCharacter, "baseCharacterKey")) : "<原版>");
		bool flag = !string.IsNullOrWhiteSpace(modValue) && modValue != EmptyToPlaceholder("");
		treeItem.SetText(0, propertyName);
		treeItem.SetText(1, text);
		treeItem.SetText(2, EmptyToPlaceholder(modValue));
		treeItem.SetText(3, flag ? "是" : "否");
	}

	private void AddCharacterSummaryRows(TowerDefenseCharacterConfig character)
	{
		AddItemIfMissing(base.PreviewList, BuildNameText(character));
		AddItemIfMissing(base.PreviewList, BuildCategoryText(character));
		AddItemIfMissing(base.PreviewList, $"生命/费用: {character.hitpoints:0.##} / {character.cost}");
		AddItemIfMissing(base.TimelineList, "创建方式 creationMode -> " + ReadString(character, "creationMode", "空白创建"));
		AddItemIfMissing(base.TimelineList, $"组件 componentList -> {ReadArray(character, "componentList").Count}");
		AddItemIfMissing(base.GraphList, "角色 -> sceneOverride -> " + FormatResource(ReadResource(character, "sceneOverride")));
		AddItemIfMissing(base.GraphList, "角色 -> spriteOverride -> " + FormatResource(ReadResource(character, "spriteOverride")));
		AddItemIfMissing(base.GraphList, $"角色 -> componentPatch -> {ReadArray(character, "componentPatch").Count}");
		AddItemIfMissing(base.ReferenceList, "baseCharacterKey -> " + ReadString(character, "baseCharacterKey"));
		AddItemIfMissing(base.ReferenceList, "targetCharacterKey -> " + ReadString(character, "targetCharacterKey"));
		AddItemIfMissing(base.ReferenceList, "packetPatch -> " + FormatResource(ReadResource(character, "packetPatch")));
		AddItemIfMissing(base.ReferenceList, "characterConfigPatch -> " + FormatResource(ReadResource(character, "characterConfigPatch")));
	}

	private void BindLineEdit(LineEdit edit, string value, Action<string> changed)
	{
		edit.Text = value ?? "";
		edit.TextChanged += delegate(string text)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(text);
			}
		};
	}

	private void MountSmallSegmentedOption(OptionButton option, HFlowContainer host)
	{
		if (!GodotObject.IsInstanceValid(option) || !GodotObject.IsInstanceValid(host) || option.ItemCount < 2 || option.ItemCount > 8)
		{
			if (GodotObject.IsInstanceValid(option))
			{
				option.Visible = true;
			}
		}
		else
		{
			XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(option, host);
			xWVisualSegmentedOption.Rebuild();
			_characterSegmentedOptions.Add(xWVisualSegmentedOption);
		}
	}

	private void DisposeCharacterVisualChoices()
	{
		foreach (XWVisualSegmentedOption characterSegmentedOption in _characterSegmentedOptions)
		{
			characterSegmentedOption?.Dispose();
		}
		_characterSegmentedOptions.Clear();
	}

	private void BindCharacterKeyCatalog(VBoxContainer root, NodePath editPath, NodePath buttonPath, string propertyName)
	{
		LineEdit edit = root.GetNode<LineEdit>(editPath);
		root.GetNode<Button>(buttonPath).Pressed += delegate
		{
			EnsureCharacterCatalogPicker();
			_characterCatalogPicker?.Open(XWGameplayResourceKind.Character, edit.Text, delegate(XWGameplayResourceChoice choice)
			{
				if (!(choice == null))
				{
					_updatingControls = true;
					edit.Text = choice.Key;
					_updatingControls = false;
					OnCharacterDiscreteFieldChanged(propertyName, Variant.From<string>(choice.Key), refreshPreview: false);
				}
			}, null, lockKind: true, "角色图鉴");
		};
	}

	private void BindCharacterResourceCatalog(VBoxContainer root, NodePath buttonPath, string propertyName, string className, string displayName, string iconPath, bool rebuildPreview)
	{
		string text = propertyName switch
		{
			"sceneOverride" => "%SceneOverridePicker", 
			"spriteOverride" => "%SpriteOverridePicker", 
			"packetPatch" => "%PacketPatchPicker", 
			"characterConfigPatch" => "%CharacterConfigPatchPicker", 
			_ => string.Empty, 
		};
		XWResourcePicker surfacePicker = (string.IsNullOrWhiteSpace(text) ? null : root.GetNodeOrNull<XWResourcePicker>(text));
		root.GetNode<Button>(buttonPath).Pressed += delegate
		{
			EnsureCharacterCatalogPicker();
			Resource resource = ReadResource(_editingCharacter, propertyName);
			_characterCatalogPicker?.OpenResourceLibrary(className, displayName, resource?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { className }, new string[3] { "Resources", "Scenes", "Asset" }, iconPath, delegate(XWGameplayResourceChoice choice)
			{
				Resource from = ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				surfacePicker?.SetEditedResource(from);
				OnCharacterDiscreteFieldChanged(propertyName, Variant.From(in from), refreshPreview: true, rebuildPreview);
			});
		};
	}

	private void EnsureCharacterCatalogPicker()
	{
		if (!GodotObject.IsInstanceValid(_characterCatalogPicker))
		{
			_characterCatalogPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_characterCatalogPicker))
			{
				AddChild(_characterCatalogPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void BindSpinBox(SpinBox spin, double value, Action<double> changed)
	{
		spin.Value = value;
		spin.ValueChanged += delegate(double newValue)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(newValue);
			}
		};
	}

	private void BindToggle(BaseButton button, bool value, Action<bool> changed)
	{
		button.ButtonPressed = value;
		button.Toggled += delegate(bool newValue)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(newValue);
			}
		};
	}

	private void BindStringOption(OptionButton option, string value, string[] values, Action<string> changed)
	{
		option.Clear();
		for (int i = 0; i < values.Length; i++)
		{
			option.AddItem(values[i], i);
			if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
			{
				option.Select(i);
			}
		}
		option.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(option.GetItemText((int)index));
			}
		};
	}

	private void BindEnumOption<T>(OptionButton option, T value, Action<T> changed) where T : struct, Enum
	{
		option.Clear();
		T[] values = Enum.GetValues<T>();
		for (int i = 0; i < values.Length; i++)
		{
			T val = values[i];
			int id = Convert.ToInt32(val);
			option.AddItem(val.ToString(), id);
			if (EqualityComparer<T>.Default.Equals(val, value))
			{
				option.Select(option.ItemCount - 1);
			}
		}
		option.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				int itemId = option.GetItemId((int)index);
				changed?.Invoke((T)Enum.ToObject(typeof(T), itemId));
			}
		};
	}

	private void BindResourcePicker(XWResourcePicker picker, string baseType, Resource value, Action<Resource> changed)
	{
		picker.Setup(baseType);
		picker.SetEditedResource(value);
		picker.ResourceChanged += delegate(Resource resource)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(resource);
			}
		};
	}

	private static void PopulateVariantList(ItemList list, Godot.Collections.Array array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			list.AddItem("未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {FormatVariant(array[i])}");
		}
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (!GodotObject.IsInstanceValid(list) || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemText(i) == text)
			{
				return;
			}
		}
		list.AddItem(text);
	}

	private static string BuildNameText(TowerDefenseCharacterConfig character)
	{
		return "角色: " + EmptyToPlaceholder(character?.name);
	}

	private static string BuildCategoryText(TowerDefenseCharacterConfig character)
	{
		return "分类: " + GetCharacterCategory(character);
	}

	private static string BuildPacketText(TowerDefenseCharacterConfig character)
	{
		if (character == null)
		{
			return "未配置";
		}
		return $"cost={character.cost}, cooldown={character.packetCooldown:0.###}";
	}

	private static string GetCharacterCategory(TowerDefenseCharacterConfig character)
	{
		if (!(character is TowerDefensePlantConfig))
		{
			if (!(character is TowerDefenseZombieConfig))
			{
				if (!(character is TowerDefenseVaseConfig))
				{
					if (!(character is TowerDefenseMowerConfig))
					{
						if (!(character is TowerDefenseItemConfig))
						{
							if (!(character is TowerDefenseGravestoneConfig))
							{
								if (character is TowerDefenseCraterConfig)
								{
									return "弹坑";
								}
								return "道具";
							}
							return "墓碑";
						}
						return "物品";
					}
					return "小推车";
				}
				return "花瓶";
			}
			return "僵尸";
		}
		return "植物";
	}

	private static Color GetCategoryColor(string category)
	{
		return category switch
		{
			"植物" => new Color(0.24f, 0.55f, 0.24f, 0.95f), 
			"僵尸" => new Color(0.42f, 0.46f, 0.52f, 0.95f), 
			"花瓶" => new Color(0.48f, 0.36f, 0.28f, 0.95f), 
			"小推车" => new Color(0.66f, 0.34f, 0.28f, 0.95f), 
			"墓碑" => new Color(0.32f, 0.32f, 0.36f, 0.95f), 
			"弹坑" => new Color(0.2f, 0.18f, 0.16f, 0.95f), 
			_ => new Color(0.32f, 0.4f, 0.58f, 0.95f), 
		};
	}

	private static void SetFlexibleProperty(Resource resource, string propertyName, Variant value)
	{
		if (GodotObject.IsInstanceValid(resource) && !string.IsNullOrWhiteSpace(propertyName))
		{
			if (HasResourceProperty(resource, propertyName))
			{
				resource.Set(propertyName, value);
			}
			else
			{
				resource.SetMeta(GetModMetaName(propertyName), value);
			}
		}
	}

	private static Variant ReadVariant(Resource resource, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return default(Variant);
		}
		try
		{
			if (HasResourceProperty(resource, propertyName))
			{
				return resource.Get(propertyName);
			}
			string modMetaName = GetModMetaName(propertyName);
			if (resource.HasMeta(modMetaName))
			{
				return resource.GetMeta(modMetaName);
			}
		}
		catch
		{
		}
		return default(Variant);
	}

	private static string ReadString(Resource resource, string propertyName, string fallback = "")
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType == Variant.Type.Nil)
		{
			return fallback;
		}
		string text = variant.AsString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return fallback;
	}

	private static double ReadDouble(Resource resource, string propertyName, double fallback)
	{
		Variant variant = ReadVariant(resource, propertyName);
		Variant.Type variantType = variant.VariantType;
		if ((ulong)(variantType - 2) > 1uL || 1 == 0)
		{
			return fallback;
		}
		return variant.AsDouble();
	}

	private static Resource ReadResource(Resource resource, string propertyName)
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType != Variant.Type.Object)
		{
			return null;
		}
		return variant.AsGodotObject() as Resource;
	}

	private static Godot.Collections.Array ReadArray(Resource resource, string propertyName)
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType != Variant.Type.Array)
		{
			return new Godot.Collections.Array();
		}
		return variant.AsGodotArray();
	}

	private static bool HasResourceProperty(Resource resource, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return false;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (property.ContainsKey("name") && property["name"].AsString() == propertyName)
			{
				return true;
			}
		}
		return false;
	}

	private static string GetModMetaName(string propertyName)
	{
		return "mod_" + propertyName;
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
				goto IL_00bb;
			}
		}
		Variant.Type num = variantType - 21;
		if ((ulong)num > 7uL)
		{
			goto IL_015e;
		}
		switch ((int)num)
		{
		case 0:
		case 1:
			break;
		case 7:
			return $"Array({value.AsGodotArray().Count})";
		case 6:
			return $"Dictionary({value.AsGodotDictionary().Count})";
		case 3:
			return FormatResource(value.AsGodotObject() as Resource);
		default:
			goto IL_015e;
		}
		goto IL_00bb;
		IL_015e:
		return value.ToString();
		IL_00bb:
		return EmptyToPlaceholder(value.AsString());
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未配置";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath.GetFile();
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourceName))
		{
			return resource.ResourceName;
		}
		return resource.GetType().Name;
	}

	private static string FormatPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.GetFile();
		}
		return "未保存";
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
