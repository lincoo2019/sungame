using Godot;

public struct BulletFieldSpawnOverrides
{
	public bool useFall;

	public bool useGravity;

	public Variant metaData;

	public BulletFieldStoredProjectile[] eventProjectiles;

	public int? gridYOverride;

	public double? zOverride;

	public double? ySpeedOverride;

	public bool landOverSubscribed;

	public ulong spawnSourceInstanceId;

	public int? fireMethodFlagsOverride;

	public int? damageFlagsOverride;

	public double? baseDamageOverride;

	public float? fireLengthOverride;

	public bool? checkAllOverride;

	public float? initialRotationOverride;

	public float? spriteRotationOverride;

	public double? gravityOverride;

	public bool? flipXOverride;

	public int? trackSearchIntervalOverride;

	public Vector2? hitBoxScaleOverride;

	public Vector2? catapultTargetPositionOverride;

	public Vector2? catapultStartPositionOverride;

	public bool catapultSkyDrop;

	public float catapultSkyDropAscentHorizontalOffset;

	public double catapultSkyDropOffscreenWaitSeconds;

	public float catapultSkyDropVisualRadius;

	public int lifecycleOwnerSequence;

	public int lifecycleSlot;

	public bool lifecycleSubscribed;

	public Vector2? spawnTweenOffset;

	public float spawnTweenDuration;

	public Tween.EaseType spawnTweenEase;

	public Tween.TransitionType spawnTweenTrans;

	public bool portalReleasedOverride;

	public bool suppressGameplay;

	public bool deprioritizeDisabledTargets;
}
