using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseProjectile : TowerDefenseGroundItemBase
{
	public delegate void LandOverEventHandler(Vector2 pos, Vector2I gridPos);

	public Node2D projectileBodyNode;

	public TowerDefenseShadowVisual shadowSprite;

	public AabbArea2D hitBox;

	public bool over;

	public Vector2 rememberPos = Vector2.Zero;

	public Variant metaData;

	internal BulletFieldStoredProjectile[] eventProjectiles;

	public bool checkAll;

	public bool suppressGameplay;

	private bool _useFall;

	private bool _useGravity;

	public TowerDefenseCharacter fireCharacter;

	public Vector2 velocity = Vector2.Zero;

	public double speed;

	public TowerDefenseProjectileConfig config;

	public TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL;

	public double damage = 20.0;

	public int collisionFlags;

	public int damageFlags;

	public int fireMethodFlags;

	public TowerDefenseEnum.CHARACTER_HEIGHT projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;

	public bool checkHeight;

	public double height;

	public TowerDefenseCharacter target;

	public TowerDefenseCharacter magneticTarget;

	public double catapultTime;

	public double catapultTimer;

	public Vector2 catapultTargetPos = Vector2.Zero;

	public Vector2 catapultControlPoint = Vector2.Zero;

	public bool catapulCheckLast;

	public int penetrateNum;

	public Node2D projectileSprite;

	public bool hitOver;

	public Vector2 savePos = Vector2.Zero;

	public double checkDistance;

	public double fireLength = -1.0;

	public Rect2 rect;

	private readonly HashSet<ulong> _penetratingTargetIds = new HashSet<ulong>();

	private readonly HashSet<ulong> _penetratingTargetIdsThisFrame = new HashSet<ulong>();

	private readonly List<ulong> _penetratingTargetIdsToRemove = new List<ulong>();

	private readonly List<TowerDefenseCharacter> _hitCandidates = new List<TowerDefenseCharacter>();

	public bool trackOpen;

	public bool catapultOpen;

	public Vector2 shadowScaleSave = Vector2.One;

	public bool initSet;

	public Array<AabbArea2D> initHitList;

	public int setZInterval = 2;

	public Tween moveTween;

	public int randFreshIndex;

	private bool _gridRefreshScheduleInitialized;

	private ulong _nextGridRefreshFrame;

	private bool _cellGroundHeightCacheInitialized;

	private bool _cellGroundHeightCacheValid;

	private TowerDefenseCellInstance _cellGroundHeightCacheCell;

	private CurveTexture _cellGroundHeightCacheCurve;

	private double _cellGroundHeightCacheValue;

	public int fireDirX = 1;

	public bool isShooter;

	public int extId = -1;

	public bool blocked;

	public int trackCacheFrame = -1;

	public TowerDefenseCharacter trackCacheTarget;

	public bool trackCacheValid;

	public int trackSearchInterval = 15;

	public int trackNoTargetInterval = 60;

	public int trackConsecutiveNoTarget;

	public int trackMaxConsecutiveNoTarget = 8;

	public bool trackForceSearch;

	public static int trackSearchBudget = 10;

	public static int trackSearchBudgetUsed = 0;

	public static int trackSearchBudgetFrame = -1;

	public static int effectSpawnBudget = 50;

	public static int effectSpawnUsed = 0;

	public static long effectSpawnFrame = -1L;

	private bool _cacheRotateFollowVelocity;

	private float _cacheRotateScale;

	private bool _monitoringState;

	private bool _monitorableState;

	private bool _processModeEnabled;

	private bool _bodyCleaned;

	private bool _initPending;

	public bool useFall
	{
		get
		{
			return _useFall;
		}
		set
		{
			_useFall = value;
			if (_useFall)
			{
				isGround = false;
				ApplyFallVisual();
			}
		}
	}

	public bool useGravity
	{
		get
		{
			return _useGravity;
		}
		set
		{
			_useGravity = value;
			if (_useGravity)
			{
				ApplyFallVisual();
			}
		}
	}

	public Rect2 WorldHitRect
	{
		get
		{
			if (!GodotObject.IsInstanceValid(hitBox))
			{
				return AabbShapeUtil.RectFromCenter(base.GlobalPosition, AabbShapeUtil.DefaultAreaSize);
			}
			return AabbShapeUtil.ComputeAreaWorldRect(hitBox);
		}
	}

	public event LandOverEventHandler OnLandOver;

	private bool IsHitBoxOutOfBounds()
	{
		return !AabbShapeUtil.Intersects(rect, WorldHitRect);
	}

	private static bool _CanSpawnEffectThisFrame()
	{
		long physicsFrames = (long)Engine.GetPhysicsFrames();
		if (physicsFrames != effectSpawnFrame)
		{
			effectSpawnFrame = physicsFrames;
			effectSpawnUsed = 0;
		}
		if (effectSpawnUsed >= effectSpawnBudget)
		{
			return false;
		}
		effectSpawnUsed++;
		return true;
	}

	private static void EndGodotArrayProjectileMetric(long startTicks, int items)
	{
		TowerDefensePerfProfiler.End("godotArray.projectile", startTicks, items);
	}

	private void SetMonitorable(bool value)
	{
		if (_monitorableState != value)
		{
			hitBox.SetDeferred("monitorable", value);
			_monitorableState = value;
		}
	}

	private void SetHitBoxProcessMode(bool enabled)
	{
		if (_processModeEnabled != enabled)
		{
			hitBox.SetDeferred("process_mode", (!enabled) ? 4 : 0);
			_processModeEnabled = enabled;
		}
	}

	private void RecycleBodyChildren()
	{
		foreach (Node child in projectileBodyNode.GetChildren())
		{
			if (child.HasMeta("_projectile_pool_scene"))
			{
				TowerDefenseProjectilePool.Push(child);
			}
			else if (config != null && config.projectileObject != ObjectManagerConfig.OBJECT.NOONE)
			{
				ObjectManager.PoolPush(config.projectileObject, child);
			}
			else
			{
				child.QueueFree();
			}
		}
	}

	private void ApplyFallVisual()
	{
		projectileBodyNode.Rotation = new Vector2(velocity.X, (float)ySpeed).Angle();
		shadowSprite.Position = new Vector2(shadowSprite.Position.X, (float)height);
		shadowSprite.Scale = Vector2.Zero;
	}

	public void Refresh()
	{
		ResetPhysicsInterpolation();
		AddToGroup("Projectile", persistent: true);
		metaData = default(Variant);
		eventProjectiles = null;
		over = false;
		foreach (StringName meta in GetMetaList())
		{
			RemoveMeta(meta);
		}
		gravityUse = true;
		ySpeed = 0.0;
		base.z = 0.0;
		base.Scale = Vector2.One;
		checkAll = false;
		suppressGameplay = false;
		useFall = false;
		useGravity = false;
		fireCharacter = null;
		projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;
		_gridRefreshScheduleInitialized = false;
		_cellGroundHeightCacheInitialized = false;
		_cellGroundHeightCacheValid = false;
		_cellGroundHeightCacheCell = null;
		_cellGroundHeightCacheCurve = null;
		_cellGroundHeightCacheValue = 0.0;
		target = null;
		magneticTarget = null;
		projectileSprite = null;
		fireLength = -1.0;
		projectileBodyNode.Rotation = 0f;
		projectileBodyNode.Scale = new Vector2(1f, projectileBodyNode.Scale.Y);
		projectileBodyNode.Position = Vector2.Zero;
		shadowSprite.Visible = true;
		shadowSprite.Scale = Vector2.Zero;
		shadowSprite.Position = new Vector2(shadowSprite.Position.X, 20f);
		hitBox.Scale = Vector2.One;
		SetMonitoring(value: false);
		SetMonitorable(value: true);
		trackOpen = false;
		catapultOpen = false;
		isShooter = false;
		extId = -1;
		blocked = false;
		trackCacheFrame = -1;
		trackCacheTarget = null;
		trackCacheValid = false;
		trackConsecutiveNoTarget = 0;
		this.OnLandOver = null;
		_penetratingTargetIds.Clear();
		_penetratingTargetIdsThisFrame.Clear();
		_penetratingTargetIdsToRemove.Clear();
		moveTween = null;
		hitOver = false;
		_cacheRotateFollowVelocity = false;
		_cacheRotateScale = 0f;
		if (!_bodyCleaned)
		{
			RecycleBodyChildren();
		}
		_bodyCleaned = false;
		SetHitBoxProcessMode(enabled: true);
	}

	public void Recycle()
	{
		if (GodotObject.IsInstanceValid(moveTween) && moveTween.IsRunning())
		{
			moveTween.Kill();
		}
		SetMonitoring(value: false);
		SetMonitorable(value: false);
		SetHitBoxProcessMode(enabled: false);
		hitOver = true;
		RecycleBodyChildren();
		_bodyCleaned = true;
		RemoveFromGroup("Projectile");
		ySpeed = 0.0;
	}

	public void Init(TowerDefenseCharacter _fireCharacter, Vector2 _velocity, TowerDefenseProjectileConfig _config, int _collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP _camp = TowerDefenseEnum.CHARACTER_CAMP.ALL, double _height = 0.0, TowerDefenseCharacter _target = null, bool isChange = false)
	{
		_penetratingTargetIds.Clear();
		_penetratingTargetIdsThisFrame.Clear();
		_penetratingTargetIdsToRemove.Clear();
		config = _config;
		if (config == null)
		{
			Over();
			return;
		}
		_cacheRotateFollowVelocity = config.rotateFollowVelocity;
		_cacheRotateScale = (float)config.rotateScale;
		projectileSprite = TowerDefenseProjectilePool.Pop(config.projectileScene, projectileBodyNode);
		projectileSprite.Scale = config.scale;
		fireCharacter = _fireCharacter;
		camp = _camp;
		velocity = _velocity;
		speed = velocity.Length();
		target = _target;
		fireDirX = Mathf.Sign(velocity.X);
		shadowSprite.Scale = config.size / new Vector2(65f, 65f);
		shadowScaleSave = shadowSprite.Scale;
		if (!isChange)
		{
			rememberPos = base.GlobalPosition;
			height = _height;
			if (_collisionFlags == -1)
			{
				if (GodotObject.IsInstanceValid(fireCharacter))
				{
					collisionFlags = fireCharacter.instance.collisionFlags;
				}
				else
				{
					collisionFlags = config.collisionFlags;
				}
			}
			else
			{
				collisionFlags = _collisionFlags;
			}
			projectileBodyNode.Rotation = 0f;
			if (GodotObject.IsInstanceValid(fireCharacter))
			{
				shadowSprite.Position = new Vector2(shadowSprite.Position.X, 0f - (float)fireCharacter.GetGroundHeight(base.GlobalPosition.Y));
			}
			else
			{
				shadowSprite.Position = new Vector2(shadowSprite.Position.X, 20f);
			}
			projectileBodyNode.Position = new Vector2(projectileBodyNode.Position.X, 0f - (float)height);
			hitBox.Position = new Vector2(hitBox.Position.X, -20f);
			savePos = shadowSprite.GlobalPosition;
			if (GodotObject.IsInstanceValid(fireCharacter))
			{
				projectileHeight = (TowerDefenseEnum.CHARACTER_HEIGHT)Mathf.Min(2, (int)fireCharacter.instance.height);
			}
			checkHeight = false;
		}
		else
		{
			projectileBodyNode.Position = new Vector2(projectileBodyNode.Position.X, 0f - (float)height);
			hitBox.Position = new Vector2(hitBox.Position.X, -20f);
		}
		damage = config.baseDamage;
		damageFlags = config.damageFlags;
		fireMethodFlags = config.fireMethodFlags;
		isShooter = (fireMethodFlags & 1) != 0 && !useGravity && !useFall;
		if ((fireMethodFlags & 0x20) != 0)
		{
			SetTrack(isChange);
		}
		if ((fireMethodFlags & 4) != 0)
		{
			penetrateNum = config.penetrateNum;
		}
		if ((fireMethodFlags & 2) != 0)
		{
			Vector2 vector = new Vector2((float)TowerDefenseManager.Instance.GetMapGroundRight(), base.GlobalPosition.Y);
			if (GodotObject.IsInstanceValid(target))
			{
				Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
				catapultTargetPos = logicalGlobalPosition + new Vector2(-10f * target.Scale.X * target.spriteGroup.Scale.X, 0f);
			}
			else
			{
				catapultTargetPos = vector + new Vector2(60f, 0f);
			}
			shadowSprite.Visible = true;
			shadowSprite.Scale = Vector2.Zero;
			double num = 2000.0;
			double catapultHeight = config.catapultHeight;
			double num2 = Mathf.Max(height, 0.0);
			ySpeed = 0.0 - Mathf.Sqrt(2.0 * num * catapultHeight);
			double num3 = Mathf.Sqrt(2.0 * catapultHeight / num);
			double num4 = Mathf.Sqrt(2.0 * (num2 + catapultHeight) / num);
			double num5 = num3 + num4;
			velocity = (catapultTargetPos - base.GlobalPosition) / (float)num5;
			base.z = num2;
			isGround = false;
			catapultTime = num5;
			catapultTimer = 0.0;
			catapultControlPoint = Vector2.Zero;
			catapulCheckLast = false;
			catapultOpen = true;
		}
		_initPending = true;
	}

	private void CompleteInit()
	{
		if (fireLength != -1.0)
		{
			checkDistance = (double)TowerDefenseManager.Instance.GetMapGridSize().X * fireLength;
		}
		if (GodotObject.IsInstanceValid(target) || useFall || useGravity)
		{
			SetMonitoring(value: false);
		}
		else
		{
			SetMonitoring(value: true);
		}
		SetMonitorable(value: true);
	}

	public void Change(TowerDefenseProjectileConfig _config, StringName projectileName, StringName changeprojectileName, TowerDefenseCharacter changeCharacter)
	{
		if (!GodotObject.IsInstanceValid(fireCharacter))
		{
			fireCharacter = null;
		}
		if (!GodotObject.IsInstanceValid(target))
		{
			target = null;
		}
		RecycleBodyChildren();
		extId = -1;
		Init(fireCharacter, velocity, _config, collisionFlags, camp, height, target, isChange: true);
	}

	public override void _Ready()
	{
		projectileBodyNode = GetNode<Node2D>("%ProjectileBodyNode");
		shadowSprite = TowerDefenseShadowVisual.Capture(this, GetNodeOrNull<Sprite2D>("%ShadowSprite"));
		hitBox = GetNode<AabbArea2D>("%HitBox");
		randFreshIndex = (int)GD.Randi();
		rect = FireComponent.ComputeProjectileMapRect();
		base.OnLand += Land;
		SetPhysicsProcess(enable: false);
	}

	public void Update(float delta, ulong currentFrame)
	{
		if (over)
		{
			return;
		}
		if (_initPending)
		{
			_initPending = false;
			if (!over)
			{
				CompleteInit();
			}
			return;
		}
		if (trackOpen && extId >= 0)
		{
			ProcessTrackCheck();
			return;
		}
		if (!_cacheRotateFollowVelocity)
		{
			if (_cacheRotateScale != 0f)
			{
				projectileSprite.Rotation += delta * _cacheRotateScale * (float)fireDirX;
			}
		}
		else if (!catapultOpen)
		{
			projectileBodyNode.Rotation = velocity.Angle();
		}
		if (isShooter && !useFall && !useGravity && extId < 0 && !trackOpen)
		{
			ProcessShooter(delta, currentFrame);
			return;
		}
		if (useGravity)
		{
			PhysiceUpdate(delta);
			hitBox.Position = new Vector2(hitBox.Position.X, 0f - (float)base.z);
			shadowSprite.Scale = shadowScaleSave * (float)Mathf.Max(1.0 - base.z / 600.0, 0.0);
			if (_cacheRotateFollowVelocity)
			{
				Vector2 vector = new Vector2(velocity.X, (float)ySpeed);
				if ((double)vector.Length() > 0.1)
				{
					projectileBodyNode.Rotation = vector.Angle();
				}
			}
			if (ySpeed >= 0.0 && base.z - base.groundHeight < 60.0)
			{
				SetMonitoring(value: true);
			}
			if (base.z <= base.groundHeight)
			{
				SetMonitoring(value: false);
				Land();
				return;
			}
		}
		if (useFall)
		{
			if (ShouldRefreshGrid(currentFrame))
			{
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
			}
			if (TryGetCachedCellGroundHeight(out var cellGroundHeight) && base.groundHeight != cellGroundHeight)
			{
				base.groundHeight = cellGroundHeight;
			}
			shadowSprite.Position = new Vector2(shadowSprite.Position.X, 0f - (float)base.groundHeight);
			if (!(base.z > base.groundHeight))
			{
				SetMonitoring(value: false);
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
				Land();
				return;
			}
			ySpeed += gravity * gravityScale * (double)delta;
			base.z -= ySpeed * (double)delta;
			SetMonitoring(value: false);
			if (base.z - base.groundHeight <= 100.0)
			{
				SetMonitoring(value: true);
				hitBox.ProcessMode = ProcessModeEnum.Inherit;
			}
		}
		if (trackOpen)
		{
			ProcessTrack(delta, currentFrame);
		}
		if (!catapultOpen)
		{
			Move(delta);
		}
		if (fireLength != -1.0 && (double)savePos.DistanceSquaredTo(base.GlobalPosition) > checkDistance * checkDistance)
		{
			Over();
			return;
		}
		if (catapultOpen)
		{
			ProcessCatapult(delta, currentFrame);
		}
		CheckAabbHits();
	}

	private void SetMonitoring(bool value)
	{
		if (_monitoringState != value)
		{
			hitBox.SetDeferred("monitoring", value);
			_monitoringState = value;
		}
	}

	private bool CanProcessHit()
	{
		if (hitOver)
		{
			return false;
		}
		if (catapultOpen && (ySpeed < 0.0 || base.z - base.groundHeight > 60.0))
		{
			return false;
		}
		if (trackOpen)
		{
			return false;
		}
		if (!_monitoringState && !hitBox.Monitoring)
		{
			return false;
		}
		if (!_processModeEnabled && hitBox.ProcessMode == ProcessModeEnum.Disabled)
		{
			return false;
		}
		if (useGravity && (ySpeed < 0.0 || base.z - base.groundHeight > 60.0))
		{
			return false;
		}
		return true;
	}

	private bool ShouldExpireTrackWithoutTarget()
	{
		if (trackOpen && !hitOver && trackMaxConsecutiveNoTarget > 0 && trackConsecutiveNoTarget >= trackMaxConsecutiveNoTarget)
		{
			return !GodotObject.IsInstanceValid(target);
		}
		return false;
	}

	private bool CanHitCharacter(TowerDefenseCharacter tdChar)
	{
		if (!GodotObject.IsInstanceValid(tdChar))
		{
			return false;
		}
		if (ShouldFilterCollisionLine() && !tdChar.IsTargetableFromLine(base.gridPos.Y))
		{
			return false;
		}
		if (!tdChar.targetRegistrationComponent.canProjectileCheck)
		{
			return false;
		}
		if (!tdChar.instance.canBeCollection)
		{
			return false;
		}
		if ((collisionFlags & tdChar.instance.maskFlags) == 0)
		{
			return false;
		}
		if (!CanTarget(tdChar))
		{
			return false;
		}
		if (checkHeight && projectileHeight > tdChar.instance.height)
		{
			return false;
		}
		return true;
	}

	private static bool IsValidMapLine(int line)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.Y <= 0)
		{
			return line != -2147483648;
		}
		if (line >= 1)
		{
			return line <= instance.gridNum.Y;
		}
		return false;
	}

	private bool ShouldFilterCollisionLine()
	{
		if (!checkAll)
		{
			return IsValidMapLine(base.gridPos.Y);
		}
		return false;
	}

	private bool IsPenetrateDamage()
	{
		if ((fireMethodFlags & 4) != 0)
		{
			return !trackOpen;
		}
		return false;
	}

	private void BeginPenetrateOverlapScan()
	{
		if (IsPenetrateDamage())
		{
			_penetratingTargetIdsThisFrame.Clear();
		}
	}

	private void EndPenetrateOverlapScan()
	{
		if (!IsPenetrateDamage())
		{
			return;
		}
		_penetratingTargetIdsToRemove.Clear();
		foreach (ulong penetratingTargetId in _penetratingTargetIds)
		{
			if (!_penetratingTargetIdsThisFrame.Contains(penetratingTargetId))
			{
				_penetratingTargetIdsToRemove.Add(penetratingTargetId);
			}
		}
		foreach (ulong item in _penetratingTargetIdsToRemove)
		{
			_penetratingTargetIds.Remove(item);
		}
		_penetratingTargetIdsToRemove.Clear();
	}

	private bool TryRegisterPenetrateHit(TowerDefenseCharacter character)
	{
		if (!IsPenetrateDamage())
		{
			return true;
		}
		ulong instanceId = character.GetInstanceId();
		_penetratingTargetIdsThisFrame.Add(instanceId);
		if (_penetratingTargetIds.Contains(instanceId))
		{
			return false;
		}
		_penetratingTargetIds.Add(instanceId);
		return true;
	}

	private void CheckAabbHits()
	{
		if (!CanProcessHit())
		{
			return;
		}
		BeginPenetrateOverlapScan();
		try
		{
			Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(hitBox);
			bool flag = ShouldFilterCollisionLine();
			int line = (flag ? base.gridPos.Y : (-2147483648));
			_hitCandidates.Clear();
			_hitCandidates.AddRange(TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(checkRect, line, flag));
			foreach (TowerDefenseCharacter hitCandidate in _hitCandidates)
			{
				if (!CanProcessHit())
				{
					break;
				}
				if (CanHitCharacter(hitCandidate))
				{
					HitCharacter(hitCandidate);
				}
			}
		}
		finally
		{
			EndPenetrateOverlapScan();
		}
	}

	public void ProcessShooter(float delta, ulong currentFrame)
	{
		if (ShouldRefreshGrid(currentFrame))
		{
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
		}
		if (CheckShooterHeight())
		{
			return;
		}
		if (extId < 0)
		{
			base.GlobalPosition += velocity * delta;
			if (IsHitBoxOutOfBounds())
			{
				Over();
				return;
			}
			if (fireLength != -1.0 && (double)savePos.DistanceSquaredTo(base.GlobalPosition) > checkDistance * checkDistance)
			{
				Over();
			}
		}
		CheckAabbHits();
	}

	public void ProcessTrack(float delta, ulong currentFrame)
	{
		bool flag = GodotObject.IsInstanceValid(target);
		if (flag && !target.targetRegistrationComponent.canProjectileCheck)
		{
			flag = false;
		}
		if (flag && (target.nearDie || target.die || !CanTarget(target) || !CanCollision(target.instance.maskFlags)))
		{
			flag = false;
		}
		if (flag && !target.IsHitBoxEnabled)
		{
			flag = false;
		}
		if (!flag)
		{
			target = null;
			FindTrackTargetCached();
		}
		int num = (GodotObject.IsInstanceValid(target) ? trackSearchInterval : trackNoTargetInterval);
		if (currentFrame % (ulong)num == 0L)
		{
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
			FindTrackTargetCached();
		}
		if (ShouldExpireTrackWithoutTarget())
		{
			Over();
		}
		else if (GodotObject.IsInstanceValid(target))
		{
			Vector2 globalPosition = base.GlobalPosition;
			Vector2 globalPositionForPhysicsFrame = target.GetGlobalPositionForPhysicsFrame(currentFrame);
			Vector2 vector = (globalPositionForPhysicsFrame - globalPosition).Normalized();
			projectileBodyNode.Rotation = (float)Mathf.LerpAngle(projectileBodyNode.Rotation, vector.Angle(), (double)delta * 5.0);
			velocity = vector * (float)speed;
			if (Geometry2D.IsPointInCircle(globalPosition, globalPositionForPhysicsFrame, 30f))
			{
				HitCharacter(target);
			}
		}
		else
		{
			velocity = new Vector2(Mathf.Cos(projectileBodyNode.Rotation), Mathf.Sin(projectileBodyNode.Rotation)) * (float)speed;
			if (IsHitBoxOutOfBounds())
			{
				Over();
			}
		}
	}

	public void ProcessTrackCheck()
	{
		if (over)
		{
			return;
		}
		int num = (GodotObject.IsInstanceValid(target) ? trackSearchInterval : trackNoTargetInterval);
		if ((ulong)((long)Engine.GetPhysicsFrames() + (long)randFreshIndex) % (ulong)num == 0L)
		{
			bool flag = GodotObject.IsInstanceValid(target);
			if (flag && !target.targetRegistrationComponent.canProjectileCheck)
			{
				flag = false;
			}
			if (flag && (target.nearDie || target.die || !CanTarget(target) || !CanCollision(target.instance.maskFlags)))
			{
				flag = false;
			}
			if (flag && !target.IsHitBoxEnabled)
			{
				flag = false;
			}
			if (!flag)
			{
				target = null;
				FindTrackTargetCached();
			}
			if (ShouldExpireTrackWithoutTarget())
			{
				Over();
			}
			else if (!GodotObject.IsInstanceValid(target) && IsHitBoxOutOfBounds())
			{
				Over();
			}
		}
	}

	public void ProcessCatapult(float delta)
	{
		ProcessCatapult(delta, Engine.GetPhysicsFrames());
	}

	private void ProcessCatapult(float delta, ulong physicsFrame)
	{
		catapultTimer += delta;
		double num = 2000.0;
		ySpeed += num * (double)delta;
		base.z -= ySpeed * (double)delta;
		base.GlobalPosition += velocity * delta;
		projectileBodyNode.Position = new Vector2(projectileBodyNode.Position.X, 0f - (float)base.z);
		shadowSprite.Scale = shadowScaleSave * (float)Mathf.Max(1.0 - base.z / 600.0, 0.0);
		hitBox.Position = new Vector2(hitBox.Position.X, 0f - (float)base.z);
		if (_cacheRotateFollowVelocity)
		{
			Vector2 vector = new Vector2(velocity.X, (float)ySpeed);
			if ((double)vector.Length() > 0.1)
			{
				projectileBodyNode.Rotation = vector.Angle();
			}
		}
		if (ySpeed > 0.0 && base.z - base.groundHeight < 100.0 && !blocked)
		{
			SetMonitoring(value: true);
			if (!hitOver && GodotObject.IsInstanceValid(target) && !target.die && target.targetRegistrationComponent.canProjectileCheck && CanTarget(target))
			{
				Vector2 globalPositionForPhysicsFrame = target.GetGlobalPositionForPhysicsFrame(physicsFrame);
				if (base.GlobalPosition.DistanceSquaredTo(globalPositionForPhysicsFrame) < 625f)
				{
					HitCharacter(target);
					return;
				}
			}
		}
		if (base.z <= base.groundHeight && ySpeed >= 0.0)
		{
			base.z = base.groundHeight;
			ySpeed = 0.0;
			isGround = true;
			Land();
		}
	}

	public void Move(float delta)
	{
		base.GlobalPosition += velocity * delta;
		if (!useFall && !useGravity && IsHitBoxOutOfBounds())
		{
			Over();
		}
	}

	public override void SetZ()
	{
		projectileBodyNode.Position = new Vector2(projectileBodyNode.Position.X, 0f - (float)base.z);
		shadowSprite.Scale = shadowScaleSave * (float)Mathf.Max(1.0 - base.z / 600.0, 0.0);
	}

	public void HitCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		if (character is TowerDefensePlant)
		{
			TowerDefenseCellInstance towerDefenseCellInstance = character.cell;
			if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
			{
				character = towerDefenseCellInstance.GetTarget(collisionFlags, camp, checkInvincible: true, catapultOpen);
			}
		}
		if (!GodotObject.IsInstanceValid(character) || !TryRegisterPenetrateHit(character))
		{
			return;
		}
		if (suppressGameplay)
		{
			HitEffect(character);
			Over();
			return;
		}
		character.ProjectileHurt(this, config);
		foreach (TowerDefenseCharacterEventBase hitTargetEvent in config.hitTargetEventList)
		{
			hitTargetEvent.ExecuteProject(this, character);
		}
		Vector2 globalPosition = base.GlobalPosition;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		if (!character.HasShield() || (damageFlags & 1) == 0 || ((fireMethodFlags & 1) != 0 && (((double)character.Scale.X > 0.0 && globalPosition.X > logicalGlobalPosition.X + 30f) || ((double)character.Scale.X < 0.0 && globalPosition.X < logicalGlobalPosition.X - 30f))) || (fireMethodFlags & 2) != 0)
		{
			if (!character.ProjectileEffectsBlocked(damageFlags, fireMethodFlags))
			{
				foreach (TowerDefenseCharacterEventBase hitCharacterEvent in config.hitCharacterEventList)
				{
					hitCharacterEvent.ExecuteProject(this, character);
				}
			}
			if (config.useRange)
			{
				Vector2 mapCellPosCenter = TowerDefenseManager.Instance.GetMapCellPosCenter(TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition));
				mapCellPosCenter = new Vector2(logicalGlobalPosition.X, mapCellPosCenter.Y);
				TowerDefenseExplode.CreateProjectileExplodeSingleExclude(mapCellPosCenter, config, character, camp, useFall && config.UsesExplosionDamage);
			}
		}
		HitEffectWithCharacterPosition(character, logicalGlobalPosition, hasCharacterPosition: true);
		bool flag = true;
		if ((fireMethodFlags & 4) != 0 && !trackOpen)
		{
			if (config.penetrateNum != -1)
			{
				penetrateNum--;
				if (penetrateNum > 0)
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			hitOver = true;
			Over();
		}
	}

	public void HitCheck(AabbArea2D area)
	{
		if (!CanProcessHit())
		{
			return;
		}
		Node parent = area.GetParent();
		if (parent is TowerDefenseCharacter)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)parent;
			if (CanHitCharacter(towerDefenseCharacter))
			{
				HitCharacter(towerDefenseCharacter);
			}
		}
	}

	public void HitEffect(TowerDefenseCharacter character)
	{
		bool flag = GodotObject.IsInstanceValid(character);
		Vector2 characterPosition = (flag ? character.GetLogicalGlobalPosition() : default(Vector2));
		HitEffectWithCharacterPosition(character, characterPosition, flag);
	}

	private void HitEffectWithCharacterPosition(TowerDefenseCharacter character, Vector2 characterPosition, bool hasCharacterPosition)
	{
		bool num = _CanSpawnEffectThisFrame();
		if (num && config.hitEffect != null)
		{
			Vector2 globalPosition = hitBox.GlobalPosition;
			Vector2I gridPosition = (GodotObject.IsInstanceValid(character) ? character.gridPos : Vector2I.Zero);
			if (!GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode) || !TowerDefenseManager.TryCreateEffectSceneOnceFast(config.hitEffect, TowerDefenseGroundItemBase.characterNode, gridPosition, globalPosition, preferSprite: false))
			{
				Node node = config.hitEffect.Instantiate(PackedScene.GenEditState.Disabled);
				Node node2;
				if (node is GPUParticles2DOnece scene)
				{
					node2 = TowerDefenseManager.Instance.CreateEffectParticlesSceneOnce(scene, gridPosition);
				}
				else
				{
					node2 = node;
					if (node2 is TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase)
					{
						towerDefenseProjectileEffectBase.suppressDeathrattles = (damageFlags & 0x20) != 0;
						if (GodotObject.IsInstanceValid(character))
						{
							towerDefenseProjectileEffectBase.Init(base.gridPos, camp, collisionFlags, character, character.groundHeight);
						}
						else if (GodotObject.IsInstanceValid(cell))
						{
							towerDefenseProjectileEffectBase.Init(base.gridPos, camp, collisionFlags, null, cell.GetGroundHeight());
						}
					}
				}
				if (node2 is Node2D node2D && GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
				{
					node2D.GlobalPosition = globalPosition;
					TowerDefenseGroundItemBase.characterNode.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				}
				else
				{
					node2.QueueFree();
				}
			}
		}
		if (num && TowerDefenseManager.Instance.GetEffectCount() < 100)
		{
			CreatSplatWithCharacterPosition(character, characterPosition, hasCharacterPosition);
		}
		if (config.splatAudio != "SplatNormal" || (character != null && !character.instance.HasAnyArmor))
		{
			PlaySplat();
		}
	}

	public void CreatSplat(TowerDefenseCharacter character)
	{
		bool flag = GodotObject.IsInstanceValid(character);
		Vector2 characterPosition = (flag ? character.GetLogicalGlobalPosition() : default(Vector2));
		CreatSplatWithCharacterPosition(character, characterPosition, flag);
	}

	private void CreatSplatWithCharacterPosition(TowerDefenseCharacter character, Vector2 characterPosition, bool hasCharacterPosition)
	{
		if (config.splatScene == null)
		{
			return;
		}
		int y = character?.gridPos.Y ?? base.gridPos.Y;
		Vector2 globalPosition = ((!config.hitBody || !hasCharacterPosition) ? projectileBodyNode.GlobalPosition : (characterPosition - new Vector2(0f, 20f)));
		if (!GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode) || !TowerDefenseManager.TryCreateEffectSceneOnceFast(config.splatScene, TowerDefenseGroundItemBase.characterNode, new Vector2I(base.gridPos.X, y), globalPosition, string.Equals(config.splatSceneType, "Sprite", StringComparison.OrdinalIgnoreCase)))
		{
			Node node = config.splatScene.Instantiate(PackedScene.GenEditState.Disabled);
			TowerDefenseEffectBase towerDefenseEffectBase = null;
			if (node is GPUParticles2DOnece scene)
			{
				towerDefenseEffectBase = TowerDefenseManager.Instance.CreateEffectParticlesSceneOnce(scene, base.gridPos);
			}
			else if (node is AdobeAnimateSprite scene2)
			{
				towerDefenseEffectBase = TowerDefenseManager.Instance.CreateEffectSpriteSceneOnce(scene2, base.gridPos);
			}
			else if (node is TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce)
			{
				towerDefenseEffectBase = towerDefenseEffectSpriteOnce;
			}
			else if (node is TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce)
			{
				towerDefenseEffectBase = towerDefenseEffectParticlesOnce;
			}
			if (towerDefenseEffectBase == null)
			{
				node.QueueFree();
				return;
			}
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectBase, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectBase.gridPos = new Vector2I(towerDefenseEffectBase.gridPos.X, y);
			towerDefenseEffectBase.GlobalPosition = globalPosition;
		}
	}

	public void PlaySplat()
	{
		AudioManager.Instance.AudioPlay(config.splatAudio);
	}

	public bool CanCollision(int maskFlags)
	{
		return (maskFlags & collisionFlags) != 0;
	}

	public bool CanTarget(TowerDefenseCharacter character)
	{
		if (fireCharacter is TowerDefenseZombie && character is TowerDefenseGravestone)
		{
			return false;
		}
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

	public void Land()
	{
		base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
		if (!blocked)
		{
			if (!suppressGameplay)
			{
				foreach (TowerDefenseCharacterEventBase hitGroundEvent in config.hitGroundEventList)
				{
					hitGroundEvent.ExecuteGroundProject(this, base.GlobalPosition, base.gridPos);
				}
				if (config.useRange)
				{
					TowerDefenseExplode.CreateProjectileExplode(base.GlobalPosition, config, null, camp, useFall && config.UsesExplosionDamage);
				}
			}
			HitEffect(null);
			PlaySplat();
		}
		if (!suppressGameplay)
		{
			this.OnLandOver?.Invoke(base.GlobalPosition, base.gridPos);
		}
		if (GodotObject.IsInstanceValid(cell) && cell.isWater)
		{
			CreateSplash();
			Over();
		}
		else
		{
			Over();
		}
	}

	public void Over()
	{
		if (!over)
		{
			over = true;
			hitOver = true;
			base.GlobalPosition = new Vector2(-100f, -100f);
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.PROJECTILE, this);
		}
	}

	public void OverFromExtension()
	{
		if (!over)
		{
			extId = -1;
			Over();
		}
	}

	public void HitTargetFromExtension()
	{
		if (!over)
		{
			if (GodotObject.IsInstanceValid(target))
			{
				HitCharacter(target);
			}
			else
			{
				Over();
			}
		}
	}

	public void LandFromExtension()
	{
		if (!over)
		{
			extId = -1;
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
			Land();
		}
	}

	public void UpdateGridPos()
	{
		base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
	}

	public bool CheckShooterHeight()
	{
		if (TryGetCachedCellGroundHeight(out var cellGroundHeight))
		{
			double num = cellGroundHeight;
			if (projectileHeight < TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL)
			{
				num -= 50.0;
			}
			else if (projectileHeight == TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL)
			{
				num -= 20.0;
			}
			if (height < num)
			{
				HitEffect(null);
				Over();
				return true;
			}
		}
		return false;
	}

	private bool ShouldRefreshGrid(ulong currentFrame)
	{
		if (!_gridRefreshScheduleInitialized)
		{
			ulong num = (ulong)randFreshIndex % 5uL;
			ulong num2 = (currentFrame + num) % 5;
			_nextGridRefreshFrame = currentFrame + (5 - num2) % 5;
			_gridRefreshScheduleInitialized = true;
		}
		if (currentFrame < _nextGridRefreshFrame)
		{
			return false;
		}
		do
		{
			_nextGridRefreshFrame += 5uL;
		}
		while (_nextGridRefreshFrame <= currentFrame);
		return true;
	}

	private bool TryGetCachedCellGroundHeight(out double cellGroundHeight)
	{
		CurveTexture curveTexture = cell?.groundHeightCurve;
		if (_cellGroundHeightCacheInitialized && _cellGroundHeightCacheCell == cell && _cellGroundHeightCacheCurve == curveTexture)
		{
			cellGroundHeight = _cellGroundHeightCacheValue;
			return _cellGroundHeightCacheValid;
		}
		_cellGroundHeightCacheInitialized = true;
		_cellGroundHeightCacheCell = cell;
		_cellGroundHeightCacheCurve = curveTexture;
		_cellGroundHeightCacheValid = GodotObject.IsInstanceValid(cell);
		_cellGroundHeightCacheValue = (_cellGroundHeightCacheValid ? cell.GetGroundHeight() : 0.0);
		cellGroundHeight = _cellGroundHeightCacheValue;
		return _cellGroundHeightCacheValid;
	}

	public void EnableMonitoring()
	{
		SetMonitoring(value: true);
		hitBox.ProcessMode = ProcessModeEnum.Inherit;
	}

	public void DisableMonitoring()
	{
		SetMonitoring(value: false);
	}

	public void TargetDiedFromExtension()
	{
		target = null;
		trackCacheValid = false;
		FindTrackTargetCached();
	}

	public void FindTrackTargetCached()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if ((int)physicsFrames != trackSearchBudgetFrame)
		{
			trackSearchBudgetFrame = (int)physicsFrames;
			trackSearchBudgetUsed = 0;
		}
		if (!trackForceSearch && trackSearchBudgetUsed >= trackSearchBudget)
		{
			return;
		}
		if (trackCacheValid && GodotObject.IsInstanceValid(trackCacheTarget))
		{
			bool flag = true;
			if (!trackCacheTarget.targetRegistrationComponent.canProjectileCheck)
			{
				flag = false;
			}
			if (trackCacheTarget.nearDie || trackCacheTarget.die || !CanTarget(trackCacheTarget) || !CanCollision(trackCacheTarget.instance.maskFlags))
			{
				flag = false;
			}
			if (!trackCacheTarget.IsHitBoxEnabled)
			{
				flag = false;
			}
			if (flag)
			{
				target = trackCacheTarget;
				trackConsecutiveNoTarget = 0;
				return;
			}
		}
		FindTrackTargetInternal();
		trackSearchBudgetUsed++;
		trackForceSearch = false;
		trackCacheFrame = (int)physicsFrames;
		trackCacheTarget = target;
		trackCacheValid = GodotObject.IsInstanceValid(target);
		if (GodotObject.IsInstanceValid(target))
		{
			trackConsecutiveNoTarget = 0;
		}
		else
		{
			trackConsecutiveNoTarget++;
		}
	}

	public void FindTrackTarget()
	{
		FindTrackTargetInternal();
	}

	public void FindTrackTargetInternal()
	{
		if (GodotObject.IsInstanceValid(target))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(magneticTarget) && !magneticTarget.nearDie && !magneticTarget.die && CanTarget(magneticTarget) && CanCollision(magneticTarget.instance.maskFlags) && magneticTarget.targetRegistrationComponent.canProjectileCheck && magneticTarget.IsHitBoxEnabled)
		{
			target = magneticTarget;
			return;
		}
		TowerDefenseCharacter projectileTargetNearest = TowerDefenseManager.Instance.GetProjectileTargetNearest(this, collisionFlags);
		if (GodotObject.IsInstanceValid(projectileTargetNearest))
		{
			target = projectileTargetNearest;
			if (target is TowerDefensePlant && GodotObject.IsInstanceValid(cell))
			{
				target = cell.GetTarget(collisionFlags, camp);
			}
		}
	}

	public void BlockedBounce()
	{
		if (!over && !hitOver)
		{
			blocked = true;
			target = null;
			hitOver = true;
			SetMonitoring(value: false);
			SetMonitorable(value: false);
			ySpeed = -500.0;
			velocity *= 0.5f;
		}
	}

	public void UpdateCatapultTarget()
	{
		if (GodotObject.IsInstanceValid(target))
		{
			Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
			Vector2 logicalGlobalPosition2 = target.GetLogicalGlobalPosition(target.transformPoint);
			catapultTargetPos = logicalGlobalPosition2 + new Vector2((float)(-10 * fireDirX) * target.Scale.X * target.spriteGroup.Scale.X, -10f);
			catapultTargetPos = new Vector2(catapultTargetPos.X, Mathf.Max(catapultTargetPos.Y, logicalGlobalPosition.Y) - 30f);
		}
	}

	public TowerDefenseEffectSpriteOnce CreateSplash()
	{
		if (TowerDefenseManager.Instance.GetEffectCount() > 100)
		{
			return null;
		}
		TowerDefenseEffectSpriteOnce obj = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, TowerDefenseGroundItemBase.characterNode) as TowerDefenseEffectSpriteOnce;
		obj.gridPos = base.gridPos;
		obj.GlobalPosition = shadowSprite.GlobalPosition - new Vector2(0f, 20f);
		return obj;
	}

	public void SetTrack(bool isChange = false)
	{
		base.gridPos = new Vector2I(base.gridPos.X, 10);
		trackOpen = true;
		checkAll = true;
		shadowSprite.Visible = false;
		penetrateNum = 0;
		if (!isChange)
		{
			base.GlobalPosition = new Vector2(base.GlobalPosition.X, projectileBodyNode.GlobalPosition.Y);
			projectileBodyNode.Position = new Vector2(projectileBodyNode.Position.X, 0f);
		}
		trackCacheFrame = -1;
		trackCacheTarget = null;
		trackCacheValid = false;
		trackConsecutiveNoTarget = 0;
		trackForceSearch = true;
		if (config != null)
		{
			trackSearchInterval = config.trackSearchInterval;
			trackNoTargetInterval = config.trackSearchInterval * 4;
		}
		hitBox.Position = new Vector2(hitBox.Position.X, -20f);
		hitBox.SetDeferred("position", new Vector2(hitBox.Position.X, 0f));
	}
}
