using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalCornpult : TowerDefenseZombie
{
	public FireComponent fireComponent;

	[Export(PropertyHint.None, "")]
	public double fireInterval = 3.0;

	[Export(PropertyHint.None, "")]
	public int fireNum = 1;

	[Export(PropertyHint.None, "")]
	public string projectileName = "Corn";

	private ZombieNormalCornpultSprite _zombieSprite;

	public bool halfHp;

	public bool isAttack;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		_zombieSprite = base.sprite as ZombieNormalCornpultSprite;
		fireComponent.OnConfirmProjectile += ConfirmProjectile;
		fireComponent.OnFireOver += FireOver;
		if (IsNodeReady())
		{
			fireComponent.fireInterval = (float)fireInterval;
			fireComponent.fireNum = fireNum;
			if (fireComponent.fireCheckList.Count > 0)
			{
				FireComponentProjectileResource projectile = fireComponent.fireCheckList[0].projectile;
				if (projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
				{
					fireComponentProjectileSingle.projectileName = projectileName;
				}
				else if (projectile is FireComponentProjectileWeight fireComponentProjectileWeight)
				{
					fireComponentProjectileWeight.projectileSimilarName = projectileName;
				}
			}
		}
		ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnConfirmProjectile -= ConfirmProjectile;
			this.fireComponent.OnFireOver -= FireOver;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			FireComponent fireComponent = this.fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				this.fireComponent.fireInterval = (float)fireInterval;
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
				fireComponent.alive = false;
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantCornpult");
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantCornpult", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}

	public void ConfirmProjectile(int projectileId, TowerDefenseProjectileCreateData projectileData)
	{
		string text = projectileData.projectileName.ToString();
		if (!(text == "Butter"))
		{
			if (text == "ButterCatapult")
			{
				_zombieSprite.head.SetFliters(new Array { "Cornpult_butter" }, open: true);
				_zombieSprite.head.SetFliters(new Array { "Cornpult_kernal" }, open: false);
			}
		}
		else
		{
			_zombieSprite.head.SetFliters(new Array { "Cornpult_butter" }, open: false);
			_zombieSprite.head.SetFliters(new Array { "Cornpult_kernal" }, open: true);
		}
	}

	public void FireOver()
	{
		_zombieSprite.head.SetFliters(new Array { "Cornpult_butter" }, open: false);
		_zombieSprite.head.SetFliters(new Array { "Cornpult_kernal" }, open: true);
	}
}
