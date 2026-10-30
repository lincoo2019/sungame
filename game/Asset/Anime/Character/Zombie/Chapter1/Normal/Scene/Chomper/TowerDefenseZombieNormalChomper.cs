using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalChomper : TowerDefenseZombie
{
	public ChomperComponent chomperComponent;

	private AttackComponent _attackComponent2;

	[Export(PropertyHint.None, "")]
	public double chewTime = 10.0;

	private ZombieNormalChomperSprite _zombieSprite;

	public bool halfHp;

	public bool isAttack;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_zombieSprite = base.sprite as ZombieNormalChomperSprite;
			if (IsNodeReady())
			{
				chomperComponent.chewTime = (float)chewTime;
			}
			_attackComponent2.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X);
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
			if (chomperComponent != null && !chomperComponent.IsReleased)
			{
				chomperComponent.OnSwallowOver += SwallowOver;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (chomperComponent != null && !chomperComponent.IsReleased)
		{
			chomperComponent.OnSwallowOver -= SwallowOver;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame)
		{
			chomperComponent?.TryRecoverOffFieldZombieMovement();
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

	public override void ComponentEntered()
	{
		base.ComponentEntered();
		if (!die && GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.SetAnimation(idleAnimeClip, loop: true, 0.2);
		}
	}

	public override void Attack()
	{
		if (die)
		{
			SendStateEvent("ToDie");
			return;
		}
		if (chomperComponent != null && !chomperComponent.IsReleased)
		{
			IStateMachineController stateMachine = chomperComponent.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				if (GodotObject.IsInstanceValid(base.attackComponent?.target) && base.attackComponent.target is TowerDefenseItemBrain)
				{
					base.Attack();
					return;
				}
				if (GodotObject.IsInstanceValid(base.attackComponent?.target))
				{
					AttackComponent attackComponent = _attackComponent2;
					if (attackComponent != null && !attackComponent.IsReleased)
					{
						_attackComponent2.target = base.attackComponent.target;
					}
				}
				Component();
				chomperComponent.SendStateEvent("ToAttack");
				return;
			}
		}
		base.Attack();
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
				chomperComponent.alive = false;
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantChomper");
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantChomper", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}

	public void SwallowOver()
	{
		Walk();
	}

	public void RiteStart()
	{
		IdleEntered();
	}

	public void RiteFail()
	{
		Walk();
	}
}
