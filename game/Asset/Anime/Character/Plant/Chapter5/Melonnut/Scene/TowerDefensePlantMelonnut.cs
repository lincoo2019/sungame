using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantMelonnut : TowerDefensePlant
{
	private const string MELON_NUT_SKIN_1_1 = "uid://cx7gtgw52t3oj";

	private const string MELON_NUT_SKIN_1_2 = "uid://cv0bbs3qujxi3";

	private const string MELON_NUT_SKIN_1_3 = "uid://lxqv1h150djq";

	private const string MELON_NUT_SKIN_2_1 = "uid://bhavtiic64dep";

	private const string MELON_NUT_SKIN_2_2 = "uid://cq6mgcgj2rv33";

	private const string MELON_NUT_SKIN_2_3 = "uid://bq5nd3utxer1h";

	public FireComponent fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Melon";

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
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("MelonNut_skin1_1.png", "uid://cx7gtgw52t3oj");
				base.sprite.SetAtlasReplace("MelonNut_skin2_1.png", "uid://bhavtiic64dep");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("MelonNut_skin1_1.png", "uid://cv0bbs3qujxi3");
				base.sprite.SetAtlasReplace("MelonNut_skin2_1.png", "uid://cq6mgcgj2rv33");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("MelonNut_skin1_1.png", "uid://lxqv1h150djq");
				base.sprite.SetAtlasReplace("MelonNut_skin2_1.png", "uid://bq5nd3utxer1h");
			}
			break;
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
		projectileName = data.GetValueOrDefault("projectileName", "Melon").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
