using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDolphinriderGatlingPea : TowerDefenseZombie
{
	private StateHandle _jumpStateHandle;

	private StateHandle _runStateHandle;

	private bool _stateSignalsConnected;

	private AttackComponent _attackComponent2;

	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

	public bool halfHp;

	public bool isAttack;

	private bool _dolphin = true;

	public bool jumpMove;

	public bool isJump;

	public bool isJumpInWater;

	public bool isBlock;

	public bool audioPlay;

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
					_fireComponent.fireInterval = (float)value;
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
					_fireComponent.fireNum = value;
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
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public bool dolphin
	{
		get
		{
			return _dolphin;
		}
		set
		{
			_dolphin = value;
			if (!_dolphin)
			{
				base.useAttackDps = true;
				attackComponent.attackType = "Eat";
				base.useAttackDps = true;
				base.walkAnimeClip = "Walk";
				base.attackAnimeClip = "Eat";
				base.dieAnimeClip = "Death";
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
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_jumpStateHandle = base.StateMachine?.GetStateById("zombie.dolphinrider.jump");
		_runStateHandle = base.StateMachine?.GetStateById("zombie.dolphinrider.run");
		StateHandle jumpStateHandle = _jumpStateHandle;
		if (jumpStateHandle != null && jumpStateHandle.IsValid)
		{
			StateHandle runStateHandle = _runStateHandle;
			if (runStateHandle != null && runStateHandle.IsValid)
			{
				_jumpStateHandle.Entered += JumpEntered;
				_jumpStateHandle.Exited += JumpExited;
				_jumpStateHandle.PhysicsProcessing += JumpProcessing;
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_jumpStateHandle != null)
			{
				_jumpStateHandle.Entered -= JumpEntered;
				_jumpStateHandle.Exited -= JumpExited;
				_jumpStateHandle.PhysicsProcessing -= JumpProcessing;
			}
			_jumpStateHandle = null;
			if (_runStateHandle != null)
			{
				_runStateHandle.Entered -= RunEntered;
				_runStateHandle.Exited -= RunExited;
				_runStateHandle.PhysicsProcessing -= RunProcessing;
			}
			_runStateHandle = null;
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
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			base.sprite.OnAnimeStarted += AnimeStarted;
			ConnectStateSignals();
		}
	}

	public override void WalkEntered()
	{
		if (jumpMove)
		{
			jumpMove = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
		}
		else if (isBlock)
		{
			isBlock = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
		}
		else if (base.inWater)
		{
			base.sprite.SetAnimation(base.swimAnimeClip, loop: true, 0.2);
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (!audioPlay && (double)GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X < TowerDefenseManager.Instance.GetMapGroundRight())
		{
			AudioManager.Instance.AudioPlay("DolphinAppears");
			audioPlay = true;
		}
		if (!dolphin && attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target) && attackComponent.target is TowerDefenseVase)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
	}

	public override void Walk()
	{
		if (dolphin && base.inWater)
		{
			SendStateEvent("ToRun");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void RunEntered()
	{
		if (base.inWater)
		{
			if (!inSwimPlay && base.inSwimAnimeClip != "")
			{
				base.sprite.SetAnimation(base.inSwimAnimeClip, loop: false, 0.2);
				base.sprite.AddAnimation("DolphinRun", 0.0);
				inSwimPlay = true;
			}
			else
			{
				base.sprite.SetAnimation("DolphinRun", loop: true, 0.2);
			}
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		groundMoveComponent.SetAlive(true);
	}

	public void RunProcessing(double delta)
	{
		if (base.sprite.clip == base.inSwimAnimeClip)
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 1.0;
		}
		if (!nearDie && !TowerDefenseManager.Instance.backZombie && base.sprite.clip == "DolphinRun" && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target))
		{
			SendStateEvent("ToJump");
		}
	}

	public void RunExited()
	{
		groundMoveComponent.SetAlive(false);
	}

	public void JumpEntered()
	{
		shadowSprite.Visible = false;
		base.sprite.SetAnimation("DolphinJump", loop: false, 0.2);
		instance.collisionFlags = 0;
		instance.maskFlags = 0;
	}

	public void JumpProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!TowerDefenseManager.Instance.backZombie)
		{
			globalPositionForPhysicsFrame.X -= base.Scale.X * (float)delta * (float)base.sprite.timeScale * 16f;
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		if (isJump)
		{
			TowerDefenseCharacter towerDefenseCharacter = _attackComponent2?.target;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.isDestroy && towerDefenseCharacter.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
			{
				dolphin = false;
				globalPositionForPhysicsFrame.X = towerDefenseCharacter.GetLogicalGlobalPosition().X + 40f;
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
				base.waterHeight = 48.0;
				base.groundHeight = 0.0 - base.waterHeight;
				base.z = base.groundHeight;
				spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
				base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
				isBlock = true;
				AudioManager.Instance.AudioPlay("Bonk");
				Walk();
			}
		}
	}

	public void JumpExited()
	{
		isJump = false;
		if (!base.inWater)
		{
			shadowSprite.Visible = !base.invisible;
		}
		instance.collisionFlags = 1;
		instance.maskFlags = 1;
	}

	public override void InWater()
	{
		base.InWater();
		if (!dolphin)
		{
			base.sprite.offset = new Vector2(24f, -97f);
		}
		base.useAttackDps = true;
	}

	public override void OutWater()
	{
		base.OutWater();
		CreateTween().TweenProperty(base.sprite, "offset", new Vector2(-40f, -92f), 0.25);
		if (dolphin && !base.IsRemoteNetworkReplica)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 30f;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		base.useAttackDps = !dolphin;
		base.waterHeight = 0.0;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-40f, -92f);
		if (base.inWater)
		{
			base.waterHeight = 60.0;
			base.groundHeight = -60.0;
			base.z = -60.0;
			Tween tween = CreateTween();
			tween.SetParallel();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(this, "groundHeight", -100.0, 1.0);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		switch (command)
		{
		case "hit":
			if (!die && !nearDie && !base.sprite.pause)
			{
				attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			break;
		case "audio":
			AudioManager.Instance.AudioPlay("DolphinreforeJumping");
			break;
		case "check":
			isJump = true;
			break;
		case "jumpOver":
			dolphin = false;
			break;
		}
	}

	public void AnimeStarted(string clip)
	{
		if (clip == "DolphinRun")
		{
			base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "DolphinJump"))
		{
			if (clip == "JumpInWater")
			{
				if (!base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.backZombie)
				{
					Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
					logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 64f;
					SetLogicalGlobalPosition(logicalGlobalPosition);
				}
				base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
			}
		}
		else if (isJump)
		{
			jumpMove = true;
			base.waterHeight = 48.0;
			base.groundHeight = 0.0 - base.waterHeight;
			base.z = base.groundHeight;
			spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
			base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
			if (!base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.backZombie)
			{
				Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition();
				logicalGlobalPosition2.X -= base.Scale.X * transformPoint.Scale.X * 104f;
				SetLogicalGlobalPosition(logicalGlobalPosition2);
			}
			base.sprite.QueueRedraw();
			Walk();
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			DamagePartCreate("Head", base.sprite.GetNode<AdobeAnimateSpriteBase>("%Head"), new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			_fireComponent.alive = false;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "dolphin", dolphin },
			{ "jumpMove", jumpMove },
			{ "isJump", isJump },
			{ "isJumpInWater", isJumpInWater },
			{ "isBlock", isBlock }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		dolphin = data.GetValueOrDefault("dolphin", true).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", false).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		isJumpInWater = data.GetValueOrDefault("isJumpInWater", false).AsBool();
		isBlock = data.GetValueOrDefault("isBlock", false).AsBool();
	}

	public override bool CanBlock()
	{
		return dolphin;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override void Block(TowerDefenseCharacter target)
	{
		dolphin = false;
		base.waterHeight = 48.0;
		base.groundHeight = 0.0 - base.waterHeight;
		base.z = base.groundHeight;
		spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
		base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
		isBlock = true;
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantGatlingPea");
		if (cell.CanPacketPlant(packetConfig, noLimit: true))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos, playAudio: true, noLimit: true);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantGatlingPea", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
