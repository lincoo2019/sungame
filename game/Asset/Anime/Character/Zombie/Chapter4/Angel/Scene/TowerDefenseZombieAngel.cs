using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieAngel : TowerDefenseZombie
{
	private StateHandle _shootingStateHandle;

	private StateHandle _reviveStateHandle;

	private bool _roleStateSignalsConnected;

	private const int MaxWalkTransitionRetries = 8;

	private int _walkTransitionRetries;

	private SceneTree _walkRetryTree;

	private Action _walkRetryHandler;

	private bool _walkReadySignalConnected;

	private static PackedScene _RIVIVE;

	public double speed = 10.0;

	public bool isRevive;

	public bool isReviveOver;

	public bool isBlow;

	public double spawnTimer = 15.0;

	public bool invincible;

	public double invincibleTimer = 8.0;

	private static PackedScene RIVIVE => _RIVIVE ?? (_RIVIVE = GD.Load<PackedScene>("uid://dbgw1lmiiyypp"));

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_shootingStateHandle = base.StateMachine?.GetStateById("zombie.angel.shooting");
		_reviveStateHandle = base.StateMachine?.GetStateById("zombie.angel.revive");
		StateHandle shootingStateHandle = _shootingStateHandle;
		if (shootingStateHandle != null && shootingStateHandle.IsValid)
		{
			StateHandle reviveStateHandle = _reviveStateHandle;
			if (reviveStateHandle != null && reviveStateHandle.IsValid)
			{
				_shootingStateHandle.Entered += ShootingAttackEntered;
				_shootingStateHandle.Exited += ShootingAttackExited;
				_shootingStateHandle.PhysicsProcessing += ShootingAttackProcessing;
				_reviveStateHandle.Entered += ReviveAttackEntered;
				_reviveStateHandle.Exited += ReviveAttackExited;
				_reviveStateHandle.PhysicsProcessing += ReviveAttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_shootingStateHandle != null)
			{
				_shootingStateHandle.Entered -= ShootingAttackEntered;
				_shootingStateHandle.Exited -= ShootingAttackExited;
				_shootingStateHandle.PhysicsProcessing -= ShootingAttackProcessing;
			}
			_shootingStateHandle = null;
			if (_reviveStateHandle != null)
			{
				_reviveStateHandle.Entered -= ReviveAttackEntered;
				_reviveStateHandle.Exited -= ReviveAttackExited;
				_reviveStateHandle.PhysicsProcessing -= ReviveAttackProcessing;
			}
			_reviveStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelWalkTransitionRetry();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && invincible)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (invincibleTimer > 0.0 && GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X > TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(4, 0)).X)
			{
				invincibleTimer -= delta;
				return;
			}
			base.sprite.SetFliters(new Godot.Collections.Array { "wing2_1", "wing2_2" }, open: false);
			instance.unUseBuffFlags = 6;
			instance.invincible = false;
			invincible = false;
		}
	}

	public void ShootingAttackEntered()
	{
		base.sprite.SetAnimation("Shooting", loop: false, 0.2);
	}

	public void ShootingAttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void ShootingAttackExited()
	{
	}

	public void ReviveAttackEntered()
	{
		base.sprite.SetAnimation("Revive", loop: false, 0.2);
		base.sprite.SetFliters(new Godot.Collections.Array { "wing2_1", "wing2_2" }, open: true);
		instance.unUseBuffFlags = -1;
		instance.invincible = true;
		invincible = true;
		invincibleTimer = 8.0;
	}

	public void ReviveAttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void ReviveAttackExited()
	{
	}

	public override void Walk()
	{
		if (!IsNodeReady())
		{
			ScheduleWalkTransitionRetry();
			return;
		}
		ActivateGameplayProcessing();
		StateHandle currentStateHandle = base.CurrentStateHandle;
		if (currentStateHandle == null || !currentStateHandle.IsValid)
		{
			ScheduleWalkTransitionRetry();
		}
		else if (isRevive && !isReviveOver)
		{
			isReviveOver = SendStateEvent("ToRevive") && base.CurrentStateHandle?.StableId == "zombie.angel.revive";
			if (!isReviveOver)
			{
				ScheduleWalkTransitionRetry();
				return;
			}
			_walkTransitionRetries = 0;
			CancelWalkTransitionRetry();
		}
		else
		{
			base.Walk();
			if (!die && base.CurrentStateHandle?.StableId != "zombie.walk")
			{
				ScheduleWalkTransitionRetry();
				return;
			}
			_walkTransitionRetries = 0;
			CancelWalkTransitionRetry();
		}
	}

	private void ScheduleWalkTransitionRetry()
	{
		if (!IsNodeReady())
		{
			if (!_walkReadySignalConnected)
			{
				base.Ready += RetryWalkTransitionAfterReady;
				_walkReadySignalConnected = true;
			}
		}
		else if (_walkRetryHandler == null && _walkTransitionRetries < 8 && IsInsideTree())
		{
			_walkRetryTree = GetTree();
			if (GodotObject.IsInstanceValid(_walkRetryTree))
			{
				_walkRetryHandler = RetryWalkTransitionOnNextFrame;
				_walkRetryTree.ProcessFrame += _walkRetryHandler;
			}
		}
	}

	private void RetryWalkTransitionAfterReady()
	{
		DisconnectWalkReadySignal();
		if (!die && !isDestroy)
		{
			Callable.From(Walk).CallDeferred();
		}
	}

	private void RetryWalkTransitionOnNextFrame()
	{
		CancelWalkTransitionRetry();
		if (IsInsideTree() && !die && !isDestroy)
		{
			_walkTransitionRetries++;
			Walk();
		}
	}

	private void CancelWalkTransitionRetry()
	{
		DisconnectWalkReadySignal();
		if (GodotObject.IsInstanceValid(_walkRetryTree) && _walkRetryHandler != null)
		{
			_walkRetryTree.ProcessFrame -= _walkRetryHandler;
		}
		_walkRetryTree = null;
		_walkRetryHandler = null;
	}

	private void DisconnectWalkReadySignal()
	{
		if (_walkReadySignalConnected)
		{
			base.Ready -= RetryWalkTransitionAfterReady;
		}
		_walkReadySignalConnected = false;
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		base.sprite.timeScale = timeScale;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!base.sprite.pause)
		{
			globalPositionForPhysicsFrame.X -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		if (base.IsInsideComponentBattlefield)
		{
			if (spawnTimer > 0.0)
			{
				spawnTimer -= delta;
			}
			else if (TowerDefenseManager.HasDeathRecord(base.camp, requireAngelEligible: true))
			{
				spawnTimer = 15.0;
				SendStateEvent("ToShooting");
			}
			else
			{
				spawnTimer = 3.0;
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

	public override async void Blow()
	{
		if (instance.invincible)
		{
			BlowBack(1.0, 1.0);
		}
		isBlow = true;
		HitBoxDestroy();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 vector = new Vector2(logicalGlobalPosition.X + (float)((double)(TowerDefenseManager.Instance.GetMapGridSize().Y * (float)TowerDefenseManager.Instance.GetMapGridNum().Y) * 2.0), logicalGlobalPosition.Y);
		Tween tween = CreateTween();
		tween.TweenMethod(Callable.From<Vector2>(base.SetLogicalGlobalPosition), logicalGlobalPosition, vector, 1.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "speed", speed },
			{ "isRevive", isRevive },
			{ "isReviveOver", isReviveOver },
			{ "isBlow", isBlow },
			{ "spawnTimer", spawnTimer },
			{ "invincible", invincible },
			{ "invincibleTimer", invincibleTimer }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		speed = data.GetValueOrDefault("speed", 10.0).AsDouble();
		isRevive = data.GetValueOrDefault("isRevive", false).AsBool();
		isReviveOver = data.GetValueOrDefault("isReviveOver", false).AsBool();
		isBlow = data.GetValueOrDefault("isBlow", false).AsBool();
		spawnTimer = data.GetValueOrDefault("spawnTimer", 15.0).AsDouble();
		invincible = data.GetValueOrDefault("invincible", false).AsBool();
		invincibleTimer = data.GetValueOrDefault("invincibleTimer", 8.0).AsDouble();
	}

	public override async void DestroySet()
	{
		if (!isRevive)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (isBlow)
			{
				CreateSelf(new Vector2((float)groundRight, logicalGlobalPosition.Y));
			}
			else
			{
				CreateSelf(logicalGlobalPosition);
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	public async void CreateSelf(Vector2 pos)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseCharacter zombie = packet.Create(pos, base.gridPos, base.groundHeight);
		if (zombie is TowerDefenseZombieAngel towerDefenseZombieAngel)
		{
			towerDefenseZombieAngel.isRevive = true;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				zombie.Hypnoses();
			}).CallDeferred();
		}
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (GodotObject.IsInstanceValid(zombie.instance))
				{
					zombie.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(zombie.transformPoint))
				{
					zombie.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		zombie.invisible = base.invisible;
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packet.saveKey, base.gridPos.X, base.gridPos.Y, nextSyncId, _hitpointScale, _scale.X, instance.hypnoses, 0.0, useCreate: true, pos.X, pos.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		((TowerDefenseZombie)zombie).Walk();
	}

	public async void CreateDeathCharacter()
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || !TowerDefenseManager.TryTakeLatestDeathRecord(base.camp, requireAngelEligible: true, out var record))
		{
			return;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(RIVIVE, record.GridPosition);
		towerDefenseEffectSpriteOnce.GlobalPosition = record.Position;
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", towerDefenseEffectSpriteOnce);
		TowerDefensePacketConfig towerDefensePacketConfig = record.Packet;
		TowerDefenseCharacter character = towerDefensePacketConfig.Create(record.Position, record.GridPosition);
		character.invisible = record.Invisible;
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", character);
		if (GodotObject.IsInstanceValid(character.transformPoint))
		{
			character.transformPoint.Scale = (float)record.Scale * Vector2.One;
		}
		if (GodotObject.IsInstanceValid(character.instance))
		{
			character.instance.hitpointScale = record.HitpointScale;
		}
		if (instance.hypnoses)
		{
			character.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, character);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(towerDefensePacketConfig.saveKey, record.GridPosition.X, record.GridPosition.Y, nextSyncId, record.HitpointScale, record.Scale, instance.hypnoses, 0.0, useCreate: true, record.Position.X, record.Position.Y, walkAfterSpawn: true);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		((TowerDefenseZombie)character).Walk();
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "shooting")
		{
			CreateDeathCharacter();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Revive"))
		{
			if (clip == "Shooting")
			{
				Walk();
			}
		}
		else
		{
			Walk();
		}
	}
}
