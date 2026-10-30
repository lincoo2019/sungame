using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileSkinData : RefCounted
{
	public List<StringName> SkinList = new List<StringName>();

	public Dictionary<StringName, PackedScene> SkinProjectileSceneDictionary = new Dictionary<StringName, PackedScene>();

	public Dictionary<StringName, PackedScene> SkinSplatSceneDictionary = new Dictionary<StringName, PackedScene>();

	public void AddSkin(StringName skinName, PackedScene projectileScene, PackedScene splatScene)
	{
		SkinProjectileSceneDictionary[skinName] = projectileScene;
		SkinSplatSceneDictionary[skinName] = splatScene;
		SkinList.Add(skinName);
	}

	public PackedScene GetSkinProjectileScene(StringName skinName)
	{
		if (!HasSkin(skinName))
		{
			return null;
		}
		return SkinProjectileSceneDictionary[skinName];
	}

	public PackedScene GetSkinSplatScene(StringName skinName)
	{
		if (!HasSkin(skinName))
		{
			return null;
		}
		return SkinSplatSceneDictionary[skinName];
	}

	public bool HasSkin(StringName skinName)
	{
		return SkinList.Contains(skinName);
	}

	public bool IsEmpty()
	{
		return SkinList.Count > 0;
	}
}
