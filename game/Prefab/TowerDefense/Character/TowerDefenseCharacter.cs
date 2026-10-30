using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[Tool]
[GlobalClass]
public partial class TowerDefenseCharacter : TowerDefenseGroundItemBase, IAabbCollisionPreview2D
{
	private enum MainRuntimeState : byte
	{
		Idle,
		Sleep,
		Component
	}

	private sealed class MainStatePhysicsFastCallbackTarget : IStateMachinePhysicsFastCallbackTarget
	{
		private readonly TowerDefenseCharacter _owner;

		internal MainStatePhysicsFastCallbackTarget(TowerDefenseCharacter owner)
		{
			_owner = owner;
		}

		void IStateMachinePhysicsFastCallbackTarget.InvokeStateMachinePhysicsFastCallback(int callbackId, double delta)
		{
			switch (callbackId)
			{
			case 1:
				_owner.IdleProcessing(delta);
				break;
			case 2:
				_owner.SleepProcessing(delta);
				break;
			}
		}
	}

	[Flags]
	public enum HitBoxSuppressionReason : uint
	{
		None = 0u,
		Manual = 1u,
		Paused = 2u,
		Destroyed = 4u,
		Carried = 8u,
		Tanglekelp = 0x10u,
		Scripted = 0x20u,
		PlanternTanglekelp = 0x40u
	}

	public delegate void DestroyEventHandler(TowerDefenseCharacter character);

	public delegate void CharacterNearDieEventHandler(TowerDefenseCharacter character);

	public delegate void BodyHurtEventHandler(int num);

	public delegate void ArmorHurtEventHandler(int num);

	public delegate void RiseOverEventHandler();

	public delegate void ComponentChangeEventHandler();

	public delegate void DamageBlockedEventHandler();

	private sealed partial class TransformWatcher : Node
	{
		private readonly TowerDefenseCharacter _source;

		private readonly TowerDefenseCharacter _target;

		private readonly double _bodyHp;

		private readonly bool _syncBodyHp;

		private readonly bool _removeSource;

		private readonly Action<TowerDefenseCharacter> _onTargetReady;

		private bool _readyInvoked;

		private int _waitFrames;

		private int _elapsedFrames;

		public TransformWatcher(TowerDefenseCharacter source, TowerDefenseCharacter target, double bodyHp, bool syncBodyHp, bool removeSource, Action<TowerDefenseCharacter> onTargetReady)
		{
			_source = source;
			_target = target;
			_bodyHp = bodyHp;
			_syncBodyHp = syncBodyHp;
			_removeSource = removeSource;
			_onTargetReady = onTargetReady;
		}

		public override void _Process(double delta)
		{
			_elapsedFrames++;
			if (_elapsedFrames > 120)
			{
				QueueFree();
				return;
			}
			if (!_readyInvoked)
			{
				if (!GodotObject.IsInstanceValid(_target))
				{
					QueueFree();
					return;
				}
				if (!_target.IsInsideTree())
				{
					return;
				}
				_readyInvoked = true;
				if (_syncBodyHp && GodotObject.IsInstanceValid(_target.instance))
				{
					_target.instance.hitpointsSave = _bodyHp;
					_target.instance.hitpoints = _bodyHp;
				}
				_onTargetReady?.Invoke(_target);
			}
			if (_waitFrames < 2)
			{
				_waitFrames++;
				return;
			}
			QueueFree();
			if (_removeSource)
			{
				_source.SilentlyRemove();
			}
		}
	}

	private const int IdlePhysicsCallbackId = 1;

	private const int SleepPhysicsCallbackId = 2;

	private static readonly StringName ToIdleStateEvent = new StringName("ToIdle");

	private static readonly StringName ToSleepStateEvent = new StringName("ToSleep");

	private static readonly StringName ToComponentStateEvent = new StringName("ToComponent");

	protected static readonly bool CachedEditorHint = Engine.IsEditorHint();

	public static bool UseCharacterBatch = true;

	public int syncId = -1;

	private static PackedScene _FIRE;

	private static PackedScene _ICE_FIRE;

	private static PackedScene _MEGA_FIRE;

	private static PackedScene _PURIFY_FIRE;

	private static PackedScene _WHITE_FIRE;

	private static PackedScene _GARLIC_FIRE;

	private static PackedScene _SNOW_FLAKES;

	protected const int FRAME_CHECK_INTERVAL = 5;

	private const double NEAR_DEATH_DAMAGE_DIVISOR = 3.0;

	public Node2D backEffectNode;

	public Node2D frontEffectNode;

	public Node2D spriteGroup;

	public Marker2D transformPoint;

	public TowerDefenseShadowVisual shadowSprite;

	private CharacterHitBoxDefinition _hitBoxDefinition;

	private bool _hitBoxRuntimeInitialized;

	private bool _hitBoxAvailable;

	private bool _hitBoxDefaultEnabled;

	private bool _hitBoxDefaultMonitorable;

	private HitBoxSuppressionReason _hitBoxSuppression;

	private HitBoxSuppressionReason _hitBoxMonitorSuppression;

	private bool _canReceiveExplosionHit;

	private bool _hitBoxPreviewDraw;

	private Rect2 _worldHitRectCache;

	private long _worldHitRectCacheFrame = -1L;

	private Transform2D _worldHitRectCacheTransform;

	private float _worldHitRectCacheScaleX;

	private float _localScaleXSnapshot = 1f;

	private bool _localScaleXSnapshotValid;

	private CharacterHitBoxDefinition _worldHitRectCacheDefinition;

	private bool _worldHitRectCacheValid;

	private ulong _hitBoxBoundsRevision = 1uL;

	private Vector2 _renderAncestorScaleSnapshot = Vector2.One;

	private bool _renderAncestorScaleSnapshotValid;

	private StateMachineController _mainStateMachineController;

	private bool _mainStateMachineDispatchEnabled;

	private bool _ownerBatchRegistered;

	private bool _batchUsesInheritedProcessMode = true;

	private Node _batchProcessModeParent;

	private bool _mainStateMachineReleased;

	private int _authoritativeMainStateRestoreDepth;

	private BattleEventBus.CharacterSkinSwitchedEventHandler _characterSkinSwitchedHandler;

	private BattleEventBus _characterSkinSwitchedEventBus;

	private bool _characterSkinSwitchedSubscribed;

	private StateMachineSnapshot _pendingMainStateMachineSnapshot;

	private bool _pendingMainStateMachineSnapshotIsRemote;

	private bool _pendingMainStateMachineSnapshotSuppressEffects;

	private bool _pendingMainStateMachineSnapshotProgressCompatible;

	private long _lastRemoteMainStateMachineRevision = -1L;

	private ulong _lastMainStateMachineProcessFrame = 18446744073709551615uL;

	private ulong _lastComponentStateMachineProcessFrame = 18446744073709551615uL;

	private ulong _lastMainStateMachinePhysicsFrame = 18446744073709551615uL;

	private bool _bowlingImpactDisplacementActive;

	private float _bowlingImpactStartGlobalX;

	private float _bowlingImpactTargetGlobalX;

	private double _bowlingImpactElapsed;

	private double _bowlingImpactDuration;

	private StateHandle _idleStateHandle;

	private StateHandle _sleepStateHandle;

	private StateHandle _componentStateHandle;

	private MainStatePhysicsFastCallbackTarget _mainStatePhysicsFastCallbackTarget;

	private bool _characterStateHandlesConnected;

	public ShowHealthComponent showHealthComponent;

	[Export(PropertyHint.None, "")]
	public Vector2 showHealthOffset = Vector2.Zero;

	public ComponentManager componentManager;

	public BuffComponent buff;

	public ShadowComponent shadowComponent;

	public TargetRegistrationComponent targetRegistrationComponent;

	public RiseComponent riseComponent;

	public DestroyComponent destroyComponent;

	public HitFlashComponent hitFlashComponent;

	public ShaderEffectComponent shaderEffectComponent;

	public BlowBackComponent blowBackComponent;

	public SleepComponent sleepComponent;

	public ResourceSpawnComponent resourceSpawnComponent;

	public GroundHeightComponent groundHeightComponent;

	public DamagePartComponent damagePartComponent;

	public HurtComponent hurtComponent;

	public HypnosesComponent hypnosesComponent;

	public ArmorVisualComponent armorVisualComponent;

	public CustomVisualComponent customVisualComponent;

	public RecycleComponent recycleComponent;

	public EffectCreateComponent effectCreateComponent;

	public PhonkComponent phonkComponent;

	public DismemberComponent dismemberComponent;

	private bool _resourceComponentFacadesCurrent;

	private bool _invisible;

	[Export(PropertyHint.None, "")]
	public string idleAnimeClip = "Idle";

	[Export(PropertyHint.None, "")]
	public string sleepAnimeClip = "Idle";

	private TowerDefenseCharacterConfig _config;

	private AdobeAnimateSprite _sprite;

	private AdobeAnimateSlot _headSlot;

	private TowerDefenseEnum.CHARACTER_CAMP _camp;

	private EconomyAccountId _economyOwnerAccountId;

	private string _damagePartClip = "particles";

	private Dictionary _damagePart;

	private List<string> _damagePartList;

	private Dictionary _damagePartSlot;

	private double _previewDamagePointPersontage = 1.0;

	private Array<string> _currentArmor;

	private Array<string> _currentCustom;

	public TowerDefenseCharacterInstance instance;

	public bool characterDisabled;

	private bool _componentAlive = true;

	public bool componentRunning;

	public bool characterFilter;

	public double timeScaleInit = 1.0;

	public double timeScale = 1.0;

	public double timeScaleSave = 1.0;

	private List<TowerDefenseCharacterEventBase> _dieEvent;

	public bool inGame = true;

	public bool editorPreviewMode;

	public bool editorMapPreviewMode;

	[Export(PropertyHint.None, "")]
	public bool forceLocalRenderDuringZMotion;

	public bool isShow;

	public TowerDefensePacketConfig packet;

	public double cost;

	public bool nearDie;

	public bool die;

	public bool canMowerMove;

	public Vector2 baseSpriteScale;

	public bool isRise;

	public bool isShovel;

	public bool isSmash;

	public bool isExplode;

	public bool isChomp;

	public bool skipDestroySet;

	internal bool suppressDeathrattles;

	public bool mowerDeathVisualOwned;

	private bool _inWater;

	public bool iceSpeedDown;

	public bool emSpeedDown;

	public bool useIdleAnimeReset = true;

	private bool _syncApplyingAnimation;

	public bool isUnlimitedFire;

	public int randFreshIndex;

	public double groundRight;

	private bool _componentGameplayUntilBattlefieldEntry;

	private bool _componentGameplayPhysicsDispatchActive;

	private bool _componentGameplayPhysicsSnapshot;

	private ulong _componentGameplayPhysicsSnapshotFrame = 18446744073709551615uL;

	private ulong _physicsGlobalPositionFrame = 18446744073709551615uL;

	private Vector2 _physicsGlobalPosition;

	private Transform2D _shadowGlobalTransform;

	private bool _shadowGlobalTransformValid;

	private Vector2 _physicsLocalPosition;

	private bool _physicsLocalPositionValid;

	private bool _writingCachedGlobalPosition;

	private Transform2D _pendingCachedGlobalTransform;

	private bool _pendingCachedGlobalTransformNotification;

	private Tween _cellMoveTween;

	private static readonly StringName PropPreviewDamagePoint = "PreviewDamagePointPersontage";

	private static readonly StringName PropArmor = "Armor";

	private static readonly StringName PropCustom = "Custom";

	private const string DamagePartSlotPrefix = "DamagePartSlot/";

	private const int ZMotionLocalRenderReleasePhysicsFrames = 2;

	private bool _zMotionOwnsLocalRender;

	private int _zMotionStablePhysicsFrames;

	private float _spriteGroupLocalX;

	private bool _spriteGroupLocalXCaptured;

	public bool isDestroy;

	private (bool Sleep, bool WakeUp)? _pendingCoverSleepState;

	private bool _progressRestorePresentationRefreshPending;

	private static PackedScene _defaultTransformEffect;

	private const int TransformTimeoutFrames = 120;

	private const int TransformSettleFrames = 2;

	private readonly List<AdobeAnimateExternalVisualHandle> _legacySpriteGroupVisualHandles = new List<AdobeAnimateExternalVisualHandle>();

	private bool _characterExternalVisualsInitialized;

	private bool _legacySpriteGroupVisualsScanned;

