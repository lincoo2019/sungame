using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorRect2i : XWInspectorPropertyEditorBase
{
	private SpinBox _xSpinBox;

	private SpinBox _ySpinBox;

	private SpinBox _wSpinBox;

	private SpinBox _hSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_xSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "x", XWInspectorPropertyEditorFactory.ColorX, 1.0);
		_ySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "y", XWInspectorPropertyEditorFactory.ColorY, 1.0);
		_wSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "w", XWInspectorPropertyEditorFactory.ColorW, 1.0);
		_hSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "h", XWInspectorPropertyEditorFactory.ColorW, 1.0);
		_xSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "position:x");
		};
		_ySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "position:y");
		};
		_wSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "size:x");
		};
		_hSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange((int)v, "size:y");
		};
	}

	public override void UpdateValue()
	{
		Rect2I rect2I = GetPropertyValue().AsRect2I();
		_xSpinBox.Value = rect2I.Position.X;
		_ySpinBox.Value = rect2I.Position.Y;
		_wSpinBox.Value = rect2I.Size.X;
		_hSpinBox.Value = rect2I.Size.Y;
	}

	public override Variant GetValue()
	{
		return new Rect2I(new Vector2I((int)_xSpinBox.Value, (int)_ySpinBox.Value), new Vector2I((int)_wSpinBox.Value, (int)_hSpinBox.Value));
	}
}
