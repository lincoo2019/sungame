using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWExplosionStagePresenter : IDisposable
{
	private const string ChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private static readonly string[] SupportedMethods = new string[5] { "Range", "Line", "Row", "Cross", "Slash" };

	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal)
	{
		"spritePath", "explodeAnimeClips", "explodeAnimeTimeScale", "hostAuthoritative", "skipGameRunning", "checkIZM", "explodeMethod", "explodeUse", "reload", "explodeOnce",
		"reverseAudio", "explodeAudio", "scaleExplodeShapeToMapGrid", "explodeRange", "explodeStateEvent", "idleStateEvent", "explodeJalaFireType", "explodeJalaNum", "explodeJalaOffset", "cameraShakeUse",
		"cameraShakeOffset", "cameraShakeForce", "cameraShakeInterval", "cameraShakeTime", "screenColorBlinkUse", "screenColorBlinkColor", "screenColorBlinkDuration", "screenColorBlinkRise", "craterCreateUse", "craterCreatePacketName"
	};

	private readonly ExplodeComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly Node2D _previewWorld;

	private readonly Action<string> _geometryMethodRequested;

	private readonly List<XWGameVisualChoiceCard> _methodCards = new List<XWGameVisualChoiceCard>();

	private VBoxContainer _offsetRows;

	private Label _validationLabel;

	private Label _geometrySummary;

	private XWExplosionStageTimeline _timeline;

	private XWExplosionStageOverlay _overlay;

	private Node _previewOwner;

	private int _offsetStructureCount = -1;

	private bool _rebuildingOffsets;

	public PanelContainer Root { get; private set; }

	public XWExplosionStageOverlay Overlay => _overlay;

	public int PreviewRevision { get; private set; }

	public int MethodCardCount => _methodCards.Count;

	public int OffsetCount => (_definition?.explodeJalaOffset?.Count).GetValueOrDefault();

	public string ValidationText => _validationLabel?.Text ?? BuildValidationText();

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
	}

	public XWExplosionStagePresenter(ExplodeComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod, Node2D previewWorld, Action<string> geometryMethodRequested)
	{
		_definition = definition;
		_binding = binding;
		_refreshTarget = refreshTarget;
		_refreshMethod = refreshMethod;
		_previewWorld = previewWorld;
		_geometryMethodRequested = geometryMethodRequested;
	}

	public void Mount(VBoxContainer host)
	{
		if (GodotObject.IsInstanceValid(host) && GodotObject.IsInstanceValid(_definition) && _binding != null)
		{
			Root = new PanelContainer
			{
				Name = "ExplosionStagePresenter"
			};
			Root.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("0d121b"), new Color("7d4b3d"), 12, 2));
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "ExplosionStageBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 10);
			vBoxContainer.AddChild(CreateTitleBlock(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateGeometryPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTimingPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateCorePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreatePresentationPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateCameraScreenPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateCraterStatePanel(), forceReadableName: false, Node.InternalMode.Disabled);
			Root.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewWorld))
			{
				_overlay = new XWExplosionStageOverlay
				{
					Name = "ExplosionStageOverlay",
					ZIndex = 120
				};
				_previewWorld.AddChild(_overlay, forceReadableName: false, Node.InternalMode.Disabled);
				_overlay.Bind(_definition, _previewOwner);
				_overlay.ConfigureEditing(BeginRangeOverlayEdit, PreviewRangeOverlayEdit, CommitRangeOverlayEdit, BeginOffsetOverlayEdit, PreviewOffsetOverlayEdit, CommitOffsetOverlayEdit);
			}
			RefreshPreview();
		}
	}

	private Control CreateTitleBlock()
	{
		VBoxContainer obj = new VBoxContainer
		{
			Name = "ExplosionStageHeader"
		};
		Label label = new Label
		{
			Text = "爆炸舞台 / Explosion Stage"
		};
		label.AddThemeFontSizeOverride("font_size", 19);
		label.AddThemeColorOverride("font_color", new Color("ffb06b"));
		obj.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		obj.AddChild(new Label
		{
			Text = "在游戏位置编辑范围、方向、表现与触发时间；事件、特效场景和碰撞资源继续由下方通用资源控件直接编辑。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("aeb9c7")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_validationLabel = new Label
		{
			Name = "ExplosionValidation",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_validationLabel.AddThemeColorOverride("font_color", new Color("ff8d79"));
		obj.AddChild(_validationLabel, forceReadableName: false, Node.InternalMode.Disabled);
		return obj;
	}

	private Control CreateGeometryPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionGeometryPanel", "爆炸几何", "图形卡只写入运行时支持的五种方法。Range 使用 explodeRange 网格框；方向模式不会把 explodeShape 冒充伤害范围。");
		VBoxContainer child = panelContainer.GetChild<VBoxContainer>(0);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ExplosionMethodCards"
		};
		ButtonGroup buttonGroup = new ButtonGroup();
		(string, string, string, string)[] obj = new(string, string, string, string)[5]
		{
			("Range", "范围", "▣ 中心网格", "res://addons/ModEditor/Icons/2D.svg"),
			("Line", "整行", "↔ 横向", "res://addons/ModEditor/Icons/CombineLines.svg"),
			("Row", "整列", "↕ 纵向", "res://addons/ModEditor/Icons/MoveUp.svg"),
			("Cross", "十字", "＋ 行列", "res://addons/ModEditor/Icons/ResourceMap.svg"),
			("Slash", "斜线", "╱ 对角", "res://addons/ModEditor/Icons/FlowPort.svg")
		};
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		(string, string, string, string)[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			string item4 = tuple.Item4;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = packedScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Name = "ExplosionMethod_" + item;
				xWGameVisualChoiceCard.ToggleMode = true;
				xWGameVisualChoiceCard.ButtonGroup = buttonGroup;
				xWGameVisualChoiceCard.CustomMinimumSize = new Vector2(126f, 68f);
				xWGameVisualChoiceCard.Configure(item, item2, item3, ResourceLoader.Load<Texture2D>(item4, null, ResourceLoader.CacheMode.Reuse), string.Equals(_definition.explodeMethod, item, StringComparison.Ordinal));
				string captured = item;
				xWGameVisualChoiceCard.Pressed += delegate
				{
					RequestGeometryMethod(captured);
				};
				_methodCards.Add(xWGameVisualChoiceCard);
				hFlowContainer.AddChild(xWGameVisualChoiceCard, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		child.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		child.AddChild(new Label
		{
			Name = "ExplosionGeometryUndoHint",
			Text = "切换到方向模式时，若偏移列表为空，会在同一个撤销步骤中补入起点 0；Range 不会改写“网格范围为权威”开关。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("aeb9c7")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_geometrySummary = new Label
		{
			Name = "ExplosionGeometrySummary",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("ffd2a3")
		};
		child.AddChild(_geometrySummary, forceReadableName: false, Node.InternalMode.Disabled);
		GridContainer gridContainer = new GridContainer
		{
			Name = "ExplosionRangeGrid",
			Columns = 2
		};
		gridContainer.AddThemeConstantOverride("h_separation", 8);
		gridContainer.AddThemeConstantOverride("v_separation", 6);
		AddBoundVector2(gridContainer, "Range 半径（格）", "ExplosionRange", "explodeRange", 0.0, 64.0, 0.05);
		AddBoundToggle(gridContainer, "网格范围为权威", "ExplosionGridRangeAuthority", "scaleExplodeShapeToMapGrid");
		AddBoundText(gridContainer, "方向火焰类型", "ExplosionJalaFireType", "explodeJalaFireType", "Fire");
		AddBoundNumber(gridContainer, "方向伤害", "ExplosionJalaDamage", "explodeJalaNum", 0.0, 1000000.0, 1.0);
		child.AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "ExplosionOffsetHeader"
		};
		hBoxContainer.AddChild(new Label
		{
			Text = "方向起点 Y 偏移（格）",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		}, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "ExplosionAddOffsetButton",
			Text = "添加偏移",
			Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Add.svg", null, ResourceLoader.CacheMode.Reuse)
		};
		button.Pressed += AddOffset;
		hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		child.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_offsetRows = new VBoxContainer
		{
			Name = "ExplosionOffsetRows"
		};
		child.AddChild(_offsetRows, forceReadableName: false, Node.InternalMode.Disabled);
		RebuildOffsetRows();
		return panelContainer;
	}

	private Control CreateTimingPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionTimingPanel", "表现时间轴", "爆炸瞬间同时触发音效、特效、镜头、屏闪和弹坑；条带显示持续时间，不伪造不存在的动画时长。");
		VBoxContainer child = panelContainer.GetChild<VBoxContainer>(0);
		_timeline = new XWExplosionStageTimeline
		{
			Name = "ExplosionStageTimeline",
			CustomMinimumSize = new Vector2(360f, 176f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_timeline.Bind(_definition);
		child.AddChild(_timeline, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateCorePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionCorePanel", "触发与权限", "这些开关决定爆炸是否执行、是否一次性销毁，以及由主机还是本地视觉阶段处理。");
		GridContainer gridContainer = CreateFieldGrid();
		AddBoundToggle(gridContainer, "执行爆炸", "ExplosionUse", "explodeUse");
		AddBoundToggle(gridContainer, "已装填", "ExplosionReload", "reload");
		AddBoundToggle(gridContainer, "爆炸后销毁", "ExplosionOnce", "explodeOnce");
		AddBoundToggle(gridContainer, "主机权威", "ExplosionHostAuthority", "hostAuthoritative");
		AddBoundToggle(gridContainer, "允许暂停阶段触发", "ExplosionSkipGameRunning", "skipGameRunning");
		AddBoundToggle(gridContainer, "检查 IZM 护甲", "ExplosionCheckIZM", "checkIZM");
		panelContainer.GetChild<VBoxContainer>(0).AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreatePresentationPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionPresentationPanel", "动画、音效与锚点", "爆炸特效实际位于角色 transformPoint 上方 30 像素；场景和事件资源继续在通用资源行中直接选择。");
		GridContainer gridContainer = CreateFieldGrid();
		AddBoundNodePath(gridContainer, "动画节点路径", "ExplosionSpritePath", "spritePath");
		AddBoundNumber(gridContainer, "动画速度", "ExplosionAnimationTimeScale", "explodeAnimeTimeScale", 0.01, 20.0, 0.05);
		AddBoundText(gridContainer, "爆炸动画片段", "ExplosionAnimeClip", "explodeAnimeClips", "Explode");
		AddBoundText(gridContainer, "爆炸音效", "ExplosionAudio", "explodeAudio", "音效注册名");
		AddBoundText(gridContainer, "反向音效", "ExplosionReverseAudio", "reverseAudio", "ReverseExplosion");
		panelContainer.GetChild<VBoxContainer>(0).AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateCameraScreenPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionCameraScreenPanel", "镜头与屏幕反馈", "舞台同时显示镜头随机偏移盒和屏闪边框；隐藏预览页后不会继续 Process 或输入。");
		GridContainer gridContainer = CreateFieldGrid();
		AddBoundToggle(gridContainer, "镜头震动", "ExplosionCameraUse", "cameraShakeUse");
		AddBoundVector2(gridContainer, "随机偏移", "ExplosionCameraOffset", "cameraShakeOffset", 0.0, 4096.0, 0.1);
		AddBoundNumber(gridContainer, "震动力度", "ExplosionCameraForce", "cameraShakeForce", 0.0, 100.0, 0.05);
		AddBoundNumber(gridContainer, "震动间隔", "ExplosionCameraInterval", "cameraShakeInterval", 0.0, 60.0, 0.01);
		AddBoundNumber(gridContainer, "震动时长", "ExplosionCameraTime", "cameraShakeTime", 0.0, 60.0, 0.05);
		AddBoundToggle(gridContainer, "屏幕闪色", "ExplosionScreenBlinkUse", "screenColorBlinkUse");
		AddBoundColor(gridContainer, "闪色", "ExplosionScreenBlinkColor", "screenColorBlinkColor");
		AddBoundNumber(gridContainer, "闪色时长", "ExplosionScreenBlinkDuration", "screenColorBlinkDuration", 0.0, 60.0, 0.05);
		AddBoundToggle(gridContainer, "渐强闪色", "ExplosionScreenBlinkRise", "screenColorBlinkRise");
		panelContainer.GetChild<VBoxContainer>(0).AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateCraterStatePanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("ExplosionCraterStatePanel", "弹坑与状态", "弹坑位于角色当前格；状态事件名直接写入 Definition，不占用右侧 Inspector。");
		GridContainer gridContainer = CreateFieldGrid();
		AddBoundToggle(gridContainer, "创建弹坑", "ExplosionCraterUse", "craterCreateUse");
		AddBoundText(gridContainer, "弹坑包名", "ExplosionCraterPacket", "craterCreatePacketName", "Crater");
		AddBoundText(gridContainer, "爆炸状态事件", "ExplosionStateEvent", "explodeStateEvent", "ToExplode");
		AddBoundText(gridContainer, "返回待机事件", "ExplosionIdleStateEvent", "idleStateEvent", "ToIdle");
		panelContainer.GetChild<VBoxContainer>(0).AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private void RequestGeometryMethod(string method)
	{
		if (IsSupportedMethod(method))
		{
			if (_geometryMethodRequested != null)
			{
				_geometryMethodRequested(method);
				return;
			}
			_binding.SetValue(_definition, "explodeMethod", method, "切换爆炸几何", _refreshTarget, _refreshMethod);
			RefreshPreview();
		}
	}

	private void AddOffset()
	{
		Array<int> array = ((_definition.explodeJalaOffset != null) ? _definition.explodeJalaOffset.Duplicate(deep: true) : new Array<int>());
		array.Add((array.Count != 0) ? (array[array.Count - 1] + 1) : 0);
		SetOffsets(array, "添加方向偏移");
	}

	private void RemoveOffset(int index)
	{
		if (index >= 0 && index < (_definition.explodeJalaOffset?.Count ?? 0))
		{
			Array<int> array = _definition.explodeJalaOffset.Duplicate(deep: true);
			array.RemoveAt(index);
			SetOffsets(array, $"移除方向偏移 {index + 1}");
		}
	}

	private void SetOffsets(Array<int> value, string action)
	{
		_binding.SetValue(_definition, "explodeJalaOffset", Variant.From(in value), action, _refreshTarget, _refreshMethod);
		RefreshPreview();
	}

	private void BeginRangeOverlayEdit()
	{
		_binding.BeginEdit(_definition, "explodeRange");
	}

	private void PreviewRangeOverlayEdit(Vector2 value)
	{
		_binding.PreviewValue(_definition, "explodeRange", Variant.From(in value));
		RefreshPreview();
	}

	private void CommitRangeOverlayEdit(Vector2 value)
	{
		_binding.CommitEdit(_definition, "explodeRange", Variant.From(in value), "在游戏位置调整爆炸范围", _refreshTarget, _refreshMethod);
		RefreshPreview();
	}

	private void BeginOffsetOverlayEdit(int index)
	{
		if (index >= 0 && index < (_definition.explodeJalaOffset?.Count ?? 0))
		{
			_binding.BeginEdit(_definition, "explodeJalaOffset");
		}
	}

	private void PreviewOffsetOverlayEdit(int index, int value)
	{
		if (index >= 0 && index < (_definition.explodeJalaOffset?.Count ?? 0))
		{
			Array<int> from = _definition.explodeJalaOffset.Duplicate(deep: true);
			from[index] = value;
			_binding.PreviewValue(_definition, "explodeJalaOffset", Variant.From(in from));
			RefreshPreview();
		}
	}

	private void CommitOffsetOverlayEdit(int index, int value)
	{
		if (index >= 0 && index < (_definition.explodeJalaOffset?.Count ?? 0))
		{
			Array<int> from = _definition.explodeJalaOffset.Duplicate(deep: true);
			from[index] = value;
			_binding.CommitEdit(_definition, "explodeJalaOffset", Variant.From(in from), "在游戏位置调整爆炸方向偏移", _refreshTarget, _refreshMethod);
			RefreshPreview();
		}
	}

	private void RebuildOffsetRows()
	{
		if (!GodotObject.IsInstanceValid(_offsetRows))
		{
			return;
		}
		_rebuildingOffsets = true;
		ClearChildren(_offsetRows);
		int num = _definition.explodeJalaOffset?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			int captured = i;
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"ExplosionOffsetRow_{i}"
			};
			hBoxContainer.AddChild(new Label
			{
				Text = $"#{i + 1}",
				CustomMinimumSize = new Vector2(42f, 36f),
				VerticalAlignment = VerticalAlignment.Center
			}, forceReadableName: false, Node.InternalMode.Disabled);
			SpinBox spinBox = new SpinBox
			{
				Name = $"ExplosionOffset_{i}",
				MinValue = -64.0,
				MaxValue = 64.0,
				Step = 1.0,
				Value = _definition.explodeJalaOffset[i],
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				TooltipText = "explodeJalaOffset"
			};
			spinBox.FocusEntered += delegate
			{
				_binding.BeginEdit(_definition, "explodeJalaOffset");
			};
			spinBox.ValueChanged += delegate(double next)
			{
				if (!_rebuildingOffsets && captured < (_definition.explodeJalaOffset?.Count ?? 0))
				{
					Array<int> from = _definition.explodeJalaOffset.Duplicate(deep: true);
					from[captured] = (int)Math.Round(next);
					_binding.PreviewValue(_definition, "explodeJalaOffset", Variant.From(in from));
					RefreshPreview();
				}
			};
			spinBox.FocusExited += delegate
			{
				if (captured < (_definition.explodeJalaOffset?.Count ?? 0))
				{
					Array<int> from = _definition.explodeJalaOffset.Duplicate(deep: true);
					_binding.CommitEdit(_definition, "explodeJalaOffset", Variant.From(in from), "修改方向偏移", _refreshTarget, _refreshMethod);
					RefreshPreview();
				}
			};
			hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
			Button button = new Button
			{
				Name = $"ExplosionRemoveOffset_{i}",
				Text = "移除",
				Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
			};
			button.Pressed += delegate
			{
				RemoveOffset(captured);
			};
			hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			_offsetRows.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		}
		if (num == 0)
		{
			_offsetRows.AddChild(new Label
			{
				Name = "ExplosionOffsetEmpty",
				Text = "没有偏移：Line / Row / Cross / Slash 当前不会执行方向爆炸。",
				Modulate = new Color("ff8d79")
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
		_offsetStructureCount = num;
		_rebuildingOffsets = false;
	}

	private void SynchronizeOffsetRows()
	{
		if (!GodotObject.IsInstanceValid(_offsetRows))
		{
			return;
		}
		_rebuildingOffsets = true;
		for (int i = 0; i < (_definition.explodeJalaOffset?.Count ?? 0); i++)
		{
			SpinBox spinBox = _offsetRows.FindChild($"ExplosionOffset_{i}", recursive: true, owned: false) as SpinBox;
			if (GodotObject.IsInstanceValid(spinBox) && !spinBox.HasFocus())
			{
				spinBox.SetValueNoSignal(_definition.explodeJalaOffset[i]);
			}
		}
		_rebuildingOffsets = false;
	}

	private GridContainer CreateFieldGrid()
	{
		GridContainer gridContainer = new GridContainer();
		gridContainer.Columns = 2;
		gridContainer.AddThemeConstantOverride("h_separation", 8);
		gridContainer.AddThemeConstantOverride("v_separation", 6);
		return gridContainer;
	}

	private void AddBoundToggle(GridContainer host, string caption, string name, string property)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
		CheckButton checkButton = new CheckButton
		{
			Name = name,
			Text = caption,
			TooltipText = property
		};
		host.AddChild(checkButton, forceReadableName: false, Node.InternalMode.Disabled);
		_binding.BindToggle(checkButton, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
	}

	private void AddBoundNumber(GridContainer host, string caption, string name, string property, double minimum, double maximum, double step)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			Name = name,
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			AllowLesser = false,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TooltipText = property
		};
		host.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		_binding.BindNumber(spinBox, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
	}

	private void AddBoundText(GridContainer host, string caption, string name, string property, string placeholder)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
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

	private void AddBoundNodePath(GridContainer host, string caption, string name, string property)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
		LineEdit edit = new LineEdit
		{
			Name = name,
			PlaceholderText = "角色内 NodePath",
			Text = _definition.Get(property).AsNodePath().ToString(),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TooltipText = property
		};
		host.AddChild(edit, forceReadableName: false, Node.InternalMode.Disabled);
		edit.FocusEntered += delegate
		{
			_binding.BeginEdit(_definition, property);
		};
		edit.TextChanged += delegate(string text)
		{
			_binding.PreviewValue(_definition, property, Variant.From<NodePath>(new NodePath(text ?? string.Empty)));
			RefreshPreview();
		};
		edit.TextSubmitted += delegate(string text)
		{
			_binding.CommitEdit(_definition, property, Variant.From<NodePath>(new NodePath(text ?? string.Empty)), "修改 " + property, _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		edit.FocusExited += delegate
		{
			_binding.CommitEdit(_definition, property, Variant.From<NodePath>(new NodePath(edit.Text ?? string.Empty)), "修改 " + property, _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(edit) && GodotObject.IsInstanceValid(_definition) && !edit.HasFocus())
			{
				edit.Text = _definition.Get(property).AsNodePath().ToString();
			}
		});
	}

	private void AddBoundVector2(GridContainer host, string caption, string name, string property, double minimum, double maximum, double step)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = name,
			TooltipText = property
		};
		SpinBox x = new SpinBox
		{
			Name = name + "X",
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Prefix = "X ",
			TooltipText = property
		};
		SpinBox y = new SpinBox
		{
			Name = name + "Y",
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Prefix = "Y ",
			TooltipText = property
		};
		Vector2 vector = _definition.Get(property).AsVector2();
		x.Value = vector.X;
		y.Value = vector.Y;
		hBoxContainer.AddChild(x, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(y, forceReadableName: false, Node.InternalMode.Disabled);
		host.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		Action value = delegate
		{
			_binding.BeginEdit(_definition, property);
		};
		Action preview = delegate
		{
			_binding.PreviewValue(_definition, property, Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value)));
			RefreshPreview();
		};
		Action value2 = delegate
		{
			_binding.CommitEdit(_definition, property, Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value)), "修改 " + property, _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		x.FocusEntered += value;
		y.FocusEntered += value;
		x.ValueChanged += delegate
		{
			preview();
		};
		y.ValueChanged += delegate
		{
			preview();
		};
		x.FocusExited += value2;
		y.FocusExited += value2;
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(x) && GodotObject.IsInstanceValid(y) && GodotObject.IsInstanceValid(_definition))
			{
				Vector2 vector2 = _definition.Get(property).AsVector2();
				if (!x.HasFocus())
				{
					x.SetValueNoSignal(vector2.X);
				}
				if (!y.HasFocus())
				{
					y.SetValueNoSignal(vector2.Y);
				}
			}
		});
	}

	private void AddBoundColor(GridContainer host, string caption, string name, string property)
	{
		host.AddChild(CreateFieldLabel(caption), forceReadableName: false, Node.InternalMode.Disabled);
		ColorPickerButton picker = new ColorPickerButton
		{
			Name = name,
			Color = _definition.Get(property).AsColor(),
			EditAlpha = true,
			TooltipText = property
		};
		host.AddChild(picker, forceReadableName: false, Node.InternalMode.Disabled);
		picker.Pressed += delegate
		{
			_binding.BeginEdit(_definition, property);
		};
		picker.ColorChanged += delegate(Color color)
		{
			_binding.PreviewValue(_definition, property, Variant.From(in color));
			RefreshPreview();
		};
		picker.PopupClosed += delegate
		{
			_binding.CommitEdit(_definition, property, Variant.From<Color>(picker.Color), "修改 " + property, _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(picker) && GodotObject.IsInstanceValid(_definition))
			{
				picker.Color = _definition.Get(property).AsColor();
			}
		});
	}

	private static Label CreateFieldLabel(string caption)
	{
		return new Label
		{
			Text = caption,
			CustomMinimumSize = new Vector2(168f, 36f),
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	public void BindPreviewCharacter(Node owner)
	{
		_previewOwner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		_overlay?.Bind(_definition, _previewOwner);
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
		foreach (XWGameVisualChoiceCard methodCard in _methodCards)
		{
			if (GodotObject.IsInstanceValid(methodCard))
			{
				methodCard.SetSelected(string.Equals(methodCard.ChoiceKey, _definition.explodeMethod, StringComparison.Ordinal));
			}
		}
		int num = _definition.explodeJalaOffset?.Count ?? 0;
		if (_offsetStructureCount != num)
		{
			RebuildOffsetRows();
		}
		else
		{
			SynchronizeOffsetRows();
		}
		if (GodotObject.IsInstanceValid(_geometrySummary))
		{
			_geometrySummary.Text = BuildGeometrySummary();
		}
		if (GodotObject.IsInstanceValid(_validationLabel))
		{
			_validationLabel.Text = BuildValidationText();
		}
		_timeline?.Bind(_definition);
		_overlay?.Bind(_definition, _previewOwner);
	}

	private string BuildGeometrySummary()
	{
		string text = _definition.explodeMethod ?? string.Empty;
		if (text == "Range")
		{
			return $"范围半径：{Mathf.Abs(_definition.explodeRange.X):0.##} × {Mathf.Abs(_definition.explodeRange.Y):0.##} 格；舞台按 80 × 98 默认网格换算。";
		}
		int value = _definition.explodeJalaOffset?.Count ?? 0;
		if (!IsSupportedMethod(text))
		{
			return "未知方法“" + text + "”：运行时不会进入任何伤害分支。";
		}
		return $"{text}：{value} 个 Y 偏移起点，伤害 {_definition.explodeJalaNum:0.##}，类型 {_definition.explodeJalaFireType}。";
	}

	private string BuildValidationText()
	{
		List<string> list = new List<string>();
		string text = _definition.explodeMethod ?? string.Empty;
		if (!IsSupportedMethod(text))
		{
			list.Add("未知 explodeMethod“" + text + "”：仅支持 Range / Line / Row / Cross / Slash；当前配置只有表现，没有对应伤害分支。");
		}
		if (text == "Range" && _definition.explodeUse)
		{
			Array<TowerDefenseCharacterEventBase> explodeEvent = _definition.explodeEvent;
			if (explodeEvent == null || explodeEvent.Count == 0)
			{
				list.Add("Range 已启用但 explodeEvent 为空：TowerDefenseExplode 会直接返回，当前爆炸没有范围效果。");
			}
		}
		if (text == "Range" && !_definition.scaleExplodeShapeToMapGrid)
		{
			list.Add("当前旧兼容开关会让 runtime 从 explodeShape 换算范围；舞台不会把 shape 冒充网格范围，请核对并切回“网格范围为权威”。");
		}
		if (text != "Range" && IsSupportedMethod(text))
		{
			Array<int> explodeJalaOffset = _definition.explodeJalaOffset;
			if (explodeJalaOffset == null || explodeJalaOffset.Count == 0)
			{
				list.Add(text + " 没有任何 explodeJalaOffset：运行时循环为空，不会执行方向爆炸。");
			}
		}
		if (_definition.craterCreateUse && string.IsNullOrWhiteSpace(_definition.craterCreatePacketName))
		{
			list.Add("已启用弹坑但弹坑包名为空。");
		}
		if (_definition.cameraShakeUse && (_definition.cameraShakeTime <= 0 || _definition.cameraShakeInterval <= 0f))
		{
			list.Add("镜头震动已启用，但时长或间隔不大于 0。");
		}
		if (list.Count != 0)
		{
			return string.Join("\n", list);
		}
		return "配置有效：舞台显示的是实际方法语义；explodeShape 只保留给通用碰撞资源编辑。";
	}

	public static bool IsSupportedMethod(string method)
	{
		string[] supportedMethods = SupportedMethods;
		foreach (string b in supportedMethods)
		{
			if (string.Equals(method, b, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static PanelContainer CreateSectionPanel(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("111824"), new Color("49586b"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = name + "Body"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeColorOverride("font_color", new Color("ffc08a"));
		label.AddThemeFontSizeOverride("font_size", 16);
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			Modulate = new Color("9ba9ba"),
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
			_overlay.SetProcess(enable: false);
			_overlay.SetPhysicsProcess(enable: false);
			_overlay.SetProcessInput(enable: false);
			if (_overlay.GetParent() != null)
			{
				_overlay.GetParent().RemoveChild(_overlay);
			}
			_overlay.QueueFree();
		}
		_methodCards.Clear();
		_offsetRows = null;
		_validationLabel = null;
		_geometrySummary = null;
		_timeline = null;
		_overlay = null;
		_previewOwner = null;
		Root = null;
	}
}
