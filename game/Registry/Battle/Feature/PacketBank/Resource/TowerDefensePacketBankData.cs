using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefensePacketBankData : Resource
{
	[Export(PropertyHint.None, "")]
	public Dictionary category = new Dictionary();

	public Array<string> GetUnlockPacket()
	{
		Array<string> array = new Array<string>();
		foreach (Variant value in category.Values)
		{
			foreach (Variant item in (Array)value)
			{
				string text = (string)item;
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
				if (packetConfig != null && packetConfig.Unlock())
				{
					array.Add(text);
				}
			}
		}
		return array;
	}

	public Array GetCategory(string _category)
	{
		if (category.ContainsKey(_category))
		{
			return ((Array)category[_category]).Duplicate();
		}
		return new Array();
	}

	public Array GetPacketList()
	{
		Array array = new Array();
		foreach (Variant key in category.Keys)
		{
			string text = (string)key;
			array.AddRange((Array)category[text]);
		}
		return array;
	}

	public Array GetPlantList()
	{
		Array array = new Array();
		foreach (Variant key in category.Keys)
		{
			string text = (string)key;
			switch (text)
			{
			case "White":
			case "Gold":
			case "Diamond":
			case "Colour":
			case "Star":
			case "Original":
				array.AddRange((Array)category[text]);
				break;
			}
		}
		array.Remove("PlantLuckyBlover");
		return array;
	}

	public Array GetZombieList()
	{
		Array array = new Array();
		foreach (Variant key in category.Keys)
		{
			string text = (string)key;
			if (text == "Zombie")
			{
				array.AddRange((Array)category[text]);
			}
		}
		return array;
	}
}
