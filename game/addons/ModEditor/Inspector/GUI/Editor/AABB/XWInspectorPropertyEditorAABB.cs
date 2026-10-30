using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorAABB : XWInspectorPropertyEditorBase
{
	private SpinBox _pxSpinBox;

	private SpinBox _pySpinBox;

	private SpinBox _pzSpinBox;

	private SpinBox _sxSpinBox;

	private SpinBox _sySpinBox;

	private SpinBox _szSpinBox;

	public override void _Ready()
	{
		base._Ready();
		VBoxContainer node = GetNode<VBoxContainer>("%EditorContainer");
		_pxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "px", XWInspectorPropertyEditorFactory.ColorX);
		_pySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "py", XWInspectorPropertyEditorFactory.ColorY);
		_pzSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "pz", XWInspectorPropertyEditorFactory.ColorZ);
		_sxSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "sx", XWInspectorPropertyEditorFactory.ColorX);
		_sySpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "sy", XWInspectorPropertyEditorFactory.ColorY);
		_szSpinBox = XWInspectorPropertyEditorFactory.CreateVectorRow(node, "sz", XWInspectorPropertyEditorFactory.ColorZ);
		_pxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "position:x");
		};
		_pySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "position:y");
		};
		_pzSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "position:z");
		};
		_sxSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "size:x");
		};
		_sySpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "size:y");
		};
		_szSpinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v, "size:z");
		};
	}

	public override void UpdateValue()
	{
		Aabb aabb = GetPropertyValue().AsAabb();
		_pxSpinBox.Value = aabb.Position.X;
		_pySpinBox.Value = aabb.Position.Y;
		_pzSpinBox.Value = aabb.Position.Z;
		_sxSpinBox.Value = aabb.Size.X;
		_sySpinBox.Value = aabb.Size.Y;
		_szSpinBox.Value = aabb.Size.Z;
	}

	public override Variant GetValue()
	{
		return new Aabb(new Vector3((float)_pxSpinBox.Value, (float)_pySpinBox.Value, (float)_pzSpinBox.Value), new Vector3((float)_sxSpinBox.Value, (float)_sySpinBox.Value, (float)_szSpinBox.Value));
	}
}
