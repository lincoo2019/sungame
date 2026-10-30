using Godot;

[GlobalClass]
[Tool]
public partial class ZombieDeathComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public string dropFeatureName = "Coins";

	[Export(PropertyHint.None, "")]
	public float dropVelocityMinX = -50f;

	[Export(PropertyHint.None, "")]
	public float dropVelocityMaxX = 50f;

	[Export(PropertyHint.None, "")]
	public float dropVelocityY = -400f;

	[Export(PropertyHint.None, "")]
	public float dropGravity = 980f;

	[Export(PropertyHint.Range, "0.01,5,0.01")]
	public double fadeDuration = 0.5;

	[Export(PropertyHint.Range, "0.01,5,0.01")]
	public double waterSinkDuration = 1.0;

	[Export(PropertyHint.None, "")]
	public double waterSinkGroundHeight = -100.0;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double animationBlend = 0.2;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ZombieDeathComponent();
	}
}
