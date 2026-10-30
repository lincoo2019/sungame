using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalWallnutABC : TowerDefenseZombie
{
	private static PackedScene _IMITATER_CLOUD;

	private const string THJG_1 = "uid://d1s3xfn24qy5k";

	private const string THJG_2 = "uid://5y7s57q3mo0d";

	private const string THJG_3 = "uid://dr4snugqr4nug";

	private const string THJG_4 = "uid://2id74xf8q2bt";

	private const string THJG_5 = "uid://d2rvj1bfag3so";

	private const string THJG_6 = "uid://douuu1u5yg5rr";

	private ZombieNormalWallnutABCSprite _zombieSprite;

	public bool halfHp;

	public bool isAttack;

	public string purifyPacketName = "PlantWallnutABC_A";

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zombieSprite = base.sprite as ZombieNormalWallnutABCSprite;
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
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
		if (damagePointName == null)
		{
			return;
		}
		switch (damagePointName.Length)
		{
		case 8:
			switch (damagePointName[0])
			{
			case 'A':
				if (!(damagePointName == "ADamage1"))
				{
					if (damagePointName == "ADamage2")
					{
						_zombieSprite.head1.SetAtlasReplace("THJG_0017_1.png", "uid://5y7s57q3mo0d");
					}
				}
				else
				{
					_zombieSprite.head1.SetAtlasReplace("THJG_0017_1.png", "uid://d1s3xfn24qy5k");
				}
				break;
			case 'B':
				switch (damagePointName)
				{
				case "BDamage0":
				{
					purifyPacketName = "PlantWallnutABC_B";
					TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce2 = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
					towerDefenseEffectParticlesOnce2.GlobalPosition = GetLogicalGlobalPosition();
					TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce2, forceReadableName: false, InternalMode.Disabled);
					_zombieSprite.head1.Visible = false;
					_zombieSprite.head2.Visible = true;
					break;
				}
				case "BDamage1":
					_zombieSprite.head1.Visible = false;
					_zombieSprite.head2.Visible = true;
					_zombieSprite.head2.SetAtlasReplace("THJG_0013_5.png", "uid://dr4snugqr4nug");
					break;
				case "BDamage2":
					_zombieSprite.head1.Visible = false;
					_zombieSprite.head2.Visible = true;
					_zombieSprite.head2.SetAtlasReplace("THJG_0013_5.png", "uid://2id74xf8q2bt");
					break;
				}
				break;
			case 'C':
				switch (damagePointName)
				{
				case "CDamage0":
				{
					purifyPacketName = "PlantWallnutABC_C";
					TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
					towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
					TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
					_zombieSprite.head2.Visible = false;
					_zombieSprite.head3.Visible = true;
					break;
				}
				case "CDamage1":
					_zombieSprite.head2.Visible = false;
					_zombieSprite.head3.Visible = true;
					_zombieSprite.head3.SetAtlasReplace("THJG_0010_8.png", "uid://d2rvj1bfag3so");
					break;
				case "CDamage2":
					_zombieSprite.head2.Visible = false;
					_zombieSprite.head3.Visible = true;
					_zombieSprite.head3.SetAtlasReplace("THJG_0010_8.png", "uid://douuu1u5yg5rr");
					break;
				}
				break;
			}
			break;
		case 3:
			if (damagePointName == "Arm")
			{
				halfHp = true;
				base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			}
			break;
		case 4:
			if (damagePointName == "Head")
			{
				DamagePartCreate("Head", _zombieSprite.head3, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			}
			break;
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(purifyPacketName);
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt(purifyPacketName, base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
