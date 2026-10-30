using Godot;

[GlobalClass]
[Tool]
public partial class CannonComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Mode", "")]
	[Export(PropertyHint.Enum, "Marker,Line")]
	public string mode = "Marker";

	[Export(PropertyHint.None, "")]
	public bool autoAttack;

	[Export(PropertyHint.None, "")]
	public double autoAttackMaxXOffset;

	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath projectileSlotPath = new NodePath();

	[Export(PropertyHint.None, "")]
	public NodePath projectileNodePath = new NodePath();

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData;

	[Export(PropertyHint.None, "")]
	public string projectileLineFireComponentInstanceId = "character.fire";

	[Export(PropertyHint.None, "")]
	public NodePath projectileLineMarkerPath = new NodePath();

	[Export(PropertyHint.None, "")]
	public NodePath spritePath = new NodePath();

	[ExportGroup("Timing", "")]
	[Export(PropertyHint.None, "")]
	public double restTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double firstRestTime = 3.0;

	[Export(PropertyHint.None, "")]
	public bool disableRestTimeout;

	[ExportGroup("Marker Projectile", "")]
	[Export(PropertyHint.None, "")]
	public double markerProjectileHeight = 600.0;

	[Export(PropertyHint.None, "")]
	public double markerProjectileFallSpeed = 800.0;

	[Export(PropertyHint.None, "")]
	public Vector2 markerProjectileHitBoxScale = new Vector2(2f, 1f);

	[Export(PropertyHint.None, "")]
	public double markerVisualTravelHeight = 600.0;

	[Export(PropertyHint.None, "")]
	public double markerVisualTravelDuration = 0.75;

	[Export(PropertyHint.None, "")]
	public bool markerProjectileStraightMove;

	[Export(PropertyHint.None, "")]
	public double markerProjectileStraightSpeed = 600.0;

	[ExportGroup("Line Projectile", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 lineProjectileVelocity = new Vector2(800f, 0f);

	[Export(PropertyHint.None, "")]
	public int lineFirePositionIndex;

	[Export(PropertyHint.None, "")]
	public bool lineProjectileFlipWithParent = true;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string fireReadyEventName = "fire_ready";

	[Export(PropertyHint.None, "")]
	public string fireEventName = "fire";

	[Export(PropertyHint.None, "")]
	public string restAnimeClips = "Rest";

	[Export(PropertyHint.None, "")]
	public double restAnimeTimeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public string chargeAnimeClips = "Charge";

	[Export(PropertyHint.None, "")]
	public double chargeAnimeTimeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public string fireAnimeClips = "Fire";

	[Export(PropertyHint.None, "")]
	public double fireAnimeTimeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double animationStartPosition = 0.2;

	[ExportGroup("Audio", "")]
	[Export(PropertyHint.None, "")]
	public string chargeAudioName = "Shoop";

	[Export(PropertyHint.None, "")]
	public string fireReadyAudioName = "CobLaunch";

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent = "ToIdle";

	[Export(PropertyHint.None, "")]
	public StringName restStateEvent = "ToRest";

	[Export(PropertyHint.None, "")]
	public StringName chargeStateEvent = "ToCharge";

	[Export(PropertyHint.None, "")]
	public StringName fireStateEvent = "ToFire";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CannonComponent();
	}
}
