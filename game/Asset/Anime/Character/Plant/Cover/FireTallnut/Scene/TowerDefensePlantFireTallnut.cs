using Godot;

[Tool]
public partial class TowerDefensePlantFireTallnut : TowerDefensePlant
{
	private const string FIRE_TALLNUT_SKIN11 = "uid://daqku0t4djmni";

	private const string FIRE_TALLNUT_SKIN12 = "uid://ckcn353sc7ri8";

	private const string FIRE_TALLNUT_SKIN13 = "uid://beejwpcgngjgm";

	private const string FIRE_TALLNUT_SKIN21 = "uid://cum4455awia7e";

	private const string FIRE_TALLNUT_SKIN22 = "uid://dodeke0xu60l4";

	private const string FIRE_TALLNUT_SKIN23 = "uid://bbjp2p5tb73oi";

	private PointLight2D _light;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("FireTallnut_skin1_1.png", "uid://daqku0t4djmni");
				base.sprite.SetAtlasReplace("FireTallnut_skin2_1.png", "uid://cum4455awia7e");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("FireTallnut_skin1_1.png", "uid://ckcn353sc7ri8");
				base.sprite.SetAtlasReplace("FireTallnut_skin2_1.png", "uid://dodeke0xu60l4");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("FireTallnut_skin1_1.png", "uid://beejwpcgngjgm");
				base.sprite.SetAtlasReplace("FireTallnut_skin2_1.png", "uid://bbjp2p5tb73oi");
			}
			break;
		}
	}
}
