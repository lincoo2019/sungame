using Godot;

[Tool]
public partial class TowerDefenseZombieFootballFumeShroom : TowerDefenseZombie
{
	private AttackComponent _attackComponent2;

	private GpuParticles2D _fireParticles;

	private bool _fumeShroomAttackRunning;

	private double _fireInterval = 1.5;

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
				AttackComponent attackComponent = _attackComponent2;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent2.attackInterval = _fireInterval;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_fireParticles = GetNode<GpuParticles2D>("%FireParticles");
			_attackComponent2.OnAttackReady += OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver += OnAttackComponent2Over;
			_attackComponent2.OnAttack += FumeShroomAttack;
			_attackComponent2.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 4.5f);
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater");
			if (!TowerDefenseManager.GetMapIsNight())
			{
				_attackComponent2.timeScale *= 0.5;
				base.walkSpeedScale *= 0.5;
			}
			else
			{
				_attackComponent2.timeScale *= 1.0;
				base.walkSpeedScale *= 1.0;
			}
		}
	}

	private void OnAttackComponent2Ready()
	{
		_fumeShroomAttackRunning = true;
	}

	private void OnAttackComponent2Over()
	{
		_fumeShroomAttackRunning = false;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent2;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent2.OnAttackReady -= OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver -= OnAttackComponent2Over;
			_attackComponent2.OnAttack -= FumeShroomAttack;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		TryStartFumeShroomAttack();
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		TryStartFumeShroomAttack();
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 3.0;
		TryStartFumeShroomAttack();
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	private void TryStartFumeShroomAttack()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			AttackComponent attackComponent = _attackComponent2;
			if (attackComponent != null && !attackComponent.IsReleased && !_fumeShroomAttackRunning && !die && !nearDie && _attackComponent2.alive && !(_attackComponent2.timer > 0.0) && GodotObject.IsInstanceValid(base.sprite) && !base.sprite.pause && !(base.sprite.timeScale <= 0.0) && _attackComponent2.CanAttack())
			{
				_attackComponent2.SendStateEvent("ToAttack");
			}
		}
	}

	public void FumeShroomAttack()
	{
		_fireParticles.Restart();
		AudioManager.Instance.AudioPlay("Fume");
		CreateFumeProjectile();
	}

	private void CreateFumeProjectile()
	{
		TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData("Puff")
		{
			damageFlags = 2,
			fireMethodFlags = 69,
			penetrateNum = -1,
			overridePenetrateNum = true
		};
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(_fireParticles);
		FireComponent.CreateProjectilePositionByData(null, null, GetGroundHeight(logicalGlobalPosition.Y) - base.groundHeight, logicalGlobalPosition, new Vector2(-600f, 0f), projectileData, 9, base.camp, Vector2.Zero, new BulletFieldSpawnOverrides
		{
			gridYOverride = base.gridPos.Y,
			flipXOverride = (base.Scale.X < 0f),
			fireLengthOverride = 4f
		});
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			DamagePartCreate("Head", ((ZombieFootballFumeShroom)base.sprite).head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
		}
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantFumeShroom");
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantFumeShroom", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
