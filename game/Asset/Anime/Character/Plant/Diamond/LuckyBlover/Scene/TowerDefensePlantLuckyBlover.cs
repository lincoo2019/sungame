using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantLuckyBlover : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		AudioManager.Instance.AudioPlay("Diamond");
		if (instance.hypnoses)
		{
			return;
		}
		TowerDefenseEnum.LEVEL_SEEDBANK_METHOD currentPacketBankMethod = TowerDefenseManager.Instance.GetCurrentPacketBankMethod();
		if ((uint)(currentPacketBankMethod - 1) > 1u)
		{
			return;
		}
		Array plantList = TowerDefenseManager.GetPacketBankData("GeneralPlant").GetPlantList();
		Array<TowerDefenseInGamePacketShow> seedBankList = TowerDefenseManager.Instance.GetSeedBankList();
		foreach (TowerDefenseInGamePacketShow item in seedBankList)
		{
			if (!(((item.originalSaveKey != "") ? item.originalSaveKey : item.config.saveKey) == packet.saveKey))
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig((string)plantList.PickRandom());
				TowerDefensePacketOverride towerDefensePacketOverride = new TowerDefensePacketOverride();
				if (packetConfig._GetType() != TowerDefenseEnum.PACKET_TYPE.DIAMOND)
				{
					towerDefensePacketOverride.cost = Mathf.Max(packetConfig.characterConfig.cost - 50, 0);
				}
				item.Cover(packetConfig, towerDefensePacketOverride, keepColddown: false, changePacket: false);
				item.coldDownTimer = packetConfig.GetStartingCooldown();
			}
		}
		int num = ((currentPacketBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET) ? 16 : TowerDefenseManager.Instance.seedbankPacketMax);
		if (seedBankList.Count >= num)
		{
			return;
		}
		int num2 = num - seedBankList.Count;
		for (int i = 0; i < num2; i++)
		{
			string packetName = (string)plantList.PickRandom();
			TowerDefensePacketOverride towerDefensePacketOverride2 = new TowerDefensePacketOverride();
			TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(packetName);
			if (packetConfig2._GetType() != TowerDefenseEnum.PACKET_TYPE.DIAMOND)
			{
				towerDefensePacketOverride2.cost = Mathf.Max(packetConfig2.characterConfig.cost - 50, 0);
			}
			TowerDefenseManager.Instance.AddPacket(packetName, towerDefensePacketOverride2);
		}
	}
}
