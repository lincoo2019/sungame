using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorBool : XWInspectorPropertyEditorBase
{
	private CheckBox _checkBox;

	public override void _Ready()
	{
		base._Ready();
		_checkBox = GetNode<CheckBox>("%CheckBox");
		_checkBox.Text = "启用";
		_checkBox.CustomMinimumSize = new Vector2(72f, 28f);
		_checkBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_checkBox.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_checkBox.Toggled += OnCheckBoxToggled;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			_checkBox.SetPressedNoSignal(propertyValue.AsBool());
		}
	}

	public override Variant GetValue()
	{
		return _checkBox.ButtonPressed;
	}

	private void OnCheckBoxToggled(bool value)
	{
		ValueChange(value);
	}
}
