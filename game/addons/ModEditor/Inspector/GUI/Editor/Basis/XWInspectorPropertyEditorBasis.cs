using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorBasis : XWInspectorPropertyEditorBase
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
		_xxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x:x");
		};
		_yxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x:y");
		};
		_zxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x:z");
		};
		_xySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y:x");
		};
		_yySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y:y");
		};
		_zySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y:z");
		};
		_xzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "z:x");
		};
		_yzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "z:y");
		};
		_zzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "z:z");
		};
	}

	public override void UpdateValue()
	{
		Basis basis = GetPropertyValue().AsBasis();
		_xxSpinBox.Value = basis.X.X;
		_yxSpinBox.Value = basis.X.Y;
		_zxSpinBox.Value = basis.X.Z;
		_xySpinBox.Value = basis.Y.X;
		_yySpinBox.Value = basis.Y.Y;
		_zySpinBox.Value = basis.Y.Z;
		_xzSpinBox.Value = basis.Z.X;
		_yzSpinBox.Value = basis.Z.Y;
		_zzSpinBox.Value = basis.Z.Z;
	}

	public override Variant GetValue()
	{
		return new Basis(new Vector3((float)_xxSpinBox.Value, (float)_yxSpinBox.Value, (float)_zxSpinBox.Value), new Vector3((float)_xySpinBox.Value, (float)_yySpinBox.Value, (float)_zySpinBox.Value), new Vector3((float)_xzSpinBox.Value, (float)_yzSpinBox.Value, (float)_zzSpinBox.Value));
	}
}
