using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class ChangeProjectileComponent : CharacterComponentRuntime, IProjectileZoneBinding, IProjectileZone
{
	private sealed class CachedChangePlan
	{
		public TowerDefenseProjectileConfig Config;

		public BulletField.PreparedProjectileChange Prepared;

		public bool IsSupported;

		public bool UseRatio;

		public double DamageRatio;

		public double FallbackDamage;

		public bool HasData;
	}

	private static readonly StringName EmptyStringName = new StringName("");

	private static readonly StringName StarStringName = new StringName("Star");

	private const string TrackConfigSuffix = "Track";

	private const double StraightStarDamageThreshold = 20.0;

	private static TowerDefenseProjectileConfig _starConfigCache;

	private static readonly System.Collections.Generic.Dictionary<StringName, StringName> _projectileChangeLookupNameCache = new System.Collections.Generic.Dictionary<StringName, StringName>();

	public StringName changeName;

	public string changeAudio;

	public AabbShape2DResource checkShape;

	public bool isStar;

	public bool trackToStraight;

	public Array<float> starDirections = new Array<float> { 330f, 270f, 180f, 90f, 30f };

	public float starProjectileSpeed = 500f;

	public int exclusionPhysicsFrames = 2;

	public int structExcludeCleanupIntervalFrames = 60;

	public TowerDefenseCharacter parent;

	public Array<TowerDefenseProjectile> excludeList = new Array<TowerDefenseProjectile>();

	public double timer;

	private Rect2 _zoneRect;

	private readonly HashSet<TowerDefenseProjectile> _changedProjectileExclude = new HashSet<TowerDefenseProjectile>();

	private readonly List<TowerDefenseProjectile> _nodeExcludeCleanupBuffer = new List<TowerDefenseProjectile>();

	private BulletField _registeredBulletField;

	private ulong _starExcludeReleaseFrame;

	private ulong _lastChangeAudioPhysicsFrame = 18446744073709551615uL;

	private StringName _cachedFromName;

	private StringName _cachedToName;

	private StringName _cachedSkinName;

	private TowerDefenseProjectileConfig _cachedSourceConfig;

	private int _cachedFireMethodFlags;

	private CachedChangePlan _cachedChangePlan;

	private readonly System.Collections.Generic.Dictionary<(StringName FromName, StringName ToName, StringName SkinName, int FireMethodFlags), CachedChangePlan> _changePlanCache = new System.Collections.Generic.Dictionary<(StringName, StringName, StringName, int), CachedChangePlan>();

	private readonly System.Collections.Generic.Dictionary<(StringName SkinName, int FireMethodFlags), TowerDefenseProjectileConfig> _straightStarConfigCache = new System.Collections.Generic.Dictionary<(StringName, int), TowerDefenseProjectileConfig>();

	private bool _configured;

	private ChangeProjectileComponentDefinition Definition => base.ComponentDefinition as ChangeProjectileComponentDefinition;

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (_changedProjectileExclude.Count <= 0)
			{
				return excludeList.Count > 0;
			}
			return true;
		}
	}

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

	public int RowSpan => 0;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnActivated()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			BindBulletField(BulletField.Instance);
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		BindBulletField(null);
		_changedProjectileExclude.Clear();
		_cachedSourceConfig = null;
		_cachedChangePlan = null;
		_changePlanCache.Clear();
		_straightStarConfigCache.Clear();
		excludeList.Clear();
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

	protected override void OnReleased()
	{
		checkShape = null;
		starDirections.Clear();
		parent = null;
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		ChangeProjectileComponentDefinition definition = Definition;
		if (definition == null)
		{
			return;
		}
		changeName = definition.changeName;
		changeAudio = definition.changeAudio ?? string.Empty;
		checkShape = definition.checkShape;
		isStar = definition.isStar;
		trackToStraight = definition.trackToStraight;
		starDirections.Clear();
		if (definition.starDirections != null)
		{
			for (int i = 0; i < definition.starDirections.Count; i++)
			{
				starDirections.Add(definition.starDirections[i]);
			}
		}
		starProjectileSpeed = definition.starProjectileSpeed;
		exclusionPhysicsFrames = definition.exclusionPhysicsFrames;
		structExcludeCleanupIntervalFrames = definition.structExcludeCleanupIntervalFrames;
		_configured = true;
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		CleanupNodeProjectileExclusions(physicsFrame);
		if (excludeList.Count > 0 && physicsFrame >= _starExcludeReleaseFrame)
		{
			excludeList.Clear();
		}
		RefreshPhysicsProcessEligibility();
	}

	public void UpdateRect()
	{
		int num = (GodotObject.IsInstanceValid(_registeredBulletField) ? _registeredBulletField.CurrentZonePhysicsFrame : (-1));
		ulong physicsFrame = ((num >= 0) ? ((ulong)num) : TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery());
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(checkShape) || !checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrame), out _zoneRect))
		{
			_zoneRect = default(Rect2);
		}
	}

	public void OnBulletIntersect(ref BulletData b, int index)
	{
		BulletField instance = BulletField.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !base.Alive || !GodotObject.IsInstanceValid(parent) || parent.die || parent.nearDie || !GodotObject.IsInstanceValid(checkShape) || !checkShape.Enabled || !b.active || b.config == null)
		{
			return;
		}
		int currentZonePhysicsFrame = instance.CurrentZonePhysicsFrame;
		if (b.zoneExclusionOwner == this)
		{
			if (currentZonePhysicsFrame <= b.zoneExclusionExpiryFrame)
			{
				b.zoneExclusionExpiryFrame = currentZonePhysicsFrame + 1;
				return;
			}
			b.zoneExclusionOwner = null;
			b.zoneExclusionExpiryFrame = 0;
		}
		if (b.camp != parent.camp || (!b.checkAll && b.gridPos.Y != parent.gridPos.Y) || (b.fireMethodFlags & 2) != 0)
		{
			return;
		}
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (_cachedSourceConfig == b.config && _cachedFireMethodFlags == b.fireMethodFlags && _cachedChangePlan != null && _cachedChangePlan.Prepared.AtlasCacheVersion == cacheVersion)
		{
			ApplyCachedChangePlan(instance, index, ref b, currentZonePhysicsFrame, _cachedChangePlan, b.fireMethodFlags);
			return;
		}
		StringName projectileChangeLookupName = GetProjectileChangeLookupName(b.config.NameSN);
		StringName projectileChange = TowerDefenseProjectileRegistry.GetProjectileChange(projectileChangeLookupName, changeName);
		if (projectileChange != null && projectileChange != EmptyStringName)
		{
			int fireMethodFlags = b.fireMethodFlags;
			int sourceFireMethodFlags = ResolveChangedFireMethodFlags(fireMethodFlags);
			CachedChangePlan value;
			if (_cachedFromName == projectileChangeLookupName && _cachedToName == projectileChange && _cachedSkinName == b.config.skinName && _cachedFireMethodFlags == b.fireMethodFlags && _cachedChangePlan != null && _cachedChangePlan.Prepared.AtlasCacheVersion == cacheVersion)
			{
				value = _cachedChangePlan;
			}
			else
			{
				(StringName, StringName, StringName, int) key = (projectileChangeLookupName, projectileChange, b.config.skinName, b.fireMethodFlags);
				if (!_changePlanCache.TryGetValue(key, out value) || value.Prepared.AtlasCacheVersion != cacheVersion)
				{
					TowerDefenseProjectileData projectile = TowerDefenseProjectileRegistry.GetProjectile(projectileChangeLookupName);
					TowerDefenseProjectileData projectile2 = TowerDefenseProjectileRegistry.GetProjectile(projectileChange);
					bool usedTrackTemplate;
					TowerDefenseProjectileConfig towerDefenseProjectileConfig = BuildChangedProjectileConfig(projectileChange, b.config.skinName, sourceFireMethodFlags, out usedTrackTemplate);
					bool flag = projectile != null && projectile2 != null && towerDefenseProjectileConfig != null && towerDefenseProjectileConfig.baseDamage != 0.0 && projectile.baseDamage != 0.0;
					if (towerDefenseProjectileConfig != null && projectile2 != null && projectile2.isFire)
					{
						towerDefenseProjectileConfig.damageFlags |= 4;
					}
					BulletField.PreparedProjectileChange prepared;
					bool isSupported = instance.TryPrepareProjectileChange(towerDefenseProjectileConfig, out prepared);
					value = new CachedChangePlan
					{
						Config = towerDefenseProjectileConfig,
						Prepared = prepared,
						IsSupported = isSupported,
						UseRatio = flag,
						DamageRatio = (flag ? (projectile2.baseDamage / projectile.baseDamage) : 0.0),
						FallbackDamage = (projectile2?.baseDamage ?? 0.0),
						HasData = (projectile2 != null)
					};
					_changePlanCache[key] = value;
				}
				_cachedFromName = projectileChangeLookupName;
				_cachedToName = projectileChange;
				_cachedSkinName = b.config.skinName;
				_cachedSourceConfig = b.config;
				_cachedFireMethodFlags = b.fireMethodFlags;
				_cachedChangePlan = value;
			}
			_cachedSourceConfig = b.config;
			if (value.IsSupported)
			{
				double damage = ((!value.HasData) ? value.Config.baseDamage : (value.UseRatio ? (b.damage * value.DamageRatio) : value.FallbackDamage));
				int num = instance.ChangeBulletDataPrepared(index, value.Config, parent, in value.Prepared, damage);
				if (num >= 0)
				{
					ref BulletData bulletDataRef = ref instance.GetBulletDataRef(num);
					bulletDataRef.zoneExclusionOwner = this;
					bulletDataRef.zoneExclusionExpiryFrame = currentZonePhysicsFrame + 1;
					ApplyStraightTrackState(ref bulletDataRef, fireMethodFlags);
					PlayChangeAudioOncePerFrame((ulong)currentZonePhysicsFrame);
				}
			}
		}
		else if (ShouldConvertLowDamageIntoStraightStar(projectileChangeLookupName, b.config, b.damage))
		{
			int straightFireMethodFlags = GetStraightFireMethodFlags(b.fireMethodFlags);
			TowerDefenseProjectileConfig straightStarConfig = GetStraightStarConfig(b.config.skinName, straightFireMethodFlags);
			if (straightStarConfig != null)
			{
				int num2 = instance.ChangeBulletData(index, straightStarConfig, parent);
				if (num2 >= 0)
				{
					ref BulletData bulletDataRef2 = ref instance.GetBulletDataRef(num2);
					bulletDataRef2.zoneExclusionOwner = this;
					bulletDataRef2.zoneExclusionExpiryFrame = currentZonePhysicsFrame + 1;
					bulletDataRef2.trackOpen = false;
					bulletDataRef2.target = null;
					bulletDataRef2.magneticTarget = null;
					PlayChangeAudioOncePerFrame((ulong)currentZonePhysicsFrame);
				}
			}
		}
		else
		{
			if (!ShouldSplitProjectileIntoStars(projectileChangeLookupName, b.config) || (b.fireMethodFlags & 2) != 0 || starDirections == null || starDirections.Count == 0)
			{
				return;
			}
			Vector2 pos = b.pos;
			int gridY = b.gridY;
			Vector2I gridPos = b.gridPos;
			Rect2 rect = b.rect;
			double height = b.height;
			double groundHeight = b.groundHeight;
			int collisionFlags = b.collisionFlags;
			TowerDefenseEnum.CHARACTER_CAMP camp = b.camp;
			TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = _starConfigCache;
			if (towerDefenseProjectileConfig2 == null)
			{
				towerDefenseProjectileConfig2 = (_starConfigCache = new TowerDefenseProjectileCreateData(StarStringName).BuildConfig());
			}
			if (towerDefenseProjectileConfig2 == null)
			{
				return;
			}
			foreach (float starDirection in starDirections)
			{
				Vector2 vel = Vector2.FromAngle(Mathf.DegToRad(starDirection)) * starProjectileSpeed;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					checkAllOverride = true
				};
				int num3 = instance.TrySpawnFromConfig(towerDefenseProjectileConfig2, pos, vel, vel.Length(), null, camp, gridPos, gridY, rect, null, height, groundHeight, collisionFlags, -1f, checkHeight: false, checkAll: true, useFall: false, useGravity: false, 1.5f, 0f, 0f, overrides);
				if (num3 >= 0)
				{
					ref BulletData bulletDataRef3 = ref instance.GetBulletDataRef(num3);
					bulletDataRef3.zoneExclusionOwner = this;
					bulletDataRef3.zoneExclusionExpiryFrame = currentZonePhysicsFrame + Math.Max(1, exclusionPhysicsFrames);
				}
			}
			instance.Despawn(index);
			PlayChangeAudioOncePerFrame((ulong)currentZonePhysicsFrame);
		}
	}

	private void ApplyCachedChangePlan(BulletField bulletField, int index, ref BulletData bullet, int currentFrame, CachedChangePlan changePlan, int sourceFireMethodFlags)
	{
		if (changePlan.IsSupported)
		{
			double damage = ((!changePlan.HasData) ? changePlan.Config.baseDamage : (changePlan.UseRatio ? (bullet.damage * changePlan.DamageRatio) : changePlan.FallbackDamage));
			int num = bulletField.ChangeBulletDataPrepared(index, changePlan.Config, parent, in changePlan.Prepared, damage);
			if (num >= 0)
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
				bulletDataRef.zoneExclusionOwner = this;
				bulletDataRef.zoneExclusionExpiryFrame = currentFrame + 1;
				ApplyStraightTrackState(ref bulletDataRef, sourceFireMethodFlags);
				PlayChangeAudioOncePerFrame((ulong)currentFrame);
			}
		}
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
		ChangeProjectileNode(projectile);
	}

	public void ChangeProjectile(AabbArea2D area)
	{
		if (GodotObject.IsInstanceValid(area))
		{
			ChangeProjectileNode(area.GetParent() as TowerDefenseProjectile);
		}
	}

	private void TrackChangedProjectile(TowerDefenseProjectile proj)
	{
		if (GodotObject.IsInstanceValid(proj) && !_changedProjectileExclude.Contains(proj))
		{
			_changedProjectileExclude.Add(proj);
			RefreshPhysicsProcessEligibility();
		}
	}

	private void ChangeProjectileNode(TowerDefenseProjectile proj)
	{
		if (!base.Alive || !GodotObject.IsInstanceValid(parent) || parent.die || parent.nearDie || !GodotObject.IsInstanceValid(checkShape) || !checkShape.Enabled || !GodotObject.IsInstanceValid(proj) || proj.config == null || excludeList.Contains(proj) || _changedProjectileExclude.Contains(proj) || proj.camp != parent.camp || (!proj.checkAll && proj.gridPos.Y != parent.gridPos.Y) || (proj.config.fireMethodFlags & 2) != 0)
		{
			return;
		}
		StringName projectileChangeLookupName = GetProjectileChangeLookupName(proj.config.NameSN);
		StringName projectileChange = TowerDefenseProjectileRegistry.GetProjectileChange(projectileChangeLookupName, changeName);
		if (projectileChange != null && projectileChange != EmptyStringName)
		{
			int fireMethodFlags = proj.fireMethodFlags;
			int sourceFireMethodFlags = ResolveChangedFireMethodFlags(fireMethodFlags);
			TowerDefenseProjectileData projectile = TowerDefenseProjectileRegistry.GetProjectile(projectileChangeLookupName);
			TowerDefenseProjectileData projectile2 = TowerDefenseProjectileRegistry.GetProjectile(projectileChange);
			bool usedTrackTemplate;
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = BuildChangedProjectileConfig(projectileChange, proj.config.skinName, sourceFireMethodFlags, out usedTrackTemplate);
			if (towerDefenseProjectileConfig != null)
			{
				if (projectile != null && projectile2 != null && towerDefenseProjectileConfig.baseDamage != 0.0 && projectile.baseDamage != 0.0)
				{
					towerDefenseProjectileConfig.baseDamage = proj.damage * (projectile2.baseDamage / projectile.baseDamage);
				}
				else if (projectile2 != null)
				{
					towerDefenseProjectileConfig.baseDamage = projectile2.baseDamage;
				}
				if (projectile2 != null && projectile2.isFire)
				{
					towerDefenseProjectileConfig.damageFlags |= 4;
				}
				TrackChangedProjectile(proj);
				proj.Change(towerDefenseProjectileConfig, projectileChangeLookupName, projectileChange, parent);
				ApplyStraightTrackState(proj, fireMethodFlags);
				PlayChangeAudioOncePerFrame(Engine.GetPhysicsFrames());
			}
		}
		else if (ShouldConvertLowDamageIntoStraightStar(projectileChangeLookupName, proj.config, proj.damage))
		{
			int straightFireMethodFlags = GetStraightFireMethodFlags(proj.fireMethodFlags);
			TowerDefenseProjectileConfig straightStarConfig = GetStraightStarConfig(proj.config.skinName, straightFireMethodFlags);
			if (straightStarConfig != null)
			{
				TrackChangedProjectile(proj);
				proj.Change(straightStarConfig, projectileChangeLookupName, StarStringName, parent);
				proj.trackOpen = false;
				proj.target = null;
				proj.magneticTarget = null;
				PlayChangeAudioOncePerFrame(Engine.GetPhysicsFrames());
			}
		}
		else
		{
			if (!ShouldSplitProjectileIntoStars(projectileChangeLookupName, proj.config) || (proj.config.fireMethodFlags & 2) != 0 || starDirections == null || starDirections.Count == 0)
			{
				return;
			}
			TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData(StarStringName);
			foreach (float starDirection in starDirections)
			{
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					checkAllOverride = true,
					initialRotationOverride = Mathf.DegToRad(starDirection)
				};
				FireComponent.CreateProjectilePosition(null, null, proj.height, proj.GlobalPosition, Vector2.FromAngle(Mathf.DegToRad(starDirection)) * starProjectileSpeed, projectileData, proj.collisionFlags, proj.camp, default(Vector2), overrides);
			}
			proj.Over();
			excludeList.Remove(proj);
			if (excludeList.Count > 0)
			{
				_starExcludeReleaseFrame = Engine.GetPhysicsFrames() + (ulong)Math.Max(1, exclusionPhysicsFrames);
				RefreshPhysicsProcessEligibility();
			}
		}
	}

	internal bool ShouldConvertLowDamageIntoStraightStar(StringName projectileName, TowerDefenseProjectileConfig runtimeConfig, double damage)
	{
		if (damage < 20.0)
		{
			return ShouldSplitProjectileIntoStars(projectileName, runtimeConfig);
		}
		return false;
	}

	internal bool ShouldSplitProjectileIntoStars(StringName projectileName, TowerDefenseProjectileConfig runtimeConfig = null)
	{
		if (isStar && !TowerDefenseProjectileRegistry.HasChangeTarget(projectileName, changeName))
		{
			return !TowerDefenseProjectileRegistry.IsStarProjectile(projectileName, runtimeConfig);
		}
		return false;
	}

	private void CleanupNodeProjectileExclusions(ulong physicsFrame)
	{
		if (_changedProjectileExclude.Count == 0)
		{
			return;
		}
		_nodeExcludeCleanupBuffer.Clear();
		foreach (TowerDefenseProjectile item in _changedProjectileExclude)
		{
			if (!ShouldKeepProjectileExcluded(item, physicsFrame))
			{
				_nodeExcludeCleanupBuffer.Add(item);
			}
		}
		for (int i = 0; i < _nodeExcludeCleanupBuffer.Count; i++)
		{
			_changedProjectileExclude.Remove(_nodeExcludeCleanupBuffer[i]);
		}
	}

	private bool ShouldKeepProjectileExcluded(TowerDefenseProjectile projectile, ulong physicsFrame)
	{
		if (base.Alive && GodotObject.IsInstanceValid(projectile) && !projectile.over && !projectile.hitOver && GodotObject.IsInstanceValid(projectile.hitBox) && GodotObject.IsInstanceValid(checkShape) && checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrame), out var rect))
		{
			return AabbShapeUtil.Intersects(AabbShapeUtil.ComputeAreaWorldRect(projectile.hitBox), rect);
		}
		return false;
	}

	private void PlayChangeAudioOncePerFrame(ulong physicsFrame)
	{
		if (!string.IsNullOrEmpty(changeAudio) && _lastChangeAudioPhysicsFrame != physicsFrame)
		{
			_lastChangeAudioPhysicsFrame = physicsFrame;
			AudioManager.Instance.AudioPlay(changeAudio);
		}
	}

	private static int GetStraightFireMethodFlags(int sourceFireMethodFlags)
	{
		return (sourceFireMethodFlags & -33) | 1;
	}

	private int ResolveChangedFireMethodFlags(int sourceFireMethodFlags)
	{
		if (!trackToStraight || !IsTrackFireMethod(sourceFireMethodFlags))
		{
			return sourceFireMethodFlags;
		}
		return GetStraightFireMethodFlags(sourceFireMethodFlags);
	}

	private static bool IsTrackFireMethod(int fireMethodFlags)
	{
		return (fireMethodFlags & 0x20) != 0;
	}

	private void ApplyStraightTrackState(ref BulletData bullet, int sourceFireMethodFlags)
	{
		if (trackToStraight && IsTrackFireMethod(sourceFireMethodFlags))
		{
			bullet.trackOpen = false;
			bullet.target = null;
			bullet.magneticTarget = null;
		}
	}

	private void ApplyStraightTrackState(TowerDefenseProjectile projectile, int sourceFireMethodFlags)
	{
		if (trackToStraight && IsTrackFireMethod(sourceFireMethodFlags))
		{
			projectile.trackOpen = false;
			projectile.target = null;
			projectile.magneticTarget = null;
		}
	}

	private TowerDefenseProjectileConfig GetStraightStarConfig(StringName skinName, int straightFireMethodFlags)
	{
		(StringName, int) key = (skinName, straightFireMethodFlags);
		if (_straightStarConfigCache.TryGetValue(key, out var value))
		{
			return value;
		}
		bool usedTrackTemplate;
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = BuildChangedProjectileConfig(StarStringName, skinName, straightFireMethodFlags, out usedTrackTemplate);
		if (towerDefenseProjectileConfig != null)
		{
			_straightStarConfigCache[key] = towerDefenseProjectileConfig;
		}
		return towerDefenseProjectileConfig;
	}

	private static StringName GetTrackProjectileResourceName(StringName projectileName)
	{
		if (projectileName == null || projectileName == EmptyStringName)
		{
			return EmptyStringName;
		}
		return new StringName(projectileName.ToString() + "Track");
	}

	private static StringName GetProjectileChangeLookupName(StringName projectileName)
	{
		if (projectileName == null || projectileName == EmptyStringName)
		{
			return projectileName;
		}
		if (_projectileChangeLookupNameCache.TryGetValue(projectileName, out var value))
		{
			return value;
		}
		string text = projectileName.ToString();
		StringName stringName = projectileName;
		if (text.Length > "Track".Length && text.EndsWith("Track", StringComparison.Ordinal))
		{
			stringName = new StringName(text.Substring(0, text.Length - "Track".Length));
		}
		_projectileChangeLookupNameCache[projectileName] = stringName;
		return stringName;
	}

	private static TowerDefenseProjectileConfig BuildChangedProjectileConfig(StringName toProjectileName, StringName skinName, int sourceFireMethodFlags, out bool usedTrackTemplate)
	{
		usedTrackTemplate = false;
		if ((sourceFireMethodFlags & 0x20) != 0)
		{
			TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig(GetTrackProjectileResourceName(toProjectileName).ToString());
			if (projectileConfig != null)
			{
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = (TowerDefenseProjectileConfig)projectileConfig.Duplicate(deep: true);
				towerDefenseProjectileConfig.fireMethodFlags = sourceFireMethodFlags;
				towerDefenseProjectileConfig.skinName = skinName;
				if (TowerDefenseProjectileRegistry.HasProjectileSkin(toProjectileName, skinName))
				{
					PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(toProjectileName, skinName);
					if (projectileSkinProjectileScene != null)
					{
						towerDefenseProjectileConfig.projectileScene = projectileSkinProjectileScene;
					}
					PackedScene projectileSkinSplatScene = TowerDefenseProjectileRegistry.GetProjectileSkinSplatScene(toProjectileName, skinName);
					if (projectileSkinSplatScene != null)
					{
						towerDefenseProjectileConfig.splatScene = projectileSkinSplatScene;
					}
				}
				usedTrackTemplate = true;
				return towerDefenseProjectileConfig;
			}
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = new TowerDefenseProjectileCreateData(toProjectileName)
		{
			skinName = skinName,
			fireMethodFlags = sourceFireMethodFlags
		}.BuildConfig();
		if (towerDefenseProjectileConfig2 != null)
		{
			towerDefenseProjectileConfig2.fireMethodFlags = sourceFireMethodFlags;
		}
		return towerDefenseProjectileConfig2;
	}
}
