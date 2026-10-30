using System.Collections.Generic;
using Godot;

public partial class ComponentPhysicsBatch : Node
{
	private static bool _mountRequested;

	private readonly List<ComponentBase> _active = new List<ComponentBase>();

	private readonly Dictionary<ComponentBase, int> _activeIndices = new Dictionary<ComponentBase, int>(ReferenceEqualityComparer.Instance);

	public static ComponentPhysicsBatch Instance { get; private set; }

	public static bool Register(ComponentBase component)
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			TryMount(component);
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return false;
		}
		component.PrepareSharedBatchRegistration();
		if (Instance._activeIndices.TryAdd(component, Instance._active.Count))
		{
			Instance._active.Add(component);
		}
		Instance.SetPhysicsProcess(enable: true);
		return true;
	}

	public static void Unregister(ComponentBase component)
	{
		if (GodotObject.IsInstanceValid(Instance) && Instance._activeIndices.TryGetValue(component, out var value))
		{
			Instance.RemoveAt(value);
			if (Instance._active.Count == 0)
			{
				Instance.SetPhysicsProcess(enable: false);
			}
		}
	}

	private static void TryMount(Node source)
	{
		if (!_mountRequested)
		{
			Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(source);
			if (GodotObject.IsInstanceValid(node))
			{
				Instance = new ComponentPhysicsBatch();
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, Instance);
			}
		}
	}

	public override void _Ready()
	{
		Instance = this;
		_mountRequested = false;
		base.ProcessMode = ProcessModeEnum.Always;
		SetPhysicsProcess(_active.Count > 0);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		_mountRequested = false;
		_active.Clear();
		_activeIndices.Clear();
	}

	public override void _PhysicsProcess(double delta)
	{
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool treePaused = GetTree()?.Paused ?? false;
		TowerDefenseProcessModeDispatch.BeginFrame();
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		int num = 0;
		while (num < _active.Count)
		{
			ComponentBase componentBase = _active[num];
			if (!componentBase.IsSharedBatchDispatchActive)
			{
				RemoveAt(num);
				continue;
			}
			if (TowerDefenseProcessModeDispatch.ShouldDispatch(componentBase, treePaused))
			{
				componentBase.SharedBatchPhysicsProcess(delta);
			}
			if (num < _active.Count && _active[num] == componentBase)
			{
				num++;
			}
		}
		if (_active.Count == 0)
		{
			SetPhysicsProcess(enable: false);
		}
		TowerDefensePerfProfiler.End("batch.component", startTicks, _active.Count);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.ComponentPhysics, startBytes);
		TowerDefensePerfProfiler.DumpIfNeeded();
	}

	private void RemoveAt(int index)
	{
		int num = _active.Count - 1;
		ComponentBase key = _active[index];
		if (index != num)
		{
			ComponentBase componentBase = _active[num];
			_active[index] = componentBase;
			_activeIndices[componentBase] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(key);
	}
}
