using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorAny : XWInspectorPropertyEditorBase
{
	private Variant _value;

	public override void UpdateValue()
	{
		_value = GetPropertyValue();
	}

	public override Variant GetValue()
	{
		return _value;
	}
}
