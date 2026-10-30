using System;
using System.Threading.Tasks;
using Godot;

public partial class QXSunGoldPriceRuntimeTest : Node
{
	private int _failures;

	private int _checks;

	public override async void _Ready()
	{
		_ = 4;
		try
		{
			CubeBoxBloverControl control = new CubeBoxBloverControl
			{
				isInit = true,
				isGameRunning = true,
				levelConfig = new TowerDefenseLevelConfig
				{
					packetColdDownUse = true
				}
			};
			AddChild(control, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = new Node2D();
			control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseManager.Instance.currentControl = control;
			TowerDefenseInGameSeedBank bank = new TowerDefenseInGameSeedBank();
			control.featureDictionary["SeedBank"] = new TowerDefenseBattleFeatureSeedBank
			{
				seedBank = bank,
				config = new TowerDefenseLevelSeedBankConfig()
			};
			TowerDefenseInGamePacketShow card = TowerDefenseManager.CreatePacketShow();
			card.SetPreviewCreationDeferred(deferred: true);
			control.characterNode.AddChild(card, forceReadableName: false, InternalMode.Disabled);
			TowerDefensePacketConfig original = GD.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres").CreateRuntimeStateCopy();
			card.Init(original);
			card.start = true;
			bank.packetList.Add(card);
			TowerDefenseSunQX sun = GD.Load<PackedScene>("res://Prefab/TowerDefense/Sun/QX/TowerDefenseSunQX.tscn").Instantiate<TowerDefenseSunQX>(PackedScene.GenEditState.Disabled);
			sun.ProcessMode = ProcessModeEnum.Disabled;
			AddChild(sun, forceReadableName: false, InternalMode.Disabled);
			TowerDefensePlantSunGodBean bean = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Diamond/SunGodBean/Scene/TowerDefensePlantSunGodBean.tscn").Instantiate<TowerDefensePlantSunGodBean>(PackedScene.GenEditState.Disabled);
			bean.editorPreviewMode = true;
			bean.inGame = false;
			bean.packet = GD.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Diamond/SunGodBean/Packet/PlantSunGodBean.tres");
			control.characterNode.AddChild(bean, forceReadableName: false, InternalMode.Disabled);
			await Wait(0.1);
			int baseline = original.GetCost();
			sun.Explode();
			Check(card.itemCost == baseline - 25, "全息阳光必须先降价25。");
			bean.Explode();
			Check(card.config._GetType() == TowerDefenseEnum.PACKET_TYPE.GOLD, "太阳神必须将真实卡片金化。");
			Check(card.itemCost == baseline - 25, "金卡仍应保留本次临时降价。");
			card.coldDownOpen = true;
			card.coldDownTimer = 5.0;
			card.TryCommitPendingUse(null);
			card.NotifyUseBehaviorSucceeded();
			await Wait(0.5);
			Check(card.config._GetType() == original._GetType(), "使用一次后必须恢复原卡色。");
			Check(card.itemCost == baseline && card.config.GetCost() == baseline, $"使用金卡后必须恢复原价，实际显示{card.itemCost}、配置{card.config.GetCost()}、原价{baseline}。");
			card.coldDownOpen = false;
			card.coldDownTimer = 0.0;
			card.NotifyUseBehaviorSucceeded();
			await Wait(0.3);
			Check(card.itemCost == baseline, "再次使用原卡色不能保留折扣。");
			((TowerDefenseLevelConfig)control.levelConfig).packetColdDownUse = false;
			sun.Explode();
			bean.Explode();
			Check(card.itemCost == baseline - 25, "第二次降价和金化必须正确叠加。");
			Check(card.TryCommitPendingUse(null), "无冷却卡片必须成功提交一次使用。");
			card.NotifyUseBehaviorSucceeded();
			await Wait(0.5);
			Check(card.config._GetType() == original._GetType(), "无冷却使用后必须恢复原卡色。");
			Check(card.itemCost == baseline && card.config.GetCost() == baseline, $"无冷却成功使用后也必须恢复原价，实际{card.itemCost}。");
			card.config.ChangeCostAdd(new TowerDefensePacketChangeCost
			{
				method = "Increase",
				key = "PermanentTest"
			});
			card.RefreshRuntimeState(includeCost: true);
			sun.Explode();
			sun.Explode();
			bean.Explode();
			bean.Explode();
			Check(card.itemCost == baseline - 25, "两次临时降价必须与长期涨价叠加。");
			TowerDefensePacketConfig towerDefensePacketConfig = card.config.CreateRuntimeStateCopy();
			Check(towerDefensePacketConfig.changeCostList.FindAll((TowerDefensePacketChangeCost rule) => rule.consumeOnPurchase).Count == 2, "运行时快照必须保留两次临时价格的购买消耗标记。");
			Check(!card.TryCommitPendingUse(new SunSpendReceipt(null, 0L, 0L, default(EconomyAccountId), 0L)) && card.itemCost == baseline - 25, "失败购买不能消耗临时价格。");
			card.Use(useSun: false);
			card.NotifyUseBehaviorSucceeded();
			Check(card.config._GetType() == TowerDefenseEnum.PACKET_TYPE.GOLD && card.itemCost == baseline + 25, "金卡还有次数时也应恢复价格，同时保留长期涨价。");
			card.Use(useSun: false);
			card.NotifyUseBehaviorSucceeded();
			Check(card.config._GetType() == original._GetType() && card.itemCost == baseline + 25, "金卡次数耗尽后原卡色的价格必须正确。");
			sun.surcharge = true;
			sun.Explode();
			Check(card.itemCost == baseline + 50, "临时涨价也必须与长期涨价独立叠加。");
			card.Use(useSun: false);
			Check(card.itemCost == baseline + 25, "成功购买只能移除临时涨价，不能清除长期价格规则。");
			sun.QueueFree();
			control.QueueFree();
			bank.Free();
			await Wait(0.1);
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		GD.Print($"QX_SUN_GOLD_PRICE_RESULT passed={_failures == 0 && _checks == 17} checks={_checks} failures={_failures}");
		GetTree().Quit((_failures != 0 || _checks != 17) ? 2 : 0);
	}

	private async Task Wait(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.Print("QX_SUN_GOLD_PRICE_FAILURE " + message);
		}
	}
}
