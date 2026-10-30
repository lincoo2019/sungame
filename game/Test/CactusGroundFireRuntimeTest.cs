using System;
using System.Threading.Tasks;
using Godot;

public partial class CactusGroundFireRuntimeTest : Node
{
	private const string CactusScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn";

	private const string GroundZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			await RunScenario();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[CactusGroundFireRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"CACTUS_GROUND_FIRE_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunScenario()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
		if (!GodotObject.IsInstanceValid(manager))
		{
			return;
		}
		TowerDefenseControlNew control = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		Node2D characterNode = new Node2D
		{
			Name = "CactusGroundFireCharacters"
		};
		TowerDefensePlantCactus cactus = null;
		TowerDefenseCharacter target = null;
		try
		{
			AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = characterNode;
			manager.currentControl = control;
			manager.gridBeginPos = new Vector2(0f, 100f);
			manager.gridSize = new Vector2(100f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
			cactus = packedScene?.Instantiate<TowerDefensePlantCactus>(PackedScene.GenEditState.Disabled);
			target = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(cactus) && GodotObject.IsInstanceValid(target), "Real Cactus and ground Zombie scenes must instantiate.");
			if (GodotObject.IsInstanceValid(cactus) && GodotObject.IsInstanceValid(target))
			{
				cactus.Position = new Vector2(100f, 100f);
				cactus.gridPos = new Vector2I(1, 1);
				cactus.inGame = true;
				target.Position = new Vector2(500f, 100f);
				target.gridPos = new Vector2I(5, 1);
				target.inGame = true;
				characterNode.AddChild(cactus, forceReadableName: false, InternalMode.Disabled);
				characterNode.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				FireComponent fireComponent = cactus.componentManager?.GetRuntime<FireComponent>("character.fire");
				FireComponentExtendCactus fireComponentExtendCactus = cactus.componentManager?.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
				Check(fireComponent != null && !fireComponent.IsReleased && fireComponentExtendCactus != null && !fireComponentExtendCactus.IsReleased, "Cactus fire runtimes must be active.");
				if (fireComponent != null && !fireComponent.IsReleased && fireComponentExtendCactus != null && !fireComponentExtendCactus.IsReleased)
				{
					fireComponent.groundRight = 10000f;
					fireComponent.timer = 0f;
					fireComponent.checkInterval = 0;
					int collectionFlag = 9;
					Check(fireComponent.CanFireCheckOnce(null, collectionFlag), "Cactus must acquire a real ground Zombie in its forward ray.");
					fireComponentExtendCactus.IdleProcessing(0.0);
					Check(fireComponentExtendCactus.CanRun(), "Cactus extension must release the FireComponent gate for a ground target.");
					fireComponent.IdleProcessing(0.0);
					Check(fireComponent.StateMachine?.CurrentStateHandle?.StableId == "fire.attack", "Cactus must enter fire.attack; got " + (fireComponent.StateMachine?.CurrentStateHandle?.StableId ?? "<null>") + ".");
					Check(fireComponent.runningCheckId == 1, $"Cactus must use its ground fire check; got {fireComponent.runningCheckId}.");
					return;
				}
				return;
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(cactus))
			{
				cactus.QueueFree();
			}
			if (GodotObject.IsInstanceValid(target))
			{
				target.QueueFree();
			}
			if (GodotObject.IsInstanceValid(characterNode))
			{
				characterNode.QueueFree();
			}
			control.Free();
			manager.currentControl = null;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CactusGroundFireRuntimeTest] " + message);
		}
	}
}
