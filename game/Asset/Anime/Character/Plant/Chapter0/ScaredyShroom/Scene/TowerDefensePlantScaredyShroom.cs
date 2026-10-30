using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantScaredyShroom : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private ScaredComponent _scaredComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Puff";

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
			if (!IsNodeReady() || _fireComponent == null)
			{
				return;
			}
			FireComponent fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0 && _fireComponent.fireCheckList[0].projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
			{
				fireComponentProjectileSingle.projectileName = value;
				TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig(value);
				if (GodotObject.IsInstanceValid(projectileConfig) && GodotObject.IsInstanceValid(fireComponentProjectileSingle.projectileData))
				{
					fireComponentProjectileSingle.projectileData.fireMethodFlags = projectileConfig.fireMethodFlags;
					fireComponentProjectileSingle.projectileData.InvalidateConfigCache();
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
			_scaredComponent = componentManager.GetRuntime<ScaredComponent>();
			if (_scaredComponent == null)
			{
				GD.PushError("ScaredyShroom is missing its Scared resource runtime.");
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Puff");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
