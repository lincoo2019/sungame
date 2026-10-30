using Godot;

[Tool]
public partial class TowerDefenseZombieTarget : TowerDefenseZombie
{
	public bool hasTarget = true;

	public override void _Ready()
	{
		base._Ready();
		ConfigureIZM2HealthDisplay();
	}

	private void ConfigureIZM2HealthDisplay()
	{
		ShowHealthComponent obj = showHealthComponent;
		if (obj != null && !obj.IsReleased)
		{
			showHealthComponent.AllowRefreshOutsideComponentBattlefield = GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && TowerDefenseManager.Instance.IsIZM2Mode();
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Target")
		{
			hasTarget = false;
			Walk();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (TowerDefenseManager.Instance.IsGameRunning() && !base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.IsIZM2Mode())
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if ((double)globalPositionForPhysicsFrame.X > groundRight)
			{
				globalPositionForPhysicsFrame.X -= (float)(100.0 * delta);
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
		}
	}

	public override void Walk()
	{
		if (hasTarget)
		{
			Idle();
		}
		else
		{
			base.Walk();
		}
	}

	public override void Spawn()
	{
		if (!base.IsRemoteNetworkReplica)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(GD.RandRange(TowerDefenseManager.Instance.GetMapGridNum().X - 2, TowerDefenseManager.Instance.GetMapGridNum().X), base.gridPos.Y)).X;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}
}
