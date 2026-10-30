using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorCallable : XWInspectorPropertyEditorBase
{
	private Label _callableLabel;

	public override void _Ready()
	{
		base._Ready();
		_callableLabel = GetNode<Label>("%CallableLabel");
	}

	public override void UpdateValue()
	{
		Callable callable = GetPropertyValue().AsCallable();
		if (!callable.Equals(default(Callable)) && GodotObject.IsInstanceValid(callable.Target))
		{
			_callableLabel.Text = $"{callable.Target}.{callable.Method}";
		}
		else
		{
			_callableLabel.Text = "<invalid>";
		}
	}

	public override Variant GetValue()
	{
		return GetPropertyValue();
	}
}
