using Godot;

[GlobalClass]
public partial class ShovelEventSkeletonShovelConfig : ShovelEventConfig
{
	private static PackedScene _rivive;

	private static PackedScene RIVIVE => _rivive ?? (_rivive = GD.Load<PackedScene>("uid://dbgw1lmiiyypp"));

	public override void Execute(TowerDefenseCharacter character)
	{
		if ((Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost) || !(character.cost >= 50.0) || !TowerDefenseManager.TryTakeLatestDeathRecord(TowerDefenseEnum.CHARACTER_CAMP.PLANT, requireAngelEligible: false, out var record))
		{
			return;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(RIVIVE, record.GridPosition);
		towerDefenseEffectSpriteOnce.GlobalPosition = record.Position;
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
		TowerDefensePacketConfig packet = record.Packet;
		TowerDefenseCharacter towerDefenseCharacter = packet.Create(record.Position, record.GridPosition);
		towerDefenseCharacter.invisible = record.Invisible;
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.transformPoint))
		{
			towerDefenseCharacter.transformPoint.Scale = (float)record.Scale * Vector2.One;
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			towerDefenseCharacter.instance.hitpointScale = record.HitpointScale;
		}
		towerDefenseCharacter.Hypnoses();
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, character);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packet.saveKey, record.GridPosition.X, record.GridPosition.Y, nextSyncId, record.HitpointScale, record.Scale, hypnoses: true, 0.0, useCreate: true, record.Position.X, record.Position.Y, walkAfterSpawn: true);
			}
		}
		towerDefenseCharacter.CallDeferred("Walk");
	}
}
