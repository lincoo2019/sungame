using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewJalaPurifyPlantZombieRuntimeTest : Node
{
	private const string JalaPurifyScenePath = "res://Asset/Anime/Character/Plant/Star/JalaPurify/Scene/TowerDefensePlantJalaPurify.tscn";

	private const string PlantZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn";

	private const string RestoredPlantPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private const string RestoredPlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private static readonly Vector2I JalaGrid = new Vector2I(2, 3);

	private static readonly Vector2I ZombieGrid = new Vector2I(6, 3);

	private const double DurableHitpoints = 100000.0;

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
		JalaPurifyPlantZombieRuntimeControlStub control = null;
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
					goto end_IL_00e3;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewJalaPurifyPlantZombieRuntimeTest");
				RegisterRealFixtures();
				control = new JalaPurifyPlantZombieRuntimeControlStub
				{
					Name = "JalaPurifyPlantZombieRuntimeControl",
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
				TowerDefensePlantJalaPurify jalaPurify = Instantiate<TowerDefensePlantJalaPurify>("res://Asset/Anime/Character/Plant/Star/JalaPurify/Scene/TowerDefensePlantJalaPurify.tscn");
				TowerDefenseZombieNormalPeaShooterSingle plantZombie = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				TowerDefenseZombieNormalPeaShooterSingle secondPlantZombie = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				Check(GodotObject.IsInstanceValid(jalaPurify) && jalaPurify.config?.name == "PlantJalaPurify", "The reported real Purify Jalapeno scene must instantiate.");
				Check(GodotObject.IsInstanceValid(plantZombie) && plantZombie.config?.name == "ZombieNormalPeaShooterSingle", "The fixture must instantiate a real Peashooter plant-zombie scene.");
				Check(GodotObject.IsInstanceValid(secondPlantZombie) && secondPlantZombie.config?.name == "ZombieNormalPeaShooterSingle", "The overlap fixture must instantiate a second real Peashooter plant-zombie scene.");
				if (!GodotObject.IsInstanceValid(jalaPurify) || !GodotObject.IsInstanceValid(plantZombie) || !GodotObject.IsInstanceValid(secondPlantZombie))
				{
					goto end_IL_00e3;
				}
				jalaPurify.inGame = false;
				jalaPurify.editorPreviewMode = true;
				jalaPurify.gridPos = JalaGrid;
				jalaPurify.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(JalaGrid);
				plantZombie.inGame = true;
				plantZombie.editorPreviewMode = false;
				plantZombie.gridPos = ZombieGrid;
				plantZombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(ZombieGrid);
				secondPlantZombie.inGame = true;
				secondPlantZombie.editorPreviewMode = false;
				secondPlantZombie.gridPos = ZombieGrid;
				secondPlantZombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(ZombieGrid);
				control.characterNode.AddChild(jalaPurify, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(plantZombie, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(secondPlantZombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				ExplodeComponent explode = jalaPurify.componentManager?.GetRuntime<ExplodeComponent>("character.explode");
				Check(explode != null && !explode.IsReleased, "The real Purify Jalapeno must bind its ExplodeComponent runtime.");
				Check(explode != null && explode.explodeMethod == "Line" && explode.explodeJalaFireType == "PurifyFire" && Mathf.IsEqualApprox(explode.explodeJalaNum, 1800f) && explode.explodeJalaOffset.Count == 1 && explode.explodeJalaOffset[0] == 0, "The real Purify Jalapeno must retain its one-row 1800-damage PurifyFire configuration.");
				Check(explode != null && explode.explodeEvent.Count == 1 && explode.explodeEvent[0] is TowerDefenseCharacterEventPurify, "The resource-backed explosion must contain the Purify event.");
				if (explode == null || explode.IsReleased)
				{
					goto end_IL_00e3;
				}
				TowerDefenseCellInstance zombieCell = TowerDefenseManager.GetMapCell(ZombieGrid);
				Check(GodotObject.IsInstanceValid(zombieCell) && plantZombie.cell == zombieCell, "The real plant-zombie must be registered in its live map cell.");
				Check(manager.GetCharacterLineList(ZombieGrid.Y, fliterGraveStone: false).Contains(plantZombie), "The real plant-zombie must be discoverable by the line explosion registry.");
				plantZombie.instance.hitpoints = 100000.0;
				plantZombie.instance.hitpointsBase = 100000.0;
				secondPlantZombie.instance.hitpoints = 100000.0;
				secondPlantZombie.instance.hitpointsBase = 100000.0;
				double hitpointsBefore = plantZombie.GetCurrentHitPoint();
				Array<TowerDefenseCharacterEventBase> array = (Array<TowerDefenseCharacterEventBase>)TowerDefenseCharacter.CreateFireEventLists(explode.explodeJalaNum, explode.explodeEvent, new Array<TowerDefenseCharacterEventBase>())["event_list"];
				Check(array.Count == 2 && array[0] is TowerDefenseCharacterEventExplodeHurt && array[1] is TowerDefenseCharacterEventPurify, "The live jalapeno pipeline must apply ordinary fire damage before Purify.");
				TowerDefenseExplode.CreateExplodeLine(ZombieGrid.Y, array, new Array<TowerDefenseCharacter>(), jalaPurify.camp, -1);
				await WaitFrames(4);
				Check(hitpointsBefore > (double)explode.explodeJalaNum && plantZombie.isDestroy && secondPlantZombie.isDestroy, "Purify must immediately destroy both same-cell plant-zombies whose hitpoints far exceed the 1800 fire damage.");
				Check(!manager.GetCharacterLineList(ZombieGrid.Y, fliterGraveStone: false).Contains(plantZombie), "The purified plant-zombie must be removed from live battle targeting immediately.");
				Check(zombieCell.HasCharacter("PlantPeaShooterSingle"), "Purify must restore the matching real plant in the original cell.");
				TowerDefenseCharacter towerDefenseCharacter = null;
				int num = 0;
				foreach (TowerDefenseCharacter character in zombieCell.characterList)
				{
					if (GodotObject.IsInstanceValid(character) && character.config?.name == "PlantPeaShooterSingle")
					{
						num++;
						towerDefenseCharacter = character;
					}
				}
				Check(num == 1, $"Simultaneous same-cell Purify must restore exactly one plant instead of overlapping plants; restored={num}.");
				Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter is TowerDefensePlant, "The replacement must be the real playable Peashooter plant, not a test stub.");
				Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.IsSleep(), "The plant restored by Purify must be awake as documented.");
				goto end_IL_00cc;
				end_IL_00e3:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewJalaPurifyPlantZombieRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00cc;
			}
			return;
			end_IL_00cc:;
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"JALA_PURIFY_PLANT_ZOMBIE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum
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

	private static T Instantiate<T>(string scenePath) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres");
		RegisterCharacter("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
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
			GD.PushError("[BugOverviewJalaPurifyPlantZombieRuntimeTest] " + message);
		}
	}
}
