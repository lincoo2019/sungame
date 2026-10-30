using Godot;
using Godot.Collections;

public partial class XWDirectPropertyProbeResource : Resource
{
	[ExportGroup("Identity", "")]
	[Export(PropertyHint.None, "")]
	public string Caption { get; set; } = "Original caption";

	[Export(PropertyHint.None, "")]
	public bool Enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int Count { get; set; } = 3;

	[Export(PropertyHint.None, "")]
	public float Speed { get; set; } = 1.25f;

	[Export(PropertyHint.Enum, "Idle,Walk,Attack")]
	public int SmallMode { get; set; } = 1;

	[Export(PropertyHint.Enum, "Pea,Sunflower,Wallnut,Cherry,Potato,SnowPea,Chomper,Repeater")]
	public int LargeMode { get; set; } = 2;

	[ExportGroup("Visual", "")]
	[Export(PropertyHint.None, "")]
	public Color Tint { get; set; } = new Color(0.4f, 0.8f, 0.3f);

	[Export(PropertyHint.None, "")]
	public Vector2 Offset { get; set; } = new Vector2(8f, 12f);

	[Export(PropertyHint.None, "")]
	public Texture2D Icon { get; set; }

	[ExportGroup("Collections", "")]
	[Export(PropertyHint.None, "")]
	public Array<string> Tags { get; set; } = new Array<string> { "plant", "probe" };

	[Export(PropertyHint.None, "")]
	public Dictionary<string, int> Costs { get; set; } = new Dictionary<string, int> { ["sun"] = 50 };
}
