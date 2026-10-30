using Godot;

[GlobalClass]
[Tool]
public partial class MousePressComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public AabbShape2DResource clickShape;

	[Export(PropertyHint.None, "")]
	public NodePath toggleTargetPath = new NodePath();

	[ExportGroup("Input", "")]
	[Export(PropertyHint.None, "")]
	public StringName pressAction = "Press";

	[Export(PropertyHint.None, "")]
	public StringName releaseAction = "Release";

	[Export(PropertyHint.None, "")]
	public bool toggleMode;

	[Export(PropertyHint.None, "")]
	public bool enableDoublePress = true;

	[Export(PropertyHint.None, "")]
	public bool followPointerWhileAiming = true;

	[Export(PropertyHint.None, "")]
	public bool hostAuthorityOnly = true;

	[ExportGroup("Timing", "")]
	[Export(PropertyHint.Range, "0,2,0.01")]
	public float doublePressWindow = 0.25f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float interactionCooldown = 0.1f;

	[ExportGroup("Presentation", "")]
	[Export(PropertyHint.Range, "0,2,0.01")]
	public float hoverBrightness = 0.3f;

	[Export(PropertyHint.None, "")]
	public StringName brightnessShaderParameter = "brightStrength";

	[Export(PropertyHint.None, "")]
	public Control.CursorShape enabledCursorShape = Control.CursorShape.PointingHand;

	[Export(PropertyHint.None, "")]
	public Control.CursorShape disabledCursorShape;

	[ExportGroup("Aiming", "")]
	[Export(PropertyHint.None, "")]
	public bool requireGroundForFinish = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new MousePressComponent();
	}
}
