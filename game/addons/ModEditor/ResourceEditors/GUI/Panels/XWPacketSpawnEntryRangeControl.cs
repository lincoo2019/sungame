using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketSpawnEntryRangeControl : Control
{
	[Signal]
	public delegate void DragStartedEventHandler(bool minimumHandle);

	[Signal]
	public delegate void ValuesPreviewedEventHandler(double minimum, double maximum);

	[Signal]
	public delegate void ValuesCommittedEventHandler(double minimum, double maximum);

	private Label _title;

	private Control _rangeTrack;

	private Control _minimumHandle;

	private Control _maximumHandle;

	private Label _minimumValue;

	private Label _maximumValue;

	private HBoxContainer _unlimitedControls;

	private CheckButton _unlimitedMinimum;

	private CheckButton _unlimitedMaximum;

	private double _minimumLimit;

	private double _maximumLimit = 1.0;

	private double _finiteMinimumLimit;

	private double _step = 1.0;

	private double _minimum;

	private double _maximum = 1.0;

	private double _lastFiniteMinimum;

	private double _lastFiniteMaximum = 1.0;

	private bool _allowUnlimited;

	private bool _dragging;

	private bool _draggingMinimum;

	private bool _updatingControls;

	public override void _Ready()
	{
		_title = GetNode<Label>("%TitleLabel");
		_rangeTrack = GetNode<Control>("%RangeTrack");
		_minimumHandle = GetNode<Control>("%MinimumHandle");
		_maximumHandle = GetNode<Control>("%MaximumHandle");
		_minimumValue = GetNode<Label>("%MinimumValue");
		_maximumValue = GetNode<Label>("%MaximumValue");
		_unlimitedControls = GetNode<HBoxContainer>("%UnlimitedControls");
		_unlimitedMinimum = GetNode<CheckButton>("%UnlimitedMinimum");
		_unlimitedMaximum = GetNode<CheckButton>("%UnlimitedMaximum");
		_rangeTrack.GuiInput += OnRangeTrackGuiInput;
		_rangeTrack.Resized += UpdateVisuals;
		_unlimitedMinimum.Toggled += OnUnlimitedMinimumToggled;
		_unlimitedMaximum.Toggled += OnUnlimitedMaximumToggled;
		UpdateVisuals();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_rangeTrack))
		{
			_rangeTrack.GuiInput -= OnRangeTrackGuiInput;
			_rangeTrack.Resized -= UpdateVisuals;
		}
		if (GodotObject.IsInstanceValid(_unlimitedMinimum))
		{
			_unlimitedMinimum.Toggled -= OnUnlimitedMinimumToggled;
		}
		if (GodotObject.IsInstanceValid(_unlimitedMaximum))
		{
			_unlimitedMaximum.Toggled -= OnUnlimitedMaximumToggled;
		}
		base._ExitTree();
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_rangeTrack))
		{
			Rect2 globalRect = _rangeTrack.GetGlobalRect();
			Vector2 vector = GetGlobalTransformWithCanvas().AffineInverse() * globalRect.Position;
			float y = vector.Y + globalRect.Size.Y * 0.5f;
			float num = vector.X + 8f;
			float num2 = vector.X + Math.Max(8f, globalRect.Size.X - 8f);
			float x = num + (num2 - num) * (float)GetNormalizedValue(_minimum, minimumEndpoint: true);
			float x2 = num + (num2 - num) * (float)GetNormalizedValue(_maximum, minimumEndpoint: false);
			DrawLine(new Vector2(num, y), new Vector2(num2, y), new Color(0.17f, 0.22f, 0.12f), 7f, antialiased: true);
			DrawLine(new Vector2(x, y), new Vector2(x2, y), new Color(0.66f, 0.86f, 0.26f), 7f, antialiased: true);
		}
	}

	public void Configure(double minimumLimit, double maximumLimit, double step, bool allowUnlimited)
	{
		_minimumLimit = minimumLimit;
		_maximumLimit = Math.Max(minimumLimit, maximumLimit);
		_step = Math.Max(0.0001, step);
		_allowUnlimited = allowUnlimited;
		_finiteMinimumLimit = (allowUnlimited ? Math.Max(0.0, minimumLimit) : minimumLimit);
		if (GodotObject.IsInstanceValid(_unlimitedControls))
		{
			_unlimitedControls.Visible = allowUnlimited;
		}
		SetValues(_minimum, _maximum);
	}

	public void SetTitle(string title)
	{
		if (GodotObject.IsInstanceValid(_title))
		{
			_title.Text = title ?? "区间";
		}
	}

	public void SetValues(double minimum, double maximum)
	{
		_minimum = NormalizeEndpoint(minimum);
		_maximum = NormalizeEndpoint(maximum);
		ConstrainFiniteEndpoints(ref _minimum, ref _maximum, movedMinimum: true);
		if (!IsUnlimited(_minimum))
		{
			_lastFiniteMinimum = _minimum;
		}
		if (!IsUnlimited(_maximum))
		{
			_lastFiniteMaximum = _maximum;
		}
		UpdateVisuals();
	}

	private void OnRangeTrackGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				_draggingMinimum = IsMinimumHandleCloser(inputEventMouseButton.Position.X);
				_dragging = true;
				EmitSignal(SignalName.DragStarted, _draggingMinimum);
				PreviewPointer(inputEventMouseButton.Position.X);
				_rangeTrack.AcceptEvent();
			}
			else if (_dragging)
			{
				PreviewPointer(inputEventMouseButton.Position.X);
				_dragging = false;
				EmitSignal(SignalName.ValuesCommitted, _minimum, _maximum);
				_rangeTrack.AcceptEvent();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
		{
			PreviewPointer(inputEventMouseMotion.Position.X);
			_rangeTrack.AcceptEvent();
		}
	}

	private void PreviewPointer(float pointerX)
	{
		float num = Math.Max(1f, _rangeTrack.Size.X - 16f);
		double num2 = Math.Clamp((pointerX - 8f) / num, 0f, 1f);
		double num3 = Quantize(_finiteMinimumLimit + num2 * (_maximumLimit - _finiteMinimumLimit));
		if (_draggingMinimum)
		{
			_minimum = num3;
			_lastFiniteMinimum = num3;
		}
		else
		{
			_maximum = num3;
			_lastFiniteMaximum = num3;
		}
		ConstrainFiniteEndpoints(ref _minimum, ref _maximum, _draggingMinimum);
		UpdateVisuals();
		EmitSignal(SignalName.ValuesPreviewed, _minimum, _maximum);
	}

	private bool IsMinimumHandleCloser(float pointerX)
	{
		float num = Math.Max(1f, _rangeTrack.Size.X - 16f);
		float num2 = 8f + num * (float)GetNormalizedValue(_minimum, minimumEndpoint: true);
		float num3 = 8f + num * (float)GetNormalizedValue(_maximum, minimumEndpoint: false);
		return Math.Abs(pointerX - num2) <= Math.Abs(pointerX - num3);
	}

	private void OnUnlimitedMinimumToggled(bool enabled)
	{
		if (!_updatingControls && _allowUnlimited)
		{
			EmitSignal(SignalName.DragStarted, true);
			_minimum = (enabled ? (-1.0) : NormalizeEndpoint(_lastFiniteMinimum));
			ConstrainFiniteEndpoints(ref _minimum, ref _maximum, movedMinimum: true);
			UpdateVisuals();
			EmitSignal(SignalName.ValuesPreviewed, _minimum, _maximum);
			EmitSignal(SignalName.ValuesCommitted, _minimum, _maximum);
		}
	}

	private void OnUnlimitedMaximumToggled(bool enabled)
	{
		if (!_updatingControls && _allowUnlimited)
		{
			EmitSignal(SignalName.DragStarted, false);
			_maximum = (enabled ? (-1.0) : NormalizeEndpoint(_lastFiniteMaximum));
			ConstrainFiniteEndpoints(ref _minimum, ref _maximum, movedMinimum: false);
			UpdateVisuals();
			EmitSignal(SignalName.ValuesPreviewed, _minimum, _maximum);
			EmitSignal(SignalName.ValuesCommitted, _minimum, _maximum);
		}
	}

	private void UpdateVisuals()
	{
		if (GodotObject.IsInstanceValid(_rangeTrack))
		{
			_updatingControls = true;
			if (GodotObject.IsInstanceValid(_unlimitedMinimum))
			{
				_unlimitedMinimum.ButtonPressed = IsUnlimited(_minimum);
			}
			if (GodotObject.IsInstanceValid(_unlimitedMaximum))
			{
				_unlimitedMaximum.ButtonPressed = IsUnlimited(_maximum);
			}
			_updatingControls = false;
			if (GodotObject.IsInstanceValid(_minimumValue))
			{
				_minimumValue.Text = FormatValue(_minimum);
			}
			if (GodotObject.IsInstanceValid(_maximumValue))
			{
				_maximumValue.Text = FormatValue(_maximum);
			}
			float num = Math.Max(1f, _rangeTrack.Size.X - 16f);
			PositionHandle(_minimumHandle, 8f + num * (float)GetNormalizedValue(_minimum, minimumEndpoint: true));
			PositionHandle(_maximumHandle, 8f + num * (float)GetNormalizedValue(_maximum, minimumEndpoint: false));
			QueueRedraw();
		}
	}

	private void PositionHandle(Control handle, float centerX)
	{
		if (GodotObject.IsInstanceValid(handle))
		{
			handle.Position = new Vector2(centerX - handle.Size.X * 0.5f, (_rangeTrack.Size.Y - handle.Size.Y) * 0.5f);
		}
	}

	private double NormalizeEndpoint(double value)
	{
		if (IsUnlimited(value))
		{
			return -1.0;
		}
		return Quantize(Math.Clamp(value, _finiteMinimumLimit, _maximumLimit));
	}

	private double Quantize(double value)
	{
		double num = Math.Round((value - _finiteMinimumLimit) / _step);
		return Math.Clamp(_finiteMinimumLimit + num * _step, _finiteMinimumLimit, _maximumLimit);
	}

	private void ConstrainFiniteEndpoints(ref double minimum, ref double maximum, bool movedMinimum)
	{
		if (!IsUnlimited(minimum) && !IsUnlimited(maximum) && !(minimum <= maximum))
		{
			if (movedMinimum)
			{
				minimum = maximum;
			}
			else
			{
				maximum = minimum;
			}
		}
	}

	private double GetNormalizedValue(double value, bool minimumEndpoint)
	{
		if (IsUnlimited(value))
		{
			return (!minimumEndpoint) ? 1 : 0;
		}
		double num = Math.Max(0.0001, _maximumLimit - _finiteMinimumLimit);
		return Math.Clamp((value - _finiteMinimumLimit) / num, 0.0, 1.0);
	}

	private bool IsUnlimited(double value)
	{
		if (_allowUnlimited)
		{
			return value < 0.0;
		}
		return false;
	}

	private string FormatValue(double value)
	{
		if (IsUnlimited(value))
		{
			return "无限制";
		}
		if (!(Math.Abs(value - Math.Round(value)) < 0.0001))
		{
			return value.ToString("0.##");
		}
		return Math.Round(value).ToString("0");
	}
}
