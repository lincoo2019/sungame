using Godot;

[GlobalClass]
public partial class FallingObjectWeightItemConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool empty;

	[Export(PropertyHint.None, "")]
	public ObjectManagerConfig.OBJECT item;

	[Export(PropertyHint.None, "")]
	public int weight = 100;
}
