using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventLadderPlace : TowerDefenseCharacterEventBase
{
	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public static void Run(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(target.cell))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemLadder");
		if (packetConfig != null)
		{
			TowerDefenseCellInstance cell = target.cell;
			if (cell.HasWallnut() && !GodotObject.IsInstanceValid(cell.characterLadder) && cell.CanPacketPlant(packetConfig))
			{
				packetConfig.Plant(cell.gridPos);
			}
		}
	}
}
