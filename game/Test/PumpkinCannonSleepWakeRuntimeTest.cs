using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class PumpkinCannonSleepWakeRuntimeTest : Node
{
	private CubeBoxBloverControl _control;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 4;
		try
		{
			typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
			_control = new CubeBoxBloverControl
			{
				isInit = true,
				isGameRunning = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(_control, forceReadableName: false, InternalMode.Disabled);
			_control.characterNode = new Node2D();
			_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			instance.currentControl = _control;
			TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
			{
				isNight = false
			};
			instance.gridNum = towerDefenseMapConfig.gridNum;
			instance.gridSize = towerDefenseMapConfig.gridSize;
			instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
			{
				config = towerDefenseMapConfig,
				mapConfig = towerDefenseMapConfig,
				control = _control
			};
			towerDefenseBattleFeatureMap.groundRect = new Rect2(towerDefenseMapConfig.gridBeginPos, towerDefenseMapConfig.gridSize * new Vector2(towerDefenseMapConfig.gridNum.X, towerDefenseMapConfig.gridNum.Y));
			towerDefenseBattleFeatureMap.mapControl = new CubeBoxBloverMap
			{
				mapFeature = towerDefenseBattleFeatureMap
			};
			_control.AddChild(towerDefenseBattleFeatureMap.mapControl, forceReadableName: false, InternalMode.Disabled);
			_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
			towerDefenseBattleFeatureMap.PlantGridInit();
			for (int i = 1; i <= 9; i++)
			{
				for (int j = 1; j <= 5; j++)
				{
					towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
				}
			}
			await Probe("cannon.idle", new Vector2I(3, 2));
			await Probe("cannon.rest", new Vector2I(4, 3));
			await Probe("cannon.charge", new Vector2I(5, 4));
			await Probe("cannon.fire", new Vector2I(6, 2));
			_control.QueueFree();
			await WaitFrames(3);
			bool flag = _failures == 0 && _checks == 32;
			GD.Print($"PUMPKIN_CANNON_SLEEP_WAKE_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PushError(ex.ToString());
			GetTree().Quit(2);
		}
	}

	private async Task Probe(string stage, Vector2I grid)
	{
		ResourceManager.Instance.TOWERDEFENSE_CHARCATERS["PlantPumpkinCannon"] = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter6/PumpkinCannon/Scene/TowerDefensePlantPumpkinCannon.tscn");
		TowerDefensePlantPumpkinCannon plant = GD.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter6/PumpkinCannon/Packet/PlantPumpkinCannon.tres").Plant(grid, playAudio: false) as TowerDefensePlantPumpkinCannon;
		await WaitFrames(3);
		CannonComponent cannon = plant.cannonComponent;
		plant.restTime = 0.5;
		string waitingStage = ((stage == "cannon.fire") ? "cannon.idle" : stage);
		for (int frame = 0; frame < 400; frame++)
		{
			if (!(cannon.StateMachine.CurrentStateHandle?.StableId != waitingStage))
			{
				break;
			}
			await WaitFrames(1);
		}
		if (stage == "cannon.fire")
		{
			cannon.FireAt(plant.GetLogicalGlobalPosition());
		}
		Check(cannon.StateMachine.CurrentStateHandle?.StableId == stage, "到达催眠前阶段：" + stage);
		TowerDefenseZombieHypnotist towerDefenseZombieHypnotist = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn").Instantiate<TowerDefenseZombieHypnotist>(PackedScene.GenEditState.Disabled);
		foreach (TowerDefenseCharacterEventBase item in towerDefenseZombieHypnotist.rangeEvent)
		{
			if (item is TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff)
			{
				towerDefenseCharacterEventAddBuff.Execute(Vector2.Zero, plant);
				break;
			}
		}
		towerDefenseZombieHypnotist.Free();
		await WaitFrames(10);
		Check(plant.IsSleep() && !plant.componentAlive, "催眠师让南瓜炮进入睡眠。");
		Check(!cannon.CanFire() && !cannon.mousePressComponent.Alive, "睡眠期间不能开炮。");
		plant.WakeUp();
		await WaitFrames(300);
		Check(!plant.IsSleep() && plant.componentAlive, "唤醒恢复角色技能。");
		GD.Print($"PUMPKIN_TRACE stage={stage} state={cannon.StateMachine.CurrentStateHandle?.StableId} canFire={cannon.canFire} mouseAlive={cannon.mousePressComponent.Alive}");
		Check(cannon.CanFire() && cannon.mousePressComponent.Alive, "唤醒后装填完成并恢复点击：" + stage);
		Check(cannon.StateMachine.CurrentStateHandle?.StableId == "cannon.idle", "唤醒后必须完成装填：" + stage);
		int shots = 0;
		cannon.OnFire += delegate
		{
			shots++;
		};
		MousePressComponent mouse = cannon.mousePressComponent;
		mouse.TryGetClickShapeScreenCenter(out var screenCenter);
		using InputEventMouseButton down = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Pressed = true,
			Position = screenCenter
		};
		mouse.ProcessInput(down);
		Check(mouse.IsPressed, "第一次点击进入瞄准：" + stage);
		down.Pressed = false;
		mouse.ProcessInput(down);
		await WaitFrames(2);
		down.Pressed = true;
		down.Position = plant.GetViewport().GetCanvasTransform() * TowerDefenseManager.Instance.GetGroundRect().GetCenter();
		mouse.ProcessInput(down);
		GD.Print($"PUMPKIN_CLICK aiming={mouse.IsPressed} shots={shots} target={down.Position} ground={TowerDefenseManager.Instance.GetGroundRect()} data={cannon.projectileData != null}");
		Check(shots == 1, "第二次点击发射一次：" + stage);
		plant.QueueFree();
		await WaitFrames(3);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.Print("PUMPKIN_FAILURE " + message);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}
}
