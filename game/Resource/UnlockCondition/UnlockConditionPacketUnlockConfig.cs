using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UnlockConditionPacketUnlockConfig : UnlockConditionBaseConfig
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	public override bool Check()
	{
		return GameSaveManager.Instance.GetTowerDefensePacketValue(packetName).GetValueOrDefault("Unlock", new Dictionary()).AsBool();
	}
}
