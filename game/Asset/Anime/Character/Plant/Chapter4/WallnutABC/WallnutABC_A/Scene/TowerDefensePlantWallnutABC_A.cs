using Godot;

[Tool]
public partial class TowerDefensePlantWallnutABC_A : TowerDefensePlant
{
	private static PackedScene _IMITATER_CLOUD;

	public override bool PreserveDeathTransformation => true;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override async void DestroySet()
	{
		base.DestroySet();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantWallnutABC_B");
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
			TowerDefenseManager.PublishSpawnedCharacter("PlantWallnutABC_B", towerDefenseCharacter, useCreate: false);
		}
	}
}
