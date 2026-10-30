using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieVampire : TowerDefenseZombie
{
	private static PackedScene _DRAIN;

	public double drainTime = 10.0;

	public double drainTimer;

	private StateHandle _shootingStateHandle;

	private bool _roleStateSignalsConnected;

	private static PackedScene DRAIN => _DRAIN ?? (_DRAIN = GD.Load<PackedScene>("uid://je8no5cuugta"));

	private static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["drainTimer"] = drainTimer;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		drainTimer = Math.Max(0.0, data.GetValueOrDefault("drainTimer", drainTimer).AsDouble());
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_shootingStateHandle = base.StateMachine?.GetStateById("zombie.vampire.shooting");
			StateHandle shootingStateHandle = _shootingStateHandle;
			if (shootingStateHandle != null && shootingStateHandle.IsValid)
			{
				_shootingStateHandle.Entered += ShootingEntered;
				_shootingStateHandle.Exited += ShootingExited;
				_shootingStateHandle.PhysicsProcessing += ShootingProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_shootingStateHandle != null)
			{
				_shootingStateHandle.Entered -= ShootingEntered;
				_shootingStateHandle.Exited -= ShootingExited;
				_shootingStateHandle.PhysicsProcessing -= ShootingProcessing;
			}
			_shootingStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base.BatchUpdate(delta);
		if (HasGameplayAuthority && TowerDefenseManager.Instance.IsGameRunning() && inGame && base.IsInsideComponentBattlefield && !die && !nearDie)
		{
			if (drainTimer < drainTime)
			{
				drainTimer += delta * timeScale;
			}
			else
			{
				SendStateEvent("ToShooting");
				drainTimer = 0.0;
			}
			if (TowerDefenseManager.GetMapIsNight())
			{
				instance.dealHurtScale = 1.0;
			}
			else
			{
				instance.dealHurtScale = 2.0;
			}
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
		if (!HasGameplayAuthority)
		{
			return;
		}
		if (!attackComponent.CanAttack())
		{
			Walk();
		}
		else if (startAttack && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps)
		{
			attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
			if (TowerDefenseManager.GetMapAttackDpsLifestealRatio() <= 0.0)
			{
				double health = attackComponent.HealthDps(delta, ((TowerDefenseZombieConfig)base.config).attack);
				ApplyBiteLifesteal(health);
			}
		}
	}

	private void ApplyBiteLifesteal(double health)
	{
		if (double.IsFinite(health) && !(health <= 0.0) && GodotObject.IsInstanceValid(instance))
		{
			instance.Health(health);
			ShowHealthComponent obj = showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				showHealthComponent.MarkDirty();
			}
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void ShootingEntered()
	{
		base.sprite.SetAnimation("Shooting", loop: false);
	}

	public void ShootingProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void ShootingExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Shooting")
		{
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "shooting")
		{
			Drain();
		}
	}

	public override void Garlic()
	{
		base.Garlic();
		if (HasGameplayAuthority)
		{
			Hurt(instance.hitpoints * 0.2);
		}
	}

	public void Drain()
	{
		if (!HasGameplayAuthority)
		{
			return;
		}
		Godot.Collections.Array campTarget = TowerDefenseManager.Instance.GetCampTarget(base.camp);
		double num = -10000.0;
		TowerDefenseCharacter towerDefenseCharacter = null;
		foreach (Variant item in campTarget)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = (TowerDefenseCharacter)(GodotObject)item;
			if (towerDefenseCharacter2.instance.hitpoints > num)
			{
				towerDefenseCharacter = towerDefenseCharacter2;
				num = towerDefenseCharacter2.instance.hitpoints;
			}
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			double num2 = towerDefenseCharacter.instance.hitpoints * 0.1;
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(DRAIN, towerDefenseCharacter.gridPos);
			towerDefenseEffectSpriteOnce.GlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseCharacter.Hurt(num2);
			instance.hitpoints += num2;
			ShowHealthComponent obj = showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				showHealthComponent.MarkDirty();
			}
		}
	}
}
