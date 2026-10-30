using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCardVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum CardCharacterSource
	{
		Mod,
		BuiltIn
	}

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCardVisualEditorLayout.tscn";

	private const string CardInspectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCardInspectorSurface.tscn";

	private const string PacketEventSelectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketEventSelectorWindow.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const string BehaviorIconPath = "res://addons/ModEditor/Icons/GraphEdit.svg";

	private const string BehaviorWarningIconPath = "res://addons/ModEditor/Icons/NodeWarning.svg";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _cardInspectorScene;

	private static PackedScene _packetEventSelectorScene;

	private static PackedScene _visualChoiceCardScene;

	private static Texture2D _behaviorIcon;

	private static Texture2D _behaviorWarningIcon;

	private TowerDefensePacketConfig _editingPacket;

	private XWVisualPropertyBinding _cardPropertyBinding;

	private LineEdit _previewNameLabel;

	private LineEdit _inlineSaveKeyLineEdit;

	private LineEdit _inlineAnimationLineEdit;

	private SpinBox _inlineCostSpinBox;

	private SpinBox _inlineCooldownSpinBox;

	private SpinBox _inlineStartingCooldownSpinBox;

	private Label _previewKeyLabel;

	private Label _previewCostLabel;

	private Label _previewCooldownLabel;

	private Label _previewTypeLabel;

	private Label _previewCharacterLabel;

	private Label _previewAnimationLabel;

	private Label _valueOverrideBadge;

	private Label _battleStatusLabel;

	private Label _cooldownPercentLabel;

	private Label _runtimeClockLabel;

	private HFlowContainer _runtimeStateCards;

	private HSlider _cooldownProgressSlider;

	private SpinBox _previewSunSpinBox;

	private CheckButton _mobilePreviewCheck;

	private Button _cardInteractionButton;

	private Button _pauseRuntimeButton;

	private PanelContainer _cardFrame;

	private Control _cardPacketShowPreviewRoot;

	private TowerDefenseInGamePacketShow _cardPacketShowPreview;

	private string _cardPacketShowSpriteFingerprint = "";

	private string _cardPacketShowAnimationFingerprint = "";

	private string _cardPacketShowSaveKeyFingerprint = "";

	private int _cardPacketShowPreviewBuildCount;

	private int _cardPacketShowSpriteRebuildCount;

	private bool _cardPreviewRefreshCommitted = true;

	private SubViewportContainer _cardCharacterViewportContainer;

	private SubViewport _cardCharacterViewport;

	private TextureRect _cardGameBackground;

	private ColorRect _cardBattleTint;

	private Node2D _cardCharacterPreviewRoot;

	private Control _cardBattleDropSurface;

	private ColorRect _cardDragTargetHighlight;

	private TowerDefenseInGamePacketShow _cardDragPacketGhost;

	private TowerDefenseCharacter _cardCharacterPreview;

	private XWGameplayLogicPreviewSafety _cardCharacterPreviewSafety;

	private Label _cardCharacterMissingLabel;

	private Label _cardImageMissingLabel;

	private Label _eventSummaryLabel;

	private Label _behaviorSummaryLabel;

	private Label _registryBehaviorStatusLabel;

	private ItemList _registryBehaviorList;

	private ItemList _inlineBehaviorList;

	private LineEdit _customBehaviorIdEdit;

	private PopupMenu _registryBehaviorPicker;

	private ItemList _pressedActionsList;

	private ItemList _useSucceededActionsList;

	private ItemList _unlockConditionList;

	private ItemList _armorList;

	private XWResourcePicker _unlockConditionPicker;

	private Button _characterBindingButton;

	private Label _characterSourceLabel;

	private Label _overrideStatus;

	private Button _createOverrideButton;

	private Button _openOverrideButton;

	private Button _clearOverrideButton;

	private XWGameplayResourcePickerWindow _characterResourcePicker;

	private XWPacketEventSelectorWindow _packetEventSelectorWindow;

	private string _packetEventTargetProperty = "pressedActions";

	private readonly List<StringName> _registryBehaviorPickerIds = new List<StringName>();

	private ulong _behaviorRegistryRevision;

	private double _behaviorRegistryPollAccumulator;

	private int _selectedRegistryBehaviorIndex = -1;

	private int _selectedInlineBehaviorIndex = -1;

	private int _selectedEventPressIndex = -1;

	private int _selectedEventPlantIndex = -1;

	private int _selectedUnlockIndex = -1;

	private int _selectedArmorIndex = -1;

	private bool _updatingControls;

	private int _cardRuntimePreviewState;

	private float _cardRuntimeCooldownProgress;

	private double _cardRuntimeCooldownDuration;

	private double _cardRuntimeCooldownRemaining;

	private bool _cardRuntimeCooldownRunning;

	private bool _cardRuntimeHovered;

	private bool _cardRuntimeMobile;

	private double _cardRuntimeReadoutAccumulator;

	private int _cardRuntimeReadoutRefreshCount;

	private TowerDefenseCharacterConfig _cardCharacterPreviewConfig;

	private string _cardCharacterPreviewArmorFingerprint = "";

	private bool _cardCharacterPreviewInitialized;

	private int _cardCharacterPreviewBuildCount;

	private bool _cardDragCandidate;

	private bool _cardDragActive;

	private bool _cardDragTouch;

	private int _cardDragTouchIndex = -1;

	private Vector2 _cardDragStartGlobalPosition;

	private Vector2 _cardDragPointerGlobalPosition;

	private Vector2I _cardDragPreviewCell = new Vector2I(-1, -1);

	private Vector2I _cardPlacedPreviewCell = new Vector2I(-1, -1);

	private bool _cardDragTargetValid;

	private bool _cardHasPlacedPreview;

	private int _cardDragPreviewPlacementCount;

	private int _cardDragRejectedCount;

	private int _cardDragCancelCount;

	private const double CardRuntimeReadoutInterval = 0.1;

	private const float CardDragStartDistance = 10f;

	private const int CardBattleColumns = 9;

	private const int CardBattleRows = 5;

	private static readonly Color CardDragValidColor = new Color(0.32f, 1f, 0.38f, 0.34f);

	private static readonly Color CardDragInvalidColor = new Color(1f, 0.24f, 0.18f, 0.38f);

	private const string BuiltInCharacterRootPath = "Asset/Anime/Character";

	public int CardRuntimeReadoutRefreshCount => _cardRuntimeReadoutRefreshCount;

	public int CardCharacterPreviewBuildCount => _cardCharacterPreviewBuildCount;

	public int CardPacketShowPreviewBuildCount => _cardPacketShowPreviewBuildCount;

	public int CardPacketShowSpriteRebuildCount => _cardPacketShowSpriteRebuildCount;

	public bool IsCardDragActive => _cardDragActive;

	public bool IsCardDragTargetValid
	{
		get
		{
			if (_cardDragActive)
			{
				return _cardDragTargetValid;
			}
			return false;
		}
	}

	public bool HasCardDragGhost
	{
		get
		{
			if (_cardDragActive && GodotObject.IsInstanceValid(_cardDragPacketGhost))
			{
				return _cardDragPacketGhost.Visible;
			}
			return false;
		}
	}

	public Vector2I CardDragPreviewCell => _cardDragPreviewCell;

	public Vector2I CardPlacedPreviewCell => _cardPlacedPreviewCell;

	public int CardDragPreviewPlacementCount => _cardDragPreviewPlacementCount;

	public int CardDragRejectedCount => _cardDragRejectedCount;

	public int CardDragCancelCount => _cardDragCancelCount;

	public Color CardDragTargetColor
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_cardDragTargetHighlight))
			{
				return Colors.Transparent;
			}
			return _cardDragTargetHighlight.Color;
		}
	}

	public int RuntimeStateVisualCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_runtimeStateCards))
			{
				return 0;
			}
			return _runtimeStateCards.GetChildCount();
		}
	}

	public bool IsCardPreviewRendering
	{
		get
		{
			if (GodotObject.IsInstanceValid(_cardCharacterViewport))
			{
				return _cardCharacterViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Disabled;
			}
			return false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		EnableVisibilityGatedProcessing();
		RequestVisibilityGatedProcessing(requested: false);
		SetProcessInput(IsVisibleInTree());
	}

	public override void _ExitTree()
	{
		CancelCardDragSession(countCancellation: false, "编辑器已关闭");
		SetProcessInput(enable: false);
		DisposeCardDragGhost();
		StopCardRuntimeClock();
		DisposeCardCharacterPreview();
		_cardCharacterPreviewBuildCount = 0;
		_cardPropertyBinding?.Dispose();
		_cardPropertyBinding = null;
		_registryBehaviorPickerIds.Clear();
		_behaviorRegistryRevision = 0uL;
		_behaviorRegistryPollAccumulator = 0.0;
		_cardPacketShowPreview = null;
		_cardPacketShowSpriteFingerprint = "";
		_cardPacketShowAnimationFingerprint = "";
		_cardPacketShowSaveKeyFingerprint = "";
		_cardPacketShowPreviewBuildCount = 0;
		_cardPacketShowSpriteRebuildCount = 0;
		_cardPreviewRefreshCommitted = true;
		_editingPacket = null;
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		if (!visible)
		{
			CancelCardDragSession(_cardDragCandidate || _cardDragActive, "已切换页面，拖拽预览已取消");
		}
		SetProcessInput(visible);
		if (GodotObject.IsInstanceValid(_cardCharacterViewport))
		{
			_cardCharacterViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(visible ? 4 : 0);
		}
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		CancelCardDragSession(countCancellation: false, "");
		DisposeCardDragGhost();
		StopCardRuntimeClock();
		DisposeCardCharacterPreview();
		_cardPropertyBinding?.Dispose();
		_cardPropertyBinding = null;
		_registryBehaviorPickerIds.Clear();
		_behaviorRegistryRevision = 0uL;
		_behaviorRegistryPollAccumulator = 0.0;
		_cardPacketShowPreview = null;
		_cardPacketShowSpriteFingerprint = "";
		_cardPacketShowAnimationFingerprint = "";
		_cardPacketShowSaveKeyFingerprint = "";
		_cardPacketShowPreviewBuildCount = 0;
		_cardPacketShowSpriteRebuildCount = 0;
		_cardPreviewRefreshCommitted = true;
		_editingPacket = null;
		if (base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_editingPacket = towerDefensePacketConfig;
			_cardPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCardPropertyEdited);
			RenderCardPreview(towerDefensePacketConfig);
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource?.GetType() != typeof(TowerDefensePacketConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void RenderCardPreview(TowerDefensePacketConfig packet)
	{
		if (base.CanvasGrid == null)
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_editorLayoutScene == null)
		{
			_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCardVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			BindCardPreviewSurface(vBoxContainer, packet);
			if (_cardInspectorScene == null)
			{
				_cardInspectorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCardInspectorSurface.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer2 = _cardInspectorScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer2))
			{
				vBoxContainer.GetNode<VBoxContainer>("%InspectorHost").AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
				BindCardInspectorSurface(vBoxContainer2, packet);
			}
			BindCardBehaviorSurface(vBoxContainer, packet);
			BindCardEventSurface(vBoxContainer, packet);
			AddSummaryRows(packet);
		}
	}

	private void BindCardPreviewSurface(VBoxContainer root, TowerDefensePacketConfig packet)
	{
		_cardFrame = root.GetNode<PanelContainer>("%CardFrame");
		_previewCostLabel = root.GetNode<Label>("%CostLabel");
		_previewNameLabel = root.GetNode<LineEdit>("%CardNameLabel");
		_cardPropertyBinding.BindText(_previewNameLabel, packet, "name", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		_inlineSaveKeyLineEdit = root.GetNode<LineEdit>("%InlineSaveKeyLineEdit");
		_cardPropertyBinding.BindText(_inlineSaveKeyLineEdit, packet, "saveKey", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		_inlineCostSpinBox = root.GetNode<SpinBox>("%InlineCostSpinBox");
		_cardPropertyBinding.BindNumber(_inlineCostSpinBox, packet, "overrideCost", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		_inlineCooldownSpinBox = root.GetNode<SpinBox>("%InlineCooldownSpinBox");
		_cardPropertyBinding.BindNumber(_inlineCooldownSpinBox, packet, "overridePacketCooldown", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		_inlineStartingCooldownSpinBox = root.GetNode<SpinBox>("%InlineStartingCooldownSpinBox");
		_cardPropertyBinding.BindNumber(_inlineStartingCooldownSpinBox, packet, "overrideStartingCooldown", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		_inlineAnimationLineEdit = root.GetNode<LineEdit>("%InlineAnimationLineEdit");
		_cardPropertyBinding.BindText(_inlineAnimationLineEdit, packet, "packetAnimeClip", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
		root.GetNode<Button>("%InlineCharacterBindingButton").Pressed += ShowCharacterSelectorWindow;
		_cardPacketShowPreviewRoot = root.GetNode<Control>("%PacketShowRoot");
		_cardPacketShowPreviewRoot.Resized += PositionPacketShowPreview;
		_cardInteractionButton = root.GetNode<Button>("%PacketInteractionButton");
		_cardInteractionButton.MouseEntered += OnCardRuntimeMouseEntered;
		_cardInteractionButton.MouseExited += OnCardRuntimeMouseExited;
		_cardInteractionButton.Pressed += ToggleCardRuntimeSelection;
		_cardInteractionButton.GuiInput += OnCardInteractionGuiInput;
		_cardCharacterViewportContainer = root.GetNode<SubViewportContainer>("%CharacterViewportContainer");
		_cardCharacterViewport = root.GetNode<SubViewport>("%CharacterViewport");
		_cardCharacterViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(IsVisibleInTree() ? 4 : 0);
		_cardGameBackground = root.GetNode<TextureRect>("%GameBackground");
		_cardBattleTint = root.GetNode<ColorRect>("%BattleTint");
		_cardCharacterPreviewRoot = root.GetNode<Node2D>("%CharacterPreviewRoot");
		_cardBattleDropSurface = root.GetNode<Control>("%CardBattleDropSurface");
		_cardBattleDropSurface.Draw += DrawCardBattleDropSurface;
		_cardBattleDropSurface.Resized += OnCardBattleDropSurfaceResized;
		_cardDragTargetHighlight = root.GetNode<ColorRect>("%CardDragTargetHighlight");
		_cardCharacterViewportContainer.Resized += UpdateCardBattleViewportLayout;
		_cardCharacterMissingLabel = root.GetNode<Label>("%CharacterMissingLabel");
		root.GetNode<Button>("%PreviewPlantButton").Pressed += PreviewCardPlantAndCooldown;
		root.GetNode<Button>("%ResetRuntimeButton").Pressed += ResetCardRuntimePreview;
		_cardImageMissingLabel = root.GetNode<Label>("%ImageMissingLabel");
		_battleStatusLabel = root.GetNode<Label>("%BattleStatusLabel");
		_cooldownPercentLabel = root.GetNode<Label>("%CooldownPercentLabel");
		_runtimeClockLabel = root.GetNode<Label>("%RuntimeClockLabel");
		_runtimeStateCards = root.GetNode<HFlowContainer>("%RuntimeStateCards");
		BuildRuntimeStateCards();
		_cooldownProgressSlider = root.GetNode<HSlider>("%CooldownProgressSlider");
		_cooldownProgressSlider.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				_cardRuntimeCooldownProgress = (float)value;
				EnsureCardRuntimeCooldownDuration();
				_cardRuntimeCooldownRemaining = _cardRuntimeCooldownDuration * (double)_cardRuntimeCooldownProgress;
				if (_cardRuntimeCooldownProgress > 0.001f)
				{
					_cardRuntimePreviewState = 2;
				}
				else if (_cardRuntimePreviewState == 2)
				{
					StopCardRuntimeClock();
					_cardRuntimePreviewState = ResolveCardAvailabilityState();
				}
				ApplyCardRuntimePreviewState();
			}
		};
		_previewSunSpinBox = root.GetNode<SpinBox>("%PreviewSunSpinBox");
		_previewSunSpinBox.ValueChanged += delegate
		{
			if (!_updatingControls)
			{
				if (_cardRuntimePreviewState != 2 && _cardRuntimePreviewState != 4)
				{
					_cardRuntimePreviewState = ResolveCardAvailabilityState();
				}
				ApplyCardRuntimePreviewState();
			}
		};
		_mobilePreviewCheck = root.GetNode<CheckButton>("%MobilePreviewCheck");
		_mobilePreviewCheck.Toggled += delegate(bool enabled)
		{
			if (!_updatingControls)
			{
				_cardRuntimeMobile = enabled;
				RefreshPacketShowPreview(_editingPacket);
			}
		};
		_pauseRuntimeButton = root.GetNode<Button>("%PauseRuntimeButton");
		_pauseRuntimeButton.Pressed += ToggleCardRuntimeClock;
		_previewKeyLabel = root.GetNode<Label>("%KeyLabel");
		_previewTypeLabel = root.GetNode<Label>("%TypeLabel");
		_previewCooldownLabel = root.GetNode<Label>("%CooldownLabel");
		_previewCharacterLabel = root.GetNode<Label>("%CharacterLabel");
		_previewAnimationLabel = root.GetNode<Label>("%AnimationLabel");
		_valueOverrideBadge = root.GetNode<Label>("%ValueOverrideBadge");
		InitializeCardRuntimePreview();
		CallDeferred("UpdateCardBattleViewportLayout");
	}

	private void InitializeCardRuntimePreview()
	{
		ResetCardDragPreviewState();
		_cardRuntimePreviewState = 0;
		_cardRuntimeCooldownProgress = 0f;
		_cardRuntimeCooldownDuration = 0.0;
		_cardRuntimeCooldownRemaining = 0.0;
		_cardRuntimeCooldownRunning = false;
		_cardRuntimeHovered = false;
		_cardRuntimeReadoutAccumulator = 0.0;
		_cardRuntimeReadoutRefreshCount = 0;
		RequestVisibilityGatedProcessing(requested: false);
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_previewSunSpinBox))
			{
				_previewSunSpinBox.Value = Math.Max(500L, GetEffectiveCost(_editingPacket));
			}
		}
		finally
		{
			_updatingControls = updatingControls;
		}
		UpdateCardPreview();
		ApplyCardRuntimePreviewState();
	}

	private void BindCardInspectorSurface(VBoxContainer root, TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(root) || packet == null)
		{
			return;
		}
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		try
		{
			_cardPropertyBinding.BindText(root.GetNode<LineEdit>("%ResourceNameEdit"), packet, "resource_name", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%LocalToSceneCheck"), packet, "resource_local_to_scene", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindText(root.GetNode<LineEdit>("%SaveKeyLineEdit"), packet, "saveKey", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindText(root.GetNode<LineEdit>("%NameLineEdit"), packet, "name", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			BuildPacketTypeCards(root.GetNode<HFlowContainer>("%TypeCards"), packet.type);
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%PacketFlipCheck"), packet, "packetFlip", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_characterBindingButton = root.GetNode<Button>("%CharacterBindingWindowButton");
			_characterBindingButton.Pressed += ShowCharacterSelectorWindow;
			root.GetNode<Button>("%ClearCharacterBindingButton").Pressed += ClearCharacterBinding;
			_characterSourceLabel = root.GetNode<Label>("%CharacterSourceLabel");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%CostSpinBox"), packet, "overrideCost", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%CooldownSpinBox"), packet, "overridePacketCooldown", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%StartingCooldownSpinBox"), packet, "overrideStartingCooldown", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%CanChangeCostCheck"), packet, "canChangeCost", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			BuildSpawnMethodCards(root.GetNode<HFlowContainer>("%SpawnMethodCards"), packet);
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%OverrideHypnosesCheck"), packet, "overrideHypnoses", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%OverrideCostRiseSpinBox"), packet, "overrideCostRise", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%OverrideWeightSpinBox"), packet, "overrideWeight", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindNumber(root.GetNode<SpinBox>("%OverrideWavePointCostSpinBox"), packet, "overrideWavePointCost", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%PlantUseCellCheck"), packet, "plantUseCell", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%IzmPlantAllCellCheck"), packet, "izmPlantAllCell", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%IzmPlantLeftCheck"), packet, "izmPlantLeft", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%DisableWhenSunNegativeCheck"), packet, "disableWhenSunNegative", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindToggle(root.GetNode<CheckButton>("%CanPlaceOnZombieCheck"), packet, "canPlaceOnZombie", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_overrideStatus = root.GetNode<Label>("%OverrideStatus");
			_createOverrideButton = root.GetNode<Button>("%CreateOverrideButton");
			_openOverrideButton = root.GetNode<Button>("%OpenOverrideButton");
			_clearOverrideButton = root.GetNode<Button>("%ClearOverrideButton");
			_createOverrideButton.Pressed += CreatePacketOverride;
			_openOverrideButton.Pressed += OpenPacketOverride;
			_clearOverrideButton.Pressed += ClearPacketOverride;
			UpdatePacketOverrideEntry();
			_cardPropertyBinding.BindText(root.GetNode<LineEdit>("%PacketAnimeClipLineEdit"), packet, "packetAnimeClip", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			BindCardVector2Field(root.GetNode<SpinBox>("%PacketAnimeOffsetX"), root.GetNode<SpinBox>("%PacketAnimeOffsetY"), packet, "packetAnimeOffset");
			BindCardVector2Field(root.GetNode<SpinBox>("%PacketAnimeScaleX"), root.GetNode<SpinBox>("%PacketAnimeScaleY"), packet, "packetAnimeScale");
			BindCardVector2Field(root.GetNode<SpinBox>("%HandbookPacketAnimeOffsetX"), root.GetNode<SpinBox>("%HandbookPacketAnimeOffsetY"), packet, "handbookPacketAnimeOffset");
			_cardPropertyBinding.BindText(root.GetNode<TextEdit>("%DescribeTextEdit"), packet, "describe", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindText(root.GetNode<TextEdit>("%PlantFoodTextEdit"), packet, "plantfood", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindText(root.GetNode<TextEdit>("%HandbookDescribeTextEdit"), packet, "handbookDescribe", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			_cardPropertyBinding.BindText(root.GetNode<TextEdit>("%HandbookStoryTextEdit"), packet, "handbookStory", UpdateCardPreview, this, "RefreshCardEditorFromHistory");
			BindUnlockAndArmorEditors(root, packet);
			RefreshCharacterBindingSelector(packet.characterConfig);
		}
		finally
		{
			_updatingControls = updatingControls;
		}
	}

	private void BindUnlockAndArmorEditors(VBoxContainer root, TowerDefensePacketConfig packet)
	{
		HBoxContainer node = root.GetNode<HBoxContainer>("%UnlockPickerHost");
		_unlockConditionPicker = XWResourcePicker.Create();
		_unlockConditionPicker.Name = "UnlockConditionPicker";
		_unlockConditionPicker.Setup("UnlockConditionBaseConfig");
		_unlockConditionPicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		node.AddChild(_unlockConditionPicker, forceReadableName: false, InternalMode.Disabled);
		root.GetNode<Button>("%AddUnlockButton").Pressed += AddUnlockCondition;
		root.GetNode<Button>("%ReplaceUnlockButton").Pressed += ReplaceSelectedUnlockCondition;
		root.GetNode<Button>("%RemoveUnlockButton").Pressed += RemoveSelectedUnlockCondition;
		root.GetNode<Button>("%OpenUnlockButton").Pressed += delegate
		{
			EditSelectedUnlockCondition(GetSelectedUnlockIndex());
		};
		root.GetNode<Button>("%MoveUnlockUpButton").Pressed += delegate
		{
			MoveSelectedUnlockCondition(-1);
		};
		root.GetNode<Button>("%MoveUnlockDownButton").Pressed += delegate
		{
			MoveSelectedUnlockCondition(1);
		};
		_armorList = root.GetNode<ItemList>("%ArmorList");
		LineEdit armorValueEdit = root.GetNode<LineEdit>("%ArmorValueEdit");
		_armorList.ItemSelected += delegate(long index)
		{
			_selectedArmorIndex = (int)index;
			if (_editingPacket?.initArmor != null && _selectedArmorIndex >= 0 && _selectedArmorIndex < _editingPacket.initArmor.Count)
			{
				armorValueEdit.Text = _editingPacket.initArmor[_selectedArmorIndex];
			}
		};
		root.GetNode<Button>("%AddArmorButton").Pressed += delegate
		{
			AddArmor(armorValueEdit.Text);
		};
		root.GetNode<Button>("%ReplaceArmorButton").Pressed += delegate
		{
			ReplaceSelectedArmor(armorValueEdit.Text);
		};
		root.GetNode<Button>("%RemoveArmorButton").Pressed += RemoveSelectedArmor;
		root.GetNode<Button>("%MoveArmorUpButton").Pressed += delegate
		{
			MoveSelectedArmor(-1);
		};
		root.GetNode<Button>("%MoveArmorDownButton").Pressed += delegate
		{
			MoveSelectedArmor(1);
		};
		armorValueEdit.TextSubmitted += delegate
		{
			AddArmor(armorValueEdit.Text);
		};
		RefreshArmorList(packet);
	}

	private int GetSelectedUnlockIndex()
	{
		if (GodotObject.IsInstanceValid(_unlockConditionList))
		{
			int[] selectedItems = _unlockConditionList.GetSelectedItems();
			if (selectedItems.Length != 0)
			{
				return selectedItems[0];
			}
		}
		return _selectedUnlockIndex;
	}

	private void AddUnlockCondition()
	{
		if (base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig && _unlockConditionPicker?.EditedResource is UnlockConditionBaseConfig unlockConditionBaseConfig && GodotObject.IsInstanceValid(unlockConditionBaseConfig))
		{
			Array<UnlockConditionBaseConfig> array = ((towerDefensePacketConfig.unlockCheckList == null) ? new Array<UnlockConditionBaseConfig>() : new Array<UnlockConditionBaseConfig>(towerDefensePacketConfig.unlockCheckList));
			array.Add(unlockConditionBaseConfig);
			_selectedUnlockIndex = array.Count - 1;
			ReplaceCardUnlockConditions(array, "添加卡牌解锁条件");
		}
	}

	private void ReplaceSelectedUnlockCondition()
	{
		if (base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig && _unlockConditionPicker?.EditedResource is UnlockConditionBaseConfig unlockConditionBaseConfig && GodotObject.IsInstanceValid(unlockConditionBaseConfig) && towerDefensePacketConfig.unlockCheckList != null)
		{
			int selectedUnlockIndex = GetSelectedUnlockIndex();
			if (selectedUnlockIndex >= 0 && selectedUnlockIndex < towerDefensePacketConfig.unlockCheckList.Count)
			{
				Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(towerDefensePacketConfig.unlockCheckList);
				array[selectedUnlockIndex] = unlockConditionBaseConfig;
				_selectedUnlockIndex = selectedUnlockIndex;
				ReplaceCardUnlockConditions(array, "替换卡牌解锁条件");
			}
		}
	}

	private void RemoveSelectedUnlockCondition()
	{
		if (_editingPacket?.unlockCheckList != null)
		{
			int selectedUnlockIndex = GetSelectedUnlockIndex();
			if (selectedUnlockIndex >= 0 && selectedUnlockIndex < _editingPacket.unlockCheckList.Count)
			{
				Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(_editingPacket.unlockCheckList);
				array.RemoveAt(selectedUnlockIndex);
				_selectedUnlockIndex = ((array.Count == 0) ? (-1) : Math.Min(selectedUnlockIndex, array.Count - 1));
				ReplaceCardUnlockConditions(array, "删除卡牌解锁条件");
			}
		}
	}

	private void MoveSelectedUnlockCondition(int direction)
	{
		if (_editingPacket?.unlockCheckList != null)
		{
			int selectedUnlockIndex = GetSelectedUnlockIndex();
			int num = selectedUnlockIndex + Math.Sign(direction);
			if (selectedUnlockIndex >= 0 && selectedUnlockIndex < _editingPacket.unlockCheckList.Count && num >= 0 && num < _editingPacket.unlockCheckList.Count)
			{
				Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(_editingPacket.unlockCheckList);
				UnlockConditionBaseConfig value = array[selectedUnlockIndex];
				array[selectedUnlockIndex] = array[num];
				array[num] = value;
				_selectedUnlockIndex = num;
				ReplaceCardUnlockConditions(array, (direction < 0) ? "上移卡牌解锁条件" : "下移卡牌解锁条件");
			}
		}
	}

	private void ReplaceCardUnlockConditions(Array<UnlockConditionBaseConfig> next, string actionName)
	{
		if (_cardPropertyBinding != null && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPropertyBinding.SetValue(towerDefensePacketConfig, "unlockCheckList", next, actionName, this, "RefreshCardEditorFromHistory");
			towerDefensePacketConfig.NotifyPropertyListChanged();
			RefreshCardLists(towerDefensePacketConfig);
		}
	}

	private void AddArmor(string armor)
	{
		if (!string.IsNullOrWhiteSpace(armor) && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			Array<string> array = ((towerDefensePacketConfig.initArmor == null) ? new Array<string>() : new Array<string>(towerDefensePacketConfig.initArmor));
			array.Add(armor.Trim());
			_selectedArmorIndex = array.Count - 1;
			ReplaceCardArmor(array, "添加初始护甲");
		}
	}

	private void ReplaceSelectedArmor(string armor)
	{
		if (!string.IsNullOrWhiteSpace(armor) && _editingPacket?.initArmor != null && _selectedArmorIndex >= 0 && _selectedArmorIndex < _editingPacket.initArmor.Count)
		{
			Array<string> array = new Array<string>(_editingPacket.initArmor);
			array[_selectedArmorIndex] = armor.Trim();
			ReplaceCardArmor(array, "替换初始护甲");
		}
	}

	private void RemoveSelectedArmor()
	{
		if (_editingPacket?.initArmor != null && _selectedArmorIndex >= 0 && _selectedArmorIndex < _editingPacket.initArmor.Count)
		{
			Array<string> array = new Array<string>(_editingPacket.initArmor);
			array.RemoveAt(_selectedArmorIndex);
			_selectedArmorIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedArmorIndex, array.Count - 1));
			ReplaceCardArmor(array, "删除初始护甲");
		}
	}

	private void MoveSelectedArmor(int direction)
	{
		if (_editingPacket?.initArmor != null && _selectedArmorIndex >= 0 && _selectedArmorIndex < _editingPacket.initArmor.Count)
		{
			int num = _selectedArmorIndex + Math.Sign(direction);
			if (num >= 0 && num < _editingPacket.initArmor.Count)
			{
				Array<string> array = new Array<string>(_editingPacket.initArmor);
				string value = array[_selectedArmorIndex];
				array[_selectedArmorIndex] = array[num];
				array[num] = value;
				_selectedArmorIndex = num;
				ReplaceCardArmor(array, (direction < 0) ? "上移初始护甲" : "下移初始护甲");
			}
		}
	}

	private void ReplaceCardArmor(Array<string> next, string actionName)
	{
		if (_cardPropertyBinding != null && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPropertyBinding.SetValue(towerDefensePacketConfig, "initArmor", next, actionName, this, "RefreshCardEditorFromHistory");
			towerDefensePacketConfig.NotifyPropertyListChanged();
			RefreshArmorList(towerDefensePacketConfig);
			UpdateCardPreview();
		}
	}

	private void RefreshArmorList(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(_armorList))
		{
			return;
		}
		_armorList.Clear();
		if (packet?.initArmor != null)
		{
			for (int i = 0; i < packet.initArmor.Count; i++)
			{
				_armorList.AddItem($"{i + 1}. {packet.initArmor[i]}");
			}
			if (_selectedArmorIndex >= 0 && _selectedArmorIndex < _armorList.ItemCount)
			{
				_armorList.Select(_selectedArmorIndex);
			}
		}
	}

	private void BindCardBehaviorSurface(VBoxContainer root, TowerDefensePacketConfig packet)
	{
		_behaviorSummaryLabel = root.GetNode<Label>("%BehaviorSummaryLabel");
		_registryBehaviorStatusLabel = root.GetNode<Label>("%RegistryBehaviorStatusLabel");
		_registryBehaviorList = root.GetNode<ItemList>("%RegistryBehaviorList");
		_inlineBehaviorList = root.GetNode<ItemList>("%InlineBehaviorList");
		_customBehaviorIdEdit = root.GetNode<LineEdit>("%CustomBehaviorIdEdit");
		_registryBehaviorPicker = root.GetNode<PopupMenu>("%RegistryBehaviorPicker");
		root.GetNode<Button>("%PickRegistryBehaviorButton").Pressed += ShowRegistryBehaviorPicker;
		root.GetNode<Button>("%AddCustomBehaviorIdButton").Pressed += AddCustomBehaviorId;
		root.GetNode<Button>("%OpenRegistryBehaviorButton").Pressed += OpenSelectedRegistryBehavior;
		root.GetNode<Button>("%RemoveRegistryBehaviorButton").Pressed += RemoveSelectedRegistryBehavior;
		root.GetNode<Button>("%MoveRegistryBehaviorUpButton").Pressed += delegate
		{
			MoveSelectedRegistryBehavior(-1);
		};
		root.GetNode<Button>("%MoveRegistryBehaviorDownButton").Pressed += delegate
		{
			MoveSelectedRegistryBehavior(1);
		};
		root.GetNode<Button>("%RefreshRegistryBehaviorButton").Pressed += RefreshBehaviorRegistryFromUser;
		root.GetNode<Button>("%AddInlineBehaviorButton").Pressed += ShowInlineBehaviorSelector;
		root.GetNode<Button>("%OpenInlineBehaviorButton").Pressed += OpenSelectedInlineBehavior;
		root.GetNode<Button>("%RemoveInlineBehaviorButton").Pressed += RemoveSelectedInlineBehavior;
		root.GetNode<Button>("%MoveInlineBehaviorUpButton").Pressed += delegate
		{
			MoveSelectedInlineBehavior(-1);
		};
		root.GetNode<Button>("%MoveInlineBehaviorDownButton").Pressed += delegate
		{
			MoveSelectedInlineBehavior(1);
		};
		_registryBehaviorPicker.IndexPressed += AddRegistryBehaviorFromPicker;
		_customBehaviorIdEdit.TextSubmitted += delegate
		{
			AddCustomBehaviorId();
		};
		_registryBehaviorList.ItemSelected += delegate(long index)
		{
			_selectedRegistryBehaviorIndex = (int)index;
		};
		_registryBehaviorList.ItemActivated += delegate(long index)
		{
			_selectedRegistryBehaviorIndex = (int)index;
			OpenSelectedRegistryBehavior();
		};
		_inlineBehaviorList.ItemSelected += delegate(long index)
		{
			_selectedInlineBehaviorIndex = (int)index;
		};
		_inlineBehaviorList.ItemActivated += delegate(long index)
		{
			_selectedInlineBehaviorIndex = (int)index;
			OpenSelectedInlineBehavior();
		};
		_behaviorRegistryRevision = TowerDefenseBehaviorRegistry.Revision;
		RefreshCardBehaviorLists(packet);
	}

	private void ShowRegistryBehaviorPicker()
	{
		if (GodotObject.IsInstanceValid(_registryBehaviorPicker))
		{
			BuildRegistryBehaviorPicker();
			_registryBehaviorPicker.Position = DisplayServer.MouseGetPosition();
			_registryBehaviorPicker.Popup();
		}
	}

	private void BuildRegistryBehaviorPicker()
	{
		_registryBehaviorPicker.Clear();
		_registryBehaviorPickerIds.Clear();
		Array<StringName> behaviorNames = TowerDefenseBehaviorRegistry.GetBehaviorNames();
		List<StringName> list = new List<StringName>();
		if (behaviorNames != null)
		{
			foreach (StringName item in behaviorNames)
			{
				list.Add(item);
			}
		}
		list.Sort((StringName left, StringName right) => string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase));
		int num = 0;
		foreach (StringName item2 in list)
		{
			Resource behavior = TowerDefenseBehaviorRegistry.GetBehavior(item2);
			bool flag = behavior is CardBehaviorDefinition;
			string value = behavior?.GetType().Name ?? "资源缺失";
			int itemCount = _registryBehaviorPicker.ItemCount;
			_registryBehaviorPicker.AddItem(flag ? $"◆ {item2}  ·  {value}" : $"⚠ {item2}  ·  非卡牌行为 {value}", -1, Key.None);
			_registryBehaviorPickerIds.Add(item2);
			_registryBehaviorPicker.SetItemDisabled(itemCount, !flag);
			if (flag)
			{
				num++;
			}
		}
		if (_registryBehaviorPicker.ItemCount == 0)
		{
			_registryBehaviorPicker.AddItem("当前行为库为空，可在下方直接输入 Mod 行为 ID", -1, Key.None);
			_registryBehaviorPicker.SetItemDisabled(0, disabled: true);
			_registryBehaviorPickerIds.Add(null);
		}
		if (GodotObject.IsInstanceValid(_registryBehaviorStatusLabel))
		{
			_registryBehaviorStatusLabel.Text = $"行为库版本 {_behaviorRegistryRevision} · 可用卡牌拼图 {num} · 其他类型 {list.Count - num}";
		}
	}

	private void AddRegistryBehaviorFromPicker(long index)
	{
		int num = (int)index;
		if (num >= 0 && num < _registryBehaviorPickerIds.Count)
		{
			StringName stringName = _registryBehaviorPickerIds[num];
			if (!(stringName == null) && !stringName.IsEmpty)
			{
				AddRegistryBehaviorId(stringName, "添加注册行为拼图");
			}
		}
	}

	private void AddCustomBehaviorId()
	{
		if (GodotObject.IsInstanceValid(_customBehaviorIdEdit))
		{
			string text = _customBehaviorIdEdit.Text.Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				AddRegistryBehaviorId(new StringName(text), "添加自定义行为 ID");
				_customBehaviorIdEdit.Text = "";
			}
		}
	}

	private void AddRegistryBehaviorId(StringName id, string actionName)
	{
		if (_editingPacket != null && !(id == null) && !id.IsEmpty)
		{
			Array<StringName> array = ((_editingPacket.behaviorIds == null) ? new Array<StringName>() : new Array<StringName>(_editingPacket.behaviorIds));
			array.Add(id);
			_selectedRegistryBehaviorIndex = array.Count - 1;
			ReplaceRegistryBehaviorIds(array, actionName);
		}
	}

	private void RemoveSelectedRegistryBehavior()
	{
		if (_editingPacket?.behaviorIds != null)
		{
			int selectedRegistryBehaviorIndex = GetSelectedRegistryBehaviorIndex();
			if (selectedRegistryBehaviorIndex >= 0 && selectedRegistryBehaviorIndex < _editingPacket.behaviorIds.Count)
			{
				Array<StringName> array = new Array<StringName>(_editingPacket.behaviorIds);
				array.RemoveAt(selectedRegistryBehaviorIndex);
				_selectedRegistryBehaviorIndex = Math.Min(selectedRegistryBehaviorIndex, array.Count - 1);
				ReplaceRegistryBehaviorIds(array, "移除注册行为拼图");
			}
		}
	}

	private void MoveSelectedRegistryBehavior(int direction)
	{
		if (_editingPacket?.behaviorIds != null)
		{
			int selectedRegistryBehaviorIndex = GetSelectedRegistryBehaviorIndex();
			int num = selectedRegistryBehaviorIndex + Math.Sign(direction);
			if (selectedRegistryBehaviorIndex >= 0 && selectedRegistryBehaviorIndex < _editingPacket.behaviorIds.Count && num >= 0 && num < _editingPacket.behaviorIds.Count)
			{
				Array<StringName> array = new Array<StringName>(_editingPacket.behaviorIds);
				StringName value = array[selectedRegistryBehaviorIndex];
				array[selectedRegistryBehaviorIndex] = array[num];
				array[num] = value;
				_selectedRegistryBehaviorIndex = num;
				ReplaceRegistryBehaviorIds(array, (direction < 0) ? "提前注册行为拼图" : "延后注册行为拼图");
			}
		}
	}

	private void ReplaceRegistryBehaviorIds(Array<StringName> next, string actionName)
	{
		if (_cardPropertyBinding != null && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPropertyBinding.SetValue(towerDefensePacketConfig, "behaviorIds", next, actionName, this, "RefreshCardEditorFromHistory");
			towerDefensePacketConfig.NotifyPropertyListChanged();
			RefreshCardBehaviorLists(towerDefensePacketConfig);
			UpdateCardPreview();
		}
	}

	private int GetSelectedRegistryBehaviorIndex()
	{
		if (GodotObject.IsInstanceValid(_registryBehaviorList))
		{
			int[] selectedItems = _registryBehaviorList.GetSelectedItems();
			if (selectedItems.Length != 0)
			{
				return selectedItems[0];
			}
		}
		return _selectedRegistryBehaviorIndex;
	}

	private void OpenSelectedRegistryBehavior()
	{
		if (_editingPacket?.behaviorIds == null)
		{
			return;
		}
		int selectedRegistryBehaviorIndex = GetSelectedRegistryBehaviorIndex();
		if (selectedRegistryBehaviorIndex >= 0 && selectedRegistryBehaviorIndex < _editingPacket.behaviorIds.Count)
		{
			Resource behavior = TowerDefenseBehaviorRegistry.GetBehavior(_editingPacket.behaviorIds[selectedRegistryBehaviorIndex]);
			if (GodotObject.IsInstanceValid(behavior))
			{
				XWEditorInterface.Instance?.EditResource(behavior);
			}
			else
			{
				XWEditorInterface.Instance?.ShowToast("该行为 ID 尚未由游戏或 Mod 注册。", 2);
			}
		}
	}

	private void ShowInlineBehaviorSelector()
	{
		EnsurePacketEventSelectorWindow();
		if (GodotObject.IsInstanceValid(_packetEventSelectorWindow))
		{
			_packetEventSelectorWindow.ShowFor("新建卡牌行为拼图", "只显示 CardBehaviorDefinition；创建后可在主面板直接编辑全部属性", typeof(CardBehaviorDefinition), OnInlineBehaviorTypeSelected);
		}
	}

	private void OnInlineBehaviorTypeSelected(Type runtimeType)
	{
		CardBehaviorDefinition cardBehaviorDefinition = CreateInlineBehaviorInstance(runtimeType);
		if (GodotObject.IsInstanceValid(cardBehaviorDefinition) && _editingPacket != null)
		{
			Array<CardBehaviorDefinition> array = ((_editingPacket.behaviors == null) ? new Array<CardBehaviorDefinition>() : new Array<CardBehaviorDefinition>(_editingPacket.behaviors));
			array.Add(cardBehaviorDefinition);
			_selectedInlineBehaviorIndex = array.Count - 1;
			ReplaceInlineBehaviors(array, "添加内联行为拼图");
			OpenInlineBehavior(array.Count - 1, cardBehaviorDefinition);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The Mod editor creates an authored card behavior selected from loaded game and Mod assemblies.")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The selector already restricts choices to public parameterless authored behavior types.")]
	private static CardBehaviorDefinition CreateInlineBehaviorInstance(Type type)
	{
		if (type == null || type.IsAbstract || type.ContainsGenericParameters || !typeof(CardBehaviorDefinition).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
		{
			return null;
		}
		try
		{
			return Activator.CreateInstance(type) as CardBehaviorDefinition;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Card behavior authoring create failed: " + type.FullName + " " + ex.Message);
			return null;
		}
	}

	private void RemoveSelectedInlineBehavior()
	{
		if (_editingPacket?.behaviors != null)
		{
			int selectedInlineBehaviorIndex = GetSelectedInlineBehaviorIndex();
			if (selectedInlineBehaviorIndex >= 0 && selectedInlineBehaviorIndex < _editingPacket.behaviors.Count)
			{
				Array<CardBehaviorDefinition> array = new Array<CardBehaviorDefinition>(_editingPacket.behaviors);
				array.RemoveAt(selectedInlineBehaviorIndex);
				_selectedInlineBehaviorIndex = Math.Min(selectedInlineBehaviorIndex, array.Count - 1);
				ReplaceInlineBehaviors(array, "移除内联行为拼图");
			}
		}
	}

	private void MoveSelectedInlineBehavior(int direction)
	{
		if (_editingPacket?.behaviors != null)
		{
			int selectedInlineBehaviorIndex = GetSelectedInlineBehaviorIndex();
			int num = selectedInlineBehaviorIndex + Math.Sign(direction);
			if (selectedInlineBehaviorIndex >= 0 && selectedInlineBehaviorIndex < _editingPacket.behaviors.Count && num >= 0 && num < _editingPacket.behaviors.Count)
			{
				Array<CardBehaviorDefinition> array = new Array<CardBehaviorDefinition>(_editingPacket.behaviors);
				CardBehaviorDefinition value = array[selectedInlineBehaviorIndex];
				array[selectedInlineBehaviorIndex] = array[num];
				array[num] = value;
				_selectedInlineBehaviorIndex = num;
				ReplaceInlineBehaviors(array, (direction < 0) ? "提前内联行为拼图" : "延后内联行为拼图");
			}
		}
	}

	private void ReplaceInlineBehaviors(Array<CardBehaviorDefinition> next, string actionName)
	{
		if (_cardPropertyBinding != null && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPropertyBinding.SetValue(towerDefensePacketConfig, "behaviors", next, actionName, this, "RefreshCardEditorFromHistory");
			towerDefensePacketConfig.NotifyPropertyListChanged();
			RefreshCardBehaviorLists(towerDefensePacketConfig);
			UpdateCardPreview();
		}
	}

	private int GetSelectedInlineBehaviorIndex()
	{
		if (GodotObject.IsInstanceValid(_inlineBehaviorList))
		{
			int[] selectedItems = _inlineBehaviorList.GetSelectedItems();
			if (selectedItems.Length != 0)
			{
				return selectedItems[0];
			}
		}
		return _selectedInlineBehaviorIndex;
	}

	private void OpenSelectedInlineBehavior()
	{
		if (_editingPacket?.behaviors == null)
		{
			return;
		}
		int selectedInlineBehaviorIndex = GetSelectedInlineBehaviorIndex();
		if (selectedInlineBehaviorIndex >= 0 && selectedInlineBehaviorIndex < _editingPacket.behaviors.Count)
		{
			CardBehaviorDefinition cardBehaviorDefinition = _editingPacket.behaviors[selectedInlineBehaviorIndex];
			if (GodotObject.IsInstanceValid(cardBehaviorDefinition))
			{
				OpenInlineBehavior(selectedInlineBehaviorIndex, cardBehaviorDefinition);
			}
		}
	}

	private void OpenInlineBehavior(int index, CardBehaviorDefinition behavior)
	{
		XWResourceEditContext context = XWResourceEditContext.ForProperty(behavior, _editingPacket, behavior.ResourcePath, base.CurrentResourcePath, "behaviors", index, "card_behavior_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
		XWEditorInterface.Instance?.EditResource(behavior, context);
	}

	private void RefreshBehaviorRegistryFromUser()
	{
		_behaviorRegistryRevision = TowerDefenseBehaviorRegistry.Revision;
		BuildRegistryBehaviorPicker();
		RefreshCardBehaviorLists(_editingPacket);
		XWEditorInterface.Instance?.ShowToast("卡牌行为库已刷新。");
	}

	private void RefreshCardBehaviorLists(TowerDefensePacketConfig packet)
	{
		if (packet == null)
		{
			return;
		}
		int value = packet.behaviorIds?.Count ?? 0;
		int value2 = packet.behaviors?.Count ?? 0;
		if (GodotObject.IsInstanceValid(_behaviorSummaryLabel))
		{
			_behaviorSummaryLabel.Text = $"注册行为 {value} · 内联行为 {value2} · 费用规则 {packet.changeCostList?.Count ?? 0}";
		}
		if (_behaviorIcon == null)
		{
			_behaviorIcon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GraphEdit.svg", null, ResourceLoader.CacheMode.Reuse);
		}
		if (_behaviorWarningIcon == null)
		{
			_behaviorWarningIcon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/NodeWarning.svg", null, ResourceLoader.CacheMode.Reuse);
		}
		if (GodotObject.IsInstanceValid(_registryBehaviorList))
		{
			_registryBehaviorList.Clear();
			int num = 0;
			int num2 = 0;
			if (packet.behaviorIds != null)
			{
				for (int i = 0; i < packet.behaviorIds.Count; i++)
				{
					StringName stringName = packet.behaviorIds[i];
					Resource behavior = TowerDefenseBehaviorRegistry.GetBehavior(stringName);
					bool flag = !GodotObject.IsInstanceValid(behavior);
					bool flag2 = !flag && !(behavior is CardBehaviorDefinition);
					string value3 = (flag ? "断开" : (flag2 ? "类型错误" : "已连接"));
					string text = behavior?.GetType().Name ?? "等待 Mod 注册";
					_registryBehaviorList.AddItem($"{i + 1}. ◆ {stringName}  [{value3}]  {text}");
					_registryBehaviorList.SetItemIcon(i, (flag || flag2) ? _behaviorWarningIcon : _behaviorIcon);
					_registryBehaviorList.SetItemCustomFgColor(i, flag ? new Color(1f, 0.42f, 0.38f) : (flag2 ? new Color(1f, 0.68f, 0.25f) : new Color(0.68f, 1f, 0.58f)));
					_registryBehaviorList.SetItemTooltip(i, flag ? $"未找到行为 ID“{stringName}”。保存是安全的；加载对应 Mod 后会自动连接。" : (flag2 ? $"“{stringName}”注册为 {text}，不是卡牌行为，运行时会跳过。" : ("双击打开 " + text + " 的直接属性编辑界面。")));
					if (flag)
					{
						num++;
					}
					else if (flag2)
					{
						num2++;
					}
				}
			}
			if (_selectedRegistryBehaviorIndex >= 0 && _selectedRegistryBehaviorIndex < _registryBehaviorList.ItemCount)
			{
				_registryBehaviorList.Select(_selectedRegistryBehaviorIndex);
			}
			if (GodotObject.IsInstanceValid(_registryBehaviorStatusLabel))
			{
				_registryBehaviorStatusLabel.Text = ((num == 0 && num2 == 0) ? $"行为库版本 {_behaviorRegistryRevision} · 全部拼图连接正常" : $"行为库版本 {_behaviorRegistryRevision} · 断开 {num} · 类型错误 {num2}");
			}
		}
		if (!GodotObject.IsInstanceValid(_inlineBehaviorList))
		{
			return;
		}
		_inlineBehaviorList.Clear();
		if (packet.behaviors != null)
		{
			for (int j = 0; j < packet.behaviors.Count; j++)
			{
				CardBehaviorDefinition cardBehaviorDefinition = packet.behaviors[j];
				bool flag3 = GodotObject.IsInstanceValid(cardBehaviorDefinition);
				string value4 = (flag3 ? cardBehaviorDefinition.GetDiagnosticName() : "空拼图");
				string value5 = (flag3 ? cardBehaviorDefinition.GetType().Name : "资源缺失");
				string value6 = ((flag3 && cardBehaviorDefinition.InitiallyEnabled) ? "启用" : "停用");
				_inlineBehaviorList.AddItem($"{j + 1}. ◆ {value4}  [{value6}]  {value5}");
				_inlineBehaviorList.SetItemIcon(j, flag3 ? _behaviorIcon : _behaviorWarningIcon);
				_inlineBehaviorList.SetItemCustomFgColor(j, (flag3 && cardBehaviorDefinition.InitiallyEnabled) ? new Color(0.58f, 0.9f, 1f) : new Color(0.72f, 0.72f, 0.68f));
				_inlineBehaviorList.SetItemTooltip(j, flag3 ? "双击进入直接属性界面；全部导出属性都可在主面板编辑。" : "该数组位置没有有效行为资源。");
			}
		}
		if (_selectedInlineBehaviorIndex >= 0 && _selectedInlineBehaviorIndex < _inlineBehaviorList.ItemCount)
		{
			_inlineBehaviorList.Select(_selectedInlineBehaviorIndex);
		}
	}

	private void BindCardEventSurface(VBoxContainer root, TowerDefensePacketConfig packet)
	{
		_eventSummaryLabel = root.GetNode<Label>("%EventSummaryLabel");
		root.GetNode<Button>("%AddPressButton").Pressed += delegate
		{
			ShowPacketEventSelector("pressedActions");
		};
		root.GetNode<Button>("%RemovePressButton").Pressed += delegate
		{
			RemoveSelectedPacketEvent("pressedActions");
		};
		root.GetNode<Button>("%AddPlantButton").Pressed += delegate
		{
			ShowPacketEventSelector("useSucceededActions");
		};
		root.GetNode<Button>("%RemovePlantButton").Pressed += delegate
		{
			RemoveSelectedPacketEvent("useSucceededActions");
		};
		root.GetNode<Button>("%MovePressUpButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("pressedActions", -1);
		};
		root.GetNode<Button>("%MovePressDownButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("pressedActions", 1);
		};
		root.GetNode<Button>("%MovePlantUpButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("useSucceededActions", -1);
		};
		root.GetNode<Button>("%MovePlantDownButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("useSucceededActions", 1);
		};
		root.GetNode<Button>("%OpenPressButton").Pressed += delegate
		{
			EditSelectedPacketEvent("pressedActions", GetSelectedPacketEventIndex("pressedActions"));
		};
		root.GetNode<Button>("%OpenPlantButton").Pressed += delegate
		{
			EditSelectedPacketEvent("useSucceededActions", GetSelectedPacketEventIndex("useSucceededActions"));
		};
		_pressedActionsList = root.GetNode<ItemList>("%EventPressList");
		_useSucceededActionsList = root.GetNode<ItemList>("%EventPlantList");
		_unlockConditionList = root.GetNode<ItemList>("%UnlockConditionList");
		_pressedActionsList.ItemSelected += delegate(long index)
		{
			_selectedEventPressIndex = (int)index;
		};
		_useSucceededActionsList.ItemSelected += delegate(long index)
		{
			_selectedEventPlantIndex = (int)index;
		};
		_pressedActionsList.ItemActivated += delegate(long index)
		{
			EditSelectedPacketEvent("pressedActions", (int)index);
		};
		_useSucceededActionsList.ItemActivated += delegate(long index)
		{
			EditSelectedPacketEvent("useSucceededActions", (int)index);
		};
		_unlockConditionList.ItemActivated += EditSelectedUnlockCondition;
		_unlockConditionList.ItemSelected += delegate(long index)
		{
			_selectedUnlockIndex = (int)index;
			if (_editingPacket?.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _editingPacket.unlockCheckList.Count)
			{
				_unlockConditionPicker?.SetEditedResource(_editingPacket.unlockCheckList[_selectedUnlockIndex]);
			}
		};
		RefreshCardLists(packet);
	}

	private void EditSelectedUnlockCondition(long index)
	{
		if (GodotObject.IsInstanceValid(_editingPacket) && _editingPacket.unlockCheckList != null && index >= 0 && index < _editingPacket.unlockCheckList.Count)
		{
			UnlockConditionBaseConfig unlockConditionBaseConfig = _editingPacket.unlockCheckList[(int)index];
			if (GodotObject.IsInstanceValid(unlockConditionBaseConfig))
			{
				XWResourceEditContext context = XWResourceEditContext.ForProperty(unlockConditionBaseConfig, _editingPacket, unlockConditionBaseConfig.ResourcePath, base.CurrentResourcePath, "unlockCheckList", (int)index, "card_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(base.CurrentResourcePath));
				XWEditorInterface.Instance?.EditResource(unlockConditionBaseConfig, context);
			}
		}
	}

	private void ShowPacketEventSelector(string targetProperty)
	{
		_packetEventTargetProperty = ((targetProperty == "useSucceededActions") ? "useSucceededActions" : "pressedActions");
		EnsurePacketEventSelectorWindow();
		if (GodotObject.IsInstanceValid(_packetEventSelectorWindow))
		{
			string title = ((_packetEventTargetProperty == "useSucceededActions") ? "种植事件拼图" : "按下事件拼图");
			string subtitle = ((_packetEventTargetProperty == "useSucceededActions") ? "完成种植时按顺序执行这些事件" : "拿起或按下卡牌时按顺序执行这些事件");
			_packetEventSelectorWindow.ShowFor(title, subtitle, OnPacketEventSelected);
		}
	}

	private void EnsurePacketEventSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_packetEventSelectorWindow))
		{
			if (_packetEventSelectorScene == null)
			{
				_packetEventSelectorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketEventSelectorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_packetEventSelectorWindow = _packetEventSelectorScene?.Instantiate<XWPacketEventSelectorWindow>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_packetEventSelectorWindow))
			{
				AddChild(_packetEventSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OnPacketEventSelected(Type runtimeType)
	{
		CardActionBehaviorDefinition packetEvent = CreatePacketEventInstance(runtimeType);
		AddPacketEventToTarget(_packetEventTargetProperty, packetEvent);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Packet event selector creates editor-only instances from loaded runtime event types discovered for Mod authoring.")]
	private static CardActionBehaviorDefinition CreatePacketEventInstance(Type type)
	{
		if (type == null || !typeof(CardActionBehaviorDefinition).IsAssignableFrom(type))
		{
			return null;
		}
		try
		{
			return Activator.CreateInstance(type) as CardActionBehaviorDefinition;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Card packet event create failed: " + type.FullName + " " + ex.Message);
			return null;
		}
	}

	private static string GetPacketEventName(CardActionBehaviorDefinition packetEvent, Type type)
	{
		if (GodotObject.IsInstanceValid(packetEvent))
		{
			try
			{
				Dictionary dictionary = packetEvent.ExportConfiguration();
				if (dictionary != null && dictionary.ContainsKey("EventName"))
				{
					string text = dictionary["EventName"].AsString();
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text;
					}
				}
			}
			catch
			{
			}
		}
		string text2 = type?.Name ?? "PacketEvent";
		if (!text2.StartsWith("CardActionBehavior", StringComparison.Ordinal))
		{
			return text2;
		}
		string text3 = text2;
		int length = "CardActionBehavior".Length;
		return text3.Substring(length, text3.Length - length);
	}

	private void AddPacketEventToTarget(string propertyName, CardActionBehaviorDefinition packetEvent)
	{
		if (_editingPacket != null && GodotObject.IsInstanceValid(packetEvent))
		{
			Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
			Array<CardActionBehaviorDefinition> array = ((packetEventArray == null) ? new Array<CardActionBehaviorDefinition>() : new Array<CardActionBehaviorDefinition>(packetEventArray));
			array.Add(packetEvent);
			if (propertyName == "useSucceededActions")
			{
				_selectedEventPlantIndex = array.Count - 1;
			}
			else
			{
				_selectedEventPressIndex = array.Count - 1;
			}
			ReplacePacketEvents(propertyName, array, "添加卡牌事件 " + propertyName);
			OpenPacketEventResource(propertyName, array.Count - 1, packetEvent);
			XWEditorInterface.Instance?.AddOutputMessage("卡牌编辑器: 添加事件 " + GetPacketEventName(packetEvent, packetEvent.GetType()) + " -> " + propertyName);
		}
	}

	private void RemoveSelectedPacketEvent(string propertyName)
	{
		if (_editingPacket == null)
		{
			return;
		}
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		int selectedPacketEventIndex = GetSelectedPacketEventIndex(propertyName);
		if (packetEventArray != null && selectedPacketEventIndex >= 0 && selectedPacketEventIndex < packetEventArray.Count)
		{
			Array<CardActionBehaviorDefinition> array = new Array<CardActionBehaviorDefinition>(packetEventArray);
			array.RemoveAt(selectedPacketEventIndex);
			if (propertyName == "useSucceededActions")
			{
				_selectedEventPlantIndex = Math.Min(selectedPacketEventIndex, array.Count - 1);
			}
			else
			{
				_selectedEventPressIndex = Math.Min(selectedPacketEventIndex, array.Count - 1);
			}
			ReplacePacketEvents(propertyName, array, "删除卡牌事件 " + propertyName);
			XWEditorInterface.Instance?.AddOutputMessage($"卡牌编辑器: 删除事件 {propertyName}[{selectedPacketEventIndex}]");
		}
	}

	private void MoveSelectedPacketEvent(string propertyName, int direction)
	{
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		int selectedPacketEventIndex = GetSelectedPacketEventIndex(propertyName);
		if (packetEventArray == null || selectedPacketEventIndex < 0 || selectedPacketEventIndex >= packetEventArray.Count)
		{
			return;
		}
		int num = selectedPacketEventIndex + Math.Sign(direction);
		if (num >= 0 && num < packetEventArray.Count)
		{
			Array<CardActionBehaviorDefinition> array = new Array<CardActionBehaviorDefinition>(packetEventArray);
			CardActionBehaviorDefinition value = array[selectedPacketEventIndex];
			array[selectedPacketEventIndex] = array[num];
			array[num] = value;
			if (propertyName == "useSucceededActions")
			{
				_selectedEventPlantIndex = num;
			}
			else
			{
				_selectedEventPressIndex = num;
			}
			ReplacePacketEvents(propertyName, array, (direction < 0) ? "上移卡牌事件" : "下移卡牌事件");
		}
	}

	private void ReplacePacketEvents(string propertyName, Array<CardActionBehaviorDefinition> next, string actionName)
	{
		if (_cardPropertyBinding != null && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPropertyBinding.SetValue(towerDefensePacketConfig, propertyName, next, actionName, this, "RefreshCardEditorFromHistory");
			towerDefensePacketConfig.NotifyPropertyListChanged();
			RefreshCardLists(towerDefensePacketConfig);
			UpdateCardPreview();
		}
	}

	private void EditSelectedPacketEvent(string propertyName, int index)
	{
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		if (packetEventArray != null && index >= 0 && index < packetEventArray.Count)
		{
			CardActionBehaviorDefinition cardActionBehaviorDefinition = packetEventArray[index];
			if (GodotObject.IsInstanceValid(cardActionBehaviorDefinition))
			{
				OpenPacketEventResource(propertyName, index, cardActionBehaviorDefinition);
			}
		}
	}

	private void OpenPacketEventResource(string propertyName, int index, CardActionBehaviorDefinition packetEvent)
	{
		if (GodotObject.IsInstanceValid(_editingPacket) && GodotObject.IsInstanceValid(packetEvent))
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(packetEvent, _editingPacket, packetEvent.ResourcePath, base.CurrentResourcePath, propertyName, index, "card_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
			XWEditorInterface.Instance?.EditResource(packetEvent, context);
		}
	}

	private Array<CardActionBehaviorDefinition> GetPacketEventArray(string propertyName)
	{
		if (_editingPacket == null)
		{
			return null;
		}
		if (propertyName == "useSucceededActions")
		{
			return _editingPacket.useSucceededActions;
		}
		return _editingPacket.pressedActions;
	}

	private int GetSelectedPacketEventIndex(string propertyName)
	{
		if (propertyName == "useSucceededActions")
		{
			if (GodotObject.IsInstanceValid(_useSucceededActionsList))
			{
				int[] selectedItems = _useSucceededActionsList.GetSelectedItems();
				if (selectedItems.Length != 0)
				{
					return selectedItems[0];
				}
			}
			return _selectedEventPlantIndex;
		}
		if (GodotObject.IsInstanceValid(_pressedActionsList))
		{
			int[] selectedItems2 = _pressedActionsList.GetSelectedItems();
			if (selectedItems2.Length != 0)
			{
				return selectedItems2[0];
			}
		}
		return _selectedEventPressIndex;
	}

	private void OnCardPropertyEdited(bool committed)
	{
		if (base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig && towerDefensePacketConfig == _editingPacket)
		{
			_cardPreviewRefreshCommitted = committed;
			towerDefensePacketConfig.EmitChanged();
			if (committed)
			{
				NotifyCurrentResourceEdited();
				SaveCardResource(towerDefensePacketConfig);
			}
			else
			{
				MarkCurrentResourceDirty();
			}
		}
	}

	public void RefreshCardEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			_cardPreviewRefreshCommitted = true;
			_editingPacket = towerDefensePacketConfig;
			_cardPropertyBinding?.RefreshBoundControls();
			SyncCardVector2HistoryControls(towerDefensePacketConfig);
			Control host = base.CanvasGrid?.FindChild("TypeCards", recursive: true, owned: false) as Control;
			int type = (int)towerDefensePacketConfig.type;
			SelectVisualCard(host, type.ToString());
			SelectVisualCard(base.CanvasGrid?.FindChild("SpawnMethodCards", recursive: true, owned: false) as Control, towerDefensePacketConfig.spawnMethod ?? "");
			RefreshCardLists(towerDefensePacketConfig);
			RefreshCharacterBindingSelector(towerDefensePacketConfig.characterConfig);
			UpdatePacketOverrideEntry();
			UpdateCardPreview();
			SaveCardResource(towerDefensePacketConfig);
		}
	}

	private void SyncCardVector2HistoryControls(TowerDefensePacketConfig packet)
	{
		if (base.CanvasGrid != null && GodotObject.IsInstanceValid(packet))
		{
			SyncCardVector2HistoryControl(packet, "packetAnimeOffset", "PacketAnimeOffsetX", "PacketAnimeOffsetY");
			SyncCardVector2HistoryControl(packet, "packetAnimeScale", "PacketAnimeScaleX", "PacketAnimeScaleY");
			SyncCardVector2HistoryControl(packet, "handbookPacketAnimeOffset", "HandbookPacketAnimeOffsetX", "HandbookPacketAnimeOffsetY");
		}
	}

	private void SyncCardVector2HistoryControl(TowerDefensePacketConfig packet, StringName property, string xControlName, string yControlName)
	{
		SpinBox spinBox = base.CanvasGrid?.FindChild(xControlName, recursive: true, owned: false) as SpinBox;
		SpinBox spinBox2 = base.CanvasGrid?.FindChild(yControlName, recursive: true, owned: false) as SpinBox;
		if (GodotObject.IsInstanceValid(spinBox) && GodotObject.IsInstanceValid(spinBox2))
		{
			Vector2 vector = packet.Get(property).AsVector2();
			spinBox.SetValueNoSignal(vector.X);
			spinBox2.SetValueNoSignal(vector.Y);
		}
	}

	private void SaveCardResource(TowerDefensePacketConfig packet)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(packet))
		{
			return;
		}
		Error error = ResourceSaver.Save(packet, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Card editor save failed: {error} {currentResourcePath}");
		}
		else
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPath(text, currentResourcePath);
			}
		}
	}

	private void UpdateCardPreview()
	{
		if (_editingPacket == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_cardFrame))
			{
				_cardFrame.AddThemeStyleboxOverride("panel", CreatePanelStyle(GetPacketTypeColor(_editingPacket.type), 2, 6));
			}
			if (GodotObject.IsInstanceValid(_previewNameLabel))
			{
				_previewNameLabel.Text = EmptyToPlaceholder(_editingPacket.name);
			}
			if (GodotObject.IsInstanceValid(_previewKeyLabel))
			{
				_previewKeyLabel.Text = "资源键：" + EmptyToPlaceholder(_editingPacket.saveKey);
			}
			if (GodotObject.IsInstanceValid(_previewCostLabel))
			{
				_previewCostLabel.Text = "☀";
			}
			if (GodotObject.IsInstanceValid(_inlineCostSpinBox))
			{
				_inlineCostSpinBox.Value = _editingPacket.overrideCost;
			}
			if (GodotObject.IsInstanceValid(_inlineSaveKeyLineEdit))
			{
				_inlineSaveKeyLineEdit.Text = _editingPacket.saveKey ?? "";
			}
			if (GodotObject.IsInstanceValid(_inlineCooldownSpinBox))
			{
				_inlineCooldownSpinBox.Value = _editingPacket.overridePacketCooldown;
			}
			if (GodotObject.IsInstanceValid(_inlineStartingCooldownSpinBox))
			{
				_inlineStartingCooldownSpinBox.Value = _editingPacket.overrideStartingCooldown;
			}
			if (GodotObject.IsInstanceValid(_inlineAnimationLineEdit))
			{
				_inlineAnimationLineEdit.Text = _editingPacket.packetAnimeClip ?? "";
			}
			if (GodotObject.IsInstanceValid(_previewCooldownLabel))
			{
				_previewCooldownLabel.Text = "冷却：" + GetEffectiveCooldownText(_editingPacket);
			}
			if (GodotObject.IsInstanceValid(_previewTypeLabel))
			{
				_previewTypeLabel.Text = "卡面：" + GetPacketTypeDisplayName(_editingPacket.type);
			}
			if (GodotObject.IsInstanceValid(_previewCharacterLabel))
			{
				_previewCharacterLabel.Text = "角色：" + FormatCharacterBinding(_editingPacket.characterConfig);
			}
			if (GodotObject.IsInstanceValid(_previewAnimationLabel))
			{
				_previewAnimationLabel.Text = "动画片段：" + EmptyToPlaceholder(_editingPacket.packetAnimeClip);
			}
			if (GodotObject.IsInstanceValid(_valueOverrideBadge))
			{
				_valueOverrideBadge.Text = BuildCardValueOverrideSummary(_editingPacket);
			}
			RefreshPacketShowPreview(_editingPacket);
			RebuildCardCharacterPreview();
			RefreshCharacterBindingSelector(_editingPacket.characterConfig);
			UpdatePacketOverrideEntry();
		}
		finally
		{
			_updatingControls = false;
			_cardPreviewRefreshCommitted = true;
		}
	}

	private void CreatePacketOverride()
	{
		if (GodotObject.IsInstanceValid(_editingPacket))
		{
			SetPacketOverride(new TowerDefensePacketOverride(), "创建卡牌覆盖规则");
			UpdatePacketOverrideEntry();
			OpenPacketOverride();
		}
	}

	private void OpenPacketOverride()
	{
		if (GodotObject.IsInstanceValid(_editingPacket) && GodotObject.IsInstanceValid(_editingPacket._override))
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(_editingPacket._override, _editingPacket, _editingPacket._override.ResourcePath, base.CurrentResourcePath, "_override", -1, "card_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(base.CurrentResourcePath));
			XWEditorInterface.Instance?.EditResource(_editingPacket._override, context);
		}
	}

	private void ClearPacketOverride()
	{
		if (GodotObject.IsInstanceValid(_editingPacket))
		{
			SetPacketOverride(null, "清除卡牌覆盖规则");
			UpdateCardPreview();
			UpdatePacketOverrideEntry();
		}
	}

	private void SetPacketOverride(TowerDefensePacketOverride value, string actionName)
	{
		if (GodotObject.IsInstanceValid(_editingPacket) && _editingPacket._override != value)
		{
			TowerDefensePacketOverride from = _editingPacket._override;
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				_editingPacket._override = value;
			}
			else
			{
				xWUndoRedoManager.CreateAction(actionName);
				xWUndoRedoManager.AddDoProperty(_editingPacket, "_override", Variant.From(in value));
				xWUndoRedoManager.AddUndoProperty(_editingPacket, "_override", Variant.From(in from));
				xWUndoRedoManager.AddDoMethod(this, "RefreshCardEditorFromHistory");
				xWUndoRedoManager.AddUndoMethod(this, "RefreshCardEditorFromHistory");
				xWUndoRedoManager.CommitAction();
			}
			_editingPacket.NotifyPropertyListChanged();
			NotifyCurrentResourceEdited();
			SaveCardResource(_editingPacket);
		}
	}

	private void UpdatePacketOverrideEntry()
	{
		bool flag = GodotObject.IsInstanceValid(_editingPacket) && GodotObject.IsInstanceValid(_editingPacket._override);
		if (GodotObject.IsInstanceValid(_overrideStatus))
		{
			_overrideStatus.Text = (flag ? $"已配置：{_editingPacket._override.type}，费用 {_editingPacket._override.cost}，冷却 {_editingPacket._override.packetCooldown:0.##}" : "当前卡牌未配置覆盖规则；创建后可比较原始卡牌与覆盖结果。");
		}
		if (GodotObject.IsInstanceValid(_createOverrideButton))
		{
			_createOverrideButton.Disabled = flag;
		}
		if (GodotObject.IsInstanceValid(_openOverrideButton))
		{
			_openOverrideButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_clearOverrideButton))
		{
			_clearOverrideButton.Disabled = !flag;
		}
	}

	private void RefreshCardLists()
	{
		if (_editingPacket != null)
		{
			RefreshCardLists(_editingPacket);
		}
	}

	private void RefreshCardLists(TowerDefensePacketConfig packet)
	{
		if (packet != null)
		{
			if (GodotObject.IsInstanceValid(_eventSummaryLabel))
			{
				_eventSummaryLabel.Text = $"事件与解锁 · 拿起 {packet.pressedActions?.Count ?? 0} · 种植 {packet.useSucceededActions?.Count ?? 0} · 解锁条件 {packet.unlockCheckList?.Count ?? 0}";
			}
			PopulateResourceList(_pressedActionsList, packet.pressedActions);
			PopulateResourceList(_useSucceededActionsList, packet.useSucceededActions);
			PopulateResourceList(_unlockConditionList, packet.unlockCheckList);
			if (GodotObject.IsInstanceValid(_pressedActionsList) && _selectedEventPressIndex >= 0 && _selectedEventPressIndex < _pressedActionsList.ItemCount)
			{
				_pressedActionsList.Select(_selectedEventPressIndex);
			}
			if (GodotObject.IsInstanceValid(_useSucceededActionsList) && _selectedEventPlantIndex >= 0 && _selectedEventPlantIndex < _useSucceededActionsList.ItemCount)
			{
				_useSucceededActionsList.Select(_selectedEventPlantIndex);
			}
			if (GodotObject.IsInstanceValid(_unlockConditionList) && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _unlockConditionList.ItemCount)
			{
				_unlockConditionList.Select(_selectedUnlockIndex);
			}
			RefreshCardBehaviorLists(packet);
			RefreshArmorList(packet);
		}
	}

	private void AddSummaryRows(TowerDefensePacketConfig packet)
	{
		AddItemIfMissing(base.PreviewList, "卡片: " + EmptyToPlaceholder(packet.name));
		AddItemIfMissing(base.PreviewList, "费用/冷却: " + GetEffectiveCostText(packet) + " / " + GetEffectiveCooldownText(packet));
		AddItemIfMissing(base.TimelineList, "卡片 key: " + EmptyToPlaceholder(packet.saveKey));
		AddItemIfMissing(base.TimelineList, $"事件: 拿起 {packet.pressedActions?.Count ?? 0}，种植 {packet.useSucceededActions?.Count ?? 0}");
		AddItemIfMissing(base.GraphList, $"行为顺序: 注册 {packet.behaviorIds?.Count ?? 0} → 内联 {packet.behaviors?.Count ?? 0} → 费用规则 {packet.changeCostList?.Count ?? 0}");
		AddItemIfMissing(base.GraphList, "卡片 -> characterConfig -> " + FormatCharacterBinding(packet.characterConfig));
		AddItemIfMissing(base.GraphList, "卡片 -> packetAnimeClip -> " + EmptyToPlaceholder(packet.packetAnimeClip));
		AddItemIfMissing(base.ReferenceList, "绑定角色 characterConfig -> " + FormatCharacterBinding(packet.characterConfig));
		AddItemIfMissing(base.ReferenceList, "卡牌动画 packetAnimeClip -> " + EmptyToPlaceholder(packet.packetAnimeClip));
		AddItemIfMissing(base.ReferenceList, $"解锁条件 unlockCheckList -> {packet.unlockCheckList?.Count ?? 0}");
	}

	private void BuildRuntimeStateCards()
	{
		if (!GodotObject.IsInstanceValid(_runtimeStateCards))
		{
			return;
		}
		ClearVisualCards(_runtimeStateCards);
		(string, string, string)[] array = new(string, string, string)[5]
		{
			("可种植", "准备", "res://addons/ModEditor/Icons/ResourceShovel.svg"),
			("已选中", "手持", "res://addons/ModEditor/Icons/Pin.svg"),
			("冷却中", "计时", "res://addons/ModEditor/Icons/Progress1.svg"),
			("阳光不足", "灰暗", "res://addons/ModEditor/Icons/NodeWarning.svg"),
			("已锁定", "禁用", "res://addons/ModEditor/Icons/Lock.svg")
		};
		for (int i = 0; i < array.Length; i++)
		{
			int selectedState = i;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = CreateVisualChoiceCard(i.ToString(), array[i].Item1, array[i].Item2, array[i].Item3, i == _cardRuntimePreviewState, new Vector2(124f, 58f));
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Pressed += delegate
				{
					SetCardRuntimePreviewState(selectedState);
				};
				_runtimeStateCards.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void SetCardRuntimePreviewState(int state)
	{
		if (!_updatingControls)
		{
			_cardRuntimePreviewState = Math.Clamp(state, 0, 4);
			if (_cardRuntimePreviewState == 2)
			{
				EnsureCardRuntimeCooldownDuration();
				_cardRuntimeCooldownRemaining = _cardRuntimeCooldownDuration * (double)Math.Max(0.001f, _cardRuntimeCooldownProgress);
			}
			else
			{
				StopCardRuntimeClock();
			}
			ApplyCardRuntimePreviewState();
		}
	}

	private void BuildPacketTypeCards(HFlowContainer host, TowerDefenseEnum.PACKET_TYPE value)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		ClearVisualCards(host);
		TowerDefenseEnum.PACKET_TYPE[] values = Enum.GetValues<TowerDefenseEnum.PACKET_TYPE>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.PACKET_TYPE pACKET_TYPE = values[i];
			TowerDefenseEnum.PACKET_TYPE selectedType = pACKET_TYPE;
			int num = (int)pACKET_TYPE;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = CreateVisualChoiceCard(num.ToString(), GetPacketTypeDisplayName(pACKET_TYPE), pACKET_TYPE.ToString(), GetPacketTexturePath(pACKET_TYPE), pACKET_TYPE == value, new Vector2(138f, 66f));
			if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				continue;
			}
			xWGameVisualChoiceCard.Pressed += delegate
			{
				if (!_updatingControls)
				{
					_cardPropertyBinding?.SetValue(_editingPacket, "type", (int)selectedType, "修改卡牌品质", this, "RefreshCardEditorFromHistory");
					HFlowContainer host2 = host;
					int num2 = (int)selectedType;
					SelectVisualCard(host2, num2.ToString());
					UpdateCardPreview();
				}
			};
			host.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BuildSpawnMethodCards(HFlowContainer host, TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		ClearVisualCards(host);
		(string, string, string, string)[] array = new(string, string, string, string)[3]
		{
			("Noone", "原地出现", "不播放入场位移", "res://addons/ModEditor/Icons/Pin.svg"),
			("Rise", "破土上升", "从地面升起", "res://addons/ModEditor/Icons/GuiArrowUp.svg"),
			("FlyDown", "空投落地", "从画面上方降落", "res://addons/ModEditor/Icons/MoveDown.svg")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, string) tuple = array[i];
			string key = tuple.Item1;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = CreateVisualChoiceCard(key, tuple.Item2, tuple.Item3, tuple.Item4, string.Equals(packet.spawnMethod, key, StringComparison.OrdinalIgnoreCase), new Vector2(160f, 66f));
			if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				continue;
			}
			xWGameVisualChoiceCard.Pressed += delegate
			{
				if (!_updatingControls)
				{
					_cardPropertyBinding?.SetValue(packet, "spawnMethod", key, "修改卡牌出场方式", this, "RefreshCardEditorFromHistory");
					SelectVisualCard(host, key);
					UpdateCardPreview();
				}
			};
			host.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static void ClearVisualCards(Control host)
	{
		foreach (Node child in host.GetChildren())
		{
			host.RemoveChild(child);
			child.QueueFree();
		}
	}

	private XWGameVisualChoiceCard CreateVisualChoiceCard(string key, string title, string detail, string iconPath, bool selected, Vector2 minimumSize)
	{
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
		{
			return null;
		}
		xWGameVisualChoiceCard.CustomMinimumSize = minimumSize;
		xWGameVisualChoiceCard.Configure(key, title, detail, ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse), selected);
		return xWGameVisualChoiceCard;
	}

	private static void SelectVisualCard(Control host, string selectedKey)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		foreach (Node child in host.GetChildren())
		{
			if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard)
			{
				xWGameVisualChoiceCard.SetSelected(xWGameVisualChoiceCard.ChoiceKey == selectedKey);
			}
		}
	}

	private void BindCardVector2Field(SpinBox x, SpinBox y, TowerDefensePacketConfig packet, StringName property)
	{
		Vector2 vector = packet.Get(property).AsVector2();
		x.Value = vector.X;
		y.Value = vector.Y;
		Action value = delegate
		{
			_cardPropertyBinding?.BeginEdit(packet, property);
		};
		Godot.Range.ValueChangedEventHandler value2 = delegate
		{
			if (!_updatingControls)
			{
				_cardPropertyBinding?.PreviewValue(packet, property, ReadValue());
				UpdateCardPreview();
			}
		};
		Action value3 = delegate
		{
			if (!_updatingControls)
			{
				_cardPropertyBinding?.CommitEdit(packet, property, ReadValue(), $"修改 {property}", this, "RefreshCardEditorFromHistory");
				UpdateCardPreview();
			}
		};
		x.FocusEntered += value;
		y.FocusEntered += value;
		x.ValueChanged += value2;
		y.ValueChanged += value2;
		x.FocusExited += value3;
		y.FocusExited += value3;
		Variant ReadValue()
		{
			return Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value));
		}
	}

	private void RefreshCharacterBindingSelector(TowerDefenseCharacterConfig current)
	{
		if (!GodotObject.IsInstanceValid(_characterBindingButton) && !GodotObject.IsInstanceValid(_characterSourceLabel))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_characterBindingButton))
		{
			if (!GodotObject.IsInstanceValid(current))
			{
				_characterBindingButton.Text = "选择角色...";
			}
			else
			{
				CardCharacterSource source = ResolveCharacterSource(current.ResourcePath);
				string characterKey = GetCharacterKey(current, current.ResourcePath);
				_characterBindingButton.Text = "更换: " + GetCharacterSourceTag(source) + " " + BuildCharacterDisplayName(characterKey, current.ResourcePath);
			}
		}
		UpdateCharacterSourceLabel(current);
	}

	private void ShowCharacterSelectorWindow()
	{
		EnsureCharacterResourcePicker();
		if (GodotObject.IsInstanceValid(_characterResourcePicker))
		{
			TowerDefenseCharacterConfig towerDefenseCharacterConfig = _editingPacket?.characterConfig;
			string currentPath = (GodotObject.IsInstanceValid(towerDefenseCharacterConfig) ? towerDefenseCharacterConfig.ResourcePath : "");
			string projectPath = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			_characterResourcePicker.OpenResourceLibrary("Character", "角色", currentPath, projectPath, new string[1] { "TowerDefenseCharacterConfig" }, new string[1] { "Asset/Anime/Character" }, "res://addons/ModEditor/Icons/ResourceCharacter.svg", ApplySelectedCharacterChoice);
		}
	}

	private void EnsureCharacterResourcePicker()
	{
		if (!GodotObject.IsInstanceValid(_characterResourcePicker))
		{
			_characterResourcePicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_characterResourcePicker))
			{
				AddChild(_characterResourcePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ApplySelectedCharacterChoice(XWGameplayResourceChoice choice)
	{
		if (!_updatingControls && !(choice == null))
		{
			TowerDefenseCharacterConfig towerDefenseCharacterConfig = LoadCharacterConfig(choice.ResourcePath);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacterConfig))
			{
				GD.PushWarning("Card character binding failed: " + choice.ResourcePath);
				return;
			}
			SetCharacterBindingResource(towerDefenseCharacterConfig);
			UpdateCharacterSourceLabel(towerDefenseCharacterConfig);
		}
	}

	private void ClearCharacterBinding()
	{
		SetCharacterBindingResource(null);
		UpdateCharacterSourceLabel(null);
	}

	private void SetCharacterBindingResource(TowerDefenseCharacterConfig character)
	{
		if (_editingPacket != null)
		{
			_cardPropertyBinding?.SetValue(_editingPacket, "characterConfig", Variant.From(in character), "更换卡牌角色绑定", this, "RefreshCardEditorFromHistory");
			_editingPacket.NotifyPropertyListChanged();
			UpdateCardPreview();
			XWEditorInterface.Instance?.AddOutputMessage("卡片编辑器: 绑定角色 " + FormatCharacterBinding(character));
		}
	}

	private static TowerDefenseCharacterConfig LoadCharacterConfig(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}
		if (!IsCharacterConfigResourcePath(resourcePath))
		{
			return null;
		}
		try
		{
			return (ResourceLoader.Load<Resource>(resourcePath, "", ResourceLoader.CacheMode.Ignore) is TowerDefenseCharacterConfig towerDefenseCharacterConfig) ? towerDefenseCharacterConfig : null;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Card character option load failed: " + resourcePath + " " + ex.Message);
			return null;
		}
	}

	private static bool IsCharacterConfigResourcePath(string resourcePath)
	{
		string text = NormalizeResourcePath(resourcePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.GetExtension().ToLowerInvariant();
		if (!(text2 == "tres") && !(text2 == "res"))
		{
			return false;
		}
		if (!text.Contains("/Config/", StringComparison.OrdinalIgnoreCase) && !text.EndsWith("Config.tres", StringComparison.OrdinalIgnoreCase) && !text.EndsWith("Config.res", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private void UpdateCharacterSourceLabel(TowerDefenseCharacterConfig current)
	{
		if (GodotObject.IsInstanceValid(_characterSourceLabel))
		{
			if (!GodotObject.IsInstanceValid(current))
			{
				_characterSourceLabel.Text = "来源: 未绑定";
				return;
			}
			CardCharacterSource cardCharacterSource = ResolveCharacterSource(current.ResourcePath);
			_characterSourceLabel.Text = ((cardCharacterSource == CardCharacterSource.Mod) ? ("来源: Mod 角色  " + FormatCharacterBinding(current)) : ("来源: 游戏内置角色  " + FormatCharacterBinding(current)));
		}
	}

	private static string GetCharacterSourceTag(CardCharacterSource source)
	{
		return source switch
		{
			CardCharacterSource.Mod => "[Mod]", 
			CardCharacterSource.BuiltIn => "[内置]", 
			_ => "[Mod]", 
		};
	}

	private static CardCharacterSource ResolveCharacterSource(string resourcePath)
	{
		if (IsBuiltInCharacterPath(resourcePath))
		{
			return CardCharacterSource.BuiltIn;
		}
		return CardCharacterSource.Mod;
	}

	private static bool IsBuiltInCharacterPath(string resourcePath)
	{
		string text = NormalizeResourcePath(resourcePath);
		if (text.StartsWith("res://Asset/Anime/Character", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text2 = NormalizeResourcePath(ProjectSettings.GlobalizePath("res://Asset/Anime/Character")).TrimEnd('/');
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text.StartsWith(text2 + "/", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string NormalizeResourcePath(string path)
	{
		return (path ?? "").Replace('\\', '/').Trim();
	}

	private static string GetCharacterKey(TowerDefenseCharacterConfig character, string resourcePath)
	{
		if (GodotObject.IsInstanceValid(character) && !string.IsNullOrWhiteSpace(character.name))
		{
			return character.name;
		}
		if (GodotObject.IsInstanceValid(character) && !string.IsNullOrWhiteSpace(character.ResourceName))
		{
			return character.ResourceName;
		}
		if (!string.IsNullOrWhiteSpace(resourcePath))
		{
			return resourcePath.GetFile().GetBaseName();
		}
		return "未命名角色";
	}

	private static string BuildCharacterDisplayName(string key, string resourcePath)
	{
		string text = (string.IsNullOrWhiteSpace(resourcePath) ? "" : resourcePath.GetFile());
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text.GetBaseName(), key, StringComparison.OrdinalIgnoreCase))
		{
			return EmptyToPlaceholder(key);
		}
		return EmptyToPlaceholder(key) + " (" + text + ")";
	}

	private static void PopulateResourceList<[MustBeVariant] T>(ItemList list, Array<T> array)
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
			list.AddItem($"{i}: {FormatObject(array[i])}");
		}
	}

	private static string FormatObject(object value)
	{
		if (value is Resource resource)
		{
			return FormatResource(resource);
		}
		if (!(value is GodotObject godotObject))
		{
			return value?.ToString() ?? "空";
		}
		return godotObject.GetType().Name;
	}

	private static StyleBoxFlat CreatePanelStyle(Color color, int borderWidth, int cornerRadius)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = cornerRadius,
			CornerRadiusTopRight = cornerRadius,
			CornerRadiusBottomLeft = cornerRadius,
			CornerRadiusBottomRight = cornerRadius,
			ContentMarginLeft = 10f,
			ContentMarginRight = 10f,
			ContentMarginTop = 10f,
			ContentMarginBottom = 10f,
			BorderColor = color.Lightened(0.22f),
			BorderWidthLeft = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthBottom = borderWidth
		};
	}

	private void RefreshPacketShowPreview(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(_cardPacketShowPreviewRoot))
		{
			return;
		}
		if (packet == null || !GodotObject.IsInstanceValid(packet.characterConfig))
		{
			if (GodotObject.IsInstanceValid(_cardPacketShowPreview))
			{
				_cardPacketShowPreview.QueueFree();
				_cardPacketShowPreview = null;
			}
			_cardPacketShowSpriteFingerprint = "";
			_cardPacketShowAnimationFingerprint = "";
			_cardPacketShowSaveKeyFingerprint = "";
			SetPacketShowPreviewMissing(missing: true);
			return;
		}
		if (GodotObject.IsInstanceValid(_cardPacketShowPreview))
		{
			ConfigurePacketShowPreview(_cardPacketShowPreview, packet, forceSpriteRebuild: false, trackMainPreview: true);
			SetPacketShowPreviewMissing(missing: false);
			PositionPacketShowPreview();
			ApplyCardRuntimePreviewState();
			return;
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		try
		{
			towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				SetPacketShowPreviewMissing(missing: true);
				return;
			}
			towerDefenseInGamePacketShow.Name = "CardPacketShowPreview";
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.setPcLayout = !_cardRuntimeMobile;
			towerDefenseInGamePacketShow.setMobileLayout = _cardRuntimeMobile;
			towerDefenseInGamePacketShow.showLove = false;
			towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
			towerDefenseInGamePacketShow.ProcessMode = ProcessModeEnum.Inherit;
			_cardPacketShowPreviewRoot.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			ConfigurePacketShowPreview(towerDefenseInGamePacketShow, packet, forceSpriteRebuild: true, trackMainPreview: true);
			_cardPacketShowPreview = towerDefenseInGamePacketShow;
			_cardPacketShowPreviewBuildCount++;
			SetPacketShowPreviewMissing(missing: false);
			PositionPacketShowPreview();
			ApplyCardRuntimePreviewState();
		}
		catch (Exception ex)
		{
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.QueueFree();
			}
			_cardPacketShowPreview = null;
			SetPacketShowPreviewMissing(missing: true);
			GD.PushWarning("Card PacketShow preview failed: " + ex.Message);
		}
	}

	private void ConfigurePacketShowPreview(TowerDefenseInGamePacketShow packetShow, TowerDefensePacketConfig packet, bool forceSpriteRebuild = false, bool trackMainPreview = false)
	{
		if (!GodotObject.IsInstanceValid(packetShow) || packet == null)
		{
			return;
		}
		XWCardBehaviorPreviewSnapshot xWCardBehaviorPreviewSnapshot = XWCardBehaviorPreviewSafety.Capture(packet);
		packetShow.onlyDraw = true;
		ApplyPacketShowPreviewLayout(packetShow);
		packetShow.showLove = false;
		packetShow.select = false;
		packetShow.alive = true;
		packetShow.@lock = false;
		packetShow.MouseFilter = MouseFilterEnum.Ignore;
		string text = BuildPacketShowSaveKeyFingerprint(packet);
		string text2 = BuildPacketShowSpriteFingerprint(packet);
		string text3 = packet.packetAnimeClip ?? "";
		bool flag = trackMainPreview && !string.Equals(text, _cardPacketShowSaveKeyFingerprint, StringComparison.Ordinal);
		bool flag2 = trackMainPreview && !string.Equals(text2, _cardPacketShowSpriteFingerprint, StringComparison.Ordinal);
		bool flag3 = forceSpriteRebuild || !GodotObject.IsInstanceValid(packetShow.sprite) || (flag2 && (!flag || _cardPreviewRefreshCommitted)) || (flag && _cardPreviewRefreshCommitted);
		bool animationChanged = !flag3 && trackMainPreview && !string.Equals(text3, _cardPacketShowAnimationFingerprint, StringComparison.Ordinal);
		if (flag3)
		{
			EnsurePacketPreviewCharacterSpriteRegistered(packet);
			text2 = BuildPacketShowSpriteFingerprint(packet);
			ulong num = (GodotObject.IsInstanceValid(packetShow.sprite) ? packetShow.sprite.GetInstanceId() : 0);
			packetShow.InitVisualPreview(packet, xWCardBehaviorPreviewSnapshot.Cost, xWCardBehaviorPreviewSnapshot.CostRise, xWCardBehaviorPreviewSnapshot.CostMultiple, xWCardBehaviorPreviewSnapshot.Cooldown);
			if (trackMainPreview && GodotObject.IsInstanceValid(packetShow.sprite) && packetShow.sprite.GetInstanceId() != num)
			{
				_cardPacketShowSpriteRebuildCount++;
			}
			if (trackMainPreview)
			{
				DisposeCardDragGhost();
			}
		}
		else
		{
			ApplyPacketShowPreviewIncremental(packetShow, packet, animationChanged);
		}
		if (trackMainPreview)
		{
			if (flag3 || !flag || _cardPreviewRefreshCommitted)
			{
				_cardPacketShowSpriteFingerprint = text2;
			}
			_cardPacketShowAnimationFingerprint = text3;
			if (flag3 || _cardPreviewRefreshCommitted)
			{
				_cardPacketShowSaveKeyFingerprint = text;
			}
		}
		packetShow.onlyDraw = true;
	}

	private void ApplyPacketShowPreviewIncremental(TowerDefenseInGamePacketShow packetShow, TowerDefensePacketConfig packet, bool animationChanged)
	{
		if (!GodotObject.IsInstanceValid(packetShow) || packet == null)
		{
			return;
		}
		XWCardBehaviorPreviewSnapshot xWCardBehaviorPreviewSnapshot = XWCardBehaviorPreviewSafety.Capture(packet);
		packetShow.config = packet;
		packetShow.riseCost = xWCardBehaviorPreviewSnapshot.CostRise;
		packetShow.costMultiple = xWCardBehaviorPreviewSnapshot.CostMultiple;
		packetShow.baseItemCost = xWCardBehaviorPreviewSnapshot.Cost;
		packetShow.itemCost = packetShow.baseItemCost;
		packetShow.coldDown = xWCardBehaviorPreviewSnapshot.Cooldown;
		packetShow.InvalidateRuntimeState();
		packetShow.UpdateBackgroundTexture();
		packetShow.UpdateSpriteLayout();
		if (GodotObject.IsInstanceValid(packetShow.coldDownProgressBar))
		{
			packetShow.coldDownProgressBar.MaxValue = Math.Max(0.001, packetShow.coldDown);
		}
		if (GodotObject.IsInstanceValid(packetShow.sprite))
		{
			if (animationChanged)
			{
				RefreshPacketShowAnimationInPlace(packetShow, packet.packetAnimeClip ?? "");
			}
			else
			{
				packetShow.sprite.Visible = true;
			}
		}
		if (GodotObject.IsInstanceValid(packetShow.previewClip))
		{
			packetShow.previewClip.Visible = GodotObject.IsInstanceValid(packetShow.sprite);
		}
		if (GodotObject.IsInstanceValid(packetShow.previewSpriteNode))
		{
			packetShow.previewSpriteNode.Visible = GodotObject.IsInstanceValid(packetShow.sprite);
		}
		packetShow.ColorSet();
	}

	private void RefreshPacketShowAnimationInPlace(TowerDefenseInGamePacketShow packetShow, string clip)
	{
		if (GodotObject.IsInstanceValid(packetShow?.sprite))
		{
			if (_cardRuntimeHovered)
			{
				packetShow.OnMouseEntered();
				return;
			}
			packetShow.sprite.SetAnimation(clip);
			packetShow.RefreshPreview();
		}
	}

	private static string BuildPacketShowSpriteFingerprint(TowerDefensePacketConfig packet)
	{
		if (packet == null || !GodotObject.IsInstanceValid(packet.characterConfig))
		{
			return "";
		}
		TowerDefenseCharacterConfig characterConfig = packet.characterConfig;
		ulong instanceId = characterConfig.GetInstanceId();
		ulong num = (GodotObject.IsInstanceValid(characterConfig.armorData) ? characterConfig.armorData.GetInstanceId() : 0);
		ulong num2 = (GodotObject.IsInstanceValid(characterConfig.customData) ? characterConfig.customData.GetInstanceId() : 0);
		PackedScene packetSpriteScene = TowerDefenseManager.GetPacketSpriteScene(packet);
		ulong num3 = (GodotObject.IsInstanceValid(packetSpriteScene) ? packetSpriteScene.GetInstanceId() : 0);
		_003C_003Ey__InlineArray9<object> buffer = default(_003C_003Ey__InlineArray9<object>);
		buffer[0] = instanceId;
		buffer[1] = characterConfig.ResourcePath ?? "";
		buffer[2] = characterConfig.name ?? "";
		buffer[3] = num;
		buffer[4] = num2;
		buffer[5] = num3;
		buffer[6] = packetSpriteScene?.ResourcePath ?? "";
		buffer[7] = XWCardBehaviorPreviewSafety.ResolveBaseOverrideHypnoses(packet);
		buffer[8] = BuildCardArmorFingerprint(packet.initArmor);
		return string.Join('\u001e', (ReadOnlySpan<object?>)buffer);
	}

	private static string BuildPacketShowSaveKeyFingerprint(TowerDefensePacketConfig packet)
	{
		return packet?.saveKey ?? "";
	}

	private static double ResolveConfiguredPacketCooldown(TowerDefensePacketConfig packet)
	{
		XWCardPreviewValueSource source;
		double val = XWCardBehaviorPreviewSafety.ResolveBaseOverrideCooldown(packet, out source);
		return Math.Max(0.0, val);
	}

	private void ApplyPacketShowPreviewLayout(TowerDefenseInGamePacketShow packetShow)
	{
		if (GodotObject.IsInstanceValid(packetShow))
		{
			packetShow.setPcLayout = !_cardRuntimeMobile;
			packetShow.setMobileLayout = _cardRuntimeMobile;
			packetShow.isMobile = _cardRuntimeMobile;
			if (_cardRuntimeMobile)
			{
				packetShow.MobilePreset();
			}
			else
			{
				packetShow.SetPcPreset();
			}
		}
	}

	private static bool EnsurePacketPreviewCharacterSpriteRegistered(TowerDefensePacketConfig packet)
	{
		TowerDefenseCharacterConfig towerDefenseCharacterConfig = packet?.characterConfig;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacterConfig) || string.IsNullOrWhiteSpace(towerDefenseCharacterConfig.name))
		{
			return false;
		}
		if (ResourceManager.Instance == null)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketSpriteScene(packet)))
		{
			return true;
		}
		if (!TryResolveCardSpriteScene(packet, out var spriteScene))
		{
			return false;
		}
		XWModRuntimeRegistry.Register("ModEditorPreview", "CharacterSprite", towerDefenseCharacterConfig.name, Variant.From(in spriteScene), allowOverride: true);
		if (!string.IsNullOrWhiteSpace(packet.saveKey) && !string.Equals(packet.saveKey, towerDefenseCharacterConfig.name, StringComparison.OrdinalIgnoreCase))
		{
			XWModRuntimeRegistry.Register("ModEditorPreview", "CharacterSprite", packet.saveKey, Variant.From(in spriteScene), allowOverride: true);
		}
		return GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketSpriteScene(packet));
	}

	private static bool TryResolveCardSpriteScene(TowerDefensePacketConfig packet, out PackedScene spriteScene)
	{
		spriteScene = TowerDefenseManager.GetPacketSpriteScene(packet);
		if (GodotObject.IsInstanceValid(spriteScene))
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(packet?.characterConfig))
		{
			return false;
		}
		List<string> list = new List<string>();
		CollectCardSpriteSceneCandidates(packet, list);
		foreach (string item in list)
		{
			if (ResourceLoader.Exists(item))
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>(item, null, ResourceLoader.CacheMode.Reuse);
				if (IsAdobeAnimateSpriteScene(packedScene))
				{
					spriteScene = packedScene;
					return true;
				}
			}
		}
		return false;
	}

	private static void CollectCardSpriteSceneCandidates(TowerDefensePacketConfig packet, List<string> candidates)
	{
		if (packet != null && candidates != null)
		{
			string characterName = packet.characterConfig?.name ?? "";
			CollectCardSpriteSceneCandidates(packet.characterConfig?.ResourcePath, characterName, candidates);
			CollectCardSpriteSceneCandidates(packet.ResourcePath, characterName, candidates);
		}
	}

	private static void CollectCardSpriteSceneCandidates(string resourcePath, string characterName, List<string> candidates)
	{
		string text = NormalizeResourcePath(resourcePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string baseDir = text.GetBaseDir();
		string file = baseDir.GetFile();
		string text2 = ((file.Equals("Config", StringComparison.OrdinalIgnoreCase) || file.Equals("Packet", StringComparison.OrdinalIgnoreCase)) ? baseDir.GetBaseDir() : baseDir);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			if (!string.IsNullOrWhiteSpace(characterName))
			{
				AddCardSpriteSceneCandidate(candidates, text2.PathJoin("Sprite").PathJoin(characterName + ".tscn"));
				AddCardSpriteSceneCandidate(candidates, text2.PathJoin(characterName + ".tscn"));
			}
			AddCardSpriteSceneCandidate(candidates, text2.PathJoin(text2.GetFile() + ".tscn"));
		}
	}

	private static void AddCardSpriteSceneCandidate(List<string> candidates, string candidate)
	{
		candidate = NormalizeResourcePath(candidate);
		if (!string.IsNullOrWhiteSpace(candidate) && !candidates.Contains(candidate))
		{
			candidates.Add(candidate);
		}
	}

	private static bool IsAdobeAnimateSpriteScene(PackedScene scene)
	{
		if (!GodotObject.IsInstanceValid(scene))
		{
			return false;
		}
		Node node = null;
		try
		{
			node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			return node is AdobeAnimateSprite;
		}
		catch
		{
			return false;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
	}

	private void PositionPacketShowPreview()
	{
		if (GodotObject.IsInstanceValid(_cardPacketShowPreviewRoot) && GodotObject.IsInstanceValid(_cardPacketShowPreview))
		{
			Vector2 vector = _cardPacketShowPreviewRoot.Size;
			if (vector.X <= 1f || vector.Y <= 1f)
			{
				vector = _cardPacketShowPreviewRoot.CustomMinimumSize;
			}
			_cardPacketShowPreview.Position = new Vector2(vector.X * 0.5f, vector.Y * 0.5f);
			_cardPacketShowPreview.Scale = Vector2.One * (_cardRuntimeMobile ? 1.25f : 1.35f);
		}
	}

	private void OnCardInteractionGuiInput(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_cardInteractionButton) || !IsVisibleInTree())
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			Vector2 globalPosition = inputEventMouseButton.GlobalPosition;
			if (inputEventMouseButton.Pressed)
			{
				BeginCardPointerSession(globalPosition, touch: false, -1);
			}
			else
			{
				CompleteCardPointerSession(globalPosition, touch: false, -1);
			}
			_cardInteractionButton.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && !_cardDragTouch)
		{
			HandleCardPointerMotion(inputEventMouseMotion.GlobalPosition, touch: false, -1);
			if (_cardDragCandidate || _cardDragActive)
			{
				_cardInteractionButton.AcceptEvent();
			}
		}
		else if (inputEvent is InputEventScreenTouch { Position: var position } inputEventScreenTouch)
		{
			if (inputEventScreenTouch.Pressed)
			{
				BeginCardPointerSession(position, touch: true, inputEventScreenTouch.Index);
			}
			else
			{
				CompleteCardPointerSession(position, touch: true, inputEventScreenTouch.Index);
			}
			_cardInteractionButton.AcceptEvent();
		}
		else if (inputEvent is InputEventScreenDrag inputEventScreenDrag)
		{
			HandleCardPointerMotion(inputEventScreenDrag.Position, touch: true, inputEventScreenDrag.Index);
			if (_cardDragCandidate || _cardDragActive)
			{
				_cardInteractionButton.AcceptEvent();
			}
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!_cardDragCandidate && !_cardDragActive)
		{
			if (IsVisibleInTree() && GodotObject.IsInstanceValid(_cardInteractionButton))
			{
				if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed && IsCanvasPointInsideControl(_cardInteractionButton, inputEventMouseButton.GlobalPosition))
				{
					BeginCardPointerSession(inputEventMouseButton.GlobalPosition, touch: false, -1);
				}
				else if (inputEvent is InputEventScreenTouch { Pressed: not false } inputEventScreenTouch && IsCanvasPointInsideControl(_cardInteractionButton, inputEventScreenTouch.Position))
				{
					BeginCardPointerSession(inputEventScreenTouch.Position, touch: true, inputEventScreenTouch.Index);
				}
			}
			return;
		}
		if (!IsVisibleInTree())
		{
			CancelCardDragSession(countCancellation: true, "页面已隐藏，拖拽预览已取消");
			return;
		}
		bool flag = false;
		if (inputEvent is InputEventMouseMotion inputEventMouseMotion && !_cardDragTouch)
		{
			HandleCardPointerMotion(inputEventMouseMotion.GlobalPosition, touch: false, -1);
			flag = true;
		}
		else if (inputEvent is InputEventMouseButton inputEventMouseButton2 && inputEventMouseButton2.ButtonIndex == MouseButton.Left && !inputEventMouseButton2.Pressed && !_cardDragTouch)
		{
			flag = _cardDragActive;
			CompleteCardPointerSession(inputEventMouseButton2.GlobalPosition, touch: false, -1);
		}
		else if (inputEvent is InputEventScreenDrag inputEventScreenDrag && _cardDragTouch && inputEventScreenDrag.Index == _cardDragTouchIndex)
		{
			HandleCardPointerMotion(inputEventScreenDrag.Position, touch: true, inputEventScreenDrag.Index);
			flag = true;
		}
		else if (inputEvent is InputEventScreenTouch { Pressed: false } inputEventScreenTouch2 && _cardDragTouch && inputEventScreenTouch2.Index == _cardDragTouchIndex)
		{
			flag = _cardDragActive;
			CompleteCardPointerSession(inputEventScreenTouch2.Position, touch: true, inputEventScreenTouch2.Index);
		}
		else if (inputEvent is InputEventKey { Pressed: not false } inputEventKey && inputEventKey.Keycode == Key.Escape)
		{
			CancelCardDragSession(countCancellation: true, "已取消卡牌拖拽");
			flag = true;
		}
		if (flag)
		{
			GetViewport()?.SetInputAsHandled();
		}
	}

	private static Vector2 CanvasPointToControlLocal(Control control, Vector2 canvasPosition)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return canvasPosition;
		}
		return control.GetGlobalTransformWithCanvas().AffineInverse() * canvasPosition;
	}

	private static bool IsCanvasPointInsideControl(Control control, Vector2 canvasPosition)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return false;
		}
		Vector2 point = CanvasPointToControlLocal(control, canvasPosition);
		return new Rect2(Vector2.Zero, control.Size).HasPoint(point);
	}

	private void BeginCardPointerSession(Vector2 globalPosition, bool touch, int touchIndex)
	{
		if (!_cardDragCandidate && !_cardDragActive)
		{
			if (TryGetCardPlacementBlockReason(out var reason))
			{
				_cardDragRejectedCount++;
				ShowCardPlacementMessage("无法拿起卡牌 · " + reason, valid: false);
				return;
			}
			_cardDragCandidate = true;
			_cardDragTouch = touch;
			_cardDragTouchIndex = (touch ? touchIndex : (-1));
			_cardDragStartGlobalPosition = globalPosition;
			_cardDragPointerGlobalPosition = globalPosition;
			SetProcessInput(enable: true);
		}
	}

	private void HandleCardPointerMotion(Vector2 globalPosition, bool touch, int touchIndex)
	{
		if ((_cardDragCandidate || _cardDragActive) && touch == _cardDragTouch && (!touch || touchIndex == _cardDragTouchIndex))
		{
			_cardDragPointerGlobalPosition = globalPosition;
			if (!_cardDragActive && globalPosition.DistanceSquaredTo(_cardDragStartGlobalPosition) >= 100f)
			{
				StartCardDragSession();
			}
			if (_cardDragActive)
			{
				UpdateCardDragTarget(globalPosition);
			}
		}
	}

	private void StartCardDragSession()
	{
		if (!_cardDragCandidate || _cardDragActive)
		{
			return;
		}
		if (TryGetCardPlacementBlockReason(out var reason))
		{
			_cardDragRejectedCount++;
			CancelCardDragSession(countCancellation: false, "无法拖拽 · " + reason);
			return;
		}
		_cardDragActive = true;
		EnsureCardDragGhost();
		if (GodotObject.IsInstanceValid(_cardDragPacketGhost))
		{
			_cardDragPacketGhost.Show();
		}
		_cardRuntimePreviewState = 1;
		ApplyCardRuntimePreviewState();
		UpdateCardDragTarget(_cardDragPointerGlobalPosition);
	}

	private void CompleteCardPointerSession(Vector2 globalPosition, bool touch, int touchIndex)
	{
		if ((!_cardDragCandidate && !_cardDragActive) || touch != _cardDragTouch || (touch && touchIndex != _cardDragTouchIndex))
		{
			return;
		}
		if (!_cardDragActive)
		{
			ClearCardPointerSession();
			return;
		}
		UpdateCardDragTarget(globalPosition);
		if (_cardDragTargetValid && IsCardBattleCell(_cardDragPreviewCell))
		{
			PlaceCardPreviewAtCell(_cardDragPreviewCell);
			return;
		}
		bool flag = IsCanvasPointInsideControl(_cardBattleDropSurface, globalPosition);
		_cardDragRejectedCount++;
		CancelCardDragSession(!flag, flag ? "落点无效 · 未扣除阳光，也未进入冷却" : "已在草坪外取消 · 未扣除阳光，也未进入冷却");
	}

	private void ClearCardPointerSession()
	{
		_cardDragCandidate = false;
		_cardDragTouch = false;
		_cardDragTouchIndex = -1;
		SetProcessInput(IsVisibleInTree());
	}

	private void EnsureCardDragGhost()
	{
		if (GodotObject.IsInstanceValid(_cardDragPacketGhost) || !GodotObject.IsInstanceValid(_cardBattleDropSurface) || !GodotObject.IsInstanceValid(_editingPacket) || !GodotObject.IsInstanceValid(_editingPacket.characterConfig))
		{
			return;
		}
		try
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.Name = "CardDragPacketGhost";
				towerDefenseInGamePacketShow.ZIndex = 220;
				towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
				towerDefenseInGamePacketShow.Modulate = new Color(1f, 1f, 1f, 0.86f);
				towerDefenseInGamePacketShow.onlyDraw = true;
				_cardBattleDropSurface.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
				ConfigurePacketShowPreview(towerDefenseInGamePacketShow, _editingPacket, forceSpriteRebuild: true);
				towerDefenseInGamePacketShow.ProcessMode = ProcessModeEnum.Disabled;
				towerDefenseInGamePacketShow.Scale = Vector2.One * (_cardRuntimeMobile ? 1.05f : 1.15f);
				towerDefenseInGamePacketShow.Hide();
				_cardDragPacketGhost = towerDefenseInGamePacketShow;
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Card drag ghost preview failed: " + ex.Message);
			DisposeCardDragGhost();
		}
	}

	private void DisposeCardDragGhost()
	{
		if (GodotObject.IsInstanceValid(_cardDragPacketGhost) && !_cardDragPacketGhost.IsQueuedForDeletion())
		{
			_cardDragPacketGhost.QueueFree();
		}
		_cardDragPacketGhost = null;
	}

	private void UpdateCardDragTarget(Vector2 globalPosition)
	{
		if (_cardDragActive && GodotObject.IsInstanceValid(_cardBattleDropSurface))
		{
			Vector2 vector = CanvasPointToControlLocal(_cardBattleDropSurface, globalPosition);
			Rect2 rect = new Rect2(Vector2.Zero, _cardBattleDropSurface.Size);
			if (GodotObject.IsInstanceValid(_cardDragPacketGhost))
			{
				_cardDragPacketGhost.Position = vector + new Vector2(18f, 8f);
			}
			bool flag = rect.HasPoint(vector);
			Vector2I cell;
			bool flag2 = TryGetCardBattleCell(vector, out cell);
			_cardDragPreviewCell = (flag2 ? cell : new Vector2I(-1, -1));
			_cardDragTargetValid = flag2 && !TryGetCardPlacementBlockReason(out var _) && (!_cardHasPlacedPreview || cell != _cardPlacedPreviewCell);
			if (flag2)
			{
				Rect2 rect2 = GetCardBattleCellRect(_cardBattleDropSurface.Size, cell).Grow(-2f);
				ShowCardDragTarget(rect2, _cardDragTargetValid);
				PreviewCharacterAtCardCell(cell, _cardDragTargetValid);
			}
			else if (flag)
			{
				Vector2 vector2 = GetCardBattleCellSize(_cardBattleDropSurface.Size) * 0.72f;
				Vector2 position = vector - vector2 * 0.5f;
				position.X = Mathf.Clamp(position.X, 0f, Math.Max(0f, _cardBattleDropSurface.Size.X - vector2.X));
				position.Y = Mathf.Clamp(position.Y, 0f, Math.Max(0f, _cardBattleDropSurface.Size.Y - vector2.Y));
				ShowCardDragTarget(new Rect2(position, vector2), valid: false);
				RestoreCardCharacterPreviewPosition();
			}
			else
			{
				HideCardDragTarget();
				RestoreCardCharacterPreviewPosition();
			}
			if (GodotObject.IsInstanceValid(_cardBattleTint))
			{
				_cardBattleTint.Color = (_cardDragTargetValid ? new Color(0.04f, 0.22f, 0.04f, 0.16f) : new Color(0.28f, 0.035f, 0.025f, flag ? 0.23f : 0.12f));
			}
			ShowCardPlacementMessage(_cardDragTargetValid ? $"可种植 · 第 {_cardDragPreviewCell.X} 列 / 第 {_cardDragPreviewCell.Y} 行" : (flag ? "红色区域不可种植 · 请移到草坪格子" : "拖到草坪内的绿色格子后松开"), _cardDragTargetValid);
		}
	}

	private void ShowCardDragTarget(Rect2 rect, bool valid)
	{
		if (GodotObject.IsInstanceValid(_cardDragTargetHighlight))
		{
			_cardDragTargetHighlight.Position = rect.Position;
			_cardDragTargetHighlight.Size = rect.Size;
			_cardDragTargetHighlight.Color = (valid ? CardDragValidColor : CardDragInvalidColor);
			_cardDragTargetHighlight.Show();
		}
	}

	private void HideCardDragTarget()
	{
		if (GodotObject.IsInstanceValid(_cardDragTargetHighlight))
		{
			_cardDragTargetHighlight.Hide();
		}
	}

	private bool TryGetCardPlacementBlockReason(out string reason)
	{
		reason = "";
		if (!GodotObject.IsInstanceValid(_editingPacket) || !GodotObject.IsInstanceValid(_editingPacket.characterConfig))
		{
			reason = "尚未绑定可预览角色";
			return true;
		}
		int cardRuntimePreviewState = _cardRuntimePreviewState;
		if ((uint)(cardRuntimePreviewState - 2) <= 2u)
		{
			reason = _cardRuntimePreviewState switch
			{
				2 => "卡牌仍在冷却", 
				3 => "阳光不足", 
				_ => "卡牌尚未解锁", 
			};
			return true;
		}
		if (_cardRuntimeCooldownRemaining > 0.001)
		{
			reason = "卡牌仍在冷却";
			return true;
		}
		long effectiveCost = GetEffectiveCost(_editingPacket);
		if (effectiveCost < 0)
		{
			reason = "卡牌费用不可用";
			return true;
		}
		double num = _previewSunSpinBox?.Value ?? 0.0;
		if (num < (double)effectiveCost)
		{
			reason = $"阳光不足（{Mathf.RoundToInt((float)num)} / {effectiveCost}）";
			return true;
		}
		return false;
	}

	private void PlaceCardPreviewAtCell(Vector2I cell)
	{
		string reason = (IsCardBattleCell(cell) ? "" : "目标不在草坪格子内");
		if (!IsCardBattleCell(cell) || TryGetCardPlacementBlockReason(out reason))
		{
			_cardDragRejectedCount++;
			bool flag = _cardDragCandidate || _cardDragActive;
			CancelCardDragSession(flag, "放置失败 · " + reason);
			if (!flag)
			{
				ShowCardPlacementMessage("放置失败 · " + reason, valid: false);
			}
			return;
		}
		_cardHasPlacedPreview = true;
		_cardPlacedPreviewCell = cell;
		_cardDragPreviewPlacementCount++;
		RebuildCardCharacterPreview(force: true);
		RestoreCardCharacterPreviewPosition();
		long num = Math.Max(0L, GetEffectiveCost(_editingPacket));
		if (GodotObject.IsInstanceValid(_previewSunSpinBox) && num > 0)
		{
			bool updatingControls = _updatingControls;
			_updatingControls = true;
			try
			{
				_previewSunSpinBox.SetValueNoSignal(Math.Max(0.0, _previewSunSpinBox.Value - (double)num));
			}
			finally
			{
				_updatingControls = updatingControls;
			}
		}
		FinishCardDragVisuals();
		StartConfiguredCardCooldown();
		ShowCardPlacementMessage($"种植完成 · 第 {cell.X} 列 / 第 {cell.Y} 行 · 消耗 {num} 阳光", valid: true);
		QueueCardBattleDropSurfaceRedraw();
	}

	private void CancelCardDragSession(bool countCancellation, string message)
	{
		bool flag = _cardDragCandidate || _cardDragActive;
		if (countCancellation && flag)
		{
			_cardDragCancelCount++;
		}
		FinishCardDragVisuals();
		if (_cardRuntimePreviewState == 1)
		{
			_cardRuntimePreviewState = ResolveCardAvailabilityState();
			ApplyCardRuntimePreviewState();
		}
		if (!string.IsNullOrWhiteSpace(message) && flag)
		{
			ShowCardPlacementMessage(message, valid: false);
		}
	}

	private void FinishCardDragVisuals()
	{
		_cardDragActive = false;
		_cardDragTargetValid = false;
		_cardDragPreviewCell = new Vector2I(-1, -1);
		ClearCardPointerSession();
		if (GodotObject.IsInstanceValid(_cardDragPacketGhost))
		{
			_cardDragPacketGhost.Hide();
		}
		HideCardDragTarget();
		if (GodotObject.IsInstanceValid(_cardBattleTint))
		{
			_cardBattleTint.Color = new Color(0.04f, 0.12f, 0.035f, 0.12f);
		}
		RestoreCardCharacterPreviewPosition();
		QueueCardBattleDropSurfaceRedraw();
	}

	private void ResetCardDragPreviewState()
	{
		_cardHasPlacedPreview = false;
		_cardPlacedPreviewCell = new Vector2I(-1, -1);
		FinishCardDragVisuals();
		_cardDragPreviewPlacementCount = 0;
		_cardDragRejectedCount = 0;
		_cardDragCancelCount = 0;
	}

	private void ShowCardPlacementMessage(string message, bool valid)
	{
		if (GodotObject.IsInstanceValid(_battleStatusLabel))
		{
			_battleStatusLabel.Text = message;
			_battleStatusLabel.AddThemeColorOverride("font_color", valid ? new Color(0.72f, 1f, 0.58f) : new Color(1f, 0.64f, 0.48f));
		}
	}

	private void OnCardBattleDropSurfaceResized()
	{
		QueueCardBattleDropSurfaceRedraw();
		if (_cardDragActive)
		{
			UpdateCardDragTarget(_cardDragPointerGlobalPosition);
		}
	}

	private void DrawCardBattleDropSurface()
	{
		if (!GodotObject.IsInstanceValid(_cardBattleDropSurface))
		{
			return;
		}
		Rect2 cardBattleGridRect = GetCardBattleGridRect(_cardBattleDropSurface.Size);
		Vector2 cardBattleCellSize = GetCardBattleCellSize(_cardBattleDropSurface.Size);
		_cardBattleDropSurface.DrawRect(cardBattleGridRect, new Color(0.12f, 0.36f, 0.08f, 0.1f));
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				Rect2 rect = new Rect2(cardBattleGridRect.Position + new Vector2((float)j * cardBattleCellSize.X, (float)i * cardBattleCellSize.Y), cardBattleCellSize);
				Color color = (((j + i) % 2 == 0) ? new Color(0.5f, 0.84f, 0.28f, 0.055f) : new Color(0.2f, 0.58f, 0.14f, 0.035f));
				_cardBattleDropSurface.DrawRect(rect, color);
				_cardBattleDropSurface.DrawRect(rect, new Color(0.82f, 1f, 0.62f, 0.22f), filled: false, 1f);
			}
		}
		_cardBattleDropSurface.DrawRect(cardBattleGridRect, new Color(0.8f, 1f, 0.54f, 0.5f), filled: false, 2f);
		if (_cardHasPlacedPreview && IsCardBattleCell(_cardPlacedPreviewCell))
		{
			Rect2 rect2 = GetCardBattleCellRect(_cardBattleDropSurface.Size, _cardPlacedPreviewCell).Grow(-3f);
			_cardBattleDropSurface.DrawRect(rect2, new Color(1f, 0.72f, 0.22f, 0.18f));
			_cardBattleDropSurface.DrawRect(rect2, new Color(1f, 0.82f, 0.32f, 0.78f), filled: false, 2f);
		}
	}

	private void QueueCardBattleDropSurfaceRedraw()
	{
		if (GodotObject.IsInstanceValid(_cardBattleDropSurface))
		{
			_cardBattleDropSurface.QueueRedraw();
		}
	}

	private bool TryGetCardBattleCell(Vector2 localPosition, out Vector2I cell)
	{
		cell = new Vector2I(-1, -1);
		if (!GodotObject.IsInstanceValid(_cardBattleDropSurface))
		{
			return false;
		}
		Rect2 cardBattleGridRect = GetCardBattleGridRect(_cardBattleDropSurface.Size);
		if (!cardBattleGridRect.HasPoint(localPosition))
		{
			return false;
		}
		Vector2 cardBattleCellSize = GetCardBattleCellSize(_cardBattleDropSurface.Size);
		if (cardBattleCellSize.X <= 0f || cardBattleCellSize.Y <= 0f)
		{
			return false;
		}
		Vector2 vector = localPosition - cardBattleGridRect.Position;
		cell = new Vector2I(Math.Clamp(Mathf.FloorToInt(vector.X / cardBattleCellSize.X) + 1, 1, 9), Math.Clamp(Mathf.FloorToInt(vector.Y / cardBattleCellSize.Y) + 1, 1, 5));
		return true;
	}

	private static Rect2 GetCardBattleGridRect(Vector2 size)
	{
		return new Rect2(size * new Vector2(0.16f, 0.19f), size * new Vector2(0.72f, 0.65f));
	}

	private static Vector2 GetCardBattleCellSize(Vector2 size)
	{
		Rect2 cardBattleGridRect = GetCardBattleGridRect(size);
		return new Vector2(cardBattleGridRect.Size.X / 9f, cardBattleGridRect.Size.Y / 5f);
	}

	private static Rect2 GetCardBattleCellRect(Vector2 size, Vector2I cell)
	{
		Rect2 cardBattleGridRect = GetCardBattleGridRect(size);
		Vector2 cardBattleCellSize = GetCardBattleCellSize(size);
		return new Rect2(cardBattleGridRect.Position + new Vector2((float)(cell.X - 1) * cardBattleCellSize.X, (float)(cell.Y - 1) * cardBattleCellSize.Y), cardBattleCellSize);
	}

	private static Vector2 GetCardBattleCellCenter(Vector2 size, Vector2I cell)
	{
		Rect2 cardBattleCellRect = GetCardBattleCellRect(size, cell);
		return cardBattleCellRect.Position + new Vector2(cardBattleCellRect.Size.X * 0.5f, cardBattleCellRect.Size.Y * 0.72f);
	}

	private static bool IsCardBattleCell(Vector2I cell)
	{
		if (cell.X >= 1 && cell.X <= 9 && cell.Y >= 1)
		{
			return cell.Y <= 5;
		}
		return false;
	}

	private void PreviewCharacterAtCardCell(Vector2I cell, bool valid)
	{
		if (GodotObject.IsInstanceValid(_cardCharacterPreviewRoot) && GodotObject.IsInstanceValid(_cardCharacterViewport) && IsCardBattleCell(cell))
		{
			Vector2 size = new Vector2(_cardCharacterViewport.Size.X, _cardCharacterViewport.Size.Y);
			_cardCharacterPreviewRoot.Position = GetCardBattleCellCenter(size, cell);
			_cardCharacterPreviewRoot.Modulate = (valid ? new Color(0.74f, 1f, 0.72f, 0.86f) : new Color(1f, 0.42f, 0.38f, 0.68f));
		}
	}

	private void RestoreCardCharacterPreviewPosition()
	{
		if (GodotObject.IsInstanceValid(_cardCharacterPreviewRoot))
		{
			Vector2 size = (GodotObject.IsInstanceValid(_cardCharacterViewport) ? new Vector2(_cardCharacterViewport.Size.X, _cardCharacterViewport.Size.Y) : new Vector2(720f, 365f));
			if (_cardHasPlacedPreview && IsCardBattleCell(_cardPlacedPreviewCell))
			{
				_cardCharacterPreviewRoot.Position = GetCardBattleCellCenter(size, _cardPlacedPreviewCell);
			}
			else
			{
				_cardCharacterPreviewRoot.Position = new Vector2(size.X * 0.5f, size.Y * 0.6f);
			}
			_cardCharacterPreviewRoot.Modulate = Colors.White;
		}
	}

	private void UpdateCardBattleViewportLayout()
	{
		if (GodotObject.IsInstanceValid(_cardCharacterViewport))
		{
			Vector2 size = new Vector2(_cardCharacterViewport.Size.X, _cardCharacterViewport.Size.Y);
			if (size.X <= 1f || size.Y <= 1f)
			{
				size = new Vector2(720f, 365f);
			}
			if (GodotObject.IsInstanceValid(_cardGameBackground))
			{
				_cardGameBackground.Size = size;
			}
			if (GodotObject.IsInstanceValid(_cardBattleTint))
			{
				_cardBattleTint.Size = size;
			}
			RestoreCardCharacterPreviewPosition();
			QueueCardBattleDropSurfaceRedraw();
		}
	}

	private void PreviewCardPlantAndCooldown()
	{
		Vector2I cell = ((_cardHasPlacedPreview && IsCardBattleCell(_cardPlacedPreviewCell)) ? _cardPlacedPreviewCell : new Vector2I(5, 3));
		PlaceCardPreviewAtCell(cell);
	}

	private void ResetCardRuntimePreview()
	{
		CancelCardDragSession(countCancellation: false, "");
		StopCardRuntimeClock();
		_cardRuntimePreviewState = 0;
		_cardRuntimeCooldownProgress = 0f;
		_cardRuntimeCooldownDuration = 0.0;
		_cardRuntimeCooldownRemaining = 0.0;
		if (GodotObject.IsInstanceValid(_previewSunSpinBox))
		{
			_previewSunSpinBox.Value = Math.Max(500L, GetEffectiveCost(_editingPacket));
		}
		_cardHasPlacedPreview = false;
		_cardPlacedPreviewCell = new Vector2I(-1, -1);
		RebuildCardCharacterPreview(force: true);
		RestoreCardCharacterPreviewPosition();
		QueueCardBattleDropSurfaceRedraw();
		ApplyCardRuntimePreviewState();
	}

	private void StartConfiguredCardCooldown()
	{
		EnsureCardRuntimeCooldownDuration(forceRefresh: true);
		_cardRuntimeCooldownRemaining = _cardRuntimeCooldownDuration;
		_cardRuntimeCooldownProgress = ((_cardRuntimeCooldownDuration > 0.0) ? 1f : 0f);
		_cardRuntimePreviewState = ((_cardRuntimeCooldownDuration > 0.0) ? 2 : ResolveCardAvailabilityState());
		_cardRuntimeCooldownRunning = _cardRuntimeCooldownDuration > 0.0;
		RequestVisibilityGatedProcessing(_cardRuntimeCooldownRunning);
		ApplyCardRuntimePreviewState();
	}

	private void ToggleCardRuntimeClock()
	{
		if (_cardRuntimeCooldownRemaining <= 0.0)
		{
			StartConfiguredCardCooldown();
			return;
		}
		_cardRuntimeCooldownRunning = !_cardRuntimeCooldownRunning;
		RequestVisibilityGatedProcessing(_cardRuntimeCooldownRunning);
		ApplyCardRuntimePreviewState();
	}

	private void StopCardRuntimeClock()
	{
		_cardRuntimeCooldownRunning = false;
		RequestVisibilityGatedProcessing(requested: false);
	}

	private void EnsureCardRuntimeCooldownDuration(bool forceRefresh = false)
	{
		if (forceRefresh || !(_cardRuntimeCooldownDuration > 0.0))
		{
			double num = (GodotObject.IsInstanceValid(_cardPacketShowPreview) ? _cardPacketShowPreview.coldDown : 0.0);
			if (num <= 0.0 && double.TryParse(GetEffectiveCooldownText(_editingPacket), out var result))
			{
				num = result;
			}
			_cardRuntimeCooldownDuration = Math.Max(0.0, num);
		}
	}

	private int ResolveCardAvailabilityState()
	{
		long effectiveCost = GetEffectiveCost(_editingPacket);
		double num = _previewSunSpinBox?.Value ?? 0.0;
		TowerDefensePacketConfig editingPacket = _editingPacket;
		if (editingPacket != null && editingPacket.disableWhenSunNegative && num < 0.0)
		{
			return 3;
		}
		if (effectiveCost < 0 || !(num < (double)effectiveCost))
		{
			return 0;
		}
		return 3;
	}

	private void ToggleCardRuntimeSelection()
	{
		int cardRuntimePreviewState = _cardRuntimePreviewState;
		if ((uint)(cardRuntimePreviewState - 2) <= 2u)
		{
			ApplyCardRuntimePreviewState();
			return;
		}
		_cardRuntimePreviewState = ((_cardRuntimePreviewState != 1) ? 1 : 0);
		ApplyCardRuntimePreviewState();
	}

	private void OnCardRuntimeMouseEntered()
	{
		_cardRuntimeHovered = true;
		_cardPacketShowPreview?.OnMouseEntered();
		ApplyCardRuntimePreviewState();
	}

	private void OnCardRuntimeMouseExited()
	{
		_cardRuntimeHovered = false;
		_cardPacketShowPreview?.OnMouseExited();
		ApplyCardRuntimePreviewState();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		_behaviorRegistryPollAccumulator += Math.Max(0.0, delta);
		if (_behaviorRegistryPollAccumulator >= 0.25)
		{
			_behaviorRegistryPollAccumulator %= 0.25;
			ulong revision = TowerDefenseBehaviorRegistry.Revision;
			if (revision != _behaviorRegistryRevision)
			{
				_behaviorRegistryRevision = revision;
				BuildRegistryBehaviorPicker();
				RefreshCardBehaviorLists(_editingPacket);
			}
		}
		if (!_cardRuntimeCooldownRunning || _cardRuntimeCooldownRemaining <= 0.0)
		{
			return;
		}
		_cardRuntimeCooldownRemaining = Math.Max(0.0, _cardRuntimeCooldownRemaining - delta);
		_cardRuntimeCooldownProgress = ((_cardRuntimeCooldownDuration > 0.0) ? ((float)(_cardRuntimeCooldownRemaining / _cardRuntimeCooldownDuration)) : 0f);
		UpdateCardRuntimeCooldownVisual();
		if (_cardRuntimeCooldownRemaining <= 0.0)
		{
			StopCardRuntimeClock();
			_cardRuntimePreviewState = ResolveCardAvailabilityState();
			ApplyCardRuntimePreviewState();
			return;
		}
		_cardRuntimeReadoutAccumulator += Math.Max(0.0, delta);
		if (_cardRuntimeReadoutAccumulator >= 0.1)
		{
			_cardRuntimeReadoutAccumulator %= 0.1;
			RefreshCardRuntimeReadouts();
		}
	}

	private void ApplyCardRuntimePreviewState()
	{
		RefreshCardRuntimeReadouts();
		UpdateCardRuntimeCooldownVisual();
		if (GodotObject.IsInstanceValid(_cardPacketShowPreview))
		{
			_cardPacketShowPreview.select = _cardRuntimePreviewState == 1;
			_cardPacketShowPreview.@lock = _cardRuntimePreviewState == 4;
			_cardPacketShowPreview.openShadow = _cardRuntimePreviewState == 3;
			_cardPacketShowPreview.coldDownOpen = _cardRuntimePreviewState == 2;
			TowerDefenseInGamePacketShow cardPacketShowPreview = _cardPacketShowPreview;
			int cardRuntimePreviewState = _cardRuntimePreviewState;
			bool alive = (uint)cardRuntimePreviewState <= 1u;
			cardPacketShowPreview.alive = alive;
			_cardPacketShowPreview.ColorSet();
		}
	}

	private void RefreshCardRuntimeReadouts()
	{
		_cardRuntimeReadoutRefreshCount++;
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		try
		{
			SelectVisualCard(_runtimeStateCards, _cardRuntimePreviewState.ToString());
			if (GodotObject.IsInstanceValid(_cooldownProgressSlider))
			{
				_cooldownProgressSlider.Value = _cardRuntimeCooldownProgress;
			}
			if (GodotObject.IsInstanceValid(_cooldownPercentLabel))
			{
				_cooldownPercentLabel.Text = $"{Mathf.RoundToInt(_cardRuntimeCooldownProgress * 100f)}%";
			}
			if (GodotObject.IsInstanceValid(_runtimeClockLabel))
			{
				_runtimeClockLabel.Text = ((_cardRuntimeCooldownDuration <= 0.0) ? "未配置冷却" : $"{_cardRuntimeCooldownRemaining:0.00} / {_cardRuntimeCooldownDuration:0.00} 秒");
			}
			if (GodotObject.IsInstanceValid(_pauseRuntimeButton))
			{
				_pauseRuntimeButton.Text = (_cardRuntimeCooldownRunning ? "暂停冷却" : "继续冷却");
			}
		}
		finally
		{
			_updatingControls = updatingControls;
		}
		if (GodotObject.IsInstanceValid(_battleStatusLabel))
		{
			_battleStatusLabel.AddThemeColorOverride("font_color", new Color(0.94f, 1f, 0.78f));
			Label battleStatusLabel = _battleStatusLabel;
			battleStatusLabel.Text = _cardRuntimePreviewState switch
			{
				1 => "卡片已拿起 · 再次点击可放回", 
				2 => $"种植完成 · {(_cardRuntimeCooldownRunning ? "实时冷却" : "冷却已暂停")} {_cardRuntimeCooldownRemaining:0.00} 秒", 
				3 => $"阳光不足 · 当前 {Mathf.RoundToInt((float)(_previewSunSpinBox?.Value ?? 0.0))} / 需要 {GetEffectiveCost(_editingPacket)}", 
				4 => "卡片尚未解锁", 
				_ => _cardRuntimeHovered ? "悬停预览 · 正在播放卡片动画" : "准备种植 · 点击卡片拿起", 
			};
		}
	}

	private void UpdateCardRuntimeCooldownVisual()
	{
		if (GodotObject.IsInstanceValid(_cardPacketShowPreview))
		{
			_cardPacketShowPreview.coldDown = _cardRuntimeCooldownDuration;
			_cardPacketShowPreview.coldDownTimer = _cardRuntimeCooldownRemaining;
			if (GodotObject.IsInstanceValid(_cardPacketShowPreview.coldDownProgressBar))
			{
				_cardPacketShowPreview.coldDownProgressBar.MaxValue = Math.Max(0.001, _cardRuntimeCooldownDuration);
				_cardPacketShowPreview.coldDownProgressBar.Value = _cardRuntimeCooldownRemaining;
				_cardPacketShowPreview.coldDownProgressBar.Visible = _cardRuntimePreviewState == 2;
			}
		}
	}

	private void RebuildCardCharacterPreview(bool force = false)
	{
		if (!GodotObject.IsInstanceValid(_cardCharacterPreviewRoot))
		{
			return;
		}
		TowerDefenseCharacterConfig towerDefenseCharacterConfig = _editingPacket?.characterConfig;
		string text = BuildCardArmorFingerprint(_editingPacket?.initArmor);
		if (!force && _cardCharacterPreviewInitialized && towerDefenseCharacterConfig == _cardCharacterPreviewConfig && string.Equals(text, _cardCharacterPreviewArmorFingerprint, StringComparison.Ordinal))
		{
			return;
		}
		DisposeCardCharacterPreview();
		_cardCharacterPreviewInitialized = true;
		_cardCharacterPreviewConfig = towerDefenseCharacterConfig;
		_cardCharacterPreviewArmorFingerprint = text;
		foreach (Node child in _cardCharacterPreviewRoot.GetChildren())
		{
			if (!child.IsQueuedForDeletion())
			{
				child.QueueFree();
			}
		}
		_cardCharacterPreview = null;
		_cardCharacterPreviewSafety = new XWGameplayLogicPreviewSafety();
		if (!GodotObject.IsInstanceValid(towerDefenseCharacterConfig) || !TryResolveCardCharacterScene(towerDefenseCharacterConfig, out var scene))
		{
			SetCardCharacterMissing(missing: true, GodotObject.IsInstanceValid(towerDefenseCharacterConfig) ? "未找到绑定角色的完整场景" : "请先为卡片绑定角色");
			return;
		}
		try
		{
			_cardCharacterPreviewBuildCount++;
			Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
			_cardCharacterPreview = FindCardRuntimeCharacter(node);
			if (!GodotObject.IsInstanceValid(_cardCharacterPreview))
			{
				node.Free();
				SetCardCharacterMissing(missing: true, "角色场景缺少 TowerDefenseCharacter");
				return;
			}
			_cardCharacterPreview.inGame = false;
			_cardCharacterPreview.editorPreviewMode = true;
			_cardCharacterPreview.config = towerDefenseCharacterConfig;
			if (_editingPacket.initArmor != null)
			{
				_cardCharacterPreview.currentArmor = new Array<string>(_editingPacket.initArmor);
			}
			_cardCharacterPreviewSafety.PrepareCharacter(node);
			_cardCharacterPreviewSafety.TrackPreviewRoot(node);
			_cardCharacterPreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			SetCardCharacterMissing(missing: false, "");
		}
		catch (Exception ex)
		{
			_cardCharacterPreview = null;
			SetCardCharacterMissing(missing: true, "角色预览失败：" + ex.Message);
		}
	}

	private void DisposeCardCharacterPreview()
	{
		_cardCharacterPreviewSafety?.Dispose();
		_cardCharacterPreviewSafety = null;
		_cardCharacterPreview = null;
		_cardCharacterPreviewInitialized = false;
		_cardCharacterPreviewConfig = null;
		_cardCharacterPreviewArmorFingerprint = "";
	}

	private static string BuildCardArmorFingerprint(Array<string> armor)
	{
		if (armor == null || armor.Count == 0)
		{
			return "";
		}
		return string.Join('\u001f', armor);
	}

	private static bool TryResolveCardCharacterScene(TowerDefenseCharacterConfig characterConfig, out PackedScene scene)
	{
		scene = null;
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(characterConfig.name) && ResourceManager.Instance != null)
		{
			scene = ResourceManager.Instance.GetCharacterScene(characterConfig.name);
			if (GodotObject.IsInstanceValid(scene))
			{
				return true;
			}
		}
		string baseDir = NormalizeResourcePath(characterConfig.ResourcePath).GetBaseDir();
		string text = (string.Equals(baseDir.GetFile(), "Config", StringComparison.OrdinalIgnoreCase) ? baseDir.GetBaseDir() : baseDir).PathJoin("Scene");
		if (!DirAccess.DirExistsAbsolute(text))
		{
			return false;
		}
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

	private static TowerDefenseCharacter FindCardRuntimeCharacter(Node node)
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
			TowerDefenseCharacter towerDefenseCharacter = FindCardRuntimeCharacter(child);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private void SetCardCharacterMissing(bool missing, string message)
	{
		if (GodotObject.IsInstanceValid(_cardCharacterMissingLabel))
		{
			_cardCharacterMissingLabel.Visible = missing;
			_cardCharacterMissingLabel.Text = message;
		}
	}

	private void SetPacketShowPreviewMissing(bool missing)
	{
		if (GodotObject.IsInstanceValid(_cardImageMissingLabel))
		{
			_cardImageMissingLabel.Visible = missing;
		}
	}

	private static string GetEffectiveCostText(TowerDefensePacketConfig packet)
	{
		long effectiveCost = GetEffectiveCost(packet);
		if (effectiveCost >= 0)
		{
			return effectiveCost.ToString();
		}
		return "-";
	}

	private static long GetEffectiveCost(TowerDefensePacketConfig packet)
	{
		XWCardPreviewValueSource source;
		return XWCardBehaviorPreviewSafety.ResolveBaseOverrideCost(packet, out source);
	}

	private static string GetEffectiveCooldownText(TowerDefensePacketConfig packet)
	{
		XWCardPreviewValueSource source;
		double num = XWCardBehaviorPreviewSafety.ResolveBaseOverrideCooldown(packet, out source);
		if (!(num < 0.0))
		{
			return num.ToString("0.###");
		}
		return "-";
	}

	private static string BuildCardValueOverrideSummary(TowerDefensePacketConfig packet)
	{
		if (packet == null)
		{
			return "数值覆盖：未加载";
		}
		XWCardBehaviorPreviewSnapshot xWCardBehaviorPreviewSnapshot = XWCardBehaviorPreviewSafety.Capture(packet);
		string previewValueSourceLabel = GetPreviewValueSourceLabel(xWCardBehaviorPreviewSnapshot.CostSource);
		string previewValueSourceLabel2 = GetPreviewValueSourceLabel(xWCardBehaviorPreviewSnapshot.CooldownSource);
		string previewValueSourceLabel3 = GetPreviewValueSourceLabel(xWCardBehaviorPreviewSnapshot.StartingCooldownSource);
		string value = ((xWCardBehaviorPreviewSnapshot.StartingCooldown < 0.0) ? "-" : xWCardBehaviorPreviewSnapshot.StartingCooldown.ToString("0.###"));
		return $"数值覆盖 · 费用 {previewValueSourceLabel} {GetEffectiveCostText(packet)} · 冷却 {previewValueSourceLabel2} {GetEffectiveCooldownText(packet)} 秒 · 初始 {previewValueSourceLabel3} {value} 秒";
	}

	private static string GetPreviewValueSourceLabel(XWCardPreviewValueSource source)
	{
		return source switch
		{
			XWCardPreviewValueSource.CharacterBase => "继承角色", 
			XWCardPreviewValueSource.PacketOverride => "卡牌覆盖", 
			XWCardPreviewValueSource.OverrideResource => "规则覆盖", 
			_ => "未配置", 
		};
	}

	private static bool TryGetInt(Resource resource, string propertyName, out int value)
	{
		value = 0;
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		try
		{
			Variant variant = resource.Get(propertyName);
			if (variant.VariantType == Variant.Type.Int)
			{
				value = variant.AsInt32();
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetDouble(Resource resource, string propertyName, out double value)
	{
		value = 0.0;
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		try
		{
			Variant variant = resource.Get(propertyName);
			if (variant.VariantType == Variant.Type.Float || variant.VariantType == Variant.Type.Int)
			{
				value = variant.AsDouble();
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static Color GetPacketTypeColor(TowerDefenseEnum.PACKET_TYPE type)
	{
		return type switch
		{
			TowerDefenseEnum.PACKET_TYPE.GOLD => new Color(0.82f, 0.62f, 0.2f), 
			TowerDefenseEnum.PACKET_TYPE.DIAMOND => new Color(0.25f, 0.6f, 0.86f), 
			TowerDefenseEnum.PACKET_TYPE.COLOUR => new Color(0.55f, 0.4f, 0.92f), 
			TowerDefenseEnum.PACKET_TYPE.STAR => new Color(0.82f, 0.46f, 0.18f), 
			TowerDefenseEnum.PACKET_TYPE.ORIGINAL => new Color(0.3f, 0.5f, 0.3f), 
			TowerDefenseEnum.PACKET_TYPE.ZOMBIE => new Color(0.4f, 0.42f, 0.46f), 
			TowerDefenseEnum.PACKET_TYPE.COVER => new Color(0.3f, 0.3f, 0.36f), 
			TowerDefenseEnum.PACKET_TYPE.GRAY => new Color(0.28f, 0.28f, 0.28f), 
			_ => new Color(0.16f, 0.18f, 0.2f), 
		};
	}

	private static string GetPacketTexturePath(TowerDefenseEnum.PACKET_TYPE packetType)
	{
		return "res://Asset/Texture/TowerDefense/Packet/PC/" + packetType switch
		{
			TowerDefenseEnum.PACKET_TYPE.GOLD => "PacketGold.png", 
			TowerDefenseEnum.PACKET_TYPE.DIAMOND => "PacketDiamond.png", 
			TowerDefenseEnum.PACKET_TYPE.COLOUR => "PacketColour.png", 
			TowerDefenseEnum.PACKET_TYPE.STAR => "PacketStar.png", 
			TowerDefenseEnum.PACKET_TYPE.ZOMBIE => "PacketZombie.png", 
			TowerDefenseEnum.PACKET_TYPE.COVER => "PacketCover.png", 
			TowerDefenseEnum.PACKET_TYPE.GRAY => "PacketGray.png", 
			_ => "PacketNormal.png", 
		};
	}

	private static string GetPacketTypeDisplayName(TowerDefenseEnum.PACKET_TYPE packetType)
	{
		return packetType switch
		{
			TowerDefenseEnum.PACKET_TYPE.NOONE => "无卡面", 
			TowerDefenseEnum.PACKET_TYPE.WHITE => "普通白卡", 
			TowerDefenseEnum.PACKET_TYPE.GOLD => "金卡", 
			TowerDefenseEnum.PACKET_TYPE.DIAMOND => "钻石卡", 
			TowerDefenseEnum.PACKET_TYPE.COLOUR => "彩卡", 
			TowerDefenseEnum.PACKET_TYPE.STAR => "星卡", 
			TowerDefenseEnum.PACKET_TYPE.ORIGINAL => "原版卡", 
			TowerDefenseEnum.PACKET_TYPE.ZOMBIE => "僵尸卡", 
			TowerDefenseEnum.PACKET_TYPE.COVER => "覆盖卡", 
			TowerDefenseEnum.PACKET_TYPE.GRAY => "灰卡", 
			_ => packetType.ToString(), 
		};
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

	private static string FormatCharacterBinding(TowerDefenseCharacterConfig character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return "未配置";
		}
		return GetCharacterSourceTag(ResolveCharacterSource(character.ResourcePath)) + " " + FormatResource(character);
	}

	private static string FormatVariant(Variant value)
	{
		switch (value.VariantType)
		{
		case Variant.Type.Nil:
			return "空";
		case Variant.Type.Object:
			return (value.AsGodotObject() is Resource resource) ? FormatResource(resource) : (value.AsGodotObject()?.GetType().Name ?? "空");
		case Variant.Type.String:
		case Variant.Type.StringName:
			return EmptyToPlaceholder(value.AsString());
		default:
			return value.ToString();
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

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}
}
