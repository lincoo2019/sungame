using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BowlingComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Movement", "")]
	[Export(PropertyHint.None, "")]
	public double rollXVelocityMax = 250.0;

	[Export(PropertyHint.None, "")]
	public double rollXVelocityMin = 200.0;

	[Export(PropertyHint.None, "")]
	public double rollYVelocity = 250.0;

	[Export(PropertyHint.None, "")]
	public bool selfRotateUse;

	[Export(PropertyHint.None, "")]
	public double selfRotationDivisor = 1000.0;

	[ExportGroup("Collision", "")]
	[Export(PropertyHint.None, "")]
	public bool useParentHitBox;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public int maxHitNum = -1;

	[Export(PropertyHint.None, "")]
	public bool hitLineUse;

	[Export(PropertyHint.None, "")]
	public bool hitLineBackUse;

	[Export(PropertyHint.None, "")]
	public bool edgeReboundUse;

	[ExportGroup("Map Boundaries", "")]
	[Export(PropertyHint.None, "")]
	public double offscreenDestroyMargin = 300.0;

	[Export(PropertyHint.None, "")]
	public double topBoundaryOffset = 50.0;

	[Export(PropertyHint.None, "")]
	public double bottomBoundaryOffset = 20.0;

	[Export(PropertyHint.Range, "1,10,1")]
	public int collisionCheckIntervalPhysicsFrames = 1;

	[ExportGroup("Rewards", "")]
	[Export(PropertyHint.None, "")]
	public bool fallCoinUse = true;

	[Export(PropertyHint.None, "")]
	public Array<int> hitCoinRewards = new Array<int> { 0, 0, 0, 10, 10, 10, 20 };

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string rollAnimeClips = "Roll";

	[Export(PropertyHint.None, "")]
	public double rollAnimeTimeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public string dieAnimeClip = "Death";

	[Export(PropertyHint.None, "")]
	public double rollAnimationStartPosition = 0.2;

	[ExportGroup("Audio", "")]
	[Export(PropertyHint.None, "")]
	public string rollAudioName = "Bowling";

	[Export(PropertyHint.None, "")]
	public string impactAudioName = "BowlingImpact";

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName rollStateEvent = "ToRoll";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent = "ToIdle";

	[ExportGroup("Camera Shake", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 cameraShakeRange = Vector2.One;

	[Export(PropertyHint.None, "")]
	public double cameraShakeStrength = 2.0;

	[Export(PropertyHint.None, "")]
	public double cameraShakeDuration = 0.05;

	[Export(PropertyHint.None, "")]
	public int cameraShakeFrequency = 4;

	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath checkAreaPath = new NodePath();

	[Export(PropertyHint.None, "")]
	public NodePath spritePath = new NodePath();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BowlingComponent();
	}
}
