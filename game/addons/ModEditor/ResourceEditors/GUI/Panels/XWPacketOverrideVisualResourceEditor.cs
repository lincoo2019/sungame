using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketOverrideVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketOverrideVisualEditorLayout.tscn";

	private const string PacketEventSelectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketEventSelectorWindow.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private static PackedScene _packetEventSelectorScene;

	private static readonly System.Collections.Generic.Dictionary<string, Texture2D> PacketTextureCache = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private static readonly TowerDefenseEnum.PACKET_TYPE[] PacketTypes = new TowerDefenseEnum.PACKET_TYPE[9]
	{
		TowerDefenseEnum.PACKET_TYPE.WHITE,
		TowerDefenseEnum.PACKET_TYPE.GOLD,
		TowerDefenseEnum.PACKET_TYPE.DIAMOND,
		TowerDefenseEnum.PACKET_TYPE.COLOUR,
		TowerDefenseEnum.PACKET_TYPE.STAR,
		TowerDefenseEnum.PACKET_TYPE.ORIGINAL,
		TowerDefenseEnum.PACKET_TYPE.ZOMBIE,
		TowerDefenseEnum.PACKET_TYPE.COVER,
		TowerDefenseEnum.PACKET_TYPE.GRAY
	};

	private TowerDefensePacketOverride _editingOverride;

	private TowerDefenseInGamePacketShow _basePacket;

	private TowerDefenseInGamePacketShow _resultPacket;

	private Control _resultCardRoot;

	private Label _baseSummary;

	private Label _resultSummary;

	private Label _overrideState;

	private Label _overrideBadge;

	private Label _characterOverrideStatus;

	private OptionButton _baseType;

	private XWVisualOptionGallery _baseTypeVisualGallery;

	private SpinBox _baseCost;

	private SpinBox _baseCooldown;

	private SpinBox _baseStartingCooldown;

	private SpinBox _baseWeight;

	private SpinBox _baseWavePointCost;

	private OptionButton _overrideType;

	private XWVisualOptionGallery _overrideTypeVisualGallery;

	private SpinBox _costRise;

	private SpinBox _overrideCost;

	private SpinBox _costMultiple;

	private SpinBox _packetCooldown;

	private SpinBox _startingCooldown;

	private SpinBox _weight;

	private SpinBox _wavePointCost;

	private CheckBox _isLimitGridNum;

	private CheckBox _coverCanDirectPlant;

	private CheckBox _hypnoses;

	private ItemList _plantCoverList;

	private LineEdit _plantCoverInput;

	private ItemList _pressedActionsList;

	private ItemList _useSucceededActionsList;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private XWResourcePicker _characterOverridePicker;

	private XWVisualPropertyBinding _propertyBinding;

	private XWPacketEventSelectorWindow _packetEventSelectorWindow;

	private string _packetEventTargetProperty = "pressedActions";

	private Tween _previewTween;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposePacketOverrideBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposePacketOverrideBinding();
		if (base.CurrentResource is TowerDefensePacketOverride towerDefensePacketOverride && base.CanvasGrid != null)
		{
			_editingOverride = towerDefensePacketOverride;
			base.CanvasGrid.Columns = 1;
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketOverrideVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindWorkbench(vBoxContainer, towerDefensePacketOverride);
				PopulateOverrideFields();
				UpdateSimulationPreview(applied: true);
				AddSummaryRows();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompletePacketOverrideVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompletePacketOverrideVisualCoverage(Resource resource)
	{
		return resource?.GetType() == typeof(TowerDefensePacketOverride);
	}

	private void DisposePacketOverrideBinding()
	{
		_baseTypeVisualGallery?.Dispose();
		_baseTypeVisualGallery = null;
		_overrideTypeVisualGallery?.Dispose();
		_overrideTypeVisualGallery = null;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingOverride = null;
	}

	private void BindWorkbench(VBoxContainer root, TowerDefensePacketOverride packetOverride)
	{
		_basePacket = root.GetNode<TowerDefenseInGamePacketShow>("%BasePacket");
		_resultPacket = root.GetNode<TowerDefenseInGamePacketShow>("%ResultPacket");
		_resultCardRoot = root.GetNode<Control>("%ResultCardRoot");
		_baseSummary = root.GetNode<Label>("%BaseSummary");
		_resultSummary = root.GetNode<Label>("%ResultSummary");
		_overrideState = root.GetNode<Label>("%OverrideState");
		_overrideBadge = root.GetNode<Label>("%OverrideBadge");
		_characterOverrideStatus = root.GetNode<Label>("%CharacterOverrideStatus");
		_baseType = root.GetNode<OptionButton>("%BaseType");
		_baseCost = root.GetNode<SpinBox>("%BaseCost");
		_baseCooldown = root.GetNode<SpinBox>("%BaseCooldown");
		_baseStartingCooldown = root.GetNode<SpinBox>("%BaseStartingCooldown");
		_baseWeight = root.GetNode<SpinBox>("%BaseWeight");
		_baseWavePointCost = root.GetNode<SpinBox>("%BaseWavePointCost");
		_overrideType = root.GetNode<OptionButton>("%OverrideType");
		_costRise = root.GetNode<SpinBox>("%CostRise");
		_overrideCost = root.GetNode<SpinBox>("%OverrideCost");
		_costMultiple = root.GetNode<SpinBox>("%CostMultiple");
		_packetCooldown = root.GetNode<SpinBox>("%PacketCooldown");
		_startingCooldown = root.GetNode<SpinBox>("%StartingCooldown");
		_weight = root.GetNode<SpinBox>("%Weight");
		_wavePointCost = root.GetNode<SpinBox>("%WavePointCost");
		_isLimitGridNum = root.GetNode<CheckBox>("%IsLimitGridNum");
		_coverCanDirectPlant = root.GetNode<CheckBox>("%CoverCanDirectPlant");
		_hypnoses = root.GetNode<CheckBox>("%Hypnoses");
		_plantCoverList = root.GetNode<ItemList>("%PlantCoverList");
		_plantCoverInput = root.GetNode<LineEdit>("%PlantCoverInput");
		_pressedActionsList = root.GetNode<ItemList>("%EventPressList");
		_useSucceededActionsList = root.GetNode<ItemList>("%EventPlantList");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Setup("TowerDefenseCharacterOverride");
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		root.GetNode<VBoxContainer>("%CharacterOverridePickerHost").AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
		_characterOverridePicker = xWResourcePicker;
		xWResourcePicker.ResourceChanged += OnCharacterOverridePicked;
		BindDirectEditControls(packetOverride);
		_overrideType.AddItem("继承原卡框");
		TowerDefenseEnum.PACKET_TYPE[] packetTypes = PacketTypes;
		foreach (TowerDefenseEnum.PACKET_TYPE pACKET_TYPE in packetTypes)
		{
			_baseType.AddItem(GetPacketTypeDisplayName(pACKET_TYPE), (int)pACKET_TYPE);
			_overrideType.AddItem(GetPacketTypeDisplayName(pACKET_TYPE), (int)pACKET_TYPE);
		}
		_baseTypeVisualGallery = new XWVisualOptionGallery(_baseType, root.GetNode<HFlowContainer>("%BaseTypeVisualChoices"), "BasePacketTypeVisualCatalog", (int index) => LoadPacketTexture((TowerDefenseEnum.PACKET_TYPE)_baseType.GetItemId(index)));
		_baseTypeVisualGallery.Rebuild();
		_overrideTypeVisualGallery = new XWVisualOptionGallery(_overrideType, root.GetNode<HFlowContainer>("%OverrideTypeVisualChoices"), "OverridePacketTypeVisualCatalog", (int index) => (_overrideType.GetItemId(index) != -1) ? LoadPacketTexture((TowerDefenseEnum.PACKET_TYPE)_overrideType.GetItemId(index)) : ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCard.svg", null, ResourceLoader.CacheMode.Reuse));
		_overrideTypeVisualGallery.Rebuild();
		ConfigurePacketCard(_basePacket, 100, TowerDefenseEnum.PACKET_TYPE.WHITE);
		ConfigurePacketCard(_resultPacket, 100, TowerDefenseEnum.PACKET_TYPE.WHITE);
		root.GetNode<Button>("%ApplyButton").Pressed += ApplyOverridePreview;
		root.GetNode<Button>("%ResetButton").Pressed += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		root.GetNode<Button>("%AddPlantCoverButton").Pressed += AddPlantCover;
		root.GetNode<Button>("%RemovePlantCoverButton").Pressed += RemovePlantCover;
		root.GetNode<Button>("%CharacterOverrideButton").Pressed += OpenCharacterOverride;
		root.GetNode<Button>("%AddPressEventButton").Pressed += delegate
		{
			ShowPacketEventSelector("pressedActions");
		};
		root.GetNode<Button>("%AddPlantEventButton").Pressed += delegate
		{
			ShowPacketEventSelector("useSucceededActions");
		};
		root.GetNode<Button>("%MovePressEventUpButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("pressedActions", -1);
		};
		root.GetNode<Button>("%MovePressEventDownButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("pressedActions", 1);
		};
		root.GetNode<Button>("%MovePlantEventUpButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("useSucceededActions", -1);
		};
		root.GetNode<Button>("%MovePlantEventDownButton").Pressed += delegate
		{
			MoveSelectedPacketEvent("useSucceededActions", 1);
		};
		root.GetNode<Button>("%RemovePressEventButton").Pressed += delegate
		{
			RemoveSelectedPacketEvent("pressedActions");
		};
		root.GetNode<Button>("%RemovePlantEventButton").Pressed += delegate
		{
			RemoveSelectedPacketEvent("useSucceededActions");
		};
		_pressedActionsList.ItemActivated += delegate(long index)
		{
			OpenPacketEventResource("pressedActions", (int)index);
		};
		_useSucceededActionsList.ItemActivated += delegate(long index)
		{
			OpenPacketEventResource("useSucceededActions", (int)index);
		};
		_baseType.ItemSelected += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_baseCost.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_baseCooldown.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_baseStartingCooldown.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_baseWeight.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_baseWavePointCost.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: true);
		};
		_overrideType.ItemSelected += OnOverrideTypeSelected;
	}

	private void BindDirectEditControls(TowerDefensePacketOverride packetOverride)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnPacketOverridePropertyEdited);
		_propertyBinding.BindText(_resourceName, packetOverride, "resource_name", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindToggle(_localToScene, packetOverride, "resource_local_to_scene", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_costRise, packetOverride, "costRise", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_overrideCost, packetOverride, "cost", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_costMultiple, packetOverride, "costMultiple", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_packetCooldown, packetOverride, "packetCooldown", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_startingCooldown, packetOverride, "startingCooldown", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_weight, packetOverride, "weight", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindNumber(_wavePointCost, packetOverride, "wavePointCost", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindToggle(_isLimitGridNum, packetOverride, "islimitGridNum", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindToggle(_coverCanDirectPlant, packetOverride, "coverCanDirectPlant", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
		_propertyBinding.BindToggle(_hypnoses, packetOverride, "hypnoses", RefreshDirectEditPreview, this, "RefreshPacketOverrideFromHistory");
	}

	private void OnOverrideTypeSelected(long index)
	{
		if (!_updatingControls && _propertyBinding != null && GodotObject.IsInstanceValid(_editingOverride) && index >= 0 && index < _overrideType.ItemCount)
		{
			_propertyBinding.SetValue(_editingOverride, "type", Variant.From<long>((long)_overrideType.GetItemId((int)index)), "修改覆盖卡框", this, "RefreshPacketOverrideFromHistory");
			UpdateSimulationPreview(applied: true);
		}
	}

	private void OnPacketOverridePropertyEdited(bool committed)
	{
		if (base.CurrentResource is TowerDefensePacketOverride towerDefensePacketOverride && towerDefensePacketOverride == _editingOverride)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				towerDefensePacketOverride.EmitChanged();
			}
			RefreshDirectEditPreview();
		}
	}

	private void RefreshDirectEditPreview()
	{
		UpdateSimulationPreview(applied: true);
		RefreshLists();
	}

	public void RefreshPacketOverrideFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is TowerDefensePacketOverride towerDefensePacketOverride && towerDefensePacketOverride == _editingOverride)
		{
			BindDirectEditControls(towerDefensePacketOverride);
			PopulateOverrideFields();
			UpdateSimulationPreview(applied: true);
		}
	}

	private void PopulateOverrideFields()
	{
		if (!GodotObject.IsInstanceValid(_editingOverride) || !GodotObject.IsInstanceValid(_overrideType))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			SelectOptionById(_overrideType, (int)_editingOverride.type);
			_overrideTypeVisualGallery?.RefreshSelection();
			_costRise.Value = _editingOverride.costRise;
			_overrideCost.Value = _editingOverride.cost;
			_costMultiple.Value = _editingOverride.costMultiple;
			_packetCooldown.Value = _editingOverride.packetCooldown;
			_startingCooldown.Value = _editingOverride.startingCooldown;
			_weight.Value = _editingOverride.weight;
			_wavePointCost.Value = _editingOverride.wavePointCost;
			_isLimitGridNum.ButtonPressed = _editingOverride.islimitGridNum;
			_coverCanDirectPlant.ButtonPressed = _editingOverride.coverCanDirectPlant;
			_hypnoses.ButtonPressed = _editingOverride.hypnoses;
			_characterOverridePicker?.SetEditedResource(_editingOverride.characterOverride);
			_characterOverrideStatus.Text = (GodotObject.IsInstanceValid(_editingOverride.characterOverride) ? "角色覆盖：已配置" : "角色覆盖：默认");
			RefreshLists();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void AddPlantCover()
	{
		if (!GodotObject.IsInstanceValid(_editingOverride) || _propertyBinding == null)
		{
			return;
		}
		string text = _plantCoverInput.Text.StripEdges();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Array<string> array = CloneStringArray(_editingOverride.plantCover);
			if (!array.Contains(text))
			{
				array.Add(text);
				_propertyBinding.SetValue(_editingOverride, "plantCover", array, "添加可覆盖植物", this, "RefreshPacketOverrideFromHistory");
				_plantCoverInput.Text = "";
				RefreshDirectEditPreview();
			}
		}
	}

	private void RemovePlantCover()
	{
		if (GodotObject.IsInstanceValid(_editingOverride) && _propertyBinding != null && _editingOverride.plantCover != null)
		{
			int[] selectedItems = _plantCoverList.GetSelectedItems();
			if (selectedItems.Length != 0 && selectedItems[0] >= 0 && selectedItems[0] < _editingOverride.plantCover.Count)
			{
				Array<string> array = CloneStringArray(_editingOverride.plantCover);
				array.RemoveAt(selectedItems[0]);
				_propertyBinding.SetValue(_editingOverride, "plantCover", array, "删除可覆盖植物", this, "RefreshPacketOverrideFromHistory");
				RefreshDirectEditPreview();
			}
		}
	}

	private static Array<string> CloneStringArray(Array<string> source)
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

	private void RefreshLists()
	{
		if (!GodotObject.IsInstanceValid(_editingOverride))
		{
			return;
		}
		_plantCoverList?.Clear();
		if (_editingOverride.plantCover != null && GodotObject.IsInstanceValid(_plantCoverList))
		{
			foreach (string item in _editingOverride.plantCover)
			{
				_plantCoverList.AddItem(item);
			}
		}
		PopulateEventList(_pressedActionsList, _editingOverride.pressedActions);
		PopulateEventList(_useSucceededActionsList, _editingOverride.useSucceededActions);
	}

	private static void PopulateEventList(ItemList list, Array<CardActionBehaviorDefinition> events)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (events != null)
		{
			for (int i = 0; i < events.Count; i++)
			{
				CardActionBehaviorDefinition cardActionBehaviorDefinition = events[i];
				list.AddItem(GodotObject.IsInstanceValid(cardActionBehaviorDefinition) ? $"{i + 1}. {cardActionBehaviorDefinition.GetType().Name}" : $"{i + 1}. <空事件>");
			}
		}
	}

	private void ApplyOverridePreview()
	{
		UpdateSimulationPreview(applied: true);
		if (GodotObject.IsInstanceValid(_resultCardRoot))
		{
			if (GodotObject.IsInstanceValid(_previewTween))
			{
				_previewTween.Kill();
			}
			_resultCardRoot.Scale = Vector2.One * 0.78f;
			_previewTween = CreateTween();
			_previewTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			_previewTween.TweenProperty(_resultCardRoot, "scale", Vector2.One * 1.08f, 0.22);
			_previewTween.TweenProperty(_resultCardRoot, "scale", Vector2.One, 0.14);
		}
	}

	private void UpdateSimulationPreview(bool applied)
	{
		if (GodotObject.IsInstanceValid(_editingOverride) && GodotObject.IsInstanceValid(_basePacket))
		{
			if (GodotObject.IsInstanceValid(_previewTween))
			{
				_previewTween.Kill();
				_previewTween = null;
			}
			TowerDefenseEnum.PACKET_TYPE selectedId = (TowerDefenseEnum.PACKET_TYPE)_baseType.GetSelectedId();
			int num = Mathf.RoundToInt(_baseCost.Value);
			double value = _baseCooldown.Value;
			double value2 = _baseStartingCooldown.Value;
			int num2 = Mathf.RoundToInt(_baseWeight.Value);
			int num3 = Mathf.RoundToInt(_baseWavePointCost.Value);
			TowerDefenseEnum.PACKET_TYPE packetType = ((applied && _editingOverride.type != TowerDefenseEnum.PACKET_TYPE.NOONE) ? _editingOverride.type : selectedId);
			int num4 = (applied ? CalculateEffectiveCost(num, _editingOverride) : num);
			double value3 = ((applied && _editingOverride.packetCooldown != -1.0) ? _editingOverride.packetCooldown : value);
			double value4 = ((applied && _editingOverride.startingCooldown != -1.0) ? _editingOverride.startingCooldown : value2);
			int value5 = ((applied && _editingOverride.weight != -1) ? _editingOverride.weight : num2);
			int value6 = ((applied && _editingOverride.wavePointCost != -1) ? _editingOverride.wavePointCost : num3);
			ConfigurePacketCard(_basePacket, num, selectedId);
			ConfigurePacketCard(_resultPacket, num4, packetType);
			_baseSummary.Text = $"费用 {num} · 冷却 {value:0.##} 秒 · 开局 {value2:0.##} 秒";
			_resultSummary.Text = (applied ? $"费用 {num4} · 冷却 {value3:0.##} 秒 · 开局 {value4:0.##} 秒" : _baseSummary.Text);
			_resultCardRoot.Modulate = ((applied && _editingOverride.hypnoses) ? new Color(0.83f, 0.68f, 1f) : Colors.White);
			_overrideBadge.Text = BuildOverrideBadge();
			_overrideState.Text = (applied ? $"{GetPacketTypeDisplayName(selectedId)} → {GetPacketTypeDisplayName(packetType)} · 僵尸权重 {num2}→{value5} · 波次点数 {num3}→{value6}{BuildFlagSuffix()}" : "当前显示继承值；点击应用覆盖规则查看游戏最终结果。");
			_resultCardRoot.Scale = Vector2.One;
		}
	}

	private static int CalculateEffectiveCost(int baseCost, TowerDefensePacketOverride packetOverride)
	{
		if (packetOverride == null || packetOverride.cost == -1)
		{
			return baseCost;
		}
		return packetOverride.cost;
	}

	private string BuildOverrideBadge()
	{
		int num = 0;
		if (_editingOverride.type != TowerDefenseEnum.PACKET_TYPE.NOONE)
		{
			num++;
		}
		if (_editingOverride.costRise != -1)
		{
			num++;
		}
		if (_editingOverride.cost != -1)
		{
			num++;
		}
		if (_editingOverride.costMultiple != -1.0)
		{
			num++;
		}
		if (_editingOverride.packetCooldown != -1.0)
		{
			num++;
		}
		if (_editingOverride.startingCooldown != -1.0)
		{
			num++;
		}
		if (_editingOverride.weight != -1)
		{
			num++;
		}
		if (_editingOverride.wavePointCost != -1)
		{
			num++;
		}
		if (num != 0)
		{
			return $"已覆盖 {num} 项运行值";
		}
		return "继承原卡牌";
	}

	private string BuildFlagSuffix()
	{
		return (_editingOverride.hypnoses ? " · 催眠" : "") + (_editingOverride.coverCanDirectPlant ? " · 可直接覆盖种植" : "") + ((!_editingOverride.islimitGridNum) ? " · 不限制同格数量" : "") + $" · 按下事件 {_editingOverride.pressedActions?.Count ?? 0} · 种植事件 {_editingOverride.useSucceededActions?.Count ?? 0}";
	}

	private static void ConfigurePacketCard(TowerDefenseInGamePacketShow packet, int cost, TowerDefenseEnum.PACKET_TYPE packetType)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.ProcessMode = ProcessModeEnum.Disabled;
			packet.onlyDraw = true;
			packet.useCost = false;
			packet.baseItemCost = cost;
			packet.itemCost = cost;
			packet.alive = true;
			packet.@lock = false;
			packet.openShadow = false;
			packet.select = false;
			packet.coldDownOpen = false;
			if (GodotObject.IsInstanceValid(packet.coldDownProgressBar))
			{
				packet.coldDownProgressBar.Visible = false;
			}
			if (GodotObject.IsInstanceValid(packet.backgroundTexture))
			{
				packet.backgroundTexture.Texture = LoadPacketTexture(packetType);
			}
			packet.ColorSet();
		}
	}

	private static Texture2D LoadPacketTexture(TowerDefenseEnum.PACKET_TYPE packetType)
	{
		string packetTexturePath = GetPacketTexturePath(packetType);
		if (!PacketTextureCache.TryGetValue(packetTexturePath, out var value) || !GodotObject.IsInstanceValid(value))
		{
			value = ResourceLoader.Load<Texture2D>(packetTexturePath, null, ResourceLoader.CacheMode.Reuse);
			PacketTextureCache[packetTexturePath] = value;
		}
		return value;
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
			TowerDefenseEnum.PACKET_TYPE.NOONE => "继承原卡框", 
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

	private static void SelectOptionById(OptionButton option, int id)
	{
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == id)
			{
				option.Select(i);
				return;
			}
		}
		option.Select(0);
	}

	private void OpenCharacterOverride()
	{
		if (GodotObject.IsInstanceValid(_editingOverride) && _propertyBinding != null)
		{
			if (!GodotObject.IsInstanceValid(_editingOverride.characterOverride))
			{
				TowerDefenseCharacterOverride from = new TowerDefenseCharacterOverride();
				_propertyBinding.SetValue(_editingOverride, "characterOverride", Variant.From(in from), "创建角色覆盖", this, "RefreshPacketOverrideFromHistory");
				_characterOverridePicker?.SetEditedResource(from);
				RefreshDirectEditPreview();
			}
			XWResourceEditContext context = XWResourceEditContext.ForProperty(_editingOverride.characterOverride, _editingOverride, _editingOverride.characterOverride.ResourcePath, base.CurrentResourcePath, "characterOverride", -1, "packet_override_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
			XWEditorInterface.Instance?.EditResource(_editingOverride.characterOverride, context);
		}
	}

	private void OnCharacterOverridePicked(Resource resource)
	{
		if (!_updatingControls && _propertyBinding != null && GodotObject.IsInstanceValid(_editingOverride) && (resource == null || resource is TowerDefenseCharacterOverride))
		{
			_propertyBinding.SetValue(_editingOverride, "characterOverride", Variant.From(in resource), "更换角色覆盖", this, "RefreshPacketOverrideFromHistory");
			RefreshDirectEditPreview();
			_characterOverrideStatus.Text = (GodotObject.IsInstanceValid(resource) ? "角色覆盖：已配置" : "角色覆盖：默认");
		}
	}

	private void ShowPacketEventSelector(string targetProperty)
	{
		_packetEventTargetProperty = ((targetProperty == "useSucceededActions") ? "useSucceededActions" : "pressedActions");
		EnsurePacketEventSelectorWindow();
		if (GodotObject.IsInstanceValid(_packetEventSelectorWindow))
		{
			string title = ((_packetEventTargetProperty == "useSucceededActions") ? "覆盖种植事件拼图" : "覆盖按下事件拼图");
			string subtitle = ((_packetEventTargetProperty == "useSucceededActions") ? "为覆盖卡牌配置完成种植后的事件链" : "为覆盖卡牌配置拿起或按下时的事件链");
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
		CardActionBehaviorDefinition cardActionBehaviorDefinition = CreatePacketEventInstance(runtimeType);
		if (GodotObject.IsInstanceValid(cardActionBehaviorDefinition) && _propertyBinding != null && GodotObject.IsInstanceValid(_editingOverride))
		{
			Array<CardActionBehaviorDefinition> array = ClonePacketEventArray(GetPacketEventArray(_packetEventTargetProperty));
			array.Add(cardActionBehaviorDefinition);
			_propertyBinding.SetValue(_editingOverride, _packetEventTargetProperty, array, "添加卡牌覆盖事件", this, "RefreshPacketOverrideFromHistory");
			RefreshDirectEditPreview();
			OpenPacketEventResource(_packetEventTargetProperty, array.Count - 1);
		}
	}

	private void RemoveSelectedPacketEvent(string propertyName)
	{
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		int[] selectedItems = ((propertyName == "useSucceededActions") ? _useSucceededActionsList : _pressedActionsList).GetSelectedItems();
		if (_propertyBinding != null && packetEventArray != null && selectedItems.Length != 0 && selectedItems[0] >= 0 && selectedItems[0] < packetEventArray.Count)
		{
			Array<CardActionBehaviorDefinition> array = ClonePacketEventArray(packetEventArray);
			array.RemoveAt(selectedItems[0]);
			_propertyBinding.SetValue(_editingOverride, propertyName, array, "删除卡牌覆盖事件", this, "RefreshPacketOverrideFromHistory");
			RefreshDirectEditPreview();
		}
	}

	private void MoveSelectedPacketEvent(string propertyName, int direction)
	{
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		ItemList itemList = ((propertyName == "useSucceededActions") ? _useSucceededActionsList : _pressedActionsList);
		int[] selectedItems = itemList.GetSelectedItems();
		if (_propertyBinding != null && packetEventArray != null && selectedItems.Length != 0)
		{
			int num = selectedItems[0];
			int num2 = num + Math.Sign(direction);
			if (num >= 0 && num < packetEventArray.Count && num2 >= 0 && num2 < packetEventArray.Count)
			{
				Array<CardActionBehaviorDefinition> array = ClonePacketEventArray(packetEventArray);
				CardActionBehaviorDefinition value = array[num];
				array[num] = array[num2];
				array[num2] = value;
				_propertyBinding.SetValue(_editingOverride, propertyName, array, "调整卡牌覆盖事件顺序", this, "RefreshPacketOverrideFromHistory");
				RefreshDirectEditPreview();
				itemList.Select(num2);
			}
		}
	}

	private void OpenPacketEventResource(string propertyName, int index)
	{
		Array<CardActionBehaviorDefinition> packetEventArray = GetPacketEventArray(propertyName);
		if (packetEventArray != null && index >= 0 && index < packetEventArray.Count)
		{
			CardActionBehaviorDefinition cardActionBehaviorDefinition = packetEventArray[index];
			if (GodotObject.IsInstanceValid(cardActionBehaviorDefinition))
			{
				XWResourceEditContext context = XWResourceEditContext.ForProperty(cardActionBehaviorDefinition, _editingOverride, cardActionBehaviorDefinition.ResourcePath, base.CurrentResourcePath, propertyName, index, "packet_override_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
				XWEditorInterface.Instance?.EditResource(cardActionBehaviorDefinition, context);
			}
		}
	}

	private Array<CardActionBehaviorDefinition> GetPacketEventArray(string propertyName)
	{
		if (!GodotObject.IsInstanceValid(_editingOverride))
		{
			return null;
		}
		if (!(propertyName == "useSucceededActions"))
		{
			return _editingOverride.pressedActions;
		}
		return _editingOverride.useSucceededActions;
	}

	private static Array<CardActionBehaviorDefinition> ClonePacketEventArray(Array<CardActionBehaviorDefinition> source)
	{
		Array<CardActionBehaviorDefinition> array = new Array<CardActionBehaviorDefinition>();
		if (source != null)
		{
			foreach (CardActionBehaviorDefinition item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Editor-only packet event instances are created from discovered runtime types.")]
	private static CardActionBehaviorDefinition CreatePacketEventInstance(Type type)
	{
		try
		{
			return Activator.CreateInstance(type) as CardActionBehaviorDefinition;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Packet override event create failed: " + type?.FullName + " " + ex.Message);
			return null;
		}
	}

	private void AddSummaryRows()
	{
		AddItemIfMissing(base.PreviewList, "真实种子栏卡牌：继承值 → 覆盖结果");
		AddItemIfMissing(base.TimelineList, "卡框 / 费用 / 冷却 / 僵尸参数 → 安全演算");
		AddItemIfMissing(base.GraphList, "卡牌覆盖规则 → 事件 / 植物覆盖 / 角色覆盖（不执行游戏事件）");
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
}
