using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewDaily488ChomperBlowTutorialRuntimeTest : Node
{
	private const string ChomperScenePath = "res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn";

	private const string ShieldZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Scene/TowerDefenseZombieShieldArmor.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugOverviewDaily488ChomperBlowControlStub control = null;
		TowerDefensePlantChomperBlow chomper = null;
		TowerDefenseZombie zombie = null;
		TowerDefenseBattleFeatureTutorial tutorialFeature = null;
		TowerDefenseBattleFeaturePreSpawn preSpawnFeature = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(TutorialManager.Instance) && GodotObject.IsInstanceValid(BroadCastManager.Instance), "The real tutorial and broadcast managers must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(TutorialManager.Instance) || !GodotObject.IsInstanceValid(BroadCastManager.Instance))
				{
					goto end_IL_008d;
				}
				control = new BugOverviewDaily488ChomperBlowControlStub
				{
					Name = "Daily488Control",
					isGameRunning = true,
					isInit = false
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				chomper = LoadCharacter<TowerDefensePlantChomperBlow>("res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
				zombie = LoadCharacter<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Scene/TowerDefenseZombieShieldArmor.tscn");
				Check(GodotObject.IsInstanceValid(chomper), "Daily 488 must use the real PlantChomperBlow scene.");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieShieldArmor", "The fixture must use the real ZombieShieldArmor card target.");
				if (!GodotObject.IsInstanceValid(chomper) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_008d;
				}
				chomper.editorPreviewMode = true;
				zombie.editorPreviewMode = true;
				chomper.inGame = false;
				zombie.inGame = false;
				chomper.gridPos = new Vector2I(2, 4);
				zombie.gridPos = new Vector2I(6, 4);
				chomper.GlobalPosition = new Vector2(200f, 400f);
				zombie.GlobalPosition = new Vector2(700f, 400f);
				control.characterNode.AddChild(chomper, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				ChomperComponent component = chomper.componentManager?.GetRuntime<ChomperComponent>();
				Check(component != null && !component.IsReleased && component.suckUse, "The real ChomperBlow suction component must be active.");
				Check(component?.biteLoopAnimeClips == "BiteLoop" && (chomper.sprite?.HasClip("BiteLoop") ?? false), "The authored BiteLoop suction clip must be available.");
				if (component?.IsReleased ?? true)
				{
					goto end_IL_008d;
				}
				BeginSuction(chomper, component, zombie);
				double firstPullX = zombie.GlobalPosition.X;
				Check(component.StateMachine?.CurrentStateHandle?.StableId == "chomper.attack" && chomper.componentRunning, $"The real Chomper component must be attacking before the tutorial ends; state={component.StateMachine?.CurrentStateHandle?.StableId}, componentRunning={chomper.componentRunning}.");
				Check(!zombie.instance.canCollection && firstPullX < 700.0, $"Suction must lock and pull the real shield zombie; canCollection={zombie.instance.canCollection}, x={firstPullX}.");
				tutorialFeature = new TowerDefenseBattleFeatureTutorial
				{
					control = control
				};
				tutorialFeature.Init(CreateDaily488TutorialData());
				BugOverviewDaily488ChomperBlowTutorialRuntimeTest bugOverviewDaily488ChomperBlowTutorialRuntimeTest = this;
				TutorialConfig config = tutorialFeature.config;
				bugOverviewDaily488ChomperBlowTutorialRuntimeTest.Check(config != null && config.GetStepNum() == 1 && tutorialFeature.config.GetTutorialStep(0).conditionList.Count == 0 && Math.Abs(tutorialFeature.config.GetTutorialStep(0).broadCastConfig.broadCastTime - 5.0) < 0.001, "The runtime tutorial must retain Daily 488's unconditional five-second broadcast shape.");
				Task tutorialTask = tutorialFeature.GameStart();
				await WaitUntilComplete(tutorialTask, 8.0);
				Check(tutorialTask.IsCompletedSuccessfully && TutorialManager.Instance.currentTutoroal == null, "The real tutorial must finish through the broadcast timeout.");
				preSpawnFeature = new TowerDefenseBattleFeaturePreSpawn
				{
					control = control
				};
				preSpawnFeature.Init(new Dictionary { ["Packet"] = new Godot.Collections.Array() });
				preSpawnFeature.preSpawnList.Add(chomper);
				await preSpawnFeature.GameStart();
				await WaitFrames(3);
				Check(chomper.componentRunning && component.StateMachine?.CurrentStateHandle?.StableId == "chomper.attack", $"PreSpawn activation after the prompt must not exit an already-running suction; componentRunning={chomper.componentRunning}, state={component.StateMachine?.CurrentStateHandle?.StableId}.");
				Check(chomper.sprite.clip == component.biteLoopAnimeClips, "PreSpawn activation must not overwrite BiteLoop with " + chomper.sprite.clip + ".");
				component.AttackProcessing(0.1);
				Check((double)zombie.GlobalPosition.X < firstPullX && !zombie.instance.canCollection, $"Suction must keep advancing after the prompt; before={firstPullX}, after={zombie.GlobalPosition.X}, canCollection={zombie.instance.canCollection}.");
				component.SendStateEvent(component.idleStateEvent);
				await WaitFrames(1);
				Check(zombie.instance.canCollection && component.target == null && !component.isSuck, "Leaving suction must release the shield zombie interaction lock.");
				zombie.GlobalPosition = new Vector2(650f, 400f);
				BeginSuction(chomper, component, zombie);
				Check(!zombie.instance.canCollection && component.StateMachine?.CurrentStateHandle?.StableId == "chomper.attack", "ChomperBlow must be able to lock a target again after the tutorial lifecycle.");
				goto end_IL_006e;
				end_IL_008d:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewDaily488ChomperBlowTutorialRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006e;
			}
			return;
			end_IL_006e:;
		}
		finally
		{
			tutorialFeature?.Destroy();
			preSpawnFeature?.Destroy();
			if (GodotObject.IsInstanceValid(TutorialManager.Instance))
			{
				TutorialManager.Instance.TutorialClear();
			}
			if (GodotObject.IsInstanceValid(chomper))
			{
				chomper.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"DAILY488_CHOMPER_TUTORIAL_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void BeginSuction(TowerDefensePlantChomperBlow chomper, ChomperComponent component, TowerDefenseZombie zombie)
	{
		chomper.Component();
		component.target = zombie;
		component.isSuck = true;
		component.SendStateEvent(component.attackStateEvent);
		chomper.sprite.SetAnimation(component.biteLoopAnimeClips);
		component.AttackProcessing(0.1);
	}

	private static Dictionary CreateDaily488TutorialData()
	{
		return new Dictionary
		{
			["isCustom"] = true,
			["SaveKey"] = "",
			["Step"] = new Godot.Collections.Array
			{
				new Dictionary
				{
					["BroadCast"] = new Dictionary
					{
						["Text"] = "Daily 488 tutorial probe",
						["Time"] = 5.0
					},
					["Condition"] = new Godot.Collections.Array()
				}
			}
		};
	}

	private async Task WaitUntilComplete(Task task, double timeoutSeconds)
	{
		double elapsed = 0.0;
		while (!task.IsCompleted && elapsed < timeoutSeconds)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			elapsed += GetProcessDeltaTime();
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewDaily488ChomperBlowTutorialRuntimeTest] " + message);
		}
	}
}
