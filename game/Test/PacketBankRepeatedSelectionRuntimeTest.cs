using Godot;

public partial class PacketBankRepeatedSelectionRuntimeTest : Node
{
	private const string PacketName = "PacketBankRepeatedSelectionProbe";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		TowerDefensePacketConfig obj = new TowerDefensePacketConfig
		{
			saveKey = "PacketBankRepeatedSelectionProbe",
			packetAnimeClip = "BodyIdle"
		};
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = CreatePacket(obj);
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2 = CreatePacket(obj);
		TowerDefenseInGameSeedBank towerDefenseInGameSeedBank = new TowerDefenseInGameSeedBank();
		TowerDefenseInGamePacketBank towerDefenseInGamePacketBank = new TowerDefenseInGamePacketBank
		{
			seedBank = towerDefenseInGameSeedBank
		};
		TowerDefenseBattleFeaturePacketBank towerDefenseBattleFeaturePacketBank = new TowerDefenseBattleFeaturePacketBank
		{
			packetBank = towerDefenseInGamePacketBank
		};
		Node node = (towerDefenseInGameSeedBank.packetContainer = new Node
		{
			Name = "SelectedPacketContainer"
		});
		towerDefenseBattleFeaturePacketBank.packetList.Add(towerDefenseInGamePacketShow);
		towerDefenseInGamePacketShow.sprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/PeaShooterSingle.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		AddChild(towerDefenseInGamePacketShow.sprite, forceReadableName: false, InternalMode.Disabled);
		towerDefenseInGamePacketShow.OnMouseExited();
		int blockedPressCount = 0;
		towerDefenseInGamePacketShow2.OnPressed += delegate
		{
			blockedPressCount++;
		};
		towerDefenseInGamePacketShow2.alive = false;
		towerDefenseInGamePacketShow2.Pressed();
		Check(blockedPressCount == 0, "普通不可用卡牌不应绕过原有点击拦截。");
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow3 = CreateSelectedPacket(obj, node, towerDefenseInGameSeedBank, locked: false);
		towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = false;
		towerDefenseInGamePacketShow.allowPressWhenUnavailable = true;
		towerDefenseInGamePacketShow.alive = false;
		towerDefenseInGamePacketShow.select = true;
		towerDefenseInGamePacketShow.OnPressed += towerDefenseBattleFeaturePacketBank.PacketChoose;
		Check(towerDefenseInGameSeedBank.HasPacket("PacketBankRepeatedSelectionProbe") && towerDefenseInGameSeedBank.packetNum == 1, "取消前种子栏必须包含目标卡牌。");
		towerDefenseInGamePacketShow.OnMouseEntered();
		Check(IsPreviewTreeFrozen(towerDefenseInGamePacketShow.sprite), "已选变暗卡牌再次收到鼠标进入时，角色及子动画必须保持冻结。");
		towerDefenseInGamePacketShow.Pressed();
		Check(IsPreviewTreeFrozen(towerDefenseInGamePacketShow.sprite), "再次点击取消选择后，角色预览不应重新播放。");
		Check(!towerDefenseInGameSeedBank.HasPacket("PacketBankRepeatedSelectionProbe"), "再次点击已选卡牌后种子栏标识未移除。");
		Check(towerDefenseInGameSeedBank.packetList.Count == 0 && towerDefenseInGameSeedBank.packetNum == 0, "再次点击已选卡牌后种子栏实例或容量未释放。");
		Check(towerDefenseInGamePacketShow.alive && !towerDefenseInGamePacketShow.select && towerDefenseInGamePacketShow.pressDelayTimer <= 0.0, "取消选择后卡库卡牌未恢复为可立即选择状态。");
		Check(!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow3.config), "取消选择后种子栏卡牌未进入既有回收流程。");
		towerDefenseInGamePacketShow.OnMouseEntered();
		Check(!towerDefenseInGamePacketShow.sprite.IsFrozenPreview, "取消选择后重新移入可用卡牌，预览仍应正常播放。");
		towerDefenseInGamePacketShow.OnMouseExited();
		Check(IsPreviewTreeFrozen(towerDefenseInGamePacketShow.sprite), "离开可用卡牌后，预览应恢复冻结。");
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow4 = CreateSelectedPacket(obj, node, towerDefenseInGameSeedBank, locked: true);
		towerDefenseInGamePacketShow.alive = false;
		towerDefenseInGamePacketShow.select = false;
		towerDefenseInGamePacketShow.pressDelayTimer = 0.0;
		towerDefenseInGamePacketShow.Pressed();
		Check(towerDefenseInGameSeedBank.HasPacket("PacketBankRepeatedSelectionProbe") && towerDefenseInGameSeedBank.packetList.Count == 1 && towerDefenseInGameSeedBank.packetNum == 1, "锁定的预选卡牌不应被再次点击取消。");
		Check(!towerDefenseInGamePacketShow.alive && !towerDefenseInGamePacketShow.select, "锁定卡牌点击后应保持不可用且不显示临时选择框。");
		towerDefenseInGamePacketShow4.@lock = false;
		towerDefenseInGameSeedBank.DeletePacket(towerDefenseInGamePacketShow4);
		towerDefenseInGameSeedBank.DisposeOwnedPackets();
		node.Free();
		towerDefenseInGamePacketShow.sprite.Free();
		towerDefenseInGamePacketShow.sprite = null;
		towerDefenseInGamePacketShow.Free();
		towerDefenseInGamePacketShow2.Free();
		towerDefenseInGamePacketBank.Free();
		towerDefenseInGameSeedBank.Free();
		towerDefenseBattleFeaturePacketBank.Dispose();
		obj.Dispose();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Finish();
	}

	private static TowerDefenseInGamePacketShow CreatePacket(TowerDefensePacketConfig packetConfig)
	{
		return new TowerDefenseInGamePacketShow
		{
			config = packetConfig,
			originalSaveKey = "PacketBankRepeatedSelectionProbe"
		};
	}

	private static TowerDefenseInGamePacketShow CreateSelectedPacket(TowerDefensePacketConfig packetConfig, Node packetContainer, TowerDefenseInGameSeedBank seedBank, bool locked)
	{
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = CreatePacket(packetConfig);
		towerDefenseInGamePacketShow.@lock = locked;
		packetContainer.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		seedBank.packetList.Add(towerDefenseInGamePacketShow);
		seedBank.packetNameSet["PacketBankRepeatedSelectionProbe"] = true;
		seedBank.packetNum = seedBank.packetList.Count;
		return towerDefenseInGamePacketShow;
	}

	private static bool IsPreviewTreeFrozen(Node node)
	{
		if (node is AdobeAnimateSprite { IsFrozenPreview: false })
		{
			return false;
		}
		foreach (Node child in node.GetChildren())
		{
			if (!IsPreviewTreeFrozen(child))
			{
				return false;
			}
		}
		return true;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PacketBankRepeatedSelectionRuntimeTest] " + message);
		}
	}

	private void Finish()
	{
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"PACKET_BANK_REPEAT_SELECTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
