using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGiftMachine : TowerDefensePlant
{
	private MagnetCoinComponent _magnetCoinComponent;

	private StateHandle _magnetState;

	private StateHandle _actionState;

	private bool _roleStateSignalsConnected;

	public bool hasCoin;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_magnetCoinComponent = componentManager.GetRuntime<MagnetCoinComponent>();
			if (_magnetCoinComponent != null)
			{
				_magnetCoinComponent.OnCoinGet += CoinGet;
			}
			_magnetState = base.StateMachine?.GetStateById("plant.gift_machine.magnet");
			_actionState = base.StateMachine?.GetStateById("plant.gift_machine.action");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		MagnetCoinComponent magnetCoinComponent = _magnetCoinComponent;
		if (magnetCoinComponent != null && !magnetCoinComponent.IsReleased)
		{
			_magnetCoinComponent.OnCoinGet -= CoinGet;
		}
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		StateHandle magnetState = _magnetState;
		if (magnetState != null && magnetState.IsValid)
		{
			StateHandle actionState = _actionState;
			if (actionState != null && actionState.IsValid)
			{
				_magnetState.Entered += MagnetEntered;
				_magnetState.Exited += MagnetExited;
				_magnetState.PhysicsProcessing += MagnetProcessing;
				_actionState.Entered += ActionEntered;
				_actionState.Exited += ActionExited;
				_actionState.PhysicsProcessing += ActionProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_magnetState.Entered -= MagnetEntered;
			_magnetState.Exited -= MagnetExited;
			_magnetState.PhysicsProcessing -= MagnetProcessing;
			_actionState.Entered -= ActionEntered;
			_actionState.Exited -= ActionExited;
			_actionState.PhysicsProcessing -= ActionProcessing;
			_magnetState = null;
			_actionState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		hasCoin = false;
		AddToGroup("GoldMagnet");
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		MagnetCoinComponent magnetCoinComponent = _magnetCoinComponent;
		if (magnetCoinComponent != null && magnetCoinComponent.CanCoinDraw())
		{
			SendStateEvent("ToMagnet");
		}
	}

	public override void IdleExited()
	{
		base.IdleExited();
		RemoveFromGroup("GoldMagnet");
	}

	public virtual void MagnetEntered()
	{
		base.sprite.SetAnimation("Attract", loop: false, 0.2);
	}

	public virtual void MagnetProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void MagnetExited()
	{
	}

	public virtual void ActionEntered()
	{
		base.sprite.SetAnimation("Loop", loop: false, 0.2);
		AudioManager.Instance.AudioPlay("Gift");
	}

	public virtual void ActionProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void ActionExited()
	{
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "action")
		{
			_magnetCoinComponent?.CoinDraw();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Attract"))
		{
			if (clip == "Loop")
			{
				AwardCreate();
				Idle();
			}
		}
		else if (hasCoin)
		{
			SendStateEvent("ToAction");
		}
		else
		{
			Idle();
		}
	}

	public void AwardCreate()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(spriteGroup);
		double num = GD.Randf();
		if (num < 0.5)
		{
			if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 15L);
			}
			else
			{
				SunCreate(logicalGlobalPosition, 15L);
			}
		}
		else if (num < 0.75)
		{
			if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 25L);
			}
			else
			{
				SunCreate(logicalGlobalPosition, 25L);
			}
		}
		else if (num < 0.85)
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantBYWZ");
			if (instance.hypnoses)
			{
				packetConfig.overrideHypnoses = true;
			}
			SpawnPacket(packetConfig, logicalGlobalPosition, 15.0, isFall: false);
		}
		else
		{
			TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("PlantPresentBox");
			if (instance.hypnoses)
			{
				packetConfig2.overrideHypnoses = true;
			}
			SpawnPacket(packetConfig2, logicalGlobalPosition, 15.0, isFall: false);
		}
	}

	public void CoinGet(TowerDefenseCoinBase coin)
	{
		hasCoin = true;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "hasCoin", hasCoin } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hasCoin = data.GetValueOrDefault("hasCoin", Variant.From<bool>(false)).AsBool();
	}
}
