using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseFogBatch : Node
{
	private const uint LightCollisionLayerMask = 8u;

	private readonly List<TowerDefenseFog> _active = new List<TowerDefenseFog>();

	private bool _needsEmptyLightUpdate = true;

	public int ActiveCount => _active.Count;

	internal bool NeedsUpdateWhenNoLights => _needsEmptyLightUpdate;

	public void Register(TowerDefenseFog fog)
	{
		if (GodotObject.IsInstanceValid(fog) && !_active.Contains(fog))
		{
			_active.Add(fog);
			_needsEmptyLightUpdate = true;
			fog.SetBatchManaged(managed: true);
			fog.BindBatch(this);
			SetPhysicsProcess(enable: true);
		}
	}

	public void RequestVisibilityUpdate()
	{
		if (_active.Count != 0)
		{
			_needsEmptyLightUpdate = true;
			SetPhysicsProcess(enable: true);
		}
	}

	public override void _Ready()
	{
		SetPhysicsProcess(_active.Count > 0);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_active.Count == 0)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(TowerDefenseManager.GetCharacterNode(), 8u);
		if (worldRectSnapshotsForCollisionLayer.Count == 0 && !_needsEmptyLightUpdate)
		{
			TowerDefensePerfProfiler.End("fog.batch", startTicks, _active.Count);
			return;
		}
		bool flag = true;
		for (int num = _active.Count - 1; num >= 0; num--)
		{
			TowerDefenseFog towerDefenseFog = _active[num];
			if (!GodotObject.IsInstanceValid(towerDefenseFog) || !towerDefenseFog.IsInsideTree())
			{
				_active.RemoveAt(num);
			}
			else
			{
				flag &= towerDefenseFog.BatchPhysicsUpdate(delta, worldRectSnapshotsForCollisionLayer);
			}
		}
		if (worldRectSnapshotsForCollisionLayer.Count == 0)
		{
			_needsEmptyLightUpdate = !flag;
		}
		else
		{
			_needsEmptyLightUpdate = true;
		}
		TowerDefensePerfProfiler.End("fog.batch", startTicks, _active.Count);
		if (_active.Count == 0)
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public override void _ExitTree()
	{
		for (int i = 0; i < _active.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_active[i]))
			{
				_active[i].SetBatchManaged(managed: false);
			}
		}
		_active.Clear();
	}
}
