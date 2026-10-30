using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorProjection : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Projection/XWInspectorPropertyEditorProjection.tscn";

	private readonly SpinBox[] _spinBoxes = new SpinBox[16];

	public static XWInspectorPropertyEditorProjection Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Projection/XWInspectorPropertyEditorProjection.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorProjection>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		GridContainer node = GetNode<GridContainer>("%EditorContainer");
		node.Columns = 4;
		for (int i = 0; i < 16; i++)
		{
			SpinBox spinBox = new SpinBox
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MinValue = -9999999999.0,
				MaxValue = 9999999999.0,
				Step = 0.001,
				AllowGreater = true,
				AllowLesser = true
			};
			node.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			XWInspectorPropertyEditorBase.NormalizeSpinBox(spinBox);
			spinBox.ValueChanged += delegate
			{
				OnSpinBoxChanged();
			};
			_spinBoxes[i] = spinBox;
		}
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Projection)
		{
			Projection projection = propertyValue.AsProjection();
			for (int i = 0; i < 4; i++)
			{
				Vector4 vector = projection[i];
				_spinBoxes[i * 4].SetValueNoSignal(vector.X);
				_spinBoxes[i * 4 + 1].SetValueNoSignal(vector.Y);
				_spinBoxes[i * 4 + 2].SetValueNoSignal(vector.Z);
				_spinBoxes[i * 4 + 3].SetValueNoSignal(vector.W);
			}
		}
	}

	public override Variant GetValue()
	{
		Projection projection = default(Projection);
		for (int i = 0; i < 4; i++)
		{
			projection[i] = new Vector4((float)_spinBoxes[i * 4].Value, (float)_spinBoxes[i * 4 + 1].Value, (float)_spinBoxes[i * 4 + 2].Value, (float)_spinBoxes[i * 4 + 3].Value);
		}
		return projection;
	}

	private void OnSpinBoxChanged()
	{
		ValueChange(GetValue());
	}
}
