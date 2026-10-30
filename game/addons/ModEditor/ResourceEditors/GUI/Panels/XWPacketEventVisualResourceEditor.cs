using System;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketEventVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketEventVisualEditorLayout.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private CardActionBehaviorDefinition _editingEvent;

	private XWVisualPropertyBinding _propertyBinding;

	private XWVisualPropertyBinding _levelPacketPropertyBinding;

	private Label _eventType;

	private Label _formHint;

	private Label _outcome;

	private Label _currentName;

	private Label _resultName;

	private Label _deletionStamp;

	private Control _currentCardRoot;

	private Control _resultCardRoot;

	private TowerDefenseInGamePacketShow _currentPacket;

	private TowerDefenseInGamePacketShow _resultPacket;

	private Control _baseFields;

	private Control _changeCostFields;

	private Control _cooldownFields;

	private Control _changePacketFields;

	private LineEdit _resourceNameEdit;

	private CheckButton _localToSceneCheck;

	private OptionButton _costMethod;

	private XWVisualSegmentedOption _costMethodVisualChoices;

	private SpinBox _costValue;

	private SpinBox _costMin;

	private SpinBox _costMax;

	private SpinBox _cooldownValue;

	private LineEdit _packetName;

	private SpinBox _replaceCount;

	private SpinBox _initialCost;

	private SpinBox _cooldownDuration;

	private SpinBox _simulatedUses;

	private Tween _previewTween;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposeEventBindings();
		if (GodotObject.IsInstanceValid(_previewTween))
		{
			_previewTween.Kill();
		}
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeEventBindings();
		if (base.CurrentResource is CardActionBehaviorDefinition packetEvent && base.CanvasGrid != null)
		{
			RenderPacketEventWorkbench(packetEvent);
		}
	}

	private void RenderPacketEventWorkbench(CardActionBehaviorDefinition packetEvent)
	{
		_editingEvent = packetEvent;
		base.CanvasGrid.Columns = 1;
		if (_visualEditorLayoutScene == null)
		{
			_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketEventVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			BindWorkbench(vBoxContainer);
			PopulateEventFields();
			BindEventProperties(vBoxContainer, packetEvent);
			UpdateSimulationPreview(applied: false);
			AddSummaryRows();
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(CardActionBehaviorDefinition)) && !(type == typeof(CardActionBehaviorChangeCost)) && !(type == typeof(CardActionBehaviorSetCooldown)) && !(type == typeof(CardActionBehaviorChangePacket)) && !(type == typeof(CardActionBehaviorDelete)))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_eventType = root.GetNode<Label>("%EventType");
		_formHint = root.GetNode<Label>("%FormHint");
		_outcome = root.GetNode<Label>("%Outcome");
		_currentName = root.GetNode<Label>("%CurrentName");
		_resultName = root.GetNode<Label>("%ResultName");
		_deletionStamp = root.GetNode<Label>("%DeletionStamp");
		_currentCardRoot = root.GetNode<Control>("%CurrentCardRoot");
		_resultCardRoot = root.GetNode<Control>("%ResultCardRoot");
		_currentPacket = root.GetNode<TowerDefenseInGamePacketShow>("%CurrentPacket");
		_resultPacket = root.GetNode<TowerDefenseInGamePacketShow>("%ResultPacket");
		_baseFields = root.GetNode<Control>("%BaseFields");
		_changeCostFields = root.GetNode<Control>("%ChangeCostFields");
		_cooldownFields = root.GetNode<Control>("%CooldownFields");
		_changePacketFields = root.GetNode<Control>("%ChangePacketFields");
		_resourceNameEdit = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToSceneCheck = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_costMethod = root.GetNode<OptionButton>("%CostMethod");
		_costMethodVisualChoices = new XWVisualSegmentedOption(_costMethod, root.GetNode<HFlowContainer>("%CostMethodVisualChoices"));
		_costMethodVisualChoices.Rebuild();
		_costValue = root.GetNode<SpinBox>("%CostValue");
		_costMin = root.GetNode<SpinBox>("%CostMin");
		_costMax = root.GetNode<SpinBox>("%CostMax");
		_cooldownValue = root.GetNode<SpinBox>("%CooldownValue");
		_packetName = root.GetNode<LineEdit>("%PacketName");
		_replaceCount = root.GetNode<SpinBox>("%ReplaceCount");
		_initialCost = root.GetNode<SpinBox>("%InitialCost");
		_cooldownDuration = root.GetNode<SpinBox>("%CooldownDuration");
		_simulatedUses = root.GetNode<SpinBox>("%SimulatedUses");
		_currentPacket.onlyDraw = true;
		_currentPacket.useCost = false;
		_resultPacket.onlyDraw = true;
		_resultPacket.useCost = false;
		root.GetNode<Button>("%TriggerButton").Pressed += ApplyEventPreview;
		root.GetNode<Button>("%ResetButton").Pressed += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_initialCost.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_cooldownDuration.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
		_simulatedUses.ValueChanged += delegate
		{
			UpdateSimulationPreview(applied: false);
		};
	}

	private void BindEventProperties(VBoxContainer root, CardActionBehaviorDefinition packetEvent)
	{
		LineEdit resourceNameEdit = _resourceNameEdit;
		CheckButton localToSceneCheck = _localToSceneCheck;
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnVisualPropertyEdited);
		_propertyBinding.BindText(resourceNameEdit, packetEvent, "resource_name", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		_propertyBinding.BindToggle(localToSceneCheck, packetEvent, "resource_local_to_scene", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		CardActionBehaviorChangeCost cardActionBehaviorChangeCost = packetEvent as CardActionBehaviorChangeCost;
		if (cardActionBehaviorChangeCost == null)
		{
			if (!(packetEvent is CardActionBehaviorSetCooldown resource))
			{
				if (packetEvent is CardActionBehaviorChangePacket cardActionBehaviorChangePacket)
				{
					_propertyBinding.BindNumber(_replaceCount, cardActionBehaviorChangePacket, "count", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
					MountLevelPacketConfigPicker(root.GetNode<HBoxContainer>("%LevelPacketConfigPickerHost"), cardActionBehaviorChangePacket);
					BindLevelPacketConfigFields(cardActionBehaviorChangePacket.levelPacketConfig);
				}
			}
			else
			{
				_propertyBinding.BindNumber(_cooldownValue, resource, "value", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
			}
			return;
		}
		_propertyBinding.BindNumber(_costValue, cardActionBehaviorChangeCost, "value", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		_propertyBinding.BindNumber(_costMin, cardActionBehaviorChangeCost, "_min", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		_propertyBinding.BindNumber(_costMax, cardActionBehaviorChangeCost, "_max", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		_costMethod.ItemSelected += delegate(long index)
		{
			if (!_updatingControls && _propertyBinding != null)
			{
				_propertyBinding.SetValue(cardActionBehaviorChangeCost, "method", _costMethod.GetItemId((int)index), "修改费用运算", this, "RefreshPacketEventEditorFromHistory");
			}
		};
	}

	private void PopulateEventFields()
	{
		if (!GodotObject.IsInstanceValid(_editingEvent) || !GodotObject.IsInstanceValid(_baseFields))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_eventType.Text = _editingEvent.GetType().Name;
			bool flag = _editingEvent is CardActionBehaviorChangeCost;
			bool flag2 = _editingEvent is CardActionBehaviorSetCooldown;
			bool flag3 = _editingEvent is CardActionBehaviorChangePacket;
			_baseFields.Visible = !flag && !flag2 && !flag3;
			_changeCostFields.Visible = flag;
			_cooldownFields.Visible = flag2;
			_changePacketFields.Visible = flag3;
			if (_editingEvent is CardActionBehaviorChangeCost cardActionBehaviorChangeCost)
			{
				SelectOptionById(_costMethod, (int)cardActionBehaviorChangeCost.method);
				_costMethodVisualChoices?.RefreshSelection();
				_costValue.Value = cardActionBehaviorChangeCost.value;
				_costMin.Value = cardActionBehaviorChangeCost._min;
				_costMax.Value = cardActionBehaviorChangeCost._max;
			}
			if (_editingEvent is CardActionBehaviorSetCooldown cardActionBehaviorSetCooldown)
			{
				_cooldownValue.Value = cardActionBehaviorSetCooldown.value;
			}
			if (_editingEvent is CardActionBehaviorChangePacket cardActionBehaviorChangePacket)
			{
				_packetName.Text = cardActionBehaviorChangePacket.levelPacketConfig?.packetName ?? "";
				_replaceCount.Value = Mathf.Max(1, cardActionBehaviorChangePacket.count);
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void MountLevelPacketConfigPicker(HBoxContainer host, CardActionBehaviorChangePacket changePacket)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = "LevelPacketConfigPicker";
		xWResourcePicker.Setup("TowerDefenseLevelPacketConfig");
		xWResourcePicker.SetEditedResource(changePacket.levelPacketConfig);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		xWResourcePicker.ResourceChanged += delegate(Resource resource)
		{
			if (!_updatingControls && _propertyBinding != null)
			{
				_propertyBinding.SetValue(changePacket, "levelPacketConfig", resource as TowerDefenseLevelPacketConfig, "更换替换卡牌配置", this, "RefreshPacketEventEditorFromHistory");
				RebuildPacketEventEditor();
			}
		};
		host.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
	}

	private void BindLevelPacketConfigFields(TowerDefenseLevelPacketConfig levelPacketConfig)
	{
		_levelPacketPropertyBinding?.Dispose();
		_levelPacketPropertyBinding = null;
		_packetName.Editable = GodotObject.IsInstanceValid(levelPacketConfig);
		if (!GodotObject.IsInstanceValid(levelPacketConfig))
		{
			_packetName.Text = "";
			_packetName.PlaceholderText = "请先新建或选择替换卡牌配置";
		}
		else
		{
			_packetName.PlaceholderText = "例如 PlantPeashooter";
			_levelPacketPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnVisualPropertyEdited);
			_levelPacketPropertyBinding.BindText(_packetName, levelPacketConfig, "packetName", RefreshPreviewAfterPropertyChange, this, "RefreshPacketEventEditorFromHistory");
		}
	}

	private void OnVisualPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(_editingEvent) && _editingEvent == base.CurrentResource)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			_editingEvent.EmitChanged();
		}
	}

	private void RefreshPreviewAfterPropertyChange()
	{
		UpdateSimulationPreview(applied: false);
	}

	public void RefreshPacketEventEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			RebuildPacketEventEditor();
		}
	}

	private void RebuildPacketEventEditor()
	{
		if (base.CanvasGrid == null || !(base.CurrentResource is CardActionBehaviorDefinition packetEvent))
		{
			return;
		}
		DisposeEventBindings();
		if (GodotObject.IsInstanceValid(_previewTween))
		{
			_previewTween.Kill();
			_previewTween = null;
		}
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderPacketEventWorkbench(packetEvent);
	}

	private void DisposeEventBindings()
	{
		_costMethodVisualChoices?.Dispose();
		_costMethodVisualChoices = null;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_levelPacketPropertyBinding?.Dispose();
		_levelPacketPropertyBinding = null;
		_editingEvent = null;
	}

	private void ApplyEventPreview()
	{
		UpdateSimulationPreview(applied: true);
		if (GodotObject.IsInstanceValid(_resultCardRoot))
		{
			if (GodotObject.IsInstanceValid(_previewTween))
			{
				_previewTween.Kill();
			}
			_resultCardRoot.Modulate = Colors.White;
			_resultCardRoot.Scale = Vector2.One * 0.78f;
			_previewTween = CreateTween();
			_previewTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			_previewTween.TweenProperty(_resultCardRoot, "scale", Vector2.One * 1.08f, 0.22);
			_previewTween.TweenProperty(_resultCardRoot, "scale", Vector2.One, 0.14);
			if (_editingEvent is CardActionBehaviorDelete)
			{
				_previewTween.TweenProperty(_resultCardRoot, "modulate:a", 0.08f, 0.35);
			}
		}
	}

	private void UpdateSimulationPreview(bool applied)
	{
		if (!GodotObject.IsInstanceValid(_editingEvent) || !GodotObject.IsInstanceValid(_currentPacket))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_previewTween))
		{
			_previewTween.Kill();
			_previewTween = null;
		}
		long num = Math.Max(0, Mathf.RoundToInt(_initialCost.Value));
		double num2 = Math.Max(0.1, _cooldownDuration.Value);
		int num3 = Math.Max(1, Mathf.RoundToInt(_simulatedUses.Value));
		ConfigurePacketCard(_currentPacket, num, num2, cooling: false, 0.0);
		ConfigurePacketCard(_resultPacket, num, num2, cooling: false, 0.0);
		_currentName.Text = "当前卡牌";
		_resultName.Text = "结果卡牌";
		_resultCardRoot.Visible = true;
		_resultCardRoot.Modulate = Colors.White;
		_resultCardRoot.Scale = Vector2.One;
		_deletionStamp.Visible = false;
		if (!applied)
		{
			_outcome.Text = BuildPreviewDescription(num, num2, num3);
			_formHint.Text = "调整事件参数与模拟状态后触发；预演只计算并更新这两张真实游戏卡片。";
			return;
		}
		CardActionBehaviorDefinition editingEvent = _editingEvent;
		if (!(editingEvent is CardActionBehaviorChangeCost cardActionBehaviorChangeCost))
		{
			if (!(editingEvent is CardActionBehaviorSetCooldown cardActionBehaviorSetCooldown))
			{
				if (!(editingEvent is CardActionBehaviorChangePacket cardActionBehaviorChangePacket))
				{
					if (!(editingEvent is CardActionBehaviorDelete))
					{
						if (editingEvent != null && _editingEvent.GetType() == typeof(CardActionBehaviorDefinition))
						{
							_resultPacket.select = true;
							_resultPacket.ColorSet();
							_outcome.Text = "基础卡牌事件没有额外业务字段；资源属性可在上方直接编辑。";
						}
						else
						{
							_resultPacket.select = true;
							_resultPacket.ColorSet();
							_outcome.Text = "自定义卡牌事件已触发视觉提示；实际 Mod 字段由右侧检查器编辑。";
						}
					}
					else
					{
						_deletionStamp.Visible = true;
						_resultName.Text = "卡牌已删除";
						_outcome.Text = "该卡牌会从当前种子栏 packetList 移除并释放。";
					}
				}
				else
				{
					int num4 = Math.Max(1, cardActionBehaviorChangePacket.count);
					bool flag = num3 >= num4;
					string text = EmptyName(cardActionBehaviorChangePacket.levelPacketConfig?.packetName, "未配置替换卡牌");
					_resultName.Text = (flag ? text : $"原卡牌（{num3}/{num4}）");
					_resultPacket.select = flag;
					_resultPacket.ColorSet();
					_outcome.Text = (flag ? $"第 {num3} 次使用达到阈值，卡牌替换为 {text}。" : $"还需使用 {num4 - num3} 次，当前卡牌保持不变。");
				}
			}
			else
			{
				double remaining = Mathf.Clamp(cardActionBehaviorSetCooldown.value, 0.0, num2);
				ConfigurePacketCard(_resultPacket, num, num2, cooling: true, remaining);
				_resultName.Text = $"冷却计时 {cardActionBehaviorSetCooldown.value:0.##} 秒";
				_outcome.Text = $"卡片进入冷却，当前计时器设置为 {cardActionBehaviorSetCooldown.value:0.##} 秒（预览按总冷却 {num2:0.##} 秒裁切）。";
			}
		}
		else
		{
			long num5 = CalculateResultCost(num, cardActionBehaviorChangeCost);
			ConfigurePacketCard(_resultPacket, num5, num2, cooling: false, 0.0);
			_resultName.Text = $"费用 {num} → {num5}";
			_outcome.Text = $"卡牌费用已按 {cardActionBehaviorChangeCost.method} 运算，并应用最低/最高费用限制。";
		}
	}

	private string BuildPreviewDescription(long initialCost, double totalCooldown, int simulatedUses)
	{
		CardActionBehaviorDefinition editingEvent = _editingEvent;
		if (!(editingEvent is CardActionBehaviorChangeCost condition))
		{
			if (!(editingEvent is CardActionBehaviorSetCooldown cardActionBehaviorSetCooldown))
			{
				if (!(editingEvent is CardActionBehaviorChangePacket cardActionBehaviorChangePacket))
				{
					if (editingEvent is CardActionBehaviorDelete)
					{
						return "触发后卡牌会从种子栏消失。";
					}
					return "这是基础或 Mod 自定义卡牌事件；点击触发查看安全视觉提示。";
				}
				return $"第 {Math.Max(1, cardActionBehaviorChangePacket.count)} 次使用后替换为 {EmptyName(cardActionBehaviorChangePacket.levelPacketConfig?.packetName, "未配置卡牌")}；当前模拟第 {simulatedUses} 次。";
			}
			return $"将当前冷却计时设置为 {cardActionBehaviorSetCooldown.value:0.##} 秒；模拟总冷却 {totalCooldown:0.##} 秒。";
		}
		return $"预计费用：{initialCost} → {CalculateResultCost(initialCost, condition)}；点击触发查看卡片变化。";
	}

	private static long CalculateResultCost(long initialCost, CardActionBehaviorChangeCost condition)
	{
		long num = ((condition.method != CardActionBehaviorChangeCost.METHOD.MULTIPLY) ? (initialCost + (long)condition.value) : ((long)Mathf.Floor((double)initialCost * condition.value)));
		long num2 = num;
		if (condition._min != -1 && num2 < condition._min)
		{
			num2 = condition._min;
		}
		if (condition._max != -1 && num2 > condition._max)
		{
			num2 = condition._max;
		}
		return num2;
	}

	private static void ConfigurePacketCard(TowerDefenseInGamePacketShow packet, long cost, double totalCooldown, bool cooling, double remaining)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.onlyDraw = true;
			packet.useCost = false;
			packet.baseItemCost = cost;
			packet.itemCost = cost;
			packet.alive = !cooling;
			packet.@lock = false;
			packet.openShadow = cooling;
			packet.select = false;
			packet.coldDownOpen = cooling;
			packet.coldDown = totalCooldown;
			packet.coldDownTimer = remaining;
			if (GodotObject.IsInstanceValid(packet.coldDownProgressBar))
			{
				packet.coldDownProgressBar.MaxValue = totalCooldown;
				packet.coldDownProgressBar.Value = remaining;
				packet.coldDownProgressBar.Visible = cooling;
			}
			packet.ColorSet();
		}
	}

	private static void SelectOptionById(OptionButton option, int id)
	{
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == id)
			{
				option.Select(i);
				break;
			}
		}
	}

	private static string EmptyName(string value, string fallback)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return fallback;
	}

	private void AddSummaryRows()
	{
		AddItemIfMissing(base.PreviewList, "卡牌事件 -> " + _editingEvent.GetType().Name);
		AddItemIfMissing(base.TimelineList, "种子栏触发前 -> 事件计算 -> 触发后卡片");
		AddItemIfMissing(base.GraphList, "卡牌事件 -> 费用 / 冷却 / 替换 / 删除（不调用 Execute）");
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
