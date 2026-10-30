using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShovelCommand : RefCounted
{
	public static string currentShovel = "";

	public static void ListShovels()
	{
		Array shovelList = TowerDefenseManager.GetShovelList();
		string text = _GetCurrentShovel();
		CommandConsole.Instance.PrintLine("[color=cyan]═══════ 铲子列表 ═══════[/color]");
		foreach (Variant item in shovelList)
		{
			string text2 = (string)item;
			ShovelConfig shovel = TowerDefenseManager.GetShovel(text2);
			string value = ((text2 == text) ? "[color=green]►[/color] " : "  ");
			string value2 = ((shovel != null) ? ((string?)TranslationServer.Translate(shovel.name)) : text2);
			CommandConsole.Instance.PrintLine($"{value}{value2} [color=gray]({text2})[/color]");
		}
	}

	public static void ChangeShovel(string shovelName)
	{
		ShovelConfig shovel = TowerDefenseManager.GetShovel(shovelName);
		if (shovel == null)
		{
			CommandConsole.Instance.PrintError("未找到铲子: " + shovelName + "  输入 /shovel list 查看所有铲子");
			return;
		}
		currentShovel = shovelName;
		GameSaveManager.Instance.SetKeyValue("CurrentShovel", shovelName);
		_UpdateShovelManager(shovel);
		CommandConsole.Instance.PrintSuccess("已更换铲子: " + TranslationServer.Translate(shovel.name));
	}

	public static Array GetShovelNames()
	{
		Array array = new Array();
		array.Add("list");
		array.AddRange(TowerDefenseManager.GetShovelList());
		return array;
	}

	public static string _GetCurrentShovel()
	{
		if (currentShovel == "")
		{
			currentShovel = (string)GameSaveManager.Instance.GetKeyValue("CurrentShovel");
			if (string.IsNullOrEmpty(currentShovel))
			{
				currentShovel = "ShovelDefault";
			}
		}
		return currentShovel;
	}

	public static void _UpdateShovelManager(ShovelConfig shovelConfig)
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return;
		}
		TowerDefenseBattleFeatureShovel towerDefenseBattleFeatureShovel = currentControl.GetFeature("Shovel") as TowerDefenseBattleFeatureShovel;
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureShovel) && GodotObject.IsInstanceValid(towerDefenseBattleFeatureShovel.shovelManager))
		{
			towerDefenseBattleFeatureShovel.shovelManager.shovelConfig = shovelConfig;
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureShovel.shovelManager.shovelSprite))
			{
				towerDefenseBattleFeatureShovel.shovelManager.shovelSprite.Texture = shovelConfig.texture;
			}
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureShovel.shovelManager.mapShovelSprite))
			{
				towerDefenseBattleFeatureShovel.shovelManager.mapShovelSprite.Texture = shovelConfig.texture;
			}
		}
	}
}
