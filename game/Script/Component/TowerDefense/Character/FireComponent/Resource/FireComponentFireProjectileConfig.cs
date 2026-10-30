using Godot;

[GlobalClass]
public partial class FireComponentFireProjectileConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public int checkProjectileId;

	[Export(PropertyHint.None, "")]
	public int firePosId;

	[Export(PropertyHint.None, "")]
	public float speed = 300f;

	[Export(PropertyHint.None, "")]
	public float dir;

	[Export(PropertyHint.None, "")]
	public int offsetLine;

	[Export(PropertyHint.None, "")]
	public int fireNumSkip = -1;

	[Export(PropertyHint.None, "")]
	public string fireEventNeed = "";

	[Export(PropertyHint.None, "")]
	public bool projectileFlip;
}
