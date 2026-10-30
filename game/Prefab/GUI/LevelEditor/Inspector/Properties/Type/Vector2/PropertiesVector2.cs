using Godot;

[GlobalClass]
public partial class PropertiesVector2 : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Vector2 value);

	private SpinBox _spinBoxX;

	private SpinBox _spinBoxY;

	public Vector2 value = Vector2.Zero;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_spinBoxX = GetNode<SpinBox>("%SpinBoxX");
		_spinBoxY = GetNode<SpinBox>("%SpinBoxY");
		value = new Vector2((float)_spinBoxX.Value, (float)_spinBoxY.Value);
		_spinBoxX.ValueChanged += ValueChange;
		_spinBoxY.ValueChanged += ValueChange;
	}

	public void SetValue(Vector2 newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			_spinBoxX.ValueChanged -= ValueChange;
			_spinBoxY.ValueChanged -= ValueChange;
			_spinBoxX.Value = value.X;
			_spinBoxY.Value = value.Y;
			_spinBoxX.ValueChanged += ValueChange;
			_spinBoxY.ValueChanged += ValueChange;
		}
	}

	private void ValueChange(double v)
	{
		value = new Vector2((float)_spinBoxX.Value, (float)_spinBoxY.Value);
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		if (!_spinBoxX.GetLineEdit().HasFocus())
		{
			return !_spinBoxY.GetLineEdit().HasFocus();
		}
		return false;
	}

	public override string _GetType()
	{
		return "Vector2";
	}
}
