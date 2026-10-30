using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest : Node
{
	private const string FumeSeaShroomPacketPath = "res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Packet/PlantFumeSeaShroom.tres";

	private const string FumeSeaShroomScenePath = "res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Scene/TowerDefensePlantFumeSeaShroom.tscn";

	private const string SnorklePacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres";

	private const string SnorkleScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn";

	private static readonly Vector2I PlantGrid = new Vector2I(2, 2);

	private static readonly Vector2I ZombieGrid = new Vector2I(5, 2);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		FumeSeaShroomSnorkleTargetControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantFumeSeaShroom plant = null;
		TowerDefenseZombieSnorkle snorkle = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00f0;
				}
				RegisterRealFixtures();
				control = new FumeSeaShroomSnorkleTargetControlStub
				{
					Name = "FumeSeaShroomSnorkleTargetControl",
					isGameRunning = false,
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				plant = LoadPacket("res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Packet/PlantFumeSeaShroom.tres")?.Plant(PlantGrid, playAudio: false) as TowerDefensePlantFumeSeaShroom;
				snorkle = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres")?.Plant(ZombieGrid, playAudio: false) as TowerDefenseZombieSnorkle;
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == "PlantFumeSeaShroom", "The scenario must instantiate the real Fume Sea-shroom scene and packet.");
				Check(GodotObject.IsInstanceValid(snorkle) && snorkle.config?.name == "ZombieSnorkle", "The scenario must instantiate the real Snorkle Zombie scene and packet.");
				if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(snorkle))
				{
					goto end_IL_00f0;
				}
				AttackComponent attack = plant.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				BugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest bugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest = this;
				int condition;
				if (attack != null && !attack.IsReleased)
				{
					Array<TowerDefenseCharacterEventBase> eventList = attack.eventList;
					condition = ((eventList != null && eventList.Count == 1) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest.Check((byte)condition != 0, "The real Fume Sea-shroom must bind its authored Attack runtime and hurt event.");
				if (attack == null || attack.IsReleased)
				{
					goto end_IL_00f0;
				}
				int ground = 1;
				int gridItem = 8;
				int num = 32;
				Check((plant.instance.collisionFlags & ground) != 0 && (plant.instance.collisionFlags & num) != 0, "Fume Sea-shroom must retain normal ground targeting and add underwater targeting.");
				Check(attack.eventList[0] is TowerDefenseCharacterEventHurt towerDefenseCharacterEventHurt && (towerDefenseCharacterEventHurt.collisionFlags & num) != 0, "The authored hurt event must remain capable of damaging an underwater target.");
				plant.inGame = true;
				plant.componentAlive = true;
				plant.componentRunning = false;
				plant.instance.sleep = false;
				attack.SetAlive(alive: true);
				snorkle.inGame = true;
				snorkle.inWater = true;
				snorkle.instance.maskFlags = num;
				await WaitFrames(2);
				Check(plant.CanCollision(snorkle.instance.maskFlags), "The production character collision gate must accept submerged Snorkle.");
				TowerDefenseCharacter target = attack.GetTarget();
				Check(target == snorkle && attack.target == snorkle, "The production Attack target query must acquire submerged Snorkle in the same row.");
				Check(attack.CanAttackOnce() && attack.target == snorkle, "The production immediate attack check must keep submerged Snorkle as its target.");
				Check(attack.GetTargetList().Contains(snorkle), "The production area target list must include submerged Snorkle.");
				double hitpointsBefore = snorkle.instance.hitpoints;
				attack.AttackEventExecute();
				await WaitFrames(2);
				Check(snorkle.instance.hitpoints < hitpointsBefore, "Fume Sea-shroom's authored attack event must damage the acquired submerged Snorkle.");
				snorkle.instance.maskFlags = ground | gridItem;
				attack.target = null;
				await WaitFrames(2);
				Check(attack.CanAttackOnce() && attack.target == snorkle, "Adding underwater targeting must preserve normal surfaced-zombie acquisition.");
				goto end_IL_00d5;
				end_IL_00f0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00d5;
			}
			return;
			end_IL_00d5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(plant) && !plant.IsQueuedForDeletion())
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(snorkle) && !snorkle.IsQueuedForDeletion())
			{
				snorkle.QueueFree();
			}
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"BUG_DEPARTMENT_FUME_SEA_SHROOM_SNORKLE_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig config = new TowerDefenseCellConfig
				{
					gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					}
				};
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(config);
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantFumeSeaShroom", "res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Packet/PlantFumeSeaShroom.tres");
		RegisterPacket("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres");
		RegisterCharacter("PlantFumeSeaShroom", "res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Scene/TowerDefensePlantFumeSeaShroom.tscn");
		RegisterCharacter("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn");
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
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
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
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
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
			GD.PushError("[BugDepartmentFumeSeaShroomSnorkleTargetRuntimeTest] " + message);
		}
	}
}
