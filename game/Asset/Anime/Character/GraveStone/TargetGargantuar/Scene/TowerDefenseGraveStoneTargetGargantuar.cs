using Godot;

[Tool]
public partial class TowerDefenseGraveStoneTargetGargantuar : TowerDefenseGravestone
{
	public bool over;

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuar");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombie zombie = (TowerDefenseZombie)packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight);
		TowerDefenseManager.GetCharacterNode().AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		double riseDuration = GD.RandRange(0.4, 0.6);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.Rise(riseDuration);
			}
		}).CallDeferred();
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuar", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, hypnoses: false, riseDuration, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
