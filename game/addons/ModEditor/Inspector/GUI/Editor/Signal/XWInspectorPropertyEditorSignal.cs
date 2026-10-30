using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorSignal : XWInspectorPropertyEditorBase
{
	private Label _signalLabel;

	public override void _Ready()
	{
		base._Ready();
		_signalLabel = GetNode<Label>("%SignalLabel");
	}

	public override void UpdateValue()
	{
		Signal signal = GetPropertyValue().AsSignal();
		if (signal.Name != null && !string.IsNullOrEmpty(signal.Name.ToString()))
		{
			_signalLabel.Text = signal.ToString();
		}
		else
		{
			_signalLabel.Text = "<invalid>";
		}
	}

	public override Variant GetValue()
	{
		return GetPropertyValue();
	}
}
