using Godot;

[GlobalClass]
[Tool]
public partial class EnvironmentAnimeComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public StringName dayAnimation = "Day";

	[Export(PropertyHint.None, "")]
	public StringName nightAnimation = "Night";

	[Export(PropertyHint.None, "")]
	public StringName waterDayAnimation = "Water";

	[Export(PropertyHint.None, "")]
	public StringName waterNightAnimation = "WaterNight";

	[Export(PropertyHint.Range, "0,20,0.1")]
	public float bobFrequency = 2f;

	[Export(PropertyHint.Range, "0,50,0.1")]
	public float bobAmplitude = 2f;

	[Export(PropertyHint.None, "")]
	public float bobBaseY;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new EnvironmentAnimeComponent();
	}
}
