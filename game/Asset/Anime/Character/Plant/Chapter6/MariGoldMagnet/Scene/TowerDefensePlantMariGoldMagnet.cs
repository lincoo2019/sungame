using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantMariGoldMagnet : TowerDefensePlant
{
	public ProduceComponent produceComponent;

	public MagnetCoinComponent magnetCoinComponent;

	public FireComponent fireComponent;

	private StateHandle _magnetState;

	private bool _roleStateSignalsConnected;

	private double _produceInterval = 25.0;

	private string _produceType = "Sun";

	public string skinName = "Default";

	public Array<int> coinNumList = new Array<int> { 0, 0, 0 };

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string produceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.produceType = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			produceComponent = componentManager.GetRuntime<ProduceComponent>();
			magnetCoinComponent = componentManager.GetRuntime<MagnetCoinComponent>();
			if (magnetCoinComponent != null)
			{
				magnetCoinComponent.OnCoinGet += CoinGet;
			}
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			AddToGroup("GoldMagnet");
			produceComponent.produceInterval = (float)produceInterval;
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Sycee";
			}
			_magnetState = base.StateMachine?.GetStateById("plant.mari_gold_magnet.magnet");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		MagnetCoinComponent obj = magnetCoinComponent;
		if (obj != null && !obj.IsReleased)
		{
			magnetCoinComponent.OnCoinGet -= CoinGet;
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

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Sycee";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		MagnetCoinComponent obj = magnetCoinComponent;
		if (obj != null && obj.CanCoinDraw())
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
			magnetCoinComponent?.CoinDraw();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Attack")
		{
			CreateProjectile();
			Idle();
		}
	}

	public virtual void CoinGet(TowerDefenseCoinBase coin)
	{
		switch (coin.num)
		{
		case 10:
			coinNumList[0]++;
			break;
		case 50:
			coinNumList[1]++;
			break;
		case 1000:
			coinNumList[2]++;
			break;
		}
	}

	public async void CreateProjectile()
	{
		if (!fireComponent.CanFireCheckOnceByName("CoinSilver", instance.collisionFlags))
		{
			return;
		}
		if (coinNumList[2] > 0)
		{
			for (int i = 0; i < 5; i++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinDiamond");
				towerDefenseProjectileCreateData.skinName = skinName;
				towerDefenseProjectileCreateData.baseDamage = 1000.0;
				towerDefenseProjectileCreateData.collisionFlags = 11;
				towerDefenseProjectileCreateData.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides);
				coinNumList[2]--;
				if (coinNumList[2] <= 0)
				{
					break;
				}
				await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
			}
		}
		else if (coinNumList[1] > 0)
		{
			for (int i = 0; i < 5; i++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinGold");
				towerDefenseProjectileCreateData.skinName = skinName;
				towerDefenseProjectileCreateData.baseDamage = 500.0;
				towerDefenseProjectileCreateData.collisionFlags = 11;
				towerDefenseProjectileCreateData.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides2 = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides2);
				coinNumList[1]--;
				if (coinNumList[1] <= 0)
				{
					break;
				}
				await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
			}
		}
		else
		{
			if (coinNumList[0] <= 0)
			{
				return;
			}
			for (int i = 0; i < 5; i++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinSilver");
				towerDefenseProjectileCreateData.skinName = skinName;
				towerDefenseProjectileCreateData.baseDamage = 100.0;
				towerDefenseProjectileCreateData.collisionFlags = 11;
				towerDefenseProjectileCreateData.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides3 = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides3);
				coinNumList[0]--;
				if (coinNumList[0] <= 0)
				{
					break;
				}
				await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "produceInterval", produceInterval },
			{ "skinName", skinName }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		skinName = data.GetValueOrDefault("skinName", "Default").AsString();
	}
}
