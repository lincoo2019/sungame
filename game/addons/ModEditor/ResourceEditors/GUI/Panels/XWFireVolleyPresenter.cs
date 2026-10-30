using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWFireVolleyPresenter : IDisposable
{
	private const string ChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal)
	{
		"firePosMarkerPaths", "fireEventName", "fireOverEventName", "fireIntervalBase", "fireInterval", "fireIntervalOffset", "fireNum", "fireNumAtOnce", "fireProjectileList", "fireAnimationStartPosition",
		"repeatFireAnimationStartPosition", "checkUse", "checkAllLine", "checkHeight", "airFirst", "catapultFirstFar", "randomChoose", "canTargetGargantuar", "checkGravestone"
	};

	private readonly FireComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly Node2D _previewWorld;

	private readonly List<XWGameVisualChoiceCard> _targetCards = new List<XWGameVisualChoiceCard>();

	private VBoxContainer _markerRows;

	private VBoxContainer _projectileRows;

	private Label _volleySummary;

	private XWFireVolleyTimeline _timeline;

	private XWFireVolleyOverlay _overlay;

	private Node _previewOwner;

	private bool _rebuildingRows;

	private int _markerStructureCount = -1;

	private string _projectileStructureToken = string.Empty;

	public PanelContainer Root { get; private set; }

	public Node2D Overlay => _overlay;

	public int MarkerHandleCount => _overlay?.MarkerHandleCount ?? 0;

	public int TrajectoryCount => _overlay?.TrajectoryCount ?? 0;

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

	public int PreviewRevision { get; private set; }

	public int MarkerRowCount => _markerRows?.GetChildCount() ?? 0;

	public int ProjectileRowCount => _projectileRows?.GetChildCount() ?? 0;

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

	public XWFireVolleyPresenter(FireComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod, Node2D previewWorld)
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
				Name = "FireVolleyPresenter",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			Root.SetProcess(enable: false);
			Root.SetPhysicsProcess(enable: false);
			Root.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("151b25"), new Color("e09245"), 14, 2));
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_top", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 16);
			marginContainer.AddThemeConstantOverride("margin_bottom", 16);
			Root.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "FireVolleyBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 12);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHeader(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateMarkerPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTargetPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateVolleyPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateProjectilePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewWorld))
			{
				_overlay = new XWFireVolleyOverlay
				{
					Name = "FireVolleyPreviewOverlay",
					ZIndex = 4090
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
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceProjectile.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(34f, 34f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		}, forceReadableName: false, Node.InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = "游戏内发射与齐射"
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color("ffe1a8"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "在角色枪口、目标和弹道所在位置直接编辑；拖动枪口会吸附到角色场景里最近的 Marker2D。",
			Modulate = new Color("c1a87e"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private Control CreateMarkerPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("FireMarkerPanel", "枪口与发射点", "路径相对角色根节点解析；每条弹道通过“枪口编号”引用这里的顺序。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_markerRows = new VBoxContainer
		{
			Name = "FireMarkerRows"
		};
		_markerRows.AddThemeConstantOverride("separation", 6);
		node.AddChild(_markerRows, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "FireAddMarkerButton",
			Text = "＋ 添加枪口引用",
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(190f, 40f)
		};
		button.Pressed += AddMarker;
		node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateTargetPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("FireTargetPanel", "目标与筛选卡", "检测范围继续使用角色预览里的真实碰撞图形；这里决定候选行、优先级和可命中类型。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "FireLaneTargetCards"
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddTargetCard(hFlowContainer, "same-line", "同行目标", "只在当前行检测", "res://addons/ModEditor/Icons/MainPlay.svg", () => !_definition.checkAllLine, delegate
		{
			SetBool("checkAllLine", value: false, "目标改为当前行");
		});
		AddTargetCard(hFlowContainer, "all-line", "跨行目标", "检测全部配置行", "res://addons/ModEditor/Icons/Game.svg", () => _definition.checkAllLine, delegate
		{
			SetBool("checkAllLine", value: true, "目标改为跨行");
		});
		HFlowContainer hFlowContainer2 = new HFlowContainer
		{
			Name = "FireTargetPriorityCards"
		};
		hFlowContainer2.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer2.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		AddTargetCard(hFlowContainer2, "nearest", "最近优先", "默认弹射目标", "res://addons/ModEditor/Icons/GuiTreeArrowRight.svg", () => !_definition.catapultFirstFar && !_definition.randomChoose, delegate
		{
			SetTargetPriority(farthest: false, random: false);
		});
		AddTargetCard(hFlowContainer2, "farthest", "最远优先", "投掷物远端目标", "res://addons/ModEditor/Icons/DistractionFree.svg", () => _definition.catapultFirstFar && !_definition.randomChoose, delegate
		{
			SetTargetPriority(farthest: true, random: false);
		});
		AddTargetCard(hFlowContainer2, "random", "随机目标", "候选中随机选择", "res://addons/ModEditor/Icons/Signals.svg", () => _definition.randomChoose, delegate
		{
			SetTargetPriority(farthest: false, random: true);
		});
		HFlowContainer hFlowContainer3 = new HFlowContainer
		{
			Name = "FireTargetFilterCards"
		};
		hFlowContainer3.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer3.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer3, forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer3.AddChild(CreateToggleCard("FireCheckUseCard", "启用目标检测", "checkUse"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer3.AddChild(CreateToggleCard("FireCheckHeightCard", "检查目标高度", "checkHeight"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer3.AddChild(CreateToggleCard("FireGargantuarCard", "允许巨人目标", "canTargetGargantuar"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer3.AddChild(CreateToggleCard("FireGravestoneCard", "允许墓碑目标", "checkGravestone"), forceReadableName: false, Node.InternalMode.Disabled);
		CheckButton node2 = CreateToggleCard("FireAirFirstCard", "空中目标优先", "airFirst");
		hFlowContainer3.AddChild(node2, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateVolleyPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("FireVolleyPanel", "齐射时间轴", "同时发射会在一个 fire 动画事件内循环；逐次发射会在每次 fire/fireOver 动画事件后推进。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_timeline = new XWFireVolleyTimeline
		{
			Name = "FireVolleyTimeline",
			CustomMinimumSize = new Vector2(0f, 128f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		node.AddChild(_timeline, forceReadableName: false, Node.InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "FireVolleyModeCards"
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddTargetCard(hFlowContainer, "repeated", "逐次动画事件", "每次事件发射一次", "res://addons/ModEditor/Icons/Reload.svg", () => !_definition.fireNumAtOnce, delegate
		{
			SetBool("fireNumAtOnce", value: false, "齐射改为逐次动画事件");
		});
		AddTargetCard(hFlowContainer, "at-once", "同一事件齐射", "一次事件循环发射", "res://addons/ModEditor/Icons/ResourceProjectileChange.svg", () => _definition.fireNumAtOnce, delegate
		{
			SetBool("fireNumAtOnce", value: true, "齐射改为同一事件发射");
		});
		GridContainer gridContainer = new GridContainer
		{
			Name = "FireVolleyValueGrid",
			Columns = 2,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		gridContainer.AddThemeConstantOverride("h_separation", 10);
		gridContainer.AddThemeConstantOverride("v_separation", 7);
		node.AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddBoundNumber(gridContainer, "齐射数量", "FireVolleyCount", _definition, "fireNum", 1.0, 64.0, 1.0);
		AddBoundNumber(gridContainer, "基础动画间隔", "FireVolleyBaseInterval", _definition, "fireIntervalBase", 0.01, 120.0, 0.05);
		AddBoundNumber(gridContainer, "实际发射间隔", "FireVolleyInterval", _definition, "fireInterval", 0.01, 120.0, 0.05);
		AddBoundNumber(gridContainer, "随机提前量", "FireVolleyIntervalOffset", _definition, "fireIntervalOffset", 0.0, 30.0, 0.01);
		AddBoundNumber(gridContainer, "首次动画起点", "FireAnimationStartPosition", _definition, "fireAnimationStartPosition", 0.0, 1.0, 0.01);
		AddBoundNumber(gridContainer, "连射动画起点", "FireRepeatAnimationStartPosition", _definition, "repeatFireAnimationStartPosition", 0.0, 1.0, 0.01);
		GridContainer gridContainer2 = new GridContainer
		{
			Columns = 2
		};
		gridContainer2.AddThemeConstantOverride("h_separation", 10);
		gridContainer2.AddThemeConstantOverride("v_separation", 7);
		node.AddChild(gridContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		AddBoundText(gridContainer2, "发射动画事件", "FireEventNameEdit", "fireEventName", "支持用 & 分隔多个动画命令");
		AddBoundText(gridContainer2, "连射结束事件", "FireOverEventNameEdit", "fireOverEventName", "留空时每次 fire 事件推进");
		_volleySummary = new Label
		{
			Name = "FireVolleySummary",
			Modulate = new Color("e3c487"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		node.AddChild(_volleySummary, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateProjectilePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("FireProjectilePanel", "弹道拼图", "每张卡直接编辑运行时使用的枪口编号、速度、方向角、目标行偏移和触发条件。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		_projectileRows = new VBoxContainer
		{
			Name = "FireProjectileRows"
		};
		_projectileRows.AddThemeConstantOverride("separation", 8);
		node.AddChild(_projectileRows, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "FireAddProjectileButton",
			Text = "＋ 添加弹道",
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceProjectile.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(170f, 40f)
		};
		button.Pressed += AddProjectile;
		node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private void RebuildMarkerRows()
	{
		if (!GodotObject.IsInstanceValid(_markerRows) || _rebuildingRows)
		{
			return;
		}
		_rebuildingRows = true;
		try
		{
			ClearChildren(_markerRows);
			int num = _definition.firePosMarkerPaths?.Count ?? 0;
			if (num == 0)
			{
				_markerRows.AddChild(new Label
				{
					Name = "FireMarkerEmptyHint",
					Text = "尚未配置枪口；弹道会回退到角色原点。",
					Modulate = new Color("b09b7c")
				}, forceReadableName: false, Node.InternalMode.Disabled);
				return;
			}
			for (int i = 0; i < num; i++)
			{
				int captured = i;
				HBoxContainer hBoxContainer = new HBoxContainer
				{
					Name = $"FireMarkerRow_{i}"
				};
				hBoxContainer.AddThemeConstantOverride("separation", 7);
				Label label = new Label
				{
					Text = $"枪口 {i}",
					CustomMinimumSize = new Vector2(72f, 38f),
					VerticalAlignment = VerticalAlignment.Center
				};
				label.AddThemeColorOverride("font_color", new Color("ffbd69"));
				hBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
				LineEdit path = new LineEdit
				{
					Name = $"FireMarkerPath_{i}",
					Text = (_definition.firePosMarkerPaths[i]?.ToString() ?? string.Empty),
					PlaceholderText = "角色根节点相对 Marker2D 路径",
					TooltipText = "firePosMarkerPaths" + $" · 第 {i} 项",
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};
				path.TextSubmitted += delegate(string value)
				{
					SelectMarkerPath(captured, new NodePath(value));
				};
				path.FocusExited += delegate
				{
					SelectMarkerPath(captured, new NodePath(path.Text));
				};
				hBoxContainer.AddChild(path, forceReadableName: false, Node.InternalMode.Disabled);
				Button button = new Button
				{
					Name = $"FireRemoveMarker_{i}",
					Text = "移除",
					Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
				};
				button.Pressed += delegate
				{
					RemoveMarker(captured);
				};
				hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
				_markerRows.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		finally
		{
			_rebuildingRows = false;
		}
	}

	private void RebuildProjectileRows()
	{
		if (!GodotObject.IsInstanceValid(_projectileRows) || _rebuildingRows)
		{
			return;
		}
		_rebuildingRows = true;
		try
		{
			ClearChildren(_projectileRows);
			int num = _definition.fireProjectileList?.Count ?? 0;
			if (num == 0)
			{
				_projectileRows.AddChild(new Label
				{
					Name = "FireProjectileEmptyHint",
					Text = "尚未配置弹道；动画事件不会创建投射物。",
					Modulate = new Color("b09b7c")
				}, forceReadableName: false, Node.InternalMode.Disabled);
				return;
			}
			for (int i = 0; i < num; i++)
			{
				FireComponentFireProjectileConfig fireComponentFireProjectileConfig = _definition.fireProjectileList[i];
				if (!GodotObject.IsInstanceValid(fireComponentFireProjectileConfig))
				{
					_projectileRows.AddChild(new Label
					{
						Name = $"FireProjectileMissing_{i}",
						Text = $"弹道 {i}：资源为空",
						Modulate = new Color("ff8f82")
					}, forceReadableName: false, Node.InternalMode.Disabled);
					continue;
				}
				int captured = i;
				PanelContainer panelContainer = CreateSectionPanel($"FireProjectileCard_{i}", $"弹道 {i}", "方向角与运行时 Vector2.FromAngle 一致；行偏移会换算成地图格 Y 位移。");
				VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
				GridContainer gridContainer = new GridContainer
				{
					Columns = 2
				};
				gridContainer.AddThemeConstantOverride("h_separation", 10);
				gridContainer.AddThemeConstantOverride("v_separation", 6);
				node.AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
				AddBoundNumber(gridContainer, "检测编号", $"FireProjectileCheck_{i}", fireComponentFireProjectileConfig, "checkProjectileId", 0.0, 255.0, 1.0);
				AddBoundNumber(gridContainer, "枪口编号", $"FireProjectileMuzzle_{i}", fireComponentFireProjectileConfig, "firePosId", 0.0, 255.0, 1.0);
				AddBoundNumber(gridContainer, "速度", $"FireProjectileSpeed_{i}", fireComponentFireProjectileConfig, "speed", 0.0, 10000.0, 5.0);
				AddBoundNumber(gridContainer, "方向角", $"FireProjectileDirection_{i}", fireComponentFireProjectileConfig, "dir", -360.0, 360.0, 1.0);
				AddBoundNumber(gridContainer, "目标行偏移", $"FireProjectileLaneOffset_{i}", fireComponentFireProjectileConfig, "offsetLine", -20.0, 20.0, 1.0);
				AddBoundNumber(gridContainer, "到第几发后跳过", $"FireProjectileSkip_{i}", fireComponentFireProjectileConfig, "fireNumSkip", -1.0, 255.0, 1.0);
				LineEdit lineEdit = new LineEdit
				{
					Name = $"FireProjectileEvent_{i}",
					PlaceholderText = "限定动画事件；留空表示全部",
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};
				node.AddChild(new Label
				{
					Text = "限定发射事件"
				}, forceReadableName: false, Node.InternalMode.Disabled);
				node.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
				_binding.BindText(lineEdit, fireComponentFireProjectileConfig, "fireEventNeed", RefreshPreview, _refreshTarget, _refreshMethod);
				CheckButton checkButton = new CheckButton
				{
					Name = $"FireProjectileFlip_{i}",
					Text = "水平翻折投射物外观"
				};
				node.AddChild(checkButton, forceReadableName: false, Node.InternalMode.Disabled);
				_binding.BindToggle(checkButton, fireComponentFireProjectileConfig, "projectileFlip", RefreshPreview, _refreshTarget, _refreshMethod);
				Button button = new Button
				{
					Name = $"FireRemoveProjectile_{i}",
					Text = "移除此弹道",
					Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
				};
				button.Pressed += delegate
				{
					RemoveProjectile(captured);
				};
				node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
				_projectileRows.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		finally
		{
			_rebuildingRows = false;
		}
	}

	private void AddBoundNumber(GridContainer host, string caption, string name, Resource owner, string property, double minimum, double maximum, double step)
	{
		host.AddChild(new Label
		{
			Text = caption,
			CustomMinimumSize = new Vector2(132f, 36f),
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			Name = name,
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			AllowLesser = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TooltipText = property
		};
		host.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		_binding.BindNumber(spinBox, owner, property, RefreshPreview, _refreshTarget, _refreshMethod);
	}

	private void AddBoundText(GridContainer host, string caption, string name, string property, string placeholder)
	{
		host.AddChild(new Label
		{
			Text = caption,
			CustomMinimumSize = new Vector2(132f, 36f),
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, Node.InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit
		{
			Name = name,
			PlaceholderText = placeholder,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TooltipText = property
		};
		host.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		_binding.BindText(lineEdit, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
	}

	private CheckButton CreateToggleCard(string name, string text, string property)
	{
		CheckButton checkButton = new CheckButton
		{
			Name = name,
			Text = text,
			CustomMinimumSize = new Vector2(205f, 42f),
			TooltipText = property
		};
		_binding.BindToggle(checkButton, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
		return checkButton;
	}

	private void AddTargetCard(Control host, string key, string title, string detail, string iconPath, Func<bool> selected, Action choose)
	{
		XWGameVisualChoiceCard card = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(card))
		{
			return;
		}
		card.Name = "FireChoice_" + key;
		card.Configure(key, title, detail, ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse), selected());
		card.Pressed += delegate
		{
			choose();
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(card))
			{
				card.SetSelected(selected());
			}
		});
		_targetCards.Add(card);
		host.AddChild(card, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void SetTargetPriority(bool farthest, bool random)
	{
		if (_refreshTarget is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor)
		{
			xWCharacterComponentVisualResourceEditor.SetFireTargetPriority(_definition, farthest, random);
			return;
		}
		_definition.catapultFirstFar = farthest;
		_definition.randomChoose = random;
		RefreshPreview();
	}

	private void SetBool(string property, bool value, string action)
	{
		_binding.SetValue(_definition, property, value, action, _refreshTarget, _refreshMethod);
	}

	private void AddMarker()
	{
		Array<NodePath> from = ((_definition.firePosMarkerPaths != null) ? _definition.firePosMarkerPaths.Duplicate(deep: true) : new Array<NodePath>());
		from.Add(new NodePath(""));
		_binding.SetValue(_definition, "firePosMarkerPaths", Variant.From(in from), "添加发射点", _refreshTarget, _refreshMethod);
	}

	private void RemoveMarker(int index)
	{
		if (index >= 0 && index < (_definition.firePosMarkerPaths?.Count ?? 0))
		{
			Array<NodePath> from = _definition.firePosMarkerPaths.Duplicate(deep: true);
			from.RemoveAt(index);
			_binding.SetValue(_definition, "firePosMarkerPaths", Variant.From(in from), $"移除枪口 {index}", _refreshTarget, _refreshMethod);
		}
	}

	public void SelectMarkerPath(int index, NodePath path)
	{
		if (index >= 0 && index < (_definition.firePosMarkerPaths?.Count ?? 0))
		{
			if ((object)path == null)
			{
				path = new NodePath("");
			}
			if (!string.Equals(_definition.firePosMarkerPaths[index]?.ToString(), path.ToString(), StringComparison.Ordinal))
			{
				Array<NodePath> from = _definition.firePosMarkerPaths.Duplicate(deep: true);
				from[index] = path;
				_binding.SetValue(_definition, "firePosMarkerPaths", Variant.From(in from), $"枪口 {index} 吸附到 {path}", _refreshTarget, _refreshMethod);
			}
		}
	}

	private void AddProjectile()
	{
		Array<FireComponentFireProjectileConfig> from = ((_definition.fireProjectileList != null) ? _definition.fireProjectileList.Duplicate() : new Array<FireComponentFireProjectileConfig>());
		from.Add(new FireComponentFireProjectileConfig());
		_binding.SetValue(_definition, "fireProjectileList", Variant.From(in from), "添加弹道", _refreshTarget, _refreshMethod);
	}

	private void RemoveProjectile(int index)
	{
		if (index >= 0 && index < (_definition.fireProjectileList?.Count ?? 0))
		{
			Array<FireComponentFireProjectileConfig> from = _definition.fireProjectileList.Duplicate();
			from.RemoveAt(index);
			_binding.SetValue(_definition, "fireProjectileList", Variant.From(in from), $"移除弹道 {index}", _refreshTarget, _refreshMethod);
		}
	}

	public void BindPreviewCharacter(Node owner)
	{
		_previewOwner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		RefreshPreview();
	}

	public bool DropMarkerHandleAt(int index, Vector2 localPosition)
	{
		if (GodotObject.IsInstanceValid(_overlay))
		{
			return _overlay.DropMarkerAt(index, localPosition);
		}
		return false;
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

	public void RefreshPreview()
	{
		if (!GodotObject.IsInstanceValid(_definition))
		{
			return;
		}
		PreviewRevision++;
		int num = _definition.firePosMarkerPaths?.Count ?? 0;
		if (_markerStructureCount != num)
		{
			_markerStructureCount = num;
			RebuildMarkerRows();
		}
		else
		{
			SynchronizeMarkerPathRows();
		}
		string text = BuildProjectileStructureToken();
		if (!string.Equals(_projectileStructureToken, text, StringComparison.Ordinal))
		{
			_projectileStructureToken = text;
			RebuildProjectileRows();
		}
		foreach (XWGameVisualChoiceCard targetCard in _targetCards)
		{
			if (GodotObject.IsInstanceValid(targetCard))
			{
				targetCard.QueueRedraw();
			}
		}
		if (GodotObject.IsInstanceValid(_volleySummary))
		{
			float num2 = Mathf.Abs(_definition.fireIntervalOffset);
			float value = Mathf.Max(0f, _definition.fireInterval - num2 * 2f);
			float value2 = Mathf.Max(0f, _definition.fireInterval - num2);
			_volleySummary.Text = (_definition.fireNumAtOnce ? $"动画事件“{_definition.fireEventName}”到达时，同时循环发射 {Math.Max(1, _definition.fireNum)} 次。" : $"每次动画事件发射 1 次，共 {Math.Max(1, _definition.fireNum)} 次；下一次检测约在 {value:0.##}～{value2:0.##} 秒后。");
		}
		_timeline?.Bind(_definition);
		_overlay?.Bind(_definition, _previewOwner);
	}

	private void SynchronizeMarkerPathRows()
	{
		if (!GodotObject.IsInstanceValid(_markerRows))
		{
			return;
		}
		for (int i = 0; i < (_definition.firePosMarkerPaths?.Count ?? 0); i++)
		{
			LineEdit lineEdit = _markerRows.FindChild($"FireMarkerPath_{i}", recursive: true, owned: false) as LineEdit;
			if (GodotObject.IsInstanceValid(lineEdit) && !lineEdit.HasFocus())
			{
				lineEdit.Text = _definition.firePosMarkerPaths[i]?.ToString() ?? string.Empty;
			}
		}
	}

	private string BuildProjectileStructureToken()
	{
		int num = _definition.fireProjectileList?.Count ?? 0;
		StringBuilder stringBuilder = new StringBuilder(num * 22 + 8);
		stringBuilder.Append(num).Append(':');
		for (int i = 0; i < num; i++)
		{
			FireComponentFireProjectileConfig fireComponentFireProjectileConfig = _definition.fireProjectileList[i];
			stringBuilder.Append(GodotObject.IsInstanceValid(fireComponentFireProjectileConfig) ? fireComponentFireProjectileConfig.GetInstanceId() : 0).Append(',');
		}
		return stringBuilder.ToString();
	}

	private static PanelContainer CreateSectionPanel(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("101722"), new Color("4b5868"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "SectionBody"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeColorOverride("font_color", new Color("ffca7a"));
		label.AddThemeFontSizeOverride("font_size", 16);
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			Modulate = new Color("96a5b8"),
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
		_targetCards.Clear();
		_markerRows = null;
		_projectileRows = null;
		_volleySummary = null;
		_timeline = null;
		_overlay = null;
		_previewOwner = null;
		_markerStructureCount = -1;
		_projectileStructureToken = string.Empty;
		Root = null;
	}
}
