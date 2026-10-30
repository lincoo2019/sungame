using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventProjectileCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData;

	[Export(PropertyHint.None, "")]
	public int createNum = 8;

	[Export(PropertyHint.None, "")]
	public Vector4I createGridToRange = new Vector4I(-1, -1, 1, 1);

	[Export(PropertyHint.None, "")]
	public Vector2 createSpeedToRange = new Vector2(-100f, 100f);

	[Export(PropertyHint.None, "")]
	public bool useFall;

	public string projectileName
	{
		get
		{
			if (projectileData != null)
			{
				return projectileData.projectileName.ToString();
			}
			return "";
		}
	}

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, projectileData, createNum, createGridToRange, createSpeedToRange, useFall);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, projectileData, createNum, createGridToRange, createSpeedToRange, useFall);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, projectileData, createNum, createGridToRange, createSpeedToRange, useFall, projectile.collisionFlags, projectile.camp);
	}

	public override void ExecuteGroundProject(TowerDefenseProjectile projectile, Vector2 position, Vector2I gridPos)
	{
		RunAtPosition(position, gridPos, projectileData, createNum, createGridToRange, createSpeedToRange, useFall, projectile.collisionFlags, projectile.camp);
	}

	public override void Init(Dictionary valueDictionary)
	{
		string text = valueDictionary.GetValueOrDefault("ProjectileName", "").AsString();
		if (text != "")
		{
			projectileData = new TowerDefenseProjectileCreateData(new StringName(text));
		}
		createNum = valueDictionary.GetValueOrDefault("CreateNum", 8).AsInt32();
		createGridToRange = valueDictionary.GetValueOrDefault("CreateGridToRange", new Vector4I(-1, -1, 1, 1)).AsVector4I();
		createSpeedToRange = valueDictionary.GetValueOrDefault("CreateSpeedToRange", new Vector2(-100f, 100f)).AsVector2();
		useFall = valueDictionary.GetValueOrDefault("useFall", false).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "ProjectileCreate",
			["Value"] = new Dictionary
			{
				["ProjectileName"] = projectileName,
				["CreateNum"] = createNum,
				["CreateSpeedToRange"] = createSpeedToRange,
				["CreateGridToRange"] = createGridToRange,
				["useFall"] = useFall
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, TowerDefenseProjectileCreateData _projectileData, int _createNum = 8, Vector4I? _createGridToRange = null, Vector2? _createToRange = null, bool _useFall = false, int _collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP _camp = TowerDefenseEnum.CHARACTER_CAMP.ALL)
	{
		if (GodotObject.IsInstanceValid(target))
		{
			RunAtPosition(target.GetLogicalGlobalPosition(), target.gridPos, _projectileData, _createNum, _createGridToRange, _createToRange, _useFall, _collisionFlags, _camp);
		}
	}

	public static void RunAtPosition(Vector2 position, Vector2I gridPos, TowerDefenseProjectileCreateData _projectileData, int _createNum = 8, Vector4I? _createGridToRange = null, Vector2? _createToRange = null, bool _useFall = false, int _collisionFlags = -1, TowerDefenseEnum.CHARACTER_CAMP _camp = TowerDefenseEnum.CHARACTER_CAMP.ALL)
	{
		Vector4I vector4I = _createGridToRange ?? new Vector4I(-1, -1, 1, 1);
		Vector2 vector = _createToRange ?? new Vector2(-100f, 100f);
		for (int i = 0; i < _createNum; i++)
		{
			Vector2I vector2I = new Vector2I(GD.RandRange(vector4I.X, vector4I.Z), GD.RandRange(vector4I.Y, vector4I.W));
			Vector2 vector2 = new Vector2((float)GD.RandRange(vector.X, vector.Y), vector2I.Y * 100);
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = gridPos.Y + vector2I.Y
			};
			if (_useFall)
			{
				overrides.useFall = true;
				overrides.zOverride = 600.0;
				overrides.ySpeedOverride = 400.0;
			}
			else
			{
				overrides.useGravity = true;
				overrides.ySpeedOverride = -800.0;
				overrides.gravityOverride = 980.0;
			}
			FireComponent.CreateProjectilePosition(null, null, 0.0, position, vector2 / 1.5f, _projectileData, _collisionFlags, _camp, default(Vector2), overrides);
		}
	}
}
