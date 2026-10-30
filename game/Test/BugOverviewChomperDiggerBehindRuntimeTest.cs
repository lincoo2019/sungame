using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewChomperDiggerBehindRuntimeTest : Node
{
	private const string ChomperScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Chomper/Scene/TowerDefensePlantChomper.tscn";

	private const string DiggerScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewChomperDiggerBehindControlStub control = null;
		TowerDefensePlantChomper chomper = null;
		TowerDefenseZombieDigger digger = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
				}
				control = new BugOverviewChomperDiggerBehindControlStub
				{
					Name = "ChomperDiggerBehindControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				chomper = InstantiateCharacter<TowerDefensePlantChomper>("res://Asset/Anime/Character/Plant/Chapter0/Chomper/Scene/TowerDefensePlantChomper.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.PLANT, EncounterGrid, new Vector2(400f, 200f));
				digger = InstantiateCharacter<TowerDefenseZombieDigger>("res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, EncounterGrid, new Vector2(352f, 200f));
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(chomper) && chomper.config?.name == "PlantChomper", "The regression must instantiate the real Chomper scene.");
				Check(GodotObject.IsInstanceValid(digger) && digger.config?.name == "ZombieDigger", "The regression must instantiate the real Digger Zombie scene.");
				if (!GodotObject.IsInstanceValid(chomper) || !GodotObject.IsInstanceValid(digger))
				{
					throw new InvalidOperationException("The real Chomper/Digger actors could not be instantiated.");
				}
				digger.digOver = true;
				digger.LandEntered();
				await WaitFrames(1);
				manager.CharacterRegister(chomper);
				manager.CharacterRegister(digger);
				ChomperComponent component = chomper.componentManager?.GetRuntime<ChomperComponent>();
				AttackComponent attackComponent = chomper.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				Check(component != null && !component.IsReleased && attackComponent != null && !attackComponent.IsReleased, "The real Chomper bite and attack components must be active.");
				Check(digger.CurrentStateHandle?.StableId != "zombie.digger.dig" && (digger.instance.collisionFlags & 1) != 0, "The Digger fixture must be a ground target behind Chomper, not an underground-only target.");
				Check(chomper.CanTarget(digger) && chomper.CanCollision(digger.instance.maskFlags), "The behind Digger must be camp/collision-reachable so only the mouth-side guard can reject it.");
				if (component == null || component.IsReleased || attackComponent == null || attackComponent.IsReleased)
				{
					throw new InvalidOperationException("The Chomper runtime components are unavailable.");
				}
				double currentHitPoint = digger.GetCurrentHitPoint();
				component.BitCharacter(digger);
				Check(!digger.die && !digger.nearDie && !digger.isChomp && !component.eatCharacter, "A direct BitCharacter call must not consume a Digger positioned behind Chomper.");
				Check(Math.Abs(digger.GetCurrentHitPoint() - currentHitPoint) < 0.001, "The rejected behind bite must not damage the Digger.");
				chomper.Idle();
				attackComponent.target = digger;
				attackComponent.timer = 0.0;
				component.IdleProcessing(0.016);
				Check(component.StateMachine?.CurrentStateHandle?.StableId != "chomper.attack" && attackComponent.target == null, "Natural idle acquisition must clear the behind Digger instead of entering Bite.");
				Check(!digger.die && !digger.nearDie && !digger.isChomp && !component.eatCharacter, "The natural behind-target rejection must leave the Digger alive and unchomped.");
				digger.gridPos = new Vector2I(5, EncounterGrid.Y);
				digger.SetLogicalGlobalPosition(new Vector2(482f, 200f));
				await WaitFrames(1);
				double currentHitPoint2 = digger.GetCurrentHitPoint();
				component.BitCharacter(digger);
				Check(digger.die || digger.nearDie || digger.isChomp || component.eatCharacter, "Moving the same real Digger to Chomper's mouth side must still allow the authored swallow.");
				Check(digger.GetCurrentHitPoint() < currentHitPoint2 || digger.isChomp || component.eatCharacter, "The front-side bite must still apply the authored Chomper consume path.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewChomperDiggerBehindRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				if (GodotObject.IsInstanceValid(chomper))
				{
					manager.CharacterUnregister(chomper);
				}
				if (GodotObject.IsInstanceValid(digger))
				{
					manager.CharacterUnregister(digger);
				}
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(chomper) && !chomper.IsQueuedForDeletion())
			{
				chomper.QueueFree();
			}
			if (GodotObject.IsInstanceValid(digger) && !digger.IsQueuedForDeletion())
			{
				digger.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"CHOMPER_DIGGER_BEHIND_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T InstantiateCharacter<T>(string path, Node parent, TowerDefenseEnum.CHARACTER_CAMP camp, Vector2I grid, Vector2 position) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.editorPreviewMode = false;
		val.inGame = true;
		val.camp = camp;
		val.gridPos = grid;
		val.GlobalPosition = position;
		parent.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
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
			GD.PushError("[BugOverviewChomperDiggerBehindRuntimeTest] " + message);
		}
	}
}
