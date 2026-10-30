using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RandomTransformationComponentPacketBankConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetBankName { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public bool categoryUseAll { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<string> categoryUseList { get; set; } = new Array<string>();

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float rand { get; set; } = 1f;

	public bool catagoryUseAll
	{
		get
		{
			return categoryUseAll;
		}
		set
		{
			categoryUseAll = value;
		}
	}

	public Array<string> catagoryUseList
	{
		get
		{
			return categoryUseList;
		}
		set
		{
			categoryUseList = value ?? new Array<string>();
		}
	}

	public void FillPacketList(ICollection<string> output)
	{
		if (output == null || string.IsNullOrEmpty(packetBankName) || GD.Randf() > Mathf.Clamp(rand, 0f, 1f))
		{
			return;
		}
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(packetBankName);
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			return;
		}
		if (categoryUseAll)
		{
			AppendValues(packetBankData.GetPacketList(), output);
		}
		else if (categoryUseList != null)
		{
			for (int i = 0; i < categoryUseList.Count; i++)
			{
				AppendValues(packetBankData.GetCategory(categoryUseList[i]), output);
			}
		}
	}

	private static void AppendValues(Array values, ICollection<string> output)
	{
		if (values == null)
		{
			return;
		}
		foreach (Variant value in values)
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				output.Add(text);
			}
		}
	}

	public Array GetPacketList()
	{
		Array array = new Array();
		List<string> list = new List<string>();
		FillPacketList(list);
		for (int i = 0; i < list.Count; i++)
		{
			array.Add(list[i]);
		}
		return array;
	}
}
