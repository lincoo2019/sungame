using Godot;

[Tool]
public partial class TowerDefenseZombieLadderGloomShroom : TowerDefenseZombieLadder
{
	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM;

	private AttackComponent _attackComponent2;

	private bool _gloomShroomAttackRunning;

	private double _fireInterval = 2.0;

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
			_attackComponent2.OnAttackReady += OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver += OnAttackComponent2Over;
			_attackComponent2.OnAttack += GloomShroomAttack;
			_attackComponent2.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
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
		_gloomShroomAttackRunning = true;
	}

	private void OnAttackComponent2Over()
	{
		_gloomShroomAttackRunning = false;
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			TryStartGloomShroomAttack();
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 4.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	private void TryStartGloomShroomAttack()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			AttackComponent attackComponent = _attackComponent2;
			if (attackComponent != null && !attackComponent.IsReleased && !_gloomShroomAttackRunning && !die && !nearDie && _attackComponent2.alive && !(_attackComponent2.timer > 0.0) && GodotObject.IsInstanceValid(base.sprite) && !base.sprite.pause && !(base.sprite.timeScale <= 0.0) && _attackComponent2.CanAttack())
			{
				_attackComponent2.SendStateEvent("ToAttack");
			}
		}
	}

	public void GloomShroomAttack()
	{
		TowerDefenseProjectileEffectGroom towerDefenseProjectileEffectGroom = TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM.Instantiate<TowerDefenseProjectileEffectGroom>(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectGroom.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectGroom.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseProjectileEffectGroom, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent2;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent2.OnAttackReady -= OnAttackComponent2Ready;
			_attackComponent2.OnAttackOver -= OnAttackComponent2Over;
			_attackComponent2.OnAttack -= GloomShroomAttack;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			DamagePartCreate("Head", ((ZombieLadderGloomShroom)base.sprite).head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantGloomShroom");
		if (cell.CanPacketPlant(packetConfig, noLimit: true))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos, playAudio: true, noLimit: true);
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantGloomShroom", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
