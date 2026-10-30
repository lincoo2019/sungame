using Godot;

[GlobalClass]
public partial class PropertiesString : PropertiesBase
{
	public delegate void ValueChangedEventHandler(string value);

	private LineEdit _lineEdit;

	public string value = "";

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		value = _lineEdit.Text;
		_lineEdit.TextChanged += ValueChange;
	}

	public void SetValue(string newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			if (CanSetValue())
			{
				_lineEdit.TextChanged -= ValueChange;
				_lineEdit.Text = value;
				_lineEdit.TextChanged += ValueChange;
			}
		}
	}

	private void ValueChange(string newText)
	{
		value = _lineEdit.Text;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_lineEdit.HasFocus();
	}

	public override string _GetType()
	{
		return "String";
	}
}
