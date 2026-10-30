using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketSpawnEntryVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum PacketEntryMode
	{
		LevelSeedBank,
		Conveyor,
		Rain
	}

	private readonly struct PacketEntryView
	{
		public Resource Resource { get; }

		public PacketEntryMode Mode { get; }

		public StringName PacketKeyProperty { get; }

		public string PacketKey { get; }

		public TowerDefensePacketOverride Override { get; }

		public bool HasSpawnTuning { get; }

		public PacketEntryView(Resource resource, PacketEntryMode mode, StringName packetKeyProperty, string packetKey, TowerDefensePacketOverride packetOverride, bool hasSpawnTuning)
		{
			Resource = resource;
			Mode = mode;
			PacketKeyProperty = packetKeyProperty;
			PacketKey = packetKey ?? "";
			Override = packetOverride;
			HasSpawnTuning = hasSpawnTuning;
		}
	}

	private const string WorkbenchScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketSpawnEntryWorkbench.tscn";

	private const string PacketSelectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketSpawnEntryPacketSelectorWindow.tscn";

	private static PackedScene _workbenchScene;

	private static PackedScene _packetSelectorScene;

	private PacketEntryView _entry;

	private Resource _editingResource;

	private VBoxContainer _workbenchRoot;

	private LineEdit _resourceNameEdit;

	private CheckButton _localToSceneCheck;

	private Label _resourceMetaStatus;

	private Label _modeBadge;

	private Label _resourceStatus;

	private TextureRect _seedBankStage;

	private TextureRect _conveyorStage;

	private Control _rainStage;

	private Control _rainLandingRing;

	private Control _packetSlot;

	private Control _packetPreviewHost;

	private Button _missingPacketBadge;

	private Button _overrideBadge;

	private Label _stageHint;

	private PanelContainer _spawnTuningPanel;

	private TextureProgressBar _weightMeter;

	private HSlider _weightSlider;

	private Label _weightLabel;

	private XWPacketSpawnEntryRangeControl _countRangeControl;

	private XWPacketSpawnEntryRangeControl _magnificationRangeControl;

	private Button _retryPreviewButton;

	private Button _choosePacketButton;

	private Button _createOverrideButton;

	private Button _openOverrideButton;

	private Button _clearOverrideButton;

	private ConfirmationDialog _clearOverrideConfirmation;

	private XWPacketSpawnEntryPacketSelectorWindow _selectorWindow;

	private XWVisualPropertyBinding _propertyBinding;

	private bool _countEditingMinimum;

	private bool _magnificationEditingMinimum;

	private int _previewRequestVersion;

	private int _previewRetryCount;

	private Tween _stageTween;

	private TowerDefenseInGamePacketShow _packetShow;

	public override void _Ready()
	{
		SetProcess(enable: false);
		base.VisibilityChanged += UpdatePreviewVisibility;
		base._Ready();
		SetPreviewActive(IsVisibleInTree());
	}

	public override void _ExitTree()
	{
		base.VisibilityChanged -= UpdatePreviewVisibility;
		DisposeWorkbench();
		if (GodotObject.IsInstanceValid(_selectorWindow))
		{
			_selectorWindow.PacketSelected -= OnPacketSelected;
			_selectorWindow.CancelPendingRequests();
		}
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeWorkbench();
		if (base.CanvasGrid == null)
		{
			return;
		}
		if (!TryCreateEntryView(base.CurrentResource, out var view))
		{
			base.RenderCustomVisualPreset(preset);
			return;
		}
		if (_workbenchScene == null)
		{
			_workbenchScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketSpawnEntryWorkbench.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_workbenchRoot = _workbenchScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(_workbenchRoot))
		{
			_editingResource = view.Resource;
			_entry = view;
			_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), base.NotifyCurrentResourceEdited);
			base.CanvasGrid.Columns = 1;
			base.CanvasGrid.AddChild(_workbenchRoot, forceReadableName: false, InternalMode.Disabled);
			BindWorkbench(_workbenchRoot);
			ApplyModeStage();
			BindVisualEditing();
			RefreshSpawnTuningVisuals();
			RefreshPacketPreview();
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompletePacketSpawnEntryVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompletePacketSpawnEntryVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(TowerDefenseLevelPacketConfig)) && !(type == typeof(TowerDefenseConveyorPacketConfig)))
		{
			return type == typeof(TowerDefenseRainModePacketConfig);
		}
		return true;
	}

	private static bool TryCreateEntryView(Resource resource, out PacketEntryView view)
	{
		if (!(resource is TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig))
		{
			if (!(resource is TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig))
			{
				if (resource is TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig)
				{
					view = new PacketEntryView(towerDefenseRainModePacketConfig, PacketEntryMode.Rain, "name", towerDefenseRainModePacketConfig.name, towerDefenseRainModePacketConfig.@override, hasSpawnTuning: true);
					return true;
				}
				view = default(PacketEntryView);
				return false;
			}
			view = new PacketEntryView(towerDefenseConveyorPacketConfig, PacketEntryMode.Conveyor, "name", towerDefenseConveyorPacketConfig.name, towerDefenseConveyorPacketConfig.@override, hasSpawnTuning: true);
			return true;
		}
		view = new PacketEntryView(towerDefenseLevelPacketConfig, PacketEntryMode.LevelSeedBank, "packetName", towerDefenseLevelPacketConfig.packetName, towerDefenseLevelPacketConfig.@override, hasSpawnTuning: false);
		return true;
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_resourceNameEdit = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToSceneCheck = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_resourceMetaStatus = root.GetNode<Label>("%ResourceMetaStatus");
		_modeBadge = root.GetNode<Label>("%ModeBadge");
		_resourceStatus = root.GetNode<Label>("%ResourceStatus");
		_seedBankStage = root.GetNode<TextureRect>("%SeedBankStage");
		_conveyorStage = root.GetNode<TextureRect>("%ConveyorStage");
		_rainStage = root.GetNode<Control>("%RainStage");
		_rainLandingRing = _rainStage.GetNode<Control>("RainLandingRing");
		_packetSlot = root.GetNode<Control>("%PacketSlot");
		_packetPreviewHost = root.GetNode<Control>("%PacketPreviewHost");
		_missingPacketBadge = root.GetNode<Button>("%MissingPacketBadge");
		_overrideBadge = root.GetNode<Button>("%OverrideBadge");
		_stageHint = root.GetNode<Label>("%StageHint");
		_spawnTuningPanel = root.GetNode<PanelContainer>("%SpawnTuningPanel");
		_weightMeter = root.GetNode<TextureProgressBar>("%WeightMeter");
		_weightSlider = root.GetNode<HSlider>("%WeightSlider");
		_weightLabel = root.GetNode<Label>("%WeightLabel");
		_countRangeControl = root.GetNode<XWPacketSpawnEntryRangeControl>("%CountRangeControl");
		_magnificationRangeControl = root.GetNode<XWPacketSpawnEntryRangeControl>("%MagnificationRangeControl");
		_retryPreviewButton = root.GetNode<Button>("%RetryPreviewButton");
		_choosePacketButton = root.GetNode<Button>("%ChoosePacketButton");
		_createOverrideButton = root.GetNode<Button>("%CreateOverrideButton");
		_openOverrideButton = root.GetNode<Button>("%OpenOverrideButton");
		_clearOverrideButton = root.GetNode<Button>("%ClearOverrideButton");
		_clearOverrideConfirmation = root.GetNode<ConfirmationDialog>("%ClearOverrideConfirmation");
		_retryPreviewButton.Pressed += RefreshPacketPreview;
		_choosePacketButton.Pressed += ShowPacketSelector;
		_missingPacketBadge.Pressed += ShowPacketSelector;
		_overrideBadge.Pressed += OpenOverride;
		_createOverrideButton.Pressed += CreateOverride;
		_openOverrideButton.Pressed += OpenOverride;
		_clearOverrideButton.Pressed += ConfirmClearOverride;
		_clearOverrideConfirmation.Confirmed += ClearOverride;
	}

	private void BindVisualEditing()
	{
		_propertyBinding.BindText(_resourceNameEdit, _entry.Resource, "resource_name", RefreshResourceMetadata, this, "RefreshPacketSpawnEntryFromHistory");
		_propertyBinding.BindToggle(_localToSceneCheck, _entry.Resource, "resource_local_to_scene", RefreshResourceMetadata, this, "RefreshPacketSpawnEntryFromHistory");
		RefreshResourceMetadata();
		if (_entry.HasSpawnTuning && _propertyBinding != null)
		{
			_weightSlider.MinValue = 0.0;
			_weightSlider.MaxValue = 999.0;
			_weightSlider.Step = 1.0;
			_propertyBinding.BindNumber(_weightSlider, _entry.Resource, "weight", RefreshWeightVisuals, this, "RefreshPacketSpawnEntryFromHistory");
			_countRangeControl.SetTitle("生成数量区间");
			_countRangeControl.Configure(-1.0, 999.0, 1.0, allowUnlimited: true);
			_countRangeControl.DragStarted += BeginCountRangeEdit;
			_countRangeControl.ValuesPreviewed += PreviewCountRange;
			_countRangeControl.ValuesCommitted += CommitCountRange;
			_magnificationRangeControl.SetTitle("低数量 / 高数量倍率");
			_magnificationRangeControl.Configure(0.0, 10.0, 0.05, allowUnlimited: false);
			_magnificationRangeControl.DragStarted += BeginMagnificationRangeEdit;
			_magnificationRangeControl.ValuesPreviewed += PreviewMagnificationRange;
			_magnificationRangeControl.ValuesCommitted += CommitMagnificationRange;
		}
	}

	private void RefreshResourceMetadata()
	{
		if (GodotObject.IsInstanceValid(_editingResource) && GodotObject.IsInstanceValid(_resourceMetaStatus))
		{
			string text = (string.IsNullOrWhiteSpace(_editingResource.ResourceName) ? _editingResource.GetType().Name : _editingResource.ResourceName);
			_resourceMetaStatus.Text = text + " · " + (_editingResource.ResourceLocalToScene ? "仅当前场景" : "可复用资源");
		}
	}

	private void ApplyModeStage()
	{
		if (GodotObject.IsInstanceValid(_modeBadge))
		{
			bool flag = _entry.Mode == PacketEntryMode.LevelSeedBank;
			bool flag2 = _entry.Mode == PacketEntryMode.Conveyor;
			bool visible = _entry.Mode == PacketEntryMode.Rain;
			_seedBankStage.Visible = flag;
			_conveyorStage.Visible = flag2;
			_rainStage.Visible = visible;
			_spawnTuningPanel.Visible = _entry.HasSpawnTuning;
			if (flag)
			{
				_modeBadge.Text = "种子栏条目";
				_stageHint.Text = "卡牌位于游戏种子栏槽位；卡面使用运行时配置副本";
				_packetSlot.Position = new Vector2(216f, 83f);
			}
			else if (flag2)
			{
				_modeBadge.Text = "传送带候选";
				_stageHint.Text = "卡牌位于传送带轨道；右侧展示实际生成权重与区间";
				_packetSlot.Position = new Vector2(216f, 145f);
			}
			else
			{
				_modeBadge.Text = "种子雨候选";
				_stageHint.Text = "卡牌位于种子雨掉落轨迹；落点环表示游戏内可拾取位置";
				_packetSlot.Position = new Vector2(216f, 76f);
			}
			_packetSlot.Size = new Vector2(160f, 220f);
			StartModeStageTween();
			SetPreviewActive(IsVisibleInTree());
		}
	}

	private void StartModeStageTween()
	{
		_stageTween?.Kill();
		_stageTween = null;
		if (!GodotObject.IsInstanceValid(_packetSlot) || _entry.Mode == PacketEntryMode.LevelSeedBank)
		{
			return;
		}
		if (_entry.Mode == PacketEntryMode.Conveyor)
		{
			Vector2 position = new Vector2(216f, 145f);
			_packetSlot.Position = position;
			_stageTween = CreateTween().SetLoops();
			_stageTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			_stageTween.TweenProperty(_packetSlot, "position:x", 338f, 1.6);
			_stageTween.TweenProperty(_packetSlot, "position:x", position.X, 1.6);
		}
		else
		{
			if (_entry.Mode != PacketEntryMode.Rain)
			{
				return;
			}
			Vector2 start = new Vector2(216f, 52f);
			_packetSlot.Position = start;
			if (GodotObject.IsInstanceValid(_rainLandingRing))
			{
				_rainLandingRing.PivotOffset = _rainLandingRing.Size * 0.5f;
				_rainLandingRing.Scale = new Vector2(0.78f, 0.78f);
				_rainLandingRing.Modulate = new Color(1f, 1f, 1f, 0.42f);
			}
			_stageTween = CreateTween().SetLoops();
			_stageTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			_stageTween.TweenProperty(_packetSlot, "position:y", 222f, 1.35);
			if (GodotObject.IsInstanceValid(_rainLandingRing))
			{
				_stageTween.Parallel().TweenProperty(_rainLandingRing, "scale", new Vector2(1.12f, 1.12f), 1.35);
				_stageTween.Parallel().TweenProperty(_rainLandingRing, "modulate:a", 0.95f, 1.35);
			}
			_stageTween.TweenInterval(0.22);
			_stageTween.TweenCallback(Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(_packetSlot))
				{
					_packetSlot.Position = start;
				}
				if (GodotObject.IsInstanceValid(_rainLandingRing))
				{
					_rainLandingRing.Scale = new Vector2(0.78f, 0.78f);
					_rainLandingRing.Modulate = new Color(1f, 1f, 1f, 0.42f);
				}
			}));
		}
	}

	private void UpdatePreviewVisibility()
	{
		bool flag = IsVisibleInTree();
		if (flag && GodotObject.IsInstanceValid(_editingResource) && TryCreateEntryView(_editingResource, out var view))
		{
			_entry = view;
			RefreshSpawnTuningVisuals();
			RefreshPacketPreview();
		}
		SetPreviewActive(flag);
	}

	private void SetPreviewActive(bool active)
	{
		active = active && GodotObject.IsInstanceValid(_editingResource) && GodotObject.IsInstanceValid(_workbenchRoot);
		SetProcess(enable: false);
		if (GodotObject.IsInstanceValid(_packetShow))
		{
			_packetShow.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		if (active)
		{
			_stageTween?.Play();
			return;
		}
		_stageTween?.Pause();
		if (GodotObject.IsInstanceValid(_selectorWindow))
		{
			_selectorWindow.CancelPendingRequests();
			_selectorWindow.Hide();
		}
	}

	private void RefreshSpawnTuningVisuals()
	{
		if (!GodotObject.IsInstanceValid(_spawnTuningPanel))
		{
			return;
		}
		if (!_entry.HasSpawnTuning)
		{
			_spawnTuningPanel.Visible = false;
			return;
		}
		int weight;
		int minNum;
		int maxNum;
		double minMagnification;
		double maxMagnification;
		if (_entry.Resource is TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig)
		{
			weight = towerDefenseConveyorPacketConfig.weight;
			minNum = towerDefenseConveyorPacketConfig.minNum;
			maxNum = towerDefenseConveyorPacketConfig.maxNum;
			minMagnification = towerDefenseConveyorPacketConfig.minMagnification;
			maxMagnification = towerDefenseConveyorPacketConfig.maxMagnification;
		}
		else
		{
			if (!(_entry.Resource is TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig))
			{
				return;
			}
			weight = towerDefenseRainModePacketConfig.weight;
			minNum = towerDefenseRainModePacketConfig.minNum;
			maxNum = towerDefenseRainModePacketConfig.maxNum;
			minMagnification = towerDefenseRainModePacketConfig.minMagnification;
			maxMagnification = towerDefenseRainModePacketConfig.maxMagnification;
		}
		double maxValue = Math.Max(100, Math.Max(1, weight));
		_weightMeter.MaxValue = maxValue;
		_weightMeter.Value = Math.Max(0, weight);
		_weightSlider.MaxValue = 999.0;
		_weightSlider.Value = Math.Max(0, weight);
		_weightLabel.Text = $"相对权重 {weight}";
		_countRangeControl?.SetValues(minNum, maxNum);
		_magnificationRangeControl?.SetValues(minMagnification, maxMagnification);
	}

	private void RefreshWeightVisuals()
	{
		if (GodotObject.IsInstanceValid(_weightMeter) && TryReadSpawnTuning(out var weight, out var _, out var _, out var _, out var _))
		{
			_weightMeter.MaxValue = Math.Max(100, Math.Max(1, weight));
			_weightMeter.Value = Math.Max(0, weight);
			_weightLabel.Text = $"相对权重 {weight}";
		}
	}

	private bool TryReadSpawnTuning(out int weight, out int minimumCount, out int maximumCount, out double minimumMagnification, out double maximumMagnification)
	{
		if (_entry.Resource is TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig)
		{
			weight = towerDefenseConveyorPacketConfig.weight;
			minimumCount = towerDefenseConveyorPacketConfig.minNum;
			maximumCount = towerDefenseConveyorPacketConfig.maxNum;
			minimumMagnification = towerDefenseConveyorPacketConfig.minMagnification;
			maximumMagnification = towerDefenseConveyorPacketConfig.maxMagnification;
			return true;
		}
		if (_entry.Resource is TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig)
		{
			weight = towerDefenseRainModePacketConfig.weight;
			minimumCount = towerDefenseRainModePacketConfig.minNum;
			maximumCount = towerDefenseRainModePacketConfig.maxNum;
			minimumMagnification = towerDefenseRainModePacketConfig.minMagnification;
			maximumMagnification = towerDefenseRainModePacketConfig.maxMagnification;
			return true;
		}
		weight = 0;
		minimumCount = -1;
		maximumCount = -1;
		minimumMagnification = 0.0;
		maximumMagnification = 0.0;
		return false;
	}

	private void BeginCountRangeEdit(bool minimumHandle)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			_countEditingMinimum = minimumHandle;
			_propertyBinding.BeginEdit(_entry.Resource, minimumHandle ? "minNum" : "maxNum");
		}
	}

	private void PreviewCountRange(double minimum, double maximum)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			StringName property = (_countEditingMinimum ? "minNum" : "maxNum");
			int from = (int)Math.Round(_countEditingMinimum ? minimum : maximum);
			_propertyBinding.PreviewValue(_entry.Resource, property, Variant.From(in from));
		}
	}

	private void CommitCountRange(double minimum, double maximum)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			StringName property = (_countEditingMinimum ? "minNum" : "maxNum");
			int from = (int)Math.Round(_countEditingMinimum ? minimum : maximum);
			_propertyBinding.CommitEdit(_entry.Resource, property, Variant.From(in from), "修改生成数量范围", this, "RefreshPacketSpawnEntryFromHistory");
		}
	}

	private void BeginMagnificationRangeEdit(bool minimumHandle)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			_magnificationEditingMinimum = minimumHandle;
			_propertyBinding.BeginEdit(_entry.Resource, minimumHandle ? "minMagnification" : "maxMagnification");
		}
	}

	private void PreviewMagnificationRange(double minimum, double maximum)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			StringName property = (_magnificationEditingMinimum ? "minMagnification" : "maxMagnification");
			double from = (_magnificationEditingMinimum ? minimum : maximum);
			_propertyBinding.PreviewValue(_entry.Resource, property, Variant.From(in from));
		}
	}

	private void CommitMagnificationRange(double minimum, double maximum)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource))
		{
			StringName property = (_magnificationEditingMinimum ? "minMagnification" : "maxMagnification");
			double from = (_magnificationEditingMinimum ? minimum : maximum);
			_propertyBinding.CommitEdit(_entry.Resource, property, Variant.From(in from), "修改生成倍率范围", this, "RefreshPacketSpawnEntryFromHistory");
		}
	}

	private void ShowPacketSelector()
	{
		EnsurePacketSelectorWindow();
		_selectorWindow?.ShowFor(_entry.PacketKey);
	}

	private void EnsurePacketSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_selectorWindow))
		{
			if (_packetSelectorScene == null)
			{
				_packetSelectorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketSpawnEntryPacketSelectorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_selectorWindow = _packetSelectorScene?.Instantiate<XWPacketSpawnEntryPacketSelectorWindow>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_selectorWindow))
			{
				_selectorWindow.PacketSelected += OnPacketSelected;
				AddChild(_selectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OnPacketSelected(string packetKey)
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource) && !string.IsNullOrWhiteSpace(packetKey))
		{
			_propertyBinding.SetValue(_entry.Resource, _entry.PacketKeyProperty, Variant.From(in packetKey), "选择关卡卡牌", this, "RefreshPacketSpawnEntryFromHistory");
			RefreshEntryAfterEdit();
		}
	}

	private void CreateOverride()
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource) && !GodotObject.IsInstanceValid(_entry.Override))
		{
			_propertyBinding.SetValue(_entry.Resource, "override", Variant.From<TowerDefensePacketOverride>(new TowerDefensePacketOverride()), "创建卡牌覆盖规则", this, "RefreshPacketSpawnEntryFromHistory");
			RefreshEntryAfterEdit();
		}
	}

	private void OpenOverride()
	{
		if (GodotObject.IsInstanceValid(_entry.Override))
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(_entry.Override, _entry.Resource, _entry.Override.ResourcePath, base.CurrentResourcePath, "override", -1, "packet_spawn_entry_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
			XWEditorInterface.Instance?.EditResource(_entry.Override, context);
		}
	}

	private void ConfirmClearOverride()
	{
		if (GodotObject.IsInstanceValid(_entry.Override))
		{
			_clearOverrideConfirmation?.PopupCentered();
		}
	}

	private void ClearOverride()
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_entry.Resource) && GodotObject.IsInstanceValid(_entry.Override))
		{
			_propertyBinding.SetValue(_entry.Resource, "override", Variant.From<TowerDefensePacketOverride>((TowerDefensePacketOverride)null), "清除卡牌覆盖规则", this, "RefreshPacketSpawnEntryFromHistory");
			RefreshEntryAfterEdit();
		}
	}

	public void RefreshPacketSpawnEntryFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || (!xWUndoRedoManager.IsUndoing() && !xWUndoRedoManager.IsRedoing()) || base.CanvasGrid == null || !TryCreateEntryView(base.CurrentResource, out var _))
		{
			return;
		}
		DisposeWorkbench();
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderCustomVisualPreset(null);
	}

	private void RefreshEntryAfterEdit()
	{
		if (TryCreateEntryView(_editingResource, out var view))
		{
			_entry = view;
			RefreshSpawnTuningVisuals();
			RefreshPacketPreview();
		}
	}

	private void RefreshPacketPreview()
	{
		_previewRequestVersion++;
		_previewRetryCount = 0;
		HydratePacketPreview(_previewRequestVersion);
	}

	private void HydratePacketPreview(int requestVersion)
	{
		if (requestVersion != _previewRequestVersion || _editingResource != base.CurrentResource)
		{
			return;
		}
		ReleasePacketPreview();
		if (!GodotObject.IsInstanceValid(_packetPreviewHost))
		{
			return;
		}
		string text = _entry.PacketKey?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			SetPacketPreviewMissing("尚未选择卡牌");
			return;
		}
		TowerDefensePacketConfig packetConfigReadOnly;
		try
		{
			packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text);
		}
		catch (Exception ex)
		{
			SchedulePacketPreviewRetry(requestVersion, "卡牌注册表未就绪：" + ex.Message);
			return;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = packetConfigReadOnly?.Duplicate(deep: true) as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			SchedulePacketPreviewRetry(requestVersion, "找不到卡牌：" + text);
			return;
		}
		bool flag = GodotObject.IsInstanceValid(_entry.Override);
		bool invalid = false;
		if (flag)
		{
			towerDefensePacketConfig._override = _entry.Override.Duplicate(deep: true) as TowerDefensePacketOverride;
			invalid = !GodotObject.IsInstanceValid(towerDefensePacketConfig._override);
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		try
		{
			towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShowWithConfig();
			if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				SchedulePacketPreviewRetry(requestVersion, "运行时卡牌场景不可用");
				return;
			}
			if (requestVersion != _previewRequestVersion || _editingResource != base.CurrentResource)
			{
				towerDefenseInGamePacketShow.QueueFree();
				return;
			}
			towerDefenseInGamePacketShow.Name = "PacketSpawnEntryPreview";
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.setPcLayout = true;
			towerDefenseInGamePacketShow.showLove = false;
			towerDefenseInGamePacketShow.select = false;
			towerDefenseInGamePacketShow.alive = true;
			towerDefenseInGamePacketShow.@lock = false;
			towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
			towerDefenseInGamePacketShow.ProcessMode = ProcessModeEnum.Inherit;
			towerDefenseInGamePacketShow.Position = new Vector2(80f, 110f);
			towerDefenseInGamePacketShow.Scale = new Vector2(2.65f, 2.65f);
			_packetPreviewHost.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(towerDefensePacketConfig);
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.RefreshPreview();
			_packetShow = towerDefenseInGamePacketShow;
			_previewRetryCount = 0;
		}
		catch (Exception ex2)
		{
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.GetParent()?.RemoveChild(towerDefenseInGamePacketShow);
				towerDefenseInGamePacketShow.QueueFree();
			}
			SchedulePacketPreviewRetry(requestVersion, "卡面预览失败：" + ex2.Message);
			return;
		}
		_missingPacketBadge.Visible = false;
		_resourceStatus.Text = "卡牌：" + text;
		ApplyOverrideBadge(flag, invalid);
		SetPreviewActive(IsVisibleInTree());
	}

	private void SchedulePacketPreviewRetry(int requestVersion, string stableMessage)
	{
		if (requestVersion != _previewRequestVersion || _editingResource != base.CurrentResource)
		{
			return;
		}
		if (_previewRetryCount >= 3)
		{
			SetPacketPreviewMissing(stableMessage);
			return;
		}
		_previewRetryCount++;
		if (GodotObject.IsInstanceValid(_resourceStatus))
		{
			_resourceStatus.Text = $"正在准备卡面预览（{_previewRetryCount}/3）";
		}
		CallDeferred("RetryPacketPreview", requestVersion, stableMessage);
	}

	private void RetryPacketPreview(int requestVersion, string stableMessage)
	{
		if (requestVersion == _previewRequestVersion && _editingResource == base.CurrentResource)
		{
			HydratePacketPreview(requestVersion);
		}
	}

	private void ApplyOverrideBadge(bool hasOverride, bool invalid)
	{
		if (GodotObject.IsInstanceValid(_overrideBadge))
		{
			_overrideBadge.Visible = hasOverride;
			_overrideBadge.Text = (invalid ? "覆盖错误" : "已覆盖");
			_overrideBadge.Modulate = (invalid ? new Color(1f, 0.38f, 0.3f) : Colors.White);
			_createOverrideButton.Disabled = hasOverride;
			_openOverrideButton.Disabled = !hasOverride;
			_clearOverrideButton.Disabled = !hasOverride;
		}
	}

	private void SetPacketPreviewMissing(string message)
	{
		if (GodotObject.IsInstanceValid(_missingPacketBadge))
		{
			_missingPacketBadge.Visible = true;
		}
		if (GodotObject.IsInstanceValid(_overrideBadge))
		{
			_overrideBadge.Visible = GodotObject.IsInstanceValid(_entry.Override);
		}
		if (GodotObject.IsInstanceValid(_resourceStatus))
		{
			_resourceStatus.Text = message;
		}
		ApplyOverrideBadge(GodotObject.IsInstanceValid(_entry.Override), invalid: false);
	}

	private void ReleasePacketPreview()
	{
		if (GodotObject.IsInstanceValid(_packetShow))
		{
			_packetShow.ClearEventHandlers();
			_packetShow.GetParent()?.RemoveChild(_packetShow);
			_packetShow.QueueFree();
		}
		_packetShow = null;
	}

	private void DisposeWorkbench()
	{
		_previewRequestVersion++;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_selectorWindow?.CancelPendingRequests();
		if (GodotObject.IsInstanceValid(_selectorWindow))
		{
			_selectorWindow.Hide();
		}
		_stageTween?.Kill();
		_stageTween = null;
		if (GodotObject.IsInstanceValid(_retryPreviewButton))
		{
			_retryPreviewButton.Pressed -= RefreshPacketPreview;
		}
		if (GodotObject.IsInstanceValid(_choosePacketButton))
		{
			_choosePacketButton.Pressed -= ShowPacketSelector;
		}
		if (GodotObject.IsInstanceValid(_missingPacketBadge))
		{
			_missingPacketBadge.Pressed -= ShowPacketSelector;
		}
		if (GodotObject.IsInstanceValid(_overrideBadge))
		{
			_overrideBadge.Pressed -= OpenOverride;
		}
		if (GodotObject.IsInstanceValid(_createOverrideButton))
		{
			_createOverrideButton.Pressed -= CreateOverride;
		}
		if (GodotObject.IsInstanceValid(_openOverrideButton))
		{
			_openOverrideButton.Pressed -= OpenOverride;
		}
		if (GodotObject.IsInstanceValid(_clearOverrideButton))
		{
			_clearOverrideButton.Pressed -= ConfirmClearOverride;
		}
		if (GodotObject.IsInstanceValid(_clearOverrideConfirmation))
		{
			_clearOverrideConfirmation.Confirmed -= ClearOverride;
		}
		if (GodotObject.IsInstanceValid(_countRangeControl))
		{
			_countRangeControl.DragStarted -= BeginCountRangeEdit;
			_countRangeControl.ValuesPreviewed -= PreviewCountRange;
			_countRangeControl.ValuesCommitted -= CommitCountRange;
		}
		if (GodotObject.IsInstanceValid(_magnificationRangeControl))
		{
			_magnificationRangeControl.DragStarted -= BeginMagnificationRangeEdit;
			_magnificationRangeControl.ValuesPreviewed -= PreviewMagnificationRange;
			_magnificationRangeControl.ValuesCommitted -= CommitMagnificationRange;
		}
		ReleasePacketPreview();
		_editingResource = null;
		_entry = default(PacketEntryView);
		_workbenchRoot = null;
		_resourceNameEdit = null;
		_localToSceneCheck = null;
		_resourceMetaStatus = null;
		_modeBadge = null;
		_resourceStatus = null;
		_seedBankStage = null;
		_conveyorStage = null;
		_rainStage = null;
		_rainLandingRing = null;
		_packetSlot = null;
		_packetPreviewHost = null;
		_missingPacketBadge = null;
		_overrideBadge = null;
		_stageHint = null;
		_spawnTuningPanel = null;
		_weightMeter = null;
		_weightSlider = null;
		_weightLabel = null;
		_countRangeControl = null;
		_magnificationRangeControl = null;
		_retryPreviewButton = null;
		_choosePacketButton = null;
		_createOverrideButton = null;
		_openOverrideButton = null;
		_clearOverrideButton = null;
		_clearOverrideConfirmation = null;
		SetProcess(enable: false);
	}
}
