using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingWallnutZ : TowerDefensePlantBowlingBase
{
	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		bowlingComponent.OnBowling += Bowling;
		if (base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantWallnutZ");
			if (towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString() != "")
			{
				base.currentCustom = new Array<string> { towerDefensePacketValue["Key"].AsGodotDictionary()["Custom"].AsString() };
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
		}
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuar");
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = packetConfig.Create(logicalGlobalPosition, character.gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		if (!instance.hypnoses)
		{
			zombie.Hypnoses();
		}
		zombie.SetMainStateMachineDispatchEnabled(enabled: false);
		Tween tween = zombie.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(zombie.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
		tween.Finished += delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				((TowerDefenseZombie)zombie).Walk();
				zombie.SetMainStateMachineDispatchEnabled(enabled: true);
			}
		};
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuar", character.gridPos.X, character.gridPos.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true);
			}
		}
		Destroy();
	}
}
