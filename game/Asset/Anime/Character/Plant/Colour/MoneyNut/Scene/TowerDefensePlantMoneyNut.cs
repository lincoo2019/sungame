using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMoneyNut : TowerDefensePlant
{
	private MagnetCoinComponent _magnetCoinComponent;

	private StateHandle _magnetState;

	private bool _roleStateSignalsConnected;

	public int[] coinNumList = new int[3];

	public bool over;

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
			_magnetState = base.StateMachine?.GetStateById("plant.money_nut.magnet");
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

	public void MagnetEntered()
	{
		base.sprite.SetAnimation("Action", loop: false, 0.2);
	}

	public void MagnetProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void MagnetExited()
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
		if (clip == "Action")
		{
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

	public void CreateProjectile()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
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
				FireComponent.CreateProjectilePositionByData(null, null, 0.0, logicalGlobalPosition, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides);
			}
			coinNumList[2] = 0;
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
				FireComponent.CreateProjectilePositionByData(null, null, 0.0, logicalGlobalPosition, new Vector2(300f, 0f), towerDefenseProjectileCreateData2, -1, base.camp, Vector2.Zero, overrides2);
			}
			coinNumList[1] = 0;
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
				FireComponent.CreateProjectilePositionByData(null, null, 0.0, logicalGlobalPosition, new Vector2(300f, 0f), towerDefenseProjectileCreateData3, -1, base.camp, Vector2.Zero, overrides3);
			}
			coinNumList[0] = 0;
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			CreateProjectile();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["coinNumList"] = new Array(coinNumList.Select((int x) => Variant.From(in x)).ToArray()) };
	}

	public override async void ImportVariantSave(Dictionary data)
	{
		if (data.ContainsKey("coinNumList"))
		{
			Array array = data["coinNumList"].AsGodotArray();
			coinNumList = new int[3]
			{
				array[0].AsInt32(),
				array[1].AsInt32(),
				array[2].AsInt32()
			};
		}
		else
		{
			coinNumList = new int[3];
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
