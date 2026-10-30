using Godot;

[Tool]
public partial class TowerDefenseZombieImphacker : TowerDefenseZombieImpBase
{
	private StateHandle _hackStateHandle;

	private bool _stateSignalsConnected;

	private Timer _hackTimer;

	public bool isHack;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_hackStateHandle = base.StateMachine?.GetStateById("zombie.imphacker.hack");
			StateHandle hackStateHandle = _hackStateHandle;
			if (hackStateHandle != null && hackStateHandle.IsValid)
			{
				_hackStateHandle.Entered += HackEntered;
				_hackStateHandle.Exited += HackExited;
				_hackStateHandle.Processing += HackProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_hackStateHandle != null)
			{
				_hackStateHandle.Entered -= HackEntered;
				_hackStateHandle.Exited -= HackExited;
				_hackStateHandle.Processing -= HackProcessing;
			}
			_hackStateHandle = null;
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
			_hackTimer = GetNode<Timer>("HackTimer");
			_hackTimer.Timeout += HackTimerTimeout;
			ConnectStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && base.IsInsideComponentBattlefield)
		{
			if (!isHack && _hackTimer.IsStopped())
			{
				_hackTimer.Start(10.0);
			}
			if (!instance.canBeCollection)
			{
				Color meshColor = base.sprite.meshColor;
				base.sprite.meshColor = new Color(meshColor)
				{
					A = (float)((double)meshColor.A * 0.5)
				};
			}
		}
	}

	public void HackEntered()
	{
		base.sprite.SetAnimation("Hack", loop: false, 0.2);
		isHack = true;
	}

	public void HackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void HackExited()
	{
		isHack = false;
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
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

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Hack")
		{
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "hack")
		{
			Hack();
		}
	}

	public async void Hack()
	{
		instance.canBeCollection = false;
		instance.collisionFlags = 0;
		SetPacket();
		await ToSignal(GetTree().CreateTimer(4.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		instance.canBeCollection = true;
		instance.collisionFlags = 1;
	}

	public void SetPacket()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return;
		}
		TowerDefenseInGameSeedBank seedBank = towerDefenseManager.GetSeedBank();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieImp");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = null;
		if (instance.hypnoses || towerDefenseManager.IsIZMMode())
		{
			packetConfig.overrideCost = 0;
			towerDefensePacketOverride = new TowerDefensePacketOverride();
			towerDefensePacketOverride.useSucceededActions.Add(new CardActionBehaviorCreditSun
			{
				amount = 50L
			});
			packetConfig._override = towerDefensePacketOverride;
		}
		switch (towerDefenseManager.GetCurrentPacketBankMethod())
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			if (GodotObject.IsInstanceValid(seedBank) && seedBank.packetList.Count > 1)
			{
				seedBank.packetList[GD.RandRange(0, seedBank.packetList.Count - 1)].Cover(packetConfig, towerDefensePacketOverride);
			}
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR:
		{
			TowerDefenseBattleFeatureConveyorBelt conveyorBeltFeature = towerDefenseManager.GetConveyorBeltFeature();
			if (GodotObject.IsInstanceValid(conveyorBeltFeature))
			{
				conveyorBeltFeature.SpawnPacket(packetConfig);
			}
			break;
		}
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN:
		{
			TowerDefenseBattleFeatureRainMode rainModeFeature = towerDefenseManager.GetRainModeFeature();
			if (GodotObject.IsInstanceValid(rainModeFeature))
			{
				rainModeFeature.SpawnPacket(packetConfig);
			}
			break;
		}
		}
	}

	public void HackTimerTimeout()
	{
		if (!die && !nearDie && base.IsInsideComponentBattlefield)
		{
			SendStateEvent("ToHack");
		}
	}
}
