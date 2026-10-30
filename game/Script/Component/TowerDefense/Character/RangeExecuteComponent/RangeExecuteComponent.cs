using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class RangeExecuteComponent : CharacterComponentRuntime
{
	public TowerDefenseEnum.RANGE_TYPE rangeType = TowerDefenseEnum.RANGE_TYPE.AREA;

	public AabbShape2DResource executeShape;

	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool filterGravestones = true;

	public bool filterVases = true;

	public int maxTargets = -1;

	public Vector2 eventOffset = Vector2.Zero;

	public TowerDefenseCharacter parent;

	private readonly List<TowerDefenseCharacter> _targetBuffer = new List<TowerDefenseCharacter>(16);

	private bool _configured;

	private RangeExecuteComponentDefinition Definition => base.ComponentDefinition as RangeExecuteComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_targetBuffer.Clear();
		parent = null;
	}

	protected override void OnReleased()
	{
		_targetBuffer.Clear();
		eventList.Clear();
		executeShape = null;
		parent = null;
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		RangeExecuteComponentDefinition definition = Definition;
		if (definition == null)
		{
			return;
		}
		rangeType = definition.rangeType;
		executeShape = definition.executeShape;
		filterGravestones = definition.filterGravestones;
		filterVases = definition.filterVases;
		maxTargets = definition.maxTargets;
		eventOffset = definition.eventOffset;
		eventList.Clear();
		if (definition.eventList != null)
		{
			for (int i = 0; i < definition.eventList.Count; i++)
			{
				eventList.Add(definition.eventList[i]);
			}
		}
		_configured = true;
	}

	public bool CanAttack(AabbShape2DResource checkShape)
	{
		if (!TryGetRuntime(out var manager) || !TryGetWorldRect(checkShape, out var rect))
		{
			return false;
		}
		return manager.GetCharacterTargetFromRectList(parent, rect, checkLine: false, filterGravestones, filterVases).Count > 0;
	}

	public bool CanExecute()
	{
		if (!TryGetRuntime(out var _))
		{
			return false;
		}
		Rect2 rect;
		if (rangeType != TowerDefenseEnum.RANGE_TYPE.ROW)
		{
			return TryGetWorldRect(executeShape, out rect);
		}
		return true;
	}

	public void Execute()
	{
		if (!CanExecute() || !TryGetRuntime(out var manager))
		{
			return;
		}
		_targetBuffer.Clear();
		switch (rangeType)
		{
		default:
			return;
		case TowerDefenseEnum.RANGE_TYPE.AREA:
		{
			if (!TryGetWorldRect(executeShape, out var rect2))
			{
				return;
			}
			_targetBuffer.AddRange(manager.GetCharacterTargetFromRectList(parent, rect2, checkLine: false, filterGravestones, filterVases));
			break;
		}
		case TowerDefenseEnum.RANGE_TYPE.ROW:
			_targetBuffer.AddRange(manager.GetCharacterTargetLineList(parent, filterGravestones));
			break;
		case TowerDefenseEnum.RANGE_TYPE.ENEMY:
		{
			if (!TryGetWorldRect(executeShape, out var rect))
			{
				return;
			}
			TowerDefenseCharacter characterTargetNearestFromRect = manager.GetCharacterTargetNearestFromRect(parent, rect, TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, filterGravestones);
			if (GodotObject.IsInstanceValid(characterTargetNearestFromRect))
			{
				_targetBuffer.Add(characterTargetNearestFromRect);
			}
			break;
		}
		}
		int num = ((maxTargets < 0) ? _targetBuffer.Count : Mathf.Min(maxTargets, _targetBuffer.Count));
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _targetBuffer[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter.die || towerDefenseCharacter.nearDie || (filterVases && towerDefenseCharacter is TowerDefenseVase))
			{
				continue;
			}
			for (int j = 0; j < eventList.Count; j++)
			{
				TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = eventList[j];
				if (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase))
				{
					towerDefenseCharacterEventBase.Execute(eventOffset, towerDefenseCharacter);
				}
			}
		}
	}

	private bool TryGetRuntime(out TowerDefenseManager manager)
	{
		manager = TowerDefenseManager.Instance;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && !parent.die && !parent.nearDie && GodotObject.IsInstanceValid(manager))
		{
			return eventList.Count > 0;
		}
		return false;
	}

	private bool TryGetWorldRect(AabbShape2DResource shape, out Rect2 rect)
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(shape))
		{
			rect = default(Rect2);
			return false;
		}
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		return shape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery), out rect);
	}
}
