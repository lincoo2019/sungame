using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseArmorRegistry : RefCounted
{
	private static Json _registryJson;

	public static bool IsInit = false;

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseArmorTypeData> ArmorDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseArmorTypeData>();

	private static Json RegistryJson => _registryJson ?? (_registryJson = GD.Load<Json>("res://Registry/Armor/ArmorRegistry.json"));

	public static void Init()
	{
		if (!IsInit)
		{
			IsInit = true;
			RegisterInit();
		}
	}

	public static void RegisterInit()
	{
		Dictionary dictionary = (Dictionary)RegistryJson.Data;
		Dictionary dictionary2 = (dictionary.ContainsKey("Armors") ? ((Dictionary)dictionary["Armors"]) : new Dictionary());
		foreach (Variant key in dictionary2.Keys)
		{
			string name = (string)key;
			TowerDefenseArmorTypeData towerDefenseArmorTypeData = GD.Load<TowerDefenseArmorTypeData>((string)dictionary2[key]);
			if (towerDefenseArmorTypeData != null)
			{
				RegisterArmor(new StringName(name), towerDefenseArmorTypeData);
			}
		}
	}

	public static void RegisterArmor(StringName armorName, TowerDefenseArmorTypeData armorTypeData)
	{
		ArmorDictionary[armorName] = armorTypeData;
	}

	public static TowerDefenseArmorTypeData GetArmorType(StringName armorName)
	{
		if (!ArmorDictionary.ContainsKey(armorName))
		{
			return null;
		}
		return ArmorDictionary[armorName].Duplicate() as TowerDefenseArmorTypeData;
	}

	public static bool HasArmor(StringName armorName)
	{
		return ArmorDictionary.ContainsKey(armorName);
	}

	public static Array<StringName> GetArmorNames()
	{
		Array<StringName> array = new Array<StringName>();
		foreach (StringName key in ArmorDictionary.Keys)
		{
			array.Add(key);
		}
		return array;
	}
}
