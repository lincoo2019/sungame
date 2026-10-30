using Godot;

[GlobalClass]
public partial class BattleOption : DialogBoxBase
{
	public CheckBox plantHealthCheckBox;

	public CheckBox zombieHealthCheckBox;

	public CheckBox bossHealthBarCheckBox;

	public CheckBox packetUIFrontCheckBox;

	public CheckBox mapEffectCheckBox;

	public CheckBox backgrounderCheckBox;

	public CheckBox phonkCheckBox;

	public Label phonkIntensityLabel;

	public HSlider phonkIntensitySlider;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base._Ready();
		plantHealthCheckBox = GetNode<CheckBox>("%PlantHealthCheckBox");
		zombieHealthCheckBox = GetNode<CheckBox>("%ZombieHealthCheckBox");
		bossHealthBarCheckBox = GetNode<CheckBox>("%BossHealthBarCheckBox");
		packetUIFrontCheckBox = GetNode<CheckBox>("%PacketUIFrontCheckBox");
		mapEffectCheckBox = GetNode<CheckBox>("%MapEffectCheckBox");
		backgrounderCheckBox = GetNode<CheckBox>("%BackgrounderCheckBox");
		phonkCheckBox = GetNode<CheckBox>("%PhonkCheckBox");
		phonkIntensityLabel = GetNode<Label>("%PhonkIntensityLabel");
		phonkIntensitySlider = GetNode<HSlider>("%PhonkIntensitySlider");
		plantHealthCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("ShowPlantHealth").AsBool();
		zombieHealthCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("ShowZombieHealth").AsBool();
		bossHealthBarCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("ShowBossHealthBar").AsBool();
		packetUIFrontCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("PacketUIFront").AsBool();
		mapEffectCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		backgrounderCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("Backgrounder").AsBool();
		phonkCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("PhonkEnabled").AsBool();
		phonkIntensitySlider.Value = GameSaveManager.Instance.GetConfigValue("PhonkIntensity").AsDouble();
		phonkIntensityLabel.Text = $"果冻弹性强度:{phonkIntensitySlider.Value:F1}";
		PhonkComponent.phonkEnabled = phonkCheckBox.ButtonPressed;
		PhonkComponent.phonkIntensity = (float)phonkIntensitySlider.Value;
		plantHealthCheckBox.Toggled += PlantHealthCheckBoxToggled;
		zombieHealthCheckBox.Toggled += ZombieHealthCheckBoxToggled;
		bossHealthBarCheckBox.Toggled += BossHealthBarCheckBoxToggled;
		packetUIFrontCheckBox.Toggled += PacketUIFrontCheckBoxToggled;
		mapEffectCheckBox.Toggled += MapEffectCheckBoxToggled;
		backgrounderCheckBox.Toggled += BackgrounderCheckBoxToggled;
		phonkCheckBox.Toggled += PhonkCheckBoxToggled;
		phonkIntensitySlider.ValueChanged += PhonkIntensitySliderChanged;
		GetNode<NinePatchButtonBase>("CenterContainer/VBoxContainer/BackButton").OnPressed += BackButtonPressed;
		if (Global.IsMultiplayerMode)
		{
			pasue = false;
			if (GetTree().Paused)
			{
				GetTree().Paused = false;
			}
		}
	}

	public void PlantHealthCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("ShowPlantHealth", toggledOn);
		BattleEventBus.Instance.EmitShowPlantHealth(toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void ZombieHealthCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("ShowZombieHealth", toggledOn);
		BattleEventBus.Instance.EmitShowZombieHealth(toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void BossHealthBarCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("ShowBossHealthBar", toggledOn);
		BattleEventBus.Instance.EmitShowBossHealthBar(toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void PacketUIFrontCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("PacketUIFront", toggledOn);
		BattleEventBus.Instance.EmitPacketUIFront(toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void MapEffectCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("MapEffect", toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void BackgrounderCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("Backgrounder", toggledOn);
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void PhonkCheckBoxToggled(bool toggledOn)
	{
		GameSaveManager.Instance.SetConfigValue("PhonkEnabled", toggledOn);
		PhonkComponent.phonkEnabled = toggledOn;
		if (toggledOn)
		{
			PhonkComponent.InjectAll();
		}
		else
		{
			PhonkComponent.RemoveAll();
		}
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void PhonkIntensitySliderChanged(double value)
	{
		GameSaveManager.Instance.SetConfigValue("PhonkIntensity", value);
		PhonkComponent.phonkIntensity = (float)value;
		phonkIntensityLabel.Text = $"果冻弹性强度:{value:F1}";
		GameSaveManager.Instance.SaveGameConfig();
	}

	public void BackButtonPressed()
	{
		CloseDialog();
	}
}
