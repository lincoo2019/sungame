using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGoldMagnet : TowerDefensePlant
{
	public MagnetCoinComponent _magnetCoinComponent;

	private StateHandle _magnetState;

	private bool _roleStateSignalsConnected;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_magnetCoinComponent = componentManager.GetRuntime<MagnetCoinComponent>();
			if (_magnetCoinComponent != null)
			{
				_magnetCoinComponent.OnCoinGet += CoinGet;
			}
			AddToGroup("GoldMagnet");
			_magnetState = base.StateMachine?.GetStateById("plant.gold_magnet.magnet");
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
		if (!_roleStateSignalsConnected)
		{
			StateHandle magnetState = _magnetState;
			if (magnetState != null && magnetState.IsValid)
			{
				_magnetState.Entered += MagnetEntered;
				_magnetState.Exited += MagnetExited;
				_magnetState.PhysicsProcessing += MagnetProcessing;
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
			_magnetState = null;
			_roleStateSignalsConnected = false;
		}
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

	public virtual void MagnetEntered()
	{
		base.sprite.SetAnimation("Attack", loop: false, 0.2);
	}

	public virtual void MagnetProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void MagnetExited()
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
		if (clip == "Attack")
		{
			Idle();
		}
	}

	public virtual void CoinGet(TowerDefenseCoinBase coin)
	{
		coin.Collection();
	}
}
