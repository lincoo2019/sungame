using Godot;

[Tool]
public partial class InspectorContinuousProbeResource : Resource
{
	[Export(PropertyHint.None, "")]
	public string ProbeTitle { get; set; } = "before-title";

	[Export(PropertyHint.None, "")]
	public double ProbeSpeed { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public Vector2 ProbeOffset { get; set; } = new Vector2(3f, 4f);
}
