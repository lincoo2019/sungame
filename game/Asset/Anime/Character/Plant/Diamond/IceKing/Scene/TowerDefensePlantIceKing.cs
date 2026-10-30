using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceKing : TowerDefensePlant
{
	public bool over;

	public bool run;

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && inGame)
		{
			if (CanSleep())
			{
				Sleep();
			}
			else if (IsIzmOneShotGateOpen())
			{
				Activate();
			}
		}
	}

	private void Activate()
	{
		if (!run)
		{
			instance.invincible = true;
			base.sprite.SetAnimation("Idle", loop: false, 0.2);
			run = true;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!run)
		{
			if (!IsIzmOneShotGateOpen())
			{
				base.sprite.timeScale = 0.0;
				return;
			}
			Activate();
		}
		base.sprite.timeScale = timeScale * 1.0;
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			timeScaleInit = timeScaleSave;
		}
	}

	public override void IdleExited()
	{
		base.IdleExited();
	}

	public override void AnimeCompleted(string clip)
	{
		if (!inGame)
		{
			return;
		}
		base.AnimeCompleted(clip);
		if (!(clip == "Idle") || !run || over)
		{
			return;
		}
		over = true;
		base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
		TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
		foreach (TowerDefenseInGamePacketShow seedBank in TowerDefenseManager.Instance.GetSeedBankList())
		{
			if (((seedBank.originalSaveKey != "") ? seedBank.originalSaveKey : seedBank.config.saveKey) == packet.saveKey || seedBank.config.saveKey == packet.saveKey)
			{
				double packetCooldown = seedBank.config.characterConfig.packetCooldown;
				seedBank.coldDownTimer += 50.0;
				if (seedBank.config.overridePacketCooldown == -1.0)
				{
					seedBank.config.overridePacketCooldown = packetCooldown + 50.0;
				}
				else
				{
					seedBank.config.overridePacketCooldown += 50.0;
				}
			}
			else
			{
				seedBank.coldDownTimer = 0.0;
			}
		}
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["run"] = run,
			["over"] = over
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.ContainsKey("run") && data["run"].AsBool();
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
