using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantTwinKabbage : TowerDefensePlant
{
	private FireComponent _fireComponent;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.OnConfirmProjectile += ConfirmProjectile;
			_fireComponent.OnFireVolley += FireVolley;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnConfirmProjectile -= ConfirmProjectile;
			_fireComponent.OnFireVolley -= FireVolley;
		}
	}

	public void ConfirmProjectile(int projectileId, TowerDefenseProjectileCreateData projectileData)
	{
		string text = projectileData.projectileName.ToString();
		base.sprite.SetFliters(new Array { "anim_cabbage", "corn", "anim_butter" }, open: false);
		switch (text)
		{
		case "Butter":
			base.sprite.SetFliters(new Array { "anim_butter" }, open: true);
			break;
		case "CabbageTrack":
			base.sprite.SetFliters(new Array { "anim_cabbage" }, open: true);
			break;
		case "KernalTrack":
			base.sprite.SetFliters(new Array { "corn" }, open: true);
			break;
		}
	}

	public void FireVolley(ulong randomSeed)
	{
		base.sprite.SetFliters(new Array { "anim_cabbage", "corn", "anim_butter" }, open: false);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 2);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
