using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantImitaterW : TowerDefensePlant
{
	private static PackedScene _IMITATER_CLOUD;

	private ExplodeComponent _explodeComponent;

	[Export(PropertyHint.None, "")]
	public string packetBank = "ImitaterWallnut";

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Destroy(freeInstance: false);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseManager.Instance.CharacterUnregister(this);
			RemoveFromGroup("Character");
			QueueFree();
			return;
		}
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(packetBank);
		if (GodotObject.IsInstanceValid(packetBankData))
		{
			Array array = new Array();
			array.AddRange(packetBankData.GetCategory("White"));
			array.AddRange(packetBankData.GetCategory("Original"));
			array.AddRange(packetBankData.GetCategory("Gold"));
			array.AddRange(packetBankData.GetCategory("Colour"));
			string text = array.PickRandom().AsString();
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
			while (packetConfig.characterConfig is TowerDefensePlantConfig && !cell.CanPacketPlant(packetConfig) && array.Count > 1)
			{
				array.Remove(text);
				text = array.PickRandom().AsString();
				packetConfig = TowerDefenseManager.GetPacketConfig(text);
			}
			if (packetConfig.characterConfig is TowerDefensePlantConfig)
			{
				TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.CallDeferred("WakeUp");
					if (instance.hypnoses)
					{
						towerDefenseCharacter.Hypnoses();
					}
					if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
					{
						TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
						if (GodotObject.IsInstanceValid(currentControl))
						{
							int nextSyncId = currentControl.GetNextSyncId();
							currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
							MultiPlayerManager.Instance.SendSpawnCharacterAt(packetConfig.saveKey, base.gridPos.X, base.gridPos.Y, nextSyncId);
						}
					}
				}
			}
			if (packetConfig.characterConfig is TowerDefenseZombieConfig)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = packetConfig.Create(logicalGlobalPosition, base.gridPos);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
				{
					TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseCharacter2, forceReadableName: false, InternalMode.Disabled);
					towerDefenseCharacter2.instance.wakeUp = true;
					if (instance.hypnoses)
					{
						towerDefenseCharacter2.Hypnoses();
					}
					towerDefenseCharacter2.CallDeferred("Walk");
					towerDefenseCharacter2.SetSpriteGroupShaderParameter("imitater", true);
					if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
					{
						TowerDefenseControlNew currentControl2 = TowerDefenseManager.Instance.currentControl;
						if (GodotObject.IsInstanceValid(currentControl2))
						{
							int nextSyncId2 = currentControl2.GetNextSyncId();
							currentControl2.RegisterSyncCharacter(nextSyncId2, towerDefenseCharacter2);
							MultiPlayerManager.Instance.SendSpawnCharacterAt(packetConfig.saveKey, base.gridPos.X, base.gridPos.Y, nextSyncId2, 1.0, 1.0, instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true);
						}
					}
				}
			}
		}
		TowerDefenseManager.Instance.CharacterUnregister(this);
		RemoveFromGroup("Character");
		QueueFree();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["packetBank"] = packetBank };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		packetBank = (data.ContainsKey("packetBank") ? data["packetBank"].AsString() : "ImitaterWallnut");
	}
}
