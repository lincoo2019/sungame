using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGravebusterGPlantingStabilityRuntimeTest : Node
{
	private const string GravebusterPacketPath = "res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Packet/PlantGravebusterG.tres";

	private const string GravebusterScenePath = "res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Scene/TowerDefensePlantGravebusterG.tscn";

	private const string GravestonePacketPath = "res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres";

	private const string GravestoneScenePath = "res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn";

	private const string NormalCraterPacketPath = "res://Asset/Anime/Character/Crater/CraterN/Packet/CraterN.tres";

	private const string NormalCraterScenePath = "res://Asset/Anime/Character/Crater/CraterN/Scene/TowerDefenseCraterN.tscn";

	private const string GoldenCraterPacketPath = "res://Asset/Anime/Character/Crater/CraterG/Packet/CraterG.tres";

	private const string GoldenCraterScenePath = "res://Asset/Anime/Character/Crater/CraterG/Scene/TowerDefenseCraterG.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private static readonly Vector2I CraterTestGrid = new Vector2I(5, 3);

	private static readonly Vector2I GroundTestGrid = new Vector2I(6, 3);

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
		GravebusterGPlantingStabilityControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseGravestone gravestone = null;
		TowerDefensePlantGravebusterG gravebuster = null;
		TowerDefenseCrater normalCrater = null;
		TowerDefensePlantGravebusterG craterGravebuster = null;
		TowerDefenseCrater goldenCrater = null;
		TowerDefensePlantGravebusterG groundGravebuster = null;
		TowerDefenseCrater groundGoldenCrater = null;
		try
		{
			_ = 8;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new GravebusterGPlantingStabilityControlStub
				{
					Name = "GravebusterGPlantingStabilityControl",
					isGameRunning = true,
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
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(mapCell), "The planting regression must use a real flat map cell.");
				gravestone = LoadPacket("res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres")?.Plant(TestGrid, playAudio: false) as TowerDefenseGravestone;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(gravestone), "The real default gravestone must be planted first.");
				if (!GodotObject.IsInstanceValid(gravestone))
				{
					throw new InvalidOperationException("The real gravestone did not spawn.");
				}
				gravebuster = LoadPacket("res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Packet/PlantGravebusterG.tres")?.Plant(TestGrid, playAudio: false) as TowerDefensePlantGravebusterG;
				Check(GodotObject.IsInstanceValid(gravebuster), "The real Treasure Gravebuster scene must be planted over the gravestone.");
				if (!GodotObject.IsInstanceValid(gravebuster))
				{
					throw new InvalidOperationException("The real Treasure Gravebuster did not spawn.");
				}
				Check(gravebuster.config?.name == "PlantGravebusterG", "The planted character must use the production Treasure Gravebuster config.");
				Check(GodotObject.IsInstanceValid(gravebuster.sprite), "The real Treasure Gravebuster must expose its authored animation sprite.");
				if (!GodotObject.IsInstanceValid(gravebuster.sprite))
				{
					throw new InvalidOperationException("The production Gravebuster runtime is incomplete.");
				}
				float authoredY = gravebuster.sprite.Position.Y;
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				GravebusterComponent component = gravebuster.componentManager?.GetRuntime<GravebusterComponent>();
				Check(component != null && !component.IsReleased, "The real Treasure Gravebuster must expose its Gravebuster runtime.");
				if (component == null)
				{
					throw new InvalidOperationException("The production Gravebuster runtime is incomplete.");
				}
				float y = gravebuster.sprite.Position.Y;
				Check(Mathf.IsEqualApprox(y, component.landStartPositionY), $"The first visible planted frame must start at Y={component.landStartPositionY}; authored {authoredY}, got {y}.");
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				float y2 = gravebuster.sprite.Position.Y;
				Check(Mathf.IsEqualApprox(y2, component.landStartPositionY), $"The first consume-state frame must remain at the landing start; got {y2}.");
				await WaitFrames(2);
				Check(component.graveStone == gravestone, "The planted Treasure Gravebuster must bind the real underlying gravestone.");
				Check(!gravestone.isDestroy, "The landing phase must not destroy its gravestone before consumption completes.");
				normalCrater = LoadPacket("res://Asset/Anime/Character/Crater/CraterN/Packet/CraterN.tres")?.Plant(CraterTestGrid, playAudio: false) as TowerDefenseCrater;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(normalCrater), "The crater conversion regression must plant a real normal crater first.");
				Check(normalCrater?.config?.name == "CraterN", "The crater conversion regression must use the production normal crater config.");
				craterGravebuster = LoadPacket("res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Packet/PlantGravebusterG.tres")?.Plant(CraterTestGrid, playAudio: false) as TowerDefensePlantGravebusterG;
				Check(GodotObject.IsInstanceValid(craterGravebuster), "Treasure Gravebuster must be plantable over a normal crater.");
				if (!GodotObject.IsInstanceValid(craterGravebuster))
				{
					throw new InvalidOperationException("Treasure Gravebuster did not spawn over the normal crater.");
				}
				await WaitFrames(3);
				GravebusterComponent craterComponent = craterGravebuster.componentManager?.GetRuntime<GravebusterComponent>();
				Check(craterComponent != null && !craterComponent.IsReleased, "The crater-planted Treasure Gravebuster must expose its production runtime.");
				if (craterComponent == null)
				{
					throw new InvalidOperationException("The crater-planted Gravebuster runtime is incomplete.");
				}
				Check(!GodotObject.IsInstanceValid(craterComponent.graveStone), "A normal crater must exercise the supported targetless consume path, not masquerade as a gravestone.");
				bool enteredConsumeState = false;
				bool movedTowardConsumeEnd = false;
				for (int frame = 0; frame < 480; frame++)
				{
					await WaitFrames(1);
					enteredConsumeState |= craterComponent.StateMachine?.CurrentStateHandle?.StableId == "gravebuster.consume";
					if (GodotObject.IsInstanceValid(craterGravebuster) && GodotObject.IsInstanceValid(craterGravebuster.sprite))
					{
						movedTowardConsumeEnd |= craterGravebuster.sprite.Position.Y > craterComponent.landStartPositionY + 0.01f;
					}
					goldenCrater = FindCharacter(CraterTestGrid, "CraterG") as TowerDefenseCrater;
					if (GodotObject.IsInstanceValid(goldenCrater))
					{
						break;
					}
				}
				Check(enteredConsumeState, "Treasure Gravebuster planted over a crater must enter its consume state.");
				Check(movedTowardConsumeEnd, "Treasure Gravebuster planted over a crater must play its landing/consume movement.");
				Check(GodotObject.IsInstanceValid(goldenCrater), "Treasure Gravebuster must replace the normal crater with a golden crater after consumption.");
				Check(normalCrater?.isDestroy ?? false, "The consumed normal crater must be destroyed when the golden crater is created.");
				Check(craterGravebuster.isDestroy, "Treasure Gravebuster must finish its instant-use lifecycle after creating the golden crater.");
				TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(GroundTestGrid);
				Check(GodotObject.IsInstanceValid(mapCell2) && mapCell2.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND), "The direct planting regression must use a real empty ground cell.");
				groundGravebuster = LoadPacket("res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Packet/PlantGravebusterG.tres")?.Plant(GroundTestGrid, playAudio: false) as TowerDefensePlantGravebusterG;
				Check(GodotObject.IsInstanceValid(groundGravebuster), "Treasure Gravebuster must be directly plantable on empty ground.");
				if (!GodotObject.IsInstanceValid(groundGravebuster))
				{
					throw new InvalidOperationException("Treasure Gravebuster did not spawn on empty ground.");
				}
				await WaitFrames(3);
				GravebusterComponent groundComponent = groundGravebuster.componentManager?.GetRuntime<GravebusterComponent>();
				Check(groundComponent != null && !groundComponent.IsReleased, "The ground-planted Treasure Gravebuster must expose its production runtime.");
				if (groundComponent == null)
				{
					throw new InvalidOperationException("The ground-planted Gravebuster runtime is incomplete.");
				}
				Check(!GodotObject.IsInstanceValid(groundComponent.graveStone), "Direct ground planting must exercise the supported targetless consume path.");
				bool groundEnteredConsumeState = false;
				bool groundMovedTowardConsumeEnd = false;
				for (int frame = 0; frame < 480; frame++)
				{
					await WaitFrames(1);
					groundEnteredConsumeState |= groundComponent.StateMachine?.CurrentStateHandle?.StableId == "gravebuster.consume";
					if (GodotObject.IsInstanceValid(groundGravebuster) && GodotObject.IsInstanceValid(groundGravebuster.sprite))
					{
						groundMovedTowardConsumeEnd |= groundGravebuster.sprite.Position.Y > groundComponent.landStartPositionY + 0.01f;
					}
					groundGoldenCrater = FindCharacter(GroundTestGrid, "CraterG") as TowerDefenseCrater;
					if (GodotObject.IsInstanceValid(groundGoldenCrater))
					{
						break;
					}
				}
				Check(groundEnteredConsumeState, "Treasure Gravebuster planted on empty ground must enter its consume state.");
				Check(groundMovedTowardConsumeEnd, "Treasure Gravebuster planted on empty ground must play its landing/consume movement.");
				Check(GodotObject.IsInstanceValid(groundGoldenCrater) && groundGoldenCrater.config?.name == "CraterG", "Treasure Gravebuster planted on empty ground must create a golden crater.");
				Check(groundGravebuster.isDestroy, "Ground-planted Treasure Gravebuster must finish its instant-use lifecycle.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGravebusterGPlantingStabilityRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(gravebuster))
			{
				gravebuster.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gravestone))
			{
				gravestone.QueueFree();
			}
			if (GodotObject.IsInstanceValid(craterGravebuster))
			{
				craterGravebuster.QueueFree();
			}
			if (GodotObject.IsInstanceValid(normalCrater))
			{
				normalCrater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(goldenCrater))
			{
				goldenCrater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(groundGravebuster))
			{
				groundGravebuster.QueueFree();
			}
			if (GodotObject.IsInstanceValid(groundGoldenCrater))
			{
				groundGoldenCrater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			ObjectManager.Instance?.Clear();
			RestoreRealFixtures();
			_previousPackets.Clear();
			_previousCharacters.Clear();
			_missingPackets.Clear();
			_missingCharacters.Clear();
			await WaitFrames(16);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(8);
		}
		bool flag = _failures == 0 && _checks == 30;
		GD.Print($"GRAVEBUSTER_G_PLANTING_STABILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
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

	private static TowerDefenseCharacter FindCharacter(Vector2I grid, string configName)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return null;
		}
		mapCell.ClearEmpty();
		foreach (TowerDefenseCharacter character in mapCell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character.config?.name == configName)
			{
				return character;
			}
		}
		return null;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantGravebusterG", "res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Packet/PlantGravebusterG.tres");
		RegisterPacket("GraveStoneDefault", "res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres");
		RegisterPacket("CraterN", "res://Asset/Anime/Character/Crater/CraterN/Packet/CraterN.tres");
		RegisterPacket("CraterG", "res://Asset/Anime/Character/Crater/CraterG/Packet/CraterG.tres");
		RegisterCharacter("PlantGravebusterG", "res://Asset/Anime/Character/Plant/Diamond/GravebusterG/Scene/TowerDefensePlantGravebusterG.tscn");
		RegisterCharacter("GraveStoneDefault", "res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn");
		RegisterCharacter("CraterN", "res://Asset/Anime/Character/Crater/CraterN/Scene/TowerDefenseCraterN.tscn");
		RegisterCharacter("CraterG", "res://Asset/Anime/Character/Crater/CraterG/Scene/TowerDefenseCraterG.tscn");
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
			GD.PushError("[BugOverviewGravebusterGPlantingStabilityRuntimeTest] " + message);
		}
	}
}
