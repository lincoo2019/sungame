using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewIceSquashCrushDamageRuntimeTest : Node
{
	private const string IceSquashPacketPath = "res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Packet/PlantIceSquash.tres";

	private const string IceSquashScenePath = "res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Scene/TowerDefensePlantIceSquash.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

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
		IceSquashCrushDamageRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0086;
				}
				RegisterRealFixtures();
				control = new IceSquashCrushDamageRuntimeControlStub
				{
					Name = "IceSquashCrushDamageRuntimeControl",
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
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Packet/PlantIceSquash.tres");
				TowerDefensePacketConfig zombiePacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				TowerDefensePlantIceSquash iceSquash = towerDefensePacketConfig?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlantIceSquash;
				await WaitFrames(5);
				TowerDefenseZombie zombie = zombiePacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(5);
				BugOverviewIceSquashCrushDamageRuntimeTest bugOverviewIceSquashCrushDamageRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(iceSquash))
				{
					SquashComponent squashComponent = iceSquash.squashComponent;
					condition = ((squashComponent != null && !squashComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewIceSquashCrushDamageRuntimeTest.Check((byte)condition != 0, "The real Ice Squash must expose its live SquashComponent.");
				BugOverviewIceSquashCrushDamageRuntimeTest bugOverviewIceSquashCrushDamageRuntimeTest2 = this;
				int condition2;
				if (GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(zombie.instance))
				{
					BuffComponent buff = zombie.buff;
					condition2 = ((buff != null && !buff.IsReleased) ? 1 : 0);
				}
				else
				{
					condition2 = 0;
				}
				bugOverviewIceSquashCrushDamageRuntimeTest2.Check((byte)condition2 != 0, "The real normal zombie must expose health and Buff runtimes.");
				if (!GodotObject.IsInstanceValid(iceSquash) || (iceSquash.squashComponent?.IsReleased ?? true) || !GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(zombie.instance) || (zombie.buff?.IsReleased ?? true))
				{
					goto end_IL_0086;
				}
				SquashComponent squashComponent2 = iceSquash.squashComponent;
				TowerDefenseCharacterEventHurt towerDefenseCharacterEventHurt = squashComponent2.eventList.OfType<TowerDefenseCharacterEventHurt>().FirstOrDefault();
				Check(towerDefenseCharacterEventHurt != null && Math.Abs(towerDefenseCharacterEventHurt.num - 1800.0) < 0.001, "The real Ice Squash definition must retain its 1800 crush event.");
				zombie.instance.hitpointsBase = 3000.0;
				zombie.instance.hitpointsSave = 3000.0;
				zombie.instance.hitpoints = 3000.0;
				iceSquash.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				double before = zombie.instance.hitpoints;
				squashComponent2.JumpDownSmashAction();
				double afterCold = zombie.instance.hitpoints;
				Check(zombie.buff.BuffHas("Frozen"), "Ice Squash landing must still apply the authored Frozen effect.");
				Check(Math.Abs(before - afterCold - 20.0) < 0.001, "The immediate global cold pulse must deal exactly its distinct 20 damage.");
				await WaitFrames(3);
				double hitpoints = zombie.instance.hitpoints;
				Check(Math.Abs(afterCold - hitpoints - 1800.0) < 0.001, "The deferred local landing explosion must independently deal 1800 crush damage.");
				Check(Math.Abs(before - hitpoints - 1820.0) < 0.001, "One real Ice Squash landing must combine cold and crush damage.");
				Check(!zombie.die && !zombie.nearDie && hitpoints > 0.0, "The high-health control zombie must survive so both damage phases remain observable.");
				goto end_IL_006f;
				end_IL_0086:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewIceSquashCrushDamageRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006f;
			}
			return;
			end_IL_006f:;
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
		GD.Print($"ICE_SQUASH_CRUSH_DAMAGE_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("PlantIceSquash", "res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Packet/PlantIceSquash.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantIceSquash", "res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Scene/TowerDefensePlantIceSquash.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
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
			GD.PushError("[BugOverviewIceSquashCrushDamageRuntimeTest] " + message);
		}
	}
}
