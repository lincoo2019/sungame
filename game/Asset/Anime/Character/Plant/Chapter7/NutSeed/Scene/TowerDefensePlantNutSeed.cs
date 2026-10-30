using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantNutSeed : TowerDefensePlant
{
	private static PackedScene _IMITATER_CLOUD;

	public bool over;

	public bool isChange;

	[Export(PropertyHint.None, "")]
	public double changeTime = 50.0;

	public double timer;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantNutFlower");
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
		if (!Engine.IsEditorHint() && !base.sprite.pause)
		{
			if (timer < changeTime)
			{
				timer += delta * timeScale;
				return;
			}
			isChange = true;
			Destroy();
		}
	}

	public override void DestroySet()
	{
		base.DestroySet();
		if (!over)
		{
			over = true;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
			else
			{
				SunCreate(logicalGlobalPosition, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
			if (isChange)
			{
				Change();
			}
		}
	}

	public async void Change()
	{
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantNutFlower");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos) : packetConfig.Plant(base.gridPos));
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter("PlantNutFlower", towerDefenseCharacter, useCreate: false);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["over"] = over,
			["isChange"] = isChange,
			["timer"] = timer,
			["changeTime"] = changeTime
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
		isChange = data.ContainsKey("isChange") && data["isChange"].AsBool();
		timer = (data.ContainsKey("timer") ? data["timer"].AsDouble() : 0.0);
		changeTime = (data.ContainsKey("changeTime") ? data["changeTime"].AsDouble() : 50.0);
	}
}
