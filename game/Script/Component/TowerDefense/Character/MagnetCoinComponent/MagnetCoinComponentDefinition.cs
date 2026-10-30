using Godot;

[GlobalClass]
[Tool]
public partial class MagnetCoinComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Targeting", "")]
	[Export(PropertyHint.None, "")]
	public NodePath posMarkerPath { get; set; } = new NodePath();

	[Export(PropertyHint.Range, "0,300,0.1,or_greater")]
	public float magnetTime { get; set; } = 5f;

	[Export(PropertyHint.None, "")]
	public int magnetNum { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public bool useRegistryCoinTypes { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int[] objectList { get; set; } = new int[6] { 0, 1, 2, 4, 5, 6 };

	[Export(PropertyHint.Range, "0,100,0.1,or_greater")]
	public float pullSpeed { get; set; } = 5f;

	[Export(PropertyHint.Range, "0,1000,0.1,or_greater")]
	public float collectDistance { get; set; } = 10f;

	[ExportGroup("Release", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 releaseVelocityMin { get; set; } = new Vector2(-50f, -200f);

	[Export(PropertyHint.None, "")]
	public Vector2 releaseVelocityMax { get; set; } = new Vector2(50f, -200f);

	[Export(PropertyHint.None, "")]
	public float releaseGravity { get; set; } = 980f;

	[Export(PropertyHint.None, "")]
	public string coinGroupName { get; set; } = "Coin";

	[Export(PropertyHint.None, "")]
	public string goldMagnetGroupName { get; set; } = "GoldMagnet";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new MagnetCoinComponent();
	}
}
