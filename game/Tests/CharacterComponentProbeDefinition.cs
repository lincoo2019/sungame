using Godot;
using Godot.Collections;

[Tool]
public partial class CharacterComponentProbeDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,99,1")]
	public int ProbePower { get; set; } = 17;

	[Export(PropertyHint.None, "")]
	public CharacterComponentProbeMode ProbeMode { get; set; } = CharacterComponentProbeMode.Armed;

	[Export(PropertyHint.None, "")]
	public Array<string> ProbeTags { get; set; } = new Array<string> { "alpha", "beta" };

	[Export(PropertyHint.None, "")]
	public Dictionary<string, int> ProbeWeights { get; set; } = new Dictionary<string, int> { ["initial"] = 7 };

	[Export(PropertyHint.None, "")]
	public int[] ProbePackedFrames { get; set; } = new int[3] { 2, 4, 8 };

	[Export(PropertyHint.None, "")]
	public Vector2 ProbeOffset { get; set; } = new Vector2(12.5f, -7.25f);

	[Export(PropertyHint.None, "")]
	public Color ProbeTint { get; set; } = new Color(0.2f, 0.4f, 0.8f, 0.65f);

	public static int RuntimeCreateCount { get; private set; }

	public static CharacterComponentProbeRuntime LastRuntime { get; private set; }

	public override CharacterComponentRuntime CreateRuntime()
	{
		RuntimeCreateCount++;
		LastRuntime = new CharacterComponentProbeRuntime();
		return LastRuntime;
	}
}
