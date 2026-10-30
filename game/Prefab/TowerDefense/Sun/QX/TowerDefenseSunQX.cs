using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseSunQX : TowerDefenseSunBase
{
	private const int PriceChangeAmount = 25;

	private const string DiscountKey = "QXSunDiscount";

	private const string SurchargeKey = "QXSunSurcharge";

	public bool surcharge;

	public override void _Ready()
	{
		base._Ready();
		base.dieDownTimer.Timeout += DieDown;
	}

	public override string GetGroupName()
	{
		return "QXSun";
	}

	public override ObjectManagerConfig.OBJECT GetPoolKey()
	{
		return ObjectManagerConfig.OBJECT.SUN_QX;
	}

	public override void OnCollectStart()
	{
		if (gridPos.X < 0 || gridPos.Y < 0)
		{
			gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.sprite.GlobalPosition);
		}
		Explode();
	}

	public override long GetCollectValue()
	{
		return sunNum;
	}

	public override bool ShouldAutoCollect()
	{
		autoCollect = true;
		return base.ShouldAutoCollect();
	}

	public override bool OnDieDown()
	{
		return false;
	}

	public override void DieDown()
	{
		if (!isCollect)
		{
			if (GameSaveManager.Instance.GetFeatureValue("SunCollect") != 0)
			{
				Collection();
			}
			else
			{
				base.DieDown();
			}
		}
	}

	public override void OnRefresh()
	{
		gridPos = new Vector2I(-1, -1);
		surcharge = false;
	}

	public void Explode()
	{
		if (surcharge)
		{
			TryApplyPriceChange("Increase", 25, "QXSunSurcharge");
		}
		else
		{
			TryApplyPriceChange("Decrease", 25, "QXSunDiscount");
		}
	}

	private void TryApplyPriceChange(string method, int amount, string key)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		Array<TowerDefenseInGamePacketShow> seedBankList = instance.GetSeedBankList();
		if (seedBankList == null || seedBankList.Count == 0)
		{
			return;
		}
		List<TowerDefenseInGamePacketShow> list = new List<TowerDefenseInGamePacketShow>();
		foreach (TowerDefenseInGamePacketShow item in seedBankList)
		{
			if (GodotObject.IsInstanceValid(item) && item.start)
			{
				TowerDefensePacketConfig config = item.config;
				if (GodotObject.IsInstanceValid(config) && config._GetType() != TowerDefenseEnum.PACKET_TYPE.DIAMOND && config.canChangeCost && !HasLockedCost(config) && (surcharge || (config.GetCostBeforeModifiers() > 0 && config.GetCost() > 0)))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count != 0)
		{
			int index = GD.RandRange(0, list.Count - 1);
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = list[index];
			TowerDefensePacketConfig config2 = towerDefenseInGamePacketShow.config;
			string key2 = CreatePriceChangeKey(key);
			TowerDefensePacketChangeCost changeCost = new TowerDefensePacketChangeCost
			{
				method = method,
				amontDictionary = new Dictionary
				{
					[0] = amount,
					[1] = amount,
					[3] = amount,
					[4] = amount,
					[5] = amount,
					[6] = amount,
					[7] = amount,
					[8] = amount
				},
				key = key2,
				consumeOnPurchase = true
			};
			if (config2.ChangeCostAdd(changeCost))
			{
				towerDefenseInGamePacketShow.RefreshRuntimeState(includeCost: true);
				towerDefenseInGamePacketShow.FlashCostChange();
			}
		}
	}

	private static string CreatePriceChangeKey(string key)
	{
		return $"{key}:{Guid.NewGuid():N}";
	}

	private static bool HasLockedCost(TowerDefensePacketConfig config)
	{
		if (!config.canChangeCost)
		{
			return true;
		}
		if (config.changeCostList == null)
		{
			return false;
		}
		foreach (TowerDefensePacketChangeCost changeCost in config.changeCostList)
		{
			if (changeCost.lockCost)
			{
				return true;
			}
		}
		return false;
	}
}
