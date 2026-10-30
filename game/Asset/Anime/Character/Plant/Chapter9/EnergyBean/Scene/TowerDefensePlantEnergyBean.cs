using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantEnergyBean : TowerDefensePlant
{
	private const int MaxChargeLevel = 5;

	private const double ChargeInterval = 30.0;

	private const double ReleaseDuration = 15.0;

	private const double WakeInterval = 1.0;

	private const string LevelTexturePath = "res://Asset/AtlasSource/Anime/Character/Plant/Chapter9/EnergyBean/EnergyBean_level";

	private const string LevelMediaName = "EnergyBean_level0.png";

	private MousePressComponent _mousePress;

	private ProduceComponent _produce;

	private StateHandle _chargingState;

	private StateHandle _releasingState;

	private bool _stateSignalsConnected;

	private int _chargeLevel;

	private double _chargeTimer;

	private double _releaseTimer;

	private double _wakeTimer;

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,QXSun")]
	public string produceType = "BrainSun";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_mousePress = componentManager.GetRuntime<MousePressComponent>();
			if (_mousePress != null)
			{
				_mousePress.OnPressed += OnPressed;
			}
			_produce = componentManager.GetRuntime<ProduceComponent>();
			_chargingState = base.StateMachine?.GetStateById("plant.energy_bean.charging");
			_releasingState = base.StateMachine?.GetStateById("plant.energy_bean.releasing");
			ConnectStateSignals();
			SetLevelVisual(_chargeLevel);
			Callable.From(SendToChargingDeferred).CallDeferred();
		}
	}

	private void SendToChargingDeferred()
	{
		SendStateEvent("ToCharging");
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		if (_mousePress != null)
		{
			_mousePress.OnPressed -= OnPressed;
			_mousePress = null;
		}
		_produce = null;
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!Engine.IsEditorHint() && !die)
		{
			_wakeTimer += delta;
			if (_wakeTimer >= 1.0)
			{
				_wakeTimer = 0.0;
				WakeUpCellPlants();
			}
		}
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			StateHandle chargingState = _chargingState;
			if (chargingState != null && chargingState.IsValid)
			{
				_chargingState.Entered += ChargingEntered;
				_chargingState.PhysicsProcessing += ChargingProcessing;
			}
			StateHandle releasingState = _releasingState;
			if (releasingState != null && releasingState.IsValid)
			{
				_releasingState.Entered += ReleasingEntered;
				_releasingState.PhysicsProcessing += ReleasingProcessing;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			StateHandle chargingState = _chargingState;
			if (chargingState != null && chargingState.IsValid)
			{
				_chargingState.Entered -= ChargingEntered;
				_chargingState.PhysicsProcessing -= ChargingProcessing;
			}
			StateHandle releasingState = _releasingState;
			if (releasingState != null && releasingState.IsValid)
			{
				_releasingState.Entered -= ReleasingEntered;
				_releasingState.PhysicsProcessing -= ReleasingProcessing;
			}
			_stateSignalsConnected = false;
		}
	}

	private void ChargingEntered()
	{
		_chargeTimer = 0.0;
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.SetAnimation("Idle");
		}
	}

	private void ChargingProcessing(double delta)
	{
		_chargeTimer += delta;
		if (_chargeTimer >= 30.0 && _chargeLevel < 5)
		{
			_chargeTimer = 0.0;
			_chargeLevel++;
			SetLevelVisual(_chargeLevel);
		}
	}

	private void ReleasingEntered()
	{
		_releaseTimer = 0.0;
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.SetAnimation("Shooting", loop: false, 0.1);
			base.sprite.AddAnimation("IdleB", 0.0);
		}
	}

	private void ReleasingProcessing(double delta)
	{
		_releaseTimer += delta;
		if (_releaseTimer >= 15.0)
		{
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation("Idle");
			}
			SetLevelVisual(0);
			_chargeLevel = 0;
			SendStateEvent("ToCharging");
		}
	}

	private void WakeUpCellPlants()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character != null && character.camp == base.camp)
			{
				character.WakeUp();
			}
		}
	}

	private void OnPressed(Vector2 pos)
	{
		StateHandle chargingState = _chargingState;
		if (chargingState != null && chargingState.IsActive && _chargeLevel > 0)
		{
			int chargeLevel = _chargeLevel;
			ProduceBrainsun(25 * chargeLevel);
			ApplySpeedUp(chargeLevel);
			SendStateEvent("ToReleasing");
		}
	}

	private void ProduceBrainsun(int amount)
	{
		if (_produce == null)
		{
			_produce = componentManager.GetRuntime<ProduceComponent>();
		}
		if (_produce != null)
		{
			bool flag = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
			_produce.produceType = (flag ? "Sun" : "BrainSun");
			_produce.Create(base.GlobalPosition, amount);
		}
	}

	private void ApplySpeedUp(int level)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefenseCharacterBuffCoffee towerDefenseCharacterBuffCoffee = new TowerDefenseCharacterBuffCoffee
		{
			timeScaleValue = level,
			time = 15.0
		};
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character != null && GodotObject.IsInstanceValid(character) && character.camp == base.camp && character != this)
			{
				character.buff.AddBuff(towerDefenseCharacterBuffCoffee.Duplicate(deep: true) as TowerDefenseCharacterBuffConfig);
			}
		}
	}

	private void SetLevelVisual(int level)
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			int num = Mathf.Clamp(level, 0, 5);
			base.sprite.SetAtlasReplace("EnergyBean_level0.png", "res://Asset/AtlasSource/Anime/Character/Plant/Chapter9/EnergyBean/EnergyBean_level" + num + ".png");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["EnergyBeanChargeLevel"] = _chargeLevel;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		if (data != null && data.ContainsKey("EnergyBeanChargeLevel"))
		{
			_chargeLevel = Mathf.Clamp(data["EnergyBeanChargeLevel"].AsInt32(), 0, 5);
		}
		SetLevelVisual(_chargeLevel);
	}
}
