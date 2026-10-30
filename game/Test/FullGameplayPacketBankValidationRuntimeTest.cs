using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class FullGameplayPacketBankValidationRuntimeTest : Node
{
	private const string ResultMarker = "FULL_GAMEPLAY_PACKET_BANK_VALIDATION_RESULT";

	private const string ValidRegistrySource = "{\n\t\"GeneralPlant\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"PlantPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"PresentBoxNoAshPlant\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"ZombiePresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"EliteZombie\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"ZombieImpPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"ZombieGargantuarPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } }\n}";

	private int _checkCount;

	private int _failureCount;

	public override void _Ready()
	{
		try
		{
			RunFixtures();
		}
		catch (Exception value)
		{
			_failureCount++;
			GD.PushError($"[FullGameplayPacketBankValidationRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failureCount == 0;
		GD.Print($"{"FULL_GAMEPLAY_PACKET_BANK_VALIDATION_RESULT"} passed={flag} checks={_checkCount} failures={_failureCount}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void RunFixtures()
	{
		FullGameplayRegistryRoots roots = CreateRegistryRoots();
		CheckNoFailures(FullGameplayResourceManifest.ValidatePacketBankClosure(CreateValidRegistry(), roots), "valid registry");
		Json json = CreateValidRegistry();
		json.Data.AsGodotDictionary()["BrokenBank"] = new Godot.Collections.Array();
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json, roots), "PacketBank entry is not an object: BrokenBank", "non-object bank");
		Json json2 = CreateValidRegistry();
		GetBank(json2, "GeneralPlant")["Category"] = new Godot.Collections.Array();
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json2, roots), "PacketBank Category is missing or is not an object: GeneralPlant", "wrong Category type");
		Json json3 = CreateValidRegistry();
		GetBank(json3, "GeneralPlant")["Include"] = new Dictionary();
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json3, roots), "PacketBank Include is missing or is not an array: GeneralPlant", "wrong Include type");
		Json json4 = CreateValidRegistry();
		GetBank(json4, "PlantPresentBox")["Category"] = new Dictionary { ["White"] = new Godot.Collections.Array() };
		List<string> failures = FullGameplayResourceManifest.ValidatePacketBankClosure(json4, roots);
		CheckContains(failures, "required PacketBank expands to an empty candidate pool: PlantPresentBox", "empty required pool");
		CheckContains(failures, "plant PresentBox PacketBank has no registered Plant candidate: PlantPresentBox", "empty plant PresentBox pool");
		Json json5 = CreateValidRegistry();
		GetBank(json5, "PlantPresentBox")["Category"] = new Dictionary { ["White"] = new Godot.Collections.Array { "ZombiePacket" } };
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json5, roots), "plant PresentBox PacketBank has no registered Plant candidate: PlantPresentBox", "wrong plant PresentBox candidate type");
		Json json6 = CreateValidRegistry();
		GetBank(json6, "ZombiePresentBox")["Category"] = new Dictionary { ["White"] = new Godot.Collections.Array { "PlantPacket" } };
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json6, roots), "zombie PresentBox PacketBank has no registered Zombie candidate: ZombiePresentBox", "wrong zombie PresentBox candidate type");
		Json json7 = CreateValidRegistry();
		GetBank(json7, "PlantPresentBox")["Include"] = new Godot.Collections.Array { "PresentBoxNoAshPlant" };
		GetBank(json7, "PresentBoxNoAshPlant")["Include"] = new Godot.Collections.Array { "PlantPresentBox" };
		CheckContains(FullGameplayResourceManifest.ValidatePacketBankClosure(json7, roots), "PacketBank Include cycle detected:", "Include cycle");
	}

	private static FullGameplayRegistryRoots CreateRegistryRoots()
	{
		System.Collections.Generic.Dictionary<string, string> dictionary = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["PlantFixture"] = "res://Test/Fixture/PlantFixture.tscn",
			["ZombieFixture"] = "res://Test/Fixture/ZombieFixture.tscn"
		};
		System.Collections.Generic.Dictionary<string, string> dictionary2 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["PlantFixture"] = "res://Test/Fixture/PlantFixtureSprite.tscn",
			["ZombieFixture"] = "res://Test/Fixture/ZombieFixtureSprite.tscn"
		};
		System.Collections.Generic.Dictionary<string, string> dictionary3 = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["PlantPacket"] = "res://Test/Fixture/PlantPacket.tres",
			["ZombiePacket"] = "res://Test/Fixture/ZombiePacket.tres"
		};
		System.Collections.Generic.Dictionary<string, string> characterNameByPacket = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["PlantPacket"] = "PlantFixture",
			["ZombiePacket"] = "ZombieFixture"
		};
		return new FullGameplayRegistryRoots(dictionary, dictionary2, dictionary3, characterNameByPacket, new List<string>(dictionary.Values), new List<string>(dictionary2.Values), new List<string>(dictionary3.Values), dictionary.Count);
	}

	private static Json CreateValidRegistry()
	{
		Json json = new Json();
		Error error = json.Parse("{\n\t\"GeneralPlant\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"PlantPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"PresentBoxNoAshPlant\": { \"Include\": [], \"Category\": { \"White\": [\"PlantPacket\"] } },\n\t\"ZombiePresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"EliteZombie\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"ZombieImpPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } },\n\t\"ZombieGargantuarPresentBox\": { \"Include\": [], \"Category\": { \"White\": [\"ZombiePacket\"] } }\n}");
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Unable to parse valid PacketBank fixture: {error}");
		}
		return json;
	}

	private static Dictionary GetBank(Json registry, string bankName)
	{
		return registry.Data.AsGodotDictionary()[bankName].AsGodotDictionary();
	}

	private void CheckNoFailures(IReadOnlyCollection<string> failures, string context)
	{
		_checkCount++;
		if (failures.Count != 0)
		{
			_failureCount++;
			GD.PushError("[FullGameplayPacketBankValidationRuntimeTest] " + context + " unexpectedly failed: " + string.Join(" | ", failures));
		}
	}

	private void CheckContains(IEnumerable<string> failures, string expectedText, string context)
	{
		_checkCount++;
		foreach (string failure in failures)
		{
			if (failure.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		_failureCount++;
		GD.PushError("[FullGameplayPacketBankValidationRuntimeTest] " + context + " did not report: " + expectedText);
	}
}
