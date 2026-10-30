using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorTransform2D : XWInspectorPropertyEditorBase
{
	private SpinBox _xxSpinBox;

	private SpinBox _yxSpinBox;

	private SpinBox _xySpinBox;

	private SpinBox _yySpinBox;

	private SpinBox _xoSpinBox;

	private SpinBox _yoSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xx", XWInspectorPropertyEditorFactory.ColorX);
		_yxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yx", XWInspectorPropertyEditorFactory.ColorY);
		_xySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xy", XWInspectorPropertyEditorFactory.ColorX);
		_yySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yy", XWInspectorPropertyEditorFactory.ColorY);
		_xoSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "xo", XWInspectorPropertyEditorFactory.ColorX);
		_yoSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "yo", XWInspectorPropertyEditorFactory.ColorY);
		_xxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x:x");
		};
		_yxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "x:y");
		};
		_xySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y:x");
		};
		_yySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "y:y");
		};
		_xoSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "origin:x");
		};
		_yoSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "origin:y");
		};
	}

	public override void UpdateValue()
	{
		Transform2D transform2D = GetPropertyValue().AsTransform2D();
		_xxSpinBox.Value = transform2D.X.X;
		_yxSpinBox.Value = transform2D.X.Y;
		_xySpinBox.Value = transform2D.Y.X;
		_yySpinBox.Value = transform2D.Y.Y;
		_xoSpinBox.Value = transform2D.Origin.X;
		_yoSpinBox.Value = transform2D.Origin.Y;
	}

	public override Variant GetValue()
	{
		return new Transform2D(new Vector2((float)_xxSpinBox.Value, (float)_yxSpinBox.Value), new Vector2((float)_xySpinBox.Value, (float)_yySpinBox.Value), new Vector2((float)_xoSpinBox.Value, (float)_yoSpinBox.Value));
	}
}
