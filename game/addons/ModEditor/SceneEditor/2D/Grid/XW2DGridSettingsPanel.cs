using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DGridSettingsPanel : PanelContainer
{
	private readonly struct GridPreset(string name, string description, Vector2 gridStep, Vector2I primaryStep, Vector2 moveStep, Color accent)
	{
		public readonly string Name = name;

		public readonly string Description = description;

		public readonly Vector2 GridStep = gridStep;

		public readonly Vector2I PrimaryStep = primaryStep;

		public readonly Vector2 MoveStep = moveStep;

		public readonly Color Accent = accent;
	}

	private sealed partial class GridPresetTile : Button
	{
		public string PresetName = "";

		public string Description = "";

		public Color Accent = Colors.White;

		public int GridDivisions = 4;

		public override void _Ready()
		{
			base.Text = "";
			base.Flat = true;
			base.MouseDefaultCursorShape = CursorShape.PointingHand;
		}

		public override void _Draw()
		{
			Color color = (base.ButtonPressed ? new Color("274838") : new Color("162936"));
			Color color2 = (base.ButtonPressed ? Accent : new Color("395669"));
			DrawRect(new Rect2(Vector2.Zero, base.Size), color);
			DrawRect(new Rect2(Vector2.One, base.Size - Vector2.One * 2f), color2, filled: false, base.ButtonPressed ? 2.5f : 1f);
			Rect2 rect = new Rect2(9f, 12f, 48f, 48f);
			DrawRect(rect, new Color("0d1820"));
			int num = Math.Clamp(GridDivisions, 2, 8);
			for (int i = 0; i <= num; i++)
			{
				float weight = (float)i / (float)num;
				float x = Mathf.Lerp(rect.Position.X, rect.End.X, weight);
				float y = Mathf.Lerp(rect.Position.Y, rect.End.Y, weight);
				Color color3;
				if (i != 0 && i != num)
				{
					Color accent = Accent;
					accent.A = 0.35f;
					color3 = accent;
				}
				else
				{
					color3 = Accent;
				}
				Color color4 = color3;
				DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), color4, (i == 0 || i == num) ? 1.4f : 1f);
				DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), color4, (i == 0 || i == num) ? 1.4f : 1f);
			}
			DrawCircle(rect.GetCenter(), 3.5f, Accent);
			Font themeDefaultFont = GetThemeDefaultFont();
			DrawString(themeDefaultFont, new Vector2(66f, 33f), PresetName, HorizontalAlignment.Left, Math.Max(1f, base.Size.X - 72f), 13, base.ButtonPressed ? Accent.Lightened(0.2f) : new Color("e5edf0"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(themeDefaultFont, new Vector2(66f, 54f), Description, HorizontalAlignment.Left, Math.Max(1f, base.Size.X - 72f), 11, new Color("91abba"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(themeDefaultFont, new Vector2(10f, 78f), base.ButtonPressed ? "✓ 已选" : "点击套用", HorizontalAlignment.Left, Math.Max(1f, base.Size.X - 20f), 10, base.ButtonPressed ? Accent : new Color("718c9b"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	private sealed partial class GridPreviewCanvas : Control
	{
		public XW2DGridSettingsPanel OwnerPanel;

		public override void _Draw()
		{
			DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("09131a"));
			if (GodotObject.IsInstanceValid(OwnerPanel))
			{
				float num = Math.Clamp(OwnerPanel._gridStep.X * 1.7f, 12f, 38f);
				float num2 = Math.Clamp(OwnerPanel._gridStep.Y * 1.7f, 12f, 38f);
				float num3 = Mathf.PosMod(OwnerPanel._gridOffset.X * 0.35f, num);
				float num4 = Mathf.PosMod(OwnerPanel._gridOffset.Y * 0.35f, num2);
				int num5 = 0;
				float num6 = num3;
				while (num6 <= base.Size.X)
				{
					bool flag = num5 % Math.Max(1, OwnerPanel._primaryGridStep.X) == 0;
					DrawLine(new Vector2(num6, 0f), new Vector2(num6, base.Size.Y), flag ? new Color("4f7666aa") : new Color("28443c88"), flag ? 1.5f : 1f);
					num6 += num;
					num5++;
				}
				int num7 = 0;
				float num8 = num4;
				while (num8 <= base.Size.Y)
				{
					bool flag2 = num7 % Math.Max(1, OwnerPanel._primaryGridStep.Y) == 0;
					DrawLine(new Vector2(0f, num8), new Vector2(base.Size.X, num8), flag2 ? new Color("4f7666aa") : new Color("28443c88"), flag2 ? 1.5f : 1f);
					num8 += num2;
					num7++;
				}
				Vector2 vector = base.Size * 0.5f;
				float radius = Math.Clamp(OwnerPanel._smartSnapThreshold * 1.35f, 8f, 64f);
				DrawCircle(vector, radius, new Color("ffd46a18"));
				DrawArc(vector, radius, 0f, (float)Math.PI * 2f, 64, new Color("ffd46acc"), 2f, antialiased: true);
				DrawLine(new Vector2(vector.X, 0f), new Vector2(vector.X, base.Size.Y), new Color("ff7373aa"), 1.5f);
				DrawLine(new Vector2(0f, vector.Y), new Vector2(base.Size.X, vector.Y), new Color("76d88baa"), 1.5f);
				DrawCircle(vector, 5f, new Color("ffe088"));
				Font themeDefaultFont = GetThemeDefaultFont();
				DrawString(themeDefaultFont, new Vector2(12f, 24f), "实时网格预览", HorizontalAlignment.Left, 150f, 13, new Color("d8ebdd"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				DrawString(themeDefaultFont, new Vector2(12f, base.Size.Y - 12f), $"◎ 智能吸附感应半径 {OwnerPanel._smartSnapThreshold:0.#} px", HorizontalAlignment.Left, Math.Max(1f, base.Size.X - 24f), 11, new Color("f4d47d"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
				DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("436a78"), filled: false, 1f);
			}
		}
	}

	private const float MinimumGridStep = 0.25f;

	private const float MaximumGridStep = 1024f;

	private const float MinimumSmartThreshold = 1f;

	private const float MaximumSmartThreshold = 64f;

	private static readonly GridPreset[] Presets = new GridPreset[4]
	{
		new GridPreset("精细像素", "4 × 4", new Vector2(4f, 4f), new Vector2I(8, 8), new Vector2(4f, 4f), new Color("79d7ff")),
		new GridPreset("经典像素", "8 × 8", new Vector2(8f, 8f), new Vector2I(8, 8), new Vector2(8f, 8f), new Color("8be58d")),
		new GridPreset("标准场景", "16 × 16", new Vector2(16f, 16f), new Vector2I(4, 4), new Vector2(16f, 16f), new Color("ffd36f")),
		new GridPreset("大地图", "32 × 32", new Vector2(32f, 32f), new Vector2I(4, 4), new Vector2(32f, 32f), new Color("ff9d73"))
	};

	private Vector2 _gridOffset = Vector2.Zero;

	private Vector2 _gridStep = new Vector2(8f, 8f);

	private Vector2I _primaryGridStep = new Vector2I(8, 8);

	private float _smartSnapThreshold = 8f;

	private Vector2 _moveSnapStep = new Vector2(8f, 8f);

	private float _rotateSnapStep = 15f;

	private float _scaleSnapStep = 0.1f;

	private GridPreviewCanvas _preview;

	private SpinBox _offsetX;

	private SpinBox _offsetY;

	private SpinBox _gridX;

	private SpinBox _gridY;

	private SpinBox _primaryX;

	private SpinBox _primaryY;

	private SpinBox _moveX;

	private SpinBox _moveY;

	private SpinBox _rotateStep;

	private SpinBox _scaleStep;

	private HSlider _thresholdSlider;

	private SpinBox _thresholdSpin;

	private Label _summaryLabel;

	private readonly List<GridPresetTile> _presetTiles = new List<GridPresetTile>();

	private bool _updatingControls;

	public Vector2 GridOffset
	{
		get
		{
			return _gridOffset;
		}
		set
		{
			SetGridOffset(value, notify: true);
		}
	}

	public Vector2 GridStep
	{
		get
		{
			return _gridStep;
		}
		set
		{
			SetGridStep(value, notify: true);
		}
	}

	public Vector2I PrimaryGridStep
	{
		get
		{
			return _primaryGridStep;
		}
		set
		{
			SetPrimaryGridStep(value, notify: true);
		}
	}

	public float SmartSnapThreshold
	{
		get
		{
			return _smartSnapThreshold;
		}
		set
		{
			SetSmartSnapThreshold(value, notify: true);
		}
	}

	public Vector2 MoveSnapStep
	{
		get
		{
			return _moveSnapStep;
		}
		set
		{
			SetMoveSnapStep(value, notify: true);
		}
	}

	public float RotateSnapStep
	{
		get
		{
			return _rotateSnapStep;
		}
		set
		{
			SetRotateSnapStep(value, notify: true);
		}
	}

	public float ScaleSnapStep
	{
		get
		{
			return _scaleSnapStep;
		}
		set
		{
			SetScaleSnapStep(value, notify: true);
		}
	}

	public event Action SettingsChanged;

	public override void _Ready()
	{
		BuildInterface();
		SyncControls();
	}

	public void BindState(Dictionary state)
	{
		if (state == null)
		{
			SyncControls();
			return;
		}
		if (state.ContainsKey("grid_offset"))
		{
			_gridOffset = ClampOffset(state["grid_offset"].AsVector2());
		}
		if (state.ContainsKey("grid_step"))
		{
			_gridStep = ClampPositiveVector(state["grid_step"].AsVector2(), 0.25f, 1024f);
		}
		if (state.ContainsKey("primary_grid_step"))
		{
			_primaryGridStep = ClampPrimaryStep(state["primary_grid_step"].AsVector2I());
		}
		if (state.ContainsKey("smart_snap_threshold"))
		{
			_smartSnapThreshold = ClampSmartThreshold(state["smart_snap_threshold"].AsSingle());
		}
		if (state.ContainsKey("move_snap_step"))
		{
			_moveSnapStep = ClampPositiveVector(state["move_snap_step"].AsVector2(), 0.25f, 1024f);
		}
		if (state.ContainsKey("rotate_snap_step"))
		{
			_rotateSnapStep = Math.Clamp(state["rotate_snap_step"].AsSingle(), 0.1f, 180f);
		}
		if (state.ContainsKey("scale_snap_step"))
		{
			_scaleSnapStep = Math.Clamp(state["scale_snap_step"].AsSingle(), 0.001f, 10f);
		}
		SyncControls();
	}

	public Dictionary CaptureState()
	{
		return new Dictionary
		{
			["grid_offset"] = _gridOffset,
			["grid_step"] = _gridStep,
			["primary_grid_step"] = _primaryGridStep,
			["smart_snap_threshold"] = _smartSnapThreshold,
			["move_snap_step"] = _moveSnapStep,
			["rotate_snap_step"] = _rotateSnapStep,
			["scale_snap_step"] = _scaleSnapStep
		};
	}

	public void ResetSettings()
	{
		_gridOffset = Vector2.Zero;
		_gridStep = new Vector2(8f, 8f);
		_primaryGridStep = new Vector2I(8, 8);
		_smartSnapThreshold = 8f;
		_moveSnapStep = new Vector2(8f, 8f);
		_rotateSnapStep = 15f;
		_scaleSnapStep = 0.1f;
		SyncControls();
		NotifySettingsChanged();
	}

	private void BuildInterface()
	{
		if (GodotObject.IsInstanceValid(_preview))
		{
			return;
		}
		base.CustomMinimumSize = new Vector2(520f, 610f);
		AddThemeStyleboxOverride("panel", MakePanelStyle(new Color("101b24"), new Color("41657c"), 12, 1));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "GridSettingsSurface",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 9);
		AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		Label node = MakeBadge("▦", new Color("7ee29d"));
		hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label node2 = MakeLabel("网格与吸附", 20, new Color("f5dd91"));
		Label node3 = MakeLabel("用图块选择常用网格，再直接微调坐标和手感", 12, new Color("91afbf"));
		vBoxContainer2.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		Button button = MakeButton("↺ 恢复默认", "恢复 8 像素网格、15° 旋转与 10% 缩放步长");
		button.Pressed += ResetSettings;
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_preview = new GridPreviewCanvas
		{
			Name = "GridPreview",
			OwnerPanel = this,
			CustomMinimumSize = new Vector2(0f, 150f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		vBoxContainer.AddChild(_preview, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer3 = MakeSection(vBoxContainer, "网格图块", "点击图块立即配置次网格、主网格与移动吸附");
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		hBoxContainer2.AddThemeConstantOverride("separation", 7);
		for (int i = 0; i < Presets.Length; i++)
		{
			int captured = i;
			GridPreset gridPreset = Presets[i];
			GridPresetTile gridPresetTile = new GridPresetTile
			{
				Name = $"Preset{i}",
				PresetName = gridPreset.Name,
				Description = gridPreset.Description,
				Accent = gridPreset.Accent,
				GridDivisions = Math.Clamp(36 / Math.Max(1, Mathf.RoundToInt(gridPreset.GridStep.X)), 2, 7),
				ToggleMode = true,
				CustomMinimumSize = new Vector2(128f, 88f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				TooltipText = gridPreset.Name + "：" + gridPreset.Description + " 像素"
			};
			gridPresetTile.Pressed += delegate
			{
				ApplyPreset(captured);
			};
			_presetTiles.Add(gridPresetTile);
			hBoxContainer2.AddChild(gridPresetTile, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer3.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		HSplitContainer hSplitContainer = new HSplitContainer();
		hSplitContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hSplitContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		hSplitContainer.SplitOffsets = new int[1] { 260 };
		HSplitContainer hSplitContainer2 = hSplitContainer;
		vBoxContainer.AddChild(hSplitContainer2, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer4 = MakeStandaloneSection("网格坐标", "图中十字代表原点偏移；粗线间隔由主网格控制");
		hSplitContainer2.AddChild(vBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		_offsetX = MakeSpin(-100000.0, 100000.0, 1.0, " px");
		_offsetY = MakeSpin(-100000.0, 100000.0, 1.0, " px");
		_gridX = MakeSpin(0.25, 1024.0, 0.25, " px");
		_gridY = MakeSpin(0.25, 1024.0, 0.25, " px");
		_primaryX = MakeSpin(1.0, 64.0, 1.0, " 格");
		_primaryY = MakeSpin(1.0, 64.0, 1.0, " 格");
		vBoxContainer4.AddChild(MakeVectorRow("⌖", "原点偏移", _offsetX, _offsetY, new Color("79c9ff")), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer4.AddChild(MakeVectorRow("▦", "次网格间距", _gridX, _gridY, new Color("87e0a2")), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer4.AddChild(MakeVectorRow("▣", "主网格倍率", _primaryX, _primaryY, new Color("f5cf71")), forceReadableName: false, InternalMode.Disabled);
		_offsetX.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				GridOffset = new Vector2((float)value, _gridOffset.Y);
			}
		};
		_offsetY.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				GridOffset = new Vector2(_gridOffset.X, (float)value);
			}
		};
		_gridX.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				GridStep = new Vector2((float)value, _gridStep.Y);
			}
		};
		_gridY.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				GridStep = new Vector2(_gridStep.X, (float)value);
			}
		};
		_primaryX.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				PrimaryGridStep = new Vector2I((int)Math.Round(value), _primaryGridStep.Y);
			}
		};
		_primaryY.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				PrimaryGridStep = new Vector2I(_primaryGridStep.X, (int)Math.Round(value));
			}
		};
		VBoxContainer vBoxContainer5 = MakeStandaloneSection("吸附手感", "拖动滑杆或数字；单位图标会同步显示实际效果");
		hSplitContainer2.AddChild(vBoxContainer5, forceReadableName: false, InternalMode.Disabled);
		_moveX = MakeSpin(0.25, 1024.0, 0.25, " px");
		_moveY = MakeSpin(0.25, 1024.0, 0.25, " px");
		_rotateStep = MakeSpin(0.1, 180.0, 0.1, "°");
		_scaleStep = MakeSpin(0.001, 10.0, 0.01, " ×");
		vBoxContainer5.AddChild(MakeVectorRow("↔", "移动步长", _moveX, _moveY, new Color("76d5ff")), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer5.AddChild(MakeScalarRow("◔", "旋转步长", _rotateStep, new Color("ffb66f")), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer5.AddChild(MakeScalarRow("◇", "缩放步长", _scaleStep, new Color("c99cff")), forceReadableName: false, InternalMode.Disabled);
		_moveX.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				MoveSnapStep = new Vector2((float)value, _moveSnapStep.Y);
			}
		};
		_moveY.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				MoveSnapStep = new Vector2(_moveSnapStep.X, (float)value);
			}
		};
		_rotateStep.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				RotateSnapStep = (float)value;
			}
		};
		_scaleStep.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				ScaleSnapStep = (float)value;
			}
		};
		VBoxContainer vBoxContainer6 = MakeSection(vBoxContainer, "智能吸附感应圈", "黄色感应圈越大，节点边缘与中心线越容易被捕获");
		HBoxContainer hBoxContainer3 = new HBoxContainer();
		hBoxContainer3.AddChild(MakeBadge("◎", new Color("ffd86f")), forceReadableName: false, InternalMode.Disabled);
		_thresholdSlider = new HSlider
		{
			MinValue = 1.0,
			MaxValue = 64.0,
			Step = 1.0,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(220f, 34f),
			TooltipText = "屏幕像素感应半径"
		};
		_thresholdSpin = MakeSpin(1.0, 64.0, 1.0, " px");
		_thresholdSpin.CustomMinimumSize = new Vector2(108f, 34f);
		hBoxContainer3.AddChild(_thresholdSlider, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer3.AddChild(_thresholdSpin, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer6.AddChild(hBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		_thresholdSlider.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				SmartSnapThreshold = (float)value;
			}
		};
		_thresholdSpin.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				SmartSnapThreshold = (float)value;
			}
		};
		_summaryLabel = MakeLabel("", 12, new Color("a6d5e8"));
		_summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vBoxContainer.AddChild(_summaryLabel, forceReadableName: false, InternalMode.Disabled);
	}

	private void ApplyPreset(int index)
	{
		if (index >= 0 && index < Presets.Length)
		{
			GridPreset gridPreset = Presets[index];
			_gridStep = gridPreset.GridStep;
			_primaryGridStep = gridPreset.PrimaryStep;
			_moveSnapStep = gridPreset.MoveStep;
			SyncControls();
			NotifySettingsChanged();
		}
	}

	private void SetGridOffset(Vector2 value, bool notify)
	{
		Vector2 vector = ClampOffset(value);
		if (!_gridOffset.IsEqualApprox(vector))
		{
			_gridOffset = vector;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetGridStep(Vector2 value, bool notify)
	{
		Vector2 vector = ClampPositiveVector(value, 0.25f, 1024f);
		if (!_gridStep.IsEqualApprox(vector))
		{
			_gridStep = vector;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetPrimaryGridStep(Vector2I value, bool notify)
	{
		Vector2I vector2I = ClampPrimaryStep(value);
		if (!(_primaryGridStep == vector2I))
		{
			_primaryGridStep = vector2I;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetSmartSnapThreshold(float value, bool notify)
	{
		float num = ClampSmartThreshold(value);
		if (!Mathf.IsEqualApprox(_smartSnapThreshold, num))
		{
			_smartSnapThreshold = num;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetMoveSnapStep(Vector2 value, bool notify)
	{
		Vector2 vector = ClampPositiveVector(value, 0.25f, 1024f);
		if (!_moveSnapStep.IsEqualApprox(vector))
		{
			_moveSnapStep = vector;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetRotateSnapStep(float value, bool notify)
	{
		float num = Math.Clamp(value, 0.1f, 180f);
		if (!Mathf.IsEqualApprox(_rotateSnapStep, num))
		{
			_rotateSnapStep = num;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SetScaleSnapStep(float value, bool notify)
	{
		float num = Math.Clamp(value, 0.001f, 10f);
		if (!Mathf.IsEqualApprox(_scaleSnapStep, num))
		{
			_scaleSnapStep = num;
			SyncControls();
			if (notify)
			{
				NotifySettingsChanged();
			}
		}
	}

	private void SyncControls()
	{
		_updatingControls = true;
		SetSpinValue(_offsetX, _gridOffset.X);
		SetSpinValue(_offsetY, _gridOffset.Y);
		SetSpinValue(_gridX, _gridStep.X);
		SetSpinValue(_gridY, _gridStep.Y);
		SetSpinValue(_primaryX, _primaryGridStep.X);
		SetSpinValue(_primaryY, _primaryGridStep.Y);
		SetSpinValue(_moveX, _moveSnapStep.X);
		SetSpinValue(_moveY, _moveSnapStep.Y);
		SetSpinValue(_rotateStep, _rotateSnapStep);
		SetSpinValue(_scaleStep, _scaleSnapStep);
		if (GodotObject.IsInstanceValid(_thresholdSlider))
		{
			_thresholdSlider.Value = _smartSnapThreshold;
		}
		SetSpinValue(_thresholdSpin, _smartSnapThreshold);
		_updatingControls = false;
		UpdatePresetSelection();
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = $"▦ {_gridStep.X:0.##} × {_gridStep.Y:0.##} px  ·  ↔ {_moveSnapStep.X:0.##} × {_moveSnapStep.Y:0.##} px  ·  ◔ {_rotateSnapStep:0.##}°  ·  ◇ {_scaleSnapStep * 100f:0.##}%  ·  ◎ {_smartSnapThreshold:0.#} px";
		}
		_preview?.QueueRedraw();
	}

	private void UpdatePresetSelection()
	{
		for (int i = 0; i < _presetTiles.Count; i++)
		{
			GridPreset gridPreset = Presets[i];
			bool pressedNoSignal = _gridStep.IsEqualApprox(gridPreset.GridStep) && _primaryGridStep == gridPreset.PrimaryStep && _moveSnapStep.IsEqualApprox(gridPreset.MoveStep);
			_presetTiles[i].SetPressedNoSignal(pressedNoSignal);
			_presetTiles[i].QueueRedraw();
		}
	}

	private void NotifySettingsChanged()
	{
		_preview?.QueueRedraw();
		this.SettingsChanged?.Invoke();
	}

	private static Vector2 ClampOffset(Vector2 value)
	{
		return new Vector2(Math.Clamp(value.X, -100000f, 100000f), Math.Clamp(value.Y, -100000f, 100000f));
	}

	private static Vector2 ClampPositiveVector(Vector2 value, float minimum, float maximum)
	{
		return new Vector2(Math.Clamp(value.X, minimum, maximum), Math.Clamp(value.Y, minimum, maximum));
	}

	private static Vector2I ClampPrimaryStep(Vector2I value)
	{
		return new Vector2I(Math.Clamp(value.X, 1, 64), Math.Clamp(value.Y, 1, 64));
	}

	private static float ClampSmartThreshold(float value)
	{
		return Math.Clamp(value, 1f, 64f);
	}

	private static void SetSpinValue(SpinBox spin, double value)
	{
		if (GodotObject.IsInstanceValid(spin))
		{
			spin.Value = value;
		}
	}

	private static VBoxContainer MakeSection(VBoxContainer parent, string title, string subtitle)
	{
		VBoxContainer vBoxContainer = MakeStandaloneSection(title, subtitle);
		parent.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private static VBoxContainer MakeStandaloneSection(string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color("172735"), new Color("33536a"), 8, 1));
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 6);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(MakeLabel(title, 15, new Color("f1d487")), forceReadableName: false, InternalMode.Disabled);
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			Label label = MakeLabel(subtitle, 11, new Color("8daaba"));
			label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		}
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer2;
	}

	private static HBoxContainer MakeVectorRow(string icon, string title, SpinBox x, SpinBox y, Color accent)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(MakeBadge(icon, accent), forceReadableName: false, InternalMode.Disabled);
		Label label = MakeLabel(title, 12, new Color("dce8eb"));
		label.CustomMinimumSize = new Vector2(82f, 0f);
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(MakeAxisCell("X", x, new Color("ff8276")), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(MakeAxisCell("Y", y, new Color("7fda91")), forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static HBoxContainer MakeScalarRow(string icon, string title, SpinBox value, Color accent)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(MakeBadge(icon, accent), forceReadableName: false, InternalMode.Disabled);
		Label label = MakeLabel(title, 12, new Color("dce8eb"));
		label.CustomMinimumSize = new Vector2(82f, 0f);
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		value.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(value, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static VBoxContainer MakeAxisCell(string axis, SpinBox spin, Color color)
	{
		VBoxContainer obj = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label label = MakeLabel(axis, 10, color);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		obj.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		spin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		obj.AddChild(spin, forceReadableName: false, InternalMode.Disabled);
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
			CustomMinimumSize = new Vector2(86f, 32f)
		};
	}

	private static Button MakeButton(string text, string tooltip)
	{
		Button button = new Button();
		button.Text = text;
		button.TooltipText = tooltip;
		button.CustomMinimumSize = new Vector2(108f, 36f);
		button.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color("203b4d"), new Color("3e6a80"), 7, 1));
		button.AddThemeStyleboxOverride("hover", MakePanelStyle(new Color("2b5265"), new Color("6bc0d6"), 7, 1));
		button.AddThemeStyleboxOverride("pressed", MakePanelStyle(new Color("142c38"), new Color("f1cb72"), 7, 1));
		return button;
	}

	private static Label MakeBadge(string text, Color color)
	{
		Label label = MakeLabel(text, 20, color);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.CustomMinimumSize = new Vector2(34f, 34f);
		label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color("142b38"), color.Darkened(0.38f), 17, 1));
		return label;
	}

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static StyleBoxFlat MakePanelStyle(Color background, Color border, int radius, int width)
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