	public bool IsRemoteNetworkReplica
	{
		get
		{
			if (syncId >= 0 && Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	public bool HasValidRuntimeConfiguration { get; private set; }

	private static PackedScene FIRE => _FIRE ?? (_FIRE = GD.Load<PackedScene>("uid://c4fjg42kgeupv"));

	private static PackedScene ICE_FIRE => _ICE_FIRE ?? (_ICE_FIRE = GD.Load<PackedScene>("uid://bvuvd1ircxhxn"));

	private static PackedScene MEGA_FIRE => _MEGA_FIRE ?? (_MEGA_FIRE = GD.Load<PackedScene>("uid://c61k0gdy05vo4"));

	private static PackedScene PURIFY_FIRE => _PURIFY_FIRE ?? (_PURIFY_FIRE = GD.Load<PackedScene>("uid://cxmk8jiq0nkjg"));

	private static PackedScene WHITE_FIRE => _WHITE_FIRE ?? (_WHITE_FIRE = GD.Load<PackedScene>("uid://dsxnymbe4mghd"));

	private static PackedScene GARLIC_FIRE => _GARLIC_FIRE ?? (_GARLIC_FIRE = GD.Load<PackedScene>("uid://dsxnymbe4mghd"));

	private static PackedScene SNOW_FLAKES => _SNOW_FLAKES ?? (_SNOW_FLAKES = GD.Load<PackedScene>("uid://b1ba7ajcvcgj8"));

	public bool HasArmorHurtSubscribers => this.OnArmorHurt != null;

	public Sprite2D icetrapSprite => buff?.GetCachedBuffVisual("Frozen");

	[Export(PropertyHint.None, "")]
	public CharacterHitBoxDefinition HitBoxDefinition
	{
		get
		{
			return _hitBoxDefinition;
		}
		set
		{
			if (_hitBoxDefinition != value)
			{
				_hitBoxDefinition = value;
				if (!_hitBoxRuntimeInitialized)
				{
					ResetHitBoxRuntimeFromDefinition();
				}
				InvalidateHitBoxBounds();
				QueueRedraw();
			}
		}
	}

	internal ulong HitBoxBoundsRevision => _hitBoxBoundsRevision;

	public bool HasHitBox
	{
		get
		{
			EnsureHitBoxRuntimeInitialized();
			if (_hitBoxAvailable && GodotObject.IsInstanceValid(_hitBoxDefinition))
			{
				return _hitBoxDefinition.HasValidGeometry;
			}
			return false;
		}
	}

	public bool IsHitBoxEnabled
	{
		get
		{
			EnsureHitBoxRuntimeInitialized();
			if (HasHitBox && _hitBoxDefaultEnabled)
			{
				return _hitBoxSuppression == HitBoxSuppressionReason.None;
			}
			return false;
		}
	}

	public bool IsHitBoxMonitorable
	{
		get
		{
			EnsureHitBoxRuntimeInitialized();
			if (HasHitBox && _hitBoxDefaultMonitorable)
			{
				return _hitBoxMonitorSuppression == HitBoxSuppressionReason.None;
			}
			return false;
		}
	}

	public Rect2 WorldBroadphaseRect => WorldHitRect;

	internal float CachedLocalScaleX
	{
		get
		{
			if (!_localScaleXSnapshotValid)
			{
				RefreshLocalScaleXSnapshot();
			}
			return _localScaleXSnapshot;
		}
	}

	public Rect2 WorldHitRect
	{
		get
		{
			ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (num == 18446744073709551615uL)
			{
				num = Engine.GetPhysicsFrames();
			}
			if (_worldHitRectCacheValid && _worldHitRectCacheFrame == (long)num && _worldHitRectCacheDefinition == _hitBoxDefinition)
			{
				return _worldHitRectCache;
			}
			Transform2D globalTransformForShadow = GetGlobalTransformForShadow(num);
			if (!_localScaleXSnapshotValid)
			{
				RefreshLocalScaleXSnapshot();
			}
			_worldHitRectCache = ((GodotObject.IsInstanceValid(_hitBoxDefinition) && _hitBoxDefinition.TryGetWorldRect(globalTransformForShadow, out var rect)) ? rect : AabbShapeUtil.RectFromCenter(globalTransformForShadow.Origin, AabbShapeUtil.DefaultAreaSize));
			_worldHitRectCacheFrame = (long)num;
			_worldHitRectCacheTransform = globalTransformForShadow;
			_worldHitRectCacheScaleX = _localScaleXSnapshot;
			_worldHitRectCacheDefinition = _hitBoxDefinition;
			_worldHitRectCacheValid = true;
			return _worldHitRectCache;
		}
	}

	[Export(PropertyHint.None, "")]
	public StateMachineDefinition MainStateMachineDefinition { get; set; }

	public IStateMachineController StateMachine { get; private set; }

	public StateHandle CurrentStateHandle => StateMachine?.CurrentStateHandle;

	internal bool WantsMainStateMachineProcessDispatch
	{
		get
		{
			if (_mainStateMachineDispatchEnabled)
			{
				StateMachineController mainStateMachineController = _mainStateMachineController;
				if (mainStateMachineController == null || !mainStateMachineController.HasProcessWork)
				{
					return componentManager?.HasStateMachineProcessWork ?? false;
				}
				return true;
			}
			return false;
		}
	}

	internal bool IsBowlingImpactDisplacementActive => _bowlingImpactDisplacementActive;

	internal bool IsOwnerBatchRegistered => _ownerBatchRegistered;

	internal bool BatchUsesInheritedProcessMode => _batchUsesInheritedProcessMode;

	internal Node BatchProcessModeParent => _batchProcessModeParent;

	internal bool IsOwnerBatchDispatchActive
	{
		get
		{
			if (_ownerBatchRegistered && inGame)
			{
				return !isDestroy;
			}
			return false;
		}
	}

	private bool CanDispatchMainStateMachine
	{
		get
		{
			if (_mainStateMachineDispatchEnabled)
			{
				StateMachineController mainStateMachineController = _mainStateMachineController;
				if (mainStateMachineController != null && mainStateMachineController.IsInitialized && inGame)
				{
					return !isDestroy;
				}
			}
			return false;
		}
	}

	private bool CanDispatchOwnedStateMachines
	{
		get
		{
			if (_mainStateMachineDispatchEnabled && inGame)
			{
				return !isDestroy;
			}
			return false;
		}
	}

	[Export(PropertyHint.None, "")]
	public CharacterComponentSet ComponentSet { get; set; }

	[Export(PropertyHint.None, "")]
	public bool invisible
	{
		get
		{
			return _invisible;
		}
		set
		{
			if (_invisible == value)
			{
				return;
			}
			_invisible = value;
			if (!IsNodeReady())
			{
				return;
			}
			if (_invisible)
			{
				sprite.invisible = true;
				sprite.Visible = false;
				shadowSprite.Visible = false;
				ShadowComponent obj = shadowComponent;
				if (obj != null && !obj.IsReleased)
				{
					shadowComponent.SetShadowVisible(visible: false);
				}
				ShowHealthComponent obj2 = showHealthComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					showHealthComponent.Visible = false;
				}
			}
			else
			{
				sprite.invisible = false;
				sprite.Visible = true;
				shadowSprite.Visible = !inWater;
				ShadowComponent obj3 = shadowComponent;
				if (obj3 != null && !obj3.IsReleased)
				{
					shadowComponent.SetShadowVisible(!inWater);
				}
				ShowHealthComponent obj4 = showHealthComponent;
				if (obj4 != null && !obj4.IsReleased)
				{
					showHealthComponent.Visible = true;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterConfig config
	{
		get
		{
			return _config;
		}
		set
		{
			_config = value;
			if (_currentCustom != null)
			{
				_currentCustom = new Array<string>();
			}
			if (IsNodeReady())
			{
				if (!CachedEditorHint && GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
				{
					TowerDefenseManager.Instance.characterRegistry.NotifyCharacterConfigChanged(this);
				}
				DamagePartInit();
				NotifyPropertyListChanged();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite sprite
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_sprite))
			{
				return null;
			}
			return _sprite;
		}
		set
		{
			_sprite = value;
			if (IsNodeReady())
			{
				if (config != null && config.customData != null && sprite != null)
				{
					SetCustoms(currentCustom);
				}
				DamagePartInit();
				NotifyPropertyListChanged();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSlot headSlot
	{
		get
		{
			return _headSlot;
		}
		set
		{
			_headSlot = value;
			if (IsNodeReady())
			{
				DamagePartInit();
				NotifyPropertyListChanged();
			}
		}
	}

	public EconomyAccountId EconomyOwnerAccountId => _economyOwnerAccountId;

	public bool HasEconomyOwner => _economyOwnerAccountId.IsValid;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_CAMP camp
	{
		get
		{
			return _camp;
		}
		set
		{
			if (_camp != value)
			{
				_camp = value;
				TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
				if (GodotObject.IsInstanceValid(towerDefenseManager) && GodotObject.IsInstanceValid(towerDefenseManager.characterRegistry))
				{
					towerDefenseManager.characterRegistry.NotifyCharacterCampChanged(this);
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string damagePartClip
	{
		get
		{
			return _damagePartClip;
		}
		set
		{
			_damagePartClip = value;
			if (IsNodeReady())
			{
				DamagePartInit();
				NotifyPropertyListChanged();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Dictionary damagePart
	{
		get
		{
			return _damagePart ?? (_damagePart = new Dictionary());
		}
		set
		{
			_damagePart = value;
		}
	}

	public List<string> damagePartList => _damagePartList ?? (_damagePartList = new List<string>());

	[Export(PropertyHint.None, "")]
	public Dictionary damagePartSlot
	{
		get
		{
			return _damagePartSlot ?? (_damagePartSlot = new Dictionary());
		}
		set
		{
			_damagePartSlot = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public double previewDamagePointPersontage
	{
		get
		{
			return _previewDamagePointPersontage;
		}
		set
		{
			_previewDamagePointPersontage = value;
			if (IsNodeReady() && config != null && config.damagePointData != null && sprite != null)
			{
				PreviewDamagePoint(value);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<string> currentArmor
	{
		get
		{
			return _currentArmor ?? (_currentArmor = new Array<string>());
		}
		set
		{
			_currentArmor = value;
			if (IsNodeReady() && config != null && config.armorData != null && sprite != null)
			{
				SetArmors(_currentArmor);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<string> currentCustom
	{
		get
		{
			return _currentCustom ?? (_currentCustom = new Array<string>());
		}
		set
		{
			_currentCustom = value;
			if (IsNodeReady() && config != null && config.customData != null && sprite != null)
			{
				SetCustoms(_currentCustom);
			}
		}
	}

	public bool componentAlive
	{
		get
		{
			if (_componentAlive)
			{
				return !characterDisabled;
			}
			return false;
		}
		set
		{
			_componentAlive = value;
		}
	}

	public List<TowerDefenseCharacterEventBase> dieEvent => _dieEvent ?? (_dieEvent = new List<TowerDefenseCharacterEventBase>());

	public bool inWater
	{
		get
		{
			return _inWater;
		}
		set
		{
			if (_inWater == value)
			{
				return;
			}
			_inWater = value;
			if (inGame)
			{
				if (value)
				{
					InWater();
				}
				else
				{
					OutWater();
				}
			}
			groundHeightComponent?.NotifyEnvironmentChanged();
		}
	}

	public bool HasComponentGameplayUntilBattlefieldEntry => _componentGameplayUntilBattlefieldEntry;

	public bool IsInsideComponentBattlefield
	{
		get
		{
			if (!inGame || _componentGameplayUntilBattlefieldEntry || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				return true;
			}
			float x = GetLogicalGlobalPosition().X;
			if ((double)x >= TowerDefenseManager.Instance.GetMapGroundLeft())
			{
				return (double)x <= TowerDefenseManager.Instance.GetMapGroundRight();
			}
			return false;
		}
	}

	internal bool IsInsideComponentBattlefieldForRuntime
	{
		get
		{
			if (!_componentGameplayPhysicsDispatchActive)
			{
				return GetComponentGameplayForCurrentPhysicsFrame();
			}
			return _componentGameplayPhysicsSnapshot;
		}
	}

	public virtual bool PreserveDeathTransformation => false;

	public virtual bool IsHardControlImmune => false;

	public bool IsProgressRestoreInFlight { get; private set; }

	private static PackedScene DefaultTransformEffect => _defaultTransformEffect ?? (_defaultTransformEffect = GD.Load<PackedScene>("uid://k8ms8gteeqp6"));

	public int CharacterExternalVisualCompatibilityScanCount { get; private set; }

	public int CharacterAnimatedStatusVisualCount { get; private set; }

	public int CharacterAnimatedStatusTopologyChangeCount { get; private set; }

	public event DestroyEventHandler OnDestroy;

	public event CharacterNearDieEventHandler OnCharacterNearDie;

	public event BodyHurtEventHandler OnBodyHurt;

	public event ArmorHurtEventHandler OnArmorHurt;

	public event RiseOverEventHandler OnRiseOver;

	public event ComponentChangeEventHandler OnComponentChange;

	public event DamageBlockedEventHandler OnDamageBlocked;

	public void EmitDestroy()
	{
		this.OnDestroy?.Invoke(this);
	}

	public void EmitBodyHurt(int num)
	{
		this.OnBodyHurt?.Invoke(num);
	}

	public void EmitArmorHurt(int num)
	{
		this.OnArmorHurt?.Invoke(num);
	}

	public void EmitRiseOver()
	{
		this.OnRiseOver?.Invoke();
	}

	public void EmitComponentChange()
	{
		this.OnComponentChange?.Invoke();
	}

	public void EmitDamageBlocked()
	{
		this.OnDamageBlocked?.Invoke();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool CanReceiveExplosionHit()
	{
		if (base.ProcessMode == ProcessModeEnum.Disabled)
		{
			return false;
		}
		EnsureHitBoxRuntimeInitialized();
		return _canReceiveExplosionHit;
	}

	public void SetHitBoxEnabled(bool enabled)
	{
		EnsureHitBoxRuntimeInitialized();
		if (_hitBoxDefaultEnabled != enabled)
		{
			_hitBoxDefaultEnabled = enabled;
			InvalidateHitBoxBounds();
		}
	}

	public void SetHitBoxMonitorable(bool monitorable)
	{
		EnsureHitBoxRuntimeInitialized();
		if (_hitBoxDefaultMonitorable != monitorable)
		{
			_hitBoxDefaultMonitorable = monitorable;
			RefreshExplosionHitEligibility();
			InvalidateHitBoxBounds();
		}
	}

	public void SetHitBoxSuppressed(HitBoxSuppressionReason reason, bool suppressed)
	{
		if (reason != HitBoxSuppressionReason.None)
		{
			EnsureHitBoxRuntimeInitialized();
			HitBoxSuppressionReason hitBoxSuppression = _hitBoxSuppression;
			if (suppressed)
			{
				_hitBoxSuppression |= reason;
			}
			else
			{
				_hitBoxSuppression &= ~reason;
			}
			if (_hitBoxSuppression != hitBoxSuppression)
			{
				InvalidateHitBoxBounds();
			}
		}
	}

	public void SetHitBoxMonitorSuppressed(HitBoxSuppressionReason reason, bool suppressed)
	{
		if (reason != HitBoxSuppressionReason.None)
		{
			EnsureHitBoxRuntimeInitialized();
			HitBoxSuppressionReason hitBoxMonitorSuppression = _hitBoxMonitorSuppression;
			if (suppressed)
			{
				_hitBoxMonitorSuppression |= reason;
			}
			else
			{
				_hitBoxMonitorSuppression &= ~reason;
			}
			if (_hitBoxMonitorSuppression != hitBoxMonitorSuppression)
			{
				RefreshExplosionHitEligibility();
				InvalidateHitBoxBounds();
			}
		}
	}

	public void DestroyHitBoxRuntime()
	{
		EnsureHitBoxRuntimeInitialized();
		_hitBoxAvailable = false;
		_hitBoxSuppression |= HitBoxSuppressionReason.Destroyed;
		_hitBoxMonitorSuppression |= HitBoxSuppressionReason.Destroyed;
		RefreshExplosionHitEligibility();
		InvalidateHitBoxBounds();
	}

	public void InvalidateHitBoxBounds()
	{
		_hitBoxBoundsRevision++;
		if (_hitBoxBoundsRevision == 0L)
		{
			_hitBoxBoundsRevision = 1uL;
		}
		_worldHitRectCacheValid = false;
		_worldHitRectCacheFrame = -1L;
		if (!CachedEditorHint && GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			TowerDefenseManager.Instance.characterRegistry.NotifyCharacterGeometryChanged(this);
		}
	}

	protected override void OnGridPositionChanged(Vector2I oldGridPos, Vector2I newGridPos)
	{
		base.OnGridPositionChanged(oldGridPos, newGridPos);
		groundHeightComponent?.NotifyCellChanged();
		if (!CachedEditorHint && GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			TowerDefenseManager.Instance.characterRegistry.NotifyCharacterGridChanged(this);
		}
	}

	protected override void OnGroundHeightChanged(double oldGroundHeight, double newGroundHeight)
	{
		base.OnGroundHeightChanged(oldGroundHeight, newGroundHeight);
		groundHeightComponent?.NotifyOwnerGroundHeightChanged();
		shadowComponent?.MarkDirty();
	}

	public bool TryGetActiveWorldHitRect(out Rect2 rect, bool requireMonitorable = false)
	{
		if (!IsHitBoxEnabled || (requireMonitorable && !IsHitBoxMonitorable))
		{
			rect = default(Rect2);
			return false;
		}
		rect = WorldHitRect;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetProjectileWorldHitRect(out Rect2 rect)
	{
		if (_hitBoxRuntimeInitialized && _hitBoxAvailable && _hitBoxDefaultEnabled && _hitBoxSuppression == HitBoxSuppressionReason.None && _worldHitRectCacheValid)
		{
			rect = _worldHitRectCache;
			return true;
		}
		return TryGetActiveWorldHitRect(out rect);
	}

	internal bool TryGetCachedHitTransform(out float originX, out float scaleX)
	{
		if (!_worldHitRectCacheValid)
		{
			originX = 0f;
			scaleX = 0f;
			return false;
		}
		originX = _worldHitRectCacheTransform.Origin.X;
		scaleX = _worldHitRectCacheScaleX;
		return true;
	}

	internal Vector2 GetCachedWorldPositionForProjectile()
	{
		if (!_worldHitRectCacheValid)
		{
			_ = WorldHitRect;
		}
		return _worldHitRectCacheTransform.Origin;
	}

	private void RefreshLocalScaleXSnapshot()
	{
		_localScaleXSnapshot = base.Scale.X;
		_localScaleXSnapshotValid = true;
	}

	private void NotifyRenderAncestorScaleChangedIfNeeded()
	{
		Vector2 scale = base.Scale;
		if (!_renderAncestorScaleSnapshotValid || !Mathf.IsEqualApprox(scale.X, _renderAncestorScaleSnapshot.X) || !Mathf.IsEqualApprox(scale.Y, _renderAncestorScaleSnapshot.Y))
		{
			_renderAncestorScaleSnapshot = scale;
			_renderAncestorScaleSnapshotValid = true;
			if (GodotObject.IsInstanceValid(_sprite))
			{
				_sprite.NotifyAncestorTransformChangedForRender();
			}
		}
	}

	private void EnsureHitBoxRuntimeInitialized()
	{
		if (!_hitBoxRuntimeInitialized)
		{
			ResetHitBoxRuntimeFromDefinition();
			_hitBoxRuntimeInitialized = true;
		}
	}

	private void ResetHitBoxRuntimeFromDefinition()
	{
		_hitBoxAvailable = GodotObject.IsInstanceValid(_hitBoxDefinition) && _hitBoxDefinition.HasValidGeometry;
		_hitBoxDefaultEnabled = _hitBoxDefinition?.DefaultEnabled ?? false;
		_hitBoxDefaultMonitorable = _hitBoxDefinition?.DefaultMonitorable ?? false;
		_hitBoxSuppression = HitBoxSuppressionReason.None;
		_hitBoxMonitorSuppression = HitBoxSuppressionReason.None;
		RefreshExplosionHitEligibility();
	}

	private void RefreshExplosionHitEligibility()
	{
		_canReceiveExplosionHit = _hitBoxAvailable && GodotObject.IsInstanceValid(_hitBoxDefinition) && _hitBoxDefinition.HasValidGeometry && _hitBoxDefaultMonitorable && _hitBoxMonitorSuppression == HitBoxSuppressionReason.None;
	}

	public void SetCollisionPreviewDraw(bool enabled)
	{
		_hitBoxPreviewDraw = enabled;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (CachedEditorHint || _hitBoxPreviewDraw)
		{
			DrawPrimaryHitBoxPreview();
			DrawResourceComponentCollisionPreviews();
		}
	}

	private void DrawPrimaryHitBoxPreview()
	{
		if (GodotObject.IsInstanceValid(_hitBoxDefinition) && _hitBoxDefinition.DebugDraw && _hitBoxDefinition.HasValidGeometry)
		{
			Vector2 vector = _hitBoxDefinition.Size * 0.5f;
			Transform2D localTransform = _hitBoxDefinition.LocalTransform;
			Vector2[] array = new Vector2[4]
			{
				localTransform * new Vector2(0f - vector.X, 0f - vector.Y),
				localTransform * new Vector2(vector.X, 0f - vector.Y),
				localTransform * new Vector2(vector.X, vector.Y),
				localTransform * new Vector2(0f - vector.X, vector.Y)
			};
			if (_hitBoxDefinition.DebugFillColor.A > 0f)
			{
				DrawColoredPolygon(array, _hitBoxDefinition.DebugFillColor);
			}
			Vector2[] points = new Vector2[5]
			{
				array[0],
				array[1],
				array[2],
				array[3],
				array[0]
			};
			DrawPolyline(points, _hitBoxDefinition.DebugOutlineColor, Mathf.Max(0.5f, _hitBoxDefinition.DebugLineWidth), antialiased: true);
		}
	}

	private void DrawResourceComponentCollisionPreviews()
	{
		CharacterComponentSet characterComponentSet = ComponentSet;
		if (!GodotObject.IsInstanceValid(characterComponentSet))
		{
			characterComponentSet = GetNodeOrNull<LegacyComponentManagerNode>("%ComponentManager")?.ComponentSet;
		}
		if (!GodotObject.IsInstanceValid(characterComponentSet))
		{
			return;
		}
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = characterComponentSet.GetFlattenedDefinitions();
		for (int i = 0; i < flattenedDefinitions.Count; i++)
		{
			CharacterComponentDefinition characterComponentDefinition = flattenedDefinitions[i];
			if (characterComponentDefinition is AttackComponentDefinition attackComponentDefinition)
			{
				AttackComponent attackComponent = componentManager?.GetRuntime<AttackComponent>(attackComponentDefinition.InstanceId);
				if (attackComponent != null && !attackComponent.IsReleased && attackComponent.DrawCheckAreaCollisionPreview(this))
				{
					continue;
				}
			}
			if (characterComponentDefinition is ICharacterComponentCollisionPreviewProvider characterComponentCollisionPreviewProvider)
			{
				for (int j = 0; j < characterComponentCollisionPreviewProvider.CollisionPreviewShapeCount; j++)
				{
					AabbCollisionResourceDebugDraw.DrawShape(this, characterComponentCollisionPreviewProvider.GetCollisionPreviewShape(j));
				}
				for (int k = 0; k < characterComponentCollisionPreviewProvider.CollisionPreviewRayCount; k++)
				{
					AabbCollisionResourceDebugDraw.DrawRay(this, characterComponentCollisionPreviewProvider.GetCollisionPreviewRay(k));
				}
			}
		}
	}

	public bool SendStateEvent(StringName eventName)
	{
		eventName = NormalizeMainStateEvent(eventName);
		if (_authoritativeMainStateRestoreDepth == 0)
		{
			return StateMachine?.SendEvent(eventName) ?? false;
		}
		return false;
	}

	private bool SetMainRuntimeState(MainRuntimeState state)
	{
		StateHandle targetState;
		StringName eventName;
		switch (state)
		{
		case MainRuntimeState.Sleep:
			targetState = _sleepStateHandle;
			eventName = ToSleepStateEvent;
			break;
		case MainRuntimeState.Component:
			targetState = _componentStateHandle;
			eventName = ToComponentStateEvent;
			break;
		default:
			targetState = _idleStateHandle;
			eventName = ToIdleStateEvent;
			break;
		}
		eventName = NormalizeMainStateEvent(eventName);
		if (_authoritativeMainStateRestoreDepth == 0)
		{
			IStateMachineController stateMachine = StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				if (!StateMachine.TrySetFlatState(targetState, eventName))
				{
					return StateMachine.SendEvent(eventName);
				}
				return true;
			}
		}
		return false;
	}

	protected virtual StringName NormalizeMainStateEvent(StringName eventName)
	{
		return eventName;
	}

	public StateHandle GetStateById(string stableId)
	{
		return StateMachine?.GetStateById(stableId);
	}

	internal void PrepareBatchRegistration()
	{
		_batchUsesInheritedProcessMode = base.ProcessMode == ProcessModeEnum.Inherit;
		_batchProcessModeParent = (_batchUsesInheritedProcessMode ? GetParent() : null);
	}

	public bool TryAssignEconomyOwner(EconomyAccountId accountId)
	{
		if (!accountId.IsValid)
		{
			return false;
		}
		if (_economyOwnerAccountId.IsValid)
		{
			return _economyOwnerAccountId == accountId;
		}
		_economyOwnerAccountId = accountId;
		return true;
	}

	internal void CancelCellMoveTween()
	{
		if (_cellMoveTween != null && _cellMoveTween.IsValid())
		{
			_cellMoveTween.Kill();
		}
		_cellMoveTween = null;
	}

	internal void TrackCellMoveTween(Tween tween)
	{
		_cellMoveTween = tween;
	}

	public void EnableComponentGameplayUntilBattlefieldEntry()
	{
		_componentGameplayUntilBattlefieldEntry = true;
	}

	private bool IsWithinComponentBattlefieldBoundsForPhysicsFrame()
	{
		if (!TowerDefenseProcessModeDispatch.TryGetComponentBattlefieldBoundsForCurrentPhysicsFrame(out var left, out var right))
		{
			return true;
		}
		float x = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame).X;
		if (x >= left)
		{
			return x <= right;
		}
		return false;
	}

	protected internal Vector2 GetGlobalPositionForPhysicsFrame(ulong physicsFrame)
	{
		if (physicsFrame != 18446744073709551615uL && _physicsGlobalPositionFrame == physicsFrame)
		{
			return _physicsGlobalPosition;
		}
		if (_physicsGlobalPositionFrame != physicsFrame)
		{
			_physicsGlobalPosition = (_shadowGlobalTransformValid ? _shadowGlobalTransform.Origin : base.GlobalPosition);
			_physicsGlobalPositionFrame = physicsFrame;
			if (_shadowGlobalTransformValid)
			{
				_shadowGlobalTransform.Origin = _physicsGlobalPosition;
			}
		}
		return _physicsGlobalPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected internal void SetGlobalPositionForPhysicsFrame(Vector2 value, ulong physicsFrame)
	{
		SetGlobalPositionForPhysicsFrameNode(value, physicsFrame);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void SetGlobalPositionForPhysicsFrameNode(Vector2 value, ulong physicsFrame)
	{
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(physicsFrame);
		_writingCachedGlobalPosition = true;
		SetNotifyLocalTransform(enable: false);
		try
		{
			base.GlobalPosition = value;
		}
		finally
		{
			SetNotifyLocalTransform(enable: true);
			_writingCachedGlobalPosition = false;
		}
		bool flag = globalPositionForPhysicsFrame != value;
		_physicsLocalPositionValid = false;
		_physicsGlobalPosition = value;
		_physicsGlobalPositionFrame = physicsFrame;
		if (_shadowGlobalTransformValid)
		{
			_shadowGlobalTransform.Origin = value;
			if (flag)
			{
				_pendingCachedGlobalTransform = _shadowGlobalTransform;
				_pendingCachedGlobalTransformNotification = true;
			}
		}
		if (flag)
		{
			InvalidateHitBoxBounds();
			PublishCharacterTranslationForRender(value - globalPositionForPhysicsFrame, physicsFrame);
		}
		InvalidateComponentGameplayForPhysicsFrame();
	}

	internal void TranslateForPhysicsFrame(Vector2 localDelta, Vector2 globalDelta, ulong physicsFrame)
	{
		Vector2 vector = (_physicsLocalPositionValid ? _physicsLocalPosition : base.Position) + localDelta;
		_writingCachedGlobalPosition = true;
		SetNotifyLocalTransform(enable: false);
		try
		{
			base.Position = vector;
		}
		finally
		{
			SetNotifyLocalTransform(enable: true);
			_writingCachedGlobalPosition = false;
		}
		_physicsLocalPosition = vector;
		_physicsLocalPositionValid = true;
		if (_shadowGlobalTransformValid)
		{
			_shadowGlobalTransform.Origin += globalDelta;
			if (localDelta != Vector2.Zero)
			{
				_pendingCachedGlobalTransform = _shadowGlobalTransform;
				_pendingCachedGlobalTransformNotification = true;
			}
		}
		if (_physicsGlobalPositionFrame == physicsFrame)
		{
			_physicsGlobalPosition += globalDelta;
		}
		else if (_shadowGlobalTransformValid)
		{
			_physicsGlobalPosition = _shadowGlobalTransform.Origin;
			_physicsGlobalPositionFrame = physicsFrame;
		}
		if (localDelta != Vector2.Zero)
		{
			InvalidateHitBoxBounds();
			PublishCharacterTranslationForRender(globalDelta, physicsFrame);
		}
		InvalidateComponentGameplayForPhysicsFrame();
	}

	private void PublishCharacterTranslationForRender(Vector2 globalDelta, ulong physicsFrame)
	{
		if (!(globalDelta == Vector2.Zero))
		{
			AdobeAnimateSprite adobeAnimateSprite = _sprite;
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.NotifyAncestorTranslatedForRender(globalDelta, physicsFrame);
			}
		}
	}

	internal Transform2D GetGlobalTransformForShadow(ulong physicsFrame)
	{
		if (physicsFrame != 18446744073709551615uL && _shadowGlobalTransformValid && _physicsGlobalPositionFrame == physicsFrame)
		{
			_shadowGlobalTransform.Origin = _physicsGlobalPosition;
			return _shadowGlobalTransform;
		}
		if (!_shadowGlobalTransformValid)
		{
			_shadowGlobalTransform = base.GlobalTransform;
			_shadowGlobalTransformValid = true;
			_physicsGlobalPosition = _shadowGlobalTransform.Origin;
			_physicsGlobalPositionFrame = physicsFrame;
		}
		else if (_physicsGlobalPositionFrame == physicsFrame)
		{
			_shadowGlobalTransform.Origin = _physicsGlobalPosition;
		}
		return _shadowGlobalTransform;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Transform2D GetGlobalTransformForPhysicsFrame(ulong physicsFrame)
	{
		return GetGlobalTransformForShadow(physicsFrame);
	}

	public Vector2 GetLogicalGlobalPosition()
	{
		return base.GlobalPosition;
	}

	public Vector2 GetLogicalGlobalPosition(Node2D descendant)
	{
		if (!GodotObject.IsInstanceValid(descendant))
		{
			return GetLogicalGlobalPosition();
		}
		return descendant.GlobalPosition;
	}

	public Vector2 GetLogicalGlobalPosition(TowerDefenseShadowVisual visual)
	{
		if (!GodotObject.IsInstanceValid(visual))
		{
			return GetLogicalGlobalPosition();
		}
		return visual.GlobalPosition;
	}

	public Transform2D GetLogicalGlobalTransform(Node2D descendant)
	{
		if (!GodotObject.IsInstanceValid(descendant))
		{
			return GetGlobalTransformForShadow(Engine.GetPhysicsFrames());
		}
		return descendant.GlobalTransform;
	}

	public void SetLogicalGlobalPosition(Vector2 value)
	{
		ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (num == 18446744073709551615uL)
		{
			num = Engine.GetPhysicsFrames();
		}
		SetGlobalPositionForPhysicsFrame(value, num);
	}

	internal void SetCellMoveLogicalGlobalPosition(Vector2 value)
	{
		SetLogicalGlobalPosition(value);
		RefreshCellMoveRenderOrder(value);
	}

	internal void RefreshCellMoveRenderOrder(Vector2 value)
	{
		FreshZIndexForGlobalPosition(value, "cell-move-visual-position");
	}

	internal void DisableGameplayForPermanentEmbeddedVisual()
	{
		inGame = false;
	}

	private void InvalidateGlobalPositionForPhysicsFrame()
	{
		_physicsGlobalPositionFrame = 18446744073709551615uL;
	}

	private void InvalidateComponentGameplayForPhysicsFrame()
	{
		_componentGameplayPhysicsSnapshotFrame = 18446744073709551615uL;
	}

	protected bool IsStateActivationCurrent(StateHandle stateHandle, ulong activationGeneration)
	{
		if (GodotObject.IsInstanceValid(this) && IsInsideTree() && inGame && !die && !nearDie && !isDestroy && stateHandle != null && stateHandle.IsActive)
		{
			return stateHandle.ActivationGeneration == activationGeneration;
		}
		return false;
	}

	protected async Task<bool> WaitForStateDelayAsync(StateHandle stateHandle, double seconds)
	{
		if (stateHandle == null || !stateHandle.IsActive || !GodotObject.IsInstanceValid(GetTree()))
		{
			return false;
		}
		ulong activationGeneration = stateHandle.ActivationGeneration;
		await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		return IsStateActivationCurrent(stateHandle, activationGeneration);
	}

	protected async Task<bool> WaitForStatePhysicsFramesAsync(StateHandle stateHandle, int frameCount)
	{
		if (stateHandle == null || !stateHandle.IsActive || !GodotObject.IsInstanceValid(GetTree()))
		{
			return false;
		}
		ulong activationGeneration = stateHandle.ActivationGeneration;
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (!IsStateActivationCurrent(stateHandle, activationGeneration))
			{
				return false;
			}
		}
		return true;
	}

	private bool GetComponentGameplayForCurrentPhysicsFrame()
	{
		if (!inGame || _componentGameplayUntilBattlefieldEntry)
		{
			return true;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (currentPhysicsFrame == 18446744073709551615uL)
		{
			return IsInsideComponentBattlefield;
		}
		if (_componentGameplayPhysicsSnapshotFrame == currentPhysicsFrame)
		{
			return _componentGameplayPhysicsSnapshot;
		}
		CacheComponentGameplayForPhysicsFrame(currentPhysicsFrame);
		return _componentGameplayPhysicsSnapshot;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RefreshComponentGameplayEntryExceptionForPhysicsFrame()
	{
		if (_componentGameplayUntilBattlefieldEntry && IsWithinComponentBattlefieldBoundsForPhysicsFrame())
		{
			_componentGameplayUntilBattlefieldEntry = false;
		}
	}

	private bool ResolveComponentGameplayForPhysicsFrame()
	{
		if (inGame && !_componentGameplayUntilBattlefieldEntry)
		{
			return IsWithinComponentBattlefieldBoundsForPhysicsFrame();
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CacheComponentGameplayForPhysicsFrame(ulong physicsFrame)
	{
		_componentGameplayPhysicsSnapshot = ResolveComponentGameplayForPhysicsFrame();
		_componentGameplayPhysicsSnapshotFrame = physicsFrame;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		if (config != null)
		{
			if (config.damagePointData != null)
			{
				array.Add(new Dictionary
				{
					["name"] = "PreviewDamagePointPersontage",
					["type"] = 3,
					["hint"] = 1,
					["hint_string"] = "0.0,1.0,0.01",
					["usage"] = num
				});
			}
			if (config.armorData != null)
			{
				array.Add(new Dictionary
				{
					["name"] = "Armor",
					["type"] = 28,
					["hint"] = 2,
					["hint_string"] = $"{4}/{2}:{string.Join(",", config.armorData.armorDictionary.Keys)}",
					["usage"] = num
				});
			}
			if (config.customData != null)
			{
				array.Add(new Dictionary
				{
					["name"] = "Custom",
					["type"] = 28,
					["hint"] = 2,
					["hint_string"] = $"{4}/{2}:{string.Join(",", config.customData.customDictionary.Keys)}",
					["usage"] = num
				});
			}
		}
		List<string> list = _damagePartList;
		if (list != null && list.Count > 0)
		{
			foreach (string damagePart in _damagePartList)
			{
				array.Add(new Dictionary
				{
					["name"] = "DamagePartSlot/" + damagePart,
					["type"] = 22,
					["hint"] = 26,
					["hint_string"] = "AdobeAnimateSlot",
					["usage"] = num
				});
			}
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property == PropPreviewDamagePoint)
		{
			previewDamagePointPersontage = value.AsDouble();
			return true;
		}
		if (property == PropArmor)
		{
			Array<string> array = new Array<string>();
			foreach (Variant item in value.AsGodotArray())
			{
				array.Add(item.AsString());
			}
			currentArmor = array;
			return true;
		}
		if (property == PropCustom)
		{
			Array<string> array2 = new Array<string>();
			foreach (Variant item2 in value.AsGodotArray())
			{
				array2.Add(item2.AsString());
			}
			currentCustom = array2;
			return true;
		}
		string text = property.ToString();
		if (text.StartsWith("DamagePartSlot/"))
		{
			string text2 = text.Substring("DamagePartSlot/".Length);
			damagePartSlot[text2] = value;
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property == PropPreviewDamagePoint)
		{
			return Variant.From<double>(previewDamagePointPersontage);
		}
		if (property == PropArmor)
		{
			return Variant.From<Array<string>>(currentArmor);
		}
		if (property == PropCustom)
		{
			return Variant.From<Array<string>>(currentCustom);
		}
		string text = property.ToString();
		if (text.StartsWith("DamagePartSlot/"))
		{
			string text2 = text.Substring("DamagePartSlot/".Length);
			Dictionary dictionary = _damagePartSlot;
			if (dictionary != null && dictionary.ContainsKey(text2))
			{
				return _damagePartSlot[text2];
			}
			return default(Variant);
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (property == PropPreviewDamagePoint || property == PropArmor || property == PropCustom)
		{
			return true;
		}
		if (property.ToString().StartsWith("DamagePartSlot/"))
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (property == PropPreviewDamagePoint)
		{
			return Variant.From<double>(1.0);
		}
		if (property == PropCustom)
		{
			return Variant.From<Array<string>>(new Array<string>());
		}
		if (property == PropArmor)
		{
			return Variant.From<Array<string>>(new Array<string>());
		}
		property.ToString().StartsWith("DamagePartSlot/");
		return default(Variant);
	}

	private void EnsureComponentManagerResource(bool migrateLegacyHost)
	{
		LegacyComponentManagerNode nodeOrNull = GetNodeOrNull<LegacyComponentManagerNode>("%ComponentManager");
		if (!GodotObject.IsInstanceValid(ComponentSet) && GodotObject.IsInstanceValid(nodeOrNull))
		{
			ComponentSet = nodeOrNull.ComponentSet;
		}
		if (!GodotObject.IsInstanceValid(componentManager))
		{
			componentManager = new ComponentManager();
		}
		if (GodotObject.IsInstanceValid(ComponentSet) || !GodotObject.IsInstanceValid(componentManager.ComponentSet))
		{
			componentManager.ComponentSet = ComponentSet;
		}
		else
		{
			ComponentSet = componentManager.ComponentSet;
		}
		componentManager.AttachOwner(this);
		if (migrateLegacyHost && GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.MigrateComponentsTo(this);
			componentManager.AttachOwner(this);
			nodeOrNull.QueueFree();
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		if (!CachedEditorHint)
		{
			EnsureComponentManagerResource(migrateLegacyHost: false);
		}
		RefreshLocalScaleXSnapshot();
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: true);
		int num;
		if (!CachedEditorHint && IsNodeReady())
		{
			num = (inGame ? 1 : 0);
			if (num != 0)
			{
				InitializeCharacterExternalVisuals();
				if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.characterRegistry))
				{
					TowerDefenseManager.Instance.CharacterRegister(this);
				}
				AddToGroup("Character", persistent: true);
			}
		}
		else
		{
			num = 0;
		}
		if (!CachedEditorHint && _mainStateMachineDispatchEnabled)
		{
			ResumeOwnerBatchDispatch();
		}
		if (num != 0)
		{
			SubscribeCharacterSkinSwitched();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		DamagePartInit();
		if (config != null && config.damagePointData != null && sprite != null && previewDamagePointPersontage < 1.0)
		{
			PreviewDamagePoint(previewDamagePointPersontage);
		}
		if (config != null && config.customData != null && sprite != null)
		{
			SetCustoms(currentCustom);
		}
		if (CachedEditorHint)
		{
			return;
		}
		EnsureCharacterSkinSwitchedHandler();
		if (!GodotObject.IsInstanceValid(config) || !GodotObject.IsInstanceValid(sprite))
		{
			DisableInvalidRuntimeCharacter();
			return;
		}
		HasValidRuntimeConfiguration = true;
		EnsureHitBoxRuntimeInitialized();
		randFreshIndex = (int)GD.Randi();
		if (TowerDefenseManager.Instance != null)
		{
			groundRight = TowerDefenseManager.Instance.GetMapGroundRight();
		}
		backEffectNode = GetNode<Node2D>("%BackEffectNode");
		frontEffectNode = GetNode<Node2D>("%FrontEffectNode");
		spriteGroup = GetNode<Node2D>("%SpriteGroup");
		transformPoint = GetNode<Marker2D>("SpriteGroup/TransformPoint");
		Sprite2D nodeOrNull = GetNodeOrNull<Sprite2D>("%ShadowSprite");
		bool flag = Global.Instance != null && Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage";
		shadowSprite = TowerDefenseShadowVisual.Capture(this, nodeOrNull, !flag);
		if (GodotObject.IsInstanceValid(shadowSprite) && shadowSprite.RequiresLegacyRendering)
		{
			GD.PushWarning("[ShadowData:E_LEGACY_NODE] '" + base.SceneFilePath + "' keeps ShadowSprite because it requires native compatibility rendering.");
		}
		InitializeCharacterExternalVisuals();
		if (MainStateMachineDefinition == null)
		{
			GD.PushError($"Character '{base.Name}' is missing its main StateMachine definition.");
			SetMainStateMachineDispatchEnabled(enabled: false);
			return;
		}
		StateMachineController stateMachineController = (StateMachineController)(StateMachine = (_mainStateMachineController = new StateMachineController()));
		if (!stateMachineController.Initialize(MainStateMachineDefinition, this))
		{
			GD.PushError("Character state machine initialization failed: " + stateMachineController.InitializationError);
			SetMainStateMachineDispatchEnabled(enabled: false);
			return;
		}
		BindMainStateHandles();
		Callable.From(EnterInitialMainStateMachine).CallDeferred();
		EnsureComponentManagerResource(migrateLegacyHost: true);
		if (GodotObject.IsInstanceValid(componentManager))
		{
			componentManager.InitializeResourceComponents();
			if (PhonkComponent.phonkEnabled && phonkComponent == null)
			{
				PhonkComponent.InjectToCharacter(this);
			}
			if (DismemberComponent.dismemberEnabled && dismemberComponent == null && this is TowerDefenseZombie)
			{
				DismemberComponent.InjectToCharacter(this);
			}
		}
		SetMainStateMachineDispatchEnabled(ShouldEnableGameplayDispatch());
		instance = new TowerDefenseCharacterInstance();
		instance._Init(this, config);
		(bool, bool)? pendingCoverSleepState = _pendingCoverSleepState;
		if (pendingCoverSleepState.HasValue)
		{
			(bool, bool) valueOrDefault = pendingCoverSleepState.GetValueOrDefault();
			instance.sleep = valueOrDefault.Item1;
			instance.wakeUp = valueOrDefault.Item2;
			_pendingCoverSleepState = null;
		}
		instance.damagePointReach += DamagePointReach;
		instance.hitpointsNearDie += HitpointsNearDie;
		instance.hitpointsEmpty += HitpointsEmpty;
		instance.armorDamagePointReach += ArmorDamagePointReach;
		instance.armorHitpointsEmpty += ArmorHitpointsEmpty;
		sprite.OnAnimeCompleted += AnimeCompleted;
		sprite.OnAnimeEvent += AnimeEvent;
		sprite.OnAnimeStarted += OnAnimeStarted;
		componentManager.ActivateResourceComponents();
		ApplySpriteGroupZPosition();
		shadowComponent?.UpdateShadow();
		AddToGroup("Character", persistent: true);
		if (inGame && TowerDefenseManager.Instance != null)
		{
			TowerDefenseManager.Instance.CharacterRegister(this);
		}
		if (config != null)
		{
			AddToGroup(config.name, persistent: true);
		}
		RefreshPacketCustomFromSave();
		if (inGame && CanSleep())
		{
			Sleep();
		}
		isUnlimitedFire = inGame && !editorPreviewMode && TowerDefenseManager.IsUnlimitedFire();
		if (isUnlimitedFire)
		{
			UnlimitedFireInit();
		}
		if (inGame)
		{
			SubscribeCharacterSkinSwitched();
		}
		BindMainStateHandles();
		ConfigureCharacterBatchProcessing();
		if (inGame)
		{
			TowerDefenseManager.ApplyMapCharacterRules(this);
		}
	}

	private void RefreshPacketCustomFromSave()
	{
		if (!GodotObject.IsInstanceValid(config?.customData) || !GodotObject.IsInstanceValid(packet))
		{
			return;
		}
		Dictionary packetState = XWModPlayerProgressService.GetPacketState(packet.saveKey);
		Dictionary dictionary = (packetState.ContainsKey("Key") ? ((Dictionary)packetState["Key"]) : new Dictionary());
		if (!dictionary.ContainsKey("Custom"))
		{
			dictionary["Custom"] = "";
		}
		packetState["Key"] = dictionary;
		if ((string)dictionary["Custom"] != "")
		{
			string text = (string)dictionary["Custom"];
			if (config.customData.customDictionary.ContainsKey(text))
			{
				currentCustom = new Array<string> { text };
			}
			else
			{
				dictionary["Custom"] = "";
				packetState["Key"] = dictionary;
				if (XWModContentCatalog.GetContentOwner("Packet", packet.saveKey).Length > 0)
				{
					XWModPlayerProgressService.SetPacketState(packet.saveKey, packetState);
				}
				else
				{
					GameSaveManager.Instance.SetTowerDefensePacketValue(packet.saveKey, packetState);
				}
				currentCustom = new Array<string>();
			}
		}
		else
		{
			currentCustom = new Array<string>();
		}
		SetCustoms(currentCustom);
	}

	private void BindMainStateHandles()
	{
		if (_characterStateHandlesConnected)
		{
			return;
		}
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_idleStateHandle = StateMachine?.GetStateById("character.idle");
		_sleepStateHandle = StateMachine?.GetStateById("character.sleep");
		_componentStateHandle = StateMachine?.GetStateById("character.component");
		if (_mainStatePhysicsFastCallbackTarget == null)
		{
			_mainStatePhysicsFastCallbackTarget = new MainStatePhysicsFastCallbackTarget(this);
		}
		if (_idleStateHandle != null)
		{
			_idleStateHandle.Entered += IdleEntered;
			_idleStateHandle.Exited += IdleExited;
			if (!_idleStateHandle.TrySetPhysicsFastCallback(_mainStatePhysicsFastCallbackTarget, 1))
			{
				_idleStateHandle.PhysicsProcessing += IdleProcessing;
			}
		}
		if (_sleepStateHandle != null)
		{
			_sleepStateHandle.Entered += SleepEntered;
			_sleepStateHandle.Exited += SleepExited;
			if (!_sleepStateHandle.TrySetPhysicsFastCallback(_mainStatePhysicsFastCallbackTarget, 2))
			{
				_sleepStateHandle.PhysicsProcessing += SleepProcessing;
			}
		}
		if (_componentStateHandle != null)
		{
			_componentStateHandle.Entered += ComponentEntered;
			_componentStateHandle.Exited += ComponentExited;
		}
		_characterStateHandlesConnected = true;
	}

	private void UnbindMainStateHandles()
	{
		if (!_characterStateHandlesConnected)
		{
			return;
		}
		if (_idleStateHandle != null)
		{
			_idleStateHandle.Entered -= IdleEntered;
			_idleStateHandle.Exited -= IdleExited;
			if (!_idleStateHandle.ClearPhysicsFastCallback(_mainStatePhysicsFastCallbackTarget))
			{
				_idleStateHandle.PhysicsProcessing -= IdleProcessing;
			}
		}
		if (_sleepStateHandle != null)
		{
			_sleepStateHandle.Entered -= SleepEntered;
			_sleepStateHandle.Exited -= SleepExited;
			if (!_sleepStateHandle.ClearPhysicsFastCallback(_mainStatePhysicsFastCallbackTarget))
			{
				_sleepStateHandle.PhysicsProcessing -= SleepProcessing;
			}
		}
		if (_componentStateHandle != null)
		{
			_componentStateHandle.Entered -= ComponentEntered;
			_componentStateHandle.Exited -= ComponentExited;
		}
		_idleStateHandle = null;
		_sleepStateHandle = null;
		_componentStateHandle = null;
		_characterStateHandlesConnected = false;
	}

	private void DisableInvalidRuntimeCharacter()
	{
		HasValidRuntimeConfiguration = false;
		inGame = false;
		SetPhysicsProcess(enable: false);
		SetProcess(enable: false);
		base.ProcessMode = ProcessModeEnum.Disabled;
		GD.PushError($"[TowerDefenseCharacter] Disabled invalid runtime character '{base.Name}': config or sprite is null/invalid.");
	}

	public override void _ExitTree()
	{
		SetNotifyTransform(enable: false);
		SetNotifyLocalTransform(enable: false);
		CancelBowlingImpactDisplacement();
		SuspendOwnerBatchDispatch();
		DisposeCharacterExternalVisuals();
		componentManager?.DetachOwner();
		base._ExitTree();
		if (!CachedEditorHint)
		{
			if (inGame && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.characterRegistry))
			{
				TowerDefenseManager.Instance.CharacterUnregister(this);
			}
			RemoveFromGroup("Character");
			UnsubscribeCharacterSkinSwitched();
		}
	}

	private void EnsureCharacterSkinSwitchedHandler()
	{
		if (_characterSkinSwitchedHandler == null)
		{
			_characterSkinSwitchedHandler = OnCharacterSkinSwitched;
		}
	}

	private void SubscribeCharacterSkinSwitched()
	{
		if (!_characterSkinSwitchedSubscribed || !GodotObject.IsInstanceValid(_characterSkinSwitchedEventBus))
		{
			_characterSkinSwitchedSubscribed = false;
			_characterSkinSwitchedEventBus = null;
			BattleEventBus battleEventBus = BattleEventBus.Instance;
			if (GodotObject.IsInstanceValid(battleEventBus))
			{
				EnsureCharacterSkinSwitchedHandler();
				battleEventBus.OnCharacterSkinSwitched += _characterSkinSwitchedHandler;
				_characterSkinSwitchedEventBus = battleEventBus;
				_characterSkinSwitchedSubscribed = true;
			}
		}
	}

	private void UnsubscribeCharacterSkinSwitched()
	{
		if (_characterSkinSwitchedSubscribed && GodotObject.IsInstanceValid(_characterSkinSwitchedEventBus) && _characterSkinSwitchedHandler != null)
		{
			_characterSkinSwitchedEventBus.OnCharacterSkinSwitched -= _characterSkinSwitchedHandler;
		}
		_characterSkinSwitchedSubscribed = false;
		_characterSkinSwitchedEventBus = null;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 28 || (long)what == 29 || (long)what == 14 || (long)what == 15 || (long)what == 18 || (long)what == 19)
		{
			PrepareBatchRegistration();
		}
		if ((long)what == 20)
		{
			AdobeAnimateSprite.SealPackedSceneStaticArraysInSubtree(this);
		}
		if ((long)what == 18 || (long)what == 19)
		{
			AdobeAnimateSprite adobeAnimateSprite = _sprite;
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.InvalidateRuntimeRenderMountForAncestorChange();
			}
		}
		bool flag = (long)what == 35;
		bool num = (long)what == 2000;
		bool flag2 = false;
		if (num && !_writingCachedGlobalPosition && _pendingCachedGlobalTransformNotification)
		{
			_pendingCachedGlobalTransformNotification = false;
			flag2 = base.GlobalTransform.IsEqualApprox(_pendingCachedGlobalTransform);
		}
		if ((num || flag) && !flag2)
		{
			InvalidateHitBoxBounds();
			if (!_writingCachedGlobalPosition)
			{
				_shadowGlobalTransformValid = false;
				_physicsLocalPositionValid = false;
				InvalidateGlobalPositionForPhysicsFrame();
				InvalidateComponentGameplayForPhysicsFrame();
				if (flag)
				{
					RefreshLocalScaleXSnapshot();
					NotifyRenderAncestorScaleChangedIfNeeded();
				}
			}
		}
		if ((long)what == 1)
		{
			componentManager?.Release();
			componentManager = null;
			ReleaseMainStateMachine();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		componentManager?.DispatchRuntimeInput(inputEvent);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		componentManager?.DispatchRuntimeUnhandledInput(inputEvent);
	}

	private void ReleaseMainStateMachine()
	{
		if (!_mainStateMachineReleased)
		{
			_mainStateMachineReleased = true;
			_mainStateMachineDispatchEnabled = false;
			UnbindMainStateHandles();
			StateMachineController mainStateMachineController = _mainStateMachineController;
			_mainStateMachineController = null;
			StateMachine = null;
			mainStateMachineController?.Dispose();
		}
	}

	public override void _Process(double delta)
	{
		if (!_ownerBatchRegistered)
		{
			BatchProcessUpdate(delta);
		}
	}

	public void BatchProcessUpdate(double delta)
	{
		ulong processFrames = Engine.GetProcessFrames();
		TickMainStateMachineProcess(delta, processFrames);
		TickComponentStateMachineProcess(delta, processFrames);
	}

	private void TickMainStateMachineProcess(double delta, ulong processFrame)
	{
		StateMachineController mainStateMachineController = _mainStateMachineController;
		if (CanDispatchMainStateMachine && mainStateMachineController != null && mainStateMachineController.HasProcessWork && _lastMainStateMachineProcessFrame != processFrame)
		{
			_lastMainStateMachineProcessFrame = processFrame;
			mainStateMachineController.TickProcess(delta);
		}
	}

	private void TickComponentStateMachineProcess(double delta, ulong processFrame)
	{
		ComponentManager componentManager = this.componentManager;
		if (CanDispatchOwnedStateMachines && componentManager != null && componentManager.HasStateMachineProcessWork && _lastComponentStateMachineProcessFrame != processFrame)
		{
			_lastComponentStateMachineProcessFrame = processFrame;
			componentManager.TickStateMachineProcess(delta);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_ownerBatchRegistered)
		{
			BatchUpdate(delta);
		}
	}

	public virtual void BatchUpdate(double delta)
	{
		if (!CachedEditorHint)
		{
			if (!TowerDefenseProcessModeDispatch.IsCharacterPhysicsBatchDispatchActive)
			{
				TowerDefenseProcessModeDispatch.BeginFrame();
			}
			PhysicsProcessWithFrame(delta, TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		}
	}

	protected void PhysicsProcessWithFrame(double delta, ulong physicsFrame)
	{
		if (inGame && _lastMainStateMachinePhysicsFrame != physicsFrame)
		{
			PrepareCharacterPhysicsForTrace(physicsFrame, out var mainStateMachine, out var canDispatchMainStateMachine, out var manager, out var canDispatchOwnedStateMachines);
			PrepareCharacterTimeScaleForPhysics(delta);
			_componentGameplayPhysicsDispatchActive = true;
			try
			{
				TickMainStateMachinePhysics(delta, mainStateMachine, canDispatchMainStateMachine);
				RunComponentPhysicsForTrace(delta, physicsFrame, manager, canDispatchOwnedStateMachines);
			}
			finally
			{
				_componentGameplayPhysicsDispatchActive = false;
			}
			RunCharacterPhysicsPostForTrace(delta, physicsFrame);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PrepareCharacterPhysicsForTrace(ulong physicsFrame, out StateMachineController mainStateMachine, out bool canDispatchMainStateMachine, out ComponentManager manager, out bool canDispatchOwnedStateMachines)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		_lastMainStateMachinePhysicsFrame = physicsFrame;
		RefreshComponentGameplayEntryExceptionForPhysicsFrame();
		mainStateMachine = _mainStateMachineController;
		int num;
		if (_mainStateMachineDispatchEnabled)
		{
			StateMachineController obj = mainStateMachine;
			if (obj != null && obj.IsInitialized && inGame)
			{
				num = ((!isDestroy) ? 1 : 0);
				goto IL_0048;
			}
		}
		num = 0;
		goto IL_0048;
		IL_0048:
		canDispatchMainStateMachine = (byte)num != 0;
		manager = componentManager;
		canDispatchOwnedStateMachines = CanDispatchOwnedStateMachines && manager != null;
		CacheComponentGameplayForPhysicsFrame(physicsFrame);
		TowerDefensePerfProfiler.End("batch.character.prepare", startTicks);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RunComponentPhysicsForTrace(double delta, ulong physicsFrame, ComponentManager manager, bool canDispatchOwnedStateMachines)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		if (canDispatchOwnedStateMachines)
		{
			bool hasStateMachinePhysicsWork = manager.HasStateMachinePhysicsWork;
			bool hasRuntimePhysicsWork = manager.HasRuntimePhysicsWork;
			bool hasComponentTimerWork = manager.HasComponentTimerWork;
			if (hasStateMachinePhysicsWork || hasRuntimePhysicsWork || hasComponentTimerWork)
			{
				if (_componentGameplayPhysicsSnapshotFrame != physicsFrame)
				{
					CacheComponentGameplayForPhysicsFrame(physicsFrame);
				}
				if (hasStateMachinePhysicsWork)
				{
					manager.TickStateMachinePhysics(delta, _componentGameplayPhysicsSnapshot);
					if (_componentGameplayPhysicsSnapshotFrame != physicsFrame)
					{
						CacheComponentGameplayForPhysicsFrame(physicsFrame);
					}
				}
				if (hasRuntimePhysicsWork)
				{
					manager.TickRuntimePhysics(delta, physicsFrame, _componentGameplayPhysicsSnapshot);
				}
				if (hasComponentTimerWork)
				{
					manager.TickComponentTimers(delta, _componentGameplayPhysicsSnapshot);
				}
			}
		}
		TowerDefensePerfProfiler.End("batch.character.componentWork", startTicks);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RunCharacterPhysicsPostForTrace(double delta, ulong physicsFrame)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		InvalidateGlobalPositionForPhysicsFrame();
		base._PhysicsProcess(delta);
		ReleaseZMotionLocalRenderWhenStable();
		BlowBackComponent obj = blowBackComponent;
		if (obj != null && !obj.IsReleased && blowBackComponent.blowBack)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (logicalGlobalPosition.X <= (float)groundRight)
			{
				logicalGlobalPosition.X += (float)(blowBackComponent.blowBackNum * delta);
				SetGlobalPositionForPhysicsFrame(logicalGlobalPosition, physicsFrame);
			}
		}
		if ((ulong)((long)physicsFrame + (long)randFreshIndex) % 5uL == 0L && ShouldUpdateGridPos() && inGame)
		{
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
		}
		if (nearDie && !die && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost || !(this is TowerDefenseZombie)))
		{
			instance.DealHurt(config.hitpointsNearDeath * delta / 3.0, playSplatAudio: false);
		}
		TowerDefensePerfProfiler.End("batch.character.post", startTicks);
	}

	private void PrepareCharacterTimeScaleForPhysics(double delta)
	{
		timeScale = timeScaleInit;
		if (inGame && this is TowerDefenseZombie && (!GodotObject.IsInstanceValid(instance) || instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS))
		{
			double mapZombieColumnSpeedMultiplier = TowerDefenseManager.GetMapZombieColumnSpeedMultiplier(base.gridPos.X);
			if (mapZombieColumnSpeedMultiplier != 1.0)
			{
				timeScale *= mapZombieColumnSpeedMultiplier;
			}
		}
		BuffComponent buffComponent = buff;
		if (buffComponent != null && buffComponent.HasFrameUpdateWork)
		{
			buffComponent.BuffUpdate(delta);
		}
		else if (sprite.meshColor != Colors.White)
		{
			sprite.meshColor = Colors.White;
		}
		SynchronizeAnimationPlaybackBlock();
	}

	private void SynchronizeAnimationPlaybackBlock()
	{
		AdobeAnimateSprite adobeAnimateSprite = sprite;
		if (GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			adobeAnimateSprite.SetPlaybackBlocked(Math.Abs(timeScale) <= 1E-06);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void TickMainStateMachinePhysics(double delta, StateMachineController controller, bool dispatchEnabled)
	{
		if (!dispatchEnabled || controller == null || !controller.HasPhysicsWork)
		{
			return;
		}
		if (TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			long startTicks = TowerDefensePerfProfiler.BeginHotPath();
			if (!TryTickFlatMainStatePhysics(delta, controller))
			{
				controller.TickPhysicsDetailed(delta);
			}
			TowerDefensePerfProfiler.End("batch.character.mainState", startTicks);
		}
		else if (!TryTickFlatMainStatePhysics(delta, controller))
		{
			controller.TickPhysics(delta);
		}
	}

	private bool TryTickFlatMainStatePhysics(double delta, StateMachineController controller)
	{
		if (!controller.SupportsFlatDirectStateTransitions || !TryResolveMainRuntimeState(controller.CurrentStateHandle, out var state))
		{
			return false;
		}
		for (int i = 0; i < 3; i++)
		{
			int num = (int)state;
			switch (state)
			{
			case MainRuntimeState.Idle:
				IdleProcessing(delta);
				break;
			case MainRuntimeState.Sleep:
				SleepProcessing(delta);
				break;
			default:
				return true;
			}
			if (!TryResolveMainRuntimeState(controller.CurrentStateHandle, out var state2) || (int)state2 <= num)
			{
				return true;
			}
			state = state2;
		}
		return true;
	}

	private bool TryResolveMainRuntimeState(StateHandle activeState, out MainRuntimeState state)
	{
		if (activeState == _idleStateHandle)
		{
			state = MainRuntimeState.Idle;
			return true;
		}
		if (activeState == _sleepStateHandle)
		{
			state = MainRuntimeState.Sleep;
			return true;
		}
		if (activeState == _componentStateHandle)
		{
			state = MainRuntimeState.Component;
			return true;
		}
		state = MainRuntimeState.Idle;
		return false;
	}

	public void DamagePartInit()
	{
		List<string> list = _damagePartList ?? (_damagePartList = new List<string>());
		list.Clear();
		Dictionary dictionary = _damagePart ?? (_damagePart = new Dictionary());
		dictionary.Clear();
		if (sprite == null || config == null)
		{
			return;
		}
		if (config.damagePointData != null)
		{
			foreach (CharacterDamagePointConfig damagePoint in config.damagePointData.damagePointList)
			{
				if (damagePoint.isDrop)
				{
					string damagePointName = damagePoint.damagePointName;
					dictionary[damagePointName] = damagePoint;
					list.Add(damagePointName);
				}
			}
		}
		if (config.armorData == null)
		{
			return;
		}
		foreach (ArmorSlotConfig armor in config.armorData.armorList)
		{
			TowerDefenseArmorTypeData towerDefenseArmorTypeData = CharacterArmorData._LoadTypeDataFromJSON(armor.armorName);
			if (towerDefenseArmorTypeData == null || (towerDefenseArmorTypeData.armorMethodFlags & 0x80) != 0)
			{
				string armorName = armor.armorName;
				dictionary[armorName] = armor;
				list.Add(armorName);
			}
		}
	}

	public void PreviewDamagePoint(double persontage)
	{
		config.damagePointData.ClearDamagePointFliters(sprite);
		int num = 0;
		foreach (Variant key in config.damagePointData.damagePointDictionary.Keys)
		{
			CharacterDamagePointConfig characterDamagePointConfig = (CharacterDamagePointConfig)(GodotObject)((Dictionary)config.damagePointData.damagePointDictionary[key])["Config"];
			if (persontage <= characterDamagePointConfig.damagePersontage)
			{
				config.damagePointData.SetDamagePointFliters(sprite, characterDamagePointConfig.damagePointName);
				if (config.customData != null)
				{
					foreach (string item in currentCustom)
					{
						config.customData.SetDamagePoint(sprite, item, num);
					}
				}
			}
			num++;
		}
	}

	public void ClearArmor(string armor)
	{
		ArmorVisualComponent obj = armorVisualComponent;
		if (obj != null && !obj.IsReleased)
		{
			armorVisualComponent.ClearArmor(armor);
		}
		else
		{
			config.armorData.ClearArmorFliters(sprite, armor);
		}
	}

	public void ClearArmorAll()
	{
		ArmorVisualComponent obj = armorVisualComponent;
		if (obj != null && !obj.IsReleased)
		{
			armorVisualComponent.ClearArmorAll();
		}
		else
		{
			config.armorData.ClearArmorFlitersAll(sprite);
		}
	}

	public void SetArmor(string armor, int stage)
	{
		ArmorVisualComponent obj = armorVisualComponent;
		if (obj != null && !obj.IsReleased)
		{
			armorVisualComponent.SetArmor(armor, stage);
			return;
		}
		ClearArmor(armor);
		ArmorSlotConfig slotConfig = config.armorData.GetSlotConfig(armor);
		if (slotConfig == null || !(slotConfig.replaceMethod == "Sprite"))
		{
			config.armorData.OpenArmorFliters(sprite, armor);
			config.armorData.SetArmorReplace(sprite, armor, stage);
		}
	}

	public void SetArmors(Array<string> armorList)
	{
		ArmorVisualComponent obj = armorVisualComponent;
		if (obj != null && !obj.IsReleased)
		{
			armorVisualComponent.SetArmors(armorList);
			return;
		}
		ClearArmorAll();
		foreach (string armor in armorList)
		{
			if (armor != "")
			{
				SetArmor(armor, 0);
			}
		}
	}

	public void ClearCustom()
	{
		CustomVisualComponent obj = customVisualComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			customVisualComponent.ClearCustom();
		}
		else
		{
			config.customData.ClearCustomFliters(sprite);
		}
	}

	public void SetCustom(string custom)
	{
		CustomVisualComponent obj = customVisualComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			customVisualComponent.SetCustom(custom);
		}
		else
		{
			config.customData.SetCustomFliters(sprite, custom);
		}
	}

	public void SetCustoms(Array<string> customList)
	{
		CustomVisualComponent obj = customVisualComponent;
		if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			customVisualComponent.SetCustoms(customList);
			return;
		}
		ClearCustom();
		foreach (string custom in customList)
		{
			if (custom != "")
			{
				SetCustom(custom);
			}
		}
	}

	public void OnCharacterSkinSwitched(string packetSaveKey, string customKey)
	{
		if (GodotObject.IsInstanceValid(packet) && !(packet.saveKey != packetSaveKey))
		{
			SwitchCustom(customKey);
		}
	}

	public void SwitchCustom(string customKey)
	{
		if (!GodotObject.IsInstanceValid(config) || !GodotObject.IsInstanceValid(config.customData))
		{
			return;
		}
		if (customKey == "")
		{
			currentCustom = new Array<string>();
			OnCustomSwitched("");
		}
		else
		{
			if (!config.customData.customDictionary.ContainsKey(customKey))
			{
				return;
			}
			currentCustom = new Array<string> { customKey };
			if (GodotObject.IsInstanceValid(packet))
			{
				Dictionary packetState = XWModPlayerProgressService.GetPacketState(packet.saveKey);
				if (!packetState.ContainsKey("Key"))
				{
					packetState["Key"] = new Dictionary();
				}
				((Dictionary)packetState["Key"])["Custom"] = customKey;
				if (XWModContentCatalog.GetContentOwner("Packet", packet.saveKey).Length > 0)
				{
					XWModPlayerProgressService.SetPacketState(packet.saveKey, packetState);
				}
				else
				{
					GameSaveManager.Instance.SetTowerDefensePacketValue(packet.saveKey, packetState);
				}
			}
			OnCustomSwitched(customKey);
		}
	}

	public virtual void OnCustomSwitched(string customKey)
	{
	}

	public virtual void IdleEntered()
	{
		sprite.timeScale = timeScale;
		if (useIdleAnimeReset && idleAnimeClip != "")
		{
			sprite.SetAnimation(idleAnimeClip, loop: true, 0.2);
		}
	}

	public virtual void IdleProcessing(double delta)
	{
		sprite.timeScale = timeScale;
		if (CanSleep())
		{
			Sleep();
		}
	}

	public virtual void IdleExited()
	{
	}

	public virtual void SleepEntered()
	{
		GetSleepRuntime()?.SleepEntered();
	}

	public virtual void SleepProcessing(double delta)
	{
		sprite.timeScale = timeScale;
		GetSleepRuntime()?.SleepProcessing((float)delta);
	}

	public virtual void SleepExited()
	{
		GetSleepRuntime()?.SleepExited();
	}

	public virtual void ComponentEntered()
	{
		componentRunning = true;
	}

	public virtual void ComponentExited()
	{
		componentRunning = false;
	}

	public virtual void Idle()
	{
		SetMainRuntimeState(MainRuntimeState.Idle);
	}

	public virtual void Sleep()
	{
		if (!GodotObject.IsInstanceValid(instance) || !instance.hologram)
		{
			SetMainRuntimeState(MainRuntimeState.Sleep);
			if (!IsSleep())
			{
				GetSleepRuntime()?.SleepEnteredWithoutStateTransition();
			}
		}
	}

	public virtual void Component()
	{
		SetMainRuntimeState(MainRuntimeState.Component);
	}

	public bool IsDie()
	{
		return die;
	}

	public virtual bool CanSleep()
	{
		return GetSleepRuntime()?.CanSleep() ?? false;
	}

	public bool IsSleep()
	{
		return instance.sleep;
	}

	public bool IsIncapacitatedTarget()
	{
		if (!instance.sleep)
		{
			return buff?.BuffHas("MagicImmobilize") ?? false;
		}
		return true;
	}

	public double GetTotalHitPoint()
	{
		double num = config.hitpoints + config.hitpointsNearDeath;
		Array<TowerDefenseArmorInstance> armorList = instance.armorList;
		for (int i = 0; i < armorList.Count; i++)
		{
			num += armorList[i].damagePointBase;
		}
		return num * instance.hitpointScale;
	}

	public double GetCurrentHitPoint()
	{
		double num = instance.hitpoints;
		Array<TowerDefenseArmorInstance> armorList = instance.armorList;
		for (int i = 0; i < armorList.Count; i++)
		{
			num += armorList[i].hitPoints;
		}
		return num;
	}

	public void SetHitpointAndScale(double hitpointScale, Vector2 scale)
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.hitpointScale = hitpointScale;
		}
		if (GodotObject.IsInstanceValid(transformPoint))
		{
			transformPoint.Scale = scale;
			componentManager?.GetRuntime<GroundMoveComponent>()?.RefreshDirectionCache();
		}
	}

	public void DamagePartCreate(StringName damagePointName, Node2D node, Vector2 velocity = default(Vector2), bool keepSlotScale = true, Vector2 offset = default(Vector2), bool fromSync = false, Vector2? synchronizedPosition = null, long synchronizedSequence = 0L)
	{
		GetDamagePartRuntime()?.DamagePartCreate(damagePointName, node, velocity, keepSlotScale, offset, fromSync, synchronizedPosition, synchronizedSequence);
	}

	public TowerDefenseMagnet MagnetCreate(TowerDefenseArmorInstance armorInstance, Node2D node)
	{
		return GetDamagePartRuntime()?.MagnetCreate(armorInstance, node);
	}

	public TowerDefenseMagnet ArmorDraw(TowerDefenseArmorInstance armor)
	{
		return GetDamagePartRuntime()?.ArmorDraw(armor);
	}

	public bool HasShield()
	{
		return instance.HasShieldArmor;
	}

	public bool HasHelm()
	{
		return instance.HasHelmetArmor;
	}

	public bool ProjectileEffectsBlocked(int damageFlags, int fireMethodFlags)
	{
		if (instance != null)
		{
			return instance.ProjectileEffectsBlockedByArmor(damageFlags, fireMethodFlags);
		}
		return false;
	}

	public bool GetHasArmor(string armorName)
	{
		foreach (TowerDefenseArmorInstance item in GetArmor())
		{
			if (item.slotConfig.armorName == armorName)
			{
				return true;
			}
		}
		return false;
	}

	public TowerDefenseArmorInstance GetArmorFromName(string armorName)
	{
		foreach (TowerDefenseArmorInstance item in GetArmor())
		{
			if (item.slotConfig.armorName == armorName)
			{
				return item;
			}
		}
		return null;
	}

	public Array<TowerDefenseArmorInstance> GetArmor()
	{
		return instance.armorList;
	}

	public Array<TowerDefenseArmorInstance> GetArmorShield()
	{
		return instance.armorShield;
	}

	public Array<TowerDefenseArmorInstance> GetArmorHelment()
	{
		return instance.armorHelm;
	}

	public Array<TowerDefenseArmorInstance> GetArmorHeadCover()
	{
		return instance.armorHeadCover;
	}

	public bool CanCollision(int maskFlags)
	{
		return (maskFlags & instance.collisionFlags) != 0;
	}

	public bool CanTarget(TowerDefenseCharacter character)
	{
		return CheckDifferentCamp(character.camp);
	}

	public bool CheckDifferentCamp(TowerDefenseEnum.CHARACTER_CAMP _camp)
	{
		return camp != _camp;
	}

	public bool CheckSameLine(int line)
	{
		return line == base.gridPos.Y;
	}

	public bool IsTargetableFromLine(int line, bool includeAllLineCheck = true)
	{
		if (line == base.gridPos.Y)
		{
			return true;
		}
		TargetRegistrationComponent obj = targetRegistrationComponent;
		if (obj == null || obj.IsReleased)
		{
			return false;
		}
		if (includeAllLineCheck && targetRegistrationComponent.allLineCheck)
		{
			return true;
		}
		if (TryGetValidTargetLineAlias(out var aliasLine))
		{
			return line == aliasLine;
		}
		return false;
	}

	public bool IsTargetableFromLineRange(int minLine, int maxLine, bool includeAllLineCheck = true)
	{
		if (base.gridPos.Y >= minLine && base.gridPos.Y <= maxLine)
		{
			return true;
		}
		TargetRegistrationComponent obj = targetRegistrationComponent;
		if (obj == null || obj.IsReleased)
		{
			return false;
		}
		if (includeAllLineCheck && targetRegistrationComponent.allLineCheck)
		{
			return true;
		}
		if (TryGetValidTargetLineAlias(out var aliasLine) && aliasLine >= minLine)
		{
			return aliasLine <= maxLine;
		}
		return false;
	}

	private bool TryGetValidTargetLineAlias(out int aliasLine)
	{
		int attackGridLineAliasOffset = targetRegistrationComponent.attackGridLineAliasOffset;
		aliasLine = base.gridPos.Y + attackGridLineAliasOffset;
		if (attackGridLineAliasOffset == 0 || aliasLine < 1)
		{
			return false;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager) && towerDefenseManager.gridNum.Y > 0 && aliasLine > towerDefenseManager.gridNum.Y)
		{
			return false;
		}
		return true;
	}

	public double GetGroundHeight(double posHeight)
	{
		return (double)GetLogicalGlobalPosition().Y - posHeight + base.groundHeight;
	}

	public void SetSpriteGroupShaderParameter(string property, Variant value)
	{
		ShaderEffectComponent obj = shaderEffectComponent;
		if (obj != null && !obj.IsReleased)
		{
			shaderEffectComponent.SetSpriteGroupShaderParameter(property, value);
		}
	}

	public override void SetZ()
	{
		base.SetZ();
		shadowComponent?.MarkDirty();
		if (forceLocalRenderDuringZMotion && GodotObject.IsInstanceValid(sprite))
		{
			_zMotionStablePhysicsFrames = 0;
			if (!sprite.forceLocalRender)
			{
				sprite.forceLocalRender = true;
				_zMotionOwnsLocalRender = true;
			}
		}
		if (GodotObject.IsInstanceValid(spriteGroup))
		{
			ApplySpriteGroupZPosition();
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.NotifyAncestorTransformChangedForRender(base.ZChangedInsidePhysicsUpdate);
			}
		}
	}

	private void ApplySpriteGroupZPosition()
	{
		if (GodotObject.IsInstanceValid(spriteGroup))
		{
			if (!_spriteGroupLocalXCaptured)
			{
				_spriteGroupLocalX = spriteGroup.Position.X;
				_spriteGroupLocalXCaptured = true;
			}
			spriteGroup.Position = new Vector2(_spriteGroupLocalX, 0f - (float)base.z);
		}
	}

	private void ReleaseZMotionLocalRenderWhenStable()
	{
		if (!_zMotionOwnsLocalRender || !GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		if (!isGround || !Mathf.IsEqualApprox((float)base.z, (float)base.groundHeight))
		{
			_zMotionStablePhysicsFrames = 0;
			return;
		}
		_zMotionStablePhysicsFrames++;
		if (_zMotionStablePhysicsFrames >= 2)
		{
			sprite.forceLocalRender = false;
			_zMotionOwnsLocalRender = false;
			_zMotionStablePhysicsFrames = 0;
		}
	}

	public virtual void ShovelDestroy()
	{
		GetDestroyRuntime()?.ShovelDestroy();
	}

	public virtual void Destroy(bool freeInstance = true)
	{
		GetDestroyRuntime()?.Destroy(freeInstance);
	}

	public virtual void ClearFromMap()
	{
		Destroy();
	}

	public virtual void DestroyWithVisualDelay(double delaySeconds)
	{
		GetDestroyRuntime()?.DestroyWithVisualDelay(delaySeconds);
	}

	public virtual void AshDestroy()
	{
		GetDestroyRuntime()?.AshDestroy();
	}

	public virtual void SmashDestroy()
	{
		GetDestroyRuntime()?.SmashDestroy();
	}

	public virtual void DestroyReplace()
	{
	}

	public virtual async void DestroySet()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public virtual void HitBoxDestroy()
	{
		GetDestroyRuntime()?.HitBoxDestroy();
	}

	public virtual double HurtWithAttackConfig(AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		return GetHurtRuntime()?.HurtWithAttackConfig(attackConfig, playSplatAudio, velocity, createDamagePart) ?? 0.0;
	}

	public virtual double Hurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, double damageLimit = -1.0)
	{
		return GetHurtRuntime()?.Hurt((float)num, playSplatAudio, velocity, createDamagePart, damageLimit) ?? 0.0;
	}

	public virtual double SkipInvincibleHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		return GetHurtRuntime()?.SkipInvincibleHurt((float)num, playSplatAudio, velocity, createDamagePart) ?? 0.0;
	}

	public virtual void Health(double num)
	{
		GetHurtRuntime()?.Health((float)num);
	}

	protected void RestoreFullHealthAfterRevive()
	{
		instance.hitpoints = instance.hitpointsSave;
		instance.die = false;
		instance.nearDie = false;
		instance.collisionFlags = config.collisionFlags;
		instance.maskFlags = config.maskFlags;
		die = false;
		nearDie = false;
		destroyComponent?.EndDeathSettlement();
		instance.RefreshDamagePoint();
		targetRegistrationComponent?.NotifyTargetStateChanged();
		ShowHealthComponent obj = showHealthComponent;
		if (obj != null && !obj.IsReleased)
		{
			showHealthComponent.MarkDirty();
		}
	}

	public virtual double BowlingHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		return GetHurtRuntime()?.BowlingHurt((float)num, playSplatAudio, velocity, hitShield, createDamagePart) ?? 0.0;
	}

	public virtual double SmashHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return GetHurtRuntime()?.SmashHurt((float)num, playSplatAudio, velocity) ?? 0.0;
	}

	public virtual double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return GetHurtRuntime()?.ExplodeHurt((float)num, damageKind, playSplatAudio, velocity) ?? 0.0;
	}

	public virtual double FlagHurt(double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return GetHurtRuntime()?.FlagHurt((float)num, damageFlags, playSplatAudio, velocity) ?? 0.0;
	}

	public virtual double ProjectileHurt(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		return GetHurtRuntime()?.ProjectileHurt(projectile, projectileConfig, playSplatAudio, velocity, isRange) ?? 0.0;
	}

	public virtual double ProjectileHurt(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		return GetHurtRuntime()?.ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange) ?? 0.0;
	}

	public void Bright(double init = 0.5, double delay = 0.0, double rise = 0.5, double riseDuration = 0.0, double duration = 0.2)
	{
		HitFlashComponent obj = hitFlashComponent;
		if (obj != null && !obj.IsReleased)
		{
			hitFlashComponent.Bright((float)init, (float)delay, (float)rise, (float)riseDuration, (float)duration);
		}
	}

	public void White(double init = 1.0, double delay = 0.0, double duration = 0.5)
	{
		HitFlashComponent obj = hitFlashComponent;
		if (obj != null && !obj.IsReleased)
		{
			hitFlashComponent.White((float)init, (float)delay, (float)duration);
		}
	}

	public TowerDefenseInGamePacketShow SpawnPacket(TowerDefensePacketConfig packetConfig, Vector2 pos, double aliveTime, bool isFall, bool useCost = false, bool useRandf = true, Vector2? velocityOverride = null)
	{
		return GetResourceSpawnRuntime()?.SpawnPacket(packetConfig, pos, (float)aliveTime, isFall, useCost, useRandf, velocityOverride);
	}

	public void YBCreate(Vector2 pos, int num, Vector2 _velocity = default(Vector2), double _gravity = 980.0, bool _collect = false)
	{
		GetResourceSpawnRuntime()?.YBCreate(pos, num, _velocity, (float)_gravity, _collect);
	}

	public void CoinCreate(Vector2 pos, int num, Vector2 _velocity = default(Vector2), double _gravity = 980.0, bool _collect = false)
	{
		GetResourceSpawnRuntime()?.CoinCreate(pos, num, _velocity, (float)_gravity, _collect);
	}

	public void LuckyBagCreate(Vector2 pos, Vector2 _velocity = default(Vector2), double _gravity = 980.0)
	{
		GetResourceSpawnRuntime()?.LuckyBagCreate(pos, _velocity, (float)_gravity);
	}

	public TowerDefenseSunBase SunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 _velocity = default(Vector2), double _gravity = 980.0, double _moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.SunCreate(pos, sunNum, movingMethod, _velocity, (float)_gravity, (float)_moveStopTime);
	}

	public TowerDefenseSunBase BrainSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 _velocity = default(Vector2), double _gravity = 980.0, double _moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.BrainSunCreate(pos, sunNum, movingMethod, _velocity, (float)_gravity, (float)_moveStopTime);
	}

	public TowerDefenseSunBase ReplicatedEventSunCreate(Vector2 pos, long sunNum, bool createBrainSun, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.ReplicatedEventSunCreate(pos, sunNum, createBrainSun, movingMethod, velocity, (float)gravity, (float)moveStopTime);
	}

	public TowerDefenseSunBase JalapenoSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 _velocity = default(Vector2), double _gravity = 980.0, double _moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.JalapenoSunCreate(pos, sunNum, movingMethod, _velocity, (float)_gravity, (float)_moveStopTime);
	}

	public TowerDefenseSunBase QXSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 _velocity = default(Vector2), double _gravity = 980.0, double _moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.QXSunCreate(pos, sunNum, movingMethod, _velocity, (float)_gravity, (float)_moveStopTime);
	}

	public TowerDefenseSunBase MagicSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 _velocity = default(Vector2), double _gravity = 980.0, double _moveStopTime = -1.0)
	{
		return GetResourceSpawnRuntime()?.MagicSunCreate(pos, sunNum, movingMethod, _velocity, (float)_gravity, (float)_moveStopTime);
	}

	public void ExplodeSunCreate(Vector2 pos, long sunNum, long sunOnce, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, double _speed = 0.0, double _gravity = 0.0, double _moveStopTime = -1.0)
	{
		GetResourceSpawnRuntime()?.ExplodeSunCreate(pos, sunNum, sunOnce, movingMethod, (float)_speed, (float)_gravity, (float)_moveStopTime);
	}

	public void GoldShardCreate(Vector2 pos, Vector2 _velocity = default(Vector2), double _gravity = 980.0)
	{
		GetResourceSpawnRuntime()?.GoldShardCreate(pos, _velocity, (float)_gravity);
	}

	public void CraterCreate(bool nolimit = false, string craterName = "CraterDayGround", bool halfCrater = false)
	{
		if (this is TowerDefensePlant && nolimit)
		{
			Destroy();
		}
		if (!GodotObject.IsInstanceValid(cell) || (!nolimit && !cell.CanCraterCreate()))
		{
			return;
		}
		if (TowerDefenseManager.GetPacketConfig(craterName).Plant(base.gridPos, playAudio: false, noLimit: true) is TowerDefenseCrater towerDefenseCrater)
		{
			towerDefenseCrater.spawnHypnoses = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
			if (halfCrater)
			{
				towerDefenseCrater.halfCrater = true;
			}
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			MultiPlayerManager.Instance.SendCraterCreate(base.gridPos.X, base.gridPos.Y, craterName);
		}
	}

	public virtual void BlowBack(double num, double time = -1.0)
	{
		GetBlowBackRuntime()?.BlowBack(num, time);
	}

	public void ApplyBowlingImpactDisplacement(double distance, double duration = 0.4)
	{
		if (Math.Abs(distance) <= 1E-06)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		float num = logicalGlobalPosition.X + (float)distance;
		if (duration <= 0.0 || !IsInsideTree())
		{
			logicalGlobalPosition.X = num;
			SetGlobalPositionForPhysicsFrame(logicalGlobalPosition, Engine.GetPhysicsFrames());
			CancelBowlingImpactDisplacement();
			return;
		}
		_bowlingImpactStartGlobalX = logicalGlobalPosition.X;
		_bowlingImpactTargetGlobalX = num;
		_bowlingImpactElapsed = 0.0;
		_bowlingImpactDuration = duration;
		_bowlingImpactDisplacementActive = true;
		if (!TowerDefenseCharacterMotionBatch.Register(this))
		{
			logicalGlobalPosition.X = num;
			SetGlobalPositionForPhysicsFrame(logicalGlobalPosition, Engine.GetPhysicsFrames());
			_bowlingImpactDisplacementActive = false;
			_bowlingImpactElapsed = 0.0;
			_bowlingImpactDuration = 0.0;
		}
	}

	internal bool UpdateBowlingImpactDisplacement(double delta, ulong physicsFrame)
	{
		if (!_bowlingImpactDisplacementActive)
		{
			return false;
		}
		if (!IsInsideTree() || _bowlingImpactDuration <= 0.0)
		{
			_bowlingImpactDisplacementActive = false;
			return false;
		}
		_bowlingImpactElapsed = Math.Min(_bowlingImpactDuration, _bowlingImpactElapsed + Math.Max(0.0, delta));
		double num = _bowlingImpactElapsed / _bowlingImpactDuration;
		double num2 = 1.0 - num;
		double num3 = 1.0 - num2 * num2 * num2 * num2 * num2;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		logicalGlobalPosition.X = Mathf.Lerp(_bowlingImpactStartGlobalX, _bowlingImpactTargetGlobalX, (float)num3);
		SetGlobalPositionForPhysicsFrame(logicalGlobalPosition, physicsFrame);
		if (num < 1.0)
		{
			return true;
		}
		_bowlingImpactDisplacementActive = false;
		_bowlingImpactElapsed = 0.0;
		_bowlingImpactDuration = 0.0;
		return false;
	}

	internal void CancelBowlingImpactDisplacement()
	{
		_bowlingImpactDisplacementActive = false;
		_bowlingImpactElapsed = 0.0;
		_bowlingImpactDuration = 0.0;
		TowerDefenseCharacterMotionBatch.Unregister(this);
	}

	protected internal virtual void OnHypnosisStateChanged()
	{
	}

	internal void SyncHologramHypnoses(bool hypnoses)
	{
		if (GodotObject.IsInstanceValid(instance) && instance.hologram && instance.hypnoses != hypnoses && hypnosesComponent != null && !hypnosesComponent.IsReleased)
		{
			hypnosesComponent.HypnosesInternal(-1f, canFliter: true, null);
		}
	}

	public virtual void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		if (!IsNodeReady())
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(this))
				{
					Hypnoses(time, canFliter, hypnosesConfig);
				}
			}).CallDeferred();
		}
		else
		{
			if (hypnosesComponent == null || hypnosesComponent.IsReleased)
			{
				return;
			}
			if (this is TowerDefensePlant && GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(cell.itemShield))
			{
				TowerDefenseItemSheild itemShield = cell.itemShield;
				if (itemShield != this && itemShield.instance.hypnoses == instance.hypnoses && itemShield.ShieldBlockCharm())
				{
					return;
				}
			}
			hypnosesComponent.Hypnoses((float)time, canFliter, hypnosesConfig);
		}
	}

	public virtual void Rise(double duration = -1.0, double delay = 0.0, bool createDirt = true, bool changeState = true, double from = 150.0, bool emitRiseOver = true)
	{
		if (duration < 0.0)
		{
			duration = GD.RandRange(0.4, 0.6);
		}
		if (!IsNodeReady())
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(this))
				{
					Rise(duration, delay, createDirt, changeState, from, emitRiseOver);
				}
			}).CallDeferred();
			return;
		}
		RiseComponent riseRuntime = GetRiseRuntime();
		if (riseRuntime == null)
		{
			GD.PushError($"[TowerDefenseCharacter.Rise] Missing RiseComponent: {base.Name}");
		}
		else
		{
			riseRuntime.Rise((float)duration, (float)delay, createDirt, changeState, (float)from, emitRiseOver);
		}
	}

	public virtual void Recycle(double percentage = -1.0, bool destroyAfterRecycle = true)
	{
		GetRecycleRuntime()?.Recycle((float)percentage, destroyAfterRecycle);
	}

	internal void RefreshResourceComponentFacades()
	{
		_resourceComponentFacadesCurrent = false;
		ComponentManager componentManager = this.componentManager;
		if (!GodotObject.IsInstanceValid(componentManager))
		{
			return;
		}
		buff = null;
		this.showHealthComponent = null;
		this.shadowComponent = null;
		this.resourceSpawnComponent = null;
		this.customVisualComponent = null;
		this.targetRegistrationComponent = null;
		this.riseComponent = null;
		this.destroyComponent = null;
		this.sleepComponent = null;
		this.groundHeightComponent = null;
		this.damagePartComponent = null;
		this.hurtComponent = null;
		this.hypnosesComponent = null;
		this.armorVisualComponent = null;
		this.hitFlashComponent = null;
		this.shaderEffectComponent = null;
		this.blowBackComponent = null;
		this.recycleComponent = null;
		this.effectCreateComponent = null;
		this.phonkComponent = null;
		this.dismemberComponent = null;
		IReadOnlyList<CharacterComponentRuntime> resourceComponents = componentManager.ResourceComponents;
		for (int i = 0; i < resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = resourceComponents[i];
			if (characterComponentRuntime != null && !characterComponentRuntime.IsReleased)
			{
				if (buff == null && characterComponentRuntime is BuffComponent buffComponent)
				{
					buff = buffComponent;
				}
				if (this.showHealthComponent == null && characterComponentRuntime is ShowHealthComponent showHealthComponent)
				{
					this.showHealthComponent = showHealthComponent;
				}
				if (this.shadowComponent == null && characterComponentRuntime is ShadowComponent shadowComponent)
				{
					this.shadowComponent = shadowComponent;
				}
				if (this.resourceSpawnComponent == null && characterComponentRuntime is ResourceSpawnComponent resourceSpawnComponent)
				{
					this.resourceSpawnComponent = resourceSpawnComponent;
				}
				if (this.customVisualComponent == null && characterComponentRuntime is CustomVisualComponent customVisualComponent)
				{
					this.customVisualComponent = customVisualComponent;
				}
				if (this.targetRegistrationComponent == null && characterComponentRuntime is TargetRegistrationComponent targetRegistrationComponent)
				{
					this.targetRegistrationComponent = targetRegistrationComponent;
				}
				if (this.riseComponent == null && characterComponentRuntime is RiseComponent riseComponent)
				{
					this.riseComponent = riseComponent;
				}
				if (this.destroyComponent == null && characterComponentRuntime is DestroyComponent destroyComponent)
				{
					this.destroyComponent = destroyComponent;
				}
				if (this.sleepComponent == null && characterComponentRuntime is SleepComponent sleepComponent)
				{
					this.sleepComponent = sleepComponent;
				}
				if (this.groundHeightComponent == null && characterComponentRuntime is GroundHeightComponent groundHeightComponent)
				{
					this.groundHeightComponent = groundHeightComponent;
				}
				if (this.damagePartComponent == null && characterComponentRuntime is DamagePartComponent damagePartComponent)
				{
					this.damagePartComponent = damagePartComponent;
				}
				if (this.hurtComponent == null && characterComponentRuntime is HurtComponent hurtComponent)
				{
					this.hurtComponent = hurtComponent;
				}
				if (this.hypnosesComponent == null && characterComponentRuntime is HypnosesComponent hypnosesComponent)
				{
					this.hypnosesComponent = hypnosesComponent;
				}
				if (this.armorVisualComponent == null && characterComponentRuntime is ArmorVisualComponent armorVisualComponent)
				{
					this.armorVisualComponent = armorVisualComponent;
				}
				if (this.hitFlashComponent == null && characterComponentRuntime is HitFlashComponent hitFlashComponent)
				{
					this.hitFlashComponent = hitFlashComponent;
				}
				if (this.shaderEffectComponent == null && characterComponentRuntime is ShaderEffectComponent shaderEffectComponent)
				{
					this.shaderEffectComponent = shaderEffectComponent;
				}
				if (this.blowBackComponent == null && characterComponentRuntime is BlowBackComponent blowBackComponent)
				{
					this.blowBackComponent = blowBackComponent;
				}
				if (this.recycleComponent == null && characterComponentRuntime is RecycleComponent recycleComponent)
				{
					this.recycleComponent = recycleComponent;
				}
				if (this.effectCreateComponent == null && characterComponentRuntime is EffectCreateComponent effectCreateComponent)
				{
					this.effectCreateComponent = effectCreateComponent;
				}
				if (this.phonkComponent == null && characterComponentRuntime is PhonkComponent phonkComponent)
				{
					this.phonkComponent = phonkComponent;
				}
				if (this.dismemberComponent == null && characterComponentRuntime is DismemberComponent dismemberComponent)
				{
					this.dismemberComponent = dismemberComponent;
				}
			}
		}
		_resourceComponentFacadesCurrent = true;
	}

	private ResourceSpawnComponent GetResourceSpawnRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (resourceSpawnComponent != null && resourceSpawnComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		ResourceSpawnComponent obj = resourceSpawnComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return resourceSpawnComponent;
	}

	protected SleepComponent GetSleepRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (sleepComponent != null && sleepComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		SleepComponent obj = sleepComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return sleepComponent;
	}

	private DamagePartComponent GetDamagePartRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (damagePartComponent != null && damagePartComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		DamagePartComponent obj = damagePartComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return damagePartComponent;
	}

	private HurtComponent GetHurtRuntime()
	{
		HurtComponent hurtComponent = this.hurtComponent;
		if (hurtComponent != null && !hurtComponent.IsReleased)
		{
			return hurtComponent;
		}
		if (!_resourceComponentFacadesCurrent || hurtComponent != null)
		{
			RefreshResourceComponentFacades();
		}
		hurtComponent = this.hurtComponent;
		if (hurtComponent == null || hurtComponent.IsReleased)
		{
			return null;
		}
		return hurtComponent;
	}

	private RiseComponent GetRiseRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (riseComponent != null && riseComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		RiseComponent obj = riseComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return riseComponent;
	}

	private DestroyComponent GetDestroyRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (destroyComponent != null && destroyComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		DestroyComponent obj = destroyComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return destroyComponent;
	}

	private BlowBackComponent GetBlowBackRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (blowBackComponent != null && blowBackComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		BlowBackComponent obj = blowBackComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return blowBackComponent;
	}

	private RecycleComponent GetRecycleRuntime()
	{
		if (!_resourceComponentFacadesCurrent || (recycleComponent != null && recycleComponent.IsReleased))
		{
			RefreshResourceComponentFacades();
		}
		RecycleComponent obj = recycleComponent;
		if (obj == null || obj.IsReleased)
		{
			return null;
		}
		return recycleComponent;
	}

	public TowerDefenseEffectParticlesOnce CreateDirt()
	{
		return effectCreateComponent?.CreateDirt();
	}

	public TowerDefenseEffectSpriteOnce CreateSplash()
	{
		return effectCreateComponent?.CreateSplash();
	}

	public TowerDefenseEffectParticlesOnce CreateIceTrap()
	{
		return effectCreateComponent?.CreateIceTrap();
	}

	public void WakeUp()
	{
		if (!IsNodeReady())
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(this))
				{
					WakeUp();
				}
			}).CallDeferred();
		}
		else
		{
			buff.DeleteBuff("Sleep");
			instance.wakeUp = true;
			GetSleepRuntime()?.ReevaluateEnvironmentState();
			OnWakeUp();
		}
	}

	protected virtual void OnWakeUp()
	{
	}

	public virtual void AnimeCompleted(string clip)
	{
	}

	public virtual void AnimeEvent(string command, Variant argument)
	{
	}

	public virtual void DamagePointReach(string damagePointName)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && syncId >= 0)
		{
			MultiPlayerManager.Instance.SendDamagePointReach(syncId, damagePointName);
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && instance.damagePointData != null)
		{
			instance.damagePointData.SetDamagePointFliters(sprite, damagePointName);
		}
	}

	public virtual void ArmorDamagePointReach(string armorName, int stage)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && syncId >= 0)
		{
			MultiPlayerManager.Instance.SendArmorDamagePointReach(syncId, armorName, stage);
		}
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseArmorInstance armorFromName = GetArmorFromName(armorName);
		if (armorFromName == null)
		{
			return;
		}
		armorFromName.stageIndex = stage;
		string replaceMethod = armorFromName.slotConfig.replaceMethod;
		if (!(replaceMethod == "Media"))
		{
			if (replaceMethod == "Sprite" && armorFromName.sprite != null && stage < armorFromName.typeData.stageAnimeTexturePaths.Count)
			{
				armorFromName.sprite.externalAtlasTexturePath = armorFromName.typeData.stageAnimeTexturePaths[stage];
			}
		}
		else
		{
			SetArmor(armorName, stage);
		}
	}

	public virtual void ArmorHitpointsEmpty(string armorName)
	{
		ShowHealthComponent obj = showHealthComponent;
		if (obj != null && !obj.IsReleased)
		{
			showHealthComponent.MarkDirty();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && syncId >= 0)
		{
			MultiPlayerManager.Instance.SendArmorHitpointsEmpty(syncId, armorName);
		}
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseArmorInstance armorFromName = GetArmorFromName(armorName);
		if (armorFromName != null && !armorFromName.isRemove)
		{
			if ((armorFromName.armorMethodFlags & 0x80) != 0 && !armorFromName.damagePartDropped)
			{
				DamagePartCreate(new StringName(armorName), null, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: true, Vector2.Zero, fromSync: true, null, 0L);
			}
			armorFromName.RemoveArmor();
			armorFromName.isRemove = true;
		}
	}

	public virtual void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
	}

	public virtual void UnlimitedFireInit()
	{
	}

	internal void InheritCoverSleepState(TowerDefenseCharacter source)
	{
		if (GodotObject.IsInstanceValid(source) && GodotObject.IsInstanceValid(source.instance) && SupportsCoverSleepInheritance(source) && SupportsCoverSleepInheritance(this))
		{
			if (!GodotObject.IsInstanceValid(instance))
			{
				_pendingCoverSleepState = (source.instance.sleep, source.instance.wakeUp);
				return;
			}
			instance.wakeUp = source.instance.wakeUp;
			GetSleepRuntime()?.ReevaluateEnvironmentState();
		}
	}

	private static bool SupportsCoverSleepInheritance(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (character.GetSleepRuntime() != null)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(character.config))
		{
			return !string.Equals(character.config.sleepTime, "Never", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public virtual void Cover(TowerDefenseCharacter character)
	{
	}

	public virtual void Spawn()
	{
	}

	public virtual void PreSpawn()
	{
	}

	private bool ShouldEnableGameplayDispatch()
	{
		if (inGame && !editorPreviewMode)
		{
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				return TowerDefenseManager.Instance.IsGameRunning();
			}
			return true;
		}
		return false;
	}

	public virtual void ActivateGameplayProcessing()
	{
		base.ProcessMode = ProcessModeEnum.Inherit;
		if (GodotObject.IsInstanceValid(sprite))
		{
			ActivateAdobeAnimateSpriteTree(sprite);
			if (_progressRestorePresentationRefreshPending)
			{
				sprite.InvalidateProgressRestorePresentation();
				_progressRestorePresentationRefreshPending = false;
			}
		}
		SetMainStateMachineDispatchEnabled(enabled: true);
		SetHitBoxEnabled(enabled: true);
		ConfigureCharacterBatchProcessing();
	}

	public virtual void ActivateLevelEntryPreview(string animationClip)
	{
		isShow = true;
		SetMainStateMachineDispatchEnabled(enabled: false);
		SuspendOwnerBatchDispatch();
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		base.ProcessMode = ProcessModeEnum.Disabled;
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.RestoreRuntimeNativeCanvasLayer();
			sprite.ProcessMode = ProcessModeEnum.Always;
			sprite.pause = false;
			if (!string.IsNullOrWhiteSpace(animationClip))
			{
				sprite.SetAnimation(animationClip);
			}
			sprite.RefreshProcessScheduling();
		}
	}

	public void SetMainStateMachineDispatchEnabled(bool enabled)
	{
		_mainStateMachineDispatchEnabled = enabled && !_mainStateMachineReleased;
		RefreshComponentStateMachineDispatch();
	}

	internal void RefreshComponentStateMachineDispatch()
	{
		if (this is TowerDefenseZombie zombie)
		{
			TowerDefenseZombieBatch.UpdateProcessDispatch(zombie);
		}
		else
		{
			TowerDefenseCharacterBatch.UpdateProcessDispatch(this);
		}
		UpdateDirectMainStateMachineProcess();
	}

	internal void SetOwnerBatchRegistration(bool registered)
	{
		_ownerBatchRegistered = registered;
		UpdateDirectMainStateMachineProcess();
		SetPhysicsProcess(!registered && IsInsideTree() && inGame && !isDestroy);
	}

	private void UpdateDirectMainStateMachineProcess()
	{
		SetProcess(IsInsideTree() && !_ownerBatchRegistered && WantsMainStateMachineProcessDispatch);
	}

	private void ResumeOwnerBatchDispatch()
	{
		if (IsInsideTree() && inGame && !((!(this is TowerDefenseZombie zombie)) ? (UseCharacterBatch && TowerDefenseCharacterBatch.Register(this)) : (TowerDefenseZombie.UseBatch && TowerDefenseZombieBatch.Register(zombie))))
		{
			SetOwnerBatchRegistration(registered: false);
		}
	}

	private void SuspendOwnerBatchDispatch()
	{
		if (_ownerBatchRegistered)
		{
			if (this is TowerDefenseZombie zombie)
			{
				TowerDefenseZombieBatch.Unregister(zombie);
			}
			else
			{
				TowerDefenseCharacterBatch.Unregister(this);
			}
		}
		_ownerBatchRegistered = false;
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
	}

	private void ConfigureCharacterBatchProcessing()
	{
		if (!IsNodeReady() || CachedEditorHint || this is TowerDefenseZombie)
		{
			return;
		}
		if (!inGame)
		{
			SetPhysicsProcess(enable: false);
		}
		else if (UseCharacterBatch)
		{
			if (!TowerDefenseCharacterBatch.Register(this))
			{
				SetOwnerBatchRegistration(registered: false);
			}
		}
		else if (_ownerBatchRegistered)
		{
			TowerDefenseCharacterBatch.Unregister(this);
		}
		else
		{
			SetOwnerBatchRegistration(registered: false);
		}
	}

	private void EnterInitialMainStateMachine()
	{
		if (_pendingMainStateMachineSnapshot != null)
		{
			ApplyPendingMainStateMachineSnapshot();
		}
		else
		{
			StateMachine?.EnterInitialState();
		}
	}

	internal Dictionary CaptureMainStateMachineSnapshotData()
	{
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return new Dictionary();
		}
		return StateMachineSnapshotCodec.Encode(StateMachine.CaptureSnapshot());
	}

	internal bool RestoreMainStateMachineSnapshotData(Dictionary data, bool remote, bool suppressEntryEffects, bool progressCompatible = false)
	{
		StateMachineSnapshot stateMachineSnapshot = StateMachineSnapshotCodec.Decode(data);
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		if (remote && stateMachineSnapshot.Revision < _lastRemoteMainStateMachineRevision)
		{
			return true;
		}
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			if (_pendingMainStateMachineSnapshot == null || !remote || !_pendingMainStateMachineSnapshotIsRemote || stateMachineSnapshot.Revision >= _pendingMainStateMachineSnapshot.Revision)
			{
				_pendingMainStateMachineSnapshot = stateMachineSnapshot;
				_pendingMainStateMachineSnapshotIsRemote = remote;
				_pendingMainStateMachineSnapshotSuppressEffects = suppressEntryEffects;
				_pendingMainStateMachineSnapshotProgressCompatible = progressCompatible;
			}
			return true;
		}
		if (!PrepareMainStateMachineSnapshot(stateMachineSnapshot, progressCompatible))
		{
			return false;
		}
		return ApplyMainStateMachineSnapshot(stateMachineSnapshot, remote, suppressEntryEffects);
	}

	internal bool RestoreLegacyMainState(string stateIdentity)
	{
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized || string.IsNullOrWhiteSpace(stateIdentity))
		{
			return false;
		}
		StateHandle stateHandle = StateMachine.ResolveState(stateIdentity, allowDisplayNameFallback: true);
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		StateMachineSnapshot stateMachineSnapshot = StateMachine.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return StateMachine.RestoreSnapshot(stateMachineSnapshot);
	}

	internal void BeginAuthoritativeMainStateRestore()
	{
		_authoritativeMainStateRestoreDepth++;
	}

	internal void EndAuthoritativeMainStateRestore()
	{
		if (_authoritativeMainStateRestoreDepth > 0)
		{
			_authoritativeMainStateRestoreDepth--;
		}
	}

	private void ApplyPendingMainStateMachineSnapshot()
	{
		if (_pendingMainStateMachineSnapshot == null)
		{
			return;
		}
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			StateMachineSnapshot pendingMainStateMachineSnapshot = _pendingMainStateMachineSnapshot;
			bool pendingMainStateMachineSnapshotIsRemote = _pendingMainStateMachineSnapshotIsRemote;
			bool pendingMainStateMachineSnapshotSuppressEffects = _pendingMainStateMachineSnapshotSuppressEffects;
			bool pendingMainStateMachineSnapshotProgressCompatible = _pendingMainStateMachineSnapshotProgressCompatible;
			_pendingMainStateMachineSnapshot = null;
			_pendingMainStateMachineSnapshotIsRemote = false;
			_pendingMainStateMachineSnapshotSuppressEffects = false;
			_pendingMainStateMachineSnapshotProgressCompatible = false;
			if (PrepareMainStateMachineSnapshot(pendingMainStateMachineSnapshot, pendingMainStateMachineSnapshotProgressCompatible))
			{
				ApplyMainStateMachineSnapshot(pendingMainStateMachineSnapshot, pendingMainStateMachineSnapshotIsRemote, pendingMainStateMachineSnapshotSuppressEffects);
			}
		}
	}

	private bool PrepareMainStateMachineSnapshot(StateMachineSnapshot snapshot, bool progressCompatible)
	{
		if (snapshot != null)
		{
			IStateMachineController stateMachine = StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				if (progressCompatible)
				{
					StateMachineSnapshot stateMachineSnapshot = StateMachine.CaptureSnapshot();
					if (stateMachineSnapshot == null || !IsProgressStateMachineDefinitionIdCompatible(snapshot.DefinitionId, stateMachineSnapshot.DefinitionId) || snapshot.SchemaVersion != stateMachineSnapshot.SchemaVersion)
					{
						return false;
					}
					snapshot.DefinitionId = stateMachineSnapshot.DefinitionId;
					snapshot.ContentHash = stateMachineSnapshot.ContentHash;
				}
				return StateMachine.CanRestoreSnapshot(snapshot);
			}
		}
		return false;
	}

	protected virtual bool IsProgressStateMachineDefinitionIdCompatible(string savedDefinitionId, string currentDefinitionId)
	{
		return string.Equals(savedDefinitionId, currentDefinitionId, StringComparison.Ordinal);
	}

	private bool ApplyMainStateMachineSnapshot(StateMachineSnapshot snapshot, bool remote, bool suppressEntryEffects)
	{
		if (remote && snapshot.Revision < _lastRemoteMainStateMachineRevision)
		{
			return true;
		}
		if (remote)
		{
			BeginAuthoritativeMainStateRestore();
		}
		bool flag;
		try
		{
			flag = StateMachine.RestoreSnapshot(snapshot, suppressEntryEffects);
		}
		finally
		{
			if (remote)
			{
				EndAuthoritativeMainStateRestore();
			}
		}
		if (flag && remote)
		{
			_lastRemoteMainStateMachineRevision = Math.Max(_lastRemoteMainStateMachineRevision, snapshot.Revision);
		}
		if (flag)
		{
			OnAuthoritativeMainStateRestored(snapshot, remote);
		}
		return flag;
	}

	protected virtual void OnAuthoritativeMainStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
	}

	private void ActivateAdobeAnimateSpriteTree(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.RestoreRuntimeNativeCanvasLayer();
			adobeAnimateSprite.ProcessMode = ProcessModeEnum.Inherit;
			if (!ShouldKeepAnimationPaused())
			{
				adobeAnimateSprite.pause = false;
			}
			adobeAnimateSprite.RefreshProcessScheduling();
		}
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			ActivateAdobeAnimateSpriteTree(node.GetChild(i));
		}
	}

	private bool ShouldKeepAnimationPaused()
	{
		if (this is TowerDefenseZombie towerDefenseZombie)
		{
			TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
			if (!towerDefenseZombie.isPause && !towerDefenseZombie.spritePause)
			{
				return towerDefenseManager?.pauseZombie ?? false;
			}
			return true;
		}
		return false;
	}

	public virtual void Blow()
	{
	}

	public virtual void Garlic()
	{
	}

	public virtual bool CanBlock()
	{
		return false;
	}

	public virtual bool ShouldUpdateGridPos()
	{
		return true;
	}

	public virtual void OnRiseStart()
	{
	}

	public virtual void OnRiseEnd()
	{
	}

	public virtual string BlockType()
	{
		return "General";
	}

	public virtual void Block(TowerDefenseCharacter target)
	{
	}

	public virtual bool CanDiggerBlock()
	{
		return false;
	}

	public virtual void BlockDigger(TowerDefenseCharacter target)
	{
	}

	public virtual void Purify()
	{
	}

	public virtual void SpawnZombie()
	{
	}

	public virtual void OnAnimeStarted(string _clipName)
	{
	}

	public virtual void SyncAnimation(string clipName, bool loopAnim, double blendTimeVal)
	{
		_syncApplyingAnimation = true;
		sprite.SetAnimation(clipName, loopAnim, (float)blendTimeVal);
		_syncApplyingAnimation = false;
	}

	public virtual void HitpointsNearDie()
	{
		destroyComponent?.BeginDeathSettlement();
		nearDie = true;
		this.OnCharacterNearDie?.Invoke(this);
	}

	public virtual void HitpointsEmpty()
	{
		die = true;
		destroyComponent?.BeginDeathSettlement();
	}

	public virtual void InWater()
	{
		ShadowComponent obj = shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			shadowComponent.SetShadowVisible(visible: false);
		}
	}

	public virtual void OutWater()
	{
		ShadowComponent obj = shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			shadowComponent.SetShadowVisible(!invisible);
		}
	}

	public static Variant GetFireAnime(string type)
	{
		return type switch
		{
			"Fire" => FIRE, 
			"IceFire" => ICE_FIRE, 
			"MegaFire" => MEGA_FIRE, 
			"PurifyFire" => PURIFY_FIRE, 
			"WhiteFire" => WHITE_FIRE, 
			"GarlicFire" => GARLIC_FIRE, 
			_ => FIRE, 
		};
	}

	public static Dictionary CreateFireEventLists(double num, Array<TowerDefenseCharacterEventBase> _eventTarget, Array<TowerDefenseCharacterEventBase> _allEventList)
	{
		Array<TowerDefenseCharacterEventBase> array = new Array<TowerDefenseCharacterEventBase>(_eventTarget);
		TowerDefenseCharacterEventExplodeHurt towerDefenseCharacterEventExplodeHurt = new TowerDefenseCharacterEventExplodeHurt();
		towerDefenseCharacterEventExplodeHurt.num = num;
		towerDefenseCharacterEventExplodeHurt.type = "Jala";
		array.Insert(0, towerDefenseCharacterEventExplodeHurt);
		Array<TowerDefenseCharacterEventBase> array2 = new Array<TowerDefenseCharacterEventBase>(_allEventList);
		TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = new TowerDefenseCharacterEventAddBuff();
		towerDefenseCharacterEventAddBuff.buffList.Insert(0, new TowerDefenseCharacterBuffFireHit());
		towerDefenseCharacterEventAddBuff.buffList.Insert(1, new TowerDefenseCharacterBuffJalaHit());
		array2.Add(towerDefenseCharacterEventAddBuff);
		return new Dictionary
		{
			["event_list"] = array,
			["all_event_list"] = array2
		};
	}

	private static void CreateFireEffectAtCell(Variant fireAnime, Vector2I cellPos, TowerDefenseControlNew battleControl, Node2D effectParent)
	{
		if (IsJalapenoBattleContextCurrent(battleControl, effectParent))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(cellPos);
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce((PackedScene)(GodotObject)fireAnime, cellPos, "Flame|Done");
			towerDefenseEffectSpriteOnce.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(cellPos) + new Vector2(0f, 30f);
			if (GodotObject.IsInstanceValid(mapCell))
			{
				towerDefenseEffectSpriteOnce.GlobalPosition = new Vector2(towerDefenseEffectSpriteOnce.GlobalPosition.X, (float)((double)towerDefenseEffectSpriteOnce.GlobalPosition.Y - mapCell.GetGroundHeight()));
			}
			effectParent.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static bool IsJalapenoBattleContextCurrent(TowerDefenseControlNew battleControl, Node2D effectParent)
	{
		if (GodotObject.IsInstanceValid(battleControl) && battleControl.IsInsideTree() && battleControl.isGameRunning && TowerDefenseManager.CurrentControl == battleControl && GodotObject.IsInstanceValid(effectParent) && effectParent.IsInsideTree())
		{
			return TowerDefenseGroundItemBase.characterNode == effectParent;
		}
		return false;
	}

	public static void PlayFireExplodeEffects()
	{
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		AudioManager.Instance.AudioPlay("ExplodeJalapeno");
	}

	public static Array<TowerDefenseCharacterEventBase> CreateSnowEventList(Array<TowerDefenseCharacterEventBase> _addEventList)
	{
		Array<TowerDefenseCharacterEventBase> array = new Array<TowerDefenseCharacterEventBase>(_addEventList);
		TowerDefenseCharacterEventForzen item = new TowerDefenseCharacterEventForzen
		{
			time = 3.0
		};
		array.Insert(0, new TowerDefenseCharacterEventHurt
		{
			collisionFlags = -1,
			num = 20.0
		});
		array.Insert(0, item);
		return array;
	}

	public static async Task CreateJalapenoFire(TowerDefenseEnum.CHARACTER_CAMP _camp, Vector2I _gridPos, double num = -1.0, Array<TowerDefenseCharacterEventBase> _eventTarget = null, Array<TowerDefenseCharacterEventBase> _allEventList = null, string type = "Fire", bool suppressDeathrattles = false)
	{
		TowerDefenseControlNew battleControl = TowerDefenseManager.CurrentControl;
		Node2D effectParent = TowerDefenseGroundItemBase.characterNode;
		if (!IsJalapenoBattleContextCurrent(battleControl, effectParent))
		{
			return;
		}
		if (_eventTarget == null)
		{
			_eventTarget = new Array<TowerDefenseCharacterEventBase>();
		}
		if (_allEventList == null)
		{
			_allEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		Variant fireAnime = GetFireAnime(type);
		Dictionary dictionary = CreateFireEventLists(num, _eventTarget, _allEventList);
		Array<TowerDefenseCharacterEventBase> eventList = (Array<TowerDefenseCharacterEventBase>)dictionary["event_list"];
		Array<TowerDefenseCharacterEventBase> eventList2 = (Array<TowerDefenseCharacterEventBase>)dictionary["all_event_list"];
		BattleEventBus.Instance.EmitJalaLineEffectEmit(_gridPos.Y);
		TowerDefenseExplode.CreateExplodeLine(_gridPos.Y, eventList, new Array<TowerDefenseCharacter>(), _camp, -1, suppressDeathrattles);
		TowerDefenseExplode.CreateExplodeLine(_gridPos.Y, eventList2, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.ALL, -1, suppressDeathrattles);
		Godot.Collections.Array mapIceCapList = TowerDefenseManager.Instance.GetMapIceCapList();
		if (_gridPos.Y >= 0 && _gridPos.Y < mapIceCapList.Count)
		{
			Node node = mapIceCapList[_gridPos.Y].AsGodotObject() as Node;
			if (GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
			}
		}
		PlayFireExplodeEffects();
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		for (int i = 0; i < mapGridNum.X; i++)
		{
			bool flag = true;
			if (i == 0)
			{
				CreateFireEffectAtCell(fireAnime, _gridPos, battleControl, effectParent);
				flag = false;
			}
			else
			{
				if (_gridPos.X - i > 0)
				{
					CreateFireEffectAtCell(fireAnime, _gridPos - new Vector2I(i, 0), battleControl, effectParent);
					flag = false;
				}
				if (_gridPos.X + i <= mapGridNum.X)
				{
					CreateFireEffectAtCell(fireAnime, _gridPos + new Vector2I(i, 0), battleControl, effectParent);
					flag = false;
				}
			}
			if (!flag)
			{
				SceneTreeTimer source = battleControl.GetTree().CreateTimer(0.025, processAlways: false);
				await battleControl.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
				if (!IsJalapenoBattleContextCurrent(battleControl, effectParent))
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	public static async Task CreateJalapenoFireColumn(TowerDefenseEnum.CHARACTER_CAMP _camp, Vector2I _gridPos, double num = -1.0, Array<TowerDefenseCharacterEventBase> _eventTarget = null, Array<TowerDefenseCharacterEventBase> _allEventList = null, string type = "Fire", bool suppressDeathrattles = false)
	{
		TowerDefenseControlNew battleControl = TowerDefenseManager.CurrentControl;
		Node2D effectParent = TowerDefenseGroundItemBase.characterNode;
		if (!IsJalapenoBattleContextCurrent(battleControl, effectParent))
		{
			return;
		}
		if (_eventTarget == null)
		{
			_eventTarget = new Array<TowerDefenseCharacterEventBase>();
		}
		if (_allEventList == null)
		{
			_allEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		Variant fireAnime = GetFireAnime(type);
		Dictionary dictionary = CreateFireEventLists(num, _eventTarget, _allEventList);
		Array<TowerDefenseCharacterEventBase> eventList = (Array<TowerDefenseCharacterEventBase>)dictionary["event_list"];
		Array<TowerDefenseCharacterEventBase> eventList2 = (Array<TowerDefenseCharacterEventBase>)dictionary["all_event_list"];
		BattleEventBus.Instance.EmitJalaRowEffectEmit(_gridPos.X);
		TowerDefenseExplode.CreateExplodeColumn(_gridPos.X, eventList, new Array<TowerDefenseCharacter>(), _camp, -1, suppressDeathrattles);
		TowerDefenseExplode.CreateExplodeColumn(_gridPos.X, eventList2, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.ALL, -1, suppressDeathrattles);
		PlayFireExplodeEffects();
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		for (int i = 0; i < mapGridNum.Y; i++)
		{
			bool flag = true;
			if (i == 0)
			{
				CreateFireEffectAtCell(fireAnime, _gridPos, battleControl, effectParent);
				flag = false;
			}
			else
			{
				if (_gridPos.Y - i > 0)
				{
					CreateFireEffectAtCell(fireAnime, _gridPos - new Vector2I(0, i), battleControl, effectParent);
					flag = false;
				}
				if (_gridPos.Y + i <= mapGridNum.Y)
				{
					CreateFireEffectAtCell(fireAnime, _gridPos + new Vector2I(0, i), battleControl, effectParent);
					flag = false;
				}
			}
			if (!flag)
			{
				SceneTreeTimer source = battleControl.GetTree().CreateTimer(0.025, processAlways: false);
				await battleControl.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
				if (!IsJalapenoBattleContextCurrent(battleControl, effectParent))
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	public static async Task CreateJalapenoFireSlash(TowerDefenseEnum.CHARACTER_CAMP _camp, Vector2I _gridPos, double num = -1.0, Array<TowerDefenseCharacterEventBase> _eventTarget = null, Array<TowerDefenseCharacterEventBase> _allEventList = null, string type = "Fire", bool suppressDeathrattles = false)
	{
		TowerDefenseControlNew battleControl = TowerDefenseManager.CurrentControl;
		Node2D effectParent = TowerDefenseGroundItemBase.characterNode;
		BattleEventBus battleEventBus = BattleEventBus.Instance;
		if (!IsJalapenoBattleContextCurrent(battleControl, effectParent) || !GodotObject.IsInstanceValid(battleEventBus))
		{
			return;
		}
		if (_eventTarget == null)
		{
			_eventTarget = new Array<TowerDefenseCharacterEventBase>();
		}
		if (_allEventList == null)
		{
			_allEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		Variant fireAnime = GetFireAnime(type);
		Dictionary dictionary = CreateFireEventLists(num, _eventTarget, _allEventList);
		Array<TowerDefenseCharacterEventBase> eventList = (Array<TowerDefenseCharacterEventBase>)dictionary["event_list"];
		Array<TowerDefenseCharacterEventBase> allEventList = (Array<TowerDefenseCharacterEventBase>)dictionary["all_event_list"];
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		List<Vector2I> list = new List<Vector2I>();
		int num2 = Math.Max(mapGridNum.X, mapGridNum.Y);
		for (int i = 0; i <= num2; i++)
		{
			foreach (Vector2I item in new List<Vector2I>
			{
				_gridPos + new Vector2I(i, i),
				_gridPos + new Vector2I(i, -i),
				_gridPos + new Vector2I(-i, i),
				_gridPos + new Vector2I(-i, -i)
			})
			{
				if (item.X >= 1 && item.X <= mapGridNum.X && item.Y >= 1 && item.Y <= mapGridNum.Y)
				{
					if (i == 0)
					{
						list.Add(item);
						break;
					}
					list.Add(item);
				}
			}
		}
		PlayFireExplodeEffects();
		foreach (Vector2I item2 in list)
		{
			battleEventBus.EmitJalaGridEffectEmit(item2);
			TowerDefenseExplode.CreateExplode(TowerDefenseManager.GetMapCellPlantPos(item2), new Vector2(0.5f, 0.5f), eventList, new Array<TowerDefenseCharacter>(), _camp, -1, suppressDeathrattles);
			TowerDefenseExplode.CreateExplode(TowerDefenseManager.GetMapCellPlantPos(item2), new Vector2(0.5f, 0.5f), allEventList, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.ALL, -1, suppressDeathrattles);
			CreateFireEffectAtCell(fireAnime, item2, battleControl, effectParent);
			SceneTreeTimer source = battleControl.GetTree().CreateTimer(0.025, processAlways: false);
			await battleControl.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
			if (!IsJalapenoBattleContextCurrent(battleControl, effectParent) || !GodotObject.IsInstanceValid(battleEventBus))
			{
				return;
			}
		}
	}

	public static void CreateColdVisualEffect(Vector2I gridPos)
	{
		ViewManager.Instance.FullScreenColorBlink(new Color(0.117647f, 0.564706f, 1f, 0.5f));
		AudioManager.Instance.AudioPlay("Frozen");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SNOW_FLAKES, gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public static void CreateColdEffect(TowerDefenseEnum.CHARACTER_CAMP _camp, Vector2I _gridPos, Array<TowerDefenseCharacterEventBase> _addEventList = null)
	{
		if (_addEventList == null)
		{
			_addEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		BattleEventBus.Instance.EmitColdEffectEmit();
		CreateColdVisualEffect(_gridPos);
		Array<TowerDefenseCharacterEventBase> array = CreateSnowEventList(_addEventList);
		foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(_camp))
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			foreach (TowerDefenseCharacterEventBase item2 in array)
			{
				item2.Execute(towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter);
			}
		}
	}

	public static void CreateColdEffectRange(TowerDefenseEnum.CHARACTER_CAMP _camp, Vector2I _gridPos, Vector2 _range, Array<TowerDefenseCharacterEventBase> _addEventList = null)
	{
		if (_addEventList == null)
		{
			_addEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		ViewManager.Instance.FullScreenColorBlink(new Color(0.117647f, 0.564706f, 1f, 0.5f));
		AudioManager.Instance.AudioPlay("Frozen");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SNOW_FLAKES, _gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(_gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		Array<TowerDefenseCharacterEventBase> eventList = CreateSnowEventList(_addEventList);
		TowerDefenseExplode.CreateExplode(towerDefenseEffectParticlesOnce.GlobalPosition, _range, eventList, new Array<TowerDefenseCharacter>(), _camp, -1);
	}

	public static TowerDefenseCharacter CreateCharacter(string packetName, Vector2 pos, Vector2I _gridPos, double _groundHeight)
	{
		return CreateCharacterCore(default(EconomyAccountId), packetName, pos, _gridPos, _groundHeight);
	}

	public static TowerDefenseCharacter CreateCharacter(EconomyAccountId economyOwnerAccountId, string packetName, Vector2 pos, Vector2I gridPos, double groundHeight)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return CreateCharacterCore(economyOwnerAccountId, packetName, pos, gridPos, groundHeight);
	}

	private static TowerDefenseCharacter CreateCharacterCore(EconomyAccountId economyOwnerAccountId, string packetName, Vector2 pos, Vector2I gridPos, double groundHeight)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = (economyOwnerAccountId.IsValid ? packetConfig.Create(economyOwnerAccountId, pos, gridPos, groundHeight) : packetConfig.Create(pos, gridPos, groundHeight));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		towerDefenseCharacter.groundHeight = groundHeight;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseCharacter;
	}

	public virtual bool TryConsumeIncomingBuff(TowerDefenseCharacterBuffConfig buffConfig)
	{
		return false;
	}

	public void BuffAdd(TowerDefenseCharacterBuffConfig buffConfig)
	{
		buff.AddBuff(buffConfig);
	}

	public void BuffDelete(string key)
	{
		buff.DeleteBuff(key);
	}

	public TowerDefenseCharacterBuffConfig BuffGet(string key)
	{
		return buff.BuffGet(key);
	}

	public virtual Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary();
	}

	public virtual void ImportNetworkSpecialState(Dictionary _data)
	{
	}

	public virtual Dictionary ExportNetworkSpawnState()
	{
		return new Dictionary();
	}

	public virtual int GetNetworkSpecialStateRevision()
	{
		return 0;
	}

	public virtual bool IsNetworkSpecialMovementActive()
	{
		return false;
	}

	public virtual void OnRemoteNetworkAnimationStart(string _clipName)
	{
	}

	public void RestoreFromSave(TowerDefenseCharacterSaveConfigCSharp saveConfig)
	{
		instance.ImportSave(saveConfig.instanceSave);
		nearDie = instance.nearDie;
		die = instance.die;
		timeScaleInit = saveConfig.timeScaleInit;
		timeScale = saveConfig.timeScale;
		timeScaleSave = saveConfig.timeScaleSave;
		if (instance.nearDie)
		{
			ShowHealthComponent obj = showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				showHealthComponent.SetAlive(alive: true);
			}
		}
		buff?.ImportSave(saveConfig.buffSave);
		if (instance.damagePointIndex <= 0 || instance.damagePointData == null)
		{
			return;
		}
		for (int i = 0; i < instance.damagePointIndex; i++)
		{
			if (i >= instance.damagePoints.Count)
			{
				continue;
			}
			string damagePointName = (string)instance.damagePoints[i]["Name"];
			instance.damagePointData.SetDamagePointFliters(sprite, damagePointName);
			if (config.customData == null)
			{
				continue;
			}
			foreach (string item in currentCustom)
			{
				config.customData.SetDamagePoint(sprite, item, i);
			}
		}
	}

	public virtual Dictionary ExportVariantSave()
	{
		return new Dictionary();
	}

	public virtual void ImportVariantSave(Dictionary _data)
	{
	}

	public virtual bool ImportVariantSaveWhenEmpty()
	{
		return false;
	}

	public virtual void PrepareForProgressRestore()
	{
		IsProgressRestoreInFlight = true;
		base.ProcessMode = ProcessModeEnum.Disabled;
	}

	public virtual void FinalizeProgressRestore()
	{
		if (die || nearDie)
		{
			destroyComponent?.BeginDeathSettlement();
		}
		else
		{
			destroyComponent?.EndDeathSettlement();
		}
		IsProgressRestoreInFlight = false;
		_progressRestorePresentationRefreshPending = true;
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.InvalidateProgressRestorePresentation();
		}
		GetSleepRuntime()?.ReevaluateEnvironmentState();
	}

	public TowerDefenseCharacter TransformTo(string targetName, Action<TowerDefenseCharacter> onTargetReady = null, PackedScene effectScene = null, bool syncCamp = true, bool syncBodyHp = true, bool syncHypnoses = true, bool removeSource = true)
	{
		if (!GodotObject.IsInstanceValid(this))
		{
			return null;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(targetName);
		if (packetConfig == null)
		{
			return null;
		}
		Vector2I vector2I = base.gridPos;
		Vector2 vector = ((this is TowerDefenseZombie) ? GetLogicalGlobalPosition() : TowerDefenseManager.GetMapCellPlantPos(vector2I));
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		double num = ((instance != null) ? instance.hitpoints : 0.0);
		bool flag = instance != null && instance.hypnoses;
		TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = camp;
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(vector2I, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
		if (towerDefenseCharacter == null || !GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		if (this is TowerDefenseZombie && towerDefenseCharacter is TowerDefenseZombie)
		{
			towerDefenseCharacter.SetLogicalGlobalPosition(node2D.ToLocal(vector));
			towerDefenseCharacter.groundHeight = base.groundHeight;
			towerDefenseCharacter.z = base.groundHeight;
			towerDefenseCharacter.Scale = new Vector2((base.Scale.X < 0f) ? (0f - Mathf.Abs(towerDefenseCharacter.Scale.X)) : Mathf.Abs(towerDefenseCharacter.Scale.X), towerDefenseCharacter.Scale.Y);
		}
		if (syncCamp)
		{
			towerDefenseCharacter.camp = cHARACTER_CAMP;
		}
		if (syncBodyHp && GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			towerDefenseCharacter.instance.hitpointsSave = num;
			towerDefenseCharacter.instance.hitpoints = num;
		}
		if (syncHypnoses && flag)
		{
			TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP2 = cHARACTER_CAMP;
			if (buff?.BuffGet("Hypnoses") is TowerDefenseCharacterBuffHypnoses { saveCamp: not TowerDefenseEnum.CHARACTER_CAMP.NOONE } towerDefenseCharacterBuffHypnoses)
			{
				cHARACTER_CAMP2 = towerDefenseCharacterBuffHypnoses.saveCamp;
			}
			else
			{
				switch (cHARACTER_CAMP)
				{
				case TowerDefenseEnum.CHARACTER_CAMP.PLANT:
					cHARACTER_CAMP2 = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
					break;
				case TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE:
					cHARACTER_CAMP2 = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
					break;
				}
			}
			if (syncCamp)
			{
				towerDefenseCharacter.camp = cHARACTER_CAMP2;
			}
			towerDefenseCharacter.Hypnoses();
		}
		if (effectScene == null)
		{
			effectScene = DefaultTransformEffect;
		}
		if (effectScene != null)
		{
			SpawnTransformEffect(effectScene, vector2I, vector);
		}
		TransformWatcher transformWatcher = new TransformWatcher(this, towerDefenseCharacter, num, syncBodyHp, removeSource, onTargetReady);
		if (GodotObject.IsInstanceValid(node2D))
		{
			node2D.AddChild(transformWatcher, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			GetTree().Root.CallDeferred("add_child", transformWatcher);
		}
		return towerDefenseCharacter;
	}

	private void SilentlyRemove()
	{
		if (GodotObject.IsInstanceValid(this))
		{
			skipDestroySet = true;
			if (GodotObject.IsInstanceValid(cell))
			{
				cell.RemoveCharacter(this);
			}
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.CharacterUnregister(this);
			}
			RemoveFromGroup("Character");
			if (!IsQueuedForDeletion())
			{
				QueueFree();
			}
		}
	}

	internal void SilentlyRemoveForTransformation()
	{
		SilentlyRemove();
	}

	private static void SpawnTransformEffect(PackedScene effectScene, Vector2I gridPos, Vector2 globalPosition)
	{
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D) && effectScene != null)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(effectScene, gridPos, "Idle");
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.GlobalPosition = globalPosition;
			}
		}
	}

	private void InitializeCharacterExternalVisuals()
	{
		if (!_characterExternalVisualsInitialized && GodotObject.IsInstanceValid(sprite))
		{
			_characterExternalVisualsInitialized = true;
			RegisterLegacySpriteGroupVisualsOnce();
		}
	}

	private void DisposeCharacterExternalVisuals()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			for (int i = 0; i < _legacySpriteGroupVisualHandles.Count; i++)
			{
				sprite.UnregisterExternalVisual(_legacySpriteGroupVisualHandles[i]);
			}
		}
		_legacySpriteGroupVisualHandles.Clear();
		_characterExternalVisualsInitialized = false;
		_legacySpriteGroupVisualsScanned = false;
	}

	public void AttachAnimatedStatusVisual(AdobeAnimateSprite child, AdobeAnimateSlot preferredSlot, Transform2D targetGlobalTransform)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(sprite))
		{
			bool num = GodotObject.IsInstanceValid(preferredSlot);
			Node node = (num ? ((Node2D)preferredSlot) : ((Node2D)sprite));
			if (!num)
			{
				child.usePos = false;
				child.useRotate = false;
			}
			Node parent = child.GetParent();
			bool num2 = parent != node;
			if (GodotObject.IsInstanceValid(parent) && parent != node)
			{
				parent.RemoveChild(child);
			}
			if (child.GetParent() != node)
			{
				node.AddChild(child, forceReadableName: false, InternalMode.Disabled);
			}
			child.GlobalTransform = targetGlobalTransform;
			if (num2)
			{
				CharacterAnimatedStatusVisualCount++;
				CharacterAnimatedStatusTopologyChangeCount++;
				sprite.RefreshManagedSlotSpriteCacheForRender();
			}
		}
	}

	public void DetachAnimatedStatusVisual(AdobeAnimateSprite child)
	{
		if (!GodotObject.IsInstanceValid(child))
		{
			return;
		}
		Node parent = child.GetParent();
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.RemoveChild(child);
			CharacterAnimatedStatusVisualCount = Mathf.Max(0, CharacterAnimatedStatusVisualCount - 1);
			CharacterAnimatedStatusTopologyChangeCount++;
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.RefreshManagedSlotSpriteCacheForRender();
			}
		}
	}

	public void RegisterLegacySpriteGroupVisualsOnce()
	{
		if (_legacySpriteGroupVisualsScanned || !GodotObject.IsInstanceValid(spriteGroup))
		{
			return;
		}
		_legacySpriteGroupVisualsScanned = true;
		CharacterExternalVisualCompatibilityScanCount++;
		foreach (Node child in spriteGroup.GetChildren())
		{
			if (child is Sprite2D sprite2D && sprite2D != icetrapSprite && sprite2D.Texture != null && sprite2D.Material == null && sprite2D.GetScript().VariantType == Variant.Type.Nil)
			{
				AdobeAnimateExternalVisualHandle item = RegisterCharacterExternalVisual(sprite2D, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
				if (item.IsValid)
				{
					_legacySpriteGroupVisualHandles.Add(item);
				}
			}
		}
	}

	public AdobeAnimateExternalVisualHandle RegisterCharacterExternalVisual(Sprite2D visual, AdobeAnimateExternalVisualDescriptor descriptor)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return AdobeAnimateExternalVisualHandle.Invalid;
		}
		return sprite.RegisterExternalVisual(visual, descriptor);
	}

	public bool UnregisterCharacterExternalVisual(AdobeAnimateExternalVisualHandle handle)
	{
		if (GodotObject.IsInstanceValid(sprite) && handle.IsValid)
		{
			return sprite.UnregisterExternalVisual(handle);
		}
		return false;
	}

	internal bool TryGetIceTrapExternalVisualForDiagnostics(out AdobeAnimateExternalVisualSnapshot visual)
	{
		BuffComponent buffComponent = buff;
		if (buffComponent != null && !buffComponent.IsReleased)
		{
			return buff.TryGetBuffExternalVisual("Frozen", out visual);
		}
		visual = default(AdobeAnimateExternalVisualSnapshot);
		return false;
	}
}
