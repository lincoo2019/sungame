using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVector2 : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y");
		};
	}

	public override void UpdateValue()
	{
		Vector2 vector = GetPropertyValue().AsVector2();
		_xSpinBox.Value = vector.X;
		_ySpinBox.Value = vector.Y;
	}

	public override Variant GetValue()
	{
		return new Vector2((float)_xSpinBox.Value, (float)_ySpinBox.Value);
	}
}
