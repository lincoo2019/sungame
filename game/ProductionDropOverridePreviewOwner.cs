using Godot;

public partial class ProductionDropOverridePreviewOwner : Node2D
{
	[Export(PropertyHint.None, "")]
	public string produceType { get; set; } = "QXSun";

	[Export(PropertyHint.None, "")]
	public int sunNum { get; set; } = 75;

	[Export(PropertyHint.None, "")]
	public double produceInterval { get; set; } = 18.0;
}
