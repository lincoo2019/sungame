using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class JalaVaseProgressRoundTripRuntimeTest : Node
{
	private const string JalaVaseKey = "PlantJalaVase";

	private const string JalaVasePacketPath = "res://Asset/Anime/Character/Plant/Star/JalaVase/Packet/PlantJalaVase.tres";

	private const string JalaVaseScenePath = "res://Asset/Anime/Character/Plant/Star/JalaVase/Scene/TowerDefensePlantJalaVase.tscn";

	private const string FullBodyAtlas = "uid://j6cc4gemmsje";

	private const string FullJalaAtlas = "uid://b57i3wamhxyrb";

	private const string FullBackAtlas = "uid://pnu58qk7jitu";

	private const string FullChunks = "uid://btoja37o43wvj";

	private static readonly (string Key, string PacketPath)[] ContentPackets = new(string, string)[4]
	{
		("PlantJalapeno", "res://Asset/Anime/Character/Plant/Chapter0/Jalapeno/Packet/PlantJalapeno.tres"),
		("PlantJalaNut", "res://Asset/Anime/Character/Plant/Chapter3/JalaNut/Packet/PlantJalaNut.tres"),
		("PlantJalaTorch", "res://Asset/Anime/Character/Plant/Chapter5/JalaTorch/Packet/PlantJalaTorch.tres"),
		("PlantJalaGhost", "res://Asset/Anime/Character/Plant/Other/JalaGhost/Packet/PlantJalaGhost.tres")
	};

	private static readonly double[] SavedTimers = new double[4] { 3.5, 12.25, 27.75, 49.0 };

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		ResourceManager resources = ResourceManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		System.Collections.Generic.Dictionary<string, Resource> previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();
		System.Collections.Generic.Dictionary<string, Resource> previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();
		List<string> registeredPacketKeys = new List<string>();
		List<string> registeredCharacterKeys = new List<string>();
		JalaVaseProgressRuntimeControlStub control = null;
		TowerDefensePlantJalaVase original = null;
		TowerDefensePlantJalaVase restored = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(resources), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources))
				{
					throw new InvalidOperationException("Required battle autoloads are missing.");
				}
				control = new JalaVaseProgressRuntimeControlStub
				{
					Name = "JalaVaseProgressRuntimeControl",
					isGameRunning = false,
					isInit = false,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Star/JalaVase/Packet/PlantJalaVase.tres", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/JalaVase/Scene/TowerDefensePlantJalaVase.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(packedScene), "The production Jala Vase packet and scene must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene))
				{
					throw new InvalidOperationException("Production Jala Vase fixtures did not load.");
				}
				RegisterPacket(resources, "PlantJalaVase", towerDefensePacketConfig, previousPackets, registeredPacketKeys);
				RegisterCharacter(resources, "PlantJalaVase", packedScene, previousCharacters, registeredCharacterKeys);
				(string, string)[] contentPackets = ContentPackets;
				for (int i = 0; i < contentPackets.Length; i++)
				{
					(string, string) tuple = contentPackets[i];
					TowerDefensePacketConfig towerDefensePacketConfig2 = ResourceLoader.Load<TowerDefensePacketConfig>(tuple.Item2, null, ResourceLoader.CacheMode.Ignore);
					Check(GodotObject.IsInstanceValid(towerDefensePacketConfig2), "Contained packet '" + tuple.Item1 + "' must load.");
					if (!GodotObject.IsInstanceValid(towerDefensePacketConfig2))
					{
						throw new InvalidOperationException("Contained packet '" + tuple.Item1 + "' did not load.");
					}
					RegisterPacket(resources, tuple.Item1, towerDefensePacketConfig2, previousPackets, registeredPacketKeys);
				}
				original = towerDefensePacketConfig.Create(Vector2.Zero, Vector2I.Zero) as TowerDefensePlantJalaVase;
				Check(GodotObject.IsInstanceValid(original), "The production packet must create a real Jala Vase.");
				if (!GodotObject.IsInstanceValid(original))
				{
					throw new InvalidOperationException("Jala Vase creation failed.");
				}
				control.characterNode.AddChild(original, forceReadableName: false, InternalMode.Disabled);
				await WaitPhysicsFrames(3);
				for (int j = 0; j < ContentPackets.Length; j++)
				{
					TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(ContentPackets[j].Key);
					if (j == 2)
					{
						packetConfig._override = new TowerDefensePacketOverride
						{
							cost = 321,
							hypnoses = true
						};
						packetConfig.canChangeCost = false;
						packetConfig.overrideHypnoses = true;
					}
					original.jalaList.Add(packetConfig);
					original.timerList[j] = SavedTimers[j];
				}
				original.timeNeed = 61.5;
				TowerDefenseCharacterSaveConfigCSharp characterSave = new TowerDefenseCharacterSaveConfigCSharp();
				characterSave.SaveCharacter(original);
				Godot.Collections.Array array = characterSave.variantSave.GetValueOrDefault("jalaList", new Godot.Collections.Array()).AsGodotArray();
				Godot.Collections.Array array2 = characterSave.variantSave.GetValueOrDefault("timerList", new Godot.Collections.Array()).AsGodotArray();
				Check(array.Count == ContentPackets.Length, "Progress must capture every contained Jala packet.");
				Check(array2.Count == ContentPackets.Length, "Progress must capture one timer for every contained packet.");
				original.QueueFree();
				await WaitPhysicsFrames(3);
				original = null;
				TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = (characterSave.owner = new TowerDefenseLevelSaveConfigCSharp());
				restored = characterSave.InstantiateCharacterForRestore() as TowerDefensePlantJalaVase;
				Check(GodotObject.IsInstanceValid(restored), "The production progress path must recreate the saved Jala Vase.");
				if (!GodotObject.IsInstanceValid(restored))
				{
					throw new InvalidOperationException("Jala Vase progress creation failed.");
				}
				towerDefenseLevelSaveConfigCSharp.charcterDicionary[characterSave.nodeName] = restored;
				characterSave.RestoreCharacter(restored);
				Check(restored.jalaList.Count == ContentPackets.Length, "Restore must recreate every contained Jala packet.");
				for (int k = 0; k < ContentPackets.Length; k++)
				{
					Check(restored.jalaList[k].saveKey == ContentPackets[k].Key, $"Restored content slot {k} must retain its packet key.");
					Check(Math.Abs(restored.timerList[k] - SavedTimers[k]) < 0.0001, $"Restored content slot {k} must retain its timer.");
					Check(restored.jalaList[k].coldDownDecreaseDictionary.ContainsKey("JalaVase"), $"Restored content slot {k} must re-register its cooldown bonus.");
				}
				Check(Math.Abs(restored.timeNeed - 61.5) < 0.0001, "Restore must retain the configured trigger interval.");
				JalaVaseProgressRoundTripRuntimeTest jalaVaseProgressRoundTripRuntimeTest = this;
				TowerDefensePacketOverride towerDefensePacketOverride = restored.jalaList[2]._override;
				jalaVaseProgressRoundTripRuntimeTest.Check(towerDefensePacketOverride != null && towerDefensePacketOverride.cost == 321 && (restored.jalaList[2]._override?.hypnoses ?? false) && !restored.jalaList[2].canChangeCost && restored.jalaList[2].overrideHypnoses, "Restore must retain contained packet override and mutable runtime flags.");
				Check(restored.sprite.GetAtlasReplacePath("JalaVase_body.png") == "uid://j6cc4gemmsje" && restored.sprite.GetAtlasReplacePath("JalaVase_jala.png") == "uid://b57i3wamhxyrb" && restored.sprite.GetAtlasReplacePath("JalaVase_back.png") == "uid://pnu58qk7jitu", "Restore must rebuild the four-content Jala Vase appearance.");
				PackedScene packedScene2 = GD.Load<PackedScene>("uid://btoja37o43wvj");
				Check(GodotObject.IsInstanceValid(restored.chunksEffect) && GodotObject.IsInstanceValid(packedScene2) && restored.chunksEffect.ResourcePath == packedScene2.ResourcePath, "Restore must rebuild the full-vase break particle selection.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[JalaVaseProgressRoundTripRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(original) && !original.IsQueuedForDeletion())
			{
				original.QueueFree();
			}
			if (GodotObject.IsInstanceValid(restored) && !restored.IsQueuedForDeletion())
			{
				restored.QueueFree();
			}
			await WaitPhysicsFrames(3);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(resources))
			{
				RestoreRegistry(resources.TOWERDEFENSE_PACKETS, registeredPacketKeys, previousPackets);
				RestoreRegistry(resources.TOWERDEFENSE_CHARCATERS, registeredCharacterKeys, previousCharacters);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"JALA_VASE_PROGRESS_ROUNDTRIP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void RegisterPacket(ResourceManager resources, string key, Resource resource, System.Collections.Generic.Dictionary<string, Resource> previous, List<string> registered)
	{
		if (resources.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			previous[key] = value;
		}
		resources.TOWERDEFENSE_PACKETS[key] = resource;
		registered.Add(key);
	}

	private static void RegisterCharacter(ResourceManager resources, string key, Resource resource, System.Collections.Generic.Dictionary<string, Resource> previous, List<string> registered)
	{
		if (resources.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			previous[key] = value;
		}
		resources.TOWERDEFENSE_CHARCATERS[key] = resource;
		registered.Add(key);
	}

	private static void RestoreRegistry(System.Collections.Generic.Dictionary<string, Resource> registry, List<string> registered, System.Collections.Generic.Dictionary<string, Resource> previous)
	{
		foreach (string item in registered)
		{
			if (previous.TryGetValue(item, out var value))
			{
				registry[item] = value;
			}
			else
			{
				registry.Remove(item);
			}
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[JalaVaseProgressRoundTripRuntimeTest] " + message);
		}
	}
}
