using System;
using System.Threading.Tasks;
using Godot;

public partial class GarlicChomperBiteRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Vector2I previousGridNum = manager.gridNum;
		Vector2 previousGridSize = manager.gridSize;
		try
		{
			manager.gridNum = new Vector2I(9, 5);
			manager.gridSize = new Vector2(80f, 98f);
			await CheckBite(targetingPlant: false, immune: false, passiveOverlap: false);
			await CheckBite(targetingPlant: true, immune: false, passiveOverlap: false);
			await CheckBite(targetingPlant: true, immune: false, passiveOverlap: true);
			await CheckBite(targetingPlant: true, immune: true, passiveOverlap: true);
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[GarlicChomperBite] {value}");
		}
		finally
		{
			manager.gridNum = previousGridNum;
			manager.gridSize = previousGridSize;
		}
		GD.Print($"GARLIC_CHOMPER_BITE_RESULT passed={_failures == 0 && _checks == 20} checks={_checks} failures={_failures}");
		GetTree().Quit((_failures != 0 || _checks != 20) ? 2 : 0);
	}

	private async Task CheckBite(bool targetingPlant, bool immune, bool passiveOverlap)
	{
		TowerDefensePlantGarlicChomper plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter5/GarlicChomper/Scene/TowerDefensePlantGarlicChomper.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<TowerDefensePlantGarlicChomper>(PackedScene.GenEditState.Disabled);
		TowerDefenseZombie zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
		try
		{
			plant.gridPos = new Vector2I(3, 2);
			zombie.gridPos = new Vector2I(3, 2);
			plant.Position = TowerDefenseManager.Instance.GetMapCellPosCenter(plant.gridPos);
			zombie.Position = plant.Position + new Vector2(20f, 0f);
			AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			plant.ProcessMode = ProcessModeEnum.Disabled;
			zombie.instance.hitpoints = 1000.0;
			zombie.garlicComponent.reactionDelay = 0.03f;
			zombie.garlicComponent.grossoutDuration = 0.03f;
			zombie.garlicComponent.changeLineDuration = 0.08f;
			zombie.garlicComponent.moveDownChance = 1f;
			if (immune)
			{
				zombie.instance.unUseBuffFlags |= 128;
			}
			zombie.attackComponent.target = (targetingPlant ? plant : null);
			float originalY = zombie.GetLogicalGlobalPosition().Y;
			int originalLine = zombie.gridPos.Y;
			plant.chomperComponent.BitCharacter(zombie);
			Check(Math.Abs(zombie.instance.hitpoints - 920.0) < 0.001, $"targeting={targetingPlant}, immune={immune}：主动啃咬必须造成80点伤害。");
			Check(zombie.isGarlic == !immune, $"targeting={targetingPlant}, immune={immune}：主动啃咬必须按免疫状态立即触发大蒜效果。");
			if (passiveOverlap)
			{
				plant.AttackDeal(zombie, "Eat", 1.0);
			}
			plant.chomperComponent.BitCharacter(zombie);
			await WaitFrames(40);
			int num = originalLine + ((!immune) ? 1 : 0);
			Check(zombie.gridPos.Y == num, $"targeting={targetingPlant}, immune={immune}：应只换到第{num}行，实际为{zombie.gridPos.Y}。");
			float num2 = originalY + (immune ? 0f : TowerDefenseManager.Instance.GetMapGridSize().Y);
			Check(Mathf.Abs(zombie.GetLogicalGlobalPosition().Y - num2) < 0.1f, $"targeting={targetingPlant}, immune={immune}：逻辑位置必须真实移动一行。");
			Check(!zombie.isGarlic && !zombie.isChangeLine, "效果结束后必须释放大蒜与换行状态。");
		}
		finally
		{
			plant.QueueFree();
			zombie.QueueFree();
			await WaitFrames(4);
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GarlicChomperBite] " + message);
		}
	}
}
