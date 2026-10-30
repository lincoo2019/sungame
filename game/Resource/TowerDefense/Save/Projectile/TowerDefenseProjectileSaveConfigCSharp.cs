using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName nodeName;

	[Export(PropertyHint.None, "")]
	public string configName;

	[Export(PropertyHint.None, "")]
	public Vector2 pos;

	[Export(PropertyHint.None, "")]
	public Vector2 velocity;

	[Export(PropertyHint.None, "")]
	public double speed;

	[Export(PropertyHint.None, "")]
	public int camp;

	[Export(PropertyHint.None, "")]
	public double damage;

	[Export(PropertyHint.None, "")]
	public int collisionFlags;

	[Export(PropertyHint.None, "")]
	public int damageFlags;

	[Export(PropertyHint.None, "")]
	public int fireMethodFlags;

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	[Export(PropertyHint.None, "")]
	public double height;

	[Export(PropertyHint.None, "")]
	public double z;

	[Export(PropertyHint.None, "")]
	public double groundHeight;

	[Export(PropertyHint.None, "")]
	public bool isGround;

	[Export(PropertyHint.None, "")]
	public bool over;

	[Export(PropertyHint.None, "")]
	public bool checkAll;

	[Export(PropertyHint.None, "")]
	public StringName fireCharacterName;

	[Export(PropertyHint.None, "")]
	public StringName targetName;

	[Export(PropertyHint.None, "")]
	public double catapultTime;

	[Export(PropertyHint.None, "")]
	public double catapultTimer;

	[Export(PropertyHint.None, "")]
	public Vector2 catapultTargetPos;

	[Export(PropertyHint.None, "")]
	public Vector2 catapultControlPoint;

	[Export(PropertyHint.None, "")]
	public bool catapulCheckLast;

	[Export(PropertyHint.None, "")]
	public int penetrateNum;

	[Export(PropertyHint.None, "")]
	public bool hitOver;

	[Export(PropertyHint.None, "")]
	public double checkDistance;

	[Export(PropertyHint.None, "")]
	public double fireLength;

	[Export(PropertyHint.None, "")]
	public bool trackOpen;

	[Export(PropertyHint.None, "")]
	public bool catapultOpen;

	[Export(PropertyHint.None, "")]
	public int fireDirX;

	[Export(PropertyHint.None, "")]
	public bool isShooter;

	[Export(PropertyHint.None, "")]
	public Dictionary spriteSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public bool useFall;

	[Export(PropertyHint.None, "")]
	public bool useGravity;

	[Export(PropertyHint.None, "")]
	public double ySpeed;

	[Export(PropertyHint.None, "")]
	public bool gravityUse;

	[Export(PropertyHint.None, "")]
	public double gravity;

	[Export(PropertyHint.None, "")]
	public double gravityScale;

	public void ReportUnsupportedLegacyRecord()
	{
		string value = (string.IsNullOrEmpty(configName) ? null : TowerDefenseManager.GetProjectileConfig(configName))?.projectileScene?.ResourcePath ?? "<none>";
		GD.PushError($"[BulletField:E_LEGACY_NODE_SAVE_UNSUPPORTED] projectile='{configName}' scene='{value}' reason='legacy projectileList records require the deleted Node loader'");
	}
}
