using Godot;

[GlobalClass]
public partial class GoldShardDropItemHandler : DropItemHandler
{
	public int GoldShardNum;

	public override void OnCollect(Vector2 pos, long value)
	{
		GoldShardNum++;
		if (GoldShardNum >= 4 && GoldShardNum % 4 == 0)
		{
			for (int i = 0; i < GoldShardNum / 4; i++)
			{
				string packetName = (string)TowerDefenseManager.GetPacketBankData("GeneralPlant").GetCategory("Gold").PickRandom();
				TowerDefenseManager.Instance.SpawnPacket(TowerDefenseManager.GetPacketConfig(packetName), pos, 15.0, isFall: false);
			}
			Reset();
		}
	}

	public override void Reset()
	{
		GoldShardNum = 0;
	}
}
