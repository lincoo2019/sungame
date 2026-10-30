using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShopItemConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string type = "";

	[Export(PropertyHint.None, "")]
	public Array<ShopItemStageConfig> stageList = new Array<ShopItemStageConfig>();

	public void Init(Dictionary data)
	{
		type = data.GetValueOrDefault("Type", "Item").AsString();
		foreach (Variant item in data.GetValueOrDefault("Stage", new Array()).AsGodotArray())
		{
			ShopItemStageConfig shopItemStageConfig = new ShopItemStageConfig();
			shopItemStageConfig.Init(item.AsGodotDictionary());
			stageList.Add(shopItemStageConfig);
		}
	}
}
