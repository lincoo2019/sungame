using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalImitater : TowerDefenseZombie
{
	private ZombieNormalLmitaterSprite _zombieSprite;

	public bool halfHp;

	public bool isAttack;

	public bool over;

	public bool spawn;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zombieSprite = base.sprite as ZombieNormalLmitaterSprite;
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
			_zombieSprite.head.OnAnimeCompleted += HeadAnimeCompleted;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || over || !TowerDefenseManager.Instance.IsGameRunning() || !inGame || !((double)GetLogicalGlobalPosition().X < TowerDefenseManager.Instance.GetMapGroundRight() - 50.0))
		{
			return;
		}
		bool flag = false;
		foreach (Variant item in TowerDefenseManager.Instance.GetZombie())
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (towerDefenseCharacter.config.canImitate && instance.hypnoses == towerDefenseCharacter.instance.hypnoses)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			over = true;
			Idle();
			if (!die && !nearDie)
			{
				_zombieSprite.head.SetAnimation("Explode", loop: false);
			}
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				DamagePartCreate("Head", _zombieSprite.head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			}
		}
		else
		{
			halfHp = true;
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public void HeadAnimeCompleted(string clip)
	{
		if (!(clip == "Explode") || spawn)
		{
			return;
		}
		spawn = true;
		Array zombie = TowerDefenseManager.Instance.GetZombie();
		double num = -10000.0;
		TowerDefenseCharacter towerDefenseCharacter = null;
		foreach (Variant item in zombie)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = (TowerDefenseCharacter)(GodotObject)item;
			if (towerDefenseCharacter2.config.canImitate && instance.hypnoses == towerDefenseCharacter2.instance.hypnoses && towerDefenseCharacter2.instance.hitpoints > num)
			{
				towerDefenseCharacter = towerDefenseCharacter2;
				num = towerDefenseCharacter2.instance.hitpoints;
			}
		}
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			Walk();
			_zombieSprite.head.SetAnimation("Idle", loop: false);
			spawn = false;
			over = false;
			return;
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.packet))
		{
			TowerDefenseCharacter zombie2 = towerDefenseCharacter.packet.Plant(base.gridPos);
			zombie2.SetLogicalGlobalPosition(new Vector2(GetLogicalGlobalPosition().X, TowerDefenseManager.GetMapCellPlantPos(base.gridPos).Y));
			bool hypnoses = instance.hypnoses;
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie2))
				{
					if (hypnoses)
					{
						zombie2.Hypnoses();
					}
					zombie2.SetSpriteGroupShaderParameter("imitater", true);
					if (GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance))
					{
						TowerDefenseBattleFeatureWave.Instance.currentHpPointTotal += zombie2.GetTotalHitPoint() / 3.0;
						TowerDefenseBattleFeatureWave.Instance.currentHpPoint += zombie2.GetTotalHitPoint() / 3.0;
					}
				}
			}).CallDeferred();
		}
		CallDeferred("Destroy", true);
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantLmitater");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantLmitater", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
