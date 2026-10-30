using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVector4i : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _zSpinBox;

	private SpinBox _wSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX, 1.0);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY, 1.0);
		_zSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "z", XWInspectorPropertyEditorFactory.ColorZ, 1.0);
		_wSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "w", XWInspectorPropertyEditorFactory.ColorW, 1.0);
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
		_wSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "w");
		};
	}

	public override void UpdateValue()
	{
		Vector4I vector4I = GetPropertyValue().AsVector4I();
		_xSpinBox.Value = vector4I.X;
		_ySpinBox.Value = vector4I.Y;
		_zSpinBox.Value = vector4I.Z;
		_wSpinBox.Value = vector4I.W;
	}

	public override Variant GetValue()
	{
		return new Vector4I((int)_xSpinBox.Value, (int)_ySpinBox.Value, (int)_zSpinBox.Value, (int)_wSpinBox.Value);
	}
}
