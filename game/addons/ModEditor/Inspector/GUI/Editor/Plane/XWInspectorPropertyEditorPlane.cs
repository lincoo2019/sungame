using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPlane : XWInspectorPropertyEditorBase
{
	private SpinBox _nxSpinBox;

	private SpinBox _nySpinBox;

	private SpinBox _nzSpinBox;

	private SpinBox _dSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_nxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "nx", XWInspectorPropertyEditorFactory.ColorX);
		_nySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "ny", XWInspectorPropertyEditorFactory.ColorY);
		_nzSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "nz", XWInspectorPropertyEditorFactory.ColorZ);
		_dSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "d", XWInspectorPropertyEditorFactory.ColorW);
		_nxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "normal:x");
		};
		_nySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "normal:y");
		};
		_nzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "normal:z");
		};
		_dSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "d");
		};
	}

	public override void UpdateValue()
	{
		Plane plane = GetPropertyValue().AsPlane();
		_nxSpinBox.Value = plane.Normal.X;
		_nySpinBox.Value = plane.Normal.Y;
		_nzSpinBox.Value = plane.Normal.Z;
		_dSpinBox.Value = plane.D;
	}

	public override Variant GetValue()
	{
		return new Plane((float)_nxSpinBox.Value, (float)_nySpinBox.Value, (float)_nzSpinBox.Value, (float)_dSpinBox.Value);
	}
}
