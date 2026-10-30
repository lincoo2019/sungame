using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieHypnotist : TowerDefenseZombie
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> singleEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> rangeEvent = new Array<TowerDefenseCharacterEventBase>();

	public bool waitGrab;

	public bool hasPlant;

	private Sprite2D _bungeeTarget;

	public double waitTimer;

	public bool canBlock = true;

	public Tween dropTween;

	public bool grabOver;

	private Tween _targetDropTween;

	private Tween _riseTween;

	private StateHandle _dropStateHandle;

	private StateHandle _grabStateHandle;

	private StateHandle _riseStateHandle;

	private bool _roleStateSignalsConnected;

	private bool _preserveProgressStateOnNextWalk;

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
		_dropStateHandle = base.StateMachine?.GetStateById("zombie.hypnotist.drop");
		_grabStateHandle = base.StateMachine?.GetStateById("zombie.hypnotist.grab");
		_riseStateHandle = base.StateMachine?.GetStateById("zombie.hypnotist.rise");
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
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_dropStateHandle != null)
			{
				_dropStateHandle.Entered -= DropEntered;
				_dropStateHandle.Exited -= DropExited;
				_dropStateHandle.PhysicsProcessing -= DropProcessing;
			}
			_dropStateHandle = null;
			if (_grabStateHandle != null)
			{
				_grabStateHandle.Entered -= GrabEntered;
				_grabStateHandle.Exited -= GrabExited;
				_grabStateHandle.PhysicsProcessing -= GrabProcessing;
			}
			_grabStateHandle = null;
			if (_riseStateHandle != null)
			{
				_riseStateHandle.Entered -= RiseEntered;
				_riseStateHandle.Exited -= RiseExited;
				_riseStateHandle.PhysicsProcessing -= RiseProcessing;
			}
			_riseStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelDropPresentation();
		CancelRisePresentation();
		DisconnectRoleStateSignals();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bungeeTarget = GetNode<Sprite2D>("%BungeeTarget");
			targetRegistrationComponent.canCarry = false;
			ApplyDroppingTargetability();
			AddToGroup("Bungi", persistent: true);
			if (TowerDefenseManager.Instance.IsGameRunning())
			{
				base.z = 600.0;
				isGround = false;
			}
			ConnectRoleStateSignals();
		}
	}

	public override void FinalizeProgressRestore()
	{
		string text = base.CurrentStateHandle?.StableId.ToString() ?? "";
		_preserveProgressStateOnNextWalk = text.StartsWith("zombie.hypnotist.");
		base.FinalizeProgressRestore();
		ApplyCurrentTargetability();
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		Destroy();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		Destroy();
	}

	public override void Walk()
	{
		if (_preserveProgressStateOnNextWalk)
		{
			_preserveProgressStateOnNextWalk = false;
		}
		else
		{
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
		if (!waitGrab)
		{
			return;
		}
		if (waitTimer < 10.0)
		{
			if (!base.sprite.pause)
			{
				waitTimer += delta * timeScale;
			}
			return;
		}
		foreach (string item in new List<string>(buff.buffDictionary.Keys))
		{
			if (item != "Hypnoses")
			{
				buff.DeleteBuff(item);
			}
		}
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
		foreach (Node item2 in GetTree().GetNodesInGroup("Plant"))
		{
			TowerDefenseCharacter towerDefenseCharacter = item2 as TowerDefenseCharacter;
			if (CanStealPlant(towerDefenseCharacter) && !list.Contains(towerDefenseCharacter.gridPos))
			{
				list2.Add(towerDefenseCharacter);
			}
		}
		if (list2.Count <= 0)
		{
			Destroy();
			base.gridPos = new Vector2I(GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().X), GD.RandRange(0, TowerDefenseManager.Instance.GetMapGridNum().Y));
		}
		else
		{
			TowerDefenseCharacter towerDefenseCharacter2 = list2[(int)(GD.Randi() % list2.Count)];
			SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(towerDefenseCharacter2.gridPos));
			base.gridPos = towerDefenseCharacter2.gridPos;
		}
	}

	public void DropEntered()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			Destroy();
			return;
		}
		ApplyDroppingTargetability();
		base.z = 600.0;
		isGround = false;
		_bungeeTarget.Visible = true;
		_targetDropTween = CreateTween();
		_targetDropTween.SetEase(Tween.EaseType.Out);
		_targetDropTween.SetTrans(Tween.TransitionType.Cubic);
		_targetDropTween.TweenProperty(_bungeeTarget, "position", new Vector2(0f, 15f - (float)cell.GetGroundHeight()), 0.5).From(new Vector2(0f, -585f));
		CompleteDropPresentationAsync(_dropStateHandle.ActivationGeneration);
	}

	private async Task CompleteDropPresentationAsync(ulong activationGeneration)
	{
		if (!(await WaitForStateDelayAsync(_dropStateHandle, 1.0)))
		{
			return;
		}
		AudioManager.Instance.AudioPlay("BungeeScream");
		if (await WaitForStateDelayAsync(_dropStateHandle, 1.0))
		{
			dropTween = CreateTween();
			dropTween.SetEase(Tween.EaseType.Out);
			dropTween.SetTrans(Tween.TransitionType.Cubic);
			dropTween.TweenProperty(this, "z", cell.GetGroundHeight(), 1.0);
			base.sprite.SetAnimation("Drop", loop: true, 0.2);
			if (await WaitForStateDelayAsync(_dropStateHandle, 1.0) && IsStateActivationCurrent(_dropStateHandle, activationGeneration))
			{
				isGround = true;
				base.groundHeight = cell.GetGroundHeight();
				base.z = base.groundHeight;
				waitGrab = true;
				ApplyLandedTargetability();
				Idle();
				TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(0.25f, 0.25f), singleEvent, new Array<TowerDefenseCharacter>(), base.camp, instance.collisionFlags);
				InvalidateHitBoxBounds();
			}
		}
	}

	private void ApplyDroppingTargetability()
	{
		ApplyTargetability(landed: false);
	}

	private void ApplyLandedTargetability()
	{
		ApplyTargetability(landed: true);
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

	public void DropProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DropExited()
	{
		CancelDropPresentation();
	}

	private void CancelDropPresentation()
	{
		if (GodotObject.IsInstanceValid(_targetDropTween))
		{
			_targetDropTween.Kill();
		}
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		_targetDropTween = null;
		dropTween = null;
	}

	public void GrabEntered()
	{
		instance.unUseBuffFlags = -1;
		ApplyDroppingTargetability();
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
		base.sprite.SetAnimation("Rise", loop: true, 0.2);
		isGround = false;
		ApplyDroppingTargetability();
		double duration = 1.5;
		if (!canBlock)
		{
			duration = 0.75;
		}
		_riseTween = CreateTween();
		_riseTween.SetParallel();
		_riseTween.SetEase(Tween.EaseType.Out);
		_riseTween.SetTrans(Tween.TransitionType.Cubic);
		_riseTween.TweenProperty(_bungeeTarget, "position", new Vector2(0f, -585f), duration);
		_riseTween.TweenProperty(this, "z", 600, duration);
		CompleteRisePresentationAsync(_riseStateHandle.ActivationGeneration, duration);
	}

	private async Task CompleteRisePresentationAsync(ulong activationGeneration, double duration)
	{
		if (await WaitForStateDelayAsync(_riseStateHandle, duration) && IsStateActivationCurrent(_riseStateHandle, activationGeneration))
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

	private void CancelRisePresentation()
	{
		if (GodotObject.IsInstanceValid(_riseTween))
		{
			_riseTween.Kill();
		}
		_riseTween = null;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		if (command == "grab")
		{
			StealTargets();
		}
	}

	private void StealTargets()
	{
		if (!TowerDefenseManager.HasGameplayAuthority || grabOver)
		{
			return;
		}
		grabOver = true;
		AudioManager.Instance.AudioPlay("Floop");
		cell = TowerDefenseManager.GetMapCell(base.gridPos);
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(cell.characterList);
		foreach (TowerDefenseCharacter item in list)
		{
			if (CanStealPlant(item))
			{
				hasPlant = true;
				if (item is TowerDefensePlant)
				{
					item.die = true;
				}
				item.Destroy(freeInstance: false);
				if (item is TowerDefenseZombie towerDefenseZombie)
				{
					towerDefenseZombie.spritePause = true;
				}
				else
				{
					item.sprite.pause = true;
				}
				item.shadowSprite.Visible = false;
				item.shadowSprite.Texture = null;
				item.DisableGameplayForPermanentEmbeddedVisual();
				item.Reparent(spriteGroup);
			}
		}
		if (hasPlant)
		{
			TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(1.25f, 1.25f), rangeEvent, new Array<TowerDefenseCharacter>(list), base.camp, instance.collisionFlags);
		}
	}

	private bool CanStealPlant(TowerDefenseCharacter target)
	{
		if (GodotObject.IsInstanceValid(target) && target is TowerDefensePlant && !(target is TowerDefensePlantBowlingBase) && !target.isDestroy && !target.die && GodotObject.IsInstanceValid(target.instance) && target.instance.canBeCollection && CanTarget(target))
		{
			return !target.IsHardControlImmune;
		}
		return false;
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Grab")
		{
			StealTargets();
			SendStateEvent("ToRise");
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
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		canBlock = false;
		instance.invincible = true;
		SendStateEvent("ToRise");
		target.Hurt(100.0);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "waitGrab", waitGrab },
			{ "hasPlant", hasPlant },
			{ "waitTimer", waitTimer },
			{ "canBlock", canBlock },
			{ "grabOver", grabOver }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		waitGrab = data.GetValueOrDefault("waitGrab", false).AsBool();
		hasPlant = data.GetValueOrDefault("hasPlant", false).AsBool();
		waitTimer = data.GetValueOrDefault("waitTimer", 0.0).AsDouble();
		canBlock = data.GetValueOrDefault("canBlock", true).AsBool();
		grabOver = data.GetValueOrDefault("grabOver", false).AsBool();
		if (IsNodeReady())
		{
			ApplyCurrentTargetability();
		}
	}
}
