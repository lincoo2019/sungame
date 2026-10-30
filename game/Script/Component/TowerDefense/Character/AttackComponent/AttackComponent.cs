using System;
using System.Buffers;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class AttackComponent : CharacterComponentRuntime
{
	public delegate void AttackReadyEventHandler();

	public delegate void AttackEventHandler();

	public delegate void AttackSeededEventHandler(ulong randomSeed);

	public delegate void AttackDpsEventHandler(double delta);

	public delegate void AttackOverEventHandler();

	[Flags]
	private enum CheckShapeOverrideFlags : byte
	{
		None = 0,
		Enabled = 1,
		LocalTransform = 2,
		RectangleSize = 4,
		SegmentEnd = 8
	}

	private struct CheckShapeOverride
	{
		public CheckShapeOverrideFlags Flags;

		public bool Enabled;

		public Transform2D LocalTransform;

		public Vector2 RectangleSize;

		public Vector2 SegmentEnd;
	}

	private struct AttackTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public string AttackType;

		public AttackComponent Component;

		public TowerDefenseCharacter Parent;

		public int Line;

		public double GroundRight;

		public bool CheckLine;

		public bool CheckGrid;

		public int GridMinColumn;

		public int GridMaxColumn;

		public bool CheckVase;

		public bool CheckBowling;

		public bool FilterGravestone;

		public ulong PhysicsFrame;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.die || checkCharacter.nearDie || checkCharacter.isDestroy)
			{
				return false;
			}
			if (checkCharacter.instance.invincible)
			{
				return false;
			}
			if (!CanUseAsAttackTarget(checkCharacter, AttackType, Parent))
			{
				return false;
			}
			if ((double)checkCharacter.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X > GroundRight)
			{
				return false;
			}
			if (!Parent.CanTarget(checkCharacter))
			{
				return false;
			}
			if (!Parent.CanCollision(checkCharacter.instance.maskFlags))
			{
				return false;
			}
			if (IsSmashImmuneTarget(AttackType, checkCharacter))
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (checkCharacter is TowerDefenseCrater)
			{
				return false;
			}
			if (checkCharacter is TowerDefenseItem { canCheck: false })
			{
				if (!CheckVase)
				{
					return false;
				}
				if (!(checkCharacter is TowerDefenseVase))
				{
					return false;
				}
			}
			if (!CheckBowling && checkCharacter is TowerDefensePlantBowlingBase)
			{
				return false;
			}
			if (FilterGravestone && checkCharacter is TowerDefenseGravestone)
			{
				return false;
			}
			if (CheckGrid && Parent is TowerDefenseZombie && checkCharacter is TowerDefensePlant && !CanBypassGridWindowForBowling(checkCharacter, CheckBowling) && !PlantMatchesAttackGridWindow(checkCharacter, GridMinColumn, GridMaxColumn))
			{
				return false;
			}
			if (Component != null && !Component.TargetIntersectsEachCheckShape(checkCharacter))
			{
				return false;
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct AttackGridCellSelector : ITowerDefenseAttackGridTraversalSelector, ITowerDefenseCharacterRectSelector
	{
		public AttackTargetPredicate Predicate;

		public TowerDefenseCharacter SelectedMatch { get; private set; }

		public bool ShouldStopAfterCell => GodotObject.IsInstanceValid(SelectedMatch);

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			return Predicate.CanConsider(checkCharacter);
		}

		public bool Visit(TowerDefenseCharacter checkCharacter, ref TowerDefenseCharacter match)
		{
			match = checkCharacter;
			SelectedMatch = checkCharacter;
			return true;
		}

		public void CompleteCell()
		{
		}
	}

	private struct AttackAreaGridCellSelector : ITowerDefenseAttackGridTraversalSelector, ITowerDefenseCharacterRectSelector
	{
		public AttackAreaTargetPredicate Predicate;

		public TowerDefenseCharacter SelectedMatch { get; private set; }

		public bool ShouldStopAfterCell => GodotObject.IsInstanceValid(SelectedMatch);

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			return Predicate.CanConsider(checkCharacter);
		}

		public bool Visit(TowerDefenseCharacter checkCharacter, ref TowerDefenseCharacter match)
		{
			match = checkCharacter;
			SelectedMatch = checkCharacter;
			return true;
		}

		public void CompleteCell()
		{
		}
	}

	private struct AttackCharacterListPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public TowerDefenseCharacter Parent;

		public bool CheckLine;

		public int Line;

		public bool CheckGrid;

		public int GridMinColumn;

		public int GridMaxColumn;

		public bool CheckBowling;

		public bool FilterGravestone;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (!GodotObject.IsInstanceValid(checkCharacter) || !GodotObject.IsInstanceValid(checkCharacter.instance) || checkCharacter.die || checkCharacter.nearDie || checkCharacter.isDestroy || !checkCharacter.instance.canBeCollection || checkCharacter.instance.invincible || checkCharacter is TowerDefenseCrater || checkCharacter is TowerDefenseItem)
			{
				return false;
			}
			if (FilterGravestone && checkCharacter is TowerDefenseGravestone)
			{
				return false;
			}
			if (!CheckBowling && checkCharacter is TowerDefensePlantBowlingBase)
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (CheckGrid && Parent is TowerDefenseZombie && checkCharacter is TowerDefensePlant && !CanBypassGridWindowForBowling(checkCharacter, CheckBowling) && !PlantMatchesAttackGridWindow(checkCharacter, GridMinColumn, GridMaxColumn))
			{
				return false;
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct AttackAreaTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public string AttackType;

		public TowerDefenseCharacter Parent;

		public bool CheckLine;

		public int Line;

		public bool FilterGravestone;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (!GodotObject.IsInstanceValid(checkCharacter) || !GodotObject.IsInstanceValid(checkCharacter.instance) || checkCharacter.die || checkCharacter.nearDie || checkCharacter.isDestroy || checkCharacter.instance.invincible || !CanUseAsAttackTarget(checkCharacter, AttackType, Parent) || !Parent.CanTarget(checkCharacter) || !Parent.CanCollision(checkCharacter.instance.maskFlags) || IsSmashImmuneTarget(AttackType, checkCharacter) || checkCharacter is TowerDefenseCrater)
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (checkCharacter is TowerDefenseItem { canCheck: false })
			{
				return false;
			}
			if (FilterGravestone)
			{
				return !(checkCharacter is TowerDefenseGravestone);
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct TallOnlyAttackTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public string AttackType;

		public AttackComponent Component;

		public TowerDefenseCharacter Parent;

		public int Line;

		public double GroundRight;

		public bool CheckLine;

		public bool CheckGrid;

		public int GridMinColumn;

		public int GridMaxColumn;

		public ulong PhysicsFrame;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.die || checkCharacter.nearDie || checkCharacter.isDestroy)
			{
				return false;
			}
			if (checkCharacter.instance.invincible)
			{
				return false;
			}
			if (!checkCharacter.instance.canBeCollection)
			{
				return false;
			}
			if ((double)checkCharacter.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X > GroundRight)
			{
				return false;
			}
			if (!Parent.CanTarget(checkCharacter))
			{
				return false;
			}
			if (checkCharacter.instance.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
			{
				return false;
			}
			if (IsBiteImmune(checkCharacter, AttackType, Parent))
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (CheckGrid && Parent is TowerDefenseZombie && checkCharacter is TowerDefensePlant && !PlantMatchesAttackGridWindow(checkCharacter, GridMinColumn, GridMaxColumn))
			{
				return false;
			}
			if (Component != null && !Component.TargetIntersectsEachCheckShape(checkCharacter))
			{
				return false;
			}
			if (checkCharacter is TowerDefensePlant && GodotObject.IsInstanceValid(checkCharacter.cell) && GodotObject.IsInstanceValid(checkCharacter.cell.characterLadder))
			{
				return false;
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct CheckTallFallbackAttackTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public AttackComponent Component;

		public TowerDefenseCharacter Parent;

		public int Line;

		public double GroundRight;

		public bool CheckLine;

		public bool CheckGrid;

		public int GridMinColumn;

		public int GridMaxColumn;

		public bool CheckVase;

		public bool CheckBowling;

		public bool FilterGravestone;

		public string AttackType;

		public ulong PhysicsFrame;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.die || checkCharacter.nearDie || checkCharacter.isDestroy)
			{
				return false;
			}
			if (checkCharacter.instance.invincible)
			{
				return false;
			}
			if (!checkCharacter.instance.canBeCollection)
			{
				return false;
			}
			if ((double)checkCharacter.GetGlobalPositionForPhysicsFrame(PhysicsFrame).X > GroundRight)
			{
				return false;
			}
			if (checkCharacter.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
			{
				return false;
			}
			if (IsBiteImmune(checkCharacter, AttackType, Parent))
			{
				return false;
			}
			if (!Parent.CanTarget(checkCharacter))
			{
				return false;
			}
			if (!Parent.CanCollision(checkCharacter.instance.maskFlags))
			{
				return false;
			}
			if (IsSmashImmuneTarget(AttackType, checkCharacter))
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (checkCharacter is TowerDefenseCrater)
			{
				return false;
			}
			if (checkCharacter is TowerDefenseItem { canCheck: false })
			{
				if (AttackType != "Smash")
				{
					return false;
				}
				if (!(checkCharacter is TowerDefenseVase))
				{
					return false;
				}
				if (!CheckVase)
				{
					return false;
				}
			}
			if (FilterGravestone && checkCharacter is TowerDefenseGravestone)
			{
				return false;
			}
			if (!CheckBowling && checkCharacter is TowerDefensePlantBowlingBase)
			{
				return false;
			}
			if (CheckGrid && Parent is TowerDefenseZombie && checkCharacter is TowerDefensePlant && !CanBypassGridWindowForBowling(checkCharacter, CheckBowling) && !PlantMatchesAttackGridWindow(checkCharacter, GridMinColumn, GridMaxColumn))
			{
				return false;
			}
			if (Component != null && !Component.TargetIntersectsEachCheckShape(checkCharacter))
			{
				return false;
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct CheckTallAttackGridSelector : ITowerDefenseAttackGridTraversalSelector, ITowerDefenseCharacterRectSelector
	{
		public TallOnlyAttackTargetPredicate TallPredicate;

		public CheckTallFallbackAttackTargetPredicate FallbackPredicate;

		public bool FoundTall;

		public TowerDefenseCharacter SelectedMatch { get; private set; }

		public bool ShouldStopAfterCell => FoundTall;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (!TallPredicate.CanConsider(checkCharacter))
			{
				return FallbackPredicate.CanConsider(checkCharacter);
			}
			return true;
		}

		public bool Visit(TowerDefenseCharacter checkCharacter, ref TowerDefenseCharacter match)
		{
			if (TallPredicate.Matches(checkCharacter))
			{
				FoundTall = true;
				match = checkCharacter;
				SelectedMatch = checkCharacter;
				return true;
			}
			if (!GodotObject.IsInstanceValid(SelectedMatch) && FallbackPredicate.Matches(checkCharacter))
			{
				match = checkCharacter;
				SelectedMatch = checkCharacter;
			}
			return false;
		}

		public void CompleteCell()
		{
		}
	}

	private StateHandle _idleState;

	private StateHandle _attackState;

	private bool _stateSignalsConnected;

	public string attackType = "Eat";

	public bool useParentHitBox;

	private TowerDefenseCharacter _runtimeHitBoxSource;

	public int checkIntreval = 5;

	private Array<string> _attackAnimeClipsArray;

	public string attackEventName = "attack";

	public double attackAnimeTimeScale = 1.0;

	public double attackIntervalBase = 1.5;

	public double attackInterval = 1.5;

	public double attackIntervalOffset = 0.1;

	public string spliceIdleAnimeClips = "";

	public double spliceIdleAnimeTimeScale = 1.0;

	private Array<TowerDefenseCharacterEventBase> _eventList;

	private TowerDefenseCharacterEventBase[] _eventExecutionCache = System.Array.Empty<TowerDefenseCharacterEventBase>();

	public bool useZombieAttackCheck = true;

	public bool checkGrid = true;

	public bool useCheckAreaGridColumn;

	public bool checkEachShape;

	public bool checkLine;

	public bool checkTall;

	public bool checkVase;

	public bool checkBowling;

	public bool checkGravestone = true;

	public bool checkAll;

	public bool fliterLadder;

	private bool _bypassLadderFilterOnce;

	public string eatAudio = "Chomp";

	public string plantDefeatAudio = "Gulp";

	public double eatAudioInterval = 1.0;

	public bool playPlantDefeatAudio = true;

	public StringName attackStateEvent = "ToAttack";

	public StringName idleStateEvent = "ToIdle";

	public TowerDefenseCharacter parent;

	public TowerDefenseCharacter target;

	public double timeScale = 1.0;

	public double timer = 0.25;

	public double attackDpsTimer;

	public int checkIntrevalNow = 5;

	private long _attackEventSequence;

	public double groundRight;

	private AdobeAnimateSprite _connectedAnimationSprite;

	private AdobeAnimateSprite _sprite;

	private string _attackAnimeClips = "Attack";

	private AdobeAnimateSprite _animationAvailabilitySprite;

	private AdobeAnimateData _animationAvailabilityData;

	private string _animationAvailabilityClipName;

	private bool _animationAvailabilityResult;

	private bool _animationAvailabilityCached;

	private Action _animationAvailabilityChangedHandler;

	private bool _componentChangeConnected;

	private bool _progressAnimationStateMissing;

	private bool _legacyProgressRecoveryPending;

	private bool _configured;

	private Rect2 _checkRect;

	private bool _checkRectValid;

	private Transform2D _checkRectCacheTransform;

	private int _checkRectCacheGeometryRevision = -1;

	private TowerDefenseCharacter _checkRectCacheSource;

	private ulong _checkRectCacheSourceBoundsRevision;

	private int _checkGeometryRevision;

	private int _checkGeometryAvailabilityRevision = -1;

	private bool _hasEnabledConfiguredCheckShape;

	private bool _checkAreaEnabled = true;

	private int _checkAreaShapeCount;

	private CheckShapeOverride[] _checkShapeOverrides;

	private Array<Dictionary> _serializedGeometryOverridesCache;

	private int _serializedGeometryOverridesRevision = -1;

	private List<TowerDefenseCharacter> _reusableTargetList;

	private List<TowerDefenseCharacter> _reusableFilteredList;

	private List<TowerDefenseCharacter> _reusableCharList;

	private List<TowerDefenseCharacter> _reusableEffectTargetList;

	private static readonly ArrayPool<TowerDefenseCharacter> _smashAttackCellBufferPool = ArrayPool<TowerDefenseCharacter>.Shared;

	private readonly List<TowerDefenseCharacter> _smashOpposingZombieBuffer = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _smashOpposingZombieQueryBuffer = new List<TowerDefenseCharacter>();

	private const double VehicleSmashPlantDamage = 10000.0;

	internal override bool WantsPhysicsProcess => timer > 0.0;

	public AdobeAnimateSprite sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (_sprite != value)
			{
				_sprite = value;
				RefreshConfiguration();
			}
		}
	}

	public Array<string> attackAnimeClipsArray
	{
		get
		{
			Array<string> array = _attackAnimeClipsArray;
			if (array == null)
			{
				Array<string> obj = new Array<string> { "Attack" };
				Array<string> array2 = obj;
				_attackAnimeClipsArray = obj;
				array = array2;
			}
			return array;
		}
		set
		{
			_attackAnimeClipsArray = value;
		}
	}

	public string attackAnimeClips
	{
		get
		{
			return _attackAnimeClips;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_attackAnimeClips == value))
			{
				_attackAnimeClips = value;
				RefreshConfiguration();
			}
		}
	}

	public Array<TowerDefenseCharacterEventBase> eventList
	{
		get
		{
			return _eventList ?? (_eventList = new Array<TowerDefenseCharacterEventBase>());
		}
		set
		{
			_eventList = value;
		}
	}

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

	private AttackComponentDefinition Definition => base.ComponentDefinition as AttackComponentDefinition;

	protected override bool IsStateMachineDispatchEligible => CanProcessOwnAttackState();

	public int CheckAreaShapeCount => _checkAreaShapeCount;

	public event AttackReadyEventHandler OnAttackReady;

	public event AttackEventHandler OnAttack;

	public event AttackSeededEventHandler OnAttackSeeded;

	public event AttackDpsEventHandler OnAttackDps;

	public event AttackOverEventHandler OnAttackOver;

	private bool IsRemoteSyncedAttacker()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	private AdobeAnimateSprite GetAnimationSprite()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			return sprite;
		}
		if (CanUseParentAnimationSpriteFallback() && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.sprite))
		{
			return parent.sprite;
		}
		return null;
	}

	private bool CanUseParentAnimationSpriteFallback()
	{
		if (this.OnAttack == null && this.OnAttackDps == null && this.OnAttackReady == null)
		{
			return this.OnAttackOver != null;
		}
		return true;
	}

	private bool HasAnimationSprite()
	{
		return GodotObject.IsInstanceValid(GetAnimationSprite());
	}

	private bool HasConfiguredAttackAnimation()
	{
		if (HasAnimationSprite())
		{
			return !string.IsNullOrEmpty(attackAnimeClips);
		}
		return false;
	}

	private bool CanPlayAnimation(string clipName)
	{
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (!GodotObject.IsInstanceValid(animationSprite))
		{
			SetAnimationAvailabilityData(null);
			return false;
		}
		AdobeAnimateData flashAnimeData = animationSprite.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData))
		{
			SetAnimationAvailabilityData(null);
			return false;
		}
		if (string.IsNullOrEmpty(clipName))
		{
			return false;
		}
		if (_animationAvailabilityCached && _animationAvailabilitySprite == animationSprite && _animationAvailabilityData == flashAnimeData && string.Equals(_animationAvailabilityClipName, clipName, StringComparison.Ordinal))
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
			if (!animationSprite.HasClip(clipName2))
			{
				flag = false;
				break;
			}
		}
		_animationAvailabilitySprite = animationSprite;
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

	private bool CanRunAnimatedAttack()
	{
		return HasConfiguredAttackAnimation();
	}

	private bool CanProcessOwnAttackState()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			return !string.IsNullOrEmpty(attackAnimeClips);
		}
		if (parent is TowerDefensePlant && !string.IsNullOrEmpty(attackAnimeClips))
		{
			return true;
		}
		return false;
	}

	public void RefreshConfiguration()
	{
		RefreshStateMachineDispatchEligibility();
		EnsureAnimationSpriteEventsConnected();
	}

	private void EnsureAnimationSpriteEventsConnected()
	{
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (GodotObject.IsInstanceValid(_connectedAnimationSprite) && _connectedAnimationSprite == animationSprite)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_connectedAnimationSprite))
		{
			_connectedAnimationSprite.OnAnimeCompleted -= AnimeCompleted;
			_connectedAnimationSprite.OnAnimeEvent -= AnimeEvent;
		}
		_connectedAnimationSprite = null;
		if (!GodotObject.IsInstanceValid(animationSprite))
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_connectedAnimationSprite = animationSprite;
			_connectedAnimationSprite.OnAnimeCompleted += AnimeCompleted;
			_connectedAnimationSprite.OnAnimeEvent += AnimeEvent;
			if (!_componentChangeConnected && GodotObject.IsInstanceValid(parent))
			{
				parent.OnComponentChange += ComponentChange;
				_componentChangeConnected = true;
			}
		}
	}

	public bool TryGetCheckAreaWorldRect(out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (!_checkAreaEnabled)
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.HasHitBox)
		{
			worldRect = towerDefenseCharacter.WorldHitRect;
			return true;
		}
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		return TryGetCheckAreaWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery), out worldRect);
	}

	private bool TryGetCheckAreaWorldRect(Transform2D ownerTransform, out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		bool flag = false;
		for (int i = 0; i < CheckAreaShapeCount; i++)
		{
			if (TryGetCheckAreaShapeWorldRect(i, ownerTransform, out var worldRect2))
			{
				worldRect = (flag ? AabbShapeUtil.Union(worldRect, worldRect2) : worldRect2);
				flag = true;
			}
		}
		return flag;
	}

	public bool DrawCheckAreaCollisionPreview(Node2D canvas)
	{
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		bool flag = GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.HasHitBox;
		if (CheckAreaShapeCount == 0 && !flag)
		{
			return false;
		}
		if (!_checkAreaEnabled)
		{
			return true;
		}
		if (flag)
		{
			if (towerDefenseCharacter != parent)
			{
				DrawHitBoxSourceCollisionPreview(canvas, towerDefenseCharacter);
			}
			return true;
		}
		for (int i = 0; i < CheckAreaShapeCount; i++)
		{
			if (TryGetCheckAreaShapeResource(i, out var resource) && TryGetCheckAreaShapeLocalTransform(i, out var localTransform))
			{
				Vector2 size = default(Vector2);
				Vector2 endpoint = default(Vector2);
				bool hasRectangleSizeOverride = resource.Geometry is RectangleShape2D && TryGetCheckAreaRectangleSize(i, out size);
				bool hasSegmentEndOverride = resource.Geometry is SegmentShape2D && TryGetCheckAreaSegmentEnd(i, out endpoint);
				AabbCollisionResourceDebugDraw.DrawEffectiveShape(canvas, resource, _checkAreaEnabled && IsCheckAreaShapeEnabled(i, resource), localTransform, hasRectangleSizeOverride, size, hasSegmentEndOverride, endpoint);
			}
		}
		return true;
	}

	private static void DrawHitBoxSourceCollisionPreview(Node2D canvas, TowerDefenseCharacter hitBoxSource)
	{
		if (GodotObject.IsInstanceValid(canvas) && GodotObject.IsInstanceValid(hitBoxSource) && hitBoxSource.HasHitBox)
		{
			Rect2 worldHitRect = hitBoxSource.WorldHitRect;
			Vector2 globalPoint = worldHitRect.Position + worldHitRect.Size;
			Vector2[] array = new Vector2[4]
			{
				canvas.ToLocal(worldHitRect.Position),
				canvas.ToLocal(new Vector2(globalPoint.X, worldHitRect.Position.Y)),
				canvas.ToLocal(globalPoint),
				canvas.ToLocal(new Vector2(worldHitRect.Position.X, globalPoint.Y))
			};
			CharacterHitBoxDefinition hitBoxDefinition = hitBoxSource.HitBoxDefinition;
			Color color = hitBoxDefinition?.DebugFillColor ?? new Color(0.1f, 0.75f, 1f, 0.18f);
			Color color2 = hitBoxDefinition?.DebugOutlineColor ?? new Color(0.1f, 0.75f, 1f, 0.85f);
			float width = Mathf.Max(0.5f, hitBoxDefinition?.DebugLineWidth ?? 2f);
			if (color.A > 0f)
			{
				canvas.DrawColoredPolygon(array, color);
			}
			canvas.DrawPolyline(new Vector2[5]
			{
				array[0],
				array[1],
				array[2],
				array[3],
				array[0]
			}, color2, width, antialiased: true);
		}
	}

	public bool SetCheckAreaEnabled(bool enabled)
	{
		if (_checkAreaEnabled == enabled)
		{
			return true;
		}
		_checkAreaEnabled = enabled;
		MarkCheckGeometryChanged();
		return true;
	}

	public bool SetCheckAreaShapeEnabled(int shapeIndex, bool enabled)
	{
		if (!TryGetCheckAreaShapeResource(shapeIndex, out var _))
		{
			return false;
		}
		EnsureCheckShapeOverrides();
		ref CheckShapeOverride reference = ref _checkShapeOverrides[shapeIndex];
		reference.Enabled = enabled;
		reference.Flags |= CheckShapeOverrideFlags.Enabled;
		MarkCheckGeometryChanged();
		return true;
	}

	public bool SetCheckAreaSegmentLengthX(int shapeIndex, float length)
	{
		if (!float.IsFinite(length) || length < 0f || !TryGetCheckAreaSegment(shapeIndex, out var segment, out var endpoint))
		{
			return false;
		}
		float num = Mathf.Sign(endpoint.X);
		if (Mathf.IsZeroApprox(num))
		{
			num = (Mathf.IsZeroApprox(segment.B.X) ? 1f : ((float)Mathf.Sign(segment.B.X)));
		}
		return SetCheckAreaSegmentEnd(shapeIndex, new Vector2(num * length, endpoint.Y));
	}

	public bool SetCheckAreaSegmentEnd(int shapeIndex, Vector2 endpoint)
	{
		if (!endpoint.IsFinite() || !TryGetCheckAreaShapeResource(shapeIndex, out var resource) || !(resource.Geometry is SegmentShape2D))
		{
			return false;
		}
		EnsureCheckShapeOverrides();
		ref CheckShapeOverride reference = ref _checkShapeOverrides[shapeIndex];
		reference.SegmentEnd = endpoint;
		reference.Flags |= CheckShapeOverrideFlags.SegmentEnd;
		MarkCheckGeometryChanged();
		return true;
	}

	public bool TryGetCheckAreaSegmentEnd(int shapeIndex, out Vector2 endpoint)
	{
		SegmentShape2D segment;
		return TryGetCheckAreaSegment(shapeIndex, out segment, out endpoint);
	}

	public bool SetCheckAreaRectangleSize(int shapeIndex, Vector2 size)
	{
		if (!size.IsFinite() || size.X < 0f || size.Y < 0f || !TryGetCheckAreaShapeResource(shapeIndex, out var resource) || !(resource.Geometry is RectangleShape2D))
		{
			return false;
		}
		EnsureCheckShapeOverrides();
		ref CheckShapeOverride reference = ref _checkShapeOverrides[shapeIndex];
		reference.RectangleSize = size;
		reference.Flags |= CheckShapeOverrideFlags.RectangleSize;
		MarkCheckGeometryChanged();
		return true;
	}

	public bool SetCheckAreaRectangleWidth(int shapeIndex, float width)
	{
		if (!float.IsFinite(width) || width < 0f || !TryGetCheckAreaRectangleSize(shapeIndex, out var size))
		{
			return false;
		}
		return SetCheckAreaRectangleSize(shapeIndex, new Vector2(width, size.Y));
	}

	public bool TryGetCheckAreaRectangleSize(int shapeIndex, out Vector2 size)
	{
		size = default(Vector2);
		if (!TryGetCheckAreaShapeResource(shapeIndex, out var resource) || !(resource.Geometry is RectangleShape2D rectangleShape2D))
		{
			return false;
		}
		if (TryGetCheckShapeOverride(shapeIndex, out var runtimeOverride) && (runtimeOverride.Flags & CheckShapeOverrideFlags.RectangleSize) != CheckShapeOverrideFlags.None)
		{
			size = runtimeOverride.RectangleSize;
		}
		else
		{
			size = rectangleShape2D.Size;
		}
		return true;
	}

	public bool SetCheckAreaShapeLocalTransform(int shapeIndex, Transform2D localTransform)
	{
		if (!localTransform.IsFinite() || !TryGetCheckAreaShapeResource(shapeIndex, out var _))
		{
			return false;
		}
		EnsureCheckShapeOverrides();
		ref CheckShapeOverride reference = ref _checkShapeOverrides[shapeIndex];
		reference.LocalTransform = localTransform;
		reference.Flags |= CheckShapeOverrideFlags.LocalTransform;
		MarkCheckGeometryChanged();
		return true;
	}

	public bool SetCheckAreaShapeLocalOrigin(int shapeIndex, Vector2 localOrigin)
	{
		if (!localOrigin.IsFinite() || !TryGetCheckAreaShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = localOrigin;
		return SetCheckAreaShapeLocalTransform(shapeIndex, localTransform);
	}

	public bool SetCheckAreaShapeWorldOrigin(int shapeIndex, Vector2 worldOrigin)
	{
		if (!worldOrigin.IsFinite() || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		return SetCheckAreaShapeLocalOrigin(shapeIndex, parent.GetLogicalGlobalTransform(parent).AffineInverse() * worldOrigin);
	}

	public bool TryGetCheckAreaShapeLocalTransform(int shapeIndex, out Transform2D localTransform)
	{
		if (!TryGetCheckAreaShapeResource(shapeIndex, out var resource))
		{
			localTransform = Transform2D.Identity;
			return false;
		}
		if (TryGetCheckShapeOverride(shapeIndex, out var runtimeOverride) && (runtimeOverride.Flags & CheckShapeOverrideFlags.LocalTransform) != CheckShapeOverrideFlags.None)
		{
			localTransform = runtimeOverride.LocalTransform;
		}
		else
		{
			localTransform = resource.LocalTransform;
		}
		return true;
	}

	private bool TryGetCheckAreaShapeResource(int shapeIndex, out AabbShape2DResource resource)
	{
		resource = null;
		if (shapeIndex < 0 || shapeIndex >= CheckAreaShapeCount)
		{
			return false;
		}
		resource = Definition.checkShapeResources[shapeIndex];
		if (GodotObject.IsInstanceValid(resource))
		{
			return GodotObject.IsInstanceValid(resource.Geometry);
		}
		return false;
	}

	private bool TryGetCheckAreaSegment(int shapeIndex, out SegmentShape2D segment, out Vector2 endpoint)
	{
		segment = null;
		endpoint = default(Vector2);
		if (!TryGetCheckAreaShapeResource(shapeIndex, out var resource) || !(resource.Geometry is SegmentShape2D segmentShape2D))
		{
			return false;
		}
		segment = segmentShape2D;
		endpoint = ((TryGetCheckShapeOverride(shapeIndex, out var runtimeOverride) && (runtimeOverride.Flags & CheckShapeOverrideFlags.SegmentEnd) != CheckShapeOverrideFlags.None) ? runtimeOverride.SegmentEnd : segmentShape2D.B);
		return true;
	}

	private bool TryGetCheckAreaShapeWorldRect(int shapeIndex, Transform2D ownerTransform, out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (!TryGetCheckAreaShapeResource(shapeIndex, out var resource) || !IsCheckAreaShapeEnabled(shapeIndex, resource) || !TryGetCheckAreaShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		Transform2D transform = ownerTransform * localTransform;
		if (resource.Geometry is RectangleShape2D && TryGetCheckAreaRectangleSize(shapeIndex, out var size))
		{
			worldRect = AabbShapeUtil.ComputeRectangleWorldRect(transform, size);
			return true;
		}
		if (resource.Geometry is SegmentShape2D segmentShape2D && TryGetCheckAreaSegmentEnd(shapeIndex, out var endpoint))
		{
			return AabbShapeUtil.TryComputeSegmentWorldRect(transform, segmentShape2D.A, endpoint, out worldRect);
		}
		return AabbShapeUtil.TryComputeShapeWorldRect(resource.Geometry, transform, out worldRect);
	}

	private bool IsCheckAreaShapeEnabled(int shapeIndex, AabbShape2DResource resource)
	{
		if (!TryGetCheckShapeOverride(shapeIndex, out var runtimeOverride) || (runtimeOverride.Flags & CheckShapeOverrideFlags.Enabled) == 0)
		{
			return resource.Enabled;
		}
		return runtimeOverride.Enabled;
	}

	private bool TryGetCheckShapeOverride(int shapeIndex, out CheckShapeOverride runtimeOverride)
	{
		if (_checkShapeOverrides != null && shapeIndex >= 0 && shapeIndex < _checkShapeOverrides.Length)
		{
			runtimeOverride = _checkShapeOverrides[shapeIndex];
			return runtimeOverride.Flags != CheckShapeOverrideFlags.None;
		}
		runtimeOverride = default(CheckShapeOverride);
		return false;
	}

	private bool TargetIntersectsEachCheckShape(TowerDefenseCharacter candidate)
	{
		if (!checkEachShape)
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(candidate) || !candidate.HasHitBox || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		Rect2 worldHitRect = candidate.WorldHitRect;
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.HasHitBox)
		{
			return AabbShapeUtil.Intersects(towerDefenseCharacter.WorldHitRect, worldHitRect);
		}
		Transform2D checkRectCacheTransform = _checkRectCacheTransform;
		for (int i = 0; i < CheckAreaShapeCount; i++)
		{
			if (!TryGetCheckAreaShapeResource(i, out var resource) || !IsCheckAreaShapeEnabled(i, resource) || !TryGetCheckAreaShapeLocalTransform(i, out var localTransform))
			{
				continue;
			}
			Transform2D transform2D = checkRectCacheTransform * localTransform;
			Rect2 worldRect;
			if (resource.Geometry is SegmentShape2D segmentShape2D && TryGetCheckAreaSegmentEnd(i, out var endpoint))
			{
				if (AabbShapeUtil.SegmentIntersectsRect(transform2D * segmentShape2D.A, transform2D * endpoint, worldHitRect, out var _))
				{
					return true;
				}
			}
			else if (TryGetCheckAreaShapeWorldRect(i, checkRectCacheTransform, out worldRect) && AabbShapeUtil.Intersects(worldRect, worldHitRect))
			{
				return true;
			}
		}
		return false;
	}

	private void EnsureCheckShapeOverrides()
	{
		if (_checkShapeOverrides == null || _checkShapeOverrides.Length != CheckAreaShapeCount)
		{
			_checkShapeOverrides = new CheckShapeOverride[CheckAreaShapeCount];
		}
	}

	private void MarkCheckGeometryChanged()
	{
		_checkGeometryRevision++;
		InvalidateCheckRect();
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.QueueRedraw();
		}
	}

	private bool _UpdateCheckRect()
	{
		if (!_checkAreaEnabled)
		{
			_checkRect = default(Rect2);
			_checkRectValid = false;
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.HasHitBox)
		{
			ulong hitBoxBoundsRevision = towerDefenseCharacter.HitBoxBoundsRevision;
			if (_checkRectValid && _checkRectCacheSource == towerDefenseCharacter && _checkRectCacheSourceBoundsRevision == hitBoxBoundsRevision)
			{
				return true;
			}
			ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
			_checkRect = towerDefenseCharacter.WorldHitRect;
			_checkRectCacheTransform = towerDefenseCharacter.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery);
			_checkRectCacheGeometryRevision = towerDefenseCharacter.HitBoxDefinition?.GetGeometryHash() ?? 0;
			_checkRectCacheSource = towerDefenseCharacter;
			_checkRectCacheSourceBoundsRevision = hitBoxBoundsRevision;
			_checkRectValid = true;
			return true;
		}
		if (_checkAreaEnabled && CheckAreaShapeCount > 0 && GodotObject.IsInstanceValid(parent))
		{
			ulong physicsFrameForCachedGameplayQuery2 = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
			Transform2D globalTransformForPhysicsFrame = parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery2);
			if (_checkRectValid && _checkRectCacheSource == null && _checkRectCacheTransform == globalTransformForPhysicsFrame && _checkRectCacheGeometryRevision == _checkGeometryRevision)
			{
				return true;
			}
			if (TryGetCheckAreaWorldRect(globalTransformForPhysicsFrame, out _checkRect))
			{
				_checkRectCacheTransform = globalTransformForPhysicsFrame;
				_checkRectCacheGeometryRevision = _checkGeometryRevision;
				_checkRectCacheSource = null;
				_checkRectValid = true;
				return true;
			}
		}
		_checkRect = default(Rect2);
		_checkRectValid = false;
		return false;
	}

	public void InvalidateCheckRect()
	{
		_checkRectValid = false;
		_checkRectCacheGeometryRevision = -1;
		_checkRectCacheSource = null;
		_checkRectCacheSourceBoundsRevision = 0uL;
	}

	public void SetCheckHitBoxSource(TowerDefenseCharacter source)
	{
		_runtimeHitBoxSource = (GodotObject.IsInstanceValid(source) ? source : null);
		InvalidateCheckRect();
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.QueueRedraw();
		}
	}

	private TowerDefenseCharacter ResolveHitBoxSource()
	{
		if (GodotObject.IsInstanceValid(_runtimeHitBoxSource))
		{
			return _runtimeHitBoxSource;
		}
		if (!useParentHitBox || !GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		return parent;
	}

	private bool HasCheckGeometry()
	{
		if (!_checkAreaEnabled)
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.HasHitBox)
		{
			return true;
		}
		if (_checkGeometryAvailabilityRevision == _checkGeometryRevision)
		{
			return _hasEnabledConfiguredCheckShape;
		}
		_checkGeometryAvailabilityRevision = _checkGeometryRevision;
		_hasEnabledConfiguredCheckShape = false;
		for (int i = 0; i < CheckAreaShapeCount; i++)
		{
			if (TryGetCheckAreaShapeResource(i, out var resource) && IsCheckAreaShapeEnabled(i, resource))
			{
				_hasEnabledConfiguredCheckShape = true;
				break;
			}
		}
		return _hasEnabledConfiguredCheckShape;
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		RefreshConfiguration();
		if (TowerDefenseManager.Instance != null)
		{
			groundRight = TowerDefenseManager.Instance.GetMapGroundRight() + (double)TowerDefenseManager.Instance.GetMapGridSize().X;
		}
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		RefreshConfiguration();
	}

	private void ApplyDefinitionOnce()
	{
		if (_configured || Definition == null)
		{
			return;
		}
		AttackComponentDefinition definition = Definition;
		attackType = definition.attackType ?? "Eat";
		useParentHitBox = definition.useParentHitBox;
		_checkAreaShapeCount = definition.checkShapeResources?.Count ?? 0;
		_checkAreaEnabled = true;
		_checkShapeOverrides = null;
		_checkGeometryRevision = 0;
		_serializedGeometryOverridesCache = null;
		_serializedGeometryOverridesRevision = -1;
		checkIntreval = Math.Max(0, definition.checkIntreval);
		attackAnimeClipsArray = ((definition.attackAnimeClipsArray != null) ? definition.attackAnimeClipsArray.Duplicate(deep: true) : new Array<string>());
		_attackAnimeClips = definition.attackAnimeClips ?? "Attack";
		attackEventName = definition.attackEventName ?? "attack";
		attackAnimeTimeScale = definition.attackAnimeTimeScale;
		attackIntervalBase = definition.attackIntervalBase;
		attackInterval = definition.attackInterval;
		attackIntervalOffset = definition.attackIntervalOffset;
		spliceIdleAnimeClips = definition.spliceIdleAnimeClips ?? "";
		spliceIdleAnimeTimeScale = definition.spliceIdleAnimeTimeScale;
		eventList = ((definition.eventList != null) ? definition.eventList.Duplicate(deep: true) : new Array<TowerDefenseCharacterEventBase>());
		for (int i = 0; i < eventList.Count; i++)
		{
			if (eventList[i] != null)
			{
				eventList[i] = (TowerDefenseCharacterEventBase)eventList[i].Duplicate(deep: true);
			}
		}
		RefreshEventExecutionCache();
		useZombieAttackCheck = definition.useZombieAttackCheck;
		checkGrid = definition.checkGrid;
		useCheckAreaGridColumn = definition.useCheckAreaGridColumn;
		checkEachShape = definition.checkEachShape;
		checkLine = definition.checkLine;
		checkTall = definition.checkTall;
		checkVase = definition.checkVase;
		checkBowling = definition.checkBowling;
		checkGravestone = definition.checkGravestone;
		checkAll = definition.checkAll;
		fliterLadder = definition.fliterLadder;
		eatAudio = definition.eatAudio ?? "Chomp";
		plantDefeatAudio = definition.plantDefeatAudio ?? "Gulp";
		eatAudioInterval = definition.eatAudioInterval;
		playPlantDefeatAudio = definition.playPlantDefeatAudio;
		attackStateEvent = definition.attackStateEvent;
		idleStateEvent = definition.idleStateEvent;
		_configured = true;
	}

	private void ResolveReferences()
	{
		if (GodotObject.IsInstanceValid(parent) && Definition != null)
		{
			sprite = ResolveOwnerNode<AdobeAnimateSprite>(Definition.spritePath);
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

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("attack.idle");
		_attackState = base.StateMachine?.GetStateById("attack.attack");
		ConnectStateSignals();
		RefreshConfiguration();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		_idleState = null;
		_attackState = null;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_legacyProgressRecoveryPending = false;
		_progressAnimationStateMissing = false;
		ClearAnimationAvailabilityCache();
		if (GodotObject.IsInstanceValid(_connectedAnimationSprite))
		{
			_connectedAnimationSprite.OnAnimeCompleted -= AnimeCompleted;
			_connectedAnimationSprite.OnAnimeEvent -= AnimeEvent;
			_connectedAnimationSprite = null;
		}
		if (_componentChangeConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnComponentChange -= ComponentChange;
			_componentChangeConnected = false;
		}
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			_runtimeHitBoxSource = null;
		}
		target = null;
		_sprite = null;
		InvalidateCheckRect();
		parent = null;
	}

	protected override void OnReleased()
	{
		_legacyProgressRecoveryPending = false;
		_progressAnimationStateMissing = false;
		ClearAnimationAvailabilityCache();
		this.OnAttackReady = null;
		this.OnAttack = null;
		this.OnAttackSeeded = null;
		this.OnAttackDps = null;
		this.OnAttackOver = null;
		_runtimeHitBoxSource = null;
		target = null;
		_checkAreaShapeCount = 0;
		_checkShapeOverrides = null;
		_checkAreaEnabled = false;
		_checkGeometryRevision = 0;
		_serializedGeometryOverridesCache = null;
		_serializedGeometryOverridesRevision = -1;
		_sprite = null;
		_connectedAnimationSprite = null;
		_reusableTargetList?.Clear();
		_reusableFilteredList?.Clear();
		_reusableCharList?.Clear();
		_reusableEffectTargetList?.Clear();
		_eventExecutionCache = System.Array.Empty<TowerDefenseCharacterEventBase>();
		eventList.Clear();
		attackAnimeClipsArray.Clear();
		InvalidateCheckRect();
		parent = null;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			if (_idleState != null)
			{
				_idleState.Entered += IdleEntered;
				_idleState.Exited += IdleExited;
				_idleState.PhysicsProcessing += IdleProcessing;
			}
			if (_attackState != null)
			{
				_attackState.Entered += AttackEntered;
				_attackState.Exited += AttackExited;
				_attackState.PhysicsProcessing += AttackProcessing;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_idleState != null)
			{
				_idleState.Entered -= IdleEntered;
				_idleState.Exited -= IdleExited;
				_idleState.PhysicsProcessing -= IdleProcessing;
			}
			if (_attackState != null)
			{
				_attackState.Entered -= AttackEntered;
				_attackState.Exited -= AttackExited;
				_attackState.PhysicsProcessing -= AttackProcessing;
			}
			_stateSignalsConnected = false;
		}
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		int num;
		if (!remote && _progressAnimationStateMissing)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				num = ((base.StateMachine.CurrentStateHandle.StableId != "attack.idle") ? 1 : 0);
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
		if (!alive || !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree())
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
		{
			if (base.StateMachine.CurrentStateHandle.StableId != "attack.idle")
			{
				SendStateEvent(idleStateEvent);
			}
			else if (parent.componentRunning && parent is TowerDefensePlant towerDefensePlant)
			{
				towerDefensePlant.Idle();
			}
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (alive && GodotObject.IsInstanceValid(parent))
		{
			BatchUpdateValidated(delta);
		}
	}

	public void BatchUpdate(double delta)
	{
		if (alive && GodotObject.IsInstanceValid(parent))
		{
			TowerDefenseProcessModeDispatch.BeginFrame();
			BatchUpdateValidated(delta);
		}
	}

	internal void BatchUpdateValidated(double delta)
	{
		double num = parent.buff?.GetAttackSpeedMultiplier() ?? 1.0;
		double num2 = parent.timeScale * timeScale * num;
		if (TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame)
		{
			num2 = num;
		}
		if (timer > 0.0)
		{
			timer -= delta * num2;
			if (timer <= 0.0)
			{
				RefreshPhysicsProcessEligibility();
			}
		}
	}

	public void Refresh()
	{
		timer = attackInterval + GD.RandRange((0.0 - attackIntervalOffset) * 2.0, 0.0 - attackIntervalOffset);
		RefreshPhysicsProcessEligibility();
	}

	public bool CanAttack()
	{
		if (parent is TowerDefensePlant)
		{
			if (timer > 0.0)
			{
				return false;
			}
			Refresh();
			return CanAttack(targetSearchIntervalChecked: true);
		}
		return CanAttack(targetSearchIntervalChecked: false);
	}

	public bool CanAttackOnContact()
	{
		bool flag = GodotObject.IsInstanceValid(target);
		if (!CanAttack(targetSearchIntervalChecked: true))
		{
			if (flag)
			{
				return CanAttack(targetSearchIntervalChecked: true);
			}
			return false;
		}
		return true;
	}

	public bool CanAttackIgnoringLadderFilter()
	{
		bool flag = fliterLadder;
		bool bypassLadderFilterOnce = _bypassLadderFilterOnce;
		fliterLadder = true;
		_bypassLadderFilterOnce = true;
		try
		{
			return CanAttack(targetSearchIntervalChecked: true);
		}
		finally
		{
			fliterLadder = flag;
			_bypassLadderFilterOnce = bypassLadderFilterOnce;
		}
	}

	private bool ShouldFilterLadderTarget(TowerDefenseCharacter checkTarget)
	{
		if (fliterLadder || _bypassLadderFilterOnce)
		{
			return false;
		}
		if (!(checkTarget is TowerDefensePlant) || !GodotObject.IsInstanceValid(checkTarget.cell) || !GodotObject.IsInstanceValid(checkTarget.cell.characterLadder))
		{
			return false;
		}
		if (parent.config is TowerDefenseZombieConfig { physique: <TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE })
		{
			return (double)parent.Scale.X > 0.0;
		}
		return false;
	}

	internal bool CanAttackFromZombieWalk()
	{
		if (!GodotObject.IsInstanceValid(target) && checkIntrevalNow > 0)
		{
			checkIntrevalNow--;
			TowerDefensePerfProfiler.Sample("attack.canAttack.intervalSkip");
			return false;
		}
		return CanAttack(targetSearchIntervalChecked: false);
	}

	public bool IsCurrentTargetReachable(TowerDefenseCharacter expectedTarget)
	{
		if (GodotObject.IsInstanceValid(expectedTarget) && target == expectedTarget && CanAttack(targetSearchIntervalChecked: true))
		{
			return target == expectedTarget;
		}
		return false;
	}

	private bool IsZombieAttackSuppressedBeforeGameRunning()
	{
		if (!(parent is TowerDefenseZombie))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (instance != null)
		{
			return !instance.IsGameRunning();
		}
		return true;
	}

	private bool IsZombieTargetingSuppressed()
	{
		if (!(parent is TowerDefenseZombie towerDefenseZombie))
		{
			return false;
		}
		if (!towerDefenseZombie.isGarlic && !towerDefenseZombie.isChangeLine)
		{
			return IsZombieBlowBackActive(towerDefenseZombie);
		}
		return true;
	}

	private static bool IsZombieBlowBackActive(TowerDefenseZombie zombie)
	{
		BlowBackComponent blowBackComponent = zombie.blowBackComponent;
		if (blowBackComponent != null && !blowBackComponent.IsReleased)
		{
			return zombie.blowBackComponent.blowBack;
		}
		return false;
	}

	private bool CanAttack(bool targetSearchIntervalChecked)
	{
		if (!base.CanExecuteGameplay)
		{
			target = null;
			return TryAcquireBrainOutsideLawn();
		}
		if (!HasValidTargetingContext())
		{
			target = null;
			return false;
		}
		if (IsZombieAttackSuppressedBeforeGameRunning())
		{
			target = null;
			return false;
		}
		if (IsZombieTargetingSuppressed())
		{
			target = null;
			return false;
		}
		if (parent is TowerDefenseZombie && GodotObject.IsInstanceValid(target) && ShouldFilterLadderTarget(target))
		{
			target = null;
			return false;
		}
		if (GodotObject.IsInstanceValid(target))
		{
			if (IsTerminalAttackTarget(target))
			{
				target = null;
				return false;
			}
			if (target.instance.invincible)
			{
				target = null;
				return false;
			}
			if (!TargetMatchesPreparedDetection(target))
			{
				target = null;
				return false;
			}
			if (!CanUseAsAttackTarget(target, attackType, parent))
			{
				target = null;
				return false;
			}
			if (!target.HasHitBox)
			{
				target = null;
				return false;
			}
			if (!parent.CanCollision(target.instance.maskFlags))
			{
				target = null;
				return false;
			}
			if (IsSmashImmuneTarget(attackType, target))
			{
				target = null;
				return false;
			}
			if (!parent.CanTarget(target))
			{
				target = null;
				return false;
			}
			if (checkGrid && !TargetMatchesAttackGridWindow(target))
			{
				target = null;
				return false;
			}
			target = ResolveCellTarget(target);
			bool num = GodotObject.IsInstanceValid(target);
			if (num)
			{
				TowerDefensePerfProfiler.Sample("attack.canAttack.cachedTarget");
			}
			return num;
		}
		if (!GodotObject.IsInstanceValid(target) && !targetSearchIntervalChecked)
		{
			if (!ConsumeTargetSearchInterval())
			{
				return false;
			}
			targetSearchIntervalChecked = true;
		}
		if (!TowerDefenseManager.Instance.characterRegistry.HasOpposingAttackCandidatesMatchingMask(parent.gridPos.Y, checkLine, parent.camp, parent.instance.collisionFlags))
		{
			TowerDefensePerfProfiler.Sample("attack.canAttack.noOpposingCamp");
			return false;
		}
		if (UseAttackGridLookup() && (!_UpdateCheckRect() || !HasAttackGridTargetCandidatesValidated()))
		{
			TowerDefensePerfProfiler.Sample("attack.canAttack.emptyGrid");
			return false;
		}
		TowerDefensePerfProfiler.Sample("attack.canAttack.search");
		TowerDefenseCharacter prefetchedTarget = null;
		bool usePrefetchedTarget = false;
		if (checkTall)
		{
			TowerDefensePerfProfiler.Sample("attack.canAttack.checkTall");
			if (TryGetTallFirstTarget(out target, out var isTallTarget))
			{
				if (isTallTarget)
				{
					return true;
				}
				usePrefetchedTarget = true;
				prefetchedTarget = target;
				target = null;
			}
		}
		if (!targetSearchIntervalChecked && !ConsumeTargetSearchInterval())
		{
			return false;
		}
		return CanAttackOnce(prefetchedTarget, usePrefetchedTarget);
	}

	private bool ConsumeTargetSearchInterval()
	{
		if (checkIntrevalNow > 0)
		{
			checkIntrevalNow--;
			TowerDefensePerfProfiler.Sample("attack.canAttack.intervalSkip");
			return false;
		}
		checkIntrevalNow = checkIntreval;
		timer = 0.0;
		RefreshPhysicsProcessEligibility();
		return true;
	}

	private bool TryAcquireBrainOutsideLawn()
	{
		if (!(parent is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie.attackComponent != this || !HasValidTargetingContext() || towerDefenseZombie.die || towerDefenseZombie.nearDie || towerDefenseZombie.instance.hypnoses || IsZombieAttackSuppressedBeforeGameRunning() || IsZombieTargetingSuppressed())
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!((double)towerDefenseZombie.GetLogicalGlobalPosition().X >= instance.GetMapGroundLeft()))
		{
			TowerDefenseBattleFeatureBrain brainFeature = instance.GetBrainFeature();
			if (brainFeature != null && brainFeature.TryGetAliveBrain(towerDefenseZombie.gridPos.Y, out var brain))
			{
				if (!brain.canCheck || !brain.instance.canBeCollection || brain.instance.invincible || !brain.IsHitBoxEnabled || !CanUseAsAttackTarget(brain, attackType, parent) || !towerDefenseZombie.CanTarget(brain) || !towerDefenseZombie.CanCollision(brain.instance.maskFlags) || !_UpdateCheckRect() || !AabbShapeUtil.Intersects(_checkRect, brain.WorldHitRect) || !TargetIntersectsEachCheckShape(brain))
				{
					return false;
				}
				target = brain;
				return true;
			}
		}
		return false;
	}

	private bool CanConsumeEmptyTargetSearchInterval()
	{
		if (!HasCheckGeometry())
		{
			return false;
		}
		if (IsZombieAttackSuppressedBeforeGameRunning())
		{
			return false;
		}
		if (IsZombieTargetingSuppressed())
		{
			return false;
		}
		return true;
	}

	private bool TargetMatchesPreparedDetection(TowerDefenseCharacter checkTarget)
	{
		if (!GodotObject.IsInstanceValid(checkTarget) || !_UpdateCheckRect())
		{
			return false;
		}
		if (UseAttackGridCellRangeLookup())
		{
			return TargetMatchesAttackGridCellRange(checkTarget);
		}
		if (AabbShapeUtil.Intersects(_checkRect, checkTarget.WorldHitRect))
		{
			return TargetIntersectsEachCheckShape(checkTarget);
		}
		return false;
	}

	public bool CanAttackOnce()
	{
		return CanAttackOnce(null, usePrefetchedTarget: false);
	}

	public bool TryCommitContactTarget(TowerDefenseCharacter contactTarget)
	{
		target = null;
		if (!base.CanExecuteGameplay || !HasValidTargetingContext() || IsZombieAttackSuppressedBeforeGameRunning() || IsZombieTargetingSuppressed() || IsTerminalAttackTarget(contactTarget) || !GodotObject.IsInstanceValid(contactTarget.instance) || contactTarget.instance.invincible || IsBiteImmune(contactTarget, attackType, parent) || !contactTarget.instance.canBeCollection || !contactTarget.HasHitBox || (double)contactTarget.GetLogicalGlobalPosition().X > groundRight || contactTarget is TowerDefenseCrater || !parent.CanCollision(contactTarget.instance.maskFlags) || !parent.CanTarget(contactTarget) || (checkLine && !contactTarget.IsTargetableFromLine(parent.gridPos.Y)) || (checkGrid && !TargetMatchesAttackGridWindow(contactTarget)) || (!checkBowling && contactTarget is TowerDefensePlantBowlingBase) || ((!checkGravestone || parent is TowerDefenseZombie) && contactTarget is TowerDefenseGravestone) || (contactTarget is TowerDefenseItem { canCheck: false } && (!checkVase || !(contactTarget is TowerDefenseVase))))
		{
			return false;
		}
		target = ResolveCellTarget(contactTarget);
		return !IsTerminalAttackTarget(target);
	}

	public bool TryRetargetImmediately()
	{
		target = null;
		if (!base.CanExecuteGameplay || !alive || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		if (!HasCheckGeometry())
		{
			return false;
		}
		if (IsZombieAttackSuppressedBeforeGameRunning() || IsZombieTargetingSuppressed())
		{
			return false;
		}
		return CanAttackOnce();
	}

	private bool CanAttackOnce(TowerDefenseCharacter prefetchedTarget, bool usePrefetchedTarget)
	{
		if (!base.CanExecuteGameplay || !HasValidTargetingContext())
		{
			target = null;
			return false;
		}
		if (!_UpdateCheckRect())
		{
			target = null;
			return false;
		}
		if (!checkAll)
		{
			if (usePrefetchedTarget)
			{
				target = prefetchedTarget;
			}
			else
			{
				GetTarget();
			}
			if (GodotObject.IsInstanceValid(target))
			{
				if (parent is TowerDefenseZombie && ShouldFilterLadderTarget(target))
				{
					target = null;
					return false;
				}
				target = ResolveCellTarget(target);
			}
			if (GodotObject.IsInstanceValid(target) && IsBiteImmune(target, attackType, parent))
			{
				target = null;
			}
			return GodotObject.IsInstanceValid(target);
		}
		TowerDefenseCharacter match;
		if (UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out var minLine, out var maxLine, out var minColumn, out var maxColumn))
		{
			ulong physicsFrames = Engine.GetPhysicsFrames();
			AttackAreaTargetPredicate predicate = new AttackAreaTargetPredicate
			{
				AttackType = attackType,
				Parent = parent,
				Line = parent.gridPos.Y,
				CheckLine = checkLine,
				FilterGravestone = checkGravestone
			};
			AttackAreaGridCellSelector selector = new AttackAreaGridCellSelector
			{
				Predicate = predicate
			};
			return TrySelectAttackGridCellRange(physicsFrames, minLine, maxLine, minColumn, maxColumn, ref selector, out match);
		}
		if (attackType == "Eat")
		{
			AttackAreaTargetPredicate predicate2 = new AttackAreaTargetPredicate
			{
				AttackType = attackType,
				Parent = parent,
				Line = parent.gridPos.Y,
				CheckLine = checkLine,
				FilterGravestone = checkGravestone
			};
			return TowerDefenseManager.Instance.characterRegistry.TryFindCharacterIntersectingRectExcludingCamp(_checkRect, checkLine ? parent.gridPos.Y : (-2147483648), checkLine, parent.camp, ref predicate2, out match);
		}
		if (checkGravestone)
		{
			return TowerDefenseManager.Instance.GetCharacterHasTargetFromRect(parent, _checkRect, checkLine);
		}
		return TowerDefenseManager.Instance.GetCharacterHasTargetFromRect(parent, _checkRect, checkLine, fliterGraveStone: false);
	}

	public TowerDefenseCharacter GetTargetTall()
	{
		if (!HasValidTargetingContext())
		{
			return null;
		}
		if (parent is TowerDefenseZombie)
		{
			checkGravestone = true;
		}
		if (!_UpdateCheckRect())
		{
			return null;
		}
		return TowerDefenseManager.Instance.GetTallCharacterTargetFromRect(parent, _checkRect, checkLine, parent.gridPos.Y, groundRight);
	}

	public TowerDefenseCharacter GetTarget()
	{
		if (!HasValidTargetingContext())
		{
			target = null;
			return null;
		}
		if (!checkTall)
		{
			if (TryGetFirstTarget(out var firstTarget))
			{
				target = ResolveCellTarget(firstTarget);
				return target;
			}
			target = null;
			return null;
		}
		List<TowerDefenseCharacter> targetList = GetTargetList();
		if (targetList.Count > 0)
		{
			TowerDefenseCharacter character = targetList[0];
			target = ResolveCellTarget(character);
			return target;
		}
		target = null;
		return null;
	}

	private bool TryGetFirstTarget(out TowerDefenseCharacter firstTarget)
	{
		firstTarget = null;
		if (!HasValidTargetingContext())
		{
			return false;
		}
		if (parent is TowerDefenseZombie)
		{
			checkGravestone = true;
		}
		if (!HasCheckGeometry())
		{
			return false;
		}
		if (!_UpdateCheckRect())
		{
			return false;
		}
		GetAttackGridColumnWindow(out var minColumn, out var maxColumn);
		int minLine = 0;
		int maxLine = 0;
		int minColumn2 = 0;
		int maxColumn2 = 0;
		bool flag = UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out minLine, out maxLine, out minColumn2, out maxColumn2);
		bool filterGravestone = !checkGravestone || parent is TowerDefenseZombie;
		ulong physicsFrames = Engine.GetPhysicsFrames();
		AttackTargetPredicate predicate = new AttackTargetPredicate
		{
			AttackType = attackType,
			Component = (flag ? null : this),
			Parent = parent,
			Line = parent.gridPos.Y,
			GroundRight = groundRight,
			CheckLine = checkLine,
			CheckGrid = checkGrid,
			GridMinColumn = minColumn,
			GridMaxColumn = maxColumn,
			CheckVase = checkVase,
			CheckBowling = checkBowling,
			FilterGravestone = filterGravestone,
			PhysicsFrame = physicsFrames
		};
		if (flag)
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.cellRange");
			if (checkLine && !(parent is TowerDefenseZombie))
			{
				return TowerDefenseManager.Instance.characterRegistry.TryFindLineCharacterInColumnRange(minLine, minColumn2, maxColumn2, includeAllLineCheck: true, parent.camp, ref predicate, out firstTarget);
			}
			AttackGridCellSelector selector = new AttackGridCellSelector
			{
				Predicate = predicate
			};
			return TrySelectAttackGridCellRange(physicsFrames, minLine, maxLine, minColumn2, maxColumn2, ref selector, out firstTarget);
		}
		if (UseAttackGridLookup())
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.grid");
			return TowerDefenseManager.Instance.characterRegistry.TryFindAttackGridCharacterIntersectingRect(_checkRect, parent.gridPos.Y, minColumn, maxColumn, checkLine, parent.camp, ref predicate, out firstTarget);
		}
		SampleFirstLineQueryReason();
		return TowerDefenseManager.Instance.characterRegistry.TryFindCharacterIntersectingRectExcludingCamp(_checkRect, checkLine ? parent.gridPos.Y : (-2147483648), checkLine, parent.camp, ref predicate, out firstTarget);
	}

	private void SampleFirstLineQueryReason()
	{
		TowerDefensePerfProfiler.Sample("attack.query.first.line");
		if (!checkLine)
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.line.allLines");
		}
		else if (!checkGrid)
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.line.noGrid");
		}
		else if (!(parent is TowerDefenseZombie))
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.line.nonZombie");
		}
		else
		{
			TowerDefensePerfProfiler.Sample("attack.query.first.line.other");
		}
	}

	private bool TryGetTallFirstTarget(out TowerDefenseCharacter selectedTarget, out bool isTallTarget)
	{
		selectedTarget = null;
		isTallTarget = false;
		if (!HasValidTargetingContext())
		{
			return false;
		}
		if (parent is TowerDefenseZombie)
		{
			checkGravestone = true;
		}
		if (!HasCheckGeometry())
		{
			return false;
		}
		if (!_UpdateCheckRect())
		{
			return false;
		}
		GetAttackGridColumnWindow(out var minColumn, out var maxColumn);
		int minLine = 0;
		int maxLine = 0;
		int minColumn2 = 0;
		int maxColumn2 = 0;
		bool flag = UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out minLine, out maxLine, out minColumn2, out maxColumn2);
		bool filterGravestone = !checkGravestone || parent is TowerDefenseZombie;
		ulong physicsFrames = Engine.GetPhysicsFrames();
		TallOnlyAttackTargetPredicate predicate = new TallOnlyAttackTargetPredicate
		{
			AttackType = attackType,
			Component = (flag ? null : this),
			Parent = parent,
			Line = parent.gridPos.Y,
			GroundRight = groundRight,
			CheckLine = checkLine,
			CheckGrid = checkGrid,
			GridMinColumn = minColumn,
			GridMaxColumn = maxColumn,
			PhysicsFrame = physicsFrames
		};
		bool flag2 = UseAttackGridLookup();
		CheckTallFallbackAttackTargetPredicate predicate2 = new CheckTallFallbackAttackTargetPredicate
		{
			Component = (flag ? null : this),
			Parent = parent,
			Line = parent.gridPos.Y,
			GroundRight = groundRight,
			CheckLine = checkLine,
			CheckGrid = checkGrid,
			GridMinColumn = minColumn,
			GridMaxColumn = maxColumn,
			CheckVase = checkVase,
			CheckBowling = checkBowling,
			FilterGravestone = filterGravestone,
			AttackType = attackType,
			PhysicsFrame = physicsFrames
		};
		if (flag)
		{
			TowerDefensePerfProfiler.Sample("attack.query.tallFallback.cellRange");
			CheckTallAttackGridSelector selector = new CheckTallAttackGridSelector
			{
				TallPredicate = predicate,
				FallbackPredicate = predicate2
			};
			TrySelectAttackGridCellRange(physicsFrames, minLine, maxLine, minColumn2, maxColumn2, ref selector, out selectedTarget);
			isTallTarget = selector.FoundTall;
			if (!isTallTarget && GodotObject.IsInstanceValid(selectedTarget))
			{
				selectedTarget = ResolveCellTarget(selectedTarget);
			}
			return true;
		}
		if (flag2)
		{
			TowerDefensePerfProfiler.Sample("attack.query.tallFallback.grid");
			CheckTallAttackGridSelector selector2 = new CheckTallAttackGridSelector
			{
				TallPredicate = predicate,
				FallbackPredicate = predicate2
			};
			TowerDefenseManager.Instance.characterRegistry.TrySelectAttackGridCharacterIntersectingRect(_checkRect, parent.gridPos.Y, minColumn, maxColumn, checkLine, parent.camp, ref selector2, out selectedTarget);
			isTallTarget = selector2.FoundTall;
			if (!isTallTarget && GodotObject.IsInstanceValid(selectedTarget))
			{
				selectedTarget = ResolveCellTarget(selectedTarget);
			}
			return true;
		}
		TowerDefensePerfProfiler.Sample("attack.query.tall.line");
		if (TowerDefenseManager.Instance.characterRegistry.TryFindTallCharacterIntersectingRectExcludingCamp(_checkRect, checkLine ? parent.gridPos.Y : (-2147483648), checkLine, parent.camp, ref predicate, out selectedTarget))
		{
			isTallTarget = true;
			return true;
		}
		TowerDefensePerfProfiler.Sample("attack.query.fallback.line");
		TowerDefenseManager.Instance.characterRegistry.TryFindCharacterIntersectingRectExcludingCamp(_checkRect, checkLine ? parent.gridPos.Y : (-2147483648), checkLine, parent.camp, ref predicate2, out selectedTarget);
		if (GodotObject.IsInstanceValid(selectedTarget))
		{
			selectedTarget = ResolveCellTarget(selectedTarget);
		}
		return true;
	}

	private bool UseAttackGridLookup()
	{
		if (checkGrid && checkLine && parent is TowerDefenseZombie)
		{
			return !checkBowling;
		}
		return false;
	}

	private bool UseAttackGridCellRangeLookup()
	{
		if (checkBowling || checkEachShape || CheckAreaShapeCount <= 0)
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = ResolveHitBoxSource();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return !towerDefenseCharacter.HasHitBox;
		}
		return true;
	}

	public bool HasAttackGridTargetCandidates()
	{
		if (!HasValidTargetingContext())
		{
			return false;
		}
		if (!UseAttackGridLookup())
		{
			return true;
		}
		if (!_UpdateCheckRect())
		{
			return false;
		}
		return HasAttackGridTargetCandidatesValidated();
	}

	private bool HasAttackGridTargetCandidatesValidated()
	{
		GetAttackGridColumnWindowFromPreparedRect(out var minColumn, out var maxColumn);
		return TowerDefenseManager.Instance.characterRegistry.HasAttackGridCharacters(parent.gridPos.Y, minColumn, maxColumn, checkLine, parent.camp);
	}

	private void GetAttackGridColumnWindow(out int minColumn, out int maxColumn)
	{
		if (useCheckAreaGridColumn && !_UpdateCheckRect())
		{
			maxColumn = (minColumn = (GodotObject.IsInstanceValid(parent) ? parent.gridPos.X : 0));
		}
		else
		{
			GetAttackGridColumnWindowFromPreparedRect(out minColumn, out maxColumn);
		}
	}

	private void GetAttackGridColumnWindowFromPreparedRect(out int minColumn, out int maxColumn)
	{
		int num = (maxColumn = (minColumn = (GodotObject.IsInstanceValid(parent) ? parent.gridPos.X : 0)));
		if (useCheckAreaGridColumn && !TryGetAttackGridColumnsForRect(_checkRect, out minColumn, out maxColumn))
		{
			minColumn = num;
			maxColumn = num;
		}
	}

	private bool TryGetAttackGridCellRangeFromPreparedRect(out int minLine, out int maxLine, out int minColumn, out int maxColumn)
	{
		minLine = 0;
		maxLine = 0;
		minColumn = 0;
		maxColumn = 0;
		if (!_checkRectValid || !TryGetAttackGridColumnsForRect(_checkRect, out minColumn, out maxColumn))
		{
			return false;
		}
		if (checkLine)
		{
			if (!GodotObject.IsInstanceValid(parent))
			{
				return false;
			}
			minLine = parent.gridPos.Y;
			maxLine = minLine;
			return true;
		}
		return TryGetAttackGridLinesForRect(_checkRect, out minLine, out maxLine);
	}

	private static bool TryGetAttackGridLinesForRect(Rect2 checkRect, out int minLine, out int maxLine)
	{
		minLine = 0;
		maxLine = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.Y <= 0 || instance.gridSize.Y <= 0f)
		{
			return false;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		float firstAnchor = ((GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.mapControl)) ? ((float)TowerDefenseManager.GetMapLineY(1)) : (instance.gridBeginPos.Y + instance.gridSize.Y * 0.5f));
		return TryGetAttackGridAnchorRange(checkRect.Position.Y, checkRect.End.Y, firstAnchor, instance.gridSize.Y, 1, instance.gridNum.Y, out minLine, out maxLine);
	}

	private void GetAttackGridColumnTraversal(int minColumn, int maxColumn, out int startColumn, out int endColumn, out int stepColumn)
	{
		startColumn = minColumn;
		endColumn = maxColumn;
		stepColumn = 1;
	}

	private bool TrySelectAttackGridCellRange<TSelector>(ulong physicsFrame, int minLine, int maxLine, int minColumn, int maxColumn, ref TSelector selector, out TowerDefenseCharacter selectedTarget) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		TowerDefenseBattleCharacterRegistry characterRegistry = TowerDefenseManager.Instance.characterRegistry;
		if (checkLine && !(parent is TowerDefenseZombie))
		{
			return characterRegistry.TrySelectLineCharacterInColumnRangeForPhysicsFrame(physicsFrame, minLine, minColumn, maxColumn, includeAllLineCheck: true, parent.camp, ref selector, out selectedTarget);
		}
		GetAttackGridColumnTraversal(minColumn, maxColumn, out var startColumn, out var endColumn, out var stepColumn);
		int visitedCells;
		return characterRegistry.TrySelectAttackGridCharacterInCellRangeForPhysicsFrame(physicsFrame, minLine, maxLine, 1, startColumn, endColumn, stepColumn, visitRowsFirst: true, checkLine, parent.camp, ref selector, out selectedTarget, out visitedCells);
	}

	private static bool TryGetAttackGridColumnsForRect(Rect2 checkRect, out int minColumn, out int maxColumn)
	{
		minColumn = 0;
		maxColumn = 0;
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
		float firstAnchor = gridBeginPos.X + gridSize.X * 0.5f;
		return TryGetAttackGridAnchorRange(checkRect.Position.X, checkRect.End.X, firstAnchor, gridSize.X, -1, gridNum.X + 1, out minColumn, out maxColumn);
	}

	private static bool TryGetAttackGridAnchorRange(float rangeStart, float rangeEnd, float firstAnchor, float stride, int clampMinimum, int clampMaximum, out int minimum, out int maximum)
	{
		minimum = 0;
		maximum = 0;
		if (!float.IsFinite(rangeStart) || !float.IsFinite(rangeEnd) || !float.IsFinite(firstAnchor) || !float.IsFinite(stride) || stride <= 0f || clampMinimum > clampMaximum)
		{
			return false;
		}
		float num = Mathf.Min(rangeStart, rangeEnd);
		float num2 = Mathf.Max(rangeStart, rangeEnd);
		minimum = Mathf.Clamp(Mathf.CeilToInt((num - firstAnchor) / stride) + 1, clampMinimum, clampMaximum);
		maximum = Mathf.Clamp(Mathf.FloorToInt((num2 - firstAnchor) / stride) + 1, clampMinimum, clampMaximum);
		return minimum <= maximum;
	}

	private bool TargetMatchesAttackGridWindow(TowerDefenseCharacter checkCharacter)
	{
		if (!GodotObject.IsInstanceValid(checkCharacter))
		{
			return false;
		}
		if (!checkGrid || !(parent is TowerDefenseZombie) || !(checkCharacter is TowerDefensePlant))
		{
			return true;
		}
		if (CanBypassGridWindowForBowling(checkCharacter, checkBowling))
		{
			return true;
		}
		GetAttackGridColumnWindow(out var minColumn, out var maxColumn);
		return PlantMatchesAttackGridWindow(checkCharacter, minColumn, maxColumn);
	}

	private bool TargetMatchesAttackGridCellRange(TowerDefenseCharacter checkCharacter)
	{
		if (!GodotObject.IsInstanceValid(checkCharacter) || !TryGetAttackGridCellRangeFromPreparedRect(out var minLine, out var maxLine, out var minColumn, out var maxColumn))
		{
			return false;
		}
		if (!checkCharacter.IsTargetableFromLineRange(minLine, maxLine))
		{
			return false;
		}
		return CharacterMatchesAttackGridWindow(checkCharacter, minColumn, maxColumn);
	}

	private static bool CanBypassGridWindowForBowling(TowerDefenseCharacter checkCharacter, bool allowBowling)
	{
		if (allowBowling)
		{
			return checkCharacter is TowerDefensePlantBowlingBase;
		}
		return false;
	}

	private static bool PlantMatchesAttackGridWindow(TowerDefenseCharacter checkCharacter, int minColumn, int maxColumn)
	{
		if (checkCharacter.gridPos.X >= minColumn && checkCharacter.gridPos.X <= maxColumn)
		{
			return true;
		}
		if (checkCharacter.config is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				int num = checkCharacter.gridPos.X + item.X;
				if (num >= minColumn && num <= maxColumn)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool CharacterMatchesAttackGridWindow(TowerDefenseCharacter checkCharacter, int minColumn, int maxColumn)
	{
		if (checkCharacter.gridPos.X >= minColumn && checkCharacter.gridPos.X <= maxColumn)
		{
			return true;
		}
		if (checkCharacter.config is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				int num = checkCharacter.gridPos.X + item.X;
				if (num >= minColumn && num <= maxColumn)
				{
					return true;
				}
			}
		}
		int num2 = checkCharacter.targetRegistrationComponent?.attackGridColumnAliasOffset ?? 0;
		int num3 = checkCharacter.gridPos.X + num2;
		if (num2 != 0 && num3 >= minColumn)
		{
			return num3 <= maxColumn;
		}
		return false;
	}

	private static bool IsTerminalAttackTarget(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance) && !character.die && !character.nearDie)
		{
			return character.isDestroy;
		}
		return true;
	}

	private static bool CanUseAsAttackTarget(TowerDefenseCharacter character, string type, TowerDefenseCharacter attacker)
	{
		if (character.instance.canBeCollection && !character.instance.hologram)
		{
			return !IsBiteImmune(character, type, attacker);
		}
		return false;
	}

	private static bool IsBiteImmune(TowerDefenseCharacter character, string type, TowerDefenseCharacter attacker)
	{
		if (type != "Eat")
		{
			return false;
		}
		if (character.instance.biteHurt == 0.0)
		{
			return true;
		}
		if (character is TowerDefensePlant && character.componentManager != null)
		{
			ComponentManager componentManager = character.componentManager;
			PotatoComponent runtime = componentManager.GetRuntime<PotatoComponent>();
			if (runtime == null || !runtime.ProtectsFromBites)
			{
				SquashComponent runtime2 = componentManager.GetRuntime<SquashComponent>();
				if (runtime2 == null || !runtime2.ProtectsFromBites)
				{
					ExplodeComponent runtime3 = componentManager.GetRuntime<ExplodeComponent>();
					if (runtime3 == null || !runtime3.ProtectsFromBites)
					{
						goto IL_007c;
					}
				}
			}
			return true;
		}
		goto IL_007c;
		IL_007c:
		if (!(character is TowerDefenseItemBrain) || !(attacker is TowerDefenseZombie))
		{
			return false;
		}
		return attacker.GetLogicalGlobalTransform(attacker.sprite).X.X < 0f;
	}

	private static bool IsSmashImmuneTarget(string attackType, TowerDefenseCharacter candidate)
	{
		if (attackType == "Smash" && GodotObject.IsInstanceValid(candidate?.instance))
		{
			return candidate.instance.smashHurt == 0.0;
		}
		return false;
	}

	private bool HasValidTargetingContext()
	{
		TowerDefenseCharacter towerDefenseCharacter = parent;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && HasCheckGeometry() && GodotObject.IsInstanceValid(instance))
		{
			return GodotObject.IsInstanceValid(instance.characterRegistry);
		}
		return false;
	}

	private TowerDefenseCharacter ResolveCellTarget(TowerDefenseCharacter character)
	{
		if (character is TowerDefensePlant && !(character is TowerDefensePlantBowlingBase))
		{
			TowerDefenseCellInstance cell = character.cell;
			if (GodotObject.IsInstanceValid(cell))
			{
				TowerDefenseCharacter towerDefenseCharacter = cell.GetTarget(parent.instance.collisionFlags, parent.camp);
				if (!IsTerminalAttackTarget(towerDefenseCharacter) && !IsBiteImmune(towerDefenseCharacter, attackType, parent) && parent.CanCollision(towerDefenseCharacter.instance.maskFlags))
				{
					return towerDefenseCharacter;
				}
			}
		}
		return character;
	}

	public List<TowerDefenseCharacter> GetTargetList()
	{
		List<TowerDefenseCharacter> list = _reusableTargetList ?? (_reusableTargetList = new List<TowerDefenseCharacter>());
		list.Clear();
		if (!HasValidTargetingContext())
		{
			return list;
		}
		if (parent is TowerDefenseZombie)
		{
			checkGravestone = true;
		}
		bool flag = !checkGravestone || parent is TowerDefenseZombie;
		if (!_UpdateCheckRect())
		{
			return list;
		}
		GetAttackGridColumnWindow(out var minColumn, out var maxColumn);
		if (!checkTall && UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out var minLine, out var maxLine, out var minColumn2, out var maxColumn2))
		{
			ulong physicsFrames = Engine.GetPhysicsFrames();
			AttackTargetPredicate predicate = new AttackTargetPredicate
			{
				AttackType = attackType,
				Parent = parent,
				Line = parent.gridPos.Y,
				GroundRight = groundRight,
				CheckLine = checkLine,
				CheckGrid = checkGrid,
				GridMinColumn = minColumn,
				GridMaxColumn = maxColumn,
				CheckVase = checkVase,
				CheckBowling = checkBowling,
				FilterGravestone = flag,
				PhysicsFrame = physicsFrames
			};
			TowerDefenseManager.Instance.characterRegistry.FillAttackGridCharactersInCellRangeForPhysicsFrame(physicsFrames, minLine, maxLine, minColumn2, maxColumn2, checkLine, parent.camp, ref predicate, list);
			return list;
		}
		if (!checkTall)
		{
			if (HasCheckGeometry())
			{
				list = TowerDefenseManager.Instance.GetCharacterTargetFromRect(parent, _checkRect, checkLine, fliterGraveStone: false, !checkVase);
			}
			List<TowerDefenseCharacter> list2 = _reusableFilteredList ?? (_reusableFilteredList = new List<TowerDefenseCharacter>());
			list2.Clear();
			foreach (TowerDefenseCharacter item in list)
			{
				if (!IsBiteImmune(item, attackType, parent) && item.instance.canBeCollection && !item.instance.invincible && !((double)item.GetLogicalGlobalPosition().X > groundRight) && (!checkGrid || !(parent is TowerDefenseZombie) || !(item is TowerDefensePlant) || CanBypassGridWindowForBowling(item, checkBowling) || PlantMatchesAttackGridWindow(item, minColumn, maxColumn)) && (checkBowling || !(item is TowerDefensePlantBowlingBase)) && (!flag || !(item is TowerDefenseGravestone)) && (!checkLine || item.IsTargetableFromLine(parent.gridPos.Y)) && TargetIntersectsEachCheckShape(item))
				{
					list2.Add(item);
				}
			}
			list = list2;
		}
		else
		{
			foreach (TowerDefenseCharacter item2 in TowerDefenseManager.Instance.GetCharactersIntersectingRect(_checkRect, checkLine, parent.gridPos.Y))
			{
				if (IsBiteImmune(item2, attackType, parent) || !item2.instance.canBeCollection || item2.instance.invincible || (double)item2.GetLogicalGlobalPosition().X > groundRight || !parent.CanTarget(item2) || (checkGrid && parent is TowerDefenseZombie && item2 is TowerDefensePlant && !CanBypassGridWindowForBowling(item2, checkBowling) && !PlantMatchesAttackGridWindow(item2, minColumn, maxColumn)))
				{
					continue;
				}
				if (item2.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
				{
					if (checkLine && !item2.IsTargetableFromLine(parent.gridPos.Y))
					{
						continue;
					}
					if (item2 is TowerDefensePlant && GodotObject.IsInstanceValid(item2.cell))
					{
						if (GodotObject.IsInstanceValid(item2.cell.characterLadder))
						{
							continue;
						}
						foreach (TowerDefenseCharacter character in item2.cell.GetCharacterList())
						{
							if (character is TowerDefensePlant towerDefensePlant && !IsBiteImmune(towerDefensePlant, attackType, parent) && towerDefensePlant.instance.canBeCollection && !towerDefensePlant.instance.invincible && item2.instance.canBeCollection && !item2.instance.invincible && parent.CanTarget(towerDefensePlant) && parent.CanCollision(towerDefensePlant.instance.maskFlags))
							{
								list.Add(towerDefensePlant);
							}
						}
					}
					list.Add(item2);
				}
				else if (parent.CanCollision(item2.instance.maskFlags) && !(item2 is TowerDefenseCrater) && (!(item2 is TowerDefenseItem { canCheck: false }) || (!(attackType != "Smash") && item2 is TowerDefenseVase && checkVase)) && (!flag || !(item2 is TowerDefenseGravestone)) && (checkBowling || !(item2 is TowerDefensePlantBowlingBase)) && (!checkLine || item2.IsTargetableFromLine(parent.gridPos.Y)))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	public List<TowerDefenseCharacter> GetCharcterList()
	{
		List<TowerDefenseCharacter> list = _reusableCharList ?? (_reusableCharList = new List<TowerDefenseCharacter>());
		list.Clear();
		if (!HasValidTargetingContext())
		{
			return list;
		}
		if (parent is TowerDefenseZombie)
		{
			checkGravestone = true;
		}
		bool flag = !checkGravestone || parent is TowerDefenseZombie;
		if (!_UpdateCheckRect())
		{
			return list;
		}
		GetAttackGridColumnWindow(out var minColumn, out var maxColumn);
		if (UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out var minLine, out var maxLine, out var minColumn2, out var maxColumn2))
		{
			ulong physicsFrames = Engine.GetPhysicsFrames();
			AttackCharacterListPredicate predicate = new AttackCharacterListPredicate
			{
				Parent = parent,
				CheckLine = checkLine,
				Line = parent.gridPos.Y,
				CheckGrid = checkGrid,
				GridMinColumn = minColumn,
				GridMaxColumn = maxColumn,
				CheckBowling = checkBowling,
				FilterGravestone = flag
			};
			TowerDefenseManager.Instance.characterRegistry.FillAttackGridCharactersInCellRangeForPhysicsFrame(physicsFrames, minLine, maxLine, minColumn2, maxColumn2, checkLine, TowerDefenseEnum.CHARACTER_CAMP.NOONE, ref predicate, list);
			return list;
		}
		foreach (TowerDefenseCharacter item in TowerDefenseManager.Instance.GetCharactersIntersectingRect(_checkRect, checkLine, parent.gridPos.Y))
		{
			if (item.instance.canBeCollection && !item.instance.invincible && !(item is TowerDefenseCrater) && !(item is TowerDefenseItem) && (!flag || !(item is TowerDefenseGravestone)) && (checkBowling || !(item is TowerDefensePlantBowlingBase)) && (!checkGrid || !(parent is TowerDefenseZombie) || !(item is TowerDefensePlant) || CanBypassGridWindowForBowling(item, checkBowling) || PlantMatchesAttackGridWindow(item, minColumn, maxColumn)) && (!checkLine || item.IsTargetableFromLine(parent.gridPos.Y)))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private bool CanExecuteGameplayEffect()
	{
		if (alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && (parent.IsOwnerBatchDispatchActive || parent.IsInsideTree()) && !parent.die && !parent.nearDie)
		{
			return GodotObject.IsInstanceValid(TowerDefenseManager.Instance);
		}
		return false;
	}

	public double AttackDpsExecute(double delta, double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect())
		{
			return num;
		}
		TowerDefenseCharacter towerDefenseCharacter = target;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return num;
		}
		if (IsBiteImmune(towerDefenseCharacter, attackType, parent))
		{
			return num;
		}
		double num2 = num * delta;
		if (parent.iceSpeedDown || parent.emSpeedDown)
		{
			num2 /= 2.0;
		}
		towerDefenseCharacter.AttackDeal(parent, attackType, num2);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.cell))
		{
			towerDefenseCharacter.cell.AttackDeal(parent, attackType, num2);
		}
		EventExecuteDps(towerDefenseCharacter, delta);
		TowerDefenseManager.ApplyMapAttackDpsLifesteal(parent, num2);
		towerDefenseCharacter.instance.skipDealHurtReduce = true;
		double result = towerDefenseCharacter.instance.Hurt(num2, playSplatAudio: false, Vector2.Zero, hitShield: false);
		towerDefenseCharacter.instance.skipDealHurtReduce = false;
		ShowHealthComponent showHealthComponent = towerDefenseCharacter.showHealthComponent;
		if (showHealthComponent != null && !showHealthComponent.IsReleased)
		{
			towerDefenseCharacter.showHealthComponent.MarkDirty();
		}
		if (playPlantDefeatAudio && towerDefenseCharacter is TowerDefensePlant && towerDefenseCharacter.instance.die)
		{
			PlayConfiguredAudio(plantDefeatAudio);
		}
		if (attackDpsTimer > 0.0)
		{
			attackDpsTimer -= delta;
			return result;
		}
		attackDpsTimer = Math.Max(0.0, eatAudioInterval);
		towerDefenseCharacter.White(0.5, 0.0, 0.2);
		PlayConfiguredAudio(eatAudio);
		return result;
	}

	private static void PlayConfiguredAudio(string audioName)
	{
		if (!string.IsNullOrEmpty(audioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(audioName);
		}
	}

	public double HealthDps(double delta, double num)
	{
		if (target == null)
		{
			return num;
		}
		double num2 = num * delta;
		if (parent.iceSpeedDown || parent.emSpeedDown)
		{
			num2 /= 2.0;
		}
		return num2;
	}

	public double AttackExecute(double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect())
		{
			return num;
		}
		TowerDefenseCharacter towerDefenseCharacter = target;
		if (towerDefenseCharacter == null)
		{
			return num;
		}
		if (IsBiteImmune(towerDefenseCharacter, attackType, parent))
		{
			return num;
		}
		towerDefenseCharacter.AttackDeal(parent, attackType, num);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.cell))
		{
			towerDefenseCharacter.cell.AttackDeal(parent, attackType, num);
		}
		EventExecute(towerDefenseCharacter);
		towerDefenseCharacter.instance.skipDealHurtReduce = true;
		double result = towerDefenseCharacter.Hurt(num, playSplatAudio: true, Vector2.Zero);
		towerDefenseCharacter.instance.skipDealHurtReduce = false;
		return result;
	}

	private List<TowerDefenseCharacter> GetAreaEffectTargetList()
	{
		List<TowerDefenseCharacter> list = _reusableEffectTargetList ?? (_reusableEffectTargetList = new List<TowerDefenseCharacter>());
		list.Clear();
		if (UseAttackGridCellRangeLookup() && TryGetAttackGridCellRangeFromPreparedRect(out var minLine, out var maxLine, out var minColumn, out var maxColumn))
		{
			ulong physicsFrames = Engine.GetPhysicsFrames();
			AttackAreaTargetPredicate predicate = new AttackAreaTargetPredicate
			{
				AttackType = attackType,
				Parent = parent,
				Line = parent.gridPos.Y,
				CheckLine = false,
				FilterGravestone = true
			};
			TowerDefenseManager.Instance.characterRegistry.FillAttackGridCharactersInCellRangeForPhysicsFrame(physicsFrames, minLine, maxLine, minColumn, maxColumn, includeAllLineCheck: false, parent.camp, ref predicate, list);
			return list;
		}
		list.AddRange(TowerDefenseManager.Instance.GetCharacterTargetFromRect(parent, _checkRect));
		return list;
	}

	public void AttackAll(double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect() || !_UpdateCheckRect())
		{
			return;
		}
		foreach (TowerDefenseCharacter areaEffectTarget in GetAreaEffectTargetList())
		{
			if (GodotObject.IsInstanceValid(areaEffectTarget) && !IsBiteImmune(areaEffectTarget, attackType, parent))
			{
				areaEffectTarget.AttackDeal(parent, attackType, num);
				if (GodotObject.IsInstanceValid(areaEffectTarget.cell))
				{
					areaEffectTarget.cell.AttackDeal(parent, attackType, num);
				}
				areaEffectTarget.instance.skipDealHurtReduce = true;
				areaEffectTarget.Hurt(num, playSplatAudio: true, Vector2.Zero);
				areaEffectTarget.instance.skipDealHurtReduce = false;
				EventExecute(areaEffectTarget);
			}
		}
	}

	public void SmashAttackAll(double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect() || !_UpdateCheckRect())
		{
			return;
		}
		foreach (TowerDefenseCharacter areaEffectTarget in GetAreaEffectTargetList())
		{
			if (GodotObject.IsInstanceValid(areaEffectTarget))
			{
				double num2 = ResolveVehicleSmashDamage(areaEffectTarget, num);
				areaEffectTarget.AttackDeal(parent, attackType, num2);
				if (GodotObject.IsInstanceValid(areaEffectTarget.cell))
				{
					areaEffectTarget.cell.AttackDeal(parent, attackType, num2);
				}
				if (!TryShieldBlockSmash(areaEffectTarget, num2) && !TryBurstWheelSmash(areaEffectTarget, num2))
				{
					areaEffectTarget.SmashHurt(num2, playSplatAudio: true, Vector2.Zero);
					EventExecute(areaEffectTarget);
				}
			}
		}
	}

	public void AttackAllFlag(double num, int flag)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect() || !_UpdateCheckRect())
		{
			return;
		}
		foreach (TowerDefenseCharacter areaEffectTarget in GetAreaEffectTargetList())
		{
			if (GodotObject.IsInstanceValid(areaEffectTarget) && !IsBiteImmune(areaEffectTarget, attackType, parent))
			{
				areaEffectTarget.AttackDeal(parent, attackType, num);
				if (GodotObject.IsInstanceValid(areaEffectTarget.cell))
				{
					areaEffectTarget.cell.AttackDeal(parent, attackType, num);
				}
				areaEffectTarget.instance.skipDealHurtReduce = true;
				areaEffectTarget.FlagHurt(num, flag, playSplatAudio: true, Vector2.Zero);
				areaEffectTarget.instance.skipDealHurtReduce = false;
				EventExecute(areaEffectTarget);
			}
		}
	}

	public double SmashAttack(double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect())
		{
			return num;
		}
		TowerDefenseCharacter towerDefenseCharacter = GetTarget();
		if (towerDefenseCharacter == null)
		{
			return num;
		}
		double num2 = ResolveVehicleSmashDamage(towerDefenseCharacter, num);
		if (TryShieldBlockSmash(towerDefenseCharacter, num2))
		{
			return 0.0;
		}
		if (TryBurstWheelSmash(towerDefenseCharacter, num2))
		{
			return 0.0;
		}
		return towerDefenseCharacter.SmashHurt(num2, playSplatAudio: true, Vector2.Zero);
	}

	public void SmashAttackCell(double num)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect() || !GodotObject.IsInstanceValid(target))
		{
			return;
		}
		if (target is TowerDefenseZombie || target is TowerDefenseGravestone || target is TowerDefenseVase)
		{
			target.SmashHurt(ResolveVehicleSmashDamage(target, num), playSplatAudio: true, Vector2.Zero);
		}
		else if (target is TowerDefensePlant)
		{
			target = GetTarget();
			if (GodotObject.IsInstanceValid(target))
			{
				SmashAttackCell(num, target.gridPos);
			}
		}
		else if (target is TowerDefenseItem { canCheck: not false })
		{
			target.SmashHurt(ResolveVehicleSmashDamage(target, num), playSplatAudio: true, Vector2.Zero);
		}
	}

	public void SmashAttackCell(double num, Vector2I grid)
	{
		if (IsRemoteSyncedAttacker() || !CanExecuteGameplayEffect())
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return;
		}
		TowerDefenseCharacter[] deferredTargets = _smashAttackCellBufferPool.Rent(4);
		int deferredTargetCount = 0;
		try
		{
			List<TowerDefenseCharacter> characterList = mapCell.characterList;
			for (int num2 = characterList.Count - 1; num2 >= 0; num2--)
			{
				TowerDefenseCharacter towerDefenseCharacter = characterList[num2];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !(towerDefenseCharacter is TowerDefenseGravestone) && !(towerDefenseCharacter is TowerDefenseCrater) && (!GodotObject.IsInstanceValid(mapCell.itemShield) || mapCell.itemShield != towerDefenseCharacter) && ApplyCellSmashHurt(towerDefenseCharacter, num, ref deferredTargets, ref deferredTargetCount))
				{
					return;
				}
			}
			if (FillOpposingZombiesOnCell(characterList, grid))
			{
				for (int i = 0; i < _smashOpposingZombieBuffer.Count; i++)
				{
					if (ApplyCellSmashHurt(_smashOpposingZombieBuffer[i], num, ref deferredTargets, ref deferredTargetCount))
					{
						return;
					}
				}
			}
			for (int j = 0; j < deferredTargetCount; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = deferredTargets[j];
				double num3 = ResolveVehicleSmashDamage(towerDefenseCharacter2, num);
				towerDefenseCharacter2.SmashHurt(num3, playSplatAudio: true, Vector2.Zero);
				towerDefenseCharacter2.AttackDeal(parent, attackType, num3);
			}
		}
		finally
		{
			_smashAttackCellBufferPool.Return(deferredTargets, clearArray: true);
		}
	}

	private bool ApplyCellSmashHurt(TowerDefenseCharacter target, double num, ref TowerDefenseCharacter[] deferredTargets, ref int deferredTargetCount)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return false;
		}
		if (!target.CanTarget(parent) || !target.CanCollision(parent.instance.collisionFlags))
		{
			return false;
		}
		double num2 = ResolveVehicleSmashDamage(target, num);
		if (TryShieldBlockSmash(target, num2))
		{
			return true;
		}
		if (TryBurstWheelSmash(target, num2))
		{
			return true;
		}
		if (target.SmashHurt(num2, playSplatAudio: true, Vector2.Zero) == num2)
		{
			if (deferredTargetCount == deferredTargets.Length)
			{
				TowerDefenseCharacter[] array = _smashAttackCellBufferPool.Rent(deferredTargets.Length * 2);
				System.Array.Copy(deferredTargets, array, deferredTargetCount);
				_smashAttackCellBufferPool.Return(deferredTargets, clearArray: true);
				deferredTargets = array;
			}
			deferredTargets[deferredTargetCount++] = target;
		}
		target.AttackDeal(parent, attackType, num2);
		return false;
	}

	private bool FillOpposingZombiesOnCell(List<TowerDefenseCharacter> cellList, Vector2I grid)
	{
		_smashOpposingZombieBuffer.Clear();
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return false;
		}
		Rect2 checkRect = AabbShapeUtil.RectFromCenter(instance.GetMapCellPosCenter(grid), instance.GetMapGridSize());
		instance.characterRegistry.FillCharactersIntersectingRectListExcludingCamp(checkRect, parent.camp, _smashOpposingZombieQueryBuffer, grid.Y);
		for (int i = 0; i < _smashOpposingZombieQueryBuffer.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _smashOpposingZombieQueryBuffer[i];
			if (towerDefenseCharacter is TowerDefenseZombie && (cellList == null || !cellList.Contains(towerDefenseCharacter)))
			{
				_smashOpposingZombieBuffer.Add(towerDefenseCharacter);
			}
		}
		return _smashOpposingZombieBuffer.Count > 0;
	}

	private double ResolveVehicleSmashDamage(TowerDefenseCharacter character, double num)
	{
		if (!(parent is TowerDefenseZombie { die: false, isDestroy: false } towerDefenseZombie) || !GodotObject.IsInstanceValid(towerDefenseZombie.instance) || towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
		{
			return num;
		}
		if (!(character is TowerDefensePlant))
		{
			return num;
		}
		return 10000.0;
	}

	private bool TryBurstWheelSmash(TowerDefenseCharacter character, double num)
	{
		if (!(parent is TowerDefenseZombie { die: false, isDestroy: false } towerDefenseZombie) || !GodotObject.IsInstanceValid(towerDefenseZombie.instance) || towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
		{
			return false;
		}
		if (!(character is TowerDefensePlant towerDefensePlant) || !GodotObject.IsInstanceValid(towerDefensePlant.instance) || !towerDefensePlant.instance.HasBurstWheelArmor())
		{
			return false;
		}
		character.SmashHurt(num, playSplatAudio: true, Vector2.Zero);
		towerDefenseZombie.Die();
		return true;
	}

	internal bool TryShieldBlockSmash(TowerDefenseCharacter character, double num, bool forceLethal = false)
	{
		if (!(character is TowerDefensePlant) || !GodotObject.IsInstanceValid(character.instance))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(character.cell) || !GodotObject.IsInstanceValid(character.cell.itemShield))
		{
			return false;
		}
		TowerDefenseItemSheild itemShield = character.cell.itemShield;
		if (!GodotObject.IsInstanceValid(itemShield.instance))
		{
			return false;
		}
		if (itemShield == character)
		{
			return false;
		}
		if (itemShield.instance.hypnoses != character.instance.hypnoses)
		{
			return false;
		}
		if (!forceLethal && character.instance.hitpoints - num > 0.0)
		{
			return false;
		}
		if (!itemShield.ShieldBlockLethal())
		{
			return false;
		}
		itemShield.ShieldDeflateVehicle(parent);
		character.EmitBodyHurt(0);
		return true;
	}

	public void AttackEventExecute()
	{
		if (!base.CanExecuteGameplay || IsRemoteSyncedAttacker())
		{
			return;
		}
		RefreshEventExecutionCache();
		foreach (TowerDefenseCharacter target in GetTargetList())
		{
			if (GodotObject.IsInstanceValid(target))
			{
				ExecuteCachedEvents(target, dps: false, 0.0);
			}
		}
	}

	public void EventExecute(TowerDefenseCharacter character)
	{
		if (base.CanExecuteGameplay)
		{
			RefreshEventExecutionCache();
			ExecuteCachedEvents(character, dps: false, 0.0);
		}
	}

	public void EventExecuteDps(TowerDefenseCharacter character, double delta)
	{
		if (base.CanExecuteGameplay)
		{
			ExecuteCachedEvents(character, dps: true, delta);
		}
	}

	public void RefreshEventExecutionCache()
	{
		int num = eventList?.Count ?? 0;
		if (num == 0)
		{
			_eventExecutionCache = System.Array.Empty<TowerDefenseCharacterEventBase>();
			return;
		}
		if (_eventExecutionCache.Length != num)
		{
			_eventExecutionCache = new TowerDefenseCharacterEventBase[num];
		}
		for (int i = 0; i < num; i++)
		{
			_eventExecutionCache[i] = eventList[i];
		}
	}

	private void ExecuteCachedEvents(TowerDefenseCharacter character, bool dps, double delta)
	{
		TowerDefenseCharacterEventBase[] eventExecutionCache = _eventExecutionCache;
		if (eventExecutionCache.Length == 0)
		{
			return;
		}
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacterEventBase towerDefenseCharacterEventBase in eventExecutionCache)
		{
			if (dps)
			{
				towerDefenseCharacterEventBase.ExecuteDps(logicalGlobalPosition, character, delta);
			}
			else
			{
				towerDefenseCharacterEventBase.Execute(logicalGlobalPosition, character);
			}
		}
	}

	public void IdleEntered()
	{
		target = null;
		if (alive)
		{
			AdobeAnimateSprite animationSprite = GetAnimationSprite();
			if (GodotObject.IsInstanceValid(animationSprite) && CanPlayAnimation(spliceIdleAnimeClips) && spliceIdleAnimeClips != "")
			{
				animationSprite.SetAnimation(spliceIdleAnimeClips, loop: true, 0.20000000298023224);
			}
			if (parent.componentRunning && parent is TowerDefensePlant towerDefensePlant)
			{
				towerDefensePlant.Idle();
			}
		}
	}

	public void IdleProcessing(double delta)
	{
		if ((parent is TowerDefensePlant && timer > 0.0) || !alive || parent is TowerDefenseZombie || !TowerDefenseManager.Instance.IsGameRunning() || !parent.inGame || !parent.componentAlive || parent.componentRunning || parent.die || parent.nearDie || timer > 0.0)
		{
			return;
		}
		EnsureAnimationSpriteEventsConnected();
		if (!CanRunAnimatedAttack())
		{
			return;
		}
		if (!(parent is TowerDefensePlant) && !GodotObject.IsInstanceValid(target) && CanConsumeEmptyTargetSearchInterval())
		{
			if (ConsumeTargetSearchInterval() && CanAttack(targetSearchIntervalChecked: true) && parent is TowerDefensePlant)
			{
				parent.Component();
				SendStateEvent(attackStateEvent);
			}
		}
		else if (CanAttack() && parent is TowerDefensePlant)
		{
			parent.Component();
			SendStateEvent(attackStateEvent);
		}
	}

	public void IdleExited()
	{
	}

	public void AttackEntered()
	{
		Refresh();
		RefreshEventExecutionCache();
		this.OnAttackReady?.Invoke();
		EnsureAnimationSpriteEventsConnected();
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (!GodotObject.IsInstanceValid(animationSprite) || !CanRunAnimatedAttack())
		{
			SendStateEvent(idleStateEvent);
		}
		else if (CanPlayAnimation(attackAnimeClips))
		{
			animationSprite.SetAnimation(attackAnimeClips, loop: true, 0.20000000298023224);
		}
		else
		{
			SendStateEvent(idleStateEvent);
		}
	}

	public void AttackProcessing(double delta)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		EnsureAnimationSpriteEventsConnected();
		if (!CanRunAnimatedAttack())
		{
			SendStateEvent(idleStateEvent);
			return;
		}
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (!GodotObject.IsInstanceValid(animationSprite))
		{
			SendStateEvent(idleStateEvent);
		}
		else if (!CanPlayAnimation(attackAnimeClips))
		{
			SendStateEvent(idleStateEvent);
		}
		else if (!alive)
		{
			parent.Idle();
			SendStateEvent(idleStateEvent);
		}
		else if (!parent.componentAlive)
		{
			SendStateEvent(idleStateEvent);
		}
		else if (parent.die || parent.nearDie)
		{
			SendStateEvent(idleStateEvent);
		}
		else if (!checkAll && (!GodotObject.IsInstanceValid(target) || target.die || target.nearDie || target.isDestroy))
		{
			target = null;
			SendStateEvent(idleStateEvent);
		}
		else
		{
			double num = (TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame ? 1.0 : parent.timeScale);
			animationSprite.timeScale = num * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0) * attackAnimeTimeScale * (attackIntervalBase + attackIntervalBase / 3.0) / (attackInterval + attackIntervalBase / 3.0);
			this.OnAttackDps?.Invoke(delta);
		}
	}

	public void AttackExited()
	{
		this.OnAttackOver?.Invoke();
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (base.CanExecuteGameplay && alive && GodotObject.IsInstanceValid(parent) && !parent.die && !parent.nearDie && command == attackEventName)
		{
			_attackEventSequence++;
			this.OnAttackSeeded?.Invoke(NetworkDeterministicSeed.ForCharacterEvent(parent?.syncId ?? (-1), _attackEventSequence, 44957458157uL));
			this.OnAttack?.Invoke();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (alive && attackAnimeClipsArray.Contains(clip))
		{
			SendStateEvent(idleStateEvent);
		}
	}

	public void ComponentChange()
	{
		if (alive)
		{
			SendStateEvent(idleStateEvent);
		}
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "timer", timer },
			{ "checkIntrevalNow", checkIntrevalNow },
			{ "attackEventSequence", _attackEventSequence },
			{ "checkAreaEnabled", _checkAreaEnabled }
		};
		Array<Dictionary> array = ExportCheckAreaGeometryOverrides();
		if (array != null)
		{
			dictionary["checkAreaGeometryOverrides"] = array;
		}
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (GodotObject.IsInstanceValid(animationSprite))
		{
			dictionary["animationState"] = animationSprite.ExportSpriteSave();
		}
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["target"] = target.Name.ToString().ValidateNodeName();
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		_progressAnimationStateMissing = !_data.ContainsKey("animationState");
		timer = _data.GetValueOrDefault("timer", 0.0).AsDouble();
		RefreshPhysicsProcessEligibility();
		checkIntrevalNow = _data.GetValueOrDefault("checkIntrevalNow", 0).AsInt32();
		_attackEventSequence = Math.Max(0L, _data.GetValueOrDefault("attackEventSequence", 0L).AsInt64());
		ImportCheckAreaGeometryState(_data);
		string text = _data.GetValueOrDefault("target", "").AsString();
		if (text != "" && _owner?.charcterDicionary != null && _owner.charcterDicionary.ContainsKey(new StringName(text)))
		{
			target = _owner.charcterDicionary[new StringName(text)];
		}
		AdobeAnimateSprite animationSprite = GetAnimationSprite();
		if (GodotObject.IsInstanceValid(animationSprite) && _data.ContainsKey("animationState"))
		{
			Dictionary dictionary = _data["animationState"].AsGodotDictionary();
			if (dictionary.Count > 0)
			{
				animationSprite.ImportSpriteSave(dictionary);
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "timer", timer },
			{ "checkIntrevalNow", checkIntrevalNow },
			{ "attackEventSequence", _attackEventSequence },
			{ "checkAreaEnabled", _checkAreaEnabled }
		};
		Array<Dictionary> array = ExportCheckAreaGeometryOverrides();
		if (array != null)
		{
			dictionary["checkAreaGeometryOverrides"] = array;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		timer = _data.GetValueOrDefault("timer", timer).AsDouble();
		RefreshPhysicsProcessEligibility();
		checkIntrevalNow = _data.GetValueOrDefault("checkIntrevalNow", checkIntrevalNow).AsInt32();
		long val = _data.GetValueOrDefault("attackEventSequence", _attackEventSequence).AsInt64();
		_attackEventSequence = Math.Max(_attackEventSequence, val);
		ImportCheckAreaGeometryState(_data);
	}

	private Array<Dictionary> ExportCheckAreaGeometryOverrides()
	{
		if (_serializedGeometryOverridesRevision == _checkGeometryRevision)
		{
			return _serializedGeometryOverridesCache;
		}
		_serializedGeometryOverridesRevision = _checkGeometryRevision;
		_serializedGeometryOverridesCache = null;
		if (_checkShapeOverrides == null)
		{
			return null;
		}
		Array<Dictionary> array = null;
		for (int i = 0; i < _checkShapeOverrides.Length; i++)
		{
			CheckShapeOverride checkShapeOverride = _checkShapeOverrides[i];
			if (checkShapeOverride.Flags != CheckShapeOverrideFlags.None)
			{
				if (array == null)
				{
					array = new Array<Dictionary>();
				}
				Dictionary dictionary = new Dictionary
				{
					["index"] = i,
					["flags"] = (int)checkShapeOverride.Flags
				};
				if ((checkShapeOverride.Flags & CheckShapeOverrideFlags.Enabled) != CheckShapeOverrideFlags.None)
				{
					dictionary["enabled"] = checkShapeOverride.Enabled;
				}
				if ((checkShapeOverride.Flags & CheckShapeOverrideFlags.LocalTransform) != CheckShapeOverrideFlags.None)
				{
					dictionary["localTransform"] = checkShapeOverride.LocalTransform;
				}
				if ((checkShapeOverride.Flags & CheckShapeOverrideFlags.RectangleSize) != CheckShapeOverrideFlags.None)
				{
					dictionary["rectangleSize"] = checkShapeOverride.RectangleSize;
				}
				if ((checkShapeOverride.Flags & CheckShapeOverrideFlags.SegmentEnd) != CheckShapeOverrideFlags.None)
				{
					dictionary["segmentEnd"] = checkShapeOverride.SegmentEnd;
				}
				array.Add(dictionary);
			}
		}
		_serializedGeometryOverridesCache = array;
		return _serializedGeometryOverridesCache;
	}

	private void ImportCheckAreaGeometryState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		bool flag = false;
		if (data.ContainsKey("checkAreaEnabled"))
		{
			bool flag2 = data["checkAreaEnabled"].AsBool();
			flag |= _checkAreaEnabled != flag2;
			_checkAreaEnabled = flag2;
		}
		if (data.ContainsKey("checkAreaGeometryOverrides"))
		{
			_checkShapeOverrides = null;
			Godot.Collections.Array array = data["checkAreaGeometryOverrides"].AsGodotArray();
			for (int i = 0; i < array.Count; i++)
			{
				Dictionary dictionary = array[i].AsGodotDictionary();
				int num = dictionary.GetValueOrDefault("index", -1).AsInt32();
				if (!TryGetCheckAreaShapeResource(num, out var resource))
				{
					continue;
				}
				CheckShapeOverrideFlags checkShapeOverrideFlags = (CheckShapeOverrideFlags)dictionary.GetValueOrDefault("flags", 0).AsInt32();
				checkShapeOverrideFlags &= CheckShapeOverrideFlags.Enabled | CheckShapeOverrideFlags.LocalTransform | CheckShapeOverrideFlags.RectangleSize | CheckShapeOverrideFlags.SegmentEnd;
				bool enabled = dictionary.GetValueOrDefault("enabled", true).AsBool();
				Transform2D localTransform = dictionary.GetValueOrDefault("localTransform", Transform2D.Identity).AsTransform2D();
				Vector2 rectangleSize = dictionary.GetValueOrDefault("rectangleSize", Vector2.Zero).AsVector2();
				Vector2 segmentEnd = dictionary.GetValueOrDefault("segmentEnd", Vector2.Zero).AsVector2();
				if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.LocalTransform) != CheckShapeOverrideFlags.None && !localTransform.IsFinite())
				{
					checkShapeOverrideFlags &= ~CheckShapeOverrideFlags.LocalTransform;
				}
				if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.RectangleSize) != CheckShapeOverrideFlags.None && (!(resource.Geometry is RectangleShape2D) || !rectangleSize.IsFinite() || rectangleSize.X < 0f || rectangleSize.Y < 0f))
				{
					checkShapeOverrideFlags &= ~CheckShapeOverrideFlags.RectangleSize;
				}
				if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.SegmentEnd) != CheckShapeOverrideFlags.None && (!(resource.Geometry is SegmentShape2D) || !segmentEnd.IsFinite()))
				{
					checkShapeOverrideFlags &= ~CheckShapeOverrideFlags.SegmentEnd;
				}
				if (checkShapeOverrideFlags != CheckShapeOverrideFlags.None)
				{
					EnsureCheckShapeOverrides();
					ref CheckShapeOverride reference = ref _checkShapeOverrides[num];
					reference.Flags = checkShapeOverrideFlags;
					if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.Enabled) != CheckShapeOverrideFlags.None)
					{
						reference.Enabled = enabled;
					}
					if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.LocalTransform) != CheckShapeOverrideFlags.None)
					{
						reference.LocalTransform = localTransform;
					}
					if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.RectangleSize) != CheckShapeOverrideFlags.None)
					{
						reference.RectangleSize = rectangleSize;
					}
					if ((checkShapeOverrideFlags & CheckShapeOverrideFlags.SegmentEnd) != CheckShapeOverrideFlags.None)
					{
						reference.SegmentEnd = segmentEnd;
					}
				}
			}
			flag = true;
		}
		if (flag)
		{
			MarkCheckGeometryChanged();
		}
	}
}
