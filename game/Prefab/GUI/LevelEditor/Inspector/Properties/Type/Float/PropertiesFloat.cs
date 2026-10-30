using Godot;

[GlobalClass]
public partial class PropertiesFloat : PropertiesBase
{
	public delegate void ValueChangedEventHandler(double value);

	private SpinBox _spinBox;

	public double value;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_spinBox = GetNode<SpinBox>("%SpinBox");
		value = _spinBox.Value;
		_spinBox.ValueChanged += ValueChange;
	}

	public void SetValue(double newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			_spinBox.ValueChanged -= ValueChange;
			_spinBox.Value = value;
			_spinBox.ValueChanged += ValueChange;
		}
	}

	private void ValueChange(double newValue)
	{
		value = _spinBox.Value;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_spinBox.GetLineEdit().HasFocus();
	}

	public override string _GetType()
	{
		return "Float";
	}
}
