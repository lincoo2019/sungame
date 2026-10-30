using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShopPageConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<ShopItemConfig> itemList = new Array<ShopItemConfig>();

	public void Init(Dictionary data)
	{
		foreach (Variant item in data["Item"].AsGodotArray())
		{
			ShopItemConfig shopItemConfig = new ShopItemConfig();
			shopItemConfig.Init(item.AsGodotDictionary());
			itemList.Add(shopItemConfig);
		}
	}
}
