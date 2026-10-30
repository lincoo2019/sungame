using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPumpkinPult : TowerDefensePlant
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
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (!inGame && !editorMapPreviewMode)
		{
			PumpkinPultSprite pumpkinPultSprite = base.sprite as PumpkinPultSprite;
			if (GodotObject.IsInstanceValid(pumpkinPultSprite) && GodotObject.IsInstanceValid(pumpkinPultSprite.back))
			{
				pumpkinPultSprite.back.ZIndex = 0;
			}
		}
		_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		_fireComponent.OnConfirmProjectile += ConfirmProjectile;
		_fireComponent.OnFireOver += FireOver;
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
		switch (projectileData.projectileName.ToString())
		{
		case "PumpkinCannonCob":
			base.sprite.SetFliters(new Array { "PumpkinPult_kernal" }, open: false);
			base.sprite.SetFliters(new Array { "PumpkinPult_pie" }, open: false);
			base.sprite.SetFliters(new Array { "PumpkinPult_pumpkin" }, open: true);
			break;
		case "PumpkinPie":
			base.sprite.SetFliters(new Array { "PumpkinPult_kernal" }, open: false);
			base.sprite.SetFliters(new Array { "PumpkinPult_pie" }, open: true);
			base.sprite.SetFliters(new Array { "PumpkinPult_pumpkin" }, open: false);
			break;
		case "PumpkinKernal":
			base.sprite.SetFliters(new Array { "PumpkinPult_kernal" }, open: true);
			base.sprite.SetFliters(new Array { "PumpkinPult_pie" }, open: false);
			base.sprite.SetFliters(new Array { "PumpkinPult_pumpkin" }, open: false);
			break;
		}
	}

	public void FireOver()
	{
		base.sprite.SetFliters(new Array { "PumpkinPult_pie" }, open: false);
		base.sprite.SetFliters(new Array { "PumpkinPult_kernal" }, open: false);
		base.sprite.SetFliters(new Array { "PumpkinPult_pumpkin" }, open: false);
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
