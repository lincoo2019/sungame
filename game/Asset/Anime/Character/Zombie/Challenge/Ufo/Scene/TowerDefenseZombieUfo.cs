using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieUfo : TowerDefenseZombie
{
	private enum AttractPhase
	{
		None,
		Up,
		Shooting,
		Down
	}

	private static PackedScene _EXPLOSION;

	private StateHandle _flyStateHandle;

	private StateHandle _flyAttackStateHandle;

	private StateHandle _landStateHandle;

	private StateHandle _landIdleStateHandle;

	private StateHandle _openStateHandle;

	private StateHandle _flyUpStateHandle;

	private bool _stateSignalsConnected;

	public double speed = 30.0;

	private double _landTimer;

	private double _landStayTimer;

	private double _landStayDuration = 3.0;

	private int _releasedPassengers;

	private bool _isOnGround;

	private bool _deathExplosionCreated;

	private AttractPhase _attractPhase;

	private double _absorbTimer;

	private double _absorbDuration;

	private List<TowerDefenseCharacter> _absorbTargets = new List<TowerDefenseCharacter>();

	private static PackedScene EXPLOSION => _EXPLOSION ?? (_EXPLOSION = GD.Load<PackedScene>("uid://dtj82qyey3wx5"));

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_flyStateHandle = base.StateMachine?.GetStateById("zombie.ufo.fly");
		_flyAttackStateHandle = base.StateMachine?.GetStateById("zombie.ufo.fly_attack");
		_landStateHandle = base.StateMachine?.GetStateById("zombie.ufo.land");
		_landIdleStateHandle = base.StateMachine?.GetStateById("zombie.ufo.land_idle");
		_openStateHandle = base.StateMachine?.GetStateById("zombie.ufo.open");
		_flyUpStateHandle = base.StateMachine?.GetStateById("zombie.ufo.fly_up");
		StateHandle flyStateHandle = _flyStateHandle;
		if (flyStateHandle == null || !flyStateHandle.IsValid)
		{
			return;
		}
		StateHandle flyAttackStateHandle = _flyAttackStateHandle;
		if (flyAttackStateHandle == null || !flyAttackStateHandle.IsValid)
		{
			return;
		}
		StateHandle landStateHandle = _landStateHandle;
		if (landStateHandle == null || !landStateHandle.IsValid)
		{
			return;
		}
		StateHandle landIdleStateHandle = _landIdleStateHandle;
		if (landIdleStateHandle == null || !landIdleStateHandle.IsValid)
		{
			return;
		}
		StateHandle openStateHandle = _openStateHandle;
		if (openStateHandle != null && openStateHandle.IsValid)
		{
			StateHandle flyUpStateHandle = _flyUpStateHandle;
			if (flyUpStateHandle != null && flyUpStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.PhysicsProcessing += FlyProcessing;
				_flyAttackStateHandle.Entered += FlyAttackEntered;
				_flyAttackStateHandle.Exited += FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing += FlyAttackProcessing;
				_landStateHandle.Entered += LandEntered;
				_landIdleStateHandle.Entered += LandIdleEntered;
				_landIdleStateHandle.PhysicsProcessing += LandIdleProcessing;
				_openStateHandle.Entered += OpenEntered;
				_flyUpStateHandle.Entered += FlyUpEntered;
				_stateSignalsConnected = true;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			base.useAttackDps = false;
			ConnectStateSignals();
		}
	}

	public void FlyEntered()
	{
		_isOnGround = false;
		_attractPhase = AttractPhase.None;
		base.sprite.SetAnimation("Idle", loop: true, 0.2);
	}

	public void FlyProcessing(double delta)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		base.sprite.timeScale = timeScale;
		if (die || nearDie)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 position = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		bool flag = false;
		if (!base.sprite.pause)
		{
			float step = 0f - (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
			flag = MoveToAbsorptionTarget(ref position, step);
			SetGlobalPositionForPhysicsFrame(position, currentPhysicsFrame);
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(position);
		}
		if ((double)position.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 20.0)
		{
			if (TowerDefenseManager.Instance.IsIZMMode())
			{
				ReleaseRemainingPassengers();
				Destroy();
			}
		}
		else
		{
			if (!base.IsInsideComponentBattlefield)
			{
				return;
			}
			if (!base.sprite.pause && attackComponent.CanAttack())
			{
				attackComponent.SmashAttackAll(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			if (flag)
			{
				SendStateEvent("ToFlyAttack");
			}
			else if (!base.sprite.pause && _releasedPassengers < 4)
			{
				_landTimer += delta * timeScale;
				if (_landTimer >= 10.0)
				{
					_landTimer = 0.0;
					SendStateEvent("ToLand");
				}
			}
		}
	}

	private bool MoveToAbsorptionTarget(ref Vector2 position, float step)
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		Vector2 vector = position + new Vector2(step, 0f);
		Vector2I mapGridPos = towerDefenseManager.GetMapGridPos(position);
		Vector2I mapGridPos2 = towerDefenseManager.GetMapGridPos(vector);
		int num = ((!(step < 0f)) ? 1 : (-1));
		int num2 = mapGridPos.X;
		while (true)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(num2, mapGridPos.Y));
			if (mapCell != null && _attractPhase == AttractPhase.None)
			{
				foreach (TowerDefenseCharacter character in mapCell.characterList)
				{
					if (CanAbsorb(character))
					{
						float x = character.GetLogicalGlobalPosition().X;
						position.X = Mathf.MoveToward(position.X, x, Mathf.Abs(step));
						return Mathf.IsEqualApprox(position.X, x);
					}
				}
			}
			if (num2 == mapGridPos2.X)
			{
				break;
			}
			num2 += num;
		}
		position = vector;
		return false;
	}

	private bool CanAbsorb(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && !character.die && !character.nearDie && !character.isDestroy && character is TowerDefensePlant towerDefensePlant && towerDefensePlant.instance.canBeCollection && !towerDefensePlant.instance.hologram && towerDefensePlant.instance.height != TowerDefenseEnum.CHARACTER_HEIGHT.LOW && (towerDefensePlant.instance.maskFlags & 1) != 0)
		{
			return towerDefensePlant.instance.hypnoses == instance.hypnoses;
		}
		return false;
	}

	public void FlyAttackEntered()
	{
		_isOnGround = false;
		_attractPhase = AttractPhase.Up;
		_absorbTimer = 0.0;
		_absorbDuration = 0.0;
		_absorbTargets.Clear();
		base.sprite.SetAnimation("Up", loop: false, 0.2);
	}

	public void FlyAttackProcessing(double delta)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		base.sprite.timeScale = timeScale;
		if (die || !base.IsInsideComponentBattlefield)
		{
			return;
		}
		if (!base.sprite.pause && _releasedPassengers < 4)
		{
			_landTimer += delta * timeScale;
		}
		if (_attractPhase == AttractPhase.Shooting)
		{
			_absorbTimer += delta * timeScale;
			if (_absorbTimer >= _absorbDuration)
			{
				StartAbsorbTween();
				_absorbTargets.Clear();
				_attractPhase = AttractPhase.Down;
				base.sprite.SetAnimation("Down", loop: false, 0.2);
			}
		}
	}

	private async void StartAbsorbTween()
	{
		List<TowerDefenseCharacter> targetsToDestroy = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter absorbTarget in _absorbTargets)
		{
			if (CanAbsorb(absorbTarget))
			{
				absorbTarget.die = true;
				if (GodotObject.IsInstanceValid(absorbTarget.sprite))
				{
					Tween tween = GetTree().CreateTween();
					tween.TweenProperty(absorbTarget.sprite, "scale", Vector2.Zero, 0.3);
					tween.Parallel().TweenProperty(absorbTarget.sprite, "modulate:a", 0.0, 0.3);
					tween.Parallel().TweenProperty(absorbTarget, "z", absorbTarget.z + 60.0, 0.3);
				}
				targetsToDestroy.Add(absorbTarget);
			}
		}
		if (targetsToDestroy.Count <= 0)
		{
			return;
		}
		await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
		foreach (TowerDefenseCharacter item in targetsToDestroy)
		{
			if (GodotObject.IsInstanceValid(item) && !item.isDestroy)
			{
				item.skipDestroySet = true;
				item.Destroy(freeInstance: false);
				item.QueueFree();
			}
		}
	}

	public void FlyAttackExited()
	{
		_attractPhase = AttractPhase.None;
	}

	private void StartAbsorbing()
	{
		_absorbTargets.Clear();
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos);
		if (mapCell != null)
		{
			foreach (TowerDefenseCharacter character in mapCell.characterList)
			{
				if (CanAbsorb(character))
				{
					_absorbTargets.Add(character);
				}
			}
		}
		if (_absorbTargets.Count == 0)
		{
			_attractPhase = AttractPhase.Down;
			base.sprite.SetAnimation("Down", loop: false, 0.2);
			return;
		}
		double num = 0.0;
		foreach (TowerDefenseCharacter absorbTarget in _absorbTargets)
		{
			if (GodotObject.IsInstanceValid(absorbTarget) && absorbTarget.instance.hitpoints > num)
			{
				num = absorbTarget.instance.hitpoints;
			}
		}
		_absorbDuration = Math.Max(0.5, num / 1000.0);
		_absorbTimer = 0.0;
		_attractPhase = AttractPhase.Shooting;
		base.sprite.SetAnimation("Shooting", loop: true, 0.2);
	}

	public void LandEntered()
	{
		_isOnGround = true;
		instance.collisionFlags = 41;
		instance.maskFlags = 9;
		base.sprite.SetAnimation("Land", loop: false, 0.2);
	}

	public void LandIdleEntered()
	{
		_landStayDuration = GD.RandRange(2.5, 3.5);
		_landStayTimer = 0.0;
		base.sprite.SetAnimation("LandIdle", loop: true, 0.2);
	}

	public void LandIdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (die || !base.IsInsideComponentBattlefield)
		{
			return;
		}
		_landStayTimer += delta * timeScale;
		if (attackComponent.CanAttack())
		{
			if (attackComponent.target is TowerDefensePlant && GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
			{
				attackComponent.target = attackComponent.target.cell.GetSpike();
			}
			if ((attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
			{
				attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			else
			{
				if (attackComponent.target.instance.spikeHurt != -1.0)
				{
					TowerDefenseCharacter target = attackComponent.target;
					double spikeHurt = attackComponent.target.instance.spikeHurt;
					target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
				}
				Die();
			}
		}
		if (_landStayTimer >= _landStayDuration)
		{
			if (_releasedPassengers >= 4)
			{
				SendStateEvent("ToFlyUp");
			}
			else
			{
				SendStateEvent("ToOpen");
			}
		}
	}

	public void OpenEntered()
	{
		base.sprite.SetAnimation("Open", loop: false, 0.2);
	}

	private async void ReleaseAlienAt(Vector2 spawnPos)
	{
		_releasedPassengers++;
		if (_releasedPassengers >= 4)
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_dolphinrider_outerarm_upper", "anim_head2", "anim_head1", "Zombie_dolphinrider_body1", "Zombie_dolphinrider_innerarm_upper" }, open: false);
		}
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieAlien");
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(node2D) || !GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return;
		}
		Vector2I mapGridPos = towerDefenseManager.GetMapGridPos(spawnPos);
		Vector2 globalPoint = new Vector2(spawnPos.X, TowerDefenseManager.GetMapCellPlantPos(mapGridPos).Y);
		TowerDefenseCharacter alien = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, node2D.ToLocal(globalPoint), mapGridPos, 40.0) : packetConfig.Create(node2D.ToLocal(globalPoint), mapGridPos, 40.0));
		if (!GodotObject.IsInstanceValid(alien))
		{
			return;
		}
		alien.EnableComponentGameplayUntilBattlefieldEntry();
		node2D.CallDeferred("add_child", alien);
		alien.CallDeferred("SetHitpointAndScale", instance.hitpointScale, transformPoint.Scale);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(alien))
				{
					alien.Hypnoses();
				}
			}).CallDeferred();
		}
		alien.CallDeferred("WalkReady");
		Dictionary spawnState = new Dictionary
		{
			["component_gameplay_until_battlefield_entry"] = true,
			["walk_ready_after_spawn"] = true
		};
		TowerDefenseManager.PublishSpawnedCharacter("ZombieAlien", alien, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState);
		SceneTree tree = GetTree();
		if (tree != null)
		{
			await ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
			if (GodotObject.IsInstanceValid(alien) && GodotObject.IsInstanceValid(alien.sprite))
			{
				alien.z = alien.groundHeight + 60.0;
				alien.sprite.Modulate = new Color(1f, 1f, 1f, 0f);
				Tween tween = tree.CreateTween();
				tween.TweenProperty(alien, "z", alien.groundHeight, 0.5);
				tween.Parallel().TweenProperty(alien.sprite, "modulate:a", 1.0, 0.5);
			}
		}
	}

	private void ReleaseAlien()
	{
		if (_releasedPassengers < 4)
		{
			ReleaseAlienAt(GetLogicalGlobalPosition());
		}
	}

	private void ReleaseRemainingPassengers()
	{
		int num = 4 - _releasedPassengers;
		if (num > 0)
		{
			float num2 = 50f;
			float num3 = (float)(-(num - 1)) * num2 / 2f;
			for (int i = 0; i < num; i++)
			{
				float x = num3 + (float)i * num2;
				ReleaseAlienAt(GetLogicalGlobalPosition() + new Vector2(x, 0f));
			}
		}
	}

	public void FlyUpEntered()
	{
		_isOnGround = false;
		instance.collisionFlags = 2;
		instance.maskFlags = 2;
		base.sprite.SetAnimation("Fly", loop: false, 0.2);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!die && command == "spawn")
		{
			ReleaseAlien();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (die && !_deathExplosionCreated && clip == "Death")
		{
			_deathExplosionCreated = true;
			CreateExplosionEffect();
			Destroy();
			return;
		}
		base.AnimeCompleted(clip);
		if (die)
		{
			return;
		}
		switch (clip)
		{
		case "Up":
			if (_attractPhase == AttractPhase.Up)
			{
				StartAbsorbing();
			}
			break;
		case "Down":
		{
			if (_attractPhase != AttractPhase.Down)
			{
				break;
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos);
			bool flag = false;
			if (mapCell != null)
			{
				foreach (TowerDefenseCharacter character in mapCell.characterList)
				{
					if (CanAbsorb(character))
					{
						flag = true;
						break;
					}
				}
			}
			SendStateEvent(flag ? "ToFlyAttack" : "ToFly");
			break;
		}
		case "Land":
			SendStateEvent("ToLandIdle");
			break;
		case "Open":
			SendStateEvent("ToFlyUp");
			break;
		case "Fly":
			SendStateEvent("ToFly");
			break;
		}
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		ReleaseRemainingPassengers();
		if (_isOnGround)
		{
			CreateExplosionEffect();
			Destroy();
		}
	}

	private void CreateExplosionEffect()
	{
		AudioManager.Instance.AudioPlay("ZamboniExplosion");
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(EXPLOSION, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public override void Walk()
	{
		SendStateEvent("ToFly");
	}

	public override void Attack()
	{
		SendStateEvent("ToFlyAttack");
	}

	public override void Blow()
	{
		BlowBack(0.3, 0.2);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "speed", speed },
			{ "releasedPassengers", _releasedPassengers },
			{ "landTimer", _landTimer }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		speed = data.GetValueOrDefault("speed", 30.0).AsDouble();
		_releasedPassengers = data.GetValueOrDefault("releasedPassengers", 0).AsInt32();
		_landTimer = data.GetValueOrDefault("landTimer", 0.0).AsDouble();
	}
}
