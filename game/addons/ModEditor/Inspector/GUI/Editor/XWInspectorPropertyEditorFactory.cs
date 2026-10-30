using Godot;

namespace PVZHE.ModEditor.Inspector;

internal static class XWInspectorPropertyEditorFactory
{
	public static readonly Color ColorX = new Color(0.475f, 0.235f, 0.278f);

	public static readonly Color ColorY = new Color(0.341f, 0.404f, 0.212f);

	public static readonly Color ColorZ = new Color(0.157f, 0.388f, 0.6f);

	public static readonly Color ColorW = new Color(0.412f, 0.412f, 0.412f);

	public static SpinBox CreateVectorRow(Container parent, string label, Color color, double step = 0.001)
	{
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 28f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		hBoxContainer.AddThemeConstantOverride("separation", 4);
		Label label2 = new Label
		{
			CustomMinimumSize = new Vector2(18f, 28f),
			Text = label,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		label2.AddThemeColorOverride("font_color", color);
		hBoxContainer.AddChild(label2, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			CustomMinimumSize = new Vector2(0f, 28f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MinValue = -9999999999.0,
			MaxValue = 9999999999.0,
			Step = step,
			AllowGreater = true,
			AllowLesser = true
		};
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		parent.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		XWInspectorPropertyEditorBase.NormalizeSpinBox(spinBox);
		return spinBox;
	}
}
