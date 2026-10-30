using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventGeneExplode : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public string plantTransformName = "PlantPeaShooterSingle";

	[Export(PropertyHint.None, "")]
	public string zombieTransformName = "ZombieNormal";

	[Export(PropertyHint.None, "")]
	public PackedScene effectScene;

	private static PackedScene _DEFAULT_GENETIC_EFFECT;

	private static PackedScene DEFAULT_GENETIC_EFFECT => _DEFAULT_GENETIC_EFFECT ?? (_DEFAULT_GENETIC_EFFECT = GD.Load<PackedScene>("uid://k8ms8gteeqp6"));

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, pos, plantTransformName, zombieTransformName, effectScene ?? DEFAULT_GENETIC_EFFECT);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, pos, plantTransformName, zombieTransformName, effectScene ?? DEFAULT_GENETIC_EFFECT);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, projectile.GlobalPosition, plantTransformName, zombieTransformName, effectScene ?? DEFAULT_GENETIC_EFFECT);
	}

	public static void Run(TowerDefenseCharacter target, Vector2 pos, string plantTransformName = "PlantPeaShooter", string zombieTransformName = "ZombieNormal", PackedScene effectScene = null)
	{
		if (!GodotObject.IsInstanceValid(target) || target.die || target.nearDie)
		{
			return;
		}
		if (target is TowerDefenseZombie towerDefenseZombie)
		{
			if ((GodotObject.IsInstanceValid(towerDefenseZombie.instance) ? towerDefenseZombie.instance.zombiePhysique : ((towerDefenseZombie.config as TowerDefenseZombieConfig)?.physique ?? TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL)) != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				target.TransformTo(zombieTransformName, null, effectScene);
			}
		}
		else if (target is TowerDefensePlant)
		{
			if (!IsProtectedByPumpkinRobot(target))
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(target.gridPos);
				if (GodotObject.IsInstanceValid(mapCell))
				{
					mapCell.TransformPlantsTo(plantTransformName, effectScene, IsProtectedByPumpkinRobot);
				}
			}
		}
		else
		{
			TowerDefenseCharacterEventExplodeHurt towerDefenseCharacterEventExplodeHurt = new TowerDefenseCharacterEventExplodeHurt();
			towerDefenseCharacterEventExplodeHurt.num = 1800.0;
			towerDefenseCharacterEventExplodeHurt.Execute(pos, target);
		}
	}

	private static bool IsProtectedByPumpkinRobot(TowerDefenseCharacter character)
	{
		if (character is TowerDefensePlantPumpkinRobot)
		{
			return true;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(character.gridPos);
		if (mapCell == null || !GodotObject.IsInstanceValid(mapCell.characterSurround))
		{
			return false;
		}
		return mapCell.characterSurround is TowerDefensePlantPumpkinRobot;
	}
}
