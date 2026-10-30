using System;
using Godot;

[GlobalClass]
public partial class DamagePartDrop : TowerDefenseGroundItemBase, IObjectPoolLifecycle
{
	private static readonly StringName EffectGroupName = new StringName("Effect");

	private const float DropGravity = 1960f;

	private const int MinimumDamagePartZIndex = -4095;

	private TowerDefenseShadowVisual shadowSprite;

	private Node2D spriteGroup;

	private Node2D spriteNode;

	private MoveComponent moveComponent;

	private bool landingPlaneReady;

	private float landingWorldY;

	private bool flightStateReady;

	private Vector2 flightWorldPosition;

	private Vector2 shadowWorldPosition;

	private Vector2 flightVelocity;

	private Transform2D flightWorldTransform;

	private Transform2D flightRootWorldInverse;

	private bool supportsDirectPoolLifecycleDispatch;

	private bool poolLifecyclePolicyInitialized;

	private SceneTreeTimer landingFadeTimer;

	private Action landingFadeTimerCallback;

	private Tween landingFadeTween;

	private int leaseVersion;

	private int landingFadeLeaseVersion = -1;

	private bool poolPushQueued;

	public double height = 60.0;

	public double jumpSpeed = 300.0;

	public int jumpTime = 3;

	public Vector2 initVelocity = Vector2.Zero;

	public bool over;

	public Node2D sprite;

	bool IObjectPoolLifecycle.SupportsDirectPoolLifecycleDispatch
	{
		get
		{
			EnsurePoolLifecyclePolicy();
			return supportsDirectPoolLifecycleDispatch;
		}
	}

	public override void _Ready()
	{
		EnsurePoolLifecyclePolicy();
		shadowSprite = TowerDefenseShadowVisual.Capture(this, GetNodeOrNull<Sprite2D>("%ShadowSprite"));
		spriteGroup = GetNode<Node2D>("%SpriteGroup");
		spriteNode = GetNode<Node2D>("%Sprite");
		moveComponent = GetNode<MoveComponent>("%MoveComponent");
		landingFadeTimerCallback = OnLandingFadeTimerTimeout;
		SetProcess(enable: false);
	}

	private void EnsurePoolLifecyclePolicy()
	{
		if (!poolLifecyclePolicyInitialized)
		{
			poolLifecyclePolicyInitialized = true;
			Type type = GetType();
			supportsDirectPoolLifecycleDispatch = type == typeof(DamagePartDrop) && ObjectPoolLifecyclePolicy.HasBuiltInCSharpScript(this);
		}
	}

	public void Refresh()
	{
		BeginLease();
		base.Visible = false;
		AddToGroup(EffectGroupName, persistent: true);
		moveComponent?.MoveClear();
		SetPhysicsProcess(enable: false);
		SetProcess(enable: true);
		spriteNode.Position = Vector2.Zero;
		spriteNode.Rotation = 0f;
		spriteGroup.Position = Vector2.Zero;
		base.Scale = Vector2.One;
		base.Rotation = 0f;
		height = 100.0;
		initVelocity = Vector2.Zero;
		base.Modulate = new Color(base.Modulate);
		jumpSpeed = 300.0;
		jumpTime = 3;
		over = false;
		poolPushQueued = false;
		landingPlaneReady = false;
		landingWorldY = 0f;
		flightStateReady = false;
		flightWorldPosition = Vector2.Zero;
		shadowWorldPosition = Vector2.Zero;
		flightVelocity = Vector2.Zero;
		flightWorldTransform = Transform2D.Identity;
		flightRootWorldInverse = Transform2D.Identity;
		sprite = null;
	}

	public void Recycle()
	{
		InvalidateLease();
		base.Visible = false;
		RemoveFromGroup(EffectGroupName);
		moveComponent?.MoveClear();
		SetPhysicsProcess(enable: false);
		SetProcess(enable: false);
		over = true;
		poolPushQueued = false;
		landingPlaneReady = false;
		flightStateReady = false;
		flightVelocity = Vector2.Zero;
		if (GodotObject.IsInstanceValid(sprite))
		{
			if (GodotObject.IsInstanceValid(sprite.GetParent()))
			{
				sprite.GetParent().RemoveChild(sprite);
			}
			sprite.QueueFree();
		}
		sprite = null;
	}

	void IObjectPoolLifecycle.RefreshFromPool()
	{
		Refresh();
	}

	void IObjectPoolLifecycle.RecycleToPool()
	{
		Recycle();
	}

	public void Init(Node2D _sprite, double _height, Vector2 _initVelocity = default(Vector2), bool preserveSpritePosition = false)
	{
		if (GodotObject.IsInstanceValid(_sprite) && GodotObject.IsInstanceValid(spriteGroup))
		{
			sprite = _sprite;
			if (!preserveSpritePosition)
			{
				sprite.Position = Vector2.Zero;
			}
			spriteGroup.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			height = _height;
			initVelocity = _initVelocity;
			landingPlaneReady = false;
			flightStateReady = false;
			flightVelocity = initVelocity;
			moveComponent.MoveClear();
			SetPhysicsProcess(enable: true);
			SetProcess(enable: true);
		}
	}

	public void InitializeRenderBucket(Vector2I sourceGridPosition, Vector2 worldPosition)
	{
		Vector2I vector2I = sourceGridPosition;
		if (vector2I.Y < 0 && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(worldPosition - new Vector2(0f, 20f));
			if (mapGridPos.Y >= 0)
			{
				vector2I = mapGridPos;
			}
		}
		if (vector2I.Y >= 0)
		{
			base.gridPos = vector2I;
			FreshZIndex();
		}
		else
		{
			base.gridPos = new Vector2I(-1, -1);
			SetTransientRenderZIndex(-4095, "damage-part-invalid-grid");
		}
	}

