using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSnowPeaCat : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "SnowPea";

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
			if (IsNodeReady())
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
			if (IsNodeReady())
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
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "SnowPea");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 1.5);
	}
}
