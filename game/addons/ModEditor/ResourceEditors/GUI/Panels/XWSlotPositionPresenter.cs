using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWSlotPositionPresenter : IDisposable
{
	private const string ChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal) { "posMarkPath", "heightFollow", "hideShadow", "occupantRefreshInterval", "restoreGroundHeightOnRelease" };

	private readonly SlotComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly Node2D _previewWorld;

	private readonly List<XWGameVisualChoiceCard> _heightCards = new List<XWGameVisualChoiceCard>();

	private readonly List<XWGameVisualChoiceCard> _shadowCards = new List<XWGameVisualChoiceCard>();

	private readonly List<XWGameVisualChoiceCard> _releaseCards = new List<XWGameVisualChoiceCard>();

	private Node _previewOwner;

	private SlotComponent _runtime;

	private LineEdit _markerPath;

	private Label _markerStatus;

	private Label _runtimeStatus;

	private Label _scanStatus;

	private Label _slotLayerStatus;

	private Label _surroundLayerStatus;

	private XWSlotPhysicsCadenceStrip _cadence;

	private XWSlotPositionOverlay _overlay;

	private bool _previewActive;

	public PanelContainer Root { get; private set; }

	public bool MarkerValid { get; private set; }

	public int MarkerPinCount => MarkerValid ? 1 : 0;

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

	public bool MeaninglessSimulationStopped
	{
		get
		{
			if (!MarkerValid)
			{
				if (GodotObject.IsInstanceValid(_overlay))
				{
					if (!_overlay.IsProcessing())
					{
						return !_overlay.IsPhysicsProcessing();
					}
					return false;
				}
				return true;
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

	public XWSlotPositionPresenter(SlotComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod, Node2D previewWorld)
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
				Name = "SlotPositionPresenter",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			Root.SetProcess(enable: false);
			Root.SetPhysicsProcess(enable: false);
			Root.AddThemeStyleboxOverride("panel", PanelStyle(new Color("111c24"), new Color("58b7d9"), 14, 2));
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_top", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 16);
			marginContainer.AddThemeConstantOverride("margin_bottom", 16);
			Root.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "SlotPositionBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 12);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHeader(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateMarkerSection(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateLayerSection(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateBehaviorSection(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateScanSection(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateRuntimeSection(), forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewWorld))
			{
				_overlay = new XWSlotPositionOverlay
				{
					Name = "SlotPositionPreviewOverlay",
					ZIndex = 4091
				};
				_overlay.MarkerPathRequested += SelectMarkerPath;
				_previewWorld.AddChild(_overlay, forceReadableName: false, Node.InternalMode.Disabled);
			}
			RefreshPreview();
		}
	}

	public void BindPreviewRuntime(Node previewOwner, SlotComponent runtime)
	{
		_previewOwner = previewOwner;
		_runtime = runtime;
		RefreshPreview();
	}

	public void SetPreviewActive(bool active)
	{
		_previewActive = active;
		if (GodotObject.IsInstanceValid(_overlay))
		{
			_overlay.Visible = active;
			_overlay.SetProcess(enable: false);
			_overlay.SetPhysicsProcess(enable: false);
			_overlay.SetProcessInput(active && MarkerValid);
			_overlay.ProcessMode = (Node.ProcessModeEnum)(active ? 0 : 4);
		}
	}

	public void RefreshPreview()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			NodePath nodePath = _definition.posMarkPath ?? new NodePath();
			if (GodotObject.IsInstanceValid(_markerPath) && !_markerPath.HasFocus())
			{
				_markerPath.Text = nodePath.ToString();
			}
			Marker2D marker2D = ResolveMarker(nodePath);
			MarkerValid = GodotObject.IsInstanceValid(marker2D);
			if (nodePath.IsEmpty)
			{
				SetStatus(_markerStatus, "未设置位置 marker：Slot runtime 无法绑定占用者，画面假模拟已停止。", new Color("ff6f69"));
			}
			else if (!GodotObject.IsInstanceValid(_previewOwner))
			{
				SetStatus(_markerStatus, $"路径 {nodePath} 尚无角色预览可解析；不能标记为可用。", new Color("ffb25d"));
			}
			else if (!MarkerValid)
			{
				SetStatus(_markerStatus, $"无效 Marker2D：{nodePath}。运行时 ResolveMarker 会得到 null 并释放占用者。", new Color("ff6f69"));
			}
			else
			{
				SetStatus(_markerStatus, $"有效 Marker2D · {nodePath} · 世界位置 {marker2D.GlobalPosition.X:0.#}, {marker2D.GlobalPosition.Y:0.#}", new Color("73d995"));
			}
			int num = Math.Clamp(_definition.occupantRefreshInterval, 1, 30);
			if (GodotObject.IsInstanceValid(_scanStatus))
			{
				_scanStatus.Text = $"每 {num} 个物理帧重新扫描 Cell.GetSlot / GetSurround；绑定后的位置与阴影仍在每个物理帧应用。" + "\n这不是卡牌冷却，也不会读取或修改 PacketConfig。";
			}
			_cadence?.Bind(num);
			RefreshChoiceCards(_heightCards, _definition.heightFollow ? "follow" : "fixed");
			RefreshChoiceCards(_shadowCards, _definition.hideShadow ? "hide" : "keep");
			RefreshChoiceCards(_releaseCards, _definition.restoreGroundHeightOnRelease ? "restore" : "retain");
			RefreshRuntime(marker2D);
			_overlay?.Bind(_definition, _previewOwner, _runtime, marker2D);
			if (GodotObject.IsInstanceValid(_overlay))
			{
				_overlay.SetProcessInput(_previewActive && MarkerValid);
			}
		}
	}

	public Vector2 GetMarkerHandlePosition()
	{
		if (!GodotObject.IsInstanceValid(_overlay))
		{
			return Vector2.Zero;
		}
		return _overlay.MarkerHandlePosition;
	}

	public void Dispose()
	{
		foreach (XWGameVisualChoiceCard heightCard in _heightCards)
		{
			if (GodotObject.IsInstanceValid(heightCard))
			{
				heightCard.QueueFree();
			}
		}
		foreach (XWGameVisualChoiceCard shadowCard in _shadowCards)
		{
			if (GodotObject.IsInstanceValid(shadowCard))
			{
				shadowCard.QueueFree();
			}
		}
		foreach (XWGameVisualChoiceCard releaseCard in _releaseCards)
		{
			if (GodotObject.IsInstanceValid(releaseCard))
			{
				releaseCard.QueueFree();
			}
		}
		_heightCards.Clear();
		_shadowCards.Clear();
		_releaseCards.Clear();
		if (GodotObject.IsInstanceValid(_overlay))
		{
			_overlay.MarkerPathRequested -= SelectMarkerPath;
			_overlay.QueueFree();
		}
		if (GodotObject.IsInstanceValid(Root))
		{
			Root.QueueFree();
		}
		_overlay = null;
		_previewOwner = null;
		_runtime = null;
		Root = null;
	}

	private Control CreateHeader()
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 10);
		hBoxContainer.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(38f, 38f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		}, forceReadableName: false, Node.InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = "游戏内槽位与承载位置"
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color("9fe4ff"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "一个 Marker2D 同时驱动槽内与环绕占用者的真实高度；两层保持独立绑定和释放状态。",
			Modulate = new Color("a6bdc9"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private Control CreateMarkerSection()
	{
		PanelContainer panelContainer = Section("SlotMarkerSection", "角色画面位置 Marker", "直接输入 NodePath，或在角色画面拖动蓝色 pin 吸附到最近 Marker2D。");
		VBoxContainer obj = panelContainer.FindChild("SectionBody", recursive: true, owned: false) as VBoxContainer;
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = "posMarkPath",
			CustomMinimumSize = new Vector2(150f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_markerPath = new LineEdit
		{
			Name = "SlotPositionMarkerPath",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = "例如 SpriteGroup/TransformPoint/.../Marker2D"
		};
		hBoxContainer.AddChild(_markerPath, forceReadableName: false, Node.InternalMode.Disabled);
		obj.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_markerStatus = StatusLabel("SlotPositionMarkerStatus");
		obj.AddChild(_markerStatus, forceReadableName: false, Node.InternalMode.Disabled);
		_markerPath.FocusEntered += delegate
		{
			_binding.BeginEdit(_definition, "posMarkPath");
		};
		_markerPath.TextChanged += delegate(string value)
		{
			_binding.PreviewValue(_definition, "posMarkPath", Variant.From<NodePath>(new NodePath(value)));
			RefreshPreview();
		};
		_markerPath.FocusExited += delegate
		{
			_binding.CommitEdit(_definition, "posMarkPath", Variant.From<NodePath>(new NodePath(_markerPath.Text)), "修改槽位角色位置 Marker", _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(_markerPath) && !_markerPath.HasFocus())
			{
				_markerPath.Text = _definition.posMarkPath?.ToString() ?? string.Empty;
			}
		});
		return panelContainer;
	}

	private Control CreateLayerSection()
	{
		PanelContainer panelContainer = Section("SlotOccupantLayers", "槽内 / 环绕 · 两个实际占用层", "runtime 分别读取 Cell.GetSlot(parent) 与 Cell.GetSurround()；不会把两者合并成一个装饰预览。");
		VBoxContainer obj = panelContainer.FindChild("SectionBody", recursive: true, owned: false) as VBoxContainer;
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "SlotOccupantLayerCards"
		};
		hBoxContainer.AddThemeConstantOverride("separation", 10);
		_slotLayerStatus = LayerCard(hBoxContainer, "SlotInsideLayer", "⬇ 槽内层", new Color("2f6380"));
		_surroundLayerStatus = LayerCard(hBoxContainer, "SlotSurroundLayer", "↻ 环绕层", new Color("554a83"));
		obj.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		obj.AddChild(new Label
		{
			Name = "SlotHeightLineExplanation",
			Text = "蓝色高度线 = (宿主世界 Y − Marker 世界 Y) ÷ transformPoint.GlobalScale.Y；负 scale 和旋转由真实全局变换换算，不使用屏幕坐标猜测。",
			Modulate = new Color("87cfea"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateBehaviorSection()
	{
		PanelContainer panelContainer = Section("SlotBehaviorSection", "绑定行为拼图", "三组开关只对应 runtime 已存在的 heightFollow / hideShadow / release 恢复语义。");
		VBoxContainer vBoxContainer = panelContainer.FindChild("SectionBody", recursive: true, owned: false) as VBoxContainer;
		vBoxContainer.AddChild(ChoiceRow("SlotHeightModes", _heightCards, ("fixed", "固定高度", "只写 groundHeight", "res://addons/ModEditor/Icons/Pin.svg", () => !_definition.heightFollow, delegate
		{
			SetBool("heightFollow", value: false, "槽位使用固定高度");
		}), ("follow", "跟随高度", "同步 shadow followHeight", "res://addons/ModEditor/Icons/MoveDown.svg", () => _definition.heightFollow, delegate
		{
			SetBool("heightFollow", value: true, "槽位启用高度跟随");
		})), forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(ChoiceRow("SlotShadowModes", _shadowCards, ("hide", "隐藏占用者阴影", "绑定期间隐藏", "res://addons/ModEditor/Icons/GuiVisibilityHidden.svg", () => _definition.hideShadow, delegate
		{
			SetBool("hideShadow", value: true, "绑定期间隐藏占用者阴影");
		}), ("keep", "保留原阴影", "保持绑定前可见性", "res://addons/ModEditor/Icons/GuiVisibilityVisible.svg", () => !_definition.hideShadow, delegate
		{
			SetBool("hideShadow", value: false, "绑定期间保留占用者阴影");
		})), forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(ChoiceRow("SlotReleaseModes", _releaseCards, ("restore", "释放时还原", "恢复绑定前 groundHeight", "res://addons/ModEditor/Icons/Reload.svg", () => _definition.restoreGroundHeightOnRelease, delegate
		{
			SetBool("restoreGroundHeightOnRelease", value: true, "释放时恢复原地面高度");
		}), ("retain", "释放时保留", "保留最后写入高度", "res://addons/ModEditor/Icons/Save.svg", () => !_definition.restoreGroundHeightOnRelease, delegate
		{
			SetBool("restoreGroundHeightOnRelease", value: false, "释放时保留最后高度");
		})), forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateScanSection()
	{
		PanelContainer panelContainer = Section("SlotPhysicsScanSection", "占用者物理扫描节奏", "occupantRefreshInterval 仅控制重新查询槽内/环绕角色的物理帧间隔。");
		VBoxContainer obj = panelContainer.FindChild("SectionBody", recursive: true, owned: false) as VBoxContainer;
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = "物理帧间隔",
			CustomMinimumSize = new Vector2(150f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			Name = "SlotOccupantRefreshIntervalValue",
			MinValue = 1.0,
			MaxValue = 30.0,
			Step = 1.0,
			AllowGreater = false,
			AllowLesser = false,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_binding.BindNumber(spinBox, _definition, "occupantRefreshInterval", RefreshPreview, _refreshTarget, _refreshMethod);
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		obj.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_cadence = new XWSlotPhysicsCadenceStrip
		{
			Name = "SlotPhysicsCadenceStrip",
			CustomMinimumSize = new Vector2(0f, 62f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_cadence.SetProcess(enable: false);
		_cadence.SetPhysicsProcess(enable: false);
		obj.AddChild(_cadence, forceReadableName: false, Node.InternalMode.Disabled);
		_scanStatus = StatusLabel("SlotPhysicsScanStatus");
		obj.AddChild(_scanStatus, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateRuntimeSection()
	{
		PanelContainer panelContainer = Section("SlotRuntimeTruthSection", "真实 runtime 状态", "没有 Cell/occupant harness 时只显示已绑定对象，不伪造角色进入槽位。");
		VBoxContainer obj = panelContainer.FindChild("SectionBody", recursive: true, owned: false) as VBoxContainer;
		_runtimeStatus = StatusLabel("SlotRuntimeTruthStatus");
		obj.AddChild(_runtimeStatus, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private void RefreshRuntime(Marker2D marker)
	{
		bool flag = _runtime != null;
		string text = ((flag && GodotObject.IsInstanceValid(_runtime.posMark)) ? $"runtime marker：{_runtime.posMark.GetPath()}" : "runtime marker：未解析");
		if (!flag)
		{
			SetStatus(_runtimeStatus, text + "\n未绑定 SlotComponent；不启动假的 Cell/occupant 模拟。", new Color("ffb25d"));
		}
		else if (!GodotObject.IsInstanceValid(marker))
		{
			SetStatus(_runtimeStatus, text + "\n路径无效：真实 ProcessBindings 会 ReleaseAllOccupants 并提前返回。", new Color("ff6f69"));
		}
		else
		{
			SetStatus(_runtimeStatus, $"{text}\n当前只读绑定：slot={CharacterName(_runtime.slotCharacter)} · surround={CharacterName(_runtime.surroundCharacter)}", new Color("83cdeb"));
		}
		if (GodotObject.IsInstanceValid(_slotLayerStatus))
		{
			_slotLayerStatus.Text = ((flag && GodotObject.IsInstanceValid(_runtime.slotCharacter)) ? (CharacterName(_runtime.slotCharacter) + "\n高度写入：启用 · 阴影原态：" + VisibleText(_runtime.slotCharacterShadow)) : "当前无真实槽内占用者\n高度/阴影：未应用");
		}
		if (GodotObject.IsInstanceValid(_surroundLayerStatus))
		{
			_surroundLayerStatus.Text = ((flag && GodotObject.IsInstanceValid(_runtime.surroundCharacter)) ? (CharacterName(_runtime.surroundCharacter) + "\n高度写入：启用 · 阴影原态：" + VisibleText(_runtime.surroundCharacterShadow)) : "当前无真实环绕占用者\n高度/阴影：未应用");
		}
	}

	private Marker2D ResolveMarker(NodePath path)
	{
		if (!GodotObject.IsInstanceValid(_previewOwner) || path == null || path.IsEmpty)
		{
			return null;
		}
		return _previewOwner.GetNodeOrNull<Marker2D>(path);
	}

	private void SelectMarkerPath(NodePath path)
	{
		if (!(path == null) && !path.IsEmpty)
		{
			_binding.SetValue(_definition, "posMarkPath", Variant.From(in path), "在角色画面吸附槽位 Marker", _refreshTarget, _refreshMethod);
			RefreshPreview();
		}
	}

	private void SetBool(string property, bool value, string action)
	{
		_binding.SetValue(_definition, property, Variant.From(in value), action, _refreshTarget, _refreshMethod);
		RefreshPreview();
	}

	private Control ChoiceRow(string name, List<XWGameVisualChoiceCard> cards, params (string Key, string Title, string Detail, string Icon, Func<bool> Selected, Action Choose)[] options)
	{
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = name
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		for (int i = 0; i < options.Length; i++)
		{
			(string Key, string Title, string Detail, string Icon, Func<bool> Selected, Action Choose) option = options[i];
			XWGameVisualChoiceCard card = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(card))
			{
				continue;
			}
			card.Name = name + "_" + option.Key;
			card.Configure(option.Key, option.Title, option.Detail, ResourceLoader.Load<Texture2D>(option.Icon, null, ResourceLoader.CacheMode.Reuse), option.Selected());
			card.Pressed += delegate
			{
				option.Choose();
				RefreshPreview();
			};
			_binding.RegisterControlSynchronizer(delegate
			{
				if (GodotObject.IsInstanceValid(card))
				{
					card.SetSelected(option.Selected());
				}
			});
			cards.Add(card);
			hFlowContainer.AddChild(card, forceReadableName: false, Node.InternalMode.Disabled);
		}
		return hFlowContainer;
	}

	private static void RefreshChoiceCards(List<XWGameVisualChoiceCard> cards, string selected)
	{
		foreach (XWGameVisualChoiceCard card in cards)
		{
			if (GodotObject.IsInstanceValid(card))
			{
				card.SetSelected(card.ChoiceKey == selected);
			}
		}
	}

	private static Label LayerCard(Container host, string name, string title, Color color)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			Name = name,
			CustomMinimumSize = new Vector2(220f, 96f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		panelContainer.AddThemeStyleboxOverride("panel", PanelStyle(color, color.Lightened(0.28f), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer();
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeFontSizeOverride("font_size", 17);
		Label label2 = new Label
		{
			Name = name + "Status",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("d9edf5")
		};
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(label2, forceReadableName: false, Node.InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		host.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return label2;
	}

	private static PanelContainer Section(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", PanelStyle(new Color("172832"), new Color("31556a"), 10, 1));
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 12);
		marginContainer.AddThemeConstantOverride("margin_top", 10);
		marginContainer.AddThemeConstantOverride("margin_right", 12);
		marginContainer.AddThemeConstantOverride("margin_bottom", 10);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "SectionBody"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AddThemeColorOverride("font_color", new Color("a6e8ff"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("91a9b7")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		panelContainer.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private static Label StatusLabel(string name)
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

	private static StyleBoxFlat PanelStyle(Color background, Color border, int radius, int width)
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

	private static string CharacterName(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return "无";
		}
		if (!string.IsNullOrWhiteSpace(character.Name))
		{
			return character.Name;
		}
		return character.GetType().Name;
	}

	private static string VisibleText(bool visible)
	{
		if (!visible)
		{
			return "隐藏";
		}
		return "可见";
	}
}
