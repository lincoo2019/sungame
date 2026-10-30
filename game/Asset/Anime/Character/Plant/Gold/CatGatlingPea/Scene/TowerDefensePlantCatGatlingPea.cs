using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCatGatlingPea : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 4;

	private string _projectileName = "Star";

	public int currentFireNum;

	public int currentFireTotal;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.fireInterval = (float)fireInterval;
			((CatGatlingPeaSprite)base.sprite).head.OnAnimeEvent += AnimeEvent;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "fire"))
		{
			return;
		}
		if (currentFireNum == 0)
		{
			TowerDefenseProjectileCreateData projetile = _fireComponent.fireCheckList[0].projectile.GetProjetile();
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = (GodotObject.IsInstanceValid(projetile) ? (projetile.Duplicate(deep: true) as TowerDefenseProjectileCreateData) : null);
			if (GodotObject.IsInstanceValid(towerDefenseProjectileCreateData))
			{
				towerDefenseProjectileCreateData.fireMethodFlags = 32;
				towerDefenseProjectileCreateData.InvalidateConfigCache();
			}
			float[] array = new float[5] { 30f, 90f, 180f, 270f, 330f };
			for (int i = 0; i < array.Length; i++)
			{
				if (!GodotObject.IsInstanceValid(towerDefenseProjectileCreateData))
				{
					break;
				}
				AudioManager.Instance.AudioPlay("ProjectileThrow");
				Vector2 velocity = 300f * Vector2.FromAngle(Mathf.DegToRad(array[i]));
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f),
					spriteRotationOverride = Mathf.DegToRad(array[i])
				};
				_fireComponent.CreateProjectileByData(i + 1, velocity, towerDefenseProjectileCreateData, instance.collisionFlags + 2, base.camp, Vector2.Zero, overrides);
			}
			if (currentFireTotal != 10)
			{
				currentFireTotal++;
			}
		}
		if (currentFireTotal == 10 && currentFireNum == 0)
		{
			AudioManager.Instance.AudioPlay("ProjectileThrow");
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData2 = new TowerDefenseProjectileCreateData("StarBigCatGatlingPea");
			towerDefenseProjectileCreateData2.fireMethodFlags = 32;
			towerDefenseProjectileCreateData2.rangeType = "Boom";
			towerDefenseProjectileCreateData2.rangeOverride = true;
			towerDefenseProjectileCreateData2.rangeSize = new Vector2(0.6f, 0.6f);
			towerDefenseProjectileCreateData2.hitPesontage = 1.0;
			BulletFieldSpawnOverrides overrides2 = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(300f, 0f), towerDefenseProjectileCreateData2, instance.collisionFlags + 2, base.camp, Vector2.Zero, overrides2);
		}
		currentFireNum++;
		if (currentFireNum == _fireComponent.fireNum)
		{
			currentFireNum = 0;
			if (currentFireTotal == 10)
			{
				currentFireTotal = 0;
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["currentFireNum"] = currentFireNum,
			["currentFireTotal"] = currentFireTotal,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 4);
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Star");
		currentFireNum = (data.ContainsKey("currentFireNum") ? data["currentFireNum"].AsInt32() : 0);
		currentFireTotal = (data.ContainsKey("currentFireTotal") ? data["currentFireTotal"].AsInt32() : 0);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
