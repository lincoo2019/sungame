using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewBowlingEasterEggImmediateExplosionRuntimeTest : Node
{
	private sealed class ScenarioSetupException : Exception
	{
	}

	private const string EggPacketPath = "res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Packet/PlantBowlingEasterEgg.tres";

	private const string EggScenePath = "res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Scene/TowerDefensePlantBowlingEasterEgg.tscn";

	private const string BoomPacketPath = "res://Asset/Anime/Character/Plant/Star/WallnutShooter/Packet/PlantBowlingWallnutShooterBoom.tres";

	private const string BoomScenePath = "res://Asset/Anime/Character/Plant/Star/WallnutShooter/Scene/BowlingBoom/TowerDefensePlantBowlingWallnutShooterBoom.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string FocusedBank = "BugOverviewBowlingEasterEggBoomOnly";

	private static readonly Vector2I CollisionGrid = new Vector2I(4, 3);

	private static readonly Vector2I DirectCollisionGrid = new Vector2I(5, 3);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private TowerDefensePacketBankData _previousBank;

	private bool _bankWasMissing;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BowlingEasterEggImmediateExplosionControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantBowlingWallnutShooterBoom directBoom = null;
		TowerDefenseZombie directZombie = null;
		TowerDefensePlantBowlingEasterEgg egg = null;
		TowerDefensePlantBowlingWallnutShooterBoom boom = null;
		TowerDefenseZombie zombie = null;
		string directBoomConfigName = null;
		bool directBoomBowlingRuntimeActive = false;
		bool directBoomExplodeRuntimeActive = false;
		bool directBoomDestroyObserved = false;
		int directBoomHitNumAtDestroy = -1;
		Vector2I directBoomGridAtDestroy = new Vector2I(-1, -1);
		Vector2 directBoomPositionAtDestroy = new Vector2(0f / 0f, 0f / 0f);
		new Vector2(0f / 0f, 0f / 0f);
		double directZombieHealthAfterExplosion = 0.0 / 0.0;
		int boomSpawnCount = 0;
		string boomConfigName = null;
		bool boomBowlingRuntimeActive = false;
		bool boomExplodeRuntimeActive = false;
		bool boomDestroyObserved = false;
		int boomHitNumAtDestroy = -1;
		Vector2I boomGridAtDestroy = new Vector2I(-1, -1);
		Vector2 boomPositionAtDestroy = new Vector2(0f / 0f, 0f / 0f);
		new Vector2(0f / 0f, 0f / 0f);
		double zombieHealthAfterExplosion = 0.0 / 0.0;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new ScenarioSetupException();
				}
				control = new BowlingEasterEggImmediateExplosionControlStub
				{
					Name = "BowlingEasterEggImmediateExplosionControl",
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				RegisterRealFixtures();
				control.characterNode.ChildEnteredTree += delegate(Node child)
				{
					TowerDefensePlantBowlingWallnutShooterBoom spawnedBoom = child as TowerDefensePlantBowlingWallnutShooterBoom;
					if (spawnedBoom != null)
					{
						boomSpawnCount++;
						boom = spawnedBoom;
						boomConfigName = spawnedBoom.config?.name;
						spawnedBoom.OnDestroy += delegate
						{
							BowlingComponent bowlingComponent3 = spawnedBoom.componentManager?.GetRuntime<BowlingComponent>();
							ExplodeComponent explodeComponent2 = spawnedBoom.componentManager?.GetRuntime<ExplodeComponent>();
							boomBowlingRuntimeActive = bowlingComponent3 != null && !bowlingComponent3.IsReleased;
							boomExplodeRuntimeActive = explodeComponent2 != null && !explodeComponent2.IsReleased;
							boomHitNumAtDestroy = bowlingComponent3?.hitNum ?? (-1);
							boomGridAtDestroy = spawnedBoom.gridPos;
							boomPositionAtDestroy = spawnedBoom.GlobalPosition;
							boomDestroyObserved = true;
						};
					}
				};
				TowerDefensePacketConfig eggPacket = LoadPacket("res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Packet/PlantBowlingEasterEgg.tres");
				TowerDefensePacketConfig boomPacket = LoadPacket("res://Asset/Anime/Character/Plant/Star/WallnutShooter/Packet/PlantBowlingWallnutShooterBoom.tres");
				TowerDefensePacketConfig zombiePacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				Check(GodotObject.IsInstanceValid(eggPacket) && eggPacket.saveKey == "PlantBowlingEasterEgg" && !eggPacket.plantUseCell && GodotObject.IsInstanceValid(boomPacket) && boomPacket.saveKey == "PlantBowlingWallnutShooterBoom" && !boomPacket.plantUseCell && GodotObject.IsInstanceValid(zombiePacket) && zombiePacket.saveKey == "ZombieNormal", "The scenario must load the authored Easter Egg, Boom, and ZombieNormal packets.");
				directZombie = zombiePacket?.Plant(DirectCollisionGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(directZombie) && directZombie.config?.name == "ZombieNormal", "The direct close-placement fixture must instantiate a real normal zombie.");
				if (!GodotObject.IsInstanceValid(directZombie))
				{
					throw new ScenarioSetupException();
				}
				directZombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(DirectCollisionGrid);
				directZombie.gridPos = DirectCollisionGrid;
				directZombie.instance.hitpointsBase = 10000.0;
				directZombie.instance.hitpointsSave = 10000.0;
				directZombie.instance.hitpoints = 10000.0;
				double directZombieHealthBeforeExplosion = directZombie.instance.hitpoints;
				Vector2 directCollisionWorldPosition = directZombie.GlobalPosition;
				control.isGameRunning = true;
				directBoom = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/WallnutShooter/Scene/BowlingBoom/TowerDefensePlantBowlingWallnutShooterBoom.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantBowlingWallnutShooterBoom>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(directBoom))
				{
					directBoomConfigName = directBoom.config?.name;
					directBoom.OnDestroy += delegate
					{
						BowlingComponent bowlingComponent3 = directBoom.componentManager?.GetRuntime<BowlingComponent>();
						ExplodeComponent explodeComponent2 = directBoom.componentManager?.GetRuntime<ExplodeComponent>();
						directBoomBowlingRuntimeActive = bowlingComponent3 != null && !bowlingComponent3.IsReleased;
						directBoomExplodeRuntimeActive = explodeComponent2 != null && !explodeComponent2.IsReleased;
						directBoomHitNumAtDestroy = bowlingComponent3?.hitNum ?? (-1);
						directBoomGridAtDestroy = directBoom.gridPos;
						directBoomPositionAtDestroy = directBoom.GlobalPosition;
						directBoomDestroyObserved = true;
					};
					directBoom.packet = boomPacket;
					directBoom.gridPos = DirectCollisionGrid;
					directBoom.cost = boomPacket.characterConfig.cost;
					directBoom.groundHeight = directZombie.groundHeight;
					directBoom.z = directBoom.groundHeight;
					directBoom.SetGlobalPositionForPhysicsFrame(directCollisionWorldPosition, Engine.GetPhysicsFrames());
					control.characterNode.AddChild(directBoom, forceReadableName: false, InternalMode.Disabled);
				}
				await WaitFrames(6);
				if (GodotObject.IsInstanceValid(directBoom) && !directBoomDestroyObserved)
				{
					BowlingComponent bowlingComponent = directBoom.componentManager?.GetRuntime<BowlingComponent>();
					ExplodeComponent explodeComponent = directBoom.componentManager?.GetRuntime<ExplodeComponent>();
					directBoomBowlingRuntimeActive = bowlingComponent != null && !bowlingComponent.IsReleased;
					directBoomExplodeRuntimeActive = explodeComponent != null && !explodeComponent.IsReleased;
					directBoom.SetGlobalPositionForPhysicsFrame(directCollisionWorldPosition, Engine.GetPhysicsFrames());
					directBoom.gridPos = DirectCollisionGrid;
					if (bowlingComponent != null && !bowlingComponent.IsReleased)
					{
						bowlingComponent.SetAlive(alive: true);
						bowlingComponent.isRoll = true;
						bowlingComponent.Refresh();
						bowlingComponent.ChangeCheck();
					}
				}
				await WaitFrames(6);
				if (GodotObject.IsInstanceValid(directZombie?.instance))
				{
					directZombieHealthAfterExplosion = directZombie.instance.hitpoints;
				}
				Check(directBoomConfigName == "PlantBowlingWallnutShooterBoom" && directBoomBowlingRuntimeActive && directBoomExplodeRuntimeActive, "The direct close-placement Boom must expose the authored Bowling and Explode runtimes.");
				Check(directBoomDestroyObserved && directBoomHitNumAtDestroy == 1 && directBoomGridAtDestroy == DirectCollisionGrid && directBoomPositionAtDestroy.IsEqualApprox(directCollisionWorldPosition), "The directly placed Boom must be consumed by its birth-cell collision instead of rebounding first.");
				Check(double.IsFinite(directZombieHealthAfterExplosion) && directZombieHealthAfterExplosion < directZombieHealthBeforeExplosion, "The directly placed Boom must apply ExplodeComponent damage to the close zombie.");
				if (GodotObject.IsInstanceValid(directBoom))
				{
					directBoom.QueueFree();
				}
				if (GodotObject.IsInstanceValid(directZombie))
				{
					directZombie.QueueFree();
				}
				directBoom = null;
				directZombie = null;
				boomSpawnCount = 0;
				boom = null;
				boomConfigName = null;
				boomBowlingRuntimeActive = false;
				boomExplodeRuntimeActive = false;
				boomDestroyObserved = false;
				boomHitNumAtDestroy = -1;
				boomGridAtDestroy = new Vector2I(-1, -1);
				boomPositionAtDestroy = new Vector2(0f / 0f, 0f / 0f);
				control.isGameRunning = false;
				await WaitFrames(3);
				egg = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Scene/TowerDefensePlantBowlingEasterEgg.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantBowlingEasterEgg>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(egg))
				{
					egg.packet = eggPacket;
					egg.gridPos = CollisionGrid;
					egg.cost = eggPacket.characterConfig.cost;
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(CollisionGrid);
					if (GodotObject.IsInstanceValid(mapCell))
					{
						egg.groundHeight = mapCell.GetGroundHeight();
					}
					egg.z = egg.groundHeight;
					egg.SetGlobalPositionForPhysicsFrame(TowerDefenseManager.GetMapCellPlantPos(CollisionGrid), Engine.GetPhysicsFrames());
					control.characterNode.AddChild(egg, forceReadableName: false, InternalMode.Disabled);
				}
				zombie = zombiePacket?.Plant(CollisionGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(egg) && egg.config?.name == "PlantBowlingEasterEgg" && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal", "The live fixture must instantiate the real Easter Egg and normal zombie scenes.");
				if (!GodotObject.IsInstanceValid(egg) || !GodotObject.IsInstanceValid(zombie))
				{
					throw new ScenarioSetupException();
				}
				egg.ProcessMode = ProcessModeEnum.Disabled;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				egg.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(CollisionGrid);
				egg.gridPos = CollisionGrid;
				zombie.GlobalPosition = egg.GlobalPosition;
				zombie.gridPos = CollisionGrid;
				zombie.instance.hitpointsBase = 10000.0;
				zombie.instance.hitpointsSave = 10000.0;
				zombie.instance.hitpoints = 10000.0;
				Vector2 collisionWorldPosition = egg.GlobalPosition;
				BowlingComponent bowlingComponent2 = egg.componentManager?.GetRuntime<BowlingComponent>();
				Check(bowlingComponent2 != null && !bowlingComponent2.IsReleased && bowlingComponent2.hitLineUse && bowlingComponent2.useParentHitBox && egg.HasHitBox, "The real Easter Egg must expose its authored same-line parent-HitBox Bowling runtime.");
				if (bowlingComponent2?.IsReleased ?? true)
				{
					throw new ScenarioSetupException();
				}
				double hitpoints = zombie.instance.hitpoints;
				bowlingComponent2.SetAlive(alive: true);
				bowlingComponent2.isRoll = true;
				bowlingComponent2.ChangeCheck();
				Check(bowlingComponent2.hitNum == 1 && zombie.instance.hitpoints < hitpoints && !bowlingComponent2.isRoll && egg.moveComponent.velocity.IsZeroApprox(), "The real parent-HitBox collision must damage the zombie and start the Easter Egg Open transformation.");
				Check(egg.sprite.clip == "Open", "The first collision must enter the authored Open clip before transformation.");
				double healthAfterEggHit = zombie.instance.hitpoints;
				double zombieHealthBeforeExplosion = healthAfterEggHit;
				zombie.ProcessMode = ProcessModeEnum.Inherit;
				control.isGameRunning = true;
				egg.packetBank = "BugOverviewBowlingEasterEggBoomOnly";
				egg.AnimeCompleted("Open");
				await WaitFrames(6);
				if (GodotObject.IsInstanceValid(zombie?.instance))
				{
					zombieHealthAfterExplosion = zombie.instance.hitpoints;
				}
				Check(boomSpawnCount == 1, "The real Easter Egg transformation must add exactly one Boom to the live character tree.");
				Check(boomConfigName == "PlantBowlingWallnutShooterBoom", "The transformed character must use the authored explosive bowling Wall-nut config.");
				Check(boomBowlingRuntimeActive && boomExplodeRuntimeActive, "The transformed real Boom must expose active Bowling and Explode runtimes.");
				Check(boomGridAtDestroy == CollisionGrid && boomPositionAtDestroy.IsEqualApprox(collisionWorldPosition), "The Boom must be born in the collision cell instead of moving to a later column first.");
				Check(boomDestroyObserved, "The Boom must be consumed by that first collision, not rebound once before exploding.");
				Check(boomHitNumAtDestroy == 1, "The transformed Boom must resolve exactly one bowling collision in the authored overlap.");
				Check(double.IsFinite(zombieHealthBeforeExplosion) && double.IsFinite(zombieHealthAfterExplosion) && Math.Abs(zombieHealthBeforeExplosion - healthAfterEggHit) <= 0.001 && zombieHealthAfterExplosion < zombieHealthBeforeExplosion && zombie.ProcessMode != ProcessModeEnum.Disabled, "The first overlap must skip bowling impact damage and apply only the deferred ExplodeComponent range damage.");
			}
			catch (ScenarioSetupException)
			{
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewBowlingEasterEggImmediateExplosionRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
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
			if (GodotObject.IsInstanceValid(directBoom))
			{
				directBoom.QueueFree();
			}
			if (GodotObject.IsInstanceValid(directZombie))
			{
				directZombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(egg))
			{
				egg.QueueFree();
			}
			if (GodotObject.IsInstanceValid(boom))
			{
				boom.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"BOWLING_EASTER_EGG_IMMEDIATE_EXPLOSION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = new Vector2(0f, 100f),
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantBowlingEasterEgg", "res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Packet/PlantBowlingEasterEgg.tres");
		RegisterPacket("PlantBowlingWallnutShooterBoom", "res://Asset/Anime/Character/Plant/Star/WallnutShooter/Packet/PlantBowlingWallnutShooterBoom.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantBowlingEasterEgg", "res://Asset/Anime/Character/Plant/Star/BowlingEasterEgg/Scene/TowerDefensePlantBowlingEasterEgg.tscn");
		RegisterCharacter("PlantBowlingWallnutShooterBoom", "res://Asset/Anime/Character/Plant/Star/WallnutShooter/Scene/BowlingBoom/TowerDefensePlantBowlingWallnutShooterBoom.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("BugOverviewBowlingEasterEggBoomOnly", out var value))
		{
			_previousBank = value;
		}
		else
		{
			_bankWasMissing = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["BugOverviewBowlingEasterEggBoomOnly"] = new TowerDefensePacketBankData
		{
			category = new Dictionary { ["White"] = new Array<string> { "PlantBowlingWallnutShooterBoom" } }
		};
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
		if (_bankWasMissing)
		{
			instance.TOWERDEFENSE_PACKETBANKS.Remove("BugOverviewBowlingEasterEggBoomOnly");
		}
		else
		{
			instance.TOWERDEFENSE_PACKETBANKS["BugOverviewBowlingEasterEggBoomOnly"] = _previousBank;
		}
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
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
			GD.PushError("[BugOverviewBowlingEasterEggImmediateExplosionRuntimeTest] " + message);
		}
	}
}
