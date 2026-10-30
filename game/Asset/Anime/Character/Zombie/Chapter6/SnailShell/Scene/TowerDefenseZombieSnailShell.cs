using Godot;

[Tool]
public partial class TowerDefenseZombieSnailShell : TowerDefenseZombie
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			targetRegistrationComponent.canCarry = false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (TowerDefenseManager.Instance.IsGameRunning() && inGame && !die && !nearDie && attackComponent.CanAttack())
			{
				attackComponent.AttackExecute(GetArmorFromName("Shell").hitPoints);
				Die();
			}
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Shell")
		{
			Die();
		}
	}

	public override void DieEntered()
	{
		if (!die)
		{
			HitpointsEmpty();
			die = true;
		}
		if (!nearDie)
		{
			HitpointsNearDie();
			nearDie = true;
		}
		if (base.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && GameSaveManager.Instance.GetFeatureValue("Coins") != 0)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectCreate(logicalGlobalPosition, GetGroundHeight(logicalGlobalPosition.Y), new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (towerDefenseGroundItemBase != null)
			{
				towerDefenseGroundItemBase.gridPos = base.gridPos;
			}
		}
		base.sprite.SetAnimation(base.dieAnimeClip, loop: false);
	}
}
