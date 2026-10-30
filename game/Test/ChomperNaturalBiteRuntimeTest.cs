using System;
using System.Threading.Tasks;
using Godot;

public partial class ChomperNaturalBiteRuntimeTest : Node
{
	private static readonly string[] PlantPaths = new string[6] { "Chapter0/Chomper/Scene/TowerDefensePlantChomper.tscn", "Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn", "Chapter5/GarlicChomper/Scene/TowerDefensePlantGarlicChomper.tscn", "Chapter6/ChomperPot/Scene/TowerDefensePlantChomperPot.tscn", "Chapter8/ChomperZ/Scene/TowerDefensePlantChomperZ.tscn", "Gold/SpikeChomper/Scene/TowerDefensePlantSpikeChomper.tscn" };

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNum = manager.gridNum;
		ChomperNaturalBiteControl control = new ChomperNaturalBiteControl
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		try
		{
			_ = 1;
			try
			{
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D();
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				string[] plantPaths = PlantPaths;
				foreach (string path in plantPaths)
				{
					await RunCase(control, path, moveTarget: false);
				}
				await RunCase(control, PlantPaths[0], moveTarget: true);
			}
			catch (Exception ex)
			{
				_failures++;
				GD.PushError(ex.ToString());
			}
		}
		finally
		{
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousBegin;
			manager.gridSize = previousSize;
			manager.gridNum = previousNum;
			control.QueueFree();
			await WaitFrames(3);
		}
		GD.Print($"CHOMPER_NATURAL_BITE_RESULT passed={_failures == 0} failures={_failures}");
		GetTree().Quit((_failures != 0) ? 2 : 0);
	}

	private async Task RunCase(ChomperNaturalBiteControl control, string path, bool moveTarget)
	{
		TowerDefensePlant plant = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/" + path).Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
		TowerDefenseZombie zombie = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn").Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
		try
		{
			plant.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
			plant.gridPos = new Vector2I(4, 2);
			plant.Position = new Vector2(350f, 114f);
			zombie.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			zombie.Position = new Vector2(path.Contains("ChomperPot") ? 350 : 400, 114f);
			zombie.gridPos = TowerDefenseManager.Instance.GetMapGridPos(zombie.Position);
			zombie.timeScale = 0.0;
			control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			TowerDefenseManager.Instance.CharacterRegister(plant);
			TowerDefenseManager.Instance.CharacterRegister(zombie);
			ChomperComponent chomper = plant.componentManager.GetRuntime<ChomperComponent>();
			double initialHitpoints = zombie.GetCurrentHitPoint();
			int biteEvents = 0;
			double biteCooldown = -1.0;
			chomper.sprite.OnAnimeEvent += delegate(string command, Variant argument)
			{
				if (command == chomper.biteEventName)
				{
					biteEvents++;
					biteCooldown = chomper.attackComponent.timer;
				}
			};
			bool moved = false;
			bool checkedCooldown = chomper.suckUse;
			for (int frame = 0; frame < 240; frame++)
			{
				if (chomper.isChew)
				{
					break;
				}
				if (!checkedCooldown && chomper.StateMachine.CurrentStateHandle?.StableId == "chomper.attack" && chomper.attackComponent.timer > 0.0)
				{
					double timer = chomper.attackComponent.timer;
					checkedCooldown = chomper.attackComponent.IsCurrentTargetReachable(zombie) && chomper.attackComponent.timer == timer;
				}
				if (moveTarget && !moved && chomper.StateMachine.CurrentStateHandle?.StableId == "chomper.attack")
				{
					zombie.gridPos = new Vector2I(9, 2);
					zombie.SetLogicalGlobalPosition(new Vector2(850f, 114f));
					moved = true;
				}
				await WaitFrames(1);
				if ((moveTarget && biteEvents > 0) || (chomper.biteOnly && zombie.GetCurrentHitPoint() < initialHitpoints))
				{
					break;
				}
			}
			bool flag = !GodotObject.IsInstanceValid(zombie) || zombie.isChomp || zombie.isDestroy;
			bool flag2 = ((!chomper.biteOnly) ? (flag && chomper.isChew) : (!flag && !chomper.isChew && initialHitpoints - zombie.GetCurrentHitPoint() == (double)chomper.biteAttack));
			bool flag3 = checkedCooldown && biteEvents > 0 && ((!moveTarget) ? flag2 : (moved && !flag && !chomper.isChew));
			GD.Print($"CHOMPER_CASE path={path} moved={moveTarget} passed={flag3} events={biteEvents} cooldown={biteCooldown:F3} consumed={flag} chew={chomper.isChew} state={chomper.StateMachine.CurrentStateHandle?.StableId}");
			if (!flag3)
			{
				_failures++;
			}
		}
		finally
		{
			TowerDefenseCharacter[] array = new TowerDefenseCharacter[2] { plant, zombie };
			foreach (TowerDefenseCharacter towerDefenseCharacter in array)
			{
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					TowerDefenseManager.Instance.CharacterUnregister(towerDefenseCharacter);
					towerDefenseCharacter.QueueFree();
				}
			}
			await WaitFrames(3);
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
