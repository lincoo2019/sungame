using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class FireComponent : CharacterComponentRuntime, IStateMachinePhysicsFastCallbackTarget
{
	private enum FireRuntimeState : byte
	{
		Idle,
		Attack,
		Restore
	}

	private struct RayAttackGridSelector : ITowerDefenseAttackGridTraversalSelector, ITowerDefenseCharacterRectSelector
	{
		public FireComponent Owner;

		public Vector2 RayOrigin;

		public Vector2 RayTargetWorld;

		public int CollectionFlag;

		public float BestT;

		public int CandidateCount;

		public ulong PhysicsFrame;

		public bool UseGridLineSemantics;

		public bool StopOnFirstGridLineMatch;

		public TowerDefenseCharacter SelectedMatch { get; private set; }

		public bool ShouldStopAfterCell => GodotObject.IsInstanceValid(SelectedMatch);

		public bool CanConsider(TowerDefenseCharacter character)
		{
			CandidateCount++;
			if (!GodotObject.IsInstanceValid(character))
			{
				return false;
			}
			if (Owner == null || Owner.IsReleased || !GodotObject.IsInstanceValid(Owner.parent))
			{
				return false;
			}
			if (GodotObject.IsInstanceValid(character.instance))
			{
				TargetRegistrationComponent targetRegistrationComponent = character.targetRegistrationComponent;
				if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased && GodotObject.IsInstanceValid(Owner.parent.instance))
				{
					if (character.die || character.nearDie)
					{
						return false;
					}
					if (character.instance.invincible)
					{
						return false;
					}
					if (!character.instance.canBeCollection)
					{
						return false;
					}
					if (Owner.FilterGravestone && character is TowerDefenseGravestone)
					{
						return false;
					}
					if (!character.targetRegistrationComponent.canProjectileCheck)
					{
						return false;
					}
					if (!Owner.parent.CanTarget(character))
					{
						return false;
					}
					if (character is TowerDefenseZombie && IsGargantuarPhysiqueBlocked(Owner.canTargetGargantuar, character.instance.zombiePhysique))
					{
						return false;
					}
					if (character.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X > Owner.groundRight)
					{
						return false;
					}
					if ((CollectionFlag & character.instance.maskFlags) == 0)
					{
						return false;
					}
					if (Owner.checkHeight && (int)character.instance.height < Mathf.Min(2, (int)Owner.parent.instance.height) && character.instance.height <= Owner.parent.instance.height)
					{
						return false;
					}
					return true;
				}
			}
			return false;
		}

		public bool Visit(TowerDefenseCharacter character, ref TowerDefenseCharacter match)
		{
			float enterT;
			if (UseGridLineSemantics)
			{
				if (StopOnFirstGridLineMatch)
				{
					BestT = 0f;
					match = character;
					SelectedMatch = character;
					return true;
				}
				float x = character.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X;
				enterT = ((RayTargetWorld.X >= RayOrigin.X) ? x : (0f - x));
			}
			else if (!AabbShapeUtil.SegmentIntersectsRect(RayOrigin, RayTargetWorld, character.WorldHitRect, out enterT))
			{
				return false;
			}
			if (enterT < BestT)
			{
				BestT = enterT;
				match = character;
				SelectedMatch = character;
			}
			return false;
		}

		public void CompleteCell()
		{
		}
	}

	private struct RayAttackGridPairSelector : ITowerDefenseAttackGridTraversalSelector, ITowerDefenseCharacterRectSelector
	{
		public FireComponent Owner;

		public Vector2 RayOrigin;

		public Vector2 RayTargetWorld;

		public int PrimaryCollectionFlag;

		public int SecondaryCollectionFlag;

		public TowerDefenseCharacter PrimaryMatch;

		public TowerDefenseCharacter SecondaryMatch;

		public float PrimaryBestT;

		public float SecondaryBestT;

		public bool TrackSecondary;

		public int CandidateCount;

		public ulong PhysicsFrame;

		public TowerDefenseCharacter SelectedMatch
		{
			get
			{
				if (!GodotObject.IsInstanceValid(PrimaryMatch))
				{
					return SecondaryMatch;
				}
				return PrimaryMatch;
			}
		}

		public bool ShouldStopAfterCell => GodotObject.IsInstanceValid(PrimaryMatch);

		public bool CanConsider(TowerDefenseCharacter character)
		{
			CandidateCount++;
			if (!GodotObject.IsInstanceValid(character))
			{
				return false;
			}
			if (Owner == null || Owner.IsReleased || !GodotObject.IsInstanceValid(Owner.parent))
			{
				return false;
			}
			if (GodotObject.IsInstanceValid(character.instance))
			{
				TargetRegistrationComponent targetRegistrationComponent = character.targetRegistrationComponent;
				if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased && GodotObject.IsInstanceValid(Owner.parent.instance))
				{
					if (character.die || character.nearDie || character.instance.invincible || !character.instance.canBeCollection)
					{
						return false;
					}
					if (Owner.FilterGravestone && character is TowerDefenseGravestone)
					{
						return false;
					}
					if (!character.targetRegistrationComponent.canProjectileCheck || !Owner.parent.CanTarget(character))
					{
						return false;
					}
					if (character is TowerDefenseZombie && IsGargantuarPhysiqueBlocked(Owner.canTargetGargantuar, character.instance.zombiePhysique))
					{
						return false;
					}
					if (character.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X > Owner.groundRight)
					{
						return false;
					}
					int maskFlags = character.instance.maskFlags;
					if (((PrimaryCollectionFlag | SecondaryCollectionFlag) & maskFlags) == 0)
					{
						return false;
					}
					if (Owner.checkHeight && (int)character.instance.height < Mathf.Min(2, (int)Owner.parent.instance.height) && character.instance.height <= Owner.parent.instance.height)
					{
						return false;
					}
					return true;
				}
			}
			return false;
		}

		public bool Visit(TowerDefenseCharacter character, ref TowerDefenseCharacter match)
		{
			if (!AabbShapeUtil.SegmentIntersectsRect(RayOrigin, RayTargetWorld, character.WorldHitRect, out var enterT))
			{
				return false;
			}
			int maskFlags = character.instance.maskFlags;
			if ((PrimaryCollectionFlag & maskFlags) != 0 && enterT < PrimaryBestT)
			{
				PrimaryBestT = enterT;
				PrimaryMatch = character;
			}
			if (TrackSecondary && (SecondaryCollectionFlag & maskFlags) != 0 && enterT < SecondaryBestT)
			{
				SecondaryBestT = enterT;
				SecondaryMatch = character;
			}
			match = (GodotObject.IsInstanceValid(PrimaryMatch) ? PrimaryMatch : SecondaryMatch);
			return false;
		}

		public void CompleteCell()
		{
			if (GodotObject.IsInstanceValid(SecondaryMatch))
			{
				TrackSecondary = false;
			}
		}
	}

	public delegate void FireReadyEventHandler();

	public delegate void ConfirmProjectileEventHandler(int projectileId, TowerDefenseProjectileCreateData projectileData);

	public delegate void PrepareProjectileDataEventHandler(int projectileId, TowerDefenseProjectileCreateData projectileData);

	public delegate void FireVolleyEventHandler(ulong randomSeed);

	public delegate void FireOverEventHandler();

	public delegate void RestoreEventHandler();

	public readonly struct ProjectileBurstSpawnContext
	{
		internal readonly BulletField Field;

		internal readonly TowerDefenseCharacter Character;

		internal readonly TowerDefenseCharacter Target;

		internal readonly TowerDefenseProjectileConfig Config;

		internal readonly Vector2 Position;

		internal readonly Vector2I GridPosition;

		internal readonly int GridY;

		internal readonly Rect2 ProjectileMapRect;

		internal readonly double Height;

		internal readonly double GroundHeight;

		internal readonly int CollisionFlags;

		internal readonly TowerDefenseEnum.CHARACTER_CAMP Camp;

		internal readonly float VelocityScaleX;

		internal bool IsValid
		{
			get
			{
				if (GodotObject.IsInstanceValid(Field))
				{
					return GodotObject.IsInstanceValid(Config);
				}
				return false;
			}
		}

		internal ProjectileBurstSpawnContext(BulletField field, TowerDefenseCharacter character, TowerDefenseCharacter target, TowerDefenseProjectileConfig config, Vector2 position, Vector2I gridPosition, int gridY, Rect2 projectileMapRect, double height, double groundHeight, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, float velocityScaleX)
		{
			Field = field;
			Character = character;
			Target = target;
			Config = config;
			Position = position;
			GridPosition = gridPosition;
			GridY = gridY;
			ProjectileMapRect = projectileMapRect;
			Height = height;
			GroundHeight = groundHeight;
			CollisionFlags = collisionFlags;
			Camp = camp;
			VelocityScaleX = velocityScaleX;
		}
	}

	public const string TowerDefenseProjectileUid = "res://Prefab/TowerDefense/Projectile/TowerDefenseProjectile.tscn";

	private const int IdlePhysicsCallbackId = 1;

	private const int AttackPhysicsCallbackId = 2;

	private const int RestorePhysicsCallbackId = 3;

	private static readonly StringName DefaultSkinName = new StringName("Default");

	private static readonly System.Collections.Generic.Dictionary<string, StringName> _stringNameCache = new System.Collections.Generic.Dictionary<string, StringName>();

	public bool checkUse = true;

	public AdobeAnimateSprite sprite;

	public string fireAudioName = "ProjectileThrow";

	public string fireEventName = "fire";

	public StringName attackStateEvent = "ToAttack";

	public StringName restoreStateEvent = "ToRestore";

	public StringName idleStateEvent = "ToIdle";

	private string _cachedFireEventName;

	private string[] _cachedFireEventNames;

	private AdobeAnimateSprite _animationAvailabilitySprite;

	private AdobeAnimateData _animationAvailabilityData;

	private string _animationAvailabilityClipName;

	private bool _animationAvailabilityResult;

	private bool _animationAvailabilityCached;

	private Action _animationAvailabilityChangedHandler;

	public string fireOverEventName = "";

	public Array<string> fireAnimeClipsArray = new Array<string> { "Fire" };

	public string fireAnimeClips = "Fire";

	public float fireAnimeTimeScale = 1f;

	public string restoreAnimeClips = "";

	public float restoreTime = 10f;

	public string spliceIdleAnimeClips = "";

	public float spliceIdleAnimeTimeScale = 1f;

	public bool isSpliceSprite;

	public Array<string> spliceSpriteFireAnimeClipsList = new Array<string>();

	public Array<string> spliceSpriteIdleAnimeClipsList = new Array<string>();

	private readonly List<Marker2D> _firePosMarkers = new List<Marker2D>();

	private readonly List<AabbRay2DResource> _checkRayResources = new List<AabbRay2DResource>();

	private readonly List<AabbShape2DResource> _checkShapeResources = new List<AabbShape2DResource>();

	private readonly List<Vector2> _runtimeRayTargets = new List<Vector2>();

	private readonly List<Transform2D> _runtimeRayTransforms = new List<Transform2D>();

	private readonly List<bool> _runtimeRayTransformOverrides = new List<bool>();

	private readonly System.Collections.Generic.Dictionary<int, int> _gridRelativeRayRows = new System.Collections.Generic.Dictionary<int, int>();

	private readonly System.Collections.Generic.Dictionary<int, int> _gridRelativeShapeRows = new System.Collections.Generic.Dictionary<int, int>();

	private float _appliedGridRelativeGeometryHeight = 0f / 0f;

	private bool _gridRelativeGeometryDirty;

	private bool _applyingGridRelativeGeometry;

	private readonly List<string> _fireAnimeClipNames = new List<string>();

	private readonly List<AdobeAnimateSprite> _spliceSprites = new List<AdobeAnimateSprite>();

	private readonly List<string> _spliceSpriteFireAnimeClips = new List<string>();

	private readonly List<string> _spliceSpriteIdleAnimeClips = new List<string>();

	private readonly HashSet<string> _fireAnimeClipSet = new HashSet<string>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, int> _spliceFireClipIndices = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

	public bool onlyEmitSignal;

	public FireComponentExtendBase preExtend;

	public float fireLength = -1f;

	public bool fireDirect;

	public float fireIntervalBase = 1.5f;

	public float fireInterval = 1.5f;

	public float fireIntervalOffset = 0.1f;

	public int fireNum = 1;

	public bool fireNumAtOnce;

	public Array<FireComponentCheckConfig> fireCheckList = new Array<FireComponentCheckConfig>();

	public Array<FireComponentFireProjectileConfig> fireProjectileList = new Array<FireComponentFireProjectileConfig>();

	private readonly List<FireComponentCheckConfig> _fireChecks = new List<FireComponentCheckConfig>();

	private readonly List<FireComponentFireProjectileConfig> _fireProjectiles = new List<FireComponentFireProjectileConfig>();

	private readonly List<TowerDefenseProjectileCreateData> _projectileWarmupCandidates = new List<TowerDefenseProjectileCreateData>();

	private readonly HashSet<FireComponentProjectileResource> _projectileWarmupResources = new HashSet<FireComponentProjectileResource>();

	public bool useCollisionEveryPos;

	public bool readyConfirmProjectile;

	public bool lockProjectileGridY;

	public bool snapStraightProjectileToSameCellTarget;

	public float offscreenTargetMarginColumns = 1f;

	public float defaultCheckLengthWorld = 2000f;

	public float fireAnimationStartPosition = 0.2f;

	public float repeatFireAnimationStartPosition = 0.1f;

	public float spliceAnimationStartPosition = 0.2f;

	public float restoreAnimationStartPosition = 0.2f;

	public float offsetLineTweenDuration = 0.15f;

	public float blockedOffsetLineX = 25f;

	public bool checkAllLine;

	private float _checkLength = -1f;

	public int checkIntervalMax = 2;

	public bool checkHeight = true;

	public bool airFirst;

	public bool catapultFirstFar;

	public bool randomChoose;

	public bool canTargetGargantuar = true;

	public bool checkGravestone = true;

	public TowerDefenseMapControl mapControl;

	public TowerDefenseCharacter parent;

	public float timeScale = 1f;

	public float timer = 0.25f;

	public bool isCheck;

	public int checkIntreval;

	public float groundRight;

	public bool hasCatPumpkin;

	public TowerDefenseCharacter firstCharacter;

	public FireComponentCheckConfig runningCheck;

	public int runningCheckId;

	public int currentFireNum;

	public string currentFireEvent = "";

	private long _fireVolleySequence;

	public int _catPumpkinCheckFrame;

	private StateHandle _idleState;

	private StateHandle _attackState;

	private StateHandle _restoreState;

	private bool _signalsConnected;

	private bool _waitingForParentReady;

	private bool _runtimeInitialized;

	private bool _configured;

	private bool _runtimeResourcesIsolated;

	private bool _idleCheckBatchActive;

	private bool _restoreTransitionPending;

	private bool _progressAnimationStateMissing;

	private bool _legacyProgressRecoveryPending;

	private bool _hasOffsetLine;

	private bool _idleStatePhysicsSuspended;

	private double _appliedIdlePresentationTimeScale = 0.0 / 0.0;

	private string _pendingStateName = "";

	private static readonly Vector2 DefaultCreateDataSize = new Vector2(28f, 28f);

	private static readonly Vector2 DefaultCreateDataScale = new Vector2(1f, 1f);

	public bool alive
	{
		get
		{
			return base.Alive;
		}
		set
		{
			SetAlive(value);
		}
	}

	public Array<Marker2D> firePosMarker { get; set; } = new Array<Marker2D>();

	public Array<AabbRay2DResource> checkRayResources { get; set; } = new Array<AabbRay2DResource>();

	public Array<AabbShape2DResource> checkShapeResources { get; set; } = new Array<AabbShape2DResource>();

	public Array<AdobeAnimateSprite> spliceSpriteList { get; set; } = new Array<AdobeAnimateSprite>();

	private bool HasGridRelativeCheckAreaRows => _gridRelativeShapeRows.Count > 0;

	public float checkLength
	{
		get
		{
			return _checkLength;
		}
		set
		{
			SetCheckLength(value);
		}
	}

	public bool FilterGravestone
	{
		get
		{
			if (checkGravestone)
			{
				return parent is TowerDefenseZombie;
			}
			return true;
		}
	}

	public int checkInterval
	{
		get
		{
			return checkIntreval;
		}
		set
		{
			checkIntreval = Math.Max(0, value);
		}
	}

	private bool CanDispatchStateProcessing
	{
		get
		{
			if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && parent != null && sprite != null)
			{
				if (_fireChecks.Count <= 0)
				{
					return _checkShapeResources.Count > 0;
				}
				return true;
			}
			return false;
		}
	}

	protected override bool IsStateMachineDispatchEligible
	{
		get
		{
			if (CanDispatchStateProcessing)
			{
				return !_idleStatePhysicsSuspended;
			}
			return false;
		}
	}

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (!(timer > 0f) && !isCheck)
			{
				if (_idleStatePhysicsSuspended)
				{
					return checkIntreval > 0;
				}
				return false;
			}
			return true;
		}
	}

	private FireComponentDefinition Definition => base.ComponentDefinition as FireComponentDefinition;

	private bool CanSuspendIdleStatePhysics => _idleState?.IsActive ?? false;

	public event FireReadyEventHandler OnFireReady;

	public event ConfirmProjectileEventHandler OnConfirmProjectile;

	public event PrepareProjectileDataEventHandler OnPrepareProjectileData;

	public event FireVolleyEventHandler OnFireVolley;

	public event FireOverEventHandler OnFireOver;

	public event RestoreEventHandler OnRestore;

	private static bool IsInstanceValid(GodotObject instance)
	{
		return GodotObject.IsInstanceValid(instance);
	}

	protected override void OnAliveChanged(bool value)
	{
		if (!value)
		{
			SetFireState(FireRuntimeState.Idle, allowWhenInactive: true);
		}
		_restoreTransitionPending = false;
	}

	private static StringName GetCachedStringName(string name)
	{
		if (name == null)
		{
			return null;
		}
		if (_stringNameCache.TryGetValue(name, out var value))
		{
			return value;
		}
		StringName stringName = new StringName(name);
		_stringNameCache[name] = stringName;
		return stringName;
	}

	private static void EndFireMetric(string metricName, long startTicks, int items)
	{
		TowerDefensePerfProfiler.End(metricName, startTicks, items);
	}

	private static float GetProjectileBodyScaleX(TowerDefenseCharacter character, bool projectileFlip)
	{
		float num = (GodotObject.IsInstanceValid(character) ? character.Scale.X : 1f);
		if (!projectileFlip)
		{
			return num;
		}
		return 0f - num;
	}

	internal static bool IsGargantuarPhysiqueBlocked(bool canTargetGargantuar, TowerDefenseEnum.ZOMBIE_PHYSIQUE physique)
	{
		if (!canTargetGargantuar)
		{
			return physique >= TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE;
		}
		return false;
	}

	internal static int GetIdleFireCheckPriorityPassCount(bool airFirst, bool checkUse)
	{
		if (!(airFirst && checkUse))
		{
			return 1;
		}
		return 2;
	}

	internal static int ResolveEffectiveCheckCollisionFlags(int configuredFlags, int parentCollisionFlags)
	{
		if (configuredFlags != -1)
		{
			return configuredFlags;
		}
		return parentCollisionFlags;
	}

	internal static bool IsIdleFireCheckInPriorityPass(int collisionFlags, int priorityPass, int priorityPasses)
	{
		if (priorityPasses != 2)
		{
			return true;
		}
		return (collisionFlags & 2) != 0 == (priorityPass == 0);
	}

	internal static void ResolveOffsetLineRoute(Vector2 authoredPosition, int offsetLine, int ownerGridY, int targetGridY, float gridHeight, float tweenDuration, float blockedOffsetX, out Vector2 routedPosition, out float yOffsetTarget, out float yOffsetDuration)
	{
		routedPosition = authoredPosition;
		yOffsetTarget = 0f;
		yOffsetDuration = 0f;
		int num = targetGridY - ownerGridY;
		if (num != 0)
		{
			yOffsetTarget = gridHeight * (float)num;
			yOffsetDuration = Mathf.Max(0f, tweenDuration);
		}
		else if (offsetLine != 0)
		{
			routedPosition = new Vector2(authoredPosition.X - blockedOffsetX, authoredPosition.Y);
		}
	}

	public Tween CreateTween()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		return parent.CreateTween();
	}

	private bool SetFireState(FireRuntimeState state, bool allowWhenInactive = false)
	{
		return state switch
		{
			FireRuntimeState.Attack => SetFlatState(_attackState, attackStateEvent, allowWhenInactive), 
			FireRuntimeState.Restore => SetFlatState(_restoreState, restoreStateEvent, allowWhenInactive), 
			_ => SetFlatState(_idleState, idleStateEvent, allowWhenInactive), 
		};
	}

	internal override bool TryTickFlatStateMachinePhysics(double delta)
	{
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.SupportsFlatDirectStateTransitions || !TryResolveFireRuntimeState(base.StateMachine.CurrentStateHandle, out var state))
		{
			return false;
		}
		for (int i = 0; i < 3; i++)
		{
			int num = (int)state;
			switch (state)
			{
			case FireRuntimeState.Attack:
				AttackProcessing(delta);
				break;
			case FireRuntimeState.Restore:
				RestoreProcessing(delta);
				break;
			default:
				IdleProcessing(delta);
				break;
			}
			if (!TryResolveFireRuntimeState(base.StateMachine.CurrentStateHandle, out var state2) || (int)state2 <= num)
			{
				return true;
			}
			state = state2;
		}
		return true;
	}

	private bool TryResolveFireRuntimeState(StateHandle activeState, out FireRuntimeState state)
	{
		if (activeState == _idleState)
		{
			state = FireRuntimeState.Idle;
			return true;
		}
		if (activeState == _attackState)
		{
			state = FireRuntimeState.Attack;
			return true;
		}
		if (activeState == _restoreState)
		{
			state = FireRuntimeState.Restore;
			return true;
		}
		state = FireRuntimeState.Idle;
		return false;
	}

	private static void CopyGodotArray(Array<Marker2D> source, List<Marker2D> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<AabbRay2DResource> source, List<AabbRay2DResource> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<AabbShape2DResource> source, List<AabbShape2DResource> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<string> source, List<string> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<AdobeAnimateSprite> source, List<AdobeAnimateSprite> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<FireComponentCheckConfig> source, List<FireComponentCheckConfig> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void CopyGodotArray(Array<FireComponentFireProjectileConfig> source, List<FireComponentFireProjectileConfig> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private void RefreshExportedArrayCaches()
	{
		List<AabbRay2DResource> list = null;
		List<Transform2D> list2 = null;
		List<bool> list3 = null;
		for (int i = 0; i < _runtimeRayTransformOverrides.Count; i++)
		{
			if (_runtimeRayTransformOverrides[i])
			{
				list = new List<AabbRay2DResource>(_checkRayResources);
				list2 = new List<Transform2D>(_runtimeRayTransforms);
				list3 = new List<bool>(_runtimeRayTransformOverrides);
				break;
			}
		}
		CopyGodotArray(firePosMarker, _firePosMarkers);
		CopyGodotArray(checkRayResources, _checkRayResources);
		CopyGodotArray(checkShapeResources, _checkShapeResources);
		_runtimeRayTargets.Clear();
		_runtimeRayTransforms.Clear();
		_runtimeRayTransformOverrides.Clear();
		for (int j = 0; j < _checkRayResources.Count; j++)
		{
			AabbRay2DResource aabbRay2DResource = _checkRayResources[j];
			_runtimeRayTargets.Add(GodotObject.IsInstanceValid(aabbRay2DResource) ? aabbRay2DResource.TargetPosition : Vector2.Zero);
			bool flag = list != null && j < list.Count && j < list2.Count && j < list3.Count && list3[j] && list[j] == aabbRay2DResource;
			_runtimeRayTransforms.Add(flag ? list2[j] : (GodotObject.IsInstanceValid(aabbRay2DResource) ? aabbRay2DResource.LocalTransform : Transform2D.Identity));
			_runtimeRayTransformOverrides.Add(flag);
		}
		CopyGodotArray(fireAnimeClipsArray, _fireAnimeClipNames);
		CopyGodotArray(spliceSpriteList, _spliceSprites);
		CopyGodotArray(spliceSpriteFireAnimeClipsList, _spliceSpriteFireAnimeClips);
		CopyGodotArray(spliceSpriteIdleAnimeClipsList, _spliceSpriteIdleAnimeClips);
		CopyGodotArray(fireCheckList, _fireChecks);
		CopyGodotArray(fireProjectileList, _fireProjectiles);
		_fireAnimeClipSet.Clear();
		for (int k = 0; k < _fireAnimeClipNames.Count; k++)
		{
			if (!string.IsNullOrEmpty(_fireAnimeClipNames[k]))
			{
				_fireAnimeClipSet.Add(_fireAnimeClipNames[k]);
			}
		}
		_spliceFireClipIndices.Clear();
		for (int l = 0; l < _spliceSpriteFireAnimeClips.Count; l++)
		{
			string text = _spliceSpriteFireAnimeClips[l];
			if (!string.IsNullOrEmpty(text) && !_spliceFireClipIndices.ContainsKey(text))
			{
				_spliceFireClipIndices.Add(text, l);
			}
		}
		_hasOffsetLine = false;
		for (int m = 0; m < _fireProjectiles.Count; m++)
		{
			if (GodotObject.IsInstanceValid(_fireProjectiles[m]) && _fireProjectiles[m].offsetLine != 0)
			{
				_hasOffsetLine = true;
				break;
			}
		}
		if (_gridRelativeRayRows.Count > 0 || _gridRelativeShapeRows.Count > 0)
		{
			_gridRelativeGeometryDirty = true;
		}
	}

	private bool CanPlayFireAnimation(string clipName)
	{
		if (!IsInstanceValid(sprite))
		{
			SetAnimationAvailabilityData(null);
			return false;
		}
		AdobeAnimateData flashAnimeData = sprite.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData))
		{
			SetAnimationAvailabilityData(null);
			return false;
		}
		if (string.IsNullOrEmpty(clipName))
		{
			return false;
		}
		if (_animationAvailabilityCached && _animationAvailabilitySprite == sprite && _animationAvailabilityData == flashAnimeData && string.Equals(_animationAvailabilityClipName, clipName, StringComparison.Ordinal))
		{
			return _animationAvailabilityResult;
		}
		SetAnimationAvailabilityData(flashAnimeData);
		string[] array = clipName.Split("&", StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return false;
		}
		bool flag = true;
		string[] array2 = array;
		foreach (string clipName2 in array2)
		{
			if (!sprite.HasClip(clipName2))
			{
				flag = false;
				break;
			}
		}
		_animationAvailabilitySprite = sprite;
		_animationAvailabilityClipName = clipName;
		_animationAvailabilityResult = flag;
		_animationAvailabilityCached = true;
		return flag;
	}

	private void SetAnimationAvailabilityData(AdobeAnimateData animationData)
	{
		if (_animationAvailabilityData == animationData)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_animationAvailabilityData) && _animationAvailabilityChangedHandler != null)
		{
			_animationAvailabilityData.Changed -= _animationAvailabilityChangedHandler;
		}
		_animationAvailabilityData = (GodotObject.IsInstanceValid(animationData) ? animationData : null);
		_animationAvailabilityCached = false;
		if (_animationAvailabilityData != null)
		{
			if (_animationAvailabilityChangedHandler == null)
			{
				_animationAvailabilityChangedHandler = InvalidateAnimationAvailabilityCache;
			}
			_animationAvailabilityData.Changed += _animationAvailabilityChangedHandler;
		}
	}

	private void InvalidateAnimationAvailabilityCache()
	{
		_animationAvailabilityCached = false;
	}

	private void ClearAnimationAvailabilityCache()
	{
		if (GodotObject.IsInstanceValid(_animationAvailabilityData) && _animationAvailabilityChangedHandler != null)
		{
			_animationAvailabilityData.Changed -= _animationAvailabilityChangedHandler;
		}
		_animationAvailabilitySprite = null;
		_animationAvailabilityData = null;
		_animationAvailabilityClipName = null;
		_animationAvailabilityResult = false;
		_animationAvailabilityCached = false;
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		DuplicateRuntimeResources();
		RefreshConfiguration();
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				InitializeRuntime();
			}
		}
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		ConnectSignals();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			InitializeRuntime();
		}
		else
		{
			ConnectParentReady();
		}
		ApplyPendingStateName();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_legacyProgressRecoveryPending = false;
		_progressAnimationStateMissing = false;
		ClearAnimationAvailabilityCache();
		DisconnectParentReady();
		DisconnectSignals();
		firePosMarker.Clear();
		spliceSpriteList.Clear();
		_firePosMarkers.Clear();
		_spliceSprites.Clear();
		sprite = null;
		preExtend = null;
		parent = null;
		_runtimeInitialized = false;
		ClearGridRelativeGeometryBindings();
	}

	protected override void OnReleased()
	{
		_legacyProgressRecoveryPending = false;
		_progressAnimationStateMissing = false;
		ClearAnimationAvailabilityCache();
		DisconnectSignals();
		this.OnFireReady = null;
		this.OnConfirmProjectile = null;
		this.OnPrepareProjectileData = null;
		this.OnFireVolley = null;
		this.OnFireOver = null;
		this.OnRestore = null;
		firePosMarker.Clear();
		spliceSpriteList.Clear();
		fireCheckList.Clear();
		fireProjectileList.Clear();
		checkRayResources.Clear();
		checkShapeResources.Clear();
		_firePosMarkers.Clear();
		_spliceSprites.Clear();
		_fireChecks.Clear();
		_fireProjectiles.Clear();
		_checkRayResources.Clear();
		_checkShapeResources.Clear();
		sprite = null;
		preExtend = null;
		parent = null;
		_runtimeResourcesIsolated = false;
		ClearGridRelativeGeometryBindings();
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			FireComponentDefinition definition = Definition;
			checkUse = definition.checkUse;
			fireAudioName = definition.fireAudioName;
			fireEventName = definition.fireEventName;
			attackStateEvent = definition.attackStateEvent;
			restoreStateEvent = definition.restoreStateEvent;
			idleStateEvent = definition.idleStateEvent;
			fireOverEventName = definition.fireOverEventName;
			fireAnimeClipsArray = ((definition.fireAnimeClipsArray != null) ? definition.fireAnimeClipsArray.Duplicate(deep: true) : new Array<string>());
			fireAnimeClips = definition.fireAnimeClips;
			fireAnimeTimeScale = definition.fireAnimeTimeScale;
			restoreAnimeClips = definition.restoreAnimeClips;
			restoreTime = definition.restoreTime;
			spliceIdleAnimeClips = definition.spliceIdleAnimeClips;
			spliceIdleAnimeTimeScale = definition.spliceIdleAnimeTimeScale;
			isSpliceSprite = definition.isSpliceSprite;
			spliceSpriteFireAnimeClipsList = ((definition.spliceSpriteFireAnimeClipsList != null) ? definition.spliceSpriteFireAnimeClipsList.Duplicate(deep: true) : new Array<string>());
			spliceSpriteIdleAnimeClipsList = ((definition.spliceSpriteIdleAnimeClipsList != null) ? definition.spliceSpriteIdleAnimeClipsList.Duplicate(deep: true) : new Array<string>());
			onlyEmitSignal = definition.onlyEmitSignal;
			fireLength = definition.fireLength;
			fireDirect = definition.fireDirect;
			fireIntervalBase = definition.fireIntervalBase;
			fireInterval = definition.fireInterval;
			fireIntervalOffset = definition.fireIntervalOffset;
			fireNum = definition.fireNum;
			fireNumAtOnce = definition.fireNumAtOnce;
			fireCheckList = definition.fireCheckList ?? new Array<FireComponentCheckConfig>();
			fireProjectileList = definition.fireProjectileList ?? new Array<FireComponentFireProjectileConfig>();
			checkRayResources = ((definition.checkRayResources != null) ? definition.checkRayResources.Duplicate() : new Array<AabbRay2DResource>());
			checkShapeResources = definition.checkShapeResources ?? new Array<AabbShape2DResource>();
			useCollisionEveryPos = definition.useCollisionEveryPos;
			readyConfirmProjectile = definition.readyConfirmProjectile;
			lockProjectileGridY = definition.lockProjectileGridY;
			snapStraightProjectileToSameCellTarget = definition.snapStraightProjectileToSameCellTarget;
			offscreenTargetMarginColumns = definition.offscreenTargetMarginColumns;
			defaultCheckLengthWorld = definition.defaultCheckLengthWorld;
			fireAnimationStartPosition = definition.fireAnimationStartPosition;
			repeatFireAnimationStartPosition = definition.repeatFireAnimationStartPosition;
			spliceAnimationStartPosition = definition.spliceAnimationStartPosition;
			restoreAnimationStartPosition = definition.restoreAnimationStartPosition;
			offsetLineTweenDuration = definition.offsetLineTweenDuration;
			blockedOffsetLineX = definition.blockedOffsetLineX;
			checkAllLine = definition.checkAllLine;
			_checkLength = definition.checkLength;
			checkIntervalMax = definition.checkIntervalMax;
			checkHeight = definition.checkHeight;
			airFirst = definition.airFirst;
			catapultFirstFar = definition.catapultFirstFar;
			randomChoose = definition.randomChoose;
			canTargetGargantuar = definition.canTargetGargantuar;
			checkGravestone = definition.checkGravestone;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		if (GodotObject.IsInstanceValid(parent) && Definition != null)
		{
			firePosMarker.Clear();
			for (int i = 0; i < (Definition.firePosMarkerPaths?.Count ?? 0); i++)
			{
				Marker2D item = ResolveOwnerNode<Marker2D>(Definition.firePosMarkerPaths[i]);
				firePosMarker.Add(item);
			}
			spliceSpriteList.Clear();
			for (int j = 0; j < (Definition.spliceSpritePaths?.Count ?? 0); j++)
			{
				AdobeAnimateSprite item2 = ResolveOwnerNode<AdobeAnimateSprite>(Definition.spliceSpritePaths[j]);
				spliceSpriteList.Add(item2);
			}
			sprite = ResolveOwnerNode<AdobeAnimateSprite>(Definition.spritePath);
			preExtend = null;
			if (!string.IsNullOrEmpty(Definition.preExtendInstanceId) && base.Manager != null && base.Manager.TryGetRuntimeByInstanceId(Definition.preExtendInstanceId, out var runtime))
			{
				preExtend = runtime as FireComponentExtendBase;
			}
		}
	}

	private T ResolveOwnerNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(parent))
		{
			return parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	private void DuplicateRuntimeResources()
	{
		if (_runtimeResourcesIsolated)
		{
			RefreshExportedArrayCaches();
			return;
		}
		if (fireCheckList == null)
		{
			fireCheckList = new Array<FireComponentCheckConfig>();
		}
		if (fireProjectileList == null)
		{
			fireProjectileList = new Array<FireComponentFireProjectileConfig>();
		}
		fireCheckList = fireCheckList.Duplicate();
		fireProjectileList = fireProjectileList.Duplicate();
		for (int i = 0; i < fireCheckList.Count; i++)
		{
			if (fireCheckList[i] != null)
			{
				fireCheckList[i] = (FireComponentCheckConfig)fireCheckList[i].Duplicate(deep: true);
			}
		}
		for (int j = 0; j < fireProjectileList.Count; j++)
		{
			if (fireProjectileList[j] != null)
			{
				fireProjectileList[j] = (FireComponentFireProjectileConfig)fireProjectileList[j].Duplicate(deep: true);
			}
		}
		if (checkShapeResources == null)
		{
			Array<AabbShape2DResource> array = (checkShapeResources = new Array<AabbShape2DResource>());
		}
		checkShapeResources = checkShapeResources.Duplicate();
		for (int k = 0; k < checkShapeResources.Count; k++)
		{
			if (checkShapeResources[k] != null)
			{
				checkShapeResources[k] = (AabbShape2DResource)checkShapeResources[k].Duplicate(deep: true);
			}
		}
		_runtimeResourcesIsolated = true;
		RefreshExportedArrayCaches();
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("fire.idle");
		_attackState = base.StateMachine?.GetStateById("fire.attack");
		_restoreState = base.StateMachine?.GetStateById("fire.restore");
		ConnectSignals();
		if (!_runtimeInitialized && GodotObject.IsInstanceValid(parent) && !parent.IsNodeReady())
		{
			ConnectParentReady();
		}
		Callable.From(ApplyPendingStateName).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingStateName();
	}

	protected override void OnStateRuntimeDetaching()
	{
		_idleStatePhysicsSuspended = false;
		DisconnectSignals();
		_idleState = null;
		_attackState = null;
		_restoreState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		int num;
		if (!remote && _progressAnimationStateMissing)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				num = ((base.StateMachine.CurrentStateHandle.StableId != "fire.idle") ? 1 : 0);
				goto IL_005d;
			}
		}
		num = 0;
		goto IL_005d;
		IL_005d:
		_progressAnimationStateMissing = false;
		if (num != 0 && !_legacyProgressRecoveryPending)
		{
			_legacyProgressRecoveryPending = true;
			Callable.From(RecoverLegacyProgressAnimationState).CallDeferred();
		}
	}

	private void RecoverLegacyProgressAnimationState()
	{
		_legacyProgressRecoveryPending = false;
		if (!base.Alive || !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree())
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
		{
			if (base.StateMachine.CurrentStateHandle.StableId != "fire.idle")
			{
				SetFireState(FireRuntimeState.Idle);
			}
			else if (parent.componentRunning && parent is TowerDefensePlant towerDefensePlant)
			{
				towerDefensePlant.Idle();
			}
		}
	}

	private void ConnectParentReady()
	{
		if (!_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready += OnParentReady;
			_waitingForParentReady = true;
		}
	}

	private void DisconnectParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
	}

	private void OnParentReady()
	{
		DisconnectParentReady();
		InitializeRuntime();
	}

	private void InitializeRuntime()
	{
		if (!_runtimeInitialized && GodotObject.IsInstanceValid(parent))
		{
			RefreshMapMetrics();
			SetCheckLength(_checkLength);
			RestoreRunningCheckReference();
			QueueProjectileRenderWarmups();
			if (isSpliceSprite)
			{
				parent.useIdleAnimeReset = false;
			}
			ApplyPendingStateName();
			_runtimeInitialized = true;
		}
	}

	private void QueueProjectileRenderWarmups()
	{
		_projectileWarmupCandidates.Clear();
		_projectileWarmupResources.Clear();
		for (int i = 0; i < _fireChecks.Count; i++)
		{
			FireComponentCheckConfig fireComponentCheckConfig = _fireChecks[i];
			if (GodotObject.IsInstanceValid(fireComponentCheckConfig) && GodotObject.IsInstanceValid(fireComponentCheckConfig.projectile))
			{
				fireComponentCheckConfig.projectile.CollectProjectileData(_projectileWarmupCandidates, _projectileWarmupResources);
			}
		}
		for (int j = 0; j < _projectileWarmupCandidates.Count; j++)
		{
			BulletField.QueueRenderTemplateWarmup(_projectileWarmupCandidates[j]);
		}
	}

	public void RefreshConfiguration()
	{
		bool signalsConnected = _signalsConnected;
		bool waitingForParentReady = _waitingForParentReady;
		if (signalsConnected)
		{
			DisconnectSignals();
		}
		RefreshExportedArrayCaches();
		_cachedFireEventName = null;
		_cachedFireEventNames = null;
		if (_runtimeInitialized)
		{
			RefreshMapMetrics();
			SetCheckLength(_checkLength);
			RestoreRunningCheckReference();
			QueueProjectileRenderWarmups();
		}
		if (signalsConnected)
		{
			ConnectSignals();
			if (waitingForParentReady)
			{
				ConnectParentReady();
			}
		}
		UpdateIdleStatePhysicsSuspension();
		RefreshStateMachineDispatchEligibility();
	}

	public void RefreshMapMetrics()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			groundRight = (float)(instance.GetMapGroundRight() + (double)(instance.GetMapGridSize().X * Math.Max(0f, offscreenTargetMarginColumns)));
		}
	}

	private void ConnectSignals()
	{
		if (_signalsConnected)
		{
			return;
		}
		if (IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			sprite.OnAnimeEvent += AnimeEvent;
		}
		if (isSpliceSprite)
		{
			for (int i = 0; i < _spliceSprites.Count; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
				if (IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite != sprite)
				{
					adobeAnimateSprite.OnAnimeCompleted += AnimeCompleted;
				}
			}
		}
		StateHandle idleState = _idleState;
		if (idleState != null && idleState.IsValid)
		{
			_idleState.Entered += IdleEntered;
			_idleState.Exited += IdleExited;
			if (!_idleState.TrySetPhysicsFastCallback(this, 1))
			{
				_idleState.PhysicsProcessing += IdleProcessing;
			}
		}
		StateHandle attackState = _attackState;
		if (attackState != null && attackState.IsValid)
		{
			_attackState.Entered += AttackEntered;
			_attackState.Exited += AttackExited;
			if (!_attackState.TrySetPhysicsFastCallback(this, 2))
			{
				_attackState.PhysicsProcessing += AttackProcessing;
			}
		}
		StateHandle restoreState = _restoreState;
		if (restoreState != null && restoreState.IsValid)
		{
			_restoreState.Entered += RestoreEntered;
			_restoreState.Exited += RestoreExited;
			if (!_restoreState.TrySetPhysicsFastCallback(this, 3))
			{
				_restoreState.PhysicsProcessing += RestoreProcessing;
			}
		}
		_signalsConnected = true;
	}

	private void DisconnectSignals()
	{
		DisconnectParentReady();
		if (IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
			sprite.OnAnimeEvent -= AnimeEvent;
		}
		for (int i = 0; i < _spliceSprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
			if (IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite != sprite)
			{
				adobeAnimateSprite.OnAnimeCompleted -= AnimeCompleted;
			}
		}
		if (_idleState != null)
		{
			_idleState.Entered -= IdleEntered;
			_idleState.Exited -= IdleExited;
			if (!_idleState.ClearPhysicsFastCallback(this))
			{
				_idleState.PhysicsProcessing -= IdleProcessing;
			}
		}
		if (_attackState != null)
		{
			_attackState.Entered -= AttackEntered;
			_attackState.Exited -= AttackExited;
			if (!_attackState.ClearPhysicsFastCallback(this))
			{
				_attackState.PhysicsProcessing -= AttackProcessing;
			}
		}
		if (_restoreState != null)
		{
			_restoreState.Entered -= RestoreEntered;
			_restoreState.Exited -= RestoreExited;
			if (!_restoreState.ClearPhysicsFastCallback(this))
			{
				_restoreState.PhysicsProcessing -= RestoreProcessing;
			}
		}
		_signalsConnected = false;
	}

	void IStateMachinePhysicsFastCallbackTarget.InvokeStateMachinePhysicsFastCallback(int callbackId, double delta)
	{
		switch (callbackId)
		{
		case 1:
			IdleProcessing(delta);
			break;
		case 2:
			AttackProcessing(delta);
			break;
		case 3:
			RestoreProcessing(delta);
			break;
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && parent != null)
		{
			PhysicsProcessValidated(delta);
		}
	}

	private void PhysicsProcessValidated(double delta)
	{
		if (_idleStatePhysicsSuspended)
		{
			ApplyIdlePresentationTimeScaleIfChanged();
		}
		float timerRunScale = GetTimerRunScale();
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (parent.instance != null && (ulong)((long)currentPhysicsFrame - (long)Math.Max(0, _catPumpkinCheckFrame)) >= 30uL)
		{
			_catPumpkinCheckFrame = (int)currentPhysicsFrame;
			if ((parent.instance.physiqueTypeFlags & 0x100) != 0)
			{
				hasCatPumpkin = GodotObject.IsInstanceValid(parent.cell) && parent.cell.HasCharacter("PlantCatPumpkin");
			}
		}
		timerRunScale *= GetCatPumpkinFireRateScale();
		if (timer > 0f)
		{
			timer -= (float)delta * timerRunScale;
		}
		if (isCheck)
		{
			checkIntreval--;
			isCheck = false;
		}
		else if (_idleStatePhysicsSuspended && timer <= 0f && checkIntreval > 0)
		{
			checkIntreval--;
		}
		UpdateIdleStatePhysicsSuspension();
		if (!WantsPhysicsProcess)
		{
			RefreshPhysicsProcessEligibility();
		}
	}

	private void ApplyIdlePresentationTimeScaleIfChanged()
	{
		double num = parent.timeScale * (double)spliceIdleAnimeTimeScale;
		if (_appliedIdlePresentationTimeScale == num)
		{
			return;
		}
		_appliedIdlePresentationTimeScale = num;
		if (sprite != null && spliceIdleAnimeClips != "")
		{
			sprite.timeScale = num;
		}
		if (!isSpliceSprite)
		{
			return;
		}
		for (int i = 0; i < _spliceSprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
			if (IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.timeScale = num;
			}
		}
	}

	private void UpdateIdleStatePhysicsSuspension()
	{
		bool flag = CanSuspendIdleStatePhysics && (timer > 0f || checkIntreval > 0 || isCheck);
		if (_idleStatePhysicsSuspended != flag)
		{
			_idleStatePhysicsSuspended = flag;
			RefreshStateMachineWorkEligibility();
			RefreshPhysicsProcessEligibility();
		}
	}

	private void MarkCheckPending()
	{
		if (!isCheck)
		{
			isCheck = true;
			RefreshPhysicsProcessEligibility();
		}
	}

	private float GetTimerRunScale()
	{
		if (TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame)
		{
			return (float)(parent?.buff?.GetAttackSpeedMultiplier() ?? 1.0);
		}
		if (parent == null)
		{
			return 0f;
		}
		return (float)(parent.timeScale * (double)timeScale * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0));
	}

	private float GetCatPumpkinFireRateScale()
	{
		if (!hasCatPumpkin)
		{
			return 1f;
		}
		return 2f;
	}

	private float GetIdleFireCheckOwnerGlobalX()
	{
		if (_idleCheckBatchActive)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (currentPhysicsFrame != 18446744073709551615uL)
			{
				return parent.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X;
			}
		}
		return parent.GetLogicalGlobalPosition().X;
	}

	public TowerDefenseCharacter GetTarget()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(parent) || !TryGetCheckAreaWorldRect(out var worldRect))
		{
			return null;
		}
		return instance.GetCharacterTargetNearestFromRect(parent, worldRect, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION, fliterGravestone: true);
	}

	public bool CanFire(TowerDefenseProjectileCreateData projectileData, int collectionFlag = -1, bool allowOutsideComponentBattlefield = false)
	{
		if ((!base.CanExecuteGameplay && !allowOutsideComponentBattlefield) || !alive || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		if (!IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning)
		{
			return false;
		}
		if (timer > 0f)
		{
			return false;
		}
		if (GetIdleFireCheckOwnerGlobalX() > groundRight)
		{
			return false;
		}
		MarkCheckPending();
		if (!_idleCheckBatchActive && checkIntreval > 0)
		{
			return false;
		}
		bool result = CheckTarget(projectileData, collectionFlag, checkInterval: true, allowOutsideComponentBattlefield);
		checkIntreval = Math.Max(0, checkIntervalMax);
		return result;
	}

	public bool CanFireCheckOnce(TowerDefenseProjectileCreateData projectileData, int collectionFlag = -1, bool allowOutsideComponentBattlefield = false)
	{
		if ((!base.CanExecuteGameplay && !allowOutsideComponentBattlefield) || !alive || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		if (!IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning)
		{
			return false;
		}
		if (parent.GetLogicalGlobalPosition().X > groundRight)
		{
			return false;
		}
		MarkCheckPending();
		return CheckTarget(projectileData, collectionFlag, checkInterval: false, allowOutsideComponentBattlefield);
	}

	public void CanFireCheckPairOnce(int primaryCollectionFlag, int secondaryCollectionFlag, out bool hasPrimaryTarget, out bool hasSecondaryTarget, bool allowOutsideComponentBattlefield = false)
	{
		hasPrimaryTarget = false;
		hasSecondaryTarget = false;
		if ((!base.CanExecuteGameplay && !allowOutsideComponentBattlefield) || !alive || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance) || !IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || parent.GetLogicalGlobalPosition().X > groundRight)
		{
			return;
		}
		MarkCheckPending();
		firstCharacter = null;
		RefreshGridRelativeCheckGeometry();
		if (HasOpposingFireCandidates(primaryCollectionFlag | secondaryCollectionFlag))
		{
			if (_checkRayResources.Count > 0)
			{
				CheckRayTargetPair(primaryCollectionFlag, secondaryCollectionFlag, out hasPrimaryTarget, out hasSecondaryTarget);
			}
			else if (_checkShapeResources.Count > 0)
			{
				CheckAreaTargetPair(primaryCollectionFlag, secondaryCollectionFlag, out hasPrimaryTarget, out hasSecondaryTarget);
			}
		}
	}

	public bool IsFireStateBusy()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			return stateHandle.StableId != "fire.idle";
		}
		return false;
	}

	public bool CheckTarget(TowerDefenseProjectileCreateData projectileData, int collectionFlag, bool checkInterval, bool allowOutsideComponentBattlefield = false)
	{
		if ((!base.CanExecuteGameplay && !allowOutsideComponentBattlefield) || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		firstCharacter = null;
		if (collectionFlag == -1)
		{
			collectionFlag = parent.instance.collisionFlags;
		}
		RefreshGridRelativeCheckGeometry();
		if (!HasOpposingFireCandidates(collectionFlag))
		{
			return false;
		}
		bool result = false;
		if (projectileData != null && (projectileData.fireMethodFlags & 0x20) != 0)
		{
			result = CheckTrackTarget(collectionFlag);
		}
		else if (_checkRayResources.Count > 0)
		{
			firstCharacter = null;
			long startTicks = TowerDefensePerfProfiler.Begin();
			int num = 0;
			try
			{
				for (int i = 0; i < _checkRayResources.Count; i++)
				{
					num++;
					if (IsInstanceValid(CheckRayHit(_checkRayResources[i], collectionFlag, i)))
					{
						result = true;
						break;
					}
				}
			}
			finally
			{
				EndFireMetric("fire.checkRayResources", startTicks, num);
			}
		}
		else if (_checkShapeResources.Count > 0)
		{
			result = CheckAreaTarget(collectionFlag, checkInterval);
		}
		else if (projectileData != null && checkAllLine)
		{
			result = CheckTrackTarget(collectionFlag);
		}
		return result;
	}

	private bool HasOpposingFireCandidates(int collectionFlag)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return instance.characterRegistry.HasOpposingAttackCandidatesMatchingMask(parent.gridPos.Y, restrictToLine: false, parent.camp, collectionFlag);
		}
		return false;
	}

	public bool CheckAreaTarget(int collectionFlag, bool checkInterval)
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		if (!TryGetCheckAreaWorldRect(out var worldRect))
		{
			return false;
		}
		bool flag = !checkAllLine && !HasGridRelativeCheckAreaRows;
		if (!instance.characterRegistry.HasOpposingAttackCandidatesMatchingMask(parent.gridPos.Y, flag, parent.camp, collectionFlag))
		{
			return false;
		}
		foreach (TowerDefenseCharacter item in instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(worldRect, parent.camp, flag ? parent.gridPos.Y : (-2147483648), flag))
		{
			if (!GodotObject.IsInstanceValid(item) || !GodotObject.IsInstanceValid(item.instance))
			{
				continue;
			}
			TargetRegistrationComponent targetRegistrationComponent = item.targetRegistrationComponent;
			if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased && item.instance.canBeCollection && !item.instance.invincible && (!FilterGravestone || !(item is TowerDefenseGravestone)) && item.targetRegistrationComponent.canProjectileCheck && (!(item is TowerDefenseZombie) || !IsGargantuarPhysiqueBlocked(canTargetGargantuar, item.instance.zombiePhysique)) && (!IsInstanceValid(mapControl) || (IsInstanceValid(mapControl) && item.GetLogicalGlobalPosition().X <= groundRight)) && (collectionFlag & item.instance.maskFlags) != 0 && parent.CanTarget(item) && (checkAllLine || HasGridRelativeCheckAreaRows || item.IsTargetableFromLine(parent.gridPos.Y)))
			{
				if (!checkHeight)
				{
					return true;
				}
				if ((int)item.instance.height >= Mathf.Min(2, (int)parent.instance.height) || item.instance.height > parent.instance.height)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void CheckAreaTargetPair(int primaryCollectionFlag, int secondaryCollectionFlag, out bool hasPrimaryTarget, out bool hasSecondaryTarget)
	{
		hasPrimaryTarget = false;
		hasSecondaryTarget = false;
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !TryGetCheckAreaWorldRect(out var worldRect))
		{
			return;
		}
		bool flag = !checkAllLine && !HasGridRelativeCheckAreaRows;
		if (!instance.characterRegistry.HasOpposingAttackCandidatesMatchingMask(parent.gridPos.Y, flag, parent.camp, primaryCollectionFlag | secondaryCollectionFlag))
		{
			return;
		}
		bool flag2 = IsInstanceValid(mapControl);
		foreach (TowerDefenseCharacter item in instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(worldRect, parent.camp, flag ? parent.gridPos.Y : (-2147483648), flag))
		{
			if (!GodotObject.IsInstanceValid(item) || !GodotObject.IsInstanceValid(item.instance))
			{
				continue;
			}
			TargetRegistrationComponent targetRegistrationComponent = item.targetRegistrationComponent;
			if (targetRegistrationComponent == null || targetRegistrationComponent.IsReleased || !item.instance.canBeCollection || item.instance.invincible || (FilterGravestone && item is TowerDefenseGravestone) || !item.targetRegistrationComponent.canProjectileCheck || (item is TowerDefenseZombie && IsGargantuarPhysiqueBlocked(canTargetGargantuar, item.instance.zombiePhysique)) || (flag2 && item.GetLogicalGlobalPosition().X > groundRight))
			{
				continue;
			}
			int maskFlags = item.instance.maskFlags;
			if (((primaryCollectionFlag | secondaryCollectionFlag) & maskFlags) != 0 && parent.CanTarget(item) && (checkAllLine || HasGridRelativeCheckAreaRows || item.IsTargetableFromLine(parent.gridPos.Y)) && (!checkHeight || (int)item.instance.height >= Mathf.Min(2, (int)parent.instance.height) || item.instance.height > parent.instance.height))
			{
				hasPrimaryTarget |= (primaryCollectionFlag & maskFlags) != 0;
				hasSecondaryTarget |= (secondaryCollectionFlag & maskFlags) != 0;
				if (hasPrimaryTarget)
				{
					break;
				}
			}
		}
	}

	private bool TryGetCheckAreaWorldRect(out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		bool flag = false;
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		Transform2D globalTransformForPhysicsFrame = parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery);
		for (int i = 0; i < _checkShapeResources.Count; i++)
		{
			AabbShape2DResource aabbShape2DResource = _checkShapeResources[i];
			if (GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.TryGetWorldRect(globalTransformForPhysicsFrame, out var rect))
			{
				worldRect = (flag ? worldRect.Merge(rect) : rect);
				flag = true;
			}
		}
		return flag;
	}

	public bool CheckTrackTarget(int collectionFlag)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(parent))
		{
			return instance.HasTrackTarget(parent, collectionFlag, canTargetGargantuar, groundRight);
		}
		return false;
	}

	public TowerDefenseCharacter CheckRayHit(AabbRay2DResource ray, int collectionFlag = -1, int runtimeIndex = -1)
	{
		if (!GodotObject.IsInstanceValid(ray) || !ray.Enabled)
		{
			return null;
		}
		Vector2 runtimeTargetPosition = ((runtimeIndex >= 0 && runtimeIndex < _runtimeRayTargets.Count) ? _runtimeRayTargets[runtimeIndex] : ray.TargetPosition);
		Transform2D checkRayLocalTransform = GetCheckRayLocalTransform(ray, runtimeIndex);
		if (!GodotObject.IsInstanceValid(parent) || !ray.TryGetWorldSegment(parent.GetGlobalTransformForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame), checkRayLocalTransform, runtimeTargetPosition, out var origin, out var target))
		{
			return null;
		}
		Vector2 vector = new Vector2(Mathf.Min(origin.X, target.X), Mathf.Min(origin.Y, target.Y));
		Vector2 vector2 = new Vector2(Mathf.Max(origin.X, target.X), Mathf.Max(origin.Y, target.Y));
		if (!TryFindRayHitWithAttackGrid(rayRect: new Rect2(vector, vector2 - vector), rayOrigin: origin, rayTargetWorld: target, collectionFlag: collectionFlag, bestTarget: out var bestTarget))
		{
			return null;
		}
		firstCharacter = bestTarget;
		return bestTarget;
	}

	private void CheckRayTargetPair(int primaryCollectionFlag, int secondaryCollectionFlag, out bool hasPrimaryTarget, out bool hasSecondaryTarget)
	{
		hasPrimaryTarget = false;
		hasSecondaryTarget = false;
		firstCharacter = null;
		TowerDefenseCharacter instance = null;
		for (int i = 0; i < _checkRayResources.Count; i++)
		{
			CheckRayHitPair(_checkRayResources[i], primaryCollectionFlag, secondaryCollectionFlag, i, out var primaryTarget, out var secondaryTarget);
			if (GodotObject.IsInstanceValid(primaryTarget))
			{
				hasPrimaryTarget = true;
				hasSecondaryTarget |= GodotObject.IsInstanceValid(secondaryTarget);
				firstCharacter = primaryTarget;
				return;
			}
			if (!GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(secondaryTarget))
			{
				instance = secondaryTarget;
				hasSecondaryTarget = true;
			}
		}
		firstCharacter = instance;
	}

	private void CheckRayHitPair(AabbRay2DResource ray, int primaryCollectionFlag, int secondaryCollectionFlag, int runtimeIndex, out TowerDefenseCharacter primaryTarget, out TowerDefenseCharacter secondaryTarget)
	{
		primaryTarget = null;
		secondaryTarget = null;
		if (GodotObject.IsInstanceValid(ray) && ray.Enabled && GodotObject.IsInstanceValid(parent))
		{
			Vector2 runtimeTargetPosition = ((runtimeIndex >= 0 && runtimeIndex < _runtimeRayTargets.Count) ? _runtimeRayTargets[runtimeIndex] : ray.TargetPosition);
			Transform2D checkRayLocalTransform = GetCheckRayLocalTransform(ray, runtimeIndex);
			if (ray.TryGetWorldSegment(parent.GetGlobalTransformForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame), checkRayLocalTransform, runtimeTargetPosition, out var origin, out var target))
			{
				Vector2 vector = new Vector2(Mathf.Min(origin.X, target.X), Mathf.Min(origin.Y, target.Y));
				Vector2 vector2 = new Vector2(Mathf.Max(origin.X, target.X), Mathf.Max(origin.Y, target.Y));
				TryFindRayHitPairWithAttackGrid(rayRect: new Rect2(vector, vector2 - vector), rayOrigin: origin, rayTargetWorld: target, primaryCollectionFlag: primaryCollectionFlag, secondaryCollectionFlag: secondaryCollectionFlag, primaryTarget: out primaryTarget, secondaryTarget: out secondaryTarget);
			}
		}
	}

	private void TryFindRayHitPairWithAttackGrid(Vector2 rayOrigin, Vector2 rayTargetWorld, Rect2 rayRect, int primaryCollectionFlag, int secondaryCollectionFlag, out TowerDefenseCharacter primaryTarget, out TowerDefenseCharacter secondaryTarget)
	{
		primaryTarget = null;
		secondaryTarget = null;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry) || !GodotObject.IsInstanceValid(parent) || !TryGetRayGridColumnRange(rayRect, rayOrigin, rayTargetWorld, out var startColumn, out var endColumn, out var stepColumn))
		{
			return;
		}
		if (TryGetHorizontalRayGridLine(rayOrigin, rayTargetWorld, out var targetLine))
		{
			if (TryGetHorizontalRayGridColumnRange(rayOrigin, rayTargetWorld, out startColumn, out endColumn, out stepColumn))
			{
				TryFindHorizontalRayHitPairWithAttackGrid(instance.characterRegistry, targetLine, startColumn, endColumn, stepColumn, rayOrigin, rayTargetWorld, primaryCollectionFlag, secondaryCollectionFlag, out primaryTarget, out secondaryTarget);
			}
			return;
		}
		int startLine;
		int endLine;
		int stepLine;
		if (checkAllLine)
		{
			if (!TryGetRayGridLineRange(rayRect, rayOrigin, rayTargetWorld, out startLine, out endLine, out stepLine))
			{
				return;
			}
		}
		else
		{
			startLine = parent.gridPos.Y;
			endLine = startLine;
			stepLine = 1;
		}
		bool includeAllLineCheck = !checkAllLine;
		bool visitRowsFirst = checkAllLine && Mathf.Abs(rayTargetWorld.Y - rayOrigin.Y) > Mathf.Abs(rayTargetWorld.X - rayOrigin.X);
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		RayAttackGridPairSelector selector = new RayAttackGridPairSelector
		{
			Owner = this,
			RayOrigin = rayOrigin,
			RayTargetWorld = rayTargetWorld,
			PrimaryCollectionFlag = primaryCollectionFlag,
			SecondaryCollectionFlag = secondaryCollectionFlag,
			PrimaryBestT = 3.4028235E+38f,
			SecondaryBestT = 3.4028235E+38f,
			TrackSecondary = true,
			PhysicsFrame = physicsFrameForCachedGameplayQuery
		};
		instance.characterRegistry.TrySelectAttackGridCharacterIntersectingRectForPhysicsFrameAcrossCells(physicsFrameForCachedGameplayQuery, rayRect, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, visitRowsFirst, includeAllLineCheck, parent.camp, ref selector, out var _, out var visitedCells);
		SampleRayGridStats(visitedCells, selector.CandidateCount);
		primaryTarget = selector.PrimaryMatch;
		secondaryTarget = selector.SecondaryMatch;
	}

	private bool TryFindRayHitWithAttackGrid(Vector2 rayOrigin, Vector2 rayTargetWorld, Rect2 rayRect, int collectionFlag, out TowerDefenseCharacter bestTarget)
	{
		bestTarget = null;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry) || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		if (!TryGetRayGridColumnRange(rayRect, rayOrigin, rayTargetWorld, out var startColumn, out var endColumn, out var stepColumn))
		{
			return false;
		}
		if (TryGetHorizontalRayGridLine(rayOrigin, rayTargetWorld, out var targetLine))
		{
			if (!TryGetHorizontalRayGridColumnRange(rayOrigin, rayTargetWorld, out startColumn, out endColumn, out stepColumn))
			{
				return false;
			}
			return TryFindHorizontalRayHitWithAttackGrid(instance.characterRegistry, targetLine, startColumn, endColumn, stepColumn, rayOrigin, rayTargetWorld, collectionFlag, out bestTarget);
		}
		int startLine;
		int endLine;
		int stepLine;
		if (checkAllLine)
		{
			if (!TryGetRayGridLineRange(rayRect, rayOrigin, rayTargetWorld, out startLine, out endLine, out stepLine))
			{
				return false;
			}
		}
		else
		{
			startLine = parent.gridPos.Y;
			endLine = startLine;
			stepLine = 1;
		}
		bool includeAllLineCheck = !checkAllLine;
		bool visitRowsFirst = checkAllLine && Mathf.Abs(rayTargetWorld.Y - rayOrigin.Y) > Mathf.Abs(rayTargetWorld.X - rayOrigin.X);
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		RayAttackGridSelector selector = new RayAttackGridSelector
		{
			Owner = this,
			RayOrigin = rayOrigin,
			RayTargetWorld = rayTargetWorld,
			CollectionFlag = collectionFlag,
			BestT = 3.4028235E+38f,
			PhysicsFrame = physicsFrameForCachedGameplayQuery
		};
		int visitedCells;
		bool num = instance.characterRegistry.TrySelectAttackGridCharacterIntersectingRectForPhysicsFrameAcrossCells(physicsFrameForCachedGameplayQuery, rayRect, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, visitRowsFirst, includeAllLineCheck, parent.camp, ref selector, out bestTarget, out visitedCells);
		SampleRayGridStats(visitedCells, selector.CandidateCount);
		if (num)
		{
			return GodotObject.IsInstanceValid(bestTarget);
		}
		return false;
	}

	private void TryFindHorizontalRayHitPairWithAttackGrid(TowerDefenseBattleCharacterRegistry registry, int targetLine, int startColumn, int endColumn, int stepColumn, Vector2 rayOrigin, Vector2 rayTargetWorld, int primaryCollectionFlag, int secondaryCollectionFlag, out TowerDefenseCharacter primaryTarget, out TowerDefenseCharacter secondaryTarget)
	{
		primaryTarget = null;
		secondaryTarget = null;
		bool includeAllLineCheck = !checkAllLine;
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		int num = 0;
		int num2 = 0;
		if (registry.HasOpposingAttackCandidatesMatchingMask(targetLine, restrictToLine: true, parent.camp, primaryCollectionFlag))
		{
			RayAttackGridSelector selector = new RayAttackGridSelector
			{
				Owner = this,
				RayOrigin = rayOrigin,
				RayTargetWorld = rayTargetWorld,
				CollectionFlag = primaryCollectionFlag,
				BestT = 3.4028235E+38f,
				PhysicsFrame = physicsFrameForCachedGameplayQuery,
				UseGridLineSemantics = true,
				StopOnFirstGridLineMatch = true
			};
			registry.TrySelectAttackGridCharacterInLineRangeForPhysicsFrame(physicsFrameForCachedGameplayQuery, targetLine, startColumn, endColumn, stepColumn, includeAllLineCheck, parent.camp, ref selector, out primaryTarget, out var visitedCells);
			num += visitedCells;
			num2 += selector.CandidateCount;
		}
		if (registry.HasOpposingAttackCandidatesMatchingMask(targetLine, restrictToLine: true, parent.camp, secondaryCollectionFlag))
		{
			RayAttackGridSelector selector2 = new RayAttackGridSelector
			{
				Owner = this,
				RayOrigin = rayOrigin,
				RayTargetWorld = rayTargetWorld,
				CollectionFlag = secondaryCollectionFlag,
				BestT = 3.4028235E+38f,
				PhysicsFrame = physicsFrameForCachedGameplayQuery,
				UseGridLineSemantics = true,
				StopOnFirstGridLineMatch = true
			};
			registry.TrySelectAttackGridCharacterInLineRangeForPhysicsFrame(physicsFrameForCachedGameplayQuery, targetLine, startColumn, endColumn, stepColumn, includeAllLineCheck, parent.camp, ref selector2, out secondaryTarget, out var visitedCells2);
			num += visitedCells2;
			num2 += selector2.CandidateCount;
		}
		SampleRayGridStats(num, num2);
	}

	private bool TryFindHorizontalRayHitWithAttackGrid(TowerDefenseBattleCharacterRegistry registry, int targetLine, int startColumn, int endColumn, int stepColumn, Vector2 rayOrigin, Vector2 rayTargetWorld, int collectionFlag, out TowerDefenseCharacter bestTarget)
	{
		bestTarget = null;
		if (!registry.HasOpposingAttackCandidatesMatchingMask(targetLine, restrictToLine: true, parent.camp, collectionFlag))
		{
			SampleRayGridStats(0, 0);
			return false;
		}
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		RayAttackGridSelector selector = new RayAttackGridSelector
		{
			Owner = this,
			RayOrigin = rayOrigin,
			RayTargetWorld = rayTargetWorld,
			CollectionFlag = collectionFlag,
			BestT = 3.4028235E+38f,
			PhysicsFrame = physicsFrameForCachedGameplayQuery,
			UseGridLineSemantics = true
		};
		int visitedCells;
		bool num = registry.TrySelectAttackGridCharacterInLineRangeForPhysicsFrame(physicsFrameForCachedGameplayQuery, targetLine, startColumn, endColumn, stepColumn, !checkAllLine, parent.camp, ref selector, out bestTarget, out visitedCells);
		SampleRayGridStats(visitedCells, selector.CandidateCount);
		if (num)
		{
			return GodotObject.IsInstanceValid(bestTarget);
		}
		return false;
	}

	private bool TryGetHorizontalRayGridLine(Vector2 rayOrigin, Vector2 rayTargetWorld, out int targetLine)
	{
		targetLine = 0;
		Vector2 vector = rayTargetWorld - rayOrigin;
		if (Mathf.Abs(vector.X) < 1E-06f || Mathf.Abs(vector.Y) > Mathf.Max(0.001f, Mathf.Abs(vector.X) * 1E-05f))
		{
			return false;
		}
		if (!checkAllLine)
		{
			targetLine = parent.gridPos.Y;
			return true;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.Y <= 0 || instance.gridSize.Y <= 0f)
		{
			return false;
		}
		targetLine = Mathf.Clamp(Mathf.FloorToInt((rayOrigin.Y - instance.gridBeginPos.Y) / instance.gridSize.Y) + 1, 1, instance.gridNum.Y);
		return true;
	}

	private static bool TryGetHorizontalRayGridColumnRange(Vector2 rayOrigin, Vector2 rayTargetWorld, out int startColumn, out int endColumn, out int stepColumn)
	{
		startColumn = 0;
		endColumn = 0;
		stepColumn = 1;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.X <= 0 || instance.gridSize.X <= 0f)
		{
			return false;
		}
		int min = -1;
		int max = instance.gridNum.X + 1;
		startColumn = Mathf.Clamp(Mathf.FloorToInt((rayOrigin.X - instance.gridBeginPos.X) / instance.gridSize.X) + 1, min, max);
		endColumn = Mathf.Clamp(Mathf.FloorToInt((rayTargetWorld.X - instance.gridBeginPos.X) / instance.gridSize.X) + 1, min, max);
		stepColumn = ((rayTargetWorld.X >= rayOrigin.X) ? 1 : (-1));
		if (stepColumn <= 0)
		{
			return startColumn >= endColumn;
		}
		return startColumn <= endColumn;
	}

	private static void SampleRayGridStats(int gridCells, int candidates)
	{
		TowerDefensePerfProfiler.Sample("fire.rayGridCells", gridCells);
		TowerDefensePerfProfiler.Sample("fire.rayCandidates", candidates);
	}

	private static bool TryGetRayGridColumnRange(Rect2 rayRect, Vector2 rayOrigin, Vector2 rayTargetWorld, out int startColumn, out int endColumn, out int stepColumn)
	{
		startColumn = 0;
		endColumn = 0;
		stepColumn = 1;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.X <= 0 || gridSize.X <= 0f)
		{
			return false;
		}
		Vector2 vector = rayRect.Position + rayRect.Size;
		float num = Mathf.Min(rayRect.Position.X, vector.X);
		float num2 = Mathf.Max(rayRect.Position.X, vector.X);
		int value = Mathf.FloorToInt((num - gridBeginPos.X) / gridSize.X);
		int value2 = Mathf.FloorToInt((num2 - gridBeginPos.X) / gridSize.X) + 2;
		value = Mathf.Clamp(value, -1, gridNum.X + 1);
		value2 = Mathf.Clamp(value2, -1, gridNum.X + 1);
		if (value > value2)
		{
			return false;
		}
		int value3 = Mathf.FloorToInt((rayOrigin.X - gridBeginPos.X) / gridSize.X) + 1;
		value3 = Mathf.Clamp(value3, value, value2);
		stepColumn = ((rayTargetWorld.X >= rayOrigin.X) ? 1 : (-1));
		if (stepColumn > 0)
		{
			startColumn = Mathf.Clamp(value3 - 1, value, value2);
			endColumn = value2;
		}
		else
		{
			startColumn = Mathf.Clamp(value3 + 1, value, value2);
			endColumn = value;
		}
		return true;
	}

	private static bool TryGetRayGridLineRange(Rect2 rayRect, Vector2 rayOrigin, Vector2 rayTargetWorld, out int startLine, out int endLine, out int stepLine)
	{
		startLine = 0;
		endLine = 0;
		stepLine = 1;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.Y <= 0 || gridSize.Y <= 0f)
		{
			return false;
		}
		Vector2 vector = rayRect.Position + rayRect.Size;
		float num = Mathf.Min(rayRect.Position.Y, vector.Y);
		float num2 = Mathf.Max(rayRect.Position.Y, vector.Y);
		int value = Mathf.FloorToInt((num - gridBeginPos.Y) / gridSize.Y);
		int value2 = Mathf.FloorToInt((num2 - gridBeginPos.Y) / gridSize.Y) + 2;
		value = Mathf.Clamp(value, 1, gridNum.Y);
		value2 = Mathf.Clamp(value2, 1, gridNum.Y);
		if (value > value2)
		{
			return false;
		}
		int value3 = Mathf.FloorToInt((rayOrigin.Y - gridBeginPos.Y) / gridSize.Y) + 1;
		value3 = Mathf.Clamp(value3, value, value2);
		stepLine = ((rayTargetWorld.Y >= rayOrigin.Y) ? 1 : (-1));
		if (stepLine > 0)
		{
			startLine = Mathf.Clamp(value3 - 1, value, value2);
			endLine = value2;
		}
		else
		{
			startLine = Mathf.Clamp(value3 + 1, value, value2);
			endLine = value;
		}
		return true;
	}

	public void Refresh()
	{
		float num = Math.Abs(fireIntervalOffset);
		timer = Math.Max(0f, fireInterval + (float)GD.RandRange((double)(0f - num) * 2.0, 0f - num));
		RefreshPhysicsProcessEligibility();
	}

	private Transform2D GetCheckRayLocalTransform(AabbRay2DResource ray, int runtimeIndex)
	{
		if (runtimeIndex < 0 || runtimeIndex >= _runtimeRayTransforms.Count)
		{
			if (!GodotObject.IsInstanceValid(ray))
			{
				return Transform2D.Identity;
			}
			return ray.LocalTransform;
		}
		return _runtimeRayTransforms[runtimeIndex];
	}

	private void EnsureResourceRayRuntimeCaches()
	{
		int num = checkRayResources?.Count ?? 0;
		bool flag = _checkRayResources.Count != num || _runtimeRayTargets.Count != num || _runtimeRayTransforms.Count != num || _runtimeRayTransformOverrides.Count != num;
		if (!flag)
		{
			for (int i = 0; i < num; i++)
			{
				if (_checkRayResources[i] != checkRayResources[i])
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			RefreshExportedArrayCaches();
		}
	}

	private void EnsureCheckShapeRuntimeCaches()
	{
		int num = checkShapeResources?.Count ?? 0;
		bool flag = _checkShapeResources.Count != num;
		if (!flag)
		{
			for (int i = 0; i < num; i++)
			{
				if (_checkShapeResources[i] != checkShapeResources[i])
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			RefreshExportedArrayCaches();
		}
	}

	public bool BindCheckRayToGridRow(int rayIndex, int relativeRow)
	{
		EnsureResourceRayRuntimeCaches();
		if (base.IsReleased || rayIndex < 0 || rayIndex >= _runtimeRayTransforms.Count)
		{
			return false;
		}
		_gridRelativeRayRows[rayIndex] = relativeRow;
		_gridRelativeGeometryDirty = true;
		return RefreshGridRelativeCheckGeometry();
	}

	public bool BindCheckAreaShapeToGridRow(int shapeIndex, int relativeRow)
	{
		EnsureCheckShapeRuntimeCaches();
		if (base.IsReleased || shapeIndex < 0 || shapeIndex >= _checkShapeResources.Count || !GodotObject.IsInstanceValid(_checkShapeResources[shapeIndex]))
		{
			return false;
		}
		_gridRelativeShapeRows[shapeIndex] = relativeRow;
		_gridRelativeGeometryDirty = true;
		return RefreshGridRelativeCheckGeometry();
	}

	private bool RefreshGridRelativeCheckGeometry()
	{
		if (_gridRelativeRayRows.Count == 0 && _gridRelativeShapeRows.Count == 0)
		{
			return true;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		float y = instance.gridSize.Y;
		if (!float.IsFinite(y) || y <= 0f)
		{
			y = instance.GetMapGridSize().Y;
		}
		if (!float.IsFinite(y) || y <= 0f)
		{
			return false;
		}
		if (!_gridRelativeGeometryDirty && Mathf.IsEqualApprox(_appliedGridRelativeGeometryHeight, y))
		{
			return true;
		}
		bool flag = true;
		_applyingGridRelativeGeometry = true;
		try
		{
			int value;
			int key;
			foreach (KeyValuePair<int, int> gridRelativeRayRow in _gridRelativeRayRows)
			{
				gridRelativeRayRow.Deconstruct(out value, out key);
				int index = value;
				int num = key;
				flag &= SetCheckRayLocalOriginY(index, y * (float)num);
			}
			foreach (KeyValuePair<int, int> gridRelativeShapeRow in _gridRelativeShapeRows)
			{
				gridRelativeShapeRow.Deconstruct(out key, out value);
				int shapeIndex = key;
				int num2 = value;
				flag &= SetCheckAreaShapeLocalOriginY(shapeIndex, y * (float)num2);
			}
		}
		finally
		{
			_applyingGridRelativeGeometry = false;
		}
		if (flag)
		{
			_appliedGridRelativeGeometryHeight = y;
			_gridRelativeGeometryDirty = false;
		}
		return flag;
	}

	private void ClearGridRelativeGeometryBindings()
	{
		_gridRelativeRayRows.Clear();
		_gridRelativeShapeRows.Clear();
		_appliedGridRelativeGeometryHeight = 0f / 0f;
		_gridRelativeGeometryDirty = false;
		_applyingGridRelativeGeometry = false;
	}

	public bool TryGetCheckRayLocalTransform(int index, out Transform2D localTransform)
	{
		EnsureResourceRayRuntimeCaches();
		if (index < 0 || index >= _runtimeRayTransforms.Count)
		{
			localTransform = Transform2D.Identity;
			return false;
		}
		localTransform = _runtimeRayTransforms[index];
		return true;
	}

	public bool SetCheckRayLocalTransform(int index, Transform2D localTransform)
	{
		EnsureResourceRayRuntimeCaches();
		if (index < 0 || index >= _runtimeRayTransforms.Count || !localTransform.IsFinite())
		{
			return false;
		}
		_runtimeRayTransforms[index] = localTransform;
		_runtimeRayTransformOverrides[index] = true;
		if (!_applyingGridRelativeGeometry && _gridRelativeRayRows.ContainsKey(index))
		{
			_gridRelativeGeometryDirty = true;
		}
		return true;
	}

	public bool SetCheckRayLocalOrigin(int index, Vector2 localOrigin)
	{
		if (!localOrigin.IsFinite() || !TryGetCheckRayLocalTransform(index, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = localOrigin;
		return SetCheckRayLocalTransform(index, localTransform);
	}

	public bool SetCheckRayLocalOriginY(int index, float localY)
	{
		if (!float.IsFinite(localY) || !TryGetCheckRayLocalTransform(index, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = new Vector2(localTransform.Origin.X, localY);
		return SetCheckRayLocalTransform(index, localTransform);
	}

	public bool SetCheckAreaShapeLocalOriginY(int shapeIndex, float localY)
	{
		EnsureCheckShapeRuntimeCaches();
		if (!float.IsFinite(localY) || shapeIndex < 0 || shapeIndex >= _checkShapeResources.Count)
		{
			return false;
		}
		AabbShape2DResource aabbShape2DResource = _checkShapeResources[shapeIndex];
		if (!GodotObject.IsInstanceValid(aabbShape2DResource))
		{
			return false;
		}
		Transform2D localTransform = aabbShape2DResource.LocalTransform;
		localTransform.Origin = new Vector2(localTransform.Origin.X, localY);
		aabbShape2DResource.LocalTransform = localTransform;
		if (!_applyingGridRelativeGeometry && _gridRelativeShapeRows.ContainsKey(shapeIndex))
		{
			_gridRelativeGeometryDirty = true;
		}
		return true;
	}

	public bool TryGetCheckAreaShapeLocalTransform(int shapeIndex, out Transform2D localTransform)
	{
		EnsureCheckShapeRuntimeCaches();
		if (shapeIndex < 0 || shapeIndex >= _checkShapeResources.Count || !GodotObject.IsInstanceValid(_checkShapeResources[shapeIndex]))
		{
			localTransform = Transform2D.Identity;
			return false;
		}
		localTransform = _checkShapeResources[shapeIndex].LocalTransform;
		return true;
	}

	public bool SetCheckAreaSegmentLengthX(int shapeIndex, float length)
	{
		if (!float.IsFinite(length) || shapeIndex < 0 || shapeIndex >= _checkShapeResources.Count || !GodotObject.IsInstanceValid(_checkShapeResources[shapeIndex]) || !(_checkShapeResources[shapeIndex].Geometry is SegmentShape2D segmentShape2D))
		{
			return false;
		}
		float num = (Mathf.IsZeroApprox(segmentShape2D.B.X) ? 1f : ((float)Mathf.Sign(segmentShape2D.B.X)));
		segmentShape2D.B = new Vector2(num * Mathf.Abs(length), segmentShape2D.B.Y);
		return true;
	}

	public bool TryGetCheckAreaSegmentEnd(int shapeIndex, out Vector2 end)
	{
		if (shapeIndex < 0 || shapeIndex >= _checkShapeResources.Count || !GodotObject.IsInstanceValid(_checkShapeResources[shapeIndex]) || !(_checkShapeResources[shapeIndex].Geometry is SegmentShape2D segmentShape2D))
		{
			end = Vector2.Zero;
			return false;
		}
		end = segmentShape2D.B;
		return true;
	}

	public bool ResetCheckRayLocalTransform(int index)
	{
		EnsureResourceRayRuntimeCaches();
		if (index < 0 || index >= _runtimeRayTransforms.Count)
		{
			return false;
		}
		AabbRay2DResource aabbRay2DResource = _checkRayResources[index];
		_runtimeRayTransforms[index] = (GodotObject.IsInstanceValid(aabbRay2DResource) ? aabbRay2DResource.LocalTransform : Transform2D.Identity);
		_runtimeRayTransformOverrides[index] = false;
		if (!_applyingGridRelativeGeometry && _gridRelativeRayRows.ContainsKey(index))
		{
			_gridRelativeGeometryDirty = true;
		}
		return true;
	}

	public void SetCheckLength(float _checkLength)
	{
		this._checkLength = _checkLength;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			if (_checkLength != -1f)
			{
				UpdateResourceRayRuntimeTargets(new Vector2(_checkLength * instance.GetMapGridSize().X, _checkLength * instance.GetMapGridSize().Y));
				UpdateAreaRuntimeSegmentLength(_checkLength * instance.GetMapGridSize().X);
			}
			else
			{
				UpdateResourceRayRuntimeTargets(Vector2.One * Math.Max(0f, defaultCheckLengthWorld));
				UpdateAreaRuntimeSegmentLength(Math.Max(0f, defaultCheckLengthWorld));
			}
		}
	}

	private void UpdateAreaRuntimeSegmentLength(float length)
	{
		SetCheckAreaSegmentLengthX(0, length);
	}

	private void UpdateResourceRayRuntimeTargets(Vector2 length)
	{
		for (int i = 0; i < _checkRayResources.Count; i++)
		{
			AabbRay2DResource aabbRay2DResource = _checkRayResources[i];
			Vector2 vector = (GodotObject.IsInstanceValid(aabbRay2DResource) ? aabbRay2DResource.TargetPosition : Vector2.Zero);
			_runtimeRayTargets[i] = ((vector.LengthSquared() >= 1E-06f) ? (vector.Normalized() * length) : Vector2.Zero);
		}
	}

	public TowerDefenseProjectile CreateProjectile(int posId, Vector2 velocity, TowerDefenseProjectileCreateData projectileData, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2 offset = default(Vector2), int offsetLine = 0, bool filterByLine = false, BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return null;
		}
		if (camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = parent.camp;
		}
		if (collisionFlags == -1)
		{
			collisionFlags = parent.instance.collisionFlags;
		}
		if (projectileData == null)
		{
			return null;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = projectileData.BuildConfig();
		if (towerDefenseProjectileConfig == null)
		{
			return null;
		}
		if (towerDefenseProjectileConfig.rotateFollowVelocity)
		{
			overrides.flipXOverride = false;
			overrides.spriteRotationOverride = null;
		}
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		if (posId >= 0 && posId < _firePosMarkers.Count)
		{
			Marker2D marker2D = _firePosMarkers[posId];
			if (IsInstanceValid(marker2D))
			{
				logicalGlobalPosition = parent.GetLogicalGlobalPosition(marker2D);
			}
		}
		double num = parent.GetGroundHeight(logicalGlobalPosition.Y) - parent.groundHeight;
		TowerDefenseCharacter towerDefenseCharacter = null;
		if ((towerDefenseProjectileConfig.fireMethodFlags & 0x20) != 0)
		{
			towerDefenseCharacter = TowerDefenseManager.Instance.GetProjectileInitialTrackTarget(logicalGlobalPosition, collisionFlags, camp, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION, FilterGravestone);
		}
		if ((towerDefenseProjectileConfig.fireMethodFlags & 2) != 0)
		{
			num -= parent.groundHeight;
			if (_checkShapeResources.Count > 0 || checkAllLine)
			{
				towerDefenseCharacter = FindCatapultTarget(collisionFlags, logicalGlobalPosition, offsetLine, filterByLine);
			}
			else if (IsInstanceValid(firstCharacter))
			{
				towerDefenseCharacter = firstCharacter;
			}
			if (filterByLine && !IsInstanceValid(towerDefenseCharacter))
			{
				return null;
			}
		}
		Vector2 vector = velocity * Mathf.Sign(parent.Scale.X * parent.transformPoint.Scale.X * parent.sprite.Scale.X);
		logicalGlobalPosition = ResolveSameCellProjectileSpawn(logicalGlobalPosition, vector, towerDefenseProjectileConfig);
		int num2 = Mathf.Clamp(parent.gridPos.Y + offsetLine, 1, TowerDefenseManager.Instance.GetMapGridNum().Y);
		Vector2I gridPos = new Vector2I(TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition).X, num2);
		ResolveOffsetLineRoute(logicalGlobalPosition, offsetLine, parent.gridPos.Y, num2, TowerDefenseManager.Instance.GetMapGridSize().Y, offsetLineTweenDuration, blockedOffsetLineX, out var routedPosition, out var yOffsetTarget, out var yOffsetDuration);
		if ((lockProjectileGridY || filterByLine) && !overrides.gridYOverride.HasValue)
		{
			overrides.gridYOverride = num2;
		}
		Rect2 rect = ComputeProjectileMapRect();
		BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			GD.PushError($"[BulletField:E_FIELD_UNAVAILABLE] projectile='{towerDefenseProjectileConfig.NameSN}' scene='{towerDefenseProjectileConfig.projectileScene?.ResourcePath ?? "<none>"}' reason='BulletField could not be mounted'");
			return null;
		}
		bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, routedPosition, vector, vector.Length(), parent, camp, gridPos, num2, rect, towerDefenseCharacter, num, parent.groundHeight, collisionFlags, overrides.fireLengthOverride ?? fireLength, checkHeight, checkAllLine && !filterByLine, overrides.useFall, overrides.useGravity, 1.5f, yOffsetTarget, yOffsetDuration, overrides);
		return null;
	}

	private Vector2 ResolveSameCellProjectileSpawn(Vector2 authoredPosition, Vector2 routedVelocity, TowerDefenseProjectileConfig projectileConfig)
	{
		if (!snapStraightProjectileToSameCellTarget || !GodotObject.IsInstanceValid(firstCharacter) || firstCharacter.gridPos != parent.gridPos || !GodotObject.IsInstanceValid(projectileConfig))
		{
			return authoredPosition;
		}
		int fireMethodFlags = projectileConfig.fireMethodFlags;
		int num = 1;
		int num2 = 34;
		if ((fireMethodFlags & num) == 0 || (fireMethodFlags & num2) != 0 || !firstCharacter.TryGetActiveWorldHitRect(out var rect))
		{
			return authoredPosition;
		}
		float num3 = Mathf.Sign(routedVelocity.X);
		bool num4;
		if (!(num3 > 0f))
		{
			if (!(num3 < 0f))
			{
				goto IL_00b3;
			}
			num4 = authoredPosition.X < rect.Position.X;
		}
		else
		{
			num4 = authoredPosition.X > rect.End.X;
		}
		if (num4)
		{
			return new Vector2(rect.GetCenter().X, authoredPosition.Y);
		}
		goto IL_00b3;
		IL_00b3:
		return authoredPosition;
	}

	internal static Rect2 ComputeProjectileMapRect()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || mapFeature.rect.Size.X <= 0f || mapFeature.rect.Size.Y <= 0f)
		{
			GD.PushError("[BulletField:E_MAP_BOUNDS_UNAVAILABLE] projectile map bounds are unavailable");
			return default(Rect2);
		}
		return mapFeature.rect;
	}

	private static int SpawnBulletField(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileConfig config, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, BulletFieldSpawnOverrides overrides)
	{
		BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			GD.PushError($"[BulletField:E_FIELD_UNAVAILABLE] projectile='{config?.NameSN}' scene='{config?.projectileScene?.ResourcePath ?? "<none>"}' reason='BulletField could not be mounted'");
			return -1;
		}
		Vector2 vel = velocity;
		Vector2I gridPos = TowerDefenseManager.Instance.GetMapGridPos(pos);
		int y = gridPos.Y;
		if (GodotObject.IsInstanceValid(character))
		{
			float s = character.Scale.X * (GodotObject.IsInstanceValid(character.transformPoint) ? character.transformPoint.Scale.X : 1f) * (GodotObject.IsInstanceValid(character.sprite) ? character.sprite.Scale.X : 1f);
			vel = velocity * Mathf.Sign(s);
			y = character.gridPos.Y;
			gridPos = new Vector2I(gridPos.X, y);
		}
		Rect2 rect = ComputeProjectileMapRect();
		return bulletField.TrySpawnFromConfig(config, pos, vel, vel.Length(), character, camp, gridPos, y, rect, target, height, character?.groundHeight ?? 0.0, collisionFlags, overrides.fireLengthOverride ?? (-1f), checkHeight: false, checkAll: false, useFall: false, useGravity: false, 1.5f, 0f, 0f, overrides);
	}

	public static bool TryPrepareProjectileBurstPositionByConfig(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 position, TowerDefenseProjectileConfig projectileConfig, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, out ProjectileBurstSpawnContext context)
	{
		context = default(ProjectileBurstSpawnContext);
		if (!GodotObject.IsInstanceValid(projectileConfig))
		{
			return false;
		}
		BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			GD.PushError($"[BulletField:E_FIELD_UNAVAILABLE] projectile='{projectileConfig.NameSN}' scene='{projectileConfig.projectileScene?.ResourcePath ?? "<none>"}' " + "reason='BulletField could not be mounted for burst'");
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			GD.PushError($"[BulletField:E_MANAGER_UNAVAILABLE] projectile='{projectileConfig.NameSN}' " + "reason='TowerDefenseManager is unavailable for burst routing'");
			return false;
		}
		Vector2I gridPosition = instance.GetMapGridPos(position);
		int y = gridPosition.Y;
		float velocityScaleX = 1f;
		double groundHeight = 0.0;
		if (GodotObject.IsInstanceValid(character))
		{
			velocityScaleX = Mathf.Sign(character.Scale.X * (GodotObject.IsInstanceValid(character.transformPoint) ? character.transformPoint.Scale.X : 1f) * (GodotObject.IsInstanceValid(character.sprite) ? character.sprite.Scale.X : 1f));
			y = character.gridPos.Y;
			gridPosition = new Vector2I(gridPosition.X, y);
			groundHeight = character.groundHeight;
		}
		context = new ProjectileBurstSpawnContext(bulletField, character, target, projectileConfig, position, gridPosition, y, ComputeProjectileMapRect(), height, groundHeight, collisionFlags, camp, velocityScaleX);
		return context.IsValid;
	}

	public static int SpawnPreparedProjectileBurstItem(in ProjectileBurstSpawnContext context, Vector2 velocity, float speed, BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		return SpawnPreparedProjectileBurstItem(in context, context.Config, velocity, speed, overrides);
	}

	public static int SpawnPreparedProjectileBurstItem(in ProjectileBurstSpawnContext context, TowerDefenseProjectileConfig projectileConfig, Vector2 velocity, float speed, BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (context.Field == null || projectileConfig == null)
		{
			return -1;
		}
		Vector2 vel = velocity * context.VelocityScaleX;
		float num = ((context.VelocityScaleX == 0f) ? 0f : speed);
		return context.Field.TrySpawnFromConfig(projectileConfig, context.Position, vel, num, context.Character, context.Camp, context.GridPosition, context.GridY, context.ProjectileMapRect, context.Target, context.Height, context.GroundHeight, context.CollisionFlags, overrides.fireLengthOverride ?? (-1f), checkHeight: false, checkAll: false, useFall: false, useGravity: false, 1.5f, 0f, 0f, overrides);
	}

	private static bool HasCreateDataOverride(TowerDefenseProjectileCreateData data)
	{
		if (data.behaviors.Count <= 0 && !data.overrideHitChestsScale && !data.overrideHitNutScale && !data.overrideHitFrozenScale && !data.overrideCatapultHeight && !data.overridePenetrateNum && !data.overridePenetrateOverBack && !data.overrideBackOutGround && !data.overrideBackDuration && !data.overrideRotateFollowVelocity && !data.rangeOverride && !data.overrideHitTargetEvent && !data.overrideHitCharacterEvent && !data.overrideHitGroundEvent && !(data.size != DefaultCreateDataSize))
		{
			return data.scale != DefaultCreateDataScale;
		}
		return true;
	}

	public TowerDefenseCharacter FindCatapultTarget(int collisionFlags, Vector2 pos, int offsetLine = 0, bool filterByLine = false)
	{
		TowerDefenseCharacter towerDefenseCharacter = null;
		bool filterGravestone = FilterGravestone;
		Rect2 worldRect;
		if (checkAllLine)
		{
			List<TowerDefenseCharacter> characterTarget = TowerDefenseManager.Instance.GetCharacterTarget(parent, checkLine: false, checkCollision: true, filterGravestone);
			if (filterByLine)
			{
				int line = Mathf.Clamp(parent.gridPos.Y + offsetLine, 1, TowerDefenseManager.Instance.GetMapGridNum().Y);
				List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
				foreach (TowerDefenseCharacter item in characterTarget)
				{
					if (item.IsTargetableFromLine(line, includeAllLineCheck: false))
					{
						list.Add(item);
					}
				}
				characterTarget.Clear();
				foreach (TowerDefenseCharacter item2 in list)
				{
					characterTarget.Add(item2);
				}
			}
			towerDefenseCharacter = (randomChoose ? FindRandomCatapultTarget(characterTarget, collisionFlags, filterGravestone) : ((!catapultFirstFar) ? TowerDefenseManager.Instance.GetCharacterTargetNearestFromArrayWithCollisionFlags(parent, collisionFlags, characterTarget, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION) : TowerDefenseManager.Instance.GetCharacterTargetFarthestFromArrayWithCollisionFlags(parent, collisionFlags, characterTarget, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION)));
		}
		else if (TryGetCheckAreaWorldRect(out worldRect))
		{
			List<TowerDefenseCharacter> list2 = (HasGridRelativeCheckAreaRows ? TowerDefenseManager.Instance.GetCharacterTargetFromRectWithCollisionFlags(parent, collisionFlags, worldRect, filterGravestone) : TowerDefenseManager.Instance.GetCharacterTargetLineFromRectWithCollisionFlags(parent, collisionFlags, worldRect, filterGravestone));
			towerDefenseCharacter = (randomChoose ? FindRandomCatapultTarget(list2, collisionFlags, filterGravestone) : ((!catapultFirstFar) ? TowerDefenseManager.Instance.GetCharacterTargetNearestFromArrayWithCollisionFlags(parent, collisionFlags, list2, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION) : TowerDefenseManager.Instance.GetCharacterTargetFarthestFromArrayWithCollisionFlags(parent, collisionFlags, list2, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION)));
		}
		if (!IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		if (towerDefenseCharacter is TowerDefenseZombie && IsGargantuarPhysiqueBlocked(canTargetGargantuar, towerDefenseCharacter.instance.zombiePhysique))
		{
			return null;
		}
		return towerDefenseCharacter;
	}

	private TowerDefenseCharacter FindRandomCatapultTarget(List<TowerDefenseCharacter> candidates, int collisionFlags, bool filterGravestone)
	{
		List<TowerDefenseCharacter> array = new List<TowerDefenseCharacter>(candidates);
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(TowerDefenseManager.Instance.GetCharacterTargetFromArrayWithCollisionFlags(parent, collisionFlags, array, checkLine: false, filterGravestone));
		if (!canTargetGargantuar)
		{
			list.RemoveAll((TowerDefenseCharacter character) => character is TowerDefenseZombie && IsGargantuarPhysiqueBlocked(canTargetGargantuar, character.instance.zombiePhysique));
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[GD.RandRange(0, list.Count - 1)];
	}

	public static TowerDefenseProjectile CreateProjectilePosition(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileCreateData projectileData, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (projectileData == null)
		{
			return null;
		}
		if (!HasCreateDataOverride(projectileData))
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = BulletField.BuildTemplateConfig(projectileData.projectileName, projectileData.skinName);
			if (towerDefenseProjectileConfig != null)
			{
				BulletFieldSpawnOverrides overrides2 = new BulletFieldSpawnOverrides
				{
					useFall = overrides.useFall,
					useGravity = overrides.useGravity,
					metaData = overrides.metaData,
					eventProjectiles = overrides.eventProjectiles,
					gridYOverride = overrides.gridYOverride,
					zOverride = overrides.zOverride,
					ySpeedOverride = overrides.ySpeedOverride,
					landOverSubscribed = overrides.landOverSubscribed,
					spawnSourceInstanceId = overrides.spawnSourceInstanceId,
					fireMethodFlagsOverride = (overrides.fireMethodFlagsOverride ?? projectileData.fireMethodFlags),
					damageFlagsOverride = projectileData.damageFlags,
					baseDamageOverride = ((projectileData.baseDamage >= 0.0) ? new double?(projectileData.baseDamage) : ((double?)null)),
					fireLengthOverride = overrides.fireLengthOverride,
					checkAllOverride = overrides.checkAllOverride,
					initialRotationOverride = overrides.initialRotationOverride,
					spriteRotationOverride = overrides.spriteRotationOverride,
					gravityOverride = overrides.gravityOverride,
					flipXOverride = overrides.flipXOverride,
					trackSearchIntervalOverride = overrides.trackSearchIntervalOverride,
					hitBoxScaleOverride = overrides.hitBoxScaleOverride,
					catapultTargetPositionOverride = overrides.catapultTargetPositionOverride,
					catapultStartPositionOverride = overrides.catapultStartPositionOverride,
					catapultSkyDrop = overrides.catapultSkyDrop,
					catapultSkyDropAscentHorizontalOffset = overrides.catapultSkyDropAscentHorizontalOffset,
					catapultSkyDropOffscreenWaitSeconds = overrides.catapultSkyDropOffscreenWaitSeconds,
					catapultSkyDropVisualRadius = overrides.catapultSkyDropVisualRadius,
					lifecycleOwnerSequence = overrides.lifecycleOwnerSequence,
					lifecycleSlot = overrides.lifecycleSlot,
					lifecycleSubscribed = overrides.lifecycleSubscribed,
					spawnTweenOffset = overrides.spawnTweenOffset,
					spawnTweenDuration = overrides.spawnTweenDuration,
					spawnTweenEase = overrides.spawnTweenEase,
					spawnTweenTrans = overrides.spawnTweenTrans,
					portalReleasedOverride = overrides.portalReleasedOverride,
					suppressGameplay = overrides.suppressGameplay,
					deprioritizeDisabledTargets = overrides.deprioritizeDisabledTargets
				};
				SpawnBulletField(character, target, height, pos, velocity, towerDefenseProjectileConfig, collisionFlags, camp, overrides2);
				return null;
			}
		}
		TowerDefenseProjectileConfig config = projectileData.BuildConfig();
		SpawnBulletField(character, target, height, pos, velocity, config, collisionFlags, camp, overrides);
		return null;
	}

	public static TowerDefenseProjectile CreateProjectilePositionWithConfig(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileConfig projectileConfig, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		SpawnBulletField(character, target, height, pos, velocity, projectileConfig, collisionFlags, camp, overrides);
		return null;
	}

	public static bool TryCreateProjectilePositionByConfig(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileConfig projectileConfig, out int bulletIndex, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		bulletIndex = SpawnBulletField(character, target, height, pos, velocity, projectileConfig, collisionFlags, camp, overrides);
		return bulletIndex >= 0;
	}

	public bool CanFireByName(string projectileName, int collectionFlag = -1, string skinName = "Default")
	{
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(GetCachedStringName(projectileName));
		towerDefenseProjectileCreateData.skinName = ((skinName == "Default") ? DefaultSkinName : GetCachedStringName(skinName));
		return CanFire(towerDefenseProjectileCreateData, collectionFlag);
	}

	public bool CanFireByData(TowerDefenseProjectileCreateData projectileData, int collectionFlag = -1, bool allowOutsideComponentBattlefield = false)
	{
		return CanFire(projectileData, collectionFlag, allowOutsideComponentBattlefield);
	}

	public bool CanFireCheckOnceByName(string projectileName, int collectionFlag = -1, string skinName = "Default")
	{
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(GetCachedStringName(projectileName));
		towerDefenseProjectileCreateData.skinName = ((skinName == "Default") ? DefaultSkinName : GetCachedStringName(skinName));
		return CanFireCheckOnce(towerDefenseProjectileCreateData, collectionFlag);
	}

	public bool CanFireCheckOnceByData(TowerDefenseProjectileCreateData projectileData, int collectionFlag = -1, bool allowOutsideComponentBattlefield = false)
	{
		return CanFireCheckOnce(projectileData, collectionFlag, allowOutsideComponentBattlefield);
	}

	public TowerDefenseProjectile CreateProjectileByName(int posId, Vector2 velocity, string projectileName, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2 offset = default(Vector2), string skinName = "Default", BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = parent.camp;
		}
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(GetCachedStringName(projectileName));
		towerDefenseProjectileCreateData.skinName = ((skinName == "Default") ? DefaultSkinName : GetCachedStringName(skinName));
		return CreateProjectile(posId, velocity, towerDefenseProjectileCreateData, collisionFlags, camp, offset, 0, filterByLine: false, overrides);
	}

	public TowerDefenseProjectile CreateProjectileByData(int posId, Vector2 velocity, TowerDefenseProjectileCreateData projectileData, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = parent.camp;
		}
		return CreateProjectile(posId, velocity, projectileData, collisionFlags, camp, offset, 0, filterByLine: false, overrides);
	}

	public TowerDefenseProjectile CreateProjectilePositionById(int id, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (!GodotObject.IsInstanceValid(parent) || id < 0 || id >= _fireChecks.Count || !GodotObject.IsInstanceValid(_fireChecks[id]) || !GodotObject.IsInstanceValid(_fireChecks[id].projectile))
		{
			return null;
		}
		if (camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = parent.camp;
		}
		TowerDefenseProjectileCreateData projectile = _fireChecks[id].projectile.GetProjectile();
		return CreateProjectilePosition(parent, target, height, pos, velocity, projectile, collisionFlags, camp, offset, overrides);
	}

	public static TowerDefenseProjectile CreateProjectilePositionByName(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, string projectileName, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), string skinName = "Default", BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(GetCachedStringName(projectileName));
		towerDefenseProjectileCreateData.skinName = ((skinName == "Default") ? DefaultSkinName : GetCachedStringName(skinName));
		return CreateProjectilePosition(character, target, height, pos, velocity, towerDefenseProjectileCreateData, collisionFlags, camp, offset, overrides);
	}

	public static TowerDefenseProjectile CreateProjectilePositionByData(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileCreateData projectileData, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		return CreateProjectilePosition(character, target, height, pos, velocity, projectileData, collisionFlags, camp, offset, overrides);
	}

	public static TowerDefenseProjectile CreateProjectilePositionByConfig(TowerDefenseCharacter character, TowerDefenseCharacter target, double height, Vector2 pos, Vector2 velocity, TowerDefenseProjectileConfig projectileConfig, int collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, Vector2 offset = default(Vector2), BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		return CreateProjectilePositionWithConfig(character, target, height, pos, velocity, projectileConfig, collisionFlags, camp, offset, overrides);
	}

	public void IdleEntered()
	{
		if (!alive || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (IsInstanceValid(sprite))
		{
			if (spliceIdleAnimeClips != "")
			{
				sprite.SetAnimation(spliceIdleAnimeClips, loop: true, 0.2);
			}
			if (isSpliceSprite)
			{
				int num = Mathf.Min(_spliceSprites.Count, _spliceSpriteIdleAnimeClips.Count);
				for (int i = 0; i < num; i++)
				{
					AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
					string text = _spliceSpriteIdleAnimeClips[i];
					if (IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.HasClip(text) && adobeAnimateSprite.clip != text)
					{
						adobeAnimateSprite.SetAnimation(text, loop: true, 0.2);
					}
				}
			}
		}
		if (parent.componentRunning && parent is TowerDefensePlant towerDefensePlant)
		{
			towerDefensePlant.Idle();
		}
		_appliedIdlePresentationTimeScale = 0.0 / 0.0;
		ApplyIdlePresentationTimeScaleIfChanged();
		UpdateIdleStatePhysicsSuspension();
	}

	public void IdleProcessing(double delta)
	{
		if ((parent is TowerDefensePlant && timer > 0f) || !CanDispatchStateProcessing || parent.instance == null)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (instance == null)
		{
			return;
		}
		ApplyIdlePresentationTimeScaleIfChanged();
		if (!instance.IsGameRunning() || !parent.inGame || !parent.componentAlive || parent.componentRunning || parent.die || parent.nearDie || (checkUse && !CanRunIdleFireCheck()))
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		int num = 0;
		try
		{
			_idleCheckBatchActive = true;
			if (preExtend != null && !preExtend.IsReleased && !preExtend.CanRun())
			{
				return;
			}
			int idleFireCheckPriorityPassCount = GetIdleFireCheckPriorityPassCount(airFirst, checkUse);
			for (int i = 0; i < idleFireCheckPriorityPassCount; i++)
			{
				for (int j = 0; j < _fireChecks.Count; j++)
				{
					num++;
					FireComponentCheckConfig fireComponentCheckConfig = _fireChecks[j];
					if (!GodotObject.IsInstanceValid(fireComponentCheckConfig) || !GodotObject.IsInstanceValid(fireComponentCheckConfig.projectile) || !IsIdleFireCheckInPriorityPass(ResolveEffectiveCheckCollisionFlags(fireComponentCheckConfig.GetCollisionFlags(), parent.instance.collisionFlags), i, idleFireCheckPriorityPassCount) || (checkUse && !fireComponentCheckConfig.CanFire(this)))
					{
						continue;
					}
					runningCheck = fireComponentCheckConfig;
					runningCheckId = j;
					if (!fireDirect)
					{
						if (CanPlayFireAnimation(fireAnimeClips))
						{
							if (parent is TowerDefensePlant)
							{
								parent.Component();
							}
							SetFireState(FireRuntimeState.Attack);
						}
					}
					else
					{
						Refresh();
						this.OnFireReady?.Invoke();
						FireConfiguredVolley();
					}
					return;
				}
			}
		}
		finally
		{
			_idleCheckBatchActive = false;
			if (parent is TowerDefensePlant)
			{
				checkIntreval = 0;
				isCheck = false;
				if (timer <= 0f)
				{
					Refresh();
				}
			}
			EndFireMetric("fire.idleChecks", startTicks, num);
		}
	}

	private bool CanRunIdleFireCheck()
	{
		if (timer > 0f)
		{
			return false;
		}
		if (checkIntreval > 0)
		{
			MarkCheckPending();
			return false;
		}
		return true;
	}

	public void IdleExited()
	{
		_appliedIdlePresentationTimeScale = 0.0 / 0.0;
		if (_idleStatePhysicsSuspended)
		{
			_idleStatePhysicsSuspended = false;
			RefreshStateMachineWorkEligibility();
			RefreshPhysicsProcessEligibility();
		}
	}

	public void AttackEntered()
	{
		if (!alive || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!CanPlayFireAnimation(fireAnimeClips))
		{
			SetFireState(FireRuntimeState.Idle);
			return;
		}
		if (readyConfirmProjectile)
		{
			long startTicks = TowerDefensePerfProfiler.Begin();
			int num = 0;
			try
			{
				for (int i = 0; i < _fireProjectiles.Count; i++)
				{
					num++;
					FireComponentFireProjectileConfig fireComponentFireProjectileConfig = _fireProjectiles[i];
					if (GodotObject.IsInstanceValid(fireComponentFireProjectileConfig) && (fireComponentFireProjectileConfig.fireNumSkip == -1 || currentFireNum < fireComponentFireProjectileConfig.fireNumSkip) && fireComponentFireProjectileConfig.checkProjectileId >= 0 && fireComponentFireProjectileConfig.checkProjectileId < _fireChecks.Count && _fireChecks[fireComponentFireProjectileConfig.checkProjectileId].projectile is FireComponentProjectileWeight fireComponentProjectileWeight)
					{
						fireComponentProjectileWeight.RefreshProjectile();
						this.OnConfirmProjectile?.Invoke(i, fireComponentProjectileWeight.readyProjectile);
					}
				}
			}
			finally
			{
				EndFireMetric("fire.readyConfirm", startTicks, num);
			}
		}
		Refresh();
		this.OnFireReady?.Invoke();
		sprite.SetAnimation(fireAnimeClips, loop: true, Math.Max(0f, fireAnimationStartPosition));
		if (!isSpliceSprite)
		{
			return;
		}
		long startTicks2 = TowerDefensePerfProfiler.Begin();
		int num2 = 0;
		try
		{
			int num3 = Mathf.Min(_spliceSprites.Count, _spliceSpriteFireAnimeClips.Count);
			for (int j = 0; j < num3; j++)
			{
				num2++;
				AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[j];
				string clipName = _spliceSpriteFireAnimeClips[j];
				if (IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.HasClip(clipName))
				{
					adobeAnimateSprite.SetAnimation(clipName, loop: true, Math.Max(0f, spliceAnimationStartPosition));
				}
			}
		}
		finally
		{
			EndFireMetric("fire.spliceFireAnimation", startTicks2, num2);
		}
	}

	public void AttackProcessing(double delta)
	{
		if (!CanDispatchStateProcessing || parent.die || parent.nearDie)
		{
			return;
		}
		if (!alive || !parent.componentAlive)
		{
			SetFireState(FireRuntimeState.Idle);
		}
		double num = Math.Max(0.0001, fireIntervalBase);
		double num2 = Math.Max(0.0001, (double)fireInterval + num / 3.0);
		double num3 = (TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame ? 1.0 : parent.timeScale) * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0) * (double)fireAnimeTimeScale * (num + num / 3.0) / num2 * (double)GetCatPumpkinFireRateScale();
		sprite.timeScale = num3;
		foreach (AdobeAnimateSprite spliceSprite in _spliceSprites)
		{
			if (IsInstanceValid(spliceSprite))
			{
				spliceSprite.timeScale = num3;
			}
		}
	}

	public void AttackExited()
	{
	}

	public void RestoreEntered()
	{
		if (alive)
		{
			if (IsInstanceValid(sprite) && restoreAnimeClips != "")
			{
				sprite.SetAnimation(restoreAnimeClips, loop: true, Math.Max(0f, restoreAnimationStartPosition));
			}
			this.OnRestore?.Invoke();
		}
	}

	public void RestoreProcessing(double delta)
	{
		if (CanDispatchStateProcessing && !parent.die && !parent.nearDie)
		{
			if (!alive || !parent.componentAlive)
			{
				SetFireState(FireRuntimeState.Idle);
			}
			if (GodotObject.IsInstanceValid(sprite.flashAnimeData))
			{
				double num = Math.Max(0.0001, sprite.flashAnimeData.frameRate);
				double num2 = (double)(sprite.clipRange.Y - sprite.clipRange.X) / num;
				sprite.timeScale = parent.timeScale * (num2 / Math.Max(0.0001, restoreTime));
			}
		}
	}

	public void RestoreExited()
	{
	}

	private bool HasTargetForRunningCheck()
	{
		if (!checkUse)
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(runningCheck))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		TowerDefenseProjectileCreateData projectileData = runningCheck.projectile?.GetProjectile();
		int collisionFlags = runningCheck.GetCollisionFlags();
		return CheckTarget(projectileData, collisionFlags, checkInterval: false);
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (!alive || !GodotObject.IsInstanceValid(parent) || parent.die || parent.nearDie)
		{
			return;
		}
		if (_cachedFireEventName != fireEventName)
		{
			_cachedFireEventName = fireEventName;
			_cachedFireEventNames = fireEventName.Split("&");
		}
		if (System.Array.IndexOf(_cachedFireEventNames, command) >= 0)
		{
			if (!HasTargetForRunningCheck())
			{
				currentFireNum = 0;
				SetFireState(FireRuntimeState.Idle);
				return;
			}
			currentFireEvent = command;
			FireConfiguredVolley();
			if (!fireNumAtOnce && fireOverEventName == "")
			{
				currentFireNum++;
				if (currentFireNum >= Math.Max(1, fireNum))
				{
					currentFireNum = 0;
				}
				else if (GodotObject.IsInstanceValid(sprite))
				{
					sprite.SetAnimation(fireAnimeClips, loop: true, Math.Max(0f, repeatFireAnimationStartPosition));
				}
			}
		}
		if (!fireNumAtOnce && fireOverEventName != "" && command == fireOverEventName)
		{
			currentFireNum++;
			if (currentFireNum >= Math.Max(1, fireNum))
			{
				currentFireNum = 0;
			}
			else if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.SetAnimation(fireAnimeClips, loop: true, Math.Max(0f, repeatFireAnimationStartPosition));
			}
		}
	}

	private void FireConfiguredVolley()
	{
		if (!fireNumAtOnce)
		{
			Fire();
			return;
		}
		int num = Math.Max(1, fireNum);
		for (int i = 0; i < num; i++)
		{
			currentFireNum = i;
			Fire(i == 0);
		}
		currentFireNum = 0;
	}

	public void Fire()
	{
		Fire(playAudio: true);
	}

	private void Fire(bool playAudio)
	{
		if (!base.CanExecuteGameplay || !alive || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return;
		}
		if (playAudio && !string.IsNullOrEmpty(fireAudioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(fireAudioName);
		}
		bool hasOffsetLine = _hasOffsetLine;
		long startTicks = TowerDefensePerfProfiler.Begin();
		int num = 0;
		try
		{
			for (int i = 0; i < _fireProjectiles.Count; i++)
			{
				num++;
				FireComponentFireProjectileConfig fireComponentFireProjectileConfig = _fireProjectiles[i];
				if (!GodotObject.IsInstanceValid(fireComponentFireProjectileConfig) || (fireComponentFireProjectileConfig.fireEventNeed != "" && currentFireEvent != fireComponentFireProjectileConfig.fireEventNeed) || (fireComponentFireProjectileConfig.fireNumSkip != -1 && currentFireNum >= fireComponentFireProjectileConfig.fireNumSkip))
				{
					continue;
				}
				if (onlyEmitSignal)
				{
					EmitFireVolley();
					continue;
				}
				Vector2 velocity = fireComponentFireProjectileConfig.speed * Vector2.FromAngle(Mathf.DegToRad(fireComponentFireProjectileConfig.dir));
				if (fireComponentFireProjectileConfig.checkProjectileId < 0 || fireComponentFireProjectileConfig.checkProjectileId >= _fireChecks.Count)
				{
					continue;
				}
				FireComponentCheckConfig fireComponentCheckConfig = _fireChecks[fireComponentFireProjectileConfig.checkProjectileId];
				if (!GodotObject.IsInstanceValid(fireComponentCheckConfig) || !GodotObject.IsInstanceValid(fireComponentCheckConfig.projectile))
				{
					continue;
				}
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = ((!readyConfirmProjectile || currentFireNum != 0 || !(fireComponentCheckConfig.projectile is FireComponentProjectileWeight fireComponentProjectileWeight)) ? fireComponentCheckConfig.projectile.GetProjectile() : fireComponentProjectileWeight.readyProjectile);
				if (towerDefenseProjectileCreateData == null)
				{
					continue;
				}
				int collisionFlags = 0;
				if (useCollisionEveryPos)
				{
					if (!fireComponentCheckConfig.useParentCollision)
					{
						collisionFlags = fireComponentCheckConfig.collisionFlags;
					}
				}
				else
				{
					collisionFlags = (IsInstanceValid(runningCheck) ? runningCheck.GetCollisionFlags() : fireComponentCheckConfig.GetCollisionFlags());
				}
				float projectileBodyScaleX = GetProjectileBodyScaleX(parent, fireComponentFireProjectileConfig.projectileFlip);
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					flipXOverride = (projectileBodyScaleX < 0f),
					spriteRotationOverride = Mathf.DegToRad(fireComponentFireProjectileConfig.dir)
				};
				if (this.OnPrepareProjectileData != null)
				{
					towerDefenseProjectileCreateData = (towerDefenseProjectileCreateData.Duplicate(deep: true) as TowerDefenseProjectileCreateData) ?? towerDefenseProjectileCreateData;
					this.OnPrepareProjectileData(i, towerDefenseProjectileCreateData);
				}
				CreateProjectile(fireComponentFireProjectileConfig.firePosId, velocity, towerDefenseProjectileCreateData, collisionFlags, parent.camp, Vector2.Zero, fireComponentFireProjectileConfig.offsetLine, hasOffsetLine, overrides);
				EmitFireVolley();
			}
		}
		finally
		{
			EndFireMetric("fire.projectileLoop", startTicks, num);
		}
	}

	private void EmitFireVolley()
	{
		if (this.OnFireVolley != null)
		{
			_fireVolleySequence++;
			if (_fireVolleySequence <= 0)
			{
				_fireVolleySequence = 1L;
			}
			this.OnFireVolley(NetworkDeterministicSeed.ForCharacterEvent(parent?.syncId ?? (-1), _fireVolleySequence, 4051590893uL));
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (!alive)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		if (restoreAnimeClips != "" && clip == restoreAnimeClips)
		{
			SetFireState(FireRuntimeState.Idle);
		}
		if (isSpliceSprite && _spliceFireClipIndices.TryGetValue(clip, out var value) && value >= 0 && value < _spliceSprites.Count && value < _spliceSpriteIdleAnimeClips.Count && IsInstanceValid(_spliceSprites[value]))
		{
			_spliceSprites[value].SetAnimation(_spliceSpriteIdleAnimeClips[value], loop: true, Math.Max(0f, spliceAnimationStartPosition));
		}
		if (_fireAnimeClipSet.Contains(clip) && currentFireNum == 0)
		{
			this.OnFireOver?.Invoke();
			if (restoreAnimeClips == "")
			{
				SetFireState(FireRuntimeState.Idle);
			}
			else if (!_restoreTransitionPending)
			{
				_restoreTransitionPending = true;
				Callable.From(RequestRestoreStateDeferred).CallDeferred();
			}
		}
	}

	private void RequestRestoreStateDeferred()
	{
		_restoreTransitionPending = false;
		if (base.Alive && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && currentFireNum == 0)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SetFireState(FireRuntimeState.Restore);
			}
		}
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "timer", timer },
			{ "isCheck", isCheck },
			{ "checkIntreval", checkIntreval },
			{ "runningCheckId", runningCheckId },
			{ "currentFireNum", currentFireNum },
			{ "currentFireEvent", currentFireEvent },
			{ "fireVolleySequence", _fireVolleySequence },
			{ "hasCatPumpkin", hasCatPumpkin },
			{ "fireInterval", fireInterval },
			{ "fireIntervalBase", fireIntervalBase },
			{ "alive", alive }
		};
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		if (IsInstanceValid(sprite))
		{
			dictionary["animationState"] = sprite.ExportSpriteSave();
		}
		if (_spliceSprites.Count > 0)
		{
			Array<Dictionary> array = new Array<Dictionary>();
			for (int i = 0; i < _spliceSprites.Count; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
				array.Add(IsInstanceValid(adobeAnimateSprite) ? adobeAnimateSprite.ExportSpriteSave() : new Dictionary());
			}
			dictionary["spliceAnimationStates"] = array;
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		_progressAnimationStateMissing = !_data.ContainsKey("animationState");
		timer = _data.GetValueOrDefault("timer", 0.0).AsSingle();
		isCheck = _data.GetValueOrDefault("isCheck", false).AsBool();
		RefreshPhysicsProcessEligibility();
		checkIntreval = _data.GetValueOrDefault("checkIntreval", 0).AsInt32();
		runningCheckId = _data.GetValueOrDefault("runningCheckId", 0).AsInt32();
		currentFireNum = _data.GetValueOrDefault("currentFireNum", 0).AsInt32();
		currentFireEvent = _data.GetValueOrDefault("currentFireEvent", "").AsString();
		_fireVolleySequence = Math.Max(0L, _data.GetValueOrDefault("fireVolleySequence", 0L).AsInt64());
		hasCatPumpkin = _data.GetValueOrDefault("hasCatPumpkin", false).AsBool();
		fireInterval = _data.GetValueOrDefault("fireInterval", fireInterval).AsSingle();
		fireIntervalBase = _data.GetValueOrDefault("fireIntervalBase", fireIntervalBase).AsSingle();
		SetAlive(_data.GetValueOrDefault("alive", alive).AsBool());
		RestoreRunningCheckReference();
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(_data.GetValueOrDefault("state", "").AsString());
		}
		ImportAnimationSave(_data);
	}

	private void ImportAnimationSave(Dictionary data)
	{
		if (IsInstanceValid(sprite) && data.ContainsKey("animationState"))
		{
			Dictionary dictionary = data["animationState"].AsGodotDictionary();
			if (dictionary.Count > 0)
			{
				sprite.ImportSpriteSave(dictionary);
			}
		}
		if (!data.ContainsKey("spliceAnimationStates"))
		{
			return;
		}
		Array<Dictionary> array = data["spliceAnimationStates"].AsGodotArray<Dictionary>();
		int num = Math.Min(_spliceSprites.Count, array.Count);
		for (int i = 0; i < num; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spliceSprites[i];
			Dictionary dictionary2 = array[i];
			if (IsInstanceValid(adobeAnimateSprite) && dictionary2 != null && dictionary2.Count > 0)
			{
				adobeAnimateSprite.ImportSpriteSave(dictionary2);
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "timer", timer },
			{ "isCheck", isCheck },
			{ "checkIntreval", checkIntreval },
			{ "runningCheckId", runningCheckId },
			{ "currentFireNum", currentFireNum },
			{ "currentFireEvent", currentFireEvent },
			{ "fireVolleySequence", _fireVolleySequence },
			{ "hasCatPumpkin", hasCatPumpkin },
			{ "alive", alive }
		};
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		timer = _data.GetValueOrDefault("timer", timer).AsSingle();
		isCheck = _data.GetValueOrDefault("isCheck", isCheck).AsBool();
		RefreshPhysicsProcessEligibility();
		checkIntreval = _data.GetValueOrDefault("checkIntreval", checkIntreval).AsInt32();
		runningCheckId = _data.GetValueOrDefault("runningCheckId", runningCheckId).AsInt32();
		currentFireNum = _data.GetValueOrDefault("currentFireNum", currentFireNum).AsInt32();
		currentFireEvent = _data.GetValueOrDefault("currentFireEvent", currentFireEvent).AsString();
		long val = _data.GetValueOrDefault("fireVolleySequence", _fireVolleySequence).AsInt64();
		_fireVolleySequence = Math.Max(_fireVolleySequence, val);
		hasCatPumpkin = _data.GetValueOrDefault("hasCatPumpkin", hasCatPumpkin).AsBool();
		SetAlive(_data.GetValueOrDefault("alive", alive).AsBool());
		RestoreRunningCheckReference();
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(_data.GetValueOrDefault("state", "").AsString());
		}
	}

	private void RestoreRunningCheckReference()
	{
		runningCheck = ((runningCheckId >= 0 && runningCheckId < _fireChecks.Count && GodotObject.IsInstanceValid(_fireChecks[runningCheckId])) ? _fireChecks[runningCheckId] : null);
	}

	private void ApplyStateName(string stateName)
	{
		if (string.IsNullOrEmpty(stateName))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				if (!SyncForceState(base.StateMachine, stateName))
				{
					_pendingStateName = stateName;
				}
				return;
			}
		}
		_pendingStateName = stateName;
	}

	private void ApplyPendingStateName()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingStateName))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingStateName))
			{
				_pendingStateName = "";
			}
		}
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId;
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}
}
