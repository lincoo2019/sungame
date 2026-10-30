using System;
using Godot;

public readonly struct TowerDefenseEffectSpriteOnceGpuHandle(TowerDefenseEffectSpriteOnceBatcher owner, int index, uint generation) : IEquatable<TowerDefenseEffectSpriteOnceGpuHandle>
{
	internal TowerDefenseEffectSpriteOnceBatcher Owner { get; } = owner;

	internal int Index { get; } = index;

	internal uint Generation { get; } = generation;

	public bool IsValid
	{
		get
		{
			if (GodotObject.IsInstanceValid(Owner))
			{
				return Owner.IsNodeFreeHandleValid(this);
			}
			return false;
		}
	}

	public bool SetTransform(Transform2D transform)
	{
		if (GodotObject.IsInstanceValid(Owner))
		{
			return Owner.SetNodeFreeTransform(this, transform);
		}
		return false;
	}

	public bool SetZIndex(int zIndex)
	{
		if (GodotObject.IsInstanceValid(Owner))
		{
			return Owner.SetNodeFreeZIndex(this, zIndex);
		}
		return false;
	}

	public bool Remove()
	{
		if (GodotObject.IsInstanceValid(Owner))
		{
			return Owner.RemoveNodeFree(this);
		}
		return false;
	}

	public bool Equals(TowerDefenseEffectSpriteOnceGpuHandle other)
	{
		if (Owner == other.Owner && Index == other.Index)
		{
			return Generation == other.Generation;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is TowerDefenseEffectSpriteOnceGpuHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Owner, Index, Generation);
	}

	public static bool operator ==(TowerDefenseEffectSpriteOnceGpuHandle left, TowerDefenseEffectSpriteOnceGpuHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TowerDefenseEffectSpriteOnceGpuHandle left, TowerDefenseEffectSpriteOnceGpuHandle right)
	{
		return !left.Equals(right);
	}
}
