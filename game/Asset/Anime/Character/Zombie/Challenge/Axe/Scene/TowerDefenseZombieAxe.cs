using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieAxe : TowerDefenseZombie
{
	private StateHandle _throwStateHandle;

	private bool _stateSignalsConnected;

	private FireComponent _fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Axe";

	private bool _hasAxe = true;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)_fireInterval;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = _fireNum;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
				}
			}
		}
	}

	public bool hasAxe
	{
		get
		{
			return _hasAxe;
		}
		set
		{
			if (_hasAxe != value)
			{
				_hasAxe = value;
				if (!_hasAxe)
				{
					instance.ArmorDelete("Axe", createDamagePart: false);
					base.useAttackDps = true;
					attackComponent.attackType = "Eat";
					base.attackAnimeClip = "Eat";
					base.attackWaterAnimeClip = "WaterEat";
				}
			}
		}
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_throwStateHandle = base.StateMachine?.GetStateById("zombie.axe.throw");
			StateHandle throwStateHandle = _throwStateHandle;
			if (throwStateHandle != null && throwStateHandle.IsValid)
			{
				_throwStateHandle.Entered += ThrowEntered;
				_throwStateHandle.Exited += ThrowExited;
				_throwStateHandle.PhysicsProcessing += ThrowProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_throwStateHandle != null)
			{
				_throwStateHandle.Entered -= ThrowEntered;
				_throwStateHandle.Exited -= ThrowExited;
				_throwStateHandle.PhysicsProcessing -= ThrowProcessing;
			}
			_throwStateHandle = null;
			_stateSignalsConnected = false;
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
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			ConnectStateSignals();
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void ThrowEntered()
	{
		if (base.inWater)
		{
			base.sprite.SetAnimation("WaterFire", loop: false, 0.2);
		}
		else
		{
			base.sprite.SetAnimation("Fire", loop: false, 0.2);
		}
	}

	public void ThrowProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void ThrowExited()
	{
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Axe")
		{
			hasAxe = false;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Arm" && hasAxe)
		{
			SendStateEvent("ToThrow");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "WaterFire" || clip == "Fire")
		{
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "fire"))
		{
			if (command == "attack" && GodotObject.IsInstanceValid(attackComponent.target))
			{
				if ((attackComponent.target.instance.physiqueTypeFlags & 1) != 0)
				{
					attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack * 2.0);
				}
				else
				{
					attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
				}
			}
		}
		else if (hasAxe)
		{
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(-500f, 0f), _fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, Vector2.Zero, overrides);
			hasAxe = false;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["hasAxe"] = hasAxe };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hasAxe = data.GetValueOrDefault("hasAxe", true).AsBool();
	}
}
