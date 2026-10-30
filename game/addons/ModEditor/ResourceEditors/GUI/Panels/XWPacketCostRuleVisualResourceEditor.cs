using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketCostRuleVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketCostRuleVisualEditorLayout.tscn";

	private static PackedScene _visualEditorLayoutScene;

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

	private static readonly System.Collections.Generic.Dictionary<string, Texture2D> PacketTextureCache = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private TowerDefensePacketChangeCost _editingCondition;

	private TowerDefenseEnum.PACKET_TYPE _selectedPacketType;

	private Label _ruleKeyBadge;

	private Label _ruleState;

	private Label _currentName;

	private Label _resultName;

	private Control _resultCardRoot;

	private TowerDefenseInGamePacketShow _currentPacket;

	private TowerDefenseInGamePacketShow _resultPacket;

	private SpinBox _initialCost;

	private OptionButton _packetType;

	private XWVisualOptionGallery _packetTypeVisualGallery;

	private CheckBox _canChangeCost;

	private OptionButton _method;

	private XWVisualSegmentedOption _methodVisualChoices;

	private SpinBox _amount;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private LineEdit _key;

	private CheckBox _lockCost;

	private CheckBox _skip;

	private XWVisualPropertyBinding _propertyBinding;

	private Tween _previewTween;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposePacketCostRuleBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposePacketCostRuleBinding();
		if (base.CurrentResource is TowerDefensePacketChangeCost towerDefensePacketChangeCost && base.CanvasGrid != null)
		{
			_editingCondition = towerDefensePacketChangeCost;
			base.CanvasGrid.Columns = 1;
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketCostRuleVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindWorkbench(vBoxContainer, towerDefensePacketChangeCost);
				PopulateConditionFields();
				UpdateSimulationPreview(applied: false);
				AddSummaryRows();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompletePacketCostRuleVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompletePacketCostRuleVisualCoverage(Resource resource)
	{
		return resource?.GetType() == typeof(TowerDefensePacketChangeCost);
	}

	private void DisposePacketCostRuleBinding()
	{
		_packetTypeVisualGallery?.Dispose();
		_packetTypeVisualGallery = null;
		_methodVisualChoices?.Dispose();
		_methodVisualChoices = null;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingCondition = null;
	}

	private void BindWorkbench(VBoxContainer root, TowerDefensePacketChangeCost condition)
	{
		_ruleKeyBadge = root.GetNode<Label>("%RuleKeyBadge");
		_ruleState = root.GetNode<Label>("%RuleState");
		_currentName = root.GetNode<Label>("%CurrentName");
		_resultName = root.GetNode<Label>("%ResultName");
		_resultCardRoot = root.GetNode<Control>("%ResultCardRoot");
		_currentPacket = root.GetNode<TowerDefenseInGamePacketShow>("%CurrentPacket");
		_resultPacket = root.GetNode<TowerDefenseInGamePacketShow>("%ResultPacket");
		_initialCost = root.GetNode<SpinBox>("%InitialCost");
		_packetType = root.GetNode<OptionButton>("%PacketType");
		_canChangeCost = root.GetNode<CheckBox>("%CanChangeCost");
		_method = root.GetNode<OptionButton>("%Method");
		_amount = root.GetNode<SpinBox>("%Amount");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_key = root.GetNode<LineEdit>("%Key");
		_lockCost = root.GetNode<CheckBox>("%LockCost");
		_skip = root.GetNode<CheckBox>("%Skip");
		BindDirectEditControls(condition);
		TowerDefenseEnum.PACKET_TYPE[] packetTypes = PacketTypes;
		foreach (TowerDefenseEnum.PACKET_TYPE pACKET_TYPE in packetTypes)
		{
			_packetType.AddItem(GetPacketTypeDisplayName(pACKET_TYPE), (int)pACKET_TYPE);
		}
		_packetTypeVisualGallery = new XWVisualOptionGallery(_packetType, root.GetNode<HFlowContainer>("%PacketTypeVisualChoices"), "PacketTypeVisualCatalog", (int index) => LoadPacketTexture((TowerDefenseEnum.PACKET_TYPE)_packetType.GetItemId(index)));
		_packetTypeVisualGallery.Rebuild();
		_methodVisualChoices = new XWVisualSegmentedOption(_method, root.GetNode<HFlowContainer>("%MethodVisualChoices"));
		_methodVisualChoices.Rebuild();
		ConfigurePacketCard(_currentPacket, 100, _selectedPacketType);
		ConfigurePacketCard(_resultPacket, 100, _selectedPacketType);
		root.GetNode<Button>("%ApplyButton").Pressed += ApplyCostRulePreview;
		root.GetNode<Button>("%ResetButton").Pressed += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_initialCost.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_canChangeCost.Toggled += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_packetType.ItemSelected += OnPacketTypeSelected;
		BindConditionSignals();
	}

	private void BindDirectEditControls(TowerDefensePacketChangeCost condition)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnPacketCostRulePropertyEdited);
		_propertyBinding.BindText(_resourceName, condition, "resource_name", RefreshDirectEditPreview, this, "RefreshPacketCostRuleFromHistory");
		_propertyBinding.BindToggle(_localToScene, condition, "resource_local_to_scene", RefreshDirectEditPreview, this, "RefreshPacketCostRuleFromHistory");
		_propertyBinding.BindText(_key, condition, "key", RefreshDirectEditPreview, this, "RefreshPacketCostRuleFromHistory");
		_propertyBinding.BindToggle(_lockCost, condition, "lockCost", RefreshDirectEditPreview, this, "RefreshPacketCostRuleFromHistory");
		_propertyBinding.BindToggle(_skip, condition, "skip", RefreshDirectEditPreview, this, "RefreshPacketCostRuleFromHistory");
	}

	private void BindConditionSignals()
	{
		_method.ItemSelected += delegate(long index)
		{
			if (!_updatingControls && _propertyBinding != null && GodotObject.IsInstanceValid(_editingCondition))
			{
				TowerDefensePacketChangeCost editingCondition = _editingCondition;
				_propertyBinding.SetValue(editingCondition, "method", _method.GetItemText((int)index), "修改费用修正方式", this, "RefreshPacketCostRuleFromHistory");
			}
		};
		_amount.FocusEntered += BeginSelectedAmountEdit;
		_amount.ValueChanged += PreviewSelectedAmount;
		_amount.FocusExited += CommitSelectedAmountEdit;
	}

	private void BeginSelectedAmountEdit()
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_editingCondition))
		{
			_propertyBinding.BeginEdit(_editingCondition, "amontDictionary");
		}
	}

	private void PreviewSelectedAmount(double value)
	{
		if (!_updatingControls && _propertyBinding != null && GodotObject.IsInstanceValid(_editingCondition))
		{
			Dictionary dictionary = ((_editingCondition.amontDictionary == null) ? new Dictionary() : _editingCondition.amontDictionary.Duplicate());
			dictionary[(int)_selectedPacketType] = Mathf.RoundToInt(value);
			_propertyBinding.PreviewValue(_editingCondition, "amontDictionary", dictionary);
		}
	}

	private void CommitSelectedAmountEdit()
	{
		if (_propertyBinding != null && GodotObject.IsInstanceValid(_editingCondition))
		{
			_propertyBinding.CommitEdit(_editingCondition, "amontDictionary", _editingCondition.amontDictionary, "修改" + GetPacketTypeDisplayName(_selectedPacketType) + "费用", this, "RefreshPacketCostRuleFromHistory");
		}
	}

	private void OnPacketTypeSelected(long index)
	{
		if (index >= 0 && index < _packetType.ItemCount)
		{
			_selectedPacketType = (TowerDefenseEnum.PACKET_TYPE)_packetType.GetItemId((int)index);
			PopulateSelectedAmount();
			UpdateSimulationPreview(applied: false);
		}
	}

	private void PopulateConditionFields()
	{
		if (!GodotObject.IsInstanceValid(_editingCondition) || !GodotObject.IsInstanceValid(_method))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			SelectOptionByText(_method, _editingCondition.method);
			_methodVisualChoices?.RefreshSelection();
			PopulateSelectedAmount();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void PopulateSelectedAmount()
	{
		if (!GodotObject.IsInstanceValid(_editingCondition) || !GodotObject.IsInstanceValid(_amount))
		{
			return;
		}
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		try
		{
			int selectedPacketType = (int)_selectedPacketType;
			_amount.Value = (TryGetAmount(_editingCondition, selectedPacketType, out var amount) ? amount : 0);
		}
		finally
		{
			_updatingControls = updatingControls;
		}
	}

	private void OnPacketCostRulePropertyEdited(bool committed)
	{
		if (base.CurrentResource is TowerDefensePacketChangeCost towerDefensePacketChangeCost && towerDefensePacketChangeCost == _editingCondition)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				towerDefensePacketChangeCost.EmitChanged();
			}
			UpdateSimulationPreview(applied: false);
		}
	}

	private void RefreshDirectEditPreview()
	{
		UpdateSimulationPreview(applied: false);
	}

	public void RefreshPacketCostRuleFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is TowerDefensePacketChangeCost towerDefensePacketChangeCost && towerDefensePacketChangeCost == _editingCondition)
		{
			BindDirectEditControls(towerDefensePacketChangeCost);
			PopulateConditionFields();
			UpdateSimulationPreview(applied: false);
		}
	}

	private void ApplyCostRulePreview()
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
		if (GodotObject.IsInstanceValid(_editingCondition) && GodotObject.IsInstanceValid(_currentPacket))
		{
			if (GodotObject.IsInstanceValid(_previewTween))
			{
				_previewTween.Kill();
				_previewTween = null;
			}
			int num = Mathf.RoundToInt(_initialCost.Value);
			bool buttonPressed = _canChangeCost.ButtonPressed;
			int num2 = (applied ? CalculateResultCost(num, _editingCondition, _selectedPacketType, buttonPressed) : num);
			ConfigurePacketCard(_currentPacket, num, _selectedPacketType);
			ConfigurePacketCard(_resultPacket, num2, _selectedPacketType);
			_currentName.Text = $"费用 {num}";
			_resultName.Text = (applied ? $"费用 {num} → {num2}" : $"费用 {num}");
			_ruleKeyBadge.Text = (string.IsNullOrWhiteSpace(_editingCondition.key) ? "未命名规则" : _editingCondition.key);
			_ruleState.Text = BuildRuleStateText(num, num2, buttonPressed, applied);
			_resultCardRoot.Scale = Vector2.One;
		}
	}

	private string BuildRuleStateText(int initialCost, int resultCost, bool canChangeCost, bool applied)
	{
		int amount;
		string value = (TryGetAmount(_editingCondition, (int)_selectedPacketType, out amount) ? amount.ToString() : "未配置");
		string value2 = (_editingCondition.lockCost ? " · 应用后锁定后续增费" : "") + (_editingCondition.skip ? " · 应用后跳过剩余规则" : "");
		if (applied)
		{
			if (_editingCondition.method == "Increase" && !canChangeCost)
			{
				return $"卡牌已禁止增加费用，Increase 被拦截：{initialCost} → {resultCost}{value2}";
			}
			return $"{GetPacketTypeDisplayName(_selectedPacketType)} 已执行 {_editingCondition.method}：{initialCost} → {resultCost}{value2}";
		}
		return $"{GetPacketTypeDisplayName(_selectedPacketType)}：{_editingCondition.method} {value}；点击应用查看真实卡框费用变化{value2}";
	}

	private static int CalculateResultCost(int initialCost, TowerDefensePacketChangeCost condition, TowerDefenseEnum.PACKET_TYPE packetType, bool canChangeCost)
	{
		if (!TryGetAmount(condition, (int)packetType, out var amount))
		{
			return initialCost;
		}
		switch (condition.method)
		{
		case "Increase":
			if (canChangeCost)
			{
				return initialCost + amount;
			}
			break;
		case "Decrease":
			return initialCost - amount;
		case "Set":
			return amount;
		}
		return initialCost;
	}

	private static bool TryGetAmount(TowerDefensePacketChangeCost condition, int typeKey, out int amount)
	{
		amount = 0;
		if (condition?.amontDictionary == null || !condition.amontDictionary.ContainsKey(typeKey))
		{
			return false;
		}
		amount = condition.amontDictionary[typeKey].AsInt32();
		return true;
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

	private static void SelectOptionByText(OptionButton option, string text)
	{
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (string.Equals(option.GetItemText(i), text, StringComparison.Ordinal))
			{
				option.Select(i);
				return;
			}
		}
		option.Select(0);
	}

	private void AddSummaryRows()
	{
		AddItemIfMissing(base.PreviewList, "真实种子栏卡框：费用修正前 → 修正后");
		AddItemIfMissing(base.TimelineList, "选择卡框 → 编辑该类型数值 → 安全演算");
		AddItemIfMissing(base.GraphList, "卡牌费用规则 → Increase / Decrease / Set（不调用 Execute）");
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
