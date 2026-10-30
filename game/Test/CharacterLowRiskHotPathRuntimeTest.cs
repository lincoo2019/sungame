using System;
using Godot;

public sealed partial class CharacterLowRiskHotPathRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			VerifyZombieColumnSpeedCache();
			VerifyGroundPhysicsFastPath();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[CharacterLowRiskHotPath] 未预期异常：{value}");
		}
		StringName stringName = RenderingServer.GetCurrentRenderingMethod();
		bool flag = _failures == 0 && _checks == 19 && stringName == (StringName)"mobile";
		GD.Print($"CHARACTER_LOW_RISK_HOT_PATH_RESULT passed={flag} checks={_checks} failures={_failures} renderer={stringName}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyZombieColumnSpeedCache()
	{
		TowerDefenseMapRuleConfig towerDefenseMapRuleConfig = new TowerDefenseMapRuleConfig
		{
			id = "column-speed-first",
			attackDpsLifestealRatio = 0.1,
			zombieColumnSpeedMinCol = 2,
			zombieColumnSpeedMaxCol = 4,
			zombieColumnSpeedMultiplier = 0.5
		};
		TowerDefenseMapRuleConfig towerDefenseMapRuleConfig2 = new TowerDefenseMapRuleConfig
		{
			id = "column-speed-second",
			attackDpsLifestealRatio = 0.2,
			zombieColumnSpeedMinCol = 3,
			zombieColumnSpeedMaxCol = 5,
			zombieColumnSpeedMultiplier = 0.2
		};
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig();
		try
		{
			towerDefenseMapConfig.specialRules.Add(towerDefenseMapRuleConfig);
			towerDefenseMapConfig.specialRules.Add(towerDefenseMapRuleConfig2);
			towerDefenseMapConfig.RefreshSpecialRuleRuntimeCache();
			Check(IsEqual(towerDefenseMapConfig.GetAttackDpsLifestealRatio(), 0.3), "地图激活刷新必须同时构建吸血倍率缓存。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(1), 1.0), "规则范围外的第一列必须保持原速。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(2), 0.5), "单条规则覆盖列必须返回缓存倍率。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(3), 0.1), "重叠列必须缓存所有有效规则的乘积。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(5), 0.2), "第二条规则单独覆盖列必须返回对应倍率。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(0), 1.0) && IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(51), 1.0), "地图有效范围外的列必须保持原速。");
			towerDefenseMapRuleConfig.zombieColumnSpeedMultiplier = 0.25;
			towerDefenseMapRuleConfig.attackDpsLifestealRatio = 0.4;
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(2), 0.5) && IsEqual(towerDefenseMapConfig.GetAttackDpsLifestealRatio(), 0.3), "规则资源变化但尚未失效时必须继续使用同一份运行时快照。");
			towerDefenseMapConfig.InvalidateSpecialRuleRuntimeCache();
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(2), 0.25), "缓存失效后的首次列查询必须自动重建倍率。");
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(3), 0.05) && IsEqual(towerDefenseMapConfig.GetAttackDpsLifestealRatio(), 0.6), "自动重建必须同时更新重叠列与吸血倍率。");
			towerDefenseMapRuleConfig.zombieColumnSpeedMultiplier = 0.75;
			towerDefenseMapRuleConfig.attackDpsLifestealRatio = 0.6;
			towerDefenseMapConfig.RefreshSpecialRuleRuntimeCache();
			Check(IsEqual(towerDefenseMapConfig.GetZombieColumnSpeedMultiplier(2), 0.75) && IsEqual(towerDefenseMapConfig.GetAttackDpsLifestealRatio(), 0.8), "显式刷新必须立即发布最新规则快照。");
		}
		finally
		{
			towerDefenseMapConfig.Dispose();
			towerDefenseMapRuleConfig.Dispose();
			towerDefenseMapRuleConfig2.Dispose();
		}
	}

	private void VerifyGroundPhysicsFastPath()
	{
		TowerDefenseGroundItemBase groundItem = new TowerDefenseGroundItemBase
		{
			groundHeight = 12.0,
			gravityUse = false,
			isGround = true,
			ySpeed = 0.0
		};
		int landingCount = 0;
		bool relaunchOnLanding = false;
		groundItem.OnLand += delegate
		{
			landingCount++;
			if (relaunchOnLanding)
			{
				groundItem.ySpeed = -80.0;
			}
		};
		try
		{
			groundItem.PhysiceUpdate(1f / 60f);
			Check(groundItem.isGround && IsEqual(groundItem.z, 12.0) && IsEqual(groundItem.ySpeed, 0.0), "稳定贴地角色跳过空事务后必须保持全部垂直状态。");
			Check(landingCount == 0, "稳定贴地快速路径不能重复触发落地事件。");
			groundItem.z = 20.0;
			groundItem.PhysiceUpdate(1f / 60f);
			Check(groundItem.isGround && IsEqual(groundItem.z, 12.0) && landingCount == 0, "贴地标记与高度不一致时仍必须执行原有高度纠正。");
			groundItem.z = 12.0;
			groundItem.ySpeed = -100.0;
			groundItem.PhysiceUpdate(0.1f);
			Check(!groundItem.isGround && IsEqual(groundItem.z, 22.0) && IsEqual(groundItem.ySpeed, -100.0), "带上升速度的贴地角色不能被快速路径拦截。");
			groundItem.z = 13.0;
			groundItem.ySpeed = 20.0;
			groundItem.PhysiceUpdate(0.1f);
			Check(groundItem.isGround && IsEqual(groundItem.z, 12.0) && IsEqual(groundItem.ySpeed, 0.0), "下降穿过地面时必须完成普通落地。");
			Check(landingCount == 1, "普通落地必须且只能触发一次落地事件。");
			groundItem.isGround = false;
			groundItem.z = 8.0;
			groundItem.ySpeed = 5.0;
			groundItem.groundHeight = 10.0;
			relaunchOnLanding = true;
			groundItem.PhysiceUpdate(1f / 60f);
			Check(!groundItem.isGround && IsEqual(groundItem.z, 10.0) && IsEqual(groundItem.ySpeed, -80.0), "上升地面超过空中角色时必须保留落地回调重新起跳语义。");
			Check(landingCount == 2, "上升地面完成接触时必须触发第二次落地事件。");
			groundItem.PhysiceUpdate(0.1f);
			Check(!groundItem.isGround && groundItem.z > groundItem.groundHeight, "落地回调重新起跳后必须在下一步离开地面。");
		}
		finally
		{
			groundItem.Free();
		}
	}

	private static bool IsEqual(double left, double right)
	{
		return Math.Abs(left - right) <= 1E-05;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CharacterLowRiskHotPath] " + message);
		}
	}
}
