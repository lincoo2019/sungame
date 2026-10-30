using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class GrowUpComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public Array<float> growUpTime { get; set; } = new Array<float>();

	[Export(PropertyHint.None, "")]
	public Array<float> growUpSize { get; set; } = new Array<float>();

	[Export(PropertyHint.None, "")]
	public string growAudio { get; set; } = "Grow";

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double growTweenDuration { get; set; } = 1.0;

	[Export(PropertyHint.None, "")]
	public bool instantGrowInIZM { get; set; } = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GrowUpComponent();
	}
}
