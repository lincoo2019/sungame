using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorGradient : XWInspectorPropertyEditorBase
{
	private TextureRect _gradientPreview;

	private VBoxContainer _gradientStops;

	private OptionButton _interpolationModeOption;

	private OptionButton _colorSpaceOption;

	private SpinBox _offsetSpinBox;

	private ColorPickerButton _colorPickerButton;

	private Button _addPointButton;

	private Button _removePointButton;

	private Gradient _gradient;

	private GradientTexture1D _previewTexture;

	private int _selectedIndex = -1;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_gradientPreview = GetNode<TextureRect>("%GradientPreview");
		_gradientStops = GetNode<VBoxContainer>("%GradientStops");
		_interpolationModeOption = GetNode<OptionButton>("%InterpolationModeOption");
		_colorSpaceOption = GetNode<OptionButton>("%ColorSpaceOption");
		_offsetSpinBox = GetNode<SpinBox>("%OffsetSpinBox");
		_colorPickerButton = GetNode<ColorPickerButton>("%ColorPickerButton");
		_addPointButton = GetNode<Button>("%AddPointButton");
		_removePointButton = GetNode<Button>("%RemovePointButton");
		SetupOptions();
		_gradientPreview.GuiInput += OnGradientPreviewGuiInput;
		_interpolationModeOption.ItemSelected += OnInterpolationModeSelected;
		_colorSpaceOption.ItemSelected += OnColorSpaceSelected;
		_offsetSpinBox.ValueChanged += OnOffsetChanged;
		_colorPickerButton.ColorChanged += OnColorChanged;
		_addPointButton.Pressed += AddPoint;
		_removePointButton.Pressed += RemovePoint;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshGradient();
	}

	public override void UpdateValue()
	{
		RefreshGradient();
	}

	public override Variant GetValue()
	{
		return _gradient;
	}

	private void SetupOptions()
	{
		_interpolationModeOption.Clear();
		_interpolationModeOption.AddItem("线性", 0);
		_interpolationModeOption.AddItem("常量", 1);
		_interpolationModeOption.AddItem("三次", 2);
		_colorSpaceOption.Clear();
		_colorSpaceOption.AddItem("sRGB", 0);
		_colorSpaceOption.AddItem("线性 sRGB", 1);
		_colorSpaceOption.AddItem("Oklab", 2);
	}

	private void RefreshGradient()
	{
		_gradient = ReadGradient();
		if (!GodotObject.IsInstanceValid(_gradient))
		{
			ClearStopRows();
			_gradientPreview.Texture = null;
			_selectedIndex = -1;
			RefreshSelectedControls();
			return;
		}
		if (_selectedIndex < 0 || _selectedIndex >= _gradient.GetPointCount())
		{
			_selectedIndex = ((_gradient.GetPointCount() <= 0) ? (-1) : 0);
		}
		UpdatePreviewTexture();
		RebuildGradientStops();
		RefreshSelectedControls();
	}

	private Gradient ReadGradient()
	{
		if (GodotObject.IsInstanceValid(Property) && GodotObject.IsInstanceValid(Property.Object) && Property.PropName == (StringName)"" && Property.Object is Gradient result)
		{
			return result;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Gradient result2))
		{
			return null;
		}
		return result2;
	}

	private void UpdatePreviewTexture()
	{
		if (GodotObject.IsInstanceValid(_gradient))
		{
			if (_previewTexture == null)
			{
				_previewTexture = new GradientTexture1D
				{
					Width = 256
				};
			}
			_previewTexture.Gradient = _gradient;
			_gradientPreview.Texture = _previewTexture;
		}
	}

	private void RebuildGradientStops()
	{
		ClearStopRows();
		if (!GodotObject.IsInstanceValid(_gradient))
		{
			return;
		}
		int pointCount = _gradient.GetPointCount();
		for (int i = 0; i < pointCount; i++)
		{
			int pointIndex = i;
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"GradientStop{pointIndex}",
				CustomMinimumSize = new Vector2(0f, 24f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			hBoxContainer.AddThemeConstantOverride("separation", 4);
			ColorRect node = new ColorRect
			{
				Color = _gradient.GetColor(pointIndex),
				CustomMinimumSize = new Vector2(28f, 20f),
				SizeFlagsVertical = SizeFlags.ShrinkCenter
			};
			hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			Button button = new Button
			{
				Text = $"{pointIndex}: {_gradient.GetOffset(pointIndex):0.###}",
				ToggleMode = true,
				ButtonPressed = (pointIndex == _selectedIndex),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Alignment = HorizontalAlignment.Left,
				TooltipText = "选择这个颜色点"
			};
			button.Pressed += delegate
			{
				SelectPoint(pointIndex);
			};
			hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			_gradientStops.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void ClearStopRows()
	{
		if (!GodotObject.IsInstanceValid(_gradientStops))
		{
			return;
		}
		foreach (Node child in _gradientStops.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void RefreshSelectedControls()
	{
		_updating = true;
		bool flag = GodotObject.IsInstanceValid(_gradient);
		bool flag2 = flag && _selectedIndex >= 0 && _selectedIndex < _gradient.GetPointCount();
		_interpolationModeOption.Disabled = !flag;
		_colorSpaceOption.Disabled = !flag;
		_offsetSpinBox.Editable = flag2;
		_colorPickerButton.Disabled = !flag2;
		_addPointButton.Disabled = !flag;
		_removePointButton.Disabled = !flag2 || _gradient.GetPointCount() <= 1;
		if (flag)
		{
			_interpolationModeOption.Select((int)_gradient.InterpolationMode);
			_colorSpaceOption.Select((int)_gradient.InterpolationColorSpace);
		}
		if (flag2)
		{
			_offsetSpinBox.Value = _gradient.GetOffset(_selectedIndex);
			_colorPickerButton.Color = _gradient.GetColor(_selectedIndex);
		}
		else
		{
			_offsetSpinBox.Value = 0.0;
			_colorPickerButton.Color = Colors.White;
		}
		_updating = false;
	}

	private void SelectPoint(int index)
	{
		if (GodotObject.IsInstanceValid(_gradient))
		{
			_selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(_gradient.GetPointCount() - 1, 0));
			RebuildGradientStops();
			RefreshSelectedControls();
		}
	}

	private void AddPoint()
	{
		if (GodotObject.IsInstanceValid(_gradient))
		{
			float offset = 0.5f;
			if (_selectedIndex >= 0 && _selectedIndex < _gradient.GetPointCount())
			{
				offset = Mathf.Clamp(_gradient.GetOffset(_selectedIndex) + 0.05f, 0f, 1f);
			}
			Color color = _gradient.Sample(offset);
			_gradient.AddPoint(offset, color);
			_selectedIndex = FindClosestPoint(offset);
			CommitGradientChange();
		}
	}

	private void RemovePoint()
	{
		if (GodotObject.IsInstanceValid(_gradient) && _selectedIndex >= 0 && _selectedIndex < _gradient.GetPointCount() && _gradient.GetPointCount() > 1)
		{
			_gradient.RemovePoint(_selectedIndex);
			_selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(_gradient.GetPointCount() - 1, 0));
			CommitGradientChange();
		}
	}

	private void OnGradientPreviewGuiInput(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_gradient) || !(inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton))
		{
			return;
		}
		float offset = Mathf.Clamp(inputEventMouseButton.Position.X / Mathf.Max(_gradientPreview.Size.X, 1f), 0f, 1f);
		if (inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			_gradient.AddPoint(offset, _gradient.Sample(offset));
			_selectedIndex = FindClosestPoint(offset);
			CommitGradientChange();
		}
		else if (inputEventMouseButton.ButtonIndex == MouseButton.Right)
		{
			int num = FindClosestPoint(offset);
			if (num >= 0)
			{
				SelectPoint(num);
			}
		}
	}

	private void OnInterpolationModeSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_gradient))
		{
			int itemId = _interpolationModeOption.GetItemId((int)index);
			_gradient.InterpolationMode = (Gradient.InterpolationModeEnum)itemId;
			CommitGradientChange();
		}
	}

	private void OnColorSpaceSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_gradient))
		{
			int itemId = _colorSpaceOption.GetItemId((int)index);
			_gradient.InterpolationColorSpace = (Gradient.ColorSpace)itemId;
			CommitGradientChange();
		}
	}

	private void OnOffsetChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_gradient) && _selectedIndex >= 0 && _selectedIndex < _gradient.GetPointCount())
		{
			_gradient.SetOffset(_selectedIndex, Mathf.Clamp((float)value, 0f, 1f));
			_selectedIndex = FindClosestPoint((float)value);
			CommitGradientChange();
		}
	}

	private void OnColorChanged(Color color)
	{
		if (!_updating && GodotObject.IsInstanceValid(_gradient) && _selectedIndex >= 0 && _selectedIndex < _gradient.GetPointCount())
		{
			_gradient.SetColor(_selectedIndex, color);
			CommitGradientChange();
		}
	}

	private int FindClosestPoint(float offset)
	{
		if (!GodotObject.IsInstanceValid(_gradient))
		{
			return -1;
		}
		int result = -1;
		float num = 3.4028235E+38f;
		int pointCount = _gradient.GetPointCount();
		for (int i = 0; i < pointCount; i++)
		{
			float num2 = Mathf.Abs(_gradient.GetOffset(i) - offset);
			if (num2 <= num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private void CommitGradientChange()
	{
		if (GodotObject.IsInstanceValid(_gradient))
		{
			_gradient.EmitChanged();
			if (GodotObject.IsInstanceValid(_previewTexture))
			{
				_previewTexture.Gradient = _gradient;
				_previewTexture.EmitChanged();
			}
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_gradient);
			}
			else
			{
				Property?.SetCall();
			}
			RebuildGradientStops();
			RefreshSelectedControls();
		}
	}
}
