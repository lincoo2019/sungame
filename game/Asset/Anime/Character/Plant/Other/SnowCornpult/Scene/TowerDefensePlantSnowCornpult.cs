using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSnowCornpult : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 2.0;

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
					_fireComponent.fireInterval = (float)_fireInterval;
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
					_fireComponent.fireNum = _fireNum;
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
		if (base.sprite != null)
		{
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
				base.sprite.SetFliters(new Array { "skin6_1" }, open: true);
			}
			else
			{
				base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
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
		string text = projectileData.projectileName.ToString();
		if (base.currentCustom.Contains("Custom0"))
		{
			if (!(text == "SnowPea"))
			{
				if (text == "Butter")
				{
					base.sprite.SetFliters(new Array { "skin6_1" }, open: false);
					base.sprite.SetFliters(new Array { "skin6_2" }, open: true);
					projectileData.skinName = "Blueberry";
				}
			}
			else
			{
				base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
				base.sprite.SetFliters(new Array { "skin6_1" }, open: true);
				projectileData.skinName = "Blueberry";
			}
		}
		else if (!(text == "SnowPea"))
		{
			if (text == "Butter")
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
		if (base.currentCustom.Contains("Custom0"))
		{
			base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
			base.sprite.SetFliters(new Array { "skin6_1" }, open: true);
		}
		else
		{
			base.sprite.SetFliters(new Array { "Cornpult_butter" }, open: false);
			base.sprite.SetFliters(new Array { "Cornpult_kernal" }, open: true);
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
			base.sprite.SetFliters(new Array { "skin6_1" }, open: true);
		}
		else
		{
			base.sprite.SetFliters(new Array { "skin6_2" }, open: false);
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
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
