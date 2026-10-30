using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSwanRiderPlantAttackRuntimeTest : Node
{
	private const string SwanPacketPath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres";

	private const string SwanScenePath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn";

	private const string PeaPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private const string PeaScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private const string CactusPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Packet/PlantCactus.tres";

	private const string CactusScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I PeaGrid = new Vector2I(1, 2);

	private static readonly Vector2I CactusGrid = new Vector2I(2, 2);

	private static readonly Vector2I SwanGrid = new Vector2I(5, 2);

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
		SwanRiderPlantAttackRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 8;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00b6;
				}
				RegisterRealFixtures();
				control = new SwanRiderPlantAttackRuntimeControlStub
				{
					Name = "SwanRiderPlantAttackRuntimeControl",
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
				TowerDefensePlantPeaShooterSingle pea = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres")?.Plant(PeaGrid, playAudio: false) as TowerDefensePlantPeaShooterSingle;
				await WaitFrames(4);
				TowerDefensePlantCactus cactus = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Cactus/Packet/PlantCactus.tres")?.Plant(CactusGrid, playAudio: false) as TowerDefensePlantCactus;
				await WaitFrames(4);
				TowerDefenseZombieSwanRider swan = LoadPacket("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres")?.Plant(SwanGrid, playAudio: false) as TowerDefenseZombieSwanRider;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(pea) && GodotObject.IsInstanceValid(cactus) && GodotObject.IsInstanceValid(swan), "Real PeaShooterSingle, Cactus, and SwanRider characters must spawn.");
				if (!GodotObject.IsInstanceValid(pea) || !GodotObject.IsInstanceValid(cactus) || !GodotObject.IsInstanceValid(swan))
				{
					goto end_IL_00b6;
				}
				pea.ProcessMode = ProcessModeEnum.Disabled;
				cactus.ProcessMode = ProcessModeEnum.Disabled;
				swan.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				FireComponent peaFire = pea.componentManager.GetRuntime<FireComponent>("character.fire");
				FireComponent runtime = cactus.componentManager.GetRuntime<FireComponent>("character.fire");
				Check(peaFire != null && !peaFire.IsReleased && runtime != null && !runtime.IsReleased, "Real PeaShooterSingle and Cactus FireComponents must be active.");
				int groundFlag = 1;
				int airFlag = 2;
				Check((swan.instance.maskFlags & groundFlag) != 0 && (swan.instance.maskFlags & airFlag) == 0, $"SwanRider must begin as a ground target; mask={swan.instance.maskFlags}.");
				Check(swan.instance.canBeCollection && !swan.instance.invincible && swan.IsHitBoxEnabled, "Grounded SwanRider must be collectible, vulnerable, and expose an enabled hit box.");
				Check(peaFire.CanFireCheckOnce(null) && peaFire.firstCharacter == swan, "A real PeaShooterSingle must acquire grounded SwanRider through its production ray check.");
				double hitpoints = swan.instance.hitpoints;
				swan.Hurt(100.0, playSplatAudio: false);
				Check(swan.instance.hitpoints < hitpoints, $"Grounded SwanRider must take plant damage; before={hitpoints}, after={swan.instance.hitpoints}.");
				swan.FlyEntered();
				Check(swan.instance.maskFlags == airFlag && swan.instance.collisionFlags == airFlag, $"Flying SwanRider must switch to the authored air-only collision channel; collision={swan.instance.collisionFlags}, mask={swan.instance.maskFlags}.");
				Check(!peaFire.CanFireCheckOnce(null), "Ground-only PeaShooterSingle must stop targeting SwanRider after takeoff by design.");
				Check(runtime.CanFireCheckOnce(null, airFlag) && runtime.firstCharacter == swan, "A real Cactus air check must continue acquiring flying SwanRider.");
				double hitpoints2 = swan.instance.hitpoints;
				swan.Hurt(100.0, playSplatAudio: false);
				Check(swan.instance.hitpoints < hitpoints2, "Flying SwanRider must remain damageable when an air-capable plant reaches it.");
				TowerDefenseZombieNormal passenger = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(SwanGrid, playAudio: false) as TowerDefenseZombieNormal;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(passenger), "A real normal-zombie passenger must spawn in the SwanRider carry area.");
				if (!GodotObject.IsInstanceValid(passenger))
				{
					throw new InvalidOperationException("Normal-zombie passenger fixture did not spawn.");
				}
				passenger.ProcessMode = ProcessModeEnum.Always;
				passenger.GlobalPosition = swan.GlobalPosition;
				passenger.gridPos = SwanGrid;
				Check((passenger.instance.maskFlags & groundFlag) != 0 && (passenger.instance.maskFlags & airFlag) == 0, $"The passenger fixture must start as a ground target; mask={passenger.instance.maskFlags}.");
				swan.instance.collisionFlags = groundFlag;
				swan.instance.maskFlags = groundFlag;
				swan.isFly = false;
				swan.isFlying = false;
				swan.BatchUpdate(0.0);
				await WaitFrames(1);
				Check(swan.carryCharacter == passenger, "The real SwanRider carry scan must attach the overlapping normal zombie.");
				swan.FlyEntered();
				Check(passenger.instance.maskFlags == airFlag, $"A carried Swan passenger must become an air-only target; mask={passenger.instance.maskFlags}.");
				Check(!peaFire.CanFireCheckOnce(null), "Ground-only PeaShooterSingle must not acquire the carried Swan passenger.");
				Array<TowerDefenseCharacterEventBase> hurtEvents = new Array<TowerDefenseCharacterEventBase>
				{
					new TowerDefenseCharacterEventHurt
					{
						num = 25.0,
						playSplatAudio = false
					}
				};
				double passengerBeforeGroundExplosion = passenger.instance.hitpoints;
				TowerDefenseExplode.CreateExplode(passenger.GlobalPosition, new Vector2(0.8f, 0.8f), hurtEvents, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, groundFlag);
				await WaitFrames(2);
				Check(Math.Abs(passenger.instance.hitpoints - passengerBeforeGroundExplosion) < 0.001, "A production ground-only explosion must not damage the airborne Swan passenger.");
				double passengerBeforeAirExplosion = passenger.instance.hitpoints;
				TowerDefenseExplode.CreateExplode(passenger.GlobalPosition, new Vector2(0.8f, 0.8f), hurtEvents, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, airFlag);
				await WaitFrames(2);
				Check(passenger.instance.hitpoints < passengerBeforeAirExplosion, "A production air-only explosion must still damage the airborne Swan passenger.");
				swan.Hypnoses();
				await WaitFrames(1);
				Check(!GodotObject.IsInstanceValid(swan.carryCharacter), "Hypnotizing SwanRider must release its carried passenger.");
				Check((passenger.instance.maskFlags & groundFlag) != 0 && (passenger.instance.maskFlags & airFlag) == 0, $"A released Swan passenger must restore its original ground target mask; mask={passenger.instance.maskFlags}.");
				double passengerBeforeReleasedGroundExplosion = passenger.instance.hitpoints;
				TowerDefenseExplode.CreateExplode(passenger.GlobalPosition, new Vector2(0.8f, 0.8f), hurtEvents, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, groundFlag);
				await WaitFrames(2);
				Check(passenger.instance.hitpoints < passengerBeforeReleasedGroundExplosion, "A released Swan passenger must become damageable by ground-only explosions again.");
				goto end_IL_0087;
				end_IL_00b6:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSwanRiderPlantAttackRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0087;
			}
			return;
			end_IL_0087:;
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
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"SWAN_RIDER_PLANT_ATTACK_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres");
		RegisterPacket("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres");
		RegisterPacket("PlantCactus", "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Packet/PlantCactus.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn");
		RegisterCharacter("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
		RegisterCharacter("PlantCactus", "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn");
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
			GD.PushError("[BugOverviewSwanRiderPlantAttackRuntimeTest] " + message);
		}
	}
}
