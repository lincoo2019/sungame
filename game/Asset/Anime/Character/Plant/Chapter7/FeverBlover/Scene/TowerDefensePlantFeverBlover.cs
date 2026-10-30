using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantFeverBlover : TowerDefensePlant
{
	private BloverComponent _bloverComponent;

	private MagnetCoinComponent _magnetCoinComponent;

	private FireComponent _fireComponent;

	private string _projectileName = "Spike";

	private string _skinName = "Default";

	public bool run;

	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(value);
			towerDefenseProjectileCreateData.skinName = skinName;
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.projectileDataList = new Array<TowerDefenseProjectileCreateData> { towerDefenseProjectileCreateData };
			}
		}
	}

	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value;
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
			towerDefenseProjectileCreateData.skinName = value;
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.projectileDataList = new Array<TowerDefenseProjectileCreateData> { towerDefenseProjectileCreateData };
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bloverComponent = componentManager.GetRuntime<BloverComponent>();
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.OnBlowOver += BlowOver;
			}
			_magnetCoinComponent = componentManager.GetRuntime<MagnetCoinComponent>();
			MagnetCoinComponent magnetCoinComponent = _magnetCoinComponent;
			if (magnetCoinComponent != null && !magnetCoinComponent.IsReleased)
			{
				_magnetCoinComponent.OnCoinGet += CoinGet;
			}
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			AddToGroup("GoldMagnet");
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Knife";
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BloverComponent bloverComponent = _bloverComponent;
		if (bloverComponent != null && !bloverComponent.IsReleased)
		{
			_bloverComponent.OnBlowOver -= BlowOver;
		}
		MagnetCoinComponent magnetCoinComponent = _magnetCoinComponent;
		if (magnetCoinComponent != null && !magnetCoinComponent.IsReleased)
		{
			_magnetCoinComponent.OnCoinGet -= CoinGet;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Knife";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override void IdleEntered()
	{
		if (inGame && TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			Run();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
		if (!inGame || !TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			return;
		}
		if (!run)
		{
			Run();
			return;
		}
		MagnetCoinComponent magnetCoinComponent = _magnetCoinComponent;
		if (magnetCoinComponent != null && !magnetCoinComponent.IsReleased && _magnetCoinComponent.CanCoinDraw())
		{
			_magnetCoinComponent.CoinDraw();
		}
	}

	public void Run()
	{
		base.sprite.SetAnimation("Blow", loop: false, 0.2);
		base.sprite.AddAnimation("Loop", 0.0);
		instance.invincible = true;
		run = true;
		GetTree().CreateTimer(5.0, processAlways: false).Timeout += _SafetyDestroy;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.Execult();
			}
			for (int i = 1; i <= TowerDefenseManager.Instance.GetMapGridNum().Y; i++)
			{
				TowerDefenseCharacter.CreateJalapenoFire(base.camp, new Vector2I(base.gridPos.X, i), 500.0, new Array<TowerDefenseCharacterEventBase>(), new Array<TowerDefenseCharacterEventBase>());
			}
		}
	}

	public override double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		instance.invincible = false;
		return base.ExplodeHurt(num, damageKind, playSplatAudio, velocity);
	}

	public void BlowOver()
	{
		Destroy();
	}

	private void _SafetyDestroy()
	{
		if (!isDestroy)
		{
			Destroy();
		}
	}

	public void CoinGet(TowerDefenseCoinBase coin)
	{
		switch (coin.num)
		{
		case 10:
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData3 = new TowerDefenseProjectileCreateData("CoinSilver");
			towerDefenseProjectileCreateData3.baseDamage = 100.0;
			towerDefenseProjectileCreateData3.fireMethodFlags = 32;
			BulletFieldSpawnOverrides overrides3 = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData3, -1, base.camp, Vector2.Zero, overrides3);
			break;
		}
		case 50:
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData2 = new TowerDefenseProjectileCreateData("CoinGold");
			towerDefenseProjectileCreateData2.baseDamage = 500.0;
			towerDefenseProjectileCreateData2.fireMethodFlags = 32;
			BulletFieldSpawnOverrides overrides2 = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData2, -1, base.camp, Vector2.Zero, overrides2);
			break;
		}
		case 1000:
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinDiamond");
			towerDefenseProjectileCreateData.baseDamage = 1000.0;
			towerDefenseProjectileCreateData.fireMethodFlags = 32;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, Vector2.Zero, overrides);
			break;
		}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["projectileName"] = projectileName,
			["skinName"] = skinName,
			["run"] = run
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Spike");
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
		run = data.ContainsKey("run") && data["run"].AsBool();
	}
}
