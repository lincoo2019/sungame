using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePogoball : TowerDefenseZombie
{
	private StateHandle _pogoStateHandle;

	private bool _stateSignalsConnected;

	private static PackedScene _QUAKE_SCENE;

	private AttackComponent _attackComponent2;

	public bool isJump;

	private bool _hasPogo = true;

	public bool pogoPlant;

	public double jumpToPos;

	public int jumpWait = 2;

	public bool quake;

	public Tween moveTween;

	private static PackedScene QUAKE_SCENE => _QUAKE_SCENE ?? (_QUAKE_SCENE = GD.Load<PackedScene>("uid://dwcqjpnoyit8f"));

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	public bool hasPogo
	{
		get
		{
			return _hasPogo;
		}
		set
		{
			_hasPogo = value;
			if (!_hasPogo)
			{
				instance.collisionFlags = 1;
				idleAnimeClip = "Idle";
			}
		}
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_pogoStateHandle = base.StateMachine?.GetStateById("zombie.pogoball.pogo");
			StateHandle pogoStateHandle = _pogoStateHandle;
			if (pogoStateHandle != null && pogoStateHandle.IsValid)
			{
				_pogoStateHandle.Entered += PogoEntered;
				_pogoStateHandle.Exited += PogoExited;
				_pogoStateHandle.PhysicsProcessing += PogoProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_pogoStateHandle != null)
			{
				_pogoStateHandle.Entered -= PogoEntered;
				_pogoStateHandle.Exited -= PogoExited;
				_pogoStateHandle.PhysicsProcessing -= PogoProcessing;
			}
			_pogoStateHandle = null;
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
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			base.OnLand += Land;
			ySpeed = -300.0;
			ConnectStateSignals();
		}
	}

	public override void Walk()
	{
		if (hasPogo)
		{
			SendStateEvent("ToPogo");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void PogoEntered()
	{
		base.sprite.SetAnimation("Pogo", loop: true, 0.2);
		if (isGround)
		{
			ySpeed = -300.0;
		}
	}

	public void PogoProcessing(double delta)
	{
		if (die)
		{
			return;
		}
		base.sprite.timeScale = timeScale;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		bool flag = attackComponent.CanAttack();
		if (flag && attackComponent.target is TowerDefensePlant && (attackComponent.target.instance.physiqueTypeFlags & 0x10) != 0)
		{
			if (attackComponent.target.instance.spikeHurt != -1.0)
			{
				TowerDefenseCharacter target = attackComponent.target;
				double spikeHurt = attackComponent.target.instance.spikeHurt;
				target.Hurt(100.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
			}
			else
			{
				attackComponent.target.Destroy();
			}
			instance.ArmorDelete("Pogo");
			hasPogo = false;
			isJump = false;
			AudioManager.Instance.AudioPlay("Bonk");
			Walk();
		}
		else
		{
			if (pogoPlant)
			{
				return;
			}
			bool pause = base.sprite.pause;
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if (!quake && !pause)
			{
				double num = base.sprite.timeScale;
				double num2 = (base.sprite.playBack ? (-1.0) : 1.0);
				double num3 = 30.0 * delta * num * (double)transformPoint.Scale.X * (double)base.Scale.X * num2;
				if ((double)globalPositionForPhysicsFrame.X > groundRight)
				{
					num3 *= 2.0;
				}
				globalPositionForPhysicsFrame.X -= (float)num3;
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (!pause && (flag || attackComponent.CanAttack()))
			{
				quake = false;
				pogoPlant = true;
				float x = base.Scale.X;
				jumpToPos = globalPositionForPhysicsFrame.X - TowerDefenseManager.Instance.GetMapGridSize().X * x - 10f * x;
				if (_attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target) && _attackComponent2.target.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
				{
					quake = true;
					jumpToPos = globalPositionForPhysicsFrame.X;
				}
			}
		}
	}

	public void PogoExited()
	{
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pogo")
		{
			instance.unUseBuffFlags = 0;
			isJump = false;
			hasPogo = false;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
			if (base.inWater)
			{
				base.groundHeight = 0.0 - base.waterHeight;
			}
			Walk();
		}
	}

	public override void InWater()
	{
		base.InWater();
		if (hasPogo)
		{
			base.groundHeight = 0.0;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = false;
			}
		}
	}

	public override void OutWater()
	{
		if (hasPogo)
		{
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
		}
		base.OutWater();
		if (hasPogo)
		{
			ySpeed = -300.0;
		}
	}

	public void Land()
	{
		if (die || !hasPogo)
		{
			return;
		}
		isJump = false;
		gravity = 490.0;
		ySpeed = -300.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		if (_attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target) && _attackComponent2.target.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
		{
			quake = true;
		}
		if (quake)
		{
			ySpeed = -400.0;
			bool flag = false;
			if (GodotObject.IsInstanceValid(_attackComponent2.target) && GodotObject.IsInstanceValid(_attackComponent2.target.cell))
			{
				TowerDefenseCharacter surround = _attackComponent2.target.cell.GetSurround();
				if (GodotObject.IsInstanceValid(surround))
				{
					surround.Hurt(100.0);
					flag = true;
				}
			}
			if (!flag)
			{
				TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(1.25f, 0.25f), eventList, new Array<TowerDefenseCharacter>(), base.camp, instance.collisionFlags);
			}
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(QUAKE_SCENE, base.gridPos, "Idle");
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			quake = false;
		}
		if (pogoPlant)
		{
			CompletePogoPlantLandingAsync();
		}
	}

	private async void CompletePogoPlantLandingAsync()
	{
		if (die || !hasPogo || !pogoPlant)
		{
			return;
		}
		isJump = true;
		if (jumpWait > 0)
		{
			jumpWait--;
		}
		else
		{
			jumpWait = GD.RandRange(2, 3);
			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Sine);
			tween.TweenMethod(Callable.From<double>(SetPogoTweenX), GetLogicalGlobalPosition().X, jumpToPos, 0.5);
			ySpeed = -400.0;
			quake = true;
			await ToSignal(tween, Tween.SignalName.Finished);
			if (die || !IsInsideTree())
			{
				return;
			}
			pogoPlant = false;
		}
		await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (!die && IsInsideTree())
		{
			isJump = false;
		}
	}

	private void SetPogoTweenX(double value)
	{
		if (!die && IsInsideTree())
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = (float)value;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		isJump = false;
		pogoPlant = false;
		quake = false;
		ySpeed = 0.0;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "pogo"))
		{
			_ = command == "jump_check";
		}
	}

	public override bool CanBlock()
	{
		if (GodotObject.IsInstanceValid(moveTween))
		{
			if (hasPogo)
			{
				return !moveTween.IsRunning();
			}
			return false;
		}
		return hasPogo;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["isJump"] = isJump,
			["hasPogo"] = hasPogo,
			["pogoPlant"] = pogoPlant,
			["jumpToPos"] = jumpToPos,
			["jumpWait"] = jumpWait,
			["quake"] = quake
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isJump = data.GetValueOrDefault("isJump", isJump).AsBool();
		hasPogo = data.GetValueOrDefault("hasPogo", hasPogo).AsBool();
		pogoPlant = data.GetValueOrDefault("pogoPlant", pogoPlant).AsBool();
		jumpToPos = data.GetValueOrDefault("jumpToPos", jumpToPos).AsDouble();
		jumpWait = Math.Max(0, data.GetValueOrDefault("jumpWait", jumpWait).AsInt32());
		quake = data.GetValueOrDefault("quake", quake).AsBool();
	}

	public override void Block(TowerDefenseCharacter target)
	{
		pogoPlant = false;
		ySpeed = -500.0;
		target.Hurt(100.0);
		moveTween = CreateTween();
		moveTween.SetEase(Tween.EaseType.Out);
		moveTween.SetTrans(Tween.TransitionType.Cubic);
		float x = GetLogicalGlobalPosition().X;
		moveTween.TweenMethod(Callable.From<double>(SetPogoTweenX), x, (double)x + (double)TowerDefenseManager.Instance.GetMapGridSize().X * 1.5, 2.0);
		AudioManager.Instance.AudioPlay("Bonk");
	}
}
