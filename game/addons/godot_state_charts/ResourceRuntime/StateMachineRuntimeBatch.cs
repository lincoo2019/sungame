using System;
using System.Collections.Generic;
using Godot;

public partial class StateMachineRuntimeBatch : Node
{
	private readonly struct PendingUpdate(StateMachineRuntime runtime, bool processActive, bool physicsActive, bool remove)
	{
		public readonly StateMachineRuntime Runtime = runtime;

		public readonly bool ProcessActive = processActive;

		public readonly bool PhysicsActive = physicsActive;

		public readonly bool Remove = remove;
	}

	private readonly Dictionary<StateChart, StateMachineRuntime> _runtimes = new Dictionary<StateChart, StateMachineRuntime>();

	private readonly List<StateChart> _processHosts = new List<StateChart>();

	private readonly List<StateMachineRuntime> _processRuntimes = new List<StateMachineRuntime>();

	private readonly List<Action<double>> _processCallbacks = new List<Action<double>>();

	private readonly Dictionary<StateChart, int> _processIndices = new Dictionary<StateChart, int>();

	private readonly List<StateChart> _physicsHosts = new List<StateChart>();

	private readonly List<StateMachineRuntime> _physicsRuntimes = new List<StateMachineRuntime>();

	private readonly List<Action<double>> _physicsCallbacks = new List<Action<double>>();

	private readonly Dictionary<StateChart, int> _physicsIndices = new Dictionary<StateChart, int>();

	private readonly Dictionary<StateChart, PendingUpdate> _pendingUpdates = new Dictionary<StateChart, PendingUpdate>();

	private bool _dispatching;

	public static StateMachineRuntimeBatch Instance { get; private set; }

