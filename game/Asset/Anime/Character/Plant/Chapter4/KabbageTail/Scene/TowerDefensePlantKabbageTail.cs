using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantKabbageTail : TowerDefensePlant
{
	public FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

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
			fireComponent.OnFireVolley += FireVolley;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnConfirmProjectile -= ConfirmProjectile;
			this.fireComponent.OnFireVolley -= FireVolley;
		}
	}

	public virtual void ConfirmProjectile(int projectileId, TowerDefenseProjectileCreateData projectileData)
	{
		string text = projectileData.projectileName.ToString();
		base.sprite.SetFliters((Array?)new Array<string> { "anim_cabbage", "corn", "anim_butter" }, open: false);
		switch (text)
		{
		case "Butter":
			base.sprite.SetFliters((Array?)new Array<string> { "anim_butter" }, open: true);
			break;
		case "Cabbage":
			base.sprite.SetFliters((Array?)new Array<string> { "anim_cabbage" }, open: true);
			break;
		case "Kernal":
			base.sprite.SetFliters((Array?)new Array<string> { "corn" }, open: true);
			break;
		}
	}

	public virtual void FireVolley(ulong randomSeed)
	{
		base.sprite.SetFliters((Array?)new Array<string> { "anim_cabbage", "corn", "anim_butter" }, open: false);
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
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
