using Godot;

[GlobalClass]
public partial class PropertiesMultilineString : PropertiesBase
{
	public delegate void ValueChangedEventHandler(string value);

	private TextEdit _textEdit;

	public string value = "";

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_textEdit = GetNode<TextEdit>("%TextEdit");
		value = _textEdit.Text;
		_textEdit.TextChanged += ValueChange;
	}

	public void SetValue(string newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			if (CanSetValue())
			{
				_textEdit.TextChanged -= ValueChange;
				_textEdit.Text = value;
				_textEdit.TextChanged += ValueChange;
			}
		}
	}

	private void ValueChange()
	{
		value = _textEdit.Text;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_textEdit.HasFocus();
	}

	public override string _GetType()
	{
		return "String";
	}
}
