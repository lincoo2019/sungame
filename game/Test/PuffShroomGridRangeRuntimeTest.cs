using System;
using System.Threading.Tasks;
using Godot;

public partial class PuffShroomGridRangeRuntimeTest : Node
{
	private const string PuffShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PuffShroom/Scene/TowerDefensePlantPuffShroom.tscn";

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
			GD.PushError($"[PuffShroomGridRangeRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"PUFF_SHROOM_GRID_RANGE_RESULT passed={flag} checks={_checks} failures={_failures}");
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
			Name = "PuffShroomGridRangeCharacters"
		};
		TowerDefenseCharacter puffShroom = null;
		TowerDefenseCharacter target = null;
		try
		{
			AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = characterNode;
			manager.currentControl = control;
			manager.gridBeginPos = new Vector2(0f, 100f);
			manager.gridSize = manager.GetMapGridSize();
			manager.gridNum = new Vector2I(9, 5);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PuffShroom/Scene/TowerDefensePlantPuffShroom.tscn", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
			puffShroom = packedScene?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			target = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(puffShroom) && GodotObject.IsInstanceValid(target), "Real Puff-shroom and ground Zombie scenes must instantiate.");
			if (GodotObject.IsInstanceValid(puffShroom) && GodotObject.IsInstanceValid(target))
			{
				puffShroom.Position = new Vector2(40f, 100f);
				puffShroom.gridPos = new Vector2I(1, 1);
				puffShroom.inGame = true;
				target.Position = new Vector2(360f, 100f);
				target.gridPos = new Vector2I(5, 1);
				target.inGame = true;
				characterNode.AddChild(puffShroom, forceReadableName: false, InternalMode.Disabled);
				characterNode.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				FireComponent fire = puffShroom.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fire != null && !fire.IsReleased, "Puff-shroom FireComponent runtime must be active.");
				if (fire != null && !fire.IsReleased)
				{
					Check(Mathf.IsEqualApprox(fire.checkLength, 3f), $"Puff-shroom checkLength must remain three cells; got {fire.checkLength}.");
					fire.groundRight = 10000f;
					int groundFlags = 9;
					Check(!fire.CanFireCheckOnce(null, groundFlags), "Puff-shroom must not acquire a target four cells ahead.");
					target.Position = new Vector2(280f, 100f);
					target.gridPos = new Vector2I(4, 1);
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					Check(fire.CanFireCheckOnce(null, groundFlags), "Puff-shroom must acquire a target three cells ahead.");
					Check(fire.firstCharacter == target, "The row-range query must preserve Puff-shroom firstCharacter.");
					return;
				}
				return;
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(puffShroom))
			{
				puffShroom.QueueFree();
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
			GD.PushError("[PuffShroomGridRangeRuntimeTest] " + message);
		}
	}
}
