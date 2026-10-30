using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSeaStar : TowerDefensePlant
{
	public FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	private string _projectileName = "SnowStar";

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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			BattleEventBus.Instance.OnColdEffectEmit += ColdEffectEmit;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnColdEffectEmit -= ColdEffectEmit;
		}
	}

	public virtual void ColdEffectEmit()
	{
		if (fireNum < 5)
		{
			fireNum++;
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
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "SnowStar").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
