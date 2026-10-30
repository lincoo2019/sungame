using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMininut : TowerDefensePlant
{
	private const string MINI_NUT_SKIN11 = "uid://ddspigw28gho4";

	private const string MINI_NUT_SKIN12 = "uid://bdsdvjbrep741";

	private const string MINI_NUT_SKIN13 = "uid://bx2nv5g7iu678";

	private CharacterTimerComponent _timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
		_timerComponent.OnTimeout += Timeout;
		if (base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantNutpult");
			if (towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString() != "")
			{
				base.currentCustom = new Array<string> { towerDefensePacketValue["Key"].AsGodotDictionary()["Custom"].AsString() };
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Destroy"))
		{
			_timerComponent.Run("Destroy", 30.0);
		}
	}

	public void Timeout(string timerName)
	{
		Destroy();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
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
				base.sprite.SetAtlasReplace("MiniNut_skin1_1.png", "uid://ddspigw28gho4");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("MiniNut_skin1_1.png", "uid://bdsdvjbrep741");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("MiniNut_skin1_1.png", "uid://bx2nv5g7iu678");
			}
			break;
		}
	}
}
