using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorString : XWInspectorPropertyEditorBase
{
	private LineEdit _lineEdit;

	public override void _Ready()
	{
		base._Ready();
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		_lineEdit.TextChanged += delegate(string v)
		{
			ValueChange(v);
		};
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_lineEdit, propertyValue.As<string>());
		}
	}

	public override Variant GetValue()
	{
		return _lineEdit.Text;
	}
}
