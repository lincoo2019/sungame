using Godot;

[GlobalClass]
[Tool]
public partial class GroundHeightComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public float interpolationSpeed = 3f;

	[Export(PropertyHint.None, "")]
	public float threshold = 0.1f;

	[Export(PropertyHint.None, "")]
	public float waterHeight = 25f;

	[Export(PropertyHint.None, "")]
	public float ladderHeight = 60f;

	[Export(PropertyHint.None, "")]
	public bool handleWaterHeight;

	[Export(PropertyHint.None, "")]
	public bool handleLadder;

	[Export(PropertyHint.None, "")]
	public bool detectWater;

	[Export(PropertyHint.None, "")]
	public bool detectLadder;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GroundHeightComponent();
	}
}
