using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBungiSP : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private StateHandle _dropStateHandle;

	private StateHandle _grabStateHandle;

	private StateHandle _riseStateHandle;

	private bool _stateSignalsConnected;

	public bool waitGrab;

	public bool hasPlant;

	public double waitTimer;

	public bool canBlock = true;

	public Tween dropTween;

	public bool grabOver;

	public bool suppressBungeeTarget;

	private TowerDefenseBungiTargetSP _target;

	private int _dropPresentationGeneration;

	private int _risePresentationGeneration;

	private Tween _riseTween;

	private int _networkSpecialStateRevision = 1;

	private bool _hasImportedNetworkState;

	private int _dropActionSequence;

	private int _grabActionSequence;

	private int _remoteDropActionSequence;

	private int _remoteGrabActionSequence;

	private int _remoteDropAudioGeneration;

	private bool _remoteActionStateInitialized;

	private bool _floopAudioPlayed;

	private bool _preserveProgressStateOnNextWalk;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_dropStateHandle = base.StateMachine?.GetStateById("zombie.bungi_sp.drop");
		_grabStateHandle = base.StateMachine?.GetStateById("zombie.bungi_sp.grab");
		_riseStateHandle = base.StateMachine?.GetStateById("zombie.bungi_sp.rise");
		StateHandle dropStateHandle = _dropStateHandle;
		if (dropStateHandle == null || !dropStateHandle.IsValid)
		{
			return;
		}
		StateHandle grabStateHandle = _grabStateHandle;
		if (grabStateHandle != null && grabStateHandle.IsValid)
		{
			StateHandle riseStateHandle = _riseStateHandle;
			if (riseStateHandle != null && riseStateHandle.IsValid)
			{
				_dropStateHandle.Entered += DropEntered;
				_dropStateHandle.Exited += DropExited;
				_dropStateHandle.PhysicsProcessing += DropProcessing;
				_grabStateHandle.Entered += GrabEntered;
				_grabStateHandle.Exited += GrabExited;
				_grabStateHandle.PhysicsProcessing += GrabProcessing;
				_riseStateHandle.Entered += RiseEntered;
				_riseStateHandle.Exited += RiseExited;
				_riseStateHandle.PhysicsProcessing += RiseProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_dropStateHandle != null)
			{
				_dropStateHandle.Entered -= DropEntered;
				_dropStateHandle.Exited -= DropExited;
				_dropStateHandle.PhysicsProcessing -= DropProcessing;
			}
			if (_grabStateHandle != null)
			{
				_grabStateHandle.Entered -= GrabEntered;
				_grabStateHandle.Exited -= GrabExited;
				_grabStateHandle.PhysicsProcessing -= GrabProcessing;
			}
			if (_riseStateHandle != null)
			{
				_riseStateHandle.Entered -= RiseEntered;
				_riseStateHandle.Exited -= RiseExited;
				_riseStateHandle.PhysicsProcessing -= RiseProcessing;
			}
			_dropStateHandle = null;
			_grabStateHandle = null;
			_riseStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		_remoteDropAudioGeneration++;
		CancelDropPresentation();
		CancelRisePresentation();
		DisconnectStateSignals();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			targetRegistrationComponent.canCarry = false;
			AddToGroup("Bungi", persistent: true);
			if (TowerDefenseManager.Instance.IsGameRunning() && !_hasImportedNetworkState)
			{
				base.z = 600.0;
				isGround = false;
			}
			ConnectStateSignals();
			ApplyCurrentTargetability();
		}
	}

	public override void FinalizeProgressRestore()
	{
		string text = base.CurrentStateHandle?.StableId.ToString() ?? "";
		_preserveProgressStateOnNextWalk = text.StartsWith("zombie.bungi_sp.");
		base.FinalizeProgressRestore();
		ApplyCurrentTargetability();
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		_hasImportedNetworkState = _hasImportedNetworkState || data.ContainsKey("z");
		int num = data.GetValueOrDefault("rev", _networkSpecialStateRevision).AsInt32();
		if (TowerDefenseManager.HasGameplayAuthority || num >= _networkSpecialStateRevision)
		{
			_networkSpecialStateRevision = Mathf.Max(_networkSpecialStateRevision, num);
			suppressBungeeTarget = data.GetValueOrDefault("suppressBungeeTarget", suppressBungeeTarget).AsBool();
			canBlock = data.GetValueOrDefault("canBlock", canBlock).AsBool();
			waitGrab = data.GetValueOrDefault("waitGrab", waitGrab).AsBool();
			hasPlant = data.GetValueOrDefault("hasPlant", hasPlant).AsBool();
			grabOver = data.GetValueOrDefault("grabOver", grabOver).AsBool();
			waitTimer = data.GetValueOrDefault("waitTimer", waitTimer).AsDouble();
			base.groundHeight = data.GetValueOrDefault("groundHeight", base.groundHeight).AsDouble();
			isGround = data.GetValueOrDefault("isGround", isGround).AsBool();
			int num2 = data.GetValueOrDefault("dropActionSequence", _dropActionSequence).AsInt32();
			int num3 = data.GetValueOrDefault("grabActionSequence", _grabActionSequence).AsInt32();
			if (!TowerDefenseManager.HasGameplayAuthority)
			{
				ApplyRemoteActionAudio(num2, num3);
				CancelDropPresentation();
				CancelRisePresentation();
				base.z = data.GetValueOrDefault("z", base.z).AsDouble();
			}
			_dropActionSequence = Mathf.Max(_dropActionSequence, num2);
			_grabActionSequence = Mathf.Max(_grabActionSequence, num3);
			if (IsNodeReady())
			{
				ApplyCurrentTargetability();
			}
		}
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		return ExportBungeeNetworkState();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return ExportBungeeNetworkState();
	}

	private Dictionary ExportBungeeNetworkState()
	{
		return new Dictionary
		{
			["rev"] = _networkSpecialStateRevision,
			["suppressBungeeTarget"] = suppressBungeeTarget,
			["canBlock"] = canBlock,
			["waitGrab"] = waitGrab,
			["hasPlant"] = hasPlant,
			["grabOver"] = grabOver,
			["waitTimer"] = waitTimer,
			["dropActionSequence"] = _dropActionSequence,
			["grabActionSequence"] = _grabActionSequence,
			["z"] = base.z,
			["groundHeight"] = base.groundHeight,
			["isGround"] = isGround
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ImportNetworkSpawnState(data);
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return _networkSpecialStateRevision;
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		return true;
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			Destroy();
		}
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			Destroy();
		}
	}

	public override void Walk()
	{
		ActivateGameplayProcessing();
		if (_preserveProgressStateOnNextWalk)
		{
			_preserveProgressStateOnNextWalk = false;
		}
		else if (TowerDefenseManager.HasGameplayAuthority)
		{
			MarkBungeeStateDirty();
			SendStateEvent("ToDrop");
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!TowerDefenseManager.HasGameplayAuthority || !waitGrab)
		{
			return;
		}
		if (waitTimer < 3.0)
		{
			if (!base.sprite.pause)
			{
				waitTimer += delta * timeScale;
			}
			return;
		}
		waitGrab = false;
		foreach (string item in new List<string>(buff.buffDictionary.Keys))
		{
			if (item != "Hypnoses")
			{
				buff.DeleteBuff(item);
			}
		}
		MarkBungeeStateDirty();
		SendStateEvent("ToGrab");
	}

	public override void Spawn()
	{
		base.z = 600.0;
		isGround = false;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		List<Vector2I> list = new List<Vector2I>();
		foreach (Node item in GetTree().GetNodesInGroup("Bungi"))
		{
			if (item != this && ((TowerDefenseCharacter)item).gridPos != new Vector2I(-1, -1))
			{
				list.Add(((TowerDefenseCharacter)item).gridPos);
			}
		}
		List<TowerDefenseCharacter> list2 = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (!(cleanCharacters is TowerDefensePlant) || cleanCharacters.isDestroy || cleanCharacters.die || !CanTarget(cleanCharacters) || cleanCharacters is TowerDefensePlantBowlingBase || !cleanCharacters.instance.canBeCollection || list.Contains(cleanCharacters.gridPos))
			{
				continue;
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(cleanCharacters.gridPos);
			bool flag = false;
			foreach (TowerDefenseCharacter character in mapCell.GetCharacterList())
			{
				if (character is TowerDefenseBungiTargetSP)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(cleanCharacters);
			}
		}
		if (list2.Count <= 0)
		{
			Destroy();
			base.gridPos = new Vector2I(GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().X), GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().Y));
		}
		else
		{
			TowerDefenseCharacter towerDefenseCharacter = list2[GD.RandRange(0, list2.Count - 1)];
			SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(towerDefenseCharacter.gridPos));
			base.gridPos = towerDefenseCharacter.gridPos;
		}
	}

	public void DropEntered()
	{
		int presentationGeneration = ++_dropPresentationGeneration;
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			_dropActionSequence = NextBungeeActionSequence(_dropActionSequence);
			MarkBungeeStateDirty();
		}
		if (!GodotObject.IsInstanceValid(cell))
		{
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				Destroy();
			}
		}
		else
		{
			ApplyTargetability(landed: false);
			base.z = 600.0;
			isGround = false;
			SpawnTarget();
			CompleteDropPresentationAsync(presentationGeneration);
		}
	}

	private async Task CompleteDropPresentationAsync(int presentationGeneration)
	{
		if (!(await WaitForStateDelayAsync(_dropStateHandle, 1.0)) || !IsDropPresentationCurrent(presentationGeneration))
		{
			return;
		}
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			AudioManager.Instance.AudioPlay("BungeeScream");
		}
		if (!(await WaitForStateDelayAsync(_dropStateHandle, 1.0)) || !IsDropPresentationCurrent(presentationGeneration) || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		dropTween = CreateTween();
		dropTween.SetEase(Tween.EaseType.Out);
		dropTween.SetTrans(Tween.TransitionType.Cubic);
		dropTween.TweenProperty(this, "z", cell.GetGroundHeight(), 1.0);
		base.sprite.SetAnimation("Drop", loop: true, 0.2);
		if (await WaitForStateDelayAsync(_dropStateHandle, 1.0) && IsDropPresentationCurrent(presentationGeneration))
		{
			isGround = true;
			base.groundHeight = cell.GetGroundHeight();
			base.z = base.groundHeight;
			waitGrab = true;
			ApplyTargetability(landed: true);
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				MarkBungeeStateDirty();
				Idle();
			}
		}
	}

	private void SpawnTarget()
	{
		if (suppressBungeeTarget || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("BungiTargetSP");
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos, playAudio: false) : packetConfig.Plant(base.gridPos, playAudio: false));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.camp = base.camp;
				_target = towerDefenseCharacter as TowerDefenseBungiTargetSP;
				TowerDefenseManager.PublishSpawnedCharacter("BungiTargetSP", towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", new Dictionary { ["plant_play_audio"] = false });
			}
		}
	}

	public void DropProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DropExited()
	{
		CancelDropPresentation();
	}

	private void ApplyTargetability(bool landed)
	{
		SetHitBoxSuppressed(HitBoxSuppressionReason.Scripted, !landed);
		SetHitBoxMonitorSuppressed(HitBoxSuppressionReason.Scripted, !landed);
		instance.invincible = !landed;
		instance.canBeCollection = landed;
		targetRegistrationComponent.canProjectileCheck = landed;
		instance.maskFlags = (landed ? 1 : 0);
	}

	private void ApplyCurrentTargetability()
	{
		if (!IsNodeReady() || !GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		TargetRegistrationComponent obj = targetRegistrationComponent;
		if (obj == null || obj.IsReleased)
		{
			return;
		}
		int num;
		if (isGround)
		{
			StateHandle grabStateHandle = _grabStateHandle;
			if (grabStateHandle == null || !grabStateHandle.IsActive)
			{
				StateHandle riseStateHandle = _riseStateHandle;
				num = ((riseStateHandle == null || !riseStateHandle.IsActive) ? 1 : 0);
				goto IL_0061;
			}
		}
		num = 0;
		goto IL_0061;
		IL_0061:
		bool landed = (byte)num != 0;
		ApplyTargetability(landed);
	}

	public void GrabEntered()
	{
		MarkBungeeStateDirty();
		instance.unUseBuffFlags = -9;
		ApplyTargetability(landed: false);
		base.sprite.SetAnimation("Grab", loop: false, 0.2);
	}

	public void GrabProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void GrabExited()
	{
	}

	public void RiseEntered()
	{
		int presentationGeneration = ++_risePresentationGeneration;
		MarkBungeeStateDirty();
		base.sprite.SetAnimation("Rise", loop: true, 0.2);
		isGround = false;
		ApplyTargetability(landed: false);
		double duration = 1.5;
		if (!canBlock)
		{
			duration = 0.75;
		}
		_riseTween = CreateTween();
		_riseTween.SetEase(Tween.EaseType.Out);
		_riseTween.SetTrans(Tween.TransitionType.Cubic);
		_riseTween.TweenProperty(this, "z", 600, duration);
		CompleteRisePresentationAsync(presentationGeneration, duration);
	}

	private async Task CompleteRisePresentationAsync(int presentationGeneration, double duration)
	{
		if (await WaitForStateDelayAsync(_riseStateHandle, duration) && IsRisePresentationCurrent(presentationGeneration) && TowerDefenseManager.HasGameplayAuthority)
		{
			Destroy();
		}
	}

	public void RiseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void RiseExited()
	{
		CancelRisePresentation();
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		if (command == "grab")
		{
			StealAllPlants();
		}
	}

	private void StealAllPlants()
	{
		PlayFloopAudioOnce();
		if (!TowerDefenseManager.HasGameplayAuthority || grabOver)
		{
			return;
		}
		grabOver = true;
		_grabActionSequence = NextBungeeActionSequence(_grabActionSequence);
		MarkBungeeStateDirty();
		cell = TowerDefenseManager.GetMapCell(base.gridPos);
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(cell.characterList))
		{
			if (GodotObject.IsInstanceValid(item) && !item.isDestroy && !item.die && !(item is TowerDefenseGravestone) && !(item is TowerDefenseCrater) && item.camp != base.camp && item is TowerDefensePlant && !(item is TowerDefensePlantBowlingBase))
			{
				hasPlant = true;
				item.die = true;
				item.Destroy(freeInstance: false);
				if (GodotObject.IsInstanceValid(item.sprite))
				{
					item.sprite.pause = true;
				}
				if (GodotObject.IsInstanceValid(item.shadowSprite))
				{
					item.shadowSprite.Visible = false;
					item.shadowSprite.Texture = null;
				}
				item.DisableGameplayForPermanentEmbeddedVisual();
				item.Reparent(spriteGroup);
			}
		}
		if (hasPlant)
		{
			MarkBungeeStateDirty();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (!(clip == "Grab"))
		{
			return;
		}
		StealAllPlants();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			StateHandle grabStateHandle = _grabStateHandle;
			if (grabStateHandle != null && grabStateHandle.IsActive)
			{
				SendStateEvent("ToRise");
			}
		}
	}

	public override bool CanBlock()
	{
		if (canBlock)
		{
			return base.z <= 50.0;
		}
		return false;
	}

	public override void Block(TowerDefenseCharacter target)
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			CancelDropPresentation();
			canBlock = false;
			MarkBungeeStateDirty();
			instance.invincible = true;
			if (GodotObject.IsInstanceValid(_target))
			{
				_target.Blow();
			}
			SendStateEvent("ToRise");
			target.Hurt(100.0);
		}
	}

	private void MarkBungeeStateDirty()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_networkSpecialStateRevision++;
			if (_networkSpecialStateRevision <= 0)
			{
				_networkSpecialStateRevision = 1;
			}
		}
	}

	private static int NextBungeeActionSequence(int current)
	{
		current++;
		if (current > 0)
		{
			return current;
		}
		return 1;
	}

	private void ApplyRemoteActionAudio(int dropSequence, int grabSequence)
	{
		if (!_remoteActionStateInitialized)
		{
			_remoteActionStateInitialized = true;
			_remoteDropActionSequence = dropSequence;
			_remoteGrabActionSequence = grabSequence;
			_floopAudioPlayed = grabSequence > 0;
			return;
		}
		if (dropSequence > _remoteDropActionSequence)
		{
			_remoteDropActionSequence = dropSequence;
			PlayRemoteBungeeScreamAfterDelayAsync(dropSequence);
		}
		if (grabSequence > _remoteGrabActionSequence)
		{
			_remoteGrabActionSequence = grabSequence;
			PlayFloopAudioOnce();
		}
	}

	private async Task PlayRemoteBungeeScreamAfterDelayAsync(int actionSequence)
	{
		int generation = ++_remoteDropAudioGeneration;
		await ToSignal(GetTree().CreateTimer(1.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (generation == _remoteDropAudioGeneration && actionSequence == _remoteDropActionSequence && IsInsideTree() && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay("BungeeScream");
		}
	}

	private void PlayFloopAudioOnce()
	{
		if (!_floopAudioPlayed)
		{
			_floopAudioPlayed = true;
			if (GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay("Floop");
			}
		}
	}

	private bool IsDropPresentationCurrent(int generation)
	{
		if (generation == _dropPresentationGeneration && IsInsideTree())
		{
			return _dropStateHandle?.IsActive ?? false;
		}
		return false;
	}

	private void CancelDropPresentation()
	{
		_dropPresentationGeneration++;
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		dropTween = null;
	}

	private bool IsRisePresentationCurrent(int generation)
	{
		if (generation == _risePresentationGeneration && IsInsideTree())
		{
			return _riseStateHandle?.IsActive ?? false;
		}
		return false;
	}

	private void CancelRisePresentation()
	{
		_risePresentationGeneration++;
		if (GodotObject.IsInstanceValid(_riseTween))
		{
			_riseTween.Kill();
		}
		_riseTween = null;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "waitGrab", waitGrab },
			{ "hasPlant", hasPlant },
			{ "waitTimer", waitTimer },
			{ "canBlock", canBlock },
			{ "grabOver", grabOver },
			{ "suppressBungeeTarget", suppressBungeeTarget },
			{ "dropActionSequence", _dropActionSequence },
			{ "grabActionSequence", _grabActionSequence },
			{ "networkSpecialStateRevision", _networkSpecialStateRevision }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		waitGrab = data.GetValueOrDefault("waitGrab", false).AsBool();
		hasPlant = data.GetValueOrDefault("hasPlant", false).AsBool();
		waitTimer = data.GetValueOrDefault("waitTimer", 0.0).AsDouble();
		canBlock = data.GetValueOrDefault("canBlock", true).AsBool();
		grabOver = data.GetValueOrDefault("grabOver", false).AsBool();
		suppressBungeeTarget = data.GetValueOrDefault("suppressBungeeTarget", false).AsBool();
		_dropActionSequence = data.GetValueOrDefault("dropActionSequence", 0).AsInt32();
		_grabActionSequence = data.GetValueOrDefault("grabActionSequence", grabOver ? 1 : 0).AsInt32();
		_floopAudioPlayed = grabOver || _grabActionSequence > 0;
		_networkSpecialStateRevision = data.GetValueOrDefault("networkSpecialStateRevision", 1).AsInt32();
		if (IsNodeReady())
		{
			ApplyCurrentTargetability();
		}
	}
}
