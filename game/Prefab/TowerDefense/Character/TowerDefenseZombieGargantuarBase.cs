using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieGargantuarBase : TowerDefenseZombie
{
	[Export(PropertyHint.None, "")]
	public string fireAnimeClip = "Fire";

	[Export(PropertyHint.None, "")]
	public string smashAnimeEvent = "smash";

	[Export(PropertyHint.None, "")]
	public string impFireEvent = "fire";

	[Export(PropertyHint.None, "")]
	public string impName = "";

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSlot impSpawnSlot;

	private string _impFilter = "";

	public Array<string> impFliters = new Array<string>();

	[Export(PropertyHint.None, "")]
	public string impThrowDamagePointName = "ThrowImp";

	public bool impThrowFlag;

	public ImpThrowerComponent impThrowerComponent;

	public GargantuarSmashComponent gargantuarSmashComponent;

	private TowerDefenseCharacter _smashCycleTarget;

	private StateHandle _fireStateHandle;

	private bool _fireStateHandleConnected;

	[Export(PropertyHint.MultilineText, "")]
	public string impFilter
	{
		get
		{
			return _impFilter;
		}
		set
		{
			_impFilter = value;
			impFliters = new Array<string>(_impFilter.Split("&", StringSplitOptions.RemoveEmptyEntries));
		}
	}

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			base._Ready();
			if (GodotObject.IsInstanceValid(componentManager))
			{
				impThrowerComponent = componentManager.GetRuntime<ImpThrowerComponent>();
				gargantuarSmashComponent = componentManager.GetRuntime<GargantuarSmashComponent>();
			}
			ConnectFireStateHandle();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	private void ConnectFireStateHandle()
	{
		if (_fireStateHandleConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_fireStateHandle = base.StateMachine?.GetStateById("gargantuar.fire");
			StateHandle fireStateHandle = _fireStateHandle;
			if (fireStateHandle != null && fireStateHandle.IsValid)
			{
				_fireStateHandle.Entered += FireEntered;
				_fireStateHandle.Exited += FireExited;
				_fireStateHandle.PhysicsProcessing += FireProcessing;
				_fireStateHandleConnected = true;
			}
		}
	}

	private void DisconnectFireStateHandle()
	{
		if (_fireStateHandleConnected && _fireStateHandle != null)
		{
			_fireStateHandle.Entered -= FireEntered;
			_fireStateHandle.Exited -= FireExited;
			_fireStateHandle.PhysicsProcessing -= FireProcessing;
		}
		_fireStateHandle = null;
		_fireStateHandleConnected = false;
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (base.IsInsideComponentBattlefield && impThrowFlag)
		{
			Fire();
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		if (base.IsInsideComponentBattlefield && impThrowFlag)
		{
			Fire();
		}
	}

	public override void AttackEntered()
	{
		CaptureSmashCycleTarget();
		base.sprite.SetAnimation(base.attackAnimeClip, loop: true, 0.2);
	}

	public override void AttackProcessing(double delta)
	{
		if (TowerDefenseZombie.IsZombieRewindActive())
		{
			_smashCycleTarget = null;
			Walk();
		}
		else if (ShouldCancelSmashForBlowBack())
		{
			CancelSmashForBlowBack();
		}
		else
		{
			base.sprite.timeScale = timeScale;
		}
	}

	public override void AttackExited()
	{
		_smashCycleTarget = null;
		base.AttackExited();
	}

	public void FireEntered()
	{
		base.sprite.SetAnimation(fireAnimeClip, loop: false, 0.2);
	}

	public void FireProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void FireExited()
	{
	}

	public void Fire()
	{
		SendStateEvent("ToFire");
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == base.attackAnimeClip)
		{
			AttackComponent obj = attackComponent;
			bool flag = obj != null && !obj.IsReleased && attackComponent.CanAttack();
			if (!flag)
			{
				AttackComponent obj2 = attackComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					flag = attackComponent.TryRetargetImmediately();
				}
			}
			if (!flag)
			{
				_smashCycleTarget = null;
				Walk();
			}
			else
			{
				CaptureSmashCycleTarget();
			}
		}
		else if (clip == fireAnimeClip)
		{
			Walk();
		}
		else if (clip == base.dieAnimeClip)
		{
			AudioManager.Instance.AudioPlay("GargantuarThump");
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == smashAnimeEvent)
		{
			if (IsCommittedSmashTargetValid(_smashCycleTarget))
			{
				attackComponent.target = _smashCycleTarget;
			}
			else
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased && attackComponent.TryRetargetImmediately())
				{
					_smashCycleTarget = attackComponent.target;
				}
				else
				{
					_smashCycleTarget = null;
					AttackComponent obj2 = attackComponent;
					if (obj2 != null && !obj2.IsReleased)
					{
						attackComponent.target = null;
					}
				}
			}
			gargantuarSmashComponent?.SmashAttack();
			_smashCycleTarget = null;
		}
		else if (command == impFireEvent && impThrowFlag)
		{
			impThrowFlag = false;
			impThrowerComponent?.SetImpFilter();
			impThrowerComponent?.SpawnImp();
		}
	}

	private void CaptureSmashCycleTarget()
	{
		AttackComponent obj = attackComponent;
		TowerDefenseCharacter towerDefenseCharacter = ((obj != null && !obj.IsReleased) ? attackComponent.target : null);
		_smashCycleTarget = (IsCommittedSmashTargetValid(towerDefenseCharacter) ? towerDefenseCharacter : null);
	}

	private bool ShouldCancelSmashForBlowBack()
	{
		BlowBackComponent obj = blowBackComponent;
		if (obj != null && !obj.IsReleased)
		{
			return blowBackComponent.blowBack;
		}
		return false;
	}

	private void CancelSmashForBlowBack()
	{
		_smashCycleTarget = null;
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.target = null;
		}
		WalkWithoutTransitionDelay();
	}

	private bool IsCommittedSmashTargetValid(TowerDefenseCharacter candidate)
	{
		if (!GodotObject.IsInstanceValid(candidate) || !GodotObject.IsInstanceValid(candidate.instance))
		{
			return false;
		}
		if (candidate.die || candidate.nearDie || candidate.isDestroy)
		{
			return false;
		}
		if (candidate.instance.invincible || !candidate.instance.canBeCollection)
		{
			return false;
		}
		if (!candidate.HasHitBox)
		{
			return false;
		}
		if (!CanTarget(candidate) || !CanCollision(candidate.instance.maskFlags))
		{
			return false;
		}
		if (!candidate.IsTargetableFromLine(base.gridPos.Y))
		{
			return false;
		}
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased && (double)candidate.GetLogicalGlobalPosition().X > attackComponent.groundRight)
		{
			return false;
		}
		return true;
	}

	public void SetImpFilter(bool open = false)
	{
		impThrowerComponent?.SetImpFilter(open);
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == impThrowDamagePointName)
		{
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			if (mapFeature != null && (double)GetLogicalGlobalPosition().X > (double)mapFeature.config.edge.X + (double)TowerDefenseManager.Instance.GetMapGridSize().X * 5.0)
			{
				impThrowFlag = true;
			}
		}
		else if (damagePointName == base.dieAnimeClip)
		{
			AudioManager.Instance.AudioPlay("GargantuarDeath");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["impThrowFlag"] = impThrowFlag };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		impThrowFlag = data.GetValueOrDefault("impThrowFlag", false).AsBool();
	}
}
