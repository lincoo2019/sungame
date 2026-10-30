using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCabbageCobX : TowerDefensePlant
{
	private const double VolleyArcHeightStep = 120.0;

	private FireComponent _fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 3;

	private string _projectileName = "CabbageCobX";

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
			if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
			{
				FireComponentProjectileResource projectile = _fireComponent.fireCheckList[0].projectile;
				if (projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
				{
					fireComponentProjectileSingle.projectileName = value;
				}
				else if (projectile is FireComponentProjectileWeight fireComponentProjectileWeight)
				{
					fireComponentProjectileWeight.projectileSimilarName = value;
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
			FireComponent fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				_fireComponent.OnPrepareProjectileData += PrepareVolleyProjectileData;
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
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		int num = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 3);
		string text = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "CabbageCobX");
		bool flag = num == 1 && text == "WinterMelon";
		fireNum = (flag ? 3 : num);
		projectileName = (flag ? "CabbageCobX" : text);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
