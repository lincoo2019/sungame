using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantJalaNut : TowerDefensePlant
{
	private static readonly string JALA_NUT_SKIN_1_1 = "uid://ddk2bg2wig1ta";

	private static readonly string JALA_NUT_SKIN_1_2 = "uid://ugbibiellhqa";

	private static readonly string JALA_NUT_SKIN_1_3 = "uid://d222h4r3nut1w";

	private static readonly string JALA_NUT_SKIN_2_1 = "uid://by3vaedjvn7h8";

	private static readonly string JALA_NUT_SKIN_2_2 = "uid://bt6m0vl7yp261";

	private static readonly string JALA_NUT_SKIN_2_3 = "uid://dsauqiyf6oauj";

	private static readonly string JALA_NUT_SKIN_5_1 = "uid://coq6s5e0e1aem";

	private static readonly string JALA_NUT_SKIN_5_2 = "uid://bw4bolg1djjyl";

	public bool over;

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("JalaNut_skin1_1.png", JALA_NUT_SKIN_1_1);
				base.sprite.SetAtlasReplace("JalaNut_skin2_1.png", JALA_NUT_SKIN_2_1);
				base.sprite.SetAtlasReplace("JalaNut_skin5_1.png", JALA_NUT_SKIN_5_1);
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("JalaNut_skin1_1.png", JALA_NUT_SKIN_1_2);
				base.sprite.SetAtlasReplace("JalaNut_skin2_1.png", JALA_NUT_SKIN_2_2);
				base.sprite.SetAtlasReplace("JalaNut_skin5_1.png", JALA_NUT_SKIN_5_2);
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("JalaNut_skin1_1.png", JALA_NUT_SKIN_1_3);
				base.sprite.SetAtlasReplace("JalaNut_skin2_1.png", JALA_NUT_SKIN_2_3);
				base.sprite.SetFliter("skin6_1", open: true);
			}
			break;
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			Explode();
		}
	}

	public async void Explode()
	{
		TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 1800.0);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
