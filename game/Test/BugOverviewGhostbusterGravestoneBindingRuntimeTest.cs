using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGhostbusterGravestoneBindingRuntimeTest : Node
{
	private const string GhostbusterPacketPath = "res://Asset/Anime/Character/Plant/Chapter8/Ghostbuster/Packet/PlantGhostbuster.tres";

	private const string GhostbusterScenePath = "res://Asset/Anime/Character/Plant/Chapter8/Ghostbuster/Scene/TowerDefensePlantGhostbuster.tscn";

	private const string GravestonePacketPath = "res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres";

	private const string GravestoneScenePath = "res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

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
		GhostbusterGravestoneBindingRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseGravestone gravestone = null;
		TowerDefensePlantGhostbuster ghostbuster = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new GhostbusterGravestoneBindingRuntimeControlStub
				{
					Name = "GhostbusterGravestoneBindingRuntimeControl",
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
				TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(cell), "The binding regression must use a real flat map cell.");
				gravestone = LoadPacket("res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres")?.Plant(TestGrid, playAudio: false) as TowerDefenseGravestone;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(gravestone), "The real default gravestone must be placed first.");
				if (!GodotObject.IsInstanceValid(gravestone))
				{
					throw new InvalidOperationException("The real gravestone did not spawn.");
				}
				ghostbuster = LoadPacket("res://Asset/Anime/Character/Plant/Chapter8/Ghostbuster/Packet/PlantGhostbuster.tres")?.Plant(TestGrid, playAudio: false) as TowerDefensePlantGhostbuster;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(ghostbuster), "The real Ghostbuster must be placed over the gravestone.");
				if (!GodotObject.IsInstanceValid(ghostbuster))
				{
					throw new InvalidOperationException("The real Ghostbuster did not spawn.");
				}
				GravebusterComponent gravebusterComponent = ghostbuster.componentManager?.GetRuntime<GravebusterComponent>();
				Check(gravebusterComponent != null && !gravebusterComponent.IsReleased, "The real Ghostbuster must expose its Gravebuster runtime.");
				Check(cell.FindSlotParent(ghostbuster) == gravestone, "The deferred cell placement mapping must resolve the underlying gravestone.");
				Check(gravebusterComponent?.graveStone == gravestone, "GravebusterComponent must re-resolve its target after deferred cell placement.");
				Check(!gravestone.isDestroy, "Binding the target must not prematurely destroy the gravestone.");
				Check(gravebusterComponent?.graveStone?.instance == gravestone.instance, "The bound target must retain the real gravestone gameplay instance.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGhostbusterGravestoneBindingRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(ghostbuster))
			{
				ghostbuster.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gravestone))
			{
				gravestone.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"GHOSTBUSTER_GRAVESTONE_BINDING_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("PlantGhostbuster", "res://Asset/Anime/Character/Plant/Chapter8/Ghostbuster/Packet/PlantGhostbuster.tres");
		RegisterPacket("GraveStoneDefault", "res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres");
		RegisterCharacter("PlantGhostbuster", "res://Asset/Anime/Character/Plant/Chapter8/Ghostbuster/Scene/TowerDefensePlantGhostbuster.tscn");
		RegisterCharacter("GraveStoneDefault", "res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn");
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
			GD.PushError("[BugOverviewGhostbusterGravestoneBindingRuntimeTest] " + message);
		}
	}
}
