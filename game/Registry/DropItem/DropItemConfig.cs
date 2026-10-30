using Godot;

[GlobalClass]
public partial class DropItemConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public ObjectManagerConfig.OBJECT Id;

	[Export(PropertyHint.None, "")]
	public StringName Name = "";

	[Export(PropertyHint.None, "")]
	public PackedScene Scene;

	[Export(PropertyHint.None, "")]
	public int PoolMaxNum = 100;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.DROP_ITEM_CATEGORY Category = TowerDefenseEnum.DROP_ITEM_CATEGORY.NOONE;

	[Export(PropertyHint.None, "")]
	public int Value;

	[Export(PropertyHint.None, "")]
	public string FallAudio = "CoinFall";

	[Export(PropertyHint.None, "")]
	public string PickAudio = "CoinPick";

	[Export(PropertyHint.None, "")]
	public int CoinObjectId = -1;

	[Export(PropertyHint.None, "")]
	public DropItemHandler Handler;
}
