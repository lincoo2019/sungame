using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantStarRobot : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private ExplodeComponent _explodeComponent;

	private const double FireIntervalMin = 0.5;

	private const double FireIntervalSpeedUp = 0.5;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "StarRobot";

	private bool _destroySetOver;

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
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>("character.explode");
		}
	}

	public override void DestroySet()
	{
		if (_destroySetOver)
		{
			return;
		}
		_destroySetOver = true;
		if (!isShovel)
		{
			if (_explodeComponent != null && !_explodeComponent.IsReleased)
			{
				_explodeComponent.Explode();
			}
			SpeedUpOtherStarRobots();
		}
		base.DestroySet();
	}

	private void SpeedUpOtherStarRobots()
	{
		if (base.config == null)
		{
			return;
		}
		foreach (Node item in GetTree().GetNodesInGroup(base.config.name))
		{
			if (item is TowerDefensePlantStarRobot towerDefensePlantStarRobot && towerDefensePlantStarRobot != this && GodotObject.IsInstanceValid(towerDefensePlantStarRobot))
			{
				towerDefensePlantStarRobot.fireInterval = Math.Max(0.5, towerDefensePlantStarRobot.fireInterval - 0.5);
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
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "StarRobot");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
