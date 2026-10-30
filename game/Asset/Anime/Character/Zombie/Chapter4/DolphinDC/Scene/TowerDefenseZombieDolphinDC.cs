using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDolphinDC : TowerDefenseZombie, IDancer, INetworkSpawnStateReceiver
{
	private AttackComponent _attackComponent2;

	private TowerDefenseCharacter _jackson;

	private string _pendingJacksonName = "";

	private bool _hasPendingNetworkSpawnState;

	private bool _pendingSpawnDolphin = true;

	private bool _pendingSpawnInSwimPlay;

	private bool _dolphin = true;

	public bool jumpMove;

	public bool isJump;

	public bool isJumpInWater;

	public bool isBlock;

	public bool audioPlay;

	private StateHandle _jumpStateHandle;

	private StateHandle _runStateHandle;

	private bool _roleStateSignalsConnected;

	public bool dolphin
	{
		get
		{
			return _dolphin;
		}
		set
		{
			_dolphin = value;
			ApplyDolphinAnimationContract();
			if (IsNodeReady())
			{
				ApplyDolphinRuntimeState();
			}
		}
	}

	public TowerDefenseCharacter GetJackson()
	{
		return _jackson;
	}

	public void SetJackson(TowerDefenseCharacter value)
	{
		_jackson = value;
	}

	private void ApplyDolphinAnimationContract()
	{
		if (!_dolphin)
		{
			base.useAttackDps = true;
			idleAnimeClip = "Walk";
			base.walkAnimeClip = "Walk";
			base.attackAnimeClip = "Eat";
			base.dieAnimeClip = "Death";
		}
	}

	private void ApplyDolphinRuntimeState()
	{
		ApplyDolphinAnimationContract();
		if (!_dolphin)
		{
			AttackComponent obj = attackComponent;
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(spriteGroup) && GodotObject.IsInstanceValid(base.sprite))
			{
				attackComponent.attackType = "Eat";
				base.waterHeight = 48.0;
				base.groundHeight = 0.0 - base.waterHeight;
				base.z = base.groundHeight;
				spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
				base.sprite.offset = new Vector2(-40f, -97f);
			}
		}
	}

	private void ApplyPendingNetworkSpawnState()
	{
		if (_hasPendingNetworkSpawnState)
		{
			_dolphin = _pendingSpawnDolphin;
			inSwimPlay = _pendingSpawnInSwimPlay;
			_hasPendingNetworkSpawnState = false;
		}
		ApplyDolphinRuntimeState();
	}

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
		_jumpStateHandle = base.StateMachine?.GetStateById("zombie.dolphin_dc.jump");
		_runStateHandle = base.StateMachine?.GetStateById("zombie.dolphin_dc.run");
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
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
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
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		if (IsQueuedForDeletion())
		{
			OutJackson();
		}
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			base.sprite.OnAnimeStarted += AnimeStarted;
			ApplyPendingNetworkSpawnState();
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_pendingJacksonName == "")
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			TowerDefenseCharacter nodeOrNull = node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(_pendingJacksonName));
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				_pendingJacksonName = "";
				SetJackson(nodeOrNull);
			}
		}
	}

	public override void WalkEntered()
	{
		inSwimPlay = false;
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
				base.sprite.SetAnimation("DolphinRun");
				inSwimPlay = false;
			}
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip);
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
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
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
			logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 40f;
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
			base.waterHeight = 35.0;
			base.groundHeight = 0.0 - base.waterHeight;
			base.z = base.groundHeight;
			spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
			base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
			if (!base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.backZombie)
			{
				Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition();
				logicalGlobalPosition2.X -= base.Scale.X * transformPoint.Scale.X * 124f;
				SetLogicalGlobalPosition(logicalGlobalPosition2);
			}
			base.sprite.QueueRedraw();
			Walk();
		}
	}

	public override async void Rise(double duration = -1.0, double delay = 0.0, bool createDirt = true, bool changeState = true, double from = 150.0, bool emitRiseOver = true)
	{
		if (duration < 0.0)
		{
			duration = GD.RandRange(0.4, 0.6);
		}
		base.Rise(duration, delay, createDirt, changeState, from, emitRiseOver);
		TaskCompletionSource<bool> tcs;
		if (!changeState)
		{
			tcs = new TaskCompletionSource<bool>();
			base.OnRiseOver += RiseOverHandler;
			try
			{
				await tcs.Task;
			}
			finally
			{
				base.OnRiseOver -= RiseOverHandler;
			}
			Walk();
		}
		void RiseOverHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "dolphin", dolphin },
			{ "jumpMove", jumpMove },
			{ "isJump", isJump },
			{ "isJumpInWater", isJumpInWater },
			{ "isBlock", isBlock }
		};
		if (GodotObject.IsInstanceValid(_jackson))
		{
			dictionary["jacksonNodeName"] = _jackson.Name;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		dolphin = data.GetValueOrDefault("dolphin", true).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", false).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		isJumpInWater = data.GetValueOrDefault("isJumpInWater", false).AsBool();
		isBlock = data.GetValueOrDefault("isBlock", false).AsBool();
		_pendingJacksonName = data.GetValueOrDefault("jacksonNodeName", "").AsString();
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		_pendingSpawnDolphin = data.GetValueOrDefault("dolphin", dolphin).AsBool();
		_pendingSpawnInSwimPlay = data.GetValueOrDefault("in_swim_play", inSwimPlay).AsBool();
		_hasPendingNetworkSpawnState = true;
		if (IsNodeReady())
		{
			ApplyPendingNetworkSpawnState();
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["dolphin"] = dolphin,
			["inSwimPlay"] = inSwimPlay
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		dolphin = data.GetValueOrDefault("dolphin", dolphin).AsBool();
		inSwimPlay = data.GetValueOrDefault("inSwimPlay", inSwimPlay).AsBool();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return (dolphin ? 1 : 0) | (inSwimPlay ? 2 : 0);
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		OutJackson();
	}

	public void OutJackson()
	{
		if (GodotObject.IsInstanceValid(_jackson) && _jackson is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie.instance) && !towerDefenseZombie.instance.hypnoses && _jackson is IJackson jackson)
		{
			jackson.RemoveDancer(this);
		}
		_jackson = null;
		_pendingJacksonName = "";
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
}
