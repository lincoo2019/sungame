using Godot;
using Godot.Collections;

[GlobalClass]
public partial class FallingObjectConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<FallingObjectWeightItemConfig> weightItem = new Array<FallingObjectWeightItemConfig>();

	public ObjectManagerConfig.OBJECT Pick()
	{
		Array<WeightPickItemBase> array = new Array<WeightPickItemBase>();
		foreach (FallingObjectWeightItemConfig item in weightItem)
		{
			array.Add(new WeightPickItemBase((int)item.item, item.weight, item.empty));
		}
		return (ObjectManagerConfig.OBJECT)WeightPickMathine.Pick(array).item.AsInt32();
	}
}
