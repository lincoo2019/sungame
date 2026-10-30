using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelPacketBankConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetBankType = "GeneralPlant";

	[Export(PropertyHint.None, "")]
	public int categoryBatchSize = 256;

	[Export(PropertyHint.None, "")]
	public int maxPoolSize = 96;

	public void Init(Dictionary data)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = TowerDefenseManager.Instance?.currentLevelConfig as TowerDefenseLevelConfig;
		string text = (GodotObject.IsInstanceValid(towerDefenseLevelConfig) ? towerDefenseLevelConfig.packetBank : packetBankType);
		packetBankType = data.GetValueOrDefault("PacketBankName", text).AsString();
		packetBankType = (string.IsNullOrEmpty(packetBankType) ? "GeneralPlant" : packetBankType);
		categoryBatchSize = Math.Max(1, data.GetValueOrDefault("CategoryBatchSize", 256).AsInt32());
		maxPoolSize = Math.Max(0, data.GetValueOrDefault("MaxPoolSize", 96).AsInt32());
	}
}
