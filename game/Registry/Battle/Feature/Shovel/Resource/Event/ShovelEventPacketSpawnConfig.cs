using Godot;

[GlobalClass]
public partial class ShovelEventPacketSpawnConfig : ShovelEventConfig
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public int everyNum = 50;

	public override void Execute(TowerDefenseCharacter character)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		if (packetName != "ItemMagnetWave")
		{
			for (int i = 0; i < (int)Mathf.Floor(character.cost / (double)everyNum); i++)
			{
				packetConfig.CallDeferred("Plant", character.gridPos);
			}
		}
		else if (character.cost >= (double)everyNum)
		{
			((TowerDefenseItemMagnetWave)packetConfig.Plant(character.gridPos)).drawNum = (int)Mathf.Floor(character.cost / (double)everyNum);
		}
	}
}
