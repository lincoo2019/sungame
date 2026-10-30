using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorQuaternion : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _zSpinBox;

	private SpinBox _wSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY);
		_zSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "z", XWInspectorPropertyEditorFactory.ColorZ);
		_wSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "w", XWInspectorPropertyEditorFactory.ColorW);
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
		_wSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "w");
		};
	}

	public override void UpdateValue()
	{
		Quaternion quaternion = GetPropertyValue().AsQuaternion();
		_xSpinBox.Value = quaternion.X;
		_ySpinBox.Value = quaternion.Y;
		_zSpinBox.Value = quaternion.Z;
		_wSpinBox.Value = quaternion.W;
	}

	public override Variant GetValue()
	{
		return new Quaternion((float)_xSpinBox.Value, (float)_ySpinBox.Value, (float)_zSpinBox.Value, (float)_wSpinBox.Value);
	}
}
