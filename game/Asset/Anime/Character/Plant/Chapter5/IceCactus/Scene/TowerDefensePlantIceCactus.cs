using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceCactus : TowerDefensePlant
{
	public FireComponentExtendCactus fireComponentExtendCactus;

	public FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "IceSpike";

	public bool up;

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
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 1)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[1].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponentExtendCactus = componentManager.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			fireComponent.BindCheckRayToGridRow(0, 1);
			fireComponent.BindCheckRayToGridRow(2, -1);
			fireComponentExtendCactus.OnUpOver += UpOver;
			fireComponentExtendCactus.OnDownOver += DownOver;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponentExtendCactus fireComponentExtendCactus = this.fireComponentExtendCactus;
		if (fireComponentExtendCactus != null && !fireComponentExtendCactus.IsReleased)
		{
			this.fireComponentExtendCactus.OnUpOver -= UpOver;
			this.fireComponentExtendCactus.OnDownOver -= DownOver;
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (!fireComponentExtendCactus.IsUp())
		{
			base.sprite.SetAnimation("Idle", loop: true, 0.1);
		}
		else
		{
			base.sprite.SetAnimation("UpIdle", loop: true, 0.1);
		}
	}

	public virtual void DownOver()
	{
		fireComponent.fireNum = 1;
	}

	public virtual void UpOver()
	{
		fireComponent.fireNum = 2;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "up", up },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "IceSpike").AsString();
		up = data.GetValueOrDefault("up", false).AsBool();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
