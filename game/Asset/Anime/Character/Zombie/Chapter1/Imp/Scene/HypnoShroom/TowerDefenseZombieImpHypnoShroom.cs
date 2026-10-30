using Godot;

[Tool]
public partial class TowerDefenseZombieImpHypnoShroom : TowerDefenseZombieImpBase
{
	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
			if (TowerDefenseManager.Instance.IsIZMMode())
			{
				instance.hitpointScale *= 14.0 / 27.0;
			}
			if (!TowerDefenseManager.GetMapIsNight())
			{
				base.walkSpeedScale *= 0.5;
			}
			else
			{
				base.walkSpeedScale *= 1.0;
			}
		}
	}

	public override void WalkProcessing(double delta)
	{
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (base.sprite.clip != base.inSwimAnimeClip)
		{
			if ((double)globalPositionForPhysicsFrame.X > groundRight)
			{
				base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
			}
			else
			{
				base.sprite.timeScale = timeScale * base.walkSpeedScale;
			}
		}
		else
		{
			base.sprite.timeScale = timeScale * base.inSwimAnimeClipScale;
		}
		if (die || nearDie || instance.sleep || over || base.sprite.pause || !attackComponent.CanAttack())
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = null;
		float num = 3.4028235E+38f;
		foreach (TowerDefenseCharacter target in attackComponent.GetTargetList())
		{
			if (target != null)
			{
				float num2 = Mathf.Abs(target.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X - globalPositionForPhysicsFrame.X);
				if (num2 < num)
				{
					num = num2;
					towerDefenseCharacter = target;
				}
			}
		}
		if (towerDefenseCharacter == null)
		{
			return;
		}
		if (((ulong)towerDefenseCharacter.instance.unUseBuffFlags & 8uL) != 0L)
		{
			if (attackComponent.CanAttack())
			{
				Attack();
			}
		}
		else
		{
			((ZombieImpHypnoShroomSprite)base.sprite).head.Visible = false;
			towerDefenseCharacter.Hypnoses();
			over = true;
			Die();
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

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		Engine.IsEditorHint();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			DamagePartCreate("Head", ((ZombieImpHypnoShroomSprite)base.sprite).head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
		}
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
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantHypnoShroom");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantHypnoShroom", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
