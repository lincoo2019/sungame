using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventCreateProjectile : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double speed = 300.0;

	[Export(PropertyHint.None, "")]
	public double dir;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData;

	public string projectileName
	{
		get
		{
			if (projectileData != null)
			{
				return (string)projectileData.Get("projectileName");
			}
			return "";
		}
	}

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL;
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		Run(target, 0.0, pos, Vector2.FromAngle(Mathf.DegToRad((float)dir)) * (float)speed, projectileData, target.instance.collisionFlags, camp);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL;
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		Run(target, 0.0, pos, Vector2.FromAngle(Mathf.DegToRad((float)dir)) * (float)speed, projectileData, target.instance.collisionFlags, camp);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, projectile.height, projectile.GlobalPosition - projectile.velocity.Normalized() * 10f, Vector2.FromAngle(Mathf.DegToRad((float)dir)) * (float)speed, projectileData, projectile.collisionFlags, projectile.camp);
	}

	public override void Init(Dictionary valueDictionary)
	{
		string text = valueDictionary.GetValueOrDefault("ProjectileName", Variant.From<string>("")).AsString();
		if (text != "")
		{
			projectileData = new TowerDefenseProjectileCreateData(new StringName(text));
		}
		speed = valueDictionary.GetValueOrDefault("Speed", Variant.From<double>(300.0)).AsDouble();
		dir = valueDictionary.GetValueOrDefault("Dir", Variant.From<double>(0.0)).AsDouble();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CreateProjectile",
			["Value"] = new Dictionary
			{
				["ProjectileName"] = projectileName,
				["Speed"] = speed,
				["Dir"] = dir
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, double height = 0.0, Vector2 pos = default(Vector2), Vector2 velocity = default(Vector2), TowerDefenseProjectileCreateData projectileData = null, int collisionFlags = 1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT)
	{
		if (velocity == default(Vector2))
		{
			velocity = new Vector2(300f, 0f);
		}
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			checkAllOverride = true,
			initialRotationOverride = velocity.Angle()
		};
		FireComponent.CreateProjectilePosition(null, null, height, pos, velocity, projectileData, collisionFlags, camp, default(Vector2), overrides);
	}
}
