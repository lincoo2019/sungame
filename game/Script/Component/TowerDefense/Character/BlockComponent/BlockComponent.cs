using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class BlockComponent : CharacterComponentRuntime, IProjectileZoneBinding, IProjectileZone, ICatapultProjectileBlockZone
{
	public delegate void BlockEventHandler();

	public delegate void ProjectileReboundEventHandler();

	public Array<string> blockType = new Array<string> { "General" };

	public Vector2 extendGrid = Vector2.One;

	public AabbShape2DResource checkShape;

	public bool checkLadder;

	public bool reboundProjectile;

	public AabbShape2DResource reboundProjectileShape;

	public TowerDefenseCharacter parent;

	public int characterCheckEveryPhysicsFrames = 1;

	private int _characterCheckFrame;

	private readonly HashSet<string> _blockTypeSet = new HashSet<string>();

	private Rect2 _zoneRect;

	private Rect2 _blockRect;

	private Rect2 _reboundRect;

	private BulletField _registeredBulletField;

	private bool _configured;

	private BlockComponentDefinition Definition => base.ComponentDefinition as BlockComponentDefinition;

	private static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	internal override bool WantsPhysicsProcess => true;

	public Rect2 WorldRect => _zoneRect;

	public int GridY
	{
		get
		{
			if (!GodotObject.IsInstanceValid(parent))
			{
				return 0;
			}
			return parent.gridPos.Y;
		}
	}

	public int RowSpan => (int)extendGrid.Y;

	public event BlockEventHandler OnBlock;

	public event ProjectileReboundEventHandler OnProjectileRebound;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnActivated()
	{
		BindBulletField(BulletField.Instance);
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		BindBulletField(null);
		parent = null;
		_characterCheckFrame = 0;
		_zoneRect = default(Rect2);
		_blockRect = default(Rect2);
		_reboundRect = default(Rect2);
	}

	protected override void OnReleased()
	{
		this.OnBlock = null;
		this.OnProjectileRebound = null;
		_blockTypeSet.Clear();
		blockType.Clear();
		checkShape = null;
		reboundProjectileShape = null;
		parent = null;
	}

	public void BindBulletField(BulletField bulletField)
	{
		if (_registeredBulletField != bulletField)
		{
			if (GodotObject.IsInstanceValid(_registeredBulletField))
			{
				_registeredBulletField.UnregisterZone(this);
			}
			_registeredBulletField = (GodotObject.IsInstanceValid(bulletField) ? bulletField : null);
			_registeredBulletField?.RegisterZone(this);
		}
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		BlockComponentDefinition definition = Definition;
		if (definition == null)
		{
			return;
		}
		blockType.Clear();
		_blockTypeSet.Clear();
		if (definition.blockType != null)
		{
			for (int i = 0; i < definition.blockType.Count; i++)
			{
				string text = definition.blockType[i];
				if (!string.IsNullOrEmpty(text))
				{
					blockType.Add(text);
					_blockTypeSet.Add(text);
				}
			}
		}
		extendGrid = definition.extendGrid;
		checkShape = (GodotObject.IsInstanceValid(definition.checkShape) ? (definition.checkShape.Duplicate(deep: true) as AabbShape2DResource) : null);
		checkLadder = definition.checkLadder;
		reboundProjectile = definition.reboundProjectile;
		reboundProjectileShape = (GodotObject.IsInstanceValid(definition.reboundProjectileShape) ? (definition.reboundProjectileShape.Duplicate(deep: true) as AabbShape2DResource) : null);
		characterCheckEveryPhysicsFrames = Mathf.Max(1, definition.characterCheckEveryPhysicsFrames);
		_configured = true;
	}

	public bool SetCheckRectangleSize(Vector2 size)
	{
		if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X < 0f || size.Y < 0f || !GodotObject.IsInstanceValid(checkShape) || !(checkShape.Geometry is RectangleShape2D rectangleShape2D))
		{
			return false;
		}
		rectangleShape2D.Size = size;
		return true;
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (!HasGameplayAuthority || !base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !TowerDefenseManager._IsGameRunning() || !GodotObject.IsInstanceValid(parent) || !parent.inGame || !parent.componentAlive)
		{
			return;
		}
		if (checkLadder && GodotObject.IsInstanceValid(parent.cell) && GodotObject.IsInstanceValid(parent.cell.characterLadder))
		{
			parent.cell.characterLadder.Block(parent);
			this.OnBlock?.Invoke();
		}
		if (++_characterCheckFrame < characterCheckEveryPhysicsFrames)
		{
			return;
		}
		_characterCheckFrame = 0;
		if (GodotObject.IsInstanceValid(checkShape) && checkShape.Enabled && checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrame), out var rect) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, parent.camp);
			for (int i = 0; i < charactersIntersectingRectListExcludingCamp.Count; i++)
			{
				BlockCheckCharacter(charactersIntersectingRectListExcludingCamp[i]);
			}
		}
	}

	public void BlockCheckCharacter(TowerDefenseCharacter character)
	{
		if (HasGameplayAuthority && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(character) && !parent.die && !parent.nearDie && parent.camp != character.camp && character.CanBlock() && !((float)character.gridPos.Y < (float)parent.gridPos.Y - extendGrid.Y) && !((float)character.gridPos.Y > (float)parent.gridPos.Y + extendGrid.Y) && !((float)character.gridPos.X < (float)parent.gridPos.X - extendGrid.X) && !((float)character.gridPos.X > (float)parent.gridPos.X + extendGrid.X) && _blockTypeSet.Contains(character.BlockType()))
		{
			character.Block(parent);
			this.OnBlock?.Invoke();
		}
	}

	public void UpdateRect()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			_zoneRect = default(Rect2);
			_blockRect = default(Rect2);
			_reboundRect = default(Rect2);
			return;
		}
		if (!GodotObject.IsInstanceValid(checkShape) || !checkShape.TryGetWorldRect(parent.GetLogicalGlobalTransform(parent), out _blockRect))
		{
			_blockRect = default(Rect2);
		}
		if (!reboundProjectile || !GodotObject.IsInstanceValid(reboundProjectileShape) || !reboundProjectileShape.TryGetWorldRect(parent.GetLogicalGlobalTransform(parent), out _reboundRect))
		{
			_reboundRect = default(Rect2);
		}
		if (_blockRect.Size == Vector2.Zero)
		{
			_zoneRect = _reboundRect;
		}
		else if (_reboundRect.Size == Vector2.Zero)
		{
			_zoneRect = _blockRect;
		}
		else
		{
			_zoneRect = _blockRect.Merge(_reboundRect);
		}
	}

	public void OnBulletIntersect(ref BulletData bullet, int index)
	{
		if (bullet.catapultOpen)
		{
			Vector2 vector = new Vector2(bullet.pos.X, (float)((double)bullet.pos.Y - bullet.z));
			TryBlockCatapultBullet(ref bullet, index, vector, vector, BulletField.GetCollisionHalfSize(ref bullet));
			return;
		}
		bool allowVisualOnly = bullet.suppressGameplay && bullet.catapultOpen;
		if (CanProcessProjectile(allowVisualOnly) && bullet.active && bullet.config != null && !bullet.hitOver && !bullet.blocked && reboundProjectile && parent.camp == bullet.camp && CanReboundProjectile(bullet.config, bullet.fireMethodFlags) && AabbShapeUtil.Intersects(BulletField.GetCollisionRect(ref bullet), _reboundRect))
		{
			float num = Mathf.Sign(parent.GetLogicalGlobalPosition().X - bullet.pos.X);
			float num2 = Mathf.Sign(bullet.vel.X);
			if (num == num2)
			{
				bullet.vel = new Vector2(0f - bullet.vel.X, bullet.vel.Y);
				bullet.fireDirX = 0f - bullet.fireDirX;
				StopStarTrackingAfterRebound(ref bullet);
				bullet.target = null;
				bullet.fireCharacter = null;
				this.OnProjectileRebound?.Invoke();
			}
		}
	}

	public bool TryBlockCatapultBullet(ref BulletData bullet, int index, Vector2 collisionStartPosition, Vector2 collisionEndPosition, Vector2 collisionHalfSize)
	{
		bool allowVisualOnly = bullet.suppressGameplay && bullet.catapultOpen;
		if (!CanProcessProjectile(allowVisualOnly) || !bullet.active || bullet.config == null || bullet.hitOver || bullet.blocked || !bullet.catapultOpen || parent.camp == bullet.camp || !IsInsideExtendedGrid(bullet) || !GodotObject.IsInstanceValid(checkShape) || !checkShape.Enabled || bullet.catapultTimer <= bullet.catapultTime * 0.75 || !BulletField.SweptCollisionIntersectsRect(collisionStartPosition, collisionEndPosition, collisionHalfSize, _blockRect))
		{
			return false;
		}
		if (HasGameplayAuthority && bullet.config.blockHurt != -1.0)
		{
			parent.Hurt(bullet.config.blockHurt);
		}
		if (GodotObject.IsInstanceValid(_registeredBulletField))
		{
			_registeredBulletField.BlockedBounceData(index);
		}
		bool blocked = bullet.blocked;
		if (blocked && HasGameplayAuthority)
		{
			BlockEventHandler blockEventHandler = this.OnBlock;
			if (blockEventHandler == null)
			{
				return blocked;
			}
			blockEventHandler();
		}
		return blocked;
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
		if (CanProcessProjectile() && GodotObject.IsInstanceValid(projectile) && projectile.config != null)
		{
			if (projectile.catapultOpen)
			{
				TryBlockCatapultProjectile(projectile);
			}
			else
			{
				TryReboundProjectile(projectile);
			}
		}
	}

	private bool CanProcessProjectile(bool allowVisualOnly = false)
	{
		if ((HasGameplayAuthority || allowVisualOnly) && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && !parent.die)
		{
			return !parent.nearDie;
		}
		return false;
	}

	private bool IsInsideExtendedGrid(BulletData bullet)
	{
		Vector2I vector2I = ResolveCatapultGridPosition(bullet);
		if ((float)vector2I.Y >= (float)parent.gridPos.Y - extendGrid.Y && (float)vector2I.Y <= (float)parent.gridPos.Y + extendGrid.Y && (float)vector2I.X >= (float)parent.gridPos.X - extendGrid.X)
		{
			return (float)vector2I.X <= (float)parent.gridPos.X + extendGrid.X;
		}
		return false;
	}

	private void TryBlockCatapultProjectile(TowerDefenseProjectile projectile)
	{
		if (projectile.over || projectile.hitOver || projectile.blocked || parent.camp == projectile.camp || !GodotObject.IsInstanceValid(checkShape) || !checkShape.Enabled)
		{
			return;
		}
		Vector2I vector2I = ResolveCatapultGridPosition(projectile);
		if (!((float)vector2I.Y < (float)parent.gridPos.Y - extendGrid.Y) && !((float)vector2I.Y > (float)parent.gridPos.Y + extendGrid.Y) && !((float)vector2I.X < (float)parent.gridPos.X - extendGrid.X) && !((float)vector2I.X > (float)parent.gridPos.X + extendGrid.X) && GodotObject.IsInstanceValid(projectile.hitBox) && AabbShapeUtil.Intersects(AabbShapeUtil.ComputeAreaWorldRect(projectile.hitBox), _blockRect) && !(projectile.catapultTimer <= projectile.catapultTime * 0.75))
		{
			if (projectile.config.blockHurt != -1.0)
			{
				parent.Hurt(projectile.config.blockHurt);
			}
			projectile.BlockedBounce();
			this.OnBlock?.Invoke();
		}
	}

	private static Vector2I ResolveCatapultGridPosition(BulletData bullet)
	{
		if (GodotObject.IsInstanceValid(bullet.target))
		{
			return bullet.target.gridPos;
		}
		if (bullet.catapultOpen && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.GetMapGridPos(bullet.catapultTargetPos);
		}
		return bullet.gridPos;
	}

	private static Vector2I ResolveCatapultGridPosition(TowerDefenseProjectile projectile)
	{
		if (GodotObject.IsInstanceValid(projectile.target))
		{
			return projectile.target.gridPos;
		}
		if (projectile.catapultOpen && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.GetMapGridPos(projectile.catapultTargetPos);
		}
		return projectile.gridPos;
	}

	private void TryReboundProjectile(TowerDefenseProjectile projectile)
	{
		if (!reboundProjectile || !GodotObject.IsInstanceValid(reboundProjectileShape) || !reboundProjectileShape.Enabled || parent.camp != projectile.camp || !CanReboundProjectile(projectile.config, projectile.fireMethodFlags) || !GodotObject.IsInstanceValid(projectile.hitBox) || !AabbShapeUtil.Intersects(AabbShapeUtil.ComputeAreaWorldRect(projectile.hitBox), _reboundRect))
		{
			return;
		}
		float num = Mathf.Sign(parent.GetLogicalGlobalPosition().X - projectile.GlobalPosition.X);
		float num2 = Mathf.Sign(projectile.velocity.X);
		if (num == num2)
		{
			projectile.velocity = new Vector2(0f - projectile.velocity.X, projectile.velocity.Y);
			StopStarTrackingAfterRebound(projectile);
			if (GodotObject.IsInstanceValid(projectile.projectileBodyNode))
			{
				projectile.projectileBodyNode.Scale = new Vector2(0f - projectile.projectileBodyNode.Scale.X, projectile.projectileBodyNode.Scale.Y);
			}
			projectile.fireDirX = -projectile.fireDirX;
			projectile.target = null;
			projectile.fireCharacter = null;
			this.OnProjectileRebound?.Invoke();
		}
	}

	private static bool CanReboundProjectile(TowerDefenseProjectileConfig config, int fireMethodFlags)
	{
		if ((fireMethodFlags & 1) == 0)
		{
			return config?.isStar ?? false;
		}
		return true;
	}

	private static void StopStarTrackingAfterRebound(ref BulletData bullet)
	{
		TowerDefenseProjectileConfig config = bullet.config;
		if (config != null && config.isStar && bullet.trackOpen)
		{
			bullet.trackOpen = false;
			bullet.fireMethodFlags = (bullet.fireMethodFlags & -33) | 1;
		}
	}

	private static void StopStarTrackingAfterRebound(TowerDefenseProjectile projectile)
	{
		TowerDefenseProjectileConfig config = projectile.config;
		if (config != null && config.isStar && projectile.trackOpen)
		{
			projectile.trackOpen = false;
			projectile.fireMethodFlags = (projectile.fireMethodFlags & -33) | 1;
			projectile.isShooter = !projectile.useGravity && !projectile.useFall;
		}
	}
}
