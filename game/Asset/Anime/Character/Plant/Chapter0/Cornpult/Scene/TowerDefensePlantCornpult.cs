using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCornpult : TowerDefensePlant
{
	private FireComponent _fireComponent;

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
			_fireComponent.OnFireOver += FireOver;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnConfirmProjectile -= ConfirmProjectile;
			_fireComponent.OnFireOver -= FireOver;
		}
	}

	public void ConfirmProjectile(int projectileId, TowerDefenseProjectileCreateData projectileData)
	{
		string text = projectileData.projectileName.ToString();
		if (!(text == "Butter"))
		{
			if (text == "ButterCatapult")
			{
				base.sprite.SetFliters(new Array { "Cornpult_butter" }, open: true);
				base.sprite.SetFliters(new Array { "Cornpult_kernal" }, open: false);
			}
		}
		else
		{
			base.sprite.SetFliters(new Array { "Cornpult_butter" }, open: false);
			base.sprite.SetFliters(new Array { "Cornpult_kernal" }, open: true);
		}
	}

	public void FireOver()
	{
		base.sprite.SetFliters(new Array { "Cornpult_butter" }, open: false);
		base.sprite.SetFliters(new Array { "Cornpult_kernal" }, open: true);
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
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
