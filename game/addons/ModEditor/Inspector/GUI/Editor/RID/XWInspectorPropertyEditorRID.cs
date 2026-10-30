using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorRID : XWInspectorPropertyEditorBase
{
	private Label _ridLabel;

	public override void _Ready()
	{
		base._Ready();
		_ridLabel = GetNode<Label>("%RIDLabel");
	}

	public override void UpdateValue()
	{
		Rid rid = GetPropertyValue().AsRid();
		_ridLabel.Text = rid.Id.ToString();
	}

	public override Variant GetValue()
	{
		return GetPropertyValue();
	}
}
