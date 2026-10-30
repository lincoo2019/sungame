using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class TowerDefenseZombieImpPresentBox : TowerDefenseZombieImpBase
{
	private static PackedScene _IMITATER_CLOUD;

	[Export(PropertyHint.None, "")]
	public string packetBank;

	public bool over;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
			if (TowerDefenseManager.Instance.IsIZMMode())
			{
				instance.hitpointScale = 0.35;
			}
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		CreateRandom();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		CreateRandom();
	}

	public async void CreateRandom()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			CallDeferred("Destroy", true);
			return;
		}
		bool inheritedHypnoses = instance.hypnoses;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = new Vector2(logicalGlobalPosition.X, TowerDefenseManager.GetMapCellPlantPos(base.gridPos).Y);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(packetBank);
		if (GodotObject.IsInstanceValid(packetBankData))
		{
			List<string> list = new List<string>(from v in packetBankData.GetZombieList()
				select v.AsString());
			if (!TowerDefenseManager.Instance.CheckMapGridPosIn(base.gridPos))
			{
				list.RemoveAll((string name) => name == "ZombieBungiBin");
			}
			if (list.Count == 0)
			{
				CallDeferred("Destroy", true);
				return;
			}
			string text = list[GD.RandRange(0, list.Count - 1)];
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
			while (inheritedHypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0 && list.Count > 1)
			{
				list.Remove(text);
				text = list[GD.RandRange(0, list.Count - 1)];
				packetConfig = TowerDefenseManager.GetPacketConfig(text);
			}
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				TowerDefenseCharacter zombie = packetConfig.Plant(base.gridPos);
				if (GodotObject.IsInstanceValid(zombie))
				{
					double num = instance.hitpointScale / 2.0;
					Vector2 vector = transformPoint.Scale / 1.5f;
					zombie.SetLogicalGlobalPosition(logicalGlobalPosition);
					zombie.CallDeferred("SetHitpointAndScale", num, vector);
					if (inheritedHypnoses)
					{
						zombie.Hypnoses(-1.0, canFliter: false);
					}
					if (GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance))
					{
						Callable.From(delegate
						{
							if (GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance))
							{
								TowerDefenseBattleFeatureWave.Instance.currentHpPointTotal += zombie.GetTotalHitPoint() / 3.0;
								TowerDefenseBattleFeatureWave.Instance.currentHpPoint += zombie.GetTotalHitPoint() / 3.0;
							}
						}).CallDeferred();
					}
					if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
					{
						TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
						if (GodotObject.IsInstanceValid(currentControl))
						{
							int nextSyncId = currentControl.GetNextSyncId();
							currentControl.RegisterSyncCharacter(nextSyncId, zombie);
							MultiPlayerManager.Instance.SendSpawnCharacterAt(text, base.gridPos.X, base.gridPos.Y, nextSyncId, num, vector.X, inheritedHypnoses);
						}
					}
				}
			}
		}
		CallDeferred("Destroy", true);
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPresentBox");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.CallDeferred("WakeUp");
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantPresentBox", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
