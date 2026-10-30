using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSquashLadderHypnotizedTargetRuntimeTest : Node
{
	private const string SquashPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres";

	private const string SquashScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string SunFlowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunFlowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string PumpkinPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres";

	private const string PumpkinScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Scene/TowerDefensePlantPumpkin.tscn";

	private const string LadderPacketPath = "res://Asset/Anime/Character/Item/Ladder/Packet/ItemLadder.tres";

	private const string LadderScenePath = "res://Asset/Anime/Character/Item/Ladder/Scene/TowerDefenseItemLadder.tscn";

	private static readonly Vector2I ScenarioGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		SquashLadderHypnotizedTargetRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_009e;
				}
				RegisterRealFixtures();
				control = new SquashLadderHypnotizedTargetRuntimeControlStub
				{
					Name = "SquashLadderHypnotizedTargetRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D node2D = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = node2D;
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
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres");
				TowerDefensePacketConfig pumpkinPacket = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres");
				TowerDefensePacketConfig ladderPacket = LoadPacket("res://Asset/Anime/Character/Item/Ladder/Packet/ItemLadder.tres");
				TowerDefensePacketConfig zombiePacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				TowerDefensePacketConfig squashPacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres");
				TowerDefensePlant sunFlower = towerDefensePacketConfig?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefensePlant pumpkin = pumpkinPacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefenseCharacter ladder = ladderPacket?.Plant(ScenarioGrid, playAudio: false);
				await WaitFrames(4);
				TowerDefenseZombie hypnotizedZombie = zombiePacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(4);
				hypnotizedZombie?.Hypnoses();
				await WaitFrames(4);
				TowerDefenseZombieNormalSquash squash = squashPacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombieNormalSquash;
				await WaitFrames(5);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ScenarioGrid);
				Check(GodotObject.IsInstanceValid(sunFlower) && GodotObject.IsInstanceValid(pumpkin), "The real SunFlower and Pumpkin must be planted in the scenario cell.");
				Check(GodotObject.IsInstanceValid(ladder) && GodotObject.IsInstanceValid(mapCell?.characterLadder) && mapCell.characterLadder == ladder, "The real ladder must be registered on the Pumpkin cell.");
				Check(GodotObject.IsInstanceValid(hypnotizedZombie) && hypnotizedZombie.instance.hypnoses && hypnotizedZombie.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "The real normal zombie must be hypnotized into the Plant camp.");
				Check(GodotObject.IsInstanceValid(squash) && squash.attackComponent2 != null, "The real Squash zombie must expose its secondary jump attack component.");
				if (!GodotObject.IsInstanceValid(sunFlower) || !GodotObject.IsInstanceValid(hypnotizedZombie) || !GodotObject.IsInstanceValid(squash) || squash.attackComponent2 == null)
				{
					goto end_IL_009e;
				}
				squash.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				AttackComponent attackComponent = squash.attackComponent2;
				attackComponent.target = hypnotizedZombie;
				Check(attackComponent.CanAttack() && attackComponent.target == hypnotizedZombie, "A ladder on the shared cell must not hide a hypnotized Zombie target from Squash.");
				attackComponent.target = sunFlower;
				Check(!attackComponent.CanAttack() && !GodotObject.IsInstanceValid(attackComponent.target), "Control: a laddered ordinary Plant must still be filtered from Squash targeting.");
				sunFlower.instance.canBeCollection = false;
				pumpkin.instance.canBeCollection = false;
				ladder.instance.canBeCollection = false;
				Check(attackComponent.TryRetargetImmediately() && attackComponent.target == hypnotizedZombie, "Immediate target acquisition must also keep the hypnotized Zombie on a laddered cell.");
				goto end_IL_007b;
				end_IL_009e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSquashLadderHypnotizedTargetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_007b;
			}
			return;
			end_IL_007b:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"SQUASH_LADDER_HYPNOTIZED_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
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

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("ZombieNormalSquash", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterPacket("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres");
		RegisterPacket("PlantPumpkin", "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres");
		RegisterPacket("ItemLadder", "res://Asset/Anime/Character/Item/Ladder/Packet/ItemLadder.tres");
		RegisterCharacter("ZombieNormalSquash", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterCharacter("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
		RegisterCharacter("PlantPumpkin", "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Scene/TowerDefensePlantPumpkin.tscn");
		RegisterCharacter("ItemLadder", "res://Asset/Anime/Character/Item/Ladder/Scene/TowerDefenseItemLadder.tscn");
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
			GD.PushError("[BugOverviewSquashLadderHypnotizedTargetRuntimeTest] " + message);
		}
	}
}
