using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ProduceComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Production", "")]
	[Export(PropertyHint.None, "")]
	public bool _IZMMode { get; set; }

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,Packet,QXSun,MagicSun")]
	public string produceType { get; set; } = "Sun";

	[Export(PropertyHint.Range, "0,300,0.1,or_greater")]
	public float produceInterval { get; set; } = 25f;

	[Export(PropertyHint.None, "")]
	public int num { get; set; } = 25;

	[Export(PropertyHint.Range, "1,1000,1,or_greater")]
	public int sunOnceMax { get; set; } = 50;

	[Export(PropertyHint.None, "")]
	public Array<NodePath> markerPaths { get; set; } = new Array<NodePath>();

	[Export(PropertyHint.None, "")]
	public bool onlyEmit { get; set; }

	[Export(PropertyHint.None, "")]
	public NodePath produceGlowTargetPath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public Vector2 initialDelayRange { get; set; } = new Vector2(3f, 6f);

	[Export(PropertyHint.Range, "0,60,0.1,or_greater")]
	public float glowLeadTime { get; set; } = 1.5f;

	[Export(PropertyHint.Range, "1,1000,1")]
	public int healthProductionSegments { get; set; } = 6;

	[Export(PropertyHint.Range, "1,120,1")]
	public int maxCatchUpProductions { get; set; } = 1;

	[ExportGroup("Glow", "")]
	[Export(PropertyHint.None, "")]
	public float glowBrightness { get; set; } = 0.5f;

	[Export(PropertyHint.None, "")]
	public float glowFadeInTime { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public float glowFadeOutTime { get; set; } = 0.5f;

	[ExportGroup("Coin", "")]
	[Export(PropertyHint.None, "")]
	public bool coinRandom { get; set; } = true;

	[ExportGroup("Packet", "")]
	[Export(PropertyHint.None, "")]
	public Array<string> packetName { get; set; } = new Array<string>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ProduceComponent();
	}
}
