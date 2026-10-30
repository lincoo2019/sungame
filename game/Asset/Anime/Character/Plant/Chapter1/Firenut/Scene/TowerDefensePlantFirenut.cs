using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantFirenut : TowerDefensePlant
{
	private PointLight2D light;

	private AttackComponent attackComponent;

	private StateHandle _attackState;

	private bool _roleStateSignalsConnected;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			light = GetNode<PointLight2D>("%Light");
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackState = base.StateMachine?.GetStateById("plant.firenut.attack");
			ConnectRoleStateSignals();
		}
	}

	private void ConnectRoleStateSignals()
	{
		if (!_roleStateSignalsConnected)
		{
			StateHandle attackState = _attackState;
			if (attackState != null && attackState.IsValid)
			{
				_attackState.Entered += AttackEntered;
				_attackState.Exited += AttackExited;
				_attackState.PhysicsProcessing += AttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Damage2" && base.currentCustom.Contains("Custom0"))
		{
			base.sprite.SetFliter("skin2_4", open: true);
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (isUnlimitedFire && attackComponent.CanAttack())
		{
			SendStateEvent("ToAttack");
		}
	}

	public void AttackEntered()
	{
		AudioManager.Instance.AudioPlay("BowlingImpact");
		attackComponent.AttackExecute(0.0);
		base.sprite.SetAnimation("Roll", loop: false, 0.2);
	}

	public void AttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void AttackExited()
	{
	}

	public override void DestroySet()
	{
		base.DestroySet();
		if (over)
		{
			return;
		}
		over = true;
		if (!isUnlimitedFire || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantFirenutBowling");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos) : packetConfig.Create(logicalGlobalPosition, base.gridPos));
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			Dictionary dictionary = new Dictionary { ["damagePoint"] = "Damage2" };
			if (towerDefenseCharacter is INetworkSpawnStateReceiver networkSpawnStateReceiver)
			{
				networkSpawnStateReceiver.ImportNetworkSpawnState(dictionary);
			}
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter("PlantFirenutBowling", towerDefenseCharacter, useCreate: true, 0.0, walkAfterSpawn: false, "", dictionary);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Roll")
		{
			Idle();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
