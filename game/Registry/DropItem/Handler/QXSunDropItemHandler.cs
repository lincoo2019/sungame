using Godot;

[GlobalClass]
public partial class QXSunDropItemHandler : DropItemHandler
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
			SunDropItemHandler.ApplyAccountValue(context);
		}
	}

	public override bool ShouldAutoCollect()
	{
		return TowerDefenseManager.Instance.IsIZMMode();
	}
}
