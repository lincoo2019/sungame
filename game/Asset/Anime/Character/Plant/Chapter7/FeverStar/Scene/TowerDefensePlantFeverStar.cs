using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantFeverStar : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "FeverStar";

	public double damageScale = 1.0;

	public double projectileScale = 1.0;

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
				ApplyProjectileLevel();
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			ApplyProjectileLevel();
			BuffComponent buffComponent = buff;
			if (buffComponent != null && !buffComponent.IsReleased)
			{
				buff.OnBuffAdd += BuffAdd;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			BuffComponent buffComponent = buff;
			if (buffComponent != null && !buffComponent.IsReleased)
			{
				buff.OnBuffAdd -= BuffAdd;
			}
		}
	}

	public void LevelUp()
	{
		if (damageScale < 5.0)
		{
			damageScale += 1.0;
		}
		if (projectileScale < 2.0)
		{
			projectileScale += 0.25;
		}
		ApplyProjectileLevel();
	}

	private void BuffAdd(string key)
	{
		if (key == "JalaHit")
		{
			LevelUp();
		}
	}

	private void ApplyProjectileLevel()
	{
		FireComponent fireComponent = _fireComponent;
		if (fireComponent == null || fireComponent.IsReleased)
		{
			return;
		}
		foreach (FireComponentCheckConfig fireCheck in _fireComponent.fireCheckList)
		{
			if (fireCheck?.projectile is FireComponentProjectileSingle { projectileData: not null, projectileName: var value } fireComponentProjectileSingle && !string.IsNullOrEmpty(value))
			{
				TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig(value);
				TowerDefenseProjectileData projectile = TowerDefenseProjectileRegistry.GetProjectile(fireComponentProjectileSingle.projectileData.projectileName);
				double num = projectileConfig?.baseDamage ?? projectile?.baseDamage ?? fireComponentProjectileSingle.projectileData.baseDamage;
				if (!(num < 0.0))
				{
					TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = (TowerDefenseProjectileCreateData)fireComponentProjectileSingle.projectileData.Duplicate(deep: true);
					towerDefenseProjectileCreateData.baseDamage = num * damageScale;
					towerDefenseProjectileCreateData.scale = Vector2.One * (float)projectileScale;
					fireComponentProjectileSingle.projectileData = towerDefenseProjectileCreateData;
				}
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["damageScale"] = damageScale,
			["projectileScale"] = projectileScale,
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		damageScale = (data.ContainsKey("damageScale") ? data["damageScale"].AsDouble() : 1.0);
		projectileScale = (data.ContainsKey("projectileScale") ? data["projectileScale"].AsDouble() : 1.0);
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "FeverStar");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
		ApplyProjectileLevel();
	}
}
