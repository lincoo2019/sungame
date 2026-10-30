using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BloverComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public double blowTime { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public double blowLength { get; set; } = 0.5;

	[Export(PropertyHint.None, "")]
	public double blowPhysiqueHugeLength { get; set; } = 0.5;

	[Export(PropertyHint.None, "")]
	public double blowAirCharacterLength { get; set; } = 0.5;

	[Export(PropertyHint.None, "")]
	public bool blowAirCharacterOut { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool checkLine { get; set; }

	[Export(PropertyHint.None, "")]
	public bool checkCollision { get; set; }

	[Export(PropertyHint.None, "")]
	public bool checkPhysiqueHuge { get; set; }

	[Export(PropertyHint.None, "")]
	public string blowAudio { get; set; } = "Blover";

	[ExportSubgroup("Projectile", "")]
	[Export(PropertyHint.Range, "0,100,1")]
	public int projectileRowNum { get; set; } = 20;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float projectileBatchInterval { get; set; } = 0.1f;

	[Export(PropertyHint.None, "")]
	public Vector2 projectileHeightOffsetRange { get; set; } = new Vector2(10f, 60f);

	[Export(PropertyHint.None, "")]
	public Vector2 projectileSpeedRange { get; set; } = new Vector2(400f, 800f);

	[Export(PropertyHint.None, "")]
	public float projectileLeftSpawnX { get; set; } = -50f;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseProjectileCreateData> projectileDataList { get; set; } = new Array<TowerDefenseProjectileCreateData>();

	[ExportSubgroup("Event", "")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BloverComponent();
	}
}
