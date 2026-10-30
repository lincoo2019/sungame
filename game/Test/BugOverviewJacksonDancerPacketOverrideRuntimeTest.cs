using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewJacksonDancerPacketOverrideRuntimeTest : Node
{
	private const string JacksonPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Packet/ZombieJackson.tres";

	private const string JacksonScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Scene/TowerDefenseZombieJackson.tscn";

	private const string DancerPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Packet/ZombieDancer.tres";

	private const string AlternateDancerPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Packet/ZombieDancerCone.tres";

	private const string DancerScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Scene/TowerDefenseZombieDancer.tscn";

	private static readonly Vector2I JacksonGrid = new Vector2I(5, 3);

	private const double OverrideHitpointScale = 2.75;

	private const float OverrideVisualScale = 1.35f;

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<Resource> _registeredResources = new List<Resource>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		JacksonDancerPacketOverrideRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieJackson jackson = null;
		TowerDefenseZombie restoredDancer = null;
		List<TowerDefenseZombie> spawned = new List<TowerDefenseZombie>();
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0103;
				}
				ResourceManager.Instance.BeginLoad();
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewJacksonDancerPacketOverrideRuntimeTest");
				GD.Print("JACKSON_DANCER_PACKET_OVERRIDE_FIXTURE_READY");
				RegisterRealFixtures();
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieJackson");
				TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly("ZombieDancer");
				TowerDefensePacketConfig packetConfigReadOnly2 = TowerDefenseManager.GetPacketConfigReadOnly("ZombieDancerCone");
				Check(GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(packetConfigReadOnly) && GodotObject.IsInstanceValid(packetConfigReadOnly2), "The regression must use the real Jackson and both backup-dancer packets.");
				if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(packetConfigReadOnly) || !GodotObject.IsInstanceValid(packetConfigReadOnly2))
				{
					goto end_IL_0103;
				}
				TowerDefenseCharacterOverride characterOverride = new TowerDefenseCharacterOverride
				{
					hitpointScale = 2.75,
					scale = 1.350000023841858,
					canMowerMove = true
				};
				packetConfigReadOnly._override = new TowerDefensePacketOverride
				{
					characterOverride = characterOverride
				};
				packetConfigReadOnly2._override = new TowerDefensePacketOverride
				{
					characterOverride = characterOverride
				};
				TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("ZombieDancer");
				Check(GodotObject.IsInstanceValid(packetConfig2?._override?.characterOverride) && Mathf.IsEqualApprox((float)packetConfig2._override.characterOverride.hitpointScale, 2.75f), "The active level packet must expose its backup-dancer CharacterOverride.");
				control = new JacksonDancerPacketOverrideRuntimeControlStub
				{
					Name = "JacksonDancerPacketOverrideRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(256f, 45f);
				manager.gridSize = new Vector2(80f, 98f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(JacksonGrid);
				jackson = packetConfig.Create(mapCellPlantPos, JacksonGrid) as TowerDefenseZombieJackson;
				Check(GodotObject.IsInstanceValid(jackson), "The real Jackson packet must create the production dance leader.");
				if (!GodotObject.IsInstanceValid(jackson))
				{
					goto end_IL_0103;
				}
				control.characterNode.AddChild(jackson, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				BugOverviewJacksonDancerPacketOverrideRuntimeTest bugOverviewJacksonDancerPacketOverrideRuntimeTest = this;
				int condition;
				if (jackson.IsNodeReady())
				{
					DancingComponent dancingComponent = jackson.dancingComponent;
					condition = ((dancingComponent != null && dancingComponent.Lifecycle == ComponentRuntimeLifecycle.Active) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewJacksonDancerPacketOverrideRuntimeTest.Check((byte)condition != 0, "The real Jackson DancingComponent must be active before summoning.");
				Check(Mathf.IsEqualApprox(jackson.dancingComponent.dancerWalkSpeedScaleMultiplier, 1f), "The production Jackson definition must retain its dancer speed multiplier.");
				TowerDefenseCharacterOverride towerDefenseCharacterOverride = new TowerDefenseCharacterOverride();
				towerDefenseCharacterOverride.propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig>
				{
					new TowerDefenseCharacterPropertyChangeConfig
					{
						propertyName = "dancerPacketName",
						value = "ZombieDancerCone"
					}
				};
				towerDefenseCharacterOverride.ExecuteCharacter(jackson);
				Check(jackson.dancerPacketName == "ZombieDancerCone" && jackson.dancingComponent.dancerPacketName == "ZombieDancerCone", "A ready Jackson CharacterOverride must update the effective dancer packet.");
				jackson.instance.hitpointScale = 1.6;
				jackson.transformPoint.Scale = Vector2.One * 0.8f;
				jackson.walkSpeedScale = 0.8;
				jackson.dancingComponent.dancerWalkSpeedScaleMultiplier = 0.5f;
				jackson.dancingComponent.SpawnDancer();
				foreach (TowerDefenseCharacter dancer in jackson.dancingComponent.dancerList)
				{
					if (dancer is TowerDefenseZombie item)
					{
						spawned.Add(item);
					}
				}
				Check(spawned.Count == 4, $"A central Jackson must summon four real backup dancers; got {spawned.Count}.");
				await WaitFrames(4);
				Check(spawned.TrueForAll((TowerDefenseZombie dancer) => GodotObject.IsInstanceValid(dancer?.packet) && dancer.packet.saveKey == "ZombieDancerCone"), "Every summoned dancer must come from the CharacterOverride-selected packet.");
				bool flag = true;
				bool flag2 = true;
				bool flag3 = true;
				bool flag4 = true;
				foreach (TowerDefenseZombie item2 in spawned)
				{
					flag &= GodotObject.IsInstanceValid(item2) && item2.IsNodeReady() && item2 is TowerDefenseZombieDancer;
					flag2 &= Mathf.IsEqualApprox((float)item2.instance.hitpointScale, 2.75f);
					flag3 &= item2.transformPoint.Scale.IsEqualApprox(Vector2.One * 1.35f);
					flag4 &= item2.canMowerMove;
				}
				Check(flag, "Every summon must be a ready production backup dancer.");
				Check(flag2, "The dancer packet hitpoint override must win over Jackson inheritance.");
				Check(flag3, "The dancer packet visual-scale override must win over Jackson inheritance.");
				Check(flag4, "Non-inherited CharacterOverride fields must execute for summoned dancers.");
				jackson.dancingComponent.OnWalkEntered();
				bool flag5 = true;
				foreach (TowerDefenseZombie item3 in spawned)
				{
					flag5 &= Mathf.IsEqualApprox((float)item3.walkSpeedScale, 0.4f);
				}
				Check(flag5, "The explicit Dancing definition speed multiplier must still control synchronized walking.");
				TowerDefenseZombie character = spawned[0];
				TowerDefenseCharacterSaveConfigCSharp save = new TowerDefenseCharacterSaveConfigCSharp();
				save.SaveCharacter(character);
				Check(Mathf.IsEqualApprox((float)save.instanceSave.GetValueOrDefault("hitpointScale", -1.0).AsDouble(), 2.75f) && Mathf.IsEqualApprox((float)save.transformPointScaleX, 1.35f), "Progress save must capture the overridden dancer attributes.");
				restoredDancer = save.InstantiateCharacterForRestore() as TowerDefenseZombie;
				Check(GodotObject.IsInstanceValid(restoredDancer), "Progress restore must recreate the real backup dancer.");
				if (GodotObject.IsInstanceValid(restoredDancer))
				{
					await WaitFrames(3);
					save.RestoreCharacter(restoredDancer);
					await WaitFrames(2);
					Check(Mathf.IsEqualApprox((float)restoredDancer.instance.hitpointScale, 2.75f) && restoredDancer.transformPoint.Scale.IsEqualApprox(Vector2.One * 1.35f) && restoredDancer.canMowerMove, "Progress restore must preserve all overridden dancer attributes.");
				}
				goto end_IL_00e4;
				end_IL_0103:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewJacksonDancerPacketOverrideRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00e4;
			}
			return;
			end_IL_00e4:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(restoredDancer) && !restoredDancer.IsQueuedForDeletion())
			{
				restoredDancer.QueueFree();
			}
			foreach (TowerDefenseZombie item4 in spawned)
			{
				if (GodotObject.IsInstanceValid(item4) && !item4.IsQueuedForDeletion())
				{
					item4.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(jackson) && !jackson.IsQueuedForDeletion())
			{
				jackson.QueueFree();
			}
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag6 = _failures == 0 && _checks == 17;
		GD.Print($"JACKSON_DANCER_PACKET_OVERRIDE_RESULT passed={flag6} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag6) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = new Vector2(256f, 45f),
				gridSize = new Vector2(80f, 98f)
			}
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("ZombieJackson", "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Packet/ZombieJackson.tres");
		RegisterPacket("ZombieDancer", "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Packet/ZombieDancer.tres");
		RegisterPacket("ZombieDancerCone", "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Packet/ZombieDancerCone.tres");
		RegisterCharacter("ZombieJackson", "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Scene/TowerDefenseZombieJackson.tscn");
		RegisterCharacter("ZombieDancer", "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/Scene/TowerDefenseZombieDancer.tscn");
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefensePacketConfig);
		instance.TOWERDEFENSE_PACKETS[key] = towerDefensePacketConfig;
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(packedScene);
		instance.TOWERDEFENSE_CHARCATERS[key] = packedScene;
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewJacksonDancerPacketOverrideRuntimeTest] " + message);
		}
	}
}
