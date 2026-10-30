using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVector3i : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _zSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX, 1.0);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY, 1.0);
		_zSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "z", XWInspectorPropertyEditorFactory.ColorZ, 1.0);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "y");
		};
		_zSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "z");
		};
	}

	public override void UpdateValue()
	{
		Vector3I vector3I = GetPropertyValue().AsVector3I();
		_xSpinBox.Value = vector3I.X;
		_ySpinBox.Value = vector3I.Y;
		_zSpinBox.Value = vector3I.Z;
	}

	public override Variant GetValue()
	{
		return new Vector3I((int)_xSpinBox.Value, (int)_ySpinBox.Value, (int)_zSpinBox.Value);
	}
}
