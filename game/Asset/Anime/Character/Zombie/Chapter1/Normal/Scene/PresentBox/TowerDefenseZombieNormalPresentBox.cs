using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalPresentBox : TowerDefenseZombie
{
	private static PackedScene _IMITATER_CLOUD;

	private ZombieNormalWallnutSprite _zombieSprite;

	[Export(PropertyHint.None, "")]
	public string packetBank;

	public bool halfHp;

	public bool isAttack;

	public bool over;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zombieSprite = base.sprite as ZombieNormalWallnutSprite;
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				DamagePartCreate("Head", _zombieSprite.head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			}
		}
		else
		{
			halfHp = true;
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
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
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			CallDeferred("Destroy", true);
			return;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketBankData towerDefensePacketBankData = ((!(packetBank == "ZombiePresentBox")) ? TowerDefenseManager.GetPacketBankData(packetBank) : TowerDefenseManager.GetPacketBankData(((double)GD.Randf() > 0.01) ? packetBank : "EliteZombie"));
		if (GodotObject.IsInstanceValid(towerDefensePacketBankData))
		{
			List<string> list = new List<string>(from v in towerDefensePacketBankData.GetZombieList()
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
			while (instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0 && list.Count > 1)
			{
				list.Remove(text);
				text = list[GD.RandRange(0, list.Count - 1)];
				packetConfig = TowerDefenseManager.GetPacketConfig(text);
			}
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				TowerDefenseCharacter zombie = packetConfig.Plant(base.gridPos);
				zombie.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, TowerDefenseManager.GetMapCellPlantPos(base.gridPos).Y));
				double hitpointScale = instance.hitpointScale;
				Vector2 scale = transformPoint.Scale;
				CallDeferred(MethodName.ApplyZombieScale, zombie, hitpointScale, scale);
				if (instance.hypnoses)
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
				if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
				{
					TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
					if (GodotObject.IsInstanceValid(currentControl))
					{
						int nextSyncId = currentControl.GetNextSyncId();
						currentControl.RegisterSyncCharacter(nextSyncId, zombie);
						MultiPlayerManager.Instance.SendSpawnCharacterAt(text, base.gridPos.X, base.gridPos.Y, nextSyncId, hitpointScale, scale.X, instance.hypnoses);
					}
				}
			}
		}
		CallDeferred("Destroy", true);
	}

	private void ApplyZombieScale(TowerDefenseCharacter zombie, double hitpointScale, Vector2 scale)
	{
		if (GodotObject.IsInstanceValid(zombie))
		{
			if (GodotObject.IsInstanceValid(zombie.instance))
			{
				zombie.instance.hitpointScale = hitpointScale;
			}
			if (GodotObject.IsInstanceValid(zombie.transformPoint))
			{
				zombie.transformPoint.Scale = scale;
			}
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
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
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
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
