using Godot;

[GlobalClass]
public partial class DropItemHandler : Resource
{
	public virtual void OnCollect(Vector2 pos, long value)
	{
	}

	public virtual void OnCollect(DropItemCollectionContext context)
	{
		if (context.UsesLegacyLocalEconomy)
		{
			OnCollect(context.Position, context.Value);
		}
		else
		{
			GD.PushWarning("Drop handler " + GetType().Name + " rejected account-owned collection because it only implements the legacy API.");
		}
	}

	public virtual void OnSpawn(Vector2 pos)
	{
	}

	public virtual void OnDestroy()
	{
	}

	public virtual long GetCollectValue(long baseValue)
	{
		return baseValue;
	}

	public virtual long GetCollectValue(DropItemCollectionContext context)
	{
		return GetCollectValue(context.Value);
	}

	public virtual bool ShouldAutoCollect()
	{
		return false;
	}

	public virtual bool ShouldAutoCollect(DropItemCollectionContext context)
	{
		return ShouldAutoCollect();
	}

	public virtual void Reset()
	{
	}
}
