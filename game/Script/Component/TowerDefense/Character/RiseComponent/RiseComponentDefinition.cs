using Godot;

[GlobalClass]
[Tool]
public partial class RiseComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0.01,10,0.01")]
	public float randomDurationMin = 0.4f;

	[Export(PropertyHint.Range, "0.01,10,0.01")]
	public float randomDurationMax = 0.6f;

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float effectDelay = 0.1f;

	[Export(PropertyHint.Range, "0,8,1")]
	public int stateStartPhysicsFrames = 2;

	[Export(PropertyHint.Range, "0,64,1")]
	public float landDiscardRevealOffset = 24f;

	[Export(PropertyHint.None, "")]
	public float discardResetPosition = 10000f;

	[Export(PropertyHint.None, "")]
	public StringName discardShaderParameter = "discardDownPos";

	[Export(PropertyHint.None, "")]
	public Tween.EaseType riseEase = Tween.EaseType.Out;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType riseTransition = Tween.TransitionType.Cubic;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new RiseComponent();
	}
}
