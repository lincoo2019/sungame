using Godot;

[Tool]
public partial class TowerDefenseZombieGargantuarGloompult : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_HEAD2 = "uid://cfx6iqy08xujv";

	private const string ZOMBIE_GARGANTUAR_DUCKXING = "uid://6dy81rx4gaue";

	private const string ZOMBIE_GARGANTUAR_ZOMBIE = "uid://dtrl03qm2d0u7";

	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM;

	private FireComponent _fireComponent;

	private AttackComponent _attackComponent2;

	private bool _gloompultAttackRunning;

	private bool _fireAttackRunning;

	public bool over;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Gloom";

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM => _TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM = GD.Load<PackedScene>("uid://bto1eksfijahm"));

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
			if (!IsNodeReady())
			{
				return;
			}
			FireComponent fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				_fireComponent.fireInterval = (float)_fireInterval;
				AttackComponent attackComponent = _attackComponent2;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent2.attackInterval = _fireInterval;
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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_attackComponent2.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
			double num = GD.Randf();
			if (num < 0.3)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://6dy81rx4gaue");
			}
			else if (num < 0.6)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://dtrl03qm2d0u7");
			}
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
			if (!TowerDefenseManager.GetMapIsNight())
			{
				_fireComponent.timeScale *= 0.5f;
				base.walkSpeedScale *= 0.5;
			}
			else
			{
				_fireComponent.timeScale *= 1f;
				base.walkSpeedScale *= 1.0;
			}
			_attackComponent2.OnAttackReady += OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver += OnAttackComponent2Over;
			_attackComponent2.OnAttack += GloompultAttack;
			_fireComponent.OnFireReady += OnFireComponentReady;
			_fireComponent.OnFireOver += OnFireComponentOver;
		}
	}

	private void RefreshComponentRunning()
	{
		componentRunning = _gloompultAttackRunning || _fireAttackRunning;
	}

	private void OnAttackComponent2Ready()
	{
		_gloompultAttackRunning = true;
		RefreshComponentRunning();
	}

	private void OnAttackComponent2Over()
	{
		_gloompultAttackRunning = false;
		RefreshComponentRunning();
	}

	private void OnFireComponentReady()
	{
		_fireAttackRunning = true;
		RefreshComponentRunning();
	}

	private void OnFireComponentOver()
	{
		_fireAttackRunning = false;
		RefreshComponentRunning();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent2;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent2.OnAttackReady -= OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver -= OnAttackComponent2Over;
			_attackComponent2.OnAttack -= GloompultAttack;
		}
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnFireReady -= OnFireComponentReady;
			_fireComponent.OnFireOver -= OnFireComponentOver;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			AttackComponent attackComponent = _attackComponent2;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				_attackComponent2.attackInterval = _fireInterval;
				TryStartGloompultAttack();
			}
		}
	}

	private void TryStartGloompultAttack()
	{
		if (!_gloompultAttackRunning && !componentRunning && !die && !nearDie && _attackComponent2.alive && !(_attackComponent2.timer > 0.0) && (!GodotObject.IsInstanceValid(base.sprite) || !base.sprite.pause) && _attackComponent2.CanAttack())
		{
			_attackComponent2.SendStateEvent("ToAttack");
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_head.png", "uid://cfx6iqy08xujv");
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == impFireEvent)
		{
			((ZombieGargantuarGloompultSprite)base.sprite).puffShroomImpHead.Visible = false;
		}
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}

	public void GloompultAttack()
	{
		if (!TowerDefenseManager.Instance.backZombie)
		{
			TowerDefenseCharacter towerDefenseCharacter = _attackComponent2?.target;
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || (GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && towerDefenseCharacter.instance.canBeCollection && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.isDestroy))
			{
				TowerDefenseProjectileEffectGroom towerDefenseProjectileEffectGroom = TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM.Instantiate<TowerDefenseProjectileEffectGroom>(PackedScene.GenEditState.Disabled);
				towerDefenseProjectileEffectGroom.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
				towerDefenseProjectileEffectGroom.GlobalPosition = GetLogicalGlobalPosition();
				TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseProjectileEffectGroom, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		((ZombieGargantuarGloompultSprite)base.sprite).puffShroomImpHead.Visible = false;
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantGloompult");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantGloompult", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
