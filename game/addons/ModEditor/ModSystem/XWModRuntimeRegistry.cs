using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModRuntimeRegistry
{
	public sealed class Registration
	{
		public string OwnerMod { get; set; } = "";

		public string Category { get; set; } = "";

		public string Key { get; set; } = "";

		public Variant OriginalValue { get; set; }

		public Variant ModValue { get; set; }

		public bool OverridesBuiltIn { get; set; }

		public bool IsOverride { get; set; }

		public long LoadOrder { get; set; }

		public bool IsEffective { get; internal set; }
	}

	private sealed class RegistrationSlot
	{
		public string Category { get; init; } = "";

		public string Key { get; init; } = "";

		public Variant OriginalValue { get; init; }

		public bool HasOriginalValue { get; init; }

		public bool HasOriginalProjectileData { get; init; }

		public TowerDefenseProjectileData OriginalProjectileData { get; init; }

		public bool HasOriginalProjectileResource { get; init; }

		public Resource OriginalProjectileResource { get; init; }

		public Action RestoreOriginalRegistration { get; init; }

		public List<Registration> Layers { get; } = new List<Registration>();
	}

	private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, RegistrationSlot>> Slots = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, RegistrationSlot>>(StringComparer.OrdinalIgnoreCase);

	private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Registration>> Registrations = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Registration>>(StringComparer.OrdinalIgnoreCase);

	private static readonly System.Collections.Generic.Dictionary<string, Texture2D> RuntimeTextures = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private static readonly System.Collections.Generic.Dictionary<string, AdobeAnimateAtlasProfile> RuntimeAnimationAtlasProfiles = new System.Collections.Generic.Dictionary<string, AdobeAnimateAtlasProfile>(StringComparer.Ordinal);

	private static long _nextLoadOrder;

	public static bool Register(string ownerMod, string category, string key, Variant value, bool allowOverride = false)
	{
		string diagnostic;
		return RegisterCore(ownerMod, category, key, value, allowOverride, requireDeclaredOverrideTarget: false, out diagnostic);
	}

	public static bool Register(string ownerMod, string category, string key, Variant value, bool allowOverride, out string diagnostic)
	{
		return RegisterCore(ownerMod, category, key, value, allowOverride, requireDeclaredOverrideTarget: true, out diagnostic);
	}

	private static bool RegisterCore(string ownerMod, string category, string key, Variant value, bool allowOverride, bool requireDeclaredOverrideTarget, out string diagnostic)
	{
		diagnostic = "";
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			diagnostic = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";
			return false;
		}
		if (string.IsNullOrWhiteSpace(ownerMod) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key))
		{
			diagnostic = "owner, category and key must all be non-empty";
			return false;
		}
		ownerMod = ownerMod.Trim();
		category = NormalizeCategory(category.Trim());
		key = key.Trim();
		if (!Slots.TryGetValue(category, out var value2))
		{
			value2 = new System.Collections.Generic.Dictionary<string, RegistrationSlot>(StringComparer.Ordinal);
			Slots[category] = value2;
		}
		if (!value2.TryGetValue(key, out var value3))
		{
			Variant value4;
			bool flag = TryGetOriginalRuntimeValue(category, key, value, out value4);
			TowerDefenseProjectileData value5 = null;
			Resource value6 = null;
			bool hasOriginalProjectileData = category == "Projectile" && TowerDefenseProjectileRegistry.ProjectileDictionary.TryGetValue(new StringName(key), out value5);
			bool hasOriginalProjectileResource = category == "Projectile" && (ResourceManager.Instance?.PROJECTILE_CONFIG.TryGetValue(key, out value6) ?? false);
			if (allowOverride && requireDeclaredOverrideTarget && !flag)
			{
				RemoveEmptySlot(value2, category, key);
				diagnostic = $"overrides target is missing: {category}/{key}; declare it under provides instead";
				return false;
			}
			if (!allowOverride && flag)
			{
				RemoveEmptySlot(value2, category, key);
				diagnostic = $"provides collision with built-in runtime value: {category}/{key}; declare it under overrides to replace it";
				return false;
			}
			RegistrationSlot registrationSlot = new RegistrationSlot
			{
				Category = category,
				Key = key,
				OriginalValue = value4,
				HasOriginalValue = flag,
				HasOriginalProjectileData = hasOriginalProjectileData,
				OriginalProjectileData = value5,
				HasOriginalProjectileResource = hasOriginalProjectileResource,
				OriginalProjectileResource = value6,
				RestoreOriginalRegistration = category switch
				{
					"Feature" => TowerDefenseBattleRegistry.CaptureFeatureRegistration(key),
					"Process" => TowerDefenseBattleRegistry.CaptureProcessRegistration(key),
					"Projectile" => CaptureProjectileRegistration(key),
					_ => null,
				},
			};
			value3 = (value2[key] = registrationSlot);
		}
		Registration registration = value3.Layers.FirstOrDefault((Registration layer) => string.Equals(layer.OwnerMod, ownerMod, StringComparison.OrdinalIgnoreCase));
		if (registration == null && !allowOverride && value3.Layers.Count > 0)
		{
			List<Registration> layers = value3.Layers;
			string ownerMod2 = layers[layers.Count - 1].OwnerMod;
			diagnostic = $"provides collision with Mod owner '{ownerMod2}': {category}/{key}; declare it under overrides to stack above it";
			return false;
		}
		if (!CanSetRuntimeValue(category, value))
		{
			if (value3.Layers.Count == 0)
			{
				RemoveEmptySlot(value2, category, key);
			}
			diagnostic = "runtime value has the wrong type or unsupported category: " + category + "/" + key;
			return false;
		}
		bool flag2 = value3.HasOriginalValue;
		Variant originalValue = value3.OriginalValue;
		if (category == "Projectile")
		{
			bool num = value.AsGodotObject() is TowerDefenseProjectileConfig;
			flag2 = (num ? value3.HasOriginalProjectileResource : (value3.HasOriginalProjectileData || value3.HasOriginalProjectileResource));
			originalValue = ((!num) ? (value3.HasOriginalProjectileData ? Variant.From<TowerDefenseProjectileData>(value3.OriginalProjectileData) : (value3.HasOriginalProjectileResource ? Variant.From<Resource>(value3.OriginalProjectileResource) : default(Variant))) : (value3.HasOriginalProjectileResource ? Variant.From<Resource>(value3.OriginalProjectileResource) : default(Variant)));
			if (registration != null && !allowOverride && flag2)
			{
				diagnostic = $"provides collision with built-in runtime value: {category}/{key}; declare it under overrides to replace it";
				return false;
			}
		}
		int num2;
		if (registration != null)
		{
			List<Registration> layers2 = value3.Layers;
			num2 = ((registration == layers2[layers2.Count - 1]) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag3 = (byte)num2 != 0;
		if (registration == null || flag3)
		{
			if (category == "Projectile")
			{
				value3.RestoreOriginalRegistration?.Invoke();
			}
			if (!TrySetRuntimeValue(category, key, value, allowOverride: true))
			{
				if (category == "Projectile")
				{
					value3.RestoreOriginalRegistration?.Invoke();
					if (value3.Layers.Count > 0)
					{
						string category2 = category;
						string key2 = key;
						List<Registration> layers3 = value3.Layers;
						TrySetRuntimeValue(category2, key2, layers3[layers3.Count - 1].ModValue, allowOverride: true);
					}
				}
				if (value3.Layers.Count == 0)
				{
					RemoveEmptySlot(value2, category, key);
				}
				diagnostic = "runtime registry could not write: " + category + "/" + key;
				return false;
			}
		}
		if (registration != null)
		{
			registration.ModValue = value;
			registration.OriginalValue = originalValue;
			registration.OverridesBuiltIn = flag2;
			registration.IsOverride = allowOverride;
			XWModEnvironmentService.Invalidate("RuntimeRegistrationChanged");
			RefreshEffectiveRegistration(value3);
			object obj;
			if (!registration.IsEffective)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 4);
				defaultInterpolatedStringHandler.AppendLiteral("updated lower ");
				defaultInterpolatedStringHandler.AppendFormatted(DeclarationName(allowOverride));
				defaultInterpolatedStringHandler.AppendLiteral(" layer beneath owner '");
				List<Registration> layers4 = value3.Layers;
				defaultInterpolatedStringHandler.AppendFormatted(layers4[layers4.Count - 1].OwnerMod);
				defaultInterpolatedStringHandler.AppendLiteral("': ");
				defaultInterpolatedStringHandler.AppendFormatted(category);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(key);
				obj = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				obj = $"updated effective {DeclarationName(allowOverride)}: {category}/{key}";
			}
			diagnostic = (string)obj;
			return true;
		}
		Registration item = new Registration
		{
			OwnerMod = ownerMod,
			Category = category,
			Key = key,
			OriginalValue = originalValue,
			ModValue = value,
			OverridesBuiltIn = flag2,
			IsOverride = allowOverride,
			LoadOrder = ++_nextLoadOrder
		};
		value3.Layers.Add(item);
		XWModEnvironmentService.Invalidate("RuntimeRegistrationChanged");
		RefreshEffectiveRegistration(value3);
		diagnostic = (allowOverride ? $"stacked override above {DescribeLowerLayer(value3)}: {category}/{key}" : ("provided new runtime value: " + category + "/" + key));
		return true;
	}

	public static int UnregisterOwner(string ownerMod)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return 0;
		}
		if (string.IsNullOrWhiteSpace(ownerMod))
		{
			return 0;
		}
		int num = 0;
		foreach (KeyValuePair<string, System.Collections.Generic.Dictionary<string, RegistrationSlot>> item in Slots.ToList())
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, RegistrationSlot> item2 in item.Value)
			{
				RegistrationSlot value = item2.Value;
				int num2 = value.Layers.FindIndex((Registration layer) => string.Equals(layer.OwnerMod, ownerMod, StringComparison.OrdinalIgnoreCase));
				if (num2 < 0)
				{
					continue;
				}
				bool num3 = num2 == value.Layers.Count - 1;
				value.Layers.RemoveAt(num2);
				num++;
				if (num3)
				{
					RestoreEffectiveRuntimeValue(value);
				}
				if (value.Layers.Count == 0)
				{
					list.Add(item2.Key);
					if (Registrations.TryGetValue(item.Key, out var value2))
					{
						value2.Remove(item2.Key);
					}
				}
				else
				{
					RefreshEffectiveRegistration(value);
				}
			}
			foreach (string item3 in list)
			{
				item.Value.Remove(item3);
			}
			if (item.Value.Count == 0)
			{
				Slots.Remove(item.Key);
			}
		}
		foreach (string item4 in (from pair in Registrations
			where pair.Value.Count == 0
			select pair.Key).ToList())
		{
			Registrations.Remove(item4);
		}
		if (num > 0)
		{
			XWModEnvironmentService.Invalidate("RuntimeRegistrationRemoved");
		}
		return num;
	}

	public static IReadOnlyDictionary<string, System.Collections.Generic.Dictionary<string, Registration>> GetRegistrations()
	{
		return Registrations;
	}

	public static IReadOnlyList<Registration> GetRegistrationStack(string category, string key)
	{
		category = NormalizeCategory(category?.Trim() ?? "");
		key = key?.Trim() ?? "";
		if (Slots.TryGetValue(category, out var value) && value.TryGetValue(key, out var value2))
		{
			return value2.Layers.ToArray();
		}
		return System.Array.Empty<Registration>();
	}

	public static bool TryGetEffectiveRegistration(string category, string key, out Registration registration)
	{
		category = NormalizeCategory(category?.Trim() ?? "");
		key = key?.Trim() ?? "";
		registration = null;
		if (Registrations.TryGetValue(category, out var value))
		{
			return value.TryGetValue(key, out registration);
		}
		return false;
	}

	public static IReadOnlyDictionary<string, Texture2D> GetRuntimeTextures()
	{
		return RuntimeTextures;
	}

	public static bool TryGetRuntimeTexture(string key, out Texture2D texture)
	{
		texture = null;
		if (!string.IsNullOrWhiteSpace(key) && RuntimeTextures.TryGetValue(key.Trim(), out texture))
		{
			return GodotObject.IsInstanceValid(texture);
		}
		return false;
	}

	public static IReadOnlyDictionary<string, AdobeAnimateAtlasProfile> GetRuntimeAnimationAtlasProfiles()
	{
		return RuntimeAnimationAtlasProfiles;
	}

	public static bool TryGetRuntimeAnimationAtlasProfile(string key, out AdobeAnimateAtlasProfile profile)
	{
		profile = null;
		if (!string.IsNullOrWhiteSpace(key) && RuntimeAnimationAtlasProfiles.TryGetValue(key.Trim(), out profile))
		{
			return GodotObject.IsInstanceValid(profile);
		}
		return false;
	}

	private static void RestoreEffectiveRuntimeValue(RegistrationSlot slot)
	{
		if (slot.Category == "Projectile")
		{
			slot.RestoreOriginalRegistration?.Invoke();
			if (slot.Layers.Count > 0)
			{
				string category = slot.Category;
				string key = slot.Key;
				List<Registration> layers = slot.Layers;
				SetRuntimeValue(category, key, layers[layers.Count - 1].ModValue, allowOverride: true);
			}
		}
		else if (slot.Layers.Count > 0)
		{
			string category2 = slot.Category;
			string key2 = slot.Key;
			List<Registration> layers2 = slot.Layers;
			SetRuntimeValue(category2, key2, layers2[layers2.Count - 1].ModValue, allowOverride: true);
		}
		else if (slot.RestoreOriginalRegistration != null)
		{
			slot.RestoreOriginalRegistration();
		}
		else if (slot.HasOriginalValue)
		{
			SetRuntimeValue(slot.Category, slot.Key, slot.OriginalValue, allowOverride: true);
		}
		else
		{
			RemoveRuntimeValue(slot.Category, slot.Key);
		}
	}

	private static void RefreshEffectiveRegistration(RegistrationSlot slot)
	{
		foreach (Registration layer in slot.Layers)
		{
			layer.IsEffective = false;
		}
		if (slot.Layers.Count != 0)
		{
			List<Registration> layers = slot.Layers;
			Registration registration = layers[layers.Count - 1];
			registration.IsEffective = true;
			if (!Registrations.TryGetValue(slot.Category, out var value))
			{
				value = new System.Collections.Generic.Dictionary<string, Registration>(StringComparer.Ordinal);
				Registrations[slot.Category] = value;
			}
			value[slot.Key] = registration;
		}
	}

	private static void RemoveEmptySlot(System.Collections.Generic.Dictionary<string, RegistrationSlot> categorySlots, string category, string key)
	{
		categorySlots.Remove(key);
		if (categorySlots.Count == 0)
		{
			Slots.Remove(category);
		}
	}

	private static string DescribeLowerLayer(RegistrationSlot slot)
	{
		if (slot.Layers.Count > 1)
		{
			List<Registration> layers = slot.Layers;
			return "Mod owner '" + layers[layers.Count - 2].OwnerMod + "'";
		}
		if (!slot.HasOriginalValue)
		{
			return "a missing lower value";
		}
		return "the built-in runtime value";
	}

	private static string DeclarationName(bool allowOverride)
	{
		if (!allowOverride)
		{
			return "provide";
		}
		return "override";
	}

	private static Action CaptureProjectileRegistration(string key)
	{
		StringName projectileName = new StringName(key);
		TowerDefenseProjectileRegistry.ProjectileRegistrationSnapshot snapshot = TowerDefenseProjectileRegistry.CaptureProjectileRegistration(projectileName);
		System.Collections.Generic.Dictionary<string, Resource> dictionary = ResourceManager.Instance?.PROJECTILE_CONFIG;
		Resource original = null;
		bool hasResource = dictionary?.TryGetValue(key, out original) ?? false;
		return delegate
		{
			TowerDefenseProjectileRegistry.RestoreProjectileRegistration(projectileName, snapshot);
			System.Collections.Generic.Dictionary<string, Resource> dictionary2 = ResourceManager.Instance?.PROJECTILE_CONFIG;
			if (dictionary2 != null)
			{
				if (hasResource)
				{
					dictionary2[key] = original;
				}
				else
				{
					dictionary2.Remove(key);
				}
			}
		};
	}

	private static void SetRuntimeValue(string category, string key, Variant value, bool allowOverride)
	{
		TrySetRuntimeValue(category, key, value, allowOverride);
	}

	private static void RemoveRuntimeValue(string category, string key)
	{
		TryRemoveRuntimeValue(category, key);
	}

	private static bool TryGetOriginalRuntimeValue(string category, string key, Variant candidate, out Variant value)
	{
		if (category == "Projectile" && candidate.VariantType == Variant.Type.Object && candidate.AsGodotObject() is TowerDefenseProjectileConfig)
		{
			System.Collections.Generic.Dictionary<string, Resource> dictionary = ResourceManager.Instance?.PROJECTILE_CONFIG;
			if (dictionary != null)
			{
				return TryGetObjectDictionaryValue(dictionary, key, out value);
			}
			value = default(Variant);
			return false;
		}
		return TryGetRuntimeValue(category, key, out value);
	}

	private static bool TryGetRuntimeValue(string category, string key, out Variant value)
	{
		if (CategoryMatches(category, "Texture", "Textures", "Image", "Images"))
		{
			return TryGetObjectDictionaryValue(RuntimeTextures, key, out value);
		}
		if (CategoryMatches(category, "AnimationAtlas", "AnimationAtlases", "AtlasProfile", "AtlasProfiles"))
		{
			return TryGetObjectDictionaryValue(RuntimeAnimationAtlasProfiles, key, out value);
		}
		if (CategoryMatches(category, "Projectile", "Projectiles") && TowerDefenseProjectileRegistry.ProjectileDictionary.TryGetValue(new StringName(key), out var value2))
		{
			value = Variant.From(in value2);
			return true;
		}
		ResourceManager instance = ResourceManager.Instance;
		if (instance != null)
		{
			if (CategoryMatches(category, "Level", "Levels"))
			{
				return TryGetGodotDictionaryValue(instance.LEVELS, key, out value);
			}
			if (CategoryMatches(category, "Audio", "Audios"))
			{
				return TryGetObjectDictionaryValue(instance.AUDIOS, key, out value);
			}
			if (CategoryMatches(category, "BGM", "Bgms"))
			{
				instance.EnsureAllBgmsLoaded();
				return TryGetObjectDictionaryValue(instance.BGMS, key, out value);
			}
			if (CategoryMatches(category, "Map", "Maps"))
			{
				return TryGetObjectDictionaryValue(instance.MAPS, key, out value);
			}
			if (CategoryMatches(category, "Projectile", "Projectiles"))
			{
				return TryGetObjectDictionaryValue(instance.PROJECTILE_CONFIG, key, out value);
			}
			if (CategoryMatches(category, "CharacterSprite"))
			{
				return TryGetObjectDictionaryValue(instance.CHARCTAER_SPRITE, key, out value);
			}
			if (CategoryMatches(category, "Character", "Characters"))
			{
				return TryGetObjectDictionaryValue(instance.TOWERDEFENSE_CHARCATERS, key, out value);
			}
			if (CategoryMatches(category, "Packet", "Card", "Cards"))
			{
				return TryGetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETS, key, out value);
			}
			if (CategoryMatches(category, "PacketBank", "PacketBanks"))
			{
				return TryGetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETBANKS, key, out value);
			}
			if (CategoryMatches(category, "Tutorial", "Tutorials"))
			{
				return TryGetObjectDictionaryValue(instance.TUTORIALS, key, out value);
			}
			if (CategoryMatches(category, "NpcTalk", "Talk", "Talks"))
			{
				return TryGetObjectDictionaryValue(instance.TALKS, key, out value);
			}
			if (CategoryMatches(category, "Collectable", "Collectables"))
			{
				return TryGetObjectDictionaryValue(instance.COLLECTABLES, key, out value);
			}
			if (CategoryMatches(category, "Shovel", "Shovels"))
			{
				return TryGetObjectDictionaryValue(instance.SHOVELS, key, out value);
			}
			if (CategoryMatches(category, "Mower", "Mowers"))
			{
				return TryGetObjectDictionaryValue(instance.MOWERS, key, out value);
			}
			if (CategoryMatches(category, "Shop", "Shops"))
			{
				return TryGetObjectDictionaryValue(instance.SHOPS, key, out value);
			}
			if (CategoryMatches(category, "Survival", "Survivals"))
			{
				return TryGetObjectDictionaryValue(instance.SURVIVALS, key, out value);
			}
		}
		if (CategoryMatches(category, "Feature", "Features"))
		{
			return TryGetStringNameObjectDictionaryValue(TowerDefenseBattleRegistry.BattleFeatureDictionary, key, out value);
		}
		if (CategoryMatches(category, "Process", "Processes"))
		{
			return TryGetStringNameObjectDictionaryValue(TowerDefenseBattleRegistry.BattleProcessDictionary, key, out value);
		}
		if (CategoryMatches(category, "ProjectileChange", "ProjectileChanges"))
		{
			return TryGetStringNameObjectDictionaryValue(TowerDefenseProjectileRegistry.ProjectileChangeDataDictionary, key, out value);
		}
		value = default(Variant);
		return false;
	}

	private static bool TrySetRuntimeValue(string category, string key, Variant value, bool allowOverride)
	{
		if (CategoryMatches(category, "Texture", "Textures", "Image", "Images"))
		{
			return TrySetObjectDictionaryValue(RuntimeTextures, key, value, allowOverride);
		}
		if (CategoryMatches(category, "AnimationAtlas", "AnimationAtlases", "AtlasProfile", "AtlasProfiles"))
		{
			RuntimeAnimationAtlasProfiles.TryGetValue(key, out var value2);
			bool flag = TrySetObjectDictionaryValue(RuntimeAnimationAtlasProfiles, key, value, allowOverride);
			if (!flag || !(value.AsGodotObject() is AdobeAnimateAtlasProfile adobeAnimateAtlasProfile))
			{
				return flag;
			}
			if (GodotObject.IsInstanceValid(value2) && value2 != adobeAnimateAtlasProfile)
			{
				AdobeAnimateDefinitionCache.ReleaseProfile(value2);
			}
			if (adobeAnimateAtlasProfile.StartupOnly)
			{
				AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray(adobeAnimateAtlasProfile);
				AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray(adobeAnimateAtlasProfile);
			}
			return true;
		}
		ResourceManager instance = ResourceManager.Instance;
		if (instance != null)
		{
			if (CategoryMatches(category, "Level", "Levels"))
			{
				return TrySetGodotDictionaryValue(instance.LEVELS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Audio", "Audios"))
			{
				return TrySetObjectDictionaryValue(instance.AUDIOS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "BGM", "Bgms"))
			{
				instance.EnsureAllBgmsLoaded();
				return TrySetObjectDictionaryValue(instance.BGMS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Map", "Maps"))
			{
				return TrySetObjectDictionaryValue(instance.MAPS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Projectile", "Projectiles"))
			{
				if (value.VariantType != Variant.Type.Object || !(value.AsGodotObject() is Resource resource) || (!(resource is TowerDefenseProjectileData) && !(resource is TowerDefenseProjectileConfig)) || (!allowOverride && (instance.PROJECTILE_CONFIG.ContainsKey(key) || TowerDefenseProjectileRegistry.ProjectileDictionary.ContainsKey(new StringName(key)))))
				{
					return false;
				}
				if (resource is TowerDefenseProjectileData towerDefenseProjectileData)
				{
					TowerDefenseProjectileRegistry.RegisterProjectileOverlay(new StringName(key), towerDefenseProjectileData);
					if (!instance.PROJECTILE_CONFIG.TryGetValue(key, out var value3) || !(value3 is TowerDefenseProjectileConfig))
					{
						instance.PROJECTILE_CONFIG[key] = towerDefenseProjectileData;
					}
				}
				else
				{
					instance.PROJECTILE_CONFIG[key] = resource;
					TowerDefenseProjectileRegistry.NotifyProjectileConfigChanged();
				}
				return true;
			}
			if (CategoryMatches(category, "CharacterSprite"))
			{
				return TrySetObjectDictionaryValue(instance.CHARCTAER_SPRITE, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Character", "Characters"))
			{
				return TrySetObjectDictionaryValue(instance.TOWERDEFENSE_CHARCATERS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Packet", "Card", "Cards"))
			{
				return TrySetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "PacketBank", "PacketBanks"))
			{
				return TrySetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETBANKS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Tutorial", "Tutorials"))
			{
				return TrySetObjectDictionaryValue(instance.TUTORIALS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "NpcTalk", "Talk", "Talks"))
			{
				return TrySetObjectDictionaryValue(instance.TALKS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Collectable", "Collectables"))
			{
				return TrySetObjectDictionaryValue(instance.COLLECTABLES, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Shovel", "Shovels"))
			{
				return TrySetObjectDictionaryValue(instance.SHOVELS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Mower", "Mowers"))
			{
				return TrySetObjectDictionaryValue(instance.MOWERS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Shop", "Shops"))
			{
				return TrySetObjectDictionaryValue(instance.SHOPS, key, value, allowOverride);
			}
			if (CategoryMatches(category, "Survival", "Survivals"))
			{
				return TrySetObjectDictionaryValue(instance.SURVIVALS, key, value, allowOverride);
			}
		}
		if (CategoryMatches(category, "Feature", "Features"))
		{
			if (!(value.AsGodotObject() is TowerDefenseBattleFeature prototype) || (!allowOverride && TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(key)))
			{
				return false;
			}
			TowerDefenseBattleRegistry.SetFeaturePrototype(key, prototype);
			return true;
		}
		if (CategoryMatches(category, "Process", "Processes"))
		{
			if (!(value.AsGodotObject() is TowerDefenseBattleProcess prototype2) || (!allowOverride && TowerDefenseBattleRegistry.BattleProcessDictionary.ContainsKey(key)))
			{
				return false;
			}
			TowerDefenseBattleRegistry.SetProcessPrototype(key, prototype2);
			return true;
		}
		if (CategoryMatches(category, "ProjectileChange", "ProjectileChanges"))
		{
			return TrySetStringNameObjectDictionaryValue(TowerDefenseProjectileRegistry.ProjectileChangeDataDictionary, key, value, allowOverride);
		}
		return false;
	}

	private static bool CanSetRuntimeValue(string category, Variant value)
	{
		if (CategoryMatches(category, "Texture", "Textures", "Image", "Images"))
		{
			return CanSetObjectDictionaryValue(RuntimeTextures, value);
		}
		if (CategoryMatches(category, "AnimationAtlas", "AnimationAtlases", "AtlasProfile", "AtlasProfiles"))
		{
			return CanSetObjectDictionaryValue(RuntimeAnimationAtlasProfiles, value);
		}
		ResourceManager instance = ResourceManager.Instance;
		if (instance != null)
		{
			if (CategoryMatches(category, "Level", "Levels"))
			{
				return value.VariantType == Variant.Type.Dictionary;
			}
			if (CategoryMatches(category, "Audio", "Audios"))
			{
				return CanSetObjectDictionaryValue(instance.AUDIOS, value);
			}
			if (CategoryMatches(category, "BGM", "Bgms"))
			{
				return CanSetObjectDictionaryValue(instance.BGMS, value);
			}
			if (CategoryMatches(category, "Map", "Maps"))
			{
				return CanSetObjectDictionaryValue(instance.MAPS, value);
			}
			if (CategoryMatches(category, "Projectile", "Projectiles"))
			{
				bool flag = instance.PROJECTILE_CONFIG != null && value.VariantType == Variant.Type.Object;
				if (flag)
				{
					GodotObject godotObject = value.AsGodotObject();
					bool flag2 = ((godotObject is TowerDefenseProjectileData || godotObject is TowerDefenseProjectileConfig) ? true : false);
					flag = flag2;
				}
				return flag;
			}
			if (CategoryMatches(category, "CharacterSprite"))
			{
				return CanSetObjectDictionaryValue(instance.CHARCTAER_SPRITE, value);
			}
			if (CategoryMatches(category, "Character", "Characters"))
			{
				return CanSetObjectDictionaryValue(instance.TOWERDEFENSE_CHARCATERS, value);
			}
			if (CategoryMatches(category, "Packet", "Card", "Cards"))
			{
				return CanSetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETS, value);
			}
			if (CategoryMatches(category, "PacketBank", "PacketBanks"))
			{
				return CanSetObjectDictionaryValue(instance.TOWERDEFENSE_PACKETBANKS, value);
			}
			if (CategoryMatches(category, "Tutorial", "Tutorials"))
			{
				return CanSetObjectDictionaryValue(instance.TUTORIALS, value);
			}
			if (CategoryMatches(category, "NpcTalk", "Talk", "Talks"))
			{
				return CanSetObjectDictionaryValue(instance.TALKS, value);
			}
			if (CategoryMatches(category, "Collectable", "Collectables"))
			{
				return CanSetObjectDictionaryValue(instance.COLLECTABLES, value);
			}
			if (CategoryMatches(category, "Shovel", "Shovels"))
			{
				return CanSetObjectDictionaryValue(instance.SHOVELS, value);
			}
			if (CategoryMatches(category, "Mower", "Mowers"))
			{
				return CanSetObjectDictionaryValue(instance.MOWERS, value);
			}
			if (CategoryMatches(category, "Shop", "Shops"))
			{
				return CanSetObjectDictionaryValue(instance.SHOPS, value);
			}
			if (CategoryMatches(category, "Survival", "Survivals"))
			{
				return CanSetObjectDictionaryValue(instance.SURVIVALS, value);
			}
		}
		if (CategoryMatches(category, "Feature", "Features"))
		{
			return CanSetObjectDictionaryValue(TowerDefenseBattleRegistry.BattleFeatureDictionary, value);
		}
		if (CategoryMatches(category, "Process", "Processes"))
		{
			return CanSetObjectDictionaryValue(TowerDefenseBattleRegistry.BattleProcessDictionary, value);
		}
		if (CategoryMatches(category, "ProjectileChange", "ProjectileChanges"))
		{
			return CanSetObjectDictionaryValue(TowerDefenseProjectileRegistry.ProjectileChangeDataDictionary, value);
		}
		return false;
	}

	private static bool TryRemoveRuntimeValue(string category, string key)
	{
		if (CategoryMatches(category, "Texture", "Textures", "Image", "Images"))
		{
			return RuntimeTextures.Remove(key);
		}
		if (CategoryMatches(category, "AnimationAtlas", "AnimationAtlases", "AtlasProfile", "AtlasProfiles"))
		{
			if (!RuntimeAnimationAtlasProfiles.Remove(key, out var value))
			{
				return false;
			}
			AdobeAnimateDefinitionCache.ReleaseProfile(value);
			return true;
		}
		ResourceManager instance = ResourceManager.Instance;
		if (instance != null)
		{
			if (CategoryMatches(category, "Level", "Levels"))
			{
				return instance.LEVELS.Remove(key);
			}
			if (CategoryMatches(category, "Audio", "Audios"))
			{
				return instance.AUDIOS.Remove(key);
			}
			if (CategoryMatches(category, "BGM", "Bgms"))
			{
				return instance.BGMS.Remove(key);
			}
			if (CategoryMatches(category, "Map", "Maps"))
			{
				return instance.MAPS.Remove(key);
			}
			if (CategoryMatches(category, "Projectile", "Projectiles"))
			{
				bool flag = TowerDefenseProjectileRegistry.RemoveProjectileRegistration(new StringName(key));
				bool num = instance.PROJECTILE_CONFIG.Remove(key);
				if (num && !flag)
				{
					TowerDefenseProjectileRegistry.NotifyProjectileConfigChanged();
				}
				return num || flag;
			}
			if (CategoryMatches(category, "CharacterSprite"))
			{
				return instance.CHARCTAER_SPRITE.Remove(key);
			}
			if (CategoryMatches(category, "Character", "Characters"))
			{
				return instance.TOWERDEFENSE_CHARCATERS.Remove(key);
			}
			if (CategoryMatches(category, "Packet", "Card", "Cards"))
			{
				return instance.TOWERDEFENSE_PACKETS.Remove(key);
			}
			if (CategoryMatches(category, "PacketBank", "PacketBanks"))
			{
				return instance.TOWERDEFENSE_PACKETBANKS.Remove(key);
			}
			if (CategoryMatches(category, "Tutorial", "Tutorials"))
			{
				return instance.TUTORIALS.Remove(key);
			}
			if (CategoryMatches(category, "NpcTalk", "Talk", "Talks"))
			{
				return instance.TALKS.Remove(key);
			}
			if (CategoryMatches(category, "Collectable", "Collectables"))
			{
				return instance.COLLECTABLES.Remove(key);
			}
			if (CategoryMatches(category, "Shovel", "Shovels"))
			{
				return instance.SHOVELS.Remove(key);
			}
			if (CategoryMatches(category, "Mower", "Mowers"))
			{
				return instance.MOWERS.Remove(key);
			}
			if (CategoryMatches(category, "Shop", "Shops"))
			{
				return instance.SHOPS.Remove(key);
			}
			if (CategoryMatches(category, "Survival", "Survivals"))
			{
				return instance.SURVIVALS.Remove(key);
			}
		}
		if (CategoryMatches(category, "Feature", "Features"))
		{
			return TowerDefenseBattleRegistry.RemoveFeatureRegistration(key);
		}
		if (CategoryMatches(category, "Process", "Processes"))
		{
			return TowerDefenseBattleRegistry.RemoveProcessRegistration(key);
		}
		if (CategoryMatches(category, "ProjectileChange", "ProjectileChanges"))
		{
			return TowerDefenseProjectileRegistry.ProjectileChangeDataDictionary.Remove(new StringName(key));
		}
		return false;
	}

	private static bool TryGetObjectDictionaryValue<T>(System.Collections.Generic.Dictionary<string, T> dict, string key, out Variant value) where T : GodotObject
	{
		if (dict != null && dict.TryGetValue(key, out var value2))
		{
			value = Variant.From(in value2);
			return true;
		}
		value = default(Variant);
		return false;
	}

	private static bool TrySetObjectDictionaryValue<T>(System.Collections.Generic.Dictionary<string, T> dict, string key, Variant value, bool allowOverride) where T : GodotObject
	{
		if (dict == null)
		{
			return false;
		}
		if (!allowOverride && dict.ContainsKey(key))
		{
			return false;
		}
		if (!(value.AsGodotObject() is T value2))
		{
			return false;
		}
		dict[key] = value2;
		return true;
	}

	private static bool CanSetObjectDictionaryValue<TKey, T>(System.Collections.Generic.Dictionary<TKey, T> dict, Variant value) where TKey : notnull where T : GodotObject
	{
		if (dict != null && value.VariantType == Variant.Type.Object)
		{
			return value.AsGodotObject() is T;
		}
		return false;
	}

	private static bool TryGetStringNameObjectDictionaryValue<T>(System.Collections.Generic.Dictionary<StringName, T> dict, string key, out Variant value) where T : GodotObject
	{
		if (dict != null && dict.TryGetValue(new StringName(key), out var value2))
		{
			value = Variant.From(in value2);
			return true;
		}
		value = default(Variant);
		return false;
	}

	private static bool TrySetStringNameObjectDictionaryValue<T>(System.Collections.Generic.Dictionary<StringName, T> dict, string key, Variant value, bool allowOverride) where T : GodotObject
	{
		if (dict == null)
		{
			return false;
		}
		StringName key2 = new StringName(key);
		if (!allowOverride && dict.ContainsKey(key2))
		{
			return false;
		}
		if (!(value.AsGodotObject() is T value2))
		{
			return false;
		}
		dict[key2] = value2;
		return true;
	}

	private static bool TryGetGodotDictionaryValue(System.Collections.Generic.Dictionary<string, Dictionary> dict, string key, out Variant value)
	{
		if (dict != null && dict.TryGetValue(key, out var value2))
		{
			value = Variant.From(in value2);
			return true;
		}
		value = default(Variant);
		return false;
	}

	private static bool TrySetGodotDictionaryValue(System.Collections.Generic.Dictionary<string, Dictionary> dict, string key, Variant value, bool allowOverride)
	{
		if (dict == null)
		{
			return false;
		}
		if (!allowOverride && dict.ContainsKey(key))
		{
			return false;
		}
		if (value.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		dict[key] = value.AsGodotDictionary();
		return true;
	}

	private static bool CategoryMatches(string category, params string[] aliases)
	{
		foreach (string b in aliases)
		{
			if (string.Equals(category, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string NormalizeCategory(string category)
	{
		if (CategoryMatches(category, "Level", "Levels"))
		{
			return "Level";
		}
		if (CategoryMatches(category, "Texture", "Textures", "Image", "Images"))
		{
			return "Texture";
		}
		if (CategoryMatches(category, "Audio", "Audios"))
		{
			return "Audio";
		}
		if (CategoryMatches(category, "AnimationAtlas", "AnimationAtlases", "AtlasProfile", "AtlasProfiles"))
		{
			return "AnimationAtlas";
		}
		if (CategoryMatches(category, "BGM", "Bgms"))
		{
			return "BGM";
		}
		if (CategoryMatches(category, "Map", "Maps"))
		{
			return "Map";
		}
		if (CategoryMatches(category, "Projectile", "Projectiles"))
		{
			return "Projectile";
		}
		if (CategoryMatches(category, "CharacterSprite"))
		{
			return "CharacterSprite";
		}
		if (CategoryMatches(category, "Character", "Characters"))
		{
			return "Character";
		}
		if (CategoryMatches(category, "Packet", "Card", "Cards"))
		{
			return "Packet";
		}
		if (CategoryMatches(category, "PacketBank", "PacketBanks"))
		{
			return "PacketBank";
		}
		if (CategoryMatches(category, "Tutorial", "Tutorials"))
		{
			return "Tutorial";
		}
		if (CategoryMatches(category, "NpcTalk", "Talk", "Talks"))
		{
			return "NpcTalk";
		}
		if (CategoryMatches(category, "Collectable", "Collectables"))
		{
			return "Collectable";
		}
		if (CategoryMatches(category, "Shovel", "Shovels"))
		{
			return "Shovel";
		}
		if (CategoryMatches(category, "Mower", "Mowers"))
		{
			return "Mower";
		}
		if (CategoryMatches(category, "Shop", "Shops"))
		{
			return "Shop";
		}
		if (CategoryMatches(category, "Survival", "Survivals"))
		{
			return "Survival";
		}
		if (CategoryMatches(category, "Feature", "Features"))
		{
			return "Feature";
		}
		if (CategoryMatches(category, "Process", "Processes"))
		{
			return "Process";
		}
		if (CategoryMatches(category, "ProjectileChange", "ProjectileChanges"))
		{
			return "ProjectileChange";
		}
		return category;
	}
}
