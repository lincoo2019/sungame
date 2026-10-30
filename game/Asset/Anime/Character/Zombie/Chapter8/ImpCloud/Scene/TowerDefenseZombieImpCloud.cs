using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieImpCloud : TowerDefenseZombie
{
	private const float SHOOT_INTERVAL = 8f;

	private const float SPIKE_BALL_FALL_TIME = 0.6f;

	private static PackedScene _IMITATER_CLOUD;

	private static PackedScene _CLOUD_SPRITE;

	private const string ARM_BONE_TEXTURE = "uid://bdvdk4frcvxu7";

	public double speed = 30.0;

	private double _shootTimer;

	private bool _isShooting;

	private StateHandle _flyStateHandle;

	private StateHandle _flyAttackStateHandle;

	private bool _roleStateSignalsConnected;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	private static PackedScene CLOUD_SPRITE => _CLOUD_SPRITE ?? (_CLOUD_SPRITE = GD.Load<PackedScene>("uid://csds4e7ibk87a"));

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_flyStateHandle = base.StateMachine?.GetStateById("zombie.imp_cloud.fly");
		_flyAttackStateHandle = base.StateMachine?.GetStateById("zombie.imp_cloud.fly_attack");
		StateHandle flyStateHandle = _flyStateHandle;
		if (flyStateHandle != null && flyStateHandle.IsValid)
		{
			StateHandle flyAttackStateHandle = _flyAttackStateHandle;
			if (flyAttackStateHandle != null && flyAttackStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.Exited += FlyExited;
				_flyStateHandle.PhysicsProcessing += FlyProcessing;
				_flyAttackStateHandle.Entered += FlyAttackEntered;
				_flyAttackStateHandle.Exited += FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing += FlyAttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_flyStateHandle != null)
			{
				_flyStateHandle.Entered -= FlyEntered;
				_flyStateHandle.Exited -= FlyExited;
				_flyStateHandle.PhysicsProcessing -= FlyProcessing;
			}
			_flyStateHandle = null;
			if (_flyAttackStateHandle != null)
			{
				_flyAttackStateHandle.Entered -= FlyAttackEntered;
				_flyAttackStateHandle.Exited -= FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing -= FlyAttackProcessing;
			}
			_flyAttackStateHandle = null;
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
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !TowerDefenseManager.Instance.IsGameRunning() || !inGame || !base.IsInsideComponentBattlefield || die || nearDie || _isShooting || base.sprite.pause)
		{
			return;
		}
		if (_shootTimer < 8.0)
		{
			_shootTimer += delta * timeScale;
			return;
		}
		_shootTimer = 0.0;
		if (!attackComponent.CanAttack())
		{
			_isShooting = true;
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		}
	}

	public void FlyEntered()
	{
		_isShooting = false;
		base.sprite.SetAnimation("Idle", loop: true, 0.2);
	}

	public void FlyProcessing(double delta)
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			base.sprite.timeScale = timeScale;
			if (!base.sprite.pause && !_isShooting)
			{
				ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
				Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				globalPositionForPhysicsFrame.X -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (!base.sprite.pause && !_isShooting && attackComponent.CanAttack())
			{
				SendStateEvent("ToFlyAttack");
			}
		}
	}

	public void FlyExited()
	{
	}

	public void FlyAttackEntered()
	{
		base.sprite.SetAnimation("Eat", loop: true, 0.2);
		ActivateAttackAfterStateDelay(_flyAttackStateHandle);
	}

	public void FlyAttackProcessing(double delta)
	{
		if (!attackComponent.CanAttack())
		{
			SendStateEvent("ToFly");
		}
		else if (startAttack && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps)
		{
			attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
		}
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void FlyAttackExited()
	{
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
		BlowBack(1.0, 1.0);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			ThrowSpikeBall();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Shooting")
		{
			_isShooting = false;
			if (!die)
			{
				Walk();
			}
		}
	}

	private void ThrowSpikeBall()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			Node2D ball = CreateSpikeBallVisual();
			ball.GlobalPosition = GetLogicalGlobalPosition(base.sprite) + new Vector2(0f, -20f);
			TowerDefenseGroundItemBase.characterNode.AddChild(ball, forceReadableName: false, InternalMode.Disabled);
			float y = TowerDefenseManager.GetMapCellPlantPos(base.gridPos).Y;
			bool cachedInGame = inGame;
			bool cachedHypnoses = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(ball, "global_position:y", y, 0.6000000238418579);
			tween.TweenCallback(Callable.From(delegate
			{
				OnSpikeBallLanded(ball, cachedInGame, cachedHypnoses);
			}));
		}
	}

	private Node2D CreateSpikeBallVisual()
	{
		AdobeAnimateData flashAnimeData = base.sprite.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData) || !flashAnimeData.mediaDictionary.TryGetValue("Zombie_cloud_ball.png", out var value))
		{
			return new Node2D();
		}
		int num = value.AsInt32();
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(flashAnimeData);
		if (orBuild == null || num < 0 || num >= orBuild.MediaRects.Length)
		{
			return new Node2D();
		}
		Vector2 size = orBuild.MediaRects[num].Size;
		Array item = new Array
		{
			num,
			Transform2D.Identity.Translated(-size / 2f),
			Colors.White
		};
		return new AdobeAnimatePart
		{
			flashAnimeData = flashAnimeData,
			mediaReplace = base.sprite.CreateActiveMediaReplaceSnapshotForRender(),
			offset = Vector2.Zero,
			elementList = new Array<Array> { item }
		};
	}

	private void OnSpikeBallLanded(Node2D ball, bool cachedInGame, bool cachedHypnoses)
	{
		if (!GodotObject.IsInstanceValid(ball))
		{
			return;
		}
		Vector2 globalPosition = ball.GlobalPosition;
		Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPosition);
		ball.QueueFree();
		if (!TowerDefenseManager.Instance.IsGameRunning() || !cachedInGame)
		{
			return;
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, mapGridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = globalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieImpDiggerSpike");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(mapGridPos);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter.SetLogicalGlobalPosition(globalPosition);
			if (cachedHypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Arm")
		{
			base.sprite.SetFliter("lower2", open: false);
			base.sprite.SetAtlasReplace("Zombie_cloud_arm1.png", "uid://bdvdk4frcvxu7");
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		CreateFleeingCloud();
		HideCloudLayers();
	}

	private void HideCloudLayers()
	{
		base.sprite.SetFliters(new Array { "front" }, open: false);
	}

	private void CreateFleeingCloud()
	{
		if (!Engine.IsEditorHint())
		{
			Node node = CLOUD_SPRITE.Instantiate(PackedScene.GenEditState.Disabled);
			TowerDefenseGroundItemBase.characterNode.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			Node2D cloudNode2D = (Node2D)node;
			cloudNode2D.GlobalPosition = GetLogicalGlobalPosition(base.sprite);
			cloudNode2D.Scale = transformPoint.Scale * base.Scale;
			if (node is AdobeAnimateSpriteBase adobeAnimateSpriteBase)
			{
				adobeAnimateSpriteBase.SetAnimation("Flee", loop: true, 0.2);
				adobeAnimateSpriteBase.SetFliters(new Array
				{
					"Zombie_imp_innerarm_upper", "Zombie_imp_innerarm_lower", "Zombie_imp_innerleg_foot", "Zombie_imp_innerleg_lower", "Zombie_imp_innerleg_upper", "Zombie_imp_body2", "Zombie_imp_body1", "Zombie_imp_outerleg_foot", "Zombie_imp_outerleg_lower", "Zombie_imp_outerleg_upper",
					"Zombie_imp_outerarm_upper", "Zombie_outerarm_lower", "anim_head1", "anim_head2", "glasses", "ball", "ball2", "spike1", "spike2", "spike3",
					"spike4", "spike5", "spike6", "spike7", "spike8", "spike9"
				}, open: false);
			}
			Tween tween = GetTree().CreateTween();
			float num = ((!((((double)GD.Randf() > 0.5) ? 1f : (-1f)) > 0f)) ? ((float)(TowerDefenseManager.Instance.GetMapGroundLeft() - 200.0)) : ((float)(TowerDefenseManager.Instance.GetMapGroundRight() + 200.0)));
			float num2 = cloudNode2D.GlobalPosition.Y - 500f;
			double duration = 2.5;
			tween.SetParallel();
			tween.TweenProperty(cloudNode2D, "global_position:x", num, duration).SetEase(Tween.EaseType.In);
			tween.TweenProperty(cloudNode2D, "global_position:y", num2, duration).SetEase(Tween.EaseType.In);
			tween.Chain().TweenCallback(Callable.From(delegate
			{
				cloudNode2D.QueueFree();
			}));
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "speed", speed },
			{ "shootTimer", _shootTimer }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		speed = data.GetValueOrDefault("speed", 30.0).AsDouble();
		_shootTimer = data.GetValueOrDefault("shootTimer", 0.0).AsDouble();
	}
}
