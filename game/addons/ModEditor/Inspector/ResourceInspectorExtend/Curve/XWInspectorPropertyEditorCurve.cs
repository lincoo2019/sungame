using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorCurve : XWInspectorPropertyEditorBase
{
	private Control _curvePreview;

	private VBoxContainer _curvePoints;

	private SpinBox _minDomainSpinBox;

	private SpinBox _maxDomainSpinBox;

	private SpinBox _minValueSpinBox;

	private SpinBox _maxValueSpinBox;

	private SpinBox _offsetSpinBox;

	private SpinBox _valueSpinBox;

	private OptionButton _leftModeOption;

	private OptionButton _rightModeOption;

	private Button _addPointButton;

	private Button _removePointButton;

	private Curve _curve;

	private int _selectedIndex = -1;

	private bool _updating;

	private bool _draggingPoint;

	public override void _Ready()
	{
		base._Ready();
		_curvePreview = GetNode<Control>("%CurvePreview");
		_curvePoints = GetNode<VBoxContainer>("%CurvePoints");
		_minDomainSpinBox = GetNode<SpinBox>("%MinDomainSpinBox");
		_maxDomainSpinBox = GetNode<SpinBox>("%MaxDomainSpinBox");
		_minValueSpinBox = GetNode<SpinBox>("%MinValueSpinBox");
		_maxValueSpinBox = GetNode<SpinBox>("%MaxValueSpinBox");
		_offsetSpinBox = GetNode<SpinBox>("%OffsetSpinBox");
		_valueSpinBox = GetNode<SpinBox>("%ValueSpinBox");
		_leftModeOption = GetNode<OptionButton>("%LeftModeOption");
		_rightModeOption = GetNode<OptionButton>("%RightModeOption");
		_addPointButton = GetNode<Button>("%AddPointButton");
		_removePointButton = GetNode<Button>("%RemovePointButton");
		SetupTangentModeOptions();
		_curvePreview.Draw += OnDrawCurvePreview;
		_curvePreview.GuiInput += OnCurvePreviewGuiInput;
		_minDomainSpinBox.ValueChanged += OnMinDomainChanged;
		_maxDomainSpinBox.ValueChanged += OnMaxDomainChanged;
		_minValueSpinBox.ValueChanged += OnMinValueChanged;
		_maxValueSpinBox.ValueChanged += OnMaxValueChanged;
		_offsetSpinBox.ValueChanged += OnOffsetChanged;
		_valueSpinBox.ValueChanged += OnValueChanged;
		_leftModeOption.ItemSelected += OnLeftModeSelected;
		_rightModeOption.ItemSelected += OnRightModeSelected;
		_addPointButton.Pressed += AddPoint;
		_removePointButton.Pressed += RemovePoint;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshCurve();
	}

	public override void UpdateValue()
	{
		RefreshCurve();
	}

	public override Variant GetValue()
	{
		return _curve;
	}

	private void SetupTangentModeOptions()
	{
		_leftModeOption.Clear();
		_leftModeOption.AddItem("自由", 0);
		_leftModeOption.AddItem("线性", 1);
		_rightModeOption.Clear();
		_rightModeOption.AddItem("自由", 0);
		_rightModeOption.AddItem("线性", 1);
	}

	private void RefreshCurve()
	{
		_curve = ReadCurve();
		if (!GodotObject.IsInstanceValid(_curve))
		{
			ClearPointRows();
			_selectedIndex = -1;
			RefreshSelectedControls();
			_curvePreview.QueueRedraw();
			return;
		}
		if (_selectedIndex < 0 || _selectedIndex >= _curve.PointCount)
		{
			_selectedIndex = ((_curve.PointCount <= 0) ? (-1) : 0);
		}
		RebuildCurvePoints();
		RefreshSelectedControls();
		_curvePreview.QueueRedraw();
	}

	private Curve ReadCurve()
	{
		if (GodotObject.IsInstanceValid(Property) && GodotObject.IsInstanceValid(Property.Object) && Property.PropName == (StringName)"" && Property.Object is Curve result)
		{
			return result;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Curve result2))
		{
			return null;
		}
		return result2;
	}

	private void RebuildCurvePoints()
	{
		ClearPointRows();
		if (!GodotObject.IsInstanceValid(_curve))
		{
			return;
		}
		for (int i = 0; i < _curve.PointCount; i++)
		{
			int pointIndex = i;
			Vector2 pointPosition = _curve.GetPointPosition(pointIndex);
			Button button = new Button
			{
				Text = $"{pointIndex}: {pointPosition.X:0.###}, {pointPosition.Y:0.###}",
				ToggleMode = true,
				ButtonPressed = (pointIndex == _selectedIndex),
				Alignment = HorizontalAlignment.Left,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				TooltipText = "选择这个曲线点"
			};
			button.Pressed += delegate
			{
				SelectPoint(pointIndex);
			};
			_curvePoints.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void ClearPointRows()
	{
		if (!GodotObject.IsInstanceValid(_curvePoints))
		{
			return;
		}
		foreach (Node child in _curvePoints.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void RefreshSelectedControls()
	{
		_updating = true;
		bool flag = GodotObject.IsInstanceValid(_curve);
		bool flag2 = flag && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount;
		_minDomainSpinBox.Editable = flag;
		_maxDomainSpinBox.Editable = flag;
		_minValueSpinBox.Editable = flag;
		_maxValueSpinBox.Editable = flag;
		_offsetSpinBox.Editable = flag2;
		_valueSpinBox.Editable = flag2;
		_leftModeOption.Disabled = !flag2;
		_rightModeOption.Disabled = !flag2;
		_addPointButton.Disabled = !flag;
		_removePointButton.Disabled = !flag2;
		if (flag)
		{
			_minDomainSpinBox.Value = _curve.MinDomain;
			_maxDomainSpinBox.Value = _curve.MaxDomain;
			_minValueSpinBox.Value = _curve.MinValue;
			_maxValueSpinBox.Value = _curve.MaxValue;
			_offsetSpinBox.MinValue = _curve.MinDomain;
			_offsetSpinBox.MaxValue = _curve.MaxDomain;
			_valueSpinBox.MinValue = _curve.MinValue;
			_valueSpinBox.MaxValue = _curve.MaxValue;
		}
		if (flag2)
		{
			Vector2 pointPosition = _curve.GetPointPosition(_selectedIndex);
			_offsetSpinBox.Value = pointPosition.X;
			_valueSpinBox.Value = pointPosition.Y;
			_leftModeOption.Select(GetTangentModeItemIndex(_leftModeOption, _curve.GetPointLeftMode(_selectedIndex)));
			_rightModeOption.Select(GetTangentModeItemIndex(_rightModeOption, _curve.GetPointRightMode(_selectedIndex)));
		}
		else
		{
			_offsetSpinBox.Value = 0.0;
			_valueSpinBox.Value = 0.0;
			_leftModeOption.Select(0);
			_rightModeOption.Select(0);
		}
		_updating = false;
	}

	private static int GetTangentModeItemIndex(OptionButton option, Curve.TangentMode mode)
	{
		int num = (int)mode;
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == num)
			{
				return i;
			}
		}
		return 0;
	}

	private void SelectPoint(int index)
	{
		if (GodotObject.IsInstanceValid(_curve))
		{
			_selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(_curve.PointCount - 1, 0));
			RebuildCurvePoints();
			RefreshSelectedControls();
			_curvePreview.QueueRedraw();
		}
	}

	private void AddPoint()
	{
		if (GodotObject.IsInstanceValid(_curve))
		{
			float num = Mathf.Clamp((float)_offsetSpinBox.Value, _curve.MinDomain, _curve.MaxDomain);
			float y = Mathf.Clamp((float)_valueSpinBox.Value, _curve.MinValue, _curve.MaxValue);
			if (_selectedIndex < 0)
			{
				num = Mathf.Lerp(_curve.MinDomain, _curve.MaxDomain, 0.5f);
				y = _curve.Sample(num);
			}
			_curve.AddPoint(new Vector2(num, y), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
			_selectedIndex = FindClosestPoint(new Vector2(num, y), compareValue: false);
			CommitCurveChange();
		}
	}

	private void RemovePoint()
	{
		if (GodotObject.IsInstanceValid(_curve) && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			_curve.RemovePoint(_selectedIndex);
			_selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(_curve.PointCount - 1, 0));
			CommitCurveChange();
		}
	}

	private void OnCurvePreviewGuiInput(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_curve))
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				if (inputEventMouseButton.Pressed)
				{
					int num = FindPointAtCanvas(inputEventMouseButton.Position);
					if (num >= 0)
					{
						_selectedIndex = num;
						_draggingPoint = true;
						SelectPoint(num);
					}
					else
					{
						Vector2 vector = CanvasToCurve(inputEventMouseButton.Position);
						_curve.AddPoint(vector, 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
						_selectedIndex = FindClosestPoint(vector, compareValue: false);
						_draggingPoint = true;
						CommitCurveChange();
					}
				}
				else
				{
					_draggingPoint = false;
				}
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
			{
				int num2 = FindPointAtCanvas(inputEventMouseButton.Position);
				if (num2 >= 0)
				{
					SelectPoint(num2);
				}
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _draggingPoint && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			Vector2 curvePosition = CanvasToCurve(inputEventMouseMotion.Position);
			_curve.SetPointOffset(_selectedIndex, curvePosition.X);
			_selectedIndex = FindClosestPoint(curvePosition, compareValue: false);
			if (_selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
			{
				_curve.SetPointValue(_selectedIndex, curvePosition.Y);
			}
			CommitCurveChange();
		}
	}

	private void OnMinDomainChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve))
		{
			_curve.MinDomain = Mathf.Min((float)value, _curve.MaxDomain - 0.001f);
			CommitCurveChange();
		}
	}

	private void OnMaxDomainChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve))
		{
			_curve.MaxDomain = Mathf.Max((float)value, _curve.MinDomain + 0.001f);
			CommitCurveChange();
		}
	}

	private void OnMinValueChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve))
		{
			_curve.MinValue = Mathf.Min((float)value, _curve.MaxValue - 0.001f);
			CommitCurveChange();
		}
	}

	private void OnMaxValueChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve))
		{
			_curve.MaxValue = Mathf.Max((float)value, _curve.MinValue + 0.001f);
			CommitCurveChange();
		}
	}

	private void OnOffsetChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve) && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			Vector2 pointPosition = _curve.GetPointPosition(_selectedIndex);
			_curve.SetPointOffset(_selectedIndex, Mathf.Clamp((float)value, _curve.MinDomain, _curve.MaxDomain));
			_selectedIndex = FindClosestPoint(new Vector2((float)value, pointPosition.Y), compareValue: false);
			CommitCurveChange();
		}
	}

	private void OnValueChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve) && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			_curve.SetPointValue(_selectedIndex, Mathf.Clamp((float)value, _curve.MinValue, _curve.MaxValue));
			CommitCurveChange();
		}
	}

	private void OnLeftModeSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve) && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			_curve.SetPointLeftMode(_selectedIndex, (Curve.TangentMode)_leftModeOption.GetItemId((int)index));
			CommitCurveChange();
		}
	}

	private void OnRightModeSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_curve) && _selectedIndex >= 0 && _selectedIndex < _curve.PointCount)
		{
			_curve.SetPointRightMode(_selectedIndex, (Curve.TangentMode)_rightModeOption.GetItemId((int)index));
			CommitCurveChange();
		}
	}

	private void OnDrawCurvePreview()
	{
		Rect2 rect = new Rect2(Vector2.Zero, _curvePreview.Size);
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = new Color(0.085f, 0.085f, 0.085f)
		};
		styleBoxFlat.SetCornerRadiusAll(3);
		_curvePreview.DrawStyleBox(styleBoxFlat, rect);
		DrawGrid(rect);
		if (GodotObject.IsInstanceValid(_curve))
		{
			List<Vector2> list = new List<Vector2>();
			int num = 96;
			for (int i = 0; i < num; i++)
			{
				float weight = (float)i / ((float)num - 1f);
				float num2 = Mathf.Lerp(_curve.MinDomain, _curve.MaxDomain, weight);
				list.Add(CurveToCanvas(new Vector2(num2, _curve.Sample(num2))));
			}
			if (list.Count > 1)
			{
				_curvePreview.DrawPolyline(list.ToArray(), new Color(0.36f, 0.62f, 1f), 2f, antialiased: true);
			}
			for (int j = 0; j < _curve.PointCount; j++)
			{
				Vector2 position = CurveToCanvas(_curve.GetPointPosition(j));
				Color color = ((j == _selectedIndex) ? new Color(1f, 0.76f, 0.24f) : new Color(0.8f, 0.86f, 1f));
				_curvePreview.DrawCircle(position, (j == _selectedIndex) ? 5f : 4f, color);
			}
		}
	}

	private void DrawGrid(Rect2 rect)
	{
		Color color = new Color(1f, 1f, 1f, 0.09f);
		Color color2 = new Color(1f, 1f, 1f, 0.045f);
		for (int i = 1; i < 4; i++)
		{
			float x = rect.Size.X * (float)i / 4f;
			float y = rect.Size.Y * (float)i / 4f;
			_curvePreview.DrawLine(new Vector2(x, 0f), new Vector2(x, rect.Size.Y), color2, 1f);
			_curvePreview.DrawLine(new Vector2(0f, y), new Vector2(rect.Size.X, y), color2, 1f);
		}
		_curvePreview.DrawRect(rect.Grow(-0.5f), color, filled: false, 1f);
	}

	private Vector2 CurveToCanvas(Vector2 curvePoint)
	{
		if (!GodotObject.IsInstanceValid(_curve))
		{
			return Vector2.Zero;
		}
		float num = Mathf.Max(_curve.MaxDomain - _curve.MinDomain, 0.001f);
		float num2 = Mathf.Max(_curve.MaxValue - _curve.MinValue, 0.001f);
		float value = (curvePoint.X - _curve.MinDomain) / num;
		return new Vector2(y: (1f - Mathf.Clamp((curvePoint.Y - _curve.MinValue) / num2, 0f, 1f)) * _curvePreview.Size.Y, x: Mathf.Clamp(value, 0f, 1f) * _curvePreview.Size.X);
	}

	private Vector2 CanvasToCurve(Vector2 canvasPoint)
	{
		float weight = Mathf.Clamp(canvasPoint.X / Mathf.Max(_curvePreview.Size.X, 1f), 0f, 1f);
		float weight2 = 1f - Mathf.Clamp(canvasPoint.Y / Mathf.Max(_curvePreview.Size.Y, 1f), 0f, 1f);
		return new Vector2(Mathf.Lerp(_curve.MinDomain, _curve.MaxDomain, weight), Mathf.Lerp(_curve.MinValue, _curve.MaxValue, weight2));
	}

	private int FindPointAtCanvas(Vector2 canvasPosition)
	{
		if (!GodotObject.IsInstanceValid(_curve))
		{
			return -1;
		}
		for (int i = 0; i < _curve.PointCount; i++)
		{
			if (CurveToCanvas(_curve.GetPointPosition(i)).DistanceTo(canvasPosition) <= 8f)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindClosestPoint(Vector2 curvePosition, bool compareValue)
	{
		if (!GodotObject.IsInstanceValid(_curve))
		{
			return -1;
		}
		int result = -1;
		float num = 3.4028235E+38f;
		for (int i = 0; i < _curve.PointCount; i++)
		{
			Vector2 pointPosition = _curve.GetPointPosition(i);
			float num2 = (compareValue ? pointPosition.DistanceSquaredTo(curvePosition) : Mathf.Abs(pointPosition.X - curvePosition.X));
			if (num2 <= num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private void CommitCurveChange()
	{
		if (GodotObject.IsInstanceValid(_curve))
		{
			_curve.Bake();
			_curve.EmitChanged();
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_curve);
			}
			else
			{
				Property?.SetCall();
			}
			RebuildCurvePoints();
			RefreshSelectedControls();
			_curvePreview.QueueRedraw();
		}
	}
}
