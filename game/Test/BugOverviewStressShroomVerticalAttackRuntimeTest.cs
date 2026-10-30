using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewStressShroomVerticalAttackRuntimeTest : Node
{
	private const string StressPacketPath = "res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Packet/PlantStressShroom.tres";

	private const string StressScenePath = "res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Scene/TowerDefensePlantStressShroom.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I StressGrid = new Vector2I(3, 3);

	private static readonly Vector2I UpperGrid = new Vector2I(3, 2);

	private static readonly Vector2I LowerGrid = new Vector2I(3, 4);

	private static readonly Vector2I SameRowGrid = new Vector2I(6, 3);

	private static readonly Vector2I DiagonalGrid = new Vector2I(6, 2);

	private static readonly Vector2 ScenarioGridSize = new Vector2(100f, 70f);

	private int _checks;

	private int _failures;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		StressShroomVerticalAttackControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new StressShroomVerticalAttackControlStub
				{
					Name = "StressShroomVerticalAttackControl",
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
				manager.gridSize = ScenarioGridSize;
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum, manager.gridSize);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePlantStressShroom stress = LoadPacket("res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Packet/PlantStressShroom.tres")?.Plant(StressGrid, playAudio: false) as TowerDefensePlantStressShroom;
				TowerDefenseZombie upper = SpawnZombie(UpperGrid);
				TowerDefenseZombie lower = SpawnZombie(LowerGrid);
				TowerDefenseZombie sameRow = SpawnZombie(SameRowGrid);
				TowerDefenseZombie diagonal = SpawnZombie(DiagonalGrid);
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(stress) && stress.config?.name == "PlantStressShroom", "The real production Stress Fume-shroom packet and scene must instantiate.");
				Check(IsRealNormalZombie(upper) && IsRealNormalZombie(lower) && IsRealNormalZombie(sameRow) && IsRealNormalZombie(diagonal), "Every target fixture must use the real production Normal zombie.");
				AttackComponent attack = stress?.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				Check(attack != null && !attack.IsReleased, "The real Stress Fume-shroom must expose its production AttackComponent.");
				if (!GodotObject.IsInstanceValid(stress) || !IsRealNormalZombie(upper) || !IsRealNormalZombie(lower) || !IsRealNormalZombie(sameRow) || !IsRealNormalZombie(diagonal) || (attack?.IsReleased ?? true))
				{
					throw new InvalidOperationException("The production vertical-attack scenario is incomplete.");
				}
				stress.ProcessMode = ProcessModeEnum.Disabled;
				upper.ProcessMode = ProcessModeEnum.Disabled;
				lower.ProcessMode = ProcessModeEnum.Disabled;
				sameRow.ProcessMode = ProcessModeEnum.Disabled;
				diagonal.ProcessMode = ProcessModeEnum.Disabled;
				stress.WakeUp();
				upper.instance.canBeCollection = true;
				lower.instance.canBeCollection = true;
				sameRow.instance.canBeCollection = true;
				diagonal.instance.canBeCollection = true;
				control.isGameRunning = true;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				Check(attack.CheckAreaShapeCount == 6 && !attack.checkLine && attack.checkEachShape, "Stress Fume-shroom must use six exact segments without same-row filtering.");
				Vector2 mapGridSize = manager.GetMapGridSize();
				float expected = mapGridSize.X * 6.5f;
				float expected2 = mapGridSize.Y * 6.5f;
				Check(SegmentLengthMatches(attack, 0, expected) && SegmentLengthMatches(attack, 3, expected), "Both production horizontal segments must retain the six-cell horizontal range.");
				Check(SegmentLengthMatches(attack, 1, expected2) && SegmentLengthMatches(attack, 2, expected2) && SegmentLengthMatches(attack, 4, expected2) && SegmentLengthMatches(attack, 5, expected2), "All four production vertical segments must retain the six-cell vertical range.");
				attack.target = null;
				attack.checkIntrevalNow = 0;
				Check(attack.CanAttack(), "Real upper/lower zombies must activate Stress Fume-shroom targeting.");
				List<TowerDefenseCharacter> targetList = attack.GetTargetList();
				Check(targetList.Contains(upper), "The real zombie one row above must be selected by a vertical segment.");
				Check(targetList.Contains(lower), "The real zombie one row below must be selected by a vertical segment.");
				Check(targetList.Contains(sameRow), "The horizontal attack contract must remain intact.");
				Check(!targetList.Contains(diagonal), "A diagonal zombie inside the broad AABB must be excluded by exact segment filtering.");
				double upperBefore = upper.instance.hitpoints;
				double lowerBefore = lower.instance.hitpoints;
				double sameRowBefore = sameRow.instance.hitpoints;
				double diagonalBefore = diagonal.instance.hitpoints;
				stress.Attack();
				await WaitFrames(2);
				Check(Mathf.IsEqualApprox((float)(upperBefore - upper.instance.hitpoints), 20f), "The production attack event must deal 20 damage upward at level one.");
				Check(Mathf.IsEqualApprox((float)(lowerBefore - lower.instance.hitpoints), 20f), "The production attack event must deal 20 damage downward at level one.");
				Check(Mathf.IsEqualApprox((float)(sameRowBefore - sameRow.instance.hitpoints), 20f), "The production attack event must preserve same-row damage.");
				Check(Mathf.IsEqualApprox((float)(diagonalBefore - diagonal.instance.hitpoints), 0f), "The production attack event must not damage a diagonal zombie.");
				lower.instance.canBeCollection = false;
				sameRow.instance.canBeCollection = false;
				diagonal.instance.canBeCollection = false;
				attack.target = null;
				attack.checkIntrevalNow = 0;
				Check(attack.CanAttack() && attack.target == upper, "Stress Fume-shroom must first cache the real zombie on its upward segment.");
				upper.gridPos = DiagonalGrid;
				upper.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(DiagonalGrid);
				await WaitFrames(2);
				Check(attack.TryGetCheckAreaWorldRect(out var worldRect) && AabbShapeUtil.Intersects(worldRect, upper.WorldHitRect) && !attack.GetTargetList().Contains(upper), "The moved cached zombie must remain inside the broad union while leaving every exact segment.");
				lower.instance.canBeCollection = true;
				Check(!attack.CanAttack() && !GodotObject.IsInstanceValid(attack.target), "A cached zombie that moved off every exact segment must be invalidated.");
				attack.checkIntrevalNow = 0;
				Check(attack.CanAttack() && attack.target == lower, "Stress Fume-shroom must reacquire the remaining real zombie on its downward segment.");
				double movedUpperBefore = upper.instance.hitpoints;
				double relockedLowerBefore = lower.instance.hitpoints;
				stress.Attack();
				await WaitFrames(2);
				Check(Mathf.IsEqualApprox((float)(movedUpperBefore - upper.instance.hitpoints), 0f) && Mathf.IsEqualApprox((float)(relockedLowerBefore - lower.instance.hitpoints), 20f), "The moved former target must take no damage while the reacquired cross target is hit.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewStressShroomVerticalAttackRuntimeTest] Unexpected exception: {value}");
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
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"BUG_OVERVIEW_STRESS_SHROOM_VERTICAL_ATTACK_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool SegmentLengthMatches(AttackComponent attack, int index, float expected)
	{
		if (attack.TryGetCheckAreaSegmentEnd(index, out var endpoint))
		{
			return Mathf.IsEqualApprox(Mathf.Abs(endpoint.X), expected);
		}
		return false;
	}

	private static bool IsRealNormalZombie(TowerDefenseZombie zombie)
	{
		if (GodotObject.IsInstanceValid(zombie))
		{
			return zombie.config?.name == "ZombieNormal";
		}
		return false;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static TowerDefenseZombie SpawnZombie(Vector2I grid)
	{
		return LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(grid, playAudio: false) as TowerDefenseZombie;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum, Vector2 gridSize)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = gridSize
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantStressShroom", "res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Packet/PlantStressShroom.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantStressShroom", "res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Scene/TowerDefensePlantStressShroom.tscn");
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
			GD.PushError("[BugOverviewStressShroomVerticalAttackRuntimeTest] " + message);
		}
	}
}
