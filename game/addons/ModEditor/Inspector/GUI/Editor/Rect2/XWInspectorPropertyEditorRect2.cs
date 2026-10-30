using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorRect2 : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _wSpinBox;

	private SpinBox _hSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY);
		_wSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "w", XWInspectorPropertyEditorFactory.ColorW);
		_hSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "h", XWInspectorPropertyEditorFactory.ColorW);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "position:x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "position:y");
		};
		_wSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "size:x");
		};
		_hSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "size:y");
		};
	}

	public override void UpdateValue()
	{
		Rect2 rect = GetPropertyValue().AsRect2();
		_xSpinBox.Value = rect.Position.X;
		_ySpinBox.Value = rect.Position.Y;
		_wSpinBox.Value = rect.Size.X;
		_hSpinBox.Value = rect.Size.Y;
	}

	public override Variant GetValue()
	{
		return new Rect2(new Vector2((float)_xSpinBox.Value, (float)_ySpinBox.Value), new Vector2((float)_wSpinBox.Value, (float)_hSpinBox.Value));
	}
}
