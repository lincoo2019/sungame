using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombie : TowerDefenseCharacter, IStateMachinePhysicsFastCallbackTarget
{
	private static readonly StringName DieStateEvent = "ToDie";

	private const string DieStateId = "zombie.die";

	private const int GarlicPhysicsCallbackId = 1;

	private const int WalkPhysicsCallbackId = 2;

	private const int AttackPhysicsCallbackId = 3;

	private const int DiePhysicsCallbackId = 4;

	public WaterInteractionComponent waterInteractionComponent;

	public GroundMoveComponent groundMoveComponent;

	public AttackComponent attackComponent;

	public GarlicComponent garlicComponent;

	public SwimComponent swimComponent;

	public ZombieDeathComponent zombieDeathComponent;

	private Array<string> _garlicFliters;

	public static bool UseBatch = true;

	private StateHandle _garlicStateHandle;

	private StateHandle _walkStateHandle;

	private StateHandle _attackStateHandle;

	private StateHandle _dieStateHandle;

	private List<StringName> _waterLineVisualLayers;

	private bool _zombieStateHandlesConnected;

	private BattleEventBus.ShowZombieHealthEventHandler _showZombieHealthHandler;

	private BattleEventBus.ShowBossHealthBarEventHandler _showBossHealthBarHandler;

	private BattleEventBus _showZombieHealthEventBus;

	private bool _showZombieHealthSubscribed;

	private ulong _walkMovementActivationVersion;

	private bool _walkWithoutTransitionDelay;

	private ulong _walkReadyGeneration;

	private ulong _groanPlaybackGeneration;

	private bool _isPause;

	public bool isGarlic;

	public bool isGarlicBird;

	public bool isChangeLine;

	public bool inSwimPlay;

	public bool inGround;

	public bool startAttack;

	public int sizeUpNum = 2;

	public bool hasGhost;

	public TowerDefenseZombie ghostCharacter;

	public bool hasSpikeball;

	public TowerDefenseZombie riderCarryOwner;

	public BobsledTeamComponent bobsledTeamOwner;

	public bool spritePause;

	public Vector2 _gridSize;

	private bool _validatedBatchDispatchActive;

	private ulong _validatedBatchPhysicsFrame;

	private bool _ensuringZeroHealthDeath;

	[Export(PropertyHint.None, "")]
	public string groan { get; set; }

	[Export(PropertyHint.None, "")]
	public double walkSpeedScale { get; set; } = 1.0;

	[Export(PropertyHint.None, "")]
	public double inSwimAnimeClipScale { get; set; } = 1.0;

	[Export(PropertyHint.None, "")]
	public bool useAttackDps { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string walkAnimeClip { get; set; } = "Walk";

	[Export(PropertyHint.None, "")]
	public string inSwimAnimeClip { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public string swimAnimeClip { get; set; } = "Walk";

	[Export(PropertyHint.None, "")]
	public string outSwimAnimeClip { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public string attackAnimeClip { get; set; } = "Eat";

	[Export(PropertyHint.None, "")]
	public string attackWaterAnimeClip { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public string dieAnimeClip { get; set; } = "Death";

	[Export(PropertyHint.None, "")]
	public string dieWaterAnimeClip { get; set; } = "Death";

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite duckytobeSprite { get; set; }

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite waterLineSprite { get; set; }

	[Export(PropertyHint.MultilineText, "")]
	public string waterAnimeFliter { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Array<string> garlicFliters
	{
		get
		{
			Array<string> array = _garlicFliters;
			if (array == null)
			{
				Array<string> obj = new Array<string> { "anim_head2", "anim_tongue" };
				Array<string> array2 = obj;
				_garlicFliters = obj;
				array = array2;
			}
			return array;
		}
		set
		{
			_garlicFliters = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public string garlicReplace { get; set; } = "Zombie_head.png";

	[Export(PropertyHint.None, "")]
	public double waterHeight { get; set; } = 25.0;

	internal bool IsBatchDispatchActive
	{
		get
		{
			if (base.IsOwnerBatchRegistered)
			{
				return !_isPause;
			}
			return false;
		}
	}

	protected StateHandle WalkStateHandle => _walkStateHandle;

	protected StateHandle AttackStateHandle => _attackStateHandle;

	public bool isPause
	{
		get
		{
			return _isPause;
		}
		set
		{
			_isPause = value;
			if (!IsNodeReady())
			{
				return;
			}
			SetMainStateMachineDispatchEnabled(!_isPause);
			if (!GodotObject.IsInstanceValid(base.sprite))
			{
				return;
			}
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine == null || !stateMachine.IsInitialized || !base.HasHitBox)
			{
				return;
			}
			GroundHeightComponent obj = groundHeightComponent;
			if (obj == null || obj.IsReleased)
			{
				return;
			}
			if (_isPause)
			{
				base.sprite.pause = true;
				SetHitBoxSuppressed(HitBoxSuppressionReason.Paused, suppressed: true);
				if (base.IsOwnerBatchRegistered)
				{
					TowerDefenseZombieBatch.Unregister(this);
				}
				SetPhysicsProcess(enable: false);
				groundHeightComponent.SetAlive(alive: false);
			}
			else
			{
				base.sprite.pause = false;
				SetHitBoxSuppressed(HitBoxSuppressionReason.Paused, suppressed: false);
				ConfigureBatchProcessing();
				groundHeightComponent.SetAlive(alive: true);
			}
		}
	}

	public override void OnRiseStart()
	{
		Idle();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.alive = false;
		}
	}

	public override void OnRiseEnd()
	{
		Walk();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.alive = true;
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		if (!TowerDefenseCharacter.CachedEditorHint && IsNodeReady() && inGame)
		{
			SubscribeShowZombieHealth();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (editorPreviewMode)
		{
			SetPhysicsProcess(enable: false);
		}
		else
		{
			if (Engine.IsEditorHint() || !base.HasValidRuntimeConfiguration)
			{
				return;
			}
			EnsureShowZombieHealthHandler();
			if (GodotObject.IsInstanceValid(componentManager))
			{
				waterInteractionComponent = componentManager.GetRuntime<WaterInteractionComponent>();
				groundMoveComponent = componentManager.GetRuntime<GroundMoveComponent>();
				zombieDeathComponent = componentManager.GetRuntime<ZombieDeathComponent>();
				garlicComponent = componentManager.GetRuntime<GarlicComponent>();
				swimComponent = componentManager.GetRuntime<SwimComponent>();
				attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
				WaterInteractionComponent obj = waterInteractionComponent;
				if (obj != null && !obj.IsReleased)
				{
					waterInteractionComponent.shadowComponent = shadowComponent;
					if (GodotObject.IsInstanceValid(waterLineSprite))
					{
						waterInteractionComponent.waterLineSprite = waterLineSprite as AdobeAnimateSpriteBase;
					}
					if (GodotObject.IsInstanceValid(duckytobeSprite))
					{
						waterInteractionComponent.duckytobeSprite = duckytobeSprite as AdobeAnimateSpriteBase;
					}
				}
				GroundHeightComponent obj2 = groundHeightComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					WaterInteractionComponent obj3 = waterInteractionComponent;
					if (obj3 != null && !obj3.IsReleased)
					{
						groundHeightComponent.waterInteractionComponent = waterInteractionComponent;
					}
				}
			}
			if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
			{
				timeScale += GD.RandRange(-0.1, 0.1);
			}
			AddToGroup("Zombie", persistent: true);
			if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
			{
				walkSpeedScale += walkSpeedScale * GD.RandRange(-0.1, 0.1);
			}
			instance.hitpointsEmpty += Die;
			RefreshWaterLineState();
			if (GodotObject.IsInstanceValid(cell) && cell.isWater && (instance.maskFlags & 2) == 0)
			{
				base.inWater = true;
			}
			_gridSize = TowerDefenseManager.Instance.GetMapGridSize();
			ShowHealthComponent obj4 = showHealthComponent;
			if (obj4 != null && !obj4.IsReleased)
			{
				RefreshShowHealthVisibility();
			}
			if (inGame)
			{
				SubscribeShowZombieHealth();
			}
			if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
			{
				AttackComponent obj5 = attackComponent;
				if (obj5 != null && !obj5.IsReleased)
				{
					attackComponent.alive = false;
					attackComponent.SetAlive(alive: false);
				}
			}
			if (inGame && Global.IsMultiplayerMode && MultiPlayerManager.IsHost && syncId < 0)
			{
				CallDeferred(MethodName.AutoRegisterSync);
			}
			ConnectZombieStateHandles();
			ConfigureBatchProcessing();
		}
	}

	private void ConnectZombieStateHandles()
	{
		if (_zombieStateHandlesConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_garlicStateHandle = base.StateMachine?.GetStateById("zombie.garlic");
		_walkStateHandle = base.StateMachine?.GetStateById("zombie.walk");
		_attackStateHandle = base.StateMachine?.GetStateById("zombie.attack");
		_dieStateHandle = base.StateMachine?.GetStateById("zombie.die");
		if (_garlicStateHandle != null)
		{
			_garlicStateHandle.Entered += GarlicEntered;
			_garlicStateHandle.Exited += GarlicExited;
			if (!_garlicStateHandle.TrySetPhysicsFastCallback(this, 1))
			{
				_garlicStateHandle.PhysicsProcessing += GarlicProcessing;
			}
		}
		if (_walkStateHandle != null)
		{
			_walkStateHandle.Entered += WalkEntered;
			_walkStateHandle.Exited += WalkExited;
			if (!_walkStateHandle.TrySetPhysicsFastCallback(this, 2))
			{
				_walkStateHandle.PhysicsProcessing += WalkProcessing;
			}
		}
		if (_attackStateHandle != null)
		{
			_attackStateHandle.Entered += AttackEntered;
			_attackStateHandle.Exited += AttackExited;
			if (!_attackStateHandle.TrySetPhysicsFastCallback(this, 3))
			{
				_attackStateHandle.PhysicsProcessing += AttackProcessing;
			}
		}
		if (_dieStateHandle != null)
		{
			_dieStateHandle.Entered += DieEntered;
			if (!_dieStateHandle.TrySetPhysicsFastCallback(this, 4))
			{
				_dieStateHandle.PhysicsProcessing += DieProcessing;
			}
		}
		_zombieStateHandlesConnected = true;
	}

	private void DisconnectZombieStateHandles()
	{
		if (!_zombieStateHandlesConnected)
		{
			return;
		}
		if (_garlicStateHandle != null)
		{
			_garlicStateHandle.Entered -= GarlicEntered;
			_garlicStateHandle.Exited -= GarlicExited;
			if (!_garlicStateHandle.ClearPhysicsFastCallback(this))
			{
				_garlicStateHandle.PhysicsProcessing -= GarlicProcessing;
			}
		}
		if (_walkStateHandle != null)
		{
			_walkStateHandle.Entered -= WalkEntered;
			_walkStateHandle.Exited -= WalkExited;
			if (!_walkStateHandle.ClearPhysicsFastCallback(this))
			{
				_walkStateHandle.PhysicsProcessing -= WalkProcessing;
			}
		}
		if (_attackStateHandle != null)
		{
			_attackStateHandle.Entered -= AttackEntered;
			_attackStateHandle.Exited -= AttackExited;
			if (!_attackStateHandle.ClearPhysicsFastCallback(this))
			{
				_attackStateHandle.PhysicsProcessing -= AttackProcessing;
			}
		}
		if (_dieStateHandle != null)
		{
			_dieStateHandle.Entered -= DieEntered;
			if (!_dieStateHandle.ClearPhysicsFastCallback(this))
			{
				_dieStateHandle.PhysicsProcessing -= DieProcessing;
			}
		}
		_garlicStateHandle = null;
		_walkStateHandle = null;
		_attackStateHandle = null;
		_dieStateHandle = null;
		_zombieStateHandlesConnected = false;
	}

	void IStateMachinePhysicsFastCallbackTarget.InvokeStateMachinePhysicsFastCallback(int callbackId, double delta)
	{
		switch (callbackId)
		{
		case 1:
			GarlicProcessing(delta);
			break;
		case 2:
			WalkProcessing(delta);
			break;
		case 3:
			AttackProcessing(delta);
			break;
		case 4:
			DieProcessing(delta);
			break;
		}
	}

	private void AutoRegisterSync()
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || syncId >= 0 || !GodotObject.IsInstanceValid(this) || isDestroy || isShow)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (GodotObject.IsInstanceValid(currentControl))
		{
			int nextSyncId = currentControl.GetNextSyncId();
			currentControl.RegisterSyncCharacter(nextSyncId, this);
			if (GodotObject.IsInstanceValid(base.config))
			{
				MultiPlayerManager.Instance.SendSpawnCharacterAt(base.config.name, base.gridPos.X, base.gridPos.Y, nextSyncId, GodotObject.IsInstanceValid(instance) ? instance.hitpointScale : 1.0, transformPoint.Scale.X, GodotObject.IsInstanceValid(instance) && instance.hypnoses, 0.0, useCreate: true, GetLogicalGlobalPosition().X, GetLogicalGlobalPosition().Y);
			}
		}
	}

	public override void _ExitTree()
	{
		_walkReadyGeneration++;
		_groanPlaybackGeneration++;
		startAttack = false;
		base._ExitTree();
		if (!Engine.IsEditorHint())
		{
			if (base.IsOwnerBatchRegistered)
			{
				TowerDefenseZombieBatch.Unregister(this);
			}
			UnsubscribeShowZombieHealth();
		}
	}

	private void EnsureShowZombieHealthHandler()
	{
		if (_showZombieHealthHandler == null)
		{
			_showZombieHealthHandler = OnShowZombieHealth;
		}
		if (_showBossHealthBarHandler == null)
		{
			_showBossHealthBarHandler = OnShowBossHealthBar;
		}
	}

	private void SubscribeShowZombieHealth()
	{
		if (!_showZombieHealthSubscribed || !GodotObject.IsInstanceValid(_showZombieHealthEventBus))
		{
			_showZombieHealthSubscribed = false;
			_showZombieHealthEventBus = null;
			BattleEventBus battleEventBus = BattleEventBus.Instance;
			if (GodotObject.IsInstanceValid(battleEventBus))
			{
				EnsureShowZombieHealthHandler();
				battleEventBus.OnShowZombieHealth += _showZombieHealthHandler;
				battleEventBus.OnShowBossHealthBar += _showBossHealthBarHandler;
				_showZombieHealthEventBus = battleEventBus;
				_showZombieHealthSubscribed = true;
			}
		}
	}

	private void UnsubscribeShowZombieHealth()
	{
		if (_showZombieHealthSubscribed && GodotObject.IsInstanceValid(_showZombieHealthEventBus) && _showZombieHealthHandler != null)
		{
			_showZombieHealthEventBus.OnShowZombieHealth -= _showZombieHealthHandler;
			_showZombieHealthEventBus.OnShowBossHealthBar -= _showBossHealthBarHandler;
		}
		_showZombieHealthSubscribed = false;
		_showZombieHealthEventBus = null;
	}

	private void OnShowZombieHealth(bool show)
	{
		RefreshShowHealthVisibility(show);
	}

	private void OnShowBossHealthBar(bool show)
	{
		bool? showBossHealthBar = show;
		RefreshShowHealthVisibility(null, showBossHealthBar);
	}

	private void RefreshShowHealthVisibility(bool? showZombieHealth = null, bool? showBossHealthBar = null)
	{
		ShowHealthComponent obj = showHealthComponent;
		if (obj != null && !obj.IsReleased)
		{
			bool flag = showZombieHealth ?? GameSaveManager.Instance.GetConfigValue("ShowZombieHealth").AsBool();
			bool flag2 = showBossHealthBar ?? GameSaveManager.Instance.GetConfigValue("ShowBossHealthBar").AsBool();
			bool flag3 = GodotObject.IsInstanceValid(instance) && instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && flag2;
			showHealthComponent.SetAlive(flag && !flag3);
		}
	}

	public override void ActivateGameplayProcessing()
	{
		base.ActivateGameplayProcessing();
		ConfigureBatchProcessing();
		groundHeightComponent?.SetAlive(alive: true);
		ReactivateAttackComponent(attackComponent);
	}

	protected void ConfigureWaterLineVisualLayers(params string[] layerNames)
	{
		if (layerNames == null)
		{
			return;
		}
		foreach (string text in layerNames)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				StringName item = new StringName(text);
				List<StringName> list = _waterLineVisualLayers ?? (_waterLineVisualLayers = new List<StringName>());
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		RefreshWaterLineVisualLayers();
	}

	private void RefreshWaterLineVisualLayers()
	{
		bool waterLine = TowerDefenseManager.MapLineHasType(base.gridPos.Y, TowerDefenseEnum.PLANTGRIDTYPE.WATER);
		RefreshWaterLineVisualLayers(waterLine);
	}

	private void RefreshWaterLineState()
	{
		bool flag = TowerDefenseManager.MapLineHasType(base.gridPos.Y, TowerDefenseEnum.PLANTGRIDTYPE.WATER);
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.inWaterLine = flag;
		}
		if (GodotObject.IsInstanceValid(duckytobeSprite))
		{
			duckytobeSprite.Visible = flag;
		}
		RefreshWaterLineVisualLayers(flag);
	}

	private void RefreshWaterLineVisualLayers(bool waterLine)
	{
		if (!GodotObject.IsInstanceValid(base.sprite) || _waterLineVisualLayers == null)
		{
			return;
		}
		foreach (StringName waterLineVisualLayer in _waterLineVisualLayers)
		{
			base.sprite.SetFliter(waterLineVisualLayer, waterLine);
		}
	}

	private static void ReactivateAttackComponent(AttackComponent component)
	{
		if (component != null && !component.IsReleased && component.alive)
		{
			component.alive = true;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!base.IsOwnerBatchRegistered)
		{
			BatchUpdate(delta);
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!TowerDefenseCharacter.CachedEditorHint && !_isPause)
		{
			if (_validatedBatchDispatchActive)
			{
				BatchUpdateCore(delta, _validatedBatchPhysicsFrame);
				return;
			}
			TowerDefenseProcessModeDispatch.BeginFrame();
			BatchUpdateCore(delta, TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		}
	}

	internal void BatchUpdateValidated(double delta, ulong physicsFrame)
	{
		if (_validatedBatchDispatchActive)
		{
			BatchUpdateCore(delta, physicsFrame);
			return;
		}
		_validatedBatchDispatchActive = true;
		_validatedBatchPhysicsFrame = physicsFrame;
		try
		{
			BatchUpdate(delta);
		}
		finally
		{
			_validatedBatchDispatchActive = false;
		}
	}

	private void BatchUpdateCore(double delta, ulong physicsFrame)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		if (EnsureZeroHealthDeath() && base.CurrentStateHandle?.StableId != "zombie.die")
		{
			SendStateEvent(DieStateEvent);
		}
		TowerDefensePerfProfiler.End("batch.zombie.preBase", startTicks);
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		PhysicsProcessWithFrame(delta, physicsFrame);
		TowerDefensePerfProfiler.End("batch.zombie.characterBase", startTicks2);
		long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
		if (!GodotObject.IsInstanceValid(base.sprite))
		{
			TowerDefensePerfProfiler.End("batch.zombie.postBase", startTicks3);
			return;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (towerDefenseManager == null)
		{
			TowerDefensePerfProfiler.End("batch.zombie.postBase", startTicks3);
			return;
		}
		base.sprite.pause = towerDefenseManager.pauseZombie || spritePause;
		base.sprite.playBack = towerDefenseManager.backZombie || isGarlicBird;
		Vector2 vector = default(Vector2);
		bool flag = false;
		if (TowerDefenseProcessModeDispatch.HasMapFeatureForCurrentPhysicsFrame)
		{
			if (GodotObject.IsInstanceValid(cell))
			{
				Vector2 mapCellPos = towerDefenseManager.GetMapCellPos(base.gridPos);
				vector = GetGlobalPositionForPhysicsFrame(physicsFrame);
				flag = true;
				cellPercentage = (vector - mapCellPos).X / _gridSize.X;
			}
			else
			{
				base.inWater = false;
			}
		}
		if ((ulong)((long)physicsFrame + (long)randFreshIndex) % 30uL == 0L)
		{
			if (!flag)
			{
				vector = GetGlobalPositionForPhysicsFrame(physicsFrame);
			}
			if (!inGround && (double)vector.X < groundRight + 150.0)
			{
				inGround = true;
				if (inGame)
				{
					PlayGroanDelayed(++_groanPlaybackGeneration);
				}
			}
			float cachedLocalScaleX = base.CachedLocalScaleX;
			if (cachedLocalScaleX < 0f && (double)vector.X > groundRight + 150.0 && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
			{
				Destroy();
			}
			if (cachedLocalScaleX > 0f)
			{
				TowerDefenseProcessModeDispatch.TryGetComponentBattlefieldBoundsForCurrentPhysicsFrame(out var left, out var _);
				if (vector.X < left - 150f && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
				{
					Destroy();
				}
			}
			if (base.inWater)
			{
				InWaterDiscardSet();
			}
			else
			{
				OutWaterDiscardSet();
			}
		}
		TowerDefensePerfProfiler.End("batch.zombie.postBase", startTicks3);
	}

	private void ConfigureBatchProcessing()
	{
		if (!IsNodeReady() || Engine.IsEditorHint())
		{
			return;
		}
		if (!inGame)
		{
			if (base.IsOwnerBatchRegistered)
			{
				TowerDefenseZombieBatch.Unregister(this);
			}
			SetPhysicsProcess(enable: false);
		}
		else if (_isPause)
		{
			if (base.StateMachine != null)
			{
				SetMainStateMachineDispatchEnabled(enabled: false);
			}
			if (base.IsOwnerBatchRegistered)
			{
				TowerDefenseZombieBatch.Unregister(this);
			}
			SetPhysicsProcess(enable: false);
		}
		else if (UseBatch)
		{
			if (!TowerDefenseZombieBatch.Register(this))
			{
				SetOwnerBatchRegistration(registered: false);
			}
		}
		else if (base.IsOwnerBatchRegistered)
		{
			TowerDefenseZombieBatch.Unregister(this);
		}
		else
		{
			SetOwnerBatchRegistration(registered: false);
		}
	}

	private async Task PlayGroanDelayed(ulong playbackGeneration)
	{
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0.5, 3.0), processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (playbackGeneration == _groanPlaybackGeneration && GodotObject.IsInstanceValid(this) && IsInsideTree() && inGame && !die && !nearDie && !isDestroy)
		{
			AudioManager.Instance.AudioPlay(groan);
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (isRise || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return;
		}
		base.sprite.timeScale = timeScale;
		if (!IsZombieRewindActive() && CanStartAttackFromCurrentPosition())
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased && attackComponent.CanAttack())
			{
				Attack();
			}
		}
	}

	protected static bool IsZombieRewindActive()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return towerDefenseManager.backZombie;
		}
		return false;
	}

	private bool CanStartAttackFromCurrentPosition()
	{
		if (inGame && !base.HasComponentGameplayUntilBattlefieldEntry && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && base.IsInsideComponentBattlefieldForRuntime)
		{
			return true;
		}
		if ((double)GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame).X > groundRight)
		{
			return false;
		}
		return true;
	}

	public void GarlicEntered()
	{
	}

	public void GarlicProcessing(double delta)
	{
		base.sprite.timeScale = 0.0;
	}

	public void GarlicExited()
	{
	}

	public virtual void WalkEntered()
	{
		bool walkWithoutTransitionDelay = _walkWithoutTransitionDelay;
		SwimComponent obj = swimComponent;
		if (obj != null && !obj.IsReleased)
		{
			try
			{
				swimComponent.WalkEntered(walkWithoutTransitionDelay ? new double?(0.0) : ((double?)null));
			}
			catch (Exception ex)
			{
				GD.PrintErr("WalkEntered: SwimComponent.WalkEntered() threw: " + ex.Message);
				base.sprite.SetAnimation(walkAnimeClip, loop: true, walkWithoutTransitionDelay ? 0.0 : 0.2);
			}
		}
		else
		{
			base.sprite.SetAnimation(walkAnimeClip, loop: true, walkWithoutTransitionDelay ? 0.0 : 0.2);
		}
		_walkMovementActivationVersion++;
		if (walkWithoutTransitionDelay)
		{
			ActivateWalkMovement(_walkMovementActivationVersion);
		}
		else
		{
			WalkEnteredDelay();
		}
	}

	private async Task WalkEnteredDelay()
	{
		ulong activationVersion = _walkMovementActivationVersion;
		if (GodotObject.IsInstanceValid(GetTree()))
		{
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		ActivateWalkMovement(activationVersion);
	}

	private void ActivateWalkMovement(ulong activationVersion)
	{
		if (activationVersion == _walkMovementActivationVersion && !die && !isDestroy && inGame)
		{
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(groundMoveComponent.HasMovementSource);
			}
		}
	}

	protected void WalkWithoutTransitionDelay()
	{
		_walkWithoutTransitionDelay = true;
		try
		{
			Walk();
		}
		finally
		{
			_walkWithoutTransitionDelay = false;
		}
	}

	public virtual void WalkProcessing(double delta)
	{
		SwimComponent swimComponent = this.swimComponent;
		if (swimComponent != null && !swimComponent.IsReleased)
		{
			AdobeAnimateSprite adobeAnimateSprite = base.sprite;
			if (_validatedBatchDispatchActive && adobeAnimateSprite != null)
			{
				swimComponent.WalkProcessingValidated(this, adobeAnimateSprite, _validatedBatchPhysicsFrame);
			}
			else
			{
				swimComponent.WalkProcessing((float)delta);
			}
		}
		if ((!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && !base.sprite.pause && !IsZombieRewindActive() && CanStartAttackFromCurrentPosition())
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased && attackComponent.CanAttackFromZombieWalk())
			{
				Attack();
			}
		}
	}

	public virtual void WalkExited()
	{
		_walkMovementActivationVersion++;
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(false);
		}
	}

	public virtual void AttackEntered()
	{
		if (base.inWater && attackWaterAnimeClip != "")
		{
			base.sprite.SetAnimation(attackWaterAnimeClip, loop: true, 0.2);
		}
		else
		{
			base.sprite.SetAnimation(attackAnimeClip, loop: true, 0.2);
		}
		ActivateAttackAfterStateDelay(AttackStateHandle);
	}

	protected void ActivateAttackAfterStateDelay(StateHandle stateHandle)
	{
		startAttack = false;
		ActivateAttackAfterStateDelayAsync(stateHandle);
	}

	private async Task ActivateAttackAfterStateDelayAsync(StateHandle stateHandle)
	{
		if (await WaitForStateDelayAsync(stateHandle, 0.1))
		{
			startAttack = true;
		}
	}

	public virtual void AttackProcessing(double delta)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		if (IsZombieRewindActive())
		{
			Walk();
			return;
		}
		if (!attackComponent.CanAttack())
		{
			Walk();
		}
		else if (startAttack && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && useAttackDps)
		{
			attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
		}
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void AttackExited()
	{
		startAttack = false;
	}

	public virtual void DieEntered()
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		_walkReadyGeneration++;
		_groanPlaybackGeneration++;
		startAttack = false;
		_walkMovementActivationVersion++;
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(false);
		}
		TowerDefensePerfProfiler.End("zombie.death.stopMovement", startTicks2, 1);
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		ZombieDeathComponent obj2 = zombieDeathComponent;
		if (obj2 != null && !obj2.IsReleased)
		{
			zombieDeathComponent.DieEntered();
		}
		TowerDefensePerfProfiler.End("zombie.death.visual", probe.StartTicks, 1);
		TowerDefensePerfProfiler.EndSpikeProbe("zombie.death.visual", in probe, 1, 0.5);
		TowerDefensePerfProfiler.End("zombie.death.enter", startTicks, 1);
	}

	public virtual void DieProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void DieExited()
	{
	}

	public virtual void WalkReady()
	{
		WalkReadyAsync(++_walkReadyGeneration);
	}

	private async Task WalkReadyAsync(ulong readyGeneration)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (readyGeneration == _walkReadyGeneration && GodotObject.IsInstanceValid(this) && IsInsideTree() && inGame && !die && !nearDie && !isDestroy)
		{
			ActivateGameplayProcessing();
			Walk();
		}
	}

	protected void ActivateGroundMovementAfterStateDelay(StateHandle stateHandle)
	{
		ActivateGroundMovementAfterStateDelayAsync(stateHandle);
	}

	private async Task ActivateGroundMovementAfterStateDelayAsync(StateHandle stateHandle)
	{
		if (await WaitForStateDelayAsync(stateHandle, 0.1))
		{
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(true);
			}
		}
	}

	public virtual void Walk()
	{
		ActivateGameplayProcessing();
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool EnsureZeroHealthDeath()
	{
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = instance;
		if (towerDefenseCharacterInstance == null || towerDefenseCharacterInstance.keepAlive || towerDefenseCharacterInstance.hitpoints > 0.0)
		{
			return false;
		}
		return ConvergeZeroHealthDeath(towerDefenseCharacterInstance);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ConvergeZeroHealthDeath(TowerDefenseCharacterInstance currentInstance)
	{
		if (_ensuringZeroHealthDeath)
		{
			return true;
		}
		_ensuringZeroHealthDeath = true;
		try
		{
			if (!currentInstance.die)
			{
				currentInstance.SkipInvincibleDealHurt(0.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
			}
			if (!currentInstance.nearDie)
			{
				currentInstance.nearDie = true;
			}
			if (!nearDie)
			{
				HitpointsNearDie();
			}
			if (!currentInstance.die)
			{
				currentInstance.DieMethod();
			}
			if (!die)
			{
				HitpointsEmpty();
			}
		}
		finally
		{
			_ensuringZeroHealthDeath = false;
		}
		return true;
	}

	protected override StringName NormalizeMainStateEvent(StringName eventName)
	{
		if (eventName != DieStateEvent && (EnsureZeroHealthDeath() || die || (GodotObject.IsInstanceValid(instance) && instance.die)))
		{
			return DieStateEvent;
		}
		return eventName;
	}

	public virtual void Attack()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else
		{
			SendStateEvent("ToAttack");
		}
	}

	public virtual void Die()
	{
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
			SendStateEvent("ToDie");
			TowerDefensePerfProfiler.End("zombie.death.stateTransition", probe.StartTicks, 1);
			TowerDefensePerfProfiler.EndSpikeProbe("zombie.death.stateTransition", in probe, 1, 0.5);
		}
	}

	public override async void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		SwimComponent obj = swimComponent;
		if (obj != null && !obj.IsReleased)
		{
			swimComponent.AnimeCompleted(clip);
		}
		ZombieDeathComponent obj2 = zombieDeathComponent;
		bool flag = obj2 != null && !obj2.IsReleased;
		if (flag)
		{
			flag = await zombieDeathComponent.AnimeCompleted(clip);
		}
		if (!flag && die)
		{
			ZombieDeathComponent obj3 = zombieDeathComponent;
			if (obj3 == null || obj3.IsReleased || !GodotObject.IsInstanceValid(base.sprite) || !zombieDeathComponent.IsDeathAnimationClip(base.sprite.clip) || zombieDeathComponent.IsDeathAnimationClip(clip))
			{
				HitBoxDestroy();
				Die();
			}
		}
	}

	public override void Garlic()
	{
		GarlicComponent obj = garlicComponent;
		if (obj != null && !obj.IsReleased)
		{
			garlicComponent.Garlic();
		}
	}

	public void ChangeLine()
	{
		GarlicComponent obj = garlicComponent;
		if (obj != null && !obj.IsReleased)
		{
			garlicComponent.ChangeLine();
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
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
			return;
		}
		base.Hypnoses(time, canFliter, hypnosesConfig);
		if ((instance.unUseBuffFlags & 8) == 0)
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased)
			{
				attackComponent.target = null;
			}
			if (time == -1.0)
			{
				EmitBodyHurt((int)GetCurrentHitPoint());
			}
		}
	}

	public virtual void InWaterDiscardSet()
	{
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.InWaterDiscardSet();
		}
	}

	public virtual void OutWaterDiscardSet()
	{
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.OutWaterDiscardSet();
		}
	}

	public override void InWater()
	{
		base.InWater();
		SwimComponent obj = swimComponent;
		if (obj != null && !obj.IsReleased)
		{
			swimComponent.InWater();
		}
		WaterInteractionComponent obj2 = waterInteractionComponent;
		if (obj2 != null && !obj2.IsReleased)
		{
			waterInteractionComponent.InWater();
		}
	}

	public override void OutWater()
	{
		base.OutWater();
		WaterInteractionComponent obj = waterInteractionComponent;
		if (obj != null && !obj.IsReleased)
		{
			waterInteractionComponent.OutWater();
		}
		SwimComponent obj2 = swimComponent;
		if (obj2 != null && !obj2.IsReleased)
		{
			swimComponent.OutWater();
		}
	}
}
