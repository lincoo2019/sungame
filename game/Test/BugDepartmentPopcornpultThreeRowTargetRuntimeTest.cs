using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentPopcornpultThreeRowTargetRuntimeTest : Node
{
	private const string PopcornPacketPath = "res://Asset/Anime/Character/Plant/Cover/Popcornpult/Packet/PlantPopcornpult.tres";

	private const string PopcornScenePath = "res://Asset/Anime/Character/Plant/Cover/Popcornpult/Scene/TowerDefensePlantPopcornpult.tscn";

	private const string CornpultPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Packet/PlantCornpult.tres";

	private const string CornpultScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Scene/TowerDefensePlantCornpult.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I PopcornGrid = new Vector2I(2, 3);

	private static readonly Vector2I UpperGrid = new Vector2I(5, 2);

	private static readonly Vector2I CenterGrid = new Vector2I(5, 3);

	private static readonly Vector2I LowerGrid = new Vector2I(5, 4);

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
		PopcornpultThreeRowTargetControlStub control = null;
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
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				TowerDefenseProjectileRegistry.Init();
				RegisterRealFixtures();
				control = new PopcornpultThreeRowTargetControlStub
				{
					Name = "PopcornpultThreeRowTargetControl",
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
				manager.gridSize = new Vector2(100f, 70f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePlant cornpult = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Packet/PlantCornpult.tres")?.Plant(PopcornGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				if (!GodotObject.IsInstanceValid(cornpult))
				{
					throw new InvalidOperationException("The real Cornpult cover base did not spawn.");
				}
				TowerDefensePlantPopcornpult popcorn = LoadPacket("res://Asset/Anime/Character/Plant/Cover/Popcornpult/Packet/PlantPopcornpult.tres")?.Plant(PopcornGrid, playAudio: false) as TowerDefensePlantPopcornpult;
				TowerDefenseZombie upper = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(UpperGrid, playAudio: false) as TowerDefenseZombie;
				TowerDefenseZombie center = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(CenterGrid, playAudio: false) as TowerDefenseZombie;
				TowerDefenseZombie lower = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(LowerGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(popcorn) && popcorn.config?.name == "PlantPopcornpult", "The production Popcornpult scene and config must instantiate.");
				Check(GodotObject.IsInstanceValid(upper) && GodotObject.IsInstanceValid(center) && GodotObject.IsInstanceValid(lower) && upper.config?.name == "ZombieNormal" && center.config?.name == "ZombieNormal" && lower.config?.name == "ZombieNormal", "All three target lanes must use real Normal zombie scenes.");
				FireComponent fire = popcorn?.componentManager?.GetRuntime<FireComponent>();
				Check(fire != null && !fire.IsReleased, "The real Popcornpult must expose its production FireComponent.");
				if (!GodotObject.IsInstanceValid(popcorn) || !GodotObject.IsInstanceValid(upper) || !GodotObject.IsInstanceValid(center) || !GodotObject.IsInstanceValid(lower) || (fire?.IsReleased ?? true))
				{
					throw new InvalidOperationException("The production three-row scenario is incomplete.");
				}
				popcorn.ProcessMode = ProcessModeEnum.Disabled;
				upper.ProcessMode = ProcessModeEnum.Disabled;
				center.ProcessMode = ProcessModeEnum.Disabled;
				lower.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				BugDepartmentPopcornpultThreeRowTargetRuntimeTest bugDepartmentPopcornpultThreeRowTargetRuntimeTest = this;
				Array<AabbShape2DResource> checkShapeResources = fire.checkShapeResources;
				bugDepartmentPopcornpultThreeRowTargetRuntimeTest.Check(checkShapeResources != null && checkShapeResources.Count == 3 && !fire.checkAllLine, "Popcornpult must use exactly three authored row shapes, not all-line targeting.");
				upper.instance.canBeCollection = true;
				lower.instance.canBeCollection = false;
				Check(fire.CheckAreaTarget(popcorn.instance.collisionFlags, checkInterval: false), "A real zombie one row above must activate Popcornpult targeting.");
				upper.instance.canBeCollection = false;
				lower.instance.canBeCollection = true;
				Check(fire.CheckAreaTarget(popcorn.instance.collisionFlags, checkInterval: false), "A real zombie one row below must activate Popcornpult targeting.");
				lower.instance.canBeCollection = false;
				center.instance.canBeCollection = true;
				Check(fire.CheckAreaTarget(popcorn.instance.collisionFlags, checkInterval: false), "A real zombie in Popcornpult's own row must activate targeting.");
				center.instance.canBeCollection = false;
				Check(!fire.CheckAreaTarget(popcorn.instance.collisionFlags, checkInterval: false), "Popcornpult must stop when all three target rows are empty.");
				upper.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(5, 1));
				upper.gridPos = new Vector2I(5, 1);
				upper.instance.canBeCollection = true;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				Check(!fire.CheckAreaTarget(popcorn.instance.collisionFlags, checkInterval: false), "A real zombie two rows above must remain outside the three-row target area.");
				upper.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(UpperGrid);
				upper.gridPos = UpperGrid;
				upper.instance.canBeCollection = false;
				center.instance.canBeCollection = true;
				lower.instance.canBeCollection = false;
				center.ProcessMode = ProcessModeEnum.Inherit;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				if (!(fire.fireCheckList[0].projectile is FireComponentProjectileWeight fireComponentProjectileWeight))
				{
					throw new InvalidOperationException("The production Popcornpult weighted projectile resource is unavailable.");
				}
				ulong num = 0uL;
				for (ulong num2 = 1uL; num2 <= 1024; num2++)
				{
					GD.Seed(num2);
					if (fireComponentProjectileWeight.GetProjectile()?.projectileName.ToString() == "Popcorn")
					{
						num = num2;
						break;
					}
				}
				if (num == 0L)
				{
					throw new InvalidOperationException("Could not select the authored Popcorn projectile deterministically.");
				}
				double upperHitpointsBefore = upper.instance.hitpoints;
				double centerHitpointsBefore = center.instance.hitpoints;
				double lowerHitpointsBefore = lower.instance.hitpoints;
				BulletField instance = BulletField.Instance;
				int num3 = (GodotObject.IsInstanceValid(instance) ? instance.ActiveCount : (-1));
				fire.runningCheck = fire.fireCheckList[0];
				GD.Seed(num);
				fire.AttackEntered();
				if (fireComponentProjectileWeight.readyProjectile?.projectileName.ToString() != "Popcorn")
				{
					throw new InvalidOperationException("The deterministic production volley did not retain the Popcorn projectile.");
				}
				fire.Fire();
				Check(GodotObject.IsInstanceValid(instance) && instance.ActiveCount > num3, $"The production Fire call must spawn a real Popcorn projectile into BulletField; fieldValid={GodotObject.IsInstanceValid(instance)}, before={num3}, after={(GodotObject.IsInstanceValid(instance) ? instance.ActiveCount : (-1))}, fireAlive={fire.alive}, insideBattlefield={popcorn.IsInsideComponentBattlefield}, inGame={popcorn.inGame}, pos={popcorn.GlobalPosition}.");
				upper.instance.canBeCollection = true;
				lower.instance.canBeCollection = true;
				upper.ProcessMode = ProcessModeEnum.Inherit;
				center.ProcessMode = ProcessModeEnum.Inherit;
				lower.ProcessMode = ProcessModeEnum.Inherit;
				for (int frame = 0; frame < 300; frame++)
				{
					if (!Mathf.IsEqualApprox((float)upper.instance.hitpoints, (float)upperHitpointsBefore) && !Mathf.IsEqualApprox((float)center.instance.hitpoints, (float)centerHitpointsBefore) && !Mathf.IsEqualApprox((float)lower.instance.hitpoints, (float)lowerHitpointsBefore))
					{
						break;
					}
					await WaitFrames(1);
				}
				Check(upper.instance.hitpoints < upperHitpointsBefore, $"The real upper-row Normal zombie must take the same production Popcorn volley damage; hp stayed {upper.instance.hitpoints}/{upperHitpointsBefore}.");
				Check(center.instance.hitpoints < centerHitpointsBefore, $"The real same-row Normal zombie must take the same production Popcorn volley damage; hp stayed {center.instance.hitpoints}/{centerHitpointsBefore}.");
				Check(lower.instance.hitpoints < lowerHitpointsBefore, $"The real lower-row Normal zombie must take the same production Popcorn volley damage; hp stayed {lower.instance.hitpoints}/{lowerHitpointsBefore}.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentPopcornpultThreeRowTargetRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"POPCORNPULT_THREE_ROW_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("PlantPopcornpult", "res://Asset/Anime/Character/Plant/Cover/Popcornpult/Packet/PlantPopcornpult.tres");
		RegisterPacket("PlantCornpult", "res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Packet/PlantCornpult.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantPopcornpult", "res://Asset/Anime/Character/Plant/Cover/Popcornpult/Scene/TowerDefensePlantPopcornpult.tscn");
		RegisterCharacter("PlantCornpult", "res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Scene/TowerDefensePlantCornpult.tscn");
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
			GD.PushError("[BugDepartmentPopcornpultThreeRowTargetRuntimeTest] " + message);
		}
	}
}
