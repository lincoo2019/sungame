using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileRegistry : RefCounted
{
	internal readonly record struct ProjectileRegistrationSnapshot(bool HasData, TowerDefenseProjectileData Data, bool HasSkin, TowerDefenseProjectileSkinData Skin);

	private static Json _registryJson;

	public static bool IsInit = false;

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileData> ProjectileDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileData>();

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileSkinData> ProjectileSkinDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileSkinData>();

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileChangeData> ProjectileChangeDataDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseProjectileChangeData>();

	private static long _registrationRevision;

	public static Json REGISTRY_JSON => _registryJson ?? (_registryJson = GD.Load<Json>("res://Registry/Projectile/ProjectileRegistry.json"));

	internal static long RegistrationRevision => Interlocked.Read(in _registrationRevision);

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
		Dictionary dictionary = (Dictionary)REGISTRY_JSON.Data;
		Dictionary dictionary2 = (Dictionary)dictionary.GetValueOrDefault("Projectiles", new Dictionary());
		foreach (Variant key in dictionary2.Keys)
		{
			string text = (string)key;
			TowerDefenseProjectileData towerDefenseProjectileData = SafeLoad<TowerDefenseProjectileData>((string)dictionary2[text], "Projectile:" + text);
			if (towerDefenseProjectileData != null)
			{
				RegisterProjectile(text, towerDefenseProjectileData);
			}
		}
		Dictionary dictionary3 = (Dictionary)dictionary.GetValueOrDefault("Skins", new Dictionary());
		foreach (Variant key2 in dictionary3.Keys)
		{
			string text2 = (string)key2;
			Dictionary dictionary4 = (Dictionary)dictionary3[text2];
			foreach (Variant key3 in dictionary4.Keys)
			{
				string text3 = (string)key3;
				Dictionary dictionary5 = (Dictionary)dictionary4[text3];
				PackedScene skinProjectileScene = null;
				PackedScene skinSplatScene = null;
				if (dictionary5.ContainsKey("SplatScene"))
				{
					skinSplatScene = SafeLoad<PackedScene>((string)dictionary5["SplatScene"], "Splat:" + text2);
				}
				if (dictionary5.ContainsKey("ProjectileScene"))
				{
					skinProjectileScene = SafeLoad<PackedScene>((string)dictionary5["ProjectileScene"], "Skin:" + text2 + "/" + text3);
				}
				RegisterProjectileSkin(text2, text3, skinProjectileScene, skinSplatScene);
			}
		}
		Dictionary dictionary6 = (Dictionary)dictionary.GetValueOrDefault("Changes", new Dictionary());
		foreach (Variant key4 in dictionary6.Keys)
		{
			string text4 = (string)key4;
			Dictionary dictionary7 = (Dictionary)dictionary6[text4];
			foreach (Variant key5 in dictionary7.Keys)
			{
				string text5 = (string)key5;
				RegisterProjectileChange(text4, text5, (string)dictionary7[text5]);
			}
		}
	}

	public static void RegisterProjectile(StringName projectileName, TowerDefenseProjectileData projectileData)
	{
		ProjectileDictionary[projectileName] = projectileData;
		ProjectileSkinDictionary[projectileName] = new TowerDefenseProjectileSkinData();
		RegisterProjectileSkin(projectileName, "Default", projectileData.projectileScene, projectileData.splatScene);
	}

	internal static void RegisterProjectileOverlay(StringName projectileName, TowerDefenseProjectileData projectileData)
	{
		ProjectileSkinDictionary.TryGetValue(projectileName, out var value);
		RegisterProjectile(projectileName, projectileData);
		if (value == null)
		{
			return;
		}
		foreach (StringName skin in value.SkinList)
		{
			if (!(skin.ToString() == "Default") && !HasProjectileSkin(projectileName, skin))
			{
				RegisterProjectileSkin(projectileName, skin, value.GetSkinProjectileScene(skin), value.GetSkinSplatScene(skin));
			}
		}
	}

	internal static void NotifyProjectileConfigChanged()
	{
		Interlocked.Increment(ref _registrationRevision);
	}

	internal static ProjectileRegistrationSnapshot CaptureProjectileRegistration(StringName projectileName)
	{
		TowerDefenseProjectileData value;
		TowerDefenseProjectileSkinData value2;
		return new ProjectileRegistrationSnapshot(ProjectileDictionary.TryGetValue(projectileName, out value), HasSkin: ProjectileSkinDictionary.TryGetValue(projectileName, out value2), Data: value, Skin: value2);
	}

	internal static void RestoreProjectileRegistration(StringName projectileName, ProjectileRegistrationSnapshot snapshot)
	{
		if (snapshot.HasData)
		{
			ProjectileDictionary[projectileName] = snapshot.Data;
		}
		else
		{
			ProjectileDictionary.Remove(projectileName);
		}
		if (snapshot.HasSkin)
		{
			ProjectileSkinDictionary[projectileName] = snapshot.Skin;
		}
		else
		{
			ProjectileSkinDictionary.Remove(projectileName);
		}
		Interlocked.Increment(ref _registrationRevision);
	}

	internal static bool RemoveProjectileRegistration(StringName projectileName)
	{
		bool num = ProjectileDictionary.Remove(projectileName) | ProjectileSkinDictionary.Remove(projectileName);
		if (num)
		{
			Interlocked.Increment(ref _registrationRevision);
		}
		return num;
	}

	public static void RegisterProjectileSkin(StringName projectileName, StringName skinName, PackedScene skinProjectileScene, PackedScene skinSplatScene)
	{
		if (ProjectileSkinDictionary.ContainsKey(projectileName))
		{
			ProjectileSkinDictionary[projectileName].AddSkin(skinName, skinProjectileScene, skinSplatScene);
			Interlocked.Increment(ref _registrationRevision);
		}
	}

	public static void RegisterProjectileChange(StringName changeName, StringName projectileName, StringName toProjectileName)
	{
		if (!ProjectileChangeDataDictionary.ContainsKey(changeName))
		{
			ProjectileChangeDataDictionary[changeName] = new TowerDefenseProjectileChangeData();
		}
		ProjectileChangeDataDictionary[changeName].AddChange(projectileName, toProjectileName);
	}

	private static T SafeLoad<T>(string path, string entry) where T : Resource
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		string text = path;
		if (path.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
		{
			long num = ResourceUid.TextToId(path);
			if (num == -1 || !ResourceUid.HasId(num))
			{
				GD.PushWarning($"Projectile registry skipped unresolved UID {path} ({entry}).");
				return null;
			}
			text = ResourceUid.GetIdPath(num);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
		}
		try
		{
			return ResourceLoader.Load(text, "", ResourceLoader.CacheMode.Reuse) as T;
		}
		catch (Exception ex)
		{
			GD.PushWarning($"Projectile registry skipped {text} ({entry}): {ex.Message}");
			return null;
		}
	}

	public static TowerDefenseProjectileData GetProjectile(StringName projectileName)
	{
		if (projectileName == null || !ProjectileDictionary.ContainsKey(projectileName))
		{
			return null;
		}
		return (TowerDefenseProjectileData)ProjectileDictionary[projectileName].Duplicate();
	}

	public static bool HasProjectileSkin(StringName projectileName, StringName skinName)
	{
		if (!ProjectileSkinDictionary.ContainsKey(projectileName))
		{
			return false;
		}
		return ProjectileSkinDictionary[projectileName].HasSkin(skinName);
	}

	public static PackedScene GetProjectileSkinProjectileScene(StringName projectileName, StringName skinName)
	{
		return ProjectileSkinDictionary[projectileName].GetSkinProjectileScene(skinName);
	}

	public static PackedScene GetProjectileSkinSplatScene(StringName projectileName, StringName skinName)
	{
		return ProjectileSkinDictionary[projectileName].GetSkinSplatScene(skinName);
	}

	public static StringName GetProjectileChange(StringName projectileName, StringName changeName)
	{
		if (!ProjectileChangeDataDictionary.ContainsKey(changeName))
		{
			return null;
		}
		return ProjectileChangeDataDictionary[changeName].GetChange(projectileName);
	}

	public static bool HasProjectileChange(StringName changeName)
	{
		return ProjectileChangeDataDictionary.ContainsKey(changeName);
	}

	public static bool IsStarProjectile(StringName projectileName, TowerDefenseProjectileConfig runtimeConfig = null)
	{
		if (runtimeConfig == null || !runtimeConfig.isStar)
		{
			if (projectileName != null && ProjectileDictionary.TryGetValue(projectileName, out var value) && value != null)
			{
				return value.isStar;
			}
			return false;
		}
		return true;
	}

	public static bool HasChangeTarget(StringName projectileName, StringName changeName)
	{
		if (!ProjectileChangeDataDictionary.ContainsKey(changeName))
		{
			return false;
		}
		return ProjectileChangeDataDictionary[changeName].HasChangeTarget(projectileName);
	}
}
