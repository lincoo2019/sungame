using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class BugOverviewCabbageCobXThreeCornpultRuntimeTest : Node
{
	private const string CabbageCobXPath = "res://Asset/Anime/Character/Plant/Cover/CabbageCobX/Packet/PlantCabbageCobX.tres";

	private const string ThreeCornpultPath = "res://Asset/Anime/Character/Plant/Other/ThreeCornpult/Packet/PlantThreeCornpult.tres";

	private const string CabbageCobPath = "res://Asset/Anime/Character/Plant/Chapter5/CabbageCob/Packet/PlantCabbageCob.tres";

	private const string CabbagepultPath = "res://Asset/Anime/Character/Plant/Chapter0/Cabbagepult/Packet/PlantCabbagepult.tres";

	private const string CabbagepultInsidePath = "res://Asset/Anime/Character/Plant/Other/CabbagepultInside/Packet/PlantCabbagepultInside.tres";

	private static readonly Vector2I TestGridPosition = new Vector2I(2, 1);

	private int _checks;

	private int _failures;

	private readonly List<TowerDefensePlant> _occupants = new List<TowerDefensePlant>();

	public override void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseControlNew currentControl = instance?.currentControl;
		TowerDefenseControlNew towerDefenseControlNew = null;
		try
		{
			Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(instance))
			{
				return;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Cover/CabbageCobX/Packet/PlantCabbageCobX.tres");
			TowerDefensePacketConfig towerDefensePacketConfig2 = LoadPacket("res://Asset/Anime/Character/Plant/Other/ThreeCornpult/Packet/PlantThreeCornpult.tres");
			TowerDefensePacketConfig towerDefensePacketConfig3 = LoadPacket("res://Asset/Anime/Character/Plant/Chapter5/CabbageCob/Packet/PlantCabbageCob.tres");
			TowerDefensePacketConfig towerDefensePacketConfig4 = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Cabbagepult/Packet/PlantCabbagepult.tres");
			TowerDefensePacketConfig towerDefensePacketConfig5 = LoadPacket("res://Asset/Anime/Character/Plant/Other/CabbagepultInside/Packet/PlantCabbagepultInside.tres");
			Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real CabbageCobX packet must load.");
			Check(GodotObject.IsInstanceValid(towerDefensePacketConfig2), "The real ThreeCornpult packet must load.");
			Check(GodotObject.IsInstanceValid(towerDefensePacketConfig3) && GodotObject.IsInstanceValid(towerDefensePacketConfig4) && GodotObject.IsInstanceValid(towerDefensePacketConfig5), "All three documented CabbageCobX base packets must load.");
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(towerDefensePacketConfig2) || !GodotObject.IsInstanceValid(towerDefensePacketConfig3) || !GodotObject.IsInstanceValid(towerDefensePacketConfig4) || !GodotObject.IsInstanceValid(towerDefensePacketConfig5))
			{
				return;
			}
			Array<string> plantCover = towerDefensePacketConfig.GetPlantCover();
			Check(plantCover.Count == 3, $"CabbageCobX must have exactly three upgrade bases; got {plantCover.Count}.");
			Check(plantCover.Contains("PlantCabbageCob"), "CabbageCobX must remain plantable on CabbageCob.");
			Check(plantCover.Contains("PlantCabbagepult"), "CabbageCobX must remain plantable on Cabbagepult.");
			Check(plantCover.Contains("PlantCabbagepultInside"), "CabbageCobX must remain plantable on CabbagepultInside.");
			Check(!plantCover.Contains("PlantThreeCornpult"), "CabbageCobX must not list ThreeCornpult as an upgrade base.");
			Check(towerDefensePacketConfig.characterConfig.plantCoverRecycle.Count == plantCover.Count, "CabbageCobX cover refunds must stay aligned with its three allowed bases.");
			Check(towerDefensePacketConfig.characterConfig.plantCoverRecycle.Count == 3 && towerDefensePacketConfig.characterConfig.plantCoverRecycle[0] == 200 && towerDefensePacketConfig.characterConfig.plantCoverRecycle[1] == 0 && towerDefensePacketConfig.characterConfig.plantCoverRecycle[2] == 0, "CabbageCobX refunds must remain [200, 0, 0] for the documented bases.");
			Check(!towerDefensePacketConfig.GetCoverCanDirectPlant(), "CabbageCobX must still require an allowed base plant.");
			towerDefenseControlNew = (instance.currentControl = CreateControlWithMap());
			Check(!CreateOccupiedCell(towerDefensePacketConfig2.characterConfig).CanPacketPlant(towerDefensePacketConfig), "The real cell placement path must reject CabbageCobX on ThreeCornpult.");
			Check(CreateOccupiedCell(towerDefensePacketConfig3.characterConfig).CanPacketPlant(towerDefensePacketConfig), "The real cell placement path must accept CabbageCobX on CabbageCob.");
			Check(CreateOccupiedCell(towerDefensePacketConfig4.characterConfig).CanPacketPlant(towerDefensePacketConfig), "The real cell placement path must accept CabbageCobX on Cabbagepult.");
			Check(CreateOccupiedCell(towerDefensePacketConfig5.characterConfig).CanPacketPlant(towerDefensePacketConfig), "The real cell placement path must accept CabbageCobX on CabbagepultInside.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewCabbageCobXThreeCornpultRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.currentControl = currentControl;
			}
			foreach (TowerDefensePlant occupant in _occupants)
			{
				if (GodotObject.IsInstanceValid(occupant))
				{
					occupant.Free();
				}
			}
			if (GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				towerDefenseControlNew.featureDictionary.Clear();
				towerDefenseControlNew.Free();
			}
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_CABBAGE_COB_X_THREE_CORN_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private static TowerDefenseControlNew CreateControlWithMap()
	{
		TowerDefenseControlNew towerDefenseControlNew = new TowerDefenseControlNew();
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap();
		towerDefenseBattleFeatureMap.iceCapList.Resize(3);
		towerDefenseControlNew.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
		return towerDefenseControlNew;
	}

	private TowerDefenseCellInstance CreateOccupiedCell(TowerDefenseCharacterConfig occupantConfig)
	{
		TowerDefenseCellInstance obj = new TowerDefenseCellInstance
		{
			gridPos = TestGridPosition,
			slot = 
			{
				[TowerDefenseEnum.PLANTGRIDTYPE.GROUND] = null,
				[TowerDefenseEnum.PLANTGRIDTYPE.AIR] = null
			}
		};
		TowerDefensePlant towerDefensePlant = new TowerDefensePlant
		{
			config = occupantConfig,
			instance = new TowerDefenseCharacterInstance
			{
				config = occupantConfig
			}
		};
		_occupants.Add(towerDefensePlant);
		obj.characterList.Add(towerDefensePlant);
		obj.characterSlotDictionary[towerDefensePlant] = null;
		obj.slot[TowerDefenseEnum.PLANTGRIDTYPE.GROUND] = towerDefensePlant;
		return obj;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewCabbageCobXThreeCornpultRuntimeTest] " + message);
		}
	}
}
