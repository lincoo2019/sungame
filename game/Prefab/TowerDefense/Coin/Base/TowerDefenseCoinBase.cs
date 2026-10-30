using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseCoinBase : TowerDefenseGroundItemBase, IObjectPoolLifecycle
{
	public delegate void CollectEventHandler(int num);

	private static readonly StringName CoinGroupName = new StringName("Coin");

	public Node2D spriteNode;

	public MoveComponent moveComponent;

	[Export(PropertyHint.None, "")]
	public string fallAudio = "CoinFall";

	[Export(PropertyHint.None, "")]
	public string pickAudio = "CoinPick";

	[Export(PropertyHint.None, "")]
	public int num = 10;

	public bool canMagnet = true;

	public double height = 500.0;

	public bool isCollect;

	public bool die;

	public bool disabledInput;

	public bool over;

	public Viewport view;

	public Camera2D camera;

	public Vector2 viewSize;

	public bool autoCollect;

	public int randFreshIndex;

	private int _viewportCheckCountdown = 1;

	private Timer _dieDownTimer;

	private Tween _collectionTween;

	private Tween _fadeTween;

	private SceneTree _initFrameTree;

	private Action _initFrameCallback;

	private SceneTreeTimer _autoCollectTimer;

	private Action _autoCollectCallback;

	private int _initFrameLeaseVersion = -1;

	private int _autoCollectLeaseVersion = -1;

	private int _collectionLeaseVersion = -1;

	private int _fadeLeaseVersion = -1;

	private Callable _collectionTweenCallback;

	private Callable _fadeTweenCallback;

	private int _leaseVersion;

	private bool _supportsDirectPoolLifecycleDispatch;

	private bool _poolLifecyclePolicyInitialized;

	[Export(PropertyHint.None, "")]
	public int coinObjectId { get; set; }

	bool IObjectPoolLifecycle.SupportsDirectPoolLifecycleDispatch
	{
		get
		{
			EnsurePoolLifecyclePolicy();
			return _supportsDirectPoolLifecycleDispatch;
		}
	}

	public event CollectEventHandler OnCollect;

	public DropItemConfig _GetDropItemConfig()
	{
		return DropItemRegistry.GetByCoinObjectId(coinObjectId);
	}

	public void Refresh()
	{
		BeginLease();
		SetPhysicsProcess(enable: true);
		AddToGroup(CoinGroupName, persistent: true);
		spriteNode.Position = Vector2.Zero;
		Color modulate = spriteNode.Modulate;
		modulate.A = 1f;
		spriteNode.Modulate = modulate;
		height = 0.0;
		isCollect = false;
		die = false;
		disabledInput = false;
		over = false;
		RestartCoinAnimations(spriteNode);
		_viewportCheckCountdown = 1 + (randFreshIndex & 0x7FFFFFFF) % 30;
		DropItemConfig dropItemConfig = _GetDropItemConfig();
		if (dropItemConfig != null)
		{
			fallAudio = dropItemConfig.FallAudio;
			pickAudio = dropItemConfig.PickAudio;
		}
		view = GetViewport();
		camera = view.GetCamera2D();
		viewSize = view.GetVisibleRect().Size;
		if (GodotObject.IsInstanceValid(_dieDownTimer))
		{
			_dieDownTimer.Start();
		}
		AudioManager.Instance.AudioPlay(fallAudio);
	}

	private void RestartCoinAnimations(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child is AdobeAnimateSprite adobeAnimateSprite && !string.IsNullOrEmpty(adobeAnimateSprite.clip))
			{
				adobeAnimateSprite.SetAnimation(adobeAnimateSprite.clip, adobeAnimateSprite.loop);
			}
			if (child != null)
			{
				Node node2 = child;
				RestartCoinAnimations(node2);
			}
		}
	}

	public void Recycle()
	{
		InvalidateLease();
		RemoveFromGroup(CoinGroupName);
		if (GodotObject.IsInstanceValid(_dieDownTimer))
		{
			_dieDownTimer.Stop();
		}
		moveComponent?.MoveClear();
		SetPhysicsProcess(enable: false);
		autoCollect = false;
		this.OnCollect = null;
		view = null;
		camera = null;
	}

	void IObjectPoolLifecycle.RefreshFromPool()
	{
		Refresh();
	}

	void IObjectPoolLifecycle.RecycleToPool()
	{
		Recycle();
	}

	public void Init(double _height = 0.0, Vector2 _velocity = default(Vector2), double _gravity = 0.0)
	{
		int leaseVersion = _leaseVersion;
		height = _height;
		moveComponent.SetVelocity(_velocity);
		moveComponent.SetGravity(_gravity);
		SetPhysicsProcess(moveComponent.HasActiveMovement);
		if (!moveComponent.HasActiveMovement)
		{
			ClampToViewportIfNeeded();
		}
		ScheduleInitAfterPhysics(leaseVersion);
	}

	public override void _Ready()
	{
		EnsurePoolLifecyclePolicy();
		if (!Engine.IsEditorHint())
		{
			spriteNode = GetNode<Node2D>("%SpriteNode");
			moveComponent = GetNode<MoveComponent>("%MoveComponent");
			moveComponent.MovementActivityChanged -= OnMovementActivityChanged;
			moveComponent.MovementActivityChanged += OnMovementActivityChanged;
			_dieDownTimer = GetNode<Timer>("DieDownTimer");
			base._Ready();
			randFreshIndex = (int)GD.Randi();
			view = GetViewport();
			camera = view.GetCamera2D();
			viewSize = view.GetVisibleRect().Size;
			_dieDownTimer.Timeout += DieDown;
		}
	}

	private void EnsurePoolLifecyclePolicy()
	{
		if (!_poolLifecyclePolicyInitialized)
		{
			_poolLifecyclePolicyInitialized = true;
			Type type = GetType();
			_supportsDirectPoolLifecycleDispatch = (type == typeof(TowerDefenseCoinBase) || type == typeof(TowerDefenseLuckyBag) || type == typeof(TowerDefenseGoldShard)) && ObjectPoolLifecyclePolicy.HasBuiltInCSharpScript(this);
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (Geometry2D.IsPointInCircle(GetGlobalMousePosition(), spriteNode.GlobalPosition, 30f * base.Scale.X))
		{
			Collection();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (autoCollect)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		_viewportCheckCountdown--;
		if (_viewportCheckCountdown <= 0)
		{
			_viewportCheckCountdown = 30;
			ClampToViewportIfNeeded();
		}
		if (!over && (double)spriteNode.Position.Y > height)
		{
			spriteNode.Position = new Vector2(spriteNode.Position.X, (float)height);
			over = true;
			moveComponent.MoveClear();
			ClampToViewportIfNeeded();
			SetPhysicsProcess(enable: false);
		}
	}

	private void ClampToViewportIfNeeded()
	{
		if (GodotObject.IsInstanceValid(camera) && GodotObject.IsInstanceValid(spriteNode) && !camera.GetViewportRect().HasPoint(spriteNode.GlobalPosition))
		{
			Vector2 globalPosition = camera.GlobalPosition;
			spriteNode.GlobalPosition = new Vector2(Mathf.Clamp(spriteNode.GlobalPosition.X, globalPosition.X + 40f, globalPosition.X + viewSize.X - 40f), Mathf.Clamp(spriteNode.GlobalPosition.Y, globalPosition.Y + 40f, globalPosition.Y + viewSize.Y - 40f));
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
			ClampToViewportIfNeeded();
			SetPhysicsProcess(enable: false);
		}
	}

	public override void Collection()
	{
		if (die || isCollect)
		{
			return;
		}
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		if (instance != null && !instance.IsUnlocked("Shop"))
		{
			BroadCastConfig broadCastConfig = new BroadCastConfig();
			broadCastConfig.broadCastString = "SHOP_OPEN";
			broadCastConfig.broadCastTime = 7.5;
			BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
			GlobalFeatureManager.Instance.Unlock("Shop");
		}
		AudioManager.Instance.AudioPlay(pickAudio);
		int leaseVersion = _leaseVersion;
		moveComponent.MoveClear();
		SetPhysicsProcess(enable: false);
		isCollect = true;
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.coinBank))
		{
			TowerDefenseManager.Instance.coinBank.ShowCoinBank();
		}
		RemoveFromGroup(CoinGroupName);
		KillTween(ref _collectionTween);
		_collectionTween = CreateTween();
		_collectionTween.SetEase(Tween.EaseType.Out);
		_collectionTween.SetTrans(Tween.TransitionType.Cubic);
		Vector2 flightStart = spriteNode.GlobalPosition;
		_collectionTween.TweenMethod(Callable.From(delegate(float progress)
		{
			if (IsCurrentLease(leaseVersion))
			{
				spriteNode.GlobalPosition = flightStart.Lerp(GetCollectionTargetPosition(), progress);
			}
		}), 0f, 1f, 1.0);
		_collectionLeaseVersion = leaseVersion;
		_collectionTweenCallback = Callable.From(FinishCollectionFlightCallback);
		_collectionTween.TweenCallback(_collectionTweenCallback);
	}

	internal Vector2 GetCollectionTargetPosition()
	{
		CoinBank coinBank = TowerDefenseManager.Instance?.coinBank;
		if (!GodotObject.IsInstanceValid(coinBank))
		{
			return spriteNode.GlobalPosition;
		}
		Vector2 vector = coinBank.GetScreenTransform() * new Vector2(34f, 23f);
		Vector2 localPoint = spriteNode.GetScreenTransform().AffineInverse() * vector;
		return spriteNode.ToGlobal(localPoint);
	}

	public void DieDown()
	{
		if (!isCollect)
		{
			moveComponent.MoveClear();
			SetPhysicsProcess(enable: false);
			RemoveFromGroup(CoinGroupName);
			die = true;
			int leaseVersion = _leaseVersion;
			KillTween(ref _fadeTween);
			_fadeTween = CreateTween();
			_fadeTween.SetEase(Tween.EaseType.Out);
			_fadeTween.SetTrans(Tween.TransitionType.Cubic);
			_fadeTween.TweenProperty(spriteNode, "modulate:a", 0.0, 0.25);
			_fadeLeaseVersion = leaseVersion;
			_fadeTweenCallback = Callable.From(FinishFadeAndDestroyCallback);
			_fadeTween.TweenCallback(_fadeTweenCallback);
		}
	}

	private void FinishCollectionFlight(int leaseVersion)
	{
		_collectionTween = null;
		if (IsCurrentLease(leaseVersion))
		{
			HandleCollectedValue(num);
			this.OnCollect?.Invoke(num);
			if (IsCurrentLease(leaseVersion))
			{
				KillTween(ref _fadeTween);
				_fadeTween = CreateTween();
				_fadeTween.TweenProperty(spriteNode, "modulate:a", 0.0, 0.25);
				_fadeLeaseVersion = leaseVersion;
				_fadeTweenCallback = Callable.From(FinishFadeAndDestroyCallback);
				_fadeTween.TweenCallback(_fadeTweenCallback);
			}
		}
	}

	private void FinishCollectionFlightCallback()
	{
		FinishCollectionFlight(_collectionLeaseVersion);
	}

	private void FinishFadeAndDestroy(int leaseVersion)
	{
		_fadeTween = null;
		if (IsCurrentLease(leaseVersion))
		{
			Destroy();
		}
	}

	private void FinishFadeAndDestroyCallback()
	{
		FinishFadeAndDestroy(_fadeLeaseVersion);
	}

	private void HandleCollectedValue(int value)
	{
		DropItemConfig dropItemConfig = _GetDropItemConfig();
		if (dropItemConfig?.Handler != null)
		{
			dropItemConfig.Handler.OnCollect(base.GlobalPosition, value);
		}
		else
		{
			TowerDefenseManager.Instance?.AddCoin(value);
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
		_initFrameLeaseVersion = -1;
		_autoCollectLeaseVersion = -1;
		_collectionLeaseVersion = -1;
		_fadeLeaseVersion = -1;
		CancelDeferredCallbacks();
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
		if (GodotObject.IsInstanceValid(_dieDownTimer))
		{
			_dieDownTimer.Stop();
		}
		this.OnCollect = null;
		view = null;
		camera = null;
		base._ExitTree();
	}

	private void ScheduleInitAfterPhysics(int leaseVersion)
	{
		CancelInitFrameCallback();
		_initFrameTree = GetTree();
		_initFrameLeaseVersion = leaseVersion;
		if (_initFrameCallback == null)
		{
			_initFrameCallback = OnInitPhysicsFrame;
		}
		_initFrameTree.PhysicsFrame += _initFrameCallback;
	}

	private void OnInitPhysicsFrame()
	{
		int initFrameLeaseVersion = _initFrameLeaseVersion;
		CancelInitFrameCallback();
		if (IsCurrentLease(initFrameLeaseVersion))
		{
			autoCollect = GameSaveManager.Instance.GetFeatureValue("CoinCollect") > 0 && GetTree().GetNodeCountInGroup("GoldMagnet") <= 0;
			if (autoCollect)
			{
				SetPhysicsProcess(enable: false);
				ScheduleAutoCollect(initFrameLeaseVersion);
			}
		}
	}

	private void ScheduleAutoCollect(int leaseVersion)
	{
		CancelAutoCollectTimer();
		_autoCollectLeaseVersion = leaseVersion;
		_autoCollectTimer = GetTree().CreateTimer(0.5, processAlways: false);
		if (_autoCollectCallback == null)
		{
			_autoCollectCallback = OnAutoCollectTimeout;
		}
		_autoCollectTimer.Timeout += _autoCollectCallback;
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
		CancelInitFrameCallback();
		CancelAutoCollectTimer();
	}

	private void CancelInitFrameCallback()
	{
		if (GodotObject.IsInstanceValid(_initFrameTree) && _initFrameCallback != null)
		{
			_initFrameTree.PhysicsFrame -= _initFrameCallback;
		}
		_initFrameLeaseVersion = -1;
		_initFrameTree = null;
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

	public void Destroy()
	{
		ObjectManagerConfig.OBJECT poolKeyByCoinObjectId = DropItemRegistry.GetPoolKeyByCoinObjectId(coinObjectId);
		if (poolKeyByCoinObjectId != ObjectManagerConfig.OBJECT.NOONE)
		{
			ObjectManager.PoolPush(poolKeyByCoinObjectId, this);
			return;
		}
		switch (coinObjectId)
		{
		case 0:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_SILVER, this);
			break;
		case 1:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_GOLD, this);
			break;
		case 2:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_DIAMOND, this);
			break;
		case 3:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_LUCKY_BAG, this);
			break;
		case 4:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_TQ, this);
			break;
		case 5:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_YB1, this);
			break;
		case 6:
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_YB2, this);
			break;
		}
	}
}