	public override void _Process(double delta)
	{
		if (GodotObject.IsInstanceValid(shadowSprite) && !shadowSprite.RequiresLegacyRendering && shadowSprite.Visible && !(base.Modulate.A <= 0f))
		{
			TowerDefenseShadowMultiMeshRenderer.SubmitVisual(shadowSprite, base.gridPos.Y);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (over)
		{
			return;
		}
		EnsureLandingPlane();
		EnsureFlightState();
		float num = (float)delta;
		flightVelocity.Y += 1960f * num;
		flightWorldPosition += flightVelocity * num;
		shadowWorldPosition = new Vector2(flightWorldPosition.X, landingWorldY);
		shadowSprite.Position = flightRootWorldInverse * shadowWorldPosition;
		if (flightWorldPosition.Y > landingWorldY)
		{
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(shadowWorldPosition - new Vector2(0f, 20f));
			InitializeRenderBucket((mapGridPos.Y >= 0) ? mapGridPos : base.gridPos, shadowWorldPosition);
			cell = TowerDefenseManager.GetMapCell(base.gridPos);
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				ApplyFlightTransform(0f);
				CreateSplash();
				Over();
				return;
			}
			flightWorldPosition.Y = landingWorldY - 1f;
			ApplyFlightTransform(0f);
			jumpTime--;
			if (jumpTime > 0 && jumpSpeed >= 10.0)
			{
				flightVelocity = new Vector2(flightVelocity.X / 2f, (float)(0.0 - jumpSpeed));
				jumpSpeed /= 2.0;
				return;
			}
			moveComponent.MoveClear();
			flightVelocity = Vector2.Zero;
			over = true;
			SetPhysicsProcess(enable: false);
			ScheduleLandingFade(leaseVersion);
		}
		else
		{
			float rotationDelta = ((flightVelocity.X != 0f) ? (flightVelocity.X / 40f * num) : 0f);
			ApplyFlightTransform(rotationDelta);
		}
	}

	private void EnsureFlightState()
	{
		if (!flightStateReady)
		{
			flightWorldPosition = spriteNode.GlobalPosition;
			flightWorldTransform = spriteNode.GlobalTransform;
			flightRootWorldInverse = base.GlobalTransform.AffineInverse();
			flightStateReady = true;
		}
	}

	private void ApplyFlightTransform(float rotationDelta)
	{
		if (rotationDelta != 0f)
		{
			float num = Mathf.Sin(rotationDelta);
			float num2 = Mathf.Cos(rotationDelta);
			Vector2 x = flightWorldTransform.X;
			Vector2 y = flightWorldTransform.Y;
			flightWorldTransform.X = new Vector2(x.X * num2 - x.Y * num, x.X * num + x.Y * num2);
			flightWorldTransform.Y = new Vector2(y.X * num2 - y.Y * num, y.X * num + y.Y * num2);
		}
		flightWorldTransform.Origin = flightWorldPosition;
		spriteNode.Transform = flightRootWorldInverse * flightWorldTransform;
	}

	private void EnsureLandingPlane()
	{
		if (!landingPlaneReady)
		{
			landingWorldY = base.GlobalPosition.Y + (float)height;
			landingPlaneReady = true;
		}
	}

	public void Over()
	{
		if (!poolPushQueued)
		{
			poolPushQueued = true;
			InvalidateLease();
			moveComponent?.MoveClear();
			flightVelocity = Vector2.Zero;
			SetPhysicsProcess(enable: false);
			over = true;
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.damagePart, this);
		}
	}

	public TowerDefenseEffectSpriteOnce CreateSplash()
	{
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return null;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, node2D) as TowerDefenseEffectSpriteOnce;
		if (!GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
		{
			return null;
		}
		towerDefenseEffectSpriteOnce.gridPos = base.gridPos;
		towerDefenseEffectSpriteOnce.GlobalPosition = shadowWorldPosition - new Vector2(0f, 20f);
		return towerDefenseEffectSpriteOnce;
	}

	private void BeginLease()
	{
		InvalidateLease();
		poolPushQueued = false;
	}

	private void InvalidateLease()
	{
		leaseVersion++;
		landingFadeLeaseVersion = -1;
		CancelLandingFadeTimer();
		KillTween(ref landingFadeTween);
	}

	private void ScheduleLandingFade(int currentLease)
	{
		CancelLandingFadeTimer();
		landingFadeLeaseVersion = currentLease;
		landingFadeTimer = GetTree().CreateTimer(0.5, processAlways: false);
		landingFadeTimer.Timeout += landingFadeTimerCallback;
	}

	private void OnLandingFadeTimerTimeout()
	{
		if (GodotObject.IsInstanceValid(landingFadeTimer))
		{
			landingFadeTimer.Timeout -= landingFadeTimerCallback;
		}
		landingFadeTimer = null;
		if (landingFadeLeaseVersion == leaseVersion && IsInsideTree())
		{
			KillTween(ref landingFadeTween);
			landingFadeTween = CreateTween();
			landingFadeTween.TweenProperty(this, "modulate:a", 0.0, 0.1);
			landingFadeTween.Finished += OnLandingFadeFinished;
		}
	}

	private void OnLandingFadeFinished()
	{
		landingFadeTween = null;
		if (landingFadeLeaseVersion == leaseVersion)
		{
			Over();
		}
	}

	private void CancelLandingFadeTimer()
	{
		if (GodotObject.IsInstanceValid(landingFadeTimer) && landingFadeTimerCallback != null)
		{
			landingFadeTimer.Timeout -= landingFadeTimerCallback;
		}
		landingFadeTimer = null;
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
