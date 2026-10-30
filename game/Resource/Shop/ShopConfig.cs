using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class ShopConfig : Resource
{
	private Json _data;

	[Export(PropertyHint.None, "")]
	public Array<ShopPageConfig> pageList = new Array<ShopPageConfig>();

	[Export(PropertyHint.None, "")]
	public Json data
	{
		get
		{
			return _data;
		}
		set
		{
			_data = value;
			Init();
			NotifyPropertyListChanged();
		}
	}

	public void Init()
	{
		pageList.Clear();
		if (data == null)
		{
			return;
		}
		foreach (Variant item in data.Data.AsGodotDictionary()["Page"].AsGodotArray())
		{
			ShopPageConfig shopPageConfig = new ShopPageConfig();
			shopPageConfig.Init(item.AsGodotDictionary());
			pageList.Add(shopPageConfig);
		}
	}

	public Array<ShopPageConfig> GetPageTypeList(string type = "Total")
	{
		if (type == "Total")
		{
			return pageList;
		}
		List<ShopItemConfig> list = new List<ShopItemConfig>();
		foreach (ShopPageConfig page in pageList)
		{
			foreach (ShopItemConfig item in page.itemList)
			{
				if (item.type == type)
				{
					list.Add(item);
				}
			}
		}
		Array<ShopPageConfig> array = new Array<ShopPageConfig>();
		ShopPageConfig shopPageConfig = null;
		for (int i = 0; i < list.Count; i++)
		{
			if (i % 8 == 0)
			{
				shopPageConfig = new ShopPageConfig();
				array.Add(shopPageConfig);
			}
			shopPageConfig.itemList.Add(list[i]);
		}
		return array;
	}
}
