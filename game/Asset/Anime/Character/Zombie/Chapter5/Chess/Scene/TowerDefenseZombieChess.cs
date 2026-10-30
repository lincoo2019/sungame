using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieChess : TowerDefenseZombie
{
	private static PackedScene _IMITATER_CLOUD;

	private AttackComponent _attackComponent2;

	public bool isJump;

	private bool _hasPogo = true;

	public bool pogoPlant;

	public double jumpToPos;

	public int jumpWait = 1;

	private StateHandle _pogoStateHandle;

	private bool _roleStateSignalsConnected;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public bool hasPogo
	{
		get
		{
			return _hasPogo;
		}
		set
		{
			_hasPogo = value;
			if (!_hasPogo)
			{
				idleAnimeClip = "Idle";
			}
		}
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
			_pogoStateHandle = base.StateMachine?.GetStateById("zombie.chess.pogo");
			StateHandle pogoStateHandle = _pogoStateHandle;
			if (pogoStateHandle != null && pogoStateHandle.IsValid)
			{
				_pogoStateHandle.Entered += PogoEntered;
				_pogoStateHandle.Exited += PogoExited;
				_pogoStateHandle.PhysicsProcessing += PogoProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_pogoStateHandle != null)
			{
				_pogoStateHandle.Entered -= PogoEntered;
				_pogoStateHandle.Exited -= PogoExited;
				_pogoStateHandle.PhysicsProcessing -= PogoProcessing;
			}
			_pogoStateHandle = null;
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
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			base.OnLand += Land;
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			ySpeed = -300.0;
			if (TowerDefenseManager.Instance.IsGameRunning() && inGame)
			{
				InitializeGameplayPresentation();
			}
		}
	}

	private void InitializeGameplayPresentation()
	{
		base.Scale = new Vector2(0f - Mathf.Abs(base.Scale.X), base.Scale.Y);
		ConnectRoleStateSignals();
	}

	public override void Spawn()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		logicalGlobalPosition.X = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(5, 0)).X;
		SetLogicalGlobalPosition(logicalGlobalPosition);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public override void PreSpawn()
	{
		base.Scale = new Vector2(0f - Mathf.Abs(base.Scale.X), base.Scale.Y);
	}

	public override void Walk()
	{
		if (hasPogo)
		{
			SendStateEvent("ToPogo");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void PogoEntered()
	{
		base.sprite.SetAnimation("Pogo", loop: true, 0.2);
		if (isGround)
		{
			ySpeed = -300.0;
		}
	}

	public void PogoProcessing(double delta)
	{
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!pogoPlant)
		{
			if (!base.sprite.pause)
			{
				double num = (((double)globalPositionForPhysicsFrame.X > groundRight) ? 2.0 : 1.0);
				globalPositionForPhysicsFrame.X -= (float)(30.0 * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * num * (double)((!base.sprite.playBack) ? 1 : (-1)));
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (!base.sprite.pause && attackComponent.CanAttack())
			{
				pogoPlant = true;
				jumpToPos = globalPositionForPhysicsFrame.X - TowerDefenseManager.Instance.GetMapGridSize().X * base.Scale.X - 10f * base.Scale.X;
			}
		}
		else if (isJump && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target) && _attackComponent2.target.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
		{
			instance.ArmorDelete("Pogo");
			hasPogo = false;
			isJump = false;
			AudioManager.Instance.AudioPlay("Bonk");
			Walk();
		}
	}

	public void PogoExited()
	{
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pogo")
		{
			instance.unUseBuffFlags = 0;
			isJump = false;
			hasPogo = false;
			Walk();
		}
	}

	public override void OutWater()
	{
		base.OutWater();
		if (hasPogo)
		{
			ySpeed = -300.0;
		}
	}

	public async void Land()
	{
		if (!hasPogo)
		{
			return;
		}
		isJump = false;
		gravity = 490.0;
		ySpeed = -300.0;
		if (!base.IsRemoteNetworkReplica && pogoPlant)
		{
			isJump = true;
			if (jumpWait > 0)
			{
				jumpWait--;
			}
			else
			{
				jumpWait = 1;
				Tween tween = CreateTween();
				tween.SetEase(Tween.EaseType.InOut);
				tween.SetTrans(Tween.TransitionType.Sine);
				Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
				tween.TweenMethod(to: new Vector2((float)jumpToPos, logicalGlobalPosition.Y), method: Callable.From<Vector2>(base.SetLogicalGlobalPosition), from: logicalGlobalPosition, duration: 0.5);
				ySpeed = -400.0;
				await ToSignal(tween, Tween.SignalName.Finished);
				pogoPlant = false;
			}
			await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			isJump = false;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "pogo"))
		{
			_ = command == "jump_check";
		}
		else if (ySpeed == 0.0)
		{
			Land();
		}
	}

	public override bool CanBlock()
	{
		return hasPogo;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override void Block(TowerDefenseCharacter target)
	{
		instance.ArmorDelete("Pogo");
		hasPogo = false;
		isJump = false;
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}

	public override void DestroySet()
	{
		base.DestroySet();
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (!((double)logicalGlobalPosition.X > groundRight + 150.0))
		{
			return;
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData("ChessZombie");
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetBankData.GetZombieList()[(int)(GD.Randi() % packetBankData.GetZombieList().Count)].AsString());
		TowerDefenseCharacter character = packetConfig.Create(logicalGlobalPosition, base.gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(character, forceReadableName: false, InternalMode.Disabled);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(character))
			{
				if (GodotObject.IsInstanceValid(character.instance))
				{
					character.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(character.transformPoint))
				{
					character.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.Hypnoses();
				}
			}).CallDeferred();
		}
		character.CallDeferred("Walk");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "isJump", isJump },
			{ "hasPogo", hasPogo },
			{ "pogoPlant", pogoPlant },
			{ "jumpToPos", jumpToPos },
			{ "jumpWait", jumpWait }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		hasPogo = data.GetValueOrDefault("hasPogo", true).AsBool();
		pogoPlant = data.GetValueOrDefault("pogoPlant", false).AsBool();
		jumpToPos = data.GetValueOrDefault("jumpToPos", 0.0).AsDouble();
		jumpWait = data.GetValueOrDefault("jumpWait", 1).AsInt32();
	}
}
