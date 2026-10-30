using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCabbagepultInside : TowerDefensePlant
{
	private const double VolleyArcHeightStep = 120.0;

	private FireComponent _fireComponent;

	public int maxFireNum = 5;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Cabbage";

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
				_fireComponent.fireInterval = (float)_fireInterval;
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
				_fireComponent.fireNum = _fireNum;
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
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnPrepareProjectileData += PrepareVolleyProjectileData;
		}
		foreach (Node item in GetTree().GetNodesInGroup("PlantCabbagepultInside"))
		{
			if (item != this && item is TowerDefensePlantCabbagepultInside towerDefensePlantCabbagepultInside && towerDefensePlantCabbagepultInside.config.name == "PlantCabbagepultInside")
			{
				towerDefensePlantCabbagepultInside.fireNum = Mathf.Min(towerDefensePlantCabbagepultInside.fireNum + 1, maxFireNum);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnPrepareProjectileData -= PrepareVolleyProjectileData;
		}
	}

	private void PrepareVolleyProjectileData(int _, TowerDefenseProjectileCreateData projectileData)
	{
		if (projectileData != null)
		{
			FireComponent fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				int num = Math.Max(1, _fireComponent.fireNum);
				double num2 = (double)_fireComponent.currentFireNum - (double)(num - 1) * 0.5;
				projectileData.overrideCatapultHeight = true;
				projectileData.catapultHeight = Math.Max(100.0, projectileData.catapultHeight + num2 * 120.0);
				projectileData.InvalidateConfigCache();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval },
			{ "maxFireNum", maxFireNum }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "Cabbage");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 3.0);
		maxFireNum = (int)data.GetValueOrDefault("maxFireNum", 5);
	}
}
