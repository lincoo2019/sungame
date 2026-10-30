using Godot;
using Godot.Collections;

[Tool]
public partial class InspectorNestedContinuousProbeResource : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<string> ProbeNames { get; set; } = new Array<string> { "alpha", "beta" };

	[Export(PropertyHint.None, "")]
	public Dictionary ProbeWeights { get; set; } = CreateTypedWeights();

	private static Dictionary CreateTypedWeights()
	{
		return (Dictionary?)new Dictionary<string, int>
		{
			{ "pea", 3 },
			{ "sun", 5 }
		};
	}
}
