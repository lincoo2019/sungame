using System;
using Godot;
using Godot.Collections;

public sealed class ResourceSpawnComponent : CharacterComponentRuntime
{
	private const string DefaultHealthEffectPath = "uid://b8c40r4tk45sf";

	private const string SunCreateOperationName = "sun_create";

	private const string SunKind = "sun";

	private const string BrainSunKind = "brain_sun";

	public const int MaxEffectCount = 100;

	private static PackedScene _healthCache;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	public Vector2 _syncLastSunVelocity = Vector2.Zero;

	public Vector2 _syncLastCoinVelocity = Vector2.Zero;

	private bool _syncSunVelocityPending;

	private bool _syncCoinVelocityPending;

	private long _nextSunCreateSequence;

	private long _lastAppliedSunCreateSequence;

	private bool _syncPayloadInitialized;

	private Vector2 _syncPayloadSunVelocity;

	private Vector2 _syncPayloadCoinVelocity;

	public static PackedScene HEALTH
	{
		get
		{
			return _healthCache ?? (_healthCache = GD.Load<PackedScene>("uid://b8c40r4tk45sf"));
		}
		set
		{
			_healthCache = value;
		}
	}

	private ResourceSpawnComponentDefinition Definition => base.ComponentDefinition as ResourceSpawnComponentDefinition;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	public bool _syncDeserializing
	{
		get
		{
			if (!_syncSunVelocityPending)
			{
				return _syncCoinVelocityPending;
			}
			return true;
		}
		set
		{
			if (value)
			{
				_syncSunVelocityPending = _syncLastSunVelocity != Vector2.Zero;
				_syncCoinVelocityPending = _syncLastCoinVelocity != Vector2.Zero;
			}
			else
			{
				_syncSunVelocityPending = false;
				_syncCoinVelocityPending = false;
			}
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_syncSunVelocityPending = false;
		_syncCoinVelocityPending = false;
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadSunVelocity = Vector2.Zero;
		_syncPayloadCoinVelocity = Vector2.Zero;
	}

	protected override void OnReleased()
	{
		_syncSunVelocityPending = false;
		_syncCoinVelocityPending = false;
		_nextSunCreateSequence = 0L;
		_lastAppliedSunCreateSequence = 0L;
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadSunVelocity = Vector2.Zero;
		_syncPayloadCoinVelocity = Vector2.Zero;
	}

	public TowerDefenseInGamePacketShow SpawnPacket(TowerDefensePacketConfig packetConfig, Vector2 pos, float aliveTime, bool isFall, bool useCost = false, bool useRandf = true, Vector2? velocityOverride = null)
	{
		if (!TryGetRuntime(out var owner, out var manager) || !GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		if (!owner.HasEconomyOwner)
		{
			return manager.SpawnPacket(packetConfig, pos, aliveTime, isFall, useCost, useRandf, velocityOverride);
		}
		return manager.SpawnPacket(owner.EconomyOwnerAccountId, packetConfig, pos, aliveTime, isFall, useCost, useRandf, velocityOverride);
	}

	public void YBCreate(Vector2 pos, int num, Vector2 velocity = default(Vector2), float gravity = 980f, bool collect = false)
	{
		SilverCoinCreate(pos, num, velocity, gravity, collect);
	}

	public void SilverCoinCreate(Vector2 pos, int num, Vector2 velocity = default(Vector2), float gravity = 980f, bool collect = false)
	{
		if (TryGetRuntime(out var owner, out var manager) && num > 0)
		{
			velocity = ResolveVelocity(velocity);
			manager.YBCreate(pos, num, GetSpawnHeight(owner, pos), velocity, gravity, collect);
		}
	}

	public void CoinCreate(Vector2 pos, int num, Vector2 velocity = default(Vector2), float gravity = 980f, bool collect = false)
	{
		GoldCoinCreate(pos, num, velocity, gravity, collect);
	}

	public void GoldCoinCreate(Vector2 pos, int num, Vector2 velocity = default(Vector2), float gravity = 980f, bool collect = false)
	{
		if (TryGetRuntime(out var owner, out var manager) && num > 0)
		{
			velocity = ResolveVelocity(velocity);
			if (_syncCoinVelocityPending)
			{
				velocity = _syncLastCoinVelocity;
				_syncCoinVelocityPending = false;
			}
			else
			{
				_syncLastCoinVelocity = velocity;
			}
			manager.CoinCreate(pos, num, GetSpawnHeight(owner, pos), velocity, gravity, collect);
		}
	}

	public void LuckyBagCreate(Vector2 pos, Vector2 velocity = default(Vector2), float gravity = 980f)
	{
		if (TryGetRuntime(out var owner, out var manager))
		{
			manager.LuckyBagCreate(pos, GetSpawnHeight(owner, pos), ResolveVelocity(velocity), gravity);
		}
	}

	public void GoldShardCreate(Vector2 pos, Vector2 velocity = default(Vector2), float gravity = 980f)
	{
		if (TryGetRuntime(out var owner, out var manager))
		{
			manager.GoldShardCreate(pos, GetSpawnHeight(owner, pos), ResolveVelocity(velocity), gravity);
		}
	}

	public TowerDefenseSunBase SunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0)
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		if (_syncSunVelocityPending)
		{
			velocity = _syncLastSunVelocity;
			_syncSunVelocityPending = false;
		}
		else
		{
			_syncLastSunVelocity = velocity;
		}
		double spawnHeight = GetSpawnHeight(owner, pos);
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			return manager.SunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		return manager.SunCreate(pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase BrainSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0)
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		double spawnHeight = GetSpawnHeight(owner, pos);
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			return manager.BrainSunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		return manager.BrainSunCreate(pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase ReplicatedEventSunCreate(Vector2 pos, long sunNum, bool createBrainSun, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0 || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		double spawnHeight = GetSpawnHeight(owner, pos);
		TowerDefenseSunBase towerDefenseSunBase = CreateExactSunDrop(owner, manager, createBrainSun, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		if (!GodotObject.IsInstanceValid(towerDefenseSunBase))
		{
			return null;
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && owner.syncId >= 0)
		{
			string text = ((towerDefenseSunBase.GetPoolKey() == ObjectManagerConfig.OBJECT.SUN_BRAIN) ? "brain_sun" : "sun");
			Dictionary data = new Dictionary
			{
				{ "kind", text },
				{ "amount", towerDefenseSunBase.sunNum },
				{
					"position_x",
					towerDefenseSunBase.GlobalPosition.X
				},
				{
					"position_y",
					towerDefenseSunBase.GlobalPosition.Y
				},
				{
					"moving_method",
					(int)towerDefenseSunBase.movingMethod
				},
				{ "height", towerDefenseSunBase.height },
				{ "velocity_x", velocity.X },
				{ "velocity_y", velocity.Y },
				{ "gravity", gravity },
				{ "move_stop_time", moveStopTime },
				{
					"economy_owner",
					towerDefenseSunBase.AccountId.ToString()
				},
				{
					"ownership_policy",
					(int)towerDefenseSunBase.OwnershipPolicy
				}
			};
			SendNetworkOperation("sun_create", ++_nextSunCreateSequence, data);
		}
		return towerDefenseSunBase;
	}

	public TowerDefenseSunBase JalapenoSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0)
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		double spawnHeight = GetSpawnHeight(owner, pos);
		TowerDefenseSunBase towerDefenseSunBase;
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			towerDefenseSunBase = manager.JalapenoSunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		else
		{
			towerDefenseSunBase = manager.JalapenoSunCreate(pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		if (GodotObject.IsInstanceValid(towerDefenseSunBase))
		{
			towerDefenseSunBase.gridPos = owner.gridPos;
		}
		return towerDefenseSunBase;
	}

	public TowerDefenseSunBase QXSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0)
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		if (_syncSunVelocityPending)
		{
			velocity = _syncLastSunVelocity;
			_syncSunVelocityPending = false;
		}
		else
		{
			_syncLastSunVelocity = velocity;
		}
		double spawnHeight = GetSpawnHeight(owner, pos);
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			return manager.QXSunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		return manager.QXSunCreate(pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase MagicSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2 velocity = default(Vector2), float gravity = 980f, float moveStopTime = -1f)
	{
		if (!TryGetRuntime(out var owner, out var manager) || sunNum <= 0)
		{
			return null;
		}
		velocity = ResolveVelocity(velocity);
		if (_syncSunVelocityPending)
		{
			velocity = _syncLastSunVelocity;
			_syncSunVelocityPending = false;
		}
		else
		{
			_syncLastSunVelocity = velocity;
		}
		double spawnHeight = GetSpawnHeight(owner, pos);
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			return manager.MagicSunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
		}
		return manager.MagicSunCreate(pos, sunNum, movingMethod, spawnHeight, velocity, gravity, moveStopTime);
	}

	public void ExplodeSunCreate(Vector2 pos, long sunNum, long sunOnce, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, float speed = 0f, float gravity = 0f, float moveStopTime = -1f)
	{
		if (sunNum > 0 && sunOnce > 0)
		{
			long num = sunNum;
			while (num > 0)
			{
				long num2 = Math.Min(num, sunOnce);
				Vector2 velocity = Vector2.FromAngle((float)Math.PI / 2f + (float)GD.RandRange(-0.2617993950843811, 0.2617993950843811)) * speed * (float)GD.RandRange(0.5, 1.5);
				SunCreate(pos, num2, movingMethod, velocity, gravity, moveStopTime);
				num -= num2;
			}
		}
	}

	public void HealthEffect(float num)
	{
		if (!TryGetRuntime(out var owner, out var manager) || !GodotObject.IsInstanceValid(owner.instance))
		{
			return;
		}
		owner.instance.Health(num);
		bool num2 = Definition?.showHealthEffect ?? true;
		PackedScene packedScene = Definition?.healthEffectScene;
		if (packedScene == null)
		{
			packedScene = HEALTH;
		}
		int b = Definition?.maxEffectCount ?? 100;
		if (num2 && GodotObject.IsInstanceValid(packedScene) && manager.GetEffectCount() < Mathf.Max(0, b))
		{
			string clip = Definition?.healthEffectClip ?? "Idle";
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(packedScene, owner.gridPos, clip);
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce) && GodotObject.IsInstanceValid(characterNode))
			{
				characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.gridPos = owner.gridPos;
				Vector2 logicalGlobalPosition = owner.GetLogicalGlobalPosition();
				float x = (GodotObject.IsInstanceValid(owner.shadowSprite) ? owner.GetLogicalGlobalPosition(owner.shadowSprite).X : logicalGlobalPosition.X);
				ShadowComponent shadowComponent = owner.shadowComponent;
				float y = ((shadowComponent != null && !shadowComponent.IsReleased) ? owner.shadowComponent.GetShadowPosition().Y : logicalGlobalPosition.Y);
				towerDefenseEffectSpriteOnce.GlobalPosition = new Vector2(x, y);
			}
		}
	}

	private Vector2 ResolveVelocity(Vector2 velocity)
	{
		if (velocity != Vector2.Zero)
		{
			return velocity;
		}
		Vector2 vector = Definition?.randomVelocityXRange ?? ResourceSpawnComponentDefinition.DefaultRandomVelocityXRange;
		float num = Mathf.Min(vector.X, vector.Y);
		float num2 = Mathf.Max(vector.X, vector.Y);
		return new Vector2(y: Definition?.defaultLaunchVelocityY ?? (-400f), x: (float)GD.RandRange(num, num2));
	}

	private double GetSpawnHeight(TowerDefenseCharacter owner, Vector2 pos)
	{
		float num = Definition?.groundHeightMultiplier ?? 2f;
		return owner.GetGroundHeight(pos.Y) - owner.groundHeight * (double)num;
	}

	private bool TryGetRuntime(out TowerDefenseCharacter owner, out TowerDefenseManager manager)
	{
		owner = base.Owner;
		manager = TowerDefenseManager.Instance;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(owner))
		{
			return GodotObject.IsInstanceValid(manager);
		}
		return false;
	}

	private static TowerDefenseSunBase CreateExactSunDrop(TowerDefenseCharacter owner, TowerDefenseManager manager, bool createBrainSun, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod, double height, Vector2 velocity, float gravity, float moveStopTime)
	{
		if (owner.HasEconomyOwner)
		{
			if (!manager.TryGetSun(owner.EconomyOwnerAccountId, out var _))
			{
				return null;
			}
			if (!createBrainSun)
			{
				return manager.SunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
			}
			return manager.BrainSunCreate(owner.EconomyOwnerAccountId, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
		}
		if (!createBrainSun)
		{
			return manager.SunCreate(pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
		}
		return manager.BrainSunCreate(pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public override void ApplyNetworkOperation(string operationName, long sequence, Dictionary operation)
	{
		if (!string.Equals(operationName, "sun_create", StringComparison.Ordinal) || operation == null || sequence <= _lastAppliedSunCreateSequence || !Global.IsMultiplayerMode || MultiPlayerManager.IsHost || !TryGetRuntime(out var owner, out var manager) || owner.syncId < 0)
		{
			return;
		}
		string a = operation.GetValueOrDefault("kind", "").AsString();
		bool flag;
		if (string.Equals(a, "sun", StringComparison.Ordinal))
		{
			flag = false;
		}
		else
		{
			if (!string.Equals(a, "brain_sun", StringComparison.Ordinal))
			{
				return;
			}
			flag = true;
		}
		long num = operation.GetValueOrDefault("amount", 0L).AsInt64();
		int num2 = operation.GetValueOrDefault("moving_method", -1).AsInt32();
		if (num <= 0 || !Enum.IsDefined(typeof(TowerDefenseEnum.SUN_MOVING_METHOD), num2))
		{
			return;
		}
		float num3 = operation.GetValueOrDefault("position_x", 0f).AsSingle();
		float num4 = operation.GetValueOrDefault("position_y", 0f).AsSingle();
		float num5 = operation.GetValueOrDefault("height", 0f).AsSingle();
		float num6 = operation.GetValueOrDefault("velocity_x", 0f).AsSingle();
		float num7 = operation.GetValueOrDefault("velocity_y", 0f).AsSingle();
		float num8 = operation.GetValueOrDefault("gravity", 980f).AsSingle();
		float num9 = operation.GetValueOrDefault("move_stop_time", -1f).AsSingle();
		if (!float.IsFinite(num3) || !float.IsFinite(num4) || !float.IsFinite(num5) || !float.IsFinite(num6) || !float.IsFinite(num7) || !float.IsFinite(num8) || !float.IsFinite(num9))
		{
			return;
		}
		int num10 = operation.GetValueOrDefault("ownership_policy", -1).AsInt32();
		if (!Enum.IsDefined(typeof(SunDropOwnershipPolicy), num10))
		{
			return;
		}
		SunDropOwnershipPolicy sunDropOwnershipPolicy = (SunDropOwnershipPolicy)num10;
		string text = operation.GetValueOrDefault("economy_owner", "").AsString();
		EconomyAccountId accountId;
		if (sunDropOwnershipPolicy == SunDropOwnershipPolicy.AccountOwned)
		{
			if (!EconomyAccountId.TryParse(text, out accountId) || !accountId.IsValid || !manager.TryGetSun(accountId, out var _) || !owner.TryAssignEconomyOwner(accountId))
			{
				return;
			}
		}
		else
		{
			if (!string.Equals(text, "local", StringComparison.Ordinal) || owner.HasEconomyOwner)
			{
				return;
			}
			accountId = EconomyAccountId.Local;
		}
		Vector2 pos = new Vector2(num3, num4);
		Vector2 velocity = new Vector2(num6, num7);
		TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = (TowerDefenseEnum.SUN_MOVING_METHOD)num2;
		if (GodotObject.IsInstanceValid((sunDropOwnershipPolicy != SunDropOwnershipPolicy.AccountOwned) ? (flag ? manager.BrainSunCreate(pos, num, movingMethod, num5, velocity, num8, num9) : manager.SunCreate(pos, num, movingMethod, num5, velocity, num8, num9)) : (flag ? manager.BrainSunCreate(accountId, pos, num, movingMethod, num5, velocity, num8, num9) : manager.SunCreate(accountId, pos, num, movingMethod, num5, velocity, num8, num9))))
		{
			_lastAppliedSunCreateSequence = sequence;
		}
	}

	public override Dictionary SyncSerialize()
	{
		if (_syncPayloadInitialized)
		{
			int currentSyncPayloadCount = GetCurrentSyncPayloadCount();
			if (_syncPayload.Count == currentSyncPayloadCount + 1)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (SyncPayloadMatchesCurrentValues())
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		if (_syncLastSunVelocity != Vector2.Zero)
		{
			_syncPayload["sun_velocity_x"] = _syncLastSunVelocity.X;
			_syncPayload["sun_velocity_y"] = _syncLastSunVelocity.Y;
		}
		if (_syncLastCoinVelocity != Vector2.Zero)
		{
			_syncPayload["coin_velocity_x"] = _syncLastCoinVelocity.X;
			_syncPayload["coin_velocity_y"] = _syncLastCoinVelocity.Y;
		}
		_syncPayloadSunVelocity = _syncLastSunVelocity;
		_syncPayloadCoinVelocity = _syncLastCoinVelocity;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private bool SyncPayloadMatchesCurrentValues()
	{
		bool num = _syncLastSunVelocity != Vector2.Zero;
		bool flag = _syncLastCoinVelocity != Vector2.Zero;
		int num2 = (num ? 2 : 0) + (flag ? 2 : 0);
		if (_syncPayload.Count == num2 && _syncPayloadSunVelocity == _syncLastSunVelocity)
		{
			return _syncPayloadCoinVelocity == _syncLastCoinVelocity;
		}
		return false;
	}

	private int GetCurrentSyncPayloadCount()
	{
		return ((_syncLastSunVelocity != Vector2.Zero) ? 2 : 0) + ((_syncLastCoinVelocity != Vector2.Zero) ? 2 : 0);
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data.ContainsKey("sun_velocity_x"))
		{
			_syncLastSunVelocity = new Vector2(data.GetValueOrDefault("sun_velocity_x", 0f).AsSingle(), data.GetValueOrDefault("sun_velocity_y", 0f).AsSingle());
			_syncSunVelocityPending = true;
		}
		if (data.ContainsKey("coin_velocity_x"))
		{
			_syncLastCoinVelocity = new Vector2(data.GetValueOrDefault("coin_velocity_x", 0f).AsSingle(), data.GetValueOrDefault("coin_velocity_y", 0f).AsSingle());
			_syncCoinVelocityPending = true;
		}
	}
}
