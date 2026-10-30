using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantCornpultM : TowerDefensePlant
{
	public FireComponent fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			fireComponent.OnConfirmProjectile += ConfirmProjectile;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnConfirmProjectile -= ConfirmProjectile;
		}
	}

	public virtual void ConfirmProjectile(int projectileId, TowerDefenseProjectileCreateData projectileData)
	{
		string text = projectileData.projectileName.ToString();
		if (!(text == "MagnetDisable"))
		{
			if (text == "MagnetDefault")
			{
				base.sprite.SetFliters((Array?)new Array<string> { "Cornpult_butter" }, open: true);
				base.sprite.SetFliters((Array?)new Array<string> { "Cornpult_kernal" }, open: false);
			}
		}
		else
		{
			base.sprite.SetFliters((Array?)new Array<string> { "Cornpult_butter" }, open: false);
			base.sprite.SetFliters((Array?)new Array<string> { "Cornpult_kernal" }, open: true);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
