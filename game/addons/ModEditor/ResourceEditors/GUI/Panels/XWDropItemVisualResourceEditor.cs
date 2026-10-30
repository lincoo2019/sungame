using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWDropItemVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum PreviewState
	{
		Idle,
		Dropping,
		Picking
	}

	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWDropItemVisualEditorLayout.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const string ObjectPoolVisualPickerScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWObjectPoolVisualPicker.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private static PackedScene _visualChoiceCardScene;

	private static PackedScene _objectPoolVisualPickerScene;

	private DropItemConfig _editingConfig;

	private DropItemHandler _editingHandler;

	private XWVisualPropertyBinding _propertyBinding;

	private XWResourcePicker _scenePicker;

	private XWResourcePicker _handlerPicker;

	private XWGameplayResourcePickerWindow _audioPicker;

	private string _audioTargetProperty = "FallAudio";

	private VBoxContainer _configFields;

	private VBoxContainer _handlerFields;

	private LineEdit _resourceNameEdit;

	private CheckButton _localToSceneCheck;

	private Label _title;

	private Label _resourceType;

	private Label _status;

	private Label _runtimeState;

	private Label _sceneStatus;

	private Label _valueLabel;

	private Label _missingScene;

	private Label _handlerTypeLabel;

	private Label _handlerEffectLabel;

	private Label _handlerOutcome;

	private LineEdit _nameEdit;

	private SpinBox _valueSpin;

	private SpinBox _poolSpin;

	private SpinBox _coinIdSpin;

	private HFlowContainer _categoryCards;

	private HFlowContainer _handlerCards;

	private XWObjectPoolVisualPicker _objectIdPicker;

	private LineEdit _fallAudioEdit;

	private LineEdit _pickAudioEdit;

	private AudioStreamPlayer _directAudioPlayer;

	private SpinBox _handlerSimulationCount;

	private Node2D _previewRoot;

	private Node2D _sceneHost;

	private TextureRect _fallbackDrop;

	private SubViewport _previewViewport;

	private readonly List<Node> _visualPreviewNodes = new List<Node>();

	private PreviewState _previewState;

	private double _previewElapsed;

	private Vector2 _pickStartPosition;

	private bool _updatingControls;

	public int DropCategoryVisualCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_categoryCards))
			{
				return 0;
			}
			return _categoryCards.GetChildCount();
		}
	}

	public int DropHandlerVisualCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_handlerCards))
			{
				return 0;
			}
			return _handlerCards.GetChildCount();
		}
	}

	public bool HasObjectLibrarySearchAndPaging
	{
		get
		{
			if (GodotObject.IsInstanceValid(_objectIdPicker))
			{
				return _objectIdPicker.HasSearchAndPaging;
			}
			return false;
		}
	}

	public bool IsDropItemPreviewRendering
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
		SetProcess(enable: false);
		base._Ready();
	}

	public override void _ExitTree()
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_audioPicker?.Dismiss();
		_editingConfig = null;
		_editingHandler = null;
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		if (GodotObject.IsInstanceValid(_previewViewport))
		{
			_previewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(visible ? 4 : 0);
		}
		UpdatePreviewProcessingVisibility();
	}

	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_previewRoot))
		{
			SetProcess(enable: false);
			return;
		}
		_previewElapsed += delta;
		switch (_previewState)
		{
		case PreviewState.Dropping:
			UpdateDropPreview();
			break;
		case PreviewState.Picking:
			UpdatePickPreview();
			break;
		default:
			SetProcess(enable: false);
			break;
		}
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingConfig = null;
		_editingHandler = null;
		if ((!(base.CurrentResource is DropItemConfig) && !(base.CurrentResource is DropItemHandler)) || base.CanvasGrid == null)
		{
			return;
		}
		_editingConfig = base.CurrentResource as DropItemConfig;
		_editingHandler = base.CurrentResource as DropItemHandler;
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnDropItemPropertyEdited);
		base.CanvasGrid.Columns = 1;
		if (_visualEditorLayoutScene == null)
		{
			_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWDropItemVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			BindWorkbench(vBoxContainer);
			BindResourceMetadata(base.CurrentResource);
			if (_editingConfig != null)
			{
				RenderConfigWorkbench();
			}
			else
			{
				RenderHandlerWorkbench();
			}
			AddSummaryRows();
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteDropItemVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteDropItemVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(DropItemConfig)) && !(type == typeof(DropItemHandler)) && !(type == typeof(SunDropItemHandler)) && !(type == typeof(CoinDropItemHandler)) && !(type == typeof(LuckyBagDropItemHandler)) && !(type == typeof(JalapenoSunDropItemHandler)) && !(type == typeof(BrainSunDropItemHandler)))
		{
			return type == typeof(GoldShardDropItemHandler);
		}
		return true;
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_resourceNameEdit = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToSceneCheck = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_configFields = root.GetNode<VBoxContainer>("%ConfigFields");
		_handlerFields = root.GetNode<VBoxContainer>("%HandlerFields");
		_title = root.GetNode<Label>("%Title");
		_resourceType = root.GetNode<Label>("%ResourceType");
		_status = root.GetNode<Label>("%Status");
		_runtimeState = root.GetNode<Label>("%RuntimeState");
		_sceneStatus = root.GetNode<Label>("%SceneStatus");
		_valueLabel = root.GetNode<Label>("%ValueLabel");
		_missingScene = root.GetNode<Label>("%MissingScene");
		_handlerTypeLabel = root.GetNode<Label>("%HandlerTypeLabel");
		_handlerEffectLabel = root.GetNode<Label>("%HandlerEffectLabel");
		_handlerOutcome = root.GetNode<Label>("%HandlerOutcome");
		_nameEdit = root.GetNode<LineEdit>("%NameEdit");
		_valueSpin = root.GetNode<SpinBox>("%ValueSpin");
		_poolSpin = root.GetNode<SpinBox>("%PoolSpin");
		_coinIdSpin = root.GetNode<SpinBox>("%CoinIdSpin");
		_categoryCards = root.GetNode<HFlowContainer>("%CategoryCards");
		_handlerCards = root.GetNode<HFlowContainer>("%HandlerCards");
		_fallAudioEdit = root.GetNode<LineEdit>("%FallAudioEdit");
		_pickAudioEdit = root.GetNode<LineEdit>("%PickAudioEdit");
		_directAudioPlayer = root.GetNode<AudioStreamPlayer>("%DirectAudioPlayer");
		_handlerSimulationCount = root.GetNode<SpinBox>("%HandlerSimulationCount");
		_previewRoot = root.GetNode<Node2D>("%PreviewRoot");
		_sceneHost = root.GetNode<Node2D>("%SceneHost");
		_fallbackDrop = root.GetNode<TextureRect>("%FallbackDrop");
		_previewViewport = root.GetNode<SubViewport>("%Viewport");
		_previewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(IsVisibleInTree() ? 4 : 0);
		root.GetNode<Button>("%DropButton").Pressed += StartDropPreview;
		root.GetNode<Button>("%PickButton").Pressed += StartPickPreview;
		root.GetNode<Button>("%ReloadButton").Pressed += RebuildDropItemScenePreview;
		root.GetNode<Button>("%PlayFallAudio").Pressed += delegate
		{
			PlayConfiguredAudio(_editingConfig?.FallAudio, "下落");
		};
		root.GetNode<Button>("%PlayPickAudio").Pressed += delegate
		{
			PlayConfiguredAudio(_editingConfig?.PickAudio, "拾取");
		};
		root.GetNode<Button>("%ChooseFallAudioButton").Pressed += delegate
		{
			ShowAudioSelector("FallAudio");
		};
		root.GetNode<Button>("%ChoosePickAudioButton").Pressed += delegate
		{
			ShowAudioSelector("PickAudio");
		};
		root.GetNode<Button>("%ClearFallAudioButton").Pressed += delegate
		{
			SetAudioKey("FallAudio", "", "清除掉落物下落音效");
		};
		root.GetNode<Button>("%ClearPickAudioButton").Pressed += delegate
		{
			SetAudioKey("PickAudio", "", "清除掉落物拾取音效");
		};
		_scenePicker = XWResourcePicker.Create();
		_scenePicker.Setup("PackedScene");
		_scenePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		root.GetNode<HBoxContainer>("%ScenePickerHost").AddChild(_scenePicker, forceReadableName: false, InternalMode.Disabled);
		_scenePicker.ResourceChanged += OnScenePicked;
		_handlerPicker = XWResourcePicker.Create();
		_handlerPicker.Setup("DropItemHandler");
		_handlerPicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		root.GetNode<HBoxContainer>("%HandlerPickerHost").AddChild(_handlerPicker, forceReadableName: false, InternalMode.Disabled);
		_handlerPicker.ResourceChanged += OnHandlerPicked;
		MountObjectIdLibrary(root.GetNode<VBoxContainer>("%ObjectLibraryHost"));
		BuildCategoryCards();
		BindConfigSignals();
		_handlerSimulationCount.ValueChanged += delegate
		{
			if (!_updatingControls)
			{
				UpdateHandlerPreview();
			}
		};
	}

	private void BindResourceMetadata(Resource resource)
	{
		_propertyBinding.BindText(_resourceNameEdit, resource, "resource_name", delegate
		{
			RefreshDropItemVisuals();
		}, this, "RefreshDropItemEditorFromHistory");
		_propertyBinding.BindToggle(_localToSceneCheck, resource, "resource_local_to_scene", delegate
		{
			RefreshDropItemVisuals();
		}, this, "RefreshDropItemEditorFromHistory");
	}

	private void BindConfigSignals()
	{
		DropItemConfig editingConfig = _editingConfig;
		if (editingConfig != null && _propertyBinding != null)
		{
			BindDropItemName(editingConfig);
			_propertyBinding.BindNumber(_valueSpin, editingConfig, "Value", delegate
			{
				RefreshDropItemVisuals();
			}, this, "RefreshDropItemEditorFromHistory");
			_propertyBinding.BindNumber(_poolSpin, editingConfig, "PoolMaxNum", delegate
			{
				RefreshDropItemVisuals();
			}, this, "RefreshDropItemEditorFromHistory");
			_propertyBinding.BindNumber(_coinIdSpin, editingConfig, "CoinObjectId", delegate
			{
				RefreshDropItemVisuals();
			}, this, "RefreshDropItemEditorFromHistory");
			_propertyBinding.BindText(_fallAudioEdit, editingConfig, "FallAudio", delegate
			{
				RefreshDropItemVisuals();
			}, this, "RefreshDropItemEditorFromHistory");
			_propertyBinding.BindText(_pickAudioEdit, editingConfig, "PickAudio", delegate
			{
				RefreshDropItemVisuals();
			}, this, "RefreshDropItemEditorFromHistory");
		}
	}

	private void MountObjectIdLibrary(VBoxContainer host)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		if (_objectPoolVisualPickerScene == null)
		{
			_objectPoolVisualPickerScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWObjectPoolVisualPicker.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_objectIdPicker = _objectPoolVisualPickerScene?.Instantiate<XWObjectPoolVisualPicker>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_objectIdPicker))
		{
			return;
		}
		_objectIdPicker.Name = "DropItemObjectLibrary";
		host.AddChild(_objectIdPicker, forceReadableName: false, InternalMode.Disabled);
		List<XWObjectPoolVisualChoice> list = new List<XWObjectPoolVisualChoice>();
		ObjectManagerConfig.OBJECT[] values = Enum.GetValues<ObjectManagerConfig.OBJECT>();
		for (int i = 0; i < values.Length; i++)
		{
			ObjectManagerConfig.OBJECT oBJECT = values[i];
			if (oBJECT != ObjectManagerConfig.OBJECT.MAX)
			{
				string objectVisualCategory = GetObjectVisualCategory(oBJECT);
				list.Add(new XWObjectPoolVisualChoice((int)oBJECT, oBJECT.ToString(), objectVisualCategory, LoadObjectVisualIcon(objectVisualCategory)));
			}
		}
		_objectIdPicker.Configure(list, (int)(_editingConfig?.Id ?? ObjectManagerConfig.OBJECT.NOONE));
		_objectIdPicker.ChoiceSelected += delegate(int id)
		{
			if (!_updatingControls && _editingConfig != null && _propertyBinding != null)
			{
				_propertyBinding.SetValue(_editingConfig, "Id", Variant.From<long>((long)id), "更换掉落物对象池 ID", this, "RefreshDropItemEditorFromHistory");
				RefreshDropItemVisuals();
			}
		};
	}

	private void BuildCategoryCards()
	{
		if (!GodotObject.IsInstanceValid(_categoryCards))
		{
			return;
		}
		ClearVisualCards(_categoryCards);
		TowerDefenseEnum.DROP_ITEM_CATEGORY[] values = Enum.GetValues<TowerDefenseEnum.DROP_ITEM_CATEGORY>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.DROP_ITEM_CATEGORY dROP_ITEM_CATEGORY = values[i];
			TowerDefenseEnum.DROP_ITEM_CATEGORY selectedCategory = dROP_ITEM_CATEGORY;
			string iconPath = dROP_ITEM_CATEGORY switch
			{
				TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN => "res://addons/ModEditor/Icons/ResourceCollectable.svg", 
				TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN => "res://addons/ModEditor/Icons/Favorites.svg", 
				_ => "res://addons/ModEditor/Icons/ResourceFallingObject.svg", 
			};
			int num = (int)dROP_ITEM_CATEGORY;
			string key = num.ToString();
			string dropCategoryLabel = GetDropCategoryLabel(dROP_ITEM_CATEGORY);
			string detail = dROP_ITEM_CATEGORY.ToString();
			DropItemConfig editingConfig = _editingConfig;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = CreateVisualCard(key, dropCategoryLabel, detail, iconPath, editingConfig != null && editingConfig.Category == dROP_ITEM_CATEGORY, new Vector2(138f, 62f));
			if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				continue;
			}
			xWGameVisualChoiceCard.Pressed += delegate
			{
				if (!_updatingControls && _editingConfig != null && _propertyBinding != null)
				{
					_propertyBinding.SetValue(_editingConfig, "Category", Variant.From<long>((long)selectedCategory), "更换掉落物分类", this, "RefreshDropItemEditorFromHistory");
					HFlowContainer categoryCards = _categoryCards;
					int num2 = (int)selectedCategory;
					SelectVisualCard(categoryCards, num2.ToString());
					RefreshDropItemVisuals();
				}
			};
			_categoryCards.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BuildHandlerCards()
	{
		if (!GodotObject.IsInstanceValid(_handlerCards))
		{
			return;
		}
		ClearVisualCards(_handlerCards);
		(string, string, string)[] array = new(string, string, string)[7]
		{
			("无处理器", "只拾取显示", "res://addons/ModEditor/Icons/Clear.svg"),
			("阳光资源", "增加阳光", "res://addons/ModEditor/Icons/ResourceCollectable.svg"),
			("金币", "增加货币", "res://addons/ModEditor/Icons/Favorites.svg"),
			("福袋里程碑", "累计奖励", "res://addons/ModEditor/Icons/Progress1.svg"),
			("火爆辣椒阳光", "特殊阳光", "res://addons/ModEditor/Icons/StatusError.svg"),
			("僵尸脑子阳光", "阵营阳光", "res://addons/ModEditor/Icons/ResourceCharacter.svg"),
			("金色碎片", "合成卡牌", "res://addons/ModEditor/Icons/ResourceCard.svg")
		};
		int builtInHandlerId = GetBuiltInHandlerId(_editingConfig?.Handler);
		for (int i = 0; i < array.Length; i++)
		{
			int handlerId = i;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = CreateVisualCard(i.ToString(), array[i].Item1, array[i].Item2, array[i].Item3, builtInHandlerId == i, new Vector2(154f, 64f));
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Pressed += delegate
				{
					ApplyBuiltInHandler(handlerId);
				};
				_handlerCards.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (builtInHandlerId < 0 && _editingConfig?.Handler != null)
		{
			XWGameVisualChoiceCard xWGameVisualChoiceCard2 = CreateVisualCard("100", "自定义处理器", _editingConfig.Handler.GetType().Name, "res://addons/ModEditor/Icons/Script.svg", selected: true, new Vector2(170f, 64f));
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard2))
			{
				xWGameVisualChoiceCard2.Disabled = true;
				_handlerCards.AddChild(xWGameVisualChoiceCard2, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ApplyBuiltInHandler(int handlerId)
	{
		if (!_updatingControls && _editingConfig != null && _propertyBinding != null)
		{
			DropItemHandler from = CreateBuiltInHandler(handlerId);
			_propertyBinding.SetValue(_editingConfig, "Handler", Variant.From(in from), "更换掉落物拾取处理器", this, "RefreshDropItemEditorFromHistory");
			_handlerPicker?.SetEditedResource(from);
			SelectVisualCard(_handlerCards, handlerId.ToString());
			RefreshDropItemVisuals();
		}
	}

	private XWGameVisualChoiceCard CreateVisualCard(string key, string title, string detail, string iconPath, bool selected, Vector2 minimumSize)
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

	private static void ClearVisualCards(Control host)
	{
		foreach (Node child in host.GetChildren())
		{
			host.RemoveChild(child);
			child.QueueFree();
		}
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

	private static string GetObjectVisualCategory(ObjectManagerConfig.OBJECT id)
	{
		string text = id.ToString();
		if (text.Contains("SUN", StringComparison.OrdinalIgnoreCase) || text == "GAMECOLLECT")
		{
			return "阳光";
		}
		if (text.Contains("COIN", StringComparison.OrdinalIgnoreCase))
		{
			return "货币";
		}
		if (text.Contains("PROJECTILE", StringComparison.OrdinalIgnoreCase) || text.Contains("damage", StringComparison.OrdinalIgnoreCase))
		{
			return "战斗";
		}
		if (text.Contains("PARTICLE", StringComparison.OrdinalIgnoreCase))
		{
			return "特效";
		}
		return "对象池";
	}

	private static Texture2D LoadObjectVisualIcon(string category)
	{
		return ResourceLoader.Load<Texture2D>(category switch
		{
			"阳光" => "res://addons/ModEditor/Icons/ResourceCollectable.svg", 
			"货币" => "res://addons/ModEditor/Icons/Favorites.svg", 
			"战斗" => "res://addons/ModEditor/Icons/ResourceProjectile.svg", 
			"特效" => "res://addons/ModEditor/Icons/ResourceAnimation.svg", 
			_ => "res://addons/ModEditor/Icons/ResourceFallingObject.svg", 
		}, null, ResourceLoader.CacheMode.Reuse);
	}

	private void BindDropItemName(DropItemConfig config)
	{
		_nameEdit.Text = config.Name.ToString();
		_nameEdit.FocusEntered += delegate
		{
			_propertyBinding?.BeginEdit(config, "Name");
		};
		_nameEdit.TextChanged += delegate(string text)
		{
			if (!_updatingControls && _propertyBinding != null)
			{
				_propertyBinding.PreviewValue(config, "Name", Variant.From<StringName>(new StringName(text)));
				RefreshDropItemVisuals();
			}
		};
		_nameEdit.TextSubmitted += delegate(string text)
		{
			CommitDropItemName(config, text);
		};
		_nameEdit.FocusExited += delegate
		{
			CommitDropItemName(config, _nameEdit.Text);
		};
	}

	private void CommitDropItemName(DropItemConfig config, string text)
	{
		if (!_updatingControls && _propertyBinding != null)
		{
			_propertyBinding.CommitEdit(config, "Name", Variant.From<StringName>(new StringName(text ?? "")), "修改掉落物注册名", this, "RefreshDropItemEditorFromHistory");
			RefreshDropItemVisuals();
		}
	}

	private void OnScenePicked(Resource resource)
	{
		if (!_updatingControls && _propertyBinding != null && _editingConfig != null && (resource == null || resource is PackedScene))
		{
			_propertyBinding.SetValue(_editingConfig, "Scene", Variant.From<PackedScene>(resource as PackedScene), "更换掉落物游戏场景", this, "RefreshDropItemEditorFromHistory");
			RefreshDropItemVisuals(rebuildScene: true);
		}
	}

	private void OnHandlerPicked(Resource resource)
	{
		if (!_updatingControls && _propertyBinding != null && _editingConfig != null && (resource == null || resource is DropItemHandler))
		{
			_propertyBinding.SetValue(_editingConfig, "Handler", Variant.From<DropItemHandler>(resource as DropItemHandler), "更换掉落物拾取处理器", this, "RefreshDropItemEditorFromHistory");
			BuildHandlerCards();
			RefreshDropItemVisuals();
		}
	}

	private void ShowAudioSelector(string propertyName)
	{
		if (_editingConfig != null)
		{
			_audioTargetProperty = ((propertyName == "PickAudio") ? "PickAudio" : "FallAudio");
			EnsureAudioSelector();
			string currentKey = ((_audioTargetProperty == "PickAudio") ? _editingConfig.PickAudio : _editingConfig.FallAudio);
			_audioPicker?.Open(XWGameplayResourceKind.Audio, currentKey, ApplyAudioChoice, (XWGameplayResourceChoice choice) => choice.Kind == XWGameplayResourceKind.Audio, lockKind: true, (_audioTargetProperty == "PickAudio") ? "掉落物拾取音效" : "掉落物下落音效");
		}
	}

	private void EnsureAudioSelector()
	{
		if (!GodotObject.IsInstanceValid(_audioPicker))
		{
			_audioPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_audioPicker))
			{
				AddChild(_audioPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ApplyAudioChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null) && choice.Kind == XWGameplayResourceKind.Audio)
		{
			SetAudioKey(_audioTargetProperty, choice.Key, (_audioTargetProperty == "PickAudio") ? "更换掉落物拾取音效" : "更换掉落物下落音效");
		}
	}

	private void SetAudioKey(string propertyName, string audioKey, string actionName)
	{
		if (_editingConfig != null && _propertyBinding != null)
		{
			propertyName = ((propertyName == "PickAudio") ? "PickAudio" : "FallAudio");
			_propertyBinding.SetValue(_editingConfig, propertyName, audioKey ?? "", actionName, this, "RefreshDropItemEditorFromHistory");
			PopulateConfigControls();
			RefreshDropItemVisuals();
		}
	}

	private void RenderConfigWorkbench()
	{
		_configFields.Visible = true;
		_handlerFields.Visible = false;
		_title.Text = "战场掉落物 · 配置与游戏预览";
		_resourceType.Text = "DropItemConfig";
		PopulateConfigControls();
		UpdateConfigPreviewLabels();
		RebuildDropItemScenePreview();
		ResetPreviewTransform();
	}

	private void RenderHandlerWorkbench()
	{
		_configFields.Visible = false;
		_handlerFields.Visible = true;
		_title.Text = "战场掉落物 · 拾取处理器";
		_resourceType.Text = _editingHandler?.GetType().Name ?? "DropItemHandler";
		_fallbackDrop.Visible = true;
		_missingScene.Visible = false;
		_sceneStatus.Text = "处理器资源不包含显示场景";
		_valueLabel.Text = "拾取";
		UpdateHandlerPreview();
		ResetPreviewTransform();
	}

	private void PopulateConfigControls()
	{
		if (_editingConfig == null || !GodotObject.IsInstanceValid(_nameEdit))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_nameEdit.Text = _editingConfig.Name.ToString();
			_valueSpin.Value = _editingConfig.Value;
			_poolSpin.Value = Math.Max(1, _editingConfig.PoolMaxNum);
			_coinIdSpin.Value = _editingConfig.CoinObjectId;
			_fallAudioEdit.Text = _editingConfig.FallAudio ?? "";
			_pickAudioEdit.Text = _editingConfig.PickAudio ?? "";
			HFlowContainer categoryCards = _categoryCards;
			int category = (int)_editingConfig.Category;
			SelectVisualCard(categoryCards, category.ToString());
			_objectIdPicker?.SetSelectedId((int)_editingConfig.Id);
			_scenePicker?.SetEditedResource(_editingConfig.Scene);
			_handlerPicker?.SetEditedResource(_editingConfig.Handler);
			BuildHandlerCards();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void OnDropItemPropertyEdited(bool committed)
	{
		Resource currentResource = base.CurrentResource;
		if (currentResource != null)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				currentResource.EmitChanged();
			}
			RefreshDropItemVisuals();
		}
	}

	private void RefreshDropItemVisuals(bool rebuildScene = false)
	{
		if (_editingConfig != null)
		{
			UpdateConfigPreviewLabels();
			if (rebuildScene)
			{
				RebuildDropItemScenePreview();
			}
		}
		else if (_editingHandler != null)
		{
			UpdateHandlerPreview();
		}
	}

	public void RefreshDropItemEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || (!xWUndoRedoManager.IsUndoing() && !xWUndoRedoManager.IsRedoing()) || base.CanvasGrid == null)
		{
			return;
		}
		Resource currentResource = base.CurrentResource;
		if (currentResource == null)
		{
			return;
		}
		MarkCurrentResourceDirty();
		currentResource.EmitChanged();
		_propertyBinding?.Dispose();
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderCustomVisualPreset(null);
	}

	private void UpdateConfigPreviewLabels()
	{
		if (_editingConfig != null && GodotObject.IsInstanceValid(_valueLabel))
		{
			string value = (string.IsNullOrEmpty(_editingConfig.Name.ToString()) ? "未命名掉落物" : _editingConfig.Name.ToString());
			_valueLabel.Text = ((_editingConfig.Value >= 0) ? $"+{_editingConfig.Value}" : _editingConfig.Value.ToString());
			_sceneStatus.Text = (GodotObject.IsInstanceValid(_editingConfig.Scene) ? FormatResourcePath(_editingConfig.Scene.ResourcePath, "已配置内嵌场景") : "未配置 Scene（可在下方资源选择器选择）");
			_status.Text = $"{value} · {GetDropCategoryLabel(_editingConfig.Category)} · 拾取值 {_editingConfig.Value}";
			_runtimeState.Text = $"{GetHandlerDisplayName(_editingConfig.Handler)} · 池上限 {_editingConfig.PoolMaxNum}";
		}
	}

	private void RebuildDropItemScenePreview()
	{
		if (!GodotObject.IsInstanceValid(_sceneHost))
		{
			return;
		}
		foreach (Node child in _sceneHost.GetChildren())
		{
			child.QueueFree();
		}
		_visualPreviewNodes.Clear();
		bool flag = _editingConfig != null && GodotObject.IsInstanceValid(_editingConfig.Scene);
		_fallbackDrop.Visible = !flag;
		_missingScene.Visible = !flag && _editingConfig != null;
		if (!flag)
		{
			ResetPreviewTransform();
			return;
		}
		try
		{
			Node node = _editingConfig.Scene.Instantiate(PackedScene.GenEditState.Disabled);
			DisablePreviewGameplayProcessing(node);
			_sceneHost.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CollectVisualPreviewNodes(node);
			UpdatePreviewProcessingVisibility();
			CenterSceneInstance(node);
			_fallbackDrop.Visible = false;
			_missingScene.Visible = false;
			_runtimeState.Text = "已载入游戏场景 · " + GetHandlerDisplayName(_editingConfig.Handler);
		}
		catch (Exception ex)
		{
			_fallbackDrop.Visible = true;
			_missingScene.Visible = true;
			_missingScene.Text = "场景预览失败：" + ex.Message;
			_runtimeState.Text = "场景预览失败";
			GD.PushWarning("Drop-item editor scene preview failed: " + ex.Message);
		}
		ResetPreviewTransform();
	}

	private static void DisablePreviewGameplayProcessing(Node node)
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
			DisablePreviewGameplayProcessing(child);
		}
	}

	private void CollectVisualPreviewNodes(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite || node is AnimatedSprite2D || node is AnimationPlayer || node is GpuParticles2D)
		{
			_visualPreviewNodes.Add(node);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectVisualPreviewNodes(child);
		}
	}

	private void UpdatePreviewProcessingVisibility()
	{
		bool flag = IsVisibleInTree();
		foreach (Node visualPreviewNode in _visualPreviewNodes)
		{
			if (!GodotObject.IsInstanceValid(visualPreviewNode))
			{
				continue;
			}
			visualPreviewNode.ProcessMode = (ProcessModeEnum)(flag ? 3 : 4);
			visualPreviewNode.SetProcess(flag);
			if (visualPreviewNode is AnimatedSprite2D animatedSprite2D && GodotObject.IsInstanceValid(animatedSprite2D.SpriteFrames))
			{
				if (flag && !animatedSprite2D.IsPlaying())
				{
					animatedSprite2D.Play();
				}
				else if (!flag)
				{
					animatedSprite2D.Pause();
				}
			}
			if (visualPreviewNode is GpuParticles2D gpuParticles2D)
			{
				gpuParticles2D.Emitting = flag;
			}
		}
		if (!flag)
		{
			SetProcess(enable: false);
		}
		else if (_previewState != PreviewState.Idle)
		{
			SetProcess(enable: true);
		}
	}

	private static void CenterSceneInstance(Node instance)
	{
		if (instance is Node2D node2D)
		{
			node2D.Position = Vector2.Zero;
		}
		else if (instance is Control control)
		{
			control.Position = -control.Size * 0.5f;
		}
	}

	private void StartDropPreview()
	{
		if (GodotObject.IsInstanceValid(_previewRoot))
		{
			_previewState = PreviewState.Dropping;
			_previewElapsed = 0.0;
			_previewRoot.Visible = true;
			_previewRoot.Position = new Vector2(480f, 88f);
			_previewRoot.Scale = Vector2.One;
			_previewRoot.Modulate = Colors.White;
			_status.Text = "掉落物正在进入草坪……";
			_runtimeState.Text = "下落中";
			SetProcess(enable: true);
		}
	}

	private void StartPickPreview()
	{
		if (GodotObject.IsInstanceValid(_previewRoot))
		{
			if (!_previewRoot.Visible)
			{
				ResetPreviewTransform();
			}
			_previewState = PreviewState.Picking;
			_previewElapsed = 0.0;
			_pickStartPosition = ((_previewRoot.Position.Y < 250f) ? new Vector2(480f, 365f) : _previewRoot.Position);
			_previewRoot.Position = _pickStartPosition;
			_previewRoot.Scale = Vector2.One;
			_previewRoot.Modulate = Colors.White;
			_status.Text = "拾取动画：飞向玩家资源栏";
			_runtimeState.Text = "拾取中";
			if (_editingHandler != null)
			{
				UpdateHandlerPreview();
			}
			SetProcess(enable: true);
		}
	}

	private void UpdateDropPreview()
	{
		float num = Mathf.Clamp((float)(_previewElapsed / 0.9), 0f, 1f);
		float weight = 1f - Mathf.Pow(1f - num, 2.3f);
		_previewRoot.Position = new Vector2(480f + Mathf.Sin(num * (float)Math.PI * 2f) * 16f, Mathf.Lerp(88f, 365f, weight));
		_previewRoot.Scale = Vector2.One * (0.84f + Mathf.Sin(num * (float)Math.PI) * 0.18f);
		if (!(num < 1f))
		{
			_previewRoot.Position = new Vector2(480f, 365f);
			_previewRoot.Scale = Vector2.One;
			_previewState = PreviewState.Idle;
			_status.Text = "掉落物已落在草坪，可模拟拾取";
			_runtimeState.Text = "等待拾取";
			SetProcess(enable: false);
		}
	}

	private void UpdatePickPreview()
	{
		float num = Mathf.Clamp((float)(_previewElapsed / 0.58), 0f, 1f);
		float weight = num * num * (3f - 2f * num);
		_previewRoot.Position = _pickStartPosition.Lerp(new Vector2(842f, 57f), weight);
		_previewRoot.Scale = Vector2.One * Mathf.Lerp(1f, 0.18f, weight);
		_previewRoot.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.25f, weight));
		if (!(num < 1f))
		{
			_previewState = PreviewState.Idle;
			_previewRoot.Visible = false;
			_status.Text = ((_editingConfig != null) ? $"已拾取：{_editingConfig.Name}，游戏效果 {GetHandlerDisplayName(_editingConfig.Handler)}" : ("已模拟处理器：" + GetHandlerDisplayName(_editingHandler)));
			_runtimeState.Text = "拾取完成（未修改游戏资源）";
			SetProcess(enable: false);
		}
	}

	private void ResetPreviewTransform()
	{
		_previewState = PreviewState.Idle;
		_previewElapsed = 0.0;
		SetProcess(enable: false);
		if (GodotObject.IsInstanceValid(_previewRoot))
		{
			_previewRoot.Visible = true;
			_previewRoot.Position = new Vector2(480f, 365f);
			_previewRoot.Scale = Vector2.One;
			_previewRoot.Modulate = Colors.White;
		}
	}

	private void UpdateHandlerPreview()
	{
		if (_editingHandler != null && GodotObject.IsInstanceValid(_handlerTypeLabel))
		{
			int num = Math.Max(1, Mathf.RoundToInt(_handlerSimulationCount.Value));
			_handlerTypeLabel.Text = GetHandlerDisplayName(_editingHandler);
			_handlerEffectLabel.Text = GetHandlerDescription(_editingHandler);
			_handlerOutcome.Text = BuildHandlerOutcome(_editingHandler, num);
			_status.Text = $"处理器模拟 · 连续拾取 {num} 次";
			_runtimeState.Text = "仅推演结果，不执行真实奖励";
		}
	}

	private void PlayConfiguredAudio(string key, string kind)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			_runtimeState.Text = "未配置" + kind + "音效键";
			return;
		}
		EnsureAudioSelector();
		AudioStream audioStream = (GodotObject.IsInstanceValid(_audioPicker) ? _audioPicker.LoadAudioStream(key) : null);
		if (GodotObject.IsInstanceValid(audioStream) && GodotObject.IsInstanceValid(_directAudioPlayer))
		{
			_directAudioPlayer.Stop();
			_directAudioPlayer.Stream = audioStream;
			_directAudioPlayer.Play();
			_runtimeState.Text = "正在直接试听" + kind + "音效：" + key;
		}
		else if (!GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			_runtimeState.Text = "无法加载" + kind + "音效：" + key;
		}
		else
		{
			AudioManager.Instance.AudioPlay(key);
			_runtimeState.Text = "正在试听" + kind + "音效：" + key;
		}
	}

	private void AddSummaryRows()
	{
		if (base.PreviewList != null)
		{
			if (_editingConfig != null)
			{
				base.PreviewList.AddItem($"注册名 -> {_editingConfig.Name}");
				base.PreviewList.AddItem("游戏场景 -> " + FormatResourcePath(_editingConfig.Scene?.ResourcePath, "未配置"));
				base.PreviewList.AddItem("拾取效果 -> " + GetHandlerDisplayName(_editingConfig.Handler));
			}
			else
			{
				base.PreviewList.AddItem("处理器 -> " + GetHandlerDisplayName(_editingHandler));
				base.PreviewList.AddItem("模拟不会调用 OnCollect，不会改金币、阳光、卡牌或存档");
			}
		}
	}

	private static int GetBuiltInHandlerId(DropItemHandler handler)
	{
		if (handler != null)
		{
			if (!(handler is SunDropItemHandler))
			{
				if (!(handler is CoinDropItemHandler))
				{
					if (!(handler is LuckyBagDropItemHandler))
					{
						if (!(handler is JalapenoSunDropItemHandler))
						{
							if (!(handler is BrainSunDropItemHandler))
							{
								if (handler is GoldShardDropItemHandler)
								{
									return 6;
								}
								return -1;
							}
							return 5;
						}
						return 4;
					}
					return 3;
				}
				return 2;
			}
			return 1;
		}
		return 0;
	}

	private static DropItemHandler CreateBuiltInHandler(int id)
	{
		return id switch
		{
			1 => new SunDropItemHandler(), 
			2 => new CoinDropItemHandler(), 
			3 => new LuckyBagDropItemHandler(), 
			4 => new JalapenoSunDropItemHandler(), 
			5 => new BrainSunDropItemHandler(), 
			6 => new GoldShardDropItemHandler(), 
			_ => null, 
		};
	}

	private static string GetHandlerDisplayName(DropItemHandler handler)
	{
		if (handler != null)
		{
			if (!(handler is SunDropItemHandler))
			{
				if (!(handler is CoinDropItemHandler))
				{
					if (!(handler is LuckyBagDropItemHandler))
					{
						if (!(handler is JalapenoSunDropItemHandler))
						{
							if (!(handler is BrainSunDropItemHandler))
							{
								if (handler is GoldShardDropItemHandler)
								{
									return "金色碎片合成卡牌";
								}
								return "自定义：" + handler.GetType().Name;
							}
							return "僵尸脑子阳光";
						}
						return "火爆辣椒阳光";
					}
					return "福袋里程碑奖励";
				}
				return "增加金币";
			}
			return "增加阳光";
		}
		return "无拾取效果";
	}

	private static string GetHandlerDescription(DropItemHandler handler)
	{
		if (!(handler is SunDropItemHandler))
		{
			if (!(handler is CoinDropItemHandler))
			{
				if (!(handler is LuckyBagDropItemHandler))
				{
					if (!(handler is JalapenoSunDropItemHandler))
					{
						if (!(handler is BrainSunDropItemHandler))
						{
							if (handler is GoldShardDropItemHandler)
							{
								return "每累计 4 个碎片，从 GeneralPlant/Gold 中生成金卡。";
							}
							return "自定义处理器。编辑器不会直接执行 OnCollect，避免预览污染游戏状态。";
						}
						return "按阵营与账户规则结算僵尸脑子阳光。";
					}
					return "按阳光账户规则结算，并表现为火爆辣椒阳光。";
				}
				return "累计拾取次数，在 10、30、60、100 次触发不同阳光或卡牌奖励。";
			}
			return "拾取后把配置数值加入金币总量。";
		}
		return "拾取后把配置数值加入当前玩家的阳光账户。";
	}

	private static string BuildHandlerOutcome(DropItemHandler handler, int count)
	{
		if (!(handler is LuckyBagDropItemHandler))
		{
			if (!(handler is GoldShardDropItemHandler))
			{
				if (!(handler is CoinDropItemHandler))
				{
					if (!(handler is SunDropItemHandler) && !(handler is JalapenoSunDropItemHandler) && !(handler is BrainSunDropItemHandler))
					{
						return $"将发生 {count} 次自定义拾取回调；预览未执行这些回调。";
					}
					return $"连续拾取 {count} 次会按账户规则结算每次配置的阳光值。";
				}
				return $"连续拾取 {count} 次会把每次配置值累计到金币。";
			}
			return $"{count} 个碎片可组成 {count / 4} 组金卡奖励，剩余 {count % 4} 个。";
		}
		return (count < 60) ? ((count < 10) ? $"还差 {10 - count} 次触发 100 阳光奖励。" : ((count >= 30) ? "已越过 30 次奖励；下一里程碑为 60 次的金卡奖励。" : "已越过 10 次奖励；下一里程碑为 30 次的白卡奖励。")) : ((count >= 100) ? $"达到 {count} 次；100 次后每个整百会按百位数量追加金卡。" : "已越过 60 次奖励；下一里程碑为 100 次的 1000 阳光奖励。");
	}

	private static string GetDropCategoryLabel(TowerDefenseEnum.DROP_ITEM_CATEGORY category)
	{
		return category switch
		{
			TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN => "阳光", 
			TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN => "硬币", 
			TowerDefenseEnum.DROP_ITEM_CATEGORY.SPECIAL => "特殊奖励", 
			_ => category.ToString(), 
		};
	}

	private static string FormatResourcePath(string path, string fallback)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path;
		}
		return fallback;
	}
}
