using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCaltropZ : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private CharacterTimerComponent _timerComponent;

	private readonly HashSet<TowerDefenseZombie> _carryOverlaps = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _carryScratch = new HashSet<TowerDefenseZombie>();

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.0;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _attackComponent != null)
			{
				AttackComponent attackComponent = _attackComponent;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_attackComponent.OnAttack += ComponentAttack;
			_timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= ComponentAttack;
		}
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			if (!_timerComponent.IsRunning("Spawn"))
			{
				_timerComponent.Run("Spawn", 25.0);
			}
			ProcessCarryOverlaps();
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			if (!base.sprite.pause && Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				_timerComponent.Run("Spawn", 25.0);
				return;
			}
			SpawnZombie();
			_timerComponent.Run("Spawn", 25.0);
		}
	}

	public override void SpawnZombie()
	{
		if (characterDisabled || base.sprite.pause || (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombieImpDiggerSpike towerDefenseZombieImpDiggerSpike = TowerDefenseCharacter.CreateCharacter("ZombieImpDiggerSpike", logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseZombieImpDiggerSpike;
		towerDefenseZombieImpDiggerSpike.Rise(2.5);
		if (!instance.hypnoses)
		{
			towerDefenseZombieImpDiggerSpike.Hypnoses();
		}
		towerDefenseZombieImpDiggerSpike.digOver = true;
		towerDefenseZombieImpDiggerSpike.SendStateEvent("ToDrill");
		towerDefenseZombieImpDiggerSpike.instance.ArmorDelete("Pick");
		towerDefenseZombieImpDiggerSpike.isRise = false;
		towerDefenseZombieImpDiggerSpike.instance.collisionFlags = 1;
		towerDefenseZombieImpDiggerSpike.instance.maskFlags = 9;
		Dictionary spawnState = new Dictionary { ["drill_spawn"] = true };
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseZombieImpDiggerSpike);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieImpDiggerSpike", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight, "", spawnState);
			}
		}
		Carry(towerDefenseZombieImpDiggerSpike);
	}

	public void ComponentAttack()
	{
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			_attackComponent.AttackAllFlag((float)attack, 2);
		}
	}

	private void ProcessCarryOverlaps()
	{
		_carryScratch.Clear();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) || !TowerDefenseManager.Instance.currentControl.isGameRunning || !inGame || nearDie || die || !TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectListForCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListForCamp(rect, base.camp);
		for (int i = 0; i < charactersIntersectingRectListForCamp.Count; i++)
		{
			if (charactersIntersectingRectListForCamp[i] is TowerDefenseZombie towerDefenseZombie)
			{
				_carryScratch.Add(towerDefenseZombie);
				if (!_carryOverlaps.Contains(towerDefenseZombie))
				{
					CallDeferred(MethodName.Carry, towerDefenseZombie);
				}
			}
		}
		_carryOverlaps.RemoveWhere((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || !_carryScratch.Contains(zombie));
		foreach (TowerDefenseZombie item in _carryScratch)
		{
			_carryOverlaps.Add(item);
		}
	}

	public void Carry(TowerDefenseZombie character)
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || character == null || character.isRise || character.hasSpikeball || (character.instance.maskFlags & 1) == 0 || character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS || character.camp != base.camp || !character.targetRegistrationComponent.canCarry)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseItem towerDefenseItem = TowerDefenseCharacter.CreateCharacter("ItemSpikeball", logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseItem;
		if (!instance.hypnoses)
		{
			towerDefenseItem.Hypnoses();
		}
		towerDefenseItem.targetZombie = character;
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseItem);
				Dictionary spawnState = new Dictionary { ["carrierSyncId"] = character.syncId };
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ItemSpikeball", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight, "", spawnState);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["attack"] = attack,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attack = (data.ContainsKey("attack") ? data["attack"].AsDouble() : 20.0);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.0);
	}
}
