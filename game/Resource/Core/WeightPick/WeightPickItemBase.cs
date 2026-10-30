using Godot;

[GlobalClass]
public partial class WeightPickItemBase : Resource
{
	public bool empty;

	public Variant item;

	public double weight = 100.0;

	public WeightPickItemBase()
	{
	}

	public WeightPickItemBase(Variant _item, double _weight = 100.0, bool _empty = false)
	{
		item = _item;
		weight = _weight;
		empty = _empty;
	}
}
