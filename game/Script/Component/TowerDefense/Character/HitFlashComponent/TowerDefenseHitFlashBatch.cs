using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseHitFlashBatch : Node
{
	private static bool _mountRequested;

	private readonly List<HitFlashComponent> _active = new List<HitFlashComponent>();

	private readonly Dictionary<HitFlashComponent, int> _activeIndices = new Dictionary<HitFlashComponent, int>(ReferenceEqualityComparer.Instance);

	public static TowerDefenseHitFlashBatch Instance { get; private set; }

	public static int ActiveCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Instance))
			{
				return 0;
			}
			return Instance._active.Count;
		}
	}

	public static bool Register(HitFlashComponent component)
	{
		TowerDefenseCharacter towerDefenseCharacter = component?.parent;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !towerDefenseCharacter.IsInsideTree())
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			TryMount(towerDefenseCharacter);
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return false;
		}
		if (Instance._activeIndices.TryAdd(component, Instance._active.Count))
		{
			Instance._active.Add(component);
		}
		Instance.SetProcess(enable: true);
		return true;
	}

	public static void Unregister(HitFlashComponent component)
	{
		if (GodotObject.IsInstanceValid(Instance) && component != null)
		{
			if (Instance._activeIndices.TryGetValue(component, out var value))
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
				TowerDefenseHitFlashBatch towerDefenseHitFlashBatch = (Instance = new TowerDefenseHitFlashBatch());
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, towerDefenseHitFlashBatch);
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
		TowerDefenseProcessModeDispatch.BeginDispatchPass();
		int num = 0;
		int num2 = 0;
		while (num2 < _active.Count)
		{
			HitFlashComponent hitFlashComponent = _active[num2];
			TowerDefenseCharacter towerDefenseCharacter = hitFlashComponent?.parent;
			if (hitFlashComponent == null || !hitFlashComponent.IsBatchUpdateActive || !GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				RemoveAtSwap(num2);
				continue;
			}
			if (!(towerDefenseCharacter.BatchUsesInheritedProcessMode ? TowerDefenseProcessModeDispatch.ShouldDispatchInherited(towerDefenseCharacter.BatchProcessModeParent, treePaused) : TowerDefenseProcessModeDispatch.ShouldDispatch(towerDefenseCharacter, treePaused)))
			{
				num2++;
				continue;
			}
			num++;
			if (!hitFlashComponent.UpdateBatch(delta))
			{
				RemoveAtSwap(num2);
			}
			else
			{
				num2++;
			}
		}
		TowerDefensePerfProfiler.End("batch.characterFeedback.hitFlash", startTicks, num);
		if (_active.Count == 0)
		{
			SetProcess(enable: false);
		}
	}

	private void RemoveAtSwap(int index)
	{
		int num = _active.Count - 1;
		HitFlashComponent key = _active[index];
		if (index != num)
		{
			HitFlashComponent hitFlashComponent = _active[num];
			_active[index] = hitFlashComponent;
			_activeIndices[hitFlashComponent] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(key);
	}
}
