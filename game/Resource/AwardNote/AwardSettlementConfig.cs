using Godot;

[GlobalClass]
public partial class AwardSettlementConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Texture2D texture;

	[Export(PropertyHint.None, "")]
	public Texture2D background;

	[Export(PropertyHint.None, "")]
	public Texture2D image;
}
