using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;

public static class FullGameplayResourceManifest
{
	public const int SchemaVersion = 1;

	public const string ResourcePath = "res://Asset/Config/Character/FullGameplayResources.json";

	public static IReadOnlyList<string> RequiredRuntimePacketBanks { get; } = System.Array.AsReadOnly(new string[7] { "GeneralPlant", "PlantPresentBox", "PresentBoxNoAshPlant", "ZombiePresentBox", "EliteZombie", "ZombieImpPresentBox", "ZombieGargantuarPresentBox" });

	public static IReadOnlyList<string> RequiredPlantPresentBoxPacketBanks { get; } = System.Array.AsReadOnly(new string[2] { "PlantPresentBox", "PresentBoxNoAshPlant" });

	public static IReadOnlyList<string> RequiredZombiePresentBoxPacketBanks { get; } = System.Array.AsReadOnly(new string[4] { "ZombiePresentBox", "EliteZombie", "ZombieImpPresentBox", "ZombieGargantuarPresentBox" });

	public static FullGameplayRegistryRoots GetRegistryRoots(Json characterRegistry)
	{
		if (characterRegistry == null || characterRegistry.Data.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("CharacterResource.json is unavailable or is not a JSON object.");
		}
		Dictionary dictionary = characterRegistry.Data.AsGodotDictionary();
		System.Collections.Generic.Dictionary<string, string> dictionary2 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		System.Collections.Generic.Dictionary<string, string> dictionary3 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		System.Collections.Generic.Dictionary<string, string> dictionary4 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		System.Collections.Generic.Dictionary<string, string> dictionary5 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			Variant variant = dictionary[key];
			if (variant.VariantType != Variant.Type.Dictionary)
			{
				throw new InvalidOperationException("Character registry entry is not an object: " + text);
			}
			Dictionary dictionary6 = variant.AsGodotDictionary();
			string value = ReadConfiguredPath(dictionary6, "Scene", text);
			string value2 = ReadConfiguredPath(dictionary6, "Sprite", text);
			if (!string.IsNullOrEmpty(value))
			{
				dictionary2[text] = value;
			}
			if (!string.IsNullOrEmpty(value2))
			{
				dictionary3[text] = value2;
			}
			if (!dictionary6.TryGetValue("Packet", out var value3) || value3.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary7 = value3.AsGodotDictionary();
			foreach (Variant key2 in dictionary7.Keys)
			{
				string text2 = key2.AsString();
				string text3 = NormalizeConfiguredPath(dictionary7[key2].AsString(), text + ".Packet." + text2);
				if (dictionary4.TryGetValue(text2, out var value4) && !string.Equals(value4, text3, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException($"Packet is registered with conflicting paths: {text2} -> {value4}, {text3}");
				}
				if (dictionary5.TryGetValue(text2, out var value5) && !string.Equals(value5, text, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException($"Packet is registered by multiple characters: {text2} -> {value5}, {text}");
				}
				dictionary4[text2] = text3;
				dictionary5[text2] = text;
			}
		}
		return new FullGameplayRegistryRoots(dictionary2, dictionary3, dictionary4, dictionary5, SortedUnique(dictionary2.Values), SortedUnique(dictionary3.Values), SortedUnique(dictionary4.Values), dictionary.Count);
	}

	public static string ComputeCharacterRegistrySignature(FullGameplayRegistryRoots roots)
	{
		List<string> list = new List<string>(roots.ScenePathByCharacter.Keys);
		HashSet<string> hashSet = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
		foreach (string key in roots.SpritePathByCharacter.Keys)
		{
			if (hashSet.Add(key))
			{
				list.Add(key);
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder(list.Count * 192);
		foreach (string item2 in list)
		{
			stringBuilder.Append("character:").Append(item2).Append('\n');
			stringBuilder.Append("scene:").Append(roots.ScenePathByCharacter.GetValueOrDefault(item2, string.Empty)).Append('\n');
			stringBuilder.Append("sprite:").Append(roots.SpritePathByCharacter.GetValueOrDefault(item2, string.Empty)).Append('\n');
			List<string> list2 = new List<string>();
			foreach (var (item, a) in roots.CharacterNameByPacket)
			{
				if (string.Equals(a, item2, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(item);
				}
			}
			list2.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (string item3 in list2)
			{
				stringBuilder.Append("packet:").Append(item3).Append('=')
					.Append(roots.PacketPathByName[item3])
					.Append('\n');
			}
		}
		return ComputeSha256(stringBuilder.ToString());
	}

	public static string ComputePacketBankRegistrySignature(Json packetBankRegistry)
	{
		if (packetBankRegistry == null || packetBankRegistry.Data.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("PacketBankResource.json is unavailable or is not a JSON object.");
		}
		StringBuilder stringBuilder = new StringBuilder();
		AppendCanonicalVariant(stringBuilder, packetBankRegistry.Data);
		return ComputeSha256(stringBuilder.ToString());
	}

	public static FullGameplayResourceManifestData ReadAndValidate(Json manifestResource, Json characterRegistry, Json packetBankRegistry)
	{
		if (manifestResource == null || manifestResource.Data.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("Full gameplay resource manifest is unavailable: res://Asset/Config/Character/FullGameplayResources.json");
		}
		FullGameplayRegistryRoots registryRoots = GetRegistryRoots(characterRegistry);
		Dictionary data = manifestResource.Data.AsGodotDictionary();
		RequireEqual(ReadRequiredInt(data, "schemaVersion"), 1, "schemaVersion");
		RequireEqual(ReadRequiredInt(data, "registeredCharacterCount"), registryRoots.RegisteredCharacterCount, "registeredCharacterCount");
		RequireEqual(ReadRequiredInt(data, "registeredSpriteCount"), registryRoots.SpritePathByCharacter.Count, "registeredSpriteCount");
		RequireEqual(ReadRequiredInt(data, "registeredPacketCount"), registryRoots.PacketPathByName.Count, "registeredPacketCount");
		RequireEqual(ReadRequiredString(data, "characterRegistrySignature"), ComputeCharacterRegistrySignature(registryRoots), "characterRegistrySignature");
		RequireEqual(ReadRequiredString(data, "packetBankRegistrySignature"), ComputePacketBankRegistrySignature(packetBankRegistry), "packetBankRegistrySignature");
		string dependencyGraphSignature = ReadRequiredString(data, "dependencyGraphSignature");
		List<string> list = ReadSortedPathArray(data, "characterSceneRoots");
		List<string> list2 = ReadSortedPathArray(data, "independentSpriteRoots");
		List<string> list3 = ReadSortedPathArray(data, "packetRoots");
		RequirePathListsEqual(list, registryRoots.UniqueSceneRoots, "characterSceneRoots");
		RequirePathListsEqual(list3, registryRoots.UniquePacketRoots, "packetRoots");
		HashSet<string> hashSet = new HashSet<string>(registryRoots.UniqueSpritePaths, StringComparer.OrdinalIgnoreCase);
		foreach (string item in list2)
		{
			if (!hashSet.Contains(item))
			{
				throw new InvalidOperationException("Full gameplay manifest contains an unregistered independent Sprite root: " + item);
			}
		}
		return new FullGameplayResourceManifestData(list, list2, list3, registryRoots.RegisteredCharacterCount, registryRoots.SpritePathByCharacter.Count, registryRoots.PacketPathByName.Count, dependencyGraphSignature);
	}

	public static List<string> ValidatePacketBankClosure(Json packetBankRegistry, FullGameplayRegistryRoots roots)
	{
		if (packetBankRegistry == null || packetBankRegistry.Data.VariantType != Variant.Type.Dictionary)
		{
			return new List<string> { "PacketBankResource.json is unavailable or invalid." };
		}
		if (roots == null)
		{
			return new List<string> { "Character registry roots are unavailable for PacketBank validation." };
		}
		Dictionary dictionary = packetBankRegistry.Data.AsGodotDictionary();
		List<string> list = new List<string>();
		foreach (string requiredRuntimePacketBank in RequiredRuntimePacketBanks)
		{
			if (!dictionary.ContainsKey(requiredRuntimePacketBank))
			{
				list.Add("required PacketBank is missing: " + requiredRuntimePacketBank);
			}
		}
		foreach (Variant key in dictionary.Keys)
		{
			ValidatePacketBankStructure(key.AsString(), dictionary[key], list);
		}
		if (list.Count > 0)
		{
			return SortUniqueFailures(list);
		}
		System.Collections.Generic.Dictionary<string, HashSet<string>> cache = new System.Collections.Generic.Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (Variant key2 in dictionary.Keys)
		{
			string text = key2.AsString();
			HashSet<string> hashSet;
			try
			{
				hashSet = ExpandPacketBank(text, dictionary, cache, new List<string>());
			}
			catch (Exception ex)
			{
				list.Add(ex.Message);
				continue;
			}
			if (ContainsIgnoreCase(RequiredRuntimePacketBanks, text) && hashSet.Count == 0)
			{
				list.Add("required PacketBank expands to an empty candidate pool: " + text);
			}
			foreach (string item in hashSet)
			{
				if (!roots.PacketPathByName.ContainsKey(item))
				{
					list.Add("PacketBank " + text + " references missing Packet config: " + item);
					continue;
				}
				if (!roots.CharacterNameByPacket.TryGetValue(item, out var value))
				{
					list.Add("PacketBank " + text + " has no character owner: " + item);
					continue;
				}
				if (!roots.ScenePathByCharacter.ContainsKey(value))
				{
					list.Add($"PacketBank {text} character has no Scene binding: {item} -> {value}");
				}
				if (!roots.SpritePathByCharacter.ContainsKey(value))
				{
					list.Add($"PacketBank {text} character has no Sprite binding: {item} -> {value}");
				}
			}
			if (ContainsIgnoreCase(RequiredPlantPresentBoxPacketBanks, text) && !ContainsCharacterCandidate(hashSet, roots, "Plant"))
			{
				list.Add("plant PresentBox PacketBank has no registered Plant candidate: " + text);
			}
			if (ContainsIgnoreCase(RequiredZombiePresentBoxPacketBanks, text) && !ContainsCharacterCandidate(hashSet, roots, "Zombie"))
			{
				list.Add("zombie PresentBox PacketBank has no registered Zombie candidate: " + text);
			}
		}
		return SortUniqueFailures(list);
	}

	private static void ValidatePacketBankStructure(string bankName, Variant bankValue, List<string> failures)
	{
		if (bankValue.VariantType != Variant.Type.Dictionary)
		{
			failures.Add("PacketBank entry is not an object: " + bankName);
			return;
		}
		Dictionary dictionary = bankValue.AsGodotDictionary();
		if (!dictionary.TryGetValue("Category", out var value) || value.VariantType != Variant.Type.Dictionary)
		{
			failures.Add("PacketBank Category is missing or is not an object: " + bankName);
		}
		else
		{
			Dictionary dictionary2 = value.AsGodotDictionary();
			foreach (Variant key in dictionary2.Keys)
			{
				string text = key.AsString();
				Variant variant = dictionary2[key];
				if (variant.VariantType != Variant.Type.Array)
				{
					failures.Add("PacketBank Category entry is not an array: " + bankName + "." + text);
					continue;
				}
				foreach (Variant item in variant.AsGodotArray())
				{
					if (item.VariantType != Variant.Type.String || string.IsNullOrWhiteSpace(item.AsString()))
					{
						failures.Add("PacketBank Category contains an invalid Packet name: " + bankName + "." + text);
					}
				}
			}
		}
		if (!dictionary.TryGetValue("Include", out var value2) || value2.VariantType != Variant.Type.Array)
		{
			failures.Add("PacketBank Include is missing or is not an array: " + bankName);
			return;
		}
		foreach (Variant item2 in value2.AsGodotArray())
		{
			if (item2.VariantType != Variant.Type.String || string.IsNullOrWhiteSpace(item2.AsString()))
			{
				failures.Add("PacketBank Include contains an invalid bank name: " + bankName);
			}
		}
	}

	private static bool ContainsCharacterCandidate(IEnumerable<string> packets, FullGameplayRegistryRoots roots, string characterKind)
	{
		foreach (string packet in packets)
		{
			if (roots.CharacterNameByPacket.TryGetValue(packet, out var value))
			{
				if (value.StartsWith(characterKind, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (roots.ScenePathByCharacter.TryGetValue(value, out var value2) && value2.Contains("/" + characterKind + "/", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool ContainsIgnoreCase(IReadOnlyList<string> values, string target)
	{
		foreach (string value in values)
		{
			if (string.Equals(value, target, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static List<string> SortUniqueFailures(IEnumerable<string> failures)
	{
		List<string> list = new List<string>(new HashSet<string>(failures, StringComparer.OrdinalIgnoreCase));
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static HashSet<string> ExpandPacketBank(string bankName, Dictionary banks, System.Collections.Generic.Dictionary<string, HashSet<string>> cache, List<string> visiting)
	{
		if (cache.TryGetValue(bankName, out var value))
		{
			return value;
		}
		if (!banks.TryGetValue(bankName, out var value2) || value2.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("PacketBank Include references missing bank: " + bankName);
		}
		int num = visiting.FindIndex((string a) => string.Equals(a, bankName, StringComparison.OrdinalIgnoreCase));
		if (num >= 0)
		{
			List<string> range = visiting.GetRange(num, visiting.Count - num);
			range.Add(bankName);
			throw new InvalidOperationException("PacketBank Include cycle detected: " + string.Join(" -> ", range));
		}
		visiting.Add(bankName);
		Dictionary dictionary = value2.AsGodotDictionary();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (dictionary.TryGetValue("Category", out var value3) && value3.VariantType == Variant.Type.Dictionary)
			{
				foreach (Variant value5 in value3.AsGodotDictionary().Values)
				{
					if (value5.VariantType != Variant.Type.Array)
					{
						throw new InvalidOperationException("PacketBank category is not an array: " + bankName);
					}
					foreach (Variant item in value5.AsGodotArray())
					{
						hashSet.Add(item.AsString());
					}
				}
			}
			if (dictionary.TryGetValue("Include", out var value4) && value4.VariantType == Variant.Type.Array)
			{
				foreach (Variant item2 in value4.AsGodotArray())
				{
					hashSet.UnionWith(ExpandPacketBank(item2.AsString(), banks, cache, visiting));
				}
			}
			cache[bankName] = hashSet;
			return hashSet;
		}
		finally
		{
			visiting.RemoveAt(visiting.Count - 1);
		}
	}

	private static string ReadConfiguredPath(Dictionary character, string propertyName, string characterName)
	{
		if (!character.TryGetValue(propertyName, out var value) || string.IsNullOrWhiteSpace(value.AsString()))
		{
			return string.Empty;
		}
		return NormalizeConfiguredPath(value.AsString(), characterName + "." + propertyName);
	}

	private static string NormalizeConfiguredPath(string configuredPath, string context)
	{
		string text = ProjectResourceUidCache.ResolveResourcePath(configuredPath).Replace('\\', '/');
		if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Registered resource path cannot be resolved: " + context + " -> " + configuredPath);
		}
		return text;
	}

	private static List<string> SortedUnique(IEnumerable<string> paths)
	{
		List<string> list = new List<string>(new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase));
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static int ReadRequiredInt(Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out var value))
		{
			throw new InvalidOperationException("Full gameplay manifest is missing " + key + ".");
		}
		return value.AsInt32();
	}

	private static string ReadRequiredString(Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value.AsString()))
		{
			throw new InvalidOperationException("Full gameplay manifest property is missing or empty: " + key);
		}
		return value.AsString();
	}

	private static List<string> ReadSortedPathArray(Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out var value) || value.VariantType != Variant.Type.Array)
		{
			throw new InvalidOperationException("Full gameplay manifest has no " + key + " array.");
		}
		List<string> list = new List<string>();
		string text = string.Empty;
		foreach (Variant item in value.AsGodotArray())
		{
			string text2 = item.AsString().Replace('\\', '/');
			if (!text2.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Full gameplay manifest contains an invalid path in " + key + ": " + text2);
			}
			if (!string.IsNullOrEmpty(text) && StringComparer.OrdinalIgnoreCase.Compare(text, text2) >= 0)
			{
				throw new InvalidOperationException("Full gameplay manifest paths are not strictly sorted and unique in " + key + ": " + text2);
			}
			list.Add(text2);
			text = text2;
		}
		return list;
	}

	private static void RequirePathListsEqual(IReadOnlyList<string> actual, IReadOnlyList<string> expected, string propertyName)
	{
		RequireEqual(actual.Count, expected.Count, propertyName + ".Count");
		for (int i = 0; i < expected.Count; i++)
		{
			RequireEqual(actual[i], expected[i], $"{propertyName}[{i}]");
		}
	}

	private static void RequireEqual(int actual, int expected, string propertyName)
	{
		if (actual != expected)
		{
			throw new InvalidOperationException($"Full gameplay manifest is stale at {propertyName}: actual={actual}, expected={expected}. Refresh Adobe Atlas.");
		}
	}

	private static void RequireEqual(string actual, string expected, string propertyName)
	{
		if (!string.Equals(actual, expected, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Full gameplay manifest is stale at " + propertyName + ". Refresh Adobe Atlas.");
		}
	}

	private static void AppendCanonicalVariant(StringBuilder source, Variant value)
	{
		switch (value.VariantType)
		{
		case Variant.Type.Dictionary:
		{
			Dictionary dictionary = value.AsGodotDictionary();
			List<string> list = new List<string>();
			foreach (Variant key in dictionary.Keys)
			{
				list.Add(key.AsString());
			}
			list.Sort(StringComparer.Ordinal);
			source.Append('{');
			foreach (string item in list)
			{
				source.Append(item.Length).Append(':').Append(item)
					.Append('=');
				AppendCanonicalVariant(source, dictionary[item]);
			}
			source.Append('}');
			break;
		}
		case Variant.Type.Array:
			source.Append('[');
			foreach (Variant item2 in value.AsGodotArray())
			{
				AppendCanonicalVariant(source, item2);
			}
			source.Append(']');
			break;
		default:
		{
			string text = GD.VarToStr(value);
			source.Append((int)value.VariantType).Append(':').Append(text.Length)
				.Append(':')
				.Append(text)
				.Append(';');
			break;
		}
		}
	}

	private static string ComputeSha256(string source)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
	}
}
