using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantRobot : TowerDefensePlant
{
	private const string PLANT_ROBOTRACK1 = "uid://2ebfsacjxwse";

	private const string PLANT_ROBOTRACK2 = "uid://drjphbxctwqg5";

	private const string PLANT_ROBOTRACK3 = "uid://800yffpdvahb";

	private const string PLANT_ROBOT_BODY_A1 = "uid://ce35mdynvmvys";

	private const string PLANT_ROBOT_BODY_A2 = "uid://dct082g5i6hg2";

	private const string PLANT_ROBOT_BODY_A3 = "uid://dxgmilyjml4ar";

	private const string PLANT_ROBOT_BODYR1 = "uid://chpeoi5efjvh3";

	private const string PLANT_ROBOT_BODYR2 = "uid://b6r70pq04d4ix";

	private const string PLANT_ROBOT_BODYR3 = "uid://btbl5f7ps30pe";

	private const string PLANT_ROBOT_LEFT_A1 = "uid://dixhlofgnr2sn";

	private const string PLANT_ROBOT_LEFT_A2 = "uid://dtj3daoe3i4ti";

	private const string PLANT_ROBOT_LEFT_A3 = "uid://doovkx7gcrx86";

	private const string PLANT_ROBOT_LEFTR1 = "uid://dqh161x05tspr";

	private const string PLANT_ROBOT_LEFTR2 = "uid://bb5kstf508tte";

	private const string PLANT_ROBOT_LEFTR3 = "uid://dmln03hqaw5gp";

	private const string PLANT_ROBOT_LEFT_C1 = "uid://btwt3uvvuwqvi";

	private const string PLANT_ROBOT_LEFT_C2 = "uid://bl70mcjce8gs5";

	private const string PLANT_ROBOT_LEFT_C3 = "uid://ipidh6t2njt5";

	private const string PLANT_ROBOT_LEFT_D1 = "uid://xxnso6kdbkmo";

	private const string PLANT_ROBOT_LEFT_D2 = "uid://dkedocvimlovk";

	private const string PLANT_ROBOT_LEFT_D3 = "uid://cvlfngqme066r";

	private const string PLANT_ROBOT_RIGHT_A1 = "uid://dl3031sobp60x";

	private const string PLANT_ROBOT_RIGHT_A2 = "uid://dhu31tsxoow2k";

	private const string PLANT_ROBOT_RIGHT_A3 = "uid://bik8j6bxg7c6f";

	private const string PLANT_ROBOT_RIGHTR1 = "uid://de2tghwbp36s1";

	private const string PLANT_ROBOT_RIGHTR2 = "uid://crlmumytyhe5g";

	private const string PLANT_ROBOT_RIGHTR3 = "uid://cfmmts1sfdfh7";

	private const string PLANT_ROBOT_RIGHT_C1 = "uid://bjdp1rn525cpp";

	private const string PLANT_ROBOT_RIGHT_C2 = "uid://cy0mkglnhyjb1";

	private const string PLANT_ROBOT_RIGHT_C3 = "uid://dcyvp5l2b6xpf";

	private const string PLANT_ROBOT_RIGHT_D1 = "uid://cxdjxqi46a72q";

	private const string PLANT_ROBOT_RIGHT_D2 = "uid://b23wdixmq6p32";

	private const string PLANT_ROBOT_RIGHT_D3 = "uid://yimmc7j73xhe";

	private FireComponent _fireComponent;

	private FireComponent _fireComponent2;

	private CharacterTimerComponent _timerComponent;

	private MousePressComponent _mousePressComponent;

	private StateHandle _downState;

	private StateHandle _upState;

	private bool _roleStateSignalsConnected;

	private string[] _modeList = new string[3] { "A", "B", "C" };

	public int modeId;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent2 = componentManager.GetRuntime<FireComponent>("character.fire.1");
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
			_timerComponent.OnTimeout += Timeout;
			_mousePressComponent.OnDoublePressed += DoublePressed;
			_fireComponent2.BindCheckAreaShapeToGridRow(0, 1);
			_fireComponent2.BindCheckAreaShapeToGridRow(2, -1);
			_downState = base.StateMachine?.GetStateById("plant.robot.down");
			_upState = base.StateMachine?.GetStateById("plant.robot.up");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		if (_mousePressComponent != null)
		{
			_mousePressComponent.OnDoublePressed -= DoublePressed;
		}
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		StateHandle downState = _downState;
		if (downState != null && downState.IsValid)
		{
			StateHandle upState = _upState;
			if (upState != null && upState.IsValid)
			{
				_downState.Entered += DownEntered;
				_downState.Exited += DownExited;
				_downState.PhysicsProcessing += DownProcessing;
				_upState.Entered += UpEntered;
				_upState.Exited += UpExited;
				_upState.PhysicsProcessing += UpProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_downState.Entered -= DownEntered;
			_downState.Exited -= DownExited;
			_downState.PhysicsProcessing -= DownProcessing;
			_upState.Entered -= UpEntered;
			_upState.Exited -= UpExited;
			_upState.PhysicsProcessing -= UpProcessing;
			_downState = null;
			_upState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		switch (modeId)
		{
		case 0:
			_fireComponent.alive = true;
			base.sprite.SetAnimation("IdleA", loop: true, 0.1);
			break;
		case 1:
			_fireComponent2.alive = true;
			base.sprite.SetAnimation("IdleB", loop: true, 0.1);
			break;
		case 2:
			instance.explosionHurt = base.config.explosionHurt / 2.0;
			instance.smashHurt = base.config.smashHurt / 2.0;
			instance.dragHurt = base.config.dragHurt / 2.0;
			instance.spikeHurt = base.config.spikeHurt / 2.0;
			instance.biteHurt = base.config.biteHurt / 2.0;
			_timerComponent.Run("Heal", 1.0);
			base.sprite.SetAnimation("IdleC", loop: true, 0.1);
			break;
		}
	}

	public virtual void DownEntered()
	{
		_timerComponent.Stop("Heal");
		_fireComponent.alive = false;
		_fireComponent2.alive = false;
		instance.explosionHurt = base.config.explosionHurt;
		instance.smashHurt = base.config.smashHurt;
		instance.dragHurt = base.config.dragHurt;
		instance.spikeHurt = base.config.spikeHurt;
		instance.biteHurt = base.config.biteHurt;
		switch (modeId)
		{
		case 0:
			base.sprite.SetAnimation("DownA", loop: false, 0.1);
			break;
		case 1:
			base.sprite.SetAnimation("DownB", loop: false, 0.1);
			break;
		case 2:
			base.sprite.SetAnimation("DownC", loop: false, 0.1);
			break;
		}
	}

	public virtual void DownProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void DownExited()
	{
	}

	public virtual void UpEntered()
	{
		switch (modeId)
		{
		case 0:
			base.sprite.SetAnimation("UpA", loop: false, 0.1);
			break;
		case 1:
			base.sprite.SetAnimation("UpB", loop: false, 0.1);
			break;
		case 2:
			base.sprite.SetAnimation("UpC", loop: false, 0.1);
			break;
		}
	}

	public virtual void UpProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void UpExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		switch (clip)
		{
		case "UpA":
		case "UpB":
		case "UpC":
			Idle();
			break;
		case "DownA":
		case "DownB":
		case "DownC":
			modeId = (modeId + 1 + _modeList.Length) % _modeList.Length;
			SendStateEvent("ToUp");
			break;
		}
	}

	public virtual void DoublePressed(Vector2 pos)
	{
		SendStateEvent("ToDown");
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Heal")
		{
			_timerComponent.Run("Heal", 1.0);
			Health(50.0);
			if (instance.hitpoints >= instance.hitpointsSave)
			{
				instance.hitpoints = instance.hitpointsSave;
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		switch (damagePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("PlantRobot_Back1.png", "uid://2ebfsacjxwse");
			base.sprite.SetAtlasReplace("PlantRobot_BodyA_1.png", "uid://ce35mdynvmvys");
			base.sprite.SetAtlasReplace("PlantRobot_BodyB_1.png", "uid://chpeoi5efjvh3");
			base.sprite.SetAtlasReplace("PlantRobot_LeftA_1.png", "uid://dixhlofgnr2sn");
			base.sprite.SetAtlasReplace("PlantRobot_LeftB_1.png", "uid://dqh161x05tspr");
			base.sprite.SetAtlasReplace("PlantRobot_LeftC_1.png", "uid://btwt3uvvuwqvi");
			base.sprite.SetAtlasReplace("PlantRobot_LeftD_1.png", "uid://xxnso6kdbkmo");
			base.sprite.SetAtlasReplace("PlantRobot_RightA_1.png", "uid://dl3031sobp60x");
			base.sprite.SetAtlasReplace("PlantRobot_RightB_1.png", "uid://de2tghwbp36s1");
			base.sprite.SetAtlasReplace("PlantRobot_RightC_1.png", "uid://bjdp1rn525cpp");
			base.sprite.SetAtlasReplace("PlantRobot_RightD_1.png", "uid://cxdjxqi46a72q");
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("PlantRobot_Back1.png", "uid://drjphbxctwqg5");
			base.sprite.SetAtlasReplace("PlantRobot_BodyA_1.png", "uid://dct082g5i6hg2");
			base.sprite.SetAtlasReplace("PlantRobot_BodyB_1.png", "uid://b6r70pq04d4ix");
			base.sprite.SetAtlasReplace("PlantRobot_LeftA_1.png", "uid://dtj3daoe3i4ti");
			base.sprite.SetAtlasReplace("PlantRobot_LeftB_1.png", "uid://bb5kstf508tte");
			base.sprite.SetAtlasReplace("PlantRobot_LeftC_1.png", "uid://bl70mcjce8gs5");
			base.sprite.SetAtlasReplace("PlantRobot_LeftD_1.png", "uid://dkedocvimlovk");
			base.sprite.SetAtlasReplace("PlantRobot_RightA_1.png", "uid://dhu31tsxoow2k");
			base.sprite.SetAtlasReplace("PlantRobot_RightB_1.png", "uid://crlmumytyhe5g");
			base.sprite.SetAtlasReplace("PlantRobot_RightC_1.png", "uid://cy0mkglnhyjb1");
			base.sprite.SetAtlasReplace("PlantRobot_RightD_1.png", "uid://b23wdixmq6p32");
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("PlantRobot_Back1.png", "uid://800yffpdvahb");
			base.sprite.SetAtlasReplace("PlantRobot_BodyA_1.png", "uid://dxgmilyjml4ar");
			base.sprite.SetAtlasReplace("PlantRobot_BodyB_1.png", "uid://btbl5f7ps30pe");
			base.sprite.SetAtlasReplace("PlantRobot_LeftA_1.png", "uid://doovkx7gcrx86");
			base.sprite.SetAtlasReplace("PlantRobot_LeftB_1.png", "uid://dmln03hqaw5gp");
			base.sprite.SetAtlasReplace("PlantRobot_LeftC_1.png", "uid://ipidh6t2njt5");
			base.sprite.SetAtlasReplace("PlantRobot_LeftD_1.png", "uid://cvlfngqme066r");
			base.sprite.SetAtlasReplace("PlantRobot_RightA_1.png", "uid://bik8j6bxg7c6f");
			base.sprite.SetAtlasReplace("PlantRobot_RightB_1.png", "uid://cfmmts1sfdfh7");
			base.sprite.SetAtlasReplace("PlantRobot_RightC_1.png", "uid://dcyvp5l2b6xpf");
			base.sprite.SetAtlasReplace("PlantRobot_RightD_1.png", "uid://yimmc7j73xhe");
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "modeId", modeId } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		modeId = data.GetValueOrDefault("modeId", Variant.From<int>(0)).AsInt32();
		_fireComponent.alive = modeId == 0;
		_fireComponent2.alive = modeId == 1;
		if (modeId == 2)
		{
			instance.explosionHurt = base.config.explosionHurt / 2.0;
			instance.smashHurt = base.config.smashHurt / 2.0;
			instance.dragHurt = base.config.dragHurt / 2.0;
			instance.spikeHurt = base.config.spikeHurt / 2.0;
			instance.biteHurt = base.config.biteHurt / 2.0;
		}
	}
}
