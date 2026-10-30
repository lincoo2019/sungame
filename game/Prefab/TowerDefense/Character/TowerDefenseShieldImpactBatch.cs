using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseShieldImpactBatch : Node
{
	private struct ShieldImpactEntry
	{
		public TowerDefenseArmorInstance Armor;

		public AdobeAnimateSlot Slot;

		public double RemainingSeconds;
	}

	private const int InitialCapacity = 256;

	private static bool _mountRequested;

	private readonly List<ShieldImpactEntry> _active = new List<ShieldImpactEntry>(256);

	private readonly Dictionary<TowerDefenseArmorInstance, int> _activeIndices = new Dictionary<TowerDefenseArmorInstance, int>(256, ReferenceEqualityComparer.Instance);

	public static TowerDefenseShieldImpactBatch Instance { get; private set; }

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

	public static bool Prepare(TowerDefenseCharacter source)
	{
		if (!GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(Instance))
		{
			TryMount(source);
		}
		return GodotObject.IsInstanceValid(Instance);
	}

	public static bool Schedule(TowerDefenseArmorInstance armor, AdobeAnimateSlot slot, double durationSeconds)
	{
		TowerDefenseCharacter towerDefenseCharacter = armor?.character;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !towerDefenseCharacter.IsInsideTree() || !GodotObject.IsInstanceValid(slot) || !slot.IsInsideTree())
		{
			return false;
		}
		if (!Prepare(towerDefenseCharacter))
		{
			return false;
		}
		durationSeconds = ((durationSeconds > 0.0) ? durationSeconds : 1E-06);
		if (Instance._activeIndices.TryGetValue(armor, out var value))
		{
			ShieldImpactEntry value2 = Instance._active[value];
			value2.Slot = slot;
			value2.RemainingSeconds = durationSeconds;
			Instance._active[value] = value2;
		}
		else
		{
			Instance._activeIndices.Add(armor, Instance._active.Count);
			Instance._active.Add(new ShieldImpactEntry
			{
				Armor = armor,
				Slot = slot,
				RemainingSeconds = durationSeconds
			});
		}
		Instance.SetProcess(enable: true);
		return true;
	}

	private static void TryMount(TowerDefenseCharacter source)
	{
		if (!_mountRequested || !GodotObject.IsInstanceValid(Instance))
		{
			Node node = TowerDefenseProcessModeDispatch.ResolveBatchParent(source);
			if (GodotObject.IsInstanceValid(node))
			{
				TowerDefenseShieldImpactBatch towerDefenseShieldImpactBatch = (Instance = new TowerDefenseShieldImpactBatch());
				_mountRequested = true;
				node.CallDeferred(Node.MethodName.AddChild, towerDefenseShieldImpactBatch);
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
		for (int i = 0; i < _active.Count; i++)
		{
			ResetVisual(_active[i].Slot);
		}
		_active.Clear();
		_activeIndices.Clear();
		if (Instance == this)
		{
			Instance = null;
			_mountRequested = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_active.Count == 0)
		{
			SetProcess(enable: false);
			return;
		}
		SceneTree tree = GetTree();
		if (tree != null && tree.Paused)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		int num = 0;
		double num2 = ((delta > 0.0) ? delta : 0.0);
		int num3 = 0;
		while (num3 < _active.Count)
		{
			ShieldImpactEntry value = _active[num3];
			if (!GodotObject.IsInstanceValid(value.Armor) || !GodotObject.IsInstanceValid(value.Slot) || !value.Slot.IsInsideTree())
			{
				RemoveAtSwap(num3);
				continue;
			}
			num++;
			value.RemainingSeconds -= num2;
			if (value.RemainingSeconds <= 0.0)
			{
				ResetVisual(value.Slot);
				RemoveAtSwap(num3);
			}
			else
			{
				_active[num3] = value;
				num3++;
			}
		}
		TowerDefensePerfProfiler.End("batch.characterFeedback.shieldImpact", startTicks, num);
		if (_active.Count == 0)
		{
			SetProcess(enable: false);
		}
	}

	private static void ResetVisual(AdobeAnimateSlot slot)
	{
		if (GodotObject.IsInstanceValid(slot))
		{
			slot.SetRuntimeVisualOffset(Vector2.Zero);
		}
	}

	private void RemoveAtSwap(int index)
	{
		int num = _active.Count - 1;
		TowerDefenseArmorInstance armor = _active[index].Armor;
		if (index != num)
		{
			ShieldImpactEntry value = _active[num];
			_active[index] = value;
			_activeIndices[value.Armor] = index;
		}
		_active.RemoveAt(num);
		_activeIndices.Remove(armor);
	}
}
