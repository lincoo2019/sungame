using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVector3 : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _zSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY);
		_zSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "z", XWInspectorPropertyEditorFactory.ColorZ);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y");
		};
		_zSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "z");
		};
	}

	public override void UpdateValue()
	{
		Vector3 vector = GetPropertyValue().AsVector3();
		_xSpinBox.Value = vector.X;
		_ySpinBox.Value = vector.Y;
		_zSpinBox.Value = vector.Z;
	}

	public override Variant GetValue()
	{
		return new Vector3((float)_xSpinBox.Value, (float)_ySpinBox.Value, (float)_zSpinBox.Value);
	}
}
