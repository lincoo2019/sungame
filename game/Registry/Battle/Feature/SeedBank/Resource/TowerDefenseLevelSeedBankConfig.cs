using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelSeedBankConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool plantColumn;

	[Export(PropertyHint.None, "")]
	public bool packetColdDownStart = true;

	[Export(PropertyHint.None, "")]
	public bool packetColdDownUse = true;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LEVEL_SEEDBANK_METHOD method = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelPacketConfig> packetList = new Array<TowerDefenseLevelPacketConfig>();

	public void Init(Dictionary data)
	{
		packetList.Clear();
		TowerDefenseLevelConfig towerDefenseLevelConfig = TowerDefenseManager.Instance?.currentLevelConfig as TowerDefenseLevelConfig;
		bool flag = GodotObject.IsInstanceValid(towerDefenseLevelConfig);
		method = (flag ? towerDefenseLevelConfig.packetBankMethod : TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE);
		string value = data.GetValueOrDefault("Method", "").AsString();
		if (!string.IsNullOrEmpty(value) && Enum.TryParse<TowerDefenseEnum.LEVEL_SEEDBANK_METHOD>(value, ignoreCase: true, out var result))
		{
			method = result;
		}
		plantColumn = data.GetValueOrDefault("PlantColumn", flag && towerDefenseLevelConfig.plantColumn).AsBool();
		packetColdDownStart = data.GetValueOrDefault("ColdDownStart", !flag || towerDefenseLevelConfig.packetColdDownStart).AsBool();
		packetColdDownUse = data.GetValueOrDefault("ColdDownUse", !flag || towerDefenseLevelConfig.packetColdDownUse).AsBool();
		Godot.Collections.Array array = data.GetValueOrDefault("Packet", new Godot.Collections.Array()).AsGodotArray();
		if (array.Count > 0)
		{
			foreach (Variant item in array)
			{
				AddPacketConfig(item);
			}
			return;
		}
		if (!flag || (method != TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE && method != TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET))
		{
			return;
		}
		foreach (Variant packetBank in towerDefenseLevelConfig.packetBankList)
		{
			AddFallbackPacketConfig(packetBank);
		}
	}

	public Dictionary Export()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseLevelPacketConfig packet in packetList)
		{
			if (GodotObject.IsInstanceValid(packet))
			{
				array.Add(packet.Export());
			}
		}
		return new Dictionary
		{
			["Method"] = method.ToString(),
			["PlantColumn"] = plantColumn,
			["ColdDownStart"] = packetColdDownStart,
			["ColdDownUse"] = packetColdDownUse,
			["Packet"] = array
		};
	}

	private void AddPacketConfig(Variant packetDataVariant)
	{
		TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = new TowerDefenseLevelPacketConfig();
		towerDefenseLevelPacketConfig.Init(packetDataVariant);
		packetList.Add(towerDefenseLevelPacketConfig);
	}

	private void AddFallbackPacketConfig(Variant packetDataVariant)
	{
		if (packetDataVariant.VariantType == Variant.Type.Object && packetDataVariant.AsGodotObject() is TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig2 = towerDefenseLevelPacketConfig.Duplicate(deep: true) as TowerDefenseLevelPacketConfig;
			packetList.Add(GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig2) ? towerDefenseLevelPacketConfig2 : towerDefenseLevelPacketConfig);
		}
		else
		{
			AddPacketConfig(packetDataVariant);
		}
	}
}
