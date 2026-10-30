using Godot;
using Godot.Collections;

public sealed class RecycleComponent : CharacterComponentRuntime
{
	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private Vector2 _syncSunVelocity = Vector2.Zero;

	private bool _syncDeserializing;

	private bool _syncPayloadInitialized;

	private Vector2 _syncPayloadSunVelocity;

	private RecycleComponentDefinition Definition => base.ComponentDefinition as RecycleComponentDefinition;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	public void Recycle(float percentage = -1f, bool destroyAfterRecycle = true)
	{
		TowerDefenseCharacter owner = base.Owner;
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(owner.instance))
		{
			return;
		}
		RecycleComponentDefinition definition = Definition;
		float b = ((!(percentage < 0f)) ? percentage : (definition?.defaultPercentage ?? 0.2f));
		long num = (long)(owner.cost * (double)Mathf.Max(0f, b));
		if (num <= 0)
		{
			if (destroyAfterRecycle)
			{
				DestroyOwner(owner);
			}
			return;
		}
		float a = definition?.horizontalVelocityMin ?? (-50f);
		float b2 = definition?.horizontalVelocityMax ?? 50f;
		float num2 = Mathf.Min(a, b2);
		float num3 = Mathf.Max(a, b2);
		Vector2 vector = new Vector2((float)GD.RandRange(num2, num3), definition?.verticalVelocity ?? (-400f));
		if (_syncDeserializing)
		{
			vector = _syncSunVelocity;
			ClearPendingSyncVelocity();
		}
		else
		{
			_syncSunVelocity = vector;
		}
		float num4 = definition?.sunGravity ?? 980f;
		if (owner.instance.hypnoses)
		{
			owner.BrainSunCreate(owner.GetLogicalGlobalPosition(), num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, vector, num4);
		}
		else
		{
			owner.SunCreate(owner.GetLogicalGlobalPosition(), num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, vector, num4);
		}
		if (destroyAfterRecycle)
		{
			DestroyOwner(owner);
		}
	}

	public override Dictionary SyncSerialize()
	{
		if (_syncPayloadInitialized)
		{
			int num = ((_syncSunVelocity != Vector2.Zero) ? 2 : 0);
			if (_syncPayload.Count == num + 1)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (SyncPayloadMatchesCurrentVelocity())
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		if (_syncSunVelocity != Vector2.Zero)
		{
			_syncPayload["sun_velocity_x"] = _syncSunVelocity.X;
			_syncPayload["sun_velocity_y"] = _syncSunVelocity.Y;
		}
		_syncPayloadSunVelocity = _syncSunVelocity;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private bool SyncPayloadMatchesCurrentVelocity()
	{
		if (!(_syncSunVelocity != Vector2.Zero))
		{
			return _syncPayload.Count == 0;
		}
		if (_syncPayload.Count == 2)
		{
			return _syncPayloadSunVelocity == _syncSunVelocity;
		}
		return false;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		ClearPendingSyncVelocity();
		if (data != null && data.ContainsKey("sun_velocity_x"))
		{
			_syncSunVelocity = new Vector2(data.GetValueOrDefault("sun_velocity_x", 0f).AsSingle(), data.GetValueOrDefault("sun_velocity_y", 0f).AsSingle());
			_syncDeserializing = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearPendingSyncVelocity();
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadSunVelocity = Vector2.Zero;
	}

	private static void DestroyOwner(TowerDefenseCharacter owner)
	{
		if (GodotObject.IsInstanceValid(owner))
		{
			owner.isShovel = true;
			owner.Destroy();
		}
	}

	private void ClearPendingSyncVelocity()
	{
		_syncSunVelocity = Vector2.Zero;
		_syncDeserializing = false;
	}
}
