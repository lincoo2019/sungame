using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileData : Resource
{
	[Export(PropertyHint.None, "")]
	public string name = "";

	[Export(PropertyHint.None, "")]
	public double baseDamage = 20.0;

	[Export(PropertyHint.None, "")]
	public Vector2 size = new Vector2(28f, 28f);

	[Export(PropertyHint.None, "")]
	public Vector2 scale = new Vector2(1f, 1f);

	[Export(PropertyHint.None, "")]
	public PackedScene projectileScene;

	[Export(PropertyHint.Enum, "Particles,Sprite")]
	public string splatSceneType = "Particles";

	[Export(PropertyHint.None, "")]
	public string splatAudio = "SplatNormal";

	[Export(PropertyHint.None, "")]
	public PackedScene splatScene;

	[Export(PropertyHint.None, "")]
	public PackedScene hitEffect;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitTargetEventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitCharacterEventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitGroundEventList = new Array<TowerDefenseCharacterEventBase>();

	[ExportGroup("Init", "")]
	[Export(PropertyHint.None, "")]
	public double blockHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public bool rotateFollowVelocity;

	[Export(PropertyHint.None, "")]
	public double rotateScale;

	[Export(PropertyHint.None, "")]
	public bool hitBody;

	[ExportGroup("Range", "")]
	[Export(PropertyHint.Enum, "Default,Bomb")]
	public string rangeType = "Default";

	[Export(PropertyHint.None, "")]
	public bool useRange;

	[Export(PropertyHint.None, "")]
	public Vector2 rangeSize = new Vector2(0.5f, 0.5f);

	[Export(PropertyHint.None, "")]
	public double hitPesontage = 0.25;

	[ExportGroup("Flag", "")]
	[Export(PropertyHint.None, "")]
	public bool isFire;

	[Export(PropertyHint.None, "")]
	public bool isStar;

	[Export(PropertyHint.None, "")]
	public bool isMagic;
}
