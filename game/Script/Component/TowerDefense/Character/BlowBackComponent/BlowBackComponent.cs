using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class BlowBackComponent : CharacterComponentRuntime
{
	private sealed class BlowBackLease
	{
		public double Value;

		public SceneTreeTimer Timer;

		public Action Handler;
	}

	public double defaultDuration = 1.0;

	public bool allowStacking = true;

	public TowerDefenseCharacter parent;

	public bool blowBack;

	public double blowBackNum;

	public int _blowBackTweens;

	private List<BlowBackLease> _leases;

	private bool _configured;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncBlowBackKey = new StringName("blowBack");

	private static readonly StringName SyncBlowBackNumKey = new StringName("blowBackNum");

	private static readonly StringName SyncBlowBackTweensKey = new StringName("blowBackTweens");

	private bool _syncPayloadInitialized;

	private bool _syncPayloadBlowBack;

	private double _syncPayloadBlowBackNum;

	private int _syncPayloadBlowBackTweens;

	private BlowBackComponentDefinition Definition => base.ComponentDefinition as BlowBackComponentDefinition;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			BlowBackComponentDefinition definition = Definition;
			defaultDuration = definition?.defaultDuration ?? 1.0;
			allowStacking = definition?.allowStacking ?? true;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearLeases();
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearLeases();
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			ClearLeases();
		}
	}

	public void BlowBack(double num, double time = -1.0)
	{
		TowerDefenseCharacter towerDefenseCharacter = parent;
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter.instance) || num == 0.0 || (towerDefenseCharacter.instance.collisionFlags & 0x10) != 0 || (towerDefenseCharacter.instance.unUseBuffFlags & 0x200) != 0 || !(towerDefenseCharacter is TowerDefenseZombie) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		double num2 = ((time < 0.0) ? defaultDuration : time);
		double num3 = (double)TowerDefenseManager.Instance.GetMapGridSize().X * num;
		if (num2 <= 0.0)
		{
			towerDefenseCharacter.SetLogicalGlobalPosition(towerDefenseCharacter.GetLogicalGlobalPosition() + new Vector2((float)num3, 0f));
			return;
		}
		if (!allowStacking)
		{
			ClearLeases();
		}
		SceneTree tree = towerDefenseCharacter.GetTree();
		if (GodotObject.IsInstanceValid(tree))
		{
			BlowBackLease lease = new BlowBackLease
			{
				Value = num3 / num2,
				Timer = tree.CreateTimer(num2, processAlways: false)
			};
			lease.Handler = delegate
			{
				CompleteLease(lease);
			};
			lease.Timer.Timeout += lease.Handler;
			if (_leases == null)
			{
				_leases = new List<BlowBackLease>();
			}
			_leases.Add(lease);
			blowBackNum += lease.Value;
			_blowBackTweens = _leases.Count;
			blowBack = true;
		}
	}

	private void CompleteLease(BlowBackLease lease)
	{
		DetachLease(lease);
		if (_leases != null && _leases.Remove(lease))
		{
			blowBackNum -= lease.Value;
			_blowBackTweens = _leases.Count;
			if (_leases.Count == 0)
			{
				blowBackNum = 0.0;
				blowBack = false;
			}
		}
	}

	private void ClearLeases()
	{
		if (_leases != null)
		{
			foreach (BlowBackLease lease in _leases)
			{
				DetachLease(lease);
			}
			_leases.Clear();
		}
		blowBack = false;
		blowBackNum = 0.0;
		_blowBackTweens = 0;
	}

	private static void DetachLease(BlowBackLease lease)
	{
		if (GodotObject.IsInstanceValid(lease.Timer) && lease.Handler != null)
		{
			lease.Timer.Timeout -= lease.Handler;
		}
		lease.Timer = null;
		lease.Handler = null;
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = blowBack;
		double num = blowBackNum;
		int blowBackTweens = _blowBackTweens;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 4)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 3 && _syncPayloadBlowBack == flag && _syncPayloadBlowBackNum == num && _syncPayloadBlowBackTweens == blowBackTweens)
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		WriteSyncPayload(flag, num, blowBackTweens);
		_syncPayloadBlowBack = flag;
		_syncPayloadBlowBackNum = num;
		_syncPayloadBlowBackTweens = blowBackTweens;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void WriteSyncPayload(bool currentBlowBack, double currentBlowBackNum, int currentBlowBackTweens)
	{
		_syncPayload[SyncBlowBackKey] = currentBlowBack;
		_syncPayload[SyncBlowBackNumKey] = currentBlowBackNum;
		_syncPayload[SyncBlowBackTweensKey] = currentBlowBackTweens;
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadBlowBack = false;
		_syncPayloadBlowBackNum = 0.0;
		_syncPayloadBlowBackTweens = 0;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		ClearLeases();
		if (data != null)
		{
			blowBackNum = data.GetValueOrDefault("blowBackNum", 0.0).AsDouble();
			_blowBackTweens = Mathf.Max(0, data.GetValueOrDefault("blowBackTweens", 0).AsInt32());
			blowBack = base.Alive && data.GetValueOrDefault("blowBack", false).AsBool() && _blowBackTweens > 0;
			if (!blowBack)
			{
				blowBackNum = 0.0;
			}
		}
	}
}
