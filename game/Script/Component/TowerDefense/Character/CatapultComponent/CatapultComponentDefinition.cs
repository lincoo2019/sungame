using Godot;

[GlobalClass]
[Tool]
public partial class CatapultComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Movement", "")]
	[Export(PropertyHint.None, "")]
	public double speed = 50.0;

	[Export(PropertyHint.None, "")]
	public double damageSpeedReduction = 40.0;

	[Export(PropertyHint.None, "")]
	public double lowHealthThreshold = 0.2;

	[Export(PropertyHint.None, "")]
	public double lowHealthDeceleration = 1.0;

	[Export(PropertyHint.None, "")]
	public double lowHealthMinSpeed = 5.0;

	[Export(PropertyHint.None, "")]
	public bool lowHealthShake = true;

	[Export(PropertyHint.None, "")]
	public double outsideMapSpeedMultiplier = 2.0;

	[Export(PropertyHint.None, "")]
	public double idleBoundaryOffsetRatio = 0.5;

	[ExportGroup("Fire", "")]
	[Export(PropertyHint.None, "")]
	public double fireInterval = 3.0;

	[Export(PropertyHint.None, "")]
	public int fireNum = 1;

	[Export(PropertyHint.None, "")]
	public int projectileNum = 20;

	[Export(PropertyHint.None, "")]
	public string projectileName = "Basketball";

	[Export(PropertyHint.None, "")]
	public double fireSpeedMultiplier = 3.0;

	[Export(PropertyHint.None, "")]
	public string fireAudioName = "Basketball";

	[Export(PropertyHint.None, "")]
	public bool useCanFireCheck = true;

	[Export(PropertyHint.None, "")]
	public bool refreshInFireEntered = true;

	[Export(PropertyHint.None, "")]
	public bool hostAuthoritativeRuntime = true;

	[Export(PropertyHint.None, "")]
	public StringName fireStateEvent = "ToFire";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent = "ToIdle";

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string walkAnimeClip = "Walk";

	[Export(PropertyHint.None, "")]
	public string fireAnimeClip = "Fire";

	[Export(PropertyHint.None, "")]
	public double walkAnimationSpeedMultiplier = 0.5;

	[Export(PropertyHint.None, "")]
	public double fireAnimationBaseSpeed = 1.75;

	[Export(PropertyHint.None, "")]
	public double fireAnimationIntervalOffset = 0.25;

	[Export(PropertyHint.None, "")]
	public double fireAnimationStartScale = 1.0 / 30.0;

	[Export(PropertyHint.None, "")]
	public double fireAnimationStartIntervalOffset = 4.5;

	[Export(PropertyHint.None, "")]
	public double repeatFireAnimationStart = 0.1;

	[ExportGroup("Damage", "")]
	[Export(PropertyHint.None, "")]
	public string speedDamagePointName = "DamagePoint2";

	[Export(PropertyHint.None, "")]
	public bool showSmokeOnSpeedDamage = true;

	[ExportGroup("Death Effect", "")]
	[Export(PropertyHint.None, "")]
	public PackedScene explosionEffect;

	[Export(PropertyHint.None, "")]
	public Vector2 explosionOffset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public string explosionAudioName = "ZamboniExplosion";

	[Export(PropertyHint.None, "")]
	public bool cameraShakeEnabled = true;

	[Export(PropertyHint.None, "")]
	public double cameraShakeStrength = 5.0;

	[Export(PropertyHint.None, "")]
	public double cameraShakeDuration = 0.05;

	[Export(PropertyHint.None, "")]
	public int cameraShakeFrequency = 4;

	[Export(PropertyHint.None, "")]
	public Vector2 cameraShakeRange = Vector2.One;

	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath smokeParticlePath;

	[Export(PropertyHint.None, "")]
	public NodePath fireSlotPath;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CatapultComponent();
	}
}
