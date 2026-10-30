using Godot;

[GlobalClass]
[Tool]
public partial class SleepComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public PackedScene sleepIndicatorScene;

	[Export(PropertyHint.None, "")]
	public Vector2 sleepIndicatorPosition = new Vector2(20f, 25f);

	[Export(PropertyHint.None, "")]
	public Vector2 sleepIndicatorHeadPosition = new Vector2(32f, -28f);

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double sleepAnimationBlend = 0.2;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float wakeSquashAmount = 0.25f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float wakeStretchAmount = 0.1f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float wakeStepDuration = 0.25f;

	[Export(PropertyHint.None, "")]
	public Tween.EaseType wakeEase = Tween.EaseType.Out;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType wakeTransition = Tween.TransitionType.Quart;

	[Export(PropertyHint.None, "")]
	public bool freezeTimeInIZM = true;

	[Export(PropertyHint.None, "")]
	public string sleepBuffName = "Sleep";

	[Export(PropertyHint.None, "")]
	public string neverSleepValue = "Never";

	[Export(PropertyHint.None, "")]
	public string daySleepValue = "Day";

	[Export(PropertyHint.None, "")]
	public string nightSleepValue = "Night";

	[Export(PropertyHint.None, "")]
	public bool mapRulesPreventSleep = true;

	[Export(PropertyHint.None, "")]
	public bool coffeePreventsSleep = true;

	[Export(PropertyHint.None, "")]
	public bool matchingCellElementPreventsSleep = true;

	[Export(PropertyHint.None, "")]
	public bool hostAuthoritativeState = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new SleepComponent();
	}
}
