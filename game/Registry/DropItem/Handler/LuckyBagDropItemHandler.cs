using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LuckyBagDropItemHandler : DropItemHandler
{
	public int LuckyBagNum;

	public override void OnCollect(Vector2 pos, long value)
	{
		LuckyBagNum++;
		switch (LuckyBagNum)
		{
		case 10:
			TowerDefenseManager.Instance.SunCreate(pos, 100L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 10.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			break;
		case 30:
			TowerDefenseManager.Instance.SpawnPacket(TowerDefenseManager.GetPacketConfigCostUpperWithTypeList(300, new Array<TowerDefenseEnum.PACKET_TYPE>
			{
				TowerDefenseEnum.PACKET_TYPE.WHITE,
				TowerDefenseEnum.PACKET_TYPE.ORIGINAL
			}).PickRandom(), pos, 15.0, isFall: false);
			break;
		case 60:
			TowerDefenseManager.Instance.SpawnPacket(TowerDefenseManager.GetPacketConfigCostUpper(0, TowerDefenseEnum.PACKET_TYPE.GOLD).PickRandom(), pos, 15.0, isFall: false);
			break;
		case 100:
			TowerDefenseManager.Instance.SunCreate(pos, 1000L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 10.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			break;
		}
		if (LuckyBagNum > 100 && LuckyBagNum % 100 == 0)
		{
			for (int i = 0; i < LuckyBagNum / 100; i++)
			{
				TowerDefenseManager.Instance.SpawnPacket(TowerDefenseManager.GetPacketConfigCostUpper(0, TowerDefenseEnum.PACKET_TYPE.GOLD).PickRandom(), pos, 15.0, isFall: false);
			}
		}
	}

	public override void Reset()
	{
		LuckyBagNum = 0;
	}
}
