using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseSunBase : Node2D, IObjectPoolLifecycle
{
	public delegate void CollectEventHandler(long num);

	public const string SaveGroupName = "SunDropItem";

	private const float InitialVisualScaleFactor = 0.25f;

	private static readonly StringName SaveGroupNameKey = new StringName("SunDropItem");

	private AdobeAnimateSprite _sprite;

	private MoveComponent _moveComponent;

	private PointLight2D _light;

	private Timer _dieDownTimer;

	private Tween _initScaleTween;

	private Tween _collectionTween;

	private Tween _fadeTween;

	private SceneTree _refreshFrameTree;

	private Action _refreshFrameCallback;

	private SceneTreeTimer _moveStopTimer;

	private Action _moveStopCallback;

	private SceneTreeTimer _autoCollectTimer;

	private Action _autoCollectCallback;

	private StringName _groupName;

	private int _refreshFrameLeaseVersion = -1;

	private int _moveStopLeaseVersion = -1;

	private int _autoCollectLeaseVersion = -1;

	private int _collectionLeaseVersion = -1;

	private int _fadeLeaseVersion = -1;

	private DropItemCollectionContext _collectionContext;

	private long _collectionValue;

	private Callable _collectionTweenCallback;

	private Callable _fadeTweenCallback;

	private int _leaseVersion;

	private int _restoredLeaseVersion = -1;

	private int _autoCollectScheduledLease = -1;

	private bool _supportsDirectPoolLifecycleDispatch;

	private bool _poolLifecyclePolicyInitialized;

	public long sunNum = 25L;

	public double height = 500.0;

	public bool isCollect;

	public bool die;

	public TowerDefenseEnum.SUN_MOVING_METHOD movingMethod;

	public bool over;

	public Vector2I gridPos = new Vector2I(-1, -1);

	public Viewport view;

	public Camera2D camera;

	public Vector2 viewSize;

	public bool autoCollect;

	public AdobeAnimateSprite sprite => _sprite;

	public MoveComponent moveComponent => _moveComponent;

	public PointLight2D light => _light;

	public Timer dieDownTimer => _dieDownTimer;

	public EconomyAccountId AccountId { get; private set; }

	public SunDropOwnershipPolicy OwnershipPolicy { get; private set; }

	public bool UsesLegacyLocalEconomy => OwnershipPolicy == SunDropOwnershipPolicy.LegacySharedReplica;

	bool IObjectPoolLifecycle.SupportsDirectPoolLifecycleDispatch
	{
		get
		{
			EnsurePoolLifecyclePolicy();
			return _supportsDirectPoolLifecycleDispatch;
		}
	}

	public event CollectEventHandler OnCollect;

	public virtual string GetGroupName()
	{
		return "Sun";
	}

	public virtual ObjectManagerConfig.OBJECT GetPoolKey()
	{
		return ObjectManagerConfig.OBJECT.SUN;
	}

	public DropItemConfig GetDropItemConfig()
	{
		return DropItemRegistry.GetById(GetPoolKey());
	}

	public virtual void OnCollectStart()
	{
	}

	public virtual long GetCollectValue()
	{
		DropItemConfig dropItemConfig = GetDropItemConfig();
		if (dropItemConfig != null && dropItemConfig.Handler != null)
		{
			return dropItemConfig.Handler.GetCollectValue(CreateCollectionContext(sunNum));
		}
		return sunNum;
	}

	public virtual bool OnDieDown()
	{
		return false;
	}

	public virtual bool ShouldAutoCollect()
	{
		DropItemConfig dropItemConfig = GetDropItemConfig();
		if (dropItemConfig != null && dropItemConfig.Handler != null && dropItemConfig.Handler.ShouldAutoCollect(CreateCollectionContext(sunNum)))
		{
			return true;
		}
		return autoCollect;
	}

	public virtual void OnReady()
	{
	}

	public virtual void OnRefresh()
	{
	}

	public virtual void OnInitScaleTween(Tween tween)
	{
	}

	public void Refresh()
	{
		int leaseVersion = BeginLease();
		SetPhysicsProcess(enable: true);
		if (_groupName == null || _groupName.IsEmpty)
		{
			_groupName = new StringName(GetGroupName());
		}
		AddToGroup(_groupName, persistent: true);
		AddToGroup(SaveGroupNameKey, persistent: true);
		AccountId = default(EconomyAccountId);
		OwnershipPolicy = SunDropOwnershipPolicy.LegacySharedReplica;
		_sprite.Position = Vector2.Zero;
		sunNum = 25L;
		height = 500.0;
		isCollect = false;
		die = false;
		over = false;
		_sprite.Scale = Vector2.One * 0.25f;
		view = GetViewport();
		camera = view.GetCamera2D();
		viewSize = view.GetVisibleRect().Size;
		if (GodotObject.IsInstanceValid(_light))
		{
			_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
		RestartSunAnimation();
		Color modulate = _sprite.Modulate;
		modulate.A = 1f;
		_sprite.Modulate = modulate;
		if (GodotObject.IsInstanceValid(_dieDownTimer))
		{
			_dieDownTimer.Start();
		}
		OnRefresh();
		ScheduleRefreshAfterPhysics(leaseVersion);
	}

	public virtual void Recycle()
	{
		InvalidateLease();
		RemoveSunGroups();
		if (GodotObject.IsInstanceValid(_dieDownTimer))
		{
			_dieDownTimer.Stop();
		}
		_moveComponent?.MoveClear();
		SetPhysicsProcess(enable: false);
		autoCollect = false;
		this.OnCollect = null;
		AccountId = default(EconomyAccountId);
		OwnershipPolicy = SunDropOwnershipPolicy.LegacySharedReplica;
	}

	void IObjectPoolLifecycle.RefreshFromPool()
	{
		Refresh();
	}

	void IObjectPoolLifecycle.RecycleToPool()
	{
		Recycle();
	}

	public void Init(long _sunNum, TowerDefenseEnum.SUN_MOVING_METHOD _movingMethod, double _height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0, double moverStopTime = -1.0)
	{
		InitCore(EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, _sunNum, _movingMethod, _height, velocity, gravity, moverStopTime);
	}

	public void Init(EconomyAccountId accountId, long _sunNum, TowerDefenseEnum.SUN_MOVING_METHOD _movingMethod, double _height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0, double moverStopTime = -1.0)
	{
		InitCore(accountId.IsValid ? accountId : EconomyAccountId.Local, accountId.IsValid ? SunDropOwnershipPolicy.AccountOwned : SunDropOwnershipPolicy.LegacySharedReplica, _sunNum, _movingMethod, _height, velocity, gravity, moverStopTime);
	}

	public void InitFromSave(EconomyAccountId accountId, SunDropOwnershipPolicy ownershipPolicy, long _sunNum, TowerDefenseEnum.SUN_MOVING_METHOD _movingMethod, double _height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0, double moverStopTime = -1.0)
	{
		bool flag = !accountId.IsValid || ownershipPolicy != SunDropOwnershipPolicy.AccountOwned;
		InitCore(flag ? EconomyAccountId.Local : accountId, (!flag) ? SunDropOwnershipPolicy.AccountOwned : SunDropOwnershipPolicy.LegacySharedReplica, _sunNum, _movingMethod, _height, velocity, gravity, moverStopTime);
		_restoredLeaseVersion = _leaseVersion;
	}

	public void RestoreAutoCollect(bool savedAutoCollect)
	{
		autoCollect = savedAutoCollect;
		_restoredLeaseVersion = _leaseVersion;
		if (ShouldAutoCollect())
		{
			SetPhysicsProcess(enable: false);
			ScheduleAutoCollect(_leaseVersion);
		}
	}

	private void InitCore(EconomyAccountId accountId, SunDropOwnershipPolicy ownershipPolicy, long _sunNum, TowerDefenseEnum.SUN_MOVING_METHOD _movingMethod, double _height, Vector2 velocity, double gravity, double moverStopTime)
	{
		int leaseVersion = _leaseVersion;
		AccountId = accountId;
		OwnershipPolicy = ownershipPolicy;
		sunNum = _sunNum;
		movingMethod = _movingMethod;
		height = _height;
		switch (_movingMethod)
		{
		case TowerDefenseEnum.SUN_MOVING_METHOD.LAND:
			_moveComponent.SetVelocity(velocity);
			break;
		case TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY:
			_moveComponent.SetVelocity(velocity);
			_moveComponent.SetGravity(gravity);
			break;
		}
		SetPhysicsProcess(_moveComponent.HasActiveMovement);
		float num = Mathf.Abs((float)_sunNum / 25f);
		if (num > 2f)
		{
			num = 2f;
		}
		KillTween(ref _initScaleTween);
		Vector2 vector = Vector2.One * num * 0.25f;
		_sprite.Scale = vector;
		_initScaleTween = CreateTween();
		_initScaleTween.TweenProperty(_sprite, "scale", Vector2.One * num, 0.1).From(vector);
		OnInitScaleTween(_initScaleTween);
		if (moverStopTime != -1.0)
		{
			ScheduleMoveStop(leaseVersion, moverStopTime);
		}
	}

	public override void _Ready()
	{
		EnsurePoolLifecyclePolicy();
		_sprite = GetNodeOrNull<AdobeAnimateSprite>("%SunSprite");
		_moveComponent = GetNodeOrNull<MoveComponent>("%MoveComponent");
		if (GodotObject.IsInstanceValid(_moveComponent))
		{
			_moveComponent.MovementActivityChanged -= OnMovementActivityChanged;
			_moveComponent.MovementActivityChanged += OnMovementActivityChanged;
		}
		_light = GetNodeOrNull<PointLight2D>("%Light");
		_dieDownTimer = GetNodeOrNull<Timer>("%DieDownTimer");
		view = GetViewport();
		camera = view.GetCamera2D();
		viewSize = view.GetVisibleRect().Size;
		RestartSunAnimation();
		OnReady();
	}

	private void EnsurePoolLifecyclePolicy()
	{
		if (!_poolLifecyclePolicyInitialized)
		{
			_poolLifecyclePolicyInitialized = true;
			Type type = GetType();
			_supportsDirectPoolLifecycleDispatch = (type == typeof(TowerDefenseSunBase) || type == typeof(TowerDefenseSun) || type == typeof(TowerDefenseBrainSun) || type == typeof(TowerDefenseSunJalapeno) || type == typeof(TowerDefenseSunQX) || type == typeof(TowerDefenseSunMagic)) && ObjectPoolLifecyclePolicy.HasBuiltInCSharpScript(this);
		}
	}

	private void RestartSunAnimation()
	{
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.Visible = true;
			_sprite.ProcessMode = ProcessModeEnum.Inherit;
			_sprite.pause = false;
			_sprite.SetAnimation("Idle");
			_sprite.UpdateChild();
			_sprite.RefreshManagedSlotSpriteCacheForRender();
			_sprite.RefreshProcessScheduling();
			_sprite.QueueRedraw();
		}
	}

	public override void _Input(InputEvent _event)
	{
		if (!isCollect && Geometry2D.IsPointInCircle(GetGlobalMousePosition(), _sprite.GlobalPosition, 40f * base.Scale.X))
		{
			Collection();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (autoCollect)
		{
			SetPhysicsProcess(enable: false);
		}
		else if (!over && (double)_sprite.Position.Y > height && _moveComponent.velocity.Y > 0f)
		{
			_sprite.Position = new Vector2(_sprite.Position.X, (float)height);
			over = true;
			_moveComponent.MoveClear();
			SetPhysicsProcess(enable: false);
		}
	}

	private void OnMovementActivityChanged(bool active)
	{
		if (active && !autoCollect && !isCollect && !die)
		{
			over = false;
			SetPhysicsProcess(enable: true);
		}
		else if (!active)
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public void Collection()
	{
		if (!die && !isCollect)
		{
			OnCollectStart();
			int leaseVersion = _leaseVersion;
			if (GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = false;
			}
			long collectValue = GetCollectValue();
			DropItemCollectionContext collectionContext = CreateCollectionContext(collectValue);
			DropItemConfig dropItemConfig = GetDropItemConfig();
			string stream = "Sun";
			if (dropItemConfig != null)
			{
				stream = dropItemConfig.PickAudio;
			}
			if (collectValue != 0L)
			{
				AudioManager.Instance.AudioPlay(stream);
			}
			_moveComponent.MoveClear();
			SetPhysicsProcess(enable: false);
			isCollect = true;
			Vector2 vector = (GodotObject.IsInstanceValid(camera) ? camera.GlobalPosition : Vector2.Zero);
			KillTween(ref _collectionTween);
			_collectionTween = CreateTween();
			_collectionTween.SetEase(Tween.EaseType.Out);
			_collectionTween.SetTrans(Tween.TransitionType.Cubic);
			_collectionTween.TweenProperty(_sprite, "global_position", vector + new Vector2(32f, 32f), 1.0);
			_collectionLeaseVersion = leaseVersion;
			_collectionContext = collectionContext;
			_collectionValue = collectValue;
			_collectionTweenCallback = Callable.From(FinishCollectionFlightCallback);
			_collectionTween.TweenCallback(_collectionTweenCallback);
		}
	}

	private void FinishCollectionFlight(int leaseVersion, DropItemCollectionContext collectionContext, long collectValue)
	{
		_collectionTween = null;
		if (IsCurrentLease(leaseVersion))
		{
			HandleCollectedValue(collectionContext);
			this.OnCollect?.Invoke(collectValue);
			if (IsCurrentLease(leaseVersion))
			{
				RemoveSunGroups();
				StartFadeAndDestroy(leaseVersion);
			}
		}
	}

	private void FinishCollectionFlightCallback()
	{
		FinishCollectionFlight(_collectionLeaseVersion, _collectionContext, _collectionValue);
	}

	private void StartFadeAndDestroy(int leaseVersion)
	{
		KillTween(ref _fadeTween);
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_sprite, "modulate:a", 0.0, 0.25);
		_fadeLeaseVersion = leaseVersion;
		_fadeTweenCallback = Callable.From(FinishFadeAndDestroyCallback);
		_fadeTween.TweenCallback(_fadeTweenCallback);
	}

	private void FinishFadeAndDestroyCallback()
	{
		_fadeTween = null;
		if (IsCurrentLease(_fadeLeaseVersion))
		{
			Destroy();
		}
	}

	public virtual void DieDown()
	{
		if (!isCollect && !OnDieDown())
		{
			_moveComponent.MoveClear();
			SetPhysicsProcess(enable: false);
			RemoveSunGroups();
			die = true;
			int leaseVersion = _leaseVersion;
			KillTween(ref _fadeTween);
			_fadeTween = CreateTween();
			_fadeTween.SetEase(Tween.EaseType.Out);
			_fadeTween.SetTrans(Tween.TransitionType.Cubic);
			_fadeTween.TweenProperty(_sprite, "modulate:a", 0.0, 0.25);
			_fadeLeaseVersion = leaseVersion;
			_fadeTweenCallback = Callable.From(FinishFadeAndDestroyCallback);
			_fadeTween.TweenCallback(_fadeTweenCallback);
		}
	}

	public void Destroy()
	{
		RemoveSunGroups();
		ObjectManager.PoolPush(GetPoolKey(), this);
	}

	private void RemoveSunGroups()
	{
		if (_groupName != null)
		{
			RemoveFromGroup(_groupName);
		}
		RemoveFromGroup(SaveGroupNameKey);
	}

	private DropItemCollectionContext CreateCollectionContext(long value)
	{
		return new DropItemCollectionContext(this, base.GlobalPosition, value, AccountId, OwnershipPolicy);
	}

	private void HandleCollectedValue(DropItemCollectionContext context)
	{
		DropItemConfig dropItemConfig = GetDropItemConfig();
		if (dropItemConfig?.Handler != null)
		{
			dropItemConfig.Handler.OnCollect(context);
		}
		else if (context.UsesLegacyLocalEconomy)
		{
			TowerDefenseManager.Instance?.AddSun(context.Value);
		}
		else
		{
			SunDropItemHandler.ApplyAccountValue(context);
		}
	}

	private int BeginLease()
	{
		InvalidateLease();
		return _leaseVersion;
	}

	private void InvalidateLease()
	{
		_leaseVersion++;
		_refreshFrameLeaseVersion = -1;
		_moveStopLeaseVersion = -1;
		_autoCollectLeaseVersion = -1;
		_collectionLeaseVersion = -1;
		_fadeLeaseVersion = -1;
		_restoredLeaseVersion = -1;
		_autoCollectScheduledLease = -1;
		CancelDeferredCallbacks();
		KillTween(ref _initScaleTween);
		KillTween(ref _collectionTween);
		KillTween(ref _fadeTween);
	}

	private bool IsCurrentLease(int leaseVersion)
	{
		if (leaseVersion == _leaseVersion)
		{
			return IsInsideTree();
		}
		return false;
	}

	public override void _ExitTree()
	{
		InvalidateLease();
		view = null;
		camera = null;
		this.OnCollect = null;
	}

	private void ScheduleRefreshAfterPhysics(int leaseVersion)
	{
		CancelRefreshFrameCallback();
		_refreshFrameTree = GetTree();
		_refreshFrameLeaseVersion = leaseVersion;
		if (_refreshFrameCallback == null)
		{
			_refreshFrameCallback = OnRefreshPhysicsFrame;
		}
		_refreshFrameTree.PhysicsFrame += _refreshFrameCallback;
	}

	private void OnRefreshPhysicsFrame()
	{
		int refreshFrameLeaseVersion = _refreshFrameLeaseVersion;
		CancelRefreshFrameCallback();
		if (IsCurrentLease(refreshFrameLeaseVersion))
		{
			if (_restoredLeaseVersion != refreshFrameLeaseVersion)
			{
				autoCollect = GameSaveManager.Instance.GetFeatureValue("SunCollect") != 0;
			}
			if (ShouldAutoCollect())
			{
				SetPhysicsProcess(enable: false);
				ScheduleAutoCollect(refreshFrameLeaseVersion);
			}
		}
	}

	private void ScheduleMoveStop(int leaseVersion, double delay)
	{
		CancelMoveStopTimer();
		_moveStopLeaseVersion = leaseVersion;
		_moveStopTimer = GetTree().CreateTimer(delay, processAlways: false);
		if (_moveStopCallback == null)
		{
			_moveStopCallback = OnMoveStopTimeout;
		}
		_moveStopTimer.Timeout += _moveStopCallback;
	}

	private void OnMoveStopTimeout()
	{
		int moveStopLeaseVersion = _moveStopLeaseVersion;
		CancelMoveStopTimer();
		if (IsCurrentLease(moveStopLeaseVersion))
		{
			_moveComponent.MoveClear();
			SetPhysicsProcess(enable: false);
		}
	}

	private void ScheduleAutoCollect(int leaseVersion)
	{
		if (IsCurrentLease(leaseVersion) && _autoCollectScheduledLease != leaseVersion)
		{
			_autoCollectScheduledLease = leaseVersion;
			CancelAutoCollectTimer();
			_autoCollectLeaseVersion = leaseVersion;
			_autoCollectTimer = GetTree().CreateTimer(0.5, processAlways: false);
			if (_autoCollectCallback == null)
			{
				_autoCollectCallback = OnAutoCollectTimeout;
			}
			_autoCollectTimer.Timeout += _autoCollectCallback;
		}
	}

	private void OnAutoCollectTimeout()
	{
		int autoCollectLeaseVersion = _autoCollectLeaseVersion;
		CancelAutoCollectTimer();
		if (IsCurrentLease(autoCollectLeaseVersion))
		{
			Collection();
		}
	}

	private void CancelDeferredCallbacks()
	{
		CancelRefreshFrameCallback();
		CancelMoveStopTimer();
		CancelAutoCollectTimer();
	}

	private void CancelRefreshFrameCallback()
	{
		if (GodotObject.IsInstanceValid(_refreshFrameTree) && _refreshFrameCallback != null)
		{
			_refreshFrameTree.PhysicsFrame -= _refreshFrameCallback;
		}
		_refreshFrameLeaseVersion = -1;
		_refreshFrameTree = null;
	}

	private void CancelMoveStopTimer()
	{
		if (GodotObject.IsInstanceValid(_moveStopTimer) && _moveStopCallback != null)
		{
			_moveStopTimer.Timeout -= _moveStopCallback;
		}
		_moveStopLeaseVersion = -1;
		_moveStopTimer = null;
	}

	private void CancelAutoCollectTimer()
	{
		if (GodotObject.IsInstanceValid(_autoCollectTimer) && _autoCollectCallback != null)
		{
			_autoCollectTimer.Timeout -= _autoCollectCallback;
		}
		_autoCollectLeaseVersion = -1;
		_autoCollectTimer = null;
	}

	private static void KillTween(ref Tween tween)
	{
		if (GodotObject.IsInstanceValid(tween))
		{
			tween.Kill();
		}
		tween = null;
	}
}
