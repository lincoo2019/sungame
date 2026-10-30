using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class SquashComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public string attackComponentInstanceId { get; set; } = "character.attack.0";

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[ExportGroup("Impact", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 hurtRange { get; set; } = new Vector2(0.625f, 0.2f);

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public bool checkAliveCharacter { get; set; }

	[Export(PropertyHint.None, "")]
	public int aliveCheckPhysicsFrames { get; set; } = 2;

	[Export(PropertyHint.None, "")]
	public double cleanupDelay { get; set; } = 0.5;

	[ExportGroup("Movement", "")]
	[Export(PropertyHint.None, "")]
	public double jumpHeight { get; set; } = 120.0;

	[Export(PropertyHint.None, "")]
	public double targetPositionDelay { get; set; }

	[Export(PropertyHint.None, "")]
	public double noLookFallbackDelay { get; set; } = 0.1;

	[Export(PropertyHint.None, "")]
	public double lookCompletionDelay { get; set; } = 0.5;

	[Export(PropertyHint.None, "")]
	public double jumpUpLeadDelay { get; set; } = 0.3;

	[Export(PropertyHint.None, "")]
	public double jumpTravelDuration { get; set; } = 0.4;

	[Export(PropertyHint.None, "")]
	public double noJumpUpLandingDelay { get; set; } = 0.1;

	[Export(PropertyHint.None, "")]
	public double descentStartDelay { get; set; } = 0.4;

	[Export(PropertyHint.None, "")]
	public double descentDuration { get; set; } = 0.1;

	[Export(PropertyHint.None, "")]
	public bool trackMovingTargetUntilImpact { get; set; }

	[Export(PropertyHint.None, "")]
	public bool completeStartedSequenceOutsideComponentBattlefield { get; set; }

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string lookLeftAnimeClip { get; set; } = "LookLeft";

	[Export(PropertyHint.None, "")]
	public string lookRightAnimeClip { get; set; } = "LookRight";

	[Export(PropertyHint.None, "")]
	public float lookAnimeTimeScale { get; set; } = 2f;

	[Export(PropertyHint.None, "")]
	public string jumpUpAnimeClip { get; set; } = "JumpUp";

	[Export(PropertyHint.None, "")]
	public string jumpDownAnimeClip { get; set; } = "JumpDown";

	[Export(PropertyHint.None, "")]
	public float jumpAnimeTimeScale { get; set; } = 3f;

	[Export(PropertyHint.None, "")]
	public float lookAnimationStartPosition { get; set; } = 0.1f;

	[Export(PropertyHint.None, "")]
	public float jumpUpAnimationStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public float jumpDownAnimationStartPosition { get; set; } = 0.1f;

	[ExportGroup("Audio and Camera", "")]
	[Export(PropertyHint.None, "")]
	public string readyAudioName { get; set; } = "SquasHmm";

	[Export(PropertyHint.None, "")]
	public string smashAudioName { get; set; } = "GargantuarThump";

	[Export(PropertyHint.None, "")]
	public string waterAudioName { get; set; } = "PlantWater";

	[Export(PropertyHint.None, "")]
	public Vector2 cameraShakeRange { get; set; } = Vector2.One;

	[Export(PropertyHint.None, "")]
	public double cameraShakeStrength { get; set; } = 5.0;

	[Export(PropertyHint.None, "")]
	public double cameraShakeDuration { get; set; } = 0.05;

	[Export(PropertyHint.None, "")]
	public int cameraShakeFrequency { get; set; } = 4;

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName readyStateEvent { get; set; } = "ToReady";

	[Export(PropertyHint.None, "")]
	public StringName jumpStateEvent { get; set; } = "ToJump";

	public int CollisionPreviewShapeCount => GodotObject.IsInstanceValid(checkShape) ? 1 : 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0)
		{
			return null;
		}
		return checkShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new SquashComponent();
	}
}
