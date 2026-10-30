using Godot;

public sealed class ChangeProjectileStateComponent : CharacterComponentRuntime, IProjectileZoneBinding, IProjectileZone
{
	public enum ProjectileCampFilter
	{
		LegacyFollowCamp,
		EnemyOnly,
		AllyOnly,
		Any
	}

	public AabbShape2DResource checkShape;

	public TowerDefenseCharacter target;

	public ProjectileCampFilter campFilter;

	public bool followCamp = true;

	public bool replaceDamageFlags = true;

	public bool replaceFireMethodFlags = true;

	public bool enableTrackingFromFireMethod = true;

	public int damageFlags = 3;

	public int fireMethodFlags = 1;

	public TowerDefenseCharacter parent;

	private Rect2 _zoneRect;

	private BulletField _registeredBulletField;

	private bool _configured;

	private int _rowSpan;

	private ChangeProjectileStateComponentDefinition Definition => base.ComponentDefinition as ChangeProjectileStateComponentDefinition;

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

	public int RowSpan => _rowSpan;

	protected override void OnBound()
	{
		parent = base.Owner;
		target = base.Owner;
		ConfigureOnce();
	}

	protected override void OnActivated()
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(BulletField.Instance))
		{
			BindBulletField(BulletField.Instance);
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		BindBulletField(null);
		parent = null;
		target = null;
		_zoneRect = default(Rect2);
	}

	protected override void OnReleased()
	{
		checkShape = null;
		parent = null;
		target = null;
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
			if (_registeredBulletField != null)
			{
				RefreshRowSpan();
			}
			_registeredBulletField?.RegisterZone(this);
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			ChangeProjectileStateComponentDefinition definition = Definition;
			if (definition != null)
			{
				checkShape = (GodotObject.IsInstanceValid(definition.checkShape) ? (definition.checkShape.Duplicate(deep: true) as AabbShape2DResource) : null);
				campFilter = definition.campFilter;
				followCamp = definition.followCamp;
				replaceDamageFlags = definition.replaceDamageFlags;
				replaceFireMethodFlags = definition.replaceFireMethodFlags;
				enableTrackingFromFireMethod = definition.enableTrackingFromFireMethod;
				damageFlags = definition.damageFlags;
				fireMethodFlags = definition.fireMethodFlags;
				_configured = true;
			}
		}
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
		ChangeProjectileNode(projectile);
	}

	private void ChangeProjectileNode(TowerDefenseProjectile projectile)
	{
		if (CanProcessProjectile() && GodotObject.IsInstanceValid(projectile) && CanAffectCamp(projectile.camp))
		{
			if (replaceDamageFlags)
			{
				projectile.damageFlags = damageFlags;
			}
			if (replaceFireMethodFlags)
			{
				projectile.fireMethodFlags = fireMethodFlags;
			}
			int flags = (replaceFireMethodFlags ? fireMethodFlags : projectile.fireMethodFlags);
			if (enableTrackingFromFireMethod && !projectile.trackOpen && HasTrackFlag(flags))
			{
				TowerDefenseCharacter magneticTarget = (GodotObject.IsInstanceValid(target) ? target : parent);
				projectile.SetTrack(isChange: true);
				projectile.target = magneticTarget;
				projectile.magneticTarget = magneticTarget;
			}
		}
	}

	public void UpdateRect()
	{
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(checkShape) || !checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery), out _zoneRect))
		{
			_zoneRect = default(Rect2);
		}
		RefreshRowSpan();
	}

	private void RefreshRowSpan()
	{
		_rowSpan = 0;
		if (GodotObject.IsInstanceValid(checkShape) && checkShape.Geometry is RectangleShape2D rectangleShape2D && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			if (mapGridSize.IsFinite() && !(mapGridSize.Y <= 0f))
			{
				float s = Mathf.Abs(rectangleShape2D.Size.Y) / (mapGridSize.Y * 2f);
				_rowSpan = Mathf.Max(0, Mathf.FloorToInt(s));
			}
		}
	}

	public void OnBulletIntersect(ref BulletData bullet, int index)
	{
		if (CanProcessProjectile() && bullet.active && bullet.config != null && CanAffectCamp(bullet.camp))
		{
			if (replaceDamageFlags)
			{
				bullet.damageFlags = damageFlags;
			}
			if (replaceFireMethodFlags)
			{
				bullet.fireMethodFlags = fireMethodFlags;
			}
			int flags = (replaceFireMethodFlags ? fireMethodFlags : bullet.fireMethodFlags);
			if (enableTrackingFromFireMethod && !bullet.trackOpen && HasTrackFlag(flags) && GodotObject.IsInstanceValid(_registeredBulletField))
			{
				TowerDefenseCharacter trackTarget = (GodotObject.IsInstanceValid(target) ? target : parent);
				_registeredBulletField.SetTrackData(index, trackTarget);
			}
		}
	}

	private bool CanProcessProjectile()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && !parent.die && !parent.nearDie && GodotObject.IsInstanceValid(checkShape))
		{
			return checkShape.Enabled;
		}
		return false;
	}

	private bool CanAffectCamp(TowerDefenseEnum.CHARACTER_CAMP projectileCamp)
	{
		bool flag = projectileCamp == parent.camp;
		return campFilter switch
		{
			ProjectileCampFilter.EnemyOnly => !flag, 
			ProjectileCampFilter.AllyOnly => flag, 
			ProjectileCampFilter.Any => true, 
			_ => !followCamp || !flag, 
		};
	}

	private static bool HasTrackFlag(int flags)
	{
		return (flags & 0x20) != 0;
	}
}
