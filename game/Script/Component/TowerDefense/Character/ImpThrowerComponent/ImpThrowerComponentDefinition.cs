using Godot;

[GlobalClass]
[Tool]
public partial class ImpThrowerComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public double impVerticalSpeed { get; set; } = -60.0;

	[Export(PropertyHint.None, "")]
	public int landingGridMin { get; set; } = 3;

	[Export(PropertyHint.None, "")]
	public int landingGridMax { get; set; } = 5;

	[Export(PropertyHint.None, "")]
	public Tween.EaseType throwEase { get; set; } = Tween.EaseType.Out;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType throwTransition { get; set; } = Tween.TransitionType.Quad;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ImpThrowerComponent();
	}
}
