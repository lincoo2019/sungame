using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunFlowerZ : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private CharacterTimerComponent _timerComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private string _produceType = "Sun";

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

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = value;
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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			if (!base.sprite.pause && Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				_timerComponent.Run("Spawn", 25.0);
				return;
			}
			SpawnZombie();
			_timerComponent.Run("Spawn", 25.0);
		}
	}

	public override void SpawnZombie()
	{
		if (characterDisabled || base.sprite.pause || (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter("ZombieNormalSunflower", logicalGlobalPosition, base.gridPos, base.groundHeight);
		towerDefenseCharacter.Rise(2.5);
		if (!instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieNormalSunflower", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight);
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 25.0);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 25);
	}
}
