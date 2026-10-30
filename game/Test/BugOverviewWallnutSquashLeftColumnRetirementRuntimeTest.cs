using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewWallnutSquashLeftColumnRetirementRuntimeTest : Node
{
	private const string BowlingKey = "PlantWallnutSquashBowling";

	private const string ZombieKey = "ZombieNormal";

	private const string BowlingPacketPath = "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Packet/PlantWallnutSquashBowling.tres";

	private const string BowlingScenePath = "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Scene/TowerDefensePlantBowlingWallnutSquash.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I CollisionGrid = new Vector2I(1, 3);

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		WallnutSquashLeftColumnRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantBowlingWallnutSquash bowlingPlant = null;
		TowerDefenseZombie zombie = null;
		try
		{
			int num;
			_ = num - 1;
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(manager.characterRegistry) && GodotObject.IsInstanceValid(ResourceManager.Instance), "Manager, character registry, and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(manager.characterRegistry) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					return;
				}
				control = new WallnutSquashLeftColumnRuntimeControlStub
				{
					Name = "WallnutSquashLeftColumnRuntimeControl",
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
				RegisterCharacter("PlantWallnutSquashBowling", "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Scene/TowerDefensePlantBowlingWallnutSquash.tscn");
				RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Packet/PlantWallnutSquashBowling.tres");
				TowerDefensePacketConfig towerDefensePacketConfig2 = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.saveKey == "PlantWallnutSquashBowling" && !towerDefensePacketConfig.plantUseCell && GodotObject.IsInstanceValid(towerDefensePacketConfig2) && towerDefensePacketConfig2.saveKey == "ZombieNormal", "The fixture must use the authored no-cell Bowling Wallnut Squash packet and real normal-zombie packet.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(towerDefensePacketConfig2))
				{
					return;
				}
				bowlingPlant = towerDefensePacketConfig.Plant(CollisionGrid, playAudio: false) as TowerDefensePlantBowlingWallnutSquash;
				zombie = towerDefensePacketConfig2.Plant(CollisionGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(bowlingPlant) && bowlingPlant.config?.name == "PlantWallnutSquashBowling" && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal", "The real first-column bowling plant and zombie scenes must instantiate through packet placement.");
				if (!GodotObject.IsInstanceValid(bowlingPlant) || !GodotObject.IsInstanceValid(zombie))
				{
					return;
				}
				bowlingPlant.gridPos = CollisionGrid;
				zombie.gridPos = CollisionGrid;
				zombie.GlobalPosition = bowlingPlant.GlobalPosition;
				zombie.instance.hitpointsBase = 1000000.0;
				zombie.instance.hitpointsSave = 1000000.0;
				zombie.instance.hitpoints = 1000000.0;
				await WaitFrames(2);
				BowlingComponent bowling = bowlingPlant.componentManager?.GetRuntime<BowlingComponent>();
				SquashComponent squash = bowlingPlant.componentManager?.GetRuntime<SquashComponent>();
				CharacterMoveComponent movement = bowlingPlant.componentManager?.GetRuntime<CharacterMoveComponent>();
				AttackComponent zombieAttack = zombie.componentManager?.GetRuntime<AttackComponent>();
				BugOverviewWallnutSquashLeftColumnRetirementRuntimeTest bugOverviewWallnutSquashLeftColumnRetirementRuntimeTest = this;
				int condition;
				if (bowling != null && !bowling.IsReleased)
				{
					if (squash != null && !squash.IsReleased)
					{
						if (movement != null && !movement.IsReleased)
						{
							condition = ((zombieAttack != null && !zombieAttack.IsReleased) ? 1 : 0);
							goto IL_0676;
						}
					}
				}
				condition = 0;
				goto IL_0676;
				IL_0676:
				bugOverviewWallnutSquashLeftColumnRetirementRuntimeTest.Check((byte)condition != 0, "The real Bowling, Squash, movement, and zombie attack runtimes must be active.");
				if ((bowling?.IsReleased ?? true) || (squash?.IsReleased ?? true) || (movement?.IsReleased ?? true) || (zombieAttack?.IsReleased ?? true))
				{
					return;
				}
				TowerDefenseCellInstance collisionCell = TowerDefenseManager.GetMapCell(CollisionGrid);
				List<TowerDefenseCharacter> activeCharacters = manager.characterRegistry.GetActiveCharacters();
				Check(activeCharacters.Contains(bowlingPlant) && bowlingPlant.IsInGroup("Character"), "Before collision, the rolling plant must be registered for opposing projectile targeting.");
				Check(GodotObject.IsInstanceValid(collisionCell) && !collisionCell.characterList.Contains(bowlingPlant), "The authored plantUseCell=false bowling plant must not occupy the leftmost cell.");
				Check(bowlingPlant.instance.hitpoints > 0.0 && bowlingPlant.instance.canBeCollection && !bowlingPlant.instance.invincible && (bowlingPlant.targetRegistrationComponent?.canProjectileCheck ?? false) && bowlingPlant.HasHitBox, "Before impact, the live roller must retain health, collision, and projectile-target eligibility.");
				Check(!zombieAttack.checkBowling && !zombieAttack.GetTargetList().Contains(bowlingPlant), "A normal zombie must keep the authored rule that it cannot bite a rolling bowling plant.");
				bowling.SetAlive(alive: true);
				bowling.isRoll = true;
				movement.velocity = new Vector2(200f, 0f);
				double zombieHealthBefore = zombie.instance.hitpoints;
				bowling.ChangeCheck();
				await WaitFrames(2);
				Check(bowling.hitNum == 1 && zombie.instance.hitpoints < zombieHealthBefore, "The overlapping real first-column scenes must execute exactly one bowling collision and damage event.");
				Check(squash.IsRunning() && squash.target == zombie && !bowling.Alive && !bowling.isRoll && movement.velocity.IsZeroApprox(), "Impact must transfer ownership from Bowling to Squash and stop rolling immediately.");
				Check(!manager.characterRegistry.GetActiveCharacters().Contains(bowlingPlant), "Entering the Squash Ready state must immediately retire the plant from the combat target registry.");
				BugOverviewWallnutSquashLeftColumnRetirementRuntimeTest bugOverviewWallnutSquashLeftColumnRetirementRuntimeTest2 = this;
				int condition2;
				if (!bowlingPlant.instance.canBeCollection && bowlingPlant.instance.invincible)
				{
					TargetRegistrationComponent targetRegistrationComponent = bowlingPlant.targetRegistrationComponent;
					if (targetRegistrationComponent != null && !targetRegistrationComponent.canProjectileCheck)
					{
						condition2 = ((!bowlingPlant.HasHitBox) ? 1 : 0);
						goto IL_09bc;
					}
				}
				condition2 = 0;
				goto IL_09bc;
				IL_09bc:
				bugOverviewWallnutSquashLeftColumnRetirementRuntimeTest2.Check((byte)condition2 != 0, "The visible cleanup interval must be non-collectible, invincible, projectile-ineligible, and hit-box free.");
				Check(bowlingPlant.instance.hitpoints > 0.0 && !collisionCell.characterList.Contains(bowlingPlant), "A positive health value during the visual jump must not recreate cell occupancy or combat eligibility.");
				Check(!zombieAttack.GetTargetList().Contains(bowlingPlant), "The colliding zombie must not acquire the retired visual as a bite target.");
				Check(await WaitUntilInvalid(bowlingPlant, 240), "The first-column collision visual must finish its jump/smash cleanup and leave the scene tree.");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.instance.hitpoints > 0.0, "The durable real zombie fixture must survive long enough to prove plant cleanup independently.");
				goto end_IL_0138;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[WallnutSquashLeftColumnRetirement] Unexpected exception: {value}");
				goto end_IL_0138;
			}
			end_IL_0138:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bowlingPlant))
			{
				bowlingPlant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await WaitFrames(3);
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
			RestoreCharacters();
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"WALLNUT_SQUASH_LEFT_COLUMN_RETIREMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
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

	private void RestoreCharacters()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
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

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			isNight = true,
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
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

	private async Task<bool> WaitUntilInvalid(GodotObject value, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (!GodotObject.IsInstanceValid(value))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		return !GodotObject.IsInstanceValid(value);
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
			GD.PushError("[WallnutSquashLeftColumnRetirement] " + message);
		}
	}
}
