using Godot;

[GlobalClass]
public partial class SunDropItemHandler : DropItemHandler
{
	public override void OnCollect(Vector2 pos, long value)
	{
		TowerDefenseManager.Instance.AddSun(value);
	}

	public override void OnCollect(DropItemCollectionContext context)
	{
		if (context.UsesLegacyLocalEconomy)
		{
			OnCollect(context.Position, context.Value);
		}
		else
		{
			ApplyAccountValue(context);
		}
	}

	public static bool ApplyAccountValue(DropItemCollectionContext context)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		bool num = instance.ApplyCollectedSunValue(context.AccountId, context.Value);
		if (!num)
		{
			GD.PushWarning($"Sun collection rejected for unregistered account {context.AccountId}.");
		}
		return num;
	}
}
