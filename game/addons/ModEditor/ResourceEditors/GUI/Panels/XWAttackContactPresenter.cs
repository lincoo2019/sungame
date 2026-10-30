using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWAttackContactPresenter : IDisposable
{
	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal)
	{
		"attackType", "useParentHitBox", "checkIntreval", "attackAnimeClipsArray", "attackAnimeClips", "attackEventName", "attackAnimeTimeScale", "attackIntervalBase", "attackInterval", "attackIntervalOffset",
		"useZombieAttackCheck", "checkGrid", "useCheckAreaGridColumn", "checkEachShape", "checkLine", "checkTall", "checkVase", "checkBowling", "checkGravestone", "checkAll",
		"fliterLadder"
	};

	private readonly AttackComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly Node2D _previewWorld;

	private readonly List<XWGameVisualChoiceCard> _choiceCards = new List<XWGameVisualChoiceCard>();

	private VBoxContainer _completionRows;

	private Label _validationLabel;

	private XWAttackContactTimeline _timeline;

	private XWAttackContactOverlay _overlay;

	private Node _previewOwner;

	private AttackComponent _runtime;

	private string _structureToken = string.Empty;

	private int _targetCardCount;

	public PanelContainer Root { get; private set; }

	public Node2D Overlay => _overlay;

	public int PreviewRevision { get; private set; }

	public int CompletionRowCount => _completionRows?.GetChildCount() ?? 0;

	public int TargetCardCount => _targetCardCount;

	public int TargetDummyCount => _overlay?.TargetDummyCount ?? 0;

	public int OverlayRedrawRevision => _overlay?.RedrawRevision ?? 0;

	public string StructureToken => _structureToken;

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

	public XWAttackContactPresenter(AttackComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod, Node2D previewWorld)
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
				Name = "AttackContactPresenter",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			Root.SetProcess(enable: false);
			Root.SetPhysicsProcess(enable: false);
			Root.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("151b25"), new Color("e55e4c"), 14, 2));
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_top", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 16);
			marginContainer.AddThemeConstantOverride("margin_bottom", 16);
			Root.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "AttackContactBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 12);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHeader(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateContactPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTargetPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateTimingPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateValidationPanel(), forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_previewWorld))
			{
				_overlay = new XWAttackContactOverlay
				{
					Name = "AttackContactPreviewOverlay",
					ZIndex = 4085
				};
				_overlay.GeometryProfileRequested += ChooseTargetGeometryProfile;
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
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", null, ResourceLoader.CacheMode.Reuse),
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
			Text = "攻击接触与命中帧"
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color("ffd1a8"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "在游戏画面中确认真实接触区、目标空间组合和动画命中点；复杂碰撞形状仍由统一碰撞资源编辑器负责。",
			Modulate = new Color("c4aa9a"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private Control CreateContactPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("AttackContactPanel", "接触区与攻击类型", "父碰撞模式会覆盖形状列表；逐形状命中用于要求候选真实接触至少一个独立形状。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "AttackContactModeCards"
		};
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddChoiceCard(hFlowContainer, "shape-contact", "配置形状", "使用攻击检测形状", "res://addons/ModEditor/Icons/2D.svg", () => !_definition.useParentHitBox, delegate
		{
			SetBool("useParentHitBox", value: false, "攻击接触改为配置形状");
		});
		AddChoiceCard(hFlowContainer, "parent-hitbox", "角色碰撞", "直接复用角色 WorldHitRect", "res://addons/ModEditor/Icons/ResourceCharacter.svg", () => _definition.useParentHitBox, delegate
		{
			SetBool("useParentHitBox", value: true, "攻击接触改为角色碰撞");
		});
		HFlowContainer hFlowContainer2 = new HFlowContainer
		{
			Name = "AttackTypeCards"
		};
		node.AddChild(hFlowContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		AddAttackTypeCard(hFlowContainer2, "Default", "普通", "默认伤害标签");
		AddAttackTypeCard(hFlowContainer2, "Eat", "啃咬", "植物被吞食语义");
		AddAttackTypeCard(hFlowContainer2, "Smash", "砸击", "重击伤害标签");
		AddAttackTypeCard(hFlowContainer2, "Chomp", "吞咬", "吞咬伤害标签");
		HFlowContainer hFlowContainer3 = new HFlowContainer
		{
			Name = "AttackContactToggles"
		};
		hFlowContainer3.AddChild(CreateToggle("AttackEachShapeToggle", "候选必须命中单个形状", "checkEachShape"), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(hFlowContainer3, forceReadableName: false, Node.InternalMode.Disabled);
		GridContainer gridContainer = new GridContainer
		{
			Columns = 2
		};
		node.AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddBoundNumber(gridContainer, "目标复查帧", "AttackCheckInterval", "checkIntreval", 0.0, 120.0, 1.0);
		return panelContainer;
	}

	private Control CreateTargetPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("AttackTargetPanel", "目标假人与过滤卡", "六种空间组合会作为一个撤销动作写入；点击预览中的目标假人也可依次切换。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "AttackGeometryCards"
		};
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddGeometryCard(hFlowContainer, 0, "仅接触区", "不追加同行和格列约束", () => !_definition.checkLine && !_definition.checkGrid);
		AddGeometryCard(hFlowContainer, 1, "同排", "仅追加与攻击者同行约束", () => _definition.checkLine && !_definition.checkGrid);
		AddGeometryCard(hFlowContainer, 2, "当前格", "角色所在格窗口，不限制同行", () => !_definition.checkLine && _definition.checkGrid && !_definition.useCheckAreaGridColumn);
		AddGeometryCard(hFlowContainer, 3, "接触区列", "接触区覆盖的格列，不限制同行", () => !_definition.checkLine && _definition.checkGrid && _definition.useCheckAreaGridColumn);
		AddGeometryCard(hFlowContainer, 4, "当前格同行", "同行 + 角色所在格窗口", () => _definition.checkLine && _definition.checkGrid && !_definition.useCheckAreaGridColumn);
		AddGeometryCard(hFlowContainer, 5, "接触区列同行", "同行 + 接触区覆盖的格列", () => _definition.checkLine && _definition.checkGrid && _definition.useCheckAreaGridColumn);
		HFlowContainer hFlowContainer2 = new HFlowContainer
		{
			Name = "AttackFilterCards"
		};
		hFlowContainer2.AddChild(CreateToggle("AttackTallToggle", "高目标优先", "checkTall"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer2.AddChild(CreateToggle("AttackVaseToggle", "允许花瓶", "checkVase"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer2.AddChild(CreateToggle("AttackBowlingToggle", "允许保龄球植物", "checkBowling"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer2.AddChild(CreateToggle("AttackGravestoneToggle", "允许墓碑", "checkGravestone"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer2.AddChild(CreateToggle("AttackAllToggle", "无需持续目标 / 全体处理", "checkAll"), forceReadableName: false, Node.InternalMode.Disabled);
		hFlowContainer2.AddChild(CreateToggle("AttackLadderToggle", "忽略梯子阻挡", "fliterLadder"), forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(hFlowContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateTimingPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("AttackTimingPanel", "攻击动画与命中帧时间轴", "播放片段决定进入攻击时播放什么；完成片段集合决定哪些动画结束后能回到待机。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("SectionBody");
		GridContainer gridContainer = new GridContainer
		{
			Columns = 2
		};
		node.AddChild(gridContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddBoundText(gridContainer, "播放片段", "AttackAnimationClip", "attackAnimeClips", "例如 Attack");
		AddBoundText(gridContainer, "命中事件", "AttackEventName", "attackEventName", "例如 attack");
		GridContainer gridContainer2 = new GridContainer
		{
			Columns = 2
		};
		node.AddChild(gridContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		AddBoundNumber(gridContainer2, "动画倍率", "AttackAnimationTimeScale", "attackAnimeTimeScale", 0.0, 20.0, 0.05);
		AddBoundNumber(gridContainer2, "动画基准间隔", "AttackIntervalBase", "attackIntervalBase", 0.0, 120.0, 0.05);
		AddBoundNumber(gridContainer2, "实际攻击间隔", "AttackInterval", "attackInterval", 0.0, 120.0, 0.05);
		AddBoundNumber(gridContainer2, "提前随机量", "AttackIntervalOffset", "attackIntervalOffset", 0.0, 120.0, 0.05);
		_timeline = new XWAttackContactTimeline
		{
			Name = "AttackHitTimeline",
			CustomMinimumSize = new Vector2(0f, 138f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		node.AddChild(_timeline, forceReadableName: false, Node.InternalMode.Disabled);
		node.AddChild(new Label
		{
			Text = "可结束攻击状态的动画片段"
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_completionRows = new VBoxContainer
		{
			Name = "AttackCompletionClipRows"
		};
		node.AddChild(_completionRows, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Name = "AttackAddCompletionClipButton",
			Text = "＋ 添加完成片段",
			CustomMinimumSize = new Vector2(180f, 38f)
		};
		button.Pressed += AddCompletionClip;
		node.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateValidationPanel()
	{
		PanelContainer panelContainer = CreateSectionPanel("AttackValidationPanel", "运行时语义检查", "旧字段 useZombieAttackCheck 当前没有运行时读取，不把它伪装成有效开关；原值仍保留在资源元数据中。");
		_validationLabel = new Label
		{
			Name = "AttackValidationLabel",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("f0bd83")
		};
		panelContainer.GetNode<VBoxContainer>("SectionBody").AddChild(_validationLabel, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private void AddAttackTypeCard(Control host, string value, string title, string detail)
	{
		AddChoiceCard(host, "type-" + value.ToLowerInvariant(), title, detail, "res://addons/ModEditor/Icons/ResourceCharacter.svg", () => string.Equals(_definition.attackType, value, StringComparison.Ordinal), delegate
		{
			SetString("attackType", value, "攻击类型改为 " + title);
		});
	}

	private void AddGeometryCard(Control host, int profile, string title, string detail, Func<bool> selected)
	{
		_targetCardCount++;
		AddChoiceCard(host, "geometry-" + profile, title, detail, "res://addons/ModEditor/Icons/Game.svg", selected, delegate
		{
			ChooseTargetGeometryProfile(profile);
		});
	}

	private void AddChoiceCard(Control host, string key, string title, string detail, string iconPath, Func<bool> selected, Action choose)
	{
		XWGameVisualChoiceCard card = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(card))
		{
			return;
		}
		card.Name = "AttackChoice_" + key;
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
		_choiceCards.Add(card);
		host.AddChild(card, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private CheckButton CreateToggle(string name, string text, string property)
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

	private void AddBoundNumber(GridContainer host, string caption, string name, string property, double minimum, double maximum, double step)
	{
		host.AddChild(new Label
		{
			Text = caption,
			CustomMinimumSize = new Vector2(152f, 36f),
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, Node.InternalMode.Disabled);
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
		host.AddChild(new Label
		{
			Text = caption,
			CustomMinimumSize = new Vector2(152f, 36f),
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

	private void SetBool(string property, bool value, string action)
	{
		_binding.SetValue(_definition, property, value, action, _refreshTarget, _refreshMethod);
	}

	private void SetString(string property, string value, string action)
	{
		_binding.SetValue(_definition, property, value, action, _refreshTarget, _refreshMethod);
	}

	public void ChooseTargetGeometryProfile(int profile)
	{
		var (checkLine, checkGrid, useCheckAreaGridColumn) = profile switch
		{
			0 => (false, false, false), 
			1 => (true, false, false), 
			2 => (false, true, false), 
			3 => (false, true, true), 
			4 => (true, true, false), 
			_ => (true, true, true), 
		};
		if (_refreshTarget is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor)
		{
			xWCharacterComponentVisualResourceEditor.SetAttackTargetGeometry(_definition, checkLine, checkGrid, useCheckAreaGridColumn);
			return;
		}
		_definition.checkLine = checkLine;
		_definition.checkGrid = checkGrid;
		_definition.useCheckAreaGridColumn = useCheckAreaGridColumn;
		RefreshPreview();
	}

	private void AddCompletionClip()
	{
		Array<string> array = ((_definition.attackAnimeClipsArray != null) ? _definition.attackAnimeClipsArray.Duplicate(deep: true) : new Array<string>());
		array.Add(string.IsNullOrWhiteSpace(_definition.attackAnimeClips) ? "Attack" : _definition.attackAnimeClips);
		SetCompletionArray(array, "添加攻击完成片段");
	}

	private void RemoveCompletionClip(int index)
	{
		if (index >= 0 && index < (_definition.attackAnimeClipsArray?.Count ?? 0))
		{
			Array<string> array = _definition.attackAnimeClipsArray.Duplicate(deep: true);
			array.RemoveAt(index);
			SetCompletionArray(array, $"移除完成片段 {index + 1}");
		}
	}

	private void SetCompletionClip(int index, string value)
	{
		if (index >= 0 && index < (_definition.attackAnimeClipsArray?.Count ?? 0) && !string.Equals(_definition.attackAnimeClipsArray[index], value, StringComparison.Ordinal))
		{
			Array<string> array = _definition.attackAnimeClipsArray.Duplicate(deep: true);
			array[index] = value ?? string.Empty;
			SetCompletionArray(array, $"修改完成片段 {index + 1}");
		}
	}

	private void SetCompletionArray(Array<string> value, string action)
	{
		_binding.SetValue(_definition, "attackAnimeClipsArray", Variant.From(in value), action, _refreshTarget, _refreshMethod);
	}

	private void RebuildCompletionRows()
	{
		if (!GodotObject.IsInstanceValid(_completionRows))
		{
			return;
		}
		ClearChildren(_completionRows);
		int num = _definition.attackAnimeClipsArray?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			int captured = i;
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"AttackCompletionRow_{i}"
			};
			LineEdit edit = new LineEdit
			{
				Name = $"AttackCompletionClip_{i}",
				Text = (_definition.attackAnimeClipsArray[i] ?? string.Empty),
				PlaceholderText = "完成后返回待机的片段名",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			edit.TextSubmitted += delegate(string text)
			{
				SetCompletionClip(captured, text);
			};
			edit.FocusExited += delegate
			{
				SetCompletionClip(captured, edit.Text);
			};
			hBoxContainer.AddChild(edit, forceReadableName: false, Node.InternalMode.Disabled);
			Button button = new Button
			{
				Name = $"AttackRemoveCompletionClip_{i}",
				Text = "移除",
				Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse)
			};
			button.Pressed += delegate
			{
				RemoveCompletionClip(captured);
			};
			hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			_completionRows.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private string BuildStructureToken()
	{
		int value = _definition.attackAnimeClipsArray?.Count ?? 0;
		int num = _definition.eventList?.Count ?? 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("clips:").Append(value).Append("|events:")
			.Append(num)
			.Append(':');
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = _definition.eventList[i];
			stringBuilder.Append(GodotObject.IsInstanceValid(towerDefenseCharacterEventBase) ? towerDefenseCharacterEventBase.GetInstanceId() : 0).Append(',');
		}
		return stringBuilder.ToString();
	}

	private string BuildValidationText()
	{
		List<string> list = new List<string>();
		if (_definition.useParentHitBox && (_definition.checkShapeResources?.Count ?? 0) > 0)
		{
			list.Add("角色碰撞模式生效：配置形状当前被宿主碰撞覆盖。");
		}
		if (_definition.useCheckAreaGridColumn && !_definition.checkGrid)
		{
			list.Add("接触区列标记已保留，但 checkGrid 关闭时运行时不会读取；选择任一空间组合可显式归一化。");
		}
		if (string.IsNullOrWhiteSpace(_definition.attackAnimeClips))
		{
			list.Add("没有播放片段：该组件只能作为外部驱动的目标查询器。");
		}
		if (string.IsNullOrWhiteSpace(_definition.attackEventName))
		{
			list.Add("没有命中事件：动画不会自动触发 OnAttack。");
		}
		if (!string.IsNullOrWhiteSpace(_definition.attackAnimeClips))
		{
			Array<string> attackAnimeClipsArray = _definition.attackAnimeClipsArray;
			if (attackAnimeClipsArray == null || !attackAnimeClipsArray.Contains(_definition.attackAnimeClips))
			{
				list.Add("播放片段“" + _definition.attackAnimeClips + "”不在完成集合中，动画结束后可能无法返回待机。");
			}
		}
		double num = _definition.attackInterval + _definition.attackIntervalBase / 3.0;
		if (!double.IsFinite(num) || num <= 0.0 || !double.IsFinite(_definition.attackAnimeTimeScale))
		{
			list.Add("攻击时间参数无效：运行时倍率分母必须大于 0，数值必须有限。");
		}
		list.Add("useZombieAttackCheck 是旧兼容字段，当前运行时没有读取；本面板不会用它暗示不存在的行为。");
		return string.Join("\n", list);
	}

	public void BindPreviewRuntime(Node owner, AttackComponent runtime)
	{
		_previewOwner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		_runtime = ((runtime != null && !runtime.IsReleased) ? runtime : null);
		_overlay?.Bind(_definition, _previewOwner, _runtime);
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
		string text = BuildStructureToken();
		if (!string.Equals(_structureToken, text, StringComparison.Ordinal))
		{
			_structureToken = text;
			RebuildCompletionRows();
		}
		else
		{
			SynchronizeCompletionRows();
		}
		foreach (XWGameVisualChoiceCard choiceCard in _choiceCards)
		{
			if (GodotObject.IsInstanceValid(choiceCard))
			{
				choiceCard.QueueRedraw();
			}
		}
		if (GodotObject.IsInstanceValid(_validationLabel))
		{
			_validationLabel.Text = BuildValidationText();
		}
		_timeline?.Bind(_definition);
		_overlay?.Bind(_definition, _previewOwner, _runtime);
	}

	private void SynchronizeCompletionRows()
	{
		if (!GodotObject.IsInstanceValid(_completionRows))
		{
			return;
		}
		for (int i = 0; i < (_definition.attackAnimeClipsArray?.Count ?? 0); i++)
		{
			LineEdit lineEdit = _completionRows.FindChild($"AttackCompletionClip_{i}", recursive: true, owned: false) as LineEdit;
			if (GodotObject.IsInstanceValid(lineEdit) && !lineEdit.HasFocus())
			{
				lineEdit.Text = _definition.attackAnimeClipsArray[i] ?? string.Empty;
			}
		}
	}

	private static PanelContainer CreateSectionPanel(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("101722"), new Color("515e70"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "SectionBody"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeColorOverride("font_color", new Color("ffb782"));
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
			_overlay.GeometryProfileRequested -= ChooseTargetGeometryProfile;
			_overlay.SetProcessInput(enable: false);
			if (_overlay.GetParent() != null)
			{
				_overlay.GetParent().RemoveChild(_overlay);
			}
			_overlay.QueueFree();
		}
		_choiceCards.Clear();
		_targetCardCount = 0;
		_completionRows = null;
		_validationLabel = null;
		_timeline = null;
		_overlay = null;
		_previewOwner = null;
		_runtime = null;
		_structureToken = string.Empty;
		Root = null;
	}
}
