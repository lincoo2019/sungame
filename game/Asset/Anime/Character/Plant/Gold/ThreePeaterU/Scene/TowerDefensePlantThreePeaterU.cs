using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantThreePeaterU : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private ExplodeComponent _explodeComponent;

	public bool over;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "FirePeaS";

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
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.BindCheckRayToGridRow(1, 1);
			_fireComponent.BindCheckRayToGridRow(2, -1);
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			if (TowerDefenseManager.Instance.IsGameRunning())
			{
				Explode();
			}
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			Explode();
		}
	}

	public void Explode()
	{
		_explodeComponent.Explode();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "over", over },
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = (bool)data.GetValueOrDefault("over", false);
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "FirePeaS");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
