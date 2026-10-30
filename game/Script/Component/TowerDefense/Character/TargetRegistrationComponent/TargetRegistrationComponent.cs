using Godot;
using Godot.Collections;

public sealed class TargetRegistrationComponent : CharacterComponentRuntime
{
	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncAllLineKey = new StringName("all_line");

	private static readonly StringName SyncProjectileKey = new StringName("projectile");

	private static readonly StringName SyncCarryKey = new StringName("carry");

	private bool _allLineCheck;

	private bool _canProjectileCheck = true;

	private bool _canCarry = true;

	private bool _syncPayloadAllLineCheck;

	private bool _syncPayloadCanProjectileCheck;

	private bool _syncPayloadCanCarry;

	private bool _syncPayloadInitialized;

	private int _attackGridColumnAliasOffset;

	private int _attackGridLineAliasOffset;

	private bool _configured;

	private TargetRegistrationComponentDefinition Definition => base.ComponentDefinition as TargetRegistrationComponentDefinition;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	public bool allLineCheck
	{
		get
		{
			return _allLineCheck;
		}
		set
		{
			if (_allLineCheck != value)
			{
				_allLineCheck = value;
				NotifyTargetStateChanged();
			}
		}
	}

	public bool canProjectileCheck
	{
		get
		{
			return _canProjectileCheck;
		}
		set
		{
			_canProjectileCheck = value;
		}
	}

	public bool canCarry
	{
		get
		{
			return _canCarry;
		}
		set
		{
			_canCarry = value;
		}
	}

	public int attackGridColumnAliasOffset
	{
		get
		{
			return _attackGridColumnAliasOffset;
		}
		set
		{
			if (_attackGridColumnAliasOffset != value)
			{
				_attackGridColumnAliasOffset = value;
				NotifyTargetStateChanged();
			}
		}
	}

	public int attackGridLineAliasOffset
	{
		get
		{
			return _attackGridLineAliasOffset;
		}
		set
		{
			if (_attackGridLineAliasOffset != value)
			{
				_attackGridLineAliasOffset = value;
				NotifyTargetStateChanged();
			}
		}
	}

	protected override void OnBound()
	{
		if (!_configured)
		{
			TargetRegistrationComponentDefinition definition = Definition;
			_allLineCheck = definition?.allLineCheck ?? false;
			_canProjectileCheck = definition?.canProjectileCheck ?? true;
			_canCarry = definition?.canCarry ?? true;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearSyncPayload();
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
	}

	public void RegisterTarget()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseCharacter owner = base.Owner;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(owner))
		{
			instance.CharacterRegister(owner);
		}
	}

	public void UnregisterTarget()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseCharacter owner = base.Owner;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(owner))
		{
			instance.CharacterUnregister(owner);
		}
	}

	public void NotifyTargetStateChanged()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseCharacter owner = base.Owner;
		if (GodotObject.IsInstanceValid(instance?.characterRegistry) && GodotObject.IsInstanceValid(owner))
		{
			instance.characterRegistry.NotifyCharacterTargetingChanged(owner);
		}
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = allLineCheck;
		bool flag2 = canProjectileCheck;
		bool flag3 = canCarry;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 4)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 3 && _syncPayloadAllLineCheck == flag && _syncPayloadCanProjectileCheck == flag2 && _syncPayloadCanCarry == flag3)
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		_syncPayload[SyncAllLineKey] = flag;
		_syncPayload[SyncProjectileKey] = flag2;
		_syncPayload[SyncCarryKey] = flag3;
		_syncPayloadAllLineCheck = flag;
		_syncPayloadCanProjectileCheck = flag2;
		_syncPayloadCanCarry = flag3;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null)
		{
			allLineCheck = data.GetValueOrDefault("all_line", allLineCheck).AsBool();
			canProjectileCheck = data.GetValueOrDefault("projectile", canProjectileCheck).AsBool();
			canCarry = data.GetValueOrDefault("carry", canCarry).AsBool();
		}
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadAllLineCheck = false;
		_syncPayloadCanProjectileCheck = false;
		_syncPayloadCanCarry = false;
		_syncPayloadInitialized = false;
	}
}