	public static int RegistrationCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._runtimes.Count;
		}
	}

	public static int ProcessRegistrationCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._processCallbacks.Count;
		}
	}

	public static int PhysicsRegistrationCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._physicsCallbacks.Count;
		}
	}

	public static bool Register(StateChart host, StateMachineRuntime runtime, bool processActive, bool physicsActive)
	{
		if (!GodotObject.IsInstanceValid(host) || runtime == null)
		{
			return false;
		}
		StateMachineRuntimeBatch stateMachineRuntimeBatch = EnsureMounted(host);
		if (!GodotObject.IsInstanceValid(stateMachineRuntimeBatch))
		{
			return false;
		}
		stateMachineRuntimeBatch.RequestUpdate(host, runtime, processActive, physicsActive, remove: false);
		return true;
	}

	public static void UpdateDispatchFlags(StateChart host, bool processActive, bool physicsActive)
	{
		if (GodotObject.IsInstanceValid(Instance) && GodotObject.IsInstanceValid(host))
		{
			StateMachineRuntimeBatch instance = Instance;
			if (instance._runtimes.TryGetValue(host, out var value))
			{
				instance.RequestUpdate(host, value, processActive, physicsActive, remove: false);
			}
		}
	}

	public static void Unregister(StateChart host)
	{
		if (GodotObject.IsInstanceValid(Instance) && host != null)
		{
			StateMachineRuntimeBatch instance = Instance;
			instance._runtimes.TryGetValue(host, out var value);
			instance.RequestUpdate(host, value, processActive: false, physicsActive: false, remove: true);
		}
	}

	public override void _Ready()
	{
		Instance = this;
		UpdateGodotProcessing();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		_runtimes.Clear();
		_processHosts.Clear();
		_processRuntimes.Clear();
		_processCallbacks.Clear();
		_processIndices.Clear();
		_physicsHosts.Clear();
		_physicsRuntimes.Clear();
		_physicsCallbacks.Clear();
		_physicsIndices.Clear();
		_pendingUpdates.Clear();
	}

	public override void _Process(double delta)
	{
		if (_processCallbacks.Count == 0)
		{
			SetProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
		bool paused = GetTree().Paused;
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		_dispatching = true;
		try
		{
			for (int i = 0; i < _processCallbacks.Count; i++)
			{
				StateChart stateChart = _processHosts[i];
				if (GodotObject.IsInstanceValid(stateChart) && TowerDefenseProcessModeDispatch.ShouldDispatch(stateChart, paused))
				{
					if (flag)
					{
						_processRuntimes[i].TickProcessDetailed(delta);
					}
					else
					{
						_processCallbacks[i](delta);
					}
				}
			}
		}
		finally
		{
			_dispatching = false;
			TowerDefensePerfProfiler.End("batch.stateResourceProcess", startTicks, _processCallbacks.Count);
			FlushPendingUpdates();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_physicsCallbacks.Count == 0)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
		bool paused = GetTree().Paused;
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		_dispatching = true;
		try
		{
			for (int i = 0; i < _physicsCallbacks.Count; i++)
			{
				StateChart stateChart = _physicsHosts[i];
				if (GodotObject.IsInstanceValid(stateChart) && TowerDefenseProcessModeDispatch.ShouldDispatch(stateChart, paused))
				{
					if (flag)
					{
						_physicsRuntimes[i].TickPhysicsDetailed(delta);
					}
					else
					{
						_physicsCallbacks[i](delta);
					}
				}
			}
		}
		finally
		{
			_dispatching = false;
			TowerDefensePerfProfiler.End("batch.stateResourcePhysics", startTicks, _physicsCallbacks.Count);
			TowerDefensePerfProfiler.DumpIfNeeded();
			FlushPendingUpdates();
		}
	}

	private static StateMachineRuntimeBatch EnsureMounted(StateChart host)
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			return Instance;
		}
		SceneTree tree = host.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return null;
		}
		Node root = tree.Root;
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		StateMachineRuntimeBatch stateMachineRuntimeBatch = (Instance = new StateMachineRuntimeBatch
		{
			Name = "StateMachineRuntimeBatch",
			ProcessMode = ProcessModeEnum.Always
		});
		root.CallDeferred("add_child", stateMachineRuntimeBatch);
		return stateMachineRuntimeBatch;
	}

	private void RequestUpdate(StateChart host, StateMachineRuntime runtime, bool processActive, bool physicsActive, bool remove)
	{
		if (_dispatching)
		{
			_pendingUpdates[host] = new PendingUpdate(runtime, processActive, physicsActive, remove);
			return;
		}
		ApplyUpdate(host, runtime, processActive, physicsActive, remove);
		UpdateGodotProcessing();
	}

	private void ApplyUpdate(StateChart host, StateMachineRuntime runtime, bool processActive, bool physicsActive, bool remove)
	{
		if (remove)
		{
			RemoveProcess(host);
			RemovePhysics(host);
			_runtimes.Remove(host);
			return;
		}
		_runtimes[host] = runtime;
		if (processActive)
		{
			AddOrUpdateProcess(host, runtime);
		}
		else
		{
			RemoveProcess(host);
		}
		if (physicsActive)
		{
			AddOrUpdatePhysics(host, runtime);
		}
		else
		{
			RemovePhysics(host);
		}
	}

	private void AddOrUpdateProcess(StateChart host, StateMachineRuntime runtime)
	{
		if (_processIndices.TryGetValue(host, out var value))
		{
			_processRuntimes[value] = runtime;
			_processCallbacks[value] = runtime.TickProcess;
			return;
		}
		_processIndices[host] = _processHosts.Count;
		_processHosts.Add(host);
		_processRuntimes.Add(runtime);
		_processCallbacks.Add(runtime.TickProcess);
	}

	private void AddOrUpdatePhysics(StateChart host, StateMachineRuntime runtime)
	{
		if (_physicsIndices.TryGetValue(host, out var value))
		{
			_physicsRuntimes[value] = runtime;
			_physicsCallbacks[value] = runtime.TickPhysics;
			return;
		}
		_physicsIndices[host] = _physicsHosts.Count;
		_physicsHosts.Add(host);
		_physicsRuntimes.Add(runtime);
		_physicsCallbacks.Add(runtime.TickPhysics);
	}

	private void RemoveProcess(StateChart host)
	{
		if (_processIndices.TryGetValue(host, out var value))
		{
			RemoveProcessAtSwap(value);
		}
	}

	private void RemovePhysics(StateChart host)
	{
		if (_physicsIndices.TryGetValue(host, out var value))
		{
			RemovePhysicsAtSwap(value);
		}
	}

	private void RemoveProcessAtSwap(int index)
	{
		int num = _processHosts.Count - 1;
		StateChart key = _processHosts[index];
		_processIndices.Remove(key);
		if (index != num)
		{
			StateChart stateChart = _processHosts[num];
			_processHosts[index] = stateChart;
			_processRuntimes[index] = _processRuntimes[num];
			_processCallbacks[index] = _processCallbacks[num];
			_processIndices[stateChart] = index;
		}
		_processHosts.RemoveAt(num);
		_processRuntimes.RemoveAt(num);
		_processCallbacks.RemoveAt(num);
	}

	private void RemovePhysicsAtSwap(int index)
	{
		int num = _physicsHosts.Count - 1;
		StateChart key = _physicsHosts[index];
		_physicsIndices.Remove(key);
		if (index != num)
		{
			StateChart stateChart = _physicsHosts[num];
			_physicsHosts[index] = stateChart;
			_physicsRuntimes[index] = _physicsRuntimes[num];
			_physicsCallbacks[index] = _physicsCallbacks[num];
			_physicsIndices[stateChart] = index;
		}
		_physicsHosts.RemoveAt(num);
		_physicsRuntimes.RemoveAt(num);
		_physicsCallbacks.RemoveAt(num);
	}

	private void FlushPendingUpdates()
	{
		if (_pendingUpdates.Count == 0)
		{
			UpdateGodotProcessing();
			return;
		}
		foreach (KeyValuePair<StateChart, PendingUpdate> pendingUpdate in _pendingUpdates)
		{
			PendingUpdate value = pendingUpdate.Value;
			ApplyUpdate(pendingUpdate.Key, value.Runtime, value.ProcessActive, value.PhysicsActive, value.Remove);
		}
		_pendingUpdates.Clear();
		UpdateGodotProcessing();
	}

	private void UpdateGodotProcessing()
	{
		SetProcess(_processCallbacks.Count > 0);
		SetPhysicsProcess(_physicsCallbacks.Count > 0);
	}
}
