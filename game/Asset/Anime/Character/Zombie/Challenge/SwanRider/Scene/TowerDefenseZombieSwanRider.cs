using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieSwanRider : TowerDefenseZombie
{
	private const string CarryOriginalMaskFlagsSaveKey = "carryOriginalMaskFlags";

	private StateHandle _flyStateHandle;

	private bool _stateSignalsConnected;

	private int _carryOriginalMaskFlags;

	private bool _hasCarryOriginalMaskFlags;

	public bool useCone;

	public bool useBucket;

	public bool over;

	public bool isFly;

	public double speed = 30.0;

	public bool isFlying;

	public bool mowerKill;

	public TowerDefenseCharacter carryCharacter;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_flyStateHandle = base.StateMachine?.GetStateById("zombie.swan_rider.fly");
			StateHandle flyStateHandle = _flyStateHandle;
			if (flyStateHandle != null && flyStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.Exited += FlyExited;
				_flyStateHandle.Processing += FlyProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_flyStateHandle != null)
			{
				_flyStateHandle.Entered -= FlyEntered;
				_flyStateHandle.Exited -= FlyExited;
				_flyStateHandle.Processing -= FlyProcessing;
			}
			_flyStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint())
		{
			ReleaseCarriedCharacter();
		}
		DisconnectStateSignals();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		ConnectStateSignals();
		if (!base.IsProgressRestoreInFlight)
		{
			double num = GD.Randf();
			if (num < 0.1)
			{
				useBucket = true;
				base.sprite.SetFliters((Array?)new Array<string> { "anim_bucket" }, open: true);
			}
			else if (num < 0.3)
			{
				useCone = true;
				base.sprite.SetFliters((Array?)new Array<string> { "anim_cone" }, open: true);
			}
		}
		targetRegistrationComponent.canCarry = false;
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && inGame && !over && !die && !nearDie)
		{
			if (GodotObject.IsInstanceValid(carryCharacter))
			{
				ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
				Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				Vector2 globalPositionForPhysicsFrame2 = carryCharacter.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				globalPositionForPhysicsFrame2.X = globalPositionForPhysicsFrame.X + (instance.hypnoses ? (-30f) : 30f);
				carryCharacter.SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame2, currentPhysicsFrame);
				carryCharacter.groundHeight = base.z + 60.0;
				carryCharacter.z = base.z + 60.0;
			}
			else
			{
				ProcessCarryCandidate();
			}
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * base.walkSpeedScale;
		if (attackComponent.CanAttack())
		{
			if (GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
			{
				attackComponent.target = attackComponent.target.cell.GetSpike();
			}
			if ((attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
			{
				attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			else
			{
				if (attackComponent.target.instance.spikeHurt != -1.0)
				{
					TowerDefenseCharacter target = attackComponent.target;
					double spikeHurt = attackComponent.target.instance.spikeHurt;
					target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
				}
				Die();
			}
		}
		if (instance.hitpoints <= 1000.0 && !isFly)
		{
			if (base.inWater)
			{
				base.sprite.SetAnimation("RiseWater", loop: false, 0.2);
			}
			else
			{
				base.sprite.SetAnimation("Rise", loop: false, 0.2);
			}
			isFly = true;
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		HitBoxDestroy();
		base.sprite.SetFliters((Array?)new Array<string>
		{
			"anim_bucket", "anim_cone", "anim_hair", "Zombie_outerarm_lower", "Zombie_outerarm_upper", "Zombie_outerarm_hand", "anim_head2", "Zombie_tie", "Zombie_body", "Zombie_outerleg_lower",
			"Zombie_outerleg_foot", "Zombie_outerleg_upper", "Zombie_innerleg_foot", "Zombie_innerleg_lower", "Zombie_innerleg_upper", "anim_head1", "Zombie_neck", "anim_innerarm1", "anim_innerarm2", "anim_innerarm3"
		}, open: false);
		ReleaseCarriedCharacter();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		if (mowerKill)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			return;
		}
		string packetName = "ZombieNormal";
		if (useCone)
		{
			packetName = "ZombieNormalCone";
		}
		if (useBucket)
		{
			packetName = "ZombieNormalBucket";
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		double height = base.groundHeight;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = packetConfig.Create(logicalGlobalPosition, base.gridPos, height);
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		zombie.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Hypnoses();
				}
			}).CallDeferred();
		}
		zombie.CallDeferred("WalkReady");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, base.gridPos.X, base.gridPos.Y, nextSyncId, hitpointScale, scale.X, instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, height);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private void ReleaseCarriedCharacter()
	{
		TowerDefenseCharacter towerDefenseCharacter = carryCharacter;
		if (TowerDefenseZombieCarryRelation.Release(this, ref carryCharacter, base.groundHeight) && _hasCarryOriginalMaskFlags && GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			towerDefenseCharacter.instance.maskFlags = _carryOriginalMaskFlags;
		}
		_carryOriginalMaskFlags = 0;
		_hasCarryOriginalMaskFlags = false;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary();
		TowerDefenseZombieCarryRelation.Export(dictionary, carryCharacter);
		if (GodotObject.IsInstanceValid(carryCharacter) && _hasCarryOriginalMaskFlags)
		{
			dictionary["carryOriginalMaskFlags"] = _carryOriginalMaskFlags;
		}
		dictionary["useCone"] = useCone;
		dictionary["useBucket"] = useBucket;
		dictionary["isFly"] = isFly;
		dictionary["isFlying"] = isFlying;
		dictionary["over"] = over;
		dictionary["speed"] = speed;
		dictionary["mowerKill"] = mowerKill;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		useCone = data.GetValueOrDefault("useCone", false).AsBool();
		useBucket = data.GetValueOrDefault("useBucket", false).AsBool();
		isFly = data.GetValueOrDefault("isFly", false).AsBool();
		isFlying = data.GetValueOrDefault("isFlying", false).AsBool();
		over = data.GetValueOrDefault("over", false).AsBool();
		speed = data.GetValueOrDefault("speed", 30.0).AsDouble();
		mowerKill = data.GetValueOrDefault("mowerKill", false).AsBool();
		base.sprite.SetFliters((Array?)new Array<string> { "anim_bucket" }, useBucket);
		base.sprite.SetFliters((Array?)new Array<string> { "anim_cone" }, useCone);
		TowerDefenseZombie towerDefenseZombie = TowerDefenseZombieCarryRelation.Resolve(data);
		int value = (data.ContainsKey("carryOriginalMaskFlags") ? data["carryOriginalMaskFlags"].AsInt32() : (towerDefenseZombie?.config?.maskFlags ?? 1));
		if (!GodotObject.IsInstanceValid(towerDefenseZombie) || towerDefenseZombie == this || !AttachCarriedZombie(towerDefenseZombie, value))
		{
			ReleaseCarriedCharacter();
		}
	}

	private void ProcessCarryCandidate()
	{
		if (!TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectListForCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListForCamp(rect, base.camp);
		for (int i = 0; i < charactersIntersectingRectListForCamp.Count; i++)
		{
			if (charactersIntersectingRectListForCamp[i] is TowerDefenseZombie zombie)
			{
				TryCarryZombieAsync(zombie);
				if (GodotObject.IsInstanceValid(carryCharacter))
				{
					break;
				}
			}
		}
	}

	private async void TryCarryZombieAsync(TowerDefenseZombie zombie)
	{
		if (TryCarryZombie(zombie) && !isFly)
		{
			if (base.inWater)
			{
				base.sprite.SetAnimation("RiseWater", loop: false, 0.2);
			}
			else
			{
				base.sprite.SetAnimation("Rise", loop: false, 0.2);
			}
			isFly = true;
			await ToSignal(GetTree().CreateTimer(0.3, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			isFlying = true;
		}
	}

	private bool TryCarryZombie(TowerDefenseZombie zombie)
	{
		if (over)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning)
		{
			return false;
		}
		if (!inGame)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie == this)
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if (zombie.camp != base.camp)
		{
			return false;
		}
		if (!zombie.CanCollision(instance.maskFlags))
		{
			return false;
		}
		if (zombie.instance.zombiePhysique > TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL)
		{
			return false;
		}
		if (!zombie.targetRegistrationComponent.canCarry)
		{
			return false;
		}
		return AttachCarriedZombie(zombie);
	}

	private bool AttachCarriedZombie(TowerDefenseZombie zombie, int? originalMaskFlags = null)
	{
		if (!TowerDefenseZombieCarryRelation.Attach(this, ref carryCharacter, zombie, base.z + 60.0))
		{
			return false;
		}
		_carryOriginalMaskFlags = originalMaskFlags ?? zombie.instance.maskFlags;
		_hasCarryOriginalMaskFlags = true;
		zombie.instance.maskFlags = 2;
		return true;
	}

	public void FlyEntered()
	{
		base.sprite.SetAnimation("Fly");
		instance.collisionFlags = 2;
		instance.maskFlags = 2;
	}

	public void FlyProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!base.sprite.pause)
		{
			if (isFlying)
			{
				double num = base.sprite.timeScale;
				double num2 = (base.sprite.playBack ? (-1.0) : 1.0);
				ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
				Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				globalPositionForPhysicsFrame.X -= (float)(speed * delta * num * (double)transformPoint.Scale.X * (double)base.Scale.X * num2);
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target) && attackComponent.target is TowerDefenseZombie)
			{
				attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
		}
	}

	public void FlyExited()
	{
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			ReleaseCarriedCharacter();
		}
	}

	public override void Blow()
	{
		if (isFly)
		{
			BlowBack(1.0, 1.0);
		}
	}

	public override void Walk()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (isFly)
		{
			SendStateEvent("ToFly");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (((!die && !isDestroy) || (!(clip == "Rise") && !(clip == "RiseWater"))) && (clip == "Rise" || clip == "RiseWater"))
		{
			isFlying = true;
			Walk();
		}
	}
}
