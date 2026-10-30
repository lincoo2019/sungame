using Godot;

[GlobalClass]
[Tool]
public partial class SwimComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,1,0.01")]
	public double animationBlend { get; set; } = 0.2;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double waterLoopBlend { get; set; }

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double outWaterWalkBlend { get; set; }

	[Export(PropertyHint.Range, "0,10,0.1")]
	public float offscreenSpeedMultiplier { get; set; } = 2f;

	[Export(PropertyHint.Range, "-100,100,0.5")]
	public float outWaterHorizontalOffset { get; set; } = 5f;

	[Export(PropertyHint.None, "")]
	public string underwaterEntryAudio { get; set; } = "ZombieEnteringWater";

	[Export(PropertyHint.None, "")]
	public string surfaceEntryAudio { get; set; } = "PlantWater";

	[Export(PropertyHint.None, "")]
	public bool createSplashOnEntryAnimation { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool updateGroundHeightOnEntry { get; set; } = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new SwimComponent();
	}
}
