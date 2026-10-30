using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WeightPickMathine : RefCounted
{
	public static WeightPickItemBase Pick(Array<WeightPickItemBase> items)
	{
		if (items.Count > 0)
		{
			int num = 0;
			foreach (WeightPickItemBase item in items)
			{
				num += (int)item.weight;
			}
			if (num <= 0)
			{
				return null;
			}
			int num2 = (int)(GD.Randi() % (uint)num);
			foreach (WeightPickItemBase item2 in items)
			{
				num2 -= (int)item2.weight;
				if (num2 <= 0)
				{
					return item2;
				}
			}
		}
		return null;
	}

	public static WeightPickItemBase PickF(Array<WeightPickItemBase> items)
	{
		if (items.Count > 0)
		{
			double num = 0.0;
			foreach (WeightPickItemBase item in items)
			{
				num += item.weight;
			}
			if (!(num > 0.0))
			{
				return null;
			}
			double num2 = GD.Randf();
			foreach (WeightPickItemBase item2 in items)
			{
				num2 -= item2.weight;
				if (num2 <= 0.0)
				{
					return item2;
				}
			}
		}
		return null;
	}
}
