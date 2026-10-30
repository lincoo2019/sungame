using System.Collections.Generic;
using Godot;

public struct BulletData
{
	public bool active;

	public bool over;

	public BulletRenderMode renderMode;

	public Vector2 pos;

	public Vector2 vel;

	public float speed;

	public int gridY;

	public int rowBucketSlot;

	public Vector2I gridPos;

	public bool lockGridY;

	public TowerDefenseEnum.CHARACTER_CAMP camp;

	public int collisionFlags;

	public TowerDefenseEnum.CHARACTER_HEIGHT projectileHeight;

	public bool checkHeight;

	public double damage;

	public int damageFlags;

	public TowerDefenseProjectileConfig config;

	public int behaviorProgramId;

	public bool canReuseCollisionGeometry;

	public TowerDefenseCharacter fireCharacter;

	public int fireLength;

	public Vector2 savePos;

	public float checkDistance;

	public Rect2 rect;

	public float fireDirX;

	public float rotateScale;

	public bool rotateFollowVelocity;

	public float rotation;

	public float spriteRotation;

	public float spriteRotationCos;

	public float spriteRotationSin;

	public int itemLayer;

	public int staticAtlasEntryIndex;

	public bool flipX;

	public Vector2 hitBoxScale;

	public int extId;

	public bool trackOpen;

	public int randFreshIndex;

	public double z;

	public double ySpeed;

	public double groundHeight;

	public double height;

	public bool useGravity;

	public float gravityScale;

	public double gravity;

	public bool useFall;

	public TowerDefenseCellInstance cell;

	public bool catapultOpen;

	public double catapultTimer;

	public double catapultTime;

	public Vector2 catapultTargetPos;

	public bool catapultSkyDrop;

	public CatapultSkyDropPhase catapultSkyDropPhase;

	public Vector2 catapultSkyDropLaunchPos;

	public double catapultSkyDropLaunchZ;

	public double catapultSkyDropAscentEndZ;

	public double catapultSkyDropAscentElapsed;

	public double catapultSkyDropAscentDuration;

	public float catapultSkyDropAscentHorizontalOffset;

	public double catapultSkyDropWaitRemaining;

	public double catapultSkyDropOffscreenWaitSeconds;

	public float catapultSkyDropVisualRadius;

	public bool isGround;

	public bool blocked;

	public TowerDefenseCharacter target;

	public TowerDefenseCharacter magneticTarget;

	public int trackSearchInterval;

	public int trackNoTargetInterval;

	public bool deprioritizeDisabledTargets;

	public int penetrateNum;

	public ulong lastHitInstanceId;

	public HashSet<ulong> penetratingTargetIds;

	public bool hitOver;

	public bool checkAll;

	public bool suppressGameplay;

	public bool portalReleased;

	public IProjectileZone zoneExclusionOwner;

	public int zoneExclusionExpiryFrame;

	public bool collisionEnabled;

	public int fireMethodFlags;

	public bool landOverSubscribed;

	public ulong spawnSourceInstanceId;

	public bool lifecycleSubscribed;

	public bool lifecycleTerminalEmitted;

	public int lifecycleOwnerSequence;

	public int lifecycleSlot;

	public int animDefId;

	public float yOffset;

	public float yOffsetTarget;

	public float yOffsetTimer;

	public float yOffsetDuration;

	public Vector2 animOffset;

	public float animElapsedTimer;

	public int animFrameMax;

	public double animFrameRate;

	public Vector2 spawnTweenStartPos;

	public Vector2 spawnTweenOffset;

	public float spawnTweenDuration;

	public float spawnTweenTimer;

	public Tween.EaseType spawnTweenEase;

	public Tween.TransitionType spawnTweenTrans;

	public bool externalControlled;

	public bool absorbOpen;

	public Vector2 absorbStartPos;

	public Vector2 absorbTargetPos;

	public Vector2 absorbControlPos;

	public float absorbDuration;

	public float absorbTimer;

	public float absorbScale;

	public float absorbSpin;
}
