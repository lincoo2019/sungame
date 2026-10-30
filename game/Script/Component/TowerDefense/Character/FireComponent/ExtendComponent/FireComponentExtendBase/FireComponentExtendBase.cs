using System.Collections.Generic;
using Godot;

public abstract class FireComponentExtendBase : CharacterComponentRuntime
{
	public FireComponentExtendBase preExtend;

	public TowerDefenseCharacter parent;

	private readonly HashSet<FireComponentExtendBase> _chainVisitSet = new HashSet<FireComponentExtendBase>();

	public bool alive
	{
		get
		{
			return base.Alive;
		}
		set
		{
			SetAlive(value);
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		preExtend = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		_chainVisitSet.Clear();
		preExtend = null;
		parent = null;
	}

	protected virtual bool CanRunLocal()
	{
		return true;
	}

	public virtual bool CanRun()
	{
		_chainVisitSet.Clear();
		FireComponentExtendBase fireComponentExtendBase = this;
		while (fireComponentExtendBase != null && !fireComponentExtendBase.IsReleased)
		{
			if (!_chainVisitSet.Add(fireComponentExtendBase) || !fireComponentExtendBase.Alive || !GodotObject.IsInstanceValid(fireComponentExtendBase.parent) || !fireComponentExtendBase.CanRunLocal())
			{
				return false;
			}
			fireComponentExtendBase = fireComponentExtendBase.preExtend;
		}
		return true;
	}
}
