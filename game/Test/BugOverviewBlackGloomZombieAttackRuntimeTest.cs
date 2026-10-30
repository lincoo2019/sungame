using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewBlackGloomZombieAttackRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackGloomShroom/TowerDefenseZombieFootballBlackGloomShroom.tscn";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewBlackGloomZombieAttackControlStub control = null;
		TowerDefenseZombieFootballBlackGloomShroom zombie = null;
		TowerDefensePlant plant = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00ce;
				}
				control = new BugOverviewBlackGloomZombieAttackControlStub
				{
					Name = "BlackGloomZombieAttackControl",
					isGameRunning = false,
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
				zombie = LoadCharacter<TowerDefenseZombieFootballBlackGloomShroom>("res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackGloomShroom/TowerDefenseZombieFootballBlackGloomShroom.tscn");
				plant = LoadCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieFootballBlackGloomShroom", "The reported Black Gloom-shroom Football Zombie scene must instantiate.");
				Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == "PlantWallnut", "The fixture must use a real Wall-nut target.");
				if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(plant))
				{
					goto end_IL_00ce;
				}
				Vector2 overlapPosition = new Vector2(400f, 252f);
				Vector2I overlapGrid = new Vector2I(4, 2);
				zombie.inGame = true;
				zombie.editorPreviewMode = false;
				zombie.gridPos = overlapGrid;
				zombie.GlobalPosition = overlapPosition;
				plant.inGame = true;
				plant.editorPreviewMode = false;
				plant.gridPos = overlapGrid;
				plant.GlobalPosition = overlapPosition;
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				plant.ProcessMode = ProcessModeEnum.Disabled;
				zombie.gridPos = overlapGrid;
				zombie.GlobalPosition = overlapPosition;
				plant.gridPos = overlapGrid;
				plant.GlobalPosition = overlapPosition;
				await WaitFrames(2);
				control.isGameRunning = true;
				AttackComponent attackComponent = zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				AttackComponent attackComponent2 = zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				Check(attackComponent != null && !attackComponent.IsReleased && zombie.attackComponent == attackComponent, "The real primary bite component must be active and bound to the zombie facade.");
				Check(attackComponent2 != null && !attackComponent2.IsReleased && attackComponent2.checkAll, "The real Gloom-shroom area attack component must be active.");
				Check(zombie.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && plant.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && zombie.HasHitBox && plant.HasHitBox, "The real opposing camps and hit boxes must be registered.");
				if (attackComponent == null || attackComponent.IsReleased || attackComponent2 == null || attackComponent2.IsReleased)
				{
					goto end_IL_00ce;
				}
				zombie.groundRight = 1000.0;
				attackComponent.groundRight = 1000.0;
				attackComponent.alive = true;
				attackComponent.timer = 0.0;
				attackComponent.checkIntrevalNow = 0;
				attackComponent2.groundRight = 1000.0;
				attackComponent2.alive = true;
				attackComponent2.timer = 0.0;
				attackComponent2.checkIntrevalNow = 0;
				Check(attackComponent.CanAttack() && attackComponent.target == plant, "Black Gloom-shroom Football Zombie must acquire the overlapping plant for its normal bite.");
				Check(attackComponent2.CanAttack(), "The independent Gloom-shroom area component must detect the nearby plant while the zombie is biting.");
				zombie.Walk();
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.WalkProcessing(0.016);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.attack", "Acquiring the plant must enter the authored zombie attack state; current=" + zombie.CurrentStateHandle?.StableId + ".");
				double hitpoints = plant.instance.hitpoints;
				zombie.startAttack = true;
				zombie.AttackProcessing(0.5);
				Check(plant.instance.hitpoints < hitpoints, $"The real bite loop must damage Wall-nut; hp stayed {plant.instance.hitpoints}/{hitpoints}.");
				double hitpointsBeforeGloom = plant.instance.hitpoints;
				plant.ProcessMode = ProcessModeEnum.Inherit;
				zombie.GloomShroomAttack();
				await WaitFrames(3);
				Check(plant.instance.hitpoints < hitpointsBeforeGloom, $"The real Gloom-shroom burst must damage Wall-nut; hp stayed {plant.instance.hitpoints}/{hitpointsBeforeGloom}.");
				goto end_IL_00b7;
				end_IL_00ce:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewBlackGloomZombieAttackRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
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
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 11;
		GD.Print($"BLACK_GLOOM_ZOMBIE_ATTACK_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
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
			GD.PushError("[BugOverviewBlackGloomZombieAttackRuntimeTest] " + message);
		}
	}
}
