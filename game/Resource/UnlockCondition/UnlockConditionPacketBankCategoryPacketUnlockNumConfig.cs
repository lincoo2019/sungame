using Godot;

[GlobalClass]
public partial class UnlockConditionPacketBankCategoryPacketUnlockNumConfig : UnlockConditionBaseConfig
{
	[Export(PropertyHint.None, "")]
	public string packetBankName = "";

	[Export(PropertyHint.None, "")]
	public string category = "";

	[Export(PropertyHint.None, "")]
	public int num = 1;

	public override bool Check()
	{
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(packetBankName);
		int num = 0;
		if (GodotObject.IsInstanceValid(packetBankData))
		{
			foreach (Variant item in packetBankData.GetCategory(category))
			{
				string key = (string)item;
				if (GameSaveManager.Instance.GetTowerDefensePacketValue(key).GetValueOrDefault("Unlock", false).AsBool())
				{
					num++;
				}
			}
		}
		return num >= this.num;
	}
}
