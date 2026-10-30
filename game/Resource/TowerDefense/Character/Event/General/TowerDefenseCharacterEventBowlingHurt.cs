using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventBowlingHurt : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 1800.0;

	[Export(PropertyHint.Enum, "Front,Back")]
	public string dir = "Front";

	[Export(PropertyHint.None, "")]
	public bool burns = true;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, num, dir);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, num * delta, dir);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, num);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, double num = 1800.0, string dir = "Front")
	{
		Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
		Vector2 velocity = new Vector2((float)((double)(logicalGlobalPosition.X - pos.X) * GD.RandRange(3.0, 6.0)), -200f);
		bool flag = false;
		bool flag2 = false;
		if (pos.Y > logicalGlobalPosition.Y + 5f || pos.Y < logicalGlobalPosition.Y - 20f)
		{
			flag = true;
		}
		if (!flag)
		{
			if (target.HasShield())
			{
				num = ((!(target.config.name == "ZombiePaper")) ? (num * (2.0 / 9.0)) : 150.0);
				flag2 = true;
			}
			else if (target.HasHelm())
			{
				num *= 2.0 / 9.0;
			}
		}
		else if (target.HasHelm())
		{
			num *= 2.0 / 9.0;
		}
		double num2 = 1.0;
		if (dir == "Back")
		{
			num2 = -1.0;
		}
		int num3;
		if (target is TowerDefenseZombie && GodotObject.IsInstanceValid(target.instance))
		{
			num3 = ((target.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS) ? 1 : 0);
			if (num3 != 0 && (flag2 || (target is TowerDefenseZombie && target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE)))
			{
				target.ApplyBowlingImpactDisplacement(50.0 * num2);
			}
		}
		else
		{
			num3 = 0;
		}
		double num4 = target.BowlingHurt(num, playSplatAudio: true, velocity, !flag && flag2);
		if (num3 != 0 && num4 > 0.0)
		{
			target.ApplyBowlingImpactDisplacement(100.0 * num2);
		}
	}
}
