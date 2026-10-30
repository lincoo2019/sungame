using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantThreeCornpult : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	[Export(PropertyHint.None, "")]
	public double FireInterval
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
	public int FireNum
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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.BindCheckAreaShapeToGridRow(0, 1);
			_fireComponent.BindCheckAreaShapeToGridRow(2, -1);
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
		switch (projectileId)
		{
		case 0:
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
			break;
		case 1:
			if (!(text == "Butter"))
			{
				if (text == "ButterCatapult")
				{
					base.sprite.SetFliters(new Array { "Cornpult_butter 澶嶅埗" }, open: true);
					base.sprite.SetFliters(new Array { "Cornpult_kernal 澶嶅埗" }, open: false);
				}
			}
			else
			{
				base.sprite.SetFliters(new Array { "Cornpult_butter 澶嶅埗" }, open: false);
				base.sprite.SetFliters(new Array { "Cornpult_kernal 澶嶅埗" }, open: true);
			}
			break;
		case 2:
			if (!(text == "Butter"))
			{
				if (text == "ButterCatapult")
				{
					base.sprite.SetFliters(new Array { "Cornpult_butter   澶嶅埗 2" }, open: true);
					base.sprite.SetFliters(new Array { "Cornpult_kernal   澶嶅埗 2" }, open: false);
				}
			}
			else
			{
				base.sprite.SetFliters(new Array { "Cornpult_butter   澶嶅埗 2" }, open: false);
				base.sprite.SetFliters(new Array { "Cornpult_kernal   澶嶅埗 2" }, open: true);
			}
			break;
		}
	}

	public void FireOver()
	{
		base.sprite.SetFliters(new Array { "Cornpult_butter" }, open: false);
		base.sprite.SetFliters(new Array { "Cornpult_butter 澶嶅埗" }, open: false);
		base.sprite.SetFliters(new Array { "Cornpult_butter  澶嶅埗 2" }, open: false);
		base.sprite.SetFliters(new Array { "Cornpult_kernal" }, open: true);
		base.sprite.SetFliters(new Array { "Cornpult_kernal 澶嶅埗" }, open: true);
		base.sprite.SetFliters(new Array { "Cornpult_kernal  澶嶅埗 2" }, open: true);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", FireNum },
			{ "fireInterval", FireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		FireNum = data.GetValueOrDefault("fireNum", Variant.From<int>(1)).AsInt32();
		FireInterval = data.GetValueOrDefault("fireInterval", Variant.From<double>(3.0)).AsDouble();
	}
}
