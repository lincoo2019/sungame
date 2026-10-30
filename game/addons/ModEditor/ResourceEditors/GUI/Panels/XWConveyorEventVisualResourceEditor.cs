using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWConveyorEventVisualResourceEditor : XWGenericVisualResourceEditor
{
	private TowerDefenseConveyorEventAddPacket _event;

	private XWVisualPropertyBinding _eventBinding;

	private XWVisualPropertyBinding _packetBinding;

	private XWResourcePicker _packetPicker;

	private Label _previewPacket;

	private Label _previewRules;

	private ProgressBar _weightBar;

	private VBoxContainer _packetFields;

	private bool _refreshQueued;

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeBindings();
		if (base.CurrentResource is TowerDefenseConveyorEventAddPacket towerDefenseConveyorEventAddPacket && base.CanvasGrid != null)
		{
			_event = towerDefenseConveyorEventAddPacket;
			base.CanvasGrid.Columns = 1;
			base.CanvasGrid.AddChild(BuildWorkbench(), forceReadableName: false, InternalMode.Disabled);
			BindEvent();
			RefreshFromHistory();
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource?.GetType() != typeof(TowerDefenseConveyorEventAddPacket))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	public override void _ExitTree()
	{
		DisposeBindings();
		base._ExitTree();
	}

	private Control BuildWorkbench()
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.AddThemeConstantOverride("separation", 12);
		PanelContainer panelContainer = Panel("传送带波次 · 加入卡牌", new Color(0.08f, 0.19f, 0.12f));
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddChild(new Label
		{
			Text = "\ud83c\udf0a  大波次触发",
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Text = "事件执行后，配置的卡牌会成为传送带候选卡。",
			Modulate = new Color(0.75f, 0.88f, 0.76f)
		}, forceReadableName: false, InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		PanelContainer panelContainer2 = Panel("", new Color(0.045f, 0.1f, 0.075f));
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 18);
		hBoxContainer.AddChild(StageBadge("旗帜波次", new Color(0.64f, 0.2f, 0.15f)), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "➜",
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer3 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_previewPacket = new Label
		{
			Text = "未配置卡牌",
			ThemeTypeVariation = "HeaderMedium"
		};
		_previewRules = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_weightBar = new ProgressBar
		{
			MinValue = 0.0,
			MaxValue = 100.0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0f, 16f)
		};
		vBoxContainer3.AddChild(_previewPacket, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer3.AddChild(_weightBar, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer3.AddChild(_previewRules, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		panelContainer2.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(panelContainer2, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer4 = Section("事件资源");
		LineEdit lineEdit = new LineEdit
		{
			PlaceholderText = "资源名称"
		};
		lineEdit.Name = "ResourceNameEdit";
		CheckButton checkButton = new CheckButton
		{
			Text = "仅场景本地"
		};
		checkButton.Name = "LocalToSceneCheck";
		vBoxContainer4.AddChild(Field("资源名", lineEdit), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer4.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(vBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer5 = Section("候选卡牌（直接编辑）");
		_packetPicker = XWResourcePicker.Create();
		_packetPicker.Setup("TowerDefenseConveyorPacketConfig");
		_packetPicker.ResourceChanged += OnPacketResourceChanged;
		_packetPicker.ResourceSelected += OpenPacketEditor;
		vBoxContainer5.AddChild(Field("卡牌条目", _packetPicker), forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		Button button = new Button
		{
			Text = "＋ 新建默认条目"
		};
		Button button2 = new Button
		{
			Text = "\ud83c\udfb4 打开完整卡牌条目"
		};
		button.Pressed += CreateDefaultPacket;
		button2.Pressed += delegate
		{
			OpenPacketEditor(_event?.packet);
		};
		hBoxContainer2.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer5.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_packetFields = new VBoxContainer();
		_packetFields.AddThemeConstantOverride("separation", 7);
		vBoxContainer5.AddChild(_packetFields, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(vBoxContainer5, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private void BindEvent()
	{
		_eventBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnEdited);
		_eventBinding.BindText(FindChild("ResourceNameEdit", recursive: true, owned: false) as LineEdit, _event, "resource_name", QueueRefreshFromHistory, this, "RefreshFromHistory");
		_eventBinding.BindToggle(FindChild("LocalToSceneCheck", recursive: true, owned: false) as CheckButton, _event, "resource_local_to_scene", QueueRefreshFromHistory, this, "RefreshFromHistory");
		_packetPicker.SetEditedResource(_event.packet);
		BuildPacketFields();
	}

	private void BuildPacketFields()
	{
		_packetBinding?.Dispose();
		_packetBinding = null;
		foreach (Node child in _packetFields.GetChildren())
		{
			child.QueueFree();
		}
		if (!GodotObject.IsInstanceValid(_event?.packet))
		{
			_packetFields.AddChild(new Label
			{
				Text = "先选择或新建卡牌条目。",
				Modulate = Colors.Gold
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		_packetBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnEdited);
		LineEdit control = new LineEdit
		{
			Name = "ConveyorPacketNameEdit"
		};
		SpinBox spinBox = Number(0.0, 100000.0, 1.0);
		spinBox.Name = "ConveyorPacketWeightSpin";
		SpinBox control2 = Number(-1.0, 999.0, 1.0);
		SpinBox control3 = Number(-1.0, 999.0, 1.0);
		SpinBox control4 = Number(0.0, 100.0, 0.05);
		SpinBox control5 = Number(0.0, 100.0, 0.05);
		_packetFields.AddChild(Field("卡牌键", control), forceReadableName: false, InternalMode.Disabled);
		_packetFields.AddChild(Field("抽取权重", spinBox), forceReadableName: false, InternalMode.Disabled);
		_packetFields.AddChild(Field("场上最少数量", control2), forceReadableName: false, InternalMode.Disabled);
		_packetFields.AddChild(Field("场上最多数量", control3), forceReadableName: false, InternalMode.Disabled);
		_packetFields.AddChild(Field("最少数量倍率", control4), forceReadableName: false, InternalMode.Disabled);
		_packetFields.AddChild(Field("最多数量倍率", control5), forceReadableName: false, InternalMode.Disabled);
		_packetBinding.BindText(control, _event.packet, "name", RefreshPreview, this, "RefreshFromHistory");
		_packetBinding.BindNumber(spinBox, _event.packet, "weight", RefreshPreview, this, "RefreshFromHistory");
		_packetBinding.BindNumber(control2, _event.packet, "minNum", RefreshPreview, this, "RefreshFromHistory");
		_packetBinding.BindNumber(control3, _event.packet, "maxNum", RefreshPreview, this, "RefreshFromHistory");
		_packetBinding.BindNumber(control4, _event.packet, "minMagnification", RefreshPreview, this, "RefreshFromHistory");
		_packetBinding.BindNumber(control5, _event.packet, "maxMagnification", RefreshPreview, this, "RefreshFromHistory");
	}

	private void OnPacketResourceChanged(Resource resource)
	{
		_eventBinding?.SetValue(_event, "packet", resource as TowerDefenseConveyorPacketConfig, "更换传送带候选卡牌", this, "RefreshFromHistory");
		RefreshFromHistory();
	}

	private void CreateDefaultPacket()
	{
		OnPacketResourceChanged(new TowerDefenseConveyorPacketConfig
		{
			name = "NewPacket",
			weight = 10
		});
	}

	private void OpenPacketEditor(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForProperty(resource, _event, resource.ResourcePath, base.CurrentResourcePath, "packet", -1, "packet_spawn_entry_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(base.CurrentResourcePath)));
		}
	}

	public void RefreshFromHistory()
	{
		_refreshQueued = false;
		if (GodotObject.IsInstanceValid(_event))
		{
			_packetPicker?.SetEditedResource(_event.packet);
			BuildPacketFields();
			RefreshPreview();
		}
	}

	private void QueueRefreshFromHistory()
	{
		if (!_refreshQueued)
		{
			_refreshQueued = true;
			CallDeferred("RefreshFromHistory");
		}
	}

	private void RefreshPreview()
	{
		TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig = _event?.packet;
		bool flag = GodotObject.IsInstanceValid(towerDefenseConveyorPacketConfig);
		_previewPacket.Text = ((flag && !string.IsNullOrWhiteSpace(towerDefenseConveyorPacketConfig.name)) ? ("\ud83c\udfb4  " + towerDefenseConveyorPacketConfig.name) : "⚠ 未配置卡牌键");
		_weightBar.Value = (flag ? Mathf.Clamp(towerDefenseConveyorPacketConfig.weight, 0, 100) : 0);
		_previewRules.Text = (flag ? $"权重 {towerDefenseConveyorPacketConfig.weight}\u3000数量 {towerDefenseConveyorPacketConfig.minNum}～{towerDefenseConveyorPacketConfig.maxNum}\u3000倍率 {towerDefenseConveyorPacketConfig.minMagnification:0.##}～{towerDefenseConveyorPacketConfig.maxMagnification:0.##}" : "事件不会向传送带加入有效候选卡。");
	}

	private void OnEdited(bool committed)
	{
		NotifyCurrentResourceEdited();
		RefreshPreview();
	}

	private void DisposeBindings()
	{
		_eventBinding?.Dispose();
		_packetBinding?.Dispose();
		_eventBinding = null;
		_packetBinding = null;
		_event = null;
	}

	private static PanelContainer Panel(string tooltip, Color color)
	{
		PanelContainer obj = new PanelContainer
		{
			TooltipText = tooltip
		};
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = 9,
			CornerRadiusTopRight = 9,
			CornerRadiusBottomLeft = 9,
			CornerRadiusBottomRight = 9
		};
		float contentMarginLeft = (styleBoxFlat.ContentMarginRight = 14f);
		styleBoxFlat.ContentMarginLeft = contentMarginLeft;
		contentMarginLeft = (styleBoxFlat.ContentMarginBottom = 12f);
		styleBoxFlat.ContentMarginTop = contentMarginLeft;
		obj.AddThemeStyleboxOverride("panel", styleBoxFlat);
		return obj;
	}

	private static VBoxContainer Section(string title)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 7);
		vBoxContainer.AddChild(new Label
		{
			Text = title,
			ThemeTypeVariation = "HeaderMedium"
		}, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private static Control StageBadge(string text, Color color)
	{
		PanelContainer panelContainer = Panel("", color);
		panelContainer.CustomMinimumSize = new Vector2(150f, 72f);
		panelContainer.AddChild(new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private static HBoxContainer Field(string label, Control control)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(150f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static SpinBox Number(double min, double max, double step)
	{
		return new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			Step = step,
			AllowGreater = true,
			AllowLesser = true
		};
	}
}
