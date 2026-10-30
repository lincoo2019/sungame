using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVector2i : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX, 1.0);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY, 1.0);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "y");
		};
	}

	public override void UpdateValue()
	{
		Vector2I vector2I = GetPropertyValue().AsVector2I();
		_xSpinBox.Value = vector2I.X;
		_ySpinBox.Value = vector2I.Y;
	}

	public override Variant GetValue()
	{
		return new Vector2I((int)_xSpinBox.Value, (int)_ySpinBox.Value);
	}
}
