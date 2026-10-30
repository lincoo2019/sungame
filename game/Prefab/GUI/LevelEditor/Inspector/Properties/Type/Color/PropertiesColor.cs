using Godot;

[GlobalClass]
public partial class PropertiesColor : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Color value);

	private ColorPickerButton _colorPickerButton;

	public Color value = Colors.Black;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_colorPickerButton = GetNode<ColorPickerButton>("%ColorPickerButton");
		value = _colorPickerButton.Color;
		_colorPickerButton.ColorChanged += ValueChange;
	}

	public void SetValue(Color newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			_colorPickerButton.ColorChanged -= ValueChange;
			_colorPickerButton.Color = value;
			_colorPickerButton.ColorChanged += ValueChange;
		}
	}

	private void ValueChange(Color color)
	{
		value = _colorPickerButton.Color;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_colorPickerButton.HasFocus();
	}

	public override string _GetType()
	{
		return "Color";
	}
}
