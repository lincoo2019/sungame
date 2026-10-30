using Godot;

[GlobalClass]
[Tool]
public partial class MowerHitComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public PackedScene mowerHitScene;

	[Export(PropertyHint.None, "")]
	public PackedScene mowerPuffScene;

	[Export(PropertyHint.Range, "0,1000,1")]
	public float startupVelocity = 100f;

	[Export(PropertyHint.Range, "0,1000,1")]
	public float runVelocity = 200f;

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float accelerationDelay = 0.25f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double attackAnimationBlend = 0.1;

	[Export(PropertyHint.None, "")]
	public double hugeTargetDamage = 100000.0;

	[Export(PropertyHint.None, "")]
	public double terminalTargetDamage = 1000000000.0;

	[Export(PropertyHint.Range, "-1,1000,1")]
	public int maxEffectCount = 100;

	[Export(PropertyHint.None, "")]
	public bool createPuffEffect = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreGravestones = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreCraters = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreBowlingPlants = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreSoccerBalls = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreHypnotizedZombies = true;

	[Export(PropertyHint.None, "")]
	public bool ignoreBossZombies = true;

	[Export(PropertyHint.None, "")]
	public bool allowJumpingPolestriker = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new MowerHitComponent();
	}
}
