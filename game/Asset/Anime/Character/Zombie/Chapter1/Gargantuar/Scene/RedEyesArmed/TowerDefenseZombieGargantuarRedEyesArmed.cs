using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieGargantuarRedEyesArmed : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_HEAD_2_REDEYE = "uid://dkgcc1dscx4mc";

	private const string ZOMBIE_GARGANTUAR_DUCKXING = "uid://6dy81rx4gaue";

	private const string ZOMBIE_GARGANTUAR_ZOMBIE = "uid://dtrl03qm2d0u7";

	private int weaponVariant;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (!base.IsProgressRestoreInFlight)
		{
			double num = GD.Randf();
			if ((double)GD.Randf() <= 0.2)
			{
				instance.ArmorAdd("Shield");
			}
			else
			{
				instance.ArmorAdd("Screendoor");
			}
			if (num <= 0.6)
			{
				instance.ArmorAdd("Bucket");
			}
			else if (num <= 0.9)
			{
				instance.ArmorAdd("BlackHelmet");
			}
			else
			{
				instance.ArmorAdd("SpecialHelmet");
			}
			double num2 = GD.Randf();
			if (num2 < 0.3)
			{
				weaponVariant = 1;
			}
			else if (num2 < 0.6)
			{
				weaponVariant = 2;
			}
			ApplyWeaponVariant();
		}
		ConfigureWaterLineVisualLayers("Zombie_duckytube");
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_head_redeye.png", "uid://dkgcc1dscx4mc");
		}
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}

	private void ApplyWeaponVariant()
	{
		if (weaponVariant == 1)
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://6dy81rx4gaue");
		}
		else if (weaponVariant == 2)
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://dtrl03qm2d0u7");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["weaponVariant"] = weaponVariant;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		weaponVariant = data.GetValueOrDefault("weaponVariant", 0).AsInt32();
		ApplyWeaponVariant();
	}
}
