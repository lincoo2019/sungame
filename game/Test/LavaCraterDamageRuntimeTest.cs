using System;
using Godot;

public partial class LavaCraterDamageRuntimeTest : Node
{
	private const string LavaCraterScenePath = "res://Asset/Anime/Character/Crater/CraterLava/Scene/TowerDefenseCraterLava.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseControlNew control = null;
		TowerDefenseCharacter crater = null;
		TowerDefenseCharacter zombie = null;
		try
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(instance))
			{
				return;
			}
			control = (instance.currentControl = new TowerDefenseControlNew
			{
				isGameRunning = true
			});
			instance.gridBeginPos = new Vector2(0f, 100f);
			instance.gridSize = new Vector2(100f, 76f);
			instance.gridNum = new Vector2I(9, 5);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Crater/CraterLava/Scene/TowerDefenseCraterLava.tscn", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
			crater = packedScene?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			zombie = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(crater) && GodotObject.IsInstanceValid(zombie), "Lava crater and normal zombie scenes must instantiate.");
			if (!GodotObject.IsInstanceValid(crater) || !GodotObject.IsInstanceValid(zombie))
			{
				return;
			}
			Vector2 position = new Vector2(300f, 176f);
			Vector2I gridPos = new Vector2I(3, 1);
			crater.Position = position;
			crater.gridPos = gridPos;
			crater.inGame = true;
			zombie.Position = position;
			zombie.gridPos = gridPos;
			zombie.inGame = true;
			AddChild(crater, forceReadableName: false, InternalMode.Disabled);
			AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			PeriodicAreaEventComponent periodicAreaEventComponent = crater.componentManager?.GetRuntime<PeriodicAreaEventComponent>("character.periodic_area_event");
			Check(periodicAreaEventComponent != null && !periodicAreaEventComponent.IsReleased, "Lava crater PeriodicAreaEventComponent must be active.");
			if (periodicAreaEventComponent == null || periodicAreaEventComponent.IsReleased)
			{
				return;
			}
			double hitpointsBefore = zombie.instance.hitpoints;
			for (int frame = 0; frame < 75; frame++)
			{
				if (!(Math.Abs(zombie.instance.hitpoints - hitpointsBefore) < 0.001))
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			double num = hitpointsBefore - zombie.instance.hitpoints;
			Check(Math.Abs(num - 40.0) < 0.001, $"One lava-crater interval must deal 40 damage; dealt {num}.");
			Check(zombie.buff.BuffHas("RedHeat"), "A zombie damaged by the lava crater must receive RedHeat.");
			zombie.Position = new Vector2(600f, 176f);
			zombie.gridPos = new Vector2I(6, 1);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			double outsideHitpoints = zombie.instance.hitpoints;
			for (int frame = 0; frame < 75; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Check(Math.Abs(zombie.instance.hitpoints - outsideHitpoints) < 0.001, "A zombie outside the lava-crater rectangle must stop taking damage.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[LavaCraterDamageRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(crater))
			{
				crater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			TowerDefenseManager instance2 = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance2))
			{
				instance2.currentControl = null;
			}
			control?.Free();
		}
		bool flag = _failures == 0;
		GD.Print($"LAVA_CRATER_DAMAGE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LavaCraterDamageRuntimeTest] " + message);
		}
	}
}
