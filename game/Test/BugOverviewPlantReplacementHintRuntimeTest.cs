using System;
using Godot;

public partial class BugOverviewPlantReplacementHintRuntimeTest : Node
{
	private const string PumpkinPeaPacketPath = "res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Packet/PlantPumpkinPea.tres";

	private const string PumpkinPeaSpritePath = "res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/PumpkinPea.tscn";

	private static readonly Vector2I TestGridPosition = new Vector2I(2, 1);

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseControlNew currentControl = instance?.currentControl;
		TowerDefenseControlNew towerDefenseControlNew = null;
		PacketPickControl packetPickControl = null;
		TowerDefenseMapControl towerDefenseMapControl = null;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		TowerDefensePlant occupant = null;
		Resource value = null;
		bool flag = false;
		try
		{
			Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
			Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				return;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Packet/PlantPumpkinPea.tres");
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/PumpkinPea.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real PumpkinPea packet must load.");
			Check(GodotObject.IsInstanceValid(packedScene), "The real PumpkinPea preview sprite must load.");
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene))
			{
				return;
			}
			Check(towerDefensePacketConfig.GetPlantCover().Count == 0, "PumpkinPea must exercise self replacement rather than the explicit plantCover path.");
			Check(towerDefensePacketConfig.characterConfig.plantCoverSelf, "PumpkinPea must retain its self-replacement contract.");
			towerDefenseControlNew = (instance.currentControl = CreateControlWithMap());
			TowerDefenseCellInstance towerDefenseCellInstance = CreateOccupiedCell(towerDefensePacketConfig.characterConfig, out occupant);
			Check(towerDefenseCellInstance.CanPacketPlant(towerDefensePacketConfig), "The production cell rules must accept PumpkinPea over the existing same-name plant.");
			Check(occupant.config.name == towerDefensePacketConfig.characterConfig.name && towerDefenseCellInstance.characterSurround == occupant, "The occupied cell must contain the same PumpkinPea that CharacterPlant will replace.");
			flag = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(towerDefensePacketConfig.saveKey, out value);
			ResourceManager.Instance.CHARCTAER_SPRITE[towerDefensePacketConfig.saveKey] = packedScene;
			instance.CharacterRegister(occupant);
			towerDefenseMapControl = new TowerDefenseMapControl
			{
				spriteNode = new Node2D()
			};
			towerDefenseMapControl.AddChild(towerDefenseMapControl.spriteNode, forceReadableName: false, InternalMode.Disabled);
			packetPickControl = new PacketPickControl();
			packetPickControl.Init(towerDefenseMapControl, (TowerDefenseBattleFeatureMap)towerDefenseControlNew.featureDictionary[new StringName("Map")]);
			towerDefenseInGamePacketShow = new TowerDefenseInGamePacketShow
			{
				config = towerDefensePacketConfig,
				select = true
			};
			int num = ShaderEffectComponent.ShaderEffectFlags["cover"];
			Check((occupant.shaderEffectComponent.GetEffectFlags() & num) == 0, "The replacement target must begin without the cover hint.");
			packetPickControl.PickPacket(towerDefenseInGamePacketShow);
			Check((occupant.shaderEffectComponent.GetEffectFlags() & num) != 0, "Selecting PumpkinPea must apply the black/white cover flicker to the same-name plant it will replace.");
			packetPickControl.PacketPickRelease();
			Check((occupant.shaderEffectComponent.GetEffectFlags() & num) == 0, "Releasing the selected packet must clear the replacement hint.");
		}
		catch (Exception value2)
		{
			_failures++;
			GD.PushError($"[BugOverviewPlantReplacementHintRuntimeTest] Unexpected exception: {value2}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				if (GodotObject.IsInstanceValid(occupant))
				{
					instance.CharacterUnregister(occupant);
				}
				instance.currentControl = currentControl;
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (flag)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantPumpkinPea"] = value;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantPumpkinPea");
				}
			}
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.Free();
			}
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				packetPickControl.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseMapControl))
			{
				towerDefenseMapControl.Free();
			}
			if (GodotObject.IsInstanceValid(occupant))
			{
				occupant.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				towerDefenseControlNew.featureDictionary.Clear();
				towerDefenseControlNew.Free();
			}
		}
		bool flag2 = _failures == 0;
		GD.Print($"BUG_OVERVIEW_PLANT_REPLACEMENT_HINT_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
	}

	private static TowerDefenseControlNew CreateControlWithMap()
	{
		TowerDefenseControlNew towerDefenseControlNew = new TowerDefenseControlNew();
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap();
		towerDefenseBattleFeatureMap.iceCapList.Resize(3);
		towerDefenseControlNew.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
		return towerDefenseControlNew;
	}

	private static TowerDefenseCellInstance CreateOccupiedCell(TowerDefenseCharacterConfig occupantConfig, out TowerDefensePlant occupant)
	{
		TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
		{
			gridPos = TestGridPosition
		};
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.GROUND] = null;
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.AIR] = null;
		occupant = CreatePlant(occupantConfig);
		occupant.cell = towerDefenseCellInstance;
		towerDefenseCellInstance.characterList.Add(occupant);
		towerDefenseCellInstance.characterSlotDictionary[occupant] = null;
		towerDefenseCellInstance.characterSurround = occupant;
		return towerDefenseCellInstance;
	}

	private static TowerDefensePlant CreatePlant(TowerDefenseCharacterConfig config)
	{
		return new TowerDefensePlant
		{
			config = config,
			instance = new TowerDefenseCharacterInstance
			{
				config = config
			},
			shaderEffectComponent = new ShaderEffectComponent()
		};
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewPlantReplacementHintRuntimeTest] " + message);
		}
	}
}
