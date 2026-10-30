using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGoldPea : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private MagnetCoinComponent _magnetCoinComponent;

	private FireComponent _fireComponent;

	private StateHandle _magnetState;

	private bool _roleStateSignalsConnected;

	public int[] coinNumList = new int[3];

	private double _produceInterval = 25.0;

	private string _produceType = "Sun";

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "GoldPea";

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
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,QXSun")]
	public string produceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceType = value;
				}
			}
		}
	}

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
			if (IsNodeReady() && _fireComponent != null)
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
			if (IsNodeReady() && _fireComponent != null)
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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_magnetCoinComponent = componentManager.GetRuntime<MagnetCoinComponent>();
			if (_magnetCoinComponent != null)
			{
				_magnetCoinComponent.OnCoinGet += CoinGet;
			}
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_produceComponent.produceInterval = (float)produceInterval;
			_fireComponent.fireInterval = (float)fireInterval;
			_magnetState = base.StateMachine?.GetStateById("plant.gold_pea.magnet");
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

	public override void IdleEntered()
	{
		base.IdleEntered();
		_fireComponent.alive = true;
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
	}

	public void MagnetEntered()
	{
		_fireComponent.alive = false;
		base.sprite.SetAnimation("Attract", loop: false, 0.2);
	}

	public void MagnetProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void MagnetExited()
	{
		_fireComponent.alive = true;
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
		if (clip == "Attract")
		{
			CreateProjectile();
			Idle();
		}
	}

	public void CoinGet(TowerDefenseCoinBase coin)
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
		if (coinNumList[2] > 0)
		{
			for (int i = 0; i < coinNumList[2]; i++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinDiamond");
				towerDefenseProjectileCreateData.baseDamage = 1000.0;
				towerDefenseProjectileCreateData.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData, instance.collisionFlags | 2, base.camp, Vector2.Zero, overrides);
			}
		}
		if (coinNumList[1] > 0)
		{
			for (int j = 0; j < coinNumList[1]; j++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData2 = new TowerDefenseProjectileCreateData("CoinGold");
				towerDefenseProjectileCreateData2.baseDamage = 500.0;
				towerDefenseProjectileCreateData2.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides2 = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData2, instance.collisionFlags | 2, base.camp, Vector2.Zero, overrides2);
			}
		}
		if (coinNumList[0] > 0)
		{
			for (int k = 0; k < coinNumList[0]; k++)
			{
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData3 = new TowerDefenseProjectileCreateData("CoinSilver");
				towerDefenseProjectileCreateData3.baseDamage = 100.0;
				towerDefenseProjectileCreateData3.fireMethodFlags = 32;
				BulletFieldSpawnOverrides overrides3 = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData3, instance.collisionFlags | 2, base.camp, Vector2.Zero, overrides3);
			}
		}
		coinNumList = new int[3];
		await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["produceInterval"] = produceInterval,
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "GoldPea");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
