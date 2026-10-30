using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWProductionDropPresenter : IDisposable
{
	private const string ChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const string SunType = "Sun";

	private const string BrainSunType = "BrainSun";

	private const string JalapenoSunType = "JalaSun";

	private const string CoinType = "Coin";

	private const string PacketType = "Packet";

	private const string QXSunType = "QXSun";

	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal)
	{
		"_IZMMode", "produceType", "produceInterval", "num", "sunOnceMax", "markerPaths", "onlyEmit", "produceGlowTargetPath", "initialDelayRange", "glowLeadTime",
		"healthProductionSegments", "maxCatchUpProductions", "glowBrightness", "glowFadeInTime", "glowFadeOutTime", "coinRandom", "packetName"
	};

	private readonly ProduceComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly Node2D _previewWorld;

	private readonly List<XWGameVisualChoiceCard> _typeCards = new List<XWGameVisualChoiceCard>();

	private readonly List<XWGameVisualChoiceCard> _modeCards = new List<XWGameVisualChoiceCard>();

	private VBoxContainer _markerRows;

	private VBoxContainer _packetRows;

	private Control _sunPanel;

	private Control _coinPanel;

	private Control _packetPanel;

	private Control _emitOnlyPanel;

	private Control _healthPanel;

	private Label _productionSummary;

	private Label _sunChunkSummary;

	private Label _coinSummary;

	private Label _numWarning;

	private Label _unknownTypeWarning;

	private Label _markerWarning;

	private Label _glowWarning;

	private Label _runtimeDifference;

	private LineEdit _glowPathEdit;

	private XWProductionDropTimeline _timeline;

	private XWProductionHealthRuler _healthRuler;

	private XWProductionDropOverlay _overlay;

	private Node _previewOwner;

	private ProduceComponent _runtime;

	private int _markerStructureCount = -1;

	private int _packetStructureCount = -1;

	public PanelContainer Root { get; private set; }

	public Node2D Overlay => _overlay;

	public int PreviewRevision { get; private set; }

	public int MarkerRowCount => _markerRows?.GetChildCount() ?? 0;

	public int PacketRowCount => _packetRows?.GetChildCount() ?? 0;

	public int MarkerPinCount => _overlay?.MarkerPinCount ?? 0;

	public bool OverlayVisible
	{
		get
		{
			if (GodotObject.IsInstanceValid(_overlay))
			{
				return _overlay.Visible;
			}
			return false;
		}
	}

	public bool OverlayInputActive
	{
		get
		{
			if (GodotObject.IsInstanceValid(_overlay))
			{
				return _overlay.IsProcessingInput();
			}
			return false;
		}
	}

	public bool IsProcessIdle
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Root) || (!Root.IsProcessing() && !Root.IsPhysicsProcessing()))
			{
				if (GodotObject.IsInstanceValid(_overlay))
				{
					if (!_overlay.IsProcessing() && !_overlay.IsPhysicsProcessing())
					{
						return !_overlay.IsProcessingInput();
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public XWProductionDropPresenter(ProduceComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod, Node2D previewWorld)
	{
		_definition = definition;
		_binding = binding;
		_refreshTarget = refreshTarget;
		_refreshMethod = refreshMethod;
		_previewWorld = previewWorld;
	}

	public void Mount(VBoxContainer host)
	{
		if (GodotObject.IsInstanceValid(host) && GodotObject.IsInstanceValid(_definition) && _binding != null)
		{
			Root = new PanelContainer
			{
				Name = "ProductionDropPresenter",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			Root.SetProcess(enable: false);
			Root.SetPhysicsProcess(enable: false);
			Root.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("132019"), new Color("d5b84e"), 14, 2));
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_top", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 16);
			marginContainer.AddThemeConstantOverride("margin_bottom", 16);
			Root.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "ProductionDropBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 12);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHeader(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTypePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateModePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreatePositionPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTypeSpecificPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTimingPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateRuntimePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewWorld))
			{
				_overlay = new XWProductionDropOverlay
				{
					Name = "ProductionDropPreviewOverlay",
					ZIndex = 4092
				};
				_overlay.MarkerPathRequested += SelectMarkerPath;
				_previewWorld.AddChild(_overlay, forceReadableName: false, Node.InternalMode.Disabled);
			}
			RefreshPreview();
		}
	}

	private Control CreateHeader()
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 10);
		hBoxContainer.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCollectable.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(36f, 36f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		}, forceReadableName: false, Node.InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = "游戏内生产与掉落"
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color("fff0a8"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "在角色实际掉落点编辑产物、数量、发光和节奏；所有提示均对应当前 ProduceComponent 的真实运行规则。",
			Modulate = new Color("b8c7a6"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private Control CreateTypePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionTypePanel", "六种产物图卡", "类型会改变实体创建规则；onlyEmit 模式仍保留类型作为事件中的 operation kind。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ProductionTypeCards"
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddTypeCard(hFlowContainer, "Sun", "阳光", "普通阵营阳光", "res://addons/ModEditor/Icons/ResourceCollectable.svg");
		AddTypeCard(hFlowContainer, "BrainSun", "脑子阳光", "僵尸阵营资源", "res://addons/ModEditor/Icons/ResourceCharacter.svg");
		AddTypeCard(hFlowContainer, "JalaSun", "辣椒阳光", "携带阵营标志", "res://addons/ModEditor/Icons/ResourceFallingObject.svg");
		AddTypeCard(hFlowContainer, "Coin", "金币", "固定值或真实随机档", "res://addons/ModEditor/Icons/ResourceShop.svg");
		AddTypeCard(hFlowContainer, "Packet", "卡牌", "从名称列表随机一张", "res://addons/ModEditor/Icons/ResourceCard.svg");
		AddTypeCard(hFlowContainer, "QXSun", "七星阳光", "魅惑时附加 surcharge", "res://addons/ModEditor/Icons/ResourceGUI.svg");
		_unknownTypeWarning = CreateStatusLabel("ProductionUnknownTypeWarning");
		node.AddChild(_unknownTypeWarning, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateModePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionModePanel", "实体与事件模式", "事件模式不会创建阳光、金币或卡牌，但仍在每个有效 marker 发送产出 operation 与 OnProduct。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ProductionModeCards"
		};
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddModeCard(hFlowContainer, "spawn-and-emit", "生成实体＋事件", "执行类型对应的真实掉落", "res://addons/ModEditor/Icons/Game.svg", () => !_definition.onlyEmit, delegate
		{
			SetBool("onlyEmit", value: false, "启用实体生产");
		});
		AddModeCard(hFlowContainer, "emit-only", "仅发送事件", "不生成实体，仍保留数量和 marker", "res://addons/ModEditor/Icons/MemberSignal.svg", () => _definition.onlyEmit, delegate
		{
			SetBool("onlyEmit", value: true, "改为仅发送产出事件");
		});
		AddModeCard(hFlowContainer, "timed", "定时生产", "按秒数与发光提前量运行", "res://addons/ModEditor/Icons/History.svg", () => !_definition._IZMMode, delegate
		{
			SetBool("_IZMMode", value: false, "改为定时生产");
		});
		AddModeCard(hFlowContainer, "health", "IZM 血量生产", "植物每跨过一个生命阈值生产", "res://addons/ModEditor/Icons/Progress1.svg", () => _definition._IZMMode, delegate
		{
			SetBool("_IZMMode", value: true, "启用 IZM 血量生产");
		});
		_emitOnlyPanel = CreateNoticePanel("ProductionEmitOnlyNotice", "仅事件模式：Sun 分块、金币随机档与 Packet 名称不会创建实体；事件仍携带 produceType、num 与 marker_index。", new Color("4a3d19"));
		node.AddChild(_emitOnlyPanel, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreatePositionPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionPositionPanel", "游戏位置与发光目标", "每个有效 Marker2D 都会完整生产一次 num；没有有效 marker 时回退到角色 GlobalPosition。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_markerRows = new VBoxContainer
		{
			Name = "ProductionMarkerRows"
		};
		_markerRows.AddThemeConstantOverride("separation", 6);
		node.AddChild(_markerRows, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "ProductionAddMarkerButton",
			Text = "＋ 添加掉落点",
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Pin.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(180f, 40f)
		};
		button.Pressed += AddMarker;
		node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		_markerWarning = CreateStatusLabel("ProductionMarkerWarning");
		node.AddChild(_markerWarning, forceReadableName: false, Node.InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "ProductionGlowTargetRow"
		};
		hBoxContainer.AddChild(new Label
		{
			Text = "发光目标",
			CustomMinimumSize = new Vector2(110f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_glowPathEdit = new LineEdit
		{
			Name = "ProductionGlowTargetPath",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = "留空则调用角色整体 Bright"
		};
		BindGlowPath(_glowPathEdit);
		hBoxContainer.AddChild(_glowPathEdit, forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_glowWarning = CreateStatusLabel("ProductionGlowWarning");
		node.AddChild(_glowWarning, forceReadableName: false, Node.InternalMode.Disabled);
		_productionSummary = new Label
		{
			Name = "ProductionTotalSummary",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_productionSummary.AddThemeColorOverride("font_color", new Color("ffe28a"));
		node.AddChild(_productionSummary, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateTypeSpecificPanel()
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.Name = "ProductionTypeSpecificPanels";
		vBoxContainer.AddThemeConstantOverride("separation", 10);
		_sunPanel = CreateSunPanel();
		_coinPanel = CreateCoinPanel();
		_packetPanel = CreatePacketPanel();
		vBoxContainer.AddChild(_sunPanel, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(_coinPanel, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(_packetPanel, forceReadableName: false, Node.InternalMode.Disabled);
		return vBoxContainer;
	}

	private Control CreateSunPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionSunPanel", "阳光分块", "每个 marker 把 num 按 sunOnceMax 拆成多枚；最后一枚使用剩余值。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		node.AddChild(CreateNumberRow("ProductionSunChunkSize", "单枚上限", "sunOnceMax", 1.0, 10000.0, 1.0), forceReadableName: false, Node.InternalMode.Disabled);
		_sunChunkSummary = new Label
		{
			Name = "ProductionSunChunkSummary",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		node.AddChild(_sunChunkSummary, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateCoinPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionCoinPanel", "金币真实概率", "随机模式使用两次独立 Randf：1000 金币 2%，50 金币 19.6%，10 金币 78.4%。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ProductionCoinModeCards"
		};
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddModeCard(hFlowContainer, "coin-random", "随机金币档", "2% / 19.6% / 78.4%", "res://addons/ModEditor/Icons/ResourceShop.svg", () => _definition.coinRandom, delegate
		{
			SetBool("coinRandom", value: true, "金币改为随机档");
		});
		AddModeCard(hFlowContainer, "coin-exact", "固定金币值", "直接使用 num", "res://addons/ModEditor/Icons/MemberProperty.svg", () => !_definition.coinRandom, delegate
		{
			SetBool("coinRandom", value: false, "金币改为固定值");
		});
		_coinSummary = new Label
		{
			Name = "ProductionCoinProbabilitySummary",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		node.AddChild(_coinSummary, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreatePacketPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionPacketPanel", "卡牌掉落列表", "每个 marker 从列表等概率 PickRandom 一项；空列表不会生成卡牌。Packet 类型忽略 num。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_packetRows = new VBoxContainer
		{
			Name = "ProductionPacketRows"
		};
		_packetRows.AddThemeConstantOverride("separation", 6);
		node.AddChild(_packetRows, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "ProductionAddPacketButton",
			Text = "＋ 添加卡牌注册名",
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCard.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(200f, 40f)
		};
		button.Pressed += AddPacket;
		node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateTimingPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionTimingPanel", "初始延迟 · 发光 · 生产 · 追赶", "普通标量只刷新已挂载的时间轴和预览，不重建 Presenter。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "ProductionInitialDelayRange"
		};
		hBoxContainer.AddChild(new Label
		{
			Text = "初始延迟",
			CustomMinimumSize = new Vector2(110f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = CreateSpin(0.0, 300.0, 0.1);
		spinBox.Name = "ProductionInitialDelayMin";
		SpinBox spinBox2 = CreateSpin(0.0, 300.0, 0.1);
		spinBox2.Name = "ProductionInitialDelayMax";
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "～"
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(spinBox2, forceReadableName: false, Node.InternalMode.Disabled);
		BindVector2RangeInPlace(spinBox, spinBox2, "initialDelayRange");
		node.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionInterval", "生产间隔", "produceInterval", 0.0, 3600.0, 0.1), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionGlowLead", "提前发光", "glowLeadTime", 0.0, 300.0, 0.1), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionGlowBrightness", "发光强度", "glowBrightness", 0.0, 10.0, 0.05), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionGlowFadeIn", "亮起时间", "glowFadeInTime", 0.0, 60.0, 0.05), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionGlowFadeOut", "熄灭时间", "glowFadeOutTime", 0.0, 60.0, 0.05), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionCatchUpLimit", "单帧追赶上限", "maxCatchUpProductions", 1.0, 120.0, 1.0), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(CreateNumberRow("ProductionAmount", "每点数量 num", "num", 0.0, 100000.0, 1.0), forceReadableName: false, Node.InternalMode.Disabled);
		_numWarning = CreateStatusLabel("ProductionNumWarning");
		node.AddChild(_numWarning, forceReadableName: false, Node.InternalMode.Disabled);
		_timeline = new XWProductionDropTimeline
		{
			Name = "ProductionTimingTimeline",
			CustomMinimumSize = new Vector2(0f, 150f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_timeline.SetProcess(enable: false);
		_timeline.SetPhysicsProcess(enable: false);
		node.AddChild(_timeline, forceReadableName: false, Node.InternalMode.Disabled);
		_healthPanel = CreateHealthPanel();
		node.AddChild(_healthPanel, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateHealthPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionHealthThresholdPanel", "IZM 血量阈值尺", "运行时按初始 hitpoints / segments 生成阈值；一次伤害跨越多段时受追赶上限限制。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		node.AddChild(CreateNumberRow("ProductionHealthSegments", "生命分段", "healthProductionSegments", 1.0, 1000.0, 1.0), forceReadableName: false, Node.InternalMode.Disabled);
		_healthRuler = new XWProductionHealthRuler
		{
			Name = "ProductionHealthRuler",
			CustomMinimumSize = new Vector2(0f, 105f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_healthRuler.SetProcess(enable: false);
		_healthRuler.SetPhysicsProcess(enable: false);
		node.AddChild(_healthRuler, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateRuntimePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ProductionRuntimeDifferencePanel", "运行时覆盖差异", "存档/同步可覆盖 produceType 与 num；IZM/IZM2 关卡还会强制启用血量生产。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_runtimeDifference = new Label
		{
			Name = "ProductionRuntimeDifference",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		node.AddChild(_runtimeDifference, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateNumberRow(string name, string label, string property, double minimum, double maximum, double step)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.Name = name;
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(145f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = CreateSpin(minimum, maximum, step);
		spinBox.Name = name + "Value";
		spinBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_binding.BindNumber(spinBox, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private static SpinBox CreateSpin(double minimum, double maximum, double step)
	{
		return new SpinBox
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			AllowLesser = false,
			CustomMinimumSize = new Vector2(130f, 38f)
		};
	}

	private void AddTypeCard(Control host, string type, string title, string detail, string iconPath)
	{
		AddChoiceCard(host, _typeCards, "ProductionType_" + type, type, title, detail, iconPath, () => NormalizeProduceType(_definition.produceType) == type, delegate
		{
			_binding.SetValue(_definition, "produceType", type, "产物类型改为 " + title, _refreshTarget, _refreshMethod);
			RefreshPreview();
		});
	}

	private void AddModeCard(Control host, string key, string title, string detail, string iconPath, Func<bool> selected, Action choose)
	{
		AddChoiceCard(host, _modeCards, "ProductionMode_" + key, key, title, detail, iconPath, selected, delegate
		{
			choose();
			RefreshPreview();
		});
	}

	private void AddChoiceCard(Control host, List<XWGameVisualChoiceCard> collection, string name, string key, string title, string detail, string iconPath, Func<bool> selected, Action choose)
	{
		XWGameVisualChoiceCard card = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(card))
		{
			return;
		}
		card.Name = name;
		card.Configure(key, title, detail, ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse), selected());
		card.Pressed += choose;
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(card))
			{
				card.SetSelected(selected());
			}
		});
		collection.Add(card);
		host.AddChild(card, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void SetBool(string property, bool value, string action)
	{
		_binding.SetValue(_definition, property, value, action, _refreshTarget, _refreshMethod);
		RefreshPreview();
	}

	private void BindVector2RangeInPlace(SpinBox minimumControl, SpinBox maximumControl, StringName property)
	{
		Vector2 vector = _definition.Get(property).AsVector2();
		minimumControl.Value = vector.X;
		maximumControl.Value = vector.Y;
		Action value = delegate
		{
			_binding.BeginEdit(_definition, property);
		};
		Godot.Range.ValueChangedEventHandler value2 = delegate
		{
			_binding.PreviewValue(_definition, property, ReadValue());
			RefreshPreview();
		};
		Action value3 = delegate
		{
			_binding.CommitEdit(_definition, property, ReadValue(), $"修改 {property}", _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		minimumControl.FocusEntered += value;
		maximumControl.FocusEntered += value;
		minimumControl.ValueChanged += value2;
		maximumControl.ValueChanged += value2;
		minimumControl.FocusExited += value3;
		maximumControl.FocusExited += value3;
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(minimumControl) && GodotObject.IsInstanceValid(maximumControl) && GodotObject.IsInstanceValid(_definition))
			{
				Vector2 vector2 = _definition.Get(property).AsVector2();
				minimumControl.SetValueNoSignal(vector2.X);
				maximumControl.SetValueNoSignal(vector2.Y);
			}
		});
		Variant ReadValue()
		{
			return Variant.From<Vector2>(new Vector2((float)minimumControl.Value, (float)maximumControl.Value));
		}
	}

	private void BindGlowPath(LineEdit edit)
	{
		edit.Text = _definition.produceGlowTargetPath?.ToString() ?? string.Empty;
		edit.FocusEntered += delegate
		{
			_binding.BeginEdit(_definition, "produceGlowTargetPath");
		};
		edit.TextChanged += delegate(string value)
		{
			_binding.PreviewValue(_definition, "produceGlowTargetPath", Variant.From<NodePath>(new NodePath(value)));
			RefreshPreview();
		};
		edit.FocusExited += delegate
		{
			_binding.CommitEdit(_definition, "produceGlowTargetPath", Variant.From<NodePath>(new NodePath(edit.Text)), "修改生产发光目标", _refreshTarget, _refreshMethod);
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(edit) && !edit.HasFocus())
			{
				edit.Text = _definition.produceGlowTargetPath?.ToString() ?? string.Empty;
			}
		});
	}

	private void RebuildMarkerRows()
	{
		if (!GodotObject.IsInstanceValid(_markerRows))
		{
			return;
		}
		ClearChildren(_markerRows);
		int num = _definition.markerPaths?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			int captured = i;
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"ProductionMarkerRow_{i}"
			};
			hBoxContainer.AddChild(new Label
			{
				Text = $"#{i + 1}",
				CustomMinimumSize = new Vector2(34f, 0f)
			}, forceReadableName: false, Node.InternalMode.Disabled);
			LineEdit edit = new LineEdit
			{
				Name = $"ProductionMarkerPath_{i}",
				Text = (_definition.markerPaths[i]?.ToString() ?? string.Empty),
				PlaceholderText = "相对角色根节点的 Marker2D 路径",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			edit.FocusEntered += delegate
			{
				_binding.BeginEdit(_definition, "markerPaths");
			};
			edit.TextChanged += delegate(string value)
			{
				PreviewMarkerPath(captured, value);
			};
			edit.FocusExited += delegate
			{
				CommitMarkerPath(captured, edit.Text);
			};
			hBoxContainer.AddChild(edit, forceReadableName: false, Node.InternalMode.Disabled);
			Button button = new Button
			{
				Name = $"ProductionRemoveMarker_{i}",
				Text = "移除",
				Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
			};
			button.Pressed += delegate
			{
				RemoveMarker(captured);
			};
			hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			Label label = CreateStatusLabel($"ProductionMarkerStatus_{i}");
			label.CustomMinimumSize = new Vector2(165f, 0f);
			hBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
			_markerRows.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void SynchronizeMarkerRows()
	{
		for (int i = 0; i < (_definition.markerPaths?.Count ?? 0); i++)
		{
			LineEdit lineEdit = _markerRows?.FindChild($"ProductionMarkerPath_{i}", recursive: true, owned: false) as LineEdit;
			if (GodotObject.IsInstanceValid(lineEdit) && !lineEdit.HasFocus())
			{
				lineEdit.Text = _definition.markerPaths[i]?.ToString() ?? string.Empty;
			}
		}
	}

	private void RefreshMarkerStatuses()
	{
		int num = 0;
		int num2 = _definition.markerPaths?.Count ?? 0;
		for (int i = 0; i < num2; i++)
		{
			NodePath nodePath = _definition.markerPaths[i];
			Label label = _markerRows?.FindChild($"ProductionMarkerStatus_{i}", recursive: true, owned: false) as Label;
			string text;
			Color color;
			if (nodePath == null || nodePath.IsEmpty)
			{
				text = "空路径 · 该项忽略";
				color = new Color("e2b45f");
			}
			else if (!GodotObject.IsInstanceValid(_previewOwner))
			{
				text = "等待预览角色验证";
				color = new Color("83a8c6");
				num++;
			}
			else
			{
				Marker2D nodeOrNull = _previewOwner.GetNodeOrNull<Marker2D>(nodePath);
				if (nodeOrNull != null && GodotObject.IsInstanceValid(nodeOrNull))
				{
					text = "Marker2D 有效";
					color = new Color("77d48d");
					num++;
				}
				else
				{
					Node nodeOrNull2 = _previewOwner.GetNodeOrNull(nodePath);
					text = (GodotObject.IsInstanceValid(nodeOrNull2) ? ("类型错误：" + nodeOrNull2.GetType().Name) : "路径不存在");
					color = new Color("ff796f");
				}
			}
			SetStatus(label, text, color);
		}
		if (num2 == 0)
		{
			SetStatus(_markerWarning, "未配置掉落点：运行时会使用角色 GlobalPosition。", new Color("83a8c6"));
		}
		else if (num == 0)
		{
			SetStatus(_markerWarning, "没有有效 Marker2D：运行时仍会回退到角色 GlobalPosition。", new Color("ff9b70"));
		}
		else if (num != num2)
		{
			SetStatus(_markerWarning, $"有效掉落点 {num}/{num2}；无效项会被忽略。", new Color("e2b45f"));
		}
		else
		{
			SetStatus(_markerWarning, $"全部 {num} 个掉落点有效。", new Color("77d48d"));
		}
	}

	private void RefreshGlowStatus()
	{
		NodePath produceGlowTargetPath = _definition.produceGlowTargetPath;
		if (produceGlowTargetPath == null || produceGlowTargetPath.IsEmpty)
		{
			SetStatus(_glowWarning, "留空：运行时使用角色整体 Bright。", new Color("83a8c6"));
			return;
		}
		if (!GodotObject.IsInstanceValid(_previewOwner))
		{
			SetStatus(_glowWarning, "已配置路径；选择预览角色后验证 AdobeAnimateSpriteBase。", new Color("83a8c6"));
			return;
		}
		Node nodeOrNull = _previewOwner.GetNodeOrNull(produceGlowTargetPath);
		if (nodeOrNull is AdobeAnimateSpriteBase && GodotObject.IsInstanceValid(nodeOrNull))
		{
			SetStatus(_glowWarning, "发光目标有效。", new Color("77d48d"));
		}
		else if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			SetStatus(_glowWarning, "发光目标类型错误：" + nodeOrNull.GetType().Name + "。运行时会回退角色整体 Bright。", new Color("ff796f"));
		}
		else
		{
			SetStatus(_glowWarning, "发光目标路径不存在；运行时会回退角色整体 Bright。", new Color("ff9b70"));
		}
	}

	private void PreviewMarkerPath(int index, string value)
	{
		if (index >= 0 && index < (_definition.markerPaths?.Count ?? 0))
		{
			Array<NodePath> from = _definition.markerPaths.Duplicate(deep: true);
			from[index] = new NodePath(value);
			_binding.PreviewValue(_definition, "markerPaths", Variant.From(in from));
			RefreshPreview();
		}
	}

	private void CommitMarkerPath(int index, string value)
	{
		if (index >= 0 && index < (_definition.markerPaths?.Count ?? 0))
		{
			Array<NodePath> from = _definition.markerPaths.Duplicate(deep: true);
			from[index] = new NodePath(value);
			_binding.CommitEdit(_definition, "markerPaths", Variant.From(in from), $"修改掉落点 {index + 1}", _refreshTarget, _refreshMethod);
		}
	}

	private void AddMarker()
	{
		Array<NodePath> from = ((_definition.markerPaths != null) ? _definition.markerPaths.Duplicate(deep: true) : new Array<NodePath>());
		from.Add(new NodePath(""));
		_binding.SetValue(_definition, "markerPaths", Variant.From(in from), "添加生产掉落点", _refreshTarget, _refreshMethod);
	}

	private void RemoveMarker(int index)
	{
		if (index >= 0 && index < (_definition.markerPaths?.Count ?? 0))
		{
			Array<NodePath> from = _definition.markerPaths.Duplicate(deep: true);
			from.RemoveAt(index);
			_binding.SetValue(_definition, "markerPaths", Variant.From(in from), $"移除生产掉落点 {index + 1}", _refreshTarget, _refreshMethod);
		}
	}

	public void SelectMarkerPath(int index, NodePath path)
	{
		if (index >= 0 && index < (_definition.markerPaths?.Count ?? 0) && !(path == null) && !path.IsEmpty)
		{
			Array<NodePath> from = _definition.markerPaths.Duplicate(deep: true);
			from[index] = path;
			_binding.SetValue(_definition, "markerPaths", Variant.From(in from), $"掉落点 {index + 1} 吸附到 {path}", _refreshTarget, _refreshMethod);
		}
	}

	private void RebuildPacketRows()
	{
		if (!GodotObject.IsInstanceValid(_packetRows))
		{
			return;
		}
		ClearChildren(_packetRows);
		for (int i = 0; i < (_definition.packetName?.Count ?? 0); i++)
		{
			int captured = i;
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"ProductionPacketRow_{i}"
			};
			hBoxContainer.AddChild(new Label
			{
				Text = $"#{i + 1}",
				CustomMinimumSize = new Vector2(34f, 0f)
			}, forceReadableName: false, Node.InternalMode.Disabled);
			LineEdit edit = new LineEdit
			{
				Name = $"ProductionPacketName_{i}",
				Text = (_definition.packetName[i] ?? string.Empty),
				PlaceholderText = "Packet 注册名",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			edit.FocusEntered += delegate
			{
				_binding.BeginEdit(_definition, "packetName");
			};
			edit.TextChanged += delegate(string value)
			{
				PreviewPacketName(captured, value);
			};
			edit.FocusExited += delegate
			{
				CommitPacketName(captured, edit.Text);
			};
			hBoxContainer.AddChild(edit, forceReadableName: false, Node.InternalMode.Disabled);
			Button button = new Button
			{
				Name = $"ProductionRemovePacket_{i}",
				Text = "移除",
				Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
			};
			button.Pressed += delegate
			{
				RemovePacket(captured);
			};
			hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			_packetRows.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void SynchronizePacketRows()
	{
		for (int i = 0; i < (_definition.packetName?.Count ?? 0); i++)
		{
			LineEdit lineEdit = _packetRows?.FindChild($"ProductionPacketName_{i}", recursive: true, owned: false) as LineEdit;
			if (GodotObject.IsInstanceValid(lineEdit) && !lineEdit.HasFocus())
			{
				lineEdit.Text = _definition.packetName[i] ?? string.Empty;
			}
		}
	}

	private void PreviewPacketName(int index, string value)
	{
		if (index >= 0 && index < (_definition.packetName?.Count ?? 0))
		{
			Array<string> from = _definition.packetName.Duplicate();
			from[index] = value;
			_binding.PreviewValue(_definition, "packetName", Variant.From(in from));
			RefreshPreview();
		}
	}

	private void CommitPacketName(int index, string value)
	{
		if (index >= 0 && index < (_definition.packetName?.Count ?? 0))
		{
			Array<string> from = _definition.packetName.Duplicate();
			from[index] = value;
			_binding.CommitEdit(_definition, "packetName", Variant.From(in from), $"修改卡牌掉落 {index + 1}", _refreshTarget, _refreshMethod);
		}
	}

	private void AddPacket()
	{
		Array<string> from = ((_definition.packetName != null) ? _definition.packetName.Duplicate() : new Array<string>());
		from.Add(string.Empty);
		_binding.SetValue(_definition, "packetName", Variant.From(in from), "添加掉落卡牌", _refreshTarget, _refreshMethod);
	}

	private void RemovePacket(int index)
	{
		if (index >= 0 && index < (_definition.packetName?.Count ?? 0))
		{
			Array<string> from = _definition.packetName.Duplicate();
			from.RemoveAt(index);
			_binding.SetValue(_definition, "packetName", Variant.From(in from), $"移除掉落卡牌 {index + 1}", _refreshTarget, _refreshMethod);
		}
	}

	public void BindPreviewRuntime(Node owner, ProduceComponent runtime)
	{
		_previewOwner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		_runtime = ((runtime != null && !runtime.IsReleased) ? runtime : null);
		RefreshPreview();
	}

	public void SetPreviewActive(bool active)
	{
		if (GodotObject.IsInstanceValid(_overlay))
		{
			_overlay.ProcessMode = (Node.ProcessModeEnum)(active ? 0 : 4);
			_overlay.SetProcess(enable: false);
			_overlay.SetPhysicsProcess(enable: false);
			_overlay.SetProcessInput(active);
			_overlay.Visible = active;
		}
	}

	public bool DropMarkerHandleAt(int index, Vector2 localPosition)
	{
		if (GodotObject.IsInstanceValid(_overlay))
		{
			return _overlay.DropMarkerAt(index, localPosition);
		}
		return false;
	}

	public Vector2 GetMarkerHandlePosition(int index)
	{
		return _overlay?.GetMarkerHandlePosition(index) ?? Vector2.Zero;
	}

	public void RefreshPreview()
	{
		if (!GodotObject.IsInstanceValid(_definition))
		{
			return;
		}
		PreviewRevision++;
		int num = _definition.markerPaths?.Count ?? 0;
		if (_markerStructureCount != num)
		{
			_markerStructureCount = num;
			RebuildMarkerRows();
		}
		else
		{
			SynchronizeMarkerRows();
		}
		int num2 = _definition.packetName?.Count ?? 0;
		if (_packetStructureCount != num2)
		{
			_packetStructureCount = num2;
			RebuildPacketRows();
		}
		else
		{
			SynchronizePacketRows();
		}
		foreach (XWGameVisualChoiceCard typeCard in _typeCards)
		{
			if (GodotObject.IsInstanceValid(typeCard))
			{
				typeCard.QueueRedraw();
			}
		}
		foreach (XWGameVisualChoiceCard modeCard in _modeCards)
		{
			if (GodotObject.IsInstanceValid(modeCard))
			{
				modeCard.QueueRedraw();
			}
		}
		string text = NormalizeProduceType(_definition.produceType);
		bool flag = IsSunType(text);
		bool num3 = IsKnownProduceType(text);
		bool flag2 = !_definition.onlyEmit;
		if (GodotObject.IsInstanceValid(_sunPanel))
		{
			_sunPanel.Visible = flag2 && flag;
		}
		if (GodotObject.IsInstanceValid(_coinPanel))
		{
			_coinPanel.Visible = flag2 && text == "Coin";
		}
		if (GodotObject.IsInstanceValid(_packetPanel))
		{
			_packetPanel.Visible = flag2 && text == "Packet";
		}
		if (GodotObject.IsInstanceValid(_emitOnlyPanel))
		{
			_emitOnlyPanel.Visible = !flag2;
		}
		if (GodotObject.IsInstanceValid(_healthPanel))
		{
			_healthPanel.Visible = _definition._IZMMode || (_runtime?._IZMMode ?? false);
		}
		if (num3)
		{
			SetStatus(_unknownTypeWarning, string.Empty, Colors.Transparent);
		}
		else
		{
			SetStatus(_unknownTypeWarning, "未知产物类型“" + text + "”：运行时会保留该值，但 switch 不会生成实体；六种图卡均不选中。", new Color("ff665e"));
		}
		RefreshMarkerStatuses();
		RefreshGlowStatus();
		RefreshSummaries(text);
		RefreshRuntimeDifference(text);
		_timeline?.Bind(_definition);
		_healthRuler?.Bind(Math.Clamp(_definition.healthProductionSegments, 1, 1000), Math.Clamp(_definition.maxCatchUpProductions, 1, 120));
		_overlay?.Bind(_definition, _previewOwner);
	}

	private void RefreshSummaries(string type)
	{
		int effectiveMarkerCount = GetEffectiveMarkerCount();
		long value = (long)Math.Max(0, _definition.num) * (long)effectiveMarkerCount;
		if (GodotObject.IsInstanceValid(_productionSummary))
		{
			if (_definition.onlyEmit)
			{
				_productionSummary.Text = $"{effectiveMarkerCount} 个有效生产位置 × 事件 amount {_definition.num} = 每轮事件总量 {value}。";
			}
			else if (type == "Packet")
			{
				_productionSummary.Text = $"{effectiveMarkerCount} 个有效生产位置：每点最多 1 张卡牌，每轮最多 {effectiveMarkerCount} 张；num 不参与实体数量。";
			}
			else if (type == "Coin" && _definition.coinRandom)
			{
				_productionSummary.Text = $"{effectiveMarkerCount} 个有效生产位置：每点生成 1 枚随机金币，每轮 {effectiveMarkerCount} 枚；金额来自概率池。";
			}
			else
			{
				_productionSummary.Text = $"{effectiveMarkerCount} 个有效生产位置 × 每点 {_definition.num} = 每轮实体配置量 {value}。";
			}
		}
		int num = Math.Max(1, _definition.sunOnceMax);
		int num2 = Math.Max(0, _definition.num);
		int num3 = ((num2 != 0) ? ((num2 + num - 1) / num) : 0);
		if (GodotObject.IsInstanceValid(_sunChunkSummary))
		{
			_sunChunkSummary.Text = $"每点拆成 {num3} 枚，{effectiveMarkerCount} 点共 {num3 * effectiveMarkerCount} 枚；单枚最大 {num}。";
		}
		if (GodotObject.IsInstanceValid(_coinSummary))
		{
			_coinSummary.Text = (_definition.coinRandom ? "真实结果：1000 金币 2.0% · 50 金币 19.6% · 10 金币 78.4%；num 不参与实体金额。" : $"固定模式：每个有效 marker 生成 {_definition.num} 金币。");
		}
		if (_definition.onlyEmit)
		{
			SetStatus(_numWarning, "onlyEmit：num 仍作为事件 amount 使用。", new Color("83a8c6"));
		}
		else if (type == "Packet")
		{
			SetStatus(_numWarning, "Packet 实体生成忽略 num：每个有效 marker 最多随机生成 1 张卡牌。", new Color("ff9b70"));
		}
		else if (type == "Coin" && _definition.coinRandom)
		{
			SetStatus(_numWarning, "随机金币模式忽略 num 的实体金额；num 只保留在产出事件中。", new Color("ff9b70"));
		}
		else
		{
			SetStatus(_numWarning, "num 当前参与每个 marker 的实体数量或数值。", new Color("77d48d"));
		}
	}

	private void RefreshRuntimeDifference(string definitionType)
	{
		if (!GodotObject.IsInstanceValid(_runtimeDifference))
		{
			return;
		}
		List<string> list = new List<string>();
		if (TryGetPreviewOwnerProperty("produceType", out var value))
		{
			string text = NormalizeProduceType(value.AsString());
			if (!string.Equals(text, definitionType, StringComparison.Ordinal))
			{
				list.Add("角色脚本最终覆盖/预期运行值 produceType：定义 " + definitionType + " → 角色 " + text);
			}
		}
		if (TryGetPreviewOwnerProperty("sunNum", out var value2))
		{
			int num = value2.AsInt32();
			if (num != _definition.num)
			{
				list.Add($"角色脚本最终覆盖/预期运行值 num：定义 {_definition.num} → 角色 sunNum {num}");
			}
		}
		if (TryGetPreviewOwnerProperty("produceInterval", out var value3))
		{
			double num2 = value3.AsDouble();
			if (!Mathf.IsEqualApprox((float)num2, _definition.produceInterval))
			{
				list.Add($"角色脚本最终覆盖/预期运行值 interval：定义 {_definition.produceInterval:0.##} → 角色 {num2:0.##}");
			}
		}
		if (_runtime == null)
		{
			if (list.Count == 0)
			{
				SetStatus(_runtimeDifference, "尚未绑定真实 ProduceComponent；当前显示定义值。", new Color("83a8c6"));
			}
			else
			{
				SetStatus(_runtimeDifference, "检测到角色脚本预期覆盖（真实组件尚未启动）：\n• " + string.Join("\n• ", list), new Color("ffd073"));
			}
			return;
		}
		string text2 = NormalizeProduceType(_runtime.produceType);
		if (!string.Equals(text2, definitionType, StringComparison.Ordinal))
		{
			list.Add("produceType：定义 " + definitionType + " → 运行时 " + text2);
		}
		if (_runtime.num != _definition.num)
		{
			list.Add($"num：定义 {_definition.num} → 运行时 {_runtime.num}");
		}
		if (_runtime._IZMMode != _definition._IZMMode)
		{
			list.Add($"IZM：定义 {_definition._IZMMode} → 运行时 {_runtime._IZMMode}");
		}
		if (_runtime.isBaseIZM)
		{
			list.Add("关卡 IZM/IZM2 已强制使用血量阈值生产");
		}
		if (definitionType == "Sun" && _runtime.isBaseIZM)
		{
			list.Add("Sun 实体会被运行时覆盖为 BrainSun");
		}
		if (list.Count == 0)
		{
			SetStatus(_runtimeDifference, "运行时与当前定义一致。", new Color("77d48d"));
		}
		else
		{
			SetStatus(_runtimeDifference, "检测到运行时覆盖：\n• " + string.Join("\n• ", list), new Color("ffd073"));
		}
	}

	private bool TryGetPreviewOwnerProperty(StringName property, out Variant value)
	{
		value = default(Variant);
		if (!GodotObject.IsInstanceValid(_previewOwner))
		{
			return false;
		}
		foreach (Dictionary property2 in _previewOwner.GetPropertyList())
		{
			if (property2.ContainsKey("name") && !(property2["name"].AsString() != property.ToString()))
			{
				value = _previewOwner.Get(property);
				return true;
			}
		}
		return false;
	}

	private int GetEffectiveMarkerCount()
	{
		int num = 0;
		for (int i = 0; i < (_definition.markerPaths?.Count ?? 0); i++)
		{
			NodePath nodePath = _definition.markerPaths[i];
			if (!(nodePath == null) && !nodePath.IsEmpty && (!GodotObject.IsInstanceValid(_previewOwner) || GodotObject.IsInstanceValid(_previewOwner.GetNodeOrNull<Marker2D>(nodePath))))
			{
				num++;
			}
		}
		return Math.Max(1, num);
	}

	private static string NormalizeProduceType(string type)
	{
		string text = type?.Trim() ?? string.Empty;
		if (string.Equals(text, "Sun", StringComparison.OrdinalIgnoreCase))
		{
			return "Sun";
		}
		if (string.Equals(text, "BrainSun", StringComparison.OrdinalIgnoreCase))
		{
			return "BrainSun";
		}
		if (string.Equals(text, "JalaSun", StringComparison.OrdinalIgnoreCase))
		{
			return "JalaSun";
		}
		if (string.Equals(text, "Coin", StringComparison.OrdinalIgnoreCase))
		{
			return "Coin";
		}
		if (string.Equals(text, "Packet", StringComparison.OrdinalIgnoreCase))
		{
			return "Packet";
		}
		if (string.Equals(text, "QXSun", StringComparison.OrdinalIgnoreCase))
		{
			return "QXSun";
		}
		return text;
	}

	private static bool IsSunType(string type)
	{
		switch (type)
		{
		case "Sun":
		case "BrainSun":
		case "JalaSun":
		case "QXSun":
			return true;
		default:
			return false;
		}
	}

	private static bool IsKnownProduceType(string type)
	{
		bool flag = IsSunType(type);
		if (!flag)
		{
			bool flag2 = ((type == "Coin" || type == "Packet") ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static Label CreateStatusLabel(string name)
	{
		return new Label
		{
			Name = name,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
	}

	private static void SetStatus(Label label, string text, Color color)
	{
		if (GodotObject.IsInstanceValid(label))
		{
			label.Text = text;
			label.Modulate = color;
		}
	}

	private static Control CreateNoticePanel(string name, string text, Color background)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(background, new Color("d9ba55"), 8, 1));
		panelContainer.AddChild(new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private static PanelContainer CreateSectionPanel(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("101a14"), new Color("52604f"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "SectionBody"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeColorOverride("font_color", new Color("ffe28a"));
		label.AddThemeFontSizeOverride("font_size", 16);
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			Modulate = new Color("9bad9b"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private static StyleBoxFlat CreatePanelStyle(Color background, Color border, int radius, int width)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = width,
			BorderWidthTop = width,
			BorderWidthRight = width,
			BorderWidthBottom = width,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 12f,
			ContentMarginTop = 10f,
			ContentMarginRight = 12f,
			ContentMarginBottom = 10f
		};
	}

	private static void ClearChildren(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void Dispose()
	{
		if (GodotObject.IsInstanceValid(_overlay))
		{
			_overlay.MarkerPathRequested -= SelectMarkerPath;
			_overlay.SetProcessInput(enable: false);
			if (_overlay.GetParent() != null)
			{
				_overlay.GetParent().RemoveChild(_overlay);
			}
			_overlay.QueueFree();
		}
		_typeCards.Clear();
		_modeCards.Clear();
		_markerRows = null;
		_packetRows = null;
		_timeline = null;
		_healthRuler = null;
		_overlay = null;
		_previewOwner = null;
		_runtime = null;
		_markerStructureCount = -1;
		_packetStructureCount = -1;
		Root = null;
	}
}
