using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewZombieCorpseSlideRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ResultMarker = "ZOMBIE_CORPSE_SLIDE_RESULT";

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		BugOverviewZombieCorpseSlideControlStub control = null;
		TowerDefenseZombieNormal zombie = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
				control = new BugOverviewZombieCorpseSlideControlStub
				{
					Name = "ZombieCorpseSlideControl",
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
				TowerDefenseZombie.UseBatch = false;
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie) && zombie.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", "The fixture must instantiate the reported real ordinary-zombie scene.");
				Require(GodotObject.IsInstanceValid(zombie), "The real ordinary-zombie scene failed to instantiate.");
				zombie.editorPreviewMode = false;
				zombie.inGame = true;
				zombie.gridPos = new Vector2I(6, 2);
				zombie.GlobalPosition = new Vector2(600f, 252f);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				Check(zombie.config?.name == "ZombieNormal" && (zombie.sprite?.HasClip("Death1") ?? false) && zombie.sprite.HasClip("Death2"), "The scenario must use the authored ordinary zombie and its real death clips.");
				GroundMoveComponent groundMove = zombie.groundMoveComponent;
				BugOverviewZombieCorpseSlideRuntimeTest bugOverviewZombieCorpseSlideRuntimeTest = this;
				int condition;
				if (zombie.HasValidRuntimeConfiguration && groundMove != null && !groundMove.IsReleased && groundMove.HasMovementSource)
				{
					ZombieDeathComponent zombieDeathComponent = zombie.zombieDeathComponent;
					condition = ((zombieDeathComponent != null && !zombieDeathComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewZombieCorpseSlideRuntimeTest.Check((byte)condition != 0, "The real zombie must bind its authored GroundSlot and shared death runtime.");
				int condition2;
				if (groundMove != null && !groundMove.IsReleased && groundMove.HasMovementSource)
				{
					ZombieDeathComponent zombieDeathComponent = zombie.zombieDeathComponent;
					condition2 = ((zombieDeathComponent != null && !zombieDeathComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition2 = 0;
				}
				Require((byte)condition2 != 0, "The ordinary-zombie movement/death runtimes did not initialize.");
				zombie.zombieDeathComponent.dropFeatureName = "";
				zombie.Idle();
				await WaitFrames(2);
				Check(zombie.CurrentStateHandle?.StableId == "character.idle" && !groundMove.Alive, $"The race fixture must begin from stopped Idle; state={zombie.CurrentStateHandle?.StableId}, alive={groundMove.Alive}.");
				zombie.Walk();
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk" && !groundMove.Alive, $"Fresh Walk must still be inside its delayed activation window; state={zombie.CurrentStateHandle?.StableId}, alive={groundMove.Alive}.");
				Vector2 deathPosition = zombie.GlobalPosition;
				zombie.instance.SkipInvincibleDealHurt(zombie.instance.hitpoints + 1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
				await WaitFrames(2);
				Check(zombie.die && zombie.nearDie && zombie.CurrentStateHandle?.StableId == "zombie.die" && zombie.zombieDeathComponent.IsDeathAnimationClip(zombie.sprite.clip), $"The lethal hit must enter a real death clip; state={zombie.CurrentStateHandle?.StableId}, clip={zombie.sprite?.clip}.");
				Check(!groundMove.Alive, "Die entry must disable the real GroundMove before the stale Walk timer expires.");
				await WaitFrames(18);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.die" && zombie.zombieDeathComponent.IsDeathAnimationClip(zombie.sprite.clip), "The corpse must remain in the production death state after the delayed Walk callback expires.");
				Check(!groundMove.Alive, "The expired Walk callback must not re-enable GroundMove on the corpse.");
				Check(zombie.GlobalPosition.DistanceTo(deathPosition) <= 0.01f, $"The ordinary-zombie corpse must not slide after death; start={deathPosition}, actual={zombie.GlobalPosition}.");
				groundMove.SetAlive(true);
				Check(!groundMove.Alive, "GroundMove.SetAlive must reject any later stale activation for a dead owner.");
				Vector2 extendedDeathPosition = zombie.GlobalPosition;
				await WaitFrames(30);
				Check(zombie.GlobalPosition.DistanceTo(extendedDeathPosition) <= 0.01f, "The real death animation must remain position-stable through a longer post-race sample.");
				Check(zombie.die && !zombie.isDestroy, "The position assertion must observe a live corpse before normal fade cleanup, not an already destroyed node.");
				Check(groundMove.parent == zombie && groundMove.groundNode.GetParent() == zombie.sprite, "The verified movement source must still be the real zombie's authored GroundSlot.");
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
			}
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(5);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(3);
		}
		Finish();
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
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
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[BugOverviewZombieCorpseSlideRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 15;
		GD.Print($"{"ZOMBIE_CORPSE_SLIDE_RESULT"} passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
