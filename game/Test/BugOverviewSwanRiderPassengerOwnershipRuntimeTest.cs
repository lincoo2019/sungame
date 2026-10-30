using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSwanRiderPassengerOwnershipRuntimeTest : Node
{
	private const int ExpectedChecks = 49;

	private const string SwanPacketPath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres";

	private const string SwanScenePath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn";

	private const string DuckPacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres";

	private const string DuckScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Scene/TowerDefenseZombieDuckRider.tscn";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I CarryGrid = new Vector2I(5, 2);

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
		SwanRiderPassengerOwnershipRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 12;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d7;
				}
				RegisterRealFixtures();
				control = new SwanRiderPassengerOwnershipRuntimeControlStub
				{
					Name = "SwanRiderPassengerOwnershipRuntimeControl",
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
				TowerDefenseZombieSwanRider firstSwan = LoadPacket("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres")?.Plant(CarryGrid, playAudio: false) as TowerDefenseZombieSwanRider;
				TowerDefenseZombieNormal passenger = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(CarryGrid, playAudio: false) as TowerDefenseZombieNormal;
				if (GodotObject.IsInstanceValid(firstSwan))
				{
					firstSwan.ProcessMode = ProcessModeEnum.Disabled;
				}
				if (GodotObject.IsInstanceValid(passenger))
				{
					passenger.ProcessMode = ProcessModeEnum.Always;
				}
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(firstSwan) && GodotObject.IsInstanceValid(passenger), "A real first SwanRider and normal-zombie passenger must spawn.");
				if (!GodotObject.IsInstanceValid(firstSwan) || !GodotObject.IsInstanceValid(passenger))
				{
					throw new InvalidOperationException("Initial rider fixtures did not spawn.");
				}
				int groundFlag = 1;
				int airFlag = 2;
				int passengerOriginalMaskFlags = passenger.instance.maskFlags;
				firstSwan.GlobalPosition = new Vector2(500f, firstSwan.GlobalPosition.Y);
				passenger.GlobalPosition = firstSwan.GlobalPosition;
				firstSwan.gridPos = CarryGrid;
				passenger.gridPos = CarryGrid;
				firstSwan.instance.collisionFlags = groundFlag;
				firstSwan.instance.maskFlags = groundFlag;
				Check((passenger.instance.collisionFlags & groundFlag) != 0 && (passenger.instance.maskFlags & groundFlag) != 0, "The real passenger must begin on the authored ground collision channel.");
				await WaitFrames(1);
				control.isGameRunning = true;
				firstSwan.BatchUpdate(0.0);
				await WaitFrames(2);
				firstSwan.BatchUpdate(0.0);
				Check(firstSwan.carryCharacter == passenger, "The first real SwanRider must acquire the overlapping passenger.");
				Check(passenger.riderCarryOwner == firstSwan, "The acquired passenger must expose the first SwanRider as its unique owner.");
				Check(passenger.isPause, "The owned passenger must enter the shared paused carry state.");
				Check(passenger.instance.maskFlags == airFlag, $"A Swan-owned passenger must use the air-only target mask; mask={passenger.instance.maskFlags}.");
				Check((passenger.instance.collisionFlags & groundFlag) != 0, "Carrying changes the passenger target mask, not its collision source flags.");
				control.isGameRunning = false;
				TowerDefenseZombieSwanRider secondSwan = LoadPacket("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres")?.Plant(CarryGrid, playAudio: false) as TowerDefenseZombieSwanRider;
				if (GodotObject.IsInstanceValid(secondSwan))
				{
					secondSwan.ProcessMode = ProcessModeEnum.Disabled;
				}
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(secondSwan), "A real later-placed SwanRider must spawn.");
				if (!GodotObject.IsInstanceValid(secondSwan))
				{
					throw new InvalidOperationException("Second SwanRider fixture did not spawn.");
				}
				Check(!GodotObject.IsInstanceValid(secondSwan.carryCharacter), "The later SwanRider must begin without a passenger.");
				secondSwan.GlobalPosition = passenger.GlobalPosition;
				secondSwan.gridPos = CarryGrid;
				secondSwan.instance.collisionFlags = groundFlag;
				secondSwan.instance.maskFlags = groundFlag;
				await WaitFrames(1);
				control.isGameRunning = true;
				secondSwan.BatchUpdate(0.0);
				await WaitFrames(1);
				Check(!GodotObject.IsInstanceValid(secondSwan.carryCharacter), "The later SwanRider must not attach a passenger already owned by the first SwanRider.");
				Check(passenger.riderCarryOwner == firstSwan, "A rejected later pickup must preserve the first SwanRider owner.");
				Check(passenger.isPause, "A rejected later pickup must not unpause the first SwanRider passenger.");
				Check(passenger.instance.maskFlags == airFlag, "A rejected later pickup must preserve the first SwanRider air mask.");
				firstSwan.GlobalPosition = new Vector2(460f, firstSwan.GlobalPosition.Y);
				secondSwan.GlobalPosition = new Vector2(720f, secondSwan.GlobalPosition.Y);
				firstSwan.BatchUpdate(0.0);
				secondSwan.BatchUpdate(0.0);
				CheckClose(passenger.GlobalPosition.X, (double)firstSwan.GlobalPosition.X + 30.0, "The later SwanRider update must not overwrite the passenger position written by its real owner.");
				Dictionary data = new Dictionary
				{
					["carryCharacterNodeName"] = passenger.Name,
					["carryOriginalMaskFlags"] = groundFlag
				};
				secondSwan.ImportVariantSave(data);
				Check(!GodotObject.IsInstanceValid(secondSwan.carryCharacter), "A conflicting progress relation must not create a second SwanRider owner.");
				Check(passenger.riderCarryOwner == firstSwan, "A conflicting progress relation must preserve the established owner.");
				Check(passenger.isPause && passenger.instance.maskFlags == airFlag, "A rejected progress restore must not release or ground the established passenger.");
				secondSwan.carryCharacter = passenger;
				bool flag = TowerDefenseZombieCarryRelation.Release(secondSwan, ref secondSwan.carryCharacter, secondSwan.groundHeight);
				Check(!flag, "A SwanRider without reverse ownership must not release another rider's passenger.");
				Check(!GodotObject.IsInstanceValid(secondSwan.carryCharacter), "A rejected stale SwanRider relation must clear only its local pointer.");
				Check(passenger.riderCarryOwner == firstSwan, "A stale SwanRider release must preserve the true owner.");
				Check(passenger.isPause, "A stale SwanRider release must preserve the passenger pause state.");
				Check(passenger.instance.maskFlags == airFlag, "A stale SwanRider release must preserve the passenger air mask.");
				control.isGameRunning = false;
				TowerDefenseZombieDuckRider duck = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres")?.Plant(CarryGrid, playAudio: false) as TowerDefenseZombieDuckRider;
				if (GodotObject.IsInstanceValid(duck))
				{
					duck.ProcessMode = ProcessModeEnum.Disabled;
				}
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(duck), "A real DuckRider must spawn for the shared cross-role ownership check.");
				if (!GodotObject.IsInstanceValid(duck))
				{
					throw new InvalidOperationException("DuckRider fixture did not spawn.");
				}
				duck.GlobalPosition = passenger.GlobalPosition;
				duck.gridPos = CarryGrid;
				duck.instance.collisionFlags = groundFlag;
				duck.instance.maskFlags = groundFlag;
				await WaitFrames(1);
				control.isGameRunning = true;
				duck.BatchUpdate(0.0);
				Check(!GodotObject.IsInstanceValid(duck.carryCharacter), "DuckRider must share the same exclusive passenger claim as SwanRider.");
				Check(passenger.riderCarryOwner == firstSwan, "A rejected cross-role pickup must preserve the first SwanRider owner.");
				duck.carryCharacter = passenger;
				bool flag2 = TowerDefenseZombieCarryRelation.Release(duck, ref duck.carryCharacter, duck.groundHeight);
				Check(!flag2, "A DuckRider without reverse ownership must not release the SwanRider passenger.");
				Check(!GodotObject.IsInstanceValid(duck.carryCharacter), "A rejected stale DuckRider relation must clear only its local pointer.");
				Check(passenger.riderCarryOwner == firstSwan && passenger.isPause, "A stale DuckRider release must preserve the true owner and pause state.");
				duck.GlobalPosition = new Vector2(820f, duck.GlobalPosition.Y);
				firstSwan.Hypnoses();
				await WaitFrames(2);
				Check(!GodotObject.IsInstanceValid(firstSwan.carryCharacter), "The true first SwanRider owner must release its passenger on hypnosis.");
				Check(!GodotObject.IsInstanceValid(passenger.riderCarryOwner), "The true owner release must clear the reverse passenger relation.");
				Check(!passenger.isPause, "The true owner release must unpause the passenger.");
				Check(passenger.instance.maskFlags == passengerOriginalMaskFlags, "The true SwanRider owner release must restore the original ground mask.");
				secondSwan.GlobalPosition = passenger.GlobalPosition;
				secondSwan.gridPos = passenger.gridPos;
				secondSwan.BatchUpdate(0.0);
				await WaitFrames(2);
				Check(secondSwan.carryCharacter == passenger, "The later SwanRider must be able to claim the passenger after the true owner releases it.");
				Check(passenger.riderCarryOwner == secondSwan, "A newly claimed passenger must expose the later SwanRider as its sole owner.");
				Check(passenger.isPause, "The newly claimed passenger must re-enter the paused carry state.");
				Check(passenger.instance.maskFlags == airFlag, "The newly claimed SwanRider passenger must return to the air-only target mask.");
				secondSwan.QueueFree();
				await WaitFrames(3);
				Check(!GodotObject.IsInstanceValid(secondSwan), "The later SwanRider must leave the scene tree during the exit fallback check.");
				Check(!GodotObject.IsInstanceValid(passenger.riderCarryOwner), "SwanRider scene exit must clear its passenger ownership.");
				Check(!passenger.isPause, "SwanRider scene exit must unpause its passenger.");
				Check(passenger.instance.maskFlags == passengerOriginalMaskFlags, "SwanRider scene exit must restore its passenger original target mask.");
				duck.GlobalPosition = passenger.GlobalPosition;
				duck.gridPos = passenger.gridPos;
				duck.BatchUpdate(0.0);
				await WaitFrames(2);
				Check(duck.carryCharacter == passenger, "DuckRider must be able to claim the passenger after SwanRider exits.");
				Check(passenger.riderCarryOwner == duck, "The shared relation must record DuckRider as the new sole owner.");
				Check(passenger.isPause, "The DuckRider-owned passenger must enter the shared paused state.");
				duck.BatchUpdate(0.0);
				CheckClose(passenger.groundHeight, duck.z + 40.0, "DuckRider must retain its authored carry height under the shared owner contract.");
				duck.QueueFree();
				await WaitFrames(3);
				Check(!GodotObject.IsInstanceValid(duck), "DuckRider must leave the scene tree during its exit fallback check.");
				Check(!GodotObject.IsInstanceValid(passenger.riderCarryOwner), "DuckRider scene exit must clear its passenger ownership.");
				Check(!passenger.isPause, "DuckRider scene exit must unpause its passenger.");
				goto end_IL_0097;
				end_IL_00d7:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSwanRiderPassengerOwnershipRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0097;
			}
			return;
			end_IL_0097:;
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
		bool flag3 = _failures == 0 && _checks == 49;
		GD.Print($"SWAN_RIDER_PASSENGER_OWNERSHIP_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
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
		RegisterPacket("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres");
		RegisterPacket("ZombieDuckRider", "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn");
		RegisterCharacter("ZombieDuckRider", "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Scene/TowerDefenseZombieDuckRider.tscn");
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

	private void CheckClose(double actual, double expected, string message)
	{
		Check(Math.Abs(actual - expected) < 0.01, $"{message} expected={expected}, actual={actual}.");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSwanRiderPassengerOwnershipRuntimeTest] " + message);
		}
	}
}
