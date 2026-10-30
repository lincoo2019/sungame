using System.Collections.Generic;
using Godot;

public partial class StateChartStatePhysicsBatch : Node
{
	private readonly List<StateChartState> _active = new List<StateChartState>();

	private readonly List<StateChartState.StatePhysicsProcessingEventHandler> _activeCallbacks = new List<StateChartState.StatePhysicsProcessingEventHandler>();

	private readonly Dictionary<StateChartState, int> _activeIndices = new Dictionary<StateChartState, int>();

	private readonly HashSet<StateChartState> _toAdd = new HashSet<StateChartState>();

	private readonly HashSet<StateChartState> _toRemove = new HashSet<StateChartState>();

	private readonly HashSet<StateChartState> _registered = new HashSet<StateChartState>();

	public static StateChartStatePhysicsBatch Instance { get; private set; }

	public static bool Register(StateChartState state)
	{
		if (Instance == null || !GodotObject.IsInstanceValid(Instance))
		{
			TryMount(state);
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return false;
		}
		StateChartStatePhysicsBatch instance = Instance;
		instance._toRemove.Remove(state);
		if (instance._registered.Contains(state))
		{
			return true;
		}
		instance._toAdd.Add(state);
		instance._registered.Add(state);
		instance.SetPhysicsProcess(enable: true);
		return true;
	}

	public static void Unregister(StateChartState state)
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			StateChartStatePhysicsBatch instance = Instance;
			if (instance._toAdd.Remove(state))
			{
				instance._registered.Remove(state);
			}
			else if (instance._registered.Contains(state))
			{
				instance._toRemove.Add(state);
				instance.SetPhysicsProcess(enable: true);
			}
		}
	}

	internal static void UpdateCallback(StateChartState state, StateChartState.StatePhysicsProcessingEventHandler callback)
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			StateChartStatePhysicsBatch instance = Instance;
			if (instance._activeIndices.TryGetValue(state, out var value))
			{
				instance._activeCallbacks[value] = callback;
			}
		}
	}

	private static void TryMount(StateChartState state)
	{
		if (!GodotObject.IsInstanceValid(state))
		{
			return;
		}
		SceneTree tree = state.GetTree();
		if (GodotObject.IsInstanceValid(tree))
		{
			Node node = tree.CurrentScene;
			if (!GodotObject.IsInstanceValid(node))
			{
				node = tree.Root;
			}
			if (GodotObject.IsInstanceValid(node))
			{
				StateChartStatePhysicsBatch stateChartStatePhysicsBatch = (Instance = new StateChartStatePhysicsBatch());
				node.CallDeferred("add_child", stateChartStatePhysicsBatch);
			}
		}
	}

	public override void _Ready()
	{
		Instance = this;
		SetPhysicsProcess(enable: true);
		if (!HasWork())
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		_active.Clear();
		_activeCallbacks.Clear();
		_activeIndices.Clear();
		_toAdd.Clear();
		_toRemove.Clear();
		_registered.Clear();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!HasWork())
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		if (_toRemove.Count > 0)
		{
			foreach (StateChartState item in _toRemove)
			{
				RemoveActive(item);
				_registered.Remove(item);
			}
			_toRemove.Clear();
		}
		if (_toAdd.Count > 0)
		{
			foreach (StateChartState item2 in _toAdd)
			{
				if (!GodotObject.IsInstanceValid(item2))
				{
					_registered.Remove(item2);
				}
				else
				{
					AddActive(item2);
				}
			}
			_toAdd.Clear();
		}
		int num = 0;
		bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
		int num2 = 0;
		while (num2 < _active.Count)
		{
			StateChartState stateChartState = _active[num2];
			if (!stateChartState.IsPhysicsBatchDispatchActive)
			{
				_registered.Remove(stateChartState);
				RemoveActiveAtSwap(num2);
				continue;
			}
			StateChartState.StatePhysicsProcessingEventHandler statePhysicsProcessingEventHandler = _activeCallbacks[num2];
			if (statePhysicsProcessingEventHandler != null)
			{
				if (flag)
				{
					stateChartState.BatchPhysicsProcessValidated(delta);
				}
				else
				{
					statePhysicsProcessingEventHandler(delta);
				}
			}
			num++;
			num2++;
		}
		TowerDefensePerfProfiler.End("batch.statePhysics", startTicks, num);
		TowerDefensePerfProfiler.DumpIfNeeded();
		if (!HasWork())
		{
			SetPhysicsProcess(enable: false);
		}
	}

	private bool HasWork()
	{
		if (_active.Count <= 0 && _toAdd.Count <= 0)
		{
			return _toRemove.Count > 0;
		}
		return true;
	}

	private void AddActive(StateChartState state)
	{
		if (GodotObject.IsInstanceValid(state) && !_activeIndices.ContainsKey(state))
		{
			_activeIndices[state] = _active.Count;
			_active.Add(state);
			_activeCallbacks.Add(state.PhysicsBatchCallback);
		}
	}

	private void RemoveActive(StateChartState state)
	{
		if (_activeIndices.TryGetValue(state, out var value))
		{
			RemoveActiveAtSwap(value);
		}
	}

	private void RemoveActiveAtSwap(int index)
	{
		int num = _active.Count - 1;
		StateChartState key = _active[index];
		_activeIndices.Remove(key);
		if (index != num)
		{
			StateChartState stateChartState = _active[num];
			StateChartState.StatePhysicsProcessingEventHandler value = _activeCallbacks[num];
			_active[index] = stateChartState;
			_activeCallbacks[index] = value;
			_activeIndices[stateChartState] = index;
		}
		_active.RemoveAt(num);
		_activeCallbacks.RemoveAt(num);
	}
}
