using Godot;

[GlobalClass]
public partial class PropertiesInt : PropertiesBase
{
	public delegate void ValueChangedEventHandler(int value);

	private SpinBox _spinBox;

	public int value;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_spinBox = GetNode<SpinBox>("%SpinBox");
		value = (int)_spinBox.Value;
		_spinBox.ValueChanged += ValueChange;
	}

	public void SetValue(int newValue)
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
		value = (int)_spinBox.Value;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_spinBox.GetLineEdit().HasFocus();
	}

	public override string _GetType()
	{
		return "Int";
	}
}
