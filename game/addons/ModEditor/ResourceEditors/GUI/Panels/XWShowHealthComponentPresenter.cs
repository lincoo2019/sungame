using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWShowHealthComponentPresenter : IDisposable
{
	private const double ShieldCurrent = 312.75;

	private const double ShieldMaximum = 500.0;

	private const double HelmetCurrent = 127.4;

	private const double HelmetMaximum = 200.0;

	private const double BodyCurrent = 723.6;

	private const double BodyMaximum = 1000.0;

	private const string VisibleIconPath = "res://addons/ModEditor/Icons/GuiVisibilityVisible.svg";

	private const string HiddenIconPath = "res://addons/ModEditor/Icons/GuiVisibilityHidden.svg";

	public static readonly HashSet<string> SpecializedProperties = new HashSet<string>(StringComparer.Ordinal) { "textTemplate", "decimalPlaces", "roundHitpoints", "showBody", "showSecondaryArmor", "showHelmet", "secondaryArmorPriority", "shieldColor", "helmetColor", "bodyColor" };

	private readonly ShowHealthComponentDefinition _definition;

	private readonly XWVisualPropertyBinding _binding;

	private readonly GodotObject _refreshTarget;

	private readonly StringName _refreshMethod;

	private readonly List<Button> _decimalButtons = new List<Button>();

	private readonly List<Button> _roundButtons = new List<Button>();

	private readonly List<Button> _priorityButtons = new List<Button>();

	private Label _shieldPreview;

	private Label _helmetPreview;

	private Label _bodyPreview;

	private Button _secondaryVisibilityButton;

	private LineEdit _templateEdit;

	public PanelContainer Root { get; private set; }

	public int PreviewRevision { get; private set; }

	public bool IsProcessIdle
	{
		get
		{
			if (GodotObject.IsInstanceValid(Root) && !Root.IsProcessing())
			{
				return !Root.IsPhysicsProcessing();
			}
			return false;
		}
	}

	public XWShowHealthComponentPresenter(ShowHealthComponentDefinition definition, XWVisualPropertyBinding binding, GodotObject refreshTarget, StringName refreshMethod)
	{
		_definition = definition;
		_binding = binding;
		_refreshTarget = refreshTarget;
		_refreshMethod = refreshMethod;
	}

	public void Mount(VBoxContainer host)
	{
		if (GodotObject.IsInstanceValid(host) && GodotObject.IsInstanceValid(_definition) && _binding != null)
		{
			Root = new PanelContainer
			{
				Name = "ShowHealthHudPresenter",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			Root.SetProcess(enable: false);
			Root.SetPhysicsProcess(enable: false);
			Root.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("111b2d"), new Color("386a9d"), 14, 2));
			host.AddChild(Root, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_top", 14);
			marginContainer.AddThemeConstantOverride("margin_right", 16);
			marginContainer.AddThemeConstantOverride("margin_bottom", 16);
			Root.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "ShowHealthHudBody"
			};
			vBoxContainer.AddThemeConstantOverride("separation", 12);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHeader(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateHudStage(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreateFormatPuzzle(), forceReadableName: false, Node.InternalMode.Disabled);
			vBoxContainer.AddChild(CreatePriorityPuzzle(), forceReadableName: false, Node.InternalMode.Disabled);
			RefreshPreview();
		}
	}

	private Control CreateHeader()
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		TextureRect node = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Game.svg", null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(30f, 30f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		hBoxContainer.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = "角色头顶生命 HUD"
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color("e7f4ff"));
		vBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "直接在护盾、头盔和本体生命文字所在位置编辑；所有变化实时使用游戏格式规则预览。",
			Modulate = new Color("8faac5"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private Control CreateHudStage()
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = "ShowHealthHudStage";
		panelContainer.CustomMinimumSize = new Vector2(0f, 270f);
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("09111d"), new Color("24486d"), 12, 1));
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 14);
		marginContainer.AddThemeConstantOverride("margin_top", 14);
		marginContainer.AddThemeConstantOverride("margin_right", 14);
		marginContainer.AddThemeConstantOverride("margin_bottom", 14);
		panelContainer.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "游戏内位置预览  ·  示例生命值",
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color("7fa6c9")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_shieldPreview = CreateHudRow(vBoxContainer, "ShowHealthShieldHudRow", "副甲", "showSecondaryArmor", "shieldColor", out var row, out var colorPicker);
		_helmetPreview = CreateHudRow(vBoxContainer, "ShowHealthHelmetHudRow", "头盔", "showHelmet", "helmetColor", out row, out colorPicker);
		_bodyPreview = CreateHudRow(vBoxContainer, "ShowHealthBodyHudRow", "本体", "showBody", "bodyColor", out var row2, out var colorPicker2);
		_templateEdit = new LineEdit
		{
			Name = "ShowHealthTextTemplateEdit",
			PlaceholderText = "例如：HP:{0}/{1}",
			TooltipText = "{0} 为当前生命，{1} 为生命上限",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(220f, 36f)
		};
		int index = _bodyPreview.GetIndex();
		row2.MoveChild(_bodyPreview, index);
		row2.AddChild(_templateEdit, forceReadableName: false, Node.InternalMode.Disabled);
		row2.MoveChild(_templateEdit, index);
		_binding.BindText(_templateEdit, _definition, "textTemplate", RefreshPreview, _refreshTarget, _refreshMethod);
		colorPicker2.TooltipText = "本体生命文字颜色";
		PanelContainer panelContainer2 = new PanelContainer
		{
			Name = "ShowHealthCharacterPlate",
			CustomMinimumSize = new Vector2(0f, 62f)
		};
		panelContainer2.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("152943"), new Color("315d88"), 10, 1));
		panelContainer2.AddChild(new Label
		{
			Text = "◆  游戏角色  ◆",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color("90badc")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(panelContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Label CreateHudRow(VBoxContainer host, string rowName, string caption, string visibilityProperty, string colorProperty, out HBoxContainer row, out ColorPickerButton colorPicker)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			Name = rowName
		};
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("101c2d"), new Color("29445f"), 8, 1));
		row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		panelContainer.AddChild(row, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = CreateVisibilityToggle(caption, visibilityProperty);
		row.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		Label label = new Label
		{
			Name = rowName + "PreviewText",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(180f, 38f)
		};
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeFontSizeOverride("font_size", 17);
		row.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		colorPicker = CreateColorPicker(colorProperty, caption + "文字颜色");
		row.AddChild(colorPicker, forceReadableName: false, Node.InternalMode.Disabled);
		host.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
		if (visibilityProperty == "showSecondaryArmor")
		{
			_secondaryVisibilityButton = button;
		}
		return label;
	}

	private Button CreateVisibilityToggle(string caption, string property)
	{
		Button button = new Button
		{
			Name = "ShowHealthVisibility_" + property,
			Text = caption,
			ToggleMode = true,
			CustomMinimumSize = new Vector2(108f, 38f),
			TooltipText = "点击切换该生命文字是否在游戏中显示"
		};
		_binding.BindToggle(button, _definition, property, RefreshPreview, _refreshTarget, _refreshMethod);
		_binding.RegisterControlSynchronizer(Synchronize);
		Synchronize();
		return button;
		void Synchronize()
		{
			if (GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(_definition))
			{
				bool flag = _definition.Get(property).AsBool();
				button.SetPressedNoSignal(flag);
				button.Icon = ResourceLoader.Load<Texture2D>(flag ? "res://addons/ModEditor/Icons/GuiVisibilityVisible.svg" : "res://addons/ModEditor/Icons/GuiVisibilityHidden.svg", null, ResourceLoader.CacheMode.Reuse);
				button.Modulate = (flag ? Colors.White : new Color("77889b"));
			}
		}
	}

	private ColorPickerButton CreateColorPicker(string property, string tooltip)
	{
		ColorPickerButton picker = new ColorPickerButton
		{
			Name = "ShowHealthColor_" + property,
			CustomMinimumSize = new Vector2(52f, 38f),
			TooltipText = tooltip
		};
		picker.Color = _definition.Get(property).AsColor();
		picker.Pressed += delegate
		{
			_binding.BeginEdit(_definition, property);
		};
		picker.ColorChanged += delegate(Color color)
		{
			_binding.PreviewValue(_definition, property, color);
			RefreshPreview();
		};
		picker.PopupClosed += delegate
		{
			_binding.CommitEdit(_definition, property, _definition.Get(property), "修改" + tooltip, _refreshTarget, _refreshMethod);
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(delegate
		{
			if (GodotObject.IsInstanceValid(picker) && GodotObject.IsInstanceValid(_definition))
			{
				picker.Color = _definition.Get(property).AsColor();
			}
		});
		return picker;
	}

	private Control CreateFormatPuzzle()
	{
		PanelContainer panelContainer = CreatePuzzlePanel("ShowHealthFormatPuzzle", "数字显示拼图", "选择整数或小数，再拼上保留位数。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("PuzzleBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ShowHealthRoundModeCards"
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_roundButtons.Add(CreateChoiceButton(hFlowContainer, "ShowHealthRoundInteger", "● 整数生命", () => _definition.roundHitpoints, delegate
		{
			SetBool("roundHitpoints", value: true, "切换为整数生命");
		}));
		_roundButtons.Add(CreateChoiceButton(hFlowContainer, "ShowHealthRoundDecimal", "◌ 小数生命", () => !_definition.roundHitpoints, delegate
		{
			SetBool("roundHitpoints", value: false, "切换为小数生命");
		}));
		HFlowContainer hFlowContainer2 = new HFlowContainer
		{
			Name = "ShowHealthDecimalPlaceCards"
		};
		hFlowContainer2.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer2.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		string[] array = new string[4] { "0 位", ".0", ".00", ".000" };
		for (int num = 0; num < array.Length; num++)
		{
			int captured = num;
			_decimalButtons.Add(CreateChoiceButton(hFlowContainer2, "ShowHealthDecimal_" + num, array[num], () => _definition.decimalPlaces == captured, delegate
			{
				SetInt("decimalPlaces", captured, $"保留 {captured} 位小数");
			}));
		}
		return panelContainer;
	}

	private Control CreatePriorityPuzzle()
	{
		PanelContainer panelContainer = CreatePuzzlePanel("ShowHealthPriorityPuzzle", "副甲优先级拼图", "角色同时有护盾和头部护具时，决定上方副甲文字读取哪一组生命。");
		VBoxContainer node = panelContainer.GetNode<VBoxContainer>("PuzzleBody");
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ShowHealthPriorityCards"
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 8);
		hFlowContainer.AddThemeConstantOverride("v_separation", 8);
		node.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_priorityButtons.Add(CreateChoiceButton(hFlowContainer, "ShowHealthPriorityShieldFirst", "▣ 护盾 → 头部护具", () => _definition.secondaryArmorPriority == ShowHealthComponent.SecondaryArmorPriority.ShieldFirst, delegate
		{
			SetPriority(ShowHealthComponent.SecondaryArmorPriority.ShieldFirst);
		}));
		_priorityButtons.Add(CreateChoiceButton(hFlowContainer, "ShowHealthPriorityHeadCoverFirst", "▰ 头部护具 → 护盾", () => _definition.secondaryArmorPriority == ShowHealthComponent.SecondaryArmorPriority.HeadCoverFirst, delegate
		{
			SetPriority(ShowHealthComponent.SecondaryArmorPriority.HeadCoverFirst);
		}));
		return panelContainer;
	}

	private static PanelContainer CreatePuzzlePanel(string name, string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = name;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("0d1726"), new Color("2d4b69"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "PuzzleBody"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		vBoxContainer.AddChild(new Label
		{
			Text = title,
			Modulate = new Color("8fd4ff")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			Modulate = new Color("8299b1"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private Button CreateChoiceButton(Control host, string name, string text, Func<bool> selected, Action choose)
	{
		Button button = new Button
		{
			Name = name,
			Text = text,
			ToggleMode = true,
			CustomMinimumSize = new Vector2(150f, 40f)
		};
		button.Pressed += delegate
		{
			choose();
			RefreshPreview();
		};
		_binding.RegisterControlSynchronizer(Synchronize);
		Synchronize();
		host.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		return button;
		void Synchronize()
		{
			if (GodotObject.IsInstanceValid(button))
			{
				bool flag = selected();
				button.SetPressedNoSignal(flag);
				button.Modulate = (flag ? new Color("a9e6ff") : new Color("8293a6"));
			}
		}
	}

	private void SetBool(string property, bool value, string action)
	{
		_binding.SetValue(_definition, property, value, action, _refreshTarget, _refreshMethod);
	}

	private void SetInt(string property, int value, string action)
	{
		_binding.SetValue(_definition, property, (long)value, action, _refreshTarget, _refreshMethod);
	}

	private void SetPriority(ShowHealthComponent.SecondaryArmorPriority value)
	{
		_binding.SetValue(_definition, "secondaryArmorPriority", (long)value, (value == ShowHealthComponent.SecondaryArmorPriority.ShieldFirst) ? "副甲改为护盾优先" : "副甲改为头部护具优先", _refreshTarget, _refreshMethod);
	}

	public void RefreshPreview()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			PreviewRevision++;
			UpdatePreviewLabel(_shieldPreview, _definition.showSecondaryArmor, Format(312.75, 500.0), _definition.shieldColor);
			UpdatePreviewLabel(_helmetPreview, _definition.showHelmet, Format(127.4, 200.0), _definition.helmetColor);
			UpdatePreviewLabel(_bodyPreview, _definition.showBody, Format(723.6, 1000.0), _definition.bodyColor);
			if (GodotObject.IsInstanceValid(_secondaryVisibilityButton))
			{
				_secondaryVisibilityButton.Text = ((_definition.secondaryArmorPriority == ShowHealthComponent.SecondaryArmorPriority.ShieldFirst) ? "副甲·护盾优先" : "副甲·头具优先");
			}
		}
		string Format(double current, double maximum)
		{
			return ShowHealthComponent.FormatHealthForDisplay(current, maximum, _definition.textTemplate, _definition.decimalPlaces, _definition.roundHitpoints);
		}
	}

	private static void UpdatePreviewLabel(Label label, bool visible, string text, Color color)
	{
		if (GodotObject.IsInstanceValid(label))
		{
			label.Visible = visible;
			label.Text = text;
			label.AddThemeColorOverride("font_color", color);
		}
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
			CornerRadiusBottomRight = radius,
			CornerRadiusBottomLeft = radius,
			ContentMarginLeft = 10f,
			ContentMarginTop = 8f,
			ContentMarginRight = 10f,
			ContentMarginBottom = 8f
		};
	}

	public void Dispose()
	{
		_decimalButtons.Clear();
		_roundButtons.Clear();
		_priorityButtons.Clear();
		_shieldPreview = null;
		_helmetPreview = null;
		_bodyPreview = null;
		_secondaryVisibilityButton = null;
		_templateEdit = null;
		Root = null;
	}
}
