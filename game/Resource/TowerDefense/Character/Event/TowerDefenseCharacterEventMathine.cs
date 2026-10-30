using Godot;

public partial class TowerDefenseCharacterEventMathine : Resource
{
	public static TowerDefenseCharacterEventBase EventGet(string eventName)
	{
		return eventName switch
		{
			"CreateAddPacket" => new TowerDefenseCharacterEventCreateAddPacket(), 
			"LuckyBagCreate" => new TowerDefenseCharacterEventLuckyBagCreate(), 
			"GoldShardCreate" => new TowerDefenseCharacterEventGoldShardCreate(), 
			"SnowBallSpawn" => new TowerDefenseCharacterEventSnowBallSpawn(), 
			"YBCreate" => new TowerDefenseCharacterEventYBCreate(), 
			"SunCreate" => new TowerDefenseCharacterEventSunCreate(), 
			"CoinCreate" => new TowerDefenseCharacterEventCoinCreate(), 
			"CreateProjectile" => new TowerDefenseCharacterEventCreateProjectile(), 
			"ProjectileSplit" => new TowerDefenseCharacterEventProjectileSplit(), 
			"CraterCreate" => new TowerDefenseCharacterEventCraterCreate(), 
			"Forzen" => new TowerDefenseCharacterEventForzen(), 
			"Hypnoses" => new TowerDefenseCharacterEventHypnoses(), 
			"IceSpeedDown" => new TowerDefenseCharacterEventIceSpeedDown(), 
			"Destroy" => new TowerDefenseCharacterEventDestroy(), 
			"Purify" => new TowerDefenseCharacterEventPurify(), 
			"WakeUp" => new TowerDefenseCharacterEventWakeUp(), 
			"PacketCreate" => new TowerDefenseCharacterEventPacketCreate(), 
			"PacketSpawn" => new TowerDefenseCharacterEventPacketSpawn(), 
			_ => null, 
		};
	}
}
