using Godot;

[GlobalClass]
[Tool]
public partial class WaterInteractionComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool inWaterLine;

	[Export(PropertyHint.None, "")]
	public bool handleDuckytobe;

	[Export(PropertyHint.None, "")]
	public float discardOffsetIn = 36f;

	[Export(PropertyHint.None, "")]
	public float discardOffsetOut = 56f;

	[Export(PropertyHint.None, "")]
	public float discardOffsetOutTarget = 86f;

	[Export(PropertyHint.Range, "0,10,0.01")]
	public float exitDuration = 1f;

	[Export(PropertyHint.None, "")]
	public float discardResetPosition = 10000f;

	[Export(PropertyHint.None, "")]
	public StringName discardShaderParameter = "discardDownPos";

	[Export(PropertyHint.None, "")]
	public bool createSplashOnEntry = true;

	[Export(PropertyHint.None, "")]
	public bool monitorScaleChanges = true;

	[Export(PropertyHint.Range, "0.0001,0.1,0.0001")]
	public float scaleChangeEpsilon = 0.001f;

	[Export(PropertyHint.None, "")]
	public Tween.EaseType exitEase = Tween.EaseType.Out;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType exitTransition = Tween.TransitionType.Cubic;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new WaterInteractionComponent();
	}
}
