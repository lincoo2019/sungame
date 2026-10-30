using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseDropItemSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public int dropItemType;

	[Export(PropertyHint.None, "")]
	public Vector2 pos;

	[Export(PropertyHint.None, "")]
	public long sunNum;

	[Export(PropertyHint.None, "")]
	public string economyAccountId = "local";

	[Export(PropertyHint.None, "")]
	public int ownershipPolicy;

	[Export(PropertyHint.None, "")]
	public int movingMethod;

	[Export(PropertyHint.None, "")]
	public bool isCollect;

	[Export(PropertyHint.None, "")]
	public bool die;

	[Export(PropertyHint.None, "")]
	public bool over;

	[Export(PropertyHint.None, "")]
	public bool autoCollect;

	[Export(PropertyHint.None, "")]
	public double height;

	[Export(PropertyHint.None, "")]
	public Vector2 velocity;

	[Export(PropertyHint.None, "")]
	public double gravity;

	[Export(PropertyHint.None, "")]
	public double ySpeed;

	[Export(PropertyHint.None, "")]
	public double z;

	[Export(PropertyHint.None, "")]
	public double groundHeight;

	[Export(PropertyHint.None, "")]
	public double timerTimeLeft;

	[Export(PropertyHint.None, "")]
	public Dictionary spriteSave = new Dictionary();

	public void SaveDropItem(Node dropItem)
	{
		if (spriteSave == null)
		{
			spriteSave = new Dictionary();
		}
		pos = ((Node2D)dropItem).GlobalPosition;
		GD.Print($"[Save] 保存掉落物: pos=({pos.X:F1}, {pos.Y:F1})");
		if (dropItem is TowerDefenseSunBase towerDefenseSunBase)
		{
			sunNum = towerDefenseSunBase.sunNum;
			economyAccountId = towerDefenseSunBase.AccountId.Value;
			ownershipPolicy = (int)towerDefenseSunBase.OwnershipPolicy;
			movingMethod = (int)towerDefenseSunBase.movingMethod;
			isCollect = towerDefenseSunBase.isCollect;
			die = towerDefenseSunBase.die;
			over = towerDefenseSunBase.over;
			autoCollect = towerDefenseSunBase.autoCollect;
			height = towerDefenseSunBase.height;
			if (GodotObject.IsInstanceValid(towerDefenseSunBase.moveComponent))
			{
				velocity = towerDefenseSunBase.moveComponent.velocity;
				gravity = towerDefenseSunBase.moveComponent.gravity;
			}
			if (GodotObject.IsInstanceValid(towerDefenseSunBase.dieDownTimer))
			{
				timerTimeLeft = towerDefenseSunBase.dieDownTimer.TimeLeft;
			}
			if (GodotObject.IsInstanceValid(towerDefenseSunBase.sprite))
			{
				spriteSave = towerDefenseSunBase.sprite.ExportSpriteSave();
			}
			dropItemType = (int)towerDefenseSunBase.GetPoolKey();
			GD.Print($"[Save] 掉落物保存完成: type={dropItemType} sunNum={sunNum} 动画={((spriteSave.Count > 0) ? "有" : "无")}");
		}
	}

	public bool LoadDropItem(TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (spriteSave == null)
		{
			spriteSave = new Dictionary();
		}
		GD.Print($"[Load] 加载掉落物: type={dropItemType} pos=({pos.X:F1}, {pos.Y:F1}) sunNum={sunNum}");
		if (isCollect || die)
		{
			GD.PushWarning($"Skipped inactive saved sun drop: type={dropItemType}.");
			return false;
		}
		ObjectManagerConfig.OBJECT id = (ObjectManagerConfig.OBJECT)dropItemType;
		DropItemConfig byId = DropItemRegistry.GetById(id);
		if (byId == null || byId.Category != TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN)
		{
			GD.PushWarning($"Skipped invalid saved sun drop type: {dropItemType}.");
			return false;
		}
		if (!HasValidNumericState())
		{
			GD.PushWarning($"Skipped saved sun drop with invalid numeric state: type={dropItemType}.");
			return false;
		}
		EconomyAccountId accountId;
		bool flag = EconomyAccountId.TryParse(economyAccountId, out accountId);
		bool flag2 = !flag && string.IsNullOrWhiteSpace(economyAccountId);
		if (!flag && !flag2)
		{
			GD.PushWarning("Skipped saved sun drop with malformed account id: " + economyAccountId + ".");
			return false;
		}
		if (flag && !accountId.IsLocal)
		{
			GD.PushWarning($"Skipped remote saved sun drop without a persisted ledger: account={accountId}.");
			return false;
		}
		if (flag2)
		{
			accountId = EconomyAccountId.Local;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Node node = ObjectManager.PoolPop(id, characterNode);
		try
		{
			if (node is TowerDefenseSunBase towerDefenseSunBase)
			{
				towerDefenseSunBase.GlobalPosition = pos;
				towerDefenseSunBase.InitFromSave(accountId, (!flag2 && ownershipPolicy == 1) ? SunDropOwnershipPolicy.AccountOwned : SunDropOwnershipPolicy.LegacySharedReplica, sunNum, (TowerDefenseEnum.SUN_MOVING_METHOD)movingMethod, height, velocity, gravity);
				towerDefenseSunBase.isCollect = isCollect;
				towerDefenseSunBase.die = die;
				towerDefenseSunBase.over = over;
				towerDefenseSunBase.RestoreAutoCollect(autoCollect);
				if (timerTimeLeft > 0.0 && GodotObject.IsInstanceValid(towerDefenseSunBase.dieDownTimer))
				{
					towerDefenseSunBase.dieDownTimer.Start(timerTimeLeft);
				}
				if (GodotObject.IsInstanceValid(towerDefenseSunBase.sprite) && spriteSave != null && spriteSave.Count > 0)
				{
					towerDefenseSunBase.sprite.ImportSpriteSave(spriteSave);
				}
				GD.Print($"[Load] 掉落物加载完成: type={dropItemType} sunNum={sunNum} 动画={((spriteSave.Count > 0) ? "已恢复" : "无")}");
				return true;
			}
			if (GodotObject.IsInstanceValid(node))
			{
				GD.PushWarning($"Pool entry {dropItemType} is not a TowerDefenseSunBase; returning it to the pool.");
				ObjectManager.PoolPush(id, node);
			}
			return false;
		}
		catch (Exception ex)
		{
			GD.PushWarning($"Skipped invalid saved sun drop {dropItemType}: {ex.Message}");
			if (GodotObject.IsInstanceValid(node))
			{
				ObjectManager.PoolPush(id, node);
			}
			return false;
		}
	}

	internal bool HasValidNumericState()
	{
		if (float.IsFinite(pos.X) && float.IsFinite(pos.Y) && float.IsFinite(velocity.X) && float.IsFinite(velocity.Y) && double.IsFinite(height) && double.IsFinite(gravity) && double.IsFinite(timerTimeLeft) && timerTimeLeft >= 0.0)
		{
			return Enum.IsDefined(typeof(TowerDefenseEnum.SUN_MOVING_METHOD), movingMethod);
		}
		return false;
	}
}
