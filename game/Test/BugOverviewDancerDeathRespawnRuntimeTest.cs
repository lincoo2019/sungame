using System;
using Godot;

public partial class BugOverviewDancerDeathRespawnRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseCharacter[] array = new TowerDefenseCharacter[4];
		TowerDefenseZombie towerDefenseZombie = null;
		try
		{
			Check(GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "TowerDefenseManager autoload must be available.");
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				towerDefenseZombie = new TowerDefenseZombie
				{
					gridPos = new Vector2I(5, 3)
				};
				DancingComponent dancingComponent = new DancingComponent
				{
					parent = towerDefenseZombie
				};
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new TowerDefenseCharacter();
					dancingComponent.dancerList.Add(array[i]);
				}
				Check(!dancingComponent.CanSpawnDancer(), "Four living dancers must keep all summon slots occupied.");
				array[0].die = true;
				Check(dancingComponent.CanSpawnDancer(), "A dancer in the death state must free its summon slot immediately.");
				array[0].die = false;
				array[0].nearDie = true;
				Check(dancingComponent.CanSpawnDancer(), "A dancer entering death must not block its replacement summon.");
				array[0].nearDie = false;
				array[0].isDestroy = true;
				Check(dancingComponent.CanSpawnDancer(), "A dancer being destroyed must not keep its summon slot occupied.");
				array[0].isDestroy = false;
				array[0].Free();
				Check(dancingComponent.CanSpawnDancer(), "A freed dancer must continue to count as a vacant summon slot.");
				array[0] = null;
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewDancerDeathRespawnRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			TowerDefenseCharacter[] array2 = array;
			foreach (TowerDefenseCharacter towerDefenseCharacter in array2)
			{
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.Free();
				}
			}
			if (GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				towerDefenseZombie.Free();
			}
			bool flag = _failures == 0 && _checks == 6;
			GD.Print($"DANCER_DEATH_RESPAWN_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewDancerDeathRespawnRuntimeTest] " + message);
		}
	}
}
