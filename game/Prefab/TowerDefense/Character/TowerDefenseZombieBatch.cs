using System;
using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseZombieBatch : Node
{
	private static bool _mountRequested;

	private readonly List<TowerDefenseZombie> _active = new List<TowerDefenseZombie>();

	private readonly Dictionary<TowerDefenseZombie, int> _activeIndices = new Dictionary<TowerDefenseZombie, int>(ReferenceEqualityComparer.Instance);

	private readonly List<TowerDefenseZombie> _processActive = new List<TowerDefenseZombie>();

	private readonly Dictionary<TowerDefenseZombie, int> _processActiveIndices = new Dictionary<TowerDefenseZombie, int>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<Type, string> _physicsMetricNames = new Dictionary<Type, string>();

	public static TowerDefenseZombieBatch Instance { get; private set; }

	public static bool Register(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie) || !zombie.IsInsideTree())
		{
			return false;
		}
		if (!HasInstance())
		{
			TryMount(zombie);
		}
		if (!HasInstance())
		{
			return false;
		}
		TowerDefenseZombieBatch instance = Instance;
		zombie.PrepareBatchRegistration();
		if (instance._activeIndices.TryAdd(zombie, instance._active.Count))
		{
			instance._active.Add(zombie);
		}
		instance.UpdateProcessMembership(zombie);
		zombie.SetOwnerBatchRegistration(registered: true);
		instance.SetProcess(instance._processActive.Count > 0);
		instance.SetPhysicsProcess(enable: true);
		return true;
	}

	public static void UpdateProcessDispatch(TowerDefenseZombie zombie)
	{
		if (HasInstance() && Instance._activeIndices.ContainsKey(zombie))
		{
			Instance.UpdateProcessMembership(zombie);
			Instance.SetProcess(Instance._processActive.Count > 0);
		}
	}

	private static bool HasInstance()
	{
		return GodotObject.IsInstanceValid(Instance);
	}

	public static void Unregister(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			zombie?.SetOwnerBatchRegistration(registered: false);
			return;
		}
		if (Instance._activeIndices.TryGetValue(zombie, out var value))
		{
			Instance.RemoveAtSwap(value);
		}
		zombie?.SetOwnerBatchRegistration(registered: false);
		Instance.SetProcess(Instance._processActive.Count > 0);
		if (Instance._active.Count == 0)
		{
			Instance.SetPhysicsProcess(enable: false);
		}
	}

	private static void TryMount(TowerDefenseZombie source)
	{
		if (!_mountRequested || !GodotObject.IsInstanceValid(Instance))
		{
			Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(source);
			if (GodotObject.IsInstanceValid(node))
			{
				TowerDefenseZombieBatch towerDefenseZombieBatch = (Instance = new TowerDefenseZombieBatch());
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, towerDefenseZombieBatch);
			}
		}
	}

	public override void _Ready()
	{
		Instance = this;
		_mountRequested = false;
		base.ProcessMode = ProcessModeEnum.Always;
		SetProcess(_processActive.Count > 0);
		SetPhysicsProcess(_active.Count > 0);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
			_mountRequested = false;
		}
		for (int i = 0; i < _active.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_active[i]))
			{
				_active[i].SetOwnerBatchRegistration(registered: false);
			}
		}
		_active.Clear();
		_activeIndices.Clear();
		_processActive.Clear();
		_processActiveIndices.Clear();
		_physicsMetricNames.Clear();
	}

	public override void _Process(double delta)
	{
		if (_processActive.Count == 0)
		{
			SetProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool treePaused = GetTree()?.Paused ?? false;
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		int num = 0;
		int num2 = 0;
		while (num2 < _processActive.Count)
		{
			TowerDefenseZombie towerDefenseZombie = _processActive[num2];
			if (!GodotObject.IsInstanceValid(towerDefenseZombie) || !towerDefenseZombie.IsOwnerBatchDispatchActive || !towerDefenseZombie.WantsMainStateMachineProcessDispatch)
			{
				RemoveProcessAtSwap(num2);
				continue;
			}
			if (towerDefenseZombie.isPause)
			{
				num2++;
				continue;
			}
			if (!(towerDefenseZombie.BatchUsesInheritedProcessMode ? TowerDefenseProcessModeDispatch.ShouldDispatchInherited(towerDefenseZombie.BatchProcessModeParent, treePaused) : TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseZombie, treePaused)))
			{
				num2++;
				continue;
			}
			towerDefenseZombie.BatchProcessUpdate(delta);
			num++;
			if (num2 < _processActive.Count && _processActive[num2] == towerDefenseZombie)
			{
				num2++;
			}
		}
		TowerDefensePerfProfiler.End("batch.zombieProcess", startTicks, num);
		if (_processActive.Count == 0)
		{
			SetProcess(enable: false);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_active.Count == 0)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		TowerDefenseProcessModeDispatch.EnterCharacterPhysicsBatchDispatch();
		try
		{
			long startTicks = TowerDefensePerfProfiler.Begin();
			bool treePaused = GetTree()?.Paused ?? false;
			TowerDefenseProcessModeDispatch.BeginFrame();
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			TowerDefenseProcessModeDispatch.BeginDispatchPass();
			bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
			int num = 0;
			int num2 = 0;
			while (num2 < _active.Count)
			{
				TowerDefenseZombie towerDefenseZombie = _active[num2];
				if (!towerDefenseZombie.IsOwnerBatchDispatchActive)
				{
					RemoveAtSwap(num2);
					continue;
				}
				if (towerDefenseZombie.isPause)
				{
					num2++;
					continue;
				}
				if (!(towerDefenseZombie.BatchUsesInheritedProcessMode ? TowerDefenseProcessModeDispatch.ShouldDispatchInherited(towerDefenseZombie.BatchProcessModeParent, treePaused) : TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseZombie, treePaused)))
				{
					num2++;
					continue;
				}
				if (flag)
				{
					long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
					towerDefenseZombie.BatchUpdateValidated(delta, currentPhysicsFrame);
					TowerDefensePerfProfiler.End(GetPhysicsMetricName(towerDefenseZombie), startTicks2);
				}
				else
				{
					towerDefenseZombie.BatchUpdateValidated(delta, currentPhysicsFrame);
				}
				num++;
				if (num2 < _active.Count && _active[num2] == towerDefenseZombie)
				{
					num2++;
				}
			}
			TowerDefensePerfProfiler.End("batch.zombie", startTicks, num);
			TowerDefensePerfProfiler.DumpIfNeeded();
			if (_active.Count == 0)
			{
				SetPhysicsProcess(enable: false);
			}
		}
		finally
		{
			TowerDefenseProcessModeDispatch.ExitCharacterPhysicsBatchDispatch();
		}
	}

	private string GetPhysicsMetricName(TowerDefenseZombie zombie)
	{
		Type type = zombie.GetType();
		if (_physicsMetricNames.TryGetValue(type, out var value))
		{
			return value;
		}
		value = "batch.zombie.type." + type.Name;
		_physicsMetricNames.Add(type, value);
		return value;
	}

	private void RemoveAtSwap(int index)
	{
		int num = _active.Count - 1;
		TowerDefenseZombie towerDefenseZombie = _active[index];
		RemoveProcess(towerDefenseZombie);
		if (index != num)
		{
			TowerDefenseZombie towerDefenseZombie2 = _active[num];
			_active[index] = towerDefenseZombie2;
			_activeIndices[towerDefenseZombie2] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(towerDefenseZombie);
		if (GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			towerDefenseZombie.SetOwnerBatchRegistration(registered: false);
		}
	}

	private void UpdateProcessMembership(TowerDefenseZombie zombie)
	{
		if (zombie.WantsMainStateMachineProcessDispatch)
		{
			if (_processActiveIndices.TryAdd(zombie, _processActive.Count))
			{
				_processActive.Add(zombie);
			}
		}
		else
		{
			RemoveProcess(zombie);
		}
	}

	private void RemoveProcess(TowerDefenseZombie zombie)
	{
		if (_processActiveIndices.TryGetValue(zombie, out var value))
		{
			RemoveProcessAtSwap(value);
		}
	}

	private void RemoveProcessAtSwap(int index)
	{
		int num = _processActive.Count - 1;
		TowerDefenseZombie key = _processActive[index];
		if (index != num)
		{
			TowerDefenseZombie towerDefenseZombie = _processActive[num];
			_processActive[index] = towerDefenseZombie;
			_processActiveIndices[towerDefenseZombie] = index;
		}
		_processActive.RemoveAt(num);
		_processActiveIndices.Remove(key);
	}
}
