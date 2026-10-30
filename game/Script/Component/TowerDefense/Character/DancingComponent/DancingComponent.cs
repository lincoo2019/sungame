using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public sealed class DancingComponent : CharacterComponentRuntime
{
	public delegate void MoonWalkStartedEventHandler();

	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private StateHandle _moonWalkState;

	private StateHandle _danceState;

	private StateHandle _pointState;

	private bool _stateSignalsConnected;

	private bool _spriteSignalsConnected;

	private bool _configured;

	private SceneTreeTimer _spotlightTimer;

	private Action _spotlightTimerHandler;

	private float _gridSizeX;

	private double _groundRight;

	private string _pendingSyncedState;

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _pendingDancerPhysicsRegistered;

	private StringName _attackComponentName = "AttackComponent";

	private NodePath _spritePath = new NodePath();

	private NodePath _spotlightPath = new NodePath();

	private NodePath _spotlight2Path = new NodePath();

	public TowerDefenseZombie parent;

	public AttackComponent attackComponent;

	public GroundMoveComponent groundMoveComponent;

	public string dancerPacketName = "ZombieDancer";

	public float dancerRiseDuration = 1.5f;

	public float dancerWalkSpeedScaleMultiplier = 1f;

	public bool syncDancerAnimation = true;

	public int walkTimeInit = 4;

	public int danceTimeInit = 2;

	public float moonWalkGridDistance = 2.5f;

	public StringName pointStateEvent = "ToPoint";

	public StringName moonWalkStateEvent = "ToMoonWalk";

	public StringName danceStateEvent = "ToDance";

	public StringName idleStateEvent = "ToIdle";

	public AdobeAnimateSprite sprite;

	public string moonWalkAnimeClip = "MoonWalk";

	public float moonWalkAnimeTimeScale = 2f;

	public string armRiseAnimeClip = "ArmRise";

	public float armRiseAnimeTimeScale = 1f;

	public string pointUpAnimeClip = "PointUp";

	public string pointDownAnimeClip = "PointDown";

	public float pointAnimeTimeScale = 1f;

	public float pointDownDelay = 0.75f;

	public string walkAnimeClip = "Walk";

	public float dieAnimeTimeScale = 2f;

	public float moonWalkSpriteScaleX = -1f;

	public float normalSpriteScaleX = 1f;

	public float danceSpriteScaleX = -1f;

	public bool armRiseFlipSprite = true;

	public Sprite2D spotlight;

	public Sprite2D spotlight2;

	public Gradient spotlightGrandient;

	public float spotlightChangeInterval = 3f;

	public string spotlightAudioName = "Dancer";

	public bool spotlightColorCycleEnabled = true;

	public bool moonWalkOver;

	public bool moonWalkMode;

	public Vector2 savePos;

	public int walkTime = 4;

	public int danceTime = 2;

	public readonly List<TowerDefenseCharacter> dancerList = new List<TowerDefenseCharacter>(4);

	public bool firstSpawn;

	public bool isPointing;

	private DancingComponentDefinition Definition => base.ComponentDefinition as DancingComponentDefinition;

	private bool IsOwnerTerminal
	{
		get
		{
			if (IsInstanceValid(parent) && !parent.die)
			{
				if (IsInstanceValid(parent.instance))
				{
					if (!parent.instance.die)
					{
						if (!parent.instance.keepAlive)
						{
							return parent.instance.hitpoints <= 0.0;
						}
						return false;
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	private bool CanDispatchDancingGameplay
	{
		get
		{
			if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && IsInstanceValid(sprite))
			{
				return !IsOwnerTerminal;
			}
			return false;
		}
	}

	protected override bool AllowStateMachineOutsideComponentBattlefield => base.StateMachine?.CurrentStateHandle?.StableId == "dancing.moon_walk";

	private bool HasPendingDancerRelations
	{
		get
		{
			for (int i = 0; i < 4; i++)
			{
				if (_pendingDancerSyncIds[i] >= 0 || _pendingDancerNodeNames[i] != "")
				{
					return true;
				}
			}
			return false;
		}
	}

	internal override bool WantsPhysicsProcess => HasPendingDancerRelations;

	public event MoonWalkStartedEventHandler OnMoonWalkStarted;

	protected override bool CanSendStateEventOutsideComponentBattlefield(StringName eventName)
	{
		if (!(eventName == moonWalkStateEvent))
		{
			return base.CanSendStateEventOutsideComponentBattlefield(eventName);
		}
		return true;
	}

	private static bool IsInstanceValid(GodotObject instance)
	{
		return GodotObject.IsInstanceValid(instance);
	}

	protected override void OnBound()
	{
		ConfigureOnce();
		parent = base.Owner as TowerDefenseZombie;
		ResolveDependencies();
		while (dancerList.Count < 4)
		{
			dancerList.Add(null);
		}
		ConnectSpriteSignals();
		RefreshMapMetrics();
		ResolvePendingDancerRelations();
	}

	protected override void OnActivated()
	{
		ApplyPendingSyncedState();
		ResolvePendingDancerRelations();
		if (!IsInstanceValid(_spotlightTimer) && IsInstanceValid(spotlight) && spotlight.Visible)
		{
			ChangeSpotlightColor();
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		StopSpotlightColorCycle();
		groundMoveComponent?.SetAlive(false);
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			DisconnectSpriteSignals();
			ReleaseDancerOwnership();
			parent = null;
			attackComponent = null;
			groundMoveComponent = null;
			sprite = null;
			spotlight = null;
			spotlight2 = null;
		}
	}

	protected override void OnRuntimeReleased()
	{
		StopSpotlightColorCycle();
		DisconnectSpriteSignals();
		ReleaseDancerOwnership();
		dancerList.Clear();
		this.OnMoonWalkStarted = null;
		parent = null;
		attackComponent = null;
		groundMoveComponent = null;
		sprite = null;
		spotlight = null;
		spotlight2 = null;
		spotlightGrandient = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			HideSpotlights();
			groundMoveComponent?.SetAlive(false);
		}
		else if (base.Lifecycle == ComponentRuntimeLifecycle.Active && IsInstanceValid(spotlight) && spotlight.Visible)
		{
			ChangeSpotlightColor();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		_pendingDancerResolveRemaining -= delta;
		if (!(_pendingDancerResolveRemaining > 0.0))
		{
			ResolvePendingDancerRelations();
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			DancingComponentDefinition definition = Definition;
			dancerPacketName = definition?.dancerPacketName ?? "ZombieDancer";
			dancerRiseDuration = Mathf.Max(0f, definition?.dancerRiseDuration ?? 1.5f);
			dancerWalkSpeedScaleMultiplier = Mathf.Max(0f, definition?.dancerWalkSpeedScaleMultiplier ?? 1f);
			syncDancerAnimation = definition?.syncDancerAnimation ?? true;
			walkTimeInit = Math.Max(1, definition?.walkTimeInit ?? 4);
			danceTimeInit = Math.Max(1, definition?.danceTimeInit ?? 2);
			moonWalkGridDistance = Mathf.Max(0f, definition?.moonWalkGridDistance ?? 2.5f);
			pointStateEvent = definition?.pointStateEvent ?? new StringName("ToPoint");
			moonWalkStateEvent = definition?.moonWalkStateEvent ?? new StringName("ToMoonWalk");
			danceStateEvent = definition?.danceStateEvent ?? new StringName("ToDance");
			idleStateEvent = definition?.idleStateEvent ?? new StringName("ToIdle");
			moonWalkAnimeClip = definition?.moonWalkAnimeClip ?? "MoonWalk";
			moonWalkAnimeTimeScale = definition?.moonWalkAnimeTimeScale ?? 2f;
			armRiseAnimeClip = definition?.armRiseAnimeClip ?? "ArmRise";
			armRiseAnimeTimeScale = definition?.armRiseAnimeTimeScale ?? 1f;
			pointUpAnimeClip = definition?.pointUpAnimeClip ?? "PointUp";
			pointDownAnimeClip = definition?.pointDownAnimeClip ?? "PointDown";
			pointAnimeTimeScale = definition?.pointAnimeTimeScale ?? 1f;
			pointDownDelay = Mathf.Max(0f, definition?.pointDownDelay ?? 0.75f);
			walkAnimeClip = definition?.walkAnimeClip ?? "Walk";
			dieAnimeTimeScale = definition?.dieAnimeTimeScale ?? 2f;
			moonWalkSpriteScaleX = definition?.moonWalkSpriteScaleX ?? (-1f);
			normalSpriteScaleX = definition?.normalSpriteScaleX ?? 1f;
			danceSpriteScaleX = definition?.danceSpriteScaleX ?? (-1f);
			armRiseFlipSprite = definition?.armRiseFlipSprite ?? true;
			_attackComponentName = definition?.attackComponentName ?? new StringName("AttackComponent");
			_spritePath = definition?.spritePath ?? new NodePath();
			_spotlightPath = definition?.spotlightPath ?? new NodePath();
			_spotlight2Path = definition?.spotlight2Path ?? new NodePath();
			spotlightGrandient = definition?.spotlightGradient;
			spotlightChangeInterval = Mathf.Max(0f, definition?.spotlightChangeInterval ?? 3f);
			spotlightAudioName = definition?.spotlightAudioName ?? "Dancer";
			spotlightColorCycleEnabled = definition?.spotlightColorCycleEnabled ?? true;
			walkTime = walkTimeInit;
			danceTime = danceTimeInit;
			_configured = true;
		}
	}

	private void ResolveDependencies()
	{
		if (base.Manager != null && IsInstanceValid(parent))
		{
			string text = _attackComponentName.ToString();
			attackComponent = ((!string.IsNullOrEmpty(text) && base.Manager.TryGetRuntimeByLegacyNodeName(text, out var runtime)) ? (runtime as AttackComponent) : null);
			groundMoveComponent = base.Manager.GetRuntime<GroundMoveComponent>();
			sprite = ((!_spritePath.IsEmpty) ? parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath) : parent.sprite);
			spotlight = ((!_spotlightPath.IsEmpty) ? parent.GetNodeOrNull<Sprite2D>(_spotlightPath) : null);
			spotlight2 = ((!_spotlight2Path.IsEmpty) ? parent.GetNodeOrNull<Sprite2D>(_spotlight2Path) : null);
		}
	}

	private void ConnectSpriteSignals()
	{
		if (!_spriteSignalsConnected && IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			sprite.OnAnimeEvent += AnimeEvent;
			_spriteSignalsConnected = true;
		}
	}

	private void DisconnectSpriteSignals()
	{
		if (_spriteSignalsConnected && IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
			sprite.OnAnimeEvent -= AnimeEvent;
		}
		_spriteSignalsConnected = false;
	}

	private void ReleaseDancerOwnership()
	{
		for (int i = 0; i < 4; i++)
		{
			SetNetworkDancer(i, null);
		}
		System.Array.Fill(_pendingDancerSyncIds, -1);
		System.Array.Fill(_pendingDancerNodeNames, "");
		_pendingDancerResolveRemaining = 0.0;
		RefreshPendingDancerPhysicsRegistration();
	}

	public void RefreshMapMetrics()
	{
		if (IsInstanceValid(TowerDefenseManager.Instance))
		{
			_gridSizeX = TowerDefenseManager.Instance.GetMapGridSize().X;
			_groundRight = TowerDefenseManager.Instance.GetMapGroundRight();
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_moonWalkState = base.StateMachine?.GetStateById("dancing.moon_walk");
		_danceState = base.StateMachine?.GetStateById("dancing.dance");
		_pointState = base.StateMachine?.GetStateById("dancing.point");
		ConnectStateSignals();
		Callable.From(ApplyPendingSyncedState).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		_moonWalkState = null;
		_danceState = null;
		_pointState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		string text = base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
		bool flag = text == "dancing.moon_walk";
		bool flag2 = text == "dancing.dance";
		bool flag3 = text == "dancing.point";
		if (flag && RepairLegacyRunawayMoonWalk())
		{
			text = "dancing.idle";
			flag = false;
		}
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(flag);
		}
		isPointing = flag3;
		if (flag3)
		{
			moonWalkOver = true;
		}
		if (IsInstanceValid(sprite))
		{
			float x = (flag ? moonWalkSpriteScaleX : (flag2 ? danceSpriteScaleX : normalSpriteScaleX));
			sprite.Scale = new Vector2(x, sprite.Scale.Y);
		}
		foreach (TowerDefenseCharacter dancer2 in dancerList)
		{
			if (IsInstanceValid(dancer2) && dancer2 is IDancer dancer)
			{
				dancer.SetJackson(parent);
			}
		}
		RestoreAuthoritativeAnimation(text, remote);
	}

	private bool RepairLegacyRunawayMoonWalk()
	{
		if (!IsInstanceValid(parent) || moonWalkMode || _gridSizeX <= 0f || base.IsInsideComponentBattlefield)
		{
			return false;
		}
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		if ((double)logicalGlobalPosition.X <= _groundRight + (double)(_gridSizeX * 2f))
		{
			return false;
		}
		float x = (float)(_groundRight + (double)(_gridSizeX * 0.5f));
		parent.SetLogicalGlobalPosition(new Vector2(x, logicalGlobalPosition.Y));
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		parent.gridPos = new Vector2I(mapGridNum.X + 1, parent.gridPos.Y);
		moonWalkOver = false;
		moonWalkMode = false;
		savePos = Vector2.Zero;
		groundMoveComponent?.SetAlive(false);
		SyncForceState(base.StateMachine, "dancing.idle");
		Callable.From(delegate
		{
			if (IsInstanceValid(parent) && !parent.die && !parent.nearDie)
			{
				parent.Walk();
			}
		}).CallDeferred();
		GD.Print($"[ProgressLoad] Repaired runaway off-field Jackson '{parent.Name}'.");
		return true;
	}

	private void RestoreAuthoritativeAnimation(string stateId, bool remote)
	{
		if (remote && CanDispatchDancingGameplay && (!(stateId == "dancing.point") || (!(sprite.clip == pointUpAnimeClip) && !(sprite.clip == pointDownAnimeClip))))
		{
			string text = stateId switch
			{
				"dancing.moon_walk" => moonWalkAnimeClip, 
				"dancing.dance" => armRiseAnimeClip, 
				"dancing.point" => pointUpAnimeClip, 
				_ => string.Empty, 
			};
			if (!string.IsNullOrEmpty(text) && !(sprite.clip == text) && sprite.HasClip(text))
			{
				bool loop = stateId != "dancing.point";
				sprite.SetAnimation(text, loop);
			}
		}
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_moonWalkState, MoonWalkEntered, MoonWalkExited, MoonWalkProcessing);
			ConnectState(_danceState, DanceEntered, DanceExited, DanceProcessing);
			ConnectState(_pointState, PointEntered, PointExited, PointProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_moonWalkState, MoonWalkEntered, MoonWalkExited, MoonWalkProcessing);
			DisconnectState(_danceState, DanceEntered, DanceExited, DanceProcessing);
			DisconnectState(_pointState, PointEntered, PointExited, PointProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null && node.IsValid)
		{
			node.Entered += entered;
			node.Exited += exited;
			node.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null)
		{
			node.Entered -= entered;
			node.Exited -= exited;
			node.PhysicsProcessing -= processing;
		}
	}

	public void MoonWalkEntered()
	{
		if (CanDispatchDancingGameplay)
		{
			sprite.SetAnimation(moonWalkAnimeClip, loop: true, 0.2);
			sprite.Scale = new Vector2(moonWalkSpriteScaleX, sprite.Scale.Y);
			groundMoveComponent?.RefreshDirectionCache();
			groundMoveComponent?.SetAlive(true);
			Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
			if ((double)logicalGlobalPosition.X < _groundRight)
			{
				moonWalkMode = true;
				savePos = logicalGlobalPosition;
			}
			this.OnMoonWalkStarted?.Invoke();
		}
	}

	public void MoonWalkProcessing(double delta)
	{
		if (!CanDispatchDancingGameplay)
		{
			return;
		}
		sprite.timeScale = parent.timeScale * (double)moonWalkAnimeTimeScale;
		float num = _gridSizeX * moonWalkGridDistance;
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		if (moonWalkMode)
		{
			if (Mathf.Abs(logicalGlobalPosition.X - savePos.X) > num)
			{
				moonWalkOver = true;
				SendStateEvent(pointStateEvent);
				return;
			}
		}
		else if ((double)logicalGlobalPosition.X < _groundRight - (double)num)
		{
			moonWalkOver = true;
			SendStateEvent(pointStateEvent);
			return;
		}
		if (base.CanExecuteGameplay && attackComponent.CanAttack())
		{
			moonWalkOver = true;
			SendStateEvent(pointStateEvent);
		}
	}

	public void MoonWalkExited()
	{
		groundMoveComponent?.SetAlive(false);
		sprite.Scale = new Vector2(normalSpriteScaleX, sprite.Scale.Y);
		groundMoveComponent?.RefreshDirectionCache();
	}

	public void DanceEntered()
	{
		if (CanDispatchDancingGameplay)
		{
			danceTime = danceTimeInit;
			sprite.Scale = new Vector2(danceSpriteScaleX, sprite.Scale.Y);
			sprite.SetAnimation(armRiseAnimeClip, loop: true, 0.2);
		}
	}

	public void DanceProcessing(double delta)
	{
		if (CanDispatchDancingGameplay)
		{
			sprite.timeScale = parent.timeScale * (double)armRiseAnimeTimeScale;
			if (!sprite.pause && attackComponent.CanAttack())
			{
				parent.Attack();
			}
		}
	}

	public void DanceExited()
	{
		sprite.Scale = new Vector2(normalSpriteScaleX, sprite.Scale.Y);
	}

	public void PointEntered()
	{
		if (CanDispatchDancingGameplay)
		{
			isPointing = true;
			moonWalkOver = true;
			sprite.SetAnimation(pointUpAnimeClip, loop: false, 0.2);
			if (pointDownAnimeClip != "")
			{
				sprite.AddAnimation(pointDownAnimeClip, pointDownDelay, loop: false, 0.2);
			}
		}
	}

	public void PointProcessing(double delta)
	{
		if (CanDispatchDancingGameplay)
		{
			sprite.timeScale = parent.timeScale * (double)pointAnimeTimeScale;
		}
	}

	public void PointExited()
	{
		isPointing = false;
	}

	public void RequestPoint()
	{
		if (CanDispatchDancingGameplay)
		{
			SendStateEvent(pointStateEvent);
		}
	}

	public void RequestDance()
	{
		if (CanDispatchDancingGameplay)
		{
			SendStateEvent(danceStateEvent);
		}
	}

	public void RequestIdle()
	{
		if (CanDispatchDancingGameplay)
		{
			SendStateEvent(idleStateEvent);
		}
	}

	public bool OnWalk()
	{
		if (!CanDispatchDancingGameplay)
		{
			return false;
		}
		if (!moonWalkOver)
		{
			parent.Component();
			if (!SendStateEvent(moonWalkStateEvent))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public void OnWalkEntered()
	{
		sprite.Scale = new Vector2(normalSpriteScaleX, sprite.Scale.Y);
		if (syncDancerAnimation)
		{
			foreach (TowerDefenseCharacter dancer in dancerList)
			{
				if (!IsInstanceValid(dancer))
				{
					continue;
				}
				dancer.timeScale = parent.timeScale;
				if (dancer is TowerDefenseZombie towerDefenseZombie)
				{
					TowerDefenseZombie towerDefenseZombie2 = parent;
					if (towerDefenseZombie2 != null)
					{
						towerDefenseZombie.walkSpeedScale = towerDefenseZombie2.walkSpeedScale * (double)dancerWalkSpeedScaleMultiplier;
					}
				}
			}
		}
		walkTime = walkTimeInit;
	}

	public bool CanWalk()
	{
		if (!syncDancerAnimation)
		{
			return true;
		}
		foreach (TowerDefenseCharacter dancer in dancerList)
		{
			if (!CanDancerMoveWithJackson(dancer))
			{
				return false;
			}
		}
		return true;
	}

	private bool CanDancerMoveWithJackson(TowerDefenseCharacter dancer)
	{
		if (!IsInstanceValid(dancer))
		{
			return true;
		}
		if (dancer.die || dancer.nearDie || dancer.isRise)
		{
			return true;
		}
		if (!IsInstanceValid(dancer.sprite))
		{
			return true;
		}
		return dancer.sprite.clip == walkAnimeClip;
	}

	public void OnAttackProcessing(double delta)
	{
		groundMoveComponent?.SetAlive(false);
	}

	public void OnDieProcessing()
	{
		sprite.timeScale = parent.timeScale * (double)dieAnimeTimeScale;
	}

	public void OnHypnoses()
	{
		ReleaseDancerOwnership();
	}

	public void AnimeCompleted(string clip)
	{
		if (!CanDispatchDancingGameplay || parent.isShow || !parent.inGame || !base.CanExecuteGameplay)
		{
			return;
		}
		if (clip == pointUpAnimeClip)
		{
			if (!(pointDownAnimeClip == ""))
			{
				return;
			}
			isPointing = false;
			if (syncDancerAnimation)
			{
				foreach (TowerDefenseCharacter dancer in dancerList)
				{
					if (IsInstanceValid(dancer) && !dancer.die && !dancer.nearDie)
					{
						((TowerDefenseZombie)dancer).Walk();
					}
				}
			}
			parent.Walk();
			SendStateEvent(idleStateEvent);
		}
		else if (clip == pointDownAnimeClip)
		{
			isPointing = false;
			if (syncDancerAnimation)
			{
				foreach (TowerDefenseCharacter dancer2 in dancerList)
				{
					if (IsInstanceValid(dancer2) && !dancer2.die && !dancer2.nearDie)
					{
						((TowerDefenseZombie)dancer2).Walk();
					}
				}
			}
			parent.Walk();
			SendStateEvent(idleStateEvent);
		}
		else if (clip == walkAnimeClip)
		{
			if (clip != sprite.clip)
			{
				return;
			}
			walkTime--;
			if (!parent.die && !parent.nearDie)
			{
				if (walkTime > 0)
				{
					return;
				}
				if (CanSpawnDancer())
				{
					parent.Component();
					SendStateEvent(pointStateEvent);
					return;
				}
				if (syncDancerAnimation)
				{
					foreach (TowerDefenseCharacter dancer3 in dancerList)
					{
						if (IsInstanceValid(dancer3) && !dancer3.die && !dancer3.nearDie && dancer3.sprite.clip == walkAnimeClip)
						{
							dancer3.SendStateEvent(danceStateEvent);
						}
					}
				}
				parent.Component();
				SendStateEvent(danceStateEvent);
			}
			else
			{
				parent.Die();
			}
		}
		else
		{
			if (!(clip == armRiseAnimeClip) || clip != sprite.clip)
			{
				return;
			}
			if (armRiseFlipSprite)
			{
				sprite.Scale = new Vector2(0f - sprite.Scale.X, sprite.Scale.Y);
			}
			danceTime--;
			if (!parent.die && !parent.nearDie)
			{
				if (danceTime > 0)
				{
					return;
				}
				if (CanSpawnDancer())
				{
					parent.Component();
					SendStateEvent(pointStateEvent);
					return;
				}
				if (syncDancerAnimation)
				{
					foreach (TowerDefenseCharacter dancer4 in dancerList)
					{
						if (IsInstanceValid(dancer4) && !dancer4.die && !dancer4.nearDie && dancer4.sprite.clip == armRiseAnimeClip)
						{
							((TowerDefenseZombie)dancer4).Walk();
						}
					}
				}
				parent.Walk();
				SendStateEvent(idleStateEvent);
			}
			else
			{
				parent.Die();
			}
		}
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (!CanDispatchDancingGameplay || parent.isShow || !parent.inGame || !base.CanExecuteGameplay || !(command == "spawn") || parent.die || parent.nearDie)
		{
			return;
		}
		SpawnDancer();
		if (IsInstanceValid(spotlight))
		{
			spotlight.Visible = true;
		}
		if (IsInstanceValid(spotlight2))
		{
			spotlight2.Visible = true;
		}
		ChangeSpotlightColor();
		if (!firstSpawn)
		{
			firstSpawn = true;
			if (spotlightAudioName != "")
			{
				AudioManager.Instance.AudioPlay(spotlightAudioName);
			}
		}
	}

	private static bool IsDancerSlotOccupied(TowerDefenseCharacter dancer)
	{
		if (IsInstanceValid(dancer) && !dancer.die && !dancer.nearDie && !dancer.isDestroy)
		{
			return !dancer.IsQueuedForDeletion();
		}
		return false;
	}

	public bool CanSpawnDancer()
	{
		if (IsOwnerTerminal || !base.CanExecuteGameplay)
		{
			return false;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		if (parent.gridPos.Y > 1 && !IsDancerSlotOccupied(dancerList[0]))
		{
			return true;
		}
		if (parent.gridPos.Y < mapGridNum.Y && !IsDancerSlotOccupied(dancerList[1]))
		{
			return true;
		}
		if (!IsDancerSlotOccupied(dancerList[2]))
		{
			return true;
		}
		if (!IsDancerSlotOccupied(dancerList[3]))
		{
			return true;
		}
		return false;
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		int num = dancerList.IndexOf(dancer);
		if (num != -1)
		{
			SetNetworkDancer(num, null);
		}
	}

	public void SpawnDancer()
	{
		if (IsOwnerTerminal || !base.CanExecuteGameplay || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(dancerPacketName);
		if (IsInstanceValid(packetConfig) && IsInstanceValid(parent.instance) && IsInstanceValid(parent.transformPoint))
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			double hitpointScale = parent.instance.hitpointScale;
			Vector2 scale = parent.transformPoint.Scale;
			Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
			if (parent.gridPos.Y > 1 && !IsDancerSlotOccupied(dancerList[0]))
			{
				TowerDefenseCharacter dancer = SpawnSingleDancer(packetConfig, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(parent.gridPos.Y - 1)), parent.gridPos - new Vector2I(0, 1), hitpointScale, scale, 0);
				SetNetworkDancer(0, dancer);
			}
			if (parent.gridPos.Y < mapGridNum.Y && !IsDancerSlotOccupied(dancerList[1]))
			{
				TowerDefenseCharacter dancer2 = SpawnSingleDancer(packetConfig, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(parent.gridPos.Y + 1)), parent.gridPos + new Vector2I(0, 1), hitpointScale, scale, 1);
				SetNetworkDancer(1, dancer2);
			}
			if (!IsDancerSlotOccupied(dancerList[2]))
			{
				TowerDefenseCharacter dancer3 = SpawnSingleDancer(packetConfig, logicalGlobalPosition - new Vector2(_gridSizeX * 1.25f, 0f), parent.gridPos - new Vector2I(1, 0), hitpointScale, scale, 2);
				SetNetworkDancer(2, dancer3);
			}
			if (!IsDancerSlotOccupied(dancerList[3]))
			{
				TowerDefenseCharacter dancer4 = SpawnSingleDancer(packetConfig, logicalGlobalPosition + new Vector2(_gridSizeX * 1.25f, 0f), parent.gridPos + new Vector2I(1, 0), hitpointScale, scale, 3);
				SetNetworkDancer(3, dancer4);
			}
		}
	}

	private TowerDefenseCharacter SpawnSingleDancer(TowerDefensePacketConfig packetConfig, Vector2 pos, Vector2I gridPos, double hitpointScale, Vector2 scaleVal, int slot)
	{
		TowerDefenseCharacter dancer = (parent.EconomyOwnerAccountId.IsValid ? packetConfig.Create(parent.EconomyOwnerAccountId, pos, gridPos) : packetConfig.Create(pos, gridPos));
		if (!IsInstanceValid(dancer))
		{
			return null;
		}
		TowerDefenseGroundItemBase.characterNode.CallDeferred("add_child", dancer);
		dancer.CallDeferred("SetHitpointAndScale", hitpointScale, scaleVal);
		dancer.SetDeferred("invisible", parent.invisible);
		if (parent.instance.hypnoses)
		{
			Callable.From(delegate
			{
				dancer.Hypnoses();
			}).CallDeferred();
		}
		Callable.From(delegate
		{
			if (IsInstanceValid(dancer) && IsInstanceValid(packetConfig?._override?.characterOverride))
			{
				packetConfig._override.characterOverride.ExecuteCharacter(dancer);
			}
		}).CallDeferred();
		Dictionary spawnState = new Dictionary
		{
			["spawn_invisible"] = parent.invisible,
			["dancer_parent_sync_id"] = parent.syncId,
			["dancer_slot"] = slot
		};
		TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer, useCreate: true, dancerRiseDuration, walkAfterSpawn: false, "", spawnState);
		Callable.From(delegate
		{
			if (IsInstanceValid(dancer))
			{
				dancer.Rise(dancerRiseDuration);
			}
		}).CallDeferred();
		return dancer;
	}

	public TowerDefenseCharacter GetDancer(int slot)
	{
		if (slot < 0 || slot >= dancerList.Count)
		{
			return null;
		}
		return dancerList[slot];
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot >= 0 && slot < 4)
		{
			while (dancerList.Count < 4)
			{
				dancerList.Add(null);
			}
			TowerDefenseCharacter towerDefenseCharacter = dancerList[slot];
			if (IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != dancer && towerDefenseCharacter is IDancer dancer2)
			{
				dancer2.SetJackson(null);
			}
			dancerList[slot] = dancer;
			if (IsInstanceValid(dancer) && dancer is IDancer dancer3)
			{
				dancer3.SetJackson(parent);
			}
			_pendingDancerSyncIds[slot] = -1;
			_pendingDancerNodeNames[slot] = "";
			if (!HasPendingDancerRelations)
			{
				_pendingDancerResolveRemaining = 0.0;
			}
			RefreshPendingDancerPhysicsRegistration();
		}
	}

	private void RefreshPendingDancerPhysicsRegistration()
	{
		bool hasPendingDancerRelations = HasPendingDancerRelations;
		if (_pendingDancerPhysicsRegistered != hasPendingDancerRelations)
		{
			_pendingDancerPhysicsRegistered = hasPendingDancerRelations;
			RefreshPhysicsProcessEligibility();
		}
	}

	private void ResolvePendingDancerRelations()
	{
		if (!IsInstanceValid(parent))
		{
			_pendingDancerResolveRemaining = 0.25;
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		for (int i = 0; i < 4; i++)
		{
			int num = _pendingDancerSyncIds[i];
			if (num >= 0 && IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && IsInstanceValid(value))
			{
				SetNetworkDancer(i, value);
				continue;
			}
			string text = _pendingDancerNodeNames[i];
			if (!(text == "") && IsInstanceValid(characterNode))
			{
				TowerDefenseCharacter nodeOrNull = characterNode.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
				if (IsInstanceValid(nodeOrNull))
				{
					SetNetworkDancer(i, nodeOrNull);
				}
			}
		}
		_pendingDancerResolveRemaining = (HasPendingDancerRelations ? 0.25 : 0.0);
	}

	public void HideSpotlights()
	{
		StopSpotlightColorCycle();
		if (IsInstanceValid(spotlight))
		{
			spotlight.Visible = false;
		}
		if (IsInstanceValid(spotlight2))
		{
			spotlight2.Visible = false;
		}
	}

	public void ChangeSpotlightColor()
	{
		StopSpotlightColorCycle();
		if (!IsInstanceValid(spotlight) || !IsInstanceValid(spotlight2) || !IsInstanceValid(spotlightGrandient))
		{
			return;
		}
		Color modulate = spotlightGrandient.Sample(GD.Randf());
		spotlight.Modulate = modulate;
		spotlight2.Modulate = modulate;
		if (spotlightColorCycleEnabled && !(spotlightChangeInterval <= 0f))
		{
			SceneTree sceneTree = parent?.GetTree();
			if (IsInstanceValid(sceneTree))
			{
				_spotlightTimer = sceneTree.CreateTimer(spotlightChangeInterval, processAlways: false);
				_spotlightTimerHandler = ChangeSpotlightColor;
				_spotlightTimer.Timeout += _spotlightTimerHandler;
			}
		}
	}

	private void StopSpotlightColorCycle()
	{
		if (IsInstanceValid(_spotlightTimer) && _spotlightTimerHandler != null)
		{
			_spotlightTimer.Timeout -= _spotlightTimerHandler;
		}
		_spotlightTimer = null;
		_spotlightTimerHandler = null;
	}

	public override Dictionary ExportComponentSave()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>
		{
			{ "moonWalkOver", moonWalkOver },
			{ "walkTime", walkTime },
			{ "danceTime", danceTime },
			{ "firstSpawn", firstSpawn },
			{ "isPointing", isPointing },
			{ "moonWalkMode", moonWalkMode },
			{ "savePosX", savePos.X },
			{ "savePosY", savePos.Y }
		};
		List<string> list = new List<string>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			if (IsInstanceValid(dancer))
			{
				list.Add(dancer.Name.ToString());
			}
			else
			{
				list.Add(_pendingDancerNodeNames[i]);
			}
		}
		dictionary["dancerNodeNames"] = new Godot.Collections.Array(((IEnumerable<string>)list).Select((Func<string, Variant>)((string n) => n)));
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		Dictionary dictionary2 = new Dictionary();
		foreach (KeyValuePair<string, Variant> item in dictionary)
		{
			dictionary2[item.Key] = item.Value;
		}
		return dictionary2;
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		moonWalkOver = _data.GetValueOrDefault("moonWalkOver", false).AsBool();
		walkTime = _data.GetValueOrDefault("walkTime", walkTimeInit).AsInt32();
		danceTime = _data.GetValueOrDefault("danceTime", danceTimeInit).AsInt32();
		firstSpawn = _data.GetValueOrDefault("firstSpawn", false).AsBool();
		isPointing = _data.GetValueOrDefault("isPointing", false).AsBool();
		moonWalkMode = _data.GetValueOrDefault("moonWalkMode", moonWalkMode).AsBool();
		savePos = new Vector2(_data.GetValueOrDefault("savePosX", savePos.X).AsSingle(), _data.GetValueOrDefault("savePosY", savePos.Y).AsSingle());
		string text = _data.GetValueOrDefault("state", "").AsString();
		if (base.ShouldApplyLegacyStateField && !string.IsNullOrEmpty(text))
		{
			StateHandle stateHandle = base.StateMachine?.ResolveState(text, allowDisplayNameFallback: true);
			bool alive = ((stateHandle != null && stateHandle.IsValid) ? (stateHandle.StableId == "dancing.moon_walk") : (text == "MoonWalk" || text == "dancing.moon_walk"));
			_pendingSyncedState = text;
			ApplyPendingSyncedState();
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(alive);
			}
		}
		if (!_data.ContainsKey("dancerNodeNames"))
		{
			return;
		}
		Godot.Collections.Array array = _data["dancerNodeNames"].AsGodotArray();
		List<string> dancerNodeNames = new List<string>();
		foreach (Variant item in array)
		{
			dancerNodeNames.Add(item.AsString());
		}
		Callable.From(delegate
		{
			RestoreDancerReferences(dancerNodeNames, _owner);
		}).CallDeferred();
	}

	private void RestoreDancerReferences(List<string> dancerNodeNames, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		if (!IsInstanceValid(parent))
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			string text = ((i < dancerNodeNames.Count) ? dancerNodeNames[i] : "");
			SetNetworkDancer(i, null);
			if (text == "")
			{
				continue;
			}
			_pendingDancerNodeNames[i] = text;
			if (_owner != null)
			{
				StringName key = new StringName(text);
				if (_owner.charcterDicionary.TryGetValue(key, out var value) && IsInstanceValid(value))
				{
					SetNetworkDancer(i, value);
				}
			}
		}
		_pendingDancerResolveRemaining = 0.0;
		RefreshPendingDancerPhysicsRegistration();
		ResolvePendingDancerRelations();
	}

	public override Dictionary SyncSerialize()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>
		{
			{ "moonWalkOver", moonWalkOver },
			{ "walkTime", walkTime },
			{ "danceTime", danceTime },
			{ "firstSpawn", firstSpawn },
			{ "isPointing", isPointing },
			{ "moonWalkMode", moonWalkMode },
			{ "savePosX", savePos.X },
			{ "savePosY", savePos.Y }
		};
		List<int> list = new List<int>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			if (IsInstanceValid(dancer))
			{
				list.Add(dancer.syncId);
			}
			else
			{
				list.Add(_pendingDancerSyncIds[i]);
			}
		}
		dictionary["dancerSyncIds"] = new Godot.Collections.Array(((IEnumerable<int>)list).Select((Func<int, Variant>)((int n) => n)));
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		Dictionary dictionary2 = new Dictionary();
		foreach (KeyValuePair<string, Variant> item in dictionary)
		{
			dictionary2[item.Key] = item.Value;
		}
		return dictionary2;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		moonWalkOver = data.GetValueOrDefault("moonWalkOver", moonWalkOver).AsBool();
		walkTime = data.GetValueOrDefault("walkTime", walkTimeInit).AsInt32();
		danceTime = data.GetValueOrDefault("danceTime", danceTimeInit).AsInt32();
		firstSpawn = data.GetValueOrDefault("firstSpawn", firstSpawn).AsBool();
		isPointing = data.GetValueOrDefault("isPointing", isPointing).AsBool();
		moonWalkMode = data.GetValueOrDefault("moonWalkMode", moonWalkMode).AsBool();
		savePos = new Vector2(data.GetValueOrDefault("savePosX", savePos.X).AsSingle(), data.GetValueOrDefault("savePosY", savePos.Y).AsSingle());
		if (data.ContainsKey("dancerSyncIds"))
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			Godot.Collections.Array array = data["dancerSyncIds"].AsGodotArray();
			for (int i = 0; i < 4; i++)
			{
				int num = ((i < array.Count) ? array[i].AsInt32() : (-1));
				if (num < 0)
				{
					SetNetworkDancer(i, null);
					continue;
				}
				if (IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && IsInstanceValid(value))
				{
					SetNetworkDancer(i, value);
					continue;
				}
				SetNetworkDancer(i, null);
				_pendingDancerSyncIds[i] = num;
				_pendingDancerNodeNames[i] = "";
				_pendingDancerResolveRemaining = 0.25;
				RefreshPendingDancerPhysicsRegistration();
			}
		}
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && IsInstanceValid(parent) && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingSyncedState))
			{
				_pendingSyncedState = null;
			}
		}
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
