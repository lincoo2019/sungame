using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoSeed : TowerDefensePlant
{
	private static PackedScene _IMITATER_CLOUD;

	private const string HypnoShroomPacketName = "PlantHypnoShroom";

	public bool over;

	public bool isChange;

	[Export(PropertyHint.None, "")]
	public double changeTime = 25.0;

	public double timer;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && inGame && !instance.sleep)
		{
			timer += delta * timeScale;
			if (timer >= changeTime)
			{
				isChange = true;
				Destroy();
			}
		}
	}

	public override void DestroySet()
	{
		base.DestroySet();
		if (!over)
		{
			over = true;
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantHypnoShroom");
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
			towerDefenseCharacter.WakeUp();
			TowerDefenseManager.PublishSpawnedCharacter("PlantHypnoShroom", towerDefenseCharacter, useCreate: false);
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
		changeTime = (data.ContainsKey("changeTime") ? data["changeTime"].AsDouble() : 25.0);
	}
}
