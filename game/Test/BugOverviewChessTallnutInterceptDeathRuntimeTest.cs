using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewChessTallnutInterceptDeathRuntimeTest : Node
{
	private const string ChessScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Chess/Scene/TowerDefenseZombieChess.tscn";

	private const string TallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewChessTallnutInterceptDeathControlStub control = null;
		TowerDefenseZombieChess chess = null;
		TowerDefensePlantTallnut tallnut = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					return;
				}
				control = new BugOverviewChessTallnutInterceptDeathControlStub
				{
					Name = "ChessTallnutInterceptDeathControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Chess/Scene/TowerDefenseZombieChess.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real Chess zombie scene must load.");
				Check(GodotObject.IsInstanceValid(packedScene2), "The real Tallnut scene must load.");
				chess = packedScene?.Instantiate<TowerDefenseZombieChess>(PackedScene.GenEditState.Disabled);
				tallnut = packedScene2?.Instantiate<TowerDefensePlantTallnut>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(chess), "The real Chess zombie must instantiate.");
				Check(GodotObject.IsInstanceValid(tallnut), "The real Tallnut must instantiate.");
				if (!GodotObject.IsInstanceValid(chess) || !GodotObject.IsInstanceValid(tallnut))
				{
					return;
				}
				Vector2 overlapPosition = new Vector2(400f, 252f);
				Vector2I overlapGrid = new Vector2I(4, 2);
				chess.inGame = true;
				chess.gridPos = overlapGrid;
				chess.SetLogicalGlobalPosition(overlapPosition);
				chess.skipDestroySet = true;
				chess.currentArmor = new Array<string> { "Pogo", "Chess" };
				tallnut.inGame = true;
				tallnut.gridPos = overlapGrid;
				tallnut.SetLogicalGlobalPosition(overlapPosition);
				control.characterNode.AddChild(tallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(chess, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				AttackComponent attackComponent = chess.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				Check(GodotObject.IsInstanceValid(chess.sprite) && chess.sprite.HasClip("Pogo") && chess.sprite.HasClip("Death"), "The real Chess zombie must expose its authored Pogo and Death clips.");
				BugOverviewChessTallnutInterceptDeathRuntimeTest bugOverviewChessTallnutInterceptDeathRuntimeTest = this;
				int condition;
				if (chess.StateMachine?.IsInitialized ?? false)
				{
					ZombieDeathComponent zombieDeathComponent = chess.zombieDeathComponent;
					if (zombieDeathComponent != null && !zombieDeathComponent.IsReleased)
					{
						DestroyComponent destroyComponent = chess.destroyComponent;
						if (destroyComponent != null && !destroyComponent.IsReleased)
						{
							condition = ((attackComponent != null && !attackComponent.IsReleased) ? 1 : 0);
							goto IL_0491;
						}
					}
				}
				condition = 0;
				goto IL_0491;
				IL_0491:
				bugOverviewChessTallnutInterceptDeathRuntimeTest.Check((byte)condition != 0, "The real Chess state machine, jump attack, death, and destroy components must be active.");
				BugOverviewChessTallnutInterceptDeathRuntimeTest bugOverviewChessTallnutInterceptDeathRuntimeTest2 = this;
				TowerDefenseCharacterInstance instance = tallnut.instance;
				bugOverviewChessTallnutInterceptDeathRuntimeTest2.Check(instance != null && instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL, "The interception fixture must use the real tall Tallnut target.");
				Check(chess.hasPogo && (chess.instance?.ArmorHas("Pogo") ?? false), "The Chess zombie must begin alive with its real pogo armor.");
				Check(chess.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && tallnut.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && chess.HasHitBox && tallnut.HasHitBox, "The real opposing camps and collision bodies must be registered.");
				chess.sprite.SetAnimation("Pogo");
				Check(chess.sprite.clip == "Pogo", "The fixture must begin during Pogo; got " + chess.sprite.clip + ".");
				chess.SetLogicalGlobalPosition(overlapPosition);
				tallnut.SetLogicalGlobalPosition(overlapPosition);
				chess.gridPos = overlapGrid;
				tallnut.gridPos = overlapGrid;
				attackComponent.target = tallnut;
				attackComponent.alive = true;
				attackComponent.timer = 0.0;
				attackComponent.checkIntrevalNow = 0;
				chess.pogoPlant = true;
				chess.isJump = true;
				Check(attackComponent.HasAttackGridTargetCandidates() && attackComponent.CanAttack() && attackComponent.target == tallnut, "The real Chess jump check must acquire the overlapping Tallnut.");
				chess.PogoProcessing(1.0 / 60.0);
				await WaitFrames(2);
				Check(!chess.hasPogo && !chess.isJump, "Tallnut interception must remove Chess jump capability.");
				BugOverviewChessTallnutInterceptDeathRuntimeTest bugOverviewChessTallnutInterceptDeathRuntimeTest3 = this;
				TowerDefenseCharacterInstance instance2 = chess.instance;
				bugOverviewChessTallnutInterceptDeathRuntimeTest3.Check(instance2 != null && !instance2.ArmorHas("Pogo"), "Tallnut interception must remove the real pogo armor.");
				bool flag = chess.StateMachine?.CurrentStateHandle?.StableId == "zombie.walk";
				if (flag)
				{
					string clip = chess.sprite.clip;
					bool flag2 = ((clip == "Idle" || clip == "Walk") ? true : false);
					flag = flag2;
				}
				Check(flag, $"Intercepted Chess must return to its walking state; state={chess.StateMachine?.CurrentStateHandle?.StableId}, clip={chess.sprite.clip}.");
				chess.die = true;
				chess.nearDie = true;
				chess.Die();
				await WaitFrames(1);
				Check(chess.StateMachine?.CurrentStateHandle?.StableId == "zombie.die" && chess.die && chess.nearDie, "The intercepted Chess must enter the real death state; state=" + chess.StateMachine?.CurrentStateHandle?.StableId + ".");
				Check(chess.zombieDeathComponent.IsDeathAnimationClip(chess.sprite.clip), "The intercepted Chess must start its authored death clip; got " + chess.sprite.clip + ".");
				string deathClip = chess.sprite.clip;
				chess.AnimeCompleted("Pogo");
				Check(chess.sprite.clip == deathClip && chess.StateMachine?.CurrentStateHandle?.StableId == "zombie.die", "A queued Pogo completion must not restart or leave the active death state.");
				chess.AnimeCompleted("Idle");
				Check(chess.sprite.clip == deathClip && chess.StateMachine?.CurrentStateHandle?.StableId == "zombie.die", "A queued post-interception Idle completion must not restart the death clip.");
				await WaitFrames(2);
				Check(GodotObject.IsInstanceValid(chess) && !chess.isDestroy && chess.sprite.clip == deathClip, "The corpse must remain on one uninterrupted death animation before completion.");
				chess.AnimeCompleted(deathClip);
				await WaitSeconds(0.8);
				await WaitFrames(2);
				Check(!GodotObject.IsInstanceValid(chess), "Completing the uninterrupted death animation must remove the Chess corpse.");
				goto end_IL_00b7;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewChessTallnutInterceptDeathRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(chess))
			{
				chess.QueueFree();
			}
			if (GodotObject.IsInstanceValid(tallnut))
			{
				tallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag3 = _failures == 0 && _checks == 21;
		GD.Print($"CHESS_TALLNUT_INTERCEPT_DEATH_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewChessTallnutInterceptDeathRuntimeTest] " + message);
		}
	}
}
