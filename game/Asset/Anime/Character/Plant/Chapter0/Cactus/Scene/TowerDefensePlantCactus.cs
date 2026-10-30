using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCactus : TowerDefensePlant
{
	private FireComponentExtendCactus fireComponentExtendCactus;

	private FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Spike";

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
			ApplyFireComponentSettings();
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
			ApplyFireComponentSettings();
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
			ApplyFireComponentSettings();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponentExtendCactus = componentManager.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			ApplyFireComponentSettings();
		}
	}

	private void ApplyFireComponentSettings()
	{
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.fireInterval = (float)_fireInterval;
			this.fireComponent.fireNum = _fireNum;
			if (this.fireComponent.fireCheckList.Count > 0 && this.fireComponent.fireCheckList[0].projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
			{
				fireComponentProjectileSingle.projectileName = _projectileName;
			}
			if (this.fireComponent.fireCheckList.Count > 1 && this.fireComponent.fireCheckList[1].projectile is FireComponentProjectileSingle fireComponentProjectileSingle2)
			{
				fireComponentProjectileSingle2.projectileName = _projectileName;
			}
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
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Spike").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
