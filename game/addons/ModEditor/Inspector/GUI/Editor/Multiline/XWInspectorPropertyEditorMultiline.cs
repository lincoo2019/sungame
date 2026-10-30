using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorMultiline : XWInspectorPropertyEditorBase
{
	private TextEdit _textEdit;

	public override void _Ready()
	{
		base._Ready();
		_textEdit = GetNode<TextEdit>("%TextEdit");
		_textEdit.TextChanged += OnTextChanged;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			XWInspectorPropertyEditorBase.SetTextEditTextPreservingCaret(_textEdit, propertyValue.As<string>());
		}
	}

	public override Variant GetValue()
	{
		return _textEdit.Text;
	}

	private void OnTextChanged()
	{
		ValueChange(_textEdit.Text);
	}
}
