using Godot;

[GlobalClass]
public partial class TowerDefenseBackgroundMusicConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string translate = "";

	[Export(PropertyHint.None, "")]
	public string entry;

	[Export(PropertyHint.None, "")]
	public string flag1;

	[Export(PropertyHint.None, "")]
	public string drums;

	[Export(PropertyHint.None, "")]
	public string win;

	[Export(PropertyHint.Range, "0,100,1")]
	public int drumsZombieThreshold = 10;

	[Export(PropertyHint.Range, "0.01,20.0,0.01")]
	public float drumsFadeSpeed = 1f;

	[Export(PropertyHint.Range, "0.05,5.0,0.05")]
	public float drumsCheckInterval = 0.25f;
}
