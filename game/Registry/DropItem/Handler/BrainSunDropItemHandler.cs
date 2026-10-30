using Godot;

[GlobalClass]
public partial class BrainSunDropItemHandler : DropItemHandler
{
	public override long GetCollectValue(long baseValue)
	{
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			return baseValue;
		}
		return -baseValue;
	}

	public override void OnCollect(Vector2 pos, long value)
	{
		if (value > 0)
		{
			TowerDefenseManager.Instance.AddSun(value);
		}
		else
		{
			TowerDefenseManager.Instance.UseSun(-value);
		}
	}

	public override void OnCollect(DropItemCollectionContext context)
	{
		if (context.UsesLegacyLocalEconomy)
		{
			OnCollect(context.Position, context.Value);
		}
		else
		{
			SunDropItemHandler.ApplyAccountValue(context);
		}
	}
}
