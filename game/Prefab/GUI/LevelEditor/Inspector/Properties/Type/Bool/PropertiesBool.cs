using Godot;

[GlobalClass]
public partial class PropertiesBool : PropertiesBase
{
	public delegate void ValueChangedEventHandler(bool value);

	private CheckBox _checkbox;

	public bool value;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_checkbox = GetNode<CheckBox>("%Checkbox");
		value = _checkbox.ButtonPressed;
		_checkbox.Toggled += ValueChange;
	}

	public void SetValue(bool newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			_checkbox.Toggled -= ValueChange;
			_checkbox.ButtonPressed = value;
			_checkbox.Toggled += ValueChange;
		}
	}

	private void ValueChange(bool toggledOn)
	{
		if (IsNodeReady())
		{
			value = _checkbox.ButtonPressed;
			this.OnValueChanged?.Invoke(value);
		}
	}

	public override bool CanSetValue()
	{
		return !_checkbox.HasFocus();
	}

	public override string _GetType()
	{
		return "Bool";
	}
}
