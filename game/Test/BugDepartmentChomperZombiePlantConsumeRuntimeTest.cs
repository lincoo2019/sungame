using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentChomperZombiePlantConsumeRuntimeTest : Node
{
	private const string ChomperZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres";

	private const string ChomperZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Chomper/TowerDefenseZombieNormalChomper.tscn";

	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

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
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugDepartmentChomperZombiePlantConsumeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlant wallnut = null;
		TowerDefenseZombieNormalChomper chomperZombie = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					return;
				}
				RegisterRealFixtures();
				control = new BugDepartmentChomperZombiePlantConsumeControlStub
				{
					Name = "ChomperZombiePlantConsumeControl",
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
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
				TowerDefensePacketConfig chomperPacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres");
				wallnut = towerDefensePacketConfig?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(5);
				chomperZombie = chomperPacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombieNormalChomper;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", "The reported overlap must contain the real Wall-nut scene and packet.");
				Check(GodotObject.IsInstanceValid(chomperZombie) && chomperZombie.config?.name == "ZombieNormalChomper", "The reported attacker must be the real Chomper Zombie scene and packet.");
				if (!GodotObject.IsInstanceValid(wallnut) || !GodotObject.IsInstanceValid(chomperZombie))
				{
					return;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ScenarioGrid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(wallnut) && wallnut.cell == mapCell && chomperZombie.cell == mapCell, "The real plant and Chomper Zombie must occupy the same reported map cell.");
				ChomperComponent chomper = chomperZombie.chomperComponent;
				BugDepartmentChomperZombiePlantConsumeRuntimeTest bugDepartmentChomperZombiePlantConsumeRuntimeTest = this;
				int condition;
				if (chomper != null && !chomper.IsReleased)
				{
					AttackComponent attackComponent = chomper.attackComponent;
					if (attackComponent != null && !attackComponent.IsReleased)
					{
						condition = ((chomper.StateMachine?.IsInitialized ?? false) ? 1 : 0);
						goto IL_05ad;
					}
				}
				condition = 0;
				goto IL_05ad;
				IL_05ad:
				bugDepartmentChomperZombiePlantConsumeRuntimeTest.Check((byte)condition != 0, "The real Chomper and its authored attack/state runtimes must be active.");
				Check(GodotObject.IsInstanceValid(chomper?.sprite) && chomper.sprite.HasClip(chomper.biteStartAnimeClips) && chomper.sprite.HasClip(chomper.chewAnimeClips) && chomper.sprite.HasClip(chomper.swallowAnimeClips), "The real plant head must expose Bite, Chew, and Swallow clips.");
				if (chomper == null || chomper.IsReleased || (chomper.attackComponent?.IsReleased ?? true))
				{
					goto end_IL_0105;
				}
				chomperZombie.chewTime = 0.1;
				chomper.chewTime = 0.1f;
				chomperZombie.GlobalPosition = wallnut.GlobalPosition;
				chomperZombie.gridPos = ScenarioGrid;
				chomperZombie.groundRight = 10000.0;
				int biteStarts = 0;
				int chewBegins = 0;
				int swallowOvers = 0;
				chomper.OnBiteStart += delegate
				{
					biteStarts++;
				};
				chomper.OnChewBegin += delegate
				{
					chewBegins++;
				};
				chomper.OnSwallowOver += delegate
				{
					swallowOvers++;
				};
				double hitpointsBefore = wallnut.instance.hitpoints;
				control.isGameRunning = true;
				chomperZombie.timeScale = 8.0;
				chomperZombie.Walk();
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(chomperZombie) && GodotObject.IsInstanceValid(wallnut) && chomper.attackComponent.target == wallnut, 90), "The Chomper Zombie must naturally acquire the real plant already in its spawn cell.");
				Check(await WaitUntil(() => biteStarts > 0, 90) && chomper.StateMachine?.CurrentStateHandle?.StableId == "chomper.attack" && chomper.sprite.clip == chomper.biteStartAnimeClips, $"Natural target acquisition must enter the authored Bite animation; state={chomper.StateMachine?.CurrentStateHandle?.StableId}, clip={chomper.sprite?.clip}.");
				Check(await WaitUntil(() => !GodotObject.IsInstanceValid(wallnut) || wallnut.die || wallnut.nearDie || wallnut.isDestroy, 180), "The Bite animation event must consume the real plant instead of repeating forever.");
				Check(!GodotObject.IsInstanceValid(wallnut) || wallnut.instance.hitpoints < hitpointsBefore, "The production bite event must actually damage the real plant target.");
				Check(!GodotObject.IsInstanceValid(wallnut) || wallnut.isChomp || wallnut.isDestroy, "The consumed real plant must enter the authored chomp/destruction path.");
				Check(await WaitUntil(() => chewBegins > 0, 120), "After consuming the plant, the real Chomper head must enter Chew.");
				Check(await WaitUntil(() => swallowOvers > 0 && chomperZombie.CurrentStateHandle?.StableId == "zombie.walk", 240), $"The Chomper Zombie must complete Swallow and resume walking; state={chomperZombie.CurrentStateHandle?.StableId}, swallowOvers={swallowOvers}.");
				await WaitFrames(30);
				Check(biteStarts == 1 && chewBegins == 1 && swallowOvers == 1, $"A consumed same-cell plant must produce one complete bite cycle, not an endless loop; bite={biteStarts}, chew={chewBegins}, swallow={swallowOvers}.");
				Check((chomperZombie.groundMoveComponent?.Alive ?? false) && !chomperZombie.componentRunning, "After the plant is gone, the real zombie must leave component mode and move normally.");
				goto end_IL_00da;
				end_IL_0105:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentChomperZombiePlantConsume] Unexpected exception: {value}");
				goto end_IL_00da;
			}
			return;
			end_IL_00da:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(chomperZombie) && !chomperZombie.IsQueuedForDeletion())
			{
				chomperZombie.QueueFree();
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 16;
		GD.Print($"CHOMPER_ZOMBIE_PLANT_CONSUME_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
		RegisterPacket("ZombieNormalChomper", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres");
		RegisterCharacter("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
		RegisterCharacter("ZombieNormalChomper", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Chomper/TowerDefenseZombieNormalChomper.tscn");
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

	private async Task<bool> WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return condition();
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
			GD.PushError("[BugDepartmentChomperZombiePlantConsume] " + message);
		}
	}
}
