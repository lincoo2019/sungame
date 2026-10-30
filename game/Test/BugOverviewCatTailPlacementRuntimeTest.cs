using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewCatTailPlacementRuntimeTest : Node
{
	private const string CatTailPacketPath = "res://Asset/Anime/Character/Plant/Cover/CatTail/Packet/PlantCatTail.tres";

	private const string CatTailScenePath = "res://Asset/Anime/Character/Plant/Cover/CatTail/Scene/TowerDefensePlantCatTail.tscn";

	private const string LilyPadPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres";

	private const string LilyPadScenePath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn";

	private const string PeaLilyPadPacketPath = "res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Packet/PlantPeaLilyPad.tres";

	private const string PeaLilyPadScenePath = "res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Scene/TowerDefensePlantPeaLilyPad.tscn";

	private static readonly Vector2I LandGrid = new Vector2I(2, 2);

	private static readonly Vector2I BareWaterGrid = new Vector2I(3, 2);

	private static readonly Vector2I LilyPadGrid = new Vector2I(4, 2);

	private static readonly Vector2I PeaLilyPadGrid = new Vector2I(5, 2);

	private static readonly Vector2I GemMatchLandGrid = new Vector2I(6, 2);

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
		CatTailPlacementRuntimeControlStub control = null;
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
					goto end_IL_0096;
				}
				RegisterRealFixtures();
				control = new CatTailPlacementRuntimeControlStub
				{
					Name = "CatTailPlacementRuntimeControl",
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
				TowerDefensePacketConfig catTailPacket = LoadPacket("res://Asset/Anime/Character/Plant/Cover/CatTail/Packet/PlantCatTail.tres");
				Check(GodotObject.IsInstanceValid(catTailPacket), "The regression must load the real CatTail packet.");
				if (!GodotObject.IsInstanceValid(catTailPacket))
				{
					goto end_IL_0096;
				}
				Check(catTailPacket.characterConfig.name == "PlantCatTail", "The packet fixture must resolve the real PlantCatTail config.");
				Check(catTailPacket.GetPlantCover().Contains("PlantLilyPad"), "CatTail must retain LilyPad as an allowed upgrade base.");
				Check(catTailPacket.GetPlantCover().Contains("PlantPeaLilyPad"), "CatTail must retain PeaLilyPad as an allowed upgrade base.");
				Check(!catTailPacket.GetCoverCanDirectPlant(), "The ordinary CatTail packet must not bypass its required LilyPad cover.");
				Check(catTailPacket.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) && catTailPacket.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER), "CatTail must retain the ground/water terrain support used by GemMatch no-limit spawning.");
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(LandGrid);
				TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(BareWaterGrid);
				Check(GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell2), "The regression must use real land and water map cells.");
				Check(mapCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND), "The land fixture must expose the real GROUND cell type.");
				Check(mapCell2.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER) && mapCell2.isWater, "The water fixture must expose the real WATER cell type.");
				Check(!mapCell.CanPacketPlant(catTailPacket), "Normal placement must reject CatTail on bare land.");
				Check(!mapCell2.CanPacketPlant(catTailPacket), "Normal placement must reject CatTail on bare water.");
				Check(catTailPacket.Plant(LandGrid, playAudio: false) == null, "The authoritative packet Plant path must reject CatTail on bare land.");
				Check(catTailPacket.Plant(BareWaterGrid, playAudio: false) == null, "The authoritative packet Plant path must reject CatTail on bare water.");
				TowerDefensePlant lilyPad = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres")?.Plant(LilyPadGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefenseCellInstance lilyPadCell = TowerDefenseManager.GetMapCell(LilyPadGrid);
				Check(GodotObject.IsInstanceValid(lilyPad), "The real LilyPad packet must plant on a water cell.");
				Check(lilyPadCell.HasCharacter("PlantLilyPad"), "The water cell must be occupied by the real LilyPad before upgrading.");
				Check(lilyPadCell.CanPacketPlant(catTailPacket), "Normal placement must allow CatTail when a real LilyPad is present.");
				TowerDefensePlant instance = catTailPacket.Plant(LilyPadGrid, playAudio: false) as TowerDefensePlant;
				Check(GodotObject.IsInstanceValid(instance), "The authoritative packet path must create CatTail over LilyPad.");
				await WaitFrames(4);
				Check(lilyPadCell.HasCharacter("PlantCatTail"), "The LilyPad cell must contain the real CatTail after the upgrade.");
				TowerDefensePlant peaLilyPad = LoadPacket("res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Packet/PlantPeaLilyPad.tres")?.Plant(PeaLilyPadGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefenseCellInstance peaLilyPadCell = TowerDefenseManager.GetMapCell(PeaLilyPadGrid);
				Check(GodotObject.IsInstanceValid(peaLilyPad), "The real PeaLilyPad packet must plant on a water cell.");
				Check(peaLilyPadCell.HasCharacter("PlantPeaLilyPad"), "The water cell must be occupied by the real PeaLilyPad before upgrading.");
				Check(peaLilyPadCell.CanPacketPlant(catTailPacket), "Normal placement must allow CatTail when a real PeaLilyPad is present.");
				TowerDefensePlant instance2 = catTailPacket.Plant(PeaLilyPadGrid, playAudio: false) as TowerDefensePlant;
				Check(GodotObject.IsInstanceValid(instance2), "The authoritative packet path must create CatTail over PeaLilyPad.");
				await WaitFrames(4);
				Check(peaLilyPadCell.HasCharacter("PlantCatTail"), "The PeaLilyPad cell must contain the real CatTail after the upgrade.");
				TowerDefenseCellInstance gemMatchCell = TowerDefenseManager.GetMapCell(GemMatchLandGrid);
				Check(gemMatchCell.CanPacketPlant(catTailPacket, noLimit: true), "GemMatch no-limit placement must still permit CatTail on its land board cells.");
				TowerDefensePlant instance3 = catTailPacket.Plant(GemMatchLandGrid, playAudio: false, noLimit: true) as TowerDefensePlant;
				Check(GodotObject.IsInstanceValid(instance3), "The real packet must preserve GemMatch's no-limit CatTail spawn path.");
				await WaitFrames(4);
				Check(gemMatchCell.HasCharacter("PlantCatTail"), "The GemMatch land cell must contain CatTail after no-limit spawning.");
				goto end_IL_0077;
				end_IL_0096:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCatTailPlacementRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0077;
			}
			return;
			end_IL_0077:;
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
		bool flag = _failures == 0 && _checks == 28;
		GD.Print($"CATTAIL_PLACEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
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
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				if (i >= BareWaterGrid.X && i <= PeaLilyPadGrid.X)
				{
					towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					};
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(towerDefenseCellConfig);
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
		RegisterPacket("PlantCatTail", "res://Asset/Anime/Character/Plant/Cover/CatTail/Packet/PlantCatTail.tres");
		RegisterPacket("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres");
		RegisterPacket("PlantPeaLilyPad", "res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Packet/PlantPeaLilyPad.tres");
		RegisterCharacter("PlantCatTail", "res://Asset/Anime/Character/Plant/Cover/CatTail/Scene/TowerDefensePlantCatTail.tscn");
		RegisterCharacter("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn");
		RegisterCharacter("PlantPeaLilyPad", "res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Scene/TowerDefensePlantPeaLilyPad.tscn");
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
			GD.PushError("[BugOverviewCatTailPlacementRuntimeTest] " + message);
		}
	}
}
