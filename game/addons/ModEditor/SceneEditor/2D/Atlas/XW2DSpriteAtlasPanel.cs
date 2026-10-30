using System;
using Godot;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DSpriteAtlasPanel : PanelContainer
{
	private XW2DSceneEditor _editor;

	private Sprite2D _sprite;

	private XW2DSpriteAtlasCanvas _canvas;

	private Label _title;

	private Label _status;

	private Label _frameLabel;

	private SpinBox _columns;

	private SpinBox _rows;

	private SpinBox _regionX;

	private SpinBox _regionY;

	private SpinBox _regionWidth;

	private SpinBox _regionHeight;

	private CheckButton _regionEnabled;

	private Button _previous;

	private Button _next;

	private bool _updating;

	private bool _regionEditActive;

	private Rect2 _regionEditStart;

	public Sprite2D CurrentSprite => _sprite;

	public XW2DSpriteAtlasCanvas AtlasCanvas => _canvas;

	public string StatusText => _status?.Text ?? "";

	public override void _Ready()
	{
		BuildInterface();
		SetProcess(enable: false);
		BindSprite(null);
	}

	public void BindEditor(XW2DSceneEditor editor)
	{
		_editor = editor;
	}

	public void BindSprite(Sprite2D sprite)
	{
		_sprite = (GodotObject.IsInstanceValid(sprite) ? sprite : null);
		RefreshFromSprite();
	}

	public void RefreshFromSprite()
	{
		bool flag = GodotObject.IsInstanceValid(_sprite);
		_updating = true;
		_title.Text = (flag ? $"图集切片 · {_sprite.Name}" : "图集切片");
		if (flag)
		{
			_columns.SetValueNoSignal(Math.Max(1, _sprite.Hframes));
			_rows.SetValueNoSignal(Math.Max(1, _sprite.Vframes));
			_regionEnabled.SetPressedNoSignal(_sprite.RegionEnabled);
			Rect2 regionRect = _sprite.RegionRect;
			_regionX.SetValueNoSignal(regionRect.Position.X);
			_regionY.SetValueNoSignal(regionRect.Position.Y);
			_regionWidth.SetValueNoSignal(regionRect.Size.X);
			_regionHeight.SetValueNoSignal(regionRect.Size.Y);
		}
		_updating = false;
		Control[] array = new Control[10] { _columns, _rows, _regionEnabled, _regionX, _regionY, _regionWidth, _regionHeight, _previous, _next, _canvas };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].MouseFilter = (MouseFilterEnum)(flag ? 0 : 2);
		}
		_columns.Editable = flag;
		_rows.Editable = flag;
		_regionEnabled.Disabled = !flag;
		_previous.Disabled = !flag;
		_next.Disabled = !flag;
		UpdateRegionFieldState();
		RefreshCanvasAndLabels();
	}

	public bool SetGridWithHistory(int columns, int rows)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return false;
		}
		int num = Math.Clamp(columns, 1, 256);
		int num2 = Math.Clamp(rows, 1, 256);
		int frame = Math.Clamp(_sprite.Frame, 0, num * num2 - 1);
		return _editor?.CommitSpriteAtlasStateWithHistory(_sprite, num, num2, frame, _sprite.RegionEnabled, _sprite.RegionRect, "设置精灵图集网格") ?? false;
	}

	public bool SetFrameWithHistory(int frame)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return false;
		}
		int max = Math.Max(0, _sprite.Hframes * _sprite.Vframes - 1);
		int frame2 = Math.Clamp(frame, 0, max);
		return _editor?.CommitSpriteAtlasStateWithHistory(_sprite, _sprite.Hframes, _sprite.Vframes, frame2, _sprite.RegionEnabled, _sprite.RegionRect, "选择精灵图集帧") ?? false;
	}

	public bool SetRegionWithHistory(bool enabled, Rect2 rect)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return false;
		}
		Rect2 regionRect = SanitizeRegionRect(rect);
		return _editor?.CommitSpriteAtlasStateWithHistory(_sprite, _sprite.Hframes, _sprite.Vframes, _sprite.Frame, enabled, regionRect, "设置精灵图集区域") ?? false;
	}

	private void BuildInterface()
	{
		AddThemeStyleboxOverride("panel", MakeStyle(new Color("#0d1721"), new Color("#3b6078"), 10, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		Label label = new Label
		{
			Text = "▦",
			CustomMinimumSize = new Vector2(38f, 38f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", 23);
		label.AddThemeColorOverride("font_color", new Color("#63dcff"));
		label.AddThemeStyleboxOverride("normal", MakeStyle(new Color("#172d3d"), new Color("#3a7997"), 19, 1));
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_title = new Label
		{
			Text = "图集切片",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_title.AddThemeFontSizeOverride("font_size", 19);
		_title.AddThemeColorOverride("font_color", new Color("#f3d889"));
		hBoxContainer.AddChild(_title, forceReadableName: false, InternalMode.Disabled);
		_status = new Label
		{
			Text = "选择 Sprite2D 后可直接点击贴图选帧",
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_status.AddThemeColorOverride("font_color", new Color("#8eafc4"));
		hBoxContainer.AddChild(_status, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HSplitContainer hSplitContainer = new HSplitContainer();
		hSplitContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hSplitContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		hSplitContainer.SplitOffsets = new int[1] { 390 };
		HSplitContainer hSplitContainer2 = hSplitContainer;
		vBoxContainer.AddChild(hSplitContainer2, forceReadableName: false, InternalMode.Disabled);
		PanelContainer panelContainer = new PanelContainer
		{
			CustomMinimumSize = new Vector2(360f, 300f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer.AddThemeStyleboxOverride("panel", MakeStyle(new Color("#101d29"), new Color("#2c4d62"), 8, 1));
		_canvas = new XW2DSpriteAtlasCanvas
		{
			Name = "SpriteAtlasCanvas"
		};
		_canvas.FramePicked += OnFramePicked;
		panelContainer.AddChild(_canvas, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer2.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(330f, 0f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		hSplitContainer2.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer2.AddThemeConstantOverride("separation", 8);
		scrollContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(MakeSectionTitle("网格切片", "设置横纵帧数，然后直接点击左侧格子。"), forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		_columns = MakeSpin(1.0, 256.0, 1.0, " 列");
		_rows = MakeSpin(1.0, 256.0, 1.0, " 行");
		hBoxContainer2.AddChild(Labeled("横向", _columns), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(Labeled("纵向", _rows), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer3 = new HBoxContainer();
		(string, int, int)[] array = new(string, int, int)[4]
		{
			("单图", 1, 1),
			("横 4 帧", 4, 1),
			("4 × 4", 4, 4),
			("8 × 8", 8, 8)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, int, int) tuple = array[i];
			string item = tuple.Item1;
			int columns = tuple.Item2;
			int rows = tuple.Item3;
			Button button = MakeButton(item);
			button.Pressed += delegate
			{
				SetGridWithHistory(columns, rows);
			};
			hBoxContainer3.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer2.AddChild(hBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		_columns.ValueChanged += delegate
		{
			OnGridChanged();
		};
		_rows.ValueChanged += delegate
		{
			OnGridChanged();
		};
		vBoxContainer2.AddChild(MakeSectionTitle("当前帧", "黄色框就是运行时 Sprite2D 显示的画面。"), forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer4 = new HBoxContainer();
		_previous = MakeButton("◀ 上一帧");
		_next = MakeButton("下一帧 ▶");
		_frameLabel = new Label
		{
			Text = "帧 0 · 第 1 列 / 第 1 行",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		hBoxContainer4.AddChild(_previous, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(_frameLabel, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(_next, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(hBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		_previous.Pressed += delegate
		{
			SetFrameWithHistory((_sprite?.Frame ?? 0) - 1);
		};
		_next.Pressed += delegate
		{
			SetFrameWithHistory((_sprite?.Frame ?? 0) + 1);
		};
		vBoxContainer2.AddChild(MakeSectionTitle("局部图集区域", "只在贴图的一块矩形内继续分帧，适合合并图集。"), forceReadableName: false, InternalMode.Disabled);
		_regionEnabled = new CheckButton
		{
			Text = "启用局部区域"
		};
		_regionEnabled.Toggled += OnRegionToggled;
		vBoxContainer2.AddChild(_regionEnabled, forceReadableName: false, InternalMode.Disabled);
		GridContainer gridContainer = new GridContainer
		{
			Columns = 2
		};
		_regionX = MakeSpin(-100000.0, 100000.0, 1.0, " X");
		_regionY = MakeSpin(-100000.0, 100000.0, 1.0, " Y");
		_regionWidth = MakeSpin(0.0, 100000.0, 1.0, " 宽");
		_regionHeight = MakeSpin(0.0, 100000.0, 1.0, " 高");
		gridContainer.AddChild(Labeled("起点", _regionX), forceReadableName: false, InternalMode.Disabled);
		gridContainer.AddChild(Labeled("起点", _regionY), forceReadableName: false, InternalMode.Disabled);
		gridContainer.AddChild(Labeled("尺寸", _regionWidth), forceReadableName: false, InternalMode.Disabled);
		gridContainer.AddChild(Labeled("尺寸", _regionHeight), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(gridContainer, forceReadableName: false, InternalMode.Disabled);
		SpinBox[] array2 = new SpinBox[4] { _regionX, _regionY, _regionWidth, _regionHeight };
		foreach (SpinBox obj in array2)
		{
			obj.ValueChanged += delegate
			{
				OnRegionValueChanged();
			};
			obj.GetLineEdit().FocusEntered += BeginRegionEdit;
			obj.GetLineEdit().FocusExited += CommitRegionEdit;
		}
	}

	private void OnGridChanged()
	{
		if (!_updating)
		{
			SetGridWithHistory(Mathf.RoundToInt(_columns.Value), Mathf.RoundToInt(_rows.Value));
		}
	}

	private void OnFramePicked(int frame)
	{
		SetFrameWithHistory(frame);
	}

	private void OnRegionToggled(bool enabled)
	{
		if (!_updating && GodotObject.IsInstanceValid(_sprite))
		{
			Rect2 rect = ReadRegionControls();
			if (enabled && (rect.Size.X <= 0f || rect.Size.Y <= 0f) && GodotObject.IsInstanceValid(_sprite.Texture))
			{
				rect = new Rect2(Vector2.Zero, _sprite.Texture.GetSize());
			}
			SetRegionWithHistory(enabled, rect);
		}
	}

	private void BeginRegionEdit()
	{
		if (!_updating && !_regionEditActive && GodotObject.IsInstanceValid(_sprite))
		{
			_regionEditActive = true;
			_regionEditStart = _sprite.RegionRect;
		}
	}

	private void OnRegionValueChanged()
	{
		if (!_updating && GodotObject.IsInstanceValid(_sprite))
		{
			if (!_regionEditActive)
			{
				BeginRegionEdit();
			}
			Rect2 regionRect = SanitizeRegionRect(ReadRegionControls());
			_sprite.RegionRect = regionRect;
			RefreshCanvasAndLabels();
			_editor?.QueueViewportRedraw();
		}
	}

	private void CommitRegionEdit()
	{
		if (_regionEditActive && GodotObject.IsInstanceValid(_sprite))
		{
			_regionEditActive = false;
			Rect2 regionRect = _sprite.RegionRect;
			_sprite.RegionRect = _regionEditStart;
			SetRegionWithHistory(_sprite.RegionEnabled, regionRect);
		}
	}

	private Rect2 ReadRegionControls()
	{
		return new Rect2(new Vector2((float)_regionX.Value, (float)_regionY.Value), new Vector2((float)_regionWidth.Value, (float)_regionHeight.Value));
	}

	private Rect2 SanitizeRegionRect(Rect2 rect)
	{
		Vector2 position = rect.Position;
		Vector2 size = new Vector2(Mathf.Max(0f, rect.Size.X), Mathf.Max(0f, rect.Size.Y));
		if (GodotObject.IsInstanceValid(_sprite?.Texture))
		{
			Vector2 size2 = _sprite.Texture.GetSize();
			position.X = Mathf.Clamp(position.X, 0f, size2.X);
			position.Y = Mathf.Clamp(position.Y, 0f, size2.Y);
			size.X = Mathf.Clamp(size.X, 0f, size2.X - position.X);
			size.Y = Mathf.Clamp(size.Y, 0f, size2.Y - position.Y);
		}
		return new Rect2(position, size);
	}

	private void UpdateRegionFieldState()
	{
		bool editable = GodotObject.IsInstanceValid(_sprite) && _regionEnabled.ButtonPressed;
		_regionX.Editable = editable;
		_regionY.Editable = editable;
		_regionWidth.Editable = editable;
		_regionHeight.Editable = editable;
	}

	private void RefreshCanvasAndLabels()
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			_canvas.Bind(null, 1, 1, 0, regionEnabled: false, default(Rect2));
			_frameLabel.Text = "未选择精灵";
			_status.Text = "选择 Sprite2D 后可直接点击贴图选帧";
			return;
		}
		int num = Math.Max(1, _sprite.Hframes);
		int num2 = Math.Max(1, _sprite.Vframes);
		int num3 = Math.Clamp(_sprite.Frame, 0, num * num2 - 1);
		_canvas.Bind(_sprite.Texture, num, num2, num3, _sprite.RegionEnabled, _sprite.RegionRect);
		_frameLabel.Text = $"帧 {num3} · 第 {num3 % num + 1} 列 / 第 {num3 / num + 1} 行";
		_status.Text = ((!GodotObject.IsInstanceValid(_sprite.Texture)) ? "当前精灵还没有贴图" : (_canvas.IsGuideDensityLimited ? "网格过密：辅助线已抽样，点选精度不受影响" : $"{num * num2} 个可视帧 · 点击即写入场景"));
		_previous.Disabled = num3 <= 0;
		_next.Disabled = num3 >= num * num2 - 1;
		UpdateRegionFieldState();
	}

	private static VBoxContainer MakeSectionTitle(string title, string hint)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", new Color("#f0cf7a"));
		vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Label label2 = new Label
		{
			Text = hint,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		label2.AddThemeColorOverride("font_color", new Color("#829fb2"));
		vBoxContainer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private static Control Labeled(string text, Control control)
	{
		VBoxContainer obj = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = text
		};
		label.AddThemeFontSizeOverride("font_size", 11);
		label.AddThemeColorOverride("font_color", new Color("#8ca8bb"));
		obj.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		obj.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		return obj;
	}

	private static SpinBox MakeSpin(double minimum, double maximum, double step, string suffix)
	{
		return new SpinBox
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			Suffix = suffix,
			AllowGreater = false,
			AllowLesser = false,
			CustomMinimumSize = new Vector2(110f, 34f)
		};
	}

	private static Button MakeButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(54f, 34f);
		button.AddThemeStyleboxOverride("normal", MakeStyle(new Color("#203548"), new Color("#3e6680"), 6, 1));
		button.AddThemeStyleboxOverride("hover", MakeStyle(new Color("#2b4a60"), new Color("#65b7d5"), 6, 1));
		button.AddThemeStyleboxOverride("pressed", MakeStyle(new Color("#152633"), new Color("#f2c961"), 6, 1));
		return button;
	}

	private static StyleBoxFlat MakeStyle(Color background, Color border, int radius, int width)
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
			ContentMarginLeft = 8f,
			ContentMarginTop = 7f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 7f
		};
	}
}
