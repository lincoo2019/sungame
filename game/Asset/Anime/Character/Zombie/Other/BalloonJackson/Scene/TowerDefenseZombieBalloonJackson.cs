using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBalloonJackson : TowerDefenseZombie
{
	public bool pop;

	public double speed = 30.0;

	public bool audioPlay;

	private StateHandle _flyStateHandle;

	private StateHandle _flyAttackStateHandle;

	private StateHandle _popStateHandle;

	private bool _roleStateSignalsConnected;

	private Tween _logicalBlowTween;

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
		_flyStateHandle = base.StateMachine?.GetStateById("zombie.balloon_jackson.fly");
		_flyAttackStateHandle = base.StateMachine?.GetStateById("zombie.balloon_jackson.fly_attack");
		_popStateHandle = base.StateMachine?.GetStateById("zombie.balloon_jackson.pop");
		StateHandle flyStateHandle = _flyStateHandle;
		if (flyStateHandle == null || !flyStateHandle.IsValid)
		{
			return;
		}
		StateHandle flyAttackStateHandle = _flyAttackStateHandle;
		if (flyAttackStateHandle != null && flyAttackStateHandle.IsValid)
		{
			StateHandle popStateHandle = _popStateHandle;
			if (popStateHandle != null && popStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.Exited += FlyExited;
				_flyStateHandle.PhysicsProcessing += FlyProcessing;
				_flyAttackStateHandle.Entered += FlyAttackEntered;
				_flyAttackStateHandle.Exited += FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing += FlyAttackProcessing;
				_popStateHandle.Entered += PopEntered;
				_popStateHandle.Exited += PopExited;
				_popStateHandle.PhysicsProcessing += PopProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_flyStateHandle != null)
			{
				_flyStateHandle.Entered -= FlyEntered;
				_flyStateHandle.Exited -= FlyExited;
				_flyStateHandle.PhysicsProcessing -= FlyProcessing;
			}
			_flyStateHandle = null;
			if (_flyAttackStateHandle != null)
			{
				_flyAttackStateHandle.Entered -= FlyAttackEntered;
				_flyAttackStateHandle.Exited -= FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing -= FlyAttackProcessing;
			}
			_flyAttackStateHandle = null;
			if (_popStateHandle != null)
			{
				_popStateHandle.Entered -= PopEntered;
				_popStateHandle.Exited -= PopExited;
				_popStateHandle.PhysicsProcessing -= PopProcessing;
			}
			_popStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelLogicalBlowTween();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater");
			ConnectRoleStateSignals();
		}
	}

	public void FlyEntered()
	{
		base.sprite.SetAnimation("Idle", loop: true, 0.2);
	}

	public virtual void FlyProcessing(double delta)
	{
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
		if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 20.0)
		{
			instance.ArmorDelete("Balloon");
			return;
		}
		if (!audioPlay && (double)globalPositionForPhysicsFrame.X < groundRight)
		{
			AudioManager.Instance.AudioPlay("BalloonInflate");
			audioPlay = true;
		}
		if (!base.sprite.pause && attackComponent.CanAttack())
		{
			SendStateEvent("ToFlyAttack");
		}
	}

	public void FlyExited()
	{
	}

	public void PopEntered()
	{
		base.sprite.SetAnimation("Pop", loop: false, 0.2);
	}

	public virtual void PopProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void PopExited()
	{
	}

	public void FlyAttackEntered()
	{
		base.sprite.SetAnimation("FlyEat", loop: true, 0.2);
		ActivateAttackAfterStateDelay(_flyAttackStateHandle);
	}

	public virtual void FlyAttackProcessing(double delta)
	{
		if (!attackComponent.CanAttack())
		{
			SendStateEvent("ToFly");
		}
		else if (startAttack && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps)
		{
			attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
		}
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void FlyAttackExited()
	{
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

	public override void DieEntered()
	{
		CancelLogicalBlowTween();
		base.DieEntered();
	}

	public override void Walk()
	{
		if (pop)
		{
			SendStateEvent("ToWalk");
		}
		else
		{
			SendStateEvent("ToFly");
		}
	}

	public override void Blow()
	{
		if (!pop)
		{
			HitBoxDestroy();
			StartLogicalBlowTween();
		}
	}

	private void StartLogicalBlowTween()
	{
		CancelLogicalBlowTween();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 targetPosition = logicalGlobalPosition + new Vector2(TowerDefenseManager.Instance.GetMapGridSize().Y * (float)TowerDefenseManager.Instance.GetMapGridNum().Y * 2f, 0f);
		Tween tween = CreateTween();
		_logicalBlowTween = tween;
		tween.TweenMethod(Callable.From<Vector2>(base.SetLogicalGlobalPosition), logicalGlobalPosition, targetPosition, 1.0);
		tween.TweenCallback(Callable.From(delegate
		{
			CompleteLogicalBlowTween(tween, targetPosition);
		}));
	}

	private void CompleteLogicalBlowTween(Tween tween, Vector2 targetPosition)
	{
		if (_logicalBlowTween == tween && !isDestroy)
		{
			_logicalBlowTween = null;
			SetLogicalGlobalPosition(targetPosition);
			Destroy();
		}
	}

	private void CancelLogicalBlowTween()
	{
		if (GodotObject.IsInstanceValid(_logicalBlowTween))
		{
			_logicalBlowTween.Kill();
		}
		_logicalBlowTween = null;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Pop")
		{
			Walk();
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (!(armorName == "Balloon"))
		{
			return;
		}
		AudioManager.Instance.AudioPlay("BalloonPop");
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		if (!GetHasArmor("SpecialHelmet"))
		{
			instance.unUseBuffFlags = 0;
		}
		else
		{
			instance.armorOverrideUnUseBuffFlagSave = 0;
		}
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			pop = true;
			SendStateEvent("ToPop");
			return;
		}
		Vector2 pos = GetLogicalGlobalPosition() + new Vector2(4f, 0f);
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetPacketConfig("ZombieJacksonBlackHelmet").Create(pos, base.gridPos, 40.0);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
			{
				towerDefenseCharacter.instance.hitpointScale = instance.hitpointScale;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.transformPoint))
			{
				towerDefenseCharacter.transformPoint.Scale = transformPoint.Scale;
			}
		}
		towerDefenseCharacter.invisible = base.invisible;
		if (instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter.CallDeferred("Walk");
			pop = true;
			SendStateEvent("ToPop");
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieJacksonBlackHelmet", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, instance.hypnoses, 0.0, useCreate: true, pos.X, pos.Y, walkAfterSpawn: true, 40.0);
			}
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			((ZombieBalloonJacksonSprite)base.sprite).head = false;
			DamagePartCreate("Head", base.sprite.GetNode<AdobeAnimateSpriteBase>("%Propeller"), default(Vector2), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "pop", pop },
			{ "speed", speed }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		pop = data.GetValueOrDefault("pop", false).AsBool();
		speed = data.GetValueOrDefault("speed", 30.0).AsDouble();
	}
}
