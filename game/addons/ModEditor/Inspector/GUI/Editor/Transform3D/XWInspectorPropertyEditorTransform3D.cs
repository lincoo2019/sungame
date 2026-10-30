using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorTransform3D : XWInspectorPropertyEditorBase
{
	private SpinBox _xxSpinBox;

	private SpinBox _yxSpinBox;

	private SpinBox _zxSpinBox;

	private SpinBox _xySpinBox;

	private SpinBox _yySpinBox;

	private SpinBox _zySpinBox;

	private SpinBox _xzSpinBox;

	private SpinBox _yzSpinBox;

	private SpinBox _zzSpinBox;

	private SpinBox _oxSpinBox;

	private SpinBox _oySpinBox;

	private SpinBox _ozSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xx", XWInspectorPropertyEditorFactory.ColorX);
		_yxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yx", XWInspectorPropertyEditorFactory.ColorY);
		_zxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "zx", XWInspectorPropertyEditorFactory.ColorZ);
		_xySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xy", XWInspectorPropertyEditorFactory.ColorX);
		_yySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yy", XWInspectorPropertyEditorFactory.ColorY);
		_zySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "zy", XWInspectorPropertyEditorFactory.ColorZ);
		_xzSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xz", XWInspectorPropertyEditorFactory.ColorX);
		_yzSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yz", XWInspectorPropertyEditorFactory.ColorY);
		_zzSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "zz", XWInspectorPropertyEditorFactory.ColorZ);
		_oxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "ox", XWInspectorPropertyEditorFactory.ColorX);
		_oySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "oy", XWInspectorPropertyEditorFactory.ColorY);
		_ozSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "oz", XWInspectorPropertyEditorFactory.ColorZ);
		_xxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:x:x");
		};
		_yxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:x:y");
		};
		_zxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:x:z");
		};
		_xySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:y:x");
		};
		_yySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:y:y");
		};
		_zySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:y:z");
		};
		_xzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:z:x");
		};
		_yzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:z:y");
		};
		_zzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "basis:z:z");
		};
		_oxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "origin:x");
		};
		_oySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "origin:y");
		};
		_ozSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "origin:z");
		};
	}

	public override void UpdateValue()
	{
		Transform3D transform3D = GetPropertyValue().AsTransform3D();
		_xxSpinBox.Value = transform3D.Basis.X.X;
		_yxSpinBox.Value = transform3D.Basis.X.Y;
		_zxSpinBox.Value = transform3D.Basis.X.Z;
		_xySpinBox.Value = transform3D.Basis.Y.X;
		_yySpinBox.Value = transform3D.Basis.Y.Y;
		_zySpinBox.Value = transform3D.Basis.Y.Z;
		_xzSpinBox.Value = transform3D.Basis.Z.X;
		_yzSpinBox.Value = transform3D.Basis.Z.Y;
		_zzSpinBox.Value = transform3D.Basis.Z.Z;
		_oxSpinBox.Value = transform3D.Origin.X;
		_oySpinBox.Value = transform3D.Origin.Y;
		_ozSpinBox.Value = transform3D.Origin.Z;
	}

	public override Variant GetValue()
	{
		Basis basis = new Basis(new Vector3((float)_xxSpinBox.Value, (float)_yxSpinBox.Value, (float)_zxSpinBox.Value), new Vector3((float)_xySpinBox.Value, (float)_yySpinBox.Value, (float)_zySpinBox.Value), new Vector3((float)_xzSpinBox.Value, (float)_yzSpinBox.Value, (float)_zzSpinBox.Value));
		Vector3 origin = new Vector3((float)_oxSpinBox.Value, (float)_oySpinBox.Value, (float)_ozSpinBox.Value);
		return new Transform3D(basis, origin);
	}
}
