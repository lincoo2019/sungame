using Godot;

[GlobalClass]
public partial class PropertiesVector4i : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Vector4I value);

	private SpinBox _spinBoxX1;

	private SpinBox _spinBoxY1;

	private SpinBox _spinBoxX2;

	private SpinBox _spinBoxY2;

	public Vector4I value = Vector4I.Zero;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_spinBoxX1 = GetNode<SpinBox>("%SpinBoxX1");
		_spinBoxY1 = GetNode<SpinBox>("%SpinBoxY1");
		_spinBoxX2 = GetNode<SpinBox>("%SpinBoxX2");
		_spinBoxY2 = GetNode<SpinBox>("%SpinBoxY2");
		value = new Vector4I((int)_spinBoxX1.Value, (int)_spinBoxY1.Value, (int)_spinBoxX2.Value, (int)_spinBoxY2.Value);
		_spinBoxX1.ValueChanged += ValueChange;
		_spinBoxY1.ValueChanged += ValueChange;
		_spinBoxX2.ValueChanged += ValueChange;
		_spinBoxY2.ValueChanged += ValueChange;
	}

	public void SetValue(Vector4I newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			_spinBoxX1.ValueChanged -= ValueChange;
			_spinBoxY1.ValueChanged -= ValueChange;
			_spinBoxX2.ValueChanged -= ValueChange;
			_spinBoxY2.ValueChanged -= ValueChange;
			_spinBoxX1.Value = value.X;
			_spinBoxY1.Value = value.Y;
			_spinBoxX2.Value = value.Z;
			_spinBoxY2.Value = value.W;
			_spinBoxX1.ValueChanged += ValueChange;
			_spinBoxY1.ValueChanged += ValueChange;
			_spinBoxX2.ValueChanged += ValueChange;
			_spinBoxY2.ValueChanged += ValueChange;
		}
	}

	private void ValueChange(double v)
	{
		value = new Vector4I((int)_spinBoxX1.Value, (int)_spinBoxY1.Value, (int)_spinBoxX2.Value, (int)_spinBoxY2.Value);
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		if (!_spinBoxX1.GetLineEdit().HasFocus() && !_spinBoxY1.GetLineEdit().HasFocus() && !_spinBoxX2.GetLineEdit().HasFocus())
		{
			return !_spinBoxY2.GetLineEdit().HasFocus();
		}
		return false;
	}

	public override string _GetType()
	{
		return "Vector4i";
	}
}
