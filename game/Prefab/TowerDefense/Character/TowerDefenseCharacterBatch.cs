using System;
using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseCharacterBatch : Node
{
	private static bool _mountRequested;

	private readonly List<TowerDefenseCharacter> _active = new List<TowerDefenseCharacter>();

	private readonly Dictionary<TowerDefenseCharacter, int> _activeIndices = new Dictionary<TowerDefenseCharacter, int>(ReferenceEqualityComparer.Instance);

	private readonly List<TowerDefenseCharacter> _processActive = new List<TowerDefenseCharacter>();

	private readonly Dictionary<TowerDefenseCharacter, int> _processActiveIndices = new Dictionary<TowerDefenseCharacter, int>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<Type, string> _physicsMetricNames = new Dictionary<Type, string>();

	public static TowerDefenseCharacterBatch Instance { get; private set; }

	public static bool Register(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !character.IsInsideTree())
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			TryMount(character);
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return false;
		}
		character.PrepareBatchRegistration();
		if (Instance._activeIndices.TryAdd(character, Instance._active.Count))
		{
			Instance._active.Add(character);
		}
		Instance.UpdateProcessMembership(character);
		character.SetOwnerBatchRegistration(registered: true);
		Instance.SetProcess(Instance._processActive.Count > 0);
		Instance.SetPhysicsProcess(enable: true);
		return true;
	}

	public static void UpdateProcessDispatch(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(Instance) && Instance._activeIndices.ContainsKey(character))
		{
			Instance.UpdateProcessMembership(character);
			Instance.SetProcess(Instance._processActive.Count > 0);
		}
	}

	public static void Unregister(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(Instance))
		{
			character?.SetOwnerBatchRegistration(registered: false);
			return;
		}
		if (Instance._activeIndices.TryGetValue(character, out var value))
		{
			Instance.RemoveAtSwap(value);
		}
		character?.SetOwnerBatchRegistration(registered: false);
		Instance.SetProcess(Instance._processActive.Count > 0);
		if (Instance._active.Count == 0)
		{
			Instance.SetPhysicsProcess(enable: false);
		}
	}

	private static void TryMount(TowerDefenseCharacter source)
	{
		if (!_mountRequested || !GodotObject.IsInstanceValid(Instance))
		{
			Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(source);
			if (GodotObject.IsInstanceValid(node))
			{
				TowerDefenseCharacterBatch towerDefenseCharacterBatch = (Instance = new TowerDefenseCharacterBatch());
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, towerDefenseCharacterBatch);
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
			TowerDefenseCharacter towerDefenseCharacter = _processActive[num2];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !towerDefenseCharacter.IsOwnerBatchDispatchActive || !towerDefenseCharacter.WantsMainStateMachineProcessDispatch)
			{
				RemoveProcessAtSwap(num2);
				continue;
			}
			if (!(towerDefenseCharacter.BatchUsesInheritedProcessMode ? TowerDefenseProcessModeDispatch.ShouldDispatchInherited(towerDefenseCharacter.BatchProcessModeParent, treePaused) : TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseCharacter, treePaused)))
			{
				num2++;
				continue;
			}
			towerDefenseCharacter.BatchProcessUpdate(delta);
			num++;
			if (num2 < _processActive.Count && _processActive[num2] == towerDefenseCharacter)
			{
				num2++;
			}
		}
		TowerDefensePerfProfiler.End("batch.characterProcess", startTicks, num);
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
			TowerDefenseProcessModeDispatch.BeginDispatchPass();
			bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
			int num = 0;
			int num2 = 0;
			while (num2 < _active.Count)
			{
				TowerDefenseCharacter towerDefenseCharacter = _active[num2];
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !towerDefenseCharacter.IsOwnerBatchDispatchActive)
				{
					RemoveAtSwap(num2);
					continue;
				}
				if (!(towerDefenseCharacter.BatchUsesInheritedProcessMode ? TowerDefenseProcessModeDispatch.ShouldDispatchInherited(towerDefenseCharacter.BatchProcessModeParent, treePaused) : TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseCharacter, treePaused)))
				{
					num2++;
					continue;
				}
				if (flag)
				{
					long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
					towerDefenseCharacter.BatchUpdate(delta);
					TowerDefensePerfProfiler.End(GetPhysicsMetricName(towerDefenseCharacter), startTicks2);
				}
				else
				{
					towerDefenseCharacter.BatchUpdate(delta);
				}
				num++;
				if (num2 < _active.Count && _active[num2] == towerDefenseCharacter)
				{
					num2++;
				}
			}
			TowerDefensePerfProfiler.End("batch.character", startTicks, num);
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

	private string GetPhysicsMetricName(TowerDefenseCharacter character)
	{
		Type type = character.GetType();
		if (_physicsMetricNames.TryGetValue(type, out var value))
		{
			return value;
		}
		value = "batch.character.type." + type.Name;
		_physicsMetricNames.Add(type, value);
		return value;
	}

	private void RemoveAtSwap(int index)
	{
		int num = _active.Count - 1;
		TowerDefenseCharacter towerDefenseCharacter = _active[index];
		RemoveProcess(towerDefenseCharacter);
		if (index != num)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = _active[num];
			_active[index] = towerDefenseCharacter2;
			_activeIndices[towerDefenseCharacter2] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(towerDefenseCharacter);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter.SetOwnerBatchRegistration(registered: false);
		}
	}

	private void UpdateProcessMembership(TowerDefenseCharacter character)
	{
		if (character.WantsMainStateMachineProcessDispatch)
		{
			if (_processActiveIndices.TryAdd(character, _processActive.Count))
			{
				_processActive.Add(character);
			}
		}
		else
		{
			RemoveProcess(character);
		}
	}

	private void RemoveProcess(TowerDefenseCharacter character)
	{
		if (_processActiveIndices.TryGetValue(character, out var value))
		{
			RemoveProcessAtSwap(value);
		}
	}

	private void RemoveProcessAtSwap(int index)
	{
		int num = _processActive.Count - 1;
		TowerDefenseCharacter key = _processActive[index];
		if (index != num)
		{
			TowerDefenseCharacter towerDefenseCharacter = _processActive[num];
			_processActive[index] = towerDefenseCharacter;
			_processActiveIndices[towerDefenseCharacter] = index;
		}
		_processActive.RemoveAt(num);
		_processActiveIndices.Remove(key);
	}
}
