using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseCharacterMotionBatch : Node
{
	private static bool _mountRequested;

	private readonly List<TowerDefenseCharacter> _active = new List<TowerDefenseCharacter>();

	private readonly Dictionary<TowerDefenseCharacter, int> _activeIndices = new Dictionary<TowerDefenseCharacter, int>(ReferenceEqualityComparer.Instance);

	public static TowerDefenseCharacterMotionBatch Instance { get; private set; }

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
		if (Instance._activeIndices.TryAdd(character, Instance._active.Count))
		{
			Instance._active.Add(character);
		}
		Instance.SetProcess(enable: true);
		return true;
	}

	public static void Unregister(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(Instance) && character != null)
		{
			if (Instance._activeIndices.TryGetValue(character, out var value))
			{
				Instance.RemoveAtSwap(value);
			}
			if (Instance._active.Count == 0)
			{
				Instance.SetProcess(enable: false);
			}
		}
	}

	private static void TryMount(TowerDefenseCharacter source)
	{
		if (!_mountRequested || !GodotObject.IsInstanceValid(Instance))
		{
			Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(source);
			if (GodotObject.IsInstanceValid(node))
			{
				TowerDefenseCharacterMotionBatch towerDefenseCharacterMotionBatch = (Instance = new TowerDefenseCharacterMotionBatch());
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, towerDefenseCharacterMotionBatch);
			}
		}
	}

	public override void _Ready()
	{
		Instance = this;
		_mountRequested = false;
		base.ProcessMode = ProcessModeEnum.Always;
		SetProcess(_active.Count > 0);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
			_mountRequested = false;
		}
		_active.Clear();
		_activeIndices.Clear();
	}

	public override void _Process(double delta)
	{
		if (_active.Count == 0)
		{
			SetProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		bool treePaused = GetTree()?.Paused ?? false;
		ulong physicsFrames = Engine.GetPhysicsFrames();
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		int num = 0;
		Node node = null;
		bool flag = false;
		bool flag2 = false;
		int num2 = 0;
		while (num2 < _active.Count)
		{
			TowerDefenseCharacter towerDefenseCharacter = _active[num2];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !towerDefenseCharacter.IsBowlingImpactDisplacementActive)
			{
				RemoveAtSwap(num2);
				continue;
			}
			bool flag3;
			if (towerDefenseCharacter.BatchUsesInheritedProcessMode)
			{
				Node batchProcessModeParent = towerDefenseCharacter.BatchProcessModeParent;
				if (!flag2 || node != batchProcessModeParent)
				{
					node = batchProcessModeParent;
					flag = TowerDefenseProcessModeDispatch.ShouldDispatchInherited(batchProcessModeParent, treePaused);
					flag2 = true;
				}
				flag3 = flag;
			}
			else
			{
				flag3 = TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseCharacter, treePaused);
			}
			if (!flag3)
			{
				num2++;
				continue;
			}
			num++;
			if (!towerDefenseCharacter.UpdateBowlingImpactDisplacement(delta, physicsFrames))
			{
				RemoveAtSwap(num2);
			}
			else
			{
				num2++;
			}
		}
		TowerDefensePerfProfiler.End("batch.characterMotion.bowlingImpact", startTicks, num);
		if (_active.Count == 0)
		{
			SetProcess(enable: false);
		}
	}

	private void RemoveAtSwap(int index)
	{
		int num = _active.Count - 1;
		TowerDefenseCharacter key = _active[index];
		if (index != num)
		{
			TowerDefenseCharacter towerDefenseCharacter = _active[num];
			_active[index] = towerDefenseCharacter;
			_activeIndices[towerDefenseCharacter] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(key);
	}
}
