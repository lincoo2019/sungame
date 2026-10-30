using System;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCollectableVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum CollectableKind
	{
		Shovel,
		AwardNote,
		Sun,
		Coin,
		Custom
	}

	private const string AwardSettlementPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAwardSettlementVisualPreview.tscn";

	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCollectableVisualEditorLayout.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private static PackedScene _awardSettlementPreviewScene;

	private static PackedScene _visualEditorLayoutScene;

	private static PackedScene _visualChoiceCardScene;

	private CollectableConfig _editingCollectable;

	private AwardSettlementConfig _editingAwardSettlement;

	private XWVisualPropertyBinding _collectablePropertyBinding;

	private XWVisualPropertyBinding _awardSettlementPropertyBinding;

	private TextureRect _awardBackgroundPreview;

	private TextureRect _awardImagePreview;

	private TextureRect _awardTexturePreview;

	private Label _awardEmptyHint;

	private Label _awardStatusLabel;

	private Label _awardTitleLabel;

	private Label _previewTitleLabel;

	private Label _previewConfigLabel;

	private Label _previewValueLabel;

	private Label _previewAudioLabel;

	private Label _previewSceneLabel;

	private Label _kindSummaryLabel;

	private TextureRect _texturePreview;

	private SubViewport _collectablePreviewViewport;

	private Node2D _collectablePreviewRoot;

	private Label _collectableRuntimeStatusLabel;

	private Button _collectableRuntimeRunButton;

	private bool _collectableRuntimeRunning;

	private HFlowContainer _collectableKindCards;

	private ItemList _configPropertyList;

	private ItemList _unlockConditionList;

	private ItemList _eventList;

	private ItemList _shovelableNameList;

	private bool _updatingControls;

	private Timer _collectableSaveTimer;

	private string _pendingCollectableSaveMessage;

	public int CollectableKindVisualCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_collectableKindCards))
			{
				return 0;
			}
			return _collectableKindCards.GetChildCount();
		}
	}

	public bool IsCollectablePreviewRendering
	{
		get
		{
			if (GodotObject.IsInstanceValid(_collectablePreviewViewport))
			{
				return _collectablePreviewViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Disabled;
			}
			return false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_collectableSaveTimer = new Timer
		{
			Name = "CollectableSaveDebounce",
			OneShot = true,
			WaitTime = 0.18
		};
		_collectableSaveTimer.Timeout += SavePendingCollectable;
		AddChild(_collectableSaveTimer, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _ExitTree()
	{
		SavePendingCollectable();
		DisposeCollectableBindings();
		if (GodotObject.IsInstanceValid(_collectableSaveTimer))
		{
			_collectableSaveTimer.QueueFree();
		}
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		if (GodotObject.IsInstanceValid(_collectablePreviewViewport))
		{
			_collectablePreviewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(visible ? 4 : 0);
		}
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeCollectableBindings();
		Resource currentResource = base.CurrentResource;
		if (!(currentResource is CollectableConfig collectableConfig))
		{
			if (currentResource is AwardSettlementConfig awardSettlementConfig)
			{
				_editingAwardSettlement = awardSettlementConfig;
				RenderAwardSettlementPreview(awardSettlementConfig);
			}
		}
		else
		{
			_editingCollectable = collectableConfig;
			RenderCollectableEditor(collectableConfig);
		}
	}

	private void DisposeCollectableBindings()
	{
		_collectablePropertyBinding?.Dispose();
		_collectablePropertyBinding = null;
		_awardSettlementPropertyBinding?.Dispose();
		_awardSettlementPropertyBinding = null;
		_editingCollectable = null;
		_editingAwardSettlement = null;
		_collectableRuntimeRunning = false;
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteCollectableVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteCollectableVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(CollectableConfig)))
		{
			return type == typeof(AwardSettlementConfig);
		}
		return true;
	}

	private void RenderAwardSettlementPreview(AwardSettlementConfig award)
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(award))
		{
			return;
		}
		if (_awardSettlementPreviewScene == null)
		{
			_awardSettlementPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAwardSettlementVisualPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Control control = _awardSettlementPreviewScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(control))
		{
			base.CanvasGrid.Columns = 1;
			base.CanvasGrid.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			_awardBackgroundPreview = control.GetNode<TextureRect>("Layout/Preview/Background");
			_awardImagePreview = control.GetNode<TextureRect>("Layout/Preview/Image");
			_awardTexturePreview = control.GetNode<TextureRect>("Layout/Preview/PickupIconFrame/PickupIcon");
			_awardEmptyHint = control.GetNode<Label>("Layout/Preview/EmptyHint");
			_awardStatusLabel = control.GetNode<Label>("Layout/Header/Status");
			_awardTitleLabel = control.GetNode<Label>("Layout/Header/Title");
			_awardSettlementPropertyBinding?.Dispose();
			_awardSettlementPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnAwardSettlementPropertyEdited);
			_awardSettlementPropertyBinding.BindText(control.GetNode<LineEdit>("%ResourceNameEdit"), award, "resource_name", RefreshAwardSettlementPreviewFromHistory);
			_awardSettlementPropertyBinding.BindToggle(control.GetNode<CheckButton>("%LocalToSceneCheck"), award, "resource_local_to_scene", RefreshAwardSettlementPreviewFromHistory);
			MountAwardSettlementPicker(control.GetNode<HBoxContainer>("%BackgroundPickerHost"), "BackgroundPicker", award.background, delegate(Resource resource)
			{
				SetAwardSettlementTexture("background", resource, "更换奖励结算背景");
			});
			MountAwardSettlementPicker(control.GetNode<HBoxContainer>("%ImagePickerHost"), "ImagePicker", award.image, delegate(Resource resource)
			{
				SetAwardSettlementTexture("image", resource, "更换奖励展示图");
			});
			MountAwardSettlementPicker(control.GetNode<HBoxContainer>("%TexturePickerHost"), "TexturePicker", award.texture, delegate(Resource resource)
			{
				SetAwardSettlementTexture("texture", resource, "更换奖励拾取图标");
			});
			RefreshAwardSettlementPreviewFromHistory();
			base.PreviewList?.AddItem("直接在结算画面下方更换背景、展示图和拾取图标；修改会立即显示并支持撤销。");
		}
	}

	private void OnAwardSettlementPropertyEdited(bool committed)
	{
		if (base.CurrentResource is AwardSettlementConfig awardSettlementConfig && awardSettlementConfig == _editingAwardSettlement)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			awardSettlementConfig.EmitChanged();
		}
	}

	private void MountAwardSettlementPicker(HBoxContainer host, string name, Texture2D value, Action<Resource> changed)
	{
		if (GodotObject.IsInstanceValid(host))
		{
			XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
			xWResourcePicker.Name = name;
			xWResourcePicker.Setup("Texture2D");
			xWResourcePicker.SetEditedResource(value);
			xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			xWResourcePicker.ResourceChanged += delegate(Resource resource)
			{
				changed?.Invoke(resource);
			};
			host.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SetAwardSettlementTexture(StringName property, Resource resource, string actionName)
	{
		if (_awardSettlementPropertyBinding != null && base.CurrentResource is AwardSettlementConfig awardSettlementConfig && awardSettlementConfig == _editingAwardSettlement)
		{
			_awardSettlementPropertyBinding.SetValue(awardSettlementConfig, property, resource as Texture2D, actionName, this, "RefreshAwardSettlementPreviewFromHistory");
			RefreshAwardSettlementPreviewFromHistory();
		}
	}

	public void RefreshAwardSettlementPreviewFromHistory()
	{
		if (base.CurrentResource is AwardSettlementConfig awardSettlementConfig && awardSettlementConfig == _editingAwardSettlement)
		{
			if (GodotObject.IsInstanceValid(_awardBackgroundPreview))
			{
				_awardBackgroundPreview.Texture = awardSettlementConfig.background;
			}
			if (GodotObject.IsInstanceValid(_awardImagePreview))
			{
				_awardImagePreview.Texture = awardSettlementConfig.image;
			}
			if (GodotObject.IsInstanceValid(_awardTexturePreview))
			{
				_awardTexturePreview.Texture = awardSettlementConfig.texture;
			}
			bool visible = !GodotObject.IsInstanceValid(awardSettlementConfig.background) && !GodotObject.IsInstanceValid(awardSettlementConfig.image) && !GodotObject.IsInstanceValid(awardSettlementConfig.texture);
			if (GodotObject.IsInstanceValid(_awardEmptyHint))
			{
				_awardEmptyHint.Visible = visible;
			}
			if (GodotObject.IsInstanceValid(_awardStatusLabel))
			{
				_awardStatusLabel.Text = $"背景 {(GodotObject.IsInstanceValid(awardSettlementConfig.background) ? "✓" : "—")} · 展示图 {(GodotObject.IsInstanceValid(awardSettlementConfig.image) ? "✓" : "—")} · 图标 {(GodotObject.IsInstanceValid(awardSettlementConfig.texture) ? "✓" : "—")}";
			}
			if (GodotObject.IsInstanceValid(_awardTitleLabel))
			{
				_awardTitleLabel.Text = (string.IsNullOrWhiteSpace(awardSettlementConfig.ResourceName) ? "奖励结算窗口预览" : (awardSettlementConfig.ResourceName + " · 结算窗口"));
			}
		}
	}

	private void RenderCollectableEditor(CollectableConfig collectable)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			_collectablePropertyBinding?.Dispose();
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCollectableVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindCollectablePreview(vBoxContainer);
				BindCollectableKindPanel(vBoxContainer);
				BindCollectableTopLevelFields(vBoxContainer, collectable);
				vBoxContainer.GetNode<VBoxContainer>("%ConfigHost").AddChild(CreateCollectableConfigPanel(collectable), forceReadableName: false, InternalMode.Disabled);
				AddSummaryRows(collectable);
			}
		}
	}

	private void BindCollectableTopLevelFields(VBoxContainer root, CollectableConfig collectable)
	{
		_collectablePropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCollectableTopLevelPropertyEdited);
		_collectablePropertyBinding.BindText(root.GetNode<LineEdit>("%ResourceNameEdit"), collectable, "resource_name", UpdateCollectablePreview, this, "RefreshCollectableEditorFromHistory");
		_collectablePropertyBinding.BindText(root.GetNode<LineEdit>("%SaveKeyLineEdit"), collectable, "saveKey", UpdateCollectablePreview, this, "RefreshCollectableEditorFromHistory");
		_collectablePropertyBinding.BindToggle(root.GetNode<CheckButton>("%LocalToSceneCheck"), collectable, "resource_local_to_scene", UpdateCollectablePreview, this, "RefreshCollectableEditorFromHistory");
		MountCollectableConfigPicker(root.GetNode<HBoxContainer>("%ConfigPickerHost"), collectable);
		BindCollectableCommonConfigFields(root.GetNode<VBoxContainer>("%CommonConfigHost"), collectable);
	}

	private void MountCollectableConfigPicker(HBoxContainer host, CollectableConfig collectable)
	{
		if (GodotObject.IsInstanceValid(host))
		{
			XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
			xWResourcePicker.Name = "ConfigPicker";
			xWResourcePicker.Setup("Resource");
			xWResourcePicker.SetEditedResource(collectable.config);
			xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			xWResourcePicker.ResourceChanged += delegate(Resource resource)
			{
				SetCollectableConfig(resource, "更换收集物子配置");
			};
			xWResourcePicker.ResourceSelected += OpenCollectableConfigResource;
			host.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BindCollectableCommonConfigFields(VBoxContainer host, CollectableConfig collectable)
	{
		if (GodotObject.IsInstanceValid(host))
		{
			host.AddChild(AddResourcePicker("ScenePicker", "游戏对象场景", "PackedScene", ReadResource(collectable.config, "scene"), delegate(Resource resource)
			{
				SetConfigProperty("scene", Variant.From(in resource), refreshPreview: true);
			}), forceReadableName: false, InternalMode.Disabled);
			host.AddChild(AddResourcePicker("TexturePicker", "画面图标", "Texture2D", ReadResource(collectable.config, "texture", "icon", "previewTexture"), delegate(Resource resource)
			{
				SetConfigProperty("texture", Variant.From(in resource), refreshPreview: true);
			}), forceReadableName: false, InternalMode.Disabled);
			host.AddChild(AddLineEdit("AudioLineEdit", "拾取音效", ReadString(collectable.config, "audio"), delegate(string text)
			{
				SetConfigProperty("audio", Variant.From(in text), refreshPreview: false);
			}), forceReadableName: false, InternalMode.Disabled);
			host.AddChild(AddSpinBox("ValueSpinBox", "数值", ReadDouble(collectable.config, "value", 0.0), -999999.0, 999999.0, 1.0, delegate(double value)
			{
				SetConfigProperty("value", Variant.From(in value), refreshPreview: false);
			}), forceReadableName: false, InternalMode.Disabled);
			host.AddChild(AddSpinBox("AmountSpinBox", "数量", ReadDouble(collectable.config, "amount", 0.0), -999999.0, 999999.0, 1.0, delegate(double value)
			{
				SetConfigProperty("amount", Variant.From(in value), refreshPreview: false);
			}), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void OpenCollectableConfigResource(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource) && _editingCollectable != null)
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(resource, _editingCollectable, resource.ResourcePath, base.CurrentResourcePath, "config", -1, "collectable_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
			XWEditorInterface.Instance?.EditResource(resource, context);
		}
	}

	private void OnCollectableTopLevelPropertyEdited(bool committed)
	{
		if (base.CurrentResource is CollectableConfig collectableConfig && collectableConfig == _editingCollectable)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				ScheduleCollectableSave(collectableConfig);
			}
			else
			{
				MarkCurrentResourceDirty();
				collectableConfig.EmitChanged();
			}
		}
	}

	private void SetCollectableConfig(Resource config, string actionName)
	{
		if (_collectablePropertyBinding != null && base.CurrentResource is CollectableConfig collectableConfig && collectableConfig == _editingCollectable)
		{
			_collectablePropertyBinding.SetValue(collectableConfig, "config", config, actionName, this, "RefreshCollectableEditorFromHistory");
			RebuildCollectableEditor();
		}
	}

	public void RefreshCollectableEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is CollectableConfig editingCollectable)
		{
			_editingCollectable = editingCollectable;
			RebuildCollectableEditor();
		}
	}

	private void BindCollectablePreview(VBoxContainer root)
	{
		_texturePreview = root.GetNode<TextureRect>("%TexturePreview");
		_previewTitleLabel = root.GetNode<Label>("%PreviewTitleLabel");
		_previewConfigLabel = root.GetNode<Label>("%PreviewConfigLabel");
		_previewValueLabel = root.GetNode<Label>("%PreviewValueLabel");
		_previewAudioLabel = root.GetNode<Label>("%PreviewAudioLabel");
		_previewSceneLabel = root.GetNode<Label>("%PreviewSceneLabel");
		BindCollectableRuntimePreview(root.GetNode<Control>("%RuntimePreview"));
		UpdateCollectablePreview();
	}

	private void BindCollectableRuntimePreview(Control frame)
	{
		_collectableRuntimeStatusLabel = frame.GetNode<Label>("Layout/Toolbar/Status");
		_collectableRuntimeRunButton = frame.GetNode<Button>("Layout/Toolbar/RunButton");
		_collectablePreviewViewport = frame.GetNode<SubViewport>("Layout/ViewportContainer/Viewport");
		_collectablePreviewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(IsVisibleInTree() ? 4 : 0);
		_collectablePreviewRoot = frame.GetNode<Node2D>("Layout/ViewportContainer/Viewport/PreviewRoot");
		frame.GetNode<Button>("Layout/Toolbar/PickupButton").Pressed += SimulateCollectablePickup;
		frame.GetNode<Button>("Layout/Toolbar/ReloadButton").Pressed += RebuildCollectableRuntimePreview;
		_collectableRuntimeRunButton.Pressed += delegate
		{
			SetCollectableRuntimeRunning(!_collectableRuntimeRunning);
		};
		RebuildCollectableRuntimePreview();
	}

	private void RebuildCollectableRuntimePreview()
	{
		if (!GodotObject.IsInstanceValid(_collectablePreviewRoot) || _editingCollectable == null)
		{
			return;
		}
		foreach (Node child in _collectablePreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		Resource config = _editingCollectable.config;
		PackedScene packedScene = ReadResource(config, "scene", "collectableScene", "packedScene") as PackedScene;
		if (packedScene == null)
		{
			packedScene = ResolveCollectablePoolScene(config);
		}
		Node node = null;
		if (GodotObject.IsInstanceValid(packedScene))
		{
			try
			{
				node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				node.ProcessMode = (ProcessModeEnum)(_collectableRuntimeRunning ? 0 : 4);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Collectable editor scene preview failed: " + ex.Message);
			}
		}
		if (node != null)
		{
			node.Name = "ConfiguredCollectableScenePreview";
			_collectablePreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
		else if (config is AwardSettlementConfig awardSettlementConfig)
		{
			AddCollectableTextureLayer(awardSettlementConfig.background, "AwardBackground", 330f, 160f, Vector2.Zero);
			AddCollectableTextureLayer(awardSettlementConfig.image, "AwardImage", 190f, 125f, Vector2.Zero);
			AddCollectableTextureLayer(awardSettlementConfig.texture, "AwardPickupIcon", 58f, 58f, new Vector2(-128f, 52f));
		}
		else
		{
			Texture2D texture = TryReadTexture(config, "texture", "icon", "previewTexture", "image");
			AddCollectableTextureLayer(texture, "CollectableTextureFallback", 150f, 140f, Vector2.Zero);
		}
		SetCollectableRuntimeRunning(_collectableRuntimeRunning);
		UpdateCollectableRuntimeStatus();
	}

	private void AddCollectableTextureLayer(Texture2D texture, string nodeName, float maxWidth, float maxHeight, Vector2 offset)
	{
		if (GodotObject.IsInstanceValid(texture) && GodotObject.IsInstanceValid(_collectablePreviewRoot))
		{
			Vector2 size = texture.GetSize();
			float a = ((size.X > 0f && size.Y > 0f) ? Mathf.Min(maxWidth / size.X, maxHeight / size.Y) : 1f);
			_collectablePreviewRoot.AddChild(new Sprite2D
			{
				Name = nodeName,
				Texture = texture,
				Position = offset,
				Scale = Vector2.One * Mathf.Min(a, 1f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SimulateCollectablePickup()
	{
		if (!GodotObject.IsInstanceValid(_collectablePreviewRoot))
		{
			return;
		}
		UpdateCollectableRuntimeStatus("拾取中");
		Tween tween = _collectablePreviewRoot.CreateTween();
		tween.SetParallel();
		tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
		tween.TweenProperty(_collectablePreviewRoot, "position", new Vector2(54f, 34f), 0.48);
		tween.TweenProperty(_collectablePreviewRoot, "scale", Vector2.One * 0.18f, 0.48);
		tween.SetParallel(parallel: false);
		tween.TweenCallback(Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(_collectablePreviewRoot))
			{
				_collectablePreviewRoot.Position = new Vector2(270f, 205f);
				_collectablePreviewRoot.Scale = Vector2.One;
				RebuildCollectableRuntimePreview();
				UpdateCollectableRuntimeStatus("已拾取并重生");
			}
		}));
	}

	private PackedScene ResolveCollectablePoolScene(Resource config)
	{
		return ResolveCollectableKind(config) switch
		{
			CollectableKind.Sun => ObjectManager.GetPoolScene(ObjectManagerConfig.OBJECT.SUN), 
			CollectableKind.Coin => ObjectManager.GetPoolScene(ObjectManagerConfig.OBJECT.COIN_GOLD), 
			_ => null, 
		};
	}

	private void SetCollectableRuntimeRunning(bool running)
	{
		_collectableRuntimeRunning = running;
		if (GodotObject.IsInstanceValid(_collectablePreviewRoot))
		{
			foreach (Node child in _collectablePreviewRoot.GetChildren())
			{
				child.ProcessMode = (ProcessModeEnum)(running ? 0 : 4);
			}
		}
		if (GodotObject.IsInstanceValid(_collectableRuntimeRunButton))
		{
			_collectableRuntimeRunButton.Text = (running ? "暂停场景" : "运行场景");
		}
	}

	private void UpdateCollectableRuntimeStatus(string prefix = "预览")
	{
		if (GodotObject.IsInstanceValid(_collectableRuntimeStatusLabel) && _editingCollectable != null)
		{
			Resource config = _editingCollectable.config;
			string collectableKindLabel = GetCollectableKindLabel(ResolveCollectableKind(config));
			string value = BuildValueText(config);
			string value2 = EmptyToPlaceholder(ReadString(config, "audio"));
			_collectableRuntimeStatusLabel.Text = $"{prefix}: {collectableKindLabel} · 数值 {value} · 音效 {value2}";
		}
	}

	private void BindCollectableKindPanel(VBoxContainer root)
	{
		_collectableKindCards = root.GetNode<HFlowContainer>("%KindCards");
		BuildCollectableKindCards(ResolveCollectableKind(_editingCollectable?.config));
		_kindSummaryLabel = root.GetNode<Label>("%KindSummaryLabel");
		_kindSummaryLabel.Text = BuildCollectableKindSummary(_editingCollectable?.config);
	}

	private void BuildCollectableKindCards(CollectableKind selectedKind)
	{
		if (!GodotObject.IsInstanceValid(_collectableKindCards))
		{
			return;
		}
		foreach (Node child in _collectableKindCards.GetChildren())
		{
			_collectableKindCards.RemoveChild(child);
			child.QueueFree();
		}
		(string, string, string)[] array = new(string, string, string)[5]
		{
			("铲子奖励", "关卡工具", "res://addons/ModEditor/Icons/ResourceShovel.svg"),
			("图鉴纸条", "解锁图鉴", "res://addons/ModEditor/Icons/FileThumbnail.svg"),
			("阳光奖励", "战斗资源", "res://addons/ModEditor/Icons/ResourceCollectable.svg"),
			("金币奖励", "账户货币", "res://addons/ModEditor/Icons/Favorites.svg"),
			("自定义资源", "保留 Mod 配置", "res://addons/ModEditor/Icons/AssetLib.svg")
		};
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		for (int i = 0; i < array.Length; i++)
		{
			CollectableKind kind = (CollectableKind)i;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				continue;
			}
			xWGameVisualChoiceCard.CustomMinimumSize = new Vector2(150f, 64f);
			xWGameVisualChoiceCard.Configure(i.ToString(), array[i].Item1, array[i].Item2, ResourceLoader.Load<Texture2D>(array[i].Item3, null, ResourceLoader.CacheMode.Reuse), kind == selectedKind);
			xWGameVisualChoiceCard.Pressed += delegate
			{
				if (!_updatingControls)
				{
					SetCollectableKind(kind);
				}
			};
			_collectableKindCards.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SetCollectableKind(CollectableKind kind)
	{
		if (_editingCollectable != null)
		{
			Resource resource = EnsureCollectableConfigForKind(kind);
			if (GodotObject.IsInstanceValid(resource))
			{
				resource.SetMeta("mod_collectable_kind", kind.ToString());
				SetCollectableConfig(resource, "更换收集物类型");
			}
		}
	}

	private Resource EnsureCollectableConfigForKind(CollectableKind kind)
	{
		Resource resource = _editingCollectable?.config;
		bool flag;
		switch (kind)
		{
		case CollectableKind.Shovel:
			if (resource is ShovelConfig)
			{
				return resource;
			}
			return CreateShovelCollectableConfig();
		case CollectableKind.AwardNote:
		case CollectableKind.Sun:
		case CollectableKind.Coin:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			if (resource is AwardSettlementConfig)
			{
				return resource;
			}
			return CreateAwardSettlementConfig(kind);
		}
		if (!GodotObject.IsInstanceValid(resource))
		{
			return new Resource
			{
				ResourceName = BuildDefaultConfigName()
			};
		}
		return resource;
	}

	private AwardSettlementConfig CreateAwardSettlementConfig(CollectableKind kind)
	{
		AwardSettlementConfig awardSettlementConfig = new AwardSettlementConfig();
		awardSettlementConfig.ResourceName = BuildDefaultConfigName();
		awardSettlementConfig.SetMeta("mod_collectable_kind", kind.ToString());
		return awardSettlementConfig;
	}

	private ShovelConfig CreateShovelCollectableConfig()
	{
		string text = EmptyToPlaceholder(_editingCollectable?.saveKey).Replace(" ", "_");
		return new ShovelConfig
		{
			ResourceName = BuildDefaultConfigName(),
			saveKey = text,
			name = text + "_NAME",
			describe = text + "_DESC",
			handbookDescribe = text + "_HANDBOOK_DESC",
			handbookStory = text + "_HANDBOOK_STORY"
		};
	}

	private void RebuildCollectableEditor()
	{
		if (base.CanvasGrid == null || _editingCollectable == null)
		{
			return;
		}
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderCollectableEditor(_editingCollectable);
	}

	private Control CreateCollectableConfigPanel(CollectableConfig collectable)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "CollectableConfigPanel",
			CustomMinimumSize = new Vector2(360f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		vBoxContainer.AddChild(CreateSectionTitle("配置摘要"), forceReadableName: false, InternalMode.Disabled);
		CollectableKind kind = ResolveCollectableKind(collectable?.config);
		if (collectable?.config is ShovelConfig config)
		{
			vBoxContainer.AddChild(CreateShovelPreviewPanel(config), forceReadableName: false, InternalMode.Disabled);
		}
		else if (collectable?.config is AwardSettlementConfig config2)
		{
			vBoxContainer.AddChild(CreateAwardPreviewPanel(config2, kind), forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			vBoxContainer.AddChild(new Label
			{
				Text = "选择收集物类型后会在这里显示对应的可视化配置。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
		}
		_configPropertyList = new ItemList
		{
			Name = "CollectableConfigPropertyList",
			CustomMinimumSize = new Vector2(320f, 260f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(_configPropertyList, forceReadableName: false, InternalMode.Disabled);
		RefreshConfigPanel(collectable);
		return vBoxContainer;
	}

	private Control CreateAwardPreviewPanel(AwardSettlementConfig config, CollectableKind kind)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.Name = "AwardPreviewPanel";
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		vBoxContainer.AddChild(CreateSectionTitle(GetCollectableKindLabel(kind) + "配置"), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddResourcePicker("TexturePicker", "拾取图标", "Texture2D", config.texture, delegate(Resource resource)
		{
			SetConfigProperty("texture", Variant.From(in resource), refreshPreview: true);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddResourcePicker("BackgroundPicker", "结算背景", "Texture2D", config.background, delegate(Resource resource)
		{
			SetConfigProperty("background", Variant.From(in resource), refreshPreview: true);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddResourcePicker("ImagePicker", "展示图片", "Texture2D", config.image, delegate(Resource resource)
		{
			SetConfigProperty("image", Variant.From(in resource), refreshPreview: true);
		}), forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private Control CreateShovelPreviewPanel(ShovelConfig config)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.Name = "ShovelPreviewPanel";
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		vBoxContainer.AddChild(CreateSectionTitle("铲子奖励配置"), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddResourcePicker("TexturePicker", "图标", "Texture2D", config.texture, delegate(Resource resource)
		{
			SetConfigProperty("texture", Variant.From(in resource), refreshPreview: true);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddLineEdit("NameLineEdit", "名称文本 Key", config.name, delegate(string text)
		{
			SetConfigProperty("name", Variant.From(in text), refreshPreview: true);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddTextEdit("DescribeTextEdit", "描述文本 Key", config.describe, delegate(string text)
		{
			SetConfigProperty("describe", Variant.From(in text), refreshPreview: false);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddTextEdit("HandbookDescribeTextEdit", "图鉴描述 Key", config.handbookDescribe, delegate(string text)
		{
			SetConfigProperty("handbookDescribe", Variant.From(in text), refreshPreview: false);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(AddTextEdit("HandbookStoryTextEdit", "图鉴故事 Key", config.handbookStory, delegate(string text)
		{
			SetConfigProperty("handbookStory", Variant.From(in text), refreshPreview: false);
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateShovelListPanel(config), forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private Control CreateShovelListPanel(ShovelConfig config)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.Name = "ShovelListPanel";
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		_unlockConditionList = CreateArraySummaryList("UnlockConditionList", "解锁条件");
		_eventList = CreateArraySummaryList("EventList", "事件");
		_shovelableNameList = CreateArraySummaryList("ShovelableNameList", "可铲对象");
		hBoxContainer.AddChild(WrapList("解锁条件", _unlockConditionList), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(WrapList("事件", _eventList), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateShovelableNamePanel(config), forceReadableName: false, InternalMode.Disabled);
		RefreshShovelListPanel(config);
		return hBoxContainer;
	}

	private Control CreateShovelableNamePanel(ShovelConfig config)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.Name = "ShovelableNamePanel";
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.AddChild(new Label
		{
			Text = "可铲对象",
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(_shovelableNameList, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddThemeConstantOverride("separation", 4);
		LineEdit edit = new LineEdit
		{
			Name = "ShovelableNameLineEdit",
			PlaceholderText = "角色 key",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Text = "添加"
		};
		button.Pressed += delegate
		{
			AddShovelableName(edit.Text);
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Text = "删除"
		};
		button2.Pressed += RemoveSelectedShovelableName;
		hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private void SetConfigProperty(string propertyName, Variant value, bool refreshPreview)
	{
		if (!_updatingControls && _editingCollectable?.config != null && !string.IsNullOrWhiteSpace(propertyName))
		{
			if (HasResourceProperty(_editingCollectable.config, propertyName))
			{
				_editingCollectable.config.Set(propertyName, value);
			}
			else
			{
				_editingCollectable.config.SetMeta("mod_" + propertyName, value);
			}
			_editingCollectable.config.EmitChanged();
			ScheduleCollectableSave(_editingCollectable);
			if (refreshPreview)
			{
				UpdateCollectablePreview();
				RebuildCollectableRuntimePreview();
			}
			else
			{
				UpdateCollectableRuntimeStatus();
			}
			RefreshKindPanel();
			RefreshConfigPanel();
		}
	}

	private void AddShovelableName(string name)
	{
		if (_editingCollectable?.config is ShovelConfig shovelConfig)
		{
			name = (name ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(name) && !shovelConfig.shovelableNames.Contains(name))
			{
				shovelConfig.shovelableNames.Add(name);
				shovelConfig.EmitChanged();
				SaveCollectableResource(_editingCollectable);
				RefreshShovelListPanel(shovelConfig);
				RefreshConfigPanel();
			}
		}
	}

	private void RemoveSelectedShovelableName()
	{
		if (_editingCollectable?.config is ShovelConfig shovelConfig && GodotObject.IsInstanceValid(_shovelableNameList))
		{
			int num = ((_shovelableNameList.GetSelectedItems().Length != 0) ? _shovelableNameList.GetSelectedItems()[0] : (-1));
			if (num >= 0 && num < shovelConfig.shovelableNames.Count)
			{
				shovelConfig.shovelableNames.RemoveAt(num);
				shovelConfig.EmitChanged();
				SaveCollectableResource(_editingCollectable);
				RefreshShovelListPanel(shovelConfig);
				RefreshConfigPanel();
			}
		}
	}

	private void SaveCollectableResource(CollectableConfig collectable)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(collectable))
		{
			return;
		}
		Error error = ResourceSaver.Save(collectable, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Collectable editor save failed: {error} {currentResourcePath}");
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

	private void ScheduleCollectableSave(CollectableConfig collectable)
	{
		if (GodotObject.IsInstanceValid(collectable))
		{
			_pendingCollectableSaveMessage = base.CurrentResourcePath;
			if (GodotObject.IsInstanceValid(_collectableSaveTimer))
			{
				_collectableSaveTimer.Start();
			}
			else
			{
				SavePendingCollectable();
			}
		}
	}

	private void SavePendingCollectable()
	{
		if (!string.IsNullOrWhiteSpace(_pendingCollectableSaveMessage))
		{
			_pendingCollectableSaveMessage = null;
			if (_editingCollectable != null)
			{
				SaveCollectableResource(_editingCollectable);
			}
		}
	}

	private void UpdateCollectablePreview()
	{
		if (_editingCollectable == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_previewTitleLabel))
			{
				_previewTitleLabel.Text = "收集物: " + EmptyToPlaceholder(_editingCollectable.saveKey);
			}
			if (GodotObject.IsInstanceValid(_previewConfigLabel))
			{
				_previewConfigLabel.Text = "配置: " + FormatResource(_editingCollectable.config);
			}
			if (GodotObject.IsInstanceValid(_previewValueLabel))
			{
				_previewValueLabel.Text = "value / amount: " + BuildValueText(_editingCollectable.config);
			}
			if (GodotObject.IsInstanceValid(_previewAudioLabel))
			{
				_previewAudioLabel.Text = "audio: " + EmptyToPlaceholder(ReadString(_editingCollectable.config, "audio"));
			}
			if (GodotObject.IsInstanceValid(_previewSceneLabel))
			{
				_previewSceneLabel.Text = "scene: " + ReadKnown(_editingCollectable.config, "scene", "collectableScene", "packedScene");
			}
			if (GodotObject.IsInstanceValid(_texturePreview))
			{
				_texturePreview.Texture = TryReadTexture(_editingCollectable.config, "texture", "icon", "previewTexture", "image");
			}
			UpdateCollectableRuntimeStatus();
			RefreshKindPanel();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshKindPanel()
	{
		CollectableKind kind = ResolveCollectableKind(_editingCollectable?.config);
		SelectCollectableKindCard(kind);
		if (GodotObject.IsInstanceValid(_kindSummaryLabel))
		{
			_kindSummaryLabel.Text = BuildCollectableKindSummary(_editingCollectable?.config);
		}
	}

	private void RefreshConfigPanel()
	{
		if (_editingCollectable != null)
		{
			RefreshConfigPanel(_editingCollectable);
		}
	}

	private void RefreshConfigPanel(CollectableConfig collectable)
	{
		if (!GodotObject.IsInstanceValid(_configPropertyList))
		{
			return;
		}
		_configPropertyList.Clear();
		Resource resource = collectable?.config;
		if (!GodotObject.IsInstanceValid(resource))
		{
			_configPropertyList.AddItem("未配置 config");
			return;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (!property.ContainsKey("name"))
			{
				continue;
			}
			string text = property["name"].AsString();
			if (!text.StartsWith("Resource/") && !text.StartsWith("resource_") && !(text == "script"))
			{
				_configPropertyList.AddItem(text + ": " + FormatVariant(ReadVariant(resource, text)));
				if (_configPropertyList.ItemCount >= 40)
				{
					break;
				}
			}
		}
	}

	private void RefreshShovelListPanel(ShovelConfig config)
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return;
		}
		PopulateArraySummary(_unlockConditionList, config.unlockCheckList);
		PopulateArraySummary(_eventList, config.eventList);
		if (!GodotObject.IsInstanceValid(_shovelableNameList))
		{
			return;
		}
		_shovelableNameList.Clear();
		if (config.shovelableNames == null || config.shovelableNames.Count == 0)
		{
			_shovelableNameList.AddItem("未配置");
			return;
		}
		for (int i = 0; i < config.shovelableNames.Count; i++)
		{
			_shovelableNameList.AddItem(config.shovelableNames[i]);
		}
	}

	private void AddSummaryRows(CollectableConfig collectable)
	{
		AddItemIfMissing(base.PreviewList, "收集物 saveKey -> " + EmptyToPlaceholder(collectable.saveKey));
		AddItemIfMissing(base.PreviewList, "config -> " + FormatResource(collectable.config));
		AddItemIfMissing(base.PreviewList, "value / amount -> " + BuildValueText(collectable.config));
		AddItemIfMissing(base.TimelineList, "收集物资源 -> " + base.CurrentResourcePath);
		AddItemIfMissing(base.GraphList, "收集物 -> config -> scene / texture / icon / audio / value / amount");
		AddItemIfMissing(base.ReferenceList, "config -> " + FormatResource(collectable.config));
		AddItemIfMissing(base.ReferenceList, "scene -> " + ReadKnown(collectable.config, "scene", "collectableScene", "packedScene"));
		AddItemIfMissing(base.ReferenceList, "texture -> " + FormatResource(TryReadTexture(collectable.config, "texture", "icon", "previewTexture", "image")));
		AddItemIfMissing(base.ReferenceList, "audio -> " + ReadString(collectable.config, "audio"));
	}

	private Control AddLineEdit(string nodeName, string label, string value, Action<string> changed)
	{
		LineEdit lineEdit = new LineEdit
		{
			Name = nodeName,
			Text = (value ?? ""),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		lineEdit.TextChanged += delegate(string text)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(text);
			}
		};
		return WrapField(label, lineEdit);
	}

	private Control AddTextEdit(string nodeName, string label, string value, Action<string> changed)
	{
		TextEdit edit = new TextEdit
		{
			Name = nodeName,
			Text = (value ?? ""),
			CustomMinimumSize = new Vector2(0f, 62f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		edit.TextChanged += delegate
		{
			if (!_updatingControls)
			{
				changed?.Invoke(edit.Text);
			}
		};
		return WrapField(label, edit);
	}

	private Control AddSpinBox(string nodeName, string label, double value, double min, double max, double step, Action<double> changed)
	{
		SpinBox spinBox = new SpinBox
		{
			Name = nodeName,
			MinValue = min,
			MaxValue = max,
			Step = step,
			Value = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(96f, 0f)
		};
		spinBox.ValueChanged += delegate(double newValue)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(newValue);
			}
		};
		return WrapField(label, spinBox);
	}

	private Control AddResourcePicker(string nodeName, string label, string baseType, Resource value, Action<Resource> changed)
	{
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = nodeName;
		xWResourcePicker.Setup(baseType);
		xWResourcePicker.SetEditedResource(value);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		xWResourcePicker.ResourceChanged += delegate(Resource resource)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(resource);
			}
		};
		return WrapField(label, xWResourcePicker);
	}

	private static Label CreateSectionTitle(string text)
	{
		return new Label
		{
			Text = text,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static Control WrapField(string label, Control editor)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(112f, 0f),
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static ItemList CreateArraySummaryList(string nodeName, string tooltip)
	{
		return new ItemList
		{
			Name = nodeName,
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2(160f, 120f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
	}

	private static Control WrapList(string title, ItemList list)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		vBoxContainer.AddChild(new Label
		{
			Text = title,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(list, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private static void PopulateArraySummary<[MustBeVariant] T>(ItemList list, Array<T> array)
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

	private void SelectCollectableKindCard(CollectableKind kind)
	{
		if (!GodotObject.IsInstanceValid(_collectableKindCards))
		{
			return;
		}
		foreach (Node child in _collectableKindCards.GetChildren())
		{
			if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard)
			{
				string choiceKey = xWGameVisualChoiceCard.ChoiceKey;
				int num = (int)kind;
				xWGameVisualChoiceCard.SetSelected(choiceKey == num.ToString());
			}
		}
	}

	private static Texture2D TryReadTexture(Resource resource, params string[] propertyNames)
	{
		foreach (string text in propertyNames)
		{
			if (ReadResource(resource, text) is Texture2D result)
			{
				return result;
			}
		}
		return null;
	}

	private static CollectableKind ResolveCollectableKind(Resource config)
	{
		if (config is ShovelConfig)
		{
			return CollectableKind.Shovel;
		}
		if (config is AwardSettlementConfig)
		{
			CollectableKind result;
			bool flag = Enum.TryParse<CollectableKind>(config.HasMeta("mod_collectable_kind") ? config.GetMeta("mod_collectable_kind").AsString() : "", out result);
			if (flag)
			{
				bool flag2 = (uint)(result - 2) <= 1u;
				flag = flag2;
			}
			if (flag)
			{
				return result;
			}
			return CollectableKind.AwardNote;
		}
		if (!GodotObject.IsInstanceValid(config))
		{
			return CollectableKind.AwardNote;
		}
		return CollectableKind.Custom;
	}

	private static string GetCollectableKindLabel(CollectableKind kind)
	{
		return kind switch
		{
			CollectableKind.Shovel => "铲子奖励", 
			CollectableKind.AwardNote => "图鉴纸条", 
			CollectableKind.Sun => "阳光奖励", 
			CollectableKind.Coin => "金币奖励", 
			_ => "自定义资源", 
		};
	}

	private static string BuildCollectableKindSummary(Resource config)
	{
		CollectableKind kind = ResolveCollectableKind(config);
		return string.Concat(str2: FormatResource(config), str0: GetCollectableKindLabel(kind), str1: " / ");
	}

	private string BuildDefaultConfigName()
	{
		string text = _editingCollectable?.saveKey;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = _editingCollectable?.ResourceName;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NewCollectable";
		}
		return text + "Config";
	}

	private static Resource ReadResource(Resource resource, params string[] propertyNames)
	{
		foreach (string propertyName in propertyNames)
		{
			Variant variant = ReadVariant(resource, propertyName);
			if (variant.VariantType == Variant.Type.Object && variant.AsGodotObject() is Resource result)
			{
				return result;
			}
		}
		return null;
	}

	private static string ReadKnown(Resource resource, params string[] propertyNames)
	{
		foreach (string propertyName in propertyNames)
		{
			Variant value = ReadVariant(resource, propertyName);
			if (value.VariantType != Variant.Type.Nil)
			{
				return FormatVariant(value);
			}
		}
		return "未配置";
	}

	private static string ReadString(Resource resource, string propertyName)
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType != Variant.Type.Nil)
		{
			return variant.AsString();
		}
		return "";
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
			string text = "mod_" + propertyName;
			if (resource.HasMeta(text))
			{
				return resource.GetMeta(text);
			}
		}
		catch
		{
		}
		return default(Variant);
	}

	private static bool HasResourceProperty(Resource resource, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(resource))
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

	private static string BuildValueText(Resource config)
	{
		string text = ReadKnown(config, "value");
		string text2 = ReadKnown(config, "amount");
		return text + " / " + text2;
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
			goto IL_0161;
		}
		switch ((int)num)
		{
		case 0:
		case 1:
			break;
		case 3:
			return FormatResource(value.AsGodotObject() as Resource);
		case 7:
			return $"Array({value.AsGodotArray().Count})";
		case 6:
			return $"Dictionary({value.AsGodotDictionary().Count})";
		default:
			goto IL_0161;
		}
		goto IL_00bb;
		IL_0161:
		return value.ToString();
		IL_00bb:
		return EmptyToPlaceholder(value.AsString());
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

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}
}
