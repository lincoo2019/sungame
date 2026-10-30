using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePaperball : TowerDefenseZombie
{
	private StateHandle _gaspStateHandle;

	private StateHandle _runStateHandle;

	private StateHandle _impactStateHandle;

	private StateHandle _restStateHandle;

	private bool _stateSignalsConnected;

	private const string ZOMBIE_PAPER_MADHEAD = "uid://bbu3gjf3ww3e7";

	private static PackedScene _HAMMER_EXPLOSION;

	public bool angry;

	public bool canRun = true;

	public bool isRun;

	public bool isGrap;

	private static PackedScene HAMMER_EXPLOSION => _HAMMER_EXPLOSION ?? (_HAMMER_EXPLOSION = GD.Load<PackedScene>("res://Prefab/Particles/Explosion/Hammer/HammerExplosion.tscn"));

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
		_gaspStateHandle = base.StateMachine?.GetStateById("zombie.paperball.gasp");
		_runStateHandle = base.StateMachine?.GetStateById("zombie.paperball.run");
		_impactStateHandle = base.StateMachine?.GetStateById("zombie.paperball.impact");
		_restStateHandle = base.StateMachine?.GetStateById("zombie.paperball.rest");
		StateHandle gaspStateHandle = _gaspStateHandle;
		if (gaspStateHandle == null || !gaspStateHandle.IsValid)
		{
			return;
		}
		StateHandle runStateHandle = _runStateHandle;
		if (runStateHandle == null || !runStateHandle.IsValid)
		{
			return;
		}
		StateHandle impactStateHandle = _impactStateHandle;
		if (impactStateHandle != null && impactStateHandle.IsValid)
		{
			StateHandle restStateHandle = _restStateHandle;
			if (restStateHandle != null && restStateHandle.IsValid)
			{
				_gaspStateHandle.Entered += GaspEntered;
				_gaspStateHandle.Exited += GaspExited;
				_gaspStateHandle.PhysicsProcessing += GaspProcessing;
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_impactStateHandle.Entered += ImpactEntered;
				_impactStateHandle.Exited += ImpactExited;
				_impactStateHandle.PhysicsProcessing += ImpactProcessing;
				_restStateHandle.Entered += RestEntered;
				_restStateHandle.Exited += RestExited;
				_restStateHandle.PhysicsProcessing += RestProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_gaspStateHandle != null)
			{
				_gaspStateHandle.Entered -= GaspEntered;
				_gaspStateHandle.Exited -= GaspExited;
				_gaspStateHandle.PhysicsProcessing -= GaspProcessing;
			}
			_gaspStateHandle = null;
			if (_runStateHandle != null)
			{
				_runStateHandle.Entered -= RunEntered;
				_runStateHandle.Exited -= RunExited;
				_runStateHandle.PhysicsProcessing -= RunProcessing;
			}
			_runStateHandle = null;
			if (_impactStateHandle != null)
			{
				_impactStateHandle.Entered -= ImpactEntered;
				_impactStateHandle.Exited -= ImpactExited;
				_impactStateHandle.PhysicsProcessing -= ImpactProcessing;
			}
			_impactStateHandle = null;
			if (_restStateHandle != null)
			{
				_restStateHandle.Entered -= RestEntered;
				_restStateHandle.Exited -= RestExited;
				_restStateHandle.PhysicsProcessing -= RestProcessing;
			}
			_restStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectStateSignals();
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

	public void GaspEntered()
	{
		isGrap = true;
		base.sprite.SetAnimation("Gasp", loop: false, 0.1);
	}

	public void GaspProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void GaspExited()
	{
		isGrap = false;
	}

	public void RunEntered()
	{
		isRun = true;
		base.sprite.SetAnimation("AngryRun");
		ActivateGroundMovementAfterStateDelay(_runStateHandle);
	}

	public void RunProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (attackComponent.CanAttackIgnoringLadderFilter())
		{
			SendStateEvent("ToImpact");
		}
	}

	public void RunExited()
	{
		isRun = false;
		groundMoveComponent.SetAlive(false);
	}

	public void ImpactEntered()
	{
		base.sprite.SetAnimation("Impact", loop: false);
	}

	public void ImpactProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.25;
	}

	public void ImpactExited()
	{
	}

	public void RestEntered()
	{
		base.sprite.SetAnimation("Rest", loop: false);
		base.sprite.AddAnimation("Up", 0.0, loop: false);
	}

	public void RestProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
	}

	public void RestExited()
	{
	}

	public override void WalkEntered()
	{
		if (!angry)
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		else
		{
			if (canRun)
			{
				SendStateEvent("ToRun");
				canRun = false;
				return;
			}
			base.sprite.SetAnimation("AngryWalk");
		}
		ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
	}

	public override void AttackEntered()
	{
		if (!angry)
		{
			base.sprite.SetAnimation(base.attackAnimeClip, loop: true, 0.2);
		}
		else
		{
			base.sprite.SetAnimation("AngryEat", loop: true, 0.2);
		}
		ActivateAttackAfterStateDelay(base.AttackStateHandle);
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (!(armorName == "Paper"))
		{
			if (armorName == "Helmet")
			{
				if (isRun)
				{
					Walk();
				}
				canRun = false;
			}
		}
		else
		{
			SendStateEvent("ToGasp");
			AudioManager.Instance.AudioPlay("NewspaperRip");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		switch (clip)
		{
		case "Gasp":
			AudioManager.Instance.AudioPlay("NewspaperRarrgh");
			base.sprite.SetAtlasReplace("Zombie_head.png", "uid://bbu3gjf3ww3e7");
			timeScaleInit = 3.0;
			angry = true;
			isGrap = false;
			Walk();
			break;
		case "Impact":
			SendStateEvent("ToRest");
			break;
		case "Up":
			Walk();
			break;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "impact") || !attackComponent.CanAttack())
		{
			return;
		}
		AudioManager.Instance.AudioPlay("Bonk");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(HAMMER_EXPLOSION, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		TowerDefenseCharacter target = attackComponent.target;
		if (GodotObject.IsInstanceValid(target))
		{
			target.AttackDeal(this, attackComponent.attackType, 500.0);
			if (GodotObject.IsInstanceValid(target.cell))
			{
				target.cell.AttackDeal(this, attackComponent.attackType, 500.0);
			}
			target.Hurt(500.0, playSplatAudio: true, Vector2.Zero);
		}
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetProcessMode(Tween.TweenProcessMode.Physics);
		tween.TweenMethod(Callable.From<Vector2>(base.SetLogicalGlobalPosition), logicalGlobalPosition, logicalGlobalPosition + new Vector2(50f * base.Scale.X, 0f), 0.1);
	}

	public override void Walk()
	{
		if (!isGrap)
		{
			base.Walk();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "angry", angry },
			{ "canRun", canRun },
			{ "isRun", isRun },
			{ "isGrap", isGrap }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		angry = data.GetValueOrDefault("angry", false).AsBool();
		canRun = data.GetValueOrDefault("canRun", true).AsBool();
		isRun = data.GetValueOrDefault("isRun", false).AsBool();
		isGrap = data.GetValueOrDefault("isGrap", false).AsBool();
	}
}
