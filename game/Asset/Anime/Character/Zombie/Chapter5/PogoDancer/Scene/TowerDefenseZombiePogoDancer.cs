using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePogoDancer : TowerDefenseZombie, IDancer, INetworkSpawnStateReceiver
{
	private AttackComponent _attackComponent2;

	public bool isJump;

	private bool _hasPogo = true;

	public bool pogoPlant;

	public double jumpToPos;

	public int jumpWait = 1;

	public int walkTime = 2;

	public int danceTime = 3;

	public TowerDefenseZombie jackson;

	private string _pendingJacksonName = "";

	private StateHandle _danceStateHandle;

	private StateHandle _pogoStateHandle;

	private bool _roleStateSignalsConnected;

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
				idleAnimeClip = "Walk";
			}
		}
	}

	public TowerDefenseCharacter GetJackson()
	{
		return jackson;
	}

	public void SetJackson(TowerDefenseCharacter value)
	{
		jackson = value as TowerDefenseZombie;
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		base.z = data.GetValueOrDefault("spawn_z", base.z).AsDouble();
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
		_danceStateHandle = base.StateMachine?.GetStateById("zombie.pogo_dancer.dance");
		_pogoStateHandle = base.StateMachine?.GetStateById("zombie.pogo_dancer.pogo");
		StateHandle danceStateHandle = _danceStateHandle;
		if (danceStateHandle != null && danceStateHandle.IsValid)
		{
			StateHandle pogoStateHandle = _pogoStateHandle;
			if (pogoStateHandle != null && pogoStateHandle.IsValid)
			{
				_danceStateHandle.Entered += DanceEntered;
				_danceStateHandle.Exited += DanceExited;
				_danceStateHandle.PhysicsProcessing += DanceProcessing;
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
			if (_danceStateHandle != null)
			{
				_danceStateHandle.Entered -= DanceEntered;
				_danceStateHandle.Exited -= DanceExited;
				_danceStateHandle.PhysicsProcessing -= DanceProcessing;
			}
			_danceStateHandle = null;
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
		if (!Engine.IsEditorHint())
		{
			base.OnLand += Land;
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			ySpeed = -300.0;
			ConnectRoleStateSignals();
		}
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
	}

	public void PogoProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
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

	public void DanceEntered()
	{
		danceTime = 3;
		base.sprite.Scale = new Vector2(-1f, base.sprite.Scale.Y);
		base.sprite.SetAnimation("ArmRise", loop: true, 0.2);
	}

	public void DanceProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (!base.sprite.pause && attackComponent.CanAttack())
		{
			Attack();
		}
	}

	public void DanceExited()
	{
		base.sprite.Scale = new Vector2(1f, base.sprite.Scale.Y);
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		base.sprite.Scale = new Vector2(1f, base.sprite.Scale.Y);
		walkTime = 2;
	}

	public override void WalkProcessing(double delta)
	{
		if (GodotObject.IsInstanceValid(jackson))
		{
			groundMoveComponent.SetAlive(jackson.groundMoveComponent?.Alive ?? false);
		}
		else
		{
			groundMoveComponent.SetAlive(true);
		}
		base.WalkProcessing(delta);
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if ((clip == "Walk" || clip == "ArmRise") && clip != base.sprite.clip)
		{
			return;
		}
		if (!(clip == "Walk"))
		{
			if (!(clip == "ArmRise"))
			{
				return;
			}
			base.sprite.Scale = new Vector2(0f - base.sprite.Scale.X, base.sprite.Scale.Y);
			danceTime--;
			if (!die && !nearDie)
			{
				if (danceTime > 0)
				{
					return;
				}
				if (GodotObject.IsInstanceValid(jackson))
				{
					if (!GodotObject.IsInstanceValid(jackson))
					{
						return;
					}
					GroundMoveComponent obj = jackson.groundMoveComponent;
					if (obj != null && obj.Alive)
					{
						return;
					}
				}
				Walk();
			}
			else
			{
				OutJackson();
				Die();
			}
		}
		else
		{
			if (TowerDefenseManager.Instance.currentControl != null && !TowerDefenseManager.Instance.currentControl.isGameRunning)
			{
				return;
			}
			walkTime--;
			if (!die && !nearDie)
			{
				if (walkTime > 0)
				{
					return;
				}
				if (GodotObject.IsInstanceValid(jackson))
				{
					if (!GodotObject.IsInstanceValid(jackson))
					{
						return;
					}
					GroundMoveComponent obj2 = jackson.groundMoveComponent;
					if (obj2 != null && obj2.Alive)
					{
						return;
					}
				}
				SendStateEvent("ToDance");
			}
			else
			{
				OutJackson();
				Die();
			}
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pogo")
		{
			instance.unUseBuffFlags = 0;
			isJump = false;
			hasPogo = false;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
			if (base.inWater)
			{
				base.groundHeight = 0.0 - base.waterHeight;
			}
			Walk();
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		OutJackson();
	}

	public void OutJackson()
	{
		if (GodotObject.IsInstanceValid(this.jackson))
		{
			if (!this.jackson.instance.hypnoses && this.jackson is IJackson jackson)
			{
				jackson.RemoveDancer(this);
			}
			this.jackson = null;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary();
		if (GodotObject.IsInstanceValid(jackson))
		{
			dictionary["jacksonNodeName"] = jackson.Name;
		}
		dictionary["hasPogo"] = hasPogo;
		dictionary["isJump"] = isJump;
		dictionary["pogoPlant"] = pogoPlant;
		dictionary["jumpToPos"] = jumpToPos;
		dictionary["jumpWait"] = jumpWait;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		if (data.ContainsKey("jacksonNodeName"))
		{
			_pendingJacksonName = data["jacksonNodeName"].AsString();
		}
		hasPogo = data.GetValueOrDefault("hasPogo", true).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		pogoPlant = data.GetValueOrDefault("pogoPlant", false).AsBool();
		jumpToPos = data.GetValueOrDefault("jumpToPos", 0.0).AsDouble();
		jumpWait = data.GetValueOrDefault("jumpWait", 1).AsInt32();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!(_pendingJacksonName != ""))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			Node nodeOrNull = node2D.GetNodeOrNull(_pendingJacksonName);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				jackson = nodeOrNull as TowerDefenseZombie;
			}
		}
		_pendingJacksonName = "";
	}

	public override void InWater()
	{
		base.InWater();
		if (hasPogo)
		{
			base.groundHeight = 0.0;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = false;
			}
		}
	}

	public override void OutWater()
	{
		if (hasPogo)
		{
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
		}
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
		GroundHeightComponent obj = groundHeightComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundHeightComponent.handleWaterHeight = true;
		}
		if (base.inWater)
		{
			base.groundHeight = 0.0 - base.waterHeight;
		}
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}
}
